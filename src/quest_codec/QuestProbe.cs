using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
public static class QuestProbe {
 static readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=64*1024*1024,RecursionLimit=100};
 static void Value(StringBuilder b,object value){
  if(value==null){b.Append("null");return;}if(value is string){b.Append(json.Serialize(value));return;}
  if(value is double){double n=(double)value;if(double.IsNaN(n)||double.IsInfinity(n))throw new InvalidDataException("nonfinite float JSON transport");b.Append(n==0&&BitConverter.DoubleToInt64Bits(n)<0?"-0.0":n.ToString("R",CultureInfo.InvariantCulture));return;}
  var dict=value as IDictionary;if(dict!=null){b.Append('{');bool first=true;foreach(DictionaryEntry e in dict){if(!first)b.Append(',');first=false;b.Append(json.Serialize((string)e.Key));b.Append(':');Value(b,e.Value);}b.Append('}');return;}
  var list=value as IEnumerable;if(list!=null){b.Append('[');bool first=true;foreach(object item in list){if(!first)b.Append(',');first=false;Value(b,item);}b.Append(']');return;}
  b.Append(json.Serialize(value));
 }
 static void Print(object value){var b=new StringBuilder();Value(b,value);Console.WriteLine(b.ToString());}
 public static int Main(string[] args){try{
  Console.OutputEncoding=new UTF8Encoding(false);
  if(args.Length==2&&args[0]=="--fixture"){
   string p=Path.GetFullPath(args[1]);if(!p.StartsWith(@"D:\MyGitRepo\Grim-Dynasty\.tooling\quest-definition-build\fixtures\",StringComparison.OrdinalIgnoreCase)||!p.EndsWith(".fixture",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("private fixture required");Print(QuestDefinition.Read(File.ReadAllBytes(p)));return 0;
  }
  if(args.Length<2)throw new ArgumentException("QuestProbe <Quests.arc> --all | --member <name>");
  using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input)){
   if(args[1]=="--all"){foreach(var e in arc.Entries.Values)Print(new{name=e.name,definition=QuestDefinition.Read(arc.ReadMember(e.name))});}
   else if(args[1]=="--member"&&args.Length==3)Print(QuestDefinition.Read(arc.ReadMember(args[2])));
   else throw new ArgumentException("unsupported quest command");
  }return 0;
 }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 2;}}
}
