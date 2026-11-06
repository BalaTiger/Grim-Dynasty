using System;
using GrimDynasty.ReverseClient.Actions;

// OWN caller fragments only. No original input/runtime, success override,
// Ground helper execution, Controller producer or movement pass is claimed.
public static class WidgetMouseCallerTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Case(Action action){action();scenarios++;}
 static void Stop(Action action,uint address){try{action();throw new Exception("Expected Unsupported");}catch(MissingContract e){Check(e.Address==address,"Unsupported address "+e.Address.ToString("x")+" expected "+address.ToString("x"));}}
 static void Unknown<T>(Field<T> field,string why){bool unknown=false;try{field.Read(0);}catch(MissingContract){unknown=true;}Check(unknown,why);}
 sealed class Fixture {
  public readonly WorldPosition24 Target=new WorldPosition24();
  public readonly WidgetMouseCallerState Widget;
  public readonly Field<byte> R15B=Field<byte>.Of(7),BL=Field<byte>.Of(0x80);
  public readonly Controller Controller=new Controller();
  public readonly Field<Controller> R13=new Field<Controller>();
  public readonly Field<WorldPosition24> RSI=new Field<WorldPosition24>();
  public readonly Field<uint> Id=Field<uint>.Of(123);
  public readonly PlayerMouseBoundaries Boundaries;
  public readonly WidgetMouseCallerInvocation Call=new WidgetMouseCallerInvocation();
  public Fixture(){
   Widget=new WidgetMouseCallerState(new object(),Target);Widget.Byte121.Write(7);Widget.Byte1D0.Write(8);Widget.Pointer868.Write(null);
   Widget.Byte85.Write(2);Widget.Byte848.Write(0xa5);Widget.Byte849.Write(0xfe);R13.Write(Controller);RSI.Write(Target);
   Boundaries=new PlayerMouseBoundaries(new PlayerMouseContext(new MoveBoundaries(new MoveContext())));
  }
  public void Run(){WidgetMouseCallerContracts.Call225A2(Widget,R15B,BL,R13,RSI,Id,Boundaries,Call);}
 }
 static WidgetMouseCallerState Widget(WorldPosition24 target){var w=new WidgetMouseCallerState(new object(),target);w.Byte120.Write(91);w.Dword9A8.Write(0xabcdef01);return w;}
 static WorldIntersectionObserved44 Hit(float distance){var h=new WorldIntersectionObserved44();h.Distance00.Write(distance);h.Position08.Q0.Write(17);h.Position08.D8.Write(1);h.Position08.DC.Write(2);h.Position08.D10.Write(3);return h;}
 static void Publish(WidgetMouseCallerState w,WorldIntersectionObserved44 hit,WidgetGroundPublishPath path,Field<uint> edi,WidgetGroundPublishInvocation call){WidgetMouseCallerContracts.PublishReturnedGround(w,hit,Field<uint>.Of(0x7f800000),path,edi,call);}
 public static int Main(){try{
  Case(delegate{
   var f=new Fixture();f.Widget.Byte121.Write(0);f.Widget.Byte1D0.Forget();f.R15B.Write(9);f.Run();
   Check(f.Call.LocalSkipped&&!f.Call.ReachedCall&&f.Call.Native.At==0x226a7,"First raw R15B guard skips before unknown later fields");Unknown(f.Call.Frame.ObjectId,"No call frame fabricated on skip");
  });
  Case(delegate{
   var f=new Fixture();f.Widget.Byte1D0.Write(7);f.Widget.Pointer868.Forget();f.Run();
   Check(f.Call.LocalSkipped&&!f.Call.ReachedCall,"Equal nonzero R15B second guard skips");Unknown(f.Call.B2,"Skipped OR never evaluated");
  });
  Case(delegate{
   var f=new Fixture();f.Widget.Pointer868.Write(new object());f.Widget.Byte85.Forget();Stop(f.Run,0x2267c);
   Check(!f.Call.LocalSkipped&&!f.Call.ReachedCall,"Alternate pointer path is Unsupported, not skipped/success");Unknown(f.Call.Frame.OptionalByte,"Alternate branch has no frame stores");
  });
  Case(delegate{
   var f=new Fixture();f.Widget.Byte121.Forget();Stop(f.Run,0x225a2);Check(!f.Call.ReachedCall,"Unknown first guard stops");
   f.Widget.Byte121.Write(7);f.Widget.Pointer868.Forget();Stop(f.Run,0x225c0);
   f.Widget.Pointer868.Write(null);f.BL.Forget();Stop(f.Run,0x225d6);Unknown(f.Call.Frame.OptionalByte,"Unknown OR stops before pointer publication");
  });
  Case(delegate{
   var f=new Fixture();Stop(f.Run,0x128530);
   Check(f.Call.ReachedCall&&!f.Call.ActionReturned&&!f.Call.LocalSkipped&&f.Call.Native.At==0x22607,"Actual PlayerMouse entered and real base lookup remains Unsupported");
   Check(f.Call.Mouse.At==0x15e219,"Actual HandleActionFromMouse body reached lookup");
   Check(f.Call.B1.Read(0)==0x80&&f.Call.B2.Read(0)==0x82&&f.Call.B3.Read(0)==0xa5&&f.Call.Frame.B4.Read(0)==0xfe,"Raw byte OR and all register/stack bool bytes retained");
   Check(Object.ReferenceEquals(f.Call.CapturedController.Read(0),f.Controller),"Exact explicit Controller identity passed, producer not verified");
   Check(Object.ReferenceEquals(f.Call.Frame.Position.Read(0),f.Target),"Position is original RSI referent");
   Check(Object.ReferenceEquals(f.Call.Frame.ObjectId.Read(0),f.Id)&&Object.ReferenceEquals(f.Call.Frame.OptionalByte.Read(0),f.Widget.Byte1D0),"ID and optional pointer cells borrowed without copies");
   Check(f.Id.Read(0)==123&&f.Widget.Byte1D0.Read(0)==8,"Lookup failure leaves caller payloads unchanged");Unknown(f.Call.ActionResult,"Unsupported is not an action result");
  });
  Case(delegate{
   var f=new Fixture();f.Widget.Byte849.Forget();f.RSI.Forget();Stop(f.Run,0x128530);
   Check(f.Call.ReachedCall&&!f.Call.ActionReturned,"Opaque stack MOVs do not eagerly consume position/B4");Unknown(f.Call.Frame.Position,"Unknown RSI pointer copied");Unknown(f.Call.Frame.B4,"Unknown B4 copied");
   f.RSI.Write(null);f.R13.Write(null);
   Stop(delegate{WidgetMouseCallerContracts.Call225A2(f.Widget,f.R15B,f.BL,f.R13,f.RSI,null,f.Boundaries,f.Call);},0x128530);
   Check(f.Call.Frame.ObjectId.Read(0)==null&&f.Call.Frame.Position.Read(0)==null&&f.Call.CapturedController.Read(0)==null,"Known-null pointers are copied, not rejected before the actual lookup boundary");
  });
  Case(delegate{
   var h=Hit(4);var halves=UInt64FieldWords.Split(h.Position08.Q0);halves.High32.Forget();var target=new WorldPosition24();target.D14.Write(99);var w=Widget(target);var call=new WidgetGroundPublishInvocation();
   Publish(w,h,WidgetGroundPublishPath.Compare221BA,null,call);
   Check(call.TargetPublished&&call.Returned&&!call.Equal&&call.NextAddress==0x22234,"First finite fragment publishes then branches to22234");
   var copied=UInt64FieldWords.Split(target.Q0);Check(copied.Low32.Read(0)==17,"Known half of opaque Q0 retained");Unknown(copied.High32,"Unknown token half stays unknown");
   Check(target.D8.Read(0)==1&&target.DC.Read(0)==2&&target.D10.Read(0)==3,"Exact XYZ DWORDs copied");Unknown(target.D14,"Unwritten result padding replaces stale target padding with Unknown");
   Check(w.Byte120.Read(0)==91&&w.Dword9A8.Read(0)==0xabcdef01&&Object.ReferenceEquals(call.BorrowedPosition,target),"First path preserves other state and same target identity");
  });
  Case(delegate{
   var h=Hit(5);h.Position08.D14.Write(0x10203040);var w=Widget(h.Position08);var call=new WidgetGroundPublishInvocation();var edi=new Field<uint>();
   Publish(w,h,WidgetGroundPublishPath.Compare221F6,edi,call);
   Check(call.TargetPublished&&call.Returned&&call.NextAddress==0x22236,"Second finite path reaches its different join");
   Check(w.Target2D8.Q0.Read(0)==17&&w.Target2D8.D10.Read(0)==3&&w.Target2D8.D14.Read(0)==0x10203040,"Same-address 16+8 copy preserves full opaque position");
   Unknown(w.Dword9A8,"Unknown EDI is copied at22224, not silently zeroed");Check(w.Byte120.Read(0)==91,"Non-equal branch does not clear120");
  });
  Case(delegate{
   var h=new WorldIntersectionObserved44();h.Distance00.Write(float.PositiveInfinity);var target=new WorldPosition24();target.Q0.Write(77);target.D14.Write(88);var w=Widget(target);var call=new WidgetGroundPublishInvocation();
   Publish(w,h,WidgetGroundPublishPath.Compare221BA,null,call);
   Check(call.Equal&&!call.TargetPublished&&call.NextAddress==0x22234&&w.Byte120.Read(0)==91,"First equal branch retains old target and120");
   Check(target.Q0.Read(0)==77&&target.D14.Read(0)==88,"Unknown result position not copied on equality");
   Publish(w,h,WidgetGroundPublishPath.Compare221F6,null,call);
   Check(call.Equal&&!call.TargetPublished&&call.NextAddress==0x226a4&&w.Byte120.Read(0)==0,"Second equal branch clears120 and exits via226A4");
   Check(w.Dword9A8.Read(0)==0xabcdef01&&target.Q0.Read(0)==77&&call.BorrowedPosition==null,"No second-path target/EDI publication on equality");
  });
  Case(delegate{
   var h=Hit(float.NaN);var target=new WorldPosition24();target.D14.Write(99);var w=Widget(target);var call=new WidgetGroundPublishInvocation();
   Stop(delegate{Publish(w,h,WidgetGroundPublishPath.Compare221BA,null,call);},0x221ba);
   Check(!call.Compared&&!call.TargetPublished&&!call.Returned&&target.D14.Read(0)==99,"Unordered input does not fabricate equality or mutate target");
   h.Distance00.Forget();Stop(delegate{Publish(w,h,WidgetGroundPublishPath.Compare221F6,null,call);},0x221f6);
   Check(w.Byte120.Read(0)==91&&!call.Returned,"Unknown compare has no equal-branch clear");
  });
  Console.WriteLine("{\"status\":\"passed-widget-mouse-caller-fragments\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"actual_player_mouse_entered\":true,\"base_128530_unsupported_observed\":true,\"ground_helper_executed\":false,\"native_controller_producer_verified\":false,\"movement_executed\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
