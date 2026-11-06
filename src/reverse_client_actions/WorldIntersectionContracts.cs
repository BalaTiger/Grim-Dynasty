using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Observed output fields through +28, not a native sizeof declaration.
 // +04 and Position08.D14 (+1C) have independent knowledge, never defaults.
 public sealed class WorldIntersectionObserved44 {
  public readonly Field<float> Distance00=new Field<float>();
  public readonly Field<uint> Unknown04=new Field<uint>();
  public readonly WorldPosition24 Position08=new WorldPosition24();
  public readonly Field<object> Entity20=new Field<object>();
  public readonly Field<uint> Surface28=new Field<uint>();
 }
 // Only Next00 and Region10 are observed here. No previous link, native list
 // constructor, empty sentinel or region publication is inferred.
 public sealed class WorldIntersectionRegionNode {
  public readonly Field<WorldIntersectionRegionNode> Next00=new Field<WorldIntersectionRegionNode>();
  public readonly Field<WorldRegion> Region10=new Field<WorldRegion>();
 }
 public sealed class WorldIntersectionWorldView {
  public readonly object Identity;
  public readonly WorldFloorContext Floor;
  public readonly Field<WorldIntersectionRegionNode> HeadC0=new Field<WorldIntersectionRegionNode>();
  readonly Dictionary<WorldRegion,WorldFloorRegionView> regions=new Dictionary<WorldRegion,WorldFloorRegionView>();
  readonly Dictionary<LevelMovementState,WorldFloorLevelView> levels=new Dictionary<LevelMovementState,WorldFloorLevelView>();
  public GroundRay24 LastRay{get;internal set;}
  public WorldIntersectionWorldView(object identity,WorldFloorContext floor){
   if(identity==null||floor==null)throw new ArgumentNullException("world identity/floor");Identity=identity;Floor=floor;
  }
  // Explicit OWN composition only. Query never binds or initializes a view.
  public WorldFloorRegionView BindRegion(ulong token,WorldRegion region){
   var view=Floor.BindRegion(token,region);WorldFloorRegionView prior;
   if(regions.TryGetValue(region,out prior)&&!Object.ReferenceEquals(prior,view))throw new InvalidOperationException("Canonical region changed");
   regions[region]=view;return view;
  }
  public WorldFloorLevelView BindLevel(object identity){
   var view=Floor.BindLevel(identity);WorldFloorLevelView prior;
   if(levels.TryGetValue(view.Level,out prior)&&!Object.ReferenceEquals(prior,view))throw new InvalidOperationException("Canonical level changed");
   levels[view.Level]=view;return view;
  }
  internal RegionPublicationView Region(WorldRegion region,uint at){
   WorldFloorRegionView view;if(region==null||!regions.TryGetValue(region,out view))throw new MissingContract(at,"Region lacks explicit shared floor/publication binding");return view.Publication;
  }
  internal WorldFloorLevelView Level(LevelMovementState level,uint at){
   WorldFloorLevelView view;if(level==null||!levels.TryGetValue(level,out view))throw new MissingContract(at,"Level lacks explicit shared floor binding");return view;
  }
 }
 // Engine220840, original hash4c2ea14c...93153, saved real assembly in
 // r1-engine-world-intersection-run1. Stable managed lifetimes and explicit
 // OWN recursive locks; finite translation/bounds math and ordered compares.
 // No callback success, same-region shortcut or native world initialization
 // is supplied. Level130130 delegates only to its separately evidenced model.
 public static class WorldIntersectionContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null world intersection view");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Finite(float value,uint at){if(float.IsNaN(value)||float.IsInfinity(value))throw new MissingContract(at,"OWN finite coordinate arithmetic only");return value;}
  static float Ordered(float value,uint at){if(float.IsNaN(value))throw new MissingContract(at,"COMISS unordered/FP-exception domain unimplemented");return value;}
  // MOV of opaque DWORD bits: unknown remains unknown without becoming an
  // arithmetic read failure. The catch covers only Field knowledge.
  static Field<float> FloatWord(Field<uint> word){
   var copy=new Field<float>();uint bits;try{bits=word.Read(0);}catch(MissingContract){return copy;}copy.Write(Float(bits));return copy;
  }
  static Field<uint> FloatBits(Field<float> value){
   var copy=new Field<uint>();float scalar;try{scalar=value.Read(0);}catch(MissingContract){return copy;}copy.Write(BitConverter.ToUInt32(BitConverter.GetBytes(scalar),0));return copy;
  }
  static float WordValue(Field<uint> word,uint at){return Finite(Float(word.Read(at)),at);}
  public static void GetIntersection220840(WorldIntersectionWorldView world,WorldRayObserved36 ray,WorldIntersectionObserved44 output,uint surface,bool suppressEntity,float maxDistance,bool flag6,WorldFloorContext context,Invocation invocation){
   var i=Need(invocation,0x22085b);
   At(i,0x22085b);var infinity=Need(context,i.At).Operands.Geometry.Infinity338458.Snapshot();
   At(i,0x220866);Need(output,i.At).Distance00.CopyFrom(FloatWord(infinity));
   At(i,0x22086e);output.Entity20.Write(null);
   At(i,0x220875);world=Need(world,i.At);if(!Object.ReferenceEquals(world.Floor,context))throw new MissingContract(i.At,"World and query must share the same floor/publication context");
   var head=world.HeadC0.Read(i.At);
   var node=Need(head,At(i,0x22087f)).Next00.Read(i.At);
   At(i,0x220882);if(Object.ReferenceEquals(node,head)){At(i,0x220a40);return;}
   // These locals are one stack lifetime, reused by successive list nodes.
   var localRay=new GroundRay24();world.LastRay=localRay;
   var tx=new Field<float>();var ty=new Field<float>();var tz=new Field<float>();
   var localHit=new GroundBlockHit20();var entityOut=new Field<object>();
   // The high DWORD read at2209E5 has no preceding writer in this function.
   var unwrittenStackTail=new Field<uint>();
   uint visits=0;
   do{
    if(++visits>65536)throw new MissingContract(At(i,0x2208a2),"OWN bounded live-list traversal limit, not a native list count");
    ulong sourceToken=Need(ray,At(i,0x2208a2)).Origin00.Q0.Read(i.At);
    var region=Need(node,At(i,0x2208a5)).Region10.Read(i.At);
    At(i,0x2208a9);
    if(sourceToken==0){At(i,0x2208ae);tx.Write(0);ty.Write(0);At(i,0x2208b2);tz.Write(0);}
    else{
     var source=context.Publications.Coordinates.Region(sourceToken,At(i,0x2208b7));
     int dx=source.Offset3C.Read(i.At);dx=unchecked(dx-Need(region,At(i,0x2208bb)).Offset3C.Read(i.At));
     At(i,0x2208bf);var x=ray.Origin00.D8.Snapshot();var y=ray.Origin00.DC.Snapshot();
     int dy=source.Offset40.Read(At(i,0x2208c5));dy=unchecked(dy-region.Offset40.Read(At(i,0x2208c8)));
     int dz=source.Offset44.Read(At(i,0x2208cb));dz=unchecked(dz-region.Offset44.Read(At(i,0x2208ce)));
     At(i,0x2208d1);var z=ray.Origin00.D10.Snapshot();
     float px=Finite(F(WordValue(x,At(i,0x2208f4))+F((float)dx)),i.At);
     float pz=Finite(F(WordValue(z,At(i,0x220906))+F((float)dz)),i.At);
     At(i,0x22090a);tx.Write(px);
     float py=Finite(F(WordValue(y,At(i,0x22090f))+F((float)dy)),i.At);
     At(i,0x220913);tz.Write(pz);At(i,0x220918);ty.Write(py);
    }
    At(i,0x22091d);var capturedZ=tz.Snapshot();
    At(i,0x220924);var capturedX=tx.Snapshot();var capturedY=ty.Snapshot();
    At(i,0x22092d);localRay.Origin00.X0.CopyFrom(capturedX);localRay.Origin00.Y4.CopyFrom(capturedY);
    At(i,0x220932);var directionX=ray.Direction18.X.Snapshot();var directionY=ray.Direction18.Y.Snapshot();
    At(i,0x220938);localRay.Origin00.Z8.CopyFrom(capturedZ);
    At(i,0x22093b);var directionZ=ray.Direction18.Z.Snapshot();
    At(i,0x22093f);localRay.Direction0C.X0.CopyFrom(directionX);localRay.Direction0C.Y4.CopyFrom(directionY);
    At(i,0x220944);localRay.Direction0C.Z8.CopyFrom(directionZ);
    At(i,0x220947);float bounds=GroundRayBoundsContracts.Intersection03D780(Need(region,i.At).Bounds70,localRay,context.Operands.Geometry,i);
    // COMISS+JA skips only ordered greater. In particular +Inf versus +Inf
    // is equal and continues; an AABB miss is not automatically an early-out.
    At(i,0x22094c);bool skip=Ordered(bounds,i.At)>Ordered(maxDistance,i.At);
    if(!skip){
     byte unavailable=region.Unavailable90.Read(At(i,0x220955));
     At(i,0x22095c);entityOut.Write(null);
     At(i,0x220960);if(unavailable==0){
      At(i,0x220968);var enterIdentity=region.Critical18.Snapshot();
      var entered=enterIdentity.Read(At(i,0x22096c)) as OwnReentrantRegionLock;
      if(entered==null)throw new MissingContract(i.At,"Critical18 requires explicit OWN synchronization");
      entered.Enter(i.At);
      At(i,0x220972);var leaveIdentity=region.Critical18.Snapshot();
      At(i,0x220976);var level=world.Region(region,i.At).Level68.Snapshot();
      var leaving=leaveIdentity.Read(At(i,0x22097a)) as OwnReentrantRegionLock;
      if(!Object.ReferenceEquals(entered,leaving))throw new MissingContract(i.At,"Critical18 changed while held; no different-lock leave fabricated");
      leaving.Leave(i.At);
      var borrowed=level.Read(At(i,0x220980));
      if(borrowed!=null){
       At(i,0x220989);Field<object> entityArgument=suppressEntity?null:entityOut;
       At(i,0x2209be);LevelIntersectionContracts.GetIntersection130130(world.Level(borrowed,i.At),localRay,localHit,surface,entityArgument,maxDistance,null,flag6,context,i);
       At(i,0x2209c3);var capturedCandidate=localHit.Distance00.Snapshot();
       float candidate=capturedCandidate.Read(At(i,0x2209cb));
       float prior=output.Distance00.Read(At(i,0x2209cb));
       // COMISS+JNC skips ordered >=. Unordered would update if masked, so
       // explicitly stop here rather than silently treating NaN as a miss.
       if(Ordered(candidate,i.At)<Ordered(prior,i.At)){
        At(i,0x2209d0);var material=localHit.Material10.Snapshot();
        At(i,0x2209d3);output.Distance00.Write(candidate);
        At(i,0x2209d7);var hitX=localHit.Position04.X0.Snapshot();var hitY=localHit.Position04.Y4.Snapshot();
        At(i,0x2209dc);output.Surface28.CopyFrom(material);
        At(i,0x2209df);var hitZ=localHit.Position04.Z8.Snapshot();
        At(i,0x2209e5);var copiedZ=FloatBits(hitZ);var copiedTail=unwrittenStackTail.Snapshot();
        At(i,0x2209ea);var capturedEntity=entityOut.Snapshot();
        // Explicit canonical token represents the already captured RDI
        // identity; no second region or Level identity is manufactured.
        ulong token=world.Region(region,At(i,0x2209f3)).RegionToken;
        At(i,0x2209fb);output.Entity20.CopyFrom(capturedEntity);
        At(i,0x2209ff);output.Position08.Q0.Write(token);output.Position08.D8.CopyFrom(FloatBits(hitX));output.Position08.DC.CopyFrom(FloatBits(hitY));
        At(i,0x220a03);output.Position08.D10.CopyFrom(copiedZ);output.Position08.D14.CopyFrom(copiedTail);
       }
      }
     }
    }
    node=Need(node,At(i,0x220a08)).Next00.Read(i.At);
    At(i,0x220a0b);head=world.HeadC0.Read(i.At);
    At(i,0x220a12);
   }while(!Object.ReferenceEquals(node,head));
   At(i,0x220a40);
  }
 }
}
