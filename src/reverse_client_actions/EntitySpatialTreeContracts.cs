using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // A spatial view borrows the authoritative Actor box; it never mirrors it.
 // Binding does not establish a list membership, network mode or native node.
 public sealed class SpatialEntityView {
  public readonly ActorBoundingBoxState Actor;
  public readonly Field<SpatialNodeState> NodeD0=new Field<SpatialNodeState>();
  public readonly Field<SpatialEntityView> Previous120=new Field<SpatialEntityView>(),Next128=new Field<SpatialEntityView>();
  public readonly Field<byte> FlagsCB=new Field<byte>();
  internal SpatialEntityView(ActorBoundingBoxState actor){Actor=actor;}
  public WorldBounds24 Box44{get{return Actor.Box44;}}
 }
 public sealed class SpatialNodeState {
  internal readonly EntitySpatialTreeContext Context;
  public readonly object Identity;
  public readonly Field<uint> VirtualRva00=new Field<uint>();
  public readonly Field<SpatialNodeState> Parent08=new Field<SpatialNodeState>();
  readonly Field<SpatialNodeState>[] children={new Field<SpatialNodeState>(),new Field<SpatialNodeState>(),new Field<SpatialNodeState>(),new Field<SpatialNodeState>()};
  public readonly WorldBounds24 Box30=new WorldBounds24();
  public readonly Field<SpatialEntityView> Head48=new Field<SpatialEntityView>();
  public readonly Field<int> Depth50=new Field<int>();
  public readonly Field<byte> Flags54=new Field<byte>();
  bool released;
  internal SpatialNodeState(EntitySpatialTreeContext context,object identity){Context=context;Identity=identity;}
  public Field<SpatialNodeState> Child(int index){if(index<0||index>3)throw new ArgumentOutOfRangeException("index");return children[index];}
  public void RequireLive(uint at){if(released)throw new MissingContract(at,"Own spatial node explicitly released");}
  internal void MarkReleased(){released=true;}
 }
 public sealed class SpatialTreeState {
  public readonly object Identity;
  public readonly Field<uint> ChangeCount08=new Field<uint>();
  public readonly Field<SpatialNodeState> Root10=new Field<SpatialNodeState>();
  public SpatialTreeState(object identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public sealed class EntitySpatialTreeContext {
  sealed class IdentityComparer:IEqualityComparer<object> {
   public new bool Equals(object x,object y){return Object.ReferenceEquals(x,y);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  readonly Dictionary<object,SpatialEntityView> entities=new Dictionary<object,SpatialEntityView>(new IdentityComparer());
  readonly Dictionary<object,SpatialNodeState> nodes=new Dictionary<object,SpatialNodeState>(new IdentityComparer());
  readonly Dictionary<object,SpatialTreeState> trees=new Dictionary<object,SpatialTreeState>(new IdentityComparer());
  // Weak keys reject stale identity rebinding without retaining every freed
  // allocation. The active map alone consumes the own finite storage budget.
  readonly ConditionalWeakTable<object,object> releasedIdentities=new ConditionalWeakTable<object,object>();
  // Exact operand bits are supplied from retained evidence or explicit own
  // inputs. No decompiler decimal is treated as a recovered disk constant.
  public readonly Field<uint> SplitBits34BAD8=new Field<uint>(),HalfBits34BADC=new Field<uint>(),XorBits34C450=new Field<uint>();
  public readonly int OwnNodeLimit;
  public readonly int OwnTransitionLimit=64;
  public int ActiveNodeCount{get{return nodes.Count;}}
  public EntitySpatialTreeContext():this(4096){}
  public EntitySpatialTreeContext(int ownNodeLimit){if(ownNodeLimit<1)throw new ArgumentOutOfRangeException("ownNodeLimit");OwnNodeLimit=ownNodeLimit;}
  // Explicit OWN selection of exact disk operands, r1-ground-loader-slot:
  // c5dcea3e / 0000003f / 00000080. Loaded values remain unobserved.
  public void UseObservedDiskConstants(){SplitBits34BAD8.Write(0x3eeadcc5u);HalfBits34BADC.Write(0x3f000000u);XorBits34C450.Write(0x80000000u);}
  public void BindTree(SpatialTreeState tree){
   if(tree==null)throw new ArgumentNullException("tree");SpatialTreeState prior;
   if(trees.TryGetValue(tree.Identity,out prior)){if(!Object.ReferenceEquals(prior,tree))throw new InvalidOperationException("Tree identity already has another authoritative view");return;}
   trees.Add(tree.Identity,tree);
  }
  internal SpatialTreeState Tree(object identity,uint at){SpatialTreeState tree;if(identity==null||!trees.TryGetValue(identity,out tree))throw new MissingContract(at,"Tree identity has no explicit own binding");return tree;}
  public SpatialEntityView BindEntity(ActorBoundingBoxState actor){
   if(actor==null)throw new ArgumentNullException("actor");SpatialEntityView old;
   if(entities.TryGetValue(actor.Entity.Identity,out old)){if(!Object.ReferenceEquals(old.Actor,actor))throw new InvalidOperationException("Spatial identity already borrows a different Actor box");return old;}
   var view=new SpatialEntityView(actor);entities.Add(actor.Entity.Identity,view);return view;
  }
  public SpatialNodeState BindNode(object identity){
   if(identity==null)throw new ArgumentNullException("identity");SpatialNodeState old;if(nodes.TryGetValue(identity,out old))return old;
   object tombstone;if(releasedIdentities.TryGetValue(identity,out tombstone))throw new InvalidOperationException("Released own node identity cannot be rebound; native address reuse is not modeled");
   if(nodes.Count>=OwnNodeLimit)throw new MissingContract(0x13465c,"Own finite node-storage domain exhausted");
   var node=new SpatialNodeState(this,identity);nodes.Add(identity,node);return node;
  }
  internal SpatialEntityView Entity(SpatialEntityView entity,uint at){
   SpatialEntityView bound;if(entity==null||!entities.TryGetValue(entity.Actor.Entity.Identity,out bound)||!Object.ReferenceEquals(bound,entity))throw new MissingContract(at,"Spatial Entity has no authoritative shared binding");entity.Actor.Entity.RequireLive(at);return entity;
  }
  internal SpatialNodeState Node(SpatialNodeState node,uint at){
   if(node!=null)node.RequireLive(at);SpatialNodeState bound;if(node==null||!Object.ReferenceEquals(node.Context,this)||!nodes.TryGetValue(node.Identity,out bound)||!Object.ReferenceEquals(bound,node))throw new MissingContract(at,"Spatial node is outside this own composition");return node;
  }
  internal SpatialNodeState AllocateChild(uint at){if(nodes.Count>=OwnNodeLimit)throw new MissingContract(at,"Own finite node-storage domain exhausted before allocation");return BindNode(new object());}
  internal void FreeOwnNode(SpatialNodeState node,uint at){
   Node(node,at);releasedIdentities.Add(node.Identity,new object());node.MarkReleased();nodes.Remove(node.Identity);
  }
 }
 // Binary32 round-to-nearest with masked exceptions is the explicit own
 // arithmetic domain. No native MXCSR/FTZ/DAZ, allocator/EH or concurrent
 // mutation behavior is claimed. All mutations precede unresolved-call stops
 // in native order; no callbacks can manufacture a successful update.
 public static class EntitySpatialTreeContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own spatial address");return value;}
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Float(uint value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
  static float Add(float x,float y){return F(x+y);}static float Sub(float x,float y){return F(x-y);}static float Mul(float x,float y){return F(x*y);}
  static bool Carry(float x,float y){return Single.IsNaN(x)||Single.IsNaN(y)||x<y;}
  static bool Above(float x,float y){return x>y;}
  static float Min(float x,float y){return x<y?x:y;}static float Max(float x,float y){return x>y?x:y;}
  static Field<T> S<T>(Field<T> value,uint at,Invocation i){i.At=at;return value.Snapshot();}
  static T R<T>(Field<T> value,uint at,Invocation i){i.At=at;return value.Read(at);}
  static void W<T>(Field<T> value,T data,uint at,Invocation i){i.At=at;value.Write(data);}
  static void C<T>(Field<T> value,Field<T> data,uint at,Invocation i){i.At=at;value.CopyFrom(data);}
  static float Constant(Field<uint> value,uint at,Invocation i){return Float(R(value,at,i));}
  static SpatialNodeState N(SpatialNodeState node,EntitySpatialTreeContext c,uint at,Invocation i){i.At=at;return Need(c,at).Node(node,at);}
  static SpatialEntityView E(SpatialEntityView entity,EntitySpatialTreeContext c,uint at,Invocation i){i.At=at;return Need(c,at).Entity(entity,at);}

  public static void Update1333E0(SpatialTreeState tree,SpatialEntityView entity,EntitySpatialTreeContext context,Invocation invocation){
   UpdateCaptured1333E0(Field<object>.Of(tree==null?null:tree.Identity),entity,context,invocation);
  }
  // Level12F682 captures an opaque pointer. The native null-node/contained
  // paths do not dereference tree this, so preserve Unknown through the call.
  public static void UpdateCaptured1333E0(Field<object> capturedTree,SpatialEntityView entity,EntitySpatialTreeContext context,Invocation invocation){
   var i=Need(invocation,0x1333f0);var e=E(entity,context,0x1333f0,i);var pointer=S(e.NodeD0,0x1333f0,i);var node=R(pointer,0x1333f7,i);if(node==null){i.At=0x133495;return;}N(node,context,0x133405,i);
   var eb=e.Box44;var nb=node.Box30;var ex=S(eb.Center00,0x133400,i);var nx=S(nb.Center00,0x133405,i);var ehx=S(eb.Extent0C,0x13340d,i);var nhx=S(nb.Extent0C,0x133415,i);
   float emin=Sub(R(ex,0x13341a,i),R(ehx,0x13341a,i));float nmin=Sub(R(nx,0x13341e,i),R(nhx,0x13341e,i));i.At=0x133422;bool outside=Carry(emin,nmin);
   if(!outside){float emax=Add(R(ehx,0x133427,i),R(ex,0x133427,i));float nmax=Add(R(nhx,0x13342b,i),R(nx,0x13342b,i));i.At=0x13342f;outside=Above(emax,nmax);}
   if(!outside){
    var ez=S(eb.Center08,0x133434,i);var nz=S(nb.Center08,0x133439,i);var ehz=S(eb.Extent14,0x133441,i);var nhz=S(nb.Extent14,0x133449,i);
    emin=Sub(R(ez,0x13344e,i),R(ehz,0x13344e,i));nmin=Sub(R(nz,0x133452,i),R(nhz,0x133452,i));i.At=0x133456;outside=Carry(emin,nmin);
    if(!outside){float emax=Add(R(ehz,0x13345b,i),R(ez,0x13345b,i));float nmax=Add(R(nhz,0x13345f,i),R(nz,0x13345f,i));i.At=0x133463;outside=Above(emax,nmax);}
   }
   if(!outside){i.At=0x133472;Refine134500(node,e,context,i);return;}
   i.At=0x133477;Remove133BC0(node,e,context,i);
   i.At=0x13347c;var tree=context.Tree(Need(capturedTree,i.At).Read(i.At),i.At);var root=tree.Root10.Snapshot();i.At=0x133483;InsertCore(root,e,context,i,0);
   W(tree.ChangeCount08,unchecked(R(tree.ChangeCount08,0x133488,i)+1u),0x133488,i);i.At=0x133495;
  }

  public static SpatialNodeState Remove133BC0(SpatialNodeState node,SpatialEntityView entity,EntitySpatialTreeContext context,Invocation invocation){
   var i=Need(invocation,0x133bc3);i.At=0x133bc3;
   if(entity!=null){
    var e=E(entity,context,0x133bc8,i);var previous=S(e.Previous120,0x133bc8,i);var next=S(e.Next128,0x133bcf,i);W(e.NodeD0,(SpatialNodeState)null,0x133bd6,i);
    var p=R(previous,0x133bdd,i);if(p!=null)C(E(p,context,0x133be2,i).Next128,next,0x133be2,i);else C(N(node,context,0x133beb,i).Head48,next,0x133beb,i);
    var nextAgain=S(e.Next128,0x133bef,i);var n=R(nextAgain,0x133bf6,i);
    if(n!=null){var priorAgain=S(e.Previous120,0x133bfb,i);C(E(n,context,0x133c02,i).Previous120,priorAgain,0x133c02,i);}
    W(e.Previous120,(SpatialEntityView)null,0x133c09,i);W(e.Next128,(SpatialEntityView)null,0x133c10,i);
   }
   node=N(node,context,0x133c17,i);if(R(node.Head48,0x133c17,i)!=null){i.At=0x133c4b;return node;}
   for(int index=0;index<4;index++)if(R(node.Child(index),0x133c21,i)!=null){i.At=0x133c4b;return node;}
   var parent=S(node.Parent08,0x133c34,i);var parentValue=R(parent,0x133c38,i);if(parentValue!=null){i.At=0x133c43;return Prune11BBC0(parentValue,node,context,i);}
   i.At=0x133c4b;return node;
  }

  public static void Insert133A10(SpatialNodeState node,SpatialEntityView entity,EntitySpatialTreeContext context,Invocation invocation){InsertCore(Field<SpatialNodeState>.Of(node),entity,context,invocation,0);}
  static void InsertCore(Field<SpatialNodeState> nodePointer,SpatialEntityView entity,EntitySpatialTreeContext context,Invocation invocation,int transitions){
   var i=Need(invocation,0x133a1a);var e=E(entity,context,0x133a1a,i);var eb=e.Box44;
   var eHx=S(eb.Extent0C,0x133a1a,i);var eZ=S(eb.Center08,0x133a1f,i);float minZ=Sub(R(eZ,0x133a24,i),R(eb.Extent14,0x133a24,i));
   var eX=S(eb.Center00,0x133a29,i);float minX=Sub(R(eX,0x133a2e,i),R(eb.Extent0C,0x133a2e,i));float maxX=Add(R(eHx,0x133a33,i),R(eb.Center00,0x133a33,i));
   var eY=S(eb.Center04,0x133a38,i);float minY=Sub(R(eY,0x133a3d,i),R(eb.Extent10,0x133a3d,i));var eZ2=S(eb.Center08,0x133a42,i);float maxZ=Add(R(eZ2,0x133a47,i),R(eb.Extent14,0x133a47,i));var eY2=S(eb.Center04,0x133a4c,i);float maxY=Add(R(eY2,0x133a51,i),R(eb.Extent10,0x133a51,i));
   var node=N(R(nodePointer,0x133a5a,i),context,0x133a5a,i);var nb=node.Box30;var nz=S(nb.Center08,0x133a5a,i);float unionMaxZ=Add(R(nz,0x133a5f,i),R(nb.Extent14,0x133a5f,i));var nx=S(nb.Center00,0x133a68,i);float unionMinX=Sub(R(nx,0x133a6d,i),R(nb.Extent0C,0x133a6d,i));
   var ny=S(nb.Center04,0x133a77,i);i.At=0x133a7d;unionMaxZ=Max(unionMaxZ,maxZ);float unionMinY=Sub(R(ny,0x133a81,i),R(nb.Extent10,0x133a81,i));i.At=0x133a96;unionMinX=Min(unionMinX,minX);
   var nz2=S(nb.Center08,0x133a9a,i);var nx2=S(nb.Center00,0x133aa3,i);i.At=0x133aa9;unionMinY=Min(unionMinY,minY);float unionMaxX=Add(R(nx2,0x133aae,i),R(nb.Extent0C,0x133aae,i));var ny2=S(nb.Center04,0x133ab4,i);float unionMaxY=Add(R(ny2,0x133aba,i),R(nb.Extent10,0x133aba,i));float unionMinZ=Sub(R(nz2,0x133ac0,i),R(nb.Extent14,0x133ac0,i));i.At=0x133ac6;unionMaxX=Max(unionMaxX,maxX);
   var half=S(context.HalfBits34BADC,0x133acb,i);i.At=0x133ad3;unionMaxY=Max(unionMaxY,maxY);i.At=0x133ad8;unionMinZ=Min(unionMinZ,minZ);
   float hx=Sub(unionMaxX,unionMinX),cx=Add(unionMaxX,unionMinX),hy=Sub(unionMaxY,unionMinY),cz=Add(unionMaxZ,unionMinZ),cy=Add(unionMaxY,unionMinY),hz=Sub(unionMaxZ,unionMinZ);
   hx=Mul(hx,Float(R(half,0x133b02,i)));cx=Mul(cx,Float(R(half,0x133b07,i)));cz=Mul(cz,Float(R(half,0x133b0b,i)));cy=Mul(cy,Float(R(half,0x133b0f,i)));
   W(nb.Center00,cx,0x133b21,i);W(nb.Center04,cy,0x133b21,i);W(nb.Center08,cz,0x133b2a,i);hy=Mul(hy,Float(R(half,0x133b2d,i)));hz=Mul(hz,Float(R(half,0x133b32,i)));W(nb.Extent0C,hx,0x133b3a,i);W(nb.Extent10,hy,0x133b3a,i);W(nb.Extent14,hz,0x133b49,i);
   var flags=S(e.FlagsCB,0x133b4c,i);W(node.Flags54,(byte)(R(node.Flags54,0x133b53,i)|R(flags,0x133b53,i)),0x133b53,i);W(e.NodeD0,node,0x133b56,i);
   var head=S(node.Head48,0x133b5d,i);var headValue=R(head,0x133b61,i);if(headValue!=null)W(E(headValue,context,0x133b66,i).Previous120,e,0x133b66,i);
   var headAgain=S(node.Head48,0x133b6d,i);C(e.Next128,headAgain,0x133b71,i);W(e.Previous120,(SpatialEntityView)null,0x133b78,i);W(node.Head48,e,0x133b83,i);
   i.At=0x133bb3;RefineCore(node,e,context,i,transitions);
  }

  public static void Refine134500(SpatialNodeState node,SpatialEntityView entity,EntitySpatialTreeContext context,Invocation invocation){RefineCore(node,entity,context,invocation,0);}
  static void RefineCore(SpatialNodeState node,SpatialEntityView entity,EntitySpatialTreeContext context,Invocation invocation,int transitions){
   var i=Need(invocation,0x134512);node=N(node,context,0x134512,i);
   if(transitions>=context.OwnTransitionLimit)throw new MissingContract(i.At,"Own finite refinement domain exhausted; earlier list/node writes retained");
   var depth=S(node.Depth50,0x134512,i);int d=R(depth,0x13452b,i);var nb=node.Box30;
   if(d<8){
    var e=E(entity,context,0x13453c,i);var eb=e.Box44;
    var nx=S(nb.Center00,0x134534,i);bool right=Carry(R(nx,0x13453c,i),R(eb.Center00,0x13453c,i));var nz=S(nb.Center08,0x134543,i);var nxh=S(nb.Extent0C,0x134548,i);var nzh=S(nb.Extent14,0x13454d,i);bool back=Carry(R(nz,0x134555,i),R(eb.Center08,0x134555,i));
    float dx=Mul(R(nxh,0x13455c,i),Constant(context.SplitBits34BAD8,0x13455c,i));float dz=Mul(R(nzh,0x134564,i),Constant(context.SplitBits34BAD8,0x134564,i));var xor=S(context.XorBits34C450,0x13456f,i);int index=(right?1:0)+(back?2:0);
    if(!right)dx=Float(Bits(dx)^R(xor,0x13457e,i));float childX=Add(R(nx,0x134581,i),dx);
    if(!back)dz=Float(Bits(dz)^R(xor,0x134598,i));var half=S(context.HalfBits34BADC,0x13459b,i);float childZ=Add(R(nz,0x1345a4,i),dz);
    var nhx=S(nb.Extent0C,0x1345a8,i);var ex=S(eb.Center00,0x1345b0,i);var ehx=S(eb.Extent0C,0x1345b5,i);var nhz=S(nb.Extent14,0x1345bd,i);float emin=Sub(R(ex,0x1345c2,i),R(ehx,0x1345c2,i));float childHx=Mul(R(nhx,0x1345c6,i),Float(R(half,0x1345c6,i)));float childHz=Mul(R(nhz,0x1345d1,i),Float(R(half,0x1345d1,i)));float nmin=Sub(childX,childHx);i.At=0x1345da;bool outside=Carry(emin,nmin);
    if(!outside){float emax=Add(R(ehx,0x1345e3,i),R(ex,0x1345e3,i));float nmax=Add(childHx,childX);i.At=0x1345eb;outside=Above(emax,nmax);}
    if(!outside){var ez=S(eb.Center08,0x1345f4,i);var ehz=S(eb.Extent14,0x1345fc,i);nmin=Sub(childZ,childHz);emin=Sub(R(ez,0x134608,i),R(ehz,0x134608,i));i.At=0x13460c;outside=Carry(emin,nmin);if(!outside){float emax=Add(R(ehz,0x134615,i),R(ez,0x134615,i));float nmax=Add(childHz,childZ);i.At=0x13461d;outside=Above(emax,nmax);}}
    if(!outside){
     if(R(node.Child(index),0x134626,i)==null){
      var zhalf=S(nb.Extent14,0x134631,i);int childDepth=unchecked(d+1);var xhalf=S(nb.Extent0C,0x134638,i);var yhalf=S(nb.Extent10,0x134642,i);float hz=Mul(R(zhalf,0x134647,i),Float(R(half,0x134647,i))),hx=Mul(R(xhalf,0x13464c,i),Float(R(half,0x13464c,i))),hy=Mul(R(yhalf,0x134657,i),Float(R(half,0x134657,i)));
      i.At=0x13465c;var child=context.AllocateChild(i.At);W(child.Box30.Center00,childX,0x13467d,i);W(child.Box30.Center04,0f,0x13467d,i);W(child.VirtualRva00,0x340e60u,0x134685,i);W(child.Box30.Center08,childZ,0x13468c,i);W(child.Parent08,node,0x13468f,i);W(child.Head48,(SpatialEntityView)null,0x134693,i);W(child.Depth50,childDepth,0x134697,i);W(child.Flags54,(byte)0,0x13469a,i);W(child.Box30.Extent0C,hx,0x1346a1,i);W(child.Box30.Extent10,hy,0x1346a1,i);W(child.Box30.Extent14,hz,0x1346a6,i);
      for(int n=0;n<4;n++)W(child.Child(n),(SpatialNodeState)null,(uint)(0x1346a9+n*4),i);W(node.Child(index),child,0x1346b9,i);
     }
     i.At=0x1346c4;Remove133BC0(node,e,context,i);var target=S(node.Child(index),0x1346c9,i);i.At=0x1346d1;InsertCore(target,e,context,i,transitions+1);i.At=0x134715;return;
    }
   }
   var fallback=E(entity,context,0x1346d8,i).Box44;var y=S(fallback.Center04,0x1346d8,i);float max=Add(R(y,0x1346e3,i),R(fallback.Extent10,0x1346e3,i));float min=Sub(R(y,0x1346e8,i),R(fallback.Extent10,0x1346e8,i));
   i.At=0x1346ed;ExpandY11BE80(node,min,max,context,i);i.At=0x134715;
  }
  public static void ExpandY11BE80(SpatialNodeState node,float min,float max,EntitySpatialTreeContext context,Invocation invocation){
   var i=Need(invocation,0x11be80);var half=S(Need(context,0x11be80).HalfBits34BADC,0x11be80,i);int hops=0;
   while(true){
    node=N(node,context,0x11be90,i);if(hops++>=context.OwnTransitionLimit)throw new MissingContract(i.At,"Own finite parent-propagation domain exhausted");
    var y=S(node.Box30.Center04,0x11be90,i);var ey=S(node.Box30.Extent10,0x11be97,i);float low=Sub(R(y,0x11be9f,i),R(ey,0x11be9f,i));float high=Add(R(ey,0x11bea3,i),R(y,0x11bea3,i));
    i.At=0x11bea7;bool highChanged=Above(max,high);if(highChanged)high=max;
    i.At=0x11beb1;if(Carry(min,low))low=min;else if(!highChanged){i.At=0x11bee5;return;}
    float extent=Sub(high,low),center=Add(high,low);extent=Mul(extent,Float(R(half,0x11beca,i)));center=Mul(center,Float(R(half,0x11bece,i)));
    W(node.Box30.Extent10,extent,0x11bed2,i);W(node.Box30.Center04,center,0x11bed7,i);
    var parent=S(node.Parent08,0x11bedc,i);node=R(parent,0x11bee0,i);if(node==null){i.At=0x11bee5;return;}
   }
  }
  public static SpatialNodeState Prune11BBC0(SpatialNodeState parent,SpatialNodeState target,EntitySpatialTreeContext context,Invocation invocation){
   var i=Need(invocation,0x11bbde);int hops=0;
   while(true){
    // 11BBDE is LEA only; the first parent dereference is MOV [RBX]11BBF0.
    parent=N(parent,context,0x11bbf0,i);if(hops++>=context.OwnTransitionLimit)throw new MissingContract(i.At,"Own finite prune domain exhausted");
    for(int index=0;index<4;index++){
     var captured=S(parent.Child(index),0x11bbf0,i);var child=R(captured,0x11bbf3,i);
     if(Object.ReferenceEquals(child,target)){
      i.At=0x11bbf8;if(child!=null){var table=S(N(child,context,0x11bbfd,i).VirtualRva00,0x11bbfd,i);i.At=0x11bc05;DestroyNodeSlot0(child,table,1,context,i,new HashSet<SpatialNodeState>());}
      W(parent.Child(index),(SpatialNodeState)null,0x11bc07,i);
     }
    }
    if(R(parent.Head48,0x11bc14,i)!=null){i.At=0x11bc58;return parent;}
    var higher=S(parent.Parent08,0x11bc1a,i);var higherValue=R(higher,0x11bc1e,i);if(higherValue==null){i.At=0x11bc58;return parent;}
    for(int index=0;index<4;index++)if(R(parent.Child(index),0x11bc26,i)!=null){i.At=0x11bc58;return parent;}
    target=parent;parent=higherValue;
   }
  }
  static SpatialNodeState DestroyNodeSlot0(SpatialNodeState child,Field<uint> table,uint flags,EntitySpatialTreeContext context,Invocation i,HashSet<SpatialNodeState> active){
   if(table.Read(i.At)!=0x340e60u)throw new MissingContract(i.At,"Unobserved dynamic node table; fixed disk340E60 slot0 binding not selected");
   if(active.Count>=context.OwnTransitionLimit||active.Contains(child))throw new MissingContract(i.At,"Own finite acyclic destructor domain exceeded; earlier clears/frees retained");
   return DestroyCore11BB30(child,flags,context,i,active);
  }
  // Engine11BB30 plus exact340E60 first-QWORD binding in
  // r1-spatial-ground-callback-slots. This direct function overwrites self's
  // table before examining children; only child virtual dispatch checks the
  // captured table. The explicit managed free adapter invalidates the own
  // view/removes its active allocation, not a native heap or allocator model.
  public static SpatialNodeState DestroyNode11BB30(SpatialNodeState node,uint flags,EntitySpatialTreeContext context,Invocation invocation){
   return DestroyCore11BB30(node,flags,context,Need(invocation,0x11bb53),new HashSet<SpatialNodeState>());
  }
  static SpatialNodeState DestroyCore11BB30(SpatialNodeState node,uint flags,EntitySpatialTreeContext context,Invocation i,HashSet<SpatialNodeState> active){
   node=N(node,context,0x11bb53,i);if(!active.Add(node))throw new MissingContract(i.At,"Own destructor recursion cycle");
   try{
    W(node.VirtualRva00,0x340e60u,0x11bb53,i);
    for(int index=0;index<4;index++){
     var pointer=S(node.Child(index),0x11bb65,i);var child=R(pointer,0x11bb68,i);
     if(child!=null){var table=S(N(child,context,0x11bb6d,i).VirtualRva00,0x11bb6d,i);i.At=0x11bb75;DestroyNodeSlot0(child,table,1,context,i,active);W(node.Child(index),(SpatialNodeState)null,0x11bb77,i);}
    }
    i.At=0x11bb84;if((flags&1u)!=0){i.At=0x11bb90;context.FreeOwnNode(node,i.At);}i.At=0x11bbb2;return node;
   }finally{active.Remove(node);}
  }
 }
}
