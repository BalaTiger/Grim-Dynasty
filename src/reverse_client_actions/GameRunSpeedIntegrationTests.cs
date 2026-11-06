using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.CombatRuntime;
using GrimDynasty.ReverseClient.Actions;

// New integration only: the prior saved-map OWN navigation-center origin is
// explicit. No native spawn, physical mouse, action submission or walking.
public static class GameRunSpeedIntegrationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static uint StopRva,NavResult,CorridorCount;
 public static bool RunSpeedReturned;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static void Name(PlayerMoveShortName name,string text){name.Capacity18.Write(15);name.Length10.Write((ulong)text.Length);for(int n=0;n<text.Length;n++)name.Inline00[n].Write((byte)text[n]);name.Inline00[text.Length].Write(0);}
 sealed class Composition {
  public readonly PlayerMoveRequestTests.Fixture Request;
  public readonly ControllerStateTransitionContext Context;
  public readonly ControllerStateTransitionState State;
  public readonly PlayerMouseState MoveTo=new PlayerMouseState(new object());
  public readonly ControllerPlayerStateFields MoveFields;
  public readonly NativeAttributeView Attributes=new NativeAttributeView();
  public readonly GamePlayerRunSpeedCapState Cap;
  public readonly GameRunSpeedState SpeedState;
  public readonly GameRunSpeedContext SpeedContext;
  public readonly GameDurationDamageConstructorInvocation Constructor=new GameDurationDamageConstructorInvocation();
  public readonly ActionRecord OldAction=new ActionRecord(),OldPending=new ActionRecord();
  public Composition(NavigationSearchWrapperTests.Scene resources){
   Request=new PlayerMoveRequestTests.Fixture(resources);var f=Request;
   f.RequestRole.Special45B4.Write(0);f.RequestRole.MaximumRatio4BF0.Write(0);f.SeedOwnMainPlayer(0x1234);f.RequestEngine.Override37748.Write(0);
   f.Position.Q0.Write(77);f.Position.D8.Write(NativeAttributeGetter.FloatBits(64.25f));f.Position.DC.Write(NativeAttributeGetter.FloatBits(29.5f));f.Position.D10.Write(NativeAttributeGetter.FloatBits(64.75f));
   Eq((byte)1,WorldFloorContracts.PutOnFloor22C620(f.Position,f.World,resources.Floor,new Invocation()),"Real saved Ground destination");
   State=ControllerStateTransitionState.Bind(f.Mouse,f.MouseController);Context=new ControllerStateTransitionContext(f.Request);f.Request.BindStateTransition(Context);
   ControllerPlayerStateFields.Bind(f.State,new NavigationView());ControllerPlayerLifecycleSlots.Bind(f.State.Virtual00.Read(0)).SelectObserved(ObservedPlayerMouseTable.Idle);
   MoveFields=ControllerPlayerStateFields.Bind(MoveTo,new NavigationView());var moveTable=new PlayerMouseTable();moveTable.BindObserved(ObservedPlayerMouseTable.MoveTo);ControllerPlayerLifecycleSlots.Bind(moveTable).SelectObserved(ObservedPlayerMouseTable.MoveTo);MoveTo.Virtual00.Write(moveTable);MoveTo.Owner08.Write(f.Controller);MoveTo.CachedRole10.Write(null);
   var header=new ControllerStateRegistryNode();var node=new ControllerStateRegistryNode();header.Nil19.Write(1);header.Parent08.Write(node);node.Nil19.Write(0);node.Left00.Write(header);node.Right10.Write(header);Name(node.Name20,"MoveTo");node.State40.Write(MoveTo);State.Header290.Write(header);
   var overlay=new MouseStateList();var sentinel=new MouseStateNode();ControllerOverlaySentinel.BindOwnSameAddress(overlay,sentinel);overlay.First00.Write(sentinel);State.Overlay2E8.Write(overlay);State.Count2F0.Write(0);State.Changed2D0.Write(9);State.Data2A8.Qword00.Write(0);
   f.Controller.CurrentA0.Write(OldAction);f.Controller.PendingA8.Write(OldPending);
   // Explicit OWN hot class/field prerequisites, not native cold initialization.
   var character=f.Callbacks.CharacterClass.Read(0);character.Parent10.Write(null);f.Callbacks.PlayerClass.Read(0).Parent10.Write(character);var pet=new AddressSymbol<ClassRecord>(0xa5cb40);pet.Write(new ClassRecord());
   Cap=GamePlayerRunSpeedCapState.Bind(f.Bridge,f.Move,Attributes);SpeedState=GameRunSpeedState.Bind(Cap);SpeedContext=new GameRunSpeedContext(SpeedState,pet,NativeAttributeConstants.FromRecordedSnapshot(),true,true);
   Context.BindRunSpeed(SpeedContext);Attributes.SetObservedFloat(0x38,100);Attributes.SetObservedFloat(0x120,0);Attributes.SetObservedFloat(0x208,0);f.Move.SpeedFlag26B3.Write(0);Cap.Minimum5018.Write(0);
   Same(SpeedState.Duration,GameDurationDamageManagerContracts.Construct2790A0(SpeedState.Duration,f.Move,Constructor),"Actual constructor returns same subobject");
  }
  public ControllerMoveToEntryInvocation Entry{get{return Request.Boundaries.LastRequest.Transition.Entry;}}
  public void Prefix(){
   var f=Request;var call=f.Boundaries.LastRequest;var transition=call.Transition;var entry=transition.Entry;
   Check(call.PathReturned&&call.Path.Precheck.NavigationReturned&&call.TransitionEntered&&!call.TransitionReturned&&!call.Returned,"Real request/path reached entered but incomplete transition");Eq(0u,call.Path.Result,"OWN navigation-center start accepted");Check(call.Path.Precheck.Navigation.CorridorCount.Read(0)>1,"Actual saved corridor spans polygons");
   Check(transition.StatePublished&&transition.DataCopied&&!transition.EntryReturned&&!transition.Returned,"Original state/data publication survives later stop");Same(MoveTo,State.Current2A0.Read(0),"Same MoveTo installed");Same(f.MouseController.State2A0,State.Current2A0,"One live current-state cell");
   Check(entry.CoordinatesReturned&&entry.DifferenceReturned&&entry.DirectionWritten&&entry.PreviousWritten&&entry.RunSpeedEntered&&!entry.Returned,"Real166020 direction and previous-position prefix");Eq(0f,MoveFields.Direction20.Y4.Read(0),"Vertical direction zero");float x=MoveFields.Direction20.X0.Read(0),z=MoveFields.Direction20.Z8.Read(0);Check(!Single.IsNaN(x)&&!Single.IsNaN(z)&&Math.Abs(x*x+z*z-1f)<.00001,"Actual direction normalized");
   Eq(0ul,MoveFields.Previous30.Q0.Read(0),"WorldVec constructor clears previous Region");Eq(call.PathOutput.D8.Read(0),MoveFields.Previous30.D8.Read(0),"Previous X retains accepted target");Eq(call.PathOutput.DC.Read(0),MoveFields.Previous30.DC.Read(0),"Previous Y retains actual Ground");Eq(call.PathOutput.D10.Read(0),MoveFields.Previous30.D10.Read(0),"Previous Z retains target");
   Eq((byte)0,State.Changed2D0.Read(0),"Final flag restore not fabricated");Same(OldAction,f.Controller.CurrentA0.Read(0),"No action fabricated");Same(OldPending,f.Controller.PendingA8.Read(0),"No pending action fabricated");Eq((byte)0,f.Manager.Moving38.Read(0),"No movement fabricated");Same(f.Move,MoveTo.CachedRole10.Read(0),"Real128530 lookup preserves role identity");Same(Cap,SpeedState.Duration.Cap,"Duration and cap share original owner");Same(Attributes,entry.RunSpeedCall.Attribute.Fields,"Entry getter uses same mutable attributes");
  }
 }
 public static void Run(){scenarios=assertions=0;StopRva=NavResult=CorridorCount=0;RunSpeedReturned=false;var resources=new NavigationSearchWrapperTests.Scene();
  Test("Actual request and166020 execute ordinary speed then stop at real action producer",delegate{
   var c=new Composition(resources);var f=c.Request;ContextBindingChecks(c);
   Missing(0x124340,delegate{f.Outer(new Invocation());},"Only unimplemented action producer remains after actual speed return");c.Prefix();var entry=c.Entry;
   Check(entry.RunSpeedReturned&&entry.RunSpeedCall.Returned&&entry.RunSpeedCall.AttributeReturned&&entry.RunSpeedCall.DurationReturned&&entry.RunSpeedCall.Cap.Returned,"All ordinary speed producers really returned");Eq(100f,entry.ProducedRunSpeed.Read(0),"166020 receives calculated speed");Same(entry.RunSpeed,entry.RunSpeedCall.Native,"Existing diagnostic invocation retained");Eq(0x0692d8u,entry.RunSpeed.At,"Speed completed native return");Eq(0x124340u,entry.Native.At,"Next direct unimplemented producer");Check(c.Constructor.Returned,"Duration was explicitly constructed");Check(!resources.Lock.IsHeldByCurrentThread&&resources.Lock.CurrentThreadDepth==0,"Actual Ground lock released");
   StopRva=entry.Native.At;RunSpeedReturned=entry.RunSpeedReturned;NavResult=f.Boundaries.LastRequest.Path.Result;CorridorCount=f.Boundaries.LastRequest.Path.Precheck.Navigation.CorridorCount.Read(0);
  });
  Test("Shared attribute invalidation preserves installed state and direction failure prefix",delegate{
   var c=new Composition(resources);var f=c.Request;c.Attributes.Invalidate();Missing(0x0cf6ee,delegate{f.Outer(new Invocation());},"Actual getter detects invalid shared storage before Duration or cap");c.Prefix();var entry=c.Entry;
   Check(!entry.RunSpeedReturned&&!entry.RunSpeedCall.Returned&&entry.RunSpeedCall.AttributeEntered&&!entry.RunSpeedCall.DurationEntered&&!entry.RunSpeedCall.CapEntered,"No late producer or fabricated speed after getter failure");Missing(0,delegate{entry.ProducedRunSpeed.Read(0);},"No copied success from other composition");Check(!resources.Lock.IsHeldByCurrentThread&&resources.Lock.CurrentThreadDepth==0,"Failure preserves released Ground lock");
  });
 }
 static void ContextBindingChecks(Composition c){
  c.Context.BindRunSpeed(c.SpeedContext);Same(c.SpeedContext,c.Context.RunSpeed,"Same run-speed binding is idempotent");
  var other=new GameRunSpeedTests.Fixture();try{c.Context.BindRunSpeed(other.Context);throw new Exception("Expected cross-bridge rejection");}catch(ArgumentException){assertions++;}
  var replacement=new GameRunSpeedContext(c.SpeedState,c.SpeedContext.PetPlayerScalingClass,c.SpeedContext.Constants,true,true);try{c.Context.BindRunSpeed(replacement);throw new Exception("Expected context replacement rejection");}catch(InvalidOperationException){assertions++;}Same(c.SpeedContext,c.Context.RunSpeed,"Rejected replacements preserve original context");
 }
}
public static class GameRunSpeedValidationProgram {
 public static int Main(string[] args){try{
  if(args==null||args.Length!=1)throw new ArgumentException("Expected exactly new-components or state-regression");
  if(args[0]=="new-components"){
   GameDurationDamageManagerTests.Run();GameRunSpeedTests.Run();GameRunSpeedIntegrationTests.Run();Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed-new-run-speed-components-and-integration-only",duration_scenarios=GameDurationDamageManagerTests.Scenarios,run_speed_scenarios=GameRunSpeedTests.Scenarios,integration_scenarios=GameRunSpeedIntegrationTests.Scenarios,scenarios=GameDurationDamageManagerTests.Scenarios+GameRunSpeedTests.Scenarios+GameRunSpeedIntegrationTests.Scenarios,assertions=GameDurationDamageManagerTests.Assertions+GameRunSpeedTests.Assertions+GameRunSpeedIntegrationTests.Assertions,stop_rva=GameRunSpeedIntegrationTests.StopRva,run_speed_returned=GameRunSpeedIntegrationTests.RunSpeedReturned,nav_start_result=GameRunSpeedIntegrationTests.NavResult,corridor_count=GameRunSpeedIntegrationTests.CorridorCount,old_test_suites_rerun=0,original_read=false,original_executed=false,backend_started=false,system_library_loaded=false,native_math_calls=0,physical_mouse=false,walking_accepted=false,action_submitted=false}));
  }else if(args[0]=="state-regression"){
   ControllerStateTransitionTests.Run();ControllerStateRequestIntegrationTests.Run();Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed-affected-controller-state-regression-only",scenarios=ControllerStateTransitionTests.Scenarios+ControllerStateRequestIntegrationTests.Scenarios,assertions=ControllerStateTransitionTests.Assertions+ControllerStateRequestIntegrationTests.Assertions,stop_rva=ControllerStateRequestIntegrationTests.StopRva,old_test_suites_rerun=1,original_read=false,original_executed=false,backend_started=false,system_library_loaded=false,native_math_calls=0,physical_mouse=false,walking_accepted=false,action_submitted=false}));
  }else throw new ArgumentException("Unsupported fixed mode");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
