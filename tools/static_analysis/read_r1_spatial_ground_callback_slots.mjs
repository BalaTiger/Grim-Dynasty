// Fixed disk observations only. Importing this module performs no I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectSpatialGroundCallbackSlots(reviewedScriptSha256, reviewedPlanSha256) {
 const expectedPlan = {
  "schema": "grim-dynasty.fixed-spatial-ground-callback-slots.v1",
  "script": "tools/static_analysis/read_r1_spatial_ground_callback_slots.mjs",
  "entrypoint": "inspectSpatialGroundCallbackSlots(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-spatial-ground-callback-slots/inspection.json",
  "source": {
    "path": "D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll",
    "bytes": 4485632,
    "sha256": "4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153"
  },
  "source_pins": [
    {
      "key": "engine_pe",
      "path": "docs/object-lifecycle/offline-class-registration-exports/discovery.json",
      "sha256": "0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58"
    },
    {
      "key": "constructor",
      "path": "docs/reverse-client/r1-engine-spatial-ground/14-EngineSpatialGround-0x1801dda90-procedure_assembly.json",
      "sha256": "858c4ee47d193164648b7fb068b0dfb2772dc6cd35347055d736d2584388a629"
    },
    {
      "key": "spatial_assembly",
      "path": "docs/reverse-client/r1-engine-spatial-ground/05-EngineSpatialGround-0x180134500-procedure_assembly.json",
      "sha256": "6dfe2938844693c4bffecd4c9340076fc0278c28c195bc6f4bfa669eddecf0fb"
    },
    {
      "key": "node_caller",
      "path": "docs/reverse-client/r1-spatial-ground-direct/08-SpatialGroundDirect-0x18011bbc0-procedure_assembly.json",
      "sha256": "1f8618dfe1a6c815e9bb5e75f2fb9d87ec273acea425e49c929d1ebe7fa16416"
    },
    {
      "key": "ground_caller",
      "path": "docs/reverse-client/r1-spatial-ground-direct/11-SpatialGroundDirect-0x1801de090-procedure_assembly.json",
      "sha256": "280b8fc0515859b8de565e69e9ee40dcb047d76631ca4c9e368d3177faa105ff"
    },
    {
      "key": "loader_binding",
      "path": "docs/reverse-client/r1-ground-loader-slot/inspection.json",
      "sha256": "aa504399179a7ff4d570d967544706e8ccc988e88cb34a7f282beb3514700215"
    }
  ],
  "assembly_proofs": [
    {
      "key": "constructor",
      "procedure": "0x1801dda90",
      "instructions": [
        "0x1801ddaad: MOV RDI, RCX",
        "0x1801ddabe: LEA RAX, [0x180343b60]",
        "0x1801ddac5: MOV qword ptr [RDI], RAX",
        "0x1801ddac8: LEA RBX, [RDI + 0x68]",
        "0x1801ddad9: LEA RAX, [0x18033e338]",
        "0x1801ddae0: MOV qword ptr [RBX], RAX",
        "0x1801ddbb7: MOV RAX, RDI"
      ]
    },
    {
      "key": "spatial_assembly",
      "procedure": "0x180134500",
      "instructions": [
        "0x18013465c: CALL 0x1802dc398",
        "0x180134667: MOV RDX, RAX",
        "0x180134676: LEA RAX, [0x180340e60]",
        "0x180134685: MOV qword ptr [RDX], RAX"
      ]
    },
    {
      "key": "node_caller",
      "procedure": "0x18011bbc0",
      "instructions": [
        "0x18011bbf8: TEST RCX, RCX",
        "0x18011bbfd: MOV RAX, qword ptr [RCX]",
        "0x18011bc00: MOV EDX, 0x1",
        "0x18011bc05: CALL qword ptr [RAX]",
        "0x18011bc07: MOV qword ptr [RBX], R15"
      ]
    },
    {
      "key": "ground_caller",
      "procedure": "0x1801de090",
      "instructions": [
        "0x1801de0b4: MOV RDI, RDX",
        "0x1801de0b7: MOV R14, RCX",
        "0x1801de0ba: MOV RAX, qword ptr [RCX]",
        "0x1801de0bd: CALL qword ptr [RAX + 0x68]",
        "0x1801de2c5: MOV RAX, qword ptr [R14]",
        "0x1801de2c8: MOV EDX, ESI",
        "0x1801de2ca: MOV RCX, R14",
        "0x1801de2cd: CALL qword ptr [RAX + 0x188]"
      ]
    }
  ],
  "limits": {
    "new_data_bytes": 24,
    "data_operations": 3,
    "single_read_bytes": 8,
    "hash_chunk_bytes": 65536,
    "new_PE_header_bytes": 0,
    "whole_table_bytes": 0,
    "target_body_bytes": 0
  },
  "requirements": [
    "Import performs no I/O. Execute only after exact approval of frozen script/plan and separate Windows Attributes/reparse-tag preflight.",
    "Only three Engine data slots:340E60 8B,343BC8 8B,343CE8 8B. No new PE headers,code,RTTI,neighbor slots,imports,exports or constants.",
    "134500 writes340E60 to allocated node primary vptr;11BBC0 calls matching nonnull child slot0 withEDX=1. This does not prove all runtime child dynamic types.",
    "1DDA90 writes343B60 to main vptr;33E338 atthis+68 is separate. Existing343BE8 binding selected1DE090,whose entry/per-type calls use+68/+188. Later vptr overwrite/table extent remain unverified.",
    "Pinned cached PE map requires same-descriptor whole-file source hash match. Before/after hash/stat/path checks are not atomic snapshots.",
    "Preserve raw bytes and executable-address candidates without guessed names,body,ABI or loaded-state behavior.",
    "Output directory must be absent;retain failures. No original loading/execution,backend,subprocess,network,saves,security changes,commit or push."
  ],
  "fixed_slots": [
    {
      "owner": "SpatialNode",
      "table_rva": "0x340e60",
      "slot_offset": "0x0",
      "rva": "0x340e60",
      "bytes": 8,
      "callsite": "0x18011bc05",
      "callsite_role": "Conditional nonnull matching child receiver,EDX=1;target role remains unproved"
    },
    {
      "owner": "GroundPrimary",
      "table_rva": "0x343b60",
      "slot_offset": "0x68",
      "rva": "0x343bc8",
      "bytes": 8,
      "callsite": "0x1801de0bd",
      "callsite_role": "Ground loader entry virtual call before reader consumption;reset semantics remain to be verified"
    },
    {
      "owner": "GroundPrimary",
      "table_rva": "0x343b60",
      "slot_offset": "0x188",
      "rva": "0x343ce8",
      "bytes": 8,
      "callsite": "0x1801de2cd",
      "callsite_role": "Per-type virtual call withEDX=ESI andRCX=R14 after current record processing;body unknown"
    }
  ],
  "direct_call_note": {
    "procedure": "0x1801de090",
    "instruction": "0x1801de303: CALL 0x1801ce910",
    "argument_instruction": "0x1801de300: MOV RCX, R14",
    "scope": "Recorded from saved assembly only;not read or queried by this reader. Semantic ConstructBlocks name is not established by CALL alone."
  }
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_spatial_ground_callback_slots.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path: '+q);
  assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=24576);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_spatial_ground_callback_slots.plan.json');
 assert(regular(planPath).size<=12288);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-spatial-ground-callback-slots');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const def=expectedPlan.source;
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:expectedPlan.source_pins,code_bytes:0,metadata_bytes:0,data_bytes:0,data_operations:0,new_PE_header_bytes:0,whole_table_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,files:[],values:[],disk_observations_complete:false,unsupported:[],failures:[],
  limitations:[
   'Three exact primary-vtable slot candidates only; no table extent, later overwrite, loaded dispatch, function body or ABI is proved.',
   'Node child dynamic types and actual final ground vptr remain runtime unknowns; the existing ground+88 binding only motivates this fixed disk table selection.',
   'Whole-file hash streams are distinct from the24B semantic read budget. Before/after checks are not atomic snapshots.',
   'Node path checks cannot identify every Windows reparse tag; separate Attributes preflight remains required.'
  ]};
 const data=new Map();
 function unsupported(reason){const e=new Error(reason);e.fixedUnsupported=true;throw e;}
 try{
  for(const pin of expectedPlan.source_pins){const p=local(pin.path);assert(regular(p).size<1048576);const b=fs.readFileSync(p);assert.equal(hash(b),pin.sha256);data.set(pin.key,JSON.parse(b));}
  const matches=data.get('engine_pe').files.filter(f=>f.target===def.path);assert.equal(matches.length,1);const cached=matches[0];
  assert.equal(cached.status,'metadata-discovery-completed');assert.equal(cached.expected_file_bytes,def.bytes);
  assert(cached.source_stat_stable&&cached.source_hash_preserved);assert.equal(cached.sha256_before,def.sha256);assert.equal(cached.sha256_after,def.sha256);
  assert.equal(cached.preferred_image_base,'0x180000000');const pe=cached.pe;
  assert(Number.isSafeInteger(pe.size_of_image)&&pe.size_of_image>0);assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections)for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);
  for(const proof of expectedPlan.assembly_proofs){const sc=data.get(proof.key).result.structuredContent;
   assert.equal(sc.operation,'procedure_assembly');assert.equal(sc.parameters.procedure,proof.procedure);
   assert.equal(sc.subject.digest.sha256,def.sha256);assert.equal(path.normalize(sc.subject.local_path),path.normalize(def.path));
   assert.equal(typeof sc.normalized_result,'string');assert(sc.normalized_result.startsWith(proof.procedure+': '));
   const lines=sc.normalized_result.split('\n');for(const line of proof.instructions)assert(lines.includes(line),line);
  }
  const binding=data.get('loader_binding');assert.equal(binding.status,'passed');assert(binding.proofs_verified&&binding.disk_observations_complete);
  assert.equal(binding.failures.length,0);assert.equal(binding.unsupported.length,0);
  const boundFiles=binding.files.filter(f=>f.source===def.path);assert.equal(boundFiles.length,1);
  assert(boundFiles[0].stable&&boundFiles[0].closed);assert.equal(boundFiles[0].sha256_before,def.sha256);assert.equal(boundFiles[0].sha256_after,def.sha256);
  const boundSlots=binding.values.filter(v=>v.table_rva==='0x343b60'&&v.slot_offset==='0x88');assert.equal(boundSlots.length,1);
  assert.equal(boundSlots[0].rva,'0x343be8');assert.equal(boundSlots[0].bytes,8);assert.equal(boundSlots[0].raw_hex,'90e01d8001000000');assert.equal(boundSlots[0].target_va,'0x1801de090');
  for(const slot of expectedPlan.fixed_slots){assert.equal(Number(slot.table_rva)+Number(slot.slot_offset),Number(slot.rva));assert.equal(slot.bytes,8);}
  report.proofs_verified=true;
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){
   const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
  }return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};}
  function read(rvaText,n,kind){assert.equal(n,8);const {section,offset}=map(Number(rvaText),n);
   assert(section.characteristics&0x40000000);assert(!(section.characteristics&0x20000000));
   assert(report.data_bytes+n<=24);assert(report.data_operations+1<=3);report.data_bytes+=n;report.data_operations++;
   const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);
   const observation={kind,rva:rvaText,file_offset:offset,section:section.name,bytes:n,raw_hex:b.toString('hex')};f.reads.push(observation);return {b,observation};
  }
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);
   initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   for(const slot of expectedPlan.fixed_slots){
    const got=read(slot.rva,slot.bytes,'fixed-primary-vtable-slot'),va=got.b.readBigUInt64LE();
    const value={...slot,...got.observation,target_va:hex(va),candidate_rva:null,classification:null,function_name:null,function_boundary_verified:false,loaded_value_observed:false,table_extent_verified:false,subsequent_overwrites_excluded:false};report.values.push(value);
    if(va<0x180000000n||va>=0x180000000n+BigInt(pe.size_of_image))unsupported(slot.owner+' slot target outside cached Engine image');
    const target=Number(va-0x180000000n);value.candidate_rva=hex(target);
    const codeSections=pe.sections.filter(s=>target>=s.rva&&target<s.rva+s.raw_size);
    if(codeSections.length!==1||!(codeSections[0].characteristics&0x20000000))unsupported(slot.owner+' slot lacks unique executable disk-backed section');
    value.classification='ExecutableAddressCandidate';value.candidate_code_section=codeSections[0].name;
   }
   assert.equal(report.values.length,3);assert.equal(report.data_bytes,24);assert.equal(report.data_operations,3);
   report.disk_observations_complete=true;
  }finally{if(fd!==undefined){try{
   f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;
  }finally{fs.closeSync(fd);f.closed=true;}}}
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.disk_observations_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete observation without explicit unsupported reason');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<65536);normal(out);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed spatial/ground callback slot read failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,data_bytes:report.data_bytes,metadata_bytes:report.metadata_bytes,values:report.values,unsupported:report.unsupported,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
