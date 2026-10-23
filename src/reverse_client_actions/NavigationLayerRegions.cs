using System;

namespace GrimDynasty.ReverseClient.Actions {
 public enum NavigationLayerRegionOutcome { Success, NativeFailure, OwnUnsupported }

 public sealed class NavigationLayerRegionResult {
  public readonly NavigationLayerData Layer;
  public readonly ArraySegment<byte> Regions;
  public readonly NavigationLayerRegionOutcome Outcome;
  public readonly uint? NativeStatus;
  public readonly string Detail;
  public readonly int RawRegionCount,CellIndex;
  public readonly bool RegionCountWritten;
  internal NavigationLayerRegionResult(NavigationLayerData layer,NavigationLayerRegionOutcome outcome,uint? status,string detail,int raw,int cell,bool written){
   Layer=layer;Regions=layer.Plane(3);Outcome=outcome;NativeStatus=status;Detail=detail;RawRegionCount=raw;CellIndex=cell;RegionCountWritten=written;
  }
 }

 // Engine 268CF0, saved Rea 16 pseudocode / 17 assembly / 18 callees in
 // docs/reverse-client/r1-tile-production-direct-stages. Assembly SHA256:
 // 209fb4fb986d0a1a9cabd047ff57c63183ef927769d86b772fe0179af347e73c.
 // Engine SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Four byte-plane views and RegionCount belong to the SAME mutable layer.
 // Managed scratch allocation is an own platform adapter, not native allocator
 // ABI/free callbacks. Managed allocation failures throw; they are NOT reported
 // as the native allocator-null status 80000004. Empty managed arrays are valid.
 // Stable input during this synchronous call is required. This is only region
 // production: no contours, polygon mesh, tile publication, or navigation search.
 public static class NavigationLayerRegions {
  public const uint SuccessStatus=0x40000000u,RegionIdExhaustedStatus=0x80000010u;
  const byte NoRegion=255;
  const int RecordSize=24,NeighborCapacity=16;
  struct Sweep { public ushort Samples; public byte Id,Neighbor; }

  static int Count(byte[] records,int record){
   int p=record*RecordSize;
   return records[p]|(records[p+1]<<8)|(records[p+2]<<16)|(records[p+3]<<24);
  }
  static void IncrementCount(byte[] records,int record){
   int p=record*RecordSize,value=Count(records,record)+1;
   for(int b=0;b<4;b++)records[p+b]=(byte)(value>>(b*8));
  }
  static bool Connected(byte[] bytes,int heightOffset,int areaOffset,int a,int b,int climb){
   return bytes[areaOffset+a]==bytes[areaOffset+b]&&Math.Abs((int)bytes[heightOffset+a]-bytes[heightOffset+b])<=climb;
  }
  static bool AppendNeighbor(byte[] records,int record,byte neighbor){
   int p=record*RecordSize,n=records[p+20];
   // 2690B9..269102 deduplicates only the LAST entry, not the whole list.
   if(n!=0&&records[p+4+n-1]==neighbor)return true;
   // Native has no bounds check here: entry 16 overwrites its own count field.
   // Reject BEFORE that unsafe write. This is an OWN domain limit, not an
   // invented native status or an assumption that every input fits 16 entries.
   if(n>=NeighborCapacity)return false;
   records[p+4+n]=neighbor;records[p+20]=(byte)(n+1);return true;
  }
  static NavigationLayerRegionResult Result(NavigationLayerData layer,NavigationLayerRegionOutcome outcome,uint? status,string detail,int raw,int cell,bool written){
   return new NavigationLayerRegionResult(layer,outcome,status,detail,raw,cell,written);
  }

