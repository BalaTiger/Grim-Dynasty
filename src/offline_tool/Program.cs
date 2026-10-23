using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;

namespace GrimDynasty.Offline {
 public static class Program {
  public static int Main(string[] args) {
   Console.OutputEncoding=new UTF8Encoding(false);
   string command=args.Length==0?"help":args[0];
   CommandResult result;
   try {
    var tail=new string[Math.Max(0,args.Length-1)];
    if(tail.Length>0)Array.Copy(args,1,tail,0,tail.Length);
    switch(command) {
     case "help": case "--help":
      if(tail.Length!=0)throw new ArgumentException("help takes no arguments");
      result=CommandResult.Known("help",new {commands=new[]{
       "gd-offline.exe capabilities",
       "gd-offline.exe inspect-record <local.arz> <record-name>",
       "gd-offline.exe inspect-quest <local.qst>",
       "gd-offline.exe inspect-quest <local.arc> <member-name.qst>",
       "gd-offline.exe replay inventory-roundtrip",
       "gd-offline.exe replay inventory-destroy-unsupported",
       "gd-offline.exe replay <local.json>"},
       exit_codes=new{known=0,unsupported=3,invalid_input=2,internal_error=1}});break;
     case "capabilities":
      if(tail.Length!=0)throw new ArgumentException("capabilities takes no arguments");
      result=CommandResult.Known(command,new {version="0.1.0",profile="M1 offline",
       supported=new[]{"ARZ v3 selected record with finite fields","ARC v3 selected QST member","QST2 v9 definition decoding", "Own inventory-cycle fixture: task start/update, existing-item take/add, events, money and reward history"},
       unsupported=new[]{"Other archive/quest versions or unknown decoder types","Other replay fixtures/actions; XP/CRT evaluation","Native object construction/destruction, live engine state","Rendering, input, combat, playable game, saves and original-save compatibility"},
       limits=new{archive_bytes=512*1024*1024,table_bytes=8*1024*1024,payload_bytes=8*1024*1024,replay_bytes=65536,replay_steps=64,json_characters=4*1024*1024},
       evidence="Current acceptance uses original project-authored fixtures. It does not establish full official-data or runtime compatibility.",
       execution="Read explicit local input files; replay changes in-memory own state only. No game DLL loading, game execution or network access."});break;
     case "inspect-record":case "inspect-quest":result=CodecCommands.Run(command,tail);break;
     case "replay":result=ReplayCommands.Run(tail);break;
     default:result=CommandResult.Invalid(command,"Unknown command; run help");break;
    }
   } catch(Exception e) {
    if(e is ArgumentException||e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is OverflowException||e is DecoderFallbackException)
     result=CommandResult.Invalid(command,e.GetType().Name+": "+e.Message);
    else result=new CommandResult{status="InternalError",command=command,reason=e.GetType().Name+": "+e.Message};
   }
   string output;
   try {output=JsonOutput.Serialize(result);}
   catch(OutputLimitException) {result=CommandResult.Unsupported(command,"M1 structured output exceeds 4 Mi characters or nesting limit");output=JsonOutput.Serialize(result);}
   catch(Exception e) {result=new CommandResult{status="InternalError",command=command,reason="JSON output: "+e.GetType().Name};output=JsonOutput.Serialize(result);}
   Console.WriteLine(output);
   return result.status=="Known"?0:result.status=="Unsupported"?3:result.status=="InvalidInput"?2:1;
  }
 }
 sealed class OutputLimitException:Exception {}
 // Only simple result objects are passed here. Finite float round trips and signed zero are preserved.
 static class JsonOutput {
  const int Max=4*1024*1024;
  static readonly JavaScriptSerializer Strings=new JavaScriptSerializer{MaxJsonLength=Max};
  public static string Serialize(object value){var b=new StringBuilder();Write(b,value,0);if(b.Length>Max)throw new OutputLimitException();return b.ToString();}
  static void Write(StringBuilder b,object v,int depth){
   if(depth>64||b.Length>Max)throw new OutputLimitException();
   if(v==null){b.Append("null");return;}
   if(v is string){
    if(((string)v).Length>Max)throw new OutputLimitException();
    // ASCII JSON survives Windows PowerShell 5.1's native-process code-page decoding;
    // Unicode is preserved as JSON escapes rather than lost in localized error text.
    foreach(char c in Strings.Serialize(v)){if(c>127){b.Append("\\u");b.Append(((int)c).ToString("x4",CultureInfo.InvariantCulture));}else b.Append(c);}
    return;
   }
   if(v is bool){b.Append((bool)v?"true":"false");return;}
   if(v is double||v is float){
    double n=Convert.ToDouble(v,CultureInfo.InvariantCulture);
    if(Double.IsNaN(n)||Double.IsInfinity(n))throw new InvalidDataException("Non-finite JSON number");
    b.Append(n==0&&BitConverter.DoubleToInt64Bits(n)<0?"-0.0":v is float?((float)v).ToString("R",CultureInfo.InvariantCulture):n.ToString("R",CultureInfo.InvariantCulture));return;
   }
   if(v is byte||v is sbyte||v is short||v is ushort||v is int||v is uint||v is long||v is ulong||v is decimal){b.Append(Convert.ToString(v,CultureInfo.InvariantCulture));return;}
   var dictionary=v as IDictionary;
   if(dictionary!=null){
    var keys=new List<string>();foreach(object key in dictionary.Keys){if(!(key is string))throw new InvalidDataException("JSON key must be text");keys.Add((string)key);}keys.Sort(StringComparer.Ordinal);
    b.Append('{');bool first=true;foreach(string key in keys){if(!first)b.Append(',');first=false;Write(b,key,depth+1);b.Append(':');Write(b,dictionary[key],depth+1);}b.Append('}');return;
   }
   var sequence=v as IEnumerable;
   if(sequence!=null){b.Append('[');bool first=true;foreach(object item in sequence){if(!first)b.Append(',');first=false;Write(b,item,depth+1);}b.Append(']');return;}
   var members=new SortedDictionary<string,object>(StringComparer.Ordinal);
   foreach(var f in v.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))members.Add(f.Name,f.GetValue(v));
   foreach(var p in v.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance))if(p.CanRead&&p.GetIndexParameters().Length==0)members.Add(p.Name,p.GetValue(v,null));
   Write(b,members,depth+1);
  }
 }
}
