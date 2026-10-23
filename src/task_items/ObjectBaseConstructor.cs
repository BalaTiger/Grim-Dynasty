using System;
namespace GrimDynasty.TaskItems {
 public enum ObjectBaseDispatch { ObjectBase }
 public sealed class ObjectBaseState {
  // Own logical identity, not a native vptr or a callable Read implementation.
  public readonly ObjectBaseDispatch Dispatch;
  public readonly RegistrationObject Fields;
  // Stable own identity lets SetObjectName distinguish true self-alias from
  // an equal-valued, distinct source. The getters preserve source-level use
  // of the constructor model; they do not preserve a prior compiled DLL ABI.
  public readonly ObjectNameString Name;
  public ulong NameLength {get{return Name.Length;}}
  public ulong NameCapacity {get{return Name.Capacity;}}
  // Logical first payload byte, resolving either storage mode; not native +8
  // pointer bits when the observed string is in heap mode.
  public byte FirstNameByte {get{return Name.FirstByte;}}
  internal ObjectBaseState(RegistrationObject fields){
   Fields=fields;Dispatch=ObjectBaseDispatch.ObjectBase;
   Name=new ObjectNameString(new byte[0],15);
  }
  // At construction only the logical name is empty. Other inline-storage
  // bytes were not zeroed and remain deliberately absent from this model.
  public byte[] CopyNameBytes(){return Name.CopyBytes();}
 }
 public static class ObjectBaseConstructor {
  // Engine 0x18003a6b0 writes only the base vptr, +20 capacity, +18 length,
  // and the first name byte at +8. In particular it does NOT write +28/+2c.
  // This is valid-construction logical state, not native storage, padding,
  // allocation, destruction, copy construction, or a concrete item constructor.
  public static ObjectBaseState Initialize(RegistrationObject existingFields){
   if(existingFields==null)throw new ArgumentNullException("existingFields");
   return new ObjectBaseState(existingFields);
  }
  public static ObjectBaseState Create(){
   // Managed zero initialization must not manufacture knowledge of untouched
   // native registration fields. Null records an unobserved value, not zero.
   return Initialize(new RegistrationObject(null,null));
  }
 }
}
