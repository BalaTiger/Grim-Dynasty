using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using GrimDynasty.Quests;
public static class OwnInventoryCycleFixture {
 static void Text(BinaryWriter w,string s){var b=Encoding.UTF8.GetBytes(s);w.Write((uint)b.Length);w.Write(b);}
 public static Dictionary<string,object> Read(){using(var s=new MemoryStream())using(var w=new BinaryWriter(s)){
  w.Write(Encoding.ASCII.GetBytes("QST2"));w.Write(9u);w.Write(0u);w.Write(3u);w.Write(0u);w.Write(2u);
  for(int i=0;i<2;i++){
   w.Write((uint)i+1);w.Write(0u);w.Write(0u);w.Write(1u); // task, flags, accept count, objective count
   w.Write(1u);w.Write(0u);w.Write(1u);w.Write(0);w.Write(9);w.Write((byte)1);w.Write(i==0?1:3);Text(w,"ming/items/military_ration.dbr");w.Write(i==0?1u:2u);w.Write(true);w.Write(0u);
   w.Write(1u);w.Write(0u);w.Write(0u);w.Write(1u);w.Write(4);w.Write((byte)0);w.Write(i==0?75:40);w.Write(false);w.Write(true);
  }
  long locale=s.Position;w.Write(1u);Text(w,"zh");w.Write(10u);foreach(string text in new[]{"军需署","军粮出入库","解送行粮","从三份中交出至少两份","保留一份以下","解送验收","领回封存行粮","重新持有至少两份","已有物品领回","领回验收"})Text(w,text);
  w.Flush();s.Position=8;w.Write((uint)locale);w.Flush();return QuestDefinition.Read(s.ToArray());
 }}
}
