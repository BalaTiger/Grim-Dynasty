using System;
using GrimDynasty.ReverseClient.Actions;

public static class EntitySpatialTreeReadPointTests {
 static int assertions,scenarios;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Equal<T>(T expected,T actual,string label){Check(Object.Equals(expected,actual),label);}
 static void Same(object expected,object actual,string label){Check(Object.ReferenceEquals(expected,actual),label);}
 static void Missing(Action action,uint at,string label){try{action();throw new Exception(label+" unexpectedly completed");}catch(MissingContract e){Equal(at,e.Address,label);}}
 static void Unknown<T>(Field<T> field,string label){Missing(delegate{field.Read(0xf001);},0xf001,label);}
 static SpatialNodeState EmptyOwnNode(EntitySpatialTreeContext context){var node=context.BindNode(new object());node.VirtualRva00.Write(0x340e60);for(int index=0;index<4;index++)node.Child(index).Write(null);return node;}
 static void KnownNullParentStopsAtFirstRead(){
  var context=new EntitySpatialTreeContext();var target=context.BindNode(new object());var invocation=new Invocation();
  Missing(delegate{EntitySpatialTreeContracts.Prune11BBC0(null,target,context,invocation);},0x11bbf0,"known-null parent stops at MOV, not LEA");Equal(0x11bbf0u,invocation.At,"recorded first read address");Equal(1,context.ActiveNodeCount,"no allocation freed");Unknown(target.VirtualRva00,"target destructor not entered");Unknown(target.Child(0),"target children untouched");target.RequireLive(0);scenarios++;
 }
 static void ReleasedParentStopsBeforeTargetEffects(){
  var context=new EntitySpatialTreeContext();var parent=EmptyOwnNode(context);var target=context.BindNode(new object());EntitySpatialTreeContracts.DestroyNode11BB30(parent,1,context,new Invocation());var invocation=new Invocation();
  Missing(delegate{EntitySpatialTreeContracts.Prune11BBC0(parent,target,context,invocation);},0x11bbf0,"released parent rejected at first memory read");Equal(0x11bbf0u,invocation.At,"recorded released-parent read");Equal(1,context.ActiveNodeCount,"already released parent not freed twice");Unknown(parent.Head48,"unread parent field not defaulted");Unknown(target.VirtualRva00,"target not dispatched");Unknown(target.Child(0),"target not cleared");target.RequireLive(0);scenarios++;
 }
 static void ForeignUnboundParentHasNoMutation(){
  var context=new EntitySpatialTreeContext();var foreign=new EntitySpatialTreeContext();var parent=foreign.BindNode(new object());var target=EmptyOwnNode(context);parent.Child(0).Write(target);var invocation=new Invocation();
  Missing(delegate{EntitySpatialTreeContracts.Prune11BBC0(parent,target,context,invocation);},0x11bbf0,"unbound parent rejected before child traversal");Equal(0x11bbf0u,invocation.At,"recorded foreign-parent read");Same(target,parent.Child(0).Read(0),"matching child not cleared");Equal(1,context.ActiveNodeCount,"target not released");Equal(1,foreign.ActiveNodeCount,"foreign parent not adopted or freed");Unknown(parent.Head48,"foreign head not read or initialized");target.RequireLive(0);scenarios++;
 }
 public static int Main(){try{
  KnownNullParentStopsAtFirstRead();ReleasedParentStopsBeforeTargetEffects();ForeignUnboundParentHasNoMutation();
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"new_prune_readpoint_only\":true,\"original23_rerun\":false,\"loaded_native_verified\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
