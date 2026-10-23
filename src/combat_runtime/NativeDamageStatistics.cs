using System;
using System.Collections.Generic;

namespace GrimDynasty.CombatRuntime {
 // Sparse, caller-supplied own views. Null means unobserved, not a native zero.
 // These do not allocate/initialize native containers or implement insertion.
 public sealed class NativeDamageStatsKeyView {
  public readonly NativeDamageStats Stats;
  public int? SecondWord; // borrowed stats +4; no default value
  public NativeDamageStatsKeyView(NativeDamageStats stats){Stats=stats;}
 }
 public sealed class NativeDamageStatisticsNode {
  public NativeDamageStatisticsNode Left;   // +0
  public NativeDamageStatisticsNode Parent; // +8 (head's root link)
  public NativeDamageStatisticsNode Right;  // +10
  public byte? Color;                       // +18; unobserved unless supplied/written
  public byte? Sentinel;                    // +19; any nonzero is terminal
  public int? FirstWord;                    // +1C; signed comparison
  public int? SecondWord;                   // +20; signed comparison
  public float? Amount;                     // +24
 }
 public sealed class NativeDamageStatisticsTree {
  public NativeDamageStatisticsNode Head;   // qword at container +0
  public ulong? Count;                      // qword at container +8; no implicit zero
 }
 public sealed class NativeDamageLookupOutput {
  public NativeDamageStatisticsNode Node;   // qword at output +0
  public byte? Inserted;                    // byte at output +8
  // Unrepresented output padding is never initialized by this model.
 }
 public sealed class NativeDamageStatisticsState {
  public readonly NativeDamageStatisticsTree Tree; // embedded subobject +1B8
  public float? Maximum;                           // +74
  public NativeDamageStatisticsState(NativeDamageStatisticsTree tree){Tree=tree;}
 }
 public sealed class NativeDamageStatisticsConditions {
  public readonly bool SerialExclusiveAccess,NormalFloatingPointEnvironment;
  public readonly int MaxNodeVisits;
  // FP: masked SSE, nearest/even, gradual underflow, matching Settlement's own
  // finite domain. No MXCSR read/change. No order/uniqueness promise is inferred.
  public NativeDamageStatisticsConditions(bool serialExclusiveAccess,bool normalFloatingPointEnvironment,int maxNodeVisits){
   SerialExclusiveAccess=serialExclusiveAccess;NormalFloatingPointEnvironment=normalFloatingPointEnvironment;MaxNodeVisits=maxNodeVisits;
  }
 }
 public enum NativeDamageStatisticsStatus { Unsupported,ConditionalNormalReturn }
 public sealed class NativeDamageStatisticsStep {
  public readonly uint Rva;
  public readonly string Operation;
  public readonly NativeDamageStatisticsNode Node;
  internal NativeDamageStatisticsStep(uint rva,string operation,NativeDamageStatisticsNode node){Rva=rva;Operation=operation;Node=node;}
 }
 public sealed class NativeDamageLookupResult {
  public readonly NativeDamageStatisticsTree Tree;
  public readonly NativeDamageStatsKeyView Key;
  public readonly NativeDamageLookupOutput Output;
  public NativeDamageLookupOutput ReturnedOutput {get;internal set;}
  public NativeDamageStatisticsStatus Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public int NodeVisits {get;internal set;}
  public int OutputStoreInstructions {get;internal set;}
  public readonly List<NativeDamageStatisticsStep> Trace=new List<NativeDamageStatisticsStep>();
  internal NativeDamageLookupResult(NativeDamageStatisticsTree tree,NativeDamageStatsKeyView key,NativeDamageLookupOutput output){Tree=tree;Key=key;Output=output;Reason="NotRun";ResumeInstructionRva=0x403AC0;}
 }
 public sealed class NativeDamageStatisticsResult {
  public readonly NativeDamageStatisticsState State;
  public readonly NativeDamageStatsKeyView Key;
  public NativeDamageStatisticsStatus Status {get;internal set;}
  public string Reason {get;internal set;}
  public uint ResumeInstructionRva {get;internal set;}
  public NativeDamageLookupResult FirstLookup {get;internal set;}
  public NativeDamageLookupResult SecondLookup {get;internal set;}
  public bool AmountRead {get;internal set;}
  public bool AmountWritten {get;internal set;}
  public bool MaximumWritten {get;internal set;}
  public int LookupCalls {get;internal set;}
  public int StoreInstructions {get{return (AmountWritten?1:0)+(MaximumWritten?1:0);}}
  public bool IsEstablishedNativeInitialization {get{return false;}}
  public readonly List<NativeDamageStatisticsStep> Trace=new List<NativeDamageStatisticsStep>();
  internal NativeDamageStatisticsResult(NativeDamageStatisticsState state,NativeDamageStatsKeyView key){State=state;Key=key;Reason="NotRun";ResumeInstructionRva=0x3FE390;}
 }
 public static class NativeDamageStatistics {
  public const uint LookupEntryRva=0x403AC0,FinalEntryRva=0x3FE390,MissingHelperRva=0x403BA0;
  static NativeDamageLookupResult Stop(NativeDamageLookupResult r,string reason,uint at){r.Reason=reason;r.ResumeInstructionRva=at;return r;}
  static NativeDamageStatisticsResult Stop(NativeDamageStatisticsResult r,string reason,uint at){r.Reason=reason;r.ResumeInstructionRva=at;return r;}
  static void Step(List<NativeDamageStatisticsStep> trace,uint at,string operation,NativeDamageStatisticsNode node){trace.Add(new NativeDamageStatisticsStep(at,operation,node));}
  static bool First(NativeDamageStatsKeyView key,out int value){
   value=0;if(key==null||key.Stats==null||!key.Stats.Type.HasValue)return false;
   value=unchecked((int)key.Stats.Type.Value);return true;
  }
  static bool Finite(float value){return !Single.IsNaN(value)&&!Single.IsInfinity(value);}
  static float Binary32(double value){return BitConverter.ToSingle(BitConverter.GetBytes((float)value),0);}

