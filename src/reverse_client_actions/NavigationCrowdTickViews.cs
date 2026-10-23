using System;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN typed aliases only. These views do not execute25C440, callbacks, floor,
 // or movement. They never initialize an agent or replace its existing cells.
 // Engine25C440 assembly:6C is read/written as binary32 at25D307..25D37B;
 // D0/D4/D8 are binary32 corrections at25DCFD..25DD73/25DE07..25DE38;
 // DC is binary32 at25E20E and E0 is a signed DWORD counter at25E218..25E26B.
 public sealed class NavigationCrowdFloatBitsView {
  public readonly NavigationCrowdAgent Agent;
  public readonly Field<uint> Raw;
  internal NavigationCrowdFloatBitsView(NavigationCrowdAgent agent,Field<uint> raw){Agent=agent;Raw=raw;}
  public uint ReadBits(uint at){Agent.RequireLive(at);return Raw.Read(at);}
  public float Read(uint at){return BitConverter.ToSingle(BitConverter.GetBytes(ReadBits(at)),0);}
  public void WriteBits(uint bits,uint at){Agent.RequireLive(at);Raw.Write(bits);}
  public void Write(float value,uint at){WriteBits(BitConverter.ToUInt32(BitConverter.GetBytes(value),0),at);}
  // MOV-like opaque DWORD copy. Unknown source remains Unknown, without first
  // converting it to float, guessing zero, or reading the old destination.
  public void CopyBitsFrom(Field<uint> source,uint at){Agent.RequireLive(at);if(source==null)throw new ArgumentNullException("source");Raw.CopyFrom(source);}
  public Field<uint> SnapshotBits(uint at){Agent.RequireLive(at);return Raw.Snapshot();}
 }

 // WordDC is a frozen old Field<ulong> with ONE knowledge bit. A partial DWORD
 // store into an unknown whole word cannot be represented faithfully by that
 // interface. Restrict lane writes to a known whole word and stop explicitly
 // otherwise; do not introduce shadow cells or mark the sibling lane known.
 // This is an OWN representation limit, not an extra native read dependency.
 public sealed class NavigationCrowdPackedWordDCView {
  public readonly NavigationCrowdAgent Agent;
  public readonly Field<ulong> Raw;
  internal NavigationCrowdPackedWordDCView(NavigationCrowdAgent agent){Agent=agent;Raw=agent.WordDC;}
  public uint ReadSpeedBitsDC(uint at){Agent.RequireLive(at);return unchecked((uint)Raw.Read(at));}
  public float ReadSpeedDC(uint at){return BitConverter.ToSingle(BitConverter.GetBytes(ReadSpeedBitsDC(at)),0);}
  public int ReadElapsedE0(uint at){Agent.RequireLive(at);return unchecked((int)(Raw.Read(at)>>32));}
  ulong PreserveSibling(uint at){
   Agent.RequireLive(at);
   try{return Raw.Read(at);}
   catch(MissingContract e){throw new MissingContract(at,"OWN packed WordDC cannot represent a partial DWORD store with an unknown sibling; no write performed",e);}
  }
  public void WriteSpeedBitsDC(uint bits,uint at){ulong word=PreserveSibling(at);Raw.Write((word&0xffffffff00000000UL)|bits);}
  public void WriteSpeedDC(float value,uint at){WriteSpeedBitsDC(BitConverter.ToUInt32(BitConverter.GetBytes(value),0),at);}
  public void WriteElapsedE0(int value,uint at){ulong word=PreserveSibling(at);Raw.Write((word&0xffffffffUL)|((ulong)unchecked((uint)value)<<32));}
  public Field<ulong> SnapshotWord(uint at){Agent.RequireLive(at);return Raw.Snapshot();}
  public void CopyWholeWordFrom(Field<ulong> source,uint at){Agent.RequireLive(at);if(source==null)throw new ArgumentNullException("source");Raw.CopyFrom(source);}
 }

 // The actual CALL25E087 borrows Agent+48 as CrowdAgentData, not a copied Vec3.
 // Offsets00/0C/18 map to Agent.Position48/Heading54/Vector60 respectively.
 // Game066F75..066F88 reads data+18/+20, so its rotation uses Vector60.
 // Forming a view is LEA-like: no data/lifetime check until a use operation.
 public sealed class NavigationCrowdAgentDataView {
  public const uint AgentOffset=0x48;
  public readonly NavigationCrowdAgent Agent;
  public NavigationCrowdAgentDataView(NavigationCrowdAgent agent){if(agent==null)throw new ArgumentNullException("agent");Agent=agent;}
  public bool SameAddress(NavigationCrowdAgentDataView other){return other!=null&&Object.ReferenceEquals(Agent,other.Agent);}
  public void RequireLive(uint at){Agent.RequireLive(at);}
  // These are the original point objects. Consumers that retain them must
  // still check this owner at their actual dereference; a BCL Field is not a
  // native checked pointer. No source/lifetime is inferred from a float value.
  public NavigationPoint3 BorrowPoint(uint offset,uint at){
   RequireLive(at);NavigationPoint3 point;
   if(offset==0)point=Agent.Position48;
   else if(offset==0xc)point=Agent.Heading54;
   else if(offset==0x18)point=Agent.Vector60;
   else throw new MissingContract(at,"Outside declared CrowdAgentData three-point view");
   point.RequireLive(at);return point;
  }
  public Field<float> Cell(uint offset,uint at){
   RequireLive(at);if(offset>0x20||(offset&3)!=0)throw new MissingContract(at,"Outside declared CrowdAgentData DWORD cell view");
   uint pointOffset=offset<0xc?0u:offset<0x18?0xcu:0x18u;
   var point=BorrowPoint(pointOffset,at);uint axis=(offset-pointOffset)/4;
   return axis==0?point.X:axis==1?point.Y:point.Z;
  }
  public float Read(uint offset,uint at){return Cell(offset,at).Read(at);}
  public void Write(uint offset,float value,uint at){Cell(offset,at).Write(value);}
  public Field<float> Snapshot(uint offset,uint at){return Cell(offset,at).Snapshot();}
  public void CopyFrom(uint offset,Field<float> source,uint at){var destination=Cell(offset,at);if(source==null)throw new ArgumentNullException("source");destination.CopyFrom(source);}
 }

 public sealed class NavigationCrowdTickStateView {
  public readonly NavigationCrowdAgent Agent;
  public readonly NavigationCrowdFloatBitsView Speed6C,CorrectionD0,CorrectionD4,CorrectionD8;
  public readonly NavigationCrowdPackedWordDCView SpeedAndElapsed;
  public readonly NavigationCrowdAgentDataView Data48;
  // Native neighbor header88/90/98 is currently frozen as raw ulong fields.
  // Expose the authoritative cells; do NOT build a separate PointerVector or
  // guess raw addresses. Group.Work40 likewise needs a borrowed-header adapter
  // before existing typed vector/proximity algorithms can consume it directly.
  public readonly Field<ulong> NeighborBegin88,NeighborEnd90,NeighborCapacity98;
  public NavigationCrowdTickStateView(NavigationCrowdAgent agent){
   if(agent==null)throw new ArgumentNullException("agent");Agent=agent;
   Speed6C=new NavigationCrowdFloatBitsView(agent,agent.Timer6C);
   CorrectionD0=new NavigationCrowdFloatBitsView(agent,agent.GapD0);
   CorrectionD4=new NavigationCrowdFloatBitsView(agent,agent.GapD4);
   CorrectionD8=new NavigationCrowdFloatBitsView(agent,agent.GapD8);
   SpeedAndElapsed=new NavigationCrowdPackedWordDCView(agent);Data48=new NavigationCrowdAgentDataView(agent);
   NeighborBegin88=agent.Words88B0[0];NeighborEnd90=agent.Words88B0[1];NeighborCapacity98=agent.Words88B0[2];
  }
 }
}
