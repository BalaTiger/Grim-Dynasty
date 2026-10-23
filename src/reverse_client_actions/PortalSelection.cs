using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Unresolved imports used by Game08DF30. Overrides supply observations or
 // adapters; neither a portal nor a vector difference has a default result.
 public class PortalSelectionBoundaries:MoveManagerBoundaries {
  public PortalSelectionBoundaries(MoveContext context):base(context){}
  public virtual int PortalCount609F10(object region){throw Missing(0x609f10,"Region portal count");}
  public virtual object PortalAt609F18(object region,int index){throw Missing(0x609f18,"Region portal at index");}
  public virtual byte PortalOpen609F20(object portal){throw Missing(0x609f20,"Portal open state");}
  public virtual object PortalRegion609F28(object portal){throw Missing(0x609f28,"Portal connected-region identity");}
  public virtual WorldPosition Choke609F30(object portal,WorldPosition hiddenReturn){throw Missing(0x609f30,"Portal choke-point output");}
  public virtual Displacement3 DifferenceReturning609A48(WorldPosition destination,Displacement3 hiddenReturn,WorldPosition coordinates){throw Missing(0x609a48,"World position difference and returned pointer");}
 }

 // Instruction-order projection of Game08DF30, backed by
 // docs/reverse-client/r1-waypoint-execution/08-Waypoint-0x18008df30-procedure_assembly.json.
 // The native manager this pointer is unused. Region and portal lifetimes,
 // import bodies, native MXCSR traps/status, FTZ and DAZ remain outside.
 public static class PortalSelection {
  static float F32(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Squared(Displacement3 value,uint yAt,uint xAt,uint zAt){
   if(value==null)throw new MissingContract(yAt,"Null returned difference pointer outside supported domain");
   // Both sites load y, x, z from the returned pointer, then evaluate x*x,
   // y*y, z*z and the two ordered additions. No generic vector replacement.
   float y=value.Y4.Read(yAt),x=value.X0.Read(xAt),z=value.Z8.Read(zAt);
   float xx=F32(x*x),yy=F32(y*y),zz=F32(z*z);
   return F32(F32(xx+yy)+zz);
  }

  public static object Select(WorldPosition from,WorldPosition to,PortalSelectionBoundaries b,Invocation i){
   i.At=0x08df4a;object destinationRegion=b.Region(to);
   // The second call is unconditional, including a null first result.
   i.At=0x08df56;object sourceRegion=b.Region(from);
   if(destinationRegion==null||sourceRegion==null||Object.ReferenceEquals(destinationRegion,sourceRegion))return null;
   i.At=0x08df9d;int count=b.PortalCount609F10(sourceRegion);
   if(count<=0)return null;

   // Native stack buffers are reused across candidates. A partially written
   // later output must not silently acquire zeros or a new identity.
   var choke=new WorldPosition();var firstOutput=new Displacement3();var secondOutput=new Displacement3();
   for(int index=0;index<count;index++){
    i.At=0x08dfb5;object portal=b.PortalAt609F18(sourceRegion,index);
    i.At=0x08dfc1;if(b.PortalOpen609F20(portal)==0)continue;
    i.At=0x08dfd2;object connected=b.PortalRegion609F28(portal);
    if(!Object.ReferenceEquals(connected,destinationRegion))continue;

    // ChokePoint's return pointer is ignored; its hidden output is used.
    i.At=0x08dfe5;b.Choke609F30(portal,choke);
    i.At=0x08dff8;Displacement3 first=b.DifferenceReturning609A48(choke,firstOutput,from);
    float portalDistance=Squared(first,0x08e009,0x08e00e,0x08e012);
    // The first distance is captured before this callback. Both difference
    // calls use their returned pointer, which may differ from hiddenReturn.
    i.At=0x08e02b;Displacement3 second=b.DifferenceReturning609A48(to,secondOutput,from);
    float destinationDistance=Squared(second,0x08e031,0x08e036,0x08e03a);
    // COMISS/JC accepts strictly below OR unordered, for normal FP completion.
    if(portalDistance<destinationDistance||Single.IsNaN(portalDistance)||Single.IsNaN(destinationDistance))return portal;
   }
   return null;
  }
 }
}
