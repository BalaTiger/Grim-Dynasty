using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GrimDynasty.Rewards;

namespace GrimDynasty.Session {
 // All files belong to the caller's fresh own runtime-data/saves directory.
 // These entry points do not launch processes, native code, combat or a GUI.
 public static class SessionDeathTests {
  static int checks;
  const string RestartSlot="death-restart";
  const string RestartMarker="death-restart.expect.json";
  static void Check(bool value,string label){if(!value)throw new Exception("Session death test failed: "+label);checks++;}
  static void Throws<T>(Action action,string label) where T:Exception {
   try{action();}catch(T){checks++;return;}throw new Exception("Session death test did not reject: "+label);
  }
  static string Text(string path){return File.ReadAllText(path,new UTF8Encoding(false,true));}
  static string FileHash(string path){return StrictJson.Sha256(Text(path));}
  static Dictionary<string,object> ReadPayload(string path){
   return (Dictionary<string,object>)((Dictionary<string,object>)StrictJson.Parse(Text(path)))["payload"];
  }
  static string Snapshot(SessionState state){
   var payload=state.Payload(1);payload.Remove("commit_id");
   return StrictJson.Canonical(new Dictionary<string,object>{{"payload",payload},{"revision",state.StoreRevision},{"fingerprint",state.StoreFingerprint}});
  }
  static SessionStore FreshStore(string root){
   var store=new SessionStore(root);
   if(Directory.GetFiles(store.RootDirectory).Length!=0||Directory.GetDirectories(store.RootDirectory).Length!=0)
    throw new ArgumentException("Session death tests require a fresh own save root");
   return store;
  }
  static void AlterPayload(string source,string target,Action<Dictionary<string,object>> alter){
   var envelope=(Dictionary<string,object>)StrictJson.Parse(Text(source));var payload=(Dictionary<string,object>)envelope["payload"];
   alter(payload);envelope["sha256"]=StrictJson.Sha256(StrictJson.Canonical(payload));
   File.WriteAllText(target,StrictJson.Canonical(envelope),new UTF8Encoding(false));
  }
  static Dictionary<string,object> Command(Dictionary<string,object> payload,string kind){
   foreach(object item in (IList)payload["commands"]){
    var command=item as Dictionary<string,object>;object value;
    if(command!=null&&command.TryGetValue("kind",out value)&&Object.Equals(value,kind))return command;
   }
   throw new Exception("Missing expected typed command: "+kind);
  }
  static void TypedJournal(string path){
   var payload=ReadPayload(path);
   Check((long)payload["schema"]==2L&&(string)payload["content"]=="liaoxi-m3-death-bridge-v1","bridge save has explicit schema/content");
   foreach(object command in (IList)payload["commands"])Check(command is Dictionary<string,object>,"schema 2 contains typed command objects");
  }
  static void M2Identity(SessionState state,string label){
   var view=state.View();Check(view.same_object_identity&&view.shared_world_identity,label+" retains M2 owned object graph");
  }
  static void Progress(SessionState state,uint count,uint required,bool complete,int actions,uint money,string label){
   var view=state.DeathView();
   Check(view.Enabled&&view.Count==count&&view.Required==required&&view.AcceptedDeaths==(int)count,label+" exact accepted progress");
   Check(view.Completed==complete&&view.RewardRecorded==complete&&view.RewardActionsApplied==actions,label+" one-shot reward history");
   Check(view.CallbackAttached==!complete&&view.UsesSessionWallet,label+" callback and wallet ownership");
   Check(state.View().money==money,label+" session wallet value");M2Identity(state,label);
  }
  static SessionDeathObservation Change(SessionDeathObservation source,string field,object value){
   return new SessionDeathObservation(field=="character_id"?(string)value:source.CharacterId,field=="encounter_id"?(string)value:source.EncounterId,
    field=="profile_id"?(string)value:source.ProfileId,field=="profile_sha256"?(string)value:source.ProfileHash,field=="origin"?(string)value:source.Origin,
    field=="source_id"?(uint)value:source.SourceId,field=="victim_id"?(uint)value:source.VictimId,field=="generation"?(uint)value:source.Generation,
    field=="record"?(string)value:source.RecordName);
  }
  static void Rejected(SessionState state,SessionDeathObservation observation,string label){
   string before=Snapshot(state);var result=state.PrepareExternalDeath(observation);
   Check(result.Status=="Unsupported"&&!result.Changed&&result.Candidate==null,label+" rejected without candidate");
   Check(Snapshot(state)==before,label+" preserves all live state");
  }
  static void Unchanged(SessionState state,SessionDeathObservation observation,string reason,string label){
   string before=Snapshot(state);var result=state.PrepareExternalDeath(observation);
   Check(result.Status=="Known"&&!result.Changed&&result.Candidate==null&&result.Reason==reason,label+" explicit known no-op");
   Check(Snapshot(state)==before,label+" preserves all live state");
  }
  static SessionDeathPreparation Prepared(SessionState state,uint victim,string label){
   string before=Snapshot(state);var result=state.PrepareExternalDeath(SessionDeathFixtures.Observation(state,victim));
   Check(result.Status=="Known"&&result.Changed&&result.Candidate!=null,label+" creates changed candidate");
   Check(!Object.ReferenceEquals(state,result.Candidate)&&Snapshot(state)==before,label+" leaves live state untouched");
   Check(result.Candidate.StoreRevision==state.StoreRevision&&result.Candidate.StoreFingerprint==state.StoreFingerprint,label+" retains commit identity");
   Check(result.ValidateSource(state),label+" validates unchanged source and candidate");
   return result;
  }
  static void Begin(SessionState state,string profile,string encounter){
   var result=state.BeginDeathObjective(profile,encounter);
   Check(result.Status=="Known"&&result.Changed,"begin explicit fixture objective");
   var view=state.DeathView();
   Check(view.Enabled&&view.ProfileId==profile&&view.EncounterId==encounter&&!String.IsNullOrEmpty(view.ObjectiveId)
    &&view.ProfileHash!=null&&view.ProfileHash.Length==64,"begin exposes pinned profile identity");
  }
  static void RejectedBegin(SessionState state,string profile,string encounter,string label){
   string before=Snapshot(state);
   try{var result=state.BeginDeathObjective(profile,encounter);Check(result.Status=="Unsupported"&&!result.Changed,label+" explicitly rejected");}
   catch(ArgumentException){checks++;}catch(SessionUnsupportedException){checks++;}
   Check(Snapshot(state)==before,label+" preserves live session");
  }
  static void Changed(SessionState state,string action){var result=state.Execute(action);Check(result.Status=="Known"&&result.Changed,"M2 action "+action);}
  static SessionState FinishM2(SessionStore store,string slot,SessionState state){
   if(!state.View().delivery_accepted){Changed(state,"accept-delivery");store.Save(slot,state);}
   if(!state.View().delivery_completed){Changed(state,"deliver");store.Save(slot,state);}
   Check(state.View().money==150&&state.View().inventory_ids.Length==0,"death reward survives ration delivery");
   if(!state.View().return_accepted){Changed(state,"accept-return");store.Save(slot,state);}
   if(!state.View().return_completed){Changed(state,"return-item");store.Save(slot,state);}
   state=store.Load(slot);Progress(state,2,2,true,1,190,"death plus full M2 lifecycle after reload");
   var view=state.View();
   Check(view.delivery_completed&&view.return_completed&&view.reward_task_uids.Length==2&&view.reward_task_uids[0]==1&&view.reward_task_uids[1]==2,"M2 reward history remains separate and complete");
   Check(view.inventory_ids.Length==1&&view.inventory_ids[0]==100,"M2 returns its retained ration object");
   string before=Snapshot(state);
   Check(!state.Execute("deliver").Changed&&!state.Execute("return-item").Changed&&Snapshot(state)==before,"M2 completed actions cannot repeat bridge or M2 rewards");
   return state;
  }
  static void BadSave<T>(SessionStore store,string source,string slot,Action<Dictionary<string,object>> alter) where T:Exception {
   string before=FileHash(source);AlterPayload(source,Path.Combine(store.RootDirectory,slot+".save"),alter);
   Throws<T>(delegate{store.Load(slot);},slot);Check(FileHash(source)==before,slot+" leaves valid source save intact");
  }
  static void Admission(SessionState state){
   var valid=SessionDeathFixtures.Observation(state,200);Rejected(state,null,"missing observation");
   Rejected(state,Change(valid,"character_id",SessionState.Create("Other owner").CharacterId),"another character");
   Rejected(state,Change(valid,"character_id",null),"unknown character");
   Rejected(state,Change(valid,"encounter_id","other-encounter"),"another encounter");
   Rejected(state,Change(valid,"profile_id",SessionDeathFixtures.NoRewardProfile),"another known profile");
   Rejected(state,Change(valid,"profile_id","future-profile"),"unknown profile");
   Rejected(state,Change(valid,"profile_sha256",new string('0',64)),"wrong profile hash");
   Rejected(state,Change(valid,"source_id",2u),"known player is not this owner");
   Rejected(state,Change(valid,"source_id",0u),"zero source");
   Rejected(state,Change(valid,"source_id",UInt32.MaxValue),"unknown source");
   Rejected(state,Change(valid,"victim_id",0u),"zero victim");
   Rejected(state,Change(valid,"victim_id",203u),"victim outside roster");
   Rejected(state,Change(valid,"generation",0u),"unknown zero generation");
   Rejected(state,Change(valid,"generation",2u),"unasserted generation");
   Rejected(state,Change(valid,"record","own/fixtures/other-target.dbr"),"conflicting record");
   Rejected(state,Change(valid,"record",null),"unknown record");
   Rejected(state,Change(valid,"record",SessionDeathFixtures.RecordName.ToUpperInvariant()),"record comparison stays exact");
   Rejected(state,Change(valid,"origin",null),"unknown producer");
   Rejected(state,Change(valid,"origin","externalfixture"),"producer marker comparison stays exact");
   string before=Snapshot(state);var producer=state.PrepareExternalDeath(Change(valid,"origin","NativeCombat"));
   Check(producer.Status=="Unsupported"&&!producer.Changed&&producer.Candidate==null&&producer.Reason=="CombatDamageProducerNotIntegrated","native damage/death producer remains explicitly unsupported");
   Check(Snapshot(state)==before,"unsupported producer leaves state unchanged");
  }
  static void PreparationOwnership(){
   var source=SessionState.Create("Preparation Guard");Begin(source,SessionDeathFixtures.MoneyProfile,"preparation-owner");
   var preparation=Prepared(source,200,"ownership preparation");var equivalent=source.CopyForCommit();
   Check(Snapshot(equivalent)==Snapshot(source)&&!Object.ReferenceEquals(equivalent,source),"copy has same content and distinct session identity");
   Check(!preparation.ValidateSource(equivalent)&&!preparation.ValidateSource(null),"preparation rejects substituted or missing source");
   long revision=source.StoreRevision;string fingerprint=source.StoreFingerprint;Changed(source,"accept-delivery");
   Check(source.StoreRevision==revision&&source.StoreFingerprint==fingerprint&&!preparation.ValidateSource(source),"unsaved source change invalidates preparation");
   var other=SessionState.Create("Candidate Guard");Begin(other,SessionDeathFixtures.MoneyProfile,"preparation-candidate");
   string before=Snapshot(other);var altered=Prepared(other,200,"candidate sealing");Changed(altered.Candidate,"accept-delivery");
   Check(Snapshot(other)==before&&!altered.ValidateSource(other),"mutated candidate invalidates preparation without touching source");
  }
  static void LegacyMigration(SessionStore store){
   var legacy=SessionState.Create("Legacy Guard");string id=legacy.CharacterId;Changed(legacy,"accept-delivery");store.Save("legacy",legacy);
   string path=Path.Combine(store.RootDirectory,"legacy.save");var payload=ReadPayload(path);
   Check((long)payload["schema"]==1L&&(string)payload["content"]=="liaoxi-m2-inventory-v1"&&((IList)payload["commands"])[0] is string,"legacy session remains schema 1 with string journal");
   legacy=store.Load("legacy");Check(!legacy.DeathView().Enabled&&legacy.CharacterId==id&&legacy.View().delivery_accepted,"legacy load preserves identity and accepted task");
   store.Save("legacy",legacy);Check((long)ReadPayload(path)["schema"]==1L,"saving untouched legacy session does not migrate");
   Begin(legacy,SessionDeathFixtures.MoneyProfile,"legacy-death");store.Save("legacy",legacy);TypedJournal(path);
   payload=ReadPayload(path);Check((string)Command(payload,"m2")["action"]=="accept-delivery","migration retains old command as typed M2 action");
   legacy=store.Load("legacy");Check(legacy.CharacterId==id&&legacy.View().delivery_accepted&&legacy.View().money==50,"migration preserves preexisting M2 progress");
   var first=Prepared(legacy,200,"legacy first death");store.Save("legacy",first.Candidate);
   legacy=store.Load("legacy");var second=Prepared(legacy,201,"legacy second death");store.Save("legacy",second.Candidate);
   FinishM2(store,"legacy",store.Load("legacy"));
  }
  static void EmptyReward(SessionStore store){
   var state=SessionState.Create("Empty Reward Guard");Begin(state,SessionDeathFixtures.NoRewardProfile,"empty-death");store.Save("empty",state);
   Progress(state,0,1,false,0,50,"empty reward objective start");var prepared=Prepared(state,200,"empty reward completion");
   Progress(prepared.Candidate,1,1,true,0,50,"empty action vector leaves wallet unchanged");store.Save("empty",prepared.Candidate);
   state=store.Load("empty");Progress(state,1,1,true,0,50,"empty reward durable receipt");
   Unchanged(state,SessionDeathFixtures.Observation(state,200),"AlreadyApplied","empty reward duplicate after reopen");
   Unchanged(state,SessionDeathFixtures.Observation(state,201),"ObjectiveAlreadyComplete","empty reward later death");
  }
  static void CappedReward(){
   // Explicit test-only wallet input. This does not add a session balance command.
   var model=ExampleInventoryCycle.Create();model.Base.Wallet.Money16C4=2000000000u;model.Base.Wallet.StaticMoneyNameReady=true;
   var probe=MoneyRewards.Execute(new MoneyAction{Version=0,Amount=25},model.Base.Wallet);
   Check(probe.Status=="Known"&&probe.Changed==false&&model.Base.Wallet.Money16C4==2000000000u,"capped money action explicitly returns Known unchanged");
   string owner=SessionState.Create("Cap Fixture Owner").CharacterId;var profile=SessionDeathProfile.Resolve(SessionDeathFixtures.MoneyProfile);
   var objective=new SessionDeathObjective(profile,"cap-fixture",owner,model);
   var first=new SessionDeathObservation(owner,"cap-fixture",profile.Id,profile.Hash,"ExternalFixture",1,200,1,SessionDeathFixtures.RecordName);
   var second=Change(first,"victim_id",201u);var one=objective.Observe(first);var two=objective.Observe(second);var view=objective.View();
   Check(one.Status=="Known"&&one.Changed==true&&two.Status=="Known"&&two.Changed==true,"capped objective accepts both explicit deaths");
   Check(view.Completed&&view.RewardRecorded&&view.RewardActionsApplied==1&&view.Count==2&&view.AcceptedDeaths==2&&!view.CallbackAttached&&view.UsesSessionWallet,"capped action consumes one reward receipt");
   Check(model.Base.Wallet.Money16C4==2000000000u,"capped reward keeps supplied wallet balance");
   var duplicate=objective.Observe(second);var later=objective.Observe(Change(first,"victim_id",202u));
   Check(duplicate.Status=="Known"&&duplicate.Changed==false&&duplicate.Reason=="AlreadyApplied"
    &&later.Status=="Known"&&later.Changed==false&&later.Reason=="ObjectiveAlreadyComplete","capped reward cannot repeat");
   Check(objective.View().RewardActionsApplied==1&&model.Base.Wallet.Money16C4==2000000000u,"capped duplicate retains action count and wallet");
  }
  static void MalformedSaves(SessionStore store,string source){
   BadSave<SessionUnsupportedException>(store,source,"death-unknown-schema",delegate(Dictionary<string,object> p){p["schema"]=99L;});
   BadSave<SessionUnsupportedException>(store,source,"death-unknown-content",delegate(Dictionary<string,object> p){p["content"]="future-death-content";});
   BadSave<SessionCorruptException>(store,source,"death-null-command",delegate(Dictionary<string,object> p){((IList)p["commands"])[0]=null;});
   BadSave<SessionCorruptException>(store,source,"death-untyped-command",delegate(Dictionary<string,object> p){((IList)p["commands"])[0]="accept-delivery";});
   BadSave<SessionUnsupportedException>(store,source,"death-unknown-kind",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["kind"]="future-command";});
   BadSave<SessionCorruptException>(store,source,"death-null-kind",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["kind"]=null;});
   BadSave<SessionCorruptException>(store,source,"death-extra-command-field",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["extra"]=true;});
   BadSave<SessionUnsupportedException>(store,source,"death-unknown-profile",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["profile_id"]="future-profile";});
   BadSave<SessionCorruptException>(store,source,"death-null-profile",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["profile_id"]=null;});
   BadSave<SessionCorruptException>(store,source,"death-profile-hash",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["profile_sha256"]=new string('0',64);});
   BadSave<SessionCorruptException>(store,source,"death-objective-id",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["objective_id"]="other-objective";});
   BadSave<SessionCorruptException>(store,source,"death-encounter-type",delegate(Dictionary<string,object> p){Command(p,"begin-death-objective")["encounter_id"]=1L;});
   BadSave<SessionCorruptException>(store,source,"death-null-observation",delegate(Dictionary<string,object> p){Command(p,"external-death")["observation"]=null;});
   BadSave<SessionCorruptException>(store,source,"death-extra-observation",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["extra"]=true;});
   BadSave<SessionCorruptException>(store,source,"death-unknown-owner",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["character_id"]=Guid.NewGuid().ToString("N");});
   BadSave<SessionCorruptException>(store,source,"death-unknown-producer",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["origin"]="NativeCombat";});
   BadSave<SessionCorruptException>(store,source,"death-null-producer",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["origin"]=null;});
   BadSave<SessionCorruptException>(store,source,"death-negative-source",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["source_id"]=-1L;});
   BadSave<SessionCorruptException>(store,source,"death-overflow-victim",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["victim_id"]=4294967296L;});
   BadSave<SessionCorruptException>(store,source,"death-string-generation",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["generation"]="1";});
   BadSave<SessionCorruptException>(store,source,"death-null-record",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["record"]=null;});
   BadSave<SessionCorruptException>(store,source,"death-null-m2-action",delegate(Dictionary<string,object> p){Command(p,"m2")["action"]=null;});
   BadSave<SessionCorruptException>(store,source,"death-unknown-m2-action",delegate(Dictionary<string,object> p){Command(p,"m2")["action"]="grant-xp";});
   BadSave<SessionCorruptException>(store,source,"death-duplicate-receipt",delegate(Dictionary<string,object> p){((IList)p["commands"]).Add(Command(p,"external-death"));});
   BadSave<SessionCorruptException>(store,source,"death-duplicate-begin",delegate(Dictionary<string,object> p){((IList)p["commands"]).Add(Command(p,"begin-death-objective"));});
   BadSave<SessionCorruptException>(store,source,"death-state-hash",delegate(Dictionary<string,object> p){p["state_sha256"]=new string('0',64);});
   // A rewritten outer checksum cannot authorize a different replayed command.
   BadSave<SessionCorruptException>(store,source,"death-changed-valid-victim",delegate(Dictionary<string,object> p){((Dictionary<string,object>)Command(p,"external-death")["observation"])["victim_id"]=202L;});
  }
  public static int Run(string storeRoot){
   checks=0;var store=FreshStore(storeRoot);var state=SessionState.Create("Death Bridge Guard");string id=state.CharacterId;
   Check(!state.DeathView().Enabled&&state.View().money==50,"new session has no implicit death fixture");
   RejectedBegin(state,null,"invalid-profile","null profile");RejectedBegin(state,"future-profile","invalid-profile","unknown begin profile");
   RejectedBegin(state,SessionDeathFixtures.MoneyProfile,null,"null encounter");RejectedBegin(state,SessionDeathFixtures.MoneyProfile,"","empty encounter");
   RejectedBegin(state,SessionDeathFixtures.MoneyProfile,"../encounter","path encounter");RejectedBegin(state,SessionDeathFixtures.MoneyProfile,new string('a',65),"overlong encounter");
   Begin(state,SessionDeathFixtures.MoneyProfile,"death-main");Progress(state,0,2,false,0,50,"started money objective");
   string before=Snapshot(state);var repeated=state.BeginDeathObjective(SessionDeathFixtures.MoneyProfile,"death-main");
   Check(repeated.Status=="Known"&&!repeated.Changed&&Snapshot(state)==before,"same begin is idempotent");
   RejectedBegin(state,SessionDeathFixtures.NoRewardProfile,"death-main","rebind profile");RejectedBegin(state,SessionDeathFixtures.MoneyProfile,"other-encounter","rebind encounter");
   var inactive=SessionState.Create("Inactive Guard");Rejected(inactive,Change(SessionDeathFixtures.Observation(state,200),"character_id",inactive.CharacterId),"observation before explicit begin");
   var detached=state.DeathView();detached.Count=99;detached.RewardActionsApplied=99;detached.ProfileId="mutated-view";
   Check(Snapshot(state)==before,"death view has no mutable ownership into live session");
   store.Save("death-main",state);string path=Path.Combine(store.RootDirectory,"death-main.save");TypedJournal(path);
   Check(state.StoreRevision==1,"first bridge checkpoint");var copy=state.CopyForCommit();
   Check(Snapshot(copy)==Snapshot(state)&&!Object.ReferenceEquals(copy,state),"commit copy preserves content and store metadata");
   Changed(copy,"accept-delivery");Check(!state.View().delivery_accepted&&copy.View().delivery_accepted,"commit copy owns independent M2 graph");
   before=FileHash(path);Admission(state);Check(FileHash(path)==before,"all rejected observations leave durable save untouched");
   PreparationOwnership();CappedReward();
   var first=Prepared(state,200,"first accepted death");Progress(first.Candidate,1,2,false,0,50,"partial candidate");
   var unsavedSecond=Prepared(first.Candidate,201,"second death before first candidate save");
   Progress(unsavedSecond.Candidate,2,2,true,1,75,"unsaved journal replays numeric fixture fields");
   Progress(first.Candidate,1,2,false,0,50,"unsaved second preparation preserves first candidate");
   before=Snapshot(state);store.Save("death-main",first.Candidate);Check(Snapshot(state)==before,"saving candidate does not mutate original session");
   state=store.Load("death-main");Check(state.CharacterId==id&&state.StoreRevision==2,"partial reopen preserves character and revision");
   Progress(state,1,2,false,0,50,"partial reopen");var accepted=SessionDeathFixtures.Observation(state,200);
   Unchanged(state,accepted,"AlreadyApplied","duplicate after partial reopen");
   Rejected(state,Change(accepted,"source_id",2u),"ownership validation precedes duplicate receipt");
   Rejected(state,Change(accepted,"record","own/fixtures/conflicting.dbr"),"same death key with conflicting body");
   var stale=store.Load("death-main");var winner=Prepared(state,201,"completion candidate");var loser=Prepared(stale,201,"stale completion candidate");
   Progress(winner.Candidate,2,2,true,1,75,"completion applies reward once");store.Save("death-main",winner.Candidate);before=FileHash(path);
   Throws<SessionConflictException>(delegate{store.Save("death-main",loser.Candidate);},"stale death candidate writer");
   Check(FileHash(path)==before,"stale candidate cannot change committed bytes");state=store.Load("death-main");
   Progress(state,2,2,true,1,75,"completed reopen");Check(state.StoreRevision==3,"stale writer did not advance revision");
   Unchanged(state,SessionDeathFixtures.Observation(state,200),"AlreadyApplied","first death receipt after completed reopen");
   Unchanged(state,SessionDeathFixtures.Observation(state,201),"AlreadyApplied","second death receipt after completed reopen");
   Unchanged(state,SessionDeathFixtures.Observation(state,202),"ObjectiveAlreadyComplete","new death after completed objective");
   Rejected(state,Change(SessionDeathFixtures.Observation(state,200),"source_id",2u),"completed duplicate still validates source owner");
   before=Snapshot(state);repeated=state.BeginDeathObjective(SessionDeathFixtures.MoneyProfile,"death-main");
   Check(repeated.Status=="Known"&&!repeated.Changed&&Snapshot(state)==before,"completed objective cannot restart");
   var unknown=state.Execute("attack");Check(unknown.Status=="Unsupported"&&!unknown.Changed&&Snapshot(state)==before,"session action does not invent a damage producer");
   state=FinishM2(store,"death-main",state);Check(state.CharacterId==id,"combined lifecycle preserves character identity");
   Unchanged(state,SessionDeathFixtures.Observation(state,201),"AlreadyApplied","death receipt survives later ordinary M2 commands");
   LegacyMigration(store);EmptyReward(store);MalformedSaves(store,path);
   Progress(store.Load("death-main"),2,2,true,1,190,"malformed save probes preserve valid slot");return checks;
  }
  // Call in one process, exit it, then call VerifyRestart in a second process.
  // The marker deliberately carries all cross-phase expectations on disk.
  public static int PrepareRestart(string storeRoot){
   checks=0;var store=FreshStore(storeRoot);var state=SessionState.Create("Restart Death Guard");
   Begin(state,SessionDeathFixtures.MoneyProfile,"death-restart-encounter");store.Save(RestartSlot,state);
   var partial=Prepared(state,200,"restart partial preparation");store.Save(RestartSlot,partial.Candidate);state=partial.Candidate;
   Progress(state,1,2,false,0,50,"restart phase one partial checkpoint");string path=Path.Combine(store.RootDirectory,RestartSlot+".save");TypedJournal(path);
   var expected=new Dictionary<string,object>{{"character_id",state.CharacterId},{"revision",state.StoreRevision},
    {"profile_sha256",state.DeathView().ProfileHash},{"snapshot",Snapshot(state)},{"save_sha256",FileHash(path)}};
   File.WriteAllText(Path.Combine(store.RootDirectory,RestartMarker),StrictJson.Canonical(expected),new UTF8Encoding(false));
   Check(state.StoreRevision==2,"restart checkpoint contains begin and partial death commits");return checks;
  }
  public static int VerifyRestart(string storeRoot){
   checks=0;var store=new SessionStore(storeRoot);string path=Path.Combine(store.RootDirectory,RestartSlot+".save");
   var expected=(Dictionary<string,object>)StrictJson.Parse(Text(Path.Combine(store.RootDirectory,RestartMarker)));
   Check(expected.Count==5&&FileHash(path)==(string)expected["save_sha256"],"restart phase two opens exact phase one save");
   var state=store.Load(RestartSlot);
   Check(state.CharacterId==(string)expected["character_id"]&&state.StoreRevision==(long)expected["revision"]
    &&state.DeathView().ProfileHash==(string)expected["profile_sha256"],"restart identity revision and pinned profile survive process boundary");
   Check(Snapshot(state)==(string)expected["snapshot"],"restart replays the complete phase one semantic state");
   Progress(state,1,2,false,0,50,"restart loaded partial progress");
   Unchanged(state,SessionDeathFixtures.Observation(state,200),"AlreadyApplied","restart duplicate does not count twice");
   Rejected(state,Change(SessionDeathFixtures.Observation(state,200),"source_id",2u),"restart duplicate cannot bypass ownership");
   var completion=Prepared(state,201,"restart second death");Progress(completion.Candidate,2,2,true,1,75,"restart reward candidate");
   store.Save(RestartSlot,completion.Candidate);state=store.Load(RestartSlot);Progress(state,2,2,true,1,75,"restart completed reload");
   Unchanged(state,SessionDeathFixtures.Observation(state,201),"AlreadyApplied","restart reward receipt is durable");
   Unchanged(state,SessionDeathFixtures.Observation(state,202),"ObjectiveAlreadyComplete","restart completed goal rejects extra progress");
   state=FinishM2(store,RestartSlot,state);Check(state.CharacterId==(string)expected["character_id"],"restart and M2 continuation retain same character");
   string before=FileHash(path);Unchanged(state,SessionDeathFixtures.Observation(state,200),"AlreadyApplied","restart final duplicate remains idempotent");
   Check(FileHash(path)==before,"restart duplicate leaves final durable save unchanged");return checks;
  }
 }
}
