using System;

namespace GrimDynasty.ReverseClient.Actions {
 // A borrowed input, not a mirrored coordinate. Ordinary points use the
 // established NavigationPoint3 cells. The explicit XYZ slice projects the
 // actual WorldPosition24 DWORD cells at +8/+C/+10, including same-target
 // aliases. Raw Q0 reinterpretation, other shifted overlaps, concurrent writes
 // and native invalid-pointer behavior are outside this own typed domain.
 public sealed class WorldPositionInput3 {
  readonly NavigationPoint3 point;
  readonly WorldPosition24 position;
  readonly bool xyzSlice;
  public WorldPositionInput3(NavigationPoint3 point){this.point=point;}
  WorldPositionInput3(WorldPosition24 position){this.position=position;xyzSlice=true;}
  public static WorldPositionInput3 BorrowLocalXYZ(WorldPosition24 position){return new WorldPositionInput3(position);}
  internal sealed class Scalar {
   readonly Field<float> value;
   readonly Field<uint> bits;
   internal Scalar(Field<float> value){this.value=value;}
   internal Scalar(Field<uint> bits){this.bits=bits;}
   internal float Read(uint at){return bits==null?value.Read(at):BitConverter.ToSingle(BitConverter.GetBytes(bits.Read(at)),0);}
  }
  internal Scalar Capture(int component,uint at){
   if(xyzSlice){
    if(position==null)throw new MissingContract(at,"Null own borrowed WorldPosition24 XYZ view");
    return new Scalar((component==0?position.D8:component==1?position.DC:position.D10).Snapshot());
   }
   if(point==null)throw new MissingContract(at,"Null own borrowed world-point view");
   point.RequireLive(at);
   try{return new Scalar((component==0?point.X:component==1?point.Y:point.Z).Snapshot());}
   catch(MissingContract error){throw new MissingContract(at,"Borrowed world-point component is outside its declared own extent",error);}
  }
 }

 // Engine22C690, retained r1-world-position-and-level-initialization04..06.
 // Engine SHA2564c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Native MOVs preserve unknown scalar payloads until arithmetic consumes them.
 // Signed32 conversion and each SUBSS are rounded separately to binary32 in
 // the ordinary round-to-nearest, masked-FP own domain. Native FP exception
 // flags/traps, alternate MXCSR modes and NaN payload choices are not emulated.
 public static class WorldPositionInitializationContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own world-position initialization view");return value;}
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  public static byte SetFromWorldPosition22C690(WorldPosition24 destination,NavigationPoint3 worldPoint,ulong regionToken,WorldCoordinateBoundaries boundaries,Invocation invocation){
   return SetFromWorldPosition22C690(destination,new WorldPositionInput3(worldPoint),regionToken,boundaries,invocation);
  }
  public static byte SetFromWorldPosition22C690(WorldPosition24 destination,WorldPositionInput3 worldPoint,ulong regionToken,WorldCoordinateBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x22c694);i.At=0x22c694;Need(destination,i.At).Q0.Write(regionToken);
   i.At=0x22c6ac;destination.D8.Write(0);destination.DC.Write(0); // One native qword store.
   i.At=0x22c6b1;destination.D10.Write(0); // D14 is never touched.
   i.At=0x22c6b4;var inputX=Need(worldPoint,i.At).Capture(0,i.At);
   // No eager region lookup: its first actual load follows source.X capture.
   // Resolve only the existing canonical shared WorldRegion, never a new view.
   i.At=0x22c6b8;var region=Need(boundaries,i.At).State.Region(regionToken,i.At);var offsetX=region.Offset3C.Snapshot();
   i.At=0x22c6be;var offsetY=region.Offset40.Snapshot();
   i.At=0x22c6c4;float xOffset=F((float)offsetX.Read(i.At));
   i.At=0x22c6c7;float yOffset=F((float)offsetY.Read(i.At));
   i.At=0x22c6ca;float x=F(inputX.Read(i.At)-xOffset);
   i.At=0x22c6ce;var inputY=worldPoint.Capture(1,i.At);
   i.At=0x22c6d3;float y=F(inputY.Read(i.At)-yOffset);
   i.At=0x22c6d7;var inputZ=worldPoint.Capture(2,i.At);
   var delta=new Displacement3();i.At=0x22c6e1;delta.X0.Write(x);
   i.At=0x22c6e7;var offsetZ=region.Offset44.Snapshot();
   i.At=0x22c6f0;float zOffset=F((float)offsetZ.Read(i.At));
   i.At=0x22c6f3;delta.Y4.Write(y);
   i.At=0x22c6f9;float z=F(inputZ.Read(i.At)-zOffset);
   i.At=0x22c6fd;delta.Z8.Write(z);
   // Preserve the real Translate path, including binary32 zero addition,
   // its bounds decision and its existing unsupported cross-region prefix.
   i.At=0x22c703;return WorldCoordinateContracts.TranslateTrue22C350(destination,delta,boundaries,i);
  }
 }
}
