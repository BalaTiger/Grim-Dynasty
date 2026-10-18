using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskAuto;
namespace GrimDynasty.TaskKills {
 public enum EntityKind {Unknown,Player,Monster,Other}
 public sealed class EntitySnapshot {public EntityKind Kind;public uint? Slot8D0OwnerId;public byte? RawByte20A4;}
 public sealed class KillWorld {
  public bool IndexComplete;
  public readonly Dictionary<uint,EntitySnapshot> Entities=new Dictionary<uint,EntitySnapshot>();
  public bool Lookup(uint id,out EntitySnapshot entity){return Entities.TryGetValue(id,out entity)||IndexComplete;}
 }
 public sealed class KillEventBody {
  // Native event std::string at+8, victim/source object IDs at+0x28/+0x2c.
  public string RecordName;public uint VictimId,SourceId;
 }
 public sealed class KillCondition : IConditionRuntime {
  readonly List<string> names;readonly int version,comparison;readonly uint required;
  readonly OfflineEvents events;readonly OfflineHandler handler;readonly KillWorld world;
  public uint Current;
  public byte ChangedByte14 {get;set;}
  public byte TextByte15 {get;private set;}
  public bool CallbackOwnerAttached {get;private set;}
  public bool Disposed {get;private set;}
  public int? SystemTimeSnapshot;
  public int LastDisplayTick,DisplayDelay=4000;
  // Boundary declaration for the pure/valid temporary localization reads.
  // No text rendering or arbitrary Localize implementation is asserted.
  public bool PureLocalizationContextKnown;
  public OfflineEvents EventManager {get{return events;}}
  public IList<string> NormalizedNames {get{return names.AsReadOnly();}}
  KillCondition(int v,int cmp,uint target,List<string> paths,OfflineEvents bus,KillWorld entities){version=v;comparison=cmp;required=target;names=paths;events=bus;world=entities;handler=new OfflineHandler{TryInvoke=e=>Receive(e==null?null:e.Body as KillEventBody)};}
  static MutationResult U(string s){return MutationResult.Unsupported(s);}
  public static MutationResult Create(int version,int comparison,uint required,IList<string> input,bool asciiCLocaleKnown,OfflineEvents bus,KillWorld world,out KillCondition condition){
   condition=null;if(version!=0&&version!=1)return U("HasKilledVersion:"+version);if(input==null)return U("UnknownRecordNames");var paths=new List<string>();
   foreach(var item in input){if(item==null)return U("InvalidRecordString");var chars=item.ToCharArray();for(int i=0;i<chars.Length;i++){char c=chars[i];if(c>127)return U("NonAsciiCrtLowercaseNotRecovered");if(c=='\\')chars[i]='/';else if(c>='A'&&c<='Z'){if(!asciiCLocaleKnown)return U("UnknownCrtLowercaseLocale");chars[i]=(char)(c+32);}}paths.Add(new string(chars));}
   condition=new KillCondition(version,comparison,required,paths,bus,world);return MutationResult.Known(true);
  }
  public bool Matches(Predicate p){return !Disposed&&p!=null&&p.TypeId==10&&p.Version==version;}
  public EvaluationResult Evaluate(World unused){
   if(Disposed)return EvaluationResult.Unsupported("DisposedCondition");int a=unchecked((int)Current),b=unchecked((int)required);bool value;
   switch(comparison){case 0:value=a<b;break;case 1:value=a<=b;break;case 2:value=a==b;break;case 3:value=a>=b;break;case 4:value=a>b;break;case 5:value=a!=b;break;default:return EvaluationResult.Unsupported("Comparison:"+comparison);}return EvaluationResult.Known(value);
  }
  public MutationResult CheckStart(){if(Disposed||events==null)return U("MissingLiveConditionOrEventManager");return events.ContainsRow(11).HasValue?MutationResult.Known(false):U("IncompleteEventIndex");}
  public MutationResult Start(){var check=CheckStart();if(check.Status!="Known")return check;bool changed=!CallbackOwnerAttached;CallbackOwnerAttached=true;var result=events.Register(11,handler);if(result.Status!="Known")throw new InvalidOperationException("Event snapshot changed during start");return MutationResult.Known(changed||result.Changed.Value);}
  public MutationResult CheckEnd(){return CheckStart();}
  public MutationResult End(){var check=CheckEnd();if(check.Status!="Known")return check;var result=events.UnRegister(11,handler);if(result.Status!="Known")throw new InvalidOperationException("Event snapshot changed during end");bool changed=CallbackOwnerAttached||result.Changed.Value;CallbackOwnerAttached=false;return MutationResult.Known(changed);}
  public MutationResult CheckReset(){return Disposed?U("DisposedCondition"):MutationResult.Known(false);}
  public MutationResult Reset(){var check=CheckReset();if(check.Status!="Known")return check;bool changed=Current!=0||TextByte15!=1;Current=0;TextByte15=1;return MutationResult.Known(changed);}
  public MutationResult CheckOnChangedNoDisplay(){
   if(Disposed)return U("DisposedCondition");if(TextByte15==0)return MutationResult.Known(false);
   if(!PureLocalizationContextKnown)return U("UnknownPureLocalizationInputs");if(!SystemTimeSnapshot.HasValue)return U("UnknownSystemTimeSnapshot");
   return DisplayDelay<unchecked(SystemTimeSnapshot.Value-LastDisplayTick)?U("CinematicTextDisplayNotRecovered"):MutationResult.Known(false);
  }
  public MutationResult Receive(KillEventBody e){
   if(Disposed||!CallbackOwnerAttached)return U("InvalidCallbackOwnerPointerDomain");if(e==null||world==null)return U("UnknownKillEventOrEntityIndex");
   EntitySnapshot source;if(!world.Lookup(e.SourceId,out source))return U("UnknownSourceEntity");if(source==null)return MutationResult.Known(false);
   if(source.Kind==EntityKind.Monster){if(!source.Slot8D0OwnerId.HasValue)return U("UnknownSourceMonsterOwnerSlot");if(!world.Lookup(source.Slot8D0OwnerId.Value,out source))return U("UnknownOwnerEntity");if(source==null)return MutationResult.Known(false);}
   if(source.Kind==EntityKind.Unknown)return U("UnknownSourceRtti");if(source.Kind!=EntityKind.Player)return MutationResult.Known(false);
   EntitySnapshot victim;if(!world.Lookup(e.VictimId,out victim))return U("UnknownVictimEntity");
   if(victim!=null){if(victim.Kind==EntityKind.Unknown)return U("UnknownVictimRtti");if(victim.Kind==EntityKind.Monster){if(!victim.RawByte20A4.HasValue)return U("UnknownVictimByte20A4");if(victim.RawByte20A4.Value!=0)return MutationResult.Known(false);}}
   if(names.Count==0)return MutationResult.Known(false);if(e.RecordName==null)return U("UnknownEventRecordName");
   foreach(var path in names)if(String.Equals(path,e.RecordName,StringComparison.Ordinal)){Current=unchecked(Current+1);ChangedByte14=1;TextByte15=1;return MutationResult.Known(true);}return MutationResult.Known(false);
  }
  public MutationResult Dispose(){if(Disposed)return U("DisposedCondition");if(CallbackOwnerAttached){var ended=End();if(ended.Status!="Known")return ended;}names.Clear();Disposed=true;return MutationResult.Known(true);}
 }
}
