using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New adapter-only fixtures. All polygon bytes, links, nearest queries and
// corridor searches use the recovered production implementations. No callback
// supplies a query result. Geometry/configuration, managed Monitor and the
// preallocated vector are explicit own domains, not a loaded native crowd.
public static class NavigationCrowdAdapterTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected explicit unsupported: "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static void Position(NavigationPoint3 p,float x,float y,float z,string why){Equal(x,p.X.Read(0),why+" X");Equal(y,p.Y.Read(0),why+" Y");Equal(z,p.Z.Read(0),why+" Z");}
 static ushort[] Six(ushort a,ushort b,ushort c){return new ushort[]{a,b,c,65535,65535,65535};}
 static NavigationTileQuantizedVertex V(ushort x,ushort z){return new NavigationTileQuantizedVertex(x,0,z);}
 static byte[] Bytes(bool isolated){
  int z=isolated?3:0;
  var vertices=isolated?new[]{V(0,0),V(1,0),V(0,1)}:new[]{V(0,0),V(1,0),V(1,1),V(0,1)};
  var polygons=isolated?new[]{new NavigationTilePolygon(Six(0,1,2),Six(65535,65535,65535))}:new[]{new NavigationTilePolygon(Six(0,1,2),Six(65535,65535,1)),new NavigationTilePolygon(Six(0,2,3),Six(0,65535,65535))};
  var input=new NavigationTileDataInput(vertices,polygons,isolated?new byte[]{1}:new byte[]{1,2},isolated?new ushort[]{1}:new ushort[]{1,1},new NavigationTileBounds(0,0,z,1,2,z+1),1,1,0,z,0,2,0.5f,1);
  var output=new NavigationTileDataOutput();var result=NavigationTileData.Build(input,output);Check(result.Outcome==NavigationTileDataOutcome.Published,"Own fixture serializes: "+result.Detail);return output.Bytes;
 }
 sealed class Fixture {
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationCrowdAdapterState Adapter=new NavigationCrowdAdapterState();
  public readonly NavigationCrowdCriticalSection Critical=new NavigationCrowdCriticalSection();
  public readonly NavigationCorridorState Search;
  public readonly NavigationCorridorFilterCosts Costs;
  public readonly NavigationCrowdAdapterBoundaries B;
  public readonly NavigationPoint3 From=Point(0.8f,0,0.2f),To=Point(0.2f,0,0.8f);
  public readonly uint Start,End;
  public Fixture(){
   Mesh=NavigationMeshTilesState.Create(0,0,0,1,1,4,8,new NavigationSpatialState());Mesh.UseObservedLinkConstants();Query.Mesh0.Write(Mesh.Mesh);
   Adapter.Query10.Write(Query);Adapter.Critical138.Write(Critical);Adapter.Filter18.Include100.Write(1);Adapter.Filter18.Exclude102.Write(0);
   Adapter.Extent11C.X.Write(0.125f);Adapter.Extent11C.Y.Write(2);Adapter.Extent11C.Z.Write(0.125f);
   Search=new NavigationCorridorState(Query);Search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(16,8));Search.Queue60.Write(NavigationSearchStorage.CreateQueue(32));
   Costs=new NavigationCorridorFilterCosts(Adapter.Filter18);for(int n=0;n<64;n++)Costs.AreaCosts00[n].Write(1);Search.Attach(Costs);
   B=new NavigationCrowdAdapterBoundaries(Mesh,Search);B.UseObservedConstants();Start=Add(Bytes(false));End=Start|1;
  }
  public uint Add(byte[] bytes){var output=new Field<uint>();var added=NavigationMeshTiles.Add(Mesh,new NavigationMeshTileBytes(bytes,bytes.Length),output);Check(added.Outcome==NavigationMeshTileOutcome.NativeSuccess,"Fixture tile insertion: "+added.Detail);return output.Read(0);}
  public uint Project(NavigationPoint3 p,NavigationPoint3 output){return NavigationCrowdAdapterContracts.Project030B30(Adapter,p,output,B,new Invocation());}
  public byte Radius(NavigationPoint3 p,Field<float> radius,NavigationPoint3 output){return NavigationCrowdAdapterContracts.Project030BB0(Adapter,p,radius,output,B,new Invocation());}
  public byte Path(NavigationPoint3 from,NavigationPoint3 to,NavigationCrowdRefVector output){return NavigationCrowdAdapterContracts.FindCorridor030790(Adapter,from,to,output,B,new Invocation());}
  public void Unlocked(){Equal(0,Critical.OwnOutstandingEnters,"Own Monitor enter is released; not a native synchronization observation");}
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Projection uses real geometry and preserves borrowed output and query identities",delegate{
   var f=new Fixture();var output=Point(7,8,9);var x=output.X;Equal(f.Start,f.Project(f.From,output),"Slot18 returns real nearest ref");Position(output,0.8f,0,0.2f,"Actual projection");Same(x,output.X,"Borrowed output cells not replaced");Same(f.Query,f.Adapter.Query10.Read(0),"Same query object");Same(f.Mesh.Mesh,f.Query.Mesh0.Read(0),"Same registered mesh");
   Equal(f.End,f.Project(f.To,null),"Optional output is genuinely optional");Equal(f.Start,f.Project(f.From,f.From),"Input/output alias survives actual selector write order");Position(f.From,0.8f,0,0.2f,"In-place result");f.Unlocked();
  });
  Test("No candidate returns zero without overwriting caller output",delegate{
   var f=new Fixture();var output=Point(7,8,9);var distant=Point(20,0,20);Equal(0u,f.Project(distant,output),"Slot18 native no-ref result");Position(output,7,8,9,"No winning point");Equal((byte)0,f.Radius(distant,Field<float>.Of(0.125f),output),"Slot20 requires nonzero ref as well as success bit");Position(output,7,8,9,"No default point on false");f.Unlocked();
  });
  Test("Radius projection uses parameter XZ and only the stored Y extent",delegate{
   var f=new Fixture();var output=Point(7,8,9);f.Adapter.Extent11C.X.Forget();f.Adapter.Extent11C.Z.Forget();Equal((byte)1,f.Radius(f.From,Field<float>.Of(0.125f),output),"Unused stored XZ remain unknown");Position(output,0.8f,0,0.2f,"Actual radius projection");Unknown(f.Adapter.Extent11C.X,"No invented stored X");Unknown(f.Adapter.Extent11C.Z,"No invented stored Z");
   f.Adapter.Extent11C.Y.Forget();Missing(0x26197d,delegate{f.Radius(f.From,Field<float>.Of(0.125f),output);},"Copied unknown Y stops at its first production arithmetic load");f.Adapter.Extent11C.Y.Write(2);Missing(0x261983,delegate{f.Radius(f.From,new Field<float>(),output);},"Unknown scalar parameter is not default radius; copied Z is consumed before X");Position(output,0.8f,0,0.2f,"Earlier successful result remains after missing observations");f.Unlocked();
  });
  Test("Unknown lock and typed query stop without fabricated dispatch or retained own lock",delegate{
   var f=new Fixture();var output=Point(7,8,9);f.Adapter.Critical138.Forget();Missing(0x030b4f,delegate{f.Project(f.From,output);},"No default critical section");f.Adapter.Critical138.Write(f.Critical);f.Adapter.Query10.Forget();Missing(0x030b76,delegate{f.Project(f.From,output);},"Unknown typed query cannot cross the frozen callee interface");Position(output,7,8,9,"No output on unsupported dispatch");f.Unlocked();f.Adapter.Query10.Write(f.Query);Equal(f.Start,f.Project(f.From,output),"The same own lock and query remain usable after failure");f.Unlocked();
  });
  Test("Real corridor resizes only added cells and shrinks without replacing storage",delegate{
   var f=new Fixture();var vector=new NavigationCrowdRefVector(40,3);var storage=vector.Storage;for(int n=0;n<3;n++)storage.Cells[n].Write((uint)(1000+n));
   Equal((byte)1,f.Path(f.From,f.To,vector),"Slot0 complete corridor");Equal(2,vector.End08.Read(0).Index,"Successful resize uses actual count");Same(storage,vector.Begin00.Read(0).Buffer,"Begin keeps original backing");Same(storage,vector.End08.Read(0).Buffer,"End keeps original backing");Equal(f.Start,storage.Cells[0].Read(0),"Start ref");Equal(f.End,storage.Cells[1].Read(0),"End ref");Equal(1002u,storage.Cells[2].Read(0),"Pre-existing third cell survives shrink");Equal(0u,storage.Cells[3].Read(0),"Only growth range was initialized");Equal(0u,storage.Cells[31].Read(0),"Native fixed32 growth upper cell");Unknown(storage.Cells[32],"Unused allocation capacity is not initialized by resize32");
   Equal((byte)1,f.Path(f.From,f.From,vector),"Same-polygon reuse");Equal(1,vector.End08.Read(0).Index,"Same-polygon result shrinks to one");Equal(f.End,storage.Cells[1].Read(0),"Shrink does not clear old second cell");Equal(0u,storage.Cells[2].Read(0),"Next growth from length2 clears newly re-added cell");f.Unlocked();
  });
  Test("Disconnected real target preserves native partial path as adapter success",delegate{
   var f=new Fixture();uint isolated=f.Add(Bytes(true));var vector=new NavigationCrowdRefVector(32,0);Equal((byte)1,f.Path(f.From,Point(0.2f,0,3.2f),vector),"Slot0 tests failure bit, not complete-goal reachability");Equal(2,vector.End08.Read(0).Index,"Reachable partial path count");Equal(f.Start,vector.Storage.Cells[0].Read(0),"Partial start");Equal(f.End,vector.Storage.Cells[1].Read(0),"Reachable neighbor is last");Check(vector.Storage.Cells[1].Read(0)!=isolated,"No fake disconnected goal");f.Unlocked();
  });
  Test("Insufficient preallocated vector stops at unrecovered allocator without replacing backing",delegate{
   var f=new Fixture();var vector=new NavigationCrowdRefVector(16,1);vector.Storage.Cells[0].Write(0x12345678);var storage=vector.Storage;Missing(0x030dc7,delegate{f.Path(f.From,f.To,vector);},"No allocation callback or invented growth");Same(storage,vector.Begin00.Read(0).Buffer,"Backing not replaced");Equal(1,vector.End08.Read(0).Index,"Length unchanged before missing allocator");Equal(0x12345678u,storage.Cells[0].Read(0),"Existing payload preserved");Equal(0,f.Search.Pool58.Read(0).Count20.Read(0),"Corridor search was not entered");f.Unlocked();
  });
  Test("Unknown area cost propagates after resize32 and real search initialization",delegate{
   var f=new Fixture();f.Costs.AreaCosts00[1].Forget();var vector=new NavigationCrowdRefVector(32,1);vector.Storage.Cells[0].Write(0x12345678);Missing(0x2620cb,delegate{f.Path(f.From,f.To,vector);},"Nearest does not consume area cost; real corridor does");Equal(32,vector.End08.Read(0).Index,"Pre-search growth remains on unsupported cost");Equal(0x12345678u,vector.Storage.Cells[0].Read(0),"No final corridor copy before cost failure");Equal(0u,vector.Storage.Cells[1].Read(0),"Newly added range remains initialized");Equal(0u,vector.Storage.Cells[31].Read(0),"Growth prefix remains at last slot");Equal(2,f.Search.Pool58.Read(0).Count20.Read(0),"Actual start and neighbor allocations survive stop");f.Unlocked();
  });
 }
}
public static class NavigationCrowdAdapterTestProgram {
 public static int Main(){try{NavigationCrowdAdapterTests.Run();Console.WriteLine("PASS "+NavigationCrowdAdapterTests.Scenarios+" scenarios, "+NavigationCrowdAdapterTests.Assertions+" assertions; production queries through own synchronized adapter and preallocated vector; no native golden, loaded crowd or character motion");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+NavigationCrowdAdapterTests.Scenarios+" scenarios, "+NavigationCrowdAdapterTests.Assertions+" assertions: "+e);return 1;}}
}
