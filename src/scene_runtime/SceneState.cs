using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GrimDynasty.Session;

namespace GrimDynasty.Scene {
 // Own world coordinates and presentation wrap the accepted headless session.
 // These geometry/interaction rules are not claimed as native engine recovery.
 public sealed class SceneState {
  public readonly SceneWorld World;
  public readonly SessionState Session;
  public readonly SceneAgents Agents;
  public bool MigratedVisualPatrol {get;internal set;}
  internal string Fingerprint;
  internal SceneState(SessionState session,SceneWorld world){Session=session;World=world;Agents=SceneAgentPersistence.Create(world);}
  public static SceneState Create(string name){return new SceneState(SessionState.Create(name),new SceneWorld());}
  public void TickAgents(float dt){Agents.Step(dt,new SceneAgentTarget(Session.CharacterId,World.PlayerX,World.PlayerY));}
  public SessionResult Interact(){
   foreach(var npc in World.Npcs){
    float dx=npc.X-World.PlayerX,dy=npc.Y-World.PlayerY;if(dx*dx+dy*dy>80*80)continue;
    var view=Session.View();
    if(npc.Id=="quartermaster")return Session.Execute(view.delivery_accepted?"deliver":"accept-delivery");
    if(npc.Id=="supply-clerk")return Session.Execute(view.return_accepted?"return-item":"accept-return");
   }
   return new SessionResult{Status="Known",Reason="NoNearbyNpc",Changed=false,View=Session.View()};
  }
 }
 public sealed class SceneStore {
  readonly string root;
  public SceneStore(string path){root=new SessionStore(path).RootDirectory;}
  string PathFor(string slot){
   if(String.IsNullOrEmpty(slot)||slot.Length>32)throw new ArgumentException("Scene slot must be a short identifier");
   foreach(char c in slot)if(!(c>='a'&&c<='z')&&!(c>='A'&&c<='Z')&&!(c>='0'&&c<='9')&&c!='-'&&c!='_')throw new ArgumentException("Scene slot cannot be a path");
   // Prefix avoids Windows device basenames without guessing arbitrary filesystem paths.
   string path=Path.Combine(root,"scene-"+slot+".save");Check(path);return path;
  }
  static void Check(string path){for(string p=path;!String.IsNullOrEmpty(p);p=Path.GetDirectoryName(p))if(File.Exists(p)||Directory.Exists(p))if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Scene save reparse path rejected");}
  FileStream Lock(string path){Check(path+".lock");try{return new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){throw new SessionConflictException("Scene slot busy");}}
  public bool Exists(string slot){return File.Exists(PathFor(slot));}
  public SceneState Load(string slot){string path=PathFor(slot);using(var gate=Lock(path))return Read(path);}
  public void Save(string slot,SceneState state){
   string path=PathFor(slot);using(var gate=Lock(path)){
    bool exists=File.Exists(path);
    if(exists){var old=Read(path);if(old.Session.CharacterId!=state.Session.CharacterId||old.Session.StoreRevision!=state.Session.StoreRevision||old.Fingerprint!=state.Fingerprint)throw new SessionConflictException("Scene changed; reload before saving");}
    else if(state.Session.StoreRevision!=0||state.Fingerprint!=null)throw new SessionConflictException("Loaded scene is missing");
    long revision=checked(state.Session.StoreRevision+1);string text=Encode(state,revision);Write(path,text,exists,true);
    state.Session.StoreRevision=revision;state.Fingerprint=StrictJson.Sha256(text);
   }
  }
  // Internal fixture bridge only. Build an independent scene candidate, persist
  // its complete session and patrol state, then let the caller publish it.
  // A failed save never installs the candidate into the currently visible scene.
  internal SceneState CommitExternalDeath(string slot,SceneState current,SessionDeathPreparation prepared){
   if(current==null||prepared==null||!prepared.ValidateSource(current.Session))throw new SessionConflictException("Death preparation no longer matches current session");
   if(prepared.Status!="Known"||!prepared.Changed||prepared.Candidate==null)throw new SessionUnsupportedException("NoAcceptedDeathCandidate");
   var candidate=new SceneState(prepared.Candidate,current.World.CopyForCommit());
   candidate.Agents.RestoreSnapshots(current.Agents.GetSnapshots());
   candidate.Fingerprint=current.Fingerprint;candidate.MigratedVisualPatrol=current.MigratedVisualPatrol;
   Save(slot,candidate);return candidate;
  }
  public SceneState Recover(string slot){
   string path=PathFor(slot);using(var gate=Lock(path)){
    var old=Read(path+".bak");long revision=old.Session.StoreRevision;
    if(File.Exists(path))try{var current=Read(path);if(current.Session.CharacterId!=old.Session.CharacterId)throw new SessionConflictException("Scene backup identity mismatch");revision=Math.Max(revision,current.Session.StoreRevision);}catch(SessionCorruptException){}
    revision=checked(revision+1);string text=Encode(old,revision);Write(path,text,File.Exists(path),false);old.Session.StoreRevision=revision;old.Fingerprint=StrictJson.Sha256(text);return old;
   }
  }
  static string Encode(SceneState state,long revision){
   if(!state.World.IsWalkable(state.World.PlayerX,state.World.PlayerY))throw new SessionCorruptException("Cannot save a blocked scene position");
   var payload=new Dictionary<string,object>{{"schema",2},{"content","liaoxi-scene-v2"},{"x_bits",BitConverter.ToUInt32(BitConverter.GetBytes(state.World.PlayerX),0)},
    {"y_bits",BitConverter.ToUInt32(BitConverter.GetBytes(state.World.PlayerY),0)},{"session",state.Session.Payload(revision)},{"agents",SceneAgentPersistence.Encode(state.Agents)}};
   return StrictJson.Canonical(new Dictionary<string,object>{{"payload",payload},{"sha256",StrictJson.Sha256(StrictJson.Canonical(payload))}});
  }
  SceneState Read(string path){
   Check(path);
   try{
    string text;using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
     if(file.Length>256*1024)throw new SessionCorruptException("Scene save size limit");var bytes=new byte[(int)file.Length];int done=0;
     while(done<bytes.Length){int n=file.Read(bytes,done,bytes.Length-done);if(n==0)throw new SessionCorruptException("Scene truncated");done+=n;}text=new UTF8Encoding(false,true).GetString(bytes);
    }
    var outer=StrictJson.Parse(text) as Dictionary<string,object>;object raw,hash;
    if(outer==null||outer.Count!=2||!outer.TryGetValue("payload",out raw)||!outer.TryGetValue("sha256",out hash)||!(raw is Dictionary<string,object>)||!(hash is string))throw new SessionCorruptException("Scene envelope invalid");
    if(StrictJson.Sha256(StrictJson.Canonical(raw))!=(string)hash)throw new SessionCorruptException("Scene checksum mismatch");
    var p=(Dictionary<string,object>)raw;object schema,content,x,y,session;
    if(!p.TryGetValue("schema",out schema)||!(schema is long))throw new SessionCorruptException("Scene schema invalid");
    bool legacy=(long)schema==1;
    if(!legacy&&(long)schema!=2)throw new SessionUnsupportedException("UnknownSceneSchema");
    if(!p.TryGetValue("content",out content)||!(content is string))throw new SessionCorruptException("Scene content invalid");
    if((string)content!=(legacy?"liaoxi-scene-v1":"liaoxi-scene-v2"))throw new SessionUnsupportedException("UnknownSceneContent");
    if(p.Count!=(legacy?5:6)||!p.TryGetValue("x_bits",out x)||!p.TryGetValue("y_bits",out y)||!p.TryGetValue("session",out session)||!(x is long)||!(y is long)||!(session is Dictionary<string,object>))throw new SessionCorruptException("Scene fields invalid");
    if((long)x<0||(long)y<0||(long)x>UInt32.MaxValue||(long)y>UInt32.MaxValue)throw new SessionCorruptException("Scene position encoding invalid");
    float px=BitConverter.ToSingle(BitConverter.GetBytes((uint)(long)x),0),py=BitConverter.ToSingle(BitConverter.GetBytes((uint)(long)y),0);
    if(Single.IsNaN(px)||Single.IsInfinity(px)||Single.IsNaN(py)||Single.IsInfinity(py))throw new SessionCorruptException("Scene position non-finite");
    var world=new SceneWorld();if(!world.TrySetPosition(px,py))throw new SessionCorruptException("Scene position intersects obstacle or map boundary");
    var state=new SceneState(SessionState.FromPayload((Dictionary<string,object>)session),world);
    // Schema 1 had no actors. Only that exact known format initializes the new
    // authored visual patrol; unknown schemas/content and malformed v2 stop.
    if(legacy)state.MigratedVisualPatrol=true;
    else{object agents;if(!p.TryGetValue("agents",out agents))throw new SessionCorruptException("Missing scene agents");SceneAgentPersistence.Restore(state.Agents,agents,state.Session.CharacterId);}
    state.Fingerprint=StrictJson.Sha256(text);return state;
   }catch(InvalidDataException ex){throw new SessionCorruptException(ex.Message);}catch(DecoderFallbackException ex){throw new SessionCorruptException(ex.Message);}
  }
  void Write(string path,string text,bool replace,bool backup){
   string temp=path+".tmp-"+Guid.NewGuid().ToString("N");Check(path);Check(path+".bak");
   try{byte[] bytes=new UTF8Encoding(false,true).GetBytes(text);using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}Read(temp);
    if(replace)File.Replace(temp,path,backup?path+".bak":null);else File.Move(temp,path);
   }finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
