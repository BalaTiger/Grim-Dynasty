using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Additional cells on the existing camera identity. Basis70 and Position94/
 // Position9C remain authoritative in PortalCameraState, with no second copy.
 public sealed class CameraImageState {
  public readonly PortalCameraState Shared;
  public readonly Field<int> Mode6C=new Field<int>();
  public readonly Field<float> FovA0=new Field<float>(),WidthA4=new Field<float>(),HeightA8=new Field<float>();
  public CameraImageState(PortalCameraState shared){if(shared==null)throw new ArgumentNullException("shared");Shared=shared;}
 }
 public sealed class CameraImagePoint2 {
  public readonly Field<float> X0,Y4;
  public CameraImagePoint2():this(new Field<float>(),new Field<float>()){}
  public CameraImagePoint2(Field<float> x,Field<float> y){if(x==null||y==null)throw new ArgumentNullException("image cells");X0=x;Y4=y;}
 }
 public sealed class CameraViewport {
  public readonly Field<int> Width08=new Field<int>(),Height0C=new Field<int>();
 }
 // Only the36 observed bytes. This is not a native sizeof/layout declaration.
 public sealed class WorldRayObserved36 {
  public readonly WorldPosition24 Origin00;
  public readonly NavigationPoint3 Direction18;
  public WorldRayObserved36():this(new WorldPosition24(),new NavigationPoint3()){}
  public WorldRayObserved36(WorldPosition24 origin,NavigationPoint3 direction){if(origin==null||direction==null)throw new ArgumentNullException("ray views");Origin00=origin;Direction18=direction;}
 }
 public sealed class CameraWorldRayContext {
  public readonly EvidenceCameraBoundaries Camera;
  public readonly WorldCoordinateState Regions;
  public readonly SystemUcrtCameraMath Math;
  public readonly Field<uint> Half34BADC=new Field<uint>(),One34BB44=new Field<uint>(),Fallback338458=new Field<uint>();
  readonly Dictionary<PortalCamera,CameraImageState> images=new Dictionary<PortalCamera,CameraImageState>();
  readonly Dictionary<PortalCameraVTable,CameraCoordinatesGetter> actualTargets=new Dictionary<PortalCameraVTable,CameraCoordinatesGetter>();
  public int LastRayGetterCount{get;private set;}
  public CameraCoordinates32 LastRayFirstHidden{get;private set;}
  public CameraCoordinates32 LastRaySecondHidden{get;private set;}
  public CameraCoordinates32 LastRayFirstReturned{get;private set;}
  public CameraCoordinates32 LastRaySecondReturned{get;private set;}
  public CameraWorldRayContext(EvidenceCameraBoundaries camera,WorldCoordinateState regions,SystemUcrtCameraMath math=null){
   if(camera==null||regions==null)throw new ArgumentNullException("camera/regions");
   if(camera.GetType()!=typeof(EvidenceCameraBoundaries)||camera.Target.GetType()!=typeof(CameraTargetBoundaries))throw new ArgumentException("This composition requires the concrete observed camera and target implementation");
   Camera=camera;Regions=regions;Math=math;
  }
  public void Attach(CameraImageState state){
   if(state==null)throw new ArgumentNullException("state");
   if(!Object.ReferenceEquals(Camera.CameraState(state.Shared.Identity,0),state.Shared))throw new InvalidOperationException("Camera image view must borrow the registered camera state");
   CameraImageState old;if(images.TryGetValue(state.Shared.Identity,out old)&&!Object.ReferenceEquals(old,state))throw new InvalidOperationException("Camera already has a different image state");images[state.Shared.Identity]=state;
  }
  public void BindObservedGameCamera(PortalCameraVTable table){
   Camera.BindObservedGameCamera(table);actualTargets[table]=Camera.Slot18Target(table,0);
  }
  public void UseObservedConstants(){Half34BADC.Write(0x3f000000);One34BB44.Write(0x3f800000);Fallback338458.Write(0x7f800000);}
  internal CameraImageState Image(PortalCamera camera,uint at){CameraImageState state;if(camera==null||!images.TryGetValue(camera,out state))throw new MissingContract(at,"Camera image state has no explicit canonical binding");return state;}
  internal void BeginRay(){LastRayGetterCount=0;LastRayFirstHidden=null;LastRaySecondHidden=null;LastRayFirstReturned=null;LastRaySecondReturned=null;}
  internal CameraCoordinates32 Target(PortalCamera camera,PortalCameraVTable table,uint at){
   var actual=Camera.Slot18Target(table,at);CameraCoordinatesGetter expected;
   if(table==null||!actualTargets.TryGetValue(table,out expected)||!actual.Equals(expected))throw new MissingContract(at,"Camera slot18 is not the bound observed GameGetTarget implementation");
   var hidden=new CameraCoordinates32();LastRayGetterCount++;
   if(LastRayGetterCount==1)LastRayFirstHidden=hidden;else LastRaySecondHidden=hidden;
   var returned=actual(camera,hidden);
   if(LastRayGetterCount==1)LastRayFirstReturned=returned;else LastRaySecondReturned=returned;
   return returned;
  }
 }
 // Engine044A10/229700: retained r1-engine-mouse-core05 and r1-engine-input08,
 // SHA2564c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // This own finite-result binary32 domain models ordinary masked FP arithmetic.
 // Nonfinite operation results stop explicitly at their operation, not through
 // a fabricated native guard/return. Native traps, FTZ/DAZ, NaN payloads,
 // concurrent mutation and arbitrary byte overlaps are outside this adapter.
 public static class CameraWorldRayContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own camera/ray view");return value;}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static float F(float value,uint at){float result=BitConverter.ToSingle(BitConverter.GetBytes(value),0);if(Single.IsNaN(result)||Single.IsInfinity(result))throw new MissingContract(at,"Own finite camera/ray arithmetic domain exceeded; not a native error return or zero-length guard");return result;}
  sealed class Scalar {
   readonly Field<float> scalar;readonly Field<uint> word;readonly Field<ulong> pair;readonly bool high;
   internal Scalar(Field<float> value){scalar=value.Snapshot();}
   internal Scalar(Field<uint> value){word=value.Snapshot();}
   internal Scalar(Field<ulong> value,bool high){pair=value.Snapshot();this.high=high;}
   internal float Read(uint at){if(scalar!=null)return scalar.Read(at);if(word!=null)return Float(word.Read(at));ulong raw=pair.Read(at);return Float(unchecked((uint)(high?raw>>32:raw)));}
  }
  static Scalar CameraFloat(CameraImageState state,int relative,uint at){
   Need(state,at);if(relative>=4&&relative<=0x24&&relative%4==0)return new Scalar(state.Shared.Basis70[(relative-4)/4]);
   if(relative==0x28||relative==0x2c)return new Scalar(state.Shared.Position94,relative==0x2c);
   if(relative==0x30)return new Scalar(state.Shared.Position9C);
   throw new ArgumentOutOfRangeException("relative");
  }
  static float Constant(Field<uint> cell,uint at){return Float(cell.Read(at));}
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){
   Need(point,at).RequireLive(at);
   try{return axis==0?point.X:axis==1?point.Y:point.Z;}
   catch(MissingContract e){throw new MissingContract(at,"Own output component outside declared extent",e);}
  }
  public static NavigationPoint3 GetImagePoint044A10(CameraImageState camera,NavigationPoint3 output,CameraImagePoint2 input,CameraViewport viewport,CameraWorldRayContext context,Invocation invocation){
   var i=Need(invocation,0x044a24);i.At=0x044a24;int mode=Need(camera,i.At).Mode6C.Read(i.At);
   if(mode!=0&&mode!=1){i.At=0x044be2;var dx=Cell(output,0,i.At);var dy=Cell(output,1,i.At);dx.Write(0);dy.Write(0);i.At=0x044be5;Cell(output,2,i.At).Write(0);return output;}
   float u,v,bx,by,bz;Scalar sourceX,sourceY;
   if(mode==0){
    i.At=0x044a4a;var fov=camera.FovA0.Snapshot();i.At=0x044a4f;float angle=F(fov.Read(i.At)*Constant(Need(context,i.At).Half34BADC,i.At),i.At);
    i.At=0x044a57;float tangent=Need(context.Math,i.At).Tan(angle,i.At);
    i.At=0x044a5c;var one=context.One34BB44.Snapshot();
    i.At=0x044a68;var height=Need(viewport,i.At).Height0C.Snapshot();i.At=0x044a6d;var width=viewport.Width08.Snapshot();
    i.At=0x044a72;sourceX=new Scalar(Need(input,i.At).X0);i.At=0x044a76;sourceY=new Scalar(input.Y4);
    i.At=0x044a7b;float xx=sourceX.Read(i.At);xx=F(xx+xx,i.At);
    i.At=0x044a7f;float h=F((float)height.Read(i.At),i.At);i.At=0x044a82;float w=F((float)width.Read(i.At),i.At);
    i.At=0x044a85;float yy=sourceY.Read(i.At);yy=F(yy+yy,i.At);
    i.At=0x044a89;u=F(w/h,i.At);i.At=0x044a91;v=F(Constant(one,i.At)-yy,i.At);
    i.At=0x044a96;var baseX=CameraFloat(camera,0x1c,i.At);i.At=0x044a9b;xx=F(Constant(one,i.At)-xx,i.At);
    i.At=0x044a9f;bx=F(baseX.Read(i.At)+CameraFloat(camera,0x28,i.At).Read(i.At),i.At);
    i.At=0x044aa4;var baseZ=CameraFloat(camera,0x30,i.At);i.At=0x044aa9;bz=F(baseZ.Read(i.At)+CameraFloat(camera,0x24,i.At).Read(i.At),i.At);
    i.At=0x044aae;u=F(u*tangent,i.At);i.At=0x044ab2;v=F(v*tangent,i.At);i.At=0x044ab7;u=F(u*xx,i.At);
    i.At=0x044abb;var baseY=CameraFloat(camera,0x2c,i.At);i.At=0x044ac4;by=F(baseY.Read(i.At)+CameraFloat(camera,0x20,i.At).Read(i.At),i.At);
    i.At=0x044ac9;float vy=F(v*CameraFloat(camera,0x10,i.At).Read(i.At),i.At);
    i.At=0x044ad1;float ux=F(u*CameraFloat(camera,4,i.At).Read(i.At),i.At);
    i.At=0x044ad9;float uy=F(u*CameraFloat(camera,8,i.At).Read(i.At),i.At);
    i.At=0x044ae2;u=F(u*CameraFloat(camera,0xc,i.At).Read(i.At),i.At);
    i.At=0x044ae7;bx=F(bx+ux,i.At);i.At=0x044aeb;float vv=F(v*CameraFloat(camera,0x14,i.At).Read(i.At),i.At);
    i.At=0x044af0;by=F(by+uy,i.At);i.At=0x044af4;v=F(v*CameraFloat(camera,0x18,i.At).Read(i.At),i.At);
    i.At=0x044afa;bz=F(bz+u,i.At);i.At=0x044afe;bx=F(bx+vy,i.At);i.At=0x044b02;by=F(by+vv,i.At);i.At=0x044b06;bz=F(bz+v,i.At);
    i.At=0x044b0b;Cell(output,0,i.At).Write(bx);i.At=0x044b0f;Cell(output,1,i.At).Write(by);i.At=0x044b14;Cell(output,2,i.At).Write(bz);return output;
   }
   i.At=0x044b27;var unit=Need(context,i.At).One34BB44.Snapshot();
   i.At=0x044b30;sourceX=new Scalar(Need(input,i.At).X0);i.At=0x044b39;var yBase=CameraFloat(camera,0x2c,i.At);
   i.At=0x044b3e;float doubledX=sourceX.Read(i.At);doubledX=F(doubledX+doubledX,i.At);
   i.At=0x044b42;by=F(yBase.Read(i.At)+CameraFloat(camera,0x20,i.At).Read(i.At),i.At);
   i.At=0x044b47;var zBase=CameraFloat(camera,0x30,i.At);i.At=0x044b4c;bz=F(zBase.Read(i.At)+CameraFloat(camera,0x24,i.At).Read(i.At),i.At);
   i.At=0x044b51;u=F(Constant(unit,i.At)-doubledX,i.At);i.At=0x044b55;sourceY=new Scalar(input.Y4);
   i.At=0x044b5b;float doubledY=sourceY.Read(i.At);doubledY=F(doubledY+doubledY,i.At);
   i.At=0x044b5f;u=F(u*camera.WidthA4.Read(i.At),i.At);i.At=0x044b64;v=F(Constant(unit,i.At)-doubledY,i.At);
   i.At=0x044b69;var xBase=CameraFloat(camera,0x28,i.At);i.At=0x044b6e;bx=F(xBase.Read(i.At)+CameraFloat(camera,0x1c,i.At).Read(i.At),i.At);
   i.At=0x044b73;u=F(u*Constant(context.Half34BADC,i.At),i.At);i.At=0x044b7b;v=F(v*camera.HeightA8.Read(i.At),i.At);
   i.At=0x044b87;float projectedX=F(u*CameraFloat(camera,4,i.At).Read(i.At),i.At);
   i.At=0x044b8c;v=F(v*Constant(context.Half34BADC,i.At),i.At);i.At=0x044b95;float horizontalY=F(u*CameraFloat(camera,8,i.At).Read(i.At),i.At);
   i.At=0x044b9a;bx=F(bx+projectedX,i.At);i.At=0x044b9e;u=F(u*CameraFloat(camera,0xc,i.At).Read(i.At),i.At);
   i.At=0x044ba7;projectedX=F(v*CameraFloat(camera,0x10,i.At).Read(i.At),i.At);i.At=0x044bb0;float projectedY=F(v*CameraFloat(camera,0x14,i.At).Read(i.At),i.At);
   i.At=0x044bb5;by=F(by+horizontalY,i.At);i.At=0x044bb9;v=F(v*CameraFloat(camera,0x18,i.At).Read(i.At),i.At);
   i.At=0x044bbf;bz=F(bz+u,i.At);i.At=0x044bc3;bx=F(bx+projectedX,i.At);i.At=0x044bc7;by=F(by+projectedY,i.At);i.At=0x044bcb;bz=F(bz+v,i.At);
   i.At=0x044bd0;Cell(output,0,i.At).Write(bx);i.At=0x044bd4;Cell(output,1,i.At).Write(by);i.At=0x044bd9;Cell(output,2,i.At).Write(bz);return output;
  }
  public static WorldRayObserved36 GetRay229700(PortalCamera camera,WorldRayObserved36 output,CameraImagePoint2 input,CameraViewport viewport,CameraWorldRayContext context,Invocation invocation){
   var i=Need(invocation,0x22971c);i.At=0x22971c;var origin=Need(output,i.At).Origin00;origin.Q0.Write(0);
   i.At=0x22972f;var table=Need(camera,i.At).Virtual00.Snapshot();
   i.At=0x22973b;Need(context,i.At).BeginRay();var first=context.Target(camera,table.Read(i.At),i.At);
   i.At=0x22973e;var shared=context.Camera.CameraState(camera,i.At);var xy=shared.Position94.Snapshot();
   i.At=0x229757;var firstToken=Need(first,i.At).Q0.Snapshot();i.At=0x22975a;var z=shared.Position9C.Snapshot();
   // The local qword loaded at229764 contains the just-written Z DWORD and
   // an unwritten high DWORD. Preserve Unknown independently through the copy.
   var padding=new Field<uint>();
   i.At=0x229774;origin.Q0.CopyFrom(firstToken);CopyPairBits(xy,origin.D8,origin.DC);
   i.At=0x229778;origin.D10.CopyFrom(z);origin.D14.CopyFrom(padding);
   i.At=0x22977e;table=camera.Virtual00.Snapshot();i.At=0x229781;var second=context.Target(camera,table.Read(i.At),i.At);
   i.At=0x229793;var secondToken=Need(second,i.At).Q0.Snapshot();
   i.At=0x229796;var image=GetImagePoint044A10(context.Image(camera,i.At),new NavigationPoint3(),input,viewport,context,i);
   i.At=0x22979b;var originToken=origin.Q0.Snapshot();i.At=0x22979e;var imageX=Cell(image,0,i.At).Snapshot();var imageY=Cell(image,1,i.At).Snapshot();
   i.At=0x2297a2;var imageZ=Cell(image,2,i.At).Snapshot();
   float x,y,dz;Field<uint> fallback=null;i.At=0x2297af;ulong destinationToken=secondToken.Read(i.At);ulong sourceToken=0;
   bool noRegion=destinationToken==0;if(!noRegion){i.At=0x2297b4;sourceToken=originToken.Read(i.At);noRegion=sourceToken==0;}
   // The scalar MOV copies fallback bits opaquely. Local placeholders below
   // are never consumed: each register obtains its captured bits at MULSS.
   if(noRegion){i.At=0x229815;fallback=context.Fallback338458.Snapshot();x=0;y=0;dz=0;}
   else{
    i.At=0x2297b9;var targetRegion=context.Regions.Region(destinationToken,i.At);var ox=targetRegion.Offset3C.Snapshot();
    i.At=0x2297bc;var sourceRegion=context.Regions.Region(sourceToken,i.At);int deltaX=unchecked(ox.Read(i.At)-sourceRegion.Offset3C.Read(i.At));
    i.At=0x2297c6;var oy=targetRegion.Offset40.Snapshot();i.At=0x2297c9;int deltaY=unchecked(oy.Read(i.At)-sourceRegion.Offset40.Read(i.At));
    i.At=0x2297cd;x=F(imageX.Read(i.At)-Float(origin.D8.Read(i.At)),i.At);
    i.At=0x2297d9;var oz=targetRegion.Offset44.Snapshot();i.At=0x2297dc;y=F(imageY.Read(i.At)-Float(origin.DC.Read(i.At)),i.At);
    i.At=0x2297e2;int deltaZ=unchecked(oz.Read(i.At)-sourceRegion.Offset44.Read(i.At));i.At=0x2297ec;dz=F(imageZ.Read(i.At)-Float(origin.D10.Read(i.At)),i.At);
    i.At=0x2297f6;float converted=F((float)deltaX,i.At);i.At=0x2297f9;x=F(x+converted,i.At);
    i.At=0x229801;converted=F((float)deltaY,i.At);i.At=0x229804;y=F(y+converted,i.At);
    i.At=0x22980c;converted=F((float)deltaZ,i.At);i.At=0x22980f;dz=F(dz+converted,i.At);
   }
   i.At=0x229823;var one=context.One34BB44.Snapshot();
   i.At=0x22984c;if(fallback!=null)x=Constant(fallback,i.At);float xx=F(x*x,i.At);
   i.At=0x229850;if(fallback!=null)y=Constant(fallback,i.At);float yy=F(y*y,i.At);
   i.At=0x229854;if(fallback!=null)dz=Constant(fallback,i.At);float zz=F(dz*dz,i.At);
   i.At=0x229858;float sum=F(yy+xx,i.At);i.At=0x22985f;sum=F(sum+zz,i.At);
   i.At=0x229863;float length=F((float)System.Math.Sqrt(sum),i.At);i.At=0x229867;float inverse=F(Constant(one,i.At)/length,i.At);
   i.At=0x22986b;y=F(y*inverse,i.At);i.At=0x22986f;x=F(x*inverse,i.At);i.At=0x229873;dz=F(dz*inverse,i.At);
   i.At=0x22988c;var dx=Cell(output.Direction18,0,i.At);var dy=Cell(output.Direction18,1,i.At);dx.Write(x);dy.Write(y);
   i.At=0x229892;Cell(output.Direction18,2,i.At).Write(dz);return output;
  }
  static void CopyPairBits(Field<ulong> source,Field<uint> low,Field<uint> high){
   // Field<T> exposes no known-bit predicate. An opaque copy of an unknown
   // qword makes both projected DWORDs unknown; it must not throw or zero them.
   ulong bits;try{bits=source.Read(0);}catch(MissingContract){low.Forget();high.Forget();return;}
   low.Write(unchecked((uint)bits));high.Write(unchecked((uint)(bits>>32)));
  }
 }
}
