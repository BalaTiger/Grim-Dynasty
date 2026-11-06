using System;
using System.Runtime.CompilerServices;
using GrimDynasty.CombatRuntime;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit OWN binding only. Fields remain unknown until observed; native
 // Player construction and the loaded game's field values are not assumed.
 public sealed class GamePlayerRunSpeedCapState {
  static readonly ConditionalWeakTable<MoveCharacter,GamePlayerRunSpeedCapState> states=new ConditionalWeakTable<MoveCharacter,GamePlayerRunSpeedCapState>();
  static readonly ConditionalWeakTable<EntityMovementState,GamePlayerRunSpeedCapState> entityOwners=new ConditionalWeakTable<EntityMovementState,GamePlayerRunSpeedCapState>();
  static readonly ConditionalWeakTable<NativeAttributeView,GamePlayerRunSpeedCapState> attributeOwners=new ConditionalWeakTable<NativeAttributeView,GamePlayerRunSpeedCapState>();
  static readonly object gate=new object();
  public readonly GameMovementIdentityBridge Bridge;
  public readonly GameMovementIdentityView Identity;
  public readonly NativeAttributeView AttributesF78;
  public readonly Field<float> Minimum5018=new Field<float>(),Maximum501C=new Field<float>();
  GamePlayerRunSpeedCapState(GameMovementIdentityBridge bridge,GameMovementIdentityView identity,NativeAttributeView attributes){Bridge=bridge;Identity=identity;AttributesF78=attributes;}
  public static GamePlayerRunSpeedCapState Bind(GameMovementIdentityBridge bridge,MoveCharacter move,NativeAttributeView attributes){
   if(bridge==null||attributes==null)throw new ArgumentNullException("existing player/attribute views");
   var identity=bridge.ForMoveCharacter(move,0);
   lock(gate){
    GamePlayerRunSpeedCapState old;
    if(states.TryGetValue(move,out old)){if(!Object.ReferenceEquals(old.Bridge,bridge)||!Object.ReferenceEquals(old.Identity,identity)||!Object.ReferenceEquals(old.AttributesF78,attributes))throw new InvalidOperationException("Player cap identity or attributes cannot be rebound");return old;}
    if(entityOwners.TryGetValue(identity.Owner.Entity,out old))throw new InvalidOperationException("One Entity cannot acquire another Player cap state");
    if(attributeOwners.TryGetValue(attributes,out old))throw new InvalidOperationException("One attribute storage cannot acquire another player owner");
    var state=new GamePlayerRunSpeedCapState(bridge,identity,attributes);states.Add(move,state);entityOwners.Add(identity.Owner.Entity,state);attributeOwners.Add(attributes,state);return state;
   }
  }
  public void RequireOwner(MoveCharacter caller,uint at){
   if(!Object.ReferenceEquals(Bridge.ForMoveCharacter(caller,at),Identity))throw new MissingContract(at,"Player cap called with another character identity");
  }
 }
 public sealed class GamePlayerRunSpeedCapInvocation {
  public readonly Invocation Native=new Invocation();
  public NativeAttributeResult Attribute;
  public bool MinimumCaptured,InputCaptured,MaximumCaptured,AttributeEntered,Returned;
  public uint? LowerBits,UpperBits,AfterMaximumBits,ReturnedBits;
  // Native receiver+12B8,index0E uses the SINGLE +F78 sparse view at own
  // indexDE: +340 bias /4 +0E. DE is not the original native argument.
  public const int NativeAttributeIndex=0xE,OwnBiasedAttributeIndex=0xDE;
  internal void Begin(){Native.At=0;Attribute=null;MinimumCaptured=InputCaptured=MaximumCaptured=AttributeEntered=Returned=false;LowerBits=UpperBits=AfterMaximumBits=ReturnedBits=null;}
 }
 public static class GamePlayerRunSpeedCapContracts {
  static uint At(GamePlayerRunSpeedCapInvocation c,uint at){c.Native.At=at;return at;}
  static float Finite(float value,uint at){if(Single.IsNaN(value)||Single.IsInfinity(value))throw new MissingContract(at,"Nonfinite cap arithmetic/comparison outside the recovered finite domain");return value;}
  static float Round(double value,uint at){return Finite(BitConverter.ToSingle(BitConverter.GetBytes((float)value),0),at);}
  static float K2(NativeAttributeConstants constants,bool snapshotApplicable,uint at){
   if(!snapshotApplicable)throw new MissingContract(at,"Constant snapshot applicability unobserved");
   if(constants==null||!constants.K2Bits.HasValue)throw new MissingContract(at,"Unknown recorded7A7FC8 constant");
   if(constants.K2Bits.Value!=NativeAttributeConstants.RecordedK2Bits)throw new MissingContract(at,"Unexpected recorded7A7FC8 constant bits");
   return NativeAttributeGetter.FromBits(constants.K2Bits.Value);
  }
  static float Bonus(GamePlayerRunSpeedCapState state,uint at){
   if(!state.AttributesF78.IsValid)throw new MissingContract(at,"Invalidated same-owner attribute storage");
   // Full Character+10A8 is +F78+130: read that SAME sparse cell.
   uint? bits=state.AttributesF78.ReadObservedBits(0x130);
   if(!bits.HasValue)throw new MissingContract(at,"Unobserved same-owner Character+10A8 field");
   return Finite(NativeAttributeGetter.FromBits(bits.Value),at);
  }
  static float Return(Field<float> input,GamePlayerRunSpeedCapInvocation c,uint load,uint ret){
   // Early MOVAPS returns opaque input bits, including signed zero/nonfinite
   // payloads. No comparison or arithmetic consumes this input on that branch.
   float value=input.Read(At(c,load));c.ReturnedBits=NativeAttributeGetter.FloatBits(value);c.Returned=true;At(c,ret);return value;
  }
  // Direct body3E5A10 only, not virtual dispatch or a recovered spawn.
  // Conditions: serial access, explicit lifetime, masked RN-even/DAZ/FTZ off.
  // Nonfinite comparisons/arithmetic and overflow stop explicitly.
  public static float CapRunSpeed3E5A10(GamePlayerRunSpeedCapState state,MoveCharacter caller,Field<float> xmm1,NativeAttributeConstants constants,bool snapshotApplicable,bool normalFpEnvironment,GamePlayerRunSpeedCapInvocation invocation){
   if(invocation==null)throw new ArgumentNullException("invocation");var c=invocation;c.Begin();
   At(c,0x3e5a1e);if(state==null)throw new MissingContract(c.Native.At,"Missing explicit Player cap state");state.RequireOwner(caller,c.Native.At);var minimum=state.Minimum5018.Snapshot();c.MinimumCaptured=true;
   float lower=minimum.Read(At(c,0x3e5a29));if(!normalFpEnvironment)throw new MissingContract(c.Native.At,"Floating-point environment unobserved");Finite(lower,c.Native.At);bool early=lower<=0;
   At(c,0x3e5a32);if(xmm1==null)throw new MissingContract(c.Native.At,"Missing input XMM1 observation");var input=xmm1.Snapshot();c.InputCaptured=true;
   if(early)return Return(input,c,0x3e5aa8,0x3e5ab7);
   At(c,0x3e5a3d);var maximum=state.Maximum501C.Snapshot();c.MaximumCaptured=true;
   float upper=maximum.Read(At(c,0x3e5a45));Finite(upper,c.Native.At);
   if(upper<=0)return Return(input,c,0x3e5a9d,0x3e5a9c);
   At(c,0x3e5a56);c.AttributeEntered=true;
   c.Attribute=NativeAttributeGetter.Evaluate(state.AttributesF78,GamePlayerRunSpeedCapInvocation.OwnBiasedAttributeIndex,constants,snapshotApplicable,normalFpEnvironment);
   if(c.Attribute.Status!=NativeAttributeStatus.ConditionalNormalReturn){At(c,c.Attribute.ResumeInstructionRva);throw new MissingContract(c.Native.At,"Native +12B8/index0E getter via OWN +F78/indexDE address bias: "+c.Attribute.Reason);}
   float attribute=c.Attribute.Value.Value;
   At(c,0x3e5a5b);lower=Round((double)lower*K2(constants,snapshotApplicable,c.Native.At),c.Native.At);
   At(c,0x3e5a63);upper=Round((double)upper+Bonus(state,c.Native.At),c.Native.At);
   At(c,0x3e5a6b);lower=Round((double)lower*attribute,c.Native.At);c.LowerBits=NativeAttributeGetter.FloatBits(lower);
   At(c,0x3e5a6f);upper=Round((double)upper*K2(constants,snapshotApplicable,c.Native.At),c.Native.At);
   float incoming=Finite(input.Read(At(c,0x3e5a77)),c.Native.At);lower=lower>incoming?lower:incoming;c.AfterMaximumBits=NativeAttributeGetter.FloatBits(lower);
   At(c,0x3e5a7c);upper=Round((double)upper*attribute,c.Native.At);c.UpperBits=NativeAttributeGetter.FloatBits(upper);
   At(c,0x3e5a80);float result=lower<upper?lower:upper; // MINSS ties choose source.
   At(c,0x3e5a84);c.ReturnedBits=NativeAttributeGetter.FloatBits(result);c.Returned=true;At(c,0x3e5a9c);return result;
  }
 }
}
