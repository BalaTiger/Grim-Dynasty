using System;
using System.Collections.Generic;

namespace GrimDynasty.TaskItems {
 // Explicit own bytes at an identity token. This is not an allocator, native
 // address dereference or string lifetime reconstruction. Owners may mutate
 // this same region or invalidate it; later pointer reads observe that state.
 public sealed class ItemNameRegion {
  public const int MaximumBytes=65536;
  public readonly ulong BaseToken;
  readonly byte?[] bytes;
  public int ByteCount {get{return bytes.Length;}}
  public bool IsValid {get;private set;}
  public ItemNameRegion(ulong baseToken,byte?[] observation){
   if(observation==null)throw new ArgumentNullException("observation");
   if(observation.Length<1||observation.Length>MaximumBytes)throw new ArgumentOutOfRangeException("observation");
   if(baseToken==0||baseToken>ulong.MaxValue-(ulong)(observation.Length-1))throw new ArgumentOutOfRangeException("baseToken");
   BaseToken=baseToken;bytes=(byte?[])observation.Clone();IsValid=true;
  }
  public byte? ReadByte(int offset){Check(offset,1);return bytes[offset];}
  public void SetByte(int offset,byte? value){Check(offset,1);bytes[offset]=value;}
  public void Store(int offset,int width,ulong value){Check(offset,width);for(int i=0;i<width;i++)bytes[offset+i]=unchecked((byte)(value>>(8*i)));}
  public void Invalidate(){IsValid=false;}
  void Check(int offset,int width){
   if(!IsValid)throw new InvalidOperationException("The own region has been invalidated");
   if(width<1||width>8||offset<0||offset>bytes.Length-width)throw new ArgumentOutOfRangeException("offset");
  }
 }
 public sealed class ItemNameMemory {
  public const int MaximumRegions=8;
  public readonly ItemConstructorStorage State;
  readonly ItemNameRegion[] regions;
  public ItemNameMemory(ItemConstructorStorage state,params ItemNameRegion[] regions){
   if(state==null)throw new ArgumentNullException("state");
   if(regions==null)throw new ArgumentNullException("regions");
   if(regions.Length>MaximumRegions)throw new ArgumentOutOfRangeException("regions");
   State=state;this.regions=(ItemNameRegion[])regions.Clone();
   for(int i=0;i<this.regions.Length;i++){
    ItemNameRegion r=this.regions[i];if(r==null)throw new ArgumentException("Null region","regions");
    if(Overlaps(r.BaseToken,r.ByteCount,state.Token,state.ByteCount))throw new ArgumentException("Region overlaps Item storage","regions");
    for(int j=0;j<i;j++)if(Overlaps(r.BaseToken,r.ByteCount,this.regions[j].BaseToken,this.regions[j].ByteCount))
     throw new ArgumentException("Regions overlap","regions");
   }
  }
  static bool Overlaps(ulong a,int an,ulong b,int bn){return a<=b+(ulong)(bn-1)&&b<=a+(ulong)(an-1);}
  static bool Within(ulong address,int count,ulong start,int size){
   return count>0&&count<=size&&address>=start&&address-start<=(ulong)(size-count);
  }
  internal bool HasRange(ulong address,int count){
   if(Within(address,count,State.Token,State.ByteCount))return true;
   foreach(ItemNameRegion r in regions)if(r.IsValid&&Within(address,count,r.BaseToken,r.ByteCount))return true;
   return false;
  }
  public bool TryReadByte(ulong address,out byte? value){
   if(Within(address,1,State.Token,State.ByteCount)){value=State.ReadByte((int)(address-State.Token));return true;}
   foreach(ItemNameRegion r in regions)if(r.IsValid&&Within(address,1,r.BaseToken,r.ByteCount)){value=r.ReadByte((int)(address-r.BaseToken));return true;}
   value=null;return false;
  }
  public bool TryWriteByte(ulong address,byte value){
   if(Within(address,1,State.Token,State.ByteCount)){State.Store((int)(address-State.Token),1,value);return true;}
   foreach(ItemNameRegion r in regions)if(r.IsValid&&Within(address,1,r.BaseToken,r.ByteCount)){r.Store((int)(address-r.BaseToken),1,value);return true;}
   return false;
  }
 }
 public sealed class ItemObjectNameGetterResult {
  public readonly ItemConstructorStorage State;
  public ulong? ReturnedPointer {get;internal set;}
  public bool Returned {get;internal set;}
  public int FieldReads {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  internal ItemObjectNameGetterResult(ItemConstructorStorage state){State=state;Status="Unsupported";ResumeInstructionRva=ItemObjectNameGetter.EntryRva;}
 }
 public static class ItemObjectNameGetter {
  public const uint EntryRva=0x3A760,ReturnRva=0x3A76E;
  // Saved15B normal body only: CMP[this+20],16;LEA RAX,this+8;JB;MOV RAX,[RAX];RET.
  // No length or payload read. A known null heap pointer still returns; mapping
  // its bytes is a separate later observation, not an implicit empty string.
  public static ItemObjectNameGetterResult Read(ItemConstructorStorage state){
   var r=new ItemObjectNameGetterResult(state);
   if(state==null){r.Reason="UnknownItemConstructorStorage";return r;}
   r.FieldReads++;ulong? capacity=state.ReadLittleEndian(0x20,8);
   if(!capacity.HasValue){r.Reason="UnknownNameCapacity";return r;}
   ulong pointer=state.Token+8;
   if(capacity.Value>=16){
    r.ResumeInstructionRva=0x3A76B;r.FieldReads++;ulong? observed=state.ReadLittleEndian(8,8);
    if(!observed.HasValue){r.Reason="UnknownNamePointer";return r;}pointer=observed.Value;
   }
   r.ReturnedPointer=pointer;r.Returned=true;r.Status="KnownReturn";r.ResumeInstructionRva=ReturnRva;return r;
  }
 }
 public sealed class ItemNameScanResult {
  public readonly ItemNameMemory Memory;
  public readonly ulong Pointer;
  public ulong? Length {get;internal set;}
  public int ByteReads {get;internal set;}
  public bool Terminated {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  internal ItemNameScanResult(ItemNameMemory memory,ulong pointer){Memory=memory;Pointer=pointer;Status="Unsupported";ResumeInstructionRva=0x32397D;}
 }
 public static class ItemNameScan {
  public const int MaximumScanBytes=65536;
  // Budget bounds distinct byte positions, not native string length. Native
  // caller first reads index0 at32397D; nonempty input then reads index0 AGAIN
  // at32398A because savedRSI=-1 is incremented before the loop's first compare.
  public static ItemNameScanResult Scan(ItemNameMemory memory,ulong pointer,int budget=MaximumScanBytes){
   var r=new ItemNameScanResult(memory,pointer);
   if(memory==null){r.Reason="UnknownNameMemory";return r;}
   if(budget<1||budget>MaximumScanBytes){r.Reason="InvalidNameScanBudget";return r;}
   byte? value;r.ByteReads++;
   if(!memory.TryReadByte(pointer,out value)){r.Reason="UnmappedNameByte";return r;}
   if(!value.HasValue){r.Reason="UnknownNameByte";return r;}
   if(value.Value==0)return Complete(r,0);
   r.ResumeInstructionRva=0x32398A;
   for(int index=0;index<budget;index++){
    if(pointer>ulong.MaxValue-(ulong)index){r.Reason="NamePointerOverflow";return r;}
    r.ByteReads++;
    if(!memory.TryReadByte(pointer+(ulong)index,out value)){r.Reason="UnmappedNameByte";return r;}
    if(!value.HasValue){r.Reason="UnknownNameByte";return r;}
    if(value.Value==0)return Complete(r,(ulong)index);
   }
   r.Reason="NameScanBudgetExceeded";return r;
  }
  static ItemNameScanResult Complete(ItemNameScanResult r,ulong length){
   r.Length=length;r.Terminated=true;r.Status="KnownTerminated";r.ResumeInstructionRva=0x323990;return r;
  }
 }
 public sealed class ItemNameAssignResult {
  public readonly ItemNameMemory Memory;
  public readonly ItemConstructorStorage State;
  public readonly int TargetOffset;
  public readonly ulong SourcePointer,Count;
  public ulong? ReturnedTarget {get;internal set;}
  public bool Returned {get;internal set;}
  public int FieldReads {get;internal set;}
  public int FieldStoreInstructions {get;internal set;}
  public int CopyBytes {get;internal set;}
  public bool MemcpyUsed {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  internal ItemNameAssignResult(ItemNameMemory memory,int target,ulong source,ulong count){
   Memory=memory;State=memory==null?null:memory.State;TargetOffset=target;SourcePointer=source;Count=count;
   Status="Unsupported";Reason="NotRun";ResumeInstructionRva=ItemNameAssignInvocation.EntryRva;
  }
 }
 // Bounded no-growth projection of Game016460. Source null bypasses alias
 // checks; an alias dispatch happens BEFORE count/error/growth checks. No
 // generic string algorithm, growth helper or alias tail helper is substituted.
 // Own execution is serial with exclusive access apart from explicit mutations
 // between invocations; it makes no concurrent-read/atomicity guarantee. Range
 // overflow/budget checks are coverage guards, not added native branches. A
 // mapped raw observation does not certify valid native C++ string invariants.
 public sealed class ItemNameAssignInvocation {
  public const uint EntryRva=0x16460;
  public const int MaximumCopyBytes=65536;
  public readonly ItemNameAssignResult Result;
  public bool HasRun {get;private set;}
  readonly ItemNameMemory memory;readonly ItemConstructorStorage state;
  readonly int offset;readonly ulong source,count;
  ulong target;
  public ItemNameAssignInvocation(ItemNameMemory memory,int targetOffset,ulong sourcePointer,ulong count){
   this.memory=memory;state=memory==null?null:memory.State;offset=targetOffset;source=sourcePointer;this.count=count;
   Result=new ItemNameAssignResult(memory,targetOffset,sourcePointer,count);
  }
  public ItemNameAssignResult Run(bool normalMemcpy=false){
   if(HasRun)throw new InvalidOperationException("This own string assignment has already been attempted");HasRun=true;
   if(memory==null)return Stop("UnknownNameMemory",EntryRva);
   if(offset<0||!state.HasRange(offset,0x20))return Stop("MissingStringTargetStorage",EntryRva);
   target=state.Token+(ulong)offset;
   ulong capacity,buffer,length;
   if(source!=0){
    if(!Field(0x18,0x1647D,"UnknownStringCapacity",out capacity))return Result;
    buffer=target;
    if(capacity>=16&&!Field(0,0x16487,"UnknownStringPointer",out buffer))return Result;
    if(source>=buffer){
     // The pointer reread at1649A occurs only after the first comparison.
     if(capacity>=16&&!Field(0,0x1649A,"UnknownStringPointer",out buffer))return Result;
     if(!Field(0x10,0x1649D,"UnknownStringLength",out length))return Result;
     if(length>ulong.MaxValue-buffer)return Stop("AliasRangeOverflow",0x1649D);
     if(source<buffer+length){
      // The alias arm selects the base again to form the tail helper's offset.
      if(capacity>=16&&!Field(0,0x164AC,"UnknownStringPointer",out buffer))return Result;
      return Stop("UnresolvedStringAliasHelper",0x164D2);
     }
    }
   }
   if(count==ulong.MaxValue)return Stop("UnresolvedStringLengthError",0x164E4);
   if(!Field(0x18,0x164EB,"UnknownStringCapacity",out capacity))return Result;
   if(count>capacity){
    if(!Field(0x10,0x164F1,"UnknownStringLength",out length))return Result;
    return Stop("UnresolvedStringGrowth",0x164FB);
   }
   if(count==0){
    if(!Field(0x18,0x16516,"UnknownStringCapacity",out capacity))return Result;
    if(!LengthStore(0x1651B))return Result;
    buffer=target;
    if(capacity>=16&&!Field(0,0x16521,"UnknownStringPointer",out buffer))return Result;
    if(!Terminator(buffer,capacity<16?0x1653DU:0x16524U))return Result;
    return Complete(capacity<16?0x1654FU:0x16539U);
   }
   if(!Field(0x18,0x16505,"UnknownStringCapacity",out capacity))return Result;
   buffer=target;
   if(capacity>=16&&!Field(0,0x1650C,"UnknownStringPointer",out buffer))return Result;
   Result.ResumeInstructionRva=0x1655E;
   if(!normalMemcpy)return Stop("MissingNormalMemcpyObservation",0x1655E);
   if(count>MaximumCopyBytes)return Stop("CopyOutsideCoveredBudget",0x1655E);
   if(count-1>ulong.MaxValue-source||count-1>ulong.MaxValue-buffer)return Stop("CopyRangeOverflow",0x1655E);
   if(source<=buffer+count-1&&buffer<=source+count-1)return Stop("OverlappingMemcpyRanges",0x1655E);
   int n=(int)count;
   if(!memory.HasRange(source,n))return Stop("UnmappedCopySource",0x1655E);
   if(!memory.HasRange(buffer,n))return Stop("UnmappedCopyDestination",0x1655E);
   // Explicit normal memcpy on known disjoint valid ranges. Preinspect own
   // source bytes before mutation; this is a coverage choice, NOT a promise
   // that native memcpy faults or unknown inputs fail atomically. No callback.
   byte[] observed=new byte[n];
   for(int i=0;i<n;i++){
    byte? value;if(!memory.TryReadByte(source+(ulong)i,out value))return Stop("UnmappedCopySource",0x1655E);
    if(!value.HasValue)return Stop("UnknownCopySourceByte",0x1655E);observed[i]=value.Value;
   }
   Result.MemcpyUsed=true;
   for(int i=0;i<n;i++){
    if(!memory.TryWriteByte(buffer+(ulong)i,observed[i]))return Stop("UnmappedCopyDestination",0x1655E);
    Result.CopyBytes++;
   }
   // Copy can affect this same observed storage. Native code rereads capacity
   // after return, then stores length, then rereads the pointer if heap-selected.
   if(!Field(0x18,0x16563,"UnknownStringCapacity",out capacity))return Result;
   if(!LengthStore(0x16568))return Result;
   buffer=target;
   if(capacity>=16&&!Field(0,0x1656E,"UnknownStringPointer",out buffer))return Result;
   if(count>ulong.MaxValue-buffer)return Stop("StringTerminatorOverflow",0x16576);
   if(!Terminator(buffer+count,0x16576))return Result;
   return Complete(0x1658C);
  }
  bool Field(int relative,uint rva,string reason,out ulong value){
   Result.ResumeInstructionRva=rva;Result.FieldReads++;ulong? observed=state.ReadLittleEndian(offset+relative,8);
   value=observed.GetValueOrDefault();if(observed.HasValue)return true;Stop(reason,rva);return false;
  }
  bool LengthStore(uint rva){
   Result.ResumeInstructionRva=rva;state.Store(offset+0x10,8,count);Result.FieldStoreInstructions++;return true;
  }
  bool Terminator(ulong pointer,uint rva){
   Result.ResumeInstructionRva=rva;
   if(!memory.TryWriteByte(pointer,0)){Stop("UnmappedStringTerminator",rva);return false;}
   Result.FieldStoreInstructions++;return true;
  }
  ItemNameAssignResult Complete(uint rva){
   Result.Returned=true;Result.ReturnedTarget=target;Result.Status="ConditionalReturn";Result.Reason=null;Result.ResumeInstructionRva=rva;return Result;
  }
  ItemNameAssignResult Stop(string reason,uint rva){Result.Status="Unsupported";Result.Reason=reason;Result.ResumeInstructionRva=rva;return Result;}
 }
 public sealed class ItemConstructorNameResult {
  public readonly ItemConstructorIdentityResult Prior;
  public readonly ItemConstructorStorage State;
  public readonly ItemNameMemory Memory;
  public readonly List<ItemNameAssignResult> Assignments=new List<ItemNameAssignResult>();
  public ItemObjectNameGetterResult Getter {get;internal set;}
  public ItemNameScanResult Scan {get;internal set;}
  public int CompletedAssignments {get;internal set;}
  public int ScalarStoreInstructions {get;internal set;}
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public string Stage {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public uint? CallSiteRva {get;internal set;}
  public bool ReachedItem323A49 {get;internal set;}
  public bool IsCompleteItemConstructor {get{return false;}}
  internal ItemConstructorNameResult(ItemConstructorIdentityResult prior,ItemNameMemory memory){
   Prior=prior;State=prior==null?null:prior.State;Memory=memory;Status="Unsupported";Reason="NotRun";Stage="NotRun";ResumeInstructionRva=ItemConstructorNameInvocation.EntryRva;
  }
 }
 public sealed class ItemConstructorNameInvocation {
  public const uint EntryRva=0x323977,NextRva=0x323A49;
  public readonly ItemConstructorNameResult Result;
  public bool HasRun {get;private set;}
  readonly ItemConstructorStorage state;readonly ItemNameMemory memory;
  public ItemConstructorNameInvocation(ItemConstructorIdentityResult prior,ItemNameMemory memory){
   Result=new ItemConstructorNameResult(prior,memory);state=Result.State;this.memory=memory;
  }
  public ItemConstructorNameResult Run(bool normalMemcpy=false,int scanBudget=ItemNameScan.MaximumScanBytes){
   if(HasRun)throw new InvalidOperationException("This Item name continuation has already been attempted");HasRun=true;
   ItemConstructorIdentityResult prior=Result.Prior;
   if(prior==null||state==null)return Stop("MissingPriorItemIdentityPrefix",EntryRva,"Prior");
   if(!prior.ReachedItem323977||prior.Status!="ConditionalItemPrefix"||prior.ResumeInstructionRva!=EntryRva)
    return Stop("PriorItemIdentityPrefixIncomplete",prior.ResumeInstructionRva,"Prior");
   if(memory==null||!Object.ReferenceEquals(memory.State,state))return Stop("ItemNameMemoryIdentityMismatch",EntryRva,"Prior");
   Result.Stage="Getter";Result.CallSiteRva=EntryRva;Result.Getter=ItemObjectNameGetter.Read(state);
   if(!Result.Getter.Returned)return Stop(Result.Getter.Reason,Result.Getter.ResumeInstructionRva,"Getter");
   Result.CallSiteRva=null;Result.Stage="Scan";Result.Scan=ItemNameScan.Scan(memory,Result.Getter.ReturnedPointer.Value,scanBudget);
   if(!Result.Scan.Terminated)return Stop(Result.Scan.Reason,Result.Scan.ResumeInstructionRva,"Scan");
   if(!Assign(0x540,Result.Getter.ReturnedPointer.Value,Result.Scan.Length.Value,0x32399D,normalMemcpy))return Result;
   // The next six calls supply count0; their literal pointer values are kept,
   // but no literal bytes are invented/read for a no-copy path. Alias decisions
   // still use the live destination, so count0 is not an unconditional shortcut.
   if(!Assign(0x560,0x1806196DEUL,0,0x3239B3,normalMemcpy))return Result;
   if(!Assign(0x580,0x1806196DFUL,0,0x3239C9,normalMemcpy))return Result;
   if(!Scalar(0x5A0,4,0,0x3239CE))return Result;
   if(!Assign(0x5A8,0x180619703UL,0,0x3239E5,normalMemcpy))return Result;
   if(!Assign(0x5C8,0x18061975DUL,0,0x3239FB,normalMemcpy))return Result;
   if(!Assign(0x5E8,0x18061975EUL,0,0x323A11,normalMemcpy))return Result;
   if(!Scalar(0x608,4,0,0x323A16))return Result;
   if(!Assign(0x610,0x18061975FUL,0,0x323A2D,normalMemcpy))return Result;
   if(!Scalar(0x630,8,0,0x323A32))return Result;
   if(!Scalar(0x698,4,0,0x323A39))return Result;
   if(!Scalar(0x6B0,4,1,0x323A3F))return Result;
   Result.ReachedItem323A49=true;Result.Status="ConditionalItemPrefix";Result.Reason=null;
   Result.Stage="Item323A49";Result.ResumeInstructionRva=NextRva;Result.CallSiteRva=null;return Result;
  }
  bool Assign(int offset,ulong pointer,ulong count,uint call,bool normalMemcpy){
   Result.Stage="Assignment";Result.CallSiteRva=call;Result.ResumeInstructionRva=ItemNameAssignInvocation.EntryRva;
   var invocation=new ItemNameAssignInvocation(memory,offset,pointer,count);Result.Assignments.Add(invocation.Result);
   ItemNameAssignResult result=invocation.Run(normalMemcpy);
   if(!result.Returned){Stop(result.Reason,result.ResumeInstructionRva,"Assignment");return false;}
   Result.CompletedAssignments++;Result.CallSiteRva=null;return true;
  }
  bool Scalar(int offset,int width,ulong value,uint rva){
   Result.Stage="CallerSuffix";Result.CallSiteRva=null;Result.ResumeInstructionRva=rva;
   if(!state.HasRange(offset,width)){Stop("MissingItemNameScalarStorage",rva,"CallerSuffix");return false;}
   state.Store(offset,width,value);Result.ScalarStoreInstructions++;return true;
  }
  ItemConstructorNameResult Stop(string reason,uint rva,string stage){Result.Status="Unsupported";Result.Reason=reason;Result.ResumeInstructionRva=rva;Result.Stage=stage;return Result;}
 }
}
