using System;
using System.IO;
using System.Text;
using GrimDynasty.Resources;

// Own wire fixtures for the archived initial/append/post import callers.
// These tests cover normal storage operations and the model's explicit budgets;
// they do not execute native code or simulate allocation/EH failures.
class ArchiveStringImportTests {
 static int assertions,scenarios;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] Text(string value){return Encoding.UTF8.GetBytes(value);}
 static bool Same(byte[] a,byte[] b){
  if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;
 }
 static bool Same(uint[] a,uint[] b){
  if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;
 }
 static bool Pool(ArchiveStringImportState state,params byte[][] expected){
  byte[][] actual=state.CopyTargetStrings();if(actual.Length!=expected.Length)return false;
  for(int i=0;i<actual.Length;i++)if(!Same(actual[i],expected[i]))return false;return true;
 }
 static bool Mappings(ArchiveStringImportState state,int start,params uint[] expected){
  for(int i=0;i<expected.Length;i++){uint value;if(!state.TryGetMapping(start+i,out value)||value!=expected[i])return false;}
  return true;
 }
 static byte[] Words(params uint[] values){
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
   foreach(uint value in values)writer.Write(value);writer.Flush();return stream.ToArray();
  }
 }
 static byte[] Join(params byte[][] parts){
  using(var stream=new MemoryStream()){foreach(byte[] part in parts)stream.Write(part,0,part.Length);return stream.ToArray();}
 }
 static byte[] Wire(params byte[][] entries){
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
   writer.Write(entries.Length);foreach(byte[] entry in entries){writer.Write(entry.Length);writer.Write(entry);}
   writer.Flush();return stream.ToArray();
  }
 }
 static byte[] RepeatedWire(int count,byte[] entry){
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
   writer.Write(count);for(int i=0;i<count;i++){writer.Write(entry.Length);writer.Write(entry);}
   writer.Flush();return stream.ToArray();
  }
 }
 static bool InvalidOperation(Action action){
  try{action();return false;}catch(InvalidOperationException){return true;}
 }
 static bool OutOfRange(Action action){
  try{action();return false;}catch(ArgumentOutOfRangeException){return true;}
 }
 static byte[] RemapField(uint key,uint value){
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
   writer.Write((ushort)2);writer.Write((ushort)1);writer.Write(key);writer.Write(value);
   writer.Flush();return stream.ToArray();
  }
 }
 static bool Remaps(RecordStringRemapContext context,uint key,uint value,uint expectedKey,uint expectedValue){
  byte[] body=RemapField(key,value);
  var result=RecordStringRemap.ApplyChecked(body,body.Length,context);
  return result.Status==RecordStringRemapStatus.Completed&&Same(body,RemapField(expectedKey,expectedValue));
 }
 static void InitialOrderAndEmpty(){
  var duplicate=new ArchiveStringImportState();
  var first=duplicate.ImportChecked(Wire(Text("A"),Text("A"),Text("B")),false);
  Need(first.Status==ArchiveStringImportStatus.Completed&&!first.Append&&first.OriginalCount==3&&
   first.EntriesProcessed==3&&first.SourceStringCount==3&&!first.SourceIdsPreserved,"initial duplicates complete while retaining the original source-item count");
  Need(Pool(duplicate,Text("A"),Text("B"),new byte[0])&&duplicate.TargetStringCount==3,
   "first occurrence gets the ID; post adds absent empty after the two unique strings");
  uint value;
  Need(duplicate.MappingCount==0&&duplicate.CopySelectors().Length==0&&!duplicate.TryGetMapping(0,out value),
   "initial deduplication does not manufacture a per-source mapping table");
  Need(InvalidOperation(delegate{duplicate.CreateRemapContext(first);}),"a duplicate initial pool cannot be exported as identity-preserving raw IDs");
  var next=duplicate.ImportChecked(Wire(Text("C"),Text("B")),true);
  Need(next.Status==ArchiveStringImportStatus.Completed&&next.BaseId==0&&Mappings(duplicate,0,3,1)&&
   Pool(duplicate,Text("A"),Text("B"),new byte[0],Text("C")),"post empty occupies ID 2 and the next unique append gets ID 3");
  var emptyFirst=new ArchiveStringImportState();
  var empty=emptyFirst.ImportChecked(Wire(new byte[0],Text("A"),new byte[0]),false);
  Need(empty.Status==ArchiveStringImportStatus.Completed&&!empty.SourceIdsPreserved&&
   Pool(emptyFirst,new byte[0],Text("A")),"an explicit first empty string keeps ID zero and post does not add another");
  var zero=new ArchiveStringImportState();var zeroResult=zero.ImportChecked(Wire(),false);
  Need(zeroResult.Status==ArchiveStringImportStatus.Completed&&zeroResult.OriginalCount==0&&
   zeroResult.SourceIdsPreserved&&zeroResult.Cursor==4&&Pool(zero,new byte[0])&&zero.MappingCount==0,
   "zero-entry initial import still performs post empty publication");
 }
 static void ThreeArchivesScenario(){
  var state=new ArchiveStringImportState();
  var initial=state.ImportChecked(Wire(Text("alpha"),Text("beta"),Text("class")),false);
  Need(initial.Status==ArchiveStringImportStatus.Completed&&initial.SourceIdsPreserved&&
   Pool(state,Text("alpha"),Text("beta"),Text("class"),new byte[0]),"unique first pool preserves source IDs before post empty");
  var second=state.ImportChecked(Wire(Text("beta"),Text("new1"),Text("alpha"),Text("new1"),new byte[0]),true);
  Need(second.Status==ArchiveStringImportStatus.Completed&&second.BaseId==0&&second.OriginalCount==5&&
   state.MappingCount==5&&Mappings(state,0,1,4,0,4,3),"first append creates five mappings starting at zero, independent of initial pool size");
  var third=state.ImportChecked(Wire(Text("new1"),Text("new2"),Text("beta")),true);
  Need(third.Status==ArchiveStringImportStatus.Completed&&third.BaseId==5&&third.OriginalCount==3&&
   state.MappingCount==8&&Mappings(state,5,4,5,1),"second append starts after all five earlier raw entries, including duplicates");
  Need(Same(state.CopySelectors(),new uint[]{0,5})&&
   Pool(state,Text("alpha"),Text("beta"),Text("class"),new byte[0],Text("new1"),Text("new2")),
   "selectors track cumulative map positions while the shared pool tracks unique strings");
  Need(Remaps(state.CreateRemapContext(initial),0,1,0,1),
   "a historical initial handle remains identity-only even after append mappings exist");
  Need(Remaps(state.CreateRemapContext(second),0,1,1,4)&&Remaps(state.CreateRemapContext(third),0,1,4,5),
   "the same local key and value resolve differently through the two actual generated archive contexts");
  Need(!state.RequiresResolution&&Same(second.CopySourceString(3),Text("new1"))&&
   Same(third.CopySourceString(0),Text("new1")),"completed source snapshots retain each archive's local order");
  scenarios++;
 }
 static void ZeroAppendAndRawBytes(){
  var state=new ArchiveStringImportState();state.ImportChecked(Wire(Text("a")),false);
  var empty=state.ImportChecked(Wire(),true);
  var context=state.CreateRemapContext(empty);
  Need(empty.Status==ArchiveStringImportStatus.Completed&&empty.Append&&empty.BaseId==0&&empty.StateChanged&&
   state.MappingCount==0&&Same(state.CopySelectors(),new uint[]{0})&&context.TablePresent,
   "zero-entry append still publishes its selector and a present empty mapping table");
  var added=state.ImportChecked(Wire(Text("b")),true);
  Need(added.BaseId==0&&Mappings(state,0,2)&&Same(state.CopySelectors(),new uint[]{0,0}),
   "zero append does not consume mapping positions; post empty already consumed a shared-pool ID");
  Need(context.SignedMappingCount==0&&Remaps(context,0,1,0,1),
   "an already-created empty remap context is a snapshot rather than a view of later map growth");
  byte[][] raw={Text("a"),Text("A"),new byte[]{97,0},new byte[]{97,0,98},new byte[]{0x80,0xff},
   new byte[]{0xc3,0xa9},new byte[]{0x65,0xcc,0x81},new byte[0]};
  var bytes=new ArchiveStringImportState();var original=bytes.ImportChecked(Wire(raw),false);
  Need(original.Status==ArchiveStringImportStatus.Completed&&original.SourceIdsPreserved&&Pool(bytes,raw),
   "case, embedded/trailing NUL, high bytes, and composed/decomposed sequences remain distinct raw strings");
  var match=bytes.ImportChecked(Wire(raw[1],raw[3],raw[4],raw[5],raw[6],raw[0],new byte[]{0x80,0xfe}),true);
  Need(match.Status==ArchiveStringImportStatus.Completed&&Mappings(bytes,0,1,3,4,5,6,0,8)&&bytes.TargetStringCount==9,
   "append uses full byte equality without UTF8 validation, normalization, case folding, or NUL truncation");
 }
 static void LengthFallbackScenario(){
  var state=new ArchiveStringImportState();state.ImportChecked(Wire(Text("A")),false);
  byte[] wire=Join(Words(5,1),Text("B"),Words(1048577,1),Text("C"),Words(99,0));
  var result=state.ImportChecked(wire,true);
  Need(result.Status==ArchiveStringImportStatus.Completed&&result.Cursor==26&&
   result.EntriesProcessed==5&&result.LengthFallbacks==2&&Mappings(state,0,2,2,3,3,1),
   "invalid native lengths consume only their length word and reuse the prior temporary string");
  Need(Pool(state,Text("A"),new byte[0],Text("B"),Text("C"))&&
   Same(result.CopySourceString(1),Text("B"))&&Same(result.CopySourceString(3),Text("C"))&&
   result.CopySourceString(4).Length==0,"fallback snapshots and map cells record the actually reused string");
  var freshTemporary=state.ImportChecked(Words(1,1048577),true);
  Need(freshTemporary.Status==ArchiveStringImportStatus.Completed&&freshTemporary.LengthFallbacks==1&&
   freshTemporary.Cursor==8&&Mappings(state,5,1)&&freshTemporary.CopySourceString(0).Length==0,
   "a new import starts with an empty temporary rather than inheriting the previous archive's last string");
  var first=new ArchiveStringImportState();
  var firstResult=first.ImportChecked(Join(Words(2,1048577,1),Text("A")),false);
  Need(firstResult.Status==ArchiveStringImportStatus.Completed&&firstResult.SourceIdsPreserved&&
   firstResult.LengthFallbacks==1&&Pool(first,new byte[0],Text("A")),
   "initial fallback before any valid payload inserts empty first and can still preserve source IDs");
  scenarios++;
 }
 static void PartialImportAndOwnership(){
  var state=new ArchiveStringImportState();var initial=state.ImportChecked(Wire(Text("old")),false);
  byte[] broken=Join(Words(3,1),Text("x"),Words(3),Text("old"),new byte[]{1,0});
  var failed=state.ImportChecked(broken,true);
  Need(failed.Status==ArchiveStringImportStatus.Unsupported&&failed.Reason=="ArchiveStringLengthPrefixUnavailable"&&
   failed.Cursor==16&&failed.EntriesProcessed==2&&
   failed.SourceStringCount==2&&failed.StateChanged&&state.RequiresResolution,
   "a missing third length preserves the two completed entries and stops before reading partial length bytes");
  Need(state.MappingCount==3&&Same(state.CopySelectors(),new uint[]{0})&&Mappings(state,0,2,0)&&
   Pool(state,Text("old"),new byte[0],Text("x")),"append reservation and the completed pool/map prefix survive a later read-domain stop");
  uint value=123;
  Need(!state.TryGetMapping(2,out value)&&value==0&&state.TryGetMapping(1,out value)&&value==0,
   "an unknown reserved mapping slot is distinguishable from a known slot containing ID zero");
  Need(InvalidOperation(delegate{state.CreateRemapContext(failed);})&&
   InvalidOperation(delegate{state.CreateRemapContext(initial);}),"unresolved state cannot export either failed or historical completed contexts");
  var retry=state.ImportChecked(Wire(Text("should-not-appear")),true);
  Need(retry.Status==ArchiveStringImportStatus.Unsupported&&retry.Reason=="ArchiveStringImportRequiresResolution"&&
   !retry.StateChanged&&state.MappingCount==3&&
   Same(state.CopySelectors(),new uint[]{0})&&Pool(state,Text("old"),new byte[0],Text("x")),
   "a stopped state rejects another import without reserving or publishing any additional entries");
  var other=new ArchiveStringImportState();var otherResult=other.ImportChecked(Wire(Text("other")),false);
  var healthy=new ArchiveStringImportState();healthy.ImportChecked(Wire(Text("healthy")),false);
  Need(InvalidOperation(delegate{healthy.CreateRemapContext(otherResult);})&&
   InvalidOperation(delegate{healthy.CreateRemapContext(null);}),"result ownership and a real completed handle are required for context export");
  Need(OutOfRange(delegate{failed.CopySourceString(2);})&&OutOfRange(delegate{failed.CopySourceString(-1);}),
   "source export exposes only completed entries, not reserved but unread source IDs");
  scenarios++;
 }
 static void SnapshotIsolation(){
  byte[] wire=Wire(Text("left"),new byte[]{0x80,0,0xff});
  var state=new ArchiveStringImportState();var result=state.ImportChecked(wire,false);
  wire[8]=0;wire[16]=0;
  Need(Pool(state,Text("left"),new byte[]{0x80,0,0xff},new byte[0])&&Same(result.CopySourceString(0),Text("left")),
   "later mutation of borrowed wire does not change imported pool bytes or the source snapshot");
  byte[] source=result.CopySourceString(1);source[0]=1;
  byte[][] pool=state.CopyTargetStrings();pool[0][0]=1;pool[1]=new byte[]{2};
  Need(Same(result.CopySourceString(1),new byte[]{0x80,0,0xff})&&
   Pool(state,Text("left"),new byte[]{0x80,0,0xff},new byte[0]),"source and nested target-pool exports are independent copies");
  state.ImportChecked(Wire(Text("left")),true);uint[] selectors=state.CopySelectors();selectors[0]=999;
  Need(Same(state.CopySelectors(),new uint[]{0})&&Mappings(state,0,0),"selector export cannot mutate the generated mapping context");
  uint ignored;
  Need(!state.TryGetMapping(-1,out ignored)&&!state.TryGetMapping(state.MappingCount,out ignored),
   "mapping lookup rejects both negative and past-end indices without inventing cells");
 }
 static void OwnCoverageBudgets(){
  byte[][] invalid={null,new byte[3],Words(uint.MaxValue)};
  string[] reasons={"NullArchiveStringWire","ArchiveStringCountPrefixUnavailable","ArchiveStringCountOutsideCoveredDomain"};
  int[] cursors={0,0,4};
  for(int i=0;i<invalid.Length;i++){
   var state=new ArchiveStringImportState();var result=state.ImportChecked(invalid[i],false);
   Need(result.Status==ArchiveStringImportStatus.Unsupported&&result.Reason==reasons[i]&&result.Cursor==cursors[i]&&
    !result.StateChanged&&state.RequiresResolution&&
    state.TargetStringCount==0&&state.MappingCount==0,"own input/count rejection is sticky without claiming native writes or rollback");
  }
  var sourceCount=new ArchiveStringImportState();var sourceFailure=sourceCount.ImportChecked(Words(65537),false);
  Need(sourceFailure.Status==ArchiveStringImportStatus.Unsupported&&sourceFailure.Reason=="ArchiveStringSourceEntriesOutsideCoveredDomain"&&
   sourceFailure.Cursor==4&&sourceFailure.OriginalCount==65537&&!sourceFailure.StateChanged&&sourceCount.TargetStringCount==0,
   "initial source-count budget is checked before reading entries, independently of unique-string count");
  var oversizedWire=new ArchiveStringImportState();
  var wireFailure=oversizedWire.ImportChecked(new byte[4194305],false);
  Need(wireFailure.Status==ArchiveStringImportStatus.Unsupported&&wireFailure.Reason=="ArchiveStringWireOutsideCoveredDomain"&&
   !wireFailure.StateChanged&&oversizedWire.TargetStringCount==0,
   "wire larger than the own four-MiB budget is rejected before import state changes");
  byte[] padded=new byte[4194304];Array.Copy(Wire(Text("a")),padded,9);
  var exactWire=new ArchiveStringImportState();var exactWireResult=exactWire.ImportChecked(padded,false);
  Need(exactWireResult.Status==ArchiveStringImportStatus.Completed&&exactWireResult.Cursor==9&&
   Pool(exactWire,Text("a"),new byte[0]),"exact wire budget is accepted and bytes after the declared entries are not consumed");
  byte[] maximumString=new byte[65536];maximumString[0]=1;maximumString[65535]=0xff;
  var exactString=new ArchiveStringImportState();var stringResult=exactString.ImportChecked(Wire(maximumString),false);
  Need(stringResult.Status==ArchiveStringImportStatus.Completed&&Same(exactString.CopyTargetStrings()[0],maximumString),
   "the exact own per-string budget preserves every raw payload byte");
  var longString=new ArchiveStringImportState();longString.ImportChecked(Wire(Text("old")),false);
  var longResult=longString.ImportChecked(Wire(new byte[65537]),true);
  uint missing;
  Need(longResult.Status==ArchiveStringImportStatus.Unsupported&&longResult.Reason=="ArchiveStringBytesOutsideCoveredDomain"&&
   longResult.LengthFallbacks==0&&
   longResult.EntriesProcessed==0&&longString.MappingCount==1&&!longString.TryGetMapping(0,out missing)&&
   Pool(longString,Text("old"),new byte[0]),"valid native length beyond the own string budget stops after reservation; it is not the stale-length fallback");
  var entries=new byte[4095][];
  for(int i=0;i<entries.Length;i++)entries[i]=Words(checked((uint)i));
  var unique=new ArchiveStringImportState();var uniqueResult=unique.ImportChecked(Wire(entries),false);
  Need(uniqueResult.Status==ArchiveStringImportStatus.Completed&&unique.TargetStringCount==4096&&
   unique.CopyTargetStrings()[4095].Length==0,"4095 unique source strings plus post empty exactly fill the unique-string budget");
  var uniqueFailure=unique.ImportChecked(Wire(Text("new")),true);
  Need(uniqueFailure.Status==ArchiveStringImportStatus.Unsupported&&uniqueFailure.Reason=="ArchiveStringTargetCountOutsideCoveredDomain"&&
   unique.TargetStringCount==4096&&
   unique.MappingCount==1&&!unique.TryGetMapping(0,out missing),"a new unique string beyond budget leaves its reserved mapping slot unknown");
  var poolEntries=new byte[32][];
  for(int i=0;i<poolEntries.Length;i++){poolEntries[i]=new byte[65536];poolEntries[i][0]=checked((byte)i);}
  var poolState=new ArchiveStringImportState();var poolResult=poolState.ImportChecked(Wire(poolEntries),false);
  Need(poolResult.Status==ArchiveStringImportStatus.Completed&&poolState.TargetStringCount==33,
   "exactly two MiB of unique payload is allowed and post empty costs no payload bytes");
  var poolFailure=poolState.ImportChecked(Wire(new byte[]{99}),true);
  Need(poolFailure.Status==ArchiveStringImportStatus.Unsupported&&poolFailure.Reason=="ArchiveStringPoolBytesOutsideCoveredDomain"&&
   poolState.TargetStringCount==33&&
   poolState.MappingCount==1&&!poolState.TryGetMapping(0,out missing),"pool-byte overflow preserves the completed pool and unresolved append reservation");
  var mapState=new ArchiveStringImportState();mapState.ImportChecked(Wire(),false);
  var mapResult=mapState.ImportChecked(RepeatedWire(65536,new byte[0]),true);
  Need(mapResult.Status==ArchiveStringImportStatus.Completed&&mapState.MappingCount==65536&&
   Mappings(mapState,0,0)&&Mappings(mapState,65535,0),"the exact mapping budget includes a cell for every duplicate raw source entry");
  var mapFailure=mapState.ImportChecked(Wire(new byte[0]),true);
  Need(mapFailure.Status==ArchiveStringImportStatus.Unsupported&&mapFailure.Reason=="ArchiveStringMappingEntriesOutsideCoveredDomain"&&
   mapState.MappingCount==65536&&
   Same(mapState.CopySelectors(),new uint[]{0}),"mapping-budget rejection cannot append a selector or an unbounded new reservation");
 }
 public static int Main(){
  try{
   InitialOrderAndEmpty();ThreeArchivesScenario();ZeroAppendAndRawBytes();LengthFallbackScenario();
   PartialImportAndOwnership();SnapshotIsolation();OwnCoverageBudgets();
   Console.WriteLine("{\"suite\":\"ArchiveStringImportTests\",\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+
    ",\"scope\":\"synthetic archive string imports and generated remap contexts; no native execution\"}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.GetType().Name+": "+error.Message);return 1;}
 }
}
