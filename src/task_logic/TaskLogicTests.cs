using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
using GrimDynasty.TaskLogic;
public static class TaskLogicTests {
 static int count;
 static void Check(EvaluationResult r,bool? expected,string label){count++;if(r.Value!=expected||(expected.HasValue?r.Status!="Known":r.Status!="Unsupported"))throw new Exception(label+": "+r.Status+" "+r.Value+" "+r.Reason);}
 static Predicate P(int id,bool expected){return new Predicate{TypeId=id,Expected=expected,Reference="Quest/A",TaskUid=9};}
 static Group G(int op,params Predicate[] ps){var g=new Group{Operation=op};g.Conditions.AddRange(ps);return g;}
 public static int Main(){try{
  var w=new World{QuestIndexComplete=true,ServerServiceSlot18NonNull=false,MainPlayerResolved=true,Level=10};w.Core.Difficulty=0;
  Check(Evaluator.EventGate(null,true,w),true,"null event pointer");Check(Evaluator.EventGate(null,false,w),null,"unknown event pointer");
  Check(Evaluator.Collection(G(0),false,w),true,"empty and");Check(Evaluator.Collection(G(1),false,w),false,"empty or");
  Check(Evaluator.Collection(G(0,new Predicate{TypeId=17},new Predicate{TypeId=99}),false,w),false,"and shortcircuit");
  Check(Evaluator.Collection(G(1,new Predicate{TypeId=17},new Predicate{TypeId=99}),false,w),null,"or reaches unknown");
  Check(Evaluator.Collection(G(0,new Predicate{TypeId=17}),true,w),true,"skip wait");
  for(int i=0;i<6;i++)Check(Evaluator.Evaluate(new Predicate{TypeId=3,ComparisonId=i,Value=11},w),new[]{true,true,false,false,false,true}[i],"unsigned level "+i);
  w.Level=uint.MaxValue;Check(Evaluator.Evaluate(new Predicate{TypeId=3,ComparisonId=4,Value=1},w),true,"level high bit unsigned");
  w.MainPlayerResolved=false;Check(Evaluator.Evaluate(new Predicate{TypeId=3},w),null,"level unresolved");w.MainPlayerResolved=true;
  Check(Evaluator.Evaluate(new Predicate{TypeId=3,ComparisonId=9},w),null,"invalid selector strict domain");
  foreach(int id in new[]{0,1,11,19})Check(Evaluator.Evaluate(P(id,false),w),false,"missing quest bypass inversion "+id);
  Check(Evaluator.Evaluate(P(14,false),w),true,"missing quest on task inverted");w.QuestIndexComplete=false;
  Check(Evaluator.Evaluate(P(14,false),w),null,"incomplete index");w.QuestIndexComplete=true;
  var q=new QuestState();w.Quests.Add("Quest/A",q);
  Check(Evaluator.Evaluate(P(0,false),w),true,"empty quest on false");Check(Evaluator.Evaluate(P(19,false),w),true,"empty blocked false");
  Check(Evaluator.Evaluate(P(1,false),w),false,"missing task complete");Check(Evaluator.Evaluate(P(14,false),w),true,"missing task on inverted");Check(Evaluator.Evaluate(P(11,false),w),false,"missing task objective");
  var t=new TaskState{Uid=9,State=2};q.Tasks.Add(t);
  Check(Evaluator.Evaluate(P(0,true),w),true,"quest active");Check(Evaluator.Evaluate(P(14,true),w),true,"task active");Check(Evaluator.Evaluate(P(1,false),w),true,"active not complete");
  t.RuntimeBlocked=255;Check(Evaluator.Evaluate(P(0,true),w),false,"blocked client quest");Check(Evaluator.Evaluate(P(14,false),w),true,"blocked client task");Check(Evaluator.Evaluate(P(19,true),w),true,"raw blocked nonzero");Check(Evaluator.Evaluate(P(11,false),w),false,"blocked objective bypasses expected");
  w.ServerServiceSlot18NonNull=true;Check(Evaluator.Evaluate(P(0,true),w),true,"server slot bypasses block");Check(Evaluator.Evaluate(P(14,true),w),true,"server task bypass");
  t.State=3;Check(Evaluator.Evaluate(P(1,true),w),true,"state3 complete");Check(Evaluator.Evaluate(P(19,true),w),false,"complete blocked excluded");Check(Evaluator.Evaluate(P(11,false),w),false,"inactive objectives earlyfalse");
  t.State=2;w.ServerServiceSlot18NonNull=null;Check(Evaluator.Evaluate(P(14,true),w),null,"unknown server slot");Check(Evaluator.Evaluate(P(1,false),w),true,"complete independent of server slot");
  w.ServerServiceSlot18NonNull=true;Check(Evaluator.Evaluate(P(11,true),w),null,"missing objective snapshot");
  var os=new List<Objective>();t.Objectives.Add(0,os);Check(Evaluator.Evaluate(P(11,true),w),true,"empty objectives all true");
  var o=new Objective{Uid=5,ConditionsKnown=true,Conditions=G(0,new Predicate{TypeId=17})};os.Add(o);
  Check(Evaluator.Evaluate(P(11,true),w),true,"all objectives skip wait");var specific=P(11,true);specific.Version=1;specific.ObjectiveUid=5;
  Check(Evaluator.Evaluate(specific,w),false,"specific objective does not skip wait");specific.Expected=false;Check(Evaluator.Evaluate(specific,w),true,"specific false inversion");
  o.Conditions=null;specific.Expected=true;Check(Evaluator.Evaluate(specific,w),true,"null objective pointer true");o.ConditionsKnown=false;Check(Evaluator.Evaluate(specific,w),null,"unknown objective pointer");o.ConditionsKnown=true;
  specific.ObjectiveUid=6;specific.Expected=false;Check(Evaluator.Evaluate(specific,w),false,"missing specific objective bypasses inversion");
  var wrongCase=P(14,false);wrongCase.Reference="quest/a";Check(Evaluator.Evaluate(wrongCase,w),true,"exact case quest key absent");
  w.Core.Difficulty=1;Check(Evaluator.Evaluate(P(11,true),w),null,"difficulty vector independent");w.Core.Difficulty=0;
  q.Tasks.Add(new TaskState{Uid=9});Check(Evaluator.Evaluate(P(1,true),w),null,"duplicate snapshot rejected");q.Tasks.RemoveAt(1);
  var bad=P(0,true);bad.Version=2;Check(Evaluator.Evaluate(bad,w),null,"uncovered version");
  o.Conditions=G(0,P(11,true));Check(Evaluator.Evaluate(P(11,true),w),null,"recursive objective cycle bounded");
  var tok=new Predicate{TypeId=7,Reference="令牌",Expected=true};w.GameMode=0;w.OtherModeTokens.Add(0,new List<string>{"令牌","abc","abc\0suffix"});w.ModeOneTokens.Add(0,new List<string>());
  Check(Evaluator.Evaluate(tok,w),true,"local bank unicode");w.GameMode=1;Check(Evaluator.Evaluate(tok,w),false,"mode1 separate bank");w.GameMode=2;Check(Evaluator.Evaluate(tok,w),true,"non1 mode uses other bank");
  tok.Reference="ABC";Check(Evaluator.Evaluate(tok,w),false,"token case sensitive");tok.Reference="abc\0querytail";Check(Evaluator.Evaluate(tok,w),true,"wrapper truncates query NUL");
  tok.Reference="abc\0different";w.OtherModeTokens[0]=new List<string>{"abc\0suffix"};Check(Evaluator.Evaluate(tok,w),false,"stored token length preserved");
  tok.Reference="";w.OtherModeTokens[0]=new List<string>{""};Check(Evaluator.Evaluate(tok,w),true,"empty token equality");w.Core.Difficulty=1;Check(Evaluator.Evaluate(tok,w),null,"unknown difficulty token bank");w.Core.Difficulty=0;
  w.MainPlayerResolved=false;Check(Evaluator.Evaluate(tok,w),null,"unresolved token player");w.MainPlayerResolved=true;
  tok.TypeId=18;Check(Evaluator.Evaluate(tok,w),null,"unknown repository snapshot");w.RepositoryTokensComplete=true;Check(Evaluator.Evaluate(tok,w),null,"server ID unknown");w.ServerPlayerId=uint.MaxValue;
  Check(Evaluator.Evaluate(tok,w),false,"server row absent");w.RepositoryTokens.Add(1,new List<string>{""});Check(Evaluator.Evaluate(tok,w),false,"other player's token not server");tok.TypeId=20;Check(Evaluator.Evaluate(tok,w),true,"anyone roster");
  w.RepositoryTokens.Add(uint.MaxValue,new List<string>{""});tok.TypeId=18;Check(Evaluator.Evaluate(tok,w),true,"unsigned server key max");tok.Expected=false;Check(Evaluator.Evaluate(tok,w),false,"token inversion");tok.Reference="\ud800";Check(Evaluator.Evaluate(tok,w),null,"invalid Unicode domain");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+count+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
