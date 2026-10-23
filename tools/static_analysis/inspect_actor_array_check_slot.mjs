// Prepared only: one file-image slot and the minimum load-config check-field evidence.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632;
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const out=path.join(root,'docs/object-lifecycle/offline-actor-array-check-slot');
const sources=[
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-actor-array-check-entry/inspection.json','8ea04fc933acee8b98876f0e99b28d88c3936ff3dde7bff5874d662de051126f']
];
const slotRva=0x302d38,base=0x180000000n,hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink(),'Redirected path refused');assert(first&&file?s.isFile():s.isDirectory());const d=path.dirname(c);if(d===c)return;c=d;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output refused');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.ino===b.ino&&a.dev===b.dev&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
function fingerprint(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');let at=0;while(at<n){const k=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(k>0);h.update(b.subarray(0,k));at+=k;}return {sha256:h.digest('hex'),bytes:at,chunk_bytes:65536};}
plain(path.dirname(out));absent(out);
const proofs=sources.map(([p,h])=>{const full=path.join(root,p);plain(full,true);assert(fs.statSync(full).size<=131072);const b=fs.readFileSync(full);assert(b.length<=131072);assert.equal(hash(b),h);return JSON.parse(b.toString('utf8'));});
for(const p of proofs){assert.equal(p.original,original);assert.equal(p.expected_sha256,expected);assert.equal(p.sha256_before,expected);assert.equal(p.sha256_after,expected);assert(p.source_hash_preserved&&p.source_stat_stable);assert.deepEqual(p.failures,[]);}
assert.equal(proofs[0].status,'exact-actor-export-and-boundary-verified');assert.equal(proofs[1].status,'fixed-eight-byte-interval-verified');
assert.equal(proofs[1].raw_hex,'48ff2571580200cc');assert(proofs[1].byte_coverage_verified);assert.equal(proofs[1].indirect_jumps.length,1);
assert.equal(proofs[1].indirect_jumps[0].slot_rva,hex(slotRva));assert.equal(proofs[1].indirect_jumps[0].slot_read,false);
const pe=proofs[0].pe;assert.equal(pe.image_base,hex(base));
const report={schema:'grim-dynasty-actor-array-check-file-slot-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,expected_file_bytes:size,
 source_evidence:sources.map(([path,sha256])=>({path,sha256})),slot_rva:hex(slotRva),slot_preferred_va:hex(base+BigInt(slotRva)),
 format_reference:'https://learn.microsoft.com/en-us/windows/win32/debug/pe-format',load_config_pe32plus_check_field_offset:112,load_config_pe32plus_guard_flags_offset:144,
 limits:{slot_bytes:8,metadata_bytes:72,code_bytes:0,fingerprint_chunk_bytes:65536},slot_bytes:0,metadata_bytes:0,code_bytes:0,reads:[],fingerprint_passes:[],
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,subprocesses:0,build_or_test_execution:false,permission_change:false,
 original_modified:false,full_target_copy_created:false,runtime_slot_value_observed:false,loader_rebinding_observed:false,
 limitations:['Only the fixed eight-byte slot plus PE header, load-config directory, Size, GuardCFCheckFunctionPointer and GuardFlags fields are read.',
  'No target code, import descriptor/name/IAT contents, other slot, table, relocation entries, EH or .pdata are read.',
  'Slot contents are the on-disk image initializer. A matching load-config field designates the slot, but does not establish the loaded slot value or actual loader rebinding.',
  'A file-image target RVA is arithmetic metadata, not a no-op, CFG enforcement, valid-call or runtime behavior claim.',
  'If the check field is absent or does not name this slot, this bounded reader stops unresolved rather than widening its search.',
  'Fingerprints are separate from semantic reads; same-handle before/after hash/stat stability is not an atomic snapshot.']};
let fd,stat,outCreated=false;const failures=[],save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);return {section:ss[0],offset:ss[0].raw_offset+rva-ss[0].rva};}
function read(at,n,kind,field){assert(Number.isSafeInteger(at)&&at>=0&&Number.isSafeInteger(n)&&n>0&&at+n<=size);assert(kind==='slot'||kind==='metadata');
 const counter=kind+'_bytes';assert(report[counter]+n<=report.limits[counter]);const r={kind,field,file_offset:at,requested_bytes:n,read_bytes:0},b=Buffer.alloc(n);report.reads.push(r);
 while(r.read_bytes<n){const k=fs.readSync(fd,b,r.read_bytes,n-r.read_bytes,at+r.read_bytes);assert(k>0);r.read_bytes+=k;report[counter]+=k;}r.raw_hex=b.toString('hex');return b;}
