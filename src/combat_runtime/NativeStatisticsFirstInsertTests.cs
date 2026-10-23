using System;
using System.Collections.Generic;
using GrimDynasty.CombatRuntime;

// Own managed allocations and the first-empty-node continuation only. No
// original allocator, CRT new handler, native container initializer or scene.
public static class NativeStatisticsFirstInsertTests {
 static int assertions,groups;
 static void Need(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Group(string name,Action test){int before=assertions;test();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static uint Bits(float value){return unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value),0));}
 static bool Same(object a,object b){return Object.ReferenceEquals(a,b);}
 sealed class Fixture {
  public readonly NativeDamageStatisticsNode Head;
  public readonly NativeDamageStatisticsTree Tree;
  public readonly NativeDamageStats Stats;
  public readonly NativeDamageStatsKeyView Key;
  public readonly NativeDamageStatisticsState State;
  public readonly NativeDamageStatisticsConditions Conditions=new NativeDamageStatisticsConditions(true,true,16);
  public readonly NativeStatisticsManagedAllocator Allocator;
  public Fixture(NativeStatisticsManagedAllocator allocator=null){
   // Explicit self-owned empty sentinel fixture; not recovered native startup.
   Head=new NativeDamageStatisticsNode {Sentinel=1,Color=1};Head.Left=Head;Head.Parent=Head;Head.Right=Head;
   Tree=new NativeDamageStatisticsTree {Head=Head,Count=0};Stats=new NativeDamageStats {Type=1};Key=new NativeDamageStatsKeyView(Stats){SecondWord=7};
   State=new NativeDamageStatisticsState(Tree){Maximum=0};Allocator=allocator??new NativeStatisticsManagedAllocator();
  }
  public NativeDamageLookupOutput Output(){return new NativeDamageLookupOutput {Node=Head,Inserted=9};}
  public NativeStatisticsFirstInsertResult Lookup(NativeDamageLookupOutput output){return NativeStatisticsFirstInsert.LookupOrInsert(Tree,Key,output,Conditions,Allocator);}
  public NativeStatisticsFirstFinalResult Final(float? amount){return NativeStatisticsFirstInsert.RunFinal(State,Key,amount,Conditions,Allocator);}
 }
 static void Stop(NativeStatisticsFirstInsertResult r,string reason,uint at,int stores){
  Need(r.Status==NativeDamageStatisticsStatus.Unsupported&&r.Lookup.Reason==reason&&r.Lookup.ResumeInstructionRva==at,"precise insert coverage stop "+reason);
  Need(r.StoreInstructions==stores,"exact preceding semantic stores for "+reason);
 }
 static int TraceCount(List<NativeDamageStatisticsStep> trace,string operation){int n=0;foreach(var step in trace)if(step.Operation==operation)n++;return n;}
 static void FreshNodeAndWrites(){
  Need(!new NativeDamageStatisticsNode().Color.HasValue&&!new NativeDamageStatisticsTree().Count.HasValue,"added fields stay nullable/unknown by default");
  var f=new Fixture();var output=f.Output();var r=f.Lookup(output);var n=r.AllocatedNode;
  Need(r.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&n!=null&&!Same(n,f.Head)&&f.Allocator.Requests==1&&f.Allocator.Allocations==1&&r.RequestedNativeBytes==40,"first insert allocates a genuinely fresh own node for the observed logical40-byte request");
  Need(Same(n.Left,f.Head)&&Same(n.Parent,f.Head)&&Same(n.Right,f.Head)&&n.Color==1&&n.Sentinel==0&&n.FirstWord==1&&n.SecondWord==7&&Bits(n.Amount.Value)==0,"three initial links, key pair, positive-zero amount and final root color are exact");
  Need(f.Tree.Count==1&&Same(f.Head.Parent,n)&&Same(f.Head.Left,n)&&Same(f.Head.Right,n),"root/first/last all reference the same fresh node");
  Need(Same(r.Lookup.Output,output)&&Same(r.Lookup.ReturnedOutput,output)&&Same(output.Node,n)&&output.Inserted==1&&Same(r.ReturnedLinkOutput,r.LinkOutput)&&Same(r.LinkOutput.Node,n)&&!r.LinkOutput.Inserted.HasValue&&Same(r.LocalSlotContents,n),"inner/outer out-address identity and reused local-slot identity are distinct and preserved");
  Need(r.NodeStoreInstructions==8&&r.CountStoreInstructions==1&&r.HeadStoreInstructions==3&&r.LinkOutputStoreInstructions==1&&r.Lookup.OutputStoreInstructions==2&&r.StoreInstructions==15&&!r.IsEstablishedNativeContainerInitialization,"store counts preserve native widths without claiming native initialization");
  uint[] expected={0x019CAE,0x019CBA,0x019CC9,0x403BB2,0x403BC3,0x403BC6,0x04DA4F,0x04DA53,0x04DA5F,0x04DA66,0x04DA90,0x04DCA3,0x04DCAD,0x403B86,0x403B8C};
  var writes=new List<uint>();foreach(var step in r.Lookup.Trace)if(step.Operation.StartsWith("Write",StringComparison.Ordinal))writes.Add(step.Rva);
  bool exact=writes.Count==expected.Length;for(int i=0;exact&&i<expected.Length;i++)exact=writes[i]==expected[i];
  Need(exact,"independent native write-order oracle includes repeated parent and color writes");
  var other=new Fixture(f.Allocator);var otherResult=other.Lookup(other.Output());
  Need(!Same(n,otherResult.AllocatedNode)&&f.Allocator.Allocations==2,"successive allocations never reuse a supplied fixture node");
  var signed=new Fixture();signed.Stats.Type=UInt32.MaxValue;signed.Key.SecondWord=Int32.MinValue;var copied=signed.Lookup(signed.Output()).AllocatedNode;
  Need(copied.FirstWord==-1&&copied.SecondWord==Int32.MinValue,"single key copy preserves both raw DWORD bit patterns and signed query interpretation");
 }
 static void ConstructionFailurePrefixes(){
  var f=new Fixture();var output=f.Output();f.Key.SecondWord=null;var r=f.Lookup(output);Stop(r,"UnknownConstructionKeySecond",0x403BC0,4);
  var n=r.AllocatedNode;
  Need(n!=null&&n.Color==0&&n.Sentinel==0&&!n.FirstWord.HasValue&&!n.SecondWord.HasValue&&!n.Amount.HasValue&&Same(n.Left,f.Head)&&Same(n.Parent,f.Head)&&Same(n.Right,f.Head),"unknown key2 keeps three pointers and WORD0; key1 and amount are not half-written");
  Need(f.Tree.Count==0&&Same(f.Head.Parent,f.Head)&&Same(output.Node,f.Head)&&output.Inserted==9&&Same(r.LocalSlotContents,f.Key),"unlinked construction failure preserves tree/output and saved key slot");
  f=new Fixture();f.Tree.Count=null;output=f.Output();r=f.Lookup(output);Stop(r,"UnknownCountForEmptyBranch",0x403BFD,6);
  Need(r.NodeConstructionReturned&&r.AllocatedNode.FirstWord==1&&r.AllocatedNode.SecondWord==7&&Bits(r.AllocatedNode.Amount.Value)==0&&Same(r.LocalSlotContents,f.Key)&&!r.LinkReturned,"count is not prevalidated before the complete fresh-node construction");
  f=new Fixture();output=f.Output();r=NativeStatisticsFirstInsert.LookupOrInsert(f.Tree,f.Key,output,f.Conditions,null);Stop(r,"MissingOwnAllocationInterface",0x019C9E,0);
  Need(r.AllocatedNode==null&&r.AllocationCalls==0&&f.Tree.Count==0&&Same(output.Node,f.Head),"missing allocation interface is not a successful native null allocation");
  f=new Fixture();f.Head.Left=null;r=f.Lookup(f.Output());Stop(r,"DeclaredEmptyHeadLinksUnobserved",0x403B59,0);
  Need(f.Allocator.Requests==0,"unobserved own empty-container shape is an admission stop, not fabricated initialization");
 }
 static void LinkingFailurePrefixes(){
  var f=new Fixture();f.Head.Color=null;var output=f.Output();var r=f.Lookup(output);Stop(r,"UnknownParentColor",0x04DA9B,11);var n=r.AllocatedNode;
  Need(f.Tree.Count==1&&Same(f.Head.Parent,n)&&Same(f.Head.Left,n)&&Same(f.Head.Right,n)&&n.Color==0&&n.Sentinel==0,"color read occurs after count and every first-node link has been published");
  Need(r.LinkOutput!=null&&r.LinkOutput.Node==null&&Same(r.LocalSlotContents,f.Key)&&r.LinkOutputStoreInstructions==0&&!r.RootBlackWritten&&Same(output.Node,f.Head)&&output.Inserted==9,"unknown color preserves old logical key slot and untouched caller output; no rollback/rootblack");
  f=new Fixture();f.Head.Color=0;r=f.Lookup(f.Output());Stop(r,"RedParentRebalanceOutsideCoverage",0x04DAB0,11);
  Need(r.AllocatedNode.Color==0&&!r.LinkReturned&&Same(f.Head.Parent,r.AllocatedNode),"red-parent repair is not invented or silently counted as successful");
  f=new Fixture();r=f.Lookup(null);Stop(r,"UnknownOutput",0x403B86,13);
  Need(r.RootBlackWritten&&r.LinkReturned&&Same(r.LocalSlotContents,r.AllocatedNode)&&Same(r.ReturnedLinkOutput,r.LinkOutput)&&f.Tree.Count==1&&r.Lookup.OutputStoreInstructions==0,"missing outer output retains completed link, local output and black root prefix");
 }
 static void ExistingReuseAndNewKeyStop(){
  var f=new Fixture();var inserted=f.Lookup(f.Output());var node=inserted.AllocatedNode;var output=f.Output();
  var existing=NativeStatisticsFirstInsert.LookupOrInsert(f.Tree,f.Key,output,f.Conditions,null);
  Need(existing.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&existing.AllocatedNode==null&&output.Inserted==0&&Same(output.Node,node)&&Same(existing.Lookup.ReturnedOutput,output)&&f.Allocator.Requests==1,"same key reuses accepted lookup and requires no new allocator");
  f.Key.SecondWord=8;output=f.Output();var missing=f.Lookup(output);Stop(missing,"NonemptyOrUnknownRootOutsideFirstInsertCoverage",0x403B59,0);
  Need(f.Allocator.Requests==1&&f.Tree.Count==1&&Same(f.Head.Parent,node)&&node.SecondWord==7&&Same(output.Node,f.Head)&&output.Inserted==9,"nonempty new key stays unsupported without generic-map insertion or effects");
 }
 static void FinalResumeExactlyTwoLookups(){
  var f=new Fixture();var r=f.Final(3);var s=r.Statistics;var node=f.Head.Parent;
  Need(r.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&r.Insertion!=null&&Same(r.Insertion.Lookup,s.FirstLookup)&&s.LookupCalls==2&&f.Allocator.Allocations==1,"resume the actual first lookup rather than adding a preparatory/replayed lookup");
  Need(s.FirstLookup.Output.Inserted==1&&s.SecondLookup.Output.Inserted==0&&Same(s.FirstLookup.Output,s.FirstLookup.ReturnedOutput)&&Same(s.SecondLookup.Output,s.SecondLookup.ReturnedOutput)&&!Same(s.FirstLookup.Output,s.SecondLookup.Output)&&Same(s.FirstLookup.Output.Node,s.SecondLookup.Output.Node),"original first output identity survives continuation; second output is distinct and hits same inserted node");
  Need(TraceCount(s.Trace,"FirstLookupStarted")==1&&TraceCount(s.Trace,"FirstLookupReturned")==1&&TraceCount(s.Trace,"SecondLookupStarted")==1&&TraceCount(s.Trace,"SecondLookupReturned")==1,"combined trace retains exactly two starts/returns without reset or duplicate old prefix");
  Need(node.Amount==3&&f.State.Maximum==3&&s.AmountWritten&&s.MaximumWritten&&f.Tree.Count==1,"same final call updates newly inserted zero amount and maximum");
  var repeat=f.Final(2);
  Need(repeat.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&repeat.Insertion==null&&repeat.Statistics.LookupCalls==2&&f.Allocator.Allocations==1&&node.Amount==5&&f.State.Maximum==3,"subsequent call directly reuses unchanged existing Final path");
  f=new Fixture();f.State.Maximum=null;r=f.Final(2);
  Need(r.Status==NativeDamageStatisticsStatus.Unsupported&&r.Statistics.Reason=="UnknownMaximum"&&r.Statistics.ResumeInstructionRva==0x3FE3EE&&r.Statistics.AmountWritten&&!r.Statistics.MaximumWritten&&f.Head.Parent.Amount==2&&f.Tree.Count==1&&f.Head.Parent.Color==1,"unknown maximum preserves inserted node and preceding amount write");
 }
 static NativeDamageState Owner(){return new NativeDamageState {Life=10,Accumulated=0,Dirty=0};}
 static void SettlementBinding(){
  var f=new Fixture();var owner=Owner();var binding=new NativeStatisticsFirstInsertBinding(owner,f.State,f.Key,f.Conditions,f.Allocator);
  var hooks=new NativeDamageHooks {Final=binding.Invoke};
  var result=NativeDamageSettlement.Run(owner,f.Stats,new NativeDamageInputs(3,1,0,true),hooks);
  Need(result.Status==NativeDamageStatus.ConditionalNormalReturn&&owner.Life==7&&owner.Accumulated==3&&owner.Dirty==1&&f.Head.Parent.Amount==3&&f.State.Maximum==3&&binding.LastResult.Statistics.LookupCalls==2,"actual settlement Final hook creates first record and uses the same owner/stats/field identities");
  var wrong=binding.Invoke(Owner(),1,f.Stats);
  Need(!wrong.ReturnedNormally&&binding.LastResult.Statistics.Reason=="BindingIdentityMismatch"&&f.Head.Parent.Amount==3&&f.Allocator.Allocations==1,"wrong owner cannot borrow a successful final observation");
  f=new Fixture();f.Key.SecondWord=null;owner=Owner();binding=new NativeStatisticsFirstInsertBinding(owner,f.State,f.Key,f.Conditions,f.Allocator);hooks.Final=binding.Invoke;
  result=NativeDamageSettlement.Run(owner,f.Stats,new NativeDamageInputs(3,1,0,true),hooks);
  Need(result.Status==NativeDamageStatus.Unsupported&&result.Reason=="FinalReturnUnobserved"&&result.ResumeInstructionRva==0x068B86&&result.StoreInstructions==3&&owner.Life==7&&owner.Accumulated==3&&owner.Dirty==1,"unknown statistics key2 retains outer settlement prefix instead of returning success");
  Need(binding.LastResult.Insertion.Lookup.Reason=="UnknownConstructionKeySecond"&&binding.LastResult.Insertion.StoreInstructions==4&&binding.LastResult.Statistics.LookupCalls==1&&f.Tree.Count==0&&Same(f.Head.Parent,f.Head),"nested getter/insert diagnosis and unlinked allocation prefix remain inspectable");
 }
 public static int Main(){
  try{
   Group("fresh allocation and exact write order",FreshNodeAndWrites);
   Group("construction failure prefixes",ConstructionFailurePrefixes);
   Group("linking failure prefixes",LinkingFailurePrefixes);
   Group("existing reuse and nonempty boundary",ExistingReuseAndNewKeyStop);
   Group("Final resumes exactly two lookups",FinalResumeExactlyTwoLookups);
   Group("real Settlement Final binding",SettlementBinding);
   Console.WriteLine("PASS NativeStatisticsFirstInsert: "+assertions+" assertions / "+groups+" groups");return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL NativeStatisticsFirstInsert: "+e);return 1;}
 }
}
