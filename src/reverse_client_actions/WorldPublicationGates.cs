using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Additional authoritative pointer cells on the existing Engine identity.
 // Names retain offsets: their producers and network-mode lifecycle are not
 // established here. New cells are Unknown, never a presumed local session.
 public sealed class PublicationGateEngineView {
  public readonly EntityEngineState Engine;
  public readonly Field<object> Identity668=new Field<object>();
  public readonly Field<object> Identity678=new Field<object>();
  internal PublicationGateEngineView(EntityEngineState engine){Engine=engine;}
 }
 public sealed class WorldPublicationGateContext {
  sealed class IdentityComparer:IEqualityComparer<WorldEngine> {
   public bool Equals(WorldEngine x,WorldEngine y){return Object.ReferenceEquals(x,y);}
   public int GetHashCode(WorldEngine value){return RuntimeHelpers.GetHashCode(value);}
  }
  readonly Dictionary<WorldEngine,PublicationGateEngineView> engines=new Dictionary<WorldEngine,PublicationGateEngineView>(new IdentityComparer());
  public readonly EntityMovementRegistry Entities;
  // Explicit import -> borrowed global cell -> Engine, rather than a copied
  // global value. Bind to the same Field used by WorldCoordinateBoundaries.
  public readonly Field<Field<WorldEngine>> EngineImport609050=new Field<Field<WorldEngine>>();
  public WorldPublicationGateContext(EntityMovementRegistry entities){if(entities==null)throw new ArgumentNullException("entities");Entities=entities;}
  public PublicationGateEngineView BindEngineView(EntityEngineState engine){
   if(engine==null)throw new ArgumentNullException("engine");
   if(!Object.ReferenceEquals(Entities.Engine(engine.Identity,0x078110),engine))throw new InvalidOperationException("Engine already has a different authoritative view");
   PublicationGateEngineView prior;if(engines.TryGetValue(engine.Identity,out prior))return prior;
   var view=new PublicationGateEngineView(engine);engines.Add(engine.Identity,view);return view;
  }
  internal PublicationGateEngineView Engine(WorldEngine identity,uint at){
   PublicationGateEngineView view;
   if(identity==null||!engines.TryGetValue(identity,out view))throw new MissingContract(at,"Engine has no explicit publication-gate view");
   if(!Object.ReferenceEquals(Entities.Engine(identity,at),view.Engine))throw new MissingContract(at,"Shared Engine binding changed");
   view.Engine.RequireLive(at);return view;
  }
 }
 // Passive own observation only: it cannot replace reads, mutate state or
 // supply callback results. Each address records an actual captured input.
 public sealed class PublicationGateInvocation {
  public readonly Invocation Native=new Invocation();
  readonly List<uint> captures=new List<uint>();
  public uint[] CapturedAt{get{return captures.ToArray();}}
  internal void Captured(uint at){captures.Add(at);}
 }
 public static class WorldPublicationGates {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own publication-gate dependency");return value;}
  static byte Server(Field<WorldEngine> pointer,WorldPublicationGateContext context,PublicationGateInvocation call){
   var c=Need(call,0x078110);var i=c.Native;i.At=0x078110;
   var view=Need(context,i.At).Engine(Need(pointer,i.At).Read(i.At),i.At);
   var right=view.Identity678.Snapshot();c.Captured(i.At);
   i.At=0x078117;var left=view.Identity668.Snapshot();c.Captured(i.At);
   // MOV keeps opaque knowledge. CMP consumes both captured identities;
   // Unknown is not null and two Unknown cells never compare as equal.
   bool equal=Object.ReferenceEquals(left.Read(i.At),right.Read(i.At));
   i.At=0x07811e;byte al=equal?(byte)1:(byte)0;i.At=0x078121;return al;
  }
  // Engine078110, r1-engine-entity-publication-core10/11. Only AL is modeled;
  // the upper RAX bits inherited from the MOV are not a bool-sized guarantee.
  public static byte IsNetworkServer078110(WorldEngine engine,WorldPublicationGateContext context,PublicationGateInvocation call){return Server(Field<WorldEngine>.Of(engine),context,call);}
  // Game016D20, r1-game-entity-publication-gates05. No this/global reads.
  public static byte CharacterDeleteOnEnteringUnloadedLevel016D20(object borrowedThis,PublicationGateInvocation call){var c=Need(call,0x016d20);c.Native.At=0x016d20;byte al=1;c.Native.At=0x016d22;return al;}
  // Game3D5BC0, r1-game-entity-publication-gates07/08. The observed 609A40
  // import binding selects Engine078110. This is a fixed evidenced own
  // implementation, not verification of a mutable loaded import table.
  public static byte PlayerDeleteOnEnteringUnloadedLevel3D5BC0(object borrowedThis,WorldPublicationGateContext context,PublicationGateInvocation call){
   var c=Need(call,0x3d5bc4);var i=c.Native;i.At=0x3d5bc4;
   var import=Need(context,i.At).EngineImport609050.Snapshot();c.Captured(i.At);
   i.At=0x3d5bcb;var global=Need(import.Read(i.At),i.At).Snapshot();c.Captured(i.At);
   i.At=0x3d5bce;byte server=Server(global,context,c);
   i.At=0x3d5bd4;bool zero=server==0;i.At=0x3d5bd6;byte al=zero?(byte)1:(byte)0;i.At=0x3d5bdd;return al;
  }
 }
 // Optional own choice of the observed disk leaf. RET0 does not establish
 // loaded code, Billboard lifetime or any surrounding publication behavior.
 public sealed class OwnObservedDiskPublicationLeaf {
  bool selected;
  public void SelectObservedDiskImplementation(){selected=true;}
  public void BillboardPunctuation00CE70(object borrowedThis,Invocation invocation){
   if(invocation==null)throw new ArgumentNullException("invocation");invocation.At=0x00ce70;
   if(!selected)throw new MissingContract(invocation.At,"Loaded00CE70 is unobserved; explicit own disk-implementation selection is required");
  }
 }
}
