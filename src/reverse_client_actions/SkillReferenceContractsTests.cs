using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Six new component scenarios only. Storage, identity bindings and numeric IDs
// are explicit OWN inputs; no constructor, native heap or active End is inferred.
public static class SkillReferenceContractsTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Invalid(Action action,string why){try{action();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected binding rejection: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly SkillReferenceManagerState Skills;
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());
   var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);
   Bridge=new GameMovementIdentityBridge(Callbacks);Bridge.Bind(Owner,Move);Skills=SkillReferenceManagerState.ForOwner(Owner);
  }
 }
 static void Empty(NavigationCrowdPointerVector<SkillMovedTarget> vector){vector.Begin00.Write(null);vector.End08.Write(null);}
 static void EmptyAll(SkillReferenceManagerState state){
  Empty(state.Collections20);Empty(state.Collections258);Empty(state.Collections378);Empty(state.Collections2C8);Empty(state.Collections360);
  state.Offset3BC.Write(0);state.Offset3C0.Write(0);state.Offset3C4.Write(0);state.Offset3C8.Write(0);
 }
 // Capacity remains Unknown because531C50 never reads it.
 static NavigationCrowdPointerBuffer<SkillMovedTarget> Store(NavigationCrowdPointerVector<SkillMovedTarget> vector,params SkillMovedTarget[] values){
  var buffer=new NavigationCrowdPointerBuffer<SkillMovedTarget>(values.Length);
  for(int n=0;n<values.Length;n++)buffer.Cells[n].Write(values[n]);
  vector.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,0));
  vector.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,values.Length));return buffer;
 }
 static SkillMovedTarget Target(uint id){var target=new SkillMovedTarget();GameObjectIdState.BindSkill(target).ObjectId2C.Write(id);return target;}
 static uint Get(SkillReferenceManagerState state,uint reference,Invocation i){return SkillReferenceContracts.GetSkillId531C50(state,reference,i);}
 public static void Run(){scenarios=assertions=0;
  Test("Canonical Game Move bridge shares notification258 and360 storage",delegate{
   var f=new Fixture();var state=f.Skills;var notifications=SkillMovedManagerState.ForOwner(f.Owner);
   Same(state,SkillReferenceManagerState.ForOwner(f.Owner),"One manager per existing full owner");
   Same(state,SkillReferenceManagerState.ForMoveCharacter(f.Bridge,f.Move,0),"Move role resolves same manager");
   Same(f.Move,f.Owner.Entity.Identity,"Existing Move object is full Entity identity");
   Same(notifications,state.Notifications,"Existing notification manager retained");
   Same(notifications.Collections258,state.Collections258,"258 vector is exact notification vector");
   Same(notifications.Collections360,state.Collections360,"360 vector is exact notification vector");
   Unknown(state.Collections258.Begin00,"Alias setup does not initialize258 header");
   Unknown(state.Offset3BC,"Manager lookup does not initialize offset");
   notifications.UseOwnEmptyCollections();Same(null,state.Collections258.Begin00.Read(0),"Explicit notification setup visible in258");Same(null,state.Collections360.End08.Read(0),"Explicit notification setup visible in360");
   EmptyAll(state);var target=Target(27);Store(notifications.Collections258,target);var i=new Invocation();
   Eq(27u,SkillReferenceContracts.GetSkillId531C50(f.Bridge,f.Move,0,i),"Bridge overload reads live notification storage");
   GameObjectIdState.ForBoundIdentity(target,0).ObjectId2C.Write(29);Eq(29u,Get(state,0,i),"No copied or cached ID");
   Empty(notifications.Collections258);Store(notifications.Collections360,Target(39));Eq(39u,Get(state,0,i),"Fifth collection reads original360 alias");
   Unknown(state.Collections360.Capacity10,"Capacity is not needed for reference lookup");
   Missing(0x531c50,delegate{SkillReferenceContracts.GetSkillId531C50(f.Bridge,new MoveCharacter(),0,i);},"Unbound Move role cannot create a manager");
  });
  Test("Five collection priority known null fallback and first nonnull short circuit",delegate{
   var f=new Fixture();var state=f.Skills;var i=new Invocation();var first=Target(10);Store(state.Collections20,first);
   Eq(10u,Get(state,0,i),"First nonnull bypasses unknown later headers and offsets");Eq(0x03a743u,i.At,"Actual ID getter return");
   GameObjectIdState.ForBoundIdentity(first,0).ObjectId2C.Write(0);Eq(0u,Get(state,0,i),"Nonnull target with actual ID0 still returns immediately");Eq(0x03a743u,i.At,"ID0 is not mistaken for null fallback");
   EmptyAll(state);
   var vectors=new NavigationCrowdPointerVector<SkillMovedTarget>[]{state.Collections20,state.Collections258,state.Collections378,state.Collections2C8,state.Collections360};
   var buffers=new NavigationCrowdPointerBuffer<SkillMovedTarget>[5];
   for(int n=0;n<5;n++)buffers[n]=Store(vectors[n],Target((uint)((n+1)*10)));
   for(int n=0;n<5;n++){Eq((uint)((n+1)*10),Get(state,0,i),"Earliest remaining nonnull segment wins");Eq(0x03a743u,i.At,"Nonempty selection invokes real scalar getter");buffers[n].Cells[0].Write(null);}
   Eq(0u,Get(state,0,i),"All five known null slots fall through");Eq(0x531d48u,i.At,"Actual miss return");
   for(int n=0;n<5;n++)Unknown(vectors[n].Capacity10,"Lookup did not consume capacity");
  });
  Test("Reference zero uint subtraction wrap and zero extended underflow",delegate{
   var f=new Fixture();var state=f.Skills;var i=new Invocation();EmptyAll(state);
   Store(state.Collections20,Target(91));Eq(91u,Get(state,0,i),"Reference0 has no synthetic no-skill special case");
   Empty(state.Collections20);Store(state.Collections258,Target(101),Target(102));state.Offset3BC.Write(UInt32.MaxValue);
   Eq(102u,Get(state,0,i),"uint0 minus uintMax wraps to index1");
   Eq(101u,Get(state,UInt32.MaxValue,i),"uintMax minus uintMax selects index0");
   state.Offset3BC.Write(1);Store(state.Collections378,Target(201));state.Offset3C0.Write(0);
   Eq(201u,Get(state,0,i),"Underflow becomes zero extended uintMax and skips small second count");
   Empty(state.Collections378);Eq(0u,Get(state,0,i),"Unsigned out of range index does not clamp into second storage");Eq(0x531d48u,i.At,"Underflow miss traverses remaining explicit empty segments");
  });
  Test("Reversed extent retains arithmetic shift unsigned compare and actual slot bounds",delegate{
   var f=new Fixture();var state=f.Skills;var i=new Invocation();
   var buffer=Store(state.Collections20,Target(1),Target(301),Target(302));
   state.Collections20.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,1));
   state.Collections20.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,0));
   Eq(301u,Get(state,0,i),"Negative eight-byte span SAR3 produces unsigned max count and loads begin1");
   Eq(302u,Get(state,1,i),"Actual next slot can be within declared storage despite reversed extent");
   Missing(0x531c68,delegate{Get(state,2,i);},"Reversed count enters actual one-past buffer load");Eq(0x531c68u,i.At,"Bounds failure stays at native slot load");
   Empty(state.Collections20);var second=Store(state.Collections258,Target(401),Target(402));
   state.Collections258.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(second,1));
   state.Collections258.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(second,0));state.Offset3BC.Write(1);
   Missing(0x531ca3,delegate{Get(state,0,i);},"Underflow uintMax compares below ulongMax but zero extended actual slot is unsupported");
   var other=new NavigationCrowdPointerBuffer<SkillMovedTarget>(1);
   state.Collections20.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,0));state.Collections20.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(other,0));
   Missing(0x531c54,delegate{Get(state,0,i);},"Cross allocation subtraction is not invented");
   state.Collections20.End08.Write(null);Missing(0x531c54,delegate{Get(state,0,i);},"Mixed null and typed address has no invented difference");
  });
  Test("Unknown consumers preserve first second and final segment instruction order",delegate{
   var f=new Fixture();var state=f.Skills;var i=new Invocation();
   Missing(0x531c54,delegate{Get(state,0,i);},"First captured unknown header is consumed by subtraction");
   Empty(state.Collections20);Missing(0x531c85,delegate{Get(state,0,i);},"Second header is consumed before unknown3BC offset");
   Empty(state.Collections258);Missing(0x531c8c,delegate{Get(state,0,i);},"Even known empty second count requires actual offset read");
   state.Offset3BC.Write(0);Empty(state.Collections378);state.Offset3C0.Write(0);Empty(state.Collections2C8);state.Offset3C4.Write(0);
   Missing(0x531d12,delegate{Get(state,0,i);},"Final3C8 offset precedes final unknown header");
   state.Offset3C8.Write(0);Missing(0x531d1f,delegate{Get(state,0,i);},"Final header consumed at subtraction after offset");
   var slot=new NavigationCrowdPointerBuffer<SkillMovedTarget>(1);state.Collections20.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(slot,0));state.Collections20.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(slot,1));
   Missing(0x531c6c,delegate{Get(state,0,i);},"Unknown first slot is captured at MOV and consumed at TEST");
   var unbound=new SkillMovedTarget();slot.Cells[0].Write(unbound);Missing(0x03a740,delegate{Get(state,0,i);},"First nonnull unbound identity reaches actual object getter before later headers");
   var bound=GameObjectIdState.BindSkill(unbound);Missing(0x03a740,delegate{Get(state,0,i);},"Explicit binding leaves2C unknown");
   bound.ObjectId2C.Write(501);Eq(501u,Get(state,0,i),"Known first ID bypasses still unknown final header");Unknown(unbound.Virtual00,"Scalar getter does not consume virtual table");
  });
  Test("Object getter requires explicit live binding and retains shared Entity identity",delegate{
   var i=new Invocation();var target=new SkillMovedTarget();
   Missing(0x03a740,delegate{GameObjectIdContracts.GetObjectId03A740(target,i);},"Unbound getter does not manufacture ID view");
   Missing(0x1234,delegate{GameObjectIdState.ForBoundIdentity(target,0x1234);},"Failed getter did not create binding");
   var skill=GameObjectIdState.BindSkill(target);Same(skill,GameObjectIdState.BindSkill(target),"Explicit same skill binding is stable");Unknown(skill.ObjectId2C,"Binding leaves scalar field unknown");
   Missing(0x03a740,delegate{GameObjectIdContracts.GetObjectId03A740(target,i);},"Unknown2C stops at actual load");
   skill.ObjectId2C.Write(0xfedcba98u);Eq(0xfedcba98u,GameObjectIdContracts.GetObjectId03A740(target,i),"Getter preserves all32 ID bits");Eq(0x03a743u,i.At,"Scalar getter real return");
   var f=new Fixture();Missing(0x03a740,delegate{GameObjectIdContracts.GetObjectId03A740(f.Move,i);},"Existing Entity identity still requires explicit ID binding");
   var entity=GameObjectIdState.BindEntity(f.Owner.Entity);Same(entity,GameObjectIdState.BindEntity(f.Owner.Entity),"Existing Entity lifetime view reused");Same(entity,GameObjectIdState.ForBoundIdentity(f.Move,0),"Same Move and Entity identity selects original ID cell");
   Unknown(entity.ObjectId2C,"Entity binding does not initialize ID");entity.ObjectId2C.Write(601);Eq(601u,GameObjectIdContracts.GetObjectId03A740(f.Owner.Entity.Identity,i),"Existing full Entity identity reaches2C");
   Invalid(delegate{GameObjectIdState.BindEntity(new EntityMovementState(f.Move));},"A second lifetime view cannot replace shared Entity binding");
   Same(entity,GameObjectIdState.ForBoundIdentity(f.Move,0),"Rejected rebind preserves original mapping");
   f.Owner.Entity.MarkReleased();Missing(0x03a740,delegate{GameObjectIdContracts.GetObjectId03A740(f.Move,i);},"Released original Entity stops actual getter");
   Missing(0x531c50,delegate{Get(f.Skills,0,i);},"Released shared owner fails before header consumption");
   target.MarkReleased();Missing(0x03a740,delegate{GameObjectIdContracts.GetObjectId03A740(target,i);},"Released skill cannot return stale scalar ID");
  });
 }
}

public static class SkillReferenceValidationProgram {
 public static int Main(string[] args){
  try{
   if(args.Length!=0)throw new ArgumentException("This fixed component program takes no arguments");
   SkillReferenceContractsTests.Run();
   Console.WriteLine("{\"status\":\"passed-new-skill-reference-components-only\",\"scenarios\":"+SkillReferenceContractsTests.Scenarios+",\"assertions\":"+SkillReferenceContractsTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"system_library_loaded\":false,\"full_tick\":false,\"active_move_end_completed\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}
 }
}
