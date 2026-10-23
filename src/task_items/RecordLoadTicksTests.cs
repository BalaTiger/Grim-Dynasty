using System;
using GrimDynasty.TaskItems;
class RecordLoadTicksTests {
 static int assertions,scenarios;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static RecordLoadTickResult Q(long raw,long basis,long frequency,uint expected,string why){
  var result=RecordLoadTicks.FromQpc(raw,basis,frequency,true);
  Need(result.Mode==RecordLoadTickMode.Qpc&&result.Status==RecordLoadTickStatus.Known&&
   result.Reason==null&&result.Tick32==expected,why);
  return result;
 }
 static RecordLoadTickResult T(uint raw,uint basis,uint expected,string why){
  var result=RecordLoadTicks.FromTimeGetTime(raw,basis);
  Need(result.Mode==RecordLoadTickMode.TimeGetTime&&result.Status==RecordLoadTickStatus.Known&&
   result.Reason==null&&result.Tick32==expected,why);
  return result;
 }
 static void SignedConversion(){
  Q(0,0,1,0,"explicit zero sample converts to known zero");
  Q(12345,45,1000,12300,"counter base is subtracted before conversion");
  Q(1,0,3,333,"positive signed division truncates toward zero");
  Q(-1,0,3,0xfffffeb3U,"negative signed division truncates toward zero then takes low32");
  Q(-1,0,3000,0,"negative fraction truncates to zero rather than floor minus one");
  Q(1,0,-3,0xfffffeb3U,"negative divisor is signed rather than rejected as a default context");
  Q(-1,0,-3,333,"two negative operands yield a positive quotient");
  Q(12,15,2,0xfffffa24U,"negative sample-minus-base is retained");
  Q(4294967297L,0,1000,1,"large quotient exposes only low32");
  Q(-4294967297L,0,1000,uint.MaxValue,"negative quotient exposes low32 without checked cast failure");
  Q(1,0,long.MinValue,0,"minimum signed divisor is valid when quotient fits");
 }
 static void WrappedArithmetic(){
  Q(long.MinValue,long.MaxValue,1,1000,"64bit subtraction wraps min-minus-max to one");
  Q(long.MaxValue,long.MinValue,1,0xfffffc18U,"64bit subtraction wraps max-minus-min to minus one");
  Q(long.MaxValue,-1,3,0,"max-minus-negative-one wraps to minimum before multiplication");
  Q(long.MinValue,0,3,0,"low64 multiplication wraps min-times1000 to zero");
  Q(long.MaxValue,0,3,0xfffffeb3U,"low64 multiplication produces minus1000 before division");
  Q(2305843009213693952L,0,3,0,"2^61 times1000 wraps to zero");
  Q(2305843009213693953L,0,3,333,"2^61 plus one times1000 wraps to1000");
  Q(1152921504606846976L,0,3,0x55555556U,"2^60 times1000 becomes signed minimum before division");
  Q(1152921504606846977L,0,3,0x555556a3U,"wrapped negative product retains its remainder before truncation");
 }
 static void UnknownAndFaults(){
  var unknownFalse=RecordLoadTicks.FromQpc(null,0,1000,false);
  Need(unknownFalse.Status==RecordLoadTickStatus.Unsupported&&unknownFalse.Mode==RecordLoadTickMode.Qpc&&
   unknownFalse.Reason=="UnknownQpcOutputSlot"&&!unknownFalse.Tick32.HasValue,"false BOOL with unknown slot does not invent zero or fallback");
  var unknownTrue=RecordLoadTicks.FromQpc(null,0,1000,true);
  Need(unknownTrue.Status==RecordLoadTickStatus.Unsupported&&unknownTrue.Reason=="UnknownQpcOutputSlot"&&
   !unknownTrue.Tick32.HasValue,"true BOOL alone does not supply missing slot bits");
  var unknownBeforeFault=RecordLoadTicks.FromQpc(null,0,0,false);
  Need(unknownBeforeFault.Status==RecordLoadTickStatus.Unsupported&&unknownBeforeFault.Reason=="UnknownQpcOutputSlot"&&
   !unknownBeforeFault.Tick32.HasValue,"unknown output slot is rejected before classifying a divide fault");
  var falseKnown=RecordLoadTicks.FromQpc(7,1,3,false);
  var trueKnown=RecordLoadTicks.FromQpc(7,1,3,true);
  Need(falseKnown.Status==RecordLoadTickStatus.Known&&falseKnown.Tick32==2000&&
   trueKnown.Status==RecordLoadTickStatus.Known&&trueKnown.Tick32==2000,
   "caller ignores BOOL when the output slot is independently known");
  var falseZero=RecordLoadTicks.FromQpc(0,0,1,false);
  Need(falseZero.Status==RecordLoadTickStatus.Known&&falseZero.Tick32.HasValue&&falseZero.Tick32.Value==0,
   "known zero slot after false BOOL differs from unknown slot");
  var zero=RecordLoadTicks.FromQpc(1,0,0,true);
  Need(zero.Status==RecordLoadTickStatus.DivideFault&&zero.Reason=="SignedDivisionByZero"&&
   !zero.Tick32.HasValue,"zero divisor reports IDIV fault without throwing managed division");
  var overflow=RecordLoadTicks.FromQpc(1152921504606846976L,0,-1,false);
  Need(overflow.Status==RecordLoadTickStatus.DivideFault&&overflow.Reason=="SignedDivisionOverflow"&&
   !overflow.Tick32.HasValue,"wrapped minimum dividend divided by minus one reports IDIV overflow");
  var negativeOverflow=RecordLoadTicks.FromQpc(-1152921504606846976L,0,-1,true);
  Need(negativeOverflow.Status==RecordLoadTickStatus.DivideFault&&negativeOverflow.Reason=="SignedDivisionOverflow"&&
   !negativeOverflow.Tick32.HasValue,"negative2^60 times1000 also wraps to minimum before minus-one division");
  Q(1152921504606846976L,0,1,0,"minimum dividend divided by one is valid and its low32 is zero");
  Q(1152921504606846976L,0,-2,0,"minimum dividend divided by minus two is valid");
  Need(unknownFalse.Tick32==null&&falseKnown.Tick32==2000&&zero.Tick32==null,
   "later conversions do not mutate earlier immutable results");
 }
 static void UIntConversion(){
  T(0,0,0,"explicit timeGetTime zero remains known");
  T(5,10,0xfffffffbU,"timeGetTime base subtraction wraps at32 bits");
  T(3,uint.MaxValue,4,"timeGetTime subtraction crosses counter wrap");
  T(uint.MaxValue,0,uint.MaxValue,"timeGetTime keeps all high bits");
  Need(RecordLoadTicks.Elapsed(0xfffffff0U,0x10)==32,"elapsed crosses uint32 wrap");
  Need(RecordLoadTicks.Elapsed(10,5)==0xfffffffbU,"backwards converted samples are not clamped");
  Need(RecordLoadTicks.Elapsed(0,0x80000001U)==0x80000001U,"elapsed high bit remains unsigned");
  Need(RecordLoadTicks.Elapsed(uint.MaxValue,uint.MaxValue)==0,"equal converted samples have zero elapsed");
 }
 static void IndependentModesScenario(){
  var start=Q(10,0,3,3333,"QPC start is converted independently");
  var end=T(4000,0,4000,"timeGetTime end may use a different mode");
  Need(RecordLoadTicks.Elapsed(start.Tick32.Value,end.Tick32.Value)==667,"mixed start and end modes have no forced shared clock context");
  var start2=T(5010,10,5000,"second start uses explicit timeGetTime base");
  var end2=Q(25,5,3,6666,"second end uses explicit QPC base");
  Need(RecordLoadTicks.Elapsed(start2.Tick32.Value,end2.Tick32.Value)==1666,"reverse mode combination is also evaluated independently");
  scenarios++;
 }
 static void SeparateTruncationScenario(){
  var start=Q(1,0,3,333,"first quotient truncates separately");
  var end=Q(3,0,3,1000,"second quotient truncates separately");
  Need(RecordLoadTicks.Elapsed(start.Tick32.Value,end.Tick32.Value)==667,"elapsed subtracts already converted low32 values");
  var collapsed=Q(2,0,3,666,"converting raw delta has a different fixed quotient");
  Need(collapsed.Tick32==666&&RecordLoadTicks.Elapsed(start.Tick32.Value,end.Tick32.Value)==667,
   "test prevents replacing two conversions with one raw-delta conversion");
  scenarios++;
 }
 static void Main(){
  SignedConversion();WrappedArithmetic();UnknownAndFaults();UIntConversion();
  IndependentModesScenario();SeparateTruncationScenario();
  Console.WriteLine("RecordLoadTicksTests: "+assertions+" assertions passed; "+scenarios+" scenarios");
 }
}
