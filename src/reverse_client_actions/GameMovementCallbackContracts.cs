using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Exact observed table identities, not an extensible callback injection map.
 // Selecting one is explicit own setup; native construction/publication is not
 // inferred. Mutable primary/interface vptr cells remain separate and unknown.
 public sealed class GameMovementPrimaryTable {
  public readonly uint Rva;
  GameMovementPrimaryTable(uint rva){Rva=rva;}
  public static readonly GameMovementPrimaryTable Character=new GameMovementPrimaryTable(0x6df6d0);
  public static readonly GameMovementPrimaryTable Player=new GameMovementPrimaryTable(0x787100);
 }
 public sealed class GameMovementInterfaceTable {
  public readonly uint Rva;
  GameMovementInterfaceTable(uint rva){Rva=rva;}
  public static readonly GameMovementInterfaceTable Character=new GameMovementInterfaceTable(0x6df690);
  public static readonly GameMovementInterfaceTable Player=new GameMovementInterfaceTable(0x787080);
 }
 public sealed class GameMovementCharacterState {
  public readonly EntityMovementState Entity;
  public readonly GameMovementInterfaceView Interface38;
  public readonly Field<byte> FlagDC8=new Field<byte>(),FlagDC9=new Field<byte>(),FlagEC2=new Field<byte>();
  public readonly Field<float> SpeedScaleDCC=new Field<float>();
  public readonly Field<uint> ActionState24B4=new Field<uint>();
  public GameMovementCharacterState(EntityMovementState entity){
   if(entity==null)throw new ArgumentNullException("entity");Entity=entity;Interface38=new GameMovementInterfaceView(this);
  }
 }
 public sealed class GameMovementInterfaceView {
  public readonly GameMovementCharacterState FullOwner;
  public readonly Field<object> Virtual38=new Field<object>();
  internal GameMovementInterfaceView(GameMovementCharacterState owner){FullOwner=owner;}
 }
 // ABI Params38 borrows Agent+10, not a value snapshot or synchronized mirror.
 // In particular Params+24 is Agent+34, index6: Created/Update never write it.
 public sealed class GameMovementParameters {
  public readonly NavigationCrowdAgent Agent;
  public readonly Field<float> Radius00,Height04,Speed08;
  public readonly Field<uint> Flags34;
  public GameMovementParameters(NavigationCrowdAgent agent){
   if(agent==null)throw new ArgumentNullException("agent");Agent=agent;
   Radius00=agent.Radius10;Height04=agent.Height14;Speed08=agent.Parameter18;Flags34=agent.Flags44;
  }
  public Field<uint> Dword(uint offset,uint at){
   Agent.RequireLive(at);if(offset==0x34)return Flags34;
   if(offset<0xc||offset>0x30||(offset&3)!=0)throw new MissingContract(at,"Outside own DWORD parameter-view extent");
   return Agent.ParameterBits1C40[(offset-0xc)/4];
  }
  public void RequireLive(uint at){Agent.RequireLive(at);}
 }
 public sealed class GameMovementCallbackContext {
  sealed class IdentityComparer:IEqualityComparer<object> {
   public new bool Equals(object x,object y){return Object.ReferenceEquals(x,y);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  readonly Dictionary<object,GameMovementCharacterState> owners=new Dictionary<object,GameMovementCharacterState>(new IdentityComparer());
  public readonly WorldCoordinateBoundaries World;
  public readonly EntityMovementRegistry Entities;
  // These are the RTTI_ClassInfo identities returned by019D20/016D10. They
  // deliberately do not refer to the unrelated factory-registration models.
  public readonly AddressSymbol<ClassRecord> CharacterClass,PlayerClass;
  public GameMovementCallbackContext(WorldCoordinateBoundaries world,EntityMovementRegistry entities,
    AddressSymbol<ClassRecord> characterClass,AddressSymbol<ClassRecord> playerClass){
   if(world==null||entities==null||characterClass==null||playerClass==null)throw new ArgumentNullException("callback context");
   World=world;Entities=entities;CharacterClass=characterClass;PlayerClass=playerClass;
   // OWN setup precondition only: bind stable symbolic address identities before
   // calling native-contract methods. No ClassRecord field is dereferenced.
   // AddressSymbol cannot forget/rebind, so later LEAs cannot acquire an earlier
   // artificial unknown-object stop. Parent10 remains independently Unknown.
   if(Object.ReferenceEquals(CharacterClass.Read(0xa5cba0),PlayerClass.Read(0xa5cab0)))throw new ArgumentException("Distinct native class symbols require distinct own address identities");
  }
  // Explicit own composition. One full-owner identity cannot silently acquire
  // another Game field view or a second Entity state with copied coordinates.
  public void Attach(GameMovementCharacterState state){
   if(state==null)throw new ArgumentNullException("state");GameMovementCharacterState prior;
   if(owners.TryGetValue(state.Entity.Identity,out prior)&&!Object.ReferenceEquals(prior,state))throw new InvalidOperationException("Owner identity already has another Game movement view");
   Entities.Attach(state.Entity);owners[state.Entity.Identity]=state;
  }
  public GameMovementCharacterState Owner(GameMovementInterfaceView view,uint at){
   GameMovementCharacterState found;
   if(view==null||!owners.TryGetValue(view.FullOwner.Entity.Identity,out found)||!Object.ReferenceEquals(found,view.FullOwner))
    throw new MissingContract(at,"Game callback owner has no explicit unique shared binding");
   if(!Object.ReferenceEquals(Entities.Entity(found.Entity.Identity,at),found.Entity))throw new MissingContract(at,"Game/Entity shared identity mismatch");
   return found;
  }
 }

 // Actual Game bodies066C20/3E8200/066D60, r1-game-movement-interface;
 // exact tables from r1-player-movement-interface-slots and callback-dependencies.
 // New05B730/591480 bodies: r1-game-callback-remaining-retry-1,04..09.
 // Same-typed cell aliases only, stable own lifetimes, masked ordinary binary32
 // completion. Native ABI/heap/EH, shifted byte aliases, FP traps/MXCSR modes and
 // native loaded state are not emulated. No user-supplied function can fabricate
 // a successful radius, action or class-parent result: unknown fields stop.
 public static class GameMovementCallbackContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own Game movement view");return value;}
  static Field<float> Cell(NavigationPoint3 point,int component,uint at){
   Need(point,at).RequireLive(at);
   try{return component==0?point.X:component==1?point.Y:point.Z;}
   catch(MissingContract error){throw new MissingContract(at,"Output point exceeds declared own extent",error);}
  }
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Speed(NavigationPoint3 motion,bool created,Invocation i){
   // Both real bodies load Y, X, Z, then square X,Y,Z and add (X2+Y2)+Z2.
   i.At=created?0x066cd4u:0x066d85u;float y=Cell(motion,1,i.At).Read(i.At);
   i.At=created?0x066cd9u:0x066d8au;float x=Cell(motion,0,i.At).Read(i.At);
   i.At=created?0x066cddu:0x066d8eu;float z=Cell(motion,2,i.At).Read(i.At);
   i.At=created?0x066ce2u:0x066d93u;x=F(x*x);
   i.At=created?0x066ce6u:0x066d97u;y=F(y*y);
   i.At=created?0x066ceau:0x066d9bu;z=F(z*z);
   i.At=created?0x066ceeu:0x066d9fu;float squared=F(x+y);
   i.At=created?0x066cf5u:0x066da3u;squared=F(squared+z);
   // UCOMISS/JZ takes the zero arm for equality OR unordered.
   i.At=created?0x066cf9u:0x066da7u;if(squared==0||Single.IsNaN(squared))return 0;
   i.At=created?0x066d01u:0x066db4u;return F((float)Math.Sqrt(squared));
  }
  static GameMovementPrimaryTable Primary(Field<object> captured,uint at){
   var value=Need(captured,at).Read(at);
   if(Object.ReferenceEquals(value,GameMovementPrimaryTable.Character))return GameMovementPrimaryTable.Character;
   if(Object.ReferenceEquals(value,GameMovementPrimaryTable.Player))return GameMovementPrimaryTable.Player;
   throw new MissingContract(at,"Unknown primary vtable; no virtual target inferred");
  }
  static GameMovementInterfaceTable Interface(Field<object> captured,uint at){
   var value=Need(captured,at).Read(at);
   if(Object.ReferenceEquals(value,GameMovementInterfaceTable.Character))return GameMovementInterfaceTable.Character;
   if(Object.ReferenceEquals(value,GameMovementInterfaceTable.Player))return GameMovementInterfaceTable.Player;
   throw new MissingContract(at,"Unknown +38 interface vtable; no virtual target inferred");
  }
  static float Radius(GameMovementCharacterState owner,GameMovementPrimaryTable table,Invocation i){
   // Both exact slot1C8 targets5D9F8C are proven FF25 -> Engine02B410.
   return EntityMovementContracts.GetRadius02B410(owner.Entity,i);
  }
  static uint CurrentAction(GameMovementCharacterState owner,GameMovementPrimaryTable table,Invocation i){
   return GetActionState05B730(owner,i);
  }
  static ClassRecord ClassInfo(GameMovementPrimaryTable table,GameMovementCallbackContext context,Invocation i){
   // Actual one-LEA/RET bodies, not a callback map or fabricated class tree.
   i.At=Object.ReferenceEquals(table,GameMovementPrimaryTable.Player)?0x016d10u:0x019d20u;
   return Object.ReferenceEquals(table,GameMovementPrimaryTable.Player)?Need(context,i.At).PlayerClass.Read(i.At):Need(context,i.At).CharacterClass.Read(i.At);
  }
  // Game05B730 is exactly MOV EAX,[RCX+24B4];RET. Keep one authoritative
  // unknown-initialized field; neither the table dispatch nor repeated calls
  // manufacture or cache a controller/action value.
  public static uint GetActionState05B730(GameMovementCharacterState owner,Invocation invocation){
   var i=Need(invocation,0x05b730);i.At=0x05b730;Need(owner,i.At).Entity.RequireLive(i.At);
   return owner.ActionState24B4.Read(i.At);
  }
  // Game591480 compares Parent10, never the incoming current identity itself.
  // Only the Boolean AL result consumed by066E25 is modeled. The native null
  // arm clears AL without defining the upper RAX bits; do not expose a guessed
  // full register return. Iteration replaces recursive calls only in the own
  // stable acyclic graph domain; a cycle is Unsupported, not native false.
  public static bool IsDerivedFrom591480(ClassRecord current,ClassRecord target,Invocation invocation){
   var i=Need(invocation,0x591484);var visited=new HashSet<ClassRecord>();
   while(true){
    i.At=0x591484;var node=Need(current,i.At);
    if(!visited.Add(node))throw new MissingContract(i.At,"Class parent cycle exceeds own acyclic domain; no native failure return inferred");
    var parent=node.Parent10.Read(i.At);i.At=0x591488;if(parent==null){i.At=visited.Count>1?0x59149bu:0x5914acu;return false;}
    i.At=0x59148d;if(Object.ReferenceEquals(parent,target)){i.At=0x5914a2;return true;}
    i.At=0x591492;current=parent;
   }
  }
  static void WriteFlags(GameMovementParameters p,uint andMask,uint orMask,uint at){
   var flags=Need(p,at).Dword(0x34,at);flags.Write((flags.Read(at)&andMask)|orMask);
  }
  public static void CharacterCreated066C20(GameMovementInterfaceView callback,NavigationPoint3 outPosition,
    NavigationPoint3 outHeading,GameMovementParameters parameters,GameMovementCallbackContext context,Invocation invocation){
   var i=Need(invocation,0x066c4f);i.At=0x066c4f;var owner=Need(context,i.At).Owner(callback,i.At);var frame=new EntityCoordinates64();
   EntityMovementContracts.GetCoords07F070(owner.Entity,frame,i);
   // Engine22C0B0 is independently saved in r1-engine-camera-target: MOV RAX,[RCX];RET.
   i.At=0x22c0b0;ulong region=frame.Position00.Q0.Read(i.At);
   if(region!=0){
    i.At=0x066c6f;var world=EntityMovementContracts.GetWorldPosition22C1A0(frame.Position00,new NavigationPoint3(),Need(context,i.At).World.State,i);
    i.At=0x066c75;var x=Cell(world,0,i.At).Snapshot();var y=Cell(world,1,i.At).Snapshot();
    i.At=0x066c79;Cell(outPosition,0,i.At).CopyFrom(x);Cell(outPosition,1,i.At).CopyFrom(y);
    i.At=0x066c7d;var z=Cell(world,2,i.At).Snapshot();i.At=0x066c80;Cell(outPosition,2,i.At).CopyFrom(z);
   }
   i.At=0x066c83;var hx=Cell(frame.Heading30,0,i.At).Snapshot();var hy=Cell(frame.Heading30,1,i.At).Snapshot();
   i.At=0x066c8d;var hz=Cell(frame.Heading30,2,i.At).Snapshot();
   i.At=0x066c93;Cell(outHeading,0,i.At).CopyFrom(hx);Cell(outHeading,1,i.At).CopyFrom(hy);
   i.At=0x066c98;Cell(outHeading,2,i.At).CopyFrom(hz);
   i.At=0x066c9c;Need(parameters,i.At).Dword(0xc,i.At).Write(0);
   i.At=0x066c9f;parameters.Dword(0x34,i.At).Write(0x481);
   i.At=0x066ca6;parameters.Dword(0x10,i.At).Write(4);
   i.At=0x066cad;parameters.Dword(0x14,i.At).Write(6);
   i.At=0x066cb4;parameters.Dword(0x2c,i.At).Write(0);parameters.Dword(0x30,i.At).Write(0); // one nativeQWORD clear
   i.At=0x066cb8;parameters.Dword(0x18,i.At).Write(0x3ccccccd);
   i.At=0x066cbf;float height=EntityMovementContracts.GetHeight02B460(owner.Entity,i);
   i.At=0x066cc9;parameters.RequireLive(i.At);parameters.Height04.Write(height);
   i.At=0x066cce;var motion=EntityMovementContracts.GetMotion07EB20(owner.Entity,i);
   float speed=Speed(motion,true,i);
   i.At=0x066d05;parameters.RequireLive(i.At);parameters.Speed08.Write(speed);
   i.At=0x066d0e;owner.Entity.RequireLive(i.At);var tableBits=owner.Entity.Virtual00.Snapshot();
   i.At=0x066d12;float radius=Radius(owner,Primary(tableBits,i.At),i);
   i.At=0x066d18;parameters.RequireLive(i.At);parameters.Radius00.Write(radius);
   i.At=0x066d1c;parameters.Dword(0x1c,i.At).Write(0);
   i.At=0x066d1f;parameters.Dword(0x20,i.At).Write(1);
   i.At=0x066d26;parameters.Dword(0x28,i.At).Write(0x3f8ccccd);
   i.At=0x066d2d;parameters.Dword(0x2c,i.At).Write(400);
   i.At=0x066d34;owner.Entity.RequireLive(i.At);if(owner.FlagEC2.Read(i.At)!=0){i.At=0x066d3d;WriteFlags(parameters,0xffffffffu,0x20,i.At);}
   i.At=0x066d5b;
  }
  public static void PlayerCreated3E8200(GameMovementInterfaceView callback,NavigationPoint3 outPosition,
    NavigationPoint3 outHeading,GameMovementParameters parameters,GameMovementCallbackContext context,Invocation invocation){
   var i=Need(invocation,0x3e8209);i.At=0x3e8209;CharacterCreated066C20(callback,outPosition,outHeading,parameters,context,i);
   i.At=0x3e820e;WriteFlags(parameters,0xffffffffu,0x80000044u,i.At);
   i.At=0x3e8215;var cell=parameters.Dword(0x20,i.At);cell.Write(unchecked(cell.Read(i.At)+1u));
   i.At=0x3e8218;parameters.Dword(0x10,i.At).Write(1);i.At=0x3e821f;parameters.Dword(0x14,i.At).Write(2);i.At=0x3e822b;
  }
  public static void CharacterUpdate066D60(GameMovementInterfaceView callback,GameMovementParameters parameters,
    GameMovementCallbackContext context,Invocation invocation){
   var i=Need(invocation,0x066d7c);i.At=0x066d7c;var owner=Need(context,i.At).Owner(callback,i.At);
   var motion=EntityMovementContracts.GetMotion07EB20(owner.Entity,i);float speed=Speed(motion,false,i);
   i.At=0x066db8;Need(parameters,i.At).RequireLive(i.At);parameters.Speed08.Write(speed);
   i.At=0x066dbd;owner.Entity.RequireLive(i.At);float factor=owner.SpeedScaleDCC.Read(i.At);
   if(factor>0){i.At=0x066dce;parameters.RequireLive(i.At);parameters.Speed08.Write(F(factor*speed));}
   i.At=0x066dd3;owner.Entity.RequireLive(i.At);if(owner.FlagDC8.Read(i.At)!=0){i.At=0x066ddc;WriteFlags(parameters,0xffffffffu,0x800,i.At);}
   else{i.At=0x066de5;WriteFlags(parameters,0xfffff7ffu,0,i.At);}
   i.At=0x066dec;owner.Entity.RequireLive(i.At);
   if(owner.FlagDC9.Read(i.At)!=0){i.At=0x066df5;parameters.Dword(0x14,i.At).Write(8);}
   else{
    i.At=0x066dfe;var tableBits=owner.Entity.Virtual00.Snapshot();i.At=0x066e04;var actual=ClassInfo(Primary(tableBits,i.At),context,i);
    i.At=0x066e06;var expected=Need(context,i.At).PlayerClass.Read(i.At);bool player=Object.ReferenceEquals(actual,expected);
    if(!player){i.At=0x066e12;var parent=Need(actual,i.At).Parent10.Read(i.At);if(parent!=null){player=Object.ReferenceEquals(parent,expected);if(!player){i.At=0x066e20;player=IsDerivedFrom591480(parent,expected,i);}}}
    i.At=0x066e41;parameters.Dword(0x14,i.At).Write(player?2u:6u);
   }
   i.At=0x066e44;owner.Entity.RequireLive(i.At);var firstTable=owner.Entity.Virtual00.Snapshot();
   i.At=0x066e4a;bool special=CurrentAction(owner,Primary(firstTable,i.At),i)==0x13;
   if(!special){i.At=0x066e55;owner.Entity.RequireLive(i.At);var secondTable=owner.Entity.Virtual00.Snapshot();i.At=0x066e5b;special=CurrentAction(owner,Primary(secondTable,i.At),i)==0x15;}
   if(special){i.At=0x066e73;WriteFlags(parameters,0xffffff7fu,0x20,i.At);}
   else{i.At=0x066e66;WriteFlags(parameters,0xffffffdfu,0,i.At);i.At=0x066e6a;WriteFlags(parameters,0xffffffffu,0x80,i.At);}
   i.At=0x066e80;owner.Entity.RequireLive(i.At);if(owner.FlagEC2.Read(i.At)!=0){i.At=0x066e89;WriteFlags(parameters,0xffffffffu,0x20,i.At);}
   i.At=0x066e94;var engineIdentity=Need(context,i.At).World.EngineGlobal.Snapshot();
   i.At=0x066e97;var engine=context.Entities.Engine(engineIdentity.Read(i.At),i.At);uint frame=EntityMovementContracts.GetFrameCount077880(engine,i);
   uint previous=unchecked(frame-1u);i.At=0x066ea3;uint last=EntityMovementContracts.GetLastFrameUpdated07E7C0(owner.Entity,i);
   i.At=last<previous?0x066ec4u:0x066eadu;WriteFlags(parameters,last<previous?0xffffffffu:0xffffefffu,last<previous?0x1000u:0u,i.At);
   i.At=last<previous?0x066edau:0x066ec3u;
  }
  // CALL-SITE fragments only: preceding list/hash/state/bounds work is NOT
  // implemented here. Created uses the original25AE20 RDX captured intoRSI,
  // whereas Update really reloads Agent.Callback08 at25C9A1. They must not be
  // forcibly synchronized; both borrow the explicit target agent's cells.
  public static void InvokeCreatedCall25AF13(GameMovementInterfaceView originalCallback,NavigationCrowdAgent agent,
    GameMovementCallbackContext context,Invocation invocation){
   var i=Need(invocation,0x25af01);i.At=0x25af01;var bits=Need(originalCallback,i.At).Virtual38.Snapshot();
   i.At=0x25af13;var table=Interface(bits,i.At);Need(agent,i.At).RequireLive(i.At);var parameters=new GameMovementParameters(agent);
   if(Object.ReferenceEquals(table,GameMovementInterfaceTable.Player))PlayerCreated3E8200(originalCallback,agent.Position48,agent.Heading54,parameters,context,i);
   else CharacterCreated066C20(originalCallback,agent.Position48,agent.Heading54,parameters,context,i);
   i.At=0x25af15;
  }
  public static void InvokeUpdateCall25C9AF(NavigationCrowdAgent agent,GameMovementCallbackContext context,Invocation invocation){
   var i=Need(invocation,0x25c9a1);i.At=0x25c9a1;Need(agent,i.At).RequireLive(i.At);var callbackBits=agent.Callback08.Snapshot();
   i.At=0x25c9a9;var callback=callbackBits.Read(i.At) as GameMovementInterfaceView;
   var tableBits=Need(callback,i.At).Virtual38.Snapshot();
   i.At=0x25c9af;Interface(tableBits,i.At);CharacterUpdate066D60(callback,new GameMovementParameters(agent),context,i);i.At=0x25c9b2;
  }
 }
}
