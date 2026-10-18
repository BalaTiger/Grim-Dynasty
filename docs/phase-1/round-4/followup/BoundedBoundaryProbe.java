// @category GrimDynasty.Audit
import ghidra.app.script.GhidraScript;
import ghidra.app.cmd.disassemble.DisassembleCommand;
import ghidra.app.decompiler.*;
import ghidra.app.plugin.core.analysis.AutoAnalysisManager;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.nio.file.*;
import java.util.*;
import com.google.gson.GsonBuilder;

public class BoundedBoundaryProbe extends GhidraScript {
 public void run() throws Exception {
  String[] args=getScriptArgs();
  if(args.length!=2)throw new IllegalArgumentException("plan TSV and output JSON required");
  AutoAnalysisManager.getAnalysisManager(currentProgram).setIgnoreChanges(true);
  DecompInterface decompiler=new DecompInterface();decompiler.toggleCCode(true);decompiler.toggleSyntaxTree(false);
  decompiler.setSimplificationStyle("decompile");decompiler.openProgram(currentProgram);
  List<Map<String,Object>> result=new ArrayList<>();
  try {
   for(String line:Files.readAllLines(Path.of(args[0]))) {
    if(line.isBlank())continue;
    String[] col=line.split("\t",3);Address start=toAddr(col[0]),end=toAddr(col[1]);
    AddressSet body=new AddressSet(start,end);
    if(body.getNumAddresses()>2048)throw new IllegalArgumentException("function budget exceeded");
    DisassembleCommand cmd=new DisassembleCommand(start,body,true);cmd.applyTo(currentProgram,monitor);
    Function fn=currentProgram.getFunctionManager().getFunctionAt(start);
    if(fn==null)fn=currentProgram.getFunctionManager().createFunction(null,start,body,SourceType.ANALYSIS);
    else fn.setBody(body);
    Map<String,Object> row=new LinkedHashMap<>();row.put("label",col[2]);row.put("start",start.toString());row.put("end",end.toString());row.put("bytesBudget",body.getNumAddresses());row.put("function",fn.getName());
    List<Map<String,String>> refs=new ArrayList<>();List<String> listing=new ArrayList<>();
    InstructionIterator instructions=currentProgram.getListing().getInstructions(body,true);
    while(instructions.hasNext()) {
     Instruction ins=instructions.next();listing.add(ins.getAddress()+" "+ins.toString());
     for(Reference ref:ins.getReferencesFrom()) {
      Symbol sym=currentProgram.getSymbolTable().getPrimarySymbol(ref.getToAddress());
      Map<String,String> r=new LinkedHashMap<>();r.put("from",ins.getAddress().toString());r.put("to",ref.getToAddress().toString());r.put("type",ref.getReferenceType().toString());r.put("symbol",sym==null?null:sym.getName(true));
      Data d=currentProgram.getListing().getDefinedDataAt(ref.getToAddress());
      if(d!=null&&d.hasStringValue())r.put("definedString",String.valueOf(d.getValue()));refs.add(r);
     }
    }
    row.put("instructions",listing);row.put("outgoingReferences",refs);
    // Declare out-of-range call/jump targets as opaque stubs so the decompiler
    // cannot absorb an unknown tail-called implementation into this function.
    for(Map<String,String> r:refs) {
     if(!r.get("type").contains("CALL")&&!r.get("type").contains("JUMP"))continue;
     Address a=toAddr(r.get("to"));
     if(!body.contains(a)&&currentProgram.getMemory().getBlock(a)!=null&&currentProgram.getMemory().getBlock(a).isExecute()&&currentProgram.getFunctionManager().getFunctionContaining(a)==null) {
      currentProgram.getFunctionManager().createFunction(null,a,new AddressSet(a),SourceType.ANALYSIS);
     }
    }
    decompiler.flushCache();
    DecompileResults dec=decompiler.decompileFunction(fn,20,monitor);
    row.put("decompileCompleted",dec.decompileCompleted());row.put("error",dec.getErrorMessage());
    row.put("pseudocode",dec.getDecompiledFunction()==null?null:dec.getDecompiledFunction().getC());
    row.put("limitations","No auto-analysis; only selected bounded bodies disassembled/decompiled. Callee bodies and incoming references outside those ranges are not analyzed; inferred types/calling signatures remain uncertain.");
    result.add(row);println("BOUNDARY_PROBE "+start+" completed="+dec.decompileCompleted());
   }
  } finally {decompiler.dispose();}
  Files.writeString(Path.of(args[1]),new GsonBuilder().setPrettyPrinting().create().toJson(result));
 }
}
