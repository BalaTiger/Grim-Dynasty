using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskRuntime {
 public static class InboundTokens {
  // Native GameEngineInboundInterface methods 0x180311de0 / 0x180311e50.
  // Already decoded command parameters; no network/packet receiver is implied.
  public static MutationResult Give(World world,uint playerId,string token){return Mutations.SetPlayerToken(world,playerId,token);}
  public static MutationResult Remove(World world,uint playerId,string token){return Mutations.RemovePlayerToken(world,playerId,token);}
 }
}
