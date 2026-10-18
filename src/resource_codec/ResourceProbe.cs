using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;

public static class ResourceProbe {
 static void Print(object value){Console.WriteLine(new JavaScriptSerializer{MaxJsonLength=64*1024*1024,RecursionLimit=100}.Serialize(value));}
 public static int Main(string[] args){try{
  Console.OutputEncoding=new UTF8Encoding(false);
  if(args.Length<2)throw new ArgumentException("ResourceProbe <installation.arz|arc> --index | --verify-all | --record <name> | --member <name>");
  if(Path.GetExtension(args[0]).Equals(".arz",StringComparison.OrdinalIgnoreCase)){
   using(var input=ResourceInput.OpenGameResource(args[0]))using(var db=new ArzArchive(input)){
    if(args[1]=="--index")Print(new{header=db.Header,entries=new List<ArzEntry>(db.Entries.Values)});
    else if(args[1]=="--record"&&args.Length==3)Print(db.ReadRecord(args[2]));
    else if(args[1]=="--verify-all"){foreach(var e in db.Entries.Values){var r=db.ReadRecord(e.name);Print(new{name=r.name,@class=r.@class,body_sha256=r.body_sha256,field_count=r.fields.Count,typed_digest=r.TypedDigest()});}}
    else throw new ArgumentException("unsupported ARZ command");
   }
  }else if(Path.GetExtension(args[0]).Equals(".arc",StringComparison.OrdinalIgnoreCase)){
   using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input)){
    if(args[1]=="--index")Print(new{header=arc.Header,entries=new List<ArcEntry>(arc.Entries.Values)});
    else if(args[1]=="--member"&&args.Length==3){byte[] b=arc.ReadMember(args[2]);Print(new{name=args[2],bytes=b.Length,sha256=ResourceUtil.Hash(b)});}
    else if(args[1]=="--verify-all"){foreach(var e in arc.Entries.Values){byte[] b=arc.ReadMember(e.name);Print(new{name=e.name,bytes=b.Length,sha256=ResourceUtil.Hash(b)});}}
    else throw new ArgumentException("unsupported ARC command");
   }
  }else throw new ArgumentException("unsupported file extension");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 2;}}
}
