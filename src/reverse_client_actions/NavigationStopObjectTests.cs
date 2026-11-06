using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Component-only OWN fixtures: no original registration writer, live map,
// frame progression, Goal callback, destructor or arrival claim is exercised.
public static class NavigationStopObjectTests {
 public static int Scenarios{get;private set;}
 public static int Assertions{get;private set;}
 static void Check(bool yes,string why){Assertions++;if(!yes)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Test(string name,Action body){body();Scenarios++;Console.WriteLine("PASS StopObject "+name);}
 static void Point(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
 sealed class Fixture {
  public readonly NavigationCrowdManager Crowd=new NavigationCrowdManager();
  public readonly NavigationCrowdAgent Agent=new NavigationCrowdAgent();
  public readonly NavigationCrowdGroup Group=new NavigationCrowdGroup();
  public readonly NavigationCrowdAdapterState Outer=new NavigationCrowdAdapterState();
  public readonly Field<NavigationCrowdAdapterState> OuterCell=new Field<NavigationCrowdAdapterState>();
  public readonly NavigationMoveManagerView Navigation;
  public readonly MoveCharacter Role=new MoveCharacter();
  public readonly MovementManager GameManager=new MovementManager();
  public readonly GameMovementCharacterState Owner;
  public readonly NavigationMoveObjectContext Movement;
  public readonly NavigationStopObjectContext Stop;
  public readonly NavigationStopObjectInvocation Call=new NavigationStopObjectInvocation();
  public readonly NavigationMoveRegistry Map;
  public readonly NavigationMoveRegistryNode Sentinel=new NavigationMoveRegistryNode(),Node=new NavigationMoveRegistryNode();
  public readonly NavigationMoveRegistryBuckets Buckets=new NavigationMoveRegistryBuckets(1);
  public readonly object WorkBegin=new object(),WorkEnd=new object(),WaypointBegin=new object(),WaypointEnd=new object();
  public Fixture(){
   Navigation=new NavigationMoveManagerView(OuterCell);OuterCell.Write(Outer);NavigationMoveOuterView.ForAdapter(Outer).Crowd08.Write(Crowd);
   Owner=new GameMovementCharacterState(new EntityMovementState(Role));Role.MovementDC0.Write(GameManager);GameManager.Owner00.Write(Role);GameManager.Moving38.Write(1);
   var cs=new AddressSymbol<ClassRecord>(0xa5cba0);cs.Write(new ClassRecord());var ps=new AddressSymbol<ClassRecord>(0xa5cab0);ps.Write(new ClassRecord());
   var game=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),cs,ps);game.Attach(Owner);
   var addresses=new NavigationMoveAddressSpace(game);addresses.BindOwnEntityAddress(Owner,0x100000000UL);
   // Unused query dependencies require an explicit OWN mesh container only.
   // No tile is loaded and StopObject is not replaced by a query callback.
   var mesh=NavigationMeshTilesState.Create(0,0,0,1,1,4,8,new NavigationSpatialState());
   Movement=new NavigationMoveObjectContext(addresses,new NavigationCrowdAdapterBoundaries(mesh),new NavigationCrowdPathBoundaries(mesh));Stop=NavigationStopObjectContext.ForMovement(Movement);
   Map=NavigationMoveRegistry.ForManager(Crowd);Map.Sentinel30.Write(Sentinel);Map.Mask58.Write(0);Map.Buckets40.Write(Buckets);Node.Key10.Write(0x100000038UL);Node.Agent18.Write(Agent);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);
   var members=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1);members.Cells[0].Write(Agent);Group.Members28.InstallOwnStorage(members,1);Group.Owner00.Write(Crowd);Group.Query08.Write(Outer);var groups=new NavigationCrowdPointerBuffer<NavigationCrowdGroup>(1);groups.Cells[0].Write(Group);Crowd.Groups10.InstallOwnStorage(groups,1);Crowd.QueryB8.Write(Outer);
   Point(Agent.Position48,1.25f,-2.5f,3.75f);Point(Agent.Target78,8,9,10);Point(Agent.VelocityB8,4,5,6);Agent.State70.Write(2);Agent.Timer6C.Write(0x3f800000);Agent.Reason74.Write(0xaabbccdd);Agent.Flags44.Write(0x800004c5);Agent.NeedReplan140.Write(7);Agent.Callback08.Write(Owner.Interface38);
   Agent.RequestE8.Generation00.Write(17);Agent.RequestE8.Complete50.Write(1);Agent.RequestE8.WaypointVector38.Begin00.Write(WaypointBegin);Agent.RequestE8.WaypointVector38.End08.Write(WaypointEnd);Group.Work40.Begin00.Write(WorkBegin);Group.Work40.End08.Write(WorkEnd);for(int n=0;n<6;n++)Agent.Words88B0[n].Write((ulong)(100+n));
  }
  public void Run(){NavigationStopObjectContracts.Stop151150(Navigation,Owner,Stop,Call);}
  public void Miss(){Buckets.Cells[0].First00.Write(Sentinel);Buckets.Cells[0].Last08.Forget();Node.Agent18.Forget();}
  public void NoStores(){Equal(0,Call.StoreAddresses.Count,"No native stores");Equal(8f,Agent.Target78.X.Read(0),"Target X preserved");Equal(9f,Agent.Target78.Y.Read(0),"Target Y preserved");Equal(10f,Agent.Target78.Z.Read(0),"Target Z preserved");Equal(2u,Agent.State70.Read(0),"State preserved");Equal(0x3f800000u,Agent.Timer6C.Read(0),"Timer preserved");}
  public void OtherStatePreserved(){
   Equal(4f,Agent.VelocityB8.X.Read(0),"Velocity X preserved");Equal(5f,Agent.VelocityB8.Y.Read(0),"Velocity Y preserved");Equal(6f,Agent.VelocityB8.Z.Read(0),"Velocity Z preserved");Equal(0xaabbccddu,Agent.Reason74.Read(0),"Adjacent Reason DWORD preserved");Equal(0x800004c5u,Agent.Flags44.Read(0),"Player flags preserved");Equal((byte)7,Agent.NeedReplan140.Read(0),"Replan preserved");Equal(17u,Agent.RequestE8.Generation00.Read(0),"Request generation preserved");Equal((byte)1,Agent.RequestE8.Complete50.Read(0),"Request complete is not arrival");Same(WaypointBegin,Agent.RequestE8.WaypointVector38.Begin00.Read(0),"Waypoint begin preserved");Same(WaypointEnd,Agent.RequestE8.WaypointVector38.End08.Read(0),"Waypoint end preserved");Same(WorkBegin,Group.Work40.Begin00.Read(0),"Work begin preserved");Same(WorkEnd,Group.Work40.End08.Read(0),"Work end preserved");for(int n=0;n<6;n++)Equal((ulong)(100+n),Agent.Words88B0[n].Read(0),"Neighbor word preserved");Equal((byte)1,GameManager.Moving38.Read(0),"Game Moving untouched");Unknown(GameManager.Registered6C,"No native registration inferred");Same(Owner.Interface38,Agent.Callback08.Read(0),"Callback identity preserved");
  }
 }
 public static void Run(){
  Scenarios=Assertions=0;
  Test("hit borrows original registry and Agent and performs only four ordered stores",delegate{
   var f=new Fixture();f.Run();Check(f.Call.Returned&&f.Call.Hit&&f.Call.LookupReturned,"Actual hit returned");Equal(0x25b5c3u,f.Call.Native.At,"Hit RET");Same(f.Stop,NavigationStopObjectContext.ForMovement(f.Movement),"One stop context");Same(f.Movement.DisableAllAssert42144A,f.Stop.DisableAllAssert42144A,"Shared global cell");Same(f.OuterCell,f.Navigation.Outer10,"Borrowed outer field");Same(f.Outer,f.Call.Outer,"Original outer");Same(f.Crowd,f.Call.Crowd,"Original Crowd");Same(f.Map,f.Call.Registry,"Original registry");Same(f.Agent,f.Call.Agent.Read(0),"Original Agent");Same(f.Agent,f.Group.Members28.Begin00.Read(0).Buffer.At(0,0).Read(0),"Group member remains same Agent");Equal(0x100000038UL,f.Call.Key.Read(0),"Original high64 owner+38 key");Equal(8,f.Call.Lookup.HashedBytes,"Real eight-byte lookup");
   Equal(4,f.Call.StoreAddresses.Count,"Four native stores");uint[] order={0x25b5a2,0x25b5a7,0x25b5ad,0x25b5b4};for(int n=0;n<4;n++)Equal(order[n],f.Call.StoreAddresses[n],"XY8/Z4/State4/Timer4 order");Equal(1.25f,f.Agent.Target78.X.Read(0),"Target X copied");Equal(-2.5f,f.Agent.Target78.Y.Read(0),"Target Y copied");Equal(3.75f,f.Agent.Target78.Z.Read(0),"Target Z copied");Equal(3u,f.Agent.State70.Read(0),"State is 3, not final Goal state4");Equal(0u,f.Agent.Timer6C.Read(0),"Timer DWORD zero");Unknown(f.Stop.DisableMissingAgent422B8E,"Hit does not read or write assertion byte");Unknown(f.Stop.DisableAllAssert42144A,"Hit bypasses common gate");f.OtherStatePreserved();
  });
  Test("MOV copies unknown and non-arithmetic float bits",delegate{var f=new Fixture();f.Agent.Position48.X.Write(Float(0x80000000));f.Agent.Position48.Y.Forget();f.Agent.Position48.Z.Write(Float(0x7fc12345));f.Run();Equal(0x80000000u,Bits(f.Agent.Target78.X.Read(0)),"Negative zero bits preserved");Unknown(f.Agent.Target78.Y,"Unknown Y copied without consumption");Equal(0x7fc12345u,Bits(f.Agent.Target78.Z.Read(0)),"Quiet NaN payload copied without arithmetic");Unknown(f.Call.CapturedPosition.Y,"Snapshot also opaque");Equal(3u,f.Agent.State70.Read(0),"Opaque copy reaches State store");Equal(0u,f.Agent.Timer6C.Read(0),"Opaque copy reaches Timer store");Check(f.Call.Returned,"Opaque normal return");});
  Test("null owner still reads outer and performs real zero-key lookup",delegate{var f=new Fixture();f.Node.Key10.Write(0);NavigationStopObjectContracts.Stop151150(f.Navigation,null,f.Stop,f.Call);Equal(0UL,f.Call.Key.Read(0),"Null owner adjusted to null");Same(f.Outer,f.Call.Outer,"Outer still read");Same(f.Agent,f.Call.Agent.Read(0),"Actual matched zero-key node");Equal(8,f.Call.Lookup.HashedBytes,"Zero key is still hashed");Equal(3u,f.Agent.State70.Read(0),"Matched Agent updated");});
  Test("miss first nonzero gate short-circuits unknown common gate",delegate{var f=new Fixture();f.Miss();f.Stop.DisableMissingAgent422B8E.Write(1);f.Run();Check(f.Call.Returned&&!f.Call.Hit,"Miss is normal void return, not movement success");Equal(0x25b6c1u,f.Call.Native.At,"Miss RET");Unknown(f.Stop.DisableAllAssert42144A,"Second gate not consumed");Unknown(f.Call.Agent,"Sentinel skips Agent18 read");f.NoStores();});
  Test("matched null Agent uses same gated miss path",delegate{var f=new Fixture();f.Node.Agent18.Write(null);f.Stop.DisableMissingAgent422B8E.Write(1);f.Agent.Position48.MarkReleased();f.Run();Same(null,f.Call.Agent.Read(0),"Known null retained");Check(f.Call.Returned&&!f.Call.Hit,"Null Agent returned through gate");f.NoStores();});
  Test("common global gate is borrowed live and can suppress missing diagnostic",delegate{var f=new Fixture();f.Miss();f.Stop.DisableMissingAgent422B8E.Write(0);f.Movement.DisableAllAssert42144A.Write(2);f.Run();Check(f.Call.Returned,"Common nonzero gate returns");Equal((byte)2,f.Stop.DisableAllAssert42144A.Read(0),"Live shared common field");f.Stop.DisableAllAssert42144A.Forget();Unknown(f.Movement.DisableAllAssert42144A,"Forget is shared both ways");f.NoStores();});
  Test("unknown first gate retains exact failure prefix",delegate{var f=new Fixture();f.Miss();Missing(0x25b5c4,delegate{f.Run();},"First missing gate");Equal(0x25b5c4u,f.Call.Native.At,"Recorded failure");Check(f.Call.LookupReturned&&!f.Call.Returned,"Lookup completed, Stop did not");f.NoStores();});
  Test("zero first gate consumes unknown common byte only afterward",delegate{var f=new Fixture();f.Miss();f.Stop.DisableMissingAgent422B8E.Write(0);Missing(0x25b5d1,delegate{f.Run();},"Common gate unknown");Equal((byte)0,f.Stop.DisableMissingAgent422B8E.Read(0),"First gate unchanged");Check(!f.Call.Returned,"No synthetic return");f.NoStores();});
  Test("enabled diagnostic stops at its first real call without disabling assert",delegate{var f=new Fixture();f.Miss();f.Stop.DisableMissingAgent422B8E.Write(0);f.Movement.DisableAllAssert42144A.Write(0);Missing(0x25b5f8,delegate{f.Run();},"GetLastError import unclosed");Equal((byte)0,f.Stop.DisableMissingAgent422B8E.Read(0),"No fabricated later422B8E store");Equal((byte)0,f.Movement.DisableAllAssert42144A.Read(0),"Common gate untouched");Check(!f.Call.Returned,"Diagnostic is Unsupported");f.NoStores();});
  Test("unknown Agent pointer stops at TEST after actual lookup",delegate{var f=new Fixture();f.Node.Agent18.Forget();Missing(0x25b595,delegate{f.Run();},"Opaque Agent MOV consumed by TEST");Check(f.Call.LookupReturned&&!f.Call.Hit&&!f.Call.Returned,"Lookup-only prefix");Unknown(f.Call.Agent,"Captured pointer remains Unknown");f.NoStores();});
  Test("wrapper preserves deferred outer and Crowd pointer failures even for null owner",delegate{
   var a=new Fixture();a.OuterCell.Forget();Missing(0x151161,delegate{NavigationStopObjectContracts.Stop151150(a.Navigation,null,a.Stop,a.Call);},"Unknown outer consumed at+8 load");Check(!a.Call.LookupReturned,"Lookup not entered");a.NoStores();
   var b=new Fixture();b.OuterCell.Write(null);Missing(0x151161,delegate{NavigationStopObjectContracts.Stop151150(b.Navigation,null,b.Stop,b.Call);},"Known null outer still dereferenced");b.NoStores();
   var c=new Fixture();NavigationMoveOuterView.ForAdapter(c.Outer).Crowd08.Forget();Missing(0x05daf5,delegate{c.Run();},"Opaque Crowd consumed in real lookup callee");Same(c.Outer,c.Call.Outer,"Outer prefix retained");Equal(0x100000038UL,c.Call.Key.Read(0),"Key captured before lookup");c.NoStores();
  });
  Test("released OWN destination preserves captured source and blocks later stores",delegate{var f=new Fixture();f.Agent.Target78.MarkReleased();Missing(0x25b5a2,delegate{f.Run();},"OWN destination lifetime at first store");Equal(1.25f,f.Call.CapturedPosition.X.Read(0),"XY captured before destination");Equal(-2.5f,f.Call.CapturedPosition.Y.Read(0),"Y captured before destination");Equal(3.75f,f.Call.CapturedPosition.Z.Read(0),"Z captured before destination");Equal(0,f.Call.StoreAddresses.Count,"No completed store");Equal(2u,f.Agent.State70.Read(0),"State not written after failure");Equal(0x3f800000u,f.Agent.Timer6C.Read(0),"Timer not written after failure");Check(!f.Call.Returned,"OWN lifetime failure not native page-fault coverage");});
 }
}
