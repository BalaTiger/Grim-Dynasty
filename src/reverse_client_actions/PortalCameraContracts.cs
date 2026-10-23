using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 // Fine-grained own output view: Engine229160 never writes D14. Arbitrary
 // byte-overlapping native memory and object lifetime are outside this view.
 public sealed class NativeCameraFrame60 {
  public readonly Field<ulong> Q0=new Field<ulong>(),Q8=new Field<ulong>();
  public readonly Field<uint> D10=new Field<uint>(),D14=new Field<uint>();
  public readonly Field<float>[] Basis18;
  public NativeCameraFrame60():this(NewBasis()){}
  // Own limited bridge for the portal caller, which consumes only the basis.
  // Share field identities so partial native writes survive a later failure.
  // Raw position fields stay here; this does not project or initialize Q16.
  public NativeCameraFrame60(Field<float>[] sharedBasis){
   if(sharedBasis==null||sharedBasis.Length!=9)throw new ArgumentException("Nine basis fields required","sharedBasis");
   Basis18=new Field<float>[9];
   for(int n=0;n<9;n++){if(sharedBasis[n]==null)throw new ArgumentException("Null basis field","sharedBasis");Basis18[n]=sharedBasis[n];}
  }
  static Field<float>[] NewBasis(){var fields=new Field<float>[9];for(int n=0;n<9;n++)fields[n]=new Field<float>();return fields;}
 }
 public sealed class CameraCoordinates32 {
  public readonly Field<ulong> Q0=new Field<ulong>(),Q8=new Field<ulong>(),Q16=new Field<ulong>(),Q24=new Field<ulong>();
 }
 public delegate CameraCoordinates32 CameraCoordinatesGetter(PortalCamera camera,CameraCoordinates32 hidden);
 // Separate view of the newly observed Engine field. Attaching a view neither
 // initializes Yaw0C nor proves that the identity is a constructed native camera.
 public sealed class PortalCameraState {
  public readonly PortalCamera Identity;
  public readonly Field<float> Yaw0C=new Field<float>();
  public readonly Field<ulong> Position94=new Field<ulong>();
  public readonly Field<uint> Position9C=new Field<uint>();
  public readonly Field<float>[] Basis70=new Field<float>[9];
  public PortalCameraState(PortalCamera identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;for(int n=0;n<9;n++)Basis70[n]=new Field<float>();}
 }
 public class PortalCameraBoundaries:PortalTransitionBoundaries {
  readonly Dictionary<PortalCamera,PortalCameraState> cameras=new Dictionary<PortalCamera,PortalCameraState>();
  readonly Dictionary<PortalCameraVTable,Field<CameraCoordinatesGetter>> coordinateSlots=new Dictionary<PortalCameraVTable,Field<CameraCoordinatesGetter>>();
  public PortalCameraBoundaries(MoveContext move,PortalContext portal):base(move,portal){}
  // Explicit own integration operation. A second state for the same identity is
  // rejected, preventing detached copies from hiding mutations or failure prefixes.
  public void Attach(PortalCameraState state){
   if(state==null)throw new ArgumentNullException("state");
   PortalCameraState previous;
   if(cameras.TryGetValue(state.Identity,out previous)&&!Object.ReferenceEquals(previous,state))throw new InvalidOperationException("Camera identity already has a different state view");
   cameras[state.Identity]=state;
  }
  public PortalCameraState CameraState(PortalCamera camera){
   return CameraState(camera,0x0825c0);
  }
  public PortalCameraState CameraState(PortalCamera camera,uint at){
   if(camera==null)throw new MissingContract(at,"Null WorldCamera field view");
   PortalCameraState state;
   if(!cameras.TryGetValue(camera,out state))throw new MissingContract(at,"WorldCamera field view has not been explicitly attached");
   return state;
  }
  // Explicit sidecar for an already supplied table identity. No vptr is created
  // or replaced, and an unbound slot never defaults to a successful callback.
  public void BindObservedSlot18(PortalCameraVTable table,CameraCoordinatesGetter getter){
   if(table==null)throw new ArgumentNullException("table");
   Field<CameraCoordinatesGetter> slot;
   if(!coordinateSlots.TryGetValue(table,out slot)){slot=new Field<CameraCoordinatesGetter>();coordinateSlots.Add(table,slot);}
   slot.Write(getter);
  }
  public CameraCoordinatesGetter Slot18Target(PortalCameraVTable table,uint at){
   if(table==null)throw new MissingContract(at,"Null captured camera table");
   Field<CameraCoordinatesGetter> slot;
   if(!coordinateSlots.TryGetValue(table,out slot))throw new MissingContract(at,"Unobserved camera virtual slot18");
   CameraCoordinatesGetter getter=slot.Read(at);
   if(getter==null)throw new MissingContract(at,"Null camera virtual slot18 target");
   return getter;
  }
  public override void SetWorldCameraYaw60A6E8(PortalCamera camera,float angle){
   PortalCameraContracts.WorldSetYaw(CameraState(camera),angle,new Invocation());
  }
  // GetCoords intentionally inherits the unsupported boundary. Its partial
  // DWORD write cannot be projected into frozen Position00.Q16 without losing
  // independent knowledge of the untouched high DWORD at output14..17.
 }
 // Engine0825C0 and Game2BFAF0 only. No constructor or vtable is installed.
 public static class PortalCameraContracts {
  public static NativeCameraFrame60 GetCoords(PortalCamera camera,NativeCameraFrame60 output,PortalCameraBoundaries b,Invocation i){
   i.At=0x22916a;
   if(camera==null)throw new MissingContract(i.At,"Null camera at vptr load");
   PortalCameraVTable table=camera.Virtual00.Read(i.At);
   var hidden=new CameraCoordinates32();
   i.At=0x229178;CameraCoordinatesGetter getter=b.Slot18Target(table,i.At);
   CameraCoordinates32 returned=getter(camera,hidden);
   i.At=0x22917b;
   if(returned==null)throw new MissingContract(i.At,"Null slot18 returned address");
   var token=returned.Q0.Snapshot(); // Only returned Q0 is read; not hidden Q0.
   i.At=0x22917e;
   if(output==null)throw new MissingContract(i.At,"Null GetCoords output");
   output.Q0.CopyFrom(token);
   i.At=0x229181;PortalCameraState state=b.CameraState(camera,i.At);var position=state.Position94.Snapshot();
   i.At=0x229189;output.Q8.CopyFrom(position);
   i.At=0x22918e;var component=state.Position9C.Snapshot();
   i.At=0x229194;output.D10.CopyFrom(component); // D14 deliberately untouched.
   i.At=0x229197;var x=state.Basis70[0].Snapshot();var y=state.Basis70[1].Snapshot();
   i.At=0x22919c;output.Basis18[0].CopyFrom(x);output.Basis18[1].CopyFrom(y);
   i.At=0x2291a1;var z=state.Basis70[2].Snapshot();
   i.At=0x2291a4;output.Basis18[2].CopyFrom(z);
   i.At=0x2291a7;x=state.Basis70[3].Snapshot();y=state.Basis70[4].Snapshot();
   i.At=0x2291ac;output.Basis18[3].CopyFrom(x);output.Basis18[4].CopyFrom(y);
   i.At=0x2291b1;z=state.Basis70[5].Snapshot();
   i.At=0x2291b7;output.Basis18[5].CopyFrom(z);
   i.At=0x2291ba;x=state.Basis70[6].Snapshot();y=state.Basis70[7].Snapshot();
   i.At=0x2291c2;output.Basis18[6].CopyFrom(x);output.Basis18[7].CopyFrom(y);
   i.At=0x2291c7;z=state.Basis70[8].Snapshot();
   i.At=0x2291d2;output.Basis18[8].CopyFrom(z);
   i.At=0x2291d5;return output;
  }
  // Own post-processing operation, not a native instruction/failure. Run only
  // after GetCoords has completed all writes. Validate both DWORDs before any
  // frozen-view store; otherwise a mixed known/unknown Q16 is unrepresentable.
  public static CoordinateFrame60 ProjectToFrozenFrame(NativeCameraFrame60 source,CoordinateFrame60 output){
   if(source==null)throw new ArgumentNullException("source");
   if(output==null)throw new ArgumentNullException("output");
   uint low,high;
   try{low=source.D10.Read(0);high=source.D14.Read(0);}
   catch(MissingContract x){throw new MissingContract(0,"Unsupported own frame projection: both DWORD10 and untouched DWORD14 must be known; native GetCoords has already completed",x);}
   output.Position00.Q0.CopyFrom(source.Q0);output.Position00.Q8.CopyFrom(source.Q8);
   output.Position00.Q16.Write(((ulong)high<<32)|low);
   for(int n=0;n<9;n++)output.Basis18[n].CopyFrom(source.Basis18[n]);
   return output;
  }
  public static void WorldSetYaw(PortalCameraState state,float angle,Invocation i){
   i.At=0x0825c0;
   if(state==null)throw new MissingContract(i.At,"Null WorldCamera state view");
   state.Yaw0C.Write(angle);
  }
  public static void GameSetYaw(PortalCamera camera,float angle,PortalTransitionBoundaries b,Invocation i){
   PortalCamera original=camera; // RBX at2BFAFB
   float saved=angle; // XMM6 at2BFAFE; no arithmetic or post-call field read
   i.At=0x2bfb01;b.SetWorldCameraYaw60A6E8(original,saved);
   i.At=0x2bfb07;
   if(original==null)throw new MissingContract(i.At,"Null original camera at yaw-field write");
   original.Yaw59C.Write(saved);
  }
 }
}
