// Fixed metadata-only request. Preparing this file does not authorize execution.
// Usage: node tools/static_analysis/read_m3_listener_classinfo.mjs docs/m3-evidence/plans/listener-classinfo.json
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const planRel='docs/m3-evidence/plans/listener-classinfo.json';
assert.equal(process.argv.length,3);assert.equal(process.argv[2],planRel);
const source={path:'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',
  sha256:'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',bytes:11385856,preferred_base:'0x180000000'};
const proof={path:'docs/m3-evidence/death-notification-target/direct-49cb80.instructions.json',
  sha256:'ca9ea0099ddcc21070705e0fda6bf6d1162621744e8937b5ebebc8409e87ebbc',address:'0x18049cba3',bytes:'4c8d2dc6db5b00'};
const record={rva:'0xa5a770',bytes:48,name_pointer_offset:8};
const limits={metadata_bytes:1024,name_window_bytes:128,data_bytes:176,code_bytes:0,subprocesses:0};
const base=BigInt(source.preferred_base),hex=n=>'0x'+n.toString(16),sha=b=>createHash('sha256').update(b).digest('hex');
const fits=(at,n,total)=>Number.isSafeInteger(at)&&Number.isSafeInteger(n)&&at>=0&&n>0&&at<=total&&n<=total-at;
function keys(o,names){assert.deepEqual(Object.keys(o).sort(),names.split(' ').sort());}
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);
  assert.ok(!s.isSymbolicLink(),'Links/junctions refused');assert.ok(first&&file?s.isFile():s.isDirectory());
  assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
  const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function local(rel){assert.ok(/^[a-z0-9./-]+$/.test(rel)&&!rel.split('/').some(s=>!s||s==='.'||s==='..'));
  const p=path.resolve(root,rel);assert.ok(p.startsWith(root+path.sep));return p;}
