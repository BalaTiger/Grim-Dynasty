using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Entity07F070 copies64 bytes, unlike the separate camera229160 getter.
 // This is one authoritative cell view, not a mirror of CoordinateFrame60.
 // All cells start Unknown. Only full same-address aliases and explicitly
 // shared typed cells are represented; shifted or cross-type byte aliases
 // are outside this own adapter's domain. No native layout is allocated.
 public sealed class EntityCoordinates64 {
  sealed class Lifetime { public bool Released; }
  readonly Lifetime life;
  readonly Field<float>[] basis;
  public readonly object AddressIdentity;
  public readonly WorldPosition24 Position00;
  public readonly Field<uint> Tail3C;
  // A new array container protects the slot map; its cells are the original
  // shared fields, never value snapshots or synchronized duplicate storage.
  public Field<float>[] Basis18 { get { return (Field<float>[])basis.Clone(); } }
  public readonly NavigationPoint3 Heading30;
  public EntityCoordinates64():this(new WorldPosition24(),NewBasis(),new Field<uint>()){}
  public EntityCoordinates64(WorldPosition24 position,Field<float>[] sharedBasis,Field<uint> tail){
   if(position==null||sharedBasis==null||tail==null)throw new ArgumentNullException("coordinate cells");
   if(sharedBasis.Length!=9)throw new ArgumentException("Exactly nine basis cells are required");
   basis=(Field<float>[])sharedBasis.Clone();
   for(int n=0;n<9;n++)if(basis[n]==null)throw new ArgumentException("Null basis cell");
   Position00=position;Tail3C=tail;AddressIdentity=position.AddressIdentity;life=new Lifetime();
   Heading30=new NavigationPoint3(basis[6],basis[7],basis[8]);
  }
  public EntityCoordinates64(EntityCoordinates64 sameAddress){
   if(sameAddress==null)throw new ArgumentNullException("sameAddress");
   AddressIdentity=sameAddress.AddressIdentity;Position00=sameAddress.Position00;
   basis=sameAddress.basis;Tail3C=sameAddress.Tail3C;Heading30=sameAddress.Heading30;life=sameAddress.life;
  }
  static Field<float>[] NewBasis(){var result=new Field<float>[9];for(int n=0;n<9;n++)result[n]=new Field<float>();return result;}
  internal Field<float> Basis(int index){return basis[index];}
  public void MarkReleased(){life.Released=true;Heading30.MarkReleased();}
  public void RequireLive(uint at){if(life.Released)throw new MissingContract(at,"Own64-byte coordinate view explicitly released; native lifetime is not inferred");}
 }

 // One composition per full Entity/Actor/Character owner identity. This does
 // not synthesize a native object or copy fields from the older MoveCharacter.
 public sealed class EntityMovementState {
  public readonly object Identity;
  public readonly EntityCoordinates64 CoordsD8;
  public readonly NavigationPoint3 Motion1D0;
  public readonly Field<object> Virtual00=new Field<object>();
  public readonly Field<EntityMovementState> Parent130=new Field<EntityMovementState>();
  public readonly Field<byte> Flag13C=new Field<byte>(),Flag13D=new Field<byte>();
  public readonly Field<uint> LastFrame178=new Field<uint>();
  public readonly Field<float> Height4A0=new Field<float>(),Scale3AC=new Field<float>();
  public readonly Field<float> RadiusComponent49C=new Field<float>(),RadiusComponent4A4=new Field<float>(),RadiusScale3A8=new Field<float>();
  bool released;
  public EntityMovementState(object identity):this(identity,new EntityCoordinates64(),new NavigationPoint3()){}
  public EntityMovementState(object identity,EntityCoordinates64 coordinates,NavigationPoint3 motion){
   if(identity==null||coordinates==null||motion==null)throw new ArgumentNullException("entity identity/view");
   Identity=identity;CoordsD8=coordinates;Motion1D0=motion;
  }
  public void MarkReleased(){released=true;CoordsD8.MarkReleased();Motion1D0.MarkReleased();}
  public void RequireLive(uint at){if(released)throw new MissingContract(at,"Own entity view explicitly released; native lifetime is not inferred");}
 }
 public sealed class EntityEngineState {
  public readonly WorldEngine Identity;
  public readonly Field<uint> FrameCount640=new Field<uint>();
  bool released;
  public EntityEngineState(WorldEngine identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
  public void MarkReleased(){released=true;}
  public void RequireLive(uint at){if(released)throw new MissingContract(at,"Own engine view explicitly released");}
 }
 // Explicit identity bindings for later callback integration. No lookup
 // creates a default object, no binding copies state, and no view is rebound.
 public sealed class EntityMovementRegistry {
  sealed class IdentityComparer:IEqualityComparer<object> {
   public new bool Equals(object x,object y){return Object.ReferenceEquals(x,y);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  readonly Dictionary<object,EntityMovementState> entities=new Dictionary<object,EntityMovementState>(new IdentityComparer());
  readonly Dictionary<object,EntityEngineState> engines=new Dictionary<object,EntityEngineState>(new IdentityComparer());
  public void Attach(EntityMovementState state){
   if(state==null)throw new ArgumentNullException("state");EntityMovementState prior;
   if(entities.TryGetValue(state.Identity,out prior)){if(!Object.ReferenceEquals(prior,state))throw new InvalidOperationException("Entity identity already has a different shared view");return;}
   entities.Add(state.Identity,state);
  }
  public void AttachEngine(EntityEngineState state){
   if(state==null)throw new ArgumentNullException("state");EntityEngineState prior;
   if(engines.TryGetValue(state.Identity,out prior)){if(!Object.ReferenceEquals(prior,state))throw new InvalidOperationException("Engine identity already has a different shared view");return;}
   engines.Add(state.Identity,state);
  }
  public EntityMovementState Entity(object identity,uint at){EntityMovementState result;if(identity==null||!entities.TryGetValue(identity,out result))throw new MissingContract(at,"Entity identity has no explicit shared view");return result;}
  public EntityEngineState Engine(WorldEngine identity,uint at){EntityEngineState result;if(identity==null||!engines.TryGetValue(identity,out result))throw new MissingContract(at,"Engine identity has no explicit shared view");return result;}
 }

 // Actual Engine evidence: r1-engine-callback-getters04..24, original SHA256
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Opaque MOV copies propagate Unknown. Arithmetic consumes values only at
 // their actual instruction. Binary32 round-to-nearest is the own FP domain;
 // rounding is explicit, but native FP
 // traps/MXCSR FTZ/DAZ and exact NaN payload selection are not emulated.
 // The parent coordinate branch is deliberately not replaced by a callback
 // fixture or local-frame fallback; slots180/188 and22A950 remain unknown.
 public static class EntityMovementContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own entity/coordinate view");return value;}
  static EntityMovementState Live(EntityMovementState state,uint at){Need(state,at).RequireLive(at);return state;}
  static EntityCoordinates64 Live(EntityCoordinates64 frame,uint at){Need(frame,at).RequireLive(at);return frame;}
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static Field<float> PointCell(NavigationPoint3 point,int component,uint at){
   Need(point,at).RequireLive(at);
   try{return component==0?point.X:component==1?point.Y:point.Z;}
   catch(MissingContract error){throw new MissingContract(at,"Own output point address is outside its declared extent",error);}
  }
  public static EntityCoordinates64 GetCoords07F070(EntityMovementState entity,EntityCoordinates64 hidden,Invocation invocation){
   var i=Need(invocation,0x07f076);i.At=0x07f076;var parentWord=Live(entity,i.At).Parent130.Snapshot();
   i.At=0x07f080;var parent=parentWord.Read(i.At);
   if(parent!=null){
    i.At=0x07f085;bool use188=entity.Flag13C.Read(i.At)!=0;
    // The parent's vptr is captured before the second flag read. The real
    // calls also borrow entity+140 and entity+144; their values are not read
    // by this prefix. A missing helper cannot manufacture those arguments.
    i.At=0x07f093;var parentTable=Live(parent,i.At).Virtual00.Snapshot();
    i.At=use188?0x07f0a9u:0x07f0bfu;bool inverseFlag=entity.Flag13D.Read(i.At)==0;
    i.At=use188?0x07f0b7u:0x07f0cdu;Need(parentTable.Read(i.At),i.At);
    throw new MissingContract(i.At,"Parent coordinate virtual slot"+(use188?"188":"180")+" and22A950 are unclosed; captured inverseFlag="+inverseFlag+"; output remains unwritten");
   }
   // Exactly four load16/store16 pairs. Snapshot every cell of a complete
   // native16-byte load before performing any corresponding destination store.
   i.At=0x07f0ef;var source=Live(entity.CoordsD8,i.At);var q0=source.Position00.Q0.Snapshot();var d8=source.Position00.D8.Snapshot();var dc=source.Position00.DC.Snapshot();
   i.At=0x07f0f9;var output=Live(hidden,i.At);output.Position00.Q0.CopyFrom(q0);output.Position00.D8.CopyFrom(d8);output.Position00.DC.CopyFrom(dc);
   i.At=0x07f0fc;Live(source,i.At);var d10=source.Position00.D10.Snapshot();var d14=source.Position00.D14.Snapshot();var b0=source.Basis(0).Snapshot();var b1=source.Basis(1).Snapshot();
   i.At=0x07f103;Live(output,i.At);output.Position00.D10.CopyFrom(d10);output.Position00.D14.CopyFrom(d14);output.Basis(0).CopyFrom(b0);output.Basis(1).CopyFrom(b1);
   i.At=0x07f107;Live(source,i.At);var b2=source.Basis(2).Snapshot();var b3=source.Basis(3).Snapshot();var b4=source.Basis(4).Snapshot();var b5=source.Basis(5).Snapshot();
   i.At=0x07f10e;Live(output,i.At);output.Basis(2).CopyFrom(b2);output.Basis(3).CopyFrom(b3);output.Basis(4).CopyFrom(b4);output.Basis(5).CopyFrom(b5);
   i.At=0x07f112;Live(source,i.At);var b6=source.Basis(6).Snapshot();var b7=source.Basis(7).Snapshot();var b8=source.Basis(8).Snapshot();var tail=source.Tail3C.Snapshot();
   i.At=0x07f119;Live(output,i.At);output.Basis(6).CopyFrom(b6);output.Basis(7).CopyFrom(b7);output.Basis(8).CopyFrom(b8);output.Tail3C.CopyFrom(tail);
   return hidden;
  }
  public static ulong GetRegion07F130(EntityMovementState entity,Invocation invocation){
   var i=Need(invocation,0x07f130);i.At=0x07f130;var current=Live(entity,i.At);var parentWord=current.Parent130.Snapshot();
   i.At=0x07f137;var parent=parentWord.Read(i.At);var visited=new HashSet<EntityMovementState>();visited.Add(current);
   while(parent!=null){
    current=parent;i.At=0x07f143;
    if(!visited.Add(current))throw new MissingContract(i.At,"Parent cycle exceeds the own acyclic traversal domain; this is not a native failure return");
    parentWord=Live(current,i.At).Parent130.Snapshot();i.At=0x07f14a;parent=parentWord.Read(i.At);
   }
   i.At=0x07f14f;return Live(current.CoordsD8,i.At).Position00.Q0.Read(i.At);
  }
  public static NavigationPoint3 GetMotion07EB20(EntityMovementState entity,Invocation invocation){
   var i=Need(invocation,0x07eb20);i.At=0x07eb20;
   // LEA forms a borrowed address and does not read three floats. A released
   // own point is still returned as that same identity; its consumer checks it.
   return Need(entity,i.At).Motion1D0;
  }
  public static NavigationPoint3 GetWorldPosition22C1A0(WorldPosition24 position,NavigationPoint3 hidden,WorldCoordinateState regions,Invocation invocation){
   var i=Need(invocation,0x22c1a4);i.At=0x22c1a4;var regionWord=Need(position,i.At).Q0.Snapshot();
   i.At=0x22c1a7;var region=Need(regions,i.At).Region(regionWord.Read(i.At),i.At);var offsetX=region.Offset3C.Snapshot();var offsetY=region.Offset40.Snapshot();
   i.At=0x22c1ac;var offsetZ=region.Offset44.Snapshot();
   // Signed32 conversion, separate scalar addition, then X/Z/Y stores. The
   // offset pair and Z were captured before any output store; coordinates are
   // reread at the real ADDSS sites, not precomputed as a vector snapshot.
   i.At=0x22c1be;float x=F((float)offsetX.Read(i.At));
   i.At=0x22c1ca;x=F(x+Float(position.D8.Read(i.At)));
   i.At=0x22c1cf;float z=F((float)offsetZ.Read(i.At));
   i.At=0x22c1d2;PointCell(hidden,0,i.At).Write(x);
   i.At=0x22c1d6;z=F(z+Float(position.D10.Read(i.At)));
   i.At=0x22c1e2;float y=F((float)offsetY.Read(i.At));
   i.At=0x22c1e5;PointCell(hidden,2,i.At).Write(z);
   i.At=0x22c1ea;y=F(y+Float(position.DC.Read(i.At)));
   i.At=0x22c1ef;PointCell(hidden,1,i.At).Write(y);return hidden;
  }
  public static float GetHeight02B460(EntityMovementState entity,Invocation invocation){
   var i=Need(invocation,0x02b460);i.At=0x02b460;var height=Live(entity,i.At).Height4A0.Snapshot();
   i.At=0x02b468;return F(height.Read(i.At)*entity.Scale3AC.Read(i.At));
  }
  // Actual Engine02B410, r1-actor-radius04..06: three-component length scaled by
  // 3A8. It shares4A0 with GetHeight but must not borrow Height's scale3AC.
  // MOVSS snapshots preserve Unknown; the arithmetic consumes each payload
  // in instruction order. UCOMISS/JZ takes the zero arm for unordered too.
  public static float GetRadius02B410(EntityMovementState entity,Invocation invocation){
   var i=Need(invocation,0x02b410);i.At=0x02b410;var state=Live(entity,i.At);var a0=state.Height4A0.Snapshot();
   i.At=0x02b418;var c49=state.RadiusComponent49C.Snapshot();i.At=0x02b420;var a4=state.RadiusComponent4A4.Snapshot();
   i.At=0x02b428;float x=c49.Read(i.At);x=F(x*x);
   i.At=0x02b42c;float y=a0.Read(i.At);y=F(y*y);
   i.At=0x02b430;float z=a4.Read(i.At);z=F(z*z);
   i.At=0x02b434;float squared=F(x+y);i.At=0x02b43b;squared=F(squared+z);
   float length=0;i.At=0x02b43f;if(squared!=0&&!Single.IsNaN(squared)){i.At=0x02b447;length=F((float)Math.Sqrt(squared));}
   i.At=0x02b44b;return F(length*state.RadiusScale3A8.Read(i.At));
  }
  public static uint GetLastFrameUpdated07E7C0(EntityMovementState entity,Invocation invocation){
   var i=Need(invocation,0x07e7c0);i.At=0x07e7c0;return Live(entity,i.At).LastFrame178.Read(i.At);
  }
  public static uint GetFrameCount077880(EntityEngineState engine,Invocation invocation){
   var i=Need(invocation,0x077880);i.At=0x077880;Need(engine,i.At).RequireLive(i.At);return engine.FrameCount640.Read(i.At);
  }
 }
}
