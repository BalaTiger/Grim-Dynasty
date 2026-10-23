using System;
using System.Collections.Generic;

namespace GrimDynasty.CombatRuntime {
 // Own sparse observations relative to the attribute subobject. One offset
 // has one mutable DWORD identity, including when two index expressions alias.
 // No length of 58, native allocation, initialization or lifetime is inferred.
 public sealed class NativeAttributeView {
  readonly Dictionary<long,uint?> words=new Dictionary<long,uint?>();
  public bool IsValid {get;private set;}
  public NativeAttributeView(){IsValid=true;}
  public void SetObservedBits(long offset,uint? bits){
   if(!IsValid)throw new InvalidOperationException("Attribute view invalidated");
   if(offset%4!=0)throw new ArgumentOutOfRangeException("offset","A DWORD observation must be aligned relative to this view");
   words[offset]=bits;
  }
  public void SetObservedFloat(long offset,float? value){SetObservedBits(offset,value.HasValue?(uint?)NativeAttributeGetter.FloatBits(value.Value):null);}
  public uint? ReadObservedBits(long offset){uint? bits;return IsValid&&words.TryGetValue(offset,out bits)?bits:null;}
  public void Invalidate(){IsValid=false;}
 }
 public sealed class NativeAttributeConstants {
  public const uint RecordedOneBits=0x3F800000,RecordedK1Bits=0x42C80000,RecordedK2Bits=0x3C23D70A;
  public readonly uint? OneBits,K1Bits,K2Bits;
  // Partial observations are permitted, but a supplied bit pattern must match
  // the recorded profile when its instruction reads it. Other constant profiles
  // are not recovered by this model. In particular K2 is binary32, not double .01.
  public NativeAttributeConstants(uint? oneBits,uint? k1Bits,uint? k2Bits){OneBits=oneBits;K1Bits=k1Bits;K2Bits=k2Bits;}
  public static NativeAttributeConstants FromRecordedSnapshot(){return new NativeAttributeConstants(RecordedOneBits,RecordedK1Bits,RecordedK2Bits);}
 }
 public enum NativeAttributeStatus { Unsupported,ConditionalNormalReturn }
 public sealed class NativeAttributeResult {
  public readonly NativeAttributeView Fields;
  public readonly int? Index;
  public NativeAttributeStatus Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public long? FailingOffset {get;internal set;}
  // Own observation counts, not native instrumentation. Missing observations
  // count as attempts. No stores or calls occur in the covered native branch.
  public int FieldReads {get;internal set;}
  public int ConstantReads {get;internal set;}
  public long? ObservedCOffset {get;internal set;}
  public long? ObservedAOffset {get;internal set;}
  public long? ObservedBOffset {get;internal set;}
  // Completed register values, useful for diagnosing the precise rounding
  // prefix. These are observations of own arithmetic, not object fields.
  public uint? CPlusOneBits {get;internal set;}
  public uint? ScaledCBits {get;internal set;}
  public uint? MultiplierBits {get;internal set;}
  public uint? ScaledBBits {get;internal set;}
  public uint? BPlusOneBits {get;internal set;}
  public uint? AfterBaseBits {get;internal set;}
  public uint? AfterMultiplierBits {get;internal set;}
  public uint? BeforeFinalMaxBits {get;internal set;}
  public uint? ReturnedBits {get;internal set;}
  public float? Value {get{return ReturnedBits.HasValue?(float?)NativeAttributeGetter.FromBits(ReturnedBits.Value):null;}}
  internal NativeAttributeResult(NativeAttributeView fields,int? index){
   Fields=fields;Index=index;Status=NativeAttributeStatus.Unsupported;Reason="NotRun";ResumeInstructionRva=NativeAttributeGetter.EntryRva;
  }
 }
 public static class NativeAttributeGetter {
  public const uint EntryRva=0x0CF6E0,ReturnRva=0x0CF73D;
  public const uint OneOperandRva=0x7A8054,K1OperandRva=0x7A8134,K2OperandRva=0x7A7FC8;
  public static uint FloatBits(float value){return unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value),0));}
  public static float FromBits(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static bool Finite(float value){return !Single.IsNaN(value)&&!Single.IsInfinity(value);}
  static float Round32(double value){return BitConverter.ToSingle(BitConverter.GetBytes((float)value),0);}
  static NativeAttributeResult Stop(NativeAttributeResult r,string reason,uint at){r.Reason=reason;r.ResumeInstructionRva=at;return r;}
  static bool Constant(NativeAttributeResult r,uint? bits,uint expected,string name,uint at,out float value){
   r.ConstantReads++;value=0;
   if(!bits.HasValue){Stop(r,"Unknown"+name+"Constant",at);return false;}
   if(bits.Value!=expected){Stop(r,"Unexpected"+name+"SnapshotBits",at);return false;}
   value=FromBits(bits.Value);return true;
  }
  static bool Field(NativeAttributeResult r,NativeAttributeView fields,long offset,string name,uint at,out float value){
   r.FieldReads++;value=0;
   if(name=="C")r.ObservedCOffset=offset;else if(name=="A")r.ObservedAOffset=offset;else r.ObservedBOffset=offset;
   if(fields==null){r.FailingOffset=offset;Stop(r,"MissingAttributeView",at);return false;}
   if(!fields.IsValid){r.FailingOffset=offset;Stop(r,"InvalidatedAttributeView",at);return false;}
   uint? bits=fields.ReadObservedBits(offset);
   if(!bits.HasValue){r.FailingOffset=offset;Stop(r,"Unknown"+name+"Field",at);return false;}
   value=FromBits(bits.Value);return true;
  }
  static bool Arithmetic(NativeAttributeResult r,float a,float b,bool multiply,string name,uint at,out float value){
   value=0;
   if(!Finite(a)||!Finite(b)){Stop(r,"Nonfinite"+name+"OperandOutsideCoverage",at);return false;}
   // Binary32 products are exact in binary64; binary64 addition followed by
   // RN32 also produces binary32 nearest/even addition for these finite inputs.
   // Each call forces the native instruction's separate binary32 rounding.
   value=Round32(multiply?(double)a*(double)b:(double)a+(double)b);
   if(!Finite(value)){Stop(r,name+"ResultOutsideCoverage",at);return false;}return true;
  }

  // Conditions: recorded constants still apply (no runtime patches), stable
  // masked RN-even with DAZ/FTZ off, serial access, and explicit field validity.
  // The getter never queries or changes MXCSR. It covers finite operands and
  // finite results, including signed zero/gradual underflow, only on A>=0.
  // Negative A stops at CF718 AFTER all earlier loads/arithmetic; the saved
  // negative continuation is incomplete. Unsupported is a coverage boundary,
  // not an invented native bounds check, fault, error return or default value.
  public static NativeAttributeResult Evaluate(NativeAttributeView fields,int? index,NativeAttributeConstants constants,bool snapshotApplicable,bool normalFpEnvironment){
   var r=new NativeAttributeResult(fields,index);
   if(!snapshotApplicable)return Stop(r,"ConstantSnapshotApplicabilityUnobserved",EntryRva);
   if(constants==null)return Stop(r,"MissingConstantSnapshot",EntryRva);
   float one;
   if(!Constant(r,constants.OneBits,NativeAttributeConstants.RecordedOneBits,"One",0x0CF6E0,out one))return r;
   if(!index.HasValue)return Stop(r,"UnknownSignedIndex",0x0CF6EB);
   // EDX is sign-extended. Every int index times four plus these offsets fits
   // Int64; never wrap through int or uint, and never assume an array bound.
   long offset=4L*(long)index.Value;
   float c,a,b;
   if(!Field(r,fields,offset+0x1D0,"C",0x0CF6EE,out c))return r;
   if(!Field(r,fields,offset,"A",0x0CF6F7,out a))return r;
   if(!normalFpEnvironment)return Stop(r,"FloatingPointEnvironmentUnobserved",0x0CF6FC);
   float cPlusOne;
   if(!Arithmetic(r,c,one,false,"CPlusOne",0x0CF6FC,out cPlusOne))return r;
   r.CPlusOneBits=FloatBits(cPlusOne);
   if(!Finite(a))return Stop(r,"NonfiniteBaseComparisonOutsideCoverage",0x0CF700);
   bool negativeBase=a<0.0f; // COMISS flags survive the following SSE operations
   float k1,scaledC;
   if(!Constant(r,constants.K1Bits,NativeAttributeConstants.RecordedK1Bits,"K1",0x0CF703,out k1))return r;
   if(!Arithmetic(r,cPlusOne,k1,true,"ScaledC",0x0CF703,out scaledC))return r;
   r.ScaledCBits=FloatBits(scaledC);
   if(!Field(r,fields,offset+0xE8,"B",0x0CF70B,out b))return r;
   float multiplier=scaledC>0.0f?scaledC:0.0f; // MAXSS ties return +0 operand
   r.MultiplierBits=FloatBits(multiplier);
   if(negativeBase)return Stop(r,"NegativeBaseBranchOutsideCoverage",0x0CF718);
   float k2,t;
   if(!Constant(r,constants.K2Bits,NativeAttributeConstants.RecordedK2Bits,"K2",0x0CF71A,out k2))return r;
   if(!Arithmetic(r,b,k2,true,"ScaledB",0x0CF71A,out t))return r;
   r.ScaledBBits=FloatBits(t);
   if(!Arithmetic(r,t,one,false,"BPlusOne",0x0CF722,out t))return r;
   r.BPlusOneBits=FloatBits(t);
   if(!Arithmetic(r,t,a,true,"AfterBase",0x0CF726,out t))return r;
   r.AfterBaseBits=FloatBits(t);
   if(!Arithmetic(r,t,multiplier,true,"AfterMultiplier",0x0CF72A,out t))return r;
   r.AfterMultiplierBits=FloatBits(t);
   // The second saved memory operand is still a distinct observation, although
   // this explicitly immutable snapshot supplies identical K2 bits both times.
   if(!Constant(r,constants.K2Bits,NativeAttributeConstants.RecordedK2Bits,"K2",0x0CF72E,out k2))return r;
   if(!Arithmetic(r,t,k2,true,"FinalScaled",0x0CF72E,out t))return r;
   r.BeforeFinalMaxBits=FloatBits(t);
   float result=t>0.0f?t:0.0f;
   r.ReturnedBits=FloatBits(result);r.Status=NativeAttributeStatus.ConditionalNormalReturn;r.Reason="FiniteNonnegativeBranchReturned";r.ResumeInstructionRva=ReturnRva;return r;
  }
 }
 // Direct adapter for the existing settlement Attribute hook. Fields denote
 // this exact owner's +F78 subobject; their initialization/binding is supplied
 // explicitly by the caller, not inferred from character creation or records.
 // No Gate or Final callbacks are installed. It never manufactures normal
 // return from an incomplete getter. The latest result survives the outer
 // AttributeReturnUnobserved stop with its inner RVA/field/rounding diagnosis.
 public sealed class NativeAttributeSettlementBinding {
  public readonly NativeDamageState Owner;
  public readonly NativeAttributeView Fields;
  public readonly NativeAttributeConstants Constants;
  public readonly bool SnapshotApplicable,NormalFpEnvironment;
  public NativeAttributeResult LastResult {get;private set;}
  public long Evaluations {get;private set;}
  public NativeAttributeSettlementBinding(NativeDamageState owner,NativeAttributeView fields,NativeAttributeConstants constants,bool snapshotApplicable,bool normalFpEnvironment){
   Owner=owner;Fields=fields;Constants=constants;SnapshotApplicable=snapshotApplicable;NormalFpEnvironment=normalFpEnvironment;
  }
  public NativeDamageObservation<float> Observe(NativeDamageState caller,int property){
   Evaluations++;
   if(Owner==null||!Object.ReferenceEquals(caller,Owner)){
    LastResult=new NativeAttributeResult(Fields,property){Reason="AttributeOwnerBindingMismatch",ResumeInstructionRva=0x068B60};
   }else LastResult=NativeAttributeGetter.Evaluate(Fields,property,Constants,SnapshotApplicable,NormalFpEnvironment);
   return LastResult.Status==NativeAttributeStatus.ConditionalNormalReturn
    ?new NativeDamageObservation<float>(true,LastResult.Value)
    :new NativeDamageObservation<float>(false,null);
  }
 }
}
