using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskItems;
using GrimDynasty.TokenActions;

namespace GrimDynasty.Offline {
 public static class ReplayCommands {
  const string Fixture = "inventory-cycle";
  const string ItemName = "ming/items/military_ration.dbr";
  static readonly string[] RoundTrip = {
   "start-delivery", "update-delivery", "take-all", "update-delivery",
   "start-return", "update-return", "add-existing", "update-return",
   "add-existing", "take-all", "add-existing"
  };
  sealed class Outcome {
   public string status, reason;
   public bool? changed, native_return;
   public int? taken;
  }

  public static CommandResult Run(string[] args) {
   if(args == null || args.Length != 1)
    return CommandResult.Invalid("replay", "Expected one scenario name or explicit .json scenario path");
   string[] steps;
   if(args[0] == "inventory-roundtrip") steps = RoundTrip;
   else if(args[0] == "inventory-destroy-unsupported") steps = new[] { "destroy-all" };
   else if(args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
    try { steps = ReadSteps(args[0]); }
    catch(ArgumentException e) { return CommandResult.Invalid("replay", e.Message); }
    catch(InvalidDataException e) { return CommandResult.Invalid("replay", e.Message); }
   } else return CommandResult.Invalid("replay", "Unknown scenario: " + args[0]);

   // This creates the existing own QST2 fixture, not an original game object.
   var x = ExampleInventoryCycle.Create();
   var original = x.Base.Item;
   var trace = new List<object>();
   var initial = State(x, original);
   var json = new JavaScriptSerializer();
   for(int i = 0; i < steps.Length; i++) {
    var before = State(x, original);
    var result = Execute(x, steps[i]);
    var after = State(x, original);
    trace.Add(new { index = i, action = steps[i], status = result.status,
     reason = result.reason, changed = result.changed, native_return = result.native_return,
     taken = result.taken, observed_state_unchanged = json.Serialize(before) == json.Serialize(after),
     before = before, after = after });
    if(result.status != "Known")
     return CommandResult.Unsupported("replay", result.reason, new {
      fixture = Fixture, scenario = args[0], steps_completed = i, stopped_at = i,
      initial = initial, trace = trace, final = after, scope = Scope()
     });
   }
   return CommandResult.Known("replay", new {
    fixture = Fixture, scenario = args[0], steps_completed = steps.Length,
    initial = initial, trace = trace, final = State(x, original), scope = Scope()
   });
  }

  static string[] ReadSteps(string path) {
   path=CodecCommands.PathFor(path,".json");
   if(new FileInfo(path).Length > 65536) throw new InvalidDataException("Replay JSON exceeds 64 KiB");
   string text = File.ReadAllText(path, new UTF8Encoding(false, true));
   if(text.Length > 65536) throw new InvalidDataException("Replay JSON exceeds input limit");
   var json = new JavaScriptSerializer { MaxJsonLength = 65536, RecursionLimit = 8 };
   var root = json.DeserializeObject(text) as Dictionary<string, object>;
   object schema, fixture, raw;
   if(root == null || root.Count != 3 || !root.TryGetValue("schema", out schema)
      || !root.TryGetValue("fixture", out fixture) || !root.TryGetValue("steps", out raw)
      || !(schema is string) || (string)schema != "grim-dynasty-replay-v1"
      || !(fixture is string) || (string)fixture != Fixture)
    throw new InvalidDataException("Expected only schema=grim-dynasty-replay-v1, fixture=inventory-cycle, steps");
   var list = raw as IList;
   if(list == null || list.Count < 1 || list.Count > 64)
    throw new InvalidDataException("steps must be an array containing 1 to 64 action strings");
   var result = new string[list.Count];
   for(int i = 0; i < list.Count; i++) {
    var action = list[i] as string;
    if(String.IsNullOrWhiteSpace(action) || action.Length > 80)
     throw new InvalidDataException("Every step must be a nonempty action string of at most 80 characters");
    result[i] = action;
   }
   return result;
  }

