using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// Fixed, reproducible real-data component integration. This is NOT a game,
// native Level.Load, complete tile cache, mesh publication or a path search.
// The caller explicitly selects the region climb parameter; no loaded global
// configuration is inferred. The source is a previously verified workspace
// artifact, never a live original installation or a player save.
public static class MainmenuLayerPipeline {
 const string MemberPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string MemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static string Hash(byte[] data,int at,int length){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data,at,length)).Replace("-","").ToLowerInvariant();}
 static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
 public static int Main(string[] args){
  try {
   int climb;
   if(args.Length!=2||args[0]!="--climb"||!Int32.TryParse(args[1],out climb))throw new ArgumentException("Usage: MainmenuLayerPipeline.exe --climb <explicit signed integer>");
   byte[] member=new byte[1091357];
   using(var file=new FileStream(MemberPath,FileMode.Open,FileAccess.Read,FileShare.Read)){
    Require(file.Length==member.Length,"Saved member length changed");int offset=0;
    while(offset<member.Length){int n=file.Read(member,offset,member.Length-offset);if(n==0)throw new EndOfStreamException();offset+=n;}
   }
   Require(Hash(member,0,member.Length)==MemberHash,"Saved member hash changed");
   Require(Hash(member,422373,442996)=="0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee","Level chunk fingerprint changed");
   var pathing=NavigationPathingData.Parse(member,422373,442996);
   Require(pathing.Groups.Count==16,"Unexpected fixed sample group count");
   var cache=NavigationLayerCache.Create(4096);
   var sources=new List<NavigationPathingByteSlice>();var references=new List<uint>();
   foreach(var group in pathing.Groups)foreach(var layer in group.Layers){
    var reference=new NavigationLayerReference(0xDEADBEEFu);
    uint status=cache.Add(layer.Payload,reference);
    Require(status==NavigationLayerCache.Success,"Layer registration failed: "+status.ToString("X8"));
    sources.Add(layer.Payload);references.Add(reference.Value);
   }
   Require(sources.Count==9&&cache.Count==9,"Actual layer count differs");
   Console.WriteLine("CONFIG explicitClimb="+climb+"; managed cache capacity=4096; runtime native configuration unobserved");
   int completed=0;
   // The explicit input order is our inspection order, not an assertion about
   // scheduling of the native worker's grid loop or publication queue.
   for(int n=0;n<sources.Count;n++){
    var source=sources[n];var slot=cache.GetSlot(n);
    Require(Object.ReferenceEquals(slot.Blob20,source)&&Object.ReferenceEquals(slot.Header08,source),"Registration lost source slice identity");
    Require(cache.EncodeReference(slot)==references[n],"Reference does not identify the inserted pool slot");
    int x=BitConverter.ToInt32(member,source.Offset+8),z=BitConverter.ToInt32(member,source.Offset+12);
    var found=new uint[32];int count=cache.Enumerate(x,z,found);bool seen=false;
    for(int k=0;k<count;k++)if(found[k]==references[n])seen=true;
    Require(seen,"Coordinate query cannot find actual inserted layer");
    var decoded=NavigationLayerData.Decode(slot.Blob20);
    Require(Object.ReferenceEquals(decoded.Source,slot.Blob20),"Decoder lost borrowed cache source identity");
    var regionPlane=decoded.Plane(3);var originalOwner=regionPlane.Array;
    var result=NavigationLayerRegions.Build(decoded,climb);
    Require(Object.ReferenceEquals(result.Layer,decoded)&&Object.ReferenceEquals(result.Regions.Array,originalOwner),"Region builder replaced the mutable layer or owner");
    Console.WriteLine("LAYER ordinal="+n+" reference="+references[n]+" x="+x+" z="+z+" width="+decoded.Width+" height="+decoded.Height+" copied="+decoded.BytesWritten+" rawRegions="+result.RawRegionCount+" regions="+decoded.RegionCount+" outcome="+result.Outcome+" detail="+result.Detail+" regionSHA256="+Hash(regionPlane.Array,regionPlane.Offset,regionPlane.Count));
    if(result.Outcome==NavigationLayerRegionOutcome.Success)completed++;
   }
   Require(Hash(member,0,member.Length)==MemberHash,"Processing changed borrowed source bytes");
   if(completed!=sources.Count){Console.Error.WriteLine("INCOMPLETE real layer pipeline: "+completed+"/"+sources.Count+" region stages succeeded; no mesh or movement success is claimed");return 2;}
   Console.WriteLine("PASS actual mainmenu: 9 shared cache records -> copied layers -> in-place region output; no final mesh publication or navigation search");return 0;
  } catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}
 }
}
