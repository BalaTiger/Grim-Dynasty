using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Typed Coords argument: basis36B followed by position12B. A borrowed view
 // may share the existing Entity's cells; it is not an allocated native ABI
 // layout. World21BB40 makes a separate local, never borrowing its input here.
 public sealed class LevelMoveCoordinates48 {
  readonly Field<float>[] basis;
  readonly EntityCoordinates64 borrowed;
  public readonly Field<uint> X24,Y28,Z2C;
  bool released;
  public LevelMoveCoordinates48(){basis=new Field<float>[9];for(int n=0;n<9;n++)basis[n]=new Field<float>();X24=new Field<uint>();Y28=new Field<uint>();Z2C=new Field<uint>();}
  public LevelMoveCoordinates48(EntityCoordinates64 shared){if(shared==null)throw new ArgumentNullException("shared");borrowed=shared;basis=shared.Basis18;X24=shared.Position00.D8;Y28=shared.Position00.DC;Z2C=shared.Position00.D10;}
  public Field<float>[] Basis00{get{return (Field<float>[])basis.Clone();}}
  public void MarkReleased(){released=true;}
  public void RequireLive(uint at){if(released)throw new MissingContract(at,"Own Coords argument explicitly released");if(borrowed!=null)borrowed.RequireLive(at);}
 }
 // Passive diagnostics only. No callback can mutate state between native
 // operations or supply a successful virtual/spatial result.
 public sealed class WorldSameRegionPublicationInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly PublicationGateInvocation Gate=new PublicationGateInvocation();
  readonly List<uint> steps=new List<uint>();
  LevelMoveCoordinates48 lastLocal;
  public uint[] ObservedAddresses{get{return steps.ToArray();}}
  public LevelMoveCoordinates48 LastWorldLocal{get{return lastLocal;}}
  internal void At(uint at){Native.At=at;steps.Add(at);}
  internal void Local(LevelMoveCoordinates48 value){lastLocal=value;}
 }
 public sealed class WorldSameRegionPublicationContext {
  sealed class IdentityComparer:IEqualityComparer<object>{public new bool Equals(object x,object y){return Object.ReferenceEquals(x,y);}public int GetHashCode(object o){return RuntimeHelpers.GetHashCode(o);}}
  readonly Dictionary<ulong,RegionPublicationView> regions=new Dictionary<ulong,RegionPublicationView>();
  readonly Dictionary<object,SpatialEntityView> entities=new Dictionary<object,SpatialEntityView>(new IdentityComparer());
  public readonly WorldPublicationRegistry Regions;
  public readonly ActorBoundingBoxContext Bounds;
  public readonly WorldPublicationGateContext Gates;
  public readonly EntitySpatialTreeContext Spatial;
  public readonly OwnObservedDiskPublicationLeaf BillboardLeaf=new OwnObservedDiskPublicationLeaf();
  bool tailSelected;
  public WorldSameRegionPublicationContext(WorldPublicationRegistry regions,ActorBoundingBoxContext bounds,WorldPublicationGateContext gates,EntitySpatialTreeContext spatial){
   if(regions==null||bounds==null||gates==null||spatial==null)throw new ArgumentNullException("publication composition");
   if(!Object.ReferenceEquals(bounds.Entities,gates.Entities))throw new ArgumentException("Bounds and gates must share the same Entity registry");
   Regions=regions;Bounds=bounds;Gates=gates;Spatial=spatial;
  }
  public void AttachRegion(RegionPublicationView view){
   if(view==null)throw new ArgumentNullException("view");
   if(!Object.ReferenceEquals(Regions.Coordinates.Region(view.RegionToken,0),view.Region))throw new InvalidOperationException("Region identity does not belong to the shared coordinate registry");
   // This is explicit own setup. Require this registry's canonical view too;
   // merely sharing Coordinates must not admit another registry's field cells.
   if(!Object.ReferenceEquals(Regions.BindRegionView(view.RegionToken,view.Region),view))throw new InvalidOperationException("Region publication cells belong to another registry");
   RegionPublicationView old;if(regions.TryGetValue(view.RegionToken,out old)){if(!Object.ReferenceEquals(old,view))throw new InvalidOperationException("Region token already has another publication view");return;}regions.Add(view.RegionToken,view);
  }
  public SpatialEntityView BindEntity(ActorBoundingBoxState actor){
   if(actor==null)throw new ArgumentNullException("actor");
   if(!Object.ReferenceEquals(Bounds.Bind(actor.Entity),actor))throw new InvalidOperationException("Actor must be the authoritative bounding-box view");
   SpatialEntityView old;if(entities.TryGetValue(actor.Entity.Identity,out old)){if(!Object.ReferenceEquals(old.Actor,actor))throw new InvalidOperationException("Entity already has another Actor view");return old;}
   var view=Spatial.BindEntity(actor);entities.Add(actor.Entity.Identity,view);return view;
  }
  public void SelectObservedDiskNoOpLeaves(){BillboardLeaf.SelectObservedDiskImplementation();tailSelected=true;}
  internal SpatialEntityView Entity(EntityMovementState entity,uint at){
   SpatialEntityView view;if(entity==null||!entities.TryGetValue(entity.Identity,out view)||!Object.ReferenceEquals(view.Actor.Entity,entity))throw new MissingContract(at,"Entity has no explicitly bound shared publication view");entity.RequireLive(at);return view;
  }
  internal RegionPublicationView Region(ulong token,uint at){
   RegionPublicationView view;if(!regions.TryGetValue(token,out view)||!Object.ReferenceEquals(Regions.Coordinates.Region(token,at),view.Region))throw new MissingContract(at,"Region has no explicitly bound shared publication view");return view;
  }
  internal void Tail015880(Invocation i){i.At=0x015880;if(!tailSelected)throw new MissingContract(i.At,"Loaded015880 is unobserved; explicit own disk-leaf selection is required");}
 }
 // Engine21BB40/12F4F0: same-region, stable own identity/cell domain only.
 // Separate or explicitly shared typed input cells are supported, not shifted
 // cross-type native byte aliases. No concurrent mutation, ABI, exception
 // cleanup, native lock or loaded-vtable compatibility is claimed. Native
 // calls use fixed evidenced implementations, never injectable success hooks.
 public static class WorldSameRegionPublicationContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own same-region publication dependency");return value;}
  static WorldSameRegionPublicationInvocation Call(WorldSameRegionPublicationInvocation call,uint at){Need(call,at).At(at);return call;}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static bool OrderedUnequal(float a,float b){return !Single.IsNaN(a)&&!Single.IsNaN(b)&&a!=b;}
  static Field<T> S<T>(Field<T> cell,uint at,WorldSameRegionPublicationInvocation c){c.At(at);return cell.Snapshot();}
  static LevelMoveCoordinates48 Input(LevelMoveCoordinates48 input,uint at,WorldSameRegionPublicationInvocation c){c.At(at);Need(input,at).RequireLive(at);return input;}
  static EntityCoordinates64 Frame(EntityCoordinates64 frame,uint at,WorldSameRegionPublicationInvocation c){c.At(at);Need(frame,at).RequireLive(at);return frame;}
  static byte Gate190(Field<object> table,EntityMovementState entity,WorldSameRegionPublicationContext context,WorldSameRegionPublicationInvocation c,uint at){
   c.At(at);var kind=ActorBoundingBoxContracts.RequireObservedDispatch(table,at);
   try{return kind==ObservedEntityDispatchKind.Character?WorldPublicationGates.CharacterDeleteOnEnteringUnloadedLevel016D20(entity,c.Gate):WorldPublicationGates.PlayerDeleteOnEnteringUnloadedLevel3D5BC0(entity,context.Gates,c.Gate);}
   finally{c.Native.At=c.Gate.Native.At;}
  }
  public static void SetCoords21BB40(object borrowedWorld,EntityMovementState entity,EntityCoordinates64 input,WorldSameRegionPublicationContext context,WorldSameRegionPublicationInvocation invocation){
   var c=Call(invocation,0x21bb53);Need(context,c.Native.At).Entity(entity,c.Native.At);
   var parentWord=entity.Parent130.Snapshot();c.At(0x21bb60);var parent=parentWord.Read(c.Native.At);Field<ulong> oldRegion;
   if(parent!=null){c.At(0x21bb65);oldRegion=Field<ulong>.Of(EntityMovementContracts.GetRegion07F130(parent,c.Native));}
   else{Frame(entity.CoordsD8,0x21bb6f,c);oldRegion=entity.CoordsD8.Position00.Q0.Snapshot();}
   Frame(input,0x21bb76,c);var newRegion=input.Position00.Q0.Snapshot();c.At(0x21bb79);ulong destination=newRegion.Read(c.Native.At);
   if(destination==0){c.At(0x21be77);return;}
   c.At(0x21bb82);ulong origin=oldRegion.Read(c.Native.At);if(origin==0){c.At(0x21be77);return;}
   entity.RequireLive(0x21bb8b);var firstTable=S(entity.Virtual00,0x21bb8b,c);
   byte gate=Gate190(firstTable,entity,context,c,0x21bb91);c.At(0x21bb97);
   if(gate==0){
    c.At(0x21bba0);var preloaded=WorldPublicationPrerequisites.GuaranteedGetLevelFalse1943E0(context.Region(destination,0x19444f),c.Native);
    c.At(0x21bba5);if(preloaded==null){c.At(0x21be77);return;}
   }
   c.At(0x21bbcc);if(origin!=destination){c.At(0x21bbd1);throw new MissingContract(c.Native.At,"Cross-region removal/network/addition path is outside this selected composition; prior gate/load prefix is retained");}
   c.At(0x21bc2c);var region=context.Region(destination,c.Native.At);bool needsSecondGate;
   if(region.Parent30.Read(c.Native.At)==null)needsSecondGate=false;
   else{
    c.At(0x21bc32);var checkedLevel=region.Level68.Read(c.Native.At);
    if(checkedLevel==null)needsSecondGate=true;
    else{c.At(0x21bc38);var unavailable=region.Region.Unavailable90.Snapshot();c.At(0x21bc3f);needsSecondGate=unavailable.Read(c.Native.At)!=0;}
   }
   if(!needsSecondGate){
    c.At(0x21bc43);var checkedLevel=region.Level68.Snapshot();c.At(0x21bc47);
    needsSecondGate=Need(checkedLevel.Read(c.Native.At),c.Native.At).Ready538C.Read(c.Native.At)==0;
   }
   if(needsSecondGate){
    entity.RequireLive(0x21bc54);var secondTable=S(entity.Virtual00,0x21bc54,c);
    gate=Gate190(secondTable,entity,context,c,0x21bc5a);c.At(0x21bc60);
    if(gate!=0){c.At(0x21bc68);throw new MissingContract(c.Native.At,"Unloaded-level deletion/logging/global-object path is outside this normal same-region domain");}
   }
   c.At(0x21bd21);var level=WorldPublicationPrerequisites.GuaranteedGetLevelFalse1943E0(region,c.Native);
   Frame(input,0x21bd26,c);var x=input.Position00.D8.Snapshot();var y=input.Position00.DC.Snapshot();
   c.At(0x21bd2f); // Origin/destination captured above are still the same.
   Frame(input,0x21be01,c);var z=input.Position00.D10.Snapshot();var local=new LevelMoveCoordinates48();c.Local(local);
   c.At(0x21be09);local.X24.CopyFrom(x);local.Y28.CopyFrom(y);
   Frame(input,0x21be11,c);var b=input.Basis18;var b0=b[0].Snapshot();var b1=b[1].Snapshot();
   c.At(0x21be1a);local.Z2C.CopyFrom(z);Frame(input,0x21be1d,c);var b2=b[2].Snapshot();var lb=local.Basis00;
   c.At(0x21be21);lb[0].CopyFrom(b0);lb[1].CopyFrom(b1);
   Frame(input,0x21be26,c);var b3=b[3].Snapshot();var b4=b[4].Snapshot();c.At(0x21be2c);lb[2].CopyFrom(b2);
   Frame(input,0x21be2f,c);var b5=b[5].Snapshot();c.At(0x21be33);lb[3].CopyFrom(b3);lb[4].CopyFrom(b4);
   Frame(input,0x21be38,c);var b6=b[6].Snapshot();var b7=b[7].Snapshot();c.At(0x21be3e);lb[5].CopyFrom(b5);
   Frame(input,0x21be41,c);var b8=b[8].Snapshot();c.At(0x21be45);lb[6].CopyFrom(b6);lb[7].CopyFrom(b7);c.At(0x21be4a);lb[8].CopyFrom(b8);
   c.At(0x21be4d);MoveEntity12F4F0(level,entity,local,context,c);c.At(0x21be77);
  }
  public static void MoveEntity12F4F0(LevelMovementState level,EntityMovementState entity,LevelMoveCoordinates48 input,WorldSameRegionPublicationContext context,WorldSameRegionPublicationInvocation invocation){
   var c=Call(invocation,0x12f511);Need(context,c.Native.At);var a=Input(input,0x12f511,c);var x=a.X24.Snapshot();var y=a.Y28.Snapshot();
   Input(a,0x12f51a,c);var z=a.Z2C.Snapshot();c.At(0x12f522);var oldRegion=Need(level,c.Native.At).Region08.Snapshot();
   var b=a.Basis00;Input(a,0x12f531,c);var saved=new Field<float>[9];saved[0]=b[0].Snapshot();saved[1]=b[1].Snapshot();
   Input(a,0x12f53c,c);saved[2]=b[2].Snapshot();Input(a,0x12f545,c);saved[3]=b[3].Snapshot();saved[4]=b[4].Snapshot();
   Input(a,0x12f54e,c);saved[5]=b[5].Snapshot();Input(a,0x12f557,c);saved[6]=b[6].Snapshot();saved[7]=b[7].Snapshot();Input(a,0x12f560,c);saved[8]=b[8].Snapshot();
   c.At(0x12f56c);context.Entity(entity,0x07f076);var old=EntityMovementContracts.GetCoords07F070(entity,new EntityCoordinates64(),c.Native);var ob=old.Basis18;
   uint[] comparisons={0x12f576,0x12f581,0x12f58c,0x12f597,0x12f5a2,0x12f5ad,0x12f5b8,0x12f5c3,0x12f5ce};bool different=false;
   for(int n=0;n<9;n++){c.At(comparisons[n]);old.RequireLive(c.Native.At);if(OrderedUnequal(saved[n].Read(c.Native.At),ob[n].Read(c.Native.At))){different=true;break;}}
   if(!different){
    Frame(old,0x12f5d4,c);var captured=old.Position00.Q0.Snapshot();c.At(0x12f5d7);different=captured.Read(c.Native.At)!=oldRegion.Read(c.Native.At);
    if(!different){c.At(0x12f5e1);different=OrderedUnequal(Float(x.Read(c.Native.At)),Float(old.Position00.D8.Read(c.Native.At)));}
    if(!different){c.At(0x12f5ec);different=OrderedUnequal(Float(y.Read(c.Native.At)),Float(old.Position00.DC.Read(c.Native.At)));}
    if(!different){c.At(0x12f5f2);z.Snapshot();} // MOVSS only: no Z comparison and no unchanged early return.
   }
   c.At(0x12f5f7);var region=level.Region08.Snapshot();Input(a,0x12f5fe,c);x=a.X24.Snapshot();y=a.Y28.Snapshot();
   Input(a,0x12f607,c);z=a.Z2C.Snapshot();Input(a,0x12f60f,c);var b0=b[0].Snapshot();var b1=b[1].Snapshot();
   Input(a,0x12f618,c);var b3=b[3].Snapshot();var b4=b[4].Snapshot();Input(a,0x12f620,c);var b2=b[2].Snapshot();
   // Native XMM1 captures Z/unknown padding/basis0/basis1 at12F623.
   c.At(0x12f623);var packetZ=z.Snapshot();var padding=new Field<uint>();var packetB0=b0.Snapshot();var packetB1=b1.Snapshot();
   Input(a,0x12f62a,c);var b5=b[5].Snapshot();Input(a,0x12f632,c);var b6=b[6].Snapshot();var b7=b[7].Snapshot();
   c.At(0x12f63c);var packetRegion=region.Snapshot();var packetX=x.Snapshot();var packetY=y.Snapshot();Input(a,0x12f643,c);var b8=b[8].Snapshot();
   var dest=Frame(entity.CoordsD8,0x12f646,c);entity.RequireLive(c.Native.At);dest.Position00.Q0.CopyFrom(packetRegion);dest.Position00.D8.CopyFrom(packetX);dest.Position00.DC.CopyFrom(packetY);
   c.At(0x12f650);var packetB2=b2.Snapshot();var packetB3=b3.Snapshot();var packetB4=b4.Snapshot();var packetB5=b5.Snapshot();
   entity.RequireLive(0x12f654);var table270=S(entity.Virtual00,0x12f654,c);var db=dest.Basis18;
   Frame(dest,0x12f657,c);dest.Position00.D10.CopyFrom(packetZ);dest.Position00.D14.CopyFrom(padding);db[0].CopyFrom(packetB0);db[1].CopyFrom(packetB1);
   c.At(0x12f65e);var packetB6=b6.Snapshot();var packetB7=b7.Snapshot();var packetB8=b8.Snapshot();var tail=new Field<uint>();
   Frame(dest,0x12f662,c);db[2].CopyFrom(packetB2);db[3].CopyFrom(packetB3);db[4].CopyFrom(packetB4);db[5].CopyFrom(packetB5);
   Frame(dest,0x12f669,c);db[6].CopyFrom(packetB6);db[7].CopyFrom(packetB7);db[8].CopyFrom(packetB8);dest.Tail3C.CopyFrom(tail);
   c.At(0x12f670);ActorBoundingBoxContracts.RequireObservedDispatch(table270,c.Native.At);context.BillboardLeaf.BillboardPunctuation00CE70(entity,c.Native);
   entity.RequireLive(0x12f676);var tableD8=S(entity.Virtual00,0x12f676,c);c.At(0x12f67c);ActorBoundingBoxContracts.RequireObservedDispatch(tableD8,c.Native.At);
   var spatial=context.Entity(entity,c.Native.At);ActorBoundingBoxContracts.UpdateBoundingBox02A990(spatial.Actor,context.Bounds,c.Native);
   c.At(0x12f682);var tree=level.Spatial10.Snapshot();c.At(0x12f689);EntitySpatialTreeContracts.UpdateCaptured1333E0(tree,spatial,context.Spatial,c.Native);
   entity.RequireLive(0x12f68e);var table90=S(entity.Virtual00,0x12f68e,c);c.At(0x12f6b3);ActorBoundingBoxContracts.RequireObservedDispatch(table90,c.Native.At);context.Tail015880(c.Native);
  }
 }
}
