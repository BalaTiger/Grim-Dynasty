using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
public static class ResourceFixtureProbe {
 public static int Main(string[] args){try{
  Console.OutputEncoding=new UTF8Encoding(false);
  if(args.Length!=3)throw new ArgumentException("fixture path, arz|arc, record/member required");string p=Path.GetFullPath(args[0]);
  if(!p.StartsWith(@"D:\MyGitRepo\Grim-Dynasty\.tooling\resource-containers-build\fixtures\",StringComparison.OrdinalIgnoreCase)||!p.EndsWith(".fixture",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("private fixture path required");
  using(var input=new ResourceInput(new MemoryStream(File.ReadAllBytes(p)))){
   object value;if(args[1]=="arz"){using(var db=new ArzArchive(input)){value=db.ReadRecord(args[2]);}}
   else if(args[1]=="arc"){using(var arc=new ArcArchive(input)){byte[] b=arc.ReadMember(args[2]);value=new{bytes=b.Length,sha256=ResourceUtil.Hash(b)};}}
   else throw new ArgumentException("fixture type");Console.WriteLine(new JavaScriptSerializer().Serialize(value));
  }return 0;
 }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 2;}}
}
