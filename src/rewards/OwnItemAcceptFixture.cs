using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using GrimDynasty.Quests;
public static class OwnItemAcceptFixture {
 static void Text(BinaryWriter w,string s){var b=Encoding.UTF8.GetBytes(s);w.Write((uint)b.Length);w.Write(b);}
 public static Dictionary<string,object> Read(){
  using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)){
   w.Write(Encoding.ASCII.GetBytes("QST2"));w.Write(9u);w.Write(0u);
   w.Write(3u);w.Write(0u);w.Write(1u);w.Write(1u);w.Write(0u);w.Write(1u);
   w.Write(0u);w.Write(1u);w.Write(0); // event flags, one AND condition
   w.Write(9);w.Write((byte)1);w.Write(3);Text(w,"ming/items/military_ration.dbr");w.Write(2u);w.Write(true);
   w.Write(1u);w.Write(4);w.Write((byte)0);w.Write(25); // one money action
   w.Write(0u);w.Write(0u);w.Write(false);w.Write(true);
   long locale=stream.Position;w.Write(1u);Text(w,"zh");w.Write(5u);
   foreach(string text in new[]{"军需署","核验行粮","军需核验","备齐两份行粮后交付","备齐者预发军饷"})Text(w,text);
   w.Flush();stream.Position=8;w.Write((uint)locale);w.Flush();return QuestDefinition.Read(stream.ToArray());
  }
 }
}
