using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // One +2C cell per explicitly bound runtime identity. Binding borrows an
 // existing lifetime view and leaves the ID Unknown; lookup never creates it.
 // ItemConstructorStorage is a separate byte-storage model and is not copied
 // into this view. Arbitrary native addresses/lifetime discovery are excluded.
 public sealed class GameObjectIdState {
  static readonly ConditionalWeakTable<object,GameObjectIdState> identities=new ConditionalWeakTable<object,GameObjectIdState>();
  static readonly object gate=new object();
  public readonly object Identity;
  public readonly Field<uint> ObjectId2C=new Field<uint>();
  readonly SkillMovedTarget skill;
  readonly EntityMovementState entity;
  GameObjectIdState(SkillMovedTarget value){Identity=value;skill=value;}
  GameObjectIdState(EntityMovementState value){Identity=value.Identity;entity=value;}
  public static GameObjectIdState BindSkill(SkillMovedTarget value){
   if(value==null)throw new ArgumentNullException("value");value.RequireLive(0);
   lock(gate){GameObjectIdState prior;if(identities.TryGetValue(value,out prior)){if(!Object.ReferenceEquals(prior.skill,value))throw new InvalidOperationException("Object identity already has another lifetime view");return prior;}var fresh=new GameObjectIdState(value);identities.Add(value,fresh);return fresh;}
  }
  public static GameObjectIdState BindEntity(EntityMovementState value){
   if(value==null)throw new ArgumentNullException("value");value.RequireLive(0);
   lock(gate){GameObjectIdState prior;if(identities.TryGetValue(value.Identity,out prior)){if(!Object.ReferenceEquals(prior.entity,value))throw new InvalidOperationException("Object identity already has another lifetime view");return prior;}var fresh=new GameObjectIdState(value);identities.Add(value.Identity,fresh);return fresh;}
  }
  public static GameObjectIdState ForBoundIdentity(object identity,uint at){
   GameObjectIdState value;if(identity==null||!identities.TryGetValue(identity,out value))throw new MissingContract(at,"Object::GetObjectId requires an explicitly bound existing object/lifetime view");value.RequireLive(at);return value;
  }
  public void RequireLive(uint at){
   GameObjectIdState actual;if(!identities.TryGetValue(Identity,out actual)||!Object.ReferenceEquals(actual,this))throw new MissingContract(at,"Object ID view is not the unique bound identity");
   var address=Identity as NavigationView;if(address!=null)address.RequireLive(at);
   if(skill!=null)skill.RequireLive(at);else if(entity!=null)entity.RequireLive(at);else throw new MissingContract(at,"Object ID view has no represented lifetime");
  }
 }
 // Game IAT609128 -> Engine03A740. Saved normal bytes8B412CC3 prove exactly
 // MOV EAX,[RCX+2C]; RET03A743. This scalar adapter requires a known uint ID;
 // it neither allocates one nor supplies an injected getter callback.
 public static class GameObjectIdContracts {
  public static uint GetObjectId03A740(object identity,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x03a740,"Missing object-ID invocation");var i=invocation;i.At=0x03a740;
   var state=GameObjectIdState.ForBoundIdentity(identity,i.At);uint result=state.ObjectId2C.Read(i.At);i.At=0x03a743;return result;
  }
 }
}
