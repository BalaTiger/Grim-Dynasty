using System;
using GrimDynasty.ReverseClient.Actions;

public static partial class PlayerMoveBeginFrameIntegrationTests {
 public sealed class StoppedFrameResult {
  public int Frame,WorkCount;public uint State70,Flags,Generation,PrefixRva,SuffixRva,ControlBefore,ControlAfter,ProximityCount;
  public float[] BeforeNavigation,NavigationPosition,BeforePublished,PublishedPosition,PreviousPosition,Velocity;
  public uint[] GapBits;
  public bool PrefixAndSuffixReturned,InsertReturned,UpdateReturned,EarlyMovedReturned,NeighborQueryReturned,EmptyNeighbors,GapStored,WorkCleared;
 }
 // Re-executes the necessary23 moving frames on one fresh registered Player,
 // then three real State3 frames. It does not run the old diagnostic/suite.
 public static ConditionalOwnLuaArrivalResult RunConditionalOwnLuaStoppedFramesBounded(){return RunConditionalOwnLuaArrivalCore(true);}

 static void RunConditionalStoppedFrames(Fixture f,SystemUcrtRotationMath math,GameReachedGoalLuaService service,OwnLuaJit204State lua,ConditionalOwnLuaArrivalResult result){
  var m=f.Move.Movement;var r=f.Move.Request;var a=m.Agent;
  var stopped=new NavigationCrowdStoppedFrameBoundaries(f.Tick.Proximity,f.Constants,f.Tick.Global422980);stopped.UseObservedDiskConstant();
  Same(m.RegistrationState.Half34BADC,stopped.Proximity.Half34BADC,"Stopped prefix and neighbor helper borrow original registration Half cell");
  Same(m.ConstructorGlobal,stopped.Global422980,"Stopped correction borrows original constructor/Goal global");
  int beforeAssertions=assertions;
  for(int n=0;n<3;n++){
   int frame=result.GoalFrame+n;var suffix=new NavigationCrowdStoppedFrameInvocation();var record=new StoppedFrameResult{Frame=frame+1,BeforeNavigation=Values(a.Position48),BeforePublished=Values(r.Owner.Entity.CoordsD8.Position00)};
   result.PostStopAttemptedFrames++;Exception primaryFailure=null;
   try{
    // Same explicit OWN scheduler inputs as the existing moving Fixture. No
    // Agent/action/Work field is repaired or assigned by the test.
    r.EngineLifetime.FrameCount640.Write((uint)(100+frame));r.Owner.Entity.LastFrame178.Write((uint)(99+frame));
    Eq(0,WorkCount(m.Group),"Prior actual tail left Work empty before stopped frame");record.ControlBefore=math.CheckControl(0x25c440);Stage="stopped-frame-"+record.Frame+"-real-prefix-and-suffix";
    NavigationCrowdTickPrefixContracts.RunSingleStoppedState3NoWork25C440(m.Group,a,f.Domain,50,f.Boundary,f.Publication.Moved,f.Prefix,stopped,suffix);
    record.ControlAfter=math.CheckControl(0x25e2fb);Eq(record.ControlBefore,record.ControlAfter,"Read-only FP profile unchanged across stopped frame");
    Check(f.Prefix.StoppedState3Returned&&suffix.SuffixReturned&&suffix.WorkCleared,"Real prefix directly reaches and completes bounded State3 suffix");Eq(0x25e2fbu,f.Prefix.Native.At,"Prefix/suffix composition reaches real return");
    Check(!suffix.CompleteFrameReturned&&!f.Prefix.SteeringChoiceReturned&&!f.Prefix.Reached25CB73&&!f.Prefix.Reached25CF79,"No full Tick claim or synthetic steering edge");Same(null,f.Prefix.SynchronousReplan,"Stopped state did not submit another path request");
    Check(suffix.InsertReturned&&suffix.UpdateReturned&&suffix.NeighborQueryReturned&&suffix.EmptyNeighbors&&suffix.GapStored,"Real self insertion, Player Update and excludeSelf query completed");Eq(1u,m.Group.Count78.Read(0),"Proximity contains actual self; empty neighbors were not injected");
    Eq(0,WorkCount(m.Group),"Only actual terminal Work reset occurred");Eq(3u,a.State70.Read(0),"Original Stop State3 retained");Eq((byte)0,r.Manager.Moving38.Read(0),"Original Stop movement byte retained");Same(null,m.Composition.Handler.Current08.Read(0),"Original destroyed action stays absent");Eq(1u,a.RequestE8.Generation00.Read(0),"One original request generation across stopped frames");
    var position=Values(a.Position48);var published=Values(r.Owner.Entity.CoordsD8.Position00);var previous=Values(a.PreviousC4);var velocity=Values(a.VelocityB8);
    for(int axis=0;axis<3;axis++){
     Eq(Bits(record.BeforeNavigation[axis]),Bits(previous[axis]),"Real Previous copy precedes projection axis "+axis);
     Eq(Bits(record.BeforePublished[axis]),Bits(published[axis]),"Fixed saved-map stopped Entity remains at its actual published position axis "+axis);
     Eq(0u,Bits(velocity[axis]),"Original Goal velocity remains axis "+axis);
    }
    Eq(Bits(record.BeforeNavigation[0]),Bits(position[0]),"No horizontal navigation drift X");Eq(Bits(record.BeforeNavigation[2]),Bits(position[2]),"No horizontal navigation drift Z");
    Eq(0u,a.GapD0.Read(0),"Actual late shared-global Gap X store");Eq(0u,a.GapD4.Read(0),"Actual late shared-global Gap Y store");Eq(0u,a.GapD8.Read(0),"Actual late shared-global Gap Z store");
    Same(service,m.Composition.Production.ReachedGoal.Engine.CaptureEngine421428().Read(0).LuaService68.Read(0),"Same Engine service across stopped frames");Same(lua,service.Lua00.Read(0),"Same live OWN VM retained");lua.RequireLive(0x0ebb1c);Eq((byte)0,service.Initialized58.Read(0),"Stopped frames do not complete native service initialization");
    f.SameState();f.Released();result.PostStopCompletedFrames++;
   }catch(MissingContract error){primaryFailure=error;result.FirstUnsupportedRva=error.Address;throw;}
   catch(Exception error){primaryFailure=error;throw;}
   finally{
    try{
     record.NavigationPosition=Values(a.Position48);record.PublishedPosition=Values(r.Owner.Entity.CoordsD8.Position00);record.PreviousPosition=Values(a.PreviousC4);record.Velocity=Values(a.VelocityB8);record.GapBits=new[]{a.GapD0.Read(0),a.GapD4.Read(0),a.GapD8.Read(0)};
     record.State70=a.State70.Read(0);record.Flags=a.Flags44.Read(0);record.Generation=a.RequestE8.Generation00.Read(0);record.WorkCount=WorkCount(m.Group);record.ProximityCount=m.Group.Count78.Read(0);record.PrefixRva=f.Prefix.Native.At;record.SuffixRva=suffix.Native.At;
     record.PrefixAndSuffixReturned=f.Prefix.StoppedState3Returned&&suffix.SuffixReturned;record.InsertReturned=suffix.InsertReturned;record.UpdateReturned=suffix.UpdateReturned;record.EarlyMovedReturned=f.Prefix.EarlyMovedReturned;record.NeighborQueryReturned=suffix.NeighborQueryReturned;record.EmptyNeighbors=suffix.EmptyNeighbors;record.GapStored=suffix.GapStored;record.WorkCleared=suffix.WorkCleared;result.StoppedFrames.Add(record);
     result.PostStopPrefixReturned=record.PrefixAndSuffixReturned;result.PostStopPrefixRva=record.PrefixRva;result.PostStopEarlyMovedReturned=record.EarlyMovedReturned;result.PostStopUpdateReturned=record.UpdateReturned;result.PostStopState=record.State70;result.PostStopWorkCount=record.WorkCount;result.PostStopNavigationPosition=record.NavigationPosition;result.PostStopPublishedPosition=record.PublishedPosition;
    }catch(Exception error){result.SnapshotFailure=error.ToString();result.SnapshotFailureFrame=frame+1;if(primaryFailure==null)throw;}
   }
  }
  result.StoppedAssertions=assertions-beforeAssertions;result.PostStopFramesVerified=true;scenarios=1;
  // One targeted failure on this same stopped object. The captured unknown
  // half must survive preparation/projection and stop at its first ADDSS;
  // the unrelated avoidance Half must not silently satisfy it. End here.
  beforeAssertions=assertions;Stage="stopped-prefix-unknown-shared-half";result.PostStopAttemptedFrames++;stopped.Proximity.Half34BADC.Forget();var failed=new NavigationCrowdStoppedFrameInvocation();
  r.EngineLifetime.FrameCount640.Write((uint)(100+result.GoalFrame+3));r.Owner.Entity.LastFrame178.Write((uint)(99+result.GoalFrame+3));
  uint control=math.CheckControl(0x25c440);
  result.StoppedUnknownHalfRva=Missing(0x25c912,delegate{NavigationCrowdTickPrefixContracts.RunSingleStoppedState3NoWork25C440(m.Group,a,f.Domain,50,f.Boundary,f.Publication.Moved,f.Prefix,stopped,failed);});
  Eq(control,math.CheckControl(0x25c912),"Failed frame preserves supported FP profile");Check(failed.InsertReturned&&!failed.UpdateReturned&&!failed.GapStored&&!failed.WorkCleared&&!failed.SuffixReturned&&!f.Prefix.StoppedState3Returned,"Unknown half preserves actual insertion/failure prefix without a returned frame");
  Eq(0,WorkCount(m.Group),"No unknown-to-default Work repair");Eq(3u,a.State70.Read(0),"Unknown operand does not rewrite stopped state");Unknown(stopped.Proximity.Half34BADC,"Unknown shared half remains unknown");Eq(.5f,f.Constants.Half34BADC.Read(0),"Known legacy constant copy cannot bypass unknown canonical cell");
  f.SameState();f.Released();result.StoppedNegativeAssertions=assertions-beforeAssertions;
 }
}
