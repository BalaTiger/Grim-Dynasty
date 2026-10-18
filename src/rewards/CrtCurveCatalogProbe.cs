using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class CrtCurveCatalogProbe {
 static uint Rounded(float x){return (uint)(long)CrtReference.Single(x+0.5f);}
 static int Main(string[] args){try{
  if(args.Length==0)throw new ArgumentException("CrtCurveCatalogProbe <authorized ARZ> [additional ARZ]");SystemCrtMath math;var profile=SystemCrtMath.Create(TargetNumericLocale.CInvariant,out math);if(profile.Status!="Known")throw new Exception(profile.Reason);
  using(math)using(var reference=new CrtReference()){
   int records=0,missing=0,comparisons=0,scenarios=0;var rows=new List<object>();
   foreach(string path in args)using(var input=ResourceInput.OpenGameResource(path))using(var db=new ArzArchive(input)){
    const string name="records/creatures/pc/playerlevels.dbr";if(!db.Entries.ContainsKey(name)){missing++;rows.Add(new{archive=path,status="known_record_absent"});continue;}
    var record=db.ReadRecord(name);string source=(string)record.fields["experienceLevelEquation"];uint max=Convert.ToUInt32(record.fields["maxPlayerLevel"]);if(max<2||max>256)throw new Exception("Curve level domain outside probe");
    string shape=Regex.Replace(source.Replace(" ","").ToLowerInvariant(),"[0-9.]+","#");if(shape!="(((((playerlevel*playerlevel*playerlevel)^#)*#)+((playerlevel*playerlevel)*#))*#)+#")throw new Exception("Independent reference shape not recognized");
    var tokens=Regex.Matches(source,"[0-9.]+");if(tokens.Count!=5)throw new Exception("Unexpected official coefficient count");float[] constants=new float[5];double[] wideConstants=new double[5];for(int i=0;i<5;i++){constants[i]=reference.Literal(tokens[i].Value);wideConstants[i]=reference.LiteralDouble(tokens[i].Value);}
    CrtEquation equation;var compiled=CrtEquation.Compile(math,source,out equation);if(compiled.Status!="Known")throw new Exception(compiled.Reason);var bits=new List<byte>();float first=0,next=0;
    int allDoubleBitDifferences=0,allDoubleThresholdDifferences=0;object firstDoubleDifference=null;
    for(uint level=0;level<=max;level++){
     float actual;var result=equation.Evaluate((int)level,out actual);if(result.Status!="Known")throw new Exception(result.Reason);float expected=reference.OfficialShape(level,constants);
     if(CrtReference.Bits(actual)!=CrtReference.Bits(expected)||Rounded(actual)!=Rounded(expected))throw new Exception("Official shape precision mismatch at "+level);bits.AddRange(BitConverter.GetBytes(actual));comparisons++;if(level==1)first=actual;if(level==2)next=actual;
     float wide=reference.NaiveAllDoubleShape(level,wideConstants);if(CrtReference.Bits(wide)!=CrtReference.Bits(actual)){allDoubleBitDifferences++;if(firstDoubleDifference==null)firstDoubleDifference=new{level,native_bits=CrtReference.Bits(actual),all_double_bits=CrtReference.Bits(wide),native_threshold=Rounded(actual),all_double_threshold=Rounded(wide)};}if(Rounded(wide)!=Rounded(actual))allDoubleThresholdDifferences++;
    }
    uint award=Rounded(first);if(award==0||Rounded(next)<=award)throw new Exception("One-level original fixture domain not met");
    var x=ExampleLevelUpQuest.Create();var p=x.Player;var c=p.Progression;c.CrtEquation=equation;c.FormulaSamples.Clear();p.NextThreshold=null;p.MaximumLevel175C=max;p.Experience1764=0;c.AttributeGainPerLevel=Convert.ToInt32(record.fields["characterModifierPoints"]);c.SkillPointSchedule=new List<int>();object raw=record.fields["skillModifierPoints"];object[] values=raw as object[]??new[]{raw};foreach(object value in values)c.SkillPointSchedule.Add(Convert.ToInt32(value));x.Completion.Events[0].Actions[0].Amount=award;
    var a=x.Quest.Auto;AutomaticTasks.Start(a);AutomaticTasks.Update(a);x.Quest.Kill();AutomaticTasks.Update(a);x.Quest.Kill();var done=AutomaticTasks.Update(a);if(done.Status!="Known"||p.Level1760!=2||p.Progression.SkillPoints176C!=c.SkillPointSchedule[0]||x.Completion.World.LevelDisplay.Queue.Messages.Count!=1)throw new Exception("Official parameter own level-up fixture failed");
    records++;scenarios++;rows.Add(new{archive=path,status="passed",record=name,record_body_sha256=record.body_sha256,expression_sha256=ResourceUtil.Hash(Encoding.UTF8.GetBytes(source)),max_player_level=max,compared_levels=max+1,all_float_bits_sha256=ResourceUtil.Hash(bits.ToArray()),naive_all_double_bit_differences=allDoubleBitDifferences,naive_all_double_threshold_differences=allDoubleThresholdDifferences,first_all_double_difference=firstDoubleDifference,first_threshold=award,second_threshold=Rounded(next),own_fixture_final_level=p.Level1760,own_fixture_final_xp=p.Experience1764,own_fixture_skill_points=c.SkillPoints176C,own_fixture_attribute_points=c.AttributePoints1768});
   }
   Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",records,missing_records=missing,bit_and_rounded_threshold_comparisons=comparisons,own_official_parameter_levelup_scenarios=scenarios,rows,scope="Read-only independent ARZ records, no DLC merge assumption. Own parser versus separately written fixed-shape reference using existing System32 UCRT; not native game/parser execution. Own original level-up fixtures use official numeric parameters with explicit local context and display snapshots."}));return 0;
  }
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
