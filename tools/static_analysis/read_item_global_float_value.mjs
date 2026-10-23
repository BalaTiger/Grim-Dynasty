// Prepared only: one exact numeric target140420 from cached raw CALL140349.
// Exact body <=1024B, or a .pdata-gap entry window <=256B ending before next begin.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_global_float_value.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632,pin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-global-float-value'),temp=path.join(root,'.tooling/offline-item-global-float-value-temp'),base=0x180000000n,target=0x140420;
const sources=[
 ['docs/object-lifecycle/offline-item-global-load-direct/inspection.json','dd24c4565a04de81051c8866e7d927cff0e807bd5ec48d88a899c1febff65b1c'],
 ['docs/object-lifecycle/offline-item-global-load-direct/binary-indexed-float-1402e0-exact.bin','0d10653e6dd2edbaf4748e98f038d110e15bd6ff182b9ce33e71bcf0382985fb']
];
const limits={search_steps:32,new_pdata_entries:34,metadata_bytes:408,exact_code_bytes:1024,gap_code_bytes:256,semantic_reads:35,dumpbin_calls:1,
 tiny_pe_bytes:8192,dumpbin_buffer_bytes:131072,dumpbin_timeout_ms:10000,hash_buffer_bytes:65536,proof_bytes_each:524288};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());
 assert(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([rel,sha])=>{const f=path.join(root,rel);normal(f,true);assert(fs.statSync(f).size<=limits.proof_bytes_each);const b=fs.readFileSync(f);assert.equal(hash(b),sha);return b;});
const prior=JSON.parse(proofs[0]),callerBody=proofs[1],pe=prior.pe;
assert.equal(prior.status,'two-fixed-global-load-direct-bodies-decoded-review-required');assert.equal(prior.original,original);assert.equal(prior.expected_sha256,pin);assert.equal(prior.expected_file_bytes,size);
assert.equal(prior.sha256_before,pin);assert.equal(prior.sha256_after,pin);assert(prior.source_hash_preserved&&prior.source_stat_stable&&prior.source_closed);assert.equal(prior.failures.length,0);
const callers=prior.targets.filter(v=>v.label==='binary-indexed-float');assert.equal(callers.length,1);const caller=callers[0];
assert.equal(caller.target,0x1402e0);assert.equal(caller.status,'exact-body-decoded-review-required');assert(caller.byte_coverage_verified);
assert.equal(caller.code_sha256,sources[1][1]);assert.equal(callerBody.length,152);assert.equal(caller.boundary.end,0x140378);
const calls=caller.calls.filter(v=>v.address==='0x180140349');assert.equal(calls.length,1);assert.equal(calls[0].bytes,'e8d2000000');
const callRaw=callerBody.subarray(0x140349-0x1402e0,0x14034e-0x1402e0);assert.equal(callRaw.toString('hex'),calls[0].bytes);assert.equal(callRaw[0],0xe8);assert.equal(0x14034e+callRaw.readInt32LE(1),target);
assert.equal(pe.image_base,hex(base));assert.equal(pe.image_size,4521984);assert.equal(pe.exception_rva,'0x424000');assert.equal(pe.exception_file_offset,4317696);assert.equal(pe.entry_count,12210);assert.equal(pe.exception_bytes,12210*12);
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);const a=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(a.length,1);
 const offset=a[0].raw_offset+rva-a[0].rva;assert(offset>=0&&offset<=size-n);return {section:a[0],offset};}
const pm=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pm.section.name,'.pdata');assert.equal(pm.offset,pe.exception_file_offset);
function decode(index,b){assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);assert.equal(b.length,12);const begin=b.readUInt32LE(),end=b.readUInt32LE(4),unwind=b.readUInt32LE(8);
 assert(begin>0&&begin<end&&end<=pe.image_size&&unwind>0&&unwind<pe.image_size);assert(map(begin,end-begin).section.characteristics&0x20000000);
 return {index,begin,end,unwind,raw_hex:b.toString('hex'),file_offset:pe.exception_file_offset+index*12};}
const cache=new Map();for(const t of prior.targets)for(const v of t.encountered_pdata){const e=decode(v.index,Buffer.from(v.raw_hex,'hex'));
 assert.equal(v.begin,e.begin);assert.equal(v.end,e.end);assert.equal(v.unwind,e.unwind);assert.equal(v.file_offset,e.file_offset);
 if(cache.has(e.index))assert.equal(cache.get(e.index).raw_hex,e.raw_hex);else cache.set(e.index,e);}
