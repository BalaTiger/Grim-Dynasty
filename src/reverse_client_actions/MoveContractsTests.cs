using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Isolated observations of recovered ordering. These fixtures do not implement
// native navigation, animation, object lifetime, coordinates, or game input.
public static class MoveContractsTests {
 static int assertions,scenarios;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static void Pass(Outcome outcome){Check(outcome.ReturnedNormally,"Unexpected stop "+outcome.Address.ToString("x")+" "+outcome.Boundary);}
 static void Missing(Outcome outcome,uint at){Check(!outcome.ReturnedNormally,"Unexpected normal return");Equal(at,outcome.Address,"Boundary address");}
 static void Unknown<T>(Field<T> field){try{field.Read(1);throw new Exception("Unknown became known");}catch(MissingContract){assertions++;}}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static Outcome Begin(F f){return MoveContracts.Run(delegate(Invocation i){MoveContracts.Begin(f.Action,f.B,i);});}
 static Outcome Stop(F f){return MoveContracts.Run(delegate(Invocation i){MoveContracts.Stop(f.Action,f.B,i);});}
 static Outcome Move(F f,float scalar,uint animation,float speed){return MoveContracts.Run(delegate(Invocation i){MoveContracts.Move(f.Role,f.Action.Destination60,scalar,animation,speed,f.B,i);});}
 static Outcome StopSkill(F f){return MoveContracts.Run(delegate(Invocation i){MoveContracts.StopCurrentSkill(f.Role,f.B,i);});}
 static bool Has(Probe b,string name){return b.Calls.Contains(name);}
 static int Count(Probe b,string name){int n=0;foreach(string s in b.Calls)if(s==name)n++;return n;}

 sealed class F {
  public readonly MoveContext Context=new MoveContext();
  public readonly MoveActionRecord Action=new MoveActionRecord();
  public readonly MoveCharacter Role=new MoveCharacter();
  public readonly MovementManager Movement=new MovementManager();
  public readonly ClassRecord CharacterClass=new ClassRecord(),SkillClass=new ClassRecord(),ActivatedClass=new ClassRecord();
  public readonly object Skill=new object(),Activated=new object(),World=new object(),SkillContext=new object();
  public readonly Handler Handler=new Handler();
  public readonly Probe B;
  public F(){
   Context.CharacterClass.Write(CharacterClass);Context.SkillClass.Write(SkillClass);Context.ActivatedSkillClass.Write(ActivatedClass);
   var engine=new WorldEngine();engine.World50.Write(World);Context.EngineGlobal.Write(engine);
   Context.AlreadyThereConstant7A7FD8.Write(0.025f);Context.NoNameImport609568.Write(new object());
   Action.OwnerId08.Write(11);Action.Flags78.Write(22);Action.SkillReference7C.Write(44);Action.Animation84.Write(7);Action.Speed80.Write(2.5f);Action.Scalar14.Write(250);Action.Flag11.Write(1);Action.PreAction18.Write(0);
   Action.Destination60.Q0.Write(101);Action.Destination60.Q8.Write(102);Action.Destination60.Q16.Write(103);
   Role.Gate26AF.Write(0);Role.SpeedFlag26B3.Write(0);Role.AssociatedId16C0.Write(21);Role.CurrentSkillA90.Write(33);Role.SkillContext868.Write(SkillContext);Role.AttackSkill26A8.Write(88);
   Role.Sound2210.Write(null);Role.Sound2110.Write(null);Role.SoundId22C8.Write(99);Role.MovementDC0.Write(Movement);
   Movement.Registered6C.Write(0);Movement.Moving38.Write(9);Movement.Owner00.Write(Role);
   Handler.Current08.Write(null);Handler.Busy18.Write(0);Handler.Count28.Write(0);Role.Handler24C0.Write(Handler);
   B=new Probe(Context);B.Objects.Add(11,Role);B.Classes.Add(Role,CharacterClass);B.Classes.Add(Skill,SkillClass);B.Classes.Add(Activated,ActivatedClass);
  }
 }

