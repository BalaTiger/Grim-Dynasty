using System;
using System.Runtime.InteropServices;

namespace GrimDynasty.ReverseClient.Actions {
 public enum OwnLuaServiceRole:uint { Client=0,Server=1 }
 public sealed class OwnLuaServiceRoleResult {
  public OwnLuaCallbackResult Call;
  public uint Kind,Phase,BooleanPushed,VersionDword,GlobalPublished,ReferenceReleased,NormalReturned;
  public int Reference,VersionKnown,PrefixIdentityPreserved;
 }
 internal sealed partial class OwnLuaCallbackBridge {
  [StructLayout(LayoutKind.Sequential,Pack=8)] internal struct ServiceRoleResult {
   internal Result Call;
   internal uint Bytes,Kind,Phase,BooleanPushed;
   internal int Reference,VersionKnown;
   internal uint VersionDword,GlobalPublished,ReferenceReleased,NormalReturned;
   internal int PrefixIdentityPreserved;internal uint Reserved;
  }
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int ServiceRoleFunction(ulong token,uint kind,ScriptReader reader,IntPtr cookie,out ServiceRoleResult result);
  internal ServiceRoleResult PublishServiceRole(ulong token,OwnLuaServiceRole kind,ScriptReader reader,IntPtr cookie){
   if(Marshal.SizeOf(typeof(ServiceRoleResult))!=112||Marshal.OffsetOf(typeof(ServiceRoleResult),"Bytes").ToInt64()!=64)
    throw new MissingContract(0,"OWN role receipt ABI differs");
   if(kind!=OwnLuaServiceRole.Client&&kind!=OwnLuaServiceRole.Server)throw new MissingContract(0,"Unknown service role");
   var run=(ServiceRoleFunction)Bind("gd_lj204_publish_service_role",typeof(ServiceRoleFunction));
   ServiceRoleResult r;int status=run(token,(uint)kind,reader,cookie,out r);GC.KeepAlive(reader);GC.KeepAlive(run);Validate(status,r.Call);
   if(r.Bytes!=112||r.Kind!=(uint)kind||r.Call.Token!=token||r.Call.FixtureId!=0||r.Phase>6||r.BooleanPushed>1||r.VersionKnown< -1||r.VersionKnown>1||r.GlobalPublished>1||r.ReferenceReleased>1||r.NormalReturned>1||r.PrefixIdentityPreserved< -1||r.PrefixIdentityPreserved>1||r.Reserved!=0)
    throw new MissingContract(r.Call.StageRva,"Invalid OWN service role receipt");
   if(r.NormalReturned==1&&(r.Call.BridgeStatus!=0||r.Call.LuaStatus!=0||r.Phase!=6||r.BooleanPushed!=1||r.VersionKnown!=1||r.GlobalPublished!=1||r.ReferenceReleased!=1))
    throw new MissingContract(r.Call.StageRva,"OWN role normal return lacks its executed publication prefix");
   return r;
  }
 }
 public sealed partial class OwnLuaCallbackBinding {
  public OwnLuaServiceRoleResult LastServiceRoleResult{get;private set;}
  // Borrow only for this protected native call; never replace the permanent
  // Script node reader. The current service VM was captured by the caller.
  internal OwnLuaServiceRoleResult PublishServiceRoleCaptured(OwnLuaServiceRole kind){
   OwnLuaServiceRoleResult result=null;
   uint capture=kind==OwnLuaServiceRole.Client?0x0f2f2eu:0x0f3018u;
   runtime.InvokeBridge(state,this,delegate{
    LastServiceRoleResult=null;ClearHost();
    OwnLuaCallbackBridge.ScriptReader reader=delegate(IntPtr received,uint stage,uint requested,out long value){
     value=0;bool entered=false;
     try{
      RequireHost(received,stage);runtime.EnterHostCallback(stage);entered=true;
      if(requested==1)value=Context.Registry422888.Read(stage);
      else if(requested==2)value=Context.Version4217DC.Read(stage);
      else throw new MissingContract(stage,"Unknown service role field request");
      return 0;
     }catch(Exception error){return Failed(error,stage);}
     finally{if(entered)runtime.ExitHostCallback();}
    };
    var r=bridge.PublishServiceRole(Token,kind,reader,cookie);LastResult=Receipt(r.Call);
    result=new OwnLuaServiceRoleResult{Call=LastResult,Kind=r.Kind,Phase=r.Phase,BooleanPushed=r.BooleanPushed,Reference=r.Reference,VersionKnown=r.VersionKnown,VersionDword=r.VersionDword,GlobalPublished=r.GlobalPublished,ReferenceReleased=r.ReferenceReleased,NormalReturned=r.NormalReturned,PrefixIdentityPreserved=r.PrefixIdentityPreserved};
    LastServiceRoleResult=result;GC.KeepAlive(reader);return LastResult;
   },capture);return result;
  }
 }
}
