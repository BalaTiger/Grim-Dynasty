using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Slot0 stays an explicit boundary. Production binds only observed primary
 // tables and exact class getters; a test observation is not native evidence.
 public class GameAssociatedLookupBoundaries {
  public readonly AddressSymbol<ClassRecord> ControllerBaseCharacterClass;
  public readonly PlayerMouseContext BoundMouseContext;
  public GameAssociatedLookupBoundaries(AddressSymbol<ClassRecord> expected):this(expected,null){}
  public GameAssociatedLookupBoundaries(AddressSymbol<ClassRecord> expected,PlayerMouseContext mouse){if(expected==null)throw new ArgumentNullException("ControllerBaseCharacter A5C9F0");ControllerBaseCharacterClass=expected;BoundMouseContext=mouse;}
  public virtual Field<object> CapturePrimary(object instance,Invocation i){throw new MissingContract(i.At,"Associated object requires an existing canonical primary view");}
  public virtual Field<ClassRecord> Class0(object instance,object capturedPrimary,Invocation i){throw new MissingContract(i.At,"Associated class slot0 target is unbound");}
  public virtual void Slot60(object instance,object capturedPrimary,Field<uint> flags,Invocation i){throw new MissingContract(i.At,"Associated virtual+60 target is unbound");}
  public virtual void Slot80(object instance,object capturedPrimary,Invocation i){throw new MissingContract(i.At,"Associated virtual+80 target is unbound");}
 }
 public sealed class GameAssociatedLookupInvocation {
  public readonly Invocation Native=new Invocation(),Derived=new Invocation();
  public GameObjectManagerLookupInvocation Lookup{get;private set;}
  public Field<object> CapturedObject{get;private set;}
  public Field<object> CapturedPrimary{get;private set;}
  public Field<ClassRecord> ReturnedClass{get;private set;}
  public Field<ClassRecord> Parent{get;private set;}
  public Field<object> Result{get;private set;}
  public bool LookupReturned,PrimaryCaptured,ClassReturned,DerivedEntered,DerivedReturned,Returned;
  public GameAssociatedLookupInvocation(){Reset();}
  internal void Reset(){Native.At=Derived.At=0;Lookup=new GameObjectManagerLookupInvocation();CapturedObject=new Field<object>();CapturedPrimary=new Field<object>();ReturnedClass=new Field<ClassRecord>();Parent=new Field<ClassRecord>();Result=new Field<object>();LookupReturned=PrimaryCaptured=ClassReturned=DerivedEntered=DerivedReturned=Returned=false;}
 }
 // Game077D60, saved Rea run3 pseudo-code and complete assembly. Reuses actual
 // locked019D40 and AL-only591480; source object identity is held in RBX across
 // slot0. Explicit OWN map/class storage and stable live views are required.
 public static class GameAssociatedLookupContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing associated lookup view");return value;}
  static object Finish(object value,GameAssociatedLookupInvocation call){call.Result.Write(value);call.Returned=true;call.Native.At=value==null?0x077daeu:0x077da6u;return value;}
  public static object Lookup077D60(GameObjectManagerState manager,Field<uint> id,GameAssociatedLookupBoundaries boundaries,GameAssociatedLookupInvocation invocation){
   var call=Need(invocation,0x077d66);call.Reset();var i=call.Native;i.At=0x077d66;
   Field<object> result;
   try{result=GameObjectManagerLookupContracts.LookupBits019D40(manager,id,call.Lookup);call.LookupReturned=true;}
   catch(MissingContract error){i.At=error.Address;throw;}
   i.At=0x077d6b;call.CapturedObject.CopyFrom(result);
   i.At=0x077d6e;object found=call.CapturedObject.Read(i.At);if(found==null)return Finish(null,call);
   i.At=0x077d73;var b=Need(boundaries,i.At);call.CapturedPrimary.CopyFrom(Need(b.CapturePrimary(found,i),0x077d73));call.PrimaryCaptured=true;
   i.At=0x077d79;object primary=call.CapturedPrimary.Read(i.At);var classBits=Need(b.Class0(found,primary,i),0x077d79);call.ReturnedClass.CopyFrom(classBits);call.ClassReturned=true;
   // LEA supplies a fixed identity, not the contents of classInfo. Binding that
   // identity is still explicit; its Unknown is consumed only at the CMP.
   i.At=0x077d82;ClassRecord actual=call.ReturnedClass.Read(i.At),expected=b.ControllerBaseCharacterClass.Read(i.At);
   if(Object.ReferenceEquals(actual,expected))return Finish(found,call);
   i.At=0x077d87;call.Parent.CopyFrom(Need(actual,i.At).Parent10);
   i.At=0x077d8b;var parent=call.Parent.Read(i.At);if(parent==null)return Finish(null,call);
   i.At=0x077d90;if(Object.ReferenceEquals(parent,expected))return Finish(found,call);
   i.At=0x077d95;call.DerivedEntered=true;bool derived;
   try{derived=GameMovementCallbackContracts.IsDerivedFrom591480(parent,expected,call.Derived);call.DerivedReturned=true;}
   catch(MissingContract error){i.At=error.Address;throw;}
   i.At=0x077d9a;return Finish(derived?found:null,call);
  }
 }
}
