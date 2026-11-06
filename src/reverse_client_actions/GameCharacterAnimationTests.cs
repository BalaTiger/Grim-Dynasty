using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New leaf tests only. Explicit OWN pointers/owners; no animation resources,
// loaded original or virtual Play implementation is supplied or simulated.
public static class GameCharacterAnimationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static GameMovementCharacterState Owner(){return new GameMovementCharacterState(new EntityMovementState(new object()));}
 public static void Run(){
  scenarios=assertions=0;
  Test("Binding retains original owner and all pointers Unknown",delegate{
   var owner=Owner();var fields=GameCharacterAnimationFields.ForOwner(owner);Same(fields,GameCharacterAnimationFields.ForOwner(owner),"One sidecar per original owner");Same(owner,fields.Owner,"Same full owner");Unknown(fields.Set2578,"No fallback initialized");Unknown(fields.Set2600,"No first override initialized");Unknown(fields.Set2608,"No second override initialized");Check(!Object.ReferenceEquals(fields,GameCharacterAnimationFields.ForOwner(Owner())),"Separate owner has separate fields");
  });
  Test("First nonnull override preserves captured identity and skips later Unknown fields",delegate{
   var owner=Owner();var fields=GameCharacterAnimationFields.ForOwner(owner);var first=new AnimationSet();var next=new AnimationSet();fields.Set2600.Write(first);var call=new GameGetAnimationSetInvocation();var result=GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);
   Check(call.Returned&&!call.FallbackCaptured,"First override returned");Eq(1,call.OverrideCaptures.Count,"Second pointer not loaded");Eq(0x2600u,call.SelectedOffset,"First slot selected");Eq(0x05ae6eu,call.Native.At,"Override return");Same(first,result.Read(0),"Exact first object identity");Unknown(first.Kind08,"Getter never reads or initializes Kind");Unknown(fields.Set2608,"Later override untouched");Unknown(fields.Set2578,"Fallback untouched");
   fields.Set2600.Write(next);Same(first,result.Read(0),"Returned pointer is captured rather than a live source Field");var again=GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);Same(next,again.Read(0),"Next invocation rereads current first pointer");Same(first,result.Read(0),"Reusing invocation does not erase earlier returned value");Eq(1,call.OverrideCaptures.Count,"Diagnostics reset on reuse");
  });
  Test("Known-null first override selects second identity",delegate{
   var owner=Owner();var fields=GameCharacterAnimationFields.ForOwner(owner);fields.Set2600.Write(null);var second=new AnimationSet();fields.Set2608.Write(second);var call=new GameGetAnimationSetInvocation();var result=GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);
   Same(second,result.Read(0),"Second pointer returned");Eq(2,call.OverrideCaptures.Count,"Both actual loop reads");Eq(0x2600u,call.OverrideOffsets[0],"First offset");Eq(0x2608u,call.OverrideOffsets[1],"Second offset");Eq(0x2608u,call.SelectedOffset,"Second selected");Check(call.Returned&&!call.FallbackCaptured,"Fallback not loaded");Unknown(fields.Set2578,"Unconsumed fallback remains Unknown");
  });
  Test("Two null overrides return captured fallback pointer or known null",delegate{
   var owner=Owner();var fields=GameCharacterAnimationFields.ForOwner(owner);fields.Set2600.Write(null);fields.Set2608.Write(null);var fallback=new AnimationSet();fields.Set2578.Write(fallback);var call=new GameGetAnimationSetInvocation();var result=GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);
   Same(fallback,result.Read(0),"Actual fallback identity");Check(call.Returned&&call.FallbackCaptured,"Fallback MOV completed");Eq(0x2578u,call.SelectedOffset,"Fallback selected");Eq(0x05ae6au,call.Native.At,"Fallback RET");fields.Set2578.Write(null);Same(fallback,result.Read(0),"Fallback captured before later source mutation");Same(null,GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call).Read(0),"Known null returned without fabricating a set");Unknown(fallback.Kind08,"No set field initialization");
  });
  Test("Unknown fallback returns normally and fails only when caller consumes it",delegate{
   var owner=Owner();var fields=GameCharacterAnimationFields.ForOwner(owner);fields.Set2600.Write(null);fields.Set2608.Write(null);var call=new GameGetAnimationSetInvocation();var result=GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);
   Check(call.Returned&&call.FallbackCaptured,"Unknown MOV payload can return");Eq(0x05ae6au,call.Native.At,"Getter really returned");Unknown(call.FallbackCapture,"Captured fallback remains Unknown");Missing(0x05fa05,delegate{result.Read(0x05fa05);},"Caller owns the eventual pointer dereference stop");Eq(0x05ae6au,call.Native.At,"Caller read does not rewrite getter diagnostics");
  });
  Test("Unknown override stops at TEST in priority order without a false fallback",delegate{
   var owner=Owner();var fields=GameCharacterAnimationFields.ForOwner(owner);fields.Set2608.Write(new AnimationSet());fields.Set2578.Write(new AnimationSet());var call=new GameGetAnimationSetInvocation();Missing(0x05ae53,delegate{GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);},"Unknown first pointer cannot be skipped");Eq(1,call.OverrideCaptures.Count,"Only first MOV executed");Check(!call.Returned&&!call.FallbackCaptured,"No return or fallback");Unknown(call.Result,"No default pointer");
   fields.Set2600.Write(null);fields.Set2608.Forget();Missing(0x05ae53,delegate{GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);},"Unknown second pointer cannot fall through");Eq(2,call.OverrideCaptures.Count,"Second loop read retained");Check(!call.Returned&&!call.FallbackCaptured,"Reused invocation has no stale return");Unknown(call.Result,"No old successful result");
  });
  Test("Released owner does not acquire a successful pointer or cached result",delegate{
   var owner=Owner();var fields=GameCharacterAnimationFields.ForOwner(owner);var set=new AnimationSet();fields.Set2600.Write(set);var call=new GameGetAnimationSetInvocation();GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);owner.Entity.MarkReleased();Missing(0x05ae50,delegate{GameCharacterAnimationContracts.GetAnimationSet05AE40(owner,call);},"Own lifetime fails at first memory load");Check(!call.Returned&&!call.FallbackCaptured,"Previous success cleared");Eq(0,call.OverrideCaptures.Count,"No load after lifetime failure");Same(set,fields.Set2600.Read(0),"Original field not modified");Unknown(call.Result,"No cached pointer returned");
  });
 }
}
