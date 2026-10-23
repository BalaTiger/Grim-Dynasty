using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Own-memory integration fixtures for the observed same-region chain. All
// normal paths use an explicitly bound, nonnull spatial node. No callback,
// concurrent mutation, native loader, gameplay acceptance or mesh producer is
// substituted for the fixed contracts under test.
public static class WorldSameRegionPublicationTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float[] Identity(){return new float[]{1,0,0,0,1,0,0,0,1};}
 static void FillFrame(EntityCoordinates64 frame,ulong region,float x,float y,float z){
  frame.Position00.Q0.Write(region);frame.Position00.D8.Write(Bits(x));frame.Position00.DC.Write(Bits(y));frame.Position00.D10.Write(Bits(z));
  frame.Position00.D14.Write(0xdeadbeef);frame.Tail3C.Write(0x87654321);var b=Identity();for(int n=0;n<9;n++)frame.Basis18[n].Write(b[n]);
 }
 static void PositionIs(EntityCoordinates64 frame,float x,float y,float z,string why){Equal(77ul,frame.Position00.Q0.Read(0),why+" region");Equal(Bits(x),frame.Position00.D8.Read(0),why+" X");Equal(Bits(y),frame.Position00.DC.Read(0),why+" Y");Equal(Bits(z),frame.Position00.D10.Read(0),why+" Z");}
 static void IdentityIs(EntityCoordinates64 frame,string why){var b=Identity();for(int n=0;n<9;n++)Equal(Bits(b[n]),Bits(frame.Basis18[n].Read(0)),why+" basis"+n);}
 static void Bounds(WorldBounds24 b,float x,float y,float z,float ex,float ey,float ez){b.Center00.Write(x);b.Center04.Write(y);b.Center08.Write(z);b.Extent0C.Write(ex);b.Extent10.Write(ey);b.Extent14.Write(ez);}
 static void BoundsIs(WorldBounds24 b,float x,float y,float z,float ex,float ey,float ez,string why){
  Equal(Bits(x),Bits(b.Center00.Read(0)),why+" center X");Equal(Bits(y),Bits(b.Center04.Read(0)),why+" center Y");Equal(Bits(z),Bits(b.Center08.Read(0)),why+" center Z");
  Equal(Bits(ex),Bits(b.Extent0C.Read(0)),why+" extent X");Equal(Bits(ey),Bits(b.Extent10.Read(0)),why+" extent Y");Equal(Bits(ez),Bits(b.Extent14.Read(0)),why+" extent Z");
 }
 static void BoundsUnknown(WorldBounds24 b,string why){Unknown(b.Center00,why+" center X");Unknown(b.Center04,why+" center Y");Unknown(b.Center08,why+" center Z");Unknown(b.Extent0C,why+" extent X");Unknown(b.Extent10,why+" extent Y");Unknown(b.Extent14,why+" extent Z");}
 static bool Has(WorldSameRegionPublicationInvocation c,uint at){return Array.IndexOf(c.ObservedAddresses,at)>=0;}
 static void Sequence(WorldSameRegionPublicationInvocation c,string why,params uint[] expected){var seen=c.ObservedAddresses;int cursor=0;foreach(uint at in expected){while(cursor<seen.Length&&seen[cursor]!=at)cursor++;Check(cursor<seen.Length,why+" missing/out-of-order "+at.ToString("x"));cursor++;}}
 static void NoCoordinateStores(WorldSameRegionPublicationInvocation c,string why){foreach(uint at in new uint[]{0x12f646,0x12f657,0x12f662,0x12f669})Check(!Has(c,at),why+" no store "+at.ToString("x"));}
 static void FourStores(WorldSameRegionPublicationInvocation c,string why){Sequence(c,why,0x12f5f7,0x12f5fe,0x12f607,0x12f60f,0x12f618,0x12f620,0x12f623,0x12f62a,0x12f632,0x12f63c,0x12f643,0x12f646,0x12f654,0x12f657,0x12f662,0x12f669,0x12f670);}
 sealed class Fixture {
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly EntityMovementState Entity=new EntityMovementState(new object());
  public readonly ActorBoundingBoxContext Bounding;
  public readonly ActorBoundingBoxState Actor;
  public readonly BorrowedMeshBounds Mesh;
  public readonly WorldCoordinateState Coordinates=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldPublicationRegistry Regions;
  public readonly RegionPublicationView RegionView;
  public readonly LevelMovementState Level;
  public readonly OwnReentrantRegionLock Sync=OwnReentrantRegionLock.InitializeOwn();
  public readonly WorldPublicationGateContext Gates;
  public readonly EntitySpatialTreeContext Spatial=new EntitySpatialTreeContext();
  public readonly WorldSameRegionPublicationContext Context;
  public readonly SpatialEntityView SpatialEntity;
  public readonly SpatialTreeState Tree=new SpatialTreeState(new object());
  public readonly SpatialNodeState Node;
  public readonly EntityCoordinates64 Input=new EntityCoordinates64();
  public Fixture():this(true){}
  public Fixture(bool selectLeaves){
   Entities.Attach(Entity);Bounding=new ActorBoundingBoxContext(Entities);Actor=Bounding.Bind(Entity);ActorBoundingBoxContracts.SelectObservedCharacterDispatch(Actor,false);Bounding.UseObservedMask();
   Entity.Parent130.Write(null);FillFrame(Entity.CoordsD8,77,1,2,3);FillFrame(Input,77,4,5,6);Mesh=Bounding.BindMesh(new object());Actor.Mesh2668.Write(Mesh);Bounds(Mesh.Bounds1A8,0,0,0,1,2,3);
   ActorBoundingBoxContracts.InitializeAttachedIdFields07D020(Actor,new Invocation());
   Regions=new WorldPublicationRegistry(Coordinates);RegionView=Regions.BindRegionView(77,Region);Level=Regions.BindLevelView(new object());RegionView.Level68.Write(Level);RegionView.Parent30.Write(null);RegionView.Reset88.Write(99);
   Region.Unavailable90.Write(0);Region.Critical18.Write(Sync);Level.Region08.Write(77);Level.Ready538C.Write(1);Level.Spatial10.Write(Tree.Identity);
   Gates=new WorldPublicationGateContext(Entities);Context=new WorldSameRegionPublicationContext(Regions,Bounding,Gates,Spatial);Context.AttachRegion(RegionView);SpatialEntity=Context.BindEntity(Actor);
   Spatial.UseObservedDiskConstants();Spatial.BindTree(Tree);Node=Spatial.BindNode(new object());Tree.Root10.Write(Node);Tree.ChangeCount08.Write(7);
   Node.Depth50.Write(8);Node.Parent08.Write(null);Node.Head48.Write(SpatialEntity);Node.Flags54.Write(0);Node.VirtualRva00.Write(0x340e60);Bounds(Node.Box30,0,0,0,100,1,100);for(int n=0;n<4;n++)Node.Child(n).Write(null);
   SpatialEntity.NodeD0.Write(Node);SpatialEntity.Previous120.Write(null);SpatialEntity.Next128.Write(null);SpatialEntity.FlagsCB.Write(2);
   if(selectLeaves)Context.SelectObservedDiskNoOpLeaves();
  }
  public void World(WorldSameRegionPublicationInvocation c){World(Input,c);}
  public void World(EntityCoordinates64 input,WorldSameRegionPublicationInvocation c){WorldSameRegionPublicationContracts.SetCoords21BB40(new object(),Entity,input,Context,c);}
  public void Move(LevelMoveCoordinates48 input,WorldSameRegionPublicationInvocation c){WorldSameRegionPublicationContracts.MoveEntity12F4F0(Level,Entity,input,Context,c);}
  public LevelMoveCoordinates48 LevelInput(float x,float y,float z){var a=new LevelMoveCoordinates48();a.X24.Write(Bits(x));a.Y28.Write(Bits(y));a.Z2C.Write(Bits(z));var b=Identity();for(int n=0;n<9;n++)a.Basis00[n].Write(b[n]);return a;}
  public void PlayerServer(){
   ActorBoundingBoxContracts.SelectObservedCharacterDispatch(Actor,true);var engine=new EntityEngineState(new WorldEngine());Entities.AttachEngine(engine);var v=Gates.BindEngineView(engine);var same=new object();v.Identity668.Write(same);v.Identity678.Write(same);Gates.EngineImport609050.Write(Field<WorldEngine>.Of(engine.Identity));
  }
  public void OldCoordinates(string why){PositionIs(Entity.CoordsD8,1,2,3,why);IdentityIs(Entity.CoordsD8,why);Equal(0xdeadbeefu,Entity.CoordsD8.Position00.D14.Read(0),why+" old padding");Equal(0x87654321u,Entity.CoordsD8.Tail3C.Read(0),why+" old tail");}
  public void Published(float x,float y,float z,string why){PositionIs(Entity.CoordsD8,x,y,z,why);IdentityIs(Entity.CoordsD8,why);Unknown(Entity.CoordsD8.Position00.D14,why+" stack padding copied Unknown");Unknown(Entity.CoordsD8.Tail3C,why+" stack tail copied Unknown");BoundsIs(Actor.Box5C,x,y,z,1,2,3,why+" local box");BoundsIs(Actor.Box44,x,y,z,1,2,3,why+" final box");Equal((byte)1,Actor.Flag74.Read(0),why+" box flag");}
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("World same-region chain uses shared identities real lock and depth8 spatial production",delegate{
   var f=new Fixture();Same(f.Actor,f.Bounding.Bind(f.Entity),"Same Actor provider");Same(f.SpatialEntity,f.Context.BindEntity(f.Actor),"Same spatial publication view");Same(f.Actor.Box44,f.SpatialEntity.Box44,"Spatial borrows produced Actor box");Same(f.Entity,f.Actor.Entity,"Original Entity identity");Same(f.Region,f.RegionView.Region,"Original region identity");Same(f.Entities,f.Gates.Entities,"Shared gate registry");
   var c=new WorldSameRegionPublicationInvocation();f.Sync.Enter(0x19440c);try{f.World(c);Equal(1,f.Sync.CurrentThreadDepth,"Nested publication acquisition released");}finally{f.Sync.Leave(0x194420);}
   f.Published(4,5,6,"World normal");Equal(0u,f.RegionView.Reset88.Read(0),"GetLevel reset prefix");Check(!f.Sync.IsHeldByCurrentThread,"Own lock fully released");Same(f.Node,f.SpatialEntity.NodeD0.Read(0),"Real contained node retained");Same(f.SpatialEntity,f.Node.Head48.Read(0),"List identity retained");BoundsIs(f.Node.Box30,0,3,0,100,4,100,"Depth8 refinement expanded Y to [-1,7]");Equal(7u,f.Tree.ChangeCount08.Read(0),"Contained path does not increment reinsertion counter");
   Sequence(c,"Full production order",0x21bd21,0x21be4d,0x12f670,0x12f67c,0x12f689,0x12f6b3,0x21be77);Equal(0x21be77u,c.Native.At,"World completion address");PositionIs(f.Input,4,5,6,"Independent input remains intact");Equal(0xdeadbeefu,f.Input.Position00.D14.Read(0),"Input padding is not published or modified");
  });
  Test("Null destination and null origin return before virtual gates or payload reads",delegate{
   var a=new Fixture();a.Entity.CoordsD8.Position00.Q0.Forget();a.Entity.Virtual00.Forget();var target=new EntityCoordinates64();target.Position00.Q0.Write(0);var ca=new WorldSameRegionPublicationInvocation();a.World(target,ca);
   Check(!Has(ca,0x21bb82),"Null target never consumes captured unknown old region");Check(!Has(ca,0x21bb91),"Null target never calls gate");Same(null,ca.LastWorldLocal,"No local for null target");BoundsUnknown(a.Actor.Box44,"Null target no publication");Unknown(a.Entity.CoordsD8.Position00.Q0,"Old region not defaulted");
   var b=new Fixture();b.Entity.CoordsD8.Position00.Q0.Write(0);b.Entity.Virtual00.Forget();b.Input.Basis18[0].Forget();var cb=new WorldSameRegionPublicationInvocation();b.World(cb);Check(Has(cb,0x21bb82),"Known null old region is tested");Check(!Has(cb,0x21bb91),"Null origin skips gate");Same(null,cb.LastWorldLocal,"No local for null origin");NoCoordinateStores(cb,"Both-null short-circuit domain");Equal(99u,b.RegionView.Reset88.Read(0),"No level prefix");
  });
  Test("Player network gate remains Unknown instead of assuming an offline session",delegate{
   var f=new Fixture();ActorBoundingBoxContracts.SelectObservedCharacterDispatch(f.Actor,true);var c=new WorldSameRegionPublicationInvocation();Missing(0x3d5bcb,delegate{f.World(c);},"Missing imported global-cell binding");
   f.OldCoordinates("Player gate failure");Equal(0x3d5bcbu,c.Native.At,"Outer invocation retains nested gate failure address");Equal(99u,f.RegionView.Reset88.Read(0),"No level lookup before gate returns");Equal(1,c.Gate.CapturedAt.Length,"Only import identity captured");Equal(0x3d5bc4u,c.Gate.CapturedAt[0],"Import capture address");Same(null,c.LastWorldLocal,"No local before gate");
  });
  Test("Ready and parent-unavailable conditions select the real second gate",delegate{
   var a=new Fixture();a.Level.Ready538C.Forget();var ca=new WorldSameRegionPublicationInvocation();Missing(0x21bc47,delegate{a.World(ca);},"Unknown Ready is consumed on parent-null path");a.OldCoordinates("Unknown Ready");Check(!Has(ca,0x21bc5a),"Unknown Ready does not fabricate second-gate condition");
   var b=new Fixture();b.Level.Ready538C.Write(0);var cb=new WorldSameRegionPublicationInvocation();Missing(0x21bc68,delegate{b.World(cb);},"Character true second gate enters unsupported deletion path");Sequence(cb,"Ready-zero gate sequence",0x21bc47,0x21bc54,0x21bc5a,0x21bc68);Equal(99u,b.RegionView.Reset88.Read(0),"No invented GetLevel prefix on deletion branch");
   var d=new Fixture();d.RegionView.Parent30.Write(new object());d.Region.Unavailable90.Write(1);d.Level.Ready538C.Forget();var cd=new WorldSameRegionPublicationInvocation();Missing(0x21bc68,delegate{d.World(cd);},"Parent plus unavailable skips Ready and requires gate");Check(!Has(cd,0x21bc47),"Unavailable branch does not consume unknown Ready");Unknown(d.Level.Ready538C,"Ready remains Unknown");
   var e=new Fixture();e.RegionView.Parent30.Write(new object());e.RegionView.Level68.Write(null);var ce=new WorldSameRegionPublicationInvocation();Missing(0x21bc68,delegate{e.World(ce);},"Parent with known-null level reaches second gate before any cold load");Check(!Has(ce,0x21bd21),"No fabricated cold-load return");
  });
  Test("Real server identity permits Player Ready-zero path through both gates",delegate{
   var f=new Fixture();f.PlayerServer();f.Level.Ready538C.Write(0);var c=new WorldSameRegionPublicationInvocation();f.World(c);f.Published(4,5,6,"Two Player gates");Equal(8,c.Gate.CapturedAt.Length,"Both Player calls execute all four real captures");Sequence(c,"Both gates and final borrowed-level retrieval",0x21bb91,0x21bba0,0x21bc47,0x21bc5a,0x21bd21,0x21be4d);Equal((byte)0,f.Level.Ready538C.Read(0),"Ready flag is not promoted by success");Equal(0,f.Sync.CurrentThreadDepth,"Both actual level lookups leave the own lock");
  });
  Test("Unknown unavailable flag preserves Reset88 before local creation",delegate{
   var f=new Fixture();f.Region.Unavailable90.Forget();var c=new WorldSameRegionPublicationInvocation();Missing(0x194404,delegate{f.World(c);},"Final GetLevel consumes unknown unavailable after reset");
   Equal(0u,f.RegionView.Reset88.Read(0),"Reset88 prefix retained");f.OldCoordinates("Unavailable failure");Check(!f.Sync.IsHeldByCurrentThread,"Failure occurs before lock enter");Same(null,c.LastWorldLocal,"No local after failed borrowed-level query");NoCoordinateStores(c,"GetLevel failure");
  });
  Test("Observed disk no-op leaves require explicit own selection",delegate{
   var f=new Fixture(false);var c=new WorldSameRegionPublicationInvocation();Missing(0x00ce70,delegate{f.World(c);},"Billboard leaf is not default success");
   PositionIs(f.Entity.CoordsD8,4,5,6,"Coordinate prefix before unselected leaf");Unknown(f.Entity.CoordsD8.Position00.D14,"Padding already overwritten");Unknown(f.Entity.CoordsD8.Tail3C,"Tail already overwritten");FourStores(c,"Four writes before leaf");BoundsUnknown(f.Actor.Box44,"No bounds call yet");BoundsUnknown(f.Actor.Box5C,"No local bounds yet");
   f.Context.SelectObservedDiskNoOpLeaves();f.World(new WorldSameRegionPublicationInvocation());f.Published(4,5,6,"Explicit leaf choice");
  });
  Test("Level borrowed input alias compares every equal field then still publishes all stores",delegate{
   var f=new Fixture();var input=new LevelMoveCoordinates48(f.Entity.CoordsD8);Same(f.Entity.CoordsD8.Position00.D8,input.X24,"Borrowed translation cell");Same(f.Entity.CoordsD8.Basis18[8],input.Basis00[8],"Borrowed last basis cell");var c=new WorldSameRegionPublicationInvocation();f.Move(input,c);
   Sequence(c,"All equal comparison path has no early return",0x12f576,0x12f581,0x12f58c,0x12f597,0x12f5a2,0x12f5ad,0x12f5b8,0x12f5c3,0x12f5ce,0x12f5d7,0x12f5e1,0x12f5ec,0x12f5f2,0x12f646,0x12f657,0x12f662,0x12f669,0x12f670);FourStores(c,"Live input rereads then block writes");f.Published(1,2,3,"Unchanged aliased input still publishes");Same(null,c.LastWorldLocal,"Direct Level call does not invent World local");Equal(99u,f.RegionView.Reset88.Read(0),"Direct Level does not call GetLevel");
  });
  Test("UCOMISS unordered comparisons continue instead of using CSharp NaN inequality",delegate{
   var a=new Fixture();var ai=a.LevelInput(4,5,6);ai.Basis00[0].Write(Single.NaN);ai.Basis00[1].Forget();var ca=new WorldSameRegionPublicationInvocation();Missing(0x12f581,delegate{a.Move(ai,ca);},"Unordered first matrix comparison continues to unknown second");Sequence(ca,"NaN is not a difference branch",0x12f576,0x12f581);NoCoordinateStores(ca,"Unknown comparison");a.OldCoordinates("Matrix NaN comparison prefix");
   var b=new Fixture();var bi=b.LevelInput(1,2,3);bi.X24.Write(Bits(Single.NaN));bi.Y28.Forget();var cb=new WorldSameRegionPublicationInvocation();Missing(0x12f5ec,delegate{b.Move(bi,cb);},"Unordered translation X continues to unknown Y");Sequence(cb,"Position UCOMISS path",0x12f5d7,0x12f5e1,0x12f5ec);b.OldCoordinates("Position NaN comparison prefix");
  });
  Test("Unknown compared inputs stop before coordinate or bbox mutation",delegate{
   var f=new Fixture();var input=f.LevelInput(4,5,6);input.Basis00[0].Forget();var c=new WorldSameRegionPublicationInvocation();Missing(0x12f576,delegate{f.Move(input,c);},"First actual matrix comparison consumes Unknown");f.OldCoordinates("Comparison prefix");NoCoordinateStores(c,"Before publication");Unknown(f.Actor.Flag74,"No bbox valid flag");BoundsUnknown(f.Actor.Box44,"No bbox");Same(f.Node,f.SpatialEntity.NodeD0.Read(0),"Spatial association untouched");
  });
  Test("All four coordinate stores precede consumption of unknown slot270 table",delegate{
   var f=new Fixture();var input=f.LevelInput(4,5,6);input.Basis00[0].Write(2);f.Entity.Virtual00.Forget();var c=new WorldSameRegionPublicationInvocation();Missing(0x12f670,delegate{f.Move(input,c);},"Unknown captured table consumed after stores");
   FourStores(c,"Table captured between first and second stores but consumed last");PositionIs(f.Entity.CoordsD8,4,5,6,"All translation words stored");Equal(2f,f.Entity.CoordsD8.Basis18[0].Read(0),"First basis packet stored");Equal(1f,f.Entity.CoordsD8.Basis18[4].Read(0),"Third packet stored");Equal(1f,f.Entity.CoordsD8.Basis18[8].Read(0),"Fourth packet stored");Unknown(f.Entity.CoordsD8.Position00.D14,"Native stack padding replaces known destination");Unknown(f.Entity.CoordsD8.Tail3C,"Native stack tail replaces known destination");BoundsUnknown(f.Actor.Box44,"No bbox invocation yet");
  });
  Test("World same-object input produces an independent48-byte local before Level alias writes",delegate{
   var f=new Fixture();var c=new WorldSameRegionPublicationInvocation();f.World(f.Entity.CoordsD8,c);f.Published(1,2,3,"World input aliases authoritative coordinates");var local=c.LastWorldLocal;Check(local!=null,"Actual World local retained");
   Check(!Object.ReferenceEquals(local.X24,f.Entity.CoordsD8.Position00.D8),"Local translation does not borrow input");for(int n=0;n<9;n++)Check(!Object.ReferenceEquals(local.Basis00[n],f.Entity.CoordsD8.Basis18[n]),"Local basis owns cell"+n);
   Sequence(c,"Native local packing order",0x21bd26,0x21be01,0x21be09,0x21be11,0x21be1a,0x21be1d,0x21be21,0x21be26,0x21be2c,0x21be2f,0x21be33,0x21be38,0x21be3e,0x21be41,0x21be45,0x21be4a,0x21be4d);
   f.Entity.CoordsD8.Position00.D8.Write(Bits(91));f.Entity.CoordsD8.Basis18[0].Write(9);Equal(Bits(1),local.X24.Read(0),"Completed local is independent of later own writes");Equal(1f,local.Basis00[0].Read(0),"Local basis remains captured");var copy=c.ObservedAddresses;uint first=copy[0];copy[0]=0;Equal(first,c.ObservedAddresses[0],"Diagnostics expose a read-only snapshot");
  });
  Test("Z is copied without a comparison and fails only when bbox arithmetic consumes it",delegate{
   var f=new Fixture();var input=f.LevelInput(1,2,3);input.Z2C.Forget();var c=new WorldSameRegionPublicationInvocation();Missing(0x02ab4e,delegate{f.Move(input,c);},"Unknown Z survives MOVSS and all four coordinate stores");
   Sequence(c,"No invented Z comparison",0x12f5e1,0x12f5ec,0x12f5f2,0x12f646,0x12f669,0x12f67c);Equal(Bits(1),f.Entity.CoordsD8.Position00.D8.Read(0),"X stored");Equal(Bits(2),f.Entity.CoordsD8.Position00.DC.Read(0),"Y stored");Unknown(f.Entity.CoordsD8.Position00.D10,"Unknown Z copied into destination");Unknown(f.Entity.CoordsD8.Position00.D14,"Padding copied");Unknown(f.Entity.CoordsD8.Tail3C,"Tail copied");Unknown(f.Actor.Flag74,"Center-Z failure precedes bbox flag store");BoundsUnknown(f.Actor.Box5C,"No partial bbox publication");
  });
  Test("Nonempty attached IDs retain published coordinates and local bbox only",delegate{
   var f=new Fixture();f.Actor.AttachedEnd80.Write(4);var c=new WorldSameRegionPublicationInvocation();Missing(0x02ac9e,delegate{f.World(c);},"Attached-ID traversal remains unsupported");
   PositionIs(f.Entity.CoordsD8,4,5,6,"Coordinate publication retained");Unknown(f.Entity.CoordsD8.Position00.D14,"Padding overwritten before bbox");Unknown(f.Entity.CoordsD8.Tail3C,"Tail overwritten before bbox");BoundsIs(f.Actor.Box5C,4,5,6,1,2,3,"Local bounding prefix retained");BoundsUnknown(f.Actor.Box44,"No fabricated attached merge/final box");Equal((byte)1,f.Actor.Flag74.Read(0),"Flag prefix retained");Check(!Has(c,0x12f682),"Spatial this not captured before bbox returns");Same(f.Node,f.SpatialEntity.NodeD0.Read(0),"Spatial membership unchanged");Check(!f.Sync.IsHeldByCurrentThread,"Earlier GetLevel own lock released");
  });
  Test("Unknown spatial membership fails after both bbox publications without rollback",delegate{
   var f=new Fixture();f.SpatialEntity.NodeD0.Forget();var c=new WorldSameRegionPublicationInvocation();Missing(0x1333f7,delegate{f.World(c);},"Actual spatial node test consumes Unknown");f.Published(4,5,6,"Spatial failure prefix");Sequence(c,"BBox before spatial call",0x12f67c,0x12f682,0x12f689);Check(!Has(c,0x12f68e),"Final virtual table not captured before spatial returns");Equal(7u,f.Tree.ChangeCount08.Read(0),"No fabricated insertion count");
  });
  Test("Real refinement creates a child and moves the entity without invoking unsupported prune",delegate{
   var f=new Fixture();f.Node.Depth50.Write(7);FillFrame(f.Input,77,50,5,50);var c=new WorldSameRegionPublicationInvocation();f.World(c);f.Published(50,5,50,"Refined publication");
   var child=f.Node.Child(3).Read(0);Check(child!=null,"Real right-back child allocated");Check(!Object.ReferenceEquals(child,f.Node),"Child is a new owned node");Same(child,f.SpatialEntity.NodeD0.Read(0),"Production insert updates membership");Same(f.Node,child.Parent08.Read(0),"Production child parent");Same(f.SpatialEntity,child.Head48.Read(0),"Production child list head");Same(null,f.Node.Head48.Read(0),"Old list head removed");Same(null,f.SpatialEntity.Previous120.Read(0),"Inserted previous null");Same(null,f.SpatialEntity.Next128.Read(0),"Inserted next null");Equal(8,child.Depth50.Read(0),"Depth7 refines to observed depth8 leaf");Equal(0x340e60u,child.VirtualRva00.Read(0),"Observed child table selection");Equal((byte)2,child.Flags54.Read(0),"Insert combines real entity flags");Equal(7u,f.Tree.ChangeCount08.Read(0),"In-node refinement is not outside-root reinsertion");
   for(int n=0;n<3;n++)Same(null,f.Node.Child(n).Read(0),"Other parent children unchanged");for(int n=0;n<4;n++)Same(null,child.Child(n).Read(0),"Depth8 child links initialized");Check(Has(c,0x12f6b3),"Refinement returned normally to tail");
  });
  Test("Cross-region request stops before constructing a same-region local",delegate{
   var f=new Fixture();f.Input.Position00.Q0.Write(88);var c=new WorldSameRegionPublicationInvocation();Missing(0x21bbd1,delegate{f.World(c);},"Cross-region removal/addition not fabricated");f.OldCoordinates("Cross-region boundary");Same(null,c.LastWorldLocal,"No same-region local");NoCoordinateStores(c,"Cross-region boundary");Equal(99u,f.RegionView.Reset88.Read(0),"Character gate did not invent preloading effects");
  });
  Test("Early differing matrix comparison does not consume opaque default input payload",delegate{
   var f=new Fixture();var input=new LevelMoveCoordinates48();input.Basis00[0].Write(2);f.Entity.Virtual00.Forget();var c=new WorldSameRegionPublicationInvocation();Missing(0x12f670,delegate{f.Move(input,c);},"Unknown payload is copied after real early-difference branch");
   Check(Has(c,0x12f576)&&!Has(c,0x12f581),"Only differing first basis comparison consumed");FourStores(c,"All opaque packets stored");Equal(77ul,f.Entity.CoordsD8.Position00.Q0.Read(0),"Level region copied");Equal(2f,f.Entity.CoordsD8.Basis18[0].Read(0),"Known first basis copied");for(int n=1;n<9;n++)Unknown(f.Entity.CoordsD8.Basis18[n],"Unconsumed basis copied Unknown"+n);Unknown(f.Entity.CoordsD8.Position00.D8,"Unknown X copied");Unknown(f.Entity.CoordsD8.Position00.DC,"Unknown Y copied");Unknown(f.Entity.CoordsD8.Position00.D10,"Unknown Z copied");Unknown(f.Entity.CoordsD8.Position00.D14,"Unknown padding copied");Unknown(f.Entity.CoordsD8.Tail3C,"Unknown tail copied");
  });
  Test("Player first-gate preload returns known null when unavailable after Reset88",delegate{
   var f=new Fixture();f.PlayerServer();f.Region.Unavailable90.Write(1);f.Region.Critical18.Forget();var c=new WorldSameRegionPublicationInvocation();f.World(c);
   Equal(0u,f.RegionView.Reset88.Read(0),"Unavailable prefix clears Reset88");f.OldCoordinates("Known-null preload return");Check(Has(c,0x21bba0)&&Has(c,0x21bba5),"Actual preliminary GetLevel was called and checked");Check(!Has(c,0x21bc2c),"Null preload returns before same-region ready checks");Same(null,c.LastWorldLocal,"Null preload does not construct local");Unknown(f.Region.Critical18,"Unavailable branch never requires a lock identity");BoundsUnknown(f.Actor.Box44,"No bbox publication");
  });
  Test("Contained nonnull spatial node does not eagerly consume unknown tree pointer",delegate{
   var f=new Fixture();f.Level.Spatial10.Forget();var c=new WorldSameRegionPublicationInvocation();f.World(c);f.Published(4,5,6,"Contained path with opaque tree this");Same(f.Node,f.SpatialEntity.NodeD0.Read(0),"Nonnull node still processed");BoundsIs(f.Node.Box30,0,3,0,100,4,100,"Real depth8 refinement still runs");Unknown(f.Level.Spatial10,"Tree this remains unknown and unused on contained path");Check(Has(c,0x12f6b3),"Spatial returned to final leaf");
  });
 }
 public static string Result(string status,string error){
  string json="{\"status\":\""+status+"\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"own_same_region_publication\":true,\"real_mesh_production\":false,\"cross_region\":false,\"gameplay_accepted\":false";
  if(error!=null)json+=",\"error\":\""+error.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n").Replace("\t","\\t")+"\"";return json+"}";
 }
}
public static class WorldSameRegionPublicationTestProgram {
 public static int Main(){try{WorldSameRegionPublicationTests.Run();Console.WriteLine(WorldSameRegionPublicationTests.Result("passed",null));return 0;}catch(Exception e){Console.WriteLine(WorldSameRegionPublicationTests.Result("failed",e.Message));return 1;}}
}
