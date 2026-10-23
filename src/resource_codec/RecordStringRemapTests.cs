using System;
using System.IO;
using GrimDynasty.Resources;

// Own byte fixtures for the archived 0x180280870 traversal. No native execution.
// Completed is the checked model's loop exit, not a recovered native bool.
// A safety stop retains earlier stores; it does not establish native rollback.
class RecordStringRemapTests {
 static int assertions,scenarios;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static byte[] Join(params byte[][] parts){
  using(var stream=new MemoryStream()){foreach(byte[] part in parts)stream.Write(part,0,part.Length);return stream.ToArray();}
 }
 static byte[] Header(ushort tag,ushort count,uint key){
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
   writer.Write(tag);writer.Write(count);writer.Write(key);writer.Flush();return stream.ToArray();
  }
 }
 static byte[] Words(params uint[] words){
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
   foreach(uint word in words)writer.Write(word);writer.Flush();return stream.ToArray();
  }
 }
 static byte[] Field(ushort tag,uint key,params uint[] words){
  return Join(Header(tag,checked((ushort)words.Length),key),Words(words));
 }
 static bool Is(RecordStringRemapResult result,RecordStringRemapStatus status,string reason,int cursor,int fields,int words){
  return result.Status==status&&result.Reason==reason&&result.Cursor==cursor&&result.FieldsProcessed==fields&&result.WordsWritten==words;
 }
 static void Completed(byte[] body,int length,RecordStringRemapContext context,byte[] expected,int cursor,int fields,int words,string why){
  var result=RecordStringRemap.ApplyChecked(body,length,context);
  Need(Is(result,RecordStringRemapStatus.Completed,null,cursor,fields,words)&&Same(body,expected),why);
 }
 static void Stopped(byte[] body,int length,RecordStringRemapContext context,byte[] expected,string reason,int cursor,int fields,int words,string why){
  var result=RecordStringRemap.ApplyChecked(body,length,context);
  Need(Is(result,RecordStringRemapStatus.Unsupported,reason,cursor,fields,words)&&
   (body==null?expected==null:Same(body,expected)),why);
 }
 static RecordStringRemapContext Map(uint[] entries,int signedCount,uint baseId){
  return new RecordStringRemapContext(true,entries,signedCount,baseId);
 }
 static void NoOpAndOwnGuards(){
  Need(RecordStringRemap.MaximumBodyBytes==1048576&&RecordStringRemapContext.MaximumMappingEntries==65536,"body and physical mapping budgets are explicit own coverage limits");
  byte[] invalidHeader={0xff};
  Completed(invalidHeader,1,new RecordStringRemapContext(false,null,int.MaxValue,uint.MaxValue),new byte[]{0xff},0,0,0,"absent native table skips even an unreadable field header and nonzero base");
  byte[] untouched=Field(2,0,1);
  Completed(untouched,0,Map(null,2,1),Field(2,0,1),0,0,0,"zero declared length does not inspect a present table or remap any word");
  Completed(new byte[0],0,Map(new uint[0],0,5),new byte[0],0,0,0,"empty body with zero declared length completes without a field iteration");
  byte[] header=Header(2,0,0);
  Stopped(header,8,null,Header(2,0,0),"UnknownRemapContext",0,0,0,"unknown archive remap context is not an assumed absent table");
  Stopped(null,0,new RecordStringRemapContext(false,null,0,0),null,"NullRemapBody",0,0,0,"null body is rejected by the own API before its no-op domain");
  Stopped(Header(2,0,0),-1,Map(null,0,0),Header(2,0,0),"RemapDeclaredLengthOutsideCoveredDomain",0,0,0,"negative declared length does not become an unsigned native scan");
  Stopped(new byte[0],1,Map(null,0,0),new byte[0],"RemapDeclaredLengthOutsideCoveredDomain",0,0,0,"declared length beyond supplied body is an input-domain rejection");
  var tooLarge=new byte[RecordStringRemap.MaximumBodyBytes+1];
  Stopped(tooLarge,0,new RecordStringRemapContext(false,null,0,0),new byte[tooLarge.Length],"RemapBodyOutsideCoveredDomain",0,0,0,"body budget applies before the absent-table no-op");
  var exact=new byte[RecordStringRemap.MaximumBodyBytes];
  Completed(exact,0,Map(null,0,0),new byte[exact.Length],0,0,0,"the exact body budget is accepted without touching unused bytes");
  bool rejected=false;
  try{new RecordStringRemapContext(true,new uint[RecordStringRemapContext.MaximumMappingEntries+1],0,0);}
  catch(ArgumentOutOfRangeException){rejected=true;}
  Need(rejected,"oversized physical mapping snapshot is rejected at construction");
 }
 static void AdditionSignedChecksAndSnapshot(){
  Completed(Field(2,1,2,3),16,Map(new uint[0],0,5),Field(2,6,7,8),16,1,3,"present empty table still adds base to every key and tag-2 value");
  foreach(int count in new[]{-1,int.MinValue})
   Completed(Field(2,0,0,uint.MaxValue),16,Map(new uint[]{99},count,1),Field(2,1,1,0),16,1,3,"negative signed mapping count skips lookup while preserving uint32 wrap and stores");
  Completed(Field(2,1,2,3),16,Map(null,0,0),Field(2,1,2,3),16,1,3,"same-bit stores still count as writes rather than no-op shortcuts");
  uint[] mapping={2,uint.MaxValue,0x80000000U};
  Completed(Field(2,uint.MaxValue,uint.MaxValue,0,1,2,0x7fffffffU,0x80000000U),32,Map(mapping,3,1),
   Field(2,2,2,uint.MaxValue,0x80000000U,3,0x80000000U,0x80000001U),32,1,7,
   "unchecked addition precedes signed range checks; mapped values are not offset or mapped again");
  Completed(Field(2,1,0,0x80000000U),16,Map(new uint[]{0x80000000U,99,100},3,uint.MaxValue),
   Field(2,0x80000000U,uint.MaxValue,0x7fffffffU),16,1,3,
   "base FFFFFFFF wraps one to index zero and preserves negative or signed-out-of-range sums");
  Completed(Field(2,0x7fffffffU,0x80000000U,uint.MaxValue),16,Map(null,int.MaxValue,0),
   Field(2,0x7fffffffU,0x80000000U,uint.MaxValue),16,1,3,
   "sum equal to signed count and negative sums do not require missing mapping storage");
  uint[] supplied={21,22};var snapshot=Map(supplied,2,0);supplied[0]=999;
  Completed(Field(2,0,1),12,snapshot,Field(2,21,22),12,1,2,"context takes a mapping snapshot before the caller changes the source array");
  Need(supplied[0]==999&&supplied[1]==22&&snapshot.TablePresent&&snapshot.SignedMappingCount==2&&snapshot.BaseId==0,"remapping neither consumes nor rewrites the external mapping array or context metadata");
 }
 static void MixedFieldsScenario(){
  byte[] body=Join(Field(0,0,uint.MaxValue,0x80000000U),Field(1,1,0x7fc01234U,0x3f800000U),
   Field(2,2,0,1,2),Field(3,3,0x12345678U));
  byte[] expected=Join(Field(0,100,uint.MaxValue,0x80000000U),Field(1,101,0x7fc01234U,0x3f800000U),
   Field(2,102,100,101,102),Field(3,103,0x12345678U));
  Completed(body,body.Length,Map(new uint[]{100,101,102,103},4,0),expected,64,4,7,
   "all field keys map, only tag-2 values map, and tag/count headers plus raw tag-0/1/3 payload bits remain unchanged");
  scenarios++;
 }
 static void UnknownTagScenario(){
  byte[] body=Join(Header(ushort.MaxValue,ushort.MaxValue,0),Field(2,1,2));
  Completed(body,20,Map(new uint[]{10,11,12},3,0),
   Join(Header(ushort.MaxValue,ushort.MaxValue,10),Field(2,11,12)),20,2,3,
   "unknown tag consumes only its header so the adjacent header is visited despite nominal count FFFF");
  foreach(ushort tag in new ushort[]{4,65534})
   Completed(Header(tag,1,5),8,Map(new uint[0],0,7),Header(tag,1,12),8,1,1,
    "other unknown tags also write their key without skipping their nominal payload");
  scenarios++;
 }
 static void DeclaredLengthAndSkips(){
  foreach(ushort tag in new ushort[]{0,1,3})
   Completed(Header(tag,ushort.MaxValue,5),1,Map(null,0,5),Header(tag,ushort.MaxValue,10),262148,1,1,
    "scalar count skip can cross declared end and actual body end without reading omitted payload");
  Completed(Header(2,0,0),1,Map(new uint[]{10},1,0),Header(2,0,10),8,1,1,
   "zero-count string field writes its key and completes after the header");
  Completed(Field(2,0,1,2),8,Map(new uint[]{10,11,12},3,0),Field(2,10,11,12),16,1,3,
   "tag-2 values beyond declared end are still read when their actual supplied bytes exist");
  byte[] suffix={0xff,0xfe,0xfd,0xfc,0xfb,0xfa,0xf9};
  Completed(Join(Field(2,0,1),suffix),12,Map(new uint[]{10,11},2,0),
   Join(Field(2,10,11),suffix),12,1,2,"loop-tail boundary leaves subsequent supplied bytes untouched");
 }
 static void TruncatedReadsRetainPrefix(){
  byte[] full=Header(2,1,0);
  for(int length=1;length<8;length++){
   var body=new byte[length];Array.Copy(full,body,length);
   Stopped(body,length,Map(new uint[]{10},1,0),(byte[])body.Clone(),"RemapHeaderOutsideSuppliedBody",0,0,0,
    "short header cannot write even a partially supplied key");
  }
  byte[] partialHeader={2,0,1,0};
  Stopped(Join(Header(0,0,0),partialHeader),12,Map(new uint[]{10},1,0),
   Join(Header(0,0,10),partialHeader),"RemapHeaderOutsideSuppliedBody",8,1,1,
   "next header failure preserves the preceding completed key store");
  for(int remainder=0;remainder<4;remainder++){
   var partialWord=new byte[remainder];for(int i=0;i<remainder;i++)partialWord[i]=checked((byte)(0xa0+i));
   byte[] body=Join(Header(2,2,0),Words(1),partialWord);
   Stopped(body,body.Length,Map(new uint[]{10,11},2,0),
    Join(Header(2,2,10),Words(11),partialWord),"RemapValueOutsideSuppliedBody",12,0,2,
    "short string word leaves prior key/value writes and unread partial bytes intact without advancing its cursor");
  }
 }
 static void LazyMappingStorageFailures(){
  Stopped(Header(0,0,0),8,Map(null,2,1),Header(0,0,0),"RemapMappingLookupOutsideSuppliedStorage",8,0,0,
   "unknown mapping cell stops after header read but before writing even the locally computed sum");
  Stopped(Header(0,0,1),8,Map(new uint[]{10},2,0),Header(0,0,1),"RemapMappingLookupOutsideSuppliedStorage",8,0,0,
   "physical mapping array length does not silently replace the separate signed count");
  Completed(Field(2,uint.MaxValue,3),12,Map(null,3,0),Field(2,uint.MaxValue,3),12,1,2,
   "missing mapping storage is irrelevant to sums skipped by signed bounds");
  Completed(Header(0,0,1),8,Map(new uint[0],1,0),Header(0,0,1),8,1,1,
   "index equal to the signed count is stored directly even with an empty physical array");
  byte[] body=Join(Field(0,0,0xaabbccddU),Field(2,1,0,2,1));
  Stopped(body,body.Length,Map(new uint[]{100,101},3,0),
   Join(Field(0,100,0xaabbccddU),Field(2,101,100,2,1)),
   "RemapMappingLookupOutsideSuppliedStorage",28,1,3,
   "later mapping failure preserves an earlier full field and current key/value stores; the read-but-unwritten word and following word remain original");
 }
 public static int Main(){
  try{
   NoOpAndOwnGuards();AdditionSignedChecksAndSnapshot();MixedFieldsScenario();UnknownTagScenario();
   DeclaredLengthAndSkips();TruncatedReadsRetainPrefix();LazyMappingStorageFailures();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+
    ",\"scope\":\"synthetic bounded in-place record string remap; no native execution\"}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.GetType().Name+": "+error.Message);return 1;}
 }
}