 sealed class Probe:MoveBoundaries {
  public readonly List<string> Calls=new List<string>();
  public readonly Dictionary<uint,object> Objects=new Dictionary<uint,object>();
  public readonly Dictionary<object,ClassRecord> Classes=new Dictionary<object,ClassRecord>();
  public Action<string,Probe> On=null;
  public object ManagerValue=new object(),AssociatedValue=null,RegionValue=new object(),NavigationValue=new object();
  public WorldPosition CoordsValue=new WorldPosition();
  public AnimationSet AnimationValue=new AnimationSet();
  public byte Can=1,Derives=0,Already=1,MoveAccepted=1,SkillCheck=1,ActivatedCheck=0,SoundValid=0,PlayAccepted=1,Moving=0;
  public uint SkillIdValue=0,StateValue=1;
  public float Distance=0,OldSpeed=2.5f;
  public bool UseDefaultMove=false;
  public object Arg0=null,Arg1=null,Arg2=null;
  public uint U0=0,U1=0;public float F0=0;public byte B0=0;
  public Probe(MoveContext c):base(c){AnimationValue.Kind08.Write(7);}
  void Hit(string name){Calls.Add(name);if(On!=null)On(name,this);}
  public override object ObjectManager(){Hit("manager");return ManagerValue;}
  public override object Lookup(object manager,uint id){Arg0=manager;U0=id;Hit("lookup");object value;return Objects.TryGetValue(id,out value)?value:null;}
  public override object Associated(object manager,uint id){Arg0=manager;U0=id;Hit("associated");return AssociatedValue;}
  public override ClassRecord Class0(object instance){Arg0=instance;Hit("class");return Classes[instance];}
  public override byte Derived(ClassRecord parent,ClassRecord expected){Arg0=parent;Arg1=expected;Hit("derived");return Derives;}
  public override byte CanMove460(MoveCharacter role){Arg0=role;Hit("can");return Can;}
  public override WorldPosition Coords(MoveCharacter role,WorldPosition buffer){Arg0=role;Arg1=buffer;Hit("coords");return CoordsValue;}
  public override object Region(WorldPosition position){Arg0=position;Hit("region");return RegionValue;}
  public override void Associated60(object associated,uint flags){Arg0=associated;U0=flags;Hit("associated60");}
  public override void Associated80(object associated){Arg0=associated;Hit("associated80");}
  public override float DistanceSquared(object world,WorldPosition a,WorldPosition b){Arg0=world;Arg1=a;Arg2=b;Hit("distance");return Distance;}
  public override void SetPathPosition(MoveCharacter role,WorldPosition position){Arg0=role;Arg1=position;Hit("path-position");}
  public override uint SkillId(MoveCharacter role,uint reference){Arg0=role;U0=reference;Hit("skill-id");return SkillIdValue;}
  public override byte Skill5A8(object skill){Arg0=skill;Hit("skill5a8");return SkillCheck;}
  public override void Skill498(object skill,object context){Arg0=skill;Arg1=context;Hit("skill498");}
  public override byte Skill6B0(object skill,object context){Arg0=skill;Arg1=context;Hit("skill6b0");return ActivatedCheck;}
  public override void Skill6B8(object skill,object context,uint flags){Arg0=skill;Arg1=context;U0=flags;Hit("skill6b8");}
  public override void ForceSpeed(MoveCharacter role){Arg0=role;Hit("force-speed");}
  public override uint State458(MoveCharacter role){Arg0=role;Hit("state");return StateValue;}
  public override void State450(MoveCharacter role,uint state){Arg0=role;U0=state;Hit("set-state");}
  public override void RunningSound(MoveCharacter role){Arg0=role;Hit("running-sound");}
  public override byte AlreadyThere(MovementManager manager,WorldPosition position,float tolerance){Arg0=manager;Arg1=position;F0=tolerance;Hit("already");return Already;}
  public override byte MoveManager(MovementManager manager,WorldPosition position){Arg0=manager;Arg1=position;Hit("move-manager");if(UseDefaultMove)return base.MoveManager(manager,position);return MoveAccepted;}
  public override void StopManager(MovementManager manager){Arg0=manager;Hit("stop-manager");}
  public override object Navigation(){Hit("navigation");return NavigationValue;}
  public override void StopObject(object navigation,object owner){Arg0=navigation;Arg1=owner;Hit("stop-object");}
  public override AnimationSet Animations(MoveCharacter role){Arg0=role;Hit("animations");return AnimationValue;}
  public override float AnimationSpeed(MoveCharacter role,uint index){Arg0=role;U0=index;Hit("animation-speed");return OldSpeed;}
  public override byte Play(AnimationSet set,MoveCharacter role,uint animation,object name,float speed,byte loop,uint extra){Arg0=set;Arg1=role;Arg2=name;U0=animation;U1=extra;F0=speed;B0=loop;Hit("play");return PlayAccepted;}
  public override byte IsMoving(MoveCharacter role){Arg0=role;Hit("is-moving");return Moving;}
  public override void StopMoving(MoveCharacter role,byte first,byte second){Arg0=role;B0=first;U0=second;Hit("stop-moving");}
  public override byte SoundCheck(object sound){Arg0=sound;Hit("sound-check");return SoundValid;}
  public override void SoundStop(object sound,uint id,uint zero){Arg0=sound;U0=id;U1=zero;Hit("sound-stop");}
  public override void VectorImport(MoveCharacter role,LocalVector3 value){Arg0=role;Arg1=value;Hit("vector");}
  public override void Stop18(ActionRecord action){Arg0=action;Hit("stop18");if(action is MoveActionRecord)base.Stop18(action);}
  public override void Destroy0(ActionRecord action,uint flags){Arg0=action;U0=flags;Hit("destroy");var a=action as MoveActionRecord;if(a!=null)a.MarkReleased();}
 }