  // climb is supplied explicitly. No guessed header/global default or floating
  // point conversion is hidden here; 268CF0 receives this signed integer.
  public static NavigationLayerRegionResult Build(NavigationLayerData layer,int climb){
   if(layer==null)throw new ArgumentNullException("layer");
   byte[] bytes=layer.WorkData.Array;
   int width=layer.Header.Array[layer.Header.Offset+48],height=layer.Header.Array[layer.Header.Offset+49];
   // Native rereads dimensions from the live header. Our decoder's owned plane
   // extents were fixed at allocation; changing dimensions is not silently
   // interpreted using new strides or an implicit reallocation.
   if(width!=layer.Width||height!=layer.Height)
    return Result(layer,NavigationLayerRegionOutcome.OwnUnsupported,null,"HeaderDimensionsChanged",0,-1,false);
   int cells=width*height,heights=layer.Plane(0).Offset,areas=layer.Plane(1).Offset,regions=layer.Plane(3).Offset;
   // 268D45..268D49 occurs before native scratch allocation and any failure.
   for(int i=0;i<cells;i++)bytes[regions+i]=NoRegion;
   Sweep[] sweeps=new Sweep[width];
   byte[] previous=new byte[256]; // Native stack scratch, byte counters.
   int nextRegion=0;
   for(int y=0;y<height;y++){
    Array.Clear(previous,0,nextRegion);
    int sweepCount=0,row=y*width;
    for(int x=0;x<width;x++){
     int cell=row+x;
     if(bytes[areas+cell]==0)continue;
     int sweep;
     if(x>0&&Connected(bytes,heights,areas,cell,cell-1,climb)&&bytes[regions+cell-1]!=NoRegion)
      sweep=bytes[regions+cell-1];
     else{
      sweep=sweepCount++;sweeps[sweep].Samples=0;sweeps[sweep].Neighbor=NoRegion;
     }
     if(y>0&&Connected(bytes,heights,areas,cell,cell-width,climb)){
      byte above=bytes[regions+cell-width];
      if(above!=NoRegion){
       if(sweeps[sweep].Samples==0)sweeps[sweep].Neighbor=above;
       if(sweeps[sweep].Neighbor==above){
        sweeps[sweep].Samples=unchecked((ushort)(sweeps[sweep].Samples+1));
        previous[above]=unchecked((byte)(previous[above]+1));
       }else sweeps[sweep].Neighbor=NoRegion;
      }
     }
     bytes[regions+cell]=(byte)sweep;
    }
    for(int s=0;s<sweepCount;s++){
     byte neighbor=sweeps[s].Neighbor;
     if(neighbor!=NoRegion&&(ushort)previous[neighbor]==sweeps[s].Samples)sweeps[s].Id=neighbor;
     else{
      // 268F12..268F83 returns before remapping THIS row. Prior rows contain
      // raw global IDs, this row local sweep IDs, following rows remain FF.
      if(nextRegion==255)return Result(layer,NavigationLayerRegionOutcome.NativeFailure,RegionIdExhaustedStatus,"RegionIdsExhaustedBeforeRowRemap",nextRegion,row,false);
      sweeps[s].Id=(byte)nextRegion++;
     }
    }
    for(int x=0;x<width;x++){
     int cell=row+x;byte sweep=bytes[regions+cell];
     if(sweep!=NoRegion)bytes[regions+cell]=sweeps[sweep].Id;
    }
   }

   byte[] records=new byte[nextRegion*RecordSize];
   for(int r=0;r<nextRegion;r++)records[r*RecordSize+21]=NoRegion;
   for(int y=0;y<height;y++)for(int x=0;x<width;x++){
    int cell=y*width+x;byte r=bytes[regions+cell];
    if(r==NoRegion)continue;
    IncrementCount(records,r);records[r*RecordSize+22]=bytes[areas+cell];
    if(y>0&&Connected(bytes,heights,areas,cell,cell-width,climb)){
     byte above=bytes[regions+cell-width];
     if(above!=NoRegion&&above!=r){
      // Keep order: current->above is appended BEFORE above->current. An own
      // safety stop preserves all plane writes and the old layer.RegionCount.
      if(!AppendNeighbor(records,r,above))return Result(layer,NavigationLayerRegionOutcome.OwnUnsupported,null,"CurrentRegionNeighborCapacity",nextRegion,cell,false);
      if(!AppendNeighbor(records,above,r))return Result(layer,NavigationLayerRegionOutcome.OwnUnsupported,null,"PreviousRegionNeighborCapacity",nextRegion,cell,false);
     }
    }
   }
   for(int r=0;r<nextRegion;r++)records[r*RecordSize+21]=(byte)r;
   for(int r=0;r<nextRegion;r++){
    int p=r*RecordSize,best=-1,bestSize=0;
    byte ownGroup=records[p+21];
    for(int n=0;n<records[p+20];n++){
     int neighbor=records[p+4+n],q=neighbor*RecordSize;byte group=records[q+21];
     int size=Count(records,neighbor);
     if(ownGroup==group||records[p+22]!=records[q+22]||size<=bestSize)continue;
     int connections=0;
     for(int j=0;j<nextRegion;j++){
      int t=j*RecordSize;if(records[t+21]!=ownGroup)continue;
      for(int k=0;k<records[t+20];k++)if(records[records[t+4+k]*RecordSize+21]==group)connections++;
     }
     // Use the neighbor RECORD's cell count, not a summed group count.
     // Strictly larger wins; equal-size candidates keep the earlier neighbor.
     if(connections==1){best=neighbor;bestSize=size;}
    }
    if(best>=0){
     byte target=records[best*RecordSize+21];
     for(int j=0;j<nextRegion;j++)if(records[j*RecordSize+21]==ownGroup)records[j*RecordSize+21]=target;
    }
   }
   Array.Clear(previous,0,256);
   for(int r=0;r<nextRegion;r++)previous[records[r*RecordSize+21]]=1;
   int dense=0;
   for(int r=0;r<256;r++)if(previous[r]!=0)previous[r]=(byte)dense++;
   for(int r=0;r<nextRegion;r++)records[r*RecordSize+21]=previous[records[r*RecordSize+21]];
   layer.RegionCount=(byte)dense; // 269368 publishes count before final plane remap.
   for(int cell=0;cell<cells;cell++){
    byte r=bytes[regions+cell];if(r!=NoRegion)bytes[regions+cell]=records[r*RecordSize+21];
   }
   return Result(layer,NavigationLayerRegionOutcome.Success,SuccessStatus,"CompletedWithManagedScratch",nextRegion,-1,true);
  }
 }
}
