using System;

public static class EngineAnimationSpeedValidationProgram {
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new ArgumentException("No arguments");
  EngineAnimationSpeedTests.Run();
  Console.WriteLine("{\"status\":\"passed-engine-animation-speed-leaf-only\",\"scenarios\":"+EngineAnimationSpeedTests.Scenarios+",\"assertions\":"+EngineAnimationSpeedTests.Assertions+",\"old_test_suites_rerun\":0,\"index_zero_storage_only\":true,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"animation_resources_loaded\":false,\"native_play_restored\":false,\"production_animation_connected\":false,\"physical_mouse\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
