using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// OWN scalar/alias fixtures. No region objects, callbacks or native math calls.
public static class WorldDistanceTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static WorldPosition24 P(ulong region,float x,float y,float z){var p=new WorldPosition24();p.Q0.Write(region);p.D8.Write(Bits(x));p.DC.Write(Bits(y));p.D10.Write(Bits(z));return p;}
 static Field<float> Run(WorldPosition24 a,WorldPosition24 b,Field<float> operand,WorldDistanceInvocation c){return WorldDistanceContracts.GetDistanceSquared224770(a,b,operand,c);}
 static void NotArithmetic(WorldDistanceInvocation c){Unknown(c.DeltaX,"X not consumed");Unknown(c.DeltaY,"Y not consumed");Unknown(c.DeltaZ,"Z not consumed");Unknown(c.SquareX,"No premature multiply");Unknown(c.SumXY,"No premature addition");}
 public static void Run(){scenarios=assertions=0;
  Test("Same nonzero region computes concrete squared distance without World registry or D14",delegate{
   var a=P(0xfedcba9876543210UL,1,2,3);var b=P(0xfedcba9876543210UL,4,6,3);var c=new WorldDistanceInvocation();var result=Run(a,b,null,c);
   Eq(25f,result.Read(0),"Concrete3-4-0 distance");Eq(-3f,c.DeltaX.Read(0),"First-minus-second X");Eq(-4f,c.DeltaY.Read(0),"First-minus-second Y");Eq(0f,c.DeltaZ.Read(0),"Z difference");Check(c.FirstCaptured&&c.SecondCaptured&&c.FirstTested&&c.SecondTested&&c.SameRegion&&c.Returned,"Actual same-token branch completed");Check(!c.NullRegion&&!c.CrossRegionEntered,"No alternative branch");Eq(0x2249b6u,c.Native.At,"Actual return boundary");Unknown(a.D14,"First padding unused");Unknown(b.D14,"Second padding unused");Eq(0xfedcba9876543210UL,c.FirstRegion.Read(0),"All pointer bits retained without object lookup");Check(!Object.ReferenceEquals(result,c.Result),"Value snapshot is not a diagnostic/native return address");
  });
  Test("Binary32 subtraction and left-associated sum match actual instruction order",delegate{
   var c=new WorldDistanceInvocation();var result=Run(P(7,4096,1,1),P(7,0,0,0),null,c);
   Eq(0x4b800000u,Bits(result.Read(0)),"(2^24+1)+1 rounds twice to2^24");Eq(0x4b800000u,Bits(c.SumXY.Read(0)),"FirstADD rounds before Z");Check(Bits(result.Read(0))!=0x4b800001u,"No reassociation to2^24+(1+1)");
   result=Run(P(7,16777216f,0,0),P(7,-1,0,0),null,c);Eq(0x4b800000u,Bits(c.DeltaX.Read(0)),"SUB rounds2^24+1 before squaring");Eq(0x57800000u,Bits(result.Read(0)),"Rounded subtraction squares to2^48");
  });
  Test("Same-address and shared-cell aliases are read-only and returned values survive reuse",delegate{
   var a=P(9,1,2,3);var alias=new WorldPosition24(a);var c=new WorldDistanceInvocation();var zero=Run(a,alias,null,c);Eq(0u,Bits(zero.Read(0)),"Same finite point produces positive zero");Check(Object.ReferenceEquals(a.Q0,alias.Q0),"Fixture truly shares address cells");
   var swapped=new WorldPosition24(new object(),a.Q0,a.DC,a.D10,a.D8,a.D14);var six=Run(a,swapped,null,c);Eq(6f,six.Read(0),"Shared XYZ cells with explicit swapped addresses");Eq(Bits(1f),a.D8.Read(0),"Input X not overwritten");Eq(Bits(2f),a.DC.Read(0),"Input Y not overwritten");Eq(Bits(3f),a.D10.Read(0),"Input Z not overwritten");Eq(0f,zero.Read(0),"Earlier return remains a value after Begin");
   var previousDiagnostic=c.Result;var nullA=new WorldPosition24();nullA.Q0.Write(0);var nullB=new WorldPosition24();var copied=Run(nullA,nullB,previousDiagnostic,c);Eq(6f,copied.Read(0),"Previous result can be an explicit observed constant operand");c.Result.Forget();Eq(6f,copied.Read(0),"Caller return independent of current diagnostic mutation");Eq(6f,six.Read(0),"Older value still independent");
   var source=P(13,5,0,0);var target=P(13,2,0,0);Run(source,target,null,c);var firstToken=c.FirstRegion;var secondToken=c.SecondRegion;
   var borrowedSource=new WorldPosition24(new object(),firstToken,source.D8,source.DC,source.D10,source.D14);var borrowedTarget=new WorldPosition24(new object(),secondToken,target.D8,target.DC,target.D10,target.D14);
   Eq(9f,Run(borrowedSource,borrowedTarget,null,c).Read(0),"Old captured token cells may be actual next-call inputs");Eq(13UL,firstToken.Read(0),"Begin does not clear borrowed first-token input");Eq(13UL,secondToken.Read(0),"Begin does not clear borrowed second-token input");Check(!Object.ReferenceEquals(firstToken,c.FirstRegion),"New capture uses a new diagnostic cell");
   var oldDelta=c.DeltaX;var oldSquare=c.SquareX;Eq(3f,Run(nullA,nullB,oldDelta,c).Read(0),"Previous delta may supply the next explicit MOVSS operand");Eq(3f,oldDelta.Read(0),"Old delta input not cleared");Eq(9f,oldSquare.Read(0),"Other retained old arithmetic observations remain unchanged");Unknown(c.DeltaX,"Current null branch has a fresh unknown delta diagnostic");
  });
  Test("Null region copies338458 including Unknown without consuming XYZ or guessed infinity",delegate{
   var first=new WorldPosition24();first.Q0.Write(0);var second=new WorldPosition24();var operand=new Field<float>();var c=new WorldDistanceInvocation();var result=Run(first,second,operand,c);
   Check(c.FirstCaptured&&c.SecondCaptured&&c.FirstTested&&!c.SecondTested&&c.NullRegion&&c.Returned,"BothQ0 copied before first-null short circuit");Unknown(c.SecondRegion,"SecondUnknown token was captured, not consumed");Unknown(result,"Unknown MOVSS operand can return unknown value");Unknown(c.Result,"Diagnostic retains unknown operand");NotArithmetic(c);
   operand.Write(Float(0x80000000));Eq(0x80000000u,Bits(Run(first,second,operand,c).Read(0)),"MOVSS preserves negative-zero bits");operand.Write(Float(0x7fc12345));Eq(0x7fc12345u,Bits(Run(first,second,operand,c).Read(0)),"MOVSS preserves explicit quiet-NaN payload");
   first.Q0.Write(9);second.Q0.Write(0);operand.Write(-17f);Eq(-17f,Run(first,second,operand,c).Read(0),"Runtime operand is borrowed, not replaced by pseudoINFINITY");Check(c.SecondTested&&c.NullRegion&&c.Returned,"Second-null branch completes");NotArithmetic(c);Unknown(first.D8,"First XYZ still unknown");Unknown(second.D10,"Second XYZ still unknown");
  });
  Test("Both operand addresses precede token TEST and null constant is loaded only on its branch",delegate{
   var a=new WorldPosition24();var b=new WorldPosition24();var c=new WorldDistanceInvocation();
   Missing(0x224781,delegate{Run(null,null,null,c);},"First address load fails first");Check(!c.FirstCaptured&&!c.SecondCaptured,"No captured token after first missing address");
   Missing(0x224787,delegate{Run(a,null,null,c);},"Second address load precedes firstUnknown TEST");Check(c.FirstCaptured&&!c.SecondCaptured&&!c.FirstTested,"FirstUnknown captured without early consumption");a.Q0.Write(0);Missing(0x224787,delegate{Run(a,null,null,c);},"Firstknown-null still requires second address");
   a.Q0.Forget();b.Q0.Write(0);Missing(0x22478d,delegate{Run(a,b,null,c);},"Secondnull cannot bypass firstUnknown TEST");Check(c.SecondCaptured&&!c.FirstTested&&!c.Returned,"Actual first test failed");
   a.Q0.Write(1);b.Q0.Forget();Missing(0x224796,delegate{Run(a,b,null,c);},"SecondUnknown consumed after successful first test");Check(c.FirstTested&&!c.SecondTested,"Second test incomplete");
   a.Q0.Write(0);Missing(0x22499a,delegate{Run(a,b,null,c);},"Missing operand address at actual null MOVSS");Check(c.NullRegion&&!c.Returned,"Missing constant is not a known infinity");Unknown(c.Result,"No return value invented");
  });
  Test("Unknown XYZ stops at actual SUB and all three differences precede multiplication",delegate{
   uint[] sites={0x2247ae,0x2247b9,0x2247c4};for(int axis=0;axis<3;axis++){
    var a=P(11,5,7,9);var b=P(11,1,2,3);var cells=new Field<uint>[]{a.D8,b.DC,a.D10};cells[axis].Forget();var c=new WorldDistanceInvocation();Missing(sites[axis],delegate{Run(a,b,null,c);},"Unknown arithmetic operand at its SUB");Eq(sites[axis],c.Native.At,"Exact operation diagnostic");Check(c.SameRegion&&!c.Returned,"Same-region decision is not successful distance");
    if(axis>0)Eq(4f,c.DeltaX.Read(0),"Earlier X subtraction retained");else Unknown(c.DeltaX,"Failed X not supplied");if(axis>1)Eq(5f,c.DeltaY.Read(0),"Earlier Y subtraction retained");else Unknown(c.DeltaY,"Later or failed Y not supplied");Unknown(c.SquareX,"No X square before all three SUBs");Unknown(c.SquareY,"No Y square before all three SUBs");Unknown(c.SquareZ,"No Z square before all three SUBs");Unknown(c.Result,"Failed prefix cannot return default0");
   }
  });
  Test("NaN infinity overflow and signed-zero classifications are preserved without payload promises",delegate{
   var c=new WorldDistanceInvocation();Check(Single.IsNaN(Run(P(1,Single.PositiveInfinity,0,0),P(1,Single.PositiveInfinity,0,0),null,c).Read(0)),"Infinity-minus-infinity yields NaN");
   Check(Single.IsNaN(Run(P(1,Float(0x7fc12345),0,0),P(1,0,0,0),null,c).Read(0)),"NaN participates in arithmetic, not a default distance");
   Check(Single.IsPositiveInfinity(Run(P(1,Single.MaxValue,0,0),P(1,-Single.MaxValue,0,0),null,c).Read(0)),"Overflowing subtraction and square classify as positive infinity");
   Check(Single.IsPositiveInfinity(Run(P(1,Single.NegativeInfinity,2,3),P(1,0,2,3),null,c).Read(0)),"Negative-infinite displacement squares positive");Eq(0u,Bits(Run(P(1,Float(0x80000000),0,0),P(1,0,0,0),null,c).Read(0)),"Squared signed-zero displacement yields positive zero");
  });
  Test("Different nonzero regions stop exactly before22C240 without XYZ reads or stale success",delegate{
   var c=new WorldDistanceInvocation();var previous=Run(P(1,3,4,0),P(1,0,0,0),null,c);Eq(25f,previous.Read(0),"Prior successful value");var a=new WorldPosition24();var b=new WorldPosition24();a.Q0.Write(1);b.Q0.Write(2);
   Missing(0x2247fb,delegate{Run(a,b,null,c);},"No guessed cross-region subtraction or portal result");Check(c.FirstCaptured&&c.SecondCaptured&&c.FirstTested&&c.SecondTested&&c.CrossRegionEntered&&!c.SameRegion&&!c.NullRegion&&!c.Returned,"Only cross-region call prefix reached");Eq(1UL,c.FirstRegion.Read(0),"First pointer captured");Eq(2UL,c.SecondRegion.Read(0),"Second pointer captured");NotArithmetic(c);Unknown(c.Result,"Reused invocation discarded stale result");Eq(25f,previous.Read(0),"Prior returned value unaffected by later failure");Unknown(a.D8,"First XYZ not consumed");Unknown(b.D10,"Second XYZ not consumed");
  });
 }
}
public static class WorldDistanceValidationProgram {
 public static int Main(){try{WorldDistanceTests.Run();Console.WriteLine("{\"status\":\"passed-new-world-distance-components-only\",\"scenarios\":"+WorldDistanceTests.Scenarios+",\"assertions\":"+WorldDistanceTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"system_library_loaded\":false,\"physical_mouse\":false,\"action_submission_completed\":false,\"walking_accepted\":false,\"gameplay_accepted\":false,\"different_region_unsupported_rva\":\"0x2247fb\",\"null_constant_inferred\":false}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
