using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// New opt-in Camera profile only. The actual system float tanf is used by
// production; the finite test reference below is independent and never injected.
// The existing eleven rotation scenarios are separate affected-loader regression.
public static class SystemUcrtCameraMathTests {
 static int scenarios,assertions;
 static readonly List<object> observations=new List<object>();
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float From(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static long Ordered(float value){uint b=Bits(value);return (b&0x80000000u)!=0?0x80000000L-(b&0x7fffffffu):0x80000000L+b;}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Check(error.Address==at,why+" exact address");return;}throw new Exception("Expected Unsupported: "+why);}
 static void Exact(float actual,uint expected,string why){Check(Bits(actual)==expected,why+" exact bits");observations.Add(new{name=why,actual_bits=Bits(actual),expected_bits=expected});}
 static void Close(float actual,float expected,string why){long ulp=Math.Abs(Ordered(actual)-Ordered(expected));Check(!Single.IsNaN(actual)&&!Single.IsInfinity(actual)&&ulp<=2,why+" ULP distance "+ulp);observations.Add(new{name=why,actual_bits=Bits(actual),expected_bits=Bits(expected),ulp_distance=ulp,allowed_ulp=2});}
 // Independent finite reference: eighteen binary64 Taylor terms for sine and
 // cosine over |x|<=1.5, then their ratio. The cosine stays above0.07 here.
 // A two-binary32-ULP fixture tolerance is not an all-input CRT accuracy claim.
 // No Math.Tan/Sin/Cos or native math call supplies this reference.
 static float Reference(float value){
  double x=value,sine=x,cosine=1,s=x,c=1;
  for(int n=1;n<18;n++){s*=(-x*x)/((2*n)*(2*n+1));c*=(-x*x)/((2*n-1)*(2*n));sine+=s;cosine+=c;}
  return (float)(sine/cosine);
 }
 public static int Main(){
  SystemUcrtRotationMath owner=null;SystemUcrtCameraMath camera=null;
  uint before=0,after=0;string digest=null;long calls=0;Dictionary<string,long> exports=null;
  try{
   Test("Camera half-FOV domain has explicit finite endpoints and rejects pole-side inputs",delegate{
    foreach(float good in new[]{-1.5f,-.5f,From(0x80000000),0f,.5f,1.5f}){SystemUcrtRotationMath.RequireCameraTangentInput(good,0x044a57);Check(true,"Accepted own finite tangent domain");}
    foreach(uint bits in new uint[]{0x3fc00001,0xbfc00001,0x7fc00000,0x7f800001,0x7f800000,0xff800000,1,0x80000001,0x007fffff}){
     float bad=From(bits);Missing(0x044a57,delegate{SystemUcrtRotationMath.RequireCameraTangentInput(bad,0x044a57);},"Rejected own domain before native call");
    }
   });
   Test("Camera selection adds exactly one verified export to the unchanged default owner",delegate{
    owner=SystemUcrtRotationMath.OpenOwnProfile();var original=owner.ExportRvas;
    Check(original.Count==4&&!original.ContainsKey("tanf")&&!original.ContainsKey("atan2f"),"Default loader still selects only four original exports");
    before=owner.CheckControl(0);digest=owner.VerifyLockedFingerprint(0);camera=owner.Camera;
    Check(Object.ReferenceEquals(camera,owner.Camera),"Camera view is stable and borrowed");
    Check(owner.NativeMathCalls==0,"Opt-in binding invokes no trigonometry");
    exports=owner.ExportRvas;Check(exports.Count==5&&exports.ContainsKey("tanf")&&exports["tanf"]>0&&!exports.ContainsKey("atan2f"),"Camera binds actual same-owner tanf only");
    foreach(var entry in original)Check(exports[entry.Key]==entry.Value,"Original export address retained "+entry.Key);
    exports["tanf"]=0;Check(owner.ExportRvas["tanf"]>0,"Report copy cannot replace native delegate");exports=owner.ExportRvas;
   });
   Test("Actual tanf preserves signed zero and agrees with independent finite references",delegate{
    Exact(camera.Tan(0,0x044a57),0,"tanf(+0)");Exact(camera.Tan(From(0x80000000),0x044a57),0x80000000,"tanf(-0)");
    foreach(float value in new[]{-1.5f,-1f,-.5f,.5f,1f,1.5f})Close(camera.Tan(value,0x044a57),Reference(value),"tanf input bits "+Bits(value));
    Check(owner.NativeMathCalls==8,"Exactly eight real float tangent calls");
   });
   Test("Unknown caller operand and live invalid inputs stop without a successful native fallback",delegate{
    long count=owner.NativeMathCalls;var unknown=new Field<float>();
    Missing(0x044a57,delegate{camera.Tan(unknown.Read(0x044a57),0x044a57);},"Unknown half-FOV remains unknown");
    foreach(float bad in new[]{Single.NaN,Single.PositiveInfinity,From(1),From(0x3fc00001),-2f})Missing(0x044a57,delegate{camera.Tan(bad,0x044a57);},"Rejected input has no fallback");
    Check(owner.NativeMathCalls==count,"Rejected inputs invoke no native tanf");
   });
   Test("Camera and Crowd selections coexist without changing original rotation domains",delegate{
    long count=owner.NativeMathCalls;long tanAddress=owner.ExportRvas["tanf"];var crowd=owner.Crowd;
    Check(Object.ReferenceEquals(camera,owner.Camera)&&Object.ReferenceEquals(crowd,owner.Crowd),"One stable view per opt-in profile");
    exports=owner.ExportRvas;Check(exports.Count==6&&exports.ContainsKey("atan2f")&&exports["tanf"]==tanAddress,"Crowd adds only atan2f to the same module");
    Missing(0x05e334,delegate{owner.Acos(2,0x05e334);},"Original inverse bounds unchanged");
    Missing(0x591a30,delegate{owner.Cos(5,0x591a30);},"Original cosine bounds unchanged");
    Missing(0x591a3b,delegate{owner.Sin(9,0x591a3b);},"Original sine bounds unchanged");
    Missing(0x25f495,delegate{crowd.Sin(10,0x25f495);},"Crowd bounds unchanged");
    Check(owner.NativeMathCalls==count,"Opt-in selection and rejected existing calls add no native math");
   });
   Test("Shared locked source and query-only control remain stable after new finite corpus",delegate{
    after=owner.CheckControl(0);Check(after==before,"No control word changed");
    Check(owner.VerifyLockedFingerprint(0)==digest&&digest==SystemUcrtRotationMath.VerifiedSha256,"Same locked system disk bytes");
    Check(String.Equals(owner.LoadedPath,SystemUcrtRotationMath.Library,StringComparison.OrdinalIgnoreCase),"Same fixed loaded path");
    calls=owner.NativeMathCalls;Check(calls==8,"Only new tangent corpus executed");
   });
   Test("Borrowed Camera view cannot call after its owner is disposed",delegate{
    owner.Dispose();owner.Dispose();Check(owner.IsDisposed,"Owner disposal is idempotent");
    Missing(0x044a57,delegate{camera.Tan(.5f,0x044a57);},"Disposed Camera call");
    Missing(0,delegate{var selected=owner.Camera;Check(selected==null,"Unreachable disposed view");},"Disposed Camera selection");
    Check(owner.NativeMathCalls==calls,"No native math after disposal");
   });
   Console.WriteLine(new JavaScriptSerializer().Serialize(new{
    status="passed",scenarios,assertions,native_math_calls=calls,own_camera_platform=true,single_loader=true,
    default_exports_unchanged=true,rotation_bounds_unchanged=true,library_sha256=digest,exports,
    control_before=before,control_after=after,control_mask_written=0,half_fov_domain=new[]{-1.5,1.5},observations,
    original_read=false,original_executed=false,memory_image_hashed=false,original_runtime_equivalence=false,old_suites_rerun=false
   }));return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}finally{if(owner!=null)owner.Dispose();}
 }
}
