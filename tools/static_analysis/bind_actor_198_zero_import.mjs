// One exact OFT slot identified by the archived 2DDB14 jump. No code or IAT payload read.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const root='C:/Users/zhuzi/Documents/Grim-Dynasty';
assert.equal(path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..').toLowerCase(),path.resolve(root).toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632,expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const sources=[
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-engine-root-helper-imports/metadata.json','0b81751351efeaeb5bc8cd5103035887b5ffbd4798d29edf2be50c697f84d369'],
 ['docs/object-lifecycle/offline-actor-198-array-helpers/2ddb14-instructions.json','918cc3a51ab772c2b55fe9455c6ae90170bc9d1dc23a9dc7e6dcfb7453cfb08e']
];
const out=path.join(root,'docs/object-lifecycle/offline-actor-198-zero-import');
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function fingerprint(fd){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<size;){const n=fs.readSync(fd,b,0,Math.min(b.length,size-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
plain(path.dirname(out));assert(!fs.existsSync(out));
const proof=sources.map(([p,h])=>{const f=path.join(root,p);plain(f,true);assert(fs.statSync(f).size<262144);const b=fs.readFileSync(f);assert.equal(hash(b),h);return JSON.parse(b);});
const pe=proof[0].pe;assert(proof[0].source_hash_preserved);assert.equal(proof[0].sha256_after,expected);assert(proof[1].source_hash_preserved);assert.equal(proof[1].sha256_after,expected);
assert.equal(proof[2].length,1);assert.equal(proof[2][0].address,'0x1802ddb14');assert.equal(proof[2][0].bytes,'ff25564c0200');assert.equal(0x2ddb14+6+Buffer.from(proof[2][0].bytes,'hex').readInt32LE(2),0x302770);
const old=proof[1].bindings.find(b=>b.module==='VCRUNTIME140.dll'&&b.name==='_CxxThrowException');assert(old);assert.equal(old.descriptor_index,13);assert.equal(old.descriptor_rva,'0x41569c');assert.equal(old.original_first_thunk_rva,'0x415eb0');assert.equal(old.first_thunk_rva,'0x3026f8');assert.equal((0x302770-0x3026f8)/8,15);
const report={schema:'grim-dynasty-actor-198-zero-import-v1',status:'started',started_utc:new Date().toISOString(),original,expected_sha256:expected,sources:sources.map(([path,sha256])=>({path,sha256})),
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,subprocesses:0,code_bytes:0,iat_payload_bytes:0,metadata_limit:256,metadata_bytes:0,reads:[],
 limitations:['Only descriptor 13, its module name, OFT index 15 and that symbol/hint are read. No complete import table or headers scanned.',
 'This binds the import name; no CRT implementation or loader-resolved target is executed or read.',
 'Before/after full-file hashes use 64KiB buffers, separate from semantic reads, and do not provide an atomic snapshot.']};
fs.mkdirSync(out);let fd,stat;const failures=[];
function read(rva,n,kind){assert(n>0&&n<=20);const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);const at=ss[0].raw_offset+rva-ss[0].rva;assert(at>=0&&at+n<=size);assert(report.metadata_bytes+n<=256);const b=Buffer.alloc(n);let done=0;while(done<n){const got=fs.readSync(fd,b,done,n-done,at+done);assert(got>0);done+=got;}report.metadata_bytes+=n;report.reads.push({kind,rva:hex(rva),file_offset:at,bytes:n,raw_hex:b.toString('hex')});return b;}
function ascii(rva,kind){let value='';for(let n=0;n<128;n+=16){const b=read(rva+n,16,kind),nul=b.indexOf(0),piece=nul<0?b:b.subarray(0,nul);assert([...piece].every(x=>x>0&&x<128));value+=piece.toString('ascii');if(nul>=0)return value;}assert.fail('Bounded symbol did not terminate');}
try{
 plain(original,true);fd=fs.openSync(original,'r');stat=fs.fstatSync(fd);assert.equal(stat.size,size);report.sha256_before=fingerprint(fd);assert.equal(report.sha256_before,expected);
 const desc=read(0x41569c,20,'descriptor');assert.equal(desc.readUInt32LE(),0x415eb0);assert.equal(desc.readUInt32LE(16),0x3026f8);
 const module=ascii(desc.readUInt32LE(12),'module-name'),thunk=read(0x415f28,8,'original-thunk').readBigUInt64LE();assert(thunk>0n&&thunk<BigInt(pe.image_size));
 const hint=read(Number(thunk),2,'hint').readUInt16LE(),name=ascii(Number(thunk)+2,'symbol-name');
 report.binding={module,name,hint,descriptor_index:13,descriptor_rva:'0x41569c',first_thunk_rva:'0x3026f8',original_first_thunk_rva:'0x415eb0',thunk_index:15,original_thunk_rva:'0x415f28',hint_name_rva:hex(thunk),iat_rva:'0x302770',caller_rva:'0x2ddb14'};
 assert.equal(module,'VCRUNTIME140.dll');assert.equal(name,'memset');report.status='exact-memset-import-binding-verified';
}catch(e){failures.push(String(e));}
finally{if(fd!==undefined){try{if(stat&&stat.size===size){report.sha256_after=fingerprint(fd);assert.equal(report.sha256_after,expected);const s=fs.fstatSync(fd);report.source_stat_stable=s.size===stat.size&&s.ino===stat.ino&&s.dev===stat.dev&&s.mtimeMs===stat.mtimeMs&&s.ctimeMs===stat.ctimeMs;assert(report.source_stat_stable);}}catch(e){failures.push(String(e));}finally{fs.closeSync(fd);}}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();plain(out);fs.writeFileSync(path.join(out,'metadata.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});}
console.log(JSON.stringify({status:report.status,binding:report.binding,metadata_bytes:report.metadata_bytes,original_unchanged:report.source_hash_preserved,failures}));if(failures.length)process.exitCode=1;
