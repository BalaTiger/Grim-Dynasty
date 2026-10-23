using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace GrimDynasty.TaskItems {
 public sealed class EngineFactoryBinding {
  public readonly object Identity;
  public readonly Func<object> Callback;
  public EngineFactoryBinding(object identity,Func<object> callback){Identity=identity;Callback=callback;}
 }
 public sealed class EngineSnapshotLimits {
  public readonly int MaxNodes,MaxEdges,MaxNameBytes,MaxDepth;
  public EngineSnapshotLimits(int maxNodes,int maxEdges,int maxNameBytes,int maxDepth){
   MaxNodes=maxNodes;MaxEdges=maxEdges;MaxNameBytes=maxNameBytes;MaxDepth=maxDepth;
  }
 }
 public sealed class EngineSnapshotResult {
  public readonly string Status,Reason;
  public readonly ClassInfoSnapshot Root;
  // Admitted capture resources; a failed capture may report a partial prefix.
  // Edges count stored slots once per distinct node, not dispatcher visits.
  public readonly int Nodes,Edges,NameBytes;
  public readonly bool HasUnknownMetadata;
  internal EngineSnapshotResult(string reason,ClassInfoSnapshot root,int nodes,int edges,int bytes,bool unknown){
   Status=reason==null?"Captured":"Unsupported";Reason=reason;Root=reason==null?root:null;
   Nodes=nodes;Edges=edges;NameBytes=bytes;HasUnknownMetadata=unknown;
  }
 }
 // Own adapter, NOT an inferred native registration operation. Caller supplies
 // exclusive, stable own state. No factory callback runs while capturing.
 // A snapshot fixes metadata: factory mutations do not affect subsequent traversal.
 // It does not model the native helper's live child-count reload after callbacks.
 public static class EngineRootSnapshot {
  public const int MaximumNodes=4096,MaximumEdges=16384,MaximumNameBytes=1048576,MaximumDepth=128,MaximumBindings=4096;
  sealed class IdentityComparer : IEqualityComparer<object> {
   public new bool Equals(object a,object b){return Object.ReferenceEquals(a,b);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  sealed class CapturedNode {
   internal readonly ClassInfoSnapshot Snapshot;internal readonly int Height;
   internal CapturedNode(ClassInfoSnapshot snapshot,int height){Snapshot=snapshot;Height=height;}
  }
  sealed class CaptureRun {
   internal readonly EngineSnapshotLimits Limits;
   internal readonly Dictionary<object,Func<object>> Bindings=new Dictionary<object,Func<object>>(new IdentityComparer());
   internal readonly Dictionary<EngineRootNode,CapturedNode> Complete=new Dictionary<EngineRootNode,CapturedNode>();
   internal readonly Dictionary<EngineRootNode,bool> Active=new Dictionary<EngineRootNode,bool>();
   internal int Nodes,Edges,NameBytes;internal bool Unknown;internal string Failure;
   internal CaptureRun(EngineSnapshotLimits limits){Limits=limits;}
   internal CapturedNode Fail(string reason){Failure=reason;return null;}
   internal CapturedNode Visit(EngineRootNode node,int depth) {
    if(node==null){Unknown=true;return new CapturedNode(null,0);}
    if(depth>Limits.MaxDepth)return Fail("CaptureDepthBudget");
    if(Active.ContainsKey(node))return Fail("CaptureChildCycle");
    CapturedNode previous;
    if(Complete.TryGetValue(node,out previous)) {
     // A shared subtree may be reached through a later, deeper path.
     if(previous.Height>Limits.MaxDepth-depth+1)return Fail("CaptureDepthBudget");
     return previous;
    }
    if(Nodes>=Limits.MaxNodes)return Fail("CaptureNodeBudget");
    Nodes++;Active.Add(node,true);
    byte[] name=node.Name as byte[];
    if(name==null)Unknown=true;
    else {
     if(name.Length>Limits.MaxNameBytes-NameBytes)return Fail("CaptureNameByteBudget");
     NameBytes+=name.Length;
     bool nul=false;for(int i=0;i<name.Length;i++)if(name[i]==0){nul=true;break;}
     if(!nul)Unknown=true; // Keep exact bytes; an early mismatch may still be known.
    }
    Func<object> factory=null;
    if(node.FactoryIdentity!=null)Bindings.TryGetValue(node.FactoryIdentity,out factory);
    if(factory==null)Unknown=true;
    int count=node.Count,height=1;
    ClassInfoSnapshot[] children=null;
    if(count>0) {
     EngineRootChildStorage storage=node.Children;
     EngineRootNode[] slots=storage==null?null:storage.Slots;
     if(slots==null)Unknown=true;
     else {
      int length=Math.Min(count,slots.Length);
      if(length<count)Unknown=true;
      if(length>Limits.MaxEdges-Edges)return Fail("CaptureEdgeBudget");
      Edges+=length;children=new ClassInfoSnapshot[length];
      for(int i=0;i<length;i++) {
       CapturedNode child=Visit(slots[i],depth+1);
       if(child==null)return null;
       children[i]=child.Snapshot;
       if(child.Height+1>height)height=child.Height+1;
      }
     }
    }
    // ClassInfoSnapshot clones both arrays. No padding, reordering, deduplication,
    // NUL insertion, encoding conversion, parent traversal, or repair from a
    // writer result's unpublished AllocatedStorage takes place here.
    var captured=new CapturedNode(new ClassInfoSnapshot(name,count,children,factory),height);
    Active.Remove(node);Complete.Add(node,captured);return captured;
   }
  }
  public static EngineSnapshotResult Capture(EngineRootNode root,EngineFactoryBinding[] bindings,EngineSnapshotLimits limits) {
   if(limits==null||limits.MaxNodes<0||limits.MaxNodes>MaximumNodes||limits.MaxEdges<0||limits.MaxEdges>MaximumEdges||
      limits.MaxNameBytes<0||limits.MaxNameBytes>MaximumNameBytes||limits.MaxDepth<0||limits.MaxDepth>MaximumDepth)
    return new EngineSnapshotResult("CaptureLimitsOutsideCoveredDomain",null,0,0,0,false);
   var run=new CaptureRun(limits);
   if(bindings!=null) {
    if(bindings.Length>MaximumBindings)return new EngineSnapshotResult("CaptureBindingBudget",null,0,0,0,false);
    for(int i=0;i<bindings.Length;i++) {
     EngineFactoryBinding binding=bindings[i];
     if(binding==null||binding.Identity==null)return new EngineSnapshotResult("CaptureInvalidBinding",null,0,0,0,false);
     if(run.Bindings.ContainsKey(binding.Identity))return new EngineSnapshotResult("CaptureDuplicateBindingIdentity",null,0,0,0,false);
     run.Bindings.Add(binding.Identity,binding.Callback);
    }
   }
   CapturedNode value=run.Visit(root,1);
   return new EngineSnapshotResult(run.Failure,value==null?null:value.Snapshot,run.Nodes,run.Edges,run.NameBytes,run.Unknown);
  }
 }
}
