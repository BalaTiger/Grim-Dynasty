using System;
using System.Collections.Generic;
using GrimDynasty.TokenActions;
namespace GrimDynasty.Rewards {
 public sealed class CinematicMessage {
  public string Title,Text;
  public int Type;
  public uint RedBits,GreenBits,BlueBits,AlphaBits;
  public CinematicMessage Copy(){return (CinematicMessage)MemberwiseClone();}
 }
 public sealed class CinematicTextQueue {
  public bool StateKnown=true;
  public readonly List<CinematicMessage> Messages=new List<CinematicMessage>();
  public CinematicTextQueue Copy(){var c=new CinematicTextQueue{StateKnown=StateKnown};foreach(var m in Messages)c.Messages.Add(m==null?null:m.Copy());return c;}
  public MutationResult Show(string title,string text,int type,uint[] color,bool front){
   if(!StateKnown||title==null||text==null||color==null||color.Length!=4)return MutationResult.Unsupported("UnknownCinematicQueueOrMessage");
   foreach(var m in Messages)if(m==null||m.Title==null||m.Text==null)return MutationResult.Unsupported("PartialCinematicQueueEntry");
   var entry=new CinematicMessage{Title=title,Text=text,Type=type,RedBits=color[0],GreenBits=color[1],BlueBits=color[2],AlphaBits=color[3]};
   if(front)Messages.Insert(0,entry);else Messages.Add(entry);return MutationResult.Known(true);
  }
 }
 public sealed class LevelDisplaySnapshot {
  public CinematicTextQueue Queue;
  public string LocalizedTitle;
  public readonly Dictionary<uint,string> LocalizedReachedText=new Dictionary<uint,string>();
  public LevelDisplaySnapshot Copy(){var c=new LevelDisplaySnapshot{Queue=Queue==null?null:Queue.Copy(),LocalizedTitle=LocalizedTitle};foreach(var p in LocalizedReachedText)c.LocalizedReachedText.Add(p.Key,p.Value);return c;}
  static string CString(string s){if(s==null)return null;int zero=s.IndexOf('\0');return zero<0?s:s.Substring(0,zero);}
  public MutationResult EnqueueLevel(uint level){
   string reached;if(Queue==null||!LocalizedReachedText.TryGetValue(level,out reached))return MutationResult.Unsupported("UnknownFormattedLevelLocalization");
   // Caller constructs UTF-16 strings from Localize C strings before Show.
   return Queue.Show(CString(LocalizedTitle),CString(reached),5,new uint[]{0x3f800000,0x3f800000,0x3f800000,0x3f800000},false);
  }
 }
}
