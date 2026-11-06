using System;
using GrimDynasty.ReverseClient.Actions;

// Explicit OWN map/class/table publication, never observed native startup.
// Tests use real query/lookup bodies; no virtual success callback is installed.
public static class ControllerLookupTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Case(Action body){body();scenarios++;}
 static void Stop(Action body,uint address){try{body();throw new Exception("Expected Unsupported");}catch(MissingContract error){Check(error.Address==address,"Unsupported "+error.Address.ToString("x")+" expected "+address.ToString("x"));}}
 static void Unknown<T>(Field<T> value,string why){bool unknown=false;try{value.Read(0);}catch(MissingContract){unknown=true;}Check(unknown,why);}
 sealed class Fixture {
  public readonly GameObjectManagerState Manager=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode(),Node=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public readonly Controller Controller=new Controller();
  public readonly ClassRecord ClassInfo=new ClassRecord();
  public readonly Field<ClassRecord> Expected=new Field<ClassRecord>();
  public readonly Field<uint> Key=Field<uint>.Of(7);
  public readonly PlayerMouseContext Mouse=new PlayerMouseContext(new MoveBoundaries(new MoveContext()));
  public readonly MouseObjectView ObjectView;
  public readonly ControllerLookupContext Context;
  public readonly ControllerLookupInvocation Call=new ControllerLookupInvocation();
  public Fixture(bool registerController=true){
   Manager.OwnCritical18.Write(Lock);Manager.Sentinel38.Write(Sentinel);Manager.Buckets48.Write(Buckets);Manager.Mask60.Write(0);
   Node.Key10.Write(7);Node.Object18.Write(Controller);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);
   ObjectView=new MouseObjectView(Controller);ObjectView.Virtual00.Write(ControllerLookupPrimaryTable.ObservedControllerPlayer);Mouse.Attach(ObjectView);
   if(registerController)Mouse.Attach(new MouseControllerState(Controller));
   Expected.Write(ClassInfo);Context=new ControllerLookupContext(Mouse,Expected);Context.ControllerPlayerClassInfoA5C180.Write(ClassInfo);
  }
  public Field<object> Run(){return ControllerLookupContracts.Resolve1BCD0(Manager,Key,Context,Call);}
  public void MapReturned(){Check(Call.LookupReturned&&Call.Lookup.Returned&&Call.Lookup.SharedAlgorithm.Returned,"Actual shared map algorithm returned");Check(Call.Lookup.SharedAlgorithm.OwnLockEntered&&Call.Lookup.SharedAlgorithm.OwnLockExited&&Lock.OwnOutstandingEnters==0,"OWN lock released, not proof of native Enter/Exit");}
 }
 public static int Main(){try{
  Case(delegate{
   var f=new Fixture();f.Key.Write(8);var result=ControllerLookupContracts.Resolve1BCD0(f.Manager,f.Key,null,f.Call);f.MapReturned();
   Check(result.Read(0)==null&&f.Call.Returned&&!f.Call.FilterAccepted&&f.Call.Native.At==0x1bd30,"Actual miss returns null before class prerequisites");
   Check(!f.Call.PrimaryCaptured&&!f.Call.ExpectedCaptured&&!f.Call.ClassCallReached,"No default class view on miss");Check(f.Call.Lookup.Native.At==0x9fc6,"EXE miss return address mapped");
  });
  Case(delegate{
   var f=new Fixture();f.Key.Forget();Stop(delegate{f.Run();},0x9f11);
   Check(f.Call.Native.At==0x9f11&&f.Call.Lookup.Native.At==0x9f11&&!f.Call.LookupReturned,"Unknown key retains EXE consumption address");
   Check(f.Call.Lookup.SharedAlgorithm.OwnCleanupReleased&&f.Lock.OwnOutstandingEnters==0,"Failure releases only OWN lock");
   var call=new ExeControllerMapInvocation();Stop(delegate{ControllerLookupContracts.Lookup9ED0(f.Manager,null,call);},0x9ee3);
   Check(call.Native.At==0x9ee3&&!call.SharedAlgorithm.OwnLockEntered,"Missing key view address is not overwritten by finally");
  });
  Case(delegate{
   var f=new Fixture();f.Node.Object18.Forget();Stop(delegate{f.Run();},0x1bce2);f.MapReturned();
   Check(f.Call.Lookup.Native.At==0x9fae&&!f.Call.ClassCallReached&&!f.Call.Returned,"Opaque map result returns before caller TEST consumes it");Unknown(f.Call.CapturedObject,"Unknown Object18 is not null");
  });
  Case(delegate{
   var f=new Fixture();var result=f.Run();f.MapReturned();
   Check(f.Call.ClassReturned&&f.Call.FilterAccepted&&f.Call.Returned&&Object.ReferenceEquals(result.Read(0),f.Controller),"Actual bound getter and equality preserve lookup object");
   Unknown(f.ClassInfo.Parent10,"Exact class match never initializes or reads parent");
   var same=ControllerLookupContracts.BorrowCanonicalController(f.Call,f.Mouse);Check(Object.ReferenceEquals(same,f.Controller),"Same registered Controller, no replacement");
   var position=new WorldPosition24();var widget=new WidgetMouseCallerState(new object(),position);widget.Byte121.Write(0);widget.Byte1D0.Write(1);widget.Pointer868.Write(null);widget.Byte85.Write(1);widget.Byte848.Write(0);widget.Byte849.Write(0);
   var caller=new WidgetMouseCallerInvocation();var id=Field<uint>.Of(9);
   Stop(delegate{WidgetMouseCallerContracts.Call225A2(widget,Field<byte>.Of(0),Field<byte>.Of(1),Field<Controller>.Of(same),Field<WorldPosition24>.Of(position),id,new PlayerMouseBoundaries(f.Mouse),caller);},0x128530);
   Check(caller.ReachedCall&&!caller.ActionReturned&&Object.ReferenceEquals(caller.CapturedController.Read(0),f.Controller),"Real caller receives exact filtered Controller and reaches genuine remaining lookup boundary");
   Check(Object.ReferenceEquals(caller.Frame.ObjectId.Read(0),id)&&Object.ReferenceEquals(caller.Frame.OptionalByte.Read(0),widget.Byte1D0),"Caller payload pointers still shared");
  });
  Case(delegate{
   var f=new Fixture();f.Expected.Forget();Stop(delegate{f.Run();},0x1bcf7);f.MapReturned();
   Check(f.Call.ExpectedCaptured&&f.Call.ClassReturned&&!f.Call.Returned,"Unknown pre-slot IAT is consumed after real getter, at CMP");
   Check(Object.ReferenceEquals(f.Call.ReturnedClass.Read(0),f.ClassInfo),"Getter fixed symbol is independent of unknown IAT");Unknown(f.Call.CapturedExpected,"Captured IAT stays unknown");
   var unbound=new ControllerLookupContext(f.Mouse,Field<ClassRecord>.Of(f.ClassInfo));
   Stop(delegate{ControllerLookupContracts.Resolve1BCD0(f.Manager,f.Key,unbound,f.Call);},0x1bcf7);
   Check(f.Call.ClassReturned&&!f.Call.Returned,"Unknown fixed-address binding travels opaquely from LEA to CMP");Unknown(f.Call.ReturnedClass,"No guessed class object");
  });
  Case(delegate{
   var f=new Fixture();f.Expected.Forget();f.ObjectView.Virtual00.Forget();Stop(delegate{f.Run();},0x1bcf4);
   Check(f.Call.PrimaryCaptured&&f.Call.ExpectedCaptured&&!f.Call.ClassReturned,"Expected MOV precedes unknown table consumption");
   f.ObjectView.Virtual00.Write(new object());Stop(delegate{f.Run();},0x1bcf4);
   Check(f.Call.ClassCallReached&&!f.Call.ClassReturned&&!f.Call.Returned,"Unrecognized concrete slot is not default class success");
  });
  Case(delegate{
   var current=new ClassRecord();current.Parent10.Write(null);var call=new ExeClassQueryInvocation();
   Check(ControllerLookupContracts.Query2A33B0(current,new Field<ClassRecord>(),call)==0,"Null parent short-circuits unknown expected");
   Check(call.Returned&&call.Visited==1&&call.Native.At==0x2a33e2,"AL-only leaf return recorded");
   Check(ControllerLookupContracts.Query2A33B0(current,Field<ClassRecord>.Of(current),call)==0,"Current equals target does not make this parent-first query true");
  });
  Case(delegate{
   var expected=new ClassRecord();var first=new ClassRecord();var second=new ClassRecord();first.Parent10.Write(second);second.Parent10.Write(expected);var call=new ExeClassQueryInvocation();
   Check(ControllerLookupContracts.Query2A33B0(first,Field<ClassRecord>.Of(expected),call)==1&&call.Visited==2&&call.Native.At==0x2a33db,"Real two-level parent traversal returns true");
   second.Parent10.Write(null);Check(ControllerLookupContracts.Query2A33B0(first,Field<ClassRecord>.Of(expected),call)==0&&call.Native.At==0x2a33d1,"Recursive false uses outer false return");
   second.Parent10.Forget();Stop(delegate{ControllerLookupContracts.Query2A33B0(first,Field<ClassRecord>.Of(expected),call);},0x2a33b8);
   first.Parent10.Write(first);Stop(delegate{ControllerLookupContracts.Query2A33B0(first,Field<ClassRecord>.Of(expected),call);},0x2a33b4);Check(!call.Returned,"Cycle remains Unsupported, not false");
  });
  Case(delegate{
   var f=new Fixture();var other=new ClassRecord();f.Expected.Write(other);f.ClassInfo.Parent10.Write(null);
   Check(f.Run().Read(0)==null&&f.Call.ClassReturned&&!f.Call.FilterAccepted,"Different OWN import identity is not silently replaced by getter identity");
   Check(Object.ReferenceEquals(f.Call.ReturnedClass.Read(0),f.ClassInfo)&&Object.ReferenceEquals(f.Call.CapturedExpected.Read(0),other),"Getter and captured import remain separate sources");
   var parent=new ClassRecord();f.ClassInfo.Parent10.Write(parent);parent.Parent10.Write(other);
   Check(Object.ReferenceEquals(f.Run().Read(0),f.Controller)&&f.Call.Query.Returned&&f.Call.Query.Visited==1,"Inline parent then real query accept explicit OWN deeper class graph");
   Check(Object.ReferenceEquals(f.Node.Object18.Read(0),f.Controller),"Filter never replaces map identity");
  });
  Case(delegate{
   var f=new Fixture(false);Check(Object.ReferenceEquals(f.Run().Read(0),f.Controller),"Class filter can return before OWN mouse registration check");
   Stop(delegate{ControllerLookupContracts.BorrowCanonicalController(f.Call,f.Mouse);},0x22604);
   var first=f.Call.Result.Snapshot();f.Key.Write(8);f.Run();
   Check(Object.ReferenceEquals(first.Read(0),f.Controller)&&f.Call.Result.Read(0)==null&&!f.Call.ClassReturned,"Reused invocation resets diagnostics, retained result identity survives");
   Stop(delegate{ControllerLookupContracts.BorrowCanonicalController(f.Call,f.Mouse);},0x22604);
  });
  Console.WriteLine("{\"status\":\"passed-controller-lookup-own-integration\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"own_map_lookup_executed\":true,\"actual_class_query_executed\":true,\"observed_static_getter_bound\":true,\"same_controller_to_caller\":true,\"native_map_initialization_verified\":false,\"runtime_controller_vptr_observed\":false,\"native_lock_abi_verified\":false,\"movement_executed\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
