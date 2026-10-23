using System;
namespace GrimDynasty.TaskItems {
 // Game whole-helper contracts over the SAME own records used by Engine writers.
 // No conversion, proxy-node creation, copy-back or post-success synchronization.
 public sealed class SharedClassRegistrationCallbacks {
  public readonly Func<byte[],byte[]> DuplicateName;
  public readonly Func<ulong,EngineRootChildStorage> Allocate;
  public readonly Action<EngineRootChildStorage> Free;
  public readonly Func<ulong,int> RegisterExit;
  public SharedClassRegistrationCallbacks(Func<byte[],byte[]> duplicateName,
   Func<ulong,EngineRootChildStorage> allocate,Action<EngineRootChildStorage> free,
   Func<ulong,int> registerExit){DuplicateName=duplicateName;Allocate=allocate;Free=free;RegisterExit=registerExit;}
 }
 public sealed class SharedClassRegistrationResult {
  public readonly EngineRootNode Node;
  public readonly ulong ExitCallbackToken;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public string Stage {get;internal set;}
  public EngineRootNode SelectedParent {get;internal set;}
  public EngineRootChildStorage AllocatedStorage {get;internal set;}
  public EngineRootChildStorage FreeArgument {get;internal set;}
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
  internal SharedClassRegistrationResult(EngineRootNode node,ulong token){
   Node=node;ExitCallbackToken=token;Status="Unsupported";Stage="Input";
  }
 }
 // Bounded Game Item/QuestItem writers; EngineRootNode is an own common state
 // type here, not a claim that a Game address belongs to Engine.dll. Callers
 // explicitly retain module/record identity, parents and controlled callbacks.
 // Requires exclusive own state; callbacks may synchronously mutate these same
 // nodes. Nothing builds a default root graph or infers native callback order.
 public static class SharedClassRegistration {
  static SharedClassRegistrationResult Stop(SharedClassRegistrationResult result,string reason){
   result.Status="Unsupported";result.Reason=reason;return result;
  }
  public static SharedClassRegistrationResult ApplyItem(EngineRootNode item,
   EngineRootNode actor,SharedClassRegistrationCallbacks callbacks){return Apply(item,actor,true,callbacks);}
  public static SharedClassRegistrationResult ApplyQuestItem(EngineRootNode quest,
   SharedClassRegistrationCallbacks callbacks){return Apply(quest,null,false,callbacks);}
  static SharedClassRegistrationResult Apply(EngineRootNode node,EngineRootNode actor,
   bool item,SharedClassRegistrationCallbacks callbacks){
   var r=new SharedClassRegistrationResult(node,item?ClassRegistrationInitialization.ItemExitCallbackToken:
    ClassRegistrationInitialization.QuestItemExitCallbackToken);
   if(node==null)return Stop(r,"UnknownClassRegistrationNode");
   try{
    // Game 5F04 writes the imported Actor record before calling strdup.
    // QuestItem's writer does not assign its parent.
    if(item){r.Stage="ParentAssign";node.Parent=actor;r.ParentAssigned=true;}
    r.Stage="DuplicateName";
    if(callbacks==null||callbacks.DuplicateName==null)return Stop(r,"UnknownDuplicateNameCallback");
    byte[] literal=item?new byte[]{73,116,101,109,0}:new byte[]{81,117,101,115,116,73,116,101,109,0};
    r.CallbacksInvoked++;byte[] duplicated=callbacks.DuplicateName(literal);
    // Reload after the callback, then keep this selected parent local, even if
    // later callbacks change node.Parent. Store the exact returned name or null.
    r.Stage="ParentRead";EngineRootNode parent=node.Parent;r.SelectedParent=parent;
    r.Stage="NameAssign";node.Name=duplicated;r.NameAssigned=true;
    if(parent!=null){
     r.Stage="Allocate";int allocationCount=parent.Count;r.AllocationCount=allocationCount;
     ulong requested=ClassRegistrationInitialization.AllocationSize(allocationCount);r.RequestedBytes=requested;
     if(callbacks.Allocate==null)return Stop(r,"UnknownClassRegistrationAllocateCallback");
     // Whole Game helper 5DAF40, not an implicit Engine allocator or retry loop.
     r.CallbacksInvoked++;EngineRootChildStorage allocated=callbacks.Allocate(requested);r.AllocatedStorage=allocated;
     if(allocated==null)return Stop(r,"UnsupportedAllocationContract");
     r.Stage="CopyChildren";
     // Own span/alias guards match the existing Game model's covered domain.
     // They occur AFTER name and allocation effects and are not native checks.
     if(parent.Count<0)return Stop(r,"ClassRegistrationNegativeCountOutsideCoveredDomain");
     if(parent.Count>=ClassRegistrationInitialization.MaximumChildren)return Stop(r,"ClassRegistrationCountBudgetOutsideCoveredDomain");
     if(allocated.Slots==null)return Stop(r,"UnknownClassRegistrationAllocatedSpan");
     if(Object.ReferenceEquals(allocated,parent.Children)||(parent.Children!=null&&
       Object.ReferenceEquals(allocated.Slots,parent.Children.Slots)))
      return Stop(r,"ClassRegistrationAllocationAliasOutsideCoveredDomain");
     for(int i=0;i<parent.Count;i++){
      EngineRootChildStorage old=parent.Children;
      if(old==null||old.Slots==null||i>=old.Slots.Length)return Stop(r,"UnknownClassRegistrationOldChildSlot");
      if(i>=allocated.Slots.Length)return Stop(r,"ClassRegistrationAllocatedSpanOutsideCoveredDomain");
      allocated.Slots[i]=old.Slots[i];r.CopiedChildren++;
     }
     r.Stage="AppendChild";int index=parent.Count;
     if(index<0||index>=ClassRegistrationInitialization.MaximumChildren||index>=allocated.Slots.Length)
      return Stop(r,"ClassRegistrationAppendOutsideCoveredDomain");
     allocated.Slots[index]=node;r.ChildAppended=true;
     r.Stage="IncrementCount";parent.Count=unchecked(parent.Count+1);r.CountIncremented=true;
     r.Stage="FreeOldStorage";r.FreeArgument=parent.Children;
     if(callbacks.Free==null)return Stop(r,"UnknownClassRegistrationFreeCallback");
     // Game 5DA5DC whole free contract; no boolean success is invented.
     r.FreeInvoked=true;r.CallbacksInvoked++;callbacks.Free(r.FreeArgument);r.FreeCompleted=true;
     r.Stage="PublishChildren";parent.Children=allocated;r.StoragePublished=true;
    }
    // Game 5DAEB8 whole helper returns 0/-1. This token is not a factory.
    r.Stage="RegisterExit";
    if(callbacks.RegisterExit==null)return Stop(r,"UnknownClassRegistrationExitCallback");
    r.ExitInvoked=true;r.CallbacksInvoked++;int exit=callbacks.RegisterExit(r.ExitCallbackToken);r.OwnExitRawReturn=exit;
    if(exit!=0&&exit!=-1)return Stop(r,"UnsupportedExitRegistrationContract");
    r.ReturnValue=exit;r.Status="Known";r.Stage="Complete";return r;
   }catch(Exception error){
    // Own callback interruption only, not recovered native EH or recoverable
    // free failure. Keep all completed stores and nested callback side effects.
    return Stop(r,"OwnClassRegistrationOperationThrew:"+error.GetType().FullName);
   }
  }
 }
}
