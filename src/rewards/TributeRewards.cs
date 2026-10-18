using System;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskAuto;
namespace GrimDynasty.Rewards {
 public sealed class TributeAction {public int Version,Amount;}
 public static class TributeRewards {
  public static uint PositiveResult(uint current,uint amount){uint next=unchecked(current+amount);return next>150?150:next;}
  public static MutationResult Execute(TributeAction a,PointActor p){
   if(a==null||a.Version!=0)return MutationResult.Unsupported("UnknownTributeActionVersion");
   if(p==null||!p.MainPlayerResolved.HasValue)return MutationResult.Unsupported("UnknownTributeActor");
   if(!p.MainPlayerResolved.Value)return MutationResult.Known(false);
   if(!p.Tribute16C8.HasValue)return MutationResult.Unsupported("UnknownTributeValue");uint old=p.Tribute16C8.Value,next;
   if(a.Amount>0){next=PositiveResult(old,(uint)a.Amount);if(next>old){
     if(p.Events==null)return MutationResult.Unsupported("UnknownTributeNotificationContext");var check=p.Events.CheckEmpty(0x3f);if(check.Status!="Known")return check;
     // AddTribute sends this kind-only event BEFORE storing its new balance.
     var e=new QuestEvent{Kind=0x3f};p.Produced.Add(e);var sent=p.Events.Send(e,0x3f);if(sent.Status!="Known")return MutationResult.Unsupported(sent.Reason);
    }}
   else if(a.Amount<0){int sum=unchecked((int)old+a.Amount);next=sum<0?0u:(uint)sum;}
   else next=0;
   p.Tribute16C8=next;return MutationResult.Known(next!=old);
  }
 }
}
