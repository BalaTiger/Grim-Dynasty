// Fixed data references only; no code execution, scan, subprocess or backend.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const hash=b=>createHash('sha256').update(b).digest('hex');
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proofs=[
 {file:'docs/reverse-client/r1-player-move-dispatch/04-MoveDispatch-0x180163490-procedure_assembly.json',sha256:'083eade1979ad63423a26e516f3681e1e3b2dc213586d2d6e747f85eddea9e1a'},
 {file:'docs/reverse-client/r1-player-move-entries/08-PlayerMove-0x1801662d0-procedure_assembly.json',sha256:'c773f187aeb38d62c58ceda6547806708fa48a69d8651070057bcd300c59345f'}];
let asm='';for(const p of proofs){const f=path.join(root,p.file);normal(f);assert(fs.statSync(f).size<100000);const b=fs.readFileSync(f);if(p.sha256)assert.equal(hash(b),p.sha256);else p.sha256=hash(b);asm+=JSON.parse(b).result.structuredContent.normalized_result;}
const selected=[{rva:0x7a8050,n:4,kind:'float32'},{rva:0x7a8054,n:4,kind:'float32'},{rva:0x7a8024,n:4,kind:'float32'},{rva:0x7a7fa0,n:4,kind:'float32'},{rva:0x675168,n:7,kind:'ascii'},{rva:0x67522c,n:7,kind:'ascii'},{rva:0x675268,n:7,kind:'ascii'}];
for(const v of selected)assert(asm.includes('[0x'+(0x180000000+v.rva).toString(16)+']'));
const out=path.join(root,'docs/reverse-client/r1-move-constants');normal(path.dirname(out));assert(!fs.existsSync(out));
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const r={started_utc:new Date().toISOString(),source,expected_sha256:expected,proofs,script_sha256:hash(fs.readFileSync(self)),metadata_bytes:0,data_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,values:[],failures:[]};
let fd,initial;
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-at),at);assert(n);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
function read(at,n){assert(at>=0&&n>0&&n<=512&&at+n<=initial.size);r.metadata_bytes+=n;assert(r.metadata_bytes<=4096);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,at),n);return b;}
try{
 fd=fs.openSync(source,'r');initial=fs.fstatSync(fd);assert.equal(initial.size,11385856);r.sha256_before=digest();assert.equal(r.sha256_before,expected);
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert(pe>=64&&pe<4096);const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);assert.equal(coff.readUInt16LE(20),240);const opt=read(pe+24,240);assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),0x180000000n);
 const count=coff.readUInt16LE(6);assert(count>0&&count<=16);const sections=Array.from({length:count},(_,i)=>{const b=read(pe+264+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 for(const v of selected){const ss=sections.filter(s=>v.rva>=s.rva&&v.rva+v.n<=s.rva+s.size);assert.equal(ss.length,1);const s=ss[0];assert.equal(s.name,'.rdata');assert(!(s.flags&0xa0000000));const b=read(s.offset+v.rva-s.rva,v.n);r.data_bytes+=v.n;const x={rva:'0x'+v.rva.toString(16),raw_hex:b.toString('hex'),bytes:v.n,kind:v.kind};if(v.kind==='float32'){x.bits='0x'+b.readUInt32LE(0).toString(16).padStart(8,'0');x.value=b.readFloatLE(0);assert(Number.isFinite(x.value));}else{assert.equal(b[v.n-1],0);assert([...b.subarray(0,-1)].every(c=>c>=32&&c<127));x.value=b.subarray(0,-1).toString('ascii');}r.values.push(x);}
 assert.equal(r.data_bytes,37);r.limitations=['On-disk constants and strings only; not loaded-state invariants.','Only seven exact direct references. No original code or surrounding data scanned.'];
}catch(e){r.failures.push(String(e));}
finally{if(fd!==undefined){try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}finally{fs.closeSync(fd);r.source_closed=true;}}}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.mkdirSync(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
