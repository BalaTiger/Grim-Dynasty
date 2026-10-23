using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New view tests only. No tick, callback, path search, floor, World publication,
// or loaded-game result is claimed. Test code is prepared, not yet compiled.
public static class NavigationCrowdTickViewTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool yes,string why){assertions++;if(!yes)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object a,object b,string why){Check(Object.ReferenceEquals(a,b),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected stop: "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 public static void Run(){
  scenarios=0;assertions=0;
  Test("View creation retains every raw identity and leaves unknowns untouched",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdTickStateView(a);var w=new NavigationCrowdTickStateView(a);
   Same(a,v.Agent,"Owner");Same(a.Timer6C,v.Speed6C.Raw,"6C");Same(a.GapD0,v.CorrectionD0.Raw,"D0");Same(a.GapD4,v.CorrectionD4.Raw,"D4");Same(a.GapD8,v.CorrectionD8.Raw,"D8");Same(a.WordDC,v.SpeedAndElapsed.Raw,"DC/E0");Same(a.Timer6C,w.Speed6C.Raw,"Two views one cell");
   Same(a.Words88B0[0],v.NeighborBegin88,"Neighbor begin");Same(a.Words88B0[1],v.NeighborEnd90,"Neighbor end");Same(a.Words88B0[2],v.NeighborCapacity98,"Neighbor capacity");
   Unknown(a.Timer6C,"No speed default");Unknown(a.GapD0,"No correction default");Unknown(a.WordDC,"No packed default");Unknown(a.Flags44,"No flags default");Unknown(a.Callback08,"No callback default");Unknown(v.NeighborBegin88,"No invented address");
  });
  Test("Float aliases preserve raw bits and observe writes from either interface",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdTickStateView(a);var w=new NavigationCrowdTickStateView(a);
   a.Timer6C.Write(0x3fc00000);Equal(1.5f,v.Speed6C.Read(0x25d31c),"Raw to float");v.Speed6C.Write(-2.5f,0x25d325);Equal(0xc0200000u,a.Timer6C.Read(0),"Float to raw");Equal(-2.5f,w.Speed6C.Read(1),"Other view sees write");
   uint[] bits={0u,0x80000000u,1u,0x7f800000u,0xff800000u,0x7fc12345u};
   foreach(uint value in bits){v.Speed6C.WriteBits(value,2);Equal(value,a.Timer6C.Read(0),"Exact raw storage");Equal(value,w.Speed6C.ReadBits(3),"Exact other view bits");}
   a.Timer6C.Write(0x80000000);Equal(0x80000000u,BitConverter.ToUInt32(BitConverter.GetBytes(v.Speed6C.Read(4)),0),"Negative zero read");
   v.CorrectionD0.Write(2,5);v.CorrectionD4.Write(3,6);v.CorrectionD8.Write(4,7);Equal(0x40000000u,a.GapD0.Read(0),"X");Equal(0x40400000u,a.GapD4.Read(0),"Y");Equal(0x40800000u,a.GapD8.Read(0),"Z");Unknown(a.GapE4,"Neighbor padding untouched");
  });
  Test("Opaque DWORD copy and snapshot preserve unknown without extra reads",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdTickStateView(a);var captured=v.Speed6C.SnapshotBits(10);Unknown(captured,"Unknown captured");v.Speed6C.Write(1,11);v.Speed6C.CopyBitsFrom(captured,12);Unknown(a.Timer6C,"Unknown propagated");
   Missing(13,delegate{v.Speed6C.Read(13);},"Actual unknown consumption");a.Timer6C.Write(0x40800000);captured=v.Speed6C.SnapshotBits(14);a.Timer6C.Write(0x41000000);Equal(0x40800000u,captured.Read(0),"Snapshot fixed");Equal(8f,v.Speed6C.Read(15),"View remains live");
  });
  Test("Packed float and signed counter writes preserve the other DWORD exactly",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdTickStateView(a);a.WordDC.Write(0x800000003fc00000UL);
   Equal(1.5f,v.SpeedAndElapsed.ReadSpeedDC(20),"Low float");Equal(Int32.MinValue,v.SpeedAndElapsed.ReadElapsedE0(21),"High signed counter");
   v.SpeedAndElapsed.WriteSpeedDC(-2,22);Equal(0x80000000c0000000UL,a.WordDC.Read(0),"Low store preserves high");v.SpeedAndElapsed.WriteElapsedE0(-1,23);Equal(0xffffffffc0000000UL,a.WordDC.Read(0),"High store preserves low");
   a.WordDC.Write(0x000000057fc12345UL);v.SpeedAndElapsed.WriteElapsedE0(6,24);Equal(0x000000067fc12345UL,a.WordDC.Read(0),"NaN payload retained without FP conversion");v.SpeedAndElapsed.WriteSpeedBitsDC(0x80000000,25);Equal(0x0000000680000000UL,a.WordDC.Read(0),"Signed zero raw write");
  });
  Test("Packed unknown sibling is an explicit own limit and no partial write occurs",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdTickStateView(a);
   Missing(30,delegate{v.SpeedAndElapsed.WriteSpeedDC(2,30);},"Unknown sibling low store");Unknown(a.WordDC,"Whole word remains unknown");Missing(31,delegate{v.SpeedAndElapsed.WriteElapsedE0(3,31);},"Unknown sibling high store");Unknown(a.WordDC,"High store did not invent low");
   var captured=v.SpeedAndElapsed.SnapshotWord(32);v.SpeedAndElapsed.CopyWholeWordFrom(Field<ulong>.Of(0),33);v.SpeedAndElapsed.WriteSpeedDC(4,34);Equal(0x40800000UL,a.WordDC.Read(0),"Explicit full write enables lane store");v.SpeedAndElapsed.CopyWholeWordFrom(captured,35);Unknown(a.WordDC,"Whole opaque unknown copy");
  });
  Test("Agent plus48 address borrows all three original point objects and cells",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdTickStateView(a);var b=new NavigationCrowdAgentDataView(a);var other=new NavigationCrowdAgentDataView(new NavigationCrowdAgent());
   Check(v.Data48.SameAddress(b),"Same agent address");Check(!v.Data48.SameAddress(other),"Distinct owner address");Check(!v.Data48.SameAddress(null),"Null address distinct");Equal(0x48u,NavigationCrowdAgentDataView.AgentOffset,"Native displacement");
   Same(a.Position48,v.Data48.BorrowPoint(0,40),"Position point");Same(a.Heading54,v.Data48.BorrowPoint(0xc,41),"Movement direction point");Same(a.Vector60,v.Data48.BorrowPoint(0x18,42),"Facing direction point");
   var points=new[]{a.Position48,a.Heading54,a.Vector60};for(uint p=0;p<3;p++){var cells=new[]{points[p].X,points[p].Y,points[p].Z};for(uint c=0;c<3;c++){uint offset=p*12+c*4;Same(cells[c],v.Data48.Cell(offset,43),"Original scalar");v.Data48.Write(offset,offset+1,44);Equal((float)(offset+1),cells[c].Read(0),"View write shared");}}
   a.Vector60.Z.Write(99);Equal(99f,b.Read(0x20,45),"Native data+20 uses agent+68");Equal(21f,b.Read(0x14,46),"Heading distinct from facing");Unknown(a.Timer6C,"No read beyond data extent");
  });
  Test("Borrowed data opaque copies preserve unknown and actual read points",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdAgentDataView(a);var captured=v.Snapshot(0x18,50);Unknown(captured,"Unknown facing snapshot");a.Vector60.X.Write(7);v.CopyFrom(0,captured,51);Unknown(a.Position48.X,"Unknown copied to position");Equal(7f,v.Read(0x18,52),"Source live write retained");Missing(53,delegate{v.Read(0,53);},"No unknown-to-zero conversion");
   Missing(54,delegate{v.Cell(0x24,54);},"Speed is outside data view");Missing(55,delegate{v.Cell(2,55);},"Unaligned extent");Missing(56,delegate{v.BorrowPoint(4,56);},"Only complete declared points");
  });
  Test("Released owner rejects existing and newly formed views without modifying raw state",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdTickStateView(a);a.Timer6C.Write(0x3f800000);a.WordDC.Write(0);a.Position48.X.Write(2);a.MarkReleased();var later=new NavigationCrowdTickStateView(a);Check(v.Data48.SameAddress(later.Data48),"Address formation does not dereference");
   Missing(60,delegate{v.Speed6C.Read(60);},"Released float read");Missing(61,delegate{later.Speed6C.Write(9,61);},"Released float write");Missing(62,delegate{v.SpeedAndElapsed.WriteElapsedE0(4,62);},"Released packed write");Missing(63,delegate{v.Data48.Read(0,63);},"Released data read");Missing(64,delegate{later.Data48.CopyFrom(0,Field<float>.Of(8),64);},"Released data copy");
   Equal(0x3f800000u,a.Timer6C.Read(0),"Rejected write retains raw speed");Equal(0UL,a.WordDC.Read(0),"Rejected write retains packed word");Equal(2f,a.Position48.X.Read(0),"Rejected write retains position");
  });
  Test("Released selected point does not invalidate unrelated borrowed points",delegate{
   var a=new NavigationCrowdAgent();var v=new NavigationCrowdAgentDataView(a);a.Position48.X.Write(3);a.Vector60.MarkReleased();Equal(3f,v.Read(0,70),"Other point remains available");Missing(71,delegate{v.Cell(0x18,71);},"Selected point lifetime");Unknown(a.Timer6C,"No unrelated initialization");
  });
 }
 public static int Main(){try{Run();Console.WriteLine("PASS NavigationCrowdTickViews scenarios="+scenarios+" assertions="+assertions+"; shared views only; no tick or game acceptance");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
