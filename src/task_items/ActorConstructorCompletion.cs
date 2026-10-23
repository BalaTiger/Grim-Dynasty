using System;
namespace GrimDynasty.TaskItems {
 // Own bounded observations, never native pointers. Normal managed execution,
 // valid exclusive nonoverlapping storage and initial malloc success only.
 public sealed class ActorConstructorStorage {
  public readonly ulong BaseToken;
  readonly byte?[] bytes;
  public int ByteCount {get{return bytes.Length;}}
  public ActorConstructorStorage(ulong token,int byteCount):this(token,Make(byteCount)){}
  public ActorConstructorStorage(ulong token,byte?[] initial){
   if(initial==null)throw new ArgumentNullException("initial");
   if(initial.Length!=0x358&&initial.Length!=0x38)throw new ArgumentException("Expected subobject or sentinel extent");
   if(token==0||token>ulong.MaxValue-(ulong)(initial.Length-1))throw new ArgumentOutOfRangeException("token");
   BaseToken=token;bytes=(byte?[])initial.Clone();
  }
  static byte?[] Make(int n){if(n!=0x358&&n!=0x38)throw new ArgumentOutOfRangeException("byteCount");return new byte?[n];}
  void Check(int at,int width){if(width<1||width>8||at<0||at>bytes.Length-width)throw new ArgumentOutOfRangeException("at");}
  public byte? ReadByte(int at){Check(at,1);return bytes[at];}
  public ulong? ReadLittleEndian(int at,int width){Check(at,width);ulong v=0;for(int i=0;i<width;i++){if(!bytes[at+i].HasValue)return null;v|=(ulong)bytes[at+i].Value<<(8*i);}return v;}
  public byte?[] CopyObservation(){return (byte?[])bytes.Clone();}
  internal void Store(int at,int width,ulong value){Check(at,width);for(int i=0;i<width;i++)bytes[at+i]=unchecked((byte)(value>>(8*i)));}
 }
 public sealed class ActorMatrixWarmObservation {
  // This asserts validity of the GS:[58]/420DD4/TLS block path, not its recovery.
  public readonly bool ValidTlsPathObserved;
  public readonly int? ThreadEpoch,GuardEpoch;
  readonly byte?[] source;
  public ActorMatrixWarmObservation(bool validTlsPath,int? threadEpoch,int? guardEpoch,byte?[] source48){
   if(source48!=null&&source48.Length!=48)throw new ArgumentException("Expected 48 observed bytes");
   ValidTlsPathObserved=validTlsPath;ThreadEpoch=threadEpoch;GuardEpoch=guardEpoch;
   source=source48==null?null:(byte?[])source48.Clone();
  }
  internal bool TryChunk(int at,out ulong low,out ulong high){
   low=0;high=0;if(source==null)return false;
   for(int i=0;i<16;i++){if(!source[at+i].HasValue)return false;ulong b=source[at+i].Value;if(i<8)low|=b<<(8*i);else high|=b<<(8*(i-8));}return true;
  }
 }
 public sealed class ActorGlobalObservation {
  // Values at their respective native reads. Null means unknown, not zero.
  public readonly ulong? ObjectToken,Value678,Value668;
  public ActorGlobalObservation(ulong? objectToken,ulong? value678,ulong? value668){ObjectToken=objectToken;Value678=value678;Value668=value668;}
 }
 // Called at the Actor tail, after earlier constructor callbacks. A source must
 // supply values for this read sequence under its own exclusive access contract.
 // This does not assert atomic sampling or lifetime safety in the native engine.
 public interface IActorGlobalStateSource { ActorGlobalObservation Read(); }
 // Optional own state source. Read only when a native matrix call site is reached;
 // successful results distinguish a warm return from executed initialization.
 public interface IActorMatrixStateSource { ActorMatrixReadResult Read(); }
 public sealed class ActorMatrixReadResult {
  public readonly bool Returned;
  public readonly string Path,Reason;
  public readonly uint StopRva;
  readonly byte?[] source;
  ActorMatrixReadResult(bool returned,string path,string reason,uint stopRva,byte?[] bytes){
   if(bytes!=null&&bytes.Length!=48)throw new ArgumentException("Expected 48 source bytes");
   Returned=returned;Path=path;Reason=reason;StopRva=stopRva;source=bytes==null?null:(byte?[])bytes.Clone();
  }
  public static ActorMatrixReadResult Success(string path,byte?[] bytes){
   if(path!="Warm"&&path!="Initialized"&&path!="HeaderExisting")throw new ArgumentException("Unknown return path");
   return new ActorMatrixReadResult(true,path,null,0,bytes);
  }
  public static ActorMatrixReadResult Unsupported(string reason,uint stopRva){return new ActorMatrixReadResult(false,"Stopped",reason,stopRva,null);}
  internal bool TryChunk(int at,out ulong low,out ulong high){
   low=0;high=0;if(source==null)return false;
   for(int i=0;i<16;i++){if(!source[at+i].HasValue)return false;ulong b=source[at+i].Value;if(i<8)low|=b<<(8*i);else high|=b<<(8*(i-8));}return true;
  }
 }
 public sealed class ActorCompletionObservations {
  internal readonly byte[] Vector34C1E0,Scale34C3B0;
  public readonly uint? SubobjectUnitBits,ActorUnitBits;
  public readonly ActorMatrixWarmObservation FirstMatrix,SecondMatrix;
  public readonly ActorGlobalObservation Global;
  public readonly string ConstantProvenance;
  public ActorCompletionObservations(byte[] vector16,uint? subobjectUnit,ActorMatrixWarmObservation first,
   byte[] scale16,ActorMatrixWarmObservation second,uint? actorUnit,ActorGlobalObservation global)
   :this(vector16,subobjectUnit,first,scale16,second,actorUnit,global,"SuppliedObservations"){}
  ActorCompletionObservations(byte[] vector,uint? subunit,ActorMatrixWarmObservation first,byte[] scale,
   ActorMatrixWarmObservation second,uint? unit,ActorGlobalObservation global,string provenance){
   if(vector!=null&&vector.Length!=16)throw new ArgumentException("Expected 16-byte vector observation");
   if(scale!=null&&scale.Length!=16)throw new ArgumentException("Expected 16-byte scale observation");
   Vector34C1E0=vector==null?null:(byte[])vector.Clone();Scale34C3B0=scale==null?null:(byte[])scale.Clone();
   SubobjectUnitBits=subunit;ActorUnitBits=unit;FirstMatrix=first;SecondMatrix=second;Global=global;ConstantProvenance=provenance;
  }
  public static ActorCompletionObservations WithArchivedConstants(ActorMatrixWarmObservation first,ActorMatrixWarmObservation second,ActorGlobalObservation global){
   return new ActorCompletionObservations(new byte[]{0,0,128,63,0,0,0,0,0,0,0,0,0,0,128,63},
    0x3F800000U,first,ActorConstructorContinuation.CopyArchivedConstant(),second,0x3F800000U,global,"ArchivedFileImage");
  }
 }
 public sealed class ActorCompletionResult {
  public readonly ActorContinuationResult Prior;
  public readonly ActorConstructorPrefixState State;
  public readonly ActorConstructorStorage Subobject,Sentinel;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public int SubobjectConstructorStores {get;internal set;}
  public int SentinelStores {get;internal set;}
  public int ActorStoresAdded {get;internal set;}
  public int BackReferenceStores {get;internal set;}
  public int TotalActorStores {get{return (Prior==null?0:Prior.TotalActorStoreInstructions)+ActorStoresAdded;}}
  public int NewPreElementCalls {get;internal set;}
  public int MatrixHelperCalls {get;internal set;}
  public int MatrixWarmReturns {get;internal set;}
  public int MatrixStateSourceCalls {get;internal set;}
  public int MatrixInitializationReturns {get;internal set;}
  public int MatrixHeaderExistingReturns {get;internal set;}
  public int MatrixChunksCopied {get;internal set;}
  public ulong? RequestedSentinelBytes {get;internal set;}
  public bool KnownInitialSentinelAllocationUsed {get;internal set;}
  public bool ElementReturned {get;internal set;}
  public bool EmptyVectorHelperReturned {get;internal set;}
  public bool SubobjectReturned {get;internal set;}
  public bool ActorObjectPublished {get;internal set;}
  public bool GlobalSlotRead {get;internal set;}
  public int GlobalStateSourceReads {get;internal set;}
  public bool Global678Read {get;internal set;}
  public bool Global668Read {get;internal set;}
  public bool NormalReturnReached {get;internal set;}
  public ulong? ReturnedSubobjectToken {get;internal set;}
  public ulong? ReturnedActorToken {get;internal set;}
  public string ConstantProvenance {get;internal set;}
  public bool ArchivedFileInitialCheckUsed {get{return Prior!=null&&Prior.ArchivedFileInitialCheckUsed;}}
  public bool IsCompleteNativeConstructor {get{return false;}}
  public bool MayHaveEffects {get{return SubobjectConstructorStores!=0||SentinelStores!=0||ActorStoresAdded!=0||(Prior!=null&&Prior.MayHaveEffects);}}
  internal ActorCompletionResult(ActorContinuationResult prior,ActorConstructorStorage sub,ActorConstructorStorage sentinel){
   Prior=prior;State=prior==null?null:prior.State;Subobject=sub;Sentinel=sentinel;Status="Unsupported";ConstantProvenance="NotUsed";
   ResumeInstructionRva=prior==null?0x25CC9:prior.ResumeInstructionRva;
  }
 }
 public static class ActorConstructorCompletion {
  public const ulong MatrixSourcePreferredIdentity=0x18041D490UL;
  public static ActorCompletionResult Continue(ActorContinuationResult prior,ulong actorToken,
   ActorConstructorStorage subobject,ActorConstructorStorage sentinel,ActorCompletionObservations data){
   return ContinueCore(prior,actorToken,subobject,sentinel,data,null,false,null,false);
  }
  public static ActorCompletionResult ContinueWithMatrixState(ActorContinuationResult prior,ulong actorToken,
   ActorConstructorStorage subobject,ActorConstructorStorage sentinel,ActorCompletionObservations data,IActorMatrixStateSource source){
   return ContinueCore(prior,actorToken,subobject,sentinel,data,source,true,null,false);
  }
  public static ActorCompletionResult ContinueWithStateSources(ActorContinuationResult prior,ulong actorToken,
   ActorConstructorStorage subobject,ActorConstructorStorage sentinel,ActorCompletionObservations data,
   IActorMatrixStateSource matrixSource,IActorGlobalStateSource globalSource){
   return ContinueCore(prior,actorToken,subobject,sentinel,data,matrixSource,true,globalSource,true);
  }
  static ActorCompletionResult ContinueCore(ActorContinuationResult prior,ulong actorToken,
   ActorConstructorStorage subobject,ActorConstructorStorage sentinel,ActorCompletionObservations data,IActorMatrixStateSource source,bool useState,
   IActorGlobalStateSource globalSource,bool useGlobalState){
   var r=new ActorCompletionResult(prior,subobject,sentinel);
   if(prior==null||prior.State==null)return Stop(r,"MissingPriorActorState",r.ResumeInstructionRva);
   if(prior.Status!="ConditionalPrefix"||prior.ResumeInstructionRva!=0x25CC9||!prior.KnownInitialAllocationAccepted||!prior.ArchivedFileInitialCheckUsed)
    return Stop(r,"PriorConstructorStageIncomplete",prior.ResumeInstructionRva);
   if(actorToken==0||actorToken>ulong.MaxValue-(ActorConstructorPrefixState.CapturedExtent-1UL))return Stop(r,"InvalidActorIdentity",0x25CC9);
   if(subobject==null)return Stop(r,"MissingSubobjectStorage",0x25CC9);
   if(subobject.ByteCount!=0x358||subobject.BaseToken!=prior.PendingConstructorThisToken)return Stop(r,"SubobjectAllocationIdentityMismatch",0x25CC9);
   // BDBE0 entry. Do not clear any unobserved gaps or padding.
   S(r,0,8,0x18033F908UL);for(int at=0x68;at<=0xD8;at+=8)S(r,at,8,0);
   // Same explicitly archived check profile as the prior; not a loaded guard claim.
   r.NewPreElementCalls=1;
   int e=0x110;
   S(r,e+0x18,1,0);S(r,e+0x1C,4,0);S(r,e+0x10,8,0);S(r,e+0x20,4,0x3F800000);
   S(r,e+0x24,8,0);S(r,e+0x2C,4,0);S(r,e+0x30,8,0);S(r,e+0x38,8,0);S(r,e+0x40,8,0);
   // 038500 -> 01F170(EDX=R8D=0) still allocates 56 bytes.
   r.RequestedSentinelBytes=0x38;
   if(sentinel==null)return Stop(r,"MissingKnownInitialSentinelAllocation",0x1F185);
   if(sentinel.ByteCount!=0x38)return Stop(r,"InvalidSentinelStorage",0x1F185);
   sentinel.Store(0,8,sentinel.BaseToken);sentinel.Store(8,8,sentinel.BaseToken);r.SentinelStores=2;r.KnownInitialSentinelAllocationUsed=true;
   S(r,e+0x38,8,sentinel.BaseToken);S(r,e+0x50,8,0);S(r,e+0x58,4,0x1F7);S(r,e+0x48,8,0);S(r,e+8,8,0);S(r,e+0x60,1,0);r.ElementReturned=true;
   S(r,0x178,8,0);S(r,0x180,4,0x1F7);S(r,0x258,8,0);S(r,0x260,8,0);S(r,0x268,8,0);
   S(r,0x270,8,0);S(r,0x278,8,0);S(r,0x280,8,0);
   // 0481F0 count-zero branch repeats all three stores and returns normally.
   S(r,0x270,8,0);S(r,0x278,8,0);S(r,0x280,8,0);r.EmptyVectorHelperReturned=true;
   S(r,0x288,8,0);S(r,0x2A0,8,0);S(r,0x2A8,8,0);S(r,0x2B0,8,0);S(r,0x2D4,8,1);
   if(data==null||data.Vector34C1E0==null)return Stop(r,"MissingSubobjectVector",0xBDD0B);
   r.ConstantProvenance=data.ConstantProvenance;V(r,0x2DC,data.Vector34C1E0);
   S(r,0x2F8,8,0);S(r,0x2D0,2,0);S(r,0x2F0,8,0);S(r,0x300,1,0);S(r,0x310,4,0x3F800000);
   S(r,0x30C,4,1);S(r,0x308,2,0);S(r,0x314,1,0);S(r,0x318,8,0);S(r,0x320,8,0);S(r,0x328,8,0);
   S(r,0x348,8,15);S(r,0x340,8,0);S(r,0x330,1,0);
   S(r,0x2C4,4,0x3F800000);S(r,0x2C8,4,0x3F800000);S(r,0x2B8,8,0);S(r,0x2C0,4,0);S(r,0x38,4,0);S(r,0x3C,1,0);
   S(r,8,8,0);S(r,0x10,8,0);S(r,0x18,8,0);S(r,0x40,8,0);S(r,0x48,8,0);S(r,0x50,8,0);S(r,0x58,8,0);S(r,0x60,8,0);S(r,0x188,8,0);
   if(!data.SubobjectUnitBits.HasValue)return Stop(r,"MissingSubobjectUnit",0xBDDDB);
   S(r,0x19C,8,Repeat(data.SubobjectUnitBits.Value));S(r,0x1A4,4,0x3F800000);S(r,0x190,8,0);S(r,0x198,4,0);
   if(!Matrix(r,data.FirstMatrix,0xE0,new uint[]{0xBDE27,0xBDE31,0xBDE3C},source,useState))return r;
   if(data.Scale34C3B0==null)return Stop(r,"MissingSubobjectScale",0xBDE47);
   V(r,0x22C,data.Scale34C3B0);S(r,0x23C,4,0);S(r,0x240,1,0);V(r,0x244,data.Scale34C3B0);S(r,0x254,4,0);
   S(r,0x290,1,1);S(r,0x294,8,0);S(r,0x1A8,8,0);S(r,0x1B0,4,0);S(r,0x1B4,8,0);S(r,0x1BC,4,0);
   if(!Matrix(r,data.SecondMatrix,0x1E4,new uint[]{0xBDEBE,0xBDEC8,0xBDED3},source,useState))return r;
   S(r,0x1D8,8,0);S(r,0x1E0,4,0);S(r,0x214,8,0);S(r,0x21C,4,0);S(r,0x220,8,0);S(r,0x228,4,0);
   S(r,0x30,8,0);S(r,0x28,8,0);S(r,0x20,8,0);S(r,0x350,2,1);
   r.SubobjectReturned=true;r.ReturnedSubobjectToken=subobject.BaseToken;
   // Actual proven return identity is now available for caller publication.
   A(r,0x1E0,8,r.ReturnedSubobjectToken.Value);r.ActorObjectPublished=true;A(r,0x248,1,1);A(r,0x4C0,1,1);
   subobject.Store(0x118,8,actorToken);subobject.Store(0x110,4,0);r.BackReferenceStores=2;
   A(r,0x4B4,4,0xFFFFFFFFUL);A(r,0x4B8,8,0);A(r,0x4C8,8,0);A(r,0x4D0,4,0);A(r,0x488,4,0);
   if(!data.ActorUnitBits.HasValue)return Stop(r,"MissingActorUnit",0x25D15);
   A(r,0x49C,8,Repeat(data.ActorUnitBits.Value));A(r,0x4A4,4,0x3F800000);
   ActorGlobalObservation global=data.Global;
   if(useGlobalState){
    if(globalSource==null)return Stop(r,"MissingGlobalStateSource",0x25D3D);
    r.GlobalStateSourceReads++;global=globalSource.Read();
   }
   if(global==null||!global.ObjectToken.HasValue)return Stop(r,"MissingGlobalSlotObservation",0x25D3D);
   r.GlobalSlotRead=true;
   if(global.ObjectToken.Value==0||global.ObjectToken.Value>ulong.MaxValue-0x67FUL)return Stop(r,"InvalidGlobalObjectIdentity",0x25D44);
   if(!global.Value678.HasValue)return Stop(r,"MissingGlobal678",0x25D44);r.Global678Read=true;
   if(!global.Value668.HasValue)return Stop(r,"MissingGlobal668",0x25D4B);r.Global668Read=true;
   A(r,0x4E1,1,global.Value678.Value==global.Value668.Value?1UL:0UL);
   A(r,0x4E2,1,0);A(r,0x4E0,1,0);A(r,0x4E4,4,0);
   r.State.Store(0x48C,8,0);r.State.Store(0x494,8,0);r.ActorStoresAdded++;
   A(r,0x528,4,0);r.ReturnedActorToken=actorToken;r.NormalReturnReached=true;
   r.Status="ConditionalNormalReturn";r.Reason=null;r.ResumeInstructionRva=0x25D8C;return r;
  }
  static bool Matrix(ActorCompletionResult r,ActorMatrixWarmObservation m,int target,uint[] loadSites,IActorMatrixStateSource source,bool useState){
   r.MatrixHelperCalls++;
   if(useState){
    if(source==null){Stop(r,"MissingMatrixStateSource",0x273AB4);return false;}
    r.MatrixStateSourceCalls++;ActorMatrixReadResult result=source.Read();
    if(result==null){Stop(r,"MissingMatrixStateResult",0x273AB4);return false;}
    if(!result.Returned){Stop(r,result.Reason,result.StopRva);return false;}
    if(result.Path=="Warm")r.MatrixWarmReturns++;
    else if(result.Path=="Initialized")r.MatrixInitializationReturns++;
    else r.MatrixHeaderExistingReturns++;
    for(int i=0;i<3;i++){
     ulong low,high;if(!result.TryChunk(i*16,out low,out high)){Stop(r,"MissingMatrixSourceChunk",loadSites[i]);return false;}
     r.Subobject.Store(target+i*16,8,low);r.Subobject.Store(target+i*16+8,8,high);r.SubobjectConstructorStores++;r.MatrixChunksCopied++;
    }
    return true;
   }
   if(m==null||!m.ValidTlsPathObserved){Stop(r,"MissingValidTlsObservation",0x273AB4);return false;}
   if(!m.ThreadEpoch.HasValue){Stop(r,"MissingThreadEpoch",0x273ACC);return false;}
   if(!m.GuardEpoch.HasValue){Stop(r,"MissingMatrixGuardEpoch",0x273ACF);return false;}
   if(m.GuardEpoch.Value>m.ThreadEpoch.Value){Stop(r,"UnresolvedMatrixInitialization",0x273AEA);return false;}
   r.MatrixWarmReturns++;
   for(int i=0;i<3;i++){
    ulong low,high;if(!m.TryChunk(i*16,out low,out high)){Stop(r,"MissingMatrixSourceChunk",loadSites[i]);return false;}
    r.Subobject.Store(target+i*16,8,low);r.Subobject.Store(target+i*16+8,8,high);r.SubobjectConstructorStores++;r.MatrixChunksCopied++;
   }
   return true;
  }
  static ActorCompletionResult Stop(ActorCompletionResult r,string reason,uint rva){r.Status="Unsupported";r.Reason=reason;r.ResumeInstructionRva=rva;return r;}
  static void S(ActorCompletionResult r,int at,int width,ulong value){r.Subobject.Store(at,width,value);r.SubobjectConstructorStores++;}
  static void A(ActorCompletionResult r,int at,int width,ulong value){r.State.Store(at,width,value);r.ActorStoresAdded++;}
  static void V(ActorCompletionResult r,int at,byte[] bytes){r.Subobject.Store(at,8,Bits(bytes,0));r.Subobject.Store(at+8,8,Bits(bytes,8));r.SubobjectConstructorStores++;}
  static ulong Bits(byte[] b,int at){ulong v=0;for(int i=0;i<8;i++)v|=(ulong)b[at+i]<<(i*8);return v;}
  static ulong Repeat(uint value){return value|((ulong)value<<32);}
 }
}
