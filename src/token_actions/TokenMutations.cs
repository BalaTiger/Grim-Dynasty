using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
namespace GrimDynasty.TokenActions {
 public sealed class MutationResult {
  public string Status,Reason;public bool? Changed;
  public static MutationResult Known(bool changed){return new MutationResult{Status="Known",Changed=changed};}
  public static MutationResult Unsupported(string reason){return new MutationResult{Status="Unsupported",Reason=reason};}
 }
 public static class Mutations {
  static bool Valid(string s){if(s==null)return false;for(int i=0;i<s.Length;i++){if(char.IsHighSurrogate(s[i])){if(++i>=s.Length||!char.IsLowSurrogate(s[i]))return false;}else if(char.IsLowSurrogate(s[i]))return false;}return true;}
  static bool Valid(List<string> tokens){if(tokens==null)return false;foreach(var s in tokens)if(!Valid(s))return false;return true;}
  static MutationResult U(string why){return MutationResult.Unsupported(why);}
  // These are repository methods accepting full TriggerToken names. Unlike
  // condition wrappers, these methods do not truncate at embedded NUL.
  public static MutationResult SetPlayerToken(World world,uint playerId,string token){return Change(world,playerId,token,true);}
  public static MutationResult RemovePlayerToken(World world,uint playerId,string token){return Change(world,playerId,token,false);}
  static MutationResult Change(World world,uint id,string token,bool add){
   if(world==null||!world.RepositoryTokensComplete)return U("IncompleteRepositorySnapshot");
   if(!Valid(token))return U("TokenOutsideDecodedUtf8Domain");List<string> tokens;
   if(!world.RepositoryTokens.TryGetValue(id,out tokens))return MutationResult.Known(false);
   if(!Valid(tokens))return U("UnknownOrInvalidTokenVector");
   int at=tokens.FindIndex(s=>String.Equals(s,token,StringComparison.Ordinal));
   if(add){if(at>=0)return MutationResult.Known(false);tokens.Add(token);return MutationResult.Known(true);}
   if(at<0)return MutationResult.Known(false);tokens.RemoveAt(at);return MutationResult.Known(true);
  }
  public static MutationResult SetPlayerTokens(World world,uint playerId,List<string> tokens,uint? clientPlayerId){
   if(world==null||!world.RepositoryTokensComplete)return U("IncompleteRepositorySnapshot");
   if(!world.ServerServiceSlot18NonNull.HasValue)return U("UnknownServerServiceSlot18");
   if(world.ServerServiceSlot18NonNull.Value&&!clientPlayerId.HasValue)return U("UnknownClientPlayerId");
   if(!Valid(tokens))return U("TokenVectorOutsideDecodedUtf8Domain");
   // Copy before mutation preserves native vector assignment's self-alias case.
   var copy=new List<string>(tokens);List<string> previous;
   bool exists=world.RepositoryTokens.TryGetValue(playerId,out previous);
   if(exists&&previous==null)return U("UnknownExistingTokenVector");
   bool changed=!exists||!Same(previous,copy);
   if(world.ServerServiceSlot18NonNull.Value&&clientPlayerId.Value==playerId){changed|=!world.ServerPlayerId.HasValue||world.ServerPlayerId.Value!=playerId;world.ServerPlayerId=playerId;}
   if(exists){previous.Clear();previous.AddRange(copy);}else world.RepositoryTokens.Add(playerId,copy);
   return MutationResult.Known(changed);
  }
  static bool Same(List<string> a,List<string> b){if(a==null||a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++)if(!String.Equals(a[i],b[i],StringComparison.Ordinal))return false;return true;}
  public static MutationResult ExecuteFullTokenAction(int type,int version,string token,World world){
   if(version!=0)return U("ActionVersion:"+version);
   if(type!=9&&type!=10)return U("ActionType:"+type);
   return U(type==9?"PlayerBestowRequiresPlayStatsDispatchAndEventSemantics":"PlayerRemoveRequiresOutboundDispatchSemantics");
  }
 }
}
