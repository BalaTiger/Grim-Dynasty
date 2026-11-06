using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// OWN storage tests. The two old public calls below are exercised only through
// their copy prefix; unresolved skill/reposition imports still throw. The fresh
// SetPathPosition hidden local cannot be made an input alias through its API.
public static class WorldPositionWordBridgeTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Throws<T>(Action body,string why)where T:Exception{try{body();}catch(T){Check(true,why);return;}throw new Exception("Expected "+typeof(T).Name+": "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static ulong Bits(uint low,uint high){return (ulong)low|((ulong)high<<32);}
 static WorldPosition24 Point(uint x,uint y,uint z){var p=new WorldPosition24();p.Q0.Write(77);p.D8.Write(x);p.DC.Write(y);p.D10.Write(z);return p;}
 static void Half(Field<uint> field,bool known,uint value,string why){if(known)Eq(value,field.Read(0),why);else Unknown(field,why);}
 public static void RunNew(){scenarios=assertions=0;
  Test("Ordinary generic and unprojected ulong operations retain their prior semantics",delegate{
   var reference=new Field<object>();Unknown(reference,"New object cell unknown");reference.Write(null);Same(null,reference.Read(0),"Known null distinct from unknown");var captured=reference.Snapshot();reference.Write(new object());Same(null,captured.Read(0),"Snapshot retained known null");reference.CopyFrom(captured);Same(null,reference.Read(0),"Known null copies");reference.Forget();Unknown(reference,"Forget removes null knowledge");
   var integer=Field<int>.Of(-7);var integerCopy=integer.Snapshot();integer.Write(8);Eq(-7,integerCopy.Read(0),"Ordinary signed snapshot independent");integer.CopyFrom(integer);Eq(8,integer.Read(0),"Ordinary self copy");
   var word=Field<ulong>.Of(0xfedcba9876543210UL);var saved=word.Snapshot();word.Write(1);Eq(0xfedcba9876543210UL,saved.Read(0),"Unprojected whole snapshot");word.CopyFrom(saved);Eq(0xfedcba9876543210UL,word.Read(0),"Unprojected opaque copy");word.CopyFrom(new Field<ulong>());Unknown(word,"Ordinary whole unknown copy");
  });
  Test("Split known word is stable and every subsequent access uses original live halves",delegate{
   var word=Field<ulong>.Of(Bits(0x11223344,0xaabbccdd));var pair=UInt64FieldWords.Split(word);
   Same(pair,UInt64FieldWords.Split(word),"Repeated split returns same pair");Eq(0x11223344u,pair.Low32.Read(0),"Little endian low");Eq(0xaabbccddu,pair.High32.Read(0),"Little endian high");
   pair.Low32.Write(9);Eq(Bits(9,0xaabbccdd),word.Read(0),"Half write immediately visible through original word object");word.Write(Bits(10,11));Eq(10u,pair.Low32.Read(0),"Whole store writes original low");Eq(11u,pair.High32.Read(0),"Whole store writes original high");Same(pair.Low32,UInt64FieldWords.Split(word).Low32,"Whole write did not replace borrowed cell");
  });
  Test("Split unknown permits a known low without inventing sibling padding",delegate{
   var word=new Field<ulong>();var pair=UInt64FieldWords.Split(word);Unknown(pair.Low32,"Unknown low preserved");Unknown(pair.High32,"Unknown high preserved");pair.Low32.Write(123);
   Eq(123u,pair.Low32.Read(0),"Known low is usable alone");Missing(0x7001,delegate{word.Read(0x7001);},"Whole read consumes unknown high at caller instruction");word.Forget();Unknown(pair.Low32,"Forget reaches existing low");Unknown(pair.High32,"Forget reaches existing high");Same(pair,UInt64FieldWords.Split(word),"Forget retains pair identity");
   pair.High32.Write(456);Missing(0x7002,delegate{word.Read(0x7002);},"Unknown low also rejects whole consumption");word.Write(0);Eq(0u,pair.Low32.Read(0),"Explicit whole zero establishes low");Eq(0u,pair.High32.Read(0),"Explicit whole zero establishes high");
  });
  Test("Join borrows authoritative DWORDs without reads writes or invalid duplicate binding",delegate{
   var low=Field<uint>.Of(21);var high=new Field<uint>();var word=UInt64FieldWords.Join(low,high);var pair=UInt64FieldWords.Split(word);
   Same(low,pair.Low32,"Low is exact input cell");Same(high,pair.High32,"High is exact input cell");Eq(21u,low.Read(0),"Join leaves low unchanged");Unknown(high,"Join does not initialize high");Unknown(word,"Partial whole remains unknown");
   high.Write(22);Eq(Bits(21,22),word.Read(0),"Live sibling write completes whole");word.Forget();Unknown(low,"Whole Forget visible in source low");Unknown(high,"Whole Forget visible in source high");
   low.Write(99);Throws<ArgumentException>(delegate{UInt64FieldWords.Join(low,low);},"Reject same cell at both offsets");Eq(99u,low.Read(0),"Rejected duplicate binding has no writes");Throws<ArgumentNullException>(delegate{UInt64FieldWords.Join(low,null);},"Reject missing high");Throws<ArgumentNullException>(delegate{UInt64FieldWords.Split(null);},"Reject missing word");
  });
  Test("Snapshot deeply captures each half before later source changes",delegate{
   var low=Field<uint>.Of(31);var high=new Field<uint>();var live=UInt64FieldWords.Join(low,high);var captured=live.Snapshot();var pair=UInt64FieldWords.Split(captured);
   Check(!Object.ReferenceEquals(low,pair.Low32)&&!Object.ReferenceEquals(high,pair.High32),"Captured halves are independent storage");low.Write(41);high.Write(42);Eq(31u,pair.Low32.Read(0),"Captured old known low");Unknown(pair.High32,"Captured unknown high remains unknown");
   captured.Write(Bits(51,52));Eq(Bits(41,42),live.Read(0),"Writing snapshot does not mutate live source");live.Forget();Eq(Bits(51,52),captured.Read(0),"Forgetting source does not mutate snapshot");
  });
  Test("CopyFrom propagates all four knowledge states into ordinary and already bound destinations",delegate{
   for(int mask=0;mask<4;mask++){
    var low=new Field<uint>();var high=new Field<uint>();if((mask&1)!=0)low.Write(61);if((mask&2)!=0)high.Write(62);var source=UInt64FieldWords.Join(low,high);var ordinary=Field<ulong>.Of(UInt64.MaxValue);ordinary.CopyFrom(source);var a=UInt64FieldWords.Split(ordinary);
    Half(a.Low32,(mask&1)!=0,61,"Ordinary destination retains low knowledge");Half(a.High32,(mask&2)!=0,62,"Ordinary destination retains high knowledge");Check(!Object.ReferenceEquals(low,a.Low32)&&!Object.ReferenceEquals(high,a.High32),"Copy does not borrow source cells");
    var bound=Field<ulong>.Of(7);var b=UInt64FieldWords.Split(bound);bound.CopyFrom(source);Same(b,UInt64FieldWords.Split(bound),"Bound destination pair is never replaced");Half(b.Low32,(mask&1)!=0,61,"Bound destination low");Half(b.High32,(mask&2)!=0,62,"Bound destination high");
    low.Write(91);high.Write(92);Half(a.Low32,(mask&1)!=0,61,"Later source mutation cannot repair captured low");Half(b.High32,(mask&2)!=0,62,"Later source mutation cannot repair captured high");
   }
   var target=Field<ulong>.Of(9);var pair=UInt64FieldWords.Split(target);target.CopyFrom(Field<ulong>.Of(Bits(71,72)));Eq(71u,pair.Low32.Read(0),"Ordinary known source fills low");Eq(72u,pair.High32.Read(0),"Ordinary known source fills high");target.CopyFrom(new Field<ulong>());Unknown(pair.Low32,"Ordinary unknown clears low knowledge");Unknown(pair.High32,"Ordinary unknown clears high knowledge");
  });
  Test("Self and swapped half overlap capture both source halves before destination stores",delegate{
   var a=Field<uint>.Of(81);var b=Field<uint>.Of(82);var source=UInt64FieldWords.Join(a,b);source.CopyFrom(source);Eq(Bits(81,82),source.Read(0),"Self copy retains value");var swapped=UInt64FieldWords.Join(b,a);swapped.CopyFrom(source);Eq(82u,a.Read(0),"Second store uses captured old high");Eq(81u,b.Read(0),"First store uses captured old low");
   a.Write(83);b.Forget();swapped.CopyFrom(source);Unknown(a,"Unknown captured high overwrites destination high even after first store");Eq(83u,b.Read(0),"Known captured low survives swap");
   var c=Field<uint>.Of(84);var overlap=UInt64FieldWords.Join(a,c);overlap.CopyFrom(swapped);Eq(83u,a.Read(0),"Cross-pair overlap copies old low");Unknown(c,"Cross-pair high stays unknown from pre-store snapshot");
  });
  Test("DWORD position bridge preserves original view identity and partial padding both ways",delegate{
   var p=Point(91,92,93);var bridge=WorldPositionWordBridge.FromDwords(p);Same(p,bridge.Dwords,"Round trip returns original DWORD view");Same(p.AddressIdentity,bridge.AddressIdentity,"Original address token retained");Same(p.Q0,bridge.Words.Q0,"Region qword exact shared cell");Same(p.D10,UInt64FieldWords.Split(bridge.Words.Q16).Low32,"Z shares old Q16 low");Same(p.D14,UInt64FieldWords.Split(bridge.Words.Q16).High32,"Padding shares old Q16 high");
   Unknown(bridge.Words.Q16,"Unknown padding does not become zero");p.D10.Write(94);Eq(94u,UInt64FieldWords.Split(bridge.Words.Q16).Low32.Read(0),"Live Z update visible");bridge.Words.Q8.Write(Bits(95,96));Eq(95u,p.D8.Read(0),"Whole XY store reaches original X");Eq(96u,p.DC.Read(0),"Whole XY store reaches original Y");
   var alias=new WorldPosition24(p);Same(p.AddressIdentity,alias.AddressIdentity,"Existing same-address ctor unchanged");Same(p.D14,alias.D14,"Existing alias shares unknown padding");
  });
  Test("Legacy position bridge retains original words and repeatable address token",delegate{
   var words=new WorldPosition();words.Q0.Write(101);words.Q8.Write(Bits(102,103));var bridge=WorldPositionWordBridge.FromWords(words);var repeated=WorldPositionWordBridge.FromWords(words);
   Same(words,bridge.Words,"Round trip returns original word view");Same(words,bridge.AddressIdentity,"Default OWN address identity is original word view");Same(bridge.AddressIdentity,repeated.AddressIdentity,"Repeated binding retains default token");Same(bridge.Dwords.D8,repeated.Dwords.D8,"Repeated split borrows identical low cell");Same(bridge.Dwords.D14,repeated.Dwords.D14,"Repeated split borrows identical high cell");
   bridge.Dwords.D10.Write(104);Unknown(words.Q16,"Unwritten padding remains unknown");words.Q16.Write(Bits(105,106));Eq(105u,bridge.Dwords.D10.Read(0),"Original word store reaches earlier borrowed Z");Eq(106u,bridge.Dwords.D14.Read(0),"Original word store reaches earlier borrowed padding");
   var explicitToken=new object();var explicitBridge=WorldPositionWordBridge.FromWords(words,explicitToken);Same(explicitToken,explicitBridge.AddressIdentity,"Explicit known address token retained");Same(words,explicitBridge.Words,"Explicit binding never replaces word object");
  });
  Test("Shared-field constructor borrows cells and never derives identity from matching contents",delegate{
   var p=new WorldPosition24();var token=new object();var alias=new WorldPosition24(token,p.Q0,p.D8,p.DC,p.D10,p.D14);Same(token,alias.AddressIdentity,"Explicit token borrowed");Same(p.D8,alias.D8,"Exact DWORD borrowed");Unknown(alias.D8,"Construction leaves value unknown");
   var other=new WorldPosition24(new object(),p.Q0,new Field<uint>(),new Field<uint>(),new Field<uint>(),new Field<uint>());Check(!Object.ReferenceEquals(alias.AddressIdentity,other.AddressIdentity),"Sharing only region does not merge position addresses");
   Throws<ArgumentNullException>(delegate{new WorldPosition24(token,p.Q0,p.D8,p.DC,p.D10,null);},"Reject missing padding cell without default");Throws<ArgumentNullException>(delegate{WorldPositionWordBridge.FromDwords(null);},"Reject missing source");
  });
  Test("Field-level whole capture preserves tail before an overlapping first16 store",delegate{
   // This models the three captures stated by SetPathPosition's saved source;
   // it is not a claim that its fresh hidden local accepts this input alias.
   var target=new WorldPosition();var a=UInt64FieldWords.Split(target.Q0);var b=UInt64FieldWords.Split(target.Q8);a.Low32.Write(111);b.Low32.Write(112);
   var source=new WorldPosition(Field<ulong>.Of(4),target.Q0,target.Q8);var first=source.Q0.Snapshot();var second=source.Q8.Snapshot();var tail=source.Q16.Snapshot();
   target.Q0.CopyFrom(first);target.Q8.CopyFrom(second);target.Q16.CopyFrom(tail);var result=UInt64FieldWords.Split(target.Q16);Eq(112u,result.Low32.Read(0),"Early tail capture retains pre-store112");Unknown(result.High32,"Tail capture preserves unknown sibling");Eq(111u,UInt64FieldWords.Split(target.Q8).Low32.Read(0),"First16 changed overlapping live source tail to111");
  });
  Test("Actual082B10 copies known Z and unknown padding through legacy word fields",delegate{
   var start=Point(121,122,123);start.DC.Forget();var end=Point(131,132,133);var startBridge=WorldPositionWordBridge.FromDwords(start);var endBridge=WorldPositionWordBridge.FromDwords(end);
   var action=new MoveActionRecord();var primary=new GameReachedGoalActionView(action);var state=GameMoveActionConstructionState.Bind(action,primary);var call=new GameMoveActionConstructionInvocation();
   // Destination view is borrowed before the ctor, so CopyFrom must retain it.
   var borrowedStart=WorldPositionWordBridge.FromWords(action.Start48);var savedZ=borrowedStart.Dwords.D10;var savedPad=borrowedStart.Dwords.D14;
   Same(action,GameMoveActionConstructionContracts.Construct082B10(state,Field<uint>.Of(8),startBridge.Words,endBridge.Words,Field<uint>.Of(0),Field<uint>.Of(UInt32.MaxValue),Field<float>.Of(100),Field<uint>.Of(5),call),"Actual ctor returns original action");Check(call.Returned,"Actual ctor completed its known copy contract");
   Same(savedZ,borrowedStart.Dwords.D10,"Constructor does not replace borrowed Z cell");Same(savedPad,borrowedStart.Dwords.D14,"Constructor does not replace borrowed pad cell");Eq(123u,savedZ.Read(0),"Start Z survives partial Q16");Unknown(savedPad,"Start padding remains unknown");Eq(121u,borrowedStart.Dwords.D8.Read(0),"Start X survives partial Q8");Unknown(borrowedStart.Dwords.DC,"Start unknown Y remains unknown");
   var copiedEnd=WorldPositionWordBridge.FromWords(action.Destination60);Eq(133u,copiedEnd.Dwords.D10.Read(0),"Previously ordinary destination carries partial Z");Unknown(copiedEnd.Dwords.D14,"Ordinary destination did not drop partial knowledge or synthesize pad");start.D10.Write(199);Eq(123u,savedZ.Read(0),"Native copy is independent of later source update");
  });
 }

 sealed class PathPrefixProbe:MoveManagerBoundaries {
  public readonly WorldPosition24 Source;
  public readonly WorldPosition IgnoredReturn=new WorldPosition();
  public WorldPosition Hidden,Repositioned;
  public int CoordinateCalls,RepositionCalls;
  public PathPrefixProbe(MoveContext context,WorldPosition24 source):base(context){Source=source;IgnoredReturn.Q0.Write(777);}
  public override WorldPosition Coords(MoveCharacter role,WorldPosition hidden){CoordinateCalls++;Hidden=hidden;hidden.Q0.Write(888);hidden.Q8.Write(999);hidden.Q16.Write(1111);Source.D10.Write(223);return IgnoredReturn;}
  public override void Reposition609BB8(object world,MoveCharacter role,WorldPosition position){RepositionCalls++;Repositioned=position;throw new MissingContract(0x609bb8,"Fixture records copy prefix; native reposition remains unbound");}
 }
 public static void RunAffected(){scenarios=assertions=0;
  Test("Actual old Target16+8 reads overlapping partial tail after first16 store",delegate{
   var role=new MoveCharacter();var target=role.AttackPosition2690;var first=UInt64FieldWords.Split(target.Q0);var second=UInt64FieldWords.Split(target.Q8);first.Low32.Write(201);second.Low32.Write(202);target.Q16.Write(9);role.AttackSkill26A8.Write(71);
   var source=new WorldPosition(Field<ulong>.Of(4),target.Q0,target.Q8);var i=new Invocation();
   Missing(0x531c50,delegate{MoveContracts.SetAttackTarget(role,4,source,55,new MoveBoundaries(new MoveContext()),i);},"Unresolved skill lookup stops after actual position copy");
   Eq(0x05e6a2u,i.At,"Production reached its skill-call boundary");Eq(4UL,target.Q0.Read(0),"Actual first-word store");Eq(201u,UInt64FieldWords.Split(target.Q8).Low32.Read(0),"Actual second word copied original first");var tail=UInt64FieldWords.Split(target.Q16);Eq(201u,tail.Low32.Read(0),"Tail read observes changed live source, not early202 snapshot");Unknown(tail.High32,"Late tail retains unknown high");Eq(71u,role.AttackSkill26A8.Read(0),"Failed dependency retains old skill");
  });
  Test("Actual old Target copy retains partial XYZ without reading padding",delegate{
   var role=new MoveCharacter();var p=Point(211,212,213);p.DC.Forget();var source=WorldPositionWordBridge.FromDwords(p);var target=WorldPositionWordBridge.FromWords(role.AttackPosition2690);var i=new Invocation();
   Missing(0x531c50,delegate{MoveContracts.SetAttackTarget(role,9,source.Words,8,new MoveBoundaries(new MoveContext()),i);},"Known copy prefix does not invent skill lookup");Eq(211u,target.Dwords.D8.Read(0),"Known X copied through partial Q8");Unknown(target.Dwords.DC,"Unknown Y copied");Eq(213u,target.Dwords.D10.Read(0),"Known Z copied through partial Q16");Unknown(target.Dwords.D14,"Unknown padding copied");Eq(9u,role.AttackFlags2688.Read(0),"Flags write before copy retained");
  });
  Test("Actual old SetPath copies fresh partial input into its own hidden buffer before failure",delegate{
   var role=new MoveCharacter();var manager=new MovementManager();manager.Moving38.Write(7);role.MovementDC0.Write(manager);var context=new MoveContext();var engine=new WorldEngine();engine.World50.Write(new object());context.EngineGlobal.Write(engine);
   var p=Point(221,222,9);var source=WorldPositionWordBridge.FromDwords(p);var probe=new PathPrefixProbe(context,p);var i=new Invocation();
   Missing(0x609bb8,delegate{MoveManagerContracts.SetPathPosition(role,source.Words,probe,i);},"Reposition remains an explicit unsupported import");Eq(0x05d9e4u,i.At,"Actual reposition-call boundary");Eq(1,probe.CoordinateCalls,"One old Coords call");Eq(1,probe.RepositionCalls,"One copy-prefix handoff");Same(probe.Hidden,probe.Repositioned,"Actual hidden local, not returned position, passed onward");Check(!Object.ReferenceEquals(probe.IgnoredReturn,probe.Repositioned),"Coords returned pointer is ignored");
   var copied=WorldPositionWordBridge.FromWords(probe.Repositioned);Eq(77UL,copied.Dwords.Q0.Read(0),"Source region replaced hidden initial value");Eq(221u,copied.Dwords.D8.Read(0),"X copied");Eq(222u,copied.Dwords.DC.Read(0),"Y copied");Eq(223u,copied.Dwords.D10.Read(0),"Source read after Coords mutation");Unknown(copied.Dwords.D14,"Whole tail copy propagates unknown padding over known hidden tail");Eq(777UL,probe.IgnoredReturn.Q0.Read(0),"Ignored returned object untouched");Eq((byte)7,manager.Moving38.Read(0),"Reposition failure prevents later manager clear");
  });
 }
}

public static class GameMoveActionConstructionValidationProgram {
 public static int Main(string[] args){
  try{
   if(args.Length!=1||(args[0]!="new-components"&&args[0]!="affected-regression"))throw new ArgumentException("Use fixed new-components or affected-regression mode");
   bool fresh=args[0]=="new-components";int constructors=0,constructorAssertions=0;
   if(fresh){GameMoveActionConstructionTests.Run();constructors=GameMoveActionConstructionTests.Scenarios;constructorAssertions=GameMoveActionConstructionTests.Assertions;WorldPositionWordBridgeTests.RunNew();}else WorldPositionWordBridgeTests.RunAffected();
   int bridge=fresh?WorldPositionWordBridgeTests.Scenarios:0,regression=fresh?0:WorldPositionWordBridgeTests.Scenarios;
   Console.WriteLine("{\"status\":\"passed-move-action-construction-"+args[0]+"\",\"scenarios\":"+(constructors+bridge+regression)+",\"assertions\":"+(constructorAssertions+WorldPositionWordBridgeTests.Assertions)+",\"constructor_scenarios\":"+constructors+",\"bridge_scenarios\":"+bridge+",\"affected_regression_scenarios\":"+regression+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"physical_mouse\":false,\"walking_accepted\":false,\"action_submitted\":false,\"system_library_loaded\":false,\"gameplay_accepted\":false}");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}
 }
}
