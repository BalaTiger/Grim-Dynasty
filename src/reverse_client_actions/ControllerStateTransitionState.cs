using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // One40-byte backing layout, matching the existing163490 caller payload.
 // Name Zero00/Zero08 belongs to that caller only: these cells may hold any
 // observed value in a controller. DWORD reads borrow the QWORD00 bits; they
 // do not introduce a second independently mutable target-ID representation.
 public sealed class ControllerStateData40 {
  public readonly PlayerMoveTransitionPayload Storage;
  public Field<ulong> Qword00{get{return Storage.Zero00;}}
  public Field<uint> Dword08{get{return Storage.Zero08;}}
  public Field<uint> Padding0C{get{return Storage.Padding0C;}}
  public WorldPosition24 Position10{get{return Storage.Destination10;}}
  public ControllerStateData40():this(new PlayerMoveTransitionPayload()){}
  public ControllerStateData40(PlayerMoveTransitionPayload storage){if(storage==null)throw new ArgumentNullException("existing state-data storage");Storage=storage;}
  public uint ReadDword00(uint at){return unchecked((uint)Qword00.Read(at));}
  public uint ReadDword04(uint at){return unchecked((uint)(Qword00.Read(at)>>32));}
 }
 // Explicit OWN finite nodes: no native allocation or default tree is inferred.
 // The name storage is the same SSO layout as the163490 caller's name.
 public sealed class ControllerStateRegistryNode:NavigationView {
  public readonly Field<ControllerStateRegistryNode> Left00=new Field<ControllerStateRegistryNode>(),Parent08=new Field<ControllerStateRegistryNode>(),Right10=new Field<ControllerStateRegistryNode>();
  public readonly Field<byte> Nil19=new Field<byte>();
  public readonly PlayerMoveShortName Name20=new PlayerMoveShortName();
  public readonly Field<PlayerMouseState> State40=new Field<PlayerMouseState>();
 }
 public sealed class ControllerStateTransitionState {
  static readonly ConditionalWeakTable<Controller,ControllerStateTransitionState> states=new ConditionalWeakTable<Controller,ControllerStateTransitionState>();
  static readonly object gate=new object();
  public readonly MouseControllerState Mouse;
  public readonly Field<PlayerMouseState> Current2A0;
  public readonly Field<MouseStateList> Overlay2E8;
  public readonly Field<ulong> Count2F0;
  public readonly Field<ControllerStateRegistryNode> Header290=new Field<ControllerStateRegistryNode>();
  public readonly ControllerStateData40 Data2A8=new ControllerStateData40();
  public readonly Field<byte> Changed2D0=new Field<byte>();
  ControllerStateTransitionState(MouseControllerState mouse){Mouse=mouse;Current2A0=mouse.State2A0;Overlay2E8=mouse.Overlay2E8;Count2F0=mouse.Count2F0;}
  public static ControllerStateTransitionState Bind(PlayerMouseContext context,MouseControllerState mouse){
   if(context==null||mouse==null)throw new ArgumentNullException("existing controller composition");if(!Object.ReferenceEquals(context.Controller(mouse.Identity,0),mouse))throw new ArgumentException("Controller must be the original registered mouse view");
   lock(gate){ControllerStateTransitionState old;if(states.TryGetValue(mouse.Identity,out old)){if(!Object.ReferenceEquals(old.Mouse,mouse))throw new InvalidOperationException("Controller already has another transition view");return old;}var value=new ControllerStateTransitionState(mouse);states.Add(mouse.Identity,value);return value;}
  }
  public static ControllerStateTransitionState ForBound(PlayerMouseContext context,Controller controller,uint at){
   if(context==null)throw new MissingContract(at,"Missing original mouse context");var mouse=context.Controller(controller,at);ControllerStateTransitionState value;
   if(!states.TryGetValue(controller,out value)||!Object.ReferenceEquals(value.Mouse,mouse))throw new MissingContract(at,"Controller transition cells require explicit unique binding");return value;
  }
 }
 // The frozen MouseStateList/MouseStateNode are distinct typed views. An
 // explicit one-to-one OWN association declares the sentinel node as the
 // same address as its header. Binding writes no First00/Count2F0 value.
 public sealed class ControllerOverlaySentinel {
  static readonly ConditionalWeakTable<MouseStateList,ControllerOverlaySentinel> headers=new ConditionalWeakTable<MouseStateList,ControllerOverlaySentinel>();
  static readonly ConditionalWeakTable<MouseStateNode,ControllerOverlaySentinel> nodes=new ConditionalWeakTable<MouseStateNode,ControllerOverlaySentinel>();
  static readonly object gate=new object();
  public readonly MouseStateList Header;
  public readonly MouseStateNode Node;
  ControllerOverlaySentinel(MouseStateList header,MouseStateNode node){Header=header;Node=node;}
  public static ControllerOverlaySentinel BindOwnSameAddress(MouseStateList header,MouseStateNode node){
   if(header==null||node==null)throw new ArgumentNullException("existing header/node views");
   lock(gate){ControllerOverlaySentinel a,b;bool ah=headers.TryGetValue(header,out a),bh=nodes.TryGetValue(node,out b);if(ah||bh){if(!ah||!bh||!Object.ReferenceEquals(a,b))throw new InvalidOperationException("Overlay sentinel views cannot be rebound");return a;}var value=new ControllerOverlaySentinel(header,node);headers.Add(header,value);nodes.Add(node,value);return value;}
  }
  public static bool SameAddress(MouseStateNode node,MouseStateList header,uint at){ControllerOverlaySentinel value;if(header==null||!headers.TryGetValue(header,out value))throw new MissingContract(at,"OWN overlay comparison requires an explicit header/sentinel address association");return Object.ReferenceEquals(node,value.Node);}
 }
 public sealed class ControllerOverlayNodeFields {
  static readonly ConditionalWeakTable<MouseStateNode,ControllerOverlayNodeFields> nodes=new ConditionalWeakTable<MouseStateNode,ControllerOverlayNodeFields>();
  public readonly MouseStateNode Node;
  public readonly NavigationView Lifetime;
  public readonly Field<MouseStateNode> Next00=new Field<MouseStateNode>();
  public readonly ControllerStateData40 Data18=new ControllerStateData40();
  ControllerOverlayNodeFields(MouseStateNode node,NavigationView lifetime){Node=node;Lifetime=lifetime;}
  public static ControllerOverlayNodeFields Bind(MouseStateNode node,NavigationView lifetime){if(node==null||lifetime==null)throw new ArgumentNullException("existing overlay node/lifetime");lifetime.RequireLive(0);lock(nodes){ControllerOverlayNodeFields old;if(nodes.TryGetValue(node,out old)){if(!Object.ReferenceEquals(old.Lifetime,lifetime))throw new InvalidOperationException("Overlay node lifetime cannot be replaced");return old;}var value=new ControllerOverlayNodeFields(node,lifetime);nodes.Add(node,value);return value;}}
  public static ControllerOverlayNodeFields ForBound(MouseStateNode node,uint at){ControllerOverlayNodeFields value;if(node==null||!nodes.TryGetValue(node,out value))throw new MissingContract(at,"Overlay node requires an explicitly bound lifetime/storage view");value.Lifetime.RequireLive(at);return value;}
 }
 // Existing state identity and existing virtual-pointer cell are borrowed.
 // These are only the newly represented inline MoveTo fields. An explicit
 // live OWN lifetime is required; binding does not initialize native fields.
 public sealed class ControllerPlayerStateFields {
  static readonly ConditionalWeakTable<object,ControllerPlayerStateFields> states=new ConditionalWeakTable<object,ControllerPlayerStateFields>();
  public readonly PlayerMouseState State;
  public readonly NavigationView Lifetime;
  public readonly Field<byte> Byte18=new Field<byte>();
  public readonly Displacement3 Direction20=new Displacement3();
  public readonly WorldPosition24 Previous30=new WorldPosition24();
  ControllerPlayerStateFields(PlayerMouseState state,NavigationView lifetime){State=state;Lifetime=lifetime;}
  public static ControllerPlayerStateFields Bind(PlayerMouseState state,NavigationView lifetime){
   if(state==null||lifetime==null)throw new ArgumentNullException("existing state/lifetime");lifetime.RequireLive(0);lock(states){ControllerPlayerStateFields old;if(states.TryGetValue(state.Identity,out old)){if(!Object.ReferenceEquals(old.State,state)||!Object.ReferenceEquals(old.Lifetime,lifetime))throw new InvalidOperationException("State identity/lifetime view cannot be replaced");return old;}var value=new ControllerPlayerStateFields(state,lifetime);states.Add(state.Identity,value);return value;}
  }
  public static ControllerPlayerStateFields ForBound(PlayerMouseState state,uint at){ControllerPlayerStateFields value;if(state==null||!states.TryGetValue(state.Identity,out value)||!Object.ReferenceEquals(value.State,state))throw new MissingContract(at,"State requires an explicitly bound unique identity/lifetime view");value.Lifetime.RequireLive(at);return value;}
 }
 public sealed class ControllerPlayerLifecycleSlots {
  static readonly ConditionalWeakTable<PlayerMouseTable,ControllerPlayerLifecycleSlots> tables=new ConditionalWeakTable<PlayerMouseTable,ControllerPlayerLifecycleSlots>();
  public readonly PlayerMouseTable Table;
  public readonly Field<uint> Begin220=new Field<uint>(),End228=new Field<uint>(),Select210=new Field<uint>();
  ControllerPlayerLifecycleSlots(PlayerMouseTable table){Table=table;}
  public static ControllerPlayerLifecycleSlots Bind(PlayerMouseTable table){if(table==null)throw new ArgumentNullException("existing table");return tables.GetValue(table,delegate(PlayerMouseTable value){return new ControllerPlayerLifecycleSlots(value);});}
  public static ControllerPlayerLifecycleSlots ForBound(PlayerMouseTable table,uint at){ControllerPlayerLifecycleSlots value;if(table==null||!tables.TryGetValue(table,out value))throw new MissingContract(at,"Lifecycle slots require an explicitly bound existing table");return value;}
  // Same exact disk tables already selected by PlayerMouseTable.BindObserved.
  // Slot210 remains Unknown; no predicate body or loaded table is inferred.
  public void SelectObserved(ObservedPlayerMouseTable kind){
   if(kind!=ObservedPlayerMouseTable.Startup&&kind!=ObservedPlayerMouseTable.Idle&&kind!=ObservedPlayerMouseTable.MoveTo)throw new ArgumentOutOfRangeException("kind");
   Begin220.Write(kind==ObservedPlayerMouseTable.Startup?0x165a80u:kind==ObservedPlayerMouseTable.Idle?0x165b30u:0x166020u);End228.Write(0x015880);
  }
 }
 public sealed class ControllerStateTransitionContext {
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly Field<float> One7A8054,NullDifference338458;
  public ControllerStateTransitionContext(PlayerMousePlayerLookupContext lookup,Field<float> one,Field<float> nullDifference){if(lookup==null||one==null||nullDifference==null)throw new ArgumentNullException("shared state-entry dependencies");Lookup=lookup;One7A8054=one;NullDifference338458=nullDifference;}
  public ControllerStateTransitionContext(PlayerMoveRequestContext request):this(Require(request).Lookup,request.One7A8054,request.Path.NullDifference338458){}
  static PlayerMoveRequestContext Require(PlayerMoveRequestContext request){if(request==null)throw new ArgumentNullException("request");return request;}
 }
}
