// No I/O at import. This prepared reader requires separate explicit execution approval.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectPlayerMovementInterfaceSlots(reviewedScriptSha256, reviewedPlanSha256) {
  assert.equal(process.platform,'win32');
  const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
  assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self),'read_r1_player_movement_interface_slots.mjs');
  const source='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856;
  const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
  const proofs=[
    {file:'docs/reverse-client/r1-game-entity-constructors/05-GameEntityConstructor-0x180052030-procedure_assembly.json',
      sha256:'34a334ae8f918b20469fb57e7d55af0ffbcfba0d5900c42a4e371192d534f856',procedure:'0x180052030',
      instructions:['0x180052055: MOV R15, RCX','0x180052058: CALL qword ptr [0x180609700]',
        '0x180052074: LEA RAX, [0x1806df690]','0x18005207b: MOV qword ptr [R15 + 0x38], RAX',
        '0x180052e42: MOV R8, R15','0x180052e4f: CALL 0x18052b360',
        '0x180052e6b: MOV RDX, R15','0x180052e71: CALL 0x18008e0b0',
        '0x180052f08: MOV RCX, R15','0x180052f0b: CALL 0x18005b2f0',
        '0x180052ffd: MOV RAX, R15','0x180053017: RET']},
    {file:'docs/reverse-client/r1-game-entity-constructors/08-GameEntityConstructor-0x1803cc3f0-procedure_assembly.json',
      sha256:'d68276f2f2212942eb66cf2180f34f0b527f5b9a48f473b4ac35098b3d479ded',procedure:'0x1803cc3f0',
      instructions:['0x1803cc415: MOV RDI, RCX','0x1803cc418: CALL 0x180052030',
        '0x1803cc433: LEA RAX, [0x180787080]','0x1803cc43a: MOV qword ptr [RDI + 0x38], RAX',
        '0x1803ccbfd: MOV RCX, RDI','0x1803ccc00: CALL 0x1803d31e0',
        '0x1803ccd03: MOV RCX, RDI','0x1803ccd06: CALL 0x1803d8e40',
        '0x1803ccf7e: MOV RAX, RDI','0x1803ccf98: RET']}
  ];
  const targets=[
    {owner:'Character',table_rva:'0x6df690',slot_offset:'0x0',rva:'0x6df690',bytes:8},
    {owner:'Character',table_rva:'0x6df690',slot_offset:'0x10',rva:'0x6df6a0',bytes:8},
    {owner:'Character',table_rva:'0x6df690',slot_offset:'0x18',rva:'0x6df6a8',bytes:8},
    {owner:'Character',table_rva:'0x6df690',slot_offset:'0x28',rva:'0x6df6b8',bytes:8},
    {owner:'Player',table_rva:'0x787080',slot_offset:'0x0',rva:'0x787080',bytes:8},
    {owner:'Player',table_rva:'0x787080',slot_offset:'0x10',rva:'0x787090',bytes:8},
    {owner:'Player',table_rva:'0x787080',slot_offset:'0x18',rva:'0x787098',bytes:8},
    {owner:'Player',table_rva:'0x787080',slot_offset:'0x28',rva:'0x7870a8',bytes:8}
  ];
  const expectedPlan={schema:'grim-dynasty.fixed-player-movement-interface-slots.v1',
    script:'tools/static_analysis/read_r1_player_movement_interface_slots.mjs',
    entrypoint:'inspectPlayerMovementInterfaceSlots(reviewedScriptSha256, reviewedPlanSha256)',
    source:{path:source,bytes:size,sha256:expected},
    output:'docs/reverse-client/r1-player-movement-interface-slots/inspection.json',targets,
    source_pins:proofs.map(p=>({path:p.file,sha256:p.sha256,procedure:p.procedure})),
    limits:{semantic_bytes:64,metadata_bytes:4096,metadata_single_read:512,hash_chunk_bytes:65536,code_bytes:0},
    requirements:['Separate explicit execution approval; preparing this plan does not read the original.',
      'External Windows Attributes/reparse-tag checks for source and output ancestors.',
      'Fixed ordinary single-link Game.dll; same-descriptor SHA256 and identity checks before and after.',
      'Only offsets00/10/18/28 of the two fixed tables; no adjacent slot or function body.',
      'New output directory and exclusive writes; preserve failure evidence and never overwrite.',
      'Constructor writes and Player overwrite are static evidence, not complete constructor or loaded-object semantics.',
      'No DLL loading, original execution, backend, saves, runtime globals or subprocess.']};
  const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
  function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
    assert(!fs.lstatSync(q).isSymbolicLink(),'redirected path: '+q);
    assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
    if(path.dirname(q)===q)break;
  }}
  function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
  function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
  assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
  assert(regular(self).size<=65536);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
  const planPath=path.join(root,'tools/static_analysis/read_r1_player_movement_interface_slots.plan.json');
  assert(regular(planPath).size<=32768);const planBytes=fs.readFileSync(planPath);
  assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
  const out=path.join(root,'docs/reverse-client/r1-player-movement-interface-slots');normal(path.dirname(out));
  try{fs.lstatSync(out);assert.fail('Existing output is preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
  fs.mkdirSync(out);normal(out);
  const report={started_utc:new Date().toISOString(),source,expected_sha256:expected,expected_size:size,
    script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,proofs,
    metadata_bytes:0,data_bytes:0,hash_bytes:0,code_bytes:0,subprocesses:0,original_executed:false,
    values:[],failures:[],constructors:{static_writers_verified:false,player_overwrite_verified:false,
      interface_offset:'0x38',character_write:'0x18005207b',character_table:'0x6df690',
      player_base_call:'0x1803cc418',player_write:'0x1803cc43a',player_table:'0x787080',
      complete_semantics_verified:false,loaded_pointer_observed:false,
      saved_caller_review:'Both complete saved caller bodies were reviewed. Each has one explicit write to its own this+38; Player calls052030 before writing its distinct table.',
      call_effects_unresolved:'Character subsequently passes this to52B360/08E0B0/05B2F0 and Player to3D31E0/3D8E40, among other calls/publications. Their transitive writes and runtime effects are not excluded here.'},
    scope:'Exactly eight QWORDs (64B) at offsets00/10/18/28 of constructor-written tables6DF690/787080, plus at most4096B PE metadata. Whole-file streams only verify SHA256.',
    limitations:['Only the two exact constructor writers and Player base-to-derived overwrite are established. Complete constructors, subsequent callee effects and loaded identities remain unverified.',
      'Character table must not be substituted for the explicitly overwritten Player table.',
      'Executable-section classification identifies a static address candidate only; no function boundary, thunk adjustment, callback ABI or behavior is inferred.',
      'Full vtable extents and adjacent offsets08/20/30/38 are not read or established.',
      'No function body, runtime global, DLL execution, backend, other original file or saves are accessed.',
      'Node does not expose all Windows reparse tags; external Attributes precheck remains necessary.',
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
      const proofFile=path.join(root,proof.file);assert(regular(proofFile).size<=1024*1024);
      const bytes=fs.readFileSync(proofFile);assert.equal(hash(bytes),proof.sha256);
      const e=JSON.parse(bytes).result.structuredContent;
      assert.equal(e.subject.name,'Game.dll');assert.equal(e.subject.digest.sha256,expected);
      assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(source).toLowerCase());
      assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,proof.procedure);
      assert.equal(typeof e.normalized_result,'string');const lines=e.normalized_result.split('\n');
      for(const instruction of proof.instructions)assert(lines.includes(instruction),'Missing proof line: '+instruction);
    }
    report.proofs_verified=true;report.constructors.static_writers_verified=true;report.constructors.player_overwrite_verified=true;
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
      const rva=Number(t.rva);assert.equal(t.bytes,8);assert.equal(rva,Number(t.table_rva)+Number(t.slot_offset));
      assert(Number.isSafeInteger(rva)&&rva>=0&&rva+t.bytes<=imageSize);
      const hits=sections.filter(s=>rva>=s.rva&&rva+t.bytes<=s.rva+s.size);assert.equal(hits.length,1);
      const section=hits[0];assert(!(section.flags&0x20000000));assert(section.flags&0x40000000);
      const offset=section.offset+rva-section.rva;assert(offset>=0&&offset+t.bytes<=size);
      assert(report.data_bytes+t.bytes<=64);const b=Buffer.alloc(t.bytes);
      assert.equal(fs.readSync(fd,b,0,t.bytes,offset),t.bytes);report.data_bytes+=t.bytes;
      const pointer=b.readBigUInt64LE(0),withinImage=pointer>=imageBase&&pointer<imageBase+BigInt(imageSize);
      const candidateRva=withinImage?Number(pointer-imageBase):null;
      const codeSections=withinImage?sections.filter(s=>(s.flags&0x20000000)&&candidateRva>=s.rva&&candidateRva<s.rva+s.size):[];
      report.values.push({owner:t.owner,table_rva:t.table_rva,slot_offset:t.slot_offset,rva:t.rva,
        va:hex(imageBase+BigInt(rva)),file_offset:offset,bytes:t.bytes,section:section.name,raw_hex:b.toString('hex'),
        uint64_va:hex(pointer),candidate_rva:candidateRva===null?null:hex(candidateRva),
        classification:codeSections.length===1?'ExecutableAddressCandidate':'Unknown',
        candidate_code_section:codeSections.length===1?codeSections[0].name:null,
        function_boundary_verified:false,callback_abi_verified:false,loaded_value_observed:false});
    }
    assert.equal(report.data_bytes,64);assert.equal(report.values.length,8);
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
  if(report.failures.length)throw Error('Fixed Player movement interface slots read failed; preserved '+out+': '+report.failures.join('; '));
  return report;
}
