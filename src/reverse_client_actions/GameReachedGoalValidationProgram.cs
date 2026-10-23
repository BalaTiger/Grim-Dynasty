using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

// Only the four new component suites. No previous executable entry point,
// saved-map fixture, native engine, UCRT adapter or physical input is invoked.
public static class GameReachedGoalValidationProgram {
 public static int Main(string[] args){
  var json=new JavaScriptSerializer();
  try{
   if(args.Length!=0)throw new ArgumentException("This fixed new component test program takes no arguments");
   var suites=new List<object>();int scenarios=0,assertions=0;
   GameMovementIdentityBridgeTests.Run();scenarios+=GameMovementIdentityBridgeTests.Scenarios;assertions+=GameMovementIdentityBridgeTests.Assertions;
   suites.Add(new{name="identity-bridge",scenarios=GameMovementIdentityBridgeTests.Scenarios,assertions=GameMovementIdentityBridgeTests.Assertions});
   GameReachedGoalEngineTests.Run();scenarios+=GameReachedGoalEngineTests.Scenarios;assertions+=GameReachedGoalEngineTests.Assertions;
   suites.Add(new{name="engine-hot-helpers",scenarios=GameReachedGoalEngineTests.Scenarios,assertions=GameReachedGoalEngineTests.Assertions});
   GameObjectManagerLookupTests.Run();scenarios+=GameObjectManagerLookupTests.Scenarios;assertions+=GameObjectManagerLookupTests.Assertions;
   suites.Add(new{name="object-manager-own-lock-lookup",scenarios=GameObjectManagerLookupTests.Scenarios,assertions=GameObjectManagerLookupTests.Assertions});
   GameReachedGoalTests.Run();scenarios+=GameReachedGoalTests.Scenarios;assertions+=GameReachedGoalTests.Assertions;
   suites.Add(new{name="conditional-arrival-and-active-end-stop",scenarios=GameReachedGoalTests.Scenarios,assertions=GameReachedGoalTests.Assertions});
   if(scenarios!=23)throw new Exception("Unexpected new component scenario count");
   Console.WriteLine(json.Serialize(new{status="passed-new-reached-goal-conditional-hot-components-only",scenarios,assertions,suites,
    conditional_empty_handler_hot_domain=true,active_move_end_completed=false,active_action_destroyed=false,native_critical_wrappers_recovered=false,
    platform_sync="explicit OWN System.Threading.Monitor",full_tick=false,physical_mouse=false,old_test_suites_rerun=0,
    native_math_calls=0,system_library_loaded=false,original_read=false,original_executed=false,backend_started=false}));return 0;
  }catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}
 }
}
