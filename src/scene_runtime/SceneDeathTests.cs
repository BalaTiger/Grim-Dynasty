using System;
using System.IO;
using GrimDynasty.Session;

namespace GrimDynasty.Scene {
 public static class SceneDeathTests {
  static int checks;
  static void Check(bool value,string why){if(!value)throw new Exception("Scene death bridge: "+why);checks++;}
  static void Reject<T>(Action action,string why) where T:Exception {try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name+": "+why);}
  static uint Bits(float f){return BitConverter.ToUInt32(BitConverter.GetBytes(f),0);}
  public static int Run(string directory){
   checks=0;var store=new SceneStore(Path.Combine(directory,"runtime-data","saves"));
   if(store.Exists("death"))throw new Exception("Fresh scene death directory required");
   var state=SceneState.Create("Death Fixture");
   Check(state.Session.BeginDeathObjective(SessionDeathFixtures.MoneyProfile,"fixture-encounter").Changed,"explicit fixture activation");
   Check(state.World.TrySetPosition(1100,1050),"approach position");state.World.Move(0,-1,.01f);state.TickAgents(.1f);
   store.Save("death",state);string path=Path.Combine(directory,"runtime-data","saves","scene-death.save");
   string oldBytes=File.ReadAllText(path);var first=state.Session.PrepareExternalDeath(SessionDeathFixtures.Observation(state.Session,200));
   Check(first.Changed&&first.ValidateSource(state.Session),"first candidate ready");
   Check(state.Session.DeathView().Count==0&&first.Candidate.DeathView().Count==1,"prepare has no live death effect");
   using(var gate=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
    Reject<SessionConflictException>(delegate{store.CommitExternalDeath("death",state,first);},"locked save prevents publication");
   Check(File.ReadAllText(path)==oldBytes&&state.Session.DeathView().Count==0&&state.Session.View().money==50,"lock failure preserves bytes and live wallet");
   var committed=store.CommitExternalDeath("death",state,first);
   Check(committed.Session.DeathView().Count==1&&state.Session.DeathView().Count==0,"successful save returns independent committed candidate");
   Check(committed.Session.StoreRevision==2&&state.Session.StoreRevision==1,"revision belongs to committed state only");
   Check(!Object.ReferenceEquals(committed.World,state.World)&&!Object.ReferenceEquals(committed.Agents,state.Agents),"mutable presentation state not shared");
   Check(Bits(committed.World.PlayerX)==Bits(state.World.PlayerX)&&Bits(committed.World.PlayerY)==Bits(state.World.PlayerY)&&Bits(committed.World.FacingX)==Bits(state.World.FacingX)&&Bits(committed.World.FacingY)==Bits(state.World.FacingY),"commit retains position and facing exactly");
   var before=state.Agents.Find("road-patrol");var after=committed.Agents.Find("road-patrol");
   Check(Bits(before.X)==Bits(after.X)&&Bits(before.Y)==Bits(after.Y)&&after.TargetId==committed.Session.CharacterId&&after.Mode==before.Mode,"commit retains actual patrol and target identity");
   var loaded=store.Load("death");Check(loaded.Session.DeathView().Count==1&&loaded.Session.DeathView().UsesSessionWallet,"scene reload restores receipt and actual session wallet ownership");
   var second=loaded.Session.PrepareExternalDeath(SessionDeathFixtures.Observation(loaded.Session,201));
   Check(second.Candidate.View().money==75&&loaded.Session.View().money==50,"reward staged in candidate only");
   loaded.Session.Execute("accept-delivery");
   Reject<SessionConflictException>(delegate{store.CommitExternalDeath("death",loaded,second);},"unsaved ordinary command invalidates prepared death");
   Check(loaded.Session.DeathView().Count==1&&loaded.Session.View().delivery_accepted&&loaded.Session.View().money==50,"stale preparation does not discard ordinary command");
   second=loaded.Session.PrepareExternalDeath(SessionDeathFixtures.Observation(loaded.Session,201));
   var finished=store.CommitExternalDeath("death",loaded,second);
   Check(finished.Session.View().money==75&&finished.Session.DeathView().Completed&&finished.Session.View().delivery_accepted,"same commit persists ordinary action and reward");
   var reopened=store.Load("death");
   Check(reopened.Session.DeathView().Count==2&&reopened.Session.DeathView().RewardActionsApplied==1&&reopened.Session.View().money==75,"reopened reward exactly once");
   var duplicate=reopened.Session.PrepareExternalDeath(SessionDeathFixtures.Observation(reopened.Session,201));
   Check(duplicate.Status=="Known"&&!duplicate.Changed&&duplicate.Candidate==null,"duplicate after reopen has no candidate/reward");
   Check(reopened.Session.Execute("deliver").Changed&&reopened.Session.View().money==150,"ordinary inventory reward preserves prior kill reward");
   store.Save("death",reopened);var final=store.Load("death");
   Check(final.Session.View().money==150&&final.Session.DeathView().Count==2&&final.Session.View().same_object_identity&&final.Session.View().shared_world_identity,"combined journal reconstructs both ownership graphs");
   var stale=SceneState.Create("Stale Fixture");stale.Session.BeginDeathObjective(SessionDeathFixtures.NoRewardProfile,"stale");store.Save("stale",stale);
   var stalePreparation=stale.Session.PrepareExternalDeath(SessionDeathFixtures.Observation(stale.Session,200));
   var external=store.Load("stale");external.Session.Execute("accept-delivery");store.Save("stale",external);
   Reject<SessionConflictException>(delegate{store.CommitExternalDeath("stale",stale,stalePreparation);},"disk conflict prevents candidate publication");
   Check(stale.Session.DeathView().Count==0&&store.Load("stale").Session.View().delivery_accepted,"disk conflict preserves old live state and newer disk state");
   return checks;
  }
 }
}
