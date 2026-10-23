using System;
namespace GrimDynasty.TaskItems {
 // Explicit inputs at the already-verified borrowed-record boundary. The
 // caller supplies a known fresh Binary-storage contract and archive context;
 // this does not fetch a record, recover a native constructor/vptr, sample a
 // clock, own native reader storage, or reconstruct exception cleanup.
 public sealed class SuppliedBinaryRecord {
  readonly byte[] key,bytes;
  readonly int initialOffset,byteLimit;
  readonly BinaryPointerCacheSnapshot names;
  readonly TableString[] sharedPool;
  readonly BinaryStringPool completePool;
  readonly BinaryNameIdSnapshot nameIds;
  internal readonly Func<RecordLoadTickResult> Clock;
  public readonly RegistrationGate TableGate;
  // Low 32 bits AFTER the respective native clock conversion. Null remains
  // unknown; these are not guessed raw API results. Fresh-source Clock is a
  // separate explicit owned hook, sampled only by the enclosing miss caller.
  public readonly uint? ConvertedStartTick32,ConvertedEndTick32;
  public SuppliedBinaryRecord(byte[] normalizedKey,byte[] completeBytes,int initialOffset,int byteLimit,
   RegistrationGate tableGate,BinaryPointerCacheSnapshot names,TableString[] sharedPool,
   BinaryNameIdSnapshot nameIds,uint? convertedStartTick32,uint? convertedEndTick32){
   if(normalizedKey==null)throw new ArgumentNullException("normalizedKey");
   if(completeBytes==null)throw new ArgumentNullException("completeBytes");
   if(tableGate==null)throw new ArgumentNullException("tableGate");
   if(names==null)throw new ArgumentNullException("names");
   if(sharedPool==null)throw new ArgumentNullException("sharedPool");
   if(nameIds==null)throw new ArgumentNullException("nameIds");
   if(normalizedKey.Length>RecordCacheSnapshot.MaximumNameBytes)throw new ArgumentOutOfRangeException("normalizedKey");
   if(completeBytes.Length>BinaryRecordReader.MaximumBytes)throw new ArgumentOutOfRangeException("completeBytes");
   if(initialOffset<0||byteLimit<initialOffset||byteLimit>completeBytes.Length)throw new ArgumentOutOfRangeException("byteLimit");
   if(sharedPool.Length>BinaryWarmState.MaximumStrings)throw new ArgumentOutOfRangeException("sharedPool");
   var pool=(TableString[])sharedPool.Clone();int poolBytes=0;
   foreach(var item in pool){
    if(item==null)throw new ArgumentException("Complete shared string pool required","sharedPool");
    poolBytes+=item.Length;
    if(poolBytes>BinaryWarmState.MaximumPoolBytes)throw new ArgumentOutOfRangeException("sharedPool");
   }
   key=(byte[])normalizedKey.Clone();bytes=(byte[])completeBytes.Clone();
   this.initialOffset=initialOffset;this.byteLimit=byteLimit;TableGate=tableGate;
   this.names=names;this.sharedPool=pool;this.nameIds=nameIds;
   ConvertedStartTick32=convertedStartTick32;ConvertedEndTick32=convertedEndTick32;
  }
  internal static SuppliedBinaryRecord FromFreshPool(byte[] normalizedKey,byte[] completeBytes,
   RegistrationGate tableGate,BinaryPointerCacheSnapshot names,BinaryStringPool completePool,
   BinaryNameIdSnapshot nameIds,uint? convertedStartTick32,uint? convertedEndTick32,
   Func<RecordLoadTickResult> captureClock){
   return new SuppliedBinaryRecord(normalizedKey,completeBytes,tableGate,names,completePool,
    nameIds,convertedStartTick32,convertedEndTick32,captureClock);
  }
  SuppliedBinaryRecord(byte[] normalizedKey,byte[] completeBytes,RegistrationGate tableGate,
   BinaryPointerCacheSnapshot names,BinaryStringPool completePool,BinaryNameIdSnapshot nameIds,
   uint? convertedStartTick32,uint? convertedEndTick32,Func<RecordLoadTickResult> captureClock){
   if(normalizedKey==null)throw new ArgumentNullException("normalizedKey");
   if(completeBytes==null)throw new ArgumentNullException("completeBytes");
   if(tableGate==null)throw new ArgumentNullException("tableGate");
   if(names==null)throw new ArgumentNullException("names");
   if(completePool==null)throw new ArgumentNullException("completePool");
   if(nameIds==null)throw new ArgumentNullException("nameIds");
   if(!completePool.IsRawBacked||!completePool.IsUnique||completePool.EmptyId<0)
    throw new ArgumentException("A complete unique raw pool with its original empty ID is required","completePool");
   if(normalizedKey.Length>RecordCacheSnapshot.MaximumNameBytes)throw new ArgumentOutOfRangeException("normalizedKey");
   if(completeBytes.Length>BinaryRecordReader.MaximumBytes)throw new ArgumentOutOfRangeException("completeBytes");
   key=(byte[])normalizedKey.Clone();bytes=(byte[])completeBytes.Clone();
   initialOffset=0;byteLimit=completeBytes.Length;TableGate=tableGate;
   this.names=names;this.completePool=completePool;this.nameIds=nameIds;Clock=captureClock;
   ConvertedStartTick32=convertedStartTick32;ConvertedEndTick32=convertedEndTick32;
  }
  // Exact full stored bytes: no case conversion, slash normalization, hash-only
  // equality or early termination at an embedded NUL.
  internal bool MatchesKey(byte[] normalizedKey){
   if(normalizedKey==null||normalizedKey.Length!=key.Length)return false;
   for(int i=0;i<key.Length;i++)if(key[i]!=normalizedKey[i])return false;
   return true;
  }
  // Input can be reused to create independent readers. The enclosing attempt
  // controls replay; parser tags/counts are deliberately left to its preflight.
  internal BinaryRecordReader CreateReader(){
   return new BinaryRecordReader(TableGate,bytes,initialOffset,byteLimit);
  }
  internal LoadTableBinarySnapshot CreateFreshTable(){
   // A real own table identity exists before parsing. An empty outer field
   // array represents fresh absent bucket storage, not invented parsed fields.
   // Shared names retain identity and require this gate or exclusive ownership.
   var fields=new BinaryFieldSnapshot(0,0,new BinaryFieldEntry[0][]);
   var fresh=completePool==null?
    new BinaryWarmState(names,sharedPool,fields,new int[0],nameIds):
    BinaryWarmState.FromPool(names,completePool,fields,new int[0],nameIds);
   return new LoadTableBinarySnapshot(TableGate,true,fresh);
  }
  // Internal caller supplies the successful record from its own CreateReader.
  // Preserve table identity and install only while the same table gate is held.
  internal void InstallParsed(LoadTableBinarySnapshot table,BinaryParsedRecord record){
   if(table==null)throw new ArgumentNullException("table");
   if(record==null)throw new ArgumentNullException("record");
   if(!Object.ReferenceEquals(table.Gate,TableGate)||!Object.ReferenceEquals(record.Gate,TableGate))
    throw new ArgumentException("Supplied table and parsed record must share the table gate");
   TableGate.Enter();
   try{table.InstallParsedWarm(completePool==null?
    record.ToWarmState(names,sharedPool,nameIds):record.ToWarmStateWithPool(names,completePool,nameIds));}
   finally{TableGate.Exit();}
  }
 }
}
