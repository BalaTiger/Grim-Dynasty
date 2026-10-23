using System;
using System.Collections.Generic;

namespace GrimDynasty.CombatRuntime {
 // Normal own-platform allocation adapter, not original malloc/new-handler/EH.
 // Each request really creates a new managed object with unknown nullable fields.
 // 0x28 is the observed NATIVE request size, not the managed object's byte size.
 // No cached/prebuilt node, implicit sentinel, pool, or null-success path exists.
 public sealed class NativeStatisticsManagedAllocator {
  public long Requests {get;private set;}
  public long Allocations {get;private set;}
  internal NativeDamageStatisticsNode AllocateFresh(){
   Requests++;var node=new NativeDamageStatisticsNode();Allocations++;return node;
  }
 }
 public sealed class NativeStatisticsFirstInsertResult {
  public readonly NativeDamageLookupResult Lookup;
  public NativeDamageStatisticsNode AllocatedNode {get;internal set;}
  public NativeDamageLookupOutput LinkOutput {get;internal set;}
  public NativeDamageLookupOutput ReturnedLinkOutput {get;internal set;}
  // Logical identity in the reused stack slot L: borrowed key before 4DCA3,
  // inserted node after that store. LinkOutput.Node=null before it is NOT a
  // claim that the native slot was zero-filled.
  public object LocalSlotContents {get;internal set;}
  public OutOfMemoryException AllocationException {get;internal set;}
  public int AllocationCalls {get;internal set;}
  public int RequestedNativeBytes {get;internal set;}
  public int NodeStoreInstructions {get;internal set;}
  public int CountStoreInstructions {get;internal set;}
  public int HeadStoreInstructions {get;internal set;}
  public int LinkOutputStoreInstructions {get;internal set;}
  public bool NodeConstructionReturned {get;internal set;}
  public bool LinkReturned {get;internal set;}
  public bool RootBlackWritten {get;internal set;}
  public int StoreInstructions {get{return NodeStoreInstructions+CountStoreInstructions+HeadStoreInstructions+LinkOutputStoreInstructions+Lookup.OutputStoreInstructions;}}
  public NativeDamageStatisticsStatus Status {get{return Lookup.Status;}}
  public bool IsEstablishedNativeContainerInitialization {get{return false;}}
  internal NativeStatisticsFirstInsertResult(NativeDamageLookupResult lookup){Lookup=lookup;}
 }
 public sealed class NativeStatisticsFirstFinalResult {
  public readonly NativeDamageStatisticsResult Statistics;
  public NativeStatisticsFirstInsertResult Insertion {get;internal set;}
  public NativeDamageStatisticsStatus Status {get{return Statistics.Status;}}
  internal NativeStatisticsFirstFinalResult(NativeDamageStatisticsResult statistics){Statistics=statistics;}
 }
 public static class NativeStatisticsFirstInsert {
  const ulong NativeCountLimit=0x0666666666666665UL;
  static void Step(List<NativeDamageStatisticsStep> trace,uint at,string operation,NativeDamageStatisticsNode node){trace.Add(new NativeDamageStatisticsStep(at,operation,node));}
  static NativeStatisticsFirstInsertResult Stop(NativeStatisticsFirstInsertResult r,string reason,uint at){r.Lookup.Reason=reason;r.Lookup.ResumeInstructionRva=at;return r;}
  static NativeStatisticsFirstFinalResult Stop(NativeStatisticsFirstFinalResult r,string reason,uint at){r.Statistics.Reason=reason;r.Statistics.ResumeInstructionRva=at;return r;}
  static bool Finite(float value){return !Single.IsNaN(value)&&!Single.IsInfinity(value);}
  static float Binary32(double value){return BitConverter.ToSingle(BitConverter.GetBytes((float)value),0);}

