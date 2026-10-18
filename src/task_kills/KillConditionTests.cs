using System;
using System.Collections.Generic;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskKills;
public static class KillConditionTests {
 static int n;
 static void Is(bool b,string s){n++;if(!b)throw new Exception(s);}
 static KillCondition Make(OfflineEvents bus,KillWorld world,int comparison,uint target,params string[] names){KillCondition c;var r=KillCondition.Create(1,comparison,target,names,false,bus,world,out c);if(r.Status!="Known")throw new Exception(r.Reason);return c;}
 public static int Main(){try{
  var bus=new OfflineEvents(true){DebugEnabled=false};var world=new KillWorld{IndexComplete=true};world.Entities.Add(1,new EntitySnapshot{Kind=EntityKind.Player});world.Entities.Add(2,new EntitySnapshot{Kind=EntityKind.Monster,RawByte20A4=0});
  var body=new KillEventBody{SourceId=1,VictimId=2,RecordName="records/a.dbr"};var c=Make(bus,world,3,3,"records/a.dbr");
  Is(c.Evaluate(null).Value==false,"initial count");Is(c.Start().Status=="Known"&&c.CallbackOwnerAttached,"start owner binding");Is(c.Start().Changed==false,"start idempotent registration");
  Is(bus.Send(new QuestEvent{Kind=999,Body=body},11).Delivered==1&&c.Current==1,"event route independent of payload Kind; count once");Is(c.ChangedByte14==1&&c.TextByte15==1,"both changed bytes");
  body.RecordName="RECORDS/A.DBR";bus.Send(new QuestEvent{Body=body},11);Is(c.Current==1,"event name not normalized");body.RecordName="records\\a.dbr";bus.Send(new QuestEvent{Body=body},11);Is(c.Current==1,"event slash exact");body.RecordName="records/a.dbr\0";bus.Send(new QuestEvent{Body=body},11);Is(c.Current==1,"full-length NUL exact");body.RecordName="records/a.dbr";
  world.Entities[1].Kind=EntityKind.Other;body.RecordName=null;Is(c.Receive(body).Changed==false&&c.Current==1,"nonplayer rejects before record access");world.Entities[1].Kind=EntityKind.Player;body.RecordName="records/a.dbr";
  world.Entities[3]=new EntitySnapshot{Kind=EntityKind.Monster,Slot8D0OwnerId=4};world.Entities[4]=new EntitySnapshot{Kind=EntityKind.Player};body.SourceId=3;Is(c.Receive(body).Changed==true&&c.Current==2,"monster controlled by any player counts");world.Entities[4].Kind=EntityKind.Other;Is(c.Receive(body).Changed==false&&c.Current==2,"owner not player");world.Entities[4].Kind=EntityKind.Player;world.Entities[3].Slot8D0OwnerId=null;Is(c.Receive(body).Status=="Unsupported"&&c.Current==2,"unknown owner slot");body.SourceId=1;
  world.Entities[2].RawByte20A4=255;Is(c.Receive(body).Changed==false&&c.Current==2,"victim flag excludes");world.Entities[2].RawByte20A4=null;Is(c.Receive(body).Status=="Unsupported","unknown victim flag");world.Entities[2].Kind=EntityKind.Other;Is(c.Receive(body).Changed==true&&c.Current==3,"nonmonster victim follows eligible guard");Is(c.Evaluate(null).Value==true,"target reached");
  world.Entities.Remove(2);Is(c.Receive(body).Changed==true&&c.Current==4,"known absent victim still eligible");world.IndexComplete=false;Is(c.Receive(body).Status=="Unsupported"&&c.Current==4,"unknown victim not absence");world.IndexComplete=true;
  c.Current=uint.MaxValue;Is(c.Receive(body).Changed==true&&c.Current==0,"32 bit increment wraps");c.ChangedByte14=7;Is(c.Reset().Status=="Known"&&c.Current==0&&c.TextByte15==1&&c.ChangedByte14==7,"reset preserves byte14 and marks byte15");
  c.PureLocalizationContextKnown=true;c.SystemTimeSnapshot=4000;Is(c.CheckOnChangedNoDisplay().Status=="Known","strict time gate equality");c.SystemTimeSnapshot=4001;Is(c.CheckOnChangedNoDisplay().Status=="Unsupported","display side effect excluded");c.LastDisplayTick=int.MaxValue;c.SystemTimeSnapshot=int.MinValue;Is(c.CheckOnChangedNoDisplay().Status=="Known","signed wrapped elapsed bits");c.PureLocalizationContextKnown=false;Is(c.CheckOnChangedNoDisplay().Status=="Unsupported","localization boundary unknown");
  uint before=c.Current;Is(c.End().Status=="Known"&&!c.CallbackOwnerAttached,"end unbinds owner");Is(bus.Send(new QuestEvent{Body=body},11).Delivered==0&&c.Current==before,"end unsubscribed");Is(c.Receive(body).Status=="Unsupported","detached callback domain");
  var duplicate=Make(bus,world,3,1,"records/a.dbr","records/a.dbr");duplicate.Start();Is(duplicate.Receive(body).Changed==true&&duplicate.Current==1,"duplicate match counts once");duplicate.End();
  for(int i=0;i<6;i++){var cmp=Make(bus,world,i,3,"a");cmp.Current=2;Is(cmp.Evaluate(null).Value==new[]{true,true,false,false,false,true}[i],"comparison "+i);}
  var signed=Make(bus,world,3,1,"a");signed.Current=uint.MaxValue;Is(signed.Evaluate(null).Value==false,"signed current high bit");signed=Make(bus,world,3,0x80000000,"a");Is(signed.Evaluate(null).Value==true,"signed target high bit");Is(Make(bus,world,9,1,"a").Evaluate(null).Status=="Unsupported","invalid comparison outside domain");
  KillCondition normalized;Is(KillCondition.Create(1,3,1,new[]{"Records\\I.DBR"},false,bus,world,out normalized).Status=="Unsupported","CRT locale unspecified");Is(KillCondition.Create(1,3,1,new[]{"Records\\I.DBR"},true,bus,world,out normalized).Status=="Known"&&normalized.NormalizedNames[0]=="records/i.dbr","explicit ASCII C locale normalization");Is(KillCondition.Create(1,3,1,new[]{"妖.dbr"},true,bus,world,out normalized).Status=="Unsupported","nonASCII CRT domain");Is(KillCondition.Create(2,3,1,new[]{"a"},false,bus,world,out normalized).Status=="Unsupported","unknown version");
  Is(c.Dispose().Status=="Known"&&c.Evaluate(null).Status=="Unsupported","dispose invalidates further evaluation");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
