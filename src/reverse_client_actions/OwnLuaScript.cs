using System;
using System.Runtime.InteropServices;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class OwnLuaScriptBindingResult {
  public uint Kind,HelperEntered,HelperReturned,Phase;
  public int WrapperMode,WrapperReference,CoreTableIndex,LookupIsC;
  public uint HelperNameLength,HelperNameCapacity,HelperAllocations,HelperReleases;
  public uint NodeCreated,UserdataCreated,NodePublished,ClosurePublished,KnownNodeFieldMask;
  public int DescriptorKnown;public uint DescriptorByte,StageRva;
 }
 public sealed class OwnLuaScriptResult {
  public OwnLuaCallbackResult Call;
  public uint Phase,HelpersCompleted,TableCreated,GlobalPublished,ReferenceReleased,NormalReturned,WrapperReads;
  public int TableReference,OuterVersion,PrefixIdentityPreserved;
  public uint OwnTempReleases,NormalTempReleases,LiveTempAllocations;
  public OwnLuaScriptBindingResult[] Bindings;
  public readonly bool OriginalRuntimeDescriptorProven=false;
  public readonly bool ScriptBusinessExecutionImplemented=false;
 }
 public sealed class OwnLuaScriptAudit {
  public uint NodesAllocated,NodesPublished,DestructorEntered,DestructorCompleted;
  public uint NameFreeAttempts,NameFreeSuccesses,NodeFreeAttempts,NodeFreeSuccesses;
  public uint LiveNodes,LiveNodeNames,OwnUnpublishedNodeFrees,OwnUnpublishedNameFrees;
  public uint UnsupportedGc,CallbackEntries,LastFailureRva,CloseNodeCallbacks;
  public int LastCallbackReturn;public ulong ContextToken;
 }
 internal sealed partial class OwnLuaCallbackBridge {
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int ScriptReader(IntPtr cookie,uint stage,uint requested,out long value);
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct ScriptBindingResult {
   internal uint Kind,HelperEntered,HelperReturned,Phase;
   internal int WrapperMode,WrapperReference,CoreTableIndex,LookupIsC;
   internal uint HelperNameLength,HelperNameCapacity,HelperAllocations,HelperReleases;
   internal uint NodeCreated,UserdataCreated,NodePublished,ClosurePublished,KnownNodeFieldMask;
   internal int DescriptorKnown;internal uint DescriptorByte,StageRva;
  }
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct ScriptResult {
   internal Result Call;internal uint Bytes,Phase,HelpersCompleted,TableCreated;
   internal int TableReference,OuterVersion;
   internal uint GlobalPublished,ReferenceReleased,NormalReturned,WrapperReads;
   internal int PrefixIdentityPreserved;internal uint OwnTempReleases,NormalTempReleases,LiveTempAllocations;
   [MarshalAs(UnmanagedType.ByValArray,SizeConst=3)] internal ScriptBindingResult[] Bindings;
  }
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct ScriptAudit {
   internal uint Bytes,NodesAllocated,NodesPublished,DestructorEntered,DestructorCompleted;
   internal uint NameFreeAttempts,NameFreeSuccesses,NodeFreeAttempts,NodeFreeSuccesses;
   internal uint LiveNodes,LiveNodeNames,OwnUnpublishedNodeFrees,OwnUnpublishedNameFrees;
   internal uint UnsupportedGc,CallbackEntries,LastFailureRva;
   internal int LastCallbackReturn;internal uint CloseNodeCallbacks;internal ulong Token;
  }
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int ScriptFunction(ulong token,ScriptReader reader,IntPtr cookie,out ScriptResult result);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int ScriptAuditFunction(ulong token,out ScriptAudit audit);
  static void ValidateScriptLayout(){
   if(Marshal.SizeOf(typeof(ScriptBindingResult))!=80||Marshal.SizeOf(typeof(ScriptResult))!=360||Marshal.OffsetOf(typeof(ScriptResult),"Bindings").ToInt64()!=120||Marshal.SizeOf(typeof(ScriptAudit))!=80||Marshal.OffsetOf(typeof(ScriptAudit),"Token").ToInt64()!=72)
    throw new MissingContract(0,"OWN Script receipt ABI differs");
  }
  internal ScriptResult InitializeScript(ulong token,ScriptReader reader,IntPtr cookie){
   ValidateScriptLayout();var run=(ScriptFunction)Bind("gd_lj204_initialize_script",typeof(ScriptFunction));
   ScriptResult r;int status=run(token,reader,cookie,out r);GC.KeepAlive(reader);GC.KeepAlive(run);Validate(status,r.Call);
   uint at=r.Call.StageRva;
   if(r.Bytes!=360||r.Call.Token!=token||r.Call.FixtureId!=0||r.Phase>5||r.HelpersCompleted>3||r.TableCreated>1||r.GlobalPublished>1||r.ReferenceReleased>1||r.NormalReturned>1||r.PrefixIdentityPreserved< -1||r.PrefixIdentityPreserved>1||r.Bindings==null||r.Bindings.Length!=3)
    throw new MissingContract(at,"Invalid OWN Script receipt");
   uint completed=0;
   for(int n=0;n<3;n++){
    var b=r.Bindings[n];
    if(b.Kind!=(uint)n||b.HelperEntered>1||b.HelperReturned>1||b.Phase>6||b.LookupIsC< -1||b.LookupIsC>1||b.NodeCreated>1||b.UserdataCreated>1||b.NodePublished>1||b.ClosurePublished>1||b.KnownNodeFieldMask>127||b.DescriptorKnown< -1||b.DescriptorKnown>1||(b.DescriptorKnown==1&&b.DescriptorByte>255)||b.HelperReturned>b.HelperEntered)
     throw new MissingContract(at,"Invalid OWN Script binding observation");
    completed+=b.HelperReturned;
   }
   if(completed!=r.HelpersCompleted||r.NormalReturned==1&&(r.Call.BridgeStatus!=0||r.Call.LuaStatus!=0||r.Phase!=5||r.HelpersCompleted!=3||r.TableCreated!=1||r.GlobalPublished!=1||r.ReferenceReleased!=1||r.LiveTempAllocations!=0))
    throw new MissingContract(at,"OWN Script normal return lacks its actual completed prefix");
   return r;
  }
  internal OwnLuaScriptAudit ReadScriptAudit(ulong token){
   ValidateScriptLayout();var read=(ScriptAuditFunction)Bind("gd_lj204_read_script_audit",typeof(ScriptAuditFunction));
   ScriptAudit v;int status=read(token,out v);GC.KeepAlive(read);
   if(status!=0||v.Bytes!=80||v.Token!=token||v.LastCallbackReturn< -1||v.LastCallbackReturn>0||v.NameFreeSuccesses>v.NameFreeAttempts||v.NodeFreeSuccesses>v.NodeFreeAttempts)
    throw new MissingContract(0,"OWN Script audit unavailable or invalid; status="+status);
   return new OwnLuaScriptAudit{NodesAllocated=v.NodesAllocated,NodesPublished=v.NodesPublished,DestructorEntered=v.DestructorEntered,DestructorCompleted=v.DestructorCompleted,NameFreeAttempts=v.NameFreeAttempts,NameFreeSuccesses=v.NameFreeSuccesses,NodeFreeAttempts=v.NodeFreeAttempts,NodeFreeSuccesses=v.NodeFreeSuccesses,LiveNodes=v.LiveNodes,LiveNodeNames=v.LiveNodeNames,OwnUnpublishedNodeFrees=v.OwnUnpublishedNodeFrees,OwnUnpublishedNameFrees=v.OwnUnpublishedNameFrees,UnsupportedGc=v.UnsupportedGc,CallbackEntries=v.CallbackEntries,LastFailureRva=v.LastFailureRva,LastCallbackReturn=v.LastCallbackReturn,CloseNodeCallbacks=v.CloseNodeCallbacks,ContextToken=v.Token};
  }
  internal static OwnLuaScriptResult ScriptReceipt(ScriptResult r,OwnLuaCallbackResult call){
   var result=new OwnLuaScriptResult{Call=call,Phase=r.Phase,HelpersCompleted=r.HelpersCompleted,TableCreated=r.TableCreated,TableReference=r.TableReference,OuterVersion=r.OuterVersion,GlobalPublished=r.GlobalPublished,ReferenceReleased=r.ReferenceReleased,NormalReturned=r.NormalReturned,WrapperReads=r.WrapperReads,PrefixIdentityPreserved=r.PrefixIdentityPreserved,OwnTempReleases=r.OwnTempReleases,NormalTempReleases=r.NormalTempReleases,LiveTempAllocations=r.LiveTempAllocations,Bindings=new OwnLuaScriptBindingResult[3]};
   for(int n=0;n<3;n++){var b=r.Bindings[n];result.Bindings[n]=new OwnLuaScriptBindingResult{Kind=b.Kind,HelperEntered=b.HelperEntered,HelperReturned=b.HelperReturned,Phase=b.Phase,WrapperMode=b.WrapperMode,WrapperReference=b.WrapperReference,CoreTableIndex=b.CoreTableIndex,LookupIsC=b.LookupIsC,HelperNameLength=b.HelperNameLength,HelperNameCapacity=b.HelperNameCapacity,HelperAllocations=b.HelperAllocations,HelperReleases=b.HelperReleases,NodeCreated=b.NodeCreated,UserdataCreated=b.UserdataCreated,NodePublished=b.NodePublished,ClosurePublished=b.ClosurePublished,KnownNodeFieldMask=b.KnownNodeFieldMask,DescriptorKnown=b.DescriptorKnown,DescriptorByte=b.DescriptorByte,StageRva=b.StageRva};}
   return result;
  }
 }
 public sealed partial class OwnLuaCallbackBinding {
  // StateHandle roots this binding, which roots the exact reader and borrowed
  // global cells until actual lua_close and native CompleteClose have returned.
  // No independent GCHandle root can keep an abandoned VM alive forever.
  OwnLuaCallbackBridge.ScriptReader scriptReader;
  public OwnLuaScriptResult LastScriptResult{get;private set;}
  public OwnLuaScriptAudit LastScriptAudit{get;private set;}
  public OwnLuaScriptAudit CloseScriptAudit{get;private set;}
  public string CloseScriptAuditFailure{get;private set;}
  internal Exception CloseScriptAuditException;
  internal OwnLuaScriptResult InitializeScriptCaptured(Field<uint> sharedFlags4217D8,byte? ownDescriptor307E60){
   OwnLuaScriptResult result=null;
   runtime.InvokeBridge(state,this,delegate{
    if(scriptReader!=null)throw new MissingContract(0x0ef8c4,"OWN Script reader/context was already assigned; no implicit retry or replacement");
    LastScriptResult=null;ClearHost();
    scriptReader=delegate(IntPtr received,uint stage,uint requested,out long value){
     value=0;bool entered=false;
     try{
      RequireHost(received,stage);runtime.EnterHostCallback(stage);entered=true;
      switch(requested){
       case 1:value=Context.Registry422888.Read(stage);break;
       case 2:value=Context.Version4217DC.Read(stage);break;
       case 3:if(sharedFlags4217D8==null)throw new MissingContract(stage,"Missing shared4217D8 identity");value=sharedFlags4217D8.Read(stage);break;
       case 4:value=Context.UpvalueBase422890.Read(stage);break;
       case 5:if(!ownDescriptor307E60.HasValue)throw new MissingContract(stage,"OWN descriptor307E60 observation is unknown");value=ownDescriptor307E60.Value;break;
       default:throw new MissingContract(stage,"Unknown typed Script global request");
      }
      return 0;
     }catch(Exception error){return Failed(error,stage);}
     finally{if(entered)runtime.ExitHostCallback();}
    };
    // Capture already happened exactly once in runtime.CaptureServiceBinding.
    // Native local wrapper and captured outerL/ref remain native-owned values.
    var observed=bridge.InitializeScript(Token,scriptReader,cookie);LastResult=Receipt(observed.Call);
    result=OwnLuaCallbackBridge.ScriptReceipt(observed,LastResult);LastScriptResult=result;GC.KeepAlive(scriptReader);return LastResult;
   },0x0ef8c4);return result;
  }
  public OwnLuaScriptAudit ReadScriptAudit(){
   OwnLuaScriptAudit value=null;runtime.InvokeBridge(state,this,delegate{value=bridge.ReadScriptAudit(Token);LastScriptAudit=value;return LastResult;},0);return value;
  }
  void CaptureCloseScriptAudit(){
   if(scriptReader==null)return;
   CloseScriptAudit=null;CloseScriptAuditFailure=null;CloseScriptAuditException=null;
   try{CloseScriptAudit=bridge.ReadScriptAudit(Token);LastScriptAudit=CloseScriptAudit;}
   catch(Exception error){CloseScriptAuditException=error;try{CloseScriptAuditFailure=error.ToString();}catch{CloseScriptAuditFailure="OWN Script close audit unavailable (formatting failed)";}}
  }
 }
}
