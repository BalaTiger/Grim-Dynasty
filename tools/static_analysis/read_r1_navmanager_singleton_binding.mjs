// Prepared, not executed: exact Game IAT609C10 -> Engine Singleton<NavManager>::Get.
// Reads metadata and fingerprints only. No DLL loading, code-byte read or backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

const self=fileURLToPath(import.meta.url);
const root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(process.argv.length,2,'No arguments or expanded targets accepted');
const hash=b=>createHash('sha256').update(b).digest('hex');
const hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink(),'Reparse/alias path rejected');assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
normal(root);normal(self);
const proofs=[
 ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
 ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json','227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'],
 ['docs/object-lifecycle/offline-class-registration-exports/discovery.json','0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
 ['docs/reverse-client/r1-path-local-dispatch/05-PathLocal-0x18008e200-procedure_assembly.json','cfb7a8d5861915ce233ebf6e0075fd25940b5b005e01d7b3a3f6f973e3453530'],
 ['docs/phase-1/boundary-audit/module-boundaries.json','d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'],
 ['docs/reverse-client/r1-navigation-binding/inspection.json','d0c171ce77a6d67e153a6911fc7c3a17ea4d61e80ab9492f3287384524e14264']
];
const data=proofs.map(([p,h])=>{const f=path.join(root,p);normal(f);assert(fs.statSync(f).size<1048576);const bytes=fs.readFileSync(f);assert.equal(hash(bytes),h);return JSON.parse(bytes);});
const wantedSymbol='?Get@?$Singleton@VNavManager@GAME@@@GAME@@SAPEAVNavManager@2@XZ';
const wantedName=Buffer.from(wantedSymbol+'\0','ascii');
const gameHash='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const engineHash='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
assert(data[3].result.structuredContent.normalized_result.includes('0x18008e28f: CALL qword ptr [0x180609c10]'));
const catalogGame=data[4].find(m=>m.relativePath==='x64/Game.dll');
assert.equal(catalogGame.sha256,gameHash);
assert.equal(catalogGame.importModules.find(m=>m.module==='Engine.dll').symbols[386],wantedSymbol);
const out=path.join(root,'docs/reverse-client/r1-navmanager-singleton-binding');
normal(path.dirname(out));assert(!fs.existsSync(out),'Refuse to overwrite retained evidence');
const report={schema:'grim-dynasty-exact-navmanager-singleton-binding-v1',started_utc:new Date().toISOString(),proofs,script_sha256:hash(fs.readFileSync(self)),expected_symbol:wantedSymbol,metadata_limit:1024,metadata_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,original_open_mode:'O_RDONLY',whole_export_scan:false,files:[],failures:[]};

function inspect(source,expected,size,pe,body){
 normal(source);const file={source,expected_sha256:expected,reads:[]};report.files.push(file);let fd,initial;
 function digest(){const h=createHash('sha256'),buffer=Buffer.alloc(65536);for(let at=0;at<size;){const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,size-at),at);assert(n);h.update(buffer.subarray(0,n));at+=n;}return h.digest('hex');}
 function map(rva,n){assert(Number.isSafeInteger(rva)&&Number.isSafeInteger(n)&&rva>0&&n>0);const sections=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(sections.length,1);const section=sections[0],offset=section.raw_offset+rva-section.rva;assert(offset>=0&&offset+n<=size);return{section,offset};}
 function read(rva,n,kind){const {section,offset}=map(rva,n);assert(!(section.characteristics&0x20000000),'Code-byte reads forbidden');report.metadata_bytes+=n;assert(report.metadata_bytes<=report.metadata_limit);const bytes=Buffer.alloc(n);assert.equal(fs.readSync(fd,bytes,0,n,offset),n);file.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:bytes.toString('hex')});return bytes;}
 try{fd=fs.openSync(source,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.equal(initial.size,size);file.sha256_before=digest();assert.equal(file.sha256_before,expected);body(read,map);}
 finally{if(fd!==undefined){try{file.sha256_after=digest();assert.equal(file.sha256_after,expected);normal(source);for(const stat of[fs.fstatSync(fd),fs.statSync(source)])for(const k of['size','ino','dev','mtimeMs','ctimeMs'])assert.equal(stat[k],initial[k]);file.stable=true;}finally{fs.closeSync(fd);file.closed=true;}}}
}

