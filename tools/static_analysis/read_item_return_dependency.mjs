// Prepared fixed-target reader. Running requires a separate review; no arguments.
// Game324560 only, exact .pdata body at most1024B; no callee or neighbor follows.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length,2);
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_return_dependency.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856;
const pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const target=0x324560,base=0x180000000n;
const out=path.join(root,'docs/object-lifecycle/offline-item-return-dependency');
const temp=path.join(root,'.tooling/offline-item-return-dependency-temp');
const sources=[
 ['docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json','7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f'],
 ['docs/object-lifecycle/offline-item-factory-dependency-boundaries/metadata.json','59265ca3af388f48af8da9a66bee439949fa8cfeca5fe1cc40a066998ad46d29'],
 ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json','227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'],
 ['docs/object-lifecycle/offline-item-name-assign/inspection.json','5a9167e7cf619e8c07ff33d74fee66e021de32bf6c638e54b67758eb7579c317']
];
const limits={search_steps:32,new_pdata_entries:34,metadata_bytes:408,code_bytes:1024,
 semantic_read_operations:35,dumpbin_calls:1,tiny_pe_bytes:6144,hash_buffer_bytes:65536,proof_bytes_each:262144};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){
 let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);
  assert(!s.isSymbolicLink(),'Symlink or junction refused');assert(first&&file?s.isFile():s.isDirectory());
  assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase(),'Redirected ancestor refused');
  const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}
}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const buffer=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');
 for(let at=0;at<n;){const got=fs.readSync(fd,buffer,0,Math.min(buffer.length,n-at),at);assert(got>0,'Short hash read');h.update(buffer.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const handle=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(handle,fs.fstatSync(handle).size);}finally{fs.closeSync(handle);}}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([relative,sha])=>{const p=path.join(root,relative);normal(p,true);assert(fs.statSync(p).size<=limits.proof_bytes_each);
 const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
const caller=proofs[0].result.structuredContent,prior=proofs[1],later=proofs[2],pe=prior.pe;
assert.equal(caller.subject.name,'Game.dll');assert.equal(caller.subject.digest.sha256,pin);
const callerSites=[0x323a6b];
const lines=caller.normalized_result.split(/\r?\n/);
const callerLines=callerSites.map(rva=>hex(base+BigInt(rva))+': CALL 0x180324560');
for(const line of callerLines)assert(lines.includes(line),'Missing fixed caller');
assert(lines.includes('0x180323a68: MOV RCX, RDI'),'Original Item identity not evidenced');
assert.equal(prior.status,'all-fixed-exact-boundaries-assessed');
assert.equal(later.status,'fixed-item-950-element-boundaries-assessed');
for(const p of [prior,later]){assert.equal(p.target,original);assert.equal(p.expected_sha256,pin);assert.equal(p.expected_bytes,size);
 assert.equal(p.sha256_before,pin);assert.equal(p.sha256_after,pin);assert(p.source_stat_stable&&p.source_hash_preserved&&p.source_handle_closed);}
assert.deepEqual(later.pe,pe);assert.equal(pe.image_base,hex(base));assert.equal(pe.image_size,11427840);
assert.equal(pe.exception_rva,'0xa6a000');assert.equal(pe.exception_file_offset,10896384);
assert.equal(pe.entry_count,22293);assert.equal(pe.exception_bytes,pe.entry_count*12);
const optional=prior.metadata_reads.filter(r=>r.kind==='optional-header-prefix');assert.equal(optional.length,1);
assert.equal(optional[0].file_offset,296);assert.equal(optional[0].read_bytes,144);assert.equal(optional[0].requested_bytes,144);
const optionalRaw=Buffer.from(optional[0].raw_hex,'hex');assert.equal(optionalRaw.length,144);
assert.equal(optionalRaw.readUInt16LE(),0x20b);assert.equal(optionalRaw.readBigUInt64LE(24),base);
assert.equal(optionalRaw.readUInt32LE(56),pe.image_size);assert.equal(optionalRaw.readUInt32LE(136),Number(pe.exception_rva));
assert.equal(optionalRaw.readUInt32LE(140),pe.exception_bytes);
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const ss=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);
 const offset=ss[0].raw_offset+rva-ss[0].rva;assert(offset>=0&&offset<=size-n);return {section:ss[0],offset};}
const pm=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pm.section.name,'.pdata');
assert.equal(pm.offset,pe.exception_file_offset);assert(!(pm.section.characteristics&0x20000000));
const cached=new Map();
function decode(index,b){assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);assert.equal(b.length,12);
 const begin=b.readUInt32LE(),end=b.readUInt32LE(4),unwind=b.readUInt32LE(8);
 assert(begin>0&&begin<end&&end<=pe.image_size&&unwind>0&&unwind<pe.image_size);
 assert(map(begin,end-begin).section.characteristics&0x20000000);
 return {index,begin,end,unwind,raw_hex:b.toString('hex'),file_offset:pe.exception_file_offset+index*12};}
