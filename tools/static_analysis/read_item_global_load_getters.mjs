// Prepared only: four fixed Engine targets; no original execution or callee recursion.
// Two exact export hints, exact .pdata begins, <=2048B per body and <=8192B total.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_global_load_getters.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632,pin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-global-load-getters'),temp=path.join(root,'.tooling/offline-item-global-load-getters-temp'),base=0x180000000n;
const sources=[
 ['docs/object-lifecycle/offline-item-global-load-inputs/inspection.json','75046b8f1e04fff6d60c07a175ffdfa20cbb24218aa8ff424d39894190b9f1d0'],
 ['docs/object-lifecycle/offline-item-name-import/inspection.json','c3ba3e79ecceb98225a9034a85649807d8d00444feccf771b294a41d1e443f31'],
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-object-id-getter/inspection.json','ece892d8a5f9fea5a7575bcb85fee8270ff6e08becbba696e5c81beb00095475']
];
const limits={targets:4,code_bytes_each:2048,code_bytes_total:8192,pdata_search_steps_each:32,pdata_new_entries_each:34,
 pdata_bytes_total:1632,export_metadata_bytes:532,semantic_reads:148,dumpbin_calls:4,tiny_pe_bytes_each:8192,
 dumpbin_buffer_bytes:262144,dumpbin_timeout_ms:10000,hash_buffer_bytes:65536,proof_bytes_each:524288};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());
 assert(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const p=sources.map(([rel,sha])=>{const f=path.join(root,rel);normal(f,true);assert(fs.statSync(f).size<=limits.proof_bytes_each);const b=fs.readFileSync(f);assert.equal(hash(b),sha);return JSON.parse(b);});
const input=p[0],binding=p[1],old=p[2],id=p[3];
assert.equal(input.status,'fixed-global-load-inputs-observed');assert(input.source_hash_preserved);assert.equal(input.failures.length,0);
assert.equal(binding.status,'fixed-item-name-import-export-verified');assert(binding.source_hash_preserved);assert.equal(binding.failures.length,0);
function checkedModule(proof){const a=proof.files.filter(f=>f.name==='Engine');assert.equal(a.length,1);const f=a[0];
 assert.equal(f.path,original);assert.equal(f.size,size);assert.equal(f.expected_sha256,pin);assert.equal(f.sha256_before,pin);assert.equal(f.sha256_after,pin);
 assert(f.source_hash_preserved&&f.source_stat_stable&&f.source_closed);return f;}
const ep=checkedModule(binding),ip=checkedModule(input);assert.deepEqual(ip.pe,ep.pe);
assert.equal(old.status,'exact-actor-export-and-boundary-verified');assert.equal(old.original,original);assert.equal(old.expected_sha256,pin);
assert.equal(old.sha256_before,pin);assert.equal(old.sha256_after,pin);assert(old.source_hash_preserved&&old.source_stat_stable);assert.equal(old.failures.length,0);
assert.equal(id.status,'fixed-object-id-entry-decoded-manual-review-required');assert.equal(id.original,original);assert.equal(id.sha256_before,pin);assert.equal(id.sha256_after,pin);
assert(id.source_hash_preserved&&id.source_stat_stable&&id.source_closed);assert.equal(id.failures.length,0);
const pe=old.pe;assert.deepEqual(pe.sections,ep.pe.sections);assert.equal(pe.image_base,hex(base));assert.equal(pe.image_size,4521984);
assert.equal(pe.exception_rva,'0x424000');assert.equal(pe.exception_file_offset,4317696);assert.equal(pe.entry_count,12210);assert.equal(pe.exception_bytes,12210*12);
const exportPe=ep.pe,ed=Buffer.from(exportPe.export_directory_bytes,'hex');assert.equal(ed.length,40);
assert.equal(exportPe.named_export_count,6283);assert.equal(exportPe.function_count,6283);
assert.equal(ed.readUInt32LE(16),exportPe.ordinal_base);assert.equal(ed.readUInt32LE(20),exportPe.function_count);assert.equal(ed.readUInt32LE(24),exportPe.named_export_count);
assert.equal(ed.readUInt32LE(28),Number(exportPe.address_table_rva));assert.equal(ed.readUInt32LE(32),Number(exportPe.name_pointer_table_rva));assert.equal(ed.readUInt32LE(36),Number(exportPe.name_ordinal_table_rva));
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const a=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(a.length,1);
 const offset=a[0].raw_offset+rva-a[0].rva;assert(offset>=0&&offset<=size-n);return {section:a[0],offset};}
