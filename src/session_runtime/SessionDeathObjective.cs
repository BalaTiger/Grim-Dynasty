using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskKills;
using GrimDynasty.TokenActions;

namespace GrimDynasty.Session {
 // Explicit test input assertion, never an attack/HP/death implementation.
 // No CLI or interactive scene command creates these internal observations.
 internal sealed class SessionDeathObservation {
  internal readonly string CharacterId,EncounterId,ProfileId,ProfileHash,Origin,RecordName;
  internal readonly uint SourceId,VictimId,Generation;
  internal SessionDeathObservation(string characterId,string encounterId,string profileId,string profileHash,string origin,
   uint sourceId,uint victimId,uint generation,string recordName){
   CharacterId=characterId;EncounterId=encounterId;ProfileId=profileId;ProfileHash=profileHash;Origin=origin;
   SourceId=sourceId;VictimId=victimId;Generation=generation;RecordName=recordName;
  }
  internal Dictionary<string,object> Encode(){return new Dictionary<string,object>{
   {"character_id",CharacterId},{"encounter_id",EncounterId},{"profile_id",ProfileId},{"profile_sha256",ProfileHash},
   {"origin",Origin},{"source_id",SourceId},{"victim_id",VictimId},{"generation",Generation},{"record",RecordName}};}
 }
 internal sealed class SessionDeathView {
  internal bool Enabled,Completed,RewardRecorded,UsesSessionWallet,CallbackAttached;
  internal string ProfileId,ProfileHash,EncounterId,ObjectiveId;
  internal uint Count,Required;
  internal int AcceptedDeaths,RewardActionsApplied;
 }
 internal sealed class SessionDeathPreparation {
  readonly SessionState source;
  readonly string sourceFingerprint,candidateFingerprint;
  internal string Status {get;private set;}
  internal string Reason {get;private set;}
  internal bool Changed {get;private set;}
  internal SessionState Candidate {get;private set;}
  SessionDeathPreparation(SessionState owner,string status,string reason,bool changed,SessionState candidate){
   source=owner;Status=status;Reason=reason;Changed=changed;Candidate=candidate;
   sourceFingerprint=owner.CommitSourceFingerprint();candidateFingerprint=candidate==null?null:candidate.CommitSourceFingerprint();
  }
  internal static SessionDeathPreparation Create(SessionState owner,string status,string reason,bool changed,SessionState candidate){
   return new SessionDeathPreparation(owner,status,reason,changed,candidate);
  }
  internal bool ValidateSource(SessionState current){return Object.ReferenceEquals(source,current)&&current!=null
   &&sourceFingerprint==current.CommitSourceFingerprint()&&(Candidate==null||candidateFingerprint==Candidate.CommitSourceFingerprint());}
 }
 internal static class SessionDeathFixtures {
  internal const string MoneyProfile="fixture-direct-kill-money-v1";
  internal const string NoRewardProfile="fixture-direct-kill-empty-v1";
  internal const string RecordName="own/fixtures/death-target.dbr";
  internal static SessionDeathObservation Observation(SessionState state,uint victimId){
   var v=state.DeathView();
   if(!v.Enabled)throw new InvalidOperationException("Explicit fixture objective must be activated first");
   return new SessionDeathObservation(state.CharacterId,v.EncounterId,v.ProfileId,v.ProfileHash,"ExternalFixture",1,victimId,1,RecordName);
  }
 }
 internal sealed class SessionDeathProfile {
  internal readonly string Id,Hash,ObjectiveId;
  internal readonly uint Required;
  readonly int[] amounts;
  SessionDeathProfile(string id,uint required,int[] rewardAmounts){
   Id=id;Required=required;ObjectiveId="own-fixture-death-objective";amounts=(int[])rewardAmounts.Clone();
   Hash=StrictJson.Sha256(StrictJson.Canonical(new Dictionary<string,object>{
    {"profile",id},{"objective",ObjectiveId},{"origin","ExternalFixture"},{"required",required},
    {"source_player_id",1},{"victims",new[]{200,201,202}},{"generation",1},{"victim_kind","Monster"},
    {"victim_byte_20a4",0},{"record",SessionDeathFixtures.RecordName},{"money_action_version",0},{"reward_amounts",amounts}}));
  }
  internal int[] Amounts {get{return (int[])amounts.Clone();}}
  internal static SessionDeathProfile Resolve(string id){
   if(id==SessionDeathFixtures.MoneyProfile)return new SessionDeathProfile(id,2,new[]{25});
   if(id==SessionDeathFixtures.NoRewardProfile)return new SessionDeathProfile(id,1,new int[0]);
   throw new SessionUnsupportedException("UnknownDeathFixtureProfile");
  }
 }
 internal sealed class SessionDeathObjective {
  readonly SessionDeathProfile profile;
  readonly string characterId,encounterId;
  readonly ExampleInventoryCycle model;
  readonly WalletSnapshot wallet;
  readonly OfflineEvents events;
  readonly KillCondition condition;
  readonly List<SessionDeathObservation> accepted=new List<SessionDeathObservation>();
  bool completed,rewardRecorded;
  int rewardActionsApplied;
  internal SessionDeathObjective(SessionDeathProfile definition,string encounter,string owner,ExampleInventoryCycle sessionModel){
   if(definition==null||sessionModel==null)throw new ArgumentNullException("definition");
   ValidateEncounter(encounter);profile=definition;encounterId=encounter;characterId=owner;model=sessionModel;wallet=model.Base.Wallet;
   if(!WalletOwned())throw new SessionUnsupportedException("DeathRewardWalletOwnershipUnknown");
   // These are explicit own fixture facts, not inferred native initial values.
   var entities=new KillWorld{IndexComplete=true};entities.Entities.Add(1,new EntitySnapshot{Kind=EntityKind.Player});
   entities.Entities.Add(2,new EntitySnapshot{Kind=EntityKind.Player});
   foreach(uint id in new uint[]{200,201,202})entities.Entities.Add(id,new EntitySnapshot{Kind=EntityKind.Monster,RawByte20A4=0});
   events=new OfflineEvents(true){DebugEnabled=false};KillCondition created;
   var result=KillCondition.Create(1,3,profile.Required,new[]{SessionDeathFixtures.RecordName},false,events,entities,out created);
   if(result.Status!="Known")throw new SessionUnsupportedException(result.Reason);condition=created;
   result=condition.Start();if(result.Status!="Known")throw new SessionUnsupportedException(result.Reason);
   // SystemTimeSnapshot intentionally stays unknown. Receive/Evaluate do not
   // call cinematic notification or the unrecovered full Task.Update branch.
  }
  internal static void ValidateEncounter(string value){
   if(String.IsNullOrEmpty(value)||value.Length>64)throw new ArgumentException("Fixture encounter requires 1..64 identifier characters");
   foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='A'&&c<='Z')&&!(c>='0'&&c<='9')&&c!='-'&&c!='_')throw new ArgumentException("Fixture encounter is not an identifier");
  }
  bool WalletOwned(){
   if(wallet==null||wallet.MainPlayerResolved!=true||!wallet.Money16C4.HasValue||!Object.ReferenceEquals(wallet,model.Base.Wallet))return false;
   var world=model.Tasks[0].Runtime.World;if(world==null||!world.MainPlayerResolved)return false;
   foreach(var task in model.Tasks){var money=task.CompletionRuntime as MoneyCompletion;
    if(money==null||!Object.ReferenceEquals(money.Wallet,wallet)||!Object.ReferenceEquals(task.Runtime.World,world))return false;}
   return true;
  }
  static MutationResult Known(bool changed,string reason){return new MutationResult{Status="Known",Changed=changed,Reason=reason};}
  internal MutationResult Observe(SessionDeathObservation observation){
   if(observation==null)return MutationResult.Unsupported("MissingExternalDeathObservation");
   if(observation.Origin!="ExternalFixture")return MutationResult.Unsupported("CombatDamageProducerNotIntegrated");
   if(observation.CharacterId!=characterId)return MutationResult.Unsupported("DeathCharacterOwnershipMismatch");
   if(observation.EncounterId!=encounterId)return MutationResult.Unsupported("DeathEncounterMismatch");
   if(observation.ProfileId!=profile.Id||observation.ProfileHash!=profile.Hash)return MutationResult.Unsupported("DeathProfileMismatch");
   if(observation.SourceId!=1)return MutationResult.Unsupported("DeathSourceOwnershipMismatch");
   if(observation.VictimId<200||observation.VictimId>202)return MutationResult.Unsupported("UnknownDeathVictim");
   if(observation.Generation!=1)return MutationResult.Unsupported("UnknownDeathVictimGeneration");
   if(!String.Equals(observation.RecordName,SessionDeathFixtures.RecordName,StringComparison.Ordinal))return MutationResult.Unsupported("DeathRecordMismatch");
   // Admission precedes duplicate handling: another character cannot turn an
   // existing receipt into a successful ownership check by reusing its key.
   foreach(var previous in accepted)if(previous.VictimId==observation.VictimId&&previous.Generation==observation.Generation)return Known(false,"AlreadyApplied");
   if(completed)return Known(false,"ObjectiveAlreadyComplete");
   if(!WalletOwned())return MutationResult.Unsupported("DeathRewardWalletOwnershipUnknown");
   uint before=condition.Current;
   var delivery=events.Send(new QuestEvent{Kind=11,Body=new KillEventBody{SourceId=observation.SourceId,VictimId=observation.VictimId,RecordName=observation.RecordName}},11);
   if(delivery.Status!="Known")return MutationResult.Unsupported(delivery.Reason);
   if(delivery.Delivered!=1||condition.Current!=before+1)return MutationResult.Unsupported("DeathConditionDidNotAcceptExactlyOnce");
   var evaluated=condition.Evaluate(model.Tasks[0].Runtime.World);
   if(evaluated.Status!="Known"||!evaluated.Value.HasValue)return MutationResult.Unsupported(evaluated.Reason??"UnknownDeathGoalResult");
   if(evaluated.Value.Value){
    foreach(int amount in profile.Amounts){
     var reward=MoneyRewards.Execute(new MoneyAction{Version=0,Amount=amount},wallet);
     if(reward.Status!="Known")return reward;rewardActionsApplied++;
    }
    // Empty plan means no money action. Amount zero is never substituted.
    // A successful capped action also consumes the one-shot reward receipt.
    completed=true;rewardRecorded=true;var ended=condition.End();if(ended.Status!="Known")return ended;
   }
   accepted.Add(observation);return Known(true,completed?"ObjectiveRewardRecorded":"DeathRecorded");
  }
  internal SessionDeathView View(){return new SessionDeathView{
   Enabled=true,ProfileId=profile.Id,ProfileHash=profile.Hash,EncounterId=encounterId,ObjectiveId=profile.ObjectiveId,
   Count=condition.Current,Required=profile.Required,Completed=completed,RewardRecorded=rewardRecorded,
   AcceptedDeaths=accepted.Count,RewardActionsApplied=rewardActionsApplied,UsesSessionWallet=WalletOwned(),CallbackAttached=condition.CallbackOwnerAttached};}
  internal object HashState(){
   var rows=new List<object>();foreach(var observation in accepted)rows.Add(observation.Encode());
   return new Dictionary<string,object>{{"profile_id",profile.Id},{"profile_sha256",profile.Hash},{"objective_id",profile.ObjectiveId},
    {"encounter_id",encounterId},{"character_id",characterId},{"count",condition.Current},{"required",profile.Required},
    {"completed",completed},{"reward_recorded",rewardRecorded},{"reward_actions",rewardActionsApplied},{"accepted",rows},
    {"dirty",condition.ChangedByte14},{"text_dirty",condition.TextByte15},{"callback_attached",condition.CallbackOwnerAttached},{"same_wallet",WalletOwned()}};
  }
 }
}
