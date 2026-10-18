using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using GrimDynasty.Quests;
public static class OwnDeliveryFixture {
 static void Text(BinaryWriter w,string s){var b=Encoding.UTF8.GetBytes(s);w.Write((uint)b.Length);w.Write(b);}
 public static Dictionary<string,object> Read(){using(var s=new MemoryStream())using(var w=new BinaryWriter(s)){
  w.Write(Encoding.ASCII.GetBytes("QST2"));w.Write(9u);w.Write(0u);w.Write(3u);w.Write(0u);w.Write(1u);w.Write(1u);w.Write(0u);
  w.Write(0u);w.Write(1u); // no accept events, one objective
  w.Write(1u);w.Write(0u);w.Write(1u);w.Write(0);w.Write(9);w.Write((byte)1);w.Write(1);Text(w,"ming/items/military_ration.dbr");w.Write(1u);w.Write(true);w.Write(0u);
  w.Write(1u);w.Write(0u);w.Write(0u);w.Write(1u);w.Write(4);w.Write((byte)0);w.Write(75);w.Write(false);w.Write(true);
  long locale=s.Position;w.Write(1u);Text(w,"zh");w.Write(6u);foreach(string text in new[]{"军需署","交付行粮","解送军粮","从已有三份行粮中交出两份","保留一份行粮","军需验收发饷"})Text(w,text);
  w.Flush();s.Position=8;w.Write((uint)locale);w.Flush();return QuestDefinition.Read(s.ToArray());
 }}
}
