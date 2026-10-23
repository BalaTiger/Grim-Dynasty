using System;
using System.Threading;
using GrimDynasty.ReverseClient.Actions;

public static class WorldPublicationPrerequisitesTests {
 static int assertions,scenarios;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Equal<T>(T expected,T actual,string label){Check(Object.Equals(expected,actual),label);}
 static void Same(object expected,object actual,string label){Check(Object.ReferenceEquals(expected,actual),label);}
 static void Missing(Action action,uint at,string label){try{action();throw new Exception(label+" unexpectedly completed");}catch(MissingContract e){Equal(at,e.Address,label);}}
 static void Unknown<T>(Field<T> field,string label){Missing(delegate{field.Read(0xf001);},0xf001,label);}
 static void Invalid(Action action,string label){bool found=false;try{action();}catch(InvalidOperationException){found=true;}Check(found,label);}
 sealed class Fixture {
  public readonly WorldCoordinateState Coordinates=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldPublicationRegistry Registry;
  public readonly RegionPublicationView View;
  public readonly LevelMovementState Level;
  public readonly OwnReentrantRegionLock Sync=OwnReentrantRegionLock.InitializeOwn();
  public Fixture(){Registry=new WorldPublicationRegistry(Coordinates);View=Registry.BindRegionView(77,Region);Level=Registry.BindLevelView(new object());View.Level68.Write(Level);View.Reset88.Write(99);Region.Unavailable90.Write(0);Region.Critical18.Write(Sync);}
  public LevelMovementState Get(){return WorldPublicationPrerequisites.GuaranteedGetLevelFalse1943E0(View,new Invocation());}
 }
 static void ConstructorOnlyFirstQword(){
  var frame=new EntityCoordinates64();frame.Position00.Q0.Write(77);frame.Position00.DC.Write(0x3f800000);frame.Position00.D14.Write(0xfeed1234);frame.Basis18[1].Write(7);frame.Tail3C.Write(0x87654321);
  Same(frame,WorldPublicationPrerequisites.ConstructWorldCoords228AB0(frame,new Invocation()),"constructor returns same view");Equal(0UL,frame.Position00.Q0.Read(0),"only Q0 cleared");
  Unknown(frame.Position00.D8,"X stays unknown");Equal(0x3f800000u,frame.Position00.DC.Read(0),"Y bits unchanged");Unknown(frame.Position00.D10,"Z stays unknown");Equal(0xfeed1234u,frame.Position00.D14.Read(0),"position padding unchanged");
  Unknown(frame.Basis18[0],"basis0 not initialized");Equal(7f,frame.Basis18[1].Read(0),"known basis unchanged");Unknown(frame.Basis18[8],"last basis not initialized");Equal(0x87654321u,frame.Tail3C.Read(0),"tail unchanged");scenarios++;
 }
 static void ConstructorAliasAndLifetime(){
  var frame=new EntityCoordinates64();var alias=new EntityCoordinates64(frame);frame.Position00.Q0.Write(99);
  Same(alias,WorldPublicationPrerequisites.ConstructWorldCoords228AB0(alias,new Invocation()),"return original alias argument");Same(frame.AddressIdentity,alias.AddressIdentity,"same address");Equal(0UL,frame.Position00.Q0.Read(0),"alias write observed by original");
  frame.Position00.Q0.Write(55);frame.MarkReleased();Missing(delegate{WorldPublicationPrerequisites.ConstructWorldCoords228AB0(alias,new Invocation());},0x228ab0,"released own frame rejected");Equal(55UL,frame.Position00.Q0.Read(0),"no write on rejected own lifetime");
  Missing(delegate{WorldPublicationPrerequisites.ConstructWorldCoords228AB0(null,new Invocation());},0x228ab0,"null argument rejected");scenarios++;
 }
 static void AuthoritativeBindings(){
  var f=new Fixture();Same(f.View,f.Registry.BindRegionView(77,f.Region),"same region binding reused");Same(f.Region,f.Coordinates.Region(77,0),"original coordinate region retained");Same(f.Region,f.View.Region,"no copied region");Same(f.Sync,f.View.Region.Critical18.Read(0),"existing lock field reused");
  Invalid(delegate{f.Registry.BindRegionView(78,f.Region);},"same region cannot acquire second token");Invalid(delegate{f.Registry.BindRegionView(78,new WorldRegion(f.Region.Identity));},"same native identity cannot acquire second view");
  Same(f.Level,f.Registry.BindLevelView(f.Level.Identity),"unique Level view");Unknown(f.View.Parent30,"unwritten parent field");Unknown(f.Level.Region08,"Level region not guessed");Unknown(f.Level.Spatial10,"no default spatial index");Unknown(f.Level.Ready538C,"no default ready status");scenarios++;
 }
 static void NormalBorrowedReturn(){
  var f=new Fixture();Same(f.Level,f.Get(),"actual borrowed Level identity");Equal(0u,f.View.Reset88.Read(0),"88 reset");Equal((byte)0,f.Region.Unavailable90.Read(0),"90 untouched on preloaded path");Check(!f.Sync.IsHeldByCurrentThread,"normal call leaves its own acquisition");Equal(0,f.Sync.CurrentThreadDepth,"depth zero");
  Unknown(f.Level.Region08,"borrow does not read/write Level fields");Unknown(f.Level.Ready538C,"false LoadLevel arm ignores render/ready state");
  var replacement=f.Registry.BindLevelView(new object());f.View.Level68.Write(replacement);Same(replacement,f.Get(),"next invocation rereads current Level68");Same(replacement,f.View.Level68.Read(0),"no mirror/writeback");scenarios++;
 }
 static void RealReentrantPlatformLock(){
  var f=new Fixture();f.Sync.Enter(0x19440c);
  try{Check(f.Sync.IsHeldByCurrentThread,"actual Monitor held");Equal(1,f.Sync.CurrentThreadDepth,"outer acquisition");Same(f.Level,f.Get(),"nested native-contract path returns borrowed level");Equal(1,f.Sync.CurrentThreadDepth,"nested Enter/Leave preserves caller depth");Same(f.Level,f.Get(),"second nested invocation");Equal(1,f.Sync.CurrentThreadDepth,"no recursion leak");}
  finally{f.Sync.Leave(0x194420);}
  Check(!f.Sync.IsHeldByCurrentThread,"outer caller can leave");Equal(0,f.Sync.CurrentThreadDepth,"all own acquisitions released");scenarios++;
 }
 static void UnavailableBranchDoesNotReadLock(){
  var f=new Fixture();f.Region.Unavailable90.Write(1);f.Region.Critical18.Forget();Same(null,f.Get(),"unavailable returns known null after preloaded call");Equal(0u,f.View.Reset88.Read(0),"88 still reset before branch");Same(f.Level,f.View.Level68.Read(0),"unavailable does not clear Level pointer");Unknown(f.Region.Critical18,"lock remains unknown and unused");scenarios++;
 }
 static void UnknownAndColdPrefixes(){
  var f=new Fixture();f.View.Level68.Forget();Missing(delegate{f.Get();},0x19445d,"unknown initial Level");Equal(99u,f.View.Reset88.Read(0),"initial unknown stops before caller reset");
  f=new Fixture();f.View.Level68.Write(null);Missing(delegate{f.Get();},0x194481,"known cold domain stops before cold effects");Equal(99u,f.View.Reset88.Read(0),"no fabricated LoadLevel return");Equal((byte)0,f.Region.Unavailable90.Read(0),"cold write90=1 was not performed");Check(!f.Sync.IsHeldByCurrentThread,"cold path did not enter lock");
  f=new Fixture();f.Region.Unavailable90.Forget();Missing(delegate{f.Get();},0x194404,"captured unknown90 consumed after reset88");Equal(0u,f.View.Reset88.Read(0),"failure preserves true reset prefix");Check(!f.Sync.IsHeldByCurrentThread,"unknown flag stops before enter");
  f=new Fixture();f.Region.Critical18.Forget();Missing(delegate{f.Get();},0x19440c,"unknown lock identity");Equal(0u,f.View.Reset88.Read(0),"lock failure preserves reset");
  f.Region.Critical18.Write(new object());Missing(delegate{f.Get();},0x19440c,"uninitialized opaque object is not a lock");f.Region.Critical18.Write(null);Missing(delegate{f.Get();},0x19440c,"null lock is not success");scenarios++;
 }
 static void ForeignCompositionRejected(){
  var f=new Fixture();var other=new WorldPublicationRegistry(new WorldCoordinateState());var foreign=other.BindLevelView(f.Level.Identity);f.View.Level68.Write(foreign);
  Missing(delegate{f.Get();},0x19445d,"same nominal identity from a different own composition is not adopted");Equal(99u,f.View.Reset88.Read(0),"rejection before native-contract caller side effects");Check(!f.Sync.IsHeldByCurrentThread,"foreign rejection did not enter lock");scenarios++;
 }
 static void WrongThreadLeaveRejected(){
  var sync=OwnReentrantRegionLock.InitializeOwn();Missing(delegate{sync.Leave(0x194420);},0x194420,"unowned leave not successful");Exception workerError=null;sync.Enter(0x19440c);
  try{
   var worker=new Thread(delegate(){try{sync.Leave(0x194420);}catch(Exception e){workerError=e;}});worker.IsBackground=true;worker.Start();Check(worker.Join(5000),"one bounded ownership check completes");
   var error=workerError as MissingContract;Check(error!=null&&error.Address==0x194420,"other actual thread cannot leave owner's Monitor");Check(sync.IsHeldByCurrentThread,"owner still holds its lock");Equal(1,sync.CurrentThreadDepth,"wrong-thread attempt does not decrement owner depth");
  }finally{sync.Leave(0x194420);}
  Equal(0,sync.CurrentThreadDepth,"owner cleanly leaves");scenarios++;
 }
 public static int Main(){try{
  ConstructorOnlyFirstQword();ConstructorAliasAndLifetime();AuthoritativeBindings();NormalBorrowedReturn();RealReentrantPlatformLock();UnavailableBranchDoesNotReadLock();UnknownAndColdPrefixes();ForeignCompositionRejected();WrongThreadLeaveRejected();
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"own_monitor_adapter\":true,\"native_critical_section\":false,\"cold_load\":false,\"position_publication\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
