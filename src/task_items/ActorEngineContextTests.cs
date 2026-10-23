using System;
using GrimDynasty.TaskItems;

// Own small fixtures. No original module, platform handle, network or filesystem.
public static class ActorEngineContextTests {
 static int assertions,groups;
 static void Need(bool ok,string text){assertions++;if(!ok)throw new Exception(text);}
 static void Group(string name,Action action){int before=assertions;action();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 sealed class Calls:IActorEngineCallSource {
  public int Count;
  public Func<ActorEngineCall,ActorEngineContext,ActorEngineCallObservation> Hook;
  public ActorEngineCallObservation Invoke(ActorEngineCall call,ActorEngineContext state){Count++;return Hook==null?Normal(call):Hook(call,state);}
 }
 static ActorEngineCallObservation Normal(ActorEngineCall c){
  // Distinct allocator and constructor identities: no inferred ABI return-this.
  return new ActorEngineCallObservation(true,c.IsAllocation?0x100000UL+c.CallSiteRva:
   c.Role=="Server"?0x1234567800000001UL:c.Role=="Stub"?0x8765432100000001UL:(ulong?)null);
 }
 static ActorEngineContext Context(){return ActorEngineContext.CreateOwnConstructedFields(0x600000UL);}
 sealed class Matrix:IActorMatrixStateSource {
  public int Count;public Action<int> Hook;public bool Fail;
  readonly ActorMatrixInitializationSource actual=new ActorMatrixInitializationSource(
   ActorMatrixInitializationState.CreateArchivedInitialState(),ActorMatrixThreadState.CreateArchivedTemplateThread(),true,true);
  public ActorMatrixReadResult Read(){Count++;if(Hook!=null)Hook(Count);return Fail?ActorMatrixReadResult.Unsupported("OwnMatrixStop",0x273AEA):actual.Read();}
 }
 sealed class Global:IActorGlobalStateSource {
  public int Count;public Func<ActorGlobalObservation> Hook;
  public ActorGlobalObservation Read(){Count++;return Hook();}
 }
 sealed class Fixture {
  public ActorContinuationResult Prior;
  public ActorConstructorStorage Sub=new ActorConstructorStorage(0x200000UL,Payload(0x358));
  public ActorConstructorStorage Sentinel=new ActorConstructorStorage(0x300000UL,Payload(0x38));
  public Fixture(){
   var actor=new ActorConstructorPrefixState(Payload(0x530));
   var array=new Actor198ArrayStorage(0x100000UL,Payload(0x208));
   Prior=ActorConstructorContinuation.ContinueUsingArchivedConstant(
    ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(actor,array),0x200000UL);
  }
 }
 static byte?[] Payload(int n){var bytes=new byte?[n];for(int i=0;i<n;i++)bytes[i]=0xA5;return bytes;}
 static ActorCompletionResult Complete(Fixture f,IActorMatrixStateSource matrix,IActorGlobalStateSource global){
  // Deliberately conflicting old snapshot must not override the selected source.
  var data=ActorCompletionObservations.WithArchivedConstants(null,null,new ActorGlobalObservation(0x700000UL,5,6));
  return ActorConstructorCompletion.ContinueWithStateSources(f.Prior,0x500000UL,f.Sub,f.Sentinel,data,matrix,global);
 }
 static void OwnCreationAndIdentity(){
  var c=Context();
  Need(c.Current668==0&&c.Server678==0&&c.Stub680==0&&c.Token==0x600000UL,"valid own object receives observed zero fields");
  Need(c.Writes.Count==3&&c.Writes[0].InstructionRva==0x72C26&&c.Writes[1].InstructionRva==0x72C34&&c.Writes[2].InstructionRva==0x72C3B,"only selected ctor stores recorded");
  Need(!c.IsCompleteNativeEngine,"local fields do not claim full Engine");
  bool zero=false,overflow=false;
  try{ActorEngineContext.CreateOwnConstructedFields(0);}catch(ArgumentOutOfRangeException){zero=true;}
  try{ActorEngineContext.CreateOwnConstructedFields(ulong.MaxValue-0x686UL);}catch(ArgumentOutOfRangeException){overflow=true;}
  Need(zero&&overflow,"zero/overflow identity cannot become valid through zero fields");
  var edge=ActorEngineContext.CreateOwnConstructedFields(ulong.MaxValue-0x687UL);
  Need(edge.Token+0x687UL==ulong.MaxValue,"full selected field extent admitted at boundary");
  var f=new Fixture();var r=Complete(f,new Matrix(),new ActorEngineSlot(c));
  Need(r.NormalReturnReached&&f.Prior.State.ReadByte(0x4E1)==1,"zero equality is valid only inside explicitly bound own context");
 }
 static void CallArgumentsAndPublication(){
  var c=Context();var calls=new Calls();var r=new ActorEngineInitializationInvocation(c,calls).Run();
  Need(r.ProjectionReturned&&r.ResumeInstructionRva==0x74E4B&&r.Calls.Count==10,"selected interval ends before rest of Initialize");
  ulong[] sizes={0x340,0x2A0,0x20,0x20,0x20};uint[] sites={0x74D9D,0x74DBE,0x74DDF,0x74E00,0x74E21};uint[] targets={0x15A8B0,0x15A7D0,0x172F30,0x172F30,0x172ED0};
  bool args=true;
  for(int i=0;i<5;i++){
   var a=r.Calls[2*i];var b=r.Calls[2*i+1];
   args&=a.Descriptor.IsAllocation&&a.Descriptor.AllocationSize==sizes[i]&&a.Descriptor.CallSiteRva==sites[i]&&a.Descriptor.TargetRva==0x2DC398&&a.Descriptor.ThisToken==null;
   args&=!b.Descriptor.IsAllocation&&b.Descriptor.TargetRva==targets[i]&&b.Descriptor.ThisToken==a.Observation.Value&&b.Descriptor.AllocationSize==null;
  }
  Need(args,"independent known allocation sizes and constructor argument identities");
  Need(c.Server678==0x1234567800000001UL&&c.Stub680==0x8765432100000001UL&&c.Current668==c.Stub680,"publish ctor return values rather than allocation tokens");
  Need(r.Writes.Count==3&&r.Writes[0].InstructionRva==0x74DB2&&r.Writes[1].InstructionRva==0x74DD3&&r.Writes[2].InstructionRva==0x74E44,"ordered selected publications");
  Need(c.Writes.Count==6&&Object.ReferenceEquals(r.Writes[0],c.Writes[3]),"shared context and invocation retain same write objects");
  var f=new Fixture();var completed=Complete(f,new Matrix(),new ActorEngineSlot(c));
  Need(completed.NormalReturnReached&&f.Prior.State.ReadByte(0x4E1)==0,"64-bit identities differ despite equal low DWORD");
  c.Current668=c.Server678;var second=new Fixture();Complete(second,new Matrix(),new ActorEngineSlot(c));
  Need(second.Prior.State.ReadByte(0x4E1)==1,"caller mode change observed on next construction");
 }
 static void LateStubRead(){
  var c=Context();bool published=false;var calls=new Calls();
  calls.Hook=delegate(ActorEngineCall d,ActorEngineContext s){
   if(d.Role=="AuxFirst"&&d.IsAllocation)published=s.Server678.HasValue&&s.Stub680==0x8765432100000001UL&&s.Current668==0;
   if(d.Role=="AuxThird"&&!d.IsAllocation)s.Stub680=s.Server678;
   return Normal(d);
  };
  var r=new ActorEngineInitializationInvocation(c,calls).Run();
  Need(published,"callbacks see Server/Stub published before current selection");
  Need(r.ProjectionReturned&&c.Current668==c.Server678,"late reload uses callback mutation instead of cached Stub result");
  Need(r.Writes[1].Value==0x8765432100000001UL&&r.Writes[2].Value==0x1234567800000001UL,"history keeps original Stub publication distinct from selected current");
  var unknown=Context();var u=new Calls();u.Hook=delegate(ActorEngineCall d,ActorEngineContext s){if(d.Role=="AuxThird"&&!d.IsAllocation)s.Stub680=null;return Normal(d);};
  var ur=new ActorEngineInitializationInvocation(unknown,u).Run();
  Need(ur.ProjectionReturned&&unknown.Current668==null&&ur.Writes[2].Value==null,"unknown late field remains an explicit unknown write");
 }
 static void FailurePrefixes(){
  for(int failure=1;failure<=10;failure++){
   var c=Context();var calls=new Calls();int at=failure;
   calls.Hook=delegate(ActorEngineCall d,ActorEngineContext s){return calls.Count==at?new ActorEngineCallObservation(false,null):Normal(d);};
   var r=new ActorEngineInitializationInvocation(c,calls).Run();
   int stores=failure<=2?0:failure<=4?1:2;
   Need(!r.ProjectionReturned&&r.Reason=="UnresolvedEngineCallReturn"&&r.Calls.Count==failure&&r.Writes.Count==stores&&c.Current668==0,
    "no later work after unresolved call "+failure);
  }
  var noReport=new Calls();noReport.Hook=delegate{return null;};var missing=new ActorEngineInitializationInvocation(Context(),noReport).Run();
  Need(missing.Reason=="MissingEngineCallObservation"&&missing.Calls.Count==1&&missing.Writes.Count==0,"missing report stops before publication");
 }
 static void UnknownValuesAndZeroInputs(){
  var calls=new Calls();calls.Hook=delegate(ActorEngineCall d,ActorEngineContext s){return new ActorEngineCallObservation(true,null);};
  var r=new ActorEngineInitializationInvocation(Context(),calls).Run();
  Need(r.Reason=="MissingEngineAllocationIdentity"&&r.ResumeInstructionRva==0x74DAC&&calls.Count==1&&r.Writes.Count==0,"unknown allocation cannot silently become constructor this");
  foreach(string role in new string[]{"Server","Stub"}){
   var c=Context();var x=new Calls();x.Hook=delegate(ActorEngineCall d,ActorEngineContext s){return d.Role==role&&!d.IsAllocation?new ActorEngineCallObservation(true,null):Normal(d);};
   var u=new ActorEngineInitializationInvocation(c,x).Run();
   Need(u.Reason=="MissingEngineConstructorReturnIdentity"&&u.Writes[u.Writes.Count-1].Value==null&&u.ResumeInstructionRva==(role=="Server"?0x74DB9U:0x74DDAU),"known return with unknown identity stores unknown for "+role);
   Need(c.Current668==0&&(role=="Server"?c.Server678==null:c.Server678==0x1234567800000001UL&&c.Stub680==null),"unknown identity keeps previous prefix and never stale field for "+role);
  }
  var zeros=new Calls();bool sawZero=false;zeros.Hook=delegate(ActorEngineCall d,ActorEngineContext s){if(!d.IsAllocation)sawZero|=d.ThisToken==0;return new ActorEngineCallObservation(true,0);};
  var z=Context();var zr=new ActorEngineInitializationInvocation(z,zeros).Run();
  Need(sawZero&&zr.ProjectionReturned&&z.Current668==0&&z.Server678==0,"observed zero input is retained; fixture is not evidence that native zero-this can succeed");
 }
 static void OwnExceptionsAndReplay(){
  var c=Context();var calls=new Calls();var error=new Exception("own callback");
  calls.Hook=delegate(ActorEngineCall d,ActorEngineContext s){if(d.Role=="Stub")throw error;return Normal(d);};
  var invocation=new ActorEngineInitializationInvocation(c,calls);bool same=false,replay=false;
  try{invocation.Run();}catch(Exception ex){same=Object.ReferenceEquals(ex,error);}
  Need(same&&invocation.Result.Reason=="EngineCallNotCompleted"&&invocation.Result.Calls.Count==3&&invocation.Result.Calls[2].Observation==null,"own exception propagates with active call trace");
  Need(c.Server678==0x1234567800000001UL&&c.Stub680==0&&invocation.Result.Writes.Count==1,"already published Server survives own exception");
  try{invocation.Run();}catch(InvalidOperationException){replay=true;}
  Need(replay&&calls.Count==3,"attempt cannot replay prior effects");
  var next=new ActorEngineInitializationInvocation(c,new Calls()).Run();
  Need(next.ProjectionReturned&&c.Writes.Count==7,"new invocation may explicitly reuse own state");
 }
 static void SlotSampleAndLateBinding(){
  var a=Context();var slot=new ActorEngineSlot(a);var snapshot=slot.Read();a.Current668=8;
  Need(snapshot.Value668==0&&slot.Read().Value668==8,"separate reads are live; completed observations remain immutable");
  var b=new ActorEngineContext(0x610000UL,9,9,10);var f=new Fixture();var matrix=new Matrix();
  matrix.Hook=delegate(int call){if(call==2)slot.Publish(b);};
  var r=Complete(f,matrix,slot);
  Need(r.NormalReturnReached&&r.GlobalStateSourceReads==1&&matrix.Count==2&&f.Prior.State.ReadByte(0x4E1)==1,"binding sampled at tail after matrix callback, ignoring old supplied snapshot");
  Need(r.MatrixInitializationReturns==1&&r.MatrixWarmReturns==1&&r.ReturnedActorToken==0x500000UL&&!r.IsCompleteNativeConstructor,"cold then warm own matrix connects to conditional Actor return");
  b.Current668=2;var second=new Fixture();Complete(second,new Matrix(),slot);
  Need(second.Prior.State.ReadByte(0x4E1)==0,"shared slot does not cache fields across Actor calls");
 }
 static void UnknownTailStages(){
  var contexts=new ActorEngineContext[]{null,new ActorEngineContext(0x600000UL,7,null,0),new ActorEngineContext(0x600000UL,null,7,0)};
  string[] reasons={"MissingGlobalSlotObservation","MissingGlobal678","MissingGlobal668"};
  for(int i=0;i<contexts.Length;i++){
   var f=new Fixture();var r=Complete(f,new Matrix(),new ActorEngineSlot(contexts[i]));
   Need(!r.NormalReturnReached&&r.Reason==reasons[i]&&r.GlobalStateSourceReads==1&&r.GlobalSlotRead==(i!=0)&&r.Global678Read==(i==2)&&!r.Global668Read,"ordered unknown tail stop "+i);
   Need(r.ActorObjectPublished&&f.Prior.State.ReadLittleEndian(0x1E0,8)==0x200000UL&&f.Prior.State.ReadByte(0x4E1)==0xA5,"tail failure preserves earlier construction and unwritten flag "+i);
  }
  var f0=new Fixture();var missing=Complete(f0,new Matrix(),null);
  Need(missing.Reason=="MissingGlobalStateSource"&&missing.GlobalStateSourceReads==0&&!missing.GlobalSlotRead,"missing adapter is distinct from unbound slot");
 }
 static void SourceTimingAndExceptions(){
  var source=new Global();source.Hook=delegate{throw new Exception("must remain unused");};
  var early=new Fixture();var stopped=Complete(early,new Matrix{Fail=true},source);
  Need(stopped.Reason=="OwnMatrixStop"&&source.Count==0&&stopped.GlobalStateSourceReads==0,"early constructor stop never samples global source");
  var error=new Exception("global own");source.Hook=delegate{throw error;};var f=new Fixture();bool caught=false;
  try{Complete(f,new Matrix(),source);}catch(Exception ex){caught=Object.ReferenceEquals(ex,error);}
  Need(caught&&source.Count==1&&f.Prior.State.ReadLittleEndian(0x1E0,8)==0x200000UL&&f.Prior.State.ReadByte(0x4E1)==0xA5,"own read exception leaves exact earlier prefix, no native EH claim");
  var absent=new Global();absent.Hook=delegate{return null;};var other=new Fixture();var r=Complete(other,new Matrix(),absent);
  Need(r.Reason=="MissingGlobalSlotObservation"&&r.GlobalStateSourceReads==1&&!r.GlobalSlotRead,"null source report remains unknown slot");
 }
 public static int Main(){
  Group("OwnCreationAndIdentity",OwnCreationAndIdentity);
  Group("CallArgumentsAndPublication",CallArgumentsAndPublication);
  Group("LateStubRead",LateStubRead);
  Group("FailurePrefixes",FailurePrefixes);
  Group("UnknownValuesAndZeroInputs",UnknownValuesAndZeroInputs);
  Group("OwnExceptionsAndReplay",OwnExceptionsAndReplay);
  Group("SlotSampleAndLateBinding",SlotSampleAndLateBinding);
  Group("UnknownTailStages",UnknownTailStages);
  Group("SourceTimingAndExceptions",SourceTimingAndExceptions);
  Console.WriteLine("ActorEngineContextTests: "+assertions+" assertions, "+groups+" groups passed; own conditional projection only");return 0;
 }
}
