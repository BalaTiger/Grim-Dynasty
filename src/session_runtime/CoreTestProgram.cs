using System;
using System.Globalization;
namespace GrimDynasty.Session {
 public static class CoreTestProgram {
  public static int Main(string[] args){
   try {if(args.Length!=1)throw new ArgumentException("owned test save root required");int count=SessionCoreTests.Run(args[0]);Console.WriteLine("M2_CORE_ASSERTIONS="+count.ToString(CultureInfo.InvariantCulture));return 0;}
   catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
  }
 }
}
