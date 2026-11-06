using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New leaf tests only. OWN field observations use the original Entity/Actor/
// borrowed mesh identities and execute the actual saved06DA30 contract.
// No resource loader, fabricated animation controller, speed default, Play
// callback or native runtime observation is supplied by these tests.
public static class EngineAnimationSpeedTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected OWN admission rejection: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 sealed class Fixture {
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly EntityMovementState Entity=new EntityMovementState(new object());
  public readonly ActorBoundingBoxContext Bounds;
  public readonly ActorBoundingBoxState Actor;
  public readonly BorrowedMeshBounds First,Second;
  public readonly EngineAnimationSpeedContext Context;
  public readonly EngineMeshAnimationSpeedFields FirstFields,SecondFields;
  public readonly EngineAnimationSpeedInvocation Call=new EngineAnimationSpeedInvocation();
  public Fixture(){
   Entities.Attach(Entity);Bounds=new ActorBoundingBoxContext(Entities);Actor=Bounds.Bind(Entity);
   First=Bounds.BindMesh(new object());Second=Bounds.BindMesh(new object());
   Context=new EngineAnimationSpeedContext(Bounds,Actor);FirstFields=Context.BindExistingMesh(First);SecondFields=Context.BindExistingMesh(Second);
   // Explicit OWN observations, not a claim that a native constructor or
   // resource loader has published this primary table or these mesh pointers.
   Entity.Virtual00.Write(GameMovementPrimaryTable.Player);Actor.Mesh2668.Write(First);
  }
  public Field<float> Get(uint index){return EngineAnimationSpeedContracts.GetAnimationSpeed027410(Context,index,Call);}
 }
 public static void Run(){
  scenarios=assertions=0;
  Test("Existing mesh identity, first priority and MOVSS snapshots",delegate{
   var f=new Fixture();f.FirstFields.Speed130.Write(1.25f);Same(f.Bounds,f.Context.Bounds,"Reuse actual bounds context");Same(f.Actor,f.Context.Actor,"Reuse actual actor sidecar");Same(f.First,f.FirstFields.Mesh,"Same borrowed mesh identity");
   var result=f.Get(0);Eq(1.25f,result.Read(0),"Actual first mesh speed");Same(f.First,f.Call.ReturnedMesh,"06DA30 returned original first mesh");Eq(0x06da5du,f.Call.MeshCall.At,"Actual mesh getter return");Eq(0x027434u,f.Call.Native.At,"Engine returned normally");Check(f.Call.PrimaryCaptured&&f.Call.MeshEntered&&f.Call.MeshReturned&&f.Call.SpeedCaptured&&f.Call.Returned,"Complete ordered path");Eq(0L,f.Call.SignedIndex,"Index sign extension");Eq(0L,f.Call.ByteOffset,"Index-zero address displacement");Unknown(f.Actor.Mesh2670,"First nonnull skips unknown second pointer");Unknown(f.First.Bounds1A8.Center00,"No unrelated bounds read or fabricated initialization");
   f.FirstFields.Speed130.Write(2.5f);Eq(1.25f,result.Read(0),"Returned register snapshot does not alias live speed field");Eq(2.5f,f.Get(0).Read(0),"Next call rereads actual field");Eq(1.25f,result.Read(0),"Invocation reuse preserves earlier returned register snapshot");
  });
  Test("Current table and second mesh are reread",delegate{
   var f=new Fixture();f.FirstFields.Speed130.Write(7);f.SecondFields.Speed130.Write(3);f.Actor.Mesh2668.Write(null);f.Actor.Mesh2670.Write(f.Second);f.Entity.Virtual00.Write(GameMovementPrimaryTable.Character);
   Eq(3f,f.Get(0).Read(0),"Character+370 also selects actual06DA30 second mesh");Same(GameMovementPrimaryTable.Character,f.Call.CapturedPrimary.Read(0),"Captured current primary");Same(f.Second,f.Call.ReturnedMesh,"Actual second pointer identity");
   f.Actor.Mesh2668.Write(f.First);f.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);Eq(7f,f.Get(0).Read(0),"Changed first pointer and table take effect next call");Same(f.First,f.Call.ReturnedMesh,"No cached mesh selection");Same(GameMovementPrimaryTable.Player,f.Call.CapturedPrimary.Read(0),"No fixed primary-table cache");
  });
  Test("Unknown pointer priority and double-null fallback preserve failure prefix",delegate{
   var f=new Fixture();f.Actor.Mesh2668.Forget();f.Actor.Mesh2670.Write(f.Second);Missing(0x06da43,delegate{f.Get(0);},"Unknown first pointer cannot be skipped");Check(f.Call.MeshEntered&&!f.Call.MeshReturned&&!f.Call.SpeedCaptured&&!f.Call.Returned,"Getter entered but no mesh return or speed copy");Unknown(f.Call.Result,"No default result after failed getter");
   f.Actor.Mesh2668.Write(null);f.Actor.Mesh2670.Forget();Missing(0x06da43,delegate{f.Get(0);},"Unknown second pointer cannot become fallback");Check(!f.Call.MeshReturned,"Second pointer read has not returned");
   f.Actor.Mesh2670.Write(null);Missing(0x06da53,delegate{f.Get(0);},"Both null reach unclosed Actor import609688");Eq(0x06da53u,f.Call.MeshCall.At,"Real fallback stop");Eq(0x06da53u,f.Call.Native.At,"Outer reports the actual nested stop");Check(!f.Call.MeshReturned&&!f.Call.Returned,"No fabricated fallback mesh");
  });
  Test("Unknown and foreign current primary stop before mesh call",delegate{
   var f=new Fixture();f.FirstFields.Speed130.Write(4);f.Get(0);f.Entity.Virtual00.Forget();Missing(0x02741c,delegate{f.Get(0);},"Unknown captured table is consumed at virtual call");Check(f.Call.PrimaryCaptured&&!f.Call.MeshEntered&&!f.Call.MeshReturned&&!f.Call.Returned,"No reused success or entered getter");Eq(0u,f.Call.MeshCall.At,"No nested call");Unknown(f.Call.Result,"Previous returned speed cleared on reuse");
   f.Entity.Virtual00.Write(new object());Missing(0x02741c,delegate{f.Get(0);},"Unknown table implementation is not defaulted to Player");Check(!f.Call.MeshEntered,"No dispatch from foreign table");f.Entity.Virtual00.Write(null);Missing(0x02741c,delegate{f.Get(0);},"Known-null primary also unsupported");
  });
  Test("Unknown MOVSS returns and payloads are not defaulted or clamped",delegate{
   var f=new Fixture();var result=f.Get(0);Check(f.Call.MeshReturned&&f.Call.SpeedCaptured&&f.Call.Returned,"Unknown speed is an opaque successful MOVSS return");Eq(0x027434u,f.Call.Native.At,"Native getter finished before value consumption");Missing(0x05fa2f,delegate{result.Read(0x05fa2f);},"Actual caller compare consumes unknown speed");Eq(0x027434u,f.Call.Native.At,"Consumer failure does not rewrite getter result");
   f.FirstFields.Speed130.Write(0);Eq(0f,f.Get(0).Read(0),"Observed zero is not replaced by one");f.FirstFields.Speed130.Write(-2);Eq(-2f,f.Get(0).Read(0),"MOVSS has no positivity clamp");f.FirstFields.Speed130.Write(Single.PositiveInfinity);Check(Single.IsPositiveInfinity(f.Get(0).Read(0)),"Infinity unchanged");f.FirstFields.Speed130.Write(Single.NaN);Check(Single.IsNaN(f.Get(0).Read(0)),"NaN not replaced");Unknown(result,"Later known values do not repair prior captured Unknown");
  });
  Test("Undeclared indices execute real getter before access-domain stop",delegate{
   var f=new Fixture();Missing(0x027426,delegate{f.Get(1);},"Index1 has no declared storage after getter");Check(f.Call.MeshEntered&&f.Call.MeshReturned&&!f.Call.SpeedCaptured&&!f.Call.Returned,"Mesh return precedes index storage restriction");Same(f.First,f.Call.ReturnedMesh,"Actual returned identity retained");Eq(1L,f.Call.SignedIndex,"Positive sign extension");Eq(104L,f.Call.ByteOffset,"Actual IMUL stride");
   Missing(0x027426,delegate{f.Get(UInt32.MaxValue);},"EDX is sign-extended rather than unsigned indexing");Eq(-1L,f.Call.SignedIndex,"Signed minus one");Eq(-104L,f.Call.ByteOffset,"Negative native displacement");Check(f.Call.MeshReturned,"Negative index still performs getter first");
   f.Actor.Mesh2668.Write(null);f.Actor.Mesh2670.Write(null);Missing(0x06da53,delegate{f.Get(1);},"Fallback failure wins before storage-domain restriction");Check(!f.Call.MeshReturned&&!f.Call.SpeedCaptured,"No post-return address operation on failed getter");Eq(0L,f.Call.ByteOffset,"IMUL has not executed");
  });
  Test("Explicit binding shares cells and rejects replacement views",delegate{
   var f=new Fixture();Same(f.FirstFields,f.Context.BindExistingMesh(f.First),"Repeated admission shares one field");var secondContext=new EngineAnimationSpeedContext(f.Bounds,f.Actor);Same(f.FirstFields,secondContext.BindExistingMesh(f.First),"Same original mesh shares sidecar across adapters");Unknown(f.FirstFields.Speed130,"Binding does not initialize speed");
   var otherBounds=new ActorBoundingBoxContext(f.Entities);var otherActor=otherBounds.Bind(f.Entity);Invalid(delegate{new EngineAnimationSpeedContext(otherBounds,f.Actor);},"Different actor sidecar cannot replace existing owner");Same(otherActor,otherBounds.Bind(f.Entity),"Rejected binding leaves authoritative actor unchanged");
   var replacement=otherBounds.BindMesh(f.First.Identity);Invalid(delegate{f.Context.BindExistingMesh(replacement);},"Same raw identity with a different mesh view is not interchangeable");Same(f.First,f.Bounds.BindMesh(f.First.Identity),"Original mesh mapping retained");
   f.Actor.Mesh2668.Write(replacement);Missing(0x027426,delegate{f.Get(0);},"Runtime cannot silently rebind returned foreign mesh");Check(f.Call.MeshReturned&&!f.Call.SpeedCaptured,"Foreign return retained but not consumed");Same(replacement,f.Call.ReturnedMesh,"No identity substitution");
   var unbound=f.Bounds.BindMesh(new object());f.Actor.Mesh2668.Write(unbound);Missing(0x027426,delegate{f.Get(0);},"Unbound storage is not synthesized on use");Check(f.Call.MeshReturned&&!f.Call.Returned,"No success for unbound speed storage");
  });
  Test("Released original mesh and owner cannot reuse earlier success",delegate{
   var f=new Fixture();f.FirstFields.Speed130.Write(9);var result=f.Get(0);f.First.MarkReleased();Missing(0x027426,delegate{f.Get(0);},"Borrowed mesh lifetime checked at actual dereference");Check(f.Call.MeshReturned&&!f.Call.SpeedCaptured&&!f.Call.Returned,"Actual getter can return the borrowed identity before consumer lifetime stop");Eq(9f,result.Read(0),"Earlier register value remains captured");
   f.Entity.MarkReleased();Missing(0x027416,delegate{f.Get(0);},"Owner lifetime checked at initial vptr load");Check(!f.Call.PrimaryCaptured&&!f.Call.MeshEntered&&!f.Call.Returned,"Released owner cannot reach getter");Eq(0u,f.Call.MeshCall.At,"Nested call diagnostics reset");Unknown(f.Call.Result,"No stale successful return");
  });
 }
}
