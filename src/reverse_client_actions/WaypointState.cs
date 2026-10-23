using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 // Typed knowledge of the observed 80-byte waypoint, not a native ABI object.
 // Padding is copied without becoming zero. Whole-record aliasing is supported;
 // arbitrary byte-overlapping records and native heap corruption are outside.
 public sealed class Waypoint80 {
  public readonly Field<uint> Kind00=new Field<uint>(),Padding04=new Field<uint>(),Padding44=new Field<uint>();
  public readonly WorldPosition Position08=new WorldPosition();
  public readonly Field<float>[] Matrix20=new Field<float>[9];
  public readonly Field<object> Auxiliary48=new Field<object>();
  public Waypoint80(){for(int n=0;n<9;n++)Matrix20[n]=new Field<float>();}
  public void CopyChunkFrom(Waypoint80 s,int chunk){
   if(s==null)throw new MissingContract(0x08f536,"Null waypoint source");
   // Each chunk snapshots its entire 16-byte load before any destination store.
   if(chunk==0){var k=s.Kind00.Snapshot();var p=s.Padding04.Snapshot();var q=s.Position08.Q0.Snapshot();Kind00.CopyFrom(k);Padding04.CopyFrom(p);Position08.Q0.CopyFrom(q);}
   else if(chunk==1){var x=s.Position08.Q8.Snapshot();var y=s.Position08.Q16.Snapshot();Position08.Q8.CopyFrom(x);Position08.Q16.CopyFrom(y);}
   else if(chunk==2||chunk==3){int start=(chunk-2)*4;var cells=new Field<float>[4];for(int n=0;n<4;n++)cells[n]=s.Matrix20[start+n].Snapshot();for(int n=0;n<4;n++)Matrix20[start+n].CopyFrom(cells[n]);}
   else if(chunk==4){var x=s.Matrix20[8].Snapshot();var p=s.Padding44.Snapshot();var a=s.Auxiliary48.Snapshot();Matrix20[8].CopyFrom(x);Padding44.CopyFrom(p);Auxiliary48.CopyFrom(a);}
   else throw new ArgumentOutOfRangeException("chunk");
  }
 }
 public sealed class WaypointMap {
  public readonly ulong Slots;
  private readonly Dictionary<ulong,Field<Waypoint80>> cells=new Dictionary<ulong,Field<Waypoint80>>();
  public bool Released;
  // An explicit own allocation size, not evidence that any native cell is zero.
  public WaypointMap(ulong slots){Slots=slots;}
  public Field<Waypoint80> Cell(ulong index,uint at){
   if(Released||index>=Slots)throw new MissingContract(at,"Released/out-of-range own map");
   Field<Waypoint80> f;if(!cells.TryGetValue(index,out f)){f=new Field<Waypoint80>();cells.Add(index,f);}return f;
  }
 }
 public sealed class WaypointProxy { public readonly Field<WaypointQueue> Container00=new Field<WaypointQueue>(); }
 public sealed class WaypointIterator {
  public readonly Field<WaypointProxy> Proxy00=new Field<WaypointProxy>();
  public readonly Field<ulong> Link08=new Field<ulong>();
  public readonly Field<ulong> Offset10=new Field<ulong>();
 }
 public sealed class WaypointQueue {
  public readonly Field<WaypointProxy> Proxy00=new Field<WaypointProxy>();
  public readonly Field<WaypointMap> Map08=new Field<WaypointMap>();
  public readonly Field<ulong> MapSize10=new Field<ulong>(),Offset18=new Field<ulong>(),Count20=new Field<ulong>();
 }
 // Extends the same sealed manager identity without duplicating its mutable fields.
 public sealed class WaypointManagerView {
  public readonly MovementManager Identity;
  public readonly WorldPosition Position08=new WorldPosition();
  public readonly WaypointQueue Queue40=new WaypointQueue();
  public WaypointManagerView(MovementManager manager){if(manager==null)throw new ArgumentNullException("manager");Identity=manager;}
 }
}