const ordered=[...cache.values()].sort((a,b)=>a.index-b.index);for(let i=1;i<ordered.length;i++)assert(ordered[i-1].begin<ordered[i].begin);
const r={schema:'grim-dynasty-item-global-float-value-v1',status:'started',started_utc:new Date().toISOString(),original,expected_sha256:pin,expected_file_bytes:size,
 sources,cached_caller:calls[0],cached_call_verified_in_exact_body:true,target_rva:hex(target),pe,limits,cached_pdata_entries:cache.size,
 metadata_bytes:0,code_bytes:0,new_pdata_entries:0,search_steps:0,semantic_reads:0,dumpbin_calls:0,reads:[],encountered_pdata:[],source_closed:false,failures:[],
 original_open_mode:'O_RDONLY',original_loaded_or_executed:false,original_path_given_to_dumpbin:false,whole_tables_scanned:false,new_header_bytes:0,unwind_body_bytes:0,data_bytes:0,recursive_callees:0,
 selected_code_range:null,limitations:['Only target140420 is selected from the independently pinned raw CALL140349. No callee, Base parser, Singleton, fallback constructor or adjacent body is followed.',
  'Exact .pdata body is read only when <=1024B. An exact larger body is not replaced by a window.',
  'Only a confirmed gap permits the candidate window min(256,next.begin-target). A containing entry or missing next entry selects no code.',
  'A gap window is not a complete function boundary or proof of a leaf without unwind. All decoded rows are preserved; the first branch is never treated as the function end.',
  'Out-of-window branches and indirect transfers remain unresolved. Raw relative targets are reported for manual CFG review without extending the read.',
  'Complete syntactic decoding does not prove reachability, semantic types, exceptional behavior or loaded compatibility.',
  'Same-handle64KiB before/after hashes check identity, not an atomic snapshot.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,kind){assert(r.semantic_reads<limits.semantic_reads);const m=map(rva,n);
 if(kind==='pdata'){assert.equal(n,12);assert(!(m.section.characteristics&0x20000000));assert(r.new_pdata_entries<34);assert(r.metadata_bytes+n<=408);r.new_pdata_entries++;r.metadata_bytes+=n;}
 else{assert.equal(kind,'selected-code');assert.equal(rva,target);assert.equal(r.code_bytes,0);assert(r.selected_code_range);assert.equal(n,r.selected_code_range.bytes);assert(n<=1024);assert(m.section.characteristics&0x20000000);r.code_bytes=n;}
 r.semantic_reads++;const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,m.offset);r.reads.push({kind,rva:hex(rva),file_offset:m.offset,requested_bytes:n,read_bytes:got,raw_hex:kind==='pdata'?b.subarray(0,got).toString('hex'):undefined});assert.equal(got,n);return b;}
const seen=new Map();function entry(index){if(seen.has(index))return seen.get(index);let e;if(cache.has(index))e={...cache.get(index),reused:true};
 else{assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);e={...decode(index,read(Number(pe.exception_rva)+index*12,12,'pdata')),reused:false};}
 for(const [i,v] of seen)assert(i<index?v.begin<e.begin:v.begin>e.begin);seen.set(index,e);r.encountered_pdata.push(e);return e;}
