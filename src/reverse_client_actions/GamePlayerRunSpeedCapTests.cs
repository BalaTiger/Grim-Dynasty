using System;
using System.Collections.Generic;
using GrimDynasty.CombatRuntime;
using GrimDynasty.ReverseClient.Actions;

public static class GamePlayerRunSpeedCapTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static uint Bits(float value){return NativeAttributeGetter.FloatBits(value);}
 static float Float(uint bits){return NativeAttributeGetter.FromBits(bits);}
 static void Missing(uint address,Action action,string why){try{action();}catch(MissingContract e){Equal(address,e.Address,why);return;}throw new Exception("Expected MissingContract: "+why);}
 static void Reject(Action action,string why){try{action();}catch(InvalidOperationException){assertions++;return;}throw new Exception("Expected rejection: "+why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly NativeAttributeView Attributes=new NativeAttributeView();
  public readonly GamePlayerRunSpeedCapState State;
  public readonly GamePlayerRunSpeedCapInvocation Call=new GamePlayerRunSpeedCapInvocation();
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);Bridge.Bind(Owner,Move);State=GamePlayerRunSpeedCapState.Bind(Bridge,Move,Attributes);
  }
  public void Caps(float minimum,float maximum){State.Minimum5018.Write(minimum);State.Maximum501C.Write(maximum);}
  public void Base(float value){Attributes.SetObservedFloat(0x378,value);Attributes.SetObservedFloat(0x460,0);Attributes.SetObservedFloat(0x548,0);}
  public float Run(Field<float> input){return GamePlayerRunSpeedCapContracts.CapRunSpeed3E5A10(State,Move,input,NativeAttributeConstants.FromRecordedSnapshot(),true,true,Call);}
  public float Run(float input){return Run(Field<float>.Of(input));}
 }
 static Fixture Normal(){var f=new Fixture();f.Caps(50,200);f.Base(100);f.Attributes.SetObservedFloat(0x130,0);return f;}
 public static void Run(){scenarios=assertions=0;
  Test("Minimum early return preserves input bits and does not read invalid attributes",delegate{
   var f=new Fixture();f.State.Minimum5018.Write(0);f.Attributes.Invalidate();
   uint[] opaque={0x80000000u,0x7fc12345u,0x7f800000u,0xff800000u};
   foreach(uint bits in opaque){Equal(bits,Bits(GamePlayerRunSpeedCapContracts.CapRunSpeed3E5A10(f.State,f.Move,Field<float>.Of(Float(bits)),null,false,true,f.Call)),"MOVAPS early return preserves opaque input");Check(f.Call.Returned&&!f.Call.MaximumCaptured&&!f.Call.AttributeEntered,"No maximum/attribute read on first early return");Equal(0x3e5ab7u,f.Call.Native.At,"First return address");}
  });
  Test("Maximum early return captures input but does not read attribute or bonus",delegate{
   var f=new Fixture();f.Caps(1,Float(0x80000000));f.Attributes.Invalidate();Equal(17f,f.Run(17),"Nonpositive maximum returns input");Check(f.Call.MaximumCaptured&&f.Call.InputCaptured&&!f.Call.AttributeEntered&&f.Call.Returned,"Second early prefix");Equal(0x3e5a9cu,f.Call.Native.At,"Second return address");
  });
  Test("Finite clamp independently checks lower interior and upper",delegate{
   var f=Normal();Equal(50f,f.Run(25),"Lower cap");Equal(100f,f.Run(100),"Interior input");Equal(200f,f.Run(300),"Upper cap");Check(f.Call.AttributeEntered&&f.Call.Returned,"Actual getter executed");Equal(0x42480000u,f.Call.LowerBits.Value,"Lower binary32 bits");Equal(0x43480000u,f.Call.UpperBits.Value,"Upper binary32 bits");
  });
  Test("Crossed bounds preserve native MAX then MIN ordering",delegate{
   var f=Normal();f.Caps(200,50);Equal(50f,f.Run(100),"No sorting or replacement of crossed bounds");Equal(0x43480000u,f.Call.AfterMaximumBits.Value,"MAX stage really reaches200 before MIN50");
  });
  Test("SSE equal operands select second signed-zero operand",delegate{
   var f=Normal();f.Caps(1,1);f.Base(0);Equal(0u,Bits(f.Run(Float(0x80000000))),"MINSS equal zeros selects positive upper");Equal(0x80000000u,f.Call.AfterMaximumBits.Value,"MAXSS equal zeros selects negative input");Equal(0u,f.Call.LowerBits.Value,"Positive lower operand");Equal(0u,f.Call.UpperBits.Value,"Positive upper operand");
  });
  Test("Unknown MOV captures stop at first comparisons",delegate{
   var f=new Fixture();Missing(0x3e5a29,delegate{f.Run(new Field<float>());},"Unknown minimum first consumed by COMISS");Check(f.Call.MinimumCaptured&&!f.Call.InputCaptured&&!f.Call.MaximumCaptured,"Input MOV has not occurred");f.State.Minimum5018.Write(1);Missing(0x3e5a45,delegate{f.Run(new Field<float>());},"Unknown maximum first consumed by second COMISS");Check(f.Call.InputCaptured&&f.Call.MaximumCaptured&&!f.Call.AttributeEntered,"Unknown input was copied without eager consumption");
  });
  Test("One attribute view preserves original receiver bias and getter read order",delegate{
   var f=new Fixture();f.Caps(1,2);Missing(0xcf6ee,delegate{f.Run(1);},"First base attribute read isC");Equal(0x548L,f.Call.Attribute.FailingOffset.Value,"C uses+F78+548 == +12B8+208");Same(f.Attributes,f.Call.Attribute.Fields,"Getter reads original shared storage");Equal(0xE,GamePlayerRunSpeedCapInvocation.NativeAttributeIndex,"Original native index");Equal(0xDE,f.Call.Attribute.Index.Value,"Explicit own address-biased index only");
   f.Attributes.SetObservedFloat(0x548,0);Missing(0xcf6f7,delegate{f.Run(1);},"A followsC");Equal(0x378L,f.Call.Attribute.FailingOffset.Value,"A uses+F78+378 == +12B8+38");f.Attributes.SetObservedFloat(0x378,100);Missing(0xcf70b,delegate{f.Run(1);},"B follows earlier C arithmetic");Equal(0x460L,f.Call.Attribute.FailingOffset.Value,"B uses+F78+460 == +12B8+120");Check(f.Call.Attribute.ScaledCBits.HasValue&&!f.Call.LowerBits.HasValue,"Getter prefix precedes cap arithmetic");
  });
  Test("Bonus10A8 aliases the original mutable+F78 storage",delegate{
   // Exact independent oracle: RN32(120*K2)=0x3F999999; multiplying100
   // rounds to0x42EFFFFF, not120. A merged/double-.01 formula loses this.
   var f=Normal();f.Caps(50,120);Equal(0x42efffffu,Bits(f.Run(300)),"Instruction-separated initial cap rounding");Equal(0x42efffffu,f.Call.UpperBits.Value,"Upper register retains rounding oracle");f.Attributes.SetObservedBits(0x130,0x42a00000);Equal(200f,f.Run(300),"Same offset130 update contributes80");f.State.Maximum501C.Write(60);f.Attributes.SetObservedFloat(0x130,40);Equal(100f,f.Run(300),"Both mutable cap and shared bonus reread");Same(f.Attributes,f.State.AttributesF78,"No mirror attribute view");Equal(0x42200000u,f.Attributes.ReadObservedBits(0x130).Value,"No writes to original bonus");
  });
  Test("Unknown input waits until return or MAX and cannot hide earlier failure",delegate{
   var f=new Fixture();f.State.Minimum5018.Write(0);Missing(0x3e5aa8,delegate{f.Run(new Field<float>());},"First early-return MOV consumes unknown input");f.Caps(1,0);Missing(0x3e5a9d,delegate{f.Run(new Field<float>());},"Second early-return MOV consumes unknown input");f.Caps(1,2);f.Base(100);Missing(0x3e5a63,delegate{f.Run(new Field<float>());},"Missing bonus occurs before MAX input read");Check(f.Call.AttributeEntered&&f.Call.Attribute.Status==NativeAttributeStatus.ConditionalNormalReturn,"Earlier getter completed");f.Attributes.SetObservedFloat(0x130,0);Missing(0x3e5a77,delegate{f.Run(new Field<float>());},"Positive path consumes input at MAX");Check(f.Call.LowerBits.HasValue&&!f.Call.AfterMaximumBits.HasValue&&!f.Call.UpperBits.HasValue&&!f.Call.Returned,"Exact arithmetic prefix retained");
  });
  Test("Unmodeled FP comparisons and arithmetic stop without false native defaults",delegate{
   var f=Normal();f.State.Minimum5018.Write(Single.NaN);Missing(0x3e5a29,delegate{f.Run(10);},"NaN minimum not mistaken for comparisonfalse");f.State.Minimum5018.Write(1);f.State.Maximum501C.Write(Single.NaN);Missing(0x3e5a45,delegate{f.Run(10);},"NaN maximum not silently accepted");f.Caps(1,2);Missing(0x3e5a77,delegate{f.Run(Single.PositiveInfinity);},"Nonfinite input stopped only when MAX consumes it");Missing(0x3e5a29,delegate{GamePlayerRunSpeedCapContracts.CapRunSpeed3E5A10(f.State,f.Move,Field<float>.Of(1),NativeAttributeConstants.FromRecordedSnapshot(),true,false,f.Call);},"FP environment required for first COMISS");Check(!f.Call.InputCaptured,"FP stop before input capture");
  });
  Test("Unique owner binding prevents storage forks and checks lifetime",delegate{
   var f=Normal();Same(f.State,GamePlayerRunSpeedCapState.Bind(f.Bridge,f.Move,f.Attributes),"Same binding idempotent");Reject(delegate{GamePlayerRunSpeedCapState.Bind(f.Bridge,f.Move,new NativeAttributeView());},"Cannot fork owner attribute storage");
   var secondBridge=new GameMovementIdentityBridge(f.Callbacks);secondBridge.Bind(f.Owner,f.Move);Reject(delegate{GamePlayerRunSpeedCapState.Bind(secondBridge,f.Move,new NativeAttributeView());},"Another bridge cannot fork the same Move state");var thirdBridge=new GameMovementIdentityBridge(f.Callbacks);var aliasMove=new MoveCharacter();thirdBridge.Bind(f.Owner,aliasMove);Reject(delegate{GamePlayerRunSpeedCapState.Bind(thirdBridge,aliasMove,new NativeAttributeView());},"Another Move view cannot fork the same Entity state");
   var other=new MoveCharacter();var owner=new GameMovementCharacterState(new EntityMovementState(other));f.Callbacks.Attach(owner);f.Bridge.Bind(owner,other);Reject(delegate{GamePlayerRunSpeedCapState.Bind(f.Bridge,other,f.Attributes);},"Cannot share attribute memory across independent owners");Missing(0x3e5a1e,delegate{GamePlayerRunSpeedCapContracts.CapRunSpeed3E5A10(f.State,other,Field<float>.Of(1),NativeAttributeConstants.FromRecordedSnapshot(),true,true,f.Call);},"Wrong caller rejects before cap read");Check(!f.Call.MinimumCaptured,"Wrong identity cannot read cap");f.Owner.Entity.MarkReleased();Missing(0x3e5a1e,delegate{f.Run(1);},"Retained state rechecks same Entity lifetime");
  });
  Test("Getter unsupported branches retain precise inner stop and no later bonus read",delegate{
   var f=Normal();Missing(0xcf6e0,delegate{GamePlayerRunSpeedCapContracts.CapRunSpeed3E5A10(f.State,f.Move,Field<float>.Of(1),NativeAttributeConstants.FromRecordedSnapshot(),false,true,f.Call);},"Constant snapshot condition checked by reused getter");Check(f.Call.AttributeEntered&&!f.Call.LowerBits.HasValue,"No cap scalar arithmetic before failed getter");f.Attributes.SetObservedFloat(0x378,-1);f.Attributes.SetObservedBits(0x130,null);Missing(0xcf718,delegate{f.Run(1);},"Negative A stops before missing bonus");Equal("NegativeBaseBranchOutsideCoverage",f.Call.Attribute.Reason,"Existing getter reason preserved");f.Attributes.Invalidate();Missing(0xcf6ee,delegate{f.Run(1);},"Invalidated attributes first consumed in getter");Equal("InvalidatedAttributeView",f.Call.Attribute.Reason,"Storage invalidation is not a default");
  });
 }
}
public static class GamePlayerRunSpeedCapValidationProgram {
 public static int Main(string[] args){
  if(args.Length!=1||args[0]!="new-components")throw new ArgumentException("Fixed new-components entry only");
  GamePlayerRunSpeedCapTests.Run();
  Console.WriteLine("{\"status\":\"passed-new-player-speed-cap-components-only\",\"scenarios\":"+GamePlayerRunSpeedCapTests.Scenarios+",\"assertions\":"+GamePlayerRunSpeedCapTests.Assertions+",\"old_test_suites_rerun\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"physical_mouse\":false,\"walking_accepted\":false,\"action_submitted\":false,\"system_library_loaded\":false,\"native_math_calls\":0}");return 0;
 }
}
