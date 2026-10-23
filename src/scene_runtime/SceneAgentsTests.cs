using System;
using System.Drawing;

namespace GrimDynasty.Scene {
 // Pure headless adapter checks. The numeric inputs below are own test geometry,
 // not original AI statistics, health, weapon damage or inferred balance.
 public static class SceneAgentsTests {
  static int checks;
  static void Check(bool value,string message){checks++;if(!value)throw new Exception("Scene agent check failed: "+message);}
  static void Reject<T>(Action action,string message) where T:Exception {
   try{action();}catch(T){checks++;return;}throw new Exception("Scene agent expected rejection: "+message);
  }
  static SceneAgentSpec Spec(string id,float x,float y,PointF[] patrol,float speed){
   return new SceneAgentSpec(id,"own/tests/patrol.dbr",new PointF(x,y),patrol,speed,200,300,1);
  }
  static bool Same(SceneAgentSnapshot a,SceneAgentSnapshot b){
   return a.Id==b.Id&&a.RecordName==b.RecordName&&a.X==b.X&&a.Y==b.Y&&a.FacingX==b.FacingX&&a.FacingY==b.FacingY
    &&a.Mode==b.Mode&&a.Intent==b.Intent&&a.TargetId==b.TargetId&&a.Blocked==b.Blocked&&a.PatrolIndex==b.PatrolIndex;
  }
  static SceneAgentSnapshot Copy(SceneAgentSnapshot s,string id=null,string record=null,float? x=null,float? y=null,
   float? fx=null,float? fy=null,SceneAgentMode? mode=null,SceneAgentIntent? intent=null,
   string target=null,bool replaceTarget=false,int? patrolIndex=null,bool? blocked=null){
   return new SceneAgentSnapshot(id??s.Id,record??s.RecordName,x??s.X,y??s.Y,fx??s.FacingX,fy??s.FacingY,
    mode??s.Mode,intent??s.Intent,replaceTarget?target:s.TargetId,blocked??s.Blocked,patrolIndex??s.PatrolIndex);
  }
  static void Unchanged(SceneAgents group,SceneAgentSnapshot[] expected,string message){
   bool equal=group.GetSnapshots().Length==expected.Length;
   foreach(var snapshot in expected){var actual=group.Find(snapshot.Id);equal=equal&&actual!=null&&Same(actual,snapshot);}
   Check(equal,message);
  }
  public static int Run(){
   checks=0;
   var world=new SceneWorld();var group=new SceneAgents(world);
   group.Add(Spec("guard",430,1150,new[]{new PointF(500,1150),new PointF(430,1150)},100));
   group.Step(.1f,null);var patrol=group.Find("guard");
   Check(patrol.Mode==SceneAgentMode.Patrol&&patrol.Intent==SceneAgentIntent.Patrol&&patrol.X>430&&patrol.Y==1150,"patrol changes actual world position");
   Check(world.PlayerX==430&&world.PlayerY==1150,"agent movement does not move player");
   group.Step(.1f,new SceneAgentTarget("player",500,1150));var chase=group.Find("guard");
   Check(chase.Mode==SceneAgentMode.Chase&&chase.Intent==SceneAgentIntent.ApproachTarget&&chase.TargetId=="player"&&chase.X>patrol.X,"nearby target produces approach intent");
   group.Step(.1f,null);Check(group.Find("guard").Mode==SceneAgentMode.Return&&group.Find("guard").TargetId==null,"lost target begins return");
   for(int i=0;i<20&&group.Find("guard").Mode!=SceneAgentMode.Patrol;i++)group.Step(.1f,null);
   var home=group.Find("guard");Check(home.Mode==SceneAgentMode.Patrol&&Math.Abs(home.X-430)<=1&&Math.Abs(home.Y-1150)<=1,"return reaches home before patrol resumes");
   group.Step(.1f,new SceneAgentTarget("first",500,1150));group.Step(.1f,new SceneAgentTarget("different",500,1150));
   Check(group.Find("guard").Mode==SceneAgentMode.Return&&group.Find("guard").TargetId==null,"target identity replacement does not silently retarget");

   var points=new[]{new PointF(500,1150)};var copiedSpec=Spec("copied",430,1150,points,100);points[0]=new PointF(810,900);
   var returnedPoints=copiedSpec.Patrol;returnedPoints[0]=new PointF(810,900);
   var copied=new SceneAgents(new SceneWorld());copied.Add(copiedSpec);copied.Step(.1f,null);
   Check(copied.Find("copied").X>430&&copied.Find("copied").Y==1150,"caller arrays do not replace patrol geometry");
   var exposed=copied.GetSnapshots();exposed[0]=null;
   Check(copied.Find("copied")!=null,"returned snapshot array cannot alter live agents");

   var wall=new SceneAgents(new SceneWorld());wall.Add(Spec("wall",780,900,new[]{new PointF(900,900)},4096));wall.Step(.1f,null);
   var stopped=wall.Find("wall");Check(stopped.X<=790&&stopped.X>780&&stopped.Y==900&&stopped.Blocked&&world.IsWalkable(stopped.X,stopped.Y),"fast move cannot tunnel through actual fort wall");
   var gate=new SceneAgents(new SceneWorld());gate.Add(Spec("gate",780,1050,new[]{new PointF(900,1050)},4096));gate.Step(.1f,null);
   Check(gate.Find("gate").X>830&&!gate.Find("gate").Blocked,"same adapter passes actual gate opening");
   var shortStep=new SceneAgents(new SceneWorld());var longStep=new SceneAgents(new SceneWorld());
   shortStep.Add(Spec("clock",430,1150,new[]{new PointF(600,1150)},100));longStep.Add(Spec("clock",430,1150,new[]{new PointF(600,1150)},100));
   shortStep.Step(.1f,null);longStep.Step(1000,null);Check(Same(shortStep.Find("clock"),longStep.Find("clock")),"large delta is bounded to one adapter step");
   var before=longStep.GetSnapshots();longStep.Step(0,null);Unchanged(longStep,before,"zero delta changes no state");
   Reject<ArgumentOutOfRangeException>(delegate{longStep.Step(Single.NaN,null);},"NaN delta");
   Reject<ArgumentOutOfRangeException>(delegate{longStep.Step(Single.PositiveInfinity,null);},"infinite delta");
   Reject<ArgumentOutOfRangeException>(delegate{longStep.Step(-1,null);},"negative delta");
   Reject<ArgumentException>(delegate{longStep.Step(.1f,new SceneAgentTarget("invalid",810,900));},"blocked target");
   Unchanged(longStep,before,"invalid step inputs preserve agents");
   Reject<ArgumentException>(delegate{longStep.Add(Spec("clock",430,1150,new PointF[0],100));},"duplicate identity");
   Reject<ArgumentException>(delegate{longStep.Add(Spec("blocked",810,900,new PointF[0],100));},"blocked home");
   Check(longStep.GetSnapshots().Length==1,"failed additions do not insert agents");
   var isolated=new SceneAgents(new SceneWorld());isolated.Add(Spec("clock",430,1150,new[]{new PointF(600,1150)},100));
   Check(isolated.Find("clock").X==430&&longStep.Find("clock").X>430,"separate scene sessions do not share agent state");
   var unsupported=longStep.RequestAttack("clock");
   Check(unsupported.Status=="Unsupported"&&unsupported.Reason=="CombatDamageProducerNotIntegrated","attack never silently invents damage");
   Check(longStep.RequestAttack("missing").Reason=="UnknownSceneAgent","missing combat actor remains explicit");
   Unchanged(longStep,before,"unsupported attack preserves movement state");

   var restored=new SceneAgents(new SceneWorld());
   restored.Add(Spec("one",430,1150,new[]{new PointF(600,1150)},100));
   restored.Add(Spec("two",500,1100,new[]{new PointF(650,1100)},100));
   restored.Step(.1f,new SceneAgentTarget("player",600,1150));
   var saved=restored.GetSnapshots();restored.Step(.1f,null);
   restored.RestoreSnapshots(new[]{saved[1],saved[0]});Unchanged(restored,saved,"round trip restores every mutable field independent of input order");
   // First entry is valid and different. A bad second entry must prevent both writes.
   var changedFirst=Copy(saved[0],x:saved[0].X+1);
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{changedFirst,Copy(saved[1],x:810,y:900)});},"blocked position in later entry");
   Unchanged(restored,saved,"late validation failure is atomic");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0]});},"missing agent");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],saved[0]});},"duplicate snapshot identity");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],id:"unknown")});},"unconfigured identity");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],record:"own/tests/other.dbr")});},"record must match configuration");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],x:Single.NaN)});},"nonfinite position");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],fx:Single.PositiveInfinity)});},"nonfinite facing");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],fx:0,fy:0)});},"zero facing");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],mode:(SceneAgentMode)99)});},"unknown mode");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],intent:(SceneAgentIntent)99)});},"unknown intent");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],patrolIndex:1)});},"out of range patrol index");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],target:null,replaceTarget:true)});},"chase without target");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],mode:SceneAgentMode.Patrol,intent:SceneAgentIntent.Patrol)});},"nonchase retaining target");
   Reject<ArgumentException>(delegate{restored.RestoreSnapshots(new[]{saved[0],Copy(saved[1],mode:SceneAgentMode.Return,intent:SceneAgentIntent.Patrol,target:null,replaceTarget:true)});},"return with patrol intent");
   Unchanged(restored,saved,"malformed restore attempts do not overwrite accepted state");
   restored.Step(.1f,new SceneAgentTarget("player",600,1150));
   Check(restored.Find("one").X>saved[0].X&&restored.Find("two").X>saved[1].X,"restored state can continue geometric simulation");
   return checks;
  }
 }
}
