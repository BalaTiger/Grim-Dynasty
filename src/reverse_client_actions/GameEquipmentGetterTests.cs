using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// OWN observed backing only. No native initialization, ObjectManager lookup,
// equipment classification, animation success or original execution is claimed.
public static class GameEquipmentGetterTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly GameMovementCharacterState Owner=new GameMovementCharacterState(new EntityMovementState(new object()));
  public readonly GameEquipmentManagerState Manager;public readonly GameEquipmentStorage Storage;
  public Fixture(int capacity){Manager=GameEquipmentManagerState.ForOwner(Owner);Storage=new GameEquipmentStorage(capacity);Manager.Begin08.Write(new GameEquipmentPointer(Storage,0));Manager.End10.Write(new GameEquipmentPointer(Storage,capacity*12L));}
  public GameEquipmentRecord R(int index){return Storage.Record(index);}
 }
 static Field<uint> Left(Fixture f,GameEquipmentGetterInvocation c){return GameEquipmentGetterContracts.GetWeaponIdLeft284720(f.Manager,c);}
 static Field<uint> Right(Fixture f,GameEquipmentGetterInvocation c){return GameEquipmentGetterContracts.GetWeaponIdRight284670(f.Manager,c);}
 public static void Run(){
  scenarios=assertions=0;
  Test("shared owner binding does not initialize vector",delegate{
   var owner=new GameMovementCharacterState(new EntityMovementState(new object()));var manager=GameEquipmentManagerState.ForOwner(owner);Same(manager,GameEquipmentManagerState.ForOwner(owner),"same embedded manager");Same(owner,manager.Owner,"same full owner");Unknown(manager.Begin08,"begin remains Unknown");Unknown(manager.End10,"end remains Unknown");
   var c=new GameEquipmentGetterInvocation();Missing(0x284733,delegate{GameEquipmentGetterContracts.GetWeaponIdLeft284720(manager,c);},"opaque initial loads consumed by SUB");Check(c.Used&&!c.Returned,"failure is not a return");Eq(0,c.VisitedRecords,"no record inspected");
  });
  Test("explicit null empty vector",delegate{
   var f=new Fixture(0);f.Manager.Begin08.Write(null);f.Manager.End10.Write(null);var l=new GameEquipmentGetterInvocation();var r=new GameEquipmentGetterInvocation();Eq(0u,Left(f,l).Read(0),"left empty code");Eq(0u,Right(f,r).Read(0),"right empty code");Eq(0L,l.InitialCount.Read(0),"signed count zero");Check(l.Returned&&r.Returned,"both normal returns");Eq(0,l.VisitedRecords,"no record");Eq(0,l.FreshEnds.Count,"no loop bound reread");Unknown(l.CapturedBegin,"second begin load not reached");Eq(0x284798u,l.Native.At,"left RET");Eq(0x2846f6u,r.Native.At,"right RET");
  });
  Test("signed division truncates sub-stride ranges before dereference",delegate{
   var f=new Fixture(1);f.Manager.End10.Write(new GameEquipmentPointer(f.Storage,11));var l=new GameEquipmentGetterInvocation();Eq(0u,Left(f,l).Read(0),"positive11 truncates zero");Eq(0,l.VisitedRecords,"unknown record unused");f.Manager.Begin08.Write(new GameEquipmentPointer(f.Storage,11));f.Manager.End10.Write(new GameEquipmentPointer(f.Storage,0));var r=new GameEquipmentGetterInvocation();Eq(0u,Right(f,r).Read(0),"negative11 truncates zero");Eq(0L,r.InitialCount.Read(0),"signed truncation not floor");Unknown(r.CapturedBegin,"zero count avoids unaligned record access");
  });
  Test("left first blocked match stops while right returns its full DWORD",delegate{
   var f=new Fixture(2);f.R(0).Location04.Write(1);f.R(0).Byte09.Write(0x80);var l=new GameEquipmentGetterInvocation();Eq(0u,Left(f,l).Read(0),"blocked first left returns zero");Eq(1,l.VisitedRecords,"does not search later records");Eq(0,l.FreshEnds.Count,"no fresh end after match");Unknown(f.R(0).Id00,"left did not need unknown id");f.R(0).Id00.Write(0x80000001u);var r=new GameEquipmentGetterInvocation();Eq(0x80000001u,Right(f,r).Read(0),"nonzero byte path returns full id bits");Eq(0x284707u,r.Native.At,"right location1 RET");
  });
  Test("left exact location and byte widths with untouched later record",delegate{
   var f=new Fixture(2);f.R(0).Location04.Write(1);f.R(0).Byte09.Write(0);f.R(0).Id00.Write(UInt32.MaxValue);var c=new GameEquipmentGetterInvocation();Eq(UInt32.MaxValue,Left(f,c).Read(0),"full DWORD opaque copy");Eq(1,c.VisitedRecords,"first match");Unknown(f.R(1).Location04,"later record untouched");Eq(0x2847b1u,c.Native.At,"left id RET");
  });
  Test("right location zero never consumes byte09",delegate{
   var f=new Fixture(1);f.R(0).Location04.Write(0);f.R(0).Id00.Write(123);var c=new GameEquipmentGetterInvocation();Eq(123u,Right(f,c).Read(0),"location zero id");Unknown(f.R(0).Byte09,"unused byte stays Unknown");Eq(0x284718u,c.Native.At,"right location0 RET");Eq(0,c.FreshEnds.Count,"no next-bound load");
  });
  Test("right nonmatching location1 rereads end and retains captured begin",delegate{
   var f=new Fixture(2);f.R(0).Location04.Write(1);f.R(0).Byte09.Write(0);f.R(1).Location04.Write(0);f.R(1).Id00.Write(456);var begin=f.Manager.Begin08.Read(0);var end=f.Manager.End10.Read(0);var c=new GameEquipmentGetterInvocation();Eq(456u,Right(f,c).Read(0),"right skips left-only entry");Eq(2,c.VisitedRecords,"two visited records");Eq(1,c.FreshEnds.Count,"one fresh bound");Same(end,c.FreshEnds[0].Read(0),"fresh bound bits");Same(begin,c.CapturedBegin.Read(0),"begin is the captured pointer");Eq(1u,c.Index,"32-bit increment");Eq(2L,c.LastCount.Read(0),"recomputed stride count");Unknown(f.R(0).Id00,"skipped id unused");Unknown(f.R(1).Byte09,"location0 byte unused");
  });
  Test("left DWORD location does not alias its low byte",delegate{
   var f=new Fixture(2);f.R(0).Location04.Write(0x10001);f.R(1).Location04.Write(1);f.R(1).Byte09.Write(0);f.R(1).Id00.Write(77);var c=new GameEquipmentGetterInvocation();Eq(77u,Left(f,c).Read(0),"0x10001 is not location1");Eq(1,c.FreshEnds.Count,"nonmatch bound reread");Unknown(f.R(0).Byte09,"nonmatch byte unused");Unknown(f.R(0).Id00,"nonmatch id unused");
  });
  Test("Unknown ID is a normal opaque return snapshot",delegate{
   var f=new Fixture(1);f.R(0).Location04.Write(0);var c=new GameEquipmentGetterInvocation();var result=Right(f,c);Check(c.Returned,"getter returns before caller consumes id");Unknown(result,"returned id Unknown");Unknown(c.Result,"diagnostic result Unknown");f.R(0).Id00.Write(12);Unknown(result,"later source publication does not change return register");var known=Right(f,new GameEquipmentGetterInvocation());f.R(0).Id00.Write(13);Eq(12u,known.Read(0),"known id also snapshots");
  });
  Test("Unknown fields fail only at their actual consumer",delegate{
   var f=new Fixture(1);Missing(0x284764,delegate{Left(f,new GameEquipmentGetterInvocation());},"left CMP consumes location");Missing(0x2846b9,delegate{Right(f,new GameEquipmentGetterInvocation());},"right MOV preserves then TEST consumes location");f.R(0).Location04.Write(1);Missing(0x2847a0,delegate{Left(f,new GameEquipmentGetterInvocation());},"left consumes matching byte");Missing(0x2846c2,delegate{Right(f,new GameEquipmentGetterInvocation());},"right consumes matching byte");f.R(0).Location04.Write(2);var c=new GameEquipmentGetterInvocation();Eq(0u,Right(f,c).Read(0),"nonmatching record reaches observed zero return");Eq(1,c.FreshEnds.Count,"end reread even for last nonmatch");Unknown(f.R(0).Id00,"unused id remains Unknown");
  });
  Test("explicit pointer domain and accessed extent",delegate{
   var f=new Fixture(1);f.Manager.End10.Write(new GameEquipmentPointer(new GameEquipmentStorage(1),12));Missing(0x284733,delegate{Left(f,new GameEquipmentGetterInvocation());},"no cross-backing address invention");f.Manager.End10.Write(null);Missing(0x284683,delegate{Right(f,new GameEquipmentGetterInvocation());},"one null pointer is not empty");f.Manager.Begin08.Write(new GameEquipmentPointer(f.Storage,1));f.Manager.End10.Write(new GameEquipmentPointer(f.Storage,13));Missing(0x284764,delegate{Left(f,new GameEquipmentGetterInvocation());},"unaligned actual record");f.Manager.Begin08.Write(new GameEquipmentPointer(f.Storage,12));f.Manager.End10.Write(new GameEquipmentPointer(f.Storage,24));Missing(0x2846b4,delegate{Right(f,new GameEquipmentGetterInvocation());},"out-of-extent dereference");
   f.Manager.Begin08.Write(new GameEquipmentPointer(f.Storage,0));f.Manager.End10.Write(new GameEquipmentPointer(f.Storage,12000));f.R(0).Location04.Write(0);f.R(0).Id00.Write(91);Eq(91u,Right(f,new GameEquipmentGetterInvocation()).Read(0),"first match does not prevalidate unused end extent");
  });
  Test("released lifetime and negative count never become empty defaults",delegate{
   var f=new Fixture(2);f.Manager.Begin08.Write(new GameEquipmentPointer(f.Storage,12));f.Manager.End10.Write(new GameEquipmentPointer(f.Storage,0));f.R(1).Location04.Write(0);f.R(1).Id00.Write(44);var c=new GameEquipmentGetterInvocation();Eq(44u,Right(f,c).Read(0),"negative nonzero count still enters first native iteration");Eq(-1L,c.InitialCount.Read(0),"signed initial count");f.R(1).Location04.Write(2);Missing(0x2846b4,delegate{Right(f,new GameEquipmentGetterInvocation());},"unsigned loop comparison cannot fabricate beyond extent");f.Storage.MarkReleased();Missing(0x284733,delegate{Left(f,new GameEquipmentGetterInvocation());},"released backing before pointer arithmetic");var g=new Fixture(0);g.Owner.Entity.MarkReleased();Missing(0x284675,delegate{Right(g,new GameEquipmentGetterInvocation());},"same owner lifetime checked");
  });
 }
}
