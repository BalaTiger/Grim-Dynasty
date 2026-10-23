using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit OWN nodes and links only; this is not a recovered allocation,
 // insertion, balancing, construction or population of the native tree.
 // Parent08 is read only on the header; no color or count field is invented.
 public sealed class PlayerHotSlotTreeNode:NavigationView {
  public readonly Field<PlayerHotSlotTreeNode> Left00=new Field<PlayerHotSlotTreeNode>(),Parent08=new Field<PlayerHotSlotTreeNode>(),Right10=new Field<PlayerHotSlotTreeNode>();
  public readonly Field<byte> Nil19=new Field<byte>();
  public readonly Field<uint> Key20=new Field<uint>();
  public readonly Field<MouseSkillSet> Value28=new Field<MouseSkillSet>();
 }
 // Native HotSlotCtrl is role+4A48 (1624FA/16251E LEA). One explicitly bound
 // state per existing MoveCharacter. The three earlier mouse cells are exact
 // aliases; the new tree fields remain Unknown until own observation/setup.
 public sealed class PlayerHotSlotState {
  static readonly ConditionalWeakTable<MoveCharacter,PlayerHotSlotState> roles=new ConditionalWeakTable<MoveCharacter,PlayerHotSlotState>();
  static readonly object gate=new object();
  public readonly MoveCharacter Role;
  public readonly GameMovementCharacterState Owner;
  public readonly MouseRoleState Mouse;
  public readonly Field<MoveCharacter> Owner00;
  public readonly Field<byte> Byte19,Byte28;
  public readonly Field<PlayerHotSlotTreeNode> Header88=new Field<PlayerHotSlotTreeNode>();
  public readonly Field<uint> Key98=new Field<uint>(),Key9C=new Field<uint>();
  PlayerHotSlotState(GameMovementCharacterState owner,MouseRoleState mouse){Role=mouse.Role;Owner=owner;Mouse=mouse;Owner00=mouse.HotSlotOwner4A48;Byte19=mouse.Byte4A61;Byte28=mouse.Byte4A70;}
  public static PlayerHotSlotState Bind(PlayerMousePlayerLookupContext context,MouseRoleState mouse){
   if(context==null||mouse==null)throw new ArgumentNullException("HotSlot binding");var owner=context.Bridge.ForMoveCharacter(mouse.Role,0).Owner;
   lock(gate){PlayerHotSlotState prior;bool exists=roles.TryGetValue(mouse.Role,out prior);if(exists&&(!Object.ReferenceEquals(prior.Owner,owner)||!Object.ReferenceEquals(prior.Mouse,mouse)))throw new InvalidOperationException("HotSlot identity already has another shared owner/mouse view");context.BindRole(mouse);if(exists)return prior;var value=new PlayerHotSlotState(owner,mouse);roles.Add(mouse.Role,value);return value;}
  }
  public static PlayerHotSlotState ForBoundRole(PlayerMousePlayerLookupContext context,MoveCharacter role,uint at){
   if(context==null)throw new MissingContract(at,"Missing existing HotSlot lookup context");var owner=context.Bridge.ForMoveCharacter(role,at).Owner;var mouse=context.Mouse.Role(role,at);PlayerHotSlotState state;
   if(role==null||!roles.TryGetValue(role,out state)||!Object.ReferenceEquals(state.Owner,owner)||!Object.ReferenceEquals(state.Mouse,mouse))throw new MissingContract(at,"HotSlot requires the explicitly bound original owner and mouse view");state.RequireLive(at);return state;
  }
  public void RequireLive(uint at){PlayerHotSlotState actual;if(!roles.TryGetValue(Role,out actual)||!Object.ReferenceEquals(actual,this))throw new MissingContract(at,"HotSlot is not the unique bound state");Owner.Entity.RequireLive(at);}
 }
 // Game3EAE40 lower_bound and equality check only. Key9C is captured once at
 // entry; zero selects the separately captured Key98. An empty tree never
 // consumes Key98. Each public invocation re-reads the mutable original cells.
 // Only explicit live OWN nodes are followed. Cycles stop at the repeated key
 // comparison; concurrent mutation and unrepresented pointer layouts are out.
 public static class PlayerHotSlotDisplayedContracts {
  static PlayerHotSlotTreeNode Node(Field<PlayerHotSlotTreeNode> bits,uint at,Invocation i){i.At=at;var node=bits.Read(at);if(node==null)throw new MissingContract(at,"Null OWN tree node at actual dereference");node.RequireLive(at);return node;}
  static PlayerHotSlotTreeNode Dereference(PlayerHotSlotTreeNode node,uint at,Invocation i){i.At=at;if(node==null)throw new MissingContract(at,"Null OWN tree node at actual dereference");node.RequireLive(at);return node;}
  static Field<PlayerHotSlotTreeNode> Child(PlayerHotSlotTreeNode node,bool left,uint at,Invocation i){Dereference(node,at,i);return (left?node.Left00:node.Right10).Snapshot();}
  static MouseSkillSet Search(Field<PlayerHotSlotTreeNode> headBits,Field<uint> keyBits,bool fallback,Invocation i){
   uint parentLoad=fallback?0x3eae5du:0x3eae9eu,firstNil=fallback?0x3eae61u:0x3eaea2u;
   uint compare=fallback?0x3eae68u:0x3eaea8u,right=fallback?0x3eae6eu:0x3eaeaeu,left=fallback?0x3eae77u:0x3eaeb7u,nextNil=fallback?0x3eae7au:0x3eaebau;
   uint finalCompare=fallback?0x3eae86u:0x3eaec5u;
   var head=Node(headBits,parentLoad,i);var currentBits=head.Parent08.Snapshot();var candidate=head;var visited=new HashSet<PlayerHotSlotTreeNode>();uint nilAt=firstNil;
   while(true){
    var current=Node(currentBits,nilAt,i);if(current.Nil19.Read(i.At)!=0)break;
    i.At=compare;if(!visited.Add(current))throw new MissingContract(i.At,"Repeated OWN lower_bound node; cyclic tree is outside the bounded acyclic domain");current.RequireLive(i.At);
    uint nodeKey=current.Key20.Read(i.At),key=keyBits.Read(i.At);
    if(nodeKey<key)currentBits=Child(current,false,right,i);
    else{candidate=current;currentBits=Child(current,true,left,i);}
    nilAt=nextNil;
   }
   // Candidate==head short-circuits the final key and value reads.
   if(!Object.ReferenceEquals(candidate,head)){
    Dereference(candidate,finalCompare,i);if(keyBits.Read(i.At)<candidate.Key20.Read(i.At))candidate=head;
   }
   i.At=0x3eaed1;if(Object.ReferenceEquals(candidate,head)){i.At=0x3eaeda;return null;}
   Dereference(candidate,0x3eaed3,i);var result=candidate.Value28.Read(i.At);i.At=0x3eaed7;return result;
  }
  public static MouseSkillSet GetDisplayedSkillSet3EAE40(PlayerHotSlotState state,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x3eae40,"Missing HotSlot invocation");var i=invocation;i.At=0x3eae40;if(state==null)throw new MissingContract(i.At,"Null bound HotSlot state");state.RequireLive(i.At);var selected=state.Key9C.Snapshot();
   i.At=0x3eae47;uint key=selected.Read(i.At);
   if(key==0){i.At=0x3eae4c;state.RequireLive(i.At);var head=state.Header88.Snapshot();i.At=0x3eae53;var fallback=state.Key98.Snapshot();return Search(head,fallback,true,i);}
   i.At=0x3eae94;state.RequireLive(i.At);return Search(state.Header88.Snapshot(),selected,false,i);
  }
 }
 // Actual override of the existing mouse producer. Resolve128530 remains the
 // already passed implementation. All other virtual boundaries remain as-is.
 // The call counter is diagnostic only and never determines a returned value.
 public class PlayerMouseDisplayedSkillSetBoundaries:PlayerMousePlayerLookupBoundaries {
  public Invocation LastDisplayedInvocation{get;private set;}
  public int DisplayedCalls{get;private set;}
  public PlayerMouseDisplayedSkillSetBoundaries(PlayerMousePlayerLookupContext context):base(context){}
  public override MouseSkillSet DisplayedSkillSet3EAE40(MoveCharacter role){LastDisplayedInvocation=new Invocation();LastDisplayedInvocation.At=0x3eae40;DisplayedCalls++;return PlayerHotSlotDisplayedContracts.GetDisplayedSkillSet3EAE40(PlayerHotSlotState.ForBoundRole(LookupContext,role,0x3eae40),LastDisplayedInvocation);}
 }
}
