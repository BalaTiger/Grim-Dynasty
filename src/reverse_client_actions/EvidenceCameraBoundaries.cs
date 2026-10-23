using System;
using System.Runtime.CompilerServices;
namespace GrimDynasty.ReverseClient.Actions {
 // Own adapter for the evidenced GetCoords -> Game GetTarget chain and the
 // portal caller's basis-only consumption. This neither constructs a native
 // camera nor initializes its position, offsets, count, basis or vptr.
 public class EvidenceCameraBoundaries:PortalCameraBoundaries {
  public readonly CameraTargetBoundaries Target;
  readonly ConditionalWeakTable<CameraCoordinates32,CameraPosition24> targets=new ConditionalWeakTable<CameraCoordinates32,CameraPosition24>();
  readonly ConditionalWeakTable<CoordinateFrame60,NativeCameraFrame60> frames=new ConditionalWeakTable<CoordinateFrame60,NativeCameraFrame60>();
  public EvidenceCameraBoundaries(MoveContext move,PortalContext portal,CameraTargetBoundaries target):base(move,portal){
   if(target==null)throw new ArgumentNullException("target");Target=target;
  }
  public void AttachTarget(CameraTargetState state){Target.Attach(state);}
  // Bind the observed concrete GameCamera table, explicitly supplied by the
  // caller. No table is installed on a camera and no loaded binding is claimed.
  public void BindObservedGameCamera(PortalCameraVTable table){
   if(table==null)throw new ArgumentNullException("table");
   BindObservedSlot18(table,ReadGameTarget);
   table.Slot10.Write(delegate(PortalCamera camera,float angle){PortalCameraContracts.GameSetYaw(camera,angle,this,new Invocation());});
  }
  CameraCoordinates32 ReadGameTarget(PortalCamera camera,CameraCoordinates32 hidden){
   if(hidden==null)throw new MissingContract(0,"Null own GetCoords hidden view");
   CameraPosition24 raw;
   if(!targets.TryGetValue(hidden,out raw)){raw=new CameraPosition24(hidden,hidden.Q0);targets.Add(hidden,raw);}
   CameraTargetContracts.GetTarget(Target.State(camera),raw,Target,new Invocation());
   // Engine229160 consumes only this returned Q0, which shares the raw output.
   // Q8/Q16 on the legacy coarse carrier are not a full-position projection;
   // their per-DWORD information remains accessible only in RawTargetFor.
   return hidden;
  }
  public CameraPosition24 RawTargetFor(CameraCoordinates32 hidden){
   CameraPosition24 raw;
   if(hidden==null||!targets.TryGetValue(hidden,out raw))throw new MissingContract(0,"No raw target associated with this hidden view");
   return raw;
  }
  public NativeCameraFrame60 RawFrameFor(CoordinateFrame60 output){
   NativeCameraFrame60 raw;
   if(output==null||!frames.TryGetValue(output,out raw))throw new MissingContract(0,"No raw camera frame associated with this portal view");
   return raw;
  }
  public override CoordinateFrame60 CameraFrame609F00(PortalCamera camera,CoordinateFrame60 output){
   if(output==null)throw new MissingContract(0,"Null own portal basis bridge");
   NativeCameraFrame60 raw;
   if(!frames.TryGetValue(output,out raw)){raw=new NativeCameraFrame60(output.Basis18);frames.Add(output,raw);}
   // All native GetCoords writes execute, including target production, before
   // the caller reads basis[6..8]. D14 remains unobserved and is never defaulted.
   PortalCameraContracts.GetCoords(camera,raw,this,new Invocation());
   // Position00 is not a general full-frame view. The exact raw output above
   // is authoritative; the only supported consumer here is portal basis math.
   return output;
  }
 }
}
