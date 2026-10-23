// Prepared for review only. Fixed Game 60BA70 import, 7A8164 scalar and 34B cache anchors.
// No original execution, dumpbin, callee/neighbor discovery, global slot read, build or tests.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/resolve_item_return_inputs.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856,pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const out=path.join(root,'docs/object-lifecycle/offline-item-return-inputs'),slot=0x60ba70,scalar=0x7a8164;
const sources=[
 ['docs/object-lifecycle/offline-item-return-dependency/inspection.json','fc0f85739966bf84241cc277b6c4d2ae7129a9daa27792ed1245e9cb7b409ff0'],
 ['docs/object-lifecycle/offline-item-return-dependency/324560-instructions.json','1998091f5ea21ab1f993401c4c83fa793760f286621a7b7d9d21d6abd8d854ef'],
 ['docs/object-lifecycle/offline-class-helper-storage-imports/metadata.json','8b8138b7268652cad7c60683ce0b0d7fa366e3903cd0b4b9ade7ca9e3767e9c1'],
 ['docs/phase-1/boundary-audit/module-boundaries.json','d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'],
 ['docs/task-kills/ghidra-selected/subscriber-constructor-routes.json','e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165'],
 ['docs/phase-1/round-4/Game-function-evidence-bounded-v2.json','1e8421fc6b83004fd39259ca19ba97faa7df520cf49200572cecd8a2bb80c502']
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
assert.equal(prior.status,'exact-return-dependency-body-decoded-review-required');assert.equal(prior.original,original);
assert(prior.source_hash_preserved&&prior.source_stat_stable&&prior.source_closed);assert.equal(prior.sha256_before,pin);assert.equal(prior.sha256_after,pin);
assert.equal(prior.exact_boundary.begin_rva,'0x324560');assert.equal(prior.exact_boundary.end_rva_exclusive,'0x3245db');
function rowAt(rva){const a=rows.filter(v=>v.address===hex(0x180000000+rva));assert.equal(a.length,1);return a[0];}
const call=rowAt(0x3245a2),callRaw=Buffer.from(call.bytes,'hex');assert.equal(callRaw.toString('hex'),'ff15c8742e00');assert.equal(0x3245a8+callRaw.readInt32LE(2),slot);
const scaleReads=[0x324580,0x324590,0x3245b1].map(rva=>{const row=rowAt(rva),b=Buffer.from(row.bytes,'hex');
 assert.equal(b.length,8);assert.equal(b.subarray(0,3).toString('hex'),'f30f59');assert(b[3]===5||b[3]===13);assert.equal(rva+8+b.readInt32LE(4),scalar);return row;});
assert.equal(imports.status,'fixed-class-helper-storage-imports-resolved');assert.equal(imports.target,original);
assert(imports.source_hash_preserved&&imports.source_stat_stable);assert.equal(imports.sha256_before,pin);assert.equal(imports.sha256_after,pin);
const descriptors=imports.metadata_reads.filter(r=>r.kind==='fixed-import-descriptor-5');assert.equal(descriptors.length,1);
const priorDescriptor=descriptors[0],pd=Buffer.from(priorDescriptor.raw_bytes,'hex');assert.equal(priorDescriptor.rva,'0xa429a4');assert.equal(pd.length,20);
assert.equal(pd.toString('hex'),'5852a40000000000000000002277a50098b76000');
// One fixed candidate from the independently cached import-module order; no descriptor scan.
const descriptorRva=Number(priorDescriptor.rva)+(8-5)*20;assert.equal(descriptorRva,0xa429e0);
const games=proofs[3].filter(m=>m.relativePath==='x64/Game.dll');assert.equal(games.length,1);assert.equal(games[0].sha256,pin);
const dll='api-ms-win-crt-utility-l1-1-0.dll',symbol='rand',cachedModule=games[0].importModules[8];
assert.equal(cachedModule.module,dll);assert.deepEqual(cachedModule.symbols,['rand','srand']);
const ctors=proofs[4].functions.filter(f=>f.observed_entry==='1802c07a0');assert.equal(ctors.length,1);
assert.equal(ctors[0].body,'[[1802c07a0, 1802c1ba6]]');
const ctorLines=['1802c07c6: MOV RDI,RCX','1802c0d23: MOV dword ptr [RDI + 0x1b58],0x40400000',
 '1802c0d2d: MOV qword ptr [RDI + 0x1b5c],0x40a00000','1802c1b8b: MOV RAX,RDI'];
for(const line of ctorLines)assert(ctors[0].assembly.includes(line));
const pubs=proofs[5].filter(f=>f.start==='18036b240');assert.equal(pubs.length,1);
const publication='18036b25f MOV qword ptr [0x180a66288],RAX';assert(pubs[0].instructions.includes(publication));
assert(pubs[0].instructions.includes('18036b257 CALL 0x1802c07a0'));
const anchors=[
 {rva:0x2c0d23,bytes:21,expected:'c787581b00000000404048c7875c1b00000000a040',meaning:'DWORD[this+1B58]=40400000; QWORD[this+1B5C]=0000000040A00000'},
 {rva:0x36b25f,bytes:7,expected:'48890522b06f00',meaning:'QWORD[Game+A66288]=RAX'},
 {rva:0x2c07c6,bytes:3,expected:'488bf9',meaning:'RDI=RCX'},
 {rva:0x2c1b8b,bytes:3,expected:'488bc7',meaning:'RAX=RDI'}
];
for(const a of anchors)assert.equal(Buffer.from(a.expected,'hex').length,a.bytes);
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const sections=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(sections.length,1);
 const offset=sections[0].raw_offset+rva-sections[0].rva;assert(offset>=0&&offset<=size-n);return {section:sections[0],offset};}
