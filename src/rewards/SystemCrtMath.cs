using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using GrimDynasty.TokenActions;
namespace GrimDynasty.Rewards {
 public enum TargetNumericLocale {Unknown,CInvariant}
 // Optional, fingerprinted Windows 10 x64 system-library profile. No game DLL.
 public sealed class SystemCrtMath : IDisposable {
  const string Library="C:\\Windows\\System32\\ucrtbase.dll";
  public const string VerifiedSha256="3c60056371f82e4744185b6f2fa0c69042b1e78804685944132974dd13f3b6d9";
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern float powf(float x,float y);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern double _atof_l(byte[] text,IntPtr locale);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern IntPtr _create_locale(int category,byte[] name);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern void _free_locale(IntPtr locale);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern int _controlfp_s(out uint current,uint value,uint mask);
  readonly object gate=new object();IntPtr locale;bool disposed;
  SystemCrtMath(IntPtr locale){this.locale=locale;}
  public static MutationResult Create(TargetNumericLocale targetLocale,out SystemCrtMath math){
   math=null;if(targetLocale!=TargetNumericLocale.CInvariant)return MutationResult.Unsupported("UnknownOrNonCNumericLocale");
   if(!Environment.Is64BitProcess||Environment.OSVersion.Platform!=PlatformID.Win32NT)return MutationResult.Unsupported("SystemCrtRequiresWindowsX64");
   try{
    object major=Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion","CurrentMajorVersionNumber",null);
    if(!(major is int)||(int)major!=10)return MutationResult.Unsupported("UnverifiedSystemUcrtDeploymentProfile");
    using(var hash=SHA256.Create())using(var stream=File.OpenRead(Library)){string value=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();if(value!=VerifiedSha256)return MutationResult.Unsupported("SystemUcrtFingerprintChanged");}
    uint control;var ready=CheckControl(out control);if(ready.Status!="Known")return ready;
    IntPtr c=_create_locale(0,new byte[]{67,0});if(c==IntPtr.Zero)return MutationResult.Unsupported("PrivateCLocaleCreationFailed");math=new SystemCrtMath(c);return MutationResult.Known(false);
   }catch(Exception e){if(e is IOException||e is UnauthorizedAccessException||e is DllNotFoundException||e is EntryPointNotFoundException||e is BadImageFormatException||e is System.Security.SecurityException)return MutationResult.Unsupported("SystemCrtUnavailable:"+e.GetType().Name);throw;}
  }
  static MutationResult CheckControl(out uint control){
   // A zero mask only queries the thread control word; it sets no bits.
   if(_controlfp_s(out control,0,0)!=0)return MutationResult.Unsupported("FloatingControlQueryFailed");
   if((control&0x300)!=0||(control&0x3000000)!=0||(control&0x8001f)!=0x8001f)return MutationResult.Unsupported("FloatingControlOutsideNearestMaskedPreservedProfile");return MutationResult.Known(false);
  }
  public MutationResult Check(out uint control){lock(gate){control=0;if(disposed||locale==IntPtr.Zero)return MutationResult.Unsupported("DisposedSystemCrtMath");return CheckControl(out control);}}
  public MutationResult Literal(string token,out float result){lock(gate){
   result=0;uint control;var ready=Check(out control);if(ready.Status!="Known")return ready;
   if(String.IsNullOrEmpty(token)||token.Length>2048)return MutationResult.Unsupported("UnknownOrOversizedNumericToken");
   foreach(char c in token)if(c!='.'&&(c<'0'||c>'9'))return MutationResult.Unsupported("NumericTokenOutsideNativeLexer");
   byte[] bytes=Encoding.ASCII.GetBytes(token+"\0");double wide=_atof_l(bytes,locale);result=(float)wide;return MutationResult.Known(false);
  }}
  public MutationResult Power(float x,float y,out float result){lock(gate){result=0;uint control;var ready=Check(out control);if(ready.Status!="Known")return ready;result=powf(x,y);return MutationResult.Known(false);}}
  public void Dispose(){lock(gate){if(!disposed){disposed=true;IntPtr p=locale;locale=IntPtr.Zero;if(p!=IntPtr.Zero)_free_locale(p);}}}
 }
}
