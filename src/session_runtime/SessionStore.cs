using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GrimDynasty.Session {
 // This store owns only its application's runtime-data/saves directory. It never
 // discovers or imports a Grim Dawn/Steam save. Callers choose their own package root.
 public sealed class SessionStore {
  const int MaxBytes=256*1024;
  readonly string root;
  public string RootDirectory {get{return root;}}
  public SessionStore(string saveRoot){
   if(String.IsNullOrWhiteSpace(saveRoot))throw new ArgumentException("Save root required");
   root=Path.GetFullPath(saveRoot).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
   if(root.Length<3||!Char.IsLetter(root[0])||root[1]!=':'||root[2]!='\\'||root.IndexOf(':',2)>=0
    ||!root.EndsWith("\\runtime-data\\saves",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Own local runtime-data/saves root required");
   if(new DriveInfo(Path.GetPathRoot(root)).DriveType==DriveType.Network)throw new ArgumentException("Network save root unsupported");
   CheckPath(root);Directory.CreateDirectory(root);CheckPath(root);
  }
  static void CheckPath(string path){
   for(string p=path;!String.IsNullOrEmpty(p);p=Path.GetDirectoryName(p)){
    if(File.Exists(p)||Directory.Exists(p))if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Reparse paths are not accepted for own state");
   }
  }
  string SlotPath(string slot){
   if(String.IsNullOrEmpty(slot)||slot.Length>32)throw new ArgumentException("Slot requires 1..32 ASCII letters, digits, dash or underscore");
   foreach(char c in slot)if(!(c>='a'&&c<='z')&&!(c>='A'&&c<='Z')&&!(c>='0'&&c<='9')&&c!='-'&&c!='_')throw new ArgumentException("Slot must be an identifier, not a path");
   string upper=slot.ToUpperInvariant();
   if(upper=="CON"||upper=="PRN"||upper=="AUX"||upper=="NUL"||(upper.Length==4&&(upper.StartsWith("COM",StringComparison.Ordinal)||upper.StartsWith("LPT",StringComparison.Ordinal))&&upper[3]>='1'&&upper[3]<='9'))throw new ArgumentException("Device slot is not accepted");
   CheckPath(root);string path=Path.Combine(root,slot+".save");CheckPath(path);return path;
  }
  FileStream Lock(string path){
   string lockPath=path+".lock";CheckPath(lockPath);
   try{return new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
   catch(IOException){throw new SessionConflictException("Save slot is busy; no state was committed");}
  }
  public SessionState Load(string slot){string path=SlotPath(slot);using(var gate=Lock(path))return Read(path);}
  public void Save(string slot,SessionState state){
   if(state==null)throw new ArgumentNullException("state");string path=SlotPath(slot);
   using(var gate=Lock(path)){
    bool exists=File.Exists(path);
    if(exists){
     var current=Read(path);
     if(state.StoreRevision==0||current.CharacterId!=state.CharacterId||current.StoreRevision!=state.StoreRevision||current.StoreFingerprint!=state.StoreFingerprint)
      throw new SessionConflictException("Save revision/identity changed; reload before writing");
    } else if(state.StoreRevision!=0||state.StoreFingerprint!=null)throw new SessionConflictException("Previously loaded save is missing; refusing implicit recreation");
    long revision=checked(state.StoreRevision+1);string text=Encode(state,revision);
    WriteAtomic(path,text,exists,true);
    state.StoreRevision=revision;state.StoreFingerprint=StrictJson.Sha256(text);
   }
  }
  public SessionState Recover(string slot){
   string path=SlotPath(slot);using(var gate=Lock(path)){
    string backup=path+".bak";CheckPath(backup);var recovered=Read(backup);
    long highest=recovered.StoreRevision;
    if(File.Exists(path)){
     // A valid newer save still requires explicit recover, and recovery advances
     // its revision rather than silently rolling the revision counter backwards.
     try{var current=Read(path);if(current.CharacterId!=recovered.CharacterId)throw new SessionConflictException("Backup belongs to a different character");highest=Math.Max(highest,current.StoreRevision);}
     catch(SessionCorruptException){}
     catch(SessionUnsupportedException){throw new SessionUnsupportedException("Current save version is unsupported; recovery cannot overwrite a newer format");}
    }
    long revision=checked(highest+1);string text=Encode(recovered,revision);
    WriteAtomic(path,text,File.Exists(path),false);
    recovered.StoreRevision=revision;recovered.StoreFingerprint=StrictJson.Sha256(text);return recovered;
   }
  }
  static string Encode(SessionState state,long revision){
   var payload=state.Payload(revision);string body=StrictJson.Canonical(payload);
   return StrictJson.Canonical(new Dictionary<string,object>{{"payload",payload},{"sha256",StrictJson.Sha256(body)}});
  }
  SessionState Read(string path){
   CheckPath(path);string text;
   try{
    using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
     if(file.Length>MaxBytes)throw new SessionCorruptException("Own save exceeds 256 KiB");
     var bytes=new byte[(int)file.Length];int position=0;
     while(position<bytes.Length){int n=file.Read(bytes,position,bytes.Length-position);if(n==0)throw new SessionCorruptException("Own save truncated during read");position+=n;}
     text=new UTF8Encoding(false,true).GetString(bytes);
    }
    var envelope=StrictJson.Parse(text) as Dictionary<string,object>;object payload,hash;
    if(envelope==null||envelope.Count!=2||!envelope.TryGetValue("payload",out payload)||!envelope.TryGetValue("sha256",out hash)
     ||!(payload is Dictionary<string,object>)||!(hash is string))throw new SessionCorruptException("Own save envelope invalid");
    if(StrictJson.Sha256(StrictJson.Canonical(payload))!=(string)hash)throw new SessionCorruptException("Own save checksum mismatch");
    var state=SessionState.FromPayload((Dictionary<string,object>)payload);state.StoreFingerprint=StrictJson.Sha256(text);return state;
   }catch(InvalidDataException ex){throw new SessionCorruptException(ex.Message);}
    catch(DecoderFallbackException ex){throw new SessionCorruptException(ex.Message);}
  }
  void WriteAtomic(string path,string text,bool replace,bool keepPrevious){
   byte[] bytes=new UTF8Encoding(false,true).GetBytes(text);if(bytes.Length>MaxBytes)throw new SessionCorruptException("Encoded own save exceeds limit");
   string temporary=path+".tmp-"+Guid.NewGuid().ToString("N");CheckPath(path);CheckPath(path+".bak");
   try{
    using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){file.Write(bytes,0,bytes.Length);file.Flush(true);}
    // Verify what was actually written before replacing any committed state.
    Read(temporary);
    if(replace)File.Replace(temporary,path,keepPrevious?path+".bak":null);
    else File.Move(temporary,path);
   }finally {if(File.Exists(temporary))File.Delete(temporary);}
  }
 }
}
