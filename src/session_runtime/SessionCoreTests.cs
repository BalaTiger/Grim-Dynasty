using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GrimDynasty.Session {
 // Runs only within the caller's fresh, own test runtime-data/saves directory.
 public static class SessionCoreTests {
  static int checks;
  static void Check(bool value,string label){if(!value)throw new Exception("Session core test failed: "+label);checks++;}
  static void Throws<T>(Action action,string label) where T:Exception {
   try{action();}catch(T){checks++;return;}throw new Exception("Session core test did not reject: "+label);
  }
  static string Text(string path){return File.ReadAllText(path,new UTF8Encoding(false,true));}
  static string Fingerprint(string path){return StrictJson.Sha256(Text(path));}
  static void WriteAltered(string source,string target,Action<Dictionary<string,object>> alter){
   var envelope=(Dictionary<string,object>)StrictJson.Parse(Text(source));var payload=(Dictionary<string,object>)envelope["payload"];
   alter(payload);envelope["sha256"]=StrictJson.Sha256(StrictJson.Canonical(payload));File.WriteAllText(target,StrictJson.Canonical(envelope),new UTF8Encoding(false));
  }
  public static int Run(string storeRoot){
   checks=0;var store=new SessionStore(storeRoot);
   if(Directory.GetFiles(store.RootDirectory).Length!=0)throw new ArgumentException("Core tests require a fresh own save root");
   string path=Path.Combine(store.RootDirectory,"hero.save");
   var state=SessionState.Create("Test Guard");string id=state.CharacterId;
   Check(state.View().money==50&&state.View().inventory_ids.Length==1&&state.View().inventory_ids[0]==100,"new character fixture");
   store.Save("hero",state);Check(state.StoreRevision==1&&File.Exists(path),"first durable save");
   string before=Fingerprint(path);var blocked=state.Execute("deliver");
   Check(blocked.Status=="Unsupported"&&!blocked.Changed&&state.View().money==50,"unaccepted delivery preserved");
   Check(Fingerprint(path)==before,"unsupported action does not write save");
   var first=store.Load("hero");var stale=store.Load("hero");
   Check(first.CharacterId==id&&stale.CharacterId==id&&first.StoreRevision==1,"identity survives reload");
   Check(first.Execute("accept-delivery").Changed,"accept delivery via old lifecycle");store.Save("hero",first);
   Check(first.StoreRevision==2&&first.View().delivery_accepted&&!stale.View().delivery_accepted,"session state ownership isolated");
   Check(stale.Execute("accept-delivery").Changed,"stale candidate can change own memory");
   Throws<SessionConflictException>(delegate{store.Save("hero",stale);},"stale writer rejected");
   Check(store.Load("hero").StoreRevision==2,"stale writer leaves committed revision");
   Check(File.Exists(path+".bak"),"atomic replacement retains previous version");
   var current=store.Load("hero");Check(current.Execute("deliver").Changed,"delivery removes item and grants reward");store.Save("hero",current);
   Check(current.View().money==125&&current.View().delivery_completed&&current.View().reward_task_uids.Length==1,"first reward state");
   Check(current.View().same_object_identity&&current.View().shared_world_identity&&current.View().inventory_ids.Length==0,"shared object retained after full take");
   int journal=current.View().journal_count;
   Check(!current.Execute("accept-delivery").Changed&&!current.Execute("deliver").Changed&&current.View().journal_count==journal&&current.View().money==125,"completed task cannot restart/reward twice");
   var detachedView=current.View();detachedView.inventory_ids=new uint[]{999};detachedView.reward_task_uids[0]=999;detachedView.money=999;
   Check(current.View().money==125&&current.View().reward_task_uids[0]==1&&current.View().inventory_ids.Length==0,"view has no mutable ownership into live state");
   var second=store.Load("hero");Check(second.Execute("accept-return").Changed,"accept second task after reopen");store.Save("hero",second);
   Check(!current.View().return_accepted&&second.View().return_accepted,"independent sessions do not share tasks");
   Check(second.Execute("return-item").Changed,"return existing retained item");store.Save("hero",second);
   var complete=store.Load("hero");var final=complete.View();
   Check(final.money==165&&final.reward_task_uids.Length==2&&final.reward_task_uids[0]==1&&final.reward_task_uids[1]==2,"both actual reward histories");
   Check(final.inventory_ids.Length==1&&final.inventory_ids[0]==100&&final.same_object_identity&&final.shared_world_identity,"same owner graph reconstructed");
   Check(final.character_id==id&&final.delivery_completed&&final.return_completed,"completed state persists");
   Check(!complete.Execute("return-item").Changed&&!complete.Execute("accept-return").Changed&&complete.View().money==165,"second reward cannot repeat after reopen");
   before=Fingerprint(path);var unsupported=complete.Execute("grant-xp");
   Check(unsupported.Status=="Unsupported"&&!unsupported.Changed&&complete.View().money==165&&Fingerprint(path)==before,"unknown action not defaulted or saved");
   Throws<SessionConflictException>(delegate{store.Save("hero",SessionState.Create("Other Guard"));},"slot ownership cannot be overwritten by new character");
   File.WriteAllText(path,"{",new UTF8Encoding(false));
   Throws<SessionCorruptException>(delegate{store.Load("hero");},"corrupt current does not automatically fall back");
   var recovered=store.Recover("hero");
   Check(recovered.CharacterId==id&&recovered.View().money==125&&recovered.View().return_accepted&&!recovered.View().return_completed,"explicit recovery restores previous valid checkpoint");
   // Recovered revision may equal the lost revision. Fingerprint comparison must
   // still reject a writer that loaded the now-corrupted later state.
   Throws<SessionConflictException>(delegate{store.Save("hero",complete);},"stale pre-recovery writer rejected by content identity");
   Check(recovered.Execute("return-item").Changed,"recovered active task can complete");store.Save("hero",recovered);
   Check(store.Load("hero").View().money==165,"recovered continuation rewards once");
   string schema=Path.Combine(store.RootDirectory,"schema.save");
   WriteAltered(path,schema,delegate(Dictionary<string,object> p){p["schema"]=99L;});
   Throws<SessionUnsupportedException>(delegate{store.Load("schema");},"valid checksum unknown schema");
   string content=Path.Combine(store.RootDirectory,"content.save");
   WriteAltered(path,content,delegate(Dictionary<string,object> p){p["content"]="future-content";});
   Throws<SessionUnsupportedException>(delegate{store.Load("content");},"valid checksum unknown content");
   string wrongState=Path.Combine(store.RootDirectory,"wrong-state.save");
   WriteAltered(path,wrongState,delegate(Dictionary<string,object> p){p["state_sha256"]=new string('0',64);});
   Throws<SessionCorruptException>(delegate{store.Load("wrong-state");},"valid outer checksum but inconsistent semantic state");
   string action=Path.Combine(store.RootDirectory,"action.save");
   WriteAltered(path,action,delegate(Dictionary<string,object> p){((IList)p["commands"]).Add("grant-xp");});
   Throws<SessionUnsupportedException>(delegate{store.Load("action");},"unrecovered journal command not ignored");
   string duplicate=Path.Combine(store.RootDirectory,"duplicate.save");
   WriteAltered(path,duplicate,delegate(Dictionary<string,object> p){((IList)p["commands"]).Add("return-item");});
   Throws<SessionCorruptException>(delegate{store.Load("duplicate");},"duplicate journal completion rejected");
   string duplicateKey=Path.Combine(store.RootDirectory,"duplicate-key.save");
   File.WriteAllText(duplicateKey,"{\"sha256\":\"duplicate\","+Text(path).Substring(1),new UTF8Encoding(false));
   Throws<SessionCorruptException>(delegate{store.Load("duplicate-key");},"duplicate JSON keys rejected before dictionary overwrite");
   string utf=Path.Combine(store.RootDirectory,"invalid-utf.save");File.WriteAllBytes(utf,new byte[]{0xc0,0xaf});
   Throws<SessionCorruptException>(delegate{store.Load("invalid-utf");},"invalid UTF8 rejected");
   Throws<ArgumentException>(delegate{store.Load("../hero");},"slot traversal rejected");
   using(var busy=new FileStream(path+".lock",FileMode.Open,FileAccess.ReadWrite,FileShare.None)){
    Throws<SessionConflictException>(delegate{store.Load("hero");},"exclusive cross-operation lock respected");
   }
   Check(store.Load("hero").View().money==165,"lock failure preserved state");
   var unchanged=SessionState.Create("Same Business State");store.Save("unchanged",unchanged);store.Save("unchanged",unchanged);
   var oldWriter=store.Load("unchanged");string unchangedPath=Path.Combine(store.RootDirectory,"unchanged.save");
   File.WriteAllText(unchangedPath,"{",new UTF8Encoding(false));var same=store.Recover("unchanged");
   Check(same.StoreRevision==oldWriter.StoreRevision&&same.View().money==oldWriter.View().money,"recovery can recreate same revision and semantic state");
   Throws<SessionConflictException>(delegate{store.Save("unchanged",oldWriter);},"unique commit identity invalidates stale writer despite equal state/revision");
   return checks;
  }
 }
}
