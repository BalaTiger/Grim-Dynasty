using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Only new22C690 cases. Explicit own fields, canonical region bindings and the
// existing production Translate are used; no callback injects successful motion.
public static class WorldPositionInitializationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected unsupported observation: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static void XYZ(WorldPosition24 p,float x,float y,float z){p.D8.Write(Bits(x));p.DC.Write(Bits(y));p.D10.Write(Bits(z));}
 static void Position(WorldPosition24 p,float x,float y,float z,string why){Equal(Bits(x),p.D8.Read(0),why+" X");Equal(Bits(y),p.DC.Read(0),why+" Y");Equal(Bits(z),p.D10.Read(0),why+" Z");}
 sealed class OwnFixture {
  public const ulong Token=0x7123456789abcdef;
  public readonly WorldPosition24 Destination=new WorldPosition24();
  public readonly NavigationPoint3 Input=new NavigationPoint3();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldCoordinateState State=new WorldCoordinateState();
  public readonly WorldCoordinateBoundaries Boundaries;
  public OwnFixture(){
   State.AttachRegion(Token,Region);Boundaries=new WorldCoordinateBoundaries(State);Boundaries.UseObservedBoundsMask();
   Region.Offset3C.Write(100);Region.Offset40.Write(-20);Region.Offset44.Write(30);
   Region.Bounds70.Center00.Write(0);Region.Bounds70.Center04.Write(0);Region.Bounds70.Center08.Write(0);
   Region.Bounds70.Extent0C.Write(10);Region.Bounds70.Extent10.Write(10);Region.Bounds70.Extent14.Write(10);
   Input.X.Write(101.5f);Input.Y.Write(-18);Input.Z.Write(33);
   Destination.Q0.Write(0x9999);XYZ(Destination,91,92,93);Destination.D14.Write(0xa1b2c3d4);
  }
  public byte Set(){return WorldPositionInitializationContracts.SetFromWorldPosition22C690(Destination,Input,Token,Boundaries,new Invocation());}
  public void Prefix(ulong token){Equal(token,Destination.Q0.Read(0),"Region write retained");Position(Destination,0,0,0,"Zero prefix retained");Equal(0xa1b2c3d4u,Destination.D14.Read(0),"D14 unchanged");}
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("SetFromWorldPosition uses canonical region and actual Translate with shared destination cells",delegate{
   var f=new OwnFixture();var alias=new WorldPosition24(f.Destination);
   Equal((byte)1,f.Set(),"Production same-region completion");Position(alias,1.5f,2,3,"Alias sees destination result");
   Equal(OwnFixture.Token,alias.Q0.Read(0),"Full64-bit region token");Equal(0xa1b2c3d4u,alias.D14.Read(0),"No tail overwrite");
   Check(Object.ReferenceEquals(f.Region,f.State.Region(alias.Q0.Read(0),0)),"Original canonical region view");
   Unknown(f.Boundaries.EngineGlobal,"Same-region completion did not need world engine");
   f.Region.Offset3C.Write(101);f.Input.Y.Write(-16);Equal((byte)1,f.Set(),"Later call reads live region and input");Position(alias,0.5f,4,3,"Shared live fields reused");
  });
  Test("Signed32 conversion rounds before subtraction and unknown D14 remains unknown",delegate{
   var f=new OwnFixture();f.Destination.D14.Forget();
   f.Region.Offset3C.Write(16777217);f.Region.Offset40.Write(-16777217);f.Region.Offset44.Write(Int32.MaxValue);
   f.Input.X.Write(16777218f);f.Input.Y.Write(-16777218f);f.Input.Z.Write(2147483648f);
   Equal((byte)1,f.Set(),"Rounded signed offset inputs accepted");Position(f.Destination,2,-2,0,"Conversion then subtraction");Unknown(f.Destination.D14,"Unwritten padding is not zero-filled");
  });
  Test("Explicit borrowed XYZ alias is read only after target zero stores",delegate{
   var f=new OwnFixture();f.Region.Offset3C.Write(1);f.Region.Offset40.Write(-2);f.Region.Offset44.Write(3);
   XYZ(f.Destination,Single.NaN,Single.PositiveInfinity,Single.NegativeInfinity);
   var input=WorldPositionInput3.BorrowLocalXYZ(new WorldPosition24(f.Destination));
   Equal((byte)1,WorldPositionInitializationContracts.SetFromWorldPosition22C690(f.Destination,input,OwnFixture.Token,f.Boundaries,new Invocation()),"Aliased call completes through Translate");
   Position(f.Destination,-1,2,-3,"Input observed zeroed XYZ");Equal(0xa1b2c3d4u,f.Destination.D14.Read(0),"Aliased D14 retained");
  });
  Test("Opaque snapshots preserve exact conversion and subtraction failure order",delegate{
   var f=new OwnFixture();f.Input.X.Forget();f.Input.Y.Forget();f.Input.Z.Forget();f.Region.Offset3C.Forget();f.Region.Offset40.Forget();f.Region.Offset44.Forget();
   Missing(0x22c6c4,delegate{f.Set();},"OffsetX conversion precedes unknown source X");f.Prefix(OwnFixture.Token);f.Region.Offset3C.Write(100);
   Missing(0x22c6c7,delegate{f.Set();},"OffsetY conversion also precedes source X subtraction");f.Region.Offset40.Write(-20);
   Missing(0x22c6ca,delegate{f.Set();},"SourceX consumed at subtraction");f.Input.X.Write(101.5f);
   Missing(0x22c6d3,delegate{f.Set();},"SourceY consumed at subtraction");f.Input.Y.Write(-18);
   Missing(0x22c6f0,delegate{f.Set();},"OffsetZ conversion precedes sourceZ subtraction");f.Prefix(OwnFixture.Token);f.Region.Offset44.Write(30);
   Missing(0x22c6f9,delegate{f.Set();},"SourceZ consumed last");f.Prefix(OwnFixture.Token);
  });
  Test("Source address failures occur at loads after zero prefix and before later offset consumption",delegate{
   var f=new OwnFixture();
   Missing(0x22c6b4,delegate{WorldPositionInitializationContracts.SetFromWorldPosition22C690(f.Destination,(NavigationPoint3)null,0,null,new Invocation());},"Source address required before region lookup");f.Prefix(0);
   var buffer=new NavigationPointBuffer(new Field<float>[]{Field<float>.Of(100),Field<float>.Of(-20)});var shortPoint=buffer.PointAtScalar(0,0);f.Region.Offset44.Forget();
   Missing(0x22c6d7,delegate{WorldPositionInitializationContracts.SetFromWorldPosition22C690(f.Destination,shortPoint,OwnFixture.Token,f.Boundaries,new Invocation());},"SourceZ address precedes offsetZ conversion");f.Prefix(OwnFixture.Token);
  });
  Test("Zero or unbound region stops at first actual offset load after destination writes",delegate{
   var f=new OwnFixture();f.Input.X.Forget();
   foreach(ulong token in new ulong[]{0,0x12345678}){
    Missing(0x22c6b8,delegate{WorldPositionInitializationContracts.SetFromWorldPosition22C690(f.Destination,f.Input,token,f.Boundaries,new Invocation());},"No invented region binding or null false result");f.Prefix(token);
   }
  });
  Test("Negative zero delta passes through actual Translate positive-zero addition",delegate{
   var f=new OwnFixture();f.Region.Offset3C.Write(0);f.Region.Offset40.Write(0);f.Region.Offset44.Write(0);
   f.Input.X.Write(Float(0x80000000));f.Input.Y.Write(Float(0x80000000));f.Input.Z.Write(Float(0x80000000));
   Equal((byte)1,f.Set(),"Actual translation succeeded");Position(f.Destination,0,0,0,"Translate positive-zero plus negative-zero result");
   Equal(0x80000000u,Bits(f.Input.X.Read(0)),"Borrowed input remains negative zero");
  });
  Test("Translate bounds failure retains its actual XYZ writes and unwritten tail",delegate{
   var f=new OwnFixture();f.Region.Bounds70.Extent0C.Forget();
   Missing(0x0664d4,delegate{f.Set();},"Existing production bounds read stops");Position(f.Destination,1.5f,2,3,"Translate prefix retained");
   Equal(OwnFixture.Token,f.Destination.Q0.Read(0),"Region remains supplied token");Equal(0xa1b2c3d4u,f.Destination.D14.Read(0),"Padding preserved after callee stop");
  });
  Test("Cross-region requirement preserves Translate engine and unsupported-conversion boundaries",delegate{
   var f=new OwnFixture();f.Input.X.Write(111);
   Missing(0x22c58f,delegate{f.Set();},"Existing Translate requires captured engine");Position(f.Destination,11,2,3,"Translated prefix before engine stop");
   var engine=new WorldEngine();engine.World50.Write(new object());f.Boundaries.EngineGlobal.Write(engine);
   Missing(0x22c593,delegate{f.Set();},"Actual GetRegionContainingPoint remains unsupported");Position(f.Destination,11,2,3,"Prefix retained without fake success or rollback");
   Equal(OwnFixture.Token,f.Destination.Q0.Read(0),"No synthetic region transition");Equal(0xa1b2c3d4u,f.Destination.D14.Read(0),"D14 preserved");
  });
 }
}
public static class WorldPositionInitializationTestProgram {
 public static int Main(){try{WorldPositionInitializationTests.Run();Console.WriteLine("PASS "+WorldPositionInitializationTests.Scenarios+" scenarios, "+WorldPositionInitializationTests.Assertions+" assertions; new22C690 own-memory contracts only");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
