// Classify exactly two saved Rea xrefs using PE directories; zero code reads.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert.ok(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proofRel='docs/reverse-client/r1-game-mouse-links/04-HandleActionFromMouse-0x18015e1f0-xrefs.json';
const proof=fs.readFileSync(path.join(root,proofRel));assert.equal(sha(proof),'2c9f201089581cf04a56eefb78022aaa897e1a35f0f2e2922f707a1f7a6090b5');
assert.deepEqual(JSON.parse(proof).result.structuredContent.normalized_result,['0x180881608','0x180a7c744']);
const output=path.join(root,'docs/reverse-client/r1-mouse-link-metadata/xref-classification.json');normal(path.dirname(output));assert.ok(!fs.existsSync(output));
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const fd=fs.openSync(source,'r'),initial=fs.fstatSync(fd);assert.equal(initial.size,11385856);
const r={started_utc:new Date().toISOString(),source,proof_file:proofRel,proof_sha256:sha(proof),metadata_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,references:[],failures:[]};
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let a=0;a<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-a),a);assert.ok(n);h.update(b.subarray(0,n));a+=n;}return h.digest('hex');}
function read(a,n){assert.ok(a>=0&&n>0&&n<=512&&a+n<=initial.size);r.metadata_bytes+=n;assert.ok(r.metadata_bytes<=4096);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,a),n);return b;}
try{
 r.sha256_before=digest();assert.equal(r.sha256_before,expected);const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const p=dos.readUInt32LE(60);assert.ok(p<4096);
 const c=read(p,24);assert.equal(c.readUInt32LE(0),0x4550);assert.equal(c.readUInt16LE(4),0x8664);assert.equal(c.readUInt16LE(20),240);const o=read(p+24,240);assert.equal(o.readBigUInt64LE(24),0x180000000n);
 const count=c.readUInt16LE(6);assert.ok(count>0&&count<=16);const sections=Array.from({length:count},(_,i)=>{const b=read(p+264+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 function rr(a,n){const ss=sections.filter(s=>a>=s.rva&&a+n<=s.rva+s.size);assert.equal(ss.length,1);assert.ok(!(ss[0].flags&0x20000000));return read(ss[0].offset+a-ss[0].rva,n);}
 const er=o.readUInt32LE(112),e=rr(er,40),eat=e.readUInt32LE(28),n=e.readUInt32LE(20),ordinalBase=e.readUInt32LE(16);
 const at=0x881608;assert.ok(at>=eat&&(at-eat)%4===0&&(at-eat)/4<n);const v=rr(at,4).readUInt32LE(0);assert.equal(v,0x15e1f0);
 r.references.push({address:'0x180881608',classification:'export-address-table-entry',entry_rva:hex(at),ordinal:ordinalBase+(at-eat)/4,target_rva:hex(v),call_instruction:false});
 const pr=o.readUInt32LE(136),size=o.readUInt32LE(140),pa=0xa7c744;assert.ok(pa>=pr&&(pa-pr)%12===0&&pa+12<=pr+size);const b=rr(pa,12);assert.equal(b.readUInt32LE(0),0x15e1f0);
 r.references.push({address:'0x180a7c744',classification:'runtime-function-table-record',record_rva:hex(pa),begin_rva:hex(b.readUInt32LE(0)),end_rva_exclusive:hex(b.readUInt32LE(4)),unwind_rva:hex(b.readUInt32LE(8)),call_instruction:false});
}catch(e){r.failures.push(String(e));}
finally{try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}fs.closeSync(fd);r.source_closed=true;}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.writeFileSync(output,JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
