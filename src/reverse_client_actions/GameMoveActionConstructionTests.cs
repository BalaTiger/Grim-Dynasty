using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

public static class GameMoveActionConstructionTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected MissingContract: "+why);}
 static void Throws<T>(Action action,string why)where T:Exception{try{action();}catch(T){Check(true,why);return;}throw new Exception("Expected "+typeof(T).Name+": "+why);}
 static void Unknown<T>(Field<T> field){Missing(0,delegate{field.Read(0);},"Unknown value not initialized");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
 static WorldPosition Position(ulong a,ulong b,ulong c){return new WorldPosition(Field<ulong>.Of(a),Field<ulong>.Of(b),Field<ulong>.Of(c));}
 static void PositionIs(WorldPosition p,ulong a,ulong b,ulong c){Equal(a,p.Q0.Read(0),"Position+0");Equal(b,p.Q8.Read(0),"Position+8");Equal(c,p.Q16.Read(0),"Position+16");}
 static void Write(GameMoveActionConstructionWrite write,uint at,uint offset,int bytes){Equal(at,write.Address,"Write instruction");Equal(offset,write.Offset,"Write offset");Equal(bytes,write.Bytes,"Write width");}
 sealed class Fixture {
  public readonly MoveActionRecord Action=new MoveActionRecord();
  public readonly GameReachedGoalActionView Primary;
  public readonly GameMoveActionConstructionState State;
  public readonly GameMoveActionConstructionInvocation Call=new GameMoveActionConstructionInvocation();
  public WorldPosition Start=Position(10,20,30),Destination=Position(40,50,60);
  public Field<uint> Owner=Field<uint>.Of(0xdeadbeef),Flags=Field<uint>.Of(0x87654321),Skill=Field<uint>.Of(9),Animation=Field<uint>.Of(0xfedcba98);
  public Field<float> Speed=Field<float>.Of(Float(0x80000000));
  public Fixture(){Primary=new GameReachedGoalActionView(Action);State=GameMoveActionConstructionState.Bind(Action,Primary);}
  public MoveActionRecord Construct(){return GameMoveActionConstructionContracts.Construct082B10(State,Owner,Start,Destination,Flags,Skill,Speed,Animation,Call);}
 }
 public static void Run(){scenarios=assertions=0;
  Test("Unique sidecar borrows original action and existing primary cells",delegate{
   var f=new Fixture();Same(f.State,GameMoveActionConstructionState.Bind(f.Action,f.Primary),"Idempotent binding");Same(f.State,GameMoveActionConstructionState.ForBound(f.Action,1),"Canonical lookup");Same(f.Action,f.State.Action,"Original action");Same(f.Primary,f.State.Primary,"Original primary view");Same(f.Primary.Virtual00,f.State.Virtual00,"One authoritative vptr cell");
   Unknown(f.State.Virtual00);Unknown(f.State.Dword0C);Unknown(f.State.Byte10);Unknown(f.Action.Flag11);Unknown(f.State.Position20.Q0);Unknown(f.State.Position20.D14);
   Throws<InvalidOperationException>(delegate{GameMoveActionConstructionState.Bind(f.Action,new GameReachedGoalActionView(f.Action));},"Reject second primary for same action");Throws<ArgumentException>(delegate{GameMoveActionConstructionState.Bind(new MoveActionRecord(),f.Primary);},"Reject other receiver primary");Missing(0x123,delegate{GameMoveActionConstructionState.ForBound(new MoveActionRecord(),0x123);},"No implicit binding");Unknown(f.Primary.Virtual00);
  });
  Test("008300 alias body only writes vptr and returns original this",delegate{
   var f=new Fixture();var i=new Invocation();f.State.Dword0C.Write(77);f.State.Byte10.Write(0x55);f.Action.Flag11.Write(0xaa);f.Action.OwnerId08.Write(0x12345678);f.State.Position20.Q0.Write(99);
   Same(f.Action,GameMoveActionConstructionContracts.ConstructCharacterAction008300(f.State,i),"Engine RAX identity");Same(GameMoveActionConstructionTable.Engine339EF0,f.Primary.Virtual00.Read(0),"Only observed Engine vptr");Equal(0x00830du,i.At,"Engine RET");Equal(0x12345678u,f.Action.OwnerId08.Read(0),"No owner argument consumption or write");Equal(77u,f.State.Dword0C.Read(0),"No DWORD0C write");Equal((byte)0x55,f.State.Byte10.Read(0),"No byte10 write");Equal((byte)0xaa,f.Action.Flag11.Read(0),"No byte11 write");Equal(99UL,f.State.Position20.Q0.Read(0),"No WorldVec write");Unknown(f.Action.PreAction18);Unknown(f.Action.Start48.Q0);Unknown(f.Action.Speed80);
  });
  Test("082B10 preserves complete normal write sequence width and return identity",delegate{
   var f=new Fixture();Same(f.Action,f.Construct(),"Original constructor return");Same(f.Action,f.Call.EngineReturnedRax,"Engine returns same action");Same(f.Action,f.Call.ReturnedRax,"Game returns same action");Check(f.Call.EngineReturned&&f.Call.WorldVecReturned&&f.Call.Returned,"All concrete calls returned");Same(GameReachedGoalActionTable.MoveTo,f.Primary.Virtual00.Read(0),"Existing MoveTo table identity");Equal(0x082bf9u,f.Call.Native.At,"Game RET");
   uint[] addresses={0x008307,0x082b4c,0x082b4f,0x082b53,0x082b5b,0x082b62,0x082b6a,0x228ab0,0x082b81,0x082b87,0x082b91,0x082b9a,0x082ba4,0x082bae,0x082bb6,0x082bc0,0x082bcd,0x082bd4,0x082bdc};
   uint[] offsets={0,0,8,0x0c,0x10,0x14,0x18,0x20,0,0x48,0x58,0x60,0x70,0x78,0x7c,0x80,0x84,0x0c,0x10};int[] widths={8,8,4,4,2,4,1,8,8,16,8,16,8,4,4,4,4,4,1};Equal(19,f.Call.Writes.Count,"Exactly19 observed stores including callees");for(int n=0;n<addresses.Length;n++)Write(f.Call.Writes[n],addresses[n],offsets[n],widths[n]);
   Equal(0xdeadbeefu,f.Action.OwnerId08.Read(0),"Owner allbits");Equal(4u,f.State.Dword0C.Read(0),"Final kind4");Equal((byte)1,f.State.Byte10.Read(0),"Final byte10");Equal((byte)1,f.Action.Flag11.Read(0),"High word byte survives final low-byte overwrite");Equal(0x437a0000u,Bits(f.Action.Scalar14.Read(0)),"Raw scalar14 bits");Equal((byte)0,f.Action.PreAction18.Read(0),"Only byte18 initialized");Equal(0UL,f.State.Position20.Q0.Read(0),"WorldVec qword only");PositionIs(f.Action.Start48,10,20,30);PositionIs(f.Action.Destination60,40,50,60);Equal(0x87654321u,f.Action.Flags78.Read(0),"Flags copied");Equal(9u,f.Action.SkillReference7C.Read(0),"Skill copied");Equal(0x80000000u,Bits(f.Action.Speed80.Read(0)),"MOVSS keeps negative zero bits");Equal(0xfedcba98u,f.Action.Animation84.Read(0),"Animation allbits");
  });
  Test("Inline WorldVec writes Q0 only and preserves all trailing knowledge",delegate{
   var f=new Fixture();f.State.Position20.Q0.Write(123);f.State.Position20.D8.Write(0x12345678);f.State.Position20.D14.Write(0xaabbccdd);f.Construct();Equal(0UL,f.State.Position20.Q0.Read(0),"Q0 reset");Equal(0x12345678u,f.State.Position20.D8.Read(0),"Unwritten28 retained");Equal(0xaabbccddu,f.State.Position20.D14.Read(0),"Unwritten34 retained");Unknown(f.State.Position20.DC);Unknown(f.State.Position20.D10);Same(f.State.Position20,GameMoveActionConstructionState.ForBound(f.Action,0).Position20,"Unique inline position identity");
  });
  Test("Unknown MOV data propagates and later calls read fresh observations",delegate{
   var f=new Fixture();f.Owner.Forget();f.Flags.Forget();f.Skill.Forget();f.Speed.Forget();f.Animation.Forget();f.Start=new WorldPosition();f.Destination=new WorldPosition();f.Construct();Check(f.Call.Returned,"No data arithmetic in ctor");Unknown(f.Call.CapturedOwnerId);Unknown(f.Action.OwnerId08);Unknown(f.Action.Flags78);Unknown(f.Action.SkillReference7C);Unknown(f.Action.Speed80);Unknown(f.Action.Animation84);Unknown(f.Action.Start48.Q0);Unknown(f.Action.Start48.Q8);Unknown(f.Action.Start48.Q16);Unknown(f.Action.Destination60.Q0);Unknown(f.Action.Destination60.Q8);Unknown(f.Action.Destination60.Q16);
   f.Owner.Write(123);f.Flags.Write(456);f.Speed.Write(Float(0x7fc12345));f.Start.Q8.Write(0xff00112233445566UL);f.Construct();Equal(123u,f.Action.OwnerId08.Read(0),"Fresh owner snapshot");Equal(456u,f.Action.Flags78.Read(0),"Fresh stack load");Equal(0x7fc12345u,Bits(f.Action.Speed80.Read(0)),"Opaque NaN payload, no floating arithmetic");Equal(0xff00112233445566UL,f.Action.Start48.Q8.Read(0),"Fresh position word");Unknown(f.Action.Start48.Q0);Unknown(f.Action.SkillReference7C);
  });
  Test("Self-source positions preserve exact16 plus8 copy",delegate{
   var f=new Fixture();f.Action.Start48.Q0.Write(11);f.Action.Start48.Q8.Write(22);f.Action.Start48.Q16.Write(33);f.Action.Destination60.Q0.Write(44);f.Action.Destination60.Q8.Write(55);f.Action.Destination60.Q16.Write(66);f.Start=f.Action.Start48;f.Destination=f.Action.Destination60;f.Construct();PositionIs(f.Action.Start48,11,22,33);PositionIs(f.Action.Destination60,44,55,66);
  });
  Test("Overlapping source captures16 together but reads tail8 after that store",delegate{
   var f=new Fixture();f.Action.Start48.Q0.Write(10);f.Action.Start48.Q8.Write(20);f.Action.Start48.Q16.Write(30);f.Start=new WorldPosition(f.Action.Start48.Q8,f.Action.Start48.Q0,f.Action.Start48.Q0);
   f.Action.Destination60.Q0.Write(40);f.Action.Destination60.Q8.Write(50);f.Action.Destination60.Q16.Write(60);f.Destination=new WorldPosition(f.Action.Destination60.Q8,f.Action.Destination60.Q0,f.Action.Destination60.Q0);f.Construct();PositionIs(f.Action.Start48,20,10,20);PositionIs(f.Action.Destination60,50,40,50);
  });
  Test("Cross-position and scalar aliases respect original load timing",delegate{
   var f=new Fixture();f.Action.Destination60.Q0.Write(7);f.Action.Destination60.Q8.Write(8);f.Action.Destination60.Q16.Write(9);f.Start=f.Action.Destination60;f.Destination=f.Action.Start48;f.State.Dword0C.Write(88);f.Owner=f.State.Dword0C;f.Flags=f.Action.OwnerId08;f.Skill=f.Action.Flags78;f.Speed=f.Action.Scalar14;f.Animation=f.State.Dword0C;f.Construct();PositionIs(f.Action.Start48,7,8,9);PositionIs(f.Action.Destination60,7,8,9);Equal(88u,f.Call.CapturedOwnerId.Read(0),"Owner snapshotted082B39 before0C reset");Equal(88u,f.Action.OwnerId08.Read(0),"Owner uses early snapshot");Equal(88u,f.Action.Flags78.Read(0),"Flags read afterOwner08 write");Equal(88u,f.Action.SkillReference7C.Read(0),"Skill read afterFlags78 write");Equal(0x437a0000u,Bits(f.Action.Speed80.Read(0)),"Speed read afterScalar14 write");Equal(0u,f.Action.Animation84.Read(0),"Animation read before0C final4");Equal(4u,f.State.Dword0C.Read(0),"Final0C does not change copied animation");
  });
  Test("Missing position stops after real prefix without claiming constructor success",delegate{
   var f=new Fixture();f.Start=null;Missing(0x082b84,delegate{f.Construct();},"First source dereference");Check(f.Call.EngineReturned&&f.Call.WorldVecReturned&&!f.Call.Returned,"Callees succeeded butGame incomplete");Same(null,f.Call.ReturnedRax,"No fabricated RAX");Equal(9,f.Call.Writes.Count,"Prefix ends with final table");Same(GameReachedGoalActionTable.MoveTo,f.Primary.Virtual00.Read(0),"Final vptr precedes source access");Equal((byte)0,f.State.Byte10.Read(0),"Low byte fromWORD0100");Equal((byte)1,f.Action.Flag11.Read(0),"High byte fromWORD0100");Equal(0u,f.State.Dword0C.Read(0),"Final kind4 not reached");Unknown(f.Action.Start48.Q0);Unknown(f.Action.Flags78);
   f.Start=Position(1,2,3);f.Destination=null;Missing(0x082b97,delegate{f.Construct();},"Second source first dereference");PositionIs(f.Action.Start48,1,2,3);Equal(11,f.Call.Writes.Count,"First16+8 stores completed");Unknown(f.Action.Destination60.Q0);Check(!f.Call.Returned,"Second-source failure is not success");
  });
  Test("Missing scalar observations fail only at individual stack loads",delegate{
   var f=new Fixture();f.Flags=null;Missing(0x082baa,delegate{f.Construct();},"Flags load");PositionIs(f.Action.Destination60,40,50,60);Equal(13,f.Call.Writes.Count,"Both position copies precede flags");Unknown(f.Action.Flags78);
   f.Flags=Field<uint>.Of(17);f.Skill=null;Missing(0x082bb2,delegate{f.Construct();},"Skill load");Equal(17u,f.Action.Flags78.Read(0),"Flags prefix remains");Equal(14,f.Call.Writes.Count,"Flags stored");Unknown(f.Action.SkillReference7C);
   f.Skill=Field<uint>.Of(18);f.Speed=null;Missing(0x082bba,delegate{f.Construct();},"Speed load");Equal(18u,f.Action.SkillReference7C.Read(0),"Skill prefix remains");Equal(15,f.Call.Writes.Count,"Skill stored");Unknown(f.Action.Speed80);
   f.Speed=Field<float>.Of(19);f.Animation=null;Missing(0x082bc9,delegate{f.Construct();},"Animation load");Equal(19f,f.Action.Speed80.Read(0),"Speed prefix remains");Equal(16,f.Call.Writes.Count,"Speed stored");Unknown(f.Action.Animation84);Equal((byte)0,f.State.Byte10.Read(0),"Finalbyte10 not reached");Equal(0u,f.State.Dword0C.Read(0),"Finalkind4 not reached");Check(!f.Call.Returned,"No success at scalar stop");
  });
  Test("Released and missing owners reject without stale successful diagnostics",delegate{
   var f=new Fixture();f.Construct();f.Action.MarkReleased();Missing(0x082b3e,delegate{f.Construct();},"Released receiver at base call");Equal(0,f.Call.Writes.Count,"No writes after receiver failure");Check(!f.Call.EngineReturned&&!f.Call.WorldVecReturned&&!f.Call.Returned,"Success flags cleared before rejection");Same(null,f.Call.EngineReturnedRax,"Engine RAX cleared");Same(null,f.Call.ReturnedRax,"Final RAX cleared");Missing(0x987,delegate{GameMoveActionConstructionState.ForBound(f.Action,0x987);},"Lookup checks lifetime");
   var live=new Fixture();Missing(0x082b3e,delegate{GameMoveActionConstructionContracts.Construct082B10(null,live.Owner,live.Start,live.Destination,live.Flags,live.Skill,live.Speed,live.Animation,live.Call);},"No missing receiver default");Equal(0,live.Call.Writes.Count,"No write for missingreceiver");live.Owner=null;Missing(0x082b39,delegate{live.Construct();},"Missing register observation is not unknown bits");Check(!live.Call.EngineReturned&&!live.Call.Returned,"No successful prefix fabricated");Same(null,live.Call.CapturedOwnerId,"Failed capture cleared");Unknown(live.Primary.Virtual00);
  });
 }
}
