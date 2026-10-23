using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New request-production tests only. Geometry is an explicit own two-poly
// fixture built by the actual tile serializer/loader. No projection callback,
// original executable, saved game, or old test entrypoint is invoked.
public static class NavigationMoveObjectTestProgram {
 static int scenarios,assertions;
 static void Check(bool yes,string why){assertions++;if(!yes)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Point(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static void Position(NavigationPoint3 p,float x,float y,float z,string why){Equal(x,p.X.Read(0),why+" X");Equal(y,p.Y.Read(0),why+" Y");Equal(z,p.Z.Read(0),why+" Z");}
 static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
 static ushort[] Six(ushort a,ushort b,ushort c){return new[]{a,b,c,(ushort)65535,(ushort)65535,(ushort)65535};}
 sealed class Fixture {
  public readonly NavigationCrowdManager Manager=new NavigationCrowdManager();
  public readonly NavigationCrowdAgent Agent=new NavigationCrowdAgent();
  public readonly NavigationCrowdGroup Group=new NavigationCrowdGroup();
  public readonly NavigationCrowdAdapterState Query=new NavigationCrowdAdapterState();
  public readonly NavigationMoveManagerView Nav=new NavigationMoveManagerView();
  public readonly EntityMovementState Entity=new EntityMovementState(new object());
  public readonly GameMovementCharacterState Owner;
  public readonly NavigationMoveObjectContext Context;
  public readonly NavigationMoveRegistry Map;
  public readonly NavigationMoveRegistryNode Sentinel=new NavigationMoveRegistryNode(),Node=new NavigationMoveRegistryNode();
  public readonly NavigationMoveRegistryBuckets Buckets=new NavigationMoveRegistryBuckets(1);
  public readonly NavigationPoint3 Target=new NavigationPoint3();
  public readonly NavigationMoveObjectInvocation Call=new NavigationMoveObjectInvocation();
  public readonly NavigationRefBuffer References=new NavigationRefBuffer(4);
  public readonly NavigationPointBuffer Points=new NavigationPointBuffer(3);
  public readonly uint EndReference;
  public Fixture(){
   var input=new NavigationTileDataInput(new[]{new NavigationTileQuantizedVertex(0,0,0),new NavigationTileQuantizedVertex(1,0,0),new NavigationTileQuantizedVertex(1,0,1),new NavigationTileQuantizedVertex(0,0,1)},new[]{new NavigationTilePolygon(Six(0,1,2),Six(65535,65535,1)),new NavigationTilePolygon(Six(0,2,3),Six(0,65535,65535))},new byte[]{1,2},new ushort[]{1,1},new NavigationTileBounds(0,0,0,1,2,1),1,1,0,0,0,2,0.5f,1);
   var output=new NavigationTileDataOutput();var serialized=NavigationTileData.Build(input,output);Check(serialized.Outcome==NavigationTileDataOutcome.Published,"Actual fixture serialization");
   var mesh=NavigationMeshTilesState.Create(0,0,0,1,1,4,8,new NavigationSpatialState());mesh.UseObservedLinkConstants();var reference=new Field<uint>();var added=NavigationMeshTiles.Add(mesh,new NavigationMeshTileBytes(output.Bytes,output.Length),reference);Check(added.Outcome==NavigationMeshTileOutcome.NativeSuccess,"Actual fixture tile load");EndReference=reference.Read(0)|1u;
   var query=new NavigationQuery();query.Mesh0.Write(mesh.Mesh);Query.Query10.Write(query);Query.Filter18.Include100.Write(1);Query.Filter18.Exclude102.Write(0);Point(Query.Extent11C,0.125f,2,0.125f);Query.Critical138.Write(new NavigationCrowdCriticalSection());
   var adapter=new NavigationCrowdAdapterBoundaries(mesh);adapter.UseObservedNearestConstants();
   Owner=new GameMovementCharacterState(Entity);var cs=new AddressSymbol<ClassRecord>(0xa5cba0);cs.Write(new ClassRecord());var ps=new AddressSymbol<ClassRecord>(0xa5cab0);ps.Write(new ClassRecord());var game=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),cs,ps);game.Attach(Owner);
   var addresses=new NavigationMoveAddressSpace(game);addresses.BindOwnEntityAddress(Owner,0x100000000UL);Context=new NavigationMoveObjectContext(addresses,adapter,new NavigationCrowdPathBoundaries(mesh));Context.UseObservedConstants();
   Agent.Callback08.Write(Owner.Interface38);Point(Agent.Position48,0.8f,0,0.2f);Point(Agent.Target78,9,8,7);Agent.State70.Write(7);Agent.NeedReplan140.Write(0);Agent.Flags44.Write(0);Agent.ParameterBits1C40[3].Write(Bits(0.01f));Agent.Radius10.Write(0.1f);
   var members=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2);members.Cells[0].Write(Agent);Group.Members28.InstallOwnStorage(members,1);Group.Owner00.Write(Manager);Group.Query08.Write(Query);var groups=new NavigationCrowdPointerBuffer<NavigationCrowdGroup>(2);groups.Cells[0].Write(Group);Manager.Groups10.InstallOwnStorage(groups,1);Manager.QueryB8.Write(Query);
   Map=NavigationMoveRegistry.ForManager(Manager);Map.Sentinel30.Write(Sentinel);Map.Buckets40.Write(Buckets);Map.Mask58.Write(0);Node.Key10.Write(0x100000038UL);Node.Agent18.Write(Agent);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);
   var outer=NavigationMoveOuterView.ForAdapter(Query);outer.Vtable00.Write(0x33a6c0);outer.Crowd08.Write(Manager);Nav.Outer10.Write(Query);
   Agent.RequestE8.InstallOwnRefStorage(References,2);References.Cells[0].Write(100);References.Cells[1].Write(200);var vector=new NavigationCrowdWaypointVector(Agent.RequestE8.WaypointVector38);vector.InstallOwnStorage(Points,3);for(int n=0;n<9;n++)Points.Cells[n].Write(n+10);Point(Target,0.2f,0,0.8f);
  }
  public byte Run(){return NavigationMoveObjectContracts.Move151130(Nav,Owner,Target,Context,Call);}
  public void NoAgent(){Buckets.Cells[0].First00.Write(Sentinel);Buckets.Cells[0].Last08.Forget();Context.DisableMissingAgent422B8D.Write(1);}
  public NavigationCrowdAgent Blocker(){var other=new NavigationCrowdAgent();Point(other.Position48,0.2f,0,0.8f);other.Radius10.Write(0.2f);other.Height14.Write(2);other.ParameterBits1C40[1].Write(1);Agent.ParameterBits1C40[2].Write(1);var begin=Group.Members28.Begin00.Read(0);begin.Buffer.Cells[1].Write(other);Group.Members28.End08.Write(new NavigationCrowdPointerAddress<NavigationCrowdAgent>(begin.Buffer,2));return other;}
  public void Unchanged(){Position(Agent.Target78,9,8,7,"Target unchanged");Equal(7u,Agent.State70.Read(0),"State unchanged");Equal((byte)0,Agent.NeedReplan140.Read(0),"Replan unchanged");}
  public void Published(){Equal(2u,Agent.State70.Read(0),"Real request state");Equal(0.2f,Agent.Target78.X.Read(0),"Projected target X");Equal(0.8f,Agent.Target78.Z.Read(0),"Projected target Z");Position(Agent.Position48,0.8f,0,0.2f,"Request is not movement");Same(Owner.Interface38,Agent.Callback08.Read(0),"Original callback identity preserved");}
 }
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new ArgumentException("No arguments");
  Test("real projection publishes same Agent and compresses both old paths",delegate{var f=new Fixture();Equal((byte)1,f.Run(),"Accepted");Same(f.Agent,f.Call.Agent.Read(0),"Selected same Agent");Same(f.Map,NavigationMoveRegistry.ForManager(f.Manager),"One map sidecar");f.Published();Equal(f.EndReference,f.Call.ProjectedReference.Read(0),"Actual polygon");Equal(1,f.Agent.RequestE8.RefVector20.End08.Read(0).Index,"Ref count");Equal(200u,f.References.Cells[0].Read(0),"Last ref copied first");Equal(1L,((NavigationCrowdPointAddress)f.Agent.RequestE8.WaypointVector38.End08.Read(0)).PointIndex,"Point count");Equal(16f,f.Points.Cells[0].Read(0),"Last X");Equal(17f,f.Points.Cells[1].Read(0),"Last Y");Equal(18f,f.Points.Cells[2].Read(0),"Last Z");Equal((byte)1,f.Agent.NeedReplan140.Read(0),"Changed token");Check(f.Call.CompressionReturned&&f.Call.Returned,"Completed real prefix");Equal(0,f.Query.Critical138.Read(0).OwnOutstandingEnters,"Projection lock released");});
  Test("full eight-byte key distinguishes equal low32 identities",delegate{var f=new Fixture();var other=new NavigationMoveRegistryNode();other.Key10.Write(0x38UL);other.Agent18.Forget();other.Next00.Write(f.Node);f.Buckets.Cells[0].First00.Write(other);Equal((byte)1,f.Run(),"Find second");Equal(2,f.Call.VisitedNodes,"Two nodes");Equal(8,f.Call.HashedBytes,"Eight FNV bytes");Equal(0x10f713e17a6a2c2cUL,f.Call.Hash.Read(0),"Explicit eight-byte FNV vector");Same(f.Agent,f.Call.Agent.Read(0),"Same Agent");});
  Test("empty registry returns false without target or query reads",delegate{var f=new Fixture();f.NoAgent();f.Target.X.Forget();f.Manager.QueryB8.Forget();Equal((byte)0,f.Run(),"No Agent");Check(!f.Call.ProjectionReturned,"No query");f.Unchanged();});
  Test("unknown missing-agent flag stops before diagnostics",delegate{var f=new Fixture();f.NoAgent();f.Context.DisableMissingAgent422B8D.Forget();Missing(0x25b468,delegate{f.Run();},"Unknown assert flag");f.Unchanged();});
  Test("enabled missing-agent diagnostics retain Unsupported",delegate{var f=new Fixture();f.NoAgent();f.Context.DisableMissingAgent422B8D.Write(0);f.Context.DisableAllAssert42144A.Write(0);Missing(0x25b48c,delegate{f.Run();},"Native diagnostics not fabricated");Check(!f.Call.Returned,"No fabricated return");f.Unchanged();});
  Test("known null Agent follows actual missing branch",delegate{var f=new Fixture();f.Node.Agent18.Write(null);f.Context.DisableMissingAgent422B8D.Write(1);Equal((byte)0,f.Run(),"Null Agent");f.Unchanged();});
  Test("threshold equality returns false before groups",delegate{var f=new Fixture();Point(f.Target,0.8f,0,0.2f);f.Agent.ParameterBits1C40[3].Write(0);f.Manager.Groups10.End08.Forget();Equal((byte)0,f.Run(),"Equal zero threshold");f.Unchanged();});
  Test("self excluded without consuming unknown masks or half",delegate{var f=new Fixture();f.Context.Half34BADC.Forget();f.Agent.ParameterBits1C40[1].Forget();f.Agent.ParameterBits1C40[2].Forget();Equal((byte)1,f.Run(),"Self excluded");f.Published();});
  Test("overlapping blocking Agent prevents query",delegate{var f=new Fixture();var other=f.Blocker();f.Agent.Radius10.Write(1);f.Manager.QueryB8.Forget();Equal((byte)0,f.Run(),"Blocked");Check(!f.Call.ProjectionReturned,"No projection");Same(other,NavigationMoveObjectContracts.Blocking25A1C0(f.Manager,f.Target,f.Agent,f.Context,new Invocation()),"Original blocker identity");f.Unchanged();});
  Test("asymmetric mask rejects otherwise overlapping Agent",delegate{var f=new Fixture();var other=f.Blocker();other.ParameterBits1C40[1].Write(2);other.Height14.Forget();Equal((byte)1,f.Run(),"Mask miss skips geometry");f.Published();});
  Test("actual projection miss does not publish",delegate{var f=new Fixture();Point(f.Target,100,0,100);Equal((byte)0,f.Run(),"Actual nearest miss");Check(f.Call.ProjectionReturned,"Projection executed");Equal(0u,f.Call.ProjectedReference.Read(0),"No polygon");f.Unchanged();});
  Test("unknown query vptr stops before success",delegate{var f=new Fixture();NavigationMoveOuterView.ForAdapter(f.Query).Vtable00.Forget();Missing(0x25b3f1,delegate{f.Run();},"Unknown vptr");f.Unchanged();});
  Test("unsupported query vptr is not a callback injection",delegate{var f=new Fixture();NavigationMoveOuterView.ForAdapter(f.Query).Vtable00.Write(0);Missing(0x25b3f4,delegate{f.Run();},"Unbound slot");f.Unchanged();});
  Test("unknown flags after projection prevents stores",delegate{var f=new Fixture();f.Agent.Flags44.Forget();Missing(0x25b401,delegate{f.Run();},"Flags tested before publication");Check(f.Call.ProjectionReturned,"Real query completed");Check(!f.Call.Published,"No stores");f.Unchanged();});
  Test("unknown waypoint header retains target and ref compression",delegate{var f=new Fixture();f.Agent.RequestE8.WaypointVector38.End08.Forget();Missing(0x25c222,delegate{f.Run();},"Unknown point count");f.Published();Equal(1,f.Agent.RequestE8.RefVector20.End08.Read(0).Index,"Ref prefix retained");Equal(200u,f.References.Cells[0].Read(0),"Copied ref retained");Equal((byte)0,f.Agent.NeedReplan140.Read(0),"Late replan store not reached");});
  Test("flag80 preserves both old paths and bypasses unknown points",delegate{var f=new Fixture();f.Agent.Flags44.Write(0x80);f.Agent.RequestE8.WaypointVector38.End08.Forget();Equal((byte)1,f.Run(),"Skip compression");Equal(2,f.Agent.RequestE8.RefVector20.End08.Read(0).Index,"Ref path retained");Equal(100u,f.References.Cells[0].Read(0),"First ref retained");Check(!f.Call.CompressionReturned,"Compression not called");Unknown(f.Agent.RequestE8.WaypointVector38.End08,"Unknown remains unknown");});
  Test("same last reference does not initialize replan",delegate{var f=new Fixture();f.References.Cells[1].Write(f.EndReference);f.Agent.NeedReplan140.Forget();Equal((byte)1,f.Run(),"Accepted");Unknown(f.Agent.NeedReplan140,"No store when token unchanged");});
  Test("unknown last reference preserves compressed prefix",delegate{var f=new Fixture();f.References.Cells[1].Forget();Missing(0x25b44f,delegate{f.Run();},"Unknown final ref consumed late");f.Published();Equal(1,f.Agent.RequestE8.RefVector20.End08.Read(0).Index,"Compressed even unknown ref");Unknown(f.References.Cells[0],"Opaque unknown copied");Equal(1L,((NavigationCrowdPointAddress)f.Agent.RequestE8.WaypointVector38.End08.Read(0)).PointIndex,"Point prefix retained");});
  Test("null vectors compress without reading capacities",delegate{var f=new Fixture();var r=f.Agent.RequestE8.RefVector20;r.Begin00.Write(null);r.End08.Write(null);r.Capacity10.Forget();var p=f.Agent.RequestE8.WaypointVector38;p.Begin00.Write(null);p.End08.Write(null);p.Capacity10.Forget();Equal((byte)1,f.Run(),"Null old paths");Equal((byte)1,f.Agent.NeedReplan140.Read(0),"Empty ref differs");Unknown(r.Capacity10,"Capacity untouched");Unknown(p.Capacity10,"Point capacity untouched");});
  Test("target aliases actual Agent target cells",delegate{var f=new Fixture();Point(f.Agent.Target78,0.2f,0,0.8f);Equal((byte)1,NavigationMoveObjectContracts.Move151130(f.Nav,f.Owner,f.Agent.Target78,f.Context,f.Call),"Aliased input");Equal(f.EndReference,f.Call.ProjectedReference.Read(0),"Actual projection before stores");f.Published();});
  Test("cyclic map stops without publishing",delegate{var f=new Fixture();var last=new NavigationMoveRegistryNode();last.Next00.Write(f.Sentinel);f.Buckets.Cells[0].Last08.Write(last);f.Node.Key10.Write(99);f.Node.Next00.Write(f.Node);Missing(0x05dbae,delegate{f.Run();},"Cyclic own map");f.Unchanged();});
  Test("released full owner cannot acquire a replacement interface",delegate{var f=new Fixture();f.Entity.MarkReleased();Missing(0x151149,delegate{f.Run();},"Released Entity");f.Unchanged();});
  Test("own address cannot be rebound",delegate{var f=new Fixture();bool rejected=false;try{f.Context.Addresses.BindOwnEntityAddress(f.Owner,0x200000000UL);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Rebinding rejected");Equal(0x100000038UL,f.Context.Addresses.Address(f.Owner.Interface38,0),"Original+38 key");Equal((byte)1,f.Run(),"Original binding remains valid");});
  Test("unobserved callback and request completion are untouched",delegate{var f=new Fixture();f.Agent.Callback08.Forget();f.Agent.RequestE8.Complete50.Forget();Equal((byte)1,f.Run(),"MoveObject has no actor callback");Unknown(f.Agent.Callback08,"No callback resolution");Unknown(f.Agent.RequestE8.Complete50,"No synthetic Submit completion");Position(f.Agent.Position48,0.8f,0,0.2f,"No synthetic position change");});
  Console.WriteLine("{\"status\":\"passed-move-object-request-only\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"agent_motion\":false,\"native_registration\":false,\"physical_mouse\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
