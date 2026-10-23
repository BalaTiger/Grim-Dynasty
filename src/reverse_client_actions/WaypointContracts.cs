using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 public class WaypointBoundaries:PortalSelectionBoundaries {
  private readonly Dictionary<MovementManager,WaypointManagerView> views=new Dictionary<MovementManager,WaypointManagerView>();
  public WaypointBoundaries(MoveContext context):base(context){}
  public void Attach(WaypointManagerView view){WaypointManagerView old;if(views.TryGetValue(view.Identity,out old)&&!Object.ReferenceEquals(old,view))throw Missing(0x08eaf0,"Conflicting view for shared manager");views[view.Identity]=view;}
  public WaypointManagerView View(MovementManager manager){WaypointManagerView view;if(!views.TryGetValue(manager,out view))throw Missing(0x08eaf0,"Missing shared queue view");return view;}
  public virtual void Grow08F340(WaypointQueue q){MoveContracts.Guard(delegate(Invocation i){WaypointContracts.Grow(q,this,i);});}
  public virtual WaypointIterator Begin074110(WaypointQueue q,WaypointIterator output){return WaypointContracts.Begin(q,output);}
  public virtual WaypointMap AllocateMap0162D0(ulong slots,uint stride){throw Missing(0x0162d0,"Map allocation wrapper; own storage adapter required");}
  public virtual void ReleaseMap016620(WaypointMap map,ulong slots,uint stride){throw Missing(0x016620,"Map release wrapper; own storage adapter required");}
  // Explicit managed projections of named memmove/memset, not native CRT/heap.
  // Range errors stay Unsupported; empty ranges require no source dereference.
  public virtual void CopyMap60B808(WaypointMap target,ulong to,WaypointMap source,ulong from,ulong count){
   if(count>Int32.MaxValue)throw Missing(0x60b808,"Own copy size outside supported domain");
   var temporary=new Field<Waypoint80>[(int)count];
   for(ulong n=0;n<count;n++){if(source==null)throw Missing(0x60b808,"Null copy source");temporary[(int)n]=source.Cell(unchecked(from+n),0x60b808).Snapshot();}
   for(ulong n=0;n<count;n++){if(target==null)throw Missing(0x60b808,"Null copy target");target.Cell(unchecked(to+n),0x60b808).CopyFrom(temporary[(int)n]);}
  }
  public virtual void ZeroMap5DC33E(WaypointMap target,ulong start,ulong count){
   if(count>Int32.MaxValue)throw Missing(0x5dc33e,"Own zero size outside supported domain");
   for(ulong n=0;n<count;n++){if(target==null)throw Missing(0x5dc33e,"Null zero target");target.Cell(unchecked(start+n),0x5dc33e).Write(null);}
  }
  public virtual Waypoint80 Allocate80(){throw Missing(0x5da6c4,"Own/native waypoint allocator must be supplied explicitly");}
  public virtual void FreeBlock(Waypoint80 block){throw Missing(0x5da6bc,"Own/native block release must be supplied explicitly");}
  public virtual void FreeMap(WaypointMap map,ulong slots){throw Missing(0x5da6bc,"Map allocator validation/release");}
  public virtual Displacement3 WorldVector609D98(WorldPosition position,Displacement3 output){throw Missing(0x609d98,"World-vector conversion");}
  public virtual byte MoveObject609F08(object navigation,object owner,Displacement3 vector){throw Missing(0x609f08,"Actual navigation movement");}
  public virtual void FromWorld609DC0(WorldPosition output,Displacement3 vector,object region){throw Missing(0x609dc0,"Region-relative position conversion");}
  public virtual void PutOnFloor609BB0(WorldPosition position){throw Missing(0x609bb0,"Actual floor placement");}
  public virtual void InitializePosition609720(WorldPosition position){throw Missing(0x609720,"Loaded WorldVec3 constructor; disk bytes alone insufficient");}
  public virtual byte ProducePortal(WaypointManagerView view,WorldPosition destination,object portal,WorldPosition hidden,Invocation i){throw Missing(0x08eb5e,"Three-waypoint portal producer not integrated; cleared queue retained");}
  public virtual byte ExecutePortal(WaypointManagerView view,Waypoint80 local,Invocation i){throw Missing(0x08dafe,"Portal coordinate-frame/camera branch not integrated; record already consumed");}
  public override byte MoveManager(MovementManager manager,WorldPosition destination){byte result=0;MoveContracts.Guard(delegate(Invocation i){result=WaypointContracts.Move(View(manager),destination,this,i);});return result;}
 }
 public static class WaypointContracts {
  static T Need<T>(T v,uint at)where T:class{if(v==null)throw new MissingContract(at,"Null native dereference outside supported domain");return v;}
  static void CopyPosition(WorldPosition d,WorldPosition s,bool tailBeforeStores,uint at){
   Need(s,at);var a=s.Q0.Snapshot();var b=s.Q8.Snapshot();
   if(tailBeforeStores){var c=s.Q16.Snapshot();d.Q0.CopyFrom(a);d.Q8.CopyFrom(b);d.Q16.CopyFrom(c);}
   else{d.Q0.CopyFrom(a);d.Q8.CopyFrom(b);d.Q16.CopyFrom(s.Q16.Snapshot());}
  }
  public static WaypointIterator Begin(WaypointQueue q,WaypointIterator output){
   output.Offset10.CopyFrom(q.Offset18.Snapshot());output.Proxy00.CopyFrom(q.Proxy00.Snapshot());output.Link08.Write(0);return output;
  }
  public static void Grow(WaypointQueue q,WaypointBoundaries b,Invocation i){
   ulong old=q.MapSize10.Read(0x08f34f),next=old==0?1:old;
   while(unchecked(next-old)<1||next<8){
    if(unchecked(0x333333333333333UL-next)<next)throw new MissingContract(0x08f4b1,"Native length_error/EH outside supported domain");
    next=unchecked(next+next);
   }
   ulong growth=unchecked(next-old),offset=q.Offset18.Read(0x08f3a9);
   i.At=0x08f3b7;WaypointMap allocated=b.AllocateMap0162D0(next,8);
   WaypointMap source=q.Map08.Read(0x08f3bc);ulong suffix=unchecked(q.MapSize10.Read(0x08f3c8)-offset);
   i.At=0x08f3e7;b.CopyMap60B808(allocated,offset,source,offset,suffix);
   source=q.Map08.Read(0x08f3ed);ulong after=unchecked(offset+suffix);
   if(offset<=growth){
    i.At=0x08f409;b.CopyMap60B808(allocated,after,source,0,offset);
    i.At=0x08f41f;b.ZeroMap5DC33E(allocated,unchecked(after+offset),unchecked(growth-offset));
    i.At=0x08f460;b.ZeroMap5DC33E(allocated,0,offset);
   }else{
    i.At=0x08f437;b.CopyMap60B808(allocated,after,source,0,growth);
    source=q.Map08.Read(0x08f43d);
    i.At=0x08f451;b.CopyMap60B808(allocated,0,source,growth,unchecked(offset-growth));
    i.At=0x08f460;b.ZeroMap5DC33E(allocated,unchecked(offset-growth),growth);
   }
   source=q.Map08.Read(0x08f465);
   if(source!=null){ulong size=q.MapSize10.Read(0x08f487);i.At=0x08f491;b.ReleaseMap016620(source,size,8);}
   q.Map08.Write(allocated);q.MapSize10.Write(unchecked(q.MapSize10.Read(0x08f49a)+growth));
  }
  public static void Clear(WaypointQueue q,WaypointBoundaries b,Invocation i){
   ulong count=q.Count20.Read(0x073f84);
   // No external call between the native decrement loop and its terminal write.
   if(count!=0){q.Count20.Write(0);q.Offset18.Write(0);}
   ulong size=q.MapSize10.Read(0x073fa1);
   for(ulong n=size;n!=0;){--n;var map=Need(q.Map08.Read(0x073fb0),0x073fb7);var block=map.Cell(n,0x073fb7).Read(0x073fb7);if(block!=null){i.At=0x073fc0;b.FreeBlock(block);}}
   var current=q.Map08.Read(0x073fca);
   if(current!=null){ulong currentSize=q.MapSize10.Read(0x073fd3);
    if(currentSize>0x1fffffffffffffffUL)throw new MissingContract(0x073fe6,"Native invalid-parameter path");
    if(currentSize>=512)throw new MissingContract(0x073ffd,"Large native map allocation alignment/header validation not modeled");
    i.At=0x074039;b.FreeMap(current,currentSize);
   }
   q.MapSize10.Write(0);q.Map08.Write(null);
  }
  public static void Append(WaypointQueue q,Waypoint80 borrowed,WaypointBoundaries b,Invocation i){
   ulong countPlus=unchecked(q.Count20.Read(0x08f4cf)+1);
   if(q.MapSize10.Read(0x08f4dc)<=countPlus){i.At=0x08f4e2;b.Grow08F340(q);}
   ulong mask=unchecked(q.MapSize10.Read(0x08f4e7)-1);q.Offset18.Write(q.Offset18.Read(0x08f4ee)&mask);
   ulong index=unchecked(q.Offset18.Read(0x08f4f2)+q.Count20.Read(0x08f4f6))&unchecked(q.MapSize10.Read(0x08f4fa)-1);
   var map=Need(q.Map08.Read(0x08f504),0x08f510);
   if(map.Cell(index,0x08f510).Read(0x08f510)==null){i.At=0x08f51c;Waypoint80 block=b.Allocate80();Need(q.Map08.Read(0x08f521),0x08f525).Cell(index,0x08f525).Write(block);}
   Waypoint80 destination=Need(q.Map08.Read(0x08f529),0x08f52d).Cell(index,0x08f52d).Read(0x08f52d);
   if(destination!=null)for(int chunk=0;chunk<5;chunk++)destination.CopyChunkFrom(borrowed,chunk);
   q.Count20.Write(unchecked(q.Count20.Read(0x08f55c)+1));
  }
  public static byte Next(WaypointManagerView view,WaypointBoundaries b,Invocation i){
   var q=view.Queue40;var manager=view.Identity;
   if(q.Count20.Read(0x08d9f5)==0)return 0;
   var hidden=new WaypointIterator();i.At=0x08da1a;var iterator=Need(b.Begin074110(q,hidden),0x08da22);
   var proxy=Need(iterator.Proxy00.Read(0x08da22),0x08da2d);var container=Need(proxy.Container00.Read(0x08da2a),0x08da2d);
   ulong size=container.MapSize10.Read(0x08da2d);var map=Need(container.Map08.Read(0x08da31),0x08da3c);
   ulong index=unchecked(size-1)&iterator.Offset10.Read(0x08da38);var source=Need(map.Cell(index,0x08da3c).Read(0x08da3c),0x08da40);
   var local=new Waypoint80();for(int n=0;n<4;n++)local.CopyChunkFrom(source,n);
   // Capture the fifth load before the live count decrement and its store.
   var last=new Waypoint80();last.CopyChunkFrom(source,4);ulong remaining=unchecked(q.Count20.Read(0x08da67)-1);q.Count20.Write(remaining);local.CopyChunkFrom(last,4);
   if(remaining==0)q.Offset18.Write(0);else q.Offset18.Write(unchecked(q.Offset18.Read(0x08da7d)+1));
   uint kind=local.Kind00.Read(0x08da81);
   if(kind==1)return b.ExecutePortal(view,local,i);
   if(kind!=0)return 0;
   var vectorBuffer=new Displacement3();i.At=0x08de7f;Displacement3 vector=b.WorldVector609D98(local.Position08,vectorBuffer);
   i.At=0x08de88;object navigation=b.Navigation();object owner=manager.Owner00.Read(0x08de8e);
   i.At=0x08de97;if(b.MoveObject609F08(navigation,owner,vector)==0)return 0;
   owner=manager.Owner00.Read(0x08dea1);var coordsBuffer=new WorldPosition();i.At=0x08dea8;var coords=b.OwnerCoords(owner,coordsBuffer);
   CopyPosition(view.Position08,coords,false,0x08deb7);
   i.At=0x08dec8;var second=Need(b.WorldVector609D98(local.Position08,vectorBuffer),0x08ded2);
   var copied=new Displacement3();var x=second.X0.Snapshot();var y=second.Y4.Snapshot();copied.X0.CopyFrom(x);copied.Y4.CopyFrom(y);copied.Z8.CopyFrom(second.Z8.Snapshot());
   i.At=0x08dee1;object region=b.Region(view.Position08);
   i.At=0x08def2;b.FromWorld609DC0(manager.Position20,copied,region);
   i.At=0x08defc;b.PutOnFloor609BB0(manager.Position20);
   manager.Moving38.Write(1);return 1;
  }
  public static byte Move(WaypointManagerView view,WorldPosition destination,WaypointBoundaries b,Invocation i){
   i.At=0x08eb17;Clear(view.Queue40,b,i);
   object owner=view.Identity.Owner00.Read(0x08eb1c);var hidden=new WorldPosition();i.At=0x08eb23;var coords=b.OwnerCoords(owner,hidden);
   var from=new WorldPosition();CopyPosition(from,coords,false,0x08eb34);
   i.At=0x08eb46;object portal=PortalSelection.Select(from,destination,b,i);
   if(portal!=null)return b.ProducePortal(view,destination,portal,hidden,i);
   var record=new Waypoint80();i.At=0x08ec2b;b.InitializePosition609720(record.Position08);
   CopyPosition(record.Position08,destination,true,0x08ec31);i.At=0x08ec49;b.PutOnFloor609BB0(record.Position08);record.Kind00.Write(0);
   i.At=0x08ec60;Append(view.Queue40,record,b,i);i.At=0x08ec68;return Next(view,b,i);
  }
 }
}
