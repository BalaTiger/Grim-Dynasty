using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Loaded EXE2DDC0..2E1FD capture, double-read2026-11-04. Main path only.
 // The supplied World view must represent the SAME Engine.World50 object.
 // No widget, portal, input event or native Engine initialization is inferred.
 public static class WidgetGroundTargetContracts {
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float Infinity(Field<uint> operand,uint at){
   if(operand==null||operand.Read(at)!=0x7f800000u)throw new MissingContract(at,"Requires explicitly sourced EXE30953C +Inf operand");
   return float.PositiveInfinity;
  }
  public static void MainPath2DDC0(WorldRayObserved36 ray,WorldIntersectionObserved44 output,MoveContext move,WorldIntersectionWorldView world,Field<uint> infinity30953C,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x2de02,"Missing invocation");var i=invocation;
   At(i,0x2de02);if(move==null)throw new MissingContract(i.At,"Missing shared move Engine binding");var engine=move.EngineGlobal.Snapshot();
   float maximum=Infinity(infinity30953C,At(i,0x2de0a));
   var actualEngine=engine.Read(At(i,0x2de20));if(actualEngine==null)throw new MissingContract(i.At,"Null Engine receiver");var receiver=actualEngine.World50.Snapshot();
   At(i,0x2de24);if(world==null||!Object.ReferenceEquals(receiver.Read(i.At),world.Identity))throw new MissingContract(i.At,"World receiver has no matching shared view");
   WorldIntersectionContracts.GetIntersection220840(world,ray,output,0,false,maximum,false,world.Floor,i);
   float sentinel=Infinity(infinity30953C,At(i,0x2de2a));
   At(i,0x2de32);float distance=output.Distance00.Read(i.At);
   // UCOMISS+JNE returns for ordered not-equal. NaN is not C# != here.
   if(float.IsNaN(distance))throw new MissingContract(i.At,"Unordered/FP-exception branch unsupported");
   if(distance!=sentinel){At(i,0x2e1fc);return;}
   // shortcut: actual miss continues into camera/frustum/portal enumeration;
   // recover its direct contracts before allowing this path to return.
   throw new MissingContract(At(i,0x2de47),"World miss requires original camera/frustum/portal fallback; no fabricated target");
  }
 }
}
