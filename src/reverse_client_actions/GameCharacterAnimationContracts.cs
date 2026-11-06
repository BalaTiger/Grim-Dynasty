using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Unique fields for the existing full Character/Player owner. Binding makes
 // no constructor/publication claim and initializes none of the three pointers.
 public sealed class GameCharacterAnimationFields {
  static readonly ConditionalWeakTable<GameMovementCharacterState,GameCharacterAnimationFields> owners=new ConditionalWeakTable<GameMovementCharacterState,GameCharacterAnimationFields>();
  public readonly GameMovementCharacterState Owner;
  public readonly Field<AnimationSet> Set2578=new Field<AnimationSet>(),Set2600=new Field<AnimationSet>(),Set2608=new Field<AnimationSet>();
  GameCharacterAnimationFields(GameMovementCharacterState owner){Owner=owner;}
  public static GameCharacterAnimationFields ForOwner(GameMovementCharacterState owner){if(owner==null)throw new ArgumentNullException("existing animation owner");return owners.GetValue(owner,delegate(GameMovementCharacterState value){return new GameCharacterAnimationFields(value);});}
 }
 public sealed class GameGetAnimationSetInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<Field<AnimationSet>> OverrideCaptures=new List<Field<AnimationSet>>();
  public readonly List<uint> OverrideOffsets=new List<uint>();
  public Field<AnimationSet> Result{get;private set;}
  public Field<AnimationSet> FallbackCapture{get;private set;}
  public uint SelectedOffset;public bool FallbackCaptured,Returned;
  public GameGetAnimationSetInvocation(){Begin();}
  internal void Begin(){Native.At=0;OverrideCaptures.Clear();OverrideOffsets.Clear();Result=new Field<AnimationSet>();FallbackCapture=new Field<AnimationSet>();SelectedOffset=0;FallbackCaptured=Returned=false;}
 }
 // Actual Game05AE40: r1-game-waypoint-animation-run1/04..05, subject SHA
 // cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2.
 // Nonvirtual getter; production must supply the original Bridge owner.
 // Stable single-thread typed pointer domain. The MOV fallback can return an
 // Unknown pointer normally: no default null/set or eager Kind08 read is added.
 public static class GameCharacterAnimationContracts {
  public static Field<AnimationSet> GetAnimationSet05AE40(GameMovementCharacterState owner,GameGetAnimationSetInvocation invocation){
   if(invocation==null)throw new MissingContract(0x05ae40,"Missing animation getter invocation");var c=invocation;c.Begin();var i=c.Native;i.At=0x05ae40;
   try{
    i.At=0x05ae42;
    for(int index=0;index<2;index++){
     i.At=0x05ae50;if(owner==null)throw new MissingContract(i.At,"Null own Character pointer");owner.Entity.RequireLive(i.At);var fields=GameCharacterAnimationFields.ForOwner(owner);
     var captured=(index==0?fields.Set2600:fields.Set2608).Snapshot();c.OverrideCaptures.Add(captured);c.OverrideOffsets.Add(index==0?0x2600u:0x2608u);
     i.At=0x05ae53;if(captured.Read(i.At)!=null){i.At=0x05ae6b;c.Result.CopyFrom(captured);c.SelectedOffset=index==0?0x2600u:0x2608u;i.At=0x05ae6e;c.Returned=true;return c.Result;}
     i.At=0x05ae58;i.At=0x05ae5a;i.At=0x05ae5e;
    }
    i.At=0x05ae63;owner.Entity.RequireLive(i.At);c.FallbackCapture.CopyFrom(GameCharacterAnimationFields.ForOwner(owner).Set2578.Snapshot());c.FallbackCaptured=true;c.Result.CopyFrom(c.FallbackCapture);c.SelectedOffset=0x2578;
    i.At=0x05ae6a;c.Returned=true;return c.Result;
   }catch(MissingContract error){i.At=error.Address;throw;}
  }
 }
}
