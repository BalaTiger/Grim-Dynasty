using System;
namespace GrimDynasty.TaskItems {
 // Own mutable state, not native memory or an Engine root. The caller provides
 // exclusive access, valid stable node identities, and controlled own callbacks.
 public sealed class ClassRegistrationNode {
  public byte[] Name;
  public ClassRegistrationNode Parent;
  public int Count;
  public ClassRegistrationChildStorage Children;
  public readonly object CreatorIdentity;
  public ClassRegistrationNode(byte[] name,ClassRegistrationNode parent,int count,
   ClassRegistrationChildStorage children,object creatorIdentity){
   Name=name;Parent=parent;Count=count;Children=children;CreatorIdentity=creatorIdentity;
  }
 }
 public sealed class ClassRegistrationChildStorage {
  // Keep storage and slot identities. Null Slots means an unprovided span;
  // null entries are known pointer values and are copied without dereferencing.
  public readonly ClassRegistrationNode[] Slots;
  public ClassRegistrationChildStorage(ClassRegistrationNode[] slots){Slots=slots;}
 }
 public sealed class ClassRegistrationCallbacks {
  public readonly Func<byte[],byte[]> DuplicateName;
  // Allocate/Free supply the normal contracts of whole helpers 5DAF40/5DA5DC.
  // Their CRT allocation retry, failure handlers and native EH are not modeled.
  public readonly Func<ulong,ClassRegistrationChildStorage> Allocate;
  public readonly Action<ClassRegistrationChildStorage> Free;
  // Contract for the whole 5DAEB8 helper: a normal return is exactly 0 or -1.
  // This is an own callback, never a native function pointer invocation.
  public readonly Func<ulong,int> RegisterExit;
  public ClassRegistrationCallbacks(Func<byte[],byte[]> duplicateName,
   Func<ulong,ClassRegistrationChildStorage> allocate,Action<ClassRegistrationChildStorage> free,
   Func<ulong,int> registerExit){DuplicateName=duplicateName;Allocate=allocate;Free=free;RegisterExit=registerExit;}
 }
 public sealed class ClassRegistrationInitializationResult {
  public readonly ClassRegistrationNode Node;
  public readonly ulong ExitCallbackToken;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public string Stage {get;internal set;}
  public ClassRegistrationNode SelectedParent {get;internal set;}
  public ClassRegistrationChildStorage AllocatedStorage {get;internal set;}
  public ClassRegistrationChildStorage FreeArgument {get;internal set;}
  public ulong? RequestedBytes {get;internal set;}
  public int? AllocationCount {get;internal set;}
  public int? OwnExitRawReturn {get;internal set;}
  public int? ReturnValue {get;internal set;}
  public int CallbacksInvoked {get;internal set;}
  public int CopiedChildren {get;internal set;}
  public bool ParentAssigned {get;internal set;}
  public bool NameAssigned {get;internal set;}
  public bool ChildAppended {get;internal set;}
  public bool CountIncremented {get;internal set;}
  public bool FreeInvoked {get;internal set;}
  public bool FreeCompleted {get;internal set;}
  public bool StoragePublished {get;internal set;}
  public bool ExitInvoked {get;internal set;}
  public bool MayHaveEffects {get{return ParentAssigned||NameAssigned||CallbacksInvoked!=0||
   CopiedChildren!=0||ChildAppended||CountIncremented||StoragePublished;}}
  internal ClassRegistrationInitializationResult(ClassRegistrationNode node,ulong token){
   Node=node;ExitCallbackToken=token;Status="Unsupported";Stage="Input";
  }
 }
 public sealed class ClassRegistrationExitDecision {
  public readonly ulong DecodedValue,ReturnedCallback;
  public readonly bool UsesCrtAtexit;
  public readonly int OuterReturn;
  internal ClassRegistrationExitDecision(ulong decoded,ulong callback,int eax){
   DecodedValue=decoded;UsesCrtAtexit=decoded==ulong.MaxValue;
   ReturnedCallback=eax==0?callback:0;OuterReturn=ReturnedCallback==0?-1:0;
  }
 }
 public static class ClassRegistrationInitialization {
  // Preferred-VA identity tokens only; neither token is executed.
  public const ulong ItemExitCallbackToken=0x180604450UL,QuestItemExitCallbackToken=0x180605530UL;
  public const int MaximumChildren=65536; // Own coverage limit, not a native restriction.
  public static ulong AllocationSize(int count){
   int next=unchecked(count+1);
   ulong extended=unchecked((ulong)(long)next);
   return extended>ulong.MaxValue/8?ulong.MaxValue:unchecked(extended*8);
  }
  public static ClassRegistrationExitDecision EvaluateExit(ulong cookie,ulong encoded,
   ulong callbackToken,int rawRegistrationEax){
   // 5DAE68: XOR then ROR by cookie's low six bits. An observed raw CRT
   // EAX is supplied explicitly; no global initialization or CRT call occurs.
   int shift=(int)(cookie&63);ulong value=cookie^encoded;
   ulong decoded=shift==0?value:unchecked((value>>shift)|(value<<(64-shift)));
   return new ClassRegistrationExitDecision(decoded,callbackToken,rawRegistrationEax);
  }
  static ClassRegistrationInitializationResult Stop(ClassRegistrationInitializationResult r,string reason){
   r.Status="Unsupported";r.Reason=reason;return r;
  }
  public static ClassRegistrationInitializationResult ApplyItem(ClassRegistrationNode item,
   ClassRegistrationNode actor,ClassRegistrationCallbacks callbacks){return Apply(item,actor,true,callbacks);}
  public static ClassRegistrationInitializationResult ApplyQuestItem(ClassRegistrationNode quest,
   ClassRegistrationCallbacks callbacks){return Apply(quest,null,false,callbacks);}
  static ClassRegistrationInitializationResult Apply(ClassRegistrationNode node,ClassRegistrationNode actor,
   bool item,ClassRegistrationCallbacks callbacks){
   // Allocate the own journal before any modeled store. Later failures retain
   // all completed stores and callback effects; no native EH/OOM claim is made.
   var r=new ClassRegistrationInitializationResult(node,item?ItemExitCallbackToken:QuestItemExitCallbackToken);
   if(node==null)return Stop(r,"UnknownClassRegistrationNode");
   try{
    if(item){r.Stage="ParentAssign";node.Parent=actor;r.ParentAssigned=true;}
    r.Stage="DuplicateName";
    if(callbacks==null||callbacks.DuplicateName==null)return Stop(r,"UnknownDuplicateNameCallback");
    byte[] literal=item?new byte[]{73,116,101,109,0}:new byte[]{81,117,101,115,116,73,116,101,109,0};
    r.CallbacksInvoked++;byte[] duplicated=callbacks.DuplicateName(literal);
    // The writer reloads +10 after strdup and before writing +08, even when
    // strdup returns null. QuestItem does not assign a parent itself.
    r.Stage="ParentRead";ClassRegistrationNode parent=node.Parent;r.SelectedParent=parent;
    r.Stage="NameAssign";node.Name=duplicated;r.NameAssigned=true;
    if(parent!=null){
     r.Stage="Allocate";int allocationCount=parent.Count;r.AllocationCount=allocationCount;
     ulong requested=AllocationSize(allocationCount);r.RequestedBytes=requested;
     if(callbacks.Allocate==null)return Stop(r,"UnknownClassRegistrationAllocateCallback");
     r.CallbacksInvoked++;ClassRegistrationChildStorage allocated=callbacks.Allocate(requested);r.AllocatedStorage=allocated;
     if(allocated==null)return Stop(r,"UnsupportedAllocationContract");
     // Check only after the native-order prefix and allocation callback.
     // Native code rereads count and old storage after allocation.
     r.Stage="CopyChildren";
     if(parent.Count<0)return Stop(r,"ClassRegistrationNegativeCountOutsideCoveredDomain");
     if(parent.Count>=MaximumChildren)return Stop(r,"ClassRegistrationCountBudgetOutsideCoveredDomain");
     if(allocated.Slots==null)return Stop(r,"UnknownClassRegistrationAllocatedSpan");
     if(Object.ReferenceEquals(allocated,parent.Children)||(parent.Children!=null&&
       Object.ReferenceEquals(allocated.Slots,parent.Children.Slots)))
      return Stop(r,"ClassRegistrationAllocationAliasOutsideCoveredDomain");
     for(int i=0;i<parent.Count;i++){
      ClassRegistrationChildStorage old=parent.Children;
      if(old==null||old.Slots==null||i>=old.Slots.Length)return Stop(r,"UnknownClassRegistrationOldChildSlot");
      if(i>=allocated.Slots.Length)return Stop(r,"ClassRegistrationAllocatedSpanOutsideCoveredDomain");
      allocated.Slots[i]=old.Slots[i];r.CopiedChildren++;
     }
     r.Stage="AppendChild";int index=parent.Count;
     if(index<0||index>=MaximumChildren||index>=allocated.Slots.Length)
      return Stop(r,"ClassRegistrationAppendOutsideCoveredDomain");
     allocated.Slots[index]=node;r.ChildAppended=true;
     r.Stage="IncrementCount";parent.Count=unchecked(parent.Count+1);r.CountIncremented=true;
     r.Stage="FreeOldStorage";r.FreeArgument=parent.Children;
     if(callbacks.Free==null)return Stop(r,"UnknownClassRegistrationFreeCallback");
     r.FreeInvoked=true;r.CallbacksInvoked++;callbacks.Free(r.FreeArgument);r.FreeCompleted=true;
     r.Stage="PublishChildren";parent.Children=allocated;r.StoragePublished=true;
    }
    // Null parent still reaches the tail helper.
    r.Stage="RegisterExit";
    if(callbacks.RegisterExit==null)return Stop(r,"UnknownClassRegistrationExitCallback");
    r.ExitInvoked=true;r.CallbacksInvoked++;int exit=callbacks.RegisterExit(r.ExitCallbackToken);r.OwnExitRawReturn=exit;
    if(exit!=0&&exit!=-1)return Stop(r,"UnsupportedExitRegistrationContract");
    r.ReturnValue=exit;r.Status="Known";r.Stage="Complete";return r;
   }catch(Exception e){
    // Own callback fault injection/interruption only. In particular Free
    // throwing does not assert a recoverable native free/EH contract.
    return Stop(r,"OwnClassRegistrationOperationThrew:"+e.GetType().FullName);
   }
  }
 }
}
