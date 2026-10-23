using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New extension tests only. External observations are explicit fixture data,
// not production movement, navigation, or a native-runtime comparison.
public static class MoveManagerContractsTests {
 static int scenarios,assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static void Pass(Outcome r){Check(r.ReturnedNormally,"Unexpected stop "+r.Address.ToString("x")+" "+r.Boundary);}
 static void Missing(Outcome r,uint address){Check(!r.ReturnedNormally,"Unexpected success");Equal(address,r.Address,"Failure address");}
 static void Unknown<T>(Field<T> f){try{f.Read(1);throw new Exception("Expected unknown field");}catch(MissingContract){assertions++;}}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static int Count(Probe p,string name){int n=0;foreach(string s in p.Calls)if(s==name)n++;return n;}
 static byte Calc(F f,float tolerance){return MoveManagerContracts.AlreadyThere(f.Manager,f.Destination,tolerance,f.B,new Invocation());}
 static Outcome SetPath(F f){return MoveContracts.Run(delegate(Invocation i){MoveManagerContracts.SetPathPosition(f.Role,f.Destination,f.B,i);});}
 static Outcome Stop(F f){return MoveContracts.Run(delegate(Invocation i){MoveManagerContracts.Stop(f.Manager,f.B,i);});}
 static Outcome Move(F f){return MoveContracts.Run(delegate(Invocation i){MoveContracts.Move(f.Role,f.Destination,999,7,2.5f,f.B,i);});}

 sealed class F {
  public readonly MoveContext Context=new MoveContext();
  public readonly MoveCharacter Role=new MoveCharacter();
  public readonly MovementManager Manager=new MovementManager();
  public readonly WorldPosition Destination=new WorldPosition();
  public readonly object World=new object(),Owner=new object();
  public readonly Probe B;
  public F(){
   var engine=new WorldEngine();engine.World50.Write(World);Context.EngineGlobal.Write(engine);
   Context.AlreadyThereConstant7A7FD8.Write(0.025f);Context.NoNameImport609568.Write(new object());
   Manager.Owner00.Write(Owner);Manager.Registered6C.Write(0);Manager.Moving38.Write(7);Role.MovementDC0.Write(Manager);Role.AssociatedId16C0.Write(123);
   Destination.Q0.Write(10);Destination.Q8.Write(20);Destination.Q16.Write(30);
   B=new Probe(Context);
  }
 }
 sealed class Probe:MoveManagerBoundaries {
  public readonly List<string> Calls=new List<string>();
  public readonly List<uint> States=new List<uint>();
  public Action<string,Probe> On=null;
  public object Arg0=null,Arg1=null,Arg2=null;
  public readonly object NavigationValue=new object(),ObjectManagerValue=new object(),RegionValue=new object();
  public WorldPosition CoordsReturn=new WorldPosition();
  public readonly AnimationSet AnimationValue=new AnimationSet();
  public float X=0,Y=0,Z=0;
  public bool WriteX=true,WriteY=true,WriteZ=true,PermitObservedMove=false;
  public Probe(MoveContext c):base(c){AnimationValue.Kind08.Write(1);}
  void Hit(string name){Calls.Add(name);if(On!=null)On(name,this);}
  public override WorldPosition OwnerCoords(object owner,WorldPosition buffer){Arg0=owner;Arg1=buffer;Hit("owner-coords");return CoordsReturn;}
  public override void Difference609A48(WorldPosition destination,Displacement3 output,WorldPosition coordinates){Arg0=destination;Arg1=output;Arg2=coordinates;Hit("difference");if(WriteX)output.X0.Write(X);if(WriteY)output.Y4.Write(Y);if(WriteZ)output.Z8.Write(Z);}
  public override WorldPosition Coords(MoveCharacter role,WorldPosition buffer){Arg0=role;Arg1=buffer;Hit("coords");return CoordsReturn;}
  public override void Reposition609BB8(object world,MoveCharacter role,WorldPosition position){Arg0=world;Arg1=role;Arg2=position;Hit("reposition");}
  public override object Navigation(){Hit("navigation");return NavigationValue;}
  public override void StopObject(object navigation,object owner){Arg0=navigation;Arg1=owner;Hit("stop-object");}
  public override void Notify609F68(object navigation,object owner){Arg0=navigation;Arg1=owner;Hit("notify-position");}
  public override object Region(WorldPosition position){Arg0=position;Hit("region");return RegionValue;}
  public override byte CanMove460(MoveCharacter role){Arg0=role;Hit("can-move");return 1;}
  public override object ObjectManager(){Hit("object-manager");return ObjectManagerValue;}
  public override object Associated(object manager,uint id){Arg0=manager;Hit("associated");return null;}
  public override byte MoveManager(MovementManager manager,WorldPosition position){Arg0=manager;Arg1=position;Hit("move-manager");if(PermitObservedMove)return 1;return base.MoveManager(manager,position);}
  public override AnimationSet Animations(MoveCharacter role){Arg0=role;Hit("animations");return AnimationValue;}
  public override float AnimationSpeed(MoveCharacter role,uint index){Arg0=role;Hit("animation-speed");return 1;}
  public override uint State458(MoveCharacter role){Arg0=role;Hit("state");return 1;}
  public override void State450(MoveCharacter role,uint state){Arg0=role;States.Add(state);Hit("set-state");}
  public override byte Play(AnimationSet set,MoveCharacter role,uint animation,object name,float speed,byte loop,uint extra){Arg0=set;Arg1=role;Hit("play");return 0;}
 }

