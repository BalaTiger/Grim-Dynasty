using System;
using System.Collections.Generic;

namespace GrimDynasty.Session {
 // Own schema 2 adds explicitly admitted fixture observations to the SAME
 // ordered session journal. Reading a schema 1 save never migrates it silently.
 public sealed partial class SessionState {
  internal const int DeathSchemaVersion=2;
  internal const string DeathContentVersion="liaoxi-m3-death-bridge-v1";
  bool deathSchema;
  SessionDeathObjective deathObjective;

  static object MakeM2Command(string action){return new Dictionary<string,object>{{"kind","m2"},{"action",action}};}
  static object MakeBeginCommand(SessionDeathProfile profile,string encounter){return new Dictionary<string,object>{
   {"kind","begin-death-objective"},{"profile_id",profile.Id},{"profile_sha256",profile.Hash},{"encounter_id",encounter},{"objective_id",profile.ObjectiveId}};}
  static object MakeDeathCommand(SessionDeathObservation observation){return new Dictionary<string,object>{{"kind","external-death"},{"observation",observation.Encode()}};}

  internal SessionState CopyForCommit(){
   var candidate=Rebuild(CharacterId,CharacterName,journal,deathSchema);
   candidate.StoreRevision=StoreRevision;candidate.StoreFingerprint=StoreFingerprint;return candidate;
  }
  void CommitCandidate(SessionState candidate){
   model=candidate.model;deathSchema=candidate.deathSchema;deathObjective=candidate.deathObjective;
   journal.Clear();journal.AddRange(candidate.journal);
  }
  internal SessionDeathView DeathView(){return deathObjective==null?new SessionDeathView():deathObjective.View();}
  internal SessionResult BeginDeathObjective(string profileId,string encounterId){
   SessionDeathProfile profile;
   try{profile=SessionDeathProfile.Resolve(profileId);SessionDeathObjective.ValidateEncounter(encounterId);}
   catch(SessionUnsupportedException ex){return Result("Unsupported",ex.Message,false);}
   catch(ArgumentException ex){return Result("Unsupported",ex.Message,false);}
   if(deathObjective!=null){
    var active=deathObjective.View();
    return active.ProfileId==profile.Id&&active.ProfileHash==profile.Hash&&active.EncounterId==encounterId
     ?Result("Known","AlreadyActive",false):Result("Unsupported","DeathObjectiveAlreadyBound",false);
   }
   if(journal.Count>=MaxCommands)return Result("Unsupported","SessionJournalLimit",false);
   var candidate=CopyForCommit();
   if(!candidate.deathSchema){
    // Preserve the already verified legacy history, in its original order.
    for(int i=0;i<candidate.journal.Count;i++)candidate.journal[i]=MakeM2Command((string)candidate.journal[i]);
    candidate.deathSchema=true;
   }
   object command=MakeBeginCommand(profile,encounterId);
   var result=candidate.ApplyJournalCommand(command);
   if(result.Status=="Known"&&result.Changed){candidate.journal.Add(command);CommitCandidate(candidate);}
   return Result(result.Status,result.Reason,result.Changed);
  }
  internal SessionDeathPreparation PrepareExternalDeath(SessionDeathObservation observation){
   if(deathObjective==null)return SessionDeathPreparation.Create(this,"Unsupported","DeathObjectiveNotActive",false,null);
   if(journal.Count>=MaxCommands)return SessionDeathPreparation.Create(this,"Unsupported","SessionJournalLimit",false,null);
   var candidate=CopyForCommit();var observed=candidate.deathObjective.Observe(observation);
   bool changed=observed.Status=="Known"&&observed.Changed==true;
   if(changed)candidate.journal.Add(MakeDeathCommand(observation));
   return SessionDeathPreparation.Create(this,observed.Status,observed.Reason,changed,changed?candidate:null);
  }
  // A revision alone cannot detect unsaved commands. Include identity, the full
  // journal, reconstructed model state and disk concurrency metadata.
  internal string CommitSourceFingerprint(){return StrictJson.Sha256(StrictJson.Canonical(new Dictionary<string,object>{
   {"character_id",CharacterId},{"character_name",CharacterName},{"schema",deathSchema?DeathSchemaVersion:SchemaVersion},
   {"commands",journal.ToArray()},{"state_sha256",StateHash()},{"revision",StoreRevision},{"store_fingerprint",StoreFingerprint}}));}

