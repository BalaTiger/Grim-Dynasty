// Prepared only: fixed6B Game5DC344 thunk and its one named copy import.
// No original module load, neighbor/callee body, dumpbin, build or tests.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/resolve_item_name_copy.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856,pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const out=path.join(root,'docs/object-lifecycle/offline-item-name-copy'),target=0x5dc344,base=0x180000000n;
const sources=[
 ['docs/object-lifecycle/offline-item-name-assign/inspection.json','5a9167e7cf619e8c07ff33d74fee66e021de32bf6c638e54b67758eb7579c317'],
 ['docs/object-lifecycle/offline-item-name-assign/016460-instructions.json','acf327dda597d7a6eb974fb35ebd9720d79b102d015fafd11585fd122454dfb5'],
 ['docs/object-lifecycle/offline-class-helper-storage-imports/metadata.json','8b8138b7268652cad7c60683ce0b0d7fa366e3903cd0b4b9ade7ca9e3767e9c1'],
 ['docs/phase-1/boundary-audit/module-boundaries.json','d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());
 assert(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing evidence preserved');}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
normal(self,true);normal(path.dirname(out));absent(out);
const proofs=sources.map(([rel,sha],i)=>{const p=path.join(root,rel);normal(p,true);assert(fs.statSync(p).size<=(i===3?1048576:262144));
 const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
const prior=proofs[0],rows=proofs[1],imports=proofs[2],pe=prior.pe;
assert.equal(prior.status,'exact-name-assign-body-decoded-review-required');assert.equal(prior.original,original);
assert(prior.source_hash_preserved&&prior.source_stat_stable&&prior.source_closed);assert.equal(prior.sha256_before,pin);assert.equal(prior.sha256_after,pin);
const calls=rows.filter(r=>r.address==='0x18001655e');assert.equal(calls.length,1);
const callRaw=Buffer.from(calls[0].bytes,'hex');assert.equal(callRaw.toString('hex'),'e8e15d5c00');assert.equal(0x1655e+5+callRaw.readInt32LE(1),target);
assert.equal(imports.status,'fixed-class-helper-storage-imports-resolved');assert.equal(imports.target,original);
assert(imports.source_hash_preserved&&imports.source_stat_stable);assert.equal(imports.sha256_before,pin);assert.equal(imports.sha256_after,pin);
const descriptors=imports.metadata_reads.filter(r=>r.kind==='fixed-import-descriptor-5');assert.equal(descriptors.length,1);
const descriptor=descriptors[0],db=Buffer.from(descriptor.raw_bytes,'hex');assert.equal(db.length,20);
assert.equal(descriptor.rva,'0xa429a4');assert.equal(descriptor.read_bytes,20);assert.equal(db.readUInt32LE(),0xa45258);
assert.equal(db.readUInt32LE(4),0);assert.equal(db.readUInt32LE(8),0);assert.equal(db.readUInt32LE(12),0xa57722);assert.equal(db.readUInt32LE(16),0x60b798);
const moduleRows=imports.metadata_reads.filter(r=>r.kind==='exact-module-name-5');assert.equal(moduleRows.length,1);
assert.equal(moduleRows[0].rva,'0xa57722');assert.equal(Buffer.from(moduleRows[0].raw_bytes,'hex').toString('ascii'),'VCRUNTIME140.dll\0');
const gameModules=proofs[3].filter(m=>m.relativePath==='x64/Game.dll');assert.equal(gameModules.length,1);assert.equal(gameModules[0].sha256,pin);
const cachedModule=gameModules[0].importModules[5];assert.equal(cachedModule.module,'VCRUNTIME140.dll');
assert.equal(cachedModule.symbols[5],'memcpy');assert.equal(cachedModule.symbols[14],'memmove');
const allowed=[{slot:0x60b7c0,index:5,name:'memcpy'},{slot:0x60b808,index:14,name:'memmove'}];
for(const c of allowed)assert.equal(db.readUInt32LE(16)+8*c.index,c.slot);
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const sections=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(sections.length,1);
 const offset=sections[0].raw_offset+rva-sections[0].rva;assert(offset>=0&&offset<=size-n);return {section:sections[0],offset};}
assert(map(target,6).section.characteristics&0x20000000);assert.equal(map(0xa429a4,20).offset,descriptor.file_offset);
const r={schema:'grim-dynasty-item-name-copy-import-v1',status:'started',started_utc:new Date().toISOString(),original,expected_sha256:pin,expected_file_bytes:size,
 sources,caller:calls[0],cached_descriptor:descriptor,cached_module:moduleRows[0],allowed_candidates:allowed,
 limits:{code_bytes:6,metadata_bytes:138,semantic_reads:4,hash_buffer_bytes:65536},reads:[],code_bytes:0,metadata_bytes:0,
 original_open_mode:'O_RDONLY',original_loaded_or_executed:false,callees_followed:0,subprocesses:0,source_closed:false,failures:[],
 limitations:['Only exactFF25 decoding is accepted. A different or truncated prefix stops without another code read.',
  'Only the two independently cached copy-import slots are admitted; a different slot is reported without metadata discovery.',
  'Descriptor/name cache proves ordinary import ownership. Newly readOFT prefix must have no terminator before the target.',
  'Import identity is not a loaded implementation observation. Native overlapping-copy, fault, exception or allocator behavior is not executed.',
  'Same-handle before/after64KiB hashes are identity checks, not an atomic snapshot.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,kind){assert(r.reads.length<r.limits.semantic_reads);const m=map(rva,n);
 if(kind==='thunk'){assert.equal(r.reads.length,0);assert.equal(rva,target);assert.equal(n,6);r.code_bytes=n;}
 else{assert(!(m.section.characteristics&0x20000000));assert(r.metadata_bytes+n<=r.limits.metadata_bytes);r.metadata_bytes+=n;}
 const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,m.offset);r.reads.push({kind,rva:hex(rva),file_offset:m.offset,requested_bytes:n,read_bytes:got,raw_hex:b.subarray(0,got).toString('hex')});assert.equal(got,n);return b;}
try{
 fs.mkdirSync(out);created=true;normal(out);r.script_sha256=hash(fs.readFileSync(self));
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);
 r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 const raw=read(target,6,'thunk');save('5dc344-entry.bin',raw);
 if(raw[0]!==0xff||raw[1]!==0x25){r.status='fixed-thunk-prefix-unresolved';r.reason='Expected exact6B FF25; no extension authorized';}
 else{
  const slot=target+6+raw.readInt32LE(2);r.thunk={rva:hex(target),raw_hex:raw.toString('hex'),slot_rva:hex(slot),instruction:'jmp qword ptr ['+hex(base+BigInt(slot))+']'};
  const chosen=allowed.find(c=>c.slot===slot);
  if(!chosen){r.status='copy-import-slot-outside-fixed-candidates';r.reason='No descriptor/slot scan authorized';}
  else{
   const prefix=read(db.readUInt32LE(),8*(chosen.index+1),'ownership-oft-prefix');
   for(let i=0;i<=chosen.index;i++){const v=prefix.readBigUInt64LE(i*8);assert(v>0n&&v<=0xffffffffn,'Terminated/ordinal/bound lookup outside scope');}
   const nameRva=Number(prefix.readBigUInt64LE(chosen.index*8));
   const iat=read(slot,8,'disk-iat');assert(iat.equals(prefix.subarray(chosen.index*8,chosen.index*8+8)),'Disk IAT/OFT mismatch');
   const named=read(nameRva,2+chosen.name.length+1,'exact-hint-name');assert.equal(named.subarray(2).toString('ascii'),chosen.name+'\0');
   r.binding={dll:'VCRUNTIME140.dll',symbol:chosen.name,hint:named.readUInt16LE(),descriptor_index:5,
    thunk_index:chosen.index,iat_rva:hex(slot),oft_rva:hex(db.readUInt32LE()+8*chosen.index),hint_name_rva:hex(nameRva)};
   r.status='fixed-copy-thunk-and-import-verified';
  }
 }
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);
  r.source_stat_stable=s.isFile()&&s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}}
  catch(e){r.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);r.source_closed=true;}catch(e){r.failures.push('Close: '+e);}}}
 r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,binding:r.binding,code_bytes:r.code_bytes,metadata_bytes:r.metadata_bytes,source_closed:r.source_closed,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
