using System;
namespace GrimDynasty.ReverseClient.Actions {
 public sealed class NavigationCrowdSynchronousReplanInvocation {
  public readonly Field<byte> Mode=new Field<byte>(),Completion=new Field<byte>();
  public readonly Field<uint> Generation=new Field<uint>();
  public bool SubmitReturned,FailurePrefixWritten,ReplanCleared,Returned;
 }
 // Exact25C727..25C832 continuation from the retained Engine25C440 assembly
 // r1-crowd-movement-direct/08. Existing25C78D copies/generation/reset/Submit
 // is reused. No async worker, result injection, diagnostic callback, full
 // native registration or complete multi-agent tick is inferred.
 public static class NavigationCrowdSynchronousReplanContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null own synchronous replan view");var n=x as NavigationView;if(n!=null)n.RequireLive(at);return x;}
  public static void Run25C727(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdTickProductionBoundaries production,NavigationCrowdSynchronousReplanInvocation call,Invocation i){
   Need(i,0x25c727);Need(call,0x25c727);call.Mode.Forget();call.Completion.Forget();call.Generation.Forget();call.SubmitReturned=false;call.FailurePrefixWritten=false;call.ReplanCleared=false;call.Returned=false;
   i.At=0x25c727;uint captured=Need(agent,i.At).Flags44.Read(i.At);byte mode=(byte)((~(captured>>7))&1u);call.Mode.Write(mode);
   i.At=0x25c736;if((agent.Flags44.Read(i.At)&0x100)!=0){i.At=0x25c73f;throw new MissingContract(i.At,"Asynchronous25B8A0 branch remains unsupported; no fabricated State1 or queue");}
   // A new boundary wrapper merely forwards the already established adapter;
   // it does not allocate another manager/query, initialize BSS or copy state.
   var state=new NavigationCrowdStateBoundaries(Need(production,0x25c78d).Adapter);
   i.At=0x25c78d;NavigationCrowdStateContracts.SubmitSynchronousFragment25C78D(group,agent,mode,state,i);call.SubmitReturned=true;call.Generation.CopyFrom(agent.RequestE8.Generation00);
   i.At=0x25c7e0;byte complete=agent.RequestE8.Complete50.Read(i.At);call.Completion.Write(complete);
   if(complete==0){
    var callback=agent.Callback08.Snapshot();i.At=0x25c7ee;agent.State70.Write(6);i.At=0x25c7fb;agent.Reason74.Write(4);call.FailurePrefixWritten=true;
    i.At=0x25c802;var global=Need(production.Global422980,i.At);var x=global.X.Snapshot();var y=global.Y.Snapshot();i.At=0x25c80a;agent.VelocityB8.X.CopyFrom(x);agent.VelocityB8.Y.CopyFrom(y);i.At=0x25c812;var z=global.Z.Snapshot();i.At=0x25c818;agent.VelocityB8.Z.CopyFrom(z);i.At=0x25c81e;agent.Timer6C.Write(0);
    i.At=0x25c825;var view=callback.Read(i.At) as GameMovementInterfaceView;Need(view,i.At).FullOwner.Entity.RequireLive(i.At);var table=view.Virtual38.Snapshot();table.Read(i.At);
    i.At=0x25c828;throw new MissingContract(i.At,"Synchronous search failure reaches actual Game callback slot38; no successful callback or replan clear is invented");
   }
   i.At=0x25c82b;agent.NeedReplan140.Write(0);call.ReplanCleared=true;i.At=0x25c832;call.Returned=true;
  }
 }
}
