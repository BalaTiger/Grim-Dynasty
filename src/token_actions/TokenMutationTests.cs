using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
public static class TokenMutationTests {
 static int n;
 static void Check(MutationResult r,bool? changed,string label){n++;if(r.Changed!=changed||(changed.HasValue?r.Status!="Known":r.Status!="Unsupported"))throw new Exception(label+": "+r.Status+" "+r.Changed+" "+r.Reason);}
 static void Is(bool ok,string label){n++;if(!ok)throw new Exception(label);}
 static bool? Read(World w,int id,string t){return Evaluator.Evaluate(new Predicate{TypeId=id,Reference=t,Expected=true},w).Value;}
 public static int Main(){try{
  var w=new World{RepositoryTokensComplete=true,ServerServiceSlot18NonNull=false,ServerPlayerId=7};
  Check(Mutations.SetPlayerToken(w,7,"a"),false,"set absent row is no-op");Is(w.RepositoryTokens.Count==0,"no implicit player creation");
  Check(Mutations.RemovePlayerToken(w,7,"a"),false,"remove absent row no-op");
  Check(Mutations.SetPlayerTokens(w,7,new List<string>(),null),true,"declare empty row");
  Check(Mutations.SetPlayerToken(w,7,"a"),true,"set new");Is(Read(w,18,"a")==true,"set server read closure");Is(Read(w,20,"a")==true,"set anyone read closure");
  Check(Mutations.SetPlayerToken(w,7,"a"),false,"duplicate set deduplicates");Is(w.RepositoryTokens[7].Count==1,"duplicate count");
  Check(Mutations.SetPlayerToken(w,7,"A"),true,"case distinct");Check(Mutations.RemovePlayerToken(w,7,"a"),true,"remove exact first");Is(Read(w,18,"a")==false&&Read(w,18,"A")==true,"case after removal");
  var source=new List<string>{"x","a","a","y"};Check(Mutations.SetPlayerTokens(w,7,source,null),true,"bulk preserves duplicates");source[0]="changed";Is(w.RepositoryTokens[7][0]=="x","deep vector copy");
  Check(Mutations.RemovePlayerToken(w,7,"a"),true,"first duplicate remove");Is(String.Join(",",w.RepositoryTokens[7])=="x,a,y","first only order preserved");Is(Read(w,18,"a")==true,"one duplicate remains");
  Check(Mutations.RemovePlayerToken(w,7,"a"),true,"second duplicate remove");Is(Read(w,18,"a")==false,"remove closure");Check(Mutations.RemovePlayerToken(w,7,"a"),false,"absent token no-op");
  Check(Mutations.SetPlayerToken(w,7,"abc\0tail"),true,"mutation full embedded NUL");Is(Read(w,18,"abc\0tail")==false,"condition query truncates, stored token does not");Check(Mutations.RemovePlayerToken(w,7,"abc"),false,"remove does not truncate");Check(Mutations.RemovePlayerToken(w,7,"abc\0tail"),true,"remove full NUL name");
  Check(Mutations.SetPlayerToken(w,7,"二十八宿"),true,"unicode");Is(Read(w,18,"二十八宿")==true,"unicode closure");
  Check(Mutations.SetPlayerTokens(w,7,w.RepositoryTokens[7],null),false,"self alias assignment");Check(Mutations.SetPlayerTokens(w,7,new List<string>(),null),true,"bulk empty clears");Is(w.RepositoryTokens.ContainsKey(7)&&w.RepositoryTokens[7].Count==0,"clear retains row");
  Check(Mutations.SetPlayerTokens(w,uint.MaxValue,new List<string>{"z"},null),true,"unsigned max player row");Is(Read(w,20,"z")==true&&Read(w,18,"z")==false,"any vs server");
  w.ServerServiceSlot18NonNull=true;Check(Mutations.SetPlayerTokens(w,8,new List<string>(),null),null,"missing client ID refuses before mutation");Is(!w.RepositoryTokens.ContainsKey(8)&&w.ServerPlayerId==7,"unsupported no partial writes");
  Check(Mutations.SetPlayerTokens(w,8,new List<string>(),9),true,"other client ID preserves server");Is(w.ServerPlayerId==7,"preserved server key");Check(Mutations.SetPlayerTokens(w,8,new List<string>(),8),true,"matching client updates server");Is(w.ServerPlayerId==8,"server key updated");
  w.RepositoryTokensComplete=false;Check(Mutations.SetPlayerToken(w,7,"a"),null,"unknown map");w.RepositoryTokensComplete=true;
  Check(Mutations.SetPlayerToken(w,7,"\ud800"),null,"invalid unicode");Check(Mutations.ExecuteFullTokenAction(9,0,"a",w),null,"full give effects unimplemented");Check(Mutations.ExecuteFullTokenAction(10,0,"a",w),null,"full remove outbound unimplemented");Is(w.RepositoryTokens[7].Count==0,"unsupported full actions do not mutate");
  var t=new TaskState{Uid=9,State=1};t.Objectives.Add(0,new List<Objective>());var s=new TaskMutationSnapshot{Task=t,Difficulty=0,QuestFlags=0,OnAcceptEventCount=0,StatesByDifficulty=new[]{1,1,1}};
  Check(TaskMutations.Start(s),true,"empty task start state1 to2");var q=new QuestState();q.Tasks.Add(t);w.Quests.Add("q",q);w.QuestIndexComplete=true;w.ServerServiceSlot18NonNull=false;
  Is(Evaluator.Evaluate(new Predicate{TypeId=14,Reference="q",TaskUid=9,Expected=true},w).Value==true,"start on-task read closure");
  Check(TaskMutations.CompleteWithoutCallbacks(s,false),null,"active completion requires outbound even no rewards");Is(t.State==2&&s.StatesByDifficulty[0]==1,"unsupported complete atomic");
  t.State=1;s.QuestFlags=4;Check(TaskMutations.Start(s),null,"tutorial requires effect");Is(t.State==1,"unsupported start no write");
  Check(TaskMutations.CompleteWithoutCallbacks(s,true),false,"state1 flag4 reverts to1");s.QuestFlags=0;Check(TaskMutations.CompleteWithoutCallbacks(s,true),true,"inactive completion state1 to3");
  Is(Evaluator.Evaluate(new Predicate{TypeId=1,Reference="q",TaskUid=9,Expected=true},w).Value==true,"complete predicate closure");Is(s.StatesByDifficulty[0]==3&&s.StatesByDifficulty[1]==1,"difficulty state bank isolation");
  Check(TaskMutations.Reset(s),true,"completed empty task reset");Is(t.State==1&&s.StatesByDifficulty[0]==1,"reset state banks");Check(TaskMutations.Reset(s),false,"reset noncompleted no-op");
  s.OnAcceptEventCount=1;Check(TaskMutations.Start(s),null,"on accept unknown execution");s.OnAcceptEventCount=0;t.Objectives[0].Add(new Objective());Check(TaskMutations.Start(s),null,"objective lifecycle domain excluded");
  t.RuntimeBlocked=255;s.Difficulty=null;Check(TaskMutations.Start(s),true,"blocked start writes2 without difficulty callbacks");Is(t.State==2,"blocked native start unconditional state");
  t.State=3;Check(TaskMutations.Reset(s),null,"reset missing difficulty");s.Difficulty=0;Check(TaskMutations.Reset(s),null,"reset nonempty objective callbacks");Is(t.State==3,"unsupported reset no writes");
  t.State=0;Check(TaskMutations.CompleteWithoutCallbacks(s,false),true,"nonactive state0 to3");s.Difficulty=3;Check(TaskMutations.CompleteWithoutCallbacks(s,false),null,"out of domain difficulty");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
