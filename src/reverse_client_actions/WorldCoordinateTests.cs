using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Pure-memory tests of observed AABB/Translate(true) and floor early prefixes.
// Masks written by OwnFixture are explicit test inputs, not assertions about
// original loaded globals. No callback supplies a floor or region success.
// The successful 24-byte floor copy remains unreachable in this recovered
// production domain; observing its 20-byte temporary is not floor validation.
public static class WorldCoordinateTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected unsupported observation: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static WorldPosition24 Point(ulong region,float x,float y,float z){var p=new WorldPosition24();p.Q0.Write(region);XYZ(p,x,y,z);return p;}
 static void XYZ(WorldPosition24 p,float x,float y,float z){p.D8.Write(Bits(x));p.DC.Write(Bits(y));p.D10.Write(Bits(z));}
 static Displacement3 Delta(float x,float y,float z){var d=new Displacement3();d.X0.Write(x);d.Y4.Write(y);d.Z8.Write(z);return d;}
 static void Position(WorldPosition24 p,float x,float y,float z,string why){Equal(Bits(x),p.D8.Read(0),why+" X bits");Equal(Bits(y),p.DC.Read(0),why+" Y bits");Equal(Bits(z),p.D10.Read(0),why+" Z bits");}
 static void Bounds(WorldBounds24 box,float x,float y,float z,float hx,float hy,float hz){box.Center00.Write(x);box.Center04.Write(y);box.Center08.Write(z);box.Extent0C.Write(hx);box.Extent10.Write(hy);box.Extent14.Write(hz);}
 sealed class OwnFixture {
  public const ulong RegionKey=0x12345678;
  public readonly WorldCoordinateState State=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly Field<WorldEngine> Engine=new Field<WorldEngine>();
  public readonly WorldCoordinateBoundaries B;
  public readonly WorldPosition24 P=Point(RegionKey,1,2,3);
  public OwnFixture(){State.AttachRegion(RegionKey,Region);B=new WorldCoordinateBoundaries(State,Engine);B.XorMask34C450.Write(0x80000000);Bounds(Region.Bounds70,0,0,0,10,10,10);}
  public void World(){var engine=new WorldEngine();engine.World50.Write(new object());Engine.Write(engine);}
  public byte Contains(WorldPosition24 point){return WorldCoordinateContracts.Contains0664D0(Region.Bounds70,point,B,new Invocation());}
  public byte Translate(Displacement3 delta){return WorldCoordinateContracts.TranslateTrue22C350(P,delta,B,new Invocation());}
  public byte Floor(){return WorldCoordinateContracts.PutOnFloor22C620(P,B,new Invocation());}
 }

 public static void Run(){
  scenarios=0;assertions=0;
  Test("AABB includes both faces and rejects each exterior using an explicit own sign mask",delegate{
   var f=new OwnFixture();Bounds(f.Region.Bounds70,10,20,30,2,3,4);var p=Point(0,10,20,30);p.Q0.Forget();
   Equal((byte)1,f.Contains(p),"Center inside");
   for(int axis=0;axis<3;axis++){for(int sign=-1;sign<=1;sign+=2){float[] v={10,20,30};float[] ext={2,3,4};v[axis]+=sign*ext[axis];XYZ(p,v[0],v[1],v[2]);Equal((byte)1,f.Contains(p),"Inclusive face axis "+axis+" sign "+sign);v[axis]+=sign*0.25f;XYZ(p,v[0],v[1],v[2]);Equal((byte)0,f.Contains(p),"Outside face axis "+axis+" sign "+sign);}}
   Unknown(p.Q0,"Contains never consumes region pointer");Unknown(p.D14,"Contains never consumes padding");
  });
  Test("AABB reads extentX then Y Z and centers before deciding outsideX",delegate{
   var f=new OwnFixture();var box=f.Region.Bounds70;var p=Point(0,100,0,0);box.Extent0C.Forget();p.DC.Forget();p.D10.Forget();
   Missing(0x0664d4,delegate{f.Contains(p);},"Extent X first");box.Extent0C.Write(10);
   Missing(0x0664d9,delegate{f.Contains(p);},"Point Y before point X");p.DC.Write(Bits(0));
   Missing(0x0664de,delegate{f.Contains(p);},"Point Z before center Y");p.D10.Write(Bits(0));box.Center04.Forget();box.Center08.Forget();
   Missing(0x0664e3,delegate{f.Contains(p);},"Center Y before center Z");box.Center04.Write(0);
   Missing(0x0664e8,delegate{f.Contains(p);},"Center Z before point X");box.Center08.Write(0);p.D8.Forget();box.Center00.Forget();
   Missing(0x0664f1,delegate{f.Contains(p);},"Point X delayed load");p.D8.Write(Bits(100));
   Missing(0x0664f5,delegate{f.Contains(p);},"Center X final subtraction");box.Center00.Write(0);box.Extent10.Forget();box.Extent14.Forget();f.B.XorMask34C450.Forget();
   Equal((byte)0,f.Contains(p),"Outside X now returns before late extents and mask");
  });
  Test("AABB upper Y and Z failures do not require later fields or lower mask",delegate{
   var f=new OwnFixture();var p=Point(0,0,11,0);f.Region.Bounds70.Extent14.Forget();f.B.XorMask34C450.Forget();
   Equal((byte)0,f.Contains(p),"Outside Y skips extent Z and mask");f.Region.Bounds70.Extent14.Write(10);XYZ(p,0,0,11);Equal((byte)0,f.Contains(p),"Outside Z skips mask");XYZ(p,0,0,0);
   Missing(0x066512,delegate{f.Contains(p);},"Lower comparisons require explicitly observed mask");
  });
  Test("AABB unordered upper comparisons continue but unordered lower comparisons reject",delegate{
   var f=new OwnFixture();var p=Point(0,0,0,0);
   for(int axis=0;axis<3;axis++){float[] v={0,0,0};v[axis]=Single.NaN;XYZ(p,v[0],v[1],v[2]);Equal((byte)0,f.Contains(p),"NaN point component rejected by lower comparison "+axis);}
   XYZ(p,Single.NaN,0,0);f.Region.Bounds70.Extent10.Forget();Missing(0x0664fe,delegate{f.Contains(p);},"NaN X does not bypass later upper-field reads");f.Region.Bounds70.Extent10.Write(10);f.B.XorMask34C450.Forget();Missing(0x066512,delegate{f.Contains(p);},"NaN still reaches mask load before lower reject");
   f.B.XorMask34C450.Write(0x80000000);XYZ(p,0,0,0);f.Region.Bounds70.Extent0C.Write(Single.NaN);Equal((byte)0,f.Contains(p),"NaN extent rejected, no invented valid box");
  });
  Test("AABB uses the supplied XOR bits instead of substituting unary negation",delegate{
   var observed=new WorldCoordinateBoundaries(new WorldCoordinateState());Unknown(observed.XorMask34C450,"No implicit default mask");observed.UseObservedBoundsMask();Equal(0x80000000u,observed.XorMask34C450.Read(0),"Explicit initializer uses the separately inspected raw mask");
   var f=new OwnFixture();Bounds(f.Region.Bounds70,0,0,0,1,1,1);f.B.XorMask34C450.Write(0); // Deliberately alternate OWN fixture mask.
   Equal((byte)0,f.Contains(Point(0,0,0,0)),"Zero mask leaves positive lower thresholds");Equal((byte)1,f.Contains(Point(0,1,1,1)),"Equal positive upper/lower thresholds pass");
   f.B.XorMask34C450.Forget();Missing(0x066512,delegate{f.Contains(Point(0,1,1,1));},"Missing mask cannot silently revert to sign bit");
  });
  Test("Translate true updates the same position cells and preserves region and padding",delegate{
   var f=new OwnFixture();var alias=new WorldPosition24(f.P);f.P.D14.Write(0xdeadbeef);
   Equal((byte)1,f.Translate(Delta(0.5f,-1,2)),"Within-region production translate");Position(f.P,1.5f,1,5,"Sequential XYZ result");Equal(OwnFixture.RegionKey,f.P.Q0.Read(0),"Same region");Equal(0xdeadbeefu,f.P.D14.Read(0),"Padding not rewritten");Same(alias.D8,f.P.D8,"Same-address X cell");Same(alias.Q0,f.P.Q0,"Same-address region cell");Position(alias,1.5f,1,5,"Alias sees real writes");Unknown(f.Engine,"Same-region result did not consume global engine");
  });
  Test("Translate null-region early return does not consume coordinates or delta",delegate{
   var f=new OwnFixture();f.P.Q0.Write(0);f.P.D8.Forget();f.P.DC.Forget();f.P.D10.Forget();
   Equal((byte)0,f.Translate(null),"Known-null region returns false before delta access");Unknown(f.P.D8,"X unchanged unknown");Unknown(f.P.D14,"Padding unchanged unknown");f.P.Q0.Forget();Missing(0x22c35e,delegate{f.Translate(null);},"Unknown region is not known null");
  });
  Test("Translate unknown Y and Z retain actual earlier component stores",delegate{
   var f=new OwnFixture();var d=Delta(4,5,6);d.Y4.Forget();f.P.D14.Write(0x10203040);Missing(0x22c466,delegate{f.Translate(d);},"Y is consumed after X store despite earlier opaque vector copy");Position(f.P,5,2,3,"X-only prefix");Equal(0x10203040u,f.P.D14.Read(0),"Padding survives failure");
   XYZ(f.P,1,2,3);d.Y4.Write(5);d.Z8.Forget();Missing(0x22c47a,delegate{f.Translate(d);},"Z is consumed after Y store");Position(f.P,5,7,3,"XY prefix");Equal(OwnFixture.RegionKey,f.P.Q0.Read(0),"Failure did not replace region");
  });
  Test("Unregistered region stops only at AABB after all translation writes",delegate{
   var f=new OwnFixture();f.P.Q0.Write(0x87654321);Missing(0x0664d4,delegate{f.Translate(Delta(1,2,3));},"No early invented region lookup");Position(f.P,2,4,6,"XYZ completed before missing bounds");Equal(0x87654321ul,f.P.Q0.Read(0),"Unresolved region identity retained");Unknown(f.P.D14,"No default padding");
  });
  Test("Out-of-region translation retains XYZ and stops at the real world dependency",delegate{
   var a=new OwnFixture();Missing(0x22c58f,delegate{a.Translate(Delta(20,0,0));},"Unknown engine pointer is consumed at its plus50 dereference");Position(a.P,21,2,3,"Translated outside before global failure");
   var b=new OwnFixture();b.Engine.Write(new WorldEngine());Missing(0x22c593,delegate{b.Translate(Delta(20,0,0));},"Opaque unknown world pointer reaches the unresolved callee");Position(b.P,21,2,3,"XYZ before unresolved world call");Unknown(b.Engine.Read(0).World50,"Argument transfer did not invent world pointer knowledge");
   var c=new OwnFixture();c.World();Missing(0x22c593,delegate{c.Translate(Delta(20,0,0));},"Unrecovered containing-region call is not fabricated false or success");Position(c.P,21,2,3,"XYZ prefix survives unsupported call");Equal(OwnFixture.RegionKey,c.P.Q0.Read(0),"No made-up destination region");
   var n=new OwnFixture();n.World();Missing(0x22c593,delegate{n.Translate(Delta(Single.NaN,0,0));},"NaN AABB rejection also reaches real unresolved world search");Check(Single.IsNaN(BitConverter.ToSingle(BitConverter.GetBytes(n.P.D8.Read(0)),0)),"NaN write is preserved, not clamped to zero");
  });
  Test("Floor wrapper distinguishes unknown region null early return and direct receiver",delegate{
   var f=new OwnFixture();f.P.Q0.Forget();Missing(0x22c626,delegate{f.Floor();},"Unknown wrapper region");f.P.Q0.Write(0);f.P.D8.Forget();f.P.DC.Forget();f.P.D10.Forget();
   Equal((byte)0,f.Floor(),"Known-null wrapper region returns false");Same(null,f.B.LastFloorTemporary,"No temporary was constructed");Unknown(f.Engine,"No engine lookup for null wrapper region");
   Missing(0x221dbb,delegate{WorldCoordinateContracts.WorldPutOnFloor221DA0(null,f.P,f.B,new Invocation());},"Direct world helper dereferences its region and has no wrapper null guard");
  });
  Test("Floor temporary exists before global reads and shares no caller cells",delegate{
   var f=new OwnFixture();f.P.D14.Write(0xaabbccdd);Missing(0x22c654,delegate{f.Floor();},"Unknown engine consumed after completed temporary copy");var temp=f.B.LastFloorTemporary;
   Check(temp!=null,"Actual diagnostic temporary retained");Check(!Object.ReferenceEquals(temp,f.P),"Fresh stack-record projection");Check(!Object.ReferenceEquals(temp.Q0,f.P.Q0)&&!Object.ReferenceEquals(temp.D8,f.P.D8),"Independent temporary cells");Equal(OwnFixture.RegionKey,temp.Q0.Read(0),"Region copied");Position(temp,1,2,3,"Exactly copied payload");Unknown(temp.D14,"Caller padding was not copied into 20-byte temporary");Equal(0xaabbccddu,f.P.D14.Read(0),"No 24-byte success writeback happened");Same(f.Engine,f.B.EngineGlobal,"Explicit shared engine-field identity");
   var engine=new WorldEngine();f.Engine.Write(engine);f.Region.Unavailable90.Write(1);Equal((byte)0,f.Floor(),"Current shared engine is observed while the world argument remains opaque");Unknown(engine.World50,"Early callee does not require world pointer knowledge");Check(!Object.ReferenceEquals(temp,f.B.LastFloorTemporary),"A subsequent call constructs a fresh local");Position(f.P,1,2,3,"Original unchanged after unsupported and early-false outcomes");
  });
  Test("Production unavailable-region floor returns false without consuming temporary coordinates",delegate{
   var f=new OwnFixture();f.World();f.Region.Unavailable90.Write(1);f.P.D8.Forget();f.P.DC.Forget();f.P.D10.Forget();f.P.D14.Write(0x55667788);
   Equal((byte)0,f.Floor(),"Observed early-false branch");var temp=f.B.LastFloorTemporary;Equal(OwnFixture.RegionKey,temp.Q0.Read(0),"Temporary region identity");Unknown(temp.D8,"Opaque X copy stays unknown");Unknown(temp.DC,"Opaque Y copy stays unknown");Unknown(temp.D10,"Opaque Z copy stays unknown");Unknown(temp.D14,"Uninitialized temporary padding remains unknown");Unknown(f.P.D8,"No original coordinate writeback on false");Equal(0x55667788u,f.P.D14.Read(0),"Original padding retained");Unknown(f.Region.Critical18,"Early false did not read lock pointer");
   f.P.Q0.Write(0);Equal((byte)0,f.Floor(),"Later null wrapper call also early false");Same(temp,f.B.LastFloorTemporary,"Null-region call does not invent a fresh local observation");
  });
  Test("World floor early failure does not require a world object",delegate{
   var f=new OwnFixture();f.Region.Unavailable90.Write(1);f.P.D8.Forget();f.P.DC.Forget();f.P.D10.Forget();Equal((byte)0,WorldCoordinateContracts.WorldPutOnFloor221DA0(null,f.P,f.B,new Invocation()),"World receiver is unused on the actual unavailable branch");Unknown(f.P.D8,"No coordinate consumption");Unknown(f.Engine,"Direct helper did not invent global access");
  });
  Test("Floor unknown availability and critical-section boundary preserve the caller",delegate{
   var f=new OwnFixture();f.World();f.P.D14.Write(0x31415926);Missing(0x221dbb,delegate{f.Floor();},"Unknown unavailable byte is not default available");Position(f.P,1,2,3,"Original unchanged before availability decision");
   f.Region.Unavailable90.Write(0);Missing(0x221dce,delegate{f.Floor();},"Opaque unknown critical-section pointer reaches the unsupported lock call");Unknown(f.Region.Critical18,"No invented critical-section object");f.Region.Critical18.Write(new object());Missing(0x221dce,delegate{f.Floor();},"Known pointer still cannot fabricate a successful lock import");Position(f.P,1,2,3,"Original unchanged after lock stop");Equal(0x31415926u,f.P.D14.Read(0),"Padding unchanged after all stops");Unknown(f.B.LastFloorTemporary.D14,"Temporary padding remains unobserved at stop");
  });
 }
}
public static class WorldCoordinateTestProgram {
 public static int Main(){try{WorldCoordinateTests.Run();Console.WriteLine("PASS "+WorldCoordinateTests.Scenarios+" scenarios, "+WorldCoordinateTests.Assertions+" assertions; production AABB and Translate(true), floor early-failure/unsupported prefixes; no successful floor or 24-byte floor-writeback validation");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+WorldCoordinateTests.Scenarios+" scenarios, "+WorldCoordinateTests.Assertions+" assertions: "+e);return 1;}}
}