function cache(index,raw,source){const e=decode(index,Buffer.from(raw,'hex'));
 if(cached.has(index)){assert.equal(cached.get(index).raw_hex,e.raw_hex);return;}
 cached.set(index,{...e,source});}
for(const r of prior.metadata_reads.filter(r=>r.kind==='selected-runtime-function')){
 assert.equal(r.read_bytes,12);assert.equal(r.requested_bytes,12);cache((r.file_offset-pe.exception_file_offset)/12,r.raw_hex,1);}
for(const r of later.metadata_reads){assert.equal(r.read_bytes,12);assert.equal(r.requested_bytes,12);
 assert.equal(r.file_offset,pe.exception_file_offset+r.index*12);cache(r.index,r.raw_hex,2);}
for(const e of later.reused_entries){assert.equal(e.file_offset,pe.exception_file_offset+e.index*12);cache(e.index,e.raw_hex,2);}
const nearest=proofs[3];assert.equal(nearest.status,'exact-name-assign-body-decoded-review-required');
assert.equal(nearest.original,original);assert.equal(nearest.sha256_before,pin);assert.equal(nearest.sha256_after,pin);
assert(nearest.source_stat_stable&&nearest.source_hash_preserved&&nearest.source_closed);assert.deepEqual(nearest.pe,pe);
for(const e of nearest.encountered_pdata){assert.equal(e.file_offset,pe.exception_file_offset+e.index*12);cache(e.index,e.raw_hex,3);}
const report={schema:'grim-dynasty-item-return-dependency-v1',started_utc:new Date().toISOString(),status:'started',
 original,expected_sha256:pin,expected_file_bytes:size,target_rva:hex(target),sources,caller_instructions:callerLines,pe,limits,
 original_open_mode:'O_RDONLY',original_loaded_or_executed:false,original_path_given_to_dumpbin:false,
 whole_table_scanned:false,callees_followed:0,new_header_bytes:0,unwind_body_bytes:0,data_bytes:0,
 metadata_bytes:0,code_bytes:0,semantic_read_operations:0,search_steps:0,dumpbin_calls:0,
 cached_entries_available:cached.size,reads:[],encountered_pdata:[],source_closed:false,failures:[],
 limitations:['Sorted .pdata table assumed; only encountered entries are checked.',
  'A missing exact begin or exact body larger than1024B selects no code; no gap/neighbor body read.',
  'Exact .pdata does not prove full logical helper, exception cleanup, or loaded runtime compatibility.',
  '64KiB before/after hashes establish file identity, not an atomic snapshot or runtime behavior.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(at,n,kind){assert(Number.isSafeInteger(at)&&at>=0&&Number.isSafeInteger(n)&&n>0&&at<=size-n);
 assert(report.semantic_read_operations<limits.semantic_read_operations);report.semantic_read_operations++;
 if(kind==='pdata'){assert.equal(n,12);assert(report.metadata_bytes+n<=limits.metadata_bytes);report.metadata_bytes+=n;}
 else{assert.equal(kind,'body');assert.equal(report.code_bytes,0);assert(n<=limits.code_bytes);report.code_bytes=n;}
 const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,at);report.reads.push({kind,file_offset:at,requested_bytes:n,read_bytes:got,raw_hex:b.subarray(0,got).toString('hex')});assert.equal(got,n,'Short semantic read');return b;}
const visited=new Map();
function entry(index){if(visited.has(index))return visited.get(index);
 let e;if(cached.has(index))e={...cached.get(index),reused:true};
 else{assert(Number.isInteger(index)&&index>=0&&index<pe.entry_count);e={...decode(index,read(pe.exception_file_offset+index*12,12,'pdata')),reused:false};}
 for(const [i,other] of visited)assert(i<index?other.begin<e.begin:other.begin>e.begin,'Encountered order mismatch');
 visited.set(index,e);report.encountered_pdata.push(e);return e;}
