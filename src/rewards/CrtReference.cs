using System;
using System.Runtime.InteropServices;
using System.Text;
// Test/probe-only independent direct ABI reference. Never linked into Rewards.dll.
public sealed class CrtReference : IDisposable {
 const string Library="C:\\Windows\\System32\\ucrtbase.dll";
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern float powf(float x,float y);
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern double _atof_l(byte[] text,IntPtr locale);
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern IntPtr _create_locale(int category,byte[] name);
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl,ExactSpelling=true)]static extern void _free_locale(IntPtr locale);
 IntPtr locale;
 public CrtReference(){locale=_create_locale(0,new byte[]{67,0});if(locale==IntPtr.Zero)throw new Exception("Reference C locale unavailable");}
 public float Literal(string s){return (float)_atof_l(Encoding.ASCII.GetBytes(s+"\0"),locale);}
 public double LiteralDouble(string s){return _atof_l(Encoding.ASCII.GetBytes(s+"\0"),locale);}
 public float Power(float x,float y){return powf(x,y);}
 public static float Single(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
 public static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
 public float OfficialShape(uint level,float[] constants){float l=level;float cubic=Single(Single(l*l)*l);float a=Single(Power(cubic,constants[0])*constants[1]);float b=Single(Single(l*l)*constants[2]);return Single(Single(Single(a+b)*constants[3])+constants[4]);}
 public float NaiveAllDoubleShape(uint level,double[] c){double l=level;return (float)((Math.Pow(l*l*l,c[0])*c[1]+l*l*c[2])*c[3]+c[4]);}
 public void Dispose(){if(locale!=IntPtr.Zero){_free_locale(locale);locale=IntPtr.Zero;}}
}
