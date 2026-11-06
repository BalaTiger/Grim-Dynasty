using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New prefix scenarios only. These fixtures declare small OWN observations on
// the production types; no callback supplies a channel or playback result and
// no old test suite/fixture constructor is invoked. The real channel getter is
// reached; its unobserved Resource10 is consumed by Engine0385B0 at0385E9.
public static class GameAnimationPlayTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}} public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static Field<float> F(float value){return Field<float>.Of(value);}
 static Field<byte> B(byte value){return Field<byte>.Of(value);}
 static Field<uint> U(uint value){return Field<uint>.Of(value);}
 static GameAnimationPlayInvocation Call(){return new GameAnimationPlayInvocation();}
 static int Count(GameAnimationPlayInvocation call,uint address){int result=0;foreach(uint at in call.Stages)if(at==address)result++;return result;}
 static void Before(GameAnimationPlayInvocation call,uint first,uint second,string why){int a=call.Stages.IndexOf(first),b=call.Stages.IndexOf(second);Check(a>=0&&b>a,why);}
 static GameAnimationEntryState Entry(EngineGraphicsResourceState resource){var e=new GameAnimationEntryState();e.Virtual00.Write(0x1806d4d48UL);e.Resource10.Write(resource);return e;}
 internal sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly ActorBoundingBoxContext Bounds;
  public readonly ActorBoundingBoxState Actor;
  public readonly BorrowedMeshBounds Mesh;
  public readonly EngineAnimationSpeedContext Speed;
  public readonly EngineMeshAnimationSpeedFields SpeedFields;
  public readonly GameMoveAnimationProductionContext Context;
  public readonly GameCharacterAnimationFields Sets;
  public readonly AnimationSet Set=new AnimationSet();
  public readonly GameAnimationSetFields Fields;
  public readonly EngineGraphicsResourceState Resource=new EngineGraphicsResourceState();
  public readonly GameAnimationEntryState Animation;
  public Fixture(){
   var world=new WorldCoordinateBoundaries(new WorldCoordinateState(),new Field<WorldEngine>());
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   Callbacks=new GameMovementCallbackContext(world,new EntityMovementRegistry(),character,player);Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);Bridge.Bind(Owner,Move);
   Bounds=new ActorBoundingBoxContext(Callbacks.Entities);Actor=Bounds.Bind(Owner.Entity);Mesh=Bounds.BindMesh(new object());Speed=new EngineAnimationSpeedContext(Bounds,Actor);SpeedFields=Speed.BindExistingMesh(Mesh);Context=new GameMoveAnimationProductionContext(Bridge,Move,Speed);
   Sets=GameCharacterAnimationFields.ForOwner(Owner);Sets.Set2600.Write(null);Sets.Set2608.Write(null);Sets.Set2578.Write(Set);Set.Kind08.Write(0);Fields=GameAnimationSetFields.ForSet(Set);
   Animation=Entry(Resource);Animation.Float18.Write(3.75f);Animation.Float1C.Write(1.5f);Fields.Entry(5).Write(Animation);Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);Actor.Mesh2668.Write(Mesh);
   // Neither resource loading nor a channel is fabricated. Float20, Speed130,
   // the unused second mesh, and all native resource payload fields stay Unknown.
  }
  public byte Play(GameAnimationPlayInvocation call){return Play(5,F(2),B(1),U(0),call);}
  public byte Play(uint id,Field<float> speed,Field<byte> loop,Field<uint> extra,GameAnimationPlayInvocation call){return GameAnimationPlayContracts.Play024DE0(Context,Move,Field<AnimationSet>.Of(Set),id,null,speed,loop,extra,call);}
 }
 static void Incomplete(Fixture f,GameAnimationPlayInvocation call){
  Eq(5u,f.Fields.Pending0C.Read(0),"Requested pending5 survives failed playback");Check(!call.AnimationSubmitted&&!call.PlayerReturned&&!call.EntryReturned&&!call.Returned,"Prefix is not playback or normal return");Same(f.Resource,call.ResourceCapture.Read(0),"Exact resource pointer retained");
 }
 static void ChannelStop(Fixture f,GameAnimationPlayInvocation call){
  Missing(0x0385e9,delegate{f.Play(call);},"Actual channel Resource10 remains unobserved");Incomplete(f,call);Check(call.ChannelAttempted&&call.ChannelEntered&&call.ChannelReturned,"Real channel getter was entered and returned");Check(call.MeshEntered&&call.MeshReturned,"Real mesh getter returned");
 }
 public static void Run(){RunPrefix();RunPublication();}
 public static void RunPublicationRegression(){scenarios=assertions=0;RunPublication();}
 static void RunPublication(){
  Test("Player publication changes the original mesh speed before unresolved hand state",delegate{
   var f=new Fixture();var channel=EngineAnimationChannelState.ForMesh(f.Mesh);channel.Resource10.Write(null);var c=Call();
   Missing(0x284733,delegate{f.Play(c);},"Actual GetHandState enters284720; unknown endpoints stop at pointer subtraction");
   Check(c.AnimationSubmitted&&c.ChannelCall.Returned&&c.ChannelCall.FieldsPublished&&c.HandStateEntered,"Recovered Engine publication completed before real hand query");Check(!c.PlayerReturned&&!c.EntryReturned&&!c.Returned,"No native tail or outer normal return fabricated");Same(f.Resource,channel.Resource10.Read(0),"Entry resource identity reaches actual channel");Same(f.SpeedFields.Speed130,channel.Speed20,"Channel+20 and mesh+130 are one Field");Eq(3f,f.SpeedFields.Speed130.Read(0),"Original speed now observes actual publication");
   var speedCall=new EngineAnimationSpeedInvocation();Eq(3f,EngineAnimationSpeedContracts.GetAnimationSpeed027410(f.Speed,0,speedCall).Read(0),"Existing production getter reads the published speed");Same(f.Mesh,speedCall.ReturnedMesh,"Getter returns same mesh");Eq((byte)1,channel.Byte18.Read(0),"Game normalized bool reaches channel byte");Eq(5u,f.Fields.Pending0C.Read(0),"Pending not cleared on later dependency stop");Eq(0u,f.Set.Kind08.Read(0),"Current Kind not advanced on incomplete Player");Unknown(f.Resource.Virtual00,"Publication does not decode or validate resource payload");
  });
  Test("Move production adapter uses the same Player channel and preserves second-play failure",delegate{
   var f=new Fixture();var channel=EngineAnimationChannelState.ForMesh(f.Mesh);channel.Resource10.Write(null);var calls=new GameMoveAnimationProductionInvocation();
   Missing(0x284733,delegate{GameMoveAnimationProductionContracts.Play(f.Context,f.Move,Field<AnimationSet>.Of(f.Set),5,null,2,1,0,0x05fa70,calls);},"Real Move caller reaches same native prefix");Eq(1,calls.PlayCalls.Count,"One owned playback invocation");Same(f.Owner,calls.PlayCalls[0].Owner,"Original Bridge owner");Same(channel,calls.PlayCalls[0].ChannelCall.Channel,"Shared channel identity");Eq(0x284733u,calls.Native.At,"Nested stop propagates to Move adapter");
   var replacement=new EngineGraphicsResourceState();f.Animation.Resource10.Write(replacement);
   Missing(0x038611,delegate{GameMoveAnimationProductionContracts.Play(f.Context,f.Move,Field<AnimationSet>.Of(f.Set),5,null,4,0,0,0x05fa70,calls);},"Second call observes previous published animation and unknown old queue");Eq(2,calls.PlayCalls.Count,"Second invocation retained");Check(!calls.PlayCalls[1].AnimationSubmitted&&!calls.PlayCalls[1].HandStateEntered,"Failure does not cross unknown insertion or reach hand query");Same(f.Resource,channel.Resource10.Read(0),"Previous channel resource preserved");Same(replacement,calls.PlayCalls[1].ResourceCapture.Read(0),"New request identity separately captured");Eq(3f,channel.Speed20.Read(0),"Previous speed not overwritten by second request");Eq((byte)0,f.Animation.Byte28.Read(0),"Earlier second-call entry flag write remains");Eq(5u,f.Fields.Pending0C.Read(0),"Requested pending prefix remains");Eq(0u,f.Set.Kind08.Read(0),"No successful kind write");
  });
 }
 public static void RunPrefix(){
  scenarios=assertions=0;
  Test("Null resource returns0 after exact byte/DWORD writes and retains Pending",delegate{
   var f=new Fixture();f.Set.Kind08.Write(45);f.Animation.Resource10.Write(null);f.Animation.Float18.Forget();f.Animation.Float1C.Forget();var c=Call();
   Eq((byte)0,GameAnimationPlayContracts.Play024DE0(null,null,Field<AnimationSet>.Of(f.Set),5,null,new Field<float>(),B(0xa5),U(0x89abcdef),c),"No actor or FP input needed for resource-null path");
   Eq((byte)0xa5,f.Animation.Byte28.Read(0),"Original BYTE is not normalized before store");Eq(0x89abcdefu,f.Animation.Dword24.Read(0),"All DWORD bits retained");Eq((byte)1,c.SubmittedBool.Read(0),"Separate flag is normalized");Eq(5u,f.Fields.Pending0C.Read(0),"False does not clear pending");Eq(45u,f.Set.Kind08.Read(0),"False does not publish current kind");Check(c.EntryReturned&&c.Returned&&!c.PlayerEntered&&!c.MeshEntered&&!c.ChannelAttempted,"Normal false is distinct from dependency failure");Eq(0x024e4eu,c.Native.At,"False return address");Unknown(f.Animation.Float20,"Unused float is untouched");
  });
  Test("Unknown loop and extra are copied before their ordered short-circuit tests",delegate{
   var a=new Fixture();a.Animation.Resource10.Write(null);var ca=Call();Missing(0x02085b,delegate{a.Play(5,F(2),new Field<byte>(),U(0x12345678),ca);},"Loop first consumption");Unknown(a.Animation.Byte28,"Unknown byte was stored");Eq(0x12345678u,a.Animation.Dword24.Read(0),"Following DWORD was stored before loop test");Check(ca.LoopWritten&&ca.ExtraWritten&&!ca.ResourceCaptured,"Resource not reached");
   var b=new Fixture();b.Animation.Resource10.Write(null);Missing(0x02085f,delegate{b.Play(5,F(2),B(0),new Field<uint>(),Call());},"Zero loop consumes extra");Unknown(b.Animation.Dword24,"Unknown extra remains stored");
   var d=new Fixture();d.Animation.Resource10.Write(null);var cd=Call();Eq((byte)0,d.Play(5,F(2),B(1),new Field<uint>(),cd),"Nonzero loop skips unknown extra test");Eq((byte)1,cd.SubmittedBool.Read(0),"Derived flag is known");Unknown(d.Animation.Dword24,"Opaque extra not defaulted");Check(!cd.Stages.Contains(0x02085f),"No extra consumption on short-circuit branch");
  });
  Test("Unknown resource is consumed after entry flags and before FP or Actor",delegate{
   var f=new Fixture();f.Animation.Resource10.Forget();f.Animation.Float18.Forget();f.Owner.Entity.Virtual00.Forget();var c=Call();Missing(0x02086b,delegate{f.Play(c);},"Pointer TEST consumes opaque capture");Eq((byte)1,f.Animation.Byte28.Read(0),"Loop prefix retained");Eq(0u,f.Animation.Dword24.Read(0),"Extra prefix retained");Eq(5u,f.Fields.Pending0C.Read(0),"Set prefix retained");Check(c.ResourceCaptured&&!c.PlayerEntered&&!c.MeshEntered,"No later owner or mesh access");Unknown(c.ResourceCapture,"Capture stays unknown");Unknown(c.SubmittedBlend,"No premature conversion");
  });
  Test("All legal slots and unsigned overflow fallback select actual entry identities",delegate{
   var f=new Fixture();var zero=Entry(null);var five=Entry(null);var last=Entry(null);f.Fields.Entry(0).Write(zero);f.Fields.Entry(5).Write(five);f.Fields.Entry(51).Write(last);f.Set.Kind08.Write(9);
   foreach(uint id in new uint[]{0,5,51,52,0x80000000u,uint.MaxValue}){var c=Call();Eq((byte)0,f.Play(id,new Field<float>(),B(1),U(0),c),"Null selected resource returns0");int index=id<=51?(int)id:0;Eq(index,c.SelectedIndex,"Unsigned slot selection");Same(index==5?five:index==51?last:zero,c.Entry,"Selected shared entry");Eq(id,f.Fields.Pending0C.Read(0),"Original request retained, not fallback index");Eq(9u,f.Set.Kind08.Read(0),"No successful kind publication");}
  });
  Test("Set entry and vptr unknowns stop at each true consumption after pending write",delegate{
   var f=new Fixture();var none=Call();Missing(0x024df0,delegate{GameAnimationPlayContracts.Play024DE0(f.Context,f.Move,new Field<AnimationSet>(),5,null,F(2),B(1),U(0),none);},"Unknown set at store");Check(!none.PendingWritten,"No pending storage exists");
   f.Fields.Entry(5).Forget();var missing=Call();Missing(0x024e0d,delegate{f.Play(missing);},"Unknown entry at vptr dereference");Eq(5u,f.Fields.Pending0C.Read(0),"Earlier pending write retained");Check(!missing.EntryEntered,"Entry body not called");
   f.Fields.Entry(5).Write(null);Missing(0x024e0d,delegate{f.Play(Call());},"Known null entry");f.Fields.Entry(5).Write(f.Animation);f.Animation.Virtual00.Forget();var table=Call();Missing(0x024e23,delegate{f.Play(table);},"Unknown vptr MOV reaches indirect call");Check(!table.EntryEntered&&!table.LoopWritten,"No entry stores before dispatch");f.Animation.Virtual00.Write(0);Missing(0x024e23,delegate{f.Play(Call());},"Different entry type not substituted");
  });
  Test("Float18 conversion precedes Actor primary and multiplier consumption",delegate{
   var f=new Fixture();f.Animation.Float18.Forget();f.Animation.Float1C.Forget();f.Owner.Entity.Virtual00.Forget();var c=Call();Missing(0x020870,delegate{f.Play(c);},"Conversion input first");Incomplete(f,c);Unknown(c.ActorPrimary,"Actor table not captured before conversion");Unknown(c.SubmittedSpeed,"Multiply not reached");Check(c.LoopWritten&&c.ExtraWritten&&!c.PlayerEntered,"Earlier writes only");
  });
  Test("Real channel stop preserves truncation multiplication and all original identities",delegate{
   float[] inputs={3.75f,-3.75f,-2147483648f,2147483520f};int[] expected={3,-3,int.MinValue,2147483520};
   for(int n=0;n<inputs.Length;n++){var f=new Fixture();f.Animation.Float18.Write(inputs[n]);var c=Call();ChannelStop(f,c);Eq(expected[n],c.SubmittedBlend.Read(0),"CVTTSS2SI truncates toward zero within declared range");Eq(3f,c.SubmittedSpeed.Read(0),"FLOAT32 speed multiplied by entry scale");Same(f.Owner,c.Owner,"Original full owner");Same(f.Actor,f.Context.Speed.Actor,"Original Actor sidecar");Same(f.Mesh,c.Mesh,"Original borrowed mesh");Same(f.Set,c.FirstSet.Read(0),"First selected set");Same(f.Set,c.SecondSet.Read(0),"Second selected set");Same(f.Animation,c.Entry,"Original entry");Eq(0u,f.Set.Kind08.Read(0),"No current-kind success write");Unknown(f.Animation.Float20,"Unused field stays unknown");Unknown(f.SpeedFields.Speed130,"Prefix must not invoke speed getter or default speed");}
  });
  Test("Invalid or unobserved FP conversion does not invent the SSE indefinite result",delegate{
   foreach(float value in new[]{Single.NaN,Single.PositiveInfinity,Single.NegativeInfinity,Single.Epsilon,2147483648f,-2147483904f}){var f=new Fixture();f.Animation.Float18.Write(value);var c=Call();Missing(0x020870,delegate{f.Play(c);},"Out-of-domain conversion");Incomplete(f,c);Unknown(c.SubmittedBlend,"No integer default");Unknown(c.ActorPrimary,"Actor not consumed");Eq((byte)1,f.Animation.Byte28.Read(0),"Entry prefix retained");}
  });
  Test("Unknown Actor vptr stays opaque through multiplication until virtual call",delegate{
   var f=new Fixture();f.Owner.Entity.Virtual00.Forget();var early=Call();Missing(0x02087a,delegate{f.Play(5,new Field<float>(),B(1),U(0),early);},"Unknown speed wins over unknown vptr");Eq(3,early.SubmittedBlend.Read(0),"Conversion already completed");Unknown(early.ActorPrimary,"Unknown primary captured without use");
   var late=Call();Missing(0x02088c,delegate{f.Play(late);},"Unknown table consumed by call");Eq(3f,late.SubmittedSpeed.Read(0),"Multiply completed before dispatch");Check(!late.PlayerEntered,"Unknown implementation not entered");f.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Character);Missing(0x02088c,delegate{f.Play(Call());},"Character+2F8 is not Player3E2900");
  });
  Test("Nonfinite subnormal and overflowing multiplication keep prior conversion and table",delegate{
   foreach(float value in new[]{Single.NaN,Single.PositiveInfinity,Single.Epsilon,Single.MaxValue}){var f=new Fixture();f.Animation.Float1C.Write(value);var c=Call();Missing(0x02087a,delegate{f.Play(c);},"Unsupported MULSS domain");Eq(3,c.SubmittedBlend.Read(0),"Earlier CVTTSS2SI result retained");Same(GameMovementPrimaryTable.Player,c.ActorPrimary.Read(0),"Table MOV already happened");Unknown(c.SubmittedSpeed,"No fabricated product");Check(!c.PlayerEntered,"No dispatch after multiply stop");}
  });
  Test("Two independent current-set selections and Kind45 override preserve shared ownership",delegate{
   var f=new Fixture();f.Set.Kind08.Write(45);var c=Call();ChannelStop(f,c);Eq(75,c.SubmittedBlend.Read(0),"Old Kind45 replaces original blend");Eq(2,c.FirstOverrides.Count,"First scan tested both null overrides");Eq(2,c.SecondOverrides.Count,"Second scan restarted at2600");Check(!Object.ReferenceEquals(c.FirstOverrides[0],c.SecondOverrides[0]),"Independent pointer snapshots");Eq(2,Count(c,0x3e2935),"First scan loads");Eq(2,Count(c,0x3e2958),"Second scan loads");Before(c,0x3e2951,0x3e2972,"Kind capture precedes independent pending capture");Before(c,0x3e2972,0x3e2979,"Kind consumed only after second selection");
   var current=new AnimationSet();current.Kind08.Write(0);GameAnimationSetFields.ForSet(current).Pending0C.Write(5);f.Sets.Set2600.Write(current);var changed=Call();ChannelStop(f,changed);Same(f.Set,changed.RequestedSet,"Outer requested set not replaced");Same(current,changed.FirstSet.Read(0),"Fresh first override");Same(current,changed.SecondSet.Read(0),"Fresh second override");Eq(3,changed.SubmittedBlend.Read(0),"No stale Kind45 decision");Eq(1,changed.FirstOverrides.Count,"Override stops first scan");Eq(1,changed.SecondOverrides.Count,"Override stops second scan");Same(f.Owner,changed.Owner,"No copied owner");
  });
  Test("Unknown Kind is consumed only after both valid set selections",delegate{
   var f=new Fixture();f.Set.Kind08.Forget();var c=Call();Missing(0x3e2979,delegate{f.Play(c);},"Kind comparison consumes capture");Check(c.KindCaptured&&c.PendingCaptured&&c.PlayerEntered,"Both fields captured before failure");Eq(2,c.FirstOverrides.Count,"First scan completed");Eq(2,c.SecondOverrides.Count,"Second scan completed");Same(f.Set,c.SecondSet.Read(0),"Second identity retained");Eq(5u,c.PendingCapture.Read(0),"Real pending write was observed");Check(!c.MeshEntered,"No mesh before decision");Incomplete(f,c);
  });
  Test("Current set pending is not replaced by requested5 and stops at conditional use",delegate{
   foreach(uint kind in new uint[]{17,0,45}){var f=new Fixture();var current=new AnimationSet();current.Kind08.Write(kind);f.Sets.Set2600.Write(current);var c=Call();Missing(kind==17?0x3e2997u:0x3e299eu,delegate{f.Play(c);},"Actual second set Pending is Unknown");Same(current,c.SecondSet.Read(0),"Current owner set, not request argument");Unknown(c.PendingCapture,"No pending5 substitution");Eq(kind==45?75:3,c.SubmittedBlend.Read(0),"Only preceding blend instructions occurred");Eq(5u,f.Fields.Pending0C.Read(0),"Outer pending publication survives");Check(!c.MeshEntered&&!c.Returned,"No later call or success");}
  });
  Test("Jump-table pending bounds stop before mesh without guessing cases",delegate{
   foreach(uint pending in new uint[]{17,49}){var f=new Fixture();var current=new AnimationSet();current.Kind08.Write(0);GameAnimationSetFields.ForSet(current).Pending0C.Write(pending);f.Sets.Set2600.Write(current);var c=Call();Missing(0x3e29af,delegate{f.Play(c);},"Unverified table byte load");Eq(pending,c.PendingCapture.Read(0),"Real current-set pending");Eq(3,c.SubmittedBlend.Read(0),"No invented switch overwrite");Check(!c.MeshEntered&&!c.ChannelAttempted,"No downstream calls");Incomplete(f,c);}
  });
  Test("Mesh getter preserves first second and unresolved fallback paths without defaults",delegate{
   var f=new Fixture();f.Actor.Mesh2668.Forget();var first=Call();Missing(0x06da43,delegate{f.Play(first);},"Unknown first mesh test");Check(first.MeshEntered&&!first.MeshReturned&&!first.ChannelAttempted,"Getter stopped without a channel");
   f.Actor.Mesh2668.Write(null);var second=Call();Missing(0x06da43,delegate{f.Play(second);},"Unknown second mesh test");Eq(0x06da43u,second.MeshCall.At,"Nested failure retained");f.Actor.Mesh2670.Write(null);Missing(0x06da53,delegate{f.Play(Call());},"Both-null native fallback remains unresolved");
   var mesh2=f.Bounds.BindMesh(new object());f.Speed.BindExistingMesh(mesh2);f.Actor.Mesh2670.Write(mesh2);var returned=Call();ChannelStop(f,returned);Same(mesh2,returned.Mesh,"Same second borrowed mesh returned");Same(f.Actor,f.Context.Speed.Actor,"Actor context not replaced");Same(f.Resource,returned.ResourceCapture.Read(0),"Resource independent of mesh selection");
  });
 }
}
