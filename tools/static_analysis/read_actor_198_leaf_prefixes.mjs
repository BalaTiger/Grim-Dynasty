// Prepared only: a fixed bounded interval and a one-byte RET-entry check.
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
const out=path.join(root,'docs/object-lifecycle/offline-actor-198-leaf-prefixes'),temp=path.join(root,'.tooling/offline-actor-198-leaf-prefixes-temp');
const sources=[
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-actor-198-constructor/0ebde0-instructions.json','9083e6b9037ded4e5ee7224b5544ac2b9abae3b6a1ecca612e9d7663b8f484ce'],
 ['docs/object-lifecycle/offline-actor-198-array-helpers/inspection.json','7c2d9759d6c00b9a95bd153dfed62ea871f480a1ee8213c3aa48d63c460701e5'],
 ['docs/object-lifecycle/offline-actor-198-array-helpers/2dcc3c-instructions.json','a8c49acbfc80c793124df217df0aace5fb16786e4aa13ba660739d5751d794c3']
];
const ranges=[{label:'0ebeb0',begin:0xebeb0,end:0xebf00,kind:'bounded-interval'},
 {label:'2dd4c0',begin:0x2dd4c0,end:0x2dd4c1,kind:'one-byte-entry-check'}];
const base=0x180000000n,hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const limits={code_bytes:81,metadata_bytes:0,proof_bytes:131072,fingerprint_chunk_bytes:65536,tool_timeout_ms:10000,tool_max_buffer_bytes:196608};
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
const proof=proofs[0],pe=proof.pe,previous=proofs[2];
assert.equal(proof.status,'exact-actor-export-and-boundary-verified');assert.equal(proof.original,original);assert.equal(proof.expected_sha256,expected);
assert.equal(proof.sha256_before,expected);assert.equal(proof.sha256_after,expected);assert(proof.source_stat_stable&&proof.source_hash_preserved);assert.deepEqual(proof.failures,[]);
assert.equal(pe.image_base,hex(base));assert.equal(previous.original,original);assert.equal(previous.expected_sha256,expected);
assert.equal(previous.sha256_before,expected);assert.equal(previous.sha256_after,expected);assert(previous.source_hash_preserved&&previous.source_stat_stable);assert.deepEqual(previous.failures,[]);
const prior=previous.ranges.filter(r=>r.label==='0ebeb0');assert.equal(prior.length,1);assert.equal(prior[0].requested_rva,'0xebeb0');
assert.equal(prior[0].status,'no-exact-entry-body-not-read');assert.equal(prior[0].exact_boundary,false);assert.equal(prior[0].body_read,false);
assert.equal(prior[0].boundary.begin,0xebf00);assert.equal(prior[0].boundary.index,4026);assert.equal(prior[0].boundary.raw_hex,'00bf0e00acbf0e0080203600');
const references=[
 {proof:1,source:0xebe73,raw:'4c8d0d36000000',text:'lea         r9,[00000001800EBEB0h]',offset:3,length:7,target:0xebeb0},
 {proof:1,source:0xebe67,raw:'488d0592000000',text:'lea         rax,[00000001800EBF00h]',offset:3,length:7,target:0xebf00},
 {proof:3,source:0x2dcc75,raw:'e846080000',text:'call        00000001802DD4C0',offset:1,length:5,target:0x2dd4c0}
];
for(const ref of references){const rows=proofs[ref.proof].filter(i=>i.address===hex(base+BigInt(ref.source)));assert.equal(rows.length,1);
 assert.equal(rows[0].bytes,ref.raw);assert.equal(rows[0].instruction,ref.text);assert.equal(ref.source+ref.length+Buffer.from(ref.raw,'hex').readInt32LE(ref.offset),ref.target);}
const report={schema:'grim-dynasty-fixed-actor-198-leaf-prefixes-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,expected_file_bytes:size,
 source_evidence:sources.map(([path,sha256])=>({path,sha256})),source_references:references,prior_boundary:prior[0],requested_ranges:ranges,limits,
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,build_or_test_execution:false,original_modified:false,full_target_copy_created:false,
 permission_change:false,tool,expected_tool_sha256:toolHash,metadata_bytes:0,code_bytes:0,reads:[],fingerprint_passes:[],subprocesses:0,ranges:[],
 limitations:[
  'EBEB0..EBF00 is an 80-byte bounded interval, not an independently established complete function. EBF00 itself is excluded.',
  '2DD4C0 is read for one byte only. Only C3 admits disassembly and a RET-entry observation; any other byte stops without expansion.',
  'RET/INT3/NOP observations are recorded from decoding; padding and full function extent are not presumed.',
  'No new .pdata, EH/unwind-body, import slot, destructor body, callee or other target is read.',
  'Fingerprints use 64KiB buffers separately from semantic byte budgets; dumpbin internal metadata I/O is not instrumented.',
  'Same-handle before/after hash/stat stability is not an atomic snapshot; byte coverage is not proof of complete logical control flow or exception behavior.'
 ]};
