using System;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GroundBlockHit20 {
  public readonly Field<float> Distance00;
  public readonly Displacement3 Position04;
  public readonly Field<uint> Material10;
  public GroundBlockHit20():this(new Field<float>(),new Displacement3(),new Field<uint>()){}
  public GroundBlockHit20(Field<float> distance,Displacement3 position,Field<uint> material){if(distance==null||position==null||material==null)throw new ArgumentNullException("hit views");Distance00=distance;Position04=position;Material10=material;}
 }
 // An OWN view of a resource, not a resource loader or a guessed native type.
 // The raw DWORD remains Unknown until a source/fixture explicitly establishes it.
 public sealed class GroundMaterialSurfaceState {
  public readonly object Identity=new object();
  public readonly Field<uint> SurfaceFC=new Field<uint>();
 }
 public sealed class GroundTerrainIntersectionBinding {
  public readonly OwnGroundData Data;
  public readonly Field<uint> Slot58=new Field<uint>(),SlotF0=new Field<uint>(),SlotD8=new Field<uint>();
  public GroundTerrainIntersectionBinding(OwnGroundData data){if(data==null)throw new ArgumentNullException("data");Data=data;}
  // Explicit OWN selection of the pinned disk table, not an observation of a
  // loaded native vtable. Only these actual decoded bodies are implemented.
  public void SelectObservedDiskSlots(){Slot58.Write(0x1df220u);SlotF0.Write(0x1de850u);SlotD8.Write(0x1de810u);}
 }
 public sealed class GroundBlockIntersectionOperands {
  public readonly GroundTriangleOperands Geometry=new GroundTriangleOperands();
  public readonly Field<uint> WeightScale34B9CC=new Field<uint>();
  public void SelectObservedDiskOperands(){Geometry.SelectObservedDiskOperands();WeightScale34B9CC.Write(0x3b808081u);}
 }
 // Engine1D1330 + disk leaves1DE810/1DE850; primary table343B60.
 // Same saved GroundData cells, Block and candidate vector are used throughout.
 // OWN finite, nonwrapping, stable single-thread lifetime domain. No arbitrary
 // byte aliases/native ABI, runtime vtable mutation, graphics Load, material
 // resolution, spatial-tree traversal or FP exceptional behavior is claimed.
 public static class GroundBlockIntersectionContracts {
  static T Need<T>(T v,uint at)where T:class{if(v==null)throw new MissingContract(at,"Null OWN ground block view");return v;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static float Float(uint x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static float Finite(float v,uint at){if(float.IsNaN(v)||float.IsInfinity(v))throw new MissingContract(at,"OWN finite block arithmetic only; native exceptional FP unasserted");return v;}
  static uint U32(ulong v,uint at){if(v>uint.MaxValue)throw new MissingContract(at,"OWN wrapping block/weight index unsupported");return (uint)v;}
  static float Constant(Field<uint> v,uint bits,Invocation i,uint at){At(i,at);if(v.Read(at)!=bits)throw new MissingContract(at,"OWN requires explicit observed disk operand");return Float(bits);}
  static float Read(Field<float> v,Invocation i,uint at){return Finite(v.Read(At(i,at)),at);}
  static float Mul(float a,float b,Invocation i,uint at){return Finite(F(a*b),At(i,at));}
  static float Add(float a,float b,Invocation i,uint at){return Finite(F(a+b),At(i,at));}
  static GroundTerrainIntersectionBinding Receiver(GroundTerrainIntersectionBinding binding,Field<OwnGroundData> captured,Invocation i,uint at){
   At(i,at);var data=Need(captured.Read(at),at);binding=Need(binding,at);
   if(!Object.ReferenceEquals(binding.Data,data))throw new MissingContract(at,"No OWN dispatch binding for this actual Terrain identity");return binding;
  }
  static void Slot(Field<uint> slot,uint target,Invocation i,uint at){At(i,at);if(slot.Read(at)!=target)throw new MissingContract(at,"Unimplemented Terrain slot target");}
  public static Field<object> Material1DE810(OwnGroundData data,uint record,Invocation invocation){
   var i=Need(invocation,0x1de810);At(i,0x1de810);data=Need(data,i.At);
   At(i,0x1de81e);return data.TypeRecordAt(record,i.At).Material.Snapshot();
  }
  public static byte Weight1DE850(OwnGroundData data,uint record,uint x,uint z,Invocation invocation){
   var i=Need(invocation,0x1de853);At(i,0x1de853);if(record==0){At(i,0x1de857);return 255;}
   At(i,0x1de85d);data=Need(data,i.At);At(i,0x1de864);uint width=data.Dimension60.Read(i.At);
   At(i,0x1de867);if(width==0)throw new MissingContract(i.At,"OWN zero/wrapping weight width unsupported");width--;
   At(i,0x1de86d);uint row=U32((ulong)width*z,i.At);
   At(i,0x1de871);var type=data.TypeRecordAt(record,i.At);
   At(i,0x1de876);uint index=U32((ulong)row+x,i.At);
   At(i,0x1de879);return type.ReadWeight(index,i.At);
  }
  public static void Intersect1D1330(GroundBlockMaterialState state,GroundRay24 ray,GroundBlockHit20 output,GroundTerrainIntersectionBinding binding,GroundBlockIntersectionOperands operands,Invocation invocation){
   var i=Need(invocation,0x1d1340);At(i,0x1d1340);operands=Need(operands,i.At);
   float infinity=Constant(operands.Geometry.Infinity338458,0x7f800000u,i,0x1d1340);
   At(i,0x1d134e);Need(output,i.At).Distance00.Write(infinity);
   var hitZ=Field<uint>.Of(0);var hitX=Field<uint>.Of(0);
   At(i,0x1d1365);output.Material10.Write(100);
   At(i,0x03d789);var block=Need(state,i.At).Block;
   float boxDistance=GroundRayBoundsContracts.Intersection03D780(block.Box10,ray,operands.Geometry,i);
   At(i,0x1d1372);if(boxDistance<infinity){
    At(i,0x1d1377);uint column=block.ColumnB0.Read(i.At);
    At(i,0x1d137d);uint width=block.WidthB8.Read(i.At);
    At(i,0x1d1383);var captured=block.TerrainA8.Snapshot();
    At(i,0x1d138a);if(width<2)throw new MissingContract(i.At,"OWN block width underflow unsupported");width-=2;
    At(i,0x1d138d);uint depth=block.DepthBC.Read(i.At);
    At(i,0x1d1394);uint xBegin=U32((ulong)column*9,i.At);
    At(i,0x1d1398);if(depth<2)throw new MissingContract(i.At,"OWN block depth underflow unsupported");depth-=2;
    At(i,0x1d139c);uint row=block.RowB4.Read(i.At);
    At(i,0x1d13a2);uint xEnd=U32((ulong)width+xBegin,i.At);
    var receiver=Receiver(binding,captured,i,0x1d13a5);
    At(i,0x1d13a8);uint zBegin=U32((ulong)row*9,i.At);
    At(i,0x1d13b4);uint zEnd=U32((ulong)depth+zBegin,i.At);
    Slot(receiver.Slot58,0x1df220,i,0x1d13da);
    GroundTriangleIntersectionContracts.Sweep1DF220(receiver.Data,ray,xBegin,zBegin,xEnd,zEnd,new GroundIntersectionResult(output.Distance00,hitX,hitZ),operands.Geometry,i);
    infinity=Constant(operands.Geometry.Infinity338458,0x7f800000u,i,0x1d13de);
   }
   At(i,0x1d13e6);float distance=output.Distance00.Read(i.At);if(float.IsNaN(distance))throw new MissingContract(i.At,"OWN unordered hit distance unsupported");if(infinity<=distance)return;
   At(i,0x1d13f0);output.Material10.Write(100);
   At(i,0x1d13f7);int count=state.CandidatesC8.Count;
   if(count!=0){
    float previous=0,best=0;int selected=0;
    At(i,0x1d141d);int index=state.CandidatesC8.Count-1;
    float one=Constant(operands.Geometry.One34BB44,0x3f800000u,i,0x1d1459);
    float scale=Constant(operands.WeightScale34B9CC,0x3b808081u,i,0x1d146b);
    for(;index>=0;index--){
     float remaining=Finite(F(one-previous),At(i,0x1d1484));At(i,0x1d1488);if(best>remaining)break;
     At(i,0x1d148d);var captured=block.TerrainA8.Snapshot();
     uint z=hitZ.Read(At(i,0x1d149b)),x=hitX.Read(At(i,0x1d14a3));
     var receiver=Receiver(binding,captured,i,0x1d14ab);
     At(i,0x1d14ae);uint record=state.CandidatesC8.ReadAt(index,i.At);
     Slot(receiver.SlotF0,0x1de850,i,0x1d14b1);
     byte coverage=Weight1DE850(receiver.Data,record,x,z,i);
     previous=Mul(Mul((float)coverage,scale,i,0x1d14c1),remaining,i,0x1d14c6);
     At(i,0x1d14ca);if(previous>best){selected=index;best=previous;}
    }
    At(i,0x1d14fa);var materialReceiver=Receiver(binding,block.TerrainA8.Snapshot(),i,0x1d150b);
    At(i,0x1d150e);uint selectedRecord=state.CandidatesC8.ReadAt(selected,i.At);
    Slot(materialReceiver.SlotD8,0x1de810,i,0x1d1512);
    var capturedMaterial=Material1DE810(materialReceiver.Data,selectedRecord,i);
    At(i,0x1d1528);object material=capturedMaterial.Read(i.At); // Unknown is not null.
    if(material!=null){
     At(i,0x1d152d);var surface=material as GroundMaterialSurfaceState;
     if(surface==null)throw new MissingContract(i.At,"Actual material identity has no established OWN +FC view");
     // MOV loads/copies opaque bits; no comparison or arithmetic consumes FC.
     // The object/field address must be established, but its DWORD may remain
     // Unknown. Capture before store to retain exact-cell alias semantics.
     var raw=surface.SurfaceFC.Snapshot();At(i,0x1d1533);output.Material10.CopyFrom(raw);
    }
   }
   distance=Read(output.Distance00,i,0x1d1536);
   At(i,0x1d1541);ray=Need(ray,i.At);float zHit=Mul(distance,Read(ray.Direction0C.Z8,i,i.At),i,i.At);
   float xHit=Mul(distance,Read(ray.Direction0C.X0,i,0x1d1547),i,0x1d1547);
   zHit=Add(zHit,Read(ray.Origin00.Z8,i,0x1d154d),i,0x1d154d);
   float yHit=Mul(distance,Read(ray.Direction0C.Y4,i,0x1d1553),i,0x1d1553);
   xHit=Add(xHit,Read(ray.Origin00.X0,i,0x1d1559),i,0x1d1559);
   yHit=Add(yHit,Read(ray.Origin00.Y4,i,0x1d155e),i,0x1d155e);
   At(i,0x1d1574);output.Position04.X0.Write(xHit);output.Position04.Y4.Write(yHit);
   At(i,0x1d1579);output.Position04.Z8.Write(zHit);
  }
 }
}
