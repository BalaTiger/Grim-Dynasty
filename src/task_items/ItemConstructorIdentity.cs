using System;

namespace GrimDynasty.TaskItems {
 public sealed class ItemObjectIdGetterResult {
  public readonly ItemConstructorStorage State;
  public uint? ReturnedValue {get;internal set;}
  public ulong? ReturnedRax {get;internal set;}
  public bool Returned {get;internal set;}
  // Counts one own field-observation attempt, never native execution.
  public int FieldReads {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  internal ItemObjectIdGetterResult(ItemConstructorStorage state){
   State=state;Status="Unsupported";ResumeInstructionRva=ItemObjectIdGetter.EntryRva;
  }
 }
 public static class ItemObjectIdGetter {
  public const uint EntryRva=0x3A740,ReturnRva=0x3A743;
  public const int FieldOffset=0x2C,FieldWidth=4;
  // Exact saved normal bytes8B412CC3: MOV EAX,DWORD[RCX+2C];RET.
  // The four trailingCC bytes in the bounded8B window are not this path.
  // No exact .pdata entry was found: the body is established by these bytes,
  // not by interpreting the metadata gap as a complete function size.
  // This reads the current own shared Actor fields. Constructors have not
  // established a value at+2C, so unknown bytes cannot be treated as zero.
  public static ItemObjectIdGetterResult Read(ItemConstructorStorage state){
   var result=new ItemObjectIdGetterResult(state);
   if(state==null){result.Reason="UnknownItemConstructorStorage";return result;}
   if(!state.HasRange(FieldOffset,FieldWidth)){result.Reason="MissingObjectIdFieldStorage";return result;}
   result.FieldReads=1;
   ulong? observed=state.ReadLittleEndian(FieldOffset,FieldWidth);
   if(!observed.HasValue){result.Reason="UnknownObjectIdField";return result;}
   // The32-bit EAX destination zero-extends into RAX, including values whose
   // high bit is set. The ID is data, not an address or a signed error code.
   result.ReturnedValue=(uint)observed.Value;result.ReturnedRax=(ulong)result.ReturnedValue.Value;
   result.Returned=true;result.Status="KnownReturn";result.ResumeInstructionRva=ReturnRva;return result;
  }
 }
 public sealed class ItemConstructorIdentityResult {
  public readonly ItemConstructorBE0Result Prior;
  public readonly ItemConstructorStorage State;
  public ItemObjectIdGetterResult Getter {get;internal set;}
  public int CallerStoreInstructions {get;internal set;}
  public string Stage {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public uint? CallSiteRva {get;internal set;}
  public bool ReachedItem323977 {get;internal set;}
  public bool IsCompleteItemConstructor {get{return false;}}
  internal ItemConstructorIdentityResult(ItemConstructorBE0Result prior){
   Prior=prior;State=prior==null?null:prior.State;
   Status="Unsupported";Reason="NotRun";Stage="NotRun";ResumeInstructionRva=ItemConstructorIdentityInvocation.EntryRva;
  }
 }
 // One same-state continuation through GetObjectId and its immediate DWORD
 // copy. The invocation constructor does not read/snapshot the source field.
 // Conditional on the archived import/body and the real prior own prefix;
 // this neither executes original code nor supplies an ID allocator/writer.
 // Unknown observations stop coverage at the getter load, not a fabricated
 // native failure branch. No callbacks, cleanup or rollback are introduced.
 public sealed class ItemConstructorIdentityInvocation {
  public const uint EntryRva=0x323968,NextRva=0x323977,CopyRva=0x32396E;
  public readonly ItemConstructorIdentityResult Result;
  public bool HasRun {get;private set;}
  readonly ItemConstructorStorage state;
  public ItemConstructorIdentityInvocation(ItemConstructorBE0Result prior){
   Result=new ItemConstructorIdentityResult(prior);state=Result.State;
  }
  public ItemConstructorIdentityResult Run(){
   if(HasRun)throw new InvalidOperationException("This Item identity continuation has already been attempted");
   HasRun=true;
   ItemConstructorBE0Result prior=Result.Prior;
   if(prior==null||state==null)return Stop("MissingPriorItemBE0Prefix",EntryRva,"Prior");
   if(!prior.ReachedItem323968||prior.Status!="ConditionalItemPrefix"||prior.ResumeInstructionRva!=EntryRva)
    return Stop("PriorItemBE0PrefixIncomplete",prior.ResumeInstructionRva,"Prior");
   Result.Stage="Getter";Result.CallSiteRva=EntryRva;Result.ResumeInstructionRva=ItemObjectIdGetter.EntryRva;
   Result.Getter=ItemObjectIdGetter.Read(state);
   if(!Result.Getter.Returned)return Stop(Result.Getter.Reason,Result.Getter.ResumeInstructionRva,"Getter");
   Result.Stage="CallerSuffix";Result.CallSiteRva=null;Result.ResumeInstructionRva=CopyRva;
   if(!state.HasRange(0x538,4))return Stop("MissingItemIdentityFieldStorage",CopyRva,"CallerSuffix");
   // Copy the completed getter value once. Later source changes cannot update
   // this already stored DWORD, and bytes+53C onward remain untouched.
   state.Store(0x538,4,Result.Getter.ReturnedValue.Value);Result.CallerStoreInstructions=1;
   Result.ReachedItem323977=true;Result.Status="ConditionalItemPrefix";Result.Reason=null;
   Result.Stage="Item323977";Result.ResumeInstructionRva=NextRva;return Result;
  }
  ItemConstructorIdentityResult Stop(string reason,uint rva,string stage){
   Result.Status="Unsupported";Result.Reason=reason;Result.ResumeInstructionRva=rva;Result.Stage=stage;return Result;
  }
 }
}
