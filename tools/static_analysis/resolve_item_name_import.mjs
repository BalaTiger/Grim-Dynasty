// Prepared only: fixed GetObjectName import/export metadata, no code reads.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const gamePath='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',enginePath='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const gamePin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',enginePin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const out=path.join(root,'docs/object-lifecycle/offline-item-name-import'),base=0x180000000n;
const symbol='?GetObjectName@Object@GAME@@QEBAPEBDXZ';
const sources=[
 ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
 ['docs/object-lifecycle/offline-item-identity-import/inspection.json','c0b80a81f7094c25816017d8e4adaeeff74c835f3049daabd0842b9563740b59'],
 ['docs/object-lifecycle/offline-item-identity-import/32396e-instructions.json','6d28b1412db5c9d04a261a5b480dfc6869fef32aca613cbeaf080616b8db0ba5']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync.native(c).toLowerCase(),c.toLowerCase());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(script,true);normal(path.dirname(out));absent(out);
const r={schema:'item-name-import-export-v1',status:'started',started_utc:new Date().toISOString(),sources:sources.map(([path,sha256])=>({path,sha256})),
 limits:{game_code_bytes:0,engine_code_bytes:0,metadata_bytes:22+2*symbol.length,read_operations:6,dumpbin_calls:0,hash_buffer:65536},
 original_loaded_or_executed:false,original_path_given_to_dumpbin:false,callees_followed:0,pdata_read:false,reads:[],metadata_bytes:0,failures:[],
 restrictions:['Only Game IAT609520 and fixed hint/name A47990, then that exact Engine hint name/ordinal/EAT are newly read.',
  'Existing 323974/323977 caller bytes, descriptor/OFT164/module name and PE/export headers are reused from hash-pinned reports.',
  'The OFT index164 is not an export hint. Hint is read from the fixed import name record and exact name equality is required in both modules.',
  'Hint mismatch stops without fallback scan. Export target is mapped for identity only, never read as code or .pdata.',
  'No Game caller reread, getter layout hypothesis, string algorithm, loaded IAT claim, subprocess, DLL loading, dumpbin or native execution.',
  'Each opened original uses read-only same-handle access and before/after64KiB full-file hashes, distinct from semantic reads.']};
