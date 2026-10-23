using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Ground's intrusive entry is not the Entity/Actor entry. It borrows the
 // same Block10 box and candidate state, with owner08/next28/previous30.
 public sealed class GroundSpatialEntry {
  public readonly GroundBlockMaterialState MaterialState;
  public readonly Field<GroundSpatialNode> Owner08=new Field<GroundSpatialNode>();
  public readonly Field<GroundSpatialEntry> Next28=new Field<GroundSpatialEntry>(),Previous30=new Field<GroundSpatialEntry>();
  public readonly Field<uint> VirtualRva00=new Field<uint>();
  public readonly Field<byte> Flags40=new Field<byte>();
  internal bool ColdLinksSelected;
  internal GroundSpatialEntry(GroundBlockMaterialState state){MaterialState=state;}
  public GroundBlockState Block{get{return MaterialState.Block;}}
  public WorldBounds24 Box10{get{return Block.Box10;}}
 }
 public sealed class GroundSpatialNode {
  internal readonly GroundSpatialContext Context;
  public readonly object Identity;
  public readonly Field<uint> VirtualRva00=new Field<uint>();
  public readonly Field<GroundSpatialNode> Parent08=new Field<GroundSpatialNode>();
  readonly Field<GroundSpatialNode>[] children={new Field<GroundSpatialNode>(),new Field<GroundSpatialNode>(),new Field<GroundSpatialNode>(),new Field<GroundSpatialNode>()};
  public readonly WorldBounds24 Box30=new WorldBounds24();
  public readonly Field<GroundSpatialEntry> Head48=new Field<GroundSpatialEntry>();
  public readonly Field<int> Depth50=new Field<int>();
  public readonly Field<byte> Flags54=new Field<byte>();
  bool released;
  internal GroundSpatialNode(GroundSpatialContext context,object identity){Context=context;Identity=identity;}
  public Field<GroundSpatialNode> Child(int n){if(n<0||n>3)throw new ArgumentOutOfRangeException("n");return children[n];}
  public void RequireLive(uint at){if(released)throw new MissingContract(at,"OWN released Ground node");}
  internal void MarkReleased(){released=true;}
 }
 public sealed class GroundSpatialTree {
  public readonly object Identity=new object();
  public readonly Field<uint> VirtualRva00=new Field<uint>(),ChangeCount08=new Field<uint>();
  public readonly Field<GroundSpatialNode> Root10=new Field<GroundSpatialNode>();
  readonly Field<object>[] scratch={new Field<object>(),new Field<object>(),new Field<object>(),new Field<object>(),new Field<object>(),new Field<object>()};
  public Field<object> Scratch18To40(int n){if(n<0||n>=6)throw new ArgumentOutOfRangeException("n");return scratch[n];}
 }
 public sealed class GroundSpatialContext {
  readonly Dictionary<GroundBlockState,GroundSpatialEntry> entries=new Dictionary<GroundBlockState,GroundSpatialEntry>();
  readonly Dictionary<object,GroundSpatialNode> nodes=new Dictionary<object,GroundSpatialNode>();
  readonly ConditionalWeakTable<object,object> released=new ConditionalWeakTable<object,object>();
  public readonly Field<uint> RootScale34BB5C=new Field<uint>(),Split34BAD8=new Field<uint>(),Half34BADC=new Field<uint>(),Xor34C450=new Field<uint>();
  public readonly GroundTerrainIntersectionBinding Terrain;
  public readonly GroundBlockIntersectionOperands IntersectionOperands;
  public readonly int OwnNodeLimit,OwnTransitionLimit=64,OwnQueryVisitLimit=65536;
  public int ActiveNodeCount{get{return nodes.Count;}}
  public GroundSpatialContext(GroundTerrainIntersectionBinding terrain,GroundBlockIntersectionOperands operands):this(terrain,operands,4096){}
  public GroundSpatialContext(GroundTerrainIntersectionBinding terrain,GroundBlockIntersectionOperands operands,int ownNodeLimit){
   if(ownNodeLimit<1)throw new ArgumentOutOfRangeException("ownNodeLimit");Terrain=terrain;IntersectionOperands=operands;OwnNodeLimit=ownNodeLimit;
  }
  public void SelectObservedDiskOperands(){RootScale34BB5C.Write(0x3f8b851fu);Split34BAD8.Write(0x3eeadcc5u);Half34BADC.Write(0x3f000000u);Xor34C450.Write(0x80000000u);}
  public GroundSpatialEntry BindBlock(GroundBlockMaterialState state){
   if(state==null)throw new ArgumentNullException("state");GroundSpatialEntry prior;
   if(entries.TryGetValue(state.Block,out prior)){if(!Object.ReferenceEquals(prior.MaterialState,state))throw new InvalidOperationException("Ground Block already has a different candidate owner");return prior;}
   var entry=new GroundSpatialEntry(state);entries.Add(state.Block,entry);return entry;
  }
  // Explicit OWN cold-link adaptation from1D0E20 writes. It does not execute
  // that native constructor or invent gEngine+640, materials or grass state.
  public GroundSpatialEntry BindOwnColdBlock(GroundBlockMaterialState state){
   var e=BindBlock(state);if(e.ColdLinksSelected)throw new InvalidOperationException("Cold links already selected; cannot erase live membership");
   e.Owner08.Write(null);e.Next28.Write(null);e.Previous30.Write(null);e.Flags40.Write(0);e.VirtualRva00.Write(0x3438f8u);e.ColdLinksSelected=true;return e;
  }
  public GroundSpatialNode BindNode(object identity){
   if(identity==null)throw new ArgumentNullException("identity");GroundSpatialNode prior;if(nodes.TryGetValue(identity,out prior))return prior;
   object tombstone;if(released.TryGetValue(identity,out tombstone))throw new InvalidOperationException("Released Ground node identity cannot be rebound");
   if(nodes.Count>=OwnNodeLimit)throw new MissingContract(0x1d03ec,"OWN finite node allocation limit");
   var node=new GroundSpatialNode(this,identity);nodes.Add(identity,node);return node;
  }
  public GroundSpatialNode[] SnapshotNodes(){var result=new GroundSpatialNode[nodes.Count];nodes.Values.CopyTo(result,0);return result;}
  internal GroundSpatialNode Node(GroundSpatialNode node,uint at){GroundSpatialNode prior;if(node!=null)node.RequireLive(at);if(node==null||!Object.ReferenceEquals(node.Context,this)||!nodes.TryGetValue(node.Identity,out prior)||!Object.ReferenceEquals(prior,node))throw new MissingContract(at,"Ground node is outside this OWN composition");return node;}
  internal GroundSpatialEntry Entry(GroundSpatialEntry entry,uint at){GroundSpatialEntry prior;if(entry==null||!entries.TryGetValue(entry.Block,out prior)||!Object.ReferenceEquals(prior,entry))throw new MissingContract(at,"Ground entry has no authoritative shared binding");return entry;}
  internal GroundSpatialNode Allocate(uint at){if(nodes.Count>=OwnNodeLimit)throw new MissingContract(at,"OWN finite node limit before allocation");return BindNode(new object());}
  internal void Free(GroundSpatialNode node,uint at){Node(node,at);released.Add(node.Identity,new object());node.MarkReleased();nodes.Remove(node.Identity);}
 }
 // Actual Ground node bodies11A600/1CFDF0/1D0290/1CFF90/1D0100 and shared
 // node bodies11BE80/11BBC0/11BB30. OWN allocation and finite binary32 arithmetic
 // are explicit adapters; no native ABI/allocator failure/threads, raw byte
 // aliases, runtime table changes or complete native Load are claimed.
 public static class GroundSpatialTreeContracts {
  static T Need<T>(T v,uint at)where T:class{if(v==null)throw new MissingContract(at,"Null OWN Ground spatial view");return v;}
  static float F(float v){return BitConverter.ToSingle(BitConverter.GetBytes(v),0);}
  static float Float(uint v){return BitConverter.ToSingle(BitConverter.GetBytes(v),0);}
  static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}
  static float Finite(float v,uint at){if(float.IsNaN(v)||float.IsInfinity(v))throw new MissingContract(at,"OWN finite Ground tree arithmetic domain");return v;}
  static float Add(float x,float y){return F(x+y);}static float Sub(float x,float y){return F(x-y);}static float Mul(float x,float y){return F(x*y);}
  static bool Carry(float x,float y){return float.IsNaN(x)||float.IsNaN(y)||x<y;}
  static bool Above(float x,float y){return x>y;}
  static float Min(float x,float y){return x<y?x:y;}static float Max(float x,float y){return x>y?x:y;}
  static Field<T> S<T>(Field<T> v,uint at,Invocation i){i.At=at;return v.Snapshot();}
  static T R<T>(Field<T> v,uint at,Invocation i){i.At=at;T result=v.Read(at);if(typeof(T)==typeof(float))Finite((float)(object)result,at);return result;}
  static void W<T>(Field<T> v,T x,uint at,Invocation i){i.At=at;v.Write(x);}
  static void C<T>(Field<T> v,Field<T> x,uint at,Invocation i){i.At=at;v.CopyFrom(x);}
  static float Constant(Field<uint> v,uint at,Invocation i){return Finite(Float(R(v,at,i)),at);}
  static GroundSpatialNode N(GroundSpatialNode n,GroundSpatialContext c,uint at,Invocation i){i.At=at;return Need(c,at).Node(n,at);}
  static GroundSpatialEntry E(GroundSpatialEntry e,GroundSpatialContext c,uint at,Invocation i){i.At=at;return Need(c,at).Entry(e,at);}

  public static GroundSpatialTree Construct11A600(GroundSpatialTree tree,Field<float> centerX,Field<float> centerZ,Field<float> extentX,Field<float> extentZ,GroundSpatialContext context,Invocation invocation){
   var i=Need(invocation,0x11a62f);tree=Need(tree,0x11a62f);W(tree.VirtualRva00,0x340e70u,0x11a62f,i);
   for(int n=0;n<6;n++)W(tree.Scratch18To40(n),(object)null,(uint)(0x11a634+4*n),i);W(tree.ChangeCount08,0u,0x11a64c,i);
   var ez=S(Need(extentZ,0x11a64f),0x11a64f,i);var ex=S(Need(extentX,0x11a655),0x11a655,i);
   float hz=Finite(Mul(R(ez,0x11a65a,i),Constant(Need(context,0x11a65a).RootScale34BB5C,0x11a65a,i)),0x11a65a);
   float hx=Finite(Mul(R(ex,0x11a662,i),Constant(context.RootScale34BB5C,0x11a662,i)),0x11a662);
   var cz=S(Need(centerZ,0x11a674),0x11a674,i);var cx=S(Need(centerX,0x11a679),0x11a679,i);
   i.At=0x11a686;var root=context.Allocate(i.At);W(root.VirtualRva00,0x340e60u,0x11a69d,i);
   C(root.Box30.Center00,cx,0x11a6a6,i);W(root.Box30.Center04,0f,0x11a6a6,i);C(root.Box30.Center08,cz,0x11a6af,i);
   W(root.Box30.Extent0C,hx,0x11a6b9,i);W(root.Box30.Extent10,0f,0x11a6b9,i);W(root.Box30.Extent14,hz,0x11a6c2,i);
   W(root.Parent08,(GroundSpatialNode)null,0x11a6c5,i);W(root.Head48,(GroundSpatialEntry)null,0x11a6c9,i);W(root.Depth50,0,0x11a6cd,i);W(root.Flags54,(byte)0,0x11a6d0,i);
   for(int n=0;n<4;n++)W(root.Child(n),(GroundSpatialNode)null,(uint)(0x11a6d4+4*n),i);
   W(tree.Root10,root,0x11a6e4,i);i.At=0x11a708;return tree;
  }
  // Caller supplies the already-produced, same-identity Block/material views.
  // This is the1CE910 publication portion plus an explicit OWN cold-link
  // adapter, not its allocator, gEngine access, graphics or slot48 lifecycle.
  public static void PublishOwnColdBlocks(GroundSpatialTree tree,OwnGroundData data,GroundBlockMaterialState[] blocks,GroundSpatialContext context,Invocation invocation){
   var i=Need(invocation,0x1ce910);data=Need(data,i.At);blocks=Need(blocks,i.At);context=Need(context,i.At);
   uint width=R(data.Dimension60,0x1ce930,i),depth=R(data.Dimension64,0x1ce947,i);
   if(width<2||depth<2||width>OwnGroundColdLoader.MaximumCells||depth>OwnGroundColdLoader.MaximumCells)throw new MissingContract(i.At,"OWN bounded Ground dimensions");
   uint columns=(width+7)/9,rows=(depth+7)/9;if((ulong)columns*rows!=(ulong)blocks.Length)throw new MissingContract(i.At,"OWN complete produced Block array required");
   var entries=new GroundSpatialEntry[blocks.Length];for(int n=0;n<blocks.Length;n++){
    var state=Need(blocks[n],i.At);if(!Object.ReferenceEquals(state.Block.TerrainA8.Read(i.At),data))throw new MissingContract(i.At,"Block borrows a different Terrain");entries[n]=context.BindOwnColdBlock(state);
   }
   uint remaining=Math.Max(columns,rows),power=0,bits=0;
   while(remaining!=0){bits+=remaining&1u;power=unchecked(1u+power*2u);remaining>>=1;}
   if(Math.Max(columns,rows)!=0){power=unchecked(power+1u);if(bits==1)power>>=1;}
   ulong side=(ulong)power*9u;if(side>uint.MaxValue)throw new MissingContract(0x1ceaad,"OWN root span overflow");
   float half=Finite(Mul(F((float)(long)side),Constant(context.Half34BADC,0x1ceaca,i)),i.At);
   Construct11A600(tree,Field<float>.Of(half),Field<float>.Of(half),Field<float>.Of(half),Field<float>.Of(half),context,i);
   for(int n=0;n<entries.Length;n++){var root=S(tree.Root10,0x1ceb31,i);InsertCore(root,entries[n],context,i,0);W(tree.ChangeCount08,unchecked(R(tree.ChangeCount08,0x1ceb3a,i)+1u),0x1ceb3a,i);}
   i.At=0x1ceb5d;
  }

  public static void Insert1CFDF0(GroundSpatialNode node,GroundSpatialEntry entry,GroundSpatialContext context,Invocation invocation){InsertCore(Field<GroundSpatialNode>.Of(node),entry,context,invocation,0);}
  static void InsertCore(Field<GroundSpatialNode> pointer,GroundSpatialEntry entry,GroundSpatialContext c,Invocation invocation,int transitions){
   var i=Need(invocation,0x1cfdfa);var e=E(entry,c,0x1cfdfa,i);var eb=e.Box10;
   var ehx=S(eb.Extent0C,0x1cfdfa,i);var ez=S(eb.Center08,0x1cfdff,i);float minZ=Sub(R(ez,0x1cfe04,i),R(eb.Extent14,0x1cfe04,i));
   var ex=S(eb.Center00,0x1cfe09,i);float minX=Sub(R(ex,0x1cfe0e,i),R(eb.Extent0C,0x1cfe0e,i));float maxX=Add(R(ehx,0x1cfe13,i),R(eb.Center00,0x1cfe13,i));
   var ey=S(eb.Center04,0x1cfe18,i);float minY=Sub(R(ey,0x1cfe1d,i),R(eb.Extent10,0x1cfe1d,i));var ez2=S(eb.Center08,0x1cfe22,i);float maxZ=Add(R(ez2,0x1cfe27,i),R(eb.Extent14,0x1cfe27,i));var ey2=S(eb.Center04,0x1cfe2c,i);float maxY=Add(R(ey2,0x1cfe31,i),R(eb.Extent10,0x1cfe31,i));
   var node=N(R(pointer,0x1cfe3a,i),c,0x1cfe3a,i);var nb=node.Box30;var nz=S(nb.Center08,0x1cfe3a,i);float highZ=Add(R(nz,0x1cfe3f,i),R(nb.Extent14,0x1cfe3f,i));var nx=S(nb.Center00,0x1cfe48,i);float lowX=Sub(R(nx,0x1cfe4d,i),R(nb.Extent0C,0x1cfe4d,i));
   var ny=S(nb.Center04,0x1cfe57,i);highZ=Max(highZ,maxZ);float lowY=Sub(R(ny,0x1cfe61,i),R(nb.Extent10,0x1cfe61,i));lowX=Min(lowX,minX);
   var nz2=S(nb.Center08,0x1cfe7a,i);var nx2=S(nb.Center00,0x1cfe83,i);lowY=Min(lowY,minY);float highX=Add(R(nx2,0x1cfe8e,i),R(nb.Extent0C,0x1cfe8e,i));var ny2=S(nb.Center04,0x1cfe94,i);float highY=Add(R(ny2,0x1cfe9a,i),R(nb.Extent10,0x1cfe9a,i));float lowZ=Sub(R(nz2,0x1cfea0,i),R(nb.Extent14,0x1cfea0,i));highX=Max(highX,maxX);
   var half=S(c.Half34BADC,0x1cfeab,i);highY=Max(highY,maxY);lowZ=Min(lowZ,minZ);
   float hx=Sub(highX,lowX),cx=Add(highX,lowX),hy=Sub(highY,lowY),cz=Add(highZ,lowZ),cy=Add(highY,lowY),hz=Sub(highZ,lowZ);
   hx=Mul(hx,Float(R(half,0x1cfee2,i)));cx=Mul(cx,Float(R(half,0x1cfee7,i)));cz=Mul(cz,Float(R(half,0x1cfeeb,i)));cy=Mul(cy,Float(R(half,0x1cfeef,i)));
   W(nb.Center00,Finite(cx,0x1cff01),0x1cff01,i);W(nb.Center04,Finite(cy,0x1cff01),0x1cff01,i);W(nb.Center08,Finite(cz,0x1cff0a),0x1cff0a,i);
   hy=Mul(hy,Float(R(half,0x1cff0d,i)));hz=Mul(hz,Float(R(half,0x1cff12,i)));W(nb.Extent0C,Finite(hx,0x1cff1a),0x1cff1a,i);W(nb.Extent10,Finite(hy,0x1cff1a),0x1cff1a,i);W(nb.Extent14,Finite(hz,0x1cff29),0x1cff29,i);
   var flags=S(e.Flags40,0x1cff2c,i);W(node.Flags54,(byte)(R(node.Flags54,0x1cff30,i)|R(flags,0x1cff30,i)),0x1cff30,i);W(e.Owner08,node,0x1cff33,i);
   var head=S(node.Head48,0x1cff37,i);var old=R(head,0x1cff3b,i);if(old!=null)W(E(old,c,0x1cff40,i).Previous30,e,0x1cff40,i);
   C(e.Next28,S(node.Head48,0x1cff44,i),0x1cff48,i);W(e.Previous30,(GroundSpatialEntry)null,0x1cff4c,i);W(node.Head48,e,0x1cff54,i);
   i.At=0x1cff84;RefineCore(node,e,c,i,transitions);
  }

  public static void Refine1D0290(GroundSpatialNode node,GroundSpatialEntry entry,GroundSpatialContext context,Invocation invocation){RefineCore(node,entry,context,invocation,0);}
  static void RefineCore(GroundSpatialNode node,GroundSpatialEntry entry,GroundSpatialContext c,Invocation invocation,int transitions){
   var i=Need(invocation,0x1d02a2);node=N(node,c,0x1d02a2,i);if(transitions>=c.OwnTransitionLimit)throw new MissingContract(i.At,"OWN refinement transition bound; prior writes retained");
   var depth=S(node.Depth50,0x1d02a2,i);int d=R(depth,0x1d02bb,i);var nb=node.Box30;
   if(d<8){
    var e=E(entry,c,0x1d02cc,i);var eb=e.Box10;var nx=S(nb.Center00,0x1d02c4,i);bool right=Carry(R(nx,0x1d02cc,i),R(eb.Center00,0x1d02cc,i));var nz=S(nb.Center08,0x1d02d3,i);var nhx=S(nb.Extent0C,0x1d02d8,i);var nhz=S(nb.Extent14,0x1d02dd,i);bool back=Carry(R(nz,0x1d02e5,i),R(eb.Center08,0x1d02e5,i));
    float dx=Mul(R(nhx,0x1d02ec,i),Constant(c.Split34BAD8,0x1d02ec,i)),dz=Mul(R(nhz,0x1d02f4,i),Constant(c.Split34BAD8,0x1d02f4,i));var xor=S(c.Xor34C450,0x1d02ff,i);int index=(right?1:0)+(back?2:0);
    if(!right)dx=Float(Bits(dx)^R(xor,0x1d030e,i));float childX=Add(R(nx,0x1d0311,i),dx);if(!back)dz=Float(Bits(dz)^R(xor,0x1d0328,i));var half=S(c.Half34BADC,0x1d032b,i);float childZ=Add(R(nz,0x1d0334,i),dz);
    var nxh=S(nb.Extent0C,0x1d0338,i);var ex=S(eb.Center00,0x1d0340,i);var ehx=S(eb.Extent0C,0x1d0345,i);var nzh=S(nb.Extent14,0x1d034d,i);float emin=Sub(R(ex,0x1d0352,i),R(ehx,0x1d0352,i));float childHx=Mul(R(nxh,0x1d0356,i),Float(R(half,0x1d0356,i))),childHz=Mul(R(nzh,0x1d0361,i),Float(R(half,0x1d0361,i)));float nmin=Sub(childX,childHx);bool outside=Carry(emin,nmin);
    if(!outside){float emax=Add(R(ehx,0x1d0373,i),R(ex,0x1d0373,i));float nmax=Add(childHx,childX);outside=Above(emax,nmax);}
    if(!outside){var ez=S(eb.Center08,0x1d0384,i);var ehz=S(eb.Extent14,0x1d038c,i);nmin=Sub(childZ,childHz);emin=Sub(R(ez,0x1d0398,i),R(ehz,0x1d0398,i));outside=Carry(emin,nmin);if(!outside){float emax=Add(R(ehz,0x1d03a5,i),R(ez,0x1d03a5,i));float nmax=Add(childHz,childZ);outside=Above(emax,nmax);}}
    if(!outside){
     if(R(node.Child(index),0x1d03b6,i)==null){
      var zh=S(nb.Extent14,0x1d03c1,i);int childDepth=unchecked(d+1);var xh=S(nb.Extent0C,0x1d03c8,i);var yh=S(nb.Extent10,0x1d03d2,i);float hz=Mul(R(zh,0x1d03d7,i),Float(R(half,0x1d03d7,i))),hx=Mul(R(xh,0x1d03dc,i),Float(R(half,0x1d03dc,i))),hy=Mul(R(yh,0x1d03e7,i),Float(R(half,0x1d03e7,i)));
      i.At=0x1d03ec;var child=c.Allocate(i.At);W(child.Box30.Center00,Finite(childX,0x1d040d),0x1d040d,i);W(child.Box30.Center04,0f,0x1d040d,i);W(child.VirtualRva00,0x340e60u,0x1d0415,i);W(child.Box30.Center08,Finite(childZ,0x1d041c),0x1d041c,i);
      W(child.Parent08,node,0x1d041f,i);W(child.Head48,(GroundSpatialEntry)null,0x1d0423,i);W(child.Depth50,childDepth,0x1d0427,i);W(child.Flags54,(byte)0,0x1d042a,i);
      W(child.Box30.Extent0C,Finite(hx,0x1d0431),0x1d0431,i);W(child.Box30.Extent10,Finite(hy,0x1d0431),0x1d0431,i);W(child.Box30.Extent14,Finite(hz,0x1d0436),0x1d0436,i);
      for(int n=0;n<4;n++)W(child.Child(n),(GroundSpatialNode)null,(uint)(0x1d0439+4*n),i);W(node.Child(index),child,0x1d0449,i);
     }
     i.At=0x1d0454;Remove1CFF90(node,e,c,i);var target=S(node.Child(index),0x1d0459,i);i.At=0x1d0461;InsertCore(target,e,c,i,transitions+1);i.At=0x1d04a5;return;
    }
   }
   var box=E(entry,c,0x1d0468,i).Box10;var y=S(box.Center04,0x1d0468,i);float max=Add(R(y,0x1d0473,i),R(box.Extent10,0x1d0473,i)),min=Sub(R(y,0x1d0478,i),R(box.Extent10,0x1d0478,i));
   i.At=0x1d047d;ExpandY11BE80(node,min,max,c,i);i.At=0x1d04a5;
  }
  public static GroundSpatialNode Remove1CFF90(GroundSpatialNode node,GroundSpatialEntry entry,GroundSpatialContext context,Invocation invocation){
   var i=Need(invocation,0x1cff93);i.At=0x1cff93;
   if(entry!=null){var e=E(entry,context,0x1cff98,i);var previous=S(e.Previous30,0x1cff98,i);var next=S(e.Next28,0x1cff9c,i);W(e.Owner08,(GroundSpatialNode)null,0x1cffa0,i);
    var p=R(previous,0x1cffa4,i);if(p!=null)C(E(p,context,0x1cffa9,i).Next28,next,0x1cffa9,i);else C(N(node,context,0x1cffaf,i).Head48,next,0x1cffaf,i);
    var nextAgain=S(e.Next28,0x1cffb3,i);var n=R(nextAgain,0x1cffb7,i);if(n!=null){var priorAgain=S(e.Previous30,0x1cffbc,i);C(E(n,context,0x1cffc0,i).Previous30,priorAgain,0x1cffc0,i);}
    W(e.Previous30,(GroundSpatialEntry)null,0x1cffc4,i);W(e.Next28,(GroundSpatialEntry)null,0x1cffc8,i);
   }
   node=N(node,context,0x1cffcc,i);if(R(node.Head48,0x1cffcc,i)!=null){i.At=0x1d0000;return node;}
   for(int n=0;n<4;n++)if(R(node.Child(n),0x1cffd6,i)!=null){i.At=0x1d0000;return node;}
   var parent=S(node.Parent08,0x1cffe9,i);var value=R(parent,0x1cffed,i);if(value!=null){i.At=0x1cfff8;return Prune11BBC0(value,node,context,i);}i.At=0x1d0000;return node;
  }

  public static void ExpandY11BE80(GroundSpatialNode node,float min,float max,GroundSpatialContext context,Invocation invocation){
   var i=Need(invocation,0x11be80);var half=S(Need(context,0x11be80).Half34BADC,0x11be80,i);int hops=0;
   while(true){
    node=N(node,context,0x11be90,i);if(hops++>=context.OwnTransitionLimit)throw new MissingContract(i.At,"Own finite parent-propagation domain exhausted");
    var y=S(node.Box30.Center04,0x11be90,i);var ey=S(node.Box30.Extent10,0x11be97,i);float low=Sub(R(y,0x11be9f,i),R(ey,0x11be9f,i));float high=Add(R(ey,0x11bea3,i),R(y,0x11bea3,i));
    i.At=0x11bea7;bool highChanged=Above(Finite(max,i.At),high);if(highChanged)high=max;
    i.At=0x11beb1;if(Carry(Finite(min,i.At),low))low=min;else if(!highChanged){i.At=0x11bee5;return;}
    float extent=Sub(high,low),center=Add(high,low);extent=Mul(extent,Float(R(half,0x11beca,i)));center=Mul(center,Float(R(half,0x11bece,i)));
    W(node.Box30.Extent10,Finite(extent,0x11bed2),0x11bed2,i);W(node.Box30.Center04,Finite(center,0x11bed7),0x11bed7,i);
    var parent=S(node.Parent08,0x11bedc,i);node=R(parent,0x11bee0,i);if(node==null){i.At=0x11bee5;return;}
   }
  }
  public static GroundSpatialNode Prune11BBC0(GroundSpatialNode parent,GroundSpatialNode target,GroundSpatialContext context,Invocation invocation){
   var i=Need(invocation,0x11bbde);int hops=0;
   while(true){
    // 11BBDE is LEA only; the first parent dereference is MOV [RBX]11BBF0.
    parent=N(parent,context,0x11bbf0,i);if(hops++>=context.OwnTransitionLimit)throw new MissingContract(i.At,"Own finite prune domain exhausted");
    for(int index=0;index<4;index++){
     var captured=S(parent.Child(index),0x11bbf0,i);var child=R(captured,0x11bbf3,i);
     if(Object.ReferenceEquals(child,target)){
      i.At=0x11bbf8;if(child!=null){var table=S(N(child,context,0x11bbfd,i).VirtualRva00,0x11bbfd,i);i.At=0x11bc05;DestroyNodeSlot0(child,table,1,context,i,new HashSet<GroundSpatialNode>());}
      W(parent.Child(index),(GroundSpatialNode)null,0x11bc07,i);
     }
    }
    if(R(parent.Head48,0x11bc14,i)!=null){i.At=0x11bc58;return parent;}
    var higher=S(parent.Parent08,0x11bc1a,i);var higherValue=R(higher,0x11bc1e,i);if(higherValue==null){i.At=0x11bc58;return parent;}
    for(int index=0;index<4;index++)if(R(parent.Child(index),0x11bc26,i)!=null){i.At=0x11bc58;return parent;}
    target=parent;parent=higherValue;
   }
  }
  static GroundSpatialNode DestroyNodeSlot0(GroundSpatialNode child,Field<uint> table,uint flags,GroundSpatialContext context,Invocation i,HashSet<GroundSpatialNode> active){
   if(table.Read(i.At)!=0x340e60u)throw new MissingContract(i.At,"Unobserved dynamic node table; fixed disk340E60 slot0 binding not selected");
   if(active.Count>=context.OwnTransitionLimit||active.Contains(child))throw new MissingContract(i.At,"Own finite acyclic destructor domain exceeded; earlier clears/frees retained");
   return DestroyCore11BB30(child,flags,context,i,active);
  }
  // Engine11BB30 plus exact340E60 first-QWORD binding in
  // r1-spatial-ground-callback-slots. This direct function overwrites self's
  // table before examining children; only child virtual dispatch checks the
  // captured table. The explicit managed free adapter invalidates the own
  // view/removes its active allocation, not a native heap or allocator model.
  public static GroundSpatialNode DestroyNode11BB30(GroundSpatialNode node,uint flags,GroundSpatialContext context,Invocation invocation){
   return DestroyCore11BB30(node,flags,context,Need(invocation,0x11bb53),new HashSet<GroundSpatialNode>());
  }
  static GroundSpatialNode DestroyCore11BB30(GroundSpatialNode node,uint flags,GroundSpatialContext context,Invocation i,HashSet<GroundSpatialNode> active){
   node=N(node,context,0x11bb53,i);if(!active.Add(node))throw new MissingContract(i.At,"Own destructor recursion cycle");
   try{
    W(node.VirtualRva00,0x340e60u,0x11bb53,i);
    for(int index=0;index<4;index++){
     var pointer=S(node.Child(index),0x11bb65,i);var child=R(pointer,0x11bb68,i);
     if(child!=null){var table=S(N(child,context,0x11bb6d,i).VirtualRva00,0x11bb6d,i);i.At=0x11bb75;DestroyNodeSlot0(child,table,1,context,i,active);W(node.Child(index),(GroundSpatialNode)null,0x11bb77,i);}
    }
    i.At=0x11bb84;if((flags&1u)!=0){i.At=0x11bb90;context.Free(node,i.At);}i.At=0x11bbb2;return node;
   }finally{active.Remove(node);}
  }

  sealed class QueryBudget {
   public int Visits;
   public readonly HashSet<GroundSpatialNode> Active=new HashSet<GroundSpatialNode>();
   public void Visit(GroundSpatialContext context,uint at){if(++Visits>context.OwnQueryVisitLimit)throw new MissingContract(at,"OWN finite query visit bound; earlier result writes retained");}
  }
  static float Distance(Field<float> value,uint at,Invocation i){i.At=at;float d=value.Read(at);if(float.IsNaN(d))throw new MissingContract(at,"OWN unordered query distance unsupported; not a native miss");return d;}
  static float Ordered(float d,uint at){if(float.IsNaN(d))throw new MissingContract(at,"OWN unordered query bound unsupported; not a native miss");return d;}
  static void BlockTable(Field<uint> table,uint at,Invocation i){i.At=at;if(table.Read(at)!=0x3438f8u)throw new MissingContract(at,"Unobserved Block primary table; no default virtual success");}
  // flags/extraFlag are preserved by the traversal ABI; the proven1D1330 body
  // does not read them. The ignored object is compared only AFTER predicate+18.
  // Caller output is never initialized here. The actual body owns its local
  // hit initialization and its material/geometry stops remain authoritative.
  public static GroundSpatialEntry Query1D0100(GroundSpatialNode node,GroundRay24 ray,GroundBlockHit20 output,uint flags,float maxDistance,GroundSpatialEntry ignoreObject,byte extraFlag,GroundSpatialContext context,Invocation invocation){
   return QueryCore(Field<GroundSpatialNode>.Of(node),ray,output,flags,maxDistance,ignoreObject,extraFlag,context,Need(invocation,0x1d011b),new QueryBudget());
  }
  static GroundSpatialEntry QueryCore(Field<GroundSpatialNode> nodePointer,GroundRay24 ray,GroundBlockHit20 output,uint flags,float maxDistance,GroundSpatialEntry ignoreObject,byte extraFlag,GroundSpatialContext context,Invocation i,QueryBudget budget){
   var node=N(R(nodePointer,0x1d011b,i),context,0x1d011b,i);budget.Visit(context,i.At);if(!budget.Active.Add(node))throw new MissingContract(i.At,"OWN recursive query cycle");
   try{
    var head=S(node.Head48,0x1d011b,i);GroundSpatialEntry best=null;var entry=R(head,0x1d0138,i);var seen=new HashSet<GroundSpatialEntry>();
    while(entry!=null){
     entry=E(entry,context,0x1d0141,i);budget.Visit(context,i.At);if(!seen.Add(entry))throw new MissingContract(i.At,"OWN cyclic Ground object list");
     var table=S(entry.VirtualRva00,0x1d0141,i);BlockTable(table,0x1d0147,i);
     // Exact disk slot18=00CE80: MOV AL,1; RET. It is not baseCE70.
     i.At=0x1d014e;
     if(!Object.ReferenceEquals(entry,ignoreObject)){
      i.At=0x1d015f;float t=GroundRayBoundsContracts.Intersection03D780(entry.Box10,ray,Need(context.IntersectionOperands,i.At).Geometry,i);
      var capturedDistance=S(Need(output,0x1d0164).Distance00,0x1d0164,i);float bound=Min(Distance(capturedDistance,0x1d0168,i),Ordered(maxDistance,0x1d0168));
      i.At=0x1d016c;Ordered(t,i.At);
      if(t<bound){
       // Same entry's vptr is loaded again, after its bounds operation.
       var currentTable=S(entry.VirtualRva00,0x1d017e,i);var local=new GroundBlockHit20();BlockTable(currentTable,0x1d0193,i);
       GroundBlockIntersectionContracts.Intersect1D1330(entry.MaterialState,ray,local,context.Terrain,context.IntersectionOperands,i);
       var capturedLocal=S(local.Distance00,0x1d0196,i);float localDistance=Distance(capturedLocal,0x1d019c,i),currentDistance=Distance(output.Distance00,0x1d019c,i);
       if(localDistance<currentDistance){ // Native does NOT recheck maxDistance here.
        var d=S(local.Distance00,0x1d01a1,i);var x=S(local.Position04.X0,0x1d01a1,i);var y=S(local.Position04.Y4,0x1d01a1,i);var z=S(local.Position04.Z8,0x1d01a1,i);var material=S(local.Material10,0x1d01a6,i);
        i.At=0x1d01aa;best=entry;C(output.Distance00,d,0x1d01ad,i);C(output.Position04.X0,x,0x1d01ad,i);C(output.Position04.Y4,y,0x1d01ad,i);C(output.Position04.Z8,z,0x1d01ad,i);C(output.Material10,material,0x1d01b0,i);
       }
      }
     }
     var next=S(entry.Next28,0x1d01b3,i);entry=R(next,0x1d01b7,i);
    }
    for(int n=0;n<4;n++){
     var captured=S(node.Child(n),0x1d01d0,i);var child=R(captured,0x1d01d3,i);if(child==null)continue;child=N(child,context,0x1d01dc,i);
     // 8+4+8+4 byte loads/stores into a local24B box. Unknown fields are
     // copied as knowledge, consumed only by the real bounds function.
     var x=S(child.Box30.Center00,0x1d01dc,i);var y=S(child.Box30.Center04,0x1d01dc,i);var z=S(child.Box30.Center08,0x1d01e1,i);
     var capturedDistance=S(Need(output,0x1d01e4).Distance00,0x1d01e4,i);float bound=Min(Distance(capturedDistance,0x1d01f0,i),Ordered(maxDistance,0x1d01f0));
     var box=new WorldBounds24();C(box.Center00,x,0x1d01f4,i);C(box.Center04,y,0x1d01f4,i);
     var hx=S(child.Box30.Extent0C,0x1d01fa,i);var hy=S(child.Box30.Extent10,0x1d01fa,i);C(box.Center08,z,0x1d01ff,i);var hz=S(child.Box30.Extent14,0x1d0203,i);
     C(box.Extent0C,hx,0x1d020b,i);C(box.Extent10,hy,0x1d020b,i);C(box.Extent14,hz,0x1d0211,i);
     i.At=0x1d0215;float t=GroundRayBoundsContracts.Intersection03D780(box,ray,Need(context.IntersectionOperands,i.At).Geometry,i);i.At=0x1d021a;Ordered(t,i.At);
     if(t<bound){var target=S(node.Child(n),0x1d023a,i);i.At=0x1d0254;var candidate=QueryCore(target,ray,output,flags,bound,ignoreObject,extraFlag,context,i,budget);if(candidate!=null)best=candidate;}
    }
    i.At=0x1d0285;return best;
   }finally{budget.Active.Remove(node);}
  }
 }
}
