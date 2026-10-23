using System;
namespace GrimDynasty.ReverseClient.Actions {
 public sealed class Displacement3 {
  public readonly Field<float> X0=new Field<float>(),Y4=new Field<float>(),Z8=new Field<float>();
 }
 // Three independently observed direct dependencies. The actual MoveTo path
 // queue/portal/navigation producer08EAF0 deliberately remains unsupported.
 public class MoveManagerBoundaries:MoveBoundaries {
  public MoveManagerBoundaries(MoveContext context):base(context){}
  public virtual WorldPosition OwnerCoords(object owner,WorldPosition hiddenReturn){throw Missing(0x609518,"Owner coordinates source");}
  public virtual void Difference609A48(WorldPosition destination,Displacement3 output,WorldPosition coordinates){throw Missing(0x609a48,"World position difference import");}
  public virtual void Reposition609BB8(object world,MoveCharacter role,WorldPosition position){throw Missing(0x609bb8,"World path-position import");}
  public virtual void Notify609F68(object navigation,object owner){throw Missing(0x609f68,"Navigation position-change import; distinct from stop609F50");}
  public override byte AlreadyThere(MovementManager manager,WorldPosition position,float tolerance){
   byte result=0;MoveContracts.Guard(delegate(Invocation i){result=MoveManagerContracts.AlreadyThere(manager,position,tolerance,this,i);});return result;
  }
  public override void StopManager(MovementManager manager){MoveContracts.Guard(delegate(Invocation i){MoveManagerContracts.Stop(manager,this,i);});}
  public override void SetPathPosition(MoveCharacter role,WorldPosition position){MoveContracts.Guard(delegate(Invocation i){MoveManagerContracts.SetPathPosition(role,position,this,i);});}
 }
 public static class MoveManagerContracts {
  static T Need<T>(T value,uint at) where T:class {if(value==null)throw new MissingContract(at,"Null dereference outside supported domain");return value;}
  // Force each scalar SSE arithmetic result back to binary32. This own adapter
  // does not emulate native MXCSR exception flags, traps, FTZ or DAZ settings.
  static float F32(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  public static byte AlreadyThere(MovementManager manager,WorldPosition destination,float tolerance,MoveManagerBoundaries b,Invocation i){
   object owner=Need(manager,0x08e179).Owner00.Read(0x08e179);
   var coordinatesBuffer=new WorldPosition();i.At=0x08e18c;WorldPosition coordinates=b.OwnerCoords(owner,coordinatesBuffer);
   var delta=new Displacement3();i.At=0x08e19d;b.Difference609A48(destination,delta,coordinates);
   float x=delta.X0.Read(0x08e1a3),y=delta.Y4.Read(0x08e1a9),z=delta.Z8.Read(0x08e1af);
   float xx=F32(x*x),tt=F32(tolerance*tolerance),yy=F32(y*y),zz=F32(z*z);
   float sum=F32(xx+yy);sum=F32(sum+zz);
   // COMISS threshold^2,distance^2; SETNC. Unordered => CF=1 => false.
   return (byte)(tt>=sum?1:0);
  }
  public static void Stop(MovementManager manager,MoveManagerBoundaries b,Invocation i){
   Need(manager,0x08ed06);
   if(manager.Registered6C.Read(0x08ed06)!=0){i.At=0x08ed0f;object navigation=b.Navigation();object owner=manager.Owner00.Read(0x08ed15);i.At=0x08ed1b;b.StopObject(navigation,owner);}
   manager.Moving38.Write(0);
  }
  public static void SetPathPosition(MoveCharacter role,WorldPosition position,MoveManagerBoundaries b,Invocation i){
   var buffer=new WorldPosition();i.At=0x05d9b5;b.Coords(role,buffer); // returned pointer deliberately ignored
   Need(position,0x05d9bb);
   // Both loads precede either store here; unlike SetCurrentAttackTarget.
   var first=position.Q0.Snapshot();var second=position.Q8.Snapshot();var tail=position.Q16.Snapshot();
   buffer.Q0.CopyFrom(first);buffer.Q8.CopyFrom(second);buffer.Q16.CopyFrom(tail);
   WorldEngine engine=Need(b.Context.EngineGlobal.Read(0x05d9dd),0x05d9e0);object world=engine.World50.Read(0x05d9e0);
   i.At=0x05d9e4;b.Reposition609BB8(world,role,buffer);
   MovementManager manager=Need(role.MovementDC0.Read(0x05d9ea),0x05d9f1);
   if(manager.Registered6C.Read(0x05d9f1)!=0){i.At=0x05d9f7;object navigation=b.Navigation();object owner=manager.Owner00.Read(0x05d9fd);i.At=0x05da03;b.Notify609F68(navigation,owner);}
   manager.Moving38.Write(0);
  }
 }
}
