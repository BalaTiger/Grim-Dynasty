// Prepared only: Engine Binary vtable+98 slot and one fixed GetLoadTable tail target.
// Two exact .pdata bodies <=1024B each. No helper/format/callee recursion.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_global_load_direct.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632,pin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-global-load-direct'),temp=path.join(root,'.tooling/offline-item-global-load-direct-temp'),base=0x180000000n,slot=0x342078;
const sources=[
 ['docs/object-lifecycle/offline-item-global-load-leaves/inspection.json','75d55f7a959c2b012cc5e9374a25bf588f6457bf1f2ef5a8867805942a2d1e66'],
 ['docs/object-lifecycle/offline-item-global-load-getters/inspection.json','4be3c63f988ab540100dbb80129506bc13fa254f5bfa868fd358b45090900b78']
];
const limits={code_bytes_each:1024,code_bytes_total:2048,data_bytes:8,pdata_steps_each:32,new_pdata_entries_each:34,metadata_bytes:816,
 semantic_reads:71,dumpbin_calls:2,tiny_pe_bytes_each:8192,dumpbin_buffer_bytes:131072,dumpbin_timeout_ms:10000,hash_buffer_bytes:65536,proof_bytes_each:524288};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());
 assert(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const p=sources.map(([rel,sha])=>{const f=path.join(root,rel);normal(f,true);assert(fs.statSync(f).size<=limits.proof_bytes_each);const b=fs.readFileSync(f);assert.equal(hash(b),sha);return JSON.parse(b);});
const leaf=p[0],prior=p[1],pe=prior.pe;
assert.equal(leaf.status,'fixed-global-load-leaves-decoded-review-required');assert.equal(prior.status,'fixed-global-load-getters-with-frontiers');
for(const v of p){assert.equal(v.original,original);assert.equal(v.expected_sha256,pin);assert.equal(v.expected_file_bytes,size);assert.equal(v.sha256_before,pin);assert.equal(v.sha256_after,pin);
 assert(v.source_hash_preserved&&v.source_stat_stable&&v.source_closed);assert.equal(v.failures.length,0);}
assert.deepEqual(leaf.pe,pe);assert.deepEqual(leaf.source,sources[1]);assert.equal(pe.image_base,hex(base));assert.equal(pe.image_size,4521984);
assert.equal(pe.exception_rva,'0x424000');assert.equal(pe.exception_file_offset,4317696);assert.equal(pe.entry_count,12210);assert.equal(pe.exception_bytes,12210*12);
function fixedLeaf(label,start,raw){const a=leaf.targets.filter(t=>t.label===label);assert.equal(a.length,1);const v=a[0];assert.equal(v.start,start);assert(v.byte_coverage_verified);
 assert.equal(v.entry_prefix.map(x=>x.bytes).join(''),raw);return v;}
const binary=fixedLeaf('binary-float',0x13fd50,'488b010f28da4533c048ffa098000000');
const get=fixedLeaf('get-load-table',0x17ee80,'4881c170010000e9a45d0400');
const jmp=Buffer.from(get.entry_prefix[1].bytes,'hex');assert.equal(jmp[0],0xe9);assert.equal(0x17ee8c+jmp.readInt32LE(1),0x1c4c30);
const vb=prior.targets.filter(t=>t.label==='binary-float');assert.equal(vb.length,1);
assert.equal(vb[0].cached_slot.cached_export.rva,'0x341fe0');assert.equal(vb[0].cached_slot.cached_export.class_name,'LoadTableBinary');assert.equal(0x341fe0+0x98,slot);
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);const a=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(a.length,1);
 const offset=a[0].raw_offset+rva-a[0].rva;assert(offset>=0&&offset<=size-n);return {section:a[0],offset};}
const pm=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pm.section.name,'.pdata');assert.equal(pm.offset,pe.exception_file_offset);
function decode(index,b){assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);assert.equal(b.length,12);const begin=b.readUInt32LE(),end=b.readUInt32LE(4),unwind=b.readUInt32LE(8);
 assert(begin>0&&begin<end&&end<=pe.image_size&&unwind>0&&unwind<pe.image_size);assert(map(begin,end-begin).section.characteristics&0x20000000);
 return {index,begin,end,unwind,raw_hex:b.toString('hex'),file_offset:pe.exception_file_offset+index*12};}
const cache=new Map();function cacheEntry(v,source){const e=decode(v.index,Buffer.from(v.raw_hex,'hex'));assert.equal(v.begin,e.begin);assert.equal(v.end,e.end);assert.equal(v.unwind,e.unwind);assert.equal(v.file_offset,e.file_offset);
 if(cache.has(e.index))assert.equal(cache.get(e.index).raw_hex,e.raw_hex);else cache.set(e.index,{...e,source});}