  // Literal observed lower-bound/control-flow fragment, with live sparse reads.
  // No global traversal, sorting, duplicate removal or implicit dictionary model.
  // Cycles/budget stops are own coverage limits, not native rejection semantics.
  public static NativeDamageLookupResult Lookup(NativeDamageStatisticsTree tree,NativeDamageStatsKeyView key,NativeDamageLookupOutput output,NativeDamageStatisticsConditions conditions){
   var r=new NativeDamageLookupResult(tree,key,output);
   if(conditions==null||!conditions.SerialExclusiveAccess)return Stop(r,"SerialAccessUnobserved",0x403AC0);
   if(conditions.MaxNodeVisits<1||conditions.MaxNodeVisits>4096)return Stop(r,"InvalidOwnVisitBudget",0x403AC0);
   Step(r.Trace,0x403ACF,"ReadHead",null);
   if(tree==null||tree.Head==null)return Stop(r,"UnknownHead",0x403ACF);
   var head=tree.Head;var candidate=head;
   Step(r.Trace,0x403ADB,"ReadRoot",head);
   var node=head.Parent;
   Step(r.Trace,0x403ADF,"ReadSentinel",node);
   if(node==null||!node.Sentinel.HasValue)return Stop(r,"UnknownSentinel",0x403ADF);
   if(node.Sentinel.Value==0){
    int cachedFirst;
    Step(r.Trace,0x403AE6,"ReadKeyFirst",null);
    if(!First(key,out cachedFirst))return Stop(r,"UnknownKeyFirst",0x403AE6);
    var visited=new HashSet<NativeDamageStatisticsNode>();
    while(true){
     if(!visited.Add(node))return Stop(r,"CycleOutsideCoverage",0x403AF0);
     if(r.NodeVisits>=conditions.MaxNodeVisits)return Stop(r,"OwnVisitBudgetExceeded",0x403AF0);
     r.NodeVisits++;
     Step(r.Trace,0x403AF0,"ReadNodeFirst",node);
     if(!node.FirstWord.HasValue)return Stop(r,"UnknownNodeFirst",0x403AF0);
     int nodeFirst=node.FirstWord.Value;bool moveRight;
     if(nodeFirst<cachedFirst)moveRight=true;
     else if(nodeFirst>cachedFirst)moveRight=false;
     else{
      Step(r.Trace,0x403AFB,"ReadKeySecond",null);
      if(key==null||!key.SecondWord.HasValue)return Stop(r,"UnknownKeySecond",0x403AFB);
      int keySecond=key.SecondWord.Value;
      Step(r.Trace,0x403AFF,"ReadNodeSecond",node);
      if(!node.SecondWord.HasValue)return Stop(r,"UnknownNodeSecond",0x403AFF);
      moveRight=node.SecondWord.Value<keySecond;
     }
     if(moveRight){Step(r.Trace,0x403B0D,"ReadRight",node);node=node.Right;}
     else{candidate=node;Step(r.Trace,0x403B08,"ReadLeft",node);node=node.Left;}
     Step(r.Trace,0x403B11,"ReadSentinel",node);
     if(node==null||!node.Sentinel.HasValue)return Stop(r,"UnknownSentinel",0x403B11);
     if(node.Sentinel.Value!=0)break;
    }
   }
   if(Object.ReferenceEquals(candidate,head))return Stop(r,"MissingKeyHelperOutsideCoverage",0x403B59);
   int freshFirst;
   Step(r.Trace,0x403B1D,"ReadKeyFirst",null);
   if(!First(key,out freshFirst))return Stop(r,"UnknownKeyFirst",0x403B1D);
   Step(r.Trace,0x403B20,"ReadNodeFirst",candidate);
   if(!candidate.FirstWord.HasValue)return Stop(r,"UnknownNodeFirst",0x403B20);
   if(freshFirst<candidate.FirstWord.Value)return Stop(r,"MissingKeyHelperOutsideCoverage",0x403B59);
   if(freshFirst==candidate.FirstWord.Value){
    Step(r.Trace,0x403B29,"ReadNodeSecond",candidate);
    if(!candidate.SecondWord.HasValue)return Stop(r,"UnknownNodeSecond",0x403B29);
    int nodeSecond=candidate.SecondWord.Value;
    Step(r.Trace,0x403B2C,"ReadKeySecond",null);
    if(key==null||!key.SecondWord.HasValue)return Stop(r,"UnknownKeySecond",0x403B2C);
    if(key.SecondWord.Value<nodeSecond)return Stop(r,"MissingKeyHelperOutsideCoverage",0x403B59);
   }
   // The actual branch accepts key >= candidate, not an extra equality test.
   // Equality follows only under a stable correctly ordered tree condition.
   if(output==null)return Stop(r,"UnknownOutput",0x403B32);
   output.Node=candidate;r.OutputStoreInstructions++;
   Step(r.Trace,0x403B32,"WriteOutputNode",candidate);
   r.ReturnedOutput=output; // 403B35: RAX is the original output address
   output.Inserted=0;r.OutputStoreInstructions++;
   Step(r.Trace,0x403B38,"WriteOutputFlagZero",candidate);
   r.Status=NativeDamageStatisticsStatus.ConditionalNormalReturn;r.Reason="ExistingBranchReturned";r.ResumeInstructionRva=0x403B4B;
   return r;
  }

