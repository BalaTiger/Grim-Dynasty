using System;
namespace GrimDynasty.ReverseClient.Actions {
 public sealed class NavigationStraightPathBoundaries:NavigationNearestBoundaries {
  public readonly NavigationMeshTilesState MeshState;
  public readonly Field<float> PortalScale34B9CC=new Field<float>(),FirstPortalThreshold34B960=new Field<float>();
  public readonly int OwnMaxSteps;
  bool ownEqualityWarm;
  static NavigationSpatialState SpatialOf(NavigationMeshTilesState mesh){if(mesh==null)throw new ArgumentNullException("mesh");return mesh.Spatial;}
  public NavigationStraightPathBoundaries(NavigationMeshTilesState mesh):this(mesh,1048576){}
  public NavigationStraightPathBoundaries(NavigationMeshTilesState mesh,int ownMaxSteps):base(SpatialOf(mesh)){if(ownMaxSteps<1)throw new ArgumentOutOfRangeException("ownMaxSteps");MeshState=mesh;OwnMaxSteps=ownMaxSteps;}
  // Explicit own configuration from preserved raw observations, not the state
  // of a loaded original DLL. r1-straight-path-constant records34B960 as358637BE,
  // one binary32 ULP above the ordinary1e-6 literal; do not decimal-round it.
  public void UseObservedStraightPathConstants(){UseObservedNearestConstants();PortalScale34B9CC.Write(NavigationCoreContracts.FromBits(0x3b808081));FirstPortalThreshold34B960.Write(NavigationCoreContracts.FromBits(0x358637be));}
  // Both the frozen25F770 helper and this function's inline equality gate need
  // explicit own warm state. This establishes neither native TLS nor CRT locks.
  public new void UseObservedEqualityConstant(){base.UseObservedEqualityConstant();ownEqualityWarm=true;}
  internal void RequireOwnEqualityWarm(uint at){if(!ownEqualityWarm)throw new MissingContract(at,"Own funnel equality warm state not explicitly established; native TLS/CRT initialization remains outside scope");}
 }
 // Engine262480: r1-navigation-core/10,11; wrapper262D30:
 // r1-straight-path-portal-wrapper. Engine source SHA256:
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Same mutable typed mesh/link views and caller buffers as the corridor stage.
 // Only the local apex/legs/endpoints are independent records, like the native
 // stack locals. No callbacks generate portals or waypoints. Offmesh objects,
 // arbitrary native byte aliases, concurrency, allocation faults and nondefault
 // MXCSR/FP traps remain outside this own managed binary32 domain. MissingContract
 // preserves the written prefix and is not converted into success/native error.
 // This generates geometric path points, not physical input, movement or scenes.
 public static class NavigationStraightPathContracts {
  public const uint Failure=0x80000008u,Success=0x40000000u,Continue=0x20000000u;
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own straight-path view");var view=value as NavigationView;if(view!=null)view.RequireLive(at);return value;}
  static float F(float x){return NavigationCoreContracts.F(x);}
  // Public properties resolve the frozen library's borrowed point address;
  // payload bits are read separately. Only address-resolution exceptions are
  // attributed to this actual instruction, without reopening frozen sources.
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){Need(point,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static float Read(NavigationPoint3 point,int axis,uint at){return Cell(point,axis,at).Read(at);}
  static void Copy(NavigationPoint3 from,NavigationPoint3 to,uint xyAt,uint zAt){var x=Cell(from,0,xyAt).Snapshot();var y=Cell(from,1,xyAt).Snapshot();var z=Cell(from,2,zAt).Snapshot();to.X.CopyFrom(x);to.Y.CopyFrom(y);to.Z.CopyFrom(z);}
  static uint Ref(NavigationRefBuffer refs,int index,uint at){return Need(refs,at).At(index,at).Read(at);}
  static byte CornerFlag(uint reference,byte type){return reference==0?(byte)2:type==1?(byte)4:(byte)0;}
  static uint Finish(NavigationPoint3 end,NavigationPointBuffer points,NavigationFlagBuffer flags,Field<uint> count,int capacity,NavigationStraightPathBoundaries b,Invocation i){
   i.At=0x262bc6;int n=unchecked((int)count.Read(i.At));bool append=true;
   if(n>0){int scalar=unchecked(n*3-3);var prior=points==null?null:points.PointAtScalar(scalar,0x262bdb);i.At=0x262bdf;append=b.Equal25F770(prior,end)==0;}
   if(!append){if(flags!=null){int index=unchecked((int)count.Read(0x262bed)-1);flags.At(index,0x262bf5).Write(2);}}
   else{
    i.At=0x262bfb;int scalar=unchecked((int)count.Read(i.At)*3);
    var x=Cell(end,0,0x262bff).Snapshot();var y=Cell(end,1,0x262c04).Snapshot();
    i.At=0x262c0e;Need(points,i.At).At(scalar,i.At).CopyFrom(x);
    var z=Cell(end,2,0x262c13).Snapshot();i.At=0x262c18;points.At((long)scalar+2,i.At).CopyFrom(z);
    i.At=0x262c1e;points.At((long)scalar+1,i.At).CopyFrom(y);
    if(flags!=null){i.At=0x262c29;int index=unchecked((int)count.Read(i.At));i.At=0x262c2d;flags.At(index,i.At).Write(2);}
    i.At=0x262c31;count.Write(unchecked(count.Read(i.At)+1));
   }
   i.At=0x262c35;return Success|(unchecked((int)count.Read(i.At))>=capacity?0x10u:0u);
  }
  public static uint StraightPath(NavigationQuery query,NavigationPoint3 from,NavigationPoint3 to,NavigationRefBuffer corridor,int pathCount,NavigationPointBuffer points,NavigationFlagBuffer flags,Field<uint> count,int capacity,NavigationStraightPathBoundaries b,Invocation invocation){
   var i=Need(invocation,0x2624ba);i.At=0x2624ba;Need(count,i.At).Write(0);
   i.At=0x2624be;uint first=Ref(corridor,0,i.At);if(first==0)return Failure;
   // RBP-78 is first the clamped start and later the right portal endpoint.
   var portal=new NavigationPortalEndpoints();var start=portal.Right;var end=new NavigationPoint3();
   i.At=0x2624df;uint status=Need(b,i.At).Clamp260AD0(query,first,from,start);if((status&0x80000000)!=0)return Failure;
   i.At=0x262506;uint last=Ref(corridor,unchecked(pathCount-1),i.At);
   i.At=0x26250c;status=b.Clamp260AD0(query,last,to,end);if((status&0x80000000)!=0)return Failure;
   i.At=0x262552;status=b.Append2623B0(start,1,points,flags,count,capacity);if(status!=Continue)return status;
   if(pathCount<=1)return Finish(end,points,flags,count,capacity,b,i);
   var apex=new NavigationPoint3();var left=new NavigationPoint3();var right=new NavigationPoint3();
   Copy(start,apex,0x2625cb,0x2625d3);Copy(start,left,0x2625cb,0x2625d3);Copy(start,right,0x2625cb,0x2625d3);
   uint leftRef=Ref(corridor,0,0x2625fb),rightRef=leftRef;int leftIndex=0,rightIndex=0,index=0;byte leftType=0,rightType=0;
   var fromType=new Field<byte>();var toType=new Field<byte>();
   float apexZ=Read(apex,2,0x262634),apexX=Read(apex,0,0x26263b);long steps=0;
   while(index<pathCount){
    if(++steps>b.OwnMaxSteps){i.At=0x262642;throw new MissingContract(i.At,"Own finite funnel traversal budget exceeded; no native guard inferred");}
    int next=unchecked(index+1);float portalLeftX,portalLeftZ;byte nextType;
    if(next<pathCount){
     uint nextRef=Ref(corridor,next,0x262691),currentRef=Ref(corridor,index,0x262695);
     i.At=0x26269f;status=NavigationPortalWrapper.GetPortal262D30(query,currentRef,nextRef,portal,fromType,toType,b.MeshState,b.PortalScale34B9CC);
     if((status&0x80000000)!=0){
      uint current=Ref(corridor,index,0x262b5a);i.At=0x262b64;status=b.Clamp260AD0(query,current,to,end);if((status&0x80000000)!=0)return Failure;
      i.At=0x262ba1;b.Append2623B0(end,0,points,flags,count,capacity); // Native ignores this returned status.
      i.At=0x262ba6;return Success|0x40u|(unchecked((int)count.Read(i.At))>=capacity?0x10u:0u);
     }
     portalLeftX=Read(portal.Left,0,0x2626af);portalLeftZ=Read(portal.Left,2,0x2626b5);
     if(index==0){
      float dx=Read(portal.Right,0,0x2626c3),dz=Read(portal.Right,2,0x2626cc);
      dx=F(dx-portalLeftX);dz=F(dz-portalLeftZ);float relativeZ=F(apexZ-portalLeftZ);
      float dx2=F(dx*dx),dz2=F(dz*dz),numerator=F(relativeZ*dz),length=F(dz2+dx2);
      float relativeX=F(apexX-portalLeftX);relativeX=F(relativeX*dx);numerator=F(numerator+relativeX);
      float t=length>0f?F(numerator/length):numerator; // JBE also rejects unordered length.
      if(!(t>=0f))t=0f;
      else{float one=b.One34BB44.Read(0x26271a);t=t<one?t:one;} // MINSS returns its second operand on unordered/equality.
      float residualZ=F(dz*t),residualX=F(dx*t);residualZ=F(residualZ+portalLeftZ);residualX=F(residualX+portalLeftX);
      residualZ=F(residualZ-apexZ);residualX=F(residualX-apexX);residualZ=F(residualZ*residualZ);residualX=F(residualX*residualX);
      float distance=F(residualZ+residualX),threshold=b.FirstPortalThreshold34B960.Read(0x26274a);
      if(!(distance>=threshold)){index=unchecked(index+1);continue;} // COMISS/JC includes unordered.
     }
     nextType=toType.Read(0x262757);
    }else{
     Copy(end,portal.Left,0x26277e,0x262786);portalLeftX=Read(portal.Left,0,0x26278e);portalLeftZ=Read(portal.Left,2,0x262797);
     Copy(end,portal.Right,0x26277e,0x262786);toType.Write(0);fromType.Write(0);nextType=0;
    }
    float newRightX=Read(portal.Right,0,0x2627b1),newRightZ=Read(portal.Right,2,0x2627b6);
    newRightX=F(newRightX-apexX);float oldRightX=Read(right,0,0x2627c1);newRightZ=F(newRightZ-apexZ);float oldRightZ=Read(right,2,0x2627cc);
    oldRightX=F(oldRightX-apexX);oldRightZ=F(oldRightZ-apexZ);
    float term=F(newRightZ*oldRightX),cross=F(newRightX*oldRightZ);cross=F(cross-term);
    if(!(cross>0f)){
     i.At=0x2627fa;b.RequireOwnEqualityWarm(i.At);
     float dy=Read(right,1,0x26282a);dy=F(dy-Read(apex,1,0x262830));
     float x2=F(oldRightX*oldRightX),z2=F(oldRightZ*oldRightZ);dy=F(dy*dy);float distance=F(F(dy+x2)+z2);
     bool tighten=!(distance>=b.Epsilon422BA4.Read(0x26284c));
     if(!tighten){float lx=Read(left,0,0x262859),lz=Read(left,2,0x26285f);lx=F(lx-apexX);lz=F(lz-apexZ);term=F(newRightZ*lx);cross=F(newRightX*lz);cross=F(cross-term);tighten=cross>0f;}
     if(!tighten){
      Copy(left,apex,0x262887,0x26288e);i.At=0x2628e0;status=b.Append2623B0(apex,CornerFlag(leftRef,leftType),points,flags,count,capacity);if(status!=Continue)return status;
      Copy(apex,left,0x2628f0,0x2628f6);Copy(apex,right,0x2628f0,0x2628f6);apexZ=Read(apex,2,0x262905);apexX=Read(apex,0,0x26290f);
      index=leftIndex;rightIndex=leftIndex;index=unchecked(index+1);continue; // Ref/type metadata is not reset.
     }
     Copy(portal.Right,right,0x262933,0x262939);rightRef=next<pathCount?Ref(corridor,next,0x26295c):0;rightType=nextType;rightIndex=index;
    }
    float oldLeftX=Read(left,0,0x262976),newLeftX=F(portalLeftX-apexX);float oldLeftZ=Read(left,2,0x262981),newLeftZ=F(portalLeftZ-apexZ);
    oldLeftX=F(oldLeftX-apexX);oldLeftZ=F(oldLeftZ-apexZ);term=F(newLeftZ*oldLeftX);cross=F(newLeftX*oldLeftZ);cross=F(cross-term);
    if(cross>=0f){ // COMISS/JC skips negative OR unordered.
     i.At=0x2629b2;b.RequireOwnEqualityWarm(i.At);
     float dy=Read(left,1,0x2629d4);dy=F(dy-Read(apex,1,0x2629da));float x2=F(oldLeftX*oldLeftX),z2=F(oldLeftZ*oldLeftZ);dy=F(dy*dy);
     float distance=F(F(dy+x2)+z2);bool tighten=!(distance>=b.Epsilon422BA4.Read(0x2629f4));
     if(!tighten){float rz=Read(right,2,0x262a01),rx=Read(right,0,0x262a07);rz=F(rz-apexZ);rx=F(rx-apexX);cross=F(rz*newLeftX);term=F(rx*newLeftZ);cross=F(cross-term);tighten=!(cross>=0f);}
     if(!tighten){
      Copy(right,apex,0x262a30,0x262a37);i.At=0x262a87;status=b.Append2623B0(apex,CornerFlag(rightRef,rightType),points,flags,count,capacity);if(status!=Continue)return status;
      Copy(apex,left,0x262a97,0x262a9d);Copy(apex,right,0x262a97,0x262a9d);apexZ=Read(apex,2,0x262aac);apexX=Read(apex,0,0x262ab6);
      index=rightIndex;leftIndex=rightIndex;index=unchecked(index+1);continue;
     }
     Copy(portal.Left,left,0x262ad7,0x262add);leftRef=next<pathCount?Ref(corridor,next,0x262afd):0;leftType=nextType;leftIndex=index;
    }
    index=unchecked(index+1);
   }
   return Finish(end,points,flags,count,capacity,b,i);
  }
 }
}
