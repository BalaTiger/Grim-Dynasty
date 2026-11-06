using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

public static class GameAnimationResourceBindingTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static GameAnimationResourceBindingInvocation Call(){return new GameAnimationResourceBindingInvocation();}
 static Field<float> F(float value){return Field<float>.Of(value);}
 static EngineResourceStringState Source(params byte[] data){
  var s=new EngineResourceStringState();s.Length.Write((ulong)data.Length);int capacity=data.Length<=15?15:data.Length|15;s.Capacity.Write((ulong)capacity);
  var storage=s.Inline;if(capacity>=16){storage=new EngineResourceByteStorage(capacity+1);s.PublishHeap(storage);}for(int n=0;n<data.Length;n++)storage.Byte(n).Write(data[n]);storage.Byte(data.Length).Write(0);return s;
 }
 static Field<EngineResourceStringState> S(params byte[] data){return Field<EngineResourceStringState>.Of(Source(data));}
 static GameAnimationEntryState Entry(){var e=new GameAnimationEntryState();e.Virtual00.Write(0x1806d4d48UL);e.Resource10.Write(null);return e;}
 sealed class Fixture {
  public readonly WorldEngine Engine=new WorldEngine();public readonly object Graphics=new object(),Loader=new object();
  public readonly Field<WorldEngine> Global;public readonly GameAnimationResourceBindingContext Context;
  public readonly EngineAnimationResourceManagerState Manager;public readonly AnimationSet Set=new AnimationSet();
  public readonly GameMovementCharacterState Owner=new GameMovementCharacterState(new EntityMovementState(new object()));
  public Fixture():this(new EngineAnimationResourceCacheNode[0]){}
  public Fixture(EngineAnimationResourceCacheNode[] nodes){
   Global=Field<WorldEngine>.Of(Engine);Context=new GameAnimationResourceBindingContext(Global,EngineAnimationNameLocale.KnownC);EngineGraphicsResourceEngineFields.ForEngine(Engine).Graphics1F8.Write(Graphics);
   Manager=EngineAnimationResourceManagerState.CreateOwnInitialized(Field<object>.Of(Loader),Global,nodes,new EngineAnimationResourcePair[0]);EngineGraphicsAnimationFields.ForGraphics(Graphics).Manager20.Write(Manager);
   Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);var sets=GameCharacterAnimationFields.ForOwner(Owner);sets.Set2600.Write(null);sets.Set2608.Write(null);sets.Set2578.Write(Set);
  }
  public Field<AnimationSet> Current(){return GameCharacterAnimationContracts.GetAnimationSet05AE40(Owner,new GameGetAnimationSetInvocation());}
  public byte Add(GameAnimationEntryState entry,Field<EngineResourceStringState> source,GameAnimationResourceBindingInvocation c){return GameAnimationResourceBinding.Add020910(Context,entry,source,F(1.5f),F(-2.0f),c);}
 }
 static EngineAnimationResourcePair Pair(Field<EngineGraphicsResourceState> resource){var p=new EngineAnimationResourcePair();p.Count00.Write(1);p.Resource08.CopyFrom(resource);return p;}
 static EngineAnimationResourceCacheNode Node(byte[] key,EngineAnimationResourcePair pair){return new EngineAnimationResourceCacheNode(key,Field<EngineAnimationResourcePair>.Of(pair));}
 public static void RunNameBoundaryRegression(){scenarios=assertions=0;Test("Fifteen-byte inline and sixteen-byte fresh Game growth",NameBoundaryRegression);}
 static void NameBoundaryRegression(){
  var f=new Fixture();byte[] fifteen=new byte[15];for(int n=0;n<15;n++)fifteen[n]=65;var ok=Call();f.Add(Entry(),S(fifteen),ok);Eq(15,ok.Load.ProcessedName.Length,"SSO boundary");Check(!ok.GrowthEntered&&!ok.OwnHeapNormalRelease,"No allocation on short temporary");
  byte[] sixteen=new byte[16];for(int n=0;n<16;n++)sixteen[n]=66;var c=Call();var entry=Entry();Eq((byte)1,f.Add(entry,S(sixteen),c),"Separately recovered Game growth returns normally");Eq(16,c.ScannedLength,"Scan length");Eq(31UL,c.LocalName.Capacity.Read(0),"Actual Game fresh capacity");Eq(16UL,c.LocalName.Length.Read(0),"Assigned after growth");Eq((byte)0,c.LocalName.ReadByte(16,0),"Terminator");Check(c.GrowthReturned&&c.OwnHeapAllocated&&c.OwnHeapNormalRelease&&c.ResourcePublished,"Ordered normal growth and publication");Eq(2,f.Manager.Cache40.Read(0).Count,"Two different names");Same(c.Load.Construction.Result,entry.Resource10.Read(0),"Same resource object");
 }
 public static int Main(){try{Run();Console.WriteLine("{\"status\":\"passed-animation-caller-binding-components-only\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"native_decoding\":false,\"native_playback\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"gameplay_accepted\":false}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 public static void Run(){
  scenarios=assertions=0;
  Test("Existing Player getter to set Add and actual cold manager preserves identities",delegate{
   var f=new Fixture();var entry=Entry();GameAnimationSetFields.ForSet(f.Set).Entry(5).Write(entry);var c=Call();
   Eq((byte)1,GameAnimationResourceBinding.Add024D80(f.Context,f.Current(),5,S(65,92,66),F(1.25f),F(-3.5f),c),"Add returned1");
   Same(f.Set,c.Set,"Same existing getter set");Same(entry,c.Entry,"Same table entry");Same(f.Graphics,c.CapturedGraphics.Read(0),"Existing Engine+1F8");Same(f.Manager,c.CapturedManager.Read(0),"Same Graphics+20 manager");
   var resource=entry.Resource10.Read(0);Same(c.Load.Construction.Result,resource,"Actual factory result directly published");Same(f.Manager,resource.Manager38.Read(0),"Original manager identity in resource");Same(f.Graphics,resource.GraphicsA8.Read(0),"Resource captures same graphics");Same(f.Loader,resource.Loader28.Read(0),"Late manager loader preserved");
   Same(c.Load.AllocatedPair,f.Manager.ResourceVector80.Read(0)[0],"One shared reference record");Same(resource,c.Load.AllocatedPair.Resource08.Read(0),"Cache and entry refer to same resource");Eq(1.25f,entry.Float1C.Read(0),"XMM2");Eq(-3.5f,entry.Float20.Read(0),"XMM3");
   Eq(4u,resource.Dword74.Read(0),"Resource remains constructed/unloaded");Unknown(entry.Float18,"No invented preload channel");Unknown(entry.Dword24,"No Play flag invented");Unknown(entry.Byte28,"No loop byte invented");Unknown(f.Set.Kind08,"Add does not invent current animation");Unknown(GameAnimationSetFields.ForSet(f.Set).Pending0C,"Add does not set pending");
   Check(c.Returned&&c.NameCopied&&c.LocalNameNormalRelease&&c.ResourcePublished&&c.ScalePublished&&c.OtherFloatPublished,"Normal ordered publication");
  });
  Test("Second entry warm lookup shares resource and reference count",delegate{
   var f=new Fixture();var first=Entry();var cold=Call();f.Add(first,S(65,92,66),cold);var resource=first.Resource10.Read(0);f.Manager.Loader38.Write(new object());var second=Entry();var warm=Call();f.Add(second,S(97,47,98),warm);
   Same(resource,second.Resource10.Read(0),"Same object without sync");Same(cold.Load.AllocatedPair,warm.Load.FirstPair,"Same reference record");Eq(2u,cold.Load.AllocatedPair.Count00.Read(0),"Shared count2");Eq(1,f.Manager.Cache40.Read(0).Count,"Single canonical key");Check(warm.Load.Construction==null&&!warm.Load.Cold,"No second constructor");Same(f.Loader,resource.Loader28.Read(0),"Later loader change is not retroactive");
  });
  Test("Existing resource returns1 without reading capacity global or changing floats",delegate{
   var f=new Fixture();var e=Entry();var resource=new EngineGraphicsResourceState();e.Resource10.Write(resource);e.Float1C.Write(7);e.Float20.Write(9);var s=new EngineResourceStringState();s.Length.Write(5);f.Global.Forget();var c=Call();
   Eq((byte)1,f.Add(e,Field<EngineResourceStringState>.Of(s),c),"Early present");Same(resource,e.Resource10.Read(0),"Pointer unchanged");Eq(7f,e.Float1C.Read(0),"Scale unchanged");Eq(9f,e.Float20.Read(0),"Other unchanged");Check(c.AlreadyPresent&&!c.NameCopied&&c.Load==null&&!c.GraphicsReturned,"No hidden load");
  });
  Test("Length zero returns1 before dereferencing even null entry",delegate{
   var f=new Fixture();f.Global.Forget();var s=new EngineResourceStringState();s.Length.Write(0);var c=Call();Eq((byte)1,f.Add(null,Field<EngineResourceStringState>.Of(s),c),"Empty source bypass");Check(c.EmptySource&&c.Returned&&!c.NameCopied,"Source capacity not read");
  });
  Test("Input and entry Unknown stop at actual first consumption",delegate{
   var f=new Fixture();Missing(0x020935,delegate{f.Add(Entry(),new Field<EngineResourceStringState>(),Call());},"Unknown source");Missing(0x020935,delegate{f.Add(Entry(),Field<EngineResourceStringState>.Of(null),Call());},"Null source");
   var unknown=new GameAnimationEntryState();Missing(0x020940,delegate{f.Add(unknown,S(65),Call());},"Unknown resource comparison");Missing(0x020940,delegate{f.Add(null,S(65),Call());},"Null entry after nonempty source");
  });
  Test("Known null manager is normal0 after temporary cleanup",delegate{
   var f=new Fixture();EngineGraphicsAnimationFields.ForGraphics(f.Graphics).Manager20.Write(null);var e=Entry();var c=Call();Eq((byte)0,f.Add(e,S(65),c),"Null manager result");Check(c.ManagerReturned&&c.LocalNameNormalRelease&&c.Returned&&c.Load==null&&!c.ResourcePublished,"Clean normal false");Same(null,e.Resource10.Read(0),"Resource remains null");Unknown(e.Float1C,"No float write on false");
  });
  Test("Null or Unknown Graphics and Unknown manager do not become normal false",delegate{
   var f=new Fixture();var fields=EngineGraphicsResourceEngineFields.ForEngine(f.Engine);fields.Graphics1F8.Write(null);var c=Call();Missing(0x091a20,delegate{f.Add(Entry(),S(65),c);},"Null Graphics dereference");Check(c.GraphicsReturned&&c.NameCopied&&!c.LocalNameNormalRelease&&!c.Returned,"Pre-call prefix retained");
   fields.Graphics1F8.Forget();var u=Call();Missing(0x091a20,delegate{f.Add(Entry(),S(65),u);},"Opaque getter returns Unknown until dereference");Check(u.GraphicsReturned,"Unknown MOV return was permitted");
   fields.Graphics1F8.Write(f.Graphics);EngineGraphicsAnimationFields.ForGraphics(f.Graphics).Manager20.Forget();Missing(0x091a24,delegate{f.Add(Entry(),S(65),Call());},"Unknown manager test");
   foreach(bool knownNull in new[]{false,true}){if(knownNull)f.Global.Write(null);else f.Global.Forget();var global=Call();var entry=Entry();Missing(0x077820,delegate{f.Add(entry,S(65),global);},"Global pointer consumed by Engine getter");Check(global.NameCopied&&!global.GraphicsReturned&&!global.LocalNameNormalRelease&&!global.ResourcePublished,"Copy before missing Engine dereference");Same(null,entry.Resource10.Read(0),"Entry untouched");}
  });
  Test("Mutable Engine Graphics and manager fields are reread between calls",delegate{
   var f=new Fixture();var first=Entry();f.Add(first,S(65),Call());var graphics2=new object();var loader2=new object();var secondManager=EngineAnimationResourceManagerState.CreateOwnInitialized(Field<object>.Of(loader2),f.Global,new EngineAnimationResourceCacheNode[0],new EngineAnimationResourcePair[0]);
   EngineGraphicsAnimationFields.ForGraphics(graphics2).Manager20.Write(secondManager);EngineGraphicsResourceEngineFields.ForEngine(f.Engine).Graphics1F8.Write(graphics2);var second=Entry();var c=Call();f.Add(second,S(65),c);
   Same(secondManager,c.CapturedManager.Read(0),"Fresh published manager");Same(graphics2,second.Resource10.Read(0).GraphicsA8.Read(0),"Fresh graphics identity");Check(!Object.ReferenceEquals(first.Resource10.Read(0),second.Resource10.Read(0)),"Different manager owns distinct cache");Eq(1,f.Manager.Cache40.Read(0).Count,"Old cache preserved");
   EngineGraphicsAnimationFields.ForGraphics(graphics2).Manager20.Write(null);Eq((byte)0,f.Add(Entry(),S(66),Call()),"Same Graphics new manager field is read");
  });
  Test("Manager must borrow same global cell even when current value happens equal",delegate{
   var f=new Fixture();var m=EngineAnimationResourceManagerState.CreateOwnInitialized(Field<object>.Of(f.Loader),Field<WorldEngine>.Of(f.Engine),new EngineAnimationResourceCacheNode[0],new EngineAnimationResourcePair[0]);EngineGraphicsAnimationFields.ForGraphics(f.Graphics).Manager20.Write(m);var c=Call();Missing(0x091a2c,delegate{f.Add(Entry(),S(65),c);},"No copied global cell");Check(c.Load==null&&!c.LocalNameNormalRelease,"No manager call");
  });
  Test("Unbound manager virtual slot rejected even on a cached key",delegate{
   var pair=Pair(Field<EngineGraphicsResourceState>.Of(new EngineGraphicsResourceState()));var f=new Fixture(new[]{Node(new byte[]{97},pair)});f.Manager.Virtual00.Write(0);var c=Call();Missing(0x091a2c,delegate{f.Add(Entry(),S(65),c);},"Actual virtual dispatch");Eq(1u,pair.Count00.Read(0),"Warm cache untouched");Check(c.Load==null,"No bypass through known manager implementation");f.Manager.Virtual00.Forget();Missing(0x091a2c,delegate{f.Add(Entry(),S(65),Call());},"Unknown vptr MOV is consumed at indirect jump");
  });
  Test("Unknown returned pointer stops after warm ref increment and local release",delegate{
   var pair=Pair(new Field<EngineGraphicsResourceState>());var f=new Fixture(new[]{Node(new byte[]{97},pair)});var e=Entry();var c=Call();Missing(0x0209c8,delegate{f.Add(e,S(65),c);},"Unknown pointer TEST");Eq(2u,pair.Count00.Read(0),"Warm ref prefix retained");Check(c.ManagerReturned&&c.Load.Returned&&c.LocalNameNormalRelease&&!c.Returned&&!c.ResourcePublished,"Native ordered prefix");Same(null,e.Resource10.Read(0),"No entry publication");
  });
  Test("Known null warm resource returns0 while preserving ref increment",delegate{
   var pair=Pair(Field<EngineGraphicsResourceState>.Of(null));var f=new Fixture(new[]{Node(new byte[]{97},pair)});var c=Call();Eq((byte)0,f.Add(Entry(),S(65),c),"Normal null warm return");Eq(2u,pair.Count00.Read(0),"Count increment was not rolled back");Check(c.LocalNameNormalRelease&&c.Returned,"Normal false is distinct from Unknown");
  });
  Test("Game truncates embedded NUL before byte manager canonicalization",delegate{
   var f=new Fixture();var e=Entry();var c=Call();f.Add(e,S(65,92,66,0,88),c);Eq(3,c.ScannedLength,"First NUL only");Eq(3,c.Load.ProcessedName.Length,"Engine receives truncated name");Eq((byte)97,c.Load.ProcessedName.Byte(0),"Lowercase byte");Eq((byte)47,c.Load.ProcessedName.Byte(1),"Slash byte");Eq((byte)98,c.Load.ProcessedName.Byte(2),"Suffix before NUL");
  });
  Test("Nonzero source length beginning with NUL still calls manager for empty name",delegate{
   var f=new Fixture();var context=new GameAnimationResourceBindingContext(f.Global,EngineAnimationNameLocale.Unknown);var c=Call();var e=Entry();Eq((byte)1,GameAnimationResourceBinding.Add020910(context,e,S(0,88),F(2),F(3),c),"Empty key constructed");Check(!c.EmptySource&&c.Load.Cold,"Not source-length early return");Eq(0,c.Load.ProcessedName.Length,"No tolower call needed");Eq(0UL,c.LocalName.Length.Read(0),"Empty temporary remains empty");
  });
  Test("Fifteen-byte inline and sixteen-byte fresh Game growth",NameBoundaryRegression);
  Test("Heap source may hold short C prefix without requiring temporary growth",delegate{
   var data=new byte[20];for(int n=0;n<data.Length;n++)data[n]=88;data[0]=65;data[1]=0;var f=new Fixture();var c=Call();f.Add(Entry(),S(data),c);Eq(1,c.ScannedLength,"Heap prefix");Eq(1,c.Load.ProcessedName.Length,"Capacity does not determine C length");Eq(15UL,c.LocalName.Capacity.Read(0),"Destination still inline");
  });
  Test("Unterminated and Unknown source bytes preserve temporary initialization",delegate{
   var f=new Fixture();var s=new EngineResourceStringState();s.Length.Write(1);s.Capacity.Write(15);for(int n=0;n<16;n++)s.Inline.Byte(n).Write(65);var c=Call();Missing(0x020977,delegate{f.Add(Entry(),Field<EngineResourceStringState>.Of(s),c);},"Unterminated region");Check(!c.NameCopied&&c.Load==null,"No speculative manager call");Eq(0UL,c.LocalName.Length.Read(0),"Initialized local only");
   s.Inline.Byte(0).Forget();Missing(0x02096b,delegate{f.Add(Entry(),Field<EngineResourceStringState>.Of(s),Call());},"Unknown byte is not NUL");
   s.Capacity.Write(16);foreach(bool knownNull in new[]{false,true}){if(knownNull)s.Heap.Write(null);else s.Heap.Forget();var heap=Call();Missing(0x02096b,delegate{f.Add(Entry(),Field<EngineResourceStringState>.Of(s),heap);},"Heap pointer is only consumed at byte dereference");Eq(15UL,heap.LocalName.Capacity.Read(0),"Heap failure follows capacity prefix");Eq(0UL,heap.LocalName.Length.Read(0),"Heap failure follows length prefix");Eq((byte)0,heap.LocalName.Inline.Byte(0).Read(0),"Heap failure follows first byte prefix");Check(!heap.NameCopied&&heap.Load==null,"No manager call after pointer failure");}
  });
  Test("Float copies remain opaque while resource publication succeeds",delegate{
   var f=new Fixture();var e=Entry();var c=Call();Eq((byte)1,GameAnimationResourceBinding.Add020910(f.Context,e,S(65),new Field<float>(),new Field<float>(),c),"Unknown MOVSS values may be copied");Unknown(e.Float1C,"Unknown scale preserved");Unknown(e.Float20,"Unknown second float preserved");Check(c.ResourcePublished&&c.ScalePublished&&c.OtherFloatPublished,"Writes recorded without inventing floats");
  });
  Test("Unsigned set ID selects0 on overflow and51 at valid upper bound",delegate{
   var f=new Fixture();var zero=Entry();var last=Entry();GameAnimationSetFields.ForSet(f.Set).Entry(0).Write(zero);GameAnimationSetFields.ForSet(f.Set).Entry(51).Write(last);
   foreach(uint id in new uint[]{52,0x80000000u,uint.MaxValue}){var c=Call();GameAnimationResourceBinding.Add024D80(f.Context,f.Current(),id,S(65),F(1),F(2),c);Eq(0,c.SelectedIndex,"Unsigned fallback");Same(zero,c.Entry,"Slot0");}
   var end=Call();GameAnimationResourceBinding.Add024D80(f.Context,f.Current(),51,S(66),F(1),F(2),end);Eq(51,end.SelectedIndex,"Last supported slot");Same(last,end.Entry,"Slot51");
  });
  Test("Set virtual dispatch precedes source early return and preserves Unknown",delegate{
   var f=new Fixture();var entry=Entry();GameAnimationSetFields.ForSet(f.Set).Entry(5).Write(entry);entry.Virtual00.Write(0);Missing(0x024daf,delegate{GameAnimationResourceBinding.Add024D80(f.Context,f.Current(),5,S(),F(1),F(2),Call());},"Wrong entry type not accepted for empty name");
   entry.Virtual00.Forget();Missing(0x024daf,delegate{GameAnimationResourceBinding.Add024D80(f.Context,f.Current(),5,S(),F(1),F(2),Call());},"Unknown entry table consumed by indirect jump");Missing(0x024d8e,delegate{GameAnimationResourceBinding.Add024D80(f.Context,Field<AnimationSet>.Of(null),5,S(),F(1),F(2),Call());},"Null set");
  });
  Test("Ninth key keeps published cache prefix and never publishes entry success",delegate{
   var f=new Fixture();for(byte n=0;n<8;n++)f.Add(Entry(),S((byte)(65+n)),Call());var entry=Entry();var c=Call();Missing(0x02eae9,delegate{f.Add(entry,S(73),c);},"Ninth native growth boundary");Eq(9,f.Manager.Cache40.Read(0).Count,"Ninth node already visible");Eq(8,f.Manager.ResourceVector80.Read(0).Count,"Vector append not reached");Check(f.Manager.RequiresResolution&&c.Load.MapPublished&&!c.Load.Returned&&!c.LocalNameNormalRelease&&!c.ResourcePublished&&!c.Returned,"Propagated partial state");Same(null,entry.Resource10.Read(0),"Original null retained");Unknown(entry.Float1C,"No later float write");Check(c.Load.OwnSafetyUnlock&&!f.Manager.SynchronizationC0.Read(0).IsHeldByCurrentThread,"OWN lock safely released, not native success");
   var retry=Call();Missing(0x03ac50,delegate{f.Add(entry,S(73),retry);},"Unresolved manager blocks reuse");Eq(9,f.Manager.Cache40.Read(0).Count,"No duplicate retry publication");
  });
  Test("Fresh Player getter selection shares manager across different sets",delegate{
   var f=new Fixture();var first=Entry();GameAnimationSetFields.ForSet(f.Set).Entry(5).Write(first);GameAnimationResourceBinding.Add024D80(f.Context,f.Current(),5,S(65),F(1),F(2),Call());
   var overrideSet=new AnimationSet();var second=Entry();GameAnimationSetFields.ForSet(overrideSet).Entry(5).Write(second);GameCharacterAnimationFields.ForOwner(f.Owner).Set2600.Write(overrideSet);var c=Call();GameAnimationResourceBinding.Add024D80(f.Context,f.Current(),5,S(97),F(3),F(4),c);
   Same(overrideSet,c.Set,"Getter rereads changed override");Same(first.Resource10.Read(0),second.Resource10.Read(0),"Both sets share manager-owned resource");Eq(1f,first.Float1C.Read(0),"First entry scale separate");Eq(3f,second.Float1C.Read(0),"Second entry scale independent");Same(f.Owner,GameCharacterAnimationFields.ForOwner(f.Owner).Owner,"No copied Player owner");
  });
 }
}
