// Exactly one vtable entry: PreStart vtable +0x70, anchored in two saved Rea bodies.
// The 0x123810 EndOfPathReached body jumps through [state vtable +0x70]; the
// 0x122960 constructor publishes vtable 0x1806e40a0 to this+2A0. This reads the
// disk-resident slot target only; it is not a loaded-runtime observation.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert.ok(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const slotProofRel='docs/reverse-client/r1-controller-action-slots-run1/07-ControllerActionSlots-0x180123810-procedure_assembly.json',slotProof=fs.readFileSync(path.join(root,slotProofRel));
assert.equal(sha(slotProof),'fe3ddc8000174be9021f50327526cd0dae35e754e9a324b0951fdc37340406bc');
const slotAsm=JSON.parse(slotProof).result.structuredContent.normalized_result;
for(const line of ['0x18012382b: JMP qword ptr [RAX + 0x70]','0x180123839: JMP qword ptr [RAX + 0x70]'])assert.ok(slotAsm.split('\n').includes(line));
const ctorProofRel='docs/reverse-client/r1-game-mouse-state/05-ControllerAI-constructor-0x180122960-procedure_assembly.json',ctorProof=fs.readFileSync(path.join(root,ctorProofRel));
assert.equal(sha(ctorProof),'7ccf127712c3253252910650ed06e2f1e01bc035ee753b771c7936aba63ad22c');
const ctorAsm=JSON.parse(ctorProof).result.structuredContent.normalized_result;
for(const line of ['0x180122ad9: LEA RCX, [0x1806e40a0]','0x180122ae0: MOV qword ptr [RAX], RCX','0x180122aea: MOV qword ptr [RDI + 0x2a0], RAX'])assert.ok(ctorAsm.split('\n').includes(line));
const output=path.join(root,'docs/reverse-client/r1-prestart-endofpath-slot/prestart-endofpath-slot.json');normal(path.dirname(output));assert.ok(!fs.existsSync(output));
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const fd=fs.openSync(source,'r'),initial=fs.fstatSync(fd);assert.equal(initial.size,11385856);
const r={started_utc:new Date().toISOString(),source,slot_proof_file:slotProofRel,slot_proof_sha256:sha(slotProof),constructor_proof_file:ctorProofRel,constructor_proof_sha256:sha(ctorProof),script_sha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),metadata_bytes:0,data_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,vtable:'0x1806e40a0',offset:'0x70',slots:[],failures:[]};
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let a=0;a<initial.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,initial.size-a),a);assert.ok(n);h.update(b.subarray(0,n));a+=n;}return h.digest('hex');}
function read(a,n){assert.ok(a>=0&&n>0&&n<=512&&a+n<=initial.size);r.metadata_bytes+=n;assert.ok(r.metadata_bytes<=4096);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,a),n);return b;}
try{
 r.sha256_before=digest();assert.equal(r.sha256_before,expected);const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const p=dos.readUInt32LE(60);assert.ok(p<4096);const c=read(p,24);assert.equal(c.readUInt32LE(0),0x4550);assert.equal(c.readUInt16LE(4),0x8664);assert.equal(c.readUInt16LE(20),240);const o=read(p+24,240);assert.equal(o.readBigUInt64LE(24),0x180000000n);
 const count=c.readUInt16LE(6);assert.ok(count>0&&count<=16);const sections=Array.from({length:count},(_,i)=>{const b=read(p+264+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 function map(a,n){const ss=sections.filter(s=>a>=s.rva&&a+n<=s.rva+s.size);assert.equal(ss.length,1);return ss[0];}
 function rr(a,n){const s=map(a,n);assert.ok(!(s.flags&0x20000000));return read(s.offset+a-s.rva,n);}
 const pr=o.readUInt32LE(136),size=o.readUInt32LE(140);assert.ok(pr&&size%12===0);
 function boundary(a){let lo=0,hi=size/12,k=0;while(lo<hi){assert.ok(++k<=24);const mid=Math.floor((lo+hi)/2),b=rr(pr+mid*12,12),begin=b.readUInt32LE(0),end=b.readUInt32LE(4);if(a<begin)hi=mid;else if(a>=end)lo=mid+1;else return {begin_rva:hex(begin),end_rva_exclusive:hex(end),exact_start:a===begin,unwind_rva:hex(b.readUInt32LE(8))};}return null;}
 for(const slot of [0x70]){const a=0x6e40a0+slot,s=map(a,8);assert.equal(s.name,'.rdata');assert.ok(!(s.flags&0x80000000));const raw=rr(a,8),value=raw.readBigUInt64LE(0);r.data_bytes+=8;assert.ok(value>=0x180000000n&&value<0x200000000n);const rv=Number(value-0x180000000n),ts=map(rv,1);assert.ok(ts.flags&0x20000000);const entry={slot:hex(slot),entry_rva:hex(a),raw_hex:raw.toString('hex'),target_va:hex(value),target_rva:hex(rv),target_section:ts.name,target_file_offset:ts.offset+rv-ts.rva,boundary:boundary(rv),target_body_read:false,decoded_target_prefix:null};
  const csec=map(rv,6);assert.ok(csec.flags&0x20000000);const pr2=read(csec.offset+rv-csec.rva,6);r.code_bytes+=6;
  entry.decoded_target_prefix={bytes:pr2.toString('hex'),stop_reason:'first-linear-ret',source:'disk-window-only'};r.slots.push(entry);}
}catch(e){r.failures.push(String(e));}
finally{try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','dev','ino','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);r.stable=true;}catch(e){r.failures.push(String(e));}fs.closeSync(fd);r.source_closed=true;}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.writeFileSync(output,JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
