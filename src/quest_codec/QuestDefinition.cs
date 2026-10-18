// Independent QST2 v9 definition decoder. Does not evaluate conditions/actions.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using GrimDynasty.Resources;

namespace GrimDynasty.Quests {
 public static class QuestDefinition {
  static Dictionary<string,object> D(params object[] pairs){var d=new Dictionary<string,object>(StringComparer.Ordinal);for(int i=0;i<pairs.Length;i+=2)d.Add((string)pairs[i],pairs[i+1]);return d;}
  static readonly string[] comparisons={"Less","LessEqual","Equal","GreaterEqual","Greater","NotEqual"};
  static readonly Dictionary<int,string[]> conditions=new Dictionary<int,string[]>{
   {0,new[]{"OnQuest","quest:s on:b"}},{1,new[]{"QuestTaskComplete","quest:s task:u complete:b"}},{2,new[]{"HasExperience","amount:u"}},{3,new[]{"IsLevel","level:u"}},{4,new[]{"IsDifficulty","difficulty:u"}},{5,new[]{"IsHardcore","hardcore:b"}},{7,new[]{"HasToken","token:s has:b"}},{8,new[]{"HasMoney","amount:u"}},{9,new[]{"HasItem","dbr:s count:u complete:b"}},{13,new[]{"KnowsPlayer","known:b"}},{14,new[]{"OnQuestTask","quest:s task:u on:b"}},{15,new[]{"UsedQuestItem","dbr:s count:u"}},{17,new[]{"WaitForCompletion",""}},{18,new[]{"ServerHasToken","token:s has:b"}},{19,new[]{"QuestBlocked","quest:s blocked:b"}},{20,new[]{"AnyoneHasToken","token:s has:b"}},{21,new[]{"CheckRandomValue","name:s value:i"}},{22,new[]{"OwnsDLC","dlcId:u"}},{23,new[]{"HasTribute","amount:u"}},{24,new[]{"EarnedDevotion","amount:u"}},{25,new[]{"HasMastery","mastery:u has:b"}},{26,new[]{"IsChallenge","challenge:u"}},{27,new[]{"HasBuff","buffName:s has:b"}},{28,new[]{"HasKilledMonsterClass","monsterClass1:u monsterClass2:u count:u"}},{29,new[]{"HasKilledFaction","faction1:i faction2:i count:u"}}
  };
  static readonly Dictionary<int,string[]> actions=new Dictionary<int,string[]>{
   {1,new[]{"BeginQuestTask","quest:s task:u"}},{2,new[]{"CompleteQuest","quest:s"}},{3,new[]{"CompleteQuestTask","quest:s task:u"}},{4,new[]{"GiveMoney","amount:i"}},{5,new[]{"GiveFaction","faction:s amount:i"}},{6,new[]{"GiveItem","dbr:s amount:i"}},{7,new[]{"GiveLevel","amount:u"}},{8,new[]{"GiveExperience","amount:u"}},{9,new[]{"GiveToken","token:s"}},{10,new[]{"RemoveToken","token:s"}},{12,new[]{"DebugPrint","text:s"}},{13,new[]{"PlaySound","dbr:s"}},{14,new[]{"PlayAnimation","animation:u"}},{15,new[]{"GiveSkillPoint","amount:u"}},{17,new[]{"Notification","tag:s"}},{18,new[]{"GiveAttributePoint","amount:u"}},{19,new[]{"ScriptEvent","eventName:s"}},{20,new[]{"GiveRandomItem","dbr:s amount:i"}},{21,new[]{"GenerateRandomValue","name:s min:i max:i"}},{22,new[]{"CastSkill","dbr:s"}},{23,new[]{"UnlockFaction","faction:s"}},{24,new[]{"SetFaction","faction:s amount:i"}},{25,new[]{"GiveDevotion","amount:i"}},{26,new[]{"GiveTribute","amount:i"}},{27,new[]{"UnlockTutorial","page:i"}},{28,new[]{"PlayVideo","path:s allowSkip:b"}}
  };
  sealed class Cursor {
   readonly byte[] data;public int Position,End,Nodes;
   public Cursor(byte[] bytes,int at,int end){data=bytes;Position=at;End=end;}
   public byte[] Take(int n){if(n<0||n>End-Position)throw new InvalidDataException("QST truncated at "+Position);var b=new byte[n];Buffer.BlockCopy(data,Position,b,0,n);Position+=n;return b;}
   public byte Byte(){return Take(1)[0];}
   public uint U(){return BitConverter.ToUInt32(Take(4),0);}
   public int I(){return BitConverter.ToInt32(Take(4),0);}
   public double Float(){return (double)BitConverter.ToSingle(Take(4),0);}
   public bool Bool(){byte x=Byte();if(x>1)throw new InvalidDataException("noncanonical QST boolean");return x!=0;}
   public int Count(int max){uint n=U();if(n>max)throw new InvalidDataException("QST collection limit");return (int)n;}
   public string String(){uint n=U();if(n>1024*1024)throw new InvalidDataException("QST string limit");return ResourceUtil.Utf8.GetString(Take((int)n));}
   public Dictionary<string,object> Fields(string schema){var d=D();foreach(string field in schema.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)){string[] f=field.Split(':');object value;switch(f[1]){case "s":value=String();break;case "u":value=U();break;case "i":value=I();break;case "b":value=Bool();break;default:throw new InvalidDataException("schema");}d.Add(f[0],value);}return d;}
   public Dictionary<string,object> Lua(){string function=String();int n=Count(10000);var args=new List<object>();string[] names={"PlayerId","QuestGiverId","Bool","Number","String","Nil"};for(int i=0;i<n;i++){int t=I();if(t<0||t>=names.Length)throw new InvalidDataException("unknown Lua argument type");args.Add(D("type_id",t,"type",names[t],"bool_value",Bool(),"number_value",Float(),"string_value",String()));}return D("function",function,"arguments",args,"execution","not executed or evaluated");}
   public Dictionary<string,object> Script(bool condition){int start=Position,t=I(),version=Byte(),comparison=condition?I():-1;if(version>1)throw new InvalidDataException("unknown QST script version");string[] entry=null;
    if((condition&&t==12)||(!condition&&t==11))entry=new[]{"LuaScript",""};
    else if(condition&&t==6)entry=new[]{"HasFaction","faction:s"+(version>=1?" value:i":"")};
    else if(condition&&t==9)entry=new[]{"HasItem","dbr:s count:u"+(version==1?" complete:b":"")};
    else if(condition&&t==11)entry=new[]{"HasQuestObjective","questFile:s taskUid:u"+(version==1?" objectiveUid:u":"")+" satisfied:b"};
    else if(condition&&(t==10||t==16)){int n=Count(10000);var names=new List<string>();for(int i=0;i<n;i++)names.Add(String());uint amount=U();return D("offset",start,"end",Position,"type_id",t,"type",t==10?"HasKilled":"HasKilledProxy","version",version,"comparison",comparison>=0&&comparison<6?(object)comparisons[comparison]:comparison,"parameters",D("names",names,"count",amount));}
    else (condition?conditions:actions).TryGetValue(t,out entry);
    if(entry==null)throw new InvalidDataException("unknown QST "+(condition?"condition":"action")+" type "+t);
    var parameters=entry[0]=="LuaScript"?Lua():Fields(entry[1]);if(condition&&entry[0]=="LuaScript"){parameters.Add("return_comparison_id",I());parameters.Add("return_type_id",I());parameters.Add("bool_return",Bool());parameters.Add("number_return",Float());parameters.Add("string_return",String());}
    var r=D("offset",start,"end",Position,"type_id",t,"type",entry[0],"version",version,"parameters",parameters);if(condition){r.Add("comparison_id",comparison);r.Add("comparison",comparison>=0&&comparison<6?comparisons[comparison]:"UNKNOWN");}return r;
   }
   public Dictionary<string,object> Conditions(){int n=Count(10000),op=n==0?-1:I();if(op < -1||op>1||(n>0&&op<0))throw new InvalidDataException("unknown QST condition operator");var items=new List<object>();for(int i=0;i<n;i++)items.Add(Script(true));return D("operator",op<0?null:op==0?"And":"Or","conditions",items);}
   public Dictionary<string,object> Node(string kind){if(++Nodes>20000)throw new InvalidDataException("QST node limit");var r=D("kind",kind,"offset",Position);if(kind=="objective")r.Add("uid",U());r.Add("flags",U());r.Add("conditions",Conditions());int n=Count(10000);var list=new List<object>();for(int i=0;i<n;i++)list.Add(Script(false));r.Add("actions",list);r.Add("end",Position);return r;}
  }
  public static Dictionary<string,object> Read(byte[] data){
   if(data==null||data.Length<16||data.Length>8*1024*1024)throw new InvalidDataException("QST payload limit/header");var h=new Cursor(data,0,data.Length);if(Encoding.ASCII.GetString(h.Take(4))!="QST2"||h.U()!=9)throw new InvalidDataException("unsupported QST version");uint localeOffset=h.U();if(localeOffset<12||localeOffset>data.Length-4)throw new InvalidDataException("QST locale offset");
   var c=new Cursor(data,12,(int)localeOffset);var tasks=new List<object>();var q=D("uid",c.U(),"flags",c.U(),"tasks",tasks);var texts=new List<Dictionary<string,object>>{D("kind","group"),D("kind","quest")};int count=c.Count(10000);
   for(int i=0;i<count;i++){
    var task=D("kind","task","offset",c.Position,"uid",c.U(),"flags",c.U());var description=D("kind","description");texts.Add(task);texts.Add(description);task.Add("description",description);
    string[] groups={"on_accept","objectives","on_complete"};foreach(string group in groups){int n=c.Count(10000);var nodes=new List<object>();for(int j=0;j<n;j++){var node=c.Node(group=="objectives"?"objective":"event");nodes.Add(node);texts.Add(node);}task.Add(group,nodes);}
    task.Add("is_blocker",c.Bool());task.Add("dont_propagate",c.Bool());task.Add("end",c.Position);tasks.Add(task);
   }
   if(c.Position!=c.End)throw new InvalidDataException("QST graph not exactly consumed");
   var l=new Cursor(data,(int)localeOffset,data.Length);int languages=l.Count(128);if(languages<1)throw new InvalidDataException("QST locale count");var seen=new HashSet<string>(StringComparer.Ordinal);
   for(int i=0;i<languages;i++){
    string language=l.String();if(!seen.Add(language))throw new InvalidDataException("duplicate QST locale");int n=l.Count(100000);if(n!=texts.Count)throw new InvalidDataException("QST text-node count mismatch");
    for(int index=0;index<n;index++){int start=l.Position;string text=l.String();object existing;if(!texts[index].TryGetValue("text",out existing)){existing=D();texts[index].Add("text",existing);}((Dictionary<string,object>)existing).Add(language,D("index",index,"length_prefix_offset",start,"byte_length",l.Position-start-4,"text",text));}
   }
   if(l.Position!=l.End)throw new InvalidDataException("QST locale trailing bytes");q.Add("group_text",texts[0]["text"]);q.Add("quest_text",texts[1]["text"]);q.Add("graph_end",c.Position);q.Add("sha256",ResourceUtil.Hash(data));return q;
  }
 }
}
