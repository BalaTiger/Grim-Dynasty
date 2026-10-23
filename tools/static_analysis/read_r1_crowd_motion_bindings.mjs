// Preparation only: importing this module performs no file I/O. Explicit execution needs approval.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectCrowdMotionBindings(reviewedScriptSha256, reviewedPlanSha256) {
  assert.equal(process.platform, 'win32');
  const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
  assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self),'read_r1_crowd_motion_bindings.mjs');
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
    {file:'docs/reverse-client/r1-crowd-agent-group-registration/08-CrowdAgentGroupRegistration-0x18025a250-procedure_assembly.json',
      sha256:'c99cc6804a210776df2057c3daa3c2ffee9b4255ea042e67d8c145f761edfda6',procedure:'0x18025a250',
      instructions:['0x18025a271: MOV RSI, RCX','0x18025a3df: MOV RAX, qword ptr [RSI + 0xb8]',
        '0x18025a3f1: MOV qword ptr [RDI + 0x8], RAX','0x18025a3fd: MOV qword ptr [RDI], RSI']},
    {file:'docs/reverse-client/r1-crowd-movement-direct/08-CrowdMovementDirect-0x18025c440-procedure_assembly.json',
      sha256:'62da8b3465041af3522b3e36a72e4680485d0c9b2a832ab8b3207ce27bf5b8ae',procedure:'0x18025c440',
      instructions:['0x18025c4a1: MOVSS XMM13, dword ptr [0x18034b9b0]',
        '0x18025c51f: MOVSS XMM14, dword ptr [0x18034bc70]',
        '0x18025ca4f: MOVSS XMM12, dword ptr [0x18034bc64]',
        '0x18025caa3: MOV RCX, qword ptr [R12 + 0x8]','0x18025caa8: MOV RAX, qword ptr [RCX]',
        '0x18025caab: CALL qword ptr [RAX + 0x8]',
        '0x18025caeb: MOV RAX, qword ptr [R12 + 0x8]','0x18025caf7: MOV RBX, qword ptr [RAX]',
        '0x18025cb04: LEA RDX, [RDI + 0x48]','0x18025cb0b: CALL qword ptr [RBX + 0x10]',
        '0x18025ce50: COMISS XMM3, dword ptr [0x18034b984]']}
  ];
  const targets=[
    {rva:'0x33a6c8',bytes:8,kind:'vtable-slot',slot_offset:'0x8',consumer:'0x18025caab'},
    {rva:'0x33a6d0',bytes:8,kind:'vtable-slot',slot_offset:'0x10',consumer:'0x18025cb0b'},
    {rva:'0x34b9b0',bytes:4,kind:'float32-bits',consumer:'0x18025c4a1'},
    {rva:'0x34b984',bytes:4,kind:'float32-bits',consumer:'0x18025ce50'},
    {rva:'0x34bc64',bytes:4,kind:'float32-bits',consumer:'0x18025ca4f'},
    {rva:'0x34bc70',bytes:4,kind:'float32-bits',consumer:'0x18025c51f'}
  ];
  const expectedPlan={schema:'grim-dynasty.fixed-crowd-motion-bindings.v1',
    script:'tools/static_analysis/read_r1_crowd_motion_bindings.mjs',
    entrypoint:'inspectCrowdMotionBindings(reviewedScriptSha256, reviewedPlanSha256)',
    source:{path:source,bytes:size,sha256:expected},
    output:'docs/reverse-client/r1-crowd-motion-bindings/inspection.json',
    targets,source_pins:proofs.map(p=>({path:p.file,sha256:p.sha256,procedure:p.procedure})),
    limits:{semantic_bytes:32,metadata_bytes:4096,metadata_single_read:512,hash_chunk_bytes:65536,code_bytes:0},
    requirements:['Separate execution approval; this plan is preparation, not evidence that any original was read.',
      'External Windows Attributes/reparse-tag precheck for every source and output ancestor.',
      'Fixed ordinary single-link Engine.dll; same-descriptor SHA256 and identity checks before and after.',
      'New output directory only; never overwrite, delete or reuse a previous output.',
      'No code body, adjacent slot, runtime global, saves, backend, DLL loading or subprocess.',
      'Disk values and static constructor paths do not establish current loaded object or constant state.']};
  const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
  function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
    assert(!fs.lstatSync(q).isSymbolicLink(),'redirected path: '+q);
    assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
    if(path.dirname(q)===q)break;
  }}
  function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
  function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
  assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
  const selfStat=regular(self);assert(selfStat.size<=65536);
  const scriptBytes=fs.readFileSync(self);assert.equal(hash(scriptBytes),reviewedScriptSha256);
  const planPath=path.join(root,'tools/static_analysis/read_r1_crowd_motion_bindings.plan.json');
  const planStat=regular(planPath);assert(planStat.size<=32768);
  const planBytes=fs.readFileSync(planPath);assert.equal(hash(planBytes),reviewedPlanSha256);
  assert.deepEqual(JSON.parse(planBytes),expectedPlan);
  const out=path.join(root,'docs/reverse-client/r1-crowd-motion-bindings');
  normal(path.dirname(out));
  try{fs.lstatSync(out);assert.fail('Existing output is preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
  fs.mkdirSync(out);normal(out);
  const report={started_utc:new Date().toISOString(),source,expected_sha256:expected,expected_size:size,
    script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,proofs,
    metadata_bytes:0,data_bytes:0,hash_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,
    values:[],failures:[],vtable:{rva:'0x33a6c0',static_constructor_binding_verified:false,
      table_extent_verified:false,loaded_value_observed:false,
      chain:['02F800 publishes vptr33A6C0 at outer+0 and passes outer as RDX to25A880.',
        '25A880 stores captured outer into manager+B8.',
        '25A250 new-group path stores manager+B8 into group+8, and manager into group+0.',
        '25C440 reads the group+8 query object before slots+8 and+10. These are not Agent.Callback08 slots.']},
    scope:'Only six fixed semantic fields: two8B slots and four4B floats (32B); PE metadata at most4096B. Whole-file streams only verify SHA256.',
    limitations:['Static constructor binding applies to those completed paths only; later mutable or loaded identities are not observed.',
      'An address in an executable file-backed section is only a candidate, not a proven function boundary or recovered behavior.',
      'Float fields are raw disk bits; no decimal is assumed and no current loaded value is established.',
      'No runtime global initialization value, adjacent slot, code body, DLL execution, backend, other original or save is accessed.',
      'Node cannot expose all Windows reparse tags; separate external Attributes precheck remains required.',
      'Before/after same-descriptor hash and stat equality is not an atomic filesystem snapshot.']};
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
      for(const instruction of proof.instructions)assert(lines.includes(instruction),'Missing proof line: '+instruction);
    }
    report.proofs_verified=true;report.vtable.static_constructor_binding_verified=true;
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
      const rva=Number(t.rva);assert(Number.isSafeInteger(rva)&&rva>=0&&rva+t.bytes<=imageSize);
      if(t.kind==='vtable-slot'){assert.equal(t.bytes,8);assert.equal(rva,0x33a6c0+Number(t.slot_offset));}
      else{assert.equal(t.kind,'float32-bits');assert.equal(t.bytes,4);}
      const hits=sections.filter(s=>rva>=s.rva&&rva+t.bytes<=s.rva+s.size);assert.equal(hits.length,1);
      const section=hits[0];assert(!(section.flags&0x20000000));assert(section.flags&0x40000000);
      const offset=section.offset+rva-section.rva;assert(offset>=0&&offset+t.bytes<=size);
      assert(report.data_bytes+t.bytes<=32);const b=Buffer.alloc(t.bytes);
      assert.equal(fs.readSync(fd,b,0,t.bytes,offset),t.bytes);report.data_bytes+=t.bytes;
      const value={rva:t.rva,va:hex(imageBase+BigInt(rva)),file_offset:offset,bytes:t.bytes,section:section.name,
        kind:t.kind,consumer:t.consumer,raw_hex:b.toString('hex'),loaded_value_observed:false};
      if(t.kind==='vtable-slot'){
        const pointer=b.readBigUInt64LE(0),withinImage=pointer>=imageBase&&pointer<imageBase+BigInt(imageSize);
        const candidateRva=withinImage?Number(pointer-imageBase):null;
        const codeSections=withinImage?sections.filter(s=>(s.flags&0x20000000)&&candidateRva>=s.rva&&candidateRva<s.rva+s.size):[];
        Object.assign(value,{slot_offset:t.slot_offset,uint64_va:hex(pointer),candidate_rva:candidateRva===null?null:hex(candidateRva),
          classification:codeSections.length===1?'ExecutableAddressCandidate':'Unknown',
          candidate_code_section:codeSections.length===1?codeSections[0].name:null,function_boundary_verified:false});
      }else{
        const floating=b.readFloatLE(0);
        Object.assign(value,{uint32_bits:'0x'+b.readUInt32LE(0).toString(16).padStart(8,'0'),
          float32:Number.isFinite(floating)?floating:null,
          float32_text:Number.isNaN(floating)?'NaN':Object.is(floating,-0)?'-0':String(floating),
          classification:'ObservedDiskFloat32Bits'});
      }
      report.values.push(value);
    }
    assert.equal(report.data_bytes,32);assert.equal(report.values.length,6);
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
  if(report.failures.length)throw Error('Fixed crowd motion bindings read failed; preserved '+out+': '+report.failures.join('; '));
  return report;
}
