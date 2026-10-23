// Second fixed probe: only REX.W MOV register,[RIP+disp32] loads of the proven IAT slot.
// Byte candidates are NOT asserted to be instructions. No disassembler/backend runs.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert.ok(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proofRel='docs/reverse-client/r1-mouse-link-metadata/inspection.json',proof=fs.readFileSync(path.join(root,proofRel));
assert.equal(sha(proof),'7eed75fd5b47b6a0699dd382c5a29eaea306f4b1856962c57c55351355e265cc');
const t=JSON.parse(proof).modules[0].targets[0];assert.equal(t.kind,'exact-import-iat-slot');assert.equal(t.preferred_va,'0x1402d78a0');
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Grim Dawn.exe',expected='d3915e2599317b6670cc088ba958afb2b2f4fa8141cfd1d777ee02f9ea1fc4c4';
const out=path.join(root,'docs/reverse-client/r1-exe-mouse-pointer-candidates');normal(path.dirname(out));assert.ok(!fs.existsSync(out));normal(source);
const fd=fs.openSync(source,'r'),initial=fs.fstatSync(fd);assert.equal(initial.size,4687944);
const r={started_utc:new Date().toISOString(),source,expected_sha256:expected,proof_file:proofRel,proof_sha256:sha(proof),script_sha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),metadata_bytes:0,search_bytes:0,search_limit:4194304,search_operand_target:'0x1402d78a0',candidate_limit:16,backend_started:false,original_executed:false,subprocesses:0,candidates:[],failures:[],limitations:['Only REX.W MOV register,[RIP+disp32] byte encodings loading the one verified IAT slot are searched within .text. This is a follow-up to the completed zero-result FF15/FF25 probe, not a retry of it. Other encodings, thunk chains and runtime-resolved calls may be missed.','Raw matches are candidates until instruction alignment and call semantics are verified. Inline data can match.','No code outside .text, resources, saves or unrelated modules is searched. Full-file hashing is separate from interpretation.']};
fs.mkdirSync(out);
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let a=0;a<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-a),a);assert.ok(n);h.update(b.subarray(0,n));a+=n;}return h.digest('hex');}
function read(a,n,search=false){assert.ok(a>=0&&n>0&&a+n<=initial.size);if(search){r.search_bytes+=n;assert.ok(r.search_bytes<=r.search_limit);}else{assert.ok(n<=512);r.metadata_bytes+=n;assert.ok(r.metadata_bytes<=8192);}const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,a),n);return b;}
try{
 r.sha256_before=digest();assert.equal(r.sha256_before,expected);const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const p=dos.readUInt32LE(60);assert.ok(p<4096);const c=read(p,24);assert.equal(c.readUInt32LE(0),0x4550);assert.equal(c.readUInt16LE(4),0x8664);assert.equal(c.readUInt16LE(20),240);const o=read(p+24,240);assert.equal(o.readBigUInt64LE(24),0x140000000n);
 const count=c.readUInt16LE(6);assert.ok(count>0&&count<=16);const sections=Array.from({length:count},(_,i)=>{const b=read(p+264+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 function rr(a,n){const ss=sections.filter(s=>a>=s.rva&&a+n<=s.rva+s.size);assert.equal(ss.length,1);assert.ok(!(ss[0].flags&0x20000000));return read(ss[0].offset+a-ss[0].rva,n);}
 const pr=o.readUInt32LE(136),size=o.readUInt32LE(140);assert.ok(pr&&size%12===0);function boundary(a){let lo=0,hi=size/12,k=0;while(lo<hi){assert.ok(++k<=24);const mid=Math.floor((lo+hi)/2),b=rr(pr+mid*12,12),begin=b.readUInt32LE(0),end=b.readUInt32LE(4);if(a<begin)hi=mid;else if(a>=end)lo=mid+1;else return {begin_rva:hex(begin),end_rva_exclusive:hex(end),unwind_rva:hex(b.readUInt32LE(8)),bytes:end-begin};}return null;}
 const ss=sections.filter(s=>s.name==='.text');assert.equal(ss.length,1);const text=ss[0];assert.ok(text.flags&0x20000000);assert.ok(text.size<=r.search_limit);r.search_section=text;
 const b=read(text.offset,text.size,true);
 for(let i=0;i+7<=b.length;i++){
  if(b[i]<0x48||b[i]>0x4f||b[i+1]!==0x8b||(b[i+2]&0xc7)!==5)continue;const a=text.rva+i;if(a+7+b.readInt32LE(i+3)!==0x2d78a0)continue;assert.ok(r.candidates.length<r.candidate_limit);
  const reg=((b[i+2]>>3)&7)+((b[i]&4)?8:0),names=['rax','rcx','rdx','rbx','rsp','rbp','rsi','rdi','r8','r9','r10','r11','r12','r13','r14','r15'];
  r.candidates.push({address:hex(0x140000000n+BigInt(a)),rva:hex(a),file_offset:hex(text.offset+i),bytes:b.subarray(i,i+7).toString('hex'),encoding_candidate:'REX.W-MOV-register-rip-memory',destination_register:names[reg],operand_target:'0x1402d78a0',containing_runtime_function:boundary(a),instruction_verified:false});
 }
}catch(e){r.failures.push(String(e));}
finally{try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}fs.closeSync(fd);r.source_closed=true;}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
