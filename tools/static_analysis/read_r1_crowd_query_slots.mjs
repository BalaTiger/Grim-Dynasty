// No file I/O at import. Explicit invocation requires separate execution approval.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectCrowdQuerySlots() {
  assert.equal(process.platform, 'win32');
  const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
  assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self),'read_r1_crowd_query_slots.mjs');
  const source='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632;
  const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
  const proofs=[
    {file:'docs/reverse-client/r1-crowd-movement-direct/05-CrowdMovementDirect-0x18002f800-procedure_assembly.json',
      sha256:'e1889f6e7564b2eb23fa4cedef700eb8ea5693f8e06b30cad2577838ac13240f',procedure:'0x18002f800',
      instructions:['0x18002f826: MOV RSI, RCX','0x18002f829: LEA RAX, [0x18033a6c0]','0x18002f830: MOV qword ptr [RCX], RAX',
        '0x18002f8c3: MOV RDX, RSI','0x18002f8c6: MOV RCX, RAX','0x18002f8c9: CALL 0x18025a880','0x18002f8cf: MOV qword ptr [RSI + 0x8], RAX']},
    {file:'docs/reverse-client/r1-crowd-query-production/05-CrowdQueryProduction-0x18025a880-procedure_assembly.json',
      sha256:'28d78e7f8452eb2dc8c4a5219114c1a751ad1ed9a519b4864f8edeff40f4fa92',procedure:'0x18025a880',
      instructions:['0x18025a89f: MOV RSI, RDX','0x18025a8a2: MOV RBX, RCX','0x18025a9a8: MOV qword ptr [RBX + 0xb8], RSI']},
    {file:'docs/reverse-client/r1-crowd-query-production/08-CrowdQueryProduction-0x18025a4f0-procedure_assembly.json',
      sha256:'c29eae5930141c3885b2bfebb1081c2df2f8a0582464a92c610a05d330d4d57b',procedure:'0x18025a4f0',
      instructions:['0x18025a522: MOV RCX, qword ptr [RSI + 0xb8]','0x18025a529: MOV RAX, qword ptr [RCX]',
        '0x18025a53d: CALL qword ptr [RAX]','0x18025a54b: CALL qword ptr [RAX + 0x18]']},
    {file:'docs/reverse-client/r1-movement-floor-core/05-MovementFloorCore-0x18025b300-procedure_assembly.json',
      sha256:'368967c8253cc4f3b7a186a1f5de8ebdcf7de015843cc12d3b332e7b4ab027f3',procedure:'0x18025b300',
      instructions:['0x18025b3e2: MOV RCX, qword ptr [RDI + 0xb8]','0x18025b3f1: MOV RAX, qword ptr [RCX]','0x18025b3f4: CALL qword ptr [RAX + 0x18]']},
    {file:'docs/reverse-client/r1-crowd-movement-direct/08-CrowdMovementDirect-0x18025c440-procedure_assembly.json',
      sha256:'62da8b3465041af3522b3e36a72e4680485d0c9b2a832ab8b3207ce27bf5b8ae',procedure:'0x18025c440',
      instructions:['0x18025c59f: MOV RCX, qword ptr [R13 + 0x8]','0x18025c5ae: MOV RAX, qword ptr [RCX]','0x18025c5b1: CALL qword ptr [RAX + 0x18]',
        '0x18025c5b8: MOV RCX, qword ptr [R13 + 0x8]','0x18025c5c9: MOV RAX, qword ptr [RCX]','0x18025c5cc: CALL qword ptr [RAX + 0x20]']}
  ];
  const tableRva=0x33a6c0,targets=[{rva:0x33a6c0,bytes:8,slot_offset:0},{rva:0x33a6d8,bytes:8,slot_offset:0x18},{rva:0x33a6e0,bytes:8,slot_offset:0x20}];
  const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
  function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
    assert(!fs.lstatSync(q).isSymbolicLink(),'redirected path: '+q);
    assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
    if(path.dirname(q)===q)break;
  }}
  function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
  function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
  regular(self);
  const out=path.join(root,'docs/reverse-client/r1-crowd-query-slots');
  normal(path.dirname(out));
  try{fs.lstatSync(out);assert.fail('Existing output is preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
  fs.mkdirSync(out);normal(out);
  const report={started_utc:new Date().toISOString(),source,expected_sha256:expected,expected_size:size,
    script_sha256:hash(fs.readFileSync(self)),proofs,metadata_bytes:0,data_bytes:0,hash_bytes:0,
    code_bytes:0,subprocesses:0,original_executed:false,values:[],failures:[],
    vtable:{rva:hex(tableRva),constructor:'0x18002f800',write_instruction:'0x18002f830: MOV qword ptr [RCX], RAX',
      table_extent_verified:false,manager_b8_binding_verified:false,update_this8_binding_verified:false,
      manager_b8_binding_kind:'static-constructor-dataflow-only',
      manager_b8_binding_chain:[
        '02F826 captures outer RCX in RSI;02F829/02F830 publish vptr33A6C0 at outer+0.',
        '02F8C3 passes that outer as RDX to25A880 at02F8C9;25A89F captures RDX in RSI and25A8A2 captures manager RCX in RBX.',
        '25A9A8 stores captured outer RSI into manager[RBX+B8]. This binds that completed construction path only, not later mutable state.',
        '25A4F0 reads manager+B8 at25A522 and its vptr at25A529, then invokes slot0 at25A53D or slot18 at25A54B.',
        '25B300 independently reads manager+B8 at25B3E2, its vptr at25B3F1 and slot18 at25B3F4.'
      ]},
    scope:'PE headers/section metadata at most4096B, then exactly three QWORDs (24B total) at RVA33A6C0,33A6D8 and33A6E0. Slots08/10 and every other adjacent entry are excluded. Entire-file streams are only before/after SHA256.',
    limitations:[
      'Only disk values at offsets00/18/20 from the constructor-observed vptr33A6C0 are reported; full vtable extent, function semantics and loaded pointer values are not established.',
      'The02F800 to25A880 argument/publication chain proves manager+B8 retains the outer on that construction path; this is static dataflow, not runtime observation or proof against later writes.',
      '25C440 independently reads its receiver+8 before slots18/20. That update receiver field is not bound to the outer here and must not be equated with manager+B8 merely from matching slot offsets.',
      'A value in a file-backed executable section is only an executable-address candidate, not a verified function boundary or callable implementation.',
      'No adjacent vtable entries, function body, DLL execution, backend, other original file or saves are accessed.',
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
    for(const proof of proofs){
      const proofFile=path.join(root,proof.file),proofStat=regular(proofFile);assert(proofStat.size<=1024*1024);
      const bytes=fs.readFileSync(proofFile);assert.equal(hash(bytes),proof.sha256);
      const e=JSON.parse(bytes).result.structuredContent;
      assert.equal(e.subject.name,'Engine.dll');assert.equal(e.subject.digest.sha256,expected);
      assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(source).toLowerCase());
      assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,proof.procedure);
      assert.equal(typeof e.normalized_result,'string');const lines=e.normalized_result.split('\n');
      for(const instruction of proof.instructions)assert(lines.includes(instruction));
    }
    report.proofs_verified=true;report.vtable.manager_b8_binding_verified=true;
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
      assert.equal(t.bytes,8);assert.equal(t.rva,tableRva+t.slot_offset);assert(t.rva+t.bytes<=imageSize);
      const hits=sections.filter(s=>t.rva>=s.rva&&t.rva+t.bytes<=s.rva+s.size);assert.equal(hits.length,1);
      const section=hits[0];assert(!(section.flags&0x20000000));assert(section.flags&0x40000000);
      const offset=section.offset+t.rva-section.rva;assert(offset>=0&&offset+t.bytes<=size);
      assert(report.data_bytes+t.bytes<=24);const b=Buffer.alloc(t.bytes);
      assert.equal(fs.readSync(fd,b,0,t.bytes,offset),t.bytes);report.data_bytes+=t.bytes;
      const value=b.readBigUInt64LE(0),withinImage=value>=imageBase&&value<imageBase+BigInt(imageSize);
      const candidateRva=withinImage?Number(value-imageBase):null;
      const codeSections=withinImage?sections.filter(s=>(s.flags&0x20000000)&&candidateRva>=s.rva&&candidateRva<s.rva+s.size):[];
      report.values.push({rva:hex(t.rva),va:hex(imageBase+BigInt(t.rva)),file_offset:offset,bytes:t.bytes,section:section.name,
        slot_offset:hex(t.slot_offset),raw_hex:b.toString('hex'),uint64_va:hex(value),
        candidate_rva:candidateRva===null?null:hex(candidateRva),
        classification:codeSections.length===1?'ExecutableAddressCandidate':'Unknown',
        candidate_code_section:codeSections.length===1?codeSections[0].name:null,
        function_boundary_verified:false,manager_binding_verified:report.vtable.manager_b8_binding_verified,
        manager_binding_kind:'static-constructor-dataflow-only',update_this8_binding_verified:false,loaded_value_observed:false});
    }
    assert.equal(report.data_bytes,24);assert.equal(report.values.length,3);
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
  if(report.failures.length)throw Error('Fixed crowd query slots read failed; preserved '+out+': '+report.failures.join('; '));
  return report;
}
