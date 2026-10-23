using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Only the new shared Agent/Group/request chain is tested here. Geometric
// fixtures are explicit own input, serialized/registered/searched by recovered
// production code. No callback supplies a position, path or completion value.
// Own preallocation and Monitor setup are distinct from native construction.
public static class NavigationCrowdStateTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object a,object b,string why){Check(Object.ReferenceEquals(a,b),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected explicit unsupported: "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 static void XYZ(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static void Position(NavigationPoint3 p,float x,float y,float z,string why){Equal(x,p.X.Read(0),why+" X");Equal(y,p.Y.Read(0),why+" Y");Equal(z,p.Z.Read(0),why+" Z");}
 static NavigationCrowdInt3 I3(int x,int y,int z){var p=new NavigationCrowdInt3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static ushort[] Six(ushort a,ushort b,ushort c){return new ushort[]{a,b,c,65535,65535,65535};}
 static NavigationTileQuantizedVertex V(ushort x,ushort z){return new NavigationTileQuantizedVertex(x,0,z);}
 sealed class Fixture {
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationCrowdAdapterState Adapter=new NavigationCrowdAdapterState();
  public readonly NavigationCrowdManager Manager=new NavigationCrowdManager();
  public readonly NavigationCrowdAgent Agent=new NavigationCrowdAgent();
  public readonly NavigationCorridorState Search;
  public readonly NavigationCorridorFilterCosts Costs;
  public readonly NavigationCrowdStateBoundaries B;
  public readonly NavigationRefBuffer Storage=new NavigationRefBuffer(40);
  public readonly NavigationCrowdPointerBuffer<NavigationCrowdGroup> Groups=new NavigationCrowdPointerBuffer<NavigationCrowdGroup>(2);
  public readonly uint Start,End;
  public NavigationCrowdRequest Request {get{return Agent.RequestE8;}}
  public Fixture(){
   var input=new NavigationTileDataInput(new[]{V(0,0),V(1,0),V(1,1),V(0,1)},new[]{new NavigationTilePolygon(Six(0,1,2),Six(65535,65535,1)),new NavigationTilePolygon(Six(0,2,3),Six(0,65535,65535))},new byte[]{1,2},new ushort[]{1,1},new NavigationTileBounds(0,0,0,1,2,1),1,1,0,0,0,2,0.5f,1);
   var output=new NavigationTileDataOutput();var serialized=NavigationTileData.Build(input,output);Check(serialized.Outcome==NavigationTileDataOutcome.Published,"Own fixture serialization: "+serialized.Detail);
   Mesh=NavigationMeshTilesState.Create(0,0,0,1,1,4,8,new NavigationSpatialState());Mesh.UseObservedLinkConstants();var reference=new Field<uint>();var added=NavigationMeshTiles.Add(Mesh,new NavigationMeshTileBytes(output.Bytes,output.Length),reference);Check(added.Outcome==NavigationMeshTileOutcome.NativeSuccess,"Fixture mesh insertion: "+added.Detail);Start=reference.Read(0);End=Start|1;Query.Mesh0.Write(Mesh.Mesh);
   Adapter.Query10.Write(Query);Adapter.Filter18.Include100.Write(1);Adapter.Filter18.Exclude102.Write(0);XYZ(Adapter.Extent11C,0.125f,2,0.125f);Adapter.Critical138.Write(new NavigationCrowdCriticalSection());
   Search=new NavigationCorridorState(Query);Search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(16,8));Search.Queue60.Write(NavigationSearchStorage.CreateQueue(32));Costs=new NavigationCorridorFilterCosts(Adapter.Filter18);for(int n=0;n<64;n++)Costs.AreaCosts00[n].Write(1);Search.Attach(Costs);
   var queryBoundary=new NavigationCrowdAdapterBoundaries(Mesh,Search);queryBoundary.UseObservedConstants();B=new NavigationCrowdStateBoundaries(queryBoundary);B.DisableAgentAssert422B9F.Write(0);B.DisableGroupAssert422B90.Write(0);B.UseObservedGroupConstants();
   NavigationCrowdStateContracts.InitializeAgent25E6C0(Agent,Field<object>.Of(new object()),B,new Invocation());
   Request.InstallOwnRefStorage(Storage,3);for(int n=0;n<3;n++)Storage.Cells[n].Write((uint)(700+n));XYZ(Request.From04,0.8f,0,0.2f);XYZ(Request.To10,0.2f,0,0.8f);XYZ(Agent.Position48,0.8f,0,0.2f);XYZ(Agent.Target78,0.2f,0,0.8f);
   Manager.CriticalA0.Write(new NavigationCrowdCriticalSection());Manager.QueryB8.Write(Adapter);Manager.Groups10.InstallOwnStorage(Groups,0);Manager.MaxGroups0C.Write(2);Manager.Margin00.X.Write(0);Manager.Margin00.Y.Write(0);Manager.Margin00.Z.Write(0);
  }
  public void Submit(uint generation,byte mode){NavigationCrowdStateContracts.Submit25A4F0(Manager,Request,generation,mode,B,new Invocation());}
  public NavigationCrowdGroup Group(){return NavigationCrowdStateContracts.GetGroup25A250(Manager,I3(0,0,0),I3(1,1,1),B,new Invocation());}
  public void Unlocked(){Equal(0,Manager.OwnOutstandingEnters,"Own manager lock released");Equal(0,Adapter.Critical138.Read(0).OwnOutstandingEnters,"Own adapter lock released");}
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Constructor preserves unobserved payload and mismatched generation never queries",delegate{
   var agent=new NavigationCrowdAgent();var request=agent.RequestE8;var b=new NavigationCrowdStateBoundaries(null);var callback=Field<object>.Of(new object());
   Missing(0x25e80a,delegate{NavigationCrowdStateContracts.InitializeAgent25E6C0(agent,callback,b,new Invocation());},"Late diagnostic flag is not silently disabled");Equal(0xffffffffu,agent.Index00.Read(0),"Earlier index initialization retained");Equal(0u,request.Generation00.Read(0),"Generation initialized");Equal((byte)0,request.Complete50.Read(0),"Completion initialized before diagnostic read");Same(null,request.RefVector20.Begin00.Read(0),"Native null begin is not an allocation");Same(null,request.RefVector20.End08.Read(0),"Native null end");Same(null,request.RefVector20.Capacity10.Read(0),"Native null capacity");
   b.DisableAgentAssert422B9F.Write(0);Same(agent,NavigationCrowdStateContracts.InitializeAgent25E6C0(agent,callback,b,new Invocation()),"Constructor returns original agent");Same(request,agent.RequestE8,"Fixed inline request identity");Same(callback.Read(0),agent.Callback08.Read(0),"Opaque callback pointer preserved without invocation");Unknown(agent.Position48.X,"Unobserved loaded vector is copied unknown");Unknown(request.From04.X,"Constructor never initializes request source");Unknown(request.To10.Z,"Constructor never initializes request destination");Unknown(request.Gap1C,"Unwritten request padding");Unknown(agent.Gap04,"Unwritten agent padding");
   var manager=new NavigationCrowdManager();manager.CriticalA0.Write(new NavigationCrowdCriticalSection());request.Generation00.Write(7);request.Complete50.Write(99);NavigationCrowdStateContracts.Submit25A4F0(manager,request,8,0,b,new Invocation());Equal((byte)99,request.Complete50.Read(0),"Mismatch does not reset completion");Same(null,request.RefVector20.Begin00.Read(0),"Mismatch leaves null vector unchanged");Unknown(manager.QueryB8,"Mismatch does not consume query");Equal(0,manager.OwnOutstandingEnters,"Own lock released after mismatch");
  });
  Test("Matching generation writes the same agent request vector through real corridor search",delegate{
   var f=new Fixture();var request=f.Request;var vector=request.RefVector20;var callback=f.Agent.Callback08.Read(0);f.Submit(0,0);Equal((byte)1,request.Complete50.Read(0),"Completion set only after real slot0 returns");Equal(2,vector.End08.Read(0).Index,"Actual corridor count");Equal(f.Start,f.Storage.Cells[0].Read(0),"Actual starting polygon");Equal(f.End,f.Storage.Cells[1].Read(0),"Actual destination polygon");Same(request,f.Agent.RequestE8,"Agent retains inline request");Same(vector,f.Request.RefVector20,"No mirrored request vector");Same(f.Storage,vector.Begin00.Read(0).Buffer,"Caller-installed backing shared");Same(f.Adapter,f.Manager.QueryB8.Read(0),"Manager shares adapter");Same(f.Query,f.Adapter.Query10.Read(0),"Adapter shares query");Same(f.Mesh.Mesh,f.Query.Mesh0.Read(0),"Query shares real mesh");Same(callback,f.Agent.Callback08.Read(0),"No callback or position success fabricated");Position(f.Agent.Position48,0.8f,0,0.2f,"Request completion does not move agent");Equal(0u,request.Generation00.Read(0),"Direct Submit does not advance generation");f.Unlocked();
  });
  Test("Projection mode appends a real reference and leaves unused source unknown",delegate{
   var f=new Fixture();f.Request.InstallOwnRefStorage(f.Storage,1);f.Storage.Cells[0].Write(0x12345678);f.Request.From04.X.Forget();f.Request.From04.Y.Forget();f.Request.From04.Z.Forget();f.Submit(0,1);Equal((byte)1,f.Request.Complete50.Read(0),"Nonzero actual nearest reference completes projection mode");Equal(2,f.Request.RefVector20.End08.Read(0).Index,"Exactly one new reference appended");Equal(0x12345678u,f.Storage.Cells[0].Read(0),"Existing reference retained");Equal(f.End,f.Storage.Cells[1].Read(0),"Real target reference appended");Unknown(f.Request.From04.X,"Projection does not require source X");
   XYZ(f.Request.To10,20,0,20);f.Submit(0,1);Equal((byte)0,f.Request.Complete50.Read(0),"No actual candidate writes false completion");Equal(2,f.Request.RefVector20.End08.Read(0).Index,"No reference appended on empty query");Equal(f.End,f.Storage.Cells[1].Read(0),"Earlier payload remains");f.Unlocked();
  });
  Test("Unknown request dependencies preserve completion and real failure prefixes",delegate{
   var f=new Fixture();f.Request.Complete50.Write(77);f.Request.Generation00.Forget();Missing(0x25a51e,delegate{f.Submit(0,0);},"Unknown generation cannot match by default");Equal((byte)77,f.Request.Complete50.Read(0),"Generation stop preserves completion");f.Unlocked();
   f.Request.Generation00.Write(0);f.Manager.QueryB8.Forget();Missing(0x25a529,delegate{f.Submit(0,0);},"Matched request requires actual query pointer");Equal(3,f.Request.RefVector20.End08.Read(0).Index,"Query failure precedes resize");Equal((byte)77,f.Request.Complete50.Read(0),"No false completion invented on exception");f.Unlocked();
   f.Manager.QueryB8.Write(f.Adapter);f.Costs.AreaCosts00[1].Forget();Missing(0x2620cb,delegate{f.Submit(0,0);},"Real corridor missing cost propagates through both wrappers");Equal(32,f.Request.RefVector20.End08.Read(0).Index,"Completed resize32 prefix retained");Equal(700u,f.Storage.Cells[0].Read(0),"No final path copy before cost failure");Equal(0u,f.Storage.Cells[31].Read(0),"New growth prefix retained");Equal((byte)77,f.Request.Complete50.Read(0),"No completion store after missing production dependency");Equal(2,f.Search.Pool58.Read(0).Count20.Read(0),"Real search allocations retained");f.Unlocked();
  });
  Test("Synchronous entry fragment copies agent inputs resets ends and wraps generation",delegate{
   var f=new Fixture();var group=f.Group();var request=f.Request;var marker=new object();request.WaypointVector38.Begin00.Write(marker);request.WaypointVector38.End08.Write(new object());request.Generation00.Write(UInt32.MaxValue);request.Complete50.Write(99);XYZ(request.From04,8,8,8);XYZ(request.To10,9,9,9);
   var invocation=new Invocation();NavigationCrowdStateContracts.SubmitSynchronousFragment25C78D(group,f.Agent,0,f.B,invocation);Equal(0x25c7e0u,invocation.At,"Stops at fragment return before later movement or callback");Equal(0u,request.Generation00.Read(0),"Native DWORD generation wraps");Equal((byte)1,request.Complete50.Read(0),"Real matching request completes");Position(request.From04,0.8f,0,0.2f,"Actual 8+4 source copy");Position(request.To10,0.2f,0,0.8f,"Actual 8+4 destination copy");Same(marker,request.WaypointVector38.End08.Read(0),"Opaque waypoint end reset to begin without inventing elements");Equal(2,request.RefVector20.End08.Read(0).Index,"Real path after reset");Equal(0u,f.Storage.Cells[2].Read(0),"Reset to begin causes old third element to be re-added and zeroed");Same(f.Manager,group.Owner00.Read(0),"Fragment uses the actual group owner");Same(f.Adapter,group.Query08.Read(0),"Created group captured the same adapter");Position(f.Agent.Position48,0.8f,0,0.2f,"Submission alone leaves position unchanged");f.Unlocked();
  });
  Test("Actual group creation and membership retain agent owner and query identities",delegate{
   var f=new Fixture();var group=f.Group();Same(group,f.Groups.Cells[0].Read(0),"GetGroup published the same object");Equal(1,f.Manager.Groups10.End08.Read(0).Index,"One group appended");Same(f.Manager,group.Owner00.Read(0),"Actual owner pointer");Same(f.Adapter,group.Query08.Read(0),"Actual query pointer");Same(null,group.Members28.Begin00.Read(0),"Native group member begin initially null");var members=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2);group.Members28.InstallOwnStorage(members,0);
   NavigationCrowdStateContracts.Join25C340(group,f.Agent,f.B,new Invocation());Equal(0u,f.Agent.Index00.Read(0),"Index assigned from real vector count before append");Equal(1,group.Members28.End08.Read(0).Index,"Actual membership append count");Same(f.Agent,members.Cells[0].Read(0),"Shared agent object appended, not a state copy");Equal(2,group.Upper1C.X.Read(0),"Actual radius and observed half expand group bound");Same(f.Agent.RequestE8,members.Cells[0].Read(0).RequestE8,"Member shares exact request identity");
   // The append is not transactional with the later position arithmetic.
   var failed=new Fixture();var failedGroup=failed.Group();var failedMembers=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1);failedGroup.Members28.InstallOwnStorage(failedMembers,0);failed.Agent.Position48.X.Forget();
   Missing(0x25c385,delegate{NavigationCrowdStateContracts.Join25C340(failedGroup,failed.Agent,failed.B,new Invocation());},"Unobserved X reaches first subtraction only after actual membership publication");Equal(0u,failed.Agent.Index00.Read(0),"Earlier index assignment retained on failure");Equal(1,failedGroup.Members28.End08.Read(0).Index,"Completed append is not rolled back");Same(failed.Agent,failedMembers.Cells[0].Read(0),"Published member remains the same agent on failure");Same(failed.Agent.RequestE8,failedMembers.Cells[0].Read(0).RequestE8,"Failed bound update does not replace the member request");Equal(0,failedGroup.Lower10.X.Read(0),"Lower X has not been written");Equal(0,failedGroup.Lower10.Y.Read(0),"Lower Y has not been written");Equal(0,failedGroup.Lower10.Z.Read(0),"Lower Z has not been written");Equal(1,failedGroup.Upper1C.X.Read(0),"Upper X retains old bound");Equal(1,failedGroup.Upper1C.Y.Read(0),"Upper Y retains old bound");Equal(1,failedGroup.Upper1C.Z.Read(0),"Upper Z retains old bound");Unknown(failed.Agent.Position48.X,"Failure does not invent a position");
   f.Manager.QueryB8.Forget();f.B.DisableGroupAssert422B90.Forget();Same(group,NavigationCrowdStateContracts.GetGroup25A250(f.Manager,null,null,f.B,new Invocation()),"Existing nonnull first group returns before unused bounds and diagnostics");Same(f.Adapter,group.Query08.Read(0),"Captured group query is not silently resynchronized");Equal(1,f.Manager.Groups10.End08.Read(0).Index,"Early return adds no group");
  });
 }
}
public static class NavigationCrowdStateTestProgram {
 public static int Main(){try{NavigationCrowdStateTests.Run();Console.WriteLine("PASS "+NavigationCrowdStateTests.Scenarios+" scenarios, "+NavigationCrowdStateTests.Assertions+" assertions; shared crowd request and production query chain in explicit own setup; no full native registration, movement or callback execution");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+NavigationCrowdStateTests.Scenarios+" scenarios, "+NavigationCrowdStateTests.Assertions+" assertions: "+e);return 1;}}
}
