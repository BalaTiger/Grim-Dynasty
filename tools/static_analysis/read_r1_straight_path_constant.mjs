// No file I/O at import. Explicit invocation requires separate execution approval.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectStraightPathConstant() {
  assert.equal(process.platform, 'win32');
  const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
  assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self),'read_r1_straight_path_constant.mjs');
  const source='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632;
  const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
  const proof={file:'docs/reverse-client/r1-navigation-core/11-NavigationCore-0x180262480-procedure_assembly.json',
    sha256:'60dd81bd3ffd941308c2407ead408c40bf9b42df47bfc5fa5ae08f042c7055d7',procedure:'0x180262480'};
  const targets=[{rva:0x34b960,bytes:4,instruction:'0x18026274a: COMISS XMM2, dword ptr [0x18034b960]'}];
  const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
  function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
    assert(!fs.lstatSync(q).isSymbolicLink(),'redirected path: '+q);
    assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
    if(path.dirname(q)===q)break;
  }}
  function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
  function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
  regular(self);
  const out=path.join(root,'docs/reverse-client/r1-straight-path-constant');
  normal(path.dirname(out));
  try{fs.lstatSync(out);assert.fail('Existing output is preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
  fs.mkdirSync(out);normal(out);
  const report={started_utc:new Date().toISOString(),source,expected_sha256:expected,expected_size:size,
    script_sha256:hash(fs.readFileSync(self)),proof,metadata_bytes:0,data_bytes:0,hash_bytes:0,
    code_bytes:0,subprocesses:0,original_executed:false,values:[],failures:[],
    scope:'PE headers/section metadata at most4096B, then exactly4B at RVA34B960. Entire-file streams are only before/after SHA256.',
    limitations:[
      'Disk float32 operand bound to262480 instruction26274A; no expected value is assumed and no loaded mutable value or navigation success is proved.',
      'No adjacent data, function body, DLL execution, backend, other original file or saves are accessed.',
      'Node does not expose every Windows reparse tag; the established external Attributes precheck is still required.',
      'Same-descriptor before/after hash/stat equality is not an atomic filesystem snapshot.'
    ]};
  let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<size;){
    const n=fs.readSync(fd,b,0,Math.min(b.length,size-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;report.hash_bytes+=n;
  }return h.digest('hex');}
  function metadata(at,n){assert(Number.isSafeInteger(at)&&at>=0&&n>0&&n<=512&&at+n<=size);
    assert(report.metadata_bytes+n<=4096);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,at),n);
    report.metadata_bytes+=n;return b;}
  try{
    const proofFile=path.join(root,proof.file),proofStat=regular(proofFile);assert(proofStat.size<=1024*1024);
    const bytes=fs.readFileSync(proofFile);assert.equal(hash(bytes),proof.sha256);
    const e=JSON.parse(bytes).result.structuredContent;
    assert.equal(e.subject.name,'Engine.dll');assert.equal(e.subject.digest.sha256,expected);
    assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(source).toLowerCase());
    assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,proof.procedure);
    assert.equal(typeof e.normalized_result,'string');const lines=e.normalized_result.split('\n');
    for(const t of targets)assert(lines.includes(t.instruction));report.proof_verified=true;
    const beforeOpen=regular(source);assert.equal(beforeOpen.size,size);
    fd=fs.openSync(source,'r');initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,beforeOpen);
    report.sha256_before=digest();assert.equal(report.sha256_before,expected);
    const dos=metadata(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
    const pe=dos.readUInt32LE(60);assert(pe>=64&&pe<4096);
    const coff=metadata(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
    assert.equal(coff.readUInt16LE(20),240);const opt=metadata(pe+24,240);assert.equal(opt.readUInt16LE(0),0x20b);
    const imageBase=opt.readBigUInt64LE(24),imageSize=opt.readUInt32LE(56);assert.equal(imageBase,0x180000000n);
    const count=coff.readUInt16LE(6);assert(count>0&&count<=16&&pe+264+count*40<=4096);
    const sections=Array.from({length:count},(_,i)=>{const b=metadata(pe+264+i*40,40),s={
      name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),virtual_size:b.readUInt32LE(8),
      rva:b.readUInt32LE(12),size:b.readUInt32LE(16),offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};
      assert(s.rva+Math.max(s.virtual_size,s.size)<=imageSize);assert(s.offset+s.size<=size);return s;});
    for(const t of targets){
      assert.equal(t.bytes,4);assert(t.rva+t.bytes<=imageSize);
      const hits=sections.filter(s=>t.rva>=s.rva&&t.rva+t.bytes<=s.rva+s.size);assert.equal(hits.length,1);
      const section=hits[0];assert(!(section.flags&0x20000000));assert(section.flags&0x40000000);
      const offset=section.offset+t.rva-section.rva;assert(offset>=0&&offset+t.bytes<=size);
      assert(report.data_bytes+t.bytes<=4);const b=Buffer.alloc(t.bytes);
      assert.equal(fs.readSync(fd,b,0,t.bytes,offset),t.bytes);report.data_bytes+=t.bytes;
      report.values.push({rva:hex(t.rva),va:hex(imageBase+BigInt(t.rva)),file_offset:offset,bytes:t.bytes,section:section.name,
        raw_hex:b.toString('hex'),float32_le:b.readFloatLE(0),
        uint32_hex:Array.from({length:1},(_,i)=>'0x'+b.readUInt32LE(i*4).toString(16).padStart(8,'0')),
        evidence_instruction:t.instruction,loaded_value_observed:false});
    }
    assert.equal(report.data_bytes,4);
  }catch(e){report.failures.push(String(e));}
  finally{
    if(fd!==undefined){try{
      report.sha256_after=digest();assert.equal(report.sha256_after,expected);
      sameStat(fs.fstatSync(fd),initial);sameStat(regular(source),initial);report.stable=true;
    }catch(e){report.failures.push(String(e));}
    finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push(String(e));}}}
  }
  report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';
  normal(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});
  if(report.failures.length)throw Error('Fixed straight-path constant read failed; preserved '+out+': '+report.failures.join('; '));
  return report;
}
