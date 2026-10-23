using System;
namespace GrimDynasty.ReverseClient.Actions {
 // Stable identities for native inline 24-byte positions. Unknown words do not
 // become zero, and callbacks receive these same views rather than snapshots.
 public sealed class WorldPosition {
  public readonly Field<ulong> Q0,Q8,Q16;
  public WorldPosition():this(new Field<ulong>(),new Field<ulong>(),new Field<ulong>()){}
  // Shared cells express whole-word overlap; byte-unaligned aliasing is outside.
  public WorldPosition(Field<ulong> q0,Field<ulong> q8,Field<ulong> q16){
   if(q0==null||q8==null||q16==null)throw new ArgumentNullException("position cell");Q0=q0;Q8=q8;Q16=q16;
  }
 }
 public sealed class MoveActionRecord:ActionRecord {
  public readonly WorldPosition Start48=new WorldPosition(),Destination60=new WorldPosition();
  public readonly Field<uint> Flags78=new Field<uint>(),SkillReference7C=new Field<uint>(),Animation84=new Field<uint>();
  public readonly Field<float> Speed80=new Field<float>();
  public readonly Field<float> Scalar14=new Field<float>();
  public readonly Field<byte> Flag11=new Field<byte>();
  private bool released;
  // Explicit own-adapter observation, not detection of native lifetime/ABA.
  public void MarkReleased(){released=true;}
  public void RequireLive(uint at){if(released)throw new MissingContract(at,"Released action view; native lifetime not emulated");}
 }
 public sealed class MoveCharacter:Role {
  public readonly Field<byte> Gate26AF=new Field<byte>(),SpeedFlag26B3=new Field<byte>();
  public readonly Field<uint> AssociatedId16C0=new Field<uint>();
  public readonly Field<object> SkillContext868=new Field<object>();
  public readonly Field<uint> CurrentSkillA90=new Field<uint>(),AttackFlags2688=new Field<uint>(),AttackSkill26A8=new Field<uint>();
  public readonly WorldPosition AttackPosition2690=new WorldPosition();
  public readonly Field<MovementManager> MovementDC0=new Field<MovementManager>();
  public readonly Field<object> Sound2210=new Field<object>(),Sound2110=new Field<object>();
  public readonly Field<uint> SoundId22C8=new Field<uint>();
 }
 public sealed class MovementManager {
  public readonly Field<object> Owner00=new Field<object>();
  public readonly Field<byte> Moving38=new Field<byte>(),Registered6C=new Field<byte>();
  public readonly WorldPosition Position20=new WorldPosition();
 }
 public sealed class AnimationSet { public readonly Field<uint> Kind08=new Field<uint>(); }
 public sealed class LocalVector3 {
  // The native stop path explicitly writes exactly 12 zero bytes.
  public readonly uint X=0,Y=0,Z=0;
 }
 public sealed class ClassRecord {
  public readonly Field<ClassRecord> Parent10=new Field<ClassRecord>();
 }
 public sealed class WorldEngine { public readonly Field<object> World50=new Field<object>(); }
 public sealed class AddressSymbol<T> where T:class {
  private readonly uint address;private readonly Field<T> value=new Field<T>();private bool bound;
  public AddressSymbol(uint address){this.address=address;}
  public void Write(T identity){
   if(identity==null)throw new MissingContract(address,"Null fixed-address symbol binding");
   if(bound&&!Object.ReferenceEquals(value.Read(address),identity))throw new MissingContract(address,"Fixed-address symbol identity cannot be rebound");
   value.Write(identity);bound=true;
  }
  public T Read(uint at){return value.Read(at);}
 }
 public sealed class MoveContext {
  // Explicit identities corresponding to GameA5CBA0/A5BFD0 and current gEngine.
  // Establishing the real registration/engine state is outside this component.
  public readonly AddressSymbol<ClassRecord> CharacterClass=new AddressSymbol<ClassRecord>(0xa5cba0),ActivatedSkillClass=new AddressSymbol<ClassRecord>(0xa5bfd0);
  public readonly AddressSymbol<ClassRecord> SkillClass=new AddressSymbol<ClassRecord>(0xa5ca50);
  public readonly Field<WorldEngine> EngineGlobal=new Field<WorldEngine>();
  public readonly Field<object> NoNameImport609568=new Field<object>();
  // Observed disk constant must be supplied explicitly; no invented tolerance.
  public readonly Field<float> AlreadyThereConstant7A7FD8=new Field<float>();
 }
}