  // Existing hits use the accepted lookup unchanged. Only its missing-helper
  // stop may continue, and only an explicitly observed empty sentinel shape.
  // Shape checks below are own admission conditions, not native bounds checks
  // or recovered sentinel initialization. Color/count are observed later at
  // their instruction reads so a failure retains the actual mutation prefix.
  public static NativeStatisticsFirstInsertResult LookupOrInsert(NativeDamageStatisticsTree tree,NativeDamageStatsKeyView key,NativeDamageLookupOutput output,NativeDamageStatisticsConditions conditions,NativeStatisticsManagedAllocator allocator){
   return Continue(NativeDamageStatistics.Lookup(tree,key,output,conditions),allocator);
  }
  static NativeStatisticsFirstInsertResult Continue(NativeDamageLookupResult lookup,NativeStatisticsManagedAllocator allocator){
   var r=new NativeStatisticsFirstInsertResult(lookup);
   if(lookup.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn||lookup.Reason!="MissingKeyHelperOutsideCoverage"||lookup.ResumeInstructionRva!=0x403B59)return r;
   r.LocalSlotContents=lookup.Key; // saved by the existing 403B4C prefix
   var tree=lookup.Tree;var key=lookup.Key;
   var head=tree==null?null:tree.Head;
   if(head==null||!Object.ReferenceEquals(head.Parent,head))return Stop(r,"NonemptyOrUnknownRootOutsideFirstInsertCoverage",0x403B59);
   if(!head.Sentinel.HasValue||head.Sentinel.Value!=1)return Stop(r,"DeclaredEmptySentinelUnobserved",0x403B59);
   if(!Object.ReferenceEquals(head.Left,head)||!Object.ReferenceEquals(head.Right,head))return Stop(r,"DeclaredEmptyHeadLinksUnobserved",0x403B59);
   Step(lookup.Trace,0x403B59,"OwnKnownEmptyShapeAdmitted",head);

   r.RequestedNativeBytes=0x28;
   Step(lookup.Trace,0x019C9E,"OwnFreshAllocationRequested",null);
   if(allocator==null)return Stop(r,"MissingOwnAllocationInterface",0x019C9E);
   NativeDamageStatisticsNode node;
   try{r.AllocationCalls++;node=allocator.AllocateFresh();}
   catch(OutOfMemoryException e){r.AllocationException=e;return Stop(r,"OwnAllocationFailedOutsideNativeFailureCoverage",0x019C9E);}
   r.AllocatedNode=node;
   // The native 5DA6C4 normal return is nonnull; its retry/throw path is NOT
   // modeled as 019C90's apparent null branch. Our fresh managed ref is valid.
   Step(lookup.Trace,0x019CAB,"ReadHeadForLeft",tree.Head);
   if(tree.Head==null)return Stop(r,"UnknownHeadForNodeLeft",0x019CAB);
   node.Left=tree.Head;r.NodeStoreInstructions++;Step(lookup.Trace,0x019CAE,"WriteNodeLeft",node);
   Step(lookup.Trace,0x019CB7,"ReadHeadForParent",tree.Head);
   if(tree.Head==null)return Stop(r,"UnknownHeadForNodeParent",0x019CB7);
   node.Parent=tree.Head;r.NodeStoreInstructions++;Step(lookup.Trace,0x019CBA,"WriteNodeParent",node);
   Step(lookup.Trace,0x019CC6,"ReadHeadForRight",tree.Head);
   if(tree.Head==null)return Stop(r,"UnknownHeadForNodeRight",0x019CC6);
   node.Right=tree.Head;r.NodeStoreInstructions++;Step(lookup.Trace,0x019CC9,"WriteNodeRight",node);
   // One native WORD store. Unrepresented bytes +1A/+1B remain unobserved.
   node.Color=0;node.Sentinel=0;r.NodeStoreInstructions++;Step(lookup.Trace,0x403BB2,"WriteNodeColorMarkerWordZero",node);
   Step(lookup.Trace,0x403BBD,"ReadBorrowedKeyReference",null);
   if(key==null||key.Stats==null)return Stop(r,"UnknownConstructionKeyReference",0x403BBD);
   Step(lookup.Trace,0x403BC0,"ReadBothKeyWords",null);
   if(!key.Stats.Type.HasValue)return Stop(r,"UnknownConstructionKeyFirst",0x403BC0);
   if(!key.SecondWord.HasValue)return Stop(r,"UnknownConstructionKeySecond",0x403BC0);
   // One native QWORD read and one QWORD write: never partially copy key1.
   int first=unchecked((int)key.Stats.Type.Value),second=key.SecondWord.Value;
   node.FirstWord=first;node.SecondWord=second;r.NodeStoreInstructions++;Step(lookup.Trace,0x403BC3,"WriteBothKeyWords",node);
   node.Amount=0.0f;r.NodeStoreInstructions++;Step(lookup.Trace,0x403BC6,"WriteNodeAmountPositiveZero",node);
   r.NodeConstructionReturned=true;

   Step(lookup.Trace,0x403BFD,"ReadCountForEmptyBranch",null);
   if(!tree.Count.HasValue)return Stop(r,"UnknownCountForEmptyBranch",0x403BFD);
   if(tree.Count.Value!=0)return Stop(r,"NonemptyCountInsertionOutsideCoverage",0x403C02);
   Step(lookup.Trace,0x403C11,"ReadLinkParentHead",tree.Head);
   var parent=tree.Head;
   if(parent==null)return Stop(r,"UnknownLinkParent",0x403C11);
   r.LinkOutput=new NativeDamageLookupOutput(); // own counterpart of the local out buffer
   Step(lookup.Trace,0x04DA16,"ReadCountForLink",null);
   if(!tree.Count.HasValue)return Stop(r,"UnknownCountForLink",0x04DA16);
   ulong count=tree.Count.Value;
   if(count>=NativeCountLimit)return Stop(r,"CountLimitFailureOutsideCoverage",0x04DA2D);
   if(count!=0)return Stop(r,"DeclaredEmptyCountChanged",0x04DA16);
   tree.Count=count+1;r.CountStoreInstructions++;Step(lookup.Trace,0x04DA4F,"WriteCountIncrement",null);
   node.Parent=parent;r.NodeStoreInstructions++;Step(lookup.Trace,0x04DA53,"WriteLinkedNodeParent",node);
   Step(lookup.Trace,0x04DA57,"ReadHeadForRootBranch",tree.Head);
   if(tree.Head==null)return Stop(r,"UnknownHeadForRootBranch",0x04DA57);
   if(!Object.ReferenceEquals(parent,tree.Head))return Stop(r,"NonrootLinkOutsideCoverage",0x04DA5D);
   tree.Head.Parent=node;r.HeadStoreInstructions++;Step(lookup.Trace,0x04DA5F,"WriteHeadRoot",tree.Head);
   Step(lookup.Trace,0x04DA63,"ReadHeadForFirst",tree.Head);
   if(tree.Head==null)return Stop(r,"UnknownHeadForFirst",0x04DA63);
   tree.Head.Left=node;r.HeadStoreInstructions++;Step(lookup.Trace,0x04DA66,"WriteHeadFirst",tree.Head);
   Step(lookup.Trace,0x04DA69,"ReadHeadForLast",tree.Head);
   if(tree.Head==null)return Stop(r,"UnknownHeadForLast",0x04DA69);
   tree.Head.Right=node;r.HeadStoreInstructions++;Step(lookup.Trace,0x04DA90,"WriteHeadLast",tree.Head);
   Step(lookup.Trace,0x04DA94,"ReadNewNodeParent",node);
   parent=node.Parent;
   if(parent==null)return Stop(r,"UnknownNewNodeParent",0x04DA94);
   Step(lookup.Trace,0x04DA9B,"ReadParentColor",parent);
   if(!parent.Color.HasValue)return Stop(r,"UnknownParentColor",0x04DA9B);
   if(parent.Color.Value==0)return Stop(r,"RedParentRebalanceOutsideCoverage",0x04DAB0);
   if(parent.Color.Value!=1)return Stop(r,"DeclaredBlackParentColorOutsideCoverage",0x04DA9B);
   Step(lookup.Trace,0x04DCA0,"ReadHeadForFinalRoot",tree.Head);
   if(tree.Head==null)return Stop(r,"UnknownHeadForFinalRoot",0x04DCA0);
   var finalHead=tree.Head;
   r.LinkOutput.Node=node;r.LocalSlotContents=node;r.LinkOutputStoreInstructions++;Step(lookup.Trace,0x04DCA3,"WriteLocalOutputNode",node);
   Step(lookup.Trace,0x04DCA6,"ReadFinalRoot",finalHead);
   var root=finalHead.Parent;
   if(root==null)return Stop(r,"UnknownFinalRoot",0x04DCA6);
   r.ReturnedLinkOutput=r.LinkOutput; // 04DCAA, RAX is the original local out address
   root.Color=1;r.NodeStoreInstructions++;r.RootBlackWritten=true;Step(lookup.Trace,0x04DCAD,"WriteRootBlack",root);
   r.LinkReturned=true;
   Step(lookup.Trace,0x403B77,"ReadReturnedLocalNode",r.LinkOutput.Node);
   if(r.LinkOutput.Node==null)return Stop(r,"UnknownReturnedLocalNode",0x403B77);
   if(lookup.Output==null)return Stop(r,"UnknownOutput",0x403B86);
   lookup.Output.Node=r.LinkOutput.Node;lookup.OutputStoreInstructions++;Step(lookup.Trace,0x403B86,"WriteOutputNode",lookup.Output.Node);
   lookup.ReturnedOutput=lookup.Output; // 403B89, preserve original outer out identity
   lookup.Output.Inserted=1;lookup.OutputStoreInstructions++;Step(lookup.Trace,0x403B8C,"WriteOutputFlagOne",lookup.Output.Node);
   lookup.Status=NativeDamageStatisticsStatus.ConditionalNormalReturn;lookup.Reason="FirstEmptyNodeInserted";lookup.ResumeInstructionRva=0x403B95;return r;
  }

