// Fixed data references only; no code execution, scan, subprocess or backend.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const hash=b=>createHash('sha256').update(b).digest('hex');
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proofs=[
 {file:'docs/reverse-client/r1-movement-action-path/08-MoveAction-0x180082b10-procedure_assembly.json',sha256:'0e14d2fbc26f938e2f6cb3c63ce058b55e33b35beb05ff20f5832a5319a5fe36'},
 {file:'docs/reverse-client/r1-action-executor/11-ActionExecutor-0x180089ac0-procedure_assembly.json',sha256:'ecb175a3836338682538fe87463511ec9059b404c733c871f57d5bca0d5adbe1'}];
const asms=proofs.map(p=>{const f=path.join(root,p.file);normal(f);assert(fs.statSync(f).size<100000);const b=fs.readFileSync(f);assert.equal(hash(b),p.sha256);return JSON.parse(b).result.structuredContent.normalized_result;});
assert(asms[0].includes('0x180082b7a: LEA RAX, [0x1806e54c0]'));assert(asms[0].includes('0x180082b81: MOV qword ptr [R14], RAX'));
assert(asms[1].includes('0x180089b17: CALL qword ptr [RAX + 0x8]'));
const selected=[{rva:0x6e54c0+0x8,n:8,kind:'code-pointer',owner:'MoveToAction',slot:'0x8'}];
const out=path.join(root,'docs/reverse-client/r1-move-start-slot');normal(path.dirname(out));assert(!fs.existsSync(out));
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const r={started_utc:new Date().toISOString(),source,expected_sha256:expected,proofs,script_sha256:hash(fs.readFileSync(self)),metadata_bytes:0,data_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,values:[],failures:[]};
let fd,initial;
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-at),at);assert(n);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
function read(at,n){assert(at>=0&&n>0&&n<=512&&at+n<=initial.size);r.metadata_bytes+=n;assert(r.metadata_bytes<=4096);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,at),n);return b;}
try{
 fd=fs.openSync(source,'r');initial=fs.fstatSync(fd);assert.equal(initial.size,11385856);r.sha256_before=digest();assert.equal(r.sha256_before,expected);
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert(pe>=64&&pe<4096);const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);assert.equal(coff.readUInt16LE(20),240);const opt=read(pe+24,240);assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),0x180000000n);
 const count=coff.readUInt16LE(6);assert(count>0&&count<=16);const sections=Array.from({length:count},(_,i)=>{const b=read(pe+264+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 for(const v of selected){const ss=sections.filter(s=>v.rva>=s.rva&&v.rva+v.n<=s.rva+s.size);assert.equal(ss.length,1);const s=ss[0];assert.equal(s.name,'.rdata');assert(!(s.flags&0xa0000000));const b=read(s.offset+v.rva-s.rva,v.n);r.data_bytes+=v.n;const x={rva:'0x'+v.rva.toString(16),raw_hex:b.toString('hex'),bytes:v.n,kind:v.kind};if(v.kind==='code-pointer'){const va=b.readBigUInt64LE(0);assert(va>=0x180000000n&&va<0x200000000n);const rv=Number(va-0x180000000n);assert.equal(sections.filter(s=>rv>=s.rva&&rv<s.rva+s.size&&(s.flags&0x20000000)).length,1);x.target_va='0x'+va.toString(16);x.owner=v.owner;x.slot=v.slot;x.target_body_read=false;}else if(v.kind==='float32'){x.bits='0x'+b.readUInt32LE(0).toString(16).padStart(8,'0');x.value=b.readFloatLE(0);assert(Number.isFinite(x.value));}else{assert.equal(b[v.n-1],0);assert([...b.subarray(0,-1)].every(c=>c>=32&&c<127));x.value=b.subarray(0,-1).toString('ascii');}r.values.push(x);}
 assert.equal(r.data_bytes,8);r.limitations=['On-disk constants and registered vtable slots, not loaded-state or active-instance observations.','Only the directly called MoveToAction+8 slot; no function body or neighboring data read.'];
}catch(e){r.failures.push(String(e));}
finally{if(fd!==undefined){try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}finally{fs.closeSync(fd);r.source_closed=true;}}}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.mkdirSync(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
