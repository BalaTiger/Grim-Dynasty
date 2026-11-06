// Resolve the eight ControllerPlayerState vtables from the saved
// ControllerPlayer::RegisterStates body (0x15E440), pair each with the state
// name string stored immediately after it, and read each table's +0x70 entry.
// Anchors are the saved assembly body; nothing is executed.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert.ok(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proofRel='docs/reverse-client/r1-player-register-states/05-PlayerRegisterStates-0x18015e440-procedure_assembly.json';
const proof=fs.readFileSync(path.join(root,proofRel));assert.equal(sha(proof),'ec9cee9df16f35c149ae5e756a3813659329a5115c760777843c4b46d188f23d');
const asm=JSON.parse(proof).result.structuredContent.normalized_result;
const lines=asm.split('\n').map(s=>s.trim());
assert.ok(lines.includes('0x18015e484: LEA RAX, [0x180745390]'));
assert.ok(lines.includes('0x18015e4d0: LEA R8D, [R14 + 0x1]'));
// Extract (vtable LEA -> next MOV [RBX],RAX) then the following name LEA + length.
const tables=[];
for(let i=0;i<lines.length;i++){
 const m=/^0x([0-9a-f]+): LEA RAX, \[0x(1807[0-9a-f]{5})\]$/.exec(lines[i]);
 if(!m)continue;
 if(i+1>=lines.length||!lines[i+1].endsWith('MOV qword ptr [RBX], RAX'))continue;
 const vtable=Number('0x'+m[2]);
 let nameAt=null,lenAt=null;
 for(let j=i+1;j<Math.min(i+40,lines.length);j++){
  const n=/^0x[0-9a-f]+: LEA RDX, \[0x(18067[0-9a-f]{4})\]$/.exec(lines[j]);
  if(n&&nameAt===null)nameAt=Number('0x'+n[1]);
  const l=/^0x[0-9a-f]+: LEA R8D, \[R14 \+ 0x([0-9a-f]+)\]$/.exec(lines[j]);
  if(l&&lenAt===null)lenAt=parseInt(l[1],16);
  if(nameAt!==null&&lenAt!==null)break;
 }
 tables.push({vtable,at:Number('0x'+m[1]),nameAt,lenAt});
}
assert.ok(tables.length>=8,'expected at least eight state registrations');
const output=path.join(root,'docs/reverse-client/r1-player-state-vtables/register-states-vtables.json');normal(path.dirname(output));assert.ok(!fs.existsSync(output));
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const fd=fs.openSync(source,'r'),st=fs.fstatSync(fd);assert.equal(st.size,11385856);
const r={started_utc:new Date().toISOString(),source,proof_file:proofRel,proof_sha256:sha(proof),script_sha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),registrations:[],failures:[],original_executed:false,subprocesses:0};
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let a=0;a<st.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,st.size-a),a);assert.ok(n);h.update(b.subarray(0,n));a+=n;}return h.digest('hex');}
function read(a,n){assert.ok(a>=0&&n>0&&a+n<=st.size);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,a),n);return b;}
try{
 r.sha256_before=digest();assert.equal(r.sha256_before,expected);
 const head=read(0,0x400);assert.equal(head.readUInt16LE(0),0x5a4d);const pe=head.readUInt32LE(60);
 const c=read(pe,0x200);assert.equal(c.readUInt32LE(0),0x4550);assert.equal(c.readUInt16LE(4),0x8664);
 const imgBase=c.readBigUInt64LE(48);assert.equal(imgBase,0x180000000n);
 const nsec=c.readUInt16LE(6);const secs=[];
 for(let i=0;i<nsec;i++){const o=pe+264+i*40,b=read(o,40);secs.push({name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),vsize:b.readUInt32LE(16),raw:b.readUInt32LE(20)});}
 function sec(rv){return secs.find(s=>rv>=s.rva&&rv<s.rva+s.vsize)||null;}
 function offOf(rv){const s=sec(rv);assert.ok(s,'rva not mapped: '+hex(rv));return s.raw+rv-s.rva;}
 function q(rv){return read(offOf(rv),8).readBigUInt64LE(0);}
 function cstr(rv,max){const s=sec(rv);assert.ok(s);const available=Math.min(max,s.rva+s.vsize-rv);const b=read(offOf(rv),available);const z=b.indexOf(0);assert.ok(z>0,'unterminated name');return b.subarray(0,z).toString('ascii');}
 for(const t of tables){
  assert.ok(t.nameAt!==null,'registration without a name LEA');
  // The assembly lists absolute VAs; the image base is 0x180000000.
  const vtableRva=t.vtable-0x180000000,nameRva=t.nameAt-0x180000000;
  assert.ok(vtableRva>0&&nameRva>0,'registration VA below image base');
  const name=cstr(nameRva,32);
  const entry70=Number(q(vtableRva+0x70)-imgBase);
  const ts=sec(entry70);assert.ok(ts,'slot70 target unmapped');
  const bytes=read(offOf(entry70),8).toString('hex');
  r.registrations.push({
   vtable_va:'0x'+Number(imgBase+BigInt(vtableRva)).toString(16),vtable_rva:hex(vtableRva),
   name_rva:hex(nameRva),name,name_length_field:t.lenAt,
   slot70_va:'0x'+Number(imgBase+BigInt(entry70)).toString(16),slot70_rva:hex(entry70),slot70_section:ts.name,slot70_first_bytes:bytes});
 }
}catch(e){r.failures.push(String(e));}
finally{try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);r.stable=true;}catch(e){r.failures.push(String(e));}fs.closeSync(fd);r.source_closed=true;}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.writeFileSync(output,JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
