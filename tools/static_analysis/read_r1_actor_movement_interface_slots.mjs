// No file I/O at import. Explicit invocation requires separate execution approval.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectActorMovementInterfaceSlots() {
  assert.equal(process.platform,'win32');
  const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
  assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self),'read_r1_actor_movement_interface_slots.mjs');
  const source='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632;
  const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
  const proofs=[
    {kind:'saved-dumpbin',file:'docs/object-lifecycle/offline-actor-constructor-body/0259a0-instructions.json',
      sha256:'59f402f428e04e2928bb485e58fa85896ac08db4548bff6fa33444a58fa521a7',
      provenance:{file:'docs/object-lifecycle/offline-actor-constructor-body/inspection.json',
        sha256:'2cc7daf8bf08ed9040dd447c0ff183fc3ff7a86afaa71eaa41672d22a7be541d',
        schema:'grim-dynasty-fixed-actor-constructor-body-v1',status:'fixed-actor-constructor-body-disassembly-verified',requested_rva:'0x259a0'},
      instructions:[
        {address:'0x1800259b8',bytes:'488bd9',instruction:'mov         rbx,rcx'},
        {address:'0x1800259bb',bytes:'e860760500',instruction:'call        000000018007D020'},
        {address:'0x1800259d6',bytes:'488d059b963100',instruction:'lea         rax,[000000018033F078h]'},
        {address:'0x1800259dd',bytes:'48894338',instruction:'mov         qword ptr [rbx+38h],rax'}]},
    {kind:'saved-dumpbin',file:'docs/object-lifecycle/offline-actor-base-dependency/07d020-instructions.json',
      sha256:'64d3237ddb1279a40f12c471d45a9fd02e9b9b043ed00aa52be6730dcf7b4671',
      provenance:{file:'docs/object-lifecycle/offline-actor-base-dependency/inspection.json',
        sha256:'cdbd4ea50af88e8230242625ee267b0325f3162d4ce27db0799a09c0ad9b0fdb',
        schema:'grim-dynasty-fixed-actor-direct-dependency-v1',status:'fixed-actor-direct-dependency-disassembly-verified',requested_rva:'0x7d020'},
      instructions:[
        {address:'0x18007d07e',bytes:'488d05f31f2c00',instruction:'lea         rax,[000000018033F078h]'},
        {address:'0x18007d085',bytes:'48894138',instruction:'mov         qword ptr [rcx+38h],rax'}]},
    {kind:'saved-rea',file:'docs/reverse-client/r1-crowd-adapter-registration/14-CrowdAdapterRegistration-0x180151020-procedure_assembly.json',
      sha256:'d5cbc4a5441fe3d3324511e9e5713f4f7b16c4bc1ddb1968afe21e8b61baa2f4',procedure:'0x180151020',
      instructions:['0x180151024: MOV RAX, RDX','0x180151027: ADD RDX, 0x38','0x18015102b: XOR R8D, R8D',
        '0x18015102e: TEST RAX, RAX','0x180151035: CMOVZ RDX, R8','0x180151039: JMP 0x18025ae20']},
    {kind:'saved-rea',file:'docs/reverse-client/r1-crowd-vector-registration/05-CrowdVectorRegistration-0x18025ae20-procedure_assembly.json',
      sha256:'213066b1cabd9009592b49353325fc676f83d3927c79d6fbcac26706e41f6862',procedure:'0x18025ae20',
      instructions:['0x18025ae3f: MOV RSI, RDX','0x18025ae7e: MOV RDX, RSI','0x18025ae81: MOV RCX, RAX','0x18025ae84: CALL 0x18025e6c0']},
    {kind:'saved-rea',file:'docs/reverse-client/r1-crowd-agent-group-registration/05-CrowdAgentGroupRegistration-0x18025e6c0-procedure_assembly.json',
      sha256:'f8b74af8241717927edf965cbc2ecedd3233f7e127a0e2b90a35a8142e542dd6',procedure:'0x18025e6c0',
      instructions:['0x18025e6e4: MOV RBX, RCX','0x18025e6ed: MOV qword ptr [RCX + 0x8], RDX']}
  ];
  const tableRva=0x33f078,targets=[
    {rva:0x33f078,bytes:8,slot_offset:0},
    {rva:0x33f088,bytes:8,slot_offset:0x10},
    {rva:0x33f090,bytes:8,slot_offset:0x18},
    {rva:0x33f0a0,bytes:8,slot_offset:0x28}
  ];
  const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
  function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
    assert(!fs.lstatSync(q).isSymbolicLink(),'redirected path: '+q);
    assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
    if(path.dirname(q)===q)break;
  }}
  function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
  function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
  function readProof(proof){const f=path.join(root,proof.file);assert(regular(f).size<=1024*1024);
    const bytes=fs.readFileSync(f);assert.equal(hash(bytes),proof.sha256);return JSON.parse(bytes);}
  regular(self);
  const out=path.join(root,'docs/reverse-client/r1-actor-movement-interface-slots');
  normal(path.dirname(out));
  try{fs.lstatSync(out);assert.fail('Existing output is preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
  fs.mkdirSync(out);normal(out);
  const report={started_utc:new Date().toISOString(),source,expected_sha256:expected,expected_size:size,
    script_sha256:hash(fs.readFileSync(self)),proofs,metadata_bytes:0,data_bytes:0,hash_bytes:0,
    code_bytes:0,subprocesses:0,original_executed:false,values:[],failures:[],
    vtable:{rva:hex(tableRva),object_field_offset:'0x38',constructors:['0x1800259a0','0x18007d020'],
      table_extent_verified:false,actor_base_constructor_binding_verified:false,
      agent8_entity38_argument_chain_verified:false,game_player_derived_vptr_verified:false,
      runtime_dynamic_dispatch_verified:false,binding_kind:'static-base-constructor-and-argument-dataflow-only',
      binding_chain:[
        '07D07E/07D085 write vptr33F078 at incoming this+38;0259B8 captures this in RBX and0259BB calls07D020 before0259D6/0259DD write the same vptr at this+38.',
        '151024..151035 form incoming Entity+38 while retaining null as null;151039 transfers that pointer to25AE20.',
        '25AE3F captures incoming RDX in RSI;25AE7E/25AE84 pass it to25E6C0;25E6ED stores it at new agent+8.',
        'This connects an interface pointer location, not the runtime dynamic type. A later Game Player derived constructor may replace the interface vptr; its value is not established here.'
      ]},
    scope:'PE headers/section metadata at most4096B, then exactly four separate QWORDs (32B total) at RVA33F078,33F088,33F090,33F0A0. Slots08/20 and every other entry are excluded. Entire-file streams are only before/after SHA256.',
    limitations:[
      'Only disk addresses at Actor-base vptr offsets00/10/18/28 are reported. Full table extent, slot roles, function semantics and loaded values remain unverified.',
      'Saved dumpbin instruction evidence supplements saved Rea assembly; the former is not described as Rea pseudocode.',
      'The actor-base constructor writes and registration argument chain do not establish a Game Player derived vptr or runtime dynamic dispatch.',
      'A value in a file-backed executable section is only an executable-address candidate, not a verified function boundary or callable implementation.',
      'No adjacent entries, function body, DLL execution, backend, other original file or saves are accessed.',
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
      const data=readProof(proof);
      if(proof.kind==='saved-dumpbin'){
        const p=readProof(proof.provenance);
        assert.equal(p.schema,proof.provenance.schema);assert.equal(p.status,proof.provenance.status);
        assert.equal(p.requested_rva,proof.provenance.requested_rva);
        assert.equal(path.resolve(p.original).toLowerCase(),path.resolve(source).toLowerCase());
        assert.equal(p.expected_sha256,expected);assert.equal(p.expected_file_bytes,size);
        assert.equal(p.sha256_before,expected);assert.equal(p.sha256_after,expected);
        for(const k of ['byte_coverage_verified','source_stat_stable','source_handle_closed','source_hash_preserved'])assert.equal(p[k],true);
        for(const k of ['target_loaded_or_executed','backend_started','original_modified','build_or_test_execution','permission_change'])assert.equal(p[k],false);
        assert.equal(p.target_open_mode,'O_RDONLY');assert.equal(p.tool_exit,0);assert.equal(p.tool_error,null);assert.deepEqual(p.failures,[]);
        assert(Array.isArray(data));
        for(const instruction of proof.instructions){const matches=data.filter(x=>x.address===instruction.address);assert.equal(matches.length,1);assert.deepEqual(matches[0],instruction);}
      }else{
        assert.equal(proof.kind,'saved-rea');const e=data.result.structuredContent;
        assert.equal(e.subject.name,'Engine.dll');assert.equal(e.subject.digest.sha256,expected);
        assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(source).toLowerCase());
        assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,proof.procedure);
        assert.equal(typeof e.normalized_result,'string');const lines=e.normalized_result.split('\n');
        for(const instruction of proof.instructions)assert(lines.includes(instruction));
      }
    }
    report.proofs_verified=true;report.vtable.actor_base_constructor_binding_verified=true;
    report.vtable.agent8_entity38_argument_chain_verified=true;
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
      assert(report.data_bytes+t.bytes<=32);const b=Buffer.alloc(t.bytes);
      assert.equal(fs.readSync(fd,b,0,t.bytes,offset),t.bytes);report.data_bytes+=t.bytes;
      const value=b.readBigUInt64LE(0),withinImage=value>=imageBase&&value<imageBase+BigInt(imageSize);
      const candidateRva=withinImage?Number(value-imageBase):null;
      const codeSections=withinImage?sections.filter(s=>(s.flags&0x20000000)&&candidateRva>=s.rva&&candidateRva<s.rva+s.size):[];
      report.values.push({rva:hex(t.rva),va:hex(imageBase+BigInt(t.rva)),file_offset:offset,bytes:t.bytes,section:section.name,
        slot_offset:hex(t.slot_offset),raw_hex:b.toString('hex'),uint64_va:hex(value),
        candidate_rva:candidateRva===null?null:hex(candidateRva),
        classification:codeSections.length===1?'ExecutableAddressCandidate':'Unknown',
        candidate_code_section:codeSections.length===1?codeSections[0].name:null,slot_role:'unknown',
        function_boundary_verified:false,table_extent_verified:false,
        game_player_derived_vptr_verified:false,runtime_dynamic_dispatch_verified:false,loaded_value_observed:false});
    }
    assert.equal(report.data_bytes,32);assert.equal(report.values.length,4);
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
  if(report.failures.length)throw Error('Fixed Actor interface slots read failed; preserved '+out+': '+report.failures.join('; '));
  return report;
}
