// Fixed12-byte Game camera-constructor call anchor; no execution, scan or backend.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const hash=b=>createHash('sha256').update(b).digest('hex');
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proof={file:'docs/task-kills/ghidra-selected/subscriber-constructor-routes.json',sha256:'e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165'};
normal(path.join(root,proof.file));const cached=fs.readFileSync(path.join(root,proof.file));assert.equal(hash(cached),proof.sha256);const item=JSON.parse(cached).functions[4];assert(item.assembly.includes('1802c0c7c: LEA RCX,[RDI + 0x13b0]'));assert(item.assembly.includes('1802c0c83: CALL 0x1802be3b0'));const proofs=[proof];
const out=path.join(root,'docs/reverse-client/r1-game-camera-anchor');normal(path.dirname(out));assert(!fs.existsSync(out));
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const r={started_utc:new Date().toISOString(),source,expected_sha256:expected,proofs,script_sha256:hash(fs.readFileSync(self)),metadata_bytes:0,data_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,values:[],failures:[]};
let fd,initial;
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-at),at);assert(n);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
function read(at,n){assert(at>=0&&n>0&&n<=512&&at+n<=initial.size);r.metadata_bytes+=n;assert(r.metadata_bytes<=4096);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,at),n);return b;}
try{
 fd=fs.openSync(source,'r');initial=fs.fstatSync(fd);assert.equal(initial.size,11385856);r.sha256_before=digest();assert.equal(r.sha256_before,expected);
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert(pe>=64&&pe<4096);const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);assert.equal(coff.readUInt16LE(20),240);const opt=read(pe+24,240);assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),0x180000000n);
 const count=coff.readUInt16LE(6);assert(count>0&&count<=16);const sections=Array.from({length:count},(_,i)=>{const b=read(pe+264+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 const rv=0x2c0c7c,n=12;const ss=sections.filter(s=>rv>=s.rva&&rv+n<=s.rva+s.size);assert.equal(ss.length,1);const section=ss[0];assert(section.flags&0x20000000);const bytes=read(section.offset+rv-section.rva,n);r.code_bytes=12;assert.equal(bytes.subarray(0,7).toString('hex'),'488d8fb0130000');assert.equal(bytes[7],0xe8);const target=rv+12+bytes.readInt32LE(8);assert.equal(target,0x2be3b0);r.values=[{rva:'0x2c0c7c',bytes:12,raw_hex:bytes.toString('hex'),inline_offset:'0x13b0',target_rva:'0x2be3b0',target_va:'0x1802be3b0'}];
 r.limitations=['Current original12-byte direct-call anchor supports the historical constructor entry; no constructor body or vtable target inferred.','No loaded object validity or gGameEngine publication timing proved.'];
}catch(e){r.failures.push(String(e));}
finally{if(fd!==undefined){try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}finally{fs.closeSync(fd);r.source_closed=true;}}}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.mkdirSync(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
