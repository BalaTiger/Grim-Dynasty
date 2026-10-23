using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Full observed coordinate frame: 24-byte position and three 12-byte vectors.
 // No default identity rotation, zero orientation or native object is implied.
 public sealed class CoordinateFrame60 {
  public readonly WorldPosition Position00=new WorldPosition();
  public readonly Field<float>[] Basis18=new Field<float>[9];
  public CoordinateFrame60(){for(int n=0;n<9;n++)Basis18[n]=new Field<float>();}
 }
 public delegate void CameraAngleSetter(PortalCamera camera,float angle);
 public sealed class PortalCameraVTable { public readonly Field<CameraAngleSetter> Slot10=new Field<CameraAngleSetter>(); }
 public sealed class PortalCamera {
  public readonly Field<PortalCameraVTable> Virtual00=new Field<PortalCameraVTable>();
  public readonly Field<float> Yaw59C=new Field<float>();
 }
 public sealed class PortalGameEngine {
  // Inline identity, not an eagerly-created native camera or initialized vptr.
  public readonly PortalCamera Camera13B0=new PortalCamera();
  public readonly Field<object> Connections40F8=new Field<object>();
 }
 public sealed class PlayerConnection { public readonly Field<uint> PlayerId10=new Field<uint>(); }
 public sealed class PortalContext {
  public readonly Field<PortalGameEngine> GameGlobalA66288=new Field<PortalGameEngine>();
  public readonly AddressSymbol<ClassRecord> PlayerClassA5CAB0=new AddressSymbol<ClassRecord>(0xa5cab0);
  public readonly Field<float> Pi7A80BC=new Field<float>();
 }
 public class PortalTransitionBoundaries:WaypointBoundaries {
  public readonly PortalContext PortalContext;
  public PortalTransitionBoundaries(MoveContext move,PortalContext portal):base(move){PortalContext=portal;}
  public virtual object ConnectedPortal609F40(object portal){throw Missing(0x609f40,"Connected portal import");}
  public virtual void PortalMatrix609F48(object portal,Field<float>[] matrix,Field<ulong> separateScratch){throw Missing(0x609f48,"Portal matrix and separate scratch import");}
  public virtual CoordinateFrame60 OwnerFrame609518(object owner,CoordinateFrame60 hidden){throw Missing(0x609518,"Full owner coordinate frame source");}
  public virtual CoordinateFrame60 CameraFrame609F00(PortalCamera camera,CoordinateFrame60 hidden){throw Missing(0x609f00,"Actual camera frame source");}
  public virtual void SetWorldCameraYaw60A6E8(PortalCamera camera,float angle){throw Missing(0x60a6e8,"WorldCamera yaw implementation");}
  public virtual PlayerConnection Connection18(object manager){throw Missing(0x2ebb83,"Connection-manager virtual18");}
  public virtual float Atan2F5DC3F8(float y,float x){throw Missing(0x5dc3f8,"CRT atan2f; exact FP behavior not supplied");}
  public virtual void SetFrame609BB8(object world,object owner,CoordinateFrame60 frame){throw Missing(0x609bb8,"Actual full coordinate-frame write");}
  public override byte ProducePortal(WaypointManagerView view,WorldPosition destination,object portal,WorldPosition hidden,Invocation i){return PortalTransitionContracts.Produce(view,destination,portal,hidden,this,i);}
  public override byte ExecutePortal(WaypointManagerView view,Waypoint80 local,Invocation i){return PortalTransitionContracts.Execute(view,local,this,i);}
 }
}
