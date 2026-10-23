using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// The observed Game2BFAF0 wrapper and Engine0825C0 field store are tested.
// Probe callbacks are explicit observations, not recovered native side effects.
public static class PortalCameraTests {
 static int assertions,scenarios;
 public static int Assertions {get{return assertions;}}
 public static int Scenarios {get{return scenarios;}}
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Value(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static void Float(uint expected,float actual,string message){Equal(expected,Bits(actual),message);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(Outcome result,uint address){Check(!result.ReturnedNormally,"Unexpected normal return");Equal(address,result.Address,"Missing boundary address");}
 static void Unknown(Field<float> field){bool missing=false;try{field.Read(0x2bfb07);}catch(MissingContract x){missing=true;Equal(0x2bfb07U,x.Address,"Unknown field address");}Check(missing,"Field unexpectedly initialized");}
 static void Unknown<T>(Field<T> field,uint at){bool missing=false;try{field.Read(at);}catch(MissingContract x){missing=true;Equal(at,x.Address,"Unknown field remains independent");}Check(missing,"Field unexpectedly became known");}
 static Outcome Attempt(PortalCamera camera,float angle,PortalTransitionBoundaries b){return MoveContracts.Run(delegate(Invocation i){PortalCameraContracts.GameSetYaw(camera,angle,b,i);});}
 sealed class CameraFixture {
  public readonly PortalCamera Camera=new PortalCamera();
  public readonly PortalCameraVTable Table=new PortalCameraVTable();
  public readonly PortalCameraState State;
  public readonly PortalCameraBoundaries B=new PortalCameraBoundaries(new MoveContext(),new PortalContext());
  public readonly NativeCameraFrame60 Output=new NativeCameraFrame60();
  public readonly CameraCoordinates32 Returned=new CameraCoordinates32();
  public CameraFixture(){State=new PortalCameraState(Camera);B.Attach(State);Camera.Virtual00.Write(Table);State.Position94.Write(0x1122334455667788UL);State.Position9C.Write(0xdeadbeefU);for(int n=0;n<9;n++)State.Basis70[n].Write(n+1);Returned.Q0.Write(0x123456789abcdef0UL);B.BindObservedSlot18(Table,delegate(PortalCamera camera,CameraCoordinates32 hidden){return Returned;});}
  public NativeCameraFrame60 Get(){return PortalCameraContracts.GetCoords(Camera,Output,B,new Invocation());}
  public Outcome Attempt(){return MoveContracts.Run(delegate(Invocation i){PortalCameraContracts.GetCoords(Camera,Output,B,i);});}
 }
 sealed class Probe:PortalTransitionBoundaries {
  public int Calls;
  public PortalCamera LastCamera;
  public float LastAngle;
  public Action<PortalCamera,float> On;
  public Probe():base(new MoveContext(),new PortalContext()){}
  public override void SetWorldCameraYaw60A6E8(PortalCamera camera,float angle){Calls++;LastCamera=camera;LastAngle=angle;if(On!=null)On(camera,angle);}
 }
 public static void Run(){
  Test("base observes previous field and exact original arguments",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(12.5f);var b=new Probe();
   b.On=delegate(PortalCamera seen,float angle){Same(c,seen,"Original receiver forwarded");Float(Bits(-3.25f),angle,"Original float forwarded");Float(Bits(12.5f),seen.Yaw59C.Read(1),"No premature59C store");};
   var result=Attempt(c,-3.25f,b);Check(result.ReturnedNormally,"Normal base returns");Equal(1,b.Calls,"One base call");Float(Bits(-3.25f),c.Yaw59C.Read(1),"Saved argument written after call");
  });
  Test("base mutation cannot replace saved incoming float",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(1);var b=new Probe();b.On=delegate(PortalCamera seen,float angle){seen.Yaw59C.Write(999);};
   Check(Attempt(c,2,b).ReturnedNormally,"Normal return");Float(Bits(2),c.Yaw59C.Read(1),"Original incoming value restored");Equal(1,b.Calls,"One callback");
  });
  Test("throwing base retains its own partial write",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(1);var b=new Probe();b.On=delegate(PortalCamera seen,float angle){seen.Yaw59C.Write(77);throw new InvalidOperationException("fixture");};
   var result=Attempt(c,2,b);Missing(result,0x2bfb01);Check(result.Exception.InnerException is InvalidOperationException,"Original callback failure retained");Float(Bits(77),c.Yaw59C.Read(1),"No59C overwrite on failure");Equal(1,b.Calls,"No retry");
  });
  Test("explicit dependency stop keeps exact missing address",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(1);var b=new Probe();b.On=delegate(PortalCamera seen,float angle){seen.Yaw59C.Write(-9);throw new MissingContract(0x229999,"fixture boundary");};
   Missing(Attempt(c,2,b),0x229999);Float(Bits(-9),c.Yaw59C.Read(1),"Dependency prefix retained");Equal(1,b.Calls,"No invented completion");
  });
  Test("same-camera reentry completes inner then restores outer input",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(1);var b=new Probe();
   b.On=delegate(PortalCamera seen,float angle){if(b.Calls==1){PortalCameraContracts.GameSetYaw(seen,8,b,new Invocation());Float(Bits(8),seen.Yaw59C.Read(1),"Inner write visible before outer return");}};
   Check(Attempt(c,3,b).ReturnedNormally,"Nested normal return");Equal(2,b.Calls,"Both base calls happen");Float(Bits(3),c.Yaw59C.Read(1),"Outer saved value written last");
  });
  Test("global and vtable replacement cannot redirect final receiver",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(1);var other=new PortalGameEngine();other.Camera13B0.Yaw59C.Write(22);var replacement=new PortalCameraVTable();var b=new Probe();
   b.On=delegate(PortalCamera seen,float angle){seen.Virtual00.Write(replacement);b.PortalContext.GameGlobalA66288.Write(other);};
   Check(Attempt(c,3,b).ReturnedNormally,"Normal return");Float(Bits(3),c.Yaw59C.Read(1),"Original receiver gets write");Float(Bits(22),other.Camera13B0.Yaw59C.Read(1),"Replacement camera untouched");Same(replacement,c.Virtual00.Read(1),"Callback table mutation retained");
  });
  Test("wrapper transports float bits without normalization",delegate{
   uint[] values={0x00000000U,0x80000000U,0x7f800000U,0xff800000U,0x7fc12345U,0x00000001U,0xc02d70a4U};
   foreach(uint bits in values){var c=new PortalCamera();var b=new Probe();Check(Attempt(c,Value(bits),b).ReturnedNormally,"Fixture base returned");Float(bits,b.LastAngle,"Argument bits preserved");Float(bits,c.Yaw59C.Read(1),"Stored bits preserved");Same(c,b.LastCamera,"Receiver identity preserved");}
  });
  Test("unknown yaw is not read before normal store",delegate{
   var c=new PortalCamera();var b=new Probe();b.On=delegate(PortalCamera seen,float angle){Unknown(seen.Yaw59C);};
   Check(Attempt(c,4,b).ReturnedNormally,"Unknown prior field allowed by wrapper");Float(Bits(4),c.Yaw59C.Read(1),"Normal write establishes value");
  });
  Test("null receiver is forwarded before wrapper post-call dereference",delegate{
   var b=new Probe();b.On=delegate(PortalCamera seen,float angle){Same(null,seen,"Fixture observes null forwarding");};
   Missing(Attempt(null,4,b),0x2bfb07);Equal(1,b.Calls,"Base entered first; not proof native base accepts null");
  });
  Test("default base boundary cannot manufacture supported yaw",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(6);var b=new PortalTransitionBoundaries(new MoveContext(),new PortalContext());
   Missing(Attempt(c,4,b),0x60a6e8);Float(Bits(6),c.Yaw59C.Read(1),"Old state retained at unsupported base");
  });
  Test("observed Engine base writes only0C",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(6);var state=new PortalCameraState(c);var i=new Invocation();
   PortalCameraContracts.WorldSetYaw(state,-7,i);Same(c,state.Identity,"Field view retains camera identity");Float(Bits(-7),state.Yaw0C.Read(1),"Exact0C store");Float(Bits(6),c.Yaw59C.Read(1),"Engine base does not write59C");Equal(0x0825c0U,i.At,"Observed store address");
  });
  Test("real field-store adapter connects base and Game wrapper",delegate{
   uint[] values={0x00000000U,0x80000000U,0x7f800000U,0xff800000U,0x7fc12345U,0x00000001U,0x3fc00000U};
   foreach(uint bits in values){var c=new PortalCamera();var state=new PortalCameraState(c);var b=new PortalCameraBoundaries(new MoveContext(),new PortalContext());b.Attach(state);Check(Attempt(c,Value(bits),b).ReturnedNormally,"Observed base and wrapper return");Float(bits,state.Yaw0C.Read(1),"Engine field bits");Float(bits,c.Yaw59C.Read(1),"Game field bits");}
  });
  Test("unattached identity stops before either inferred state or59C write",delegate{
   var c=new PortalCamera();c.Yaw59C.Write(6);var state=new PortalCameraState(c);var b=new PortalCameraBoundaries(new MoveContext(),new PortalContext());
   Missing(Attempt(c,4,b),0x0825c0);Float(Bits(6),c.Yaw59C.Read(1),"Game prefix unchanged");Unknown(state.Yaw0C);
  });
  Test("registry preserves one shared state per distinct identity",delegate{
   var a=new PortalCamera();var c=new PortalCamera();var sa=new PortalCameraState(a);var sc=new PortalCameraState(c);sa.Yaw0C.Write(8);sc.Yaw0C.Write(9);var b=new PortalCameraBoundaries(new MoveContext(),new PortalContext());b.Attach(sa);b.Attach(sa);b.Attach(sc);
   Same(sa,b.CameraState(a),"Idempotent attach preserves view");Same(sc,b.CameraState(c),"Other identity retains separate view");bool rejected=false;try{b.Attach(new PortalCameraState(a));}catch(InvalidOperationException){rejected=true;}Check(rejected,"Conflicting view rejected");Same(sa,b.CameraState(a),"Rejected attach preserves prior state");Check(Attempt(a,3,b).ReturnedNormally,"First identity updated");Float(Bits(3),sa.Yaw0C.Read(1),"Shared view sees write");Float(Bits(9),sc.Yaw0C.Read(1),"Other state unchanged");
  });
  Test("observed Engine adapter rejects null at0C store",delegate{
   var b=new PortalCameraBoundaries(new MoveContext(),new PortalContext());Missing(Attempt(null,4,b),0x0825c0);
  });
  Test("attaching yaw view does not invent GetCoords padding",delegate{
   var c=new PortalCamera();var b=new PortalCameraBoundaries(new MoveContext(),new PortalContext());b.Attach(new PortalCameraState(c));var output=new CoordinateFrame60();
   Outcome result=MoveContracts.Run(delegate(Invocation i){i.At=0x08dd4c;b.CameraFrame609F00(c,output);});Missing(result,0x609f00);bool missing=false;try{output.Position00.Q16.Read(0x229194);}catch(MissingContract x){missing=true;Equal(0x229194U,x.Address,"Whole-word projection remains unknown");}Check(missing,"No zero or invented high DWORD");
  });
  Test("raw GetCoords completes all basis writes with unknown padding",delegate{
   var f=new CameraFixture();Same(f.Output,f.Get(),"Original output identity returned");Equal(0x123456789abcdef0UL,f.Output.Q0.Read(1),"Returned pointer token copied");Equal(0x1122334455667788UL,f.Output.Q8.Read(1),"Position94 copied");Equal(0xdeadbeefU,f.Output.D10.Read(1),"Only low DWORD written");Unknown(f.Output.D14,0x14);for(int n=0;n<9;n++)Float(Bits(n+1),f.Output.Basis18[n].Read(1),"Basis completed");
   var projected=new CoordinateFrame60();projected.Position00.Q0.Write(99);Outcome r=MoveContracts.Run(delegate(Invocation i){PortalCameraContracts.ProjectToFrozenFrame(f.Output,projected);});Missing(r,0);Check(r.Boundary.IndexOf("own frame projection",StringComparison.Ordinal)>=0,"Projection failure is not a native address");Equal(99UL,projected.Position00.Q0.Read(1),"Failed projection leaves target intact");Float(Bits(9),f.Output.Basis18[8].Read(1),"Native copy was already complete");
  });
  Test("known padding survives native copy and permits explicit projection",delegate{
   var f=new CameraFixture();f.Output.D14.Write(0xa1b2c3d4U);f.Get();Equal(0xa1b2c3d4U,f.Output.D14.Read(1),"Padding not replaced by zero");var output=new CoordinateFrame60();Same(output,PortalCameraContracts.ProjectToFrozenFrame(f.Output,output),"Frozen output identity returned");Equal(0xa1b2c3d4deadbeefUL,output.Position00.Q16.Read(1),"Exact two-DWORD composition");Equal(f.Output.Q0.Read(1),output.Position00.Q0.Read(1),"Token projected");Equal(f.Output.Q8.Read(1),output.Position00.Q8.Read(1),"Position projected");for(int n=0;n<9;n++)Float(Bits(n+1),output.Basis18[n].Read(1),"Basis projected");
  });
  Test("null slot return retains callback changes but no output prefix",delegate{
   var f=new CameraFixture();f.Output.Q0.Write(41);f.Output.Q8.Write(42);f.Output.D10.Write(43);f.Output.D14.Write(44);int calls=0;
   f.B.BindObservedSlot18(f.Table,delegate(PortalCamera camera,CameraCoordinates32 hidden){calls++;f.State.Position94.Write(777);return null;});Missing(f.Attempt(),0x22917b);Equal(1,calls,"No retry");Equal(777UL,f.State.Position94.Read(1),"Callback side effect retained");Equal(41UL,f.Output.Q0.Read(1),"First output untouched");Equal(42UL,f.Output.Q8.Read(1),"Later output untouched");Equal(43U,f.Output.D10.Read(1),"DWORD untouched");Equal(44U,f.Output.D14.Read(1),"Padding untouched");
  });
  Test("slot returned identity supplies token while hidden storage is ignored",delegate{
   var f=new CameraFixture();CameraCoordinates32 seen=null;f.B.BindObservedSlot18(f.Table,delegate(PortalCamera camera,CameraCoordinates32 hidden){Same(f.Camera,camera,"Captured camera forwarded");seen=hidden;hidden.Q0.Write(1);hidden.Q8.Write(2);return f.Returned;});f.Get();Check(seen!=null,"Own hidden32 provided");Check(!Object.ReferenceEquals(seen,f.Returned),"Returned pointer is distinct");Equal(0x123456789abcdef0UL,f.Output.Q0.Read(1),"Uses returned Q0");Unknown(f.Returned.Q8,8);Unknown(f.Returned.Q16,16);Unknown(f.Returned.Q24,24);
  });
  Test("slot callback mutations are read through original camera after return",delegate{
   var f=new CameraFixture();var replacement=new PortalCameraVTable();int oldCalls=0,newCalls=0;f.B.BindObservedSlot18(replacement,delegate(PortalCamera camera,CameraCoordinates32 hidden){newCalls++;throw new Exception("Unexpected replacement call");});
   f.B.BindObservedSlot18(f.Table,delegate(PortalCamera camera,CameraCoordinates32 hidden){oldCalls++;camera.Virtual00.Write(replacement);f.State.Position94.Write(7);f.State.Position9C.Write(8);for(int n=0;n<9;n++)f.State.Basis70[n].Write(n+11);return f.Returned;});f.Get();Equal(1,oldCalls,"Captured old table target called once");Equal(0,newCalls,"No second slot dispatch");Equal(7UL,f.Output.Q8.Read(1),"Late position read");Equal(8U,f.Output.D10.Read(1),"Late DWORD read");for(int n=0;n<9;n++)Float(Bits(n+11),f.Output.Basis18[n].Read(1),"Late basis read");Same(replacement,f.Camera.Virtual00.Read(1),"Callback vptr mutation retained");
  });
  Test("unknown vptr stops without default callback success",delegate{
   var f=new CameraFixture();f.Camera.Virtual00.Forget();f.Output.Q0.Write(77);Missing(f.Attempt(),0x22916a);Equal(77UL,f.Output.Q0.Read(1),"No output before vptr resolution");
  });
  Test("null table unbound slot and null target stop at dispatch",delegate{
   var f=new CameraFixture();f.Camera.Virtual00.Write(null);Missing(f.Attempt(),0x229178);
   var other=new PortalCameraVTable();f.Camera.Virtual00.Write(other);Missing(f.Attempt(),0x229178);f.B.BindObservedSlot18(other,null);Missing(f.Attempt(),0x229178);Unknown(f.Output.Q0,0);
  });
  Test("slot exception preserves callback prefix without native copy",delegate{
   var f=new CameraFixture();f.Output.Q0.Write(17);f.B.BindObservedSlot18(f.Table,delegate(PortalCamera camera,CameraCoordinates32 hidden){f.State.Position9C.Write(27);throw new InvalidOperationException("fixture");});var r=f.Attempt();Missing(r,0x229178);Check(r.Exception.InnerException is InvalidOperationException,"Original callback failure retained");Equal(17UL,f.Output.Q0.Read(1),"Output unchanged");Equal(27U,f.State.Position9C.Read(1),"Callback effect retained");
  });
  Test("opaque unknown camera components copy without skipping later basis",delegate{
   var f=new CameraFixture();f.State.Position9C.Forget();f.State.Basis70[0].Forget();f.Output.D10.Write(77);f.Output.D14.Write(88);f.Get();Unknown(f.Output.D10,0x10);Unknown(f.Output.Basis18[0],0x18);Equal(88U,f.Output.D14.Read(1),"Known padding remains known");Float(Bits(9),f.Output.Basis18[8].Read(1),"Later basis copied despite unknown payload");Missing(MoveContracts.Run(delegate(Invocation i){PortalCameraContracts.ProjectToFrozenFrame(f.Output,new CoordinateFrame60());}),0);
  });
  Test("null output is rejected only after slot callback and returned load",delegate{
   var f=new CameraFixture();int calls=0;f.B.BindObservedSlot18(f.Table,delegate(PortalCamera camera,CameraCoordinates32 hidden){calls++;return f.Returned;});Outcome r=MoveContracts.Run(delegate(Invocation i){PortalCameraContracts.GetCoords(f.Camera,null,f.B,i);});Missing(r,0x22917e);Equal(1,calls,"Dispatch precedes output dereference");
  });
 }
 public static int Main(){try{Run();Console.WriteLine("PASS "+scenarios+" scenarios, "+assertions+" assertions; camera field contracts and explicit slot observations only, no native camera validation");return 0;}catch(Exception x){Console.Error.WriteLine("FAIL after "+scenarios+" scenarios / "+assertions+" assertions: "+x);return 1;}}
}
