// Prepared only: five fixed direct dependency RVAs plus six fixed trade-name prefixes.
// PE headers/export metadata/.pdata only; no code/global/callee body reads or backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const target='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856;
const pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',base=0x180000000n;
const fixedRvas=[0x11d340,0x77ec0,0x288d30,0x2895a0,0x289650];
const prefixes=['?Buy@','?Sell@','?PurchaseItem@','?Purchase@','?SellToMerchant@','?BuyFromMerchant@'];
const proofPath=path.join(root,'docs/m3-evidence/entry-bodies/inspection.json');
const proofPin='1e9e83e8062a642d8ff226b6c6e7b6bf8bcb9b873536d6a64a4307044d4de3f5';
const output=path.join(root,'docs/m3-evidence/direct-dependency-bounds.json');
const limits={metadata_bytes:32768,metadata_operations:4096,single_read_bytes:4096,hash_chunk_bytes:65536,
  header_end:65536,sections:96,exports:1000000,pdata_bytes:2*1024*1024,search_steps:32,
  distinct_names:256,name_bytes:1024,trade_candidates_per_prefix:2,proof_bytes:65536,report_bytes:131072};
const hex=n=>'0x'+n.toString(16),hash=b=>createHash('sha256').update(b).digest('hex');
const inside=(offset,n,total)=>Number.isSafeInteger(offset)&&Number.isSafeInteger(n)&&offset>=0&&n>0&&offset<=total&&n<=total-offset;
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert.ok(!s.isSymbolicLink());
  assert.ok(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
  const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function outputCheck(){assert.ok(output.startsWith(root+path.sep));normal(path.dirname(output));
  try{fs.lstatSync(output);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing evidence preserved');}
outputCheck();normal(proofPath,true);assert.ok(fs.statSync(proofPath).size<=limits.proof_bytes);
const pb=fs.readFileSync(proofPath);assert.equal(hash(pb),proofPin);const proof=JSON.parse(pb);
assert.equal(proof.status,'FixedBodiesDecodedReviewRequired');assert.equal(proof.sha256_before,pin);assert.equal(proof.sha256_after,pin);
assert.equal(proof.source_hash_preserved,true);assert.equal(proof.source_closed,true);assert.equal(proof.failures.length,0);
assert.ok(proof.bodies.every(b=>b.instruction_byte_coverage_verified));
const refs=proof.bodies.flatMap(b=>[...b.calls,...b.branches]);
for(const rva of fixedRvas)assert.ok(refs.some(r=>r.instruction.toUpperCase().includes((base+BigInt(rva)).toString(16).toUpperCase().padStart(16,'0'))),'Fixed target not in saved direct references');
const report={schema:'grim-dynasty-m3-direct-dependency-bounds-v1',started_utc:new Date().toISOString(),target,expected_sha256:pin,limits,
  source_evidence:{path:'docs/m3-evidence/entry-bodies/inspection.json',sha256:proofPin},fixed_rvas:fixedRvas.map(hex),trade_prefixes:prefixes,
  code_bytes:0,global_data_bytes:0,target_loaded_or_executed:false,backend_started:false,whole_export_table_scanned:false,whole_pdata_scanned:false,
  metadata_bytes:0,metadata_operations:0,metadata_by_kind:{},dependencies:[],trade_searches:[],trade_candidates:[],
  limitations:['Fixed prefixes are candidate names only. No match means Unknown, not absence of trade behavior.',
    'Only encountered lexical ordering is checked; full export names and .pdata tables are not scanned.',
    'Ranges are exact containing .pdata entries; interior aliases/missing entries remain explicit. Sizes do not prove logical body completeness.',
    'No instruction decoding, body/global reads, algorithms, live ABI, runtime initialization or native compatibility is established.',
    'Two 64 KiB streaming hash passes include uninterpreted source bytes outside the metadata scope.']};
function fingerprint(){normal(target,true);const fd=fs.openSync(target,'r'),buffer=Buffer.alloc(limits.hash_chunk_bytes),h=createHash('sha256');
  try{assert.equal(fs.fstatSync(fd).size,size);for(let offset=0;offset<size;){const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,size-offset),offset);assert.ok(n>0);h.update(buffer.subarray(0,n));offset+=n;}return h.digest('hex');}
  finally{fs.closeSync(fd);}}
report.sha256_before=fingerprint();assert.equal(report.sha256_before,pin,'Target fingerprint changed');
const fd=fs.openSync(target,'r');
try{
  assert.equal(fs.fstatSync(fd).size,size);
  function read(offset,n,kind){assert.ok(inside(offset,n,size)&&n<=limits.single_read_bytes);assert.ok(++report.metadata_operations<=limits.metadata_operations);
    assert.ok(report.metadata_bytes+n<=limits.metadata_bytes,'32 KiB metadata budget exceeded; do not expand');report.metadata_bytes+=n;
    report.metadata_by_kind[kind]=(report.metadata_by_kind[kind]||0)+n;const b=Buffer.alloc(n);let got=0;
    while(got<n){const count=fs.readSync(fd,b,got,n-got,offset+got);assert.ok(count>0);got+=count;}return b;}
  const dos=read(0,64,'header');assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert.ok(pe>=64&&pe<limits.header_end-24);
  const coff=read(pe,24,'header');assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
  const count=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);assert.ok(count>0&&count<=limits.sections&&optSize>=144&&optSize<=4096&&pe+24+optSize+40*count<=limits.header_end);
  const opt=read(pe+24,optSize,'header');assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),base);assert.ok(opt.readUInt32LE(108)>=4);
  const sections=Array.from({length:count},(_,i)=>{const b=read(pe+24+optSize+40*i,40,'section');return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20)};});
  function map(rva,n){const hits=sections.filter(s=>inside(rva-s.rva,n,s.size));assert.equal(hits.length,1);const s=hits[0],offset=s.raw+rva-s.rva;assert.ok(inside(offset,n,size));return {offset,available:s.size-rva+s.rva,section:s.name};}
  const rr=(rva,n,kind)=>read(map(rva,n).offset,n,kind);
  const prva=opt.readUInt32LE(136),psize=opt.readUInt32LE(140);assert.ok(prva&&psize>0&&psize%12===0&&psize<=limits.pdata_bytes);const pdata=map(prva,psize);assert.equal(pdata.section,'.pdata');
  function bounds(rva){let lo=0,hi=psize/12-1,steps=0;while(lo<=hi){assert.ok(++steps<=limits.search_steps);const index=Math.floor((lo+hi)/2),b=read(pdata.offset+index*12,12,'pdata-search');
    const begin=b.readUInt32LE(0),end=b.readUInt32LE(4);assert.ok(begin<end);if(rva<begin)hi=index-1;else if(rva>=end)lo=index+1;else return {status:begin===rva?'ExactEntry':'InteriorAddress',index,begin_rva:hex(begin),end_rva_exclusive:hex(end),bytes:end-begin,unwind_info_rva:hex(b.readUInt32LE(8)),logical_function_complete:false};}
    return {status:'UnknownNoContainingPdata',begin_rva:null,end_rva_exclusive:null,bytes:null};}
  for(const rva of fixedRvas)report.dependencies.push({rva:hex(rva),preferred_va:hex(base+BigInt(rva)),pdata:bounds(rva)});
  const erva=opt.readUInt32LE(112),esize=opt.readUInt32LE(116);assert.ok(erva&&esize>=40);const exp=rr(erva,40,'export-directory');
  const ordinalBase=exp.readUInt32LE(16),nf=exp.readUInt32LE(20),nn=exp.readUInt32LE(24),eat=exp.readUInt32LE(28),npt=exp.readUInt32LE(32),ord=exp.readUInt32LE(36);
  assert.ok(nf>0&&nf<=limits.exports&&nn<=limits.exports);map(eat,nf*4);if(nn){map(npt,nn*4);map(ord,nn*2);}report.named_export_count=nn;
  const cache=new Map();function nameAt(index){assert.ok(index>=0&&index<nn);if(cache.has(index))return cache.get(index);assert.ok(cache.size<limits.distinct_names);
    const rva=rr(npt+index*4,4,'export-name-pointer').readUInt32LE(0),parts=[];
    for(let used=0;used<limits.name_bytes;){const m=map(rva+used,1),n=Math.min(32,m.available,limits.name_bytes-used),b=read(m.offset,n,'export-name-ascii'),end=b.indexOf(0),part=end<0?b:b.subarray(0,end);
      for(const ch of part)assert.ok(ch<128);parts.push(part);used+=n;if(end>=0){const value=Buffer.concat(parts).toString('ascii');
        for(const [other,old] of cache)assert.ok(other<index?old<value:old>value,'Encountered name order invalid');cache.set(index,value);return value;}}
    throw Error('Export name length limit');}
  function lower(value){let lo=0,hi=nn,steps=0;while(lo<hi){assert.ok(++steps<=limits.search_steps);const mid=Math.floor((lo+hi)/2);if(nameAt(mid)<value)lo=mid+1;else hi=mid;}return lo;}
  for(const prefix of prefixes){const first=lower(prefix),last=lower(prefix+'\x7f'),matches=last-first,emit=Math.min(matches,limits.trade_candidates_per_prefix);
    report.trade_searches.push({prefix,status:matches?'NamedCandidatesOnly':'UnknownNoMatchingExport',matching_name_range:[first,last],matches,emitted:emit,omitted_due_to_limit:matches-emit});
    for(let i=first;i<first+emit;i++){const name=nameAt(i);assert.ok(name.startsWith(prefix));const index=rr(ord+2*i,2,'export-ordinal').readUInt16LE(0);assert.ok(index<nf);
      const rva=rr(eat+4*index,4,'export-address').readUInt32LE(0),forwarded=rva>=erva&&rva-erva<esize;
      report.trade_candidates.push({prefix,decorated_name:name,rva:hex(rva),ordinal:ordinalBase+index,forwarded,
        pdata:rva&&!forwarded?bounds(rva):{status:forwarded?'UnknownForwardedExport':'UnknownNullExport',bytes:null}});}}
  report.distinct_export_names_read=cache.size;
}finally{fs.closeSync(fd);}
report.sha256_after=fingerprint();assert.equal(report.sha256_after,pin,'Target changed during metadata read');report.source_hash_preserved=true;
report.status='MetadataOnlyCompleted';report.finished_utc=new Date().toISOString();const json=JSON.stringify(report,null,2)+'\n';assert.ok(Buffer.byteLength(json)<=limits.report_bytes);
outputCheck();fs.writeFileSync(output,json,{encoding:'utf8',flag:'wx'});
console.log(JSON.stringify({status:report.status,metadata_bytes:report.metadata_bytes,dependencies:report.dependencies,trade_candidates:report.trade_candidates.length,output:'docs/m3-evidence/direct-dependency-bounds.json'}));
