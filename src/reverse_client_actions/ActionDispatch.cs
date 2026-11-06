using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Own typed, shared memory views. Unknown and known-null are different.
 // These are not native allocations, ABI wrappers, or a recovered running game.
 public sealed class Field<T> {
  private bool known; private T value;
  // Only explicit ulong Split/Join, or opaque copies from that domain, opt in.
  // In this mode the pair is authoritative; the old scalar slot stays unused.
  private UInt64FieldWords words;
  public T Read(uint at) { if(words!=null)return (T)(object)((ulong)words.Low32.Read(at)|((ulong)words.High32.Read(at)<<32));if(!known) throw new MissingContract(at,"Unobserved field"); return value; }
  public void Write(T v) { if(words!=null){ulong bits=(ulong)(object)v;words.Low32.Write(unchecked((uint)bits));words.High32.Write((uint)(bits>>32));return;}value=v; known=true; }
  public void Forget() { if(words!=null){words.Low32.Forget();words.High32.Forget();return;}known=false; value=default(T); }
  // Opaque native copies preserve knowledge, including an unobserved value.
  public Field<T> Snapshot() { var f=new Field<T>();if(words!=null)f.words=new UInt64FieldWords(words.Low32.Snapshot(),words.High32.Snapshot());else{f.known=known;f.value=value;}return f; }
  public void CopyFrom(Field<T> source) {
   if(words==null&&source.words==null){known=source.known;value=source.value;return;}
   // Capture BOTH halves before any destination store, including when fields
   // overlap in swapped order. No Read is used merely to copy unknown bits.
   Field<uint> low,high;
   if(source.words!=null){low=source.words.Low32.Snapshot();high=source.words.High32.Snapshot();}
   else{low=new Field<uint>();high=new Field<uint>();if(source.known){ulong bits=(ulong)(object)source.value;low.Write(unchecked((uint)bits));high.Write((uint)(bits>>32));}}
   if(words==null){words=new UInt64FieldWords(new Field<uint>(),new Field<uint>());known=false;value=default(T);}
   words.Low32.CopyFrom(low);words.High32.CopyFrom(high);
  }
  public static Field<T> Of(T v) { var f=new Field<T>();f.Write(v);return f; }
  internal UInt64FieldWords SplitUInt64(){
   if(typeof(T)!=typeof(ulong))throw new InvalidOperationException("Only ulong fields have DWORD views");
   if(words==null){var low=new Field<uint>();var high=new Field<uint>();if(known){ulong bits=(ulong)(object)value;low.Write(unchecked((uint)bits));high.Write((uint)(bits>>32));}words=new UInt64FieldWords(low,high);known=false;value=default(T);}
   return words;
  }
  internal static Field<T> JoinUInt64(Field<uint> low,Field<uint> high){
   if(typeof(T)!=typeof(ulong))throw new InvalidOperationException("Only ulong fields have DWORD views");
   var pair=new UInt64FieldWords(low,high);var f=new Field<T>();f.words=pair;return f;
  }
 }
 // Explicit little-endian storage view, not an evaluator/callback mechanism.
 // Pair/cell identities never change after binding. Join borrows its inputs;
 // Split migrates an ordinary whole word once, without making unknown bits known.
 public sealed class UInt64FieldWords {
  public readonly Field<uint> Low32,High32;
  internal UInt64FieldWords(Field<uint> low,Field<uint> high){
   if(low==null||high==null)throw new ArgumentNullException("DWORD cell");
   if(Object.ReferenceEquals(low,high))throw new ArgumentException("Distinct DWORD offsets require distinct cells");Low32=low;High32=high;
  }
  public static UInt64FieldWords Split(Field<ulong> word){if(word==null)throw new ArgumentNullException("word");return word.SplitUInt64();}
  public static Field<ulong> Join(Field<uint> low,Field<uint> high){return Field<ulong>.JoinUInt64(low,high);}
 }
 public sealed class MissingContract:Exception {
  public readonly uint Address;
  public MissingContract(uint address,string message):base(message){Address=address;}
  public MissingContract(uint address,string message,Exception inner):base(message,inner){Address=address;}
 }
 public class ActionRecord {
  public readonly Field<byte> PreAction18=new Field<byte>();
  public readonly Field<uint> OwnerId08=new Field<uint>();
 }
 public sealed class Controller {
  public readonly Field<ActionRecord> CurrentA0=new Field<ActionRecord>();
  public readonly Field<ActionRecord> PendingA8=new Field<ActionRecord>();
 }
 public class Role {
  public readonly Field<Handler> Handler24C0=new Field<Handler>();
 }
 public sealed class Handler {
  public readonly Field<ActionRecord> Current08=new Field<ActionRecord>();
  public readonly Field<byte> Busy18=new Field<byte>();
  public readonly Field<Node> Head20=new Field<Node>();
  public readonly Field<ulong> Count28=new Field<ulong>();
 }
 public sealed class Node {
  public readonly Field<Node> Next00=new Field<Node>(),Previous08=new Field<Node>();
  public readonly Field<ActionRecord> Value10=new Field<ActionRecord>();
  public bool Freed;
 }
 public sealed class Counter { public readonly Field<int> Value=new Field<int>(); public Counter(int value){Value.Write(value);} }
 public sealed class Engine {
  // Key is the signed byte displacement from the observed engine pointer.
  public readonly Dictionary<long,int> PermissionCells=new Dictionary<long,int>();
 }
 public sealed class Environment { public readonly Field<Engine> EngineGlobal=new Field<Engine>(); }
 public sealed class Outcome {
  public bool ReturnedNormally; public string Boundary; public uint Address; public Exception Exception;
 }
 public sealed class Invocation { public uint At; }

 // Missing concrete virtual calls stop explicitly. Tests can supply observations,
 // but they do not establish the implementation of these native dependencies.
 public class Boundaries {
  protected static MissingContract Missing(uint at,string name){return new MissingContract(at,name);}
  public virtual Role GetAI(Controller c){throw Missing(0x078d40,"GetAI/object lifetime");}
  public virtual int Kind58(ActionRecord a){throw Missing(0x160111,"Action virtual +58");}
  public virtual byte Active30(ActionRecord a){throw Missing(0x160135,"Action virtual +30");}
  public virtual int Resolve80(ActionRecord active,ActionRecord incoming,ActionRecord pending){throw Missing(0x1601be,"Action virtual +80");}
  public virtual byte IsRunningSkill(Role role){throw Missing(0x5405a0,"Skill state");}
  public virtual void Role8A8(Role r){throw Missing(0x127eba,"Role virtual +8A8");}
  public virtual void LogMissingRole(){throw Missing(0x127e79,"Engine logging virtual +18");}
  public virtual void Destroy0(ActionRecord a,uint flags){throw Missing(0x127efe,"Action virtual +0/lifetime");}
  public virtual void Finish20(ActionRecord a){throw Missing(0x127ee7,"Action virtual +20");}
  public virtual void Stop18(ActionRecord a){throw Missing(0x089c05,"Action virtual +18");}
  public virtual void Start08(ActionRecord a){throw Missing(0x089b17,"Action virtual +8; motion not recovered");}
  public virtual ActionRecord PreAction60(ActionRecord a){
   if(a.PreAction18.Read(0x08188f)==0)return null;
   throw Missing(0x081899,"Nonzero pre-action requires object lookup/type validation/coordinates/construction");
  }
  public virtual int Check(Controller c,ActionRecord a,Environment e,Invocation i){return Dispatch.CheckAction(c,a,e,this,i);}
  public virtual void Execute(Handler h,ActionRecord a,Counter budget,Invocation i){Executor.Execute(h,a,budget,this,i);}
  public virtual Node AllocateLinks(Handler h,Node next,Node previous,Field<ActionRecord> borrowedInputSlot){throw Missing(0x079cd0,"Queue allocation/link constructor not recovered");}
  // Own managed storage adapter only; native free/heap faults are not emulated.
  public virtual void ReleaseNode(Node node){if(node==null||node.Freed)throw Missing(0x5da6bc,"Invalid or already released own node");node.Freed=true;}
  public virtual void LengthError(){throw Missing(0x089ba9,"Native length_error/EH outside supported normal path");}
 }

 public static class Dispatch {
  private static T NonNull<T>(T value,uint at) where T:class {
   if(value==null)throw new MissingContract(at,"Null native dereference outside own supported domain");return value;
  }
  private static ActionRecord Active(Role r,uint at){return NonNull(r.Handler24C0.Read(at),at).Current08.Read(at);}
  public static Outcome RunLocal(Controller c,ActionRecord a,Environment e,Boundaries b){
   var i=new Invocation();try{Local(c,a,e,b,i);return new Outcome{ReturnedNormally=true};}
   catch(MissingContract x){return new Outcome{Address=x.Address,Boundary=x.Message,Exception=x};}
   catch(Exception x){return new Outcome{Address=i.At,Boundary="External callback did not return normally; native EH unmodeled",Exception=x};}
  }
  public static int CheckAction(Controller c,ActionRecord incoming,Environment e,Boundaries b,Invocation i){
   NonNull(incoming,0x160105);i.At=0x160111;int firstKind=b.Kind58(incoming);
   i.At=0x16011a;Role role=NonNull(b.GetAI(c),i.At);
   ActionRecord current=Active(role,0x160122);byte busy=0;
   if(current!=null){i.At=0x160135;busy=b.Active30(current);}
   if(busy==0){i.At=0x160142;if(b.Kind58(incoming)!=8)return 0;}
   current=Active(role,0x16014e);int oldKind=0;
   if(current!=null){i.At=0x160166;oldKind=b.Kind58(current);}
   if(oldKind!=8){i.At=0x16017b;if(b.IsRunningSkill(role)!=0)return 0;}
   // A composition without the permission-table projection stops at its first
   // actual global access; earlier no-permission paths do not invent one.
   Engine engine=NonNull(NonNull(e,0x16018e).EngineGlobal.Read(0x16018e),0x160195);
   long displacement=0x3616cL+((long)firstKind*26L+(long)oldKind)*4L;
   int permission;if(!engine.PermissionCells.TryGetValue(displacement,out permission))throw new MissingContract(0x160195,"Unobserved permission cell at signed displacement "+displacement);
   if(permission!=3)return permission;
   current=Active(role,0x1601a1);if(current==null)return 0;
   ActionRecord pending=c.PendingA8.Read(0x1601b7);i.At=0x1601be;return b.Resolve80(current,incoming,pending);
  }
  public static void Local(Controller c,ActionRecord incoming,Environment e,Boundaries b,Invocation i){
   i.At=0x127e55;Role originalRole=b.GetAI(c);
   if(originalRole==null){i.At=0x127e79;b.LogMissingRole();if(incoming!=null){i.At=0x127e9d;b.Destroy0(incoming,1);}return;}
   i.At=0x127ea9;int permission=b.Check(c,incoming,e,i);
   if(permission==4){i.At=0x127eba;b.Role8A8(originalRole);}
   else if(permission!=0){
    if(permission==1){ActionRecord old=c.PendingA8.Read(0x127f93);if(old!=null){i.At=0x127fa7;b.Destroy0(old,1);}c.PendingA8.Write(incoming);}
    else if(permission==2&&incoming!=null){i.At=0x127fe4;b.Destroy0(incoming,1);}
    return;
   }
   ActionRecord action=c.CurrentA0.Read(0x127eca);c.CurrentA0.Write(null);
   if(action!=null){i.At=0x127ee7;b.Finish20(action);}
   action=c.CurrentA0.Read(0x127eea);if(action!=null){i.At=0x127efe;b.Destroy0(action,1);}
   action=c.PendingA8.Read(0x127f00);c.CurrentA0.Write(null);
   if(action!=null){i.At=0x127f1b;b.Destroy0(action,1);}
   c.PendingA8.Write(null);
   var budget=new Counter(100);NonNull(incoming,0x127f27);i.At=0x127f32;ActionRecord pre=b.PreAction60(incoming);
   ActionRecord selected=incoming;
   if(pre!=null){action=c.CurrentA0.Read(0x127f3d);if(action!=null){i.At=0x127f51;b.Destroy0(action,1);}c.CurrentA0.Write(incoming);selected=pre;}
   Handler handler=NonNull(originalRole.Handler24C0.Read(0x127f63),0x127f6a);i.At=0x127f6a;b.Execute(handler,selected,budget,i);
  }
 }
}
