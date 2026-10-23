using System;

namespace GrimDynasty.Offline {
 public sealed class CommandResult {
  public string schema="grim-dynasty-offline-v1";
  public string status,command,reason;
  public object data;
  public static CommandResult Known(string command,object data){
   return new CommandResult{status="Known",command=command,data=data};
  }
  public static CommandResult Unsupported(string command,string reason,object data=null){
   return new CommandResult{status="Unsupported",command=command,reason=reason,data=data};
  }
  public static CommandResult Invalid(string command,string reason){
   return new CommandResult{status="InvalidInput",command=command,reason=reason};
  }
 }
}
