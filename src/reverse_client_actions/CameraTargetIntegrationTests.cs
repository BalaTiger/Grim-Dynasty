using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;
public static class CameraTargetIntegrationTests {
 static int assertions,scenarios;
 public static int Assertions {get{return assertions;}}
 public static int Scenarios {get{return scenarios;}}
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Eq<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message);}
 static void Same(object a,object b,string m){Check(Object.ReferenceEquals(a,b),m);}
 static void Unknown<T>(Field<T> f){bool missing=false;try{f.Read(1);}catch(MissingContract){missing=true;}Check(missing,"Unknown field was defaulted");}
 static uint Bits(float x){return CameraTargetContracts.BitsFromFloat(x);}
 static void Vec(CameraVector3 v,float x,float y,float z){v.X.Write(x);v.Y.Write(y);v.Z.Write(z);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 sealed class Fixture {
  public readonly MoveContext Move=new MoveContext();
  public readonly PortalContext Portal=new PortalContext();
  public readonly PortalGameEngine Engine=new PortalGameEngine();
  public readonly PortalCameraVTable Table=new PortalCameraVTable();
  public readonly CameraTargetBoundaries Target=new CameraTargetBoundaries();
  public readonly PortalCameraState Base;
  public readonly CameraTargetState State;
  public readonly Probe B;
  public Fixture(){
   State=new CameraTargetState(Engine.Camera13B0);Base=new PortalCameraState(Engine.Camera13B0);
   B=new Probe(Move,Portal,Target);B.Attach(Base);B.AttachTarget(State);B.BindObservedGameCamera(Table);Engine.Camera13B0.Virtual00.Write(Table);
   State.Target120.Q0.Write(7);State.Target120.D8.Write(Bits(1));State.Target120.DC.Write(Bits(2));State.Target120.D10.Write(Bits(3));
   Vec(State.Offset55C,10,20,30);Vec(State.Offset60,.5f,1,1.5f);State.Count138.Write(1);
   Base.Position94.Write(0x1122334455667788UL);Base.Position9C.Write(0xaabbccddU);for(int n=0;n<9;n++)Base.Basis70[n].Write(n+1);
   Portal.GameGlobalA66288.Write(Engine);Engine.Connections40F8.Write(new object());Portal.PlayerClassA5CAB0.Write(B.PlayerClass);Portal.Pi7A80BC.Write(CameraTargetContracts.FloatFromBits(0x40490fdb));
   var world=new WorldEngine();world.World50.Write(B.World);Move.EngineGlobal.Write(world);
  }
 }
 // Only surrounding owner/class/network/atan/frame services are explicit
 // observations. The camera target, its imports and yaw use production code.
 sealed class Probe:EvidenceCameraBoundaries {
  public readonly object Owner=new object(),World=new object(),Manager=new object();
  public readonly ClassRecord PlayerClass=new ClassRecord();
  public CoordinateFrame60 CameraOutput,OwnerOutput,WrittenFrame;
  public float AtanY,AtanX;public int Writes;
  public Probe(MoveContext m,PortalContext p,CameraTargetBoundaries t):base(m,p,t){}
  public override CoordinateFrame60 CameraFrame609F00(PortalCamera camera,CoordinateFrame60 output){CameraOutput=output;return base.CameraFrame609F00(camera,output);}
  public override CoordinateFrame60 OwnerFrame609518(object owner,CoordinateFrame60 output){Same(Owner,owner,"Owner address");OwnerOutput=output;for(int n=0;n<9;n++)output.Basis18[n].Write(n%4==0?1:0);return output;}
  public override ClassRecord Class0(object value){Same(Owner,value,"Classification identity");return PlayerClass;}
  public override PlayerConnection Connection18(object manager){var c=new PlayerConnection();c.PlayerId10.Write(7);return c;}
  public override object ObjectManager(){return Manager;}
  public override object Lookup(object manager,uint id){Same(Manager,manager,"Lookup manager");Eq(7U,id,"Observed lookup ID");return Owner;}
  public override float Atan2F5DC3F8(float y,float x){AtanY=y;AtanX=x;return 7;}
  public override void SetFrame609BB8(object world,object owner,CoordinateFrame60 frame){Same(World,world,"World argument");Same(Owner,owner,"SetFrame owner");WrittenFrame=frame;Writes++;}
 }
 static Waypoint80 Local(){var p=new Waypoint80();p.Kind00.Write(1);p.Position08.Q0.Write(80);p.Position08.Q8.Write(81);p.Position08.Q16.Write(82);for(int n=0;n<9;n++)p.Matrix20[n].Write(n%4==0?1:0);return p;}
 static WaypointManagerView View(Fixture f){var m=new MovementManager();m.Owner00.Write(f.B.Owner);m.Moving38.Write(7);m.Registered6C.Write(0);var v=new WaypointManagerView(m);v.Queue40.Count20.Write(0);return v;}
 public static void Run(){
  Test("observed slot18 produces target with the same hidden Q0",delegate{
   var f=new Fixture();var hidden=new CameraCoordinates32();var result=f.B.Slot18Target(f.Table,1)(f.Engine.Camera13B0,hidden);
   Same(hidden,result,"Original hidden carrier returned");var raw=f.B.RawTargetFor(hidden);Same(hidden,raw.AddressIdentity,"Hidden address identity");Same(hidden.Q0,raw.Q0,"Shared token cell");Eq(7UL,hidden.Q0.Read(1),"Region token");
   Eq(Bits(11.5f),raw.D8.Read(1),"Real X translations");Eq(Bits(24),raw.DC.Read(1),"Real Y translations");Eq(Bits(34.5f),raw.D10.Read(1),"Real Z translations");Unknown(raw.D14);Unknown(hidden.Q8);Unknown(hidden.Q16);Unknown(hidden.Q24);
  });
  Test("basis-only portal bridge completes without unknown padding projection",delegate{
   var f=new Fixture();var frame=new CoordinateFrame60();Same(frame,f.B.CameraFrame609F00(f.Engine.Camera13B0,frame),"Output identity");var raw=f.B.RawFrameFor(frame);
   Eq(7UL,raw.Q0.Read(1),"Produced target token");Eq(0x1122334455667788UL,raw.Q8.Read(1),"Camera position94");Eq(0xaabbccddU,raw.D10.Read(1),"Only native low DWORD");Unknown(raw.D14);Unknown(frame.Position00.Q0);Unknown(frame.Position00.Q8);Unknown(frame.Position00.Q16);
   for(int n=0;n<9;n++){Same(raw.Basis18[n],frame.Basis18[n],"Basis field identity");Eq((float)(n+1),frame.Basis18[n].Read(1),"Basis value");}
   bool stopped=false;try{PortalCameraContracts.ProjectToFrozenFrame(raw,new CoordinateFrame60());}catch(MissingContract x){stopped=true;Eq(0U,x.Address,"Own projection boundary");}Check(stopped,"General projection must remain unsupported");
  });
  Test("basis binding preserves unknown components without default values",delegate{
   var f=new Fixture();f.Base.Basis70[7].Forget();var frame=new CoordinateFrame60();frame.Basis18[7].Write(123);f.B.CameraFrame609F00(f.Engine.Camera13B0,frame);
   Unknown(frame.Basis18[7]);Eq(9f,frame.Basis18[8].Read(1),"Later native writes still complete");Unknown(f.B.RawFrameFor(frame).D14);
  });
  Test("raw target failure keeps constructor and local prefix without camera output writes",delegate{
   var f=new Fixture();f.State.Offset55C.Y.Forget();var hidden=new CameraCoordinates32();hidden.Q0.Write(99);
   var r=MoveContracts.Run(delegate(Invocation i){f.B.Slot18Target(f.Table,1)(f.Engine.Camera13B0,hidden);});Check(!r.ReturnedNormally,"Target failure");Eq(0x22c5deU,r.Address,"Actual Y operand boundary");Eq(0UL,hidden.Q0.Read(1),"Ctor persists");Unknown(f.B.RawTargetFor(hidden).D8);Eq(Bits(1),f.State.Target120.D8.Read(1),"Camera target untouched by local translation");
  });
  Test("missing camera view retains GetCoords Q0 write before stopping",delegate{
   var f=new Fixture();var b=new EvidenceCameraBoundaries(f.Move,f.Portal,f.Target);b.BindObservedGameCamera(f.Table);var frame=new CoordinateFrame60();
   var r=MoveContracts.Run(delegate(Invocation i){b.CameraFrame609F00(f.Engine.Camera13B0,frame);});Check(!r.ReturnedNormally,"Missing base view");Eq(0x229181U,r.Address,"Late base field lookup");Eq(7UL,b.RawFrameFor(frame).Q0.Read(1),"Q0 was already stored");Unknown(frame.Basis18[0]);
  });
  Test("same portal output reuses raw view and preserves its untouched DWORD",delegate{
   var f=new Fixture();var frame=new CoordinateFrame60();f.B.CameraFrame609F00(f.Engine.Camera13B0,frame);var raw=f.B.RawFrameFor(frame);raw.D14.Write(0x12345678);f.Base.Basis70[6].Write(21);f.B.CameraFrame609F00(f.Engine.Camera13B0,frame);
   Same(raw,f.B.RawFrameFor(frame),"Raw identity retained");Eq(0x12345678U,raw.D14.Read(1),"Unwritten DWORD retained");Eq(21f,frame.Basis18[6].Read(1),"Current camera basis read");
  });
  Test("portal transition uses real target and yaw while surrounding services remain explicit observations",delegate{
   var f=new Fixture();var view=View(f);Eq((byte)0,PortalTransitionContracts.Execute(view,Local(),f.B,new Invocation()),"Empty following queue");
   Eq(7f,f.B.AtanY,"Transformed forward X");Eq(9f,f.B.AtanX,"Transformed forward Z");float yaw=CameraTargetContracts.F32(7-f.Portal.Pi7A80BC.Read(1));Eq(Bits(yaw),Bits(f.Base.Yaw0C.Read(1)),"Base yaw store");Eq(Bits(yaw),Bits(f.Engine.Camera13B0.Yaw59C.Read(1)),"Game yaw store");
   Eq(1,f.B.Writes,"Owner frame written once");Same(f.B.OwnerOutput,f.B.WrittenFrame,"Separate owner frame");Check(!Object.ReferenceEquals(f.B.CameraOutput,f.B.WrittenFrame),"Camera scratch is not SetFrame argument");Unknown(f.B.RawFrameFor(f.B.CameraOutput).D14);Eq((byte)0,view.Identity.Moving38.Read(1),"Moving flag cleared after frame write");
  });
  Test("target unknown stops portal before yaw and world publication",delegate{
   var f=new Fixture();var view=View(f);f.State.Offset55C.Y.Forget();f.Base.Yaw0C.Write(12);f.Engine.Camera13B0.Yaw59C.Write(13);
   var r=MoveContracts.Run(delegate(Invocation i){PortalTransitionContracts.Execute(view,Local(),f.B,i);});Check(!r.ReturnedNormally,"Portal stops");Eq(0x22c5deU,r.Address,"Target operand remains missing");Eq(0,f.B.Writes,"No world publication");Eq((byte)7,view.Identity.Moving38.Read(1),"Moving prefix unchanged");Eq(12f,f.Base.Yaw0C.Read(1),"No base yaw");Eq(13f,f.Engine.Camera13B0.Yaw59C.Read(1),"No Game yaw");Unknown(view.Identity.Position20.Q0);
  });
 }
}
public static class CameraTargetTestProgram {
 public static int Main(){try{CameraTargetTests.Run();CameraTargetIntegrationTests.Run();Console.WriteLine("PASS "+(CameraTargetTests.Scenarios+CameraTargetIntegrationTests.Scenarios)+" scenarios, "+(CameraTargetTests.Assertions+CameraTargetIntegrationTests.Assertions)+" assertions; evidenced camera components with explicit surrounding fixtures, no native/gameplay validation");return 0;}catch(Exception x){Console.Error.WriteLine(x);return 1;}}
}
