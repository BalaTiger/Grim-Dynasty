using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN bounded storage adapter for the observed live pointer-vector fields.
 // Allocation does not initialize Begin/End or imply an empty native list.
 // Explicit scene publication is not evidence that native map loading leaves
 // these vectors empty. Elements remain shared Field cells and live identities.
 public sealed class WorldFloorVector<T> where T:class {
  public readonly Field<ulong> Begin=new Field<ulong>(),End=new Field<ulong>(),CapacityEnd=new Field<ulong>();
  readonly Dictionary<ulong,Field<T>> slots=new Dictionary<ulong,Field<T>>();
  public string OwnSourceIdentity{get;private set;}
  bool published,coldSelected;
  public void PublishOwnContents(ulong begin,T[] values,string sourceIdentity){
   if(published)throw new InvalidOperationException("OWN vector storage already published");
   if(values==null||String.IsNullOrEmpty(sourceIdentity))throw new ArgumentException("Explicit OWN scene contents/source required");
   if(coldSelected&&(Begin.Read(0x191aec)!=0||End.Read(0x191af3)!=0||CapacityEnd.Read(0x191afa)!=0))throw new InvalidOperationException("Cold neighbor state changed before OWN publication");
   if(values.Length>65536||(begin&7)!=0||(values.Length!=0&&begin==0)||(ulong)values.Length>(ulong.MaxValue-begin)/8)throw new ArgumentException("OWN bounded aligned vector domain");
   for(int n=0;n<values.Length;n++)slots.Add(begin+(ulong)n*8,Field<T>.Of(values[n]));
   // OWN publication order, not a recovered native vector writer.
   Begin.Write(begin);End.Write(begin+(ulong)values.Length*8);CapacityEnd.Write(begin+(ulong)values.Length*8);
   OwnSourceIdentity=sourceIdentity;published=true;
  }
  internal void SelectColdRegion191980(){
   if(published||coldSelected)throw new InvalidOperationException("Cannot erase published or already selected cold neighbor state");
   // 1919D9 XOR R12D,R12D; 191AEC/191AF3/191AFA store three QWORDs.
   Begin.Write(0);End.Write(0);CapacityEnd.Write(0);coldSelected=true;
   OwnSourceIdentity="Observed Region191980 cold vector writes only; later map publication unverified";
  }
  public Field<T> Element(ulong address,uint at){Field<T> slot;if(!slots.TryGetValue(address,out slot))throw new MissingContract(at,"Live vector address has no OWN shared storage binding");return slot;}
 }
 public sealed class WorldFloorActorView {
  public readonly object Identity;
  public readonly WorldBounds24 Bounds44;
  // Bounds may borrow existing actor storage; no duplicate box or native
  // geometry/virtual dispatch is manufactured by this view.
  public WorldFloorActorView(object identity,WorldBounds24 bounds){if(identity==null||bounds==null)throw new ArgumentNullException("actor shared view");Identity=identity;Bounds44=bounds;}
 }
 public sealed class WorldFloorGroundOwner {
  public readonly OwnGroundData Data;
  public readonly GroundSpatialTree Tree;
  public readonly GroundSpatialContext Context;
  public WorldFloorGroundOwner(OwnGroundData data,GroundSpatialTree tree,GroundSpatialContext context){
   if(data==null||tree==null||context==null||context.Terrain==null||!Object.ReferenceEquals(context.Terrain.Data,data))throw new ArgumentException("Ground data/tree/context must share the actual Terrain binding");
   Data=data;Tree=tree;Context=context;
  }
 }
 public sealed class WorldFloorLandscape {
  public readonly object Identity;
  public readonly Field<WorldFloorGroundOwner> Ground20=new Field<WorldFloorGroundOwner>();
  public WorldFloorLandscape(object identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public sealed class WorldFloorLevelView {
  public readonly LevelMovementState Level;
  public readonly Field<WorldFloorLandscape> Landscape30=new Field<WorldFloorLandscape>();
  public readonly Field<object> TerrainFallback38=new Field<object>();
  public readonly Field<GroundWaterState> Water40=new Field<GroundWaterState>();
  public readonly WorldFloorVector<WorldFloorActorView> PathableActors=new WorldFloorVector<WorldFloorActorView>();
  public Field<ulong> PathableActors54D8{get{return PathableActors.Begin;}}
  public Field<ulong> PathableActors54E0{get{return PathableActors.End;}}
  internal WorldFloorLevelView(LevelMovementState level){Level=level;}
 }
 public sealed class WorldFloorRegionView {
  public readonly RegionPublicationView Publication;
  public readonly WorldFloorVector<RegionPublicationView> Neighbors=new WorldFloorVector<RegionPublicationView>();
  public Field<ulong> Neighbor100{get{return Neighbors.Begin;}}
  public Field<ulong> Neighbor108{get{return Neighbors.End;}}
  internal WorldFloorRegionView(RegionPublicationView publication){Publication=publication;}
  // Explicit OWN selection of only the observed cold-vector stores. This
  // neither runs Region construction nor establishes post-load emptiness.
  public void SelectOwnColdNeighborWrites191980(){Neighbors.SelectColdRegion191980();}
 }
 public sealed class WorldFloorOperands {
  public readonly GroundTriangleOperands Geometry;
  public readonly Field<uint> RayYOffset34BC00=new Field<uint>(),RayDirectionY34BDF4=new Field<uint>(),NeighborMargin34BC8C=new Field<uint>();
  // The three new DWORDs remain Unknown until actual raw operand evidence or
  // an explicitly labeled OWN input binds them. No pseudocode-decimal default.
  public WorldFloorOperands(GroundTriangleOperands geometry){if(geometry==null)throw new ArgumentNullException("geometry");Geometry=geometry;}
  // r1-world-floor-operands/inspection.json: actual three DWORD disk reads,
  // not a loaded-runtime assertion. Geometry keeps its existing shared cells.
  public void SelectObservedFloorDiskOperands(){RayYOffset34BC00.Write(0x40200000u);RayDirectionY34BDF4.Write(0xbf800000u);NeighborMargin34BC8C.Write(0x41800000u);}
 }
 public sealed class WorldFloorContext {
  public readonly WorldPublicationRegistry Publications;
  public readonly WorldFloorOperands Operands;
  readonly Dictionary<WorldRegion,WorldFloorRegionView> regions=new Dictionary<WorldRegion,WorldFloorRegionView>();
  readonly Dictionary<LevelMovementState,WorldFloorLevelView> levels=new Dictionary<LevelMovementState,WorldFloorLevelView>();
  public WorldPosition24 LastWrapperTemporary{get;private set;}
  public GroundRay24 LastRay{get;private set;}
  public GroundBlockHit20 LastGroundHit{get;private set;}
  public GroundBlockHit20 LastWaterHit{get;private set;}
  public WorldFloorContext(WorldPublicationRegistry publications,WorldFloorOperands operands){if(publications==null||operands==null)throw new ArgumentNullException("floor composition");Publications=publications;Operands=operands;}
  public WorldFloorRegionView BindRegion(ulong token,WorldRegion region){
   var canonical=Publications.BindRegionView(token,region);WorldFloorRegionView view;
   if(regions.TryGetValue(region,out view)){if(!Object.ReferenceEquals(view.Publication,canonical))throw new InvalidOperationException("Different canonical region view");return view;}
   view=new WorldFloorRegionView(canonical);regions.Add(region,view);return view;
  }
  public WorldFloorLevelView BindLevel(object identity){
   var canonical=Publications.BindLevelView(identity);WorldFloorLevelView view;if(levels.TryGetValue(canonical,out view))return view;
   view=new WorldFloorLevelView(canonical);levels.Add(canonical,view);return view;
  }
  internal WorldFloorRegionView Region(WorldRegion region,uint at){WorldFloorRegionView view;if(!regions.TryGetValue(region,out view))throw new MissingContract(at,"Region lacks explicit canonical floor attachment");return view;}
  internal WorldFloorLevelView Level(LevelMovementState level,uint at){WorldFloorLevelView view;if(level==null||!levels.TryGetValue(level,out view))throw new MissingContract(at,"Level lacks explicit canonical floor attachment");return view;}
  internal RegionPublicationView Neighbor(RegionPublicationView view,uint at){if(view==null||!Object.ReferenceEquals(Region(view.Region,at).Publication,view))throw new MissingContract(at,"Neighbor has no canonical floor attachment");return view;}
  internal void Observe(WorldPosition24 value){LastWrapperTemporary=value;}
  internal void Observe(GroundRay24 ray,GroundBlockHit20 hit,GroundBlockHit20 water){LastRay=ray;LastGroundHit=hit;LastWaterHit=water;}
 }
 // Saved Engine221DA0 +130270 +22C620 evidence, Engine SHA256
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // This is shared OWN composition, not native Load/ABI/render initialization.
 // Stable single-thread views, finite coordinate arithmetic, native +Inf miss
 // sentinel, same-address aliases and actual OWN recursive locks are supported.
 // No success callbacks, fake water/actor/neighbor emptiness or navigation-height
 // substitution. Real unresolved material pointers still stop in actual Block.
 public static class WorldFloorContracts {
  static T Need<T>(T v,uint at)where T:class{if(v==null)throw new MissingContract(at,"Null OWN world floor view");return v;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float v){return BitConverter.ToSingle(BitConverter.GetBytes(v),0);}
  static float Float(uint v){return BitConverter.ToSingle(BitConverter.GetBytes(v),0);}
  static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}
  // Reinterpret an opaque MOV between the existing uint/float typed views.
  // The catch covers only Field.Read knowledge, never query/arithmetic failure.
  static void CopyFloatBits(Field<float> to,Field<uint> from){uint bits;try{bits=from.Read(0);}catch(MissingContract){to.Forget();return;}to.Write(Float(bits));}
  static float Finite(float v,uint at){if(float.IsNaN(v)||float.IsInfinity(v))throw new MissingContract(at,"OWN finite world-floor arithmetic domain");return v;}
  static float R(Field<float> v,uint at,Invocation i){return Finite(v.Read(At(i,at)),at);}
  static float Scalar(Field<uint> v,uint at,Invocation i){return Finite(Float(v.Read(At(i,at))),at);}
  static float Infinity(WorldFloorContext c,uint at,Invocation i){uint bits=c.Operands.Geometry.Infinity338458.Read(At(i,at));if(bits!=0x7f800000u)throw new MissingContract(at,"Expected explicitly sourced positive infinity operand");return Float(bits);}
  static float Distance(float v,uint at){if(float.IsNaN(v)||float.IsNegativeInfinity(v))throw new MissingContract(at,"OWN unordered/negative-infinite floor distance unsupported");return v;}
  static ulong Count<T>(WorldFloorVector<T> vector,uint endAt,uint beginAt,Invocation i)where T:class{
   ulong end=vector.End.Read(At(i,endAt)),begin=vector.Begin.Read(At(i,beginAt));
   if(end<begin||((end-begin)&7)!=0||(end-begin)/8>65536)throw new MissingContract(i.At,"OWN bounded nonwrapping vector extent");return (end-begin)/8;
  }
  static T Element<T>(WorldFloorVector<T> vector,uint index,uint beginAt,uint elementAt,Invocation i)where T:class{
   ulong begin=vector.Begin.Read(At(i,beginAt));if(begin>ulong.MaxValue-(ulong)index*8)throw new MissingContract(i.At,"OWN vector address overflow");return vector.Element(begin+(ulong)index*8,At(i,elementAt)).Read(i.At);
  }
  static LevelMovementState Borrow(RegionPublicationView view,uint enterLoad,uint enterAt,uint leaveLoad,uint levelLoad,uint leaveAt,uint testAt,Invocation i){
   At(i,enterLoad);var enter=view.Region.Critical18.Snapshot();
   var entered=enter.Read(At(i,enterAt)) as OwnReentrantRegionLock;if(entered==null)throw new MissingContract(i.At,"Critical18 is not explicit OWN synchronization");entered.Enter(i.At);
   At(i,leaveLoad);var leave=view.Region.Critical18.Snapshot();At(i,levelLoad);var level=view.Level68.Snapshot();
   var leaving=leave.Read(At(i,leaveAt)) as OwnReentrantRegionLock;
   if(!Object.ReferenceEquals(entered,leaving))throw new MissingContract(i.At,"Critical18 changed while held; no different-lock leave fabricated");
   leaving.Leave(i.At);return level.Read(At(i,testAt));
  }
  // Exact known-empty return or actual bounds-rejected actors only. An actor
  // entering the geometric branch stops before unresolved virtual C0/07F070.
  public static float PathableActorIntersection130270(WorldFloorLevelView level,GroundRay24 ray,float incoming,bool neighbor,WorldFloorContext context,Invocation invocation){
   var i=Need(invocation,0x13028a);level=Need(level,0x13028a);var vector=level.PathableActors;
   if(Count(vector,0x13028a,0x130294,i)==0){At(i,0x1306ba);return incoming;}
   At(i,0x1302be);var mask=Need(context,i.At).Operands.Geometry.XorMask34C450.Snapshot();uint index=0;
   do{
    ulong begin=vector.Begin.Read(At(i,0x130310));float x=R(Need(ray,0x130317).Origin00.X0,0x130317,i),z=R(ray.Origin00.Z8,0x13031c,i);
    if(begin>ulong.MaxValue-(ulong)index*8)throw new MissingContract(At(i,0x130322),"OWN actor vector address overflow");
    var actor=Need(vector.Element(begin+(ulong)index*8,At(i,0x130322)).Read(i.At),i.At);var box=actor.Bounds44;
    float dx=Finite(F(x-R(box.Center00,0x130326,i)),i.At),ex=R(box.Extent0C,0x13032b,i);
    float dz=Finite(F(z-R(box.Center08,0x130330,i)),i.At);bool rejected=dx>ex;
    if(!rejected){float ez=R(box.Extent14,0x13033e,i);rejected=dz>ez;
     if(!rejected){float lowerX=Finite(Float(Bits(ex)^mask.Read(At(i,0x13034c))),i.At);rejected=dx<lowerX;
      if(!rejected){float lowerZ=Finite(Float(Bits(ez)^mask.Read(At(i,0x130358))),i.At);rejected=dz<lowerZ;}
     }
    }
    if(!rejected){float cy=R(box.Center04,0x130364,i),ey=R(box.Extent10,0x130369,i),y=R(ray.Origin00.Y4,0x130371,i);
     float bottom=Finite(F(cy-ey),At(i,0x130377));rejected=bottom>y;
     if(!rejected){float top=Finite(F(ey+cy),At(i,0x130384));float rayEnd=F(y-Distance(incoming,At(i,0x130388)));rejected=top<rayEnd;}
    }
    if(!rejected)throw new MissingContract(At(i,0x130395),"Actor geometry candidate requires real vtable+C0,07F070,returned object+30 and actor+49C; no collision result injected");
    index++;if(index>65536)throw new MissingContract(At(i,0x130620),"OWN live actor iteration bound");
   }while((ulong)index<Count(vector,0x130619,0x130623,i));
   At(i,0x13063a);return incoming; // No candidate altered the native incoming t.
  }
  public static byte PutOnFloor221DA0(object unusedWorld,WorldPosition24 position,WorldFloorContext context,Invocation invocation){return Core(position,context,invocation);}
  static byte Core(WorldPosition24 position,WorldFloorContext c,Invocation invocation){
   var i=Need(invocation,0x221db5);ulong token=Need(position,0x221db5).Q0.Read(At(i,0x221db5));
   var region=Need(c,0x221dbb).Publications.Coordinates.Region(token,At(i,0x221dbb));if(region.Unavailable90.Read(i.At)!=0){At(i,0x222156);return 0;}
   var regionView=c.Region(region,0x221dca);var level=Borrow(regionView.Publication,0x221dca,0x221dce,0x221dd4,0x221dd8,0x221ddc,0x221de2,i);
   if(level==null){At(i,0x222156);return 0;}
   var ray=new GroundRay24();var hit=new GroundBlockHit20();var waterHit=new GroundBlockHit20();c.Observe(ray,hit,waterHit);
   At(i,0x221deb);var x=position.D8.Snapshot();At(i,0x221df2);var z=position.D10.Snapshot();At(i,0x221df9);var capturedY=position.DC.Snapshot();
   float y=Finite(F(Scalar(capturedY,0x221e00,i)+Scalar(c.Operands.RayYOffset34BC00,0x221e00,i)),i.At);CopyFloatBits(ray.Origin00.Z8,z);
   float infinity=Infinity(c,0x221e15,i);CopyFloatBits(ray.Origin00.X0,x);At(i,0x221e22);var directionY=c.Operands.RayDirectionY34BDF4.Snapshot();
   At(i,0x221e45);ray.Origin00.Y4.Write(y);At(i,0x221e4a);ray.Direction0C.X0.Write(0);CopyFloatBits(ray.Direction0C.Y4,directionY);At(i,0x221e4f);ray.Direction0C.Z8.Write(0);
   var levelView=c.Level(level,0x221e52);var landscape=levelView.Landscape30.Read(At(i,0x221e52));
   if(landscape!=null){
    At(i,0x221e5b);hit.Distance00.Write(infinity);At(i,0x221e60);hit.Material10.Write(100);
    var ground=landscape.Ground20.Read(At(i,0x221e67));if(ground!=null){var root=ground.Tree.Root10.Read(At(i,0x221e70));At(i,0x221e96);
     if(!Object.ReferenceEquals(ground.Context.IntersectionOperands.Geometry,c.Operands.Geometry))throw new MissingContract(i.At,"Floor and Ground must share one mutable geometry/global-operand view");
     GroundSpatialTreeContracts.Query1D0100(root,ray,hit,7,infinity,null,0,ground.Context,i);
    }
   }else{
    var fallback=levelView.TerrainFallback38.Read(At(i,0x221e9d));if(fallback!=null)throw new MissingContract(At(i,0x221ea6),"Actual Terrain fallback needs recovered vtable and B8 dispatch; no null or success assumed");
   }
   var water=levelView.Water40.Read(At(i,0x221ec3));if(water!=null){
    At(i,0x221ee0);bool accepted=GroundWaterContracts.GetIntersection1F9BD0(water,ray,waterHit,7,infinity,c.Operands.Geometry,i);
    if(accepted){float wt=Distance(waterHit.Distance00.Read(At(i,0x221ee9)),i.At),gt=Distance(hit.Distance00.Read(At(i,0x221eee)),i.At);
     if(wt<gt){var distance=waterHit.Distance00.Snapshot();var px=waterHit.Position04.X0.Snapshot();var py=waterHit.Position04.Y4.Snapshot();var pz=waterHit.Position04.Z8.Snapshot();var material=waterHit.Material10.Snapshot();
      At(i,0x221efb);hit.Material10.CopyFrom(material);At(i,0x221efe);hit.Distance00.CopyFrom(distance);hit.Position04.X0.CopyFrom(px);hit.Position04.Y4.CopyFrom(py);hit.Position04.Z8.CopyFrom(pz);
     }
    }
   }
   float t=Distance(hit.Distance00.Read(At(i,0x221f02)),i.At);
   if(t>0){At(i,0x221f1d);t=Distance(PathableActorIntersection130270(levelView,ray,t,false,c,i),i.At);
    if(t>0){var neighbors=regionView.Neighbors;if(Count(neighbors,0x221f2e,0x221f35,i)!=0){
     At(i,0x221f70);var mask=c.Operands.Geometry.AbsMask34C410.Snapshot();At(i,0x221f82);var margin=c.Operands.NeighborMargin34BC8C.Snapshot();uint index=0;
     do{
      var candidate=c.Neighbor(Element(neighbors,index,0x221f90,0x221f97,i),0x221f9b);LevelMovementState neighborLevel=null;
      if(candidate.Region.Unavailable90.Read(At(i,0x221f9b))==0)neighborLevel=Borrow(candidate,0x221faa,0x221fae,0x221fb4,0x221fb8,0x221fbc,0x221fc2,i);
      if(neighborLevel!=null){
       ulong begin=neighbors.Begin.Read(At(i,0x221fcb));int oy=region.Offset40.Read(At(i,0x221fd2)),ox=region.Offset3C.Read(At(i,0x221fd5));
       if(begin>ulong.MaxValue-(ulong)index*8)throw new MissingContract(At(i,0x221fd8),"OWN neighbor address overflow");
       var reread=c.Neighbor(neighbors.Element(begin+(ulong)index*8,At(i,0x221fd8)).Read(i.At),i.At).Region;
       int oz=region.Offset44.Read(At(i,0x221fdc));oz=unchecked(oz-reread.Offset44.Read(At(i,0x221fdf)));oy=unchecked(oy-reread.Offset40.Read(At(i,0x221fe3)));ox=unchecked(ox-reread.Offset3C.Read(At(i,0x221fe7)));
       var translated=new GroundRay24();translated.Direction0C.Z8.CopyFrom(ray.Direction0C.Z8.Snapshot());
       float ty=Finite(F(F((float)oy)+R(ray.Origin00.Y4,0x222000,i)),i.At),tx=Finite(F(F((float)ox)+R(ray.Origin00.X0,0x22200d,i)),i.At);
       translated.Origin00.Y4.Write(ty);translated.Direction0C.X0.CopyFrom(ray.Direction0C.X0.Snapshot());translated.Direction0C.Y4.CopyFrom(ray.Direction0C.Y4.Snapshot());
       float ex=R(reread.Bounds70.Extent0C,0x22201c,i);translated.Origin00.X0.Write(tx);ex=Finite(F(ex+Scalar(margin,0x22202a,i)),i.At);
       float dx=Finite(F(tx-R(reread.Bounds70.Center00,0x22202f,i)),i.At),tz=Finite(F(F((float)oz)+R(ray.Origin00.Z8,0x222035,i)),i.At);
       dx=Finite(Float(Bits(dx)&mask.Read(At(i,0x22203a))),i.At);translated.Origin00.Z8.Write(tz);
       if(!(dx>ex)){float dz=Finite(F(tz-R(reread.Bounds70.Center08,0x222048,i)),i.At),ez=R(reread.Bounds70.Extent14,0x22204e,i);ez=Finite(F(ez+Scalar(margin,0x222057,i)),i.At);dz=Finite(Float(Bits(dz)&mask.Read(At(i,0x22205c))),i.At);
        if(!(dz>ez)){At(i,0x222072);t=Distance(PathableActorIntersection130270(c.Level(neighborLevel,i.At),translated,t,true,c,i),i.At);if(t<=0)break;}
       }
      }
      index++;if(index>65536)throw new MissingContract(At(i,0x222086),"OWN live neighbor iteration bound");
     }while((ulong)index<Count(neighbors,0x22207f,0x222089,i));
    }}
   }
   if(!(t<Infinity(c,0x2220ca,i))){At(i,0x222152);return 0;}
   float finalZ=Finite(F(R(ray.Direction0C.Z8,0x2220db,i)*t),0x2220ea),finalX=Finite(F(R(ray.Direction0C.X0,0x2220e0,i)*t),0x2220f2);
   float finalY=Finite(F(R(ray.Direction0C.Y4,0x2220e5,i)*t),0x2220fb);
   finalZ=Finite(F(finalZ+R(ray.Origin00.Z8,0x2220f6,i)),i.At);finalX=Finite(F(finalX+R(ray.Origin00.X0,0x2220ff,i)),i.At);finalY=Finite(F(finalY+R(ray.Origin00.Y4,0x222104,i)),i.At);
   var local=new WorldPosition24();local.Q0.Write(token);local.D8.Write(Bits(finalX));local.DC.Write(Bits(finalY));local.D10.Write(Bits(finalZ));
   // +14 is genuinely unwritten stack data, copied as Unknown, not input tail.
   var q=local.Q0.Snapshot();var lx=local.D8.Snapshot();var ly=local.DC.Snapshot();var lz=local.D10.Snapshot();var tail=local.D14.Snapshot();
   At(i,0x22212a);position.Q0.CopyFrom(q);position.D8.CopyFrom(lx);position.DC.CopyFrom(ly);At(i,0x22212f);position.D10.CopyFrom(lz);position.D14.CopyFrom(tail);At(i,0x222151);return 1;
  }
  public static byte PutOnFloor22C620(WorldPosition24 position,WorldCoordinateBoundaries boundaries,WorldFloorContext context,Invocation invocation){
   var i=Need(invocation,0x22c626);ulong token=Need(position,0x22c626).Q0.Read(At(i,0x22c626));if(token==0)return 0;
   var temporary=new WorldPosition24();Need(context,0x22c631).Observe(temporary);
   At(i,0x22c631);var x=position.D8.Snapshot();var y=position.DC.Snapshot();At(i,0x22c63b);temporary.Q0.Write(token);At(i,0x22c640);var z=position.D10.Snapshot();
   var b=Need(boundaries,0x22c643);if(!Object.ReferenceEquals(b.State,context.Publications.Coordinates))throw new MissingContract(At(i,0x22c643),"Wrapper and floor must share canonical coordinate state");
   var enginePointer=b.EngineGlobal.Snapshot();At(i,0x22c64a);temporary.D8.CopyFrom(x);temporary.DC.CopyFrom(y);At(i,0x22c650);temporary.D10.CopyFrom(z);
   var engine=Need(enginePointer.Read(At(i,0x22c654)),i.At);engine.World50.Snapshot(); // opaque transfer;221DA0 does not consume this
   At(i,0x22c658);if(Core(temporary,context,i)==0)return 0;
   At(i,0x22c661);var q=temporary.Q0.Snapshot();x=temporary.D8.Snapshot();y=temporary.DC.Snapshot();At(i,0x22c668);z=temporary.D10.Snapshot();var tail=temporary.D14.Snapshot();
   At(i,0x22c66e);position.Q0.CopyFrom(q);position.D8.CopyFrom(x);position.DC.CopyFrom(y);At(i,0x22c671);position.D10.CopyFrom(z);position.D14.CopyFrom(tail);return 1;
  }
 }
}
