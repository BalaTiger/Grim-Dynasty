using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Native node26DCA0 preserves bits29..31 of an allocation that26DC10 never
 // clears. This word records knowledge separately, so plain MOV/AND/OR do not
 // manufacture initialized high bits. ReadMasked requests only consumed bits.
 public sealed class NavigationNodePackedWord {
  uint bits,known;
  public uint KnownMask {get{return known;}}
  public uint Read(uint at){return ReadMasked(UInt32.MaxValue,at);}
  public uint ReadMasked(uint mask,uint at){if((known&mask)!=mask)throw new MissingContract(at,"Unobserved bits in packed node word");return bits&mask;}
  public void Write(uint value){bits=value;known=UInt32.MaxValue;}
  public void Forget(){bits=0;known=0;}
  public void Transform(uint andMask,uint orBits){bits=(bits&andMask)|orBits;known=(known&andMask)|~andMask|orBits;}
  public NavigationNodePackedWord Snapshot(){var value=new NavigationNodePackedWord();value.bits=bits;value.known=known;return value;}
  public void CopyFrom(NavigationNodePackedWord value){if(value==null)throw new ArgumentNullException("packed snapshot");bits=value.bits;known=value.known;}
 }
 public sealed class NavigationSearchNode:NavigationView {
  public readonly NavigationPoint3 Position00=new NavigationPoint3();
  public readonly Field<float> Cost0C=new Field<float>(),Total10=new Field<float>();
  public readonly NavigationNodePackedWord Packed14=new NavigationNodePackedWord();
  public readonly Field<uint> Reference18=new Field<uint>();
 }
 public sealed class NavigationNodePool:NavigationView {
  public readonly Field<NavigationSearchNode[]> Nodes00=new Field<NavigationSearchNode[]>();
  public readonly Field<Field<ushort>[]> First08=new Field<Field<ushort>[]>(),Next10=new Field<Field<ushort>[]>();
  public readonly Field<int> MaxNodes18=new Field<int>(),HashSize1C=new Field<int>(),Count20=new Field<int>();
 }
 public sealed class NavigationNodeQueue:NavigationView {
  public readonly Field<Field<NavigationSearchNode>[]> Heap00=new Field<Field<NavigationSearchNode>[]>();
  public readonly Field<int> Capacity08=new Field<int>(),Count0C=new Field<int>();
 }
 // Each bundle owns two distinct local point records. Consumers may retain or
 // reuse it to observe a failed call's prefix. Arbitrary output/source aliases
 // and replacing the coordinates with native overlapping addresses are outside
 // this adapter; these are not additional guards in the recovered function.
 public sealed class NavigationPortalEndpoints {
  public readonly NavigationPoint3 Left=new NavigationPoint3(),Right=new NavigationPoint3();
 }
 // Source: r1-navigation-cross-poly-corridor normalized pseudo+assembly:
 // 26DC10,26DCA0,25F680,26DDC0,25F6F0,2631E0. Core/Mesh stay unchanged.
 // Managed allocations succeed in this finite owned-address domain. No CRT
 // allocator result, native exception, concurrent mutation, overlapping cell
 // layout, or initialization of unobserved node/heap bytes is synthesized.
 public static class NavigationSearchStorage {
  public const int OwnMaximumNodes=65535,OwnMaximumHashSize=65536;
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null search storage/address");var view=value as NavigationView;if(view!=null)view.RequireLive(at);return value;}
  static T At<T>(T[] values,long index,uint at)where T:class{if(values==null||index<0||index>=values.Length)throw new MissingContract(at,"Outside declared own search-array extent");return Need(values[(int)index],at);}
  static void Budget(int steps,uint at){if(steps>OwnMaximumNodes+1)throw new MissingContract(at,"Own search-storage traversal budget; no native cycle recovery implied");}
  static Field<ushort>[] Shorts(int size){var values=new Field<ushort>[size];for(int n=0;n<size;n++)values[n]=new Field<ushort>();return values;}
  public static NavigationNodePool CreatePool26DC10(int capacity,int hashSize){
   if(capacity<1||capacity>OwnMaximumNodes||hashSize<1||hashSize>OwnMaximumHashSize)throw new ArgumentOutOfRangeException("Own positive finite pool allocation domain");
   var p=new NavigationNodePool();p.MaxNodes18.Write(capacity);p.Nodes00.Write(null);p.First08.Write(null);p.Next10.Write(null);p.HashSize1C.Write(hashSize);p.Count20.Write(0);
   var nodes=new NavigationSearchNode[capacity];for(int n=0;n<capacity;n++)nodes[n]=new NavigationSearchNode();p.Nodes00.Write(nodes);
   var next=Shorts(capacity);p.Next10.Write(next);var first=Shorts(hashSize);p.First08.Write(first);
   for(int n=0;n<hashSize;n++)first[n].Write(UInt16.MaxValue);for(int n=0;n<capacity;n++)next[n].Write(UInt16.MaxValue);
   return p; //Nodes themselves deliberately remain unknown, unlike the tables.
  }
  //25F910 allocates(capacity+1) pointers while recording capacity/count at8/C.
  // The recovered helper does not enforce capacity before pushing. An access
  // outside this actual allocated extent is an OWN stop after its write prefix.
  public static NavigationNodeQueue CreateQueue(int capacity){
   if(capacity<1||capacity>OwnMaximumNodes)throw new ArgumentOutOfRangeException("Own positive finite heap allocation domain");
   var q=new NavigationNodeQueue();q.Heap00.Write(null);q.Capacity08.Write(capacity);q.Count0C.Write(0);var cells=new Field<NavigationSearchNode>[capacity+1];for(int n=0;n<cells.Length;n++)cells[n]=new Field<NavigationSearchNode>();q.Heap00.Write(cells);return q;
  }
  public static void ClearPool(NavigationNodePool pool,uint hashSizeAt,uint headsAt,uint countAt){
   Need(pool,hashSizeAt);int size=pool.HashSize1C.Read(hashSizeAt);var heads=pool.First08.Read(headsAt);if(size<0)throw new MissingContract(hashSizeAt,"Own negative memset extent unsupported");
   for(int n=0;n<size;n++)At(heads,n,headsAt).Write(UInt16.MaxValue);pool.RequireLive(countAt);pool.Count20.Write(0);
   //Neither Nodes00 nor Next10 is cleared by261D51..261D76/25F941..25F958.
  }
  public static NavigationSearchNode NodeAt(NavigationNodePool pool,int index,uint at){Need(pool,at);return At(pool.Nodes00.Read(at),index,at);}
  public static int IndexOf(NavigationNodePool pool,NavigationSearchNode node,uint at){
   Need(pool,at);Need(node,at);var array=Need(pool.Nodes00.Read(at),at);int found=-1;
   for(int n=0;n<array.Length;n++)if(Object.ReferenceEquals(array[n],node)){if(found!=-1)throw new MissingContract(at,"One node identity has multiple declared addresses");found=n;}
   if(found<0)throw new MissingContract(at,"Node is not in the currently loaded pool base");return found;
  }
  public static NavigationSearchNode GetNode26DCA0(NavigationNodePool pool,uint reference,byte state){
   Need(pool,0x26dce3);uint key=unchecked(~(reference<<15)+reference);key=unchecked(((key>>10)^key)*9);key^=key>>6;key=unchecked(key+~(key<<11));
   uint mask=unchecked((uint)(pool.HashSize1C.Read(0x26dce3)-1));uint bucket=((key>>16)^key)&mask;
   var heads=pool.First08.Read(0x26dcf2);ushort index=At(heads,bucket,0x26dcf8).Read(0x26dcf8);
   if(index!=UInt16.MaxValue){var captured=pool.Nodes00.Read(0x26dd02);int steps=0;do{
    Budget(++steps,0x26dd10);var node=At(captured,index,0x26dd17);if(node.Reference18.Read(0x26dd17)==reference&&((node.Packed14.ReadMasked(0x03000000,0x26dd1e)>>24)&3)==state)return node;
    var next=pool.Next10.Read(0x26dd2a);index=At(next,index,0x26dd2e).Read(0x26dd2e);
   }while(index!=UInt16.MaxValue);}
   int count=pool.Count20.Read(0x26dd38);if(count>=pool.MaxNodes18.Read(0x26dd3c))return null;
   ushort slot=unchecked((ushort)pool.Count20.Read(0x26dd5e));pool.Count20.Write(unchecked(count+1));var fresh=NodeAt(pool,slot,0x26dd7d);
   fresh.Cost0C.Write(0);fresh.Total10.Write(0); //one native QWORD store26DD80.
   fresh.RequireLive(0x26dd84);var packed=fresh.Packed14.Snapshot();fresh.Reference18.Write(reference);packed.Transform(0xe0000000,(uint)(state&3)<<24);fresh.Packed14.CopyFrom(packed);
   heads=pool.First08.Read(0x26dd96);var links=pool.Next10.Read(0x26dd9a);ushort previous=At(heads,bucket,0x26dd9e).Read(0x26dd9e);At(links,slot,0x26dda2).Write(previous);heads=pool.First08.Read(0x26dda7);At(heads,bucket,0x26ddab).Write(slot);return fresh;
  }
  static Field<NavigationSearchNode> QueueCell(NavigationNodeQueue queue,int index,uint baseAt,uint cellAt){Need(queue,baseAt);return At(queue.Heap00.Read(baseAt),index,cellAt);}
  static float Total(NavigationSearchNode node,uint at){return Scalar(Need(node,at).Total10.Read(at),at);}
  public static void Push25F680(NavigationNodeQueue queue,NavigationSearchNode node){
   Need(queue,0x25f680);queue.Count0C.Write(unchecked(queue.Count0C.Read(0x25f680)+1));int index=unchecked(queue.Count0C.Read(0x25f686)-1),parent=unchecked(index-1)/2;
   if(index>0){float total=Total(node,0x25f69e);int steps=0;do{
    Budget(++steps,0x25f6b0);var array=queue.Heap00.Read(0x25f6b0);var p=At(array,parent,0x25f6b6).Read(0x25f6b6);if(total>=Total(p,0x25f6ba))break; //JNC; NaN bubbles upward.
    At(array,index,0x25f6c9).Write(p);index=parent;parent=unchecked(parent-1)/2;
   }while(index>0);}
   QueueCell(queue,index,0x25f6d7,0x25f6dd).Write(node);
  }
  public static void TrickleDown26DDC0(NavigationNodeQueue queue,NavigationSearchNode node){TrickleDown26DDC0(queue,Field<NavigationSearchNode>.Of(node));}
  //261CB0 caller passes a saved pointer. This overload permits a pure unknown
  // MOV to remain unknown if count<=1 needs no dereference before the store.
  public static void TrickleDown26DDC0(NavigationNodeQueue queue,Field<NavigationSearchNode> replacement){
   Need(queue,0x26ddc2);Need(replacement,0);var saved=replacement.Snapshot();int count=queue.Count0C.Read(0x26ddc2),hole=0,child=1,steps=0;
   if(count>1){do{
    Budget(++steps,0x26dde4);if(unchecked(child+1)<count){var array=queue.Heap00.Read(0x26dded);var left=At(array,child,0x26ddf0).Read(0x26ddf0);var right=At(array,unchecked(child+1),0x26ddf4).Read(0x26ddf4);float rightTotal=Total(right,0x26ddf9);if(!(rightTotal>=Total(left,0x26ddfe)))child++;} //NaN selects right; equal chooses left.
    var current=queue.Heap00.Read(0x26de0a);var moved=At(current,child,0x26de18).Snapshot();At(current,hole,0x26de1c).CopyFrom(moved);hole=child;child=unchecked(child*2+1);count=queue.Count0C.Read(0x26de23);
   }while(child<count);}
   int parent=unchecked(hole-1)/2;
   if(hole>0){float total=Total(saved.Read(0x26de47),0x26de47);steps=0;do{
    Budget(++steps,0x26de50);var array=queue.Heap00.Read(0x26de50);var p=At(array,parent,0x26de56).Read(0x26de56);if(total>=Total(p,0x26de5a))break;
    At(array,hole,0x26de69).Write(p);hole=parent;parent=unchecked(parent-1)/2;
   }while(hole>0);}
   QueueCell(queue,hole,0x26de77,0x26de7d).CopyFrom(saved);
  }
  public static void Modify25F6F0(NavigationNodeQueue queue,NavigationSearchNode node){
   Need(queue,0x25f6f6);int count=queue.Count0C.Read(0x25f6f6);if(count<=0)return;var captured=queue.Heap00.Read(0x25f701);int index=0;
   for(;index<count;index++){Budget(index+1,0x25f707);if(Object.ReferenceEquals(At(captured,index,0x25f707).Read(0x25f707),node))break;}if(index==count)return;
   int parent=unchecked(index-1)/2;
   if(index>0){float total=Total(node,0x25f72a);int steps=0;do{
    Budget(++steps,0x25f730);var array=queue.Heap00.Read(0x25f730);var p=At(array,parent,0x25f736).Read(0x25f736);if(total>=Total(p,0x25f73a))break;
    At(array,index,0x25f749).Write(p);index=parent;parent=unchecked(parent-1)/2;
   }while(index>0);}
   QueueCell(queue,index,0x25f757,0x25f75d).Write(node);
  }
  // Arithmetic is scalar binary32, with a rounding boundary after each native
  // ADDSS/SUBSS/MULSS. No FMA or unobserved MXCSR/DAZ/FTZ behavior is assumed.
  static float Scalar(float value,uint at){uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(value),0);if((bits&0x7f800000U)==0&&(bits&0x7fffffU)!=0)throw new MissingContract(at,"Own floating domain excludes subnormal input; native MXCSR unobserved");return value;}
  static float Round(double value,uint at){if(value!=0&&!Double.IsNaN(value)&&!Double.IsInfinity(value)&&Math.Abs(value)<1.1754943508222875E-38)throw new MissingContract(at,"Own floating domain excludes a subnormal intermediate");return Scalar(BitConverter.ToSingle(BitConverter.GetBytes((float)value),0),at);}
  static float Add(float a,float b,uint at){return Round((double)a+b,at);} static float Sub(float a,float b,uint at){return Round((double)a-b,at);} static float Mul(float a,float b,uint at){return Round((double)a*b,at);}
  // A public point property may itself resolve a deferred OWN address. Its
  // extent/lifetime stop is attributed to this access, not a native exception.
  static Field<float> PointCell(NavigationPoint3 p,int part,uint at){Need(p,at);try{return part==0?p.X:part==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,"Own point address resolution: "+e.Message);}}
  static float ReadPoint(NavigationPoint3 p,int part,uint at){return Scalar(PointCell(p,part,at).Read(at),at);}
  static float Vertex(NavigationPoint3[] vertices,int index,int part,uint at){return ReadPoint(At(vertices,index,at),part,at);}
  static void CopyVertex(NavigationPoint3[] vertices,int index,int part,NavigationPoint3 output,uint loadAt,uint storeAt){var bits=PointCell(At(vertices,index,loadAt),part,loadAt).Snapshot();PointCell(output,part,storeAt).CopyFrom(bits);}
  static void WritePoint(NavigationPoint3 p,int part,float value,uint at){PointCell(p,part,at).Write(value);}
  // The only consumer creates two distinct local endpoint records. Keeping
  // this helper private avoids promising arbitrary native source/output
  // overlap or the offmesh objects not produced by our cold mesh adapter.
  static uint PortalEndpoints262F10(NavigationMeshTilesState mesh,uint currentReference,NavigationPoly currentPoly,NavigationTile currentTile,uint neighborReference,NavigationPoly neighborPoly,NavigationTile neighborTile,NavigationPoint3 left,NavigationPoint3 right,Field<float> portalScale){
   Need(mesh,0x262f1f);uint first=mesh.Poly(currentPoly,0x262f1f).FirstLink00.Read(0x262f1f);if(first==UInt32.MaxValue)return 0x80000008;
   var links=mesh.Tile(currentTile,0x262f31).Links20.Read(0x262f31);uint index=first;NavigationMeshLink12 link;int steps=0;
   for(;;){Budget(++steps,0x262f40);link=At(links,index,0x262f44);if(link.Reference00.Read(0x262f44)==neighborReference)break;index=link.Next04.Read(0x262f4a);if(index==UInt32.MaxValue)return 0x80000008;}
   byte currentType=mesh.Spatial.Poly(currentPoly,0x262f72).Type1F.Read(0x262f72);
   if((currentType&0xc0)==0x40)throw new MissingContract(0x262f81,"Own cold mesh domain excludes offmesh portal objects; native branch not substituted");
   byte nextType=mesh.Spatial.Poly(neighborPoly,0x263013).Type1F.Read(0x263013);
   if((nextType&0xc0)==0x40)throw new MissingContract(0x26301e,"Own cold mesh domain excludes neighbor offmesh portal objects; native branch not substituted");
   byte edge=Need(link,0x263069).Edge08.Read(0x263069);int count=Need(currentPoly,0x263070).VertexCount1E.Read(0x263070);
   int a=At(currentPoly.Indices04,edge,0x26307e).Read(0x26307e);if(count==0)throw new MissingContract(0x263086,"Own valid polygon domain excludes division by zero; not a native status result");
   int edgeNext=(edge+1)%count;bool internalSide=link.Side09.Read(0x263088)==0xff;var vertices=Need(currentTile,0x263090).Vertices18.Read(0x263090);int b=At(currentPoly.Indices04,edgeNext,0x263094).Read(0x263094);
   CopyVertex(vertices,a,0,left,0x2630a0,0x2630a3);CopyVertex(vertices,a,1,left,0x2630a6,0x2630ab);CopyVertex(vertices,a,2,left,0x2630af,0x2630b4);
   CopyVertex(vertices,b,0,right,0x2630bd,0x2630c0);CopyVertex(vertices,b,1,right,0x2630c3,0x2630c7);CopyVertex(vertices,b,2,right,0x2630cb,0x2630cf);
   if(internalSide)return 0x40000000;byte min=link.Min0A.Read(0x2630d9);if(min==0&&link.Max0B.Read(0x2630e2)==0xff)return 0x40000000;
   float dx=Vertex(vertices,b,0,0x2630ed);dx=Sub(dx,Vertex(vertices,a,0,0x2630f2),0x2630f2);
   float dy=Vertex(vertices,b,1,0x2630f8);dy=Sub(dy,Vertex(vertices,a,1,0x2630fe),0x2630fe);
   float dz=Vertex(vertices,b,2,0x263105);dz=Sub(dz,Vertex(vertices,a,2,0x26310b),0x26310b);
   byte max=link.Max0B.Read(0x263119);float low=Mul(min,Scalar(Need(portalScale,0x263125).Read(0x263125),0x263125),0x263125);
   dx=Mul(dx,low,0x263130);float high=Mul(max,Scalar(portalScale.Read(0x263134),0x263134),0x263134);
   dx=Add(dx,Vertex(vertices,a,0,0x26313c),0x26313c);dy=Mul(dy,low,0x263142);dz=Mul(dz,low,0x263146);dy=Add(dy,Vertex(vertices,a,1,0x26314a),0x26314a);
   WritePoint(left,0,dx,0x263151);dz=Add(dz,Vertex(vertices,a,2,0x263156),0x263156);dx=Vertex(vertices,b,0,0x26315d);dx=Sub(dx,Vertex(vertices,a,0,0x263162),0x263162);
   WritePoint(left,1,dy,0x263168);dy=Vertex(vertices,b,1,0x26316e);dy=Sub(dy,Vertex(vertices,a,1,0x263174),0x263174);WritePoint(left,2,dz,0x26317b);
   dz=Vertex(vertices,b,2,0x263181);dz=Sub(dz,Vertex(vertices,a,2,0x263187),0x263187);dx=Mul(dx,high,0x26318e);dy=Mul(dy,high,0x263192);dx=Add(dx,Vertex(vertices,a,0,0x263196),0x263196);dz=Mul(dz,high,0x26319c);dy=Add(dy,Vertex(vertices,a,1,0x2631a0),0x2631a0);dz=Add(dz,Vertex(vertices,a,2,0x2631a7),0x2631a7);
   WritePoint(right,0,dx,0x2631ae);WritePoint(right,1,dy,0x2631b3);WritePoint(right,2,dz,0x2631b9);return 0x40000000;
  }
  // Small reuse surface for262D30. It calls the same body as Entry2631E0 and
  // writes the caller-held independent locals directly, including partial
  // writes before MissingContract. The existing Entry path is unchanged.
  public static uint FillPortalEndpoints262F10(NavigationMeshTilesState mesh,uint currentReference,NavigationPoly currentPoly,NavigationTile currentTile,uint neighborReference,NavigationPoly neighborPoly,NavigationTile neighborTile,NavigationPortalEndpoints outputs,Field<float> portalScale){
   Need(outputs,0);return PortalEndpoints262F10(mesh,currentReference,currentPoly,currentTile,neighborReference,neighborPoly,neighborTile,outputs.Left,outputs.Right,portalScale);
  }
  // Source: r1-corridor-portal-helper, plus2631E0 in the cross-poly batch.
  // Constants are explicit caller-owned observations, never default globals:
  // half34BADC=3F000000 and scale34B9CC=3B808081 (saved raw-byte evidence).
  public static uint Entry2631E0(NavigationMeshTilesState mesh,uint currentReference,NavigationPoly currentPoly,NavigationTile currentTile,uint neighborReference,NavigationPoly neighborPoly,NavigationTile neighborTile,NavigationPoint3 output,Field<float> halfConstant,Field<float> portalScale){
   var left=new NavigationPoint3();var right=new NavigationPoint3();uint status=PortalEndpoints262F10(mesh,currentReference,currentPoly,currentTile,neighborReference,neighborPoly,neighborTile,left,right,portalScale);if((status&0x80000000)!=0)return 0x80000008;
   float half=Scalar(Need(halfConstant,0x26323b).Read(0x26323b),0x26323b);
   float x=ReadPoint(right,0,0x263243);x=Add(x,ReadPoint(left,0,0x263249),0x263249);float z=ReadPoint(right,2,0x26324f);z=Add(z,ReadPoint(left,2,0x263255),0x263255);x=Mul(x,half,0x26325b);z=Mul(z,half,0x26325f);
   WritePoint(output,0,x,0x263263);float y=ReadPoint(right,1,0x263267);y=Add(y,ReadPoint(left,1,0x26326d),0x26326d);WritePoint(output,2,z,0x263273);y=Mul(y,half,0x263278);WritePoint(output,1,y,0x26327c);return 0x40000000;
  }
 }
}
