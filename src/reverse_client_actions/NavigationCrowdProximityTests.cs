using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New pure-memory Prepare -> Insert -> Neighbors fixtures. Agent fields and
// empty group header are explicit own inputs; production code constructs the
// actual pools, buckets and output. No result callback, native golden, loaded
// registration, complete avoidance update or character movement is claimed.
public static class NavigationCrowdProximityTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object a,object b,string why){Check(Object.ReferenceEquals(a,b),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected explicit unsupported: "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static NavigationCrowdAgent Agent(float x,float y,float z,uint flags){var a=new NavigationCrowdAgent();a.Position48.X.Write(x);a.Position48.Y.Write(y);a.Position48.Z.Write(z);a.ParameterBits1C40[1].Write(flags);return a;}
 sealed class Result {
  public readonly NavigationCrowdPointerBuffer<NavigationCrowdAgent> Storage;
  public readonly NavigationCrowdPointerVector<NavigationCrowdAgent> Vector=new NavigationCrowdPointerVector<NavigationCrowdAgent>();
  public Result(int capacity,int count){Storage=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(capacity);Vector.InstallOwnStorage(Storage,count);}
  public int Count {get{return Vector.End08.Read(0).Index;}}
  public NavigationCrowdAgent At(int index){var begin=Vector.Begin00.Read(0);return begin.Buffer.At(begin.Index+index,0).Read(0);}
 }
 sealed class Fixture {
  public readonly NavigationCrowdGroup Group=new NavigationCrowdGroup();
  public readonly NavigationCrowdProximityIndex Index;
  public readonly NavigationCrowdStateBoundaries State=new NavigationCrowdStateBoundaries(null);
  public readonly NavigationCrowdProximityBoundaries B;
  public Fixture(bool prepare){
   Group.Pointer58.Write(null);Group.Pointer60.Write(null);Group.Count68.Write(0);Index=new NavigationCrowdProximityIndex(Group);State.UseObservedGroupConstants();B=new NavigationCrowdProximityBoundaries(State);if(prepare)Prepare(1);
  }
  public void Prepare(uint requested){NavigationCrowdProximityContracts.Prepare25E900(Index,requested,new Invocation());}
  public void Insert(NavigationCrowdAgent agent){NavigationCrowdProximityContracts.Insert25EA00(Index,agent,B,new Invocation());}
  public void Query(NavigationPoint3 center,float radius,float height,uint mask,NavigationCrowdAgent excluded,Result output){NavigationCrowdProximityContracts.Neighbors25EB90(Index,center,radius,height,mask,excluded,output.Vector,B,new Invocation());}
  public NavigationCrowdPointerBuffer<object> Buckets {get{return (NavigationCrowdPointerBuffer<object>)Index.Buckets00.Read(0);}}
  public NavigationCrowdProximityNodeBuffer Nodes {get{return (NavigationCrowdProximityNodeBuffer)Index.Nodes08.Read(0);}}
  public NavigationCrowdProximityNode Free {get{return (NavigationCrowdProximityNode)Index.Free18.Read(0);}}
  public void EmptyBuckets(string why){bool empty=true;foreach(var cell in Buckets.Cells)if(cell.Read(0)!=null)empty=false;Check(empty,why);}
 }
 static bool Contains(Result r,NavigationCrowdAgent agent){for(int n=0;n<r.Count;n++)if(Object.ReferenceEquals(agent,r.At(n)))return true;return false;}
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Prepare creates real empty buckets and reverse free-list in shared group fields",delegate{
   var f=new Fixture(false);Same(f.Group.Pointer58,f.Index.Buckets00,"Bucket header aliases actual group cell");Same(f.Group.Pointer60,f.Index.Nodes08,"Node header aliases actual group cell");Same(f.Group.Pointer70,f.Index.Free18,"Free header aliases actual group cell");Same(f.Group.Count78,f.Index.Count20,"Count aliases actual group cell");Unknown(f.Index.Free18,"Own empty setup does not prebuild a free list");Unknown(f.Index.Count20,"Prepare must establish count");f.Prepare(0);
   Equal(128u,f.Index.Capacity10.Read(0),"Native <=64 minimum capacity");Equal(128,f.Nodes.Length,"Actual allocated node extent");Equal(0u,f.Index.Count20.Read(0),"Count initialized");f.EmptyBuckets("Every bucket was actually cleared");Same(f.Nodes.At(127,0),f.Free,"Last initialized node is free-list head");var seen=new HashSet<NavigationCrowdProximityNode>();object p=f.Index.Free18.Read(0);int expected=127;bool ordered=true;
   while(p!=null){var node=(NavigationCrowdProximityNode)p;if(!seen.Add(node))throw new Exception("Free-list cycle");ordered=ordered&&node.Index==expected&&Object.ReferenceEquals(node.Buffer,f.Nodes);expected--;p=node.Next18.Read(0);}Check(ordered&&expected==-1&&seen.Count==128,"Free chain visits each actual node once in reverse allocation order");Unknown(f.Nodes.At(127,0).Gap0C,"Prepare leaves node padding unobserved");Unknown(f.Nodes.At(127,0).Agent10,"Prepare does not manufacture an agent");
   var oldNodes=f.Nodes;var oldBuckets=f.Buckets;f.Prepare(65);Equal(128u,f.Index.Capacity10.Read(0),"65 rounds up to128 rather than256");Same(oldNodes,f.Nodes,"65 request reuses sufficient node capacity");Same(oldBuckets,f.Buckets,"65 request reuses sufficient bucket capacity");f.Prepare(129);Equal(256u,f.Index.Capacity10.Read(0),"Above128 grows to next power of two");Equal(256,f.Nodes.Length,"New allocation extent");Same(f.Nodes.At(255,0),f.Free,"New reverse free-list head");Missing(0,delegate{oldNodes.RequireLive(0);},"Old owned node allocation released");Missing(0,delegate{oldBuckets.RequireLive(0);},"Old owned bucket allocation released");
  });
  Test("Reused Prepare resets index links without erasing node payload or padding",delegate{
   var f=new Fixture(true);var nodes=f.Nodes;var buckets=f.Buckets;var agent=Agent(1,2,3,1);var used=f.Free;used.Gap0C.Write(0x1234abcd);f.Insert(agent);Equal(1u,f.Index.Count20.Read(0),"Real insertion count");Same(agent,used.Agent10.Read(0),"Inserted agent identity");f.Prepare(1);
   Same(nodes,f.Nodes,"No replacement when capacity suffices");Same(buckets,f.Buckets,"Bucket allocation reused");Same(used,f.Free,"Original last node becomes free head again");f.EmptyBuckets("All previous bucket heads cleared");Equal(0u,f.Index.Count20.Read(0),"Count reset");Equal(1,used.X00.Read(0),"Unwritten cached X retained");Equal(2,used.Y04.Read(0),"Unwritten cached Y retained");Equal(3,used.Z08.Read(0),"Unwritten cached Z retained");Same(agent,used.Agent10.Read(0),"Unwritten agent field retained");Equal(0x1234abcdu,used.Gap0C.Read(0),"Padding retained");Same(nodes.At(126,0),used.Next18.Read(0),"Next was rebuilt for the free chain");
  });
  Test("Neighbors preserves identity and applies self mask and strict three-dimensional distance",delegate{
   var f=new Fixture(true);var self=Agent(0,0,0,1);var a=Agent(0.25f,0,0,1);var twin=Agent(0.25f,0,0,1);var vertical=Agent(0,1.5f,0,1);var exact=Agent(2,0,0,1);var outside3D=Agent(1.5f,1.5f,0,1);var masked=Agent(0.5f,0,0,2);
   foreach(var agent in new[]{self,a,twin,vertical,exact,outside3D,masked})f.Insert(agent);self.ParameterBits1C40[1].Forget();masked.Position48.X.Forget();masked.Position48.Y.Forget();masked.Position48.Z.Forget();var result=new Result(16,0);f.Query(Point(0,0,0),2,4,1,self,result);
   Equal(3,result.Count,"Only three eligible identities");Check(Contains(result,a)&&Contains(result,twin)&&Contains(result,vertical),"Equal positions do not collapse distinct agent identities and Y participates in real distance");Check(!Contains(result,self),"Self pointer excluded before unknown mask consumption");Check(!Contains(result,masked),"Mask rejects before unknown position consumption");Check(!Contains(result,exact),"Distance equal to radius is excluded");Check(!Contains(result,outside3D),"XZ-close candidate can still be outside the three-dimensional sphere");Same(f.B.Half34BADC,f.State.Half34BADC,"Observed half constant is a shared state cell");
  });
  Test("Negative coordinates truncate toward zero and colliding cells are filtered before payload",delegate{
   var f=new Fixture(true);var near=Agent(-0.75f,0,0,1);var far=Agent(128,0,0,1);var nearNode=f.Free;f.Insert(near);var farNode=f.Free;f.Insert(far);Equal(0,nearNode.X00.Read(0),"CVTTSS2SI is truncation rather than floor");Equal(128,farNode.X00.Read(0),"Far cached cell");Same(farNode,f.Buckets.Cells[0].Read(0),"Cells0 and128 share hash bucket modulo128");Same(nearNode,farNode.Next18.Read(0),"Head insertion preserves actual collision chain");far.ParameterBits1C40[1].Forget();far.Position48.X.Forget();far.Position48.Y.Forget();far.Position48.Z.Forget();var result=new Result(4,0);f.Query(Point(-0.25f,0,0),0.6f,2,1,null,result);
   Equal(1,result.Count,"Only cell0 candidate survives");Same(near,result.At(0),"Cached coordinate comparison excludes collision before unobserved mask and position");
  });
  Test("Results follow Y then Z then X cells and reverse insertion order within a cell",delegate{
   var f=new Fixture(true);var lowY=Agent(0,-1,0,1);var lowZ=Agent(0,0,-1,1);var lowX=Agent(-1,0,0,1);var oldCenter=Agent(0.1f,0,0,1);var newCenter=Agent(0.2f,0,0,1);var highX=Agent(1,0,0,1);var highZ=Agent(0,0,1,1);var highY=Agent(0,1,0,1);
   foreach(var agent in new[]{highY,oldCenter,lowZ,highX,lowY,newCenter,highZ,lowX})f.Insert(agent);var result=new Result(12,0);f.Query(Point(0,0,0),4,6,1,null,result);var expected=new[]{lowY,lowZ,lowX,newCenter,oldCenter,highX,highZ,highY};Equal(expected.Length,result.Count,"Every actual eligible node once");for(int n=0;n<expected.Length;n++)Same(expected[n],result.At(n),"Recovered cell/chain iteration order "+n);
  });
  Test("Unknown insertion position retains popped free-list and only completed coordinate stores",delegate{
   var f=new Fixture(true);var first=f.Free;var afterFirst=first.Next18.Read(0);var missingX=Agent(1,2,3,1);missingX.Position48.X.Forget();Missing(0x25ea1b,delegate{f.Insert(missingX);},"X consumption follows free-list pop");Same(afterFirst,f.Index.Free18.Read(0),"First popped node not rolled back");Unknown(first.X00,"No X store before missing X");Unknown(first.Agent10,"No invented agent publication");Equal(0u,f.Index.Count20.Read(0),"No successful insertion count");f.EmptyBuckets("No bucket published on first conversion failure");
   var second=f.Free;var afterSecond=second.Next18.Read(0);var missingZ=Agent(1.9f,2.8f,3,1);missingZ.Position48.Z.Forget();Missing(0x25ea2c,delegate{f.Insert(missingZ);},"Z consumption precedes agent and Z stores");Same(afterSecond,f.Index.Free18.Read(0),"Second pop retained");Equal(1,second.X00.Read(0),"Completed truncated X prefix");Equal(2,second.Y04.Read(0),"Completed truncated Y prefix");Unknown(second.Z08,"Z never stored");Unknown(second.Agent10,"Agent pointer follows successful Z conversion");Equal(0u,f.Index.Count20.Read(0),"No phantom insertion after partial node writes");f.EmptyBuckets("No bucket publication after unknown Z");
  });
  Test("Neighbor output appends without clearing and grows through recovered034FE0",delegate{
   var f=new Fixture(true);var a=Agent(0.1f,0,0,1);var b=Agent(0.2f,0,0,1);var c=Agent(0.3f,0,0,1);f.Insert(a);f.Insert(b);f.Insert(c);var result=new Result(3,1);var prior=new NavigationCrowdAgent();result.Storage.Cells[0].Write(prior);var free=f.Index.Free18.Read(0);
   // The previously passed version stopped at25EE4F; its original evidence is
   // retained in r1-crowd-proximity-validation. This necessary regression now
   // exercises the newly recovered caller calculation: max(3+1,3+(3>>1))=4.
   f.Query(Point(0,0,0),1,2,1,null,result);Equal(4,result.Count,"Third eligible neighbor appended after real growth");Same(prior,result.At(0),"Existing output element was not cleared");Same(c,result.At(1),"First head-chain neighbor retained");Same(b,result.At(2),"Second neighbor retained");Same(a,result.At(3),"Third neighbor appended to fresh allocation");var grown=result.Vector.Begin00.Read(0).Buffer;Check(!Object.ReferenceEquals(result.Storage,grown),"Growth replaces backing rather than overwriting the old extent");Same(grown,result.Vector.End08.Read(0).Buffer,"End publishes the same fresh allocation");Same(grown,result.Vector.Capacity10.Read(0).Buffer,"Capacity publishes the same fresh allocation");Equal(4,result.Vector.Capacity10.Read(0).Index,"25EE22..25EE4B requests exact computed capacity4");Missing(0,delegate{result.Storage.At(0,0);},"Old allocation is invalid after release");Same(free,f.Index.Free18.Read(0),"Read-only query does not consume index free-list");Equal(3u,f.Index.Count20.Read(0),"Query does not rewrite inserted count");
  });
 }
}
public static class NavigationCrowdProximityTestProgram {
 public static int Main(){try{NavigationCrowdProximityTests.Run();Console.WriteLine("PASS "+NavigationCrowdProximityTests.Scenarios+" scenarios, "+NavigationCrowdProximityTests.Assertions+" assertions; production Prepare/Insert/Neighbors over explicit own shared Agent/Group fixtures; no native golden, complete motion update or character movement");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+NavigationCrowdProximityTests.Scenarios+" scenarios, "+NavigationCrowdProximityTests.Assertions+" assertions: "+e);return 1;}}
}
