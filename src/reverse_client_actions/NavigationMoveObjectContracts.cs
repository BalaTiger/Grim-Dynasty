using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit OWN address identities, not CLR addresses or a recovered native
 // allocator. A full Entity and its +38 interface have one immutable binding.
 public sealed class NavigationMoveAddressSpace {
  readonly Dictionary<GameMovementInterfaceView,ulong> addresses=new Dictionary<GameMovementInterfaceView,ulong>();
  readonly Dictionary<ulong,GameMovementInterfaceView> identities=new Dictionary<ulong,GameMovementInterfaceView>();
  public readonly GameMovementCallbackContext Game;
  public NavigationMoveAddressSpace(GameMovementCallbackContext game){if(game==null)throw new ArgumentNullException("game");Game=game;}
  public void BindOwnEntityAddress(GameMovementCharacterState owner,ulong baseAddress){
   if(owner==null||baseAddress==0||baseAddress>UInt64.MaxValue-0x38)throw new ArgumentException("Non-null bounded own Entity address required");
   if(!Object.ReferenceEquals(Game.Owner(owner.Interface38,0),owner))throw new ArgumentException("Noncanonical owner");
   ulong address=baseAddress+0x38,previous;GameMovementInterfaceView prior;
   if(addresses.TryGetValue(owner.Interface38,out previous)&&previous!=address)throw new InvalidOperationException("Cannot rebind own interface address");
   if(identities.TryGetValue(address,out prior)&&!Object.ReferenceEquals(prior,owner.Interface38))throw new InvalidOperationException("Own interface address collision");
   addresses[owner.Interface38]=address;identities[address]=owner.Interface38;
  }
  public ulong Address(GameMovementInterfaceView view,uint at){if(view==null)return 0;Game.Owner(view,at).Entity.RequireLive(at);ulong address;if(!addresses.TryGetValue(view,out address))throw new MissingContract(at,"Interface38 has no explicit own address binding");return address;}
 }
 public sealed class NavigationMoveRegistryNode:NavigationView {
  public readonly Field<NavigationMoveRegistryNode> Next00=new Field<NavigationMoveRegistryNode>();
  public readonly Field<ulong> Key10=new Field<ulong>();
  public readonly Field<NavigationCrowdAgent> Agent18=new Field<NavigationCrowdAgent>();
 }
 public sealed class NavigationMoveRegistryBucket {
  public readonly Field<NavigationMoveRegistryNode> First00=new Field<NavigationMoveRegistryNode>(),Last08=new Field<NavigationMoveRegistryNode>();
 }
 public sealed class NavigationMoveRegistryBuckets:NavigationView {
  public readonly NavigationMoveRegistryBucket[] Cells;
  public NavigationMoveRegistryBuckets(int count){if(count<0)throw new ArgumentOutOfRangeException("count");Cells=new NavigationMoveRegistryBucket[count];for(int n=0;n<count;n++)Cells[n]=new NavigationMoveRegistryBucket();}
  internal NavigationMoveRegistryBucket At(ulong index,uint at){RequireLive(at);if(index>=(ulong)Cells.Length||Cells[(int)index]==null)throw new MissingContract(at,"Outside own stride16 hash bucket extent");return Cells[(int)index];}
 }
 // One sidecar for missing CrowdManager+28 map fields; no second Agent list.
 // Its node.Agent18 must point to the same pre-existing registered Agent.
 // Header construction/insertion (AddAgent) is NOT implemented by this view.
 public sealed class NavigationMoveRegistry {
  static readonly ConditionalWeakTable<NavigationCrowdManager,NavigationMoveRegistry> maps=new ConditionalWeakTable<NavigationCrowdManager,NavigationMoveRegistry>();
  public readonly NavigationCrowdManager Manager;
  public readonly Field<NavigationMoveRegistryNode> Sentinel30=new Field<NavigationMoveRegistryNode>();
  public readonly Field<NavigationMoveRegistryBuckets> Buckets40=new Field<NavigationMoveRegistryBuckets>();
  public readonly Field<ulong> Mask58=new Field<ulong>();
  NavigationMoveRegistry(NavigationCrowdManager manager){Manager=manager;}
  public static NavigationMoveRegistry ForManager(NavigationCrowdManager manager){if(manager==null)throw new ArgumentNullException("manager");return maps.GetValue(manager,delegate(NavigationCrowdManager value){return new NavigationMoveRegistry(value);});}
 }
 public sealed class NavigationMoveOuterView {
  static readonly ConditionalWeakTable<NavigationCrowdAdapterState,NavigationMoveOuterView> views=new ConditionalWeakTable<NavigationCrowdAdapterState,NavigationMoveOuterView>();
  public readonly NavigationCrowdAdapterState Adapter;
  public readonly Field<uint> Vtable00=new Field<uint>();
  public readonly Field<NavigationCrowdManager> Crowd08=new Field<NavigationCrowdManager>();
  NavigationMoveOuterView(NavigationCrowdAdapterState adapter){Adapter=adapter;}
  public static NavigationMoveOuterView ForAdapter(NavigationCrowdAdapterState adapter){if(adapter==null)throw new ArgumentNullException("adapter");return views.GetValue(adapter,delegate(NavigationCrowdAdapterState value){return new NavigationMoveOuterView(value);});}
 }
 public sealed class NavigationMoveManagerView {
  public readonly Field<NavigationCrowdAdapterState> Outer10;
  public NavigationMoveManagerView():this(new Field<NavigationCrowdAdapterState>()){}
  public NavigationMoveManagerView(Field<NavigationCrowdAdapterState> borrowedOuter10){if(borrowedOuter10==null)throw new ArgumentNullException("borrowedOuter10");Outer10=borrowedOuter10;}
 }
 public sealed class NavigationMoveObjectContext {
  public readonly NavigationMoveAddressSpace Addresses;
  public readonly NavigationCrowdAdapterBoundaries Adapter;
  public readonly NavigationCrowdPathBoundaries Path;
  public readonly Field<float> Half34BADC=new Field<float>(),Quarter34BA80=new Field<float>();
  public readonly Field<byte> DisableMissingAgent422B8D=new Field<byte>(),DisableAllAssert42144A=new Field<byte>();
  public NavigationMoveObjectContext(NavigationMoveAddressSpace addresses,NavigationCrowdAdapterBoundaries adapter,NavigationCrowdPathBoundaries path){if(addresses==null||adapter==null||path==null)throw new ArgumentNullException("move context");Addresses=addresses;Adapter=adapter;Path=path;}
  // Saved disk operands only: explicit OWN choice, not loaded-state evidence.
  public void UseObservedConstants(){Half34BADC.Write(0.5f);Quarter34BA80.Write(0.25f);}
 }
 public sealed class NavigationMoveObjectInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Field<ulong> Key=new Field<ulong>(),Hash=new Field<ulong>(),Bucket=new Field<ulong>();
  public readonly Field<NavigationCrowdAgent> Agent=new Field<NavigationCrowdAgent>();
  public readonly Field<uint> ProjectedReference=new Field<uint>();
  public readonly Field<byte> Result=new Field<byte>();
  public int HashedBytes,VisitedNodes;public bool LookupReturned,ProjectionReturned,Published,CompressionReturned,Returned;
  internal void Reset(){Native.At=0;Key.Forget();Hash.Forget();Bucket.Forget();Agent.Forget();ProjectedReference.Forget();Result.Forget();HashedBytes=0;VisitedNodes=0;LookupReturned=false;ProjectionReturned=false;Published=false;CompressionReturned=false;Returned=false;}
 }
 // Evidence: Engine151130;25B300;05DAF0;25A1C0;25E320;25C1C0 saved
 // respectively in r1-navigation-consumer-direct, r1-movement-floor-core,
 // r1-crowd-movement-direct and r1-crowd-query-production. Query table33A6C0
 // slot18 is030B30, r1-crowd-query-slots. Original SHA4c2ea14c...8a93153.
 // OWN domain: finite ordinary binary32, stable single-thread typed pointers,
 // bounded acyclic maps/vectors. Real existing030B30/030D20/030C60 are called;
 // no successful callback or Submit25A4F0 substitutes for the request prefix.
 public static class NavigationMoveObjectContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own MoveObject view");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static float Scalar(float value,uint at){uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(value),0);if((bits&0x7f800000u)==0x7f800000u||((bits&0x7f800000u)==0&&(bits&0x7fffffu)!=0))throw new MissingContract(at,"MoveObject OWN FP domain excludes nonfinite/subnormal and unobserved MXCSR");return value;}
  static float F(double value,uint at){if(value!=0&&Math.Abs(value)<1.1754943508222875E-38)throw new MissingContract(at,"Subnormal intermediate outside OWN FP domain");return Scalar((float)value,at);}
  static Field<float> Cell(NavigationPoint3 p,int n,uint at){Need(p,at);try{return n==0?p.X:n==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static float Read(NavigationPoint3 p,int n,uint at){return Scalar(Cell(p,n,at).Read(at),at);}
  static float Horizontal(NavigationPoint3 a,NavigationPoint3 b,uint at){float x=Read(a,0,at),z=Read(a,2,at);z=F((double)z-Read(b,2,at),at);x=F((double)x-Read(b,0,at),at);z=F((double)z*z,at);x=F((double)x*x,at);float sq=F((double)z+x,at);return sq==0?0:F(Math.Sqrt(sq),at);}
  static NavigationMoveRegistryNode Node(NavigationMoveRegistryNode n,uint at){return Need(n,at);}
  public static NavigationMoveRegistryNode Lookup05DAF0(NavigationMoveRegistry map,Field<ulong> key,NavigationMoveObjectInvocation call){
   var i=Need(call,0x05daf5).Native;Need(map,0x05daf5).Manager.RequireLive(0x05daf5);i.At=0x05daf5;ulong value=Need(key,i.At).Read(i.At),hash=0xcbf29ce484222325UL;
   // The local caller key is disjoint from the map and cannot mutate in this
   // single-thread domain. All eight bytes, including upper32, are hashed.
   var buckets=map.Buckets40.Snapshot();var sentinel=map.Sentinel30.Snapshot();
   for(int n=0;n<8;n++){hash=unchecked((hash^(byte)(value>>(8*n)))*0x100000001b3UL);call.HashedBytes++;}call.Hash.Write(hash);
   i.At=0x05db7d;ulong index=map.Mask58.Read(i.At)&hash;call.Bucket.Write(index);
   i.At=0x05db89;var bucket=Need(buckets.Read(i.At),i.At).At(index,i.At);var cursor=bucket.First00.Snapshot();var first=bucket.First00.Snapshot();var seen=new HashSet<NavigationMoveRegistryNode>();
   for(;;){
    i.At=0x05db95;Field<NavigationMoveRegistryNode> end;
    if(Object.ReferenceEquals(first.Read(i.At),sentinel.Read(i.At)))end=sentinel.Snapshot();
    else{i.At=0x05dba3;end=Node(bucket.Last08.Read(i.At),i.At).Next00.Snapshot();}
    i.At=0x05dba6;if(Object.ReferenceEquals(cursor.Read(i.At),end.Read(i.At))){call.LookupReturned=true;return sentinel.Read(0x05dbd0);}
    i.At=0x05dbae;var node=Node(cursor.Read(i.At),i.At);if(!seen.Add(node))throw new MissingContract(i.At,"Cyclic own registry bucket");call.VisitedNodes++;
    if(node.Key10.Read(i.At)==key.Read(i.At)){i.At=0x05dbbd;var found=node.Key10.Read(i.At)==key.Read(i.At)?node:sentinel.Read(i.At);call.LookupReturned=true;return found;}
    i.At=0x05dbb4;cursor=node.Next00.Snapshot();
   }
  }
  static long Count<T>(NavigationCrowdPointerAddress<T> end,NavigationCrowdPointerAddress<T> begin,uint at)where T:class{if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer)||end.Index<begin.Index)throw new MissingContract(at,"Invalid own stride8 range");begin.Buffer.RequireLive(at);return (long)end.Index-begin.Index;}
  static T At<T>(NavigationCrowdPointerAddress<T> begin,uint n,uint at)where T:class{Need(begin,at);long index=(long)begin.Index+n;if(index>Int32.MaxValue)throw new MissingContract(at,"Own vector index overflow");return begin.Buffer.At((int)index,at).Read(at);}
  public static NavigationCrowdAgent Overlap25E320(NavigationCrowdGroup group,NavigationPoint3 target,NavigationCrowdAgent excluded,NavigationMoveObjectContext context,Invocation i){
   var v=Need(group,0x25e320).Members28;var end=v.End08.Snapshot();var begin=v.Begin00.Snapshot();i.At=0x25e332;if(Count(end.Read(i.At),begin.Read(i.At),i.At)==0)return null;
   begin=v.Begin00.Snapshot();var half=Need(context,0x25e342).Half34BADC.Snapshot();uint n=0;
   do{
    i.At=0x25e350;var agent=At(begin.Read(i.At),n,i.At);
    if(!Object.ReferenceEquals(agent,excluded)){
     i.At=0x25e359;uint mask=Need(agent,i.At).ParameterBits1C40[1].Read(i.At);i.At=0x25e35c;
     if((Need(excluded,i.At).ParameterBits1C40[2].Read(i.At)&mask)!=0){
      // Height is MOV-captured before horizontal arithmetic but consumed only
      // at MUL25E39B. Unknown is not prematurely turned into a failure.
      var height=agent.Height14.Snapshot();i.At=0x25e362;float distance=Horizontal(agent.Position48,target,i.At);
      i.At=0x25e397;bool horizontal=distance<=Scalar(agent.Radius10.Read(i.At),i.At);i.At=0x25e39b;float extent=F((double)Scalar(height.Read(i.At),i.At)*Scalar(half.Read(i.At),i.At),i.At);
      if(horizontal){i.At=0x25e3a1;float y=Read(agent.Position48,1,i.At),ty=Read(target,1,0x25e3a6);float low=F((double)y-extent,0x25e3ae);if(ty>=low&&ty<=F((double)y+extent,0x25e3b7)){i.At=0x25e3dd;return agent;}}
     }
    }
    i.At=0x25e3c0;end=v.End08.Snapshot();n=unchecked(n+1);if(n==0)throw new MissingContract(i.At,"OWN finite loop index overflow");i.At=0x25e3d1;
   }while(n<(ulong)Count(end.Read(i.At),begin.Read(i.At),i.At));i.At=0x25e3da;return null;
  }
  public static NavigationCrowdAgent Blocking25A1C0(NavigationCrowdManager manager,NavigationPoint3 target,NavigationCrowdAgent excluded,NavigationMoveObjectContext context,Invocation i){
   var v=Need(manager,0x25a1cf).Groups10;var end=v.End08.Snapshot();var begin=v.Begin00.Snapshot();i.At=0x25a1e3;if(Count(end.Read(i.At),begin.Read(i.At),i.At)==0)return null;begin=v.Begin00.Snapshot();uint n=0;
   do{i.At=0x25a1f3;var group=At(begin.Read(i.At),n,i.At);i.At=0x25a1fa;var found=Overlap25E320(group,target,excluded,context,i);if(found!=null)return found;end=v.End08.Snapshot();n=unchecked(n+1);if(n==0)throw new MissingContract(0x25a20b,"OWN finite loop index overflow");i.At=0x25a216;}while(n<(ulong)Count(end.Read(i.At),begin.Read(i.At),i.At));return null;
  }
  static long RefCount(NavigationCrowdRefAddress end,NavigationCrowdRefAddress begin,uint at){if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer)||end.Index<begin.Index)throw new MissingContract(at,"Invalid own stride4 range");begin.Buffer.RequireLive(at);return (long)end.Index-begin.Index;}
  static NavigationCrowdPointAddress PointAddress(object value,uint at){if(value==null)return null;var p=value as NavigationCrowdPointAddress;if(p==null)throw new MissingContract(at,"Waypoint header is not a stride12 address");return p;}
  static long PointCount(NavigationCrowdPointAddress end,NavigationCrowdPointAddress begin,uint at){if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer)||end.PointIndex<begin.PointIndex)throw new MissingContract(at,"Invalid own stride12 range");begin.Buffer.RequireLive(at);return end.PointIndex-begin.PointIndex;}
  public static void Compress25C1C0(NavigationCrowdRequest request,NavigationCrowdPathBoundaries path,Invocation i){
   var r=Need(request,0x25c1c6).RefVector20;var end=r.End08.Snapshot();var begin=r.Begin00.Snapshot();i.At=0x25c1d9;
   if(RefCount(end.Read(i.At),begin.Read(i.At),i.At)>1){var b=r.Begin00.Read(0x25c1df);var e=r.End08.Read(0x25c1e2);long count=RefCount(e,b,0x25c1e9);var last=Need(b,0x25c1ed).Buffer.At((long)b.Index+count-1,0x25c1ed).Snapshot();i.At=0x25c1f1;b.Buffer.At(b.Index,i.At).CopyFrom(last);i.At=0x25c1f8;NavigationCrowdAdapterContracts.Resize030D20(r,1,i);}
   var v=new NavigationCrowdWaypointVector(request.WaypointVector38);var pe=v.End08.Snapshot();var pb=v.Begin00.Snapshot();i.At=0x25c222;
   if(PointCount(PointAddress(pe.Read(i.At),i.At),PointAddress(pb.Read(i.At),i.At),i.At)>1){var b=PointAddress(v.Begin00.Read(0x25c228),0x25c228);var e=PointAddress(v.End08.Read(0x25c22f),0x25c22f);long count=PointCount(e,b,0x25c247);var last=new NavigationCrowdPointAddress(Need(b,0x25c253).Buffer,b.PointIndex+count-1);var x=last.Cell(0,0x25c253).Snapshot();var y=last.Cell(1,0x25c253).Snapshot();b.Cell(0,0x25c25a).CopyFrom(x);b.Cell(1,0x25c25a).CopyFrom(y);var z=last.Cell(2,0x25c25f).Snapshot();b.Cell(2,0x25c264).CopyFrom(z);i.At=0x25c26d;NavigationCrowdPathContracts.Resize030C60(v,1,path,i);}
  }
  static byte Return(byte value,NavigationMoveObjectInvocation call){call.Native.At=value==0?0x25b53du:0x25b461u;call.Result.Write(value);call.Returned=true;return value;}
  public static byte Move25B300(NavigationCrowdManager manager,Field<ulong> interfaceKey,NavigationPoint3 target,NavigationMoveObjectContext context,NavigationMoveObjectInvocation call){
   Need(call,0x25b300);call.Reset();var i=call.Native;var ctx=Need(context,0x25b327);call.Key.CopyFrom(Need(interfaceKey,0x25b310));var map=NavigationMoveRegistry.ForManager(Need(manager,0x25b327));var selected=Lookup05DAF0(map,call.Key,call);
   i.At=0x25b334;NavigationCrowdAgent agent=null;if(!Object.ReferenceEquals(selected,map.Sentinel30.Read(i.At))){i.At=0x25b33e;agent=Need(selected,i.At).Agent18.Read(i.At);}call.Agent.Write(agent);
   if(agent==null){i.At=0x25b468;if(ctx.DisableMissingAgent422B8D.Read(i.At)==0){i.At=0x25b475;if(ctx.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25b48c;throw new MissingContract(i.At,"Native missing-agent diagnostics/callbacks unsupported; no fabricated false success");}}return Return(0,call);}
   agent.RequireLive(0x25b34b);i.At=0x25b34b;float distance=Horizontal(target,agent.Position48,i.At);i.At=0x25b37e;if(distance<=Scalar(BitConverter.ToSingle(BitConverter.GetBytes(agent.ParameterBits1C40[3].Read(i.At)),0),i.At))return Return(0,call);
   i.At=0x25b391;var blocker=Blocking25A1C0(manager,target,agent,ctx,i);
   if(blocker!=null){i.At=0x25b39b;distance=Horizontal(agent.Position48,blocker.Position48,i.At);float radius=Scalar(blocker.Radius10.Read(0x25b3c7),0x25b3c7);radius=F((double)radius+Scalar(agent.Radius10.Read(0x25b3cc),0x25b3cc),0x25b3cc);radius=F((double)radius+Scalar(ctx.Quarter34BA80.Read(0x25b3d1),0x25b3d1),0x25b3d1);if(distance<=radius)return Return(0,call);}
   i.At=0x25b3e2;var query=Need(manager.QueryB8.Read(i.At),i.At);i.At=0x25b3f1;uint table=NavigationMoveOuterView.ForAdapter(query).Vtable00.Read(i.At);i.At=0x25b3f4;if(table!=0x33a6c0)throw new MissingContract(i.At,"Unbound real query virtual18; no injected projection");
   var projected=new NavigationPoint3();uint reference=NavigationCrowdAdapterContracts.Project030B30(query,target,projected,ctx.Adapter,i);call.ProjectedReference.Write(reference);call.ProjectionReturned=true;if(reference==0)return Return(0,call);
   i.At=0x25b401;uint flags=agent.Flags44.Read(i.At);var px=projected.X.Snapshot();var py=projected.Y.Snapshot();var pz=projected.Z.Snapshot();i.At=0x25b40f;agent.Target78.X.CopyFrom(px);agent.Target78.Y.CopyFrom(py);i.At=0x25b414;agent.Target78.Z.CopyFrom(pz);i.At=0x25b41a;agent.State70.Write(2);call.Published=true;
   if((flags&0x80)==0){i.At=0x25b42a;Compress25C1C0(agent.RequestE8,ctx.Path,i);call.CompressionReturned=true;}
   var refs=agent.RequestE8.RefVector20;var re=refs.End08.Snapshot();var rb=refs.Begin00.Snapshot();i.At=0x25b441;int count=unchecked((int)RefCount(re.Read(i.At),rb.Read(i.At),i.At));uint last=0;if(count!=0){var b=Need(refs.Begin00.Read(0x25b448),0x25b44f);last=b.Buffer.At((long)b.Index+unchecked((uint)(count-1)),0x25b44f).Read(0x25b44f);}i.At=0x25b456;if(last!=reference){i.At=0x25b45a;agent.NeedReplan140.Write(1);}return Return(1,call);
  }
  public static byte Move151130(NavigationMoveManagerView navigation,GameMovementCharacterState owner,NavigationPoint3 target,NavigationMoveObjectContext context,NavigationMoveObjectInvocation call){
   var outer=Need(Need(navigation,0x151130).Outer10.Read(0x151130),0x151141);var crowd=NavigationMoveOuterView.ForAdapter(outer).Crowd08.Read(0x151141);var key=Field<ulong>.Of(Need(context,0x151149).Addresses.Address(owner==null?null:owner.Interface38,0x151149));
   return Move25B300(crowd,key,target,context,call);
  }
 }
}
