using System;
using GrimDynasty.ReverseClient.Actions;

// New composition cases only. Explicit own fields share one Entity and one
// canonical primary-table identity across callback and bounding-box contracts.
// No native execution, old-suite invocation or per-call vptr replacement.
public static class ActorBoundingBoxDispatchIntegrationTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(Object.Equals(expected,actual),why+": expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static void Point(NavigationPoint3 point,float x,float y,float z){point.X.Write(x);point.Y.Write(y);point.Z.Write(z);}
 static void PointIs(NavigationPoint3 point,float x,float y,float z,string why){
  Equal(Bits(x),Bits(point.X.Read(0)),why+" X");Equal(Bits(y),Bits(point.Y.Read(0)),why+" Y");Equal(Bits(z),Bits(point.Z.Read(0)),why+" Z");
 }
 static void BoundsIs(WorldBounds24 box,string why){
  Equal(Bits(12),Bits(box.Center00.Read(0)),why+" center X");Equal(Bits(23),Bits(box.Center04.Read(0)),why+" center Y");Equal(Bits(34),Bits(box.Center08.Read(0)),why+" center Z");
  Equal(Bits(5),Bits(box.Extent0C.Read(0)),why+" extent X");Equal(Bits(6),Bits(box.Extent10.Read(0)),why+" extent Y");Equal(Bits(7),Bits(box.Extent14.Read(0)),why+" extent Z");
 }
 sealed class Fixture {
  public readonly EntityMovementState Entity=new EntityMovementState(new object());
  public readonly NavigationCrowdAgent Agent=new NavigationCrowdAgent();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementParameters Parameters;
  public readonly GameMovementCallbackContext Callbacks;
  public readonly ActorBoundingBoxContext Bounds;
  public readonly ActorBoundingBoxState Actor;
  public readonly GameMovementPrimaryTable Primary;
  public Fixture(bool player,bool selectThroughActor){
   Primary=player?GameMovementPrimaryTable.Player:GameMovementPrimaryTable.Character;
   Owner=new GameMovementCharacterState(Entity);Parameters=new GameMovementParameters(Agent);
   var characterClass=new ClassRecord();characterClass.Parent10.Write(null);
   var playerClass=new ClassRecord();
   var characterSymbol=new AddressSymbol<ClassRecord>(0xa5cba0);characterSymbol.Write(characterClass);
   var playerSymbol=new AddressSymbol<ClassRecord>(0xa5cab0);playerSymbol.Write(playerClass);
   var world=new WorldCoordinateBoundaries(new WorldCoordinateState());
   Callbacks=new GameMovementCallbackContext(world,new EntityMovementRegistry(),characterSymbol,playerSymbol);Callbacks.Attach(Owner);
   Bounds=new ActorBoundingBoxContext(Callbacks.Entities);Actor=Bounds.Bind(Entity);
   // Exactly one setup write to Virtual00; every later operation consumes it.
   if(selectThroughActor)ActorBoundingBoxContracts.SelectObservedCharacterDispatch(Actor,player);
   else Entity.Virtual00.Write(Primary);
   Owner.Interface38.Virtual38.Write(player?GameMovementInterfaceTable.Player:GameMovementInterfaceTable.Character);
   Agent.Callback08.Write(Owner.Interface38);
   Entity.Parent130.Write(null);Entity.CoordsD8.Position00.Q0.Write(7);
   Entity.CoordsD8.Position00.D8.Write(Bits(10));Entity.CoordsD8.Position00.DC.Write(Bits(20));Entity.CoordsD8.Position00.D10.Write(Bits(30));
   for(int n=0;n<9;n++)Entity.CoordsD8.Basis18[n].Write(n==0||n==4||n==8?1f:0f);
   var region=new WorldRegion(new object());region.Offset3C.Write(0);region.Offset40.Write(0);region.Offset44.Write(0);world.State.AttachRegion(7,region);
   Entity.Height4A0.Write(3);Entity.Scale3AC.Write(2);Entity.RadiusComponent49C.Write(6);Entity.RadiusComponent4A4.Write(2);Entity.RadiusScale3A8.Write(3);
   Point(Entity.Motion1D0,3,4,0);Entity.LastFrame178.Write(3);
   Owner.FlagDC8.Write(0);Owner.FlagDC9.Write(0);Owner.FlagEC2.Write(0);Owner.SpeedScaleDCC.Write(2);Owner.ActionState24B4.Write(0);
   var engine=new EntityEngineState(new WorldEngine());engine.FrameCount640.Write(4);Callbacks.Entities.AttachEngine(engine);world.EngineGlobal.Write(engine.Identity);
   Agent.Radius10.Write(99);Agent.Height14.Write(98);Agent.Parameter18.Write(97);Agent.Flags44.Write(0xdead0000);
   for(uint n=0;n<10;n++)Agent.ParameterBits1C40[n].Write(0xa000+n);
   Point(Agent.Position48,7,8,9);Point(Agent.Heading54,10,11,12);
   var mesh=Bounds.BindMesh(new object());Actor.Mesh2668.Write(mesh);
   mesh.Bounds1A8.Center00.Write(2);mesh.Bounds1A8.Center04.Write(3);mesh.Bounds1A8.Center08.Write(4);
   mesh.Bounds1A8.Extent0C.Write(5);mesh.Bounds1A8.Extent10.Write(6);mesh.Bounds1A8.Extent14.Write(7);
   Bounds.UseObservedMask();ActorBoundingBoxContracts.InitializeAttachedIdFields07D020(Actor,new Invocation());
  }
  public void Created(){GameMovementCallbackContracts.InvokeCreatedCall25AF13(Owner.Interface38,Agent,Callbacks,new Invocation());}
  public void Updated(){GameMovementCallbackContracts.InvokeUpdateCall25C9AF(Agent,Callbacks,new Invocation());}
 }
 static void RoundTrip(bool player,bool selectThroughActor){
  var f=new Fixture(player,selectThroughActor);
  Same(f.Primary,f.Entity.Virtual00.Read(0),"Initial canonical primary identity");
  Same(f.Entity,f.Actor.Entity,"Actor keeps callback Entity");Same(f.Callbacks.Entities,f.Bounds.Entities,"One registry serves both contracts");
  Same(f.Owner,f.Callbacks.Owner(f.Owner.Interface38,0),"Original callback owner");
  Equal(player?ObservedEntityDispatchKind.Player:ObservedEntityDispatchKind.Character,ActorBoundingBoxContracts.RequireObservedDispatch(f.Entity.Virtual00.Snapshot(),0),"Canonical read-only classification");
  f.Created();
  Same(f.Primary,f.Entity.Virtual00.Read(0),"Created leaves primary identity unchanged");
  Equal(21f,f.Agent.Radius10.Read(0),"Created computes radius through canonical primary dispatch");Equal(6f,f.Agent.Height14.Read(0),"Created computes height");Equal(5f,f.Agent.Parameter18.Read(0),"Created computes speed");
  PointIs(f.Agent.Position48,10,20,30,"Created world position");PointIs(f.Agent.Heading54,0,0,1,"Created shared heading");
  Equal(player?2u:1u,f.Parameters.Dword(0x20,0).Read(0),"Selected interface Created implementation");
  f.Updated();
  Same(f.Primary,f.Entity.Virtual00.Read(0),"First Update leaves primary identity unchanged");
  Equal(10f,f.Agent.Parameter18.Read(0),"First Update uses current speed factor");Equal(player?2u:6u,f.Parameters.Dword(0x14,0).Read(0),"First Update reads canonical class identity");
  ActorBoundingBoxContracts.UpdateBoundingBox02A990(f.Actor,f.Bounds,new Invocation());
  Same(f.Primary,f.Entity.Virtual00.Read(0),"Bounding-box operation needs no replacement primary table");
  BoundsIs(f.Actor.Box5C,"Published local box");BoundsIs(f.Actor.Box44,"Published final box");Equal((byte)1,f.Actor.Flag74.Read(0),"Bounding flag published");
  Point(f.Entity.Motion1D0,0,0,12);f.Owner.SpeedScaleDCC.Write(0.5f);f.Owner.ActionState24B4.Write(0x13);
  f.Updated();
  Same(f.Primary,f.Entity.Virtual00.Read(0),"Second Update still consumes original primary table");
  Same(f.Owner.Interface38,f.Agent.Callback08.Read(0),"Callback pointer never replaced");
  Equal(6f,f.Agent.Parameter18.Read(0),"Second Update rereads same Entity motion after bounds");Equal(player?2u:6u,f.Parameters.Dword(0x14,0).Read(0),"Second Update retains canonical class dispatch");
  Equal(0x20u,f.Agent.Flags44.Read(0)&0xa0u,"Second Update reads changed action through canonical primary dispatch");
  Equal(21f,f.Agent.Radius10.Read(0),"Bounds and Updates retain Created radius");PointIs(f.Agent.Position48,10,20,30,"Parameter Update retains Created position");BoundsIs(f.Actor.Box44,"Update retains published final box");
  scenarios++;
 }
 public static int Main(){
  try{
   RoundTrip(false,false);RoundTrip(true,false);RoundTrip(false,true);RoundTrip(true,true);
   Console.WriteLine("{\"status\":\"passed\",\"new_scenarios\":"+scenarios+",\"new_assertions\":"+assertions+",\"canonical_dispatch\":true}");return 0;
  }catch(Exception error){
   Console.WriteLine("{\"status\":\"failed\",\"new_scenarios\":"+scenarios+",\"new_assertions\":"+assertions+",\"canonical_dispatch\":true}");Console.Error.WriteLine(error.ToString());return 1;
  }
 }
}
