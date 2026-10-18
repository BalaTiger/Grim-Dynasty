using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
class ExperienceQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 static int Main(){try{
  var x=ExampleExperienceQuest.Create(75);var a=x.Quest.Auto;var p=x.Player;var w=x.Completion.World;var trace=new List<object>();Need(AutomaticTasks.Start(a).Status=="Known","start");AutomaticTasks.Update(a);trace.Add(new{step="accept_report",experience=p.Experience1764,lifetime=p.Lifetime1968,level=p.Level1760,name_cache_cold=!w.ExperienceNameCache.Initialized});
  for(int i=0;i<2;i++){Need(x.Quest.Kill().Delivered==1,"decoded event");Need(AutomaticTasks.Update(a).Status=="Known","automatic update");}
  Need(a.Runtime.State.Task.State==3&&p.Experience1764==175&&p.Lifetime1968==275&&p.Level1760==4,"XP completion");trace.Add(new{step="complete_and_route_local_bonus",target=w.DispatchTrace[0].TargetId,amount=w.DispatchTrace[0].Amount,source=w.DispatchTrace[0].ExperienceSource,origin=w.DispatchTrace[0].OriginId});trace.Add(new{step="own_runtime_state_written",experience=p.Experience1764,lifetime=p.Lifetime1968,level=p.Level1760,equation_input=p.EquationInput1710,life=p.Bio.Life,energy=p.Bio.Energy,stamina=p.Bio.Stamina,life_stats_row=p.Bio.LifeStats[0],reward_task_uid=x.Completion.QuestsByUid[2].RewardTaskUidsAt50[0],kill_handler_detached=!x.Quest.Counter.CallbackOwnerAttached});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",trace,scope="Own original scenario and decoded kill inputs. Static-derived GiveExperience -> constructed Bonus -> local inbound -> TakeBonus -> below-level ReceiveExperience and actual own XP/stat writes. Explicit sampled threshold, zero XP modifier, empty devotion skill vector and canonical zero-Bonus Bio. No native formula interpreter, level-up, native combat/UI/network, original DLL/resources or saves."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
