// Fixed metadata-only lookup. Preparation does not authorize execution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length,2,'No arguments accepted');
const root='C:/Users/zhuzi/Documents/Grim-Dynasty';
const self=fileURLToPath(import.meta.url);
assert.equal(path.resolve(path.dirname(self),'../..').toLowerCase(),path.resolve(root).toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const size=4485632,symbol='??0WorldVec3@GAME@@QEAA@XZ',hint=565;
const out=path.join(root,'docs/object-lifecycle/offline-worldvec3-constructor-boundary');
const sources=[
 ['docs/object-lifecycle/offline-item-ba8-caller-import/inspection.json','4624f2030c5f5c8ae5501c00bd012869c124095476c9fe154c2378d63f8a5091'],
 ['docs/object-lifecycle/offline-class-registration-exports/discovery.json','0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b']
];
const hash=b=>createHash('sha256').update(b).digest('hex');
const hex=n=>'0x'+n.toString(16);
function plain(p,file=false){
 let first=true;
 for(let c=path.resolve(p);;){
  const s=fs.lstatSync(c);
  assert(!s.isSymbolicLink(),'Symlink/junction rejected: '+c);
  assert(first&&file?s.isFile():s.isDirectory(),'Unexpected path kind: '+c);
  assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase(),'Redirected path rejected');
  const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;
 }
}
plain(path.dirname(out));assert(!fs.existsSync(out),'Output already exists');
fs.mkdirSync(out);plain(out);
const report={schema:'grim-dynasty-worldvec3-export-boundary-v1',started_utc:new Date().toISOString(),status:'started',
 original,expected_sha256:expected,expected_file_bytes:size,symbol,hint,
 sources:sources.map(([path,sha256])=>({path,sha256})),target_open_mode:'O_RDONLY',
 target_loaded_or_executed:false,subprocesses:0,code_bytes_read:0,metadata_limit:4096,metadata_bytes:0,
 search_step_limit:32,reads:[],cached_metadata:[],encountered_pdata:[],failures:[],source_closed:false,
 restrictions:['Only the fixed hint name, its ordinal/EAT entry and exact-target .pdata search are authorized metadata.',
  'A hint-name mismatch stops without another name lookup. No header, code, unwind body, import or callee read.',
  'Pinned export/exception metadata and encountered .pdata raw entries are reused without original rereads.',
  'No exact .pdata start means no selected code range; adjacent metadata is evidence only.',
  'PE sorted runtime-function table is assumed; only encountered entries are checked.',
  '64KiB full-file hash passes are identity checks, separate from semantic metadata bytes.',
  'Hash/stat stability is not an atomic snapshot. Exact .pdata bounds do not prove the full logical constructor or EH graph.',
  'No DLL load, original execution, tool subprocess, security change, or build/test execution.']};
