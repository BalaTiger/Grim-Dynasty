// Fixed exported entry3A740: bounded runtime-function lookup and <=8B candidate.
// No original module execution, callee following, unwind-body read or fallback scan.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632,pin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-object-id-getter'),temp=path.join(root,'.tooling/offline-object-id-getter-temp');
const start=0x3a740,base=0x180000000n,hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const sources=[
 ['docs/object-lifecycle/offline-item-identity-import/inspection.json','c0b80a81f7094c25816017d8e4adaeeff74c835f3049daabd0842b9563740b59'],
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b']
];
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync.native(c).toLowerCase(),c.toLowerCase());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved');}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([rel,sha])=>{const p=path.join(root,rel);normal(p,true);assert(fs.statSync(p).size<262144);const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
const binding=proofs[0],old=proofs[1];assert.equal(binding.status,'fixed-item-id-import-export-and-caller-copy-verified');assert(binding.source_hash_preserved);assert.equal(binding.failures.length,0);
assert.equal(binding.export_binding.name,'?GetObjectId@Object@GAME@@QEBAIXZ');assert.equal(binding.export_binding.target_rva,'0x3a740');assert.equal(binding.export_binding.ordinal,3190);
const ep=binding.files.find(x=>x.name==='Engine');assert(ep.source_closed&&ep.source_stat_stable&&ep.source_hash_preserved);assert.equal(ep.sha256_after,pin);
assert.equal(old.status,'exact-actor-export-and-boundary-verified');assert(old.source_hash_preserved&&old.source_stat_stable);assert.equal(old.sha256_after,pin);assert.equal(old.original,original);assert.equal(old.pe.image_base,hex(base));
const pe=old.pe;assert.deepEqual(pe.sections,ep.pe.sections);assert.equal(pe.exception_rva,'0x424000');assert.equal(pe.exception_bytes,146520);assert.equal(pe.exception_file_offset,4317696);assert.equal(pe.entry_count,12210);
const er=old.reads.find(x=>x.kind==='exception-directory');assert(er&&er.bytes===8);const eb=Buffer.from(er.hex,'hex');assert.equal(eb.readUInt32LE(),0x424000);assert.equal(eb.readUInt32LE(4),146520);
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);const offset=ss[0].raw_offset+rva-ss[0].rva;assert(offset>=0&&offset<=size-n);return {section:ss[0],offset};}
assert.equal(map(0x424000,146520).offset,4317696);assert.equal(map(0x424000,146520).section.name,'.pdata');
const cache=new Map();for(const e of old.encountered_pdata){const b=Buffer.from(e.raw_hex,'hex');assert.equal(b.length,12);assert(e.index>=0&&e.index<12210&&!cache.has(e.index));assert.equal(b.readUInt32LE(),e.begin);assert.equal(b.readUInt32LE(4),e.end);assert.equal(b.readUInt32LE(8),e.unwind);const read=old.reads.find(x=>x.kind==='pdata-entry'&&x.file_offset===4317696+e.index*12);assert(read&&read.hex===e.raw_hex);cache.set(e.index,e);}
const r={schema:'object-id-getter-entry-v1',status:'started',started_utc:new Date().toISOString(),original,expected_sha256:pin,sources,entry_rva:hex(start),
 limits:{metadata_bytes:408,metadata_reads:34,binary_search_steps:32,code_bytes:8,dumpbin_calls:1,tiny_pe_bytes:8192,hash_buffer:65536},
 metadata_bytes:0,code_bytes:0,reads:[],cached_entries:[],encountered_pdata:[],failures:[],original_loaded_or_executed:false,original_path_given_to_dumpbin:false,unwind_body_read:false,callees_followed:0};
