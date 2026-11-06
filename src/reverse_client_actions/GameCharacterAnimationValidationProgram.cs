using System;

public static class GameCharacterAnimationValidationProgram {
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new ArgumentException("No arguments");
  GameCharacterAnimationTests.Run();
  Console.WriteLine("{\"status\":\"passed-character-animation-getter-components-only\",\"scenarios\":"+GameCharacterAnimationTests.Scenarios+",\"assertions\":"+GameCharacterAnimationTests.Assertions+",\"old_test_suites_rerun\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"animation_resources_loaded\":false,\"native_play_restored\":false,\"production_animation_connected\":false,\"physical_mouse\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
