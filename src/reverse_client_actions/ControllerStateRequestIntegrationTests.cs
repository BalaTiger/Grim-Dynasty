using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// One new saved-map integration scenario. The accepted route starts at the
// explicitly declared OWN navigation-center height used by the prior receipt;
// the distinct real Ground-height attempt must still fail4. Neither is a
// physical mouse input, native spawn, successful movement action or game.
public static class ControllerStateRequestIntegrationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static uint NavResult,GroundResult,CorridorCount,StopRva;
 public static bool StatePublished,DirectionWritten;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);StopRva=e.Address;return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){try{field.Read(0);}catch(MissingContract){assertions++;return;}throw new Exception(why);}
 static uint Bits(float value){return NavigationCoreContracts.ToBits(value);}
 static void Name(PlayerMoveShortName name,string text){name.Capacity18.Write(15);name.Length10.Write((ulong)text.Length);for(int n=0;n<text.Length;n++)name.Inline00[n].Write((byte)text[n]);name.Inline00[text.Length].Write(0);}
 sealed class Composition {
  public readonly PlayerMoveRequestTests.Fixture Request;
  public readonly ControllerStateTransitionState State;
  public readonly ControllerStateTransitionContext Context;
  public readonly PlayerMouseState MoveTo=new PlayerMouseState(new object());
  public readonly ControllerPlayerStateFields MoveFields;
  public Composition(NavigationSearchWrapperTests.Scene resources){
   Request=new PlayerMoveRequestTests.Fixture(resources);var f=Request;
   f.RequestRole.Special45B4.Write(0);f.RequestRole.MaximumRatio4BF0.Write(0);f.SeedOwnMainPlayer(0x1234);f.RequestEngine.Override37748.Write(0);
   f.Position.Q0.Write(77);f.Position.D8.Write(Bits(64.25f));f.Position.DC.Write(Bits(29.5f));f.Position.D10.Write(Bits(64.75f));
   Eq((byte)1,WorldFloorContracts.PutOnFloor22C620(f.Position,f.World,resources.Floor,new Invocation()),"Actual Ground destination");
   State=ControllerStateTransitionState.Bind(f.Mouse,f.MouseController);Context=new ControllerStateTransitionContext(f.Request);f.Request.BindStateTransition(Context);
   Same(f.Lookup,Context.Lookup,"Transition uses same lookup");Same(f.Request.One7A8054,Context.One7A8054,"Shared1.0 operand");Same(f.Request.Path.NullDifference338458,Context.NullDifference338458,"Shared null-difference operand");
   ControllerPlayerStateFields.Bind(f.State,new NavigationView());var idleTable=f.State.Virtual00.Read(0);ControllerPlayerLifecycleSlots.Bind(idleTable).SelectObserved(ObservedPlayerMouseTable.Idle);
   MoveFields=ControllerPlayerStateFields.Bind(MoveTo,new NavigationView());var moveTable=new PlayerMouseTable();moveTable.BindObserved(ObservedPlayerMouseTable.MoveTo);ControllerPlayerLifecycleSlots.Bind(moveTable).SelectObserved(ObservedPlayerMouseTable.MoveTo);MoveTo.Virtual00.Write(moveTable);MoveTo.Owner08.Write(f.Controller);MoveTo.CachedRole10.Write(null);
   var header=new ControllerStateRegistryNode();var node=new ControllerStateRegistryNode();header.Nil19.Write(1);header.Parent08.Write(node);node.Nil19.Write(0);node.Left00.Write(header);node.Right10.Write(header);Name(node.Name20,"MoveTo");node.State40.Write(MoveTo);State.Header290.Write(header);
   var overlay=new MouseStateList();var sentinel=new MouseStateNode();ControllerOverlaySentinel.BindOwnSameAddress(overlay,sentinel);overlay.First00.Write(sentinel);State.Overlay2E8.Write(overlay);
   State.Count2F0.Write(0);State.Changed2D0.Write(9);State.Data2A8.Qword00.Write(0);
  }
 }
 public static void Run(){
  scenarios=assertions=0;NavResult=GroundResult=UInt32.MaxValue;CorridorCount=StopRva=0;StatePublished=DirectionWritten=false;
  var resources=new NavigationSearchWrapperTests.Scene();var c=new Composition(resources);var f=c.Request;var oldAction=new ActionRecord();var oldPending=new ActionRecord();f.Controller.CurrentA0.Write(oldAction);f.Controller.PendingA8.Write(oldPending);
  Missing(0x069040,delegate{f.Outer(new Invocation());},"Actual request reaches production run-speed boundary through installed state");var call=f.Boundaries.LastRequest;
  Check(call.PathReturned&&call.Path.PrecheckReturned&&call.Path.Precheck.NavigationReturned,"Actual request/precheck returned");NavResult=call.Path.Result;CorridorCount=call.Path.Precheck.Navigation.CorridorCount.Read(0);Eq(0u,NavResult,"Accepted OWN nav-center start");Check(CorridorCount>1,"Saved route traverses real polygons");
  Check(call.TransitionEntered&&!call.TransitionReturned&&!call.Returned,"No artificial return across GetRunSpeed");Same(f.Controller,call.CapturedTransitionOwner.Read(0),"1636A8 captured owner is actual controller");Same(f.MouseController.State2A0,c.State.Current2A0,"Single current-state cell");Same(c.MoveTo,c.State.Current2A0.Read(0),"Registered MoveTo published to original mouse controller");
  var transition=call.Transition;var entry=transition.Entry;StatePublished=transition.StatePublished;DirectionWritten=entry.DirectionWritten;
  Check(transition.LookupReturned&&transition.ExitReturned&&transition.DataCopied&&transition.StatePublished&&!transition.EntryReturned&&!transition.Returned,"Actual lookup/copy/publication failure prefix");
  Check(entry.RegionReturned&&entry.CoordinatesReturned&&entry.DifferenceReturned&&entry.DirectionWritten&&entry.PreviousWritten&&entry.RunSpeedEntered&&!entry.Returned,"Actual166020 prefix executes without speed substitution");
  Eq(1,entry.LookupCalls,"MoveTo null role cache is filled by real128530 lookup once");Same(f.Move,c.MoveTo.CachedRole10.Read(0),"MoveTo cache reuses canonical role");Same(f.Owner,f.Bridge.ForMoveCharacter(c.MoveTo.CachedRole10.Read(0),0).Owner,"Same owner identity across request and entry");
  Eq(call.Payload.Zero00.Read(0),c.State.Data2A8.Qword00.Read(0),"Real first16 data copied");Eq(call.Payload.Zero08.Read(0),c.State.Data2A8.Dword08.Read(0),"Caller DWORD copied");Unknown(c.State.Data2A8.Padding0C,"Unwritten middle padding retained");
  Eq(call.PathOutput.Q0.Read(0),c.State.Data2A8.Position10.Q0.Read(0),"Region copied from actual path output");Eq(call.PathOutput.D8.Read(0),c.State.Data2A8.Position10.D8.Read(0),"X copied from actual path output");Eq(call.PathOutput.DC.Read(0),c.State.Data2A8.Position10.DC.Read(0),"Ground Y copied from actual path output");Eq(call.PathOutput.D10.Read(0),c.State.Data2A8.Position10.D10.Read(0),"Final8 Z copied");Unknown(c.State.Data2A8.Position10.D14,"Unwritten last padding retained");
  Eq(0f,c.MoveFields.Direction20.Y4.Read(0),"Entry zeros vertical direction");float x=c.MoveFields.Direction20.X0.Read(0),z=c.MoveFields.Direction20.Z8.Read(0);Check(!Single.IsNaN(x)&&!Single.IsNaN(z)&&Math.Abs(x*x+z*z-1f)<.00001,"Saved nonzero delta yields normalized horizontal direction");
  Eq(0ul,c.MoveFields.Previous30.Q0.Read(0),"Actual constructor cleared previous Region only");Eq(call.PathOutput.D8.Read(0),c.MoveFields.Previous30.D8.Read(0),"Previous local X preserves real target");Eq(call.PathOutput.DC.Read(0),c.MoveFields.Previous30.DC.Read(0),"Previous local Y preserves Ground target");Eq(call.PathOutput.D10.Read(0),c.MoveFields.Previous30.D10.Read(0),"Previous local Z preserves real target");Unknown(c.MoveFields.Previous30.D14,"Previous padding unknown");
  Eq((byte)0,c.State.Changed2D0.Read(0),"Final flag restore has not executed");Same(oldAction,f.Controller.CurrentA0.Read(0),"No fabricated current action");Same(oldPending,f.Controller.PendingA8.Read(0),"No fabricated pending action");Eq((byte)0,f.Manager.Moving38.Read(0),"No fabricated movement");Unknown(f.MouseController.Byte432,"Outer return store did not run");Check(!resources.Lock.IsHeldByCurrentThread&&resources.Lock.CurrentThreadDepth==0,"Accepted route released Ground lock");
  // Independent OWN hot composition, same saved resources; no fake recovery or
  // transition rollback is applied to the already partially changed instance.
  var g=new Composition(resources);var gf=g.Request;Eq((byte)1,WorldFloorContracts.PutOnFloor22C620(gf.Owner.Entity.CoordsD8.Position00,gf.World,resources.Floor,new Invocation()),"Ground supplies actual initial Y");uint groundBits=gf.Owner.Entity.CoordsD8.Position00.DC.Read(0);
  Missing(0x1636d7,delegate{gf.Outer(new Invocation());},"True Ground start still requires unclosed camera-retry path");var failed=gf.Boundaries.LastRequest;GroundResult=failed.Path.Result;Eq(4u,GroundResult,"Prior Ground nearest miss preserved");Check(!failed.TransitionEntered&&!failed.TransitionPayloadPrepared&&!failed.Returned,"Ground failure does not install a state");Unknown(failed.CapturedTransitionOwner,"No late owner capture before transition point");Same(gf.State,g.State.Current2A0.Read(0),"Original Idle retained before transition");Eq((byte)9,g.State.Changed2D0.Read(0),"No state flag clear before transition");Eq(groundBits,gf.Owner.Entity.CoordsD8.Position00.DC.Read(0),"No NAV height substituted");Check(!resources.Lock.IsHeldByCurrentThread&&resources.Lock.CurrentThreadDepth==0,"Rejected route released Ground lock");
  StopRva=0x069040;scenarios=1;
 }
}
public static class ControllerStateIntegratedValidationProgram {
 public static int Main(string[] args){try{
  if(args==null||args.Length!=1)throw new ArgumentException("Expected exactly new-components or request-regression");
  if(args[0]=="new-components"){
   ControllerStateTransitionTests.Run();ControllerStateRequestIntegrationTests.Run();Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed-new-controller-state-request-integration-only",component_scenarios=ControllerStateTransitionTests.Scenarios,integration_scenarios=ControllerStateRequestIntegrationTests.Scenarios,scenarios=ControllerStateTransitionTests.Scenarios+ControllerStateRequestIntegrationTests.Scenarios,assertions=ControllerStateTransitionTests.Assertions+ControllerStateRequestIntegrationTests.Assertions,nav_start_result=ControllerStateRequestIntegrationTests.NavResult,ground_start_result=ControllerStateRequestIntegrationTests.GroundResult,corridor_count=ControllerStateRequestIntegrationTests.CorridorCount,state_published=ControllerStateRequestIntegrationTests.StatePublished,direction_written=ControllerStateRequestIntegrationTests.DirectionWritten,stop_rva=ControllerStateRequestIntegrationTests.StopRva,old_test_suites_rerun=0,original_read=false,original_executed=false,backend_started=false,system_library_loaded=false,native_math_calls=0,physical_mouse=false,walking_accepted=false,action_submitted=false,request_continuation_complete=false}));
  }else if(args[0]=="request-regression"){
   PlayerMoveRequestTests.Run();Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed-affected-request-regression-only",scenarios=PlayerMoveRequestTests.Scenarios,assertions=PlayerMoveRequestTests.Assertions,nav_start_result=PlayerMoveRequestTests.SavedNavStartResult,ground_start_result=PlayerMoveRequestTests.SavedGroundStartResult,corridor_count=PlayerMoveRequestTests.SavedCorridorCount,transition_caller=PlayerMoveRequestTests.LastTransitionCaller,old_test_suites_rerun=1,original_read=false,original_executed=false,backend_started=false,system_library_loaded=false,native_math_calls=0,physical_mouse=false,walking_accepted=false,action_submitted=false}));
  }else throw new ArgumentException("Unsupported fixed mode");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
