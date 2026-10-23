using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

public static class SystemUcrtCrowdMathTests {
 static int scenarios,assertions;static readonly List<object> observations=new List<object>();
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float From(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static long Ordered(float value){uint b=Bits(value);return (b&0x80000000)!=0?0x80000000L-(b&0x7fffffff):0x80000000L+b;}
 static void Close(float actual,float expected,int ulps,string why){long distance=Math.Abs(Ordered(actual)-Ordered(expected));Check(!Single.IsNaN(actual)&&!Single.IsInfinity(actual)&&distance<=ulps,why+" ULP "+distance);observations.Add(new{name=why,actual_bits=Bits(actual),expected_bits=Bits(expected),ulp_distance=distance,allowed_ulp=ulps});}
 static void Exact(float actual,uint expected,string why){Check(Bits(actual)==expected,why+" exact bits");observations.Add(new{name=why,actual_bits=Bits(actual),expected_bits=expected});}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Check(e.Address==at,why+" exact address");return;}throw new Exception("Expected Unsupported: "+why);}
 // Independent reference only, never production. Thirty Taylor terms on
 // |x|<=9 have a mathematical next-term magnitude below 9e-25; binary64
 // evaluation/cancellation is allowed four binary32 ULP in this finite corpus.
 // No Math.Sin/Cos/Atan2 or same native function is used as the oracle.
 static float Taylor(float input,bool sine){double x=input,t=sine?x:1,sum=t;for(int n=1;n<30;n++){int a=sine?2*n:2*n-1;t*=(-x*x)/(a*(a+1));sum+=t;}return (float)sum;}
 public static int Main(){
  SystemUcrtRotationMath owner=null;SystemUcrtCrowdMath crowd=null;uint before=0,after=0;string digest=null;long calls=0;Dictionary<string,long> exports=null;
  try{
   Test("Crowd pure domains remain finite and separately bounded",delegate{
    foreach(float v in new[]{-4f,From(0x80000000),0f,4f,9f}){SystemUcrtRotationMath.RequireCrowdAngleInput(v,4);Check(true,"Crowd angle accepted");}
    foreach(float v in new[]{-Single.MaxValue,-1f,From(0x80000000),0f,From(0x00800000),Single.MaxValue}){SystemUcrtRotationMath.RequireAtan2Inputs(v,1,4);SystemUcrtRotationMath.RequireAtan2Inputs(1,v,4);Check(true,"Normal/zero atan arguments accepted");}
    foreach(uint bits in new uint[]{0x7fc00000,0x7f800000,0xff800000,1,0x80000001}){float v=From(bits);Missing(5,delegate{SystemUcrtRotationMath.RequireAtan2Inputs(v,1,5);},"Bad y");Missing(5,delegate{SystemUcrtRotationMath.RequireAtan2Inputs(1,v,5);},"Bad x");Missing(5,delegate{SystemUcrtRotationMath.RequireCrowdAngleInput(v,5);},"Bad angle");}
    Missing(6,delegate{SystemUcrtRotationMath.RequireCrowdAngleInput(From(0x41100001),6);},"Next float above9");Missing(6,delegate{SystemUcrtRotationMath.RequireCrowdAngleInput(From(0xc0800001),6);},"Next magnitude below-4");
   });
   Test("Crowd reuses one loader and verifies the extra export owner",delegate{
    owner=SystemUcrtRotationMath.OpenOwnProfile();Check(owner.ExportRvas.Count==4,"Original rotation selection still exactly four exports");var original=owner.ExportRvas;before=owner.CheckControl(0);digest=owner.VerifyLockedFingerprint(0);crowd=owner.Crowd;
    Check(Object.ReferenceEquals(crowd,owner.Crowd),"One immutable borrowed profile");Check(owner.NativeMathCalls==0,"Selecting profile invokes no trig");Check(String.Equals(owner.LoadedPath,SystemUcrtRotationMath.Library,StringComparison.OrdinalIgnoreCase),"Fixed actual module path");exports=owner.ExportRvas;Check(exports.Count==5&&exports.ContainsKey("atan2f")&&exports["atan2f"]>0,"Same-module atan2f owner and address");foreach(var old in original)Check(exports[old.Key]==old.Value,"Original export identity unchanged "+old.Key);exports["atan2f"]=0;Check(owner.ExportRvas["atan2f"]>0,"Export report is not mutable dispatch");exports=owner.ExportRvas;
   });
   Test("Actual float atan2 y-x order and independent quadrant references",delegate{
    float[,] pairs={{1,1},{1,-1},{-1,-1},{-1,1},{1,0},{-1,0},{3,4},{4,3},{Single.MaxValue,Single.MaxValue},{From(0x00800000),From(0x00800000)}};
    float[] expected={0.7853981633974483f,2.356194490192345f,-2.356194490192345f,-0.7853981633974483f,1.5707963267948966f,-1.5707963267948966f,0.6435011087932844f,0.9272952180016122f,0.7853981633974483f,0.7853981633974483f};
    for(int n=0;n<expected.Length;n++)Close(crowd.Atan2(pairs[n,0],pairs[n,1],0x25f40c),expected[n],2,"atan2 known pair "+n);
   });
   Test("Signed-zero axes have explicit own-profile requirements",delegate{
    Exact(crowd.Atan2(0,1,0x25f40c),0,"atan2(+0,+1)");Exact(crowd.Atan2(From(0x80000000),1,0x25f40c),0x80000000,"atan2(-0,+1)");Exact(crowd.Atan2(0,0,0x25f40c),0,"atan2(+0,+0)");Close(crowd.Atan2(0,-1,0x25f40c),3.141592653589793f,1,"atan2(+0,-1)");Close(crowd.Atan2(From(0x80000000),-1,0x25f40c),-3.141592653589793f,1,"atan2(-0,-1)");
   });
   float[] samples={-4,-3,-1,-.125f,.125f,1,3,4,5,6,7,8,8.976f,9};
   Test("Crowd sine beyond old rotation range has independent references",delegate{foreach(float v in samples)Close(crowd.Sin(v,0x25f495),Taylor(v,true),4,"crowd sin "+Bits(v));});
   Test("Crowd cosine beyond old rotation range has independent references",delegate{foreach(float v in samples)Close(crowd.Cos(v,0x25f483),Taylor(v,false),4,"crowd cos "+Bits(v));});
   Test("Saved three-ring seven-point sample angles fit own declared interval",delegate{
    float half=From(0x3ee5c8fb),step=From(0x3f65c8fb),maximum=0;
    foreach(float heading in new[]{-3.141592653589793f,0f,3.141592653589793f})for(int ring=0;ring<3;ring++){
     float angle=(float)((float)((ring&1)*half)+heading);for(int sample=0;sample<7;sample++){SystemUcrtRotationMath.RequireCrowdAngleInput(angle,0x25f483);Check(angle>=-4&&angle<=9,"Actual sequential sample within profile");if(angle>maximum)maximum=angle;angle=(float)(angle+step);}
    }
    Check(maximum>8.97f&&maximum<8.98f,"Evidence-derived upper sample, not normalized silently");Close(crowd.Sin(maximum,0x25f495),Taylor(maximum,true),4,"Highest sampled angle sin");Close(crowd.Cos(maximum,0x25f483),Taylor(maximum,false),4,"Highest sampled angle cos");
   });
   Test("Original rotation angle domain is unchanged after crowd selection",delegate{
    long n=owner.NativeMathCalls;Missing(0x591a30,delegate{owner.Cos(5,0x591a30);},"Old cosine stays [-4,4]");Missing(0x591a3b,delegate{owner.Sin(9,0x591a3b);},"Old sine stays [-4,4]");Check(owner.NativeMathCalls==n,"No call or profile widening");
   });
   Test("Unknown caller operands and invalid inputs stop before native math",delegate{
    long n=owner.NativeMathCalls;var unknown=new Field<float>();Missing(0x25f40c,delegate{crowd.Atan2(unknown.Read(0x25f40c),1,0x25f40c);},"Unknown y consumed by caller");Missing(0x25f40c,delegate{crowd.Atan2(1,unknown.Read(0x25f40c),0x25f40c);},"Unknown x consumed by caller");Missing(0x25f495,delegate{crowd.Sin(unknown.Read(0x25f495),0x25f495);},"Unknown angle consumed by caller");
    Missing(0x25f40c,delegate{crowd.Atan2(Single.NaN,1,0x25f40c);},"No NaN atan fallback");Missing(0x25f40c,delegate{crowd.Atan2(1,Single.PositiveInfinity,0x25f40c);},"No infinite x");Missing(0x25f40c,delegate{crowd.Atan2(From(1),1,0x25f40c);},"No subnormal y");Missing(0x25f483,delegate{crowd.Cos(10,0x25f483);},"No widened angle");Check(owner.NativeMathCalls==n,"Zero math invocations after rejected gates");
    foreach(uint bad in new uint[]{0x8011f,0x108001f,0x1f,0x8008001f})Missing(9,delegate{SystemUcrtRotationMath.RequireControlProfile(bad,9);},"Pure unsupported FP mode; actual control not written");
   });
   Test("Shared module fingerprint and query-only FP control remain stable",delegate{
    after=owner.CheckControl(0);Check(after==before,"Same control before/after corpus");Check(owner.VerifyLockedFingerprint(0)==digest&&digest==SystemUcrtRotationMath.VerifiedSha256,"Held disk identity unchanged");Check(String.Equals(owner.LoadedPath,SystemUcrtRotationMath.Library,StringComparison.OrdinalIgnoreCase),"Same actual loaded path");calls=owner.NativeMathCalls;Check(calls==45,"Exactly45 new native calls");Check(owner.ExportRvas.Count==5,"No other exported target selected");
   });
   Test("Borrowed crowd view cannot outlive its single owner",delegate{
    owner.Dispose();owner.Dispose();Missing(0x25f40c,delegate{crowd.Atan2(1,1,0x25f40c);},"Disposed binary call");Missing(0x25f483,delegate{crowd.Cos(1,0x25f483);},"Disposed cosine");Missing(0x25f495,delegate{crowd.Sin(1,0x25f495);},"Disposed sine");Missing(0,delegate{var unused=owner.Crowd;Check(unused==null,"Unreachable disposed view");},"No new borrowed view after disposal");Check(owner.NativeMathCalls==calls,"No post-disposal call");
   });
   Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scenarios,assertions,native_math_calls=calls,own_crowd_platform=true,rotation_bounds_unchanged=true,single_loader=true,library_sha256=digest,exports,control_before=before,control_after=after,control_mask_written=0,memory_image_hashed=false,original_executed=false,original_runtime_equivalence=false,observations}));return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{if(owner!=null)owner.Dispose();}
 }
}
