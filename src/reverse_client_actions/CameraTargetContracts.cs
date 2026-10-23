using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 // Own 24-byte view. Q0 and four independent DWORD cells preserve an unknown
 // padding DWORD at +14. Only independent or completely same-address views
 // are supported; partial/shifted and uint/float overlap is not emulated.
 public sealed class CameraPosition24 {
  sealed class AliasDeclaration { public bool Unsupported; }
  readonly AliasDeclaration alias;
  public readonly object AddressIdentity;
  public readonly Field<ulong> Q0;
  public readonly Field<uint> D8,DC,D10,D14;
  public CameraPosition24():this(new object(),new Field<ulong>()){}
  // The injected Q0 can be the exact hidden-return Q0 cell. The caller keeps
  // one DWORD sidecar per hidden address; it must not fabricate complete Q8/Q16.
  public CameraPosition24(Field<ulong> sharedQ0):this(sharedQ0,sharedQ0){}
  public CameraPosition24(object addressIdentity,Field<ulong> sharedQ0){
   if(addressIdentity==null)throw new ArgumentNullException("addressIdentity");
   if(sharedQ0==null)throw new ArgumentNullException("sharedQ0");
   AddressIdentity=addressIdentity;Q0=sharedQ0;
   D8=new Field<uint>();DC=new Field<uint>();D10=new Field<uint>();D14=new Field<uint>();alias=new AliasDeclaration();
  }
  public CameraPosition24(CameraPosition24 sameAddress){
   if(sameAddress==null)throw new ArgumentNullException("sameAddress");
   AddressIdentity=sameAddress.AddressIdentity;Q0=sameAddress.Q0;
   D8=sameAddress.D8;DC=sameAddress.DC;D10=sameAddress.D10;D14=sameAddress.D14;alias=sameAddress.alias;
  }
  public void DeclareUnsupportedOverlap(){alias.Unsupported=true;}
  public void RequireSupportedAlias(){
   if(alias.Unsupported)throw new MissingContract(0,"Unsupported own position view: partial, shifted or cross-type overlap; not a native exception");
  }
  internal bool SharesAllCells(CameraPosition24 other){
   return Object.ReferenceEquals(Q0,other.Q0)&&Object.ReferenceEquals(D8,other.D8)&&Object.ReferenceEquals(DC,other.DC)&&Object.ReferenceEquals(D10,other.D10)&&Object.ReferenceEquals(D14,other.D14);
  }
  internal bool SharesAnyCell(CameraPosition24 other){
   return Object.ReferenceEquals(Q0,other.Q0)||Object.ReferenceEquals(D8,other.D8)||Object.ReferenceEquals(DC,other.DC)||Object.ReferenceEquals(D10,other.D10)||Object.ReferenceEquals(D14,other.D14);
  }
 }
 public sealed class CameraVector3 {
  public readonly Field<float> X=new Field<float>(),Y=new Field<float>(),Z=new Field<float>();
  bool unsupportedOverlap;
  public void DeclareUnsupportedOverlap(){unsupportedOverlap=true;}
  public void RequireSupportedAlias(){if(unsupportedOverlap)throw new MissingContract(0,"Unsupported own vector view: position/vector cross-type overlap; not a native exception");}
 }
 public sealed class CameraTargetState {
  public readonly PortalCamera Identity;
  public readonly CameraPosition24 Target120;
  public readonly CameraVector3 Offset55C=new CameraVector3(),Offset60=new CameraVector3();
  public readonly Field<int> Count138=new Field<int>();
  public CameraTargetState(PortalCamera identity):this(identity,new CameraPosition24()){}
  public CameraTargetState(PortalCamera identity,CameraPosition24 target120){
   if(identity==null)throw new ArgumentNullException("identity");
   if(target120==null)throw new ArgumentNullException("target120");
   Identity=identity;Target120=target120;
  }
 }
 public class CameraTargetBoundaries {
  readonly Dictionary<PortalCamera,CameraTargetState> states=new Dictionary<PortalCamera,CameraTargetState>();
  // Selected Game image data at7A8054 was read as3F800000. This is an explicit
  // observed-data input, not a default region/coordinate or loaded-image claim.
  public readonly Field<float> UnitY7A8054=Field<float>.Of(1.0f);
  public void Attach(CameraTargetState state){
   if(state==null)throw new ArgumentNullException("state");
   CameraTargetState previous;
   if(states.TryGetValue(state.Identity,out previous)&&!Object.ReferenceEquals(previous,state))throw new InvalidOperationException("Camera identity already has a different target state view");
   states[state.Identity]=state;
  }
  public CameraTargetState State(PortalCamera camera){
   CameraTargetState state;
   if(camera==null||!states.TryGetValue(camera,out state))throw new MissingContract(0,"Own camera target state view has not been explicitly attached; no native pointer failure is inferred");
   return state;
  }
  internal void RequireCompatibleRegisteredState(CameraTargetState state){
   CameraTargetState registered;
   if(states.TryGetValue(state.Identity,out registered)&&!Object.ReferenceEquals(registered,state))throw new MissingContract(0,"Own supplied camera view differs from the registered identity view");
  }
  // Engine22C0B0: a Q0 load only. Do not resolve or manufacture a region object.
  public virtual ulong GetRegion6094A0(CameraPosition24 position){
   if(position==null)throw new MissingContract(0x22c0b0,"Null position at region Q0 load");
   position.RequireSupportedAlias();return position.Q0.Read(0x22c0b0);
  }
  // Engine22C5D0. The operands are read in instruction order, with each store
  // preceding the next component's reads. Native MXCSR flags/traps, FTZ/DAZ,
  // NaN payload selection and cross-type memory aliases remain outside scope.
  public virtual void Translate609B70(CameraPosition24 position,CameraVector3 vector){
   if(position==null)throw new MissingContract(0x22c5d0,"Null translation position");
   position.RequireSupportedAlias();if(vector!=null)vector.RequireSupportedAlias();
   float old=CameraTargetContracts.FloatFromBits(position.D8.Read(0x22c5d0));
   if(vector==null)throw new MissingContract(0x22c5d5,"Null vector at X operand read");
   float delta=vector.X.Read(0x22c5d5);
   position.D8.Write(CameraTargetContracts.BitsFromFloat(CameraTargetContracts.F32(old+delta)));
   delta=vector.Y.Read(0x22c5de);old=CameraTargetContracts.FloatFromBits(position.DC.Read(0x22c5e3));
   position.DC.Write(CameraTargetContracts.BitsFromFloat(CameraTargetContracts.F32(delta+old)));
   delta=vector.Z.Read(0x22c5ed);old=CameraTargetContracts.FloatFromBits(position.D10.Read(0x22c5f2));
   position.D10.Write(CameraTargetContracts.BitsFromFloat(CameraTargetContracts.F32(delta+old)));
  }
  // Engine0716E0 returns this+60 without reading the vector. Registry lookup
  // only maps that inline identity into an own view; it never initializes it.
  public virtual CameraVector3 GetCameraOffset60A690(PortalCamera camera){return State(camera).Offset60;}
 }
 public static class CameraTargetContracts {
  public static float FloatFromBits(uint value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  public static uint BitsFromFloat(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
  public static float F32(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static CameraPosition24 Need(CameraPosition24 value,uint at){
   if(value==null)throw new MissingContract(at,"Null position view");value.RequireSupportedAlias();return value;
  }
  static void CheckPair(CameraPosition24 a,CameraPosition24 b){
   if(a!=null)a.RequireSupportedAlias();if(b!=null)b.RequireSupportedAlias();
   if(a==null||b==null)return;
   if((Object.ReferenceEquals(a.AddressIdentity,b.AddressIdentity)||a.SharesAnyCell(b))&&!a.SharesAllCells(b))throw new MissingContract(0,"Own positions with a shared address or cells must share every cell, not just a Q0 prefix");
  }
  public static CameraPosition24 GetTarget(CameraTargetState state,CameraPosition24 output,CameraTargetBoundaries b,Invocation i){
   if(b==null)throw new ArgumentNullException("b");if(i==null)throw new ArgumentNullException("i");
   // Domain declaration only: do not present an unsupported own alias layout
   // as a failure of the original function or partially execute such a layout.
   CheckPair(state==null?null:state.Target120,output);
   CameraTargetState original=state;CameraPosition24 destination=output;
   i.At=0x2be6f8;
   // Game609720 -> Engine228AB0: only Q0 is written. Return value ignored.
   i.At=0x228ab0;Need(destination,i.At).Q0.Write(0);
   i.At=0x2be6fe;
   if(original==null)throw new MissingContract(i.At,"Unavailable camera inline target address");
   CameraPosition24 source=original.Target120;
   i.At=0x2be705;ulong firstRegion=b.GetRegion6094A0(source);
   i.At=0x2be70b;CheckPair(source,destination);
   var q=source.Q0.Snapshot();var d8=source.D8.Snapshot();var dc=source.DC.Snapshot();
   Field<uint> d10,d14;
   if(firstRegion!=0){
    i.At=0x2be717;d10=source.D10.Snapshot();d14=source.D14.Snapshot();
    var local=new CameraPosition24();var vertical=new CameraVector3();
    i.At=0x2be724;local.D10.CopyFrom(d10);local.D14.CopyFrom(d14);
    i.At=0x2be72f;float unitY=b.UnitY7A8054.Read(i.At);
    i.At=0x2be737;vertical.Y.Write(unitY);
    i.At=0x2be73d;local.Q0.CopyFrom(q);local.D8.CopyFrom(d8);local.DC.CopyFrom(dc);
    i.At=0x2be742;vertical.X.Write(0.0f);
    i.At=0x2be74a;vertical.Z.Write(0.0f);
    i.At=0x2be752;b.Translate609B70(local,vertical);
    i.At=0x2be764;b.Translate609B70(local,original.Offset55C);
    i.At=0x2be76a;Need(local,i.At);q=local.Q0.Snapshot();d8=local.D8.Snapshot();dc=local.DC.Snapshot();
    i.At=0x2be76f;d10=local.D10.Snapshot();d14=local.D14.Snapshot();
    i.At=0x2be775;Need(destination,i.At);destination.Q0.CopyFrom(q);destination.D8.CopyFrom(d8);destination.DC.CopyFrom(dc);
   }else{
    i.At=0x2be77a;Need(destination,i.At);destination.Q0.CopyFrom(q);destination.D8.CopyFrom(d8);destination.DC.CopyFrom(dc);
    // This is a separate late load AFTER the destination's first16-byte store.
    i.At=0x2be77d;Need(source,i.At);d10=source.D10.Snapshot();d14=source.D14.Snapshot();
   }
   i.At=0x2be788;destination.D10.CopyFrom(d10);destination.D14.CopyFrom(d14);
   i.At=0x2be78d;ulong secondRegion=b.GetRegion6094A0(destination);
   if(secondRegion!=0){
    i.At=0x2be798;int count=original.Count138.Read(i.At);
    if(count>0){
     i.At=0x2be7a4;b.RequireCompatibleRegisteredState(original);
     CameraVector3 offset=b.GetCameraOffset60A690(original.Identity);
     // The returned pointer is passed verbatim; no null-as-zero shortcut.
     i.At=0x2be7b0;b.Translate609B70(destination,offset);
    }
   }
   i.At=0x2be7bb;return destination;
  }
 }
}
