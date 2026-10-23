using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

public static class EntitySpatialTreeTests {
 static int assertions,scenarios;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Equal<T>(T expected,T actual,string label){Check(Object.Equals(expected,actual),label);}
 static void Same(object expected,object actual,string label){Check(Object.ReferenceEquals(expected,actual),label);}
 static void Missing(Action action,uint at,string label){try{action();throw new Exception(label+" unexpectedly completed");}catch(MissingContract e){Equal(at,e.Address,label);}}
 static void Unknown<T>(Field<T> field,string label){Missing(delegate{field.Read(0xf001);},0xf001,label);}
 static void Invalid(Action action,string label){bool caught=false;try{action();}catch(InvalidOperationException){caught=true;}Check(caught,label);}
 static void Box(WorldBounds24 b,float x,float y,float z,float hx,float hy,float hz){b.Center00.Write(x);b.Center04.Write(y);b.Center08.Write(z);b.Extent0C.Write(hx);b.Extent10.Write(hy);b.Extent14.Write(hz);}
 sealed class Fixture {
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly ActorBoundingBoxContext Actors;
  public readonly EntitySpatialTreeContext Spatial;
  public readonly SpatialTreeState Tree=new SpatialTreeState(new object());
  public readonly SpatialNodeState Root;
  public Fixture():this(64){}
  public Fixture(int nodeLimit){
   Actors=new ActorBoundingBoxContext(Entities);Spatial=new EntitySpatialTreeContext(nodeLimit);Spatial.UseObservedDiskConstants();Root=Spatial.BindNode(new object());Spatial.BindTree(Tree);Tree.Root10.Write(Root);Tree.ChangeCount08.Write(0);
   // Explicit own initial world extent, not recovered native startup data.
   Box(Root.Box30,0,0,0,16,8,16);Root.VirtualRva00.Write(0x340e60);Root.Parent08.Write(null);Root.Head48.Write(null);Root.Depth50.Write(0);Root.Flags54.Write(0);for(int n=0;n<4;n++)Root.Child(n).Write(null);
  }
  public SpatialEntityView Entity(float x,float y,float z,float hx,float hy,float hz,byte flags){var entity=new EntityMovementState(new object());Entities.Attach(entity);var box=Actors.Bind(entity);Box(box.Box44,x,y,z,hx,hy,hz);var view=Spatial.BindEntity(box);view.FlagsCB.Write(flags);return view;}
  public void Insert(SpatialEntityView e){EntitySpatialTreeContracts.Insert133A10(Root,e,Spatial,new Invocation());}
  public void Update(SpatialEntityView e){EntitySpatialTreeContracts.Update1333E0(Tree,e,Spatial,new Invocation());}
 }
 static int CountEntities(SpatialNodeState node,HashSet<SpatialEntityView> seen){
  int count=0;SpatialEntityView previous=null;for(var e=node.Head48.Read(0);e!=null;e=e.Next128.Read(0)){Check(seen.Add(e),"each entity appears once");Same(node,e.NodeD0.Read(0),"entity owns actual containing node");Same(previous,e.Previous120.Read(0),"list previous is reciprocal");previous=e;count++;}
  for(int index=0;index<4;index++){var child=node.Child(index).Read(0);if(child!=null){Same(node,child.Parent08.Read(0),"child parent identity");count+=CountEntities(child,seen);}}
  return count;
 }
 static void Membership(Fixture f,int count){Equal(count,CountEntities(f.Root,new HashSet<SpatialEntityView>()),"real tree entity count");}
 static void DefaultKnowledgeAndSharedBox(){
  var entities=new EntityMovementRegistry();var original=new EntityMovementState(new object());entities.Attach(original);var actorContext=new ActorBoundingBoxContext(entities);var actor=actorContext.Bind(original);var context=new EntitySpatialTreeContext();var view=context.BindEntity(actor);Same(view,context.BindEntity(actor),"same spatial sidecar reused");Same(original,view.Actor.Entity,"original entity retained");Same(actor.Box44,view.Box44,"same bbox object");Same(actor.Box44.Center00,view.Box44.Center00,"same bbox cell");
  Unknown(view.NodeD0,"node not guessed");Unknown(view.Previous120,"previous not guessed");Unknown(view.Next128,"next not guessed");Unknown(view.FlagsCB,"flags not guessed");Unknown(context.HalfBits34BADC,"constant not defaulted");Unknown(context.SplitBits34BAD8,"split not defaulted");Unknown(context.XorBits34C450,"mask not defaulted");
  var node=context.BindNode(new object());Unknown(node.Head48,"new binding is not initialized root");Unknown(node.Depth50,"depth not defaulted");Unknown(node.Child(0),"child not defaulted");Unknown(node.Box30.Center04,"node Y not defaulted");scenarios++;
 }
 static void RealInsertionCreatesNonemptyChild(){
  var f=new Fixture();var a=f.Entity(8,0,8,1,1,1,1);f.Insert(a);var node=a.NodeD0.Read(0);Check(node!=null&&!Object.ReferenceEquals(node,f.Root),"actual insertion descends from depth0 root");Equal(1,node.Depth50.Read(0),"specific fixture stops at first child");Same(node,f.Root.Child(3).Read(0),"selected quadrant3 actually contains entity");Same(a,node.Head48.Read(0),"new node has a real list member");Same(f.Root,node.Parent08.Read(0),"constructed child parent");Equal(0f,node.Box30.Center04.Read(0),"child constructor Y is +0");Equal((byte)1,node.Flags54.Read(0),"entity flags propagated on insertion");Membership(f,1);scenarios++;
 }
 static void HeadMiddleAndTailRemoval(){
  var f=new Fixture();var a=f.Entity(0,0,0,4,1,4,1);var b=f.Entity(0,0,0,4,1,4,2);var c=f.Entity(0,0,0,4,1,4,4);f.Insert(a);f.Insert(b);f.Insert(c);Same(f.Root,a.NodeD0.Read(0),"broad real bounds remain at depth0");Same(c,f.Root.Head48.Read(0),"latest head");Membership(f,3);
  Same(f.Root,EntitySpatialTreeContracts.Remove133BC0(f.Root,b,f.Spatial,new Invocation()),"middle removal returns live root");Same(a,c.Next128.Read(0),"middle successor splice");Same(c,a.Previous120.Read(0),"middle predecessor splice");Same(null,b.NodeD0.Read(0),"removed node pointer cleared");Same(null,b.Next128.Read(0),"removed next cleared");Same(null,b.Previous120.Read(0),"removed previous cleared");Membership(f,2);
  EntitySpatialTreeContracts.Remove133BC0(f.Root,c,f.Spatial,new Invocation());Same(a,f.Root.Head48.Read(0),"head changed to successor");Same(null,a.Previous120.Read(0),"new head previous cleared");Membership(f,1);EntitySpatialTreeContracts.Remove133BC0(f.Root,a,f.Spatial,new Invocation());Membership(f,0);f.Root.RequireLive(0);scenarios++;
 }
 static void ContainedUpdateUsesNoTreePointer(){
  var f=new Fixture();var a=f.Entity(0,0,0,4,1,4,1);f.Insert(a);a.Box44.Center04.Write(20);
  EntitySpatialTreeContracts.UpdateCaptured1333E0(new Field<object>(),a,f.Spatial,new Invocation());Same(f.Root,a.NodeD0.Read(0),"contained update keeps membership");Equal(0u,f.Tree.ChangeCount08.Read(0),"no reinsert counter on inside path");Equal(6.5f,f.Root.Box30.Center04.Read(0),"Y union midpoint");Equal(14.5f,f.Root.Box30.Extent10.Read(0),"Y union extent");Membership(f,1);scenarios++;
 }
 static void NonemptyTreeCrossQuadrantMove(){
  var f=new Fixture();var anchor=f.Entity(0,0,0,4,1,4,2);var moving=f.Entity(8,0,8,1,1,1,1);f.Insert(anchor);f.Insert(moving);var old=moving.NodeD0.Read(0);var shared=moving.Actor.Box44.Center00;shared.Write(-8);f.Update(moving);
  var current=moving.NodeD0.Read(0);Check(!Object.ReferenceEquals(old,current),"cross-quadrant update installs another actual node");Same(current,f.Root.Child(2).Read(0),"new quadrant selected");Same(null,f.Root.Child(3).Read(0),"old empty child removed");Same(shared,moving.Box44.Center00,"bbox input uses original cell");Equal(1u,f.Tree.ChangeCount08.Read(0),"one actual reinsert counted");Missing(delegate{old.RequireLive(0x55);},0x55,"old node explicitly invalidated");Membership(f,2);scenarios++;
 }
 static void RepeatedMovementReclaimsActiveCapacity(){
  var f=new Fixture(2);var anchor=f.Entity(0,0,0,4,1,4,2);var moving=f.Entity(8,0,8,1,1,1,1);f.Insert(anchor);f.Insert(moving);var first=moving.NodeD0.Read(0);
  for(int n=0;n<40;n++){int quadrant=n%4;moving.Box44.Center00.Write((quadrant==0||quadrant==1)?-8:8);moving.Box44.Center08.Write((quadrant==1||quadrant==2)?-8:8);f.Update(moving);Check(moving.NodeD0.Read(0)!=null,"repeated move has real membership");Equal(2,f.Spatial.ActiveNodeCount,"only live root and one child consume capacity");}
  Equal(40u,f.Tree.ChangeCount08.Read(0),"all40 moves completed under capacity2");Membership(f,2);Missing(delegate{first.RequireLive(0x56);},0x56,"old borrowed node remains invalid");Invalid(delegate{f.Spatial.BindNode(first.Identity);},"released identity cannot silently rebind");scenarios++;
 }
 static void UnknownGlobalTreeStopsAfterDetach(){
  var f=new Fixture();var a=f.Entity(0,0,0,4,1,4,1);f.Insert(a);a.Box44.Center00.Write(40);Missing(delegate{EntitySpatialTreeContracts.UpdateCaptured1333E0(new Field<object>(),a,f.Spatial,new Invocation());},0x13347c,"unknown tree consumed at root dereference only");Same(null,a.NodeD0.Read(0),"detach already happened");Same(null,f.Root.Head48.Read(0),"list detach retained");Equal(0u,f.Tree.ChangeCount08.Read(0),"counter not reached");scenarios++;
 }
 static void RootPointerConsumptionFollowsEntityArithmetic(){
  var f=new Fixture();var a=f.Entity(0,0,0,4,1,4,1);f.Insert(a);a.Box44.Center00.Write(40);a.Box44.Center08.Forget();f.Tree.Root10.Forget();Missing(delegate{f.Update(a);},0x133a24,"unknown Z arithmetic precedes unknown captured root");Same(null,a.NodeD0.Read(0),"old membership removed first");Unknown(f.Tree.Root10,"unknown root remains untouched");Equal(0u,f.Tree.ChangeCount08.Read(0),"counter still not reached");scenarios++;
 }
 static void UnknownNodeAndPreviousPrefixes(){
  var f=new Fixture();var a=f.Entity(0,0,0,4,1,4,1);Missing(delegate{f.Update(a);},0x1333f7,"unobserved membership does not mean unregistered");Same(null,f.Root.Head48.Read(0),"unobserved membership has no tree write");f.Insert(a);a.Previous120.Forget();Missing(delegate{EntitySpatialTreeContracts.Remove133BC0(f.Root,a,f.Spatial,new Invocation());},0x133bdd,"unknown previous consumed after node clear");Same(null,a.NodeD0.Read(0),"node clear prefix retained");Same(a,f.Root.Head48.Read(0),"head not changed before previous branch");scenarios++;
 }
 static void UnknownNextIsCopiedBeforeLaterConsumption(){
  var f=new Fixture();var a=f.Entity(0,0,0,4,1,4,1);f.Insert(a);a.Next128.Forget();Missing(delegate{EntitySpatialTreeContracts.Remove133BC0(f.Root,a,f.Spatial,new Invocation());},0x133bf6,"next opaque copy precedes test");Unknown(f.Root.Head48,"unknown next copied into head");Same(null,a.NodeD0.Read(0),"clear prefix retained");Unknown(a.Next128,"later null writes not reached");scenarios++;
 }
 static void UnknownFlagsAfterBoundsBeforeMembership(){
  var f=new Fixture();var a=f.Entity(40,0,0,1,1,1,1);a.FlagsCB.Forget();Missing(delegate{f.Insert(a);},0x133b53,"flags unknown after box writes");Equal(12.5f,f.Root.Box30.Center00.Read(0),"node union center published before flag failure");Equal(28.5f,f.Root.Box30.Extent0C.Read(0),"node union extent published");Unknown(a.NodeD0,"membership store not reached");Same(null,f.Root.Head48.Read(0),"head unchanged");scenarios++;
 }
 static void UnknownHeadAfterMembershipStore(){
  var f=new Fixture();var a=f.Entity(0,0,0,4,1,4,1);f.Root.Head48.Forget();Missing(delegate{f.Insert(a);},0x133b61,"unknown head is consumed after NodeD0 store");Same(f.Root,a.NodeD0.Read(0),"membership pointer written before failure");Unknown(a.Previous120,"list previous not written yet");Unknown(a.Next128,"list next not written yet");scenarios++;
 }
 static void ConstantUnknownNeverGetsDefault(){
  var f=new Fixture();var a=f.Entity(40,0,0,1,1,1,1);f.Spatial.HalfBits34BADC.Forget();Missing(delegate{f.Insert(a);},0x133b02,"unknown half stops first arithmetic use");Equal(0f,f.Root.Box30.Center00.Read(0),"box write not reached");Unknown(a.NodeD0,"membership not reached");
  f=new Fixture();a=f.Entity(8,0,8,1,1,1,1);f.Spatial.SplitBits34BAD8.Forget();Missing(delegate{f.Insert(a);},0x13455c,"unknown split stops descent");Same(f.Root,a.NodeD0.Read(0),"real insertion prefix retained");Same(a,f.Root.Head48.Read(0),"prefix has real list member");Same(null,f.Root.Child(3).Read(0),"no fabricated child");scenarios++;
 }
 static void AllocationDomainStopsBeforeUnlink(){
  var f=new Fixture(1);var a=f.Entity(8,0,8,1,1,1,1);Missing(delegate{f.Insert(a);},0x13465c,"explicit own capacity stop at allocation");Same(f.Root,a.NodeD0.Read(0),"entity still in parent list");Same(a,f.Root.Head48.Read(0),"head retained");Same(null,f.Root.Child(3).Read(0),"child unpublished");Membership(f,1);scenarios++;
 }
 static void YPropagationAndUnknownParentPrefix(){
  var f=new Fixture();var a=f.Entity(8,0,8,1,1,1,1);f.Insert(a);var child=a.NodeD0.Read(0);EntitySpatialTreeContracts.ExpandY11BE80(child,-2,20,f.Spatial,new Invocation());Equal(8f,child.Box30.Center04.Read(0),"child expanded Y center");Equal(12f,child.Box30.Extent10.Read(0),"child expanded Y extent");Equal(6f,f.Root.Box30.Center04.Read(0),"ancestor expanded Y center");Equal(14f,f.Root.Box30.Extent10.Read(0),"ancestor expanded Y extent");
  child.Parent08.Forget();Missing(delegate{EntitySpatialTreeContracts.ExpandY11BE80(child,-3,24,f.Spatial,new Invocation());},0x11bee0,"parent unknown only after Y writes");Equal(10f,child.Box30.Center04.Read(0),"Y write before parent failure");Equal(14f,child.Box30.Extent10.Read(0),"extent write before parent failure");scenarios++;
 }
 static void YUnorderedAndUnusedHalfSemantics(){
  var f=new Fixture();f.Spatial.HalfBits34BADC.Forget();EntitySpatialTreeContracts.ExpandY11BE80(f.Root,-1,Single.NaN,f.Spatial,new Invocation());Equal(0f,f.Root.Box30.Center04.Read(0),"unordered maximum does not select candidate");Equal(8f,f.Root.Box30.Extent10.Read(0),"contained minimum returns without consuming half");
  f.Spatial.UseObservedDiskConstants();EntitySpatialTreeContracts.ExpandY11BE80(f.Root,Single.NaN,1,f.Spatial,new Invocation());Check(Single.IsNaN(f.Root.Box30.Center04.Read(0)),"unordered minimum follows carry and writes NaN");Check(Single.IsNaN(f.Root.Box30.Extent10.Read(0)),"NaN retained rather than clamped");scenarios++;
 }
 static void CounterFailureRetainsCompletedReinsert(){
  var f=new Fixture();var anchor=f.Entity(0,0,0,4,1,4,2);var moving=f.Entity(8,0,8,1,1,1,1);f.Insert(anchor);f.Insert(moving);moving.Box44.Center00.Write(-8);f.Tree.ChangeCount08.Forget();Missing(delegate{f.Update(moving);},0x133488,"unknown counter consumed after completed reinsert");Same(f.Root.Child(2).Read(0),moving.NodeD0.Read(0),"new membership retained");Same(null,f.Root.Child(3).Read(0),"old node already removed");Membership(f,2);scenarios++;
 }
 static void NullNodeDoesNotConsumeTreeButIsNotProductionFixture(){
  var f=new Fixture();var a=f.Entity(0,0,0,1,1,1,0);a.NodeD0.Write(null);EntitySpatialTreeContracts.UpdateCaptured1333E0(new Field<object>(),a,f.Spatial,new Invocation());Same(null,f.Root.Head48.Read(0),"native no-membership branch has no insertion");Unknown(a.Previous120,"no fake list initialization");Equal(0u,f.Tree.ChangeCount08.Read(0),"no claimed update");scenarios++;
 }
 static SpatialNodeState DestructorNode(EntitySpatialTreeContext context){var node=context.BindNode(new object());node.VirtualRva00.Write(0x340e60);for(int index=0;index<4;index++)node.Child(index).Write(null);return node;}
 static void RecursiveDestructorFlagsAndReturnIdentity(){
  var c=new EntitySpatialTreeContext();var parent=DestructorNode(c);var child=DestructorNode(c);var grandchild=DestructorNode(c);parent.Child(2).Write(child);child.Child(1).Write(grandchild);parent.VirtualRva00.Forget();
  Same(parent,EntitySpatialTreeContracts.DestroyNode11BB30(parent,0x100,c,new Invocation()),"flags low bit0 returns original still allocated pointer");Equal(0x340e60u,parent.VirtualRva00.Read(0),"self vptr written before child traversal");Same(null,parent.Child(2).Read(0),"parent clears child only after recursive return");Same(null,child.Child(1).Read(0),"grandchild slot cleared before child free");Equal(1,c.ActiveNodeCount,"recursive child allocations released");parent.RequireLive(0);Missing(delegate{child.RequireLive(0x61);},0x61,"child released");Missing(delegate{grandchild.RequireLive(0x62);},0x62,"grandchild released");Unknown(parent.Head48,"destructor does not initialize entity head");Unknown(parent.Box30.Center00,"destructor does not read or initialize bounds");
  Same(parent,EntitySpatialTreeContracts.DestroyNode11BB30(parent,0x101,c,new Invocation()),"flags low bit1 still returns original pointer after own free");Equal(0,c.ActiveNodeCount,"self allocation released");Missing(delegate{parent.RequireLive(0x63);},0x63,"returned freed pointer is not live");scenarios++;
 }
 static void DestructorUnknownSlotPreservesCompletedPrefix(){
  var c=new EntitySpatialTreeContext();var parent=DestructorNode(c);var child=DestructorNode(c);parent.Child(0).Write(child);parent.Child(1).Forget();Missing(delegate{EntitySpatialTreeContracts.DestroyNode11BB30(parent,1,c,new Invocation());},0x11bb68,"unknown later child does not erase successful earlier destroy");Same(null,parent.Child(0).Read(0),"earlier slot cleared");Unknown(parent.Child(1),"failing slot remains unknown");Missing(delegate{child.RequireLive(0x64);},0x64,"earlier child freed");Equal(1,c.ActiveNodeCount,"parent retained on unresolved child");parent.RequireLive(0);scenarios++;
 }
 static void DestructorRejectsUnknownDynamicDispatch(){
  var c=new EntitySpatialTreeContext();var parent=DestructorNode(c);var child=DestructorNode(c);parent.Child(0).Write(child);child.VirtualRva00.Forget();Missing(delegate{EntitySpatialTreeContracts.DestroyNode11BB30(parent,1,c,new Invocation());},0x11bb75,"unknown child table does not select disk destructor");Same(child,parent.Child(0).Read(0),"child slot not cleared before failed call");Equal(2,c.ActiveNodeCount,"both allocations retained");child.VirtualRva00.Write(0x1234);Missing(delegate{EntitySpatialTreeContracts.DestroyNode11BB30(parent,1,c,new Invocation());},0x11bb75,"different dynamic table unsupported");Equal(2,c.ActiveNodeCount,"different table did not free");scenarios++;
 }
 static void DestructorAliasAndCycleStops(){
  var c=new EntitySpatialTreeContext();var parent=DestructorNode(c);var child=DestructorNode(c);parent.Child(0).Write(child);parent.Child(1).Write(child);Missing(delegate{EntitySpatialTreeContracts.DestroyNode11BB30(parent,1,c,new Invocation());},0x11bb6d,"second alias is use-after-release at table dereference");Same(null,parent.Child(0).Read(0),"first alias slot cleared");Same(child,parent.Child(1).Read(0),"second alias retained at failure");Equal(1,c.ActiveNodeCount,"no double free");
  c=new EntitySpatialTreeContext();parent=DestructorNode(c);parent.Child(0).Write(parent);Missing(delegate{EntitySpatialTreeContracts.DestroyNode11BB30(parent,1,c,new Invocation());},0x11bb75,"own finite domain rejects recursive self alias");Same(parent,parent.Child(0).Read(0),"cycle not replaced by invented cleanup");Equal(1,c.ActiveNodeCount,"cycle stop retains live allocation");scenarios++;
 }
 static void PruneDispatchFailureRetainsUnlinkedState(){
  var f=new Fixture();var moving=f.Entity(8,0,8,1,1,1,1);f.Insert(moving);var old=moving.NodeD0.Read(0);old.VirtualRva00.Forget();moving.Box44.Center00.Write(-8);Missing(delegate{f.Update(moving);},0x11bc05,"prune invokes actual captured node table");Same(null,moving.NodeD0.Read(0),"entity detach prefix retained");Same(null,old.Head48.Read(0),"old list already empty");Same(old,f.Root.Child(3).Read(0),"parent slot not cleared before destructor return");Equal(2,f.Spatial.ActiveNodeCount,"allocation not reclaimed before successful destructor");Equal(0u,f.Tree.ChangeCount08.Read(0),"reinsert not reached");old.RequireLive(0);scenarios++;
 }
 public static int Main(){try{
  DefaultKnowledgeAndSharedBox();RealInsertionCreatesNonemptyChild();HeadMiddleAndTailRemoval();ContainedUpdateUsesNoTreePointer();NonemptyTreeCrossQuadrantMove();RepeatedMovementReclaimsActiveCapacity();UnknownGlobalTreeStopsAfterDetach();RootPointerConsumptionFollowsEntityArithmetic();UnknownNodeAndPreviousPrefixes();UnknownNextIsCopiedBeforeLaterConsumption();UnknownFlagsAfterBoundsBeforeMembership();UnknownHeadAfterMembershipStore();ConstantUnknownNeverGetsDefault();AllocationDomainStopsBeforeUnlink();YPropagationAndUnknownParentPrefix();YUnorderedAndUnusedHalfSemantics();CounterFailureRetainsCompletedReinsert();NullNodeDoesNotConsumeTreeButIsNotProductionFixture();RecursiveDestructorFlagsAndReturnIdentity();DestructorUnknownSlotPreservesCompletedPrefix();DestructorRejectsUnknownDynamicDispatch();DestructorAliasAndCycleStops();PruneDispatchFailureRetainsUnlinkedState();
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"own_nonempty_tree_moves\":true,\"shared_actor_bounds\":true,\"loaded_native_verified\":false,\"position_publication\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
