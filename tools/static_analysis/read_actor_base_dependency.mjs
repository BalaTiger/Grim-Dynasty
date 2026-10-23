// Prepared only. One explicit direct callee; no name/inheritance assumption.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632;
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-actor-base-dependency'),temp=path.join(root,'.tooling/offline-actor-base-dependency-temp');
const sources=[
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-actor-constructor-body/0259a0-instructions.json','59f402f428e04e2928bb485e58fa85896ac08db4548bff6fa33444a58fa521a7']
];
const target=0x7d020,base=0x180000000n,hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const limits={code_bytes:2048,metadata_bytes:384,search_steps:32,proof_bytes:131072,fingerprint_chunk_bytes:65536,tool_timeout_ms:10000,tool_max_buffer_bytes:196608};
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink(),'Redirected path refused');
 assert(first&&file?s.isFile():s.isDirectory());const d=path.dirname(c);if(d===c)return;c=d;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output/temp path refused');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.ino===b.ino&&a.dev===b.dev&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
function fingerprint(fd,length){const b=Buffer.alloc(limits.fingerprint_chunk_bytes),h=createHash('sha256');let at=0;
 while(at<length){const n=fs.readSync(fd,b,0,Math.min(b.length,length-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;}
 return {sha256:h.digest('hex'),bytes:at,chunk_bytes:b.length};}
function hashFile(p){plain(p,true);const f=fs.openSync(p,fs.constants.O_RDONLY);try{const s=fs.fstatSync(f);assert(s.isFile());
 const r=fingerprint(f,s.size);assert(stable(s,fs.fstatSync(f)));return r;}finally{fs.closeSync(f);}}
plain(path.dirname(out));plain(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([p,h])=>{const full=path.join(root,p);plain(full,true);assert(fs.statSync(full).size<=limits.proof_bytes);
 const b=fs.readFileSync(full);assert(b.length<=limits.proof_bytes);assert.equal(hash(b),h);return JSON.parse(b.toString('utf8'));});
const proof=proofs[0],pe=proof.pe;
assert.equal(proof.status,'exact-actor-export-and-boundary-verified');assert.equal(proof.original,original);assert.equal(proof.expected_sha256,expected);
assert.equal(proof.sha256_before,expected);assert.equal(proof.sha256_after,expected);assert(proof.source_stat_stable&&proof.source_hash_preserved);assert.deepEqual(proof.failures,[]);
const calls=proofs[1].filter(i=>i.address==='0x1800259bb');assert.equal(calls.length,1);
assert.equal(calls[0].bytes,'e860760500');assert.equal(calls[0].instruction,'call        000000018007D020');
const callBytes=Buffer.from(calls[0].bytes,'hex');assert.equal(0x259bb+5+callBytes.readInt32LE(1),target);
assert.equal(pe.image_base,hex(base));assert.equal(pe.entry_count,12210);assert.equal(pe.exception_rva,'0x424000');
assert.equal(pe.exception_bytes,146520);assert.equal(pe.exception_file_offset,4317696);assert.equal(pe.exception_bytes,pe.entry_count*12);
const report={schema:'grim-dynasty-fixed-actor-direct-dependency-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,expected_file_bytes:size,
 source_evidence:sources.map(([path,sha256])=>({path,sha256})),requested_rva:hex(target),source_call:calls[0],limits,
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,build_or_test_execution:false,
 original_modified:false,full_target_copy_created:false,permission_change:false,tool,expected_tool_sha256:toolHash,
 metadata_bytes:0,code_bytes:0,reads:[],fingerprint_passes:[],subprocesses:0,body_read:false,
 limitations:[
  'Only an exact BeginAddress for RVA 07D020, at most 2048 bytes, admits one dumpbin call; absent/oversize entries stop without body reading.',
  'At most 32 distinct 12-byte .pdata entries. Binary search relies on sorted-table format; only encountered ordering is checked.',
  'No neighboring code, callee, EH/unwind-body, import target or additional function is followed; function identity is not inferred.',
  'Full-file fingerprints use 64KiB buffers and are separate from semantic byte budgets. dumpbin internal metadata I/O is not instrumented.',
  'The target is only data; .pdata and instruction byte coverage do not prove complete logical control flow or exception behavior.',
  'Before/after same-handle hash/stat stability is not an atomic snapshot.'
 ]};
let fd,stat,outCreated=false,toolAttempted=false;const failures=[],cache=new Map();
const save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
function read(at,n,kind){assert(Number.isSafeInteger(at)&&at>=0&&Number.isSafeInteger(n)&&n>0&&at+n<=size);
 if(kind==='pdata'){assert.equal(n,12);assert(report.metadata_bytes+n<=limits.metadata_bytes);}
 else{assert.equal(kind,'code');assert(n<=limits.code_bytes);assert.equal(report.code_bytes,0);}
 const b=Buffer.alloc(n),r={kind,file_offset:at,requested_bytes:n,read_bytes:0};report.reads.push(r);
 while(r.read_bytes<n){const k=fs.readSync(fd,b,r.read_bytes,n-r.read_bytes,at+r.read_bytes);assert(k>0);r.read_bytes+=k;
  if(kind==='pdata')report.metadata_bytes+=k;else report.code_bytes+=k;}
 if(kind==='pdata')r.raw_hex=b.toString('hex');else r.sha256=hash(b);return b;
}
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);
 return {section:ss[0],offset:ss[0].raw_offset+rva-ss[0].rva};}
