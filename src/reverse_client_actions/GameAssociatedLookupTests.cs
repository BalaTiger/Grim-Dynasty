using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Isolated OWN class-slot observations exercise the recovered caller. They do
// not supply production virtual targets or establish native runtime loading.
public static class GameAssociatedLookupTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 sealed class OwnClassObservations:GameAssociatedLookupBoundaries {
  public readonly Field<object> Primary=new Field<object>();
  public readonly Field<ClassRecord> ClassResult=new Field<ClassRecord>();
  public readonly List<object> ClassInstances=new List<object>(),ClassPrimaries=new List<object>();
  public int Captures;
  public bool SlotUnbound;
  public Action DuringClass;
  public OwnClassObservations(AddressSymbol<ClassRecord> expected):base(expected){}
  public override Field<object> CapturePrimary(object instance,Invocation i){Captures++;Eq(0x077d73u,i.At,"Capture at actual MOV");return Primary;}
  public override Field<ClassRecord> Class0(object instance,object capturedPrimary,Invocation i){
   Eq(0x077d79u,i.At,"Slot at actual CALL");ClassInstances.Add(instance);ClassPrimaries.Add(capturedPrimary);
   if(SlotUnbound)return base.Class0(instance,capturedPrimary,i);
   if(DuringClass!=null)DuringClass();return ClassResult.Snapshot();
  }
 }
 sealed class Fixture {
  public readonly GameObjectManagerState Manager=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode(),Node=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public readonly object Value=new object(),Primary=new object();
  public readonly ClassRecord Expected=new ClassRecord();
  public readonly AddressSymbol<ClassRecord> Symbol=new AddressSymbol<ClassRecord>(0xa5c9f0);
  public readonly OwnClassObservations Observations;
  public readonly GameAssociatedLookupInvocation Call=new GameAssociatedLookupInvocation();
  public readonly Field<uint> Id=Field<uint>.Of(7);
  public Fixture():this(true){}
  public Fixture(bool bindTarget){
   if(bindTarget)Symbol.Write(Expected);Observations=new OwnClassObservations(Symbol);Observations.Primary.Write(Primary);Observations.ClassResult.Write(Expected);
   Manager.OwnCritical18.Write(Lock);Manager.Sentinel38.Write(Sentinel);Manager.Buckets48.Write(Buckets);Manager.Mask60.Write(0);
   Node.Key10.Write(7);Node.Object18.Write(Value);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);
  }
  public object Run(){return GameAssociatedLookupContracts.Lookup077D60(Manager,Id,Observations,Call);}
  public void LookupFinished(){Check(Call.LookupReturned&&Call.Lookup.Returned,"Actual019D40 returned");Check(Call.Lookup.OwnLockEntered&&Call.Lookup.OwnLockExited,"Actual shared map lock released before slot");Eq(0,Lock.OwnOutstandingEnters,"No outstanding own lock");Eq(4,Call.Lookup.HashedBytes,"Actual four-byte map hashing");}
  public void Returned(object result){LookupFinished();Check(Call.Returned,"Caller returned");Same(result,Call.Result.Read(0),"Recorded exact result");Eq(result==null?0x077daeu:0x077da6u,Call.Native.At,"Actual RET");}
 }
 public static void Run(){scenarios=assertions=0;
  Test("Real map miss bypasses every class prerequisite",delegate{
   var f=new Fixture(false);f.Id.Write(8);
   Same(null,GameAssociatedLookupContracts.Lookup077D60(f.Manager,f.Id,null,f.Call),"Real019D40 miss");f.Returned(null);
   Check(!f.Call.PrimaryCaptured&&!f.Call.ClassReturned&&!f.Call.DerivedEntered,"Miss never reads class state");Eq(0,f.Observations.Captures,"No synthetic class call");
  });
  Test("Exact class returns captured object without reading Parent10",delegate{
   var f=new Fixture();Same(f.Value,f.Run(),"Exact class keeps full identity");f.Returned(f.Value);
   Same(f.Value,f.Call.CapturedObject.Read(0),"RBX capture");Same(f.Primary,f.Call.CapturedPrimary.Read(0),"Captured vptr");Same(f.Expected,f.Call.ReturnedClass.Read(0),"Returned class identity");
   Missing(0,delegate{f.Expected.Parent10.Read(0);},"Exact match leaves unknown parent");Check(!f.Call.DerivedEntered,"No unnecessary inheritance traversal");
  });
  Test("Direct parent match bypasses helper and returns original object",delegate{
   var f=new Fixture();var child=new ClassRecord();child.Parent10.Write(f.Expected);f.Observations.ClassResult.Write(child);
   Same(f.Value,f.Run(),"Direct parent accepted");f.Returned(f.Value);Same(f.Expected,f.Call.Parent.Read(0),"Captured parent");Check(!f.Call.DerivedEntered,"Inline parent match skips591480");
  });
  Test("Deeper parent uses actual591480",delegate{
   var f=new Fixture();var child=new ClassRecord();var middle=new ClassRecord();child.Parent10.Write(middle);middle.Parent10.Write(f.Expected);f.Observations.ClassResult.Write(child);
   Same(f.Value,f.Run(),"Actual helper accepts grandparent");f.Returned(f.Value);Check(f.Call.DerivedEntered&&f.Call.DerivedReturned,"Real helper returned");Eq(0x5914a2u,f.Call.Derived.At,"Actual helper true return");
  });
  Test("Known unrelated graphs return null without changing map",delegate{
   var f=new Fixture();var child=new ClassRecord();child.Parent10.Write(null);f.Observations.ClassResult.Write(child);Same(null,f.Run(),"Inline null parent");f.Returned(null);Check(!f.Call.DerivedEntered,"Null parent skips helper");
   var middle=new ClassRecord();child.Parent10.Write(middle);middle.Parent10.Write(null);Same(null,f.Run(),"Actual helper false");f.Returned(null);Check(f.Call.DerivedEntered&&f.Call.DerivedReturned,"Actual helper returned false");Same(f.Value,f.Node.Object18.Read(0),"Filter never deletes map value");
  });
  Test("Unknown map value returns from019D40 before caller TEST fails",delegate{
   var f=new Fixture();f.Node.Object18.Forget();Missing(0x077d6e,delegate{f.Run();},"Unknown is consumed at TEST, not lookup MOV");f.LookupFinished();
   Check(!f.Call.Returned&&!f.Call.PrimaryCaptured,"No false null or class call");Missing(0,delegate{f.Call.CapturedObject.Read(0);},"RBX retains opaque Unknown");
  });
  Test("Lookup failures retain exact nested prefix and release own lock",delegate{
   var unknown=new Fixture();unknown.Id.Forget();Missing(0x019d81,delegate{unknown.Run();},"Unknown key consumed by actual hash");Eq(0x019d81u,unknown.Call.Native.At,"Outer diagnostic retains nested hash failure");Check(!unknown.Call.LookupReturned&&!unknown.Call.ClassReturned,"No false lookup success");Check(unknown.Call.Lookup.OwnCleanupReleased,"OWN exceptional lock cleanup");Eq(0,unknown.Lock.OwnOutstandingEnters,"Unknown key lock released");
   var released=new Fixture();released.Node.MarkReleased();Missing(0x019dde,delegate{released.Run();},"Released bucket-tail node is read before its key");Eq(0x019ddeu,released.Call.Native.At,"Outer diagnostic retains first nested node failure");Check(released.Call.Lookup.OwnCleanupReleased&&!released.Call.LookupReturned,"Released node prefix");Eq(0,released.Lock.OwnOutstandingEnters,"Released node lock released");
  });
  Test("Unknown primary and unbound slot stop before class return",delegate{
   var f=new Fixture();f.Observations.Primary.Forget();Missing(0x077d79,delegate{f.Run();},"Unknown captured vptr consumed by CALL");f.LookupFinished();Check(f.Call.PrimaryCaptured&&!f.Call.ClassReturned,"MOV prefix retained");Eq(0,f.Observations.ClassInstances.Count,"Unknown table never dispatched");
   f.Observations.Primary.Write(f.Primary);f.Observations.SlotUnbound=true;Missing(0x077d79,delegate{f.Run();},"No inferred virtual target");Check(!f.Call.ClassReturned,"Unbound virtual target not a class return");
  });
  Test("Unknown class and target fail at CMP; null class fails at dereference",delegate{
   var unknownClass=new Fixture();unknownClass.Observations.ClassResult.Forget();Missing(0x077d82,delegate{unknownClass.Run();},"Opaque class return consumed at CMP");Check(unknownClass.Call.ClassReturned&&!unknownClass.Call.Returned,"Class returned Unknown, caller did not return");
   var unknownTarget=new Fixture(false);Missing(0x077d82,delegate{unknownTarget.Run();},"Fixed symbol requires explicit identity");Check(unknownTarget.Call.ClassReturned&&!unknownTarget.Call.Returned,"Unknown target does not skip slot");
   var nullClass=new Fixture();nullClass.Observations.ClassResult.Write(null);Missing(0x077d87,delegate{nullClass.Run();},"Null class never becomes a rejected-null lookup result");Check(nullClass.Call.ClassReturned&&!nullClass.Call.Returned,"Null class reaches actual parent dereference");
  });
  Test("Unknown parent and cyclic ancestry remain explicit failures",delegate{
   var f=new Fixture();var child=new ClassRecord();f.Observations.ClassResult.Write(child);Missing(0x077d8b,delegate{f.Run();},"Parent MOV is opaque; TEST consumes Unknown");Check(!f.Call.DerivedEntered&&!f.Call.Returned,"No unknown parent converted to null");
   var middle=new ClassRecord();child.Parent10.Write(middle);Missing(0x591484,delegate{f.Run();},"Actual helper reads unknown next parent");Check(f.Call.DerivedEntered&&!f.Call.DerivedReturned&&!f.Call.Returned,"Nested unknown retains helper failure");
   middle.Parent10.Write(middle);Missing(0x591484,delegate{f.Run();},"Cyclic own graph is outside supported domain");Check(!f.Call.Returned,"Cycle is not false");
  });
  Test("Class-call reentry cannot replace captured return or primary",delegate{
   var f=new Fixture();var replacement=new object();var newPrimary=new object();var nested=new GameAssociatedLookupInvocation();
   f.Observations.DuringClass=delegate{
    Eq(0,f.Lock.OwnOutstandingEnters,"Native lookup left lock before virtual callback");f.Node.Object18.Write(replacement);f.Observations.Primary.Write(newPrimary);f.Observations.DuringClass=null;
    Same(replacement,GameAssociatedLookupContracts.Lookup077D60(f.Manager,f.Id,f.Observations,nested),"Reentrant lookup sees current map");
   };
   Same(f.Value,f.Run(),"Outer RBX survives reentry and map mutation");f.Returned(f.Value);Same(replacement,f.Node.Object18.Read(0),"Mutation is retained");Same(f.Value,f.Observations.ClassInstances[0],"Outer class self");Same(replacement,f.Observations.ClassInstances[1],"Nested class self");Same(f.Primary,f.Observations.ClassPrimaries[0],"Outer vptr snapshot");Same(newPrimary,f.Observations.ClassPrimaries[1],"Nested fresh vptr");
  });
  Test("Reused invocation replaces diagnostic fields without altering retained results",delegate{
   var f=new Fixture();Same(f.Value,f.Run(),"First call");var firstResult=f.Call.Result;var firstCapture=f.Call.CapturedObject;var firstLookup=f.Call.Lookup;
   f.Id.Write(8);Same(null,f.Run(),"Second actual miss");f.Returned(null);Same(f.Value,firstResult.Read(0),"Retained first result");Same(f.Value,firstCapture.Read(0),"Retained first capture");Check(!Object.ReferenceEquals(firstLookup,f.Call.Lookup),"Independent nested lookup diagnostics");Check(!f.Call.PrimaryCaptured&&!f.Call.ClassReturned&&!f.Call.DerivedEntered,"No stale success flags");
  });
 }
}
