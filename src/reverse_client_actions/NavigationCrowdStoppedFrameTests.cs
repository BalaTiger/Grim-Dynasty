using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Reached suffix tests. Real registration/Stop and proximity bodies operate on
// one shared OWN Agent. This is NOT a complete frame, real-map query, physical
// mouse or render test. The preceding projection remains an explicit gap.
public static class NavigationCrowdStoppedFrameTests {
 public static int Scenarios{get;private set;}public static int Assertions{get;private set;}
 static void Check(bool ok,string why){Assertions++;if(!ok)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> f,string why){Missing(0,delegate{f.Read(0);},why);}
 static void Test(string name,Action body){try{body();Scenarios++;Console.WriteLine("PASS StoppedFrameSuffix "+name);}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly GameReachedGoalStopProductionTests.Fixture Goal=new GameReachedGoalStopProductionTests.Fixture(true,true,true);
  public readonly NavigationCrowdPointerDomain<NavigationCrowdAgent> Domain=new NavigationCrowdPointerDomain<NavigationCrowdAgent>();
  public readonly NavigationCrowdAvoidanceConstants Constants=new NavigationCrowdAvoidanceConstants();
  public readonly NavigationCrowdStoppedFrameInvocation Call=new NavigationCrowdStoppedFrameInvocation();
  public readonly NavigationCrowdGroup Group;public readonly NavigationCrowdAgent Agent;
  public readonly NavigationCrowdStoppedFrameBoundaries Boundaries;
  public Fixture(){
   Goal.OwnActivationGates();Group=Goal.OwnRegistrationInputs();
   // Explicit OWN header setup precedes registration/Stop. No stop or suffix
   // manufactures an empty Work vector to hide an earlier incomplete frame.
   Group.Work40.Begin00.Write(null);Group.Work40.End08.Write(null);Group.Work40.Capacity10.Write(null);Group.Pointer58.Write(null);Group.Pointer60.Write(null);Group.Count68.Write(0);
   var activation=new GameMovementActivationInvocation();GameMovementActivationContracts.Activate08ED40(Goal.Manager,Goal.Activation,activation);Agent=activation.Registration.NewAgent;
   Goal.Stop();
   // These are declared OWN callback/global inputs, not recovered native cold
   // initialization. Updating the shared global does not rewrite old velocity.
   Goal.Owner.SpeedScaleDCC.Write(0);Goal.Owner.FlagDC8.Write(0);Goal.Owner.FlagDC9.Write(0);Goal.Owner.Entity.LastFrame178.Write(99);
   var engine=new EntityEngineState(Goal.ActivationEngine);engine.FrameCount640.Write(100);Goal.Callbacks.Entities.AttachEngine(engine);
   Constants.UseObservedDiskConstants();Boundaries=new NavigationCrowdStoppedFrameBoundaries(new NavigationCrowdProximityBoundaries(Goal.RegistrationState),Constants,Goal.RegistrationState.Global422980);Boundaries.UseObservedDiskConstant();
   Boundaries.Global422980.X.Write(0);Boundaries.Global422980.Y.Write(0);Boundaries.Global422980.Z.Write(0);PrepareIndex();
  }
  public void PrepareIndex(){NavigationCrowdProximityContracts.Prepare25E900(new NavigationCrowdProximityIndex(Group),1,new Invocation());}
  public void Run(){NavigationCrowdStoppedFrameContracts.FinishReachedSingleState3NoWork25C8C2(Group,Agent,Domain,Boundaries,Goal.Callbacks,Call);}
  public void StableStopped(){Eq(3u,Agent.State70.Read(0),"Actual State3 retained");Eq(0u,Agent.Timer6C.Read(0),"Real Stop timer retained");Eq(1.25f,Agent.Position48.X.Read(0),"Position X does not drift");Eq(2.5f,Agent.Position48.Y.Read(0),"Position Y does not drift");Eq(3.75f,Agent.Position48.Z.Read(0),"Position Z does not drift");Unknown(Agent.VelocityB8.X,"Stop/suffix do not invent zero velocity");Same(null,Goal.Handler.Current08.Read(0),"Actual caller release/clear remains");Same(Agent,Goal.Adapter.LastStop.Agent.Read(0),"Same real registration Agent stopped");Same(Agent,Group.Members28.Begin00.Read(0).Buffer.At(0,0).Read(0),"Same published member");Same(Boundaries.Global422980,Goal.RegistrationState.Global422980,"One shared loaded-global view");}
 }
 public static void Run(){Scenarios=Assertions=0;
  Test("Registered Player Stop and three reached State3 suffixes preserve position with true excludeSelf",delegate{
   var f=new Fixture();var request=f.Agent.RequestE8;var oldTarget=f.Agent.Target78.X.Read(0);
   Same(f.Goal.RegistrationState.Half34BADC,f.Boundaries.Proximity.Half34BADC,"Bounds and proximity borrow the same registration Half cell");
   f.Constants.Half34BADC.Write(.25f); // Deliberately conflicting unused cell.
   for(int n=0;n<3;n++){if(n!=0)f.PrepareIndex();f.Run();Check(f.Call.InsertReturned&&f.Call.UpdateReturned&&f.Call.NeighborQueryReturned&&f.Call.EmptyNeighbors,"Real index/update/query ran, empty is query result");Check(f.Call.GapStored&&f.Call.WorkCleared&&f.Call.SuffixReturned&&!f.Call.CompleteFrameReturned,"Only reached suffix finished");Eq(0x25e2fbu,f.Call.Native.At,"Actual suffix RET");Eq(1u,f.Group.Count78.Read(0),"Actual proximity contains self, not an injected empty index");var nodes=(NavigationCrowdProximityNodeBuffer)f.Group.Pointer60.Read(0);Same(f.Agent,nodes.At(127,0).Agent10.Read(0),"Inserted identity is exactly excluded identity");Eq(0UL,f.Agent.Words88B0[0].Read(0),"Constructor neighbor begin remains null");Eq(0UL,f.Agent.Words88B0[1].Read(0),"Real reset/query yields empty end");Eq(0u,f.Agent.GapD0.Read(0),"Actual shared-global X store");Same(null,f.Group.Work40.End08.Read(0),"Only real tail resets empty Work");Same(request,f.Agent.RequestE8,"No request replacement/reissue");Eq(oldTarget,f.Agent.Target78.X.Read(0),"Stop target remains");f.StableStopped();}
   Eq(.5f,f.Boundaries.Proximity.Half34BADC.Read(0),"Unused avoidance Half cannot overwrite shared global");Eq(.25f,f.Constants.Half34BADC.Read(0),"Unused conflicting cell is not silently synchronized");Eq(64f,f.Call.CenterX.Read(0),"Bounds use shared Half rather than conflicting avoidance copy");Eq(64f,f.Call.CenterZ.Read(0),"Z center uses shared Half");
   Eq((byte)0,f.Goal.Manager.Moving38.Read(0),"Actual Game Moving clear retained");Missing(0,delegate{f.Goal.Action.RequireLive(0);},"Original action is still actually released");
  });
  Test("Unknown shared Gap is propagated through stores and stops at first arithmetic consumer",delegate{
   var f=new Fixture();f.Boundaries.Global422980.X.Forget();Missing(0x25dd9e,delegate{f.Run();},"Unknown X at MULSS");Check(f.Call.InsertReturned&&f.Call.UpdateReturned&&f.Call.NeighborQueryReturned&&f.Call.EmptyNeighbors&&f.Call.GapStored,"Actual successful prefix retained");Check(!f.Call.WorkCleared&&!f.Call.SuffixReturned,"No false end-of-frame result");Unknown(f.Agent.GapD0,"MOV retained unknown X");Eq(0u,f.Agent.GapD8.Read(0),"Observed Z store retained");f.StableStopped();
  });
  Test("Unknown threshold and nonzero correction cannot be defaulted into no-drift success",delegate{
   var f=new Fixture();f.Constants.SpeedTolerance34B9B0.Forget();Missing(0x25ddaa,delegate{f.Run();},"Unknown captured COMISS operand");Check(f.Call.EmptyNeighbors&&f.Call.GapStored&&!f.Call.WorkCleared&&!f.Call.SuffixReturned,"Known query/store prefix remains before unknown threshold");f.StableStopped();
   f=new Fixture();f.Boundaries.Global422980.X.Write(1);Missing(0x25ddba,delegate{f.Run();},"Correction enters unimplemented actual Work append");Eq(0x3f800000u,f.Agent.GapD0.Read(0),"Nonzero global bits preserved");Check(f.Call.EmptyNeighbors&&!f.Call.WorkCleared&&!f.Call.SuffixReturned,"Empty neighbors alone do not mean frame success");Same(null,f.Group.Work40.End08.Read(0),"Unimplemented append has not been fabricated");f.StableStopped();
  });
  Test("Existing Work is not cleared and unknown neighbor factor stops after real neighbor reset",delegate{
   var f=new Fixture();var storage=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1);storage.Cells[0].Write(f.Agent);var work=NavigationCrowdBorrowedPointerContracts.Work40(f.Group,f.Domain);work.InstallOwnStorage(storage,1);Missing(0x25ca46,delegate{f.Run();},"Existing Work needs actual moving phases");Check(f.Call.InsertReturned&&f.Call.UpdateReturned&&!f.Call.GapStored&&!f.Call.WorkCleared&&!f.Call.SuffixReturned,"Only true prefix ran");Eq(1,work.Read(NavigationCrowdPointerSlot.End,0).Index,"Existing Work survives stop and failed suffix");f.StableStopped();
   f=new Fixture();f.Boundaries.CorrectionNeighborFactor34BC38.Forget();Missing(0x25dbe6,delegate{f.Run();},"Unknown actual factor is not guessed");Check(f.Call.GapStored&&!f.Call.NeighborQueryReturned&&!f.Call.WorkCleared&&!f.Call.SuffixReturned,"Failure prefix retained before query");Eq(f.Agent.Words88B0[0].Read(0),f.Agent.Words88B0[1].Read(0),"Real neighbor Begin-to-End store preceded factor use");f.StableStopped();
   f=new Fixture();f.Goal.RegistrationState.Half34BADC.Forget();Missing(0x25c912,delegate{f.Run();},"Unknown shared Half stops at first bounds ADDSS");Check(f.Call.InsertReturned&&!f.Call.UpdateReturned&&!f.Call.GapStored&&!f.Call.WorkCleared&&!f.Call.SuffixReturned,"Real insertion retained without later bounds/callback/correction success");Eq(.5f,f.Constants.Half34BADC.Read(0),"Known unused Half does not satisfy unknown shared operand");Eq(1u,f.Group.Count78.Read(0),"Earlier actual index insertion retained");f.StableStopped();
  });
 }
}
