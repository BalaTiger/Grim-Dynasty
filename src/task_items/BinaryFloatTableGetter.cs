using System;

namespace GrimDynasty.TaskItems {
 // The result of the already-evidenced283840 name phase ONLY. No string
 // field, dummy numeric snapshot, fallback value or tag2 lookup is performed.
 public sealed class BinaryFloatNameResolution {
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  public readonly int? NameId,CStringLength;
  public readonly BinaryNameCacheEntry PointerEntry;
  public readonly BinaryNameIdLookupResult NameLookup;
  public readonly bool Cold,Stale,NameTemporaryReleased;
  public readonly ulong? LocalNameCapacity;
  public readonly bool CacheIdWritten,CacheIdChanged,CacheCountWritten,CacheNodeInserted,CacheStorageGrown;
  public readonly ulong? PreviousCachedId,WrittenCachedId;
  public readonly uint? PreviousCacheCount,CurrentCacheCount,InsertedSlotIndex,PreviousCacheCapacity,CurrentCacheCapacity;
  public bool MayHaveEffects {get{return CacheIdWritten||CacheCountWritten||CacheNodeInserted||CacheStorageGrown;}}
  internal BinaryFloatNameResolution(string reason,string last,string failed,int? id,int? length,
   BinaryNameCacheEntry pointer,BinaryNameIdLookupResult lookup,bool cold,bool stale,bool released,ulong? capacity,BinaryLookupEffects effects){
   Status=reason==null?"KnownNameId":"Unsupported";Reason=reason;LastCompletedStage=last;FailureStage=reason==null?null:failed;
   NameId=id;CStringLength=length;PointerEntry=pointer;NameLookup=lookup;Cold=cold;Stale=stale;NameTemporaryReleased=released;LocalNameCapacity=capacity;
   CacheIdWritten=effects.Written;CacheIdChanged=effects.Changed;CacheCountWritten=effects.CountWritten;CacheNodeInserted=effects.Inserted;CacheStorageGrown=effects.Grown;
   PreviousCachedId=effects.Previous;WrittenCachedId=effects.Current;PreviousCacheCount=effects.PreviousCount;CurrentCacheCount=effects.CurrentCount;
   InsertedSlotIndex=effects.SlotIndex;PreviousCacheCapacity=effects.PreviousCapacity;CurrentCacheCapacity=effects.CurrentCapacity;
  }
 }
 // Deliberately separate from BinaryWarmLookup.Read's following STRING value
 // phase. This reuses the same mutable cache nodes, name index and shared pool
 // and the same proven primitive operations without altering the old API. All
 // access requires the enclosing table gate or exclusive ownership. Managed
 // temporary allocation/release is not native malloc/free/EH reconstruction.
 internal static class BinaryFloatNameLookup {
  internal static BinaryFloatNameResolution Resolve(BinaryWarmState state,ulong? address,byte[] query,BinaryLookupEffects effects){
   string last="Start",stage="PointerCache";BinaryNameCacheEntry pointer=null;
   int? length=null,id=null;bool cold=false,stale=false,released=false;ulong? capacity=null;BinaryNameIdLookupResult lookup=null;
   Func<string,BinaryFloatNameResolution> result=delegate(string reason){return new BinaryFloatNameResolution(reason,last,stage,id,length,pointer,lookup,cold,stale,released,capacity,effects);};
   if(state.Names==null)return result("UnknownBinaryPointerCache");
   if(state.Names.BucketCount==0&&state.NameIds==null){cold=true;return result("UnimplementedBinaryPointerCacheMiss");}
   stage="QueryAddress";
   if(!address.HasValue)return result("UnknownBinaryQueryAddress");
   if(address.Value==0)return result("NullBinaryQueryAddressOutsideCoveredDomain");
   stage="PointerLookup";pointer=state.Names.Find(address.Value);cold=pointer==null;
   if(cold){
    if(state.NameIds==null)return result("UnimplementedBinaryPointerCacheMiss");
    last="PointerCacheMiss";
   }else{
    last="PointerCacheHit";stage="CachedNameRead";
    if(state.Strings==null)return result("UnknownBinarySharedStringPool");
    // Native uses the full cached QWORD for this pool read before the final
    // int32 return. Never truncate first to turn an invalid read into a hit.
    if(pointer.CachedId>=(ulong)state.Strings.Count)return result("CachedBinaryNameIndexOutsideCoveredDomain");
    TableString cached=state.Strings.Get((int)pointer.CachedId);stage="NameContentCompare";
    if(query==null)return result("UnknownBinaryQueryCString");
    for(int i=0;;i++){
     if(i>TableString.MaximumBytes||i>=query.Length)return result("BinaryQueryReadOutsideCoveredDomain");
     byte stored=i==cached.Length?(byte)0:cached.Bytes[i];
     if(stored!=query[i]){stale=true;break;}
     if(stored==0){length=i;break;}
    }
   }
   if(cold||stale){
    if(state.NameIds==null)return result("UnimplementedBinaryStalePointerLookup");
    last=cold?"PointerCacheMiss":"StaleNameDetected";stage=cold?"ColdQueryCStringRead":"StaleQueryCStringRead";
    if(query==null)return result("UnknownBinaryQueryCString");
    int count=0;while(count<query.Length&&count<=TableString.MaximumBytes&&query[count]!=0)count++;
    if(count>TableString.MaximumBytes||count==query.Length)return result(cold?"BinaryColdQueryReadOutsideCoveredDomain":"BinaryStaleQueryReadOutsideCoveredDomain");
    length=count;stage=cold?"ColdNameCopy":"StaleNameCopy";
    var local=new byte[count];Array.Copy(query,local,count);capacity=count<=15?15UL:((ulong)count|15UL);last=cold?"ColdNameCopied":"StaleNameCopied";
    stage=cold?"ColdNameLookup":"StaleNameLookup";lookup=state.NameIds.Find(local);id=lookup.Found?lookup.Entry.Id:-1;last=cold?"ColdNameLookupComplete":"StaleNameLookupComplete";
    // The known successful temporary release precedes the -1 test and any
    // cache mutation. A not-found ID is still a known int32 return from283840.
    local=null;released=true;last="NameTemporaryReleased";
    if(id.Value!=-1){
     ulong next=unchecked((ulong)(long)id.Value);
     if(cold){
      stage="CacheInsert";string failure=state.Names.Insert(address.Value,next,effects,out pointer);
      if(failure!=null)return result(failure);last="CacheNodeInserted";
     }else{
      stage="CacheIdWrite";ulong previous=pointer.CachedId;pointer.WriteId(next);
      effects.Written=true;effects.Changed=previous!=next;effects.Previous=previous;effects.Current=next;last="CacheIdWritten";
     }
    }
   }else{last="NameContentMatched";id=unchecked((int)pointer.CachedId);}
   stage="NameIdReturn";return result(null);
  }
 }
 public sealed class BinaryFloatNumericResult {
  public readonly int NameId;
  public readonly uint RequestedIndex;
  public BinaryFieldEntry Field {get;internal set;}
  public BinaryRecordDescriptor Descriptor {get;internal set;}
  public uint? ElementCount {get;internal set;}
  public uint? SelectedElementIndex {get;internal set;}
  public uint? BufferWordIndex {get;internal set;}
  public uint? ValueBits {get;internal set;}
  public bool UsedDefault {get;internal set;}
  public bool Returned {get;internal set;}
  public string Reason {get;internal set;}
  public string LastCompletedStage {get;internal set;}
  public string FailureStage {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  internal BinaryFloatNumericResult(int id,uint index){
   NameId=id;RequestedIndex=index;Reason="NotRun";LastCompletedStage="NumericEntry";FailureStage="FieldLookup";ResumeInstructionRva=0x140420;
  }
 }
 // Reachable140420..14048E instructions only. The following CC is excluded;
 // the bounded metadata gap is not asserted to be an exact function size.
 internal static class BinaryFloatNumericLookup {
  internal static BinaryFloatNumericResult Read(BinaryWarmState warm,BinaryParsedRecord parsed,RegistrationGate gate,
   int id,uint elementIndex,uint? defaultBits){
   var r=new BinaryFloatNumericResult(id,elementIndex);
   if(warm.Fields==null)return Stop(r,"UnknownBinaryFieldSnapshot","FieldLookup",0x140420);
   // MOVSXD R9,EDX; the existing snapshot preserves the unsigned shift/truncate
   // bucket calculation and first matching chain entry, including duplicate IDs.
   ulong key=unchecked((ulong)(long)id);r.Field=warm.Fields.Find(key);
   if(r.Field==null)return Default(r,defaultBits);
   r.LastCompletedStage="NumericFieldMatched";
   if(r.Field.Value==null)return Stop(r,"UnknownBinaryFieldPayload","FieldPayloadRead",0x14046E);
   if(r.Field.Value.TypeTag!=1)return Default(r,defaultBits);
   r.LastCompletedStage="FloatTypeMatched";
   // The old field projection has no count or raw tag1 words. A real parsed
   // record supplies them; never infer count1 or reinterpret tag2 string IDs.
   // These identity checks cover our immutable fresh-parser representation,
   // not native runtime storage, out-of-bounds access or exception behavior.
   if(parsed==null)return Stop(r,"MissingParsedFloatRecord","ElementCountRead",0x140473);
   if(!Object.ReferenceEquals(parsed.Gate,gate))return Stop(r,"ParsedFloatRecordGateMismatch","ElementCountRead",0x140473);
   if(!Object.ReferenceEquals(parsed.Fields,warm.Fields))return Stop(r,"ParsedFloatRecordFieldsMismatch","ElementCountRead",0x140473);
   BinaryRecordDescriptor[] descriptors=parsed.CopyDescriptors();
   // Parser prepends each field node, including equal keys. Select the LAST
   // descriptor for this key, then verify its tag/index against the node that
   // the actual chain lookup selected; do not skip a mismatching later node.
   for(int i=descriptors.Length-1;i>=0;i--){
    if((ulong)descriptors[i].Key==r.Field.Key){r.Descriptor=descriptors[i];break;}
   }
   if(r.Descriptor==null||r.Descriptor.TypeTag!=r.Field.Value.TypeTag||r.Descriptor.BufferIndex!=r.Field.Value.BufferIndex)
    return Stop(r,"ParsedFloatDescriptorMismatch","ElementCountRead",0x140473);
   uint count=r.Descriptor.ElementCount;r.ElementCount=count;
   // Zero counts are excluded by the real fresh parser. Native would wrap
   // count-1, not choose a safe default; this own scope retains that frontier.
   if(count==0)return Stop(r,"ZeroFloatElementCountOutsideFreshParsedDomain","ElementCountRead",0x140473);
   uint selected=elementIndex<count?elementIndex:unchecked(count-1);r.SelectedElementIndex=selected;
   r.LastCompletedStage="ElementIndexSelected";
   uint index=unchecked(r.Field.Value.BufferIndex+selected);r.BufferWordIndex=index;
   uint[] raw=parsed.CopyRawValues(1);
   // Native performs no corresponding bounds check. Its effective word index
   // is the zero-extended wrapped ECX; this is an OWN observation boundary.
   if(index>=(uint)raw.Length)return Stop(r,"FloatBufferReadOutsideFreshParsedDomain","FloatBufferRead",0x140489);
   r.ValueBits=raw[(int)index];r.Returned=true;r.Reason=null;r.FailureStage=null;
   r.LastCompletedStage="RawFloatBitsSelected";r.ResumeInstructionRva=0x14048E;return r;
  }
  static BinaryFloatNumericResult Default(BinaryFloatNumericResult r,uint? bits){
   r.UsedDefault=true;r.ValueBits=bits;r.LastCompletedStage="FloatDefaultSelected";
   if(!bits.HasValue)return Stop(r,"UnknownFloatDefaultBits","FloatDefaultReturn",0x140461);
   r.Returned=true;r.Reason=null;r.FailureStage=null;r.ResumeInstructionRva=0x140464;return r;
  }
  static BinaryFloatNumericResult Stop(BinaryFloatNumericResult r,string reason,string stage,uint rva){
   r.Reason=reason;r.FailureStage=stage;r.ResumeInstructionRva=rva;return r;
  }
 }
 public sealed class BinaryFloatTableGetterResult {
  public readonly LoadTableBinarySnapshot Table;
  public readonly BinaryParsedRecord ParsedRecord;
  public readonly uint ElementIndex;
  public readonly string Status,Reason,LastCompletedStage,FailureStage;
  public readonly uint? ValueBits;
  public readonly bool Returned,UsedDefault,GateAcquired,GateReleased;
  public readonly uint ResumeInstructionRva;
  public readonly BinaryFloatNameResolution Name;
  public readonly BinaryFloatNumericResult Numeric;
  public readonly bool CacheIdWritten,CacheIdChanged,CacheCountWritten,CacheNodeInserted,CacheStorageGrown;
  public bool MayHaveEffects {get{return CacheIdWritten||CacheCountWritten||CacheNodeInserted||CacheStorageGrown;}}
  internal BinaryFloatTableGetterResult(LoadTableBinarySnapshot table,BinaryParsedRecord parsed,uint index,
   string reason,string last,string failed,uint? bits,bool usedDefault,bool acquired,bool released,uint rva,
   BinaryFloatNameResolution name,BinaryFloatNumericResult numeric,BinaryLookupEffects effects){
   Table=table;ParsedRecord=parsed;ElementIndex=index;Status=reason==null?"KnownFloatBits":"Unsupported";Reason=reason;
   LastCompletedStage=last;FailureStage=reason==null?null:failed;ValueBits=bits;Returned=reason==null;
   UsedDefault=usedDefault;GateAcquired=acquired;GateReleased=released;ResumeInstructionRva=rva;Name=name;Numeric=numeric;
   CacheIdWritten=effects.Written;CacheIdChanged=effects.Changed;CacheCountWritten=effects.CountWritten;
   CacheNodeInserted=effects.Inserted;CacheStorageGrown=effects.Grown;
  }
 }
 public static class BinaryFloatTableGetter {
  public const uint WrapperRva=0x1402E0,NameToIdRva=0x283840,NumericRva=0x140420,ReturnRva=0x140377;
  // Exact known binary wrapper only. The13FD50 scalar thunk supplies index0
  // to this indexed wrapper. No vtable, archive, parser, gate or native object
  // is fabricated. The caller supplies a stable valid table-kind observation.
  public static BinaryFloatTableGetterResult GetValue(LoadTableBinarySnapshot table,BinaryParsedRecord parsed,
   byte[] keyCString,uint? defaultBits,ulong? queryAddress){
   return GetIndexedValue(table,parsed,keyCString,0,defaultBits,queryAddress);
  }
  public static BinaryFloatTableGetterResult GetIndexedValue(LoadTableBinarySnapshot table,BinaryParsedRecord parsed,
   byte[] keyCString,uint elementIndex,uint? defaultBits,ulong? queryAddress){
   string reason=null,last="Start",stage="TableIdentity";bool acquired=false,released=false,usedDefault=false;
   uint? bits=null;uint rva=WrapperRva;BinaryFloatNameResolution name=null;BinaryFloatNumericResult numeric=null;var effects=new BinaryLookupEffects();
   if(table==null)reason="UnknownBinaryFloatTable";
   else if(table.Gate==null){stage="GateAcquire";rva=0x140318;reason="UnknownBinaryFloatTableGate";}
   else try{
    stage="GateAcquire";rva=0x14031C;table.Gate.Enter();acquired=true;last="GateAcquired";
    stage="BinaryArchiveRead";rva=0x140323;
    if(!table.ArchivePresent.HasValue)reason="UnknownBinaryArchivePresence";
    else if(!table.ArchivePresent.Value){
     // This native branch performs no query/cache/parsed-payload reads.
     usedDefault=true;bits=defaultBits;last="AbsentArchiveDefaultSelected";
     if(!bits.HasValue)reason="UnknownFloatDefaultBits";
    }else{
     stage="BinaryNameToId";rva=NameToIdRva;
     BinaryWarmState warm=table.Warm;
     if(warm==null)reason="UnknownBinaryNameContext";
     else{
      name=BinaryFloatNameLookup.Resolve(warm,queryAddress,keyCString,effects);
      reason=name.Reason;last=name.LastCompletedStage;stage=name.FailureStage;
      if(reason==null){
       stage="BinaryFloatNumeric";rva=NumericRva;
       numeric=BinaryFloatNumericLookup.Read(warm,parsed,table.Gate,name.NameId.Value,elementIndex,defaultBits);
       reason=numeric.Reason;bits=numeric.ValueBits;usedDefault=numeric.UsedDefault;
       last=numeric.LastCompletedStage;stage=numeric.FailureStage;rva=numeric.ResumeInstructionRva;
      }
     }
    }
   }catch(Exception e){reason="OwnBinaryFloatGetterOperationThrew:"+e.GetType().FullName;}
   finally{
    if(acquired){
     // Managed cleanup on unsupported/throwing own paths is not recovered
     // native exception cleanup or a promise of native rollback/atomicity.
     try{table.Gate.Exit();released=true;}
     catch(Exception e){stage="GateRelease";rva=0x140355;reason="OwnBinaryFloatGetterUnlockThrew:"+e.GetType().FullName;}
    }
   }
   if(reason==null){last="GateReleased";rva=ReturnRva;}
   return new BinaryFloatTableGetterResult(table,parsed,elementIndex,reason,last,stage,bits,usedDefault,acquired,released,rva,name,numeric,effects);
  }
 }
}
