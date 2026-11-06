using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Depends on the inactive opaque-move-fields.patch APIs. These are explicit
// TEST boundary observations of 05F870 register consumption and failure order,
// not production navigation, animation playback, or a recovered playable game.
// Legacy Play results below are fixtures, never a native virtual+8 success claim.
public static class GameMoveAnimationFieldCaptureTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static void Calls(Probe probe,string expected){Eq(expected,String.Join(",",probe.Calls.ToArray()),"Observed boundary call order");}
 const string ThroughState="region,can,already,move-manager,animations:1,animation-speed,state";
 const string ThroughSecond=ThroughState+",set-state:5,animations:2";

 class Probe:MoveBoundaries {
  public readonly List<string> Calls=new List<string>();
  public readonly List<uint> WrittenStates=new List<uint>();
  public readonly AnimationSet First=new AnimationSet(),Second=new AnimationSet();
  readonly object region=new object();
  public uint StateValue=5,SpeedIndex;
  public float LegacySpeed=2.5f;
  public byte PlayResult=1;
  public int AnimationCalls,SpeedCalls,StateCalls,PlayCalls,LegacyAnimationCalls,LegacySpeedCalls;
  public Action OnState,OnPlay;
  public AnimationSet PlayedSet;public MoveCharacter PlayedRole;public object PlayedName;
  public uint PlayedAnimation,PlayedExtra;public float PlayedSpeed;public byte PlayedLoop;
  public MovementManager StoppedManager;
  public Probe(MoveContext context):base(context){First.Kind08.Write(7);Second.Kind08.Write(17);}
  public override object Region(WorldPosition position){Calls.Add("region");return region;}
  public override byte CanMove460(MoveCharacter role){Calls.Add("can");return 1;}
  public override byte AlreadyThere(MovementManager manager,WorldPosition position,float tolerance){Calls.Add("already");return 0;}
  public override byte MoveManager(MovementManager manager,WorldPosition position){Calls.Add("move-manager");return 1;}
  public override AnimationSet Animations(MoveCharacter role){AnimationCalls++;LegacyAnimationCalls++;Calls.Add("animations:"+AnimationCalls);return AnimationCalls==1?First:Second;}
  public override float AnimationSpeed(MoveCharacter role,uint index){SpeedCalls++;LegacySpeedCalls++;SpeedIndex=index;Calls.Add("animation-speed");return LegacySpeed;}
  public override uint State458(MoveCharacter role){StateCalls++;Calls.Add("state");if(OnState!=null)OnState();return StateValue;}
  public override void State450(MoveCharacter role,uint state){WrittenStates.Add(state);StateValue=state;Calls.Add("set-state:"+state);}
  public override byte Play(AnimationSet set,MoveCharacter role,uint animation,object name,float speed,byte loop,uint extra){
   PlayCalls++;PlayedSet=set;PlayedRole=role;PlayedAnimation=animation;PlayedName=name;PlayedSpeed=speed;PlayedLoop=loop;PlayedExtra=extra;Calls.Add("play");if(OnPlay!=null)OnPlay();return PlayResult;
  }
  public override void StopManager(MovementManager manager){StoppedManager=manager;Calls.Add("stop-manager");}
 }
 sealed class CapturedProbe:Probe {
  public readonly Field<AnimationSet> FirstPointer=new Field<AnimationSet>(),SecondPointer=new Field<AnimationSet>();
  public readonly Field<float> Speed=new Field<float>();
  public CapturedProbe(MoveContext context):base(context){FirstPointer.Write(First);SecondPointer.Write(Second);}
  public override Field<AnimationSet> AnimationsCaptured(MoveCharacter role){AnimationCalls++;Calls.Add("animations:"+AnimationCalls);return AnimationCalls==1?FirstPointer:SecondPointer;}
  public override Field<float> AnimationSpeedCaptured(MoveCharacter role,uint index){SpeedCalls++;SpeedIndex=index;Calls.Add("animation-speed");return Speed;}
 }
 sealed class Fixture {
  public readonly MoveContext Context=new MoveContext();
  public readonly MoveCharacter Role=new MoveCharacter();
  public readonly MovementManager Manager=new MovementManager();
  public readonly WorldPosition Destination=new WorldPosition();
  public readonly object Name=new object();
  public readonly Probe B;public readonly CapturedProbe Captured;
  public Invocation LastCall;
  public Fixture(bool captured){
   Context.AlreadyThereConstant7A7FD8.Write(0.025f);Context.NoNameImport609568.Write(Name);Role.MovementDC0.Write(Manager);
   if(captured){Captured=new CapturedProbe(Context);B=Captured;}else B=new Probe(Context);
  }
  public void Move(float speed){LastCall=new Invocation();MoveContracts.Move(Role,Destination,0,7,speed,B,LastCall);}
 }
 public static void Run(){
  scenarios=assertions=0;
  Test("First pointer is consumed before speed or state",delegate{
   var f=new Fixture(true);f.Captured.FirstPointer.Forget();Missing(0x05fa05,delegate{f.Move(2.5f);},"Unknown first getter result fails at Kind08 load");
   Eq(0,f.B.SpeedCalls,"No speed call before first pointer use");Eq(0,f.B.StateCalls,"No state call");Eq(0,f.B.PlayCalls,"No fixture playback");Calls(f.B,"region,can,already,move-manager,animations:1");
   var n=new Fixture(true);n.Captured.FirstPointer.Write(null);Missing(0x05fa05,delegate{n.Move(2.5f);},"Known-null first pointer fails at same dereference");Eq(0,n.B.SpeedCalls,"Null first pointer does not reach speed");
  });
  Test("Unknown speed waits for State458 and the actual comparison",delegate{
   var f=new Fixture(true);Missing(0x05fa2f,delegate{f.Move(2.5f);},"Matching state5 and captured kind consume Unknown at UCOMISS");
   Eq(1,f.B.SpeedCalls,"Speed getter executes");Eq(1,f.B.StateCalls,"State executes before consuming speed");Eq(0,f.B.WrittenStates.Count,"No state write before comparison failure");Eq(0,f.B.PlayCalls,"No playback after comparison failure");Calls(f.B,ThroughState);
   var failedState=new Fixture(true);failedState.B.OnState=delegate{throw new MissingContract(0x05b730,"Explicit test state dependency failure");};
   Missing(0x05b730,delegate{failedState.Move(2.5f);},"State dependency failure precedes pending Unknown-speed consumption");Eq(0,failedState.B.WrittenStates.Count,"State failure leaves prefix unchanged");Calls(failedState.B,ThroughState);
  });
  Test("Nonmoving state skips captured Unknown kind and reaches second getter then name",delegate{
   var f=new Fixture(true);f.B.First.Kind08.Forget();f.B.StateValue=4;f.Context.NoNameImport609568.Forget();
   Missing(0x05fa4d,delegate{f.Move(2.5f);},"State not5 bypasses kind and speed comparisons and reaches actual NoName read");Eq(1,f.B.SpeedCalls,"Speed getter ran despite Unknown kind");Eq(1,f.B.StateCalls,"State queried before any kind consumption");Eq(2,f.B.AnimationCalls,"Both getters returned");Eq(1,f.B.WrittenStates.Count,"Actual requested moving-state prefix reached");Eq(5u,f.B.WrittenStates[0],"Moving state requested before second getter");Eq(0,f.B.PlayCalls,"Unknown name prevents fixture playback");Unknown(f.B.First.Kind08,"Skipped kind not defaulted");Unknown(f.Captured.Speed,"Skipped speed not defaulted");Calls(f.B,ThroughSecond);
  });
  Test("Moving state consumes captured Unknown kind only at CMP after speed and state",delegate{
   var f=new Fixture(true);f.B.First.Kind08.Forget();f.B.OnState=delegate{f.B.First.Kind08.Write(7);};
   Missing(0x05fa2b,delegate{f.Move(2.5f);},"Captured kind remains Unknown even if state callback later writes matching kind");Eq(1,f.B.SpeedCalls,"Speed getter already ran before CMP");Eq(1,f.B.StateCalls,"State already returned5 before CMP");Eq(7u,f.B.First.Kind08.Read(0),"Source mutation occurred but cannot repair captured register");Unknown(f.Captured.Speed,"Unknown speed is not consumed before kind comparison");Eq(1,f.B.AnimationCalls,"Second getter not reached");Eq(0,f.B.WrittenStates.Count,"No moving-state setter before failed CMP");Eq(0,f.B.PlayCalls,"No playback after kind failure");Eq(0x05fa2bu,f.LastCall.At,"Exact kind consumer diagnostics");Calls(f.B,ThroughState);
  });
  Test("Nonmatching state or kind skips Unknown speed comparison",delegate{
   for(int branch=0;branch<2;branch++){
    var f=new Fixture(true);if(branch==0)f.B.StateValue=4;else f.B.First.Kind08.Write(8);f.Move(2.5f);
    Eq(1,f.B.SpeedCalls,"Getter still executes before branch");Eq(1,f.B.StateCalls,"State still queried");Eq(1,f.B.PlayCalls,"Explicit fixture Play result reached");Eq(1,f.B.WrittenStates.Count,"Only moving-state write");Eq(5u,f.B.WrittenStates[0],"Moving state requested");Same(f.B.Second,f.B.PlayedSet,"Second returned identity passed to legacy Play");Unknown(f.Captured.Speed,"Skipped comparison does not initialize Unknown speed");Calls(f.B,ThroughSecond+",play");
   }
  });
  Test("State callback cannot alter already captured speed or kind",delegate{
   var f=new Fixture(true);f.Captured.Speed.Write(2.5f);f.B.OnState=delegate{f.Captured.Speed.Write(99);f.B.First.Kind08.Write(999);};f.Move(2.5f);
   Eq(99f,f.Captured.Speed.Read(0),"Fixture mutation occurred");Eq(999u,f.B.First.Kind08.Read(0),"Later kind mutation occurred");Eq(0,f.B.PlayCalls,"Prior matching captures retain early return");Eq(0,f.B.WrittenStates.Count,"No replay state write");Eq(0x05fa2fu,f.LastCall.At,"Actual comparison was reached");Calls(f.B,ThroughState);
   var unknown=new Fixture(true);unknown.B.OnState=delegate{unknown.Captured.Speed.Write(2.5f);};Missing(0x05fa2f,delegate{unknown.Move(2.5f);},"Later known source cannot repair captured Unknown");Eq(2.5f,unknown.Captured.Speed.Read(0),"Source is now known but snapshot is not");Eq(0,unknown.B.WrittenStates.Count,"No state change after captured Unknown failure");
  });
  Test("Second pointer remains opaque through name load and null fails before legacy Play",delegate{
   var nameFirst=new Fixture(true);nameFirst.B.StateValue=1;nameFirst.Captured.SecondPointer.Forget();nameFirst.Context.NoNameImport609568.Forget();
   Missing(0x05fa4d,delegate{nameFirst.Move(2.5f);},"Unknown name precedes unknown second pointer use");Eq(5u,nameFirst.B.StateValue,"Moving-state prefix already happened");Eq(2,nameFirst.B.AnimationCalls,"Both getters returned");Eq(0,nameFirst.B.PlayCalls,"Legacy Play did not run");Calls(nameFirst.B,ThroughSecond);
   var pointer=new Fixture(true);pointer.B.StateValue=1;pointer.Captured.SecondPointer.Forget();Missing(0x024df0,delegate{pointer.Move(2.5f);},"Known name allows Unknown pointer to reach Play's first dereference");Eq(5u,pointer.B.StateValue,"Moving state retained after pointer failure");Eq(0,pointer.B.PlayCalls,"Unknown pointer cannot be accepted by legacy Play");Calls(pointer.B,ThroughSecond);
   var knownNull=new Fixture(true);knownNull.B.StateValue=1;knownNull.Captured.SecondPointer.Write(null);Missing(0x024df0,delegate{knownNull.Move(2.5f);},"Known-null pointer is not passed to a permissive legacy Play override");Eq(5u,knownNull.B.StateValue,"Null failure preserves moving-state prefix");Eq(0,knownNull.B.PlayCalls,"Legacy Play cannot manufacture null success");Calls(knownNull.B,ThroughSecond);
  });
  Test("Legacy concrete overrides retain equal and unordered early returns",delegate{
   float[] old={2.5f,Single.NaN,2.5f,Single.NaN};float[] requested={2.5f,2.5f,Single.NaN,Single.NaN};
   for(int n=0;n<old.Length;n++){
    var f=new Fixture(false);f.B.LegacySpeed=old[n];f.Move(requested[n]);Eq(1,f.B.LegacyAnimationCalls,"Default field wrapper invokes legacy getter once");Eq(1,f.B.LegacySpeedCalls,"Default speed wrapper invokes legacy override once");Eq(0u,f.B.SpeedIndex,"Original index0");Eq(0,f.B.PlayCalls,"Equal or unordered match avoids playback");Eq(0,f.B.WrittenStates.Count,"Early return does not set moving state");Calls(f.B,ThroughState);
   }
  });
  Test("Legacy Play failure stops fresh manager then restores state",delegate{
   var f=new Fixture(false);f.B.StateValue=1;f.B.PlayResult=0;var fresh=new MovementManager();f.B.OnPlay=delegate{f.Role.MovementDC0.Write(fresh);};f.Move(2.5f);
   Eq(2,f.B.LegacyAnimationCalls,"Both default wrappers invoke old getter");Eq(1,f.B.LegacySpeedCalls,"Old concrete speed override preserved");Eq(1,f.B.PlayCalls,"Exactly one explicit fixture Play call");Same(f.B.Second,f.B.PlayedSet,"Second original set reaches legacy Play");Same(f.Role,f.B.PlayedRole,"Original role identity");Same(f.Name,f.B.PlayedName,"Imported name identity");Eq(7u,f.B.PlayedAnimation,"Requested animation");Eq(2.5f,f.B.PlayedSpeed,"Requested speed");Eq((byte)1,f.B.PlayedLoop,"Loop byte");Eq(0u,f.B.PlayedExtra,"Trailing DWORD");Same(fresh,f.B.StoppedManager,"Manager reread after Play");Eq(2,f.B.WrittenStates.Count,"Two state transitions");Eq(5u,f.B.WrittenStates[0],"Enter moving before Play");Eq(1u,f.B.WrittenStates[1],"Restore idle after stop");Calls(f.B,ThroughSecond+",play,stop-manager,set-state:1");
  });
 }
}
