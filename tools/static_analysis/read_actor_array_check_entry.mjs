// Prepared only: fixed 8-byte interval; not a complete-function assertion.
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
const out=path.join(root,'docs/object-lifecycle/offline-actor-array-check-entry'),temp=path.join(root,'.tooling/offline-actor-array-check-entry-temp');
const sources=[
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-actor-array-check/inspection.json','08dd49f6992288b2dc6557d05521561d7bdde0a865baa3fb1e3522492488d3e5'],
 ['docs/object-lifecycle/offline-actor-198-array-helpers/2dcc3c-instructions.json','a8c49acbfc80c793124df217df0aace5fb16786e4aa13ba660739d5751d794c3']
];
const begin=0x2dd4c0,end=0x2dd4c8,base=0x180000000n,hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink(),'Redirected path refused');assert(first&&file?s.isFile():s.isDirectory());const d=path.dirname(c);if(d===c)return;c=d;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output/temp path refused');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.ino===b.ino&&a.dev===b.dev&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
function fingerprint(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');let at=0;while(at<n){const k=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(k>0);h.update(b.subarray(0,k));at+=k;}return {sha256:h.digest('hex'),bytes:at,chunk_bytes:65536};}
function hashFile(p){plain(p,true);const f=fs.openSync(p,fs.constants.O_RDONLY);try{const s=fs.fstatSync(f),r=fingerprint(f,s.size);assert(stable(s,fs.fstatSync(f)));return r;}finally{fs.closeSync(f);}}
plain(path.dirname(out));plain(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([p,h])=>{const f=path.join(root,p);plain(f,true);assert(fs.statSync(f).size<=131072);const b=fs.readFileSync(f);assert(b.length<=131072);assert.equal(hash(b),h);return JSON.parse(b.toString('utf8'));});
const pe=proofs[0].pe,prior=proofs[1];
assert.equal(proofs[0].status,'exact-actor-export-and-boundary-verified');assert.equal(pe.image_base,hex(base));
for(const p of proofs.slice(0,2)){assert.equal(p.original,original);assert.equal(p.expected_sha256,expected);assert.equal(p.sha256_before,expected);assert.equal(p.sha256_after,expected);assert(p.source_hash_preserved&&p.source_stat_stable);assert.deepEqual(p.failures,[]);}
assert.equal(prior.status,'no-exact-entry-body-not-read');assert.equal(prior.requested_rva,'0x2dd4c0');assert.equal(prior.body_read,false);assert.equal(prior.code_bytes,0);
assert.equal(prior.boundary.begin,end);assert.equal(prior.boundary.index,11194);assert.equal(prior.boundary.raw_hex,'c8d42d0007d52d00a0213600');
const calls=proofs[2].filter(r=>r.address==='0x1802dcc75');assert.equal(calls.length,1);assert.equal(calls[0].bytes,'e846080000');assert.equal(calls[0].instruction,'call        00000001802DD4C0');
assert.equal(0x2dcc75+5+Buffer.from(calls[0].bytes,'hex').readInt32LE(1),begin);
const report={schema:'grim-dynasty-actor-array-check-fixed-entry-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,expected_file_bytes:size,
 source_evidence:sources.map(([path,sha256])=>({path,sha256})),source_call:calls[0],prior_boundary:prior.boundary,begin_rva:hex(begin),end_rva_exclusive:hex(end),
 interval_kind:'fixed-bounded-interval',complete_function_claimed:false,limits:{code_bytes:8,metadata_bytes:0,fingerprint_chunk_bytes:65536},
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,build_or_test_execution:false,original_modified:false,full_target_copy_created:false,permission_change:false,
 tool,expected_tool_sha256:toolHash,metadata_bytes:0,code_bytes:0,subprocesses:0,reads:[],fingerprint_passes:[],indirect_jumps:[],
 limitations:['Only [2DD4C0,2DD4C8) is read; no new .pdata, endpoint byte, callee, import-slot contents, global or EH reads.',
  'RIP-relative indirect-slot addresses may be calculated from observed instruction bytes only; no binding or target behavior is inferred.',
  'This is an eight-byte interval, not a complete-function claim. Prior non-RET and no-exact-entry reports remain unchanged.',
  'Full-file fingerprints are separate from the semantic byte budget; dumpbin internal metadata I/O is not instrumented.',
  'Same-handle before/after hash/stat stability is not an atomic snapshot.']};
let fd,stat,outCreated=false,toolAttempted=false;const failures=[],save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);return {section:ss[0],offset:ss[0].raw_offset+rva-ss[0].rva};}
try{
 fs.mkdirSync(out);outCreated=true;fs.mkdirSync(temp);plain(out);plain(temp);report.script_sha256=hashFile(script).sha256;
 plain(tool,true);toolAttempted=true;report.tool_before=hashFile(tool);assert.equal(report.tool_before.sha256,toolHash);
 plain(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);assert(stat.isFile()&&stat.size===size);
 const before=fingerprint(fd,size);report.fingerprint_passes.push({label:'before',...before});report.sha256_before=before.sha256;assert.equal(before.sha256,expected);
 const mapped=map(begin,8);assert.equal(mapped.section.name,'.text');assert(mapped.section.characteristics&0x20000000);assert(mapped.offset>=0&&mapped.offset+8<=size);
 const bytes=Buffer.alloc(8),read={kind:'fixed-code-interval',file_offset:mapped.offset,requested_bytes:8,read_bytes:0};report.reads.push(read);
 while(read.read_bytes<8){const n=fs.readSync(fd,bytes,read.read_bytes,8-read.read_bytes,mapped.offset+read.read_bytes);assert(n>0);read.read_bytes+=n;report.code_bytes+=n;}
 report.code_file_offset=mapped.offset;report.code_sha256=hash(bytes);report.raw_hex=bytes.toString('hex');save('2dd4c0-entry.bin',bytes);
 const startVA=base+BigInt(begin),endVA=base+BigInt(end),args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(startVA)},${hex(endVA-1n)}`,original];
 report.command_arguments=args;report.subprocesses=1;const started=Date.now();
 const child=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:196608,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 report.tool_elapsed_ms=Date.now()-started;report.tool_exit=child.status;report.tool_signal=child.signal;report.tool_error=child.error?String(child.error):null;
 save('2dd4c0-entry-dumpbin.txt',child.stdout||'');save('2dd4c0-entry-stderr.txt',child.stderr||'');assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
 const rows=[];for(const line of child.stdout.split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
  if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
  const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
 let cursor=startVA;for(const row of rows){assert(row.instruction&&row.bytes.length>0&&row.bytes.length<=30&&row.bytes.length%2===0);assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=endVA);}
 assert.equal(cursor,endVA);assert.equal(rows.map(r=>r.bytes).join(''),bytes.toString('hex'));report.byte_coverage_verified=true;report.instructions=rows.length;
 for(const row of rows){const b=Buffer.from(row.bytes,'hex'),prefix=b[0]>=0x40&&b[0]<=0x4f?1:0;
  if(b[prefix]===0xff&&b[prefix+1]===0x25&&b.length===prefix+6){const slot=Number(row.address-base)+b.length+b.readInt32LE(prefix+2);assert(slot>=0&&slot+8<=pe.image_size);const m=map(slot,8);
   report.indirect_jumps.push({address:hex(row.address),encoding:row.bytes,slot_rva:hex(slot),slot_va:hex(base+BigInt(slot)),slot_section:m.section.name,slot_read:false,target_followed:false});}}
 save('2dd4c0-entry-instructions.json',JSON.stringify(rows.map(r=>({...r,address:hex(r.address)})),null,2)+'\n');report.status='fixed-eight-byte-interval-verified';
}catch(error){failures.push(String(error));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){const after=fingerprint(fd,size);report.fingerprint_passes.push({label:'after',...after});report.sha256_after=after.sha256;assert.equal(after.sha256,expected);report.source_stat_stable=stable(stat,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){failures.push('Original final verification: '+String(e));}finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(e){failures.push('Original close: '+String(e));}}}
 if(toolAttempted){try{report.tool_after=hashFile(tool);assert.equal(report.tool_after.sha256,toolHash);}catch(e){failures.push('Tool final verification: '+String(e));}}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;report.tool_hash_preserved=report.tool_before?.sha256===toolHash&&report.tool_after?.sha256===toolHash;
 if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();if(outCreated){plain(out);save('inspection.json',JSON.stringify(report,null,2)+'\n');}
}
console.log(JSON.stringify({status:report.status,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,instructions:report.instructions,indirect_jumps:report.indirect_jumps,
 original_unchanged:report.source_hash_preserved,tool_unchanged:report.tool_hash_preserved,subprocesses:report.subprocesses,failures}));
if(failures.length)process.exitCode=1;
