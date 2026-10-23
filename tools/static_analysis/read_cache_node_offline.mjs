// Fixed, read-only PE extraction for two already-known functions and their caller.
// No process spawning, target loading, installation, symbol server or backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const target='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const out=path.join(root,'docs/object-lifecycle/offline-cache-node-inspection');
const copyDir=path.join(root,'.tooling/offline-cache-node');
const copyPath=path.join(copyDir,'Engine.readonly.dll');
const hash=b=>createHash('sha256').update(b).digest('hex');
const readJson=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const hex=n=>'0x'+n.toString(16);
assert.ok(!fs.existsSync(out)&&!fs.existsSync(copyDir),'Preserve prior evidence');
const priorDir=path.join(root,'.tooling/checkpoints/monitor-diagnostic-complete');
const prior=readJson(path.join(priorDir,'manifest.json'));
assert.equal(prior.total_files,345);
for(const e of prior.entries){
 assert.equal(hash(fs.readFileSync(path.join(priorDir,e.path))),e.sha256,'Prior copy changed: '+e.path);
 assert.equal(hash(fs.readFileSync(path.join(root,e.path))),e.sha256,'Current baseline changed: '+e.path);
}
const evidence=[
 ['docs/object-lifecycle/rea-record-cache-publication/05-Cache-publication-0x18005deb0-assembly.json','a4fe68eb62328f6715c70fe810645e60d6045720d8ad94ae11a1379260d917c9'],
 ['docs/object-lifecycle/rea-record-cache-publication/06-Cache-publication-0x18005deb0-direct-callees.json','f2f6f385e085bbe548a16c2d3c4141a26ffe717e12f67b87bfb3dea2f89e5aa7']
];
for(const [p,h]of evidence)assert.equal(hash(fs.readFileSync(path.join(root,p))),h,'Address evidence changed');
fs.mkdirSync(out,{recursive:true});fs.mkdirSync(copyDir,{recursive:true});
const started=new Date().toISOString(),fd=fs.openSync(target,'r');
let record;
try {
 const size=fs.fstatSync(fd).size;
 assert.ok(size>0&&size<=64*1024*1024,'File-size budget exceeded');
 function read(offset,length){
  assert.ok(Number.isInteger(offset)&&Number.isInteger(length)&&offset>=0&&length>0&&offset+length<=size);
  const b=Buffer.alloc(length);assert.equal(fs.readSync(fd,b,0,length,offset),length);return b;
 }
 function hashSource(){
  const h=createHash('sha256'),chunk=Buffer.alloc(65536);
  for(let offset=0;offset<size;){const n=fs.readSync(fd,chunk,0,Math.min(chunk.length,size-offset),offset);assert.ok(n>0);h.update(chunk.subarray(0,n));offset+=n;}
  return h.digest('hex');
 }
 const before=hashSource();assert.equal(before,expected,'Original fingerprint changed');
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
 const pe=dos.readUInt32LE(60);assert.ok(pe<=65536);
 const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const sectionCount=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);
 assert.ok(sectionCount>0&&sectionCount<=96&&optSize>=144&&optSize<=4096);
 const optional=read(pe+24,optSize);assert.equal(optional.readUInt16LE(0),0x20b);
 const base=optional.readBigUInt64LE(24);assert.equal(base,0x180000000n);
 const sections=Array.from({length:sectionCount},(_,i)=>{
  const b=read(pe+24+optSize+i*40,40);
  return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),raw_size:b.readUInt32LE(16),raw_offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};
 });
 function map(rva,length,executable=false){
  const found=sections.filter(s=>rva>=s.rva&&rva+length<=s.rva+s.raw_size);
  assert.equal(found.length,1,'Unmapped or ambiguous RVA');const s=found[0];
  if(executable)assert.ok((s.flags&0x20000000)!==0,'Not executable code section');
  return {file_offset:s.raw_offset+rva-s.rva,section:s.name};
 }
 assert.ok(optional.readUInt32LE(108)>3,'Missing exception directory');
 const pdataRva=optional.readUInt32LE(112+3*8),pdataSize=optional.readUInt32LE(116+3*8);
 assert.ok(pdataSize>0&&pdataSize%12===0&&pdataSize<=2*1024*1024,'Unexpected exception index');
 const pdataOffset=map(pdataRva,pdataSize).file_offset;
 function findFunction(rva){
  let lo=0,hi=pdataSize/12-1,reads=0;
  while(lo<=hi){
   const index=(lo+hi)>>1,b=read(pdataOffset+index*12,12);reads++;
   const begin=b.readUInt32LE(0),end=b.readUInt32LE(4),unwind=b.readUInt32LE(8);
   assert.ok(begin<end,'Invalid exception index entry');
   if(rva<begin)hi=index-1;
   else if(rva>=end)lo=index+1;
   else return {exact_entry:begin===rva,begin_rva:begin,end_rva:end,unwind_rva:unwind,index,index_reads:reads};
  }
  return {exact_entry:false,index_reads:reads};
 }
 const functions=[];
 for(const rva of [0x1f170,0x22860]){
  const boundary=findFunction(rva);
  if(!boundary.exact_entry){functions.push({requested_va:hex(base+BigInt(rva)),boundary,status:'stopped-no-exact-runtime-function-entry'});continue;}
  const length=boundary.end_rva-rva;
  assert.ok(length>0&&length<=4096,'Function exceeds fixed code budget');
  const mapping=map(rva,length,true),bytes=read(mapping.file_offset,length);
  const rawName=hex(base+BigInt(rva))+'.bin';
  fs.writeFileSync(path.join(out,rawName),bytes,{flag:'wx'});
  functions.push({requested_va:hex(base+BigInt(rva)),end_va_exclusive:hex(base+BigInt(boundary.end_rva)),boundary,mapping,length,raw_file:rawName,sha256:hash(bytes),hex_bytes:bytes.toString('hex'),status:'exact-pdata-body-extracted'});
 }
 const callerRva=0x5deb0,callerLength=0x5df0c-callerRva,callerMapping=map(callerRva,callerLength,true),callerBytes=read(callerMapping.file_offset,callerLength);
 fs.writeFileSync(path.join(out,'0x18005deb0-caller.bin'),callerBytes,{flag:'wx'});
 const caller={start_va:'0x18005deb0',end_va_exclusive:'0x18005df0c',length:callerLength,mapping:callerMapping,sha256:hash(callerBytes),hex_bytes:callerBytes.toString('hex'),scope:'Previously archived complete 05DEB0 caller only'};
 const copyFd=fs.openSync(copyPath,'wx');let copyHash;
 try {
  const h=createHash('sha256'),chunk=Buffer.alloc(65536);
  for(let offset=0;offset<size;){
   const n=fs.readSync(fd,chunk,0,Math.min(chunk.length,size-offset),offset);assert.ok(n>0);
   assert.equal(fs.writeSync(copyFd,chunk,0,n,offset),n);h.update(chunk.subarray(0,n));offset+=n;
  }
  fs.fsyncSync(copyFd);copyHash=h.digest('hex');
 }finally{fs.closeSync(copyFd);}
 assert.equal(copyHash,expected);fs.chmodSync(copyPath,0o444);
 const after=hashSource();assert.equal(after,expected,'Original changed during extraction');
 record={
  status:functions.every(x=>x.status==='exact-pdata-body-extracted')?'extracted-fixed-functions':'partial-boundaries',
  started,finished:new Date().toISOString(),target,original_sha256_before:before,original_sha256_after:after,
  readonly_copy:copyPath,copy_sha256:copyHash,file_bytes:size,hash_chunk_bytes:65536,
  image_base:hex(base),exception_index:{rva:hex(pdataRva),bytes:pdataSize,entry_bytes:12,access:'bounded binary lookup; unrelated code not read'},
  source_evidence:evidence.map(([file,sha256])=>({file,sha256})),prior_checkpoint_verified:345,
  functions,caller,target_loaded_or_executed:false,backend_launches:0,disassembler_launches:0,
  resource_usage:process.resourceUsage(),
  limitations:['PE runtime-function records supply code ranges but do not establish semantics or exception behavior.','Only two target bodies and the archived 05DEB0 caller are extracted; no callees are followed.','Full-file hashing and a read-only local copy are included; the complete DLL copy must not be included in checkpoints.']
 };
}finally{fs.closeSync(fd);}
fs.writeFileSync(path.join(out,'extraction.json'),JSON.stringify(record,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({status:record.status,functions:record.functions.map(x=>({va:x.requested_va,end:x.end_va_exclusive,bytes:x.length,status:x.status})),copy:copyPath,original_sha256:record.original_sha256_after,peak_rss_kib:record.resource_usage.maxRSS}));