const pm=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pm.section.name,'.pdata');assert.equal(pm.offset,pe.exception_file_offset);
function decode(index,b){assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);assert.equal(b.length,12);
 const begin=b.readUInt32LE(),end=b.readUInt32LE(4),unwind=b.readUInt32LE(8);assert(begin>0&&begin<end&&end<=pe.image_size&&unwind>0&&unwind<pe.image_size);
 assert(map(begin,end-begin).section.characteristics&0x20000000);return {index,begin,end,unwind,raw_hex:b.toString('hex'),file_offset:pe.exception_file_offset+index*12};}
const cached=new Map();function addCache(index,raw,source){const e=decode(index,Buffer.from(raw,'hex'));
 if(cached.has(index))assert.equal(cached.get(index).raw_hex,e.raw_hex);else cached.set(index,{...e,source});}
for(const v of old.encountered_pdata){const e=decode(v.index,Buffer.from(v.raw_hex,'hex'));assert.equal(v.begin,e.begin);assert.equal(v.end,e.end);assert.equal(v.unwind,e.unwind);addCache(v.index,v.raw_hex,2);}
for(const v of id.reads.filter(x=>x.kind==='pdata')){assert.equal(v.bytes,12);addCache((v.file_offset-pe.exception_file_offset)/12,v.hex,3);}
const ordered=[...cached.values()].sort((a,b)=>a.index-b.index);for(let i=1;i<ordered.length;i++)assert(ordered[i-1].begin<ordered[i].begin);
const targets=[{label:'base-float',target:0x13ca00,slot:0x341f58},{label:'binary-float',target:0x13fd50,slot:0x342028},
 {label:'load-table-file',iat:'0x609090',hint:4239},{label:'get-load-table',iat:'0x609a90',hint:3007}];
for(const t of targets){t.status='pending';t.search_steps=0;t.new_pdata_entries=0;t.encountered_pdata=[];
 if(t.slot){const a=input.vtable_slots.filter(v=>v.slot_rva===hex(t.slot));assert.equal(a.length,1);
  assert.equal(a[0].preferred_pointer,hex(base+BigInt(t.target)));assert.equal(Buffer.from(a[0].raw_hex,'hex').readBigUInt64LE(),base+BigInt(t.target));t.cached_slot=a[0];}
 else{const a=input.import_bindings.filter(v=>v.iat_rva===t.iat);assert.equal(a.length,1);assert.equal(a[0].dll,'Engine.dll');assert.equal(a[0].hint,t.hint);
  assert(a[0].text.length>0&&a[0].text.length+1<=256);t.expected_symbol=a[0].text;t.cached_import=a[0];}}
const r={schema:'grim-dynasty-item-global-load-getters-v1',status:'started',started_utc:new Date().toISOString(),original,expected_sha256:pin,expected_file_bytes:size,
 sources,pe,cached_export_pe:exportPe,limits,targets,cached_pdata_entries:cached.size,original_open_mode:'O_RDONLY',original_loaded_or_executed:false,
 original_path_given_to_dumpbin:false,whole_tables_scanned:false,callees_followed:0,new_header_bytes:0,unwind_body_bytes:0,data_bytes:0,
 metadata_bytes:0,pdata_metadata_bytes:0,export_metadata_bytes:0,code_bytes:0,semantic_reads:0,dumpbin_calls:0,reads:[],source_closed:false,failures:[],
 limitations:['Exactly four requested targets only. Singleton is not resolved or read; no call/branch target is followed.',
  'Only the two supplied name hints are tried. A name mismatch, invalid ordinal or forwarded/non-executable target is a frontier; there is no alternate export search.',
  'Each target uses at most32 search steps and34 new runtime-function entries; earlier cached/new entries are reused across targets.',
  'No exact .pdata begin or a body larger than2048B selects no body. No gap, neighboring or partial body is substituted.',
  'At most four synthetic-PE dumpbin invocations run serially. All decoded bytes must match the exact body; data/invalid/coverage issues retain an explicit frontier.',
  'A full syntactic decode does not prove runtime reachability, callees, logical completeness, field behavior or loaded compatibility.',
  'Same-handle64KiB before/after hashes check source identity, not an atomic snapshot.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,kind,t){assert(r.semantic_reads<limits.semantic_reads);const m=map(rva,n);
 if(kind==='body'){assert(t&&rva===t.target&&n===t.boundary.end-t.boundary.begin&&n<=limits.code_bytes_each);assert(m.section.characteristics&0x20000000);
  assert(r.code_bytes+n<=limits.code_bytes_total);r.code_bytes+=n;}
 else{assert(!(m.section.characteristics&0x20000000));r.metadata_bytes+=n;
  if(kind==='pdata'){assert(t&&n===12);assert(t.new_pdata_entries<limits.pdata_new_entries_each);assert(r.pdata_metadata_bytes+n<=limits.pdata_bytes_total);t.new_pdata_entries++;r.pdata_metadata_bytes+=n;}
  else{assert(['hint-name-pointer','hint-name','hint-ordinal','export-address'].includes(kind));assert(r.export_metadata_bytes+n<=limits.export_metadata_bytes);r.export_metadata_bytes+=n;}}
 r.semantic_reads++;const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,m.offset);
 r.reads.push({target:t.label,kind,rva:hex(rva),file_offset:m.offset,requested_bytes:n,read_bytes:got,raw_hex:kind==='body'?undefined:b.subarray(0,got).toString('hex')});assert.equal(got,n,'Short fixed read');return b;}
