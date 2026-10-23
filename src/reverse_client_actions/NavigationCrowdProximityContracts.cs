using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace GrimDynasty.ReverseClient.Actions {
 // This header is a view of Group+58, not a second set of index state.
 public sealed class NavigationCrowdProximityIndex {
  public readonly NavigationCrowdGroup Group;
  public readonly Field<object> Buckets00,Nodes08,Free18;
  public readonly Field<uint> Capacity10,Count20;
  public NavigationCrowdProximityIndex(NavigationCrowdGroup group){if(group==null)throw new ArgumentNullException("group");Group=group;Buckets00=group.Pointer58;Nodes08=group.Pointer60;Capacity10=group.Count68;Free18=group.Pointer70;Count20=group.Count78;}
  public void RequireLive(uint at){Group.RequireLive(at);}
 }
 public sealed class NavigationCrowdProximityNode:NavigationView {
  public readonly Field<int> X00=new Field<int>(),Y04=new Field<int>(),Z08=new Field<int>();
  public readonly Field<uint> Gap0C=new Field<uint>();
  public readonly Field<NavigationCrowdAgent> Agent10=new Field<NavigationCrowdAgent>();
  // Opaque pointer cells let MOV preserve unknown bits until their first use.
  public readonly Field<object> Next18=new Field<object>();
  public readonly NavigationCrowdProximityNodeBuffer Buffer;public readonly int Index;
  internal NavigationCrowdProximityNode(NavigationCrowdProximityNodeBuffer buffer,int index){Buffer=buffer;Index=index;}
  public new void RequireLive(uint at){base.RequireLive(at);Buffer.RequireLive(at);}
 }
 public sealed class NavigationCrowdProximityNodeBuffer:NavigationView {
  readonly NavigationCrowdProximityNode[] nodes;
  public int Length{get{return nodes.Length;}}
  public NavigationCrowdProximityNodeBuffer(int capacity){if(capacity<0)throw new ArgumentOutOfRangeException("capacity");nodes=new NavigationCrowdProximityNode[capacity];for(int n=0;n<capacity;n++)nodes[n]=new NavigationCrowdProximityNode(this,n);}
  public NavigationCrowdProximityNode At(uint index,uint at){RequireLive(at);if(index>=nodes.Length)throw new MissingContract(at,"Outside own stride20 proximity-node extent");return nodes[(int)index];}
 }
 public sealed class NavigationCrowdProximityBoundaries {
  public readonly Field<float> Half34BADC;
  public readonly Field<byte> DisableAllAssert42144A;
  public readonly Field<byte> DisableNoFreeAssert422BA0=new Field<byte>();
  public NavigationCrowdProximityBoundaries(NavigationCrowdStateBoundaries state){if(state==null)throw new ArgumentNullException("state");Half34BADC=state.Half34BADC;DisableAllAssert42144A=state.DisableAllAssert42144A;}
 }
 // Evidence: same-Engine SHA4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153,
 // r1-crowd-motion-helpers/{04,05,07,08,10,11}; neighbor output growth:
 // r1-crowd-vector-growth-retry-1/{07,08}, direct034FE0 target capacity.
 // Floating .5 disk bytes:
 // r1-navigation-compressor-slot/inspection.json,
 // SHA191e63e5cc34d9b1abf1326426827a27041743913768a3159e73af3dc62126de.
 // OWN domain: stable single-thread typed identities, normal BCL allocation,
 // finite binary32 nearest rounding, disjoint local/heap pointer addresses.
 // No native allocator/OOM, MXCSR, arbitrary byte aliases or complete motion
 // is claimed. Unknown pointers, expired pools and unresolved calls fail closed.
 public static class NavigationCrowdProximityContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own proximity view");var view=value as NavigationView;if(view!=null)view.RequireLive(at);return value;}
  static NavigationCrowdProximityIndex Header(NavigationCrowdProximityIndex index,uint at){Need(index,at).RequireLive(at);return index;}
  static NavigationCrowdProximityNode Node(object pointer,uint at){var node=pointer as NavigationCrowdProximityNode;if(node==null)throw new MissingContract(at,"Pointer is not a declared own proximity node");node.RequireLive(at);return node;}
  static NavigationCrowdPointerBuffer<object> Buckets(object pointer,uint at){var buffer=pointer as NavigationCrowdPointerBuffer<object>;if(buffer==null)throw new MissingContract(at,"Pointer is not a declared own stride8 bucket allocation");buffer.RequireLive(at);return buffer;}
  static NavigationCrowdProximityNodeBuffer Nodes(object pointer,uint at){var buffer=pointer as NavigationCrowdProximityNodeBuffer;if(buffer==null)throw new MissingContract(at,"Pointer is not a declared own stride20 node allocation");buffer.RequireLive(at);return buffer;}
  static Field<object> Bucket(NavigationCrowdPointerBuffer<object> buffer,uint index,uint at){if(index>Int32.MaxValue)throw new MissingContract(at,"Bucket index outside own BCL extent");return buffer.At((int)index,at);}
  static void FreeBuckets(Field<object> pointer,uint at){object value=pointer.Read(at);if(value!=null)Buckets(value,at).MarkReleased();}
  static void FreeNodes(Field<object> pointer,uint at){object value=pointer.Read(at);if(value!=null)Nodes(value,at).MarkReleased();}
  static int AllocationCount(uint count,uint at){if(count>Int32.MaxValue)throw new MissingContract(at,"Native allocation size exceeds the declared own BCL array domain");return (int)count;}
  public static void Prepare25E900(NavigationCrowdProximityIndex index,uint requested,Invocation invocation){
   var i=Need(invocation,0x25e909);uint size;
   if(requested<=64)size=128;else{size=unchecked(requested-1);size|=size>>1;size|=size>>2;size|=size>>4;size|=size>>8;size|=size>>16;size=unchecked(size+1);}
   i.At=0x25e93c;var h=Header(index,i.At);
   if(h.Capacity10.Read(i.At)<size){
    var oldBuckets=h.Buckets00.Snapshot();i.At=0x25e949;h.Capacity10.Write(size);
    i.At=0x25e94c;FreeBuckets(oldBuckets,i.At);uint count=h.Capacity10.Read(0x25e951);
    i.At=0x25e96a;var buckets=new NavigationCrowdPointerBuffer<object>(AllocationCount(count,i.At));
    var oldNodes=h.Nodes08.Snapshot();i.At=0x25e973;h.Buckets00.Write(buckets);
    i.At=0x25e976;FreeNodes(oldNodes,i.At);count=h.Capacity10.Read(0x25e97b);
    i.At=0x25e98d;var nodes=new NavigationCrowdProximityNodeBuffer(AllocationCount(count,i.At));
    i.At=0x25e997;h.Nodes08.Write(nodes);
   }
   i.At=0x25e9a1;uint n=0;
   if(h.Capacity10.Read(i.At)>0){do{var pointer=h.Buckets00.Snapshot();i.At=0x25e9b7;Bucket(Buckets(pointer.Read(i.At),i.At),n,i.At).Write(null);n=unchecked(n+1);i.At=0x25e9bb;}while(n<h.Capacity10.Read(i.At));}
   i.At=0x25e9c3;h.Free18.Write(null);n=0;i.At=0x25e9c7;
   if(h.Capacity10.Read(i.At)>0){
    var baseBits=h.Nodes08.Snapshot();
    do{var next=h.Free18.Snapshot();i.At=0x25e9dc;Nodes(baseBits.Read(i.At),i.At).At(n,i.At).Next18.CopyFrom(next);
     baseBits=h.Nodes08.Snapshot();i.At=0x25e9e9;h.Free18.Write(Nodes(baseBits.Read(i.At),i.At).At(n,i.At));n=unchecked(n+1);i.At=0x25e9ed;
    }while(n<h.Capacity10.Read(i.At));
   }
   i.At=0x25e9f2;h.Count20.Write(0);i.At=0x25e9fb;
  }
  static float Scalar(float value,uint at){uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(value),0);if((bits&0x7f800000u)==0x7f800000u||((bits&0x7f800000u)==0&&(bits&0x7fffffu)!=0))throw new MissingContract(at,"Own proximity FP domain excludes nonfinite/subnormal values and unobserved MXCSR modes");return value;}
  [MethodImpl(MethodImplOptions.NoInlining)]
  static float FP(double value,uint at){if(value!=0&&Math.Abs(value)<1.17549435082228750796873653722224568E-38)throw new MissingContract(at,"Own proximity FP domain excludes subnormal intermediates");return Scalar(BitConverter.ToSingle(BitConverter.GetBytes((float)value),0),at);}
  static int Cvt(float value,uint at){value=Scalar(value,at);if((double)value< -2147483648.0||(double)value>=2147483648.0)throw new MissingContract(at,"CVTTSS2SI invalid/overflow depends on unobserved native FP controls");return (int)value;}
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){Need(point,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static uint Remainder(uint hash,uint capacity,uint at){if(capacity==0)throw new MissingContract(at,"Native DIV by zero outside normal own proximity domain");return hash%capacity;}
  public static void Insert25EA00(NavigationCrowdProximityIndex index,NavigationCrowdAgent agent,NavigationCrowdProximityBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x25ea07);var h=Header(index,0x25ea07);var free=h.Free18.Snapshot();i.At=0x25ea0e;object pointer=free.Read(i.At);
   if(pointer==null){i.At=0x25ea78;if(Need(boundaries,i.At).DisableNoFreeAssert422BA0.Read(i.At)==0){i.At=0x25ea85;if(boundaries.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25eab4;throw new MissingContract(i.At,"Native exhausted proximity-pool diagnostics not implemented");}}i.At=0x25eb84;return;}
   i.At=0x25ea13;var node=Node(pointer,i.At);var next=node.Next18.Snapshot();i.At=0x25ea17;h.Free18.CopyFrom(next);
   i.At=0x25ea1b;int x=Cvt(Cell(Need(agent,i.At).Position48,0,i.At).Read(i.At),i.At);i.At=0x25ea20;node.X00.Write(x);
   i.At=0x25ea23;int y=Cvt(Cell(agent.Position48,1,i.At).Read(i.At),i.At);i.At=0x25ea28;node.Y04.Write(y);
   i.At=0x25ea2c;int z=Cvt(Cell(agent.Position48,2,i.At).Read(i.At),i.At);i.At=0x25ea31;node.Agent10.Write(agent);i.At=0x25ea37;node.Z08.Write(z);
   uint hash=unchecked((uint)node.Z08.Read(0x25ea3b)*0x04f9ffb7u);hash=unchecked((uint)node.Y04.Read(0x25ea43)*0x0127409fu+hash);hash=unchecked(hash+(uint)node.X00.Read(0x25ea4d)*0x0466f45du);
   i.At=0x25ea56;uint slot=Remainder(hash,h.Capacity10.Read(i.At),i.At);var bucketBits=h.Buckets00.Snapshot();
   i.At=0x25ea5d;var oldHead=Bucket(Buckets(bucketBits.Read(i.At),i.At),slot,i.At).Snapshot();i.At=0x25ea61;node.Next18.CopyFrom(oldHead);
   bucketBits=h.Buckets00.Snapshot();i.At=0x25ea68;Bucket(Buckets(bucketBits.Read(i.At),i.At),slot,i.At).Write(node);
   i.At=0x25ea6c;h.Count20.Write(unchecked(h.Count20.Read(i.At)+1u));i.At=0x25ea77;
  }
  static bool Same(NavigationCrowdPointerAddress<NavigationCrowdAgent> a,NavigationCrowdPointerAddress<NavigationCrowdAgent> b){return a==null?b==null:b!=null&&Object.ReferenceEquals(a.Buffer,b.Buffer)&&a.Index==b.Index;}
  static long Difference(NavigationCrowdPointerAddress<NavigationCrowdAgent> end,NavigationCrowdPointerAddress<NavigationCrowdAgent> begin,uint at){if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"Own pointer subtraction needs one declared allocation or two null pointers");end.Buffer.RequireLive(at);return (long)end.Index-begin.Index;}
  // The native input is a fresh stack local at25ECC4, disjoint from this own
  // heap vector. Only this caller-specific non-alias append arm is projected.
  static void AppendLocal(NavigationCrowdPointerHeader<NavigationCrowdAgent> output,NavigationCrowdAgent agent,Invocation i){
   Need(output,0x25ed3b);var endBits=output.Capture(NavigationCrowdPointerSlot.End,0x25ed3b);i.At=0x25ed44;var end=endBits.Read(i.At);i.At=0x25edf3;var cap=output.Read(NavigationCrowdPointerSlot.Capacity,i.At);
   if(Same(end,cap)){
    i.At=0x25ee0e;var begin=output.Read(NavigationCrowdPointerSlot.Begin,i.At);ulong count=unchecked((ulong)Difference(end,begin,i.At)),maximum=0x1ffffffffffffffful;
    if(unchecked(maximum-count)<1){i.At=0x25ef14;throw new MissingContract(i.At,"Native vector length-error dependency not implemented");}
    ulong capacity=unchecked((ulong)Difference(cap,begin,0x25ee22)),requested=unchecked(count+1),half=capacity>>1;
    ulong grown=unchecked(maximum-half)>=capacity?unchecked(capacity+half):0;if(grown>=requested)requested=grown;
    i.At=0x25ee4f;NavigationCrowdBorrowedPointerContracts.Reallocate034FE0(output,requested,i);
   }
   i.At=0x25ee62;end=output.Read(NavigationCrowdPointerSlot.End,i.At);if(end!=null){i.At=0x25ee67;end.Buffer.At(end.Index,i.At).Write(agent);}
   i.At=0x25ee6a;end=output.Read(NavigationCrowdPointerSlot.End,i.At);if(end==null||end.Index>=end.Buffer.Cells.Length)throw new MissingContract(i.At,"Pointer increment leaves own stride8 output domain");end.Buffer.RequireLive(i.At);output.Write(NavigationCrowdPointerSlot.End,new NavigationCrowdPointerAddress<NavigationCrowdAgent>(end.Buffer,end.Index+1),i.At);
  }
  public static void Neighbors25EB90(NavigationCrowdProximityIndex index,NavigationPoint3 center,float radius,float height,uint mask,NavigationCrowdAgent excluded,NavigationCrowdPointerVector<NavigationCrowdAgent> output,NavigationCrowdProximityBoundaries boundaries,Invocation invocation){
   NeighborsBorrowed25EB90(index,center,radius,height,mask,excluded,NavigationCrowdPointerHeader<NavigationCrowdAgent>.BorrowTyped(output),boundaries,invocation);
  }
  public static void NeighborsBorrowed25EB90(NavigationCrowdProximityIndex index,NavigationPoint3 center,float radius,float height,uint mask,NavigationCrowdAgent excluded,NavigationCrowdPointerHeader<NavigationCrowdAgent> output,NavigationCrowdProximityBoundaries boundaries,Invocation invocation){
   NeighborsCapturedBorrowed25EB90(index,center,Field<float>.Of(radius),Field<float>.Of(height),Field<uint>.Of(mask),excluded,output,boundaries,invocation);
  }
  // Borrowed register/stack argument snapshots preserve Unknown until the
  // corresponding arithmetic or mask test, including no-hit mask non-use.
  public static void NeighborsCapturedBorrowed25EB90(NavigationCrowdProximityIndex index,NavigationPoint3 center,Field<float> radiusArgument,Field<float> heightArgument,Field<uint> maskArgument,NavigationCrowdAgent excluded,NavigationCrowdPointerHeader<NavigationCrowdAgent> output,NavigationCrowdProximityBoundaries boundaries,Invocation invocation){
   var radiusBits=Need(radiusArgument,0x25eba2).Snapshot();var heightBits=Need(heightArgument,0x25eba2).Snapshot();var maskBits=Need(maskArgument,0x25eba2).Snapshot();
   var i=Need(invocation,0x25eba2);i.At=0x25eba2;var xBits=Cell(center,0,i.At).Snapshot();var yBits=Cell(center,1,0x25eba9).Snapshot();var zBits=Cell(center,2,0x25ebb1).Snapshot();
   i.At=0x25ebb9;float halfHeight=FP((double)Scalar(heightBits.Read(i.At),i.At)*Scalar(Need(boundaries,i.At).Half34BADC.Read(i.At),i.At),i.At);
   i.At=0x25ebc9;float x=Scalar(xBits.Read(i.At),i.At);float radius=Scalar(radiusBits.Read(i.At),i.At);float lowX=FP((double)x-radius,i.At),highX=FP((double)x+radius,0x25ebcd);int minX=Cvt(lowX,0x25ebd1);
   i.At=0x25ebd8;float y=Scalar(yBits.Read(i.At),i.At),highY=FP((double)y+halfHeight,i.At),lowY=FP((double)y-halfHeight,0x25ebdc);int maxX=Cvt(highX,0x25ebe4),minY=Cvt(lowY,0x25ebe8);
   i.At=0x25ebf7;float z=Scalar(zBits.Read(i.At),i.At),highZ=FP((double)z+radius,i.At),lowZ=FP((double)z-radius,0x25ebfb),radiusSquared=FP((double)radius*radius,0x25ebff);
   int maxY=Cvt(highY,0x25ec03),minZ=Cvt(lowZ,0x25ec08),maxZ=Cvt(highZ,0x25ec12);
   for(int cy=minY;cy<=maxY;cy=unchecked(cy+1)){
    for(int cz=minZ;cz<=maxZ;cz=unchecked(cz+1)){
     if(minX>maxX)continue;uint hz=unchecked((uint)cz*0x04f9ffb7u),hy=unchecked((uint)cy*0x0127409fu);
     for(int cx=minX;cx<=maxX;cx=unchecked(cx+1)){
      uint hash=unchecked(unchecked((uint)cx*0x0466f45du+hy)+hz);i.At=0x25ecac;var h=Header(index,i.At);uint slot=Remainder(hash,h.Capacity10.Read(i.At),i.At);
      var bucketBits=h.Buckets00.Snapshot();i.At=0x25ecb3;var head=Bucket(Buckets(bucketBits.Read(i.At),i.At),slot,i.At).Snapshot();i.At=0x25ecb7;object pointer=head.Read(i.At);
      var visited=new HashSet<NavigationCrowdProximityNode>();
      while(pointer!=null){
       i.At=0x25ecc0;var node=Node(pointer,i.At);if(!visited.Add(node))throw new MissingContract(i.At,"Cyclic hash chain outside finite own traversal domain");var agentBits=node.Agent10.Snapshot();
       i.At=0x25ecc9;var agent=agentBits.Read(i.At);bool match=!Object.ReferenceEquals(agent,excluded);
       if(match){i.At=0x25ecd2;match=node.X00.Read(i.At)==cx;}
       if(match){i.At=0x25ecda;match=node.Y04.Read(i.At)==cy;}
       if(match){i.At=0x25ece4;match=node.Z08.Read(i.At)==cz;}
       if(match){i.At=0x25ecf5;match=(Need(agent,i.At).ParameterBits1C40[1].Read(i.At)&maskBits.Read(i.At))!=0;}
       if(match){
        var ax=Cell(agent.Position48,0,0x25ecfe).Snapshot();var ay=Cell(agent.Position48,1,0x25ed03).Snapshot();
        i.At=0x25ed08;float dy=FP((double)Scalar(ay.Read(i.At),i.At)-Scalar(Cell(center,1,i.At).Read(i.At),i.At),i.At);
        i.At=0x25ed0e;float dx=FP((double)Scalar(ax.Read(i.At),i.At)-Scalar(Cell(center,0,i.At).Read(i.At),i.At),i.At);
        var az=Cell(agent.Position48,2,0x25ed13).Snapshot();i.At=0x25ed18;float dz=FP((double)Scalar(az.Read(i.At),i.At)-Scalar(Cell(center,2,i.At).Read(i.At),i.At),i.At);
        dy=FP((double)dy*dy,0x25ed1e);dx=FP((double)dx*dx,0x25ed22);dz=FP((double)dz*dz,0x25ed26);float distance=FP((double)dy+dx,0x25ed2a);distance=FP((double)distance+dz,0x25ed2e);
        i.At=0x25ed32;if(distance<radiusSquared)AppendLocal(output,agent,i);
       }
       var next=node.Next18.Snapshot();i.At=0x25ee73;pointer=next.Read(i.At);
      }
     }
    }
   }
   i.At=0x25eefe;
  }
 }
}
