using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Projection of Game08DB04..08DCBE only. The caller supplies the hidden
 // 60-byte frame from08DAFE. The original owner field is captured at08DC51;
 // no camera, owner classification or native callback is invoked here.
 // Scalar arithmetic rounds after every MULSS/ADDSS. Native MXCSR flags,
 // traps, FTZ/DAZ and NaN payload equivalence are outside this managed model.
 public static class PortalFrameMath {
  static float F32(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Read(Field<float> field,uint at,Invocation i){i.At=at;return field.Read(at);}
  static void Write(Field<float> field,float value,uint at,Invocation i){i.At=at;field.Write(value);}
  public static object TransformOwner(CoordinateFrame60 frame,Waypoint80 local,Field<object> owner00,Invocation i){
   i.At=0x08db04;
   if(frame==null)throw new MissingContract(i.At,"Null hidden coordinate frame");
   // Preserve register order, including reads of later vectors before earlier
   // vector stores. This also preserves the prefix at an unknown-field stop.
   float f6=Read(frame.Basis18[1],0x08db04,i);
   float f5=Read(frame.Basis18[0],0x08db09,i);
   float f4=Read(frame.Basis18[2],0x08db0e,i);
   i.At=0x08db13;
   if(local==null)throw new MissingContract(i.At,"Null local waypoint");
   var position0=local.Position08.Q0.Snapshot();
   var position8=local.Position08.Q8.Snapshot();
   float f13=Read(local.Matrix20[0],0x08db18,i);
   float f7=Read(local.Matrix20[3],0x08db1f,i);
   i.At=0x08db25;var position16=local.Position08.Q16.Snapshot();
   float f12=Read(local.Matrix20[6],0x08db2b,i);
   float f15=Read(local.Matrix20[4],0x08db32,i);
   float f11=Read(local.Matrix20[1],0x08db39,i);
   float f3=f15;
   float f10=Read(local.Matrix20[7],0x08db44,i);
   float f14=Read(local.Matrix20[5],0x08db4b,i);
   float f9=Read(local.Matrix20[2],0x08db52,i);
   float f2=f14;
   float f8=Read(local.Matrix20[8],0x08db5d,i);
   i.At=0x08db64;frame.Position00.Q0.CopyFrom(position0);frame.Position00.Q8.CopyFrom(position8);
   float f0=f13;
   f0=F32(f0*f5); //08DB6D
   i.At=0x08db71;frame.Position00.Q16.CopyFrom(position16);
   float f1=f12;
   f1=F32(f1*f4); //08DB7A
   f7=F32(f7*f6); //08DB7E
   f2=F32(f2*f6); //08DB82
   f7=F32(f7+f0); //08DB86
   f3=F32(f3*f6); //08DB8A
   f6=Read(frame.Basis18[4],0x08db8e,i);
   f0=f11;
   f0=F32(f0*f5); //08DB97
   f7=F32(f7+f1); //08DB9B
   f1=f10;
   f1=F32(f1*f4); //08DBA3
   f3=F32(f3+f0); //08DBA7
   f0=f9;
   f0=F32(f0*f5); //08DBAF
   f5=Read(frame.Basis18[3],0x08dbb3,i);
   Write(frame.Basis18[0],f7,0x08dbb8,i);
   f7=Read(local.Matrix20[3],0x08dbbd,i);
   f3=F32(f3+f1); //08DBC3
   f2=F32(f2+f0); //08DBC7
   f7=F32(f7*f6); //08DBCB
   f1=f8;
   f0=f13;
   f1=F32(f1*f4); //08DBD7
   f4=f15;
   f0=F32(f0*f5); //08DBDF
   f2=F32(f2+f1); //08DBE3
   f4=F32(f4*f6); //08DBE7
   f1=f12;
   Write(frame.Basis18[1],f3,0x08dbef,i);
   f3=Read(frame.Basis18[5],0x08dbf4,i);
   f7=F32(f7+f0); //08DBF9
   f1=F32(f1*f3); //08DBFD
   f0=f11;
   f0=F32(f0*f5); //08DC05
   f7=F32(f7+f1); //08DC09
   Write(frame.Basis18[2],f2,0x08dc0d,i);
   f1=f10;
   f2=f14;
   f4=F32(f4+f0); //08DC1A
   f1=F32(f1*f3); //08DC1E
   f0=f9;
   f2=F32(f2*f6); //08DC26
   f0=F32(f0*f5); //08DC2A
   f4=F32(f4+f1); //08DC2E
   f1=f8;
   f1=F32(f1*f3); //08DC36
   f2=F32(f2+f0); //08DC3A
   f3=Read(frame.Basis18[7],0x08dc3e,i);
   f2=F32(f2+f1); //08DC43
   f1=Read(frame.Basis18[6],0x08dc47,i);
   f0=Read(frame.Basis18[8],0x08dc4c,i);
   i.At=0x08dc51;
   if(owner00==null)throw new MissingContract(i.At,"Null owner field view");
   object capturedOwner=owner00.Read(0x08dc51);
   f15=F32(f15*f3); //08DC54
   Write(frame.Basis18[5],f2,0x08dc59,i);
   f2=Read(local.Matrix20[3],0x08dc5e,i);
   f2=F32(f2*f3); //08DC64
   f14=F32(f14*f3); //08DC68
   f13=F32(f13*f1); //08DC6D
   f11=F32(f11*f1); //08DC72
   f9=F32(f9*f1); //08DC77
   f2=F32(f2+f13); //08DC7C
   f12=F32(f12*f0); //08DC81
   f15=F32(f15+f11); //08DC86
   f10=F32(f10*f0); //08DC8B
   f14=F32(f14+f9); //08DC90
   f8=F32(f8*f0); //08DC95
   f2=F32(f2+f12); //08DC9A
   f15=F32(f15+f10); //08DC9F
   Write(frame.Basis18[3],f7,0x08dca4,i);
   Write(frame.Basis18[4],f4,0x08dca9,i);
   f14=F32(f14+f8); //08DCAE
   Write(frame.Basis18[6],f2,0x08dcb3,i);
   Write(frame.Basis18[7],f15,0x08dcb8,i);
   Write(frame.Basis18[8],f14,0x08dcbe,i);
   return capturedOwner;
  }
 }
}
