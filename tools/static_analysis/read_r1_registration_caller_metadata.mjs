// Fixed AddState callers -> export identities. PE metadata only; no code execution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const proofFile='docs/reverse-client/r1-game-add-state/07-AddState-0x180125730-procedure_callers.json',proofHash='c62826910919e92c45d46e8732e3be811e8c9c77ab9d2bfd40cf50f89281843c';
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
normal(self);normal(source);normal(path.join(root,proofFile));
const proofBytes=fs.readFileSync(path.join(root,proofFile));assert.equal(sha(proofBytes),proofHash);
const addresses=JSON.parse(proofBytes).result.structuredContent.normalized_result;
assert.equal(addresses.length,16);assert.equal(new Set(addresses).size,16);
const out=path.join(root,'docs/reverse-client/r1-registration-caller-metadata');normal(path.dirname(out));assert(!fs.existsSync(out));
const fd=fs.openSync(source,'r'),initial=fs.fstatSync(fd);assert.equal(initial.size,11385856);
const r={started_utc:new Date().toISOString(),source,expected_sha256:expected,script_sha256:sha(fs.readFileSync(self)),proof_file:proofFile,proof_sha256:proofHash,code_bytes:0,metadata_bytes:0,subprocesses:0,original_executed:false,targets:addresses.map(address=>({address,names:[]})),failures:[]};
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-at),at);assert(n);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
function read(at,n){assert(Number.isSafeInteger(at)&&n>0&&n<=1048576&&at>=0&&at+n<=initial.size);r.metadata_bytes+=n;assert(r.metadata_bytes<=2097152);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,at),n);return b;}
try{
 r.sha256_before=digest();assert.equal(r.sha256_before,expected);
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert(pe>=64&&pe<4096);
 const c=read(pe,24);assert.equal(c.readUInt32LE(0),0x4550);assert.equal(c.readUInt16LE(4),0x8664);assert.equal(c.readUInt16LE(20),240);
 const opt=read(pe+24,240);assert.equal(opt.readUInt16LE(0),0x20b);const base=opt.readBigUInt64LE(24);assert.equal(base,0x180000000n);
 const count=c.readUInt16LE(6);assert(count>0&&count<=16);
 const sections=Array.from({length:count},(_,i)=>{const b=read(pe+264+i*40,40);return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 function rr(va,n){const matches=sections.filter(s=>va>=s.rva&&va+n<=s.rva+s.size);assert.equal(matches.length,1);const s=matches[0];assert(!(s.flags&0x20000000),'Metadata only');return read(s.offset+va-s.rva,n);}
 const er=opt.readUInt32LE(112),e=rr(er,40),nf=e.readUInt32LE(20),nn=e.readUInt32LE(24);assert(nf>0&&nf<=100000&&nn>0&&nn<=100000);
 const eat=rr(e.readUInt32LE(28),nf*4),names=rr(e.readUInt32LE(32),nn*4),ords=rr(e.readUInt32LE(36),nn*2);
 const byRva=new Map(r.targets.map(t=>[Number(BigInt(t.address)-base),t]));
 function str(va){const a=[];for(let i=0;i<512;i++){const v=rr(va+i,1)[0];if(v===0)return Buffer.from(a).toString('ascii');assert(v<128);a.push(v);}throw Error('Export name exceeds bound');}
 for(let i=0;i<nn;i++){const ordinal=ords.readUInt16LE(i*2);assert(ordinal<nf);const target=byRva.get(eat.readUInt32LE(ordinal*4));if(target)target.names.push({name:str(names.readUInt32LE(i*4)),ordinal:ordinal+e.readUInt32LE(16)});}
 r.metadata_scope='PE headers, export address/name-pointer/ordinal arrays; names read only for the 16 direct caller addresses. No caller bodies or unrelated strings read.';
 r.export_functions=nf;r.export_named_entries=nn;
}catch(e){r.failures.push(String(e));}
finally{try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}fs.closeSync(fd);r.source_closed=true;}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.mkdirSync(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
