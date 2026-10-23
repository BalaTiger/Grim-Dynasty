using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

public static class GroundSpatialTreeTestProgram {
 const string SavedPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int assertions,scenarios,realNodeCount,realMaxDepth,realNonRootEntries;
 static bool realResourceStop,realExternalMiss;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Scenario(Action action){action();scenarios++;}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static void Missing(Action action,uint at){
  assertions++;try{action();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Wrong OWN stop address "+e.Address.ToString("x")+", wanted "+at.ToString("x"));return;}
  throw new Exception("Expected OWN Unsupported at "+at.ToString("x"));
 }
 static Displacement3 Point(float x,float y,float z){var p=new Displacement3();p.X0.Write(x);p.Y4.Write(y);p.Z8.Write(z);return p;}
 static GroundRay24 Ray(float x,float y,float z,float dx,float dy,float dz){return new GroundRay24(Point(x,y,z),Point(dx,dy,dz));}
 static GroundGeometryOperands Geometry(){var o=new GroundGeometryOperands();o.SelectObservedDiskOperands();return o;}
 static GroundSpatialContext Context(OwnGroundData data,bool selectTreeOperands){
  var binding=new GroundTerrainIntersectionBinding(data);binding.SelectObservedDiskSlots();
  var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();
  var context=new GroundSpatialContext(binding,operands);if(selectTreeOperands)context.SelectObservedDiskOperands();return context;
 }
 static GroundSpatialTree Tree(GroundSpatialContext context,float cx,float cz,float ex,float ez){
  var tree=new GroundSpatialTree();GroundSpatialTreeContracts.Construct11A600(tree,Field<float>.Of(cx),Field<float>.Of(cz),Field<float>.Of(ex),Field<float>.Of(ez),context,new Invocation());return tree;
 }
 static GroundBlockHit20 Output(){return new GroundBlockHit20(Field<float>.Of(99),Point(71,72,73),Field<uint>.Of(987));}
 static bool Untouched(GroundBlockHit20 output){return output.Distance00.Read(0)==99&&output.Position04.X0.Read(0)==71&&output.Position04.Y4.Read(0)==72&&output.Position04.Z8.Read(0)==73&&output.Material10.Read(0)==987;}
 static GroundSpatialEntry Query(GroundSpatialNode root,GroundRay24 ray,GroundBlockHit20 output,float maximum,GroundSpatialEntry ignored,GroundSpatialContext context,Invocation invocation){
  // The actual Block body does not consume these two forwarded flag arguments.
  return GroundSpatialTreeContracts.Query1D0100(root,ray,output,0x12345678u,maximum,ignored,0xa5,context,invocation);
 }
 static OwnGroundData OwnData(int width,int depth,float[] heights){
  if(heights.Length!=width*depth)throw new Exception("OWN fixture cell extent");
  using(var s=new MemoryStream()){using(var w=new BinaryWriter(s)){
   w.Write(2u);foreach(uint value in new uint[]{1,1,(uint)width,(uint)depth,0,0,0,0})w.Write(value);
   foreach(float value in heights)w.Write(Bits(value));for(int n=0;n<heights.Length;n++)w.Write((byte)2);
   byte[] name=Encoding.ASCII.GetBytes("OWN explicit tree-query material fixture");w.Write((uint)name.Length);w.Write(name);w.Write(0u);w.Flush();
   var data=OwnGroundColdLoader.DecodeType2Chunk(s.ToArray(),"OWN tree-query fixture; not a real resource");data.SelectObservedScalarSlot();return data;
  }}
 }
 static GroundMaterialSurfaceState OwnSurface(OwnGroundData data){
  var material=new GroundMaterialSurfaceState();material.SurfaceFC.Write(0x10203040u);data.TypeRecordAt(0,0).Material.Write(material);return material;
 }
 static GroundBlockMaterialState[] States(OwnGroundData data){
  var blocks=GroundGeometryContracts.CreateOwnBlockBounds(data,Geometry(),new Invocation());var states=new GroundBlockMaterialState[blocks.Length];
  for(int n=0;n<blocks.Length;n++){states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(Geometry(),new Invocation());}return states;
 }
 static GroundSpatialEntry Insert(GroundSpatialNode node,GroundBlockMaterialState state,GroundSpatialContext context){
  var entry=context.BindOwnColdBlock(state);GroundSpatialTreeContracts.Insert1CFDF0(node,entry,context,new Invocation());return entry;
 }
 sealed class Audit {
  public readonly HashSet<GroundSpatialNode> Nodes=new HashSet<GroundSpatialNode>();
  public readonly HashSet<GroundSpatialEntry> Entries=new HashSet<GroundSpatialEntry>();
  public readonly HashSet<GroundBlockState> Blocks=new HashSet<GroundBlockState>();
  public int MaxDepth,NonRootEntries;
 }
 static void AuditNode(GroundSpatialNode node,GroundSpatialNode parent,GroundSpatialContext context,Dictionary<GroundBlockState,GroundBlockMaterialState> expected,Audit audit){
  Check(node!=null&&audit.Nodes.Add(node),"Published nodes are unique and acyclic");
  Check(Object.ReferenceEquals(node.Parent08.Read(0),parent),"Child parent identity is authoritative");
  int depth=node.Depth50.Read(0);Check(depth==(parent==null?0:parent.Depth50.Read(0)+1),"Published child depth increments from parent");
  Check(node.VirtualRva00.Read(0)==0x340e60u,"Published node selects observed table");audit.MaxDepth=Math.Max(audit.MaxDepth,depth);
  GroundSpatialEntry previous=null;
  for(var entry=node.Head48.Read(0);entry!=null;entry=entry.Next28.Read(0)){
   Check(audit.Entries.Add(entry)&&audit.Blocks.Add(entry.Block),"Every real Block occurs exactly once across all lists");
   GroundBlockMaterialState state;Check(expected.TryGetValue(entry.Block,out state),"Published entry belongs to original produced Block set");
   Check(Object.ReferenceEquals(entry.MaterialState,state)&&Object.ReferenceEquals(entry.Box10,state.Block.Box10)&&Object.ReferenceEquals(context.BindBlock(state),entry),"Entry preserves Block, Box, candidate owner and context identity");
   Check(Object.ReferenceEquals(entry.Owner08.Read(0),node)&&Object.ReferenceEquals(entry.Previous30.Read(0),previous),"Owner and previous links match actual list traversal");
   if(previous!=null)Check(Object.ReferenceEquals(previous.Next28.Read(0),entry),"Previous-next links are reciprocal");
   if(parent!=null)audit.NonRootEntries++;previous=entry;
  }
  for(int n=0;n<4;n++){var child=node.Child(n).Read(0);if(child!=null)AuditNode(child,node,context,expected,audit);}
 }
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],SavedPath,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixed saved member argument required");
  byte[] member=File.ReadAllBytes(SavedPath);Check(member.Length==1091357&&Hash(member)==SavedHash,"Fixed saved member before tree tests");
  var chunk=new byte[149073];Buffer.BlockCopy(member,273260,chunk,0,chunk.Length);
  OwnGroundData real=null;GroundSpatialTree realTree=null;GroundSpatialContext realContext=null;
  Scenario(delegate{
   var data=OwnData(2,2,new float[4]);var context=Context(data,true);var tree=Tree(context,2,3,4,8);var root=tree.Root10.Read(0);
   Check(tree.VirtualRva00.Read(0)==0x340e70u&&tree.ChangeCount08.Read(0)==0&&context.ActiveNodeCount==1,"Root constructor publishes one node and zero change count");
   Check(root.VirtualRva00.Read(0)==0x340e60u&&root.Depth50.Read(0)==0&&root.Flags54.Read(0)==0,"Root table, depth zero and flags");
   Check(root.Parent08.Read(0)==null&&root.Head48.Read(0)==null,"Cold root has no parent or entries");
   Check(root.Box30.Center00.Read(0)==2&&root.Box30.Center04.Read(0)==0&&root.Box30.Center08.Read(0)==3,"Root center write layout");
   Check(Bits(root.Box30.Extent0C.Read(0))==0x408b851fu&&Bits(root.Box30.Extent14.Read(0))==0x410b851fu&&Bits(root.Box30.Extent10.Read(0))==0,"Observed scale bit pattern multiplied by exact powers of two");
   for(int n=0;n<4;n++)Check(root.Child(n).Read(0)==null,"Cold child "+n);for(int n=0;n<6;n++)Check(tree.Scratch18To40(n).Read(0)==null,"Cold scratch "+n);
  });
  Scenario(delegate{
   var data=OwnData(2,2,new float[4]);var context=Context(data,false);var tree=new GroundSpatialTree();var invocation=new Invocation();
   Missing(delegate{GroundSpatialTreeContracts.Construct11A600(tree,Field<float>.Of(2),Field<float>.Of(3),Field<float>.Of(4),Field<float>.Of(8),context,invocation);},0x11a65a);
   Check(invocation.At==0x11a65a&&tree.VirtualRva00.Read(0)==0x340e70u&&tree.ChangeCount08.Read(0)==0,"Unknown root scale preserves only prior constructor stores");
   for(int n=0;n<6;n++)Check(tree.Scratch18To40(n).Read(0)==null,"Unknown scale still follows earlier scratch clear "+n);
   Check(context.ActiveNodeCount==0,"Unknown scale stops before root allocation");Missing(delegate{tree.Root10.Read(0);},0);
  });
  Scenario(delegate{
   real=OwnGroundColdLoader.DecodeType2Chunk(chunk,SavedHash+":273260+149073");real.SelectObservedScalarSlot();var states=States(real);
   Check(states.Length==225,"Real225 same-data bounds and candidates prepared");var expected=new Dictionary<GroundBlockState,GroundBlockMaterialState>();
   foreach(var state in states){Check(Object.ReferenceEquals(state.Block.TerrainA8.Read(0),real),"Real produced Block shares terrain");expected.Add(state.Block,state);}
   realContext=Context(real,true);realTree=new GroundSpatialTree();GroundSpatialTreeContracts.PublishOwnColdBlocks(realTree,real,states,realContext,new Invocation());
   Check(realTree.ChangeCount08.Read(0)==225,"Publication increments change count for each of225 entries");
   var audit=new Audit();AuditNode(realTree.Root10.Read(0),null,realContext,expected,audit);
   Check(audit.Entries.Count==225&&audit.Blocks.Count==225,"All225 original identities are reachable exactly once");
   Check(audit.Nodes.Count==realContext.ActiveNodeCount&&audit.Nodes.Count==realContext.SnapshotNodes().Length,"No active orphan nodes outside published tree");
   Check(audit.Nodes.Count>1&&audit.MaxDepth>0&&audit.NonRootEntries>0,"Real publication performs actual subdivision and descendant membership");
   realNodeCount=audit.Nodes.Count;realMaxDepth=audit.MaxDepth;realNonRootEntries=audit.NonRootEntries;
   for(uint n=0;n<real.TypeRecordCount;n++)Missing(delegate{real.TypeRecordAt(n,0).Material.Read(0);},0);
  });
  Scenario(delegate{
   var output=Output();var invocation=new Invocation();
   Missing(delegate{Query(realTree.Root10.Read(0),Ray(64.25f,32,64.75f,0,-1,0),output,100,null,realContext,invocation);},0x1d1528);
   Check(invocation.At==0x1d1528&&Untouched(output),"Real tree reaches actual Block material stop; local triangle hit never leaks into shared output");realResourceStop=true;
   var untouched=Output();var missed=Query(realTree.Root10.Read(0),Ray(-1000,32,-1000,0,-1,0),untouched,100,null,realContext,new Invocation());
   Check(missed==null&&Untouched(untouched),"External real miss returns no object and preserves all shared output fields");realExternalMiss=true;
  });
  Scenario(delegate{
   // One OWN grid has a height5 plateau then a height0 plateau. The real
   // triangle body finds parameter distances5 and10 along the same ray.
   var heights=new float[34];for(int z=0;z<2;z++)for(int x=0;x<17;x++)heights[z*17+x]=x<=8?5f:0f;
   var data=OwnData(17,2,heights);var material=OwnSurface(data);var states=States(data);Check(states.Length==2,"OWN two-plateau descriptors");
   var context=Context(data,true);var tree=Tree(context,8,.5f,8,.5f);var root=tree.Root10.Read(0);
   // Explicit OWN query fixture selects the proven no-refine depth branch.
   // It is not the normal published225-node topology tested above.
   root.Depth50.Write(8);var near=Insert(root,states[0],context);var far=Insert(root,states[1],context);
   Check(Object.ReferenceEquals(root.Head48.Read(0),far)&&Object.ReferenceEquals(far.Next28.Read(0),near)&&Object.ReferenceEquals(near.Previous30.Read(0),far)&&near.Next28.Read(0)==null,"Actual Begin list insertion visits far before near");
   var ray=Ray(.25f,10,.75f,1,-1,0);var output=Output();var chosen=Query(root,ray,output,100,null,context,new Invocation());
   Check(Object.ReferenceEquals(chosen,near)&&output.Distance00.Read(0)==5&&output.Position04.X0.Read(0)==5.25f&&output.Position04.Y4.Read(0)==5&&output.Position04.Z8.Read(0)==.75f,"Later nearer real triangle replaces earlier farther shared hit");
   Check(output.Material10.Read(0)==0x10203040u&&Object.ReferenceEquals(data.TypeRecordAt(0,0).Material.Read(0),material),"Only explicit OWN material supplies the surface DWORD");
   output=Output();chosen=Query(root,ray,output,100,near,context,new Invocation());Check(Object.ReferenceEquals(chosen,far)&&output.Distance00.Read(0)==10&&output.Position04.Y4.Read(0)==0,"Ignoring nearest object retains real farther hit");
   output=Output();chosen=Query(root,ray,output,100,far,context,new Invocation());Check(Object.ReferenceEquals(chosen,near)&&output.Distance00.Read(0)==5,"Ignoring first list entry reaches the next live entry");
   output=Output();chosen=Query(root,ray,output,5,null,context,new Invocation());Check(chosen==null&&Untouched(output),"Equality to broadphase maximum is rejected before body");
   output=Output();chosen=Query(root,ray,output,6,null,context,new Invocation());Check(Object.ReferenceEquals(chosen,near)&&output.Distance00.Read(0)==5,"Finite broadphase cutoff keeps nearer real hit");
   material.SurfaceFC.Forget();output=Output();chosen=Query(root,ray,output,100,null,context,new Invocation());
   Check(Object.ReferenceEquals(chosen,near)&&output.Distance00.Read(0)==5&&output.Position04.X0.Read(0)==5.25f&&output.Position04.Y4.Read(0)==5&&output.Position04.Z8.Read(0)==.75f,"Valid OWN material with unknown DWORD preserves selected identity and XYZ through actual Block and tree copy");
   Missing(delegate{output.Material10.Read(0x1d01b0);},0x1d01b0);Missing(delegate{material.SurfaceFC.Read(0x1d152d);},0x1d152d);
  });
  Scenario(delegate{
   var data=OwnData(2,2,new float[4]);OwnSurface(data);var state=States(data)[0];
   // Deliberately loose OWN bounds: the ray enters at0 but the actual flat
   // terrain hit remains10. No callback returns a fabricated distance.
   state.Block.Box10.Center04.Write(5);state.Block.Box10.Extent10.Write(5);
   var context=Context(data,true);var tree=Tree(context,.5f,.5f,.5f,.5f);var root=tree.Root10.Read(0);root.Depth50.Write(8);var entry=Insert(root,state,context);
   var output=Output();var chosen=Query(root,Ray(.25f,10,.75f,0,-1,0),output,1,null,context,new Invocation());
   Check(Object.ReferenceEquals(chosen,entry)&&output.Distance00.Read(0)==10&&output.Distance00.Read(0)>1&&output.Position04.Y4.Read(0)==0,"Actual body hit is not incorrectly subjected to a second maxDistance test");
  });
  Scenario(delegate{
   var data=OwnData(2,2,new float[4]);OwnSurface(data);var context=Context(data,true);
   var parent=Tree(context,.5f,.5f,.5f,.5f).Root10.Read(0);var first=Tree(context,.5f,.5f,.5f,.5f).Root10.Read(0);var second=Tree(context,.5f,.5f,.5f,.5f).Root10.Read(0);
   first.Depth50.Write(8);second.Depth50.Write(8);var good=Insert(first,States(data)[0],context);var broken=Insert(second,States(data)[0],context);
   // Explicit OWN overlapping child topology isolates traversal order. Normal
   // subdivision/link ownership is independently audited on the real225 set.
   first.Parent08.Write(parent);second.Parent08.Write(parent);first.Depth50.Write(1);second.Depth50.Write(1);
   parent.Child(0).Write(first);parent.Child(1).Write(second);parent.Box30.Center04.Write(5);parent.Box30.Extent10.Write(5);
   second.Box30.Center04.Write(5);second.Box30.Extent10.Write(5);broken.VirtualRva00.Forget();
   var output=Output();var invocation=new Invocation();var ray=Ray(.25f,10,.75f,0,-1,0);
   Missing(delegate{Query(parent,ray,output,100,null,context,invocation);},0x1d0147);
   Check(invocation.At==0x1d0147&&output.Distance00.Read(0)==10&&output.Material10.Read(0)==0x10203040u&&output.Position04.X0.Read(0)==.25f&&output.Position04.Y4.Read(0)==0&&output.Position04.Z8.Read(0)==.75f,"Child0 actual hit is published before child1 predicate stop");
   parent.Child(0).Write(second);parent.Child(1).Write(first);output=Output();
   Missing(delegate{Query(parent,ray,output,100,broken,context,new Invocation());},0x1d0147);
   Check(Untouched(output),"Reversed child slots stop before hit; predicate is checked even for ignored object");
   broken.VirtualRva00.Write(0x3438f8u);parent.Child(0).Write(first);parent.Child(1).Write(second);output=Output();
   var chosen=Query(parent,ray,output,100,null,context,new Invocation());Check(Object.ReferenceEquals(chosen,good)&&output.Distance00.Read(0)==10,"Later equal-distance child preserves first selected object");
  });
  Check(Hash(File.ReadAllBytes(SavedPath))==SavedHash,"Saved member unchanged after tree tests");
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"real_entries\":225,\"real_nodes\":"+realNodeCount+",\"real_max_depth\":"+realMaxDepth+",\"real_nonroot_entries\":"+realNonRootEntries+",\"real_material_unknown_stop\":"+(realResourceStop?"true":"false")+",\"real_external_miss\":"+(realExternalMiss?"true":"false")+",\"own_fixture_query_hits\":true,\"real_material_resolved\":false,\"native_load\":false,\"world_floor\":false,\"gameplay_accepted\":false,\"saved_member_sha256\":\""+SavedHash+"\"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