let fd,initial,created=false,newReads=0;function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function entry(index){assert(index>=0&&index<12210);let e=cache.get(index);if(e){if(!r.cached_entries.includes(index))r.cached_entries.push(index);}else{assert(++newReads<=34);assert(r.metadata_bytes+12<=408);r.metadata_bytes+=12;const b=Buffer.alloc(12),at=4317696+index*12,got=fs.readSync(fd,b,0,12,at);r.reads.push({kind:'pdata',file_offset:at,bytes:got,hex:b.subarray(0,got).toString('hex')});assert.equal(got,12);e={index,begin:b.readUInt32LE(),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8),raw_hex:b.toString('hex')};assert(e.begin<e.end);assert(map(e.begin,e.end-e.begin).section.characteristics&0x20000000);cache.set(index,e);}if(!r.encountered_pdata.some(x=>x.index===index))r.encountered_pdata.push(e);return e;}
try{
 fs.mkdirSync(out);fs.mkdirSync(temp);created=true;r.script_sha256=fingerprint(self);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.equal(initial.size,size);r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 let lo=0,hi=12210,steps=0;while(lo<hi){assert(++steps<=32);const mid=Math.floor((lo+hi)/2);if(entry(mid).begin<start)lo=mid+1;else hi=mid;}r.search_steps=steps;
 const next=lo<12210?entry(lo):null,previous=lo>0?entry(lo-1):null;r.next=next;r.previous=previous;
 let length;if(next&&next.begin===start){r.exact_boundary=next;length=Math.min(8,next.end-start);r.boundary_assessment='exact-runtime-function-start-capped-entry';}
 else{assert(previous&&next&&previous.end<=start&&start+8<=next.begin,'No supported gap for the fixed8B candidate');length=8;r.boundary_assessment='entry-candidate-in-metadata-gap-not-whole-function';}
 assert(length>0&&length<=8);const mapped=map(start,length);assert(mapped.section.characteristics&0x20000000);const code=Buffer.alloc(length),got=fs.readSync(fd,code,0,length,mapped.offset);r.code_bytes=got;r.reads.push({kind:'entry-code',rva:hex(start),file_offset:mapped.offset,bytes:got,hex:code.subarray(0,got).toString('hex')});assert.equal(got,length);save('03a740-entry.bin',code);
 const page=start&~0xfff,delta=start-page,rawSize=Math.ceil((delta+length)/512)*512;
 const b=Buffer.alloc(512+rawSize),coff=0x84,opt=0x98,sec=0x188;assert(b.length<=8192);
 b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawSize,opt+4);b.writeUInt32LE(page,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(page+0x1000,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
 b.write('.text',sec,'ascii');b.writeUInt32LE(delta+length,sec+8);b.writeUInt32LE(page,sec+12);b.writeUInt32LE(rawSize,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);
 const tiny=path.join(temp,'03a740-entry.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});r.synthetic={bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(start))+','+hex(base+BigInt(start+length-1)),tiny];r.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:32768,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save('03a740-dumpbin.txt',child.stdout||'');save('03a740-stderr.txt',child.stderr||'');r.tool_exit=child.status;assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),r.synthetic.sha256);
 const rows=[];for(const line of (child.stdout||'').split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);if(m){rows.push({address:'0x'+m[1].toLowerCase().replace(/^0+/,''),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
 let cursor=base+BigInt(start);for(const row of rows){assert.equal(BigInt(row.address),cursor);assert(row.instruction&&!/[?]|\(bad\)|invalid/i.test(row.instruction));cursor+=BigInt(row.bytes.length/2);}assert.equal(cursor,base+BigInt(start+length));assert.equal(rows.map(x=>x.bytes).join(''),code.toString('hex'));
 r.instructions=rows.length;r.byte_coverage_verified=true;r.returns=rows.filter(x=>/^ret\b/.test(x.instruction));r.calls_or_branches=rows.filter(x=>/^(call|j[a-z]+)\b/.test(x.instruction));save('03a740-instructions.json',JSON.stringify(rows,null,2)+'\n');r.status='fixed-object-id-entry-decoded-manual-review-required';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);r.source_stat_stable=s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}catch(e){r.failures.push('Final: '+e);}finally{fs.closeSync(fd);r.source_closed=true;}}
 try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool: '+e);}r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,metadata_bytes:r.metadata_bytes,code_bytes:r.code_bytes,boundary:r.boundary_assessment,instructions:r.instructions,returns:r.returns,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
