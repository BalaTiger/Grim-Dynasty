using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Own-memory fixtures for the observed Actor bounding-box path only. No native
// object, loaded dispatch, attached-ID implementation or injected callback is
// supplied. Independent goldens below retain the assembly's binary32 grouping.
public static class ActorBoundingBoxTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name,e);}}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected Unsupported: "+why);}
 static void MissingSetup(Action body,string why){try{body();}catch(MissingContract){Check(true,why);return;}throw new Exception("Expected missing own binding: "+why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected own identity rejection: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static void Bounds(WorldBounds24 b,float x,float y,float z,float ex,float ey,float ez){b.Center00.Write(x);b.Center04.Write(y);b.Center08.Write(z);b.Extent0C.Write(ex);b.Extent10.Write(ey);b.Extent14.Write(ez);}
 static void BoundsIs(WorldBounds24 b,float x,float y,float z,float ex,float ey,float ez,string why){
  Equal(Bits(x),Bits(b.Center00.Read(0)),why+" center X");Equal(Bits(y),Bits(b.Center04.Read(0)),why+" center Y");Equal(Bits(z),Bits(b.Center08.Read(0)),why+" center Z");
  Equal(Bits(ex),Bits(b.Extent0C.Read(0)),why+" extent X");Equal(Bits(ey),Bits(b.Extent10.Read(0)),why+" extent Y");Equal(Bits(ez),Bits(b.Extent14.Read(0)),why+" extent Z");
 }
 static void BoundsUnknown(WorldBounds24 b,string why){Unknown(b.Center00,why+" center X");Unknown(b.Center04,why+" center Y");Unknown(b.Center08,why+" center Z");Unknown(b.Extent0C,why+" extent X");Unknown(b.Extent10,why+" extent Y");Unknown(b.Extent14,why+" extent Z");}
 static void Position(EntityMovementState e,float x,float y,float z){e.CoordsD8.Position00.D8.Write(Bits(x));e.CoordsD8.Position00.DC.Write(Bits(y));e.CoordsD8.Position00.D10.Write(Bits(z));}
 static void Basis(EntityMovementState e,params float[] values){Check(values.Length==9,"Exactly nine fixture basis values");for(int n=0;n<9;n++)e.CoordsD8.Basis18[n].Write(values[n]);}
 sealed class Fixture {
  public readonly EntityMovementRegistry Registry=new EntityMovementRegistry();
  public readonly EntityMovementState Entity=new EntityMovementState(new object());
  public readonly ActorBoundingBoxContext Context;
  public readonly ActorBoundingBoxState State;
  public readonly BorrowedMeshBounds Mesh;
  public Fixture(){
   Registry.Attach(Entity);Context=new ActorBoundingBoxContext(Registry);State=Context.Bind(Entity);ActorBoundingBoxContracts.SelectObservedCharacterDispatch(State,false);
   Entity.Parent130.Write(null);Entity.CoordsD8.Position00.Q0.Write(7);Position(Entity,10,20,30);Basis(Entity,1,2,3,4,5,6,7,8,9);
   Mesh=Context.BindMesh(new object());State.Mesh2668.Write(Mesh);Bounds(Mesh.Bounds1A8,2,3,4,5,6,7);Context.UseObservedMask();
  }
  public void Empty(){State.AttachedBegin78.Write(0x1000);State.AttachedEnd80.Write(0x1000);}
  public void Update(){ActorBoundingBoxContracts.UpdateBoundingBox02A990(State,Context,new Invocation());}
  public void SeedOutputs(){State.Flag74.Write(9);Bounds(State.Box44,101,102,103,104,105,106);Bounds(State.Box5C,201,202,203,204,205,206);}
  public void OutputsUntouched(string why){BoundsIs(State.Box44,101,102,103,104,105,106,why+" Box44");BoundsIs(State.Box5C,201,202,203,204,205,206,why+" Box5C");}
  public void Gold(string why){BoundsIs(State.Box5C,52,71,90,78,96,114,why+" Box5C");BoundsIs(State.Box44,52,71,90,78,96,114,why+" Box44");Equal((byte)1,State.Flag74.Read(0),why+" flag");}
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Explicit binding retains authoritative Entity and repeated provider identities",delegate{
   var registry=new EntityMovementRegistry();var e=new EntityMovementState(new object());registry.Attach(e);var c=new ActorBoundingBoxContext(registry);var s=c.Bind(e);
   Same(e,s.Entity,"Actor state keeps the original Entity");Same(s,c.Bind(e),"Repeated bind returns same state");Same(e,registry.Entity(e.Identity,0),"Registry has no mirrored Entity");
   Invalid(delegate{c.Bind(new EntityMovementState(e.Identity));},"Different Entity view cannot occupy the same identity");
   var identity=new object();var mesh=c.BindMesh(identity);Same(identity,mesh.Identity,"Original borrowed address identity");Same(mesh,c.BindMesh(identity),"Repeated mesh bind returns same provider");
   mesh.Bounds1A8.Center00.Write(17);Equal(17f,c.BindMesh(identity).Bounds1A8.Center00.Read(0),"Provider uses shared cells");
   var absent=new EntityMovementState(new object());MissingSetup(delegate{c.Bind(absent);},"Binding cannot lazily register an absent Entity");
  });
  Test("Allocation leaves native fields and mask Unknown until explicit observed setup",delegate{
   var registry=new EntityMovementRegistry();var e=new EntityMovementState(new object());registry.Attach(e);var c=new ActorBoundingBoxContext(registry);var s=c.Bind(e);var mesh=c.BindMesh(new object());
   Unknown(c.Mask34C410,"No default abs mask");Unknown(s.Flag74,"No default valid flag");Unknown(s.AttachedBegin78,"No default attached begin");Unknown(s.AttachedEnd80,"No default attached end");Unknown(s.AttachedCapacity88,"No default attached capacity");
   Unknown(s.Mesh2668,"No default first mesh");Unknown(s.Mesh2670,"No default fallback mesh");BoundsUnknown(s.Box44,"Final box");BoundsUnknown(s.Box5C,"Local box");BoundsUnknown(mesh.Bounds1A8,"Borrowed source");Unknown(e.Parent130,"No default parent");Unknown(e.Virtual00,"No default vptr");
   c.UseObservedMask();Equal(0x7fffffffu,c.Mask34C410.Read(0),"Pinned DWORD bits");Unknown(s.AttachedEnd80,"Mask setup does not initialize attachments");
  });
  Test("Observed constructor prefix clears only all three attached-ID fields",delegate{
   var registry=new EntityMovementRegistry();var e=new EntityMovementState(new object());registry.Attach(e);var c=new ActorBoundingBoxContext(registry);var s=c.Bind(e);
   s.AttachedBegin78.Write(11);s.AttachedEnd80.Write(22);s.AttachedCapacity88.Write(33);var i=new Invocation();ActorBoundingBoxContracts.InitializeAttachedIdFields07D020(s,i);
   Equal(0ul,s.AttachedBegin78.Read(0),"Begin zeroed");Equal(0ul,s.AttachedEnd80.Read(0),"End zeroed");Equal(0ul,s.AttachedCapacity88.Read(0),"Capacity zeroed");Equal(0x07d094u,i.At,"Last observed constructor-prefix store");
   Unknown(s.Flag74,"No inferred bounding validity");Unknown(s.Mesh2668,"No inferred mesh");BoundsUnknown(s.Box44,"Constructor does not publish a box");Unknown(e.Parent130,"No invented parent initialization");Unknown(c.Mask34C410,"No implicit constant setup");
  });
  Test("Real IsInWorld consumes parent before region and never traverses a parent",delegate{
   var e=new EntityMovementState(new object());Missing(0x07ed70,delegate{ActorBoundingBoxContracts.IsInWorld07ED70(e,new Invocation());},"Unknown parent");
   e.Parent130.Write(new EntityMovementState(new object()));Equal((byte)0,ActorBoundingBoxContracts.IsInWorld07ED70(e,new Invocation()),"Any nonnull parent returns false");Unknown(e.CoordsD8.Position00.Q0,"Parent branch skips local region");
   e.Parent130.Write(null);Missing(0x07ed7a,delegate{ActorBoundingBoxContracts.IsInWorld07ED70(e,new Invocation());},"Unknown region after known-null parent");
   e.CoordsD8.Position00.Q0.Write(0);Equal((byte)0,ActorBoundingBoxContracts.IsInWorld07ED70(e,new Invocation()),"Known-null local region");e.CoordsD8.Position00.Q0.Write(UInt64.MaxValue);Equal((byte)1,ActorBoundingBoxContracts.IsInWorld07ED70(e,new Invocation()),"Any nonzero region word is true");
  });
  Test("False parent gate skips coordinates mesh mask and list without initializing them",delegate{
   var registry=new EntityMovementRegistry();var e=new EntityMovementState(new object());registry.Attach(e);var c=new ActorBoundingBoxContext(registry);var s=c.Bind(e);ActorBoundingBoxContracts.SelectObservedCharacterDispatch(s,false);e.Parent130.Write(new EntityMovementState(new object()));
   ActorBoundingBoxContracts.UpdateBoundingBox02A990(s,c,new Invocation());Unknown(s.Flag74,"No valid-flag store on false gate");BoundsUnknown(s.Box44,"No final publication");BoundsUnknown(s.Box5C,"No local publication");Unknown(s.Mesh2668,"No mesh lookup");Unknown(c.Mask34C410,"No mask consumption");Unknown(s.AttachedEnd80,"No list consumption");Unknown(e.CoordsD8.Position00.Q0,"No coordinate/region read after parent rejects");
  });
  Test("Known-null region gate leaves preexisting outputs intact",delegate{
   var f=new Fixture();f.SeedOutputs();f.Entity.CoordsD8.Position00.Q0.Write(0);f.Entity.CoordsD8.Basis18[0].Forget();f.State.Mesh2668.Forget();f.Context.Mask34C410.Forget();f.Update();
   f.OutputsUntouched("False region gate");Equal((byte)9,f.State.Flag74.Read(0),"No flag store");Unknown(f.State.AttachedBegin78,"No default empty list on false gate");
  });
  Test("Unknown and foreign primary tables cannot fabricate the virtual60 result",delegate{
   var f=new Fixture();f.SeedOutputs();f.Entity.Virtual00.Forget();Missing(0x02a9a8,delegate{f.Update();},"Unknown table at indirect call");f.OutputsUntouched("Unknown table");
   f.Entity.Virtual00.Write(new object());Missing(0x02a9a8,delegate{f.Update();},"Unrecognized primary table");Equal((byte)9,f.State.Flag74.Read(0),"No flag store before valid dispatch");
  });
  Test("Read-only dispatch classification uses the same captured primary-table identity",delegate{
   var f=new Fixture();var character=f.Entity.Virtual00.Snapshot();
   Equal(ObservedEntityDispatchKind.Character,ActorBoundingBoxContracts.RequireObservedDispatch(character,0x100),"Captured Character table classification");
   ActorBoundingBoxContracts.SelectObservedCharacterDispatch(f.State,true);
   Equal(ObservedEntityDispatchKind.Character,ActorBoundingBoxContracts.RequireObservedDispatch(character,0x100),"Old capture remains Character");
   Equal(ObservedEntityDispatchKind.Player,ActorBoundingBoxContracts.RequireObservedDispatch(f.Entity.Virtual00.Snapshot(),0x101),"Live re-read observes Player");
   f.Entity.Virtual00.Write(new object());Missing(0x102,delegate{ActorBoundingBoxContracts.RequireObservedDispatch(f.Entity.Virtual00.Snapshot(),0x102);},"Foreign identity cannot register success");
   f.Entity.Virtual00.Forget();Missing(0x103,delegate{ActorBoundingBoxContracts.RequireObservedDispatch(f.Entity.Virtual00.Snapshot(),0x103);},"Unknown identity remains unknown");
  });
  Test("Mesh getter selects first nonnull provider without reading later fields",delegate{
   var f=new Fixture();f.Mesh.Bounds1A8.Center00.Forget();Same(f.Mesh,ActorBoundingBoxContracts.GetMeshInstance06DA30(f.State,new Invocation()),"Exact primary provider returned");Unknown(f.State.Mesh2670,"Fallback not read");Unknown(f.Mesh.Bounds1A8.Center00,"Bounds not consumed by getter");
   var fallback=f.Context.BindMesh(new object());f.State.Mesh2668.Write(null);f.State.Mesh2670.Write(fallback);Same(fallback,ActorBoundingBoxContracts.GetMeshInstance06DA30(f.State,new Invocation()),"Exact second provider returned");BoundsUnknown(fallback.Bounds1A8,"Fallback does not need known payload");
  });
  Test("Unknown mesh entries stop at real test while two known null entries reach fallback",delegate{
   var f=new Fixture();f.State.Mesh2668.Forget();Missing(0x06da43,delegate{ActorBoundingBoxContracts.GetMeshInstance06DA30(f.State,new Invocation());},"Unknown first entry is not null");
   f.State.Mesh2668.Write(null);Missing(0x06da43,delegate{ActorBoundingBoxContracts.GetMeshInstance06DA30(f.State,new Invocation());},"Unknown second entry is not null");f.State.Mesh2670.Write(null);
   Missing(0x06da53,delegate{ActorBoundingBoxContracts.GetMeshInstance06DA30(f.State,new Invocation());},"Known both-null invokes unclosed real fallback");
  });
  Test("Mesh selection precedes consumption of captured unknown coordinates",delegate{
   var f=new Fixture();f.SeedOutputs();f.Entity.CoordsD8.Basis18[0].Forget();f.State.Mesh2668.Forget();Missing(0x06da43,delegate{f.Update();},"No eager full coordinate validation");f.OutputsUntouched("Mesh failure");Equal((byte)9,f.State.Flag74.Read(0),"No flag prefix yet");
  });
  Test("Released mesh is still borrowed by getter and rejected at first caller access",delegate{
   var f=new Fixture();f.SeedOutputs();f.Mesh.MarkReleased();Same(f.Mesh,ActorBoundingBoxContracts.GetMeshInstance06DA30(f.State,new Invocation()),"Getter does not dereference returned mesh");
   Missing(0x02aa93,delegate{f.Update();},"Caller dereferences released bounds");f.OutputsUntouched("Released borrowed bounds");Equal((byte)9,f.State.Flag74.Read(0),"No flag prefix");
  });
  Test("Provider owned by a different context is not silently rebound",delegate{
   var f=new Fixture();f.SeedOutputs();var foreign=new ActorBoundingBoxContext(new EntityMovementRegistry()).BindMesh(new object());f.State.Mesh2668.Write(foreign);
   Same(foreign,ActorBoundingBoxContracts.GetMeshInstance06DA30(f.State,new Invocation()),"Getter returns captured identity without rebinding");Missing(0x02aa93,delegate{f.Update();},"Caller requires explicit provider ownership");f.OutputsUntouched("Foreign provider");
  });
  Test("Character dispatch publishes the six independently computed golden scalars",delegate{
   var f=new Fixture();f.Empty();f.Update();f.Gold("Character");Unknown(f.State.Mesh2670,"Primary mesh skipped fallback");Unknown(f.State.AttachedCapacity88,"NoIDs path never consumes capacity");Unknown(f.Entity.CoordsD8.Position00.D14,"Coordinate padding stays unknown");Unknown(f.Entity.CoordsD8.Tail3C,"Coordinate tail stays unknown");BoundsIs(f.Mesh.Bounds1A8,2,3,4,5,6,7,"Borrowed input unchanged");
  });
  Test("Player dispatch reaches the same observed bounds contracts",delegate{
   var f=new Fixture();ActorBoundingBoxContracts.SelectObservedCharacterDispatch(f.State,true);f.Empty();f.Update();f.Gold("Player");
  });
  Test("Extent X and Y add Z contribution to the already rounded X plus Y pair",delegate{
   var f=new Fixture();f.Empty();Position(f.Entity,0,0,0);Basis(f.Entity,1,1,16777216f,1,1,1,16777216f,16777216f,1);Bounds(f.Mesh.Bounds1A8,0,0,0,1,1,1);f.Update();
   // X/Y: 2^24 + (1+1) = 2^24+2. The decompiler's apparent
   // (2^24+1)+1 instead rounds down twice. Z really uses (2^24+1)+1.
   BoundsIs(f.State.Box44,0,0,0,16777218f,16777218f,16777216f,"Distinct assembly associations");BoundsIs(f.State.Box5C,0,0,0,16777218f,16777218f,16777216f,"Same local result");
  });
  Test("Center cancellation rounds each add before translation instead of widening",delegate{
   var f=new Fixture();f.Empty();Position(f.Entity,1,2,3);Basis(f.Entity,16777216f,0,0,1,0,0,-16777216f,0,0);Bounds(f.Mesh.Bounds1A8,1,1,1,0,0,0);f.Update();
   BoundsIs(f.State.Box44,1,2,3,0,0,0,"Binary32 cancellation then translation");
  });
  Test("Center multiplication rounds to binary32 independently",delegate{
   var f=new Fixture();f.Empty();Position(f.Entity,0,0,0);Basis(f.Entity,Float(0x3f800001),0,0,0,0,0,0,0,0);Bounds(f.Mesh.Bounds1A8,Float(0x3f800001),0,0,0,0,0);f.Update();
   Equal(0x3f800002u,Bits(f.State.Box44.Center00.Read(0)),"Rounded product bits");BoundsIs(f.State.Box44,Float(0x3f800002),0,0,0,0,0,"Product golden");
  });
  Test("Observed mask clears product signs and does not change center signs",delegate{
   var f=new Fixture();f.Empty();Basis(f.Entity,-1,-2,-3,-4,-5,-6,-7,-8,-9);f.Update();BoundsIs(f.State.Box44,-32,-31,-30,78,96,114,"Signed transform with positive extents");
   Bounds(f.Mesh.Bounds1A8,2,3,4,-5,-6,-7);f.Update();BoundsIs(f.State.Box44,-32,-31,-30,78,96,114,"Mask applies after every product");
  });
  Test("Negative zero extent products become positive zero through the observed mask",delegate{
   var f=new Fixture();f.Empty();Position(f.Entity,0,0,0);Basis(f.Entity,1,0,0,0,1,0,0,0,1);Bounds(f.Mesh.Bounds1A8,0,0,0,Float(0x80000000),Float(0x80000000),Float(0x80000000));f.Update();
   Equal(0u,Bits(f.State.Box44.Extent0C.Read(0)),"Extent X positive zero");Equal(0u,Bits(f.State.Box44.Extent10.Read(0)),"Extent Y positive zero");Equal(0u,Bits(f.State.Box44.Extent14.Read(0)),"Extent Z positive zero");
  });
  Test("Unknown center arithmetic stops before any shared bounding write",delegate{
   var f=new Fixture();f.SeedOutputs();f.Mesh.Bounds1A8.Center00.Forget();Missing(0x02aad4,delegate{f.Update();},"Center X first product");f.OutputsUntouched("Unknown center");Equal((byte)9,f.State.Flag74.Read(0),"Flag untouched");
   var m=new Fixture();m.SeedOutputs();m.Entity.CoordsD8.Basis18[0].Forget();Missing(0x02aad4,delegate{m.Update();},"Captured matrix scalar consumed at first product");m.OutputsUntouched("Unknown matrix");
  });
  Test("Unknown mask fails at ANDPS after flag store and before either box",delegate{
   var f=new Fixture();f.SeedOutputs();f.Context.Mask34C410.Forget();Missing(0x02ab8d,delegate{f.Update();},"Opaque mask load is not premature consumption");Equal((byte)1,f.State.Flag74.Read(0),"Flag store prefix retained");f.OutputsUntouched("Unknown mask");
  });
  Test("Unknown extent X fails at its multiplication after flag store",delegate{
   var f=new Fixture();f.SeedOutputs();f.Mesh.Bounds1A8.Extent0C.Forget();Missing(0x02aba0,delegate{f.Update();},"MOVUPS source capture retains unknown extent X");Equal((byte)1,f.State.Flag74.Read(0),"Flag prefix retained");f.OutputsUntouched("Unknown extent X");
  });
  Test("Unknown extent Y fails after flag while unknown extent Z fails before it",delegate{
   var y=new Fixture();y.SeedOutputs();y.Mesh.Bounds1A8.Extent10.Forget();Missing(0x02aba8,delegate{y.Update();},"Extent Y first product");Equal((byte)1,y.State.Flag74.Read(0),"Extent Y flag prefix");y.OutputsUntouched("Unknown extent Y");
   var z=new Fixture();z.SeedOutputs();z.Mesh.Bounds1A8.Extent14.Forget();Missing(0x02ab78,delegate{z.Update();},"Extent Z first product");Equal((byte)9,z.State.Flag74.Read(0),"Extent Z precedes flag store");z.OutputsUntouched("Unknown extent Z");
  });
  Test("Uninitialized attachment endpoints fail only after local box publication",delegate{
   var f=new Fixture();f.SeedOutputs();Missing(0x02ac89,delegate{f.Update();},"Unknown end is consumed by SUB rather than MOV");Equal((byte)1,f.State.Flag74.Read(0),"Flag retained");BoundsIs(f.State.Box5C,52,71,90,78,96,114,"Local16 plus8 stores retained");BoundsIs(f.State.Box44,101,102,103,104,105,106,"Final box not yet published");Unknown(f.State.AttachedBegin78,"Begin not default initialized");Unknown(f.State.AttachedEnd80,"End not default initialized");Unknown(f.State.AttachedCapacity88,"Capacity not consumed");
   f.State.AttachedEnd80.Write(0x1000);Missing(0x02ac89,delegate{f.Update();},"Unknown begin also consumed at SUB");
  });
  Test("Nonempty attached IDs stop at the unsupported branch with local-only prefix",delegate{
   var f=new Fixture();f.State.AttachedBegin78.Write(0x1000);f.State.AttachedEnd80.Write(0x1004);Missing(0x02ac9e,delegate{f.Update();},"No inferred traversal or fabricated merged bounds");
   Equal((byte)1,f.State.Flag74.Read(0),"Flag prefix");BoundsIs(f.State.Box5C,52,71,90,78,96,114,"Local box complete");BoundsUnknown(f.State.Box44,"No final box success fabricated");Unknown(f.State.AttachedCapacity88,"Unsupported branch still does not read capacity");
  });
  Test("Known-empty endpoints and borrowed bounds are read live on each invocation",delegate{
   var f=new Fixture();f.Empty();f.Update();f.Gold("First invocation");f.Mesh.Bounds1A8.Center00.Write(12);f.State.AttachedEnd80.Write(0x1004);
   Missing(0x02ac9e,delegate{f.Update();},"Prior empty result is not cached");BoundsIs(f.State.Box5C,62,91,120,78,96,114,"Fresh borrowed center used before branch");BoundsIs(f.State.Box44,52,71,90,78,96,114,"Previous final publication survives failure");
   f.State.AttachedEnd80.Write(0x1000);f.Update();BoundsIs(f.State.Box44,62,91,120,78,96,114,"Restored known-empty path publishes current values");Unknown(f.State.AttachedCapacity88,"Capacity remains unconsumed across invocations");
  });
  Test("Constructor-proven empty list permits success without extra list assumptions",delegate{
   var f=new Fixture();ActorBoundingBoxContracts.InitializeAttachedIdFields07D020(f.State,new Invocation());f.Update();f.Gold("Observed constructor empty prefix");Equal(0ul,f.State.AttachedBegin78.Read(0),"No allocation or mutation of list");Equal(0ul,f.State.AttachedEnd80.Read(0),"No inferred added IDs");
  });
  Test("NaN center propagates through products without being treated as a false gate",delegate{
   var f=new Fixture();f.Empty();Position(f.Entity,0,0,0);Basis(f.Entity,1,0,0,0,1,0,0,0,1);Bounds(f.Mesh.Bounds1A8,Single.NaN,2,3,4,5,6);f.Update();
   Check(Single.IsNaN(f.State.Box44.Center00.Read(0)),"NaN X published");Check(Single.IsNaN(f.State.Box44.Center04.Read(0)),"Zero times NaN still propagates into Y");Check(Single.IsNaN(f.State.Box44.Center08.Read(0)),"Zero times NaN still propagates into Z");
   Check(Single.IsNaN(f.State.Box5C.Center00.Read(0)),"Local box also keeps NaN");Equal(4f,f.State.Box44.Extent0C.Read(0),"Independent extent X");Equal(5f,f.State.Box44.Extent10.Read(0),"Independent extent Y");Equal(6f,f.State.Box44.Extent14.Read(0),"Independent extent Z");Equal((byte)1,f.State.Flag74.Read(0),"No invented NaN rejection");
  });
 }
}
public static class ActorBoundingBoxTestProgram {
 public static int Main(){try{ActorBoundingBoxTests.Run();Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+ActorBoundingBoxTests.Scenarios+",\"assertions\":"+ActorBoundingBoxTests.Assertions+",\"own_actor_bounding_box\":true,\"mesh_resource_production\":false,\"attached_id_traversal\":false,\"position_publication\":false,\"gameplay_accepted\":false}");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