try{
 fs.mkdirSync(out);created=true;normal(out);report.script_sha256=fingerprint(self);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);
 report.sha256_before=digest(fd,size);assert.equal(report.sha256_before,pin);
 let lo=0,hi=pe.entry_count;
 while(lo<hi){assert(report.search_steps<limits.search_steps);report.search_steps++;
  const mid=lo+Math.floor((hi-lo)/2);if(entry(mid).begin<target)lo=mid+1;else hi=mid;}
 report.lower_bound_index=lo;const next=lo<pe.entry_count?entry(lo):null;
 if(!next||next.begin!==target){
  const previous=lo>0?entry(lo-1):null;report.previous=previous;report.next=next;report.selected_code_range=null;
  report.boundary_assessment=previous&&target>=previous.begin&&target<previous.end?'no-exact-begin-containing-entry':'no-exact-begin-gap';
  report.status='no-exact-return-dependency-boundary-code-not-read';
 }else{
  const length=next.end-next.begin,m=map(next.begin,length);assert(m.section.characteristics&0x20000000);
  report.exact_boundary={...next,begin_rva:hex(next.begin),end_rva_exclusive:hex(next.end),range_bytes:length,code_file_offset:m.offset};
  if(length>limits.code_bytes){report.selected_code_range=null;report.status='exact-return-dependency-boundary-exceeds-code-budget';}
  else{
   report.selected_code_range={rva:hex(target),end_rva_exclusive:hex(next.end),bytes:length};
   const code=read(m.offset,length,'body');save('324560-exact.bin',code);
   fs.mkdirSync(temp);normal(temp);report.tool_before=fingerprint(tool);assert.equal(report.tool_before,toolPin);
   const sectionRva=target&~0xfff,delta=target-sectionRva,virtualBytes=delta+length,rawBytes=Math.ceil(virtualBytes/512)*512;
   const b=Buffer.alloc(512+rawBytes);assert(b.length<=limits.tiny_pe_bytes);
   const coff=0x84,opt=0x98,sec=0x188;b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);
   b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
   b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawBytes,opt+4);b.writeUInt32LE(sectionRva,opt+20);b.writeBigUInt64LE(base,opt+24);
   b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);
   b.writeUInt32LE(Math.ceil((sectionRva+virtualBytes)/4096)*4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
   b.write('.text',sec,'ascii');b.writeUInt32LE(virtualBytes,sec+8);b.writeUInt32LE(sectionRva,sec+12);b.writeUInt32LE(rawBytes,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);
   const tiny=path.join(temp,'324560-exact.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});
   report.synthetic={path:tiny,bytes:b.length,sha256:hash(b),never_executed:true};
   const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(target))+','+hex(base+BigInt(next.end-1)),tiny];
   report.command_arguments=args;assert(report.dumpbin_calls<limits.dumpbin_calls);report.dumpbin_calls++;
   const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:131072,
    env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
   save('324560-dumpbin.txt',child.stdout||'');save('324560-stderr.txt',child.stderr||'');report.tool_exit=child.status;
   assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
   assert.equal(fingerprint(tiny),report.synthetic.sha256);
   const rows=[];for(const line of (child.stdout||'').split(/\r?\n/)){
    const match=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if(match){rows.push({address:hex(BigInt('0x'+match[1])),bytes:match[2].replace(/\s/g,'').toLowerCase(),instruction:match[3].trim()});continue;}
    const tail=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(tail){assert(rows.length);rows[rows.length-1].bytes+=tail[1].replace(/\s/g,'').toLowerCase();}}
   let cursor=base+BigInt(target);for(const row of rows){assert.equal(BigInt(row.address),cursor);assert(row.instruction&&!/[?]|\(bad\)|invalid/i.test(row.instruction));cursor+=BigInt(row.bytes.length/2);}
   assert.equal(cursor,base+BigInt(next.end));assert.equal(rows.map(r=>r.bytes).join(''),code.toString('hex'));
   save('324560-instructions.json',JSON.stringify(rows,null,2)+'\n');report.instructions=rows.length;report.byte_coverage_verified=true;
   report.calls=rows.filter(r=>/^call\b/.test(r.instruction));report.returns=rows.filter(r=>/^ret\b/.test(r.instruction));
   report.status='exact-return-dependency-body-decoded-review-required';
  }
 }
}catch(e){report.failures.push(String(e));report.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){report.sha256_after=digest(fd,size);assert.equal(report.sha256_after,pin);report.source_stat_stable=stable(initial,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){report.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('Close: '+e);}}}
 if(report.tool_before!==undefined){try{report.tool_after=fingerprint(tool);assert.equal(report.tool_after,toolPin);}catch(e){report.failures.push('Tool final: '+e);}}
 report.source_hash_preserved=report.sha256_before===pin&&report.sha256_after===pin;if(report.failures.length)report.status='failed';
 report.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,exact_boundary:report.exact_boundary,metadata_bytes:report.metadata_bytes,code_bytes:report.code_bytes,
 instructions:report.instructions,source_closed:report.source_closed,source_hash_preserved:report.source_hash_preserved,failures:report.failures}));
if(report.failures.length)process.exitCode=1;
