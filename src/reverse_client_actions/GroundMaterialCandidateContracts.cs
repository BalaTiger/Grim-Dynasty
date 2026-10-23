using System;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN storage for the normal Begin-insertion call made by1DEB80. No raw
 // pointer alias, arbitrary iterator, concurrent observer or native allocator
 // contract is exposed. Native geometry/data inputs remain shared objects.
 public sealed class GroundCandidateVector {
  uint[] storage;
  int count,capacity;
  public GroundCandidateVector(int initialCapacity){
   if(initialCapacity<0||initialCapacity>256)throw new ArgumentOutOfRangeException("initialCapacity");
   storage=new uint[initialCapacity];capacity=initialCapacity;
  }
  public int Count{get{return count;}}
  public int Capacity{get{return capacity;}}
  public object StorageIdentity{get{return storage;}}
  // The future1D1330 consumer must reload this shared vector, not retain the
  // detached inspection snapshot across Terrain callbacks or other mutation.
  public uint ReadAt(int index,uint at){if(index<0||index>=count)throw new MissingContract(at,"OWN candidate extent exceeded; not a native failure contract");return storage[index];}
  public uint[] SnapshotValues(){var result=new uint[count];Array.Copy(storage,0,result,0,count);return result;}
  internal void ResetEnd(){count=0;}
  internal void InsertBegin(uint value,Invocation i){
   //1CE0E0: position is precisely currentBegin, value is the independent
   // caller-local DWORD, and the ignored result iterator denotes newBegin.
   // The general alias/iterator branches are deliberately not generalized.
   int oldCount=count;
   i.At=0x1ce120;
   if(capacity-count<1){
    i.At=0x1ce179;int wanted=Math.Max(oldCount+1,capacity+capacity/2);
    //256 live records can require316 slots when cold-grown by native1.5x.
    //384 also covers a caller-supplied initial capacity up to256 growing once.
    if(wanted>384)throw new MissingContract(i.At,"OWN candidate-vector capacity limit; not native length_error behavior");
    i.At=0x1ce185;var replacement=new uint[wanted];
    // Normal OWN allocation only. OOM is not caught or called native success;
    // no claim is made about native exceptional allocation/cleanup semantics.
    i.At=0x1ce1b2;replacement[0]=value;
    // Prefix before Begin has zero length. Suffix is copied after new value.
    i.At=0x1ce1de;Array.Copy(storage,0,replacement,1,oldCount);
    // Managed storage release replaces native free only in this OWN domain.
    i.At=0x1ce216;capacity=wanted;
    i.At=0x1ce222;count=oldCount+1;
    i.At=0x1ce226;storage=replacement;
   }else if(oldCount==0){
    //1CE245 captures value before the zero-length move; construction atEnd
    // occurs before End advances. The caller-local input cannot alias output.
    i.At=0x1ce27d;storage[0]=value;
    i.At=0x1ce282;count=1;
   }else{
    i.At=0x1ce2e7;storage[oldCount]=storage[oldCount-1];
    i.At=0x1ce2f9;count=oldCount+1;
    i.At=0x1ce30a;Array.Copy(storage,0,storage,1,oldCount-1);
    i.At=0x1ce320;storage[0]=value;
   }
   i.At=0x1ce33e; // Return iterator would be this storage's Begin.
  }
 }

 // A per-Block companion, preserving the authoritative Block and Terrain
 // identities. This does not run primary slot48/GrassObject cleanup or claim
 // complete native ConstructBlocks. Callers may retain one companion perBlock.
 public sealed class GroundBlockMaterialState {
  public readonly GroundBlockState Block;
  public readonly GroundCandidateVector CandidatesC8;
  public GroundBlockMaterialState(GroundBlockState block){
   if(block==null)throw new ArgumentNullException("block");Block=block;CandidatesC8=new GroundCandidateVector(0);
  }
  public void Populate(GroundGeometryOperands operands,Invocation i){
   if(i==null)throw new ArgumentNullException("i");i.At=0x1d12f9;
   var terrain=Block.TerrainA8.Read(i.At);i.At=0x1d130e;
   GroundMaterialCandidateContracts.Produce1DEB80(terrain,CandidatesC8,Block.Box10,operands,i);
  }
 }

 // Engine1DEB80+1CE0E0 evidence: ground-intersection-remaining/11 and
 // ground-face-and-unlink/14. Engine SHA2564c2ea14c...b8a93153.
 // The supported normal domain is finite binary32, nonwrapping uint indices,
 // valid OWN dimensions/packed extents and independent candidate-value input.
 // No fake empty candidate list, resolved material or null resource is added.
 public static class GroundMaterialCandidateContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null OWN ground candidate view");return value;}
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static float Finite(float x,uint at){if(float.IsNaN(x)||float.IsInfinity(x))throw new MissingContract(at,"OWN finite binary32 candidate domain; native exceptional FP behavior unknown");return x;}
  static int Truncate(float x,uint at){Finite(x,at);if(x>=2147483648.0f||x< -2147483648.0f)throw new MissingContract(at,"OWN signed CVTTSS2SI range unsupported");return (int)x;}
  static uint Index(ulong value,uint at){if(value>uint.MaxValue)throw new MissingContract(at,"OWN unsupported wrapping packed120 index");return (uint)value;}
  static uint Dimension(uint value,uint at){if(value<2||value>OwnGroundColdLoader.MaximumCells)throw new MissingContract(at,"OWN bounded dimension domain");return value;}
  static float Half(GroundGeometryOperands o,uint at){uint bits=Need(o,at).Half34BADC.Read(at);if(bits!=0x3f000000u)throw new MissingContract(at,"OWN requires explicitly observed half operand");return Float(bits);}

  public static void Produce1DEB80(OwnGroundData data,GroundCandidateVector output,WorldBounds24 box,GroundGeometryOperands operands,Invocation invocation){
   var i=Need(invocation,0x1deb91);i.At=0x1deb97;Need(output,i.At).ResetEnd();
   i.At=0x1deb9e;float cx=Finite(Need(box,i.At).Center00.Read(i.At),i.At);
   i.At=0x1deba6;float ex=Finite(box.Extent0C.Read(i.At),i.At);
   i.At=0x1debaf;float cz=Finite(box.Center08.Read(i.At),i.At);
   i.At=0x1debb5;float lowerX=Finite(F(cx-ex),i.At);
   i.At=0x1debb9;float ez=Finite(box.Extent14.Read(i.At),i.At);
   i.At=0x1debbf;float upperX=Finite(F(ex+cx),i.At);
   i.At=0x1debc3;uint width=Need(data,i.At).Dimension60.Read(i.At);
   i.At=0x1debc6;int signedLowerX=Truncate(lowerX,i.At);
   i.At=0x1debca;upperX=Finite(F(upperX+Half(operands,i.At)),i.At);
   uint firstX=signedLowerX<0?0u:(uint)signedLowerX;
   i.At=0x1debd7;float lowerZ=Finite(F(cz-ez),i.At);
   i.At=0x1debdf;float upperZ=Finite(F(ez+cz),i.At);
   i.At=0x1debe3;uint maxX=Dimension(width,i.At)-2;
   i.At=0x1debe6;uint lastX=unchecked((uint)Truncate(upperX,i.At));
   i.At=0x1debea;upperZ=Finite(F(upperZ+Half(operands,i.At)),i.At);
   i.At=0x1debf2;int signedLowerZ=Truncate(lowerZ,i.At);
   i.At=0x1debf7;uint lastZ=unchecked((uint)Truncate(upperZ,i.At));
   uint firstZ=signedLowerZ<0?0u:(uint)signedLowerZ;
   if(lastX>maxX)lastX=maxX; // Actual unsigned comparison, not float clamp.
   i.At=0x1dec12;uint depth=data.Dimension64.Read(i.At);
   i.At=0x1dec15;uint maxZ=Dimension(depth,i.At)-2;
   if(lastZ>maxZ)lastZ=maxZ;
   // OWN loader owns a fixed32B-record-count equivalent; no snapshot of the
   // packed array or Box is substituted. Native begin/end pointer aliases and
   // concurrent record-vector mutation are outside this API.
   i.At=0x1dec2f;uint records=data.TypeRecordCount;
   if(records>256)throw new MissingContract(i.At,"OWN record domain exceeded");
   for(int candidate=(int)records-1;candidate>=0;candidate--){
    uint layer=(uint)candidate+1;
    byte mask=(byte)(1u<<(int)(layer&7));uint byteInCell=(byte)(layer>>3);
    bool found=false;
    for(uint z=firstZ;z<=lastZ&&!found;z++){
     i.At=0x1dec95;uint liveWidth=data.Dimension60.Read(i.At);
     i.At=0x1dec9b;uint row=Index((ulong)liveWidth*z,i.At);
     for(uint x=firstX;x<=lastX&&!found;x++){
      i.At=0x1deca4;uint cell=Index((ulong)row+x,i.At);
      // Native reloads+120 and+118 per probed cell. ReadPacked120 borrows the
      // one immutable backing buffer owned by this same OwnGroundData.
      i.At=0x1decb7;uint stride=data.Stride118.Read(i.At);
      uint offset=Index((ulong)cell*stride,i.At);
      i.At=0x1decc6;offset=Index((ulong)offset+byteInCell,i.At);
      i.At=0x1deccd;found=(data.ReadPacked120(offset,i.At)&mask)!=0;
     }
    }
    if(found){i.At=0x1ded18;output.InsertBegin((uint)candidate,i);}
   }
   i.At=0x1ded53;
  }
 }
}
