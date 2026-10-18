using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class CrtMathTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static float Value(SystemCrtMath m,string s,int input){CrtEquation e;Need(CrtEquation.Compile(m,s,out e).Status=="Known","compile "+s);float value;Need(e.Evaluate(input,out value).Status=="Known","evaluate "+s);return value;}
 static int Main(){try{
  SystemCrtMath math;Need(SystemCrtMath.Create(TargetNumericLocale.Unknown,out math).Status=="Unsupported"&&math==null,"unknown target locale");Need(SystemCrtMath.Create(TargetNumericLocale.CInvariant,out math).Status=="Known","verified system CRT profile");
  int differences=0;long maxUlp=0;object first=null;uint controlBefore,controlAfter;
  using(math)using(var reference=new CrtReference()){
   Need(math.Check(out controlBefore).Status=="Known","read-only float context");float f;
   math.Literal("1.16",out f);Need(CrtReference.Bits(f)==1066695393u,"double to float exponent");math.Literal("1.0000000596046447753906250000000001",out f);Need(CrtReference.Bits(f)==0x3f800000,"double rounding midpoint witness");math.Literal("1.000000059604645",out f);Need(CrtReference.Bits(f)==0x3f800001,"above midpoint");math.Literal("16777217",out f);Need(f==16777216,"large integer literal float rounding");math.Literal(".",out f);Need(f==0,"atof invalid numeric token returns zero");math.Literal("1..2",out f);Need(f==1,"atof prefix conversion");
   math.Power(0,0,out f);Need(f==1,"zero to zero");math.Power(BitConverter.ToSingle(BitConverter.GetBytes(0x80000000u),0),3,out f);Need(CrtReference.Bits(f)==0x80000000u,"negative zero odd exponent");math.Power(-1,0.5f,out f);Need(float.IsNaN(f),"raw powf domain result");math.Power(0,-1,out f);Need(float.IsPositiveInfinity(f),"raw powf pole result");
   Need(Value(math,"2^3^2",0)==64,"native power is left associative");Need(Value(math,"20-5-3",0)==12,"subtraction left associative");Need(Value(math,"20/5/2",0)==2,"division left associative");Need(Value(math,"-2^2",0)==-4,"unary precedence below exponent");Need(Value(math,"(-2)^2",0)==4,"parenthesis changes unary binding");Need(Value(math,"2^(-2)",0)==0.25f,"parenthesized negative exponent");Need(Value(math,"--2",0)==2,"unary right association");Need(Value(math,"1 + + 2",0)==3,"unary plus is skipped");Need(Value(math,"1 2+",0)==3,"native stack arity behavior");Need(Value(math,"1.2.3",0)==reference.Literal("1.2.3"),"native digit-dot token scan");Need(Value(math,"PLAYERLEVEL",-1)==4294967296f,"unsigned playerLevel conversion");
   CrtEquation e;Need(CrtEquation.Compile(math,"2^-2",out e).Status=="Unsupported","native unparenthesized negative exponent underflow");Need(CrtEquation.Compile(math,"1e2",out e).Status=="Unsupported","scientific notation absent from native lexer");Need(CrtEquation.Compile(math,"(1+2",out e).Status=="Unsupported","unmatched parentheses");Need(CrtEquation.Compile(math,"(-1)^0.5",out e).Status=="Unsupported","nonfinite expression outside covered domain");Need(CrtEquation.Compile(math,"charLevel",out e).Status=="Unsupported","other resolver branch unknown");
   uint seed=1234567;
   for(int i=0;i<100000;i++){seed=unchecked(seed*1664525+1013904223);float x=(seed%1000000+1)/1000f;seed=unchecked(seed*1664525+1013904223);float y=(seed%40000)/10000f;float native;math.Power(x,y,out native);float wide=(float)Math.Pow(x,y);if(CrtReference.Bits(native)!=CrtReference.Bits(wide)){differences++;maxUlp=Math.Max(maxUlp,Math.Abs((long)CrtReference.Bits(native)-CrtReference.Bits(wide)));if(first==null)first=new{x_bits=CrtReference.Bits(x),y_bits=CrtReference.Bits(y),powf_bits=CrtReference.Bits(native),double_math_bits=CrtReference.Bits(wide)};}}
   Need(differences>0,"measured double Math.Pow substitution differences");
   var q=ExampleLevelUpQuest.Create();Need(CrtEquation.Compile(math,"playerLevel^1.5*60.25+0.5",out e).Status=="Known","original full-math program");q.Player.Progression.CrtEquation=e;q.Player.Progression.FormulaSamples.Clear();q.Player.NextThreshold=null;var a=q.Quest.Auto;AutomaticTasks.Start(a);AutomaticTasks.Update(a);q.Quest.Kill();AutomaticTasks.Update(a);q.Quest.Kill();Need(AutomaticTasks.Update(a).Status=="Known"&&q.Player.Level1760==2&&q.Player.Progression.SkillPoints176C==3&&q.Completion.World.LevelDisplay.Queue.Messages.Count==1,"powf/decimal program closes main level-up quest");
   Need(math.Check(out controlAfter).Status=="Known"&&controlAfter==controlBefore,"no float control mutation");
  }
  float disposed;Need(math.Literal("1",out disposed).Status=="Unsupported","disposed private locale cannot be reused");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",fixed_assertions=n,pow_comparison_samples=100000,powf_vs_double_math_differences=differences,maximum_positive_result_ulp_difference=maxUlp,first_difference=first,scope="Own executables and existing fingerprinted System32 UCRT. C-locale numeric value profile, read-only nearest/masked/denormal-preserved context. This is not original game execution or its dynamic oracle."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
