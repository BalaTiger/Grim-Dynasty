using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Engine2290E0 uses the same camera and actual Game target getter as GetRay,
 // but does not normalize a ray or resolve a region. D14 is never written.
 // Evidence: r1-engine-worldcamera-imagepoint-run1/05 assembly, de2427897804...
 public static class WorldCameraImageContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own image view");return value;}
  static float F(float value,uint at){float rounded=BitConverter.ToSingle(BitConverter.GetBytes(value),0);if(Single.IsNaN(rounded)||Single.IsInfinity(rounded))throw new MissingContract(at,"Own finite binary32 image domain exceeded; not a native guard or error return");return rounded;}
  // MOV copies unknown bits without requiring their arithmetic interpretation.
  static Field<uint> Bits(Field<float> source){var result=new Field<uint>();try{result.Write(BitConverter.ToUInt32(BitConverter.GetBytes(source.Read(0)),0));}catch(MissingContract){}return result;}

  public static WorldPosition24 GetImagePoint2290E0(PortalCamera camera,WorldPosition24 output,CameraImagePoint2 input,CameraViewport viewport,CameraWorldRayContext context,Invocation invocation){
   var i=Need(invocation,0x2290fa);i.At=0x2290fa;var table=Need(camera,i.At).Virtual00.Snapshot();
   var hidden=new CameraCoordinates32();
   i.At=0x22910e;var getter=Need(context,i.At).CheckedTarget(table.Read(i.At),i.At);var returned=getter(camera,hidden);
   i.At=0x229120;var token=Need(returned,i.At).Q0.Snapshot();
   i.At=0x229123;var image=CameraWorldRayContracts.GetImagePoint044A10(context.Image(camera,i.At),new NavigationPoint3(),input,viewport,context,i);
   i.At=0x229137;image.RequireLive(i.At);var x=Bits(image.X.Snapshot());var y=Bits(image.Y.Snapshot());
   i.At=0x22913b;var z=Bits(image.Z.Snapshot());
   i.At=0x22913e;Need(output,i.At).D8.CopyFrom(x);output.DC.CopyFrom(y);
   i.At=0x229144;output.D10.CopyFrom(z);
   i.At=0x22914b;output.Q0.CopyFrom(token);return output;
  }

  // Only the EXE2176C..217B6 arithmetic slice, entered after its preceding
  // checks. Pixel input and viewport share typed cells; no clamp or defaults.
  // Ordinary finite masked arithmetic only; MXCSR traps/FTZ/DAZ and arbitrary
  // byte overlap remain outside this own adapter, as in Camera044A10.
  public static CameraImagePoint2 Normalize2176C(CameraImagePoint2 pixels,CameraViewport viewport,CameraImagePoint2 output,Invocation invocation){
   var i=Need(invocation,0x2176c);i.At=0x2176c;var originX=Need(viewport,i.At).OriginX00.Snapshot();
   i.At=0x21772;float ox=F((float)originX.Read(i.At),i.At);
   i.At=0x21775;var px=Need(pixels,i.At).X0.Snapshot();
   i.At=0x2177c;float x=F(px.Read(i.At)-ox,i.At);
   i.At=0x21780;var width=viewport.Width08.Snapshot();i.At=0x21786;float w=F((float)width.Read(i.At),i.At);
   i.At=0x21789;x=F(x/w,i.At);
   i.At=0x2178d;var originY=viewport.OriginY04.Snapshot();i.At=0x21793;float oy=F((float)originY.Read(i.At),i.At);
   i.At=0x21796;var py=pixels.Y4.Snapshot();i.At=0x2179d;float y=F(py.Read(i.At)-oy,i.At);
   i.At=0x217a1;var height=viewport.Height0C.Snapshot();i.At=0x217a7;float h=F((float)height.Read(i.At),i.At);
   i.At=0x217aa;y=F(y/h,i.At);
   i.At=0x217ae;Need(output,i.At).X0.Write(x);i.At=0x217b6;output.Y4.Write(y);return output;
  }
 }
}
