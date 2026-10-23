using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GrimDynasty.ReverseClient.Actions {
 // Additional authoritative fields bound to an existing WorldRegion view.
 // This does not copy Critical18/Unavailable90 or create a native Level.
 public sealed class RegionPublicationView {
  internal readonly WorldPublicationRegistry Registry;
  public readonly WorldRegion Region;
  public readonly ulong RegionToken;
  public readonly Field<object> Parent30=new Field<object>();
  public readonly Field<LevelMovementState> Level68=new Field<LevelMovementState>();
  public readonly Field<uint> Reset88=new Field<uint>();
  internal RegionPublicationView(WorldPublicationRegistry registry,WorldRegion region,ulong token){Registry=registry;Region=region;RegionToken=token;}
 }
 public sealed class LevelMovementState {
  internal readonly WorldPublicationRegistry Registry;
  public readonly object Identity;
  public readonly Field<ulong> Region08=new Field<ulong>();
  public readonly Field<object> Spatial10=new Field<object>();
  public readonly Field<byte> Ready538C=new Field<byte>();
  internal LevelMovementState(WorldPublicationRegistry registry,object identity){Registry=registry;Identity=identity;}
 }
 // One explicit composition owns one view per identity. Binding is own setup,
 // never a lazy native allocation or an implicit initialized field value.
 public sealed class WorldPublicationRegistry {
  sealed class IdentityComparer:IEqualityComparer<object> {
   public new bool Equals(object x,object y){return Object.ReferenceEquals(x,y);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  readonly Dictionary<object,RegionPublicationView> regions=new Dictionary<object,RegionPublicationView>(new IdentityComparer());
  readonly Dictionary<object,LevelMovementState> levels=new Dictionary<object,LevelMovementState>(new IdentityComparer());
  public readonly WorldCoordinateState Coordinates;
  public WorldPublicationRegistry(WorldCoordinateState coordinates){if(coordinates==null)throw new ArgumentNullException("coordinates");Coordinates=coordinates;}
  public RegionPublicationView BindRegionView(ulong token,WorldRegion region){
   if(region==null)throw new ArgumentNullException("region");RegionPublicationView prior;
   if(regions.TryGetValue(region.Identity,out prior)){
    if(!Object.ReferenceEquals(prior.Region,region)||prior.RegionToken!=token)throw new InvalidOperationException("Region identity already has a different view/token");return prior;
   }
   Coordinates.AttachRegion(token,region);var view=new RegionPublicationView(this,region,token);regions.Add(region.Identity,view);return view;
  }
  public LevelMovementState BindLevelView(object identity){
   if(identity==null)throw new ArgumentNullException("identity");LevelMovementState prior;
   if(levels.TryGetValue(identity,out prior))return prior;
   var view=new LevelMovementState(this,identity);levels.Add(identity,view);return view;
  }
  internal void RequireOwned(LevelMovementState level,uint at){
   if(level==null)return;LevelMovementState prior;
   if(!Object.ReferenceEquals(level.Registry,this)||!levels.TryGetValue(level.Identity,out prior)||!Object.ReferenceEquals(level,prior))
    throw new MissingContract(at,"Level pointer has no unique view in this own composition");
  }
 }
 // Explicit OWN platform adapter. Monitor supplies real recursive ownership
 // and exclusion; this is not a native CRITICAL_SECTION address/layout or a
 // claim about loaded native synchronization. It has no success callbacks.
 public sealed class OwnReentrantRegionLock {
  readonly object gate=new object();
  int depth;
  OwnReentrantRegionLock(){}
  public static OwnReentrantRegionLock InitializeOwn(){return new OwnReentrantRegionLock();}
  public bool IsHeldByCurrentThread{get{return Monitor.IsEntered(gate);}}
  public int CurrentThreadDepth{get{return Monitor.IsEntered(gate)?depth:0;}}
  public void Enter(uint at){Monitor.Enter(gate);depth++;}
  public void Leave(uint at){
   if(!Monitor.IsEntered(gate))throw new MissingContract(at,"Own region lock is not held by this thread");
   depth--;Monitor.Exit(gate);
  }
 }
 public static class WorldPublicationPrerequisites {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own publication view");return value;}
  static OwnReentrantRegionLock Lock(object identity,uint at){
   var value=identity as OwnReentrantRegionLock;
   if(value==null)throw new MissingContract(at,"Critical18 is not an explicitly initialized own synchronization adapter");return value;
  }
  // Engine228AB0, r1-position-publication07/08: write exactly the first QWORD,
  // return the incoming address. Other position/orientation/padding cells are
  // neither initialized nor consumed. Only the existing WorldCoords view is
  // exposed here; other exported type aliases are not additional guarantees.
  public static EntityCoordinates64 ConstructWorldCoords228AB0(EntityCoordinates64 value,Invocation invocation){
   var i=Need(invocation,0x228ab0);i.At=0x228ab0;Need(value,i.At).RequireLive(i.At);
   value.Position00.Q0.Write(0);i.At=0x228ab7;return value;
  }
  // Engine194440(false,false), r1-region-load-publication07/08. Only the real
  // preloaded fast return is supported. A known null Level stops at194481,
  // before the unimplemented cold-path flags/logging/load effects, not false
  // or fabricated success. No render-ready5381 read exists on this false arm.
  static void RequirePreloadedFalse194440(RegionPublicationView view,Invocation i){
   i.At=0x19444f;var captured=Need(view,i.At).Level68.Snapshot();
   i.At=0x19445d;var level=captured.Read(i.At);
   if(level==null){i.At=0x194481;throw new MissingContract(i.At,"Cold Region::LoadLevel path is outside this preloaded own domain; native cold effects not executed");}
   view.Registry.RequireOwned(level,i.At);i.At=0x19446f;
  }
  // Engine1943E0(false), r1-same-region-movement04/05. This returns the same
  // borrowed Level identity, not a copied/default object. The supported own
  // lock pointer is stable during its critical section. If that precondition
  // is broken, stop without pretending to leave another lock; callers must
  // clean up any explicitly held own locks after such an out-of-domain error.
  public static LevelMovementState GuaranteedGetLevelFalse1943E0(RegionPublicationView view,Invocation invocation){
   var i=Need(invocation,0x1943f0);i.At=0x1943f0;RequirePreloadedFalse194440(view,i);
   i.At=0x1943f5;var unavailable=view.Region.Unavailable90.Snapshot();
   i.At=0x1943fe;view.Reset88.Write(0);
   i.At=0x194404;if(unavailable.Read(i.At)!=0){i.At=0x194426;return null;}
   i.At=0x194408;var enterPointer=view.Region.Critical18.Snapshot();
   i.At=0x19440c;var entered=Lock(enterPointer.Read(i.At),i.At);entered.Enter(i.At);
   i.At=0x194412;view.Reset88.Write(0);
   i.At=0x194418;var leavePointer=view.Region.Critical18.Snapshot();
   i.At=0x19441c;var borrowed=view.Level68.Snapshot();
   i.At=0x194420;var leaving=Lock(leavePointer.Read(i.At),i.At);
   if(!Object.ReferenceEquals(entered,leaving))throw new MissingContract(i.At,"Critical18 changed during own critical section; no different-lock leave is fabricated");
   leaving.Leave(i.At);
   i.At=0x194426;var result=borrowed.Read(i.At);view.Registry.RequireOwned(result,i.At);return result;
  }
 }
}
