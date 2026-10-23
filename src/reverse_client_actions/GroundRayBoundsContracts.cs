using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Engine03D780 and its direct03D760 local-array selector, from the pinned
 // Engine image 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // This is the observed candidate-plane algorithm, not a substituted slab test.
 // OWN finite, per-operation binary32 domain. Exceptional FP/ABI/concurrency
 // and arbitrary unmanaged byte aliases remain outside this projection.
 public static class GroundRayBoundsContracts {
  static T Need<T>(T p,uint at)where T:class{if(p==null)throw new MissingContract(at,"Null own ray/bounds view");return p;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static float Finite(float x,uint at){if(float.IsNaN(x)||float.IsInfinity(x))throw new MissingContract(at,"OWN finite ray-box arithmetic domain");return x;}
  static float Read(Field<float> x,Invocation i,uint at){At(i,at);return Finite(x.Read(at),at);}
  static float Add(float a,float b,Invocation i,uint at){return Finite(F(a+b),At(i,at));}
  static float Sub(float a,float b,Invocation i,uint at){return Finite(F(a-b),At(i,at));}
  static float Mul(float a,float b,Invocation i,uint at){return Finite(F(a*b),At(i,at));}
  static float Div(float a,float b,Invocation i,uint at){return Finite(F(a/b),At(i,at));}
  static float Axis(Displacement3 v,int n,Invocation i,uint x,uint y,uint z){uint at=n==0?x:n==1?y:z;At(i,at);v=Need(v,at);return Read(n==0?v.X0:n==1?v.Y4:v.Z8,i,at);}
  static float Miss(GroundTriangleOperands operands,Invocation i){At(i,0x03dac2);uint value=Need(operands,i.At).Infinity338458.Read(i.At);if(value!=0x7f800000u)throw new MissingContract(i.At,"OWN requires explicitly observed infinity operand");return float.PositiveInfinity;}
  public static float Intersection03D780(WorldBounds24 bounds,GroundRay24 ray,GroundTriangleOperands operands,Invocation invocation){
   var i=Need(invocation,0x03d789);At(i,0x03d789);bounds=Need(bounds,i.At);
   float cy=Read(bounds.Center04,i,0x03d789),cx=Read(bounds.Center00,i,0x03d791),cz=Read(bounds.Center08,i,0x03d798);
   float minX=Sub(cx,Read(bounds.Extent0C,i,0x03d7a0),i,0x03d7a0);
   float maxY=Add(cy,Read(bounds.Extent10,i,0x03d7a5),i,0x03d7a5);
   float minY=Sub(cy,Read(bounds.Extent10,i,0x03d7aa),i,0x03d7aa);
   float maxX=Add(cx,Read(bounds.Extent0C,i,0x03d7af),i,0x03d7af);
   float minZ=Sub(cz,Read(bounds.Extent14,i,0x03d7ce),i,0x03d7ce);
   float maxZ=Add(cz,Read(bounds.Extent14,i,0x03d7d3),i,0x03d7d3);
   var min=new float[]{minX,minY,minZ};var max=new float[]{maxX,maxY,maxZ};
   var t=new float[]{-1,-1,-1};var coordinate=new float[3];bool inside=true;
   for(int axis=0;axis<3;axis++){
    At(i,axis==0?0x03d819u:axis==1?0x03d811u:0x03d809u);ray=Need(ray,i.At);
    float origin=Axis(ray.Origin00,axis,i,0x03d819,0x03d811,0x03d809);
    At(i,0x03d837);
    if(origin<min[axis]){
     At(i,0x03d878);coordinate[axis]=min[axis];inside=false;
     float direction=Axis(ray.Direction0C,axis,i,0x03d893,0x03d88b,0x03d883);
     At(i,0x03d899);if(direction==0)continue;
     origin=Axis(ray.Origin00,axis,i,0x03d8d4,0x03d8cc,0x03d8c4);
     direction=Axis(ray.Direction0C,axis,i,0x03d8f2,0x03d8ea,0x03d8e2);
     t[axis]=Div(Sub(min[axis],origin,i,0x03d916),direction,i,0x03d91a);
    }else{
     origin=Axis(ray.Origin00,axis,i,0x03d940,0x03d938,0x03d930);
     At(i,0x03d95e);if(origin<=max[axis])continue;
     At(i,0x03d99f);coordinate[axis]=max[axis];inside=false;
     float direction=Axis(ray.Direction0C,axis,i,0x03d9ba,0x03d9b2,0x03d9aa);
     At(i,0x03d9c0);if(direction==0)continue;
     origin=Axis(ray.Origin00,axis,i,0x03d9fb,0x03d9f3,0x03d9eb);
     direction=Axis(ray.Direction0C,axis,i,0x03da19,0x03da11,0x03da09);
     t[axis]=Div(Sub(max[axis],origin,i,0x03da3d),direction,i,0x03da41);
    }
   }
   At(i,0x03da54);if(inside){At(i,0x03da59);return 0;}
   // Strict comparisons retain X then Y then Z on equal candidate distances.
   At(i,0x03da6a);int selected=t[1]>t[0]?1:0;At(i,0x03da9b);if(t[2]>t[selected])selected=2;
   At(i,0x03dabd);if(!(t[selected]>0))return Miss(operands,i);
   for(int axis=0;axis<3;axis++){
    At(i,0x03dad0);if(axis==selected)continue;
    float origin=Axis(ray.Origin00,axis,i,0x03daf4,0x03daec,0x03dae4);
    float direction=Axis(ray.Direction0C,axis,i,0x03db14,0x03db0c,0x03db04);
    // 03D760 returns the existing local candidate cell; no copy or callback.
    coordinate[axis]=Add(Mul(direction,t[selected],i,0x03db41),origin,i,0x03db45);
    At(i,0x03db88);if(min[axis]>coordinate[axis])return Miss(operands,i);
    At(i,0x03dbcb);if(max[axis]<coordinate[axis])return Miss(operands,i);
   }
   At(i,selected==0?0x03dc0cu:selected==1?0x03dbfeu:0x03dbf0u);return t[selected];
  }
 }
}