 static void RunAll(){
  Test("fixed native class address rejects replacement identity",delegate{
   var f=new F();f.Context.CharacterClass.Write(f.CharacterClass);
   bool rejected=false;try{f.Context.CharacterClass.Write(new ClassRecord());}catch(MissingContract){rejected=true;}
   Check(rejected,"LEA class symbol cannot become a mutable global pointer");Same(f.CharacterClass,f.Context.CharacterClass.Read(1),"Original symbol identity retained");
  });
  Test("unknown owner fails before singleton",delegate{
   var f=new F();f.Action.OwnerId08.Forget();Missing(Begin(f),0x082c0f);Equal(0,f.B.Calls.Count,"No external reads");
  });
  Test("default start remains unsupported and executor keeps failure prefix",delegate{
   var f=new F();var b=new MoveBoundaries(f.Context);var budget=new Counter(2);
   Outcome result=Executor.Run(f.Handler,f.Action,budget,b);Missing(result,0x609098);
   Same(f.Action,f.Handler.Current08.Read(1),"Published same action");Equal((byte)1,f.Handler.Busy18.Read(1),"Busy not finally cleared");Equal(1,budget.Value.Read(1),"One budget consumed");
  });
  Test("missing owner calls action stop without destroying published action",delegate{
   var f=new F();f.B.Objects.Remove(11);var budget=new Counter(2);Pass(Executor.Run(f.Handler,f.Action,budget,f.B));
   Equal(1,Count(f.B,"stop18"),"One stop");Equal(2,Count(f.B,"lookup"),"Stop performs its own lookup");
   Check(!Has(f.B,"destroy"),"No destruction");Same(f.Action,f.Handler.Current08.Read(1),"Current identity retained");Equal((byte)0,f.Handler.Busy18.Read(1),"Normal start clears executor busy");Equal((byte)0,f.Action.Flag11.Read(1),"Stop tail clears only action flag");
  });
  Test("initial cannot-move follows stop path without target production",delegate{
   var f=new F();f.B.Can=0;Pass(Begin(f));Equal(1,Count(f.B,"stop18"),"Stop called");Check(!Has(f.B,"distance"),"No distance query");Check(!Has(f.B,"move-manager"),"No mover query");Equal(88u,f.Role.AttackSkill26A8.Read(1),"Target unchanged");
  });
  Test("direct parent class accepted without unknown walk",delegate{
   var f=new F();var child=new ClassRecord();child.Parent10.Write(f.CharacterClass);f.B.Classes[f.Role]=child;Pass(Begin(f));Check(!Has(f.B,"derived"),"No inheritance walker");
  });
  Test("unknown class parent is not a false class check",delegate{
   var f=new F();f.B.Classes[f.Role]=new ClassRecord();Missing(Begin(f),0x082c45);Check(!Has(f.B,"stop18"),"Unknown does not trigger normal failure");
  });
  Test("start preserves returned pointers hidden buffer and late world",delegate{
   var f=new F();var first=new WorldPosition();var second=new WorldPosition();object hidden=null;var world2=new object();var associated=new object();f.B.AssociatedValue=associated;
   f.B.On=delegate(string n,Probe p){
    if(n=="manager"&&Count(p,n)==2)f.Role.AssociatedId16C0.Write(555);
    if(n=="associated"&&Count(p,n)==1)Equal(21u,p.U0,"Associated ID captured before singleton");
    if(n=="associated60"){Same(associated,p.Arg0,"Associated identity");Equal(22u,p.U0,"Flags read at notification");f.Action.Flags78.Write(66);}
    if(n=="coords"){
     if(Count(p,n)==1){hidden=p.Arg1;p.CoordsValue=first;}else{Same(hidden,p.Arg1,"Same hidden return buffer reused");p.CoordsValue=second;var engine=new WorldEngine();engine.World50.Write(world2);f.Context.EngineGlobal.Write(engine);}
    }
    if(n=="region"&&Count(p,n)==1)Same(first,p.Arg0,"Region uses returned pointer");
    if(n=="distance"){Same(world2,p.Arg0,"World read after coords callback");Same(second,p.Arg1,"Distance uses second returned pointer");Same(f.Action.Start48,p.Arg2,"Inline start identity");}
   };
   Pass(Begin(f));Equal(66u,f.Role.AttackFlags2688.Read(1),"Target reads mutated flags");Equal(2,Count(f.B,"coords"),"Two coordinate calls");
  });
  Test("distance comparison keeps unordered and exact threshold behavior",delegate{
   float[] values={Single.NaN,100.0f,100.25f,Single.PositiveInfinity};bool[] correction={false,false,true,true};
   for(int k=0;k<values.Length;k++){var f=new F();f.B.Distance=values[k];Pass(Begin(f));Equal(correction[k],Has(f.B,"path-position"),"Ordered greater-than only");}
  });
  Test("distance callback failure retains exact boundary and untouched target",delegate{
   var f=new F();f.B.On=delegate(string n,Probe p){if(n=="distance")throw new InvalidOperationException("fixture");};
   Outcome r=Begin(f);Missing(r,0x082cf8);Check(r.Exception.InnerException is InvalidOperationException,"Inner failure retained");Equal(88u,f.Role.AttackSkill26A8.Read(1),"No target write after failure");
  });
  Test("stop skill captures ID before singleton and null preserves it",delegate{
   var f=new F();f.B.On=delegate(string n,Probe p){if(n=="manager")f.Role.CurrentSkillA90.Write(999);if(n=="lookup")Equal(33u,p.U0,"Snapshot ID");};
   Pass(StopSkill(f));Equal(999u,f.Role.CurrentSkillA90.Read(1),"Null lookup does not clear current");
  });
  Test("stop skill wrong class does not clear ID",delegate{
   var f=new F();f.B.Objects[33]=f.Skill;var wrong=new ClassRecord();wrong.Parent10.Write(null);f.B.Classes[f.Skill]=wrong;
   Pass(StopSkill(f));Equal(33u,f.Role.CurrentSkillA90.Read(1),"Wrong class leaves ID");Check(!Has(f.B,"skill5a8"),"No virtual call");
  });
  Test("stop skill clears before callback preserves republish and reads late context",delegate{
   var f=new F();f.B.Objects[33]=f.Skill;object late=new object();
   f.B.On=delegate(string n,Probe p){if(n=="skill5a8"){Equal(0u,f.Role.CurrentSkillA90.Read(1),"Cleared before virtual");f.Role.CurrentSkillA90.Write(777);f.Role.SkillContext868.Write(late);}if(n=="skill498"){Same(f.Skill,p.Arg0,"Same skill");Same(late,p.Arg1,"Late context");}};
   Pass(StopSkill(f));Equal(777u,f.Role.CurrentSkillA90.Read(1),"Republished value retained");
  });
  Test("stop skill virtual failure leaves prior clear",delegate{
   var f=new F();f.B.Objects[33]=f.Skill;f.B.On=delegate(string n,Probe p){if(n=="skill5a8")throw new Exception("fixture");};
   Missing(StopSkill(f),0x5345a6);Equal(0u,f.Role.CurrentSkillA90.Read(1),"Clear is not rolled back");Check(!Has(f.B,"skill498"),"No later call");
  });
  Test("target copies unknown words and retains old skill on failure",delegate{
   var f=new F();f.Action.Destination60.Q8.Forget();f.B.On=delegate(string n,Probe p){if(n=="skill-id"){Equal(55u,p.U0,"Incoming reference snapshot");throw new Exception("fixture");}};
   Outcome r=MoveContracts.Run(delegate(Invocation i){MoveContracts.SetAttackTarget(f.Role,0xffffffff,f.Action.Destination60,55,f.B,i);});
   Missing(r,0x05e6a2);Equal(0xffffffffu,f.Role.AttackFlags2688.Read(1),"DWORD flags");Equal(101UL,f.Role.AttackPosition2690.Q0.Read(1),"Known first word copied");Unknown(f.Role.AttackPosition2690.Q8);Equal(103UL,f.Role.AttackPosition2690.Q16.Read(1),"Tail copied");Equal(88u,f.Role.AttackSkill26A8.Read(1),"Old target ID retained");
  });
  Test("target overlap observes 16-byte copy before tail load",delegate{
   var f=new F();var target=f.Role.AttackPosition2690;target.Q0.Write(1);target.Q8.Write(2);target.Q16.Write(3);
   // Restricted overlap fixture: source starts at target-8 (2688), so its
   // first word overlaps the flags DWORD. Initial ulong4 and incoming flags4
   // make that write value-preserving. General DWORD aliasing is not modeled.
   var source=new WorldPosition(Field<ulong>.Of(4),target.Q0,target.Q8);f.B.SkillIdValue=99;
   Pass(MoveContracts.Run(delegate(Invocation i){MoveContracts.SetAttackTarget(f.Role,4,source,55,f.B,i);}));
   Equal(4UL,target.Q0.Read(1),"First word after value-preserving flags write");Equal(1UL,target.Q8.Read(1),"Second from original first");Equal(1UL,target.Q16.Read(1),"Tail reads changed source, not early snapshot2");Equal(99u,f.Role.AttackSkill26A8.Read(1),"ID stored after callback");
  });
  Test("target null source keeps first flags write only",delegate{
   var f=new F();Missing(MoveContracts.Run(delegate(Invocation i){MoveContracts.SetAttackTarget(f.Role,5,null,55,f.B,i);}),0x05e67f);
   Equal(5u,f.Role.AttackFlags2688.Read(1),"Flags prior to dereference");Equal(88u,f.Role.AttackSkill26A8.Read(1),"No ID overwrite");Check(!Has(f.B,"skill-id"),"No helper call");
  });
  Test("invalid region inline stop captures manager but reads owner after singleton",delegate{
   var f=new F();f.B.RegionValue=null;f.Movement.Registered6C.Write(1);object lateOwner=new object();var replacement=new MovementManager();replacement.Moving38.Write(6);
   f.B.On=delegate(string n,Probe p){if(n=="navigation"){f.Role.MovementDC0.Write(replacement);f.Movement.Owner00.Write(lateOwner);}if(n=="stop-object"){Same(lateOwner,p.Arg1,"Late owner");Equal((byte)9,f.Movement.Moving38.Read(1),"Clear follows stop import");}};
   Pass(Move(f,Single.NaN,7,2.5f));Equal((byte)0,f.Movement.Moving38.Read(1),"Captured manager cleared");Equal((byte)6,replacement.Moving38.Read(1),"Replacement not cleared");Check(!Has(f.B,"can"),"Invalid region short circuits role gate");
  });
  Test("inline stop failure leaves moving byte unchanged",delegate{
   var f=new F();f.B.RegionValue=null;f.Movement.Registered6C.Write(1);f.B.On=delegate(string n,Probe p){if(n=="stop-object")throw new Exception("fixture");};
   Missing(Move(f,0,7,2.5f),0x05fac9);Equal((byte)9,f.Movement.Moving38.Read(1),"No finally clear");
  });
  Test("AlreadyThere refreshes manager and reads associated ID after singleton",delegate{
   var f=new F();var fresh=new MovementManager();fresh.Registered6C.Write(0);fresh.Moving38.Write(5);var associated=new object();f.B.AssociatedValue=associated;
   f.B.On=delegate(string n,Probe p){if(n=="already"){Same(f.Movement,p.Arg0,"First manager");Equal(0.025f,p.F0,"Explicit tolerance, not scalar");f.Role.MovementDC0.Write(fresh);}if(n=="manager")f.Role.AssociatedId16C0.Write(456);if(n=="associated")Equal(456u,p.U0,"Late ID");if(n=="associated80")Same(associated,p.Arg0,"Associated identity");};
   Pass(Move(f,-999,7,2.5f));Equal((byte)9,f.Movement.Moving38.Read(1),"Old manager unchanged");Equal((byte)0,fresh.Moving38.Read(1),"Fresh manager stopped");
  });
  Test("native movement dependency remains explicit",delegate{
   var f=new F();f.B.Already=0;f.B.UseDefaultMove=true;Missing(Move(f,0,7,2.5f),0x08eaf0);Check(!Has(f.B,"animations"),"Unknown move cannot manufacture animation success");
  });
  Test("unordered speeds skip replay only when state and captured animation match",delegate{
   float[] old={Single.NaN,2.5f,Single.NaN};float[] requested={2.5f,Single.NaN,Single.NaN};
   for(int j=0;j<old.Length;j++){var f=new F();f.B.Already=0;f.B.StateValue=5;f.B.OldSpeed=old[j];f.B.On=delegate(string n,Probe p){if(n=="animation-speed")p.AnimationValue.Kind08.Write(999);};Pass(Move(f,Single.NaN,7,requested[j]));Check(!Has(f.B,"play"),"NaN accepts captured animation match");Check(!Has(f.B,"set-state"),"No state change");}
  });
  Test("different animation still replays with NaN speed and exact arguments",delegate{
   var f=new F();f.B.Already=0;f.B.StateValue=5;f.B.OldSpeed=Single.NaN;f.B.AnimationValue.Kind08.Write(8);object name=f.Context.NoNameImport609568.Read(1);
   f.B.On=delegate(string n,Probe p){if(n=="play"){Same(f.Role,p.Arg1,"Role identity");Same(name,p.Arg2,"Imported no-name identity");Equal(7u,p.U0,"Requested animation");Equal((byte)1,p.B0,"Loop byte");Equal(0u,p.U1,"Trailing DWORD");Equal(2.5f,p.F0,"Requested speed");}};
   Pass(Move(f,999,7,2.5f));Equal(1,Count(f.B,"play"),"One replay");
  });
  Test("animation failure stops current manager then restores state",delegate{
   var f=new F();f.B.Already=0;f.B.PlayAccepted=0;var fresh=new MovementManager();var sequence=new List<uint>();
   f.B.On=delegate(string n,Probe p){if(n=="play")f.Role.MovementDC0.Write(fresh);if(n=="stop-manager")Same(fresh,p.Arg0,"Manager refreshed after Play");if(n=="set-state")sequence.Add(p.U0);};
   Pass(Move(f,0,7,2.5f));Equal(2,sequence.Count,"Two state calls");Equal(5u,sequence[0],"Enter moving");Equal(1u,sequence[1],"Restore after stop");
  });
  Test("failed move refreshes manager between region and fallback distance",delegate{
   var f=new F();f.B.Already=0;f.B.MoveAccepted=0;var fresh=new MovementManager();
   f.B.On=delegate(string n,Probe p){if(n=="region"&&Count(p,n)==2){Same(f.Movement.Position20,p.Arg0,"Current target region");f.Role.MovementDC0.Write(fresh);}if(n=="already"&&Count(p,n)==2){Same(fresh,p.Arg0,"Refreshed manager");Same(fresh.Position20,p.Arg1,"Position belongs to refreshed manager");p.Already=1;}};
   Pass(Move(f,0,7,2.5f));Check(!Has(f.B,"stop-moving"),"Fallback already-there avoids full stop");
  });
  Test("failed move missing region calls StopMoving with both true",delegate{
   var f=new F();f.B.Already=0;f.B.MoveAccepted=0;
   f.B.On=delegate(string n,Probe p){if(n=="region"&&Count(p,n)==2)p.RegionValue=null;if(n=="stop-moving"){Equal((byte)1,p.B0,"First true");Equal(1u,p.U0,"Second true");}};
   Pass(Move(f,0,7,2.5f));Check(Has(f.B,"stop-moving"),"StopMoving reached");Check(!Has(f.B,"associated"),"No unrelated notification");
  });
  Test("fallback Handler.Stop rereads current after stop callback",delegate{
   var f=new F();f.B.Already=0;f.B.MoveAccepted=0;f.B.Moving=1;var old=new ActionRecord();var replacement=new ActionRecord();f.Handler.Current08.Write(old);
   f.B.On=delegate(string n,Probe p){if(n=="already"&&Count(p,n)==2)p.Already=1;if(n=="stop18"){Same(old,p.Arg0,"Old stop target");f.Handler.Current08.Write(replacement);}if(n=="destroy"){Same(replacement,p.Arg0,"Late destruction target");Equal(1u,p.U0,"Destructor flag");}};
   Pass(Move(f,0,7,2.5f));Same(null,f.Handler.Current08.Read(1),"Handler current cleared");Check(Has(f.B,"set-state"),"State restored after stop");
  });
  Test("start rereads skill reference after move and publishes speed flag before update",delegate{
   var f=new F();f.B.Objects[77]=f.Activated;f.B.ActivatedCheck=1;f.B.StateValue=5;
   f.B.On=delegate(string n,Probe p){if(n=="already")f.Action.SkillReference7C.Write(77);if(n=="skill-id")p.SkillIdValue=p.U0==77?77u:0u;if(n=="force-speed")Equal((byte)1,f.Role.SpeedFlag26B3.Read(1),"Flag published before update");};
   Pass(Begin(f));Equal(2,Count(f.B,"skill-id"),"Independent target and late skill queries");Check(Has(f.B,"skill6b0"),"Fresh skill activated");Check(Has(f.B,"running-sound"),"State5 tail sound");
  });
  Test("inactive skill does not clear existing speed flag",delegate{
   var f=new F();f.Role.SpeedFlag26B3.Write(1);f.B.Objects[44]=f.Activated;f.B.SkillIdValue=44;f.B.ActivatedCheck=0;Pass(Begin(f));
   Equal((byte)1,f.Role.SpeedFlag26B3.Read(1),"No invented else clear");Check(!Has(f.B,"force-speed"),"No update");
  });
  Test("unknown scalar read remains required after target writes",delegate{
   var f=new F();f.Action.Scalar14.Forget();f.B.SkillIdValue=123;Missing(Begin(f),0x0819e0);
   Equal(123u,f.Role.AttackSkill26A8.Read(1),"Target completed before getter");Check(!Has(f.B,"already"),"Move body not started");
  });
  Test("stop captures flags early but context and second skill reference late",delegate{
   var f=new F();f.B.Objects[55]=f.Activated;var late=new object();f.Movement.Registered6C.Write(1);
   f.B.On=delegate(string n,Probe p){
    if(n=="skill-id"){if(Count(p,n)==1){Equal(44u,p.U0,"Initial reference");f.Action.Flags78.Write(999);f.Role.SkillContext868.Write(late);p.SkillIdValue=55;}else{Equal(777u,p.U0,"Second reference refreshed");p.SkillIdValue=1;}}
    if(n=="skill6b8"){Equal(22u,p.U0,"Flags captured before first helper");Same(late,p.Arg1,"Late context");}
    if(n=="stop-object")f.Action.SkillReference7C.Write(777);
    if(n=="vector"){var v=(LocalVector3)p.Arg1;Equal(0u,v.X,"ZeroX");Equal(0u,v.Y,"ZeroY");Equal(0u,v.Z,"ZeroZ");Equal((byte)1,f.Action.Flag11.Read(1),"Tail flag remains during import");}
   };
   Pass(Stop(f));Equal(2,Count(f.B,"skill-id"),"Two helper calls");Equal((byte)0,f.Action.Flag11.Read(1),"Tail clear");
  });
  Test("stop sound preference retains identity and reads ID after check",delegate{
   var f=new F();object primary=new object(),fallback=new object();f.Role.Sound2210.Write(primary);f.Role.Sound2110.Write(fallback);f.B.SoundValid=1;
   f.B.On=delegate(string n,Probe p){if(n=="sound-check"){Same(primary,p.Arg0,"Primary selected");f.Role.Sound2210.Write(null);f.Role.SoundId22C8.Write(700);}if(n=="sound-stop"){Same(primary,p.Arg0,"Captured primary identity");Equal(700u,p.U0,"Late sound ID");Equal(0u,p.U1,"Trailing zero");}};
   Pass(Stop(f));Equal(1,Count(f.B,"sound-check"),"No fallback reselect");
  });
  Test("stop speed failure preserves clear but not final action flag",delegate{
   var f=new F();f.Role.SpeedFlag26B3.Write(1);f.B.On=delegate(string n,Probe p){if(n=="force-speed")throw new Exception("fixture");};
   Missing(Stop(f),0x082fe4);Equal((byte)0,f.Role.SpeedFlag26B3.Read(1),"Prior byte clear retained");Equal((byte)1,f.Action.Flag11.Read(1),"No finally tail clear");Equal((byte)9,f.Movement.Moving38.Read(1),"Later manager untouched");
  });
  Test("stop vector failure preserves manager prefix and original flag",delegate{
   var f=new F();f.B.On=delegate(string n,Probe p){if(n=="skill-id"&&Count(p,n)==2)p.SkillIdValue=1;if(n=="vector")throw new Exception("fixture");};
   Missing(Stop(f),0x083074);Equal((byte)0,f.Movement.Moving38.Read(1),"Manager clear precedes vector");Equal((byte)1,f.Action.Flag11.Read(1),"Final flag untouched after failure");
  });
  Test("released shared action is not silently reused after movement callback",delegate{
   var f=new F();f.B.On=delegate(string n,Probe p){if(n=="already")f.Action.MarkReleased();};
   Outcome r=Begin(f);Check(!r.ReturnedNormally,"Released action must stop before late reference read");Equal(1,Count(f.B,"skill-id"),"No post-move skill lookup after invalidation");
  });
 }
 public static int Main(){try{RunAll();Console.WriteLine("PASS "+scenarios+" scenarios, "+assertions+" assertions; isolated contracts only, no native gameplay validation");return 0;}catch(Exception x){Console.Error.WriteLine("FAIL after "+scenarios+" scenarios / "+assertions+" assertions: "+x);return 1;}}
}
