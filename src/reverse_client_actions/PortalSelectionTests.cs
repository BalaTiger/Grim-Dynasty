using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Bounded 08DF30 selection contract. All engine callbacks below are explicit
// observations; this is not a portal loader, pathfinder or live game adapter.
public static class PortalSelectionTests {
 static int scenarios,assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static void Missing(Outcome r,uint address){Check(!r.ReturnedNormally,"Unexpected success");Equal(address,r.Address,"Failure address");}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static int Count(Probe p,string name){int n=0;foreach(string s in p.Calls)if(s==name)n++;return n;}
 static object Select(F f){return PortalSelection.Select(f.From,f.To,f.B,new Invocation());}
 static Outcome Attempt(F f){return MoveContracts.Run(delegate(Invocation i){PortalSelection.Select(f.From,f.To,f.B,i);});}
 static Displacement3 Delta(float x,float y,float z){var d=new Displacement3();d.X0.Write(x);d.Y4.Write(y);d.Z8.Write(z);return d;}

 sealed class F {
  public readonly WorldPosition From=new WorldPosition(),To=new WorldPosition();
  public readonly object SourceRegion=new object(),DestinationRegion=new object(),Portal=new object();
  public readonly Probe B;
  public F(){
   B=new Probe(new MoveContext(),From,To);B.SourceRegion=SourceRegion;B.DestinationRegion=DestinationRegion;
   B.Portals.Add(Portal);B.Open[Portal]=1;B.Connected[Portal]=DestinationRegion;B.CountValue=1;
   B.Differences.Add(Delta(1,0,0));B.Differences.Add(Delta(2,0,0));
  }
 }
 sealed class Probe:PortalSelectionBoundaries {
  public readonly List<string> Calls=new List<string>();
  public readonly List<object> Portals=new List<object>();
  public readonly Dictionary<object,byte> Open=new Dictionary<object,byte>();
  public readonly Dictionary<object,object> Connected=new Dictionary<object,object>();
  public readonly List<Displacement3> Differences=new List<Displacement3>();
  public readonly WorldPosition From,To;
  public WorldPosition ChokeReturn=new WorldPosition();
  public object SourceRegion=null,DestinationRegion=null,Arg0=null,Arg1=null,Arg2=null;
  public int CountValue=0,Index=-1;
  public Action<string,Probe> On=null;
  public Probe(MoveContext context,WorldPosition from,WorldPosition to):base(context){From=from;To=to;}
  void Hit(string name){Calls.Add(name);if(On!=null)On(name,this);}
  public override object Region(WorldPosition position){Arg0=position;Hit("region");if(Object.ReferenceEquals(position,To))return DestinationRegion;if(Object.ReferenceEquals(position,From))return SourceRegion;throw new Exception("Unexpected position identity");}
  public override int PortalCount609F10(object region){Arg0=region;Hit("count");return CountValue;}
  public override object PortalAt609F18(object region,int index){Arg0=region;Index=index;Hit("at");return Portals[index];}
  public override byte PortalOpen609F20(object portal){Arg0=portal;Hit("open");return Open[portal];}
  public override object PortalRegion609F28(object portal){Arg0=portal;Hit("connected");return Connected[portal];}
  public override WorldPosition Choke609F30(object portal,WorldPosition hiddenReturn){Arg0=portal;Arg1=hiddenReturn;Hit("choke");return ChokeReturn;}
  public override Displacement3 DifferenceReturning609A48(WorldPosition destination,Displacement3 output,WorldPosition coordinates){Arg0=destination;Arg1=output;Arg2=coordinates;Hit("difference");return Differences[Count(this,"difference")-1];}
 }

