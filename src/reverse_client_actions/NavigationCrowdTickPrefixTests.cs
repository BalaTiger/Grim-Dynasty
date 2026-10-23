using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// New prefix scenarios only. Saved Assets/Scene are fixture constructors in
// the exact previously passed GameMoved executable; no old suite is invoked.
// Component fixtures have no mesh/terrain. The saved scenario retains the
// actual Created flags481/minimum1.1/limit400/speed2 and starts with empty Work.
public static class NavigationCrowdTickPrefixTests {
 const string FixtureAssemblyHash="69c5d29b116453a20b6e340c8e4f3728f76447188afa2ed6a37f8f261be7a290";
 static int scenarios,assertions;static object sample;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field){Missing(0,delegate{field.Read(0);},"Unknown preserved");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static void Point(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
 static float Float(uint x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
 static bool Has(NavigationCrowdTickPrefixInvocation i,uint at){return Array.IndexOf(i.ObservedAddresses,at)>=0;}
 static int Occurrences(NavigationCrowdTickPrefixInvocation i,uint at){int n=0;foreach(uint observed in i.ObservedAddresses)if(observed==at)n++;return n;}
 static int WorkCount(NavigationCrowdGroup g){var begin=g.Work40.Begin00.Read(0) as NavigationCrowdPointerAddress<NavigationCrowdAgent>;var end=g.Work40.End08.Read(0) as NavigationCrowdPointerAddress<NavigationCrowdAgent>;if(begin==null&&end==null)return 0;Check(begin!=null&&end!=null&&Object.ReferenceEquals(begin.Buffer,end.Buffer),"Work shares one actual allocation");return end.Index-begin.Index;}
 static string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
 static void Pin(){Eq(FixtureAssemblyHash,Hash(typeof(GameMovedIntegrationTests).Assembly.Location),"Frozen GameMoved fixture assembly");}
 static NavigationCrowdGroup EmptyGroup(NavigationCrowdAgent a){
  var g=new NavigationCrowdGroup();var members=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1);members.At(0,0).Write(a);g.Members28.InstallOwnStorage(members,1);
  g.Work40.Begin00.Write(null);g.Work40.End08.Write(null);g.Work40.Capacity10.Write(null);
  g.Pointer58.Write(null);g.Pointer60.Write(null);g.Count68.Write(0);
  // Pointer70/Count78 are produced by real Prepare25E900. Owner00 and Vector80
  // are deliberately Unknown: this explicit ready branch never needs them.
  return g;
 }
 static NavigationCrowdTickProductionBoundaries Production(SystemUcrtRotationMath math,NavigationMeshTilesState mesh,NavigationCrowdAdapterBoundaries adapter){
  var state=new NavigationCrowdStateBoundaries(adapter);state.UseObservedGroupConstants();state.DisableAllAssert42144A.Write(0);
  var proximity=new NavigationCrowdProximityBoundaries(state);proximity.DisableNoFreeAssert422BA0.Write(0);
  NavigationCrowdPathBoundaries path=null;
  if(mesh!=null){path=new NavigationCrowdPathBoundaries(mesh);path.UseObservedPathConstants();path.UseObservedStraightPathConstants();path.UseObservedEqualityConstant();path.UseObservedDiskNoOp00CE70OwnAdapter();path.DisableAllAssert42144A.Write(0);path.DisableSecondAssert422B92.Write(0);path.DisableTrimAssert422B93.Write(0);path.DisablePopAssert422B94.Write(0);}
  var constants=new NavigationCrowdAvoidanceConstants();constants.UseObservedDiskConstants();var b=new NavigationCrowdTickProductionBoundaries(path,proximity,adapter,constants,math);b.DisableFirstAssert422B91.Write(0);b.DisableHeadingAssert422B9C.Write(0);
  Point(b.Global422980,0,0,0); // Explicit OWN loaded-global input; no disk default.
  return b;
 }
 static NavigationCrowdTickPrefixBoundaries Prefix(NavigationCrowdTickProductionBoundaries b,NavigationMeshTilesState mesh){
  NavigationCrowdRaycastBoundaries raycast=null;if(mesh!=null){raycast=new NavigationCrowdRaycastBoundaries(mesh);raycast.UseObservedRaycastConstants();}
  var p=new NavigationCrowdTickPrefixBoundaries(b,raycast);p.UseObservedDiskConstants();
  p.Global4228C0.X.Write(0);p.Global4228C0.Y.Write(0);p.Global4228C0.Z.Write(0); // Explicit OWN bounds seed, not observed loaded BSS.
  return p;
 }
 sealed class Tiny {
  public readonly NavigationCrowdAgent A=new NavigationCrowdAgent();public readonly NavigationCrowdGroup G;
  public readonly NavigationCrowdPointerDomain<NavigationCrowdAgent> D=new NavigationCrowdPointerDomain<NavigationCrowdAgent>();
  public readonly NavigationCrowdTickProductionBoundaries B;public readonly NavigationCrowdTickPrefixBoundaries P;
  public readonly NavigationCrowdTickPrefixInvocation I=new NavigationCrowdTickPrefixInvocation();
  public readonly EntityMovementState Entity=new EntityMovementState(new object());public readonly GameMovedContext Game;
  public Tiny(SystemUcrtRotationMath math){
   Point(A.Position48,1,2,3);A.Radius10.Write(.5f);A.Height14.Write(2);A.Parameter18.Write(7);A.Flags44.Write(0x20);A.State70.Write(2);A.NeedReplan140.Write(0);for(int n=0;n<3;n++)A.Words88B0[n].Write(0);
   G=EmptyGroup(A);B=Production(math,null,null);P=Prefix(B,null);
   var entities=new EntityMovementRegistry();var coordinates=new WorldCoordinateState();var world=new WorldCoordinateBoundaries(coordinates);var regions=new WorldPublicationRegistry(coordinates);
   var characterSymbol=new AddressSymbol<ClassRecord>(0xa5cba0);characterSymbol.Write(new ClassRecord());var playerSymbol=new AddressSymbol<ClassRecord>(0xa5cab0);playerSymbol.Write(new ClassRecord());
   var callbacks=new GameMovementCallbackContext(world,entities,characterSymbol,playerSymbol);var owner=new GameMovementCharacterState(Entity);owner.Interface38.Virtual38.Write(GameMovementInterfaceTable.Character);A.Callback08.Write(owner.Interface38);callbacks.Attach(owner);
   var publication=new WorldSameRegionPublicationContext(regions,new ActorBoundingBoxContext(entities),new WorldPublicationGateContext(entities),new EntitySpatialTreeContext());
   var floor=new WorldFloorContext(regions,new WorldFloorOperands(new GroundTriangleOperands()));Game=new GameMovedContext(new GameRotationPublicationContext(callbacks,publication,math),floor);
  }
  public void Run(){NavigationCrowdTickPrefixContracts.RunSingleReady25C440(G,A,D,50,P,Game,I);}
  public void NotReached(){Check(!I.Reached25CB73&&!I.UpdateReturned,"No invented prefix completion");Check(!Has(I,0x25cb73),"No fabricated reached edge");}
 }
 static void Components(SystemUcrtRotationMath math){
  Test("Unknown time scale stops at multiplication before allocation or snapshots",delegate{var f=new Tiny(math);f.B.Constants.SpeedTolerance34B9B0.Forget();Missing(0x25c4d2,f.Run,"Actual MULSS input");Unknown(f.I.Seconds);Same(null,f.G.Work40.Begin00.Read(0),"Work not allocated");Eq(0u,f.G.Count68.Read(0),"Proximity not prepared");Unknown(f.A.PreviousC4.X);f.NotReached();});
  Test("Unknown flags occurs after Previous position and bounds seed writes",delegate{var f=new Tiny(math);f.A.Flags44.Forget();Missing(0x25c595,f.Run,"Actual initial projection flag test");Eq(1f,f.A.PreviousC4.X.Read(0),"Previous X copied");Eq(2f,f.A.PreviousC4.Y.Read(0),"Previous Y copied");Eq(3f,f.A.PreviousC4.Z.Read(0),"Previous Z copied");Eq(Bits(.05f),Bits(f.I.Seconds.Read(0)),"Seconds from integer ticks");Eq(0,f.G.Lower10.Z.Read(0),"Seed copied");Eq(0u,f.G.Count78.Read(0),"Real proximity prepared empty");Eq(0,WorkCount(f.G),"Allocated Work still empty");Unknown(f.G.Query08);f.NotReached();});
  Test("State1 and both replan branches stop before membership publication",delegate{
   var state=new Tiny(math);state.A.State70.Write(1);Missing(0x25c664,state.Run,"State1 lock branch");state.NotReached();
   var sync=new Tiny(math);sync.A.NeedReplan140.Write(1);Missing(0x25c78d,sync.Run,"Real synchronous request replacement edge");Eq((byte)1,sync.A.NeedReplan140.Read(0),"Replan flag not pre-cleared");sync.NotReached();
   var async=new Tiny(math);async.A.NeedReplan140.Write(1);async.A.Flags44.Write(0x120);Missing(0x25c73f,async.Run,"Real asynchronous request edge");Eq(2u,async.A.State70.Read(0),"Unimplemented state1 write not fabricated");async.NotReached();
   Eq(0,WorkCount(state.G),"State1 no Work append");Eq(0,WorkCount(sync.G),"Replan no Work append");
  });
  Test("Unknown bounds seed Z transfers until first lowerZ compare",delegate{var f=new Tiny(math);f.P.Global4228C0.Z.Forget();Missing(0x25c926,f.Run,"Actual bounds Z compare");Unknown(f.G.Lower10.Z);Unknown(f.G.Upper1C.Z);Eq(0,f.G.Lower10.X.Read(0),"No early lowerX store");Eq(1u,f.G.Count78.Read(0),"Actual member proximity insertion retained");Eq(1,WorkCount(f.G),"Actual Work append retained");Eq(0f,f.I.MaximumRadius.Read(0),"Radius MAX not yet reached");f.NotReached();});
  Test("Prefix dispatches actual Update and stops at unknown motionY",delegate{var f=new Tiny(math);Missing(0x066d85,f.Run,"Real CharacterUpdate first motion use");Eq(.5f,f.I.MaximumRadius.Read(0),"Maximum radius captured before Update");Check(Has(f.I,0x25c9af),"Real Update call reached");Eq(7f,f.A.Parameter18.Read(0),"Speed not written before motion succeeds");Eq(1u,f.G.Count78.Read(0),"Proximity member retained");f.NotReached();});
  Test("Update partial speed write survives unknown Character speed scale",delegate{var f=new Tiny(math);Point(f.Entity.Motion1D0,2,0,0);Missing(0x066dbd,f.Run,"Real Update speed factor read");Eq(2f,f.A.Parameter18.Read(0),"Actual speed write retained");Eq(0x20u,f.A.Flags44.Read(0),"Later flags not rewritten");Eq(0x066dbdu,f.I.Native.At,"Nested stop retained");f.NotReached();});
 }
 static T Field<T>(object obj,string name){return NavigationCrowdTickTailTests.Fixtures.Field<T>(obj,name);}
 static void Saved(SystemUcrtRotationMath math){
  Test("Saved ready Character executes two prefix producer callback tail frames",delegate{
   var request=MainmenuCrowdRequestPipeline.BuildExplicitRequestContext();var a=request.Agent;var authoritativePosition=a.Position48;float initialX=a.Position48.X.Read(0),initialY=a.Position48.Y.Read(0),initialZ=a.Position48.Z.Read(0);
   object assets=NavigationCrowdTickTailTests.Fixtures.Assets();object scene=NavigationCrowdTickTailTests.Fixtures.Scene(assets,math,a);
   var entity=Field<EntityMovementState>(scene,"Entity");var character=Field<GameMovementCharacterState>(scene,"Character");var callbacks=Field<GameMovementCallbackContext>(scene,"Callbacks");var moved=Field<GameMovedContext>(scene,"Moved");
   entity.CoordsD8.Position00.D8.Write(Bits(initialX));entity.CoordsD8.Position00.DC.Write(Bits(initialY));entity.CoordsD8.Position00.D10.Write(Bits(initialZ));
   entity.Height4A0.Write(2);entity.Scale3AC.Write(1);entity.RadiusComponent49C.Write(0);entity.RadiusComponent4A4.Write(0);entity.RadiusScale3A8.Write(.25f);Point(entity.Motion1D0,2,0,0);
   character.SpeedScaleDCC.Write(0);character.FlagDC8.Write(0);character.FlagDC9.Write(0);character.FlagEC2.Write(0);character.ActionState24B4.Write(0);callbacks.CharacterClass.Read(0).Parent10.Write(null);
   SkillMovedManagerState.ForOwner(character).UseOwnEmptyCollections();var created=new Invocation();GameMovementCallbackContracts.InvokeCreatedCall25AF13(character.Interface38,a,callbacks,created);
   Eq(0x481u,a.Flags44.Read(0),"Real Created flags");Eq(2f,a.Parameter18.Read(0),"Real Created speed2");Eq(0x3f8ccccdu,a.ParameterBits1C40[7].Read(0),"Real Created minimum1.1");Eq(400u,a.ParameterBits1C40[8].Read(0),"Real Created limit400");Eq(.5f,a.Radius10.Read(0),"Real Created radius");Eq(2f,a.Height14.Read(0),"Real Created height");
   Eq(entity.CoordsD8.Heading30.X.Read(0),a.Heading54.X.Read(0),"Created copies real basis headingX");Eq(entity.CoordsD8.Heading30.Z.Read(0),a.Heading54.Z.Read(0),"Created copies real basis headingZ");
   a.State70.Write(2); // Explicit OWN ready-state precondition; State1 completion is not claimed.
   for(int n=0;n<3;n++)a.Words88B0[n].Write(0);
   var group=EmptyGroup(a);group.Query08.Write(request.Adapter);var domain=new NavigationCrowdPointerDomain<NavigationCrowdAgent>();
   var adapter=new NavigationCrowdAdapterBoundaries(request.Mesh);adapter.UseObservedConstants();var production=Production(math,request.Mesh,adapter);var boundary=Prefix(production,request.Mesh);
   var prefix=new NavigationCrowdTickPrefixInvocation();var continuation=new GameMovedReachedTailInvocation();var frames=new List<object>();long mathBefore=math.NativeMathCalls;object allocation=null;int raycasts=0;
   var engine=Field<EntityEngineState>(scene,"EngineState");
   for(int frame=0;frame<2;frame++){
    // Explicit OWN per-frame gate inputs. The native producer of LastFrame178
    // is not part of this bounded chain and is not attributed to publication.
    engine.FrameCount640.Write((uint)(100+frame));entity.LastFrame178.Write((uint)(99+frame));
    Same(group.Work40.Begin00.Read(0),group.Work40.End08.Read(0),"Frame starts with truly empty Work");float beforeX=a.Position48.X.Read(0),beforeZ=a.Position48.Z.Read(0);
    NavigationCrowdTickPrefixContracts.RunSingleReady25C440(group,a,domain,50,boundary,moved,prefix);
    Check(prefix.Reached25CB73&&prefix.UpdateReturned,"Actual prefix reaches nonempty-waypoint edge");Eq(0x25cb73u,prefix.Native.At,"Prefix final edge");Same(a,prefix.Agent,"Same actual member");Eq(1,Occurrences(prefix,0x25c461),"Reused invocation trace reset");
    Eq(Bits(.05f),Bits(prefix.Seconds.Read(0)),"Seconds produced from ticks");Eq(.5f,prefix.MaximumRadius.Read(0),"Maximum radius produced by member loop");Eq(8f,prefix.NeighborFactor.Read(0),"Observed factor copied at actual load");
    Check(Has(prefix,0x25c5b1)&&Has(prefix,0x25caab),"Actual initial projection and straight-path calls");if(prefix.RaycastCalled){Check(prefix.RaycastReturned,"Actual second-waypoint raycast returns");raycasts++;}
    Eq(1,WorkCount(group),"Prefix actually appends Work");if(frame==0)allocation=group.Work40.Begin00.Read(0);else Same(allocation,group.Work40.Begin00.Read(0),"Second frame reuses retained allocation");
    GameMovedReachedTailContracts.ProduceInvokeAndFinishReachedLastWork(group,a,domain,prefix.MaximumRadius,prefix.NeighborFactor,prefix.Seconds,50,production,moved,continuation);
    Check(continuation.Callback.CallbackReturned&&continuation.Callback.Moved.NotificationReturned&&continuation.Tail.Returned&&continuation.Tail.WorkCleared,"Actual callback and tail return");Check(!continuation.FullTick&&!continuation.Tail.FullTick,"Bounded composition remains explicit");Eq(0x25e2fbu,continuation.Tail.Native.At,"Real tail RET");Same(group.Work40.Begin00.Read(0),group.Work40.End08.Read(0),"Only real tail clears Work");
    Eq(0x481u,a.Flags44.Read(0),"No branch-bit workaround");Eq(2f,a.Parameter18.Read(0),"Update retains speed2");Eq(0x3f8ccccdu,a.ParameterBits1C40[7].Read(0),"Minimum not changed to skip low-speed branch");Eq(400u,a.ParameterBits1C40[8].Read(0),"Elapsed limit retained");Check(continuation.Tail.SpeedWritten&&!continuation.Tail.StuckPrefixWritten,"Actual min1.1 gate measures normal movement");Eq(Bits(continuation.Tail.MeasuredSpeed),new NavigationCrowdTickStateView(a).SpeedAndElapsed.ReadSpeedBitsDC(0),"Measured speed published to original DC lane");Check(continuation.Tail.QueriedReference!=0&&continuation.Tail.ReferenceQueryReturned,"Real suffix reference query");Eq((byte)1,continuation.Tail.TrimResult,"Real suffix retains corridor");Eq((byte)0,a.NeedReplan140.Read(0),"Ready state remains valid for next prefix");
    Check(a.Position48.X.Read(0)!=beforeX||a.Position48.Z.Read(0)!=beforeZ,"Each real producer frame moves XZ");Same(authoritativePosition,a.Position48,"Same authoritative position view");Eq(Bits(a.Position48.X.Read(0)),entity.CoordsD8.Position00.D8.Read(0),"Actual published X");Eq(Bits(a.Position48.Z.Read(0)),entity.CoordsD8.Position00.D10.Read(0),"Actual published Z");
    var floor=Field<WorldFloorContext>(scene,"Floor");Eq(Bits(floor.LastGroundHit.Position04.Y4.Read(0)),entity.CoordsD8.Position00.DC.Read(0),"Actual Ground result published without old floorY oracle");Eq(0,request.Adapter.Critical138.Read(0).OwnOutstandingEnters,"Actual navigation locks released");
    frames.Add(new{frame=frame+1,engine_frame=engine.FrameCount640.Read(0),last_frame=entity.LastFrame178.Read(0),seconds=prefix.Seconds.Read(0),maximum_radius=prefix.MaximumRadius.Read(0),neighbor_factor=prefix.NeighborFactor.Read(0),early_moved=prefix.EarlyMovedReturned,update_returned=prefix.UpdateReturned,raycast_called=prefix.RaycastCalled,raycast_returned=prefix.RaycastReturned,waypoint_popped=prefix.WaypointPopped,projection=production.ProjectionOutcome,projection_reference=production.ProjectionReference,suffix_reference=continuation.Tail.QueriedReference,position=new[]{a.Position48.X.Read(0),a.Position48.Y.Read(0),a.Position48.Z.Read(0)},ground_floor_y=Float(entity.CoordsD8.Position00.DC.Read(0)),measured_speed=continuation.Tail.MeasuredSpeed,elapsed=new NavigationCrowdTickStateView(a).SpeedAndElapsed.ReadElapsedE0(0),work_cleared=continuation.Tail.WorkCleared});
   }
   Unknown(group.Owner00);Unknown(group.Vector80.Begin00);Same(character.Interface38,a.Callback08.Read(0),"Original same Character callback");Same(character,SkillMovedManagerState.ForOwner(character).Owner,"Same SkillManager owner");
   object clearedEnd=group.Work40.End08.Read(0);production.Constants.SpeedTolerance34B9B0.Forget();Missing(0x25c4d2,delegate{NavigationCrowdTickPrefixContracts.RunSingleReady25C440(group,a,domain,50,boundary,moved,prefix);},"Reused successful invocation now stops at unknown scale");Check(!prefix.Reached25CB73&&!prefix.UpdateReturned&&!prefix.RaycastCalled&&!prefix.EarlyMovedReturned,"Previous success diagnostics cleared before early stop");Unknown(prefix.Seconds);Unknown(prefix.MaximumRadius);Same(clearedEnd,group.Work40.End08.Read(0),"Unknown prefix did not fabricate cleanup");
   sample=new{tile_count=request.TileCount,corridor_count=request.CorridorCount,tile_crossings=request.TileCrossings,flags=a.Flags44.Read(0),speed=a.Parameter18.Read(0),minimum_bits=a.ParameterBits1C40[7].Read(0),elapsed_limit=a.ParameterBits1C40[8].Read(0),frames=frames.ToArray(),raycast_calls=raycasts,native_math_calls=math.NativeMathCalls-mathBefore,same_agent=true,real_created=true,work_initially_empty=true,frame_gate_inputs_owned=true,full_tick=false,physical_mouse=false};
  });
 }
 public static int Main(string[] args){try{if(args.Length>1||(args.Length==1&&args[0]!="--saved-resource"))throw new ArgumentException("Only new components or --saved-resource");Pin();long calls;using(var math=SystemUcrtRotationMath.OpenOwnProfile()){if(args.Length==0)Components(math);else Saved(math);calls=math.NativeMathCalls;}Console.WriteLine(new JavaScriptSerializer().Serialize(new{status=args.Length==0?"passed-new-crowd-prefix-components":"passed-new-saved-crowd-prefix-two-frames",scenarios=scenarios,assertions=assertions,sample=sample,native_math_calls=calls,full_tick=false,physical_mouse=false,old_test_entrypoints_called=false,original_read=false,original_executed=false}));return 0;}catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
