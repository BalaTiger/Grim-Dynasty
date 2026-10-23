// Fixed read-only PE metadata probe. No code scan, target execution or subprocess.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(process.argv.length,2);
const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
const proofPath=path.join(root,'docs/phase-1/boundary-audit/module-boundaries.json');
const proof=fs.readFileSync(proofPath);assert.equal(sha(proof),'d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7');
const inv=JSON.parse(proof),symbol='?HandleActionFromMouse@ControllerPlayer@GAME@@QEAA_N_N000AEBVWorldVec3@2@AEAIPEA_N@Z';
const out=path.join(root,'docs/reverse-client/r1-mouse-link-metadata');
assert.ok(!fs.existsSync(out),'Preserve earlier output');
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){const s=fs.lstatSync(q);assert.ok(!s.isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
normal(path.dirname(out));normal(proofPath);
const report={started_utc:new Date().toISOString(),source_proof_sha256:sha(proof),script_sha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),code_bytes:0,subprocesses:0,originals_loaded_or_executed:false,modules:[],failures:[]};
fs.mkdirSync(out);
for(const rel of ['x64/Grim Dawn.exe','x64/Game.dll']){
 const src=inv.find(x=>x.relativePath===rel);assert.ok(src);
 const source=path.join('D:/Steam/steamapps/common/Grim Dawn',rel);normal(source);
 const fd=fs.openSync(source,'r'),initial=fs.fstatSync(fd),m={source,expected_sha256:src.sha256,metadata_bytes:0,reads:0,targets:[]};report.modules.push(m);
 function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-at),at);assert.ok(n);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
 function read(at,n){assert.ok(Number.isSafeInteger(at)&&at>=0&&n>0&&n<=512&&at+n<=initial.size);m.metadata_bytes+=n;assert.ok(m.metadata_bytes<=24576&&++m.reads<=2048);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,at),n);return b;}
 try{
  assert.equal(initial.size,src.bytes);m.sha256_before=digest();assert.equal(m.sha256_before,src.sha256);
  const d=read(0,64);assert.equal(d.readUInt16LE(0),0x5a4d);const p=d.readUInt32LE(60);assert.ok(p>=64&&p<=4000);
  const c=read(p,24);assert.equal(c.readUInt32LE(0),0x4550);assert.equal(c.readUInt16LE(4),0x8664);assert.equal(c.readUInt16LE(20),240);
  const o=read(p+24,240);assert.equal(o.readUInt16LE(0),0x20b);assert.ok(o.readUInt32LE(108)>=4);const base=o.readBigUInt64LE(24);m.image_base=hex(base);
  const count=c.readUInt16LE(6);assert.ok(count>0&&count<=16);
  const sections=Array.from({length:count},(_,i)=>{const b=read(p+264+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
  function map(r,n=1){const matches=sections.filter(s=>r>=s.rva&&r+n<=s.rva+s.size);assert.equal(matches.length,1);const s=matches[0];return {s,at:s.offset+r-s.rva,left:s.rva+s.size-r};}
  function rr(r,n){const a=map(r,n);assert.ok(!(a.s.flags&0x20000000),'Metadata only');return read(a.at,n);}
  function str(r){const parts=[];for(let used=0;used<512;){const a=map(r+used),n=Math.min(32,a.left,512-used),b=rr(r+used,n),end=b.indexOf(0);parts.push(end<0?b:b.subarray(0,end));used+=n;if(end>=0){const all=Buffer.concat(parts);assert.ok(all.every(v=>v<128));return all.toString('ascii');}}throw Error('String bound');}
  function boundary(rva){const pr=o.readUInt32LE(136),size=o.readUInt32LE(140);assert.ok(pr&&size%12===0);let lo=0,hi=size/12,steps=0;while(lo<hi){assert.ok(++steps<=24);const mid=Math.floor((lo+hi)/2),b=rr(pr+mid*12,12),begin=b.readUInt32LE(0),end=b.readUInt32LE(4);if(rva<begin)hi=mid;else if(rva>=end)lo=mid+1;else return {begin_rva:hex(begin),end_rva_exclusive:hex(end),exact_start:rva===begin,unwind_rva:hex(b.readUInt32LE(8))};}return null;}
  if(rel.endsWith('.exe')){
   const im=src.importModules.find(x=>x.module==='Game.dll'),idx=im.symbols.indexOf(symbol);assert.ok(idx>=0&&im.symbols.lastIndexOf(symbol)===idx);
   const ir=o.readUInt32LE(120),isz=o.readUInt32LE(124);let found=false;
   for(let i=0;i<Math.min(64,Math.floor(isz/20));i++){const b=rr(ir+i*20,20),name=b.readUInt32LE(12);if(!name)break;if(str(name)!=='Game.dll')continue;
    assert.ok(!found);found=true;const oft=b.readUInt32LE(0),ft=b.readUInt32LE(16);assert.ok(oft&&ft);const value=rr(oft+idx*8,8).readBigUInt64LE(0);assert.ok(value<0x80000000n);assert.equal(str(Number(value)+2),symbol);
    m.targets.push({kind:'exact-import-iat-slot',symbol,source_index:idx,lookup_rva:hex(oft+idx*8),iat_rva:hex(ft+idx*8),preferred_va:hex(base+BigInt(ft+idx*8)),disk_iat_value:hex(rr(ft+idx*8,8).readBigUInt64LE(0)),caller_unknown:true});}
   assert.ok(found,'Game import descriptor required');
  }else{
   const er=o.readUInt32LE(112),esz=o.readUInt32LE(116),e=rr(er,40),n=e.readUInt32LE(24),np=e.readUInt32LE(32),ord=e.readUInt32LE(36),eat=e.readUInt32LE(28);assert.ok(n<100000&&n>0);
   const cache=new Map();function nameAt(i){assert.ok(i>=0&&i<n);if(!cache.has(i)){assert.ok(cache.size<128);cache.set(i,str(rr(np+i*4,4).readUInt32LE(0)));}return cache.get(i);}
   for(const prefix of ['??0ControllerPlayer@GAME@@','??_7ControllerPlayer@GAME@@']){
    let lo=0,hi=n;while(lo<hi){const mid=Math.floor((lo+hi)/2);if(nameAt(mid)<prefix)lo=mid+1;else hi=mid;}
    const hits=[];for(let i=lo;i<n&&nameAt(i).startsWith(prefix);i++){assert.ok(hits.length<4);const oi=rr(ord+i*2,2).readUInt16LE(0);assert.ok(oi<e.readUInt32LE(20));const rva=rr(eat+oi*4,4).readUInt32LE(0);assert.ok(rva&&!(rva>=er&&rva<er+esz));const a=map(rva);hits.push({name:nameAt(i),rva:hex(rva),preferred_va:hex(base+BigInt(rva)),section:a.s.name,executable:!!(a.s.flags&0x20000000),boundary:(a.s.flags&0x20000000)?boundary(rva):null});}
    m.targets.push({kind:'bounded-export-prefix',prefix,hits});}
  }
 }catch(e){report.failures.push(rel+': '+String(e));}
 finally{try{m.sha256_after=digest();assert.equal(m.sha256_after,src.sha256);const end=fs.fstatSync(fd),named=fs.statSync(source);for(const s of [end,named])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);normal(source);m.stable=true;}catch(e){report.failures.push(rel+' final: '+String(e));}fs.closeSync(fd);m.closed=true;}
 if(report.failures.length)break;
}
report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';
fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify(report));if(report.failures.length)process.exitCode=1;
