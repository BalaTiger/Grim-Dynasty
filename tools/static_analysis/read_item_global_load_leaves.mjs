// Prepared only. Three bounded symbol-entry windows in cached .pdata gaps.
// At most one conditional exact139B body; no new metadata or automatic traversal.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_global_load_leaves.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632,pin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-global-load-leaves'),temp=path.join(root,'.tooling/offline-item-global-load-leaves-temp'),base=0x180000000n;
const source=['docs/object-lifecycle/offline-item-global-load-getters/inspection.json','4be3c63f988ab540100dbb80129506bc13fa254f5bfa868fd358b45090900b78'];
const limits={entry_code_bytes:80,conditional_code_bytes:139,code_bytes:219,metadata_bytes:0,semantic_reads:4,dumpbin_calls:4,
 tiny_pe_bytes_each:8192,dumpbin_buffer_bytes:65536,dumpbin_timeout_ms:10000,hash_buffer_bytes:65536,proof_bytes:524288};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());
 assert(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const f=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(f,fs.fstatSync(f).size);}finally{fs.closeSync(f);}}
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const proofPath=path.join(root,source[0]);normal(proofPath,true);assert(fs.statSync(proofPath).size<=limits.proof_bytes);
const rawProof=fs.readFileSync(proofPath);assert.equal(hash(rawProof),source[1]);const prior=JSON.parse(rawProof),pe=prior.pe;
assert.equal(prior.status,'fixed-global-load-getters-with-frontiers');assert.equal(prior.original,original);assert.equal(prior.expected_sha256,pin);assert.equal(prior.expected_file_bytes,size);
assert.equal(prior.sha256_before,pin);assert.equal(prior.sha256_after,pin);assert(prior.source_closed&&prior.source_stat_stable&&prior.source_hash_preserved);assert.equal(prior.failures.length,0);
assert.equal(pe.image_base,hex(base));assert.equal(pe.image_size,4521984);assert.equal(pe.exception_file_offset,4317696);assert.equal(pe.entry_count,12210);
function old(label,target){const a=prior.targets.filter(t=>t.label===label);assert.equal(a.length,1);const v=a[0];assert.equal(v.target,target);assert.equal(v.status,'no-exact-begin-gap');
 for(const e of [v.previous,v.next]){const b=Buffer.from(e.raw_hex,'hex');assert.equal(b.length,12);assert.equal(b.readUInt32LE(),e.begin);assert.equal(b.readUInt32LE(4),e.end);assert.equal(b.readUInt32LE(8),e.unwind);
  assert.equal(e.file_offset,pe.exception_file_offset+e.index*12);}
 assert(v.previous.end<=target&&target<v.next.begin);return v;}
const binary=old('binary-float',0x13fd50),get=old('get-load-table',0x17ee80),load=old('load-table-file',0x17ee90);
assert.equal(binary.cached_slot.slot_rva,'0x342028');assert.equal(binary.cached_slot.raw_hex,'50fd138001000000');
assert.equal(get.export_binding.hint,3007);assert.equal(get.export_binding.target_rva,'0x17ee80');
assert.equal(load.export_binding.hint,4239);assert.equal(load.export_binding.target_rva,'0x17ee90');
assert.equal(binary.next.begin,0x13fd70);assert.equal(binary.next.end,0x13fdfb);assert.equal(binary.next.raw_hex,'70fd1300fbfd1300243f3800');
assert.equal(get.next.begin,0x17eeb0);assert.equal(load.next.begin,0x17eeb0);
const targets=[{label:'binary-float',start:0x13fd50,end:0x13fd70,bytes:32,cached_binding:binary},
 {label:'get-load-table',start:0x17ee80,end:0x17ee90,bytes:16,cached_binding:get},
 {label:'load-table-file',start:0x17ee90,end:0x17eeb0,bytes:32,cached_binding:load}];
for(const t of targets){assert.equal(t.end-t.start,t.bytes);assert(t.end<=t.cached_binding.next.begin);t.status='pending';t.boundary_assessment='bounded-symbol-entry-window-not-pdata-function-body';}
assert.equal(targets[1].end,load.target,'GetLoadTable window must stop at the next independently bound export');
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const a=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(a.length,1);
 const offset=a[0].raw_offset+rva-a[0].rva;assert(offset>=0&&offset<=size-n);return {section:a[0],offset};}
