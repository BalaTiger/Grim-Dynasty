using System;

namespace GrimDynasty.TaskItems {
 // Twelve explicitly observed bytes, starting at GameEngine+1B58. This is an
 // own bounded live view, not a native GameEngine allocation or initialization.
 // In particular the two ctor stores are NOT presumed to survive its later
 // LoadFromDatabase call. No default values or implicit publication are supplied.
 public sealed class ItemGameEngineFields {
  public const int FirstOffset=0x1B58,ByteCount=12;
  public readonly ulong Token;
  readonly byte?[] bytes;
  public bool IsValid {get;private set;}
  public ItemGameEngineFields(ulong token,byte?[] twelveBytes){
   if(token==0||token>ulong.MaxValue-(ulong)(FirstOffset+ByteCount-1))throw new ArgumentOutOfRangeException("token");
   if(twelveBytes==null||twelveBytes.Length!=ByteCount)throw new ArgumentException("Twelve explicit observed bytes required","twelveBytes");
   Token=token;bytes=(byte?[])twelveBytes.Clone();IsValid=true;
  }
  public byte? ReadByte(int relativeOffset){Check(relativeOffset,1);return bytes[relativeOffset];}
  public void SetByte(int relativeOffset,byte? value){Check(relativeOffset,1);bytes[relativeOffset]=value;}
  public void Store(int relativeOffset,int width,ulong value){Check(relativeOffset,width);for(int i=0;i<width;i++)bytes[relativeOffset+i]=unchecked((byte)(value>>(8*i)));}
  public void Invalidate(){IsValid=false;}
  internal uint? ReadDword(int relativeOffset){
   Check(relativeOffset,4);uint value=0;
   for(int i=0;i<4;i++){if(!bytes[relativeOffset+i].HasValue)return null;value|=(uint)bytes[relativeOffset+i].Value<<(8*i);}return value;
  }
  void Check(int relativeOffset,int width){
   if(!IsValid)throw new InvalidOperationException("Borrowed GameEngine fields have been invalidated");
   if(width<1||width>8||relativeOffset<0||relativeOffset>ByteCount-width)throw new ArgumentOutOfRangeException("relativeOffset");
  }
 }
 // Pointer zero, pointer unknown, missing observed backing, and a mismatched
 // backing token are distinct. Publish only changes an OWN slot; it does not
 // recover InitializeObjects, a loaded global, ownership, or native lifetime.
 public sealed class ItemGameEngineSlot {
  public ulong? Pointer;
  public ItemGameEngineFields Fields;
  public ItemGameEngineSlot(ulong? pointer,ItemGameEngineFields fields){Pointer=pointer;Fields=fields;}
  public void Publish(ulong? pointer,ItemGameEngineFields fields){Pointer=pointer;Fields=fields;}
 }
 public sealed class ItemReturnNumericInputs {
  public const uint ScaleOperandRva=0x7A8164,ArchivedScaleBits=0x447A0000;
  // Read afresh at each saved MULSS. These are explicitly supplied observations
  // of a loaded operand, never a claim that its disk bytes cannot change.
  public uint? ScaleBits;
  // Required even in this restricted domain. It denotes the caller's explicit
  // stable normal masked SSE environment with round-to-nearest/even for the
  // entire invocation, including the external-call interval. The model never
  // sets MXCSR, observes native flags, or assumes a callback preserves them.
  public readonly bool NormalMaskedFpEnvironmentObserved;
  public ItemReturnNumericInputs(uint? scaleBits,bool normalMaskedFpEnvironment=false){
   ScaleBits=scaleBits;NormalMaskedFpEnvironmentObserved=normalMaskedFpEnvironment;
  }
  // Explicit selection of the pinned disk operand00007A44 (1000.0f). The
  // caller still asserts that this is the relevant loaded operand, and must
  // separately supply the stable environment condition. It is never automatic.
  public static ItemReturnNumericInputs WithArchivedScale(bool normalMaskedFpEnvironment=false){
   return new ItemReturnNumericInputs(ArchivedScaleBits,normalMaskedFpEnvironment);
  }
 }
 public sealed class ItemReturnRandomObservation {
  public readonly bool ReturnedNormally;
  // Raw signed EAX observation at the known call boundary, bound on disk to
  // api-ms-win-crt-utility-l1-1-0.dll!rand. No RNG sequence, seed, thread state,
  // callback, or native library is executed here. Arbitrary supplied EAX
  // register fixtures are not claims about rand's documented value range.
  public readonly int? Eax;
  public ItemReturnRandomObservation(bool returnedNormally,int? eax){ReturnedNormally=returnedNormally;Eax=eax;}
 }
 public sealed class ItemConstructorReturnResult {
  public readonly ItemConstructorNameResult Prior;
  public readonly ItemConstructorStorage State;
  public readonly ItemGameEngineSlot Slot;
  public ulong? BorrowedPointer {get;internal set;}
  public ItemGameEngineFields BorrowedFields {get;internal set;}
  public int SlotReads {get;internal set;}
  public int GlobalFieldReads {get;internal set;}
  public int ItemFieldReads {get;internal set;}
  public int ScaleReads {get;internal set;}
  public int CallerStoreInstructions {get;internal set;}
  public int DependencyStoreInstructions {get;internal set;}
  public int? SavedDivisor {get;internal set;}
  public int? RandomEax {get;internal set;}
  public int? Remainder {get;internal set;}
  public int? StoredValue {get;internal set;}
  public ulong? ReturnedItemToken {get;internal set;}
  public bool DependencyReturned {get;internal set;}
  public bool ConditionalNormalReturn {get;internal set;}
  public bool AwaitingRandomObservation {get;internal set;}
  public bool IsCompleteItemConstructor {get{return false;}}
  public string Phase {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public uint? CallSiteRva {get;internal set;}
  internal ItemConstructorReturnResult(ItemConstructorNameResult prior,ItemGameEngineSlot slot){
   Prior=prior;State=prior==null?null:prior.State;Slot=slot;Phase="NotRun";Status="Unsupported";Reason="NotRun";ResumeInstructionRva=ItemConstructorReturnInvocation.EntryRva;
  }
 }

