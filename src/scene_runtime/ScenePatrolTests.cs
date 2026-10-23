using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using GrimDynasty.Session;

namespace GrimDynasty.Scene {
 public static class ScenePatrolTests {
  static int checks;
  static void Check(bool value,string why){if(!value)throw new Exception("Patrol integration: "+why);checks++;}
  static void Reject<T>(Action action,string why) where T:Exception {try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name+": "+why);}
  static Dictionary<string,object> Read(string path){return (Dictionary<string,object>)StrictJson.Parse(File.ReadAllText(path));}
  static void Write(string path,Dictionary<string,object> envelope){envelope["sha256"]=StrictJson.Sha256(StrictJson.Canonical(envelope["payload"]));File.WriteAllText(path,StrictJson.Canonical(envelope),new UTF8Encoding(false));}
  static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
  public static int Run(string directory){
   checks=0;Directory.CreateDirectory(directory);string saveRoot=Path.Combine(directory,"runtime-data","saves");var store=new SceneStore(saveRoot);
   if(store.Exists("patrol"))throw new Exception("Fresh patrol test directory required");
   var state=SceneState.Create("Patrol Check");Check(state.World.TrySetPosition(1100,1050),"player approach origin");
   for(int i=0;i<4;i++)state.TickAgents(.1f);
   var before=state.Agents.Find("road-patrol");
   Check(before.Mode==SceneAgentMode.Chase&&before.TargetId==state.Session.CharacterId&&before.X>1000,"timer adapter uses actual character identity and moves patrol");
   var noAttack=state.Agents.RequestAttack(before.Id);Check(noAttack.Status=="Unsupported"&&noAttack.Reason=="CombatDamageProducerNotIntegrated","no fabricated attack side effect");
   store.Save("patrol",state);var loaded=store.Load("patrol");var after=loaded.Agents.Find(before.Id);
   Check(Bits(before.X)==Bits(after.X)&&Bits(before.Y)==Bits(after.Y)&&Bits(before.FacingX)==Bits(after.FacingX)&&Bits(before.FacingY)==Bits(after.FacingY),"exact actor position and facing roundtrip");
   Check(after.TargetId==loaded.Session.CharacterId&&after.Mode==before.Mode&&after.Intent==before.Intent&&after.PatrolIndex==before.PatrolIndex&&after.Blocked==before.Blocked,"actor decision state roundtrip, no reset to spawn");
   loaded.TickAgents(.1f);Check(loaded.Agents.Find(before.Id).X>after.X&&state.Agents.Find(before.Id).X==before.X,"reopened actor continues independently");
   Check(loaded.World.TrySetPosition(430,1150),"player can leave patrol leash");loaded.TickAgents(.1f);
   Check(loaded.Agents.Find(before.Id).Mode==SceneAgentMode.Return&&loaded.Agents.Find(before.Id).TargetId==null,"lost target returns without stale character identity");
   // Render only to a bitmap; no Form, Application.Run, screenshot or foreground launch.
   Check(loaded.World.TrySetPosition(1100,1050),"render camera origin");
   using(var plain=new Bitmap(1100,760))using(var scene=new Bitmap(1100,760)){
    using(var g=Graphics.FromImage(plain))SceneRenderer.Draw(g,loaded.World,1100,760);
    using(var g=Graphics.FromImage(scene))SceneRenderer.Draw(g,loaded,1100,760);
    int changed=0;for(int y=0;y<760;y+=2)for(int x=0;x<1100;x+=2)if(plain.GetPixel(x,y)!=scene.GetPixel(x,y))changed++;
    Check(changed>30,"actor is actually painted into scene, not merely simulated");scene.Save(Path.Combine(directory,"patrol-scene.png"),ImageFormat.Png);
   }
   string path=Path.Combine(saveRoot,"scene-patrol.save");
   var envelope=Read(path);var payload=(Dictionary<string,object>)envelope["payload"];
   var rows=(List<object>)payload["agents"];var row=(Dictionary<string,object>)rows[0];row["target"]="another-character";Write(path,envelope);
   Reject<SessionCorruptException>(delegate{store.Load("patrol");},"checksummed foreign target is rejected");
   row["target"]=state.Session.CharacterId;row["x_bits"]=Bits(810);row["y_bits"]=Bits(900);Write(path,envelope);
   Reject<SessionCorruptException>(delegate{store.Load("patrol");},"checksummed blocked actor position is rejected");
   row["x_bits"]=Bits(before.X);row["y_bits"]=Bits(before.Y);rows.Clear();Write(path,envelope);
   Reject<SessionCorruptException>(delegate{store.Load("patrol");},"missing actor is not silently respawned in schema 2");
   payload["schema"]=99;Write(path,envelope);
   Reject<SessionUnsupportedException>(delegate{store.Load("patrol");},"unknown future schema stays Unsupported");
   payload.Remove("agents");payload["schema"]=1;payload["content"]="liaoxi-scene-v1";
   string legacy=Path.Combine(saveRoot,"scene-legacy.save");Write(legacy,envelope);
   var migrated=store.Load("legacy");Check(migrated.MigratedVisualPatrol&&migrated.Session.CharacterId==state.Session.CharacterId&&migrated.Session.View().money==state.Session.View().money,"only explicit schema1 migration preserves session and adds authored patrol");
   store.Save("legacy",migrated);var reopened=store.Load("legacy");
   Check(!reopened.MigratedVisualPatrol&&reopened.Agents.Find(before.Id).X==1000,"migrated save becomes explicit schema2");
   return checks;
  }
 }
}
