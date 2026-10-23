// Fixed caller-backed archive dependencies: PE exception metadata only, no code read/disassembly.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const copy=path.join(root,'.tooling/offline-cache-node/Engine.readonly.dll');
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const out=path.join(root,'docs/object-lifecycle/offline-archive-init-boundaries');
const proofs=[
 ['docs/phase-1/round-4/followup/Engine-function-evidence-bounded-v2.json','cbae1984329cb2a490d5ea9224543535776ae518fc6f3fff89c7c97108e573bb'],
 ['docs/resource-contracts/followup/Engine-function-evidence-bounded-v2.json','3fb2d8de177305c978a6e64d8d2c26130919a4b26593d3c3145bae47642011de'],
 ['docs/resource-contracts/database-ownership/Engine-function-evidence-bounded-v2.json','6779a5d15657b3058907af317d5e6869e4fa0509b8ec6670dbddd7e27627f0c5']
];
const targets=[
 {rva:0x27f9c0,exact:true,role:'archive-constructor',caller:'073ABF',proof:0},
 {rva:0x2800d6,exact:false,role:'archive-load-missing-continuation',caller:'2800D3 last saved instruction',proof:1},
 {rva:0x280143,exact:false,role:'archive-load-known-unread-jump-target',caller:'2800D1 JZ',proof:1},
 {rva:0x27ff20,exact:true,role:'archive-region-acquisition',caller:'280191',proof:2},
 {rva:0x02d950,exact:true,role:'offset-vector-append',caller:'2801E7 / 280210',proof:2}
];
function hashFile(p){
 const h=createHash('sha256'),buffer=Buffer.alloc(65536),fd=fs.openSync(p,'r');
 try{let n;while((n=fs.readSync(fd,buffer,0,buffer.length,null))>0)h.update(buffer.subarray(0,n));}
 finally{fs.closeSync(fd);}return h.digest('hex');
}
assert(!fs.existsSync(out));
for(const [p,h] of proofs)assert.equal(hashFile(path.join(root,p)),h);
assert.equal(hashFile(copy),expected);
fs.mkdirSync(out,{recursive:true});
const report={status:'started',started_utc:new Date().toISOString(),original,working_copy:copy,
 source_evidence:proofs.map(([p,h])=>({path:p,sha256:h})),targets:[],
 code_bytes_read:0,dumpbin_started:false,target_loaded_or_executed:false,backend_started:false,
 metadata_byte_limit:32768,hash_chunk_bytes:65536,
 boundary_scope:'Unwind entries delimit fragments, not proof of complete logical functions'};
let fd,failure,metadataBytes=0;
try{
 report.original_sha256_before=hashFile(original);assert.equal(report.original_sha256_before,expected);
 fd=fs.openSync(original,'r');const size=fs.fstatSync(fd).size;assert(size>0&&size<=64*1024*1024);
 function read(offset,length){
  assert(Number.isSafeInteger(offset)&&offset>=0&&Number.isInteger(length)&&length>0&&length<=4096&&offset+length<=size);
  assert(metadataBytes+length<=32768);const b=Buffer.alloc(length);
  assert.equal(fs.readSync(fd,b,0,length,offset),length);metadataBytes+=length;return b;
 }
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
 const pe=dos.readUInt32LE(60);assert(pe<=65536);
 const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const n=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);assert(n>0&&n<=96&&optSize>=144&&optSize<=4096);
 const optional=read(pe+24,optSize);assert.equal(optional.readUInt16LE(0),0x20b);
 assert.equal(optional.readBigUInt64LE(24),0x180000000n);assert(optional.readUInt32LE(108)>3);
 const sections=Array.from({length:n},(_,i)=>{const b=read(pe+24+optSize+i*40,40);return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20)};});
 function map(rva,length){const found=sections.filter(s=>rva>=s.rva&&rva+length<=s.rva+s.size);assert.equal(found.length,1);return found[0].raw+rva-found[0].rva;}
 const pdata=optional.readUInt32LE(136),pdataSize=optional.readUInt32LE(140);
 assert(pdataSize>0&&pdataSize%12===0&&pdataSize<=2*1024*1024);
 const pdataOffset=map(pdata,pdataSize);
 for(const target of targets){
  let lo=0,hi=pdataSize/12-1,boundary=null,reads=0;
  while(lo<=hi){
   const index=(lo+hi)>>1,b=read(pdataOffset+12*index,12);reads++;
   const begin=b.readUInt32LE(0),end=b.readUInt32LE(4);assert(begin<end);
   if(target.rva<begin)hi=index-1;else if(target.rva>=end)lo=index+1;
   else{boundary={begin_rva:begin,end_rva:end,unwind_rva:b.readUInt32LE(8),index,bytes:end-begin};break;}
  }
  report.targets.push({...target,rva_hex:'0x'+target.rva.toString(16),boundary,index_reads:reads});
  assert(boundary&&(!target.exact||boundary.begin_rva===target.rva),'Missing required unwind entry');
 }
 report.status='fixed-archive-init-boundaries-assessed';
}catch(error){failure=error;report.status='stopped';report.failure=String(error);}
finally{
 if(fd!==undefined)fs.closeSync(fd);
 report.metadata_bytes=metadataBytes;
 report.original_sha256_after=hashFile(original);report.copy_sha256_after=hashFile(copy);
 report.original_hash_matches=report.original_sha256_before===expected&&report.original_sha256_after===expected;
 report.copy_hash_matches=report.copy_sha256_after===expected;
 report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
 fs.writeFileSync(path.join(out,'boundaries.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});
}
assert(report.original_hash_matches&&report.copy_hash_matches);if(failure)throw failure;
console.log(JSON.stringify({status:report.status,metadata_bytes:metadataBytes,targets:report.targets}));
