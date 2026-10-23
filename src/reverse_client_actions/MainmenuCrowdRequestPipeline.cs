using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Only the new shared request submission is probed. The fixed saved-resource
// producer is reused as a method; no old test program or adapter suite is run.
// Positions are explicit inputs taken from actual produced polygons, not a
// fabricated callback result or evidence of loaded player/actor position.
public static class MainmenuCrowdRequestPipeline {
 // Reusable saved-resource producer for subsequent adapters. These are the
 // original shared instances, not snapshots or a second query implementation.
 public sealed class RequestContext {
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationQuery Query;
  public readonly NavigationCrowdAdapterState Adapter;
  public readonly NavigationCrowdAgent Agent;
  public readonly NavigationPoint3 From,To;
  public readonly int TileCount,CorridorCount,TileCrossings;
  public readonly string ReferenceText;
  public NavigationCrowdRequest Request{get{return Agent.RequestE8;}}
  public RequestContext(NavigationMeshTilesState mesh,NavigationQuery query,NavigationCrowdAdapterState adapter,NavigationCrowdAgent agent,NavigationPoint3 from,NavigationPoint3 to,int tileCount,int corridorCount,int crossings,string referenceText){Mesh=mesh;Query=query;Adapter=adapter;Agent=agent;From=from;To=to;TileCount=tileCount;CorridorCount=corridorCount;TileCrossings=crossings;ReferenceText=referenceText;}
 }
 static void Require(bool value,string why){if(!value)throw new InvalidOperationException(why);}
 static NavigationTile Tile(NavigationMeshTilesState s,uint r){return s.Mesh.Tiles50.Read(0)[(int)((r>>10)&4095)];}
 static NavigationPoly Poly(NavigationMeshTilesState s,uint r){return Tile(s,r).Polys10.Read(0)[(int)(r&1023)];}
 static NavigationPoint3 Center(NavigationMeshTilesState s,uint r){var p=Poly(s,r);var vertices=Tile(s,r).Vertices18.Read(0);int n=p.VertexCount1E.Read(0);Require(n>=3&&n<=6,"Actual polygon extent");float x=0,y=0,z=0;for(int j=0;j<n;j++){var v=vertices[p.Indices04[j].Read(0)];x+=v.X.Read(0);y+=v.Y.Read(0);z+=v.Z.Read(0);}var result=new NavigationPoint3();result.X.Write(x/n);result.Y.Write(y/n);result.Z.Write(z/n);return result;}
 static void Copy(NavigationPoint3 to,NavigationPoint3 from){to.X.CopyFrom(from.X);to.Y.CopyFrom(from.Y);to.Z.CopyFrom(from.Z);}
 static bool SamePoint(NavigationPoint3 a,NavigationPoint3 b){return a.X.Read(0)==b.X.Read(0)&&a.Y.Read(0)==b.Y.Read(0)&&a.Z.Read(0)==b.Z.Read(0);}
 static NavigationCrowdInt3 Int3(int value){var p=new NavigationCrowdInt3();p.X.Write(value);p.Y.Write(value);p.Z.Write(value);return p;}
 static IEnumerable<uint> Neighbors(NavigationMeshTilesState s,uint r){var tile=Tile(s,r);var links=s.Tile(tile,0).Links20.Read(0);uint at=s.Poly(Poly(s,r),0).FirstLink00.Read(0);var seen=new HashSet<uint>();while(at!=UInt32.MaxValue){Require(seen.Add(at)&&at<links.Length,"Actual link chain cycle or missing extent");var link=links[(int)at];uint target=link.Reference00.Read(0);Require(NavigationCoreContracts.ValidateReference(s.Mesh,target)==1,"Invalid actual linked polygon");yield return target;at=link.Next04.Read(0);}}
 static bool Linked(NavigationMeshTilesState s,uint a,uint b){foreach(uint r in Neighbors(s,a))if(r==b)return true;return false;}
 public static RequestContext BuildExplicitRequestContext(){
  var produced=MainmenuTilePipeline.BuildExplicitColdTiles();var mesh=NavigationMeshTilesState.Create(0,0,0,32,32,4096,1024,new NavigationSpatialState());mesh.UseObservedLinkConstants();var roots=new List<uint>();var owners=new List<NavigationMeshTileBytes>();
  foreach(var output in produced){byte[] bytes=output.Bytes;int x=BitConverter.ToInt32(bytes,8),z=BitConverter.ToInt32(bytes,12);Require(BitConverter.ToSingle(bytes,72)==x*32f&&BitConverter.ToSingle(bytes,80)==z*32f&&BitConverter.ToSingle(bytes,84)==(x+1)*32f&&BitConverter.ToSingle(bytes,92)==(z+1)*32f,"Actual tile bounds differ from explicit mesh grid");var owner=new NavigationMeshTileBytes(bytes,output.Length);var reference=new Field<uint>();var result=NavigationMeshTiles.Add(mesh,owner,reference);Require(result.Outcome==NavigationMeshTileOutcome.NativeSuccess&&result.NativeStatus==0x40000000,"Actual insertion: "+result.Detail);uint r=reference.Read(0);Require(r!=0&&Object.ReferenceEquals(owner,mesh.Tile(Tile(mesh,r),0).Data50.Read(0)),"Actual byte owner identity");roots.Add(r);owners.Add(owner);}
  Require(roots.Count==9,"All nine actual layers must be present");uint selectedFrom=0,selectedTo=0;
  foreach(uint basis in roots){var polys=Tile(mesh,basis).Polys10.Read(0);for(int n=0;n<polys.Length&&selectedFrom==0;n++){if((mesh.Spatial.Poly(polys[n],0).Flags1C.Read(0)&1)==0)continue;uint r=basis|(uint)n;foreach(uint next in Neighbors(mesh,r)){if((r>>10)==(next>>10)||(mesh.Spatial.Poly(Poly(mesh,next),0).Flags1C.Read(0)&1)==0)continue;selectedFrom=r;selectedTo=next;break;}}if(selectedFrom!=0)break;}
  Require(selectedFrom!=0&&selectedTo!=0,"Actual walkable external edge required for endpoint selection");var from=Center(mesh,selectedFrom);var to=Center(mesh,selectedTo);
  var query=new NavigationQuery();query.Mesh0.Write(mesh.Mesh);var adapter=new NavigationCrowdAdapterState();adapter.Query10.Write(query);adapter.Filter18.Include100.Write(1);adapter.Filter18.Exclude102.Write(0);adapter.Extent11C.X.Write(0.125f);adapter.Extent11C.Y.Write(2);adapter.Extent11C.Z.Write(0.125f);adapter.Critical138.Write(new NavigationCrowdCriticalSection());
  var search=new NavigationCorridorState(query);search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(4096,1024));search.Queue60.Write(NavigationSearchStorage.CreateQueue(4096));var costs=new NavigationCorridorFilterCosts(adapter.Filter18);for(int n=0;n<64;n++)costs.AreaCosts00[n].Write(1);search.Attach(costs);var adapterBoundary=new NavigationCrowdAdapterBoundaries(mesh,search);adapterBoundary.UseObservedConstants();
  var b=new NavigationCrowdStateBoundaries(adapterBoundary);b.DisableAgentAssert422B9F.Write(0);b.DisableGroupAssert422B90.Write(0);b.UseObservedGroupConstants();var manager=new NavigationCrowdManager();manager.CriticalA0.Write(new NavigationCrowdCriticalSection());manager.QueryB8.Write(adapter);manager.Margin00.X.Write(0);manager.Margin00.Y.Write(0);manager.Margin00.Z.Write(0);manager.MaxGroups0C.Write(2);var groupStorage=new NavigationCrowdPointerBuffer<NavigationCrowdGroup>(2);manager.Groups10.InstallOwnStorage(groupStorage,0);
  // Explicit own group bounds; no claim these are the runtime's registration
  // arguments. The actual GetGroup/Join body performs all publication writes.
  var group=NavigationCrowdStateContracts.GetGroup25A250(manager,Int3(-4096),Int3(4096),b,new Invocation());Require(Object.ReferenceEquals(group,groupStorage.Cells[0].Read(0))&&Object.ReferenceEquals(manager,group.Owner00.Read(0))&&Object.ReferenceEquals(adapter,group.Query08.Read(0)),"GetGroup did not publish shared owner/query identity");
  var agent=new NavigationCrowdAgent();var callbackIdentity=new object();Require(Object.ReferenceEquals(agent,NavigationCrowdStateContracts.InitializeAgent25E6C0(agent,Field<object>.Of(callbackIdentity),b,new Invocation())),"Constructor returned another agent");var request=agent.RequestE8;Require(request.RefVector20.Begin00.Read(0)==null&&request.RefVector20.End08.Read(0)==null&&request.RefVector20.Capacity10.Read(0)==null,"Native constructor null pointers must precede own storage setup");Copy(agent.Position48,from);Copy(agent.Target78,to);
  var references=new NavigationRefBuffer(32);request.InstallOwnRefStorage(references,0);var members=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1);group.Members28.InstallOwnStorage(members,0);NavigationCrowdStateContracts.Join25C340(group,agent,b,new Invocation());Require(agent.Index00.Read(0)==0&&group.Members28.End08.Read(0).Index==1&&Object.ReferenceEquals(agent,members.Cells[0].Read(0)),"Actual Join must publish the same agent, not a surrogate member");
  var invocation=new Invocation();NavigationCrowdStateContracts.SubmitSynchronousFragment25C78D(group,agent,0,b,invocation);Require(invocation.At==0x25c7e0,"Execution must stop at request-fragment return");Require(request.Generation00.Read(0)==1&&request.Complete50.Read(0)==1,"Actual request generation/completion publication failed");Require(Object.ReferenceEquals(request,agent.RequestE8)&&Object.ReferenceEquals(request,members.Cells[0].Read(0).RequestE8),"Member/request identity copied");Require(SamePoint(request.From04,from)&&SamePoint(request.To10,to),"Actual 8+4 request copies differ from selected geometry");Require(SamePoint(agent.Position48,from),"This probe must not claim or synthesize movement");
  var vector=request.RefVector20;Require(Object.ReferenceEquals(references,vector.Begin00.Read(0).Buffer)&&Object.ReferenceEquals(references,vector.End08.Read(0).Buffer),"Request lost explicit shared reference storage");int count=vector.End08.Read(0).Index;Require(count>1&&count<=32,"Expected nontrivial actual request corridor");Require(references.Cells[0].Read(0)==selectedFrom&&references.Cells[count-1].Read(0)==selectedTo,"Complete byte alone is insufficient: this actual probe requires both chosen endpoints");int crossings=0;var path=new List<string>();
  for(int n=0;n<count;n++){uint r=references.Cells[n].Read(0);Require(NavigationCoreContracts.ValidateReference(mesh.Mesh,r)==1,"Request contains invalid actual ref");path.Add(r.ToString("X8"));if(n!=0){uint prior=references.Cells[n-1].Read(0);Require(Linked(mesh,prior,r),"Request corridor contains nonadjacent polygons");if((prior>>10)!=(r>>10))crossings++;}}
  Require(crossings>0,"Actual request must cross a tile");Require(Object.ReferenceEquals(adapter,manager.QueryB8.Read(0))&&Object.ReferenceEquals(query,adapter.Query10.Read(0))&&Object.ReferenceEquals(mesh.Mesh,query.Mesh0.Read(0)),"Shared manager/adapter/query/mesh chain changed");Require(manager.OwnOutstandingEnters==0&&adapter.Critical138.Read(0).OwnOutstandingEnters==0,"Own lock release failed");for(int n=0;n<roots.Count;n++)Require(Object.ReferenceEquals(owners[n],mesh.Tile(Tile(mesh,roots[n]),0).Data50.Read(0)),"Original byte owner changed");
  return new RequestContext(mesh,query,adapter,agent,from,to,roots.Count,count,crossings,String.Join(",",path.ToArray()));
 }
 public static int Main(string[] args){try{
  Require(args.Length==1&&args[0]=="--explicit-default-domain","Explicit own cold domain required");var context=BuildExplicitRequestContext();
  Console.WriteLine("REQUEST CONFIG explicit own cold mesh/query; extents(.125,2,.125), pool4096/hash1024/cost1, group bounds[-4096,4096] margin0, preallocated group2/member1/ref32, managed locks and opaque non-invoked callback; loaded registration and native golden unobserved");
  Console.WriteLine("REQUEST generation="+context.Request.Generation00.Read(0)+" complete="+context.Request.Complete50.Read(0)+" count="+context.CorridorCount+" tileCrossings="+context.TileCrossings+" refs="+context.ReferenceText);Console.WriteLine("PASS actual saved tiles=9, same Agent/Group/Manager/Query request=1, complete endpoint-checked corridor; explicit own initialization only; no native golden, complete runtime registration, callback execution or character movement");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
