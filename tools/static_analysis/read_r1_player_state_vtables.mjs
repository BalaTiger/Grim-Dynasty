// Locate ControllerPlayerState vtables on disk from the already-observed
// PlayerMouseTable slot triple, then read their +0x70 entry.
// The triple (0x1B0=Primary, 0x1B8=Secondary, 0x1C8=Super) is the fixed
// BindObserved binding, so it identifies a table start without guessing
// contiguous-pointer runs. Method discovery only: nothing is executed.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert.ok(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const anchors=[
 {file:'docs/reverse-client/r1-player-move-entries/10-PlayerMove-0x180166020-procedure_pseudo_code.json',hash:'360a47d14f23eddfab0ee14320f2c16815a52dc8a967c55460cee160f10ee960',symbol:'ControllerPlayerStateMoveTo::OnBegin',va:0x180166020},
 {file:'docs/reverse-client/r1-player-move-entries/04-PlayerMove-0x180165cb0-procedure_pseudo_code.json',hash:'5a61bffc2d57d696f041e8a943c937778d3189c72143af17cefdf09803a64079',symbol:'ControllerPlayerStateIdle::RequestMoveAction',va:0x180165cb0},
];
const TRIPLE=[[0x1b0,0x180161d00],[0x1b8,0x180162290],[0x1c8,0x1801624b0]];
const output=path.join(root,'docs/reverse-client/r1-player-state-vtables/player-state-vtables.json');normal(path.dirname(output));assert.ok(!fs.existsSync(output));
const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';normal(source);
const fd=fs.openSync(source,'r'),st=fs.fstatSync(fd);assert.equal(st.size,11385856);
const r={started_utc:new Date().toISOString(),source,script_sha256:sha(fs.readFileSync(fileURLToPath(import.meta.url))),anchors:[],triple:TRIPLE.map(([o,v])=>({slot:hex(o),value:'0x'+v.toString(16)})),tables:[],failures:[],original_executed:false,subprocesses:0};
function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let a=0;a<st.size;){const n=fs.readSync(fd,b,0,Math.min(b.length,st.size-a),a);assert.ok(n);h.update(b.subarray(0,n));a+=n;}return h.digest('hex');}
function read(a,n){assert.ok(a>=0&&n>0&&a+n<=st.size);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,a),n);return b;}
try{
 r.sha256_before=digest();assert.equal(r.sha256_before,expected);
 const head=read(0,0x400);assert.equal(head.readUInt16LE(0),0x5a4d);const pe=head.readUInt32LE(60);assert.ok(pe<0x400);
 const c=read(pe,0x200);assert.equal(c.readUInt32LE(0),0x4550);assert.equal(c.readUInt16LE(4),0x8664);assert.equal(c.readUInt16LE(20),240);
 const imgBase=c.readBigUInt64LE(48);assert.equal(imgBase,0x180000000n);
 const nsec=c.readUInt16LE(6);assert.ok(nsec>0&&nsec<=16);
 const secs=[];for(let i=0;i<nsec;i++){const o=pe+264+i*40,b=read(o,40);secs.push({name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),vsize:b.readUInt32LE(16),raw:b.readUInt32LE(20),flags:b.readUInt32LE(36)});}
 const rdata=secs.find(s=>s.name==='.rdata');assert.ok(rdata);
 function sec(rv){return secs.find(s=>rv>=s.rva&&rv<s.rva+s.vsize)||null;}
 function offOf(rv){const s=sec(rv);assert.ok(s&&rv+s.vsize-s.rva>=0);return s.raw+rv-s.rva;}
 function rvaOf(off){for(const s of secs)if(off>=s.raw&&off<s.raw+s.vsize)return s.rva+off-s.raw;return null;}
 function q(rv){return read(offOf(rv),8).readBigUInt64LE(0);}
 function isText(v){const rv=Number(v-imgBase);const s=sec(rv);return !!(s&&s.name==='.text');}
 for(const a of anchors){
  const p=fs.readFileSync(path.join(root,a.file)),text=p.toString('utf8'),parsed=JSON.parse(text);
  assert.equal(sha(p),a.hash,'anchor proof hash changed');
  assert.equal(String(parsed.result.structuredContent.parameters.procedure).toLowerCase(),'0x'+a.va.toString(16),'anchor procedure differs');
  assert.ok(text.includes(a.symbol),'anchor symbol missing from proof');
  r.anchors.push({file:a.file,sha256:sha(p),va:'0x'+a.va.toString(16),symbol:a.symbol,verified_by_procedure_and_symbol:true});
 }
 const starts=[];
 for(let rv=rdata.rva;rv+0x1d0<=rdata.rva+rdata.vsize;rv+=8){
  let ok=true;for(const [slot,value] of TRIPLE){if(q(rv+slot)!==BigInt(value)){ok=false;break;}}
  if(ok)starts.push(rv-0x1b0);
 }
 for(const start of starts){
  if(start<rdata.rva||!isText(q(start+0x70)))continue;
  const entry70=Number(q(start+0x70)-imgBase);
  const bytes=read(offOf(entry70),8).toString('hex');
  const found=[];
  for(const a of anchors){
   for(let slot=0;slot<0x300;slot+=8){if(q(start+slot)===BigInt(a.va)){found.push({slot:hex(slot),va:'0x'+a.va.toString(16),symbol:a.symbol});}}
  }
  r.tables.push({
   vtable_va:'0x'+Number(imgBase+BigInt(start)).toString(16),vtable_rva:hex(start),
   slot70_va:'0x'+Number(imgBase+BigInt(entry70)).toString(16),slot70_rva:hex(entry70),
   slot70_first_bytes:bytes,
   anchors_in_table:found,
   slots_0x1b0_to_0x1c8_match_documented_triple:true});
 }
}catch(e){r.failures.push(String(e));}
finally{try{r.sha256_after=digest();assert.equal(r.sha256_after,expected);normal(source);r.stable=true;}catch(e){r.failures.push(String(e));}fs.closeSync(fd);r.source_closed=true;}
r.finished_utc=new Date().toISOString();r.status=r.failures.length?'failed':'passed';fs.writeFileSync(output,JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));if(r.failures.length)process.exitCode=1;
