using System;
namespace GrimDynasty.TaskItems {
 public sealed class ClassInfoSnapshot {
  readonly byte[] name;
  readonly ClassInfoSnapshot[] children;
  readonly Func<object> constructor;
  public readonly int? ChildCount;
  // Null fields mean unobserved metadata. A supplied callback returning null is
  // a known result, not an unknown callback or evidence of a native constructor.
  // Arrays are copied so callbacks cannot change the metadata being traversed.
  public ClassInfoSnapshot(byte[] rawName,int? childCount,ClassInfoSnapshot[] children,Func<object> constructor){
   name=rawName==null?null:(byte[])rawName.Clone();ChildCount=childCount;
   this.children=children==null?null:(ClassInfoSnapshot[])children.Clone();this.constructor=constructor;
  }
  internal Func<object> Constructor {get{return constructor;}}
  internal bool NameByte(int index,out byte value){value=0;if(name==null||index>=name.Length)return false;value=name[index];return true;}
  internal bool Child(int index,out ClassInfoSnapshot value){value=null;if(children==null||index>=children.Length)return false;value=children[index];return true;}
 }
 public sealed class ClassFactoryResult {
  public readonly string Status,Reason;
  public readonly object Instance;
  public readonly int VisitedNodes,CallbacksInvoked;
  // Callback side effects are unknown and are never rolled back, including on
  // Unsupported or an exception after a callback has started.
  public bool MayHaveCallbackSideEffects {get{return CallbacksInvoked!=0;}}
  internal ClassFactoryResult(string reason,object instance,int visited,int invoked){
   Status=reason==null?"Known":"Unsupported";Reason=reason;Instance=instance;VisitedNodes=visited;CallbacksInvoked=invoked;
  }
 }
 public static class ClassFactoryDispatch {
  // Coverage limits of this reconstruction, not native ClassInfo restrictions.
  public const int MaximumDepth=128,MaximumVisits=4096;
  sealed class Run {
   internal readonly byte[] Name;internal readonly int DepthLimit,VisitLimit;
   internal int Visited,Invoked;
   internal Run(byte[] name,int depth,int visits){Name=(byte[])name.Clone();DepthLimit=depth;VisitLimit=visits;}
  }
  static bool? Matches(ClassInfoSnapshot node,byte[] query){
   // Engine.dll 0x180274ee1: byte equality, ending at the first equal NUL.
   // Unknown bytes beyond an earlier mismatch or NUL are not required.
   for(int i=0;;i++){byte value;if(i>=query.Length||!node.NameByte(i,out value))return null;if(query[i]!=value)return false;if(value==0)return true;}
  }
  static string Visit(ClassInfoSnapshot node,int depth,Run run,out object instance){
   instance=null;
   if(depth>run.DepthLimit)return "ClassFactoryDepthOutsideCoveredDomain";
   if(run.Visited>=run.VisitLimit)return "ClassFactoryVisitBudgetOutsideCoveredDomain";
   run.Visited++;if(node==null)return "UnknownClassInfoSnapshot";
   bool? matched=Matches(node,run.Name);if(!matched.HasValue)return "ClassFactoryNameBytesIncomplete";
   if(matched.Value){
    if(node.Constructor==null)return "UnknownClassFactoryCallback";
    // The native matching branch tail-calls +0x28 and returns even on null.
    run.Invoked++;
    try{instance=node.Constructor();return null;}
    catch(Exception e){return "ClassFactoryCallbackThrew:"+e.GetType().FullName;}
   }
   if(!node.ChildCount.HasValue)return "UnknownClassFactoryChildCount";
   if(node.ChildCount.Value<=0)return null;
   for(int i=0;i<node.ChildCount.Value;i++){
    ClassInfoSnapshot child;if(!node.Child(i,out child))return "UnknownClassFactoryChildSlot";
    string reason=Visit(child,depth+1,run,out instance);
    if(reason!=null||instance!=null)return reason;
   }
   return null;
  }
  public static ClassFactoryResult Create(ClassInfoSnapshot root,byte[] requestedName,int visitBudget=MaximumVisits,int depthBudget=MaximumDepth){
   if(visitBudget<0||visitBudget>MaximumVisits||depthBudget<0||depthBudget>MaximumDepth)return new ClassFactoryResult("ClassFactoryTraversalLimitsOutsideCoveredDomain",null,0,0);
   if(requestedName==null)return new ClassFactoryResult("UnknownRequestedClassName",null,0,0);
   var run=new Run(requestedName,depthBudget,visitBudget);object instance;
   string reason=Visit(root,1,run,out instance);return new ClassFactoryResult(reason,instance,run.Visited,run.Invoked);
  }
 }
}
