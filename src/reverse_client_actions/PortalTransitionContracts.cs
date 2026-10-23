using System;
namespace GrimDynasty.ReverseClient.Actions {
 public static class PortalTransitionContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null native dereference outside supported domain");return value;}
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  // Same identity is returned on success; this does not construct a Player.
  public static object ResolvePlayer(object input,PortalTransitionBoundaries b,Invocation i){
   if(input==null)return null;
   i.At=0x019241;ClassRecord actual=b.Class0(input);var target=b.PortalContext.PlayerClassA5CAB0.Read(0x019243);
   if(Object.ReferenceEquals(actual,target))return input;
   var parent=Need(actual,0x01924f).Parent10.Read(0x01924f);if(parent==null)return null;
   if(Object.ReferenceEquals(parent,target))return input;
   i.At=0x01925d;return b.Derived(parent,target)!=0?input:null;
  }
  public static object MainPlayer(PortalGameEngine engine,PortalTransitionBoundaries b,Invocation i){
   object connections=Need(engine,0x2ebb79).Connections40F8.Read(0x2ebb79);
   Need(connections,0x2ebb80);i.At=0x2ebb83;var first=Need(b.Connection18(connections),0x2ebb86);if(first.PlayerId10.Read(0x2ebb86)==0)return null;
   connections=engine.Connections40F8.Read(0x2ebb8c);Need(connections,0x2ebb93);i.At=0x2ebb96;var second=Need(b.Connection18(connections),0x2ebb99);uint id=second.PlayerId10.Read(0x2ebb99);
   i.At=0x2ebb9c;object manager=b.ObjectManager();i.At=0x2ebba7;object value=b.Lookup(manager,id);if(value==null)return null;
   i.At=0x2ebbba;ClassRecord actual=b.Class0(value);var target=b.PortalContext.PlayerClassA5CAB0.Read(0x2ebbbc);
   if(Object.ReferenceEquals(actual,target))return value;var parent=Need(actual,0x2ebbc8).Parent10.Read(0x2ebbc8);if(parent==null)return null;
   if(Object.ReferenceEquals(parent,target))return value;i.At=0x2ebbd6;return b.Derived(parent,target)!=0?value:null;
  }
  public static byte Produce(WaypointManagerView view,WorldPosition destination,object portal,WorldPosition choke,PortalTransitionBoundaries b,Invocation i){
   i.At=0x08eb5e;b.Choke609F30(portal,choke); // same hidden buffer as OwnerCoords@08EB23; return ignored
   var approach=new Waypoint80();var transition=new Waypoint80();var finish=new Waypoint80();
   i.At=0x08eb68;b.InitializePosition609720(approach.Position08);
   i.At=0x08eb73;b.InitializePosition609720(transition.Position08);
   i.At=0x08eb7d;b.InitializePosition609720(finish.Position08);
   var a=choke.Q0.Snapshot();var c=choke.Q8.Snapshot();var tail=choke.Q16.Snapshot();
   approach.Position08.Q0.CopyFrom(a);approach.Position08.Q8.CopyFrom(c);approach.Kind00.Write(0);approach.Position08.Q16.CopyFrom(tail);transition.Kind00.Write(1);
   i.At=0x08eba7;object connected=b.ConnectedPortal609F40(portal);
   var hidden=new WorldPosition();i.At=0x08ebb4;var returned=Need(b.Choke609F30(connected,hidden),0x08ebc9);
   a=returned.Q0.Snapshot();c=returned.Q8.Snapshot();transition.Position08.Q0.CopyFrom(a);transition.Position08.Q8.CopyFrom(c);transition.Position08.Q16.CopyFrom(returned.Q16.Snapshot());
   var scratch=new Field<ulong>();scratch.Write(0);i.At=0x08ebe7;b.PortalMatrix609F48(portal,transition.Matrix20,scratch);
   Need(destination,0x08ebed);a=destination.Q0.Snapshot();c=destination.Q8.Snapshot();finish.Kind00.Write(0);tail=destination.Q16.Snapshot();
   finish.Position08.Q0.CopyFrom(a);finish.Position08.Q8.CopyFrom(c);finish.Position08.Q16.CopyFrom(tail);
   i.At=0x08ec0d;WaypointContracts.Append(view.Queue40,approach,b,i);
   i.At=0x08ec1b;WaypointContracts.Append(view.Queue40,transition,b,i);
   i.At=0x08ec60;WaypointContracts.Append(view.Queue40,finish,b,i);
   i.At=0x08ec68;return WaypointContracts.Next(view,b,i);
  }
  static void Camera(Waypoint80 local,PortalTransitionBoundaries b,Invocation i){
   var engine=Need(b.PortalContext.GameGlobalA66288.Read(0x08dd3a),0x08dd45);
   var frame=new CoordinateFrame60();i.At=0x08dd4c;b.CameraFrame609F00(engine.Camera13B0,frame); // hidden buffer, not RAX
   float y=frame.Basis18[7].Read(0x08dd52),x=frame.Basis18[6].Read(0x08dd57),z=frame.Basis18[8].Read(0x08dd5c);
   float m6=local.Matrix20[6].Read(0x08dd61),m0=local.Matrix20[0].Read(0x08dd67),m3=local.Matrix20[3].Read(0x08dd6d),m4=local.Matrix20[4].Read(0x08dd73);
   var receiver=b.PortalContext.GameGlobalA66288.Read(0x08dd79);
   float zTerm=F(m6*z),xTerm=F(m0*x),xx=F(m3*y),yy=F(m4*y);xx=F(xx+xTerm);
   float m1=local.Matrix20[1].Read(0x08dd94);xTerm=F(m1*x);xx=F(xx+zTerm);
   float m7=local.Matrix20[7].Read(0x08dda2);zTerm=F(m7*z);yy=F(yy+xTerm);
   frame.Basis18[6].Write(xx);yy=F(yy+zTerm);
   float m5=local.Matrix20[5].Read(0x08ddbc);float zz=F(m5*y);
   float m2=local.Matrix20[2].Read(0x08ddc6);xTerm=F(m2*x);
   float m8=local.Matrix20[8].Read(0x08ddd0);zTerm=F(m8*z);zz=F(zz+xTerm);
   frame.Basis18[7].Write(yy);zz=F(zz+zTerm);frame.Basis18[8].Write(zz);
   // Capture only the table identity before atan2f. Its slot is loaded later.
   var table=Need(receiver,0x08ddec).Camera13B0.Virtual00.Read(0x08ddec);
   i.At=0x08ddf3;float angle=b.Atan2F5DC3F8(xx,zz);angle=F(angle-b.PortalContext.Pi7A80BC.Read(0x08ddf8));
   CameraAngleSetter setter=Need(Need(table,0x08de0a).Slot10.Read(0x08de0a),0x08de0a);i.At=0x08de0a;setter(receiver.Camera13B0,angle);
  }
  public static byte Execute(WaypointManagerView view,Waypoint80 local,PortalTransitionBoundaries b,Invocation i){
   var manager=view.Identity;object owner=manager.Owner00.Read(0x08da9e);var frame=new CoordinateFrame60();i.At=0x08dafe;b.OwnerFrame609518(owner,frame);
   object classified=PortalFrameMath.TransformOwner(frame,local,manager.Owner00,i);
   i.At=0x08dcc4;object player=ResolvePlayer(classified,b,i);
   if(player!=null){var engine=b.PortalContext.GameGlobalA66288.Read(0x08dd25);i.At=0x08dd2c;object main=MainPlayer(engine,b,i);if(Object.ReferenceEquals(player,main))Camera(local,b,i);}
   var a=local.Position08.Q0.Snapshot();var c=local.Position08.Q8.Snapshot();var tail=local.Position08.Q16.Snapshot();owner=manager.Owner00.Read(0x08de24);
   manager.Position20.Q0.CopyFrom(a);manager.Position20.Q8.CopyFrom(c);manager.Position20.Q16.CopyFrom(tail);
   var worldEngine=Need(b.Context.EngineGlobal.Read(0x08de30),0x08de33);object world=worldEngine.World50.Read(0x08de33);
   i.At=0x08de37;b.SetFrame609BB8(world,owner,frame);
   if(manager.Registered6C.Read(0x08de3d)!=0){i.At=0x08de4b;object navigation=b.Navigation();owner=manager.Owner00.Read(0x08de51);i.At=0x08de57;b.Notify609F68(navigation,owner);}
   manager.Moving38.Write(0);i.At=0x08de64;return WaypointContracts.Next(view,b,i);
  }
 }
}
