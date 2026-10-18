using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
public static class EventRewardProbeTests {
 static int n;
 static EventSnapshot E(Group g,params int?[] ids){return new EventSnapshot{ConditionPointerKnown=true,Conditions=g,ActionPointerKnown=true,ActionTypes=ids==null?null:new List<int?>(ids)};}
 static void Check(EventSnapshot[] events,bool? expected){n++;var r=EventRewardProbe.HasRewardActions(events,new World());if(r.Value!=expected||(expected.HasValue?r.Status!="Known":r.Status!="Unsupported"))throw new Exception("event reward "+n+": "+r.Reason);}
 public static int Main(){try{
  Check(null,null);Check(new EventSnapshot[]{},false);Check(new[]{E(null,4)},true);Check(new[]{E(null,9,10)},false);
  Check(new[]{E(null,(int?[])null)},false);
  var wait=new Group{Operation=0};wait.Conditions.Add(new Predicate{TypeId=17});
  Check(new[]{E(wait,4)},false);var unknown=new Group{Operation=0};unknown.Conditions.Add(new Predicate{TypeId=99});
  Check(new[]{E(unknown,4)},null);Check(new[]{E(null,4),E(unknown,4)},true);Check(new[]{E(unknown,4),E(null,4)},null);
  Check(new[]{E(unknown,(int?[])null)},null);Check(new[]{E(wait,(int?[])null),E(null,25)},true);
  var missingActions=E(wait,4);missingActions.ActionPointerKnown=false;Check(new[]{missingActions},false);missingActions.Conditions=null;Check(new[]{missingActions},null);
  var missingConditions=E(null,4);missingConditions.ConditionPointerKnown=false;Check(new[]{missingConditions},null);
  Check(new[]{E(null,9),E(null,10),E(null,18)},true);Check(new EventSnapshot[]{null},null);Check(new[]{E(null,null,4)},null);Check(new[]{E(null,4,null)},true);
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
