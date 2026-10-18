using System;
using System.Collections.Generic;
using GrimDynasty.TaskAuto;
public static class EventDispatchTests {
 static int n;
 static void Is(bool b,string label){n++;if(!b)throw new Exception(label);}
 public static int Main(){try{
  var bus=new OfflineEvents(true){DebugEnabled=false};var calls=new List<string>();var payload=new QuestEvent{Kind=42};
  Is(bus.Send(payload,99).Delivered==0&&bus.ContainsRow(99)==false,"send missing does not create");
  var a=new OfflineHandler{Invoke=e=>calls.Add("a")};var b=new OfflineHandler{Invoke=e=>calls.Add("b")};
  Is(bus.Register(99,a).Changed==true,"register");Is(bus.Register(99,a).Changed==false,"identity dedup");Is(bus.Register(99,b).Changed==true,"second");
  Is(bus.Send(payload,99).Delivered==2&&String.Join(",",calls)=="a,b","route independent of payload kind and ordered");
  Is(bus.UnRegister(99,a).Changed==true,"remove first identity");calls.Clear();bus.Send(payload,99);Is(String.Join(",",calls)=="b","after removal");
  Is(bus.UnRegister(100,a).Changed==true&&bus.ContainsRow(100)==true,"unregister missing creates empty row");Is(bus.UnRegister(100,a).Changed==false,"repeat empty unregister");
  var live=new OfflineEvents(true){DebugEnabled=false};var c=new OfflineHandler{Invoke=e=>calls.Add("c")};var append=new OfflineHandler{Invoke=e=>{calls.Add("append");live.Register(1,c);}};live.Register(1,append);calls.Clear();
  Is(live.Send(payload,1).Delivered==2&&String.Join(",",calls)=="append,c","newly appended receiver visited in same send");
  var remove=new OfflineEvents(true){DebugEnabled=false};OfflineHandler self=null;self=new OfflineHandler{Invoke=e=>{calls.Add("self");remove.UnRegister(1,self);}};remove.Register(1,self);remove.Register(1,b);remove.Register(1,c);calls.Clear();
  Is(remove.Send(payload,1).Delivered==2&&String.Join(",",calls)=="self,c","self removal shifts and skips next pointer");
  var nested=new OfflineEvents(true){DebugEnabled=false};nested.Register(2,c);nested.Register(1,new OfflineHandler{Invoke=e=>{calls.Add("outer");nested.Send(e,2);}});calls.Clear();Is(nested.Send(payload,1).Delivered==1&&String.Join(",",calls)=="outer,c","reentrant critical section");
  var unknown=new OfflineEvents(true){DebugEnabled=false};unknown.Register(1,a);unknown.Register(1,new OfflineHandler());calls.Clear();var r=unknown.Send(payload,1);Is(r.Status=="Unsupported"&&r.Delivered==1&&r.PartialEffects&&calls.Count==1,"partial known deliveries disclosed before unknown handler");
  var incomplete=new OfflineEvents(false);Is(incomplete.Send(payload,1).Status=="Unsupported"&&incomplete.ContainsRow(1)==null,"unknown index not empty");Is(incomplete.Register(1,a).Status=="Unsupported","unknown register");
  bus.DebugEnabled=true;Is(bus.Send(payload,99).Status=="Unsupported","debug logging excluded");Is(bus.Send(payload,12345).Status=="Known","debug branch not reached for absent route");
  Is(bus.CheckEmpty(100).Status=="Unsupported","empty existing debug row still logs");bus.DebugEnabled=false;Is(bus.CheckEmpty(100).Status=="Known","known empty row");Is(bus.CheckEmpty(99).Status=="Unsupported","auto refuses populated route");
  Is(bus.Register(uint.MaxValue,c).Status=="Known"&&bus.Send(payload,uint.MaxValue).Delivered==1,"uint route high bits");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
