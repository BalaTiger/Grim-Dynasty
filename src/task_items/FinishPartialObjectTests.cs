using System;
using System.Text;
using GrimDynasty.TaskItems;
class FinishPartialObjectTests {
 static int n;
 const uint Id=77,FullNameKey=0xb0853751;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static byte[] B(string value){return Encoding.ASCII.GetBytes(value);}
 static bool Same(byte[] left,byte[] right){if(left==null||right==null)return left==right;if(left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static byte[] Repeated(int count,byte value){var bytes=new byte[count];for(int i=0;i<count;i++)bytes[i]=value;return bytes;}
 static ObjectNameString Name(byte[] bytes){return new ObjectNameString(bytes,Math.Max(15UL,(ulong)bytes.Length));}
 static void Assign(ObjectBaseState value,byte[] bytes){if(ObjectNameAssignment.Apply(value,Name(bytes)).Status!="Known")throw new Exception("invalid name fixture");}
 static ObjectBaseState ObjectWithName(byte[] bytes){var value=ObjectBaseConstructor.Initialize(new RegistrationObject(123,null));Assign(value,bytes);return value;}
 static RegistrationIndex Index(params RegistrationEntry[] entries){return new RegistrationIndex(8,8,1.0f,entries);}
 static ObjectRegistrationState Manager(uint id=Id){return new ObjectRegistrationState(new RegistrationGate(),Index(new RegistrationEntry(id,new object())),Index(),Index());}
 static RecordDepotState Depot(byte[] key,object table,uint hits=0){return new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new[]{new RecordCacheEntry(key,table)}),hits);}
 static void Guards(){
  var result=FinishPartialObject.Apply(null,null,null,null,RecordNameLocale.Unknown,null);
  Need(result.Status=="Known"&&result.LastCompletedStage=="NoObject"&&result.Instance==null&&result.Load==null&&result.Registration==null&&!result.DiagnosticRequired&&!result.MayHaveEffects&&!result.CStringLength.HasValue&&!result.TemporaryNameCapacity.HasValue&&result.CopyRecordName()==null,"null object takes precedence over unknown ID and all unavailable dependencies");
  var value=ObjectBaseConstructor.Create();
  result=FinishPartialObject.Apply(null,null,value,0,RecordNameLocale.Unknown,delegate{throw new Exception("unreached Read");});
  Need(result.Status=="Known"&&result.LastCompletedStage=="ZeroId"&&Object.ReferenceEquals(result.Instance,value)&&result.Load==null&&result.Registration==null&&!result.MayHaveEffects&&!result.DiagnosticRequired&&result.CopyRecordName()==null&&!value.Fields.Id2C.HasValue&&!value.Fields.RawKey28.HasValue,"zero ID returns before reading name, depot, manager or Read metadata");
  result=FinishPartialObject.Apply(null,null,value,null,RecordNameLocale.Unknown,null);
  Need(result.Status=="Unsupported"&&result.Reason=="UnknownRegistrationId"&&result.FailureStage=="Guards"&&result.LastCompletedStage=="Start"&&result.Load==null&&result.Registration==null&&!result.MayHaveEffects&&result.CopyRecordName()==null&&!value.Fields.Id2C.HasValue,"unknown ID is distinct from a known zero and stops before any name snapshot or load");
 }
 static void CStringAndLocale(){
  byte[] original={0x41,0,0x42,0x80};var value=ObjectWithName(original);uint? raw=value.Fields.RawKey28;var name=value.Name;var fields=value.Fields;var table=new object();var fullTable=new object();
  var depot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new[]{new RecordCacheEntry(original,fullTable),new RecordCacheEntry(B("a"),table)}),0);
  var manager=Manager();int reads=0;object readTable=null;
  var result=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.KnownC,delegate(RegistrationObject incoming,object loaded){reads++;readTable=loaded;});
  Need(result.Status=="Known"&&result.CStringLength==1&&result.TemporaryNameCapacity==15&&Same(result.CopyRecordName(),B("A"))&&Same(result.Load.CopyNormalizedName(),B("a"))&&Object.ReferenceEquals(result.Load.Table,table)&&!Object.ReferenceEquals(result.Load.Table,fullTable)&&Object.ReferenceEquals(readTable,table)&&reads==1&&depot.HitCount54==1,"only the pre-NUL name enters normalization and reload; a high-byte suffix cannot reject or select the full-name table");
  Need(Object.ReferenceEquals(result.Instance,value)&&Object.ReferenceEquals(value.Name,name)&&Object.ReferenceEquals(value.Fields,fields)&&Same(value.CopyNameBytes(),original)&&fields.RawKey28==raw&&fields.Id2C==Id&&result.Registration.RawKeyInsert.Node.Key==raw.Value&&Object.ReferenceEquals(result.Registration.RawKeyInsert.Value,fields),"prefix loading preserves complete original name, name identity and full-name raw registration key");
  byte[] prefixCopy=result.CopyRecordName(),normalizedCopy=result.Load.CopyNormalizedName();prefixCopy[0]=0;normalizedCopy[0]=0;
  Need(Same(result.CopyRecordName(),B("A"))&&Same(result.Load.CopyNormalizedName(),B("a"))&&Same(value.CopyNameBytes(),original),"returned prefix and normalized buffers are defensive copies rather than exposed name storage");
  byte[][] names={new byte[]{0,0x80},new byte[]{0x5c,0x5c,0,0xff}};byte[][] keys={new byte[0],B("//")};
  for(int i=0;i<names.Length;i++){
   value=ObjectWithName(names[i]);depot=Depot(keys[i],table);manager=Manager();reads=0;
   result=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.Unknown,delegate{reads++;});
   Need(result.Status=="Known"&&reads==1&&result.CStringLength==(i==0?0:2)&&Same(result.CopyRecordName(),i==0?new byte[0]:B("\\\\"))&&Same(result.Load.CopyNormalizedName(),keys[i])&&depot.HitCount54==1&&Same(value.CopyNameBytes(),names[i]),"empty or all-backslash prefix needs no tolower locale and ignores its high-byte suffix: "+i);
  }
  value=ObjectWithName(new byte[]{0x41,0,0x80});depot=Depot(B("a"),table,5);manager=Manager();raw=value.Fields.RawKey28;
  result=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.Unknown,null);
  Need(result.Status=="Unsupported"&&result.Reason=="RecordLoadUnsupported:UnknownCrtToLowerLocale"&&result.FailureStage=="LoadFile"&&result.LastCompletedStage=="NameSnapshotCopied"&&result.CStringLength==1&&Same(result.CopyRecordName(),B("A"))&&result.Registration==null&&!result.DiagnosticRequired&&!result.MayHaveEffects&&depot.HitCount54==5&&value.Fields.Id2C==123&&value.Fields.RawKey28==raw&&result.Load.LockReleased,"a reached ASCII prefix still needs the explicit locale even when the ignored suffix begins with NUL");
 }
 static void NullTableAndUnfinishedLoad(){
  byte[][] names={B("ITEM"),new byte[0]};
  for(int i=0;i<names.Length;i++){
   var value=ObjectWithName(names[i]);uint? raw=value.Fields.RawKey28;var manager=Manager();var depot=Depot(i==0?B("item"):new byte[0],null,uint.MaxValue);int reads=0;
   var result=FinishPartialObject.Apply(manager,depot,value,Id,i==0?RecordNameLocale.KnownC:RecordNameLocale.Unknown,delegate{reads++;});
   Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedLoadFailureDiagnostic"&&result.LastCompletedStage=="LoadedNullTable"&&result.FailureStage=="LoadFailureDiagnostic"&&result.DiagnosticRequired&&result.Load.Status=="Known"&&result.Load.CacheHit&&result.Load.Table==null&&result.Load.HitCountIncremented&&depot.HitCount54==0&&result.MayHaveEffects,"a counted known-null table requires the unresolved diagnostic, including an empty name: "+i);
   Need(result.Registration==null&&value.Fields.Id2C==123&&value.Fields.RawKey28==raw&&manager.Pending128.Count==1&&manager.ById30.Count==0&&manager.ByRawKey70.Count==0&&reads==0&&!depot.Gate.IsHeldByCurrentThread&&!manager.Gate.IsHeldByCurrentThread&&Same(value.CopyNameBytes(),names[i]),"null table preserves its load effects but performs no ID write, registration or Read: "+i);
  }
  var missing=ObjectWithName(B("item"));var emptyDepot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new RecordCacheEntry[0]),9);
  var miss=FinishPartialObject.Apply(null,emptyDepot,missing,Id,RecordNameLocale.KnownC,null);
  Need(miss.Status=="Unsupported"&&miss.Reason=="RecordLoadUnsupported:UnimplementedRecordCacheMissLoader"&&miss.FailureStage=="LoadFile"&&miss.LastCompletedStage=="NameSnapshotCopied"&&miss.Load.Lookup!=null&&!miss.Load.Lookup.Found&&!miss.DiagnosticRequired&&miss.Registration==null&&!miss.MayHaveEffects&&emptyDepot.HitCount54==9&&missing.Fields.Id2C==123,"an unfinished cache miss is not a known-null load and cannot trigger the null-table diagnostic");
  var unknownCache=new RecordDepotState(new RegistrationGate(),null,11);var unknown=FinishPartialObject.Apply(null,unknownCache,missing,Id,RecordNameLocale.KnownC,null);
  Need(unknown.Status=="Unsupported"&&unknown.Reason=="RecordLoadUnsupported:UnknownRecordCacheSnapshot"&&unknown.Load.FailureStage=="CacheLookup"&&unknown.Load.LockReleased&&unknown.Registration==null&&!unknown.DiagnosticRequired&&!unknown.MayHaveEffects&&unknownCache.HitCount54==11&&missing.Fields.Id2C==123,"unknown cache metadata preserves the earlier prefix snapshot without inventing table failure or registration");
 }
 static void RegistrationOrdering(){
  var table=new object();var value=ObjectWithName(B("ITEM"));var depot=Depot(B("item"),table);
  var result=FinishPartialObject.Apply(null,depot,value,Id,RecordNameLocale.KnownC,null);
  Need(result.Status=="Unsupported"&&result.Reason=="RegistrationUnsupported:UnknownObjectRegistrationState"&&result.FailureStage=="Registration"&&result.LastCompletedStage=="LoadFileComplete"&&result.Registration.IdAssigned&&!result.Registration.LockAcquired&&value.Fields.Id2C==Id&&depot.HitCount54==1&&result.MayHaveEffects&&!result.DiagnosticRequired,"unknown manager is reached after the successful load and the existing registration ID write");
  value=ObjectWithName(B("ITEM"));depot=Depot(B("item"),table);var manager=new ObjectRegistrationState(null,Index(new RegistrationEntry(Id,new object())),Index(),Index());
  result=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.KnownC,null);
  Need(result.Status=="Unsupported"&&result.Reason=="RegistrationUnsupported:UnknownRegistrationGate"&&result.Registration.IdAssigned&&!result.Registration.LockAcquired&&value.Fields.Id2C==Id&&depot.HitCount54==1&&manager.Pending128.Count==1&&manager.ById30.Count==0&&manager.ByRawKey70.Count==0&&result.MayHaveEffects,"missing registration gate does not move its guard before load or ID assignment");
  value=ObjectWithName(B("ITEM"));value.Fields.RawKey28=null;depot=Depot(B("item"),table);manager=Manager();int reads=0;
  result=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.KnownC,delegate{reads++;});
  Need(result.Status=="Unsupported"&&result.Reason=="RegistrationUnsupported:UnknownRawRegistrationKey"&&result.Registration.FailureStage=="RawKeyRead"&&result.Registration.PendingErase.RemovedCount==1&&result.Registration.IdInsert.Inserted==true&&result.Registration.RawKeyInsert==null&&result.Registration.LockReleased&&manager.Pending128.Count==0&&manager.ById30.Count==1&&manager.ByRawKey70.Count==0&&value.Fields.Id2C==Id&&!value.Fields.RawKey28.HasValue&&reads==0&&depot.HitCount54==1&&result.MayHaveEffects,"unknown raw key is discovered only after the preserved pending erase and first insertion");
  value=ObjectWithName(B("ITEM"));depot=Depot(B("item"),table);manager=Manager();bool unlocked=false;reads=0;
  result=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.KnownC,delegate(RegistrationObject incoming,object loaded){reads++;unlocked=!depot.Gate.IsHeldByCurrentThread&&!manager.Gate.IsHeldByCurrentThread&&Object.ReferenceEquals(loaded,table);incoming.Id2C=999;throw new InvalidOperationException("own Read boundary");});
  Need(result.Status=="Unsupported"&&result.Reason=="RegistrationUnsupported:OwnReadCallbackThrew:System.InvalidOperationException"&&result.Registration.ReadInvoked&&result.Registration.LockReleased&&reads==1&&unlocked&&value.Fields.Id2C==999&&manager.Pending128.Count==0&&manager.ById30.Count==1&&manager.ByRawKey70.Count==1&&depot.HitCount54==1&&result.MayHaveEffects&&!result.DiagnosticRequired,"own Read failure preserves its side effects and all completed load/registration changes outside both gates");
 }
 static void RawIdsAndLengths(){
  foreach(uint id in new uint[]{0x80000000,uint.MaxValue}){
   var value=ObjectWithName(B("item"));var manager=Manager(id);var table=new object();var depot=Depot(B("item"),table);int reads=0;
   var result=FinishPartialObject.Apply(manager,depot,value,id,RecordNameLocale.KnownC,delegate(RegistrationObject incoming,object loaded){if(incoming.Id2C==id&&Object.ReferenceEquals(loaded,table))reads++;});
   Need(result.Status=="Known"&&reads==1&&value.Fields.Id2C==id&&result.Registration.IdInsert.Node.Key==id&&manager.Pending128.Count==0&&result.Registration.LockReleased,"nonzero IDs retain all raw uint bits through registration: "+id);
  }
  int[] lengths={15,16,65536};ulong[] capacities={15,31,65551};
  for(int i=0;i<lengths.Length;i++){
   byte[] bytes=Repeated(lengths[i],0x61);var value=ObjectWithName(bytes);uint? raw=value.Fields.RawKey28;var table=new object();var depot=Depot(bytes,table);var manager=Manager();int reads=0;
   var result=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.KnownC,delegate{reads++;});
   Need(result.Status=="Known"&&result.CStringLength==lengths[i]&&result.TemporaryNameCapacity==capacities[i]&&result.Load.LocalNameCapacity==capacities[i]&&Same(result.CopyRecordName(),bytes)&&Same(value.CopyNameBytes(),bytes)&&value.Fields.RawKey28==raw&&reads==1&&depot.HitCount54==1,"the logical trailing NUL closes a fresh temporary at covered length "+lengths[i]);
  }
  var retained=ObjectWithName(Repeated(200,0x61));Assign(retained,Repeated(16,0x61));var keptCapacity=retained.NameCapacity;var retainedDepot=Depot(Repeated(16,0x61),new object());int freshReads=0;
  var fresh=FinishPartialObject.Apply(Manager(),retainedDepot,retained,Id,RecordNameLocale.KnownC,delegate{freshReads++;});
  Need(fresh.Status=="Known"&&freshReads==1&&keptCapacity==207&&retained.NameCapacity==207&&fresh.TemporaryNameCapacity==31&&fresh.Load.LocalNameCapacity==31,"fresh name temporaries use their own capacity rather than copying the object's retained heap capacity");
 }
 static RegistrationIndex FullIndex(){var entries=new RegistrationEntry[8];for(int i=0;i<entries.Length;i++)entries[i]=new RegistrationEntry((uint)i,new object());return Index(entries);}
 static bool Grown(RegistrationIndex index){return index.StateComplete&&index.Count==9&&index.BucketCount==64&&index.BucketCapacity==64;}
 static void WholeScenario(bool supplyOwnRead){
  byte[] originalBytes=B("Records\\MiXeD\\Entry\0TAIL/AbC.dbr"),fullKey=B("records/mixed/entry\0tail/abc.dbr"),prefix=B("Records\\MiXeD\\Entry"),prefixKey=B("records/mixed/entry");
  var classValue=new TableString(B("Own\0Ignored"));var classField=new LoadTableField(new TableString(B("Class")),classValue);var creationTable=new LoadTableSnapshot(new RegistrationGate(),new[]{classField});
  // Finish does not call the Class getter. Its own Read accepts this separate
  // opaque nonnull payload as the explicitly supplied table identity.
  var finishTable=new object();var depot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new[]{new RecordCacheEntry(fullKey,creationTable),new RecordCacheEntry(prefixKey,finishTable)}),0);
  int constructors=0;var classes=new ClassInfoSnapshot(B("Own\0"),null,null,delegate{constructors++;return ObjectBaseConstructor.Create();});var original=Name(originalBytes);
  var created=CreatePartialObject.Apply(depot,original,RecordNameLocale.KnownC,classes);var value=created.Instance as ObjectBaseState;
  Need(created.Status=="Known"&&value!=null&&constructors==1&&depot.HitCount54==1&&Object.ReferenceEquals(created.Load.Table,creationTable)&&Object.ReferenceEquals(created.Getter.Value,classValue)&&created.Getter.GateReleased&&Same(created.Load.CopyNormalizedName(),fullKey)&&value.NameLength==32&&value.NameCapacity==47&&value.Fields.RawKey28==FullNameKey&&!value.Fields.Id2C.HasValue,"CreatePartial loads the complete NUL-containing name and uses its actual Class getter once");
  var fields=value.Fields;var name=value.Name;var ids=FullIndex();var raw=FullIndex();var manager=new ObjectRegistrationState(new RegistrationGate(),Index(new RegistrationEntry(Id,new object())),ids,raw);var oldIdNode=ids.FirstInBucket(ids.BucketFor(0));var oldRawNode=raw.FirstInBucket(raw.BucketFor(0));
  int reads=0;bool sawComplete=false;Action<RegistrationObject,object> read=null;
  if(supplyOwnRead)read=delegate(RegistrationObject incoming,object loaded){reads++;sawComplete=Object.ReferenceEquals(incoming,fields)&&Object.ReferenceEquals(loaded,finishTable)&&!Object.ReferenceEquals(loaded,creationTable)&&incoming.Id2C==Id&&incoming.RawKey28==FullNameKey&&manager.Pending128.Count==0&&Grown(ids)&&Grown(raw)&&!manager.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread&&!creationTable.Gate.IsHeldByCurrentThread;};
  var finished=FinishPartialObject.Apply(manager,depot,value,Id,RecordNameLocale.KnownC,read);
  Need(finished.Load.Status=="Known"&&finished.Load.CacheHit&&Object.ReferenceEquals(finished.Load.Table,finishTable)&&!Object.ReferenceEquals(finished.Load.Table,created.Load.Table)&&depot.HitCount54==2&&finished.CStringLength==19&&finished.TemporaryNameCapacity==31&&Same(finished.CopyRecordName(),prefix)&&Same(finished.Load.CopyNormalizedName(),prefixKey),"FinishPartial performs a second load using only the C-string prefix and selects the different table");
  Need(Object.ReferenceEquals(finished.Instance,value)&&Object.ReferenceEquals(value.Fields,fields)&&Object.ReferenceEquals(value.Name,name)&&fields.Id2C==Id&&fields.RawKey28==FullNameKey&&value.NameLength==32&&value.NameCapacity==47&&Same(value.CopyNameBytes(),originalBytes)&&Same(original.CopyBytes(),originalBytes),"the shorter reload neither renames nor rehashes the complete object and preserves both original identities");
  Need(constructors==1&&Object.ReferenceEquals(created.Getter.Value,classValue)&&Object.ReferenceEquals(created.Getter.MatchedField,classField)&&Object.ReferenceEquals(created.Load.Table,creationTable)&&Same(created.Load.CopyNormalizedName(),fullKey)&&!creationTable.Gate.IsHeldByCurrentThread&&!finished.DiagnosticRequired,"Finish does not rerun the constructor or require a getter on its opaque table; the prior Create result stays intact");
  Need(finished.Registration.IdAssigned&&finished.Registration.PendingErase.Supported&&finished.Registration.PendingErase.RemovedCount==1&&manager.Pending128.Count==0&&finished.Registration.LockAcquired&&finished.Registration.LockReleased&&!manager.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread,"registration follows reload and preserves ID assignment, pending erase and lock release");
  Need(Grown(ids)&&Grown(raw)&&finished.Registration.IdInsert.Inserted==true&&finished.Registration.RawKeyInsert.Inserted==true&&finished.Registration.IdInsert.Node.Key==Id&&finished.Registration.RawKeyInsert.Node.Key==FullNameKey&&Object.ReferenceEquals(finished.Registration.IdInsert.Value,fields)&&Object.ReferenceEquals(finished.Registration.RawKeyInsert.Value,fields),"both ninth insertions grow from 8 to 64 buckets using the same object and full-name raw key");
  var retainedId=ids.Insert(0,new object());var retainedRaw=raw.Insert(0,new object());
  Need(retainedId.Supported&&retainedRaw.Supported&&retainedId.Inserted==false&&retainedRaw.Inserted==false&&Object.ReferenceEquals(retainedId.Node,oldIdNode)&&Object.ReferenceEquals(retainedRaw.Node,oldRawNode)&&!retainedId.MayHaveEffects&&!retainedRaw.MayHaveEffects&&Grown(ids)&&Grown(raw),"both reorders retain prior node identity and later duplicate probes have no new insertion effects");
  if(supplyOwnRead)
   Need(finished.Status=="Known"&&finished.LastCompletedStage=="ReadComplete"&&finished.FailureStage==null&&finished.Registration.ReadInvoked&&reads==1&&sawComplete,"explicit own Read sees the prefix table and completed growth outside all gates");
  else
   Need(finished.Status=="Unsupported"&&finished.Reason=="RegistrationUnsupported:UnknownOwnReadCallback"&&finished.LastCompletedStage=="LoadFileComplete"&&finished.FailureStage=="Registration"&&finished.Registration.LastCompletedStage=="LockReleased"&&finished.Registration.FailureStage=="OwnRead"&&!finished.Registration.ReadInvoked&&reads==0&&!sawComplete,"missing Read stops after the same completed registration prefix and is not replaced by an empty implementation");
  Need(finished.MayHaveEffects&&finished.Load.HitCountIncremented&&finished.Registration.MayHaveEffects&&depot.HitCount54==2&&fields.Id2C==Id&&fields.RawKey28==FullNameKey&&constructors==1&&Grown(ids)&&Grown(raw)&&Same(value.CopyNameBytes(),originalBytes),"both successful and uncovered Read outcomes retain the counted reload, construction, name and registration effects");
 }
 static int Main(){try{Guards();CStringAndLocale();NullTableAndUnfinishedLoad();RegistrationOrdering();RawIdsAndLengths();WholeScenario(true);WholeScenario(false);Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scenarios\":2,\"scope\":\"Own bounded FinishPartial caller over complete name storage, explicit cache/registration states and optional own Read; no original execution, native diagnostic, miss loader, concrete Read or native ABI.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
