using System;

public static class GameWaypointValidationProgram {
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new ArgumentException("No arguments");
  GameWaypointProductionIntegrationTests.Run();
  Console.WriteLine("{\"status\":\"passed-shared-waypoint-agent-target-prefix-only\",\"scenarios\":"+GameWaypointProductionIntegrationTests.Scenarios+",\"assertions\":"+GameWaypointProductionIntegrationTests.Assertions+",\"prior_fixture_setup_checks_excluded\":"+GameWaypointProductionIntegrationTests.PriorFixtureSetupChecks+",\"actual_target_submitted\":"+GameWaypointProductionIntegrationTests.ActualTargetSubmitted.ToString().ToLowerInvariant()+",\"repeat_preserved_identity\":"+GameWaypointProductionIntegrationTests.RepeatPreservedIdentity.ToString().ToLowerInvariant()+",\"ignored_floor_zero\":"+GameWaypointProductionIntegrationTests.IgnoredFloorZero.ToString().ToLowerInvariant()+",\"post_submission_prefix\":"+GameWaypointProductionIntegrationTests.PostSubmissionPrefix.ToString().ToLowerInvariant()+",\"begin_waypoint_connected\":"+GameWaypointProductionIntegrationTests.BeginWaypointConnected.ToString().ToLowerInvariant()+",\"next_stop_rva\":\"0x"+GameWaypointProductionIntegrationTests.NextStopRva.ToString("x")+"\",\"old_test_suites_rerun\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"system_library_loaded\":false,\"native_math_calls\":0,\"physical_mouse\":false,\"action_submission_completed\":false,\"walking_accepted\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
