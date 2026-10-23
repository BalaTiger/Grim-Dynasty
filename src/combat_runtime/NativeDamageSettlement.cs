using System;

namespace GrimDynasty.CombatRuntime {
 // Own reusable mutable field view of the saved Game 068AB0 contract. Nullable
 // means unobserved, never a native zero/default. The typed fields preserve the
 // observed widths; this is not a native object allocation or an ABI binding.
 public sealed class NativeDamageState {
  public double? Life;                  // +F00, 8 bytes
  public float? Accumulated;            // +F0C, 4 bytes
  public byte? GateByte;                // +1F72, 1 byte
  public byte? Dirty;                   // +1698, 1 byte
  public float? ThresholdMultiplier;    // +27C8, 4 bytes
  public byte? ThresholdFlag;           // +27D8, 1 byte; this body never clears it
 }
 public sealed class NativeDamageStats {
  public uint? Type;                    // dword at borrowed stats +0
 }
 public sealed class NativeDamageInputs {
  public readonly float? Damage;
  public readonly byte? BypassGate;      // R9B, any nonzero value skips both gates
  public readonly byte? CheckThreshold;  // fifth argument's low byte
  // Explicit condition for this invocation INCLUDING external-call intervals:
  // stable masked SSE, nearest/even, gradual underflow (DAZ/FTZ off), and own
  // serial/exclusive access except synchronous reentry through these hooks.
  // No MXCSR query/change, native flags, or exception emulation is performed.
  public readonly bool NormalFloatingPointEnvironment;
  public NativeDamageInputs(float? damage,byte? bypassGate,byte? checkThreshold,bool normalFloatingPointEnvironment){
   Damage=damage;BypassGate=bypassGate;CheckThreshold=checkThreshold;NormalFloatingPointEnvironment=normalFloatingPointEnvironment;
  }
 }
 public sealed class NativeDamageObservation<T> where T:struct {
  public readonly bool ReturnedNormally;
  public readonly T? Value;
  public NativeDamageObservation(bool returnedNormally,T? value){ReturnedNormally=returnedNormally;Value=value;}
 }
 public sealed class NativeDamageCallObservation {
  public readonly bool ReturnedNormally;
  public NativeDamageCallObservation(bool returnedNormally){ReturnedNormally=returnedNormally;}
 }
 public sealed class NativeDamageHooks {
  // Gate: owner vtable +838, low AL. Attribute: CF6E0(owner+F78,4).
  // Final: 3FE390(owner+1790,original float damage,same borrowed stats).
  // A null hook, null observation, or nonnormal observation is Unsupported.
  // Implementations may mutate the same state/stats and synchronously reenter;
  // the model neither snapshots nor rolls back their effects on failure.
  public Func<NativeDamageState,NativeDamageObservation<byte>> Gate;
  public Func<NativeDamageState,int,NativeDamageObservation<float>> Attribute;
  public Func<NativeDamageState,float,NativeDamageStats,NativeDamageCallObservation> Final;
 }
 public enum NativeDamageStatus { Unsupported,NormalEarlyReturn,ConditionalNormalReturn }
 public sealed class NativeDamageResult {
  public readonly NativeDamageState State;
  public readonly NativeDamageStats Stats;
  public NativeDamageStatus Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public Exception CallbackException {get;internal set;}
  // Own observations/stores in this invocation only, excluding hook or nested
  // invocation effects. These counters are not native execution instrumentation.
  public int FieldReads {get;internal set;}
  public int GateCalls {get;internal set;}
  public int AttributeCalls {get;internal set;}
  public int FinalCalls {get;internal set;}
  public bool LifeWritten {get;internal set;}
  public bool AccumulatedWritten {get;internal set;}
  public bool DirtyWritten {get;internal set;}
  public bool ThresholdFlagWritten {get;internal set;}
  public int StoreInstructions {get{return (LifeWritten?1:0)+(AccumulatedWritten?1:0)+(DirtyWritten?1:0)+(ThresholdFlagWritten?1:0);}}
  public bool IsCompleteApplyDamage {get{return false;}}
  public bool IsEstablishedDeathProducer {get{return false;}}
  internal NativeDamageResult(NativeDamageState state,NativeDamageStats stats){
   State=state;Stats=stats;Status=NativeDamageStatus.Unsupported;Reason="NotRun";ResumeInstructionRva=NativeDamageSettlement.EntryRva;
  }
 }
 public static class NativeDamageSettlement {
  public const uint EntryRva=0x068AB0,ReturnRva=0x068B9A,AttributeTargetRva=0x0CF6E0,FinalTargetRva=0x3FE390;
  public const int AttributeSubobjectOffset=0xF78,FinalSubobjectOffset=0x1790;
  static NativeDamageResult Stop(NativeDamageResult r,string reason,uint at){r.Reason=reason;r.ResumeInstructionRva=at;return r;}
  static NativeDamageResult Early(NativeDamageResult r,string reason){r.Status=NativeDamageStatus.NormalEarlyReturn;r.Reason=reason;r.ResumeInstructionRva=ReturnRva;return r;}
  static bool Finite(double x){return !Double.IsNaN(x)&&!Double.IsInfinity(x);}
  // Force storage widths even on a managed runtime allowing wider expression
  // temporaries. The caller must satisfy the explicit normal FP condition.
  static double Binary64(double x){return BitConverter.ToDouble(BitConverter.GetBytes(x),0);}
  static float Binary32(double x){return BitConverter.ToSingle(BitConverter.GetBytes((float)x),0);}

