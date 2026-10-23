using System;
using System.Collections.Generic;
using System.Drawing;

namespace GrimDynasty.Scene {
 // This is a project-owned scene adapter. Its explicit presentation parameters
 // are not native AI, attack timing, attributes, hit ranges or damage values.
 public enum SceneAgentMode { Patrol, Chase, Return }
 public enum SceneAgentIntent { None, Patrol, ApproachTarget, ReturnHome }

 public sealed class SceneAgentTarget {
  public readonly string Id;
  public readonly float X,Y;
  public SceneAgentTarget(string id,float x,float y){
   SceneAgents.RequireName(id,"id");SceneAgents.RequireFinite(x,"x");SceneAgents.RequireFinite(y,"y");
   Id=id;X=x;Y=y;
  }
 }

 public sealed class SceneAgentSpec {
  public readonly string Id,RecordName;
  public readonly PointF Home;
  public readonly float MoveSpeed,AcquireDistance,ReturnDistance,ArrivalDistance;
  readonly PointF[] patrol;
  public PointF[] Patrol {get{return (PointF[])patrol.Clone();}}
  public SceneAgentSpec(string id,string recordName,PointF home,PointF[] patrolPoints,
   float moveSpeed,float acquireDistance,float returnDistance,float arrivalDistance){
   SceneAgents.RequireName(id,"id");SceneAgents.RequireName(recordName,"recordName");
   SceneAgents.RequireFinite(home.X,"home.X");SceneAgents.RequireFinite(home.Y,"home.Y");
   SceneAgents.RequireFinite(moveSpeed,"moveSpeed");SceneAgents.RequireFinite(acquireDistance,"acquireDistance");
   SceneAgents.RequireFinite(returnDistance,"returnDistance");SceneAgents.RequireFinite(arrivalDistance,"arrivalDistance");
   // Own bounded work domain, not a cap on original-game movement or sensing.
   if(moveSpeed<=0||moveSpeed>4096)throw new ArgumentOutOfRangeException("moveSpeed");
   if(acquireDistance<=0||returnDistance<acquireDistance)throw new ArgumentOutOfRangeException("acquireDistance");
   if(arrivalDistance<=0||arrivalDistance>4)throw new ArgumentOutOfRangeException("arrivalDistance");
   if(patrolPoints==null)throw new ArgumentNullException("patrolPoints");
   if(patrolPoints.Length>128)throw new ArgumentOutOfRangeException("patrolPoints");
   foreach(PointF point in patrolPoints){SceneAgents.RequireFinite(point.X,"patrol.X");SceneAgents.RequireFinite(point.Y,"patrol.Y");}
   Id=id;RecordName=recordName;Home=home;patrol=(PointF[])patrolPoints.Clone();
   MoveSpeed=moveSpeed;AcquireDistance=acquireDistance;ReturnDistance=returnDistance;ArrivalDistance=arrivalDistance;
  }
 }

 // Immutable copies are safe for rendering and cannot mutate another session.
 public sealed class SceneAgentSnapshot {
  public readonly string Id,RecordName,TargetId;
  public readonly float X,Y,FacingX,FacingY;
  public readonly SceneAgentMode Mode;
  public readonly SceneAgentIntent Intent;
  public readonly bool Blocked;
  public readonly int PatrolIndex;
  public SceneAgentSnapshot(string id,string recordName,float x,float y,float facingX,float facingY,
   SceneAgentMode mode,SceneAgentIntent intent,string targetId,bool blocked,int patrolIndex){
   Id=id;RecordName=recordName;X=x;Y=y;FacingX=facingX;FacingY=facingY;
   Mode=mode;Intent=intent;TargetId=targetId;Blocked=blocked;PatrolIndex=patrolIndex;
  }
 }

 public sealed class SceneAgentCombatResult {
  public readonly string Status,Reason,AgentId,TargetId;
  internal SceneAgentCombatResult(string reason,string agentId,string targetId){
   Status="Unsupported";Reason=reason;AgentId=agentId;TargetId=targetId;
  }
 }

