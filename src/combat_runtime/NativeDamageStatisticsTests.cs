using System;
using GrimDynasty.CombatRuntime;

// New own statistics paths only. Compile/run separately under root approval.
public static class NativeDamageStatisticsTests {
 static int assertions,groups;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Group(string name,Action action){int before=assertions;action();groups++;Console.WriteLine("PASS "+name+": "+(assertions-before)+" assertions");}
 static NativeDamageStatisticsConditions C(int visits=64,bool fp=true,bool serial=true){return new NativeDamageStatisticsConditions(serial,fp,visits);}
 static uint Bits(float value){return unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value),0));}
 static float F(uint value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
 sealed class Fixture {
  public readonly NativeDamageStats Stats=new NativeDamageStats();
  public readonly NativeDamageStatsKeyView Key;
  public readonly NativeDamageStatisticsNode Head=new NativeDamageStatisticsNode{Sentinel=255};
  public readonly NativeDamageStatisticsTree Tree=new NativeDamageStatisticsTree();
  public readonly NativeDamageStatisticsState State;
  public NativeDamageStatisticsNode Node;
  public Fixture(int first=1,int second=-7,float amount=5){
   Stats.Type=unchecked((uint)first);Key=new NativeDamageStatsKeyView(Stats){SecondWord=second};
   Tree.Head=Head;Node=Make(first,second,amount);Head.Parent=Node;
   State=new NativeDamageStatisticsState(Tree){Maximum=1};
  }
  public NativeDamageStatisticsNode Make(int first,int second,float amount){return new NativeDamageStatisticsNode{Sentinel=0,FirstWord=first,SecondWord=second,Amount=amount,Left=Head,Right=Head,Parent=Head};}
  public NativeDamageLookupResult Lookup(NativeDamageLookupOutput output=null,NativeDamageStatisticsConditions conditions=null){return NativeDamageStatistics.Lookup(Tree,Key,output??new NativeDamageLookupOutput(),conditions??C());}
  public NativeDamageStatisticsResult Final(float? damage=2,NativeDamageStatisticsConditions conditions=null){return NativeDamageStatistics.RunFinal(State,Key,damage,conditions??C());}
 }
 static void Stop(NativeDamageLookupResult result,string reason,uint rva){Need(result.Status==NativeDamageStatisticsStatus.Unsupported&&result.Reason==reason&&result.ResumeInstructionRva==rva,"lookup coverage: "+reason);}
 static void Stop(NativeDamageStatisticsResult result,string reason,uint rva,int stores){Need(result.Status==NativeDamageStatisticsStatus.Unsupported&&result.Reason==reason&&result.ResumeInstructionRva==rva,"final coverage: "+reason);Need(result.StoreInstructions==stores,"final preserved store count");}
 static int Find(NativeDamageStatisticsResult result,string operation){for(int i=0;i<result.Trace.Count;i++)if(result.Trace[i].Operation==operation)return i;return -1;}
 static int Count(NativeDamageLookupResult result,string operation){int n=0;foreach(var step in result.Trace)if(step.Operation==operation)n++;return n;}

 static void DefaultsAndOutputIdentity(){
  var stats=new NativeDamageStats();var key=new NativeDamageStatsKeyView(stats);var node=new NativeDamageStatisticsNode();
  Need(Object.ReferenceEquals(key.Stats,stats)&&!key.SecondWord.HasValue&&!stats.Type.HasValue,"borrowed stats identity and unknown second word");
  Need(node.Left==null&&node.Right==null&&node.Parent==null&&!node.Sentinel.HasValue&&!node.FirstWord.HasValue&&!node.SecondWord.HasValue&&!node.Amount.HasValue,"no invented node initialization");
  var f=new Fixture();var output=new NativeDamageLookupOutput{Inserted=99};var r=f.Lookup(output);
  Need(r.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&r.Reason=="ExistingBranchReturned","existing branch normal return");
  Need(Object.ReferenceEquals(r.ReturnedOutput,output)&&Object.ReferenceEquals(output.Node,f.Node)&&output.Inserted==0,"returned output identity, candidate identity and byte-zero flag");
  Need(r.OutputStoreInstructions==2&&r.Trace[r.Trace.Count-2].Rva==0x403B32&&r.Trace[r.Trace.Count-1].Rva==0x403B38,"pointer then flag store order");
  Need(Count(r,"ReadKeyFirst")==2&&Count(r,"ReadKeySecond")==2,"cached traversal key then fresh final comparisons");
  Need(Object.ReferenceEquals(r.Tree,f.Tree)&&Object.ReferenceEquals(r.Key,f.Key)&&Object.ReferenceEquals(f.Key.Stats,f.Stats),"live input identities preserved");
 }
 static void SignedPairBranches(){
  var f=new Fixture(-1,-1);var negative=f.Node;var root=f.Make(0,0,8);root.Left=negative;f.Head.Parent=root;
  var r=f.Lookup();Need(Object.ReferenceEquals(r.Output.Node,negative)&&r.NodeVisits==2,"uint FFFFFFFF is signed -1, so traverses left of zero");
  f=new Fixture(Int32.MinValue,Int32.MaxValue);var lower=f.Node;root=f.Make(Int32.MaxValue,Int32.MinValue,9);root.Left=lower;f.Head.Parent=root;
  r=f.Lookup();Need(Object.ReferenceEquals(r.Output.Node,lower),"signed first extrema order");
  f=new Fixture(1,Int32.MinValue);lower=f.Node;root=f.Make(1,Int32.MaxValue,9);root.Left=lower;f.Head.Parent=root;
  r=f.Lookup();Need(Object.ReferenceEquals(r.Output.Node,lower)&&Count(r,"ReadKeySecond")==3,"signed second extrema and repeated equal-first reads");
  f.Key.SecondWord=Int32.MaxValue;r=f.Lookup();Need(Object.ReferenceEquals(r.Output.Node,root),"same first different second selects separate record");
  Need(f.Stats.Type==1&&f.Key.SecondWord==Int32.MaxValue,"lookup does not rewrite either borrowed key dword");
 }
 static void SparseMissingAndNoInsertion(){
  var f=new Fixture();var marker=f.Make(9,9,99);var output=new NativeDamageLookupOutput{Node=marker,Inserted=77};
  f.Head.Parent=f.Head;f.Stats.Type=null;f.Key.SecondWord=null;var r=f.Lookup(output);
  Stop(r,"MissingKeyHelperOutsideCoverage",0x403B59);
  Need(Count(r,"ReadKeyFirst")==0&&Count(r,"ReadKeySecond")==0&&r.OutputStoreInstructions==0,"empty tree stops before key dereferences or helper");
  Need(Object.ReferenceEquals(output.Node,marker)&&output.Inserted==77&&r.ReturnedOutput==null,"failed lookup preserves caller output prefix");
  f=new Fixture(1,-7);f.Key.SecondWord=null;r=f.Lookup();Stop(r,"UnknownKeySecond",0x403AFB);Need(r.OutputStoreInstructions==0,"missing second key does not become zero");
  f=new Fixture(2,3);f.Stats.Type=1;f.Key.SecondWord=null;f.Node.SecondWord=null;r=f.Lookup();Stop(r,"MissingKeyHelperOutsideCoverage",0x403B59);
  Need(Count(r,"ReadKeySecond")==0&&Count(r,"ReadNodeSecond")==0,"unequal first words skip unobserved second fields");
  f=new Fixture();f.Node.SecondWord=null;Stop(f.Lookup(),"UnknownNodeSecond",0x403AFF);
  f=new Fixture();f.Node.Left=null;Stop(f.Lookup(),"UnknownSentinel",0x403B11);
  f=new Fixture();f.Head.Parent=null;Stop(f.Lookup(),"UnknownSentinel",0x403ADF);
  f=new Fixture();f.Tree.Head=null;Stop(f.Lookup(),"UnknownHead",0x403ACF);
  f=new Fixture();Stop(f.Lookup(null,C(64,true,false)),"SerialAccessUnobserved",0x403AC0);
 }
 static void LiteralDuplicatesAndVisitBounds(){
  var f=new Fixture();var first=f.Node;var duplicate=f.Make(1,-7,20);duplicate.Left=first;f.Head.Parent=duplicate;
  var r=f.Lookup();Need(r.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&Object.ReferenceEquals(r.Output.Node,first),"literal walk returns leftmost visited duplicate, no global dedup or fabricated rejection");
  Need(duplicate.Amount==20&&first.Amount==5,"lookup leaves all amounts unchanged");
  f=new Fixture();f.Node.Left=f.Node;r=f.Lookup();Stop(r,"CycleOutsideCoverage",0x403AF0);Need(r.OutputStoreInstructions==0,"cycle resource stop preserves output");
  f=new Fixture();var child=f.Node;var root=f.Make(2,0,10);root.Left=child;f.Head.Parent=root;
  Stop(f.Lookup(null,C(1)),"OwnVisitBudgetExceeded",0x403AF0);
  Stop(f.Lookup(null,C(0)),"InvalidOwnVisitBudget",0x403AC0);
  f=new Fixture();f.Head.Sentinel=2;r=f.Lookup();Need(r.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn,"any nonzero sentinel byte terminates traversal");
 }
 static void TwoLookupsAndLateRead(){
  var f=new Fixture(1,-7,7);f.State.Maximum=2;var r=f.Final(3);
  Need(r.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&r.LookupCalls==2,"two calls completed, not lookup memoization");
  Need(!Object.ReferenceEquals(r.FirstLookup.Output,r.SecondLookup.Output)&&Object.ReferenceEquals(r.FirstLookup.Output.Node,f.Node)&&Object.ReferenceEquals(r.SecondLookup.Output.Node,f.Node),"separate output slots and shared stable existing node");
  Need(Object.ReferenceEquals(r.FirstLookup.Key,f.Key)&&Object.ReferenceEquals(r.SecondLookup.Key,f.Key),"both calls borrow the same key view");
  Need(Find(r,"SecondLookupReturned")>Find(r,"SecondLookupStarted")&&Find(r,"ReadFirstAmount")>Find(r,"SecondLookupReturned")&&Find(r,"ReadFirstAmount")>Find(r,"ReadFirstOutputNode"),"first amount is read only after second lookup normal return");
  Need(f.Node.Amount==10&&f.State.Maximum==3&&r.AmountRead&&r.AmountWritten&&r.MaximumWritten,"amount then conditional maximum stores");
  Need(Find(r,"WriteSecondAmount")<Find(r,"ReadMaximum")&&Find(r,"WriteSecondAmount")<Find(r,"WriteMaximum"),"maximum work follows accumulated amount write");
  Need(r.StoreInstructions==2&&!r.IsEstablishedNativeInitialization,"own two-store conditional boundary");
  r=f.Final(2);Need(f.Node.Amount==12&&f.State.Maximum==3&&!r.MaximumWritten,"next invocation rereads live amount, smaller damage does not reduce peak");
  r=f.Final(3);Need(f.State.Maximum==3&&!r.MaximumWritten,"equal damage leaves maximum untouched");
 }
 static void FinalCoveragePrefixes(){
  var f=new Fixture();f.Key.SecondWord=null;var r=f.Final();Stop(r,"FirstLookup:UnknownKeySecond",0x3FE3B9,0);
  Need(r.LookupCalls==1&&!r.AmountRead&&r.SecondLookup==null&&f.Node.Amount==5,"failed first query cannot read amount or run second");
  f=new Fixture();f.Node.Amount=null;r=f.Final();Stop(r,"UnknownFirstAmount",0x3FE3DA,0);
  Need(r.LookupCalls==2&&r.FirstLookup.Output.Inserted==0&&r.SecondLookup.Output.Inserted==0,"missing amount observed after both output stores");
  f=new Fixture();r=f.Final(null);Stop(r,"UnknownDamage",0x3FE3D7,0);Need(r.LookupCalls==2&&!r.AmountRead,"unknown damage retains completed query prefix");
  f=new Fixture();r=f.Final(2,C(64,false));Stop(r,"FloatingPointEnvironmentUnobserved",0x3FE3DA,0);Need(r.LookupCalls==2&&f.Node.Amount==5,"FP condition not guessed");
  f=new Fixture();f.State.Maximum=null;r=f.Final(2);Stop(r,"UnknownMaximum",0x3FE3EE,1);Need(f.Node.Amount==7&&r.AmountWritten,"missing peak preserves prior amount store");
  f=new Fixture();f.State.Maximum=Single.NaN;r=f.Final();Stop(r,"NonfiniteMaximumOutsideCoverage",0x3FE3EE,1);
  f=new Fixture(2,-7);f.State.Maximum=null;r=f.Final();Need(r.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn&&f.Node.Amount==7&&Find(r,"ReadMaximum")==-1,"non-type-one never reads unknown peak");
  f=new Fixture();f.Node.Amount=Single.NaN;Stop(f.Final(),"NonfiniteAdditionOutsideCoverage",0x3FE3DA,0);
  f=new Fixture();f.Node.Amount=Single.MaxValue;Stop(f.Final(Single.MaxValue),"AmountResultOutsideCoverage",0x3FE3DA,0);
 }
 static void FloatWidthsAndLiveViews(){
  var f=new Fixture(2,-7,16777216);var r=f.Final(1);Need(Bits(f.Node.Amount.Value)==0x4B800000U,"binary32 ties round to even, not persistent double");
  f=new Fixture(2,-7,F(0x80000000));r=f.Final(F(0x80000000));Need(Bits(f.Node.Amount.Value)==0x80000000U,"two negative zero operands preserve negative zero in supported FP domain");
  f=new Fixture(2,-7,10);r=f.Final(-2);Need(f.Node.Amount==8,"final local body does not independently reject negative amount");
  var changed=f.Make(2,44,90);f.Key.SecondWord=44;f.Head.Parent=changed;r=f.Final(3);
  Need(Object.ReferenceEquals(r.FirstLookup.Output.Node,changed)&&Object.ReferenceEquals(r.SecondLookup.Output.Node,changed)&&changed.Amount==93&&f.Node.Amount==8,"next invocation uses live root and second key, no stale node cache");
 }
 static void SettlementBindingAndFailurePrefix(){
  var f=new Fixture();var owner=new NativeDamageState{Life=10,Accumulated=0,Dirty=0};
  var binding=new NativeDamageStatisticsBinding(owner,f.State,f.Key,C());var hooks=new NativeDamageHooks{Final=binding.Invoke};
  var r=NativeDamageSettlement.Run(owner,f.Stats,new NativeDamageInputs(2,1,0,true),hooks);
  Need(r.Status==NativeDamageStatus.ConditionalNormalReturn&&r.FinalCalls==1,"settlement binds real own final model");
  Need(owner.Life==8&&owner.Accumulated==2&&owner.Dirty==1&&f.Node.Amount==7&&f.State.Maximum==2,"settlement prefix then statistics stores on shared state");
  Need(binding.LastResult.LookupCalls==2&&Object.ReferenceEquals(binding.LastResult.Key.Stats,r.Stats),"bound final preserves the original borrowed stats identity");
  f=new Fixture();f.Key.SecondWord=null;owner=new NativeDamageState{Life=10,Accumulated=0,Dirty=0};binding=new NativeDamageStatisticsBinding(owner,f.State,f.Key,C());hooks=new NativeDamageHooks{Final=binding.Invoke};
  r=NativeDamageSettlement.Run(owner,f.Stats,new NativeDamageInputs(2,1,0,true),hooks);
  Need(r.Status==NativeDamageStatus.Unsupported&&r.Reason=="FinalReturnUnobserved"&&binding.LastResult.Reason=="FirstLookup:UnknownKeySecond","binding exposes unresolved second key, no default success");
  Need(owner.Life==8&&owner.Accumulated==2&&owner.Dirty==1&&f.Node.Amount==5&&r.StoreInstructions==3,"failed final keeps settlement write prefix without statistics stores");
  f=new Fixture();owner=new NativeDamageState{Life=10,Accumulated=0,Dirty=0};binding=new NativeDamageStatisticsBinding(owner,f.State,f.Key,C());
  Need(!binding.Invoke(owner,2,new NativeDamageStats{Type=1}).ReturnedNormally&&binding.LastResult.Reason=="BindingIdentityMismatch","equal-valued different stats identity is rejected");
  Need(!binding.Invoke(new NativeDamageState(),2,f.Stats).ReturnedNormally&&f.Node.Amount==5,"different owner cannot use the associated statistics model");
 }
 public static int Main(string[] args){
  try{
   if(args.Length!=0)throw new ArgumentException("No arguments; own fixture-only test executable");
   Group("defaults and output identity",DefaultsAndOutputIdentity);
   Group("signed pair branches",SignedPairBranches);
   Group("sparse missing and no insertion",SparseMissingAndNoInsertion);
   Group("literal duplicates and own visit bounds",LiteralDuplicatesAndVisitBounds);
   Group("two lookups and late read",TwoLookupsAndLateRead);
   Group("final coverage prefixes",FinalCoveragePrefixes);
   Group("float widths and live views",FloatWidthsAndLiveViews);
   Group("settlement binding and failure prefix",SettlementBindingAndFailurePrefix);
   Console.WriteLine("PASS NativeDamageStatistics: "+assertions+" assertions / "+groups+" groups");return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL NativeDamageStatistics: "+e);return 1;}
 }
}
