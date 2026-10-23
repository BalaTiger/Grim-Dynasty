using System;
using GrimDynasty.TaskItems;
class RecordLoadFromSuppliedBinaryTests {
 static int assertions,scenarios;
 const ulong QueryAddress=0x12345008;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string value){var bytes=new byte[value.Length];for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)value[i];return bytes;}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static bool Same(int[] a,int[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static TableString S(string value){return new TableString(B(value));}
 static ObjectNameString Name(string value){return new ObjectNameString(B(value),(ulong)Math.Max(15,value.Length));}
 static byte[] Literal(){
  // Two actual tag-2 wire fields, including a collision under mask 3.
  return new byte[]{
   2,0,1,0, 0,0,0,0, 1,0,0,0,
   2,0,1,0, 4,0,0,0, 0,0,0,0
  };
 }
 static byte[] MixedWindow(){
  // Explicit [2,38) window: tag 0, tag 2/Class, tag 1. Outside bytes are poison.
  return new byte[]{
   0xff,0xff,
   0,0,1,0, 4,0,0,0, 0xef,0xbe,0xad,0xde,
   2,0,1,0, 0,0,0,0, 1,0,0,0,
   1,0,1,0, 8,0,0,0, 0,0,0,0x80,
   0xff,0xff,0xff
  };
 }
 static BinaryPointerCacheSnapshot Names(){return new BinaryPointerCacheSnapshot(0,0,new[]{new[]{new BinaryNameCacheEntry(QueryAddress,0)}});}
 static BinaryNameIdSnapshot NameIds(){return new BinaryNameIdSnapshot(1,new[]{new BinaryNameIdEntry(B("Class"),0)});}
 static SuppliedBinaryRecord Input(string key,uint? start,uint? end){
  byte[] bytes=Literal();return Input(B(key),bytes,0,bytes.Length,start,end);
 }
 static SuppliedBinaryRecord Input(byte[] key,byte[] bytes,int offset,int limit,uint? start,uint? end){
  return new SuppliedBinaryRecord(key,bytes,offset,limit,new RegistrationGate(),Names(),new[]{S("Class"),S("Item")},NameIds(),start,end);
 }
 static RecordDepotState Depot(RecordCacheSnapshot cache,float load,uint hits,uint loads,uint total,uint maximum){
  return new RecordDepotState(new RegistrationGate(),cache,hits,cache.BucketCount,load,loads,total,maximum);
 }
 static RecordCacheSnapshot Empty(int buckets){return new RecordCacheSnapshot(buckets,new RecordCacheEntry[0]);}
 static RecordTableGetterResult Class(object table){return RecordTableGetter.GetValue(table,B("Class\0"),S("fallback"),QueryAddress);}
 static bool Ring(RecordCachePublicationInspection snapshot){
  int[] next=snapshot.CopyNextLinks(),previous=snapshot.CopyPreviousLinks(),order=snapshot.CopyListOrder();
  if(order.Length!=snapshot.Count||next.Length!=previous.Length)return false;
  int current=0;var seen=new bool[next.Length];seen[0]=true;
  foreach(int id in order){
   if(id<=0||id>=next.Length||seen[id]||next[current]!=id||previous[id]!=current)return false;
   seen[id]=true;current=id;
  }
  return next[current]==0&&previous[0]==current;
 }
 static void FirstHitAndSecondMissScenario(){
  byte[] key=B("dir/file"),bytes=MixedWindow();var item=S("Item");var pool=new[]{S("Class"),item};var tableGate=new RegistrationGate();
  var supplied=new SuppliedBinaryRecord(key,bytes,2,38,tableGate,Names(),pool,NameIds(),100,105);
  key[0]=0;bytes[2]=3;bytes[22]=0xff;pool[1]=S("Changed");
  var original=new ObjectNameString(B("Dir\\FILE"),63);byte[] originalBytes=original.CopyBytes();
  var depot=Depot(Empty(8),1,4,9,20,7);
  var attempt=new RecordLoadFromSuppliedBinaryAttempt(depot,original,RecordNameLocale.KnownC,supplied);
  Need(!attempt.Attempted&&!attempt.Trace.Entered&&depot.Cache.Count==0,"attempt construction does not read archive or publish a table");
  var result=attempt.RunOnce();var trace=attempt.Trace;object table=result.Table;
  Need(result.Status=="Known"&&table!=null&&!result.CacheHit&&!result.HitCountIncremented&&result.MayHaveEffects&&Object.ReferenceEquals(result.SuppliedMiss,trace),"first supplied miss reaches a real known table without pretending it was a cache hit");
  Need(trace.Parser.Status=="Known"&&trace.Parser.CompletedFields==3&&trace.Parser.InitialPosition==2&&trace.Parser.FinalPosition==38&&trace.Parser.FirstPassEnd==38&&trace.Parser.ReaderRewound,"isolated mixed literal window drives both actual parser passes");
  var parsed=trace.Parser.Record;
  Need(parsed.CopyRawValues(0)[0]==0xdeadbeefU&&parsed.CopyRawValues(1)[0]==0x80000000U&&parsed.CopyValues()[0]==1,"three parsed vectors preserve independent raw words rather than prefilled getter fields");
  Need(trace.ReaderDisposed&&trace.Reader.IsDisposed&&trace.Reader.Consumed&&trace.Reader.Position==38&&trace.Reader.Gate==tableGate&&trace.Table.Gate==tableGate&&!tableGate.IsHeldByCurrentThread,"own consumed reader is cleaned up while the table retains its separate state");
  Need(Object.ReferenceEquals(table,trace.Table)&&Object.ReferenceEquals(trace.TemporaryPair.Table,table)&&trace.TemporaryPair.Name.Length==0&&trace.TemporaryPair.Name.Capacity==15,"the same parsed table passes through a moved temporary node pair");
  Need(trace.Publication.Status=="Known"&&trace.Publication.NodeConstruction.Status=="Known"&&trace.Publication.CountIncremented&&trace.Publication.HeadPreviousWritten&&trace.Publication.PreviousNextWritten&&trace.Publication.Publication.Inserted==true,"actual node construction precedes all three caller writes and indexing");
  Need(trace.CacheCommitted&&trace.LoadCountIncremented&&trace.ElapsedRecorded&&trace.Elapsed==5&&!trace.RequiresResolution&&!depot.RequiresResolution&&depot.PendingPublication==null&&depot.Cache.Count==1&&Ring(trace.CallerPublication.Inspect()),"completed graph commits to the same depot and clears pending state");
  Need(depot.LoadCount50==10&&depot.HitCount54==4&&depot.ElapsedTotal5C==25&&depot.ElapsedMaximum60==7&&result.LockAcquired&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"first miss updates only its load/timing counters and releases the depot gate");
  Need(Same(result.CopyNormalizedName(),B("dir/file"))&&Same(original.CopyBytes(),originalBytes)&&original.Capacity==63&&!trace.NativeArchiveRead&&!trace.NativeFactoryRecovered,"full normalized key is bound while the original requested name is neither moved nor rewritten");
  var getter=Class(table);
  Need(getter.Status=="Known"&&Object.ReferenceEquals(getter.Value,item)&&!getter.UsedDefault&&!getter.MayHaveEffects&&getter.GateReleased&&!tableGate.IsHeldByCurrentThread,"actual Binary getter returns original shared pool identity after reader disposal and input-array mutation");
  var parser=trace.Parser;var reader=trace.Reader;var node=trace.Publication.NodeConstruction.Node;
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&Object.ReferenceEquals(trace.Parser,parser)&&Object.ReferenceEquals(trace.Reader,reader)&&Object.ReferenceEquals(trace.Publication.NodeConstruction.Node,node)&&depot.LoadCount50==10&&depot.HitCount54==4,"same attempt returns the identical result without reparsing, moving or recounting");
  var hit=RecordLoadFile.Apply(depot,Name("DIR\\FILE"),RecordNameLocale.KnownC);
  Need(hit.Status=="Known"&&hit.CacheHit&&hit.HitCountIncremented&&hit.SuppliedMiss==null&&Object.ReferenceEquals(hit.Table,table)&&depot.HitCount54==5,"ordinary Apply subsequently hits the newly committed table identity");
  Need(depot.LoadCount50==10&&depot.ElapsedTotal5C==25&&reader.IsDisposed&&reader.Position==38&&Object.ReferenceEquals(trace.Parser,parser)&&trace.TemporaryPair.Name.Length==0,"ordinary hit performs no supplied parse, move or miss-tail work");
  var hitOnly=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("dir/file"),RecordNameLocale.KnownC,null);var hitAgain=hitOnly.RunOnce();
  Need(hitAgain.Status=="Known"&&Object.ReferenceEquals(hitAgain.Table,table)&&!hitOnly.Trace.Entered&&hitOnly.Trace.Parser==null&&depot.HitCount54==6,"cache hit needs no supplied input or miss context");
  var second=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("SECOND"),RecordNameLocale.KnownC,Input("second",200,211));
  var secondResult=second.RunOnce();
  Need(secondResult.Status=="Known"&&!Object.ReferenceEquals(secondResult.Table,table)&&depot.Cache.Count==2&&depot.LoadCount50==11&&depot.HitCount54==6&&depot.ElapsedTotal5C==36&&depot.ElapsedMaximum60==11,"second new key creates a separate table without losing the first cache entry");
  Need(Object.ReferenceEquals(depot.Cache.Find(B("dir/file")).Entry.Table,table)&&Object.ReferenceEquals(depot.Cache.Find(B("second")).Entry.Table,secondResult.Table)&&Ring(second.Trace.CallerPublication.Inspect()),"second caller adopts and preserves the first table's completed index");
  Need(Object.ReferenceEquals(Class(table).Value,item)&&Class(secondResult.Table).Status=="Known"&&Same(original.CopyBytes(),originalBytes),"both actual table getters remain usable and the caller's original source name stays intact");
  scenarios++;
 }
 static void InputRejection(RecordDepotState depot,ObjectNameString requested,SuppliedBinaryRecord input,string reason){
  var old=depot.LastCompletedCache;uint hits=depot.HitCount54,loads=depot.LoadCount50;
  var attempt=new RecordLoadFromSuppliedBinaryAttempt(depot,requested,RecordNameLocale.KnownC,input);var result=attempt.RunOnce();var trace=attempt.Trace;
  Need(result.Status=="Unsupported"&&result.Reason==reason&&trace.Entered&&trace.FailureStage=="SuppliedInput"&&trace.Reader==null&&trace.Parser==null&&trace.Table==null,"input boundary refuses missing or unbound data before parsing: "+reason);
  Need(!result.MayHaveEffects&&!trace.RequiresResolution&&!depot.RequiresResolution&&Object.ReferenceEquals(depot.Cache,old)&&depot.HitCount54==hits&&depot.LoadCount50==loads&&!trace.NativeArchiveRead&&!trace.NativeFactoryRecovered&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"input refusal does not pretend to fetch native data or mutate the depot");
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&trace.Parser==null&&trace.TemporaryPair==null,"input refusal remains sticky without creating a parser or node");
 }
 static void InputGuards(){
  InputRejection(Depot(Empty(8),1,3,4,5,6),Name("a"),null,"UnimplementedRecordArchiveFetch");
  InputRejection(Depot(Empty(8),1,3,4,5,6),Name(""),Input("",0,1),"SuppliedBinaryMissEmptyNameTakesFallback");
  InputRejection(Depot(Empty(8),1,3,4,5,6),Name("a"),Input("A",0,1),"SuppliedRecordNameMismatch");
  InputRejection(Depot(Empty(8),1,3,4,5,6),Name("a\0tail"),Input("a",0,1),"SuppliedRecordNameMismatch");
  InputRejection(new RecordDepotState(new RegistrationGate(),Empty(8),3),Name("a"),Input("a",0,1),"UnknownRecordCachePublicationConfiguration");
  InputRejection(Depot(Empty(8),1,3,4,5,6),Name("a"),Input("a",null,1),"UnknownConvertedMissStartTick");
 }
 static void ParserCoverageRejections(){
  foreach(bool invalidTag in new[]{true,false}){
   byte[] bytes=Literal();if(invalidTag)bytes[0]=4;
   var supplied=Input(B("bad"),bytes,0,invalidTag?bytes.Length:bytes.Length-1,0,1);
   var depot=Depot(Empty(8),1,3,4,5,6);var old=depot.Cache;
   var attempt=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("bad"),RecordNameLocale.KnownC,supplied);
   var result=attempt.RunOnce();var trace=attempt.Trace;
   string reason=invalidTag?"BinaryRecordTagOutsideCoveredDomain":"BinaryRecordPayloadOutsideCoveredDomain";
   Need(result.Status=="Unsupported"&&result.Reason==reason&&trace.Parser!=null&&trace.Parser.DomainRejected&&trace.Parser.Record==null&&!trace.Parser.MayHaveEffects,"malformed input is an own parser coverage stop, not a native false return");
   Need(trace.ReaderDisposed&&trace.Reader.IsDisposed&&!trace.Reader.Consumed&&trace.Reader.Position==0&&trace.Parser.FinalPosition==0&&trace.Parser.CompletedFields==0&&trace.Parser.GateReleased&&!supplied.TableGate.IsHeldByCurrentThread,"preflight rejection retains the initial cursor and still disposes own reader storage");
   Need(trace.Table!=null&&result.Table==null&&trace.TemporaryPair==null&&trace.CallerPublication==null&&trace.Publication==null&&!trace.CacheCommitted&&!trace.LoadCountIncremented&&!result.MayHaveEffects,"a prepared fresh table is not exposed as a successfully parsed or published fallback");
   Need(!depot.RequiresResolution&&Object.ReferenceEquals(depot.Cache,old)&&depot.Cache.Count==0&&depot.LoadCount50==4&&depot.HitCount54==3&&depot.ElapsedTotal5C==5&&depot.ElapsedMaximum60==6&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"preflight stop leaves cache and every tail counter unchanged");
   var parser=trace.Parser;var reader=trace.Reader;
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&Object.ReferenceEquals(trace.Parser,parser)&&Object.ReferenceEquals(trace.Reader,reader)&&reader.IsDisposed&&!reader.Consumed,"outer attempt does not retry even a nonconsuming parser coverage refusal");
  }
 }
 static void GrowthStopScenario(){
  object oldTable=new object();var old=new RecordCacheSnapshot(4096,new[]{new RecordCacheEntry(B("old"),oldTable)});
  var depot=Depot(old,1.0f/4096.0f,3,4,5,6);
  var attempt=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("new"),RecordNameLocale.KnownC,Input("new",10,20));
  var result=attempt.RunOnce();var trace=attempt.Trace;var publication=trace.Publication;
  Need(result.Status=="Unsupported"&&result.Reason=="RecordCacheGrowthBucketBudgetOutsideCoveredDomain"&&trace.FailureStage=="CallerPublication"&&result.Table==null,"growth coverage stop propagates instead of claiming a returned loaded table");
  Need(trace.Parser.Status=="Known"&&trace.ReaderDisposed&&trace.Reader.Consumed&&trace.Reader.IsDisposed&&trace.TemporaryPair.Name.Length==0&&trace.TemporaryPair.Name.Capacity==15&&publication.NodeConstruction.SourceNameCleared,"successful parse and moved node name survive later publication refusal");
  Need(publication.CountIncremented&&publication.HeadPreviousWritten&&publication.PreviousNextWritten&&publication.Publication.Status=="Unsupported"&&publication.StopAddress=="0x18002eae9"&&publication.MayHaveEffects,"all three caller writes remain before the growth stopping address");
  var graph=trace.CallerPublication.Inspect();int[] order=graph.CopyListOrder();
  Need(graph.Count==2&&graph.BucketCount==4096&&Ring(graph)&&Object.ReferenceEquals(graph.EntryForNode(1).Table,trace.Table)&&graph.CopyBucketFirst()[(int)(RecordCacheSnapshot.HashBytes(B("new"))&4095UL)]==1,"live partial graph retains the new candidate and its written bucket endpoint");
  Need(result.MayHaveEffects&&trace.RequiresResolution&&depot.RequiresResolution&&depot.Cache==null&&Object.ReferenceEquals(depot.LastCompletedCache,old)&&Object.ReferenceEquals(depot.PendingPublication,trace.CallerPublication)&&depot.CurrentCacheCount==2,"pending depot hides old complete view while retaining its actual unresolved graph");
  Need(!trace.CacheCommitted&&!trace.LoadCountIncremented&&!trace.ElapsedRecorded&&depot.LoadCount50==4&&depot.HitCount54==3&&depot.ElapsedTotal5C==5&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"growth refusal does not run miss-tail counters");
  var blocked=RecordLoadFile.Apply(depot,Name("old"),RecordNameLocale.KnownC);
  Need(blocked.Status=="Unsupported"&&blocked.Reason=="RecordDepotMissRequiresResolution"&&blocked.Lookup==null&&blocked.Table==null&&!blocked.HitCountIncremented&&depot.HitCount54==3&&blocked.LockReleased,"even the old key cannot masquerade as a normal live cache hit while pending");
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&trace.CallerPublication.Count==2&&Same(trace.CallerPublication.Inspect().CopyListOrder(),order)&&depot.LoadCount50==4,"replaying the stopped outer attempt repeats no parse, count or links");
  int constructors=0,reads=0;
  var classes=new ClassInfoSnapshot(B("Item\0"),null,null,delegate{constructors++;return ObjectBaseConstructor.Create();});
  var created=CreatePartialObject.Apply(depot,Name("old"),RecordNameLocale.KnownC,classes,QueryAddress);
  Need(created.Status=="Unsupported"&&created.Reason=="RecordLoadUnsupported:RecordDepotMissRequiresResolution"&&created.Getter==null&&created.Factory==null&&constructors==0,"CreatePartial stops at the pending depot before getter or supplied factory");
  var instance=ObjectBaseConstructor.Create();ObjectNameAssignment.Apply(instance,Name("old"));instance.Fields.Id2C=123;
  var finished=FinishPartialObject.Apply(null,depot,instance,77,RecordNameLocale.KnownC,delegate(RegistrationObject value,object table){reads++;});
  Need(finished.Status=="Unsupported"&&finished.Reason=="RecordLoadUnsupported:RecordDepotMissRequiresResolution"&&finished.Registration==null&&reads==0&&instance.Fields.Id2C==123&&depot.HitCount54==3,"FinishPartial cannot register or Read through a pending depot");
  scenarios++;
 }
 static void UnknownEndTickScenario(){
  var old=Empty(8);var depot=Depot(old,1,2,8,12,6);
  var attempt=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("item"),RecordNameLocale.KnownC,Input("item",10,null));
  var result=attempt.RunOnce();var trace=attempt.Trace;
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownConvertedMissEndTick"&&trace.FailureStage=="ConvertedMissEndTick"&&trace.LastCompletedStage=="LoadCountIncremented"&&result.Table==null,"unknown converted end tick stops only after the observed publication and load-count prefix");
  Need(trace.CacheCommitted&&trace.LoadCountIncremented&&!trace.ElapsedRecorded&&!trace.Elapsed.HasValue&&trace.Publication.Status=="Known"&&depot.LoadCount50==9&&depot.ElapsedTotal5C==12&&depot.ElapsedMaximum60==6,"timing refusal retains committed cache and +50 without fabricating elapsed time");
  Need(depot.RequiresResolution&&trace.RequiresResolution&&depot.Cache==null&&!Object.ReferenceEquals(depot.LastCompletedCache,old)&&depot.LastCompletedCache.Count==1&&Object.ReferenceEquals(depot.LastCompletedCache.Find(B("item")).Entry.Table,trace.Table)&&old.Count==0,"latest completed cache is diagnostic only until the miss tail resolves");
  Need(trace.ReaderDisposed&&trace.Reader.IsDisposed&&trace.Reader.Consumed&&trace.TemporaryPair.Name.Length==0&&Object.ReferenceEquals(trace.TemporaryPair.Table,trace.Table)&&result.MayHaveEffects&&result.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"reader cleanup and table ownership survive a tail stop");
  var getter=Class(trace.Table);
  Need(getter.Status=="Known"&&getter.Value!=null&&getter.GateReleased,"retained parsed table remains inspectable despite no outer success return");
  var ordinary=RecordLoadFile.Apply(depot,Name("item"),RecordNameLocale.KnownC);
  Need(ordinary.Status=="Unsupported"&&ordinary.Reason=="RecordDepotMissRequiresResolution"&&depot.HitCount54==2&&ordinary.Table==null,"published entry is not exposed as a live hit before tail resolution");
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&depot.LoadCount50==9&&depot.LastCompletedCache.Count==1&&depot.ElapsedTotal5C==12,"sticky end-tick result prevents a second count or publication");
  scenarios++;
 }
 static void UnsignedTimingScenario(){
  var depot=Depot(Empty(8),1,7,uint.MaxValue,uint.MaxValue-2,4);
  var first=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("one"),RecordNameLocale.KnownC,Input("one",uint.MaxValue-2,2));
  var a=first.RunOnce();
  Need(a.Status=="Known"&&first.Trace.Elapsed==5&&depot.LoadCount50==0&&depot.ElapsedTotal5C==2&&depot.ElapsedMaximum60==5,"32-bit converted tick subtraction, load increment and elapsed accumulation wrap unsigned");
  var second=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("two"),RecordNameLocale.KnownC,Input("two",0,0x80000001U));
  var b=second.RunOnce();
  Need(b.Status=="Known"&&second.Trace.Elapsed==0x80000001U&&depot.ElapsedTotal5C==0x80000003U&&depot.ElapsedMaximum60==0x80000001U&&depot.LoadCount50==1,"elapsed high bit remains unsigned when compared with the previous small maximum");
  var third=new RecordLoadFromSuppliedBinaryAttempt(depot,Name("three"),RecordNameLocale.KnownC,Input("three",10,11));
  var c=third.RunOnce();
  Need(c.Status=="Known"&&third.Trace.Elapsed==1&&depot.ElapsedTotal5C==0x80000004U&&depot.ElapsedMaximum60==0x80000001U&&depot.LoadCount50==2&&depot.HitCount54==7,"later small elapsed value cannot replace an unsigned high-bit maximum");
  Need(depot.Cache.Count==3&&!depot.RequiresResolution&&Object.ReferenceEquals(depot.Cache.Find(B("one")).Entry.Table,a.Table)&&Object.ReferenceEquals(depot.Cache.Find(B("two")).Entry.Table,b.Table)&&Object.ReferenceEquals(depot.Cache.Find(B("three")).Entry.Table,c.Table),"sequential real miss completions retain all previous table identities");
  scenarios++;
 }
 static int Main(){
  try{
   FirstHitAndSecondMissScenario();InputGuards();ParserCoverageRejections();GrowthStopScenario();UnknownEndTickScenario();UnsignedTimingScenario();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"scope\":\"explicit verified borrowed bytes and archive context through actual bounded parser, node/caller/index publication, same-depot hits and guarded miss-tail effects; no native archive fetch, factory, malformed-input ABI or exception cleanup claim\"}");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
