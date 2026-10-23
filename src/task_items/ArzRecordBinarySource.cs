using System;
using GrimDynasty.Resources;
namespace GrimDynasty.TaskItems {
 // Independent bounded ARZ-file bytes -> supplied parser context. This is not
 // Engine 280E20, native scratch ownership or native archive initialization.
 // Optional body remapping uses a supplied or generated immutable context.
 // The stream is borrowed: callers keep it stable, exclusively accessed and
 // open for a miss. Hits do not access it. This object never closes the stream.
 public sealed class ArzRecordBinarySource {
  public const long MaximumReadWorkingBytes=8L*1024*1024;
  public const long MaximumFreshChainArrayPayloadBytes=24L*1024*1024;
  readonly ResourceInput input;
  readonly ArzRawLimits limits;
  readonly RegistrationGate tableGate;
  readonly BinaryPointerCacheSnapshot names;
  readonly BinaryNameIdSnapshot nameIds;
  readonly bool freshUniqueArchive;
  readonly ulong emptyQueryAddress;
  internal readonly Func<RecordLoadTickResult> Clock;
  public readonly ArzRawDecodeProfile DecodeProfile;
  public readonly ArzRecordRemapContext RemapContext;
  public readonly uint? ConvertedStartTick32,ConvertedEndTick32;
  // A conservative explicit allowance for array payloads in one fresh chain,
  // not measured live memory, native heap usage or a CLR process-peak bound.
  public readonly long FreshChainArrayPayloadAllowanceBytes;
  public bool NativeArchiveFetchRecovered {get{return false;}}
  public ArzRecordBinarySource(ResourceInput borrowedInput,ArzRawLimits readLimits,
   RegistrationGate knownTableGate,BinaryPointerCacheSnapshot knownNames,
   BinaryNameIdSnapshot knownNameIds,uint? convertedStartTick32,uint? convertedEndTick32)
   :this(borrowedInput,readLimits,knownTableGate,knownNames,knownNameIds,
    convertedStartTick32,convertedEndTick32,ArzRawDecodeProfile.BoundedInput){}
  public ArzRecordBinarySource(ResourceInput borrowedInput,ArzRawLimits readLimits,
   RegistrationGate knownTableGate,BinaryPointerCacheSnapshot knownNames,
   BinaryNameIdSnapshot knownNameIds,uint? convertedStartTick32,uint? convertedEndTick32,
   ArzRawDecodeProfile decodeProfile)
   :this(borrowedInput,readLimits,knownTableGate,knownNames,knownNameIds,
    convertedStartTick32,convertedEndTick32,decodeProfile,null){}
  public ArzRecordBinarySource(ResourceInput borrowedInput,ArzRawLimits readLimits,
   RegistrationGate knownTableGate,BinaryPointerCacheSnapshot knownNames,
   BinaryNameIdSnapshot knownNameIds,uint? convertedStartTick32,uint? convertedEndTick32,
   ArzRawDecodeProfile decodeProfile,ArzRecordRemapContext remapContext){
   if(decodeProfile!=ArzRawDecodeProfile.BoundedInput&&decodeProfile!=ArzRawDecodeProfile.ObservedEngineOutputSize)
    throw new ArgumentOutOfRangeException("decodeProfile");
   if(borrowedInput==null)throw new ArgumentNullException("borrowedInput");
   if(readLimits==null)throw new ArgumentNullException("readLimits");
   if(knownTableGate==null)throw new ArgumentNullException("knownTableGate");
   if(knownNames==null)throw new ArgumentNullException("knownNames");
   if(knownNameIds==null)throw new ArgumentNullException("knownNameIds");
   // Refuse an oversized context before the raw reader allocates it. Never
   // trim/reindex an actual archive pool to force it into the parser's domain.
   if(readLimits.MaxStrings>BinaryWarmState.MaximumStrings||
    readLimits.MaxPoolBytes>BinaryWarmState.MaximumPoolBytes||
    readLimits.MaxStringBytes>TableString.MaximumBytes||
    readLimits.MaxBodyBytes>BinaryRecordReader.MaximumBytes||
    readLimits.MaxPackedBytes>2*BinaryRecordReader.MaximumBytes||
    readLimits.MaxWorkingBytes>MaximumReadWorkingBytes)
    throw new ArgumentOutOfRangeException("readLimits","ARZ source exceeds supplied-parser coverage budgets");
   input=borrowedInput;limits=readLimits;tableGate=knownTableGate;names=knownNames;nameIds=knownNameIds;
   DecodeProfile=decodeProfile;
   RemapContext=remapContext;
   ConvertedStartTick32=convertedStartTick32;ConvertedEndTick32=convertedEndTick32;
  }
  // Normal completed single initial archive import only: unique full pool,
  // original IDs, existing empty entry and no append remap. The owned query
  // token is an explicit identity, never a recovered original-game address.
  public static ArzRecordBinarySource ForFreshUniqueArchive(ResourceInput borrowedInput,
   ArzRawLimits readLimits,RegistrationGate knownTableGate,ulong knownEmptyQueryAddress,
   uint? convertedStartTick32,uint? convertedEndTick32){
   return new ArzRecordBinarySource(borrowedInput,readLimits,knownTableGate,
    knownEmptyQueryAddress,convertedStartTick32,convertedEndTick32,null);
  }
  public static ArzRecordBinarySource ForFreshUniqueArchiveWithClock(ResourceInput borrowedInput,
   ArzRawLimits readLimits,RegistrationGate knownTableGate,ulong knownEmptyQueryAddress,
   Func<RecordLoadTickResult> captureClock){
   if(captureClock==null)throw new ArgumentNullException("captureClock");
   return new ArzRecordBinarySource(borrowedInput,readLimits,knownTableGate,
    knownEmptyQueryAddress,null,null,captureClock);
  }
  ArzRecordBinarySource(ResourceInput borrowedInput,ArzRawLimits readLimits,
   RegistrationGate knownTableGate,ulong knownEmptyQueryAddress,
   uint? convertedStartTick32,uint? convertedEndTick32,Func<RecordLoadTickResult> captureClock){
   if(borrowedInput==null)throw new ArgumentNullException("borrowedInput");
   if(readLimits==null)throw new ArgumentNullException("readLimits");
   if(knownTableGate==null)throw new ArgumentNullException("knownTableGate");
   if(knownEmptyQueryAddress==0)throw new ArgumentOutOfRangeException("knownEmptyQueryAddress");
   if(readLimits.MaxStrings>BinaryStringPool.MaximumRawStrings||
    readLimits.MaxPoolBytes>BinaryStringPool.MaximumRawPayloadBytes||
    readLimits.MaxStringTableBytes>BinaryStringPool.MaximumRawWireBytes||
    readLimits.MaxStringBytes>TableString.MaximumBytes||
    readLimits.MaxBodyBytes>BinaryRecordReader.MaximumBytes||
    readLimits.MaxPackedBytes>2*BinaryRecordReader.MaximumBytes||
    readLimits.MaxWorkingBytes>MaximumReadWorkingBytes)
    throw new ArgumentOutOfRangeException("readLimits","Fresh ARZ source exceeds explicit complete-pool budgets");
   FreshChainArrayPayloadAllowanceBytes=FreshArrayAllowance(readLimits.MaxWorkingBytes,
    readLimits.MaxStrings,readLimits.MaxBodyBytes);
   if(FreshChainArrayPayloadAllowanceBytes>MaximumFreshChainArrayPayloadBytes)
    throw new ArgumentOutOfRangeException("readLimits","Fresh ARZ source exceeds single-chain array-payload allowance");
   input=borrowedInput;limits=readLimits;tableGate=knownTableGate;
   emptyQueryAddress=knownEmptyQueryAddress;freshUniqueArchive=true;Clock=captureClock;
   DecodeProfile=ArzRawDecodeProfile.ObservedEngineOutputSize;
   ConvertedStartTick32=convertedStartTick32;ConvertedEndTick32=convertedEndTick32;
  }
  static long FreshArrayAllowance(long rawWorking,int originalCount,int bodyBytes){
   // rawWorking already includes all raw owned arrays; never add them again.
   // 12*N covers the sorted original-ID index and x64 memo references.
   // Pointer-cache arrays/temporary clones: <=2MiB. Body exports, supplied
   // clone and reader clone: 3*body. Lazy payload memo: <=256KiB.
   // The final 2MiB covers bounded parser and small publication/cache arrays.
   // Object headers, allocator/GC metadata and runtime baseline are excluded.
   return rawWorking+12L*originalCount+2L*1024*1024+3L*bodyBytes+
    BinaryStringPool.MaximumMaterializedBytes+2L*1024*1024;
  }
  internal ArzRawRecord ReadExact(byte[] normalizedName){
   // Deliberately exact stored bytes. No TrimEnd(NUL), Unicode case folding,
   // alternate-name search, or assumption about native record-name mapping.
   return ArzRawRecordReader.ReadExact(input,normalizedName,limits,DecodeProfile);
  }
  internal SuppliedBinaryRecord ToSupplied(ArzRawRecord raw,byte[] normalizedName,
   RecordSuppliedBinaryMissTrace trace){
   return ToSupplied(raw,normalizedName,trace,ConvertedStartTick32);
  }
  internal SuppliedBinaryRecord ToSupplied(ArzRawRecord raw,byte[] normalizedName,
   RecordSuppliedBinaryMissTrace trace,uint? capturedStartTick32){
   if(freshUniqueArchive){
    // Raw acquisition is already retained in trace. All initialization guards
    // run before constructing the parser/table or publishing anything.
    trace.FailureStage="OwnArzFreshCompletePool";
    if(FreshArrayAllowance(limits.MaxWorkingBytes,raw.StringCount,(int)raw.BodyBytes)>
     MaximumFreshChainArrayPayloadBytes)
     throw new System.IO.InvalidDataException("OwnArzFreshChainArrayPayloadBudget");
    var completePool=BinaryStringPool.FromUniqueRaw(raw);
    trace.FailureStage="OwnArzFreshPointerCache";
    BinaryPointerCacheSnapshot initializedNames;
    try{
     initializedNames=BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(
      completePool.Count,completePool.EmptyId,emptyQueryAddress);
    }catch(ArgumentOutOfRangeException error){
     throw new System.IO.InvalidDataException("OwnArzFreshPointerCacheOutsideCoveredDomain:"+error.Message,error);
    }
    trace.FailureStage="OwnArzFreshCompleteNameIndex";
    var completeNames=BinaryNameIdSnapshot.FromCompleteUniquePool(completePool);
    trace.FailureStage="OwnArzContextProjection";
    byte[] freshBody=raw.CopyBodyBytes();
    return SuppliedBinaryRecord.FromFreshPool(normalizedName,freshBody,tableGate,
     initializedNames,completePool,completeNames,capturedStartTick32,ConvertedEndTick32,Clock);
   }
   if(RemapContext!=null){
    trace.FailureStage="OwnArzMappingSourceCheck";
    RemapContext.ValidateSource(raw);
   }
   byte[] body=raw.CopyBodyBytes();
   TableString[] pool;
   if(RemapContext==null){
    pool=new TableString[raw.StringCount];
    for(int i=0;i<pool.Length;i++)pool[i]=new TableString(raw.CopyStringBytes((uint)i));
   }else{
    trace.FailureStage="OwnArzRecordRemap";
    // Preserve the private scratch even on a safety guard or interrupted
    // result allocation. The source file and detached raw record stay intact.
    trace.BeginOwnRemap(body);
    trace.OwnRemapResult=RecordStringRemap.ApplyChecked(body,body.Length,RemapContext.Mapping);
    trace.OwnRemapStateUncertain=false;
    if(trace.OwnRemapResult.Status!=RecordStringRemapStatus.Completed)
     throw new System.IO.InvalidDataException("ArzRecordRemapUnsupported:"+trace.OwnRemapResult.Reason);
    trace.Completed("OwnArzRecordRemapped");
    trace.FailureStage="OwnArzContextProjection";
    pool=RemapContext.CopyPool();
   }
   return new SuppliedBinaryRecord(normalizedName,body,0,body.Length,tableGate,names,pool,nameIds,
    ConvertedStartTick32,ConvertedEndTick32);
  }
 }
}
