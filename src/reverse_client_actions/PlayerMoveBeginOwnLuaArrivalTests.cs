using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Conditional OWN bootstrap, not completion of native Engine0F2E00. A real
// pinned VM and the observed five-library helper establish this consumer's
// explicit input; the unimplemented native initialization continuation is
// neither invoked nor caught. All movement/Goal writes still come from the
// existing contracts. The next State3 prefix must retain its Unsupported.
public static partial class PlayerMoveBeginFrameIntegrationTests {
 public sealed class ConditionalOwnLuaArrivalResult {
  public string Outcome="not-run",Failure,Stage,CloseFailure,SnapshotFailure;
  public int SnapshotFailureFrame;
  public int Scenarios,Assertions,PriorFixtureSetupChecks,AttemptedFrames,CompletedFrames,GoalFrame,PostStopAttemptedFrames;
  public readonly int FrameLimit=30;
  public readonly bool NativeServiceInitializationCompleted=false,FullTick=false,GameplayAccepted=false;
  public bool PostStopFramesVerified;
  public int PostStopCompletedFrames,StoppedAssertions,StoppedNegativeAssertions;
  public uint StoppedUnknownHalfRva;
  public bool PassedDiagnostic,VmCreated,VmClosed,ModuleUnloadObserved,CoreReturned,GoalReturned,GoalTailReturned,HookInitialized;
  public uint FirstUnsupportedRva,CoreReturnRva,GoalRva,GoalTailRva,PostStopUnsupportedRva,PostStopPrefixRva,PostStopState;
  public byte? ServiceInitialized58;
  public long NewStateCalls,CloseCalls,CorePushCalls,CorePCallCalls,ActiveStates,NativeMathCalls;
  public int PostStopWorkCount;
  public bool PostStopPrefixReturned,PostStopEarlyMovedReturned,PostStopUpdateReturned;
  public float[] PostStopNavigationPosition,PostStopPublishedPosition;
  public readonly List<ArrivalFrameResult> Frames=new List<ArrivalFrameResult>();
  public readonly List<StoppedFrameResult> StoppedFrames=new List<StoppedFrameResult>();
 }
 public static ConditionalOwnLuaArrivalResult LastConditionalOwnLuaArrival;

