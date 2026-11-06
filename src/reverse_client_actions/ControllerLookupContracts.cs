using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class ExeControllerMapInvocation {
  public readonly Invocation Native=new Invocation();
  public GameObjectManagerLookupInvocation SharedAlgorithm{get;private set;}
  public readonly Field<object> Result=new Field<object>();
  public bool Returned;
  public ExeControllerMapInvocation(){Reset();}
  internal void Reset(){Native.At=0;SharedAlgorithm=new GameObjectManagerLookupInvocation();Result.Forget();Returned=false;}
 }
 public sealed class ExeClassQueryInvocation {
  public readonly Invocation Native=new Invocation();
  public int Visited;
  public bool Returned;
  public readonly Field<byte> ResultAL=new Field<byte>();
  internal void Reset(){Native.At=0;Visited=0;Returned=false;ResultAL.Forget();}
 }
 // Only the mutable IAT value is borrowed. Binding a view initializes neither
 // the native manager, controller vptr nor a ClassRecord parent field.
 public sealed class ControllerLookupContext {
  public readonly PlayerMouseContext Mouse;
  public readonly Field<ClassRecord> ExpectedImport2D7198;
  public readonly AddressSymbol<ClassRecord> ControllerPlayerClassInfoA5C180=new AddressSymbol<ClassRecord>(0xa5c180);
  public ControllerLookupContext(PlayerMouseContext mouse,Field<ClassRecord> expected){if(mouse==null||expected==null)throw new ArgumentNullException("shared mouse/import cells");Mouse=mouse;ExpectedImport2D7198=expected;}
 }
 // Symbolic OWN selection of a separately evidenced static vtable. This is
 // never installed into a controller implicitly or claimed as a sampled vptr.
 public sealed class ControllerLookupPrimaryTable {
  public static readonly ControllerLookupPrimaryTable ObservedControllerPlayer=new ControllerLookupPrimaryTable();
  ControllerLookupPrimaryTable(){}
 }
 public sealed class ControllerLookupInvocation {
  public readonly Invocation Native=new Invocation();
  public ExeControllerMapInvocation Lookup{get;private set;}
  public ExeClassQueryInvocation Query{get;private set;}
  public Field<object> CapturedObject{get;private set;}
  public Field<object> CapturedPrimary{get;private set;}
  public Field<ClassRecord> CapturedExpected{get;private set;}
  public Field<ClassRecord> ReturnedClass{get;private set;}
  public Field<ClassRecord> Parent{get;private set;}
  public Field<object> Result{get;private set;}
  public bool LookupReturned,PrimaryCaptured,ExpectedCaptured,ClassCallReached,ClassReturned,FilterAccepted,Returned;
  public ControllerLookupInvocation(){Reset();}
  internal void Reset(){Native.At=0;Lookup=new ExeControllerMapInvocation();Query=new ExeClassQueryInvocation();CapturedObject=new Field<object>();CapturedPrimary=new Field<object>();CapturedExpected=new Field<ClassRecord>();ReturnedClass=new Field<ClassRecord>();Parent=new Field<ClassRecord>();Result=new Field<object>();LookupReturned=PrimaryCaptured=ExpectedCaptured=ClassCallReached=ClassReturned=FilterAccepted=Returned=false;}
 }
 public static class ControllerLookupContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing own controller lookup view");return value;}
  // Disk Game62310: 8B81C0160000C3, MOV EAX,[RCX+16C0];RET.
  // Borrow the existing full Character field; no new sidecar or native
  // writer/initialization/runtime-equivalence claim is introduced here.
  public static uint CharacterGetControllerId62310(GameMovementIdentityBridge bridge,MoveCharacter role,Invocation invocation){
   var i=Need(invocation,0x62310);i.At=0x62310;
   var view=Need(bridge,i.At).ForMoveCharacter(role,i.At);
   uint value=view.BorrowAssociatedId16C0(i.At).Read(i.At);i.At=0x62316;return value;
  }
  // Saved EXE9ED0 and Game019D40 have the same67 instruction/offset shape,
  // FNV constants, mutable map reads and branch targets. Only label/import
  // references differ. Reuse the SAME map and OWN Monitor, not a second lock.
  // Native Enter/Exit IAT targets/ABI and diagnostic label remain unproved.
  static uint ExeMapAddress(uint at){return at>=0x019d40&&at<=0x019e36?at-0xfe70u:at;}
  public static Field<object> Lookup9ED0(GameObjectManagerState manager,Field<uint> key,ExeControllerMapInvocation invocation){
   var call=Need(invocation,0x9ee3);call.Reset();bool missing=false;
   try{var result=GameObjectManagerLookupContracts.LookupBits019D40(manager,key,call.SharedAlgorithm);call.Result.CopyFrom(result);call.Returned=true;return call.Result.Snapshot();}
   catch(MissingContract error){missing=true;uint mapped=ExeMapAddress(error.Address);call.Native.At=mapped;if(mapped==error.Address)throw;throw new MissingContract(mapped,error.Message,error);}
   finally{if(!missing)call.Native.At=ExeMapAddress(call.SharedAlgorithm.Native.At);}
  }
  // Actual loaded EXE2A33B0..2A33E3. The parent is tested before expected is
  // consumed; this is NOT self-or-derived. Return contract is AL only.
  public static byte Query2A33B0(ClassRecord current,Field<ClassRecord> expected,ExeClassQueryInvocation invocation){
   var call=Need(invocation,0x2a33b4);call.Reset();var target=expected==null?new Field<ClassRecord>():expected.Snapshot();var visited=new HashSet<ClassRecord>();
   while(true){
    call.Native.At=0x2a33b4;var node=Need(current,call.Native.At);if(!visited.Add(node))throw new MissingContract(call.Native.At,"Cyclic OWN class chain is outside the finite domain");call.Visited++;
    var parentBits=node.Parent10.Snapshot();call.Native.At=0x2a33b8;var parent=parentBits.Read(call.Native.At);
    if(parent==null){call.Native.At=call.Visited==1?0x2a33e2u:0x2a33d1u;call.ResultAL.Write(0);call.Returned=true;return 0;}
    call.Native.At=0x2a33bd;if(Object.ReferenceEquals(parent,target.Read(call.Native.At))){call.Native.At=0x2a33db;call.ResultAL.Write(1);call.Returned=true;return 1;}
    call.Native.At=0x2a33c2;current=parent;
   }
  }
  static Field<object> Finish(bool accepted,ControllerLookupInvocation call){
   call.FilterAccepted=accepted;if(accepted)call.Result.CopyFrom(call.CapturedObject);else call.Result.Write(null);
   call.Native.At=accepted?0x1bd23u:0x1bd30u;call.Returned=true;return call.Result.Snapshot();
  }
  static Field<object> Filter(ControllerLookupInvocation call){
   var i=call.Native;i.At=0x1bcf7;var actual=call.ReturnedClass.Read(i.At);var expected=call.CapturedExpected.Read(i.At);
   if(Object.ReferenceEquals(actual,expected))return Finish(true,call);
   i.At=0x1bcfc;call.Parent.CopyFrom(Need(actual,i.At).Parent10);i.At=0x1bd00;var parent=call.Parent.Read(i.At);if(parent==null)return Finish(false,call);
   i.At=0x1bd05;if(Object.ReferenceEquals(parent,expected))return Finish(true,call);
   i.At=0x1bd0d;byte result;try{result=Query2A33B0(parent,call.CapturedExpected,call.Query);}catch(MissingContract error){i.At=error.Address;throw;}
   i.At=0x1bd12;return Finish(result!=0,call);
  }
  public static Field<ClassRecord> Class0(object instance,object capturedPrimary,ControllerLookupContext context,Invocation i){
   if(!Object.ReferenceEquals(capturedPrimary,ControllerLookupPrimaryTable.ObservedControllerPlayer))throw new MissingContract(i.At,"Concrete controller class slot0 target is unbound");
   // Disk6E0350 slot0 ->51AC0: 488D05B9A6A000C3, LEA A5C180;RET.
   // Symbolic OWN binding only: runtime vptr/getter equivalence is unobserved.
   i.At=0x051ac0;var bits=new Field<ClassRecord>();
   // This catch converts ONLY this model symbol's unknown knowledge to an
   // opaque LEA result. It is not recovery from a native execution exception.
   try{bits.Write(context.ControllerPlayerClassInfoA5C180.Read(0));}catch(MissingContract){}
   i.At=0x051ac7;return bits;
  }
  public static Field<object> Resolve1BCD0(GameObjectManagerState manager,Field<uint> key,ControllerLookupContext context,ControllerLookupInvocation invocation){
   var call=Need(invocation,0x1bcda);call.Reset();var i=call.Native;i.At=0x1bcda;Field<object> found;
   try{found=Lookup9ED0(manager,key,call.Lookup);call.LookupReturned=true;}catch(MissingContract error){i.At=error.Address;throw;}
   i.At=0x1bcdf;call.CapturedObject.CopyFrom(found);i.At=0x1bce2;var instance=call.CapturedObject.Read(i.At);if(instance==null)return Finish(false,call);
   i.At=0x1bce7;var ctx=Need(context,i.At);call.CapturedPrimary.CopyFrom(ctx.Mouse.Object(instance,i.At).Virtual00.Snapshot());call.PrimaryCaptured=true;
   i.At=0x1bced;call.CapturedExpected.CopyFrom(ctx.ExpectedImport2D7198.Snapshot());call.ExpectedCaptured=true;
   i.At=0x1bcf4;var table=call.CapturedPrimary.Read(i.At);call.ClassCallReached=true;call.ReturnedClass.CopyFrom(Class0(instance,table,ctx,i));call.ClassReturned=true;
   return Filter(call);
  }
  // OWN composition check, not a native cast/getter. No new Controller or
  // synchronized state is manufactured after the actual class filter.
  public static Controller BorrowCanonicalController(ControllerLookupInvocation completed,PlayerMouseContext mouse){
   var call=Need(completed,0x22604);if(!call.Returned||!call.FilterAccepted)throw new MissingContract(0x22604,"Controller lookup has no accepted result");
   var identity=call.Result.Read(0x22604) as Controller;if(identity==null)throw new MissingContract(0x22604,"Accepted object lacks an existing Controller identity");
   var view=Need(mouse,0x22604).Controller(identity,0x22604);if(!Object.ReferenceEquals(view.Identity,identity))throw new MissingContract(0x22604,"Canonical Controller identity differs");return identity;
  }
 }
}