 // A deliberately smaller domain than complete SSE binary32. Operations are
 // accepted only when their mathematical result is EXACTLY a finite normal
 // binary32 value or zero. All rounding-needed, subnormal, nonfinite, overflow
 // and invalid conversion cases remain explicit coverage stops. UInt64 signed
 // magnitude work avoids losing tiny operands through a double intermediate.
 // CVTT truncates toward zero; a normal masked environment is still required.
 internal static class ItemReturnExactBinary32 {
  static bool Parts(uint bits,out bool negative,out ulong magnitude,out int exponent,out string reason){
   negative=(bits&0x80000000U)!=0;uint e=(bits>>23)&255,f=bits&0x7FFFFFU;
   magnitude=0;exponent=0;reason=null;
   if(e==255){reason="NonfiniteBinary32OutsideCoverage";return false;}
   if(e==0){if(f!=0){reason="SubnormalBinary32OutsideCoverage";return false;}return true;}
   magnitude=0x800000UL+f;exponent=(int)e-150;return true;
  }
  static bool Compose(bool negative,ulong magnitude,int exponent,out uint bits,out string reason){
   bits=negative?0x80000000U:0;reason=null;if(magnitude==0)return true;
   while((magnitude&1UL)==0){magnitude>>=1;exponent++;}
   int n=0;for(ulong v=magnitude;v!=0;v>>=1)n++;
   if(n>24){reason="Binary32RoundingRequired";return false;}
   int top=exponent+n-1;
   if(top < -126){reason="SubnormalBinary32ResultOutsideCoverage";return false;}
   if(top > 127){reason="Binary32OverflowOutsideCoverage";return false;}
   uint significand=(uint)(magnitude<<(24-n));
   bits|=(uint)(top+127)<<23;bits|=significand&0x7FFFFFU;return true;
  }
  public static bool Add(uint a,uint b,bool subtract,out uint result,out string reason){
   bool an,bn;ulong am,bm;int ae,be;result=0;
   if(!Parts(a,out an,out am,out ae,out reason)||!Parts(b,out bn,out bm,out be,out reason))return false;
   if(subtract)bn=!bn;
   if(am==0&&bm==0)return Compose(an&&bn,0,0,out result,out reason);
   if(am==0)return Compose(bn,bm,be,out result,out reason);
   if(bm==0)return Compose(an,am,ae,out result,out reason);
   int common=Math.Min(ae,be),delta=Math.Abs(ae-be);
   // With >39 exponent separation both operands are nonzero and the low
   // significant bits cannot cancel into an exactly representable binary32.
   if(delta>39){reason="Binary32RoundingRequired";return false;}
   am<<=ae-common;bm<<=be-common;
   if(an==bn)return Compose(an,am+bm,common,out result,out reason);
   if(am==bm)return Compose(false,0,0,out result,out reason);
   return am>bm?Compose(an,am-bm,common,out result,out reason):Compose(bn,bm-am,common,out result,out reason);
  }
  public static bool Multiply(uint a,uint b,out uint result,out string reason){
   bool an,bn;ulong am,bm;int ae,be;result=0;
   if(!Parts(a,out an,out am,out ae,out reason)||!Parts(b,out bn,out bm,out be,out reason))return false;
   return Compose(an!=bn,am*bm,ae+be,out result,out reason);
  }
  public static bool Truncate(uint bits,out int result,out string reason){
   bool negative;ulong magnitude;int exponent;result=0;
   if(!Parts(bits,out negative,out magnitude,out exponent,out reason))return false;
   if(magnitude==0)return true;
   ulong integer;
   if(exponent>=0){
    if(exponent>31||magnitude>((negative?0x80000000UL:0x7FFFFFFFUL)>>exponent)){
     reason="InvalidCvttss2siOutsideCoverage";return false;
    }
    integer=magnitude<<exponent;
   }else integer=exponent<=-64?0:magnitude>>(-exponent);
   if(integer>(negative?0x80000000UL:0x7FFFFFFFUL)){reason="InvalidCvttss2siOutsideCoverage";return false;}
   result=negative?unchecked((int)(0U-(uint)integer)):(int)integer;return true;
  }
  public static bool FromInt32(int value,out uint bits,out string reason){
   bool negative=value<0;ulong magnitude=negative?(ulong)(-(long)value):(ulong)value;
   return Compose(negative,magnitude,0,out bits,out reason);
  }
 }

