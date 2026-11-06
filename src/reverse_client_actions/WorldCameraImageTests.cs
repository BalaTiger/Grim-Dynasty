using System;
using System.Collections.Generic;
using System.Globalization;
using GrimDynasty.ReverseClient.Actions;

// New wrapper/slice tests with the concrete Game target getter and Camera044A10.
// Mode1 is explicit own fixture state; no UCRT, original DLL or saved map loads.
public static class WorldCameraImageTestProgram {
 static int scenarios,assertions;
 static void Check(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action action){action();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract error){Equal(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static ulong Pair(float x,float y){return (ulong)Bits(x)|((ulong)Bits(y)<<32);}
 static void Position(WorldPosition24 p,float x,float y,float z){Equal(Bits(x),p.D8.Read(0),"position X bits");Equal(Bits(y),p.DC.Read(0),"position Y bits");Equal(Bits(z),p.D10.Read(0),"position Z bits");}
 static void Point2(CameraImagePoint2 p,float x,float y){Equal(Bits(x),Bits(p.X0.Read(0)),"image X bits");Equal(Bits(y),Bits(p.Y4.Read(0)),"image Y bits");}

 sealed class Fixture {
  public readonly PortalCamera Camera=new PortalCamera();
  public readonly PortalCameraVTable Table=new PortalCameraVTable();
  public readonly CameraTargetBoundaries Targets=new CameraTargetBoundaries();
  public readonly EvidenceCameraBoundaries Boundaries;
  public readonly PortalCameraState Base;
  public readonly CameraTargetState Target;
  public readonly CameraImageState Image;
  public readonly CameraWorldRayContext Context;
  public readonly CameraViewport Viewport=new CameraViewport();
  public readonly CameraImagePoint2 Input=new CameraImagePoint2(Field<float>.Of(.5f),Field<float>.Of(.5f));
  public readonly WorldPosition24 Output=new WorldPosition24();
  public Fixture(){
   Base=new PortalCameraState(Camera);Target=new CameraTargetState(Camera);Image=new CameraImageState(Base);
   Boundaries=new EvidenceCameraBoundaries(new MoveContext(),new PortalContext(),Targets);Boundaries.Attach(Base);Boundaries.AttachTarget(Target);
   var regions=new WorldCoordinateState();var region=new WorldRegion(new object());region.Offset3C.Write(0);region.Offset40.Write(0);region.Offset44.Write(0);regions.AttachRegion(77,region);
   Context=new CameraWorldRayContext(Boundaries,regions);Context.Attach(Image);Context.BindObservedGameCamera(Table);Context.UseObservedConstants();Camera.Virtual00.Write(Table);
   Target.Target120.Q0.Write(77);Target.Target120.D8.Write(Bits(-11));Target.Target120.DC.Write(Bits(-22));Target.Target120.D10.Write(Bits(-33));
   Target.Offset55C.X.Write(0);Target.Offset55C.Y.Write(0);Target.Offset55C.Z.Write(0);Target.Count138.Write(0);
   Base.Position94.Write(Pair(10,20));Base.Position9C.Write(Bits(30));for(int n=0;n<9;n++)Base.Basis70[n].Write(n%4==0?1:0);
   Image.Mode6C.Write(1);Image.WidthA4.Write(2);Image.HeightA8.Write(2);
   Viewport.OriginX00.Write(-100);Viewport.OriginY04.Write(20);Viewport.Width08.Write(200);Viewport.Height0C.Write(80);
   Output.Q0.Write(999);Output.D8.Write(Bits(91));Output.DC.Write(Bits(92));Output.D10.Write(Bits(93));Output.D14.Write(0xa1b2c3d4);
  }
  public WorldPosition24 Project(){return WorldCameraImageContracts.GetImagePoint2290E0(Camera,Output,Input,Viewport,Context,new Invocation());}
  public WorldRayObserved36 Ray(){return CameraWorldRayContracts.GetRay229700(Camera,new WorldRayObserved36(),Input,Viewport,Context,new Invocation());}
  public void Unchanged(){Equal(999UL,Output.Q0.Read(0),"failure preserves old token");Position(Output,91,92,93);Equal(0xa1b2c3d4U,Output.D14.Read(0),"failure preserves padding");}
 }

 static void Run(){
  Test("Actual Game getter and image helper return original hidden output without writing known D14",delegate{
   var f=new Fixture();Same(f.Output,f.Project(),"original hidden result identity");Equal(77UL,f.Output.Q0.Read(0),"actual target token");Position(f.Output,10,20,31);Equal(0xa1b2c3d4U,f.Output.D14.Read(0),"known padding untouched");Equal(0,f.Context.LastRayGetterCount,"wrapper does not count as a ray getter");
  });
  Test("Unknown D14 remains unknown after successful wrapper",delegate{
   var f=new Fixture();f.Output.D14.Forget();f.Project();Unknown(f.Output.D14,"output padding stays unknown");Position(f.Output,10,20,31);Equal(77UL,f.Output.Q0.Read(0),"token still published");
  });
  Test("Unknown target token fails in the real getter before the image mode read",delegate{
   var f=new Fixture();f.Target.Target120.Q0.Forget();f.Image.Mode6C.Forget();Missing(0x22c0b0,delegate{f.Project();},"actual getter requires its input region token first");f.Unchanged();
   // This concrete getter cannot return an unknown Q0: its second GetRegion
   // consumes it. The wrapper's opaque Snapshot/CopyFrom is reviewed, not faked.
  });
  Test("Image-helper failure follows the actual getter and precedes every output store",delegate{
   var f=new Fixture();f.Image.Mode6C.Forget();Missing(0x044a24,delegate{f.Project();},"image mode is still unknown");f.Unchanged();
  });
  Test("Unbound captured camera table is rejected without a fabricated getter",delegate{
   var f=new Fixture();f.Camera.Virtual00.Write(new PortalCameraVTable());f.Image.Mode6C.Forget();Missing(0x22910e,delegate{f.Project();},"unknown slot target");f.Unchanged();
  });
  Test("Subsequent calls read shared camera state and do not resolve or default the token",delegate{
   var f=new Fixture();f.Project();f.Base.Position94.Write(Pair(40,50));f.Base.Position9C.Write(Bits(60));f.Base.Basis70[8].Write(2);f.Target.Target120.Q0.Write(88);
   f.Project();Position(f.Output,40,50,62);Equal(88UL,f.Output.Q0.Read(0),"unregistered but known token copied without region resolution");Equal(0xa1b2c3d4U,f.Output.D14.Read(0),"same padding");
  });
  Test("Shared getter validation preserves the two-call GetRay diagnostics",delegate{
   var f=new Fixture();var ray=f.Ray();Equal(2,f.Context.LastRayGetterCount,"existing ray makes two getters");
   var first=f.Context.LastRayFirstHidden;var second=f.Context.LastRaySecondHidden;var firstReturned=f.Context.LastRayFirstReturned;var secondReturned=f.Context.LastRaySecondReturned;
   Check(!Object.ReferenceEquals(first,second),"ray hidden outputs are distinct");Equal(Bits(1),Bits(ray.Direction18.Z.Read(0)),"existing ray direction");
   f.Project();Equal(2,f.Context.LastRayGetterCount,"wrapper leaves prior ray diagnostics intact");Same(first,f.Context.LastRayFirstHidden,"first hidden unchanged");Same(second,f.Context.LastRaySecondHidden,"second hidden unchanged");Same(firstReturned,f.Context.LastRayFirstReturned,"first returned unchanged");Same(secondReturned,f.Context.LastRaySecondReturned,"second returned unchanged");
  });
  Test("Viewport normalization converts signed origins and does not clamp",delegate{
   var f=new Fixture();var pixels=new CameraImagePoint2(Field<float>.Of(-50),Field<float>.Of(80));var output=new CameraImagePoint2();
   Same(output,WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation()),"normalization output identity");Point2(output,.25f,.75f);
   pixels.X0.Write(500);pixels.Y4.Write(200);WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());Point2(output,3,2.25f);
   f.Viewport.Width08.Write(-200);f.Viewport.Height0C.Write(-80);WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());Point2(output,-3,-2.25f);
  });
  Test("Zero extents stop at division in the declared finite domain without partial outputs",delegate{
   var f=new Fixture();var pixels=new CameraImagePoint2(Field<float>.Of(-50),Field<float>.Of(80));var output=new CameraImagePoint2(Field<float>.Of(91),Field<float>.Of(92));
   f.Viewport.Width08.Write(0);Missing(0x21789,delegate{WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());},"zero width gives nonfinite own result");Point2(output,91,92);
   f.Viewport.Width08.Write(200);f.Viewport.Height0C.Write(0);Missing(0x217aa,delegate{WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());},"zero height after X math");Point2(output,91,92);
  });
  Test("Unknown operands and nonfinite input preserve arithmetic order and both output cells",delegate{
   var f=new Fixture();var pixels=new CameraImagePoint2();var output=new CameraImagePoint2(Field<float>.Of(91),Field<float>.Of(92));f.Viewport.OriginX00.Forget();
   Missing(0x21772,delegate{WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());},"signed origin conversion before pixel consumption");f.Viewport.OriginX00.Write(-100);
   Missing(0x2177c,delegate{WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());},"pixel consumed by subtraction");pixels.X0.Write(-50);pixels.Y4.Write(80);f.Viewport.Height0C.Forget();
   Missing(0x217a7,delegate{WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());},"height conversion after prior arithmetic");Point2(output,91,92);
   f.Viewport.Height0C.Write(80);pixels.X0.Write(Single.PositiveInfinity);Missing(0x2177c,delegate{WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());},"finite-domain boundary is explicit");Point2(output,91,92);
  });
  Test("Both normalization axes are calculated before aliased output stores",delegate{
   var f=new Fixture();var pixels=new CameraImagePoint2(Field<float>.Of(-50),Field<float>.Of(80));var output=new CameraImagePoint2(pixels.Y4,pixels.X0);
   WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,output,new Invocation());Point2(output,.25f,.75f);Point2(pixels,.75f,.25f);
  });
  Test("The evidenced normalization slice feeds the WorldCamera wrapper on the same viewport",delegate{
   var f=new Fixture();var pixels=new CameraImagePoint2(Field<float>.Of(-50),Field<float>.Of(80));WorldCameraImageContracts.Normalize2176C(pixels,f.Viewport,f.Input,new Invocation());
   f.Project();Position(f.Output,10.5f,19.5f,31);Equal(77UL,f.Output.Q0.Read(0),"same actual target token");Equal(0xa1b2c3d4U,f.Output.D14.Read(0),"composition preserves padding");
  });
 }
 public static int Main(){try{Run();Console.WriteLine("{\"status\":\"passed-worldcamera-imagepoint-only\",\"scenarios\":"+scenarios.ToString(CultureInfo.InvariantCulture)+",\"assertions\":"+assertions.ToString(CultureInfo.InvariantCulture)+",\"native_math_calls\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"gameplay_accepted\":false,\"actual_game_target_getter\":true,\"actual_image_helper\":true,\"wrapper_hidden_identity\":true,\"normalization_slice\":true,\"ray_count_regression\":true,\"unknown_return_token_reachable\":false}");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
