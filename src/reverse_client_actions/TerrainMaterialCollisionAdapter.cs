using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace GrimDynasty.ReverseClient.Actions {
 // Saved evidence only. No file access, native allocation, shader setup,
 // ObjectManager registration or native TerrainType ABI is implemented here.
 // Address zero denotes an OWN admission failure, not a native instruction.
 internal static class TerrainSavedEvidence {
  internal static void Need(bool condition,string reason){if(!condition)throw new MissingContract(0,"Unsupported OWN terrain evidence: "+reason);}
  internal static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  internal static bool Digest(string s){if(s==null||s.Length!=64)return false;foreach(char c in s)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))return false;return true;}
  internal static Dictionary<string,object> Object(object v){var d=v as Dictionary<string,object>;Need(d!=null,"JSON object required");return d;}
  internal static object Get(Dictionary<string,object> d,string key){object v;Need(d.TryGetValue(key,out v),"Missing "+key);return v;}
  internal static string Text(Dictionary<string,object> d,string key){var v=Get(d,key) as string;Need(v!=null,"String "+key+" required");return v;}
  internal static object[] Array(Dictionary<string,object> d,string key){var v=Get(d,key) as object[];Need(v!=null,"Array "+key+" required");return v;}
  internal static void Equal(Dictionary<string,object> d,string key,string expected){Need(String.Equals(Text(d,key),expected,StringComparison.Ordinal),key+" mismatch");}
  internal static long Integer(Dictionary<string,object> d,string key){object v=Get(d,key);Need(v is int||v is long,key+" integer required");return Convert.ToInt64(v);}
  internal static void Number(Dictionary<string,object> d,string key,long expected){Need(Integer(d,key)==expected,key+" integer mismatch");}
  internal static void Flag(Dictionary<string,object> d,string key,bool expected){object v=Get(d,key);Need(v is bool&&(bool)v==expected,key+" flag mismatch");}
  internal static Dictionary<string,object> Parse(byte[] bytes,int maximum){
   Need(bytes!=null&&bytes.Length>0&&bytes.Length<=maximum,"Bounded saved JSON required");
   try{return Object(new JavaScriptSerializer{MaxJsonLength=maximum,RecursionLimit=32}.DeserializeObject(new UTF8Encoding(false,true).GetString(bytes)));}
   catch(MissingContract){throw;}catch(Exception e){throw new MissingContract(0,"Unsupported OWN terrain JSON",e);}
  }
  internal static bool BytesEqual(byte[] a,byte[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int n=0;n<a.Length;n++)if(a[n]!=b[n])return false;return true;}
  internal static byte[] Ascii(string value){Need(value!=null,"Missing string");foreach(char c in value)Need(c>=32&&c<=126,"Only exact saved ASCII bytes supported");return Encoding.ASCII.GetBytes(value);}
  internal static byte[] Hex(string value,int count){
   Need(value!=null&&value.Length==count*2,"Exact operand byte count required");var b=new byte[count];
   for(int n=0;n<count;n++){int hi=Digit(value[n*2]),lo=Digit(value[n*2+1]);Need(hi>=0&&lo>=0,"Canonical lower-case raw_hex required");b[n]=(byte)((hi<<4)|lo);}return b;
  }
  static int Digit(char c){return c>='0'&&c<='9'?c-'0':c>='a'&&c<='f'?c-'a'+10:-1;}
 }

 public sealed class TerrainSurfaceOperandEvidence {
  public const string EngineHash="4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153";
  public const string PlanHash="0711e4f382930322564293669b861edb3780f0616f22a95498e442a38a5623a4";
  public const string SavedReportHash="56db24887edb67e188ba5f034d3734c24042e973c70be4c3e833896942b1a75f";
  public const string SavedReaderHash="8be08c76e92519ad71b0cb465113b3f2e9813fcfaee9bbc4004d15dd57996e04";
  const string EnginePath="D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll";
  static readonly string[] Rvas={"0x3244cc","0x3244d4","0x3244dc","0x32446c","0x32447c","0x3225dc"};
  static readonly uint[] Values={2,3,4,6,9,10};
  static readonly string[] References={"0x1801e1dc6: LEA RDX, [0x1803244cc]","0x1801e1e03: LEA RDX, [0x1803244d4]","0x1801e1e43: LEA RDX, [0x1803244dc]","0x1801e1eb9: LEA RDX, [0x18032446c]","0x1801e1f38: LEA RDX, [0x18032447c]","0x1801e1f7c: LEA RDX, [0x1803225dc]"};
  static readonly string[] Writes={"0x1801e1ddb: MOV dword ptr [RDI + 0xfc], 0x2","0x1801e1e1e: MOV dword ptr [RDI + 0xfc], R8D","0x1801e1e58: MOV dword ptr [RDI + 0xfc], R13D","0x1801e1ece: MOV dword ptr [RDI + 0xfc], 0x6","0x1801e1f4d: MOV dword ptr [RDI + 0xfc], 0x9","0x1801e1f92: MOV dword ptr [RDI + 0xfc], 0xa"};
  readonly byte[][] operands;
  public readonly string ReportSha256,ReaderSha256;
  TerrainSurfaceOperandEvidence(byte[][] bytes,string reportHash,string readerHash){operands=bytes;ReportSha256=reportHash;ReaderSha256=readerHash;}
  public static TerrainSurfaceOperandEvidence FromSavedReport(byte[] report,string reviewedReportSha256,string reviewedReaderSha256){
   TerrainSavedEvidence.Need(report!=null&&report.Length>0&&report.Length<=65536&&TerrainSavedEvidence.Digest(reviewedReportSha256)&&TerrainSavedEvidence.Digest(reviewedReaderSha256),"Reviewed receipt and reader hashes required");
   TerrainSavedEvidence.Need(reviewedReportSha256==SavedReportHash&&reviewedReaderSha256==SavedReaderHash,"Only the root-reviewed actual fixed23B receipt is admitted");
   TerrainSavedEvidence.Need(TerrainSavedEvidence.Hash(report)==reviewedReportSha256,"Receipt fingerprint changed");
   var d=TerrainSavedEvidence.Parse(report,65536);
   TerrainSavedEvidence.Equal(d,"schema","grim-dynasty.fixed-terrain-surface-strings.v1");TerrainSavedEvidence.Equal(d,"status","passed");
   TerrainSavedEvidence.Equal(d,"plan_sha256",PlanHash);TerrainSavedEvidence.Equal(d,"script_sha256",reviewedReaderSha256);
   TerrainSavedEvidence.Flag(d,"disk_observations_complete",true);TerrainSavedEvidence.Flag(d,"proofs_verified",true);
   TerrainSavedEvidence.Flag(d,"original_loaded_or_executed",false);TerrainSavedEvidence.Number(d,"subprocesses",0);
   TerrainSavedEvidence.Number(d,"metadata_bytes",0);TerrainSavedEvidence.Number(d,"new_header_bytes",0);TerrainSavedEvidence.Number(d,"code_bytes",0);
   TerrainSavedEvidence.Number(d,"data_bytes",23);TerrainSavedEvidence.Number(d,"read_operations",6);
   TerrainSavedEvidence.Need(TerrainSavedEvidence.Array(d,"failures").Length==0,"Reader failures present");
   var source=TerrainSavedEvidence.Object(TerrainSavedEvidence.Get(d,"source"));TerrainSavedEvidence.Equal(source,"path",EnginePath);TerrainSavedEvidence.Equal(source,"sha256",EngineHash);TerrainSavedEvidence.Number(source,"bytes",4485632);
   var files=TerrainSavedEvidence.Array(d,"files");TerrainSavedEvidence.Need(files.Length==1,"Exactly one fingerprinted source required");var file=TerrainSavedEvidence.Object(files[0]);
   TerrainSavedEvidence.Equal(file,"source",EnginePath);TerrainSavedEvidence.Equal(file,"expected_sha256",EngineHash);TerrainSavedEvidence.Number(file,"expected_size",4485632);
   TerrainSavedEvidence.Equal(file,"sha256_before",EngineHash);TerrainSavedEvidence.Equal(file,"sha256_after",EngineHash);
   TerrainSavedEvidence.Number(file,"hash_bytes",8971264);TerrainSavedEvidence.Number(file,"hash_passes",2);TerrainSavedEvidence.Flag(file,"stable",true);TerrainSavedEvidence.Flag(file,"closed",true);
   var pins=TerrainSavedEvidence.Array(d,"source_pins");TerrainSavedEvidence.Need(pins.Length==3,"Exactly three pinned proofs required");
   string[] keys={"pe","assembly","pseudo"};string[] paths={"docs/object-lifecycle/offline-class-registration-exports/discovery.json","docs/reverse-client/r1-terrain-type-material/05-TerrainTypeMaterial-0x1801e1940-procedure_assembly.json","docs/reverse-client/r1-terrain-type-material/04-TerrainTypeMaterial-0x1801e1940-procedure_pseudo_code.json"};
   string[] hashes={"0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58","6c4e417d3b9c908a716eb680ad7ca8f3cf0b1c3e53f847ba891950ccee8730e5","26f717082e3030352ffe34277406c1132c45662d0867b707c27df55375579038"};
   for(int n=0;n<3;n++){var pin=TerrainSavedEvidence.Object(pins[n]);TerrainSavedEvidence.Equal(pin,"key",keys[n]);TerrainSavedEvidence.Equal(pin,"path",paths[n]);TerrainSavedEvidence.Equal(pin,"sha256",hashes[n]);}
   var rows=TerrainSavedEvidence.Array(d,"values");var reads=TerrainSavedEvidence.Array(file,"reads");TerrainSavedEvidence.Need(rows.Length==6&&reads.Length==6,"Six actual operand observations required");var bytes=new byte[6][];
   for(int n=0;n<6;n++){
    var row=TerrainSavedEvidence.Object(rows[n]);var read=TerrainSavedEvidence.Object(reads[n]);int count=n==5?3:4;
    TerrainSavedEvidence.Equal(row,"rva",Rvas[n]);TerrainSavedEvidence.Number(row,"bytes",count);TerrainSavedEvidence.Number(row,"surface_fc",Values[n]);
    TerrainSavedEvidence.Equal(row,"reference",References[n]);TerrainSavedEvidence.Equal(row,"write",Writes[n]);TerrainSavedEvidence.Flag(row,"loaded_value_observed",false);TerrainSavedEvidence.Equal(row,"section",".rdata");
    TerrainSavedEvidence.Equal(read,"rva",Rvas[n]);TerrainSavedEvidence.Number(read,"requested_bytes",count);TerrainSavedEvidence.Number(read,"read_bytes",count);
    long offset=TerrainSavedEvidence.Integer(row,"file_offset");TerrainSavedEvidence.Need(offset>=0&&offset+count<=4485632,"Operand file extent");TerrainSavedEvidence.Number(read,"file_offset",offset);
    string raw=TerrainSavedEvidence.Text(row,"raw_hex");bytes[n]=TerrainSavedEvidence.Hex(raw,count);
    // CString termination/default bytes were explicitly outside the read scope.
    // Reject a surprising non-printable operand rather than invent encoding.
    foreach(byte value in bytes[n])TerrainSavedEvidence.Need(value>=32&&value<=126,"Unsupported operand encoding");
    for(int prior=0;prior<n;prior++)TerrainSavedEvidence.Need(!TerrainSavedEvidence.BytesEqual(bytes[prior],bytes[n]),"Ambiguous duplicate fixed operand");
   }
   return new TerrainSurfaceOperandEvidence(bytes,reviewedReportSha256,reviewedReaderSha256);
  }
  internal static bool Known(byte[] value,out uint fc){
   string[] names={"Unwalkable","Grass","Stone","Water"};uint[] codes={0,1,5,7};
   for(int n=0;n<names.Length;n++)if(TerrainSavedEvidence.BytesEqual(value,Encoding.ASCII.GetBytes(names[n]))){fc=codes[n];return true;}fc=0;return false;
  }
  public uint Resolve(byte[] surfaceBytes){
   uint fc;if(Known(surfaceBytes,out fc))return fc;
   for(int n=0;n<operands.Length;n++)if(TerrainSavedEvidence.BytesEqual(surfaceBytes,operands[n]))return Values[n];
   throw new MissingContract(0,"Unsupported OWN surfaceType: exact observed comparison operand required; native unmatched FC100 is not a default-success policy");
  }
 }

 public sealed class TerrainMaterialCollisionAdapter {
  public const string SavedMemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
  public const string SavedChunkHash="b104e3c16e1111593ae64eca7aa436be270459e4ecfa0ad79bb2c870e6897768";
  static readonly string[] Names={"records/terraintextures/earth/flat_rockdark.dbr","records/terraintextures/grass/poly_grasssparserocky.dbr","records/terraintextures/earth/flat_soilforestroots.dbr","records/terraintextures/plants/poly_cornstalks_dead01.dbr","records/terraintextures/earth/flat_leaflittermaplebrown.dbr"};
  static readonly string[] SurfaceNames={"Stone","Grass","Dirt","Dirt","Dirt"};
  static readonly string[] RecordHashes={"99174ba1196eae2c109ad46c18fdcbf2e368923d49117739d0496829d3389d93","d3d75935c98d5909f95ce0ca5c330e110dd42f5a4251ec881706c95c00edd4bf","267ec1e9b06cd9c54812eeaee3e0a8a7b7ff7cc9223f42823de48c2a43213895","6512c54802bcd3b6ad1bb83fe86b8b35a5f0e98397245a8347501cece66c7b4b","e829033a0fb91dfb904973a53809b1eda34f41c5369a9b22726637dc4cde98ee"};
  static readonly string[] BodyHashes={"960567353cdbecc252bf4daace95e1c0f0926af6c9a5215fb296ac5e065553d0","01a14d2df22ee21bf621f0244d944e77206a27ff45a023ede94f13ded965a59e","25468bc8b7cdc6227326024b98c3b2fd5e95138f5242631775b97f2b39cec907","646460a4cfefd6c9167b227357db7402bd92278350bb7df71fd54a9b3c815fb6","f8bb1f8680774b098bb324d5c970f96465a6e6e3838bfec828b633393f23ea7e"};
  public readonly OwnGroundData Data;
  readonly OwnGroundTypeRecord[] records=new OwnGroundTypeRecord[5];
  readonly GroundMaterialSurfaceState[] materials=new GroundMaterialSurfaceState[5];
  readonly bool[] known=new bool[5];readonly uint[] expectedFC=new uint[5];
  TerrainMaterialCollisionAdapter(OwnGroundData data){Data=data;}
  static void CheckGroundBytes(OwnGroundData data){
   // SourceIdentity is caller-supplied and is not proof. Re-encode the live
   // OWN decoded inputs and compare the actual saved chunk fingerprint. This
   // borrows the same data/records rather than decoding a replacement object.
   TerrainSavedEvidence.Need(data.Dimension60.Read(0)==129&&data.Dimension64.Read(0)==129&&data.Stride118.Read(0)==1,"Fixed saved dimensions required");
   var cells=data.Cells128.Read(0);TerrainSavedEvidence.Need(cells!=null&&cells.Count==16641,"Fixed saved cell extent required");
   using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream)){
    writer.Write(2u);foreach(uint word in data.HeaderWords())writer.Write(word);
    for(uint n=0;n<16641;n++)writer.Write(cells.Read(n,0));writer.Write(data.RawArray120());
    for(uint n=0;n<5;n++){
     var record=data.TypeRecordAt(n,0);byte[] name=record.NameBytes(),weights=record.WeightBytes();
     writer.Write((uint)name.Length);writer.Write(name);
     TerrainSavedEvidence.Need(n==0?weights==null:weights!=null&&weights.Length==16384,"Fixed saved weight extent required");if(weights!=null)writer.Write(weights);
    }
    writer.Write(data.SkippedTrailer());writer.Flush();
    TerrainSavedEvidence.Need(stream.Length==149073&&TerrainSavedEvidence.Hash(stream.ToArray())==SavedChunkHash,"Actual saved ground bytes changed; a source label is insufficient");
   }}
  }
  public static TerrainMaterialCollisionAdapter FromSavedRecords(OwnGroundData data,byte[][] savedRecordJson,TerrainSurfaceOperandEvidence evidence){
   TerrainSavedEvidence.Need(data!=null&&savedRecordJson!=null&&savedRecordJson.Length==5,"Exactly five saved records and actual GroundData required");
   TerrainSavedEvidence.Need(data.TypeRecordCount==5&&data.SourceIdentity==SavedMemberHash+":273260+149073"&&data.ConsumedBytes==149073,"Fixed saved GroundData identity required");
   CheckGroundBytes(data);
   var adapter=new TerrainMaterialCollisionAdapter(data);
   for(int n=0;n<5;n++){
    byte[] json=savedRecordJson[n];TerrainSavedEvidence.Need(json!=null&&json.Length<=65536&&TerrainSavedEvidence.Hash(json)==RecordHashes[n],"Pinned record JSON identity/order mismatch");
    var document=TerrainSavedEvidence.Parse(json,65536);TerrainSavedEvidence.Equal(document,"schema","grim-dynasty-offline-v1");TerrainSavedEvidence.Equal(document,"status","Known");TerrainSavedEvidence.Equal(document,"command","inspect-record");
    var detail=TerrainSavedEvidence.Object(TerrainSavedEvidence.Get(document,"data"));
    TerrainSavedEvidence.Equal(detail,"input",@"D:\Steam\steamapps\common\Grim Dawn\database\database.arz");
    var record=TerrainSavedEvidence.Object(TerrainSavedEvidence.Get(detail,"record"));TerrainSavedEvidence.Equal(record,"name",Names[n]);TerrainSavedEvidence.Equal(record,"class","TerrainType");TerrainSavedEvidence.Equal(record,"body_sha256",BodyHashes[n]);
    var types=TerrainSavedEvidence.Object(TerrainSavedEvidence.Get(record,"field_types"));TerrainSavedEvidence.Number(types,"surfaceType",2);TerrainSavedEvidence.Number(types,"Class",2);
    var fields=TerrainSavedEvidence.Object(TerrainSavedEvidence.Get(record,"fields"));TerrainSavedEvidence.Equal(fields,"Class","TerrainType");TerrainSavedEvidence.Equal(fields,"surfaceType",SurfaceNames[n]);
    var own=data.TypeRecordAt((uint)n,0);TerrainSavedEvidence.Need(TerrainSavedEvidence.BytesEqual(own.NameBytes(),TerrainSavedEvidence.Ascii(Names[n])),"Exact saved name bytes required; no folding or normalization");
    adapter.records[n]=own;adapter.materials[n]=new GroundMaterialSurfaceState();byte[] surface=TerrainSavedEvidence.Ascii(TerrainSavedEvidence.Text(fields,"surfaceType"));uint fc;
    if(TerrainSurfaceOperandEvidence.Known(surface,out fc)){adapter.known[n]=true;adapter.expectedFC[n]=fc;}
    else if(evidence!=null){adapter.expectedFC[n]=evidence.Resolve(surface);adapter.known[n]=true;}
    if(adapter.known[n])adapter.materials[n].SurfaceFC.Write(adapter.expectedFC[n]);
   }
   return adapter;
  }
  int Index(uint index){TerrainSavedEvidence.Need(index<5,"Unsupported record index");int n=(int)index;TerrainSavedEvidence.Need(Object.ReferenceEquals(Data.TypeRecordAt(index,0),records[n]),"Ground type-record identity changed");return n;}
  public OwnGroundTypeRecord RecordAt(uint index){return records[Index(index)];}
  public GroundMaterialSurfaceState MaterialViewAt(uint index){return materials[Index(index)];}
  public uint RequireSurfaceFC(uint index){int n=Index(index);TerrainSavedEvidence.Need(known[n],"surfaceType has no actual fixed-operand evidence");TerrainSavedEvidence.Need(materials[n].SurfaceFC.Read(0)==expectedFC[n],"Admitted material FC changed");return expectedFC[n];}
  void CheckPublish(uint index){
   RequireSurfaceFC(index);int n=Index(index);object value;
   try{value=records[n].Material.Read(0);}catch(MissingContract){return;}
   TerrainSavedEvidence.Need(Object.ReferenceEquals(value,materials[n]),"Existing material identity is not owned by this adapter");
  }
  public GroundMaterialSurfaceState BindRecord(uint index){CheckPublish(index);int n=Index(index);records[n].Material.Write(materials[n]);return materials[n];}
  public void BindAll(){
   // Preflight the whole set before publishing. Unsupported Dirt cannot cause
   // a partial success. Repeating publication reuses these same five objects.
   for(uint n=0;n<5;n++)CheckPublish(n);for(uint n=0;n<5;n++)records[n].Material.Write(materials[n]);
  }
 }
}
