using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Own storage and callback observations only. Mock allocations/releases below
// are not native allocator recovery or evidence of gameplay/navigation success.
public static class WaypointContractsTests {
 static int scenarios,assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static void Pass(Outcome r){Check(r.ReturnedNormally,"Unexpected stop "+r.Address.ToString("x")+" "+r.Boundary);}
 static void Missing(Outcome r,uint at){Check(!r.ReturnedNormally,"Unexpected success");Equal(at,r.Address,"Boundary address");}
 static void Unknown<T>(Field<T> f){try{f.Read(1);throw new Exception("Unobserved field became known");}catch(MissingContract){assertions++;}}
 static int Count(MockBoundaries b,string name){int n=0;foreach(string s in b.Calls)if(s==name)n++;return n;}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static WaypointMap KnownNullMap(ulong size){var map=new WaypointMap(size);for(ulong n=0;n<size;n++)map.Cell(n,1).Write(null);return map;}
 static Waypoint80 Record(uint kind,ulong word){var r=new Waypoint80();r.Kind00.Write(kind);r.Position08.Q0.Write(word);r.Position08.Q8.Write(word+1);r.Position08.Q16.Write(word+2);return r;}
 static Displacement3 Delta(float x,float y,float z){var v=new Displacement3();v.X0.Write(x);v.Y4.Write(y);v.Z8.Write(z);return v;}
 static Outcome Clear(F f){return MoveContracts.Run(delegate(Invocation i){WaypointContracts.Clear(f.Queue,f.B,i);});}
 static Outcome Grow(F f){return MoveContracts.Run(delegate(Invocation i){WaypointContracts.Grow(f.Queue,f.B,i);});}
 static Outcome Append(F f,Waypoint80 r){return MoveContracts.Run(delegate(Invocation i){WaypointContracts.Append(f.Queue,r,f.B,i);});}
 static byte Next(F f){return WaypointContracts.Next(f.View,f.B,new Invocation());}
 static Outcome AttemptNext(F f){return MoveContracts.Run(delegate(Invocation i){WaypointContracts.Next(f.View,f.B,i);});}
 sealed class F {
  public readonly MovementManager Manager=new MovementManager();
  public readonly WaypointManagerView View;
  public readonly WaypointQueue Queue;
  public readonly MoveContext Context=new MoveContext();
  public readonly WorldPosition Destination=new WorldPosition();
  public readonly object Owner=new object();
  public readonly MockBoundaries B;
  public F(){
   View=new WaypointManagerView(Manager);Queue=View.Queue40;var proxy=new WaypointProxy();proxy.Container00.Write(Queue);Queue.Proxy00.Write(proxy);
   Queue.Map08.Write(null);Queue.MapSize10.Write(0);Queue.Count20.Write(0);Queue.Offset18.Write(0);
   Manager.Owner00.Write(Owner);Manager.Moving38.Write(7);Manager.Registered6C.Write(0);
   Destination.Q0.Write(80);Destination.Q8.Write(81);Destination.Q16.Write(82);
   B=new MockBoundaries(Context);B.Attach(View);
  }
  public Waypoint80 Seed(uint kind,ulong count,ulong offset){var map=KnownNullMap(8);var record=Record(kind,10);map.Cell(offset&7,1).Write(record);Queue.Map08.Write(map);Queue.MapSize10.Write(8);Queue.Count20.Write(count);Queue.Offset18.Write(offset);return record;}
 }
 sealed class MockBoundaries:WaypointBoundaries {
  public readonly List<string> Calls=new List<string>();
  public readonly List<object> Freed=new List<object>();
  public Action<string,MockBoundaries> On=null;
  public object Arg0=null,Arg1=null,Arg2=null;
  public ulong U0=0,U1=0,U2=0;public uint Stride=0;
  public readonly object NavigationValue=new object();
  public object RegionValue=new object();
  public WorldPosition CoordsReturn=new WorldPosition();
  public Displacement3 VectorFirst=Delta(1,2,3),VectorSecond=Delta(4,5,6);
  public byte MoveAccepted=1;
  public bool NullBlock=false,UnobserveAfterGrow=false;
  public WaypointMap MapOverride=null;
  public Waypoint80 LastAllocatedBlock=null;
  public WaypointMap LastAllocatedMap=null;
  public MockBoundaries(MoveContext context):base(context){CoordsReturn.Q0.Write(30);CoordsReturn.Q8.Write(31);CoordsReturn.Q16.Write(32);}
  void Hit(string name){Calls.Add(name);if(On!=null)On(name,this);}
  public override void Grow08F340(WaypointQueue q){base.Grow08F340(q);if(UnobserveAfterGrow){q.MapSize10.Forget();q.Offset18.Forget();}}
  public override WaypointMap AllocateMap0162D0(ulong slots,uint stride){U0=slots;Stride=stride;Hit("allocate-map");LastAllocatedMap=MapOverride??new WaypointMap(slots);return LastAllocatedMap;}
  public override void ReleaseMap016620(WaypointMap map,ulong slots,uint stride){Arg0=map;U0=slots;Stride=stride;Hit("release-map");Freed.Add(map);map.Released=true;}
  public override void CopyMap60B808(WaypointMap target,ulong to,WaypointMap source,ulong from,ulong count){Arg0=target;Arg1=source;U0=to;U1=from;U2=count;Hit("copy-map");base.CopyMap60B808(target,to,source,from,count);}
  public override void ZeroMap5DC33E(WaypointMap target,ulong start,ulong count){Arg0=target;U0=start;U1=count;Hit("zero-map");base.ZeroMap5DC33E(target,start,count);}
  public override Waypoint80 Allocate80(){Hit("allocate80");LastAllocatedBlock=NullBlock?null:new Waypoint80();return LastAllocatedBlock;}
  public override void FreeBlock(Waypoint80 block){Arg0=block;Hit("free-block");Freed.Add(block);}
  public override void FreeMap(WaypointMap map,ulong slots){Arg0=map;U0=slots;Hit("free-map");Freed.Add(map);map.Released=true;}
  public override WaypointIterator Begin074110(WaypointQueue q,WaypointIterator output){Arg0=q;Arg1=output;Hit("begin");return base.Begin074110(q,output);}
  public override Displacement3 WorldVector609D98(WorldPosition position,Displacement3 output){Arg0=position;Arg1=output;Hit("vector");return Count(this,"vector")==1?VectorFirst:VectorSecond;}
  public override object Navigation(){Hit("navigation");return NavigationValue;}
  public override byte MoveObject609F08(object navigation,object owner,Displacement3 vector){Arg0=navigation;Arg1=owner;Arg2=vector;Hit("move-object");return MoveAccepted;}
  public override WorldPosition OwnerCoords(object owner,WorldPosition output){Arg0=owner;Arg1=output;Hit("owner-coords");return CoordsReturn;}
  public override object Region(WorldPosition position){Arg0=position;Hit("region");return RegionValue;}
  public override void FromWorld609DC0(WorldPosition output,Displacement3 vector,object region){Arg0=output;Arg1=vector;Arg2=region;Hit("from-world");output.Q0.Write(100);output.Q8.Write(101);output.Q16.Write(102);}
  public override void PutOnFloor609BB0(WorldPosition position){Arg0=position;Hit("floor");}
  public override void InitializePosition609720(WorldPosition position){Arg0=position;Hit("initialize-position");position.Q0.Write(0);}
 }

