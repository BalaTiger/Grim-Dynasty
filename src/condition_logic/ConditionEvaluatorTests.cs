using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
public static class ConditionEvaluatorTests {
 static int checks;
 static void Assert(bool value,string label){checks++;if(!value)throw new Exception("Assertion: "+label);}
 static ConditionSpec Difficulty(uint stored,int comparison){return new ConditionSpec{TypeId=4,Version=0,StoredDifficulty=stored,ComparisonId=comparison};}
 static void Known(EvaluationResult r,bool expected,string label){Assert(r.Status=="Known"&&r.Value.HasValue&&r.Value.Value==expected,label);}
 static void Unsupported(EvaluationResult r,string label){Assert(r.Status=="Unsupported"&&!r.Value.HasValue,label);}
 public static int Main(){try{
  // Fixed expected tables taken from the native switch's SETcc branches.
  bool[][] truth={new[]{true,true,false,false,false,true},new[]{false,false,false,true,true,true},new[]{false,true,true,true,false,false}};
  uint[] stored={1,2,2},actual={2,1,2};
  for(int row=0;row<3;row++)for(int op=0;op<6;op++)Known(ConditionEvaluator.Evaluate(Difficulty(stored[row],op),new SimulatedContext{Difficulty=actual[row]}),truth[row][op],"stored-to-actual signed switch "+row+"/"+op);
  Known(ConditionEvaluator.Evaluate(Difficulty(0x80000000,0),new SimulatedContext{Difficulty=0x7fffffff}),true,"signed minimum is less than maximum");
  Known(ConditionEvaluator.Evaluate(Difficulty(0xffffffff,0),new SimulatedContext{Difficulty=0}),true,"stored -1 less than zero");
  Known(ConditionEvaluator.Evaluate(Difficulty(0,4),new SimulatedContext{Difficulty=0xffffffff}),true,"zero greater than observed -1");
  var overridden=Difficulty(1,4);overridden.DifficultyOverride=0;Known(ConditionEvaluator.Evaluate(overridden,new SimulatedContext{Difficulty=2}),true,"override replaces context value");
  overridden.DifficultyOverride=uint.MaxValue;Known(ConditionEvaluator.Evaluate(overridden,new SimulatedContext{Difficulty=2}),false,"-1 override sentinel falls back to context");
  overridden.DifficultyOverride=0;Unsupported(ConditionEvaluator.Evaluate(overridden,new SimulatedContext()),"missing simulated context explicit even with override");
  Unsupported(ConditionEvaluator.Evaluate(Difficulty(1,99),new SimulatedContext{Difficulty=1}),"uncovered comparison not false");
  var wrongVersion=Difficulty(1,2);wrongVersion.Version=1;Unsupported(ConditionEvaluator.Evaluate(wrongVersion,new SimulatedContext{Difficulty=1}),"version outside restored subset");
  var hardcore=new ConditionSpec{TypeId=5,ExpectedHardcore=true,ComparisonId=99};Known(ConditionEvaluator.Evaluate(hardcore,new SimulatedContext{MainPlayerHardcoreByte=1}),true,"hardcore equality ignores comparison selector");hardcore.ExpectedHardcore=false;Known(ConditionEvaluator.Evaluate(hardcore,new SimulatedContext{MainPlayerHardcoreByte=1}),false,"hardcore mismatch");Unsupported(ConditionEvaluator.Evaluate(hardcore,new SimulatedContext()),"missing hardcore context");hardcore.ExpectedHardcore=true;Known(ConditionEvaluator.Evaluate(hardcore,new SimulatedContext{MainPlayerHardcoreByte=255}),false,"raw main-player byte not normalized to nonzero bool");
  var wait=new ConditionSpec{TypeId=17,ComparisonId=99};Known(ConditionEvaluator.Evaluate(wait,null),false,"wait leaf returns false without context");
  var unknown=new ConditionSpec{TypeId=7};Unsupported(ConditionEvaluator.Evaluate(unknown,null),"token condition unsupported");
  var context=new SimulatedContext{Difficulty=1};var yes=Difficulty(1,2);var no=Difficulty(0,2);
  Known(ConditionEvaluator.Collection(new ConditionSpec[0],0,false,null),true,"explicit empty AND");Known(ConditionEvaluator.Collection(new ConditionSpec[0],1,false,null),false,"explicit empty OR");Unsupported(ConditionEvaluator.Collection(new ConditionSpec[0],2,false,null),"invalid collection operator unsupported policy");
  var r=ConditionEvaluator.Collection(new[]{no,unknown},0,false,context);Known(r,false,"AND false short circuits unsupported");Assert(r.Evaluated.Count==1&&r.Evaluated[0]==0,"AND call order");
  r=ConditionEvaluator.Collection(new[]{yes,unknown},1,false,context);Known(r,true,"OR true short circuits unsupported");Assert(r.Evaluated.Count==1&&r.Evaluated[0]==0,"OR call order");
  r=ConditionEvaluator.Collection(new[]{yes,unknown},0,false,context);Unsupported(r,"visited unknown in AND stays unsupported");Assert(r.Evaluated.Count==2,"AND unknown visitation");
  r=ConditionEvaluator.Collection(new[]{no,unknown},1,false,context);Unsupported(r,"visited unknown in OR stays unsupported");
  r=ConditionEvaluator.Collection(new[]{wait,yes},0,true,context);Known(r,true,"skip wait AND");Assert(r.Skipped.Count==1&&r.Skipped[0]==0&&r.Evaluated.Count==1&&r.Evaluated[0]==1,"skip does not evaluate leaf");
  Known(ConditionEvaluator.Collection(new[]{wait,no},1,true,context),false,"skip wait OR");Known(ConditionEvaluator.Collection(new[]{wait,unknown},0,false,context),false,"wait false short circuits AND");
  Known(ConditionEvaluator.Collection(new[]{wait},0,true,null),true,"all skipped AND identity");Known(ConditionEvaluator.Collection(new[]{wait},1,true,null),false,"all skipped OR identity");
  var skippedVersion=new ConditionSpec{TypeId=17,Version=99};Known(ConditionEvaluator.Collection(new[]{skippedVersion},0,true,null),true,"skip uses type before leaf dispatch/version policy");
  Unsupported(ConditionEvaluator.Collection(new ConditionSpec[]{null},0,false,null),"null child rejected");
  Console.WriteLine("{\"status\":\"passed\",\"assertions\":"+checks+",\"oracle\":\"fixed static-native-derived truth tables and traces; original game not executed\"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
