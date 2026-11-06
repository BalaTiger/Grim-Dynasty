using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace GrimDynasty.ReverseClient.Actions {
 // Actual native observations. Unknown signed fields remain -1, and an
 // uncaptured offset remains UInt32.MaxValue. These are not expected results.
 public sealed class OwnLuaGlueRecordResult {
  public int PreparedLength,LoadStatus,PCallStatus,TopBeforeLoad,TopAfterLoad,TopAfterPCall,TopAfterPublish,TableFieldType,GlobalType,GlobalAliasEqual;
  public uint SnapshotOffset,SnapshotBytes,Published,Prepared,Phase,StageRva;
  public string PreparedSha256;
 }
 public sealed class OwnLuaGlueResult {
  public OwnLuaCallbackResult Call;
  public uint Phase,PreparedCount,LoadedCount,ExecutedCount,PublishedCount,SnapshotBytes,VersionPublicationMask,TableObservedCount,GlobalsObservedCount;
  public int CurrentRecord,NativeReturnByte;
  public OwnLuaGlueRecordResult[] Records;
  // Exactly the returned valid output prefix, without per-record NULs. Hashes
  // are computed from these observed bytes, never from expected source text.
  public byte[] PreparedSnapshot;
 }
 internal sealed partial class OwnLuaCallbackBridge {
  const int GlueSnapshotCapacity=8192;
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct GlueRecord {
   internal int PreparedLength,LoadStatus,PCallStatus,TopBeforeLoad,TopAfterLoad,TopAfterPCall,TopAfterPublish,TableFieldType,GlobalType,GlobalAliasEqual;
   internal uint SnapshotOffset,SnapshotBytes,Published,Prepared,Phase,StageRva;
  }
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct GlueResult {
   internal Result Call;
   internal uint Bytes,Phase;internal int CurrentRecord,NativeReturnByte;
   internal uint PreparedCount,LoadedCount,ExecutedCount,PublishedCount,SnapshotBytes,VersionPublicationMask,TableObservedCount,GlobalsObservedCount;
   [MarshalAs(UnmanagedType.ByValArray,SizeConst=11)] internal GlueRecord[] Records;
  }
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int LuaGlueFunction(ulong token,out GlueResult result,[Out,MarshalAs(UnmanagedType.LPArray,SizeParamIndex=3)] byte[] snapshot,uint capacity);
  readonly LuaGlueFunction initializeLuaGlue;
  static void ValidateLuaGlueLayout(){
   if(Marshal.SizeOf(typeof(GlueRecord))!=64||Marshal.SizeOf(typeof(GlueResult))!=816||Marshal.OffsetOf(typeof(GlueResult),"Records").ToInt64()!=112)
    throw new MissingContract(0,"OWN LuaGlue receipt ABI differs");
  }
  internal GlueResult InitializeLuaGlue(ulong token,out byte[] snapshot){
   snapshot=new byte[GlueSnapshotCapacity];GlueResult result;
   int status=initializeLuaGlue(token,out result,snapshot,GlueSnapshotCapacity);
   Validate(status,result.Call);ValidateGlueResult(result);return result;
  }
  static void ValidateGlueResult(GlueResult result){
   uint at=result.Call.StageRva;
   if(result.Bytes!=816||result.Call.FixtureId!=0||result.Records==null||result.Records.Length!=11||result.Phase>8||result.CurrentRecord< -1||result.CurrentRecord>11||result.NativeReturnByte< -1||result.NativeReturnByte>1||result.SnapshotBytes>GlueSnapshotCapacity||result.VersionPublicationMask>15||result.TableObservedCount>11||result.GlobalsObservedCount>11)
    throw new MissingContract(at,"Invalid OWN LuaGlue receipt structure");
   uint end=0,prepared=0,loaded=0,executed=0,published=0;
   foreach(var r in result.Records){
    if(r.Prepared>1||r.Published>1||r.Phase>8||r.PreparedLength< -1||r.LoadStatus< -1||r.PCallStatus< -1)
     throw new MissingContract(at,"Invalid OWN LuaGlue record state");
    if(r.Prepared==1){
     if(r.PreparedLength<0||r.SnapshotOffset!=end||r.SnapshotBytes!=(uint)r.PreparedLength||r.SnapshotOffset>result.SnapshotBytes||r.SnapshotBytes>result.SnapshotBytes-r.SnapshotOffset)
      throw new MissingContract(at,"Invalid or overlapping OWN LuaGlue actual snapshot interval");
     end+=r.SnapshotBytes;prepared++;
    }else if(r.SnapshotOffset!=UInt32.MaxValue||r.SnapshotBytes!=0)
     throw new MissingContract(at,"Unprepared OWN LuaGlue record claims captured bytes");
    if(r.LoadStatus==0)loaded++;if(r.PCallStatus==0)executed++;published+=r.Published;
   }
   if(end!=result.SnapshotBytes||prepared!=result.PreparedCount||loaded!=result.LoadedCount||executed!=result.ExecutedCount||published!=result.PublishedCount||published>executed||executed>loaded||loaded>prepared)
    throw new MissingContract(at,"OWN LuaGlue counts do not match actual record observations");
  }
  internal static OwnLuaGlueResult GlueReceipt(GlueResult r,OwnLuaCallbackResult call,byte[] snapshot){
   var result=new OwnLuaGlueResult{Call=call,Phase=r.Phase,CurrentRecord=r.CurrentRecord,NativeReturnByte=r.NativeReturnByte,PreparedCount=r.PreparedCount,LoadedCount=r.LoadedCount,ExecutedCount=r.ExecutedCount,PublishedCount=r.PublishedCount,SnapshotBytes=r.SnapshotBytes,VersionPublicationMask=r.VersionPublicationMask,TableObservedCount=r.TableObservedCount,GlobalsObservedCount=r.GlobalsObservedCount,Records=new OwnLuaGlueRecordResult[11],PreparedSnapshot=new byte[(int)r.SnapshotBytes]};
   Buffer.BlockCopy(snapshot,0,result.PreparedSnapshot,0,(int)r.SnapshotBytes);
   for(int n=0;n<r.Records.Length;n++){
    var v=r.Records[n];string digest=null;
    if(v.Prepared==1)using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(snapshot,(int)v.SnapshotOffset,(int)v.SnapshotBytes)).Replace("-","").ToLowerInvariant();
    result.Records[n]=new OwnLuaGlueRecordResult{PreparedLength=v.PreparedLength,LoadStatus=v.LoadStatus,PCallStatus=v.PCallStatus,TopBeforeLoad=v.TopBeforeLoad,TopAfterLoad=v.TopAfterLoad,TopAfterPCall=v.TopAfterPCall,TopAfterPublish=v.TopAfterPublish,TableFieldType=v.TableFieldType,GlobalType=v.GlobalType,GlobalAliasEqual=v.GlobalAliasEqual,SnapshotOffset=v.SnapshotOffset,SnapshotBytes=v.SnapshotBytes,Published=v.Published,Prepared=v.Prepared,Phase=v.Phase,StageRva=v.StageRva,PreparedSha256=digest};
   }
   return result;
  }
 }
 public sealed partial class OwnLuaCallbackBinding {
  public OwnLuaGlueResult LastLuaGlueResult{get;private set;}
  public OwnLuaGlueResult InitializeLuaGlue(){
   OwnLuaGlueResult result=null;
   // Reuse the exact state/thread/reentry protection of the established bridge.
   // The caller load at0F2ECF occurs once here; the native helper holds that L.
   runtime.InvokeBridge(state,this,delegate{
    LastLuaGlueResult=null;ClearHost();var captured=Context.Service.Lua00.Read(0x0f2ecf) as OwnLuaJit204State;
    if(captured==null||!Object.ReferenceEquals(captured,state))throw new MissingContract(0x0f2ecf,"OWN LuaGlue requires the captured current Lua00 to be this same live bound state");
    captured.RequireLive(0x0f2ecf);byte[] snapshot;var observed=bridge.InitializeLuaGlue(Token,out snapshot);
    LastResult=Receipt(observed.Call);result=OwnLuaCallbackBridge.GlueReceipt(observed,LastResult,snapshot);LastLuaGlueResult=result;GC.KeepAlive(captured);return LastResult;
   },0x0f2ecf);
   return result;
  }
 }
}