 static void RunAll(){
  Test("destination region queried before source even when destination null",delegate{
   var f=new F();f.B.DestinationRegion=null;
   f.B.On=delegate(string n,Probe p){if(n=="region")Same(Count(p,n)==1?f.To:f.From,p.Arg0,"Native region query order");};
   Same(null,Select(f),"Null destination rejects");Equal(2,Count(f.B,"region"),"Both region queries still happen");Equal(0,Count(f.B,"count"),"No portal count");
  });
  Test("null source also rejects after both queries",delegate{
   var f=new F();f.B.SourceRegion=null;Same(null,Select(f),"Null source rejects");Equal(2,Count(f.B,"region"),"Both regions queried");Equal(0,Count(f.B,"count"),"No count");
  });
  Test("same region identity bypasses portal enumeration",delegate{
   var f=new F();f.B.DestinationRegion=f.SourceRegion;Same(null,Select(f),"Same region returns no portal");Equal(0,Count(f.B,"count"),"No count on same identity");Equal(0,Count(f.B,"at"),"No indexing");
  });
  Test("zero and signed negative portal counts do not enumerate",delegate{
   int[] counts={0,-1,Int32.MinValue};foreach(int count in counts){var f=new F();f.B.CountValue=count;Same(null,Select(f),"Nonpositive count rejects");Equal(1,Count(f.B,"count"),"Count read once");Equal(0,Count(f.B,"at"),"No unsigned reinterpretation");}
  });
  Test("first qualifying candidate wins without nearest portal search",delegate{
   var f=new F();object nearer=new object();f.B.Portals.Add(nearer);f.B.Open[nearer]=1;f.B.Connected[nearer]=f.DestinationRegion;f.B.CountValue=2;
   f.B.Differences[0]=Delta(3,0,0);f.B.Differences[1]=Delta(10,0,0);f.B.Differences.Add(Delta(1,0,0));f.B.Differences.Add(Delta(10,0,0));
   Same(f.Portal,Select(f),"Returns first qualifying portal");Equal(1,Count(f.B,"at"),"Does not inspect later nearer candidate");Equal(2,Count(f.B,"difference"),"Only first candidate distances");
  });
  Test("closed and unconnected candidates are skipped in source order",delegate{
   var f=new F();object wrong=new object(),chosen=new object();f.B.Open[f.Portal]=0;f.B.Portals.Add(wrong);f.B.Portals.Add(chosen);f.B.Open[wrong]=1;f.B.Open[chosen]=1;f.B.Connected[wrong]=new object();f.B.Connected[chosen]=f.DestinationRegion;f.B.CountValue=3;
   var indexes=new List<int>();f.B.On=delegate(string n,Probe p){if(n=="count"||n=="at")Same(f.SourceRegion,p.Arg0,"Enumerates source region");if(n=="at")indexes.Add(p.Index);if(n=="connected")Check(!Object.ReferenceEquals(f.Portal,p.Arg0),"Closed portal skips connection query");};
   Same(chosen,Select(f),"First open connected qualifying portal");Equal(3,indexes.Count,"Three candidates");Equal(0,indexes[0],"Index0");Equal(1,indexes[1],"Index1");Equal(2,indexes[2],"Index2");Equal(1,Count(f.B,"choke"),"Only eligible candidate queried");
  });
  Test("portal count is captured once across callbacks",delegate{
   var f=new F();object chosen=new object();f.B.Portals.Add(chosen);f.B.Open[f.Portal]=0;f.B.Open[chosen]=1;f.B.Connected[chosen]=f.DestinationRegion;f.B.CountValue=2;
   f.B.On=delegate(string n,Probe p){if(n=="at"&&p.Index==0)p.CountValue=0;};
   Same(chosen,Select(f),"Second index remains in captured bound");Equal(1,Count(f.B,"count"),"No count reread");Equal(2,Count(f.B,"at"),"Original count controls loop");
  });
  Test("choke return ignored but each difference return used",delegate{
   var f=new F();WorldPosition hidden=null;Displacement3 firstOutput=null;var returnedChoke=f.B.ChokeReturn;
   f.B.On=delegate(string n,Probe p){
    if(n=="choke"){Same(f.Portal,p.Arg0,"Portal identity");hidden=(WorldPosition)p.Arg1;}
    if(n=="difference"){
     Same(f.From,p.Arg2,"Original source coordinates both calls");
     if(Count(p,n)==1){Same(hidden,p.Arg0,"First difference uses hidden choke buffer");Check(!Object.ReferenceEquals(returnedChoke,p.Arg0),"Choke return deliberately ignored");firstOutput=(Displacement3)p.Arg1;}
     else{Same(f.To,p.Arg0,"Second difference uses destination");Check(!Object.ReferenceEquals(firstOutput,p.Arg1),"Distinct 12-byte output buffers");}
     Check(!Object.ReferenceEquals(p.Arg1,p.Differences[Count(p,n)-1]),"Fixture returns separate difference object");
    }
   };
   Same(f.Portal,Select(f),"Reads returned displacement despite unknown supplied output buffers");
  });
  Test("finite equal and farther choke distances fail strict comparison",delegate{
   float[] choke={2,3,1};bool[] selects={false,false,true};
   for(int k=0;k<choke.Length;k++){var f=new F();f.B.Differences[0]=Delta(choke[k],0,0);f.B.Differences[1]=Delta(2,0,0);Same(selects[k]?f.Portal:null,Select(f),"Strict squared distance comparison");}
  });
  Test("unordered distance on either side takes native JC selection",delegate{
   for(int k=0;k<3;k++){var f=new F();f.B.Differences[0]=Delta(k!=1?Single.NaN:1,0,0);f.B.Differences[1]=Delta(k!=0?Single.NaN:2,0,0);Same(f.Portal,Select(f),"NaN sets carry and selects");}
  });
  Test("binary32 addition order controls portal comparison",delegate{
   var f=new F();f.B.Differences[0]=Delta(4096,1,1);f.B.Differences[1]=Delta(1,1,4096);Same(f.Portal,Select(f),"16777216 is below16777218 after per-op float32 rounding");
   var reversed=new F();reversed.B.Differences[0]=Delta(1,1,4096);reversed.B.Differences[1]=Delta(4096,1,1);Same(null,Select(reversed),"Reversed rounded distances reject");
  });
  Test("first distance retained across second callback mutation",delegate{
   var f=new F();Displacement3 first=f.B.Differences[0];f.B.On=delegate(string n,Probe p){if(n=="difference"&&Count(p,n)==2)first.X0.Write(100);};
   Same(f.Portal,Select(f),"First squared distance1 retained, not recomputed as10000");Equal(100.0f,first.X0.Read(1),"Callback mutation is visible in shared object");
  });
  Test("candidate loop reuses stack buffer identities and partially written values",delegate{
   var f=new F();object selected=new object();f.B.Portals.Add(selected);f.B.Open[selected]=1;f.B.Connected[selected]=f.DestinationRegion;f.B.CountValue=2;
   f.B.Differences.Add(Delta(0,0,0));f.B.Differences.Add(Delta(0,0,0));WorldPosition choke=null;Displacement3 first=null,second=null;
   f.B.On=delegate(string n,Probe p){
    if(n=="choke"){
     if(choke==null){choke=(WorldPosition)p.Arg1;choke.Q0.Write(55);}
     else{Same(choke,p.Arg1,"Same choke buffer across candidates");Equal(55UL,choke.Q0.Read(1),"Prior partial choke value retained");}
    }
    if(n=="difference"){
     int k=Count(p,n);var output=(Displacement3)p.Arg1;
     if(k==1){first=output;output.X0.Write(3);output.Y4.Write(0);output.Z8.Write(0);}
     else if(k==2){second=output;output.X0.Write(2);output.Y4.Write(0);output.Z8.Write(0);}
     else if(k==3){Same(first,output,"First output reused");output.X0.Write(1);}
     else{Same(second,output,"Second output reused");Equal(2.0f,output.X0.Read(1),"Unwritten second output retains previous value");}
     p.Differences[k-1]=output;
    }
   };
   Same(selected,Select(f),"Second candidate accepted with retained buffer values");Equal(4,Count(f.B,"difference"),"Both candidate pairs evaluated");
  });
  Test("unknown returned scalars stop in native Y then X read order",delegate{
   var f=new F();f.B.Differences[0].Y4.Forget();Missing(Attempt(f),0x08e009);Equal(1,Count(f.B,"difference"),"No second callback after first unobserved scalar");
   var second=new F();second.B.Differences[1].X0.Forget();Missing(Attempt(second),0x08e036);Equal(2,Count(second.B,"difference"),"Second callback already happened");
  });
  Test("difference callback failure preserves exact boundary and candidate prefix",delegate{
   var f=new F();f.B.On=delegate(string n,Probe p){if(n=="difference")throw new InvalidOperationException("fixture");};
   Outcome r=Attempt(f);Missing(r,0x08dff8);Check(r.Exception.InnerException is InvalidOperationException,"Inner callback failure retained");Equal(1,Count(f.B,"at"),"Current candidate only");Equal(1,Count(f.B,"choke"),"Choke already returned");Equal(1,Count(f.B,"difference"),"No synthetic second distance");
  });
  Test("source-region callback failure occurs after destination query",delegate{
   var f=new F();f.B.On=delegate(string n,Probe p){if(n=="region"&&Count(p,n)==2)throw new Exception("fixture");};
   Missing(Attempt(f),0x08df56);Equal(2,Count(f.B,"region"),"Both calls entered in order");Equal(0,Count(f.B,"count"),"No enumeration after failure");
  });
 }
 public static int Main(){try{RunAll();Console.WriteLine("PASS "+scenarios+" scenarios, "+assertions+" assertions; portal-selection observations only, no native gameplay validation");return 0;}catch(Exception x){Console.Error.WriteLine("FAIL after "+scenarios+" scenarios / "+assertions+" assertions: "+x);return 1;}}
}
