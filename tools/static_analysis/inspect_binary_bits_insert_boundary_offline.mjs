// Fixed PE metadata-only boundary assessment for the archived 0C6A89 -> 047D30 call.
// No code decoding, disassembler, backend or target loading/execution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const copy=path.join(root,'.tooling/offline-cache-node/Engine.readonly.dll');
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const caller='docs/object-lifecycle/rea-binary-parser-helpers/07-Parser-helper-0x1800c69f0-assembly.json';
const callerHash='5f977a7cf394c5426a7ef3ec54d0be7d8f646313f4aec2c1a464d54bd32cb6da';
const out=path.join(root,'docs/object-lifecycle/offline-binary-bits-insert-boundary');
function hashFile(p){
 const fd=fs.openSync(p,'r'),buf=Buffer.alloc(65536),h=createHash('sha256');
 try{let n;while((n=fs.readSync(fd,buf,0,buf.length,null))>0)h.update(buf.subarray(0,n));}
 finally{fs.closeSync(fd);}return h.digest('hex');
}
assert(!fs.existsSync(out),'Preserve earlier evidence');
assert.equal(hashFile(path.join(root,caller)),callerHash);
const copyBefore=hashFile(copy);assert.equal(copyBefore,expected);
fs.mkdirSync(out,{recursive:true});
const record={started_utc:new Date().toISOString(),status:'started',original,working_copy:copy,
 requested_va:'0x180047d30',source_evidence:{path:caller,sha256:callerHash,
 evidence_id:'ev_c3ceaad56025f087eb678a5f57c4d049a103c641b6bf21ad561e95f6f71817cd',
 instruction:'0x1800c6a89: CALL 0x180047d30'},
 copy_sha256_before:copyBefore,code_bytes_read:0,dumpbin_started:false,backend_started:false,
 target_loaded_or_executed:false,
 hash_scope:'64 KiB streaming full-file SHA256; only PE metadata decoded; no code range read'};
let fd,failure;
try{
 record.original_sha256_before=hashFile(original);assert.equal(record.original_sha256_before,expected);
 fd=fs.openSync(original,'r');const size=fs.fstatSync(fd).size;assert(size>0&&size<=64*1024*1024);
 let metadataBytes=0;
 function read(offset,length){
  assert(Number.isInteger(offset)&&Number.isInteger(length)&&offset>=0&&length>0&&offset+length<=size&&length<=4096);
  const b=Buffer.alloc(length);assert.equal(fs.readSync(fd,b,0,length,offset),length);metadataBytes+=length;return b;
 }
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
 const pe=dos.readUInt32LE(60);assert(pe<=65536);
 const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const n=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);assert(n>0&&n<=96&&optSize>=144&&optSize<=4096);
 const optional=read(pe+24,optSize);assert.equal(optional.readUInt16LE(0),0x20b);
 assert.equal(optional.readBigUInt64LE(24),0x180000000n);
 const sections=Array.from({length:n},(_,i)=>{const b=read(pe+24+optSize+i*40,40);return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20)};});
 function map(rva,length){
  const matches=sections.filter(s=>rva>=s.rva&&rva+length<=s.rva+s.size);assert.equal(matches.length,1);
  return matches[0].raw+rva-matches[0].rva;
 }
 assert(optional.readUInt32LE(108)>3);
 const pdata=optional.readUInt32LE(136),pdataSize=optional.readUInt32LE(140);
 assert(pdataSize>0&&pdataSize%12===0&&pdataSize<=2*1024*1024);
 const start=map(pdata,pdataSize),rva=0x47d30;
 let lo=0,hi=pdataSize/12-1,boundary=null,indexReads=0;
 while(lo<=hi){
  const index=(lo+hi)>>1,b=read(start+12*index,12);indexReads++;
  const begin=b.readUInt32LE(0),end=b.readUInt32LE(4);assert(begin<end);
  if(rva<begin)hi=index-1;else if(rva>=end)lo=index+1;
  else{boundary={begin_rva:begin,end_rva:end,unwind_rva:b.readUInt32LE(8),index,bytes:end-begin};break;}
 }
 assert(boundary&&boundary.begin_rva===rva,'No exact runtime-function entry');
 record.boundary=boundary;record.exception_index_reads=indexReads;record.metadata_bytes=metadataBytes;
 record.end_va_exclusive='0x'+(0x180000000n+BigInt(boundary.end_rva)).toString(16);
 record.within_followup_code_budget=boundary.bytes<=4096;
 record.logical_function_complete_not_assumed=true;record.status='metadata-boundary-assessed';
}catch(e){failure=e;record.status='failed';record.failure=String(e);}
finally{
 if(fd!==undefined)fs.closeSync(fd);
 record.original_sha256_after=hashFile(original);record.copy_sha256_after=hashFile(copy);
 record.original_hash_matches=record.original_sha256_after===expected;record.copy_hash_matches=record.copy_sha256_after===expected;
 record.finished_utc=new Date().toISOString();record.controller_resource_usage=process.resourceUsage();
 fs.writeFileSync(path.join(out,'boundary.json'),JSON.stringify(record,null,2)+'\n',{flag:'wx'});
}
assert(record.original_hash_matches&&record.copy_hash_matches);if(failure)throw failure;
console.log(JSON.stringify({status:record.status,boundary:record.boundary,end:record.end_va_exclusive,
 within_followup_code_budget:record.within_followup_code_budget,metadata_bytes:record.metadata_bytes,
 original_unchanged:true,copy_unchanged:true,controller_peak_rss_kib:record.controller_resource_usage.maxRSS}));
