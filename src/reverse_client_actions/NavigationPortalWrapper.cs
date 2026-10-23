using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Engine262D30, actual normalized assembly in r1-straight-path-portal-wrapper.
 // Only direct callee262F10 is reused from NavigationSearchStorage. This is
 // the inline decode actually present here, not a call to2688C0 or its read
 // order. The same captured mesh pointer serves both independent decodes.
 // Own address domain: stable managed mesh/sidecar identity and independent
 // endpoint locals; no arbitrary byte overlap, native faults, or offmesh data
 // publication is implied. Byte type fields may share an explicit Field cell.
 // MissingContract is an own unresolved-address/value stop, never a native
 // status replacement. All preceding writes remain observable to the caller.
 public static class NavigationPortalWrapper {
  public const uint Failure=0x80000008u,Success=0x40000000u;
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own portal view/address");var view=value as NavigationView;if(view!=null)view.RequireLive(at);return value;}
  static T At<T>(T[] array,uint index,uint at)where T:class{if(array==null||index>=array.Length)throw new MissingContract(at,"Outside own portal array extent");return Need(array[(int)index],at);}
  static uint Mask(uint bits){return unchecked((1u<<(int)(bits&31))-1u);}
  // ADD forms a polygon address; its pointer bits and target are consumed only
  // by the subsequent Type1F load. Pure unknown MOV remains unknown until then.
  sealed class PolyAddress {
   readonly Field<NavigationPoly[]> pointer;readonly uint index;
   public PolyAddress(Field<NavigationPoly[]> source,uint index){pointer=source.Snapshot();this.index=index;}
   public NavigationPoly Read(uint at){return At(pointer.Read(at),index,at);}
  }
  static bool Decode(NavigationMesh mesh,uint reference,bool second,out NavigationTile tile,out PolyAddress polygon){
   tile=null;polygon=null; // Own result locals; no native caller output write.
   if(reference==0)return false;
   uint at=second?0x262e17u:0x262d74u;Need(mesh,at);uint tileBits=mesh.TileBits5C.Read(at);
   at=second?0x262e1du:0x262d7au;mesh.RequireLive(at);uint polyBits=mesh.PolyBits60.Read(at);
   uint tileIndex=(reference>>(int)(polyBits&31))&Mask(tileBits),polyIndex=reference&Mask(polyBits);
   at=second?0x262e3du:0x262d9bu;mesh.RequireLive(at);if(tileIndex>=mesh.TileLimit30.Read(at))return false;
   at=second?0x262e4eu:0x262dafu;mesh.RequireLive(at);var tiles=mesh.Tiles50.Snapshot();
   uint salt=reference>>(int)(unchecked(tileBits+polyBits)&31);
   at=second?0x262e54u:0x262db5u;mesh.RequireLive(at);salt&=Mask(mesh.SaltBits58.Read(at));
   at=second?0x262e5du:0x262dbeu;var found=At(tiles.Read(at),tileIndex,at);if(found.Salt00.Read(at)!=salt)return false;
   at=second?0x262e62u:0x262dc3u;found.RequireLive(at);var header=found.Header08.Read(at);if(header==null)return false;
   at=second?0x262e6bu:0x262dccu;if(polyIndex>=Need(header,at).PolyCount18.Read(at))return false;
   at=second?0x262e7bu:0x262de1u;found.RequireLive(at);polygon=new PolyAddress(found.Polys10,polyIndex);tile=found;return true;
  }
  static byte Type(NavigationMesh mesh,NavigationMeshTilesState state,NavigationPoly polygon,uint at){
   Need(state,at);if(!Object.ReferenceEquals(mesh,state.Mesh))throw new MissingContract(at,"Own portal sidecar belongs to a different mesh identity");return state.Spatial.Poly(polygon,at).Type1F.Read(at);
  }
  public static uint GetPortal262D30(NavigationQuery query,uint fromReference,uint toReference,NavigationPortalEndpoints outputs,Field<byte> fromType,Field<byte> toType,NavigationMeshTilesState meshState,Field<float> portalScale){
   var meshPointer=Need(query,0x262d51).Mesh0.Snapshot();if(fromReference==0)return Failure;
   var mesh=Need(meshPointer.Read(0x262d74),0x262d74);NavigationTile fromTile,toTile;PolyAddress fromAddress,toAddress;
   if(!Decode(mesh,fromReference,false,out fromTile,out fromAddress))return Failure;
   var fromPoly=fromAddress.Read(0x262dfe);byte first=Type(mesh,meshState,fromPoly,0x262dfe);Need(fromType,0x262e11).Write((byte)(first>>6));
   if(!Decode(mesh,toReference,true,out toTile,out toAddress))return Failure;
   var toPoly=toAddress.Read(0x262e9b);byte second=Type(mesh,meshState,toPoly,0x262e9b);Need(toType,0x262eb1).Write((byte)(second>>6));
   return NavigationSearchStorage.FillPortalEndpoints262F10(meshState,fromReference,fromPoly,fromTile,toReference,toPoly,toTile,outputs,portalScale);
  }
 }
}
