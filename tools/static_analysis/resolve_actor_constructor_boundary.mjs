// Fixed Actor export and runtime-function metadata only. Never loads either DLL.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const root='C:/Users/zhuzi/Documents/Grim-Dynasty';
assert.equal(path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..').toLowerCase(),path.resolve(root).toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153',size=4485632;
const symbol='??0Actor@GAME@@QEAA@XZ',out=path.join(root,'docs/object-lifecycle/offline-actor-constructor-boundary');
const sources=[
 ['docs/object-lifecycle/offline-class-registration-exports/discovery.json','0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
 ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());const d=path.dirname(c);if(d===c)return;c=d;first=false;}}
function fingerprint(fd){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<size;){const n=fs.readSync(fd,b,0,Math.min(b.length,size-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
plain(path.dirname(out));assert(!fs.existsSync(out));
const proofs=sources.map(([p,h])=>{const f=path.join(root,p);plain(f,true);assert(fs.statSync(f).size<524288);const b=fs.readFileSync(f);assert.equal(hash(b),h);return JSON.parse(b);});
const source=proofs[0].files.find(f=>f.target===original);assert(source&&source.source_hash_preserved);assert.equal(source.sha256_before,expected);assert.equal(source.sha256_after,expected);
const binding=proofs[1].normal_imports.find(b=>b.iat_va==='0x180609700');assert.equal(binding.dll,'Engine.dll');assert.equal(binding.name,symbol);assert.equal(binding.source_instruction_va,'0x1803236b3');
const pe=source.pe;assert.equal(pe.named_export_count,6283);assert.equal(pe.function_count,6283);assert.equal(source.preferred_image_base,'0x180000000');
const report={schema:'grim-dynasty-actor-export-boundary-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,symbol,
 sources:sources.map(([path,sha256])=>({path,sha256})),source_binding:{iat:binding.iat_va,caller:binding.source_instruction_va,dll:binding.dll,name:binding.name,hint:binding.hint},
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,subprocesses:0,metadata_limit:16384,metadata_bytes:0,reads:[],search_step_limit:32,
 pe:{image_base:source.preferred_image_base,image_size:pe.size_of_image,sections:pe.sections},
 limitations:['Exact named export lookup and exact .pdata lookup only; no code, unwind records, tables in full or callees read.',
 'Binary search relies on PE sorted-table contract; only encountered entries checked. Hash passes use 64KiB buffers and are separate from semantic metadata reads.',
 'Before/after hash and stat stability is not an atomic snapshot; runtime-function boundary alone is not the complete logical constructor or EH graph.']};
fs.mkdirSync(out);let fd,stat;const failures=[];
function read(at,n,kind){assert(Number.isSafeInteger(at)&&at>=0&&n>0&&at+n<=size);assert(report.metadata_bytes+n<=report.metadata_limit);report.metadata_bytes+=n;
 const b=Buffer.alloc(n);let done=0;while(done<n){const got=fs.readSync(fd,b,done,n-done,at+done);assert(got>0);done+=got;}
 report.reads.push({kind,file_offset:at,bytes:n,hex:b.toString('hex')});return b;}
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);return {section:ss[0],offset:ss[0].raw_offset+rva-ss[0].rva};}
function rread(rva,n,kind){return read(map(rva,n).offset,n,kind);}
try{
 plain(original,true);fd=fs.openSync(original,'r');stat=fs.fstatSync(fd);assert.equal(stat.size,size);report.sha256_before=fingerprint(fd);assert.equal(report.sha256_before,expected);
 assert.equal(rread(Number(pe.export_directory_rva),40,'export-directory').toString('hex'),pe.export_directory_bytes);
 const names=new Map();
 function name(i){if(names.has(i))return names.get(i);assert(i>=0&&i<6283);const rva=rread(Number(pe.name_pointer_table_rva)+i*4,4,'name-pointer').readUInt32LE();const bs=[];let done=false;
  for(let n=0;n<512;n+=16){const b=rread(rva+n,16,'name-chunk'),z=b.indexOf(0);bs.push(z<0?b:b.subarray(0,z));if(z>=0){done=true;break;}}
  assert(done);const b=Buffer.concat(bs);assert([...b].every(x=>x>0&&x<128));const text=b.toString('ascii');for(const[j,o]of names)assert(j<i?o.text<=text:o.text>=text);const v={index:i,rva:hex(rva),text};names.set(i,v);return v;}
 let lo=0,hi=6283,steps=0;while(lo<hi){assert(steps++<32);const mid=lo+Math.floor((hi-lo)/2);if(name(mid).text<symbol)lo=mid+1;else hi=mid;}
 assert(lo<6283);assert.equal(name(lo).text,symbol);const ordinalIndex=rread(Number(pe.name_ordinal_table_rva)+lo*2,2,'ordinal').readUInt16LE();assert(ordinalIndex<6283);
 const target=rread(Number(pe.address_table_rva)+ordinalIndex*4,4,'export-address').readUInt32LE();report.export={...name(lo),ordinal_index:ordinalIndex,ordinal:ordinalIndex+pe.ordinal_base,target_rva:hex(target),search_steps:steps};report.encountered_names=[...names.values()];
 assert(!(target>=Number(pe.export_directory_rva)&&target<Number(pe.export_directory_rva)+pe.export_directory_size),'Forwarder requires separately scoped investigation');assert(map(target,1).section.characteristics&0x20000000);
 const dir=read(pe.pe_header_file_offset+24+136,8,'exception-directory');const prva=dir.readUInt32LE(),pbytes=dir.readUInt32LE(4);assert(pbytes>0&&pbytes%12===0);const pd=map(prva,pbytes);assert.equal(pd.section.name,'.pdata');
 Object.assign(report.pe,{exception_rva:hex(prva),exception_bytes:pbytes,exception_file_offset:pd.offset,entry_count:pbytes/12});
 const entries=new Map();function entry(i){if(entries.has(i))return entries.get(i);assert(i>=0&&i<pbytes/12);const b=read(pd.offset+i*12,12,'pdata-entry');const e={index:i,begin:b.readUInt32LE(),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8),raw_hex:b.toString('hex')};assert(e.begin<e.end);for(const[j,o]of entries)assert(j<i?o.begin<=e.begin:o.begin>=e.begin);entries.set(i,e);return e;}
 lo=0;hi=pbytes/12;steps=0;while(lo<hi){assert(steps++<32);const mid=lo+Math.floor((hi-lo)/2);if(entry(mid).begin<target)lo=mid+1;else hi=mid;}
 const e=lo<pbytes/12?entry(lo):null;report.pdata_search_steps=steps;report.encountered_pdata=[...entries.values()];report.boundary=e;
 if(e&&e.begin===target){const m=map(e.begin,e.end-e.begin);assert(m.section.characteristics&0x20000000);report.code_file_offset=m.offset;report.code_bytes=e.end-e.begin;report.status='exact-actor-export-and-boundary-verified';}else{report.status='exact-export-without-exact-runtime-function';}
}catch(e){failures.push(String(e));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){report.sha256_after=fingerprint(fd);assert.equal(report.sha256_after,expected);const s=fs.fstatSync(fd);report.source_stat_stable=s.size===stat.size&&s.ino===stat.ino&&s.dev===stat.dev&&s.mtimeMs===stat.mtimeMs&&s.ctimeMs===stat.ctimeMs;assert(report.source_stat_stable);}}catch(e){failures.push(String(e));}finally{fs.closeSync(fd);}}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();plain(out);fs.writeFileSync(path.join(out,'metadata.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});
}
console.log(JSON.stringify({status:report.status,export:report.export,boundary:report.boundary,metadata_bytes:report.metadata_bytes,code_bytes:report.code_bytes,original_unchanged:report.source_hash_preserved,failures}));if(failures.length)process.exitCode=1;
