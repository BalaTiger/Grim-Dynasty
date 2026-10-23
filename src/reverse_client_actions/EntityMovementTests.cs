using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Pure-memory fixtures for the seven recovered Entity/Actor/Engine/WorldVec3
// getters. They do not register a loaded player, publish movement, supply a
// parent transform callback, or validate native allocation/lifetime/FP modes.
// Shared same-type cells below are declared adapter aliases, not evidence for
// arbitrary shifted/cross-type native byte aliases.
public static class EntityMovementTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected Unsupported: "+why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected own identity rejection: "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static Field<float>[] Basis(){var a=new Field<float>[9];for(int n=0;n<9;n++)a[n]=new Field<float>();return a;}
 static EntityMovementState Entity(){return new EntityMovementState(new object());}
 static void Position(WorldPosition24 p,ulong region,float x,float y,float z){p.Q0.Write(region);p.D8.Write(Bits(x));p.DC.Write(Bits(y));p.D10.Write(Bits(z));}
 static void Fill(EntityCoordinates64 frame){Position(frame.Position00,0x123456789abcdef0ul,1,2,3);frame.Position00.D14.Write(0x11223344);var b=frame.Basis18;for(int n=0;n<9;n++)b[n].Write(n+11);frame.Tail3C.Write(0xaabbccdd);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static void PointIs(NavigationPoint3 p,float x,float y,float z,string why){Equal(Bits(x),Bits(p.X.Read(0)),why+" X");Equal(Bits(y),Bits(p.Y.Read(0)),why+" Y");Equal(Bits(z),Bits(p.Z.Read(0)),why+" Z");}
 sealed class WorldFixture {
  public readonly WorldCoordinateState Regions=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldPosition24 Position=new WorldPosition24();
  public readonly NavigationPoint3 Output=Point(91,92,93);
  public WorldFixture(){Regions.AttachRegion(7,Region);EntityMovementTests.Position(Position,7,1,2,3);Region.Offset3C.Write(10);Region.Offset40.Write(20);Region.Offset44.Write(30);}
  public NavigationPoint3 Get(){return EntityMovementContracts.GetWorldPosition22C1A0(Position,Output,Regions,new Invocation());}
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Coordinate views retain authoritative shared cells and unknown payload",delegate{
   var p=new WorldPosition24();var basis=Basis();var tail=new Field<uint>();var frame=new EntityCoordinates64(p,basis,tail);var alias=new EntityCoordinates64(frame);
   Same(p,frame.Position00,"Original position view");Same(p.AddressIdentity,frame.AddressIdentity,"Original address identity");Same(frame.AddressIdentity,alias.AddressIdentity,"Full same-address alias");Same(frame.Position00,alias.Position00,"Alias shares position");Same(tail,alias.Tail3C,"Alias shares byte60..63 cell");
   Same(basis[6],frame.Heading30.X,"Heading X is basis6");Same(basis[7],frame.Heading30.Y,"Heading Y is basis7");Same(basis[8],frame.Heading30.Z,"Heading Z is basis8");
   var slots=frame.Basis18;slots[0]=new Field<float>();basis[1]=new Field<float>();Check(!Object.ReferenceEquals(slots[0],frame.Basis18[0]),"Exposed array cannot replace slot");Check(!Object.ReferenceEquals(basis[1],frame.Basis18[1]),"Constructor also protects slot map");
   Unknown(p.Q0,"No default region");Unknown(frame.Basis18[0],"No default orientation");Unknown(tail,"No default tail");frame.Heading30.X.Write(12);Equal(12f,alias.Basis18[6].Read(0),"Live shared heading, no mirror synchronization");
  });
  Test("No-parent GetCoords returns hidden64 and propagates unknown through all four loads",delegate{
   var e=Entity();e.Parent130.Write(null);Fill(e.CoordsD8);e.CoordsD8.Position00.D14.Forget();e.CoordsD8.Basis18[4].Forget();var output=new EntityCoordinates64();Fill(output);var i=new Invocation();
   Same(output,EntityMovementContracts.GetCoords07F070(e,output,i),"Exact hidden return identity");Equal(0x07f119u,i.At,"Last64-byte destination store");
   Equal(0x123456789abcdef0ul,output.Position00.Q0.Read(0),"Region word copied");Equal(Bits(3),output.Position00.D10.Read(0),"Position tail copied");Unknown(output.Position00.D14,"Unknown padding overwrites known output");Unknown(output.Basis18[4],"Unknown basis copied without arithmetic");
   Equal(19f,output.Heading30.Z.Read(0),"Last basis scalar at56 copied");Equal(0xaabbccddu,output.Tail3C.Read(0),"Fourth load includes final DWORD, not only60 bytes");Unknown(e.Flag13C,"No-parent branch does not consume flagC");Unknown(e.Flag13D,"No-parent branch does not consume flagD");
   Check(!Object.ReferenceEquals(e.CoordsD8.Basis18[8],output.Basis18[8]),"Ordinary hidden result owns different cells");output.Heading30.Z.Write(55);Equal(19f,e.CoordsD8.Heading30.Z.Read(0),"Copy does not alias source");
  });
  Test("Full same-address GetCoords copy is stable including unknown and tail",delegate{
   var e=Entity();e.Parent130.Write(null);Fill(e.CoordsD8);e.CoordsD8.Basis18[2].Forget();var same=new EntityCoordinates64(e.CoordsD8);
   Same(same,EntityMovementContracts.GetCoords07F070(e,same,new Invocation()),"Exact alias object returned");Equal(0x11223344u,e.CoordsD8.Position00.D14.Read(0),"Padding survives self copy");Unknown(same.Basis18[2],"Unknown remains unknown");Equal(0xaabbccddu,same.Tail3C.Read(0),"Final DWORD survives");Same(e.CoordsD8.Heading30,same.Heading30,"Borrowed heading identity survives");
  });
  Test("Declared shared basis cells expose16-byte snapshots and subsequent load timing",delegate{
   var sourceCells=Basis();for(int n=0;n<9;n++)sourceCells[n].Write(n+1);
   var source=new EntityCoordinates64(new WorldPosition24(),sourceCells,new Field<uint>());var e=new EntityMovementState(new object(),source,new NavigationPoint3());e.Parent130.Write(null);
   var targets=new Field<float>[]{sourceCells[1],sourceCells[0],sourceCells[3],sourceCells[2],sourceCells[6],sourceCells[5],sourceCells[4],sourceCells[8],sourceCells[7]};
   var output=new EntityCoordinates64(new WorldPosition24(),targets,new Field<uint>());EntityMovementContracts.GetCoords07F070(e,output,new Invocation());
   Equal(2f,sourceCells[0].Read(0),"Second load snapshots both cells before swap");Equal(1f,sourceCells[1].Read(0),"First swap value");Equal(4f,sourceCells[2].Read(0),"Third load snapshots complete16 bytes");Equal(3f,sourceCells[3].Read(0),"Third load partner");
   Equal(5f,output.Basis18[6].Read(0),"Fourth load sees earlier store into source basis6; no whole-frame precopy");Equal(8f,output.Basis18[7].Read(0),"Fourth load snapshots before own swap");Equal(9f,output.Basis18[8].Read(0),"Fourth load second swap value");Unknown(output.Tail3C,"Final unknown DWORD copied");
  });
  Test("Parent branch consumes flags in order then stops at actual unclosed virtual slot",delegate{
   var e=Entity();var output=new EntityCoordinates64();Fill(output);Missing(0x07f080,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Unknown parent is not null");var parent=Entity();e.Parent130.Write(parent);
   Missing(0x07f085,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"First child flag");e.Flag13C.Write(1);
   Missing(0x07f0a9,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Captured unknown vptr not consumed before flagD");e.Flag13D.Write(0);
   Missing(0x07f0b7,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Unknown vptr consumed at slot188");parent.Virtual00.Write(new object());
   Missing(0x07f0b7,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Known vptr does not fabricate parent transform");e.Flag13C.Write(0);e.Flag13D.Forget();
   Missing(0x07f0bf,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Alternative flagD site");e.Flag13D.Write(1);
   Missing(0x07f0cd,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Slot180 is also unclosed");Equal(0xaabbccddu,output.Tail3C.Read(0),"Parent stops never write hidden tail");Equal(0x123456789abcdef0ul,output.Position00.Q0.Read(0),"Hidden first word retained");
   parent.MarkReleased();Missing(0x07f093,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Explicit parent invalidation at vptr load");
  });
  Test("Coordinate source and hidden lifetimes stop at their actual first access",delegate{
   var e=Entity();e.Parent130.Write(null);var output=new EntityCoordinates64();Fill(output);e.CoordsD8.MarkReleased();Missing(0x07f0ef,delegate{EntityMovementContracts.GetCoords07F070(e,output,new Invocation());},"Released source before hidden write");Equal(0xaabbccddu,output.Tail3C.Read(0),"Hidden retained");
   var live=Entity();live.Parent130.Write(null);var dead=new EntityCoordinates64();var alias=new EntityCoordinates64(dead);dead.MarkReleased();Missing(0x07f0f9,delegate{EntityMovementContracts.GetCoords07F070(live,alias,new Invocation());},"Full alias shares own lifetime");Missing(0x07f0f9,delegate{EntityMovementContracts.GetCoords07F070(live,null,new Invocation());},"Null hidden stops at first store");
  });
  Test("GetRegion follows current parent identity and reads only final root word",delegate{
   var child=Entity();var mid=Entity();var root=Entity();child.Parent130.Write(mid);mid.Parent130.Write(root);root.Parent130.Write(null);root.CoordsD8.Position00.Q0.Write(0xfeedbeeful);
   Equal(0xfeedbeeful,EntityMovementContracts.GetRegion07F130(child,new Invocation()),"Root region returned");Unknown(child.CoordsD8.Position00.Q0,"Child local region not consumed");Unknown(mid.CoordsD8.Position00.Q0,"Intermediate local region not consumed");Unknown(root.Flag13C,"No parent-coordinate flag path");
   root.CoordsD8.Position00.Q0.Write(0);Equal(0ul,EntityMovementContracts.GetRegion07F130(child,new Invocation()),"Known-null root pointer is returned, not rejected");child.Parent130.Write(null);child.CoordsD8.Position00.Q0.Write(9);Equal(9ul,EntityMovementContracts.GetRegion07F130(child,new Invocation()),"Subsequent call sees changed parent, no cached ancestry");
  });
  Test("Region unknowns and own cycle guard never turn into a default region",delegate{
   var e=Entity();Missing(0x07f137,delegate{EntityMovementContracts.GetRegion07F130(e,new Invocation());},"Initial unknown parent");var p=Entity();e.Parent130.Write(p);Missing(0x07f14a,delegate{EntityMovementContracts.GetRegion07F130(e,new Invocation());},"Ancestor unknown parent");p.Parent130.Write(null);
   Missing(0x07f14f,delegate{EntityMovementContracts.GetRegion07F130(e,new Invocation());},"Final unknown region");p.Parent130.Write(e);Missing(0x07f143,delegate{EntityMovementContracts.GetRegion07F130(e,new Invocation());},"Own acyclic traversal guard, not native return status");
  });
  Test("GetMotion is a borrowed LEA view with consumption deferred to its caller",delegate{
   var e=Entity();var motion=EntityMovementContracts.GetMotion07EB20(e,new Invocation());Same(e.Motion1D0,motion,"Exact inline motion identity");Unknown(motion.X,"Getter does not require X known");motion.Y.Write(12);Equal(12f,e.Motion1D0.Y.Read(0),"Borrowed write visible to same owner");
   e.MarkReleased();Same(motion,EntityMovementContracts.GetMotion07EB20(e,new Invocation()),"LEA still returns explicit released own address");Missing(0x1234,delegate{motion.RequireLive(0x1234);},"Consumer checks lifetime");Missing(0x07eb20,delegate{EntityMovementContracts.GetMotion07EB20(null,new Invocation());},"Null own view is not fabricated");
  });
  Test("World position uses signed32 offsets and separate binary32 conversion and add",delegate{
   var f=new WorldFixture();f.Region.Offset3C.Write(16777217);f.Region.Offset40.Write(-20);f.Region.Offset44.Write(-16777217);Position(f.Position,7,1,2.5f,-1);var i=new Invocation();
   Same(f.Output,EntityMovementContracts.GetWorldPosition22C1A0(f.Position,f.Output,f.Regions,i),"Exact12-byte hidden identity");PointIs(f.Output,16777216f,-17.5f,-16777216f,"Convert then add, not integer/double combined sum");Equal(0x22c1efu,i.At,"Y is final store");Unknown(f.Position.D14,"Position padding not consumed");Equal(Bits(1),f.Position.D8.Read(0),"Caller position not published/mutated");
   Position(f.Position,7,Single.NaN,2,Single.PositiveInfinity);f.Get();Check(Single.IsNaN(f.Output.X.Read(0)),"NaN not replaced with default");Equal(Single.PositiveInfinity,f.Output.Z.Read(0),"Infinity propagated within own ordinary FP domain");
  });
  Test("World conversion failures retain only actual X then Z store prefixes",delegate{
   var x=new WorldFixture();x.Region.Offset3C.Forget();x.Position.D8.Forget();Missing(0x22c1be,delegate{x.Get();},"Captured offset X first arithmetic consumption");PointIs(x.Output,91,92,93,"No writes");x.Region.Offset3C.Write(10);Missing(0x22c1ca,delegate{x.Get();},"Position X at ADDSS");
   var oz=new WorldFixture();oz.Region.Offset44.Forget();Missing(0x22c1cf,delegate{oz.Get();},"Z conversion occurs before X store");PointIs(oz.Output,91,92,93,"Z offset failure blocks even X store");
   var z=new WorldFixture();z.Position.D10.Forget();Missing(0x22c1d6,delegate{z.Get();},"Z local after X store");PointIs(z.Output,11,92,93,"X-only prefix");
   var oy=new WorldFixture();oy.Region.Offset40.Forget();Missing(0x22c1e2,delegate{oy.Get();},"Y conversion before Z store");PointIs(oy.Output,11,92,93,"Known Z sum still not stored before Y conversion");
   var y=new WorldFixture();y.Position.DC.Forget();Missing(0x22c1ea,delegate{y.Get();},"Y local after X/Z stores");PointIs(y.Output,11,92,33,"XZ prefix");
  });
  Test("World hidden output honors X Z Y order and actual limited output extent",delegate{
   var f=new WorldFixture();var shared=new Field<float>();var z=new Field<float>();var aliased=new NavigationPoint3(shared,shared,z);EntityMovementContracts.GetWorldPosition22C1A0(f.Position,aliased,f.Regions,new Invocation());Equal(22f,shared.Read(0),"Y overwrites earlier X in declared same-type output alias");Equal(33f,z.Read(0),"Z was written between them");
   var cells=new Field<float>[]{new Field<float>(),new Field<float>()};cells[0].Write(91);cells[1].Write(92);var shortOutput=new NavigationPointBuffer(cells).PointAtScalar(0,0);Missing(0x22c1e5,delegate{EntityMovementContracts.GetWorldPosition22C1A0(f.Position,shortOutput,f.Regions,new Invocation());},"Own missing Z address at actual store");Equal(11f,cells[0].Read(0),"X retained before Z address failure");Equal(92f,cells[1].Read(0),"Y not written yet");
   var released=new NavigationPoint3();released.MarkReleased();Missing(0x22c1d2,delegate{EntityMovementContracts.GetWorldPosition22C1A0(f.Position,released,f.Regions,new Invocation());},"Released output at first store");
  });
  Test("World region lookup rejects unknown null and unbound identity before coordinate use",delegate{
   var f=new WorldFixture();f.Position.Q0.Forget();f.Position.D8.Forget();Missing(0x22c1a7,delegate{f.Get();},"Unknown captured region at dereference");f.Position.Q0.Write(0);Missing(0x22c1a7,delegate{f.Get();},"No invented zero-region world origin");f.Position.Q0.Write(99);Missing(0x22c1a7,delegate{f.Get();},"Unknown identity not auto-created");PointIs(f.Output,91,92,93,"All lookup failures leave hidden untouched");
  });
  Test("Actor height multiplies observed scalars without defaults or extra precision",delegate{
   var e=Entity();Missing(0x02b468,delegate{EntityMovementContracts.GetHeight02B460(e,new Invocation());},"MOV is opaque, arithmetic consumes missing height");e.Height4A0.Write(2);Missing(0x02b468,delegate{EntityMovementContracts.GetHeight02B460(e,new Invocation());},"Unknown scale not default one");
   e.Height4A0.Write(Float(0x3f800001));e.Scale3AC.Write(Float(0x3f800001));Equal(0x3f800002u,Bits(EntityMovementContracts.GetHeight02B460(e,new Invocation())),"Binary32 product rounding");e.Height4A0.Write(Float(0x80000000));e.Scale3AC.Write(2);Equal(0x80000000u,Bits(EntityMovementContracts.GetHeight02B460(e,new Invocation())),"Signed zero retained");e.Height4A0.Write(Single.PositiveInfinity);e.Scale3AC.Write(0);Check(Single.IsNaN(EntityMovementContracts.GetHeight02B460(e,new Invocation())),"Infinity times zero is not default height");e.MarkReleased();Missing(0x02b460,delegate{EntityMovementContracts.GetHeight02B460(e,new Invocation());},"Own entity lifetime at source access");
  });
  Test("Frame getters and registry preserve unsigned values and exact shared owner identity",delegate{
   var e=Entity();var engine=new EntityEngineState(new WorldEngine());var registry=new EntityMovementRegistry();registry.Attach(e);registry.Attach(e);registry.AttachEngine(engine);registry.AttachEngine(engine);Same(e,registry.Entity(e.Identity,1),"One authoritative entity view");Same(engine,registry.Engine(engine.Identity,2),"One authoritative engine view");
   Invalid(delegate{registry.Attach(new EntityMovementState(e.Identity));},"No second mirror for same entity");Invalid(delegate{registry.AttachEngine(new EntityEngineState(engine.Identity));},"No engine rebind");Missing(3,delegate{registry.Entity(new object(),3);},"Absent entity cannot default");Missing(4,delegate{registry.Engine(new WorldEngine(),4);},"Absent engine cannot default");
   Missing(0x07e7c0,delegate{EntityMovementContracts.GetLastFrameUpdated07E7C0(e,new Invocation());},"Unknown last frame");Missing(0x077880,delegate{EntityMovementContracts.GetFrameCount077880(engine,new Invocation());},"Unknown engine counter");e.LastFrame178.Write(UInt32.MaxValue);engine.FrameCount640.Write(0x80000001);Equal(UInt32.MaxValue,EntityMovementContracts.GetLastFrameUpdated07E7C0(registry.Entity(e.Identity,0),new Invocation()),"Unsigned last frame unchanged");Equal(0x80000001u,EntityMovementContracts.GetFrameCount077880(registry.Engine(engine.Identity,0),new Invocation()),"Unsigned engine count unchanged");Equal(UInt32.MaxValue,e.LastFrame178.Read(0),"Getter never increments or publishes frame");engine.MarkReleased();Missing(0x077880,delegate{EntityMovementContracts.GetFrameCount077880(engine,new Invocation());},"Explicit engine lifetime");
  });
 }
}
public static class EntityMovementTestProgram {
 public static int Main(){try{EntityMovementTests.Run();Console.WriteLine("PASS "+EntityMovementTests.Scenarios+" scenarios, "+EntityMovementTests.Assertions+" assertions; recovered getter contracts in own memory views only; no parent-transform success, loaded player or movement publication");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
