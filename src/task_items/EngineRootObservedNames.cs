using System;
namespace GrimDynasty.TaskItems {
 // Observed bytes only: Engine.dll SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // offline-engine-root-names/metadata.json pins writer LEA -> ClassInfo+8 -> export role.
 // Preferred-VA tokens identify data in that specific Engine image, not runtime pointers.
 public static class EngineRootObservedNames {
  public static bool TryCopy(ulong sourceToken,out byte[] bytes) {
   bytes=null;
   switch(sourceToken) {
    case 0x180309E64UL: bytes=new byte[]{0x41,0x63,0x74,0x6f,0x72,0};return true;
    case 0x180309AC4UL: bytes=new byte[]{0x45,0x6e,0x74,0x69,0x74,0x79,0};return true;
    case 0x180309AECUL: bytes=new byte[]{0x4f,0x62,0x6a,0x65,0x63,0x74,0};return true;
    default:return false;
   }
  }
  public static byte[] CopyForWriter(EngineRootWriter writer) {
   byte[] bytes;
   if(!TryCopy(EngineRootRegistration.GetWriterSpec(writer).NameSourceToken,out bytes))
    throw new ArgumentOutOfRangeException("writer");
   return bytes;
  }
 }
}
