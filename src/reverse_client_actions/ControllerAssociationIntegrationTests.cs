using System;
using GrimDynasty.ReverseClient.Actions;
using SharedFixture=ControllerPlayerLookupIntegrationTestProgram.Fixture;

// Reuses the existing fixture only; its old Main is never called. All hot
// publications are explicit OWN inputs. The store is entered conditionally at
// 052674, not by pretending the full Character constructor has completed.
public static class ControllerAssociationIntegrationTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Case(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static void Stop(uint address,Action body){try{body();}catch(MissingContract error){Check(error.Address==address,"Unsupported "+error.Address.ToString("x")+" expected "+address.ToString("x"));return;}throw new Exception("Expected Unsupported "+address.ToString("x"));}
 static void Invalid(Action body){try{body();}catch(ArgumentException){Check(true,"Invalid disk graph rejected before publication");return;}throw new Exception("Expected ArgumentException");}
 static void Unknown<T>(Field<T> field,string why){bool unknown=false;try{field.Read(0);}catch(MissingContract){unknown=true;}Check(unknown,why);}
 static void ActionsUnchanged(SharedFixture f){Same(f.Current,f.Controller.CurrentA0.Read(0),"No current action replacement");Same(f.Pending,f.Controller.PendingA8.Read(0),"No pending action replacement");}
 sealed class Association {
  public readonly SharedFixture F=new SharedFixture();
  public readonly AddressSymbol<ClassRecord> ExpectedA5C9F0=new AddressSymbol<ClassRecord>(0xa5c9f0);
  public readonly ClassRecord BaseA5C9F0=new ClassRecord(),RecordA5BF10=new ClassRecord(),RecordA5C990=new ClassRecord(),RecordA5C9C0=new ClassRecord();
  public readonly ControllerPlayerAssociatedLookupBoundaries Boundaries;
  public readonly Invocation Getter=new Invocation(),Singleton=new Invocation();
  public readonly Field<uint> CapturedKey=new Field<uint>();
  public GameAssociatedLookupInvocation Call=new GameAssociatedLookupInvocation();
  public object Result;
  public Association(){Boundaries=new ControllerPlayerAssociatedLookupBoundaries(F.ControllerLookup,ExpectedA5C9F0);}
  public object Run(){
   CapturedKey.Forget();Singleton.At=0;Result=null;Call=new GameAssociatedLookupInvocation();
   CapturedKey.Write(ControllerLookupContracts.CharacterGetControllerId62310(F.Bridge,F.Move,Getter));
   var manager=GameReachedGoalEngineContracts.GetObjectManager01C040(F.Engine,Singleton);
   Result=GameAssociatedLookupContracts.Lookup077D60(manager as GameObjectManagerState,CapturedKey,Boundaries,Call);return Result;
  }
  public void SelectObservedDiskParents(){
   // Explicit initialization from the four saved disk edges. It does not
   // observe the loaded graph or execute its native initialization writers.
   ExpectedA5C9F0.Write(BaseA5C9F0);Boundaries.InitializeDiskParentChain(RecordA5BF10,RecordA5C990,RecordA5C9C0);
  }
  public void ActualLookupReturned(){
   Check(Getter.At==0x62316&&Singleton.At==0x01c103,"Actual62310 and hot singleton returned");
   Check(CapturedKey.Read(0)==7&&F.ControllerFields.ObjectId30.Read(0)==0x1234,"Controller key and role key remain distinct");
   Check(Call.LookupReturned&&Call.Lookup.Returned&&Call.Lookup.HashedBytes==4,"Actual077D60 uses existing four-byte hash lookup");
   Check(Call.Lookup.OwnLockExited&&F.Lock.OwnOutstandingEnters==0,"Map lock released before class consumption");
  }
 }
 static void ConfigureOwnAliveTrue(SharedFixture f){
  var alive=f.BindAlive();alive.Byte24B9.Write(0);alive.Flags24B0.Write(0);
  GameObjectIdState.BindEntity(f.Owner.Entity).ObjectId2C.Write(0x77);
  var identity=new NavigationView();var video=GamePlayerVideoState.Bind(identity,identity,new Field<object>());video.Video19B0.Write(null);f.Mouse.GameEngineA66288.Write(identity);
 }
 public static int Main(){try{
  Case("Reached word store writes both original adjacent byte cells",delegate{
   var f=new SharedFixture();var state=CharacterMouseConstructor242AState.Bind(f.PlayerLookup,f.Role);
   Same(state,CharacterMouseConstructor242AState.Bind(f.PlayerLookup,f.Role),"One state for the original role");Same(f.Role,state.Role,"Original mouse role");Same(f.Owner,state.Owner,"Original full owner");Same(f.Role.Byte242A,state.Byte242A,"242A is the caller's exact field");Same(f.Owner.Entity.Virtual00,f.Role.Virtual00,"Original vptr cell remains shared");
   Unknown(state.Byte242A,"Binding does not initialize242A");Unknown(state.Byte242B,"Binding does not initialize242B");
   state.Byte242A.Write(0xfe);state.Byte242B.Write(0xab);f.Role.Byte4A70.Write(0x5c);var invocation=new Invocation();
   CharacterMouseConstructorContracts.ApplyReachedWordZero052674(state,invocation);
   Check(invocation.At==0x05267e&&state.Byte242A.Read(0)==0&&state.Byte242B.Read(0)==0,"Actual reached word-zero slice completes at next instruction");
   Check(f.Role.Byte4A70.Read(0)==0x5c&&f.Move.AssociatedId16C0.Read(0)==7,"Unrelated role gate and controller ID unchanged");Same(GameMovementPrimaryTable.Player,f.Owner.Entity.Virtual00.Read(0),"No vptr replacement");
   f.Role.Byte242A.Write(9);Check(state.Byte242A.Read(0)==9&&state.Byte242B.Read(0)==0,"External242A change is live and does not overwrite adjacent byte");
   state.Byte242B.Write(8);Check(f.Role.Byte242A.Read(0)==9,"Adjacent external write does not synchronize242A");
   CharacterMouseConstructorContracts.ApplyReachedWordZero052674(state,invocation);Check(state.Byte242A.Read(0)==0&&state.Byte242B.Read(0)==0,"A second explicitly reached store rewrites both bytes");ActionsUnchanged(f);
  });
  Case("Wrong role binding and released owner do not publish zeros",delegate{
   var f=new SharedFixture();var state=CharacterMouseConstructor242AState.Bind(f.PlayerLookup,f.Role);
   var rogue=new MouseRoleState(f.Move);Stop(0,delegate{CharacterMouseConstructor242AState.Bind(f.PlayerLookup,rogue);});Unknown(rogue.Byte242A,"Rejected parallel role left unknown");
   state.Byte242A.Write(3);state.Byte242B.Write(4);f.Owner.Entity.MarkReleased();var invocation=new Invocation();
   Stop(0x052674,delegate{CharacterMouseConstructorContracts.ApplyReachedWordZero052674(state,invocation);});
   Check(invocation.At==0x052674&&state.Byte242A.Read(0)==3&&state.Byte242B.Read(0)==4,"Lifetime guard precedes both writes");ActionsUnchanged(f);
  });
  Case("Unknown controller key is consumed before singleton",delegate{
   var a=new Association();a.F.Move.AssociatedId16C0.Forget();a.F.Engine.Critical421398.Forget();
   Stop(0x62310,delegate{a.Run();});Check(a.Singleton.At==0&&!a.Call.LookupReturned,"No singleton or map call before unknown62310 read");Unknown(a.CapturedKey,"No zero/default controller key");
   a.F.Move.AssociatedId16C0.Write(7);Stop(0x01c056,delegate{a.Run();});Check(a.Getter.At==0x62316&&a.CapturedKey.Read(0)==7,"Real nonzero getter returns before singleton failure");ActionsUnchanged(a.F);
  });
  Case("Concrete slot returns actual class before unknown expected symbol",delegate{
   var a=new Association();a.F.ControllerLookup.ExpectedImport2D7198.Forget();
   Stop(0x077d82,delegate{a.Run();});a.ActualLookupReturned();
   Check(a.Call.PrimaryCaptured&&a.Call.ClassReturned&&!a.Call.Returned,"Real slot0 returned before A5C9F0 comparison");Same(a.F.Controller,a.Call.CapturedObject.Read(0),"Captured original Controller");Same(a.F.ControllerClass,a.Call.ReturnedClass.Read(0),"Actual fixed A5C180 class, not expected symbol substitution");
   Stop(0,delegate{a.ExpectedA5C9F0.Read(0);});Unknown(a.F.ControllerClass.Parent10,"Unknown expected symbol cannot invent parent");Unknown(a.F.ControllerLookup.ExpectedImport2D7198,"Associated adapter does not consume unrelated EXE IAT expectation");ActionsUnchanged(a.F);
  });
  Case("Unknown parent remains a stop; explicit disk graph uses real helper",delegate{
   var a=new Association();a.ExpectedA5C9F0.Write(a.BaseA5C9F0);
   Stop(0x077d8b,delegate{a.Run();});a.ActualLookupReturned();Check(a.Call.ClassReturned&&!a.Call.DerivedEntered&&!a.Call.Returned,"Unknown Parent10 stops after actual class return");Unknown(a.Call.Parent,"Parent snapshot remains unknown");
   a.F.ControllerClass.Parent10.Write(a.RecordA5BF10);Stop(0x591484,delegate{a.Run();});Check(a.Call.DerivedEntered&&!a.Call.DerivedReturned&&!a.Call.Returned,"Real parent helper stops at next unknown parent");
   a.SelectObservedDiskParents();Same(a.F.Controller,a.Run(),"Explicit disk-parent initialization returns original Controller");a.ActualLookupReturned();
   Check(a.Call.DerivedEntered&&a.Call.DerivedReturned&&a.Call.Derived.At==0x5914a2&&a.Call.Returned,"Actual591480 accepted saved four-edge graph");Same(a.F.Controller,a.Call.Result.Read(0),"No replacement/cast-adjusted identity");Unknown(a.BaseA5C9F0.Parent10,"Successful expected-class match never reads or initializes base parent");
   a.RecordA5C9C0.Parent10.Write(null);Same(null,a.Run(),"Later parent mutation is read by actual helper");Check(a.Call.DerivedReturned&&a.Call.Returned,"Changed graph really rejects association");Same(null,a.RecordA5C9C0.Parent10.Read(0),"Lookup does not silently restore disk graph");ActionsUnchanged(a.F);
  });
  Case("Disk graph initialization rejects aliases before any edge write",delegate{
   var a=new Association();var before=new ClassRecord();a.F.ControllerClass.Parent10.Write(before);a.RecordA5BF10.Parent10.Write(before);a.RecordA5C990.Parent10.Write(before);a.RecordA5C9C0.Parent10.Write(before);
   Stop(0,delegate{a.Boundaries.InitializeDiskParentChain(a.RecordA5BF10,a.RecordA5C990,a.RecordA5C9C0);});
   Same(before,a.F.ControllerClass.Parent10.Read(0),"Unknown expected symbol leaves first edge unchanged");Same(before,a.RecordA5C9C0.Parent10.Read(0),"Unknown symbol leaves final edge unchanged");
   a.ExpectedA5C9F0.Write(a.BaseA5C9F0);
   Invalid(delegate{a.Boundaries.InitializeDiskParentChain(a.RecordA5BF10,a.RecordA5BF10,a.RecordA5C9C0);});
   Invalid(delegate{a.Boundaries.InitializeDiskParentChain(a.RecordA5BF10,null,a.RecordA5C9C0);});
   Invalid(delegate{a.Boundaries.InitializeDiskParentChain(a.RecordA5BF10,a.RecordA5C990,a.BaseA5C9F0);});
   Same(before,a.F.ControllerClass.Parent10.Read(0),"Invalid graph performs no first-edge publication");Same(before,a.RecordA5BF10.Parent10.Read(0),"Invalid graph does not partially write BF10");Same(before,a.RecordA5C990.Parent10.Read(0),"Invalid graph does not partially write C990");Same(before,a.RecordA5C9C0.Parent10.Read(0),"Invalid graph does not partially write C9C0");
   a.SelectObservedDiskParents();Same(a.RecordA5BF10,a.F.ControllerClass.Parent10.Read(0),"Saved C180 to BF10 edge");Same(a.RecordA5C990,a.RecordA5BF10.Parent10.Read(0),"Saved BF10 to C990 edge");Same(a.RecordA5C9C0,a.RecordA5C990.Parent10.Read(0),"Saved C990 to C9C0 edge");Same(a.BaseA5C9F0,a.RecordA5C9C0.Parent10.Read(0),"Saved C9C0 to C9F0 edge");Unknown(a.BaseA5C9F0.Parent10,"Terminal base parent is outside the write set");ActionsUnchanged(a.F);
  });
  Case("Unknown and changed vptr use original cell with no fabricated class",delegate{
   var a=new Association();a.SelectObservedDiskParents();var primary=a.F.Mouse.Object(a.F.Controller,0).Virtual00;primary.Forget();
   Stop(0x077d79,delegate{a.Run();});a.ActualLookupReturned();Check(a.Call.PrimaryCaptured&&!a.Call.ClassReturned,"Unknown captured vptr stops at virtual call");Unknown(a.Call.CapturedPrimary,"Captured vptr stays opaque");
   primary.Write(new object());Stop(0x077d79,delegate{a.Run();});Check(!a.Call.ClassReturned,"Unbound concrete table is not accepted as ControllerPlayer");
   primary.Write(ControllerLookupPrimaryTable.ObservedControllerPlayer);Same(a.F.Controller,a.Run(),"Later call consumes changed live vptr through concrete adapter");Same(primary,a.F.Mouse.Object(a.F.Controller,0).Virtual00,"One original vptr cell");ActionsUnchanged(a.F);
  });
  Case("Map mutation and key mutation are reread; unknown object is not null",delegate{
   var a=new Association();a.SelectObservedDiskParents();Same(a.F.Controller,a.Run(),"Initial disk-initialized association");
   a.F.ControllerNode.Object18.Forget();Stop(0x077d6e,delegate{a.Run();});a.ActualLookupReturned();Check(!a.Call.PrimaryCaptured&&!a.Call.Returned,"Unknown Object18 consumed after real map return");Unknown(a.Call.CapturedObject,"Opaque map result preserved");
   a.F.ControllerNode.Object18.Write(null);Same(null,a.Run(),"Known null map value is a distinct real early return");Check(a.Call.Returned&&!a.Call.ClassReturned&&a.Call.Native.At==0x077dae,"Null bypasses all class work");
   a.F.ControllerNode.Object18.Write(a.F.Controller);a.F.Move.AssociatedId16C0.Write(8);Same(null,a.Run(),"Changed full-character key makes real map miss");Check(a.CapturedKey.Read(0)==8&&a.F.ControllerFields.ObjectId30.Read(0)==0x1234,"Neither old key7 nor role key substituted");
   a.F.ControllerNode.Key10.Write(8);Same(a.F.Controller,a.Run(),"Updated node key is observed without changing object identity");Check(a.F.Lock.OwnOutstandingEnters==0,"All real map calls released lock");ActionsUnchanged(a.F);
  });
  Case("Released bucket tail preserves actual failure and OWN cleanup",delegate{
   var a=new Association();a.SelectObservedDiskParents();a.F.RoleNode.MarkReleased();
   Stop(0x019dde,delegate{a.Run();});Check(!a.Call.LookupReturned&&!a.Call.PrimaryCaptured&&!a.Call.Returned,"Released tail is read before successful key comparison");
   Check(a.Call.Lookup.OwnCleanupReleased&&a.F.Lock.OwnOutstandingEnters==0,"OWN exception cleanup releases the acquired map lock");ActionsUnchanged(a.F);
  });
  Case("Reached word store advances real Alive caller to next unknown state",delegate{
   var a=new Association();a.SelectObservedDiskParents();var f=a.F;f.CaptureController();
   Same(f.Controller,f.CapturedController,"Widget Controller comes from actual62310 then EXE1BCD0");Same(f.CapturedController,a.Run(),"Separate077D60 association agrees with original Controller identity");
   ConfigureOwnAliveTrue(f);var state=CharacterMouseConstructor242AState.Bind(f.PlayerLookup,f.Role);var store=new Invocation();CharacterMouseConstructorContracts.ApplyReachedWordZero052674(state,store);
   Stop(0x15e292,delegate{f.CallCaptured(f.AliveBoundaries);});
   Check(f.AliveBoundaries.LastLookup.Returned&&f.AliveBoundaries.LastAlive.Returned&&f.AliveBoundaries.LastAlive.Result.Read(0)==1,"Actual128530 and PlayerAlive returned under explicit OWN inputs");
   Same(f.Move,f.AliveBoundaries.LastLookup.Result.Read(0),"Same canonical role");Same(f.Controller,f.Caller.CapturedController.Read(0),"Same associated Controller enters widget call");
   Check(f.Caller.ReachedCall&&!f.Caller.ActionReturned&&f.Caller.Mouse.At==0x15e292,"242A zero advances only to unknown selected-state count");Unknown(f.MouseController.Count2F0,"No active state default");Unknown(f.Position.Q0,"Position copied opaquely, no synthetic region");
   Check(f.InputId.Read(0)==91&&f.MouseController.Byte438.Read(0)==7&&state.Byte242B.Read(0)==0,"No false-gate cleanup or adjacent byte repair");
   f.Role.Byte242A.Write(1);f.CallCaptured(f.AliveBoundaries);
   Check(f.Caller.ActionReturned&&f.Caller.ActionResult.Read(0)==0&&f.InputId.Read(0)==0&&f.MouseController.Byte438.Read(0)==0&&f.MouseController.Dword43C.Read(0)==0,"Later external242A change drives actual cleanup on the same fields");
   Unknown(f.MouseController.Count2F0,"External gate bypasses, rather than initializes, active state");Check(state.Byte242A.Read(0)==1&&state.Byte242B.Read(0)==0,"Constructor zero was not frozen or re-applied");ActionsUnchanged(f);
  });
  Console.WriteLine("{\"status\":\"passed-controller-association-own-integration\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"actual_associated_lookup\":true,\"actual_word_store\":true,\"canonical_identity_shared\":true,\"actual_alive_prefix\":true,\"disk_parent_edges_initialized\":true,\"old_suite_executed\":false,\"native_constructor_completed\":false,\"runtime_class_graph_verified\":false,\"movement_executed\":false,\"physical_mouse_verified\":false}");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
