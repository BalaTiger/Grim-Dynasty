using System;
using System.IO;
using System.Text;
using System.Reflection;
using GrimDynasty.Offline;

namespace GrimDynasty.Session {
 public static class SessionProgram {
  public static int Main(string[] args) {
   Console.OutputEncoding=new UTF8Encoding(false);
   string command=args.Length==0?"help":args[0];
   CommandResult result;
   try {
    var store=new SessionStore(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"runtime-data","saves"));
    SessionState session;
    switch(command){
     case "help":case "--help":
      Require(args.Length<=1,"help takes no arguments");
      result=CommandResult.Known("help",new{commands=new[]{"gd-session.exe new <slot> <character-name>","gd-session.exe show <slot>","gd-session.exe act <slot> accept-delivery|deliver|accept-return|return-item","gd-session.exe recover <slot>"},
       scenario="Liaoxi supply delivery: own character and two-task inventory-cycle scenario",save_location="runtime-data/saves beside this executable; project-owned format only",
       scope="Headless own session over verified task/inventory/money rules. No original game save, native character factory, XP or arbitrary world state."});break;
     case "new":
      Require(args.Length==3,"new <slot> <character-name>");
      session=SessionState.Create(args[2]);store.Save(args[1],session);
      result=CommandResult.Known(command,session.View());break;
     case "show":
      Require(args.Length==2,"show <slot>");session=store.Load(args[1]);
      result=CommandResult.Known(command,session.View());break;
     case "act":
      Require(args.Length==3,"act <slot> <action>");session=store.Load(args[1]);
      var action=session.Execute(args[2]);
      if(action.Status!="Known")result=CommandResult.Unsupported(command,action.Reason,session.View());
      else {if(action.Changed)store.Save(args[1],session);result=CommandResult.Known(command,new{changed=action.Changed,state=session.View()});}
      break;
     case "recover":
      Require(args.Length==2,"recover <slot>");session=store.Recover(args[1]);
      result=CommandResult.Known(command,session.View());break;
     default:result=CommandResult.Invalid(command,"Unknown command; run help");break;
    }
   }catch(SessionUnsupportedException e){result=CommandResult.Unsupported(command,e.Message);}
   catch(Exception e){
    if(e is IOException||e is InvalidDataException||e is ArgumentException||e is UnauthorizedAccessException||e is OverflowException)
     result=CommandResult.Invalid(command,e.GetType().Name+": "+e.Message);
    else result=new CommandResult{status="InternalError",command=command,reason=e.GetType().Name+": "+e.Message};
   }
   result.schema="grim-dynasty-session-v1";
   Console.WriteLine(JsonOutput.Serialize(result));
   return result.status=="Known"?0:result.status=="Unsupported"?3:result.status=="InvalidInput"?2:1;
  }
  static void Require(bool condition,string usage){if(!condition)throw new ArgumentException(usage);}
 }
}
