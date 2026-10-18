using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
namespace GrimDynasty.TokenActions {
 public static class RewardClassifier {
  // Native switch in Game.dll 0x18046e490. This classifies raw action type
  // integers; no action execution, localization, version or guessed meaning.
  public static EvaluationResult HasRewardActions(IList<int?> types){
   if(types==null)return EvaluationResult.Unsupported("UnknownActionVector");
   for(int i=0;i<types.Count;i++){
    if(!types[i].HasValue)return EvaluationResult.Unsupported("UnknownActionType:"+i);
    switch(types[i].Value){case 4:case 5:case 6:case 7:case 8:case 15:case 18:case 20:case 25:case 26:return EvaluationResult.Known(true);}
   }return EvaluationResult.Known(false);
  }
 }
}
