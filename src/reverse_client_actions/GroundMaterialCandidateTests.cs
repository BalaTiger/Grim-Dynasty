using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

public static class GroundMaterialCandidateTestProgram {
 const string SavedPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 const int GroundOffset=273260,GroundLength=149073;
 // Source: r1-ground-intersection-remaining/11, Engine1DEB80 assembly
 // SHA256 5e573fa7eb94b60e885304656fa1dfb997f35640b5d28163e8554b3fe3ac1ba8.
 // OWN fixtures and saved-member component checks only: no native Load,
 // material/resource resolution, spatial tree or gameplay claim.
 static int assertions,scenarios,realNonempty,realMemberships;
 static string realMembershipHash;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Scenario(Action action){action();scenarios++;}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create()){return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}}
 static void Missing(Action action,uint at){
  assertions++;try{action();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Wrong OWN failure address: "+e.Address.ToString("x")+", wanted "+at.ToString("x"));return;}
  throw new Exception("Expected explicit OWN Unsupported at "+at.ToString("x"));
 }
 static GroundGeometryOperands Operands(){var o=new GroundGeometryOperands();o.SelectObservedDiskOperands();return o;}
 static WorldBounds24 Box(float x,float z,float ex,float ez){
  var b=new WorldBounds24();b.Center00.Write(x);b.Center08.Write(z);b.Extent0C.Write(ex);b.Extent14.Write(ez);
  // Y center/extent intentionally remain unknown: this candidate path reads XZ.
  return b;
 }
 static void Produce(OwnGroundData data,GroundCandidateVector output,WorldBounds24 box){GroundMaterialCandidateContracts.Produce1DEB80(data,output,box,Operands(),new Invocation());}
 static void Values(GroundCandidateVector output,string label,params uint[] expected){
  uint[] actual=output.SnapshotValues();Check(output.Count==expected.Length&&actual.Length==expected.Length,label+" count");
  bool equal=true,ascending=true;for(int n=0;n<actual.Length;n++){equal&=actual[n]==expected[n];if(n>0)ascending&=actual[n-1]<actual[n];}
  Check(equal&&ascending,label+" exact ordered membership without duplicates");
 }
 static byte[] Fixture(int width,int depth,int typeCount,byte[] packed){
  int stride=(typeCount>>3)+1;if(packed.Length!=width*depth*stride)throw new Exception("Fixture packed extent");
  using(var s=new MemoryStream()){using(var w=new BinaryWriter(s)){
   w.Write(2u);foreach(uint value in new uint[]{1u,(uint)typeCount,(uint)width,(uint)depth,0u,0u,0u,0u})w.Write(value);
   for(int n=0;n<width*depth;n++)w.Write(0u);w.Write(packed);
   for(int n=0;n<typeCount;n++){byte[] name=Encoding.ASCII.GetBytes("own/candidate/"+n);w.Write((uint)name.Length);w.Write(name);if(n>0)w.Write(new byte[(width-1)*(depth-1)]);}
   w.Write(0u);w.Flush();return s.ToArray();
  }}
 }
 static OwnGroundData Data(int width,int depth,int typeCount,byte[] packed){return OwnGroundColdLoader.DecodeType2Chunk(Fixture(width,depth,typeCount,packed),"OWN analytical candidate fixture");}
 static OwnGroundData AllAtFirstCell(int count){
  var packed=new byte[9*((count>>3)+1)];for(int bit=1;bit<=count;bit++)packed[bit/8]|=(byte)(1<<(bit%8));return Data(3,3,count,packed);
 }
 // Independent saved-data membership oracle: scan each known integer Block
 // rectangle once, union raw serialized bitmap bytes, then enumerate set bits.
 // It does not call Produce, ReadPacked120 or reproduce its descending search,
 // float conversion, early-exit or vector insertion implementation.
 static uint[] RealExpected(byte[] chunk,int column,int row){
  int bitmapOffset=36+129*129*4,x0=column*9,z0=row*9;
  int x1=Math.Min(x0+9,127),z1=Math.Min(z0+9,127);byte union=0;
  for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)union|=chunk[bitmapOffset+z*129+x];
  var values=new uint[5];int count=0;for(int bit=1;bit<=5;bit++)if((union&(1<<bit))!=0)values[count++]=(uint)(bit-1);
  var result=new uint[count];Array.Copy(values,result,count);return result;
 }
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],SavedPath,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixed saved member argument required");
  byte[] member=File.ReadAllBytes(SavedPath);Check(member.Length==1091357&&Hash(member)==SavedHash,"Saved member fingerprint before component tests");
  var chunk=new byte[GroundLength];Buffer.BlockCopy(member,GroundOffset,chunk,0,chunk.Length);
  Scenario(delegate{
   var packed=new byte[9];packed[0]=1;var vector=new GroundCandidateVector(0);
   Produce(Data(3,3,1,packed),vector,Box(0,0,0,0));Values(vector,"bit0 is not candidate0");Check(vector.Capacity==0,"No hit does not manufacture storage");
   packed[0]=2;Produce(Data(3,3,1,packed),vector,Box(0,0,0,0));Values(vector,"bit1 selects candidate0",0u);
   Check(vector.Capacity==1,"First real hit grows capacity to one");
  });
  Scenario(delegate{
   var packed=new byte[18];packed[1]=1;var vector=new GroundCandidateVector(0);
   Produce(Data(3,3,9,packed),vector,Box(0,0,0,0));Values(vector,"second byte bit0 is candidate7",7u);
   packed[1]=2;Produce(Data(3,3,9,packed),vector,Box(0,0,0,0));Values(vector,"second byte bit1 is candidate8",8u);
   packed[0]=2;packed[1]=3;Produce(Data(3,3,9,packed),vector,Box(0,0,0,0));Values(vector,"descending search and Begin insertion order",0u,7u,8u);
  });
  Scenario(delegate{
   var packed=new byte[25];packed[0]=2;packed[1]=4;packed[2]=8;packed[4]=4;packed[5]=8;
   var data=Data(5,5,3,packed);var vector=new GroundCandidateVector(0);
   Produce(data,vector,Box(0,0,0,0));Values(vector,"first XZ point",0u);
   Produce(data,vector,Box(1,0,0,0));Values(vector,"second XZ point",1u);
   Produce(data,vector,Box(2,0,0,0));Values(vector,"third XZ point",2u);
   Produce(data,vector,Box(0,1,0,0));Values(vector,"row address uses width five, not width minus one",2u);
   Produce(data,vector,Box(.5f,0,0,0));Values(vector,"upper half addition includes next integer cell",0u,1u);
   Produce(data,vector,Box(3.75f,0,.25f,0));Values(vector,"upper clamp excludes last grid column");
   Produce(data,vector,Box(10,0,0,0));Values(vector,"lower beyond clamped upper is empty");
   Produce(data,vector,Box(-2,0,0,0));Values(vector,"negative upper conversion uses unsigned comparison",0u,1u,2u);
  });
  Scenario(delegate{
   var vector=new GroundCandidateVector(0);int[] expectedCapacity={1,2,3,4,6,6};
   for(int n=1;n<=6;n++){
    object prior=vector.StorageIdentity;int oldCapacity=vector.Capacity;Produce(AllAtFirstCell(n),vector,Box(0,0,0,0));
    var expected=new uint[n];for(int k=0;k<n;k++)expected[k]=(uint)k;Values(vector,"growth step "+n,expected);
    Check(vector.Capacity==expectedCapacity[n-1],"Observed Begin-insertion capacity step "+n);
    Check(Object.ReferenceEquals(prior,vector.StorageIdentity)==(oldCapacity==vector.Capacity),"Only growth replaces the owned storage at step "+n);
   }
   uint[] detached=vector.SnapshotValues();detached[0]=999;Check(vector.SnapshotValues()[0]==0,"SnapshotValues cannot mutate authoritative vector");
  });
  Scenario(delegate{
   // Independent maximum loader domain: layer bits1..256 are all present in
   // cell0, including layer256 in byte32. Normal1.5x growth must retain the
   // final211->316 allocation rather than cap the buffer at the record count.
   var data=AllAtFirstCell(256);var before=data.TypeRecords();var vector=new GroundCandidateVector(0);
   Produce(data,vector,Box(0,0,0,0));var expected=new uint[256];for(int n=0;n<expected.Length;n++)expected[n]=(uint)n;
   Values(vector,"all256 supported records",expected);Check(vector.Count==256&&vector.Capacity==316,"Maximum record count preserves normal final growth to316");
   var after=data.TypeRecords();Check(before.Length==256&&after.Length==256,"All256 original material records remain present");
   bool sameRecords=true;for(int n=0;n<after.Length;n++){sameRecords&=Object.ReferenceEquals(before[n],after[n]);Missing(delegate{after[n].Material.Read(0);},0);}
   Check(sameRecords,"Candidate production preserves shared records and never resolves their Materials");
   for(int n=0;n<before.Length;n++){
    Check(Object.ReferenceEquals(data.TypeRecordAt((uint)n,0),before[n]),"TypeRecordAt borrows authoritative record identity "+n);
    if(n>0)Check(data.TypeRecordAt((uint)n,0).ReadWeight(0,0)==0,"Nonzero record borrows actual zero-filled fixture weight "+n);
   }
   Missing(delegate{data.TypeRecordAt(0,0).ReadWeight(0,0);},0);
   Missing(delegate{data.TypeRecordAt(256,0);},0);
   Missing(delegate{data.TypeRecordAt(1,0).ReadWeight(4,0);},0);
  });
  Scenario(delegate{
   var vector=new GroundCandidateVector(8);object storage=vector.StorageIdentity;var data=AllAtFirstCell(3);
   Produce(data,vector,Box(0,0,0,0));Values(vector,"preallocated suffix movement",0u,1u,2u);
   Produce(Data(3,3,3,new byte[9]),vector,Box(0,0,0,0));Values(vector,"second call clears previous count");
   var packed=new byte[9];packed[0]=4;Produce(Data(3,3,3,packed),vector,Box(0,0,0,0));Values(vector,"reuse contains only new membership",1u);
   Check(vector.Capacity==8&&Object.ReferenceEquals(storage,vector.StorageIdentity),"Successful, empty and refill calls retain existing capacity and identity");
  });
  Scenario(delegate{
   var data=AllAtFirstCell(1);var vector=new GroundCandidateVector(0);Produce(data,vector,Box(0,0,0,0));object storage=vector.StorageIdentity;
   var invocation=new Invocation();Missing(delegate{GroundMaterialCandidateContracts.Produce1DEB80(data,vector,new WorldBounds24(),Operands(),invocation);},0x1deb9e);
   Check(invocation.At==0x1deb9e,"Unknown Box stop is recorded");Values(vector,"unknown Box fails after Count clear");
   Check(vector.Capacity==1&&Object.ReferenceEquals(storage,vector.StorageIdentity),"Unknown Box keeps allocated storage after clear");
   Produce(data,vector,Box(0,0,0,0));Values(vector,"same vector recovers through a later explicit valid invocation",0u);
  });
  Scenario(delegate{
   var data=AllAtFirstCell(1);var vector=new GroundCandidateVector(4);object storage=vector.StorageIdentity;
   Produce(data,vector,Box(0,0,0,0));Missing(delegate{Produce(data,vector,Box(float.NaN,0,0,0));},0x1deb9e);Values(vector,"NaN does not leave stale successful membership");
   Missing(delegate{Produce(data,vector,Box(0,0,0,float.PositiveInfinity));},0x1debb9);
   Missing(delegate{Produce(data,vector,Box(2147483648.0f,0,0,0));},0x1debc6);
   Missing(delegate{Produce(data,vector,Box(float.MaxValue,0,float.MaxValue,0));},0x1debbf);
   Check(vector.Count==0&&vector.Capacity==4&&Object.ReferenceEquals(storage,vector.StorageIdentity),"OWN FP/conversion stops retain only the actual clear prefix");
  });
  Scenario(delegate{
   var vector=new GroundCandidateVector(2);var data=AllAtFirstCell(1);Produce(data,vector,Box(0,0,0,0));
   var operands=new GroundGeometryOperands();var invocation=new Invocation();
   Missing(delegate{GroundMaterialCandidateContracts.Produce1DEB80(data,vector,Box(0,0,0,0),operands,invocation);},0x1debca);
   Values(vector,"unobserved half operand is Unsupported after clear");Check(invocation.At==0x1debca,"Operand failure address recorded");
   data.Dimension60.Forget();Missing(delegate{Produce(data,vector,Box(0,0,0,0));},0x1debc3);Values(vector,"unknown live dimension is not defaulted");
  });
  Scenario(delegate{
   // Nine cells, two packed bytes per cell. Candidates8 and7 hit byte1 at
   // cell0. Candidate6 misses byte0 then probes cell1 at the mutated stride18,
   // outside the18-byte owner. Its earlier insertions must survive the stop.
   var packed=new byte[18];packed[1]=3;var data=Data(3,3,9,packed);data.Stride118.Write(18);
   var vector=new GroundCandidateVector(0);var invocation=new Invocation();
   Missing(delegate{GroundMaterialCandidateContracts.Produce1DEB80(data,vector,Box(1,1,1,1),Operands(),invocation);},0x1deccd);
   Check(invocation.At==0x1deccd,"Packed extent stop records actual TEST address");Values(vector,"failure keeps completed Begin-insertion prefix",7u,8u);
   Check(vector.Capacity==2,"Failure prefix includes the two real growth steps");object storage=vector.StorageIdentity;
   data.Stride118.Write(2);Produce(data,vector,Box(1,1,1,1));Values(vector,"restored live stride produces real membership",7u,8u);
   Check(Object.ReferenceEquals(storage,vector.StorageIdentity),"Valid retry clears and reuses prefix storage");
  });
  Scenario(delegate{
   var a=new byte[9];a[0]=2;a[1]=4;var first=Data(3,3,3,a);var b=new byte[9];b[1]=8;var second=Data(3,3,3,b);
   var block=new GroundBlockState();block.TerrainA8.Write(first);block.Box10.Center00.Write(0);block.Box10.Center08.Write(0);block.Box10.Extent0C.Write(0);block.Box10.Extent14.Write(0);
   var state=new GroundBlockMaterialState(block);object boxIdentity=block.Box10;
   state.Populate(Operands(),new Invocation());Values(state.CandidatesC8,"companion initial terrain",0u);
   block.Box10.Center00.Write(1);state.Populate(Operands(),new Invocation());Values(state.CandidatesC8,"companion consumes same live Box",1u);
   block.TerrainA8.Write(second);state.Populate(Operands(),new Invocation());Values(state.CandidatesC8,"companion consumes new authoritative terrain binding",2u);
   Check(Object.ReferenceEquals(state.Block,block)&&Object.ReferenceEquals(state.Block.Box10,boxIdentity)&&Object.ReferenceEquals(state.Block.TerrainA8.Read(0),second),"Companion preserves Block/Box/shared terrain identity");
   Missing(delegate{block.EngineValue38.Read(0);},0);Missing(delegate{second.TypeRecords()[2].Material.Read(0);},0);
  });
  Scenario(delegate{
   var data=OwnGroundColdLoader.DecodeType2Chunk(chunk,SavedHash+":"+GroundOffset+"+"+GroundLength);
   Check(data.ConsumedBytes==GroundLength&&data.TypeRecordCount==5&&data.Dimension60.Read(0)==129&&data.Dimension64.Read(0)==129&&data.Stride118.Read(0)==1,"Fixed real candidate data domain");
   data.SelectObservedScalarSlot();var blocks=GroundGeometryContracts.CreateOwnBlockBounds(data,Operands(),new Invocation());Check(blocks.Length==225,"Real225 Block bounds prepared");
   var manifest=new StringBuilder();int expectedNonempty=0,expectedMemberships=0;
   for(int n=0;n<blocks.Length;n++){
    uint[] expected=RealExpected(chunk,n%15,n/15);var state=new GroundBlockMaterialState(blocks[n]);state.Populate(Operands(),new Invocation());
    Values(state.CandidatesC8,"real Block "+n,expected);
    Check(Object.ReferenceEquals(state.Block,blocks[n])&&Object.ReferenceEquals(state.Block.TerrainA8.Read(0),data),"Real Block shares authoritative decoded data "+n);
    if(expected.Length>0)expectedNonempty++;expectedMemberships+=expected.Length;
    uint[] actual=state.CandidatesC8.SnapshotValues();if(actual.Length>0)realNonempty++;realMemberships+=actual.Length;
    manifest.Append(n).Append(':');for(int k=0;k<actual.Length;k++){if(k!=0)manifest.Append(',');manifest.Append(actual[k]);}manifest.Append('\n');
   }
   Check(expectedNonempty>0&&expectedMemberships>0,"Real serialized bitmap has material membership: default empty cannot pass");
   Check(realNonempty==expectedNonempty&&realMemberships==expectedMemberships,"All225 real membership results match independent serialized-byte union oracle");
   realMembershipHash=Hash(Encoding.UTF8.GetBytes(manifest.ToString()));
   Missing(delegate{data.TypeRecords()[0].Material.Read(0);},0);
  });
  Check(Hash(File.ReadAllBytes(SavedPath))==SavedHash,"Saved member unchanged after tests");
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"real_blocks\":225,\"real_nonempty_blocks\":"+realNonempty+",\"real_candidate_memberships\":"+realMemberships+",\"real_membership_manifest_sha256\":\""+realMembershipHash+"\",\"saved_member_sha256\":\""+SavedHash+"\",\"native_load\":false,\"resolved_materials\":false,\"full_construct_blocks\":false,\"floor_collision\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