let fd,initialStat,pe,pdata;
function fingerprint(){
 const buffer=Buffer.alloc(65536),h=createHash('sha256');
 for(let at=0;at<size;){const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,size-at),at);assert(n>0,'Short hash read');h.update(buffer.subarray(0,n));at+=n;}
 return h.digest('hex');
}
function map(rva,n){
 assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const matches=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(matches.length,1,'Non-unique file mapping');
 const section=matches[0],offset=section.raw_offset+rva-section.rva;assert(offset>=0&&offset+n<=size);
 return {section,offset};
}
function read(at,n,kind){
 assert(Number.isSafeInteger(at)&&at>=0&&Number.isSafeInteger(n)&&n>0&&at+n<=size);
 assert(report.metadata_bytes+n<=report.metadata_limit,'Metadata budget');report.metadata_bytes+=n;
 const b=Buffer.alloc(n);let done=0;
 while(done<n){const got=fs.readSync(fd,b,done,n-done,at+done);assert(got>0,'Short metadata read');done+=got;}
 report.reads.push({kind,file_offset:at,bytes:n,hex:b.toString('hex')});return b;
}
function rread(rva,n,kind){const m=map(rva,n);assert(!(m.section.characteristics&0x20000000),'Metadata unexpectedly maps executable section');return read(m.offset,n,kind);}
try{
 plain(self,true);report.script_sha256=hash(fs.readFileSync(self));
 const proofs=sources.map(([relative,sha])=>{const f=path.join(root,relative);plain(f,true);assert(fs.statSync(f).size<524288);const b=fs.readFileSync(f);assert.equal(hash(b),sha,'Evidence hash: '+relative);return JSON.parse(b);});
 const game=proofs[0],binding=game.binding;
 assert.equal(game.status,'fixed-caller-and-engine-import-verified');assert(game.source_closed&&game.source_hash_preserved&&game.source_stat_stable);
 assert.equal(game.sha256_before,'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');assert.equal(game.sha256_after,game.sha256_before);
 assert.equal(binding.iat_rva,'0x609720');assert.equal(binding.dll,'Engine.dll');assert.equal(binding.symbol,symbol);assert.equal(binding.hint,hint);assert.equal(binding.hint_name_rva,'0xa48732');report.source_binding=binding;
 const exportProof=proofs[1].files.find(f=>f.target===original),cache=proofs[2];
 assert(exportProof&&exportProof.source_hash_preserved&&exportProof.source_stat_stable);
 assert.equal(exportProof.sha256_before,expected);assert.equal(exportProof.sha256_after,expected);assert.equal(exportProof.preferred_image_base,'0x180000000');
 assert.equal(cache.original,original);assert.equal(cache.status,'exact-actor-export-and-boundary-verified');assert(cache.source_hash_preserved&&cache.source_stat_stable);assert.equal(cache.sha256_before,expected);assert.equal(cache.sha256_after,expected);
 pe=exportProof.pe;assert.equal(pe.function_count,6283);assert.equal(pe.named_export_count,6283);
 assert.deepEqual(cache.pe.sections,pe.sections);assert.equal(cache.pe.image_base,'0x180000000');assert.equal(cache.pe.image_size,pe.size_of_image);
 const exportRaw=Buffer.from(pe.export_directory_bytes,'hex');assert.equal(exportRaw.length,40);
 assert.equal(exportRaw.readUInt32LE(16),pe.ordinal_base);assert.equal(exportRaw.readUInt32LE(20),pe.function_count);assert.equal(exportRaw.readUInt32LE(24),pe.named_export_count);
 assert.equal(exportRaw.readUInt32LE(28),Number(pe.address_table_rva));assert.equal(exportRaw.readUInt32LE(32),Number(pe.name_pointer_table_rva));assert.equal(exportRaw.readUInt32LE(36),Number(pe.name_ordinal_table_rva));
 const exceptionReads=cache.reads.filter(r=>r.kind==='exception-directory');assert.equal(exceptionReads.length,1);
 const exception=exceptionReads[0],exceptionRaw=Buffer.from(exception.hex,'hex');assert.equal(exception.bytes,8);assert.equal(exceptionRaw.length,8);assert.equal(exception.file_offset,pe.pe_header_file_offset+24+136);
 const prva=exceptionRaw.readUInt32LE(),pbytes=exceptionRaw.readUInt32LE(4);assert.equal(prva,Number(cache.pe.exception_rva));assert.equal(pbytes,cache.pe.exception_bytes);assert(pbytes>0&&pbytes%12===0);
 const pm=map(prva,pbytes);assert.equal(pm.section.name,'.pdata');assert(!(pm.section.characteristics&0x20000000));assert.equal(pm.offset,cache.pe.exception_file_offset);assert.equal(pbytes/12,cache.pe.entry_count);
 pdata={rva:prva,bytes:pbytes,offset:pm.offset,count:pbytes/12};report.pe={...cache.pe,export_directory_rva:pe.export_directory_rva,export_directory_size:pe.export_directory_size,address_table_rva:pe.address_table_rva,name_pointer_table_rva:pe.name_pointer_table_rva,name_ordinal_table_rva:pe.name_ordinal_table_rva,ordinal_base:pe.ordinal_base};
 report.cached_metadata.push({kind:'export-directory',source_index:1,hex:pe.export_directory_bytes},{kind:'exception-directory',source_index:2,...exception});
 const cachedEntries=new Map();
 for(const e of cache.encountered_pdata){
  assert(Number.isInteger(e.index)&&e.index>=0&&e.index<pdata.count&&!cachedEntries.has(e.index));const b=Buffer.from(e.raw_hex,'hex');assert.equal(b.length,12);
  assert.equal(b.readUInt32LE(),e.begin);assert.equal(b.readUInt32LE(4),e.end);assert.equal(b.readUInt32LE(8),e.unwind);assert(e.begin<e.end);
  const oldRead=cache.reads.find(r=>r.kind==='pdata-entry'&&r.file_offset===pdata.offset+e.index*12);assert(oldRead);assert.equal(oldRead.bytes,12);assert.equal(oldRead.hex,e.raw_hex);cachedEntries.set(e.index,e);
 }
 plain(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initialStat=fs.fstatSync(fd);assert(initialStat.isFile());assert.equal(initialStat.size,size);
 report.sha256_before=fingerprint();assert.equal(report.sha256_before,expected);
 // The import hint is a name-table index, not an ordinal or target RVA.
 assert(hint<pe.named_export_count);
 const nameRva=rread(Number(pe.name_pointer_table_rva)+hint*4,4,'fixed-hint-name-pointer').readUInt32LE();
 const expectedName=Buffer.from(symbol+'\0','ascii'),nameRaw=rread(nameRva,expectedName.length,'fixed-hint-name');
 report.name_candidate={index:hint,rva:hex(nameRva),raw_hex:nameRaw.toString('hex')};
 assert(nameRaw.equals(expectedName),'Hint name mismatch: no fallback lookup authorized');
 const ordinalIndex=rread(Number(pe.name_ordinal_table_rva)+hint*2,2,'fixed-hint-ordinal').readUInt16LE();assert(ordinalIndex<pe.function_count);
 const target=rread(Number(pe.address_table_rva)+ordinalIndex*4,4,'fixed-export-address').readUInt32LE();
 report.export={index:hint,name:symbol,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+pe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target)};
 assert(!(target>=Number(pe.export_directory_rva)&&target<Number(pe.export_directory_rva)+pe.export_directory_size),'Export forwarder outside scope');assert(map(target,1).section.characteristics&0x20000000,'Export is not executable-mapped');
 const visited=new Map();
 function entry(index){
  if(visited.has(index))return visited.get(index);assert(Number.isInteger(index)&&index>=0&&index<pdata.count);
  let e;
  if(cachedEntries.has(index)){e={...cachedEntries.get(index),reused:true,source_index:2};report.cached_metadata.push({kind:'pdata-entry',source_index:2,index,file_offset:pdata.offset+index*12,bytes:12,hex:e.raw_hex});}
  else{const b=read(pdata.offset+index*12,12,'pdata-entry');e={index,begin:b.readUInt32LE(),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8),raw_hex:b.toString('hex'),reused:false};}
  assert(e.begin<e.end);for(const [i,other] of visited)assert(i<index?other.begin<=e.begin:other.begin>=e.begin,'Encountered .pdata order');visited.set(index,e);report.encountered_pdata.push(e);return e;
 }
 let lo=0,hi=pdata.count,steps=0;
 while(lo<hi){assert(steps<report.search_step_limit,'Search step budget');steps++;report.pdata_search_steps=steps;const mid=lo+Math.floor((hi-lo)/2);if(entry(mid).begin<target)lo=mid+1;else hi=mid;}
 report.lower_bound_index=lo;
 const next=lo<pdata.count?entry(lo):null;
 if(next&&next.begin===target){
  const m=map(next.begin,next.end-next.begin);assert(m.section.characteristics&0x20000000);
  report.exact_boundary={...next,begin_rva:hex(next.begin),end_rva_exclusive:hex(next.end),begin_va:hex(0x180000000+next.begin),end_va_exclusive:hex(0x180000000+next.end),range_bytes:next.end-next.begin,code_file_offset:m.offset,code_bytes_read:0};
  report.status='exact-worldvec3-export-and-boundary-verified';
 }else{
  const previous=lo>0?entry(lo-1):null;report.previous=previous;report.next=next;report.selected_code_range=null;
  report.boundary_assessment=previous&&target>=previous.begin&&target<previous.end?'no-exact-begin-containing-entry':'no-exact-begin-gap';report.status='exact-export-without-exact-runtime-function';
 }
}catch(e){report.failures.push(String(e));report.status='failed';}
finally{
 if(fd!==undefined){
  try{if(initialStat&&initialStat.size===size){report.sha256_after=fingerprint();assert.equal(report.sha256_after,expected);const s=fs.fstatSync(fd);report.source_stat_stable=s.size===initialStat.size&&s.ino===initialStat.ino&&s.dev===initialStat.dev&&s.mtimeMs===initialStat.mtimeMs&&s.ctimeMs===initialStat.ctimeMs;assert(report.source_stat_stable,'Source stat changed');}}
  catch(e){report.failures.push(String(e));}
  finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push(String(e));}}
 }
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;
 if(report.failures.length)report.status='failed';report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
 plain(out);fs.writeFileSync(path.join(out,'metadata.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});
}
console.log(JSON.stringify({status:report.status,export:report.export,exact_boundary:report.exact_boundary,metadata_bytes:report.metadata_bytes,code_bytes_read:0,source_closed:report.source_closed,source_hash_preserved:report.source_hash_preserved,failures:report.failures}));
if(report.failures.length)process.exitCode=1;
