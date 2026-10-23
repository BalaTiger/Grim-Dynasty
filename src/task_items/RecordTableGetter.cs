using System;
namespace GrimDynasty.TaskItems {
 // Complete immutable std::string payload plus its logical trailing NUL.
 // Interior NUL bytes still belong to the payload. This is not a native ptr.
 public sealed class TableString {
  public const int MaximumBytes=65536;
  readonly byte[] bytes;
  public int Length {get{return bytes.Length;}}
  public TableString(byte[] fullStoredPayload){
   if(fullStoredPayload==null)throw new ArgumentNullException("fullStoredPayload");
   if(fullStoredPayload.Length>MaximumBytes)throw new ArgumentOutOfRangeException("fullStoredPayload");
   bytes=(byte[])fullStoredPayload.Clone();
  }
  public byte[] CopyCStringBytes(){var copy=new byte[bytes.Length+1];Array.Copy(bytes,copy,bytes.Length);return copy;}
  internal byte[] Bytes {get{return bytes;}}
 }
 public sealed class LoadTableField {
  public readonly TableString Key,Value;
  public LoadTableField(TableString key,TableString value){
   if(key==null||value==null)throw new ArgumentException("Complete key and value storage required");
   Key=key;Value=value;
  }
 }
 public abstract class RecordTableSnapshot {
  public readonly RegistrationGate Gate;
  protected RecordTableSnapshot(RegistrationGate gate){Gate=gate;}
 }
 public sealed class LoadTableSnapshot:RecordTableSnapshot {
  public const int MaximumFields=4096,MaximumPayloadBytes=2097152;
  readonly LoadTableField[] fields;
  public int Count {get{return fields.Length;}}
  // Complete in-order view of a valid, unique-key ordered tree. This own
  // abstraction preserves lookup outcomes and storage identity, not tree
  // shape, native pointer layout or the number/order of memcmp invocations.
  public LoadTableSnapshot(RegistrationGate gate,LoadTableField[] fieldsInKeyOrder):base(gate){
   if(fieldsInKeyOrder==null)throw new ArgumentNullException("fieldsInKeyOrder");
   if(fieldsInKeyOrder.Length>MaximumFields)throw new ArgumentOutOfRangeException("fieldsInKeyOrder");
   fields=(LoadTableField[])fieldsInKeyOrder.Clone();int total=0;
   for(int i=0;i<fields.Length;i++){
    if(fields[i]==null)throw new ArgumentException("Unknown field","fieldsInKeyOrder");
    total+=fields[i].Key.Length+fields[i].Value.Length;
    if(total>MaximumPayloadBytes)throw new ArgumentOutOfRangeException("fieldsInKeyOrder");
    if(i!=0&&Compare(fields[i-1].Key.Bytes,fields[i].Key.Bytes)>=0)throw new ArgumentException("Strict unsigned-byte key order required","fieldsInKeyOrder");
   }
  }
  internal static int Compare(byte[] left,byte[] right){
   int end=Math.Min(left.Length,right.Length);
   for(int i=0;i<end;i++)if(left[i]!=right[i])return left[i]<right[i]?-1:1;
   return left.Length<right.Length?-1:(left.Length>right.Length?1:0);
  }
  internal LoadTableField Find(byte[] key){
   int lo=0,hi=fields.Length;
   // 13EBC0 selects the first key >= query. The caller's query>=candidate
   // check then requires equality in this valid unique-key tree domain.
   while(lo<hi){int mid=lo+(hi-lo)/2;if(Compare(fields[mid].Key.Bytes,key)<0)lo=mid+1;else hi=mid;}
   return lo<fields.Length&&Compare(key,fields[lo].Key.Bytes)>=0?fields[lo]:null;
  }
 }
 public sealed class LoadTableBinarySnapshot:RecordTableSnapshot {
  // Present archive requires explicit binary-state inputs. Presence alone
  // does not fabricate the name-pointer cache, shared pool or parsed fields.
  public readonly bool? ArchivePresent;
  BinaryWarmState warm;
  public BinaryWarmState Warm {get{return warm;}}
  public LoadTableBinarySnapshot(RegistrationGate gate,bool? archivePresent):this(gate,archivePresent,null){}
  public LoadTableBinarySnapshot(RegistrationGate gate,bool? archivePresent,BinaryWarmState warm):base(gate){ArchivePresent=archivePresent;this.warm=warm;}
  internal void InstallParsedWarm(BinaryWarmState parsed){
   if(Gate==null||!Gate.IsHeldByCurrentThread)throw new InvalidOperationException("The table gate must be held when installing parsed state");
   if(parsed==null)throw new ArgumentNullException("parsed");
   warm=parsed;
  }
 }
 public sealed class RecordTableGetterResult {
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  public readonly TableString Value;
  public readonly LoadTableField MatchedField;
  public readonly bool UsedDefault,GateAcquired,GateReleased;
  public readonly int? KeyLength;
  public readonly ulong? LocalKeyCapacity;
  public readonly BinaryWarmLookupResult Binary;
  // ID flags describe a stale entry rewrite; cold insertion has its own flags.
  public readonly bool CacheIdWritten,CacheIdChanged,CacheCountWritten,CacheNodeInserted,CacheStorageGrown;
  public bool MayHaveEffects {get{return CacheIdWritten||CacheCountWritten||CacheNodeInserted||CacheStorageGrown;}}
  // Known null Value can be the exact default, or Binary's native bounds
  // result for a negative/out-of-pool string index. UsedDefault separates it.
  // Unknown results are distinguished by Status, not by Value alone.
  internal RecordTableGetterResult(string reason,string last,string failed,TableString value,LoadTableField field,bool usedDefault,bool acquired,bool released,int? length,ulong? capacity,BinaryWarmLookupResult binary,BinaryLookupEffects effects){
   Status=reason==null?"Known":"Unsupported";Reason=reason;LastCompletedStage=last;FailureStage=reason==null?null:failed;
   Value=value;MatchedField=field;UsedDefault=usedDefault;GateAcquired=acquired;GateReleased=released;KeyLength=length;LocalKeyCapacity=capacity;Binary=binary;
   CacheIdWritten=effects!=null&&effects.Written;CacheIdChanged=effects!=null&&effects.Changed;
   CacheCountWritten=effects!=null&&effects.CountWritten;CacheNodeInserted=effects!=null&&effects.Inserted;
   CacheStorageGrown=effects!=null&&effects.Grown;
  }
 }
 public static class RecordTableGetter {
  // Normal successful native string allocation/copy/free and memcmp only;
  // caller supplies stable table/type/string storage. No record parsing.
  public static RecordTableGetterResult GetValue(object knownTable,byte[] keyCString,TableString defaultValue){
   return GetValue(knownTable,keyCString,defaultValue,null);
  }
  // Address is an explicit logical native-pointer value, never inferred from
  // array identity, GC location or an image's preferred VA. Unread on Base/
  // absent-archive paths. The caller guarantees stable readable query bytes.
  public static RecordTableGetterResult GetValue(object knownTable,byte[] keyCString,TableString defaultValue,ulong? queryAddress){
   string reason=null,last="Start",attempt="TableKind";bool acquired=false,released=false,usedDefault=false;
   TableString value=null;LoadTableField field=null;int? length=null;ulong? capacity=null;BinaryWarmLookupResult warm=null;BinaryLookupEffects effects=null;
   var plain=knownTable as LoadTableSnapshot;var binary=knownTable as LoadTableBinarySnapshot;
   RecordTableSnapshot table=plain!=null?(RecordTableSnapshot)plain:binary;
   if(table==null)reason="UnknownRecordTableSnapshotKind";
   else if(table.Gate==null){attempt="GateAcquire";reason="UnknownRecordTableGate";}
   else try{
    attempt="GateAcquire";table.Gate.Enter();acquired=true;last="GateAcquired";
    if(binary!=null){
     attempt="BinaryArchiveRead";
     if(!binary.ArchivePresent.HasValue)reason="UnknownBinaryArchivePresence";
     else if(binary.ArchivePresent.Value){
      attempt="BinaryNameToId";
      if(binary.Warm==null)reason="UnimplementedBinaryNameIdCache";
      else {
       effects=new BinaryLookupEffects();warm=BinaryWarmLookup.Read(binary.Warm,queryAddress,keyCString,defaultValue,effects);
       reason=warm.Reason;value=warm.Value;usedDefault=warm.UsedDefault;length=warm.CStringLength;capacity=warm.LocalNameCapacity;
       last=warm.LastCompletedStage;attempt=warm.FailureStage;
      }
     }
     else {value=defaultValue;usedDefault=true;last="AbsentArchiveDefaultSelected";}
    }else{
     attempt="KeyCStringRead";
     if(keyCString==null)reason="UnknownGetterKeyCString";
     else{
      int count=0;while(count<keyCString.Length&&count<=TableString.MaximumBytes&&keyCString[count]!=0)count++;
      if(count>TableString.MaximumBytes||count==keyCString.Length)reason="GetterKeyCStringUnterminatedOrOutsideCoveredDomain";
      else{
       var key=new byte[count];Array.Copy(keyCString,key,count);length=count;capacity=count<=15?15UL:((ulong)count|15UL);last="KeyCopied";
       attempt="FieldLookup";field=plain.Find(key);last="FieldLookupComplete";
       // 13C7E3 tests stored length, not the first returned character.
       usedDefault=field==null||field.Value.Length==0;value=usedDefault?defaultValue:field.Value;last="ValueSelected";
      }
     }
    }
   }catch(Exception e){reason="OwnTableGetterOperationThrew:"+e.GetType().FullName;}
   finally{
    if(acquired){
     // Own cleanup at an uncovered boundary/managed failure, not native EH.
     try{table.Gate.Exit();released=true;}
     catch(Exception e){attempt="GateRelease";reason="OwnTableGetterUnlockThrew:"+e.GetType().FullName;}
    }
   }
   if(reason==null)last="GateReleased";
   return new RecordTableGetterResult(reason,last,attempt,value,field,usedDefault,acquired,released,length,capacity,warm,effects);
  }
 }
}
