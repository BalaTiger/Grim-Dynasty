using System;
using System.IO;
using System.Collections.Generic;
using GrimDynasty.Session;
namespace GrimDynasty.Scene {
 internal static class DeathBridgeTestProgram {
  public static int Main(string[] args){try{
   if(args.Length==2&&(args[0]=="prepare-restart"||args[0]=="verify-restart")){
    int count=args[0]=="prepare-restart"?SessionDeathTests.PrepareRestart(args[1]):SessionDeathTests.VerifyRestart(args[1]);
    Console.WriteLine("M3_DEATH_RESTART "+args[0]+"="+count);return 0;
   }
   if(args.Length!=1)throw new ArgumentException("Fresh package validation root required");
   string root=args[0];
   int core=SessionCoreTests.Run(Path.Combine(root,"core","runtime-data","saves"));
   Console.WriteLine("M2_CORE_ASSERTIONS="+core);
   int death=SessionDeathTests.Run(Path.Combine(root,"death","runtime-data","saves"));
   int scene=SceneDeathTests.Run(Path.Combine(root,"scene-death"));
   int sceneRegression=SceneTests.Run(Path.Combine(root,"scene"));
   int patrolRegression=ScenePatrolTests.Run(Path.Combine(root,"patrol"));
   var old=(Dictionary<string,object>)StrictJson.Parse(File.ReadAllText(Path.Combine(root,"legacy-m2.save")));
   if(StrictJson.Sha256(StrictJson.Canonical(old["payload"]))!=(string)old["sha256"])throw new Exception("Pinned old fixture checksum invalid");
   var legacy=SessionState.FromPayload((Dictionary<string,object>)old["payload"]);
   if(legacy.View().money!=165||!legacy.View().return_completed||legacy.DeathView().Enabled)throw new Exception("Old accepted M2 save did not reconstruct exactly");
   Console.WriteLine("M3_DEATH_BRIDGE SESSION="+death+" SCENE="+scene+" OLD_M2_FIXTURE=1 SCENE_REGRESSION="+sceneRegression+" PATROL_REGRESSION="+patrolRegression);
   return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 }
}
