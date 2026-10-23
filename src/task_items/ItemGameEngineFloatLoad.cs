using System;
using System.Text;

namespace GrimDynasty.TaskItems {
 // Logical, explicitly supplied observations, not native pointers or a DLL
 // loader. Nonzero slot targets are observed Engine RVAs after module
 // attribution; zero explicitly denotes an observed null slot, not Engine+0.
 // Unknown, known-null, unsupported and invalidated are separate stops.
 public sealed class ItemFloatVtableObservation {
  public readonly ulong Token;
  public uint? Slot48TargetRva,Slot98TargetRva;
  public bool IsValid {get;private set;}
  public ItemFloatVtableObservation(ulong token,uint? slot48TargetRva,uint? slot98TargetRva){
   if(token==0||token>ulong.MaxValue-0x9FUL)throw new ArgumentOutOfRangeException("token");
   Token=token;Slot48TargetRva=slot48TargetRva;Slot98TargetRva=slot98TargetRva;IsValid=true;
  }
  public void Invalidate(){IsValid=false;}
 }
 // The same logical RSI table object survives both calls. Replacing these
 // observations changes its currently observed vptr/context; it does not
 // allocate or publish a native object. BinaryTable/ParsedRecord are read at
 // the actual own getter step, not cached at window entry or outer dispatch.
 public sealed class ItemFloatTableObservation {
  public readonly ulong Token;
  public ulong? Vptr;
  public ItemFloatVtableObservation Vtable;
  public LoadTableBinarySnapshot BinaryTable;
  public BinaryParsedRecord ParsedRecord;
  public bool IsValid {get;private set;}
  public ItemFloatTableObservation(ulong token,ulong? vptr,ItemFloatVtableObservation vtable,
   LoadTableBinarySnapshot binaryTable,BinaryParsedRecord parsedRecord){
   if(token==0||token>ulong.MaxValue-0xCFUL)throw new ArgumentOutOfRangeException("token");
   Token=token;Vptr=vptr;Vtable=vtable;BinaryTable=binaryTable;ParsedRecord=parsedRecord;IsValid=true;
  }
  public void Invalidate(){IsValid=false;}
 }
 // A key address is an explicit logical pointer identity for the native name
 // cache. It is never inferred from CLR array identity or a preferred image
 // VA. Bytes at that address may be replaced between steps. A missing or
 // invalidated byte view remains unknown; the absent-archive branch need not
 // read it. No native allocation, ownership or loaded lifetime is recovered.
 public sealed class ItemFloatQueryObservation {
  public readonly ulong? Address;
  byte[] bytes;
  public bool IsValid {get;private set;}
  public ItemFloatQueryObservation(ulong? address,byte[] observedCString){
   Address=address;IsValid=true;ReplaceCString(observedCString);
  }
  public void ReplaceCString(byte[] observedCString){
   if(!IsValid)throw new InvalidOperationException("Invalidated key observation");
   bytes=observedCString==null?null:(byte[])observedCString.Clone();
  }
  public byte[] CopyCString(){return IsValid&&bytes!=null?(byte[])bytes.Clone():null;}
  public void Invalidate(){IsValid=false;}
  internal byte[] ReadCString(){return IsValid?bytes:null;}
  internal int ObservedByteCount {get{return bytes==null?0:bytes.Length;}}
 }
 public sealed class ItemGameEngineFloatLoadInputs {
  public const uint FirstDefaultRva=0x7A80B8,SecondDefaultRva=0x7A80C8;
  public const uint FirstKeyRva=0x696378,SecondKeyRva=0x696360;
  public const uint ArchivedFirstDefaultBits=0x40400000,ArchivedSecondDefaultBits=0x40A00000;
  public uint? FirstDefaultBits,SecondDefaultBits;
  public ItemFloatQueryObservation FirstKey,SecondKey;
  public ItemGameEngineFloatLoadInputs(uint? firstDefaultBits,uint? secondDefaultBits,
   ItemFloatQueryObservation firstKey,ItemFloatQueryObservation secondKey){
   FirstDefaultBits=firstDefaultBits;SecondDefaultBits=secondDefaultBits;FirstKey=firstKey;SecondKey=secondKey;
  }
  // Explicit selection of the pinned disk key/default profile as the own
  // scenario's observations. This does not prove unchanged loaded values.
  // Both logical addresses must be supplied by the caller, with no fallback.
  public static ItemGameEngineFloatLoadInputs WithArchivedInputs(ulong firstLogicalKeyAddress,ulong secondLogicalKeyAddress){
   return new ItemGameEngineFloatLoadInputs(ArchivedFirstDefaultBits,ArchivedSecondDefaultBits,
    new ItemFloatQueryObservation(firstLogicalKeyAddress,Encoding.ASCII.GetBytes("itemSparkleMinTime\0")),
    new ItemFloatQueryObservation(secondLogicalKeyAddress,Encoding.ASCII.GetBytes("itemSparkleMaxTime\0")));
  }
 }
 public sealed class ItemGameEngineFloatLoadCall {
  public readonly int Number,RelativeOffset;
  public readonly uint VptrReadRva,DefaultReadRva,KeyAddressRva,CallSiteRva,StoreRva;
  public ulong? OuterVptr {get;internal set;}
  public ulong? InnerVptr {get;internal set;}
  public ulong? QueryAddress {get;internal set;}
  public uint? OuterTargetRva {get;internal set;}
  public uint? InnerTargetRva {get;internal set;}
  public uint? DefaultBits {get;internal set;}
  public ItemFloatVtableObservation OuterVtable {get;internal set;}
  public ItemFloatVtableObservation InnerVtable {get;internal set;}
  public ItemFloatQueryObservation Key {get;internal set;}
  public BinaryFloatTableGetterResult Getter {get;internal set;}
  public bool OuterDispatched {get;internal set;}
  public bool CallerStored {get;internal set;}
  internal ItemGameEngineFloatLoadCall(bool first){
   Number=first?1:2;RelativeOffset=first?0:4;
   VptrReadRva=first?0x2CEC27U:0x2CEC4DU;DefaultReadRva=first?0x2CEC2AU:0x2CEC50U;
   KeyAddressRva=first?0x2CEC37U:0x2CEC58U;CallSiteRva=first?0x2CEC41U:0x2CEC62U;StoreRva=first?0x2CEC44U:0x2CEC65U;
  }
 }
 public sealed class ItemGameEngineFloatLoadResult {
  public readonly ItemGameEngineFields State;
  public readonly ItemFloatTableObservation Table;
  public readonly ItemGameEngineFloatLoadInputs Inputs;
  public readonly ItemGameEngineFloatLoadCall First,Second;
  public ulong? ThisToken {get{return State==null?(ulong?)null:State.Token;}}
  public ulong? TableToken {get{return Table==null?(ulong?)null:Table.Token;}}
  public int VptrReads {get;internal set;}
  public int DefaultReads {get;internal set;}
  public int Slot48Reads {get;internal set;}
  public int Slot98Reads {get;internal set;}
  public int GetterCalls {get;internal set;}
  public int CallerStoreInstructions {get;internal set;}
  public bool ReachedWindowEnd {get;internal set;}
  public bool IsCompleteLoadFromDatabase {get{return false;}}
  public bool IsCompleteGameEngineConstructor {get{return false;}}
  public bool IsCompleteItemConstructor {get{return false;}}
  public string Phase {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public string ResumeModule {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public uint? CallSiteRva {get;internal set;}
  public bool MayHaveEffects {get{return CallerStoreInstructions!=0||
   (First.Getter!=null&&First.Getter.MayHaveEffects)||(Second.Getter!=null&&Second.Getter.MayHaveEffects);}}
  internal ItemGameEngineFloatLoadResult(ItemGameEngineFields state,ItemFloatTableObservation table,ItemGameEngineFloatLoadInputs inputs){
   State=state;Table=table;Inputs=inputs;First=new ItemGameEngineFloatLoadCall(true);Second=new ItemGameEngineFloatLoadCall(false);
   Phase="LocalWindowEntry";Status="Ready";ResumeModule="Game";ResumeInstructionRva=ItemGameEngineFloatLoadInvocation.EntryRva;
  }
 }
 // This starts ONLY at the local Game2CEC27 window, with explicit original
 // R13/RSI logical identities. There is no fabricated proof/boolean that the
 // earlier1904-call-containing body prefix completed. It neither invokes nor
 // completes LoadFromDatabase, its source-table acquisition, publication or
 // later2CEC81 write. Native nonvolatile-register preservation is conditional.
 //
 // A single-owner own model: four ordered steps expose changes between the
 // outer+48 and inner+98 dispatches and between the two calls. No arbitrary
 // callback supplies getter results, and no concurrent/native-thread behavior
 // is promised. Table/vtable/key views must not alias the12 GameEngine field
 // bytes. Known overlaps in the bounded views are rejected as OWN coverage
 // stops; this does not prove nonaliasing of unknown native backing/ranges or
 // reproduce native overlap behavior. All supplied logical views must be
 // mutually consistent. Binary cache/parsed/allocator storage whose address
 // is not modeled retains explicit valid-storage and nonaliasing conditions;
 // these bounded checks cannot establish them. Source observations remain
 // conditional and changing BinaryTable/ParsedRecord is an explicit new own
 // observation of the same logical table, not a native load operation.
 // Unknowns permanently stop this attempt, retaining all prior
 // DWORD/cache writes. Managed exceptions are not reconstructed native EH.
 public sealed class ItemGameEngineFloatLoadInvocation {
  public const uint EntryRva=0x2CEC27,EndRva=0x2CEC6E,BinaryScalarRva=0x13FD50,BinaryTailRva=0x13FD59;
  public readonly ItemGameEngineFloatLoadResult Result;
  int step;
  public ItemGameEngineFloatLoadInvocation(ItemGameEngineFields state,ItemFloatTableObservation table,ItemGameEngineFloatLoadInputs inputs){
   Result=new ItemGameEngineFloatLoadResult(state,table,inputs);
  }
  public ItemGameEngineFloatLoadResult DispatchFirst(){Require(0);return Dispatch(Result.First);}
  public ItemGameEngineFloatLoadResult CompleteFirst(){Require(1);return Complete(Result.First);}
  public ItemGameEngineFloatLoadResult DispatchSecond(){Require(2);return Dispatch(Result.Second);}
  public ItemGameEngineFloatLoadResult CompleteSecond(){Require(3);return Complete(Result.Second);}
  void Require(int expected){if(step!=expected)throw new InvalidOperationException("Window step is not available; attempts cannot be resumed after a stop or replayed");}
  ItemGameEngineFloatLoadResult Dispatch(ItemGameEngineFloatLoadCall call){
   // Consume this step before any allocation/helper, so an escaping own
   // exception cannot be retried to duplicate earlier effects.
   step=-1;
   string label=call.Number==1?"First":"Second";
   Mark(label+"OuterVptr","Game",call.VptrReadRva);Result.CallSiteRva=null;
   ItemFloatVtableObservation vtable;
   if(!ReadVptr(call,false,out vtable))return Result;
   // Native reads the scalar before LEA/key setup or the indirect call. An
   // unknown default stops here even if the selected getter might not use it.
   Mark(label+"Default","Game",call.DefaultReadRva);
   if(Result.Inputs==null)return Stop("UnknownFloatLoadInputs");
   Result.DefaultReads++;call.DefaultBits=call.Number==1?Result.Inputs.FirstDefaultBits:Result.Inputs.SecondDefaultBits;
   if(!call.DefaultBits.HasValue)return Stop("UnknownCallerFloatDefaultBits");
   Mark(label+"KeyAddress","Game",call.KeyAddressRva);
   call.Key=call.Number==1?Result.Inputs.FirstKey:Result.Inputs.SecondKey;
   call.QueryAddress=call.Key==null?(ulong?)null:call.Key.Address;
   Mark(label+"OuterSlot","Game",call.CallSiteRva);Result.CallSiteRva=call.CallSiteRva;
   // Uses the vptr just read, not a replacement table.Vptr. The actual leaf
   // will independently read table.Vptr when CompleteFirst/Second is called.
   if(!ValidateVtable(call.OuterVptr,vtable))return Result;
   Result.Slot48Reads++;call.OuterTargetRva=vtable.Slot48TargetRva;
   if(!call.OuterTargetRva.HasValue)return Stop("UnknownOuterFloatTarget");
   if(call.OuterTargetRva.Value==0)return Stop("NullOuterFloatTarget");
   if(call.OuterTargetRva.Value!=BinaryScalarRva)return Stop("UnsupportedOuterFloatTarget");
   call.OuterDispatched=true;step=call.Number==1?1:3;
   Mark(label+"AwaitInnerDispatch","Engine",BinaryScalarRva);Result.Status="AwaitingInnerDispatch";return Result;
  }
  ItemGameEngineFloatLoadResult Complete(ItemGameEngineFloatLoadCall call){
   step=-1;
   string label=call.Number==1?"First":"Second";
   Mark(label+"InnerVptr","Engine",BinaryScalarRva);
   ItemFloatVtableObservation vtable;
   if(!ReadVptr(call,true,out vtable))return Result;
   Mark(label+"InnerSlot","Engine",BinaryTailRva);
   if(!ValidateVtable(call.InnerVptr,vtable))return Result;
   Result.Slot98Reads++;call.InnerTargetRva=vtable.Slot98TargetRva;
   if(!call.InnerTargetRva.HasValue)return Stop("UnknownInnerFloatTarget");
   if(call.InnerTargetRva.Value==0)return Stop("NullInnerFloatTarget");
   if(call.InnerTargetRva.Value!=BinaryFloatTableGetter.WrapperRva)return Stop("UnsupportedInnerFloatTarget");
   //13FD53 moves the captured XMM2 default to XMM3;13FD56 sets R8D=0.
   // This is the real own wrapper/name/numeric implementation, not a supplied
   // successful return observation or string-getter surrogate.
   Mark(label+"Getter","Engine",BinaryFloatTableGetter.WrapperRva);Result.GetterCalls++;
   call.Getter=BinaryFloatTableGetter.GetValue(Result.Table.BinaryTable,Result.Table.ParsedRecord,
    call.Key==null?null:call.Key.ReadCString(),call.DefaultBits,call.QueryAddress);
   if(!call.Getter.Returned||!call.Getter.ValueBits.HasValue){
    Result.ResumeInstructionRva=call.Getter.ResumeInstructionRva;
    return Stop(call.Getter.Reason??"UnknownFloatGetterReturnBits");
   }
   Mark(label+"Store","Game",call.StoreRva);
   if(Result.State==null)return Stop("UnknownGameEngineFields");
   if(!Result.State.IsValid)return Stop("InvalidatedGameEngineFields");
   // MOVSS copies raw32 bits. It does not normalize NaNs, erase -0, clamp,
   // or touch relative8..11. State and its Token are retained by identity.
   Result.State.Store(call.RelativeOffset,4,call.Getter.ValueBits.Value);
   call.CallerStored=true;Result.CallerStoreInstructions++;step=call.Number==1?2:4;
   Result.CallSiteRva=null;
   if(call.Number==1){Mark("AwaitSecondDispatch","Game",0x2CEC4D);Result.Status="Ready";}
   else{Mark("WindowEnd","Game",EndRva);Result.ReachedWindowEnd=true;Result.Status="ReachedWindowEnd";}
   return Result;
  }
  bool ReadVptr(ItemGameEngineFloatLoadCall call,bool inner,out ItemFloatVtableObservation vtable){
   vtable=null;
   if(Result.Table==null){Stop("UnknownFloatTableObject");return false;}
   if(!Result.Table.IsValid){Stop("InvalidatedFloatTableObject");return false;}
   // Own-view consistency checks, not extra native branches or memory reads.
   // The table's observed prefix includes the wrapper's C8 QWORD.
   if(OverlapsGlobal(Result.Table.Token,0xD0UL)){Stop("AliasedGlobalAndTableViewsOutsideCoverage");return false;}
   if(KeyOverlapsGlobal(call.Key)||(Result.Inputs!=null&&
    (KeyOverlapsGlobal(Result.Inputs.FirstKey)||KeyOverlapsGlobal(Result.Inputs.SecondKey)))){
    Stop("AliasedGlobalAndQueryViewsOutsideCoverage");return false;
   }
   Result.VptrReads++;ulong? pointer=Result.Table.Vptr;
   if(inner)call.InnerVptr=pointer;else call.OuterVptr=pointer;
   if(!pointer.HasValue){Stop("UnknownFloatTableVptr");return false;}
   vtable=Result.Table.Vtable;
   if(inner)call.InnerVtable=vtable;else call.OuterVtable=vtable;
   return true;
  }
  bool ValidateVtable(ulong? pointer,ItemFloatVtableObservation vtable){
   // Checking the backing represents the later indirect slot read. A known
   // null/invalid vptr does not suppress the earlier scalar read in Game.
   if(!pointer.HasValue){Stop("UnknownFloatTableVptr");return false;}
   if(pointer.Value==0){Stop("NullFloatTableVptr");return false;}
   if(vtable==null){Stop("UnknownFloatVtableBacking");return false;}
   if(vtable.Token!=pointer.Value){Stop("FloatVtableIdentityMismatch");return false;}
   if(!vtable.IsValid){Stop("InvalidatedFloatVtable");return false;}
   if(OverlapsGlobal(vtable.Token,0xA0UL)){Stop("AliasedGlobalAndVtableViewsOutsideCoverage");return false;}
   return true;
  }
  bool KeyOverlapsGlobal(ItemFloatQueryObservation key){
   if(key==null||!key.Address.HasValue||key.Address.Value==0)return false;
   // Metadata only: no CString dereference is invented on absent archives.
   // Unknown length still lets us reject an address within the field view;
   // unknown ranges elsewhere retain the declared nonaliasing condition.
   return OverlapsGlobal(key.Address.Value,(ulong)Math.Max(1,key.ObservedByteCount));
  }
  bool OverlapsGlobal(ulong start,ulong count){
   if(Result.State==null||count==0)return false;
   ulong first=Result.State.Token+(ulong)ItemGameEngineFields.FirstOffset;
   ulong last=first+((ulong)ItemGameEngineFields.ByteCount-1);
   if(start>last)return false;
   return start>=first||count>first-start;
  }
  void Mark(string phase,string module,uint rva){Result.Phase=phase;Result.ResumeModule=module;Result.ResumeInstructionRva=rva;}
  ItemGameEngineFloatLoadResult Stop(string reason){step=-1;Result.Status="Unsupported";Result.Reason=reason;return Result;}
 }
}
