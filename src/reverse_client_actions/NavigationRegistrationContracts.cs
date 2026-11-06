using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Caller-local, disjoint from the declared map/node storage. This models the
 // live key+Agent input consumed by05DF10; it is not a native address overlay.
 public sealed class NavigationRegistrationPair:NavigationView {
  public readonly Field<ulong> Key00=new Field<ulong>();
  public readonly Field<NavigationCrowdAgent> Agent08=new Field<NavigationCrowdAgent>();
 }
 public sealed class NavigationRegistrationResult:NavigationView {
  public readonly Field<NavigationMoveRegistryNode> Node00=new Field<NavigationMoveRegistryNode>();
  public readonly Field<byte> Inserted08=new Field<byte>();
  // Only the single byte08 is written by01FB90. These neighboring cells are
  // diagnostic OWN storage and are never filled by a pointer/boolean default.
  public readonly Field<byte>[] Tail09To0F=new Field<byte>[7];
  public NavigationRegistrationResult(){for(int n=0;n<7;n++)Tail09To0F[n]=new Field<byte>();}
 }
 public sealed class NavigationRegistrationContext {
  public readonly NavigationMoveObjectContext Movement;
  public readonly NavigationCrowdStateBoundaries State;
  public GameMovementCallbackContext Game{get{return Movement.Addresses.Game;}}
  public readonly Field<byte> DisableAlreadyRegistered422B8C=new Field<byte>();
  public Field<byte> DisableAllAssert42144A{get{return State.DisableAllAssert42144A;}}
  readonly HashSet<NavigationMoveRegistryNode> allocations=new HashSet<NavigationMoveRegistryNode>();
  public NavigationRegistrationContext(NavigationMoveObjectContext movement,NavigationCrowdStateBoundaries state){if(movement==null||state==null)throw new ArgumentNullException("registration dependencies");if(!Object.ReferenceEquals(movement.DisableAllAssert42144A,state.DisableAllAssert42144A))throw new ArgumentException("Registration, movement and state require the same42144A Field identity");Movement=movement;State=state;}
  internal NavigationMoveRegistryNode AllocateNode(){var node=new NavigationMoveRegistryNode();allocations.Add(node);return node;}
  internal void FreeNode(NavigationMoveRegistryNode node,uint at){
   if(node==null||!allocations.Contains(node))throw new MissingContract(at,"Engine free requires a live node from this explicit OWN allocation domain");
   node.RequireLive(at);node.MarkReleased();allocations.Remove(node);
  }
 }
 public sealed class NavigationRegistrationInvocation {
  public readonly Invocation Native=new Invocation();
  public NavigationMoveObjectInvocation Lookup{get;private set;}
  public readonly List<uint> StoreAddresses=new List<uint>();
  public readonly List<int> StoreWidths=new List<int>();
  public readonly Field<ulong> Key=new Field<ulong>(),Hash=new Field<ulong>(),Bucket=new Field<ulong>();
  public readonly Field<float> LoadRatio=new Field<float>();
  public readonly Field<ulong> RequestedBucketCount=new Field<ulong>();
  public NavigationCrowdAdapterState Outer;
  public NavigationCrowdManager Crowd;
  public NavigationMoveRegistry Registry;
  public NavigationCrowdAgent NewAgent;
  public NavigationMoveRegistryNode NewNode;
  public NavigationCrowdGroup Group;
  public NavigationRegistrationResult PublicationResult;
  public int HashedBytes,VisitedNodes,OwnNodesFreed;
  public bool LookupReturned,AlreadyRegistered,NodeLinked,MapPublicationReturned,CreatedReturned,GroupReturned,Joined,Returned;
  public NavigationRegistrationInvocation(){Reset();}
  internal void Reset(){Native.At=0;Lookup=new NavigationMoveObjectInvocation();StoreAddresses.Clear();StoreWidths.Clear();Key.Forget();Hash.Forget();Bucket.Forget();LoadRatio.Forget();RequestedBucketCount.Forget();Outer=null;Crowd=null;Registry=null;NewAgent=null;NewNode=null;Group=null;PublicationResult=null;HashedBytes=VisitedNodes=OwnNodesFreed=0;LookupReturned=AlreadyRegistered=NodeLinked=MapPublicationReturned=CreatedReturned=GroupReturned=Joined=Returned=false;}
  internal void Store(uint at,int bytes){Native.At=at;StoreAddresses.Add(at);StoreWidths.Add(bytes);}
 }
 // Evidence:151020/25AE20 saved in r1-crowd-{adapter,vector}-registration;
 // 05DF10/01FB90 in r1-registration-publication-run1;020960 in
 // object-lifecycle/rea-container-support. Engine2DC390->2DDB3E->302970
 // is the independently pinned Engine CRT free import, not Game's import.
 // OWN support: stable single-thread typed identities, managed normal
 // allocations, fresh caller-local result/pair disjoint from heap fields.
 // No native heap/OOM, raw pointer wraparound, arbitrary byte aliases or EH.
 public static class NavigationRegistrationContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own registration view");var live=value as NavigationView;if(live!=null)live.RequireLive(at);return value;}
  static NavigationMoveRegistryNode Node(NavigationMoveRegistryNode node,uint at){return Need(node,at);}
  static NavigationMoveRegistry Map(NavigationMoveRegistry map,uint at){Need(map,at).Manager.RequireLive(at);return map;}
  static void NodeStore(Field<NavigationMoveRegistryNode> cell,Field<NavigationMoveRegistryNode> value,uint at,NavigationRegistrationInvocation call){cell.CopyFrom(value);call.Store(at,8);}
  static void NodeStore(Field<NavigationMoveRegistryNode> cell,NavigationMoveRegistryNode value,uint at,NavigationRegistrationInvocation call){cell.Write(value);call.Store(at,8);}
  public static NavigationMoveRegistryNode CreateNode020960(Field<NavigationMoveRegistryNode> next,Field<NavigationMoveRegistryNode> previous,NavigationRegistrationContext context,NavigationRegistrationInvocation call){
   var i=Need(call,0x02096f).Native;var prevBits=Need(previous,0x02096f).Snapshot();var nextBits=Need(next,0x020972).Snapshot();
   i.At=0x020975;var node=Need(context,i.At).AllocateNode();call.NewNode=node;
   i.At=0x02097a;if(nextBits.Read(i.At)==null){nextBits.Write(node);prevBits.Write(node);}
   // Explicit normal OWN allocation is non-null; no native allocation success
   // or pointer-wrapping LEA branch is inferred from managed allocation.
   NodeStore(node.Next00,nextBits,0x02098a,call);NodeStore(node.Prev08,prevBits,0x020996,call);i.At=0x0209a3;return node;
  }
  public static NavigationMoveRegistryNode CreatePairNode05DF10(Field<NavigationMoveRegistryNode> next,Field<NavigationMoveRegistryNode> previous,NavigationRegistrationPair pair,NavigationRegistrationContext context,NavigationRegistrationInvocation call){
   var node=CreateNode020960(next,previous,context,call);var i=call.Native;
   i.At=0x05df27;var key=Need(pair,i.At).Key00.Snapshot();node.Key10.CopyFrom(key);call.Store(0x05df2a,8);
   i.At=0x05df2d;var agent=Need(pair,i.At).Agent08.Snapshot();node.Agent18.CopyFrom(agent);call.Store(0x05df31,8);i.At=0x05df3a;return node;
  }
  static NavigationMoveRegistryBucket Bucket(NavigationMoveRegistryBuckets buckets,ulong index,uint at){return Need(buckets,at).At(index,at);}
  public static void Publish01FB90(NavigationMoveRegistry map,NavigationRegistrationResult output,Field<ulong> key,NavigationMoveRegistryNode current,NavigationRegistrationContext context,NavigationRegistrationInvocation call){
   var i=Need(call,0x01fbb9).Native;i.At=0x01fbb9;ulong value=Need(key,i.At).Read(i.At),hash=0xcbf29ce484222325UL;
   // No intervening callback or write during these eight byte loads in the
   // supported single-thread domain. Equality below still rereads live key.
   for(int n=0;n<8;n++){hash=unchecked((hash^(byte)(value>>(n*8)))*0x100000001b3UL);call.HashedBytes++;}call.Hash.Write(hash);
   i.At=0x01fc2c;var maskBits=Map(map,i.At).Mask58.Snapshot();i.At=0x01fc30;ulong index=maskBits.Read(i.At)&hash;call.Bucket.Write(index);
   i.At=0x01fc37;var buckets=map.Buckets40.Snapshot();i.At=0x01fc3b;var bucket=Bucket(buckets.Read(i.At),index,i.At);var firstBits=bucket.First00.Snapshot();
   i.At=0x01fc3f;var first=firstBits.Read(i.At);Field<NavigationMoveRegistryNode> cursorBits;
   if(Object.ReferenceEquals(first,map.Sentinel30.Read(i.At))){i.At=0x01fc45;cursorBits=map.Sentinel30.Snapshot();}
   else{i.At=0x01fc4b;var last=Bucket(buckets.Read(i.At),index,i.At).Last08.Snapshot();i.At=0x01fc50;cursorBits=Node(last.Read(i.At),i.At).Next00.Snapshot();}
   i.At=0x01fc53;var cursor=cursorBits.Read(i.At);
   if(!Object.ReferenceEquals(cursor,first)){
    i.At=0x01fc58;ulong compare=Need(key,i.At).Read(i.At);var seen=new HashSet<NavigationMoveRegistryNode>();
    for(;;){i.At=0x01fc60;cursorBits=Node(cursor,i.At).Prev08.Snapshot();i.At=0x01fc64;cursor=cursorBits.Read(i.At);var at=Node(cursor,i.At);if(!seen.Add(at))throw new MissingContract(i.At,"Cyclic own reverse bucket traversal");call.VisitedNodes++;
     if(compare==at.Key10.Read(i.At)){
      i.At=0x01fc71;var prev=Node(current,i.At).Prev08.Snapshot();i.At=0x01fc75;var next=Node(current,i.At).Next00.Snapshot();
      i.At=0x01fc78;NodeStore(Node(prev.Read(i.At),i.At).Next00,next,i.At,call);
      i.At=0x01fc7b;next=Node(current,i.At).Next00.Snapshot();i.At=0x01fc7e;prev=Node(current,i.At).Prev08.Snapshot();
      i.At=0x01fc82;NodeStore(Node(next.Read(i.At),i.At).Prev08,prev,i.At,call);
      i.At=0x01fc86;map.Count38.Write(unchecked(map.Count38.Read(i.At)-1));call.Store(i.At,8);
      i.At=0x01fc8d;Need(context,i.At).FreeNode(current,i.At);call.OwnNodesFreed++;
      i.At=0x01fc92;Need(output,i.At).Node00.Write(cursor);call.Store(i.At,8);i.At=0x01fc95;output.Inserted08.Write(0);call.Store(i.At,1);i.At=0x01fd47;return;
     }
     i.At=0x01fc6a;if(Object.ReferenceEquals(cursor,first))break;
    }
   }
   i.At=0x01fca6;var oldNextBits=Node(current,i.At).Next00.Snapshot();
   i.At=0x01fca9;var oldNext=oldNextBits.Read(i.At);if(!Object.ReferenceEquals(cursor,oldNext)){
    i.At=0x01fcae;var previous=Node(current,i.At).Prev08.Snapshot();i.At=0x01fcb2;NodeStore(Node(previous.Read(i.At),i.At).Next00,oldNext,i.At,call);
    i.At=0x01fcb5;previous=Node(oldNext,i.At).Prev08.Snapshot();i.At=0x01fcb9;NodeStore(Node(previous.Read(i.At),i.At).Next00,cursor,i.At,call);
    i.At=0x01fcbc;previous=Node(cursor,i.At).Prev08.Snapshot();i.At=0x01fcc0;NodeStore(Node(previous.Read(i.At),i.At).Next00,current,i.At,call);
    i.At=0x01fcc3;var capturedPrevious=Node(cursor,i.At).Prev08.Snapshot();i.At=0x01fcc7;previous=Node(oldNext,i.At).Prev08.Snapshot();i.At=0x01fccb;NodeStore(Node(cursor,i.At).Prev08,previous,i.At,call);
    i.At=0x01fccf;previous=Node(current,i.At).Prev08.Snapshot();i.At=0x01fcd3;NodeStore(Node(oldNext,i.At).Prev08,previous,i.At,call);
    i.At=0x01fcd7;NodeStore(Node(current,i.At).Prev08,capturedPrevious,i.At,call);
   }
   i.At=0x01fcdb;var liveBuckets=map.Buckets40.Snapshot();i.At=0x01fce2;var liveBucket=Bucket(liveBuckets.Read(i.At),index,i.At);var liveFirst=liveBucket.First00.Snapshot();i.At=0x01fce5;
   if(Object.ReferenceEquals(liveFirst.Read(i.At),map.Sentinel30.Read(i.At))){
    NodeStore(liveBucket.First00,current,0x01fceb,call);i.At=0x01fcee;var latest=map.Buckets40.Snapshot();i.At=0x01fcf2;NodeStore(Bucket(latest.Read(i.At),index,i.At).Last08,current,i.At,call);
   }else if(Object.ReferenceEquals(liveFirst.Read(0x01fcf9),cursor))NodeStore(liveBucket.First00,current,0x01fcfe,call);
   else{
    i.At=0x01fd03;var last=liveBucket.Last08.Snapshot();i.At=0x01fd07;var next=Node(last.Read(i.At),i.At).Next00.Snapshot();i.At=0x01fd0a;NodeStore(liveBucket.Last08,next,i.At,call);
    i.At=0x01fd0e;if(!Object.ReferenceEquals(next.Read(i.At),current)){i.At=0x01fd13;var latest=map.Buckets40.Snapshot();i.At=0x01fd17;var latestBucket=Bucket(latest.Read(i.At),index,i.At);last=latestBucket.Last08.Snapshot();i.At=0x01fd1c;var previous=Node(last.Read(i.At),i.At).Prev08.Snapshot();i.At=0x01fd20;NodeStore(latestBucket.Last08,previous,i.At,call);}
   }
   i.At=0x01fd28;CheckGrowth0205F0(map,call);
   i.At=0x01fd2e;Need(output,i.At).Node00.Write(current);call.Store(i.At,8);i.At=0x01fd31;output.Inserted08.Write(1);call.Store(i.At,1);i.At=0x01fd47;
  }
  public static void CheckGrowth0205F0(NavigationMoveRegistry map,NavigationRegistrationInvocation call){
   var i=Need(call,0x0205f6).Native;i.At=0x0205f6;var countBits=Map(map,i.At).Count38.Snapshot();i.At=0x0205fd;var bucketBits=map.BucketCount60.Snapshot();
   // The native bias34BDC8 is only consumed for signed-negative uint64
   // inputs. This declared exact arithmetic domain never takes that arm and
   // does not invent the loaded bias or an MXCSR rounding mode.
   i.At=0x02060c;ulong count=countBits.Read(i.At);if(count>0x1000000UL)throw new MissingContract(i.At,"Count CVTSI2SS outside exact nonnegative24-bit OWN domain; no bias/rounding inference");
   i.At=0x02061d;ulong buckets=bucketBits.Read(i.At);if(buckets>0x1000000UL)throw new MissingContract(i.At,"Bucket CVTSI2SS outside exact nonnegative24-bit OWN domain");
   i.At=0x02062b;if(buckets==0||(buckets&(buckets-1))!=0)throw new MissingContract(i.At,"Exact normal DIVSS domain requires a nonzero power-of-two bucket count; no divide-by-zero or rounded result inferred");
   float ratio=(float)count/(float)buckets;call.LoadRatio.Write(ratio);i.At=0x02062f;float maximum=Scalar(map.MaxLoadFactor28.Read(i.At),i.At);
   if(ratio<=maximum){i.At=0x0206af;return;}
   ulong requested=buckets<512?unchecked(buckets*8):buckets<0xfffffffffffffffUL?unchecked(buckets*2):buckets;call.RequestedBucketCount.Write(requested);
   i.At=0x020659;throw new MissingContract(i.At,"01B4F0 bucket growth/rehash is unsupported; preceding node/list/bucket publication remains visible");
  }
  static float Scalar(float value,uint at){uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(value),0);if((bits&0x7f800000u)==0x7f800000u||((bits&0x7f800000u)==0&&(bits&0x7fffffu)!=0))throw new MissingContract(at,"Registration finite ordinary FP domain excludes nonfinite/subnormal values and unknown MXCSR");return value;}
  static float F(float value,uint at){return Scalar(value,at);}
  static int Cvt(float value,uint at){value=Scalar(value,at);if((double)value < -2147483648.0 || (double)value >= 2147483648.0)throw new MissingContract(at,"Registration CVTTSS2SI outside declared valid int32 range");return (int)value;}
  static void Bounds(NavigationCrowdAgent agent,NavigationCrowdStateBoundaries state,NavigationCrowdInt3 lower,NavigationCrowdInt3 upper,Invocation i){
   i.At=0x25af15;var radiusBits=agent.Radius10.Snapshot();i.At=0x25af1b;var xBits=agent.Position48.X.Snapshot();i.At=0x25af24;float radius=Scalar(radiusBits.Read(i.At),i.At),x=Scalar(xBits.Read(i.At),i.At);float lowX=F(x-radius,i.At);i.At=0x25af28;int lx=Cvt(lowX,i.At);lower.X.Write(lx);
   i.At=0x25af30;var yBits=agent.Position48.Y.Snapshot();i.At=0x25af36;float y=Scalar(yBits.Read(i.At),i.At);lower.Y.Write(Cvt(y,i.At));i.At=0x25af3e;var zBits=agent.Position48.Z.Snapshot();i.At=0x25af47;float z=Scalar(zBits.Read(i.At),i.At),lowZ=F(z-radius,i.At);i.At=0x25af4b;lower.Z.Write(Cvt(lowZ,i.At));
   i.At=0x25af53;float highX=F(x+radius,i.At);i.At=0x25af57;var halfBits=Need(state,i.At).Half34BADC.Snapshot();i.At=0x25af5f;float half=Scalar(halfBits.Read(i.At),i.At);highX=F(highX+half,i.At);upper.X.Write(Cvt(highX,0x25af63));
   i.At=0x25af6b;float highY=F(y+Scalar(agent.Height14.Read(i.At),i.At),i.At);highY=F(highY+half,0x25af71);upper.Y.Write(Cvt(highY,0x25af75));float highZ=F(z+radius,0x25af7d);highZ=F(highZ+half,0x25af81);upper.Z.Write(Cvt(highZ,0x25af85));
  }
  public static void Add25AE20(NavigationCrowdManager crowd,GameMovementInterfaceView originalCallback,NavigationRegistrationContext context,NavigationRegistrationInvocation call){
   var i=Need(call,0x25ae49).Native;call.Crowd=crowd;var map=NavigationMoveRegistry.ForManager(Need(crowd,0x25ae49));call.Registry=map;i.At=0x25ae49;var sentinel=map.Sentinel30.Snapshot();
   i.At=0x25ae59;var ctx=Need(context,i.At);call.Key.Write(ctx.Movement.Addresses.Address(originalCallback,i.At));var selected=NavigationMoveObjectContracts.Lookup05DAF0(map,call.Key,call.Lookup);call.LookupReturned=true;
   i.At=0x25ae5e;if(!Object.ReferenceEquals(selected,sentinel.Read(i.At))){
    call.AlreadyRegistered=true;i.At=0x25afaf;if(ctx.DisableAlreadyRegistered422B8C.Read(i.At)==0){i.At=0x25afbc;if(ctx.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25afcb;throw new MissingContract(i.At,"Already-in-simulation diagnostics not implemented; no synthetic assertion disable");}}
    i.At=0x25b08b;call.Returned=true;return;
   }
   i.At=0x25ae71;var agent=new NavigationCrowdAgent();call.NewAgent=agent;i.At=0x25ae84;NavigationCrowdStateContracts.InitializeAgent25E6C0(agent,Field<object>.Of(originalCallback),ctx.State,i);
   var pair=new NavigationRegistrationPair();pair.Key00.CopyFrom(call.Key);pair.Agent08.Write(agent);
   i.At=0x25ae96;var sentinelBits=map.Sentinel30.Snapshot();i.At=0x25ae9a;var firstBits=Node(sentinelBits.Read(i.At),i.At).Next00.Snapshot();i.At=0x25aea2;var first=firstBits.Read(i.At);var previous=Node(first,i.At).Prev08.Snapshot();
   i.At=0x25aea9;var node=CreatePairNode05DF10(Field<NavigationMoveRegistryNode>.Of(first),previous,pair,ctx,call);call.NewNode=node;
   i.At=0x25aeb1;var countBits=map.Count38.Snapshot();i.At=0x25aebf;ulong count=countBits.Read(i.At);if(unchecked(0x7fffffffffffffeUL-count)<1){i.At=0x25aecf;throw new MissingContract(i.At,"Native list length_error after node allocation; no rollback inferred");}
   map.Count38.Write(unchecked(count+1));call.Store(0x25aed9,8);NodeStore(Node(first,0x25aedd).Prev08,node,0x25aedd,call);i.At=0x25aee1;var latePrevious=node.Prev08.Snapshot();i.At=0x25aee5;NodeStore(Node(latePrevious.Read(i.At),i.At).Next00,node,i.At,call);call.NodeLinked=true;
   i.At=0x25aee8;var lateSentinel=map.Sentinel30.Snapshot();i.At=0x25aeec;var currentBits=Node(lateSentinel.Read(i.At),i.At).Next00.Snapshot();
   // Forming the caller's declared node+10 source view requires a known live
   // typed node. Unknown/raw-pointer LEA aliases stop at this OWN boundary;
   // this is not a claim that native LEA dereferences or checks node lifetime.
   i.At=0x25aeef;var current=currentBits.Read(i.At);var sourceKey=Node(current,i.At).Key10;var result=new NavigationRegistrationResult();call.PublicationResult=result;
   i.At=0x25aefc;Publish01FB90(map,result,sourceKey,current,ctx,call);call.MapPublicationReturned=true;
   // The caller ignores01FB90's pair/boolean and retains R14. In particular a
   // duplicate helper result must not silently replace this freshly made Agent.
   i.At=0x25af13;GameMovementCallbackContracts.InvokeCreatedCall25AF13(originalCallback,agent,ctx.Game,i);call.CreatedReturned=true;
   var lower=new NavigationCrowdInt3();var upper=new NavigationCrowdInt3();Bounds(agent,ctx.State,lower,upper,i);
   i.At=0x25af9a;var group=NavigationCrowdStateContracts.GetGroup25A250(crowd,lower,upper,ctx.State,i);call.Group=group;call.GroupReturned=true;i.At=0x25afa5;NavigationCrowdStateContracts.Join25C340(group,agent,ctx.State,i);call.Joined=true;i.At=0x25b08b;call.Returned=true;
  }
  public static void Add151020(NavigationMoveManagerView navigation,GameMovementCharacterState owner,NavigationRegistrationContext context,NavigationRegistrationInvocation call){
   Need(call,0x151020).Reset();var i=call.Native;
   try{i.At=0x151020;var outerBits=Need(navigation,i.At).Outer10.Snapshot();bool noOwner=owner==null;i.At=0x151031;var outer=Need(outerBits.Read(i.At),i.At);call.Outer=outer;var crowdBits=NavigationMoveOuterView.ForAdapter(outer).Crowd08.Snapshot();i.At=0x151039;Add25AE20(crowdBits.Read(0x25ae49),noOwner?null:owner.Interface38,context,call);}
   catch(MissingContract e){i.At=e.Address;throw;}
  }
 }
}
