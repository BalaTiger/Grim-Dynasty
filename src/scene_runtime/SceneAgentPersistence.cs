using System;
using System.Collections.Generic;
using System.Drawing;
using GrimDynasty.Session;

namespace GrimDynasty.Scene {
 // Authored visual patrol configuration. These pixel-space choices are not
 // native movement balance, a Monster initializer, or a combat stat profile.
 internal static class SceneAgentPersistence {
  internal static SceneAgents Create(SceneWorld world){
   var agents=new SceneAgents(world);
   agents.Add(new SceneAgentSpec("road-patrol","ming/liaoxi/visual-road-patrol",
    new PointF(1000,1050),new[]{new PointF(1000,1050),new PointF(1240,850)},
    95,180,380,1));
   return agents;
  }
  static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
  internal static object Encode(SceneAgents agents){
   var rows=new List<object>();
   foreach(var s in agents.GetSnapshots())rows.Add(new Dictionary<string,object>{
    {"id",s.Id},{"record",s.RecordName},{"x_bits",Bits(s.X)},{"y_bits",Bits(s.Y)},
    {"fx_bits",Bits(s.FacingX)},{"fy_bits",Bits(s.FacingY)},{"mode",(int)s.Mode},
    {"intent",(int)s.Intent},{"target",s.TargetId},{"blocked",s.Blocked},{"patrol_index",s.PatrolIndex}});
   return rows;
  }
  static object Field(Dictionary<string,object> row,string key){object value;if(!row.TryGetValue(key,out value))throw new SessionCorruptException("Missing agent field: "+key);return value;}
  static long Integer(Dictionary<string,object> row,string key){var v=Field(row,key);if(!(v is long))throw new SessionCorruptException("Agent integer field: "+key);return (long)v;}
  static string Text(Dictionary<string,object> row,string key,bool nullable){var v=Field(row,key);if(v==null&&nullable)return null;if(!(v is string))throw new SessionCorruptException("Agent text field: "+key);return (string)v;}
  static float Float(Dictionary<string,object> row,string key){long v=Integer(row,key);if(v<0||v>UInt32.MaxValue)throw new SessionCorruptException("Agent float bits: "+key);return BitConverter.ToSingle(BitConverter.GetBytes((uint)v),0);}
  internal static void Restore(SceneAgents agents,object encoded,string characterId){
   var rows=encoded as List<object>;if(rows==null||rows.Count>SceneAgents.MaxAgents)throw new SessionCorruptException("Scene agents array invalid");
   var result=new List<SceneAgentSnapshot>();
   foreach(var value in rows){
    var row=value as Dictionary<string,object>;if(row==null||row.Count!=11)throw new SessionCorruptException("Scene agent fields invalid");
    long mode=Integer(row,"mode"),intent=Integer(row,"intent"),index=Integer(row,"patrol_index");
    if(mode<0||mode>2||intent<0||intent>3||index<0||index>127)throw new SessionCorruptException("Scene agent enum/index invalid");
    object blocked=Field(row,"blocked");if(!(blocked is bool))throw new SessionCorruptException("Scene agent blocked flag invalid");
    string target=Text(row,"target",true);if(target!=null&&target!=characterId)throw new SessionCorruptException("Scene agent target belongs to another character");
    result.Add(new SceneAgentSnapshot(Text(row,"id",false),Text(row,"record",false),Float(row,"x_bits"),Float(row,"y_bits"),
     Float(row,"fx_bits"),Float(row,"fy_bits"),(SceneAgentMode)mode,(SceneAgentIntent)intent,target,(bool)blocked,(int)index));
   }
   try{agents.RestoreSnapshots(result.ToArray());}catch(ArgumentException ex){throw new SessionCorruptException("Scene agent snapshot invalid: "+ex.Message);}
  }
 }
}
