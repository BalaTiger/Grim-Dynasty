// Prepared only: three fixed references from EBDE0; no transitive reads.
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
const out=path.join(root,'docs/object-lifecycle/offline-actor-198-array-helpers'),temp=path.join(root,'.tooling/offline-actor-198-array-helpers-temp');
const sources=[
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-actor-198-constructor/0ebde0-instructions.json','9083e6b9037ded4e5ee7224b5544ac2b9abae3b6a1ecca612e9d7663b8f484ce']
];
const specs=[
 {label:'2ddb14',target:0x2ddb14,maximum:2048,prefix:true,source:0xebe5b,raw:'e8b41c1f00',text:'call        00000001802DDB14',displacement:1,length:5},
 {label:'2dcc3c',target:0x2dcc3c,maximum:2048,prefix:false,source:0xebe85,raw:'e8b20d1f00',text:'call        00000001802DCC3C',displacement:1,length:5},
 {label:'0ebeb0',target:0xebeb0,maximum:512,prefix:false,source:0xebe73,raw:'4c8d0d36000000',text:'lea         r9,[00000001800EBEB0h]',displacement:3,length:7}
];
const base=0x180000000n,hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const limits={code_bytes:4096,metadata_bytes:1152,search_steps_per_target:32,entries_per_target:32,proof_bytes:131072,fingerprint_chunk_bytes:65536,tool_timeout_ms:10000,tool_max_buffer_bytes:196608};
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink(),'Redirected path refused');
 assert(first&&file?s.isFile():s.isDirectory());const d=path.dirname(c);if(d===c)return;c=d;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output/temp path refused');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.ino===b.ino&&a.dev===b.dev&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