function saved(rel,max){const p=local(rel);normal(p,true);assert.ok(fs.statSync(p).size<=max);return fs.readFileSync(p);}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing evidence preserved: '+p);}
function digest(fd,n){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<n;){
  const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert.ok(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}

const planBytes=saved(planRel,8192),plan=JSON.parse(planBytes);
keys(plan,'schema id purpose source proof record limits output follow_parent_factory_or_vtable execution_requires_separate_review_and_approval');
assert.equal(plan.schema,'grim-dynasty-m3-fixed-listener-classinfo-plan-v1');assert.equal(plan.id,'listener-classinfo');
assert.ok(typeof plan.purpose==='string'&&plan.purpose.length<=1024);assert.deepEqual(plan.source,source);assert.deepEqual(plan.proof,proof);
assert.deepEqual(plan.record,record);assert.deepEqual(plan.limits,limits);assert.equal(plan.output,'docs/m3-evidence/listener-classinfo');
assert.equal(plan.follow_parent_factory_or_vtable,false);assert.equal(plan.execution_requires_separate_review_and_approval,true);
const proofBytes=saved(proof.path,131072);assert.equal(sha(proofBytes),proof.sha256);
const rows=JSON.parse(proofBytes);assert.ok(Array.isArray(rows));const matches=rows.filter(r=>r.address===proof.address);assert.equal(matches.length,1);
assert.equal(matches[0].bytes,proof.bytes);assert.match(matches[0].instruction,/^lea\s+r13,\[0000000180A5A770h\]$/i);
const encoded=Buffer.from(proof.bytes,'hex');assert.equal(encoded.subarray(0,3).toString('hex'),'4c8d2d');
assert.equal(BigInt(proof.address)+7n+BigInt(encoded.readInt32LE(3)),base+BigInt(record.rva));
normal(self,true);const out=local(plan.output);normal(path.dirname(out));absent(out);
const report={schema:'grim-dynasty-m3-fixed-listener-classinfo-read-v1',status:'Started',started_utc:new Date().toISOString(),
  plan:{path:planRel,sha256:sha(planBytes)},script_sha256:sha(fs.readFileSync(self)),source,proof,limits,
  source_open_mode:'r',source_loaded_or_executed:false,source_closed:false,metadata_bytes:0,data_bytes:0,
  code_bytes:0,subprocesses:0,exports_read:false,pdata_read:false,follow_parent_factory_or_vtable:false,reads:[],failures:[],
  limitations:['ClassInfo disk bytes do not establish runtime registration state, callback identity or event production.',
    'Name text is reported only for printable ASCII bytes before NUL; other bytes remain uninterpreted.',
    'The bounded name window may include bytes after NUL; those bytes are not classified as name content.',
    'Full before/after SHA256 reads use one read-only descriptor and 64 KiB buffers; hashing is not semantic inspection.',
    'Hash and stat checks detect changes but do not provide an atomic snapshot or exclusive file lock.']};
let fd,initial,created=false;
try{
  fs.mkdirSync(out);created=true;normal(out);normal(source.path,true);fd=fs.openSync(source.path,'r');initial=fs.fstatSync(fd);
  assert.ok(initial.isFile());assert.equal(initial.size,source.bytes);report.sha256_before=digest(fd,source.bytes);assert.equal(report.sha256_before,source.sha256);
  function read(offset,n,kind){assert.ok(fits(offset,n,source.bytes));
    if(kind==='header'){assert.ok(n<=512&&report.metadata_bytes+n<=limits.metadata_bytes);report.metadata_bytes+=n;}
    else{assert.ok(report.data_bytes+n<=limits.data_bytes);report.data_bytes+=n;}
    const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);report.reads.push({kind,file_offset:offset,bytes:n});return b;}
  const dos=read(0,64,'header');assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert.ok(pe>=64&&pe<=4072);
  const coff=read(pe,24,'header');assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
  const count=coff.readUInt16LE(6),os=coff.readUInt16LE(20);assert.ok(count>0&&count<=16&&os===240&&pe+24+os+count*40<=4096);
  const opt=read(pe+24,os,'header');assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),base);
  const imageSize=opt.readUInt32LE(56),sections=Array.from({length:count},(_,i)=>{const b=read(pe+24+os+i*40,40,'header');
    return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),raw_size:b.readUInt32LE(16),
      raw_offset:b.readUInt32LE(20),characteristics:b.readUInt32LE(36)};});
  report.pe={image_base:hex(base),image_bytes:imageSize,sections};
  function mapData(rva,n){assert.ok(fits(rva,n,imageSize),'Outside image');const hits=sections.filter(s=>fits(rva-s.rva,n,s.raw_size));
    assert.equal(hits.length,1,'Data must map uniquely to section raw bytes');const section=hits[0],flags=section.characteristics;
    assert.ok((flags&0x40000000)!==0&&(flags&0x20000000)===0&&(flags&0x40)!==0,'Readable initialized non-executable data required');
    const offset=section.raw_offset+rva-section.rva;assert.ok(fits(offset,n,source.bytes));return {section,offset};}
  const mapped=mapData(Number(record.rva),48),bytes=read(mapped.offset,48,'classinfo-record');
  report.record={...record,file_offset:mapped.offset,section:mapped.section.name,hex_bytes:bytes.toString('hex'),sha256:sha(bytes),
    raw_qwords:Array.from({length:6},(_,i)=>({offset:hex(i*8),value:hex(bytes.readBigUInt64LE(i*8))}))};
  const pointer=bytes.readBigUInt64LE(8);report.name={pointer:hex(pointer),status:'Unknown',window_bytes:0};
  if(pointer===0n)report.name.status='ZeroDiskNamePointer';
  else if(pointer<base||pointer>=base+BigInt(imageSize))report.name.status='UnknownPointerOutsideImage';
  else{
    let nameMapping;const rva=Number(pointer-base);
    try{nameMapping=mapData(rva,1);}catch(e){report.name.status='UnknownPointerNotRawData';report.name.reason=String(e);}
    if(nameMapping){const n=Math.min(128,nameMapping.section.raw_size-(rva-nameMapping.section.rva),source.bytes-nameMapping.offset,imageSize-rva);
      mapData(rva,n);const window=read(nameMapping.offset,n,'name-window'),nul=window.indexOf(0),content=nul>=0?window.subarray(0,nul):null;
      Object.assign(report.name,{status:nul>=0?'NulTerminatedNameBytesObserved':'UnknownNoNulWithinBound',rva:hex(rva),
        file_offset:nameMapping.offset,section:nameMapping.section.name,window_bytes:n,window_hex:window.toString('hex'),
        window_sha256:sha(window),nul_offset:nul>=0?nul:null,name_hex:content?.toString('hex')??null,
        ascii_if_printable:content&&content.every(b=>b>=32&&b<=126)?content.toString('ascii'):null,
        unclassified_after_nul_bytes:nul>=0?n-nul-1:null});}
  }
  report.status='FixedClassInfoDataCompleted';
}catch(e){report.failures.push(String(e));report.status='Failed';}
finally{
  if(fd!==undefined){try{report.sha256_after=digest(fd,source.bytes);assert.equal(report.sha256_after,source.sha256);
      report.source_descriptor_stable=stable(initial,fs.fstatSync(fd));assert.ok(report.source_descriptor_stable);
      normal(source.path,true);report.source_path_stable=stable(initial,fs.statSync(source.path));assert.ok(report.source_path_stable);}
    catch(e){report.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('Close: '+e);}}}
  report.source_hash_preserved=report.sha256_before===source.sha256&&report.sha256_after===source.sha256;
  if(report.failures.length)report.status='Failed';report.finished_utc=new Date().toISOString();
  if(created){normal(out);const result=JSON.stringify(report,null,2)+'\n';assert.ok(Buffer.byteLength(result)<=32768);
    fs.writeFileSync(path.join(out,'inspection.json'),result,{flag:'wx'});}
}
console.log(JSON.stringify({status:report.status,name_status:report.name?.status,metadata_bytes:report.metadata_bytes,data_bytes:report.data_bytes,
  code_bytes:0,subprocesses:0,source_hash_preserved:report.source_hash_preserved,output:plan.output,failures:report.failures}));
if(report.status==='Failed')process.exitCode=1;
