using System;
using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskRuntime {
 public sealed class TokenStatsSnapshot {public uint? DefenseBuilt,PowerupActive;}
 public static class TokenStats {
  public static MutationResult Granted(TokenStatsSnapshot s,string token){
   if(token==null)return MutationResult.Unsupported("MissingToken");
   if(token=="SURVIVALMODE_DEFENSEBUILT"){
    if(s==null||!s.DefenseBuilt.HasValue)return MutationResult.Unsupported("UnknownDefenseCounter");s.DefenseBuilt=unchecked(s.DefenseBuilt.Value+1);return MutationResult.Known(true);
   }
   if(token=="SURVIVALMODE_POWERUPACTIVE"){
    if(s==null||!s.PowerupActive.HasValue)return MutationResult.Unsupported("UnknownPowerupCounter");s.PowerupActive=unchecked(s.PowerupActive.Value+1);return MutationResult.Known(true);
   }
   return MutationResult.Known(false);
  }
 }
}
