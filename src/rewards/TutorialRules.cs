using System;
using System.Collections.Generic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskAuto;
namespace GrimDynasty.Rewards {
 public sealed class TutorialNotification {public uint Page;}
 public sealed class TutorialDispatch {public uint Route;public QuestEvent Payload;}
 public sealed class TutorialSnapshot {
  public ulong? PageCount36CD0;
  public uint[] UnlockWords36CB8;
  public List<uint> UnlockOrder36CA0;
  public OfflineEvents Events;
  public readonly List<TutorialDispatch> Produced=new List<TutorialDispatch>();
  public TutorialSnapshot Copy(){var c=new TutorialSnapshot{PageCount36CD0=PageCount36CD0,UnlockWords36CB8=UnlockWords36CB8==null?null:(uint[])UnlockWords36CB8.Clone(),UnlockOrder36CA0=UnlockOrder36CA0==null?null:new List<uint>(UnlockOrder36CA0),Events=Events};c.Produced.AddRange(Produced);return c;}
 }
 public static class TutorialRules {
  static MutationResult U(string s){return MutationResult.Unsupported(s);}
  static MutationResult Route(TutorialSnapshot s,uint route){return s.Events==null?U("UnknownTutorialEventManager"):s.Events.CheckEmpty(route);}
  static void Send(TutorialSnapshot s,uint page,uint route){
   var e=new QuestEvent{Kind=0x36,Body=new TutorialNotification{Page=page}};
   s.Produced.Add(new TutorialDispatch{Route=route,Payload=e});var sent=s.Events.Send(e,route);
   if(sent.Status!="Known")throw new InvalidOperationException("Tutorial event context changed: "+sent.Reason);
  }
  static MutationResult ApplyUnlock(TutorialSnapshot s,uint page,bool notify){
   if(s==null||!s.PageCount36CD0.HasValue)return U("UnknownTutorialPageCount");
   if((ulong)page>=s.PageCount36CD0.Value)return MutationResult.Known(false);
   uint index=page>>5,mask=1u<<(int)(page&31);
   if(s.UnlockWords36CB8==null||(ulong)index>=(ulong)s.UnlockWords36CB8.Length)return U("UnknownTutorialBitWord");
   if((s.UnlockWords36CB8[(int)index]&mask)!=0)return MutationResult.Known(false);
   if(s.UnlockOrder36CA0==null)return U("UnknownTutorialUnlockVector");
   if(notify){var ready=Route(s,0x36);if(ready.Status!="Known")return ready;}
   s.UnlockWords36CB8[(int)index]|=mask;s.UnlockOrder36CA0.Add(page);
   if(notify)Send(s,page,0x36);return MutationResult.Known(true);
  }
  public static MutationResult CheckUnlock(TutorialSnapshot s,uint page,bool notify){return s==null?U("UnknownTutorialState"):ApplyUnlock(s.Copy(),page,notify);}
  public static MutationResult Unlock(TutorialSnapshot s,uint page,bool notify){var ready=CheckUnlock(s,page,notify);return ready.Status=="Known"?ApplyUnlock(s,page,notify):ready;}
  // ScriptableAction_UnlockTutorial tail-jumps here with notification=true.
  public static MutationResult ExecuteAction(TutorialSnapshot s,uint page){return Unlock(s,page,true);}
  public static MutationResult Show(TutorialSnapshot s,uint page){
   if(s==null||!s.PageCount36CD0.HasValue)return U("UnknownTutorialPageCount");
   if((ulong)page>=s.PageCount36CD0.Value)return MutationResult.Known(false);
   var ready=Route(s,0x37);if(ready.Status!="Known")return ready;
   // The payload remains 0x36 even though Show routes it through 0x37.
   Send(s,page,0x37);return MutationResult.Known(true);
  }
 }
}
