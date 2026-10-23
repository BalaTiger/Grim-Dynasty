using System;
using GrimDynasty.ReverseClient.Actions;

public static class WorldPublicationGateTests {
 static int assertions,scenarios;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Equal<T>(T expected,T actual,string label){Check(Object.Equals(expected,actual),label);}
 static void Same(object expected,object actual,string label){Check(Object.ReferenceEquals(expected,actual),label);}
 static void Missing(Action action,uint at,string label){try{action();throw new Exception(label+" unexpectedly completed");}catch(MissingContract e){Equal(at,e.Address,label);}}
 static void Unknown<T>(Field<T> field,string label){Missing(delegate{field.Read(0xf001);},0xf001,label);}
 static void Captures(PublicationGateInvocation call,params uint[] expected){var actual=call.CapturedAt;Equal(expected.Length,actual.Length,"capture count");for(int n=0;n<expected.Length;n++)Equal(expected[n],actual[n],"capture order "+n);}
 sealed class Fixture {
  public readonly WorldEngine Identity=new WorldEngine();
  public readonly EntityEngineState Engine;
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly Field<WorldEngine> Global=new Field<WorldEngine>();
  public readonly WorldCoordinateBoundaries World;
  public readonly WorldPublicationGateContext Context;
  public readonly PublicationGateEngineView View;
  public Fixture(){Engine=new EntityEngineState(Identity);Entities.AttachEngine(Engine);World=new WorldCoordinateBoundaries(new WorldCoordinateState(),Global);Context=new WorldPublicationGateContext(Entities);View=Context.BindEngineView(Engine);}
  public void BindGlobal(){Context.EngineImport609050.Write(Global);Global.Write(Identity);}
  public byte Server(PublicationGateInvocation c){return WorldPublicationGates.IsNetworkServer078110(Identity,Context,c);}
  public byte Player(PublicationGateInvocation c){return WorldPublicationGates.PlayerDeleteOnEnteringUnloadedLevel3D5BC0(null,Context,c);}
 }
 static void UnknownDefaultsAndAuthoritativeIdentity(){
  var f=new Fixture();Same(f.View,f.Context.BindEngineView(f.Engine),"idempotent sidecar");Same(f.Engine,f.View.Engine,"same entity-engine view");Same(f.Identity,f.View.Engine.Identity,"same WorldEngine identity");Same(f.Global,f.World.EngineGlobal,"shared world global cell");
  Unknown(f.View.Identity668,"668 not initialized");Unknown(f.View.Identity678,"678 not initialized");Unknown(f.Global,"global not initialized");Unknown(f.Context.EngineImport609050,"import not initialized");Unknown(f.Engine.FrameCount640,"unwritten existing field");Unknown(f.Identity.World50,"world pointer untouched");scenarios++;
 }
 static void DirectIdentityAndAl(){
  var f=new Fixture();var pointer=new object();f.View.Identity678.Write(pointer);f.View.Identity668.Write(pointer);var c=new PublicationGateInvocation();byte al=f.Server(c);Equal((byte)1,al,"same pointers set AL1");Equal(0x078121u,c.Native.At,"actual return");Captures(c,0x078110,0x078117);
  f.View.Identity668.Write(new object());Equal((byte)0,f.Server(new PublicationGateInvocation()),"different pointers set AL0");Same(pointer,f.View.Identity678.Read(0),"helper does not write pointer");Unknown(f.Engine.FrameCount640,"frame count not used");scenarios++;
 }
 static void KnownNullIsARealComparedValue(){
  var f=new Fixture();f.View.Identity668.Write(null);f.View.Identity678.Write(null);Equal((byte)1,f.Server(new PublicationGateInvocation()),"two known null pointers compare equal");
  f.View.Identity678.Write(new object());Equal((byte)0,f.Server(new PublicationGateInvocation()),"null versus pointer differs");f.View.Identity668.Write(f.View.Identity678.Read(0));f.View.Identity678.Write(null);Equal((byte)0,f.Server(new PublicationGateInvocation()),"pointer versus null differs");scenarios++;
 }
 static void UnknownComparisonStopsAfterBothCaptures(){
  var f=new Fixture();var c=new PublicationGateInvocation();Missing(delegate{f.Server(c);},0x078117,"two unknowns are not equal");Captures(c,0x078110,0x078117);Equal(0x078117u,c.Native.At,"comparison failure prefix");
  f.View.Identity668.Write(null);c=new PublicationGateInvocation();Missing(delegate{f.Server(c);},0x078117,"unknown captured678 is not null");Captures(c,0x078110,0x078117);
  f.View.Identity678.Write(null);f.View.Identity668.Forget();Missing(delegate{f.Server(new PublicationGateInvocation());},0x078117,"unknown668 is not null");Unknown(f.View.Identity668,"failed comparison does not initialize");scenarios++;
 }
 sealed class EqualObjects {
  public override bool Equals(object other){return other is EqualObjects;}
  public override int GetHashCode(){return 1;}
 }
 static void IdentityNotSemanticEquality(){
  var f=new Fixture();var a=new EqualObjects();var b=new EqualObjects();Check(a.Equals(b),"test objects compare semantically equal");f.View.Identity668.Write(a);f.View.Identity678.Write(b);Equal((byte)0,f.Server(new PublicationGateInvocation()),"native pointer comparison ignores Equals");f.View.Identity678.Write(a);Equal((byte)1,f.Server(new PublicationGateInvocation()),"same actual object compares equal");scenarios++;
 }
 static void CharacterLeafDoesNotNeedOwnerOrGlobal(){
  var c=new PublicationGateInvocation();Equal((byte)1,WorldPublicationGates.CharacterDeleteOnEnteringUnloadedLevel016D20(null,c),"leaf permits unread null this");Equal(0x016d22u,c.Native.At,"character return");Captures(c);
  Equal((byte)1,WorldPublicationGates.CharacterDeleteOnEnteringUnloadedLevel016D20(new object(),new PublicationGateInvocation()),"opaque owner also unused");scenarios++;
 }
 static void PlayerInvertsActualEngineResult(){
  var f=new Fixture();f.BindGlobal();f.View.Identity668.Write(null);f.View.Identity678.Write(null);var c=new PublicationGateInvocation();Equal((byte)0,f.Player(c),"server AL1 inverted to AL0");Captures(c,0x3d5bc4,0x3d5bcb,0x078110,0x078117);Equal(0x3d5bddu,c.Native.At,"player return");
  f.View.Identity678.Write(new object());Equal((byte)1,f.Player(new PublicationGateInvocation()),"server AL0 inverted to AL1");Same(f.Global,f.Context.EngineImport609050.Read(0),"import borrows actual global cell");Same(f.Identity,f.Global.Read(0),"query never rewrites global");scenarios++;
 }
 static void PlayerUnknownImportAndGlobalPrefixes(){
  var f=new Fixture();var c=new PublicationGateInvocation();Missing(delegate{f.Player(c);},0x3d5bcb,"unknown import stops at dereference");Captures(c,0x3d5bc4);
  f.Context.EngineImport609050.Write(null);c=new PublicationGateInvocation();Missing(delegate{f.Player(c);},0x3d5bcb,"known null import is not a global cell");Captures(c,0x3d5bc4);
  f.Context.EngineImport609050.Write(f.Global);c=new PublicationGateInvocation();Missing(delegate{f.Player(c);},0x078110,"unknown global consumed at actual Engine dereference");Captures(c,0x3d5bc4,0x3d5bcb);
  f.Global.Write(null);c=new PublicationGateInvocation();Missing(delegate{f.Player(c);},0x078110,"known null Engine is not server or client");Captures(c,0x3d5bc4,0x3d5bcb);scenarios++;
 }
 static void SharedGlobalAndViewAreReread(){
  var f=new Fixture();f.BindGlobal();f.View.Identity668.Write(null);f.View.Identity678.Write(null);Equal((byte)0,f.Player(new PublicationGateInvocation()),"first engine same pointers");
  var second=new EntityEngineState(new WorldEngine());f.Entities.AttachEngine(second);var sidecar=f.Context.BindEngineView(second);sidecar.Identity668.Write(null);sidecar.Identity678.Write(new object());f.World.EngineGlobal.Write(second.Identity);Equal((byte)1,f.Player(new PublicationGateInvocation()),"existing world global mutation observed immediately");Same(second.Identity,f.Global.Read(0),"no copied global");
  sidecar.Identity668.Write(sidecar.Identity678.Read(0));Equal((byte)0,f.Player(new PublicationGateInvocation()),"same bound mutable sidecar reread");Unknown(second.FrameCount640,"other engine field still unknown");Unknown(second.Identity.World50,"no implicit world created");scenarios++;
 }
 static void ImportCellCanBeReboundExplicitly(){
  var f=new Fixture();f.BindGlobal();f.View.Identity668.Write(null);f.View.Identity678.Write(null);var another=new Field<WorldEngine>();f.Context.EngineImport609050.Write(another);Missing(delegate{f.Player(new PublicationGateInvocation());},0x078110,"new unknown borrowed global is observed");another.Write(f.Identity);Equal((byte)0,f.Player(new PublicationGateInvocation()),"explicit new global identity read on next invocation");Same(f.Identity,f.Global.Read(0),"old global not mutated");scenarios++;
 }
 static void UnboundAndReleasedEngineAreNotDefaults(){
  var f=new Fixture();f.BindGlobal();var other=new EntityEngineState(new WorldEngine());f.Entities.AttachEngine(other);f.Global.Write(other.Identity);Missing(delegate{f.Player(new PublicationGateInvocation());},0x078110,"no lazy sidecar for known Engine");
  f.Global.Write(f.Identity);f.View.Identity668.Write(null);f.View.Identity678.Write(null);f.Engine.MarkReleased();var c=new PublicationGateInvocation();Missing(delegate{f.Player(c);},0x078110,"shared released Engine rejected");Captures(c,0x3d5bc4,0x3d5bcb);scenarios++;
 }
 static void DiskLeafIsExplicitAndUnrelatedStateUntouched(){
  var leaf=new OwnObservedDiskPublicationLeaf();var i=new Invocation();Missing(delegate{leaf.BillboardPunctuation00CE70(null,i);},0x00ce70,"loaded leaf not inferred");leaf.SelectObservedDiskImplementation();leaf.BillboardPunctuation00CE70(null,i);Equal(0x00ce70u,i.At,"selected RET0 leaf has no owner dereference");
  var entity=new EntityMovementState(new object());entity.CoordsD8.Position00.D8.Write(0x12345678);leaf.BillboardPunctuation00CE70(entity,i);Equal(0x12345678u,entity.CoordsD8.Position00.D8.Read(0),"no fabricated position write");Unknown(entity.Virtual00,"no vtable initialization");scenarios++;
 }
 public static int Main(){try{
  UnknownDefaultsAndAuthoritativeIdentity();DirectIdentityAndAl();KnownNullIsARealComparedValue();UnknownComparisonStopsAfterBothCaptures();IdentityNotSemanticEquality();CharacterLeafDoesNotNeedOwnerOrGlobal();PlayerInvertsActualEngineResult();PlayerUnknownImportAndGlobalPrefixes();SharedGlobalAndViewAreReread();ImportCellCanBeReboundExplicitly();UnboundAndReleasedEngineAreNotDefaults();DiskLeafIsExplicitAndUnrelatedStateUntouched();
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"own_publication_gates\":true,\"disk_leaf_opt_in\":true,\"loaded_leaf_verified\":false,\"position_publication\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
