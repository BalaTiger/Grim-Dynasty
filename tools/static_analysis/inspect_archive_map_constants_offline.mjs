// Two observed dependencies only: empty-query first byte, normal memcmp import.
// Streaming fingerprints; bounded PE metadata reads. No DLL load or execution.
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
const proofs=[
 'docs/object-lifecycle/offline-archive-map-publish/280ab0-instructions.json',
 'docs/object-lifecycle/rea-record-cache-compare/05-Record-cache-compare-0x1802ee060-assembly.json'
];
const proofBytes=proofs.map(p=>fs.readFileSync(path.join(root,p)));
assert(JSON.parse(proofBytes[0]).some(r=>r.address==='0x180280ad4'&&r.instruction.includes('0000000180339CE0h')));
assert(proofBytes[1].toString().toLowerCase().includes('302710'));
const output=path.join(root,'docs/object-lifecycle/offline-archive-map-publish/empty-query-and-compare-import.json');
assert(!fs.existsSync(output));
const hash=b=>createHash('sha256').update(b).digest('hex');
function hashFile(p){
 const fd=fs.openSync(p,'r'),b=Buffer.alloc(65536),h=createHash('sha256');
 try{let n;while((n=fs.readSync(fd,b,0,b.length,null))>0)h.update(b.subarray(0,n));}
 finally{fs.closeSync(fd);}return h.digest('hex');
}
const result={started_utc:new Date().toISOString(),status:'started',original,
 source_evidence:proofs.map((p,i)=>({path:p,sha256:hash(proofBytes[i])})),
 target_loaded_or_executed:false,backend_started:false,
 limitations:['Static on-disk normal import identity only; external CRT implementation and failure behavior are not recovered.',
 'Only one first byte of the observed post-import query is interpreted; no string or library scan.']};
let fd,failure;
try{
 result.original_sha256_before=hashFile(original);assert.equal(result.original_sha256_before,expected);
 assert.equal(hashFile(copy),expected);
 fd=fs.openSync(original,'r');const size=fs.fstatSync(fd).size;assert(size<=64*1024*1024);
 let metadataBytes=0;
 function read(offset,length,metadata=true){
  assert(Number.isSafeInteger(offset)&&offset>=0&&length>0&&length<=4096&&offset+length<=size);
  const b=Buffer.alloc(length);assert.equal(fs.readSync(fd,b,0,length,offset),length);
  if(metadata)metadataBytes+=length;return b;
 }
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
 const pe=dos.readUInt32LE(60);assert(pe<=65536);
 const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const n=coff.readUInt16LE(6),optLength=coff.readUInt16LE(20);
 assert(n>0&&n<=96&&optLength>=128&&optLength<=4096);
 const opt=read(pe+24,128);assert.equal(opt.readUInt16LE(0),0x20b);
 assert.equal(opt.readBigUInt64LE(24),0x180000000n);assert(opt.readUInt32LE(108)>=2);
 const sections=Array.from({length:n},(_,i)=>{
  const b=read(pe+24+optLength+i*40,40);
  return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20),flags:b.readUInt32LE(36)};
 });
 function offset(rva,length){
  const ss=sections.filter(s=>rva>=s.rva&&rva+length<=s.rva+s.size);
  assert.equal(ss.length,1);return ss[0].raw+rva-ss[0].rva;
 }
 const qs=sections.filter(s=>0x339ce0>=s.rva&&0x339ce1<=s.rva+s.size);
 assert.equal(qs.length,1);assert((qs[0].flags&0xa0000000)===0);
 const query=read(offset(0x339ce0,1),1,false);
 result.empty_query={va:'0x180339ce0',bytes:query.toString('hex'),bytes_read:1};
 assert.equal(query[0],0);
 const thunk=read(offset(0x2ee060,6),6,false);
 assert.equal(thunk[0],0xff);assert.equal(thunk[1],0x25);
 const iatRva=0x2ee060+6+thunk.readInt32LE(2);assert.equal(iatRva,0x302710);
 result.compare_thunk={va:'0x1802ee060',bytes:thunk.toString('hex'),bytes_read:6,iat_va:'0x180302710'};
 const importRva=opt.readUInt32LE(120),importSize=opt.readUInt32LE(124);
 assert(importRva>0&&importSize>=20&&importSize<=4096);
 function ascii(rva){
  const a=[];
  for(let i=0;i<256;i++){const b=read(offset(rva+i,1),1)[0];if(b===0)return Buffer.from(a).toString('ascii');assert(b<128);a.push(b);}
  throw Error('Import name exceeds covered bound');
 }
 for(let i=0;(i+1)*20<=importSize;i++){
  const d=read(offset(importRva+i*20,20),20);
  const values=Array.from({length:5},(_,j)=>d.readUInt32LE(j*4));
  if(values.every(v=>v===0))break;
  const [oft,,,nameRva,ft]=values;
  if(iatRva<ft||(iatRva-ft)%8!==0)continue;
  const index=(iatRva-ft)/8;if(index>=4096)continue;
  let entry=0n;
  for(let j=0;j<=index;j++){
   entry=read(offset((oft||ft)+j*8,8),8).readBigUInt64LE();
   if(entry===0n)break;
  }
  if(entry===0n)continue;
  assert((entry&0x8000000000000000n)===0n);
  assert(entry<=0xffffffffn);
  const symbol=ascii(Number(entry)+2),dll=ascii(nameRva);
  result.normal_import={dll,name:symbol,descriptor_rva:importRva+i*20,thunk_index:index};
  assert.equal(symbol,'memcmp');break;
 }
 assert(result.normal_import);
 result.metadata_bytes=metadataBytes;result.code_bytes_read=6;result.fixed_data_bytes=1;
 result.status='empty-query-and-normal-memcmp-import-confirmed';
}catch(e){failure=e;result.status='failed';result.failure=String(e);}
finally{
 if(fd!==undefined)fs.closeSync(fd);
 result.original_sha256_after=hashFile(original);result.copy_sha256_after=hashFile(copy);
 result.original_hash_matches=result.original_sha256_after===expected;
 result.copy_hash_matches=result.copy_sha256_after===expected;
 result.finished_utc=new Date().toISOString();result.controller_resource_usage=process.resourceUsage();
 fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});
}
assert(result.original_hash_matches&&result.copy_hash_matches);if(failure)throw failure;
console.log(JSON.stringify({status:result.status,empty_query:result.empty_query,normal_import:result.normal_import,
 metadata_bytes:result.metadata_bytes,original_unchanged:true,copy_unchanged:true}));