let game,engine,created=false;
function save(){normal(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});}
function map(module,rva,n){assert(Number.isSafeInteger(rva)&&Number.isSafeInteger(n)&&n>0);const ss=module.pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);const offset=ss[0].raw_offset+rva-ss[0].rva;assert(offset>=0&&offset<=module.size-n);return {offset,section:ss[0]};}
function read(module,fd,rva,n,kind){
 const m=map(module,rva,n);assert(!(m.section.characteristics&0x20000000));assert(r.reads.length<r.limits.read_operations);assert(r.metadata_bytes+n<=r.limits.metadata_bytes);r.metadata_bytes+=n;
 const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,m.offset);r.reads.push({module:module.name,rva:hex(rva),file_offset:m.offset,kind,bytes:got,hex:b.subarray(0,got).toString('hex')});assert.equal(got,n);return b;
}
function examine(module,action){
 let fd,initial;
 try{normal(module.path,true);fd=fs.openSync(module.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,module.size);module.sha256_before=digest(fd,module.size);assert.equal(module.sha256_before,module.expected_sha256);return action(fd);}
 finally{if(fd!==undefined){
  try{if(initial&&initial.size===module.size){module.sha256_after=digest(fd,module.size);assert.equal(module.sha256_after,module.expected_sha256);const s=fs.fstatSync(fd);module.source_stat_stable=s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(module.source_stat_stable);}}catch(e){r.failures.push(module.name+' final: '+e);}
  finally{try{fs.closeSync(fd);module.source_closed=true;}catch(e){r.failures.push(module.name+' close: '+e);}}
  module.source_hash_preserved=module.sha256_before===module.expected_sha256&&module.sha256_after===module.expected_sha256;
 }}
}
try{
 fs.mkdirSync(out);created=true;normal(out);r.script_sha256=fingerprint(script);
 const proofs=sources.map(([rel,sha])=>{const p=path.join(root,rel);normal(p,true);assert(fs.statSync(p).size<524288);const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
 const prior=proofs[1];assert.equal(prior.status,'fixed-item-id-import-export-and-caller-copy-verified');assert(prior.source_hash_preserved&&prior.byte_coverage_verified);assert.equal(prior.failures.length,0);
 const caller=proofs[2].find(x=>x.address==='0x180323977'),thisMove=proofs[2].find(x=>x.address==='0x180323974');assert(caller&&thisMove);assert.equal(thisMove.bytes,'488bcf');assert.equal(caller.bytes,'ff15a35b2e00');
 const cb=Buffer.from(caller.bytes,'hex');assert.equal(BigInt(caller.address)+6n+BigInt(cb.readInt32LE(2)),base+0x609520n);assert.equal(prior.unfollowed_next_call.bytes,caller.bytes);r.cached_caller={source_index:2,this_move:thisMove,call:caller};
 const cached=proofs[0].normal_imports[3].raw_metadata,descriptor=Buffer.from(cached.descriptor.bytes,'hex'),lookup=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');
 assert.equal(proofs[0].status,'fixed-seven-questitem-imports-confirmed');assert.equal(proofs[0].original,gamePath);assert.equal(descriptor.length,20);assert.equal(descriptor.readUInt32LE(0),0xa42ac0);assert.equal(descriptor.readUInt32LE(12),0xa56aa6);assert.equal(descriptor.readUInt32LE(16),0x609000);
 assert.equal(cached.ownership_lookup_prefix.rva,'0xa42ac0');assert(cached.ownership_lookup_prefix.entries>164);assert.equal(lookup.subarray(164*8,165*8).toString('hex'),'9079a40000000000');assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(0x609000+164*8,0x609520);
 r.cached_binding={descriptor:cached.descriptor,lookup_index:164,lookup_raw:'9079a40000000000',dll:cached.dll_name_with_nul};
 function module(name,p,size,pin){const proof=prior.files.find(f=>f.name===name);assert(proof&&proof.source_hash_preserved&&proof.source_stat_stable&&proof.source_closed);assert.equal(proof.path,p);assert.equal(proof.expected_sha256,pin);assert.equal(proof.sha256_before,pin);assert.equal(proof.sha256_after,pin);assert.equal(proof.size,size);return {name,path:p,size,expected_sha256:pin,pe:proof.pe,source_closed:false};}
 game=module('Game',gamePath,11385856,gamePin);engine=module('Engine',enginePath,4485632,enginePin);r.files=[game,engine];let hint;
 examine(game,fd=>{
  const iat=read(game,fd,0x609520,8,'iat');assert.equal(iat.toString('hex'),'9079a40000000000');
  const name=read(game,fd,0xa47990,2+symbol.length+1,'hint-and-name');assert(name.subarray(2).equals(Buffer.from(symbol+'\0','ascii')));hint=name.readUInt16LE();r.import_binding={iat_rva:'0x609520',dll:'Engine.dll',symbol,hint,hint_name_rva:'0xa47990'};
 });
 assert(game.source_hash_preserved&&game.source_closed&&game.source_stat_stable);assert.equal(r.failures.length,0);
 examine(engine,fd=>{
  const pe=engine.pe,raw=Buffer.from(pe.export_directory_bytes,'hex');assert.equal(raw.length,40);assert.equal(pe.named_export_count,6283);assert.equal(pe.function_count,6283);assert.equal(raw.readUInt32LE(16),pe.ordinal_base);
  assert.equal(raw.readUInt32LE(20),pe.function_count);assert.equal(raw.readUInt32LE(24),pe.named_export_count);assert.equal(raw.readUInt32LE(28),Number(pe.address_table_rva));assert.equal(raw.readUInt32LE(32),Number(pe.name_pointer_table_rva));assert.equal(raw.readUInt32LE(36),Number(pe.name_ordinal_table_rva));assert(hint<pe.named_export_count);
  const nameRva=read(engine,fd,Number(pe.name_pointer_table_rva)+hint*4,4,'fixed-hint-name-pointer').readUInt32LE();const name=read(engine,fd,nameRva,symbol.length+1,'fixed-hint-name');assert(name.equals(Buffer.from(symbol+'\0','ascii')),'Hint mismatch; no fallback scan');
  const ordinalIndex=read(engine,fd,Number(pe.name_ordinal_table_rva)+hint*2,2,'fixed-hint-ordinal').readUInt16LE();assert(ordinalIndex<pe.function_count);const target=read(engine,fd,Number(pe.address_table_rva)+ordinalIndex*4,4,'fixed-export-address').readUInt32LE();
  r.export_binding={name:symbol,index:hint,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+pe.ordinal_base,target_rva:hex(target),target_va:hex(base+BigInt(target))};
  assert(!(target>=Number(pe.export_directory_rva)&&target<Number(pe.export_directory_rva)+pe.export_directory_size),'Forwarder outside scope');const m=map(engine,target,1);assert(m.section.characteristics&0x20000000,'Target not executable-mapped');
  r.unread_getter_target={rva:hex(target),file_offset:m.offset,body_bytes_read:0,boundary_known:false};
 });
 assert(engine.source_hash_preserved&&engine.source_closed&&engine.source_stat_stable);assert.equal(r.failures.length,0);assert.equal(r.reads.length,6);assert.equal(r.metadata_bytes,r.limits.metadata_bytes);r.status='fixed-item-name-import-export-verified';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 r.source_hash_preserved=!!(game&&engine&&game.source_hash_preserved&&engine.source_hash_preserved);if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save();
}
console.log(JSON.stringify({status:r.status,import_binding:r.import_binding,export_binding:r.export_binding,game_code_bytes:0,engine_code_bytes:0,metadata_bytes:r.metadata_bytes,dumpbin_calls:0,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
