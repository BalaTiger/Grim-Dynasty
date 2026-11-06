using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // One extra assertion byte per existing movement composition. The common
 // 42144A cell is borrowed, never copied; all fields remain Unknown by default.
 public sealed class NavigationStopObjectContext {
  static readonly ConditionalWeakTable<NavigationMoveObjectContext,NavigationStopObjectContext> contexts=new ConditionalWeakTable<NavigationMoveObjectContext,NavigationStopObjectContext>();
  public readonly NavigationMoveObjectContext Movement;
  public readonly Field<byte> DisableMissingAgent422B8E=new Field<byte>();
  public Field<byte> DisableAllAssert42144A{get{return Movement.DisableAllAssert42144A;}}
  NavigationStopObjectContext(NavigationMoveObjectContext movement){Movement=movement;}
  public static NavigationStopObjectContext ForMovement(NavigationMoveObjectContext movement){if(movement==null)throw new ArgumentNullException("existing movement context");return contexts.GetValue(movement,delegate(NavigationMoveObjectContext value){return new NavigationStopObjectContext(value);});}
 }
 public sealed class NavigationStopObjectInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Field<ulong> Key=new Field<ulong>();
  public readonly Field<NavigationCrowdAgent> Agent=new Field<NavigationCrowdAgent>();
  public NavigationMoveObjectInvocation Lookup{get;private set;}
  public NavigationPoint3 CapturedPosition{get;private set;}
  public readonly List<uint> StoreAddresses=new List<uint>();
  public NavigationCrowdAdapterState Outer;
  public NavigationCrowdManager Crowd;
  public NavigationMoveRegistry Registry;
  public bool LookupReturned,Hit,Returned;
  public NavigationStopObjectInvocation(){Reset();}
  internal void Reset(){Native.At=0;Key.Forget();Agent.Forget();Lookup=new NavigationMoveObjectInvocation();CapturedPosition=new NavigationPoint3();StoreAddresses.Clear();Outer=null;Crowd=null;Registry=null;LookupReturned=Hit=Returned=false;}
 }
 // Engine151150 and25B560: r1-engine-stop-object-run1 and
 // r1-engine-stop-final-dependencies-run1; Engine SHA4c2ea14c...8a93153.
 // Stable single-thread OWN typed allocations/address bindings. MOV copies
 // preserve unknown coordinate bits; no floating-point operation is performed.
 // No registration, Agent allocation, callback, path completion or drift claim.
 public static class NavigationStopObjectContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own StopObject view");var live=value as NavigationView;if(live!=null)live.RequireLive(at);return value;}
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){Need(point,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static void Core(Field<NavigationCrowdManager> crowdBits,Field<ulong> key,NavigationStopObjectContext context,NavigationStopObjectInvocation call){
   var i=call.Native;i.At=0x25b56e;var localKey=Need(key,i.At).Snapshot();call.Key.CopyFrom(localKey);
   i.At=0x25b57e;
   // LEA manager+28 is not a dereference. The borrowed manager becomes
   // necessary in the actual05DAF0 callee, not while forming the address.
   var crowd=Need(Need(crowdBits,0x05daf5).Read(0x05daf5),0x05daf5);call.Crowd=crowd;
   var map=NavigationMoveRegistry.ForManager(crowd);call.Registry=map;
   var node=NavigationMoveObjectContracts.Lookup05DAF0(map,localKey,call.Lookup);call.LookupReturned=true;
   i.At=0x25b58b;
   if(!Object.ReferenceEquals(node,map.Sentinel30.Read(i.At))){
    i.At=0x25b591;var agentBits=Need(node,i.At).Agent18.Snapshot();call.Agent.CopyFrom(agentBits);
    i.At=0x25b595;var agent=agentBits.Read(i.At);
    if(agent!=null){
     i.At=0x25b59a;Need(agent,i.At);var x=Cell(agent.Position48,0,i.At).Snapshot();var y=Cell(agent.Position48,1,i.At).Snapshot();
     i.At=0x25b59f;var z=Cell(agent.Position48,2,i.At).Snapshot();
     call.CapturedPosition.X.CopyFrom(x);call.CapturedPosition.Y.CopyFrom(y);call.CapturedPosition.Z.CopyFrom(z);call.Hit=true;
     // Native QWORD XY store, then DWORD Z. Both source loads precede either
     // destination store; the two scalar cells represent one stable XY pair.
     i.At=0x25b5a2;var targetX=Cell(agent.Target78,0,i.At);var targetY=Cell(agent.Target78,1,i.At);targetX.CopyFrom(x);targetY.CopyFrom(y);call.StoreAddresses.Add(i.At);
     i.At=0x25b5a7;Cell(agent.Target78,2,i.At).CopyFrom(z);call.StoreAddresses.Add(i.At);
     i.At=0x25b5ad;agent.State70.Write(3);call.StoreAddresses.Add(i.At);
     i.At=0x25b5b4;agent.Timer6C.Write(0);call.StoreAddresses.Add(i.At);
     i.At=0x25b5c3;call.Returned=true;return;
    }
   }
   i.At=0x25b5c4;
   if(Need(context,i.At).DisableMissingAgent422B8E.Read(i.At)==0){
    i.At=0x25b5d1;
    if(context.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25b5f8;throw new MissingContract(i.At,"Native GetLastError/diagnostic path is unclosed; no assert byte is changed");}
   }
   i.At=0x25b6c1;call.Returned=true;
  }
  public static void Stop25B560(NavigationCrowdManager crowd,Field<ulong> key,NavigationStopObjectContext context,NavigationStopObjectInvocation call){
   Need(call,0x25b560).Reset();try{Core(Field<NavigationCrowdManager>.Of(crowd),key,context,call);}catch(MissingContract e){call.Native.At=e.Address;throw;}
  }
  public static void Stop151150(NavigationMoveManagerView navigation,GameMovementCharacterState owner,NavigationStopObjectContext context,NavigationStopObjectInvocation call){
   Need(call,0x151150).Reset();var i=call.Native;
   try{
    i.At=0x151150;var outerBits=Need(navigation,i.At).Outer10.Snapshot();
    i.At=0x15115e;bool nullOwner=owner==null;
    i.At=0x151161;var outer=Need(outerBits.Read(i.At),i.At);call.Outer=outer;var crowdBits=NavigationMoveOuterView.ForAdapter(outer).Crowd08.Snapshot();
    // The CLR-to-OWN address translation is not a native actor dereference.
    // A non-null owner must already have its canonical immutable+38 binding.
    i.At=0x151165;var key=Field<ulong>.Of(nullOwner?0:Need(context,i.At).Movement.Addresses.Address(owner.Interface38,i.At));
    i.At=0x151169;Core(crowdBits,key,context,call);
   }catch(MissingContract e){i.At=e.Address;throw;}
  }
 }
}
