using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace GrimDynasty.TaskItems {
 public sealed class EngineDynamicFactoryLimits {
  public readonly int MaxVisits,MaxDepth,MaxByteComparisons;
  public EngineDynamicFactoryLimits(int maxVisits,int maxDepth,int maxByteComparisons){
   MaxVisits=maxVisits;MaxDepth=maxDepth;MaxByteComparisons=maxByteComparisons;
  }
 }
 public sealed class EngineDynamicFactoryResult {
  public readonly string Status,Reason;
  public readonly object Instance;
  public readonly int VisitedNodes,CallbacksInvoked,ComparedBytes;
  public bool MayHaveCallbackSideEffects {get{return CallbacksInvoked!=0;}}
  internal EngineDynamicFactoryResult(string reason,object instance,int visits,int calls,int compared){
   Status=reason==null?"Known":"Unsupported";Reason=reason;Instance=instance;
   VisitedNodes=visits;CallbacksInvoked=calls;ComparedBytes=compared;
  }
 }
 // Saved Engine 274EC0 control flow over own shared records. Requires exclusive
 // own state except controlled synchronous callbacks; no concurrent mutation.
 // Name/count/storage/slots/query bytes are live. Factory identities and the
 // explicit identity->own callback map stay fixed during this call. Func<object>
 // is an own callback abstraction, NOT a recovered native constructor ABI.
 public static class EngineDynamicFactoryDispatch {
  public const int MaximumVisits=4096,MaximumDepth=128,MaximumByteComparisons=1048576,MaximumBindings=4096;
  sealed class IdentityComparer : IEqualityComparer<object> {
   public new bool Equals(object a,object b){return Object.ReferenceEquals(a,b);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  sealed class Run {
   internal readonly byte[] Query;
   internal readonly EngineDynamicFactoryLimits Limits;
   internal readonly Dictionary<object,Func<object>> Factories=new Dictionary<object,Func<object>>(new IdentityComparer());
   internal int Visits,Calls,Compared;
   internal Run(byte[] query,EngineDynamicFactoryLimits limits){Query=query;Limits=limits;}
   internal EngineDynamicFactoryResult Result(string reason,object instance){
    return new EngineDynamicFactoryResult(reason,instance,Visits,Calls,Compared);
   }
  }
  static string Matches(byte[] name,Run run,out bool matched){
   matched=false;
   // 274ED4 loads this visit's name pointer once. RBP preserves the same query
   // pointer throughout recursive calls; neither array is copied or normalized.
   for(int i=0;;i++){
    if(run.Compared>=run.Limits.MaxByteComparisons)return "ClassFactoryByteComparisonBudgetOutsideCoveredDomain";
    if(i>=run.Query.Length)return "ClassFactoryNameBytesIncomplete";
    byte queryByte=run.Query[i];
    if(name==null||i>=name.Length)return "ClassFactoryNameBytesIncomplete";
    byte nameByte=name[i];run.Compared++;
    if(queryByte!=nameByte)return null;
    if(nameByte==0){matched=true;return null;}
   }
  }
  static string Visit(EngineRootNode node,int depth,Run run,out object instance){
   instance=null;
   if(depth>run.Limits.MaxDepth)return "ClassFactoryDepthOutsideCoveredDomain";
   if(run.Visits>=run.Limits.MaxVisits)return "ClassFactoryVisitBudgetOutsideCoveredDomain";
   run.Visits++;
   if(node==null)return "UnknownClassInfoSnapshot"; // Existing result vocabulary; no snapshot is created.
   byte[] name=node.Name as byte[];
   bool matched;string reason=Matches(name,run,out matched);
   if(reason!=null)return reason;
   if(matched){
    Func<object> callback=null;
    if(node.FactoryIdentity!=null)run.Factories.TryGetValue(node.FactoryIdentity,out callback);
    if(callback==null)return "UnknownClassFactoryCallback";
    // 274F0F tail-jumps: even a null result ends this frame without children.
    run.Calls++;
    try{instance=callback();return null;}
    catch(Exception error){return "ClassFactoryCallbackThrew:"+error.GetType().FullName;}
   }
   // Count is known signed own state. Do not turn an unknown native count into 0.
   if(node.Count<=0)return null;
   int index=0;
   while(true){
    // 274F20/27 reload storage and fetch this indexed child BEFORE recursion.
    EngineRootChildStorage storage=node.Children;
    if(storage==null||storage.Slots==null||index>=storage.Slots.Length)return "UnknownClassFactoryChildSlot";
    EngineRootNode child=storage.Slots[index];
    reason=Visit(child,depth+1,run,out instance);
    if(reason!=null||instance!=null)return reason;
    // 274F35/37/3B: advance, then compare with the CURRENT signed parent count.
    // Our visit cap prevents reaching native 32-bit index/64-bit offset wrap.
    index=unchecked(index+1);
    if(index>=node.Count)return null;
   }
  }
  public static EngineDynamicFactoryResult Create(EngineRootNode root,byte[] requestedName,
   EngineFactoryBinding[] bindings,EngineDynamicFactoryLimits limits){
   if(limits==null||limits.MaxVisits<0||limits.MaxVisits>MaximumVisits||limits.MaxDepth<0||limits.MaxDepth>MaximumDepth||
      limits.MaxByteComparisons<0||limits.MaxByteComparisons>MaximumByteComparisons)
    return new EngineDynamicFactoryResult("ClassFactoryTraversalLimitsOutsideCoveredDomain",null,0,0,0);
   if(requestedName==null)return new EngineDynamicFactoryResult("UnknownRequestedClassName",null,0,0,0);
   var run=new Run(requestedName,limits);
   if(bindings!=null){
    if(bindings.Length>MaximumBindings)return run.Result("ClassFactoryBindingBudgetOutsideCoveredDomain",null);
    for(int i=0;i<bindings.Length;i++){
     EngineFactoryBinding binding=bindings[i];
     if(binding==null||binding.Identity==null)return run.Result("InvalidClassFactoryBinding",null);
     if(run.Factories.ContainsKey(binding.Identity))return run.Result("DuplicateClassFactoryBindingIdentity",null);
     run.Factories.Add(binding.Identity,binding.Callback);
    }
   }
   // No graph prewalk, name prevalidation, query clone, cycle rejection or memo.
   // Reentrant Create calls have independent counters. Callback code must itself
   // be bounded by its caller; this traversal cannot interrupt arbitrary code.
   object instance;string reason=Visit(root,1,run,out instance);
   return run.Result(reason,instance);
  }
 }
}