  SessionResult ApplyJournalCommand(object command){
   if(!deathSchema){
    var action=command as string;if(action==null||action.Length>80)throw new SessionCorruptException("Save journal action invalid");
    return Apply(action);
   }
   var row=command as Dictionary<string,object>;if(row==null)throw new SessionCorruptException("Typed session command is not an object");
   string kind=Text(row,"kind");
   if(kind=="m2"){
    Keys(row,"kind","action");string action=Text(row,"action");
    if(action.Length>80)throw new SessionCorruptException("Save journal action invalid");return Apply(action);
   }
   if(kind=="begin-death-objective"){
    Keys(row,"kind","profile_id","profile_sha256","encounter_id","objective_id");
    var profile=SessionDeathProfile.Resolve(Text(row,"profile_id"));
    if(Text(row,"profile_sha256")!=profile.Hash||Text(row,"objective_id")!=profile.ObjectiveId)throw new SessionCorruptException("Death definition fingerprint mismatch");
    string encounter=Text(row,"encounter_id");
    try{SessionDeathObjective.ValidateEncounter(encounter);}catch(ArgumentException ex){throw new SessionCorruptException(ex.Message);}
    if(deathObjective!=null)return Result("Unsupported","DeathObjectiveAlreadyBound",false);
    deathObjective=new SessionDeathObjective(profile,encounter,CharacterId,model);return Result("Known","DeathObjectiveActivated",true);
   }
   if(kind=="external-death"){
    Keys(row,"kind","observation");object body;
    if(!row.TryGetValue("observation",out body))throw new SessionCorruptException("Death observation missing");
    var fields=body as Dictionary<string,object>;if(fields==null)throw new SessionCorruptException("Death observation is not an object");
    Keys(fields,"character_id","encounter_id","profile_id","profile_sha256","origin","source_id","victim_id","generation","record");
    var observation=new SessionDeathObservation(Text(fields,"character_id"),Text(fields,"encounter_id"),Text(fields,"profile_id"),Text(fields,"profile_sha256"),
     Text(fields,"origin"),UInt(fields,"source_id"),UInt(fields,"victim_id"),UInt(fields,"generation"),Text(fields,"record"));
    if(deathObjective==null)throw new SessionCorruptException("Death observation precedes objective activation");
    var result=deathObjective.Observe(observation);return Result(result.Status,result.Reason,result.Changed==true);
   }
   throw new SessionUnsupportedException("UnknownSessionCommandKind:"+kind);
  }
  static void Keys(Dictionary<string,object> row,params string[] names){
   if(row.Count!=names.Length)throw new SessionCorruptException("Typed session command fields mismatch");
   foreach(string name in names)if(!row.ContainsKey(name))throw new SessionCorruptException("Typed session command field missing:"+name);
  }
  static string Text(Dictionary<string,object> row,string key){
   object value;if(!row.TryGetValue(key,out value)||!(value is string))throw new SessionCorruptException("Typed session field must be text:"+key);return (string)value;
  }
  static uint UInt(Dictionary<string,object> row,string key){
   object value;if(!row.TryGetValue(key,out value)||!(value is long)||(long)value<0||(long)value>UInt32.MaxValue)throw new SessionCorruptException("Typed session field must be uint32:"+key);return (uint)(long)value;
  }
  string StateHash(){
   if(!deathSchema)return LegacyStateHash();
   return StrictJson.Sha256(StrictJson.Canonical(new Dictionary<string,object>{{"m2_state_sha256",LegacyStateHash()},{"death",deathObjective==null?null:deathObjective.HashState()}}));
  }
 }
}