  static Outcome Execute(ExampleInventoryCycle x, string action) {
   int taken;
   switch(action) {
    case "start-delivery": return From(AutomaticTasks.Start(x.Tasks[0]));
    case "update-delivery": return From(AutomaticTasks.Update(x.Tasks[0]));
    case "start-return": return From(AutomaticTasks.Start(x.Tasks[1]));
    case "update-return": return From(AutomaticTasks.Update(x.Tasks[1]));
    case "take-all": {
     var result = From(InventoryTake.ExecuteInventoryByName(x.Base.Inventory, ItemName, 3, out taken));
     result.taken = taken; return result;
    }
    case "add-existing": return From(InventoryAdd.Execute(x.Add, 100, false));
    case "destroy-all": {
     // Per-call requested policy; the real module rejects it before removal.
     bool? saved = x.Base.Inventory.DestroyAfterFullTake;
     try {
      x.Base.Inventory.DestroyAfterFullTake = true;
      var result = From(InventoryTake.Execute(x.Base.Inventory, 100, -1, out taken));
      result.taken = taken; return result;
     } finally { x.Base.Inventory.DestroyAfterFullTake = saved; }
    }
    default: return new Outcome { status = "Unsupported", reason = "ReplayActionNotSupported:" + action };
   }
  }

  static Outcome From(MutationResult r) {
   return new Outcome { status = r.Status, reason = r.Reason, changed = r.Changed };
  }
  static Outcome From(AutoResult r) {
   return new Outcome { status = r.Status, reason = r.Reason, native_return = r.NativeReturn };
  }

  static object State(ExampleInventoryCycle x, ItemObject original) {
   ItemObject retained;
   bool present = x.Base.Quest.Stock.Objects.TryGetValue(100, out retained);
   var taskStates = new int[2]; var objectives = new int[2];
   var dirty = new byte[2]; var attached16 = new bool[2]; var attached17 = new bool[2];
   var rewardBanks = new bool[2][]; var completionBanks = new bool[2][];
   for(int i = 0; i < 2; i++) {
    var task = x.Tasks[i];
    taskStates[i] = task.Runtime.State.Task.State;
    objectives[i] = task.Runtime.ObjectiveCounters[task.Runtime.State.Task.Objectives[0][0]];
    dirty[i] = x.Conditions[i].ChangedByte14;
    attached16[i] = x.Conditions[i].Owner16Attached;
    attached17[i] = x.Conditions[i].Owner17Attached;
    rewardBanks[i] = (bool[])task.RewardRecordedA3.Clone();
    completionBanks[i] = (bool[])task.Runtime.CompletionRecorded.Clone();
   }
   return new {
    money = x.Base.Wallet.Money16C4,
    inventory_ids = x.Base.Quest.Stock.InventoryItemIds.ToArray(),
    object_100_retained = present, same_object_identity = present && Object.ReferenceEquals(original, retained),
    stack = original.RawStack88C, replica_stack = original.RawReplicaStack6B0,
    gender = original.RawGender874, engine_add_count = x.Add.Engine.ItemAddCount1C,
    reward_task_uids = x.Base.Record.RewardTaskUidsAt50.ToArray(),
    task_states = taskStates, objective_states = objectives, condition_dirty = dirty,
    attached_16 = attached16, attached_17 = attached17,
    reward_recorded = rewardBanks, completion_recorded = completionBanks,
    detached = x.Base.Inventory.DetachedObjects.Contains(100),
    add_events = x.Add.Produced.Count, take_events = x.Base.Inventory.Produced.Count,
    add_last_delivered = x.Add.LastDelivered, take_last_delivered = x.Base.Inventory.LastDelivered,
    add_trace = x.Add.Trace.ToArray(), removal_trace = x.Base.Inventory.RemovalTrace.ToArray()
   };
  }

  static string Scope() {
   return "Own inventory-cycle QST2 fixture; two tasks, existing base item 100, inventory events and money rewards. "
    + "No native object factory, XP/CRT provider, save format, game runtime or formal-sample compatibility claim.";
  }
 }
}