 // One fresh Fixture, original request and actual registration; no old suite
 // entry point is called. A finite diagnostic bound never means arrival.
 public static ConditionalOwnLuaArrivalResult RunConditionalOwnLuaArrivalBounded(){return RunConditionalOwnLuaArrivalCore(false);}
 static ConditionalOwnLuaArrivalResult RunConditionalOwnLuaArrivalCore(bool validateStoppedFrames){
  ResetFrameResults();var result=new ConditionalOwnLuaArrivalResult();LastConditionalOwnLuaArrival=result;
  SystemUcrtRotationMath math=null;OwnLuaJit204Runtime runtime=null;OwnLuaJit204State lua=null;
  GameReachedGoalLuaService service=null;
  try{
   using(math=SystemUcrtRotationMath.OpenOwnProfile()){
    var f=new Fixture(math,true,true);var m=f.Move.Movement;var r=f.Move.Request;var a=m.Agent;
    var goal=m.Composition.Production.ReachedGoal;var active=m.Composition.Entry.MoveProductionCall.AllocatedAction;
    Check(m.ActivationCall.Returned&&m.ActivationCall.Registration.CreatedReturned&&m.ActivationCall.Registration.Joined,"Actual Add/Created/Join precedes this diagnostic");
    Same(a,m.ActivationCall.Registration.NewAgent,"Actual registered Agent");Same(m.Node,m.ActivationCall.Registration.NewNode,"Actual registered node");
    Same(r.Movement,goal.Move,"Goal uses original MoveContext");Same(r.ManagerEngine,goal.Engine,"Goal uses original ObjectManager context");Same(f.StopProduction,goal.StopProduction,"Original Stop production");
    Same(active,m.Composition.Handler.Current08.Read(0),"Keep actual constructed action until Goal destroys it");
    Same(r.Movement.EngineGlobal,goal.Engine.BorrowedEngineGlobal,"One mutable Engine global");
    var engine=goal.Engine.CaptureEngine421428().Read(0);Same(r.Engine,engine.WorldIdentity,"Original WorldEngine");Same(engine,GameReachedGoalLogEngine.ForWorldEngine(r.Engine),"Canonical Engine sidecar");
    Unknown(engine.LuaService68,"Fixture does not fabricate an existing service");Unknown(goal.Engine.Engine421428,"Legacy independent Engine cell remains unused");
    var hook=GameReachedGoalCharacterFields.ForOwner(r.Owner).HookPack45F0;
    var records=GameReachedGoalEngineContracts.ConstructOwnColdPack0EBDE0(hook,11,new Invocation());
    Same(records,hook.Records10.Read(0),"Original inline Hook owns the constructed records");Eq(11u,hook.Count18.Read(0),"Observed Character Hook count");Eq((byte)0,hook.Initialized1C.Read(0),"Constructor does not initialize Hook");
    for(uint n=0;n<11;n++){Eq(0UL,records.At(n,0).FunctionLength30.Read(0),"Observed empty function name "+n);Same(null,records.At(n,0).Lua40.Read(0),"Observed null Hook value "+n);}
    using(runtime=OwnLuaJit204Runtime.OpenOwnProfile()){
     Stage="conditional-own-service-bootstrap-before-frame0";
     var publish=new Invocation();service=GameLuaServiceInitializationContracts.PublishOwnColdService(r.Engine,publish);
     Eq(0x074c3fu,publish.At,"Cold service publishes last");Same(service,engine.LuaService68.Read(0),"Published into same Engine+68");Same(null,service.Lua00.Read(0),"Observed cold Lua is known-null before creation");
     Eq((byte)0,service.Initialized58.Read(0),"Native-ready byte starts and remains cold");Eq((ushort)0x0101,service.Options08.Read(0),"Observed cold options");
     using(lua=runtime.NewState(0x0f2e28)){
      // Explicit OWN writer for a conditional consumer test. This does not
      // claim that native0F2E00 traversed its unimplemented callback/glue path.
      service.Lua00.Write(lua);
      if(lua==null)throw new MissingContract(0x0f2e2e,"Actual OWN VM allocation returned null; conditional live-VM prerequisite is not established");
      lua.RequireLive(0x0f2e2e);result.VmCreated=true;Same(lua,service.Lua00.Read(0),"One actual VM identity in same service");Eq(1L,runtime.NativeNewStateCalls,"Exactly one native VM creation");Eq(1L,runtime.ActiveStateCount,"Exactly one live VM");
      var core=new Invocation();byte coreResult=GameLuaServiceInitializationContracts.OpenCore25A030(service,GameLuaLibraryBindings.ResolveOwn(runtime),runtime,core);
      result.CoreReturnRva=core.At;Eq((byte)1,coreResult,"Observed core helper normal AL return");Eq(0x25a142u,core.At,"Actual core helper return site");result.CoreReturned=true;
      Eq(5L,runtime.NativePushCClosureCalls,"Five real library pushes");Eq(5L,runtime.NativePCallZeroCalls,"Five real protected library calls");Eq(0,lua.GetTop(0),"Actual VM stack balanced before frame0");Eq((byte)0,service.Initialized58.Read(0),"Core return does not complete native service startup");
      InitialHorizontalDistance=Distance(a.Position48.X.Read(0),a.Position48.Z.Read(0),f.Target);bool reached=false;
      for(int frame=0;frame<result.FrameLimit;frame++){
       result.AttemptedFrames=frame+1;bool prefixReturned=false,tailEntered=false;uint unsupported=0;Exception primaryFailure=null;
       try{
        f.PrefixFrame(frame,true);prefixReturned=true;Check((f.Prefix.Reached25CB73||f.Prefix.Reached25CF79)&&f.Prefix.UpdateReturned,"Actual registered Agent reaches its steering edge");
        tailEntered=true;f.FinishChosenFrame(frame,goal);
        Check(f.Tail.Callback.CallbackReturned&&f.Tail.Callback.Moved.FloorSucceeded&&f.Tail.Callback.Moved.NotificationReturned&&f.Tail.Tail.Returned&&f.Tail.Tail.WorkCleared,"Actual Ground/publication/Goal-conditional tail returns");
        Check(!f.Tail.FullTick&&!f.Tail.Tail.FullTick,"Bounded contract never claims complete native Tick");
        Eq(Bits(a.Position48.X.Read(0)),r.Owner.Entity.CoordsD8.Position00.D8.Read(0),"Same Entity X publication");Eq(Bits(a.Position48.Z.Read(0)),r.Owner.Entity.CoordsD8.Position00.D10.Read(0),"Same Entity Z publication");
        Eq(Bits(f.Scene.Floor.LastGroundHit.Position04.Y4.Read(0)),r.Owner.Entity.CoordsD8.Position00.DC.Read(0),"Published Y comes from real saved Ground hit");Eq(0,WorkCount(m.Group),"Only returned actual tail cleared Work");
        Same(service,goal.Engine.CaptureEngine421428().Read(0).LuaService68.Read(0),"Goal/frames retain same service");Same(lua,service.Lua00.Read(0),"Goal/frames retain same live VM");lua.RequireLive(0x0ebb1c);Eq((byte)0,service.Initialized58.Read(0),"Frames do not fabricate native service readiness");
        f.SameState();f.Released();result.CompletedFrames++;
       }catch(MissingContract error){primaryFailure=error;unsupported=error.Address;result.FirstUnsupportedRva=error.Address;throw;}
       catch(Exception error){primaryFailure=error;throw;}
       finally{
        try{result.Frames.Add(RecordArrival(f,frame,prefixReturned,tailEntered,unsupported));}
        catch(Exception error){result.SnapshotFailure=error.ToString();result.SnapshotFailureFrame=frame+1;if(primaryFailure==null)throw;}
       }
       if(!f.Tail.Tail.GoalReturned)continue;
       result.GoalFrame=frame+1;result.GoalReturned=true;result.GoalTailReturned=f.Tail.Tail.Returned;result.GoalRva=f.Tail.Tail.GoalCall.Native.At;result.GoalTailRva=f.Tail.Tail.Native.At;
       Eq(23,result.GoalFrame,"Frozen same request reaches Goal on observed frame23");
       var call=f.Tail.Tail.GoalCall;Check(call.Returned&&call.HookReturned&&call.ActionStopReturned&&call.ActionDestructorReturned&&call.CharacterStopReturned&&call.MovementCleared,"Actual Goal, Hook, Stop and own action destruction all return");
       Check(f.StopProduction.LastDestruction!=null&&f.StopProduction.LastDestruction.OwnStorageReleased&&!f.StopProduction.LastDestruction.NativeHeapFreeExecuted,"Actual OWN action release, not original heap execution");
       Same(active,f.StopProduction.LastDestruction.ReturnedRax,"Original action allocation released");Same(a,f.StopProduction.LastStop.Agent.Read(0),"Stop resolves original registered Agent");Same(m.Registry,f.StopProduction.LastStop.Registry,"Stop uses original publication map");Same(r.Owner,f.StopProduction.LastStopOwner,"Stop acts on original Player");
       Missing(0,delegate{active.RequireLive(0);});Same(null,m.Composition.Handler.Current08.Read(0),"Actual destructor caller clears current action");Eq(1u,r.Owner.ActionState24B4.Read(0),"Actual stopped action state");Eq((byte)0,r.Manager.Moving38.Read(0),"Actual movement byte clear");Eq(3u,a.State70.Read(0),"Actual Stop writes State3");Eq(0u,a.Timer6C.Read(0),"Actual Stop clears timer");
       for(int axis=0;axis<3;axis++)Eq(0u,Bits(Values(a.VelocityB8)[axis]),"Goal copies existing OWN zero global velocity axis "+axis);
       Eq(1UL,m.Registry.Count38.Read(0),"Stop does not deregister");Eq(1u,a.RequestE8.Generation00.Read(0),"One original request generation");Eq((byte)1,hook.Initialized1C.Read(0),"Actual empty11 Hook initializer returns and publishes its own flag");result.HookInitialized=true;
       Eq(5L,runtime.NativePushCClosureCalls,"Empty11 Goal adds no synthetic Lua push");Eq(5L,runtime.NativePCallZeroCalls,"Empty11 Goal adds no synthetic Lua call");
       if(validateStoppedFrames){
        RunConditionalStoppedFrames(f,math,service,lua,result);reached=true;break;
       }
       // Attempt the real next prefix once. Observing this stop ends the
       // diagnostic; no suffix is entered by catching an unsupported prefix.
       result.PostStopAttemptedFrames=1;Stage="conditional-own-lua-post-stop-prefix";
       try{result.PostStopUnsupportedRva=Missing(0x25c8c2,delegate{f.PrefixFrame(frame+1,true);result.PostStopPrefixReturned=true;});}
       finally{
        result.PostStopPrefixRva=f.Prefix.Native.At;result.PostStopEarlyMovedReturned=f.Prefix.EarlyMovedReturned;result.PostStopUpdateReturned=f.Prefix.UpdateReturned;
        result.PostStopState=a.State70.Read(0);result.PostStopWorkCount=WorkCount(m.Group);result.PostStopNavigationPosition=Values(a.Position48);result.PostStopPublishedPosition=Values(r.Owner.Entity.CoordsD8.Position00);
       }
       result.FirstUnsupportedRva=result.PostStopUnsupportedRva;Eq(0x25c8c2u,result.PostStopPrefixRva,"Exact existing State3 membership boundary");Check(!result.PostStopPrefixReturned&&!result.PostStopUpdateReturned,"Post-stop prefix is not a completed frame");Eq(3u,result.PostStopState,"No diagnostic state repair");Eq(0,result.PostStopWorkCount,"Prior actual tail, not a fixture clear, emptied Work");
       Same(service,engine.LuaService68.Read(0),"Same published service after next-prefix stop");Same(lua,service.Lua00.Read(0),"Same still-live VM after next-prefix stop");lua.RequireLive(0x0ebb1c);Eq((byte)0,service.Initialized58.Read(0),"Native initializer remains incomplete");Eq((ushort)0x0101,service.Options08.Read(0),"No bootstrap option rewrite");
       f.SameState();f.Released();reached=true;break;
      }
      Check(reached,"Finite frame bound reached actual Goal and observed the next exact Unsupported; exhaustion is not success");
     }
     Check(lua!=null&&!lua.IsAlive,"OWN cleanup invalidates the actual VM identity");Same(lua,service.Lua00.Read(0),"OWN cleanup does not rewrite native service storage to null");Eq(1L,runtime.NativeCloseCalls,"Exactly one actual VM close");Eq(0L,runtime.ActiveStateCount,"No live VM remains");Same(null,runtime.CloseFailure,"Actual close has no recorded failure");
    }
    Check(runtime.ModuleUnloadObserved,"Pinned OWN module unload observed");
   }
   scenarios=validateStoppedFrames?2:1;result.PassedDiagnostic=true;result.Outcome=validateStoppedFrames?"observed-conditional-own-lua-goal-and-three-bounded-stopped-frames":"observed-conditional-own-lua-goal-tail-and-next-state3-unsupported";Stage="conditional-own-lua-diagnostic-complete";return result;
  }catch(MissingContract error){if(result.FirstUnsupportedRva==0)result.FirstUnsupportedRva=error.Address;result.Outcome="unsupported-conditional-own-lua-diagnostic";result.Failure=error.ToString();throw;}
  catch(Exception error){result.Outcome="failed-conditional-own-lua-diagnostic";result.Failure=error.ToString();throw;}
  finally{
   result.Stage=Stage;result.Scenarios=scenarios;result.Assertions=assertions;result.PriorFixtureSetupChecks=PriorFixtureSetupChecks;
   if(math!=null)result.NativeMathCalls=math.NativeMathCalls;
   if(service!=null)result.ServiceInitialized58=service.Initialized58.Read(0);
   if(runtime!=null){result.NewStateCalls=runtime.NativeNewStateCalls;result.CloseCalls=runtime.NativeCloseCalls;result.CorePushCalls=runtime.NativePushCClosureCalls;result.CorePCallCalls=runtime.NativePCallZeroCalls;result.ActiveStates=runtime.ActiveStateCount;result.CloseFailure=runtime.CloseFailure;result.ModuleUnloadObserved=runtime.ModuleUnloadObserved;}
   result.VmClosed=lua!=null&&runtime!=null&&runtime.CloseFailure==null&&runtime.ActiveStateCount==0&&!lua.IsAlive;
  }
 }
}
