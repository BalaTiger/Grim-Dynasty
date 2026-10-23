using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // A stable OWN skill address with an independently unknown primary vptr.
 // No observed table or +1B0 implementation is currently bound. A caller
 // cannot supply a delegate to turn a notification into synthetic success.
 public sealed class SkillMovedTarget:NavigationView {
  public readonly Field<object> Virtual00=new Field<object>();
 }
 // SkillManager is embedded at full Character+850 (interface38+818).
 // This view retains that same canonical Game owner; ForOwner never creates
 // a second manager for that owner. All six consumed cells start Unknown.
 public sealed class SkillMovedManagerState {
  static readonly ConditionalWeakTable<GameMovementCharacterState,SkillMovedManagerState> owners=new ConditionalWeakTable<GameMovementCharacterState,SkillMovedManagerState>();
  public readonly GameMovementCharacterState Owner;
  public readonly NavigationCrowdPointerVector<SkillMovedTarget> Collections228=new NavigationCrowdPointerVector<SkillMovedTarget>();
  public readonly NavigationCrowdPointerVector<SkillMovedTarget> Collections258=new NavigationCrowdPointerVector<SkillMovedTarget>();
  public readonly NavigationCrowdPointerVector<SkillMovedTarget> Collections360=new NavigationCrowdPointerVector<SkillMovedTarget>();
  SkillMovedManagerState(GameMovementCharacterState owner){Owner=owner;}
  public static SkillMovedManagerState ForOwner(GameMovementCharacterState owner){
   if(owner==null)throw new ArgumentNullException("owner");
   return owners.GetValue(owner,delegate(GameMovementCharacterState value){return new SkillMovedManagerState(value);});
  }
  public void RequireLive(uint at){Owner.Entity.RequireLive(at);}
  // Explicit OWN precondition, not a recovered SkillManager constructor or
  // evidence that a loaded character has no skills. Capacity is never read.
  public void UseOwnEmptyCollections(){
   RequireLive(0);Collections228.Begin00.Write(null);Collections228.End08.Write(null);
   Collections258.Begin00.Write(null);Collections258.End08.Write(null);
   Collections360.Begin00.Write(null);Collections360.End08.Write(null);
  }
 }
 // Game533410, exact r1-skill-moved-direct/04..06 saved body. Each begin is
 // captured once. The end is read again after every successful virtual call;
 // no end/count snapshot or collection prevalidation is substituted. At
 // present no +1B0 target can return, so post-call advancement is unreachable
 // in supported executions. Native heap/ABI/concurrent mutation are excluded.
 public static class SkillMovedContracts {
  static bool Same(NavigationCrowdPointerAddress<SkillMovedTarget> a,NavigationCrowdPointerAddress<SkillMovedTarget> b){
   if(a==null||b==null)return a==null&&b==null;
   return Object.ReferenceEquals(a.Buffer,b.Buffer)&&a.Index==b.Index;
  }
  static void Slot1B0(Field<object> table,uint at,Invocation i){
   i.At=at;table.Read(at);
   throw new MissingContract(at,"Skill moved virtual +1B0 target is unbound; no synthetic callback completion");
  }
  static void Collection(SkillMovedManagerState manager,NavigationCrowdPointerVector<SkillMovedTarget> vector,
   uint beginLoad,uint firstCompare,uint targetLoad,uint vptrLoad,uint call,uint advance,uint nextCompare,Invocation i){
   i.At=beginLoad;manager.RequireLive(i.At);vector.RequireLive(i.At);
   var cursorBits=vector.Begin00.Snapshot();uint compare=firstCompare;
   while(true){
    i.At=compare;manager.RequireLive(i.At);vector.RequireLive(i.At);
    var cursor=cursorBits.Read(i.At);var end=vector.End08.Read(i.At);
    if(Same(cursor,end))return;
    i.At=targetLoad;if(cursor==null)throw new MissingContract(i.At,"Null OWN skill-vector element address");
    var targetBits=cursor.Buffer.At(cursor.Index,i.At).Snapshot();
    i.At=vptrLoad;var target=targetBits.Read(i.At);
    if(target==null)throw new MissingContract(i.At,"Null OWN skill target before primary vptr load");
    target.RequireLive(i.At);var table=target.Virtual00.Snapshot();
    Slot1B0(table,call,i);
    i.At=advance;cursorBits=Field<NavigationCrowdPointerAddress<SkillMovedTarget>>.Of(new NavigationCrowdPointerAddress<SkillMovedTarget>(cursor.Buffer,checked(cursor.Index+1)));
    compare=nextCompare;
   }
  }
  public static void Moved533410(SkillMovedManagerState manager,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x53341a,"Missing own invocation");var i=invocation;i.At=0x53341a;
   if(manager==null)throw new MissingContract(i.At,"Null OWN SkillManager view");
   Collection(manager,manager.Collections228,0x53341a,0x533424,0x533430,0x533433,0x533436,0x53343c,0x533440,i);
   Collection(manager,manager.Collections258,0x533449,0x533450,0x533460,0x533463,0x533466,0x53346c,0x533470,i);
   Collection(manager,manager.Collections360,0x533479,0x533480,0x533490,0x533493,0x533496,0x53349c,0x5334a0,i);
   i.At=0x5334b3;
  }
 }
}
