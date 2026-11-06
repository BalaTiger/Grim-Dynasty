using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

public static class EngineGraphicsResourceConstructionTests {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static Field<EngineResourceNameInput> Name(params byte[] bytes){return Field<EngineResourceNameInput>.Of(new EngineResourceNameInput(bytes));}
 static Field<object> Manager(){return Field<object>.Of(new object());}
 static Field<WorldEngine> Engine(object graphics){var e=new WorldEngine();EngineGraphicsResourceEngineFields.ForEngine(e).Graphics1F8.Write(graphics);return Field<WorldEngine>.Of(e);}
 static EngineGraphicsResourceConstructionInvocation Call(){return new EngineGraphicsResourceConstructionInvocation();}
 static EngineGraphicsResourceState Create(Field<object> manager,Field<EngineResourceNameInput> name,Field<WorldEngine> engine,EngineGraphicsResourceConstructionInvocation call){return EngineGraphicsResourceConstruction.Create094650(manager,name,engine,call);}
 static void EmptyString(EngineResourceStringState s){Eq(15UL,s.Capacity.Read(0),"Empty capacity");Eq(0UL,s.Length.Read(0),"Empty length");Eq((byte)0,s.ReadByte(0,0),"Only terminator initialized");for(int n=1;n<16;n++)Unknown(s.Inline.Byte(n),"Unwritten inline byte "+n);Unknown(s.Heap,"Empty inline string has no invented heap pointer");}
 static EngineResourceConstructionWrite WriteAt(EngineGraphicsResourceConstructionInvocation c,uint at){foreach(var w in c.Writes)if(w.At==at)return w;throw new Exception("Missing write at "+at.ToString("x"));}
 static void PrefixBeforeGlobal(EngineGraphicsResourceState s,EngineGraphicsResourceConstructionInvocation c){
  Check(c.BaseReturned&&c.NameCopyReturned&&!c.GraphicsReturned&&!c.Returned,"Base returned; derived did not return");Same(s,c.BaseResult,"Base and derived retain original this");Check(c.Result==null&&c.GraphicsResult==null,"No fake return");
  Eq(0x18033f670UL,s.Virtual00.Read(0),"Derived vtable was written");Eq(0UL,s.Qword110.Read(0),"Last vector field preceded global read");Eq(15UL,s.Name178.Capacity.Read(0),"Last string initialized before global read");Eq((byte)0,s.Name178.ReadByte(0,0),"Last string terminator precedes global read");
  Eq(0x095f83u,c.Writes[c.Writes.Count-1].At,"Exact final completed write");Eq(0x095f90u,c.Native.At,"Global pointer first consumed at dereference");
 }
 public static void Run(){
  scenarios=assertions=0;
  Test("Factory returns one shared Resource/GraphicsAnim identity",delegate{
   var manager=Manager();var graphics=new object();var c=Call();var s=Create(manager,Name(0x41,0,0xff,0x5c,0x42),Engine(graphics),c);
   Same(s,c.AllocatedResource,"Factory allocation identity");Same(s,c.Target,"Target identity");Same(s,c.BaseResult,"Base identity");Same(s,c.GraphicsResult,"Derived identity");Same(s,c.Result,"Final identity");Eq(0x1a0,c.NativeRequestedAllocationBytes,"Native request size, not managed memory usage");
   Check(c.BaseEntered&&c.BaseReturned&&c.GraphicsEntered&&c.GraphicsReturned&&c.Returned,"All normal returns completed");Eq(0x094692u,c.Native.At,"Factory return");
   Eq(0x18033f670UL,s.Virtual00.Read(0),"Derived vtable replaces base in SAME cell");Same(manager.Read(0),s.Manager38.Read(0),"Manager retained");Same(graphics,s.GraphicsA8.Read(0),"Graphics value retained");Check(s.Loader28.Read(0)==null,"Loader initialized null, not a fabricated loader");Eq(0u,s.Type30.Read(0),"Derived passes type0");
   byte[] wanted={0x41,0,0xff,0x5c,0x42};Eq(5UL,s.Name08.Length.Read(0),"Full byte length");for(int n=0;n<wanted.Length;n++)Eq(wanted[n],s.Name08.ReadByte(n,0),"No truncation, normalization or signed-byte rewrite");Eq((byte)0,s.Name08.ReadByte(5,0),"Final terminator");Unknown(s.Name08.Inline.Byte(6),"Spare inline byte remains unknown");
   Eq(0UL,s.Qword44.Read(0),"Unaligned zero qword");Eq(4u,s.Dword74.Read(0),"Resource74 constant");Eq(0UL,s.Qword50.Read(0),"Late base write");Eq(0UL,s.QwordC8.Read(0),"Derived post-global C8");Eq(0u,s.Dword80.Read(0),"Derived DWORD80");Eq((byte)0,s.Byte198.Read(0),"Derived byte198");
   s.Loader28.Write(new object());Check(s.Loader28.Read(0)!=null,"Same mutable resource retains later loader publication");Same(s,c.BaseResult,"No copied base state");
  });
  Test("Standalone base constructor preserves supplied type and does not run derived",delegate{
   var s=new EngineGraphicsResourceState();var c=Call();Same(s,EngineGraphicsResourceConstruction.ConstructResource199970(s,Manager(),Name(),0xfedcba98,c),"Base returns this");
   Eq(0xfedcba98u,s.Type30.Read(0),"Full DWORD type");Eq(0x180342cc8UL,s.Virtual00.Read(0),"Base table");Eq(0x18033d398UL,s.Virtual58.Read(0),"Subobject table58");Eq(0x180348880UL,s.Virtual60.Read(0),"Subobject table60");
   Check(c.BaseReturned&&!c.GraphicsEntered&&c.Returned,"Only base path executed");Unknown(s.Name88.Capacity,"Derived string untouched");Unknown(s.GraphicsA8,"No graphics field invented");Unknown(s.Dword80,"No derived80 write");
  });
  Test("Empty slots initialize only proven bytes",delegate{
   var s=Create(Manager(),Name(),Engine(new object()),Call());EmptyString(s.Name08);EmptyString(s.Name88);EmptyString(s.Name118);EmptyString(s.Name138);EmptyString(s.Name158);EmptyString(s.Name178);
  });
  foreach(int length in new[]{15,16,31,32}){int count=length;Test("Name storage boundary "+count,delegate{
   var bytes=new byte[count];for(int n=0;n<count;n++)bytes[n]=(byte)(n+0x80);bytes[count/2]=0;var c=Call();var s=Create(Manager(),Name(bytes),Engine(null),c);
   Eq((ulong)count,s.Name08.Length.Read(0),"Complete copied length");Eq(count<=15?15UL:((ulong)count|15UL),s.Name08.Capacity.Read(0),"Fresh growth capacity");for(int n=0;n<count;n++)Eq(bytes[n],s.Name08.ReadByte(n,0),"Copied byte");Eq((byte)0,s.Name08.ReadByte(count,0),"Terminator at full length");
   if(count>15){var heap=s.Name08.Heap.Read(0);Eq((int)s.Name08.Capacity.Read(0)+1,heap.Extent,"Declared capacity plus terminator extent");for(int n=0;n<16;n++)Unknown(s.Name08.Inline.Byte(n),"Pointer bytes and untouched inline tail are not fabricated");if(count+1<heap.Extent)Unknown(heap.Byte(count+1),"Heap spare byte unknown");Check(WriteAt(c,0x023066).Width==8,"Heap identity published at a qword store");}
   else Unknown(s.Name08.Heap,"SSO does not allocate a heap");
  });}
  Test("Input admission owns a snapshot without modifying requested bytes",delegate{
   byte[] bytes={0x41,0,0x5c};var input=new EngineResourceNameInput(bytes);bytes[0]=0x7f;var s=Create(Manager(),Field<EngineResourceNameInput>.Of(input),Engine(null),Call());Eq((byte)0x41,s.Name08.ReadByte(0,0),"Admitted source snapshot unchanged");Eq((byte)0x7f,bytes[0],"Construction never mutates caller buffer");
  });
  Test("Unwritten existing inline bytes survive construction",delegate{
   var s=new EngineGraphicsResourceState();s.Name08.Inline.Byte(9).Write(0xa5);s.Name88.Heap.Write(new EngineResourceByteStorage(32));for(int n=1;n<16;n++)s.Name88.Inline.Byte(n).Write((byte)(0x60+n));
   var c=Call();EngineGraphicsResourceConstruction.ConstructGraphics095EA0(s,Manager(),Name(0x41),Engine(null),c);
   Eq((byte)0xa5,s.Name08.Inline.Byte(9).Read(0),"Name copy does not clear spare storage");Unknown(s.Name88.Heap,"Actual inline byte0 write invalidates the overlapping old pointer identity");
   for(int n=1;n<16;n++)Eq((byte)(0x60+n),s.Name88.Inline.Byte(n).Read(0),"Empty derived string preserves unwritten sentinel byte "+n);
  });
  Test("Public name reader rejects unknown, null and undeclared storage",delegate{
   var s=new EngineResourceStringState();Missing(0x11,delegate{s.ReadByte(0,0x11);},"Unconstructed capacity remains Unknown");s.Capacity.Write(16);s.Heap.Write(null);Missing(0x22,delegate{s.ReadByte(0,0x22);},"Known-null heap does not escape as NRE");s.Capacity.Write(15);Missing(0x33,delegate{s.ReadByte(16,0x33);},"Declared inline extent enforced");Missing(0x44,delegate{s.ReadByte(-1,0x44);},"Negative byte outside OWN extent");
  });
  Test("Known Engine with unknown graphics value returns normally",delegate{
   var e=new WorldEngine();var fields=EngineGraphicsResourceEngineFields.ForEngine(e);Same(fields,EngineGraphicsResourceEngineFields.ForEngine(e),"Canonical fields per existing Engine");Same(e,fields.Owner,"Original Engine identity");var c=Call();var s=Create(Manager(),Name(),Field<WorldEngine>.Of(e),c);
   Unknown(fields.Graphics1F8,"Binding publishes nothing");Unknown(c.CapturedGraphics,"Opaque captured graphics");Unknown(s.GraphicsA8,"Unknown is copied rather than dereferenced");Check(c.Returned&&c.GraphicsReturned,"Opaque copy allows normal return");Eq(0UL,s.QwordC8.Read(0),"Following stores execute");Eq((byte)0,s.Byte198.Read(0),"Final byte executes");Unknown(e.World50,"No unrelated Engine mutation");
  });
  Test("Known-null graphics value is copied without use",delegate{var s=Create(Manager(),Name(),Engine(null),Call());Check(s.GraphicsA8.Read(0)==null,"Known null graphics copied");});
  Test("Unknown global stops after complete preceding writes and preserves tail",delegate{
   var s=new EngineGraphicsResourceState();var prior=new object();s.GraphicsA8.Write(prior);s.QwordC8.Write(71);s.Dword80.Write(72);s.Byte198.Write(73);var c=Call();
   Missing(0x095f90,delegate{EngineGraphicsResourceConstruction.ConstructGraphics095EA0(s,Manager(),Name(),new Field<WorldEngine>(),c);},"Unknown gEngine consumption");PrefixBeforeGlobal(s,c);Same(prior,s.GraphicsA8.Read(0),"A8 not written");Eq(71UL,s.QwordC8.Read(0),"C8 retained");Eq(72u,s.Dword80.Read(0),"80 retained");Eq((byte)73,s.Byte198.Read(0),"198 retained");
  });
  Test("Known-null global keeps allocated identity and fails at dereference",delegate{
   var c=Call();Missing(0x095f90,delegate{Create(Manager(),Name(),Field<WorldEngine>.Of(null),c);},"Known-null gEngine");Check(c.AllocatedResource!=null,"OWN allocation retained for inspection");PrefixBeforeGlobal(c.AllocatedResource,c);Unknown(c.AllocatedResource.GraphicsA8,"No A8 store");Unknown(c.AllocatedResource.Byte198,"No final store");
  });
  Test("Unknown and null name inputs retain base initialization only",delegate{
   foreach(var input in new[]{new Field<EngineResourceNameInput>(),Field<EngineResourceNameInput>.Of(null)}){
    var s=new EngineGraphicsResourceState();var loader=new object();s.Loader28.Write(loader);var c=Call();
    Missing(0x023195,delegate{EngineGraphicsResourceConstruction.ConstructGraphics095EA0(s,Manager(),input,Engine(null),c);},"Source length unavailable");
    Eq(0x180342cc8UL,s.Virtual00.Read(0),"Base vptr prefix survives");EmptyString(s.Name08);Same(loader,s.Loader28.Read(0),"Post-name loader write has not run");Unknown(s.Type30,"Type store not run");Unknown(s.Synchronization68,"Platform construction not run");Unknown(s.Name88.Capacity,"Derived writes not run");Check(!c.NameCopyReturned&&!c.BaseReturned&&!c.GraphicsReturned&&!c.Returned,"No false completion");Eq(0x1999c1u,c.Writes[c.Writes.Count-1].At,"Exact completed caller prefix");
   }
  });
  Test("Name budget rejection retains factory and base prefix",delegate{
   var c=Call();var oversized=Name(new byte[EngineGraphicsResourceConstruction.MaximumNameBytes+1]);Missing(0x023195,delegate{Create(Manager(),oversized,Engine(null),c);},"Explicit OWN input limit");Check(c.AllocatedResource!=null&&c.Result==null,"Failed object is retained but not returned");Eq(0x180342cc8UL,c.AllocatedResource.Virtual00.Read(0),"No derived table after unsupported copy");Unknown(c.AllocatedResource.Manager38,"No late manager publication");Unknown(c.AllocatedResource.Loader28,"No post-copy base fields");
  });
  Test("Unknown and null manager pointers copy without dereference",delegate{
   var s=new EngineGraphicsResourceState();s.Manager38.Write(new object());var c=Call();EngineGraphicsResourceConstruction.ConstructResource199970(s,new Field<object>(),Name(),0,c);Unknown(s.Manager38,"Unknown parameter overwrites prior value opaquely");Check(c.Returned,"Unknown manager is not a failed dereference");
   var second=Create(Field<object>.Of(null),Name(),Engine(null),Call());Check(second.Manager38.Read(0)==null,"Known-null manager retained");
  });
  Test("Captured manager and graphics values are not live field aliases",delegate{
   var manager=Manager();object oldManager=manager.Read(0),oldGraphics=new object();var engine=Engine(oldGraphics);var e=engine.Read(0);var c=Call();var first=Create(manager,Name(),engine,c);var newManager=new object();var newGraphics=new object();manager.Write(newManager);EngineGraphicsResourceEngineFields.ForEngine(e).Graphics1F8.Write(newGraphics);engine.Write(new WorldEngine());
   Same(oldManager,first.Manager38.Read(0),"Resource manager is a copied value");Same(oldGraphics,first.GraphicsA8.Read(0),"Resource graphics is a copied value");Same(e,c.CapturedEngine.Read(0),"Invocation global capture remains the original identity");
   var next=Create(manager,Name(),Field<WorldEngine>.Of(e),c);Same(newManager,next.Manager38.Read(0),"Next call captures fresh manager");Same(newGraphics,next.GraphicsA8.Read(0),"Next call rereads Engine graphics");Same(oldGraphics,first.GraphicsA8.Read(0),"Reuse of invocation does not modify previous object");
  });
  Test("OWN monitor is usable and private to each constructed resource",delegate{
   var a=Create(Manager(),Name(),Engine(null),Call());var b=Create(Manager(),Name(),Engine(null),Call());var ma=a.Synchronization68.Read(0);var mb=b.Synchronization68.Read(0);Check(ma.Initialized&&mb.Initialized&&!Object.ReferenceEquals(ma,mb),"Independent initialized OWN monitor identities");
   ma.Enter();try{Check(ma.IsHeldByCurrentThread&&!mb.IsHeldByCurrentThread,"Actual platform monitor ownership");}finally{ma.Exit();}Check(!ma.IsHeldByCurrentThread,"OWN monitor released by its caller");
  });
  Test("Instruction-width and publication order retain actual boundaries",delegate{
   var c=Call();Create(Manager(),Name(),Engine(null),c);var unaligned=WriteAt(c,0x1999de);Eq(0x44u,unaligned.Offset,"Unaligned base offset");Eq(8,unaligned.Width,"Unaligned store is QWORD, not DWORD");Eq(4,WriteAt(c,0x095fa8).Width,"80 is DWORD");Eq(1,WriteAt(c,0x095fae).Width,"198 is BYTE");
   int sync=c.Writes.IndexOf(WriteAt(c,0x199a11)),manager=c.Writes.IndexOf(WriteAt(c,0x199a25)),baseTail=c.Writes.IndexOf(WriteAt(c,0x199a30)),derived=c.Writes.IndexOf(WriteAt(c,0x095eba));Check(sync<manager&&manager<baseTail&&baseTail<derived,"Base pointer publication and tail finish before derived writes");
   int n=c.Writes.Count;Eq(0x095f9au,c.Writes[n-4].At,"Graphics copy first in tail");Eq(0x095fa1u,c.Writes[n-3].At,"Then C8");Eq(0x095fa8u,c.Writes[n-2].At,"Then80");Eq(0x095faeu,c.Writes[n-1].At,"Then198");
  });
 }
 public static int Main(string[] args){try{if(args.Length!=0)throw new ArgumentException("No arguments");Run();Console.WriteLine("{\"status\":\"passed-resource-construction-components-only\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"native_decoding\":false,\"native_playback\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"gameplay_accepted\":false,\"old_test_suites_rerun\":0,\"own_platform_allocation_and_monitor\":true}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
