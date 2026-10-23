// Prepared for review; execution is separate. Fixed Game 2C8750 .pdata lookup only.
// No function body, neighbor code, unwind body, import, data, dumpbin or subprocess.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length,2);
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_global_load_boundary.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856;
const pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const target=0x2c8750,out=path.join(root,'docs/object-lifecycle/offline-item-global-load-boundary');
const sources=[
 ['docs/task-kills/ghidra-selected/subscriber-constructor-routes.json','e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165'],
 ['docs/object-lifecycle/offline-item-return-dependency/inspection.json','fc0f85739966bf84241cc277b6c4d2ae7129a9daa27792ed1245e9cb7b409ff0']
];
const limits={search_steps:32,new_pdata_entries:34,metadata_bytes:408,semantic_read_operations:34,
 code_bytes:0,data_bytes:0,unwind_body_bytes:0,subprocesses:0,hash_buffer_bytes:65536,proof_bytes_each:262144};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);
 assert(!s.isSymbolicLink(),'Symlink or junction refused');assert(first&&file?s.isFile():s.isDirectory());
 assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase(),'Redirected ancestor refused');
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');
 for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0,'Short hash read');h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
normal(self,true);normal(path.dirname(out));absent(out);
const proofs=sources.map(([relative,sha])=>{const p=path.join(root,relative);normal(p,true);assert(fs.statSync(p).size<=limits.proof_bytes_each);
 const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
const callers=proofs[0].functions.filter(f=>f.observed_entry==='1802c07a0');assert.equal(callers.length,1);
assert.equal(callers[0].body,'[[1802c07a0, 1802c1ba6]]');
const callerLines=['1802c18ca: LEA RDX,[RBP + -0x28]','1802c18ce: MOV RCX,RDI','1802c18d1: CALL 0x1802c8750'];
for(const line of callerLines)assert(callers[0].assembly.includes(line),'Missing fixed caller evidence');
const prior=proofs[1],pe=prior.pe;
assert.equal(prior.status,'exact-return-dependency-body-decoded-review-required');assert.equal(prior.original,original);
assert.equal(prior.expected_file_bytes,size);assert.equal(prior.expected_sha256,pin);assert.equal(prior.sha256_before,pin);assert.equal(prior.sha256_after,pin);
assert(prior.source_stat_stable&&prior.source_hash_preserved&&prior.source_closed);assert.equal(prior.failures.length,0);
assert.equal(pe.image_base,'0x180000000');assert.equal(pe.image_size,11427840);
assert.equal(pe.exception_rva,'0xa6a000');assert.equal(pe.exception_file_offset,10896384);
assert.equal(pe.entry_count,22293);assert.equal(pe.exception_bytes,pe.entry_count*12);
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const ss=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);
 const offset=ss[0].raw_offset+rva-ss[0].rva;assert(offset>=0&&offset<=size-n);return {section:ss[0],offset};}
const pm=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pm.section.name,'.pdata');
assert.equal(pm.offset,pe.exception_file_offset);assert(!(pm.section.characteristics&0x20000000));
function decode(index,b){assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);assert.equal(b.length,12);
 const begin=b.readUInt32LE(),end=b.readUInt32LE(4),unwind=b.readUInt32LE(8);
 assert(begin>0&&begin<end&&end<=pe.image_size&&unwind>0&&unwind<pe.image_size);
 // Validate cached section mapping only; this does not read any body bytes.
 assert(map(begin,end-begin).section.characteristics&0x20000000);
 return {index,begin,end,unwind,raw_hex:b.toString('hex'),file_offset:pe.exception_file_offset+index*12};}
const cached=new Map();
for(const v of prior.encountered_pdata){const e=decode(v.index,Buffer.from(v.raw_hex,'hex'));
 assert.equal(v.file_offset,e.file_offset);assert.equal(v.begin,e.begin);assert.equal(v.end,e.end);assert.equal(v.unwind,e.unwind);
 if(cached.has(e.index))assert.equal(cached.get(e.index).raw_hex,e.raw_hex);else cached.set(e.index,e);}