function bind(t){
 const nameRva=read(Number(exportPe.name_pointer_table_rva)+t.hint*4,4,'hint-name-pointer',t).readUInt32LE();
 const named=read(nameRva,t.expected_symbol.length+1,'hint-name',t);t.export_name_observation={hint:t.hint,rva:hex(nameRva),raw_hex:named.toString('hex')};
 if(!named.equals(Buffer.from(t.expected_symbol+'\0','ascii'))){t.status='fixed-hint-name-mismatch';return false;}
 const index=read(Number(exportPe.name_ordinal_table_rva)+t.hint*2,2,'hint-ordinal',t).readUInt16LE();
 if(index>=exportPe.function_count){t.status='fixed-hint-ordinal-outside-export-table';return false;}
 const target=read(Number(exportPe.address_table_rva)+index*4,4,'export-address',t).readUInt32LE();
 t.export_binding={symbol:t.expected_symbol,hint:t.hint,name_rva:hex(nameRva),ordinal_index:index,ordinal:index+exportPe.ordinal_base,target_rva:hex(target)};
 if(target>=Number(exportPe.export_directory_rva)&&target<Number(exportPe.export_directory_rva)+exportPe.export_directory_size){t.status='forwarded-export-outside-scope';return false;}
 if(target===0||!pe.sections.some(s=>target>=s.rva&&target-s.rva<s.raw_size&&(s.characteristics&0x20000000))){t.status='non-executable-export-outside-scope';return false;}
 t.target=target;return true;
}
function boundary(t){const visited=new Map();function entry(index){if(visited.has(index))return visited.get(index);let e;
  if(cached.has(index))e={...cached.get(index),reused:true};else{assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);
   e={...decode(index,read(Number(pe.exception_rva)+index*12,12,'pdata',t)),source:t.label,reused:false};cached.set(index,e);}
  for(const [i,other] of visited)assert(i<index?other.begin<e.begin:other.begin>e.begin,'Encountered table order mismatch');visited.set(index,e);t.encountered_pdata.push(e);return e;}
 let lo=0,hi=pe.entry_count;while(lo<hi){assert(t.search_steps<limits.pdata_search_steps_each);t.search_steps++;
  const mid=lo+Math.floor((hi-lo)/2);if(entry(mid).begin<t.target)lo=mid+1;else hi=mid;}
 t.lower_bound_index=lo;const next=lo<pe.entry_count?entry(lo):null;
 if(!next||next.begin!==t.target){const previous=lo>0?entry(lo-1):null;t.previous=previous;t.next=next;
  t.status=previous&&t.target>=previous.begin&&t.target<previous.end?'no-exact-begin-containing-entry':'no-exact-begin-gap';return false;}
 const bytes=next.end-next.begin;t.boundary={...next,begin_rva:hex(next.begin),end_rva_exclusive:hex(next.end),bytes,code_file_offset:map(next.begin,bytes).offset};
 if(bytes>limits.code_bytes_each){t.status='exact-body-exceeds2048B-budget';return false;}return true;
}
function decodeBody(t){const code=read(t.target,t.boundary.bytes,'body',t),stem=t.label+'-'+t.target.toString(16);
 save(stem+'-exact.bin',code);t.code_sha256=hash(code);t.code_bytes=code.length;
 const page=t.target&~0xfff,delta=t.target-page,virtualBytes=delta+code.length,rawBytes=Math.ceil(virtualBytes/512)*512;
 const b=Buffer.alloc(512+rawBytes);assert(b.length<=limits.tiny_pe_bytes_each);const coff=0x84,opt=0x98,sec=0x188;
 b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawBytes,opt+4);b.writeUInt32LE(page,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);
 b.writeUInt32LE(Math.ceil((page+virtualBytes)/4096)*4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
 b.write('.text',sec,'ascii');b.writeUInt32LE(virtualBytes,sec+8);b.writeUInt32LE(page,sec+12);b.writeUInt32LE(rawBytes,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);
 const tiny=path.join(temp,stem+'.inspect.dll');normal(temp);fs.writeFileSync(tiny,b,{flag:'wx'});t.synthetic={path:tiny,bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(t.target))+','+hex(base+BigInt(t.boundary.end-1)),tiny];
 assert(r.dumpbin_calls<limits.dumpbin_calls);r.dumpbin_calls++;t.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:limits.dumpbin_timeout_ms,maxBuffer:limits.dumpbin_buffer_bytes,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save(stem+'-dumpbin.txt',child.stdout||'');save(stem+'-stderr.txt',child.stderr||'');t.tool_exit=child.status;
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),t.synthetic.sha256);
 const rows=[],issues=[];function issue(kind,where,detail){if(issues.length<32)issues.push({kind,where,detail});}
 for(const line of(child.stdout||'').split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
  if(m){rows.push({address:hex(BigInt('0x'+m[1])),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
  const tail=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(tail){if(rows.length)rows[rows.length-1].bytes+=tail[1].replace(/\s/g,'').toLowerCase();else issue('orphan-byte-line',null,line.trim());}}
 save(stem+'-instructions.json',JSON.stringify(rows,null,2)+'\n');let cursor=base+BigInt(t.target);
 for(const row of rows){const address=BigInt(row.address),n=row.bytes.length/2;if(address!==cursor)issue('noncontiguous-address',row.address,hex(cursor));
  if(!Number.isInteger(n)||n<1||n>15)issue('invalid-instruction-width',row.address,String(n));
  if(!row.instruction||/[?]|\(bad\)|invalid/i.test(row.instruction)||/^(?:db|dw|dd|dq)\b/i.test(row.instruction))issue('invalid-or-data-decode',row.address,row.instruction);
  cursor=address+BigInt(n);}
 if(cursor!==base+BigInt(t.boundary.end))issue('end-boundary-mismatch',hex(cursor),hex(base+BigInt(t.boundary.end)));
 if(rows.map(x=>x.bytes).join('')!==code.toString('hex'))issue('byte-concatenation-mismatch',null,'Exact bytes differ');
 t.instructions=rows.length;t.decode_issues=issues;t.byte_coverage_verified=issues.length===0;
 t.calls=rows.filter(x=>/^call\b/i.test(x.instruction));t.branches=rows.filter(x=>/^(?:j[a-z]+|loop[a-z]*|xbegin)\b/i.test(x.instruction));t.returns=rows.filter(x=>/^ret\b/i.test(x.instruction));
 t.status=t.byte_coverage_verified?'exact-body-decoded-review-required':'fixed-body-decode-frontier';
}
try{
 fs.mkdirSync(out);created=true;normal(out);fs.mkdirSync(temp);normal(temp);r.script_sha256=fingerprint(self);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);
 r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 for(const t of targets){if(t.hint&&!bind(t))continue;if(!boundary(t))continue;decodeBody(t);}
 r.status=targets.every(t=>t.status==='exact-body-decoded-review-required')?'four-fixed-global-load-bodies-decoded-review-required':'fixed-global-load-getters-with-frontiers';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);
  r.source_stat_stable=s.isFile()&&s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}}
  catch(e){r.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);r.source_closed=true;}catch(e){r.failures.push('Close: '+e);}}}
 if(r.tool_before!==undefined){try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}}
 r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,targets:targets.map(t=>({label:t.label,target:t.target===undefined?null:hex(t.target),status:t.status,boundary:t.boundary,code_bytes:t.code_bytes,instructions:t.instructions})),
 metadata_bytes:r.metadata_bytes,pdata_metadata_bytes:r.pdata_metadata_bytes,export_metadata_bytes:r.export_metadata_bytes,code_bytes:r.code_bytes,dumpbin_calls:r.dumpbin_calls,
 source_closed:r.source_closed,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
