using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
namespace GrimDynasty.Scene {
 public static class SceneProgram {
  [STAThread] public static int Main(string[] args){
   try{
    string package=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    if(args.Length==2&&args[0]=="--test-patrol"){
     int old=SceneTests.Run(Path.Combine(args[1],"scene-regression"));
     int agents=SceneAgentsTests.Run();int integrated=ScenePatrolTests.Run(Path.Combine(args[1],"patrol"));
     Console.WriteLine("M3_PATROL_CHECKS="+agents+" INTEGRATION="+integrated+" SCENE_REGRESSION="+old);
     return NativeDamageSettlementTests.Main();
    }
    if(args.Length==2&&args[0]=="--test"){int count=SceneTests.Run(args[1]);Console.WriteLine("M3_SCENE_CHECKS="+count);return 0;}
    if(args.Length==2&&args[0]=="--render"){
     using(var bitmap=new Bitmap(1100,760))using(var g=Graphics.FromImage(bitmap)){SceneRenderer.Draw(g,new SceneWorld(),bitmap.Width,bitmap.Height);bitmap.Save(args[1],ImageFormat.Png);}return 0;
    }
    bool smoke=args.Length==2&&args[0]=="--smoke";
    string root=smoke?Path.Combine(Path.GetFullPath(args[1]),"runtime-data","saves"):Path.Combine(package,"runtime-data","saves");
    var store=new SceneStore(root);
    if(args.Length==2&&args[0]=="--recover"){store.Recover(args[1]);return 0;}
    if(!smoke&&(args.Length>2||(args.Length>0&&args[0].StartsWith("--",StringComparison.Ordinal))))throw new ArgumentException("gd-scene.exe [slot] [name]");
    string slot=smoke?"smoke":args.Length>0?args[0]:"player";
    SceneState state;if(store.Exists(slot))state=store.Load(slot);else {state=SceneState.Create(smoke?"Scene Test":args.Length==2?args[1]:"边军新卒");store.Save(slot,state);}
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    using(var window=new SceneWindow(store,slot,state,smoke,smoke?args[1]:null)){
     Application.Run(window);if(smoke&&!window.SmokeSaveSucceeded)throw new IOException("Scene smoke closing save failed");
    }
    if(smoke){
     var reopened=store.Load(slot);var view=reopened.Session.View();
     File.WriteAllText(Path.Combine(args[1],"window-reopen.json"),GrimDynasty.Session.StrictJson.Canonical(new System.Collections.Generic.Dictionary<string,object>{
      {"x_bits",BitConverter.ToUInt32(BitConverter.GetBytes(reopened.World.PlayerX),0)},
      {"y_bits",BitConverter.ToUInt32(BitConverter.GetBytes(reopened.World.PlayerY),0)},
      {"money",view.money},{"character_id",view.character_id},{"revision",view.revision},
      {"journal_count",view.journal_count},{"same_object_identity",view.same_object_identity},{"shared_world_identity",view.shared_world_identity}}));
    }
    return 0;
   }catch(Exception e){
    Console.Error.WriteLine(e.ToString());
    bool automated=args.Length>0&&(args[0]=="--test"||args[0]=="--test-patrol"||args[0]=="--render"||args[0]=="--smoke");
    if(!automated)MessageBox.Show("无法打开场景："+e.Message,"辽西驿堡",MessageBoxButtons.OK,MessageBoxIcon.Error);
    return 1;
   }
  }
 }
}