  // Call the existing Final exactly once. An existing-key result is returned
  // unchanged. If its FIRST lookup stops at the covered helper, continue that
  // same result/output/trace, then resume at 3FE3BE. No preparatory lookup or
  // replay adds a third call. The short saved ADD/MAX tail is repeated here to
  // keep the accepted lookup/Final implementations untouched.
  public static NativeStatisticsFirstFinalResult RunFinal(NativeDamageStatisticsState state,NativeDamageStatsKeyView key,float? damage,NativeDamageStatisticsConditions conditions,NativeStatisticsManagedAllocator allocator){
   var original=NativeDamageStatistics.RunFinal(state,key,damage,conditions);
   var r=new NativeStatisticsFirstFinalResult(original);
   if(original.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn||original.FirstLookup==null||original.Reason!="FirstLookup:MissingKeyHelperOutsideCoverage")return r;
   int oldTraceCount=original.FirstLookup.Trace.Count;
   r.Insertion=Continue(original.FirstLookup,allocator);
   for(int i=oldTraceCount;i<original.FirstLookup.Trace.Count;i++)original.Trace.Add(original.FirstLookup.Trace[i]);
   if(r.Insertion.Status!=NativeDamageStatisticsStatus.ConditionalNormalReturn)return Stop(r,"FirstLookup:"+original.FirstLookup.Reason,0x3FE3B9);
   var firstOutput=original.FirstLookup.Output;
   Step(original.Trace,0x3FE3BE,"FirstLookupReturned",firstOutput.Node);
   var secondOutput=new NativeDamageLookupOutput();
   Step(original.Trace,0x3FE3CD,"SecondLookupStarted",null);original.LookupCalls++;
   original.SecondLookup=NativeDamageStatistics.Lookup(state.Tree,key,secondOutput,conditions);original.Trace.AddRange(original.SecondLookup.Trace);
   if(original.SecondLookup.Status!=NativeDamageStatisticsStatus.ConditionalNormalReturn)return Stop(r,"SecondLookup:"+original.SecondLookup.Reason,0x3FE3CD);
   Step(original.Trace,0x3FE3D2,"SecondLookupReturned",secondOutput.Node);
   Step(original.Trace,0x3FE3D2,"ReadFirstOutputNode",firstOutput.Node);
   if(firstOutput.Node==null)return Stop(r,"UnknownFirstOutputNode",0x3FE3D2);
   if(!damage.HasValue)return Stop(r,"UnknownDamage",0x3FE3D7);
   Step(original.Trace,0x3FE3DA,"ReadFirstAmount",firstOutput.Node);original.AmountRead=true;
   if(!firstOutput.Node.Amount.HasValue)return Stop(r,"UnknownFirstAmount",0x3FE3DA);
   float oldAmount=firstOutput.Node.Amount.Value,amount=damage.Value;
   if(conditions==null||!conditions.NormalFloatingPointEnvironment)return Stop(r,"FloatingPointEnvironmentUnobserved",0x3FE3DA);
   if(!Finite(amount)||!Finite(oldAmount))return Stop(r,"NonfiniteAdditionOutsideCoverage",0x3FE3DA);
   float sum=Binary32((double)amount+(double)oldAmount);
   if(!Finite(sum))return Stop(r,"AmountResultOutsideCoverage",0x3FE3DA);
   Step(original.Trace,0x3FE3DF,"ReadSecondOutputNode",secondOutput.Node);
   if(secondOutput.Node==null)return Stop(r,"UnknownSecondOutputNode",0x3FE3DF);
   secondOutput.Node.Amount=sum;original.AmountWritten=true;Step(original.Trace,0x3FE3E4,"WriteSecondAmount",secondOutput.Node);
   Step(original.Trace,0x3FE3E9,"ReadKeyFirst",null);
   if(key==null||key.Stats==null||!key.Stats.Type.HasValue)return Stop(r,"UnknownFinalType",0x3FE3E9);
   if(unchecked((int)key.Stats.Type.Value)==1){
    Step(original.Trace,0x3FE3EE,"ReadMaximum",null);
    if(!state.Maximum.HasValue)return Stop(r,"UnknownMaximum",0x3FE3EE);
    float maximum=state.Maximum.Value;
    if(!Finite(maximum))return Stop(r,"NonfiniteMaximumOutsideCoverage",0x3FE3EE);
    if(amount>maximum){state.Maximum=amount;original.MaximumWritten=true;Step(original.Trace,0x3FE3F4,"WriteMaximum",null);}
   }
   original.Status=NativeDamageStatisticsStatus.ConditionalNormalReturn;original.Reason="FirstInsertAndExistingBranchReturned";original.ResumeInstructionRva=0x3FE40D;return r;
  }
 }
 public sealed class NativeStatisticsFirstInsertBinding {
  readonly NativeDamageState owner;
  readonly NativeDamageStatisticsState state;
  readonly NativeDamageStatsKeyView key;
  readonly NativeDamageStatisticsConditions conditions;
  readonly NativeStatisticsManagedAllocator allocator;
  public NativeStatisticsFirstFinalResult LastResult {get;private set;}
  public NativeStatisticsFirstInsertBinding(NativeDamageState owner,NativeDamageStatisticsState state,NativeDamageStatsKeyView key,NativeDamageStatisticsConditions conditions,NativeStatisticsManagedAllocator allocator){this.owner=owner;this.state=state;this.key=key;this.conditions=conditions;this.allocator=allocator;}
  public NativeDamageCallObservation Invoke(NativeDamageState actualOwner,float damage,NativeDamageStats actualStats){
   if(owner==null||!Object.ReferenceEquals(owner,actualOwner)||key==null||key.Stats==null||!Object.ReferenceEquals(key.Stats,actualStats)){
    var failure=new NativeDamageStatisticsResult(state,key){Reason="BindingIdentityMismatch"};LastResult=new NativeStatisticsFirstFinalResult(failure);return new NativeDamageCallObservation(false);
   }
   LastResult=NativeStatisticsFirstInsert.RunFinal(state,key,damage,conditions,allocator);
   return new NativeDamageCallObservation(LastResult.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn);
  }
 }
}
