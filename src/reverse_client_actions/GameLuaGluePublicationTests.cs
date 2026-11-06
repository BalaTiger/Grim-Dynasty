using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

// Fixed cached Engine data only. These tests load/execute the eleven defining
// chunks; they never invoke the defined function bodies or registry closures.
// Expected hashes were independently derived from hash-verified raw artifacts
// using the observed255F70/255CE0 normal, stable nonaliasing byte contract.
public static class GameLuaGluePublicationTests {
 static readonly string[] Names={"dump","dumpr","dumpex","readonly","igetter","isetter","tgetter","tsetter","instance","derive","class"};
 static readonly int[] RawLengths={106,118,2091,203,1076,977,514,592,920,382,1105};
 static readonly int[] Tabs={47,43,1059,97,556,468,281,297,470,161,452};
 static readonly int[] Lengths={59,75,1032,106,520,509,233,295,450,221,653};
 static readonly string[] Digests={
  "fe29a6708964f6a4952c36c8c5d45a10f999a1d08d31a6dd380b3b67a7fd91d8","69b5f0ed6b4bad71fb552d414f56e4a5905c6219fc49be4b0e286bb5dbe35605","d5ae3e8ec8dc901caa72be754eda7cbe3d1e4b2632810ca9ce57da5e3492742f",
  "4485187d5dd5daab1bce760e64859bca1ed416d2e5969621144fa3b7fa48cea6","c97734c20bda847fc74055375e0d4ce74c5dab04fac3049fc19da253adb30ed7","07459f970d054635c95901d38b3c8fa808762fc23dcdcd8265d0b58214edc5ae",
  "cb97469de89b9181b63ed217c0ac22b6e328710ea94cabd3f2c0a278a2f403a9","bf68467e9263e8437007be44127279e4ba98b9075301c83c901606ef28a2a92b","e2fe155c8b4ba5bd855657ae6b761b401331c711c00073dd1d66575c0a4c077a",
  "edfb1b5b1d3057894ac6607c013398a8ff6635ef9e478c1dbda380566fa1a23b","cdfd18d194d05c87d77ae8bd79756b10c2ae1891dffe672f0bf36a3c8ab9f81e"};
 static int scenarios,assertions,olderAssertions;
 static readonly List<object> checks=new List<object>(),receipts=new List<object>();
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint expected,Action action,string why){try{action();}catch(MissingContract error){Eq(expected,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Test(string name,Action action){int before=assertions;action();scenarios++;checks.Add(new Dictionary<string,object>{{"name",name},{"assertions",assertions-before}});}
 static string Hash(byte[] bytes,int offset,int count){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes,offset,count)).Replace("-","").ToLowerInvariant();}
 static Dictionary<string,object> Call(OwnLuaCallbackResult r){return new Dictionary<string,object>{{"bridge_status",r.BridgeStatus},{"lua_status",r.LuaStatus},{"stage_rva",r.StageRva},{"published_mask",r.PublishedMask},{"callback_calls",r.CallbackCalls},{"logger_calls",r.LoggerCalls},{"resolver_calls",r.ResolverCalls},{"entry_top",r.EntryTop},{"exit_top",r.ExitTop},{"last_measured_local_top",r.FailureLocalTop},{"host_status",r.HostStatus},{"flags",r.Flags},{"fixture_id",r.FixtureId},{"context_token",r.ContextToken},{"host_failure",r.HostFailure},{"host_failure_rva",r.HostFailureRva},{"engine_read_rva",r.EngineReadRva}};}
 static void Remember(OwnLuaGlueResult r){
  var records=new List<object>();for(int n=0;n<r.Records.Length;n++){var x=r.Records[n];records.Add(new Dictionary<string,object>{{"index",n},{"record_name",Names[n]},{"prepared_length",x.PreparedLength},{"prepared",x.Prepared},{"snapshot_offset",x.SnapshotOffset},{"snapshot_bytes",x.SnapshotBytes},{"prepared_sha256",x.PreparedSha256},{"load_status",x.LoadStatus},{"pcall_status",x.PCallStatus},{"top_before_load",x.TopBeforeLoad},{"top_after_load",x.TopAfterLoad},{"top_after_pcall",x.TopAfterPCall},{"top_after_publish",x.TopAfterPublish},{"published",x.Published},{"table_field_type",x.TableFieldType},{"global_type",x.GlobalType},{"global_alias_equal",x.GlobalAliasEqual},{"phase",x.Phase},{"stage_rva",x.StageRva}});}
  receipts.Add(new Dictionary<string,object>{{"call",Call(r.Call)},{"phase",r.Phase},{"current_record",r.CurrentRecord},{"native_return_byte",r.NativeReturnByte},{"prepared_count",r.PreparedCount},{"loaded_count",r.LoadedCount},{"executed_count",r.ExecutedCount},{"published_count",r.PublishedCount},{"snapshot_bytes",r.SnapshotBytes},{"version_publication_mask",r.VersionPublicationMask},{"table_observed_count",r.TableObservedCount},{"globals_observed_count",r.GlobalsObservedCount},{"records",records}});
 }
 sealed class Scope:IDisposable {
  internal readonly OwnLuaJit204Runtime Runtime=OwnLuaJit204Runtime.OpenOwnProfile();
  internal readonly WorldEngine World=new WorldEngine();
  internal readonly Field<WorldEngine> EngineGlobal=new Field<WorldEngine>();
  internal readonly GameReachedGoalLuaService Service;
  internal readonly OwnLuaCallbackContext Context;
  internal OwnLuaCallbackBinding Binding;
  internal Scope(){EngineGlobal.Write(World);Service=GameLuaServiceInitializationContracts.PublishOwnColdService(World,new Invocation());Context=new OwnLuaCallbackContext(Service,EngineGlobal,null);Runtime.SelectOwnRegistry(Context);}
  internal OwnLuaJit204State State{get{return Service.Lua00.Read(0) as OwnLuaJit204State;}}
  internal void CallbackPrefix(){
   var call=new GameLuaCallbackInitializationInvocation();try{Missing(0x0f2ed2,delegate{GameLuaServiceInitializationContracts.InitializeCallbackPrefix0F2E00(Service,GameLuaLibraryBindings.ResolveOwn(Runtime),Runtime,Context,call);},"Existing callback prefix stops before LuaGlue");}finally{Binding=call.Binding;}
   Check(call.Publication!=null&&call.Publication.BridgeStatus==0&&call.Publication.LuaStatus==0&&call.Publication.PublishedMask==127,"Actual callback prefix completed");Same(Binding,State.CallbackBinding,"Same state owns real callback binding");Same(null,Context.OwnSink,"Positive LuaGlue uses no synthetic logger");
  }
  internal OwnLuaGlueResult Glue(){var r=Binding.InitializeLuaGlue();Remember(r);return r;}
  internal void ErrorFixture(int id){var r=Binding.ExecuteFixture((OwnLuaCallbackFixture)id);receipts.Add(new Dictionary<string,object>{{"own_fault_setup",Call(r)}});Check(r.BridgeStatus==0&&r.LuaStatus==0,"Fixed OWN global-assignment fault setup actually executed");Check(r.CallbackCalls==0&&r.LoggerCalls==0,"Fault setup neither calls native print nor a logger");}
  public void Dispose(){var state=State;if(state!=null)state.Dispose();Runtime.Dispose();Check(Runtime.ActiveStateCount==0&&Runtime.CloseFailure==null,"Actual created state closed without failure");Check(Runtime.ModuleUnloadObserved,"Pinned OWN module unload observed");}
 }
 static void NoFunctionExecution(OwnLuaGlueResult r){Check(r.Call.CallbackCalls==0&&r.Call.LoggerCalls==0,"Only definition chunks ran, no message function or logger");Check(r.Call.HostFailure==null,"No managed callback failure masquerades as source result");Eq(-1,r.NativeReturnByte,"Native return byte remains unobserved");}
 static void PrefixComplete(OwnLuaGlueResult r){
  Check(r.Call.BridgeStatus==7&&r.Call.StageRva==0x25762b,"Complete supported definition prefix explicitly stops at unknown registry continuation");Check(r.Call.LuaStatus==0&&(r.Call.Flags&3)==0,"Normal boundary does not fabricate Lua error or protected unwind");Eq(8u,r.Phase,"Registry boundary phase");Eq(11u,r.PreparedCount,"Eleven actually prepared records");Eq(11u,r.LoadedCount,"Eleven actual successful loads");Eq(11u,r.ExecutedCount,"Eleven actual successful protected definition calls");Eq(4u,r.PublishedCount,"Only four flagged global aliases published");Eq(15u,r.VersionPublicationMask,"All four actual version/table publication operations returned");NoFunctionExecution(r);
 }
 static void Snapshot(OwnLuaGlueResult r,int prepared){
  Eq(11,r.Records.Length,"Exactly eleven record receipts");Eq((int)r.SnapshotBytes,r.PreparedSnapshot.Length,"Only valid native output prefix is exposed");int total=0;
  for(int n=0;n<11;n++){
   var x=r.Records[n];if(n>=prepared){Check(x.Prepared==0&&x.PreparedLength==-1&&x.SnapshotOffset==UInt32.MaxValue&&x.SnapshotBytes==0,"Unvisited source remains unprepared: "+n);Check(x.LoadStatus==-1&&x.PCallStatus==-1&&x.PreparedSha256==null,"Unvisited execution/hash remains unobserved: "+n);continue;}
   Eq(1u,x.Prepared,"Actual preparation completed: "+Names[n]);Eq(Lengths[n],x.PreparedLength,"Actual length: "+Names[n]);Eq(RawLengths[n]-Tabs[n],x.PreparedLength,"Only TAB removed in stable observed domain");Eq((uint)total,x.SnapshotOffset,"Contiguous actual snapshot interval");Eq((uint)Lengths[n],x.SnapshotBytes,"Snapshot excludes trailing NUL");
   string digest=Hash(r.PreparedSnapshot,total,Lengths[n]);Eq(Digests[n],digest,"Independent hash of actual native prepared bytes: "+Names[n]);Eq(digest,x.PreparedSha256,"Wrapper hash is also derived from actual snapshot");
   bool noTab=true;for(int k=0;k<Lengths[n];k++)if(r.PreparedSnapshot[total+k]==9)noTab=false;Check(noTab,"No TAB survives preparation");total+=Lengths[n];
  }
  Eq((uint)total,r.SnapshotBytes,"No omitted or invented prepared-byte suffix");
 }
 static void ObserverUnknown(OwnLuaGlueResult r){Eq(0u,r.TableObservedCount,"No successful-loop table observation claimed after failure");Eq(0u,r.GlobalsObservedCount,"No successful-loop global observation claimed after failure");foreach(var x in r.Records)Check(x.TableFieldType==-1&&x.GlobalType==-1&&x.GlobalAliasEqual==-1,"Unexecuted OWN observer is unknown, not fabricated nil or equality");}
 static void RealLuaError(OwnLuaGlueResult r,uint at){Check(r.Call.BridgeStatus==6&&r.Call.LuaStatus!=0,"Actual Lua assignment error crosses only native protection");Eq(at,r.Call.StageRva,"Exact failed assignment callsite");Check((r.Call.Flags&3)==3,"OWN unwind and retained error object are explicit");Check(r.Call.ExitTop==r.Call.EntryTop+1,"Outer protected frame retains one error object; original failed stack is not reconstructed");NoFunctionExecution(r);}
 public static Dictionary<string,object> Run(){
  scenarios=assertions=olderAssertions=0;checks.Clear();receipts.Clear();
  Test("Eleven real prepared sources and four observed global function identities",delegate{
   byte[] version=File.ReadAllBytes(@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-luaglue-embedded-data-run1\version.bin");Eq("081b30cf6bf5ff34b6894ae23191840ca371ab45392b5c8725d8d7218a04762f",Hash(version,0,version.Length),"Cached original version artifact fingerprint");Eq("1.00",Encoding.ASCII.GetString(version,0,4),"Observed original version bytes, not an extra runtime version read");Check(version.Length==5&&version[4]==0,"Version artifact includes terminal NUL");
   using(var s=new Scope()){
    s.CallbackPrefix();var r=s.Glue();PrefixComplete(r);Snapshot(r,11);Eq(4153u,r.SnapshotBytes,"Actual prepared snapshot total");Eq(11u,r.TableObservedCount,"OWN raw observations find eleven table fields");Eq(4u,r.GlobalsObservedCount,"Only the four published globals were read back");
    for(int n=0;n<11;n++){var x=r.Records[n];Eq(0,x.LoadStatus,"Actual load status "+Names[n]);Eq(0,x.PCallStatus,"Actual protected definition status "+Names[n]);Check(x.TopBeforeLoad>=0&&x.TopAfterLoad==x.TopBeforeLoad+1,"Real load pushes exactly its compiled function");Eq(x.TopBeforeLoad,x.TopAfterPCall,"Zero-result definition execution returns to prior stack");Eq(6,x.TableFieldType,"Observed __luaglue entry is a real function");bool published=n==0||n==1||n==2||n==10;Eq(published?1u:0u,x.Published,"Observed per-record global publication flag");if(published){Eq(6,x.GlobalType,"Observed global is function");Eq(1,x.GlobalAliasEqual,"Global and table contain the same actual Lua function");Eq(x.TopBeforeLoad,x.TopAfterPublish,"Successful global publication balances its stack");}else Check(x.GlobalType==-1&&x.GlobalAliasEqual==-1,"Non-published globals remain unobserved, not asserted nil");}
    Eq((byte)0,s.Service.Initialized58.Read(0),"Eleven definitions never mark service initialized");
   }
  });
  Test("New service entry retains one Engine service VM and unknown readiness",delegate{
   using(var s=new Scope()){
    s.Service.Initialized58.Forget();var call=new GameLuaGlueInitializationInvocation();try{Missing(0x25762b,delegate{GameLuaServiceInitializationContracts.InitializeLuaGluePrefix0F2E00(s.Service,GameLuaLibraryBindings.ResolveOwn(s.Runtime),s.Runtime,s.Context,call);},"New entry stops only at proven next LuaGlue boundary");}finally{s.Binding=call.Binding;}
    Check(call.Glue!=null,"Actual production Glue receipt retained");Remember(call.Glue);PrefixComplete(call.Glue);Eq(0x25762bu,call.Native.At,"New precise stop point");Same(s.State.CallbackBinding,call.Binding,"Created VM and callback owner stay identical");Same(s.Service,call.Binding.Context.Service,"No cloned service");Same(s.Service,GameReachedGoalLogEngine.ForWorldEngine(s.World).LuaService68.Read(0),"Original shared Engine publication remains authoritative");Eq(1L,s.Runtime.NativeNewStateCalls,"One real VM across core callbacks and Glue");Eq(1L,s.Runtime.ActiveStateCount,"Same VM is still live at unsupported continuation");Eq(5L,s.Runtime.NativePCallZeroCalls,"Core library prefix ran once");Missing(0,delegate{s.Service.Initialized58.Read(0);},"Unknown native readiness remains unknown");Same(null,s.Context.OwnSink,"No OWN logger success adapter");
   }
  });
  Test("Unknown registry after callback publication is not consumed by this Glue prefix",delegate{
   using(var s=new Scope()){
    s.CallbackPrefix();var lua=s.State;var token=s.Binding.Token;s.Context.Registry422888.Forget();var r=s.Glue();PrefixComplete(r);Same(lua,s.Service.Lua00.Read(0),"Same VM remains in service");Eq(token,r.Call.ContextToken,"Same native context used");Missing(0,delegate{s.Context.Registry422888.Read(0);},"Glue never fills or substitutes unknown registry");Eq((byte)0,s.Service.Initialized58.Read(0),"Registry boundary does not initialize service");
   }
  });
  Test("Real early __luaglue assignment error preserves zero-record prefix",delegate{
   using(var s=new Scope()){
    s.CallbackPrefix();s.ErrorFixture(11);var r=s.Glue();RealLuaError(r,0x25693a);Eq(1u,r.Phase,"Failure remains in version publication");Eq(-1,r.CurrentRecord,"No source record entered");Eq(3u,r.VersionPublicationMask,"Only table creation and its version field completed");Check(r.PreparedCount==0&&r.LoadedCount==0&&r.ExecutedCount==0&&r.PublishedCount==0,"No definition work fabricated after early Lua error");Snapshot(r,0);ObserverUnknown(r);Eq((byte)0,s.Service.Initialized58.Read(0),"Error preserves service readiness");var lua=s.State;lua.Dispose();Check(!lua.IsAlive&&s.Runtime.ActiveStateCount==0&&s.Runtime.NativeCloseCalls==1,"Actual error state closes exactly once");Check(s.Binding.IsClosed,"Native callbacks invalidated after error close");
   }
  });
  Test("Third global assignment error preserves three loads calls and only two publications",delegate{
   using(var s=new Scope()){
    s.CallbackPrefix();s.ErrorFixture(12);var r=s.Glue();RealLuaError(r,0x2575fd);Eq(6u,r.Phase,"Failure is publication, not load or pcall");Eq(2,r.CurrentRecord,"Actual third source record");Eq(15u,r.VersionPublicationMask,"Version publications predate record failure");Eq(3u,r.PreparedCount,"Three prepared records");Eq(3u,r.LoadedCount,"Three actual successful loads");Eq(3u,r.ExecutedCount,"Three actual successful pcall returns");Eq(2u,r.PublishedCount,"Only first two global writes completed");Snapshot(r,3);Eq(1166u,r.SnapshotBytes,"Actual first-three snapshot prefix");for(int n=0;n<3;n++){Eq(0,r.Records[n].LoadStatus,"Load did not fail");Eq(0,r.Records[n].PCallStatus,"Definition execution did not fail");Eq(n<2?1u:0u,r.Records[n].Published,"Actual global write prefix");}Eq(-1,r.Records[2].TopAfterPublish,"Failed global write has no after-return stack observation");ObserverUnknown(r);Eq((byte)0,s.Service.Initialized58.Read(0),"Partial definitions cannot mark service initialized");var lua=s.State;lua.Dispose();Check(!lua.IsAlive&&s.Runtime.ActiveStateCount==0&&s.Runtime.NativeCloseCalls==1,"Actual third-record error state closes once");
   }
  });
  Test("LuaGlue binding rejects reuse after real close",delegate{
   using(var s=new Scope()){
    s.CallbackPrefix();var r=s.Glue();PrefixComplete(r);var lua=s.State;var binding=s.Binding;lua.Dispose();Check(!lua.IsAlive&&binding.IsClosed,"Actual Lua close releases bridge ownership");Check(binding.CloseResult!=null,"Native close receipt exists");receipts.Add(new Dictionary<string,object>{{"close",Call(binding.CloseResult)}});Missing(0x0f2ecf,delegate{binding.InitializeLuaGlue();},"Closed state never reaches native LuaGlue again");Eq(1L,s.Runtime.NativeCloseCalls,"No second native close");Same(lua,s.Service.Lua00.Read(0),"Closed identity is not rewritten to null");
   }
  });
  Test("Definition-only Glue does not consume unknown Engine or invent a logger",delegate{
   using(var s=new Scope()){
    s.CallbackPrefix();s.EngineGlobal.Forget();var r=s.Glue();PrefixComplete(r);Eq(0u,r.Call.EngineReadRva,"No message leaf was entered");Missing(0,delegate{s.EngineGlobal.Read(0);},"Unknown Engine remains unknown");Same(null,s.Context.OwnSink,"No substitute logger was supplied");Check(s.State.IsAlive,"Real definition VM remains live");
   }
  });
  int beforeLegacy=assertions;
  Test("Selected regression: callback-only service entry still stops at ED2",delegate{
   using(var s=new Scope()){
    s.Service.Initialized58.Write(0x5a);s.CallbackPrefix();Eq(1L,s.Runtime.NativeNewStateCalls,"Old callback entry creates one VM");Eq(5L,s.Runtime.NativePCallZeroCalls,"Old core prefix executes five calls");Eq((byte)0x5a,s.Service.Initialized58.Read(0),"Old entry preserves ready byte");Same(null,s.Binding.LastLuaGlueResult,"Old entry did not silently invoke new LuaGlue work");Check(s.State.IsAlive,"Old exact stop keeps real VM alive");
   }
  });
  olderAssertions=assertions-beforeLegacy;
  return new Dictionary<string,object>{{"status","passed-own-luaglue-eleven-record-prefix"},{"scenarios",scenarios},{"new_scenarios",scenarios-1},{"selected_older_regressions",1},{"assertions",assertions},{"new_assertions",assertions-olderAssertions},{"selected_older_assertions",olderAssertions},{"old_whole_suites_run",0},{"targeted_checks",checks},{"luaglue_receipts",receipts},{"expected_unsupported",0x25762b},{"normal_prepared_bytes",4153},{"native_return_known",false},{"full_native_luaglue_complete",false},{"native_service_initialization_completed",false},{"script_helper_initialization_completed",false},{"registry_closures_invoked",false},{"defined_function_bodies_invoked",false},{"runtime_version_string_readback",false},{"own_raw_function_observations",true},{"own_logger_adaptation",false},{"original_engine_logger_verified",false},{"original_exception_cleanup_equivalent",false},{"original_executed",false},{"physical_mouse",false},{"native_decoding",false},{"native_playback",false},{"gameplay_accepted",false}};
 }
}