function fingerprint(fd,length){const b=Buffer.alloc(limits.fingerprint_chunk_bytes),h=createHash('sha256');let at=0;
 while(at<length){const n=fs.readSync(fd,b,0,Math.min(b.length,length-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;}return {sha256:h.digest('hex'),bytes:at,chunk_bytes:b.length};}
function hashFile(p){plain(p,true);const f=fs.openSync(p,fs.constants.O_RDONLY);try{const s=fs.fstatSync(f);assert(s.isFile());
 const r=fingerprint(f,s.size);assert(stable(s,fs.fstatSync(f)));return r;}finally{fs.closeSync(f);}}
plain(path.dirname(out));plain(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([p,h])=>{const full=path.join(root,p);plain(full,true);assert(fs.statSync(full).size<=limits.proof_bytes);
 const b=fs.readFileSync(full);assert(b.length<=limits.proof_bytes);assert.equal(hash(b),h);return JSON.parse(b.toString('utf8'));});
const proof=proofs[0],pe=proof.pe;
assert.equal(proof.status,'exact-actor-export-and-boundary-verified');assert.equal(proof.original,original);assert.equal(proof.expected_sha256,expected);
assert.equal(proof.sha256_before,expected);assert.equal(proof.sha256_after,expected);assert(proof.source_stat_stable&&proof.source_hash_preserved);assert.deepEqual(proof.failures,[]);
for(const s of specs){const rows=proofs[1].filter(i=>i.address===hex(base+BigInt(s.source)));assert.equal(rows.length,1);
 assert.equal(rows[0].bytes,s.raw);assert.equal(rows[0].instruction,s.text);assert.equal(s.source+s.length+Buffer.from(s.raw,'hex').readInt32LE(s.displacement),s.target);}
assert.equal(pe.image_base,hex(base));assert.equal(pe.entry_count,12210);assert.equal(pe.exception_rva,'0x424000');
assert.equal(pe.exception_bytes,146520);assert.equal(pe.exception_file_offset,4317696);assert.equal(pe.exception_bytes,pe.entry_count*12);
const report={schema:'grim-dynasty-fixed-actor-198-array-helpers-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,expected_file_bytes:size,
 source_evidence:sources.map(([path,sha256])=>({path,sha256})),requested_targets:specs,limits,
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,build_or_test_execution:false,
 original_modified:false,full_target_copy_created:false,permission_change:false,tool,expected_tool_sha256:toolHash,
 metadata_bytes:0,code_bytes:0,reads:[],fingerprint_passes:[],subprocesses:0,ranges:[],
 limitations:[
  'Only these three exact .pdata entries within individual and batch limits admit body reads. No containing-body or neighboring-code fallback.',
  'Only 2DDB14 permits a six-byte entry prefix when no exact .pdata entry exists; FF25 RIP-relative JMP is mandatory, and the prefix is not a complete function claim.',
  'A computed indirect-slot RVA is arithmetic evidence only: its contents/import identity are not read or inferred.',
  'At most 32 distinct .pdata entries per target and 1152 newly read metadata bytes total; sorted-table format is assumed and encountered order checked.',
  'No EBF00 destructor, EH/unwind-body, import target, other callee or callback is followed.',
  'Fingerprints use 64KiB buffers separately from semantic byte budgets; dumpbin internal metadata I/O is not instrumented.',
  'Same-handle before/after hash/stat stability is not an atomic snapshot; byte coverage is not proof of complete logical control flow or exception behavior.'
 ]};
let fd,stat,outCreated=false,toolAttempted=false;const failures=[],cache=new Map();
const save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
function read(at,n,kind){assert(Number.isSafeInteger(at)&&at>=0&&Number.isSafeInteger(n)&&n>0&&at+n<=size);
 if(kind==='pdata'){assert.equal(n,12);assert(report.metadata_bytes+n<=limits.metadata_bytes);}
 else{assert.equal(kind,'code');assert(report.code_bytes+n<=limits.code_bytes);}
 const b=Buffer.alloc(n),r={kind,file_offset:at,requested_bytes:n,read_bytes:0};report.reads.push(r);
 while(r.read_bytes<n){const k=fs.readSync(fd,b,r.read_bytes,n-r.read_bytes,at+r.read_bytes);assert(k>0);r.read_bytes+=k;if(kind==='pdata')report.metadata_bytes+=k;else report.code_bytes+=k;}
 if(kind==='pdata')r.raw_hex=b.toString('hex');else r.sha256=hash(b);return b;}
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);return {section:ss[0],offset:ss[0].raw_offset+rva-ss[0].rva};}
try{
 fs.mkdirSync(out);outCreated=true;fs.mkdirSync(temp);plain(out);plain(temp);report.script_sha256=hashFile(script).sha256;
 plain(tool,true);toolAttempted=true;report.tool_before=hashFile(tool);assert.equal(report.tool_before.sha256,toolHash);
 plain(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);assert(stat.isFile()&&stat.size===size);
 const before=fingerprint(fd,size);report.fingerprint_passes.push({label:'before',...before});report.sha256_before=before.sha256;assert.equal(before.sha256,expected);
 const pd=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pd.section.name,'.pdata');assert.equal(pd.offset,pe.exception_file_offset);
 for(const s of specs){const r={label:s.label,requested_rva:hex(s.target),status:'started',body_read:false,entry_prefix_only:false,subprocesses:0,maximum_code_bytes:s.maximum};report.ranges.push(r);const visited=new Set();
  function entry(i){assert(Number.isInteger(i)&&i>=0&&i<pe.entry_count);if(!visited.has(i)){assert(visited.size<limits.entries_per_target);visited.add(i);}if(cache.has(i))return cache.get(i);
   const b=read(pd.offset+i*12,12,'pdata'),e={index:i,begin:b.readUInt32LE(0),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8),raw_hex:b.toString('hex')};
   assert(e.begin<e.end&&e.end<=pe.image_size);for(const [j,o]of cache)assert(j<i?o.begin<=e.begin:o.begin>=e.begin);cache.set(i,e);return e;}
  let lo=0,hi=pe.entry_count,steps=0;while(lo<hi){assert(steps++<limits.search_steps_per_target);const mid=lo+Math.floor((hi-lo)/2),e=entry(mid);if(e.begin<s.target)lo=mid+1;else hi=mid;}
  const e=lo<pe.entry_count?entry(lo):null;r.search_steps=steps;r.visited_indices=[...visited];r.lower_bound_index=lo;r.boundary=e;r.exact_boundary=!!e&&e.begin===s.target;
  if(!r.exact_boundary&&!s.prefix){r.status='no-exact-entry-body-not-read';continue;}
  const length=r.exact_boundary?e.end-e.begin:6;
  if(length>s.maximum||report.code_bytes+length>limits.code_bytes){r.status='code-budget-body-not-read';continue;}
  const mapped=map(s.target,length);assert.equal(mapped.section.name,'.text');assert(mapped.section.characteristics&0x20000000);
  r.code_file_offset=mapped.offset;r.code_bytes=length;const bytes=read(mapped.offset,length,'code');r.code_sha256=hash(bytes);
  if(!r.exact_boundary){r.entry_prefix_only=true;r.entry_prefix_hex=bytes.toString('hex');assert.equal(bytes.subarray(0,2).toString('hex'),'ff25','Only FF25 RIP-relative entry prefix is admitted');
   const iat=s.target+6+bytes.readInt32LE(2);assert(Number.isSafeInteger(iat)&&iat>=0&&iat+8<=pe.image_size);const mappedIat=map(iat,8);
   r.indirect_slot_rva=hex(iat);r.indirect_slot_va=hex(base+BigInt(iat));r.indirect_slot_section=mappedIat.section.name;r.indirect_slot_read=false;r.complete_function_claimed=false;
  }else r.body_read=true;
  save(s.label+'.bin',bytes);const start=base+BigInt(s.target),end=start+BigInt(length);
  const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(start)},${hex(end-1n)}`,original];r.command_arguments=args;r.subprocesses=1;report.subprocesses++;assert(report.subprocesses<=3);const started=Date.now();
  const child=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:limits.tool_timeout_ms,maxBuffer:limits.tool_max_buffer_bytes,
   env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
  r.tool_elapsed_ms=Date.now()-started;r.tool_exit=child.status;r.tool_signal=child.signal;r.tool_error=child.error?String(child.error):null;
  save(s.label+'-dumpbin.txt',child.stdout||'');save(s.label+'-stderr.txt',child.stderr||'');assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
  const rows=[];for(const line of child.stdout.split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
   if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
   const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
  let cursor=start;for(const row of rows){assert(row.instruction&&row.bytes.length>0&&row.bytes.length<=30&&row.bytes.length%2===0);assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=end);}
  assert.equal(cursor,end);assert.equal(rows.map(v=>v.bytes).join(''),bytes.toString('hex'));r.byte_coverage_verified=true;r.instructions=rows.length;
  save(s.label+'-instructions.json',JSON.stringify(rows.map(v=>({...v,address:hex(v.address)})),null,2)+'\n');r.status=r.entry_prefix_only?'ff25-entry-prefix-verified':'exact-body-disassembly-verified';
 }
 report.status=report.ranges.every(r=>r.byte_coverage_verified)?'fixed-actor-198-array-helpers-verified':'fixed-targets-stopped-at-boundary-or-budget';
}catch(error){failures.push(String(error));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){const after=fingerprint(fd,size);report.fingerprint_passes.push({label:'after',...after});report.sha256_after=after.sha256;
  assert.equal(after.sha256,expected);report.source_stat_stable=stable(stat,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){failures.push('Original final verification: '+String(e));}finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(e){failures.push('Original close: '+String(e));}}}
 if(toolAttempted){try{report.tool_after=hashFile(tool);assert.equal(report.tool_after.sha256,toolHash);}catch(e){failures.push('Tool final verification: '+String(e));}}
 report.entries=[...cache.values()];report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;report.tool_hash_preserved=report.tool_before?.sha256===toolHash&&report.tool_after?.sha256===toolHash;
 if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();if(outCreated){plain(out);save('inspection.json',JSON.stringify(report,null,2)+'\n');}
}
console.log(JSON.stringify({status:report.status,ranges:report.ranges.map(r=>({label:r.label,status:r.status,code_bytes:r.code_bytes,instructions:r.instructions,indirect_slot_rva:r.indirect_slot_rva})),
 metadata_bytes:report.metadata_bytes,code_bytes:report.code_bytes,original_unchanged:report.source_hash_preserved,tool_unchanged:report.tool_hash_preserved,subprocesses:report.subprocesses,failures}));
if(failures.length)process.exitCode=1;