try{
 const cache=data[0].normal_imports[4].raw_metadata;
 const descriptor=Buffer.from(cache.descriptor.bytes,'hex'),prefix=Buffer.from(cache.ownership_lookup_prefix.bytes,'hex');
 assert.equal(descriptor.length,20);assert.equal(descriptor.readUInt32LE(0),0xa42ac0);assert.equal(descriptor.readUInt32LE(12),0xa56aa6);assert.equal(descriptor.readUInt32LE(16),0x609000);
 assert.equal(cache.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(Number(cache.ownership_lookup_prefix.rva),0xa42ac0);assert.equal(cache.ownership_lookup_prefix.entries,350);assert.equal(prefix.length,350*8);
 for(let at=0;at<prefix.length;at+=8)assert.notEqual(prefix.readBigUInt64LE(at),0n);
 assert.equal(data[5].status,'passed');
 const prior=data[5].files.find(f=>f.source==='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll');
 assert.equal(prior.sha256_before,gameHash);assert.equal(prior.sha256_after,gameHash);assert(prior.stable&&prior.closed);
 const tail=prior.reads.find(r=>r.kind==='OFT-nonzero-continuation-through-selected-slot');
 assert.equal(Number(tail.rva),0xa435b0);assert.equal(tail.bytes,1104);
 const tailBytes=Buffer.from(tail.raw_hex,'hex');assert.equal(tailBytes.length,1104);
 for(let at=0;at<tailBytes.length;at+=8)assert.notEqual(tailBytes.readBigUInt64LE(at),0n);
 assert.equal(tailBytes.readBigUInt64LE((386-350)*8),0xa4a86an);
 report.cached_ownership={descriptor_source:proofs[0][0],continuation_source:proofs[5][0],descriptor_index:4,nonzero_lookup_entries:488,selected_lookup_index:386,cached_lookup_rva:'0xa436d0',cached_hint_name_rva:'0xa4a86a'};
 assert.equal(data[1].sha256_before,gameHash);assert.equal(data[1].sha256_after,gameHash);assert.equal(data[1].pe.image_base,'0x180000000');
 inspect('D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',gameHash,11385856,data[1].pe,read=>{
  const lookup=read(0xa436d0,8,'exact-OFT-386').readBigUInt64LE();
  const iat=read(0x609c10,8,'exact-IAT-slot').readBigUInt64LE();
  assert.equal(lookup,0xa4a86an);assert.equal(iat,lookup);
  const hint=read(0xa4a86a,2,'exact-import-hint').readUInt16LE();
  assert(read(0xa4a86c,wantedName.length,'exact-import-name').equals(wantedName),'Selected import is not the observed singleton symbol');
  report.binding={dll:'Engine.dll',iat_rva:'0x609c10',lookup_index:386,lookup_rva:'0xa436d0',hint_name_rva:'0xa4a86a',hint,symbol:wantedSymbol};
 });
 const engineProof=data[2].files.find(f=>f.target==='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll');
 assert(engineProof.source_hash_preserved&&engineProof.source_stat_stable);assert.equal(engineProof.sha256_before,engineHash);assert.equal(engineProof.sha256_after,engineHash);
 const pe=engineProof.pe,raw=Buffer.from(pe.export_directory_bytes,'hex');assert.equal(raw.length,40);
 for(const[offset,value]of[[16,pe.ordinal_base],[20,pe.function_count],[24,pe.named_export_count],[28,Number(pe.address_table_rva)],[32,Number(pe.name_pointer_table_rva)],[36,Number(pe.name_ordinal_table_rva)]])assert.equal(raw.readUInt32LE(offset),value);
 assert.equal(pe.named_export_count,6283);assert.equal(pe.function_count,6283);
 inspect(engineProof.target,engineHash,4485632,pe,(read,map)=>{
  const hint=report.binding.hint;assert(hint<pe.named_export_count);
  const nameRva=read(Number(pe.name_pointer_table_rva)+hint*4,4,'exact-hint-name-pointer').readUInt32LE();
  assert(nameRva>=Number(pe.export_directory_rva)&&nameRva+wantedName.length<=Number(pe.export_directory_rva)+pe.export_directory_size,'Export name outside proven export range');
  assert(read(nameRva,wantedName.length,'exact-export-name').equals(wantedName),'Hint name mismatch: stop; no fallback or wider scan');
  const ordinalIndex=read(Number(pe.name_ordinal_table_rva)+hint*2,2,'exact-name-ordinal').readUInt16LE();assert(ordinalIndex<pe.function_count);
  const target=read(Number(pe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();
  assert(!(target>=Number(pe.export_directory_rva)&&target<Number(pe.export_directory_rva)+pe.export_directory_size),'Forwarder unsupported');
  assert(map(target,1).section.characteristics&0x20000000,'Target must map to executable section; no code read');
  report.export={symbol:wantedSymbol,hint,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+pe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target)};
 });
 report.limitations=['On-disk import/export identity only; no getter body, lazy initialization or writer semantics inferred.','No live IAT/global objects, code, backend, game, player saves or cloud data accessed.','Fingerprint passes are identity checks, not atomic snapshots; future execution still requires normal approval.'];
}catch(error){report.failures.push(String(error));}
report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';
fs.mkdirSync(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({status:report.status,binding:report.binding,export:report.export,metadata_bytes:report.metadata_bytes,files:report.files.map(({reads,...rest})=>rest),failures:report.failures}));
if(report.failures.length)process.exitCode=1;
