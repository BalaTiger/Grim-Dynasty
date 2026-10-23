using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // No3E0 cell existed on the frozen WorldEngine view. This explicit unique
 // sidecar owns that cell and borrows the existing EntityEngineState lifetime.
 // Binding leaves3E0 Unknown; it is not native Options initialization.
 public sealed class EngineOptionsState {
  static readonly ConditionalWeakTable<WorldEngine,EngineOptionsState> states=new ConditionalWeakTable<WorldEngine,EngineOptionsState>();
  static readonly object gate=new object();
  public readonly EntityEngineState Owner;
  public readonly Field<object> Options3E0=new Field<object>();
  EngineOptionsState(EntityEngineState owner){Owner=owner;}
  public static EngineOptionsState Bind(EntityEngineState owner){
   if(owner==null)throw new ArgumentNullException("owner");owner.RequireLive(0);
   lock(gate){EngineOptionsState old;if(states.TryGetValue(owner.Identity,out old)){if(!Object.ReferenceEquals(old.Owner,owner))throw new InvalidOperationException("Engine already has a different lifetime/3E0 view");return old;}var value=new EngineOptionsState(owner);states.Add(owner.Identity,value);return value;}
  }
  public static EngineOptionsState ForBoundEngine(WorldEngine engine,uint at){EngineOptionsState value;if(engine==null||!states.TryGetValue(engine,out value))throw new MissingContract(at,"Engine3E0 requires an explicit existing-engine binding");value.Owner.RequireLive(at);return value;}
 }
 // An address into existing DWORD cells, not a new array of copied integers.
 // A signed index may refer to declared cells before BaseIndex. Outside that
 // own storage is Unsupported, never a native bounds check or clamped index.
 public sealed class OptionsIntPointer:NavigationView {
  public readonly NavigationRefBuffer Storage;
  public readonly int BaseIndex;
  public OptionsIntPointer(NavigationRefBuffer storage,int baseIndex){if(storage==null)throw new ArgumentNullException("storage");if(baseIndex<0||baseIndex>storage.Cells.Length)throw new ArgumentOutOfRangeException("baseIndex");Storage=storage;BaseIndex=baseIndex;}
  public Field<uint> Cell(long signedIndex,uint at){RequireLive(at);return Storage.At((long)BaseIndex+signedIndex,at);}
 }
 public sealed class EngineOptionsObjectState {
  static readonly ConditionalWeakTable<object,EngineOptionsObjectState> states=new ConditionalWeakTable<object,EngineOptionsObjectState>();
  static readonly object gate=new object();
  public readonly object Identity;
  public readonly NavigationView Lifetime;
  public readonly Field<OptionsIntPointer> Integers48=new Field<OptionsIntPointer>();
  EngineOptionsObjectState(object identity,NavigationView lifetime){Identity=identity;Lifetime=lifetime;}
  public static EngineOptionsObjectState Bind(object identity,NavigationView lifetime){
   if(identity==null||lifetime==null)throw new ArgumentNullException("identity/lifetime");var address=identity as NavigationView;if(address!=null)address.RequireLive(0);lifetime.RequireLive(0);
   lock(gate){EngineOptionsObjectState old;if(states.TryGetValue(identity,out old)){if(!Object.ReferenceEquals(old.Lifetime,lifetime))throw new InvalidOperationException("Options identity already has another lifetime/48 view");return old;}var value=new EngineOptionsObjectState(identity,lifetime);states.Add(identity,value);return value;}
  }
  public static EngineOptionsObjectState ForBoundIdentity(object identity,uint at){EngineOptionsObjectState value;if(identity==null||!states.TryGetValue(identity,out value))throw new MissingContract(at,"Options48 requires an explicit existing-identity binding");value.RequireLive(at);return value;}
  public void RequireLive(uint at){var address=Identity as NavigationView;if(address!=null)address.RequireLive(at);Lifetime.RequireLive(at);}
 }
 // Engine Rea r1-engine-mouse-getters-run1:00BBE0,073660,182D60.
 // The OWN object/pointer domain preserves unknown cells and borrowed identity;
 // it does not restore cold Options allocation, constructor, files or defaults.
 public static class EngineMouseGetterContracts {
  static Invocation Begin(Invocation invocation,uint at){if(invocation==null)throw new MissingContract(at,"Missing getter invocation");invocation.At=at;return invocation;}
  public static EntityCoordinates64 GetCoordsUnparented00BBE0(EntityMovementState entity,Invocation invocation){
   var i=Begin(invocation,0x00bbe0);if(entity==null)throw new MissingContract(i.At,"Null own Entity address");
   // LEA does not read object memory or validate the pointed coordinate
   // lifetime. A later dereference of this same borrowed view must do that.
   var result=entity.CoordsD8;i.At=0x00bbe7;return result;
  }
  public static object GetOptions073660(WorldEngine engine,Invocation invocation){
   var i=Begin(invocation,0x073677);var state=EngineOptionsState.ForBoundEngine(engine,i.At);
   if(state.Options3E0.Read(i.At)==null){i.At=0x073686;throw new MissingContract(i.At,"Cold Options allocation2DC398/constructor1808D0 and initialization are unclosed;3E0 remains null");}
   i.At=0x0736e5;state.Owner.RequireLive(i.At);var result=state.Options3E0.Read(i.At);i.At=0x0736f6;return result;
  }
  public static int GetInt182D60(object options,uint index,Invocation invocation){
   var i=Begin(invocation,0x182d60);var state=EngineOptionsObjectState.ForBoundIdentity(options,i.At);var pointer=state.Integers48.Snapshot();
   i.At=0x182d64;long signedIndex=unchecked((int)index);
   i.At=0x182d67;var address=pointer.Read(i.At);if(address==null)throw new MissingContract(i.At,"Null own Options integer pointer");
   int result=unchecked((int)address.Cell(signedIndex,i.At).Read(i.At));i.At=0x182d6a;return result;
  }
 }
 // Extends the already verified lookup -> IsAlive -> HotSlot selector chain;
 // no fixture delegate supplies either Options identity or integer values.
 public class PlayerMouseOptionsBoundaries:PlayerMouseAliveBoundaries {
  public Invocation LastOptionsInvocation{get;private set;}
  public Invocation LastOptionIntInvocation{get;private set;}
  public PlayerMouseOptionsBoundaries(PlayerAliveContext context):base(context){}
  public override object GetOptions609AD8(WorldEngine engine){LastOptionsInvocation=new Invocation();return EngineMouseGetterContracts.GetOptions073660(engine,LastOptionsInvocation);}
  public override int OptionInt60A148(object options,uint index){LastOptionIntInvocation=new Invocation();return EngineMouseGetterContracts.GetInt182D60(options,index,LastOptionIntInvocation);}
 }
}
