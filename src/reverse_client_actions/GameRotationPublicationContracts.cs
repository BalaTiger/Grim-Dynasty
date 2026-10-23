using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Additional observed Character fields share the existing full owner; no
 // copied Entity, coordinates or private vptr identity is introduced.
 public sealed class GameRotationState {
  public readonly GameMovementCharacterState Character;
  public readonly Field<byte> Flag26B0=new Field<byte>();
  public readonly Field<float> Rate45A8=new Field<float>(),Rate45AC=new Field<float>(),Rate45B0=new Field<float>();
  internal GameRotationState(GameMovementCharacterState c){Character=c;}
 }
 public sealed class GameRotationConstants {
  public readonly GameMovementMathConstants Matrix=new GameMovementMathConstants();
  public readonly Field<float> Minimum7A7FFC=new Field<float>(),MinusOne7A81AC=new Field<float>(),Half7A8024=new Field<float>(),Scale7A7FA4=new Field<float>();
  public readonly Field<uint> Sign7A8350=new Field<uint>();
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  // Explicit own choice of saved disk bytes, not an assertion about native
  // loaded writable data. All four XOR lanes are80000000; only low is used.
  public void UseObservedDiskConstants(){Matrix.UseObservedDiskOne();Minimum7A7FFC.Write(Float(0x3e4ccccd));MinusOne7A81AC.Write(Float(0xbf800000));Half7A8024.Write(Float(0x3f000000));Scale7A7FA4.Write(Float(0x3a83126f));Sign7A8350.Write(0x80000000);}
 }
 public sealed class GameRotationPublicationContext {
  readonly Dictionary<GameMovementCharacterState,GameRotationState> states=new Dictionary<GameMovementCharacterState,GameRotationState>();
  public readonly GameMovementCallbackContext Callbacks;
  public readonly WorldSameRegionPublicationContext Publication;
  public readonly SystemUcrtRotationMath Platform;
  public readonly GameRotationConstants Constants=new GameRotationConstants();
  public GameRotationPublicationContext(GameMovementCallbackContext callbacks,WorldSameRegionPublicationContext publication,SystemUcrtRotationMath platform){
   if(callbacks==null||publication==null||platform==null)throw new ArgumentNullException("rotation context");
   if(!Object.ReferenceEquals(callbacks.Entities,publication.Bounds.Entities)||!Object.ReferenceEquals(callbacks.World.State,publication.Regions.Coordinates))throw new ArgumentException("Rotation requires the same Entity and coordinate registries");
   Callbacks=callbacks;Publication=publication;Platform=platform;
  }
  public GameRotationState Bind(GameMovementCharacterState character){
   if(character==null)throw new ArgumentNullException("character");Callbacks.Owner(character.Interface38,0);
   GameRotationState state;if(!states.TryGetValue(character,out state)){state=new GameRotationState(character);states.Add(character,state);}return state;
  }
  internal void RequireBound(GameRotationState state,uint at){GameRotationState found;if(state==null||!states.TryGetValue(state.Character,out found)||!Object.ReferenceEquals(state,found))throw new MissingContract(at,"Rotation has no unique shared Character binding");Callbacks.Owner(state.Character.Interface38,at).Entity.RequireLive(at);}
 }
 public sealed class GameRotationInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly WorldSameRegionPublicationInvocation Publication=new WorldSameRegionPublicationInvocation();
  readonly List<uint> steps=new List<uint>();
  public EntityCoordinates64 InitialLocal {get;internal set;}
  public EntityCoordinates64 PublicationInput {get;internal set;}
  public GameMovementMatrix48 AxisMatrix {get;internal set;}
  public GameMovementMatrix48 ResultMatrix {get;internal set;}
  public uint[] ObservedAddresses{get{return steps.ToArray();}}
  internal void At(uint at){Native.At=at;steps.Add(at);}
 }
 // Real591A10 instruction order; own fixed UCRT platform adapter is explicit.
 // Domain is separate live typed local buffers, ordinary masked binary32.
 // No shifted aliases, concurrent writes, native ABI/EH or FP traps claimed.
 public static class GameRotationPublicationContracts {
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static Field<float> V(float x){return Field<float>.Of(x);}
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null own rotation dependency");return x;}
  static Field<float> P(NavigationPoint3 p,int n,uint at){Need(p,at).RequireLive(at);try{return n==0?p.X:n==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,"Point outside declared own extent",e);}}
  static Field<float> L(Field<float> x,uint at,Invocation i){i.At=at;return x.Snapshot();}
  static Field<float> Mul(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return V(F(x*y));}
  static Field<float> Add(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return V(F(x+y));}
  static Field<float> Sub(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return V(F(x-y));}
  static Field<float> Sqrt(Field<float> x,uint at,Invocation i){i.At=at;return V(F((float)Math.Sqrt(x.Read(at))));}
  static void W(GameMovementMatrix48 o,int n,Field<float> x,uint at,Invocation i){i.At=at;o.At(n,at).CopyFrom(x);}
  static void W2(GameMovementMatrix48 o,int n,Field<float> x,Field<float> y,uint at,Invocation i){var a=x.Snapshot();var b=y.Snapshot();W(o,n,a,at,i);W(o,n+1,b,at,i);}
  public static GameMovementMatrix48 AxisRotation591A10(GameMovementMatrix48 output,NavigationPoint3 axis,float angle,GameMovementMathConstants constants,SystemUcrtRotationMath platform,Invocation invocation){
   var i=Need(invocation,0x591a10);i.At=0x591a10;Need(output,i.At).RequireLive(i.At);Need(axis,i.At).RequireLive(i.At);Need(constants,i.At);Need(platform,i.At);
   for(int n=0;n<12;n++)for(int k=0;k<3;k++)if(Object.ReferenceEquals(output.At(n,i.At),P(axis,k,i.At)))throw new MissingContract(i.At,"Output/input overlap outside observed separate locals");
   i.At=0x591a30;var cosine=V(platform.Cos(angle,i.At));i.At=0x591a3b;var sine=V(platform.Sin(angle,i.At));
   var x1=L(P(axis,0,0x591a40),0x591a40,i);var x3=sine.Snapshot();var x4=L(constants.One7A8054,0x591a47,i);
   x4=Sub(x4,cosine,0x591a54,i);x1=Mul(x1,x1,0x591a58,i);x1=Mul(x1,x4,0x591a68,i);x1=Add(x1,cosine,0x591a6c,i);W(output,0,x1,0x591a70,i);
   var x0=Mul(sine,P(axis,2,0x591a74),0x591a74,i);x1=L(P(axis,1,0x591a79),0x591a79,i);x1=Mul(x1,P(axis,0,0x591a7e),0x591a7e,i);x1=Mul(x1,x4,0x591a82,i);x1=Add(x1,x0,0x591a86,i);W(output,1,x1,0x591a8d,i);
   x0=Mul(x3,P(axis,1,0x591a92),0x591a92,i);var x2=L(P(axis,2,0x591a97),0x591a97,i);x2=Mul(x2,P(axis,0,0x591a9c),0x591a9c,i);x2=Mul(x2,x4,0x591aa0,i);x2=Sub(x2,x0,0x591aa4,i);W(output,2,x2,0x591aab,i);
   x0=Mul(x3,P(axis,2,0x591ab0),0x591ab0,i);x1=L(P(axis,1,0x591ab5),0x591ab5,i);x1=Mul(x1,P(axis,0,0x591aba),0x591aba,i);x1=Mul(x1,x4,0x591abe,i);x1=Sub(x1,x0,0x591ac2,i);W(output,3,x1,0x591ac9,i);
   x1=L(P(axis,1,0x591ace),0x591ace,i);x1=Mul(x1,x1,0x591ad3,i);x1=Mul(x1,x4,0x591ad7,i);x1=Add(x1,cosine,0x591adb,i);W(output,4,x1,0x591adf,i);
   x0=Mul(x3,P(axis,0,0x591ae4),0x591ae4,i);x2=L(P(axis,2,0x591ae8),0x591ae8,i);x2=Mul(x2,P(axis,1,0x591aed),0x591aed,i);x2=Mul(x2,x4,0x591af2,i);x2=Add(x2,x0,0x591af6,i);W(output,5,x2,0x591afd,i);
   x0=Mul(x3,P(axis,1,0x591b02),0x591b02,i);x1=L(P(axis,2,0x591b07),0x591b07,i);x1=Mul(x1,P(axis,0,0x591b0c),0x591b0c,i);x1=Mul(x1,x4,0x591b10,i);x1=Add(x1,x0,0x591b14,i);W(output,6,x1,0x591b18,i);
   x2=L(P(axis,2,0x591b20),0x591b20,i);x2=Mul(x2,P(axis,1,0x591b25),0x591b25,i);x3=Mul(x3,P(axis,0,0x591b2a),0x591b2a,i);x2=Mul(x2,x4,0x591b2e,i);x2=Sub(x2,x3,0x591b32,i);W(output,7,x2,0x591b36,i);
   x0=L(P(axis,2,0x591b3b),0x591b3b,i);x0=Mul(x0,x0,0x591b45,i);x0=Mul(x0,x4,0x591b49,i);x0=Add(x0,cosine,0x591b4d,i);W(output,8,x0,0x591b56,i);W2(output,9,V(0),V(0),0x591b61,i);W(output,11,V(0),0x591b66,i);i.At=0x591b69;return output;
  }
  static bool ZeroOrUnordered(Field<float> x,uint at,Invocation i){i.At=at;float v=x.Read(at);return v==0||Single.IsNaN(v);}
  static Field<float> MinMax(Field<float> a,Field<float> b,bool max,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return V(max?(x>y?x:y):(x<y?x:y));}
  sealed class Position16 {public Field<ulong> Q;public Field<uint> X,Y;}
  sealed class Position8 {public Field<uint> Z,Padding;}
  // WorldPosition24 has no release/lifetime API: its caller supplies a stable
  // valid own typed view. Do not invent a native lifetime check or new API.
  static Position16 Capture16(WorldPosition24 p,uint at,GameRotationInvocation c){c.At(at);Need(p,at);return new Position16{Q=p.Q0.Snapshot(),X=p.D8.Snapshot(),Y=p.DC.Snapshot()};}
  static Position8 Capture8(WorldPosition24 p,uint at,GameRotationInvocation c){c.At(at);Need(p,at);return new Position8{Z=p.D10.Snapshot(),Padding=p.D14.Snapshot()};}
  static void Store16(EntityCoordinates64 d,Position16 p,uint at,GameRotationInvocation c){c.At(at);d.RequireLive(at);d.Position00.Q0.CopyFrom(p.Q);d.Position00.D8.CopyFrom(p.X);d.Position00.DC.CopyFrom(p.Y);}
  static void Store8(EntityCoordinates64 d,Position8 p,uint at,GameRotationInvocation c){c.At(at);d.RequireLive(at);d.Position00.D10.CopyFrom(p.Z);d.Position00.D14.CopyFrom(p.Padding);}
  static Field<Field<WorldEngine>> Import(GameRotationPublicationContext ctx,uint at,GameRotationInvocation c){c.At(at);return ctx.Publication.Gates.EngineImport609050.Snapshot();}
  static void Publish(Field<Field<WorldEngine>> import,uint deref,uint world,uint call,GameRotationState state,EntityCoordinates64 frame,GameRotationPublicationContext ctx,GameRotationInvocation c){
   c.PublicationInput=frame;c.At(deref);var cell=Need(import.Read(deref),deref);
   if(!Object.ReferenceEquals(cell,ctx.Callbacks.World.EngineGlobal))throw new MissingContract(deref,"Imported gEngine cell is not the shared callback global");
   var captured=cell.Snapshot();c.At(world);var engine=Need(captured.Read(world),world);var borrowed=engine.World50.Snapshot();
   // World21BB40 never consumes its this pointer. Transfer the captured word
   // without reading an Unknown World50 into an artificial earlier failure.
   c.At(call);try{WorldSameRegionPublicationContracts.SetCoords21BB40(borrowed,state.Character.Entity,frame,ctx.Publication,c.Publication);}finally{c.Native.At=c.Publication.Native.At;}
  }
  static byte Direct(bool longArm,GameRotationState state,WorldPosition24 destination,EntityCoordinates64 frame,GameRotationPublicationContext ctx,GameRotationInvocation c){
   uint a=longArm?0x05e57eu:0x05e5b4u;var p=Capture16(destination,a,c);var imp=Import(ctx,a+3,c);var q=Capture8(destination,a+15,c);Store16(frame,p,a+23,c);Store8(frame,q,a+28,c);
   Publish(imp,a+34,a+37,a+41,state,frame,ctx,c);c.At(a+47);return (byte)(longArm?1:0);
  }
  static Field<float>[] MatrixCapture(GameMovementMatrix48 matrix,int start,int count,uint at,GameRotationInvocation c){c.At(at);var a=new Field<float>[count];for(int n=0;n<count;n++)a[n]=matrix.At(start+n,at).Snapshot();return a;}
  static void BasisWrite(EntityCoordinates64 frame,int start,Field<float>[] values,uint at,GameRotationInvocation c){c.At(at);frame.RequireLive(at);for(int n=0;n<values.Length;n++)frame.Basis18[start+n].CopyFrom(values[n]);}
  // No blanket finite-input gate: evidenced UCOMISS unordered arms (including
  // a vertical direction's zero horizontal normalization) retain their actual
  // direct publication branch. UCRT finite-domain checks apply only when a
  // real trig call is reached. NaN payload/FP status/traps are not promised.
  // The returned AL describes the native branch, not publication success.
  public static byte RotateTowards05E160(GameRotationState state,int ticks,NavigationPoint3 direction,WorldPosition24 destination,GameRotationPublicationContext context,GameRotationInvocation invocation){
   var c=Need(invocation,0x05e160);c.At(0x05e160);Need(context,c.Native.At).RequireBound(state,c.Native.At);var i=c.Native;
   c.At(0x05e196);var local=EntityMovementContracts.GetCoords07F070(state.Character.Entity,new EntityCoordinates64(),i);c.InitialLocal=local;
   var y=L(P(direction,1,0x05e19c),0x05e19c,i);var x=L(P(direction,0,0x05e1a5),0x05e1a5,i);var z=L(P(direction,2,0x05e1a9),0x05e1a9,i);
   x=Mul(x,x,0x05e1ae,i);y=Mul(y,y,0x05e1b2,i);z=Mul(z,z,0x05e1b6,i);var squared=Add(x,y,0x05e1ba,i);squared=Add(squared,z,0x05e1be,i);
   if(ZeroOrUnordered(squared,0x05e1c2,i))return Direct(false,state,destination,local,context,c);
   var len=Sqrt(squared,0x05e1cf,i);i.At=0x05e1d3;float lv=len.Read(i.At),minimum=context.Constants.Minimum7A7FFC.Read(i.At);
   if(Single.IsNaN(lv)||Single.IsNaN(minimum)||lv<minimum)return Direct(false,state,destination,local,context,c);
   i.At=0x05e1e0;byte skip=state.Flag26B0.Read(i.At);var px=L(P(direction,0,0x05e1e7),0x05e1e7,i);L(P(direction,1,0x05e1e7),0x05e1e7,i);var pz=L(P(direction,2,0x05e1eb),0x05e1eb,i);var one=L(context.Constants.Matrix.One7A8054,0x05e1ee,i);
   var xx=Mul(px,px,0x05e216,i);var zz=Mul(pz,pz,0x05e232,i);i.At=0x05e23c;var tick=V(F((float)ticks));var horizontal=Add(xx,zz,0x05e23f,i);horizontal=Sqrt(horizontal,0x05e246,i);
   i.At=0x05e24a;var inv=V(F(one.Read(i.At)/horizontal.Read(i.At)));var nx=Mul(px,inv,0x05e252,i);var ny=Mul(inv,V(0),0x05e256,i);var nz=Mul(pz,inv,0x05e25b,i);
   var normalized=new NavigationPoint3();i.At=0x05e260;normalized.Y.CopyFrom(ny);i.At=0x05e266;normalized.X.CopyFrom(nx);i.At=0x05e26b;normalized.Z.CopyFrom(nz);
   if(skip!=0)return Direct(true,state,destination,local,context,c);
   y=Mul(ny,ny,0x05e27e,i);x=Mul(nx,nx,0x05e287,i);z=Mul(nz,nz,0x05e28b,i);squared=Add(y,x,0x05e290,i);squared=Add(squared,z,0x05e294,i);
   if(ZeroOrUnordered(squared,0x05e298,i)||ZeroOrUnordered(Sqrt(squared,0x05e2a5,i),0x05e2a9,i))return Direct(true,state,destination,local,context,c);
   var b=local.Basis18;y=L(b[7],0x05e2b3,i);z=L(b[8],0x05e2b9,i);x=L(b[6],0x05e2bf,i);y=Mul(y,ny,0x05e2c5,i);
   var rate=L(state.Rate45B0,0x05e2d2,i);var other=rate.Snapshot();z=Mul(z,nz,0x05e2dd,i);rate=Mul(rate,state.Rate45A8,0x05e2e2,i);other=Mul(other,state.Rate45AC,0x05e2ea,i);x=Mul(x,nx,0x05e2f2,i);rate=Sub(rate,other,0x05e2f6,i);
   var dot=Add(y,x,0x05e2fa,i);dot=Add(dot,z,0x05e2fe,i);dot=MinMax(dot,context.Constants.MinusOne7A81AC,true,0x05e302,i);dot=MinMax(dot,one,false,0x05e30a,i);
   var factor=Add(dot,one,0x05e314,i);factor=Mul(factor,context.Constants.Half7A8024,0x05e318,i);rate=Mul(rate,factor,0x05e320,i);rate=Add(rate,other,0x05e324,i);rate=Mul(rate,tick,0x05e328,i);rate=Mul(rate,context.Constants.Scale7A7FA4,0x05e32c,i);
   c.At(0x05e334);float angle=context.Platform.Acos(dot.Read(i.At),i.At);i.At=0x05e339;float step=rate.Read(i.At);
   if(!Single.IsNaN(step)&&angle>=step)return Partial(state,destination,local,nx,ny,nz,rate,context,c);
   var origin=new NavigationPoint3();origin.X.Write(0);origin.Y.Write(0);origin.Z.Write(0);var up=new NavigationPoint3();up.X.Write(0);up.Y.Write(1);up.Z.Write(0);
   c.At(0x05e376);var matrix=GameMovementMathContracts.Basis591B80(new GameMovementMatrix48(),origin,normalized,up,context.Constants.Matrix,i);c.ResultMatrix=matrix;
   c.At(0x05e37f);var frame=WorldPublicationPrerequisites.ConstructWorldCoords228AB0(new EntityCoordinates64(),i);
   var p=Capture16(destination,0x05e385,c);var m2=MatrixCapture(matrix,2,1,0x05e388,c);var q=Capture8(destination,0x05e38f,c);Store16(frame,p,0x05e397,c);var m01=MatrixCapture(matrix,0,2,0x05e39b,c);BasisWrite(frame,2,m2,0x05e3a0,c);
   var m5=MatrixCapture(matrix,5,1,0x05e3a3,c);BasisWrite(frame,5,m5,0x05e3a6,c);var m8=MatrixCapture(matrix,8,1,0x05e3a9,c);BasisWrite(frame,0,m01,0x05e3ac,c);var m34=MatrixCapture(matrix,3,2,0x05e3b1,c);BasisWrite(frame,8,m8,0x05e3b6,c);
   var imp=Import(context,0x05e3b9,c);BasisWrite(frame,3,m34,0x05e3c0,c);var m67=MatrixCapture(matrix,6,2,0x05e3c5,c);BasisWrite(frame,6,m67,0x05e3ca,c);Store8(frame,q,0x05e3cf,c);Publish(imp,0x05e3d4,0x05e3d7,0x05e3db,state,frame,context,c);c.At(0x05e3e1);return 1;
  }
  static byte Partial(GameRotationState state,WorldPosition24 destination,EntityCoordinates64 local,Field<float> nx,Field<float> ny,Field<float> nz,Field<float> step,GameRotationPublicationContext ctx,GameRotationInvocation c){
   var i=c.Native;var b=local.Basis18;var y=L(b[1],0x05e427,i);var x=L(b[0],0x05e42d,i);var z=L(b[2],0x05e433,i);y=Mul(y,ny,0x05e439,i);x=Mul(x,nx,0x05e43e,i);z=Mul(z,nz,0x05e442,i);var side=Add(y,x,0x05e447,i);side=Add(side,z,0x05e44b,i);i.At=0x05e44f;float value=side.Read(i.At);
   if(Single.IsNaN(value)||value<0){i.At=0x05e455;uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(step.Read(i.At)),0)^ctx.Constants.Sign7A8350.Read(i.At);step=V(BitConverter.ToSingle(BitConverter.GetBytes(bits),0));}
   var axis=new NavigationPoint3();axis.X.Write(0);axis.Y.Write(1);axis.Z.Write(0);c.At(0x05e479);var rotation=new GameMovementMatrix48();c.AxisMatrix=rotation;AxisRotation591A10(rotation,axis,step.Read(i.At),ctx.Constants.Matrix,ctx.Platform,i);
   var old=new GameMovementMatrix48();var p0=L(b[0],0x05e47e,i);var p1=L(b[1],0x05e47e,i);var p2=L(b[2],0x05e488,i);W2(old,0,p0,p1,0x05e490,i);
   var p3=L(b[3],0x05e499,i);var p4=L(b[4],0x05e499,i);W2(old,3,p3,p4,0x05e49f,i);var p6=L(b[6],0x05e4a4,i);var p7=L(b[7],0x05e4a4,i);W(old,2,p2,0x05e4aa,i);var p5=L(b[5],0x05e4ad,i);W2(old,6,p6,p7,0x05e4b1,i);W(old,5,p5,0x05e4ba,i);var p8=L(b[8],0x05e4bd,i);W(old,8,p8,0x05e4c5,i);W2(old,9,V(0),V(0),0x05e4d4,i);W(old,11,V(0),0x05e4d9,i);
   c.At(0x05e4dc);var result=GameMovementMathContracts.Compose07E6F0(new GameMovementMatrix48(),old,rotation,i);c.ResultMatrix=result;
   var p=Capture16(destination,0x05e4e1,c);var q=Capture8(destination,0x05e4ec,c);var r4=MatrixCapture(result,4,4,0x05e4f1,c);var r0=MatrixCapture(result,0,4,0x05e4f5,c);var r8=MatrixCapture(result,8,4,0x05e4f8,c);var imp=Import(ctx,0x05e4fc,c);
   Store16(local,p,0x05e503,c);BasisWrite(local,1,new[]{r0[1]},0x05e50f,c);Store8(local,q,0x05e515,c);BasisWrite(local,0,new[]{r0[0]},0x05e51f,c);
   // The native two local16B stores and overlapping8B reload pick D3,D4.
   c.At(0x05e525);for(int n=0;n<4;n++)old.At(n,i.At).CopyFrom(r0[n]);c.At(0x05e529);for(int n=0;n<4;n++)old.At(4+n,i.At).CopyFrom(r4[n]);var r34=MatrixCapture(old,3,2,0x05e52d,c);BasisWrite(local,3,r34,0x05e532,c);
   BasisWrite(local,6,new[]{r4[2]},0x05e53f,c);BasisWrite(local,8,new[]{r8[0]},0x05e545,c);BasisWrite(local,7,new[]{r4[3]},0x05e54f,c);BasisWrite(local,2,new[]{r0[2]},0x05e559,c);BasisWrite(local,5,new[]{r4[1]},0x05e564,c);
   Publish(imp,0x05e56a,0x05e56d,0x05e571,state,local,ctx,c);c.At(0x05e577);return 0;
  }
 }
}
