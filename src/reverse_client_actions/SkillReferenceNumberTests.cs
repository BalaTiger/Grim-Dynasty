using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Ten new component scenarios. Explicit OWN storage and ObjectID bindings;
// no SkillManager constructor, native heap or action submission is inferred.
public static class SkillReferenceNumberTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly SkillReferenceManagerState Skills;
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());
   var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   var callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));callbacks.Attach(Owner);
   Bridge=new GameMovementIdentityBridge(callbacks);Bridge.Bind(Owner,Move);Skills=SkillReferenceManagerState.ForOwner(Owner);
  }
 }
 static NavigationCrowdPointerVector<SkillMovedTarget>[] Vectors(SkillReferenceManagerState s){return new NavigationCrowdPointerVector<SkillMovedTarget>[]{s.Collections20,s.Collections258,s.Collections378,s.Collections2C8,s.Collections360};}
 static Field<uint>[] Offsets(SkillReferenceManagerState s){return new Field<uint>[]{s.Offset3BC,s.Offset3C0,s.Offset3C4,s.Offset3C8};}
 static void Empty(NavigationCrowdPointerVector<SkillMovedTarget> v){v.Begin00.Write(null);v.End08.Write(null);}
 static void EmptyAll(SkillReferenceManagerState s){foreach(var v in Vectors(s))Empty(v);}
 static NavigationCrowdPointerBuffer<SkillMovedTarget> Store(NavigationCrowdPointerVector<SkillMovedTarget> v,params SkillMovedTarget[] targets){
  var buffer=new NavigationCrowdPointerBuffer<SkillMovedTarget>(targets.Length);
  for(int n=0;n<targets.Length;n++)buffer.Cells[n].Write(targets[n]);
  v.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,0));v.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,targets.Length));return buffer;
 }
 static SkillMovedTarget Target(uint id){var target=new SkillMovedTarget();GameObjectIdState.BindSkill(target).ObjectId2C.Write(id);return target;}
 static uint Get(SkillReferenceManagerState s,uint id,SkillReferenceNumberInvocation c){return SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(s,Field<uint>.Of(id),c);}
 public static void Run(){scenarios=assertions=0;
  Test("All empty returns FFFFFFFF without consuming unknown ID offsets or capacity",delegate{
   var f=new Fixture();EmptyAll(f.Skills);var c=new SkillReferenceNumberInvocation();var key=new Field<uint>();
   Eq(UInt32.MaxValue,SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(f.Skills,key,c),"Known empty search is a miss");
   Check(c.Returned&&!c.Matched,"Miss is distinct from matched prefix");Eq(UInt32.MaxValue,c.ReturnedReference.Value,"Recorded scalar miss");Eq(0x531c1cu,c.Native.At,"Native common RET");
   Eq(0,c.SlotsRead,"No slot load on empty spans");Eq(0,c.GetterCalls,"No fabricated ID lookup");Unknown(key,"Caller input remains unknown");Unknown(c.CapturedObjectId,"Opaque ID capture remains unknown");
   foreach(var offset in Offsets(f.Skills))Unknown(offset,"Empty search does not need offsets");foreach(var vector in Vectors(f.Skills))Unknown(vector.Capacity10,"Capacity never consumed or initialized");
  });
  Test("Five-vector order and first duplicate ID within each vector",delegate{
   var f=new Fixture();var s=f.Skills;var vectors=Vectors(s);var offsets=Offsets(s);var targets=new SkillMovedTarget[5];
   for(int n=0;n<5;n++){targets[n]=Target(42);Store(vectors[n],null,Target(7),targets[n],Target(42));if(n>0)offsets[n-1].Write((uint)(n*100));}
   uint[] nativeOffsets={0x20,0x258,0x378,0x2c8,0x360};var c=new SkillReferenceNumberInvocation();
   for(int n=0;n<5;n++){
    Eq((uint)(n*100+2),Get(s,42,c),"First matching vector and local index2 win");Same(targets[n],c.MatchedTarget,"Exact matching target identity retained");
    Eq(nativeOffsets[n],c.MatchVectorOffset,"Native collection offset order");Eq(2u,c.MatchLocalIndex,"Second duplicate is not selected");Eq(3,c.SlotsRead,"Null plus mismatch plus first match only");Eq(2,c.GetterCalls,"Null skip and immediate match return");Eq(2,c.GetterReturns,"Each nonnull reaches real scalar getter");Empty(vectors[n]);
   }
   Eq(UInt32.MaxValue,Get(s,42,c),"No stale match when all segments later become empty");Check(c.Returned&&!c.Matched&&c.MatchedTarget==null,"Invocation reuse clears prior match identity");
  });
  Test("First match bypasses unknown later headers and reads live all-bit IDs",delegate{
   var f=new Fixture();var s=f.Skills;var target=Target(0);Store(s.Collections20,target);var c=new SkillReferenceNumberInvocation();
   Eq(0u,Get(s,0,c),"ObjectID0 is a valid match, not a no-skill shortcut");Same(target,c.MatchedTarget,"Match retains existing skill identity");Unknown(target.Virtual00,"Scalar ID lookup never reads primary vptr");
   GameObjectIdState.ForBoundIdentity(target,0).ObjectId2C.Write(0xfedcba98u);Eq(0u,Get(s,0xfedcba98u,c),"Live uint32 ID change is visible");Eq(0xfedcba98u,c.CapturedObjectId.Read(0),"All input ID bits retained");
   Unknown(s.Collections258.Begin00,"Short circuit does not manufacture next header");Unknown(s.Offset3BC,"Short circuit does not consume next offset");
   Missing(0x531a99,delegate{Get(s,0,c);},"Mismatch continues to actual next unknown span");Check(!c.Returned&&!c.Matched,"No stale success after failed reuse");
  });
  Test("Nonnull misses and all-null vectors ignore unknown offsets",delegate{
   var f=new Fixture();var s=f.Skills;var c=new SkillReferenceNumberInvocation();var vectors=Vectors(s);
   for(int n=0;n<5;n++)Store(vectors[n],null,Target((uint)(n+1)));
   Eq(UInt32.MaxValue,Get(s,99,c),"All real getter mismatches return FFFFFFFF");Eq(10,c.SlotsRead,"Both slots in all five vectors searched");Eq(5,c.GetterReturns,"Exactly five scalar getter returns");
   foreach(var offset in Offsets(s))Unknown(offset,"Mismatches do not read or initialize group offset");
   for(int n=0;n<5;n++)Store(vectors[n],null,null);
   Eq(UInt32.MaxValue,SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(s,new Field<uint>(),c),"All null slots can skip unknown key");Eq(10,c.SlotsRead,"Every null slot visited");Eq(0,c.GetterCalls,"No callback or getter for null");
  });
  Test("Unknown headers and slots stop at actual first consumer in every segment",delegate{
   uint[] subtraction={0x531a4a,0x531a99,0x531af2,0x531b52,0x531bb0};uint[] slotTests={0x531a68,0x531abb,0x531b1b,0x531b7b,0x531bdb};
   for(int n=0;n<5;n++){
    var f=new Fixture();var vectors=Vectors(f.Skills);for(int p=0;p<n;p++)Empty(vectors[p]);var c=new SkillReferenceNumberInvocation();
    Missing(subtraction[n],delegate{SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(f.Skills,new Field<uint>(),c);},"Unknown header consumed at SUB before key comparison");Eq(subtraction[n],c.Native.At,"Native stop recorded");Check(!c.Returned,"Unknown header cannot return miss");
    var buffer=Store(vectors[n],Target(8));buffer.Cells[0].Forget();
    Missing(slotTests[n],delegate{Get(f.Skills,8,c);},"Unknown slot consumed at TEST rather than MOV or skipped");Eq(0,c.GetterCalls,"Unknown slot cannot enter scalar getter");Check(!c.Returned&&!c.Matched,"Unknown slot is not a search miss");
   }
  });
  Test("Real ObjectID getter failure precedes unknown key comparison",delegate{
   uint[] compares={0x531a73,0x531ac6,0x531b26,0x531b86,0x531be6};
   for(int n=0;n<5;n++){
    var f=new Fixture();var s=f.Skills;EmptyAll(s);var target=new SkillMovedTarget();Store(Vectors(s)[n],target);var c=new SkillReferenceNumberInvocation();var unknownKey=new Field<uint>();
    Missing(0x03a740,delegate{SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(s,unknownKey,c);},"Unbound target stops getter before key");Eq(1,c.GetterCalls,"Actual nonnull getter entered");Eq(0,c.GetterReturns,"Unbound getter did not return");
    var id=GameObjectIdState.BindSkill(target);Missing(0x03a740,delegate{SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(s,unknownKey,c);},"Bound unknown2C also cannot become mismatch");
    id.ObjectId2C.Write(5);Missing(compares[n],delegate{SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(s,unknownKey,c);},"Only successful real getter reaches key consumer");Eq(1,c.GetterReturns,"Successful scalar prefix preserved");Check(!c.Returned&&!c.Matched,"Unknown key is not no-match");
    target.MarkReleased();Missing(0x03a740,delegate{Get(s,5,c);},"Released target cannot return a stale known ID");Eq(0,c.GetterReturns,"Lifetime failure is propagated");
   }
  });
  Test("Offsets consumed only on match with uint32 addition and FFFFFFFF ambiguity",delegate{
   uint[] additions={0x531c27,0x531c31,0x531c3b,0x531c45};
   for(int n=0;n<4;n++){
    var f=new Fixture();var s=f.Skills;EmptyAll(s);var target=Target(55);var vector=Vectors(s)[n+1];Store(vector,null,target);var offset=Offsets(s)[n];var c=new SkillReferenceNumberInvocation();
    Missing(additions[n],delegate{Get(s,55,c);},"Matching group captures offset at MOV and consumes it at ADD");Eq(additions[n],c.Native.At,"Unknown offset stops at ADD");Check(c.Matched&&!c.Returned,"ID match prefix is distinct from returning a reference");Same(target,c.MatchedTarget,"Failure retains actual matched identity");Eq(1u,c.MatchLocalIndex,"Failure retains index before offset");
    offset.Write(UInt32.MaxValue);Eq(0u,Get(s,55,c),"Max plus index1 wraps uint32");Check(c.Matched&&c.Returned,"Wrapped reference0 is a successful match");
    Store(vector,target);Eq(UInt32.MaxValue,Get(s,55,c),"Max plus index0 can return miss bit-pattern");Check(c.Matched&&c.Returned,"Same scalar as miss remains an actual match");
    Eq(UInt32.MaxValue,Get(s,56,c),"Actual mismatch returns same scalar");Check(!c.Matched&&c.Returned,"Diagnostic distinguishes miss from offset result");
   }
  });
  Test("Reversed span preserves unsigned count and unsupported pointer domains stop",delegate{
   var f=new Fixture();var s=f.Skills;var c=new SkillReferenceNumberInvocation();var buffer=Store(s.Collections20,Target(1),Target(31),Target(32));
   s.Collections20.Begin00.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,1));s.Collections20.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(buffer,0));
   Eq(0u,Get(s,31,c),"Negative span enters loop and uses begin-relative index0");Eq(1u,Get(s,32,c),"Negative span can reach next valid own slot");Eq(2,c.GetterReturns,"Real mismatching predecessor was inspected");
   Missing(0x531a64,delegate{Get(s,99,c);},"Reversed extent is not clamped; stops at actual out-of-storage load");Eq(2,c.GetterReturns,"Completed getter prefix retained before bad slot");Check(!c.Returned,"Out-of-domain extent is not a miss");
   var other=new NavigationCrowdPointerBuffer<SkillMovedTarget>(1);s.Collections20.End08.Write(new NavigationCrowdPointerAddress<SkillMovedTarget>(other,0));
   Missing(0x531a4a,delegate{Get(s,99,c);},"Cross-allocation subtraction is unsupported");s.Collections20.End08.Write(null);
   Missing(0x531a4a,delegate{Get(s,99,c);},"Mixed null and typed pointer is unsupported");
  });
  Test("Released owner vector storage and missing role do not become no-match",delegate{
   var f=new Fixture();var c=new SkillReferenceNumberInvocation();EmptyAll(f.Skills);f.Owner.Entity.MarkReleased();
   Missing(0x531a44,delegate{Get(f.Skills,1,c);},"Owner lifetime checked at first end header");
   f=new Fixture();EmptyAll(f.Skills);f.Skills.Collections20.MarkReleased();Missing(0x531a44,delegate{Get(f.Skills,1,c);},"Vector lifetime checked at first end header");
   f=new Fixture();var buffer=Store(f.Skills.Collections20,Target(1));buffer.MarkReleased();Missing(0x531a64,delegate{Get(f.Skills,1,c);},"Released storage fails at slot dereference");Check(!c.Returned,"Released storage cannot return an old result");
   Missing(0x531a30,delegate{SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(f.Bridge,new MoveCharacter(),Field<uint>.Of(1),c);},"Unbound Move role cannot select another manager");
  });
  Test("Search and existing inverse borrow identical notification storage and IDs",delegate{
   var f=new Fixture();var s=f.Skills;var notifications=SkillMovedManagerState.ForOwner(f.Owner);var vectors=Vectors(s);var offsets=Offsets(s);
   Same(s,SkillReferenceManagerState.ForMoveCharacter(f.Bridge,f.Move,0),"Same canonical manager through Move bridge");Same(notifications.Collections258,s.Collections258,"258 is existing notification vector");Same(notifications.Collections360,s.Collections360,"360 is existing notification vector");
   var targets=new SkillMovedTarget[5];for(int n=0;n<5;n++){targets[n]=Target((uint)(700+n));Store(vectors[n],targets[n]);if(n>0)offsets[n-1].Write((uint)(n*10));}
   var c=new SkillReferenceNumberInvocation();
   for(int n=0;n<5;n++){
    uint reference=SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(f.Bridge,f.Move,Field<uint>.Of((uint)(700+n)),c);
    Eq((uint)(n*10),reference,"Search returns reference in explicit nonoverlapping groups");Same(targets[n],c.MatchedTarget,"Search retains original target, not a copied ID record");
    Eq((uint)(700+n),SkillReferenceContracts.GetSkillId531C50(s,reference,new Invocation()),"Existing inverse consumes same state and scalar ID");
   }
   GameObjectIdState.ForBoundIdentity(targets[1],0).ObjectId2C.Write(900);Eq(10u,Get(s,900,c),"Live notification target ID visible in search");Eq(900u,SkillReferenceContracts.GetSkillId531C50(s,10,new Invocation()),"Same live ID visible in existing inverse");
   Store(notifications.Collections360,Target(999));Eq(40u,Get(s,999,c),"Replacing shared notification storage immediately affects search");Unknown(s.Collections360.Capacity10,"Composition does not add a capacity requirement");
   Missing(0x531a30,delegate{SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(f.Bridge,new MoveCharacter(),Field<uint>.Of(999),c);},"Wrong role fails before new invocation can search");
   Check(!c.Returned&&!c.Matched&&c.ReturnedReference==null&&c.MatchedTarget==null,"Bridge failure clears prior success diagnostics");Eq(0x531a30u,c.Native.At,"Wrong role stop recorded for current invocation");
   Eq(40u,SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(f.Bridge,f.Move,Field<uint>.Of(999),c),"Valid existing bridge can search again");f.Owner.Entity.MarkReleased();
   Missing(0x531a30,delegate{SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(f.Bridge,f.Move,Field<uint>.Of(999),c);},"Released canonical owner fails before search");
   Check(!c.Returned&&!c.Matched&&c.ReturnedReference==null,"Released owner bridge failure cannot retain returned reference");Eq(0,c.GetterCalls,"Released owner did not enter a getter");
  });
 }
}

public static class SkillReferenceNumberValidationProgram {
 public static int Main(string[] args){
  try{
   if(args.Length!=0)throw new ArgumentException("This fixed component program takes no arguments");SkillReferenceNumberTests.Run();
   Console.WriteLine("{\"status\":\"passed-new-skill-reference-number-components-only\",\"scenarios\":"+SkillReferenceNumberTests.Scenarios+",\"assertions\":"+SkillReferenceNumberTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"system_library_loaded\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"physical_mouse\":false,\"action_submitted\":false}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}
 }
}
