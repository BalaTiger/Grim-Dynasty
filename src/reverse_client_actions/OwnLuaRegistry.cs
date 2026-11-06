using System;
using System.Runtime.InteropServices;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class OwnLuaRegistryResult {
  public OwnLuaCallbackResult Call;
  public uint Phase,RegistryPublishedMask,ObservedMask,GcClosureMatchMask;
  public int[] RegistryTypes;
  public int LuaGlueAlias,NativeReturnByte;
 }
 // Cumulative OWN metadata, not proof of original dynamic destructors or heap
 // compatibility. Reading this receipt does not inspect freed userdata memory.
 public sealed class OwnLuaGcAudit {
  public uint ListEntered,ListCompleted,ObjectEntered,ObjectCompleted,FixtureUserdataCreated;
  public uint OwnBufferAllocations,OwnBufferFreeCalls,OwnBufferFreeSuccesses,LiveOwnBuffers;
  public uint UnsupportedCallbacks,CloseCallbacks,FieldsUnchangedChecks,LastFailureRva;
  public int LastCallbackReturn;
  public ulong ContextToken;
 }
 internal sealed partial class OwnLuaCallbackBridge {
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct RegistryResult {
   internal Result Call;
   internal uint Bytes,Phase,RegistryPublishedMask,ObservedMask;
   [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] internal int[] RegistryTypes;
   internal int LuaGlueAlias;internal uint GcClosureMatchMask;
   internal int NativeReturnByte;internal uint Reserved;
  }
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct GcAudit {
   internal uint Bytes,ListEntered,ListCompleted,ObjectEntered,ObjectCompleted,FixtureUserdataCreated;
   internal uint OwnBufferAllocations,OwnBufferFreeCalls,OwnBufferFreeSuccesses,LiveOwnBuffers;
   internal uint UnsupportedCallbacks,CloseCallbacks,FieldsUnchangedChecks,LastFailureRva;
   internal int LastCallbackReturn;internal uint Reserved;internal ulong Token;
  }
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int RegistryFunction(ulong token,out RegistryResult result);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GcAuditFunction(ulong token,out GcAudit result);
  readonly RegistryFunction publishRegistry;
  readonly GcAuditFunction readGcAudit;
  static void ValidateRegistryLayout(){
   if(Marshal.SizeOf(typeof(RegistryResult))!=112||Marshal.OffsetOf(typeof(RegistryResult),"RegistryTypes").ToInt64()!=80||Marshal.SizeOf(typeof(GcAudit))!=72||Marshal.OffsetOf(typeof(GcAudit),"Token").ToInt64()!=64)
    throw new MissingContract(0,"OWN Lua registry/audit receipt ABI differs");
  }
  internal RegistryResult PublishRegistry(ulong token){
   RegistryResult result;int status=publishRegistry(token,out result);Validate(status,result.Call);
   if(result.Bytes!=112||result.Call.Token!=token||result.Call.FixtureId!=0||result.Phase>3||result.RegistryPublishedMask>15||result.ObservedMask>15||result.GcClosureMatchMask>3||result.RegistryTypes==null||result.RegistryTypes.Length!=4||result.LuaGlueAlias< -1||result.LuaGlueAlias>1||result.NativeReturnByte< -1||result.NativeReturnByte>1||result.Reserved!=0)
    throw new MissingContract(result.Call.StageRva,"Invalid OWN Lua registry receipt");
   return result;
  }
  internal OwnLuaGcAudit ReadGcAudit(ulong token){
   GcAudit v;int status=readGcAudit(token,out v);
   if(status!=0||v.Bytes!=72||v.Token!=token||v.Reserved!=0||v.LastCallbackReturn< -1||v.LastCallbackReturn>0)
    throw new MissingContract(0,"OWN GC audit unavailable or invalid; status="+status);
   return new OwnLuaGcAudit{ListEntered=v.ListEntered,ListCompleted=v.ListCompleted,ObjectEntered=v.ObjectEntered,ObjectCompleted=v.ObjectCompleted,FixtureUserdataCreated=v.FixtureUserdataCreated,OwnBufferAllocations=v.OwnBufferAllocations,OwnBufferFreeCalls=v.OwnBufferFreeCalls,OwnBufferFreeSuccesses=v.OwnBufferFreeSuccesses,LiveOwnBuffers=v.LiveOwnBuffers,UnsupportedCallbacks=v.UnsupportedCallbacks,CloseCallbacks=v.CloseCallbacks,FieldsUnchangedChecks=v.FieldsUnchangedChecks,LastFailureRva=v.LastFailureRva,LastCallbackReturn=v.LastCallbackReturn,ContextToken=v.Token};
  }
 }
 public sealed partial class OwnLuaCallbackBinding {
  public OwnLuaRegistryResult LastRegistryResult{get;private set;}
  public OwnLuaGcAudit LastGcAudit{get;private set;}
  public OwnLuaGcAudit CloseGcAudit{get;private set;}
  public string CloseGcAuditFailure{get;private set;}
  internal Exception CloseGcAuditException;
  public OwnLuaRegistryResult PublishLuaGlueRegistry(){
   OwnLuaRegistryResult result=null;
   // Same captured L throughout2568D0. Unlike its initial entry, this continuation
   // MUST NOT read service.Lua00. Native retains its own completion qualification;
   // changing a public managed receipt cannot authorize this operation.
   runtime.InvokeBridge(state,this,delegate{
    LastRegistryResult=null;ClearHost();var v=bridge.PublishRegistry(Token);LastResult=Receipt(v.Call);
    result=new OwnLuaRegistryResult{Call=LastResult,Phase=v.Phase,RegistryPublishedMask=v.RegistryPublishedMask,ObservedMask=v.ObservedMask,GcClosureMatchMask=v.GcClosureMatchMask,RegistryTypes=(int[])v.RegistryTypes.Clone(),LuaGlueAlias=v.LuaGlueAlias,NativeReturnByte=v.NativeReturnByte};
    LastRegistryResult=result;return LastResult;
   },0x25762b);
   return result;
  }
  public OwnLuaGcAudit ReadGcAudit(){
   OwnLuaGcAudit audit=null;
   runtime.InvokeBridge(state,this,delegate{audit=bridge.ReadGcAudit(Token);LastGcAudit=audit;return LastResult;},0);
   return audit; // Metadata only; do not manufacture a Lua-call success receipt.
  }
  void CaptureCloseGcAudit(){
   CloseGcAudit=null;CloseGcAuditFailure=null;CloseGcAuditException=null;
   try{CloseGcAudit=bridge.ReadGcAudit(Token);LastGcAudit=CloseGcAudit;}
   catch(Exception error){
    // Actual lua_close already returned. Missing audit remains unknown but must
    // not prevent the independent, established context-release attempt.
    CloseGcAuditException=error;
    try{CloseGcAuditFailure=error.ToString();}catch{CloseGcAuditFailure="OWN GC close audit unavailable (formatting failed)";}
   }
  }
 }
}
