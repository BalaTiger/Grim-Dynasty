using System;
using System.Collections.Generic;
using GrimDynasty.CombatRuntime;
using GrimDynasty.ReverseClient.Actions;

public static class GameDurationDamageManagerTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint address,Action action,string why){try{action();}catch(MissingContract e){Equal(address,e.Address,why);return;}throw new Exception("Expected MissingContract: "+why);}
 static void Unknown<T>(Field<T> field){Missing(0,delegate{field.Read(0);},"Unwritten field stays unknown");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly NativeAttributeView Attributes=new NativeAttributeView();
  public readonly GamePlayerRunSpeedCapState Cap;
  public readonly GameDurationDamageManagerState State;
  public readonly GameDurationDamageConstructorInvocation Ctor=new GameDurationDamageConstructorInvocation();
  public readonly GameDurationDamageLookupInvocation Call=new GameDurationDamageLookupInvocation();
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);Bridge.Bind(Owner,Move);Cap=GamePlayerRunSpeedCapState.Bind(Bridge,Move,Attributes);
   Missing(0x123456,delegate{GameDurationDamageManagerState.ForBound(Cap,0x123456);},"Lookup cannot invent Duration binding");State=GameDurationDamageManagerState.Bind(Cap);
  }
  public void Construct(){Same(State,GameDurationDamageManagerContracts.Construct2790A0(State,Move,Ctor),"Constructor returns original this");}
  public float Get(Field<uint> type){return GameDurationDamageManagerContracts.GetFixedDamage279D10(State,Move,type,Call);}
  public float Get(uint type){return Get(Field<uint>.Of(type));}
  public GameDurationDamageRecords Records(int count){var records=new GameDurationDamageRecords(count);State.Begin60.Write(records.Pointer(0));State.End68.Write(records.Pointer(records.ByteLength));State.Capacity70.Write(records.Pointer(records.ByteLength));return records;}
 }
 static void Write(GameDurationWrite write,uint address,uint offset,int bytes){Equal(address,write.Address,"Store instruction");Equal(offset,write.Offset,"Store displacement");Equal(bytes,write.Bytes,"Store width");}
 public static void Run(){scenarios=assertions=0;
  Test("Duration binding borrows the sole cap owner without initializing fields",delegate{
   var f=new Fixture();Same(f.State,GameDurationDamageManagerState.Bind(f.Cap),"Idempotent same-cap bind");Same(f.State,GameDurationDamageManagerState.ForBound(f.Cap,1),"Retrieve original state");Same(f.Cap,f.State.Cap,"One original cap owner");Same(f.Attributes,f.State.Cap.AttributesF78,"No copied attribute storage");Unknown(f.State.Virtual00);Unknown(f.State.Head30);Unknown(f.State.Begin60);Unknown(f.State.End68);Unknown(f.Cap.Minimum5018);Unknown(f.Owner.Entity.Virtual00);
  });
  Test("019520 normal OWN allocation returns original RAX and only four observed stores",delegate{
   var call=new GameDurationSentinelInvocation();var node=GameDurationDamageManagerContracts.AllocateSentinel019520(call);Same(node,call.Allocated,"Allocator identity retained");Same(node,call.ReturnedRax,"Helper is not void");Same(node,node.Left00.Read(0),"Left self pointer");Same(node,node.Parent08.Read(0),"Parent self pointer");Same(node,node.Right10.Read(0),"Right self pointer");Equal((ushort)0x0101,node.Flags18.Read(0),"Two native flag bytes");Check(call.OwnAllocationAttempted&&call.OwnAllocationReturned&&call.Returned,"Explicit normal managed allocation path");Equal(0x019558u,call.Native.At,"Helper RET");Equal(4,call.Writes.Count,"Only observed writes");Write(call.Writes[0],0x019533,0,8);Write(call.Writes[1],0x01953f,8,8);Write(call.Writes[2],0x01954b,0x10,8);Write(call.Writes[3],0x01954e,0x18,2);foreach(var field in node.Unwritten1A27)Unknown(field);
  });
  Test("2790A0 preserves exact store sequence widths and original return identity",delegate{
   var f=new Fixture();f.Attributes.SetObservedFloat(0x130,7);f.Construct();Same(f.State,f.Ctor.ReturnedRax,"RAX copies original receiver at2790E2");Check(f.Ctor.ReturnIdentityCopied&&f.Ctor.HelperReturned&&f.Ctor.Returned,"Normal construction sequence completed");Same(f.Ctor.Helper.ReturnedRax,f.State.Head30.Read(0),"Header receives original helperRAX");Same(GameDurationDamageManagerTable.Observed75EF00,f.State.Virtual00.Read(0),"Observed table only");Equal(0x279119u,f.Ctor.Native.At,"Constructor RET");
   uint[] addresses={0x2790b5,0x2790c0,0x2790c6,0x2790ca,0x2790d3,0x2790d6,0x2790df,0x2790ea,0x2790ed,0x2790f1,0x2790f5,0x2790f9,0x2790fd,0x279101,0x279105,0x279109};
   uint[] offsets={8,0,0x18,0x24,0x30,0x38,0x30,0x40,0x48,0x50,0x58,0x60,0x68,0x70,0x78,0x80};Equal(16,f.Ctor.Writes.Count,"Six manager stores before helper and ten after");for(int n=0;n<addresses.Length;n++)Write(f.Ctor.Writes[n],addresses[n],offsets[n],n==7||n==15?4:8);
   Equal(0UL,f.State.Qword08.Read(0),"qword08");Equal(0UL,f.State.Qword18.Read(0),"qword18");Equal(0UL,f.State.Qword24.Read(0),"unaligned qword24");Equal(0UL,f.State.Size38.Read(0),"size38");Equal(0u,f.State.Dword40.Read(0),"dword40");Equal(0u,f.State.Dword80.Read(0),"dword80");Equal(0UL,f.State.Qword48.Read(0),"qword48");Equal(0UL,f.State.Qword50.Read(0),"qword50");Equal(0UL,f.State.Qword58.Read(0),"qword58");Equal(0UL,f.State.Qword78.Read(0),"qword78");Same(null,f.State.Begin60.Read(0),"begin60");Same(null,f.State.End68.Read(0),"end68");Same(null,f.State.Capacity70.Read(0),"capacity70");Unknown(f.State.Unwritten10);Unknown(f.State.Unwritten20);Unknown(f.State.Unwritten2C);Unknown(f.State.Unwritten44);Equal(0x40e00000u,f.Attributes.ReadObservedBits(0x130).Value,"No attribute write");
  });
  Test("Constructor touches only its written cells and allocates distinct normal sentinel",delegate{
   var f=new Fixture();f.Construct();var first=f.State.Head30.Read(0);f.State.Unwritten10.Write(0x1122334455667788UL);f.State.Unwritten20.Write(0xaabbccdd);f.State.Unwritten2C.Write(0x12345678);f.State.Unwritten44.Write(0x87654321);f.State.Qword24.Write(UInt64.MaxValue);f.State.Dword40.Write(UInt32.MaxValue);f.Construct();Check(!Object.ReferenceEquals(first,f.State.Head30.Read(0)),"Separate own allocations");Same(first,first.Parent08.Read(0),"No unproven destruction of old sentinel");Equal(0UL,f.State.Qword24.Read(0),"Exactly qword24 reset");Equal(0u,f.State.Dword40.Read(0),"Exactly DWORD40 reset");Equal(0x1122334455667788UL,f.State.Unwritten10.Read(0),"Untouched10");Equal(0xaabbccddu,f.State.Unwritten20.Read(0),"Untouched20");Equal(0x12345678u,f.State.Unwritten2C.Read(0),"Untouched2C");Equal(0x87654321u,f.State.Unwritten44.Read(0),"Untouched44");
  });
  Test("Real constructed empty vector produces positive zero without consuming key or vptr",delegate{
   var f=new Fixture();f.Construct();f.State.Virtual00.Forget();Equal(0u,NativeAttributeGetter.FloatBits(f.Get(new Field<uint>())),"XORPS positive zero");Equal(0x279d8au,f.Call.Native.At,"Zero return RET");Check(f.Call.Returned&&!f.Call.Matched&&f.Call.ReturnedBits==0,"No matching callback");Equal(1,f.Call.EndCaptures,"Initial end captured");Equal(1,f.Call.InitialBeginReads,"Initial begin subtracted");Equal(0,f.Call.LoopBeginCaptures,"Empty branch never recaptures begin");Equal(0,f.Call.RecordComparisons,"No record or key consumption");Unknown(f.Call.CapturedKey);Unknown(f.State.Virtual00);
  });
  Test("Bounded unmatched records use stride38 captured begin and a fresh end every iteration",delegate{
   var f=new Fixture();var records=f.Records(3);for(int n=0;n<3;n++)records.Record(n).Type0C.Write((uint)(n+1));var begin=f.State.Begin60.Read(0);var end=f.State.End68.Read(0);Equal(0f,f.Get(13),"No match returnszero");Equal(3L,f.Call.InitialCount,"Exact record count");Equal(3,f.Call.RecordComparisons,"All3 records visited");Equal(4,f.Call.EndCaptures,"Initial plus3 freshEnd captures");Equal(1,f.Call.InitialBeginReads,"Initial begin only for first count");Equal(1,f.Call.LoopBeginCaptures,"Single loop begin capture");Same(begin,f.Call.CapturedBegin.Read(0),"Original begin retained");Same(end,f.State.End68.Read(0),"No End overwrite");for(int n=0;n<3;n++)Unknown(records.Record(n).Virtual00);
  });
  Test("Matching type retains same record and stops at the unclosed slot30",delegate{
   var f=new Fixture();var records=f.Records(2);records.Record(0).Type0C.Write(1);records.Record(1).Type0C.Write(13);Missing(0x279d9e,delegate{f.Get(13);},"Unknown vptr copied then consumed at tailcall");Check(f.Call.Matched&&!f.Call.Returned,"Match is not a successful value");Same(records.Record(1),f.Call.MatchedRecord,"Exact second record receiver");Equal(1u,f.Call.Index,"Second record index");Equal(2,f.Call.EndCaptures,"No fresh end after matching record");records.Record(1).Virtual00.Write(new object());Missing(0x279d9e,delegate{f.Get(13);},"Known vptr still lacks real slot30 implementation");records.Record(1).Type0C.Write(UInt32.MaxValue);Missing(0x279d9e,delegate{f.Get(UInt32.MaxValue);},"Comparison preserves all original EDX bits");Same(records.Record(1),f.Call.MatchedRecord,"High-bit type does not change record identity");
  });
  Test("Unknown data fails at actual first consumption without inventing no-match",delegate{
   var f=new Fixture();Missing(0x279d1c,delegate{f.Get(new Field<uint>());},"Unknown captured end consumed by subtraction");Check(f.Call.CapturedKey!=null,"Index MOV copied unknown before pointer arithmetic");f.State.End68.Write(null);Missing(0x279d1c,delegate{f.Get(new Field<uint>());},"Unknown begin subtraction operand");var records=f.Records(2);Missing(0x279d54,delegate{f.Get(13);},"Unknown first record type");records.Record(0).Type0C.Write(1);Missing(0x279d54,delegate{f.Get(new Field<uint>());},"Unknown copied key consumed by CMP");Missing(0x279d54,delegate{f.Get(13);},"Unknown second type cannot become absent");Equal(1u,f.Call.Index,"First mismatch already counted");Equal(2,f.Call.EndCaptures,"Fresh end consumed before second comparison");Check(!f.Call.Returned&&!f.Call.Matched,"No fabricated false result");
  });
  Test("Pointer count uses truncation and rejects only explicit own unsupported domains",delegate{
   var f=new Fixture();var records=f.Records(1);f.State.End68.Write(records.Pointer(55));Equal(0f,f.Get(new Field<uint>()),"Positive span55 truncates tozero before key read");Equal(0L,f.Call.InitialCount,"Count is signed division by56, not nonzero byte test");f.State.Begin60.Write(records.Pointer(56));f.State.End68.Write(records.Pointer(56));Equal(0f,f.Get(new Field<uint>()),"Distinct pointer wrappers at same one-past address are empty");f.State.End68.Write(records.Pointer(0));Missing(0x279d30,delegate{f.Get(1);},"Negative span explicitly unsupported, not empty");
   f.State.Begin60.Write(records.Pointer(0));f.State.End68.Write(new GameDurationDamageRecords(1).Pointer(56));Missing(0x279d1c,delegate{f.Get(1);},"Cross-allocation difference unproven");f.State.End68.Write(records.Pointer(57));Missing(0x279d1c,delegate{f.Get(1);},"Out-of-storage end");var two=new GameDurationDamageRecords(2);f.State.Begin60.Write(two.Pointer(1));f.State.End68.Write(two.Pointer(57));Missing(0x279d54,delegate{f.Get(1);},"Unaligned record not silently rounded to record0");f.State.Begin60.Write(two.Pointer(0));f.State.End68.Write(two.Pointer(56));two.Invalidate();Missing(0x279d1c,delegate{f.Get(1);},"Released own storage never reused");
  });
  Test("Wrong caller and released owner stop before construction or record access",delegate{
   var f=new Fixture();var other=new MoveCharacter();var owner=new GameMovementCharacterState(new EntityMovementState(other));f.Callbacks.Attach(owner);f.Bridge.Bind(owner,other);Missing(0x2790b5,delegate{GameDurationDamageManagerContracts.Construct2790A0(f.State,other,f.Ctor);},"Wrong constructor receiver");Equal(0,f.Ctor.Writes.Count,"No writes before identity rejection");Check(!f.Ctor.Helper.OwnAllocationAttempted,"No allocation before identity rejection");Unknown(f.State.Virtual00);Missing(0x279d16,delegate{GameDurationDamageManagerContracts.GetFixedDamage279D10(f.State,other,Field<uint>.Of(13),f.Call);},"Wrong getter receiver");Equal(0,f.Call.EndCaptures,"No End read before rejection");f.Owner.Entity.MarkReleased();Missing(0x2790b5,delegate{f.Construct();},"Constructor rechecks Entity lifetime");Missing(0x279d16,delegate{f.Get(13);},"Getter rechecks Entity lifetime");Missing(0x123456,delegate{GameDurationDamageManagerState.ForBound(f.Cap,0x123456);},"Lookup cannot return released state");
  });
 }
}
