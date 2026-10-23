using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Proven branch/write order, not a recovered native allocation or running mover.
 // Normal FP completion only: native MXCSR traps/status and EH remain outside.
 public static class MoveContracts {
  internal static T Need<T>(T value,uint at) where T:class {if(value==null)throw new MissingContract(at,"Null dereference outside supported domain");return value;}
  internal static MoveCharacter Character(object value,uint at){var role=value as MoveCharacter;if(role==null)throw new MissingContract(at,"Class accepted but no shared character view");return role;}
  public static void Guard(Action<Invocation> body){
   var i=new Invocation();try{body(i);}catch(MissingContract){throw;}
   catch(Exception x){throw new MissingContract(i.At,"Dependency did not return normally; native EH unmodeled",x);}
  }
  public static Outcome Run(Action<Invocation> body){
   try{Guard(body);return new Outcome{ReturnedNormally=true};}
   catch(MissingContract x){return new Outcome{Address=x.Address,Boundary=x.Message,Exception=x};}
  }
  // LEA binds a fixed symbol identity, not a replaceable global pointer.
  static bool Accept(object value,AddressSymbol<ClassRecord> expected,MoveBoundaries b,Invocation i,uint call,uint parentAt,uint derivedAt){
   i.At=call;ClassRecord actual=Need(b.Class0(value),call);
   ClassRecord target=Need(expected.Read(call+2),call+2);
   if(Object.ReferenceEquals(actual,target))return true;
   ClassRecord parent=actual.Parent10.Read(parentAt);if(parent==null)return false;
   if(Object.ReferenceEquals(parent,target))return true;
   i.At=derivedAt;return b.Derived(parent,target)!=0;
  }
  public static void Begin(MoveActionRecord a,MoveBoundaries b,Invocation i){
   a.RequireLive(0x082c0f);uint owner=a.OwnerId08.Read(0x082c0f);i.At=0x082c15;object manager=b.ObjectManager();
   i.At=0x082c20;object value=b.Lookup(manager,owner);
   if(value==null||!Accept(value,b.Context.CharacterClass,b,i,0x082c37,0x082c45,0x082c57)){FailStart(a,b,i);return;}
   MoveCharacter role=Character(value,0x082c64);
   i.At=0x082c6a;if(b.CanMove460(role)==0){FailStart(a,b,i);return;}
   var buffer=new WorldPosition();i.At=0x082c80;WorldPosition coords=b.Coords(role,buffer);
   i.At=0x082c89;if(b.Region(coords)==null){FailStart(a,b,i);return;}
   if(role.Gate26AF.Read(0x082c98)!=0){FailStart(a,b,i);return;}
   uint associatedId=role.AssociatedId16C0.Read(0x082ca5);
   i.At=0x082cb3;manager=b.ObjectManager();i.At=0x082cbe;object associated=b.Associated(manager,associatedId);
   if(associated!=null){a.RequireLive(0x082cce);uint flags=a.Flags78.Read(0x082cce);i.At=0x082cd1;b.Associated60(associated,flags);}
   i.At=0x082cdd;coords=b.Coords(role,buffer);
   a.RequireLive(0x082ce6);
   WorldEngine engine=Need(b.Context.EngineGlobal.Read(0x082cf1),0x082cf4);
   object world=engine.World50.Read(0x082cf4);
   i.At=0x082cf8;float distance=b.DistanceSquared(world,coords,a.Start48);
   // COMISS/JBE: unordered, equal and below skip. Constant verified as 100f.
   if(distance>100.0f){a.RequireLive(0x082d07);i.At=0x082d0e;b.SetPathPosition(role,a.Start48);}
   StopCurrentSkill(role,b,i);
   a.RequireLive(0x082d1f);uint reference=a.SkillReference7C.Read(0x082d1f),attackFlags=a.Flags78.Read(0x082d27);
   SetAttackTarget(role,attackFlags,a.Destination60,reference,b,i);
   // Exact vslot+68 target0819E0: no callback and no inferred radius meaning.
   a.RequireLive(0x082d32);float scalar=a.Scalar14.Read(0x0819e0);
   uint animation=a.Animation84.Read(0x082d3b);float speed=a.Speed80.Read(0x082d4c);
   Move(role,a.Destination60,scalar,animation,speed,b,i);
   a.RequireLive(0x082d5f);reference=a.SkillReference7C.Read(0x082d5f);i.At=0x082d69;uint skillId=b.SkillId(role,reference);
   i.At=0x082d70;manager=b.ObjectManager();i.At=0x082d7b;object skill=b.Lookup(manager,skillId);
   if(skill!=null&&Accept(skill,b.Context.ActivatedSkillClass,b,i,0x082d8e,0x082d9c,0x082daa)){
    object context=role.SkillContext868.Read(0x082db9);i.At=0x082dc0;
    if(b.Skill6B0(skill,context)!=0){role.SpeedFlag26B3.Write(1);i.At=0x082dd4;b.ForceSpeed(role);}
   }
   i.At=0x082ddf;if(b.State458(role)==5){i.At=0x082e06;b.RunningSound(role);}
  }
  static void FailStart(MoveActionRecord a,MoveBoundaries b,Invocation i){a.RequireLive(0x082e0b);i.At=0x082e11;b.Stop18(a);}
  public static void SetAttackTarget(MoveCharacter role,uint flags,WorldPosition position,uint reference,MoveBoundaries b,Invocation i){
   role.AttackFlags2688.Write(flags);Need(position,0x05e67f);
   // Native 16-byte load/store followed by a separate 8-byte load/store.
   var lo=position.Q0.Snapshot();var hi=position.Q8.Snapshot();
   role.AttackPosition2690.Q0.CopyFrom(lo);role.AttackPosition2690.Q8.CopyFrom(hi);
   var tail=position.Q16.Snapshot();role.AttackPosition2690.Q16.CopyFrom(tail);
   i.At=0x05e6a2;uint id=b.SkillId(role,reference);role.AttackSkill26A8.Write(id);
  }
  public static void StopCurrentSkill(MoveCharacter role,MoveBoundaries b,Invocation i){
   uint id=role.CurrentSkillA90.Read(0x53454a);i.At=0x534553;object manager=b.ObjectManager();
   i.At=0x53455e;object skill=b.Lookup(manager,id);
   if(skill==null||!Accept(skill,b.Context.SkillClass,b,i,0x534571,0x53457f,0x53458d))return;
   role.CurrentSkillA90.Write(0);i.At=0x5345a6;
   if(b.Skill5A8(skill)!=0){object context=role.SkillContext868.Read(0x5345b6);i.At=0x5345ba;b.Skill498(skill,context);}
  }
  // Manager identity is captured across Navigation(), but Owner00 is read after it.
  static void InlineStop(MovementManager manager,MoveBoundaries b,Invocation i,uint flagAt,uint singletonAt,uint ownerAt,uint stopAt){
   Need(manager,flagAt);
   if(manager.Registered6C.Read(flagAt)!=0){i.At=singletonAt;object nav=b.Navigation();object owner=manager.Owner00.Read(ownerAt);i.At=stopAt;b.StopObject(nav,owner);}
   manager.Moving38.Write(0);
  }
  static void NotifyAssociated(MoveCharacter role,MoveBoundaries b,Invocation i,uint getAt,uint idAt,uint lookupAt,uint callAt){
   i.At=getAt;object manager=b.ObjectManager();uint id=role.AssociatedId16C0.Read(idAt);
   i.At=lookupAt;object associated=b.Associated(manager,id);
   if(associated!=null){i.At=callAt;b.Associated80(associated);}
  }
  public static void Move(MoveCharacter role,WorldPosition destination,float unusedScalar,uint animation,float speed,MoveBoundaries b,Invocation i){
   // 05F870 overwrites XMM2 before reading it. Do not use unusedScalar as radius.
   i.At=0x05f88b;object region=b.Region(destination);
   if(region==null){InlineStop(role.MovementDC0.Read(0x05fab0),b,i,0x05fab7,0x05fabd,0x05fac3,0x05fac9);return;}
   i.At=0x05f8a0;if(b.CanMove460(role)==0){InlineStop(role.MovementDC0.Read(0x05fab0),b,i,0x05fab7,0x05fabd,0x05fac3,0x05fac9);return;}
   float tolerance=b.Context.AlreadyThereConstant7A7FD8.Read(0x05f8ae);
   MovementManager movement=role.MovementDC0.Read(0x05f8b9);i.At=0x05f8c0;
   if(b.AlreadyThere(movement,destination,tolerance)!=0){
    InlineStop(role.MovementDC0.Read(0x05f8c9),b,i,0x05f8d0,0x05f8d6,0x05f8dc,0x05f8e2);
    NotifyAssociated(role,b,i,0x05f8ec,0x05f8f2,0x05f8fb,0x05f91e);return;
   }
   movement=role.MovementDC0.Read(0x05f925);i.At=0x05f92f;
   if(b.MoveManager(movement,destination)!=0){
    i.At=0x05f9fb;AnimationSet set=Need(b.Animations(role),0x05fa05);uint oldAnimation=set.Kind08.Read(0x05fa05);
    i.At=0x05fa08;float oldSpeed=b.AnimationSpeed(role,0);i.At=0x05fa17;uint state=b.State458(role);
    // UCOMISS/JZ accepts equal OR unordered; not ordinary float equality.
    if(state==5&&oldAnimation==animation&&(oldSpeed==speed||Single.IsNaN(oldSpeed)||Single.IsNaN(speed)))return;
    i.At=0x05fa3f;b.State450(role,5);i.At=0x05fa48;set=b.Animations(role);
    object name=b.Context.NoNameImport609568.Read(0x05fa4d);i.At=0x05fa70;
    if(b.Play(set,role,animation,name,speed,1,0)==0){movement=role.MovementDC0.Read(0x05fa79);i.At=0x05fa80;b.StopManager(movement);i.At=0x05fa90;b.State450(role,1);}return;
   }
   movement=Need(role.MovementDC0.Read(0x05f93c),0x05f943);i.At=0x05f947;region=b.Region(movement.Position20);
   if(region!=null){
    movement=Need(role.MovementDC0.Read(0x05f952),0x05f961);tolerance=b.Context.AlreadyThereConstant7A7FD8.Read(0x05f959);i.At=0x05f965;
    if(b.AlreadyThere(movement,movement.Position20,tolerance)!=0){
     i.At=0x05f971;if(b.IsMoving(role)!=0){Handler handler=Need(role.Handler24C0.Read(0x05f97a),0x05f981);Executor.Stop(handler,b,i);i.At=0x05f991;b.State450(role,1);}
     NotifyAssociated(role,b,i,0x05f997,0x05f99d,0x05f9a6,0x05f9c9);return;
    }
   }
   i.At=0x05f9e9;b.StopMoving(role,1,1);
  }
  public static void Stop(MoveActionRecord a,MoveBoundaries b,Invocation i){
   a.RequireLive(0x082eff);uint owner=a.OwnerId08.Read(0x082eff);i.At=0x082f05;object manager=b.ObjectManager();i.At=0x082f10;object value=b.Lookup(manager,owner);
   if(value!=null&&Accept(value,b.Context.CharacterClass,b,i,0x082f27,0x082f35,0x082f47)){
    MoveCharacter role=Character(value,0x082f54);a.RequireLive(0x082f54);uint reference=a.SkillReference7C.Read(0x082f54),flags=a.Flags78.Read(0x082f68);
    i.At=0x082f6c;uint id=b.SkillId(role,reference);i.At=0x082f73;manager=b.ObjectManager();i.At=0x082f7e;object skill=b.Lookup(manager,id);
    if(skill!=null&&Accept(skill,b.Context.ActivatedSkillClass,b,i,0x082f91,0x082f9f,0x082fad)){
     object context=role.SkillContext868.Read(0x082fbc);i.At=0x082fc6;b.Skill6B8(skill,context,flags);
    }
    if(role.SpeedFlag26B3.Read(0x082fcc)!=0){role.SpeedFlag26B3.Write(0);i.At=0x082fe4;b.ForceSpeed(role);}
    object sound=role.Sound2210.Read(0x082fe9);if(sound==null)sound=role.Sound2110.Read(0x082ff5);
    if(sound!=null){i.At=0x083004;if(b.SoundCheck(sound)!=0){uint soundId=role.SoundId22C8.Read(0x08300e);i.At=0x08301a;b.SoundStop(sound,soundId,0);}}
    InlineStop(role.MovementDC0.Read(0x083020),b,i,0x083027,0x08302d,0x083033,0x083039);
    a.RequireLive(0x08304a);reference=a.SkillReference7C.Read(0x08304a);i.At=0x08304d;id=b.SkillId(role,reference);
    if(id!=0){i.At=0x083074;b.VectorImport(role,new LocalVector3());}
   }
   a.RequireLive(0x08307f);a.Flag11.Write(0);
  }
 }
}
