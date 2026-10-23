using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Mock clamp/validation observations isolate recovered wrappers. Preselected
// polygon references and copied fixture endpoints are NOT nearest search,
// mesh loading, native navigation or movement. Production leaves are separate.
public static class NavigationCoreTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Equal<T>(T expected,T actual,string message){Check(EqualityComparer<T>.Default.Equals(expected,actual),message+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
 static void Unknown<T>(Field<T> field){try{field.Read(1);throw new Exception("Unobserved field became known");}catch(MissingContract){assertions++;}}
 static void Missing(Outcome result,uint at){Check(!result.ReturnedNormally,"Unexpected success");Equal(at,result.Address,"Failure address");}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static void Copy(NavigationPoint3 from,NavigationPoint3 to){to.X.CopyFrom(from.X);to.Y.CopyFrom(from.Y);to.Z.CopyFrom(from.Z);}
 static int Calls(Mock b,string name){int n=0;foreach(string s in b.Calls)if(s==name)n++;return n;}
 static NavigationCoreBoundaries Warm(){var b=new NavigationCoreBoundaries();b.UseObservedEqualityConstant();return b;}
 static NavigationMesh ValidMesh(){
  var m=new NavigationMesh();m.PolyBits60.Write(2);m.TileBits5C.Write(2);m.SaltBits58.Write(28);m.TileLimit30.Write(2);
  var tile=new NavigationTile();tile.Salt00.Write(0x08000000);var header=new NavigationTileHeader();header.PolyCount18.Write(2);tile.Header08.Write(header);m.Tiles50.Write(new NavigationTile[]{null,tile});return m;
 }
 sealed class F {
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationMesh Mesh=new NavigationMesh();
  public readonly NavigationPoint3 From=Point(1,2,3),To=Point(4,5,6);
  public readonly NavigationRefBuffer Refs=new NavigationRefBuffer(2);
  public readonly NavigationPointBuffer Points=new NavigationPointBuffer(4);
  public readonly NavigationFlagBuffer Flags=new NavigationFlagBuffer(4);
  public readonly Field<uint> Count=Field<uint>.Of(99);
  public readonly Mock B=new Mock();
  public F(){Query.Mesh0.Write(Mesh);Refs.Cells[0].Write(0x80000005);Refs.Cells[1].Write(0x80000006);B.UseObservedEqualityConstant();}
 }
 // Real model geometry, using default production boundaries and an explicit
 // in-memory mesh. The reference is supplied; no nearest-poly search is implied.
 sealed class G {
  public const uint Reference=0x80000005;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationMesh Mesh=ValidMesh();
  public readonly NavigationPoly Poly=new NavigationPoly();
  public readonly NavigationPoint3[] Vertices;
  public readonly NavigationCoreBoundaries B=Warm();
  public G(NavigationPoint3[] vertices){Vertices=vertices;Query.Mesh0.Write(Mesh);var tile=Mesh.Tiles50.Read(1)[1];Poly.VertexCount1E.Write((byte)vertices.Length);for(int n=0;n<vertices.Length;n++)Poly.Indices04[n].Write((ushort)n);tile.Polys10.Write(new NavigationPoly[]{null,Poly});tile.Vertices18.Write(vertices);B.UseObservedGeometryConstants();}
 }
 static NavigationPoint3[] Triangle(){return new NavigationPoint3[]{Point(0,0,0),Point(4,4,0),Point(0,8,4)};}
 static NavigationPoint3[] Square(){return new NavigationPoint3[]{Point(0,0,0),Point(4,4,0),Point(4,8,4),Point(0,12,4)};}
 static void AssertPoint(NavigationPoint3 p,float x,float y,float z,string label){Equal(x,p.X.Read(1),label+" X");Equal(y,p.Y.Read(1),label+" Y");Equal(z,p.Z.Read(1),label+" Z");}
 static Field<float>[] Scalars(int count){var a=new Field<float>[count];for(int n=0;n<count;n++)a[n]=new Field<float>();return a;}
 sealed class Mock:NavigationCoreBoundaries {
  public readonly List<string> Calls=new List<string>();
  public Action<string,Mock> On=null;
  public object Arg0,Arg1,Arg2;
  public uint Ref;
  public byte FirstValid=1,SecondValid=1,EqualityValue=0;
  public uint FirstClamp=NavigationCoreContracts.Success,SecondClamp=NavigationCoreContracts.Success,AppendResult=NavigationCoreContracts.Continue;
  public bool ForceEquality=false,ForceAppend=false;
  void Hit(string name){Calls.Add(name);if(On!=null)On(name,this);}
  public override byte ValidateRef2688C0(NavigationMesh mesh,uint reference){Arg0=mesh;Ref=reference;Hit("validate");return CallsOf("validate")==1?FirstValid:SecondValid;}
  public override uint Clamp260AD0(NavigationQuery query,uint reference,NavigationPoint3 point,NavigationPoint3 output){Arg0=query;Arg1=point;Arg2=output;Ref=reference;Hit("clamp");Copy(point,output);return CallsOf("clamp")==1?FirstClamp:SecondClamp;}
  public override byte Equal25F770(NavigationPoint3 a,NavigationPoint3 b){Arg0=a;Arg1=b;Hit("equal");return ForceEquality?EqualityValue:base.Equal25F770(a,b);}
  public override uint Append2623B0(NavigationPoint3 point,byte flag,NavigationPointBuffer points,NavigationFlagBuffer flags,Field<uint> count,int capacity){Arg0=point;Arg1=points;Arg2=count;Hit("append");return ForceAppend?AppendResult:base.Append2623B0(point,flag,points,flags,count,capacity);}
  int CallsOf(string name){return NavigationCoreTests.Calls(this,name);}
 }
 static uint Straight(F f,int pathCount,int capacity){return NavigationCoreContracts.StraightPath(f.Query,f.From,f.To,f.Refs,pathCount,f.Points,f.Flags,f.Count,capacity,f.B,new Invocation());}
 static Outcome AttemptStraight(F f,int pathCount,int capacity){return MoveContracts.Run(delegate(Invocation i){NavigationCoreContracts.StraightPath(f.Query,f.From,f.To,f.Refs,pathCount,f.Points,f.Flags,f.Count,capacity,f.B,i);});}
 static uint Find(F f,uint start,uint end){return NavigationCoreContracts.FindPath(f.Query,start,end,f.From,f.To,null,f.Refs,f.Count,2,f.B,new Invocation());}
 public static void Run(){
  Test("corridor zero refs clear count before returning without validation",delegate{
   foreach(uint start in new uint[]{0,7}){var f=new F();uint end=start==0?7u:0u;Equal(NavigationCoreContracts.Failure,Find(f,start,end),"Zero reference status");Equal(0u,f.Count.Read(1),"Count cleared");Equal(0x80000005u,f.Refs.Cells[0].Read(1),"Output remains untouched");Equal(0,Calls(f.B,"validate"),"No validation");}
  });
  Test("same integer ref validates twice against captured mesh before output",delegate{
   var f=new F();var replacement=new NavigationMesh();f.B.On=delegate(string n,Mock b){if(n=="validate"){Same(f.Mesh,b.Arg0,"Both calls use captured original mesh");Equal(0x80000005u,b.Ref,"Reference remains uint, not float payload");Equal(0u,f.Count.Read(1),"Count already cleared");if(Calls(b,n)==1)f.Query.Mesh0.Write(replacement);}};
   Equal(NavigationCoreContracts.Success,Find(f,0x80000005,0x80000005),"Same-poly success");Equal(2,Calls(f.B,"validate"),"Same ref still twice validated");Equal(0x80000005u,f.Refs.Cells[0].Read(1),"Exact integer ref copied");Equal(1u,f.Count.Read(1),"One result");
  });
  Test("either validation rejection preserves cleared count and untouched output",delegate{
   for(int rejected=1;rejected<=2;rejected++){var f=new F();if(rejected==1)f.B.FirstValid=0;else f.B.SecondValid=0;Equal(NavigationCoreContracts.Failure,Find(f,0x3f800000,0x3f800000),"Rejection status");Equal(rejected,Calls(f.B,"validate"),"Short circuit order");Equal(0u,f.Count.Read(1),"Count remains zero");Equal(0x80000005u,f.Refs.Cells[0].Read(1),"No premature ref write");}
  });
  Test("same-poly output-count alias records ref before final count one",delegate{
   var f=new F();var alias=new NavigationRefBuffer(new Field<uint>[]{f.Count});Equal(NavigationCoreContracts.Success,NavigationCoreContracts.FindPath(f.Query,0x3f800000,0x3f800000,null,null,null,alias,f.Count,1,f.B,new Invocation()),"No coordinate or filter dependency");Equal(1u,alias.Cells[0].Read(1),"Final count overwrites same-cell output ref");Equal(2,Calls(f.B,"validate"),"Validation prefix retained");
  });
  Test("unknown mesh and released corridor preserve distinct failure prefixes",delegate{
   var f=new F();f.Query.Mesh0.Forget();Missing(MoveContracts.Run(delegate(Invocation i){NavigationCoreContracts.FindPath(f.Query,1,1,null,null,null,f.Refs,f.Count,2,f.B,i);}),0x261cf8);Equal(0u,f.Count.Read(1),"Unknown mesh follows count clear");Equal(0,Calls(f.B,"validate"),"No made-up mesh");
   var g=new F();g.Refs.MarkReleased();Missing(MoveContracts.Run(delegate(Invocation i){NavigationCoreContracts.FindPath(g.Query,1,1,null,null,null,g.Refs,g.Count,2,g.B,i);}),0x261d29);Equal(2,Calls(g.B,"validate"),"Both validation calls precede released output stop");Equal(0u,g.Count.Read(1),"No count increment");
  });
  Test("distinct valid refs stop at unsupported multi-polygon search",delegate{
   var f=new F();Missing(MoveContracts.Run(delegate(Invocation i){NavigationCoreContracts.FindPath(f.Query,1,2,f.From,f.To,null,f.Refs,f.Count,2,f.B,i);}),0x261d51);Equal(2,Calls(f.B,"validate"),"Validated direct prefix");Equal(0u,f.Count.Read(1),"No synthetic corridor");Equal(0x80000005u,f.Refs.Cells[0].Read(1),"Output remains original");
  });
  Test("production validator decodes full uint salt tile and polygon fields",delegate{
   var b=new NavigationCoreBoundaries();var m=ValidMesh();Equal((byte)1,b.ValidateRef2688C0(m,0x80000005),"High-bit reference accepted as unsigned identity");Equal((byte)0,b.ValidateRef2688C0(null,0),"Zero ref requires no mesh");Equal((byte)0,b.ValidateRef2688C0(m,0x80000006),"Poly index equal to count is outside");
  });
  Test("production validator failure gates do not read later unknown fields",delegate{
   var b=new NavigationCoreBoundaries();var outOfTile=new NavigationMesh();outOfTile.PolyBits60.Write(2);outOfTile.TileBits5C.Write(2);outOfTile.TileLimit30.Write(1);Equal((byte)0,b.ValidateRef2688C0(outOfTile,0x80000005),"Tile limit rejection before salt or table");Unknown(outOfTile.SaltBits58);Unknown(outOfTile.Tiles50);
   var mismatch=ValidMesh();var tile=mismatch.Tiles50.Read(1)[1];tile.Salt00.Write(0);tile.Header08.Forget();Equal((byte)0,b.ValidateRef2688C0(mismatch,0x80000005),"Salt mismatch before header");Unknown(tile.Header08);tile.Salt00.Write(0x08000000);tile.Header08.Write(null);Equal((byte)0,b.ValidateRef2688C0(mismatch,0x80000005),"Known-null header rejects");
  });
  Test("production validator unknown and released states never mean invalid ref",delegate{
   var b=new NavigationCoreBoundaries();var m=new NavigationMesh();Missing(MoveContracts.Run(delegate(Invocation i){b.ValidateRef2688C0(m,1);}),0x2688d5);
   var g=ValidMesh();g.Tiles50.Read(1)[1].Header08.Read(1).PolyCount18.Forget();Missing(MoveContracts.Run(delegate(Invocation i){b.ValidateRef2688C0(g,0x80000005);}),0x268951);
   var h=ValidMesh();h.Tiles50.Read(1)[1].MarkReleased();Missing(MoveContracts.Run(delegate(Invocation i){b.ValidateRef2688C0(h,0x80000005);}),0x26892f);
  });
  Test("production equality requires explicit warm state and strict squared threshold",delegate{
   var cold=new NavigationCoreBoundaries();var a=Point(0,0,0);var b=Point(0,0,0);Missing(MoveContracts.Run(delegate(Invocation i){cold.Equal25F770(a,b);}),0);var warm=Warm();Equal((byte)1,warm.Equal25F770(a,b),"Identical finite values");b.X.Write(NavigationCoreContracts.FromBits(0x387fffff));Equal((byte)1,warm.Equal25F770(a,b),"One float below distance threshold");b.X.Write(NavigationCoreContracts.FromBits(0x38800000));Equal((byte)0,warm.Equal25F770(a,b),"Exact threshold excluded by SETA");b.X.Write(NavigationCoreContracts.FromBits(0x38800001));Equal((byte)0,warm.Equal25F770(a,b),"Above threshold excluded");b.X.Write(Single.NaN);Equal((byte)0,warm.Equal25F770(a,b),"Unordered comparison false");
  });
  Test("production equality preserves operand-read and epsilon-read order",delegate{
   var warm=Warm();var a=new NavigationPoint3();var b=new NavigationPoint3();Missing(MoveContracts.Run(delegate(Invocation i){warm.Equal25F770(a,b);}),0x25f7a5);b.Z.Write(0);Missing(MoveContracts.Run(delegate(Invocation i){warm.Equal25F770(a,b);}),0x25f7aa);b.X.Write(0);b.Y.Write(0);Missing(MoveContracts.Run(delegate(Invocation i){warm.Equal25F770(a,b);}),0x25f7b3);Copy(Point(0,0,0),a);warm.Epsilon422BA4.Forget();Missing(MoveContracts.Run(delegate(Invocation i){warm.Equal25F770(a,b);}),0x25f7da);
  });
  Test("production append copies unknown payload without inventing read failures",delegate{
   var b=new NavigationCoreBoundaries();var p=Point(1,2,3);p.Z.Forget();var output=new NavigationPointBuffer(2);var flags=new NavigationFlagBuffer(2);var count=Field<uint>.Of(0);Equal(NavigationCoreContracts.Continue,b.Append2623B0(p,1,output,flags,count,2),"First append needs no equality warm state");Equal(1.0f,output.Cells[0].Read(1),"X copied");Equal(2.0f,output.Cells[1].Read(1),"Y copied");Unknown(output.Cells[2]);Equal((byte)1,flags.Cells[0].Read(1),"Flag still written");Equal(1u,count.Read(1),"Unknown payload still counted");
  });
  Test("duplicate append only updates flag and returns continue even for terminal flag",delegate{
   var b=Warm();var output=new NavigationPointBuffer(2);var flags=new NavigationFlagBuffer(2);var count=Field<uint>.Of(0);b.Append2623B0(Point(1,2,3),1,output,flags,count,2);Equal(NavigationCoreContracts.Continue,b.Append2623B0(Point(1,2,3),2,output,flags,count,1),"Duplicate branch does not apply capacity or terminal tests");Equal(1u,count.Read(1),"No duplicate point");Equal((byte)2,flags.Cells[0].Read(1),"Existing flag replaced");Unknown(output.Cells[3]);Equal(NavigationCoreContracts.Continue,b.Append2623B0(Point(1,2,3),2,output,null,count,1),"Null flags also continue");
  });
  Test("append terminal and full statuses use count after coordinate stores",delegate{
   var b=Warm();var output=new NavigationPointBuffer(2);var count=Field<uint>.Of(0);Equal(NavigationCoreContracts.Success|0x10u,b.Append2623B0(Point(1,2,3),1,output,null,count,1),"Exactly full adds buffer bit");Equal(1u,count.Read(1),"One point stored before capacity status");count.Write(0);Equal(NavigationCoreContracts.Success,b.Append2623B0(Point(4,5,6),2,output,null,count,2),"Terminal flag completes below capacity");Equal(6.0f,output.Cells[2].Read(1),"Z stored");
  });
  Test("append released flags retain coordinate writes before count increment",delegate{
   var b=Warm();var output=new NavigationPointBuffer(2);var flags=new NavigationFlagBuffer(2);flags.MarkReleased();var count=Field<uint>.Of(0);Missing(MoveContracts.Run(delegate(Invocation i){b.Append2623B0(Point(1,2,3),1,output,flags,count,2);}),0x262438);Equal(1.0f,output.Cells[0].Read(1),"X prefix retained");Equal(2.0f,output.Cells[1].Read(1),"Y prefix retained");Equal(3.0f,output.Cells[2].Read(1),"Z prefix retained");Equal(0u,count.Read(1),"Count has not advanced");Unknown(flags.Cells[0]);
  });
  Test("append overlapping same-type cells follows sequential XYZ loads and stores",delegate{
   var b=Warm();var backing=new NavigationPointBuffer(3);for(int n=0;n<4;n++)backing.Cells[n].Write(n+1);var point=new NavigationPoint3(backing.Cells[0],backing.Cells[1],backing.Cells[2]);var output=new NavigationPointBuffer(new Field<float>[]{backing.Cells[1],backing.Cells[2],backing.Cells[3],backing.Cells[4],backing.Cells[5],backing.Cells[6]});var count=Field<uint>.Of(0);
   Equal(NavigationCoreContracts.Continue,b.Append2623B0(point,1,output,null,count,2),"Contiguous destination shifted by one float cell");Equal(1.0f,output.Cells[0].Read(1),"X load then store");Equal(1.0f,output.Cells[1].Read(1),"Y load observes earlier aliased X write");Equal(1.0f,output.Cells[2].Read(1),"Z load observes earlier aliased Y write");
  });
  Test("straight count clear affects aliased first ref before any clamp",delegate{
   var f=new F();f.Refs.Cells[0]=f.Count;Equal(NavigationCoreContracts.Failure,Straight(f,1,4),"Cleared aliased first ref is zero");Equal(0u,f.Count.Read(1),"Count reset retained");Equal(0,Calls(f.B,"clamp"),"No clamp after zero first ref");Equal(0,Calls(f.B,"append"),"No synthetic start point");
  });
  Test("straight clamps twice with late last-ref read and independent hidden points",delegate{
   var f=new F();NavigationPoint3 firstHidden=null,secondHidden=null;f.B.On=delegate(string n,Mock b){if(n=="clamp"){Same(f.Query,b.Arg0,"Query identity");if(Calls(b,n)==1){Equal(0x80000005u,b.Ref,"Initial integer ref");Same(f.From,b.Arg1,"First point identity");firstHidden=(NavigationPoint3)b.Arg2;f.Refs.Cells[0].Write(0x3f800000);}else{Equal(0x3f800000u,b.Ref,"Last ref read after prior callback");Same(f.To,b.Arg1,"Second point identity");secondHidden=(NavigationPoint3)b.Arg2;}}};
   Equal(NavigationCoreContracts.Success,Straight(f,1,4),"Observed wrapper completion");Equal(2,Calls(f.B,"clamp"),"Two independent calls even pathCount1");Check(!Object.ReferenceEquals(firstHidden,secondHidden),"Separate hidden outputs");Equal(2u,f.Count.Read(1),"Two fixture points");Equal((byte)1,f.Flags.Cells[0].Read(1),"Start flag");Equal((byte)2,f.Flags.Cells[1].Read(1),"End flag");
  });
  Test("either negative clamp status stops before append with count zero",delegate{
   for(int failed=1;failed<=2;failed++){var f=new F();if(failed==1)f.B.FirstClamp=0x80000020;else f.B.SecondClamp=0xffffffff;Equal(NavigationCoreContracts.Failure,Straight(f,1,4),"Negative helper status normalized");Equal(failed,Calls(f.B,"clamp"),"Short circuit");Equal(0,Calls(f.B,"append"),"No append after failed clamp");Equal(0u,f.Count.Read(1),"Count clear retained");Unknown(f.Points.Cells[0]);}
  });
  Test("straight propagates any first-append result other than exact continue",delegate{
   foreach(uint status in new uint[]{NavigationCoreContracts.Success,NavigationCoreContracts.Success|0x10u,0x80000020u,NavigationCoreContracts.Continue|1u}){var f=new F();f.B.ForceAppend=true;f.B.AppendResult=status;f.B.On=delegate(string n,Mock b){if(n=="append")f.Count.Write(3);};Equal(status,Straight(f,1,4),"Original append status propagated");Equal(3u,f.Count.Read(1),"Callback prefix not reset");Equal(0,Calls(f.B,"equal"),"No final compare after non-exact continue");}
  });
  Test("straight multi-polygon stop retains completed initial append only",delegate{
   var f=new F();Missing(AttemptStraight(f,2,4),0x2625c2);Equal(2,Calls(f.B,"clamp"),"Both endpoints obtained");Equal(1,Calls(f.B,"append"),"Start append completed");Equal(1u,f.Count.Read(1),"Start prefix remains");Equal(1.0f,f.Points.Cells[0].Read(1),"Start X copied");Equal((byte)1,f.Flags.Cells[0].Read(1),"Start flag retained");Unknown(f.Points.Cells[3]);Equal(0,Calls(f.B,"equal"),"No guessed funnel or final compare");
  });
  Test("single-poly duplicate endpoint updates end flag without duplicate point",delegate{
   var f=new F();Copy(f.From,f.To);Equal(NavigationCoreContracts.Success,Straight(f,1,4),"Single endpoint completion");Equal(1u,f.Count.Read(1),"Duplicate endpoint collapsed");Equal((byte)2,f.Flags.Cells[0].Read(1),"End flag replaces start");Unknown(f.Points.Cells[3]);Equal(1,Calls(f.B,"equal"),"One real equality check");
  });
  Test("straight opaque unknown endpoint Z copies through with Y flag and count",delegate{
   var f=new F();f.To.Z.Forget();f.B.ForceEquality=true;f.B.EqualityValue=0;Equal(NavigationCoreContracts.Success,Straight(f,1,4),"Explicit mock comparison continues wrapper");Equal(4.0f,f.Points.Cells[3].Read(1),"End X copied");Equal(5.0f,f.Points.Cells[4].Read(1),"End Y copied after unknown Z");Unknown(f.Points.Cells[5]);Equal((byte)2,f.Flags.Cells[1].Read(1),"End flag still written");Equal(2u,f.Count.Read(1),"Unknown payload not unreadable memory");
  });
  Test("straight released flag at final write retains XYZ and prior count",delegate{
   var f=new F();f.B.ForceEquality=true;f.B.EqualityValue=0;f.B.On=delegate(string n,Mock b){if(n=="equal")f.Flags.MarkReleased();};Missing(AttemptStraight(f,1,4),0x262c2d);Equal(4.0f,f.Points.Cells[3].Read(1),"Final X copied");Equal(5.0f,f.Points.Cells[4].Read(1),"Final Y copied");Equal(6.0f,f.Points.Cells[5].Read(1),"Final Z copied");Equal(1u,f.Count.Read(1),"Final count increment not reached");Equal((byte)1,f.Flags.Cells[0].Read(1),"Initial append flag remains");Unknown(f.Flags.Cells[1]);
  });
  Test("production triangle and square interior clamp preserve input height",delegate{
   var triangle=new G(Triangle());var output=new NavigationPoint3();Equal(NavigationCoreContracts.Success,triangle.B.Clamp260AD0(triangle.Query,G.Reference,Point(1,99,1),output),"Triangle interior status");AssertPoint(output,1,99,1,"Triangle interior is not height sampling");
   var square=new G(Square());var same=Point(2,-37,2);Equal(NavigationCoreContracts.Success,square.B.Clamp260AD0(square.Query,G.Reference,same,same),"Same input-output view allowed");AssertPoint(same,2,-37,2,"Square interior same-address result");
  });
  Test("production outside clamp projects to nearest edge and interpolates edge height",delegate{
   var triangle=new G(Triangle());var output=new NavigationPoint3();Equal(NavigationCoreContracts.Success,triangle.B.Clamp260AD0(triangle.Query,G.Reference,Point(3,99,3),output),"Triangle exterior status");AssertPoint(output,2,6,2,"Triangle diagonal edge midpoint");
   var square=new G(Square());Equal(NavigationCoreContracts.Success,square.B.Clamp260AD0(square.Query,G.Reference,Point(5,99,2),output),"Square exterior status");AssertPoint(output,4,6,2,"Square right-edge midpoint");
  });
  Test("production boundary classification and repeated edge keep observed geometry",delegate{
   var square=new G(Square());var output=new NavigationPoint3();Equal(NavigationCoreContracts.Success,square.B.Clamp260AD0(square.Query,G.Reference,Point(4,99,2),output),"Right boundary status");AssertPoint(output,4,6,2,"Strict crossing makes right edge use edge projection");Equal(NavigationCoreContracts.Success,square.B.Clamp260AD0(square.Query,G.Reference,Point(0,99,2),output),"Left boundary status");AssertPoint(output,0,99,2,"Opposite boundary is inside under observed crossing rule");Equal(NavigationCoreContracts.Success,square.B.Clamp260AD0(square.Query,G.Reference,Point(4,99,4),output),"Vertex status");AssertPoint(output,4,8,4,"Vertex projected to its own height");
   var duplicate=new G(new NavigationPoint3[]{Point(0,0,0),Point(4,4,0),Point(4,4,0),Point(4,8,4),Point(0,12,4)});Equal(NavigationCoreContracts.Success,duplicate.B.Clamp260AD0(duplicate.Query,G.Reference,Point(5,99,2),output),"Repeated adjacent vertex status");AssertPoint(output,4,6,2,"Zero-length edge does not replace nearer valid edge");
  });
  Test("production interior copies unknown Y without guessing polygon height",delegate{
   var g=new G(Triangle());foreach(var v in g.Vertices)v.Y.Forget();var output=new NavigationPoint3();var point=Point(1,99,1);Equal(NavigationCoreContracts.Success,g.B.Clamp260AD0(g.Query,G.Reference,point,output),"Unknown vertex heights do not affect XZ containment");AssertPoint(output,1,99,1,"Input Y retained");point.Y.Forget();Equal(NavigationCoreContracts.Success,g.B.Clamp260AD0(g.Query,G.Reference,point,output),"Unknown source Y is copied opaquely");Equal(1.0f,output.X.Read(1),"X copied");Unknown(output.Y);Equal(1.0f,output.Z.Read(1),"Z copied");
  });
  Test("production clamp distinguishes invalid refs from unknown required fields",delegate{
   var g=new G(Triangle());var output=Point(71,72,73);Equal(NavigationCoreContracts.Failure,g.B.Clamp260AD0(g.Query,0,Point(1,9,1),output),"Zero ref returns native failure");AssertPoint(output,71,72,73,"Invalid ref leaves output intact");Equal(NavigationCoreContracts.Failure,g.B.Clamp260AD0(g.Query,0x70000005,Point(1,9,1),output),"Salt mismatch returns native failure");AssertPoint(output,71,72,73,"Salt failure leaves output intact");
   g.Poly.VertexCount1E.Forget();Missing(MoveContracts.Run(delegate(Invocation i){g.B.Clamp260AD0(g.Query,G.Reference,Point(1,9,1),output);}),0x260ba0);AssertPoint(output,71,72,73,"Unknown vertex count is not a fabricated valid polygon");
   var h=new G(Triangle());var unknown=Point(1,9,1);unknown.Z.Forget();Missing(MoveContracts.Run(delegate(Invocation i){h.B.Clamp260AD0(h.Query,G.Reference,unknown,output);}),0x26d7b0);AssertPoint(output,71,72,73,"Unknown geometry input stops before output");
   h.Query.Mesh0.Forget();Equal(NavigationCoreContracts.Failure,h.B.Clamp260AD0(h.Query,0,Point(1,9,1),output),"Zero ref does not consume captured unknown mesh pointer");AssertPoint(output,71,72,73,"Zero ref with unknown mesh leaves output unchanged");Unknown(h.Query.Mesh0);Missing(MoveContracts.Run(delegate(Invocation i){h.B.Clamp260AD0(h.Query,G.Reference,Point(1,9,1),output);}),0x260b10);
  });
  Test("production geometry constants remain explicit inputs",delegate{
   var g=new G(Triangle());var output=Point(71,72,73);g.B.One34BB44.Forget();Missing(MoveContracts.Run(delegate(Invocation i){g.B.Clamp260AD0(g.Query,G.Reference,Point(1,9,1),output);}),0x26d78d);AssertPoint(output,71,72,73,"Unknown one constant has no fallback");g.B.UseObservedGeometryConstants();g.B.MaxFinite34BDCC.Forget();Missing(MoveContracts.Run(delegate(Invocation i){g.B.Clamp260AD0(g.Query,G.Reference,Point(3,99,3),output);}),0x260d07);AssertPoint(output,71,72,73,"Unknown nearest-edge initial bound has no fallback");
  });
  Test("production polygon helper nonpositive count has no hidden input reads",delegate{
   Equal((byte)0,NavigationCoreContracts.PolygonGeometry(null,null,0,null,null,null),"Zero count exits before point vertices or constants");Equal((byte)0,NavigationCoreContracts.PolygonGeometry(null,null,-1,null,null,null),"Signed negative count also bypasses loop");
  });
  Test("production polygon helper uses previous-edge indices and clamps unordered parameter",delegate{
   var b=Warm();b.UseObservedGeometryConstants();var distances=Scalars(3);var parameters=Scalars(3);Equal((byte)0,NavigationCoreContracts.PolygonGeometry(Point(-1,99,-1),Triangle(),3,distances,parameters,b),"Exterior point classification");Equal(0.0f,parameters[0].Read(1),"Negative first-edge parameter becomes zero");Equal(0.5f,parameters[1].Read(1),"Diagonal edge midpoint parameter");Equal(1.0f,parameters[2].Read(1),"Initially processed closing edge stored at index2");Equal(2.0f,distances[0].Read(1),"First edge squared distance");Equal(18.0f,distances[1].Read(1),"Diagonal edge squared distance");Equal(2.0f,distances[2].Read(1),"Closing edge squared distance");
   Equal((byte)0,NavigationCoreContracts.PolygonGeometry(Point(Single.NaN,99,1),Triangle(),3,distances,parameters,b),"Unordered crossings do not toggle inside");for(int n=0;n<3;n++){Equal(0u,NavigationCoreContracts.ToBits(parameters[n].Read(1)),"Unordered t forced to positive zero "+n);Check(Single.IsNaN(distances[n].Read(1)),"Distance remains NaN rather than sanitized "+n);}
  });
  Test("production same-poly corridor and straight path use real clamp and append",delegate{
   var g=new G(Square());var from=Point(5,99,2);var to=Point(4,-99,4);var corridor=new NavigationRefBuffer(2);var pathCount=Field<uint>.Of(99);Equal(NavigationCoreContracts.Success,NavigationCoreContracts.FindPath(g.Query,G.Reference,G.Reference,from,to,null,corridor,pathCount,2,g.B,new Invocation()),"Production validator yields one-poly corridor");Equal(1u,pathCount.Read(1),"One actual reference");Equal(G.Reference,corridor.Cells[0].Read(1),"Reference retained");
   var points=new NavigationPointBuffer(4);var flags=new NavigationFlagBuffer(4);var count=Field<uint>.Of(99);Equal(NavigationCoreContracts.Success,NavigationCoreContracts.StraightPath(g.Query,from,to,corridor,1,points,flags,count,4,g.B,new Invocation()),"Production single-poly straight path");Equal(2u,count.Read(1),"Two clamped endpoints");AssertPoint(points.PointAtScalar(0,1),4,6,2,"Clamped start");AssertPoint(points.PointAtScalar(3,1),4,8,4,"Clamped end");Equal((byte)1,flags.Cells[0].Read(1),"Start flag");Equal((byte)2,flags.Cells[1].Read(1),"End flag");
  });
  Test("production clamped endpoints coalesce and mark only endpoint flag",delegate{
   var g=new G(Square());var refs=new NavigationRefBuffer(1);refs.Cells[0].Write(G.Reference);var points=new NavigationPointBuffer(4);var flags=new NavigationFlagBuffer(4);var count=Field<uint>.Of(99);Equal(NavigationCoreContracts.Success,NavigationCoreContracts.StraightPath(g.Query,Point(5,99,2),Point(8,-99,2),refs,1,points,flags,count,4,g.B,new Invocation()),"Different outside points project to same endpoint");Equal(1u,count.Read(1),"Real geometry and equality remove duplicate");AssertPoint(points.PointAtScalar(0,1),4,6,2,"Shared clamped endpoint");Equal((byte)2,flags.Cells[0].Read(1),"Endpoint flag replaces start flag");Unknown(points.Cells[3]);
  });
 }
}
public static class NavigationCoreTestProgram {
 public static int Main(){try{NavigationCoreTests.Run();Console.WriteLine("PASS "+NavigationCoreTests.Scenarios+" scenarios, "+NavigationCoreTests.Assertions+" assertions; production validator/geometry/append/equality plus explicit Mock wrapper observations; no complete navigation validation");return 0;}catch(Exception x){Console.Error.WriteLine("FAIL after "+NavigationCoreTests.Scenarios+" scenarios / "+NavigationCoreTests.Assertions+" assertions: "+x);return 1;}}
}