const ordered=[...cached.values()].sort((a,b)=>a.index-b.index);
for(let i=1;i<ordered.length;i++)assert(ordered[i-1].begin<ordered[i].begin,'Cached order mismatch');
const report={schema:'grim-dynasty-item-global-load-boundary-v1',started_utc:new Date().toISOString(),status:'started',
 original,expected_sha256:pin,expected_file_bytes:size,target_rva:hex(target),sources,caller_instructions:callerLines,pe,limits,
 original_open_mode:'O_RDONLY',original_loaded_or_executed:false,whole_table_scanned:false,
 new_header_bytes:0,unwind_body_bytes:0,data_bytes:0,code_bytes:0,callees_followed:0,subprocesses:0,
 metadata_bytes:0,new_pdata_entries:0,semantic_read_operations:0,search_steps:0,cached_entries_available:cached.size,
 reads:[],encountered_pdata:[],selected_code_range:null,source_closed:false,failures:[],
 limitations:['Sorted .pdata is assumed; only cached and encountered entries are checked.',
  'Only exact2C8750 boundary metadata is sought. No body bytes are selected regardless of size or match.',
  'A containing entry or gap is reported without inferring an entry or reading neighboring code.',
  'Boundary metadata does not prove LoadFromDatabase field writes, exception cleanup, logical completeness or runtime compatibility.',
  'Full-file before/after hashes use one64KiB buffer and the same read-only handle; they check identity, not an atomic snapshot.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function readEntry(index){assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);
 assert(report.semantic_read_operations<limits.semantic_read_operations);assert(report.new_pdata_entries<limits.new_pdata_entries);
 assert(report.metadata_bytes+12<=limits.metadata_bytes);
 const at=pe.exception_file_offset+index*12;assert(at>=pm.offset&&at<=pm.offset+pe.exception_bytes-12);assert(at<=size-12);
 report.semantic_read_operations++;report.new_pdata_entries++;report.metadata_bytes+=12;
 const b=Buffer.alloc(12),got=fs.readSync(fd,b,0,12,at);
 report.reads.push({kind:'pdata',index,file_offset:at,requested_bytes:12,read_bytes:got,raw_hex:b.subarray(0,got).toString('hex')});assert.equal(got,12,'Short metadata read');return decode(index,b);}
const visited=new Map();
function entry(index){if(visited.has(index))return visited.get(index);
 const e=cached.has(index)?{...cached.get(index),reused:true}:{...readEntry(index),reused:false};
 for(const [i,other] of visited)assert(i<index?other.begin<e.begin:other.begin>e.begin,'Encountered order mismatch');
 visited.set(index,e);report.encountered_pdata.push(e);return e;}
try{
 fs.mkdirSync(out);created=true;normal(out);report.script_sha256=hash(fs.readFileSync(self));
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);
 report.sha256_before=digest(fd,size);assert.equal(report.sha256_before,pin);
 let lo=0,hi=pe.entry_count;
 while(lo<hi){assert(report.search_steps<limits.search_steps);report.search_steps++;
  const mid=lo+Math.floor((hi-lo)/2);if(entry(mid).begin<target)lo=mid+1;else hi=mid;}
 report.lower_bound_index=lo;const next=lo<pe.entry_count?entry(lo):null;
 if(next&&next.begin===target){
  const length=next.end-next.begin,m=map(next.begin,length);
  report.exact_boundary={...next,begin_rva:hex(next.begin),end_rva_exclusive:hex(next.end),range_bytes:length,code_file_offset:m.offset};
  report.boundary_assessment='exact-begin-body-not-read';report.status='exact-global-load-boundary-metadata-only';
 }else{
  const previous=lo>0?entry(lo-1):null;report.previous=previous;report.next=next;
  report.boundary_assessment=previous&&target>=previous.begin&&target<previous.end?'no-exact-begin-containing-entry':'no-exact-begin-gap';
  report.status='no-exact-global-load-boundary-body-not-read';
 }
}catch(e){report.failures.push(String(e));report.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){report.sha256_after=digest(fd,size);assert.equal(report.sha256_after,pin);
  report.source_stat_stable=stable(initial,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){report.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('Close: '+e);}}}
 report.source_hash_preserved=report.sha256_before===pin&&report.sha256_after===pin;if(report.failures.length)report.status='failed';
 report.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,exact_boundary:report.exact_boundary,boundary_assessment:report.boundary_assessment,
 metadata_bytes:report.metadata_bytes,new_pdata_entries:report.new_pdata_entries,code_bytes:report.code_bytes,
 source_closed:report.source_closed,source_hash_preserved:report.source_hash_preserved,failures:report.failures}));
if(report.failures.length)process.exitCode=1;