  // No external-call replacement hook is invented: both calls use the literal
  // local Lookup above. Fields remain live; the first amount is not a snapshot.
  public static NativeDamageStatisticsResult RunFinal(NativeDamageStatisticsState state,NativeDamageStatsKeyView key,float? damage,NativeDamageStatisticsConditions conditions){
   var r=new NativeDamageStatisticsResult(state,key);
   if(state==null)return Stop(r,"MissingStatisticsState",0x3FE390);
   var firstOutput=new NativeDamageLookupOutput();var secondOutput=new NativeDamageLookupOutput();
   Step(r.Trace,0x3FE3B9,"FirstLookupStarted",null);r.LookupCalls++;
   r.FirstLookup=Lookup(state.Tree,key,firstOutput,conditions);r.Trace.AddRange(r.FirstLookup.Trace);
   if(r.FirstLookup.Status!=NativeDamageStatisticsStatus.ConditionalNormalReturn)return Stop(r,"FirstLookup:"+r.FirstLookup.Reason,0x3FE3B9);
   Step(r.Trace,0x3FE3BE,"FirstLookupReturned",firstOutput.Node);
   Step(r.Trace,0x3FE3CD,"SecondLookupStarted",null);r.LookupCalls++;
   r.SecondLookup=Lookup(state.Tree,key,secondOutput,conditions);r.Trace.AddRange(r.SecondLookup.Trace);
   if(r.SecondLookup.Status!=NativeDamageStatisticsStatus.ConditionalNormalReturn)return Stop(r,"SecondLookup:"+r.SecondLookup.Reason,0x3FE3CD);
   Step(r.Trace,0x3FE3D2,"SecondLookupReturned",secondOutput.Node);
   Step(r.Trace,0x3FE3D2,"ReadFirstOutputNode",firstOutput.Node);
   if(firstOutput.Node==null)return Stop(r,"UnknownFirstOutputNode",0x3FE3D2);
   if(!damage.HasValue)return Stop(r,"UnknownDamage",0x3FE3D7);
   Step(r.Trace,0x3FE3DA,"ReadFirstAmount",firstOutput.Node);r.AmountRead=true;
   if(!firstOutput.Node.Amount.HasValue)return Stop(r,"UnknownFirstAmount",0x3FE3DA);
   float oldAmount=firstOutput.Node.Amount.Value,amount=damage.Value;
   if(conditions==null||!conditions.NormalFloatingPointEnvironment)return Stop(r,"FloatingPointEnvironmentUnobserved",0x3FE3DA);
   if(!Finite(amount)||!Finite(oldAmount))return Stop(r,"NonfiniteAdditionOutsideCoverage",0x3FE3DA);
   float sum=Binary32((double)amount+(double)oldAmount);
   if(!Finite(sum))return Stop(r,"AmountResultOutsideCoverage",0x3FE3DA);
   Step(r.Trace,0x3FE3DF,"ReadSecondOutputNode",secondOutput.Node);
   if(secondOutput.Node==null)return Stop(r,"UnknownSecondOutputNode",0x3FE3DF);
   secondOutput.Node.Amount=sum;r.AmountWritten=true;Step(r.Trace,0x3FE3E4,"WriteSecondAmount",secondOutput.Node);
   int type;Step(r.Trace,0x3FE3E9,"ReadKeyFirst",null);
   if(!First(key,out type))return Stop(r,"UnknownFinalType",0x3FE3E9);
   if(type==1){
    Step(r.Trace,0x3FE3EE,"ReadMaximum",null);
    if(!state.Maximum.HasValue)return Stop(r,"UnknownMaximum",0x3FE3EE);
    float maximum=state.Maximum.Value;
    if(!Finite(maximum))return Stop(r,"NonfiniteMaximumOutsideCoverage",0x3FE3EE);
    if(amount>maximum){state.Maximum=amount;r.MaximumWritten=true;Step(r.Trace,0x3FE3F4,"WriteMaximum",null);}
   }
   r.Status=NativeDamageStatisticsStatus.ConditionalNormalReturn;r.Reason="BothExistingBranchesReturned";r.ResumeInstructionRva=0x3FE40D;
   return r;
  }
 }
 // Reuses the existing Settlement.Final interface without changing old sources.
 // Own field views must be associated with exactly this owner and borrowed stats.
 public sealed class NativeDamageStatisticsBinding {
  readonly NativeDamageState owner;
  readonly NativeDamageStatisticsState state;
  readonly NativeDamageStatsKeyView key;
  readonly NativeDamageStatisticsConditions conditions;
  public NativeDamageStatisticsResult LastResult {get;private set;}
  public NativeDamageStatisticsBinding(NativeDamageState owner,NativeDamageStatisticsState state,NativeDamageStatsKeyView key,NativeDamageStatisticsConditions conditions){this.owner=owner;this.state=state;this.key=key;this.conditions=conditions;}
  public NativeDamageCallObservation Invoke(NativeDamageState actualOwner,float damage,NativeDamageStats actualStats){
   if(owner==null||!Object.ReferenceEquals(owner,actualOwner)||key==null||key.Stats==null||!Object.ReferenceEquals(key.Stats,actualStats)){
    LastResult=new NativeDamageStatisticsResult(state,key);LastResult.Reason="BindingIdentityMismatch";
    return new NativeDamageCallObservation(false);
   }
   LastResult=NativeDamageStatistics.RunFinal(state,key,damage,conditions);
   return new NativeDamageCallObservation(LastResult.Status==NativeDamageStatisticsStatus.ConditionalNormalReturn);
  }
 }
}