assert.equal(map(Number(priorDescriptor.rva),20).offset,priorDescriptor.file_offset);
const r={schema:'grim-dynasty-item-return-inputs-v1',status:'started',started_utc:new Date().toISOString(),original,expected_sha256:pin,expected_file_bytes:size,
 sources,caller:call,scale_reads:scaleReads,cached_descriptor_anchor:priorDescriptor,cached_constructor_lines:ctorLines,cached_publication:publication,
 limits:{code_bytes:34,data_bytes:4,metadata_bytes:128,semantic_reads:10,hash_buffer_bytes:65536},reads:[],code_bytes:0,data_bytes:0,metadata_bytes:0,
 original_open_mode:'O_RDONLY',original_loaded_or_executed:false,callees_followed:0,subprocesses:0,global_slot_read:false,source_closed:false,failures:[],
 limitations:['Only one fixed import descriptor and its first lookup/name are inspected; mismatch stops without discovery.',
  'The scale is a disk scalar observation. Import binding does not observe loaded rand state, implementation or return values.',
  'The34B anchors rebind four cached instruction sites to the pinned file; they do not execute constructor, publication or prove subsequent writer absence.',
  'QWORD initialization at1B5C is distinct from the Item caller DWORD read. No live global pointer or GameEngine object is inferred from disk zeros.',
  'SSE/MXCSR, conversion exceptions, signed IDIV and abnormal paths are not executed. Zero span still falls through rand and IDIV0.',
  'Same-handle before/after64KiB hashes check identity, not atomic snapshot or loaded compatibility.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,kind){assert(r.reads.length<r.limits.semantic_reads);const m=map(rva,n);
 if(kind==='code-anchor'){assert(anchors.some(a=>a.rva===rva&&a.bytes===n));assert(m.section.characteristics&0x20000000);assert(r.code_bytes+n<=34);r.code_bytes+=n;}
 else if(kind==='scale'){assert.equal(rva,scalar);assert.equal(n,4);assert.equal(r.data_bytes,0);assert(!(m.section.characteristics&0x20000000));r.data_bytes=n;}
 else{assert(!(m.section.characteristics&0x20000000));assert(r.metadata_bytes+n<=128);r.metadata_bytes+=n;}
 const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,m.offset);r.reads.push({kind,rva:hex(rva),file_offset:m.offset,requested_bytes:n,read_bytes:got,raw_hex:b.subarray(0,got).toString('hex')});assert.equal(got,n);return b;}
try{
 fs.mkdirSync(out);created=true;normal(out);r.script_sha256=hash(fs.readFileSync(self));
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);
 r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 const db=read(descriptorRva,20,'fixed-descriptor-8');assert.equal(db.readUInt32LE(4),0);assert.equal(db.readUInt32LE(8),0);
 assert.equal(db.readUInt32LE(16),slot,'Fixed candidate does not own the known IAT');
 const oft=db.readUInt32LE(),name=db.readUInt32LE(12);assert(oft>0&&name>0);
 const moduleName=read(name,dll.length+1,'exact-module-name-8');assert.equal(moduleName.toString('ascii'),dll+'\0');
 const first=read(oft,8,'first-oft-entry');const namedRva=first.readBigUInt64LE();assert(namedRva>0n&&namedRva<=0xffffffffn,'Ordinal, missing or bound lookup outside fixed scope');
 const iat=read(slot,8,'disk-iat');assert(iat.equals(first),'Disk IAT/OFT mismatch');
 const named=read(Number(namedRva),2+symbol.length+1,'exact-hint-name');assert.equal(named.subarray(2).toString('ascii'),symbol+'\0');
 r.binding={dll,symbol,hint:named.readUInt16LE(),descriptor_index:8,descriptor_rva:hex(descriptorRva),thunk_index:0,iat_rva:hex(slot),oft_rva:hex(oft),hint_name_rva:hex(namedRva)};
 const constant=read(scalar,4,'scale');save('7a8164-scalar.bin',constant);
 r.scalar={rva:hex(scalar),raw_hex:constant.toString('hex'),uint32:constant.readUInt32LE(),float32:constant.readFloatLE()};
 r.anchors=[];for(const a of anchors){const b=read(a.rva,a.bytes,'code-anchor');save(a.rva.toString(16)+'-anchor.bin',b);
  const matched=b.toString('hex')===a.expected;r.anchors.push({...a,rva:hex(a.rva),raw_hex:b.toString('hex'),matched});assert(matched,'Exact cached instruction opcode mismatch');}
 r.status='fixed-return-inputs-and-cache-anchors-verified';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);
  r.source_stat_stable=s.isFile()&&s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}}
  catch(e){r.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);r.source_closed=true;}catch(e){r.failures.push('Close: '+e);}}}
 r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,binding:r.binding,scalar:r.scalar,code_bytes:r.code_bytes,data_bytes:r.data_bytes,metadata_bytes:r.metadata_bytes,source_closed:r.source_closed,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
