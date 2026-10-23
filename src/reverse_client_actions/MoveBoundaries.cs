using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Each default is an unresolved native dependency, never a successful mock.
 // Overrides are observations/adapters; they are not proof of native behavior.
 public class MoveBoundaries:Boundaries {
  public readonly MoveContext Context;
  public MoveBoundaries(MoveContext context){Context=context;}
  public override void Start08(ActionRecord action){
   var a=action as MoveActionRecord;if(a==null){base.Start08(action);return;}
   MoveContracts.Guard(delegate(Invocation i){MoveContracts.Begin(a,this,i);});
  }
  public override void Stop18(ActionRecord action){
   var a=action as MoveActionRecord;if(a==null){base.Stop18(action);return;}
   MoveContracts.Guard(delegate(Invocation i){MoveContracts.Stop(a,this,i);});
  }
  public virtual object ObjectManager(){throw Missing(0x609098,"Object manager singleton/lifetime");}
  public virtual object Lookup(object manager,uint id){throw Missing(0x019d40,"Object lookup");}
  public virtual object Associated(object manager,uint id){throw Missing(0x077d60,"Associated-object lookup");}
  public virtual ClassRecord Class0(object instance){throw Missing(0x082c37,"Object class virtual +0");}
  public virtual byte Derived(ClassRecord parent,ClassRecord expected){throw Missing(0x591480,"Class inheritance walk");}
  public virtual byte CanMove460(MoveCharacter role){throw Missing(0x082c6a,"Character virtual +460");}
  public virtual WorldPosition Coords(MoveCharacter role,WorldPosition hiddenReturn){throw Missing(0x609518,"GetCoords returned pointer/coordinate source");}
  public virtual object Region(WorldPosition position){throw Missing(0x6094a0,"Position region");}
  public virtual void Associated60(object associated,uint flags){throw Missing(0x082cd1,"Associated virtual +60");}
  public virtual void Associated80(object associated){throw Missing(0x05f91e,"Associated virtual +80");}
  public virtual float DistanceSquared(object world,WorldPosition a,WorldPosition b){throw Missing(0x609a10,"World distance");}
  public virtual void SetPathPosition(MoveCharacter role,WorldPosition position){throw Missing(0x05d9a0,"SetPathPosition");}
  public virtual uint SkillId(MoveCharacter role,uint reference){throw Missing(0x531c50,"Skill reference -> ID");}
  public virtual byte Skill5A8(object skill){throw Missing(0x5345a6,"Skill virtual +5A8");}
  public virtual void Skill498(object skill,object context){throw Missing(0x5345ba,"Skill virtual +498");}
  public virtual byte Skill6B0(object skill,object context){throw Missing(0x082dc0,"Activated skill virtual +6B0");}
  public virtual void Skill6B8(object skill,object context,uint flags){throw Missing(0x082fc6,"Activated skill virtual +6B8");}
  public virtual void ForceSpeed(MoveCharacter role){throw Missing(0x068ca0,"ForceSpeedUpdate");}
  public virtual uint State458(MoveCharacter role){throw Missing(0x082ddf,"Character virtual +458");}
  public virtual void State450(MoveCharacter role,uint state){throw Missing(0x05fa3f,"Character virtual +450");}
  public virtual void RunningSound(MoveCharacter role){throw Missing(0x05cdc0,"Running sound");}
  public virtual byte AlreadyThere(MovementManager manager,WorldPosition position,float tolerance){throw Missing(0x08e170,"AlreadyThere/navigation distance");}
  public virtual byte MoveManager(MovementManager manager,WorldPosition position){throw Missing(0x08eaf0,"MovementManager::MoveTo/navigation");}
  public virtual void StopManager(MovementManager manager){throw Missing(0x08ed00,"MovementManager::Stop");}
  public virtual object Navigation(){throw Missing(0x609c10,"Navigation singleton");}
  public virtual void StopObject(object navigation,object owner){throw Missing(0x609f50,"Navigation stop import");}
  public virtual AnimationSet Animations(MoveCharacter role){throw Missing(0x05ae40,"GetAnimationSet");}
  public virtual float AnimationSpeed(MoveCharacter role,uint index){throw Missing(0x609bf0,"Actor animation speed");}
  public virtual byte Play(AnimationSet set,MoveCharacter role,uint animation,object name,float speed,byte loop,uint extra){throw Missing(0x024de0,"PlayAnimation");}
  public virtual byte IsMoving(MoveCharacter role){throw Missing(0x05bc20,"IsMoving");}
  public virtual void StopMoving(MoveCharacter role,byte first,byte second){throw Missing(0x05fc90,"StopMoving");}
  public virtual byte SoundCheck(object sound){throw Missing(0x609a08,"Stop-path sound check import");}
  public virtual void SoundStop(object sound,uint id,uint zero){throw Missing(0x609a28,"Stop-path sound stop import");}
  public virtual void VectorImport(MoveCharacter role,LocalVector3 value){throw Missing(0x609d10,"Stop-path vector import");}
 }
}
