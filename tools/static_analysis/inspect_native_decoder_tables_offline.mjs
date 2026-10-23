// Two exact 16-byte readonly data references observed in the decoder entry.
// No additional functions, broad data scan, backend or target execution.
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
const proofPath='docs/object-lifecycle/offline-native-decoder-entry/278d20-instructions.json';
const proof=fs.readFileSync(path.join(root,proofPath));
const rows=JSON.parse(proof);
assert(rows.some(r=>r.address==='0x180278d39'&&r.instruction.includes('000000018034C470h')));
assert(rows.some(r=>r.address==='0x180278d44'&&r.instruction.includes('000000018034C000h')));
const output=path.join(root,'docs/object-lifecycle/offline-native-decoder-boundary/copy-tables.json');
assert(!fs.existsSync(output));
const hash=b=>createHash('sha256').update(b).digest('hex');
function hashFile(p){const fd=fs.openSync(p,'r'),buf=Buffer.alloc(65536),h=createHash('sha256');
 try{let n;while((n=fs.readSync(fd,buf,0,buf.length,null))>0)h.update(buf.subarray(0,n));}
 finally{fs.closeSync(fd);}return h.digest('hex');}
assert.equal(hashFile(copy),expected);
const result={started_utc:new Date().toISOString(),status:'started',original,source_evidence:{path:proofPath,sha256:hash(proof)},
 code_bytes_read:0,fixed_data_bytes:32,target_loaded_or_executed:false,backend_started:false};
let fd,failure;
try{
 result.original_sha256_before=hashFile(original);assert.equal(result.original_sha256_before,expected);
 fd=fs.openSync(original,'r');const size=fs.fstatSync(fd).size;assert(size>0&&size<=64*1024*1024);
 let metadataBytes=0;
 function read(offset,length,metadata=true){
  assert(Number.isInteger(offset)&&Number.isInteger(length)&&offset>=0&&length>0&&length<=4096&&offset+length<=size);
  const b=Buffer.alloc(length);assert.equal(fs.readSync(fd,b,0,length,offset),length);
  if(metadata)metadataBytes+=length;return b;
 }
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
 const pe=dos.readUInt32LE(60);assert(pe<=65536);
 const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const n=coff.readUInt16LE(6),opt=coff.readUInt16LE(20);assert(n>0&&n<=96&&opt>=32&&opt<=4096);
 const optional=read(pe+24,32);assert.equal(optional.readUInt16LE(0),0x20b);assert.equal(optional.readBigUInt64LE(24),0x180000000n);
 const sections=Array.from({length:n},(_,i)=>{const b=read(pe+24+opt+i*40,40);return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 result.tables=[0x34c470,0x34c000].map(rva=>{
  const sectionsAt=sections.filter(s=>rva>=s.rva&&rva+16<=s.rva+s.size);assert.equal(sectionsAt.length,1);
  const section=sectionsAt[0];assert((section.flags&0x20000000)===0&&(section.flags&0x80000000)===0,'Expected non-executable readonly data');
  const offset=section.raw+rva-section.rva,b=read(offset,16,false);
  return {va:'0x'+(0x180000000n+BigInt(rva)).toString(16),file_offset:offset,bytes:b.toString('hex'),int32:Array.from({length:4},(_,i)=>b.readInt32LE(i*4))};
 });
 result.metadata_bytes=metadataBytes;result.status='two-fixed-copy-tables-read';
}catch(e){failure=e;result.status='failed';result.failure=String(e);}
finally{
 if(fd!==undefined)fs.closeSync(fd);
 result.original_sha256_after=hashFile(original);result.copy_sha256_after=hashFile(copy);
 result.original_hash_matches=result.original_sha256_after===expected;result.copy_hash_matches=result.copy_sha256_after===expected;
 result.finished_utc=new Date().toISOString();result.controller_resource_usage=process.resourceUsage();
 fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});
}
assert(result.original_hash_matches&&result.copy_hash_matches);if(failure)throw failure;
console.log(JSON.stringify({status:result.status,tables:result.tables,original_unchanged:true,copy_unchanged:true}));