const r={schema:'grim-dynasty-item-global-load-leaves-v1',status:'started',started_utc:new Date().toISOString(),source,original,expected_sha256:pin,expected_file_bytes:size,
 pe,limits,targets,metadata_bytes:0,code_bytes:0,entry_code_bytes:0,conditional_code_bytes:0,semantic_reads:0,dumpbin_calls:0,reads:[],source_closed:false,failures:[],
 original_open_mode:'O_RDONLY',original_loaded_or_executed:false,original_path_given_to_dumpbin:false,new_header_bytes:0,unwind_body_bytes:0,data_bytes:0,
 automatic_callee_traversal:false,conditional_body:{start:'0x13fd70',end_exclusive:'0x13fdfb',bytes:139,cached_pdata:binary.next,status:'not-selected'},
 limitations:['The three windows begin at independently bound entries and stop at a cached next entry/export. They are not claimed to be whole functions or .pdata bodies.',
  'All window bytes are retained and decoded, but only the linear prefix through the first branch/return/trap is labelled as an entry prefix. Remaining bytes are unclassified, not silently assigned to the function.',
  'Only a raw E8/E9 rel32 or EB rel8 call/jump to13FD70 within the verified binary-float entry prefix can authorize the one139B exact body.',
  'A later padding/unclassified branch, conditional branch or indirect branch never selects the extra body. No other target is read.',
  'Byte/address/decode failures retain a frontier; no wider read, alternate entry, extra metadata or retry is performed.',
  'No algorithm, runtime reachability, exception cleanup, loaded vtable or whole-function compatibility follows merely from syntactic decoding.',
  'Same-handle64KiB before/after hashes are identity checks, not an atomic snapshot.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function inspect(t,conditional=false){const n=t.end-t.start,m=map(t.start,n);assert(m.section.characteristics&0x20000000);
 assert(r.semantic_reads<limits.semantic_reads);assert(r.code_bytes+n<=limits.code_bytes);
 if(conditional){assert.equal(t.start,0x13fd70);assert.equal(t.end,0x13fdfb);assert.equal(n,139);assert.equal(r.conditional_code_bytes,0);
  assert(r.conditional_body.validated_trigger);r.conditional_code_bytes=n;}
 else{assert(targets.includes(t));assert.equal(n,t.bytes);assert(r.entry_code_bytes+n<=80);r.entry_code_bytes+=n;}
 r.semantic_reads++;r.code_bytes+=n;const code=Buffer.alloc(n),got=fs.readSync(fd,code,0,n,m.offset);
 r.reads.push({kind:conditional?'conditional-exact-body':'fixed-entry-window',rva:hex(t.start),file_offset:m.offset,requested_bytes:n,read_bytes:got});assert.equal(got,n);
 const stem=t.label+'-'+t.start.toString(16);save(stem+(conditional?'-exact.bin':'-entry.bin'),code);t.sha256=hash(code);
 const page=t.start&~0xfff,delta=t.start-page,virtualBytes=delta+n,rawBytes=Math.ceil(virtualBytes/512)*512;
 const b=Buffer.alloc(512+rawBytes);assert(b.length<=limits.tiny_pe_bytes_each);const coff=0x84,opt=0x98,sec=0x188;
 b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawBytes,opt+4);b.writeUInt32LE(page,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);
 b.writeUInt32LE(Math.ceil((page+virtualBytes)/4096)*4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);b.write('.text',sec,'ascii');b.writeUInt32LE(virtualBytes,sec+8);b.writeUInt32LE(page,sec+12);b.writeUInt32LE(rawBytes,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);
 const tiny=path.join(temp,stem+'.inspect.dll');normal(temp);fs.writeFileSync(tiny,b,{flag:'wx'});t.synthetic={path:tiny,bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(t.start))+','+hex(base+BigInt(t.end-1)),tiny];
 assert(r.dumpbin_calls<limits.dumpbin_calls);r.dumpbin_calls++;t.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:limits.dumpbin_timeout_ms,maxBuffer:limits.dumpbin_buffer_bytes,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save(stem+'-dumpbin.txt',child.stdout||'');save(stem+'-stderr.txt',child.stderr||'');t.tool_exit=child.status;
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),t.synthetic.sha256);
 const rows=[],issues=[];function issue(kind,where,detail){if(issues.length<32)issues.push({kind,where,detail});}
 for(const line of(child.stdout||'').split(/\r?\n/)){const v=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
  if(v){rows.push({address:hex(BigInt('0x'+v[1])),bytes:v[2].replace(/\s/g,'').toLowerCase(),instruction:v[3].trim()});continue;}
  const more=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(more){if(rows.length)rows[rows.length-1].bytes+=more[1].replace(/\s/g,'').toLowerCase();else issue('orphan-bytes',null,line.trim());}}
 save(stem+'-instructions.json',JSON.stringify(rows,null,2)+'\n');let cursor=base+BigInt(t.start);
 for(const v of rows){const at=BigInt(v.address),width=v.bytes.length/2;if(at!==cursor)issue('noncontiguous-address',v.address,hex(cursor));
  if(!Number.isInteger(width)||width<1||width>15)issue('invalid-width',v.address,String(width));
  if(!v.instruction||/[?]|\(bad\)|invalid/i.test(v.instruction)||/^(?:db|dw|dd|dq)\b/i.test(v.instruction))issue('invalid-or-data',v.address,v.instruction);
  cursor=at+BigInt(width);}
 if(cursor!==base+BigInt(t.end))issue('end-boundary-mismatch',hex(cursor),hex(base+BigInt(t.end)));
 if(rows.map(v=>v.bytes).join('')!==code.toString('hex'))issue('byte-concatenation-mismatch',null,'Decoded bytes differ');
 t.instructions=rows.length;t.decode_issues=issues;t.byte_coverage_verified=issues.length===0;t.raw_calls_or_branches=rows.filter(v=>/^(?:call|j[a-z]+|loop[a-z]*)\b/i.test(v.instruction));
 t.status=issues.length?'fixed-window-decode-frontier':conditional?'exact-conditional-body-decoded-review-required':'bounded-entry-window-decoded-manual-review-required';
 if(!conditional&&t.byte_coverage_verified){t.entry_prefix=[];t.prefix_stop='window-exhausted-no-function-boundary';let after=base+BigInt(t.start);
  for(const v of rows){t.entry_prefix.push(v);after=BigInt(v.address)+BigInt(v.bytes.length/2);
   if(/^(?:ret|j[a-z]+|loop[a-z]*|int3|int|ud2|hlt)\b/i.test(v.instruction)){t.prefix_stop='first-transfer-or-trap';break;}}
  t.entry_prefix_bytes=Number(after-(base+BigInt(t.start)));t.unclassified_remaining_bytes=t.bytes-t.entry_prefix_bytes;
 }
 return rows;
}
function conditionalTrigger(t){if(!t.byte_coverage_verified||!t.entry_prefix)return null;
 for(const row of t.entry_prefix){const b=Buffer.from(row.bytes,'hex');let target;
  if(b.length===5&&(b[0]===0xe8||b[0]===0xe9)&&/^(?:call|jmp)\b/i.test(row.instruction))target=BigInt(row.address)+5n+BigInt(b.readInt32LE(1));
  else if(b.length===2&&b[0]===0xeb&&/^jmp\b/i.test(row.instruction))target=BigInt(row.address)+2n+BigInt(b.readInt8(1));
  else continue;
  if(target===base+0x13fd70n)return {instruction:row,decoded_from_raw_relative_bytes:true,target_va:hex(target),within_verified_entry_prefix:true};
 }return null;}
