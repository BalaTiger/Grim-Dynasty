// One actual ClassInfo virtual-slot target; fixed unwind fragment, 76 bytes.
// Only the installed dumpbin runs. Game.dll is read as a file, never loaded.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
assert.equal(process.argv.length,2);
const root='C:/Users/zhuzi/Documents/Grim-Dynasty';
process.chdir(root);
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const expectedTool='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out='docs/object-lifecycle/offline-classinfo-slot',temp='.tooling/offline-classinfo-slot-temp';
const sources=[
 ['docs/object-lifecycle/offline-class-registration-metadata/metadata.json','54aa510cffe5a281c54e6de4a594f40224585822f0b40cad6c847f8318ef76b7'],
 ['docs/object-lifecycle/offline-questitem-boundaries/boundary.json','fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05']
];
const plans=[{source:0,index:0,role:'ClassInfo-actual-virtual-slot0',begin:0x591430,end:0x59147c}];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function fileHash(p){
 const f=fs.openSync(p,'r'),b=Buffer.alloc(65536),h=createHash('sha256');
 try{let n;while((n=fs.readSync(f,b,0,b.length,null))>0)h.update(b.subarray(0,n));}
 finally{fs.closeSync(f);}return h.digest('hex');
}
const values=sources.map(([p,h])=>{const b=fs.readFileSync(p);assert.equal(hash(b),h);return JSON.parse(b);});
assert.equal(values[0].status,'fixed-class-registration-metadata-verified');
const proof=values[0].files[0];
assert.equal(proof.source_hash_preserved,true);
assert.equal(proof.sha256_before,expected);assert.equal(proof.sha256_after,expected);
const proofs=[{...proof,pe:values[1].pe}];
for(const p of plans){const e=proofs[0].boundaries[0];assert.equal(e.begin_rva,hex(p.begin));assert.equal(e.end_rva_exclusive,hex(p.end));}
for(const d of [root,'docs','docs/object-lifecycle','.tooling'])assert(!fs.lstatSync(d).isSymbolicLink());
assert(!fs.existsSync(out));assert(!fs.existsSync(temp));
const toolBefore=fileHash(tool);assert.equal(toolBefore,expectedTool);
fs.mkdirSync(out);fs.mkdirSync(temp);
const save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
const report={status:'started',started_utc:new Date().toISOString(),original,expected_sha256:expected,
 source_evidence:sources.map(([p,h])=>({path:p,sha256:h})),tool,tool_sha256_before:toolBefore,
 code_byte_limit:76,code_bytes:0,metadata_recheck_byte_limit:12,metadata_bytes:0,fragments:[],
 target_loaded_or_executed:false,backend_started:false,original_modified:false,full_target_copy_created:false,
 dumpbin_input:'Original file read directly; no DLL loading or new whole-file copy',
 scope:'One actual ClassInfo vtable target; no inference of its role before inspection, no callee traversal',
 hash_scope:'64KiB streaming full-file SHA256 before and after; separate from code/metadata byte totals',
 dumpbin_io_scope:'RANGE limits output; internal dumpbin metadata I/O is not instrumented'};
let fd,beforeStat;const failures=[];
try{
 report.original_sha256_before=fileHash(original);assert.equal(report.original_sha256_before,expected);
 fd=fs.openSync(original,'r');beforeStat=fs.fstatSync(fd);assert.equal(beforeStat.size,11385856);
 function read(at,n){
  assert(Number.isSafeInteger(at)&&Number.isSafeInteger(n)&&at>=0&&n>0&&at+n<=beforeStat.size);
  const b=Buffer.alloc(n);let done=0;while(done<n){const z=fs.readSync(fd,b,done,n-done,at+done);assert(z>0);done+=z;}return b;
 }
 for(const p of plans){
  const proof=proofs[p.source],e=proof.boundaries[p.index];
  const pdata=proof.pe.sections.find(s=>s.name==='.pdata'),entryRva=Number(e.exception_entry_rva);
  assert(entryRva>=pdata.rva&&entryRva+12<=pdata.rva+pdata.raw_size);
  const data=read(pdata.raw_offset+entryRva-pdata.rva,12);report.metadata_bytes+=12;
  assert.equal(data.readUInt32LE(0),p.begin);assert.equal(data.readUInt32LE(4),p.end);
  assert.equal(hex(data.readUInt32LE(8)),e.unwind_info_rva);
  const size=p.end-p.begin;assert(report.code_bytes+size<=76);
  const bytes=read(Number(e.file_offset),size);report.code_bytes+=size;
  const start=0x180000000n+BigInt(p.begin),end=0x180000000n+BigInt(p.end),label=p.begin.toString(16);
  save(hex(start)+'.bin',bytes);
  const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(start)},${hex(end-1n)}`,original];
  const fragment={role:p.role,start_va:hex(start),end_va_exclusive:hex(end),code_bytes:size,
   code_sha256:hash(bytes),command_arguments:args};report.fragments.push(fragment);
  const launched=Date.now();
  const r=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:196608,
   env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:path.resolve(temp),TMP:path.resolve(temp)}});
  fragment.tool_elapsed_ms=Date.now()-launched;fragment.tool_exit=r.status;fragment.tool_error=r.error?String(r.error):null;
  save(label+'-dumpbin.txt',r.stdout||'');save(label+'-stderr.txt',r.stderr||'');
  assert.equal(r.error,undefined);assert.equal(r.status,0);assert.equal(r.stderr,'');
  const rows=[];
  for(const line of r.stdout.split(/\r?\n/)){
   const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
   if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
   const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
   if(c){assert(rows.length);rows.at(-1).bytes+=c[1].replace(/\s/g,'').toLowerCase();}
  }
  let cursor=start;
  for(const r of rows){assert(r.instruction&&r.bytes.length>0&&r.bytes.length%2===0&&r.bytes.length<=30);
   assert.equal(r.address,cursor);cursor+=BigInt(r.bytes.length/2);assert(cursor<=end);}
  assert.equal(cursor,end);assert.equal(rows.map(r=>r.bytes).join(''),bytes.toString('hex'));
  save(label+'-instructions.json',JSON.stringify(rows.map(r=>({...r,address:hex(r.address)})),null,2)+'\n');
  fragment.instructions=rows.length;fragment.byte_coverage_verified=true;
 }
 assert.equal(report.code_bytes,76);assert.equal(report.metadata_bytes,12);
}catch(e){failures.push(String(e));}
finally{
 if(fd!==undefined)fs.closeSync(fd);
 for(const [field,p,wanted]of [['original_sha256_after',original,expected],['tool_sha256_after',tool,expectedTool]]){
  try{report[field]=fileHash(p);assert.equal(report[field],wanted);}catch(e){failures.push(String(e));}
 }
 if(beforeStat){const after=fs.statSync(original);report.original_stat_stable=after.size===beforeStat.size&&after.mtimeMs===beforeStat.mtimeMs&&after.ctimeMs===beforeStat.ctimeMs;if(!report.original_stat_stable)failures.push('Original stat changed');}
 report.original_hash_matches=report.original_sha256_before===expected&&report.original_sha256_after===expected;
 report.tool_hash_matches=report.tool_sha256_after===expectedTool;
 report.status=failures.length?'failed':'fixed-classinfo-slot-disassembly-verified';report.failures=failures;
 report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
 save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,bytes:report.code_bytes,
 instructions:report.fragments.reduce((n,f)=>n+(f.instructions||0),0),original_unchanged:report.original_hash_matches,
 tool_unchanged:report.tool_hash_matches,controller_peak_rss_kib:report.controller_resource_usage.maxRSS}));
if(failures.length)process.exitCode=1;
