using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 public enum ObservedEntityDispatchKind { Character,Player }
 // One authoritative sidecar per existing Entity. These cells start Unknown.
 // They do not construct an Actor, load a mesh, initialize a world, or copy
 // CoordsD8/Parent130 from another state. Raw ID endpoints are never dereferenced
 // by this component; only the native zero-count path is supported.
 public sealed class ActorBoundingBoxState {
  public readonly EntityMovementState Entity;
  public readonly WorldBounds24 Box44=new WorldBounds24(),Box5C=new WorldBounds24();
  public readonly Field<byte> Flag74=new Field<byte>();
  public readonly Field<ulong> AttachedBegin78=new Field<ulong>(),AttachedEnd80=new Field<ulong>(),AttachedCapacity88=new Field<ulong>();
  public readonly Field<BorrowedMeshBounds> Mesh2668=new Field<BorrowedMeshBounds>(),Mesh2670=new Field<BorrowedMeshBounds>();
  internal ActorBoundingBoxState(EntityMovementState entity){Entity=entity;}
 }
 // A borrowed mesh identity and its observed inline24-byte box, not a
 // synthesized mesh resource. Sharing this view shares all six cells/lifetime.
 public sealed class BorrowedMeshBounds {
  public readonly object Identity;
  public readonly WorldBounds24 Bounds1A8=new WorldBounds24();
  bool released;
  internal BorrowedMeshBounds(object identity){Identity=identity;}
  public void MarkReleased(){released=true;}
  public void RequireLive(uint at){if(released)throw new MissingContract(at,"Borrowed mesh was explicitly released; native mesh lifetime is unobserved");}
 }
 public sealed class ActorBoundingBoxContext {
  sealed class IdentityComparer:IEqualityComparer<object> {
   public new bool Equals(object x,object y){return Object.ReferenceEquals(x,y);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  readonly Dictionary<object,ActorBoundingBoxState> states=new Dictionary<object,ActorBoundingBoxState>(new IdentityComparer());
  readonly Dictionary<object,BorrowedMeshBounds> meshes=new Dictionary<object,BorrowedMeshBounds>(new IdentityComparer());
  public readonly EntityMovementRegistry Entities;
  public readonly Field<uint> Mask34C410=new Field<uint>();
  public ActorBoundingBoxContext(EntityMovementRegistry entities){if(entities==null)throw new ArgumentNullException("entities");Entities=entities;}
  public ActorBoundingBoxState Bind(EntityMovementState entity){
   if(entity==null)throw new ArgumentNullException("entity");
   if(!Object.ReferenceEquals(Entities.Entity(entity.Identity,0x02a9a2),entity))throw new InvalidOperationException("Entity identity already has a different authoritative view");
   ActorBoundingBoxState prior;if(states.TryGetValue(entity.Identity,out prior))return prior;
   var value=new ActorBoundingBoxState(entity);states.Add(entity.Identity,value);return value;
  }
  public BorrowedMeshBounds BindMesh(object identity){
   if(identity==null)throw new ArgumentNullException("identity");BorrowedMeshBounds prior;
   if(meshes.TryGetValue(identity,out prior))return prior;
   var value=new BorrowedMeshBounds(identity);meshes.Add(identity,value);return value;
  }
  // Exact cached disk bytesffffff7f, r1-navigation-nearest-constants; reused
  // by r1-entity-bounds-thunk against the same current Engine fingerprint.
  // This selects own constants; it is not an observation of loaded memory.
  public void UseObservedMask(){Mask34C410.Write(0x7fffffffu);}
  internal void RequireState(ActorBoundingBoxState state,uint at){
   ActorBoundingBoxState current;
   if(state==null||!states.TryGetValue(state.Entity.Identity,out current)||!Object.ReferenceEquals(state,current)||
      !Object.ReferenceEquals(Entities.Entity(state.Entity.Identity,at),state.Entity))
    throw new MissingContract(at,"Entity has no unique actor-bounds sidecar in this composition");
   state.Entity.RequireLive(at);
  }
  internal void RequireMesh(BorrowedMeshBounds mesh,uint at){
   BorrowedMeshBounds current;
   if(mesh==null||!meshes.TryGetValue(mesh.Identity,out current)||!Object.ReferenceEquals(mesh,current))
    throw new MissingContract(at,"Borrowed mesh has no unique live view in this composition");
   mesh.RequireLive(at);
  }
 }
 // Real Rea sources: Engine02A990 in r1-engine-entity-publication-core;
 // Engine07ED70 in r1-engine-spatial-ground; Game06DA30 in
 // r1-game-level-publication-tail. Fixed disk dispatch mappings are selected
 // explicitly below. No Func/delegate can replace a native query result.
 //
 // Domain: the shared Entity and separately owned mesh/coordinate/box views
 // are stable except for the sequential native writes represented here.
 // Native ABI, allocation/EH, concurrent mutation, arbitrary byte aliases and
 // MXCSR/FTZ/DAZ/trap modes are not emulated. Every scalar multiply/add rounds
 // separately to binary32; NaNs are not replaced, exact NaN payload choices
 // across host/native FP implementations remain unverified.
 public static class ActorBoundingBoxContracts {
  static readonly object characterDispatch=new object(),playerDispatch=new object();
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own actor-bounds dependency");return value;}
  static Invocation At(Invocation invocation,uint at){var i=Need(invocation,at);i.At=at;return i;}
  static float F(float v){return BitConverter.ToSingle(BitConverter.GetBytes(v),0);}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}
  static Field<float> M(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return Field<float>.Of(F(x*y));}
  static Field<float> A(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return Field<float>.Of(F(x+y));}
  static Field<float> AU(Field<float> a,Field<uint> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=Float(b.Read(at));return Field<float>.Of(F(x+y));}
  static Field<float> Q(Field<float> value,Field<uint> mask,uint at,Invocation i){i.At=at;uint bits=Bits(value.Read(at));return Field<float>.Of(Float(bits&mask.Read(at)));}
  public static ObservedEntityDispatchKind RequireObservedDispatch(Field<object> captured,uint at){
   object table=Need(captured,at).Read(at);
   if(Object.ReferenceEquals(table,characterDispatch))return ObservedEntityDispatchKind.Character;
   if(Object.ReferenceEquals(table,playerDispatch))return ObservedEntityDispatchKind.Player;
   throw new MissingContract(at,"Current virtual table is not an explicitly selected observed Character/Player implementation");
  }
  public static void SelectObservedCharacterDispatch(ActorBoundingBoxState state,bool player){Need(state,0x02a9a2).Entity.Virtual00.Write(player?playerDispatch:characterDispatch);}
  // Project only these three actual base-constructor writes. This is not the
  // complete07D020 constructor or proof of post-load emptiness. Future readers
  // always re-read the live endpoints; no permanent empty flag is maintained.
  public static void InitializeAttachedIdFields07D020(ActorBoundingBoxState state,Invocation invocation){
   var i=At(invocation,0x07d089);Need(state,i.At).Entity.RequireLive(i.At);state.AttachedBegin78.Write(0);
   i.At=0x07d08d;state.AttachedEnd80.Write(0);i.At=0x07d094;state.AttachedCapacity88.Write(0);
  }
  public static byte IsInWorld07ED70(EntityMovementState entity,Invocation invocation){
   var i=At(invocation,0x07ed70);Need(entity,i.At).RequireLive(i.At);
   if(entity.Parent130.Read(i.At)!=null){i.At=0x07ed89;return 0;}
   i.At=0x07ed7a;entity.CoordsD8.RequireLive(i.At);ulong region=entity.CoordsD8.Position00.Q0.Read(i.At);
   if(region==0){i.At=0x07ed89;return 0;}i.At=0x07ed86;return 1;
  }
  public static BorrowedMeshBounds GetMeshInstance06DA30(ActorBoundingBoxState state,Invocation invocation){
   var i=At(invocation,0x06da40);Need(state,i.At).Entity.RequireLive(i.At);
   var first=state.Mesh2668.Snapshot();i.At=0x06da43;var value=first.Read(i.At);
   if(value!=null){i.At=0x06da5d;return value;}
   i.At=0x06da40;state.Entity.RequireLive(i.At);var second=state.Mesh2670.Snapshot();
   i.At=0x06da43;value=second.Read(i.At);if(value!=null){i.At=0x06da5d;return value;}
   i.At=0x06da53;throw new MissingContract(i.At,"Actor::GetMeshInstance import609688 fallback is unclosed; no default mesh or box is supplied");
  }
  static void Box16(WorldBounds24 box,Field<float> x,Field<float> y,Field<float> z,Field<float> ex,uint at,Invocation i){
   i.At=at;var a=x.Snapshot();var b=y.Snapshot();var c=z.Snapshot();var d=ex.Snapshot();
   box.Center00.CopyFrom(a);box.Center04.CopyFrom(b);box.Center08.CopyFrom(c);box.Extent0C.CopyFrom(d);
  }
  static void Box8(WorldBounds24 box,Field<float> ey,Field<float> ez,uint at,Invocation i){
   i.At=at;var a=ey.Snapshot();var b=ez.Snapshot();box.Extent10.CopyFrom(a);box.Extent14.CopyFrom(b);
  }
  public static void UpdateBoundingBox02A990(ActorBoundingBoxState state,ActorBoundingBoxContext context,Invocation invocation){
   var i=At(invocation,0x02a9a2);Need(context,i.At).RequireState(state,i.At);var entity=state.Entity;
   var firstTable=entity.Virtual00.Snapshot();i.At=0x02a9a8;RequireObservedDispatch(firstTable,i.At);
   if(IsInWorld07ED70(entity,i)==0){i.At=0x02ae73;return;}
   i.At=0x02aa1a;var frame=EntityMovementContracts.GetCoords07F070(entity,new EntityCoordinates64(),i);
   // Use the actual returned identity. Packed MOVs preserve Unknown until the
   // instruction that consumes it. The native captured order is not XYZ order.
   i.At=0x02aa22;Need(frame,i.At).RequireLive(i.At);var tz=frame.Position00.D10.Snapshot();var basis=frame.Basis18;
   i.At=0x02aa25;var tx=frame.Position00.D8.Snapshot();var ty=frame.Position00.DC.Snapshot();
   i.At=0x02aa30;var m0=basis[0].Snapshot();var m1=basis[1].Snapshot();
   i.At=0x02aa3b;var m3=basis[3].Snapshot();var m4=basis[4].Snapshot();
   i.At=0x02aa44;var m2=basis[2].Snapshot();i.At=0x02aa47;var m5=basis[5].Snapshot();
   i.At=0x02aa4e;var m8=basis[8].Snapshot();
   i.At=0x02aa55;entity.RequireLive(i.At);var secondTable=entity.Virtual00.Snapshot();
   i.At=0x02aa5e;var m6=basis[6].Snapshot();var m7=basis[7].Snapshot();
   i.At=0x02aa70;RequireObservedDispatch(secondTable,i.At);var mesh=GetMeshInstance06DA30(state,i);
   i.At=0x02aa93;context.RequireMesh(mesh,i.At);var bounds=mesh.Bounds1A8;
   var cx=bounds.Center00.Snapshot();var cy=bounds.Center04.Snapshot();var cz=bounds.Center08.Snapshot();var ex=bounds.Extent0C.Snapshot();
   i.At=0x02aa9b;mesh.RequireLive(i.At);var ey=bounds.Extent10.Snapshot();var ez=bounds.Extent14.Snapshot();
   // Scalar register schedule from02AAD4 onward, including interleaved axes.
   var x13=m3;var x14=m0;var x6=x13;var x15=m6;var x0=x14;
   var x9=m4;var x10=m1;var x11=m7;var x3=x9;var x7=m5;var x8=m2;var x2=x7;
   x0=M(x0,cx,0x02aad4,i);
   var x5=cy;x6=M(x6,x5,0x02aae8,i);x3=M(x3,x5,0x02aaec,i);
   x6=A(x6,x0,0x02aaf0,i);x2=M(x2,x5,0x02aaf4,i);x5=m8;x0=x10;
   x0=M(x0,cx,0x02ab02,i);var x4=cz;var x1=M(x15,x4,0x02ab0c,i);
   x3=A(x3,x0,0x02ab10,i);x0=x8;x0=M(x0,cx,0x02ab18,i);
   x6=A(x6,x1,0x02ab1d,i);x1=M(x11,x4,0x02ab25,i);
   x2=A(x2,x0,0x02ab29,i);x6=AU(x6,tx,0x02ab2d,i);
   x3=A(x3,x1,0x02ab33,i);x1=M(x5,x4,0x02ab3a,i);
   x3=AU(x3,ty,0x02ab41,i);x2=A(x2,x1,0x02ab47,i);x2=AU(x2,tz,0x02ab4e,i);
   var resultX=x6;var resultY=x3;var resultZ=x2;
   i.At=0x02ab5f;var mask=context.Mask34C410.Snapshot();
   i.At=0x02ab71;var parentWord=entity.Parent130.Snapshot();
   x15=M(x15,ez,0x02ab78,i);x11=M(x11,ez,0x02ab80,i);
   i.At=0x02ab85;entity.RequireLive(i.At);state.Flag74.Write(1);
   x5=M(x5,ez,0x02ab89,i);x15=Q(x15,mask,0x02ab8d,i);
   x11=Q(x11,mask,0x02ab97,i);x8=M(x8,ex,0x02aba0,i);
   x5=Q(x5,mask,0x02aba5,i);x13=M(x13,ey,0x02aba8,i);x14=M(x14,ex,0x02abad,i);
   x8=Q(x8,mask,0x02abb2,i);x9=M(x9,ey,0x02abb6,i);
   x13=Q(x13,mask,0x02abbb,i);x10=M(x10,ex,0x02abbf,i);x14=Q(x14,mask,0x02abc4,i);
   x14=A(x14,x13,0x02abd1,i);x9=Q(x9,mask,0x02abdf,i);x7=M(x7,ey,0x02abe3,i);
   x10=Q(x10,mask,0x02abe7,i);x10=A(x10,x9,0x02abeb,i);x15=A(x15,x14,0x02abf9,i);
   x7=Q(x7,mask,0x02ac07,i);x8=A(x8,x7,0x02ac0a,i);x11=A(x11,x10,0x02ac0f,i);x8=A(x8,x5,0x02ac2a,i);
   // X/Y extent association differs from Z. Do not use a matrix library,
   // reassociate additions, or fuse a multiply/add.
   entity.RequireLive(0x02ac59);Box16(state.Box5C,resultX,resultY,resultZ,x15,0x02ac59,i);
   Box8(state.Box5C,x11,x8,0x02ac5d,i);
   i.At=0x02ac63;var parent=parentWord.Read(i.At);ulong region;
   if(parent!=null){i.At=0x02ac68;region=EntityMovementContracts.GetRegion07F130(parent,i);}
   else {i.At=0x02ac72;entity.CoordsD8.RequireLive(i.At);region=entity.CoordsD8.Position00.Q0.Read(i.At);}
   i.At=0x02ac79;
   if(region!=0){
    i.At=0x02ac82;var endWord=state.AttachedEnd80.Snapshot();
    i.At=0x02ac89;ulong end=endWord.Read(i.At),begin=state.AttachedBegin78.Read(i.At);
    long byteDifference=unchecked((long)(end-begin));i.At=0x02ac97;long count=byteDifference>>2;
    i.At=0x02ac9e;if(count!=0)throw new MissingContract(i.At,"Nonempty attached-ID iteration is outside this selected domain; prior flag and Box5C writes are retained");
   }
   entity.RequireLive(0x02ae4f);Box16(state.Box44,resultX,resultY,resultZ,x15,0x02ae4f,i);
   Box8(state.Box44,x11,x8,0x02ae5b,i);i.At=0x02ae73;
  }
 }
}
