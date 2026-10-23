using System;
namespace GrimDynasty.TaskItems {
 public enum RecordLoadTickMode { Qpc,TimeGetTime }
 public enum RecordLoadTickStatus { Known,Unsupported,DivideFault }
 public sealed class RecordLoadTickResult {
  public readonly RecordLoadTickMode Mode;
  public readonly RecordLoadTickStatus Status;
  public readonly string Reason;
  // A missing value is not a zero tick.
  public readonly uint? Tick32;
  internal RecordLoadTickResult(RecordLoadTickMode mode,RecordLoadTickStatus status,string reason,uint? tick){
   Mode=mode;Status=status;Reason=reason;Tick32=tick;
  }
 }
 public static class RecordLoadTicks {
  // Pure projection of Engine 1c4e49..1c4e6e and 1c5109..1c512e.
  // IAT 302208 is QueryPerformanceCounter; this class never calls it.
  // All globals and the raw output slot are explicit inputs, not recovered
  // initialization or a default clock context. Null includes an unwritten or
  // otherwise unknown output slot even when the API returned success.
  public static RecordLoadTickResult FromQpc(long? rawSlot,long counterBase,long frequency,bool apiReturnedSuccess){
   // The native caller does not branch on BOOL. A known slot remains usable
   // even after a false BOOL; false does not manufacture knowledge of a slot.
   if(!rawSlot.HasValue)return new RecordLoadTickResult(RecordLoadTickMode.Qpc,
    RecordLoadTickStatus.Unsupported,"UnknownQpcOutputSlot",null);
   long delta=unchecked(rawSlot.Value-counterBase);
   long dividend=unchecked(delta*1000L);
   // CQO sign-extends the already truncated IMUL result. Report the two IDIV
   // fault cases without executing a managed divide or claiming native EH,
   // fallback, recovery or unchanged native caller state.
   if(frequency==0)return new RecordLoadTickResult(RecordLoadTickMode.Qpc,
    RecordLoadTickStatus.DivideFault,"SignedDivisionByZero",null);
   if(dividend==long.MinValue&&frequency==-1)return new RecordLoadTickResult(RecordLoadTickMode.Qpc,
    RecordLoadTickStatus.DivideFault,"SignedDivisionOverflow",null);
   long quotient=dividend/frequency;
   return new RecordLoadTickResult(RecordLoadTickMode.Qpc,RecordLoadTickStatus.Known,
    null,unchecked((uint)quotient));
  }
  // IAT 302788 is timeGetTime. The caller consumes EAX and subtracts its
  // independently read DWORD base; no API or time initialization runs here.
  public static RecordLoadTickResult FromTimeGetTime(uint rawTick,uint tickBase){
   return new RecordLoadTickResult(RecordLoadTickMode.TimeGetTime,RecordLoadTickStatus.Known,
    null,unchecked(rawTick-tickBase));
  }
  // Modes and contexts are not coupled across samples: the caller rereads its
  // mode flag for the end sample. Only successfully converted values belong
  // here. Existing depot code owns count/total/max updates and prefix state.
  public static uint Elapsed(uint convertedStart,uint convertedEnd){
   return unchecked(convertedEnd-convertedStart);
  }
 }
}
