import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.cmd.function.CreateFunctionCmd;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import com.google.gson.*;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
public class ExportObservedFunctions extends GhidraScript {
 public void run() throws Exception {
  String[] args=getScriptArgs();Path output=Paths.get(args[0]);
  if(Files.exists(output))throw new IllegalStateException("Refusing to overwrite evidence");
  JsonArray rows=new JsonArray();DecompInterface d=new DecompInterface();d.openProgram(currentProgram);
  for(int i=1;i<args.length;i++){
   Address a=toAddr(args[i]);Function f=getFunctionAt(a);JsonObject row=new JsonObject();row.addProperty("observed_entry",a.toString());
   if(f==null){row.addProperty("status","undefined");rows.add(row);continue;}
   CreateFunctionCmd.fixupFunctionBody(currentProgram,f,monitor);
   row.addProperty("name",f.getName());row.addProperty("body",f.getBody().toString());
   JsonArray asm=new JsonArray();for(Instruction ins:currentProgram.getListing().getInstructions(f.getBody(),true))asm.add(ins.getAddress()+": "+ins.toString());row.add("assembly",asm);
   DecompileResults r=d.decompileFunction(f,60,monitor);row.addProperty("decompile_completed",r.decompileCompleted());
   row.addProperty("pseudocode",r.decompileCompleted()?r.getDecompiledFunction().getC():null);row.addProperty("diagnostic",r.getErrorMessage());rows.add(row);
  }
  d.dispose();JsonObject result=new JsonObject();result.addProperty("source","Existing Ghidra 12.1.4 with own observed-entry seed/export scripts; NOT a successful Rea MCP result");result.addProperty("program",currentProgram.getName());result.add("functions",rows);
  Files.writeString(output,new GsonBuilder().setPrettyPrinting().create().toJson(result),StandardCharsets.UTF_8,StandardOpenOption.CREATE_NEW);
 }
}
