using System;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameEquipmentHandStateInvocation {
  public readonly Invocation Native=new Invocation(),LeftSingleton=new Invocation(),RightSingleton=new Invocation();
  public readonly GameEquipmentGetterInvocation LeftGetter=new GameEquipmentGetterInvocation(),RightGetter=new GameEquipmentGetterInvocation();
  public readonly GameObjectManagerLookupInvocation LeftLookup=new GameObjectManagerLookupInvocation(),RightLookup=new GameObjectManagerLookupInvocation();
  public readonly Field<uint> LeftId=new Field<uint>(),RightId=new Field<uint>();
  public readonly Field<object> LeftResult=new Field<object>(),RightResult=new Field<object>();
  public GameMovementCharacterState Owner;public GameEquipmentManagerState Equipment;
  public object LeftManager,RightManager;
  public bool Used,LeftGetterReturned,RightGetterReturned,LeftSingletonReturned,RightSingletonReturned,LeftLookupReturned,RightLookupReturned,Returned;
  public uint ReturnValue;
 }
 // Game285E50 uses TWO fresh hot singleton reads and actual locked lookups.
 // Only both known-null results reach the observed EAX=0 return here. ID zero
 // is still looked up; matched-null payload and missing key both count as null.
 // shortcut: nonnull weapon class/virtual6B0 paths stop at the first unknown
 // object-vptr read. No unarmed default or weapon class is guessed.
 public static class GameEquipmentHandStateContracts {
  static T Need<T>(T value,uint at,string why)where T:class{if(value==null)throw new MissingContract(at,why);return value;}
  static PlayerMousePlayerLookupContext Lookup(GameMovementCharacterState owner,PlayerMousePlayerLookupContext context,uint at){
   var result=Need(context,at,"Hand state has no borrowed mouse/ObjectManager context");
   var view=result.Bridge.ForCallback(owner.Interface38,at);
   if(!Object.ReferenceEquals(view.Owner,owner))throw new MissingContract(at,"Hand state cannot use another canonical owner");return result;
  }
  public static uint GetHandState285E50(GameMovementCharacterState owner,PlayerMousePlayerLookupContext context,GameEquipmentHandStateInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");if(c.Used)throw new InvalidOperationException("Use a fresh hand-state invocation");c.Used=true;
   try{
    c.Native.At=0x285e67;c.Owner=Need(owner,c.Native.At,"Missing original Player for embedded EquipManager+5C8");c.Equipment=GameEquipmentManagerState.ForOwner(c.Owner);
    c.LeftId.CopyFrom(GameEquipmentGetterContracts.GetWeaponIdLeft284720(c.Equipment,c.LeftGetter));c.LeftGetterReturned=true;
    c.Native.At=0x285e6e;var lookup=Lookup(owner,context,c.Native.At);
    c.LeftManager=GameReachedGoalEngineContracts.GetObjectManager01C040(lookup.Engine,c.LeftSingleton);c.LeftSingletonReturned=true;
    c.Native.At=0x285e79;var leftManager=c.LeftManager as GameObjectManagerState;
    if(leftManager==null)throw new MissingContract(0x019d5f,"Published ObjectManager has no shared concrete hash-lookup view");
    c.LeftResult.CopyFrom(GameObjectManagerLookupContracts.LookupBits019D40(leftManager,c.LeftId,c.LeftLookup));c.LeftLookupReturned=true;
    c.Native.At=0x285e88;
    if(c.LeftResult.Read(c.Native.At)!=null){c.Native.At=0x285e8d;throw new MissingContract(c.Native.At,"Nonnull left weapon object needs its real primary/class0 and type6B0 targets");}
    c.Native.At=0x285ef7;c.RightId.CopyFrom(GameEquipmentGetterContracts.GetWeaponIdRight284670(c.Equipment,c.RightGetter));c.RightGetterReturned=true;
    c.Native.At=0x285efe;c.RightManager=GameReachedGoalEngineContracts.GetObjectManager01C040(lookup.Engine,c.RightSingleton);c.RightSingletonReturned=true;
    c.Native.At=0x285f09;var rightManager=c.RightManager as GameObjectManagerState;
    if(rightManager==null)throw new MissingContract(0x019d5f,"Fresh ObjectManager has no shared concrete hash-lookup view");
    c.RightResult.CopyFrom(GameObjectManagerLookupContracts.LookupBits019D40(rightManager,c.RightId,c.RightLookup));c.RightLookupReturned=true;
    c.Native.At=0x285f11;
    if(c.RightResult.Read(c.Native.At)!=null){c.Native.At=0x285f1a;throw new MissingContract(c.Native.At,"Nonnull right weapon object needs its real primary/class0 and type6B0 targets");}
    c.Native.At=0x286072;c.ReturnValue=0;c.Native.At=0x286088;c.Returned=true;return c.ReturnValue;
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
 }
}
