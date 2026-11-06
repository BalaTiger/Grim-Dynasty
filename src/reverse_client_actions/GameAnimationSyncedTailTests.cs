using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Fixed OWN fixtures exercise actual recovered submission, getter/hash and
// empty attached-list bodies; they do not decode resources or render a frame.
public static class GameAnimationSyncedTailTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object a,object b,string why){Check(Object.ReferenceEquals(a,b),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly GameAnimationPlayTests.Fixture Play=new GameAnimationPlayTests.Fixture();
  public readonly GameEquipmentManagerState Equipment;
  public readonly GameEquipmentStorage Records=new GameEquipmentStorage(2);
  public readonly GameReachedGoalEngineState Engine=new GameReachedGoalEngineState();
  public readonly GameObjectManagerState Manager=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly PlayerMousePlayerLookupContext Lookup;
  public Fixture(){
   Equipment=GameEquipmentManagerState.ForOwner(Play.Owner);Equipment.Begin08.Write(new GameEquipmentPointer(Records,0));Equipment.End10.Write(new GameEquipmentPointer(Records,24));
   Records.Record(0).Id00.Write(0x89abcdef);Records.Record(0).Location04.Write(1);Records.Record(0).Byte09.Write(0);
   Records.Record(1).Id00.Write(0x12345678);Records.Record(1).Location04.Write(0);
   var sentinel=new GameObjectManagerNode();var buckets=new GameObjectManagerBuckets(1);buckets.Cells[0].First00.Write(sentinel);
   Manager.OwnCritical18.Write(Lock);Manager.Sentinel38.Write(sentinel);Manager.Buckets48.Write(buckets);Manager.Mask60.Write(0);
   Engine.Critical421398.Write(new GameReachedGoalCriticalState());Engine.Instance4213A0.Write(Manager);
   Lookup=new PlayerMousePlayerLookupContext(new PlayerMouseContext(new MoveBoundaries(new MoveContext())),Play.Bridge,Engine);Play.Context.BindLookup(Lookup);
   EngineAnimationChannelState.ForMesh(Play.Mesh).Resource10.Write(null);
  }
  public void EmptyAttached(){ActorBoundingBoxContracts.InitializeAttachedIdFields07D020(Play.Actor,new Invocation());}
 }
 static void Prefix(Fixture f,GameAnimationPlayInvocation c){
  Check(c.AnimationSubmitted&&c.ChannelCall.FieldsPublished&&c.HandStateEntered,"Actual channel writes survive later failure");
  Same(f.Play.Resource,EngineAnimationChannelState.ForMesh(f.Play.Mesh).Resource10.Read(0),"Original resource pointer published");
  Eq(3f,f.Play.SpeedFields.Speed130.Read(0),"Same existing speed field changed");Eq(5u,f.Play.Fields.Pending0C.Read(0),"Pending remains until complete return");Eq(0u,f.Play.Set.Kind08.Read(0),"Current Kind not advanced by prefix");
  Check(!c.Returned&&!c.PlayerReturned&&!c.EntryReturned,"No invented normal return");Eq(0,f.Lock.OwnOutstandingEnters,"No lock leaked");
 }
 public static void Run(){scenarios=assertions=0;
  Test("Actual Player request completes conditional normal return through recovered empty Entity propagation",delegate{
   var f=new Fixture();f.EmptyAttached();var c=new GameAnimationPlayInvocation();Eq((byte)1,f.Play.Play(c),"Entry returns1 after actual Player tail");
   Check(c.AnimationSubmitted&&c.HandStateReturned&&c.SyncedEntered&&c.SyncedReturned&&c.PlayerReturned&&c.EntryReturned&&c.Returned,"Every recovered body returned");
   Eq(0x89abcdefu,c.HandCall.LeftId.Read(0),"Real left scan ID");Eq(0x12345678u,c.HandCall.RightId.Read(0),"Real right scan ID");Eq(4,c.HandCall.LeftLookup.HashedBytes,"Real left hash");Eq(4,c.HandCall.RightLookup.HashedBytes,"Separate right hash");Same(f.Manager,c.HandCall.LeftManager,"Shared manager");Same(f.Manager,c.HandCall.RightManager,"Fresh singleton returns same identity");
   Eq(0u,c.HandState.Read(0),"Observed dual-null return encoding");Same(f.Play.Actor,c.SyncedCall.Actor,"Same Actor+78/+80");Eq(5u,c.SyncedCall.AnimationCapture.Read(0),"Captured Pending argument survives outer clear");Eq((byte)1,c.SyncedCall.LoopCapture.Read(0),"Normalized flag passed");Eq(0x07f575u,c.SyncedCall.Native.At,"Actual empty return");
   Eq(5u,f.Play.Set.Kind08.Read(0),"Outer current-kind success write now executes");Eq(0u,f.Play.Fields.Pending0C.Read(0),"Outer pending clear now executes");Eq(0x024e41u,c.Native.At,"Actual outer return");Unknown(c.SyncedCall.BlendCapture,"Empty branch does not load stack blend");Unknown(f.Play.Resource.Virtual00,"Success does not claim resource decode");Unknown(f.Records.Record(1).Byte09,"Right location0 never consumes flag");Eq(0,f.Lock.OwnOutstandingEnters,"Both actual lookup locks released");
  });
  Test("Move adapter reaches same conditional tail without rebuilding owner or resources",delegate{
   var f=new Fixture();f.EmptyAttached();var c=new GameMoveAnimationProductionInvocation();Eq((byte)1,GameMoveAnimationProductionContracts.Play(f.Play.Context,f.Play.Move,Field<AnimationSet>.Of(f.Play.Set),5,null,2,1,0,0x05fa70,c),"Existing Move call returns");Eq(1,c.PlayCalls.Count,"One real submission");Same(f.Play.Owner,c.PlayCalls[0].HandCall.Owner,"Same embedded equipment owner");Same(f.Play.Actor,c.PlayCalls[0].SyncedCall.Actor,"Same Actor identity");Eq(5u,f.Play.Set.Kind08.Read(0),"Same set updated");
  });
  Test("Unknown equipment endpoints retain channel prefix and do not become empty",delegate{
   var f=new Fixture();f.Equipment.End10.Forget();var c=new GameAnimationPlayInvocation();Missing(0x284733,delegate{f.Play.Play(c);},"Unknown end at pointer subtract");Prefix(f,c);Check(!c.HandStateReturned&&!c.SyncedEntered,"No unknown hand-state success");Unknown(c.HandState,"No return encoding guessed");
  });
  Test("Cold singleton is encountered after real equipment scan and channel publication",delegate{
   var f=new Fixture();f.Engine.Critical421398.Write(null);var c=new GameAnimationPlayInvocation();Missing(0x01c05e,delegate{f.Play.Play(c);},"Cold singleton allocation remains unresolved");Prefix(f,c);Eq(0x89abcdefu,c.HandCall.LeftId.Read(0),"Earlier getter result retained");Check(c.HandCall.LeftGetterReturned&&!c.HandCall.LeftLookupReturned&&!c.SyncedEntered,"No map/default side effect");
  });
  Test("Unknown attached endpoints stop only after real two-handed lookup return",delegate{
   var f=new Fixture();var c=new GameAnimationPlayInvocation();Missing(0x07f4a6,delegate{f.Play.Play(c);},"Unknown end consumed at SUB");Prefix(f,c);Check(c.HandStateReturned&&c.SyncedEntered&&!c.SyncedReturned,"Hand result returned, Entity tail still incomplete");Check(c.HandCall.LeftLookupReturned&&c.HandCall.RightLookupReturned,"Both real lookups complete");Unknown(c.SyncedCall.EndCapture,"No empty-list assumption");
  });
  Test("Nonempty attachment path stops at actual first DWORD read",delegate{
   var f=new Fixture();f.Play.Actor.AttachedBegin78.Write(0x1000);f.Play.Actor.AttachedEnd80.Write(0x1004);var c=new GameAnimationPlayInvocation();Missing(0x07f4e7,delegate{f.Play.Play(c);},"No Engine lookup or forwarding fabricated");Prefix(f,c);Check(c.HandStateReturned&&c.SyncedCall.CountComputed&&!c.SyncedReturned,"Only confirmed count prefix");Eq(1L,c.SyncedCall.ShiftedCount,"SAR2 count");Eq(3,c.SyncedCall.BlendCapture.Read(0),"Nonempty branch loads previously computed blend");Eq(0x1000UL,c.SyncedCall.FreshBegin.Read(0),"Fresh current begin captured");
  });
  Test("Entity empty branch uses exact wrapping SUB SAR and skips unused argument consumption",delegate{
   var f=new Fixture();foreach(ulong delta in new ulong[]{0,1,2,3}){f.Play.Actor.AttachedBegin78.Write(ulong.MaxValue);f.Play.Actor.AttachedEnd80.Write(unchecked(ulong.MaxValue+delta));var c=new EngineEntitySyncedAnimationInvocation();EngineEntitySyncedAnimationContracts.PlaySyncedAnimation07F490(f.Play.Actor,new Field<uint>(),new Field<uint>(),new Field<byte>(),new Field<int>(),c);Check(c.Returned&&c.CountComputed,"Shift-zero path returns");Eq(0L,c.ShiftedCount,"Exact64bit wrapping subtract");Unknown(c.AnimationCapture,"Opaque animation argument not consumed");Unknown(c.HandCapture,"Opaque hand argument not consumed");}
   f.Play.Actor.AttachedBegin78.Write(100);f.Play.Actor.AttachedEnd80.Write(99);var negative=new EngineEntitySyncedAnimationInvocation();Missing(0x07f4e7,delegate{EngineEntitySyncedAnimationContracts.PlaySyncedAnimation07F490(f.Play.Actor,new Field<uint>(),new Field<uint>(),new Field<byte>(),new Field<int>(),negative);},"Negative arithmetic shift is nonempty");Eq(-1L,negative.ShiftedCount,"SAR not unsigneddivide");
  });
  Test("Each Entity call reloads live list words and never changes capacity",delegate{
   var f=new Fixture();f.EmptyAttached();f.Play.Actor.AttachedCapacity88.Forget();var c=new EngineEntitySyncedAnimationInvocation();EngineEntitySyncedAnimationContracts.PlaySyncedAnimation07F490(f.Play.Actor,new Field<uint>(),new Field<uint>(),new Field<byte>(),new Field<int>(),c);Check(c.Returned,"Observed constructor establishes empty path");f.Play.Actor.AttachedEnd80.Write(4);
   Missing(0x07f4e7,delegate{EngineEntitySyncedAnimationContracts.PlaySyncedAnimation07F490(f.Play.Actor,new Field<uint>(),new Field<uint>(),new Field<byte>(),new Field<int>(),new EngineEntitySyncedAnimationInvocation());},"No cached empty result");Unknown(f.Play.Actor.AttachedCapacity88,"Capacity unused");
  });
 }
}
