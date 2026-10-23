using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using GrimDynasty.Session;

namespace GrimDynasty.Scene {
 public static class SceneTests {
  static int checks;
  static void Check(bool ok,string label){if(!ok)throw new Exception("Scene check failed: "+label);checks++;}
  static void Reject<T>(Action action,string label) where T:Exception {try{action();}catch(T){checks++;return;}throw new Exception("Scene expected rejection: "+label);}
  static void Walk(SceneWorld world,PointF target){
   for(int n=0;n<3000;n++){float dx=target.X-world.PlayerX,dy=target.Y-world.PlayerY;float distance=(float)Math.Sqrt(dx*dx+dy*dy);
    if(distance<1.5)return;world.Move(dx,dy,Math.Min(1f/30,distance/SceneWorld.MoveSpeed));
   }throw new Exception("Scene route blocked at "+world.PlayerX+","+world.PlayerY+" towards "+target);
  }
  static void Render(SceneWorld world,string path){using(var bitmap=new Bitmap(1100,760))using(var g=Graphics.FromImage(bitmap)){SceneRenderer.Draw(g,world,1100,760);bitmap.Save(path,ImageFormat.Png);}}
  public static int Run(string directory){
   checks=0;Directory.CreateDirectory(directory);var store=new SceneStore(Path.Combine(directory,"runtime-data","saves"));
   if(store.Exists("route"))throw new ArgumentException("Fresh scene test root required");
   var state=SceneState.Create("Route Guard");var world=state.World;
   Check(world.IsWalkable(world.PlayerX,world.PlayerY),"initial position valid");
   Render(world,Path.Combine(directory,"scene-start.png"));
   Check(!world.TrySetPosition(-1,0)&&!world.TrySetPosition(Single.NaN,10),"invalid position does not default");
   Reject<ArgumentOutOfRangeException>(delegate{world.Move(Single.PositiveInfinity,0,.03f);},"nonfinite movement");
   var collision=new SceneWorld();Check(collision.TrySetPosition(780,900),"wall test origin");
   for(int n=0;n<80;n++)collision.Move(1,0,.033f);
   Check(collision.PlayerX<=790&&collision.IsWalkable(collision.PlayerX,collision.PlayerY),"player circle stops at real fort wall");
   Check(!collision.IsWalkable(810,900),"wall itself blocked");
   Check(collision.TrySetPosition(780,1050),"gateway test origin");for(int n=0;n<20;n++)collision.Move(1,0,.033f);
   Check(collision.PlayerX>830,"gateway passable through wall opening");
   var diagonal=new SceneWorld();float initialX=diagonal.PlayerX,initialY=diagonal.PlayerY;diagonal.Move(1,1,.1f);
   double length=Math.Sqrt(Math.Pow(diagonal.PlayerX-initialX,2)+Math.Pow(diagonal.PlayerY-initialY,2));Check(Math.Abs(length-SceneWorld.MoveSpeed*.1)<.02,"diagonal speed normalized");
   Check(state.Interact().Reason=="NoNearbyNpc","no remote quest interaction");
   Walk(world,world.TestWaypoints[1]);Check(state.Interact().Changed&&state.Session.View().delivery_accepted,"nearby quartermaster accepts task");
   Check(state.Interact().Changed&&state.Session.View().money==125,"nearby delivery executes actual verified modules");
   store.Save("route",state);var loaded=store.Load("route");
   Check(loaded.Session.CharacterId==state.Session.CharacterId&&loaded.Session.View().money==125&&loaded.World.PlayerX==world.PlayerX&&loaded.World.PlayerY==world.PlayerY,"session and exact float position reload together");
   Check(!loaded.Interact().Changed&&loaded.Session.View().money==125,"completed NPC action no repeat reward");
   foreach(PointF point in loaded.World.TestWaypoints)Walk(loaded.World,point);
   Check(loaded.World.IsWalkable(loaded.World.PlayerX,loaded.World.PlayerY)&&Math.Abs(loaded.World.PlayerX-1660)<2&&Math.Abs(loaded.World.PlayerY-560)<2,"whole road reachable with actual movement and collisions");
   Check(loaded.Interact().Changed&&loaded.Session.View().return_accepted,"supply clerk accepts second task");
   Check(loaded.Interact().Changed&&loaded.Session.View().money==165,"supply clerk completes actual reward");
   var stale=store.Load("route");store.Save("route",loaded);
   Reject<SessionConflictException>(delegate{store.Save("route",stale);},"stale scene writer");
   var complete=store.Load("route");Check(complete.Session.View().return_completed&&complete.Session.View().same_object_identity&&complete.Session.View().shared_world_identity,"scene retains verified ownership on reopen");
   Render(complete.World,Path.Combine(directory,"scene-end.png"));
   string path=Path.Combine(directory,"runtime-data","saves","scene-route.save");
   var envelope=(Dictionary<string,object>)StrictJson.Parse(File.ReadAllText(path));var payload=(Dictionary<string,object>)envelope["payload"];
   payload["x_bits"]=BitConverter.ToUInt32(BitConverter.GetBytes(810f),0);payload["y_bits"]=BitConverter.ToUInt32(BitConverter.GetBytes(900f),0);
   envelope["sha256"]=StrictJson.Sha256(StrictJson.Canonical(payload));File.WriteAllText(path,StrictJson.Canonical(envelope),new UTF8Encoding(false));
   Reject<SessionCorruptException>(delegate{store.Load("route");},"checksummed blocked position rejected instead of defaulted");
   var restored=store.Recover("route");Check(restored.Session.View().money==125&&restored.World.IsWalkable(restored.World.PlayerX,restored.World.PlayerY),"explicit recovery restores prior coupled scene/session");
   using(var bitmap=(Bitmap)Image.FromFile(Path.Combine(directory,"scene-start.png"))){
    var colors=new HashSet<int>();for(int y=0;y<bitmap.Height;y+=10)for(int x=0;x<bitmap.Width;x+=10)colors.Add(bitmap.GetPixel(x,y).ToArgb());
    Check(colors.Count>40,"render contains differentiated scene geometry rather than blank window");
   }
   return checks;
  }
 }
}