  // Finite operands/results only. Gradual underflow and signed zero are in the
  // supported own numeric domain; NaN/infinity, overflow, alternate rounding,
  // native FP faults/flags and concurrent accesses remain coverage stops.
  // A stop is an inability to observe/model the next instruction, NOT proof
  // that the native instruction fails or that native faults are atomic.
  public static NativeDamageResult Run(NativeDamageState state,NativeDamageStats stats,NativeDamageInputs input,NativeDamageHooks hooks){
   var r=new NativeDamageResult(state,stats);
   if(state==null)return Stop(r,"MissingState",EntryRva);
   if(input==null)return Stop(r,"MissingInputs",EntryRva);
   if(!input.BypassGate.HasValue)return Stop(r,"UnknownBypassGate",0x068AC8);
   if(input.BypassGate.Value==0){
    if(hooks==null||hooks.Gate==null)return Stop(r,"MissingGateInterface",0x068AD0);
    NativeDamageObservation<byte> gate;
    try{r.GateCalls++;gate=hooks.Gate(state);}catch(Exception e){r.CallbackException=e;return Stop(r,"GateCallbackThrew",0x068AD0);}
    if(gate==null||!gate.ReturnedNormally)return Stop(r,"GateReturnUnobserved",0x068AD0);
    if(!gate.Value.HasValue)return Stop(r,"UnknownGateAl",0x068AD6);
    if(gate.Value.Value!=0)return Early(r,"GateReturnedNonzero");
    r.FieldReads++;
    if(!state.GateByte.HasValue)return Stop(r,"UnknownGateByte",0x068ADE);
    if(state.GateByte.Value!=0)return Early(r,"GateByteNonzero");
   }
   if(!input.Damage.HasValue)return Stop(r,"UnknownDamage",0x068AF0);
   if(!input.NormalFloatingPointEnvironment)return Stop(r,"FloatingPointEnvironmentUnobserved",0x068AF0);
   float damage=input.Damage.Value;
   if(!Finite(damage))return Stop(r,"NonfiniteDamageOutsideCoverage",0x068AF0);
   double widened=(double)damage;
   if(widened>=0.0){
    // Native loads BOTH fields before the first store. Missing accumulator
    // therefore leaves life unchanged; unsupported later narrowing does not.
    r.FieldReads++;
    if(!state.Life.HasValue)return Stop(r,"UnknownLife",0x068AFA);
    double life=state.Life.Value;
    r.FieldReads++;
    if(!state.Accumulated.HasValue)return Stop(r,"UnknownAccumulated",0x068B02);
    float accumulated=state.Accumulated.Value;
    if(!Finite(life))return Stop(r,"NonfiniteLifeOutsideCoverage",0x068B0A);
    double remaining=Binary64(life-widened);
    if(!Finite(remaining))return Stop(r,"LifeResultOutsideCoverage",0x068B0A);
    if(!Finite(accumulated))return Stop(r,"NonfiniteAccumulatedOutsideCoverage",0x068B0E);
    double oldAccumulated=(double)accumulated;
    state.Life=remaining;r.LifeWritten=true; // 068B11, eight bytes, no clamp
    double sum=Binary64(oldAccumulated+widened);
    float rounded=Binary32(sum);
    if(!Finite(rounded))return Stop(r,"AccumulatedResultOutsideCoverage",0x068B1D);
    state.Accumulated=rounded;r.AccumulatedWritten=true; // 068B21, four bytes
   }
   // CMP reads the fifth argument BEFORE the independent dirty store.
   if(!input.CheckThreshold.HasValue)return Stop(r,"UnknownThresholdArgument",0x068B29);
   state.Dirty=1;r.DirtyWritten=true;
   if(input.CheckThreshold.Value!=0){
    if(stats==null)return Stop(r,"MissingStatsReference",0x068B37);
    r.FieldReads++;
    if(!stats.Type.HasValue)return Stop(r,"UnknownStatsType",0x068B37);
    if(stats.Type.Value==1){
     r.FieldReads++;
     if(!state.Life.HasValue)return Stop(r,"UnknownThresholdLife",0x068B3C);
     double currentLife=state.Life.Value;
     if(!Finite(currentLife))return Stop(r,"NonfiniteThresholdLifeOutsideCoverage",0x068B44);
     // MAXSD ties select its second operand: both zero signs become +0.
     // Only the temporary is clamped; tiny positive doubles can round to zero.
     double maximum=currentLife>0.0?currentLife:0.0;
     float thresholdLife=Binary32(maximum);
     if(!Finite(thresholdLife))return Stop(r,"ThresholdLifeResultOutsideCoverage",0x068B48);
     if(!(thresholdLife>0.0f)){
      if(hooks==null||hooks.Attribute==null)return Stop(r,"MissingAttributeInterface",0x068B60);
      NativeDamageObservation<float> attribute;
      try{r.AttributeCalls++;attribute=hooks.Attribute(state,4);}catch(Exception e){r.CallbackException=e;return Stop(r,"AttributeCallbackThrew",0x068B60);}
      if(attribute==null||!attribute.ReturnedNormally)return Stop(r,"AttributeReturnUnobserved",0x068B60);
      if(!attribute.Value.HasValue)return Stop(r,"UnknownAttributeValue",0x068B65);
      // Load the multiplier AFTER the external call, never cache it at entry.
      r.FieldReads++;
      if(!state.ThresholdMultiplier.HasValue)return Stop(r,"UnknownThresholdMultiplier",0x068B65);
      float value=attribute.Value.Value,multiplier=state.ThresholdMultiplier.Value;
      if(!Finite(value)||!Finite(multiplier))return Stop(r,"NonfiniteThresholdOperandOutsideCoverage",0x068B65);
      // A binary32 product is exact in binary64 before its binary32 rounding.
      float threshold=Binary32((double)value*(double)multiplier);
      if(!Finite(threshold))return Stop(r,"ThresholdProductOutsideCoverage",0x068B65);
      if(!(damage<threshold)){state.ThresholdFlag=1;r.ThresholdFlagWritten=true;}
     }
    }
   }
   if(stats==null)return Stop(r,"MissingFinalStatsReference",0x068B80);
   if(hooks==null||hooks.Final==null)return Stop(r,"MissingFinalInterface",0x068B86);
   NativeDamageCallObservation final;
   try{r.FinalCalls++;final=hooks.Final(state,damage,stats);}catch(Exception e){r.CallbackException=e;return Stop(r,"FinalCallbackThrew",0x068B86);}
   if(final==null||!final.ReturnedNormally)return Stop(r,"FinalReturnUnobserved",0x068B86);
   r.Status=NativeDamageStatus.ConditionalNormalReturn;r.Reason="ObservedCallbacksReturned";r.ResumeInstructionRva=ReturnRva;return r;
  }
 }
}
