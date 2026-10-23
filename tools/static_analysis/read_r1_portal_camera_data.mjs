// Fixed portal references only: one IAT slot/name owner and one float constant.
// Import descriptors are bounded ownership metadata, not a code/name scan.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proofs=[
 {file:'docs/reverse-client/r1-waypoint-execution/14-Waypoint-0x18008d9e0-procedure_assembly.json',sha256:'d37b38f06d40e9e26b3454ad422c9ba17cbcea827d308219d1949b7d755056ac'},
 {file:'docs/reverse-client/r1-portal-dependencies/08-PortalDependency-0x1805dc3f8-procedure_assembly.json',sha256:'80992d4146102200984d01702c000d3c472e56899e89037525a14ad0cfe57f12'}
];
const texts=proofs.map(p=>{const f=path.join(root,p.file);normal(f);assert(fs.statSync(f).size<100000);const b=fs.readFileSync(f);assert.equal(hash(b),p.sha256);return JSON.parse(b).result.structuredContent.normalized_result;});
assert(texts[0].includes('0x18008ddf8: SUBSS XMM0, dword ptr [0x1807a80bc]'));assert.equal(texts[1].trim(),'0x1805dc3f8: JMP qword ptr [0x18060b8f0]');
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const out=path.join(root,'docs/reverse-client/r1-portal-camera-data');normal(path.dirname(out));assert(!fs.existsSync(out));
const r={started_utc:new Date().toISOString(),source,expected_sha256:expected,proofs,script_sha256:hash(fs.readFileSync(self)),metadata_limit:8192,metadata_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,reads:[],failures:[]};let fd,initial;
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-at),at);assert(n);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
function read(at,n,kind){assert(at>=0&&n>0&&n<=4096&&at+n<=initial.size);r.metadata_bytes+=n;assert(r.metadata_bytes<=r.metadata_limit);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,at),n);r.reads.push({file_offset:at,bytes:n,kind,raw_hex:b.toString('hex')});return b;}
try{
 fd=fs.openSync(source,'r');initial=fs.fstatSync(fd);assert.equal(initial.size,11385856);r.sha256_before=digest();assert.equal(r.sha256_before,expected);
 const dos=read(0,64,'DOS');assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert(pe>=64&&pe<4096);const coff=read(pe,24,'COFF');assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);assert.equal(coff.readUInt16LE(20),240);const opt=read(pe+24,240,'optional-header');assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),0x180000000n);
 const count=coff.readUInt16LE(6);assert(count>0&&count<=16);const sections=Array.from({length:count},(_,i)=>{const b=read(pe+264+i*40,40,'section-header');return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 function data(rva,n,kind){const ss=sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.size);assert.equal(ss.length,1);const s=ss[0];assert(!(s.flags&0x20000000));return read(s.offset+rva-s.rva,n,kind);}
 function string(rva,limit,kind){const chars=[];for(let n=0;n<limit;n++){const c=data(rva+n,1,kind)[0];if(!c)return Buffer.from(chars).toString('ascii');assert(c>=32&&c<127);chars.push(c);}throw Error('Unterminated selected string');}
 const importRva=opt.readUInt32LE(120),importSize=opt.readUInt32LE(124);assert(importRva>0&&importSize>=20&&importSize<=4096);const descs=[];let terminated=false;
 for(let offset=0;offset+20<=importSize;offset+=20){const d=data(importRva+offset,20,'import-descriptor-ownership');if(d.every(v=>v===0)){terminated=true;break;}descs.push({rva:importRva+offset,oft:d.readUInt32LE(0),name:d.readUInt32LE(12),iat:d.readUInt32LE(16)});}assert(terminated);
 const slot=0x60b8f0,candidates=descs.filter(d=>d.iat>0&&d.iat<=slot).sort((a,b)=>b.iat-a.iat);assert(candidates.length);const owner=candidates[0];assert.equal(descs.filter(d=>d.iat===owner.iat).length,1);assert(owner.oft>0);const index=(slot-owner.iat)/8;assert(Number.isInteger(index)&&index>=0&&index<256);
 const prefix=data(owner.oft,(index+1)*8,'selected-OFT-nonzero-prefix');for(let n=0;n<=index;n++)assert.notEqual(prefix.readBigUInt64LE(n*8),0n);
 const lookup=prefix.readBigUInt64LE(index*8);assert(lookup>0n&&lookup<0x100000000n);const iat=data(slot,8,'exact-IAT').readBigUInt64LE();assert.equal(iat,lookup);const nameRva=Number(lookup);const hint=data(nameRva,2,'selected-import-hint').readUInt16LE();
 r.binding={iat_rva:hex(slot),owner_descriptor_rva:hex(owner.rva),first_thunk_rva:hex(owner.iat),original_first_thunk_rva:hex(owner.oft),index,dll:string(owner.name,256,'selected-DLL-name'),hint,name:string(nameRva+2,256,'selected-import-name')};
 const c=data(0x7a80bc,4,'exact-camera-offset');r.constant={rva:'0x7a80bc',raw_hex:c.toString('hex'),bits:hex(c.readUInt32LE()),float32:c.readFloatLE()};assert(Number.isFinite(r.constant.float32));
 r.limitations=['On-disk import/constant evidence only; loaded IAT mutation and CRT floating-point environment unobserved.','No system DLL loaded, no function body scanned, no camera instance or setter identity inferred.'];
}catch(e){r.failures.push(String(e));}
finally{if(fd!==undefined){try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}finally{fs.closeSync(fd);r.source_closed=true;}}}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.mkdirSync(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({status:r.status,binding:r.binding,constant:r.constant,metadata_bytes:r.metadata_bytes,sha256_before:r.sha256_before,sha256_after:r.sha256_after,failures:r.failures}));if(r.failures.length)process.exitCode=1;
