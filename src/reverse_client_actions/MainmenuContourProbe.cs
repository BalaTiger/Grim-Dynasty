using System;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// Fixed saved-resource test driver. Core configuration is explicitly selected
// by RunSavedMainMenu: climb=4 and simplification tolerance=1.5f, not observed
// loaded game state. This neither creates a game nor claims mesh publication.
public static class MainmenuContourProbe {
 public static int Main(string[] args) {
  try {
   if(args.Length!=0)throw new ArgumentException("This probe accepts no arguments");
   const string path=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
   const string expected="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
   var info=new FileInfo(path);
   if((info.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Saved member is a reparse file");
   var data=new byte[1091357];
   using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
    if(file.Length!=data.Length)throw new InvalidOperationException("Saved member size changed");
    int offset=0;
    while(offset<data.Length){int n=file.Read(data,offset,data.Length-offset);if(n==0)throw new EndOfStreamException();offset+=n;}
   }
   string before=Hash(data);if(before!=expected)throw new InvalidOperationException("Saved member hash changed");
   NavigationLayerContoursTests.RunSavedMainMenu(data);
   if(Hash(data)!=expected)throw new InvalidOperationException("Probe changed borrowed source");
   Console.WriteLine("PASS real saved contour probe; explicit climb=4 tolerance=1.5; source unchanged; no final mesh or game acceptance");
   return 0;
  } catch(Exception error){Console.Error.WriteLine("FAIL "+error);return 1;}
 }
 static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
}
