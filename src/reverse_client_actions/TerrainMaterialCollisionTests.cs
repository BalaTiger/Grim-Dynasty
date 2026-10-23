using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// New tests only. Run after the root has separately reviewed and executed the
// fixed 23-byte reader. No original file or analysis backend is accessed here.
public static class TerrainMaterialCollisionTestProgram {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string SavedRelative=@"docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string ReportRelative=@"docs\reverse-client\r1-terrain-surface-strings\inspection.json";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static readonly string[] RecordHashes={
  "99174ba1196eae2c109ad46c18fdcbf2e368923d49117739d0496829d3389d93",
  "d3d75935c98d5909f95ce0ca5c330e110dd42f5a4251ec881706c95c00edd4bf",
  "267ec1e9b06cd9c54812eeaee3e0a8a7b7ff7cc9223f42823de48c2a43213895",
  "6512c54802bcd3b6ad1bb83fe86b8b35a5f0e98397245a8347501cece66c7b4b",
  "e829033a0fb91dfb904973a53809b1eda34f41c5369a9b22726637dc4cde98ee"
 };
 static int assertions,scenarios;
 static uint realHitFc,dirtFc;
 static int realNodeCount;
 static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
 static void Scenario(Action action){action();scenarios++;}
 static void Unsupported(Action action){
  assertions++;try{action();}catch(MissingContract e){if(e.Address!=0)throw new Exception("Expected OWN Unsupported address 0, got "+e.Address.ToString("x"));return;}
  throw new Exception("Expected OWN Unsupported");
 }
 static void Unknown<T>(Field<T> field){Unsupported(delegate{field.Read(0);});}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static string RecordPath(int index){return Path.Combine(Root,@"docs\reverse-client\r1-terrain-records\terrain-"+index+".json");}
 static OwnGroundData Data(byte[] chunk){var data=OwnGroundColdLoader.DecodeType2Chunk(chunk,SavedHash+":273260+149073");data.SelectObservedScalarSlot();return data;}
 static byte[][] Copy(byte[][] input){var result=new byte[input.Length][];for(int n=0;n<input.Length;n++)result[n]=(byte[])input[n].Clone();return result;}
 static byte[] Replace(byte[] source,string before,string after){
  var text=Encoding.UTF8.GetString(source);Check(text.IndexOf(before,StringComparison.Ordinal)>=0,"Mutation target exists in saved JSON");return Encoding.UTF8.GetBytes(text.Replace(before,after));
 }
 static byte[] Hex(string text){
  Check(text!=null&&text.Length%2==0,"Receipt hex has complete bytes");var result=new byte[text.Length/2];
  for(int n=0;n<result.Length;n++)result[n]=Convert.ToByte(text.Substring(n*2,2),16);return result;
 }
 static Displacement3 Point(float x,float y,float z){var p=new Displacement3();p.X0.Write(x);p.Y4.Write(y);p.Z8.Write(z);return p;}
 static GroundRay24 Ray(float x,float y,float z){return new GroundRay24(Point(x,y,z),Point(0,-1,0));}
 static GroundBlockHit20 Output(){return new GroundBlockHit20(Field<float>.Of(99),Point(71,72,73),Field<uint>.Of(987));}
 static bool Untouched(GroundBlockHit20 output){return output.Distance00.Read(0)==99&&output.Position04.X0.Read(0)==71&&output.Position04.Y4.Read(0)==72&&output.Position04.Z8.Read(0)==73&&output.Material10.Read(0)==987;}
 static GroundGeometryOperands Geometry(){var o=new GroundGeometryOperands();o.SelectObservedDiskOperands();return o;}
 static GroundSpatialContext Context(OwnGroundData data){
  var binding=new GroundTerrainIntersectionBinding(data);binding.SelectObservedDiskSlots();
  var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();
  var context=new GroundSpatialContext(binding,operands);context.SelectObservedDiskOperands();return context;
 }
 static GroundBlockMaterialState[] States(OwnGroundData data){
  var blocks=GroundGeometryContracts.CreateOwnBlockBounds(data,Geometry(),new Invocation());var result=new GroundBlockMaterialState[blocks.Length];
  for(int n=0;n<result.Length;n++){result[n]=new GroundBlockMaterialState(blocks[n]);result[n].Populate(Geometry(),new Invocation());}return result;
 }
 static GroundSpatialEntry Query(GroundSpatialTree tree,GroundRay24 ray,GroundBlockHit20 hit,GroundSpatialContext context){
  return GroundSpatialTreeContracts.Query1D0100(tree.Root10.Read(0),ray,hit,0x12345678u,100,null,0xa5,context,new Invocation());
 }
 static void EqualHit(GroundBlockHit20 a,GroundBlockHit20 b){
  Check(Bits(a.Distance00.Read(0))==Bits(b.Distance00.Read(0)),"Tree and same selected Block copy identical distance bits");
  Check(Bits(a.Position04.X0.Read(0))==Bits(b.Position04.X0.Read(0))&&Bits(a.Position04.Y4.Read(0))==Bits(b.Position04.Y4.Read(0))&&Bits(a.Position04.Z8.Read(0))==Bits(b.Position04.Z8.Read(0)),"Tree and same selected Block copy identical XYZ bits");
  Check(a.Material10.Read(0)==b.Material10.Read(0),"Tree and same selected Block copy identical material DWORD");
 }
 public static int Main(string[] args){try{
  if(args.Length!=3||!String.Equals(args[0],Root,StringComparison.Ordinal))throw new Exception("Fixed workspace plus reviewed report and reader SHA256 arguments required");
  string reviewedReportHash=args[1],reviewedReaderHash=args[2];
  byte[] member=File.ReadAllBytes(Path.Combine(Root,SavedRelative));Check(member.Length==1091357&&Hash(member)==SavedHash,"Exact saved mainmenu member before tests");
  var chunk=new byte[149073];Buffer.BlockCopy(member,273260,chunk,0,chunk.Length);
  var records=new byte[5][];for(int n=0;n<5;n++){records[n]=File.ReadAllBytes(RecordPath(n));Check(Hash(records[n])==RecordHashes[n],"Saved decoded record pin "+n);}
  byte[] report=File.ReadAllBytes(Path.Combine(Root,ReportRelative));Check(Hash(report)==reviewedReportHash,"Reviewed actual 23-byte receipt fingerprint");
  TerrainSurfaceOperandEvidence evidence=null;TerrainMaterialCollisionAdapter realAdapter=null;
  OwnGroundData real=null;GroundSpatialTree realTree=null;GroundSpatialContext realContext=null;GroundSpatialEntry realSelected=null;

  Scenario(delegate{
   var data=Data(chunk);var adapter=TerrainMaterialCollisionAdapter.FromSavedRecords(data,records,null);
   Check(adapter.RequireSurfaceFC(0)==5&&adapter.RequireSurfaceFC(1)==1,"Pseudo evidence alone resolves exact Stone and Grass");
   for(uint n=2;n<5;n++){uint index=n;Unknown(adapter.MaterialViewAt(index).SurfaceFC);Unsupported(delegate{adapter.RequireSurfaceFC(index);});Unsupported(delegate{adapter.BindRecord(index);});Unknown(data.TypeRecordAt(index,0).Material);}
   Unsupported(delegate{adapter.BindAll();});for(uint n=0;n<5;n++)Unknown(data.TypeRecordAt(n,0).Material);
   Check(Object.ReferenceEquals(adapter.BindRecord(0),adapter.BindRecord(0))&&Object.ReferenceEquals(adapter.BindRecord(1),adapter.MaterialViewAt(1)),"Known-only binding retains per-record view identity");
   adapter.MaterialViewAt(2).SurfaceFC.Write(999);Unsupported(delegate{adapter.RequireSurfaceFC(2);});Unsupported(delegate{adapter.BindRecord(2);});Unknown(data.TypeRecordAt(2,0).Material);
  });

  Scenario(delegate{
   byte[][] reordered=Copy(records);var first=reordered[0];reordered[0]=reordered[1];reordered[1]=first;
   Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(Data(chunk),reordered,null);});
   Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(Data(chunk),new byte[][]{records[0],records[1],records[2],records[3]},null);});
   var changed=Copy(records);changed[0]=Replace(changed[0],"flat_rockdark.dbr","Flat_rockdark.dbr");Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(Data(chunk),changed,null);});
   changed=Copy(records);changed[0]=Replace(changed[0],"TerrainType","OtherType");Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(Data(chunk),changed,null);});
   changed=Copy(records);changed[0]=Replace(changed[0],"\"surfaceType\":2","\"surfaceType\":0");Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(Data(chunk),changed,null);});
   changed=Copy(records);changed[0]=Replace(changed[0],"\"surfaceType\":\"Stone\"","\"surfaceType\":\"stone\"");Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(Data(chunk),changed,null);});
   var wrongSource=OwnGroundColdLoader.DecodeType2Chunk(chunk,"unverified saved member");Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(wrongSource,records,null);});
   var changedChunk=(byte[])chunk.Clone();byte[] name=Encoding.ASCII.GetBytes("flat_rockdark.dbr");int at=-1;
   for(int n=0;n<=changedChunk.Length-name.Length;n++){bool same=true;for(int k=0;k<name.Length;k++)if(changedChunk[n+k]!=name[k]){same=false;break;}if(same){at=n;break;}}
   Check(at>=0,"Real layout contains exact record name bytes");changedChunk[at]=(byte)'F';Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(Data(changedChunk),records,null);});
   // Keep the exact source label and record names while changing actual ground
   // bits. The label alone must never admit a different decoded terrain.
   var changedHeightChunk=(byte[])chunk.Clone();changedHeightChunk[36]^=1;var changedHeight=Data(changedHeightChunk);
   Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(changedHeight,records,null);});
   for(uint n=0;n<5;n++)Unknown(changedHeight.TypeRecordAt(n,0).Material);
   var changedCells=Data(chunk);var liveCells=changedCells.Cells128.Read(0);liveCells.WriteOwned(0,liveCells.Read(0,0)^1u);
   Unsupported(delegate{TerrainMaterialCollisionAdapter.FromSavedRecords(changedCells,records,null);});
   for(uint n=0;n<5;n++)Unknown(changedCells.TypeRecordAt(n,0).Material);
  });

  Scenario(delegate{
   Unsupported(delegate{TerrainSurfaceOperandEvidence.FromSavedReport(null,reviewedReportHash,reviewedReaderHash);});
   Unsupported(delegate{TerrainSurfaceOperandEvidence.FromSavedReport(report,new string('0',64),reviewedReaderHash);});
   Unsupported(delegate{TerrainSurfaceOperandEvidence.FromSavedReport(report,reviewedReportHash,new string('0',64));});
   var changed=(byte[])report.Clone();changed[changed.Length-1]^=1;Unsupported(delegate{TerrainSurfaceOperandEvidence.FromSavedReport(changed,reviewedReportHash,reviewedReaderHash);});
   // Deliberately invalid OWN receipt fixture, even with its matching test hash.
   // It never supplies a surface mapping and is not an approved production receipt.
   var json=new JavaScriptSerializer();var invalid=(Dictionary<string,object>)json.DeserializeObject(Encoding.UTF8.GetString(report));invalid["status"]="failed";
   changed=Encoding.UTF8.GetBytes(json.Serialize(invalid));Unsupported(delegate{TerrainSurfaceOperandEvidence.FromSavedReport(changed,Hash(changed),reviewedReaderHash);});
   evidence=TerrainSurfaceOperandEvidence.FromSavedReport(report,reviewedReportHash,reviewedReaderHash);Check(evidence!=null,"Actual reviewed receipt accepted");
  });

  Scenario(delegate{
   var json=new JavaScriptSerializer();var parsed=(Dictionary<string,object>)json.DeserializeObject(Encoding.UTF8.GetString(report));var values=(object[])parsed["values"];
   Check(values.Length==6,"Actual receipt has exactly six comparison operands");int dirtMatches=0;
   foreach(object item in values){var value=(Dictionary<string,object>)item;string raw=(string)value["raw_hex"];uint expected=Convert.ToUInt32(value["surface_fc"]);byte[] bytes=Hex(raw);
    Check(evidence.Resolve(bytes)==expected,"Exact returned operand maps to its observed FC write");
    if(raw=="44697274"){Check(bytes.Length==4,"Actual Dirt comparison length");dirtMatches++;dirtFc=expected;}
   }
   Check(dirtMatches==1,"Dirt must occur exactly once in the actual returned operand bytes before all-five support");
   Check(evidence.Resolve(Encoding.ASCII.GetBytes("Unwalkable"))==0&&evidence.Resolve(Encoding.ASCII.GetBytes("Grass"))==1&&evidence.Resolve(Encoding.ASCII.GetBytes("Stone"))==5&&evidence.Resolve(Encoding.ASCII.GetBytes("Water"))==7,"Pseudo-derived known comparisons retain their exact values");
   foreach(string value in new string[]{"stone","Stone "," Stone","Ston","Stone\0","dirt","Dirt ","UNKNOWN_SURFACE"}){string text=value;Unsupported(delegate{evidence.Resolve(Encoding.ASCII.GetBytes(text));});}
   Unsupported(delegate{evidence.Resolve(null);});Unsupported(delegate{evidence.Resolve(new byte[0]);});
  });

  Scenario(delegate{
   real=Data(chunk);realAdapter=TerrainMaterialCollisionAdapter.FromSavedRecords(real,records,evidence);
   Check(Object.ReferenceEquals(realAdapter.Data,real),"Adapter borrows the same real GroundData");var identities=new HashSet<object>();
   realAdapter.BindAll();
   for(uint n=0;n<5;n++){
    var view=realAdapter.MaterialViewAt(n);var record=real.TypeRecordAt(n,0);uint expected=n==0?5u:n==1?1u:dirtFc;
    Check(identities.Add(view)&&Object.ReferenceEquals(realAdapter.RecordAt(n),record),"Each exact real record has a distinct stable material view");
    Check(realAdapter.RequireSurfaceFC(n)==expected&&view.SurfaceFC.Read(0)==expected,"Actual saved surface reaches the same FC field");
    Check(Object.ReferenceEquals(record.Material.Read(0),view)&&Object.ReferenceEquals(GroundBlockIntersectionContracts.Material1DE810(real,n,new Invocation()).Read(0),view),"Record material and actual getter return the same material view");
    Check(Object.ReferenceEquals(realAdapter.BindRecord(n),view)&&Object.ReferenceEquals(realAdapter.MaterialViewAt(n),view),"Repeated binding reuses existing material identity");
   }
   realAdapter.BindAll();for(uint n=0;n<5;n++)Check(Object.ReferenceEquals(real.TypeRecordAt(n,0).Material.Read(0),realAdapter.MaterialViewAt(n)),"Repeated all-five bind preserves each shared view");
   var detached=realAdapter.RecordAt(0).NameBytes();detached[0]=(byte)'X';Check(realAdapter.RecordAt(0).NameBytes()[0]!=(byte)'X',"Inspection name copy cannot alter authoritative record identity");
  });

  Scenario(delegate{
   var data=Data(chunk);var adapter=TerrainMaterialCollisionAdapter.FromSavedRecords(data,records,evidence);var foreign=new object();data.TypeRecordAt(4,0).Material.Write(foreign);
   Unsupported(delegate{adapter.BindAll();});for(uint n=0;n<4;n++)Unknown(data.TypeRecordAt(n,0).Material);Check(Object.ReferenceEquals(data.TypeRecordAt(4,0).Material.Read(0),foreign),"Conflicting material is preserved and whole-bind preflight makes no partial publication");
   Unsupported(delegate{adapter.BindRecord(4);});Check(Object.ReferenceEquals(data.TypeRecordAt(4,0).Material.Read(0),foreign),"Single bind does not replace conflicting material");
   var second=Data(chunk);var secondAdapter=TerrainMaterialCollisionAdapter.FromSavedRecords(second,records,evidence);secondAdapter.MaterialViewAt(0).SurfaceFC.Write(999);
   Unsupported(delegate{secondAdapter.RequireSurfaceFC(0);});Unsupported(delegate{secondAdapter.BindAll();});for(uint n=0;n<5;n++)Unknown(second.TypeRecordAt(n,0).Material);
  });

  Scenario(delegate{
   object cells=real.Cells128.Read(0);string packedHash=Hash(real.RawArray120());var states=States(real);Check(states.Length==225,"All 225 Blocks use the actual saved ground layout");
   var candidates=new Dictionary<GroundBlockState,GroundBlockMaterialState>();foreach(var state in states){Check(Object.ReferenceEquals(state.Block.TerrainA8.Read(0),real),"Every Block borrows the same real terrain");candidates.Add(state.Block,state);}
   realContext=Context(real);realTree=new GroundSpatialTree();GroundSpatialTreeContracts.PublishOwnColdBlocks(realTree,real,states,realContext,new Invocation());
   Check(realTree.ChangeCount08.Read(0)==225,"Same real 225 Blocks published into the existing Tree implementation");realNodeCount=realContext.ActiveNodeCount;Check(realNodeCount>1,"Actual tree publication subdivides the real bounds");
   var ray=Ray(64.25f,32,64.75f);var hit=Output();realSelected=Query(realTree,ray,hit,realContext);Check(realSelected!=null,"Actual real tree and material adapter complete the central downward hit");
   GroundBlockMaterialState original;Check(candidates.TryGetValue(realSelected.Block,out original)&&Object.ReferenceEquals(original,realSelected.MaterialState)&&Object.ReferenceEquals(realContext.BindBlock(original),realSelected),"Selected entry preserves original Block, candidate owner and context identity");
   var direct=Output();GroundBlockIntersectionContracts.Intersect1D1330(realSelected.MaterialState,ray,direct,realContext.Terrain,realContext.IntersectionOperands,new Invocation());EqualHit(hit,direct);
   realHitFc=hit.Material10.Read(0);Check(realHitFc==1||realHitFc==5||realHitFc==dirtFc,"Real hit material comes from one of the actual resolved saved surfaces");
   Check(hit.Distance00.Read(0)>0&&hit.Distance00.Read(0)<100&&hit.Position04.X0.Read(0)==64.25f&&hit.Position04.Z8.Read(0)==64.75f,"Real hit retains finite geometry and the input vertical ray coordinates");
   Check(Object.ReferenceEquals(real.Cells128.Read(0),cells)&&Hash(real.RawArray120())==packedHash,"Material binding and query preserve authoritative cells and packed weights");
   for(uint n=0;n<5;n++)Check(Object.ReferenceEquals(realAdapter.RecordAt(n),real.TypeRecordAt(n,0))&&Object.ReferenceEquals(real.TypeRecordAt(n,0).Material.Read(0),realAdapter.MaterialViewAt(n)),"All five record-to-material identities survive real Block and Tree queries");
  });

  Scenario(delegate{
   var output=Output();var missed=Query(realTree,Ray(-1000,32,-1000),output,realContext);Check(missed==null&&Untouched(output),"External real miss preserves shared hit fields");
   Check(realSelected!=null&&Object.ReferenceEquals(realSelected.Block.TerrainA8.Read(0),real),"Prior successful entry remains the same real terrain identity");
   for(uint n=0;n<5;n++)Check(Object.ReferenceEquals(real.TypeRecordAt(n,0).Material.Read(0),realAdapter.MaterialViewAt(n)),"Miss does not replace any real material identity");
   Check(Hash(File.ReadAllBytes(Path.Combine(Root,SavedRelative)))==SavedHash,"Saved member unchanged after new integration scenarios");
   for(int n=0;n<5;n++)Check(Hash(File.ReadAllBytes(RecordPath(n)))==RecordHashes[n],"Saved record unchanged after tests "+n);
   Check(Hash(File.ReadAllBytes(Path.Combine(Root,ReportRelative)))==reviewedReportHash,"Actual receipt unchanged after tests");
  });

  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"real_entries\":225,\"real_nodes\":"+realNodeCount+",\"real_hit_fc\":"+realHitFc+",\"actual_dirt_fc\":"+dirtFc+",\"same_material_identity\":true,\"block_tree_same_hit\":true,\"real_external_miss\":true,\"real_material_resolved\":true,\"collision_adapter_only\":true,\"native_load\":false,\"world_floor\":false,\"original_read\":false,\"backend_started\":false,\"gameplay_accepted\":false,\"saved_member_sha256\":\""+SavedHash+"\",\"operand_report_sha256\":\""+reviewedReportHash+"\"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
