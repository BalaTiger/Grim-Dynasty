using System;

namespace GrimDynasty.ReverseClient.Actions {
 // One explicit binding retains both views. There is no value/address lookup,
 // global map, parallel position storage, whole-word read or data copy here.
 // Retain this binding for round trips: Words/Dwords return these exact views.
 public sealed class WorldPositionWordBridge {
  public readonly WorldPosition Words;
  public readonly WorldPosition24 Dwords;
  public object AddressIdentity{get{return Dwords.AddressIdentity;}}
  WorldPositionWordBridge(WorldPosition words,WorldPosition24 dwords){Words=words;Dwords=dwords;}
  public static WorldPositionWordBridge FromDwords(WorldPosition24 source){
   if(source==null)throw new ArgumentNullException("source");
   var words=new WorldPosition(source.Q0,UInt64FieldWords.Join(source.D8,source.DC),UInt64FieldWords.Join(source.D10,source.D14));
   return new WorldPositionWordBridge(words,source);
  }
  public static WorldPositionWordBridge FromWords(WorldPosition source){
   if(source==null)throw new ArgumentNullException("source");
   return FromWords(source,source);
  }
  // An existing explicit address token can be retained when the caller already
  // knows it. Equal values or sharing only Q0 never establish such identity.
  public static WorldPositionWordBridge FromWords(WorldPosition source,object addressIdentity){
   if(source==null||addressIdentity==null)throw new ArgumentNullException("source/addressIdentity");
   var q8=UInt64FieldWords.Split(source.Q8);var q16=UInt64FieldWords.Split(source.Q16);
   return new WorldPositionWordBridge(source,new WorldPosition24(addressIdentity,source.Q0,q8.Low32,q8.High32,q16.Low32,q16.High32));
  }
 }
}
