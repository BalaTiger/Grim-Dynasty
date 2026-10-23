using System;
using GrimDynasty.ReverseClient.Actions;

// Own shared-state tests against observed native contracts. No native calls,
// original files or successful callback injections. Full Created/Update here
// remains conditional on explicit own state, not loaded game or movement proof.
public static class GameMovementCallbackTests {
 static int assertions,scenarios;
 static void Check(bool condition,string message){assertions++;if(!condition)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(Object.Equals(expected,actual),message+": expected="+expected+" actual="+actual);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static void Point(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static void ExpectPoint(NavigationPoint3 p,float x,float y,float z,string label){Equal(Bits(x),Bits(p.X.Read(0)),label+"X");Equal(Bits(y),Bits(p.Y.Read(0)),label+"Y");Equal(Bits(z),Bits(p.Z.Read(0)),label+"Z");}
 static void Missing(Action body,uint at,string label){try{body();throw new Exception(label+" unexpectedly returned");}catch(MissingContract e){Equal(at,e.Address,label+" stop");}}
 sealed class Fixture {
  public readonly EntityMovementState Entity=new EntityMovementState(new object());
  public readonly GameMovementCharacterState Owner;
  public readonly NavigationCrowdAgent Agent=new NavigationCrowdAgent();
  public readonly GameMovementParameters Params;
  public readonly ClassRecord CharacterClass=new ClassRecord(),PlayerClass=new ClassRecord();
  public readonly GameMovementCallbackContext Context;
  public Fixture(){
   Owner=new GameMovementCharacterState(Entity);Params=new GameMovementParameters(Agent);
   Entity.Parent130.Write(null);Entity.CoordsD8.Position00.Q0.Write(0);Point(Entity.CoordsD8.Heading30,1,2,3);
   Entity.Height4A0.Write(3);Entity.Scale3AC.Write(2);Point(Entity.Motion1D0,3,4,0);
   Entity.Virtual00.Write(GameMovementPrimaryTable.Character);Owner.Interface38.Virtual38.Write(GameMovementInterfaceTable.Character);
   Owner.FlagDC8.Write(0);Owner.FlagDC9.Write(1);Owner.FlagEC2.Write(0);Owner.SpeedScaleDCC.Write(0);
   Agent.Callback08.Write(Owner.Interface38);Agent.Radius10.Write(99);Agent.Height14.Write(98);Agent.Parameter18.Write(97);Agent.Flags44.Write(0xdead0000);
   for(uint n=0;n<10;n++)Agent.ParameterBits1C40[n].Write(0xa000+n);
   Point(Agent.Position48,7,8,9);Point(Agent.Heading54,10,11,12);
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(CharacterClass);
   var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(PlayerClass);
   Context=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);Context.Attach(Owner);
  }
  public void Created(){GameMovementCallbackContracts.InvokeCreatedCall25AF13(Owner.Interface38,Agent,Context,new Invocation());}
  public void Updated(){GameMovementCallbackContracts.InvokeUpdateCall25C9AF(Agent,Context,new Invocation());}
  public void KnownRadius(){Entity.RadiusComponent49C.Write(6);Entity.RadiusComponent4A4.Write(2);Entity.RadiusScale3A8.Write(3);}
  public EntityEngineState KnownUpdate(){
   Owner.ActionState24B4.Write(0);Entity.LastFrame178.Write(3);
   var engine=new EntityEngineState(new WorldEngine());engine.FrameCount640.Write(4);Context.Entities.AttachEngine(engine);Context.World.EngineGlobal.Write(engine.Identity);return engine;
  }
 }
 static void BorrowedView(){
  var f=new Fixture();Check(Object.ReferenceEquals(f.Params.Agent,f.Agent),"same agent");Check(Object.ReferenceEquals(f.Params.Radius00,f.Agent.Radius10),"radius identity");
  Check(Object.ReferenceEquals(f.Params.Height04,f.Agent.Height14),"height identity");Check(Object.ReferenceEquals(f.Params.Speed08,f.Agent.Parameter18),"speed identity");
  Check(Object.ReferenceEquals(f.Params.Flags34,f.Agent.Flags44),"flags identity");
  for(uint n=0;n<10;n++)Check(Object.ReferenceEquals(f.Params.Dword(0xc+n*4,0),f.Agent.ParameterBits1C40[n]),"parameter cell"+n);
  f.Params.Dword(0x24,0).Write(0x12345678);Equal(0x12345678u,f.Agent.ParameterBits1C40[6].Read(0),"no snapshot");
  f.Context.Attach(f.Owner);Check(Object.ReferenceEquals(f.Context.Entities.Entity(f.Entity.Identity,0),f.Entity),"same Entity owner");
  bool rejected=false;try{f.Context.Attach(new GameMovementCharacterState(f.Entity));}catch(InvalidOperationException){rejected=true;}Check(rejected,"duplicate Game fields for same owner rejected");scenarios++;
 }
 static void CreatedPrefix(){
  var f=new Fixture();Missing(f.Created,0x02b428,"radius arithmetic consumes unknown49C");
  ExpectPoint(f.Agent.Position48,7,8,9,"null region keeps position");ExpectPoint(f.Agent.Heading54,1,2,3,"heading");
  Equal(6f,f.Agent.Height14.Read(0),"height production");Equal(5f,f.Agent.Parameter18.Read(0),"speed production");Equal(99f,f.Agent.Radius10.Read(0),"radius not fabricated");
  Equal(0x481u,f.Agent.Flags44.Read(0),"flags prefix");Equal(0u,f.Params.Dword(0xc,0).Read(0),"param0c");Equal(4u,f.Params.Dword(0x10,0).Read(0),"param10");
  Equal(6u,f.Params.Dword(0x14,0).Read(0),"param14");Equal(0x3ccccccdu,f.Params.Dword(0x18,0).Read(0),"param18");
  Equal(0u,f.Params.Dword(0x2c,0).Read(0),"QWORD low clear before radius");Equal(0u,f.Params.Dword(0x30,0).Read(0),"QWORD high clear before radius");
  Equal(0xa004u,f.Params.Dword(0x1c,0).Read(0),"postradius1c untouched");Equal(0xa005u,f.Params.Dword(0x20,0).Read(0),"postradius20 untouched");
  Equal(0xa006u,f.Params.Dword(0x24,0).Read(0),"native-unwritten24 untouched");Equal(0xa007u,f.Params.Dword(0x28,0).Read(0),"postradius28 untouched");scenarios++;
 }
 static void WorldPositionProduction(){
  var f=new Fixture();var p=f.Entity.CoordsD8.Position00;p.Q0.Write(77);p.D8.Write(Bits(10));p.DC.Write(Bits(20));p.D10.Write(Bits(30));
  var region=new WorldRegion(new object());region.Offset3C.Write(100);region.Offset40.Write(200);region.Offset44.Write(300);f.Context.World.State.AttachRegion(77,region);
  Missing(f.Created,0x02b428,"world conversion then unknown radius input");ExpectPoint(f.Agent.Position48,110,220,330,"actual world getter");scenarios++;
 }
 static void CreatedUsesOriginalArgument(){
  var f=new Fixture();var otherEntity=new EntityMovementState(new object());var other=new GameMovementCharacterState(otherEntity);
  // Deliberately unrelated and uninitialized. Its identity is a legal pointer
  // argument value, but it must not be consumed by Created's captured RSI.
  f.Agent.Callback08.Write(other.Interface38);Missing(f.Created,0x02b428,"different stored callback ignored");
  ExpectPoint(f.Agent.Heading54,1,2,3,"original owner heading");Equal(6f,f.Agent.Height14.Read(0),"original owner height");
  Check(Object.ReferenceEquals(f.Agent.Callback08.Read(0),other.Interface38),"stored callback remains different");scenarios++;
 }
 static void UpdateReloadsOtherOwner(){
  var f=new Fixture();var entity=new EntityMovementState(new object());var other=new GameMovementCharacterState(entity);f.Context.Attach(other);
  Point(entity.Motion1D0,0,0,12);entity.Virtual00.Write(GameMovementPrimaryTable.Player);other.Interface38.Virtual38.Write(GameMovementInterfaceTable.Player);
  other.SpeedScaleDCC.Write(2);other.FlagDC8.Write(1);other.FlagDC9.Write(1);f.Owner.SpeedScaleDCC.Forget();f.Agent.Callback08.Write(other.Interface38);
  Missing(f.Updated,0x05b730,"actual loaded owner reaches action stop");Equal(24f,f.Agent.Parameter18.Read(0),"other owner motion and factor");
  Equal(0xdead0800u,f.Agent.Flags44.Read(0),"other flagDC8");Equal(8u,f.Params.Dword(0x14,0).Read(0),"other flagDC9");
  ExpectPoint(f.Agent.Position48,7,8,9,"update no position publication");
  f.Agent.Callback08.Forget();Missing(f.Updated,0x25c9a9,"next invocation rereads unknown callback");Equal(24f,f.Agent.Parameter18.Read(0),"no new body writes");Equal(0xdead0800u,f.Agent.Flags44.Read(0),"flags untouched on next failure");scenarios++;
 }
 static void MissingHeightAndMotion(){
  var f=new Fixture();f.Entity.Height4A0.Forget();Missing(f.Created,0x02b468,"height arithmetic consumes unknown");Equal(98f,f.Agent.Height14.Read(0),"height store not reached");Equal(97f,f.Agent.Parameter18.Read(0),"motion not reached");Equal(0x481u,f.Agent.Flags44.Read(0),"earlier flags retained");
  f=new Fixture();f.Entity.Motion1D0.Y.Forget();Missing(f.Created,0x066cd4,"Y load first");Equal(6f,f.Agent.Height14.Read(0),"height retained");Equal(97f,f.Agent.Parameter18.Read(0),"speed unmodified");scenarios++;
 }
 static void SpeedFailureAndUnorderedFactor(){
  var f=new Fixture();f.Owner.SpeedScaleDCC.Forget();Missing(f.Updated,0x066dbd,"missing factor");Equal(5f,f.Agent.Parameter18.Read(0),"speed published before factor read");Equal(0xdead0000u,f.Agent.Flags44.Read(0),"flags later");
  f=new Fixture();f.Entity.Motion1D0.X.Write(Single.NaN);f.Owner.SpeedScaleDCC.Write(Single.NaN);Missing(f.Updated,0x05b730,"unordered own masked FP");
  Equal(0u,Bits(f.Agent.Parameter18.Read(0)),"UCOMISS unordered zero arm; factor skipped");scenarios++;
 }
 static void ClassIdentities(){
  var f=new Fixture();f.Owner.FlagDC9.Write(0);f.CharacterClass.Parent10.Write(null);Missing(f.Updated,0x05b730,"nonplayer null parent");Equal(6u,f.Params.Dword(0x14,0).Read(0),"nonplayer6");
  f=new Fixture();f.Owner.FlagDC9.Write(0);f.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);Missing(f.Updated,0x05b730,"direct Player identity");Equal(2u,f.Params.Dword(0x14,0).Read(0),"player2 without parent read");
  f=new Fixture();f.Owner.FlagDC9.Write(0);Missing(f.Updated,0x066e12,"unknown parent");Equal(0xa002u,f.Params.Dword(0x14,0).Read(0),"classification store not reached");
  f=new Fixture();f.Owner.FlagDC9.Write(0);f.CharacterClass.Parent10.Write(new ClassRecord());Missing(f.Updated,0x591484,"unknown parent in proven helper");Equal(0xa002u,f.Params.Dword(0x14,0).Read(0),"helper stop preserves parameter");scenarios++;
 }
 static void DirectActionState(){
  var f=new Fixture();var i=new Invocation();
  Missing(delegate{GameMovementCallbackContracts.GetActionState05B730(f.Owner,i);},0x05b730,"action field begins unknown");
  f.Owner.ActionState24B4.Write(0xffffffff);Equal(0xffffffffu,GameMovementCallbackContracts.GetActionState05B730(f.Owner,i),"all DWORD bits retained");
  f.Owner.ActionState24B4.Write(0x13);Equal(0x13u,GameMovementCallbackContracts.GetActionState05B730(f.Owner,i),"same owner reread after mutation");
  f.Owner.ActionState24B4.Forget();Missing(delegate{GameMovementCallbackContracts.GetActionState05B730(f.Owner,i);},0x05b730,"no cached action result");
  f.Owner.ActionState24B4.Write(0);f.Entity.MarkReleased();Missing(delegate{GameMovementCallbackContracts.GetActionState05B730(f.Owner,i);},0x05b730,"own released entity is not a valid field read");scenarios++;
 }
 static void DirectParentIdentity(){
  var self=new ClassRecord();var target=new ClassRecord();var middle=new ClassRecord();var i=new Invocation();
  self.Parent10.Write(null);Check(!GameMovementCallbackContracts.IsDerivedFrom591480(self,self,i),"incoming self is not compared");Equal(0x5914acu,i.At,"null first parent only defines AL");
  self.Parent10.Write(target);Check(GameMovementCallbackContracts.IsDerivedFrom591480(self,target,i),"direct parent identity, target field stays unknown");
  self.Parent10.Write(middle);middle.Parent10.Write(target);Check(GameMovementCallbackContracts.IsDerivedFrom591480(self,target,i),"recursive parent identity");
  middle.Parent10.Write(null);Check(!GameMovementCallbackContracts.IsDerivedFrom591480(self,target,i),"current parent graph reread");Equal(0x59149bu,i.At,"false recursion unwinds with EAX zero");
  Check(!GameMovementCallbackContracts.IsDerivedFrom591480(self,null,i),"known null target is not equal to terminating null parent");
  middle.Parent10.Forget();Missing(delegate{GameMovementCallbackContracts.IsDerivedFrom591480(self,target,i);},0x591484,"recursive unknown cannot become false");
  Missing(delegate{GameMovementCallbackContracts.IsDerivedFrom591480(null,target,i);},0x591484,"null current not default empty class");scenarios++;
 }
 static void ParentCycleBoundary(){
  var a=new ClassRecord();var b=new ClassRecord();var target=new ClassRecord();var i=new Invocation();a.Parent10.Write(b);b.Parent10.Write(a);
  Missing(delegate{GameMovementCallbackContracts.IsDerivedFrom591480(a,target,i);},0x591484,"unmatched cycle is own Unsupported, not native false");
  Check(GameMovementCallbackContracts.IsDerivedFrom591480(a,a,i),"native comparison can find target before repetition");
  a.Parent10.Write(a);Check(GameMovementCallbackContracts.IsDerivedFrom591480(a,a,i),"self parent target matches actual loaded parent");scenarios++;
 }
 static void RadiusUsesThreeComponentsAndDistinctScale(){
  var e=new EntityMovementState(new object());var i=new Invocation();e.RadiusComponent49C.Write(3);e.Height4A0.Write(4);e.RadiusComponent4A4.Write(12);e.RadiusScale3A8.Write(2);e.Scale3AC.Write(7);
  Equal(26f,EntityMovementContracts.GetRadius02B410(e,i),"3/4/12 norm times3A8");Equal(28f,EntityMovementContracts.GetHeight02B460(e,i),"height still uses3AC");
  e.RadiusComponent49C.Write(5);e.Height4A0.Write(12);e.RadiusComponent4A4.Write(0);Equal(26f,EntityMovementContracts.GetRadius02B410(e,i),"same mutable4A0 reused in radius");Equal(84f,EntityMovementContracts.GetHeight02B460(e,i),"same mutable4A0 reused in height");
  e.RadiusScale3A8.Write(1);Equal(13f,EntityMovementContracts.GetRadius02B410(e,i),"radius scale reread without height scale mirror");scenarios++;
 }
 static void RadiusFloatingBranches(){
  var e=new EntityMovementState(new object());var i=new Invocation();e.RadiusComponent49C.Write(0);e.Height4A0.Write(0);e.RadiusComponent4A4.Write(0);e.RadiusScale3A8.Write(-2);
  Equal(0x80000000u,Bits(EntityMovementContracts.GetRadius02B410(e,i)),"zero arm still multiplies and preserves negative zero");
  e.RadiusComponent49C.Write(Single.NaN);e.Height4A0.Write(1);e.RadiusComponent4A4.Write(2);e.RadiusScale3A8.Write(3);Equal(0u,Bits(EntityMovementContracts.GetRadius02B410(e,i)),"unordered square sum takes zero arm");
  e.RadiusScale3A8.Write(Single.NaN);Check(Single.IsNaN(EntityMovementContracts.GetRadius02B410(e,i)),"scale read occurs after unordered branch");
  e.RadiusComponent49C.Write(1e20f);e.Height4A0.Write(0);e.RadiusComponent4A4.Write(0);e.RadiusScale3A8.Write(1);Check(Single.IsPositiveInfinity(EntityMovementContracts.GetRadius02B410(e,i)),"binary32 square overflows before sqrt");
  e.RadiusScale3A8.Write(0);Check(Single.IsNaN(EntityMovementContracts.GetRadius02B410(e,i)),"infinite length times zero remains unordered");scenarios++;
 }
 static void RadiusUnknownConsumption(){
  var e=new EntityMovementState(new object());var i=new Invocation();Action call=delegate{EntityMovementContracts.GetRadius02B410(e,i);};
  Missing(call,0x02b428,"MOVSS copies unknown before first arithmetic49C");e.RadiusComponent49C.Write(1);Missing(call,0x02b42c,"shared4A0 consumed second");
  e.Height4A0.Write(1);Missing(call,0x02b430,"4A4 consumed third");e.RadiusComponent4A4.Write(0);Missing(call,0x02b44b,"3A8 required after length");
  e.RadiusScale3A8.Write(1);e.MarkReleased();Missing(call,0x02b410,"released radius source view");scenarios++;
 }
 static void CompleteCreatedContracts(){
  var f=new Fixture();f.KnownRadius();f.Created();Equal(21f,f.Agent.Radius10.Read(0),"actual radius production");Equal(6f,f.Agent.Height14.Read(0),"independent height scale");Equal(5f,f.Agent.Parameter18.Read(0),"motion speed");
  Equal(0u,f.Params.Dword(0x1c,0).Read(0),"tail1c");Equal(1u,f.Params.Dword(0x20,0).Read(0),"tail20");Equal(0xa006u,f.Params.Dword(0x24,0).Read(0),"native-unwritten24 retains caller state");Equal(0x3f8ccccdu,f.Params.Dword(0x28,0).Read(0),"tail28 bits");Equal(400u,f.Params.Dword(0x2c,0).Read(0),"tail2c");Equal(0u,f.Params.Dword(0x30,0).Read(0),"high QWORD remains clear");Equal(0x481u,f.Agent.Flags44.Read(0),"character flags");
  f=new Fixture();f.KnownRadius();f.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);f.Owner.Interface38.Virtual38.Write(GameMovementInterfaceTable.Player);f.Owner.FlagEC2.Write(1);f.Created();Equal(0x800004e5u,f.Agent.Flags44.Read(0),"player flags follow completed base callback");Equal(2u,f.Params.Dword(0x20,0).Read(0),"player increments base count");Equal(1u,f.Params.Dword(0x10,0).Read(0),"player param10");Equal(2u,f.Params.Dword(0x14,0).Read(0),"player param14");scenarios++;
 }
 static void NewCreatedFailurePrefixes(){
  var f=new Fixture();f.KnownRadius();f.Entity.RadiusScale3A8.Forget();Missing(f.Created,0x02b44b,"unknown3A8 stops before radius publication");Equal(99f,f.Agent.Radius10.Read(0),"radius prior value retained");Equal(6f,f.Agent.Height14.Read(0),"earlier height retained");Equal(0xa004u,f.Params.Dword(0x1c,0).Read(0),"tail not reached");
  f=new Fixture();f.KnownRadius();f.Owner.FlagEC2.Forget();Missing(f.Created,0x066d34,"unknown EC2 after newly reached tail");Equal(21f,f.Agent.Radius10.Read(0),"radius already published");Equal(400u,f.Params.Dword(0x2c,0).Read(0),"tail write retained");Equal(0x481u,f.Agent.Flags44.Read(0),"unknown EC2 not invented false");scenarios++;
 }
 static void CompleteUpdateContracts(){
  var f=new Fixture();f.KnownUpdate();f.Owner.ActionState24B4.Write(0x13);f.Updated();Equal(0xdead0020u,f.Agent.Flags44.Read(0),"action13 special flags");Equal(8u,f.Params.Dword(0x14,0).Read(0),"DC9 override");
  f.Owner.ActionState24B4.Write(0x15);f.Updated();Equal(0xdead0020u,f.Agent.Flags44.Read(0),"action15 independently read special flags");
  f.Owner.ActionState24B4.Write(0);f.Owner.FlagDC8.Write(1);f.Owner.FlagEC2.Write(1);f.Entity.LastFrame178.Write(2);f.Updated();Equal(0xdead18a0u,f.Agent.Flags44.Read(0),"normal action EC2 stale frame andDC8 combine");ExpectPoint(f.Agent.Position48,7,8,9,"parameter update does not publish position");
  f.Owner.FlagDC8.Write(0);f.Owner.FlagEC2.Write(0);f.Entity.LastFrame178.Write(3);f.Updated();Equal(0xdead0080u,f.Agent.Flags44.Read(0),"live fields clear prior flags");scenarios++;
 }
 static void UpdateParentClassification(){
  var f=new Fixture();f.KnownUpdate();f.Owner.FlagDC9.Write(0);var middle=new ClassRecord();f.CharacterClass.Parent10.Write(middle);middle.Parent10.Write(f.PlayerClass);f.Updated();Equal(2u,f.Params.Dword(0x14,0).Read(0),"complete update finds Player ancestor");
  middle.Parent10.Write(null);f.Updated();Equal(6u,f.Params.Dword(0x14,0).Read(0),"same mutable graph now nonplayer");
  middle.Parent10.Forget();f.Params.Dword(0x14,0).Write(0x777);Missing(f.Updated,0x591484,"unknown ancestor during update");Equal(0x777u,f.Params.Dword(0x14,0).Read(0),"no classification store on incomplete traversal");scenarios++;
 }
 static void LateUpdateFailurePrefixes(){
  var f=new Fixture();var e=f.KnownUpdate();f.Owner.ActionState24B4.Write(0x13);f.Agent.Flags44.Write(0x1000);e.FrameCount640.Forget();Missing(f.Updated,0x077880,"unknown actual frame");Equal(0x1020u,f.Agent.Flags44.Read(0),"action changes visible before frame lookup");
  e.FrameCount640.Write(4);f.Entity.LastFrame178.Forget();Missing(f.Updated,0x07e7c0,"last frame is separate required state");Equal(0x1020u,f.Agent.Flags44.Read(0),"stale flag untouched before missing last frame");
  f.Context.World.EngineGlobal.Forget();Missing(f.Updated,0x066e97,"unknown engine does not create default");f.Context.World.EngineGlobal.Write(new WorldEngine());Missing(f.Updated,0x066e97,"unbound engine identity rejected");scenarios++;
 }
 static void UnsignedFrameBoundary(){
  uint[] frames={0,0,1,0xffffffff,0xffffffff};uint[] last={0,0xffffffff,0,0xfffffffd,0xfffffffe};bool[] stale={true,false,false,true,false};
  for(int n=0;n<frames.Length;n++){var f=new Fixture();var e=f.KnownUpdate();e.FrameCount640.Write(frames[n]);f.Entity.LastFrame178.Write(last[n]);f.Updated();Equal(stale[n],(f.Agent.Flags44.Read(0)&0x1000)!=0,"unsigned frame-minus-one case"+n);}scenarios++;
 }
 public static int Main(){try{
  BorrowedView();CreatedPrefix();WorldPositionProduction();CreatedUsesOriginalArgument();UpdateReloadsOtherOwner();MissingHeightAndMotion();SpeedFailureAndUnorderedFactor();ClassIdentities();
  int regressionScenarios=scenarios,regressionAssertions=assertions;
  DirectActionState();DirectParentIdentity();ParentCycleBoundary();RadiusUsesThreeComponentsAndDistinctScale();RadiusFloatingBranches();RadiusUnknownConsumption();CompleteCreatedContracts();NewCreatedFailurePrefixes();CompleteUpdateContracts();UpdateParentClassification();LateUpdateFailurePrefixes();UnsignedFrameBoundary();
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"new_scenarios\":"+(scenarios-regressionScenarios)+",\"new_assertions\":"+(assertions-regressionAssertions)+",\"affected_regression_scenarios\":"+regressionScenarios+",\"affected_regression_assertions\":"+regressionAssertions+",\"native_execution\":false,\"conditional_created_update\":true,\"position_movement\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
