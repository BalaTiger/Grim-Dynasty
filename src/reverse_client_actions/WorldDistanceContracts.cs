using System;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class WorldDistanceInvocation {
  public readonly Invocation Native=new Invocation();
  public Field<ulong> FirstRegion{get;private set;}public Field<ulong> SecondRegion{get;private set;}
  public Field<float> DeltaX{get;private set;}public Field<float> DeltaY{get;private set;}public Field<float> DeltaZ{get;private set;}
  public Field<float> SquareX{get;private set;}public Field<float> SquareY{get;private set;}public Field<float> SquareZ{get;private set;}public Field<float> SumXY{get;private set;}
  public Field<float> Result{get;private set;}
  public bool FirstCaptured,SecondCaptured,FirstTested,SecondTested,NullRegion,SameRegion,CrossRegionEntered,Returned;
  public WorldDistanceInvocation(){Begin();}
  internal void Begin(){
   // Retained observations may be explicit later inputs. Reset diagnostics by
   // replacing their cells, never by mutating the caller's borrowed old cells.
   Native.At=0;FirstRegion=new Field<ulong>();SecondRegion=new Field<ulong>();DeltaX=new Field<float>();DeltaY=new Field<float>();DeltaZ=new Field<float>();SquareX=new Field<float>();SquareY=new Field<float>();SquareZ=new Field<float>();SumXY=new Field<float>();Result=new Field<float>();
   FirstCaptured=SecondCaptured=FirstTested=SecondTested=NullRegion=SameRegion=CrossRegionEntered=Returned=false;
  }
 }
 // Engine224770, r1-move-world-distance-run1/04..05. This receiver is unused
 // in the supported null/same-token branches; no World/Region registry is read.
 // Result is a value projection, not a borrowed native return-address identity.
 // shortcut: ordinary binary32 rounding only; native MXCSR/traps/FTZ/DAZ and
 // arithmetic NaN payload selection are outside this OWN scalar domain.
 public static class WorldDistanceContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing own WorldDistance operand address");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Read(Field<uint> bits,uint at){return BitConverter.ToSingle(BitConverter.GetBytes(bits.Read(at)),0);}
  static Field<float> Return(WorldDistanceInvocation c){c.Returned=true;c.Native.At=0x2249b6;return c.Result.Snapshot();}
  public static Field<float> GetDistanceSquared224770(WorldPosition24 first,WorldPosition24 second,Field<float> nullValue338458,WorldDistanceInvocation invocation){
   var c=Need(invocation,0x224781);c.Begin();var i=c.Native;
   At(i,0x224781);c.FirstRegion.CopyFrom(Need(first,i.At).Q0);c.FirstCaptured=true;
   At(i,0x224787);c.SecondRegion.CopyFrom(Need(second,i.At).Q0);c.SecondCaptured=true;
   ulong a=c.FirstRegion.Read(At(i,0x22478d));c.FirstTested=true;ulong b=0;
   if(a!=0){b=c.SecondRegion.Read(At(i,0x224796));c.SecondTested=true;}
   if(a==0||b==0){
    c.NullRegion=true;At(i,0x22499a);c.Result.CopyFrom(Need(nullValue338458,i.At));return Return(c);
   }
   At(i,0x2247a4);if(a!=b){c.CrossRegionEntered=true;At(i,0x2247fb);throw new MissingContract(i.At,"Different nonzero regions require actual22C240 and portal processing; this component does not substitute straight-line distance");}
   c.SameRegion=true;
   At(i,0x2247a9);var x=first.D8.Snapshot();At(i,0x2247ae);c.DeltaX.Write(F(Read(x,i.At)-Read(second.D8,i.At)));
   At(i,0x2247b4);var y=first.DC.Snapshot();At(i,0x2247b9);c.DeltaY.Write(F(Read(y,i.At)-Read(second.DC,i.At)));
   At(i,0x2247bf);var z=first.D10.Snapshot();At(i,0x2247c4);c.DeltaZ.Write(F(Read(z,i.At)-Read(second.D10,i.At)));
   At(i,0x2247ca);float dx=c.DeltaX.Read(i.At);c.SquareX.Write(F(dx*dx));
   At(i,0x2247ce);float dy=c.DeltaY.Read(i.At);c.SquareY.Write(F(dy*dy));
   At(i,0x2247d2);float dz=c.DeltaZ.Read(i.At);c.SquareZ.Write(F(dz*dz));
   At(i,0x2247d6);c.SumXY.Write(F(c.SquareX.Read(i.At)+c.SquareY.Read(i.At)));
   At(i,0x2247da);c.Result.Write(F(c.SumXY.Read(i.At)+c.SquareZ.Read(i.At)));return Return(c);
  }
 }
}
