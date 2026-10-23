using System;
namespace GrimDynasty.TaskItems {
 // Portable data projection of the proved normal, exclusively owned path.
 // This does not create Windows TLS, a native critical section, event or callback.
 public sealed class ActorMatrixInitializationState {
  readonly byte?[] source;
  public int? GuardEpoch {get;private set;}
  public int? GlobalEpoch {get;private set;}
  public int GuardStores {get;private set;}
  public int GlobalEpochStores {get;private set;}
  public int SourceStores {get;private set;}
  public string Provenance {get;private set;}
  public ActorMatrixInitializationState(int? guard,int? global,byte?[] source48){
   if(source48!=null&&source48.Length!=48)throw new ArgumentException("Expected 48 source bytes");
   source=source48==null?new byte?[48]:(byte?[])source48.Clone();
   GuardEpoch=guard;GlobalEpoch=global;Provenance="SuppliedState";
  }
  public byte?[] CopySource(){return (byte?[])source.Clone();}
  public static ActorMatrixInitializationState CreateArchivedInitialState(){
   // Pinned file41D490: three 3F800000 DWORDs, at offsets0/16/32; all other bytes0.
   // Guard423F88 is virtual zero-fill; file41C51C is 80000000.
   var bytes=new byte?[48];for(int i=0;i<bytes.Length;i++)bytes[i]=0;
   for(int i=0;i<3;i++){bytes[i*16+2]=128;bytes[i*16+3]=63;}
   var state=new ActorMatrixInitializationState(0,int.MinValue,bytes);
   state.Provenance="ArchivedFileInitialState";return state;
  }
  internal void SetGuard(int value){GuardEpoch=value;GuardStores++;}
  internal void SetGlobal(int value){GlobalEpoch=value;GlobalEpochStores++;}
  internal void InitializeTail(){
   // 273B11 DWORD+2C, then273B1A QWORD+24. Never clear the unproved first36B.
   for(int i=44;i<48;i++)source[i]=0;SourceStores++;
   for(int i=36;i<44;i++)source[i]=0;SourceStores++;
  }
 }
 public sealed class ActorMatrixThreadState {
  public readonly bool ValidTlsPath;
  public int? Epoch {get;private set;}
  public int EpochStores {get;private set;}
  public string Provenance {get;private set;}
  public ActorMatrixThreadState(bool validTlsPath,int? epoch){ValidTlsPath=validTlsPath;Epoch=epoch;Provenance="SuppliedThreadState";}
  public static ActorMatrixThreadState CreateArchivedTemplateThread(){
   // The complete8B TLS file template is0000000000000080; this is an OWN logical
   // thread record, not proof that GS:[58], loader index or callbacks are valid.
   var thread=new ActorMatrixThreadState(true,int.MinValue);thread.Provenance="OwnThreadFromArchivedTemplate";return thread;
  }
  internal void SetEpoch(int value){Epoch=value;EpochStores++;}
 }
 public sealed class ActorMatrixInitializationSource:IActorMatrixStateSource {
  readonly ActorMatrixInitializationState state;
  readonly ActorMatrixThreadState thread;
  readonly bool normalExclusiveSynchronization,normalNotificationReturn;
  public int ReadCalls {get;private set;}
  public int HeaderCalls {get;private set;}
  public int FooterCalls {get;private set;}
  public int NotificationCalls {get;private set;}
  public int WaitCalls {get;private set;}
  public int WarmReturns {get;private set;}
  public int InitializationReturns {get;private set;}
  public int HeaderExistingReturns {get;private set;}
  // These are explicit coverage contracts, NOT values inferred from zero-filled
  // OS handles or encoded pointers. Caller owns the state exclusively throughout
  // Read; lock/unlock return normally. Notification is separately conditional.
  public ActorMatrixInitializationSource(ActorMatrixInitializationState state,ActorMatrixThreadState thread,
   bool normalExclusiveSynchronization,bool normalNotificationReturn){
   this.state=state;this.thread=thread;this.normalExclusiveSynchronization=normalExclusiveSynchronization;
   this.normalNotificationReturn=normalNotificationReturn;
  }
  public ActorMatrixReadResult Read(){
   ReadCalls++;
   if(thread==null||!thread.ValidTlsPath)return Stop("MissingValidTlsObservation",0x273AB4);
   if(!thread.Epoch.HasValue)return Stop("MissingThreadEpoch",0x273ACC);
   if(state==null||!state.GuardEpoch.HasValue)return Stop("MissingMatrixGuardEpoch",0x273ACF);
   if(state.GuardEpoch.Value<=thread.Epoch.Value){WarmReturns++;return Done("Warm");}
   HeaderCalls++;
   if(!normalExclusiveSynchronization)return Stop("UnresolvedMatrixSynchronization",0x2DCAF0);
   if(state.GuardEpoch.Value==-1){
    // 2DCB05 calls2DCBA4(100), then loops to re-read guard regardless of AL.
    // No busy wait, guessed scheduler result or synthetic guard mutation.
    WaitCalls++;return Stop("UnresolvedMatrixWait",0x2DCB05);
   }
   if(state.GuardEpoch.Value!=0){
    if(!state.GlobalEpoch.HasValue)return Stop("MissingMatrixGlobalEpoch",0x2DCB2A);
    thread.SetEpoch(state.GlobalEpoch.Value);HeaderExistingReturns++;return Done("HeaderExisting");
   }
   state.SetGuard(-1); // header releases before returning; exclusivity is required.
   state.InitializeTail();
   FooterCalls++;
   if(!state.GlobalEpoch.HasValue)return Stop("MissingMatrixGlobalEpoch",0x2DCA96);
   int next=unchecked(state.GlobalEpoch.Value+1);
   state.SetGlobal(next);state.SetGuard(next);
   // Footer re-reads global after storing guard. No intervening write is admitted
   // in this explicit exclusive domain; do not generalize to arbitrary reentry.
   thread.SetEpoch(state.GlobalEpoch.Value);
   NotificationCalls++;
   if(!normalNotificationReturn)return Stop("UnresolvedMatrixNotification",0x2DCB48);
   InitializationReturns++;return Done("Initialized");
  }
  ActorMatrixReadResult Done(string path){return ActorMatrixReadResult.Success(path,state.CopySource());}
  static ActorMatrixReadResult Stop(string reason,uint rva){return ActorMatrixReadResult.Unsupported(reason,rva);}
 }
}
