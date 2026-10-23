using System;
using GrimDynasty.CombatRuntime;

// Own bounded settlement tests. No native callback, original binary, scene,
// player save, complete ApplyDamage or established death producer is executed.
public static class NativeDamageSettlementTests {
 static int assertions,groups;
 static void Need(bool condition,string why){assertions++;if(!condition)throw new Exception(why);}
 static void Group(string name,Action body){int n=assertions;body();groups++;Console.WriteLine("PASS "+name+": "+(assertions-n)+" assertions");}
 static ulong Bits(double v){return unchecked((ulong)BitConverter.DoubleToInt64Bits(v));}
 static uint Bits(float v){return unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(v),0));}
 static float F(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static double D(ulong bits){return BitConverter.Int64BitsToDouble(unchecked((long)bits));}
 sealed class Fixture {
  public readonly NativeDamageState State=new NativeDamageState {Life=10.0,Accumulated=0.0f,GateByte=0,Dirty=0,ThresholdMultiplier=1.0f,ThresholdFlag=9};
  public readonly NativeDamageStats Stats=new NativeDamageStats {Type=1};
  // Only this self-owned fixture explicitly supplies normal callbacks. The
  // production model has no default-success interface or assumed native body.
  public readonly NativeDamageHooks Hooks=new NativeDamageHooks {
   Gate=delegate(NativeDamageState s){return new NativeDamageObservation<byte>(true,0);},
   Attribute=delegate(NativeDamageState s,int property){return new NativeDamageObservation<float>(true,1.0f);},
   Final=delegate(NativeDamageState s,float damage,NativeDamageStats stats){return new NativeDamageCallObservation(true);}
  };
  public NativeDamageResult Run(float? damage=2.0f,byte? bypass=0,byte? check=1,bool fp=true){return NativeDamageSettlement.Run(State,Stats,new NativeDamageInputs(damage,bypass,check,fp),Hooks);}
 }
 static void Stop(NativeDamageResult r,string reason,uint at,int stores){
  Need(r.Status==NativeDamageStatus.Unsupported&&r.Reason==reason&&r.ResumeInstructionRva==at,"coverage stop "+reason+" at "+at.ToString("X"));
  Need(r.StoreInstructions==stores,"preserved own store prefix for "+reason);
 }
 static void DefaultsAndLazyGate(){
  var unknown=new NativeDamageState();var stats=new NativeDamageStats();
  Need(!unknown.Life.HasValue&&!unknown.Accumulated.HasValue&&!unknown.GateByte.HasValue&&!unknown.Dirty.HasValue&&!unknown.ThresholdMultiplier.HasValue&&!unknown.ThresholdFlag.HasValue&&!stats.Type.HasValue,"new field views contain no fabricated defaults");
  Stop(NativeDamageSettlement.Run(null,stats,new NativeDamageInputs(1,1,0,true),null),"MissingState",0x068AB0,0);
  Stop(NativeDamageSettlement.Run(unknown,stats,null,null),"MissingInputs",0x068AB0,0);
  Stop(NativeDamageSettlement.Run(unknown,stats,new NativeDamageInputs(null,null,null,false),null),"UnknownBypassGate",0x068AC8,0);
  Stop(NativeDamageSettlement.Run(unknown,stats,new NativeDamageInputs(null,0,null,false),null),"MissingGateInterface",0x068AD0,0);
  var f=new Fixture();f.Hooks.Gate=delegate(NativeDamageState s){s.Life=123;return new NativeDamageObservation<byte>(true,255);};
  var r=NativeDamageSettlement.Run(f.State,null,new NativeDamageInputs(null,0,null,false),f.Hooks);
  Need(r.Status==NativeDamageStatus.NormalEarlyReturn&&r.Reason=="GateReturnedNonzero"&&r.StoreInstructions==0&&r.FieldReads==0&&r.FinalCalls==0&&f.State.Life==123,"nonzero AL returns before all later unknown inputs and preserves gate mutation");
  f=new Fixture();f.Hooks.Gate=delegate(NativeDamageState s){s.GateByte=2;return new NativeDamageObservation<byte>(true,0);};r=f.Run(null,0,null,false);
  Need(r.Status==NativeDamageStatus.NormalEarlyReturn&&r.Reason=="GateByteNonzero"&&r.FieldReads==1&&r.StoreInstructions==0,"false gate is followed by fresh byte observation, any nonzero stops normally");
  f=new Fixture();f.State.GateByte=null;f.Hooks.Gate=null;r=f.Run(-1,255,0);
  Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.GateCalls==0&&r.FieldReads==0&&r.StoreInstructions==1,"nonzero bypass skips both gate dependencies; negative damage skips numeric fields");
 }
 static void UnknownGateAndInputs(){
  var f=new Fixture();f.Hooks.Gate=delegate(NativeDamageState s){return null;};Stop(f.Run(),"GateReturnUnobserved",0x068AD0,0);
  f.Hooks.Gate=delegate(NativeDamageState s){return new NativeDamageObservation<byte>(false,0);};Stop(f.Run(),"GateReturnUnobserved",0x068AD0,0);
  f.Hooks.Gate=delegate(NativeDamageState s){return new NativeDamageObservation<byte>(true,null);};Stop(f.Run(),"UnknownGateAl",0x068AD6,0);
  f=new Fixture();f.State.GateByte=null;Stop(f.Run(),"UnknownGateByte",0x068ADE,0);
  f=new Fixture();Stop(f.Run(null),"UnknownDamage",0x068AF0,0);
  Stop(f.Run(1,0,1,false),"FloatingPointEnvironmentUnobserved",0x068AF0,0);
  Stop(f.Run(Single.NaN),"NonfiniteDamageOutsideCoverage",0x068AF0,0);
  Stop(f.Run(Single.PositiveInfinity),"NonfiniteDamageOutsideCoverage",0x068AF0,0);
  Stop(f.Run(Single.NegativeInfinity),"NonfiniteDamageOutsideCoverage",0x068AF0,0);
 }
 static void ReadAndWritePrefixes(){
  var f=new Fixture();f.State.Life=null;Stop(f.Run(),"UnknownLife",0x068AFA,0);
  f=new Fixture();f.State.Accumulated=null;Stop(f.Run(),"UnknownAccumulated",0x068B02,0);
  Need(f.State.Life==10&&f.State.Dirty==0,"unknown accumulator is observed before any life write");
  f=new Fixture();f.State.Life=Double.NaN;f.State.Accumulated=null;Stop(f.Run(),"UnknownAccumulated",0x068B02,0);
  f.State.Accumulated=0;Stop(f.Run(),"NonfiniteLifeOutsideCoverage",0x068B0A,0);
  f=new Fixture();f.State.Accumulated=Single.NaN;Stop(f.Run(),"NonfiniteAccumulatedOutsideCoverage",0x068B0E,0);
  Need(f.State.Life==10,"unsupported accumulator conversion precedes life store");
  f=new Fixture();f.State.Life=1;f.State.Accumulated=Single.MaxValue;var r=f.Run(Single.MaxValue);
  Stop(r,"AccumulatedResultOutsideCoverage",0x068B1D,1);
  Need(f.State.Life==1.0-(double)Single.MaxValue&&f.State.Accumulated==Single.MaxValue&&f.State.Dirty==0&&!r.AccumulatedWritten,"float narrowing overflow keeps life store but not accumulated/dirty writes");
  f=new Fixture();r=f.Run(2,0,null);Stop(r,"UnknownThresholdArgument",0x068B29,2);
  Need(f.State.Life==8&&f.State.Accumulated==2&&f.State.Dirty==0,"fifth-argument read precedes dirty store");
  f=new Fixture();f.Stats.Type=null;r=f.Run();Stop(r,"UnknownStatsType",0x068B37,3);
  Need(f.State.Dirty==1&&r.FinalCalls==0,"type stop retains completed numerical and dirty writes");
  f=new Fixture();f.State.Life=null;f.State.Accumulated=null;r=f.Run(-1);Stop(r,"UnknownThresholdLife",0x068B3C,1);
  Need(!r.LifeWritten&&!r.AccumulatedWritten&&f.State.Dirty==1,"negative damage first reads life only on type-one threshold path");
  f=new Fixture();r=NativeDamageSettlement.Run(f.State,null,new NativeDamageInputs(2,1,1,true),f.Hooks);Stop(r,"MissingStatsReference",0x068B37,3);
  f=new Fixture();r=NativeDamageSettlement.Run(f.State,null,new NativeDamageInputs(2,1,0,true),f.Hooks);Stop(r,"MissingFinalStatsReference",0x068B80,3);
 }
 static void NormalWidthsPrecisionAndNonclamp(){
  var f=new Fixture();f.State.Life=1;f.State.Accumulated=F(0x4B800000);var r=f.Run(3,0,0);
  Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&f.State.Life==-2.0,"negative remaining life is stored, not clamped");
  Need(Bits(f.State.Accumulated.Value)==0x4B800002U,"double sum 16777219 narrows to even binary32 16777220");
  Need(r.LifeWritten&&r.AccumulatedWritten&&r.DirtyWritten&&!r.ThresholdFlagWritten&&f.State.ThresholdFlag==9&&r.StoreInstructions==3,"exact 8-byte/4-byte/1-byte updates preserve untouched flag");
  f=new Fixture();f.State.Life=16777217.0;f.State.Accumulated=16777216.0f;r=f.Run(1,0,0);
  Need(f.State.Life==16777216.0&&Bits(f.State.Accumulated.Value)==0x4B800000U,"life stays double while accumulator ties round back to binary32");
  r=f.Run(1,0,0);Need(f.State.Life==16777215.0&&f.State.Accumulated==16777216.0f,"successive calls reuse rounded accumulator rather than a persistent double sum");
  f=new Fixture();f.State.Life=1;r=f.Run(F(0x3DCCCCCD),0,0);
  Need(Bits(f.State.Life.Value)==0x3FECCCCCCC000000UL,"life subtracts exact widening of float 0.1, not a double decimal 0.1");
  Need(Object.ReferenceEquals(r.State,f.State)&&Object.ReferenceEquals(r.Stats,f.Stats)&&!r.IsCompleteApplyDamage&&!r.IsEstablishedDeathProducer,"shared mutable identities and explicit compatibility limits");
 }
 static void NegativeAndConditionalReads(){
  var f=new Fixture();f.State.Life=null;f.State.Accumulated=null;f.State.ThresholdMultiplier=null;f.Stats.Type=null;
  var r=f.Run(-4,1,0);Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.FieldReads==0&&r.StoreInstructions==1&&r.FinalCalls==1,"negative amount and disabled threshold need no numerical/type fields");
  f=new Fixture();f.State.Life=null;f.State.Accumulated=null;f.Stats.Type=0xFFFFFFFF;r=f.Run(-4,1,1);
  Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.FieldReads==1&&r.AttributeCalls==0,"non-type-one skips threshold life/attribute/multiplier reads");
  f=new Fixture();f.State.ThresholdMultiplier=null;f.Hooks.Attribute=null;r=f.Run(2,1,1);
  Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.AttributeCalls==0&&r.FinalCalls==1,"positive rounded life skips unresolved property interface and multiplier");
  f=new Fixture();f.State.Life=0;f.State.Accumulated=null;r=f.Run(-2,1,1);
  Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.AttributeCalls==1&&r.StoreInstructions==1&&f.State.ThresholdFlag==9,"negative amount can enter threshold path but below-product comparison does not clear prior flag");
 }
 static void UnknownAttributeAndFinal(){
  var f=new Fixture();f.State.Life=1;f.Hooks.Attribute=null;Stop(f.Run(),"MissingAttributeInterface",0x068B60,3);
  f=new Fixture();f.State.Life=1;f.Hooks.Attribute=delegate(NativeDamageState s,int p){return null;};Stop(f.Run(),"AttributeReturnUnobserved",0x068B60,3);
  f=new Fixture();f.State.Life=1;f.Hooks.Attribute=delegate(NativeDamageState s,int p){return new NativeDamageObservation<float>(false,1);};Stop(f.Run(),"AttributeReturnUnobserved",0x068B60,3);
  f=new Fixture();f.State.Life=1;f.Hooks.Attribute=delegate(NativeDamageState s,int p){return new NativeDamageObservation<float>(true,null);};Stop(f.Run(),"UnknownAttributeValue",0x068B65,3);
  f=new Fixture();f.State.Life=1;f.State.ThresholdMultiplier=null;Stop(f.Run(),"UnknownThresholdMultiplier",0x068B65,3);
  f=new Fixture();f.Hooks.Final=null;Stop(f.Run(),"MissingFinalInterface",0x068B86,3);
  f=new Fixture();f.Hooks.Final=delegate(NativeDamageState s,float d,NativeDamageStats t){return null;};Stop(f.Run(),"FinalReturnUnobserved",0x068B86,3);
  f=new Fixture();f.State.Life=1;f.Hooks.Final=delegate(NativeDamageState s,float d,NativeDamageStats t){s.Life=77;return new NativeDamageCallObservation(false);};var r=f.Run();Stop(r,"FinalReturnUnobserved",0x068B86,4);
  Need(f.State.Life==77&&f.State.ThresholdFlag==1&&f.State.Accumulated==2&&f.State.Dirty==1,"nonnormal final observation keeps callback and completed settlement effects");
 }
 static void ThresholdRoundingAndBoundary(){
  var f=new Fixture();f.State.Life=Double.Epsilon;f.State.Accumulated=null;f.Hooks.Attribute=delegate(NativeDamageState s,int p){Need(p==4,"property identifier remains numeric four");return new NativeDamageObservation<float>(true,0);};
  var r=f.Run(-1,1,1);Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.AttributeCalls==1&&f.State.Life==Double.Epsilon,"tiny positive double rounds to zero in temporary comparison without changing life");
  f=new Fixture();f.State.Life=(double)Single.Epsilon;f.Hooks.Attribute=null;r=f.Run(-1,1,1);
  Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.AttributeCalls==0,"representable positive float subnormal remains above zero with gradual underflow");
  f=new Fixture();f.State.Life=1;f.State.ThresholdMultiplier=2;r=f.Run(2);
  Need(r.ThresholdFlagWritten&&f.State.ThresholdFlag==1,"input equal to property product sets flag");
  f=new Fixture();f.State.Life=1;f.State.ThresholdMultiplier=F(0x40000001);r=f.Run(2);
  Need(!r.ThresholdFlagWritten&&f.State.ThresholdFlag==9,"one float ULP larger threshold skips the flag store");
  f=new Fixture();f.State.Life=0;f.State.ThresholdMultiplier=Single.Epsilon;f.Hooks.Attribute=delegate(NativeDamageState s,int p){return new NativeDamageObservation<float>(true,0.5f);};r=f.Run(0);
  Need(r.ThresholdFlagWritten&&f.State.ThresholdFlag==1,"positive half-minsubnormal product rounds to even zero before comparison");
 }
 static void SignedZero(){
  var f=new Fixture();f.State.Life=D(0x8000000000000000UL);f.State.Accumulated=F(0x80000000);f.Hooks.Attribute=delegate(NativeDamageState s,int p){return new NativeDamageObservation<float>(true,F(0x80000000));};
  var r=f.Run(F(0x80000000));
  Need(r.LifeWritten&&r.AccumulatedWritten&&Bits(f.State.Life.Value)==0UL,"negative-zero input takes nonnegative branch; -0 minus -0 becomes +0");
  Need(Bits(f.State.Accumulated.Value)==0x80000000U&&r.ThresholdFlagWritten,"-0 plus -0 preserves accumulator sign and ordered zero equality sets threshold flag");
  f=new Fixture();f.State.Life=D(0x8000000000000000UL);f.State.Accumulated=F(0x80000000);r=f.Run(0,1,0);
  Need(Bits(f.State.Life.Value)==0x8000000000000000UL&&Bits(f.State.Accumulated.Value)==0U,"-0 minus +0 preserves negative life zero; opposite-sign zero sum is positive");
  f=new Fixture();f.State.Life=D(0x8000000000000000UL);r=f.Run(-1,1,1);
  Need(Bits(f.State.Life.Value)==0x8000000000000000UL&&r.AttributeCalls==1,"temporary MAXSD does not rewrite stored negative zero");
 }
 static void CallbackVisibilityAndReentry(){
  var f=new Fixture();int finalCalls=0;
  f.Hooks.Final=delegate(NativeDamageState s,float d,NativeDamageStats t){Need(Object.ReferenceEquals(s,f.State)&&Object.ReferenceEquals(t,f.Stats),"all final calls borrow identical state/stats");finalCalls++;return new NativeDamageCallObservation(true);};
  f.Hooks.Gate=delegate(NativeDamageState s){
   s.Life=6;s.Accumulated=2;
   var nested=NativeDamageSettlement.Run(s,f.Stats,new NativeDamageInputs(-2,1,0,true),f.Hooks);
   Need(nested.Status==NativeDamageStatus.ConditionalNormalReturn,"gate permits synchronous own reentry");s.GateByte=0;return new NativeDamageObservation<byte>(true,0);
  };
  var r=f.Run();Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&f.State.Life==4&&f.State.Accumulated==4&&finalCalls==2,"outer observes state produced during gate reentry");
  Need(r.StoreInstructions==3&&r.FinalCalls==1,"outer counters exclude nested/callback writes and calls");
  f=new Fixture();f.State.Life=1;int seen=0;
  f.Hooks.Attribute=delegate(NativeDamageState s,int p){
   Need(s.Life==-1&&s.Accumulated==2&&s.Dirty==1,"property callback observes earlier life/accumulator/dirty prefix");
   var nested=NativeDamageSettlement.Run(s,f.Stats,new NativeDamageInputs(0.5f,1,0,true),f.Hooks);
   Need(nested.Status==NativeDamageStatus.ConditionalNormalReturn,"attribute permits synchronous own reentry");
   s.ThresholdMultiplier=4;f.Stats.Type=99;return new NativeDamageObservation<float>(true,1);
  };
  f.Hooks.Final=delegate(NativeDamageState s,float d,NativeDamageStats t){
   Need(Object.ReferenceEquals(s,f.State)&&Object.ReferenceEquals(t,f.Stats),"property reentry retains shared identities");
   if(d==2){seen++;Need(s.Life==-1.5&&s.Accumulated==2.5f&&t.Type==99&&s.ThresholdFlag==9,"outer final sees reentry state and fresh multiplier result, while original damage stays saved");}
   return new NativeDamageCallObservation(true);
  };
  r=f.Run();Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&!r.ThresholdFlagWritten&&seen==1,"fresh multiplier after property call controls outer comparison");
  f=new Fixture();f.State.Life=1;f.State.ThresholdMultiplier=null;
  f.Hooks.Attribute=delegate(NativeDamageState s,int p){s.ThresholdMultiplier=0.5f;return new NativeDamageObservation<float>(true,1);};
  r=f.Run();Need(r.ThresholdFlagWritten,"unknown entry multiplier may become known inside property callback");
 }
 static void CallbackFailurePrefixes(){
  var f=new Fixture();f.Hooks.Gate=delegate(NativeDamageState s){s.Dirty=7;s.Life=42;throw new InvalidOperationException("own gate failure");};var r=f.Run();
  Stop(r,"GateCallbackThrew",0x068AD0,0);Need(r.CallbackException is InvalidOperationException&&f.State.Dirty==7&&f.State.Life==42,"gate exception does not roll back hook mutations");
  f=new Fixture();f.State.Life=1;f.Hooks.Attribute=delegate(NativeDamageState s,int p){s.ThresholdMultiplier=3;s.Life=-99;throw new InvalidOperationException("own property failure");};r=f.Run();
  Stop(r,"AttributeCallbackThrew",0x068B60,3);Need(f.State.Life==-99&&f.State.Accumulated==2&&f.State.Dirty==1&&f.State.ThresholdMultiplier==3&&f.State.ThresholdFlag==9&&r.FinalCalls==0,"property exception retains settlement prefix and own callback changes");
  f=new Fixture();f.State.Life=1;f.Hooks.Final=delegate(NativeDamageState s,float d,NativeDamageStats t){Need(s.Life==-1&&s.Accumulated==2&&s.Dirty==1&&s.ThresholdFlag==1,"final observes every preceding store");t.Type=7;throw new InvalidOperationException("own final failure");};r=f.Run();
  Stop(r,"FinalCallbackThrew",0x068B86,4);Need(f.Stats.Type==7&&f.State.ThresholdFlag==1&&r.FinalCalls==1,"final exception preserves shared stats mutation and completed flag store");
 }
 static void ExplicitNumericCoverage(){
  var f=new Fixture();f.State.Life=Double.PositiveInfinity;var r=f.Run(-1,1,1);Stop(r,"NonfiniteThresholdLifeOutsideCoverage",0x068B44,1);
  f=new Fixture();f.State.Life=Double.MaxValue;r=f.Run(-1,1,1);Stop(r,"ThresholdLifeResultOutsideCoverage",0x068B48,1);
  f=new Fixture();f.State.Life=1;f.State.ThresholdMultiplier=Single.NaN;r=f.Run();Stop(r,"NonfiniteThresholdOperandOutsideCoverage",0x068B65,3);
  f=new Fixture();f.State.Life=1;f.Hooks.Attribute=delegate(NativeDamageState s,int p){return new NativeDamageObservation<float>(true,Single.PositiveInfinity);};r=f.Run();Stop(r,"NonfiniteThresholdOperandOutsideCoverage",0x068B65,3);
  f=new Fixture();f.State.Life=1;f.State.ThresholdMultiplier=Single.MaxValue;f.Hooks.Attribute=delegate(NativeDamageState s,int p){return new NativeDamageObservation<float>(true,2);};r=f.Run();Stop(r,"ThresholdProductOutsideCoverage",0x068B65,3);
  Need(f.State.Life==-1&&f.State.Accumulated==2&&f.State.Dirty==1&&f.State.ThresholdFlag==9&&r.FinalCalls==0,"unsupported numeric product retains exact prefix without inventing final success");
 }
 public static int Main(){
  try{
   Group("unknown defaults and lazy gates",DefaultsAndLazyGate);
   Group("gate and input coverage stops",UnknownGateAndInputs);
   Group("field read and write prefixes",ReadAndWritePrefixes);
   Group("double life and float rounding",NormalWidthsPrecisionAndNonclamp);
   Group("negative and conditional lazy reads",NegativeAndConditionalReads);
   Group("unresolved property and final interfaces",UnknownAttributeAndFinal);
   Group("threshold rounding and boundaries",ThresholdRoundingAndBoundary);
   Group("signed zero",SignedZero);
   Group("shared callback visibility and reentry",CallbackVisibilityAndReentry);
   Group("callback failure prefixes",CallbackFailurePrefixes);
   Group("explicit numeric coverage",ExplicitNumericCoverage);
   Console.WriteLine("PASS NativeDamageSettlement: "+assertions+" assertions / "+groups+" groups (own conditional model)");return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL NativeDamageSettlement: "+e);return 1;}
 }
}