try{
 fs.mkdirSync(out);outCreated=true;plain(out);plain(script,true);report.script_sha256=hash(fs.readFileSync(script));
 plain(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);assert(stat.isFile()&&stat.size===size);
 const before=fingerprint(fd,size);report.fingerprint_passes.push({label:'before',...before});report.sha256_before=before.sha256;assert.equal(before.sha256,expected);
 const slot=map(slotRva,8);assert.equal(slot.section.name,'.rdata');const bytes=read(slot.offset,8,'slot','fixed slot file initializer'),value=bytes.readBigUInt64LE(0);
 report.slot={file_offset:slot.offset,section:slot.section.name,raw_hex:bytes.toString('hex'),file_value_va:hex(value),target_code_read:false};save('302d38-slot.bin',bytes);
 if(value>=base&&value-base<BigInt(pe.image_size)){const rva=Number(value-base);report.slot.file_value_rva=hex(rva);report.slot.file_value_sections=pe.sections.filter(s=>rva>=s.rva&&rva<s.rva+Math.max(s.virtual_size,s.raw_size)).map(s=>s.name);}
 assert.equal(read(0,2,'metadata','DOS signature').toString('hex'),'4d5a');const nt=read(0x3c,4,'metadata','e_lfanew').readUInt32LE(0);assert(nt>=64&&nt+24+200<=4096);
 const coff=read(nt,24,'metadata','PE signature and COFF header');assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const optional=nt+24,optionalBytes=coff.readUInt16LE(20);assert(optionalBytes>=200&&optional+optionalBytes<=4096);
 assert.equal(read(optional,2,'metadata','PE32+ magic').readUInt16LE(0),0x20b);
 assert.equal(read(optional+24,8,'metadata','ImageBase').readBigUInt64LE(0),base);
 assert.equal(read(optional+56,4,'metadata','SizeOfImage').readUInt32LE(0),pe.image_size);
 const count=read(optional+108,4,'metadata','NumberOfRvaAndSizes').readUInt32LE(0);assert(count>10&&count<=16);
 const directory=read(optional+112+10*8,8,'metadata','load-config data-directory'),rva=directory.readUInt32LE(0),length=directory.readUInt32LE(4);
 report.load_config={directory_entry_file_offset:optional+112+10*8,rva:hex(rva),directory_bytes:length,check_slot_match:false};
 if(!rva||length<4)report.status='load-config-absent-slot-unresolved';
 else{const start=map(rva,4),declared=read(start.offset,4,'metadata','load-config Size').readUInt32LE(0);report.load_config.declared_bytes=declared;
  if(length<120||declared<120)report.status='load-config-check-field-absent-slot-unresolved';
  else{const field=map(rva+112,8),pointer=read(field.offset,8,'metadata','GuardCFCheckFunctionPointer').readBigUInt64LE(0);
   report.load_config.check_field={relative_offset:112,file_offset:field.offset,declared_slot_va:hex(pointer)};
   report.load_config.check_slot_match=pointer===base+BigInt(slotRva);
   if(length>=148&&declared>=148){const flags=map(rva+144,4);report.load_config.guard_flags=hex(read(flags.offset,4,'metadata','GuardFlags').readUInt32LE(0));}
   report.status=report.load_config.check_slot_match?'file-slot-matches-load-config-check-field':'load-config-check-field-does-not-match-slot';
  }
 }
}catch(error){failures.push(String(error));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){const after=fingerprint(fd,size);report.fingerprint_passes.push({label:'after',...after});report.sha256_after=after.sha256;assert.equal(after.sha256,expected);report.source_stat_stable=stable(stat,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){failures.push('Original final verification: '+String(e));}finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(e){failures.push('Original close: '+String(e));}}}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();
 report.controller_resource_usage=process.resourceUsage();if(outCreated){plain(out);save('metadata.json',JSON.stringify(report,null,2)+'\n');}
}
console.log(JSON.stringify({status:report.status,slot:report.slot,load_config:report.load_config,slot_bytes:report.slot_bytes,metadata_bytes:report.metadata_bytes,code_bytes:0,original_unchanged:report.source_hash_preserved,failures}));
if(failures.length)process.exitCode=1;
