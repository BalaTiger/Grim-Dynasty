using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Loaded EXE evidence: offline-decode-callreturns.json, 221AE..22236 and
 // 225A2..2260D. This view neither initializes a native widget nor proves1BCD0.
 public sealed class WidgetMouseCallerState {
  public readonly object Identity;
  public readonly Field<byte> Byte85=new Field<byte>(),Byte120=new Field<byte>(),Byte121=new Field<byte>();
  public readonly Field<byte> Byte1D0=new Field<byte>(),Byte848=new Field<byte>(),Byte849=new Field<byte>();
  public readonly Field<object> Pointer868=new Field<object>();
  public readonly Field<uint> Dword9A8=new Field<uint>();
  public readonly WorldPosition24 Target2D8;
  public WidgetMouseCallerState(object identity,WorldPosition24 target){if(identity==null||target==null)throw new ArgumentNullException("widget identity/target");Identity=identity;Target2D8=target;}
 }
 public sealed class WidgetMouseCallerInvocation {
  public readonly Invocation Native=new Invocation(),Mouse=new Invocation();
  public MouseInputFrame Frame{get;private set;}
  public Field<Controller> CapturedController{get;private set;}
  public Field<byte> B1{get;private set;}
  public Field<byte> B2{get;private set;}
  public Field<byte> B3{get;private set;}
  public readonly Field<byte> ActionResult=new Field<byte>();
  public bool LocalSkipped,ReachedCall,ActionReturned;
  public WidgetMouseCallerInvocation(){Reset();}
  internal void Reset(){Native.At=Mouse.At=0;Frame=new MouseInputFrame();CapturedController=new Field<Controller>();B1=new Field<byte>();B2=new Field<byte>();B3=new Field<byte>();ActionResult.Forget();LocalSkipped=ReachedCall=ActionReturned=false;}
 }
 public enum WidgetGroundPublishPath { Compare221BA,Compare221F6 }
 public sealed class WidgetGroundPublishInvocation {
  public readonly Invocation Native=new Invocation();
  public WorldPosition24 BorrowedPosition;
  public bool Compared,Equal,TargetPublished,Returned;
  public uint NextAddress;
  internal void Reset(){Native.At=0;BorrowedPosition=null;Compared=Equal=TargetPublished=Returned=false;NextAddress=0;}
 }
 public static class WidgetMouseCallerContracts {
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing own caller view");return value;}
  // R13/RSI/BL/R15B are explicit captured registers. Their native producers,
  // including EXE1BCD0 and all intervening handler branches, remain unverified.
  public static void Call225A2(WidgetMouseCallerState widget,Field<byte> r15b,Field<byte> bl,Field<Controller> r13,Field<WorldPosition24> rsi,Field<uint> objectId50,PlayerMouseBoundaries boundaries,WidgetMouseCallerInvocation invocation){
   var call=Need(invocation,0x225a2);call.Reset();var i=call.Native;var w=Need(widget,At(i,0x225a2));
   var compare=Need(r15b,i.At).Snapshot();
   if(w.Byte121.Read(i.At)!=compare.Read(i.At)){At(i,0x226a7);call.LocalSkipped=true;return;}
   if(w.Byte1D0.Read(At(i,0x225b6))==compare.Read(i.At)){At(i,0x226a7);call.LocalSkipped=true;return;}
   if(w.Pointer868.Read(At(i,0x225c0))!=null)throw new MissingContract(At(i,0x2267c),"Non-null widget868 takes unimplemented alternate action path");
   At(i,0x225ce);var second=w.Byte85.Snapshot();At(i,0x225d6);var first=Need(bl,i.At).Snapshot();call.B2.Write((byte)(second.Read(i.At)|first.Read(i.At)));
   At(i,0x225d9);call.Frame.OptionalByte.Write(w.Byte1D0);
   At(i,0x225e3);call.Frame.ObjectId.Write(objectId50);
   At(i,0x225e8);call.Frame.Position.CopyFrom(Need(rsi,i.At).Snapshot());
   At(i,0x225ed);var fourth=w.Byte849.Snapshot();At(i,0x225f5);call.Frame.B4.CopyFrom(fourth);
   At(i,0x225f9);call.B3.CopyFrom(w.Byte848.Snapshot());
   At(i,0x22601);call.B1.CopyFrom(first);
   At(i,0x22604);call.CapturedController.CopyFrom(Need(r13,i.At).Snapshot());
   At(i,0x22607);call.ReachedCall=true;
   byte result=PlayerMouseContracts.HandleActionFromMouse15E1F0(call.CapturedController.Read(i.At),call.B1.Read(i.At),call.B2.Read(i.At),call.B3.Read(i.At),call.Frame,Need(boundaries,i.At),call.Mouse);
   At(i,0x2260d);call.ActionResult.Write(result);call.ActionReturned=true;
   // shortcut: this is the Action AL result only; later Widget-consumed return,
   // timers and actual motion require their own evidenced continuation.
  }
  static void CopyPosition(WorldPosition24 source,WorldPosition24 target,Invocation i,uint first,uint store,uint tail,uint last){
   At(i,first);var q=source.Q0.Snapshot();var x=source.D8.Snapshot();var y=source.DC.Snapshot();
   At(i,store);target.Q0.CopyFrom(q);target.D8.CopyFrom(x);target.DC.CopyFrom(y);
   At(i,tail);var z=source.D10.Snapshot();var padding=source.D14.Snapshot();
   At(i,last);target.D10.CopyFrom(z);target.D14.CopyFrom(padding);
  }
  // Caller-only slice AFTER2DDC0 returned. Supplying a record does not prove
  // that helper returned: in particular its +Inf fallback is still unsupported.
  public static void PublishReturnedGround(WidgetMouseCallerState widget,WorldIntersectionObserved44 returned,Field<uint> operand30953C,WidgetGroundPublishPath path,Field<uint> edi,WidgetGroundPublishInvocation invocation){
   if(path!=WidgetGroundPublishPath.Compare221BA&&path!=WidgetGroundPublishPath.Compare221F6)throw new ArgumentOutOfRangeException("path");
   bool first=path==WidgetGroundPublishPath.Compare221BA;uint load=first?0x221aeu:0x221f1u,compare=first?0x221bau:0x221f6u;
   var call=Need(invocation,load);call.Reset();var i=call.Native;At(i,load);var distance=Need(returned,i.At).Distance00.Snapshot();
   var w=Need(widget,first?At(i,0x221b3):i.At);if(first)call.BorrowedPosition=w.Target2D8;
   At(i,compare);float value=distance.Read(i.At);uint bits=Need(operand30953C,i.At).Read(i.At);float operand=BitConverter.ToSingle(BitConverter.GetBytes(bits),0);
   if(float.IsNaN(value)||float.IsNaN(operand))throw new MissingContract(i.At,"UCOMISS unordered/FP-exception domain is unimplemented");
   call.Compared=true;call.Equal=value==operand;
   if(call.Equal){
    if(first){At(i,0x221c1);call.NextAddress=0x22234;}
    else{At(i,0x221ff);w.Byte120.Write(0);At(i,0x22207);call.NextAddress=0x226a4;}
    call.Returned=true;return;
   }
   if(first){CopyPosition(returned.Position08,w.Target2D8,i,0x221c3,0x221c7,0x221ca,0x221cf);call.TargetPublished=true;At(i,0x221d4);call.NextAddress=0x22234;}
   else{
    At(i,0x2220c);call.BorrowedPosition=w.Target2D8;CopyPosition(returned.Position08,w.Target2D8,i,0x22213,0x22217,0x2221a,0x2221f);call.TargetPublished=true;
    At(i,0x22224);w.Dword9A8.CopyFrom(Need(edi,i.At).Snapshot());At(i,0x2222b);call.NextAddress=0x22236;
   }
   call.Returned=true;
  }
 }
}
