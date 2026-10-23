using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Engine22C240, exact r1-engine-mouse-getters-run1/14. Integer offsets are
 // subtracted modulo32 BEFORE signed conversion; local floats are subtracted
 // BEFORE adding that converted difference. This is not world-float subtraction.
 // The null-region operand338458 is borrowed explicitly, Unknown by default;
 // its MOV-only path propagates that knowledge without guessing infinity.
 public static class WorldVectorDifferenceContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own WorldVec3 difference view");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float x){return NavigationCoreContracts.F(x);}
  static float Read(Field<uint> bits,uint at){return NavigationCoreContracts.FromBits(bits.Read(at));}
  public static Displacement3 Subtract22C240(WorldPosition24 to,Displacement3 output,WorldPosition24 from,WorldCoordinateState regions,Field<float> nullValue338458,Invocation invocation){
   var i=Need(invocation,0x22c244);var fromToken=Need(from,At(i,0x22c244)).Q0.Snapshot();var toToken=Need(to,At(i,0x22c24a)).Q0.Snapshot();
   ulong a=toToken.Read(At(i,0x22c250));ulong b=0;if(a!=0)b=fromToken.Read(At(i,0x22c259));
   if(a==0||b==0){
    At(i,0x22c2d9);var value=Need(nullValue338458,i.At).Snapshot();At(i,0x22c2e4);Need(output,i.At).X0.CopyFrom(value);At(i,0x22c2e9);output.Y4.CopyFrom(value);At(i,0x22c2ef);output.Z8.CopyFrom(value);At(i,0x22c2f9);return output;
   }
   var ar=Need(regions,At(i,0x22c25e)).Region(a,i.At);var offsetX=ar.Offset3C.Snapshot();At(i,0x22c261);var br=regions.Region(b,i.At);int dx=unchecked(offsetX.Read(i.At)-br.Offset3C.Read(i.At));
   At(i,0x22c265);var offsetY=ar.Offset40.Snapshot();At(i,0x22c268);var localX=to.D8.Snapshot();At(i,0x22c26e);float x=F(Read(localX,i.At)-Read(from.D8,i.At));
   At(i,0x22c274);int dy=unchecked(offsetY.Read(i.At)-br.Offset40.Read(i.At));At(i,0x22c278);var localY=to.DC.Snapshot();At(i,0x22c27e);var offsetZ=ar.Offset44.Snapshot();At(i,0x22c281);float y=F(Read(localY,i.At)-Read(from.DC,i.At));
   At(i,0x22c287);int dz=unchecked(offsetZ.Read(i.At)-br.Offset44.Read(i.At));At(i,0x22c28b);var localZ=to.D10.Snapshot();At(i,0x22c291);float z=F(Read(localZ,i.At)-Read(from.D10,i.At));
   At(i,0x22c29b);float ox=F((float)dx);At(i,0x22c29e);x=F(x+ox);At(i,0x22c2a6);float oy=F((float)dy);At(i,0x22c2a9);y=F(y+oy);At(i,0x22c2b1);float oz=F((float)dz);At(i,0x22c2b4);z=F(z+oz);
   At(i,0x22c2be);Need(output,i.At).X0.Write(x);output.Y4.Write(y);At(i,0x22c2cd);output.Z8.Write(z);At(i,0x22c2d8);return output;
  }
 }
}