let fd,stat,outCreated=false,toolAttempted=false;const failures=[];
const save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);return {section:ss[0],offset:ss[0].raw_offset+rva-ss[0].rva};}
try{
 fs.mkdirSync(out);outCreated=true;fs.mkdirSync(temp);plain(out);plain(temp);report.script_sha256=hashFile(script).sha256;
 plain(tool,true);toolAttempted=true;report.tool_before=hashFile(tool);assert.equal(report.tool_before.sha256,toolHash);
 plain(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);assert(stat.isFile()&&stat.size===size);
 const before=fingerprint(fd,size);report.fingerprint_passes.push({label:'before',...before});report.sha256_before=before.sha256;assert.equal(before.sha256,expected);
 for(const spec of ranges){const n=spec.end-spec.begin,mapped=map(spec.begin,n);assert.equal(mapped.section.name,'.text');assert(mapped.section.characteristics&0x20000000);
  assert(Number.isSafeInteger(mapped.offset)&&mapped.offset>=0&&mapped.offset+n<=size&&report.code_bytes+n<=limits.code_bytes);
  const r={...spec,status:'started',code_file_offset:mapped.offset,requested_bytes:n,read_bytes:0,complete_function_claimed:false,subprocesses:0};report.ranges.push(r);report.reads.push(r);
  const bytes=Buffer.alloc(n);while(r.read_bytes<n){const k=fs.readSync(fd,bytes,r.read_bytes,n-r.read_bytes,mapped.offset+r.read_bytes);assert(k>0);r.read_bytes+=k;report.code_bytes+=k;}
  r.code_sha256=hash(bytes);save(spec.label+'.bin',bytes);
  if(spec.kind==='one-byte-entry-check'){r.entry_byte_hex=bytes.toString('hex');if(bytes[0]!==0xc3){r.status='non-ret-byte-stopped-without-expansion';continue;}r.ret_entry_observed=true;}
  const start=base+BigInt(spec.begin),end=base+BigInt(spec.end),args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(start)},${hex(end-1n)}`,original];
  r.command_arguments=args;r.subprocesses=1;report.subprocesses++;assert(report.subprocesses<=2);const started=Date.now();
  const child=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:limits.tool_timeout_ms,maxBuffer:limits.tool_max_buffer_bytes,
   env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
  r.tool_elapsed_ms=Date.now()-started;r.tool_exit=child.status;r.tool_signal=child.signal;r.tool_error=child.error?String(child.error):null;
  save(spec.label+'-dumpbin.txt',child.stdout||'');save(spec.label+'-stderr.txt',child.stderr||'');assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
  const rows=[];for(const line of child.stdout.split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
   if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
   const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
  let cursor=start;for(const row of rows){assert(row.instruction&&row.bytes.length>0&&row.bytes.length<=30&&row.bytes.length%2===0);assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=end);}
  assert.equal(cursor,end);assert.equal(rows.map(v=>v.bytes).join(''),bytes.toString('hex'));r.byte_coverage_verified=true;r.instructions=rows.length;
  r.decoded_ret_addresses=rows.filter(v=>/^ret(?:\s|$)/i.test(v.instruction)).map(v=>hex(v.address));
  r.decoded_int3_addresses=rows.filter(v=>/^int\s+3$|^int3$/i.test(v.instruction)).map(v=>hex(v.address));
  r.decoded_nop_addresses=rows.filter(v=>/^nop(?:\s|$)/i.test(v.instruction)).map(v=>hex(v.address));
  save(spec.label+'-instructions.json',JSON.stringify(rows.map(v=>({...v,address:hex(v.address)})),null,2)+'\n');r.status=spec.kind==='one-byte-entry-check'?'ret-entry-verified':'bounded-interval-disassembly-verified';
 }
 report.status=report.ranges.every(r=>r.byte_coverage_verified)?'fixed-leaf-prefixes-verified':'fixed-prefix-stopped-without-expansion';
}catch(error){failures.push(String(error));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){const after=fingerprint(fd,size);report.fingerprint_passes.push({label:'after',...after});report.sha256_after=after.sha256;
  assert.equal(after.sha256,expected);report.source_stat_stable=stable(stat,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){failures.push('Original final verification: '+String(e));}finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(e){failures.push('Original close: '+String(e));}}}
 if(toolAttempted){try{report.tool_after=hashFile(tool);assert.equal(report.tool_after.sha256,toolHash);}catch(e){failures.push('Tool final verification: '+String(e));}}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;report.tool_hash_preserved=report.tool_before?.sha256===toolHash&&report.tool_after?.sha256===toolHash;
 if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();if(outCreated){plain(out);save('inspection.json',JSON.stringify(report,null,2)+'\n');}
}
console.log(JSON.stringify({status:report.status,ranges:report.ranges.map(r=>({label:r.label,status:r.status,read_bytes:r.read_bytes,instructions:r.instructions})),metadata_bytes:report.metadata_bytes,
 code_bytes:report.code_bytes,original_unchanged:report.source_hash_preserved,tool_unchanged:report.tool_hash_preserved,subprocesses:report.subprocesses,failures}));
if(failures.length)process.exitCode=1;