function inspectSelected(){const n=r.selected_code_range.bytes,end=target+n,code=read(target,n,'selected-code');
 save(r.boundary_assessment==='exact-pdata-body'?'140420-exact.bin':'140420-entry-window.bin',code);r.code_sha256=hash(code);
 const page=target&~0xfff,delta=target-page,virtualBytes=delta+n,rawBytes=Math.ceil(virtualBytes/512)*512,b=Buffer.alloc(512+rawBytes);assert(b.length<=8192);const coff=0x84,opt=0x98,sec=0x188;
 b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawBytes,opt+4);b.writeUInt32LE(page,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(Math.ceil((page+virtualBytes)/4096)*4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
 b.write('.text',sec,'ascii');b.writeUInt32LE(virtualBytes,sec+8);b.writeUInt32LE(page,sec+12);b.writeUInt32LE(rawBytes,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);
 const tiny=path.join(temp,'140420-selected.inspect.dll');normal(temp);fs.writeFileSync(tiny,b,{flag:'wx'});r.synthetic={path:tiny,bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(target))+','+hex(base+BigInt(end-1)),tiny];assert.equal(r.dumpbin_calls,0);r.dumpbin_calls=1;r.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:limits.dumpbin_timeout_ms,maxBuffer:limits.dumpbin_buffer_bytes,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save('140420-dumpbin.txt',child.stdout||'');save('140420-stderr.txt',child.stderr||'');r.tool_exit=child.status;assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),r.synthetic.sha256);
 const rows=[],issues=[];function issue(kind,at,detail){if(issues.length<32)issues.push({kind,at,detail});}
 for(const line of(child.stdout||'').split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);if(m){rows.push({address:hex(BigInt('0x'+m[1])),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
  const more=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(more){if(rows.length)rows[rows.length-1].bytes+=more[1].replace(/\s/g,'').toLowerCase();else issue('orphan-bytes',null,line.trim());}}
 save('140420-instructions.json',JSON.stringify(rows,null,2)+'\n');let cursor=base+BigInt(target);
 for(const v of rows){const at=BigInt(v.address),width=v.bytes.length/2;if(at!==cursor)issue('noncontiguous-address',v.address,hex(cursor));if(!Number.isInteger(width)||width<1||width>15)issue('invalid-width',v.address,String(width));
  if(!v.instruction||/[?]|\(bad\)|invalid/i.test(v.instruction)||/^(?:db|dw|dd|dq)\b/i.test(v.instruction))issue('invalid-or-data',v.address,v.instruction);cursor=at+BigInt(width);}
 if(cursor!==base+BigInt(end))issue('end-boundary-mismatch',hex(cursor),hex(base+BigInt(end)));if(rows.map(v=>v.bytes).join('')!==code.toString('hex'))issue('byte-concatenation-mismatch',null,'Exact selected bytes differ');
 r.instructions=rows.length;r.decode_issues=issues;r.byte_coverage_verified=issues.length===0;
 r.calls=rows.filter(v=>/^call\b/i.test(v.instruction));r.branches=rows.filter(v=>/^(?:j[a-z]+|loop[a-z]*|xbegin)\b/i.test(v.instruction));r.returns=rows.filter(v=>/^ret\b/i.test(v.instruction));r.relative_targets=[];
 if(r.byte_coverage_verified)for(const v of [...r.calls,...r.branches]){const b=Buffer.from(v.bytes,'hex');let displacement;
  if(b.length===5&&(b[0]===0xe8||b[0]===0xe9))displacement=b.readInt32LE(1);
  else if(b.length===2&&(b[0]===0xeb||(b[0]>=0x70&&b[0]<=0x7f)||(b[0]>=0xe0&&b[0]<=0xe3)))displacement=b.readInt8(1);
  else if(b.length===6&&b[0]===0x0f&&b[1]>=0x80&&b[1]<=0x8f)displacement=b.readInt32LE(2);else continue;
  const at=BigInt(v.address)+BigInt(b.length)+BigInt(displacement);r.relative_targets.push({instruction:v,target_va:hex(at),inside_selected_range:at>=base+BigInt(target)&&at<base+BigInt(end),followed:false});}
 r.status=issues.length?'fixed-float-value-decode-frontier':r.boundary_assessment==='exact-pdata-body'?'exact-float-value-body-decoded-review-required':'bounded-float-value-entry-window-decoded-manual-cfg-required';
}
try{
 fs.mkdirSync(out);created=true;normal(out);fs.mkdirSync(temp);normal(temp);r.script_sha256=fingerprint(self);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 let lo=0,hi=pe.entry_count;while(lo<hi){assert(r.search_steps<32);r.search_steps++;const mid=lo+Math.floor((hi-lo)/2);if(entry(mid).begin<target)lo=mid+1;else hi=mid;}
 r.lower_bound_index=lo;const next=lo<pe.entry_count?entry(lo):null;
 if(next&&next.begin===target){const bytes=next.end-next.begin;r.exact_boundary={...next,begin_rva:hex(next.begin),end_rva_exclusive:hex(next.end),bytes};
  if(bytes<=1024){r.boundary_assessment='exact-pdata-body';r.selected_code_range={rva:hex(target),end_exclusive:hex(next.end),bytes};}else{r.boundary_assessment='exact-body-over-budget';r.status='exact-float-value-body-exceeds1024B';}}
 else{const previous=lo>0?entry(lo-1):null;r.previous=previous;r.next=next;
  if(previous&&target<previous.end){r.boundary_assessment='no-exact-begin-containing-entry';r.status='containing-entry-no-code-selected';}
  else if(!next){r.boundary_assessment='no-next-entry-gap-unbounded';r.status='missing-next-boundary-no-code-selected';}
  else{assert(next.begin>target);const bytes=Math.min(256,next.begin-target);assert(bytes>0);r.boundary_assessment='gap-entry-window-not-function-boundary';
   r.selected_code_range={rva:hex(target),end_exclusive:hex(target+bytes),bytes};}}
 if(r.selected_code_range)inspectSelected();
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);r.source_stat_stable=s.isFile()&&s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}}
  catch(e){r.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);r.source_closed=true;}catch(e){r.failures.push('Close: '+e);}}}
 if(r.tool_before!==undefined){try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}}
 r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,boundary_assessment:r.boundary_assessment,selected_code_range:r.selected_code_range,metadata_bytes:r.metadata_bytes,code_bytes:r.code_bytes,
 instructions:r.instructions,byte_coverage_verified:r.byte_coverage_verified,source_closed:r.source_closed,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
