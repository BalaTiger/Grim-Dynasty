using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // A declared index-zero cell on the ORIGINAL borrowed mesh view. This is
 // neither a mesh constructor nor an animation controller. No initial value
 // or resource publication is inferred; the field starts Unknown.
 public sealed class EngineMeshAnimationSpeedFields {
  static readonly ConditionalWeakTable<BorrowedMeshBounds,EngineMeshAnimationSpeedFields> meshes=new ConditionalWeakTable<BorrowedMeshBounds,EngineMeshAnimationSpeedFields>();
  public readonly BorrowedMeshBounds Mesh;
  public readonly Field<float> Speed130=new Field<float>();
  EngineMeshAnimationSpeedFields(BorrowedMeshBounds mesh){Mesh=mesh;}
  internal static EngineMeshAnimationSpeedFields ForMesh(BorrowedMeshBounds mesh){return meshes.GetValue(mesh,delegate(BorrowedMeshBounds value){return new EngineMeshAnimationSpeedFields(value);});}
 }
 public sealed class EngineAnimationSpeedContext {
  public readonly ActorBoundingBoxContext Bounds;
  public readonly ActorBoundingBoxState Actor;
  readonly Dictionary<BorrowedMeshBounds,EngineMeshAnimationSpeedFields> meshes=new Dictionary<BorrowedMeshBounds,EngineMeshAnimationSpeedFields>();
  // Explicit OWN admission, before any native call. The existing Bounds
  // registry remains authoritative; Bind cannot replace an existing view.
  // Its public Bind APIs may add an Unknown canonical view before rejecting
  // a previously unseen foreign identity. Admission is not mutation-free;
  // no original field or successful runtime result is produced by that entry.
  public EngineAnimationSpeedContext(ActorBoundingBoxContext bounds,ActorBoundingBoxState actor){
   if(bounds==null||actor==null)throw new ArgumentNullException("existing actor-bounds composition");
   if(!Object.ReferenceEquals(bounds.Bind(actor.Entity),actor))throw new InvalidOperationException("Use the original Actor sidecar in the existing Bounds context");
   Bounds=bounds;Actor=actor;
  }
  public EngineMeshAnimationSpeedFields BindExistingMesh(BorrowedMeshBounds mesh){
   if(mesh==null)throw new ArgumentNullException("existing mesh");
   if(!Object.ReferenceEquals(Bounds.BindMesh(mesh.Identity),mesh))throw new InvalidOperationException("Use the original borrowed mesh view in the existing Bounds context");
   EngineMeshAnimationSpeedFields fields;
   if(meshes.TryGetValue(mesh,out fields))return fields;
   fields=EngineMeshAnimationSpeedFields.ForMesh(mesh);meshes.Add(mesh,fields);return fields;
  }
  internal EngineMeshAnimationSpeedFields RequireMesh(BorrowedMeshBounds mesh,uint at){
   EngineMeshAnimationSpeedFields fields;
   if(mesh==null||!meshes.TryGetValue(mesh,out fields))throw new MissingContract(at,"Returned mesh has no explicitly declared index-zero speed cell in this composition");
   mesh.RequireLive(at);return fields;
  }
 }
 public sealed class EngineAnimationSpeedInvocation {
  public readonly Invocation Native=new Invocation(),MeshCall=new Invocation();
  public Field<object> CapturedPrimary{get;private set;}
  public Field<float> Result{get;private set;}
  public BorrowedMeshBounds ReturnedMesh;
  public long SignedIndex,ByteOffset;
  public bool PrimaryCaptured,MeshEntered,MeshReturned,SpeedCaptured,Returned;
  public EngineAnimationSpeedInvocation(){Begin();}
  internal void Begin(){Native.At=MeshCall.At=0;CapturedPrimary=new Field<object>();Result=new Field<float>();ReturnedMesh=null;SignedIndex=ByteOffset=0;PrimaryCaptured=MeshEntered=MeshReturned=SpeedCaptured=Returned=false;}
 }
 // Real Engine027410: r1-engine-animation-speed-run1/04..05. The saved
 // Character6DF6D0/Player787100 tables both bind +370 to Game06DA30
 // (r1-entity-bounds-slots/inspection.json). This selects the observed disk
 // dispatch explicitly; it does not assert a loaded game's table contents.
 //
 // Domain: same existing Actor/mesh identities, stable sequential typed
 // storage and index0 only. No native allocation, arbitrary pointer aliases,
 // GetMesh fallback609688, other array indices, mesh loading or speed writer
 // is supplied. The index restriction is checked AFTER the actual getter;
 // it is an OWN declared-storage limit, not an invented native bounds check.
 public static class EngineAnimationSpeedContracts {
  public static Field<float> GetAnimationSpeed027410(EngineAnimationSpeedContext context,uint index,EngineAnimationSpeedInvocation invocation){
   if(invocation==null)throw new MissingContract(0x027410,"Missing animation-speed invocation");var c=invocation;c.Begin();var i=c.Native;i.At=0x027410;
   try{
    i.At=0x027416;if(context==null)throw new MissingContract(i.At,"Missing existing actor-bounds composition");context.Actor.Entity.RequireLive(i.At);c.CapturedPrimary.CopyFrom(context.Actor.Entity.Virtual00.Snapshot());c.PrimaryCaptured=true;
    i.At=0x027419;c.SignedIndex=unchecked((int)index);
    i.At=0x02741c;ActorBoundingBoxContracts.RequireObservedDispatch(c.CapturedPrimary,i.At);c.MeshEntered=true;
    c.ReturnedMesh=ActorBoundingBoxContracts.GetMeshInstance06DA30(context.Actor,c.MeshCall);c.MeshReturned=true;
    i.At=0x027422;c.ByteOffset=c.SignedIndex*0x68L;
    i.At=0x027426;
    if(c.SignedIndex!=0)throw new MissingContract(i.At,"Only the observed caller's index0 speed130 storage is declared; other signed indices remain Unsupported after GetMesh");
    var fields=context.RequireMesh(c.ReturnedMesh,i.At);c.Result.CopyFrom(fields.Speed130.Snapshot());c.SpeedCaptured=true;
    i.At=0x027434;c.Returned=true;return c.Result;
   }catch(MissingContract error){i.At=error.Address;throw;}
  }
 }
}