try{
 fs.mkdirSync(out);outCreated=true;fs.mkdirSync(temp);plain(out);plain(temp);report.script_sha256=hashFile(script).sha256;
 plain(tool,true);toolAttempted=true;report.tool_before=hashFile(tool);assert.equal(report.tool_before.sha256,toolHash);
 plain(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);assert(stat.isFile()&&stat.size===size);
 const before=fingerprint(fd,size);report.fingerprint_passes.push({label:'before',...before});report.sha256_before=before.sha256;assert.equal(before.sha256,expected);
 const pd=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pd.section.name,'.pdata');assert.equal(pd.offset,pe.exception_file_offset);
 function entry(i){assert(Number.isInteger(i)&&i>=0&&i<pe.entry_count);if(cache.has(i))return cache.get(i);assert(cache.size<32);
  const b=read(pd.offset+i*12,12,'pdata'),e={index:i,begin:b.readUInt32LE(0),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8),raw_hex:b.toString('hex')};
  assert(e.begin<e.end&&e.end<=pe.image_size);for(const [j,o]of cache)assert(j<i?o.begin<=e.begin:o.begin>=e.begin);cache.set(i,e);return e;}
 let lo=0,hi=pe.entry_count,steps=0;while(lo<hi){assert(steps++<limits.search_steps);const mid=lo+Math.floor((hi-lo)/2),e=entry(mid);if(e.begin<target)lo=mid+1;else hi=mid;}
 report.search_steps=steps;report.lower_bound_index=lo;const e=lo<pe.entry_count?entry(lo):null;report.boundary=e;
 if(!e||e.begin!==target){report.status='no-exact-entry-body-not-read';report.not_read_reason='No exact BeginAddress; no containing or neighboring body selected';}
 else if(e.end-e.begin>limits.code_bytes){report.status='exact-boundary-exceeds-body-budget';report.not_read_reason='Exact body exceeds 2048 bytes';}
 else{
  const mapped=map(e.begin,e.end-e.begin);assert.equal(mapped.section.name,'.text');assert(mapped.section.characteristics&0x20000000);
  report.code_file_offset=mapped.offset;const bytes=read(mapped.offset,e.end-e.begin,'code');report.body_read=true;report.code_sha256=hash(bytes);save('07d020.bin',bytes);
  const start=base+BigInt(e.begin),end=base+BigInt(e.end),args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(start)},${hex(end-1n)}`,original];
  report.command_arguments=args;report.subprocesses=1;const started=Date.now();
  const child=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:limits.tool_timeout_ms,maxBuffer:limits.tool_max_buffer_bytes,
   env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
  report.tool_elapsed_ms=Date.now()-started;report.tool_exit=child.status;report.tool_signal=child.signal;report.tool_error=child.error?String(child.error):null;
  save('07d020-dumpbin.txt',child.stdout||'');save('07d020-stderr.txt',child.stderr||'');
  assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
  const rows=[];for(const line of child.stdout.split(/\r?\n/)){
   const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
   if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
   const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
  let cursor=start;for(const row of rows){assert(row.instruction&&row.bytes.length>0&&row.bytes.length<=30&&row.bytes.length%2===0);
   assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=end);}
  assert.equal(cursor,end);assert.equal(rows.map(r=>r.bytes).join(''),bytes.toString('hex'));report.byte_coverage_verified=true;report.instructions=rows.length;
  save('07d020-instructions.json',JSON.stringify(rows.map(r=>({...r,address:hex(r.address)})),null,2)+'\n');report.status='fixed-actor-direct-dependency-disassembly-verified';
 }
}catch(error){failures.push(String(error));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){const after=fingerprint(fd,size);report.fingerprint_passes.push({label:'after',...after});report.sha256_after=after.sha256;
  assert.equal(after.sha256,expected);report.source_stat_stable=stable(stat,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){failures.push('Original final verification: '+String(e));}finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(e){failures.push('Original close: '+String(e));}}}
 if(toolAttempted){try{report.tool_after=hashFile(tool);assert.equal(report.tool_after.sha256,toolHash);}catch(e){failures.push('Tool final verification: '+String(e));}}
 report.entries=[...cache.values()];report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;
 report.tool_hash_preserved=report.tool_before?.sha256===toolHash&&report.tool_after?.sha256===toolHash;
 if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
 if(outCreated){plain(out);save('inspection.json',JSON.stringify(report,null,2)+'\n');}
}
console.log(JSON.stringify({status:report.status,boundary:report.boundary,metadata_bytes:report.metadata_bytes,code_bytes:report.code_bytes,instructions:report.instructions,
 original_unchanged:report.source_hash_preserved,tool_unchanged:report.tool_hash_preserved,subprocesses:report.subprocesses,failures}));
if(failures.length)process.exitCode=1;
