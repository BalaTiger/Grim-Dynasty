using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

public static class SystemUcrtRotationMathTests {
 static int scenarios,assertions;
 static readonly List<object> observations=new List<object>();
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
 static float From(uint x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Check(e.Address==at,why+" exact caller stop");return;}throw new Exception("Expected Unsupported: "+why);}
 static void Exact(float actual,uint expected,string why){Check(Bits(actual)==expected,why+" exact binary32");observations.Add(new {name=why,actual_bits=Bits(actual),expected_bits=expected});}
 static long Ordered(float f){uint b=Bits(f);return (b&0x80000000u)!=0?0x80000000L-(b&0x7fffffffu):0x80000000L+b;}
 static void Close(float actual,float expected,long maxUlp,string why){
  long distance=Math.Abs(Ordered(actual)-Ordered(expected));Check(!Single.IsNaN(actual)&&!Single.IsInfinity(actual)&&distance<=maxUlp,why+" independent reference ULP distance "+distance);
  observations.Add(new {name=why,actual_bits=Bits(actual),expected_bits=Bits(expected),ulp_distance=distance,allowed_ulp=maxUlp});
 }
 // Independent polynomial reference, never used by production. On |x|<=4,
 // 20 sine/cosine terms leave a mathematical remainder below 2e-24.
 // Binary64 evaluation is allowed a two-binary32-ULP acceptance tolerance;
 // this is a small finite fixture, not an all-input UCRT accuracy claim.
 static float Taylor(float input,bool sine){
  double x=input,term=sine?x:1.0,sum=term;
  for(int n=1;n<20;n++){int a=sine?2*n:2*n-1;term*=(-x*x)/(a*(a+1));sum+=term;}
  return (float)sum;
 }
 public static int Main(){
  SystemUcrtRotationMath math=null;uint before=0,after=0;string digest=null;Dictionary<string,long> exports=null;long nativeCalls=0;
  try{
   Test("Fingerprint gate preserves old profile rejection",delegate{
    SystemUcrtRotationMath.RequireFingerprint(SystemUcrtRotationMath.VerifiedSha256,0);
    Missing(0,delegate{SystemUcrtRotationMath.RequireFingerprint("3c60056371f82e4744185b6f2fa0c69042b1e78804685944132974dd13f3b6d9",0);},"Old rewards profile is not silently accepted");
    Missing(7,delegate{SystemUcrtRotationMath.RequireFingerprint(null,7);},"Unknown fingerprint");
   });
   Test("Pure control gates reject nondefault modes without setting the actual control word",delegate{
    SystemUcrtRotationMath.RequireControlProfile(0x8001f,0);Check(true,"nearest masked preserved accepted");
    foreach(uint bad in new uint[]{0x8011f,0x8021f,0x8031f,0x108001f,0x208001f,0x308001f,0x8000f,0x1f,0x8008001f})
     Missing(9,delegate{SystemUcrtRotationMath.RequireControlProfile(bad,9);},"Different control profile");
   });
   Test("Pure finite domain gates reject NaN infinity subnormal and range errors",delegate{
    foreach(float good in new float[]{-1f,-.5f,From(0x80000000),0,.5f,1f}){SystemUcrtRotationMath.RequireAcosInput(good,0);Check(true,"Valid inverse domain");}
    foreach(float good in new float[]{-4f,-1f,From(0x80000000),0,1f,4f}){SystemUcrtRotationMath.RequireAngleInput(good,0);Check(true,"Valid angle domain");}
    foreach(uint bits in new uint[]{0x7fc00000,0x7f800001,0x7f800000,0xff800000,1,0x80000001,0x007fffff}){
     float value=From(bits);Missing(0x5e334,delegate{SystemUcrtRotationMath.RequireAcosInput(value,0x5e334);},"Unsupported inverse special input");
     Missing(0x591a30,delegate{SystemUcrtRotationMath.RequireAngleInput(value,0x591a30);},"Unsupported angle special input");
    }
    Missing(0x5e334,delegate{SystemUcrtRotationMath.RequireAcosInput(From(0x3f800001),0x5e334);},"Just beyond acos upper bound");
    Missing(0x5e334,delegate{SystemUcrtRotationMath.RequireAcosInput(From(0xbf800001),0x5e334);},"Just beyond acos lower bound");
    Missing(0x591a3b,delegate{SystemUcrtRotationMath.RequireAngleInput(From(0x40800001),0x591a3b);},"Just beyond angle upper bound");
   });
   Test("Fixed system source and four actual export owners resolve",delegate{
    math=SystemUcrtRotationMath.OpenOwnProfile();
    Check(String.Equals(math.LoadedPath,SystemUcrtRotationMath.Library,StringComparison.OrdinalIgnoreCase),"Actual loaded absolute module path");
    Check(!math.IsDisposed&&math.NativeMathCalls==0,"Opening performs no trigonometry");
    exports=math.ExportRvas;Check(exports.Count==4,"Exactly three math exports and one read-only control export");
    foreach(string name in new[]{"acosf","cosf","sinf","_controlfp_s"})Check(exports.ContainsKey(name)&&exports[name]>0,"Same-module actual export "+name);
    var copy=math.ExportRvas;copy["acosf"]=0;Check(math.ExportRvas["acosf"]==exports["acosf"],"Export report cannot alter callable ownership");
    before=math.CheckControl(0);digest=math.VerifyLockedFingerprint(0);Check(digest==SystemUcrtRotationMath.VerifiedSha256,"Held source fingerprint");
   });
   Test("Acos endpoints and elementary angles use independent known references",delegate{
    Exact(math.Acos(1f,0x5e334),0,"acos(1)");
    Close(math.Acos(0f,0x5e334),1.57079632679489661923f,1,"acos(0)=pi/2");
    Close(math.Acos(-1f,0x5e334),3.14159265358979323846f,1,"acos(-1)=pi");
    Close(math.Acos(.5f,0x5e334),1.04719755119659774615f,1,"acos(1/2)=pi/3");
    Close(math.Acos(-.5f,0x5e334),2.09439510239319549231f,1,"acos(-1/2)=2pi/3");
   });
   Test("Observed signed-zero profile is explicit",delegate{
    Exact(math.Sin(0f,0x591a3b),0,"sin(+0)");
    Exact(math.Sin(From(0x80000000),0x591a3b),0x80000000,"sin(-0)");
    Exact(math.Cos(0f,0x591a30),0x3f800000,"cos(+0)");
    Exact(math.Cos(From(0x80000000),0x591a30),0x3f800000,"cos(-0)");
   });
   float[] samples={-4f,-3f,-2f,-1f,-.5f,-.0625f,.0625f,.5f,1f,2f,3f,4f};
   Test("Sine finite samples use independent polynomial reference",delegate{
    foreach(float value in samples)Close(math.Sin(value,0x591a3b),Taylor(value,true),2,"sin input bits "+Bits(value));
   });
   Test("Cosine finite samples use independent polynomial reference",delegate{
    foreach(float value in samples)Close(math.Cos(value,0x591a30),Taylor(value,false),2,"cos input bits "+Bits(value));
   });
   Test("Live input failures perform zero additional math calls",delegate{
    long count=math.NativeMathCalls;
    Missing(0x5e334,delegate{math.Acos(2f,0x5e334);},"Acos domain error rejected before native call");
    Missing(0x591a30,delegate{math.Cos(Single.NaN,0x591a30);},"NaN rejected before native call");
    Missing(0x591a3b,delegate{math.Sin(Single.PositiveInfinity,0x591a3b);},"Infinity rejected before native call");
    Missing(0x591a3b,delegate{math.Sin(From(1),0x591a3b);},"Subnormal rejected before native call");
    Missing(0x591a30,delegate{math.Cos(5,0x591a30);},"Large finite angle rejected before native call");
    Check(math.NativeMathCalls==count,"No math call on failed input gates");
   });
   Test("Source and queried control are unchanged after the finite corpus",delegate{
    after=math.CheckControl(0);Check(before==after,"No control-word change");
    Check(math.VerifyLockedFingerprint(0)==digest,"Same locked system disk-file hash");
    nativeCalls=math.NativeMathCalls;Check(nativeCalls==33,"Exactly 33 native float math calls");
   });
   Test("Disposed provider stops and cannot reuse module delegates",delegate{
    math.Dispose();math.Dispose();Check(math.IsDisposed,"Idempotent disposal");
    Missing(0x5e334,delegate{math.Acos(1,0x5e334);},"Disposed inverse provider");
    Missing(0x591a30,delegate{math.Cos(0,0x591a30);},"Disposed cosine provider");
    Check(math.NativeMathCalls==nativeCalls,"No native math after disposal");
   });
   Console.WriteLine(new JavaScriptSerializer().Serialize(new{
    status="passed",scenarios,assertions,own_platform_adapter=true,library=SystemUcrtRotationMath.Library,
    library_sha256=digest,library_version=SystemUcrtRotationMath.VerifiedFileVersion,exports,
    control_before=before,control_after=after,control_mask_written=0,native_math_calls=nativeCalls,observations,
    original_read=false,original_executed=false,memory_image_hashed=false,native_game_equivalence=false,full_rotation_integrated=false,old_suites_rerun=false,
    scope="Fixed current System32 UCRT finite float adapter only. Numeric fixture is independent of the production entrypoints; no CRT internals or original game execution. Signed zero is a measured own-profile requirement. No errno/sticky-status equivalence."}));
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{if(math!=null)math.Dispose();}
 }
}
