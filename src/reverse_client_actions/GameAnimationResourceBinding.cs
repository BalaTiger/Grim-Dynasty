using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // A sidecar on the actual Graphics identity, not a second graphics engine.
 // Registration of a view publishes no native field and supplies no default manager.
 public sealed class EngineGraphicsAnimationFields {
  static readonly ConditionalWeakTable<object,EngineGraphicsAnimationFields> owners=new ConditionalWeakTable<object,EngineGraphicsAnimationFields>();
  public readonly object Owner;
  public readonly Field<EngineAnimationResourceManagerState> Manager20=new Field<EngineAnimationResourceManagerState>();
  EngineGraphicsAnimationFields(object owner){Owner=owner;}
  public static EngineGraphicsAnimationFields ForGraphics(object owner){if(owner==null)throw new ArgumentNullException("graphics");return owners.GetValue(owner,delegate(object value){return new EngineGraphicsAnimationFields(value);});}
 }
 // The simple Game entry whose observed primary table is1806D4D48. Fields remain
 // unknown until a caller establishes them. This is not a full024080 constructor.
 public sealed class GameAnimationEntryState {
  public readonly Field<ulong> Virtual00=new Field<ulong>();
  public readonly Field<EngineGraphicsResourceState> Resource10=new Field<EngineGraphicsResourceState>();
  public readonly Field<float> Float18=new Field<float>(),Float1C=new Field<float>(),Float20=new Field<float>();
  public readonly Field<uint> Dword24=new Field<uint>();
  public readonly Field<byte> Byte28=new Field<byte>();
 }
 public sealed class GameAnimationSetFields {
  static readonly ConditionalWeakTable<AnimationSet,GameAnimationSetFields> owners=new ConditionalWeakTable<AnimationSet,GameAnimationSetFields>();
  readonly Field<GameAnimationEntryState>[] entries=new Field<GameAnimationEntryState>[52];
  public readonly AnimationSet Owner;
  public readonly Field<uint> Pending0C=new Field<uint>();
  GameAnimationSetFields(AnimationSet owner){Owner=owner;for(int n=0;n<entries.Length;n++)entries[n]=new Field<GameAnimationEntryState>();}
  public Field<GameAnimationEntryState> Entry(int index){if(index<0||index>=52)throw new ArgumentOutOfRangeException("index");return entries[index];}
  public static GameAnimationSetFields ForSet(AnimationSet owner){if(owner==null)throw new ArgumentNullException("set");return owners.GetValue(owner,delegate(AnimationSet value){return new GameAnimationSetFields(value);});}
 }
 public sealed class GameAnimationResourceBindingContext {
  public readonly Field<WorldEngine> EngineGlobal;
  public readonly EngineAnimationNameLocale Locale;
  public GameAnimationResourceBindingContext(Field<WorldEngine> engine,EngineAnimationNameLocale locale){if(engine==null)throw new ArgumentNullException("engine");EngineGlobal=engine;Locale=locale;}
 }
 public sealed class GameAnimationResourceBindingInvocation {
  public readonly Invocation Native=new Invocation();
  // Selected consumption/publication markers, not an instruction/read trace.
  public readonly List<uint> Stages=new List<uint>();
  public readonly EngineResourceStringState LocalName=new EngineResourceStringState();
  public readonly Field<object> CapturedGraphics=new Field<object>();
  public readonly Field<EngineAnimationResourceManagerState> CapturedManager=new Field<EngineAnimationResourceManagerState>();
  public readonly Field<EngineGraphicsResourceState> CapturedResource=new Field<EngineGraphicsResourceState>();
  public AnimationSet Set;public GameAnimationEntryState Entry;public EngineAnimationResourceLoadInvocation Load;
  public int SelectedIndex=-1,ScannedLength,OwnAllocationBytes;
  // Logical temporary lifetime only; passive byte observations remain available
  // in diagnostics. Managed reclamation is not a native allocator/free ABI claim.
  public bool GrowthEntered,GrowthReturned,OwnHeapAllocated,OwnHeapNormalRelease;
  public bool Used,EmptySource,AlreadyPresent,NameCopied,LocalNameNormalRelease,GraphicsReturned,ManagerReturned,ResourcePublished,ScalePublished,OtherFloatPublished,Returned;
  public byte ReturnValue;
 }
 // SavedGame024D80/020910,Engine077820/091A20 and their precise imports/table
 // bindings. Calls manager03AC50 itself, with no success/decoder/play callbacks.
 // shortcut: exclusive stable typed OWN strings/resources; fresh disjoint Game
 // temporary names <=65536 bytes use016460 plus the separately recovered Game
 // 016340 fresh-cap15/oldLength0 branch. Its162D0 allocation and16620 release use
 // normal OWN managed storage, not an implementation of the native allocator. Native
 // allocator/EH/concurrent mutation and actual resource decoding remain unclaimed.
 public static class GameAnimationResourceBinding {
  static uint At(GameAnimationResourceBindingInvocation c,uint at){c.Native.At=at;c.Stages.Add(at);return at;}
  static T Need<T>(T value,uint at,string reason)where T:class{if(value==null)throw new MissingContract(at,reason);return value;}
  static void Begin(GameAnimationResourceBindingInvocation c){if(c==null)throw new ArgumentNullException("invocation");if(c.Used)throw new InvalidOperationException("Use a fresh invocation per attempt");c.Used=true;}
  static byte Return(GameAnimationResourceBindingInvocation c,byte value){c.ReturnValue=value;c.Returned=true;At(c,0x0209fa);return value;}
  static EngineResourceByteStorage GrowFreshTemporary(int count,GameAnimationResourceBindingInvocation c){
   c.GrowthEntered=true;At(c,0x016379);
   if(count<16||count>EngineGraphicsResourceConstruction.MaximumNameBytes||c.LocalName.Capacity.Read(c.Native.At)!=15||c.LocalName.Length.Read(c.Native.At)!=0)throw new MissingContract(c.Native.At,"Only fresh cap15/oldLength0 normal Game string growth is covered");
   // Actual Game016393 compares half(oldCapacity) with floor(rounded/3).
   // In this bounded fresh domain7<=10, so the geometric branch is never taken.
   ulong capacity=(ulong)count|15UL;c.OwnAllocationBytes=checked((int)capacity+1);At(c,0x0163b7);
   EngineResourceByteStorage storage;
   try{storage=new EngineResourceByteStorage(c.OwnAllocationBytes);}catch(OutOfMemoryException e){throw new MissingContract(c.Native.At,"OWN allocation failed; native162D0 failure/EH is unmodeled",e);}
   c.OwnHeapAllocated=true;
   // Old length is0: no old-byte copy. Old capacity15: no old allocation release.
   At(c,0x016414);c.LocalName.Capacity.Write(15);At(c,0x01641c);c.LocalName.Length.Write(0);At(c,0x016433);c.LocalName.Inline.Byte(0).Write(0);
   At(c,0x016436);c.LocalName.PublishHeap(storage);At(c,0x016439);c.LocalName.Capacity.Write(capacity);At(c,0x01643d);c.LocalName.Length.Write(0);At(c,0x01644b);storage.Byte(0).Write(0);
   At(c,0x016458);c.GrowthReturned=true;return storage;
  }
  public static Field<object> GetGraphics077820(Field<WorldEngine> capturedEngine,GameAnimationResourceBindingInvocation c){
   At(c,0x077820);var engine=Need(capturedEngine.Read(c.Native.At),c.Native.At,"Null Engine at +1F8 read");
   c.CapturedGraphics.CopyFrom(EngineGraphicsResourceEngineFields.ForEngine(engine).Graphics1F8.Snapshot());
   At(c,0x077827);c.GraphicsReturned=true;return c.CapturedGraphics.Snapshot();
  }
  public static Field<EngineGraphicsResourceState> LoadGraphics091A20(GameAnimationResourceBindingContext context,Field<object> graphics,Field<EngineResourceNameInput> name,GameAnimationResourceBindingInvocation c){
   At(c,0x091a20);var identity=Need(graphics.Read(c.Native.At),c.Native.At,"Null Graphics at manager+20 read");
   c.CapturedManager.CopyFrom(EngineGraphicsAnimationFields.ForGraphics(identity).Manager20.Snapshot());
   At(c,0x091a24);var manager=c.CapturedManager.Read(c.Native.At);
   if(manager==null){At(c,0x091a30);c.ManagerReturned=true;return Field<EngineGraphicsResourceState>.Of(null);}
   At(c,0x091a29);var table=manager.Virtual00.Snapshot();At(c,0x091a2c);
   if(table.Read(c.Native.At)!=0x18033f478UL)throw new MissingContract(c.Native.At,"Unbound animation manager virtual+10");
   if(!Object.ReferenceEquals(manager.EngineGlobal,context.EngineGlobal))throw new MissingContract(c.Native.At,"Manager must borrow this same live gEngine cell");
   c.Load=new EngineAnimationResourceLoadInvocation();var result=EngineAnimationResourceManager.Load03AC50(manager,name,context.Locale,c.Load);c.ManagerReturned=true;return result;
  }
  static EngineResourceNameInput CopyTemporary(EngineResourceStringState source,GameAnimationResourceBindingInvocation c){
   At(c,0x02094b);ulong capacity=source.Capacity.Read(c.Native.At);
   Field<EngineResourceByteStorage> capturedBytes;
   if(capacity<16)capturedBytes=Field<EngineResourceByteStorage>.Of(source.Inline);else{At(c,0x020952);capturedBytes=source.Heap.Snapshot();}
   At(c,0x020955);c.LocalName.Capacity.Write(15);At(c,0x020961);c.LocalName.Length.Write(0);At(c,0x020966);c.LocalName.Inline.Byte(0).Write(0);
   int count=0;At(c,0x02096b);var bytes=Need(capturedBytes.Read(c.Native.At),c.Native.At,"Null source string buffer");
   while(true){
    if(count>=bytes.Extent||count>EngineGraphicsResourceConstruction.MaximumNameBytes)throw new MissingContract(c.Native.At,"C-string terminator outside known OWN byte region");
    if(bytes.Byte(count).Read(c.Native.At)==0)break;
    count++;c.ScannedLength=count;At(c,0x020977);
   }
   At(c,0x020983);At(c,0x0164eb);
   var destination=c.LocalName.Inline;if(count>15){At(c,0x0164fb);destination=GrowFreshTemporary(count,c);}
   var result=new byte[count];
   if(count==0){At(c,0x01651b);c.LocalName.Length.Write(0);At(c,0x01653d);c.LocalName.Inline.Byte(0).Write(0);}
   else{At(c,0x01655e);for(int n=0;n<count;n++){result[n]=bytes.Byte(n).Read(c.Native.At);destination.Byte(n).Write(result[n]);}At(c,0x016568);c.LocalName.Length.Write((ulong)count);At(c,0x016576);destination.Byte(count).Write(0);}
   c.NameCopied=true;return new EngineResourceNameInput(result);
  }
  static byte AddEntry(GameAnimationResourceBindingContext context,GameAnimationEntryState entry,Field<EngineResourceStringState> input,Field<float> scale,Field<float> other,GameAnimationResourceBindingInvocation c){
   var scaleCapture=scale.Snapshot();var otherCapture=other.Snapshot();c.Entry=entry;
   At(c,0x020935);var source=Need(input.Read(c.Native.At),c.Native.At,"Null source string at length comparison");
   if(source.Length.Read(c.Native.At)==0){c.EmptySource=true;return Return(c,1);}
   At(c,0x020940);Need(entry,c.Native.At,"Null entry at resource comparison");
   if(entry.Resource10.Read(c.Native.At)!=null){c.AlreadyPresent=true;return Return(c,1);}
   var copied=CopyTemporary(source,c);
   At(c,0x020990);var engine=context.EngineGlobal.Snapshot();
   At(c,0x020993);var graphics=GetGraphics077820(engine,c);At(c,0x0209a1);
   c.CapturedResource.CopyFrom(LoadGraphics091A20(context,graphics,Field<EngineResourceNameInput>.Of(copied),c));
   At(c,0x0209aa);if(c.LocalName.Capacity.Read(c.Native.At)>=16){At(c,0x0209c3);c.OwnHeapNormalRelease=true;}c.LocalNameNormalRelease=true;
   At(c,0x0209c8);var resource=c.CapturedResource.Read(c.Native.At);if(resource==null)return Return(c,0);
   At(c,0x0209cd);entry.Resource10.Write(resource);c.ResourcePublished=true;
   At(c,0x0209d1);entry.Float1C.CopyFrom(scaleCapture);c.ScalePublished=true;
   At(c,0x0209d6);entry.Float20.CopyFrom(otherCapture);c.OtherFloatPublished=true;return Return(c,1);
  }
  public static byte Add020910(GameAnimationResourceBindingContext context,GameAnimationEntryState entry,Field<EngineResourceStringState> source,Field<float> scale,Field<float> other,GameAnimationResourceBindingInvocation c){
   if(context==null||source==null||scale==null||other==null)throw new ArgumentNullException("OWN input cells");Begin(c);
   try{return AddEntry(context,entry,source,scale,other,c);}catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
  public static byte Add024D80(GameAnimationResourceBindingContext context,Field<AnimationSet> set,uint animation,Field<EngineResourceStringState> source,Field<float> scale,Field<float> other,GameAnimationResourceBindingInvocation c){
   if(context==null||set==null||source==null||scale==null||other==null)throw new ArgumentNullException("OWN input cells");Begin(c);
   try{
    uint at=animation<=51?0x024d8eu:0x024d95u;At(c,at);c.Set=Need(set.Read(at),at,"Null AnimationSet at entry-table read");c.SelectedIndex=animation<=51?(int)animation:0;
    var capture=GameAnimationSetFields.ForSet(c.Set).Entry(c.SelectedIndex).Snapshot();At(c,0x024da1);var entry=Need(capture.Read(c.Native.At),c.Native.At,"Null entry at virtual table read");c.Entry=entry;
    var table=entry.Virtual00.Snapshot();At(c,0x024daf);if(table.Read(c.Native.At)!=0x1806d4d48UL)throw new MissingContract(c.Native.At,"Unbound animation entry virtual+28");
    return AddEntry(context,entry,source,scale,other,c);
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
 }
}
