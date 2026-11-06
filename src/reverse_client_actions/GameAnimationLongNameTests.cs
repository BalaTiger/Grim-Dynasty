using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

public static class GameAnimationLongNameTests {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static byte[] Bytes(int count,byte value){var b=new byte[count];for(int n=0;n<count;n++)b[n]=value;return b;}
 static Field<EngineResourceStringState> Source(byte[] data){var s=new EngineResourceStringState();s.Capacity.Write((ulong)(data.Length|15));s.Length.Write((ulong)data.Length);var b=data.Length<16?s.Inline:new EngineResourceByteStorage((data.Length|15)+1);if(data.Length>=16)s.PublishHeap(b);for(int n=0;n<data.Length;n++)b.Byte(n).Write(data[n]);b.Byte(data.Length).Write(0);return Field<EngineResourceStringState>.Of(s);}
 static GameAnimationEntryState Entry(){var e=new GameAnimationEntryState();e.Resource10.Write(null);return e;}
 sealed class Fixture {
  public readonly WorldEngine Engine=new WorldEngine();public readonly object Graphics=new object();public readonly Field<WorldEngine> Global;
  public readonly GameAnimationResourceBindingContext Context;public readonly EngineAnimationResourceManagerState Manager;
  public Fixture():this(new EngineAnimationResourceCacheNode[0]){}
  public Fixture(EngineAnimationResourceCacheNode[] nodes){Global=Field<WorldEngine>.Of(Engine);EngineGraphicsResourceEngineFields.ForEngine(Engine).Graphics1F8.Write(Graphics);Manager=EngineAnimationResourceManagerState.CreateOwnInitialized(Field<object>.Of(new object()),Global,nodes,new EngineAnimationResourcePair[0]);EngineGraphicsAnimationFields.ForGraphics(Graphics).Manager20.Write(Manager);Context=new GameAnimationResourceBindingContext(Global,EngineAnimationNameLocale.KnownC);}
  public byte Add(GameAnimationEntryState e,byte[] name,GameAnimationResourceBindingInvocation c){return GameAnimationResourceBinding.Add020910(Context,e,Source(name),Field<float>.Of(2),Field<float>.Of(3),c);}
 }
 static void Ordered(GameAnimationResourceBindingInvocation c){
  uint[] expected={0x0163b7,0x016414,0x01641c,0x016433,0x016436,0x016439,0x01643d,0x01644b,0x016458,0x01655e,0x016568,0x016576,0x0209c3,0x0209c8,0x0209cd};int last=-1;
  foreach(uint at in expected){int index=c.Stages.IndexOf(at);Check(index>last,"Native selected write/call order at"+at.ToString("x"));last=index;}
 }
 public static int Main(){try{Run();int newScenarios=scenarios,newAssertions=assertions;GameAnimationResourceBindingTests.RunNameBoundaryRegression();Console.WriteLine("{\"status\":\"passed-animation-long-name-components-only\",\"scenarios\":"+(newScenarios+GameAnimationResourceBindingTests.Scenarios)+",\"assertions\":"+(newAssertions+GameAnimationResourceBindingTests.Assertions)+",\"new_scenarios\":"+newScenarios+",\"new_assertions\":"+newAssertions+",\"old_boundary_regressions\":"+GameAnimationResourceBindingTests.Scenarios+",\"old_boundary_assertions\":"+GameAnimationResourceBindingTests.Assertions+",\"native_allocator_verified\":false,\"native_decoding\":false,\"native_playback\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"gameplay_accepted\":false}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 public static void Run(){
  scenarios=assertions=0;
  Test("Fresh Game growth covers capacity boundaries and complete bounded bytes",delegate{
   var f=new Fixture();foreach(int length in new[]{16,31,32,255,256,65536}){var data=Bytes(length,65);var e=Entry();var c=new GameAnimationResourceBindingInvocation();Eq((byte)1,f.Add(e,data,c),"Normal load");Eq((ulong)(length|15),c.LocalName.Capacity.Read(0),"Game fresh capacity");Eq((length|15)+1,c.OwnAllocationBytes,"Allocation includes terminator");Eq((ulong)length,c.LocalName.Length.Read(0),"New length after growth returned");Eq((byte)0,c.LocalName.ReadByte(length,0),"Final NUL");bool complete=true;for(int n=0;n<length;n++)if(c.LocalName.ReadByte(n,0)!=65||c.Load.ProcessedName.Byte(n)!=97){complete=false;break;}Check(complete,"All copied bytes and canonical bytes match");Check(c.GrowthEntered&&c.GrowthReturned&&c.OwnHeapAllocated&&c.OwnHeapNormalRelease&&c.Returned,"OWN normal heap lifetime markers");Same(c.Load.Construction.Result,e.Resource10.Read(0),"Factory identity");Missing(0,delegate{c.LocalName.Inline.Byte(0).Read(0);},"Pointer publication invalidated inline bytes");if((length|15)>length)Missing(0,delegate{c.LocalName.ReadByte(length|15,0);},"Unwritten allocation padding remains Unknown");Ordered(c);}
  });
  Test("Long embedded NUL canonical names share same cache resource",delegate{
   var f=new Fixture();var first=Bytes(30,65);first[3]=92;first[20]=0;var e1=Entry();var c1=new GameAnimationResourceBindingInvocation();f.Add(e1,first,c1);var second=Bytes(20,97);second[3]=47;var e2=Entry();var c2=new GameAnimationResourceBindingInvocation();f.Add(e2,second,c2);Eq(20,c1.ScannedLength,"C prefix only");Eq(20,c1.Load.ProcessedName.Length,"Manager receives prefix");Same(e1.Resource10.Read(0),e2.Resource10.Read(0),"Same canonical resource");Same(c1.Load.AllocatedPair,c2.Load.FirstPair,"Same reference record");Eq(2u,c2.Load.FirstPair.Count00.Read(0),"Reference increment");Check(!c2.Load.Cold&&c2.OwnHeapNormalRelease,"Warm path still releases own temporary");
  });
  Test("Long-name null manager returnsfalse after heap release",delegate{
   var f=new Fixture();EngineGraphicsAnimationFields.ForGraphics(f.Graphics).Manager20.Write(null);var e=Entry();var c=new GameAnimationResourceBindingInvocation();Eq((byte)0,f.Add(e,Bytes(20,65),c),"Normal null manager");Check(c.GrowthReturned&&c.OwnHeapNormalRelease&&c.LocalNameNormalRelease&&c.Returned&&!c.ResourcePublished,"Release before result test");Same(null,e.Resource10.Read(0),"Entry still null");Missing(0,delegate{e.Float1C.Read(0);},"Float not written");
  });
  Test("Unknown warm pointer stops only after long temporary release",delegate{
   var pair=new EngineAnimationResourcePair();pair.Count00.Write(1);var node=new EngineAnimationResourceCacheNode(Bytes(20,97),Field<EngineAnimationResourcePair>.Of(pair));var f=new Fixture(new[]{node});var c=new GameAnimationResourceBindingInvocation();var e=Entry();Missing(0x0209c8,delegate{f.Add(e,Bytes(20,65),c);},"Unknown resource TEST");Eq(2u,pair.Count00.Read(0),"Reference prefix");Check(c.GrowthReturned&&c.ManagerReturned&&c.OwnHeapNormalRelease&&!c.Returned&&!c.ResourcePublished,"Cleanup does not fake success");Same(null,e.Resource10.Read(0),"No publication");
  });
  Test("Unsupported manager locale does not claim native exception cleanup",delegate{
   var f=new Fixture();var context=new GameAnimationResourceBindingContext(f.Global,EngineAnimationNameLocale.Unknown);var c=new GameAnimationResourceBindingInvocation();var e=Entry();Missing(0x27b418,delegate{GameAnimationResourceBinding.Add020910(context,e,Source(Bytes(20,65)),Field<float>.Of(2),Field<float>.Of(3),c);},"Unknown locale on heap manager key");Check(c.GrowthReturned&&c.OwnHeapAllocated&&!c.OwnHeapNormalRelease&&!c.LocalNameNormalRelease&&!c.Returned,"Native unwind left unclaimed");Check(c.Load.OwnSafetyUnlock&&!f.Manager.SynchronizationC0.Read(0).IsHeldByCurrentThread,"Manager own lock released");Same(null,e.Resource10.Read(0),"No false publication");
  });
  Test("Ninth long key retains native publication boundary",delegate{
   var f=new Fixture();for(byte n=0;n<8;n++)f.Add(Entry(),Bytes(20,(byte)(65+n)),new GameAnimationResourceBindingInvocation());var c=new GameAnimationResourceBindingInvocation();var e=Entry();Missing(0x02eae9,delegate{f.Add(e,Bytes(20,73),c);},"Ninth rehash remains unsupported");Eq(9,f.Manager.Cache40.Read(0).Count,"Map prefix preserved");Eq(8,f.Manager.ResourceVector80.Read(0).Count,"No vector append");Check(c.GrowthReturned&&c.Load.MapPublished&&f.Manager.RequiresResolution&&!c.OwnHeapNormalRelease&&!c.Returned,"No guessed cleanup or success");Same(null,e.Resource10.Read(0),"Entry unchanged");
  });
  Test("Byte coverage limit stops before growth and already-present bypass remains",delegate{
   var f=new Fixture();var c=new GameAnimationResourceBindingInvocation();Missing(0x020977,delegate{f.Add(Entry(),Bytes(65537,65),c);},"Bounded scan coverage");Eq(65537,c.ScannedLength,"Known non-NUL prefix before coverage stop");Check(!c.GrowthEntered&&!c.OwnHeapAllocated&&c.Load==null,"No unbounded allocation");Eq(0UL,c.LocalName.Length.Read(0),"Initial temporary prefix retained");
   var e=Entry();var existing=new EngineGraphicsResourceState();e.Resource10.Write(existing);var bypass=new GameAnimationResourceBindingInvocation();Eq((byte)1,f.Add(e,Bytes(65537,65),bypass),"Native already-present early return does not scan");Same(existing,e.Resource10.Read(0),"Existing identity");Check(bypass.AlreadyPresent&&!bypass.GrowthEntered&&!bypass.NameCopied,"No new heap");
  });
 }
}