 // Phase boundaries are own inspection points, not recovered callbacks or
 // native concurrency guarantees. Exclusive ownership is required within each
 // phase. Invocation construction never reads the slot; Begin captures it once.
 // Later publication does not redirect the borrowed object or its live bytes.
 public sealed class ItemConstructorReturnInvocation {
  public const uint EntryRva=0x323A49,DependencyEntryRva=0x324560;
  public const uint DependencyCallRva=0x323A6B,RandomCallRva=0x3245A2;
  public const uint DependencyReturnRva=0x3245DA,ItemReturnRva=0x323A86;
  public readonly ItemConstructorReturnResult Result;
  public readonly ItemReturnNumericInputs NumericInputs;
  public bool HasRun {get;private set;}
  readonly ItemConstructorStorage state;
  uint upperBits;
  public ItemConstructorReturnInvocation(ItemConstructorNameResult prior,ItemGameEngineSlot slot,ItemReturnNumericInputs numericInputs){
   Result=new ItemConstructorReturnResult(prior,slot);state=Result.State;NumericInputs=numericInputs;
  }
  public ItemConstructorReturnResult Begin(){
   Require("NotRun");HasRun=true;
   var prior=Result.Prior;
   if(prior==null||state==null)return Stop("MissingPriorItemNamePrefix",EntryRva);
   if(!prior.ReachedItem323A49||prior.Status!="ConditionalItemPrefix"||prior.ResumeInstructionRva!=EntryRva)
    return Stop("PriorItemNamePrefixIncomplete",prior.ResumeInstructionRva);
   Result.SlotReads=1;
   if(Result.Slot==null||!Result.Slot.Pointer.HasValue)return Stop("UnknownGameEngineGlobalPointer",EntryRva);
   Result.BorrowedPointer=Result.Slot.Pointer.Value;Result.BorrowedFields=Result.Slot.Fields;
   Progress("Borrowed",0x323A50);return Result;
  }
  public ItemConstructorReturnResult CopyFirstField(){
   Require("Borrowed");uint value;
   if(!GlobalField(0,0x323A50,out value))return Result;
   if(!Store(0xB68,value,0x323A56,false))return Result;
   Progress("FirstCopied",0x323A5C);return Result;
  }
  public ItemConstructorReturnResult CopySecondField(){
   Require("FirstCopied");uint value;
   if(!GlobalField(4,0x323A5C,out value))return Result;
   if(!Store(0xB6C,value,0x323A62,false))return Result;
   Progress("FieldsCopied",DependencyCallRva);return Result;
  }
  public ItemConstructorReturnResult AdvanceToRandom(){
   Require("FieldsCopied");Result.CallSiteRva=DependencyCallRva;
   uint lower,difference,scale,span;string reason;
   if(!ItemField(0xB6C,0x32456A,out upperBits))return Result;
   if(!ItemField(0xB68,0x324578,out lower))return Result;
   if(!Environment(0x324578))return Result;
   if(!ItemReturnExactBinary32.Add(upperBits,lower,true,out difference,out reason))return Stop(reason,0x324578);
   if(!Scale(0x324580,out scale))return Result;
   if(!ItemReturnExactBinary32.Multiply(difference,scale,out span,out reason))return Stop(reason,0x324580);
   int divisor;if(!ItemReturnExactBinary32.Truncate(span,out divisor,out reason))return Stop(reason,0x324588);
   Result.SavedDivisor=divisor;
   if(divisor==0){
    uint scaledUpper;if(!Scale(0x324590,out scale))return Result;
    if(!ItemReturnExactBinary32.Multiply(upperBits,scale,out scaledUpper,out reason))return Stop(reason,0x324590);
    int initial;if(!ItemReturnExactBinary32.Truncate(scaledUpper,out initial,out reason))return Stop(reason,0x324598);
    if(!Store(0xB64,unchecked((uint)initial),0x32459C,true))return Result;Result.StoredValue=initial;
   }
   // There is NO jump around the call or IDIV after the zero-span store.
   Result.AwaitingRandomObservation=true;Result.Phase="AwaitingRandom";Result.Status="PendingObservation";
   Result.Reason="MissingRandomCallObservation";Result.ResumeInstructionRva=RandomCallRva;return Result;
  }
  public ItemConstructorReturnResult RunToRandom(){
   Require("NotRun");Begin();if(Result.Phase!="Borrowed")return Result;
   CopyFirstField();if(Result.Phase!="FirstCopied")return Result;
   CopySecondField();if(Result.Phase!="FieldsCopied")return Result;
   return AdvanceToRandom();
  }
  public ItemConstructorReturnResult CompleteWithRandomObservation(ItemReturnRandomObservation observation){
   Require("AwaitingRandom");Result.AwaitingRandomObservation=false;
   if(observation==null)return Stop("MissingRandomCallObservation",RandomCallRva);
   if(!observation.ReturnedNormally)return Stop("UnresolvedRandomCallReturn",RandomCallRva);
   if(!observation.Eax.HasValue)return Stop("UnknownRandomReturnEax",RandomCallRva);
   Result.RandomEax=observation.Eax.Value;
   // Saved EDI is retained across the call. Only B68 is reread; B6C and the
   // previously borrowed GameEngine slot are not reselected or reread.
   uint lower,scale,scaledLower;string reason;
   if(!ItemField(0xB68,0x3245A8,out lower))return Result;
   if(!Environment(0x3245B1))return Result;
   if(!Scale(0x3245B1,out scale))return Result;
   if(!ItemReturnExactBinary32.Multiply(lower,scale,out scaledLower,out reason))return Stop(reason,0x3245B1);
   int dividend=observation.Eax.Value,divisor=Result.SavedDivisor.Value;
   if(divisor==0)return Stop("IntegerDivideByZero",0x3245B9,"NativeIntegerFaultFrontier");
   if(dividend==int.MinValue&&divisor==-1)return Stop("IntegerDivideOverflow",0x3245B9,"NativeIntegerFaultFrontier");
   int remainder=dividend%divisor;Result.Remainder=remainder;
   uint remainderBits;if(!ItemReturnExactBinary32.FromInt32(remainder,out remainderBits,out reason))return Stop(reason,0x3245BF);
   uint finalBits;if(!ItemReturnExactBinary32.Add(scaledLower,remainderBits,false,out finalBits,out reason))return Stop(reason,0x3245C2);
   int value;if(!ItemReturnExactBinary32.Truncate(finalBits,out value,out reason))return Stop(reason,0x3245C6);
   if(!Store(0xB64,unchecked((uint)value),0x3245CA,true))return Result;
   Result.StoredValue=value;Result.DependencyReturned=true;
   // Saved caller323A71 selects original RDI, after the dependency's normal
   // RET. Its epilogue has no further modeled Item stores or dependency calls.
   Result.ReturnedItemToken=state.Token;Result.ConditionalNormalReturn=true;Result.CallSiteRva=null;
   Result.Phase="Returned";Result.Status="ConditionalItemReturn";Result.Reason=null;Result.ResumeInstructionRva=ItemReturnRva;return Result;
  }
  bool GlobalField(int relative,uint rva,out uint value){
   value=0;Result.ResumeInstructionRva=rva;Result.GlobalFieldReads++;
   if(Result.BorrowedPointer.Value==0){Stop("NullGameEngineGlobalPointer",rva);return false;}
   var fields=Result.BorrowedFields;
   if(fields==null){Stop("MissingBorrowedGameEngineFields",rva);return false;}
   if(fields.Token!=Result.BorrowedPointer.Value){Stop("GameEngineBorrowedIdentityMismatch",rva);return false;}
   if(!fields.IsValid){Stop("InvalidatedGameEngineFields",rva);return false;}
   // This projection owns separate byte backing. Equal observed addresses
   // must not silently denote two different byte arrays: aliasing the Item
   // capture lies outside its covered domain, not on a native error branch.
   ulong fieldStart=fields.Token+(ulong)ItemGameEngineFields.FirstOffset;
   ulong fieldEnd=fieldStart+(ulong)(ItemGameEngineFields.ByteCount-1);
   ulong itemEnd=state.Token+(ulong)(state.ByteCount-1);
   if(fieldStart<=itemEnd&&state.Token<=fieldEnd){Stop("BorrowedFieldAliasingOutsideCoverage",rva);return false;}
   uint? observed=fields.ReadDword(relative);
   if(!observed.HasValue){Stop("UnknownGameEngineField",rva);return false;}
   value=observed.Value;return true;
  }
  bool ItemField(int offset,uint rva,out uint value){
   value=0;Result.ResumeInstructionRva=rva;Result.ItemFieldReads++;
   if(!state.HasRange(offset,4)){Stop("MissingItemReturnFieldStorage",rva);return false;}
   ulong? observed=state.ReadLittleEndian(offset,4);
   if(!observed.HasValue){Stop("UnknownItemReturnField",rva);return false;}value=(uint)observed.Value;return true;
  }
  bool Environment(uint rva){
   if(NumericInputs!=null&&NumericInputs.NormalMaskedFpEnvironmentObserved)return true;
   Stop("MissingNormalMaskedFpEnvironment",rva);return false;
  }
  bool Scale(uint rva,out uint bits){
   bits=0;Result.ScaleReads++;Result.ResumeInstructionRva=rva;
   if(NumericInputs==null||!NumericInputs.ScaleBits.HasValue){Stop("UnknownScaleOperand",rva);return false;}
   bits=NumericInputs.ScaleBits.Value;return true;
  }
  bool Store(int offset,uint value,uint rva,bool dependency){
   Result.ResumeInstructionRva=rva;
   if(!state.HasRange(offset,4)){Stop("MissingItemReturnFieldStorage",rva);return false;}
   state.Store(offset,4,value);if(dependency)Result.DependencyStoreInstructions++;else Result.CallerStoreInstructions++;return true;
  }
  void Require(string phase){if(Result.Phase!=phase)throw new InvalidOperationException("Expected own phase "+phase+", observed "+Result.Phase);}
  void Progress(string phase,uint rva){Result.Phase=phase;Result.Status="ConditionalItemPrefix";Result.Reason=null;Result.ResumeInstructionRva=rva;}
  ItemConstructorReturnResult Stop(string reason,uint rva,string status="Unsupported"){
   Result.Status=status;Result.Reason=reason;Result.ResumeInstructionRva=rva;Result.Phase="Stopped";Result.AwaitingRandomObservation=false;return Result;
  }
 }
}
