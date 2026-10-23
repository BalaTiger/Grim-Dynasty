using System;
using System.Threading;
namespace GrimDynasty.ReverseClient.Actions {
 // Observed263290 result layout. Construction does not invent native defaults.
 // Path10 is a borrowed stride4 address; writes retain its actual Field cells.
 public sealed class NavigationCrowdRaycastResult:NavigationView {
  public readonly Field<float> Fraction00=new Field<float>();
  public readonly NavigationPoint3 Normal04=new NavigationPoint3();
  public readonly Field<NavigationRefWindow> Path10=new Field<NavigationRefWindow>();
  public readonly Field<uint> Count18=new Field<uint>(),Reserved20=new Field<uint>();
  public readonly Field<int> Capacity1C=new Field<int>();
 }
 public sealed class NavigationCrowdRaycastBoundaries:NavigationNearestBoundaries {
  public readonly NavigationMeshTilesState MeshState;
  public readonly Field<float> PortalScale34B9CC=new Field<float>(),ParallelEpsilon34B93C=new Field<float>(),VisibleThreshold34BB38=new Field<float>();
  public readonly Field<uint> NegateMask34C450=new Field<uint>();
  public readonly int OwnMaxSteps;
  int outstanding;
  // Diagnostic actual caller-local allocation, not an injected result/callback.
  public NavigationCrowdRaycastResult LastAdapterResult{get;internal set;}
  static NavigationSpatialState SpatialOf(NavigationMeshTilesState mesh){if(mesh==null)throw new ArgumentNullException("mesh");return mesh.Spatial;}
  public NavigationCrowdRaycastBoundaries(NavigationMeshTilesState mesh):this(mesh,65536){}
  public NavigationCrowdRaycastBoundaries(NavigationMeshTilesState mesh,int ownMaxSteps):base(SpatialOf(mesh)){if(ownMaxSteps<1)throw new ArgumentOutOfRangeException("ownMaxSteps");MeshState=mesh;OwnMaxSteps=ownMaxSteps;}
  public int OwnOutstandingEnters{get{return Interlocked.CompareExchange(ref outstanding,0,0);}}
  internal void Entered(){Interlocked.Increment(ref outstanding);}internal void Left(){Interlocked.Decrement(ref outstanding);}
  // Raw operands: corridor/portal, nearest mask, region mask, and
  // r1-crowd-raycast-threshold/inspection.json. Parallel epsilon comes from
  // r1-raycast-parallel-threshold/inspection.json (raw77cc2b32); loaded globals
  // and native FP state remain unobserved. No pseudo decimal is used as proof.
  public void UseObservedRaycastConstants(){UseObservedNearestConstants();PortalScale34B9CC.Write(NavigationCoreContracts.FromBits(0x3b808081));ParallelEpsilon34B93C.Write(NavigationCoreContracts.FromBits(0x322bcc77));NegateMask34C450.Write(0x80000000u);VisibleThreshold34BB38.Write(NavigationCoreContracts.FromBits(0x3f7d70a4));}
 }
 // Engine263290 and0309F0: r1-crowd-direct-algorithms/07-09 and
 // r1-crowd-motion-branches/16-18. Original Engine SHA256:
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // OWN supported domain: stable typed mesh/cell lifetimes, <=6 vertices/poly,
 // declared stride4 allocations, bounded link traversal, masked FP exceptions,
 // per-instruction binary32 rounding. Subnormal arithmetic and signaling NaNs,
 // arbitrary native byte aliases, concurrent mutation, native EH/heap/locks,
 // FP flags and NaN payload propagation are not established. Unordered branch
 // predicates are preserved. No native executable is loaded or called here.
 public static class NavigationCrowdRaycastContracts {
  public const uint Failure=0x80000008u,Success=0x40000000u,BufferTooSmall=0x10u;
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own raycast view");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static T Entry<T>(T[] array,long index,uint at)where T:class{if(array==null||index<0||index>=array.Length)throw new MissingContract(at,"Outside own raycast table extent");return Need(array[(int)index],at);}
  static Field<float> Cell(NavigationPoint3 p,int axis,uint at){Need(p,at);try{return axis==0?p.X:axis==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static float Scalar(float value,uint at){uint bits=NavigationCoreContracts.ToBits(value);if((bits&0x7f800000u)==0&&(bits&0x7fffffu)!=0)throw new MissingContract(at,"Own raycast FP domain excludes subnormal operands; native MXCSR is unobserved");if((bits&0x7f800000u)==0x7f800000u&&(bits&0x7fffffu)!=0&&(bits&0x400000u)==0)throw new MissingContract(at,"Signaling NaN/FP exception state is outside own raycast domain");return value;}
  static float Round(double value,uint at){if(value!=0&&!Double.IsNaN(value)&&!Double.IsInfinity(value)&&Math.Abs(value)<1.1754943508222875E-38)throw new MissingContract(at,"Own raycast FP domain excludes subnormal intermediate");return Scalar(NavigationCoreContracts.F((float)value),at);}
  static float Read(NavigationPoint3 p,int axis,uint at){return Scalar(Cell(p,axis,at).Read(at),at);}
  static float Read(Field<float> p,uint at){return Scalar(p.Read(at),at);}
  static uint Mask(uint bits){return unchecked((1u<<(int)(bits&31))-1u);}
  static void Budget(ref long steps,NavigationCrowdRaycastBoundaries b,uint at){if(++steps>b.OwnMaxSteps)throw new MissingContract(at,"Own finite polygon/link traversal budget exceeded");}
  static void Identify(NavigationMesh mesh,NavigationCrowdRaycastBoundaries b,uint at){Need(b,at).MeshState.RequireLive(at);if(!Object.ReferenceEquals(mesh,b.MeshState.Mesh))throw new MissingContract(at,"Raycast mesh sidecar belongs to another actual mesh identity");}
  static Field<ushort> Index(NavigationPoly poly,long index,uint at){Need(poly,at);return Entry(poly.Indices04,index,at);}
  static NavigationPoint3 Vertex(Field<NavigationPoint3[]> vertices,uint index,uint at){return Entry(vertices.Read(at),index,at);}
  static void CopyVertex(NavigationPoly poly,int index,Field<NavigationPoint3[]> vertices,NavigationPoint3 output,uint indexAt,uint xAt,uint yAt,uint zAt){uint n=Index(poly,index,indexAt).Read(indexAt);var p=Vertex(vertices,n,xAt);output.X.CopyFrom(Cell(p,0,xAt).Snapshot());output.Y.CopyFrom(Cell(p,1,yAt).Snapshot());output.Z.CopyFrom(Cell(p,2,zAt).Snapshot());}
  static NavigationPoint3[] Gather(NavigationTile tile,NavigationPoly poly,int count){
   if(count>6)throw new MissingContract(0x2633e0,"Own declared polygon index storage has six entries; larger native scratch domain not modeled");
   var points=new NavigationPoint3[count];for(int k=0;k<count;k++)points[k]=new NavigationPoint3();int done=0;
   if(count>=4){var vertices=Need(tile,0x263401).Vertices18.Snapshot();for(;done+4<=count;done+=4){CopyVertex(poly,done,vertices,points[done],0x263440,0x263451,0x263458,0x263460);CopyVertex(poly,done+1,vertices,points[done+1],0x263468,0x263471,0x263478,0x263480);CopyVertex(poly,done+2,vertices,points[done+2],0x263488,0x263491,0x263498,0x2634a0);CopyVertex(poly,done+3,vertices,points[done+3],0x2634a8,0x2634b1,0x2634b8,0x2634c0);}}
   if(done<count){var vertices=Need(tile,0x2634e7).Vertices18.Snapshot();for(;done<count;done++)CopyVertex(poly,done,vertices,points[done],0x263511,0x263521,0x263528,0x263530);}return points;
  }
  // Start validation is the real2688C0. Later neighbor decoding intentionally
  // does not add salt/header validation absent from263290. Inline SHR is64-bit.
  static NavigationPoly Neighbor(NavigationQuery query,uint reference,NavigationCrowdRaycastBoundaries b,out NavigationTile tile){
   var mesh=Need(Need(query,0x2636d4).Mesh0.Read(0x2636d4),0x2636da);uint tb=mesh.TileBits5C.Read(0x2636da),pb=mesh.PolyBits60.Read(0x2636de);Identify(mesh,b,0x2636fc);
   var tiles=mesh.Tiles50.Snapshot();tile=Entry(tiles.Read(0x263715),(long)(((ulong)reference>>(int)(pb&63))&Mask(tb)),0x263715);var polys=tile.Polys10.Snapshot();return Entry(polys.Read(0x263719),reference&Mask(pb),0x263719);
  }
  static uint Finish(NavigationCrowdRaycastResult result,int count,uint status,Invocation i){i.At=0x263960;result.Count18.Write(unchecked((uint)count));return status;}
  // Native sixth argument is unused by this body; its stack slot becomes the
  // local status. It is deliberately absent from this typed API.
  public static uint Raycast263290(NavigationQuery query,uint startRef,NavigationPoint3 from,NavigationPoint3 to,NavigationQueryFilter filter,NavigationCrowdRaycastResult result,NavigationCrowdRaycastBoundaries b,Invocation invocation){
   var i=Need(invocation,0x263290);i.At=0x2632c2;Need(result,i.At).Fraction00.Write(0);i.At=0x2632c6;result.Count18.Write(0);i.At=0x2632ca;result.Reserved20.Write(0);if(startRef==0)return Failure;
   i.At=0x2632d6;var mesh=Need(query,i.At).Mesh0.Read(i.At);i.At=0x2632de;if(NavigationCoreContracts.ValidateReference(mesh,startRef)==0)return Failure;
   var scale=Need(b,0x263336).PortalScale34B9CC.Snapshot();i.At=0x26333f;Cell(result.Normal04,0,i.At).Write(0);Cell(result.Normal04,1,i.At).Write(0);i.At=0x263343;Cell(result.Normal04,2,i.At).Write(0);
   uint tileBits=Need(mesh,0x263347).TileBits5C.Read(0x263347),polyBits=mesh.PolyBits60.Read(0x26334a);var one=b.One34BB44.Snapshot();var absMask=b.AbsMask34C410.Snapshot();var epsilon=b.ParallelEpsilon34B93C.Snapshot();Identify(mesh,b,0x2633bc);
   var tiles=mesh.Tiles50.Snapshot();var tile=Entry(tiles.Read(0x2633d5),(long)(((ulong)startRef>>(int)(polyBits&63))&Mask(tileBits)),0x2633d5);var polyBitsPointer=tile.Polys10.Snapshot();var poly=Entry(polyBitsPointer.Read(0x2633e0),startRef&Mask(polyBits),0x2633e0);
   uint currentRef=startRef,status=Success;int pathCount=0,pathOffset=0;long steps=0;
   while(true){
    Budget(ref steps,b,0x2633e0);i.At=0x2633e0;int count=Need(poly,i.At).VertexCount1E.Read(i.At);var points=Gather(tile,poly,count);
    float fromX=Read(from,0,0x26353e),toX=Read(to,0,0x263547),fromZ=Read(from,2,0x26354d);float dx=Round((double)toX-fromX,0x263553),toZ=Read(to,2,0x263558),dz=Round((double)toZ-fromZ,0x263561);
    float tMin=0;var tMax=one.Snapshot();int exitEdge=-1,previous=count-1;
    for(int n=0;n<count;n++){
     float curX=Read(points[n],0,0x263590),edgeX=Round((double)curX-Read(points[previous],0,0x26359a),0x26359a),relZ=Round((double)fromZ-Read(points[previous],2,0x2635a0),0x2635a0);
     float curZ=Read(points[n],2,0x2635a6),edgeZ=Round((double)curZ-Read(points[previous],2,0x2635b0),0x2635b0),relX=Round((double)fromX-Read(points[previous],0,0x2635b6),0x2635b6);
     float a=Round((double)relZ*edgeX,0x2635bc),d1=Round((double)edgeX*dz,0x2635c0),c=Round((double)relX*edgeZ,0x2635c4),d2=Round((double)edgeZ*dx,0x2635c8);float numerator=Round((double)c-a,0x2635cc),denominator=Round((double)d1-d2,0x2635d0);
     float absolute=NavigationCoreContracts.FromBits(NavigationCoreContracts.ToBits(denominator)&absMask.Read(0x2635d7));i.At=0x2635db;
     if(!(Scalar(absolute,i.At)>=Read(epsilon,i.At))){i.At=0x2635e1;if(!(numerator>=0))return Finish(result,pathCount,status,i);}
     else{
      bool upper=denominator>=0;float fraction=Round((double)numerator/denominator,0x2635eb);
      if(!upper){if(fraction>tMin){i.At=0x2635f6;float upperValue=Read(tMax,i.At);tMin=fraction;if(fraction>upperValue)return Finish(result,pathCount,status,i);}}
      else{i.At=0x263604;if(!(fraction>=Read(tMax,i.At))){tMax.Write(fraction);exitEdge=previous;i.At=0x26360f;if(!(fraction>=tMin))return Finish(result,pathCount,status,i);}}
     }
     previous=n;
    }
    i.At=0x263631;float max=Read(tMax,i.At);if(max>Read(result.Fraction00,i.At)){i.At=0x263638;result.Fraction00.Write(max);}
    i.At=0x263645;if(pathCount<result.Capacity1C.Read(i.At)){var output=result.Path10.Snapshot();pathCount=unchecked(pathCount+1);i.At=0x26365d;Need(output.Read(i.At),i.At).At(pathOffset,i.At).Write(currentRef);pathOffset=unchecked(pathOffset+1);}else status|=BufferTooSmall;
    if(exitEdge==-1){i.At=0x263a0a;result.Fraction00.Write(NavigationCoreContracts.FromBits(0x7f7fffff));i.At=0x263a12;result.Count18.Write(unchecked((uint)pathCount));return status;}
    i.At=0x26368d;uint linkIndex=b.MeshState.Poly(poly,i.At).FirstLink00.Read(i.At);NavigationTile acceptedTile=null;NavigationPoly acceptedPoly=null;uint acceptedRef=0;
    if(linkIndex!=UInt32.MaxValue){var links=b.MeshState.Tile(tile,0x2636a2).Links20.Snapshot();while(linkIndex!=UInt32.MaxValue){
     Budget(ref steps,b,0x2636b4);i.At=0x2636b4;var link=Entry(links.Read(i.At),linkIndex,i.At);byte edge=link.Edge08.Read(i.At);bool accept=false;
     if(edge==exitEdge){uint neighborRef=link.Reference00.Read(0x2636d0);NavigationTile candidateTile;var candidate=Neighbor(query,neighborRef,b,out candidateTile);var spatial=b.MeshState.Spatial.Poly(candidate,0x263719);
      if((spatial.Type1F.Read(0x263719)&0xc0)!=0x40){ushort flags=spatial.Flags1C.Read(0x26372f);if((Need(filter,0x263733).Include100.Read(0x263733)&flags)!=0&&(filter.Exclude102.Read(0x263740)&flags)==0){
       byte side=link.Side09.Read(0x26374d);if(side==0xff)accept=true;
       else{byte min=link.Min0A.Read(0x26375d);if(min==0&&link.Max0B.Read(0x263768)==0xff)accept=true;
        else{
         uint first=Index(poly,edge,0x263774).Read(0x263774);var vertices=Need(tile,0x26377d).Vertices18.Snapshot();uint vertexCount=poly.VertexCount1E.Read(0x263787);if(vertexCount==0)throw new MissingContract(0x26378d,"Native integer divide by zero is outside own normal raycast domain");uint next=((uint)edge+1)%vertexCount;uint second=Index(poly,next,0x263792).Read(0x263792);
         bool useZ=(side&0xfb)==0,useX=(((byte)(side-2))&0xfb)==0;
         if(useZ||useX){float start,end,lower,upper,originalFrom,originalTo,delta;byte maximum;
          if(useZ){end=Read(Vertex(vertices,second,0x26381a),2,0x26381a);start=Read(Vertex(vertices,first,0x263821),2,0x263821);delta=Round((double)end-start,0x26382c);maximum=link.Max0B.Read(0x263834);lower=Round((double)min*delta,0x263844);upper=Round((double)maximum*delta,0x263848);lower=Round((double)lower*Read(scale,0x26384c),0x26384c);upper=Round((double)upper*Read(scale,0x263851),0x263851);lower=Round((double)lower+start,0x263856);upper=Round((double)upper+start,0x26385a);if(lower>upper){float tmp=lower;lower=upper;upper=tmp;}originalFrom=Read(from,2,0x263874);originalTo=Read(to,2,0x263881);}
          else{end=Read(Vertex(vertices,second,0x2637b0),0,0x2637b0);start=Read(Vertex(vertices,first,0x2637b6),0,0x2637b6);delta=Round((double)end-start,0x2637c0);maximum=link.Max0B.Read(0x2637c8);lower=Round((double)min*Read(scale,0x2637d8),0x2637d8);upper=Round((double)maximum*Read(scale,0x2637dd),0x2637dd);lower=Round((double)lower*delta,0x2637e2);upper=Round((double)upper*delta,0x2637e6);lower=Round((double)lower+start,0x2637ea);upper=Round((double)upper+start,0x2637ee);if(lower>upper){float tmp=lower;lower=upper;upper=tmp;}originalFrom=Read(from,0,0x263808);originalTo=Read(to,0,0x263814);}
          float hit=Round((double)originalTo-originalFrom,0x263886);hit=Round((double)hit*max,0x26388a);hit=Round((double)hit+originalFrom,0x26388e);accept=hit>=lower&&!(hit>upper); // JC rejects unordered lower; JBE accepts unordered upper.
         }
        }
       }
       if(accept){acceptedRef=link.Reference00.Read(0x2639e3);if(acceptedRef!=0){acceptedTile=candidateTile;acceptedPoly=candidate;}break;}
      }}
     }
     i.At=0x2638a0;linkIndex=link.Next04.Read(i.At);
    }}
    if(acceptedRef!=0){currentRef=acceptedRef;tile=acceptedTile;poly=acceptedPoly;continue;}
    i.At=0x2638c0;Cell(result.Normal04,1,i.At).Write(0);int nextEdge=exitEdge+1<count?exitEdge+1:0;
    float normalZ=Round((double)Read(points[nextEdge],0,0x2638d8)-Read(points[exitEdge],0,0x2638de),0x2638de),normalX=Round((double)Read(points[nextEdge],2,0x2638e4)-Read(points[exitEdge],2,0x2638ea),0x2638ea);
    normalZ=NavigationCoreContracts.FromBits(NavigationCoreContracts.ToBits(normalZ)^b.NegateMask34C450.Read(0x2638f0));i.At=0x2638f7;Cell(result.Normal04,2,i.At).Write(normalZ);i.At=0x263900;Cell(result.Normal04,0,i.At).Write(normalX);float normalY=Read(result.Normal04,1,0x263909);
    float xx=Round((double)normalX*normalX,0x263912),yy=Round((double)normalY*normalY,0x263919),zz=Round((double)Scalar(normalZ,0x263920)*normalZ,0x263920),length=Round((double)yy+xx,0x263924);length=Round((double)length+zz,0x26392b);float root=Round(Math.Sqrt(length),0x26392f),inverse=Round((double)Read(one,0x263933)/root,0x263933);
    normalX=Round((double)normalX*inverse,0x263938);normalY=Round((double)normalY*inverse,0x26393d);normalZ=Round((double)normalZ*inverse,0x263942);i.At=0x263947;Cell(result.Normal04,0,i.At).Write(normalX);i.At=0x26394d;Cell(result.Normal04,1,i.At).Write(normalY);i.At=0x263953;Cell(result.Normal04,2,i.At).Write(normalZ);return Finish(result,pathCount,status,i);
   }
  }
  static NavigationQuery Dispatch(Field<NavigationQuery> bits,uint at){try{return bits.Read(at);}catch(MissingContract e){throw new MissingContract(at,"Own query dispatch requires known pointer bits; native callee prefix not evaluated",e);}}
  sealed class HeldLock{internal NavigationCrowdCriticalSection Value;internal bool Entered;}
  public static byte CanTraverse0309F0(NavigationCrowdAdapterState state,NavigationPoint3 from,NavigationPoint3 to,NavigationCrowdRaycastBoundaries b,Invocation invocation){
   var i=Need(invocation,0x0309f0);var lockBits=Need(state,0x030a11).Critical138.Snapshot();i.At=0x030a1b;var critical=Need(lockBits.Read(i.At),i.At);Need(b,i.At);Monitor.Enter(critical.Identity);b.Entered();var held=new HeldLock{Value=critical,Entered=true};b.LastAdapterResult=null;
   try{
    var query=state.Query10.Snapshot();var reference=new Field<uint>();var projected=new NavigationPoint3();i.At=0x030a4d;uint status=NavigationNearestContracts.Nearest(Dispatch(query,i.At),from,state.Extent11C,state.Filter18,reference,projected,b,i);byte value=0;
    if((status&0x80000000u)==0){uint startRef=reference.Read(0x030a5d);if(startRef!=0){query=Need(state,0x030a6c).Query10.Snapshot();i.At=0x030a94;status=NavigationNearestContracts.Nearest(Dispatch(query,i.At),to,state.Extent11C,state.Filter18,reference,projected,b,i);
     if((status&0x80000000u)==0&&reference.Read(0x030aa0)!=0){query=Need(state,0x030aaa).Query10.Snapshot();var output=new NavigationCrowdRaycastResult();b.LastAdapterResult=output;output.Path10.Write(new NavigationRefWindow(new NavigationRefBuffer(32),0));output.Capacity1C.Write(32);i.At=0x030ada;status=Raycast263290(Dispatch(query,i.At),startRef,from,to,state.Filter18,output,b,i);
      if((status&0x80000000u)==0){float fraction=Read(output.Fraction00,0x030ae6);i.At=0x030aec;value=(byte)(fraction>Read(b.VisibleThreshold34BB38,i.At)?1:0);}
     }
    }}
    var leave=Need(state,0x030afa).Critical138.Snapshot();i.At=0x030b01;var current=Need(leave.Read(i.At),i.At);if(!Object.ReferenceEquals(current.Identity,held.Value.Identity))throw new MissingContract(i.At,"Late leave targets a different own critical-section identity");Monitor.Exit(held.Value.Identity);b.Left();held.Entered=false;return value;
   }finally{if(held.Entered){Monitor.Exit(held.Value.Identity);b.Left();held.Entered=false;}} // OWN lock cleanup, not native EH.
  }
 }
}
