using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Own immutable-during-call input fixtures through the same borrowed-view API.
// These are algorithm assertions, never captured native runtime golden data.
public static class NavigationLayerPolygonsTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static ushort[] Poly(params ushort[] words){var p=new ushort[6];for(int i=0;i<6;i++)p[i]=i<words.Length?words[i]:(ushort)65535;return p;}
 static ushort[] Records(params ushort[][] polygons){var p=new ushort[polygons.Length*12];for(int i=0;i<p.Length;i++)p[i]=65535;for(int i=0;i<polygons.Length;i++)Array.Copy(polygons[i],0,p,i*12,6);return p;}
 static ushort[] Square(int width,int depth){return new ushort[]{0,0,0,0,0,(ushort)depth,(ushort)width,0,(ushort)depth,(ushort)width,0,0};}
 static NavigationPolygonContourInput Input(byte area,params byte[] quads){return new NavigationPolygonContourInput(quads,quads.Length/4,area);}
 static NavigationPolygonContourInput Triangle(byte y,byte area,byte firstFlag){return Input(area,0,y,0,firstFlag,0,y,4,15,4,y,0,15);}
 static void Words(ushort[] expected,ushort[] actual,int at,string why){for(int i=0;i<expected.Length;i++)Equal(expected[i],actual[at+i],why+" word "+i);}
 static void Success(NavigationPolygonResult r,int vertices,int polygons){Equal(NavigationPolygonOutcome.Success,r.Outcome,"Managed normal return");Equal((uint?)0x40000000u,r.NativeStatus,"Observed success status");Equal(6,r.Nvp,"Nvp6");Equal(vertices,r.VertexCount,"Vertex count");Equal(polygons,r.PolygonCount,"Polygon count");}
 static NavigationPolygonDomainException Domain(Action body){try{body();throw new Exception("Expected own domain stop");}catch(NavigationPolygonDomainException e){assertions++;return e;}}

 public static void RunMemory(){
  scenarios=0;assertions=0;
  Test("Merge selects the common diagonal and joins a clockwise square",delegate{
   var a=Poly(0,1,2);var b=Poly(0,2,3);int ea=91,eb=92;
   Equal(32,NavigationLayerPolygons.MergeScore(a,0,b,0,Square(4,4),ref ea,ref eb),"Squared diagonal score");Equal(2,ea,"A common edge");Equal(0,eb,"B common edge");
   var oldB=(ushort[])b.Clone();NavigationLayerPolygons.Merge(a,0,b,0,ea,eb);Words(Poly(0,1,2,3),a,0,"Merged order");Words(oldB,b,0,"Separate B unchanged");
  });
  Test("Merge score preserves early outputs and chooses last matching A edge",delegate{
   int ea=77,eb=88;Equal(-1,NavigationLayerPolygons.MergeScore(Poly(0,1,2,3,4),0,Poly(0,1,2,3),0,new ushort[15],ref ea,ref eb),"Over six vertices rejected");Equal(77,ea,"Early A untouched");Equal(88,eb,"Early B untouched");
   Equal(-1,NavigationLayerPolygons.MergeScore(Poly(0,1,2),0,Poly(0,1,2),0,Square(4,4),ref ea,ref eb),"Identical winding fails convex join");Equal(2,ea,"Last matching A");Equal(2,eb,"First B for last A");
   int alias=99;Equal(-1,NavigationLayerPolygons.MergeScore(Poly(0,1,2),0,Poly(0,2,3),0,Square(4,4),ref alias,ref alias),"Aliased output is reread");Equal(0,alias,"Write i then j shares cell");
  });
  Test("Merge score uses wrapping int32 length rather than saturation",delegate{
   int ea=0,eb=0;int expected=unchecked(50000*50000+1);
   Equal(expected,NavigationLayerPolygons.MergeScore(Poly(0,1,2),0,Poly(0,2,3),0,Square(50000,1),ref ea,ref eb),"Negative wrapped score is preserved");Check(expected<0&&expected!=-1,"Distinguish wrapped score from no merge");Equal(2,ea,"Geometry still located");Equal(0,eb,"Geometry still located B");
  });
  Test("Merge reads both aliases before output and itself does not cap nA+nB",delegate{
   var shared=Poly(0,1,2);NavigationLayerPolygons.Merge(shared,0,shared,0,2,0);Words(Poly(0,1,1,2),shared,0,"Scratch before shared output");
   var a=Poly(0,1,2,3,4,5);NavigationLayerPolygons.Merge(a,0,Poly(6,7,8,9,10,11),0,0,0);Words(Poly(1,2,3,4,5,7),a,0,"Helper copies first six of ten scratch entries");
  });
  Test("Merge own extent stops preserve native 8 plus 4 byte store groups",delegate{
   var shortA=new ushort[]{65535,91,92};var before=(ushort[])shortA.Clone();var e=Domain(delegate{NavigationLayerPolygons.Merge(shortA,0,Poly(6,7,8,9,10,11),0,0,0);});Equal(0x26b247u,e.Rva,"First 8B store boundary");Words(before,shortA,0,"No partial first store");
   var five=new ushort[]{65535,91,92,93,94};e=Domain(delegate{NavigationLayerPolygons.Merge(five,0,Poly(6,7,8,9,10,11),0,0,0);});Equal(0x26b24bu,e.Rva,"Second 4B store boundary");Words(new ushort[]{7,8,9,10,94},five,0,"Completed first store retained");
  });
  Test("Adjacency pairs reverse edges and keeps ascending insertion order",delegate{
   var p=Records(Poly(0,1,2),Poly(0,2,3));NavigationLayerPolygons.BuildAdjacency(p,2,Square(4,4),4,new NavigationPolygonContourInput[0]);
   Words(new ushort[]{65535,65535,1,65535,65535,65535},p,6,"First polygon neighbor");Words(new ushort[]{0,65535,65535,65535,65535,65535},p,18,"Second polygon neighbor");
   var three=Records(Poly(0,1,2),Poly(0,2,3),Poly(0,2,3));NavigationLayerPolygons.BuildAdjacency(three,3,Square(4,4),4,new NavigationPolygonContourInput[0]);
   Equal((ushort)2,three[8],"Newest ascending edge head paired first");Equal((ushort)65535,three[18],"Older duplicate stays unpaired");Equal((ushort)0,three[30],"Newest reverse neighbor");
  });
  Test("Portal edge labels follow original contour direction and ignore Y",delegate{
   var c=Input(8,0,9,0,0,0,17,4,1,4,33,4,2,4,99,0,3);var p=Records(Poly(0,1,2,3));
   NavigationLayerPolygons.BuildAdjacency(p,1,Square(4,4),4,new NavigationPolygonContourInput[]{c});Words(new ushort[]{32768,32769,32770,32771,65535,65535},p,6,"Four portal directions");
  });
  Test("Portal intervals exclude endpoint contact and later labels overwrite",delegate{
   var endpoint=Input(1,0,9,4,0,0,77,8,15,9,0,9,15);var p=Records(Poly(0,1,2,3));NavigationLayerPolygons.BuildAdjacency(p,1,Square(4,4),4,new NavigationPolygonContourInput[]{endpoint});Equal((ushort)65535,p[6],"Touch at4 does not overlap");
   var first=Input(1,0,9,1,2,0,77,3,15,9,0,9,15);var last=Input(1,0,19,1,0,0,88,3,15,9,0,9,15);
   NavigationLayerPolygons.BuildAdjacency(p,1,Square(4,4),4,new NavigationPolygonContourInput[]{first,last});Equal((ushort)32768,p[6],"Later direction0 overwrites2");
  });
  Test("Strict intersection uses unsigned XZ and excludes collinear or endpoint contact",delegate{
   var crossing=Input(0,0,99,0,255,255,1,255,255,0,77,255,255,255,9,0,255).Vertices;
   Check(NavigationLayerPolygons.IntersectStrict(crossing,0,4,8,12),"255 coordinates are unsigned and proper crossing accepted");
   var collinear=Input(0,0,9,0,15,4,8,0,15,2,7,0,15,6,6,0,15).Vertices;
   Check(!NavigationLayerPolygons.IntersectStrict(collinear,0,4,8,12),"Collinear overlap is not strict crossing");
   var touch=Input(0,0,0,0,15,4,0,0,15,4,0,0,15,4,0,4,15).Vertices;
   Check(!NavigationLayerPolygons.IntersectStrict(touch,0,4,8,12),"Endpoint touch rejected by strict helper");Check(NavigationLayerPolygons.Intersect(touch,0,4,8,12),"Wrapper admits endpoint via Between");
   var separate=Input(0,0,0,0,15,2,0,0,15,3,0,1,15,3,0,4,15).Vertices;
   Check(!NavigationLayerPolygons.Intersect(separate,0,4,8,12),"Disjoint segments rejected");
  });
  Test("Between preserves inclusive reversed axes and degenerate native branch",delegate{
   var horizontal=Input(0,8,0,2,15,2,0,2,15,5,99,2,255).Vertices;Check(NavigationLayerPolygons.Between(horizontal,0,4,8),"Reversed horizontal interval");
   Check(NavigationLayerPolygons.Between(horizontal,0,4,0),"A endpoint inclusive");Check(NavigationLayerPolygons.Between(horizontal,0,4,4),"B endpoint inclusive");
   var vertical=Input(0,3,0,8,15,3,0,2,15,3,77,5,15).Vertices;Check(NavigationLayerPolygons.Between(vertical,0,4,8),"Reversed vertical interval");vertical[8]=4;Check(!NavigationLayerPolygons.Between(vertical,0,4,8),"Noncollinear point rejected");
   var degenerate=Input(0,2,0,7,15,2,99,7,15,99,5,7,15).Vertices;
   Check(NavigationLayerPolygons.Between(degenerate,0,4,8),"Zero-length AB falls through Z-only check, not Euclidean point equality");degenerate[10]=8;Check(!NavigationLayerPolygons.Between(degenerate,0,4,8),"Degenerate Z still bounded");
  });
  Test("Intersection wrapper reaches the ordered later Between branches",delegate{
   var second=Input(0,0,0,0,15,4,0,0,15,5,0,0,15,2,0,0,15).Vertices;
   Check(!NavigationLayerPolygons.Between(second,0,4,8),"First endpoint outside AB");Check(NavigationLayerPolygons.Intersect(second,0,4,8,12),"Second endpoint D inside AB");
   var third=Input(0,1,0,0,15,2,0,0,15,0,0,0,15,4,0,0,15).Vertices;
   Check(!NavigationLayerPolygons.Between(third,0,4,8)&&!NavigationLayerPolygons.Between(third,0,4,12),"Neither CD endpoint inside AB");Check(NavigationLayerPolygons.Intersect(third,0,4,8,12),"A inside CD");
   var fourth=Input(0,0,0,0,15,2,0,2,15,1,0,2,15,3,0,2,15).Vertices;
   Check(!NavigationLayerPolygons.Between(fourth,0,4,8)&&!NavigationLayerPolygons.Between(fourth,0,4,12)&&!NavigationLayerPolygons.Between(fourth,8,12,0),"First three Between results false");Check(NavigationLayerPolygons.Intersect(fourth,0,4,8,12),"Fourth branch B on CD");
  });
  Test("Segment predicates preserve first reads and short circuit invalid later addresses",delegate{
   var e=Domain(delegate{NavigationLayerPolygons.IntersectStrict(null,0,4,8,12);});Equal(0x26a666u,e.Rva,"Strict first reads C.X");
   e=Domain(delegate{NavigationLayerPolygons.Between(null,0,4,8);});Equal(0x26a73au,e.Rva,"Between first reads A.Z");
   var collinear=Input(0,0,0,0,15,4,0,0,15,2,0,0,15).Vertices;
   Check(!NavigationLayerPolygons.IntersectStrict(collinear,0,4,8,100),"First zero cross avoids D");Check(NavigationLayerPolygons.Intersect(collinear,0,4,8,100),"First Between true avoids invalid D throughout wrapper");
   var noncollinear=Input(0,0,0,0,15,4,0,0,15,2,0,2,15).Vertices;
   e=Domain(delegate{NavigationLayerPolygons.IntersectStrict(noncollinear,0,4,8,12);});Equal(0x26a6a0u,e.Rva,"After first cross, D.Z precedes D.X");
  });
  Test("Empty or fewer-than-three contours never dereference ignored buffers",delegate{
   var empty=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[0]);Success(empty,0,0);Equal(0,empty.Polygons.Length,"Empty managed allocation");
   var skipped=new NavigationPolygonContourInput[]{new NavigationPolygonContourInput(null,2,99),new NavigationPolygonContourInput(null,-1,99)};var r=NavigationLayerPolygons.Build(skipped);Success(r,0,0);Same(skipped,r.Inputs,"Borrowed input view array retained");
  });
  Test("Clockwise quad triangulates then merges through the actual helper chain",delegate{
   var c=Input(7,0,0,0,15,0,0,4,15,4,0,4,15,4,0,0,15);var bytes=c.Vertices;var old=(byte[])bytes.Clone();var inputs=new NavigationPolygonContourInput[]{c};var r=NavigationLayerPolygons.Build(inputs);
   Success(r,4,1);Same(inputs,r.Inputs,"Exact view array");Same(bytes,r.Inputs[0].Vertices,"Exact borrowed quad array");Words(Poly(0,1,2,3),r.Polygons,0,"Quad polygon order");Equal((byte)7,r.Areas[0],"Area copied");Equal((ushort)0,r.Flags[0],"Flags initialized to zero");Equal(2,r.Scratch.SignedTriangleCount,"Two native algorithm triangles");for(int i=0;i<old.Length;i++)Equal(old[i],bytes[i],"Input is read only");
  });
  Test("Coalescing keeps first height within two and distinguishes difference three",delegate{
   var a=Triangle(10,7,15);var b=Triangle(12,8,15);var r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{a,b});Success(r,3,2);Equal((ushort)10,r.Vertices[1],"First height retained");Words(new ushort[]{0,1,2},r.Polygons,12,"Second contour shares IDs");Equal((byte)8,r.Areas[1],"Areas remain per contour");
   r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{a,Triangle(13,8,15)});Success(r,6,2);Words(new ushort[]{3,4,5},r.Polygons,12,"Difference three produces distinct IDs");
   r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{b,a});Success(r,3,2);Equal((ushort)12,r.Vertices[1],"Reversing inputs changes retained height");
  });
  Test("Coalescing follows newest hash link and does not average heights",delegate{
   var r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{Triangle(0,1,15),Triangle(4,2,15),Triangle(2,3,15)});Success(r,6,3);Words(new ushort[]{3,4,5},r.Polygons,24,"Height2 selects newest height4 over height0");Equal((ushort)4,r.Vertices[10],"Retained newest height stays4");
  });
  Test("Unclosed removal path stops after real polygons before adjacency",delegate{
   var r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{Triangle(0,1,15),Triangle(2,2,143)});Equal(NavigationPolygonOutcome.OwnUnsupported,r.Outcome,"Removal never silently skipped");Equal((uint?)null,r.NativeStatus,"No invented native failure status");Equal(0x26c4a7u,r.StopRva,"Actual next helper call");Equal(3,r.VertexCount,"Coalesced prefix retained");Equal(2,r.PolygonCount,"All contour polygons precede removal loop");Equal((byte)1,r.Scratch.RemovalFlags[0],"Flag retained on shared vertex");Equal((byte)2,r.Areas[1],"Area prefix retained");for(int p=0;p<2;p++)for(int k=0;k<6;k++)Equal((ushort)65535,r.Polygons[p*12+6+k],"Adjacency not reached");
  });
  Test("Concave contour preserves covered area and nvp six bound",delegate{
   var r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{Input(5,0,0,0,15,0,0,4,15,2,0,4,15,2,0,2,15,4,0,2,15,4,0,0,15)});
   Equal(NavigationPolygonOutcome.Success,r.Outcome,"Concave input supported by actual triangulation");Check(r.PolygonCount>=2&&r.PolygonCount<=4,"Concavity is not flattened into one convex polygon");int twiceArea=0;
   for(int p=0;p<r.PolygonCount;p++){
    int n=0;while(n<6&&r.Polygons[p*12+n]!=65535)n++;Check(n>=3&&n<=6,"Polygon size");int signed=0;
    for(int j=0;j<n;j++){int a=r.Polygons[p*12+j]*3,b=r.Polygons[p*12+(j+1)%n]*3;signed+=r.Vertices[a]*r.Vertices[b+2]-r.Vertices[b]*r.Vertices[a+2];}Check(signed<0,"Original clockwise winding");twiceArea-=signed;
   }Equal(24,twiceArea,"L contour area12 preserved");
  });
  Test("No-ear zero result retains vertex writes without fallback triangulation",delegate{
   var r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{Input(5,0,0,0,15,1,0,0,15,2,0,0,15,3,0,0,15)});
   Success(r,4,0);Equal(0,r.Scratch.SignedTriangleCount,"Negative zero means no produced triangles");Equal(0,r.Scratch.LocalPolygonCount,"No fallback triangulation");
   for(int p=0;p<r.Polygons.Length;p++)Equal((ushort)65535,r.Polygons[p],"Uninitialized polygon capacity remains FF");
  });
  Test("Unsafe 15-bit contour indexing stops before output allocation",delegate{
   var r=NavigationLayerPolygons.Build(new NavigationPolygonContourInput[]{new NavigationPolygonContourInput(null,32769,1)});Equal(NavigationPolygonOutcome.OwnUnsupported,r.Outcome,"Explicit own index domain");Equal((uint?)null,r.NativeStatus,"Not native out-of-memory");Equal(0,r.Nvp,"Count scan stop precedes native nvp write");Check(r.Vertices==null&&r.Polygons==null,"No fabricated output buffers");
  });
 }
}
public static class NavigationLayerPolygonsTestProgram {
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new ArgumentException("No arguments: memory-only polygon contract fixtures");
  NavigationLayerPolygonsTests.RunMemory();Console.WriteLine("PASS "+NavigationLayerPolygonsTests.Scenarios+" scenarios, "+NavigationLayerPolygonsTests.Assertions+" assertions; managed polygon algorithms only, no native runtime/gameplay compatibility claim");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
