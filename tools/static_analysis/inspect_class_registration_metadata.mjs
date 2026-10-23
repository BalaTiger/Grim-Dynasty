// Three exact exported ClassInfo records and their first virtual slots; one
// actual QuestItem ClassInfo vtable target's unwind boundary. No code reads.
import fs from 'node:fs';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
assert.equal(process.argv.length,2);
process.chdir('C:/Users/zhuzi/Documents/Grim-Dynasty');
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const source='docs/object-lifecycle/offline-class-registration-exports/discovery.json';
const raw=fs.readFileSync(source);
assert.equal(hash(raw),'0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58');
const discovery=JSON.parse(raw);
const boundaryPath='docs/object-lifecycle/offline-questitem-boundaries/boundary.json';
const boundaryRaw=fs.readFileSync(boundaryPath),old=JSON.parse(boundaryRaw);
assert.equal(hash(boundaryRaw),'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05');
const slotPath='docs/object-lifecycle/offline-questitem-read-dependencies/dependencies.json';
const slotRaw=fs.readFileSync(slotPath);
assert.equal(hash(slotRaw),'34da4f65ac861c51fe2f64dda560543629c3652b1768e920af2213135f58abbc');
assert.equal(JSON.parse(slotRaw).classinfo_vtable.slot0,'0x180591430');
const out='docs/object-lifecycle/offline-class-registration-metadata';
assert(!fs.existsSync(out));
const report={status:'started',started_utc:new Date().toISOString(),
  source_evidence:[{path:source,sha256:hash(raw)},{path:boundaryPath,sha256:hash(boundaryRaw)},
    {path:slotPath,sha256:hash(slotRaw)}],
  target_loaded_or_executed:false,backend_started:false,code_bytes_read:0,
  fixed_data_bytes:168,per_dll_read_budget:4096,files:[],failures:[],
  scope:'Three exact exported 48-byte records and first 8-byte vtable slots; one observed virtual target boundary only',
  limitations:['On-disk records do not establish runtime registration. No neighboring code candidate is followed.',
    'Full-file SHA256 passes read all bytes using 64KiB buffers, outside interpreted metadata/data totals.']};
for(let index=0;index<2;index++){
 const evidence=discovery.files[index],r={target:evidence.target,expected_sha256:evidence.expected_sha256,
   read_bytes:0,reads:[],records:[],boundaries:[]};report.files.push(r);
 let fd,stat;
 const sections=evidence.pe.sections;
 function fingerprint(){
  const b=Buffer.alloc(65536),h=createHash('sha256');let p=0;
  while(p<stat.size){const n=fs.readSync(fd,b,0,Math.min(b.length,stat.size-p),p);assert(n>0);h.update(b.subarray(0,n));p+=n;}
  return h.digest('hex');
 }
 function readRva(rva,n,role){
  assert(n>0&&n<=512&&r.read_bytes+n<=4096);
  const owners=sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(owners.length,1);
  const s=owners[0];assert.equal(s.characteristics&0x20000000,0,'No executable reads');
  const off=s.raw_offset+rva-s.rva,b=Buffer.alloc(n);
  assert.equal(fs.readSync(fd,b,0,n,off),n);
  r.read_bytes+=n;r.reads.push({rva:hex(rva),bytes:n,file_offset:off,role});return b;
 }
 try{
  assert.equal(evidence.source_hash_preserved,true);
  fd=fs.openSync(evidence.target,'r');stat=fs.fstatSync(fd);assert.equal(stat.size,evidence.expected_file_bytes);
  r.sha256_before=fingerprint();assert.equal(r.sha256_before,evidence.expected_sha256);
  const names=index===0?['Item']:['Actor','Object'];
  for(const name of names){
   const candidates=evidence.candidates.filter(e=>e.prefix==='?classInfo@'+name+'@GAME@@');assert.equal(candidates.length,1);
   const e=candidates[0],rva=Number(e.rva),bytes=readRva(rva,48,'exact-exported-'+name+'-classinfo');
   const qwords=Array.from({length:6},(_,i)=>hex(bytes.readBigUInt64LE(i*8)));
   const vp=Number(bytes.readBigUInt64LE(0)-0x180000000n),v=readRva(vp,8,'first-classinfo-vtable-slot');
   r.records.push({name,rva:hex(rva),decorated_name:e.decorated_name,hex:bytes.toString('hex'),qwords,
     vtable_rva:hex(vp),vtable_slot0:hex(v.readBigUInt64LE(0))});
  }
  if(index===0){
   const pdata=Number(old.pe.exception_directory_rva),count=old.pe.entry_count,cache=new Map();
   function entry(i){assert(i>=0&&i<count);if(cache.has(i))return cache.get(i);
    const b=readRva(pdata+12*i,12,'fixed-classinfo-virtual-target-pdata');
    const e={index:i,begin:b.readUInt32LE(0),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8)};
    assert(e.begin<e.end);cache.set(i,e);return e;}
   let lo=0,hi=count,steps=0;while(lo<hi){assert(steps++<32);const mid=lo+Math.floor((hi-lo)/2),e=entry(mid);
    if(e.begin<0x591430)lo=mid+1;else hi=mid;}
   const e=entry(lo);assert.equal(e.begin,0x591430);
   const s=sections.find(s=>e.begin>=s.rva&&e.end<=s.rva+s.raw_size);assert(s&&(s.characteristics&0x20000000));
   r.boundaries.push({role:'QuestItem-classinfo-actual-vtable-slot0',begin_rva:hex(e.begin),end_rva_exclusive:hex(e.end),
    begin_va:hex(0x180000000n+BigInt(e.begin)),end_va_exclusive:hex(0x180000000n+BigInt(e.end)),
    range_bytes:e.end-e.begin,exception_entry_index:e.index,exception_entry_rva:hex(pdata+12*e.index),
    unwind_info_rva:hex(e.unwind),file_offset:hex(s.raw_offset+e.begin-s.rva)});
  }
  r.status='fixed-classinfo-metadata-verified';
 }catch(e){r.status='failed';report.failures.push(String(e));}
 finally{
  if(fd!==undefined){
   try{r.sha256_after=fingerprint();const after=fs.fstatSync(fd);
    r.source_hash_preserved=r.sha256_before===evidence.expected_sha256&&r.sha256_after===evidence.expected_sha256;
    r.source_stat_stable=after.size===stat.size&&after.mtimeMs===stat.mtimeMs&&after.ctimeMs===stat.ctimeMs;
    assert(r.source_hash_preserved&&r.source_stat_stable);
   }catch(e){r.status='failed';report.failures.push(String(e));}
   finally{fs.closeSync(fd);}
  }
 }
 if(report.failures.length)break;
}
report.status=report.failures.length?'failed':'fixed-class-registration-metadata-verified';
report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
fs.mkdirSync(out);fs.writeFileSync(out+'/metadata.json',JSON.stringify(report,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({status:report.status,files:report.files.map(({reads,...r})=>r)}));
if(report.failures.length)process.exitCode=1;
