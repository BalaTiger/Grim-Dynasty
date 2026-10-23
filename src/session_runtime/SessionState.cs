using System;
using System.Collections.Generic;
using System.IO;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskItems;
using GrimDynasty.TokenActions;

namespace GrimDynasty.Session {
 public sealed class SessionUnsupportedException:Exception {public SessionUnsupportedException(string reason):base(reason){}}
 public sealed class SessionCorruptException:IOException {public SessionCorruptException(string reason):base(reason){}}
 public sealed class SessionConflictException:IOException {public SessionConflictException(string reason):base(reason){}}
 public sealed class SessionResult {
  public string Status,Reason;public bool Changed;public SessionView View;
 }
 public sealed class SessionView {
  public string character_id,name;public long revision;public int journal_count;
  public uint? money;public uint[] inventory_ids,reward_task_uids;public int[] task_states;
  public bool delivery_accepted,delivery_completed,return_accepted,return_completed;
  public bool same_object_identity,shared_world_identity;public uint? stack,replica_stack;
  public uint? engine_add_count;public int add_events,take_events;public bool[] attached_16,attached_17;
 }
 // Own session policy around the previously evidenced fixture. Native task Start is
 // not a one-shot quest API, so this adapter explicitly prevents completed restarts.
 public sealed partial class SessionState {
  public const int SchemaVersion=1;
  public const string ContentVersion="liaoxi-m2-inventory-v1";
  public const int MaxCommands=4096;
  const string ItemName="ming/items/military_ration.dbr";
  ExampleInventoryCycle model;
  readonly List<object> journal=new List<object>();
  public string CharacterId {get;private set;}
  public string CharacterName {get;private set;}
  public long StoreRevision {get;internal set;}
  internal string StoreFingerprint;
  SessionState(string id,string name){CharacterId=id;CharacterName=name;model=ExampleInventoryCycle.Create();}
  public static SessionState Create(string name){ValidateName(name);return new SessionState(Guid.NewGuid().ToString("N"),name);}
  static void ValidateName(string name){
   if(String.IsNullOrWhiteSpace(name)||name.Length>48||name!=name.Trim())throw new ArgumentException("Character name requires 1..48 characters without leading/trailing whitespace");
   foreach(char c in name)if(Char.IsControl(c)||Char.IsSurrogate(c))throw new ArgumentException("Character name contains unsupported control or surrogate characters");
  }
  public SessionResult Execute(string action){
   if(String.IsNullOrWhiteSpace(action))throw new ArgumentException("Action required");
   if(journal.Count>=MaxCommands)return Result("Unsupported","SessionJournalLimit",false);
   // Candidate reconstruction retains the live state's ownership graph on failure.
   var candidate=Rebuild(CharacterId,CharacterName,journal,deathSchema);
   var result=candidate.Apply(action);
   if(result.Status=="Known"&&result.Changed){
    candidate.journal.Add(deathSchema?MakeM2Command(action):(object)action);CommitCandidate(candidate);
   }
   return Result(result.Status,result.Reason,result.Changed);
  }
  SessionResult Result(string status,string reason,bool changed){return new SessionResult{Status=status,Reason=reason,Changed=changed,View=View()};}
  SessionResult Apply(string action){
   int index;
   switch(action){
    case "accept-delivery":index=0;break;
    case "deliver":index=0;break;
    case "accept-return":index=1;break;
    case "return-item":index=1;break;
    default:return Result("Unsupported","SessionActionNotSupported:"+action,false);
   }
   int state=model.Tasks[index].Runtime.State.Task.State;
   if(state==3)return Result("Known","AlreadyCompleted",false);
   bool accepting=action=="accept-delivery"||action=="accept-return";
   if(accepting&&state==2)return Result("Known","AlreadyAccepted",false);
   if(index==1&&model.Tasks[0].Runtime.State.Task.State!=3)return Result("Unsupported","DeliveryMustCompleteFirst",false);
   if(accepting){
    if(state!=1)return Result("Unsupported","TaskOutsideUnacceptedDomain",false);
    var start=AutomaticTasks.Start(model.Tasks[index]);if(start.Status!="Known")return Result(start.Status,start.Reason,false);
   } else {
    if(state!=2)return Result("Unsupported","TaskMustBeAcceptedFirst",false);
    if(index==0){
     int taken;var take=InventoryTake.ExecuteInventoryByName(model.Base.Inventory,ItemName,3,out taken);
     if(take.Status!="Known")return Result(take.Status,take.Reason,false);
     if(taken!=3)return Result("Unsupported","DeliveryRequiresThreeRations",false);
    } else {
     var add=InventoryAdd.Execute(model.Add,100,false);if(add.Status!="Known")return Result(add.Status,add.Reason,false);
    }
   }
   var update=AutomaticTasks.Update(model.Tasks[index]);
   if(update.Status!="Known")return Result(update.Status,update.Reason,false);
   if(!accepting&&model.Tasks[index].Runtime.State.Task.State!=3)return Result("Unsupported","ExpectedTaskCompletionNotReached",false);
   return Result("Known",null,true);
  }
  static SessionState Rebuild(string id,string name,IEnumerable<object> commands,bool bridgeSchema){
   var state=new SessionState(id,name);state.deathSchema=bridgeSchema;
   foreach(object input in commands){
    object command=StrictJson.Parse(StrictJson.Canonical(input));
    var result=state.ApplyJournalCommand(command);
    if(result.Status!="Known"){
     if(bridgeSchema)throw new SessionCorruptException("JournalReplay:"+result.Reason);
     throw new SessionUnsupportedException("JournalReplay:"+result.Reason);
    }
    if(!result.Changed)throw new SessionCorruptException("Journal contains a duplicate or non-changing command");
    state.journal.Add(command);
   }
   return state;
  }
  public SessionView View(){
   var x=model;ItemObject retained;
   bool same=x.Base.Quest.Stock.Objects.TryGetValue(100,out retained)&&Object.ReferenceEquals(retained,x.Base.Item);
   bool shared=Object.ReferenceEquals(x.Add.Stock,x.Base.Quest.Stock)&&Object.ReferenceEquals(x.Base.Inventory.Stock,x.Base.Quest.Stock)
    &&Object.ReferenceEquals(x.Add.SharedRemovalContext,x.Base.Inventory)&&Object.ReferenceEquals(x.Tasks[0].Runtime.World,x.Tasks[1].Runtime.World)
    &&Object.ReferenceEquals(x.Tasks[0].Events,x.Tasks[1].Events)&&Object.ReferenceEquals(x.Tasks[0].Runtime.State.Task,x.Base.Record.Quest.Tasks[0])
    &&Object.ReferenceEquals(x.Tasks[1].Runtime.State.Task,x.Base.Record.Quest.Tasks[1]);
   int a=x.Tasks[0].Runtime.State.Task.State,b=x.Tasks[1].Runtime.State.Task.State;
   return new SessionView{character_id=CharacterId,name=CharacterName,revision=StoreRevision,journal_count=journal.Count,money=x.Base.Wallet.Money16C4,
    inventory_ids=x.Base.Quest.Stock.InventoryItemIds.ToArray(),reward_task_uids=x.Base.Record.RewardTaskUidsAt50.ToArray(),task_states=new[]{a,b},
    delivery_accepted=a>=2,delivery_completed=a==3,return_accepted=b>=2,return_completed=b==3,same_object_identity=same,shared_world_identity=shared,
    stack=x.Base.Item.RawStack88C,replica_stack=x.Base.Item.RawReplicaStack6B0,engine_add_count=x.Add.Engine.ItemAddCount1C,
    add_events=x.Add.Produced.Count,take_events=x.Base.Inventory.Produced.Count,
    attached_16=new[]{x.Conditions[0].Owner16Attached,x.Conditions[1].Owner16Attached},attached_17=new[]{x.Conditions[0].Owner17Attached,x.Conditions[1].Owner17Attached}};
  }
  internal Dictionary<string,object> Payload(long revision){
   return new Dictionary<string,object>{{"schema",deathSchema?DeathSchemaVersion:SchemaVersion},{"content",deathSchema?DeathContentVersion:ContentVersion},{"commit_id",Guid.NewGuid().ToString("N")},{"character_id",CharacterId},{"character_name",CharacterName},
    {"revision",revision},{"commands",journal.ToArray()},{"state_sha256",StateHash()}};
  }
  string LegacyStateHash(){
   var v=View();var goals=new List<object>();
   for(int i=0;i<2;i++){
    var t=model.Tasks[i];goals.Add(new Dictionary<string,object>{{"objective",t.Runtime.ObjectiveCounters[t.Runtime.State.Task.Objectives[0][0]]},
     {"reward",t.RewardRecordedA3},{"completion",t.Runtime.CompletionRecorded},{"states",t.Runtime.State.StatesByDifficulty},
     {"dirty",model.Conditions[i].ChangedByte14},{"outbound_task",t.Runtime.Outbound.LastTaskUid},{"outbound_quest",t.Runtime.Outbound.LastQuestReference}});
   }
   return StrictJson.Sha256(StrictJson.Canonical(new Dictionary<string,object>{{"character_id",v.character_id},{"name",v.name},{"journal_count",v.journal_count},
    {"money",v.money},{"inventory_ids",v.inventory_ids},{"reward_task_uids",v.reward_task_uids},{"task_states",v.task_states},
    {"same_object_identity",v.same_object_identity},{"shared_world_identity",v.shared_world_identity},{"stack",v.stack},{"replica_stack",v.replica_stack},
    {"gender",model.Base.Item.RawGender874},{"engine_add_count",v.engine_add_count},{"add_events",v.add_events},{"take_events",v.take_events},
    {"attached_16",v.attached_16},{"attached_17",v.attached_17},{"goals",goals},{"detached",model.Base.Inventory.DetachedObjects.Contains(100)},
    {"add_trace",model.Add.Trace.ToArray()},{"take_trace",model.Base.Inventory.RemovalTrace.ToArray()}}));
  }
  internal static SessionState FromPayload(Dictionary<string,object> p){
   object schema,content,commit,id,name,revision,commands,hash;
   if(!p.TryGetValue("schema",out schema)||!(schema is long))throw new SessionCorruptException("Save schema missing or noninteger");
   if((long)schema!=SchemaVersion&&(long)schema!=DeathSchemaVersion)throw new SessionUnsupportedException("UnknownSessionSchema:"+schema);
   bool bridgeSchema=(long)schema==DeathSchemaVersion;
   if(!p.TryGetValue("content",out content)||!(content is string))throw new SessionCorruptException("Save content missing");
   if((string)content!=(bridgeSchema?DeathContentVersion:ContentVersion))throw new SessionUnsupportedException("UnknownSessionContent:"+content);
   if(p.Count!=8||!p.TryGetValue("commit_id",out commit)||!p.TryGetValue("character_id",out id)||!p.TryGetValue("character_name",out name)||!p.TryGetValue("revision",out revision)
    ||!p.TryGetValue("commands",out commands)||!p.TryGetValue("state_sha256",out hash))throw new SessionCorruptException("Save payload fields mismatch");
   Guid parsed,commitGuid;
   if(!(commit is string)||!Guid.TryParseExact((string)commit,"N",out commitGuid)||commitGuid==Guid.Empty||!(id is string)||!Guid.TryParseExact((string)id,"N",out parsed)||parsed==Guid.Empty||!(name is string)||!(revision is long)||(long)revision<1
    ||!(hash is string)||((string)hash).Length!=64)throw new SessionCorruptException("Save identity/revision/hash invalid");
   try{ValidateName((string)name);}catch(ArgumentException ex){throw new SessionCorruptException(ex.Message);}
   var list=commands as System.Collections.IList;
   if(list==null||list.Count>MaxCommands)throw new SessionCorruptException("Save journal invalid or too large");
   var actions=new List<object>();foreach(object command in list){if(!bridgeSchema&&(!(command is string)||((string)command).Length>80))throw new SessionCorruptException("Save journal action invalid");actions.Add(command);}
   var state=Rebuild((string)id,(string)name,actions,bridgeSchema);state.StoreRevision=(long)revision;
   if(bridgeSchema&&state.deathObjective==null)throw new SessionCorruptException("Typed session lacks explicit objective activation");
   if(state.StateHash()!=(string)hash)throw new SessionCorruptException("Save state does not match deterministic journal replay");
   return state;
  }
 }
}
