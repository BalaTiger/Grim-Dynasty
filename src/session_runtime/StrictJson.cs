using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;

namespace GrimDynasty.Session {
 // Deliberately small save-format subset: integer numbers, unique keys, valid Unicode.
 internal static class StrictJson {
  internal const int MaxCharacters=256*1024;
  const int MaxDepth=16;
  internal static object Parse(string text) {
   if(text==null||text.Length>MaxCharacters)throw Bad("JSON size limit");
   var reader=new Reader(text);object value=reader.Value(0);reader.Space();
   if(reader.Position!=text.Length)throw Bad("JSON trailing content");return value;
  }
  internal static string Canonical(object value) {
   var output=new StringBuilder();Write(output,value,0);Limit(output);return output.ToString();
  }
  internal static string Sha256(string text) {
   if(text==null||text.Length>MaxCharacters)throw Bad("Hash input size limit");
   try {using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(new UTF8Encoding(false,true).GetBytes(text))).Replace("-","").ToLowerInvariant();}
   catch(EncoderFallbackException){throw Bad("Invalid Unicode hash input");}
  }
  static InvalidDataException Bad(string reason){return new InvalidDataException(reason);}
  static void Limit(StringBuilder output){if(output.Length>MaxCharacters)throw Bad("Canonical JSON size limit");}
  static void Unicode(string text){
   for(int i=0;i<text.Length;i++){
    char c=text[i];if(Char.IsHighSurrogate(c)){if(i+1>=text.Length||!Char.IsLowSurrogate(text[++i]))throw Bad("Unpaired JSON surrogate");}
    else if(Char.IsLowSurrogate(c))throw Bad("Unpaired JSON surrogate");
   }
  }
  static void Quote(StringBuilder output,string text){
   Unicode(text);output.Append('"');
   foreach(char c in text){
    if(c=='"'||c=='\\'){output.Append('\\');output.Append(c);}
    else if(c<32||c>126){output.Append("\\u");output.Append(((int)c).ToString("x4",CultureInfo.InvariantCulture));}
    else output.Append(c);Limit(output);
   }output.Append('"');Limit(output);
  }
  static void Write(StringBuilder output,object value,int depth){
   if(depth>MaxDepth)throw Bad("Canonical JSON depth limit");Limit(output);
   if(value==null)output.Append("null");
   else if(value is string)Quote(output,(string)value);
   else if(value is bool)output.Append((bool)value?"true":"false");
   else if(value is byte||value is sbyte||value is short||value is ushort||value is int||value is uint||value is long)
    output.Append(Convert.ToString(value,CultureInfo.InvariantCulture));
   else if(value is ulong){if((ulong)value>(ulong)Int64.MaxValue)throw Bad("JSON integer exceeds signed 64-bit range");output.Append(((ulong)value).ToString(CultureInfo.InvariantCulture));}
   else {
    var dictionary=value as IDictionary;
    if(dictionary!=null){
     var keys=new List<string>();foreach(object key in dictionary.Keys){if(!(key is string))throw Bad("JSON object key must be text");keys.Add((string)key);}keys.Sort(StringComparer.Ordinal);
     output.Append('{');bool first=true;foreach(string key in keys){if(!first)output.Append(',');first=false;Quote(output,key);output.Append(':');Write(output,dictionary[key],depth+1);}output.Append('}');
    }else{
     var list=value as IList;if(list==null)throw Bad("Unsupported canonical JSON value type");
     output.Append('[');for(int i=0;i<list.Count;i++){if(i!=0)output.Append(',');Write(output,list[i],depth+1);}output.Append(']');
    }
   }Limit(output);
  }
  sealed class Reader {
   readonly string text;internal int Position;
   internal Reader(string input){text=input;}
   internal void Space(){while(Position<text.Length&&(text[Position]==' '||text[Position]=='\t'||text[Position]=='\r'||text[Position]=='\n'))Position++;}
   bool Take(char c){if(Position<text.Length&&text[Position]==c){Position++;return true;}return false;}
   void Need(char c){if(!Take(c))throw Bad("JSON expected '"+c+"'");}
   internal object Value(int depth){
    if(depth>MaxDepth)throw Bad("JSON depth limit");Space();if(Position==text.Length)throw Bad("JSON value missing");char c=text[Position];
    if(c=='"')return String();
    if(c=='{'){
     Position++;Space();var map=new Dictionary<string,object>(StringComparer.Ordinal);if(Take('}'))return map;
     while(true){Space();string key=String();if(map.ContainsKey(key))throw Bad("Duplicate JSON key: "+key);Space();Need(':');map.Add(key,Value(depth+1));Space();if(Take('}'))return map;Need(',');}
    }
    if(c=='['){
     Position++;Space();var list=new List<object>();if(Take(']'))return list;
     while(true){list.Add(Value(depth+1));Space();if(Take(']'))return list;Need(',');}
    }
    if(c=='t'){Word("true");return true;}if(c=='f'){Word("false");return false;}if(c=='n'){Word("null");return null;}
    if(c=='-'||(c>='0'&&c<='9'))return Number();throw Bad("Invalid JSON value");
   }
   void Word(string word){if(text.Length-Position<word.Length||System.String.CompareOrdinal(text,Position,word,0,word.Length)!=0)throw Bad("Invalid JSON literal");Position+=word.Length;}
   long Number(){
    int start=Position;Take('-');if(Position>=text.Length)throw Bad("Truncated JSON integer");
    if(Take('0')){if(Position<text.Length&&text[Position]>='0'&&text[Position]<='9')throw Bad("Leading zero in JSON integer");}
    else {if(text[Position]<'1'||text[Position]>'9')throw Bad("Invalid JSON integer");while(Position<text.Length&&text[Position]>='0'&&text[Position]<='9')Position++;}
    if(Position<text.Length&&(text[Position]=='.'||text[Position]=='e'||text[Position]=='E'))throw Bad("Only integer JSON numbers are supported");
    long value;if(!Int64.TryParse(text.Substring(start,Position-start),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out value))throw Bad("JSON integer outside signed 64-bit range");return value;
   }
   string String(){
    Need('"');var output=new StringBuilder();
    while(Position<text.Length){
     char c=text[Position++];if(c=='"'){string value=output.ToString();Unicode(value);return value;}if(c<32)throw Bad("Unescaped JSON control character");
     if(c!='\\'){output.Append(c);continue;}if(Position==text.Length)throw Bad("Truncated JSON escape");c=text[Position++];
     switch(c){
      case '"':case '\\':case '/':output.Append(c);break;
      case 'b':output.Append('\b');break;case 'f':output.Append('\f');break;case 'n':output.Append('\n');break;case 'r':output.Append('\r');break;case 't':output.Append('\t');break;
      case 'u':
       if(text.Length-Position<4)throw Bad("Truncated JSON Unicode escape");int code=0;
       for(int i=0;i<4;i++){char digit=text[Position++];int n=digit>='0'&&digit<='9'?digit-'0':digit>='a'&&digit<='f'?digit-'a'+10:digit>='A'&&digit<='F'?digit-'A'+10:-1;if(n<0)throw Bad("Invalid JSON Unicode escape");code=code*16+n;}output.Append((char)code);break;
      default:throw Bad("Unknown JSON escape");
     }
    }throw Bad("Unterminated JSON string");
   }
  }
 }
}
