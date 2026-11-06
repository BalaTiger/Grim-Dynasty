using System;
using System.Collections.Generic;
using GrimDynasty.CombatRuntime;
using GrimDynasty.ReverseClient.Actions;

public static class GameRunSpeedTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static uint Bits(float value){return NativeAttributeGetter.FloatBits(value);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 public sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly NativeAttributeView Attributes=new NativeAttributeView();
  public readonly GamePlayerRunSpeedCapState Cap;
  public readonly GameRunSpeedState State;
  public readonly AddressSymbol<ClassRecord> Pet=new AddressSymbol<ClassRecord>(0xa5cb40);
  public readonly GameRunSpeedContext Context;
  public readonly GameRunSpeedInvocation Call=new GameRunSpeedInvocation();
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());Pet.Write(new ClassRecord());
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);Bridge.Bind(Owner,Move);
   Cap=GamePlayerRunSpeedCapState.Bind(Bridge,Move,Attributes);State=GameRunSpeedState.Bind(Cap);Context=new GameRunSpeedContext(State,Pet,NativeAttributeConstants.FromRecordedSnapshot(),true,true);Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);
  }
  public void OrdinaryClass(){Callbacks.PlayerClass.Read(0).Parent10.Write(null);}
  public void Attribute(float value){Attributes.SetObservedFloat(0x38,value);Attributes.SetObservedFloat(0x120,0);Attributes.SetObservedFloat(0x208,0);}
  public void ConstructDuration(){Same(State.Duration,GameDurationDamageManagerContracts.Construct2790A0(State.Duration,Move,new GameDurationDamageConstructorInvocation()),"Constructor returns same Duration identity");}
  public void Normal(){OrdinaryClass();Attribute(100);ConstructDuration();}
  public float Run(byte mode){return GameRunSpeedContracts.GetRunSpeed069040(Move,mode,Context,Call);}
 }
 public static void Run(){scenarios=assertions=0;
  Test("Ordinary nonzero mode evaluates producers but bypasses unknown flag and cap",delegate{
   var f=new Fixture();f.Normal();Eq(100f,f.Run(1),"Actual attribute and zero-duration arithmetic");Check(f.Call.ClassReturned&&f.Call.PetTestReturned&&f.Call.AttributeReturned&&f.Call.DurationReturned&&f.Call.Returned,"Predecessors really returned");Check(!f.Call.CapEntered&&!f.Call.AbsoluteCapEntered,"Mode branch bypasses both caps");Eq(0u,f.Call.DurationBits.Value,"Constructed empty vector returns positive zero");Eq(0x13u,f.Call.Duration.CapturedKey.Read(0),"Actual fixed-damage argument13");Eq(0L,f.Call.Duration.InitialCount,"Actual constructor produced empty vector");Eq(0xe,f.Call.Attribute.Index.Value,"Original +F78 getter index");Same(f.Attributes,f.Call.Attribute.Fields,"Original attribute storage");Same(f.Cap,f.State.Duration.Cap,"Duration shares cap owner");Same(f.Move.SpeedFlag26B3,f.State.SpeedFlag26B3,"Original flag cell borrowed");Same(f.State,GameRunSpeedState.Bind(f.Cap),"Unique run-speed composition");Missing(0,delegate{f.Move.SpeedFlag26B3.Read(0);},"Mode did not initialize flag");Missing(0,delegate{f.Cap.Minimum5018.Read(0);},"Mode did not initialize caps");Eq(0x0692d8u,f.Call.Native.At,"Actual ordinary return");
  });
  Test("Player cap early return preserves computed speed and fresh shared attributes",delegate{
   var f=new Fixture();f.Normal();f.Move.SpeedFlag26B3.Write(0);f.Cap.Minimum5018.Write(0);Eq(100f,f.Run(0),"Early cap returns computed input");Check(f.Call.CapEntered&&f.Call.Cap.Returned&&!f.Call.Cap.AttributeEntered,"No invented cap base read on early path");Eq(f.Call.AdjustedBits.Value,f.Call.Cap.ReturnedBits.Value,"Cap receives actual arithmetic register");f.Attributes.SetObservedFloat(0x38,123.5f);Eq(123.5f,f.Run(0),"Ordinary getter rereads same mutable field");Missing(0,delegate{f.Cap.Maximum501C.Read(0);},"Minimum early return never fabricates maximum");
  });
  Test("Positive cap calls the biased second getter with exact RN32 output",delegate{
   var f=new Fixture();f.Normal();f.Attribute(300);f.Move.SpeedFlag26B3.Write(0);f.Cap.Minimum5018.Write(50);f.Cap.Maximum501C.Write(120);f.Attributes.SetObservedFloat(0x378,100);f.Attributes.SetObservedFloat(0x460,0);f.Attributes.SetObservedFloat(0x548,0);f.Attributes.SetObservedFloat(0x130,0);
   Eq(0x42efffffu,Bits(f.Run(0)),"Native separate multiply rounding retained");Check(f.Call.Cap.AttributeEntered&&f.Call.Cap.Returned&&f.Call.Returned,"Both actual getters returned");Same(f.Call.Attribute.Fields,f.Call.Cap.Attribute.Fields,"Both native accumulator addresses share one sparse storage");Eq(0xe,f.Call.Attribute.Index.Value,"Ordinary original index");Eq(0xde,f.Call.Cap.Attribute.Index.Value,"Explicit +340 address bias only in cap");Eq(Bits(300),f.Call.AdjustedBits.Value,"Precap arithmetic retained");
  });
  Test("Player table never skips actual parent field or ancestor traversal",delegate{
   var f=new Fixture();f.Attribute(100);f.ConstructDuration();Missing(0x06908e,delegate{f.Run(1);},"Player class still consumes unknown Parent10");Check(f.Call.ClassReturned&&!f.Call.PetTestReturned&&!f.Call.AttributeEntered,"No class-name shortcut");var parent=new ClassRecord();f.Callbacks.PlayerClass.Read(0).Parent10.Write(parent);Missing(0x591484,delegate{f.Run(1);},"Deeper unknown parent consumed by real helper");parent.Parent10.Write(null);Eq(100f,f.Run(1),"Explicit non-Pet ancestry reaches ordinary branch");
  });
  Test("Pet ancestry stops at actual unresolved slot8D0 before attributes",delegate{
   var f=new Fixture();f.Callbacks.PlayerClass.Read(0).Parent10.Write(f.Pet.Read(0));Missing(0x0690af,delegate{f.Run(0);},"Immediate Pet ancestor");Check(f.Call.PetTestReturned&&!f.Call.AttributeEntered&&!f.Call.DurationEntered&&!f.Call.Returned,"Pet branch cannot impersonate ordinary result");var parent=new ClassRecord();parent.Parent10.Write(f.Pet.Read(0));f.Callbacks.PlayerClass.Read(0).Parent10.Write(parent);Missing(0x0690af,delegate{f.Run(1);},"Deeper Pet ancestor through591480");
  });
  Test("Attribute failures precede Duration and cannot be hidden by cap early return",delegate{
   var f=new Fixture();f.OrdinaryClass();f.ConstructDuration();f.Move.SpeedFlag26B3.Write(0);f.Cap.Minimum5018.Write(0);Missing(0x0cf6ee,delegate{f.Run(0);},"C first");Eq(0x208L,f.Call.Attribute.FailingOffset.Value,"Ordinary C address");Check(f.Call.AttributeEntered&&!f.Call.AttributeReturned&&!f.Call.DurationEntered&&!f.Call.CapEntered,"No skipped predecessor");f.Attributes.SetObservedFloat(0x208,0);Missing(0x0cf6f7,delegate{f.Run(0);},"A second");f.Attributes.SetObservedFloat(0x38,10);Missing(0x0cf70b,delegate{f.Run(0);},"B after prior arithmetic");f.Attributes.SetObservedFloat(0x120,0);Eq(10f,f.Run(0),"Only actual full predecessor allows cap");
  });
  Test("Unconstructed and wrong Duration table never become zero damage",delegate{
   var f=new Fixture();f.OrdinaryClass();f.Attribute(100);Missing(0x06926a,delegate{f.Run(1);},"Unknown copied subobject vptr is consumed at dispatch");Check(f.Call.AttributeReturned&&!f.Call.DurationEntered&&!f.Call.Returned,"Attribute prefix retained");f.ConstructDuration();Eq(100f,f.Run(1),"Actual constructor enables the empty vector getter");f.State.Duration.Virtual00.Write(null);Missing(0x06926a,delegate{f.Run(1);},"Known null table is not an observed dispatch");Check(!f.Call.DurationReturned&&!f.Call.Returned,"Reused diagnostic return flags cleared");
   var g=new Fixture();g.Normal();g.Move.SpeedFlag26B3.Write(0);g.Cap.Minimum5018.Write(0);var records=new GameDurationDamageRecords(1);records.Record(0).Type0C.Write(0x13);records.Record(0).Virtual00.Write(new object());g.State.Duration.Begin60.Write(records.Pointer(0));g.State.Duration.End68.Write(records.Pointer(records.ByteLength));g.State.Duration.Capacity70.Write(records.Pointer(records.ByteLength));
   Missing(0x279d9e,delegate{g.Run(0);},"Matching Duration tailcall cannot be bypassed by an eligible cap early return");Check(g.Call.AttributeReturned&&g.Call.DurationEntered&&!g.Call.DurationReturned&&!g.Call.CapEntered&&!g.Call.Returned,"Precap Duration failure propagates without fabricated scalar");Same(records.Record(0),g.Call.Duration.MatchedRecord,"Exact matching record retained");Check(!g.Call.AdjustedBits.HasValue,"Three-step scalar has not executed");
  });
  Test("Late original26B3 read selects exact unclosed absolute-cap boundary",delegate{
   var f=new Fixture();f.Normal();Missing(0x069292,delegate{f.Run(0);},"Unknown flag after scalar prefix");Check(f.Call.AttributeReturned&&f.Call.DurationReturned&&f.Call.AdjustedBits.HasValue&&!f.Call.CapEntered,"No early flag read");f.Move.SpeedFlag26B3.Write(1);Missing(0x0687e0,delegate{f.Run(0);},"Absolute database path remains Unsupported");Check(f.Call.AbsoluteCapEntered&&!f.Call.CapEntered&&!f.Call.Returned,"No fixture database result");f.Move.SpeedFlag26B3.Write(0);f.Cap.Minimum5018.Write(0);Eq(100f,f.Run(0),"Same flag cell reread on next call");Check(!f.Call.AbsoluteCapEntered&&f.Call.CapEntered&&f.Call.Returned,"No stale branch flags");
  });
  Test("Canonical lifetime and actual primary vptr remain authoritative",delegate{
   var f=new Fixture();f.Normal();Missing(0x069079,delegate{GameRunSpeedContracts.GetRunSpeed069040(new MoveCharacter(),1,f.Context,f.Call);},"Different owner cannot borrow attributes");f.Owner.Entity.Virtual00.Write(new object());Missing(0x06907c,delegate{f.Run(1);},"Unknown primary target");f.Owner.Entity.Virtual00.Forget();Missing(0x06907c,delegate{f.Run(1);},"Unknown captured table consumed at call");f.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);f.Owner.Entity.MarkReleased();Missing(0x069079,delegate{f.Run(1);},"Original Entity lifetime enforced");Check(!f.Call.ClassReturned&&!f.Call.AttributeEntered&&!f.Call.Returned,"Failure before all producers");
  });
  Test("Getter domain conditions remain explicit before later speed operations",delegate{
   var f=new Fixture();f.Normal();var unknownFp=new GameRunSpeedContext(f.State,f.Pet,NativeAttributeConstants.FromRecordedSnapshot(),true,false);Missing(0x0cf6fc,delegate{GameRunSpeedContracts.GetRunSpeed069040(f.Move,1,unknownFp,f.Call);},"FP condition is not silently assumed");Check(!f.Call.DurationEntered&&!f.Call.CapEntered,"No continuation past incomplete getter");f.Attribute(-1);Missing(0x0cf718,delegate{f.Run(1);},"Negative attribute branch remains existing Unsupported");Check(f.Call.Attribute.ScaledCBits.HasValue&&!f.Call.AdjustedBits.HasValue,"Precise arithmetic prefix retained");
  });
 }
}
