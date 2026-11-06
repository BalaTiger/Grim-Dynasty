using System;
using GrimDynasty.CombatRuntime;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameRunSpeedInvocation {
  public readonly Invocation Native,ClassAncestry=new Invocation();
  public NativeAttributeResult Attribute;
  public GameDurationDamageLookupInvocation Duration;
  public GamePlayerRunSpeedCapInvocation Cap;
  public bool ClassReturned,PetTestReturned,AttributeEntered,AttributeReturned,DurationEntered,DurationReturned,CapEntered,AbsoluteCapEntered,Returned;
  public uint? AttributeBits,DurationBits,AdjustedBits,ReturnedBits;
  public GameRunSpeedInvocation():this(new Invocation()){}
  public GameRunSpeedInvocation(Invocation native){if(native==null)throw new ArgumentNullException("invocation");Native=native;Begin();}
  internal void Begin(){Native.At=ClassAncestry.At=0;Attribute=null;Duration=new GameDurationDamageLookupInvocation();Cap=new GamePlayerRunSpeedCapInvocation();ClassReturned=PetTestReturned=AttributeEntered=AttributeReturned=DurationEntered=DurationReturned=CapEntered=AbsoluteCapEntered=Returned=false;AttributeBits=DurationBits=AdjustedBits=ReturnedBits=null;}
 }
 // Exact ordinary069040 branch. Pet production and absolute-cap database
 // dependencies stop explicitly. No caller callback supplies stats or speed.
 public static class GameRunSpeedContracts {
  static uint At(GameRunSpeedInvocation c,uint at){c.Native.At=at;return at;}
  static float Finite(float value,uint at){if(Single.IsNaN(value)||Single.IsInfinity(value))throw new MissingContract(at,"Nonfinite ordinary speed arithmetic outside the finite domain");return value;}
  static float Round(double value,uint at){return Finite(BitConverter.ToSingle(BitConverter.GetBytes((float)value),0),at);}
  static float Constant(GameRunSpeedContext context,uint? bits,uint expected,uint at){
   if(!context.SnapshotApplicable)throw new MissingContract(at,"Run-speed constant snapshot applicability unobserved");
   if(!bits.HasValue)throw new MissingContract(at,"Unobserved run-speed constant");
   if(bits.Value!=expected)throw new MissingContract(at,"Unexpected run-speed constant bits");return NativeAttributeGetter.FromBits(bits.Value);
  }
  static ClassRecord ClassInfo(object table,GameRunSpeedContext context,GameRunSpeedInvocation c){
   var callbacks=context.State.Cap.Bridge.Callbacks;
   if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Player)){At(c,0x016d10);return callbacks.PlayerClass.Read(c.Native.At);}
   if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Character)){At(c,0x019d20);return callbacks.CharacterClass.Read(c.Native.At);}
   throw new MissingContract(c.Native.At,"Captured primary slot0 has no observed class-return binding");
  }
  static float Return(float value,GameRunSpeedInvocation c){c.ReturnedBits=NativeAttributeGetter.FloatBits(value);c.Returned=true;At(c,0x0692d8);return value;}
  public static float GetRunSpeed069040(MoveCharacter caller,byte mode,GameRunSpeedContext context,GameRunSpeedInvocation invocation){
   if(invocation==null)throw new ArgumentNullException("invocation");var c=invocation;c.Begin();At(c,0x069079);
   if(context==null)throw new MissingContract(c.Native.At,"Missing explicit run-speed composition");var state=context.State;state.RequireOwner(caller,c.Native.At);
   var table=state.Cap.Identity.Owner.Entity.Virtual00.Snapshot();At(c,0x06907c);var actual=ClassInfo(table.Read(c.Native.At),context,c);c.ClassReturned=true;
   At(c,0x06907e);var pet=context.PetPlayerScalingClass.Read(c.Native.At);At(c,0x069085);bool scaled=Object.ReferenceEquals(actual,pet);
   if(!scaled){
    At(c,0x06908a);if(actual==null)throw new MissingContract(c.Native.At,"Null returned class identity");var parentBits=actual.Parent10.Snapshot();var parent=parentBits.Read(At(c,0x06908e));
    if(parent!=null){At(c,0x069097);scaled=Object.ReferenceEquals(parent,pet);if(!scaled){At(c,0x06909c);scaled=GameMovementCallbackContracts.IsDerivedFrom591480(parent,pet,c.ClassAncestry);At(c,0x0690a1);}}
   }
   c.PetTestReturned=true;
   if(scaled){At(c,0x0690a9);table=state.Cap.Identity.Owner.Entity.Virtual00.Snapshot();At(c,0x0690af);table.Read(c.Native.At);throw new MissingContract(c.Native.At,"Pet scaling slot8D0 and owner lookup are outside the ordinary Player contract");}
   At(c,0x069253);c.AttributeEntered=true;
   c.Attribute=NativeAttributeGetter.Evaluate(state.Cap.AttributesF78,0xe,context.Constants,context.SnapshotApplicable,context.NormalFpEnvironment);
   if(c.Attribute.Status!=NativeAttributeStatus.ConditionalNormalReturn){At(c,c.Attribute.ResumeInstructionRva);throw new MissingContract(c.Native.At,"Actual +F78/index0E getter: "+c.Attribute.Reason);}
   c.AttributeReturned=true;float attribute=c.Attribute.Value.Value;c.AttributeBits=NativeAttributeGetter.FloatBits(attribute);
   At(c,0x069262);state.RequireOwner(caller,c.Native.At);var durationTable=state.Duration.Virtual00.Snapshot();At(c,0x06926a);
   if(!Object.ReferenceEquals(durationTable.Read(c.Native.At),GameDurationDamageManagerTable.Observed75EF00))throw new MissingContract(c.Native.At,"Duration slot10 requires the actual observed75EF00 table");
   c.DurationEntered=true;float duration=GameDurationDamageManagerContracts.GetFixedDamage279D10(state.Duration,caller,Field<uint>.Of(0x13),c.Duration);c.DurationReturned=true;c.DurationBits=NativeAttributeGetter.FloatBits(duration);
   At(c,0x06926d);float factor=Round((double)Finite(duration,c.Native.At)*Constant(context,context.Constants==null?null:context.Constants.K2Bits,NativeAttributeConstants.RecordedK2Bits,c.Native.At),c.Native.At);
   At(c,0x069275);uint? oneBits=context.Constants==null?null:context.Constants.OneBits;
   At(c,0x06927d);factor=Round((double)Constant(context,oneBits,NativeAttributeConstants.RecordedOneBits,c.Native.At)-factor,c.Native.At);
   At(c,0x069281);float adjusted=Round((double)factor*Finite(attribute,c.Native.At),c.Native.At);c.AdjustedBits=NativeAttributeGetter.FloatBits(adjusted);
   At(c,0x069285);if(mode!=0){At(c,0x06928a);return Return(adjusted,c);}
   if(state.SpeedFlag26B3.Read(At(c,0x069292))!=0){At(c,0x06929b);c.AbsoluteCapEntered=true;At(c,0x0687e0);throw new MissingContract(c.Native.At,"Actual absolute-cap database/difficulty/LoadTable producers remain unclosed");}
   At(c,0x0692a2);table=state.Cap.Identity.Owner.Entity.Virtual00.Snapshot();At(c,0x0692a5);
   if(!Object.ReferenceEquals(table.Read(c.Native.At),GameMovementPrimaryTable.Player))throw new MissingContract(c.Native.At,"Captured primary+678 has no recovered ordinary Player cap binding");
   c.CapEntered=true;float value=GamePlayerRunSpeedCapContracts.CapRunSpeed3E5A10(state.Cap,caller,Field<float>.Of(adjusted),context.Constants,context.SnapshotApplicable,context.NormalFpEnvironment,c.Cap);
   return Return(value,c);
  }
 }
}
