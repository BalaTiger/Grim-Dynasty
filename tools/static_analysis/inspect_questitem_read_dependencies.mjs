// Fixed literal references, two classInfo vtable slots, and one direct base Read
// boundary. Data/metadata only; no code decoding, target execution, or recursion.
import fs from 'node:fs';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
assert.equal(process.argv.length,2);
process.chdir('C:/Users/zhuzi/Documents/Grim-Dynasty');
const target='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const saved='docs/object-lifecycle/offline-questitem-boundaries/boundary.json';
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const savedBytes=fs.readFileSync(saved);
assert.equal(hash(savedBytes),'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05');
const proof=JSON.parse(savedBytes);assert.equal(proof.source_hash_preserved,true);
const asmPath='docs/object-lifecycle/offline-questitem-core/427e40-instructions.json',asmBytes=fs.readFileSync(asmPath);
const asm=JSON.parse(asmBytes),ins=JSON.parse(fs.readFileSync('docs/object-lifecycle/offline-questitem-core/inspection.json'));
assert.equal(ins.status,'fixed-questitem-core-disassembly-verified');
assert.equal(hash(Buffer.from(asm.map(x=>x.bytes).join(''),'hex')),ins.fragments[2].code_sha256);
assert(asm.some(x=>x.address==='0x180427e5b'&&x.instruction==='call        0000000180324C60'));
const literals=[
 [0x6b4cc4,'first-string-key'],[0x61abc5,'first-string-default'],
 [0x6b4eb8,'resource-string-key'],[0x6163ad,'resource-string-default'],
 [0x6b4e78,'second-owned-string-key'],[0x61abc6,'second-owned-string-default'],
 [0x6b4e68,'vslot-38-key'],[0x6b4ea0,'vslot-58-key']
];
for(const [rva]of literals)assert(asm.some(x=>x.instruction.toLowerCase().includes((0x180000000n+BigInt(rva)).toString(16))));
const out='docs/object-lifecycle/offline-questitem-read-dependencies';assert(!fs.existsSync(out));
for(const d of ['docs','docs/object-lifecycle'])assert(!fs.lstatSync(d).isSymbolicLink());
fs.mkdirSync(out);
const result={status:'started',started_utc:new Date().toISOString(),target,expected_sha256:expected,
 source_evidence:[{path:saved,sha256:hash(savedBytes)},{path:asmPath,sha256:hash(asmBytes)}],
 target_loaded_or_executed:false,backend_started:false,code_bytes_read:0,
 interpreted_read_byte_limit:8192,read_bytes:0,reads:[],literal_byte_limit:64,literals:[],boundaries:[],
 hash_scope:'Full-file SHA256 before/after with one 64KiB buffer, outside semantic-read totals',
 limitations:['ClassInfo vtable slots are on-disk pointers; their behavior and runtime name initialization remain unknown.',
 'One .pdata fragment for the directly called Item Read is metadata only; no inherited function is automatically followed.']};
let fd,stat;const failures=[];
function fingerprint(){
 const h=createHash('sha256'),b=Buffer.alloc(65536);let pos=0;
 while(pos<stat.size){const n=fs.readSync(fd,b,0,Math.min(b.length,stat.size-pos),pos);assert(n>0);h.update(b.subarray(0,n));pos+=n;}return h.digest('hex');
}
function readRva(rva,n,kind,executable=false){
 assert(n>0&&n<=4096&&result.read_bytes+n<=8192);
 const owners=proof.pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(owners.length,1);
 const s=owners[0];assert.equal(Boolean(s.characteristics&0x20000000),executable);
 const off=s.raw_offset+rva-s.rva;assert(off>=0&&off+n<=stat.size);
 const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,off),n);
 result.read_bytes+=n;result.reads.push({rva:hex(rva),file_offset:off,bytes:n,kind});return b;
}
try{
 fd=fs.openSync(target,'r');stat=fs.fstatSync(fd);assert.equal(stat.size,11385856);
 result.sha256_before=fingerprint();assert.equal(result.sha256_before,expected);
 for(const [rva,role]of literals){
  const bytes=[];let terminated=false;
  for(let i=0;i<64;i++){const x=readRva(rva+i,1,'fixed-literal-byte')[0];bytes.push(x);if(x===0){terminated=true;break;}}
  assert(terminated,'Fixed literal exceeds 64-byte budget');
  const b=Buffer.from(bytes);result.literals.push({rva:hex(rva),role,bytes:b.length,hex:b.toString('hex'),
   ascii:b.subarray(0,b.length-1).every(x=>x<128)?b.subarray(0,b.length-1).toString('ascii'):null});
 }
 assert.equal(proof.classinfo.qwords[0],'0x1807a7e58');
 const vt=readRva(0x7a7e58,16,'two-classinfo-vtable-slots');
 result.classinfo_vtable={rva:'0x7a7e58',bytes:16,slot0:hex(vt.readBigUInt64LE(0)),slot8:hex(vt.readBigUInt64LE(8))};
 const pdata=Number(proof.pe.exception_directory_rva),count=proof.pe.entry_count,cache=new Map();
 function entry(i){
  assert(i>=0&&i<count);
  if(cache.has(i))return cache.get(i);
  const b=readRva(pdata+12*i,12,'Item-Read-boundary-search');
  const e={index:i,begin:b.readUInt32LE(0),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8)};
  assert(e.begin<e.end);cache.set(i,e);return e;
 }
 let lo=0,hi=count,steps=0;
 while(lo<hi){assert(steps++<32);const mid=lo+Math.floor((hi-lo)/2),e=entry(mid);if(e.begin<0x324c60)lo=mid+1;else hi=mid;}
 const e=entry(lo);assert.equal(e.begin,0x324c60);
 const section=proof.pe.sections.find(s=>e.begin>=s.rva&&e.end<=s.rva+s.raw_size);assert(section&&(section.characteristics&0x20000000));
 result.boundaries.push({name:'direct-Item-Read',begin_rva:hex(e.begin),end_rva_exclusive:hex(e.end),range_bytes:e.end-e.begin,
  begin_va:hex(0x180000000n+BigInt(e.begin)),end_va_exclusive:hex(0x180000000n+BigInt(e.end)),
  exception_entry_index:e.index,exception_entry_rva:hex(pdata+12*e.index),unwind_info_rva:hex(e.unwind),
  file_offset:hex(section.raw_offset+e.begin-section.rva),section:section.name,code_bytes_read:0});
 result.status='fixed-QuestItem-literals-and-base-boundary-confirmed';
}catch(e){failures.push(String(e));}
finally{
 if(fd!==undefined){
  try{result.sha256_after=fingerprint();const after=fs.fstatSync(fd);
   result.source_stat_stable=after.size===stat.size&&after.mtimeMs===stat.mtimeMs&&after.ctimeMs===stat.ctimeMs;
   result.source_hash_preserved=result.sha256_before===expected&&result.sha256_after===expected;
   assert(result.source_stat_stable&&result.source_hash_preserved);
  }catch(e){failures.push(String(e));}finally{fs.closeSync(fd);}
 }
 if(failures.length)result.status='failed';result.failures=failures;
 result.finished_utc=new Date().toISOString();result.controller_resource_usage=process.resourceUsage();
 fs.writeFileSync(out+'/dependencies.json',JSON.stringify(result,null,2)+'\n',{flag:'wx'});
}
console.log(JSON.stringify({status:result.status,read_bytes:result.read_bytes,literals:result.literals,
 classinfo_vtable:result.classinfo_vtable,boundaries:result.boundaries,source_hash_preserved:result.source_hash_preserved}));
if(failures.length)process.exitCode=1;
