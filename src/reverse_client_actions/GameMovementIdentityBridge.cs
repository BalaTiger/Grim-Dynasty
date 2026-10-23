using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit OWN association between two existing views of the full Character.
 // Neither view is constructed here and no field value is copied or defaulted.
 // Use one bridge for this composition. The maps are single-threaded setup/
 // access state, not a recovered native ObjectManager or lifetime mechanism.
 public sealed class GameMovementIdentityBridge {
  sealed class ReferenceComparer<T>:IEqualityComparer<T> where T:class {
   public bool Equals(T x,T y){return Object.ReferenceEquals(x,y);}
   public int GetHashCode(T value){return RuntimeHelpers.GetHashCode(value);}
  }
  public readonly GameMovementCallbackContext Callbacks;
  readonly Dictionary<GameMovementCharacterState,GameMovementIdentityView> owners=new Dictionary<GameMovementCharacterState,GameMovementIdentityView>(new ReferenceComparer<GameMovementCharacterState>());
  readonly Dictionary<MoveCharacter,GameMovementIdentityView> moves=new Dictionary<MoveCharacter,GameMovementIdentityView>(new ReferenceComparer<MoveCharacter>());
  public GameMovementIdentityBridge(GameMovementCallbackContext callbacks){if(callbacks==null)throw new ArgumentNullException("callbacks");Callbacks=callbacks;}
  GameMovementCharacterState Canonical(GameMovementInterfaceView callback,uint at){
   var owner=Callbacks.Owner(callback,at);owner.Entity.RequireLive(at);return owner;
  }
  // Setup operation only. Validate the canonical owner and both directions
  // before publishing either map. Unknown MovementDC0/AssociatedId16C0 and
  // ActionState24B4 remain untouched; no manager or MoveCharacter is inferred.
  public GameMovementIdentityView Bind(GameMovementCharacterState owner,MoveCharacter move){
   if(owner==null||move==null)throw new ArgumentNullException("existing character views");
   if(!Object.ReferenceEquals(Canonical(owner.Interface38,0),owner))throw new InvalidOperationException("Game owner is not the callback context's canonical view");
   GameMovementIdentityView byOwner,byMove;
   bool ownerBound=owners.TryGetValue(owner,out byOwner),moveBound=moves.TryGetValue(move,out byMove);
   if(ownerBound||moveBound){
    if(!ownerBound||!moveBound||!Object.ReferenceEquals(byOwner,byMove)||!Object.ReferenceEquals(byOwner.Owner,owner)||!Object.ReferenceEquals(byOwner.Move,move))
     throw new InvalidOperationException("Character identity bridge cannot rebind either existing view");
    RequireBound(byOwner,0);return byOwner;
   }
   var view=new GameMovementIdentityView(this,owner,move);owners.Add(owner,view);moves.Add(move,view);return view;
  }
  public GameMovementIdentityView ForCallback(GameMovementInterfaceView callback,uint at){
   var owner=Canonical(callback,at);GameMovementIdentityView view;
   if(!owners.TryGetValue(owner,out view))throw new MissingContract(at,"Canonical Game owner has no explicit MoveCharacter binding");
   RequireBound(view,at);return view;
  }
  public GameMovementIdentityView ForMoveCharacter(MoveCharacter move,uint at){
   GameMovementIdentityView view;
   if(move==null||!moves.TryGetValue(move,out view))throw new MissingContract(at,"MoveCharacter has no explicit Game owner binding");
   RequireBound(view,at);return view;
  }
  public void RequireBound(GameMovementIdentityView view,uint at){
   GameMovementIdentityView byOwner,byMove;
   if(view==null||!Object.ReferenceEquals(view.Bridge,this)||!owners.TryGetValue(view.Owner,out byOwner)||!moves.TryGetValue(view.Move,out byMove)||!Object.ReferenceEquals(byOwner,view)||!Object.ReferenceEquals(byMove,view))
    throw new MissingContract(at,"Character view does not belong to this unique bidirectional bridge");
   if(!Object.ReferenceEquals(Canonical(view.Owner.Interface38,at),view.Owner))throw new MissingContract(at,"Game/Move bridge canonical owner changed");
  }
 }
 public sealed class GameMovementIdentityView {
  internal readonly GameMovementIdentityBridge Bridge;
  public readonly GameMovementCharacterState Owner;
  public readonly MoveCharacter Move;
  internal GameMovementIdentityView(GameMovementIdentityBridge bridge,GameMovementCharacterState owner,MoveCharacter move){Bridge=bridge;Owner=owner;Move=move;}
  // Return the original Field itself, not a snapshot. The eventual native
  // consumer controls its MOV capture and first value use. Evidence067170:
  // interface38+D88 == full+DC0, interface38+1688 == full+16C0.
  // These accessors implement no Stop, lookup, controller or event callback.
  public Field<MovementManager> BorrowMovementDC0(uint at){Bridge.RequireBound(this,at);return Move.MovementDC0;}
  public Field<uint> BorrowAssociatedId16C0(uint at){Bridge.RequireBound(this,at);return Move.AssociatedId16C0;}
 }
}