try{
 fs.mkdirSync(out);created=true;normal(out);fs.mkdirSync(temp);normal(temp);r.script_sha256=fingerprint(self);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);
 r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 for(const t of targets)inspect(t);
 const trigger=conditionalTrigger(targets[0]);
 if(trigger){r.conditional_body.validated_trigger=trigger;const t={label:'binary-float-target',start:0x13fd70,end:0x13fdfb,bytes:139,boundary_assessment:'cached-exact-pdata-body',cached_pdata:binary.next};
  inspect(t,true);r.conditional_body.result=t;r.conditional_body.status=t.status;}
 else r.conditional_body.status='not-selected-no-verified-direct-prefix-reference';
 r.status=targets.every(t=>t.byte_coverage_verified)&&(!r.conditional_body.result||r.conditional_body.result.byte_coverage_verified)?'fixed-global-load-leaves-decoded-review-required':'fixed-global-load-leaves-with-decode-frontiers';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);
  r.source_stat_stable=s.isFile()&&s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}}
  catch(e){r.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);r.source_closed=true;}catch(e){r.failures.push('Close: '+e);}}}
 if(r.tool_before!==undefined){try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}}
 r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,targets:targets.map(t=>({label:t.label,status:t.status,entry_prefix:t.entry_prefix,prefix_stop:t.prefix_stop,unclassified_remaining_bytes:t.unclassified_remaining_bytes})),
 conditional_body:r.conditional_body,code_bytes:r.code_bytes,metadata_bytes:r.metadata_bytes,dumpbin_calls:r.dumpbin_calls,source_closed:r.source_closed,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));
if(r.failures.length)process.exitCode=1;
