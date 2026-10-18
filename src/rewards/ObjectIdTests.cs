using System;
using System.Collections.Generic;
using System.Threading;
using GrimDynasty.TaskItems;
class ObjectIdTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static ObjectIdState State(uint counter,byte wrapped){return new ObjectIdState{Counter168=counter,Wrapped16C=wrapped,NetworkManagerPresent=false};}
 static void Main(){
  uint id;var s=State(40,0);Need(ObjectIdAllocator.Check(s,out id).Status=="Known"&&id==40&&s.Counter168==40&&s.Wrapped16C==0,"read-only allocation plan");
  Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==40&&s.Counter168==41,"unwrapped postincrement");
  s=State(0,0);Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0&&s.Counter168==1,"allocator does not invent a zero-ID skip");
  s=State(7,0);s.IndexedKeys=new HashSet<uint>{7};Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==7,"unwrapped branch does not probe index");
  s=State(0x0ffffffe,0);Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0x0ffffffe&&s.Counter168==0x0fffffff&&s.Wrapped16C==0,"last pre-wrap counter");s.IndexComplete=true;s.IndexedKeys=new HashSet<uint>{0,1};Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==2&&s.Counter168==3&&s.Wrapped16C==1,"sentinel switches sticky mode and probes from zero");
  s=State(0x0fffffff,0);Need(ObjectIdAllocator.Allocate(s,out id).Status=="Unsupported"&&s.Counter168==0x0fffffff&&s.Wrapped16C==0,"unknown wrap index rejects before reset writes");
  s=State(5,2);s.IndexComplete=true;s.IndexedKeys=new HashSet<uint>();Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==5&&s.Wrapped16C==2&&s.IndexedKeys.Count==0,"nonzero mode byte retained and no index reservation");
  s=State(0x0ffffffe,1);s.IndexComplete=true;s.IndexedKeys=new HashSet<uint>{0x0ffffffe,0};Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==1&&s.Counter168==2,"collision increment skips sentinel and wraps during search");
  s=State(5,0);s.NetworkManagerPresent=true;s.NetworkReady6A8=true;s.AssignedSlot1A8=3;Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0x30000005,"network prefix shift OR");
  s=State(5,0);s.NetworkManagerPresent=true;s.NetworkReady6A8=false;Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==5,"not-ready manager needs no role fields");
  s=State(5,0);s.NetworkManagerPresent=true;s.NetworkReady6A8=true;s.AssignedSlot1A8=-1;s.PeerSlot78=2;Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0x30000005,"fallback peer plus one");
  s=State(5,0);s.NetworkManagerPresent=true;s.NetworkReady6A8=true;s.AssignedSlot1A8=19;Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0x30000005,"shift uses low slot nibble without guessed validation");
  s=State(5,1);s.NetworkManagerPresent=true;s.NetworkReady6A8=true;s.AssignedSlot1A8=3;s.IndexComplete=true;s.IndexedKeys=new HashSet<uint>{5,0x30000006};Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0x30000006,"native search uses raw counter key, not final prefixed ID");
  s=State(0xa0000001,0);Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0xa0000001,"counter is not silently masked outside sentinel test");
  s=State(0xafffffff,0);s.IndexComplete=true;s.IndexedKeys=new HashSet<uint>();Need(ObjectIdAllocator.Allocate(s,out id).Status=="Known"&&id==0&&s.Counter168==1&&s.Wrapped16C==1,"low-mask sentinel resets whole raw counter");
  s=State(10,0);s.NetworkManagerPresent=true;Need(ObjectIdAllocator.Allocate(s,out id).Status=="Unsupported"&&s.Counter168==10,"unknown network-ready field not false");
  s=State(10,0);s.NetworkManagerPresent=true;s.NetworkReady6A8=true;s.AssignedSlot1A8=-1;Need(ObjectIdAllocator.Allocate(s,out id).Status=="Unsupported"&&s.Counter168==10,"unknown fallback peer rejected");
  s=State(0,1);s.IndexComplete=true;s.IndexedKeys=new HashSet<uint>();for(uint i=0;i<4097;i++)s.IndexedKeys.Add(i);Need(ObjectIdAllocator.Allocate(s,out id).Status=="Unsupported"&&s.Counter168==0&&s.Wrapped16C==1,"bounded search failure leaves state unchanged");
  s=State(1000,0);var seen=new HashSet<uint>();Exception error=null;ThreadStart run=()=>{try{for(int i=0;i<100;i++){uint allocated;if(ObjectIdAllocator.Allocate(s,out allocated).Status!="Known")throw new Exception("allocate");lock(seen){if(!seen.Add(allocated))throw new Exception("duplicate");}}}catch(Exception e){error=e;}};var a=new Thread(run);var b=new Thread(run);a.Start();b.Start();a.Join();b.Join();Need(error==null&&seen.Count==200&&s.Counter168==1200,"own critical section serializes concurrent allocation calls");
  Console.WriteLine("Object ID assertions passed: "+n);
 }
}
