using System;
using GrimDynasty.TokenActions;
namespace GrimDynasty.Rewards {
 public static class ExperienceScaling {
  static float Single(float f){return BitConverter.ToSingle(BitConverter.GetBytes(f),0);}
  public static MutationResult Scale(uint amount,float? modifier,SystemCrtMath math,out uint effective){
   effective=0;if(!modifier.HasValue)return MutationResult.Unsupported("UnknownExperienceGainModifier");float percent=modifier.Value;
   if(percent==0){effective=amount;return MutationResult.Known(false);}
   if(math==null)return MutationResult.Unsupported("NonzeroExperienceModifierRequiresVerifiedFloatProfile");uint control;var ready=math.Check(out control);if(ready.Status!="Known")return ready;
   // UCOMISS followed only by JZ skips the modifier for unordered (NaN) too.
   if(float.IsNaN(percent)){effective=amount;return MutationResult.Known(false);}
   float fraction=Single(percent*0.01f),scaled=Single(fraction*(float)amount),biased=Single(scaled+0.5f);
   if(float.IsNaN(biased)||float.IsInfinity(biased)||(double)biased>=9223372036854775808d||(double)biased< -9223372036854775808d)return MutationResult.Unsupported("ExperienceScaleCVTTOutsideFiniteInt64Domain");
   long delta=unchecked((long)biased);effective=unchecked(amount+(uint)delta);return MutationResult.Known(false);
  }
 }
}
