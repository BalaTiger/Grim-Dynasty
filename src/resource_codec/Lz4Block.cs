// Reimplemented raw LZ4 decoder. No game DLLs, external packages or save APIs.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GrimDynasty.Resources {
 public static class Lz4Block {
  public const int MaxOutput = 64 * 1024 * 1024;
  static int Length(byte[] input, ref int at, int nibble, int bound) {
   int n=nibble;
   if(nibble==15) {
    int x;
    do {if(at>=input.Length)throw new InvalidDataException("truncated length");x=input[at++];n=checked(n+x);if(n>bound)throw new InvalidDataException("length exceeds output bound");}while(x==255);
   }
   return n;
  }
  public static byte[] Decode(byte[] input,int expected) {
   if(input==null)throw new ArgumentNullException("input");
   if(expected<0||expected>MaxOutput)throw new InvalidDataException("output limit");
   byte[] output=new byte[expected];int p=0,q=0;
   while(p<input.Length) {
    int token=input[p++],literal=Length(input,ref p,token>>4,expected);
    if(literal>expected-q||literal>input.Length-p)throw new InvalidDataException("literal bounds");
    Buffer.BlockCopy(input,p,output,q,literal);p+=literal;q+=literal;
    if(p==input.Length)break;
    if(input.Length-p<2)throw new InvalidDataException("truncated offset");
    int distance=input[p]|(input[p+1]<<8);p+=2;
    if(distance==0||distance>q)throw new InvalidDataException("match offset");
    int match=checked(Length(input,ref p,token&15,expected)+4);
    if(match>expected-q)throw new InvalidDataException("match bounds");
    for(int i=0;i<match;i++){output[q]=output[q-distance];q++;}
   }
   if(q!=expected)throw new InvalidDataException("decoded size mismatch");
   return output;
  }
 }
 public static class Lz4Probe {
  static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
  static void Assert(bool v){if(!v)throw new Exception("self-test failed");}
  static void Reject(byte[] b,int n){try{Lz4Block.Decode(b,n);}catch(InvalidDataException){return;}throw new Exception("invalid input accepted");}
  static int Tests(){Assert(Encoding.ASCII.GetString(Lz4Block.Decode(new byte[]{0x30,97,98,99},3))=="abc");Assert(Encoding.ASCII.GetString(Lz4Block.Decode(new byte[]{0x14,97,1,0},9))=="aaaaaaaaa");Assert(Lz4Block.Decode(new byte[0],0).Length==0);Reject(new byte[]{0xf0},20);Reject(new byte[]{0x10,97,0,0},5);Reject(new byte[]{0x10,97,2,0},5);Reject(new byte[]{0x14,97,1,0},8);Reject(new byte[]{0x20,97},2);return 8;}
  public static int Main(string[] args) {
   try {
    if(args.Length==1&&args[0]=="--self-test"){Console.WriteLine("{\"self_tests_passed\":"+Tests()+"}");return 0;}
    if(args.Length!=2)throw new ArgumentException("usage: Lz4Probe <project-private.lz4block> <exact output size> | --self-test");
    string path=Path.GetFullPath(args[0]);string project=Path.GetFullPath(@"D:\MyGitRepo\Grim-Dynasty")+Path.DirectorySeparatorChar;
    if(!path.StartsWith(project,StringComparison.OrdinalIgnoreCase)||!path.EndsWith(".lz4block",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("input must be a project-private .lz4block fixture");
    int expected=int.Parse(args[1]);var fi=new FileInfo(path);if(fi.Length>Lz4Block.MaxOutput+1024*1024)throw new InvalidDataException("packed input limit");
    byte[] b=Lz4Block.Decode(File.ReadAllBytes(path),expected);
    Console.WriteLine("{\"decoded_bytes\":"+b.Length+",\"sha256\":\""+Hash(b)+"\"}");return 0;
   }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 2;}
  }
 }
}