for(const t of prior.targets)for(const e of t.encountered_pdata)cacheEntry(e,1);
cacheEntry(leaf.conditional_body.cached_pdata,0);assert.equal(leaf.conditional_body.cached_pdata.begin,0x13fd70);assert.equal(leaf.conditional_body.cached_pdata.end,0x13fdfb);
const ordered=[...cache.values()].sort((a,b)=>a.index-b.index);for(let i=1;i<ordered.length;i++)assert(ordered[i-1].begin<ordered[i].begin);
const targets=[{label:'binary-indexed-float',slot,status:'awaiting-fixed-slot'},{label:'get-load-table-tail',target:0x1c4c30,status:'pending'}];
for(const t of targets){t.search_steps=0;t.new_pdata_entries=0;t.encountered_pdata=[];}
const r={schema:'grim-dynasty-item-global-load-direct-v1',status:'started',started_utc:new Date().toISOString(),original,expected_sha256:pin,expected_file_bytes:size,sources,pe,limits,
 cached_binary_prefix:binary.entry_prefix,cached_get_table_prefix:get.entry_prefix,cached_binary_export:vb[0].cached_slot.cached_export,cached_pdata_entries:cache.size,targets,
 metadata_bytes:0,data_bytes:0,code_bytes:0,semantic_reads:0,dumpbin_calls:0,reads:[],source_closed:false,failures:[],original_open_mode:'O_RDONLY',
 original_loaded_or_executed:false,original_path_given_to_dumpbin:false,whole_tables_scanned:false,new_header_bytes:0,unwind_body_bytes:0,recursive_callees:0,
 limitations:['Only Engine slot342078 is read to select its actual disk target. The vtable export base and +98 jump are independently cached; no target is guessed from neighboring addresses.',
  'The second target is exactly1C4C30 from the saved adjusted-this tail jump.1C4D20, Base helper4DD80 and format316034 are not read.',
  'Only exact .pdata begin/body <=1024B is selected. Missing begin, gap, containing entry or excess size remains a frontier with no alternate/partial code read.',
  'A cached exact13FD70 entry is reused if and only if the newly observed slot actually selects13FD70. It is not read merely because it is adjacent.',
  'At most two synthetic-PE dumpbin calls run serially. Calls/branches are reported without recursive traversal.',
  'Complete byte decoding is not a proof of reachable paths, runtime vtable identity, exception cleanup or loaded compatibility.',
  'Same-handle64KiB before/after hashes check identity, not atomic snapshot.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,kind,t){assert(r.semantic_reads<limits.semantic_reads);const m=map(rva,n);
 if(kind==='slot'){assert.equal(rva,slot);assert.equal(n,8);assert.equal(r.data_bytes,0);assert(!(m.section.characteristics&0x20000000));r.data_bytes=8;}
 else if(kind==='pdata'){assert.equal(n,12);assert(!(m.section.characteristics&0x20000000));assert(t.new_pdata_entries<34);assert(r.metadata_bytes+n<=816);t.new_pdata_entries++;r.metadata_bytes+=n;}
 else{assert.equal(kind,'body');assert.equal(rva,t.target);assert.equal(n,t.boundary.bytes);assert(n<=1024);assert(r.code_bytes+n<=2048);assert(m.section.characteristics&0x20000000);r.code_bytes+=n;}
 r.semantic_reads++;const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,m.offset);r.reads.push({kind,target:t.label,rva:hex(rva),file_offset:m.offset,requested_bytes:n,read_bytes:got,raw_hex:kind==='body'?undefined:b.subarray(0,got).toString('hex')});assert.equal(got,n);return b;}