 static void RunAll(){
  Test("squared threshold is inclusive and keeps signed tolerance",delegate{
   var f=new F();f.B.X=3;f.B.Y=4;Equal((byte)1,Calc(f,5),"Exact boundary");Equal((byte)0,Calc(f,4.999f),"Below distance");Equal((byte)1,Calc(f,-5),"Negative tolerance is squared");
   f.B.X=0;f.B.Y=0;Equal((byte)1,Calc(f,0),"Zero distance and threshold");
  });
  Test("unordered comparison rejects NaN while infinities follow COMISS",delegate{
   var f=new F();f.B.X=Single.NaN;Equal((byte)0,Calc(f,1),"NaN distance");f.B.X=0;Equal((byte)0,Calc(f,Single.NaN),"NaN tolerance");
   f.B.X=Single.PositiveInfinity;Equal((byte)0,Calc(f,1),"Infinite distance finite threshold");Equal((byte)1,Calc(f,Single.PositiveInfinity),"Positive infinity equals positive infinity");
  });
  Test("binary32 rounding and x+y then z order affect result",delegate{
   var f=new F();f.B.X=4096;f.B.Y=1;f.B.Z=1;
   Equal((byte)1,Calc(f,4096),"Each add rounds tie back to 16777216; double accumulation would fail");
   f.B.X=1;f.B.Y=1;f.B.Z=4096;
   Equal((byte)0,Calc(f,4096),"Small terms sum before large term yields 16777218");
  });
  Test("AlreadyThere uses returned coordinates identity and captured owner",delegate{
   var f=new F();object hidden=null;var returned=new WorldPosition();object lateOwner=new object();f.B.CoordsReturn=returned;
   f.B.On=delegate(string n,Probe p){
    if(n=="owner-coords"){Same(f.Owner,p.Arg0,"Owner captured before callback");hidden=p.Arg1;f.Manager.Owner00.Write(lateOwner);}
    if(n=="difference"){Same(f.Destination,p.Arg0,"Original destination identity");Same(returned,p.Arg2,"Uses returned pointer, not hidden buffer");Check(!Object.ReferenceEquals(hidden,p.Arg2),"Fixture differentiates pointers");}
   };
   Equal((byte)1,Calc(f,0),"Known zero displacement");Same(lateOwner,f.Manager.Owner00.Read(1),"Callback write retained");
  });
  Test("missing difference output stops at first unobserved scalar in read order",delegate{
   var f=new F();f.B.WriteY=false;f.B.WriteZ=false;
   Missing(MoveContracts.Run(delegate(Invocation i){MoveManagerContracts.AlreadyThere(f.Manager,f.Destination,1,f.B,i);}),0x08e1a9);
   var first=new F();first.B.WriteX=false;
   Missing(MoveContracts.Run(delegate(Invocation i){MoveManagerContracts.AlreadyThere(first.Manager,first.Destination,1,first.B,i);}),0x08e1a3);
   Equal((byte)7,f.Manager.Moving38.Read(1),"Read-only predicate preserves moving byte");
  });
  Test("difference failure has exact call boundary and no invented zero vector",delegate{
   var f=new F();f.B.On=delegate(string n,Probe p){if(n=="difference")throw new InvalidOperationException("fixture failure");};
   Outcome r=MoveContracts.Run(delegate(Invocation i){MoveManagerContracts.AlreadyThere(f.Manager,f.Destination,1,f.B,i);});
   Missing(r,0x08e19d);Check(r.Exception.InnerException is InvalidOperationException,"Original failure retained");Equal((byte)7,f.Manager.Moving38.Read(1),"No predicate state change");
  });
  Test("SetPath ignores returned pointer and copies into hidden buffer preserving unknown",delegate{
   var f=new F();WorldPosition hidden=null;WorldPosition returned=f.B.CoordsReturn;returned.Q0.Write(999);f.Destination.Q8.Forget();
   f.B.On=delegate(string n,Probe p){
    if(n=="coords"){hidden=(WorldPosition)p.Arg1;hidden.Q0.Write(111);hidden.Q8.Write(222);hidden.Q16.Write(333);}
    if(n=="reposition"){Same(hidden,p.Arg2,"Hidden buffer passed to world");Check(!Object.ReferenceEquals(returned,p.Arg2),"Returned pointer deliberately ignored");var v=(WorldPosition)p.Arg2;Equal(10UL,v.Q0.Read(1),"First word copied");Unknown(v.Q8);Equal(30UL,v.Q16.Read(1),"Tail copied");}
   };
   Pass(SetPath(f));Equal(999UL,returned.Q0.Read(1),"Return object not overwritten");Equal((byte)0,f.Manager.Moving38.Read(1),"Manager clear follows reposition");
  });
  Test("SetPath reads world and manager late then retains manager across navigation",delegate{
   var f=new F();object lateWorld=new object(),lateOwner=new object();var current=new MovementManager();current.Registered6C.Write(1);current.Moving38.Write(8);current.Owner00.Write(f.Owner);var newest=new MovementManager();newest.Moving38.Write(9);
   f.B.On=delegate(string n,Probe p){
    if(n=="coords"){var engine=new WorldEngine();engine.World50.Write(lateWorld);f.Context.EngineGlobal.Write(engine);}
    if(n=="reposition"){Same(lateWorld,p.Arg0,"World read after coordinates");Same(f.Role,p.Arg1,"Same role");f.Role.MovementDC0.Write(current);}
    if(n=="navigation"){current.Owner00.Write(lateOwner);f.Role.MovementDC0.Write(newest);}
    if(n=="notify-position"){Same(f.B.NavigationValue,p.Arg0,"Navigation identity");Same(lateOwner,p.Arg1,"Late owner on captured manager");Equal((byte)8,current.Moving38.Read(1),"Clear not before notify");}
   };
   Pass(SetPath(f));Equal(1,Count(f.B,"notify-position"),"609F68 notification");Equal(0,Count(f.B,"stop-object"),"609F50 stop not substituted");Equal((byte)0,current.Moving38.Read(1),"Captured manager cleared");Equal((byte)9,newest.Moving38.Read(1),"Later manager unaffected");Equal((byte)7,f.Manager.Moving38.Read(1),"Earlier manager unaffected");
  });
  Test("SetPath reposition failure retains copied buffer but does not clear manager",delegate{
   var f=new F();WorldPosition retained=null;
   f.B.On=delegate(string n,Probe p){if(n=="reposition"){retained=(WorldPosition)p.Arg2;throw new Exception("fixture");}};
   Missing(SetPath(f),0x05d9e4);Equal(10UL,retained.Q0.Read(1),"Copied prefix retained");Equal(30UL,retained.Q16.Read(1),"Tail copied before failure");Equal((byte)7,f.Manager.Moving38.Read(1),"No later clear");Equal(0,Count(f.B,"navigation"),"No navigation after failure");
  });
  Test("SetPath notify failure does not finally clear moving byte",delegate{
   var f=new F();f.Manager.Registered6C.Write(1);f.B.On=delegate(string n,Probe p){if(n=="notify-position")throw new Exception("fixture");};
   Missing(SetPath(f),0x05da03);Equal((byte)7,f.Manager.Moving38.Read(1),"Clear not reached");Equal(1,Count(f.B,"reposition"),"World operation already returned");Equal(0,Count(f.B,"stop-object"),"No substitute stop");
  });
  Test("Stop unregistered manager clears byte without reading unknown owner",delegate{
   var f=new F();f.Manager.Owner00.Forget();Pass(Stop(f));Equal((byte)0,f.Manager.Moving38.Read(1),"Clear moving");Equal((byte)0,f.Manager.Registered6C.Read(1),"Registration flag unchanged");Equal(0,f.B.Calls.Count,"No external calls");
  });
  Test("Stop reads owner after singleton and preserves failure prefix",delegate{
   var f=new F();object lateOwner=new object();f.Manager.Registered6C.Write(1);
   f.B.On=delegate(string n,Probe p){if(n=="navigation")f.Manager.Owner00.Write(lateOwner);if(n=="stop-object"){Same(lateOwner,p.Arg1,"Late owner");Equal((byte)7,f.Manager.Moving38.Read(1),"Not cleared before import");throw new Exception("fixture");}};
   Missing(Stop(f),0x08ed1b);Equal((byte)7,f.Manager.Moving38.Read(1),"No finally clear");Equal(0,Count(f.B,"notify-position"),"Stop uses609F50, not609F68");
  });
  Test("Move integrates real AlreadyThere arithmetic and stops near destination",delegate{
   var f=new F();f.B.X=0.01f;Pass(Move(f));Equal(1,Count(f.B,"difference"),"New predicate body used");Equal(0,Count(f.B,"move-manager"),"No fabricated movement request when already there");Equal((byte)0,f.Manager.Moving38.Read(1),"Existing inline stop runs");Equal(1,Count(f.B,"associated"),"Normal near-destination notification lookup");
  });
  Test("Move outside threshold reaches unresolved production mover",delegate{
   var f=new F();f.B.X=1;Missing(Move(f),0x08eaf0);Equal(1,Count(f.B,"difference"),"Actual predicate used");Equal(1,Count(f.B,"move-manager"),"Exact unresolved boundary");Equal(0,Count(f.B,"animations"),"Unknown movement cannot become animation success");Equal((byte)7,f.Manager.Moving38.Read(1),"No manufactured moving result");
  });
  Test("Move animation failure integrates real StopManager after explicit observed move",delegate{
   var f=new F();f.B.X=1;f.B.PermitObservedMove=true;f.Manager.Registered6C.Write(1);
   f.B.On=delegate(string n,Probe p){if(n=="stop-object"){Same(f.Owner,p.Arg1,"Stop uses original owner");Equal((byte)7,f.Manager.Moving38.Read(1),"Clear happens after import");}};
   Pass(Move(f));Equal(1,Count(f.B,"play"),"Fixture animation failure observed");Equal(1,Count(f.B,"stop-object"),"Real StopManager contract invoked");Equal((byte)0,f.Manager.Moving38.Read(1),"Manager stopped");Equal(2,f.B.States.Count,"State transitions");Equal(5u,f.B.States[0],"Moving state");Equal(1u,f.B.States[1],"Fallback state after stop");Equal(0,Count(f.B,"notify-position"),"Stop not position notification");
  });
 }
 public static int Main(){try{RunAll();Console.WriteLine("PASS "+scenarios+" scenarios, "+assertions+" assertions; extension observations only, no native gameplay validation");return 0;}catch(Exception x){Console.Error.WriteLine("FAIL after "+scenarios+" scenarios / "+assertions+" assertions: "+x);return 1;}}
}