 public sealed class SceneAgents {
  public const float MaxDeltaSeconds=0.1f;
  public const int MaxAgents=32;
  readonly SceneWorld world;
  readonly List<Agent> agents=new List<Agent>();
  sealed class Agent {
   internal readonly SceneAgentSpec Spec;
   internal readonly PointF[] Patrol;
   internal float X,Y,FacingX,FacingY=1;
   internal SceneAgentMode Mode=SceneAgentMode.Patrol;
   internal SceneAgentIntent Intent=SceneAgentIntent.None;
   internal string TargetId;
   internal bool Blocked;
   internal int PatrolIndex;
   internal Agent(SceneAgentSpec spec){Spec=spec;Patrol=spec.Patrol;X=spec.Home.X;Y=spec.Home.Y;}
   internal SceneAgentSnapshot Snapshot(){return new SceneAgentSnapshot(Spec.Id,Spec.RecordName,X,Y,FacingX,FacingY,Mode,Intent,TargetId,Blocked,PatrolIndex);}
  }
  public SceneAgents(SceneWorld sceneWorld){
   if(sceneWorld==null)throw new ArgumentNullException("sceneWorld");world=sceneWorld;
  }
  internal static void RequireName(string value,string parameter){
   if(String.IsNullOrWhiteSpace(value)||value!=value.Trim())throw new ArgumentException("Nonempty scene identifier required",parameter);
   foreach(char c in value)if(Char.IsControl(c))throw new ArgumentException("Control characters are not supported",parameter);
  }
  internal static void RequireFinite(float value,string parameter){
   if(Single.IsNaN(value)||Single.IsInfinity(value))throw new ArgumentOutOfRangeException(parameter,"Finite value required");
  }
  public void Add(SceneAgentSpec spec){
   if(spec==null)throw new ArgumentNullException("spec");
   if(agents.Count>=MaxAgents)throw new InvalidOperationException("Scene agent budget exceeded");
   foreach(var agent in agents)if(String.Equals(agent.Spec.Id,spec.Id,StringComparison.Ordinal))throw new ArgumentException("Duplicate scene agent identity","spec");
   if(!world.IsWalkable(spec.Home.X,spec.Home.Y))throw new ArgumentException("Agent home is outside walkable scene","spec");
   foreach(PointF point in spec.Patrol)if(!world.IsWalkable(point.X,point.Y))throw new ArgumentException("Patrol point is outside walkable scene","spec");
   agents.Add(new Agent(spec));
  }
  public SceneAgentSnapshot[] GetSnapshots(){
   var result=new SceneAgentSnapshot[agents.Count];for(int i=0;i<agents.Count;i++)result[i]=agents[i].Snapshot();return result;
  }
  public SceneAgentSnapshot Find(string id){
   foreach(var agent in agents)if(String.Equals(agent.Spec.Id,id,StringComparison.Ordinal))return agent.Snapshot();return null;
  }
  public void RestoreSnapshots(SceneAgentSnapshot[] snapshots){
   if(snapshots==null)throw new ArgumentNullException("snapshots");
   if(snapshots.Length!=agents.Count)throw new ArgumentException("Scene snapshot must contain the configured agent set","snapshots");
   var candidates=new SceneAgentSnapshot[agents.Count];var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(var snapshot in snapshots){
    if(snapshot==null||snapshot.Id==null||!seen.Add(snapshot.Id))throw new ArgumentException("Null or duplicate scene snapshot","snapshots");
    int index=-1;for(int i=0;i<agents.Count;i++)if(String.Equals(agents[i].Spec.Id,snapshot.Id,StringComparison.Ordinal)){index=i;break;}
    if(index<0||!String.Equals(agents[index].Spec.RecordName,snapshot.RecordName,StringComparison.Ordinal))throw new ArgumentException("Scene snapshot identity or record differs from configuration","snapshots");
    RequireFinite(snapshot.X,"snapshot.X");RequireFinite(snapshot.Y,"snapshot.Y");
    RequireFinite(snapshot.FacingX,"snapshot.FacingX");RequireFinite(snapshot.FacingY,"snapshot.FacingY");
    if(!world.IsWalkable(snapshot.X,snapshot.Y))throw new ArgumentException("Scene snapshot position is blocked","snapshots");
    double facingLength=(double)snapshot.FacingX*snapshot.FacingX+(double)snapshot.FacingY*snapshot.FacingY;
    if(Math.Abs(facingLength-1)>0.0001)throw new ArgumentException("Scene snapshot facing must be a unit direction","snapshots");
    if(!Enum.IsDefined(typeof(SceneAgentMode),snapshot.Mode)||!Enum.IsDefined(typeof(SceneAgentIntent),snapshot.Intent))throw new ArgumentException("Unknown scene snapshot mode or intent","snapshots");
    int count=agents[index].Patrol.Length;
    if(snapshot.PatrolIndex<0||(count==0?snapshot.PatrolIndex!=0:snapshot.PatrolIndex>=count))throw new ArgumentException("Scene snapshot patrol index invalid","snapshots");
    if(snapshot.Mode==SceneAgentMode.Chase){
     RequireName(snapshot.TargetId,"snapshot.TargetId");
     if(snapshot.Intent!=SceneAgentIntent.ApproachTarget)throw new ArgumentException("Chasing snapshot requires approach intent","snapshots");
    }else{
     if(snapshot.TargetId!=null)throw new ArgumentException("Nonchasing snapshot cannot retain a target","snapshots");
     if(snapshot.Mode==SceneAgentMode.Return&&snapshot.Intent!=SceneAgentIntent.ReturnHome)throw new ArgumentException("Returning snapshot requires return intent","snapshots");
     if(snapshot.Mode==SceneAgentMode.Patrol&&(snapshot.Intent!=SceneAgentIntent.None&&snapshot.Intent!=SceneAgentIntent.Patrol))throw new ArgumentException("Patrol snapshot intent invalid","snapshots");
     if(snapshot.Mode==SceneAgentMode.Patrol&&count==0&&snapshot.Intent!=SceneAgentIntent.None)throw new ArgumentException("Empty patrol has no movement intent","snapshots");
    }
    if(snapshot.Intent==SceneAgentIntent.None&&snapshot.Blocked)throw new ArgumentException("Idle snapshot cannot report a blocked move","snapshots");
    candidates[index]=snapshot;
   }
   // Every identity and mutable field is checked before the first live write.
   // Specs/waypoints are never accepted from the saved snapshot or replaced.
   for(int i=0;i<agents.Count;i++){
    var agent=agents[i];var snapshot=candidates[i];
    agent.X=snapshot.X;agent.Y=snapshot.Y;agent.FacingX=snapshot.FacingX;agent.FacingY=snapshot.FacingY;
    agent.Mode=snapshot.Mode;agent.Intent=snapshot.Intent;agent.TargetId=snapshot.TargetId;
    agent.Blocked=snapshot.Blocked;agent.PatrolIndex=snapshot.PatrolIndex;
   }
  }
  public SceneAgentCombatResult RequestAttack(string agentId){
   var agent=Find(agentId);
   return new SceneAgentCombatResult(agent==null?"UnknownSceneAgent":"CombatDamageProducerNotIntegrated",agentId,agent==null?null:agent.TargetId);
  }
  public void Step(float dt,SceneAgentTarget target){
   // Validate the complete external input before changing any agent.
   RequireFinite(dt,"dt");if(dt<0)throw new ArgumentOutOfRangeException("dt");
   if(target!=null&&!world.IsWalkable(target.X,target.Y))throw new ArgumentException("Target is outside walkable scene","target");
   dt=Math.Min(dt,MaxDeltaSeconds);if(dt==0)return;
   foreach(var agent in agents)StepAgent(agent,dt,target);
  }
  static double Distance(float ax,float ay,float bx,float by){
   double dx=(double)ax-bx,dy=(double)ay-by;return Math.Sqrt(dx*dx+dy*dy);
  }
  void StepAgent(Agent agent,float dt,SceneAgentTarget target){
   var spec=agent.Spec;agent.Blocked=false;
   if(agent.Mode==SceneAgentMode.Chase){
    if(target==null||!String.Equals(target.Id,agent.TargetId,StringComparison.Ordinal)
     ||Distance(agent.X,agent.Y,target.X,target.Y)>spec.ReturnDistance
     ||Distance(spec.Home.X,spec.Home.Y,target.X,target.Y)>spec.ReturnDistance){
     agent.Mode=SceneAgentMode.Return;agent.TargetId=null;
    }
   }else if(agent.Mode==SceneAgentMode.Patrol&&target!=null
    &&Distance(agent.X,agent.Y,target.X,target.Y)<=spec.AcquireDistance
    &&Distance(spec.Home.X,spec.Home.Y,target.X,target.Y)<=spec.ReturnDistance){
    agent.Mode=SceneAgentMode.Chase;agent.TargetId=target.Id;
   }

   if(agent.Mode==SceneAgentMode.Return){
    agent.Intent=SceneAgentIntent.ReturnHome;
    if(Distance(agent.X,agent.Y,spec.Home.X,spec.Home.Y)<=spec.ArrivalDistance){
     agent.Mode=SceneAgentMode.Patrol;agent.Intent=SceneAgentIntent.None;agent.PatrolIndex=0;return;
    }
    MoveToward(agent,spec.Home.X,spec.Home.Y,dt);return;
   }
   if(agent.Mode==SceneAgentMode.Chase){
    agent.Intent=SceneAgentIntent.ApproachTarget;
    // Arrival only stops geometric movement. It does not create a hit/death.
    if(Distance(agent.X,agent.Y,target.X,target.Y)>spec.ArrivalDistance)MoveToward(agent,target.X,target.Y,dt);
    return;
   }
   agent.TargetId=null;
   if(agent.Patrol.Length==0){agent.Intent=SceneAgentIntent.None;return;}
   agent.Intent=SceneAgentIntent.Patrol;
   PointF goal=agent.Patrol[agent.PatrolIndex];
   if(Distance(agent.X,agent.Y,goal.X,goal.Y)<=spec.ArrivalDistance){
    agent.PatrolIndex=(agent.PatrolIndex+1)%agent.Patrol.Length;goal=agent.Patrol[agent.PatrolIndex];
   }
   if(Distance(agent.X,agent.Y,goal.X,goal.Y)>spec.ArrivalDistance)MoveToward(agent,goal.X,goal.Y,dt);
  }
  void MoveToward(Agent agent,float goalX,float goalY,float dt){
   double dx=(double)goalX-agent.X,dy=(double)goalY-agent.Y;
   double distance=Math.Sqrt(dx*dx+dy*dy);if(distance==0)return;
   agent.FacingX=(float)(dx/distance);agent.FacingY=(float)(dy/distance);
   double travel=Math.Min(distance,(double)agent.Spec.MoveSpeed*dt);
   float mx=(float)(dx/distance*travel),my=(float)(dy/distance*travel);
   int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Abs(mx),Math.Abs(my))/4f));
   float sx=mx/steps,sy=my/steps;
   // Uses the existing scene radius and geometry, including the real fort gate.
   // This is axis-separated collision/sliding, not native navigation or crowd AI.
   for(int i=0;i<steps;i++){
    if(world.IsWalkable(agent.X+sx,agent.Y))agent.X+=sx;else if(sx!=0)agent.Blocked=true;
    if(world.IsWalkable(agent.X,agent.Y+sy))agent.Y+=sy;else if(sy!=0)agent.Blocked=true;
   }
  }
 }
}
