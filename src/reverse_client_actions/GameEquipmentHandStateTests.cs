using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Explicit OWN equipment/map setup. Every lookup below runs the recovered
// getter, hot singleton and real Monitor-backed019D40; no result callbacks.
public static class GameEquipmentHandStateTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected invalid operation: "+why);}
 static void Argument(Action body,string why){try{body();}catch(ArgumentException){Check(true,why);return;}throw new Exception("Expected argument rejection: "+why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly MoveContext Movement=new MoveContext();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly PlayerMouseContext Mouse;
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly GameReachedGoalEngineState Engine=new GameReachedGoalEngineState();
  public readonly GameReachedGoalCriticalState SingletonCritical=new GameReachedGoalCriticalState();
  public readonly GameObjectManagerState Objects=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public readonly GameEquipmentManagerState Equipment;
  public readonly GameEquipmentStorage Storage=new GameEquipmentStorage(2);
  public readonly GameEquipmentRecord Left,Right;
  public Fixture(){
   Movement.CharacterClass.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   var callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),Movement.CharacterClass,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(callbacks);Bridge.Bind(Owner,Move);
   Mouse=new PlayerMouseContext(new MoveBoundaries(Movement));Lookup=new PlayerMousePlayerLookupContext(Mouse,Bridge,Engine);Lookup.BindRole(new MouseRoleState(Move,Owner.Entity.Virtual00));
   Equipment=GameEquipmentManagerState.ForOwner(Owner);Left=Storage.Record(0);Right=Storage.Record(1);
   Equipment.Begin08.Write(new GameEquipmentPointer(Storage,0));Equipment.End10.Write(new GameEquipmentPointer(Storage,24));
   Left.Location04.Write(1);Left.Byte09.Write(0);Left.Id00.Write(7);Right.Location04.Write(0);Right.Id00.Write(8);
   Engine.Critical421398.Write(SingletonCritical);Engine.Instance4213A0.Write(Objects);
   Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(Sentinel);Objects.Buckets48.Write(Buckets);Objects.Mask60.Write(0);Buckets.Cells[0].First00.Write(Sentinel);
  }
  public uint Run(GameEquipmentHandStateInvocation call){return GameEquipmentHandStateContracts.GetHandState285E50(Owner,Lookup,call);}
  public GameObjectManagerNode Single(uint key){
   var node=new GameObjectManagerNode();node.Key10.Write(key);node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(node);Buckets.Cells[0].Last08.Write(node);return node;
  }
  public GameObjectManagerNode[] Pair(uint firstKey,uint secondKey){
   var first=new GameObjectManagerNode();var second=new GameObjectManagerNode();first.Key10.Write(firstKey);second.Key10.Write(secondKey);first.Next00.Write(second);second.Next00.Write(Sentinel);
   Buckets.Cells[0].First00.Write(first);Buckets.Cells[0].Last08.Write(second);return new[]{first,second};
  }
  public void LookupReturned(GameObjectManagerLookupInvocation call,int visited){
   Check(call.Returned&&call.OwnLockEntered&&call.OwnLockExited,"Actual lookup returned after normal OWN Monitor exit");Check(!call.OwnCleanupReleased,"Normal return needs no exceptional cleanup");
   Eq(4,call.HashedBytes,"All four ID bytes hashed");Eq(visited,call.VisitedNodes,"Exact real map traversal");Eq(0,Lock.OwnOutstandingEnters,"No outstanding OWN lock");
  }
  public void LookupFailed(GameObjectManagerLookupInvocation call,uint at){
   Eq(at,call.Native.At,"Exact nested failure address");Check(call.OwnLockEntered&&!call.OwnLockExited&&call.OwnCleanupReleased&&!call.Returned,"Actual lookup failure releases OWN Monitor without claiming a normal return");
   Eq(0,Lock.OwnOutstandingEnters,"Failure does not leak OWN lock");Unknown(call.Result,"Failed lookup has no fabricated result");
  }
  public void Returned(GameEquipmentHandStateInvocation call,int leftVisited,int rightVisited){
   Eq(0u,call.ReturnValue,"Both-null observed encoding");Eq(0x286088u,call.Native.At,"Actual return address");Check(call.Used&&call.Returned,"Hand-state call completed");
   Check(call.LeftGetterReturned&&call.RightGetterReturned&&call.LeftSingletonReturned&&call.RightSingletonReturned&&call.LeftLookupReturned&&call.RightLookupReturned,"Both actual getter/singleton/lookup chains completed");
   Same(Owner,call.Owner,"Original owner retained");Same(Equipment,call.Equipment,"Original inline equipment sidecar retained");Same(Objects,call.LeftManager,"Left hot manager identity");Same(Objects,call.RightManager,"Right independently obtained hot manager identity");
   Eq(0x01c103u,call.LeftSingleton.At,"Left singleton hot return");Eq(0x01c103u,call.RightSingleton.At,"Right singleton hot return");
   Check(!Object.ReferenceEquals(call.LeftLookup,call.RightLookup)&&!Object.ReferenceEquals(call.LeftSingleton,call.RightSingleton),"Separate native-call diagnostics");
   LookupReturned(call.LeftLookup,leftVisited);LookupReturned(call.RightLookup,rightVisited);Same(null,call.LeftResult.Read(0),"Left known-null result");Same(null,call.RightResult.Read(0),"Right known-null result");
  }
 }
 static void RightNotStarted(GameEquipmentHandStateInvocation call){Check(!call.RightGetterReturned&&!call.RightSingletonReturned&&!call.RightLookupReturned&&!call.Returned,"Left failure prevents the entire right chain");Unknown(call.RightId,"Right ID not fabricated");Unknown(call.RightResult,"Right result not fabricated");}
 public static void Run(){scenarios=assertions=0;
  Test("Distinct equipment IDs run both real hot lookups before the both-null return",delegate{
   var f=new Fixture();var c=new GameEquipmentHandStateInvocation();Eq(0u,f.Run(c),"Two actual map misses");f.Returned(c,0,0);
   Eq(7u,c.LeftId.Read(0),"Actual left record ID");Eq(8u,c.RightId.Read(0),"Actual right record ID");Check(c.LeftLookup.Hash.Read(0)!=c.RightLookup.Hash.Read(0),"Right lookup hashes its own distinct ID");
   Same(f.Equipment,GameEquipmentManagerState.ForOwner(f.Owner),"Stable shared equipment identity");Unknown(f.Right.Byte09,"Location0 right record does not consume Byte09");Unknown(f.Owner.ActionState24B4,"Hand query does not initialize action state");Unknown(f.SingletonCritical.Native10,"Hot singleton did not initialize cold synchronization state");
  });
  Test("Explicit empty equipment still looks up zero IDs and accepts matched-null records",delegate{
   var f=new Fixture();f.Equipment.Begin08.Write(null);f.Equipment.End10.Write(null);var node=f.Single(0);node.Object18.Write(null);var c=new GameEquipmentHandStateInvocation();
   Eq(0u,f.Run(c),"Known-null matches for actual zero keys");f.Returned(c,1,1);Eq(0u,c.LeftId.Read(0),"Left empty-table ID");Eq(0u,c.RightId.Read(0),"Right empty-table ID");
   Eq(0x4d25767f9dce13f5UL,c.LeftLookup.Hash.Read(0),"Zero ID FNV reference");Eq(0x4d25767f9dce13f5UL,c.RightLookup.Hash.Read(0),"Second actual zero-ID hash");Same(null,node.Object18.Read(0),"Query leaves matched-null storage intact");
  });
  Test("Unknown left ID reaches hashing only after actual singleton and OWN lock entry",delegate{
   var f=new Fixture();f.Left.Id00.Forget();var c=new GameEquipmentHandStateInvocation();Missing(0x019d81,delegate{f.Run(c);},"Opaque ID first consumed by019D81");
   Eq(0x019d81u,c.Native.At,"Caller retains exact nested stop");Check(c.LeftGetterReturned&&c.LeftSingletonReturned&&!c.LeftLookupReturned,"Getter returned Unknown and singleton returned before hash failure");
   Unknown(c.LeftId,"Left ID stays Unknown");Eq(0,c.LeftLookup.HashedBytes,"No hash multiplication before missing ID");f.LookupFailed(c.LeftLookup,0x019d81);RightNotStarted(c);
  });
  Test("Unknown right ID retains completed left miss then releases the second OWN lookup lock",delegate{
   var f=new Fixture();f.Right.Id00.Forget();var c=new GameEquipmentHandStateInvocation();Missing(0x019d81,delegate{f.Run(c);},"Right opaque ID consumed by its own real lookup");
   Check(c.LeftLookupReturned&&c.RightGetterReturned&&c.RightSingletonReturned&&!c.RightLookupReturned&&!c.Returned,"Only left chain returned fully");f.LookupReturned(c.LeftLookup,0);Same(null,c.LeftResult.Read(0),"Completed left miss remains known-null");
   Eq(7u,c.LeftId.Read(0),"Left ID retained");Unknown(c.RightId,"Right ID remains Unknown");Unknown(c.RightResult,"Failed right lookup is not converted to null");f.LookupFailed(c.RightLookup,0x019d81);
  });
  Test("Unknown or undeclared manager and missing OWN lock cannot become empty-map defaults",delegate{
   var f=new Fixture();f.Engine.Instance4213A0.Forget();var c=new GameEquipmentHandStateInvocation();Missing(0x01c0ae,delegate{f.Run(c);},"Unknown actual hot instance");
   Check(c.LeftGetterReturned&&!c.LeftSingletonReturned&&!c.LeftLookupReturned,"ID getter precedes manager failure");Eq(7u,c.LeftId.Read(0),"ID captured before unknown instance");Check(!c.LeftLookup.OwnLockEntered,"No map lock before manager resolution");RightNotStarted(c);
   f=new Fixture();f.Engine.Instance4213A0.Write(new object());c=new GameEquipmentHandStateInvocation();Missing(0x019d5f,delegate{f.Run(c);},"Opaque manager has no concrete map");Check(c.LeftSingletonReturned&&!c.LeftLookupReturned&&!c.LeftLookup.OwnLockEntered,"Singleton return is distinct from lookup completion");RightNotStarted(c);
   f=new Fixture();f.Objects.OwnCritical18.Forget();c=new GameEquipmentHandStateInvocation();Missing(0x019d63,delegate{f.Run(c);},"Unknown lock stops actual map entry");Check(!c.LeftLookup.OwnLockEntered&&!c.LeftLookup.OwnCleanupReleased,"No fabricated lock enter or cleanup");Eq(0,f.Lock.OwnOutstandingEnters,"No lock held");RightNotStarted(c);
  });
  Test("Unknown left Object18 is consumed by caller TEST after lookup normal unlock",delegate{
   var f=new Fixture();f.Single(7);var c=new GameEquipmentHandStateInvocation();Missing(0x285e88,delegate{f.Run(c);},"Unknown result is not an empty map");
   Check(c.LeftLookupReturned,"Lookup returned opaque Object18");f.LookupReturned(c.LeftLookup,1);Unknown(c.LeftResult,"Caller captured Unknown bits");Unknown(c.LeftLookup.Result,"Nested result also remains Unknown");RightNotStarted(c);
  });
  Test("Unknown right Object18 fails at right TEST after both independent normal unlocks",delegate{
   var f=new Fixture();var nodes=f.Pair(7,8);nodes[0].Object18.Write(null);var c=new GameEquipmentHandStateInvocation();Missing(0x285f11,delegate{f.Run(c);},"Right result TEST consumes Unknown");
   Check(c.LeftLookupReturned&&c.RightLookupReturned&&!c.Returned,"Both map calls returned but hand state did not");f.LookupReturned(c.LeftLookup,1);f.LookupReturned(c.RightLookup,2);Same(null,c.LeftResult.Read(0),"First matched-null retained");Unknown(c.RightResult,"Right Unknown is not null");Eq(8u,c.RightId.Read(0),"Right query used distinct equipment ID");
  });
  Test("Nonnull left weapon stops before unproved class dispatch and before right getter",delegate{
   var f=new Fixture();var value=new object();f.Single(7).Object18.Write(value);f.Right.Id00.Forget();var c=new GameEquipmentHandStateInvocation();Missing(0x285e8d,delegate{f.Run(c);},"Left actual vptr boundary remains Unsupported");
   Check(c.LeftLookupReturned,"Actual left lookup completed");f.LookupReturned(c.LeftLookup,1);Same(value,c.LeftResult.Read(0),"Original weapon identity captured");RightNotStarted(c);Unknown(f.Right.Id00,"Early stop did not initialize right ID");
  });
  Test("Nonnull right weapon follows left matched-null and stops at its distinct vptr boundary",delegate{
   var f=new Fixture();var nodes=f.Pair(7,8);var value=new object();nodes[0].Object18.Write(null);nodes[1].Object18.Write(value);var c=new GameEquipmentHandStateInvocation();Missing(0x285f1a,delegate{f.Run(c);},"Right class target remains Unsupported");
   Check(c.LeftLookupReturned&&c.RightLookupReturned&&!c.Returned,"Both actual queries completed without inventing hand encoding");f.LookupReturned(c.LeftLookup,1);f.LookupReturned(c.RightLookup,2);Same(null,c.LeftResult.Read(0),"Left branch remains null");Same(value,c.RightResult.Read(0),"Right identity preserved");Same(value,nodes[1].Object18.Read(0),"No lookup mutation");
  });
  Test("Hand lookup and animation binding require the same existing owner and bridge",delegate{
   var f=new Fixture();var foreign=new Fixture();var c=new GameEquipmentHandStateInvocation();Missing(0x285e6e,delegate{GameEquipmentHandStateContracts.GetHandState285E50(f.Owner,foreign.Lookup,c);},"Foreign lookup bridge cannot resolve original owner");
   Check(c.LeftGetterReturned&&!c.LeftSingletonReturned&&!c.LeftLookupReturned,"Identity rejection occurs after real ID getter but before singleton");Eq(7u,c.LeftId.Read(0),"Original owner's ID captured");Eq(0,foreign.Lock.OwnOutstandingEnters,"Foreign map was not entered");RightNotStarted(c);
   var bounds=new ActorBoundingBoxContext(f.Bridge.Callbacks.Entities);var actor=bounds.Bind(f.Owner.Entity);var animation=new GameMoveAnimationProductionContext(f.Bridge,f.Move,new EngineAnimationSpeedContext(bounds,actor));
   animation.BindLookup(f.Lookup);animation.BindLookup(f.Lookup);Same(f.Lookup,animation.Lookup,"Stable original lookup binding");Same(f.Engine,animation.Lookup.Engine,"Original singleton state is borrowed");Same(f.Engine.Instance4213A0,animation.Lookup.Engine.Instance4213A0,"Singleton pointer cell is shared, not copied");Argument(delegate{animation.BindLookup(foreign.Lookup);},"Foreign bridge binding rejected");
   var duplicate=new PlayerMousePlayerLookupContext(f.Mouse,f.Bridge,f.Engine);Invalid(delegate{animation.BindLookup(duplicate);},"Same bridge cannot silently replace existing lookup context");Same(f.Lookup,animation.Lookup,"Rejected bindings retain original identity");
  });
  Test("Fresh calls observe live equipment and map while reused invocation cannot retain a false success",delegate{
   var f=new Fixture();var first=new GameEquipmentHandStateInvocation();Eq(0u,f.Run(first),"Initial actual misses");f.Returned(first,0,0);
   f.Left.Id00.Write(19);var value=new object();f.Single(19).Object18.Write(value);Invalid(delegate{f.Run(first);},"Invocation reuse rejected before new work");Eq(7u,first.LeftId.Read(0),"Original capture retained");Same(null,first.LeftResult.Read(0),"Original completed miss retained");
   var next=new GameEquipmentHandStateInvocation();Missing(0x285e8d,delegate{f.Run(next);},"Fresh call sees changed real record and object map");Eq(19u,next.LeftId.Read(0),"Fresh ID from same storage");Same(value,next.LeftResult.Read(0),"Fresh original object identity");f.LookupReturned(next.LeftLookup,1);RightNotStarted(next);Eq(0,f.Lock.OwnOutstandingEnters,"Both calls leave no lock held");
  });
 }
}
