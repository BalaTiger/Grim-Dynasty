using System;

public static class GameMoveBeginValidationProgram {
 public static int Main(string[] args){try{
  if(args.Length!=1||(args[0]!="associated"&&args[0]!="begin"))throw new ArgumentException("Expected associated or begin");
  int scenarios,assertions,newScenarios,affected=0,fixtureChecks=0;string detail="";
  if(args[0]=="associated"){GameAssociatedLookupTests.Run();scenarios=GameAssociatedLookupTests.Scenarios;assertions=GameAssociatedLookupTests.Assertions;}
  else{
   GameControllerMoveBeginIntegrationTests.Run();scenarios=GameControllerMoveBeginIntegrationTests.Scenarios;assertions=GameControllerMoveBeginIntegrationTests.Assertions;affected=GameControllerMoveBeginIntegrationTests.AffectedRegressionScenarios;fixtureChecks=GameControllerMoveBeginIntegrationTests.PriorFixtureSetupChecks;
   detail=",\"gate_crossed\":"+GameControllerMoveBeginIntegrationTests.GateCrossed.ToString().ToLowerInvariant()+",\"associated_miss_returned\":"+GameControllerMoveBeginIntegrationTests.AssociatedMissReturned.ToString().ToLowerInvariant()+",\"distance_returned\":"+GameControllerMoveBeginIntegrationTests.DistanceReturned.ToString().ToLowerInvariant()+",\"skill_returned\":"+GameControllerMoveBeginIntegrationTests.SkillReturned.ToString().ToLowerInvariant()+",\"already_there_returned\":"+GameControllerMoveBeginIntegrationTests.AlreadyThereReturned.ToString().ToLowerInvariant()+",\"saved_corridor_count\":"+GameControllerMoveBeginIntegrationTests.SavedCorridorCount+",\"next_stop_rva\":\"0x"+GameControllerMoveBeginIntegrationTests.NextStopRva.ToString("x")+"\"";
  }
  newScenarios=scenarios-affected;
  Console.WriteLine("{\"status\":\"passed-associated-and-begin-prefix-only\",\"mode\":\""+args[0]+"\",\"scenarios\":"+scenarios+",\"new_scenarios\":"+newScenarios+",\"affected_regression_scenarios\":"+affected+",\"assertions\":"+assertions+",\"assertion_count_scope\":\"Combined current suite; Begin includes two affected regressions. Old fixture setup checks are excluded.\",\"prior_fixture_setup_checks_excluded\":"+fixtureChecks+",\"old_test_suites_rerun\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"system_library_loaded\":false,\"native_math_calls\":0,\"physical_mouse\":false,\"action_submission_completed\":false,\"walking_accepted\":false,\"gameplay_accepted\":false"+detail+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
