// Uses the already-installed, pinned Microsoft static dumper. Never loads the target.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const dir=path.join(root,'docs/object-lifecycle/offline-cache-node-inspection');
const extraction=JSON.parse(fs.readFileSync(path.join(dir,'extraction.json'),'utf8'));
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const hash=b=>createHash('sha256').update(b).digest('hex');
assert.equal(hash(fs.readFileSync(tool)),toolHash);
assert.equal(extraction.status,'extracted-fixed-functions');
const copy=path.join(root,'.tooling/offline-cache-node/Engine.readonly.dll');
assert.equal(extraction.readonly_copy,copy);
function hashCopy(){const h=createHash('sha256'),fd=fs.openSync(copy,'r'),buf=Buffer.alloc(65536);try{let n;while((n=fs.readSync(fd,buf,0,buf.length,null))>0)h.update(buf.subarray(0,n));}finally{fs.closeSync(fd);}return h.digest('hex');}
assert.equal(hashCopy(),expected);
const specs=[
 {label:'01f170',start:0x18001f170n,end:0x18001f1b4n,bytes:extraction.functions[0].hex_bytes},
 {label:'022860',start:0x180022860n,end:0x1800228e1n,bytes:extraction.functions[1].hex_bytes},
 {label:'05deb0-caller',start:0x18005deb0n,end:0x18005df0cn,bytes:extraction.caller.hex_bytes}
];
const temp=path.join(root,'.tooling/offline-cache-node/temp');fs.mkdirSync(temp,{recursive:true});
const results=[];
for(const s of specs){
 const outputPath=path.join(dir,s.label+'-dumpbin.txt');
 assert.ok(!fs.existsSync(outputPath),'Preserve previous output');
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:0x${s.start.toString(16)},0x${(s.end-1n).toString(16)}`,copy];
 const started=Date.now(),r=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:65536,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 assert.equal(r.error,undefined,'Static tool failed');assert.equal(r.status,0,'Static tool nonzero exit');
 assert.equal(r.stderr,'','Unexpected stderr');
 const rows=[];
 for(const line of r.stdout.split(/\r?\n/)){
  const match=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
  if(match){rows.push({address:BigInt('0x'+match[1]),bytes:match[2].replace(/\s/g,'').toLowerCase(),text:match[3].trim()});continue;}
  const continued=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
  if(continued){assert.ok(rows.length>0);rows.at(-1).bytes+=continued[1].replace(/\s/g,'').toLowerCase();}
 }
 assert.ok(rows.length>0);let cursor=s.start;
 for(const row of rows){assert.equal(row.address,cursor,'Gap or out-of-range disassembly');assert.ok(row.bytes.length>0&&row.bytes.length%2===0);cursor+=BigInt(row.bytes.length/2);assert.ok(cursor<=s.end,'Out-of-range decode');}
 assert.equal(cursor,s.end,'Incomplete function byte coverage');
 assert.equal(rows.map(x=>x.bytes).join(''),s.bytes,'Decoded bytes differ from fixed extraction');
 fs.writeFileSync(outputPath,r.stdout,{flag:'wx'});
 const normalized=rows.map(x=>({address:'0x'+x.address.toString(16),bytes:x.bytes,instruction:x.text}));
 fs.writeFileSync(path.join(dir,s.label+'-instructions.json'),JSON.stringify(normalized,null,2)+'\n',{flag:'wx'});
 results.push({label:s.label,start_va:'0x'+s.start.toString(16),end_va_exclusive:'0x'+s.end.toString(16),bytes:s.bytes.length/2,instructions:rows.length,elapsed_ms:Date.now()-started,arguments:args,exit_code:0,output_sha256:hash(Buffer.from(r.stdout)),byte_coverage_verified:true});
}
assert.equal(hashCopy(),expected,'Read-only copy changed');
const result={utc:new Date().toISOString(),status:'bounded-disassembly-verified',tool,tool_sha256:toolHash,version:'14.44.35229.0',installed_signature_verified:'Microsoft Corporation; Authenticode Valid (read-only verification)',target_copy_sha256_before:expected,target_copy_sha256_after:expected,results,original_loaded_or_executed:false,rea_or_java_started:false,installations:false,permission_escalation:false,symbol_server_access:false,preliminary_invocations:{help:1,two_single_function_range_probes:2},limits:{per_invocation_timeout_ms:10000,max_output_bytes:65536,total_selected_code_bytes:289},controller_resource_usage:process.resourceUsage()};
fs.writeFileSync(path.join(dir,'disassembly-summary.json'),JSON.stringify(result,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({status:result.status,functions:results.map(({label,bytes,instructions,elapsed_ms})=>({label,bytes,instructions,elapsed_ms})),copy_unchanged:true}));