function findBoundary(t){const exact=[...cache.values()].filter(e=>e.begin===t.target);assert(exact.length<=1);let found;
 if(exact.length){found={...exact[0],reused:true};t.encountered_pdata.push(found);t.boundary_from_cached_exact=true;}
 else{const seen=new Map();function entry(index){if(seen.has(index))return seen.get(index);let e;if(cache.has(index))e={...cache.get(index),reused:true};
   else{assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);e={...decode(index,read(Number(pe.exception_rva)+index*12,12,'pdata',t)),source:t.label,reused:false};cache.set(index,e);}
   for(const [i,other] of seen)assert(i<index?other.begin<e.begin:other.begin>e.begin);seen.set(index,e);t.encountered_pdata.push(e);return e;}
  let lo=0,hi=pe.entry_count;while(lo<hi){assert(t.search_steps<32);t.search_steps++;const mid=lo+Math.floor((hi-lo)/2);if(entry(mid).begin<t.target)lo=mid+1;else hi=mid;}
  t.lower_bound_index=lo;const next=lo<pe.entry_count?entry(lo):null;
  if(!next||next.begin!==t.target){const previous=lo>0?entry(lo-1):null;t.previous=previous;t.next=next;t.status=previous&&t.target>=previous.begin&&t.target<previous.end?'no-exact-begin-containing-entry':'no-exact-begin-gap';return false;}found=next;}
 const n=found.end-found.begin;t.boundary={...found,begin_rva:hex(found.begin),end_rva_exclusive:hex(found.end),bytes:n,code_file_offset:map(found.begin,n).offset};
 if(n>1024){t.status='exact-body-exceeds1024B-budget';return false;}return true;
}
function body(t){const code=read(t.target,t.boundary.bytes,'body',t),stem=t.label+'-'+t.target.toString(16);save(stem+'-exact.bin',code);t.code_sha256=hash(code);t.code_bytes=code.length;
 const page=t.target&~0xfff,delta=t.target-page,virtualBytes=delta+code.length,rawBytes=Math.ceil(virtualBytes/512)*512,b=Buffer.alloc(512+rawBytes);assert(b.length<=8192);const coff=0x84,opt=0x98,sec=0x188;
 b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawBytes,opt+4);b.writeUInt32LE(page,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);
 b.writeUInt32LE(Math.ceil((page+virtualBytes)/4096)*4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);b.write('.text',sec,'ascii');b.writeUInt32LE(virtualBytes,sec+8);b.writeUInt32LE(page,sec+12);b.writeUInt32LE(rawBytes,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);
 const tiny=path.join(temp,stem+'.inspect.dll');normal(temp);fs.writeFileSync(tiny,b,{flag:'wx'});t.synthetic={path:tiny,bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(t.target))+','+hex(base+BigInt(t.boundary.end-1)),tiny];
 assert(r.dumpbin_calls<2);r.dumpbin_calls++;t.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:limits.dumpbin_timeout_ms,maxBuffer:limits.dumpbin_buffer_bytes,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save(stem+'-dumpbin.txt',child.stdout||'');save(stem+'-stderr.txt',child.stderr||'');t.tool_exit=child.status;assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),t.synthetic.sha256);
 const rows=[],issues=[];function issue(kind,at,detail){if(issues.length<32)issues.push({kind,at,detail});}
 for(const line of(child.stdout||'').split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);if(m){rows.push({address:hex(BigInt('0x'+m[1])),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
  const tail=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(tail){if(rows.length)rows[rows.length-1].bytes+=tail[1].replace(/\s/g,'').toLowerCase();else issue('orphan-bytes',null,line.trim());}}
 save(stem+'-instructions.json',JSON.stringify(rows,null,2)+'\n');let cursor=base+BigInt(t.target);
 for(const v of rows){const at=BigInt(v.address),n=v.bytes.length/2;if(at!==cursor)issue('noncontiguous-address',v.address,hex(cursor));if(!Number.isInteger(n)||n<1||n>15)issue('invalid-width',v.address,String(n));
  if(!v.instruction||/[?]|\(bad\)|invalid/i.test(v.instruction)||/^(?:db|dw|dd|dq)\b/i.test(v.instruction))issue('invalid-or-data',v.address,v.instruction);cursor=at+BigInt(n);}
 if(cursor!==base+BigInt(t.boundary.end))issue('end-boundary-mismatch',hex(cursor),hex(base+BigInt(t.boundary.end)));if(rows.map(v=>v.bytes).join('')!==code.toString('hex'))issue('byte-concatenation-mismatch',null,'Exact bytes differ');
 t.instructions=rows.length;t.decode_issues=issues;t.byte_coverage_verified=issues.length===0;t.calls=rows.filter(v=>/^call\b/i.test(v.instruction));t.branches=rows.filter(v=>/^(?:j[a-z]+|loop[a-z]*|xbegin)\b/i.test(v.instruction));t.returns=rows.filter(v=>/^ret\b/i.test(v.instruction));t.status=t.byte_coverage_verified?'exact-body-decoded-review-required':'fixed-body-decode-frontier';
}
try{
 fs.mkdirSync(out);created=true;normal(out);fs.mkdirSync(temp);normal(temp);r.script_sha256=fingerprint(self);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 const value=read(slot,8,'slot',targets[0]).readBigUInt64LE();r.slot_observation={rva:hex(slot),preferred_pointer:hex(value)};
 if(value<base||value>=base+BigInt(pe.image_size))targets[0].status='slot-target-outside-fixed-engine-image';
 else{const target=Number(value-base);if(!pe.sections.some(s=>target>=s.rva&&target-s.rva<s.raw_size&&(s.characteristics&0x20000000)))targets[0].status='slot-target-not-executable-mapped';
  else{targets[0].target=target;targets[0].status='pending';}}
 for(const t of targets){if(t.target===undefined)continue;if(findBoundary(t))body(t);}
 r.status=targets.every(t=>t.status==='exact-body-decoded-review-required')?'two-fixed-global-load-direct-bodies-decoded-review-required':'fixed-global-load-direct-with-frontiers';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);r.source_stat_stable=s.isFile()&&s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}}
  catch(e){r.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);r.source_closed=true;}catch(e){r.failures.push('Close: '+e);}}}
 if(r.tool_before!==undefined){try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}}
 r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,slot_observation:r.slot_observation,targets:targets.map(t=>({label:t.label,target:t.target===undefined?null:hex(t.target),status:t.status,boundary:t.boundary,code_bytes:t.code_bytes,instructions:t.instructions})),
 metadata_bytes:r.metadata_bytes,data_bytes:r.data_bytes,code_bytes:r.code_bytes,dumpbin_calls:r.dumpbin_calls,source_closed:r.source_closed,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
