using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GrimDynasty.TaskItems {
 // Own state and dependency outcomes only. No native address is invoked.
 public enum EngineRootWriter { Actor, Entity, Object }
 public enum EngineModelStatus { Completed, Unknown, NonlocalExit, Unsupported, ResourceLimit, OwnFault, TrapContinuation }
 public enum EngineDependencyKind { Normal, Unknown, NonlocalExit }
 public enum EngineUnit { Value }

 public sealed class EngineDependencyResult<T> {
  public readonly EngineDependencyKind Kind;
  public readonly T Value;
  public readonly string Reason;
  private EngineDependencyResult(EngineDependencyKind kind,T value,string reason){Kind=kind;Value=value;Reason=reason;}
  public static EngineDependencyResult<T> Normal(T value){return new EngineDependencyResult<T>(EngineDependencyKind.Normal,value,null);}
  public static EngineDependencyResult<T> Unknown(string reason){return new EngineDependencyResult<T>(EngineDependencyKind.Unknown,default(T),reason);}
  public static EngineDependencyResult<T> Nonlocal(string reason){return new EngineDependencyResult<T>(EngineDependencyKind.NonlocalExit,default(T),reason);}
 }
 public sealed class EngineRootNode {
  public object Name;
  public EngineRootNode Parent;
  public int Count;
  public EngineRootChildStorage Children;
  public readonly object FactoryIdentity;
  public EngineRootNode(object name,EngineRootNode parent,int count,EngineRootChildStorage children,object factoryIdentity){
   Name=name;Parent=parent;Count=count;Children=children;FactoryIdentity=factoryIdentity;
  }
 }
 public sealed class EngineRootChildStorage {
  // Null Slots is an unprovided span; null elements are known null child tokens.
  public readonly EngineRootNode[] Slots;
  public EngineRootChildStorage(EngineRootNode[] slots){Slots=slots;}
 }
 public sealed class EngineExitState {
  public readonly object Identity;
  public ulong? Cookie,Encoded;
  public EngineExitState(object identity,ulong? cookie,ulong? encoded){Identity=identity;Cookie=cookie;Encoded=encoded;}
 }
 public sealed class EngineModelLimits {
  public readonly int MaxSteps,MaxSlots;
  public EngineModelLimits(int maxSteps,int maxSlots){MaxSteps=maxSteps;MaxSlots=maxSlots;}
 }
 public sealed class EngineRootWriterSpec {
  public readonly uint WriterRva,NodeRva;
  public readonly ulong NameSourceToken,ExitCallbackToken;
  internal EngineRootWriterSpec(uint writer,uint node,ulong name,ulong callback){
   WriterRva=writer;NodeRva=node;NameSourceToken=name;ExitCallbackToken=callback;
  }
 }
 public sealed class EngineThrowFieldWrite {
  public readonly int Offset;
  public readonly ulong Value;
  internal EngineThrowFieldWrite(int offset,ulong value){Offset=offset;Value=value;}
 }
 public sealed class EngineThrowRequest {
  public readonly uint WrapperRva,ConstructorRva;
  public readonly ulong DescriptorToken;
  // Describes the observed constructor. Result.ThrowFieldsStored/Trace record
  // how much of it the bounded model actually reached.
  public readonly ReadOnlyCollection<EngineThrowFieldWrite> Writes;
  internal EngineThrowRequest(bool maximumSize){
   WrapperRva=maximumSize?0x2DD600U:0x2DD5E0U;
   ConstructorRva=maximumSize?0x2DD568U:0x2DD508U;
   DescriptorToken=maximumSize?0x1803AF5B8UL:0x1803AF558UL;
   Writes=Array.AsReadOnly(new EngineThrowFieldWrite[]{
    new EngineThrowFieldWrite(0x10,0),
    new EngineThrowFieldWrite(0x08,maximumSize?0x1803074B0UL:0x180307488UL),
    new EngineThrowFieldWrite(0x00,maximumSize?0x1803074A0UL:0x180307478UL)});
  }
 }
 public sealed class EngineRootDependencies {
  public Func<ulong,EngineDependencyResult<object>> DuplicateName;
  public Func<ulong,EngineDependencyResult<EngineRootChildStorage>> Malloc;
  public Func<ulong,EngineDependencyResult<int>> NewHandler;
  public Func<EngineThrowRequest,EngineDependencyResult<EngineUnit>> Throw;
  public Func<EngineRootChildStorage,EngineDependencyResult<EngineUnit>> Free;
  public Func<ulong,EngineDependencyResult<int>> CrtAtExit;
  public Func<EngineExitState,ulong,EngineDependencyResult<int>> RegisterOnExit;
 }
 public sealed class EngineRootEvent {
  public readonly string Stage,Kind;
  public readonly ulong? Argument;
  public readonly object Subject;
  public readonly int? Detail;
  internal EngineRootEvent(string stage,string kind,ulong? argument,object subject,int? detail){
   Stage=stage;Kind=kind;Argument=argument;Subject=subject;Detail=detail;
  }
 }
 public sealed class EngineRootResult {
  public EngineModelStatus Status {get;internal set;}
  public string Stage {get;internal set;}
  public string Reason {get;internal set;}
  public EngineRootNode SelectedParent {get;internal set;}
  public EngineRootChildStorage AllocatedStorage {get;internal set;}
  public EngineRootChildStorage FreeArgument {get;internal set;}
  public ulong? RequestedBytes {get;internal set;}
  public int? AllocationCount {get;internal set;}
  public bool NameStored {get;internal set;}
  public bool ChildAppended {get;internal set;}
  public bool CountIncremented {get;internal set;}
  public bool FreeInvoked {get;internal set;}
  public bool FreeCompleted {get;internal set;}
  public bool StoragePublished {get;internal set;}
  public bool ExitInvoked {get;internal set;}
  public int CopiedChildren {get;internal set;}
  public int MallocCalls {get;internal set;}
  public int NewHandlerCalls {get;internal set;}
  public int ThrowFieldsStored {get;internal set;}
  public EngineThrowRequest ThrowRequest {get;internal set;}
  public ulong? DecodedExitValue {get;internal set;}
  public bool? UsesCrtAtExit {get;internal set;}
  public int? RawExitEax {get;internal set;}
  public ulong? ReturnedCallback {get;internal set;}
  public int? ReturnValue {get;internal set;}
  public ulong? ReturnRax {get;internal set;}
  private readonly List<EngineRootEvent> events=new List<EngineRootEvent>();
  // Semantic progress events, not a native instruction/memory-access trace.
  public ReadOnlyCollection<EngineRootEvent> Trace {get{return events.AsReadOnly();}}
  internal void Add(EngineRootEvent item){events.Add(item);}
  internal EngineRootResult(){Status=EngineModelStatus.Unknown;Stage="Input";}
 }

 public static class EngineRootRegistration {
  // Preferred-VA tokens for unread strings/callbacks, not runnable pointers.
  private static readonly EngineRootWriterSpec actor=new EngineRootWriterSpec(0x5C70,0x41CFC0,0x180309E64UL,0x180300F90UL);
  private static readonly EngineRootWriterSpec entity=new EngineRootWriterSpec(0x61F0,0x41CF30,0x180309AC4UL,0x180301110UL);
  private static readonly EngineRootWriterSpec rootObject=new EngineRootWriterSpec(0x7270,0x41CF00,0x180309AECUL,0x180301590UL);
  public static EngineRootWriterSpec GetWriterSpec(EngineRootWriter writer){
   switch(writer){case EngineRootWriter.Actor:return actor;case EngineRootWriter.Entity:return entity;case EngineRootWriter.Object:return rootObject;default:throw new ArgumentOutOfRangeException("writer");}
  }
  public static ulong AllocationSize(int count){
   int next=unchecked(count+1);
   ulong extended=unchecked((ulong)(long)next);
   return extended>ulong.MaxValue/8?ulong.MaxValue:unchecked(extended*8);
  }
  public static ulong DecodeSelector(ulong cookie,ulong encoded){
   ulong value=cookie^encoded;int shift=(int)(cookie&63);
   return shift==0?value:unchecked((value>>shift)|(value<<(64-shift)));
  }
  private sealed class Context {
   internal readonly EngineRootResult Result;
   internal readonly EngineModelLimits Limits;
   private int steps;
   internal Context(EngineRootResult result,EngineModelLimits limits){Result=result;Limits=limits;}
   internal bool Stop(EngineModelStatus status,string reason){Result.Status=status;Result.Reason=reason;return false;}
   internal bool Step(string stage,string kind,ulong? argument,object subject,int? detail){
    Result.Stage=stage;
    if(steps>=Limits.MaxSteps)return Stop(EngineModelStatus.ResourceLimit,"OwnStepLimit");
    steps++;Result.Add(new EngineRootEvent(stage,kind,argument,subject,detail));return true;
   }
   internal bool Accept<T>(EngineDependencyResult<T> outcome,out T value){
    value=default(T);
    if(outcome==null)return Stop(EngineModelStatus.Unknown,"MissingDependencyOutcome");
    if(outcome.Kind==EngineDependencyKind.Unknown)return Stop(EngineModelStatus.Unknown,outcome.Reason);
    if(outcome.Kind==EngineDependencyKind.NonlocalExit)return Stop(EngineModelStatus.NonlocalExit,outcome.Reason);
    value=outcome.Value;return true;
   }
  }
  private static EngineRootResult Run(EngineModelLimits limits,Action<Context> body){
   var result=new EngineRootResult();
   // These are explicit own coverage limits, not recovered native restrictions.
   if(limits==null||limits.MaxSteps<0||limits.MaxSteps>100000||limits.MaxSlots<0||limits.MaxSlots>65536){
    result.Status=EngineModelStatus.Unsupported;result.Reason="InvalidOwnLimits";return result;
   }
   var context=new Context(result,limits);
   try{body(context);}catch(Exception ex){
    context.Stop(EngineModelStatus.OwnFault,"OwnDependencyOrModelException:"+ex.GetType().FullName);
   }
   return result;
  }
  private static void Complete(Context c){c.Result.Status=EngineModelStatus.Completed;c.Result.Stage="Complete";}
  public static EngineRootResult Allocate(ulong size,EngineRootDependencies deps,EngineModelLimits limits){
   return Run(limits,delegate(Context c){if(AllocateCore(c,size,deps))Complete(c);});
  }
  private static bool AllocateCore(Context c,ulong size,EngineRootDependencies deps){
   EngineRootResult r=c.Result;r.RequestedBytes=size;
   while(true){
    r.Stage="Malloc";
    if(deps==null||deps.Malloc==null)return c.Stop(EngineModelStatus.Unknown,"MissingMalloc");
    if(!c.Step("Malloc","MallocCall",size,null,null))return false;
    r.MallocCalls++;
    EngineRootChildStorage storage;
    if(!c.Accept(deps.Malloc(size),out storage))return false;
    if(storage!=null){r.AllocatedStorage=storage;return true;}
    r.Stage="NewHandler";
    if(deps.NewHandler==null)return c.Stop(EngineModelStatus.Unknown,"MissingNewHandler");
    if(!c.Step("NewHandler","NewHandlerCall",size,null,null))return false;
    r.NewHandlerCalls++;
    int handler;
    if(!c.Accept(deps.NewHandler(size),out handler))return false;
    if(handler!=0)continue; // Full EAX; negative/non-AL nonzero values retry.
    EngineThrowRequest request=new EngineThrowRequest(size==ulong.MaxValue);r.ThrowRequest=request;
    foreach(EngineThrowFieldWrite write in request.Writes){
     if(!c.Step("ThrowConstruction","ThrowFieldStore",write.Value,request,write.Offset))return false;
     r.ThrowFieldsStored++;
    }
    r.Stage="Throw";
    if(deps.Throw==null)return c.Stop(EngineModelStatus.Unknown,"MissingThrowDependency");
    if(!c.Step("Throw","ThrowCall",request.DescriptorToken,request,null))return false;
    EngineUnit ignored;
    if(!c.Accept(deps.Throw(request),out ignored))return false;
    // The actual CALL continuation is INT3, never a null return/retry.
    r.Stage="Trap";return c.Stop(EngineModelStatus.TrapContinuation,"ThrowDependencyReturnedToInt3");
   }
  }
  public static EngineRootResult RegisterExit(ulong callback,EngineExitState state,EngineRootDependencies deps,EngineModelLimits limits){
   return Run(limits,delegate(Context c){if(RegisterExitCore(c,callback,state,deps))Complete(c);});
  }
  private static bool RegisterExitCore(Context c,ulong callback,EngineExitState state,EngineRootDependencies deps){
   EngineRootResult r=c.Result;r.Stage="ExitSelector";
   if(state==null)return c.Stop(EngineModelStatus.Unknown,"MissingExitState");
   ulong? cookie=state.Cookie;ulong? encoded=state.Encoded; // Observed load order.
   if(!cookie.HasValue||!encoded.HasValue)return c.Stop(EngineModelStatus.Unknown,"UnknownExitSelectorInputs");
   ulong decoded=DecodeSelector(cookie.Value,encoded.Value);
   bool crt=decoded==ulong.MaxValue;
   if(!c.Step("ExitSelector","ExitSelector",decoded,state,null))return false;
   r.DecodedExitValue=decoded;r.UsesCrtAtExit=crt;
   r.Stage="RegisterExit";
   if(deps==null)return c.Stop(EngineModelStatus.Unknown,"MissingExitDependencies");
   EngineDependencyResult<int> outcome;
   if(crt){
    if(deps.CrtAtExit==null)return c.Stop(EngineModelStatus.Unknown,"MissingCrtAtExit");
    if(!c.Step("RegisterExit","CrtAtExitCall",callback,null,null))return false;
    r.ExitInvoked=true;outcome=deps.CrtAtExit(callback);
   }else{
    if(state.Identity==null)return c.Stop(EngineModelStatus.Unknown,"UnknownExitStateCellIdentity");
    if(deps.RegisterOnExit==null)return c.Stop(EngineModelStatus.Unknown,"MissingRegisterOnExit");
    if(!c.Step("RegisterExit","RegisterOnExitCall",callback,state,null))return false;
    r.ExitInvoked=true;outcome=deps.RegisterOnExit(state,callback);
   }
   int eax;
   if(!c.Accept(outcome,out eax))return false;
   r.RawExitEax=eax;
   ulong returned=eax==0?callback:0;
   int outer=returned==0?-1:0;
   if(!c.Step("ExitReturn","ExitReturn",returned,null,outer))return false;
   r.ReturnedCallback=returned;r.ReturnValue=outer;r.ReturnRax=unchecked((ulong)(uint)outer);
   return true;
  }
  public static EngineRootResult Apply(EngineRootWriter writer,EngineRootNode node,EngineRootDependencies deps,EngineExitState state,EngineModelLimits limits){
   return Run(limits,delegate(Context c){
    if(writer!=EngineRootWriter.Actor&&writer!=EngineRootWriter.Entity&&writer!=EngineRootWriter.Object){c.Stop(EngineModelStatus.Unsupported,"UnknownWriterIdentity");return;}
    if(node==null){c.Stop(EngineModelStatus.Unknown,"MissingNode");return;}
    EngineRootWriterSpec spec=GetWriterSpec(writer);
    if(ApplyCore(c,spec,node,deps,state))Complete(c);
   });
  }
  private static bool Writable(Context c,EngineRootChildStorage storage,int index){
   if(index<0)return c.Stop(EngineModelStatus.Unsupported,"NegativeChildIndex");
   if(index>=c.Limits.MaxSlots)return c.Stop(EngineModelStatus.ResourceLimit,"OwnChildSlotLimit");
   if(storage.Slots==null)return c.Stop(EngineModelStatus.Unknown,"UnknownAllocatedSpan");
   if(index>=storage.Slots.Length)return c.Stop(EngineModelStatus.Unsupported,"AllocatedSpanTooSmall");
   return true;
  }
  private static bool ApplyCore(Context c,EngineRootWriterSpec spec,EngineRootNode node,EngineRootDependencies deps,EngineExitState state){
   EngineRootResult r=c.Result;r.Stage="DuplicateName";
   if(deps==null||deps.DuplicateName==null)return c.Stop(EngineModelStatus.Unknown,"MissingDuplicateName");
   if(!c.Step("DuplicateName","DuplicateNameCall",spec.NameSourceToken,node,null))return false;
   object name;
   if(!c.Accept(deps.DuplicateName(spec.NameSourceToken),out name))return false;
   if(!c.Step("ParentRead","ParentRead",null,node,null))return false;
   EngineRootNode parent=node.Parent;r.SelectedParent=parent;
   if(!c.Step("NameStore","NameStore",null,name,null))return false;
   node.Name=name;r.NameStored=true;
   if(parent!=null){
    int count=parent.Count;ulong size=AllocationSize(count);
    if(!c.Step("AllocationSize","AllocationSize",size,parent,count))return false;
    r.AllocationCount=count;
    if(!AllocateCore(c,size,deps))return false;
    EngineRootChildStorage allocated=r.AllocatedStorage;
    r.Stage="CopyChildren";
    // Own coverage guard: this extra read/alias check is not a native check.
    // Caller supplies exclusive own state; uncontrolled concurrent mutation
    // and aliasing allocations are outside the representable domain.
    EngineRootChildStorage initialOld=parent.Children;
    if(Object.ReferenceEquals(allocated,initialOld)||(initialOld!=null&&allocated.Slots!=null&&Object.ReferenceEquals(allocated.Slots,initialOld.Slots)))
     return c.Stop(EngineModelStatus.Unsupported,"AllocationAliasesOldStorage");
    // Preserve the observed signed entry test, per-iteration storage load,
    // and trailing count reload. No callback is invented between these reads.
    int copyIndex=0;
    if(parent.Count>0){
     do{
      EngineRootChildStorage old=parent.Children;
      int slot=copyIndex;copyIndex=unchecked(copyIndex+1);
      if(old==null||old.Slots==null)return c.Stop(EngineModelStatus.Unknown,"UnknownOldChildSpan");
      if(slot>=old.Slots.Length)return c.Stop(EngineModelStatus.Unsupported,"OldChildSpanTooSmall");
      if(!Writable(c,allocated,slot))return false;
      EngineRootNode child=old.Slots[slot];
      if(!c.Step("CopyChildren","CopyChild",unchecked((ulong)slot),child,null))return false;
      allocated.Slots[slot]=child;r.CopiedChildren++;
     }while(copyIndex<parent.Count);
    }
    r.Stage="AppendChild";int index=parent.Count;
    if(!Writable(c,allocated,index))return false;
    if(!c.Step("AppendChild","AppendChild",unchecked((ulong)index),node,null))return false;
    allocated.Slots[index]=node;r.ChildAppended=true;
    if(!c.Step("IncrementCount","CountStore",null,parent,unchecked(parent.Count+1)))return false;
    parent.Count=unchecked(parent.Count+1);r.CountIncremented=true;
    r.Stage="FreeOldStorage";r.FreeArgument=parent.Children;
    if(deps.Free==null)return c.Stop(EngineModelStatus.Unknown,"MissingFree");
    if(!c.Step("FreeOldStorage","FreeCall",null,r.FreeArgument,null))return false;
    r.FreeInvoked=true;
    EngineUnit ignored;
    if(!c.Accept(deps.Free(r.FreeArgument),out ignored))return false;
    r.FreeCompleted=true;
    if(!c.Step("PublishChildren","StoragePublish",null,allocated,null))return false;
    parent.Children=allocated;r.StoragePublished=true;
   }
   return RegisterExitCore(c,spec.ExitCallbackToken,state,deps);
  }
 }
}
