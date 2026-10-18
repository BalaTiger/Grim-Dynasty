using System;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class PointRewardTests {
 static int n;
 static void Check(bool b,string s){n++;if(!b)throw new Exception(s);}
 static PointActor Actor(){return new PointActor{MainPlayerResolved=true,Difficulty=0,Flags17F4=0,Skill176C=4,Attribute1768=3,ObjectId=91,StaticSkillNameReady=true,Events=new OfflineEvents(true){DebugEnabled=false}};}
 static void Main(){
  var a=new PointAction{OriginalAmount=2,CurrentAmount=2};var p=Actor();
  Check(PointRewards.Execute(a,p).Status=="Known"&&p.Skill176C==6,"skill add");
  a.CurrentAmount=unchecked((uint)-20);PointRewards.Execute(a,p);Check(p.Skill176C==0,"skill clamp");
  p.Skill176C=int.MaxValue;a.CurrentAmount=1;PointRewards.Execute(a,p);Check(p.Skill176C==0,"skill wrap before clamp");
  a.CurrentAmount=2;p=Actor();p.Flags17F4=1;PointRewards.Execute(a,p);Check(p.Skill176C==4,"normal profile suppress");
  p.Difficulty=1;p.Flags17F4=2;PointRewards.Execute(a,p);Check(p.Skill176C==4,"elite profile suppress");
  p.Difficulty=2;p.Flags17F4=null;PointRewards.Execute(a,p);Check(p.Skill176C==6,"ultimate does not read flags");
  p=Actor();p.Flags17F4=null;Check(PointRewards.Execute(a,p).Status=="Unsupported"&&p.Skill176C==4,"unknown flags atomic");
  p=Actor();p.StaticSkillNameReady=false;Check(PointRewards.Execute(a,p).Status=="Unsupported"&&p.Skill176C==4,"skill initialization explicit");
  p=Actor();p.MainPlayerResolved=false;p.Skill176C=null;Check(PointRewards.Execute(a,p).Status=="Known","missing player noop");
  a.OriginalAmount=3;PointRewards.Modify(a,2);Check(a.CurrentAmount==6,"modify base");PointRewards.Modify(a,3);Check(a.CurrentAmount==9&&a.OriginalAmount==3,"modify does not compound");
  a.OriginalAmount=0x80000001;PointRewards.Modify(a,2);Check(a.CurrentAmount==2,"multiply wraps");
  a.Attribute=true;a.CurrentAmount=2;p=Actor();p.StaticSkillNameReady=false;
  Check(PointRewards.Execute(a,p).Status=="Known"&&p.Attribute1768==5&&p.Produced.Count==1,"attribute writes and produces event");
  Check(p.Produced[0].Kind==0x32&&((AttributePointNotification)p.Produced[0].Body).PlayerObjectId==91,"native event layout represented");
  p.Attribute1768=int.MaxValue;a.CurrentAmount=1;PointRewards.Execute(a,p);Check(p.Attribute1768==int.MinValue,"attribute signed wrap no clamp");
  a.CurrentAmount=0;int count=p.Produced.Count;PointRewards.Execute(a,p);Check(p.Produced.Count==count+1,"attribute zero still sends");
  p=Actor();p.Flags17F4=1;PointRewards.Execute(a,p);Check(p.Produced.Count==0&&p.Attribute1768==3,"suppressed attribute no notification");
  p=Actor();p.Events.Register(0x32,new OfflineHandler{Invoke=e=>{}});Check(PointRewards.Execute(a,p).Status=="Unsupported"&&p.Attribute1768==3&&p.Produced.Count==0,"unknown listener atomic");
  p=Actor();p.Events.IndexComplete=false;Check(PointRewards.Execute(a,p).Status=="Unsupported"&&p.Attribute1768==3,"unknown event index atomic");
  a.Version=1;Check(PointRewards.Modify(a,1).Status=="Unsupported","unknown version");
  Console.WriteLine("Point reward assertions passed: "+n);
 }
}
