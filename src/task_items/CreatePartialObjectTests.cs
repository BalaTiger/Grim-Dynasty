using System;
using System.Text;
using GrimDynasty.TaskItems;
class CreatePartialObjectTests {
 static int n;
 const uint Id=77,CombinedNameKey=0xb0853751;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static byte[] B(string value){return Encoding.ASCII.GetBytes(value);}
 static bool Same(byte[] left,byte[] right){if(left==null||right==null||left.Length!=right.Length)return false;for(int i=0;i<left.Length;i++)if(left[i]!=right[i])return false;return true;}
 static ObjectNameString Name(byte[] value){return new ObjectNameString(value,Math.Max(15UL,(ulong)value.Length));}
 static LoadTableField Field(string key,TableString value){return new LoadTableField(new TableString(B(key)),value);}
 static LoadTableSnapshot Table(TableString classValue){return new LoadTableSnapshot(new RegistrationGate(),new[]{Field("Class",classValue)});}
 static RecordDepotState Depot(object table,byte[] normalized){return new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new[]{new RecordCacheEntry(normalized,table)}),0);}
 static ClassInfoSnapshot Own(Func<object> constructor){return new ClassInfoSnapshot(B("Own\0"),null,null,constructor);}
 static ClassInfoSnapshot Empty(Func<object> constructor){return new ClassInfoSnapshot(new byte[]{0},null,null,constructor);}
 static CreatePartialObjectResult Create(object table,ClassInfoSnapshot classes){return CreatePartialObject.Apply(Depot(table,B("item")),Name(B("item")),RecordNameLocale.KnownC,classes);}
 static void ShortCircuits(){
  var nullDepot=Depot(null,B("item"));
  var result=CreatePartialObject.Apply(nullDepot,Name(B("item")),RecordNameLocale.KnownC,null);
  Need(result.Status=="Known"&&result.LastCompletedStage=="NoLoadedTable"&&result.Instance==null&&result.Load.CacheHit&&nullDepot.HitCount54==1&&result.MayHaveEffects&&result.Getter==null&&result.Factory==null&&result.NameAssignment==null&&!result.DiagnosticRequired,"known null table returns after its counted hit without requiring getter or class metadata");
  var opaque=new object();result=Create(opaque,null);
  Need(result.Status=="Unsupported"&&result.Reason=="ClassGetterUnsupported:UnknownRecordTableSnapshotKind"&&result.FailureStage=="ClassGetter"&&Object.ReferenceEquals(result.Load.Table,opaque)&&result.Load.HitCountIncremented&&result.Getter!=null&&result.Factory==null&&result.NameAssignment==null&&result.MayHaveEffects,"opaque nonnull table retains its load prefix and cannot masquerade as a typed table");
  var table=Table(new TableString(B("Own")));result=Create(table,null);
  Need(result.Status=="Unsupported"&&result.Reason=="ClassFactoryUnsupported:UnknownClassInfoSnapshot"&&result.FailureStage=="ClassFactory"&&result.Getter.Status=="Known"&&result.Getter.GateReleased&&result.Factory.CallbacksInvoked==0&&!result.DiagnosticRequired&&result.MayHaveEffects,"class metadata becomes required only after a successful real getter");
  var missDepot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new RecordCacheEntry[0]),4);
  result=CreatePartialObject.Apply(missDepot,Name(B("item")),RecordNameLocale.KnownC,null);
  Need(result.Status=="Unsupported"&&result.Reason=="RecordLoadUnsupported:UnimplementedRecordCacheMissLoader"&&result.FailureStage=="LoadFile"&&result.Getter==null&&result.Factory==null&&!result.MayHaveEffects&&missDepot.HitCount54==4&&!missDepot.Gate.IsHeldByCurrentThread,"cache miss stops before getter or factory and does not count a hit");
 }
 static void EmptyClasses(){
  var absent=new LoadTableSnapshot(new RegistrationGate(),new LoadTableField[0]);int calls=0;
  var result=Create(absent,Empty(delegate{calls++;return ObjectBaseConstructor.Create();}));
  var value=result.Instance as ObjectBaseState;
  Need(result.Status=="Known"&&result.LastCompletedStage=="NameAssigned"&&result.Getter.UsedDefault&&result.Getter.Value.Length==0&&result.Getter.MatchedField==null&&calls==1&&value!=null&&Same(value.CopyNameBytes(),B("item"))&&!value.Fields.Id2C.HasValue,"missing Class selects the empty default and still dispatches an empty-name constructor");
  var storedEmpty=new TableString(new byte[0]);calls=0;
  result=Create(Table(storedEmpty),Empty(delegate{calls++;return null;}));
  Need(result.Status=="Known"&&result.LastCompletedStage=="NullFactoryResult"&&result.Getter.UsedDefault&&!Object.ReferenceEquals(result.Getter.Value,storedEmpty)&&result.Getter.Value.Length==0&&result.Getter.MatchedField.Value==storedEmpty&&calls==1&&result.Instance==null&&!result.DiagnosticRequired&&result.NameAssignment==null,"zero stored length chooses the distinct caller default before a known null empty-class result");
  var leadingNul=new TableString(new byte[]{0,88});calls=0;
  result=Create(Table(leadingNul),Empty(delegate{calls++;return null;}));
  Need(result.Status=="Known"&&!result.Getter.UsedDefault&&Object.ReferenceEquals(result.Getter.Value,leadingNul)&&Same(result.Getter.Value.CopyCStringBytes(),new byte[]{0,88,0})&&calls==1&&result.Factory.CallbacksInvoked==1&&result.Instance==null&&!result.DiagnosticRequired&&result.NameAssignment==null,"nonempty stored payload beginning with NUL is retained but is an empty class C-string");
  result=Create(absent,new ClassInfoSnapshot(B("Other\0"),0,null,null));
  Need(result.Status=="Known"&&result.Getter.UsedDefault&&result.Factory.Status=="Known"&&result.Factory.CallbacksInvoked==0&&result.Instance==null&&!result.DiagnosticRequired,"an unmatched empty default returns null without requiring the spawn diagnostic");
 }
 static void FactoryBoundaries(){
  var table=Table(new TableString(B("Own")));int effects=0;
  var result=Create(table,Own(delegate{effects++;return null;}));
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedSpawnDiagnostic"&&result.FailureStage=="SpawnDiagnostic"&&result.LastCompletedStage=="NullFactoryResult"&&result.DiagnosticRequired&&result.Factory.Status=="Known"&&result.Factory.CallbacksInvoked==1&&effects==1&&result.Instance==null&&result.NameAssignment==null&&result.MayHaveEffects,"known null construction for a nonempty class requires the unimplemented diagnostic after preserving callback effects");
  result=Create(table,new ClassInfoSnapshot(B("Other\0"),0,null,null));
  Need(result.Status=="Unsupported"&&result.Reason=="UnimplementedSpawnDiagnostic"&&result.DiagnosticRequired&&result.Factory.Status=="Known"&&result.Factory.CallbacksInvoked==0&&result.NameAssignment==null,"a complete tree with no nonempty class match reaches the same diagnostic without invoking a constructor");
  var wrong=new object();result=Create(table,Own(delegate{return wrong;}));
  Need(result.Status=="Unsupported"&&result.Reason=="ConstructedObjectOutsideCoveredModel"&&result.FailureStage=="SetObjectName"&&Object.ReferenceEquals(result.Instance,wrong)&&Object.ReferenceEquals(result.Factory.Instance,wrong)&&!result.DiagnosticRequired&&result.NameAssignment==null&&result.MayHaveEffects,"wrong constructed type remains observable and is not relabeled as a null factory result");
  var depot=Depot(table,B("item"));effects=0;
  result=CreatePartialObject.Apply(depot,Name(B("item")),RecordNameLocale.KnownC,Own(delegate{effects++;throw new InvalidOperationException("own callback boundary");}));
  Need(result.Status=="Unsupported"&&result.Reason=="ClassFactoryUnsupported:ClassFactoryCallbackThrew:System.InvalidOperationException"&&result.FailureStage=="ClassFactory"&&result.Factory.CallbacksInvoked==1&&effects==1&&result.Instance==null&&result.NameAssignment==null&&!result.DiagnosticRequired&&result.MayHaveEffects,"own callback exception is unsupported and does not become a known null construction or erase effects");
  Need(result.Load.Status=="Known"&&result.Getter.Status=="Known"&&result.Getter.GateReleased&&depot.HitCount54==1&&!depot.Gate.IsHeldByCurrentThread&&!table.Gate.IsHeldByCurrentThread,"the load hit and real getter remain complete and unlocked after the throwing callback");
 }
 static void OriginalAlias(){
  var fields=new RegistrationObject(123,null);var owner=ObjectBaseConstructor.Initialize(fields);
  var first=ObjectNameAssignment.Apply(owner,Name(B("FIRST\\PATH")));var original=owner.Name;
  Need(first.Status=="Known"&&fields.RawKey28==0x4724fbb0&&fields.Id2C==123&&Object.ReferenceEquals(owner.Name,original),"alias fixture starts with a named owner and a seeded ID");
  var table=Table(new TableString(B("Own")));var depot=Depot(table,B("first/path"));int calls=0;bool callbackUnlocked=false,changed=false;
  var result=CreatePartialObject.Apply(depot,original,RecordNameLocale.KnownC,Own(delegate{
   calls++;callbackUnlocked=!depot.Gate.IsHeldByCurrentThread&&!table.Gate.IsHeldByCurrentThread;
   changed=ObjectNameAssignment.Apply(owner,Name(B("SECOND\\PATH"))).Status=="Known";
   fields.RawKey28=0;return owner;
  }));
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Instance,owner)&&Object.ReferenceEquals(owner.Name,original)&&Object.ReferenceEquals(owner.Fields,fields)&&result.NameAssignment.AssignmentSkipped&&!result.NameAssignment.NameWritten&&result.NameAssignment.KeyWritten&&fields.RawKey28==0xbbc228b4&&fields.Id2C==123&&Same(owner.CopyNameBytes(),B("SECOND\\PATH")),"setter receives the live original reference after the callback and rehashes the changed self-alias without copying an old name");
  Need(calls==1&&callbackUnlocked&&changed&&depot.HitCount54==1&&Same(result.Load.CopyNormalizedName(),B("first/path"))&&result.Load.LocalNameCapacity==15&&result.Getter.GateReleased,"load retains the earlier normalized key while the callback and alias assignment observe the later live name");
 }
 static void NameBudgets(){
  var bytes=new byte[65536];for(int i=0;i<bytes.Length;i++)bytes[i]=97;
  var table=Table(new TableString(B("Own")));var depot=Depot(table,bytes);int calls=0;
  var result=CreatePartialObject.Apply(depot,Name(bytes),RecordNameLocale.KnownC,Own(delegate{calls++;return ObjectBaseConstructor.Create();}));
  var value=result.Instance as ObjectBaseState;
  Need(result.Status=="Known"&&calls==1&&value!=null&&value.NameLength==65536&&value.NameCapacity==65551&&!value.Fields.Id2C.HasValue&&value.Fields.RawKey28.HasValue&&Same(value.CopyNameBytes(),bytes)&&result.Load.LocalNameCapacity==65551&&depot.HitCount54==1,"the maximum covered name survives load, actual getter, factory and full-length assignment");
  var oversized=Name(new byte[65537]);var emptyDepot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(1,new RecordCacheEntry[0]),9);
  result=CreatePartialObject.Apply(emptyDepot,oversized,RecordNameLocale.KnownC,null);
  Need(result.Status=="Unsupported"&&result.Reason=="RecordLoadUnsupported:RecordNameOutsideCoveredDomain"&&result.FailureStage=="LoadFile"&&!result.Load.LockAcquired&&result.Getter==null&&result.Factory==null&&result.NameAssignment==null&&result.Instance==null&&!result.MayHaveEffects&&emptyDepot.HitCount54==9&&oversized.Length==65537,"65537 bytes stop at the own coverage budget before any load hit or factory effects, not a claimed native size limit");
 }
 static RegistrationIndex FullIndex(){
  var entries=new RegistrationEntry[8];
  // These keys occupy distinct buckets at B=8, so each seed segment is valid.
  for(int i=0;i<entries.Length;i++)entries[i]=new RegistrationEntry((uint)i,new object());
  return new RegistrationIndex(8,8,1.0f,entries);
 }
 static bool Grown(RegistrationIndex index){return index.StateComplete&&index.Count==9&&index.BucketCount==64&&index.BucketCapacity==64;}
 static void WholeScenario(bool supplyOwnRead){
  byte[] originalBytes=B("Records\\MiXeD\\Entry\0TAIL/AbC.dbr"),normalized=B("records/mixed/entry\0tail/abc.dbr");
  var original=Name(originalBytes);var classValue=new TableString(B("Own\0Ignored"));
  // Real ordinary typed getter storage in strict unsigned-byte key order.
  var table=new LoadTableSnapshot(new RegistrationGate(),new[]{Field("Before",new TableString(B("unused"))),Field("Class",classValue),Field("Zed",new TableString(B("unused")))});
  var depot=Depot(table,normalized);ObjectBaseState constructed=null;ObjectNameString constructedName=null;RegistrationObject fields=null;int constructions=0;bool constructorSawBase=false,constructorUnlocked=false;
  var created=CreatePartialObject.Apply(depot,original,RecordNameLocale.KnownC,Own(delegate{
   constructions++;constructorUnlocked=!depot.Gate.IsHeldByCurrentThread&&!table.Gate.IsHeldByCurrentThread;
   constructed=ObjectBaseConstructor.Create();constructedName=constructed.Name;fields=constructed.Fields;
   constructorSawBase=constructed.Dispatch==ObjectBaseDispatch.ObjectBase&&constructed.NameLength==0&&constructed.NameCapacity==15&&!fields.Id2C.HasValue&&!fields.RawKey28.HasValue;
   return constructed;
  }));
  var value=created.Instance as ObjectBaseState;
  Need(created.Status=="Known"&&created.LastCompletedStage=="NameAssigned"&&created.Load.Status=="Known"&&created.Load.CacheHit&&created.Load.LockReleased&&Object.ReferenceEquals(created.Load.Table,table)&&depot.HitCount54==1&&Same(created.Load.CopyNormalizedName(),normalized),"whole scenario starts with a normalized cache hit on the actual typed table");
  Need(created.Getter.Status=="Known"&&!created.Getter.UsedDefault&&Object.ReferenceEquals(created.Getter.Value,classValue)&&Object.ReferenceEquals(created.Getter.MatchedField.Value,classValue)&&created.Getter.KeyLength==5&&created.Getter.LocalKeyCapacity==15&&created.Getter.GateAcquired&&created.Getter.GateReleased,"the real getter finds exact Class storage and releases its gate before factory dispatch");
  Need(created.Factory.Status=="Known"&&created.Factory.CallbacksInvoked==1&&constructions==1&&constructorUnlocked&&constructorSawBase&&value!=null&&Object.ReferenceEquals(value,constructed),"Class C-string stops at its embedded NUL and invokes the explicit own base-constructor callback once");
  // Independent literal FNV32 over all 32 ASCII/NUL original bytes; the cache
  // normalizer's lowercased slash form must not be used for object naming.
  Need(created.NameAssignment.Status=="Known"&&!created.NameAssignment.AssignmentSkipped&&created.NameAssignment.CapacityGrew&&Object.ReferenceEquals(value.Name,constructedName)&&Object.ReferenceEquals(value.Fields,fields)&&value.NameLength==32&&value.NameCapacity==47&&value.Name.TerminatorByte==0&&fields.RawKey28==CombinedNameKey&&!fields.Id2C.HasValue&&Same(value.CopyNameBytes(),originalBytes),"assignment preserves original case, backslashes and the full NUL-containing payload while leaving ID unknown");
  // This is an explicit second full-string LoadFile composition. It does not
  // assert that FinishPartial's native name-getter/C-string path is recovered.
  var loadedAgain=RecordLoadFile.Apply(depot,value.Name,RecordNameLocale.KnownC);
  Need(loadedAgain.Status=="Known"&&loadedAgain.CacheHit&&loadedAgain.LockReleased&&Object.ReferenceEquals(loadedAgain.Table,table)&&depot.HitCount54==2&&constructions==1&&Same(loadedAgain.CopyNormalizedName(),normalized)&&Same(original.CopyBytes(),originalBytes)&&value.NameCapacity==47,"loading the assigned complete name again hits the same table without reconstructing or changing the original name");
  var ids=FullIndex();var raw=FullIndex();var pending=new RegistrationIndex(8,8,1.0f,new[]{new RegistrationEntry(Id,new object())});
  var manager=new ObjectRegistrationState(new RegistrationGate(),pending,ids,raw);var oldIdNode=ids.FirstInBucket(ids.BucketFor(0));var oldRawNode=raw.FirstInBucket(raw.BucketFor(0));
  Need(ids.Count==8&&raw.Count==8&&ids.BucketCount==8&&raw.BucketCount==8&&ids.BucketCapacity==8&&raw.BucketCapacity==8&&pending.Count==1,"both real registration indices start full with a separate pending entry");
  int reads=0;bool readSawComplete=false;Action<RegistrationObject,object> ownRead=null;
  if(supplyOwnRead)ownRead=delegate(RegistrationObject incoming,object loaded){
   reads++;readSawComplete=Object.ReferenceEquals(incoming,fields)&&Object.ReferenceEquals(loaded,table)&&!manager.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread&&!table.Gate.IsHeldByCurrentThread&&incoming.Id2C==Id&&incoming.RawKey28==CombinedNameKey&&pending.Count==0&&Grown(ids)&&Grown(raw)&&Object.ReferenceEquals(value.Name,constructedName)&&value.NameCapacity==47&&Same(value.CopyNameBytes(),originalBytes);
  };
  var registered=LoadedObjectRegistration.Apply(manager,fields,Id,loadedAgain.Table,ownRead);
  Need(registered.IdAssigned&&registered.LockAcquired&&registered.LockReleased&&registered.PendingErase.Supported&&registered.PendingErase.RemovedCount==1&&pending.Count==0&&!manager.Gate.IsHeldByCurrentThread,"registration assigns ID, erases pending and releases its own gate after the recovered prefix");
  Need(Grown(ids)&&Grown(raw)&&registered.IdInsert.Supported&&registered.RawKeyInsert.Supported&&registered.IdInsert.Inserted==true&&registered.RawKeyInsert.Inserted==true&&registered.IdInsert.Node.Key==Id&&registered.RawKeyInsert.Node.Key==CombinedNameKey&&Object.ReferenceEquals(registered.IdInsert.Value,fields)&&Object.ReferenceEquals(registered.RawKeyInsert.Value,fields),"both ninth insertions grow from eight to sixty-four buckets and retain the same registration object");
  var retainedId=ids.Insert(0,new object());var retainedRaw=raw.Insert(0,new object());
  Need(retainedId.Supported&&retainedRaw.Supported&&retainedId.Inserted==false&&retainedRaw.Inserted==false&&!retainedId.MayHaveEffects&&!retainedRaw.MayHaveEffects&&Object.ReferenceEquals(retainedId.Node,oldIdNode)&&Object.ReferenceEquals(retainedRaw.Node,oldRawNode)&&Grown(ids)&&Grown(raw),"both growth operations retain prior node identities and subsequent duplicates do not grow again");
  Need(fields.Id2C==Id&&fields.RawKey28==CombinedNameKey&&Object.ReferenceEquals(value.Fields,fields)&&Object.ReferenceEquals(value.Name,constructedName)&&Same(value.CopyNameBytes(),originalBytes)&&value.NameCapacity==47&&created.MayHaveEffects&&registered.MayHaveEffects&&depot.HitCount54==2,"registration and any uncovered later stage preserve the completed load, construction, name and index effects");
  if(supplyOwnRead)
   Need(registered.Status=="Known"&&registered.LastCompletedStage=="ReadComplete"&&registered.ReadInvoked&&reads==1&&readSawComplete,"explicit own Read receives the same table and object outside all gates after both growth operations");
  else
   Need(registered.Status=="Unsupported"&&registered.Reason=="UnknownOwnReadCallback"&&registered.FailureStage=="OwnRead"&&registered.LastCompletedStage=="LockReleased"&&!registered.ReadInvoked&&reads==0&&!readSawComplete,"missing actual Read remains unsupported after the same completed prefix and is never replaced by an empty callback");
 }
 static int Main(){try{ShortCircuits();EmptyClasses();FactoryBoundaries();OriginalAlias();NameBudgets();WholeScenario(true);WholeScenario(false);Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scenarios\":2,\"scope\":\"Own typed-cache/getter/factory/base-name/registration composition; explicit ClassInfo callbacks and optional own Read, no native binding, complete FinishPartial, record miss loader or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
