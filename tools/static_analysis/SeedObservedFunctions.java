import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
public class SeedObservedFunctions extends GhidraScript {
 public void run() throws Exception {
  for(String value:getScriptArgs()){
   Address address=toAddr(value);disassemble(address);Function f=getFunctionAt(address);
   if(f==null)f=createFunction(address,"Observed_"+value.replace("0x",""));
   println("Observed vtable/export entry "+address+": "+(f==null?"UNDEFINED":f.getName()));
  }
 }
}
