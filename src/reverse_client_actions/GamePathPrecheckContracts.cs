using System;
using System.Runtime.CompilerServices;
namespace GrimDynasty.ReverseClient.Actions {
 // The sole +70 sidecar belongs to the existing MovementManager. There is no
 // second manager, copied movement flag, queue or owner. Bind is OWN setup;
 // the uint remains Unknown until an explicit observation or native store.
 public sealed class MovementPathState {
  static readonly ConditionalWeakTable<MovementManager,MovementPathState> managers=new ConditionalWeakTable<MovementManager,MovementPathState>();
  static readonly object gate=new object();
  public readonly MovementManager Manager;
  public readonly Field<uint> CachedReference70=new Field<uint>();
  MovementPathState(MovementManager manager){Manager=manager;}
  public static MovementPathState Bind(MovementManager manager){if(manager==null)throw new ArgumentNullException("manager");lock(gate){MovementPathState value;if(managers.TryGetValue(manager,out value))return value;value=new MovementPathState(manager);managers.Add(manager,value);return value;}}
  public static MovementPathState ForBound(MovementManager manager,uint at){MovementPathState value;if(manager==null||!managers.TryGetValue(manager,out value))throw new MissingContract(at,"MovementManager70 requires its explicit unique sidecar");return value;}
 }
 public sealed class NavigationSearchCriticalState {public readonly Field<object> Native10=new Field<object>();}
 public sealed class NavigationSearchGlobals {
  public readonly Field<NavigationSearchCriticalState> Critical421380=new Field<NavigationSearchCriticalState>();
  public readonly Field<NavigationSearchManagerView> Instance4213B8=new Field<NavigationSearchManagerView>();
 }
 public static class NavigationSearchSingletonContracts {
  // Actual Engine01C510. Only both-published hot globals can complete. Cold
  // allocation/locking remains at its actual first unresolved call, not null.
  public static NavigationSearchManagerView Get01C510(NavigationSearchGlobals globals,Invocation i){
   if(i==null)throw new MissingContract(0x01c51f,"Missing singleton invocation");i.At=0x01c51f;if(globals==null)throw new MissingContract(i.At,"Missing explicit NavManager globals");var criticalBits=globals.Critical421380.Snapshot();i.At=0x01c526;var critical=criticalBits.Read(i.At);
   if(critical==null){i.At=0x01c52e;throw new MissingContract(i.At,"Cold NavManager critical allocation2DC398 is unclosed");}
   i.At=0x01c577;var instanceBits=globals.Instance4213B8.Snapshot();i.At=0x01c57e;var instance=instanceBits.Read(i.At);if(instance!=null){i.At=0x01c5d3;return instance;}
   i.At=0x01c583;critical.Native10.Snapshot();i.At=0x01c587;throw new MissingContract(i.At,"Cold NavManager instance requires actual EnterCriticalSection and150280 construction");
  }
 }
 public sealed class GamePathPrecheckContext {
  public readonly GameMovementIdentityBridge Bridge;
  public readonly MoveContext Movement;
  public readonly NavigationSearchGlobals Globals;
  public readonly NavigationSearchWrapperContext Navigation;
  public readonly Field<float> One7A8054=new Field<float>(),DistanceGate7A80F0=new Field<float>(),NullDifference338458=new Field<float>();
  public GamePathPrecheckContext(GameMovementIdentityBridge bridge,MoveContext movement,NavigationSearchGlobals globals,NavigationSearchWrapperContext navigation){
   if(bridge==null||movement==null||globals==null||navigation==null)throw new ArgumentNullException("existing precheck composition");if(!Object.ReferenceEquals(bridge.Callbacks.World,navigation.World))throw new ArgumentException("Precheck and navigation must share the same canonical WorldCoordinate boundary");Bridge=bridge;Movement=movement;Globals=globals;Navigation=navigation;
  }
  public void SelectObservedOne7A8054(){One7A8054.Write(NavigationCoreContracts.FromBits(0x3f800000));}
 }
 public sealed class GamePathPrecheckInvocation {
  public readonly Invocation Native=new Invocation();
  public NavigationSearchWrapperInvocation Navigation{get;private set;}
  public EntityCoordinates64 Coordinates{get;private set;}
  public WorldPosition24 Start{get;private set;}
  public WorldPosition24 LocalOutput{get;private set;}
  public Displacement3 Difference{get;private set;}
  public NavigationPoint3 Direction{get;private set;}
  public Field<uint> LocalReference{get;private set;}
  public Field<float> LocalDistance{get;private set;}
  public bool CoordsReturned,PortalReturned,DifferenceReturned,SingletonReturned,NavigationReturned,SameReference,OutputPublished,DistancePublished,ReferencePublished,Returned;
  public uint Result,DecisionAt;
  public GamePathPrecheckInvocation(){Begin();}
  internal void Begin(){Native.At=0;Navigation=new NavigationSearchWrapperInvocation();Coordinates=new EntityCoordinates64();Start=new WorldPosition24();LocalOutput=new WorldPosition24();Difference=new Displacement3();Direction=new NavigationPoint3();LocalReference=new Field<uint>();LocalDistance=new Field<float>();CoordsReturned=PortalReturned=DifferenceReturned=SingletonReturned=NavigationReturned=SameReference=OutputPublished=DistancePublished=ReferencePublished=Returned=false;Result=DecisionAt=0;}
 }
 public static class GamePathPrecheckContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own path-precheck view");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float x){return NavigationCoreContracts.F(x);}
  static float Sqrt(float x){return F((float)Math.Sqrt(x));}
  static uint Finish(GamePathPrecheckInvocation c,uint result,uint decision){c.DecisionAt=decision;c.Result=result;c.Native.At=0x08e58b;c.Returned=true;return result;}
  // The frozen PortalSelection only needs Q0 before its no-portal return.
  // These token-only views BORROW Q0; they are never packed representations
  // of XYZ/D14. Other portal imports remain Unsupported, so no consumer can
  // mistake the deliberately unknown coarse Q8/Q16 words for copied values.
  sealed class SameRegionPortalBoundary:PortalSelectionBoundaries {
   readonly WorldCoordinateState regions;readonly Invocation i;
   public SameRegionPortalBoundary(GamePathPrecheckContext context,Invocation invocation):base(context.Movement){regions=context.Navigation.World.State;i=invocation;}
   // The frozen object-return interface cannot carry opaque Unknown tokens.
   // Resolve this OWN adapter domain at the caller, before entering22C0B0.
   // Native22C0B0 itself only MOVs the token and does not query a registry;
   // its two-call Unknown prefix is deliberately not claimed by this adapter.
   public override object Region(WorldPosition position){uint caller=i.At;object region;try{ulong token=Need(position,caller).Q0.Read(caller);region=token==0?null:regions.Region(token,caller);}catch(MissingContract error){i.At=caller;throw new MissingContract(caller,"OWN object-return PortalSelection adapter requires an explicit resolved Region token; opaque native getter prefix is outside this domain",error);}At(i,0x22c0b0);At(i,0x22c0b3);return region;}
  }
  static WorldPosition TokenView(WorldPosition24 value,uint at){return new WorldPosition(Need(value,at).Q0,new Field<ulong>(),new Field<ulong>());}
  static void Publish(WorldPosition24 local,WorldPosition24 output,bool same,Invocation i){
   uint first=same?0x08e548u:0x08e51bu,tail=same?0x08e54du:0x08e520u,store=same?0x08e553u:0x08e526u,last=same?0x08e556u:0x08e529u;
   At(i,first);var q=local.Q0.Snapshot();var x=local.D8.Snapshot();var y=local.DC.Snapshot();At(i,tail);var z=local.D10.Snapshot();var pad=local.D14.Snapshot();At(i,store);output.Q0.CopyFrom(q);output.D8.CopyFrom(x);output.DC.CopyFrom(y);At(i,last);output.D10.CopyFrom(z);output.D14.CopyFrom(pad);
  }
  // Game08E200 ordinary same-Region branch. All path production is the real
  //150590/10C6C0 body; no callback can supply path results. The parameter views
  // preserve native register captures and late stack reads. Ordinary masked
  // binary32 only; native exceptions/FTZ/DAZ/concurrent mutation are excluded.
  public static uint FindPath08E200(MovementManager manager,WorldPosition24 destination,Field<float> radius,Field<float> minimumDistance,Field<float> maximumRatio,Field<float> minimumDot,WorldPosition24 output,Field<float> distanceOutput,Field<float> ratioBaseline,Field<byte> forceRatio,GamePathPrecheckContext context,GamePathPrecheckInvocation invocation){
   var c=Need(invocation,0x08e232);c.Begin();var i=c.Native;At(i,0x08e232);var ownerBits=Need(manager,i.At).Owner00.Snapshot();At(i,0x08e235);var radiusBits=Need(radius,i.At).Snapshot();At(i,0x08e239);var minimumBits=Need(minimumDistance,i.At).Snapshot();
   At(i,0x08e23d);var ctx=Need(context,i.At);At(i,0x07f076);var owner=ownerBits.Read(i.At) as MoveCharacter;if(owner==null)throw new MissingContract(i.At,"Manager owner lacks original MoveCharacter identity");var view=ctx.Bridge.ForMoveCharacter(owner,i.At);var coords=EntityMovementContracts.GetCoords07F070(view.Owner.Entity,c.Coordinates,i);c.CoordsReturned=true;
   At(i,0x08e24d);var source=coords.Position00;var q=source.Q0.Snapshot();var x=source.D8.Snapshot();var y=source.DC.Snapshot();At(i,0x08e250);c.Start.Q0.CopyFrom(q);c.Start.D8.CopyFrom(x);c.Start.DC.CopyFrom(y);At(i,0x08e254);var z=source.D10.Snapshot();var pad=source.D14.Snapshot();At(i,0x08e259);c.Start.D10.CopyFrom(z);c.Start.D14.CopyFrom(pad);
   At(i,0x08e25e);var portal=PortalSelection.Select(TokenView(c.Start,i.At),TokenView(destination,i.At),new SameRegionPortalBoundary(ctx,i),i);c.PortalReturned=true;
   At(i,0x08e266);if(portal!=null)throw new MissingContract(0x08e276,"Portal choke and two-leg navigation branch is outside this precheck domain");
   At(i,0x08e383);WorldVectorDifferenceContracts.Subtract22C240(destination,c.Difference,c.Start,ctx.Navigation.World.State,ctx.NullDifference338458,i);c.DifferenceReturned=true;
   At(i,0x08e389);var dxBits=c.Difference.X0.Snapshot();At(i,0x08e392);var dyBits=c.Difference.Y4.Snapshot();At(i,0x08e398);var dzBits=c.Difference.Z8.Snapshot();float dx=dxBits.Read(At(i,0x08e39e));float xx=F(dx*dx);float dy=dyBits.Read(At(i,0x08e3a2));float yy=F(dy*dy);float dz=dzBits.Read(At(i,0x08e3a6));float zz=F(dz*dz);At(i,0x08e3aa);float squared=F(xx+yy);At(i,0x08e3ae);squared=F(squared+zz);At(i,0x08e3b2);float directDistance=(squared==0||Single.IsNaN(squared))?0:Sqrt(squared);
   At(i,0x08e3c8);GameMovedContracts.ConstructWorldVec228AB0(c.LocalOutput,i);
   At(i,0x08e3d7);if(distanceOutput!=null){At(i,0x08e3dc);MovementPathState.ForBound(manager,i.At).CachedReference70.Write(0);}
   At(i,0x08e3df);var state=MovementPathState.ForBound(manager,i.At);var cached=state.CachedReference70.Snapshot();At(i,0x08e3e2);c.LocalReference.CopyFrom(cached);
   At(i,0x08e3e5);var navigation=NavigationSearchSingletonContracts.Get01C510(ctx.Globals,i);c.SingletonReturned=true;
   At(i,0x08e428);byte found=NavigationSearchWrapperContracts.FindPath150590(navigation,c.Start,destination,radiusBits,c.LocalOutput,c.LocalReference,c.LocalDistance,null,c.Direction,Field<byte>.Of(0),ctx.Navigation,c.Navigation);c.NavigationReturned=true;
   At(i,0x08e42e);if(found==0)return Finish(c,4,0x08e432);
   At(i,0x08e43c);var returnedReference=c.LocalReference.Snapshot();At(i,0x08e43f);if(returnedReference.Read(i.At)==state.CachedReference70.Read(i.At)){
    c.SameReference=true;At(i,0x08e543);if(output!=null){Publish(c.LocalOutput,output,true,i);c.OutputPublished=true;}return Finish(c,0,0x08e55b);
   }
   float minimum=minimumBits.Read(At(i,0x08e448));At(i,0x08e44c);var lengthBits=c.LocalDistance.Snapshot();if(minimum>0){float length=lengthBits.Read(At(i,0x08e454));if(!(length>=minimum))return Finish(c,1,0x08e45a);}
   At(i,0x08e464);var rawX=c.Difference.X0.Snapshot();At(i,0x08e46a);var rawZ=c.Difference.Z8.Snapshot();At(i,0x08e473);var rawY=c.Difference.Y4.Snapshot();At(i,0x08e479);var one=ctx.One7A8054.Snapshot();
   float vy=rawY.Read(At(i,0x08e481));yy=F(vy*vy);float vx=rawX.Read(At(i,0x08e485));xx=F(vx*vx);At(i,0x08e489);float norm=F(xx+yy);float vz=rawZ.Read(At(i,0x08e490));zz=F(vz*vz);At(i,0x08e494);norm=F(norm+zz);At(i,0x08e49b);float root=Sqrt(norm);At(i,0x08e49f);var headingZ=c.Direction.Z.Snapshot();float inverse=F(one.Read(At(i,0x08e4a4))/root);At(i,0x08e4a8);var headingX=c.Direction.X.Snapshot();At(i,0x08e4ad);vx=F(vx*inverse);At(i,0x08e4b1);vz=F(inverse*vz);float dotX=F(headingX.Read(At(i,0x08e4b5))*vx);float dotZ=F(headingZ.Read(At(i,0x08e4b9))*vz);At(i,0x08e4bd);float dot=F(dotZ+dotX);At(i,0x08e4c1);if(!(dot>=Need(minimumDot,i.At).Read(i.At)))return Finish(c,3,0x08e4c7);
   At(i,0x08e4d1);var ratioBits=Need(maximumRatio,i.At).Snapshot();float ratio=ratioBits.Read(At(i,0x08e4d6));if(ratio>0){
    bool apply=Need(forceRatio,At(i,0x08e4db)).Read(i.At)!=0;if(!apply){At(i,0x08e4e3);apply=lengthBits.Read(i.At)>=ctx.DistanceGate7A80F0.Read(i.At);}
    if(apply){At(i,0x08e4ec);var baselineBits=Need(ratioBaseline,i.At).Snapshot();float baseline=baselineBits.Read(At(i,0x08e4f4));if(!(baseline>0))baseline=directDistance;float actual=F(lengthBits.Read(At(i,0x08e4ff))/baseline);At(i,0x08e503);if(actual>ratio)return Finish(c,2,0x08e508);}
   }
   At(i,0x08e516);if(output!=null){Publish(c.LocalOutput,output,false,i);c.OutputPublished=true;}At(i,0x08e52e);if(distanceOutput!=null){At(i,0x08e533);distanceOutput.CopyFrom(lengthBits);c.DistancePublished=true;}At(i,0x08e537);state.CachedReference70.CopyFrom(returnedReference);c.ReferencePublished=true;return Finish(c,0,0x08e55b);
  }
 }
}
