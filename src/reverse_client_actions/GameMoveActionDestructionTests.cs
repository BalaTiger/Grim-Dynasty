using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

public static class GameMoveActionDestructionTests {
 static int scenarios,assertions,newScenarios,newAssertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 public static int NewScenarios{get{return newScenarios;}}
 public static int NewAssertions{get{return newAssertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action run,string why){try{run();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Test(string name,Action run){try{run();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static void TestNew(string name,Action run){int before=assertions;Test(name,run);newScenarios++;newAssertions+=assertions-before;}
 sealed class Fixture {
  public readonly MoveActionRecord Action;
  public readonly GameReachedGoalActionView Primary;
  public readonly GameMoveActionConstructionState State;
  public readonly GameMoveActionDestructionInvocation Call=new GameMoveActionDestructionInvocation();
  public Fixture(bool owned=false){Action=owned?GameMoveActionOwnHeap.AllocateOwn():new MoveActionRecord();Primary=new GameReachedGoalActionView(Action);State=GameMoveActionConstructionState.Bind(Action,Primary);}
  public void Construct(){GameMoveActionConstructionContracts.Construct082B10(State,Field<uint>.Of(31),new WorldPosition(),new WorldPosition(),Field<uint>.Of(7),Field<uint>.Of(11),Field<float>.Of(2),Field<uint>.Of(5),new GameMoveActionConstructionInvocation());}
 }
 public static void Run(){scenarios=assertions=newScenarios=newAssertions=0;
  Test("Constructed scalar keeps shared aliases and separates Engine/table from Game/this return",delegate{
   var f=new Fixture(true);f.Construct();var holder=new Handler();holder.Current08.Write(f.Action);
   Same(f.Action,GameMoveActionDestructionContracts.Destroy088EB0(f.State,0,f.Call),"Game returns original action");
   Same(f.Action,f.Call.ReturnedRax,"Original RAX identity");Same(f.Action,holder.Current08.Read(0),"Destructor does not clear caller-owned handler");Same(f.Primary.Virtual00,f.State.Virtual00,"One authoritative vptr cell");
   Same(GameMoveActionConstructionTable.Engine339EF0,f.Primary.Virtual00.Read(0),"Alias sees base table");Same(GameMoveActionConstructionTable.Engine339EF0,f.Call.EngineReturnedRax,"Engine RAX is table, not action");
   Check(f.Call.EngineEntered&&f.Call.EngineReturned&&f.Call.Returned&&!f.Call.FreeAttempted&&!f.Call.OwnStorageReleased&&!f.Call.NativeHeapFreeExecuted,"Actual scalar0 normal path retains OWN storage");Equal(0x088f42u,f.Call.Native.At,"Outer RET");
   f.Action.RequireLive(1);Check(true,"Scalar0 does not free OWN view");Same(f.State,GameMoveActionConstructionState.ForBound(f.Action,1),"Canonical state remains borrowed");
   Equal(2,f.Call.Writes.Count,"Exactly two observed stores");Equal(0x088f12u,f.Call.Writes[0].Address,"Game table store first");Equal(0x0485e7u,f.Call.Writes[1].Address,"Engine table store second");Equal(8,f.Call.Writes[0].Bytes,"Game pointer width");Equal(8,f.Call.Writes[1].Bytes,"Engine pointer width");
  });
  Test("Scalar0 leaves payload and unknown padding alone",delegate{
   var f=new Fixture();f.Action.OwnerId08.Write(0xdeadbeef);f.State.Dword0C.Write(0xaabbccdd);f.State.Byte10.Write(0x77);f.Action.Flag11.Write(0x88);f.Action.Destination60.Q16.Write(0xfedcba9876543210UL);
   GameMoveActionDestructionContracts.Destroy088EB0(f.State,0,f.Call);
   Equal(0xdeadbeefu,f.Action.OwnerId08.Read(0),"Owner retained");Equal(0xaabbccddu,f.State.Dword0C.Read(0),"Kind retained");Equal((byte)0x77,f.State.Byte10.Read(0),"Byte10 retained");Equal((byte)0x88,f.Action.Flag11.Read(0),"Byte11 retained");Equal(0xfedcba9876543210UL,f.Action.Destination60.Q16.Read(0),"Destination retained");
   Missing(17,delegate{f.Action.Start48.Q0.Read(17);},"Unknown position not initialized");Missing(18,delegate{f.State.Position20.D14.Read(18);},"Unknown padding not cleared");Missing(19,delegate{f.Action.Speed80.Read(19);},"Unused speed not consumed");
  });
  Test("Base body writes existing cell and returns its actual table RAX",delegate{
   var f=new Fixture();var i=new Invocation();f.Primary.Virtual00.Write(new object());f.Action.Flags78.Write(99);
   Same(GameMoveActionConstructionTable.Engine339EF0,GameMoveActionDestructionContracts.DestroyCharacterAction0485E0(f.State,i),"Observed LEA result");Same(GameMoveActionConstructionTable.Engine339EF0,f.Primary.Virtual00.Read(0),"Base store aliases primary");Equal(0x0485eau,i.At,"Base RET");Equal(99u,f.Action.Flags78.Read(0),"Base does not mutate derived payload");f.Action.RequireLive(0);Check(true,"Base does not release storage");
  });
  Test("Scalar1 rejects unregistered storage after completed base prefix",delegate{
   var f=new Fixture();f.Construct();var alias=f.Action;var holder=new Handler();holder.Current08.Write(alias);
   Missing(0x088f2a,delegate{GameMoveActionDestructionContracts.Destroy088EB0(f.State,1,f.Call);},"No allocation provenance from direct new");
   Check(f.Call.EngineEntered&&f.Call.EngineReturned&&f.Call.FreeAttempted&&!f.Call.Returned&&!f.Call.OwnStorageReleased&&!f.Call.NativeHeapFreeExecuted,"No false completed deletion");Same(GameMoveActionConstructionTable.Engine339EF0,f.Primary.Virtual00.Read(0),"Base prefix retained");Same(GameMoveActionConstructionTable.Engine339EF0,f.Call.EngineReturnedRax,"Base return recorded before failure");Same(null,f.Call.ReturnedRax,"No outer return invented");Equal(2,f.Call.Writes.Count,"Prefix not rolled back");Same(alias,holder.Current08.Read(0),"No caller pointer clearing");alias.RequireLive(0);Check(true,"Unregistered storage not marked released");Equal(31u,alias.OwnerId08.Read(0),"Payload preserved after stop");
  });
  TestNew("Scalar1 releases the original OWN allocation and invalidates every alias",delegate{
   var f=new Fixture(true);f.Construct();var alias=f.Action;var holder=new Handler();holder.Current08.Write(alias);
   Same(alias,GameMoveActionDestructionContracts.Destroy088EB0(f.State,1,f.Call),"Scalar delete returns original address identity without dereferencing it");
   Same(alias,f.Call.ReturnedRax,"Outer RAX preserves freed allocation identity");Same(alias,holder.Current08.Read(0),"Only caller may clear Handler.Current");
   Same(GameMoveActionConstructionTable.Engine339EF0,f.Primary.Virtual00.Read(0),"Actual base store retained for evidence");Same(GameMoveActionConstructionTable.Engine339EF0,f.Call.EngineReturnedRax,"Base RAX still table identity");
   Check(f.Call.EngineEntered&&f.Call.EngineReturned&&f.Call.FreeAttempted&&f.Call.OwnStorageReleased&&f.Call.Returned,"OWN allocation was actually invalidated before normal return");Check(!f.Call.NativeHeapFreeExecuted,"No native heap operation claimed");Equal(0x088f42u,f.Call.Native.At,"Outer RET after OWN release");
   Equal(2,f.Call.Writes.Count,"Exactly two observed vptr writes");Equal(0x088f12u,f.Call.Writes[0].Address,"Game vptr before free");Equal(0x0485e7u,f.Call.Writes[1].Address,"Base vptr before free");
   Missing(0x100,delegate{alias.RequireLive(0x100);},"Original alias is dead");Missing(0x101,delegate{((MoveActionRecord)holder.Current08.Read(0)).RequireLive(0x101);},"Handler alias is also dead until caller clears it");Missing(0x102,delegate{GameMoveActionConstructionState.ForBound(alias,0x102);},"Construction binding cannot revive released storage");
  });
  TestNew("Repeated scalar deletion and direct OWN free cannot hide double release",delegate{
   var f=new Fixture(true);f.Construct();GameMoveActionDestructionContracts.Destroy088EB0(f.State,1,f.Call);var prior=f.Primary.Virtual00.Read(0);
   Missing(0x088f12,delegate{GameMoveActionDestructionContracts.Destroy088EB0(f.State,1,f.Call);},"Released receiver fails before a second prefix");
   Equal(0,f.Call.Writes.Count,"No repeated table stores");Same(prior,f.Primary.Virtual00.Read(0),"No write through released view");Check(!f.Call.EngineEntered&&!f.Call.FreeAttempted&&!f.Call.OwnStorageReleased&&!f.Call.Returned,"Previous successful invocation state reset");Same(null,f.Call.ReturnedRax,"No stale successful return");
   Missing(0x088f2a,delegate{GameMoveActionOwnHeap.ReleaseOwn(f.Action,0x088f2a);},"Ledger independently rejects repeated free");
  });
  TestNew("Releasing one OWN allocation preserves a different allocation and unknown cells",delegate{
   var first=new Fixture(true);var second=new Fixture(true);first.Construct();
   Missing(0x110,delegate{second.Action.OwnerId08.Read(0x110);},"Allocation does not initialize native payload");Missing(0x111,delegate{second.Primary.Virtual00.Read(0x111);},"Allocation does not initialize a native vptr");
   GameMoveActionDestructionContracts.Destroy088EB0(first.State,1,first.Call);second.Action.RequireLive(0x112);Check(true,"Different OWN storage remains live");Same(second.State,GameMoveActionConstructionState.ForBound(second.Action,0x113),"Second canonical view retained");
   Missing(0x114,delegate{second.Action.OwnerId08.Read(0x114);},"Neighbor allocation remains Unknown");Missing(0x115,delegate{first.Action.RequireLive(0x115);},"First remains released");
  });
  Test("Array flags2 and3 stop before scalar writes or guessed header reads",delegate{
   foreach(uint flags in new uint[]{2,3}){var f=new Fixture(true);var old=new object();f.Primary.Virtual00.Write(old);
    Missing(0x088eda,delegate{GameMoveActionDestructionContracts.Destroy088EB0(f.State,flags,f.Call);},"Array domain rejected");Same(old,f.Primary.Virtual00.Read(0),"No scalar table prefix on array path");Equal(0,f.Call.Writes.Count,"No synthesized array stores");Check(!f.Call.EngineEntered&&!f.Call.FreeAttempted&&!f.Call.Returned,"No fabricated iterator/free");f.Action.RequireLive(0);Check(true,"Array rejection leaves lifetime untouched");}
  });
  Test("Released shared action fails before any destructor prefix",delegate{
   var f=new Fixture();f.Construct();var old=f.Primary.Virtual00.Read(0);var alias=f.Action;alias.MarkReleased();
   Missing(0x088f12,delegate{GameMoveActionDestructionContracts.Destroy088EB0(f.State,0,f.Call);},"OWN released receiver");Same(old,f.Primary.Virtual00.Read(0),"No write through released alias");Equal(0,f.Call.Writes.Count,"No prefix on dead receiver");Check(!f.Call.EngineEntered&&!f.Call.Returned,"No base entered");
   var i=new Invocation();Missing(0x0485e7,delegate{GameMoveActionDestructionContracts.DestroyCharacterAction0485E0(f.State,i);},"Base rejects same released receiver");Same(old,f.Primary.Virtual00.Read(0),"Base preserves dead prefix");
  });
  Test("Invocation reuse clears prior success before new Unsupported",delegate{
   var f=new Fixture();GameMoveActionDestructionContracts.Destroy088EB0(f.State,0,f.Call);var old=f.Primary.Virtual00.Read(0);
   Missing(0x088ece,delegate{GameMoveActionDestructionContracts.Destroy088EB0(f.State,0x100,f.Call);},"Higher bits outside explicit0/1 profile");Equal(0x100u,f.Call.CapturedFlags,"Exact uint input retained");Equal(0,f.Call.Writes.Count,"Trace reset");Check(!f.Call.EngineEntered&&!f.Call.EngineReturned&&!f.Call.Returned&&!f.Call.FreeAttempted,"Prior success not reused");Same(null,f.Call.ReturnedRax,"Prior receiver cleared");Same(null,f.Call.EngineReturnedRax,"Prior base return cleared");Same(old,f.Primary.Virtual00.Read(0),"Unsupported does not alter state");
  });
  Test("Missing receiver fails at actual scalar store",delegate{
   var c=new GameMoveActionDestructionInvocation();Missing(0x088f12,delegate{GameMoveActionDestructionContracts.Destroy088EB0(null,0,c);},"Missing receiver");Equal(0,c.Writes.Count,"No borrowed state fabricated");Check(!c.EngineEntered&&!c.Returned,"No call success");
   var i=new Invocation();Missing(0x0485e7,delegate{GameMoveActionDestructionContracts.DestroyCharacterAction0485E0(null,i);},"Missing base receiver");
  });
 }
}
