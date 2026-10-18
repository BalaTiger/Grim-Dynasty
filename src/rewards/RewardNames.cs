using System;
using GrimDynasty.TokenActions;
namespace GrimDynasty.Rewards {
 public static class RewardNames {
  // Engine.dll Name::Create: C-string length, MOVSX of each char, XOR, IMUL.
  public static uint Create(byte[] bytes){
   if(bytes==null)throw new ArgumentNullException("bytes");uint h=0x811c9dc5;
   foreach(byte b in bytes){if(b==0)break;h=unchecked((h^(uint)(int)unchecked((sbyte)b))*0x1000193u);}return h;
  }
 }
 public sealed class NameCacheSnapshot {
  readonly object gate=new object();
  public bool StateKnown=true,Initialized;
  public uint Value,InitializationCount;
  public NameCacheSnapshot Copy(){lock(gate){return new NameCacheSnapshot{StateKnown=StateKnown,Initialized=Initialized,Value=Value,InitializationCount=InitializationCount};}}
  public MutationResult EnsureHeadEffect(){lock(gate){
   if(!StateKnown)return MutationResult.Unsupported("UnknownStaticNameCacheState");
   if(Initialized)return Value==0xcb999846u?MutationResult.Known(false):MutationResult.Unsupported("InconsistentHeadEffectCache");
   Value=RewardNames.Create(new byte[]{72,101,97,100,69,102,102,101,99,116});Initialized=true;InitializationCount=unchecked(InitializationCount+1);return MutationResult.Known(true);
  }}
 }
}