 static void RunAll(){
  Test("Clear zero count preserves offset",delegate{
   var f=new F();f.Queue.Offset18.Write(5);Pass(Clear(f));Equal(5UL,f.Queue.Offset18.Read(1),"No decrement loop means no offset reset");Equal(0,f.B.Calls.Count,"No allocation data touched");Equal(0UL,f.Queue.MapSize10.Read(1),"Empty map size");Same(null,f.Queue.Map08.Read(1),"Empty map pointer");
  });
  Test("Clear reverses slots and rereads map after free callback",delegate{
   var f=new F();var first=KnownNullMap(3);var replacement=KnownNullMap(3);var a=Record(2,10);var b=Record(2,20);var c=Record(2,30);first.Cell(2,1).Write(c);replacement.Cell(1,1).Write(b);replacement.Cell(0,1).Write(a);
   f.Queue.Map08.Write(first);f.Queue.MapSize10.Write(3);f.Queue.Count20.Write(2);f.Queue.Offset18.Write(6);
   f.B.On=delegate(string n,MockBoundaries p){if(n=="free-block"){Equal(0UL,f.Queue.Count20.Read(1),"Count drained before frees");Equal(0UL,f.Queue.Offset18.Read(1),"Offset reset before frees");if(Count(p,n)==1)f.Queue.Map08.Write(replacement);}};
   Pass(Clear(f));Equal(4,f.B.Freed.Count,"Three blocks and latest map");Same(c,f.B.Freed[0],"Highest original slot");Same(b,f.B.Freed[1],"Replacement slot1");Same(a,f.B.Freed[2],"Replacement slot0");Same(replacement,f.B.Freed[3],"Final map reread");Check(!first.Released,"No invented release of old captured map");Same(null,f.Queue.Map08.Read(1),"Tail reset");
  });
  Test("Clear failure retains consumed prefix and map fields",delegate{
   var f=new F();var map=KnownNullMap(2);map.Cell(0,1).Write(Record(0,1));map.Cell(1,1).Write(Record(0,2));f.Queue.Map08.Write(map);f.Queue.MapSize10.Write(2);f.Queue.Count20.Write(1);f.Queue.Offset18.Write(5);
   f.B.On=delegate(string n,MockBoundaries p){if(n=="free-block"&&Count(p,n)==2)throw new Exception("fixture");};
   Missing(Clear(f),0x073fc0);Equal(0UL,f.Queue.Count20.Read(1),"Count already drained");Equal(0UL,f.Queue.Offset18.Read(1),"Offset already reset");Equal(1,f.B.Freed.Count,"First release retained");Same(map,f.Queue.Map08.Read(1),"Map not reset after failure");Equal(2UL,f.Queue.MapSize10.Read(1),"Size not reset");Equal(0,Count(f.B,"free-map"),"Final release not reached");
  });
  Test("Grow empty map to eight initializes only explicit zero ranges",delegate{
   var f=new F();f.B.On=delegate(string n,MockBoundaries p){if(n=="allocate-map"){Equal(8UL,p.U0,"Requested slots");Equal(8u,p.Stride,"Pointer stride");}};
   Pass(Grow(f));Equal(8UL,f.Queue.MapSize10.Read(1),"Size8");var map=f.Queue.Map08.Read(1);Same(f.B.LastAllocatedMap,map,"Published allocation");for(ulong n=0;n<8;n++)Same(null,map.Cell(n,1).Read(1),"Native zeroed range");Equal(0,Count(f.B,"release-map"),"Null old map not released");Equal(0UL,f.Queue.Offset18.Read(1),"Offset retained");
  });
  Test("Grow eight to sixteen preserves ring placement and unknown cells",delegate{
   var f=new F();var old=KnownNullMap(8);var a=Record(0,1);var b=Record(0,2);var c=Record(0,3);old.Cell(6,1).Write(a);old.Cell(7,1).Write(b);old.Cell(0,1).Write(c);old.Cell(3,1).Forget();f.Queue.Map08.Write(old);f.Queue.MapSize10.Write(8);f.Queue.Offset18.Write(6);f.Queue.Count20.Write(2);
   Pass(Grow(f));var map=f.Queue.Map08.Read(1);Equal(16UL,map.Slots,"Allocation16");Equal(16UL,f.Queue.MapSize10.Read(1),"Published size");Same(a,map.Cell(6,1).Read(1),"Suffix6");Same(b,map.Cell(7,1).Read(1),"Suffix7");Same(c,map.Cell(8,1).Read(1),"Wrapped prefix moved after suffix");Unknown(map.Cell(11,1));Same(null,map.Cell(0,1).Read(1),"Leading zero range");Same(null,map.Cell(15,1).Read(1),"Trailing zero range");Equal(6UL,f.Queue.Offset18.Read(1),"Offset unchanged");Equal(2UL,f.Queue.Count20.Read(1),"Count unchanged");Check(old.Released,"Mock release called");
  });
  Test("Grow map-copy failure does not publish new map",delegate{
   var f=new F();var old=KnownNullMap(8);f.Queue.Map08.Write(old);f.Queue.MapSize10.Write(8);f.Queue.Offset18.Write(3);
   f.B.On=delegate(string n,MockBoundaries p){if(n=="copy-map"&&Count(p,n)==2)throw new Exception("fixture");};
   Missing(Grow(f),0x08f409);Same(old,f.Queue.Map08.Read(1),"Old map retained");Equal(8UL,f.Queue.MapSize10.Read(1),"Size not changed");Check(!old.Released,"Old map not released");Check(f.B.LastAllocatedMap!=null,"New allocation exists as failure prefix");
  });
  Test("Append reads capacity before offset after observed Grow callback",delegate{
   // Explicit knowledge loss at the callback boundary, not a native writer claim.
   var f=new F();f.B.UnobserveAfterGrow=true;
   Missing(Append(f,Record(0,1)),0x08f4e7);Unknown(f.Queue.MapSize10);Unknown(f.Queue.Offset18);
   Same(f.B.LastAllocatedMap,f.Queue.Map08.Read(1),"Completed Grow publication retained");Equal(0UL,f.Queue.Count20.Read(1),"No append increment after unknown capacity");Equal(0,Count(f.B,"allocate80"),"No block allocation after unknown capacity");
  });
  Test("Grow adds saved growth to live capacity after release callback",delegate{
   var f=new F();var old=KnownNullMap(8);f.Queue.Map08.Write(old);f.Queue.MapSize10.Write(8);
   f.B.On=delegate(string n,MockBoundaries p){if(n=="release-map"){
    Same(old,f.Queue.Map08.Read(1),"New map not yet published during release");Same(old,p.Arg0,"Released map identity");Equal(8UL,p.U0,"Release received prior live size");Equal(8u,p.Stride,"Pointer stride");f.Queue.MapSize10.Write(10);
   }};
   Pass(Grow(f));Same(f.B.LastAllocatedMap,f.Queue.Map08.Read(1),"New map published after callback");Equal(18UL,f.Queue.MapSize10.Read(1),"Live10 plus captured growth8");Equal(16UL,f.B.LastAllocatedMap.Slots,"Allocation request remained16");
   // This intentionally mismatched final capacity is only a callback-order
   // observation. It is not a valid container used for subsequent operations.
  });
  Test("Grow release failure preserves copied allocation without publishing it",delegate{
   var f=new F();var old=KnownNullMap(8);var retained=Record(0,20);old.Cell(2,1).Write(retained);f.Queue.Map08.Write(old);f.Queue.MapSize10.Write(8);f.Queue.Offset18.Write(2);
   f.B.On=delegate(string n,MockBoundaries p){if(n=="release-map")throw new Exception("fixture release failure");};
   Missing(Grow(f),0x08f491);Same(old,f.Queue.Map08.Read(1),"Old map still published");Equal(8UL,f.Queue.MapSize10.Read(1),"Capacity not incremented");Check(!old.Released,"Mock release did not complete");
   var allocated=f.B.LastAllocatedMap;Check(allocated!=null,"New allocation retained as failure prefix");Same(retained,allocated.Cell(2,1).Read(1),"Copy completed before release failure");Same(null,allocated.Cell(0,1).Read(1),"Zero range completed before release failure");Equal(0,f.B.Freed.Count,"No release success fabricated");
  });
  Test("Append null allocation still increments count without source dereference",delegate{
   var f=new F();var map=KnownNullMap(8);f.Queue.Map08.Write(map);f.Queue.MapSize10.Write(8);f.B.NullBlock=true;
   Pass(Append(f,null));Equal(1UL,f.Queue.Count20.Read(1),"Count increments despite null destination");Same(null,map.Cell(0,1).Read(1),"Null stored");Equal(1,Count(f.B,"allocate80"),"Allocation attempted once");
  });
  Test("Append rereads map and borrowed record after allocation callback",delegate{
   var f=new F();var old=KnownNullMap(8);var fresh=KnownNullMap(8);var borrowed=Record(0,1);borrowed.Matrix20[8].Write(4);f.Queue.Map08.Write(old);f.Queue.MapSize10.Write(8);f.Queue.Offset18.Write(3);
   f.B.On=delegate(string n,MockBoundaries p){if(n=="allocate80"){f.Queue.Map08.Write(fresh);borrowed.Kind00.Write(77);borrowed.Position08.Q0.Write(99);borrowed.Matrix20[8].Write(8);}};
   Pass(Append(f,borrowed));Same(null,old.Cell(3,1).Read(1),"Old map not written");var copied=fresh.Cell(3,1).Read(1);Same(f.B.LastAllocatedBlock,copied,"Allocation stored in fresh map");Equal(77u,copied.Kind00.Read(1),"Late kind");Equal(99UL,copied.Position08.Q0.Read(1),"Late coordinate");Equal(8.0f,copied.Matrix20[8].Read(1),"Last chunk copied");Unknown(copied.Padding04);Unknown(copied.Padding44);Unknown(copied.Auxiliary48);Equal(1UL,f.Queue.Count20.Read(1),"Count advanced");
  });
  Test("Append normalizes offset and preserves unknown source into reused block",delegate{
   var f=new F();var map=KnownNullMap(8);var existing=Record(9,9);existing.Padding04.Write(4);map.Cell(3,1).Write(existing);f.Queue.Map08.Write(map);f.Queue.MapSize10.Write(8);f.Queue.Offset18.Write(11);var borrowed=Record(2,20);
   Pass(Append(f,borrowed));Equal(3UL,f.Queue.Offset18.Read(1),"Offset masked");Same(existing,map.Cell(3,1).Read(1),"Existing block identity");Equal(2u,existing.Kind00.Read(1),"Record replaced");Unknown(existing.Padding04);Equal(0,Count(f.B,"allocate80"),"No duplicate allocation");
  });
  Test("Begin returns same output and copies proxy offset with link zero",delegate{
   var f=new F();var output=new WaypointIterator();output.Link08.Write(55);f.Queue.Offset18.Write(19);Same(output,WaypointContracts.Begin(f.Queue,output),"Hidden result identity");Same(f.Queue.Proxy00.Read(1),output.Proxy00.Read(1),"Proxy identity");Equal(19UL,output.Offset10.Read(1),"Offset copied");Equal(0UL,output.Link08.Read(1),"Link zero");f.Queue.Offset18.Forget();WaypointContracts.Begin(f.Queue,output);Unknown(output.Offset10);
  });
  Test("Next empty queue does not inspect proxy or map",delegate{
   var f=new F();f.Queue.Proxy00.Forget();f.Queue.Map08.Forget();Equal((byte)0,Next(f),"No waypoint");Equal(0,f.B.Calls.Count,"No begin or external operation");Equal((byte)7,f.Manager.Moving38.Read(1),"No invented clear");
  });
  Test("Next unknown kind consumes record without navigation",delegate{
   var f=new F();f.Seed(9,2,6);Equal((byte)0,Next(f),"Unrecognized kind");Equal(1UL,f.Queue.Count20.Read(1),"One record consumed");Equal(7UL,f.Queue.Offset18.Read(1),"Offset incremented");Equal(0,Count(f.B,"vector"),"No vector conversion");Equal((byte)7,f.Manager.Moving38.Read(1),"Moving flag unchanged");
  });
  Test("Next unobserved kind fails after consume and reset",delegate{
   var f=new F();var record=f.Seed(0,1,7);record.Kind00.Forget();Missing(AttemptNext(f),0x08da81);Equal(0UL,f.Queue.Count20.Read(1),"Count consumed");Equal(0UL,f.Queue.Offset18.Read(1),"Empty offset reset");Equal(0,Count(f.B,"vector"),"Unknown not treated as kind0");
  });
  Test("Next kind1 stops explicitly after consuming portal waypoint",delegate{
   var f=new F();f.Seed(1,1,0);Missing(AttemptNext(f),0x08dafe);Equal(0UL,f.Queue.Count20.Read(1),"Consumed portal prefix");Equal(0UL,f.Queue.Offset18.Read(1),"Reset offset");Equal(0,Count(f.B,"owner-coords"),"Unimplemented branch not faked");Equal((byte)7,f.Manager.Moving38.Read(1),"No guessed moving state");
  });
  Test("Next navigation rejection consumes waypoint and uses late owner",delegate{
   var f=new F();f.Seed(0,1,0);f.B.MoveAccepted=0;object late=new object();
   f.B.On=delegate(string n,MockBoundaries p){if(n=="navigation")f.Manager.Owner00.Write(late);if(n=="move-object"){Same(late,p.Arg1,"Owner read after singleton");Same(f.B.VectorFirst,p.Arg2,"Returned first vector identity");}};
   Equal((byte)0,Next(f),"Navigation rejected");Equal(0UL,f.Queue.Count20.Read(1),"No rollback");Equal(1,Count(f.B,"vector"),"Only first conversion");Equal(0,Count(f.B,"owner-coords"),"No coords after rejected move");Equal((byte)7,f.Manager.Moving38.Read(1),"Moving unchanged");
  });
  Test("Next success uses returned coords two vectors and copied second values despite null region",delegate{
   var f=new F();var original=f.Seed(0,1,0);object late=new object();WorldPosition localPosition=null;Displacement3 hiddenVector=null;f.B.RegionValue=null;
   f.B.On=delegate(string n,MockBoundaries p){
    if(n=="vector"){
     if(Count(p,n)==1){localPosition=(WorldPosition)p.Arg0;hiddenVector=(Displacement3)p.Arg1;Check(!Object.ReferenceEquals(original.Position08,localPosition),"Waypoint copied to local record");original.Position08.Q0.Write(999);}
     else{Same(localPosition,p.Arg0,"Same local position on second call");Same(hiddenVector,p.Arg1,"Same hidden vector buffer");Equal(10UL,localPosition.Q0.Read(1),"Local snapshot survives source mutation");}
    }
    if(n=="move-object")f.Manager.Owner00.Write(late);
    if(n=="owner-coords"){Same(late,p.Arg0,"Owner refreshed after navigation");Check(!Object.ReferenceEquals(f.B.CoordsReturn,p.Arg1),"Fixture return differs from hidden coords");}
    if(n=="region"){Same(f.View.Position08,p.Arg0,"Region of newly copied coords");Equal(30UL,f.View.Position08.Q0.Read(1),"Copied returned coordinates");f.B.VectorSecond.X0.Write(999);}
    if(n=="from-world"){Same(f.Manager.Position20,p.Arg0,"Shared manager target");Same(null,p.Arg2,"Null region still passed onward");Equal(4.0f,((Displacement3)p.Arg1).X0.Read(1),"Second vector copied before region callback");Check(!Object.ReferenceEquals(f.B.VectorSecond,p.Arg1),"Conversion gets local copy");}
    if(n=="floor"){Same(f.Manager.Position20,p.Arg0,"Floor same target");Equal((byte)7,f.Manager.Moving38.Read(1),"Flag set only after floor returns");}
   };
   Equal((byte)1,Next(f),"Normal observed move");Equal(2,Count(f.B,"vector"),"Two vector calls");Equal(32UL,f.View.Position08.Q16.Read(1),"Returned coords tail copied");Equal(100UL,f.Manager.Position20.Q0.Read(1),"FromWorld write retained");Equal((byte)1,f.Manager.Moving38.Read(1),"Success flag");
  });
  Test("Next floor failure retains consumed queue and coordinate writes",delegate{
   var f=new F();f.Seed(0,1,0);f.B.On=delegate(string n,MockBoundaries p){if(n=="floor")throw new Exception("fixture");};
   Missing(AttemptNext(f),0x08defc);Equal(0UL,f.Queue.Count20.Read(1),"Record already consumed");Equal(30UL,f.View.Position08.Q0.Read(1),"Owner coords copied");Equal(100UL,f.Manager.Position20.Q0.Read(1),"Region conversion returned");Equal((byte)7,f.Manager.Moving38.Read(1),"No success flag on failure");
  });
  Test("Move requires unique attached manager view",delegate{
   var f=new F();f.B.Attach(f.View);Same(f.View,f.B.View(f.Manager),"Same identity attachment is stable");bool rejected=false;try{f.B.Attach(new WaypointManagerView(f.Manager));}catch(MissingContract x){rejected=x.Address==0x08eaf0;}Check(rejected,"Conflicting mutable queue rejected");
   var missing=new MovementManager();Missing(MoveContracts.Run(delegate(Invocation i){f.B.MoveManager(missing,f.Destination);}),0x08eaf0);
  });
  Test("Move no-portal entry composes clear construction append and actual Next contract",delegate{
   var f=new F();f.Seed(9,1,2);Waypoint80 queued=null;
   f.B.On=delegate(string n,MockBoundaries p){
    if(n=="initialize-position")Equal(0UL,f.Queue.Count20.Read(1),"Old queue cleared before construction");
    if(n=="floor"&&Count(p,n)==1){var position=(WorldPosition)p.Arg0;Equal(80UL,position.Q0.Read(1),"Destination copied before first floor");position.Q0.Write(88);}
    if(n=="begin"){var map=f.Queue.Map08.Read(1);queued=map.Cell(0,1).Read(1);Equal(0u,queued.Kind00.Read(1),"No-portal kind0");Equal(88UL,queued.Position08.Q0.Read(1),"Floor mutation copied into waypoint");Unknown(queued.Padding04);Unknown(queued.Matrix20[0]);Unknown(queued.Auxiliary48);}
   };
   byte result=0;Pass(MoveContracts.Run(delegate(Invocation i){result=f.B.MoveManager(f.Manager,f.Destination);}));
   Equal((byte)1,result,"Observed chain result");Same(f.View,f.B.View(f.Manager),"Shared view retained");Check(queued!=null,"Produced queue record inspected");Equal(0UL,f.Queue.Count20.Read(1),"Next consumed new record");Equal(8UL,f.Queue.MapSize10.Read(1),"New map retained");Equal(1,Count(f.B,"initialize-position"),"Explicit constructor boundary");Equal(2,Count(f.B,"floor"),"Producer and consumer floor calls");Equal((byte)1,f.Manager.Moving38.Read(1),"Shared moving state");
  });
  Test("Move producer floor failure preserves cleared queue and stops before allocation",delegate{
   var f=new F();f.Seed(9,1,0);f.B.On=delegate(string n,MockBoundaries p){if(n=="floor")throw new Exception("fixture");};
   Missing(MoveContracts.Run(delegate(Invocation i){f.B.MoveManager(f.Manager,f.Destination);}),0x08ec49);Equal(0UL,f.Queue.Count20.Read(1),"Old queue remains cleared");Same(null,f.Queue.Map08.Read(1),"Old map released and cleared");Equal(0,Count(f.B,"allocate80"),"No new waypoint allocation");Equal(0,Count(f.B,"move-object"),"Navigation not reached");Equal((byte)7,f.Manager.Moving38.Read(1),"Moving flag unchanged");
  });
 }
 public static int Main(){try{RunAll();Console.WriteLine("PASS "+scenarios+" scenarios, "+assertions+" assertions; mock storage / boundary observations only, no native gameplay validation");return 0;}catch(Exception x){Console.Error.WriteLine("FAIL after "+scenarios+" scenarios / "+assertions+" assertions: "+x);return 1;}}
}
