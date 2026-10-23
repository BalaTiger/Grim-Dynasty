using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// New uncompiled component cases. Selector cases use the actual fixed system
// UCRT crowd view, never a fake math callback. No tick or game acceptance is
// claimed. Constants are the explicit observed-disk own profile.
public static class NavigationCrowdAvoidanceTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool condition,string why){assertions++;if(!condition)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Bits(float expected,float actual,string why){Equal(NavigationCoreContracts.ToBits(expected),NavigationCoreContracts.ToBits(actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected MissingContract: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static NavigationPoint3 Point(float x,float y,float z){return new NavigationPoint3(Field<float>.Of(x),Field<float>.Of(y),Field<float>.Of(z));}
 static NavigationCrowdAvoidanceConstants Constants(){var k=new NavigationCrowdAvoidanceConstants();k.UseObservedDiskConstants();return k;}
 static NavigationCrowdAvoidanceVector Vector(int count,out NavigationCrowdAvoidanceBuffer buffer){var v=new NavigationCrowdAvoidanceVector(new NavigationCrowdOpaqueVector());buffer=new NavigationCrowdAvoidanceBuffer(count);v.InstallOwnStorage(buffer,count);return v;}
 static void Write(NavigationCrowdAvoidanceBuffer b,int n,uint offset,float value){b.Cell(n,offset,0).Write(value);}
 static float Read(NavigationCrowdAvoidanceBuffer b,int n,uint offset){return b.Cell(n,offset,0).Read(0);}
 static void SeedPreprocess(NavigationCrowdAvoidanceBuffer b,int n,float x,float y,float z,float vx,float vz){Write(b,n,0,x);Write(b,n,4,y);Write(b,n,8,z);Write(b,n,0x1c,vx);Write(b,n,0x24,vz);}
 static void SeedScore(NavigationCrowdAvoidanceBuffer b,int n,float x,float z,float sideX){Write(b,n,0,x);Write(b,n,8,z);Write(b,n,0xc,1);Write(b,n,0x10,0);Write(b,n,0x18,0);Write(b,n,0x28,1);Write(b,n,0x30,0);Write(b,n,0x34,sideX);Write(b,n,0x3c,0);}
 static float Score(NavigationCrowdAvoidanceVector v){return NavigationCrowdAvoidanceContracts.Score25EF20(v,Point(1,0,0),Point(0,0,0),1,Point(0,0,0),Point(1,0,0),1,Constants(),new Invocation());}
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Group.Vector80 header is the authoritative shared storage",delegate{
   var group=new NavigationCrowdGroup();var a=new NavigationCrowdAvoidanceVector(group.Vector80);var b=new NavigationCrowdAvoidanceVector(group.Vector80);
   Same(group.Vector80,a.Header,"Header identity");Same(group.Vector80.Begin00,a.Begin00,"Begin cell");Same(group.Vector80.End08,a.End08,"End cell");Same(group.Vector80.Capacity10,a.Capacity10,"Capacity cell");Same(a.End08,b.End08,"Two views alias");
   Unknown(a.Begin00,"Construction does not initialize");var storage=new NavigationCrowdAvoidanceBuffer(1);a.InstallOwnStorage(storage,1);Same(a.Begin00.Read(0),b.Begin00.Read(0),"Same pointer value");
   SeedPreprocess(storage,0,1,0,0,0,0);NavigationCrowdAvoidanceContracts.Preprocess25F250(b,Point(0,0,0),Point(0,0,0),Constants(),new Invocation());Bits(1,Read(storage,0,0x28),"Write reaches original storage");Unknown(group.Owner00,"Unrelated group state unchanged");
   group.Vector80.End08.CopyFrom(group.Vector80.Begin00);NavigationCrowdAvoidanceContracts.Preprocess25F250(a,null,null,new NavigationCrowdAvoidanceConstants(),new Invocation());Bits(1,Read(storage,0,0x28),"Late header change observed without synchronization");
  });
  Test("Empty preprocess consumes no point or unknown scalar defaults",delegate{
   var v=new NavigationCrowdAvoidanceVector(new NavigationCrowdOpaqueVector());v.Begin00.Write(null);v.End08.Write(null);var k=new NavigationCrowdAvoidanceConstants();var i=new Invocation();
   NavigationCrowdAvoidanceContracts.Preprocess25F250(v,null,null,k,i);Equal(0x25f3f4u,i.At,"Prefix endpoint only");Unknown(k.One34BB44,"No invented one");Unknown(k.SignMask34C450,"No invented sign");Unknown(v.Capacity10,"Capacity unconsumed");
  });
  Test("Preprocess normalizes all three axes but leaves side Y and other fields unknown",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedPreprocess(b,0,0,3,4,0,0);var i=new Invocation();NavigationCrowdAvoidanceContracts.Preprocess25F250(v,Point(0,0,0),Point(0,99,0),Constants(),i);
   Bits(0,Read(b,0,0x28),"Normalized X");Bits(0.6f,Read(b,0,0x2c),"Normalized Y");Bits(0.8f,Read(b,0,0x30),"Normalized Z");Bits(-0.8f,Read(b,0,0x34),"Else-side X");Bits(0,Read(b,0,0x3c),"Else-side Z");
   Unknown(b.Cell(0,0x38,0),"Side Y never written");Unknown(b.Cell(0,0x20,0),"Desired Y never read/written");Unknown(b.Cell(0,0xc,0),"Radius not needed by preprocess");Equal(0x25f3f4u,i.At,"No false selector completion");
  });
  Test("Both side branches preserve signed zeros",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(2,out b);SeedPreprocess(b,0,1,0,0,0,-1);SeedPreprocess(b,1,1,0,0,0,0);NavigationCrowdAvoidanceContracts.Preprocess25F250(v,Point(0,0,0),Point(0,0,0),Constants(),new Invocation());
   Equal(0u,NavigationCoreContracts.ToBits(Read(b,0,0x34)),"Positive cross keeps positive zero");Bits(-1,Read(b,0,0x3c),"Positive cross flips X");Equal(0x80000000u,NavigationCoreContracts.ToBits(Read(b,1,0x34)),"Other branch flips zero sign");Bits(1,Read(b,1,0x3c),"Other branch copies X");
  });
  Test("A later unknown record preserves the completed first record",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(2,out b);SeedPreprocess(b,0,1,0,0,0,0);Write(b,1,0,2);
   Missing(0x25f328,delegate{NavigationCrowdAvoidanceContracts.Preprocess25F250(v,Point(0,0,0),Point(0,0,0),Constants(),new Invocation());},"Second-record unknown Y consumed by SUBSS");Bits(1,Read(b,0,0x28),"Prior normal retained");Bits(1,Read(b,0,0x3c),"Prior side retained");Unknown(b.Cell(1,0x28,0),"Second record not written");
  });
  Test("Unknown threshold or sign stops after the exact write prefix",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedPreprocess(b,0,1,0,0,0,-1);var k=Constants();k.SideThreshold34B9E0.Forget();
   Missing(0x25f3b0,delegate{NavigationCrowdAvoidanceContracts.Preprocess25F250(v,Point(0,0,0),Point(0,0,0),k,new Invocation());},"Threshold first consumed at compare");Bits(1,Read(b,0,0x28),"Normal prefix written");Unknown(b.Cell(0,0x34,0),"Side not written before compare");
   k=Constants();k.SignMask34C450.Forget();Missing(0x25f3d1,delegate{NavigationCrowdAvoidanceContracts.Preprocess25F250(v,Point(0,0,0),Point(0,0,0),k,new Invocation());},"Positive-branch sign first consumed after sideX store");Bits(0,Read(b,0,0x34),"SideX prefix retained");Unknown(b.Cell(0,0x3c,0),"SideZ still unknown");
  });
  Test("Same-typed aliases observe the real late normalized-X reread",delegate{
   var cells=new Field<float>[16];for(int n=0;n<16;n++)cells[n]=new Field<float>();cells[13]=cells[10];var b=new NavigationCrowdAvoidanceBuffer(cells);var v=new NavigationCrowdAvoidanceVector(new NavigationCrowdOpaqueVector());v.InstallOwnStorage(b,1);SeedPreprocess(b,0,0,0,1,1,0);
   NavigationCrowdAvoidanceContracts.Preprocess25F250(v,Point(0,0,0),Point(0,0,0),Constants(),new Invocation());Same(b.Cell(0,0x28,0),b.Cell(0,0x34,0),"Same cell, no synchronized duplicate");Bits(1,Read(b,0,0x28),"SideX write replaces aliased normalizedX");Bits(-1,Read(b,0,0x3c),"Late reread sees replacement");
  });
  Test("Coincident normalization is an explicit domain stop with no invented direction",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedPreprocess(b,0,0,0,0,0,0);Missing(0x25f371,delegate{NavigationCrowdAvoidanceContracts.Preprocess25F250(v,Point(0,0,0),Point(0,0,0),Constants(),new Invocation());},"Finite domain excludes division by zero");Unknown(b.Cell(0,0x28,0),"No zero-vector success");Unknown(b.Cell(0,0x34,0),"No side-vector success");
  });
  Test("Empty-neighbor score still has time and velocity penalties",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);v.Capacity10.Forget();var i=new Invocation();var c=Point(0,0,0);c.Y.Forget();var current=Point(0,0,0);current.Y.Forget();var desired=Point(0,0,0);desired.Y.Forget();
   float score=NavigationCrowdAvoidanceContracts.Score25EF20(v,c,null,Single.NaN,current,desired,1,Constants(),i);Bits(2.2727273f,score,"2.5/(2.5*0.4+0.1), not zero");Equal(0x25f246u,i.At,"Return XMM0");Unknown(v.Capacity10,"Capacity unused");
   desired.X.Write(1);Bits(12.272727f,NavigationCrowdAvoidanceContracts.Score25EF20(v,c,null,0,current,desired,1,Constants(),new Invocation()),"Desired displacement weighted by ten");
  });
  Test("Head-on collision produces actual earliest-time penalty",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedScore(b,0,4,0,0);Bits(5.75f,Score(v),"Relative speed2, entry t1, time penalty5 plus current penalty0.75");Unknown(b.Cell(0,4,0),"Y not consumed by score");Unknown(b.Cell(0,0x38,0),"Unwritten sideY not consumed");Bits(4,Read(b,0,0),"Scoring does not modify record");
  });
  Test("Overlap chooses the negative-half adjusted entry time",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedScore(b,0,1,0,0);Bits(13.25f,Score(v),"Entry -0.5 and exit1.5 adjust entry to0.25, time penalty12.5");
  });
  Test("Behind and negative-discriminant obstacles do not shorten the horizon",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedScore(b,0,-4,0,0);Bits(3.0227273f,Score(v),"Both times negative keep horizon");Write(b,0,0,4);Write(b,0,8,5);Bits(3.0227273f,Score(v),"Negative discriminant keeps horizon");
  });
  Test("Side scores clamp and average over the captured count",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(2,out b);SeedScore(b,0,-4,0,1);SeedScore(b,1,-4,0,0);Bits(3.3977273f,Score(v),"One clamped side1 plus side0 gives mean0.5 and penalty0.375");
   v.End08.Write(new NavigationCrowdAvoidanceAddress(b,1));Bits(3.7727273f,Score(v),"Live caller header change is consumed by next call");
  });
  Test("Tiny relative velocity avoids the quadratic and keeps unknown negative-half unused",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedScore(b,0,4,0,0);var k=Constants();k.NegativeHalf34BDE8.Forget();var zero=Point(0,0,0);
   Bits(2.2727273f,NavigationCrowdAvoidanceContracts.Score25EF20(v,zero,zero,1,zero,zero,1,k,new Invocation()),"Relative0 below epsilon");Unknown(k.NegativeHalf34BDE8,"Not consumed on skip");
  });
  Test("Score unknowns stop at consumption without eager optional-field reads",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var k=Constants();k.Horizon34BC00.Forget();var zero=Point(0,0,0);Missing(0x25f1f2,delegate{NavigationCrowdAvoidanceContracts.Score25EF20(v,zero,null,0,zero,zero,1,k,new Invocation());},"Captured horizon first used at time multiplication");
   v=Vector(1,out b);SeedScore(b,0,4,0,0);b.Cell(0,0x3c,0).Forget();Missing(0x25f052,delegate{Score(v);},"Missing sideZ not zero-filled");Bits(4,Read(b,0,0),"No score writes on failure");
  });
  Test("Invalid headers and released storage fail before invented record reads",delegate{
   var v=new NavigationCrowdAvoidanceVector(new NavigationCrowdOpaqueVector());var k=Constants();Missing(0x25f2df,delegate{NavigationCrowdAvoidanceContracts.Preprocess25F250(v,null,null,k,new Invocation());},"Unknown count subtraction");
   v.Begin00.Write(new object());v.End08.Write(null);Missing(0x25ef31,delegate{Score(v);},"No unknown header binding");NavigationCrowdAvoidanceBuffer b;v=Vector(1,out b);b.MarkReleased();Missing(0x25f31b,delegate{NavigationCrowdAvoidanceContracts.Preprocess25F250(v,null,null,k,new Invocation());},"Released record allocation");
  });
  Test("Preprocess and scoring compose on shared production records",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedPreprocess(b,0,4,0,0,0,0);Write(b,0,0xc,1);Write(b,0,0x10,0);Write(b,0,0x18,0);var p=Point(0,0,0);var desired=Point(1,0,0);
   NavigationCrowdAvoidanceContracts.Preprocess25F250(v,p,desired,Constants(),new Invocation());Bits(5.75f,NavigationCrowdAvoidanceContracts.Score25EF20(v,desired,p,1,Point(0,0,0),desired,1,Constants(),new Invocation()),"Real prefix output feeds real score without fake callback");Unknown(b.Cell(0,0x38,0),"Unwritten field stays unknown across composition");
  });
 }
 public static void RunSelector(SystemUcrtRotationMath owner){
  var math=owner.Crowd;
  Test("Disk profile leaves the unbacked fallback unknown",delegate{
   var k=Constants();Equal(0x3eaaaaabu,NavigationCoreContracts.ToBits(k.Third34BA9C.Read(0)),"Third raw bits");Equal(0x3ee5c8fbu,NavigationCoreContracts.ToBits(k.HalfStep34BAD0.Read(0)),"Half-step raw bits");Equal(0x3f65c8fbu,NavigationCoreContracts.ToBits(k.Step34BB18.Read(0)),"Step raw bits");Unknown(k.Fallback422980.X,"BSS X unknown");Unknown(k.Fallback422980.Y,"BSS Y unknown");Unknown(k.Fallback422980.Z,"BSS Z unknown");
  });
  Test("Zero desired velocity selects original zero with all43 real platform calls",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var zero=Point(0,0,0);var output=Point(71,72,73);var k=Constants();var i=new Invocation();long before=owner.NativeMathCalls;
   NavigationCrowdAvoidanceContracts.Select25F250(v,null,Single.NaN,2,zero,zero,output,k,math,i);Bits(0,output.X.Read(0),"Selected X");Bits(0,output.Y.Read(0),"Selected Y explicitly zero");Bits(0,output.Z.Read(0),"Selected Z");Equal(43L,owner.NativeMathCalls-before,"atan2 plus21cos plus21sin");Equal(0x25f67au,i.At,"Actual selector return");Unknown(k.Fallback422980.X,"Unknown fallback overwritten locally, not read or initialized");Unknown(k.Fallback422980.Y,"No BSS Y write");Unknown(k.Fallback422980.Z,"No BSS Z write");
  });
  Test("Unobstructed unit demand selects actual unit speed and preserves unknown inputY",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var demand=Point(1,0,0);demand.Y.Forget();var current=Point(1,0,0);current.Y.Forget();var output=new NavigationPoint3();
   NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,1,current,demand,output,Constants(),math,new Invocation());Bits(1,output.X.Read(0),"Best exact forward sample");Bits(0,output.Y.Read(0),"Selected Y");Bits(0,output.Z.Read(0),"Forward Z");Unknown(demand.Y,"DemandY was not consumed");Unknown(current.Y,"CurrentY was not consumed");
  });
  Test("Negative-X demand exercises the crowd angle domain beyond oldrotation4",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var demand=Point(-1,0,0);var output=new NavigationPoint3();long before=owner.NativeMathCalls;
   NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,1,demand,demand,output,Constants(),math,new Invocation());float x=output.X.Read(0),z=output.Z.Read(0);Check(x< -0.9f,"Motion candidate points toward negativeX");Check(Math.Abs(z)<0.1f,"Small platform sin(pi) residual does not reverse demand");Check((double)x*x+(double)z*z<1.0021,"Selected speed remains within observed tolerance");Equal(43L,owner.NativeMathCalls-before,"All angles accepted by crowd view");
  });
  Test("Real obstacle records affect selection without an injected collision callback",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedPreprocess(b,0,1,0,0,0,0);Write(b,0,0xc,1);Write(b,0,0x10,0);Write(b,0,0x18,0);var position=Point(0,0,0);var current=Point(0,0,0);var desired=Point(1,0,0);var output=new NavigationPoint3();var k=Constants();
   NavigationCrowdAvoidanceContracts.Select25F250(v,position,1,1,current,desired,output,k,math,new Invocation());float chosen=NavigationCrowdAvoidanceContracts.Score25EF20(v,output,position,1,current,desired,1,k,new Invocation());float straight=NavigationCrowdAvoidanceContracts.Score25EF20(v,desired,position,1,current,desired,1,k,new Invocation());Check(chosen<=straight,"Three refinements preserve or improve observed collision score");float x=output.X.Read(0),z=output.Z.Read(0);Check((double)x*x+(double)z*z<1.0021,"Speed admissible");Bits(1,Read(b,0,0x28),"Preprocessor published into actual record");Unknown(b.Cell(0,0x38,0),"Unwritten sideY remains unknown");
  });
  Test("Missing platform stops only after the committed record prefix",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(1,out b);SeedPreprocess(b,0,1,0,0,0,0);var output=Point(71,72,73);var zero=Point(0,0,0);long before=owner.NativeMathCalls;
   Missing(0x25f40c,delegate{NavigationCrowdAvoidanceContracts.Select25F250(v,zero,1,1,zero,zero,output,Constants(),null,new Invocation());},"No substitute atan2");Bits(1,Read(b,0,0x28),"Completed prefix retained");Bits(71,output.X.Read(0),"No partial output");Equal(before,owner.NativeMathCalls,"No native call silently made");
  });
  Test("Zero maximum speed stops atdivision before the first platform call",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var zero=Point(0,0,0);var output=Point(71,72,73);long before=owner.NativeMathCalls;
   Missing(0x25f407,delegate{NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,0,zero,zero,output,Constants(),math,new Invocation());},"Explicit finite arithmetic boundary");Equal(before,owner.NativeMathCalls,"No atan2 before division");Bits(71,output.X.Read(0),"OutputX retained");Bits(72,output.Y.Read(0),"OutputY retained");Bits(73,output.Z.Read(0),"OutputZ retained");
  });
  Test("Unknown sampling constant stops afteratan2 but beforetrig or output",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var zero=Point(0,0,0);var output=Point(71,72,73);var k=Constants();k.Third34BA9C.Forget();long before=owner.NativeMathCalls;
   Missing(0x25f46e,delegate{NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,1,zero,zero,output,k,math,new Invocation());},"Third consumed at radiusfactor multiplication");Equal(1L,owner.NativeMathCalls-before,"Only true atan2 call");Bits(71,output.X.Read(0),"Output retained");
  });
  Test("Unknown fallback is required only when no admissible sample replaces it",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var demand=Point(100,0,0);var output=Point(71,72,73);var k=Constants();long before=owner.NativeMathCalls;
   Missing(0x25f552,delegate{NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,1,null,demand,output,k,math,new Invocation());},"Next refinement consumes unknown fallbackZ at ADDSS, not at MOVSS");Equal(43L,owner.NativeMathCalls-before,"Sampling completed once");Bits(71,output.X.Read(0),"OutputX retained");Bits(72,output.Y.Read(0),"OutputY retained");Bits(73,output.Z.Read(0),"OutputZ retained");Unknown(k.Fallback422980.Z,"No fallback fabricated");
  });
  Test("Unknown best-score sentinel stops at real comparison after firstscore",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var zero=Point(0,0,0);var output=Point(71,72,73);var k=Constants();k.MaxFinite34BDCC.Forget();
   Missing(0x25f5a2,delegate{NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,1,zero,zero,output,k,math,new Invocation());},"No maxscore default");Bits(71,output.X.Read(0),"No output before completed selector");
  });
  Test("Output may alias demand and is changed only by final stores",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var demand=Point(1,77,0);var current=Point(1,0,0);NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,1,current,demand,demand,Constants(),math,new Invocation());Bits(1,demand.X.Read(0),"Aliased X");Bits(0,demand.Y.Read(0),"Final Y store");Bits(0,demand.Z.Read(0),"Aliased Z");
  });
  Test("Missing output is consumed atfinalstore after complete selection",delegate{
   NavigationCrowdAvoidanceBuffer b;var v=Vector(0,out b);var zero=Point(0,0,0);long before=owner.NativeMathCalls;Missing(0x25f667,delegate{NavigationCrowdAvoidanceContracts.Select25F250(v,null,0,1,zero,zero,null,Constants(),math,new Invocation());},"No eager output dereference");Equal(43L,owner.NativeMathCalls-before,"Real sampling completed before store failure");
  });
 }
 public static int Main(){
  try{
   using(var owner=SystemUcrtRotationMath.OpenOwnProfile()){
    uint before=owner.CheckControl(0);long started=owner.NativeMathCalls;Run();RunSelector(owner);uint after=owner.CheckControl(0);long calls=owner.NativeMathCalls-started;string digest=owner.VerifyLockedFingerprint(0);
    Check(scenarios==29,"Exactly the29 new avoidance cases");Check(calls==345,"Eight full43-call samplers and one atan2-before-stop");Check(before==after,"Query-only floating control unchanged");
    Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scenarios,assertions,scoring_preprocess_scenarios=17,selector_scenarios=12,native_math_calls=calls,own_crowd_platform=true,library_sha256=digest,exports=owner.ExportRvas,control_before=before,control_after=after,control_mask_written=0,memory_image_hashed=false,bss_defaults_inferred=false,original_read=false,original_executed=false,tick_executed=false,character_movement_validated=false,gameplay_accepted=false,original_runtime_equivalence=false}));
   }
   return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}
 }
}
