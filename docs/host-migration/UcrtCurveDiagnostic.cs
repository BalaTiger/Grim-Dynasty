using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;

// Standalone finite-domain diagnostic. References only own Resources.dll and the
// unchanged test-only CrtReference.cs; never links Rewards.dll or changes its gate.
class UcrtCurveDiagnostic {
 const string Library="C:\\Windows\\System32\\ucrtbase.dll";
 const string RecordName="records/creatures/pc/playerlevels.dbr";
 const string ExpectedExpression="fca2d52130c957f3c3cf2de30e7d17bbfe31422d59f7e887c1d3e1e60f0ab099";
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]
 static extern int _controlfp_s(out uint current,uint value,uint mask);
 sealed class CurveCase {
  public string archive,expected_archive_sha256,expected_record_body_sha256,expected_output_sha256;
  public uint expected_maximum_level;
  public string archive_sha256_before,archive_sha256_after,record_body_sha256,expression_sha256,output_sha256;
  public bool archive_matches_old,archive_unchanged,record_matches_old,expression_matches_old,maximum_level_matches_old,output_matches_old;
  public uint maximum_level,compared_levels;
  public string[] coefficient_tokens;
  public uint[] coefficient_bits,current_float_bits;
  public uint first_threshold,second_threshold;
 }
 static string FileHash(string path){using(var h=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static uint Rounded(float value){return (uint)(long)CrtReference.Single(value+0.5f);}
 static int Main(){try{
  if(!Environment.Is64BitProcess||Environment.OSVersion.Platform!=PlatformID.Win32NT)throw new Exception("Requires Windows x64 diagnostic process");
  // Exactly the three input paths approved for this comparison; no discovery or GDX3 access.
  var cases=new[]{
   new CurveCase{archive=@"D:\Steam\steamapps\common\Grim Dawn\database\database.arz",expected_archive_sha256="9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603",expected_record_body_sha256="2f5b3f3768fbff986edfab9558e38414ddfed010889a6591c20082094c17ed37",expected_maximum_level=85,expected_output_sha256="3fed605ea9f18497b0988113d04ae1b64209be306e1dcbe907501ac7eefe8a91"},
   new CurveCase{archive=@"D:\Steam\steamapps\common\Grim Dawn\gdx1\database\GDX1.arz",expected_archive_sha256="c9eb9b8b76f5a2db6de14523205253102509b07dfac47b1da090c27d0b5a8893",expected_record_body_sha256="7a24126cfb8c691cd464d788060111125979372e07b002ba9d0e884db4b27d13",expected_maximum_level=100,expected_output_sha256="85ac7a9d418a8ddd7e37399733039b761437b06b286efc8b53cd8c23bd9daf86"},
   new CurveCase{archive=@"D:\Steam\steamapps\common\Grim Dawn\gdx2\database\GDX2.arz",expected_archive_sha256="163acc749ee9d647a0a7122d1f93b3c7ee9859142c9acabf1d7528591f5bc539",expected_record_body_sha256="e6418d7531e3bd7de9acb616fd5e2d515ec46a6004293ac6b048e491ab22155f",expected_maximum_level=100,expected_output_sha256="85ac7a9d418a8ddd7e37399733039b761437b06b286efc8b53cd8c23bd9daf86"}
  };
  string ucrtBefore=FileHash(Library);
  bool inputsMatch=true;
  // Validate all selected records and expressions before making numerical UCRT calls.
  foreach(var row in cases){
   row.archive_sha256_before=FileHash(row.archive);row.archive_matches_old=row.archive_sha256_before==row.expected_archive_sha256;
   using(var input=ResourceInput.OpenGameResource(row.archive))using(var db=new ArzArchive(input)){
    if(!db.Entries.ContainsKey(RecordName))throw new InvalidDataException("Approved record absent: "+row.archive);
    var record=db.ReadRecord(RecordName);
    row.record_body_sha256=record.body_sha256;row.record_matches_old=row.record_body_sha256==row.expected_record_body_sha256;
    string source=(string)record.fields["experienceLevelEquation"];
    row.expression_sha256=ResourceUtil.Hash(Encoding.UTF8.GetBytes(source));row.expression_matches_old=row.expression_sha256==ExpectedExpression;
    row.maximum_level=Convert.ToUInt32(record.fields["maxPlayerLevel"]);row.maximum_level_matches_old=row.maximum_level==row.expected_maximum_level;
    string shape=Regex.Replace(source.Replace(" ","").ToLowerInvariant(),"[0-9.]+","#");
    if(shape!="(((((playerlevel*playerlevel*playerlevel)^#)*#)+((playerlevel*playerlevel)*#))*#)+#")throw new InvalidDataException("Unexpected expression shape: "+row.archive);
    var tokens=Regex.Matches(source,"[0-9.]+");if(tokens.Count!=5)throw new InvalidDataException("Unexpected coefficient count");
    row.coefficient_tokens=new string[5];for(int i=0;i<5;i++)row.coefficient_tokens[i]=tokens[i].Value;
    inputsMatch&=row.record_matches_old&&row.expression_matches_old&&row.maximum_level_matches_old;
   }
  }
  uint beforeControl=0,afterControl=0;bool controlQueried=false;int comparisons=0;bool outputsMatch=false;
  if(inputsMatch){
   if(_controlfp_s(out beforeControl,0,0)!=0)throw new Exception("Read-only floating control query failed");controlQueried=true;
   if((beforeControl&0x300)!=0||(beforeControl&0x3000000)!=0||(beforeControl&0x8001f)!=0x8001f)throw new Exception("Floating control outside archived nearest/masked/denormal-preserved profile");
   outputsMatch=true;
   using(var reference=new CrtReference())foreach(var row in cases){
    var constants=new float[5];row.coefficient_bits=new uint[5];
    for(int i=0;i<5;i++){constants[i]=reference.Literal(row.coefficient_tokens[i]);row.coefficient_bits[i]=CrtReference.Bits(constants[i]);}
    row.current_float_bits=new uint[row.maximum_level+1];var bytes=new List<byte>();
    for(uint level=0;level<=row.maximum_level;level++){
     float value=reference.OfficialShape(level,constants);if(float.IsNaN(value)||float.IsInfinity(value))throw new Exception("Nonfinite diagnostic curve value");
     row.current_float_bits[level]=CrtReference.Bits(value);bytes.AddRange(BitConverter.GetBytes(value));
     if(level==1)row.first_threshold=Rounded(value);if(level==2)row.second_threshold=Rounded(value);comparisons++;
    }
    row.compared_levels=row.maximum_level+1;row.output_sha256=ResourceUtil.Hash(bytes.ToArray());row.output_matches_old=row.output_sha256==row.expected_output_sha256;outputsMatch&=row.output_matches_old;
   }
   if(_controlfp_s(out afterControl,0,0)!=0)throw new Exception("Final read-only floating control query failed");
  }
  bool archivesUnchanged=true;foreach(var row in cases){row.archive_sha256_after=FileHash(row.archive);row.archive_unchanged=row.archive_sha256_before==row.archive_sha256_after;archivesUnchanged&=row.archive_unchanged;}
  string ucrtAfter=FileHash(Library);bool controlUnchanged=controlQueried&&beforeControl==afterControl;
  bool matched=inputsMatch&&outputsMatch&&archivesUnchanged&&ucrtBefore==ucrtAfter&&controlUnchanged;
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{
   status=matched?"diagnostic_finite_curve_digests_match":"diagnostic_comparison_incomplete_or_mismatched",utc=DateTime.UtcNow.ToString("o"),production_profile_accepted=false,full_build_status="still failed at unchanged UCRT fingerprint gate",
   baseline="docs/rewards/acceptance-crt.json: official_curve_probe.result.rows; exact input record/expression hashes checked before numerical calls",record=RecordName,expected_expression_sha256=ExpectedExpression,
   library=new{path=Library,version=FileVersionInfo.GetVersionInfo(Library).FileVersion,sha256_before=ucrtBefore,sha256_after=ucrtAfter,unchanged=ucrtBefore==ucrtAfter},
   floating_control=new{queried=controlQueried,before=beforeControl,after=afterControl,unchanged=controlUnchanged,query_mask=0,explicit_control_changes=false,private_c_locale=true},
   records=cases.Length,compared_values=comparisons,all_input_records_and_expressions_match=inputsMatch,all_output_digests_match=outputsMatch,all_archives_unchanged=archivesUnchanged,digest_encoding="four-byte little-endian IEEE754 binary32 outputs for levels zero through each maximum in ascending order",rows=cases,
   limitations=new[]{"288 comparisons include overlapping 0..85/0..100 domains across three records; they are not 288 distinct input levels.","Only unchanged test-only CrtReference.OfficialShape was executed. Production CrtEquation, ExperienceScaling, integration scenarios and the failed 894-assertion full build were not replayed or accepted.","Matching these finite output sequences does not prove all powf inputs or all expressions are equivalent across UCRT versions.","Exactly three authorized ARZ files were read. No original DLL/executable, GDX3, other installation resource, player save or Steam cloud data was opened."}
  }));
  return matched?0:2;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
