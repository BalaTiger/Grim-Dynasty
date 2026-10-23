using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New shared-state image/ray cases only. Both camera getters are the actual
// observed GameCamera implementation. No callback manufactures a target token,
// camera image point, normalized direction or tangent result.
public static class CameraWorldRayTests {
 static int scenarios,assertions;
 static long nativeMathCalls;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 public static long NativeMathCalls{get{return nativeMathCalls;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Equal(at,error.Address,why+" address");return;}throw new Exception("Expected unsupported observation: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected conflicting identity rejection: "+why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static ulong Pair(float x,float y){return (ulong)Bits(x)|((ulong)Bits(y)<<32);}
 static void Point(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static void PointIs(NavigationPoint3 p,float x,float y,float z,string why){Equal(Bits(x),Bits(p.X.Read(0)),why+" X");Equal(Bits(y),Bits(p.Y.Read(0)),why+" Y");Equal(Bits(z),Bits(p.Z.Read(0)),why+" Z");}
 static void PositionIs(WorldPosition24 p,float x,float y,float z,string why){Equal(Bits(x),p.D8.Read(0),why+" X");Equal(Bits(y),p.DC.Read(0),why+" Y");Equal(Bits(z),p.D10.Read(0),why+" Z");}
 static void Vector(CameraVector3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 sealed class Fixture {
  public const ulong Token=0x1723456789abcdefUL;
  public readonly PortalGameEngine Engine=new PortalGameEngine();
  public readonly PortalCameraVTable Table=new PortalCameraVTable();
  public readonly CameraTargetBoundaries Targets=new CameraTargetBoundaries();
  public readonly EvidenceCameraBoundaries Boundaries;
  public readonly PortalCameraState Base;
  public readonly CameraTargetState Target;
  public readonly WorldCoordinateState Regions=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly CameraImageState Image;
  public readonly CameraWorldRayContext Context;
  public readonly CameraImagePoint2 Input=new CameraImagePoint2(Field<float>.Of(.5f),Field<float>.Of(.5f));
  public readonly CameraViewport Viewport=new CameraViewport();
  public readonly NavigationPoint3 ImageOutput=new NavigationPoint3();
  public readonly WorldRayObserved36 Ray=new WorldRayObserved36(new WorldPosition24(),new NavigationPoint3());
  public Fixture():this(null){}
  public Fixture(SystemUcrtCameraMath math){
   Base=new PortalCameraState(Engine.Camera13B0);Target=new CameraTargetState(Engine.Camera13B0);
   Boundaries=new EvidenceCameraBoundaries(new MoveContext(),new PortalContext(),Targets);
   Boundaries.Attach(Base);Boundaries.AttachTarget(Target);Engine.Camera13B0.Virtual00.Write(Table);
   Target.Target120.Q0.Write(Token);Target.Target120.D8.Write(Bits(-11));Target.Target120.DC.Write(Bits(-22));Target.Target120.D10.Write(Bits(-33));
   Vector(Target.Offset55C,0,0,0);Target.Count138.Write(0);
   Base.Position94.Write(Pair(10,20));Base.Position9C.Write(Bits(30));for(int n=0;n<9;n++)Base.Basis70[n].Write(n%4==0?1:0);
   Region.Offset3C.Write(100);Region.Offset40.Write(-200);Region.Offset44.Write(300);Regions.AttachRegion(Token,Region);
   Image=new CameraImageState(Base);Image.Mode6C.Write(1);Image.FovA0.Write(1);Image.WidthA4.Write(2);Image.HeightA8.Write(2);
   Context=new CameraWorldRayContext(Boundaries,Regions,math);Context.Attach(Image);Context.BindObservedGameCamera(Table);Context.UseObservedConstants();
   Viewport.Width08.Write(2);Viewport.Height0C.Write(1);Point(ImageOutput,81,82,83);
   Ray.Origin00.Q0.Write(0x55);Ray.Origin00.D8.Write(Bits(91));Ray.Origin00.DC.Write(Bits(92));Ray.Origin00.D10.Write(Bits(93));Ray.Origin00.D14.Write(0xa1b2c3d4);Point(Ray.Direction18,71,72,73);
  }
  public NavigationPoint3 Project(){return CameraWorldRayContracts.GetImagePoint044A10(Image,ImageOutput,Input,Viewport,Context,new Invocation());}
  public WorldRayObserved36 Cast(){return CameraWorldRayContracts.GetRay229700(Engine.Camera13B0,Ray,Input,Viewport,Context,new Invocation());}
  public void Origin(){Origin(Token);}
  public void Origin(ulong token){Equal(token,Ray.Origin00.Q0.Read(0),"Actual target region token");PositionIs(Ray.Origin00,10,20,30,"Origin comes from shared base camera");Unknown(Ray.Origin00.D14,"Fresh local padding overwrote old known D14");}
 }

 public static void Run(){
  scenarios=0;assertions=0;nativeMathCalls=0;
  Test("New optical fields remain unknown and one camera identity rejects a second image state",delegate{
   var f=new Fixture();var separate=new CameraImageState(f.Base);
   Unknown(separate.Mode6C,"No implicit camera mode");Unknown(separate.FovA0,"No implicit FOV");Unknown(separate.WidthA4,"No implicit width");Unknown(separate.HeightA8,"No implicit height");
   f.Context.Attach(f.Image);Invalid(delegate{f.Context.Attach(separate);},"One authoritative optical view per camera");
  });
  Test("Unknown mode stops before output while another mode bypasses all image and viewport operands",delegate{
   var f=new Fixture();f.Image.Mode6C.Forget();
   Missing(0x044a24,delegate{f.Project();},"Mode read first");PointIs(f.ImageOutput,81,82,83,"No premature image output");
   f.Image.Mode6C.Write(-7);f.Context.Half34BADC.Forget();f.Context.One34BB44.Forget();f.Context.Fallback338458.Forget();
   Same(f.ImageOutput,CameraWorldRayContracts.GetImagePoint044A10(f.Image,f.ImageOutput,null,null,f.Context,new Invocation()),"Hidden image address returned");PointIs(f.ImageOutput,0,0,0,"Observed default zero stores");
  });
  Test("Mode1 borrows canonical camera basis and packed position and bypasses a null viewport",delegate{
   var f=new Fixture();Same(f.ImageOutput,CameraWorldRayContracts.GetImagePoint044A10(f.Image,f.ImageOutput,f.Input,null,f.Context,new Invocation()),"Mode1 does not need viewport");
   PointIs(f.ImageOutput,10,20,31,"Shared original camera fields");
   f.Base.Position94.Write(Pair(40,50));f.Base.Position9C.Write(Bits(60));f.Base.Basis70[8].Write(2);
   f.Project();PointIs(f.ImageOutput,40,50,62,"Later canonical field mutations are read without mirrored synchronization");
  });
  Test("Mode1 output aliases input and basis cells only after all arithmetic reads complete",delegate{
   var f=new Fixture();var output=new NavigationPoint3(f.Input.X0,f.Input.Y4,f.Base.Basis70[8]);
   Same(output,CameraWorldRayContracts.GetImagePoint044A10(f.Image,output,f.Input,null,f.Context,new Invocation()),"Same borrowed output returned");
   PointIs(output,10,20,31,"Aliased result");Equal(10f,f.Input.X0.Read(0),"Image X cell receives output X");Equal(20f,f.Input.Y4.Read(0),"Image Y cell receives output Y");Equal(31f,f.Base.Basis70[8].Read(0),"Basis cell receives output Z");
  });
  Test("Mode1 unknown snapshots fail at their actual arithmetic consumption in instruction order",delegate{
   var f=new Fixture();f.Input.X0.Forget();f.Base.Position94.Forget();
   Missing(0x044b3e,delegate{f.Project();},"Input X addition precedes captured packed-position consumption");f.Input.X0.Write(.5f);
   Missing(0x044b42,delegate{f.Project();},"Packed position Y is consumed by the base Y addition");f.Base.Position94.Write(Pair(10,20));f.Context.One34BB44.Forget();
   Missing(0x044b51,delegate{f.Project();},"Explicit one constant is consumed by horizontal subtraction");f.Context.UseObservedConstants();f.Input.Y4.Forget();f.Image.WidthA4.Forget();
   Missing(0x044b5b,delegate{f.Project();},"Input Y addition precedes width consumption");f.Input.Y4.Write(.5f);
   Missing(0x044b5f,delegate{f.Project();},"Width is consumed by horizontal multiplication");PointIs(f.ImageOutput,81,82,83,"All failures precede final image stores");
  });
  Test("Mode1 short output retains ordered X and Y stores before the Z address failure",delegate{
   var f=new Fixture();var buffer=new NavigationPointBuffer(new Field<float>[]{Field<float>.Of(81),Field<float>.Of(82)});var output=buffer.PointAtScalar(0,0);
   Missing(0x044bd9,delegate{CameraWorldRayContracts.GetImagePoint044A10(f.Image,output,f.Input,null,f.Context,new Invocation());},"Final scalar output requires its address");
   Equal(Bits(10),Bits(buffer.Cells[0].Read(0)),"X store retained");Equal(Bits(20),Bits(buffer.Cells[1].Read(0)),"Y store retained");
  });
  Test("Actual GameCamera getters feed the canonical finite WorldRay and unknown padding",delegate{
   var f=new Fixture();Same(f.Ray,f.Cast(),"Hidden ray address returned");f.Origin();PointIs(f.Ray.Direction18,0,0,1,"Actual normalized direction");
   Same(f.Region,f.Regions.Region(f.Ray.Origin00.Q0.Read(0),0),"Region identity is the canonical world view");Equal(Bits(-22),f.Target.Target120.DC.Read(0),"Real getter did not rewrite source target");Unknown(f.Target.Target120.D14,"Source target padding remains unknown");
   Equal(2,f.Context.LastRayGetterCount,"Two actual GameGetTarget invocations");Check(!Object.ReferenceEquals(f.Context.LastRayFirstHidden,f.Context.LastRaySecondHidden),"Distinct fresh hidden return buffers");
   Same(f.Context.LastRayFirstHidden,f.Context.LastRayFirstReturned,"First actual getter returned its supplied hidden address");Same(f.Context.LastRaySecondHidden,f.Context.LastRaySecondReturned,"Second actual getter returned its supplied hidden address");
   var first=f.Boundaries.RawTargetFor(f.Context.LastRayFirstHidden);var second=f.Boundaries.RawTargetFor(f.Context.LastRaySecondHidden);
   Equal(Bits(-21),first.DC.Read(0),"First real target includes the observed vertical translation");Equal(Bits(-21),second.DC.Read(0),"Second real target independently executes the vertical translation");Unknown(first.D14,"First target padding not synthesized");Unknown(second.D14,"Second target padding not synthesized");
  });
  Test("First real target failure retains only the initial ray Q0 clear",delegate{
   var f=new Fixture();f.Target.Offset55C.Y.Forget();
   Missing(0x22c5de,delegate{f.Cast();},"Actual GameGetTarget translation consumes missing Y");Equal(0UL,f.Ray.Origin00.Q0.Read(0),"Initial qword clear persists");
   PositionIs(f.Ray.Origin00,91,92,93,"Origin scalar stores not reached");Equal(0xa1b2c3d4u,f.Ray.Origin00.D14.Read(0),"Initial padding not yet replaced");PointIs(f.Ray.Direction18,71,72,73,"Direction not yet written");
   Equal(1,f.Context.LastRayGetterCount,"Failed first actual getter was invoked once");
  });
  Test("Image helper failure after target production retains full origin copy and no direction writes",delegate{
   var f=new Fixture();f.Image.Mode6C.Forget();
   Missing(0x044a24,delegate{f.Cast();},"Real image helper mode read stops");f.Origin();PointIs(f.Ray.Direction18,71,72,73,"Earlier direction preserved");
   Equal(2,f.Context.LastRayGetterCount,"Both real target calls completed before image mode failure");
  });
  Test("WorldRay final short direction keeps its qword XY store before missing Z",delegate{
   var f=new Fixture();var buffer=new NavigationPointBuffer(new Field<float>[]{Field<float>.Of(71),Field<float>.Of(72)});var ray=new WorldRayObserved36(f.Ray.Origin00,buffer.PointAtScalar(0,0));
   Missing(0x229892,delegate{CameraWorldRayContracts.GetRay229700(f.Engine.Camera13B0,ray,f.Input,f.Viewport,f.Context,new Invocation());},"Direction Z requires the final dword address");
   f.Origin();Equal(0u,Bits(buffer.Cells[0].Read(0)),"Direction X qword component stored");Equal(0u,Bits(buffer.Cells[1].Read(0)),"Direction Y qword component stored");
  });
  Test("An unbound nonzero region stops after real target and image production instead of creating a region",delegate{
   var f=new Fixture();var context=new CameraWorldRayContext(f.Boundaries,new WorldCoordinateState());context.Attach(f.Image);context.BindObservedGameCamera(f.Table);context.UseObservedConstants();
   Missing(0x2297b9,delegate{CameraWorldRayContracts.GetRay229700(f.Engine.Camera13B0,f.Ray,f.Input,f.Viewport,context,new Invocation());},"First real region offset load needs the canonical binding");
   f.Origin();PointIs(f.Ray.Direction18,71,72,73,"Region lookup failure precedes direction stores");Equal(2,context.LastRayGetterCount,"Canonical lookup was not replaced by skipped getters");
  });
  Test("Null token fallback and zero length stop only at the declared finite arithmetic domain",delegate{
   var absent=new Fixture();absent.Target.Target120.Q0.Write(0);
   Missing(0x22984c,delegate{absent.Cast();},"Observed infinite fallback reaches first normalization multiplication");absent.Origin(0);PointIs(absent.Ray.Direction18,71,72,73,"No fabricated fallback direction");Equal(2,absent.Context.LastRayGetterCount,"Null token still executes both actual getters");
   absent.Context.Fallback338458.Forget();
   Missing(0x22984c,delegate{absent.Cast();},"Unknown fallback MOV is consumed only by the first normalization multiplication");absent.Origin(0);PointIs(absent.Ray.Direction18,71,72,73,"Unknown fallback preserves the already copied origin and unwritten direction");
   var zero=new Fixture();zero.Base.Basis70[8].Write(0);
   Missing(0x229867,delegate{zero.Cast();},"Zero length reaches reciprocal division without an invented zero guard");zero.Origin();PointIs(zero.Ray.Direction18,71,72,73,"Nonfinite reciprocal does not fabricate a direction");
  });
  Test("Mode0 without an explicit real tangent provider stops before output",delegate{
   var f=new Fixture();f.Image.Mode6C.Write(0);
   Missing(0x044a57,delegate{f.Project();},"No injected or double-precision tangent fallback");PointIs(f.ImageOutput,81,82,83,"Missing provider leaves output untouched");
  });
  using(var owner=SystemUcrtRotationMath.OpenOwnProfile()){
   long beforeSuite=owner.NativeMathCalls;
   Test("Mode0 invokes actual system tanf before signed viewport conversion and downstream failure",delegate{
    var f=new Fixture(owner.Camera);f.Image.Mode6C.Write(0);f.Input.X0.Write(0);f.Viewport.Width08.Write(-2);
    long before=owner.NativeMathCalls;Same(f.ImageOutput,f.Project(),"Actual mode0 image result address");Equal(before+1,owner.NativeMathCalls,"One real tanf invocation");
    Check(f.ImageOutput.X.Read(0)<10,"Signed negative viewport width reverses horizontal displacement");Equal(Bits(20),Bits(f.ImageOutput.Y.Read(0)),"Centered vertical component");Equal(Bits(31),Bits(f.ImageOutput.Z.Read(0)),"Shared camera forward component");
    Point(f.ImageOutput,81,82,83);f.Viewport.Height0C.Forget();before=owner.NativeMathCalls;
    Missing(0x044a7f,delegate{f.Project();},"Viewport height consumed by signed conversion after tanf");Equal(before+1,owner.NativeMathCalls,"Actual tanf already ran before unknown height failure");PointIs(f.ImageOutput,81,82,83,"Unknown height leaves image output untouched");
    f.Viewport.Height0C.Write(0);before=owner.NativeMathCalls;
    Missing(0x044a89,delegate{f.Project();},"Zero viewport denominator reaches its division finite-domain stop");Equal(before+1,owner.NativeMathCalls,"Actual tanf already ran before zero denominator failure");PointIs(f.ImageOutput,81,82,83,"Division failure leaves image output untouched");
   });
   nativeMathCalls+=owner.NativeMathCalls-beforeSuite;
  }
 }
}
public static class CameraWorldRayTestProgram {
 public static int Main(){try{CameraWorldRayTests.Run();Console.WriteLine("PASS "+CameraWorldRayTests.Scenarios+" scenarios, "+CameraWorldRayTests.Assertions+" assertions; new shared camera image and ray contracts only; native_tanf_calls="+CameraWorldRayTests.NativeMathCalls);return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
