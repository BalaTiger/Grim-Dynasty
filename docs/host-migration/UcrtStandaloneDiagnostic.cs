using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

// Diagnostic only. Compile with the unchanged test-only CrtReference.cs.
// No production assembly is linked, and no accepted deployment profile is changed.
class UcrtStandaloneDiagnostic {
 const string Library = "C:\\Windows\\System32\\ucrtbase.dll";
 const string ArchivedFingerprint = "3c60056371f82e4744185b6f2fa0c69042b1e78804685944132974dd13f3b6d9";
 [DllImport(Library, CallingConvention=CallingConvention.Cdecl, ExactSpelling=true)]
 static extern int _controlfp_s(out uint current, uint value, uint mask);
 static readonly List<object> fixedVectors = new List<object>();
 static int matchedVectors;
 static string Hash(byte[] bytes) { using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
 static string FileHash(string path) { using(var h=SHA256.Create()) using(var s=File.OpenRead(path)) return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "").ToLowerInvariant(); }
 static float Float(uint bits) { return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0); }
 static void Vector(string name, float actual, uint expected) {
  bool match=CrtReference.Bits(actual)==expected; if(match) matchedVectors++;
  fixedVectors.Add(new{name,comparison="exact_float_bits",actual_bits=CrtReference.Bits(actual),archived_expected_bits=expected,match});
 }
 static void Classification(string name, float actual, string expected, bool match) {
  if(match) matchedVectors++;
  fixedVectors.Add(new{name,comparison="archived_value_classification_only",actual_bits=CrtReference.Bits(actual),archived_expected_classification=expected,match});
 }
 static int Main() { try {
  if(!Environment.Is64BitProcess || Environment.OSVersion.Platform!=PlatformID.Win32NT) throw new Exception("Requires Windows x64 diagnostic process");
  string beforeHash=FileHash(Library); uint beforeControl,afterControl;
  if(_controlfp_s(out beforeControl,0,0)!=0) throw new Exception("Read-only floating control query failed");
  if((beforeControl&0x300)!=0 || (beforeControl&0x3000000)!=0 || (beforeControl&0x8001f)!=0x8001f) throw new Exception("Floating control is outside the archived nearest/masked/denormal-preserved profile; numerical calls not attempted");
  int differences=0; long maxUlp=0; object first=null; var differenceRows=new List<object>();
  byte[] nativeBits=new byte[100000*4],inputBits=new byte[100000*8];
  uint archivedWitnessNative,archivedWitnessDouble;
  using(var reference=new CrtReference()) {
   Vector("literal 1.16",reference.Literal("1.16"),1066695393u);
   Vector("literal double-rounding midpoint",reference.Literal("1.0000000596046447753906250000000001"),0x3f800000u);
   Vector("literal above midpoint",reference.Literal("1.000000059604645"),0x3f800001u);
   Vector("literal integer 16777217",reference.Literal("16777217"),0x4b800000u);
   Classification("literal invalid dot",reference.Literal("."),"equals zero; sign not specified by archived assertion",reference.Literal(".")==0f);
   Vector("literal prefix 1..2",reference.Literal("1..2"),0x3f800000u);
   Vector("powf zero to zero",reference.Power(0f,0f),0x3f800000u);
   Vector("powf negative zero odd exponent",reference.Power(Float(0x80000000u),3f),0x80000000u);
   float domain=reference.Power(-1f,0.5f),pole=reference.Power(0f,-1f);
   Classification("powf negative fractional domain",domain,"NaN; payload not specified by archived assertion",float.IsNaN(domain));
   Classification("powf zero negative exponent",pole,"positive infinity",float.IsPositiveInfinity(pole));
   float witnessX=Float(1134153466u),witnessY=Float(1080358339u);
   archivedWitnessNative=CrtReference.Bits(reference.Power(witnessX,witnessY));
   archivedWitnessDouble=CrtReference.Bits((float)Math.Pow(witnessX,witnessY));
   uint seed=1234567u;
   for(int i=0;i<100000;i++) {
    seed=unchecked(seed*1664525u+1013904223u); float x=(seed%1000000u+1u)/1000f;
    seed=unchecked(seed*1664525u+1013904223u); float y=(seed%40000u)/10000f;
    uint actual=CrtReference.Bits(reference.Power(x,y)),wide=CrtReference.Bits((float)Math.Pow(x,y));
    Buffer.BlockCopy(BitConverter.GetBytes(x),0,inputBits,i*8,4); Buffer.BlockCopy(BitConverter.GetBytes(y),0,inputBits,i*8+4,4);
    Buffer.BlockCopy(BitConverter.GetBytes(actual),0,nativeBits,i*4,4);
    if(actual!=wide) {
     differences++; maxUlp=Math.Max(maxUlp,Math.Abs((long)actual-wide));
     var row=new{index=i,x_bits=CrtReference.Bits(x),y_bits=CrtReference.Bits(y),powf_bits=actual,double_math_bits=wide};
     if(first==null) first=row;
     if(differenceRows.Count<1000) differenceRows.Add(row);
    }
   }
  }
  if(_controlfp_s(out afterControl,0,0)!=0) throw new Exception("Final read-only floating control query failed");
  string afterHash=FileHash(Library);
  var result=new {
   status="diagnostic_only",utc=DateTime.UtcNow.ToString("o"),runtime=Environment.Version.ToString(),is64BitProcess=Environment.Is64BitProcess,
   production_profile_accepted=false,production_gate="unchanged; SystemUcrtFingerprintChanged",
   library=new{path=Library,version=FileVersionInfo.GetVersionInfo(Library).FileVersion,sha256_before=beforeHash,sha256_after=afterHash,unchanged=beforeHash==afterHash,archived_verified_sha256=ArchivedFingerprint},
   floating_control=new{before=beforeControl,after=afterControl,unchanged=beforeControl==afterControl,query_mask=0,explicit_control_changes=false,private_c_locale=true,locale_freed=true},
   archived_fixed_vectors=new{source="src/rewards/CrtMathTests.cs; archived successful 56-assertion run in docs/rewards/acceptance-crt.json",total=fixedVectors.Count,matched=matchedVectors,rows=fixedVectors},
   archived_exact_pow_witness=new{x_bits=1134153466u,y_bits=1080358339u,expected_old_powf_bits=1312670141u,current_powf_bits=archivedWitnessNative,exact_native_match=archivedWitnessNative==1312670141u,expected_old_double_math_bits=1312670142u,current_double_math_bits=archivedWitnessDouble,exact_double_match=archivedWitnessDouble==1312670142u},
   same_seed_current_corpus=new{samples=100000,initial_seed=1234567,comparison="Current-system powf against current-runtime (float)Math.Pow; this is not a Win10 versus Win11 corpus comparison",powf_vs_double_math_differences=differences,maximum_positive_result_ulp_difference=maxUlp,first_difference=first,difference_rows=differenceRows,difference_rows_truncated=differences>differenceRows.Count,input_bits_sha256=Hash(inputBits),all_current_powf_bits_sha256=Hash(nativeBits),digest_encoding="IEEE754 binary32 bit patterns in little-endian sample order; inputs are x then y",archived_summary=new{samples=100000,differences=7,max_ulp=1,full_old_input_or_output_digest_available=false}},
   limitations=new[]{"No production SystemCrtMath, CrtEquation, ExperienceScaling or quest integration was executed by this diagnostic.","Old corpus retains only count, maximum ULP and first differing pair; matching aggregate metrics cannot prove all old outputs match.","Literal and edge checks cover only their named inputs; NaN payload and zero sign are not invented where archived assertions did not constrain them.","No original game executable, original installation DLL/resource, player save or Steam cloud was read or executed.","This evidence cannot whitelist the Win11 hash or convert the failed full build to passed."}
  };
  Console.WriteLine(new JavaScriptSerializer().Serialize(result));
  return beforeHash==afterHash && beforeControl==afterControl ? 0 : 2;
 } catch(Exception e) { Console.Error.WriteLine(e); return 1; } }
}
