import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import com.google.gson.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
public class FindEventRegistrations extends GhidraScript {
 public void run() throws Exception {
  Path output=Paths.get(getScriptArgs()[0]);if(Files.exists(output))throw new IllegalStateException("Evidence exists");
  String target=getScriptArgs().length>1?getScriptArgs()[1]:"0x1806090b8";
  JsonArray rows=new JsonArray();ReferenceIterator refs=currentProgram.getReferenceManager().getReferencesTo(toAddr(target));
  while(refs.hasNext()){
   Reference ref=refs.next();Address at=ref.getFromAddress();Function f=getFunctionContaining(at);JsonObject row=new JsonObject();row.addProperty("callsite",at.toString());
   row.addProperty("function",f==null?null:f.getEntryPoint().toString());row.addProperty("name",f==null?null:f.getName());
   Instruction ins=getInstructionAt(at);for(int i=0;i<18&&ins!=null;i++)ins=ins.getPrevious();
   JsonArray a=new JsonArray();for(int i=0;i<22&&ins!=null;i++,ins=ins.getNext())a.add(ins.getAddress()+": "+ins.toString());row.add("window",a);rows.add(row);
  }
  JsonObject root=new JsonObject();root.addProperty("source","Direct cached Ghidra cross references to observed Engine EventManager::Register IAT; not Rea");root.add("registrations",rows);
  Files.writeString(output,new GsonBuilder().setPrettyPrinting().create().toJson(root),StandardCharsets.UTF_8,StandardOpenOption.CREATE_NEW);
 }
}
