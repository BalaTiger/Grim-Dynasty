using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Unique OWN views of an explicitly bound NavManager and its implementation.
 // No constructor, query, filter, buffer payload or loaded value is inferred.
 public sealed class NavigationSearchManagerView {
  static readonly ConditionalWeakTable<object,NavigationSearchManagerView> views=new ConditionalWeakTable<object,NavigationSearchManagerView>();
  public readonly object Identity;
  public readonly Field<NavigationSearchImplementationView> Implementation08=new Field<NavigationSearchImplementationView>();
  NavigationSearchManagerView(object identity){Identity=identity;}
  public static NavigationSearchManagerView ForIdentity(object identity){if(identity==null)throw new ArgumentNullException("identity");return views.GetValue(identity,delegate(object key){return new NavigationSearchManagerView(key);});}
 }
 public sealed class NavigationSearchImplementationView:NavigationView {
  static readonly ConditionalWeakTable<object,NavigationSearchImplementationView> views=new ConditionalWeakTable<object,NavigationSearchImplementationView>();
  public readonly object Identity;
  public readonly Field<NavigationQuery> Query10=new Field<NavigationQuery>();
  public readonly NavigationQueryFilter Filter18=new NavigationQueryFilter();
  public readonly NavigationCorridorFilterCosts Costs;
  public readonly NavigationRefBuffer Corridor1F0=new NavigationRefBuffer(1024);
  public readonly NavigationPointBuffer Points11F0=new NavigationPointBuffer(2048);
  // 71F0..75EF is the nonoverlapping typed byte domain. Native maxPoints is
  // still2048; an actual write beyond1023 stops instead of inventing aliases
  // over75F0/75F4 or silently allocating a larger independent native array.
  public readonly NavigationFlagBuffer Flags71F0=new NavigationFlagBuffer(1024);
  public readonly NavigationPoint3 Extent75F4=new NavigationPoint3();
  public readonly Field<uint> Searches76E8=new Field<uint>();
  NavigationSearchImplementationView(object identity){Identity=identity;Costs=new NavigationCorridorFilterCosts(Filter18);}
  public static NavigationSearchImplementationView ForIdentity(object identity){if(identity==null)throw new ArgumentNullException("identity");return views.GetValue(identity,delegate(object key){return new NavigationSearchImplementationView(key);});}
  // Explicit selection of just the three immediate writes at10A9B7/C1/CB.
  // It does not run10A530, start its thread, or establish loaded runtime values.
  public void SelectObservedConstructorExtents10A530(){Extent75F4.X.Write(NavigationCoreContracts.FromBits(0x3d4ccccd));Extent75F4.Y.Write(NavigationCoreContracts.FromBits(0x40400000));Extent75F4.Z.Write(NavigationCoreContracts.FromBits(0x3d4ccccd));}
 }
 public sealed class NavigationSearchWrapperContext {
  public readonly WorldCoordinateBoundaries World;
  public readonly WorldFloorContext Floor;
  public readonly NavigationNearestBoundaries Nearest;
  public readonly NavigationCorridorBoundaries Corridor;
  public readonly NavigationStraightPathBoundaries Straight;
  public NavigationSearchWrapperContext(NavigationMeshTilesState mesh,NavigationCorridorState search,WorldCoordinateBoundaries world,WorldFloorContext floor){
   if(mesh==null||search==null||world==null||floor==null)throw new ArgumentNullException("explicit shared navigation/floor composition");
   World=world;Floor=floor;Nearest=new NavigationNearestBoundaries(mesh.Spatial);Corridor=new NavigationCorridorBoundaries(mesh,search);Straight=new NavigationStraightPathBoundaries(mesh);
  }
  public void SelectObservedAlgorithmConstants(){Nearest.UseObservedNearestConstants();Corridor.UseObservedCorridorConstants();Straight.UseObservedStraightPathConstants();Straight.UseObservedEqualityConstant();}
 }
 public sealed class NavigationSearchWrapperInvocation {
  public readonly Invocation Native=new Invocation();
  public NavigationPoint3 FromWorld,ToWorld,FirstProjection,SecondProjection,SecondExtent;
  public Field<uint> LocalRefOrCount;
  public Field<uint> StartReference,EndReference,CorridorCount,PointCount;
  public bool FirstNearestReturned,SecondNearestReturned,CorridorReturned,StraightReturned,CacheHit,TranslateReturned,FloorReturned,Returned;
  public byte Result;
  internal void Begin(){
   FromWorld=new NavigationPoint3();ToWorld=new NavigationPoint3();FirstProjection=new NavigationPoint3();SecondProjection=new NavigationPoint3();SecondExtent=new NavigationPoint3();LocalRefOrCount=new Field<uint>();
   StartReference=new Field<uint>();EndReference=new Field<uint>();CorridorCount=new Field<uint>();PointCount=new Field<uint>();
   FirstNearestReturned=SecondNearestReturned=CorridorReturned=StraightReturned=CacheHit=TranslateReturned=FloorReturned=Returned=false;Result=0;Native.At=0;
  }
 }
 // Engine10C6C0 exact saved assembly: r1-engine-navigation-search/05.150590:
 // r1-engine-mouse-core/11. Existing algorithms are invoked directly; there is
 // no replaceable successful query, corridor, translation or floor callback.
 // Supported domain: same canonical Region, typed same-address aliases,
 // explicitly resolved Query10, finite OWN storage/lifetimes, vector output null, normal masked
 // binary32 arithmetic. Unknown cells and unsupported branches keep prefixes.
 public static class NavigationSearchWrapperContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null navigation wrapper view outside supported domain");var live=value as NavigationView;if(live!=null)live.RequireLive(at);return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float x){return NavigationCoreContracts.F(x);}
  static float Read(Field<uint> cell,Invocation i,uint at){return NavigationCoreContracts.FromBits(cell.Read(At(i,at)));}
  static Field<float> PointCell(NavigationPoint3 point,int axis,Invocation i,uint at){At(i,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,"Own point component outside declared wrapper storage",e);}}
  static float Sqrt(float x){return F((float)Math.Sqrt(x));}
  // Frozen algorithms take a typed query, not opaque pointer bits. Unknown
  // Query10 therefore stops at this explicit OWN adapter boundary, BEFORE
  // entering the callee. Its native callee clear-prefix is not claimed here.
  // Known null is forwarded unchanged so the actual callee owns its prefix.
  static NavigationQuery ResolvedQuery(Field<NavigationQuery> bits,Invocation i,uint at){At(i,at);try{return bits.Read(at);}catch(MissingContract e){throw new MissingContract(at,"OWN adapter requires explicitly resolved Query10; callee not entered and its native prefix is outside this domain",e);}}
  static byte Finish(NavigationSearchWrapperInvocation call,byte value,uint at){call.Native.At=at;call.Result=value;call.Returned=true;return value;}
  static NavigationSearchImplementationView Impl(NavigationSearchImplementationView value,Invocation i,uint at){return Need(value,At(i,at));}
  static NavigationSearchWrapperContext Context(NavigationSearchWrapperContext value,Invocation i,uint at){return Need(value,At(i,at));}
  static void CopyTranslateFloor(WorldPosition24 output,WorldPosition24 from,NavigationPoint3 endpoint,float sourceX,float sourceY,float sourceZ,bool cached,float capturedX,float capturedZ,NavigationSearchWrapperContext context,NavigationSearchWrapperInvocation call){
   var i=call.Native;var d=new Displacement3();
   uint load=cached?0x10c8abu:0x10c9e5u,store=cached?0x10c8beu:0x10c9f7u,tailLoad=cached?0x10c8cbu:0x10c9fau,tailStore=cached?0x10c8dbu:0x10ca04u;
   At(i,load);var q=from.Q0.Snapshot();var x=from.D8.Snapshot();var y=from.DC.Snapshot();
   float dx=0,dz=0;Field<float> cachedY=null;
   if(cached)dx=F(capturedX-sourceX);
   At(i,store);output.Q0.CopyFrom(q);output.D8.CopyFrom(x);output.DC.CopyFrom(y);
   if(cached){cachedY=PointCell(endpoint,1,i,0x10c8c1).Snapshot();dz=F(capturedZ-sourceZ);}
   At(i,tailLoad);var z=from.D10.Snapshot();var padding=from.D14.Snapshot();
   if(cached)d.Y4.Write(F(cachedY.Read(At(i,0x10c8d1))-sourceY));
   At(i,tailStore);output.D10.CopyFrom(z);output.D14.CopyFrom(padding);
   if(cached){d.X0.Write(dx);d.Z8.Write(dz);}
   else{
    var targetX=PointCell(endpoint,0,i,0x10ca09).Snapshot();var targetY=PointCell(endpoint,1,i,0x10ca15).Snapshot();
    dx=F(targetX.Read(At(i,0x10ca1e))-sourceX);float dy=F(targetY.Read(At(i,0x10ca23))-sourceY);d.X0.Write(dx);
    var targetZ=PointCell(endpoint,2,i,0x10ca2d).Snapshot();dz=F(targetZ.Read(At(i,0x10ca36))-sourceZ);d.Y4.Write(dy);d.Z8.Write(dz);
   }
   At(i,cached?0x10c8eau:0x10ca45u);WorldCoordinateContracts.TranslateTrue22C350(output,d,context.World,i);call.TranslateReturned=true;
   At(i,cached?0x10c8f2u:0x10ca4du);WorldFloorContracts.PutOnFloor22C620(output,context.World,context.Floor,i);call.FloorReturned=true;
  }
  public static byte FindPath150590(NavigationSearchManagerView manager,WorldPosition24 from,WorldPosition24 to,Field<float> horizontalExtent,WorldPosition24 output,Field<uint> reference,Field<float> distance,object outputVector,NavigationPoint3 direction,Field<byte> allowPartial,NavigationSearchWrapperContext context,NavigationSearchWrapperInvocation call){
   Need(call,0x150590).Begin();var i=call.Native;At(i,0x150590);var partial=Need(allowPartial,i.At).Snapshot();At(i,0x150595);var implementation=Need(manager,i.At).Implementation08.Snapshot();
   //150599 merely copies AL back to the stack; Unknown remains opaque until
   //10C9CF actually tests it. The target's first Impl dereference is later.
   At(i,0x150599);return Core(implementation,from,to,horizontalExtent,output,reference,distance,outputVector,direction,partial,context,call);
  }
  public static byte FindPath10C6C0(NavigationSearchImplementationView implementation,WorldPosition24 from,WorldPosition24 to,Field<float> horizontalExtent,WorldPosition24 output,Field<uint> reference,Field<float> distance,object outputVector,NavigationPoint3 direction,Field<byte> allowPartial,NavigationSearchWrapperContext context,NavigationSearchWrapperInvocation call){
   Need(call,0x10c6c0).Begin();return Core(Field<NavigationSearchImplementationView>.Of(implementation),from,to,horizontalExtent,output,reference,distance,outputVector,direction,allowPartial,context,call);
  }
  static byte Core(Field<NavigationSearchImplementationView> implementation,WorldPosition24 from,WorldPosition24 to,Field<float> horizontalExtent,WorldPosition24 output,Field<uint> reference,Field<float> distance,object outputVector,NavigationPoint3 direction,Field<byte> allowPartial,NavigationSearchWrapperContext context,NavigationSearchWrapperInvocation call){
   var i=call.Native;At(i,0x10c6ff);var radius=Need(horizontalExtent,i.At).Snapshot();At(i,0x10c703);var startToken=Need(from,i.At).Q0.Snapshot();At(i,0x10c706);call.SecondExtent.Z.CopyFrom(radius);
   var regions=Context(context,i,0x10c70c).World.State;var startRegion=regions.Region(startToken.Read(i.At),i.At);
   var ox=startRegion.Offset3C.Snapshot();var oy=startRegion.Offset40.Snapshot();At(i,0x10c711);var oz=startRegion.Offset44.Snapshot();
   float sx=F((float)ox.Read(At(i,0x10c730)));At(i,0x10c739);var endToken=Need(to,i.At).Q0.Snapshot();sx=F(sx+Read(from.D8,i,0x10c73c));
   float sy=F((float)oy.Read(At(i,0x10c742)));At(i,0x10c746);var endRegion=regions.Region(endToken.Read(i.At),i.At);var ez=endRegion.Offset44.Snapshot();
   if(!Object.ReferenceEquals(startRegion,endRegion))throw new MissingContract(i.At,"Cross-Region wrapper domain is not selected; no navigation output was invented");
   At(i,0x10c749);var ex=endRegion.Offset3C.Snapshot();var ey=endRegion.Offset40.Snapshot();sy=F(sy+Read(from.DC,i,0x10c74e));
   At(i,0x10c76a);var impl=Impl(implementation.Read(i.At),i,i.At);var query=impl.Query10.Snapshot();float sz=F((float)oz.Read(At(i,0x10c76e)));sz=F(sz+Read(from.D10,i,0x10c77a));
   float ty=F((float)ey.Read(At(i,0x10c780)));At(i,0x10c796);call.FromWorld.X.Write(sx);ty=F(ty+Read(to.DC,i,0x10c79d));float tx=F((float)ex.Read(At(i,0x10c7a3)));At(i,0x10c7a6);call.ToWorld.Y.Write(ty);tx=F(tx+Read(to.D8,i,0x10c7ac));
   var extentY=PointCell(impl.Extent75F4,1,i,0x10c7b2).Snapshot();float tz=F((float)ez.Read(At(i,0x10c7ba)));At(i,0x10c7bd);call.SecondExtent.Y.CopyFrom(extentY);tz=F(tz+Read(to.D10,i,0x10c7c2));
   At(i,0x10c7cf);call.FromWorld.Y.Write(sy);At(i,0x10c7d6);call.FromWorld.Z.Write(sz);At(i,0x10c7dd);call.ToWorld.X.Write(tx);At(i,0x10c7e3);call.SecondExtent.X.CopyFrom(radius);At(i,0x10c7e9);call.ToWorld.Z.Write(tz);
   At(i,0x10c7ef);uint status=NavigationNearestContracts.Nearest(ResolvedQuery(query,i,i.At),call.FromWorld,impl.Extent75F4,impl.Filter18,call.LocalRefOrCount,call.FirstProjection,context.Nearest,i);call.FirstNearestReturned=true;
   if((status&0x80000000u)!=0)return Finish(call,0,0x10ccef);At(i,0x10c7ff);call.StartReference.CopyFrom(call.LocalRefOrCount.Snapshot());uint first=call.StartReference.Read(At(i,0x10c803));if(first==0)return Finish(call,0,0x10ccef);
   At(i,0x10c80c);query=impl.Query10.Snapshot();At(i,0x10c836);status=NavigationNearestContracts.Nearest(ResolvedQuery(query,i,i.At),call.ToWorld,call.SecondExtent,impl.Filter18,call.LocalRefOrCount,call.SecondProjection,context.Nearest,i);call.SecondNearestReturned=true;
   if((status&0x80000000u)!=0)return Finish(call,0,0x10cceb);At(i,0x10c846);call.EndReference.CopyFrom(call.LocalRefOrCount.Snapshot());uint last=call.EndReference.Read(At(i,0x10c849));if(last==0)return Finish(call,0,0x10cceb);
   At(i,0x10c851);var projectedX=call.SecondProjection.X.Snapshot();At(i,0x10c856);var projectedZ=call.SecondProjection.Z.Snapshot();float px=projectedX.Read(At(i,0x10c85b));float dx=F(tx-px);float pz=projectedZ.Read(At(i,0x10c85f));float dz=F(tz-pz);float sum=F(F(dz*dz)+F(dx*dx));
   float horizontal=(sum==0||Single.IsNaN(sum))?0:Sqrt(sum);if(horizontal>radius.Read(At(i,0x10c883)))return Finish(call,0,0x10cceb);
   if(reference!=null&&reference.Read(At(i,0x10c89e))==last){
    call.CacheHit=true;if(output!=null)CopyTranslateFloor(output,from,call.SecondProjection,sx,sy,sz,true,px,pz,context,call);
    return Finish(call,1,0x10c8f7);
   }
   At(i,0x10c8fe);query=impl.Query10.Snapshot();At(i,0x10c906);impl.Searches76E8.Write(unchecked(impl.Searches76E8.Read(i.At)+1));
   At(i,0x10c947);status=NavigationCorridorContracts.FindPath(ResolvedQuery(query,i,i.At),first,last,call.FromWorld,call.ToWorld,impl.Filter18,impl.Corridor1F0,call.LocalRefOrCount,1024,context.Corridor,i);call.CorridorReturned=true;
   if((status&0x80000000u)!=0)return Finish(call,0,0x10cce7);At(i,0x10c957);call.CorridorCount.CopyFrom(call.LocalRefOrCount.Snapshot());uint corridorCount=call.CorridorCount.Read(At(i,0x10c95b));if(corridorCount==0)return Finish(call,0,0x10cce7);
   At(i,0x10c991);query=impl.Query10.Snapshot();At(i,0x10c9a1);status=NavigationStraightPathContracts.StraightPath(ResolvedQuery(query,i,i.At),call.FirstProjection,call.SecondProjection,impl.Corridor1F0,unchecked((int)corridorCount),impl.Points11F0,impl.Flags71F0,call.LocalRefOrCount,2048,context.Straight,i);call.StraightReturned=true;
   if((status&0x80000000u)!=0)return Finish(call,0,0x10cce7);At(i,0x10c9b1);call.PointCount.CopyFrom(call.LocalRefOrCount.Snapshot());uint pointCount=call.PointCount.Read(At(i,0x10c9b5));if(pointCount==0)return Finish(call,0,0x10cce7);
   int lastIndex=unchecked((int)corridorCount-1);if(last!=impl.Corridor1F0.At(lastIndex,At(i,0x10c9c5)).Read(i.At)&&Need(allowPartial,At(i,0x10c9cf)).Read(i.At)==0)return Finish(call,0,0x10cce7);
   if(output!=null){int scalar=unchecked((int)(pointCount-1)*3);CopyTranslateFloor(output,from,impl.Points11F0.PointAtScalar(scalar,0x10ca00),sx,sy,sz,false,0,0,context,call);}
   if(reference!=null){var returned=impl.Corridor1F0.At(lastIndex,At(i,0x10ca57)).Snapshot();At(i,0x10ca5f);reference.CopyFrom(returned);}
   if(distance!=null){
    At(i,0x10ca6d);distance.Write(0);if(unchecked((int)pointCount)>1)for(int n=0;n<unchecked((int)pointCount-1);n++){
     int s=n*3;var nextY=impl.Points11F0.At(s+4,At(i,0x10ca90)).Snapshot();float dy=F(nextY.Read(At(i,0x10ca99))-impl.Points11F0.At(s+1,i.At).Read(i.At));
     var nextX=impl.Points11F0.At(s+3,At(i,0x10ca9d)).Snapshot();float ax=F(nextX.Read(At(i,0x10caa2))-impl.Points11F0.At(s,i.At).Read(i.At));
     var nextZ=impl.Points11F0.At(s+5,At(i,0x10caa7)).Snapshot();float az=F(nextZ.Read(At(i,0x10caac))-impl.Points11F0.At(s+2,i.At).Read(i.At));
     float sq=F(F(F(dy*dy)+F(ax*ax))+F(az*az));float length=(sq==0||Single.IsNaN(sq))?0:Sqrt(sq);distance.Write(F(length+distance.Read(At(i,0x10cad9))));
    }
   }
   At(i,0x10caee);if(outputVector!=null)throw new MissingContract(At(i,0x10caf7),"Nonnull WorldVec3 vector output is outside this selected wrapper domain; prior output/reference/distance writes retained");
   if(direction!=null){
    var directionX=impl.Points11F0.At(3,At(i,0x10cc21)).Snapshot();var directionY=impl.Points11F0.At(4,At(i,0x10cc29)).Snapshot();float vx=F(directionX.Read(At(i,0x10cc31))-sx);var directionZ=impl.Points11F0.At(5,At(i,0x10cc36)).Snapshot();float vy=F(directionY.Read(At(i,0x10cc3e))-sy);
    At(i,0x10cc43);var one=context.Straight.One34BB44.Snapshot();float vz=F(directionZ.Read(At(i,0x10cc4b))-sz);
    float sq=F(F(F(vy*vy)+F(vx*vx))+F(vz*vz)),scale=F(one.Read(At(i,0x10cc74))/Sqrt(sq));vx=F(vx*scale);vy=F(vy*scale);vz=F(vz*scale);
    var outX=PointCell(direction,0,i,0x10cc8a);var outY=PointCell(direction,1,i,0x10cc8a);outX.Write(vx);outY.Write(vy);PointCell(direction,2,i,0x10cc96).Write(vz);
   }
   return Finish(call,1,0x10cc99);
  }
 }
}
