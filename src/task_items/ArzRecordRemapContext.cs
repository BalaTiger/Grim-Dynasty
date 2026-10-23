using System;
using GrimDynasty.Resources;
namespace GrimDynasty.TaskItems {
 // A supplied mapping, or a snapshot from the bounded observed import model.
 // Neither path claims native container/cache initialization or archive fetch.
 public sealed class ArzRecordRemapContext {
  readonly TableString[] targetPool;
  readonly ArchiveStringImportResult generatedImport;
  public readonly RecordStringRemapContext Mapping;
  public int TargetStringCount {get{return targetPool.Length;}}
  public ArzRecordRemapContext(RecordStringRemapContext mapping,TableString[] knownTargetPool){
   if(mapping==null)throw new ArgumentNullException("mapping");
   if(knownTargetPool==null)throw new ArgumentNullException("knownTargetPool");
   if(knownTargetPool.Length>BinaryWarmState.MaximumStrings)
    throw new ArgumentOutOfRangeException("knownTargetPool");
   int bytes=0;
   foreach(var value in knownTargetPool){
    if(value==null)throw new ArgumentException("Complete target pool required","knownTargetPool");
    bytes+=value.Length;
    if(bytes>BinaryWarmState.MaximumPoolBytes)throw new ArgumentOutOfRangeException("knownTargetPool");
   }
   Mapping=mapping;targetPool=(TableString[])knownTargetPool.Clone();
  }
  ArzRecordRemapContext(RecordStringRemapContext mapping,TableString[] pool,
   ArchiveStringImportResult import):this(mapping,pool){generatedImport=import;}

  // The raw reader has already validated and detached this table. Import its
  // original wire bytes once, in explicit initial/append order. No record body
  // is modified by import; its separate load attempt still owns remap scratch.
  public static ArchiveStringImportResult ImportChecked(ArchiveStringImportState state,
   ArzRawRecord raw,bool append){
   if(state==null)throw new ArgumentNullException("state");
   if(raw==null)throw new ArgumentNullException("raw");
   if(raw.StringTableBytes>ArchiveStringImportState.MaximumWireBytes)
    throw new System.IO.InvalidDataException("ArchiveStringTableOutsideImportCoverage");
   return state.ImportChecked(raw.CopyStringTableBytes(),append);
  }

  // No caller-supplied mapping or destination pool. Historical successful
  // handles select their own append base against the current complete state.
  // Initial duplicate IDs are deliberately not repaired with an invented map.
  public static ArzRecordRemapContext FromGeneratedImport(ArchiveStringImportState state,
   ArchiveStringImportResult import){
   if(state==null)throw new ArgumentNullException("state");
   if(import==null)throw new ArgumentNullException("import");
   RecordStringRemapContext mapping=state.CreateRemapContext(import);
   if(import.LengthFallbacks!=0)
    throw new InvalidOperationException("GeneratedArzContextRequiresValidatedSourceTable");
   byte[][] bytes=state.CopyTargetStrings();
   TableString[] pool=new TableString[bytes.Length];
   for(int i=0;i<pool.Length;i++)pool[i]=new TableString(bytes[i]);
   return new ArzRecordRemapContext(mapping,pool,import);
  }

  internal void ValidateSource(ArzRawRecord raw){
   if(generatedImport==null)return; // Existing explicit context contract.
   if(raw.StringCount!=generatedImport.SourceStringCount)
    throw new System.IO.InvalidDataException("GeneratedArchiveStringPoolMismatch");
   for(int i=0;i<raw.StringCount;i++){
    byte[] actual=raw.CopyStringBytes((uint)i),expected=generatedImport.CopySourceString(i);
    if(actual.Length!=expected.Length)
     throw new System.IO.InvalidDataException("GeneratedArchiveStringPoolMismatch");
    for(int j=0;j<actual.Length;j++)
     if(actual[j]!=expected[j])
      throw new System.IO.InvalidDataException("GeneratedArchiveStringPoolMismatch");
   }
  }
  internal TableString[] CopyPool(){return (TableString[])targetPool.Clone();}
 }
}
