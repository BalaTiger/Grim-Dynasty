using System;
using System.Runtime.InteropServices;

namespace GrimDynasty.ReverseClient.Actions {
 // One fresh invocation owns the same eleven records through its native tail.
 // OWN protected cleanup is separate from the observed normal destructor order.
 public sealed class OwnLuaGlueCompletionResult {
  public OwnLuaGlueResult Glue;
  public OwnLuaCallbackResult Call{get{return Glue.Call;}}
  public uint Phase,RegistryPublishedMask,RegistryObservedMask,GcClosureMatchMask;
  public int[] RegistryTypes;
  public int LuaGlueAlias,FlagsKnown,LogReturned,CleanupCurrentRecord,RecordsLiveAtFlag;
  public uint FlagsDword,RecordsConstructed,NormalCleanupEntered,NormalRecordsCompleted;
  public uint NormalFreeAttempts,NormalFreeSuccesses,OwnFreeAttempts,OwnFreeSuccesses;
  public uint LiveRecordAllocations,CleanupTraceCount,PreparedFailureReleases;
  public uint[] CleanupTrace;
 }
 internal sealed partial class OwnLuaCallbackBridge {
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int FlagsReader(IntPtr cookie,uint stage,out uint flagsDword);
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct CompletionResult {
   internal GlueResult Glue;
   internal uint Bytes,Phase,RegistryPublishedMask,RegistryObservedMask;
   [MarshalAs(UnmanagedType.ByValArray,SizeConst=4)] internal int[] RegistryTypes;
   internal int LuaGlueAlias;internal uint GcClosureMatchMask;
   internal int FlagsKnown;internal uint FlagsDword;
   internal int LogReturned,CleanupCurrentRecord;internal uint RecordsConstructed;
   internal int RecordsLiveAtFlag;
   internal uint NormalCleanupEntered,NormalRecordsCompleted,NormalFreeAttempts,NormalFreeSuccesses,OwnFreeAttempts,OwnFreeSuccesses;
   internal uint LiveRecordAllocations,CleanupTraceCount;
   [MarshalAs(UnmanagedType.ByValArray,SizeConst=33)] internal uint[] CleanupTrace;
   internal uint PreparedFailureReleases;
  }
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CompletionFunction(ulong token,FlagsReader reader,IntPtr cookie,out CompletionResult result,[Out,MarshalAs(UnmanagedType.LPArray,SizeParamIndex=5)] byte[] snapshot,uint capacity);
  internal CompletionResult InitializeLuaGlueCompletion(ulong token,FlagsReader reader,IntPtr cookie,out byte[] snapshot){
   if(Marshal.SizeOf(typeof(CompletionResult))!=1048||Marshal.OffsetOf(typeof(CompletionResult),"Bytes").ToInt64()!=816||Marshal.OffsetOf(typeof(CompletionResult),"CleanupTrace").ToInt64()!=912||Marshal.OffsetOf(typeof(CompletionResult),"PreparedFailureReleases").ToInt64()!=1044)
    throw new MissingContract(0,"OWN LuaGlue completion receipt ABI differs");
   // Existing Bind verifies the exact pinned module/export owner. No arbitrary
   // library or entry point is admitted; the per-call delegate is never cached.
   var run=(CompletionFunction)Bind("gd_lj204_initialize_luaglue_completion",typeof(CompletionFunction));
   snapshot=new byte[GlueSnapshotCapacity];CompletionResult result;
   int status=run(token,reader,cookie,out result,snapshot,GlueSnapshotCapacity);
   GC.KeepAlive(reader);GC.KeepAlive(run);
   Validate(status,result.Glue.Call);ValidateGlueResult(result.Glue);
   ValidateCompletionResult(result,token);return result;
  }
  static void ValidateCompletionResult(CompletionResult r,ulong token){
   uint at=r.Glue.Call.StageRva;
   if(r.Bytes!=1048||r.Glue.Call.Token!=token||r.Phase>6||r.RegistryPublishedMask>15||r.RegistryObservedMask>15||r.RegistryTypes==null||r.RegistryTypes.Length!=4||r.LuaGlueAlias< -1||r.LuaGlueAlias>1||r.GcClosureMatchMask>3||r.FlagsKnown< -1||r.FlagsKnown>1||r.LogReturned< -1||r.LogReturned>1||r.CleanupCurrentRecord< -1||r.CleanupCurrentRecord>10||r.RecordsConstructed>11||r.RecordsLiveAtFlag< -1||r.RecordsLiveAtFlag>11||r.NormalCleanupEntered>1||r.NormalRecordsCompleted>11||r.CleanupTrace==null||r.CleanupTrace.Length!=33||r.CleanupTraceCount>33||r.PreparedFailureReleases>1)
    throw new MissingContract(at,"Invalid OWN LuaGlue completion receipt");
   if(r.NormalFreeSuccesses>r.NormalFreeAttempts||r.OwnFreeSuccesses>r.OwnFreeAttempts||r.NormalRecordsCompleted>r.RecordsConstructed||r.LiveRecordAllocations>33)
    throw new MissingContract(at,"Inconsistent OWN LuaGlue cleanup observations");
   for(int n=0;n<(int)r.CleanupTraceCount;n++)if(r.CleanupTrace[n]>32)
    throw new MissingContract(at,"Invalid OWN LuaGlue completed-release trace");
   if(r.Glue.NativeReturnByte!=-1&&(r.Phase!=6||r.NormalCleanupEntered!=1||r.NormalRecordsCompleted!=11||r.LiveRecordAllocations!=0))
    throw new MissingContract(at,"OWN LuaGlue return lacks completed normal record cleanup");
   if(r.Glue.NativeReturnByte!=-1){
    if(r.RecordsConstructed!=11||r.NormalFreeAttempts!=33||r.NormalFreeSuccesses!=33||r.CleanupTraceCount!=33||r.OwnFreeAttempts!=0||r.OwnFreeSuccesses!=0)
     throw new MissingContract(at,"OWN LuaGlue return lacks the same complete normal-release trace");
    for(int n=0;n<33;n++)if(r.CleanupTrace[n]!=(uint)((10-n/3)*3+n%3))
     throw new MissingContract(at,"OWN LuaGlue normal destructor order differs");
    var call=r.Glue.Call;
    if(r.Glue.NativeReturnByte==1){
     if(call.BridgeStatus!=0||call.LuaStatus!=0||(call.Flags&32)!=0||r.FlagsKnown!=1||(r.FlagsDword&1)!=0)
      throw new MissingContract(at,"OWN LuaGlue normal1 does not match the supported flag-clear path");
    }else{
     bool pcallFailed=false;foreach(var record in r.Glue.Records)if(record.PCallStatus>0)pcallFailed=true;
     if(call.BridgeStatus!=6||call.LuaStatus<=0||(call.Flags&35)!=35||call.ExitTop<=call.EntryTop||!pcallFailed||r.PreparedFailureReleases!=1)
      throw new MissingContract(at,"OWN LuaGlue normal0 lacks the actual protected error transfer");
    }
   }
  }
  internal static OwnLuaGlueCompletionResult CompletionReceipt(CompletionResult r,OwnLuaCallbackResult call,byte[] snapshot){
   var trace=new uint[(int)r.CleanupTraceCount];Array.Copy(r.CleanupTrace,trace,trace.Length);
   return new OwnLuaGlueCompletionResult{Glue=GlueReceipt(r.Glue,call,snapshot),Phase=r.Phase,RegistryPublishedMask=r.RegistryPublishedMask,RegistryObservedMask=r.RegistryObservedMask,RegistryTypes=(int[])r.RegistryTypes.Clone(),LuaGlueAlias=r.LuaGlueAlias,GcClosureMatchMask=r.GcClosureMatchMask,FlagsKnown=r.FlagsKnown,FlagsDword=r.FlagsDword,LogReturned=r.LogReturned,CleanupCurrentRecord=r.CleanupCurrentRecord,RecordsConstructed=r.RecordsConstructed,RecordsLiveAtFlag=r.RecordsLiveAtFlag,NormalCleanupEntered=r.NormalCleanupEntered,NormalRecordsCompleted=r.NormalRecordsCompleted,NormalFreeAttempts=r.NormalFreeAttempts,NormalFreeSuccesses=r.NormalFreeSuccesses,OwnFreeAttempts=r.OwnFreeAttempts,OwnFreeSuccesses=r.OwnFreeSuccesses,LiveRecordAllocations=r.LiveRecordAllocations,CleanupTraceCount=r.CleanupTraceCount,CleanupTrace=trace,PreparedFailureReleases=r.PreparedFailureReleases};
  }
 }
 public sealed partial class OwnLuaCallbackBinding {
  public OwnLuaGlueCompletionResult LastLuaGlueCompletionResult{get;private set;}
  public OwnLuaGlueCompletionResult InitializeLuaGlueCompletion(Field<uint> sharedFlags4217D8){
   OwnLuaGlueCompletionResult result=null;
   runtime.InvokeBridge(state,this,delegate{
    LastLuaGlueCompletionResult=null;ClearHost();
    var captured=Context.Service.Lua00.Read(0x0f2ecf) as OwnLuaJit204State;
    if(captured==null||!Object.ReferenceEquals(captured,state))throw new MissingContract(0x0f2ecf,"OWN LuaGlue requires the captured current Lua00 to be this same live bound state");
    captured.RequireLive(0x0f2ecf);
    OwnLuaCallbackBridge.FlagsReader reader=delegate(IntPtr received,uint stage,out uint flagsDword){
     flagsDword=0;bool entered=false;
     try{
      RequireHost(received,stage);runtime.EnterHostCallback(stage);entered=true;
      if(stage!=0x257707)throw new MissingContract(stage,"Unknown OWN LuaGlue flags read site");
      if(sharedFlags4217D8==null)throw new MissingContract(stage,"Missing shared4217D8 field identity");
      flagsDword=sharedFlags4217D8.Read(stage);return 0;
     }catch(Exception error){return Failed(error,stage);}
     finally{if(entered)runtime.ExitHostCallback();}
    };
    byte[] snapshot;var observed=bridge.InitializeLuaGlueCompletion(Token,reader,cookie,out snapshot);
    LastResult=Receipt(observed.Glue.Call);result=OwnLuaCallbackBridge.CompletionReceipt(observed,LastResult,snapshot);LastLuaGlueCompletionResult=result;
    GC.KeepAlive(reader);GC.KeepAlive(sharedFlags4217D8);GC.KeepAlive(captured);return LastResult;
   },0x0f2ecf);
   return result;
  }
 }
}
