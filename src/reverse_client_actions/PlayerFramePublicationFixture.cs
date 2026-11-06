using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Test setup only: extends the existing Ground/Player/animation composition.
// These are declared OWN frame-publication inputs, not native initialization,
// resource mesh geometry, a server-mode producer or an alternate Character.
// The main test owns FrameCount640/LastFrame178 for each frame; this helper
// never writes them, Agent flags/callbacks, current XYZ or either Player table.
public sealed class PlayerFramePublicationFixture {
 public readonly GameMovedContext Moved;
 public readonly GameRotationPublicationContext Rotation;
 public readonly GameRotationState RotationState;
 public readonly WorldSameRegionPublicationContext Publication;
 public readonly EntitySpatialTreeContext Spatial;
 public readonly WorldPublicationGateContext Gates;
 public readonly PublicationGateEngineView GateEngine;
 public readonly SpatialTreeState Tree;
 public readonly SpatialNodeState Node;
 public readonly SpatialEntityView SpatialEntity;

 static void Same(object expected,object actual,string name){if(!Object.ReferenceEquals(expected,actual))throw new ArgumentException("Publication fixture must borrow original "+name);}
 static bool TryRead<T>(Field<T> field,out T value){try{value=field.Read(0);return true;}catch(MissingContract){value=default(T);return false;}}
 // Only an unobserved input may be initialized. A different existing value is
 // not reset to make this fixture pass, including already published geometry.
 static void Seed<T>(Field<T> field,T value,string name){T current;if(TryRead(field,out current)){if(!EqualityComparer<T>.Default.Equals(current,value))throw new InvalidOperationException("Existing "+name+" differs from this OWN fixture");return;}field.Write(value);}
 static void Box(WorldBounds24 box,float x,float y,float z,float ex,float ey,float ez,string name){
  Seed(box.Center00,x,name+" center X");Seed(box.Center04,y,name+" center Y");Seed(box.Center08,z,name+" center Z");
  Seed(box.Extent0C,ex,name+" extent X");Seed(box.Extent10,ey,name+" extent Y");Seed(box.Extent14,ez,name+" extent Z");
 }
 static void RequireEmpty(NavigationCrowdPointerVector<SkillMovedTarget> vector,string name){
  var begin=vector.Begin00.Read(0);var end=vector.End08.Read(0);
  if(begin==null?end!=null:end==null||!Object.ReferenceEquals(begin.Buffer,end.Buffer)||begin.Index!=end.Index)throw new InvalidOperationException(name+" must already be an explicit empty collection");
 }
 static void InitializeEmpty228(NavigationCrowdPointerVector<SkillMovedTarget> vector){
  NavigationCrowdPointerAddress<SkillMovedTarget> begin,end;
  bool knownBegin=TryRead(vector.Begin00,out begin),knownEnd=TryRead(vector.End08,out end);
  if((knownBegin&&begin!=null)||(knownEnd&&end!=null)){RequireEmpty(vector,"SkillMoved228");return;}
  if(!knownBegin)vector.Begin00.Write(null);if(!knownEnd)vector.End08.Write(null);
 }
 public PlayerFramePublicationFixture(GameMoveBeginResourceIntegrationTests.Fixture move,NavigationSearchWrapperTests.Scene scene,SystemUcrtRotationMath math){
  if(move==null||scene==null||math==null)throw new ArgumentNullException("existing publication fixture");
  var request=move.Request;var entity=request.Owner.Entity;
  Same(scene,move.Movement.Scene,"saved scene");Same(scene.Floor,request.Request.Path.Navigation.Floor,"Floor");Same(scene.Regions,request.World.State,"coordinates");
  Same(request.Callbacks.Entities,move.Bounds.Entities,"Entity registry");Same(entity,move.Actor.Entity,"Actor Entity");Same(move.Actor,move.Bounds.Bind(entity),"Actor view");
  Same(move.Mesh,move.Actor.Mesh2668.Read(0),"animation mesh");Same(request.Owner,request.Callbacks.Owner(request.Owner.Interface38,0),"callback owner");
  Same(request.Owner.Interface38,move.Movement.Agent.Callback08.Read(0),"Agent callback");Same(GameMovementPrimaryTable.Player,entity.Virtual00.Read(0),"Player primary");Same(GameMovementInterfaceTable.Player,request.Owner.Interface38.Virtual38.Read(0),"Player interface");
  Same(request.EngineLifetime,request.Callbacks.Entities.Engine(request.Engine,0),"Engine lifetime");Same(request.Movement.EngineGlobal,request.Callbacks.World.EngineGlobal,"Engine global cell");
  Same(scene.Level.Level,scene.RegionView.Publication.Level68.Read(0),"published Level");
  var skills=SkillMovedManagerState.ForOwner(request.Owner);
  // These two vectors already belong to the request's SkillReference manager.
  // Check their existing emptiness; do not call UseOwnEmptyCollections and
  // erase them. Only the previously unobserved228 endpoints are established.
  RequireEmpty(skills.Collections258,"SkillMoved258");RequireEmpty(skills.Collections360,"SkillMoved360");InitializeEmpty228(skills.Collections228);

  // The old Moved fixture's explicit identity pose and test mesh box. Retain
  // this exact mesh object used by animation; dimensions are not decoded assets.
  for(int n=0;n<9;n++)Seed(entity.CoordsD8.Basis18[n],n%4==0?1f:0f,"Entity basis"+n);
  Seed(move.Bounds.Mask34C410,0x7fffffffu,"observed bounds mask");
  Box(move.Mesh.Bounds1A8,0,0,0,1,2,3,"OWN mesh box");

  Gates=new WorldPublicationGateContext(request.Callbacks.Entities);
  Gates.EngineImport609050.Write(request.Movement.EngineGlobal);GateEngine=Gates.BindEngineView(request.EngineLifetime);
  // Explicit OWN non-server relation: two different nonnull identities make
  // actual078110 return0. Player3D5BC0 then returns1; Ready538C remains explicit.
  GateEngine.Identity668.Write(new object());GateEngine.Identity678.Write(new object());
  Spatial=new EntitySpatialTreeContext();Spatial.UseObservedDiskConstants();
  Publication=new WorldSameRegionPublicationContext(scene.Floor.Publications,move.Bounds,Gates,Spatial);
  Publication.AttachRegion(scene.RegionView.Publication);Publication.SelectObservedDiskNoOpLeaves();SpatialEntity=Publication.BindEntity(move.Actor);
  Tree=new SpatialTreeState(new object());Spatial.BindTree(Tree);Node=Spatial.BindNode(new object());
  Tree.Root10.Write(Node);Tree.ChangeCount08.Write(7);Node.Depth50.Write(8);Node.Parent08.Write(null);Node.Head48.Write(SpatialEntity);Node.Flags54.Write(0);Node.VirtualRva00.Write(0x340e60);
  Box(Node.Box30,64,0,64,200,1,200,"OWN spatial node");for(int n=0;n<4;n++)Node.Child(n).Write(null);
  SpatialEntity.NodeD0.Write(Node);SpatialEntity.Previous120.Write(null);SpatialEntity.Next128.Write(null);SpatialEntity.FlagsCB.Write(2);
  Seed(scene.RegionView.Publication.Parent30,(object)null,"Region parent");
  Seed(scene.Level.Level.Region08,77UL,"Level region");Seed(scene.Level.Level.Ready538C,(byte)1,"Level ready");Seed(scene.Level.Level.Spatial10,Tree.Identity,"Level spatial tree");
  // Existing Region offsets/bounds, lock, landscape, water, actor/neighbor
  // lists and Level68 are borrowed unchanged. Reset88 is written by1943E0.
  Rotation=new GameRotationPublicationContext(request.Callbacks,Publication,math);Rotation.Constants.UseObservedDiskConstants();RotationState=Rotation.Bind(request.Owner);
  RotationState.Flag26B0.Write(0);RotationState.Rate45A8.Write(1);RotationState.Rate45AC.Write(1);RotationState.Rate45B0.Write(1);
  Moved=new GameMovedContext(Rotation,scene.Floor);
 }
}
