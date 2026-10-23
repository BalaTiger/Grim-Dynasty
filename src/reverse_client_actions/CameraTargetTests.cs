using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Mock callbacks below observe the wrapper only. Separate production-helper
// cases exercise the recovered Engine leaves; neither group is native gameplay.
public static class CameraTargetTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static void Missing(Outcome r,uint address){Check(!r.ReturnedNormally,"Unexpected success");Equal(address,r.Address,"Failure address");}
 static void Pass(Outcome r){Check(r.ReturnedNormally,"Unexpected stop "+r.Address.ToString("x")+" "+r.Boundary);}
 static void Unknown<T>(Field<T> field){try{field.Read(1);throw new Exception("Unobserved field became known");}catch(MissingContract){assertions++;}}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Value(Field<uint> word){return BitConverter.ToSingle(BitConverter.GetBytes(word.Read(1)),0);}
 static void Vector(CameraVector3 v,float x,float y,float z){v.X.Write(x);v.Y.Write(y);v.Z.Write(z);}
 static void Position(CameraPosition24 p,ulong region,float x,float y,float z,uint pad){p.Q0.Write(region);p.D8.Write(Bits(x));p.DC.Write(Bits(y));p.D10.Write(Bits(z));p.D14.Write(pad);}
 static int Count(Mock b,string name){int n=0;foreach(string s in b.Calls)if(s==name)n++;return n;}
 static CameraPosition24 Get(F f){return CameraTargetContracts.GetTarget(f.State,f.Output,f.B,new Invocation());}
 static Outcome Attempt(F f){return MoveContracts.Run(delegate(Invocation i){CameraTargetContracts.GetTarget(f.State,f.Output,f.B,i);});}
 sealed class F {
  public readonly PortalCamera Camera=new PortalCamera();
  public readonly CameraTargetState State;
  public readonly CameraPosition24 Output=new CameraPosition24();
  public readonly Mock B=new Mock();
  public F(){State=new CameraTargetState(Camera);Position(State.Target120,10,1,2,3,0xdeadbeefu);Position(Output,777,91,92,93,123);Vector(State.Offset55C,10,20,30);Vector(State.Offset60,100,200,300);State.Count138.Write(0);B.Attach(State);}
 }
 sealed class Mock:CameraTargetBoundaries {
  public readonly List<string> Calls=new List<string>();
  public Action<string,Mock> On=null;
  public ulong FirstRegion=0,SecondRegion=0;
  public object Arg0,Arg1;
  public CameraVector3 OffsetReturn=new CameraVector3();
  void Hit(string name){Calls.Add(name);if(On!=null)On(name,this);}
  public override ulong GetRegion6094A0(CameraPosition24 p){Arg0=p;Hit("region");return Count(this,"region")==1?FirstRegion:SecondRegion;}
  public override void Translate609B70(CameraPosition24 p,CameraVector3 v){Arg0=p;Arg1=v;Hit("translate");}
  public override CameraVector3 GetCameraOffset60A690(PortalCamera c){Arg0=c;Hit("offset");return OffsetReturn;}
 }
 public static void Run(){
  Test("wrapper ctor writes only Q0 before first region failure",delegate{
   var f=new F();var output=new CameraPosition24();f.B.On=delegate(string n,Mock b){if(n=="region"){Same(f.State.Target120,b.Arg0,"First region receives target view");Equal(0UL,output.Q0.Read(1),"Constructor cleared only region word");Unknown(output.D8);Unknown(output.DC);Unknown(output.D10);Unknown(output.D14);throw new InvalidOperationException("mock first region");}};
   Missing(MoveContracts.Run(delegate(Invocation i){CameraTargetContracts.GetTarget(f.State,output,f.B,i);}),0x2be705);Equal(1,Count(f.B,"region"),"First call entered once");Equal(0,Count(f.B,"translate"),"No translation after failure");
  });
  Test("wrapper ctor preserves preexisting non-Q0 bytes on early failure",delegate{
   var f=new F();f.B.On=delegate(string n,Mock b){if(n=="region")throw new InvalidOperationException("mock first region");};
   Missing(Attempt(f),0x2be705);Equal(0UL,f.Output.Q0.Read(1),"Q0 cleared");Equal(91.0f,Value(f.Output.D8),"X untouched");Equal(92.0f,Value(f.Output.DC),"Y untouched");Equal(93.0f,Value(f.Output.D10),"Z untouched");Equal(123u,f.Output.D14.Read(1),"Padding untouched");
  });
  Test("zero first region still copies 24 bytes and calls second region",delegate{
   var f=new F();f.State.Target120.D14.Forget();f.State.Count138.Forget();f.B.UnitY7A8054.Forget();
   f.B.On=delegate(string n,Mock b){if(n=="region"&&Count(b,n)==2){Same(f.Output,b.Arg0,"Second region receives original output");Equal(10UL,f.Output.Q0.Read(1),"First region result does not replace copied Q0");Equal(3.0f,Value(f.Output.D10),"Tail copied before second region");Unknown(f.Output.D14);}};
   Same(f.Output,Get(f),"Original output returned");Equal(2,Count(f.B,"region"),"Both region calls occur");Equal(0,Count(f.B,"translate"),"First zero skips temporary translations");Equal(0,Count(f.B,"offset"),"Second zero skips unknown Count");Unknown(f.State.Count138);Unknown(f.B.UnitY7A8054);
  });
  Test("two translations share a temporary and copy its final 16 plus 8 bytes",delegate{
   var f=new F();f.B.FirstRegion=1;CameraPosition24 temporary=null;
   f.B.On=delegate(string n,Mock b){if(n=="translate"){
    var p=(CameraPosition24)b.Arg0;var v=(CameraVector3)b.Arg1;
    if(Count(b,n)==1){temporary=p;Check(!Object.ReferenceEquals(p,f.Output)&&!Object.ReferenceEquals(p,f.State.Target120),"Temporary distinct from both views");Equal(0.0f,v.X.Read(1),"Explicit unit X zero");Equal(1.0f,v.Y.Read(1),"Observed unit Y");Equal(0.0f,v.Z.Read(1),"Explicit unit Z zero");Equal(0xdeadbeefu,p.D14.Read(1),"Temporary includes tail padding");p.D8.Write(Bits(11));p.D14.Write(456);f.State.Target120.DC.Write(Bits(800));}
    else{Same(temporary,p,"Second translation uses same temporary");Same(f.State.Offset55C,v,"Second vector is inline offset identity");Equal(2.0f,Value(p.DC),"Source mutation did not rewrite temporary snapshot");p.DC.Write(Bits(22));p.D10.Write(Bits(33));p.Q0.Write(99);}
    Equal(0UL,f.Output.Q0.Read(1),"Output still ctor prefix during both translations");Equal(91.0f,Value(f.Output.D8),"Output payload not published early");
   }};
   Same(f.Output,Get(f),"Original output returned");Equal(2,Count(f.B,"translate"),"Two temporary translations");Equal(99UL,f.Output.Q0.Read(1),"Final temporary token copied");Equal(11.0f,Value(f.Output.D8),"X copied");Equal(22.0f,Value(f.Output.DC),"Y copied");Equal(33.0f,Value(f.Output.D10),"Tail Z copied");Equal(456u,f.Output.D14.Read(1),"Tail padding copied");
  });
  Test("second region callback determines late signed Count and final borrowed offset",delegate{
   var f=new F();f.B.SecondRegion=1;f.State.Count138.Write(-1);var returned=new CameraVector3();Vector(returned,4,5,6);f.B.OffsetReturn=returned;
   f.B.On=delegate(string n,Mock b){if(n=="region"&&Count(b,n)==2)f.State.Count138.Write(1);if(n=="offset"){Same(f.Camera,b.Arg0,"Offset getter receiver identity");f.State.Count138.Write(-100);returned.Y.Write(55);}if(n=="translate"){Same(f.Output,b.Arg0,"Final translation writes output");Same(returned,b.Arg1,"Borrowed offset returned pointer used directly");Equal(55.0f,((CameraVector3)b.Arg1).Y.Read(1),"Getter callback mutation visible");}};
   Same(f.Output,Get(f),"Return identity");Equal(1,Count(f.B,"offset"),"Positive late Count enters getter");Equal(1,Count(f.B,"translate"),"Count not reread after getter");
  });
  Test("nonpositive signed Count skips offset while positive Count enters",delegate{
   int[] counts={Int32.MinValue,-1,0,1};foreach(int count in counts){var f=new F();f.B.SecondRegion=1;f.State.Count138.Write(count);Same(f.Output,Get(f),"Return identity");Equal(count>0?1:0,Count(f.B,"offset"),"Signed branch for "+count);Equal(count>0?1:0,Count(f.B,"translate"),"Final translation branch for "+count);}
  });
  Test("unknown late Count stops after output copy and both region calls",delegate{
   var f=new F();f.B.SecondRegion=1;f.State.Count138.Forget();Missing(Attempt(f),0x2be798);Equal(2,Count(f.B,"region"),"Both region calls returned");Equal(10UL,f.Output.Q0.Read(1),"Output token copy retained");Equal(3.0f,Value(f.Output.D10),"Output tail retained");Equal(0xdeadbeefu,f.Output.D14.Read(1),"Padding copy retained");Equal(0,Count(f.B,"offset"),"No guessed Count");
  });
  Test("either temporary translation failure leaves output at constructor prefix",delegate{
   for(int failureCall=1;failureCall<=2;failureCall++){var f=new F();f.B.FirstRegion=1;CameraPosition24 partial=null;int wanted=failureCall;
    f.B.On=delegate(string n,Mock b){if(n=="translate"){partial=(CameraPosition24)b.Arg0;partial.D8.Write(Bits(44));if(Count(b,n)==wanted)throw new InvalidOperationException("mock temporary translation");}};
    Missing(Attempt(f),failureCall==1?0x2be752u:0x2be764u);Equal(44.0f,Value(partial.D8),"Temporary mutation retained");Equal(0UL,f.Output.Q0.Read(1),"No temporary token copied");Equal(91.0f,Value(f.Output.D8),"Output payload unchanged");Equal(123u,f.Output.D14.Read(1),"Output tail unchanged");Equal(1,Count(f.B,"region"),"Second region not reached");
   }
  });
  Test("offset and final translation failures retain published output prefix",delegate{
   var f=new F();f.B.SecondRegion=1;f.State.Count138.Write(1);f.B.On=delegate(string n,Mock b){if(n=="offset")throw new InvalidOperationException("mock offset");};Missing(Attempt(f),0x2be7a4);Equal(10UL,f.Output.Q0.Read(1),"Output already copied before getter");Equal(0,Count(f.B,"translate"),"No final translation");
   var g=new F();g.B.SecondRegion=1;g.State.Count138.Write(1);var failure=new InvalidOperationException("mock final translate");g.B.On=delegate(string n,Mock b){if(n=="translate"){((CameraPosition24)b.Arg0).D8.Write(Bits(77));throw failure;}};
   Outcome result=Attempt(g);Missing(result,0x2be7b0);Same(failure,result.Exception.InnerException,"Original callback exception retained");Equal(77.0f,Value(g.Output.D8),"Final partial X retained");Equal(2.0f,Value(g.Output.DC),"Y remains pre-call copy");Equal(3.0f,Value(g.Output.D10),"Z remains pre-call copy");Equal(0xdeadbeefu,g.Output.D14.Read(1),"Padding retained");
  });
  Test("unknown unit constant stops before temporary translation or output publication",delegate{
   var f=new F();f.B.FirstRegion=1;f.B.UnitY7A8054.Forget();Missing(Attempt(f),0x2be72f);Equal(0,Count(f.B,"translate"),"No invented unit constant");Equal(0UL,f.Output.Q0.Read(1),"Constructor prefix retained");Equal(91.0f,Value(f.Output.D8),"Output payload untouched");Equal(1,Count(f.B,"region"),"Only first region reached");
  });
  Test("same-address output aliases constructor while independent output does not",delegate{
   var camera=new PortalCamera();var state=new CameraTargetState(camera);Position(state.Target120,10,1,2,3,17);Vector(state.Offset55C,10,20,30);state.Count138.Write(0);var b=new CameraTargetBoundaries();var independent=new CameraPosition24();
   Same(independent,CameraTargetContracts.GetTarget(state,independent,b,new Invocation()),"Independent output returned");Equal(10UL,state.Target120.Q0.Read(1),"Independent constructor cannot clear target token");Equal(11.0f,Value(independent.D8),"Nonzero region takes translation branch");Equal(23.0f,Value(independent.DC),"Unit and inline offsets applied");Equal(33.0f,Value(independent.D10),"Z inline offset applied");
   var alias=new CameraPosition24(state.Target120);Same(state.Target120.AddressIdentity,alias.AddressIdentity,"Explicit same-address identity");Same(alias,CameraTargetContracts.GetTarget(state,alias,b,new Invocation()),"Exact alias wrapper returned");Equal(0UL,state.Target120.Q0.Read(1),"Aliased constructor clears source region before query");Equal(1.0f,Value(alias.D8),"Zero region skips translations for aliased X");Equal(2.0f,Value(alias.DC),"Aliased Y unchanged");Equal(3.0f,Value(alias.D10),"16 plus 8 self-copy retains tail");Equal(17u,alias.D14.Read(1),"Padding retained");
  });
  Test("production GetRegion reads only Q0 and never fabricates unknown region",delegate{
   var b=new CameraTargetBoundaries();var p=new CameraPosition24();p.Q0.Write(0xfedcba9876543210UL);Equal(0xfedcba9876543210UL,b.GetRegion6094A0(p),"Full-width token returned");Unknown(p.D8);Unknown(p.D14);p.Q0.Forget();Missing(MoveContracts.Run(delegate(Invocation i){b.GetRegion6094A0(p);}),0x22c0b0);
  });
  Test("production Translate performs binary32 additions and preserves token and padding",delegate{
   var b=new CameraTargetBoundaries();var p=new CameraPosition24();Position(p,987,16777216.0f,2,3,0xabcdef01u);var v=new CameraVector3();Vector(v,1,20,30);b.Translate609B70(p,v);
   Equal(16777216.0f,Value(p.D8),"X rounds at binary32 store");Equal(22.0f,Value(p.DC),"Y is parameter plus prior Y");Equal(33.0f,Value(p.D10),"Z is parameter plus prior Z");Equal(987UL,p.Q0.Read(1),"Region token untouched");Equal(0xabcdef01u,p.D14.Read(1),"Fourth DWORD untouched");
   p.D14.Forget();p.D8.Write(Bits(Single.NaN));v.Y.Write(Single.PositiveInfinity);b.Translate609B70(p,v);Check(Single.IsNaN(Value(p.D8)),"NaN is not sanitized");Check(Single.IsPositiveInfinity(Value(p.DC)),"Infinity is not clamped");Unknown(p.D14);Equal(987UL,p.Q0.Read(1),"Special values leave region unchanged");
  });
  Test("production Translate unknown X operands fail before any component write",delegate{
   var b=new CameraTargetBoundaries();for(int input=0;input<2;input++){var p=new CameraPosition24();Position(p,3,1,2,3,9);var v=new CameraVector3();Vector(v,10,20,30);if(input==0)p.D8.Forget();else v.X.Forget();Missing(MoveContracts.Run(delegate(Invocation i){b.Translate609B70(p,v);}),input==0?0x22c5d0u:0x22c5d5u);if(input==0)Unknown(p.D8);else Equal(1.0f,Value(p.D8),"Known X not stored before missing parameter");Equal(2.0f,Value(p.DC),"Y untouched");Equal(3.0f,Value(p.D10),"Z untouched");}
  });
  Test("production Translate unknown Y operands preserve only X prefix",delegate{
   var b=new CameraTargetBoundaries();for(int input=0;input<2;input++){var p=new CameraPosition24();Position(p,3,1,2,3,9);var v=new CameraVector3();Vector(v,10,20,30);if(input==0)v.Y.Forget();else p.DC.Forget();Missing(MoveContracts.Run(delegate(Invocation i){b.Translate609B70(p,v);}),input==0?0x22c5deu:0x22c5e3u);Equal(11.0f,Value(p.D8),"X store retained");if(input==0)Equal(2.0f,Value(p.DC),"Y not stored before missing vector");else Unknown(p.DC);Equal(3.0f,Value(p.D10),"Z untouched");Equal(9u,p.D14.Read(1),"Padding retained");}
  });
  Test("production Translate unknown Z operands preserve X and Y prefix",delegate{
   var b=new CameraTargetBoundaries();for(int input=0;input<2;input++){var p=new CameraPosition24();Position(p,3,1,2,3,9);var v=new CameraVector3();Vector(v,10,20,30);if(input==0)v.Z.Forget();else p.D10.Forget();Missing(MoveContracts.Run(delegate(Invocation i){b.Translate609B70(p,v);}),input==0?0x22c5edu:0x22c5f2u);Equal(11.0f,Value(p.D8),"X retained");Equal(22.0f,Value(p.DC),"Y retained");if(input==0)Equal(3.0f,Value(p.D10),"Z not stored before missing vector");else Unknown(p.D10);Equal(9u,p.D14.Read(1),"Padding retained");}
  });
  Test("production offset getter returns same live inline vector without initializing it",delegate{
   var camera=new PortalCamera();var state=new CameraTargetState(camera);var b=new CameraTargetBoundaries();b.Attach(state);var borrowed=b.GetCameraOffset60A690(camera);Same(state.Offset60,borrowed,"Inline plus60 view identity");Unknown(borrowed.X);Unknown(borrowed.Y);Unknown(borrowed.Z);state.Offset60.Y.Write(7);Equal(7.0f,borrowed.Y.Read(1),"Later writes visible through borrowed view");Check(!Object.ReferenceEquals(state.Offset55C,borrowed),"Distinct Game inline offset not conflated");
  });
  Test("production wrapper integrates both temporary translations and final camera offset",delegate{
   var f=new F();var b=new CameraTargetBoundaries();b.Attach(f.State);f.State.Count138.Write(1);f.State.Target120.D14.Forget();Same(f.Output,CameraTargetContracts.GetTarget(f.State,f.Output,b,new Invocation()),"Original output returned");Equal(10UL,f.Output.Q0.Read(1),"Production region token preserved");Equal(111.0f,Value(f.Output.D8),"X plus both offsets");Equal(223.0f,Value(f.Output.DC),"Y plus unit and both offsets");Equal(333.0f,Value(f.Output.D10),"Z plus both offsets");Unknown(f.Output.D14);Equal(1.0f,Value(f.State.Target120.D8),"Original source remains unchanged");
  });
  Test("production helper failures preserve temporary versus final-output prefixes",delegate{
   var f=new F();var b=new CameraTargetBoundaries();f.State.Target120.DC.Forget();Missing(MoveContracts.Run(delegate(Invocation i){CameraTargetContracts.GetTarget(f.State,f.Output,b,i);}),0x22c5e3);Equal(0UL,f.Output.Q0.Read(1),"Failed temporary never published");Equal(91.0f,Value(f.Output.D8),"Original output payload retained");
   var g=new F();var c=new CameraTargetBoundaries();c.Attach(g.State);g.State.Count138.Write(1);g.State.Offset60.Z.Forget();Missing(MoveContracts.Run(delegate(Invocation i){CameraTargetContracts.GetTarget(g.State,g.Output,c,i);}),0x22c5ed);Equal(111.0f,Value(g.Output.D8),"Final output X prefix applied");Equal(223.0f,Value(g.Output.DC),"Final output Y prefix applied");Equal(33.0f,Value(g.Output.D10),"Final Z retains prior temporary result");Equal(0xdeadbeefu,g.Output.D14.Read(1),"Padding remains exact");
  });
  Test("declared arbitrary overlap remains an explicit own-domain stop",delegate{
   var f=new F();f.Output.DeclareUnsupportedOverlap();Missing(Attempt(f),0);Equal(0,Count(f.B,"region"),"Unsupported overlap cannot masquerade as native copy validation");Equal(0,Count(f.B,"translate"),"No speculative overlap translation");
  });
 }
}
