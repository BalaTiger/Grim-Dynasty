using System;
using System.IO;
using System.Text;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN cold-data adapter, not native Load/ConstructBlocks. It owns the decoded
 // buffers and never resolves a material, runs a graphics callback, constructs
 // a spatial tree or supplies gEngine+640. Raw header/trailer meanings stay open.
 public sealed class OwnGroundTypeRecord {
  readonly byte[] name,weights;
  public readonly Field<object> Material=new Field<object>();
  internal OwnGroundTypeRecord(byte[] n,byte[] w){name=n;weights=w;}
  public byte[] NameBytes(){return (byte[])name.Clone();}
  public byte[] WeightBytes(){return weights==null?null:(byte[])weights.Clone();}
  public byte ReadWeight(uint index,uint at){if(weights==null||index>=(uint)weights.Length)throw new MissingContract(at,"OWN weight plane unavailable or extent exceeded; not a native failure contract");return weights[(int)index];}
  public string AsciiName(){foreach(byte b in name)if(b<32||b>126)return null;return Encoding.ASCII.GetString(name);}
 }
 public sealed class OwnGroundCells {
  readonly uint[] bits;
  internal OwnGroundCells(uint[] source){bits=source;}
  public int Count{get{return bits.Length;}}
  public uint Read(uint index,uint at){if(index>=(uint)bits.Length)throw new MissingContract(at,"OWN cell extent exceeded; not a native failure contract");return bits[(int)index];}
  public void WriteOwned(uint index,uint value){if(index>=(uint)bits.Length)throw new ArgumentOutOfRangeException("index");bits[(int)index]=value;}
 }
 public enum GroundScalarDispatch { ObservedDisk1DE880 }
 public sealed class OwnGroundData {
  public readonly object Identity=new object();
  public readonly Field<uint> Dimension60=new Field<uint>(),Dimension64=new Field<uint>(),Stride118=new Field<uint>();
  public readonly Field<OwnGroundCells> Cells128=new Field<OwnGroundCells>();
  public readonly Field<GroundScalarDispatch> ScalarSlot100=new Field<GroundScalarDispatch>();
  readonly uint[] header;
  readonly byte[] array120,trailer;
  readonly OwnGroundTypeRecord[] types;
  public readonly string SourceIdentity;
  public readonly int ConsumedBytes;
  internal OwnGroundData(uint[] h,OwnGroundCells cells,byte[] packed,OwnGroundTypeRecord[] records,byte[] tail,string source,int consumed){
   header=h;array120=packed;types=records;trailer=tail;SourceIdentity=source;ConsumedBytes=consumed;
   Dimension60.Write(h[2]);Dimension64.Write(h[3]);Stride118.Write((h[1]>>3)+1);Cells128.Write(cells);
  }
  public uint[] HeaderWords(){return (uint[])header.Clone();}
  public byte[] RawArray120(){return (byte[])array120.Clone();}
  // Shared immutable packed bytes, without creating a second snapshot buffer.
  public uint TypeRecordCount{get{return (uint)types.Length;}}
  public byte ReadPacked120(uint index,uint at){if(index>=(uint)array120.Length)throw new MissingContract(at,"OWN packed120 extent exceeded; not a native failure contract");return array120[(int)index];}
  public OwnGroundTypeRecord TypeRecordAt(uint index,uint at){if(index>=(uint)types.Length)throw new MissingContract(at,"OWN material record extent exceeded; not a native failure contract");return types[(int)index];}
  public byte[] SkippedTrailer(){return (byte[])trailer.Clone();}
  public OwnGroundTypeRecord[] TypeRecords(){return (OwnGroundTypeRecord[])types.Clone();}
  // Explicit platform configuration based on the pinned disk table. This is
  // not evidence that a loaded native vtable cannot change.
  public void SelectObservedScalarSlot(){ScalarSlot100.Write(GroundScalarDispatch.ObservedDisk1DE880);}
 }
 public sealed class GroundBlockState {
  public readonly object Identity=new object();
  public readonly WorldBounds24 Box10=new WorldBounds24();
  public readonly Field<OwnGroundData> TerrainA8=new Field<OwnGroundData>();
  public readonly Field<uint> ColumnB0=new Field<uint>(),RowB4=new Field<uint>(),WidthB8=new Field<uint>(),DepthBC=new Field<uint>();
  public readonly Field<uint> EngineValue38=new Field<uint>(); // gEngine+640: unresolved.
  // No post-construction success flag: primary slot48 and its dependencies
  // are not executed by this data/bounds adapter.
 }
 public sealed class GroundGeometryOperands {
  public readonly Field<uint> PositiveInfinity338458=new Field<uint>(),XorMask34C450=new Field<uint>(),Half34BADC=new Field<uint>();
  public void SelectObservedDiskOperands(){PositiveInfinity338458.Write(0x7f800000u);XorMask34C450.Write(0x80000000u);Half34BADC.Write(0x3f000000u);}
 }
 public static class OwnGroundColdLoader {
  // These limits and all extent checks are OWN safety policy. Native memcpy
  // and low32/MOVSXD arithmetic do not establish the same rejection behavior.
  public const int MaximumChunkBytes=16777216;
  public const uint MaximumCells=1048576;
  sealed class Cursor {
   readonly byte[] bytes;public int At;
   public Cursor(byte[] input){bytes=input;}
   public byte[] Take(int n){if(n<0||n>bytes.Length-At)throw new InvalidDataException("OWN bounded ground extent");var b=new byte[n];Buffer.BlockCopy(bytes,At,b,0,n);At+=n;return b;}
   public uint U32(){var b=Take(4);return (uint)b[0]|((uint)b[1]<<8)|((uint)b[2]<<16)|((uint)b[3]<<24);}
   public int Remaining{get{return bytes.Length-At;}}
  }
  static int Size(ulong n){if(n>MaximumChunkBytes)throw new InvalidDataException("OWN bounded ground size limit");return (int)n;}
  public static OwnGroundData DecodeType2Chunk(byte[] chunk,string sourceIdentity){
   if(chunk==null)throw new ArgumentNullException("chunk");if(chunk.Length>MaximumChunkBytes)throw new InvalidDataException("OWN chunk size limit");
   var c=new Cursor(chunk);if(c.U32()!=2)throw new InvalidDataException("Unsupported ground discriminator");
   var h=new uint[8];for(int k=0;k<h.Length;k++)h[k]=c.U32();uint count=h[1],w=h[2],d=h[3];
   if(count==0||count>256||w<2||d<2||((w-1)&(w-2))!=0||((d-1)&(d-2))!=0)throw new InvalidDataException("OWN supported positive power-of-two-plus-one dimensions/type count");
   ulong cells=(ulong)w*d;if(cells>MaximumCells)throw new InvalidDataException("OWN cell allocation limit");
   var raw=c.Take(Size(cells*4));var values=new uint[(int)cells];
   for(int k=0;k<values.Length;k++){int p=k*4;values[k]=(uint)raw[p]|((uint)raw[p+1]<<8)|((uint)raw[p+2]<<16)|((uint)raw[p+3]<<24);}
   byte[] packed=c.Take(Size(cells*((count>>3)+1)));int weightSize=Size((ulong)(w-1)*(d-1));
   var records=new OwnGroundTypeRecord[(int)count];
   for(int k=0;k<records.Length;k++){uint length=c.U32();if(length>1048576)throw new InvalidDataException("OWN name size limit");records[k]=new OwnGroundTypeRecord(c.Take((int)length),k==0?null:c.Take(weightSize));}
   byte[] tail=c.Take(4);if(c.Remaining!=0)throw new InvalidDataException("OWN exact chunk consumption required");
   return new OwnGroundData(h,new OwnGroundCells(values),packed,records,tail,sourceIdentity,c.At);
  }
 }
 // Pinned native evidence: Engine1DE880,1D0FC0 and descriptor writes1CE910;
 // Engine SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Finite scalar projection only, binary32 after each instruction. NaN/Inf,
 // overflowing uint address arithmetic and signed conversion edge domains
 // explicitly stop as OWN Unsupported. Native NaN clamping/comparison, FP
 // modes/traps, ABI, concurrent mutation and arbitrary byte aliases are open.
 public static class GroundGeometryContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null own ground view");return x;}
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static float Float(uint x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float Finite(float x,uint at){if(float.IsNaN(x)||float.IsInfinity(x))throw new MissingContract(at,"OWN finite binary32 domain only; native exceptional FP behavior not asserted");return x;}
  static uint U32(ulong x,uint at){if(x>uint.MaxValue)throw new MissingContract(at,"OWN unsupported wrapping uint arithmetic");return (uint)x;}
  static float Value(Field<OwnGroundCells> capturedPointer,uint index,uint at){return Finite(Float(Need(capturedPointer.Read(at),at).Read(index,at)),at);}
  public static float Sample1DE880(OwnGroundData data,float x,float z,Invocation invocation){
   var i=Need(invocation,0x1de88f);i.At=0x1de88f;uint w=Need(data,i.At).Dimension60.Read(i.At);
   // MOV captures the pointer/knowledge, not a pointee dereference. A null or
   // unobserved captured pointer stops only at the first real array load.
   i.At=0x1de895;var cells=data.Cells128.Snapshot();
   i.At=0x1de89c;z=Finite(z,i.At);if(!(z>0))z=0;
   i.At=0x1de8a0;uint d=data.Dimension64.Read(i.At);
   i.At=0x1de8a4;x=Finite(x,i.At);if(!(x>0))x=0;
   if(w==0||d==0||w>int.MaxValue||d>int.MaxValue)throw new MissingContract(i.At,"OWN supported positive signed-conversion dimensions");
   i.At=0x1de8c1;float cx=F((float)(long)(w-1));if(x<cx||x==cx)cx=x;
   i.At=0x1de8c5;float cz=F((float)(long)(d-1));if(z<cz||z==cz)cz=z;
   // CVTTSS2SI is signed32, even though dimensions/neighbors use unsigned CMP.
   i.At=0x1de8c9;if(cx>=2147483648.0f)throw new MissingContract(i.At,"OWN CVTTSS2SI range unsupported");uint ix=(uint)(int)cx;
   i.At=0x1de8ce;if(cz>=2147483648.0f)throw new MissingContract(i.At,"OWN CVTTSS2SI range unsupported");uint iz=(uint)(int)cz;
   uint nx=ix+1;if(nx>=w)nx=ix;uint nz=iz+1;if(nz>=d)nz=iz;
   i.At=0x1de8e8;uint row=U32((ulong)w*iz,i.At);float fx=F(cx-F((float)(int)ix));
   i.At=0x1de902;uint i00=U32((ulong)row+ix,i.At);i.At=0x1de906;uint nextRow=U32((ulong)w*nz,i.At);
   i.At=0x1de90a;float h00=Value(cells,i00,i.At);float fz=F(cz-F((float)(int)iz));
   i.At=0x1de919;uint i10=U32((ulong)row+nx,i.At);i.At=0x1de91c;float h10=Value(cells,i10,i.At);
   float a=Finite(F(h10-h00),At(i,0x1de922));i.At=0x1de926;uint i11=U32((ulong)nextRow+nx,i.At);i.At=0x1de92f;uint i01=U32((ulong)nextRow+ix,i.At);
   a=Finite(F(a*fx),At(i,0x1de933));a=Finite(F(a+h00),At(i,0x1de937));
   i.At=0x1de93b;float h11=Value(cells,i11,i.At);
   i.At=0x1de941;float b=Finite(F(h11-Value(cells,i01,i.At)),i.At);
   b=Finite(F(b*fx),At(i,0x1de947));i.At=0x1de94b;b=Finite(F(b+Value(cells,i01,i.At)),i.At); // actual reread
   b=Finite(F(b-a),At(i,0x1de951));b=Finite(F(b*fz),At(i,0x1de955));i.At=0x1de959;return Finite(F(b+a),i.At);
  }
  public static void UpdateBounds1D0FC0(GroundBlockState block,OwnGroundData data,GroundGeometryOperands operands,Invocation invocation){
   var i=Need(invocation,0x1d0fd8);i.At=0x1d0fd8;uint col=Need(block,i.At).ColumnB0.Read(i.At);
   // R14 captures the data argument at1D0FDE; first object access is1D1050.
   i.At=0x1d0fe1;uint depth=block.DepthBC.Read(i.At);i.At=0x1d0feb;uint width=block.WidthB8.Read(i.At);
   i.At=0x1d0ff7;uint x0=U32((ulong)col*9,i.At);i.At=0x1d0fff;uint row=block.RowB4.Read(i.At);
   i.At=0x1d1005;float min=Float(Need(operands,i.At).PositiveInfinity338458.Read(i.At));
   i.At=0x1d1010;float max=Float(Bits(min)^operands.XorMask34C450.Read(i.At));
   if(Bits(min)!=0x7f800000u||Bits(max)!=0xff800000u)throw new MissingContract(i.At,"OWN bounds domain requires explicitly observed infinity operands");
   i.At=0x1d1017;uint z0=U32((ulong)row*9,i.At);
   if(width==0||depth==0||width>10||depth>10)throw new MissingContract(i.At,"OWN bounded 1CE910 descriptor extent domain (1..10)");
   uint endZ=U32((ulong)z0+depth,At(i,0x1d1022));
   for(uint z=z0;z<endZ;z++){
    i.At=0x1d1030;uint endX=U32((ulong)x0+block.WidthB8.Read(i.At),i.At);
    for(uint x=x0;x<endX;x++){
     i.At=0x1d1050;var scalar=Need(data,i.At).ScalarSlot100.Snapshot();
     i.At=0x1d1064;if(scalar.Read(i.At)!=GroundScalarDispatch.ObservedDisk1DE880)throw new MissingContract(i.At,"Unsupported Terrain slot100 selection");
     float v=Sample1DE880(data,F((float)(long)x),F((float)(long)z),i);
     if(!(min<v))min=v;if(!(max>v))max=v;
     i.At=0x1d107a;endX=U32((ulong)x0+block.WidthB8.Read(i.At),i.At);
    }
    i.At=0x1d1088;endZ=U32((ulong)z0+block.DepthBC.Read(i.At),i.At);
   }
   i.At=0x1d10a5;float half=Float(operands.Half34BADC.Read(i.At));if(Bits(half)!=0x3f000000u)throw new MissingContract(i.At,"OWN requires explicit observed half operand");
   i.At=0x1d10b1;uint liveWidth=block.WidthB8.Read(i.At);
   uint cx=U32((ulong)x0*2+width-1,At(i,0x1d10b7));float span=Finite(F(max-min),At(i,0x1d10cd));float centerX=F((float)(long)cx);
   if(liveWidth==0||liveWidth>int.MaxValue)throw new MissingContract(At(i,0x1d10d6),"OWN signed width conversion domain");
   uint cz=U32((ulong)z0*2+depth-1,At(i,0x1d10e0));float sum=Finite(F(max+min),At(i,0x1d10e3));
   float ey=Finite(F(span*half),At(i,0x1d10ec));centerX=F(centerX*half);float centerY=Finite(F(sum*half),At(i,0x1d10f4));
   i.At=0x1d10f8;block.Box10.Center00.Write(centerX);float centerZ=F(F((float)(long)cz)*half);
   i.At=0x1d1105;block.Box10.Extent10.Write(ey);i.At=0x1d1113;block.Box10.Center04.Write(centerY);i.At=0x1d1118;block.Box10.Center08.Write(centerZ);
   i.At=0x1d1121;uint liveDepth=block.DepthBC.Read(i.At);if(liveDepth==0||liveDepth>int.MaxValue)throw new MissingContract(i.At,"OWN signed depth conversion domain");
   i.At=0x1d1130;block.Box10.Extent0C.Write(F(F((float)(int)(liveWidth-1))*half));i.At=0x1d1140;block.Box10.Extent14.Write(F(F((float)(int)(liveDepth-1))*half));
  }
  public static GroundBlockState[] CreateOwnBlockBounds(OwnGroundData data,GroundGeometryOperands operands,Invocation invocation){
   Need(data,0x1ce910);uint w=data.Dimension60.Read(0x1ce910),d=data.Dimension64.Read(0x1ce910);
   if(w<2||d<2||w>OwnGroundColdLoader.MaximumCells||d>OwnGroundColdLoader.MaximumCells||(ulong)w*d>OwnGroundColdLoader.MaximumCells)throw new MissingContract(0x1ce910,"OWN bounded descriptor allocation");
   uint columns=(w+7)/9,rows=(d+7)/9;var blocks=new GroundBlockState[(int)(columns*rows)];
   for(uint row=0;row<rows;row++)for(uint col=0;col<columns;col++){
    var b=new GroundBlockState();b.TerrainA8.Write(data);b.ColumnB0.Write(col);b.RowB4.Write(row);
    // Row outer/column inner. Native uses low32 min(end,dimension-1)-end+10,
    // including intermediate unsigned wrap on clipped edges. This equivalent
    // OWN bounded-domain expression preserves the final value without treating
    // that normal clipping wrap as a malformed-input failure.
    uint x=col*9,z=row*9;b.WidthB8.Write(Math.Min(x+9,w-1)-x+1);b.DepthBC.Write(Math.Min(z+9,d-1)-z+1);
    blocks[(int)(row*columns+col)]=b;UpdateBounds1D0FC0(b,data,operands,invocation);
   }
   return blocks; // OWN descriptors/bounds only; no slot48, tree or collision.
  }
 }
}
