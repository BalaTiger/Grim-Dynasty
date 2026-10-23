using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // This identity binds only the two exact disk table entries recovered from
 // 767C98. It is not an RTTI/type-name claim or a native loaded vtable.
 public static class GameMainPlayerConnectionTable {
  public static readonly object Observed767C98=new object();
 }
 // Unique fields on the EXISTING connection, preserving its vptr and lifetime.
 // 30AFE0/3F60E0 cold construction is not implemented here. All new cells are
 // Unknown until an explicit OWN hot-state publication supplies their values.
 public sealed class GameMainPlayerConnectionFields {
  static readonly ConditionalWeakTable<GamePlayerVideoConnection,GameMainPlayerConnectionFields> connections=new ConditionalWeakTable<GamePlayerVideoConnection,GameMainPlayerConnectionFields>();
  public readonly GamePlayerVideoConnection Connection;
  public readonly Field<object> Owner08=new Field<object>(),Record20=new Field<object>();
  GameMainPlayerConnectionFields(GamePlayerVideoConnection connection){Connection=connection;}
  public static GameMainPlayerConnectionFields ForConnection(GamePlayerVideoConnection connection){
   if(connection==null)throw new ArgumentNullException("connection");connection.RequireLive(0);
   return connections.GetValue(connection,delegate(GamePlayerVideoConnection value){return new GameMainPlayerConnectionFields(value);});
  }
 }
 // The returned record retains its original identity and a BORROWED DWORD10
 // cell. It is not silently replaced by a Player/connection/zero-filled record.
 public sealed class GameMainPlayerRecordFields {
  static readonly ConditionalWeakTable<object,GameMainPlayerRecordFields> records=new ConditionalWeakTable<object,GameMainPlayerRecordFields>();
  static readonly object gate=new object();
  public readonly object Identity;
  public readonly NavigationView Lifetime;
  public readonly Field<uint> ObjectId10;
  GameMainPlayerRecordFields(object identity,NavigationView lifetime,Field<uint> id){Identity=identity;Lifetime=lifetime;ObjectId10=id;}
  public static GameMainPlayerRecordFields Bind(object identity,NavigationView lifetime,Field<uint> objectId10){
   if(identity==null||lifetime==null||objectId10==null)throw new ArgumentNullException("record identity/lifetime/ID10");
   var address=identity as NavigationView;if(address!=null)address.RequireLive(0);lifetime.RequireLive(0);
   lock(gate){GameMainPlayerRecordFields prior;if(records.TryGetValue(identity,out prior)){if(!Object.ReferenceEquals(prior.Lifetime,lifetime)||!Object.ReferenceEquals(prior.ObjectId10,objectId10))throw new InvalidOperationException("Record already has another lifetime or ID10 cell");return prior;}var value=new GameMainPlayerRecordFields(identity,lifetime,objectId10);records.Add(identity,value);return value;}
  }
  public static GameMainPlayerRecordFields ForBoundIdentity(object identity,uint at){GameMainPlayerRecordFields value;if(identity==null||!records.TryGetValue(identity,out value))throw new MissingContract(at,"Returned record requires its original identity/lifetime/ID10 binding");value.RequireLive(at);return value;}
  public void RequireLive(uint at){var address=Identity as NavigationView;if(address!=null)address.RequireLive(at);Lifetime.RequireLive(at);}
 }
 // Diagnostics contain no callbacks and cannot alter field values at a read.
 public sealed class GameMainPlayerInvocation {
  public readonly Invocation Native;
  public GameObjectManagerLookupInvocation Lookup{get;private set;}
  public readonly Field<object> FirstConnection=new Field<object>(),SecondConnection=new Field<object>();
  public readonly Field<object> FirstRecord=new Field<object>(),SecondRecord=new Field<object>();
  public readonly Field<uint> CapturedId=new Field<uint>();
  public readonly Field<MoveCharacter> Result=new Field<MoveCharacter>();
  public int Slot18Returns;
  public bool SingletonReturned,LookupReturned,ClassReturned,Returned;
  public GameMainPlayerInvocation():this(new Invocation()){}
  public GameMainPlayerInvocation(Invocation native){if(native==null)throw new ArgumentNullException("native");Native=native;Lookup=new GameObjectManagerLookupInvocation();}
  internal void Reset(){Native.At=0;Lookup=new GameObjectManagerLookupInvocation();FirstConnection.Forget();SecondConnection.Forget();FirstRecord.Forget();SecondRecord.Forget();CapturedId.Forget();Result.Forget();Slot18Returns=0;SingletonReturned=false;LookupReturned=false;ClassReturned=false;Returned=false;}
 }
 public static class GameMainPlayerContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing concrete main-player view");return value;}
  // Exact089CB0 MOV: retain even Unknown argument bits; no target dereference.
  public static void SetOwner089CB0(GamePlayerVideoConnection connection,Field<object> ownerBits,Invocation i){
   Need(i,0x089cb0).At=0x089cb0;Need(connection,i.At).RequireLive(i.At);
   GameMainPlayerConnectionFields.ForConnection(connection).Owner08.CopyFrom(Need(ownerBits,i.At));i.At=0x089cb4;
  }
  // Exact0D04E0 MOV/RET returns opaque pointer bits. A null, stale or Unknown
  // pointee is consumed by the CALLER, never converted to an empty record.
  public static Field<object> GetRecordBits0D04E0(GamePlayerVideoConnection connection,Invocation i){
   Need(i,0x0d04e0).At=0x0d04e0;Need(connection,i.At).RequireLive(i.At);
   var result=GameMainPlayerConnectionFields.ForConnection(connection).Record20.Snapshot();i.At=0x0d04e4;return result;
  }
  static Field<object> Slot18(object engineIdentity,uint loadAt,uint vptrAt,uint callAt,Field<object> capturedConnection,GameMainPlayerInvocation call){
   var i=call.Native;i.At=loadAt;var engine=GamePlayerVideoState.ForBoundIdentity(engineIdentity,i.At);capturedConnection.CopyFrom(engine.Connections40F8);
   i.At=vptrAt;var connection=capturedConnection.Read(i.At) as GamePlayerVideoConnection;
   Need(connection,i.At).RequireLive(i.At);var table=connection.Virtual00.Snapshot();
   i.At=callAt;if(!Object.ReferenceEquals(table.Read(i.At),GameMainPlayerConnectionTable.Observed767C98))throw new MissingContract(i.At,"Connection slot18 has no observed exact table binding");
   var result=GetRecordBits0D04E0(connection,i);call.Slot18Returns++;return result;
  }
  static MoveCharacter Return(MoveCharacter value,uint at,GameMainPlayerInvocation call){call.Native.At=at;call.Result.Write(value);call.Returned=true;return value;}
  static ClassRecord ClassInfo(object table,PlayerMousePlayerLookupContext context,Invocation i){
   if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Player)){i.At=0x016d10;return context.Bridge.Callbacks.PlayerClass.Read(i.At);}
   if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Character)){i.At=0x019d20;return context.Bridge.Callbacks.CharacterClass.Read(i.At);}
   throw new MissingContract(i.At,"Main-player class slot0 has no observed concrete binding");
  }
  public static MoveCharacter GetMainPlayer2EBB70(object capturedEngine,PlayerMousePlayerLookupContext lookup,Invocation invocation){return GetMainPlayer2EBB70(capturedEngine,lookup,new GameMainPlayerInvocation(Need(invocation,0x2ebb79)));}
  // Actual2EBB70: two separate40F8 loads and slot18 calls on the CAPTURED
  // engine, then exact hot01C040/019D40 and Player A5CAB0 class checks. No
  // global is read here. The first zero ID bypasses even the lookup context.
  public static MoveCharacter GetMainPlayer2EBB70(object capturedEngine,PlayerMousePlayerLookupContext lookup,GameMainPlayerInvocation invocation){
   var call=Need(invocation,0x2ebb79);call.Reset();var i=call.Native;
   call.FirstRecord.CopyFrom(Slot18(capturedEngine,0x2ebb79,0x2ebb80,0x2ebb83,call.FirstConnection,call));
   i.At=0x2ebb86;if(GameMainPlayerRecordFields.ForBoundIdentity(call.FirstRecord.Read(i.At),i.At).ObjectId10.Read(i.At)==0)return Return(null,0x2ebbf1,call);
   call.SecondRecord.CopyFrom(Slot18(capturedEngine,0x2ebb8c,0x2ebb93,0x2ebb96,call.SecondConnection,call));
   i.At=0x2ebb99;call.CapturedId.CopyFrom(GameMainPlayerRecordFields.ForBoundIdentity(call.SecondRecord.Read(i.At),i.At).ObjectId10);
   i.At=0x2ebb9c;var context=Need(lookup,i.At);object manager=GameReachedGoalEngineContracts.GetObjectManager01C040(context.Engine,i);call.SingletonReturned=true;
   i.At=0x2ebba7;var typed=manager as GameObjectManagerState;if(typed==null)throw new MissingContract(0x019d5f,"Published ObjectManager lacks its concrete shared lookup view");
   var result=GameObjectManagerLookupContracts.LookupBits019D40(typed,call.CapturedId,call.Lookup);call.LookupReturned=true;
   i.At=0x2ebbaf;object found=result.Read(i.At);if(found==null)return Return(null,0x2ebbe9,call);
   i.At=0x2ebbb4;var move=found as MoveCharacter;Need(move,i.At);var view=context.Bridge.ForMoveCharacter(move,i.At);view.Owner.Entity.RequireLive(i.At);var table=view.Owner.Entity.Virtual00.Snapshot();
   i.At=0x2ebbba;var actual=ClassInfo(table.Read(i.At),context,i);call.ClassReturned=true;
   i.At=0x2ebbc3;var expected=context.Bridge.Callbacks.PlayerClass.Read(i.At);if(Object.ReferenceEquals(actual,expected))return Return(move,0x2ebbe9,call);
   i.At=0x2ebbc8;var parentBits=Need(actual,i.At).Parent10.Snapshot();i.At=0x2ebbcc;var parent=parentBits.Read(i.At);if(parent==null)return Return(null,0x2ebbe9,call);
   i.At=0x2ebbd1;if(Object.ReferenceEquals(parent,expected))return Return(move,0x2ebbe9,call);
   i.At=0x2ebbd6;if(GameMovementCallbackContracts.IsDerivedFrom591480(parent,expected,i))return Return(move,0x2ebbe9,call);
   return Return(null,0x2ebbe9,call);
  }
 }
}
