using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Engine130130, real Rea assembly in r1-engine-level-intersection-run1.
 // Uses the same registered Level, Landscape, Ground and Water cells as Floor.
 // No default scene objects, success callback or entity-geometry substitute.
 public static class LevelIntersectionContracts {
  static T Need<T>(T v,uint at)where T:class{if(v==null)throw new MissingContract(at,"Null shared Level intersection view");return v;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static Field<float> FloatWord(Field<uint> cell){var result=new Field<float>();uint bits;try{bits=cell.Read(0);}catch(MissingContract){return result;}result.Write(BitConverter.ToSingle(BitConverter.GetBytes(bits),0));return result;}
  public static void GetIntersection130130(WorldFloorLevelView level,GroundRay24 ray,GroundBlockHit20 output,uint surface,Field<object> entityOut,float maxDistance,object excluded,bool flag6,WorldFloorContext context,Invocation invocation){
   var i=Need(invocation,0x130150);At(i,0x130150);var landscape=Need(level,i.At).Landscape30.Snapshot();
   var land=landscape.Read(At(i,0x130166));
   if(land!=null){
    At(i,0x13016b);var infinity=Need(context,i.At).Operands.Geometry.Infinity338458.Snapshot();
    At(i,0x130173);Need(output,i.At).Distance00.CopyFrom(FloatWord(infinity));
    At(i,0x130178);output.Material10.Write(100);
    At(i,0x130180);var capturedGround=land.Ground20.Snapshot();
    var ground=capturedGround.Read(At(i,0x130184));
    if(ground!=null){
     At(i,0x130189);var root=ground.Tree.Root10.Snapshot();
     At(i,0x13019e);
     if(!Object.ReferenceEquals(ground.Context.IntersectionOperands.Geometry,Need(context,i.At).Operands.Geometry))throw new MissingContract(i.At,"Level and Ground must share the same observed global operand cells");
     // Only these two arguments are hard-coded by Level: null exclusion/0 flag.
     GroundSpatialTreeContracts.Query1D0100(root.Read(At(i,0x1d011b)),ray,output,surface,maxDistance,null,0,ground.Context,i);
    }
   }else{
    At(i,0x1301a5);var fallback=level.TerrainFallback38.Snapshot();
    if(fallback.Read(At(i,0x1301a9))!=null)throw new MissingContract(At(i,0x1301ae),"Real fallback vtable and B8 target are unbound; no ground substitution");
   }
   At(i,0x1301bd);var spatialPointer=level.Level.Spatial10.Snapshot();
   object spatial=spatialPointer.Read(At(i,0x1301c9));
   if(spatial!=null&&entityOut!=null){
    // shortcut: spatial-object +10 and133E70 are not implemented; bind their
    // actual evidence before supporting an entity candidate in this branch.
    throw new MissingContract(At(i,0x1301de),"Spatial query receiver+10/133E70 unknown; any preceding real Ground hit is retained");
   }
   At(i,0x130207);var waterPointer=level.Water40.Snapshot();
   var water=waterPointer.Read(At(i,0x13020b));
   if(water!=null){
    var waterHit=new GroundBlockHit20();At(i,0x130221);
    bool hit=GroundWaterContracts.GetIntersection1F9BD0(water,ray,waterHit,surface,maxDistance,Need(context,i.At).Operands.Geometry,i);
    At(i,0x130226);if(hit){
     float distance=waterHit.Distance00.Read(At(i,0x13022a));
     float previous=Need(output,At(i,0x130230)).Distance00.Read(i.At);
     if(float.IsNaN(distance)||float.IsNaN(previous))throw new MissingContract(i.At,"COMISS unordered/FP-exception domain unsupported");
     if(distance<previous){
      At(i,0x130235);var material=waterHit.Material10.Snapshot();
      At(i,0x130239);var d=waterHit.Distance00.Snapshot();var x=waterHit.Position04.X0.Snapshot();var y=waterHit.Position04.Y4.Snapshot();var z=waterHit.Position04.Z8.Snapshot();
      At(i,0x13023e);output.Distance00.CopyFrom(d);output.Position04.X0.CopyFrom(x);output.Position04.Y4.CopyFrom(y);output.Position04.Z8.CopyFrom(z);
      At(i,0x130241);output.Material10.CopyFrom(material);
      if(entityOut!=null){At(i,0x130249);entityOut.Write(null);}
     }
    }
   }
   At(i,0x13026f);
  }
 }
}
