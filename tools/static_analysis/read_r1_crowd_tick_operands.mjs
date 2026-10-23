// Fixed observations only. Importing this module performs no I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectCrowdTickOperands(reviewedScriptSha256, reviewedPlanSha256) {
 const expectedPlan = {
  "schema": "grim-dynasty.fixed-crowd-tick-operands.v1",
  "script": "tools/static_analysis/read_r1_crowd_tick_operands.mjs",
  "entrypoint": "inspectCrowdTickOperands(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-crowd-tick-operands/inspection.json",
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
      "key": "tick",
      "path": "docs/reverse-client/r1-crowd-movement-direct/08-CrowdMovementDirect-0x18025c440-procedure_assembly.json",
      "sha256": "62da8b3465041af3522b3e36a72e4680485d0c9b2a832ab8b3207ce27bf5b8ae"
    },
    {
      "key": "avoidance",
      "path": "docs/reverse-client/r1-crowd-motion-branches/20-CrowdMotionBranch-0x18025f250-procedure_assembly.json",
      "sha256": "4ccf1f9256b2ab72f5367398a2717d6316e22ca769104f96695dccb90cac9b57"
    },
    {
      "key": "score",
      "path": "docs/reverse-client/r1-crowd-direct-algorithms/11-CrowdDirectAlgorithm-0x18025ef20-procedure_assembly.json",
      "sha256": "f600da86dd2159b8e4bf691f2f3b8e7b31caf6f360766a445d09ad5595d48bdf"
    },
    {
      "key": "reused",
      "path": "docs/reverse-client/r1-mesh-connection-constants/inspection.json",
      "sha256": "e3dab5d3d29a21bbd3c25f71c1bb51831638d0d1414c0be3e5394ba5e5edd0fc"
    }
  ],
  "assembly_proofs": [
    {
      "key": "tick",
      "procedure": "0x18025c440"
    },
    {
      "key": "avoidance",
      "procedure": "0x18025f250"
    },
    {
      "key": "score",
      "procedure": "0x18025ef20"
    }
  ],
  "targets": [
    {
      "rva": "0x34ba9c",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "avoidance",
      "instructions": [
        "0x18025f425: MOVSS XMM14, dword ptr [0x18034ba9c]"
      ]
    },
    {
      "rva": "0x34bad0",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "avoidance",
      "instructions": [
        "0x18025f432: MOVSS XMM15, dword ptr [0x18034bad0]"
      ]
    },
    {
      "rva": "0x34bb18",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "avoidance",
      "instructions": [
        "0x18025f43b: MOVSS XMM8, dword ptr [0x18034bb18]"
      ]
    },
    {
      "rva": "0x34ba80",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "avoidance",
      "instructions": [
        "0x18025f4c1: MULSS XMM9, dword ptr [0x18034ba80]"
      ]
    },
    {
      "rva": "0x34bb08",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "avoidance",
      "instructions": [
        "0x18025f4d4: MULSS XMM6, dword ptr [0x18034bb08]"
      ]
    },
    {
      "rva": "0x34bc00",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "score",
      "instructions": [
        "0x18025ef53: MOVSS XMM12, dword ptr [0x18034bc00]"
      ]
    },
    {
      "rva": "0x34bde8",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "score",
      "instructions": [
        "0x18025efb9: MOVSS XMM14, dword ptr [0x18034bde8]"
      ]
    },
    {
      "rva": "0x34bab8",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "score",
      "instructions": [
        "0x18025f1f2: MULSS XMM6, dword ptr [0x18034bab8]"
      ]
    },
    {
      "rva": "0x34ba34",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "score",
      "instructions": [
        "0x18025f20b: ADDSS XMM6, dword ptr [0x18034ba34]"
      ]
    },
    {
      "rva": "0x34bc38",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "tick",
      "instructions": [
        "0x18025db49: MOVSS XMM9, dword ptr [0x18034bc38]"
      ]
    },
    {
      "rva": "0x34baac",
      "bytes": 4,
      "kind": "constant-dword",
      "key": "tick",
      "instructions": [
        "0x18025db52: MOVSS XMM8, dword ptr [0x18034baac]"
      ]
    },
    {
      "rva": "0x4228c0",
      "bytes": 12,
      "kind": "global-vector",
      "key": "tick",
      "instructions": [
        "0x18025c513: MOVSD XMM0, qword ptr [0x1804228c0]",
        "0x18025c53b: MOV EAX, dword ptr [0x1804228c8]"
      ]
    },
    {
      "rva": "0x422980",
      "bytes": 12,
      "kind": "global-vector",
      "key": "tick",
      "instructions": [
        "0x18025c631: MOVSD XMM0, qword ptr [0x180422980]",
        "0x18025c641: MOV EAX, dword ptr [0x180422988]"
      ]
    }
  ],
  "reused_constants": [
    {
      "key": "reused",
      "rva": "0x34b9e0",
      "bytes": 4,
      "raw_hex": "0ad7233c",
      "uint32_hex": "0x3c23d70a",
      "consumer_key": "avoidance",
      "instruction": "0x18025f2fa: MOVSS XMM10, dword ptr [0x18034b9e0]"
    },
    {
      "key": "reused",
      "rva": "0x34bbd8",
      "bytes": 4,
      "raw_hex": "00000040",
      "uint32_hex": "0x40000000",
      "consumer_key": "score",
      "instruction": "0x18025ef79: MOVSS XMM4, dword ptr [0x18034bbd8]"
    }
  ],
  "limits": {
    "requested_data_bytes": 68,
    "maximum_new_data_bytes": 68,
    "maximum_data_operations": 13,
    "single_read_bytes": 12,
    "hash_chunk_bytes": 65536,
    "new_PE_header_bytes": 0,
    "whole_table_bytes": 0,
    "target_body_bytes": 0
  },
  "requirements": [
    "Import performs no I/O. Execute only after formal review of exact script/plan hashes and separate Windows Attributes/reparse-tag ancestor preflight.",
    "Exactly eleven new DWORD operands and two 12B global references. No original code, new PE headers, adjacent data, imports, exports, or 41D488.",
    "Reuse two pinned observations34B9E0/34BBD8 from the identical Engine SHA; do not reread them.",
    "Classify each requested range against pinned PE virtual/raw extents after the same-descriptor original hash passes. Fully unbacked virtual ranges produce NoDiskBytes records without any data read or synthesized zero.",
    "Reject ambiguous, unmapped, executable, mixed disk/BSS, or unreadable ranges. No partial-range substitution or inferred loaded values.",
    "Before/after whole-file hash streams and descriptor/path stat identity are separate from semantic data reads; they are not an atomic filesystem snapshot.",
    "Absent output directory required; preserve failures. No DLL load/execution, subprocess, backend, network, saves, settings changes, commit or push."
  ]
};

 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');
 assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_crowd_tick_operands.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path: '+q);
  assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
  if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=32768);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_crowd_tick_operands.plan.json');
 assert(regular(planPath).size<=16384);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-crowd-tick-operands');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output is preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const def=expectedPlan.source;
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),
  script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,source:def.path,
  expected_sha256:def.sha256,expected_size:def.bytes,source_pins:expectedPlan.source_pins,
  requested_data_bytes:68,data_requested_bytes:0,data_bytes:0,data_operations:0,data_read_attempts:[],hash_bytes:0,metadata_bytes:0,
  new_PE_header_bytes:0,code_bytes:0,whole_table_bytes:0,subprocesses:0,original_loaded_or_executed:false,
  constants:[],globals:[],reused_constants:[],unbacked_ranges:[],failures:[],
  observations_complete:false,global_runtime_values_observed:false,
  limitations:[
   'Raw disk bits are observations of this fixed Engine image, not loaded values or completed crowd movement.',
   'A NoDiskBytes virtual range has no observed bytes or value. Neither loader zero-fill nor a later runtime initializer is inferred.',
   'Whole-file SHA streams are separate from the maximum68B semantic read scope; before/after checks are not atomic snapshots.',
   'Node path checks cannot identify every Windows reparse tag; separate Windows Attributes preflight remains required.'
  ]};
 const data=new Map(),assemblies=new Map();let fd,initial;
 function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){
  const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);
  h.update(b.subarray(0,n));at+=n;report.hash_bytes+=n;
 }return h.digest('hex');}
 try{
  for(const pin of expectedPlan.source_pins){
   const p=local(pin.path);assert(regular(p).size<1048576);
   const bytes=fs.readFileSync(p);assert.equal(hash(bytes),pin.sha256);data.set(pin.key,JSON.parse(bytes));
  }
  const matches=data.get('engine_pe').files.filter(f=>f.target===def.path);assert.equal(matches.length,1);
  const cached=matches[0];assert.equal(cached.status,'metadata-discovery-completed');
  assert.equal(cached.expected_file_bytes,def.bytes);assert(cached.source_stat_stable&&cached.source_hash_preserved);
  assert.equal(cached.sha256_before,def.sha256);assert.equal(cached.sha256_after,def.sha256);
  assert.equal(cached.preferred_image_base,'0x180000000');const pe=cached.pe;
  assert(Number.isSafeInteger(pe.size_of_image)&&pe.size_of_image>0);
  assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const section of pe.sections){
   for(const k of ['rva','virtual_size','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(section[k])&&section[k]>=0);
   assert(section.rva+Math.max(section.virtual_size,section.raw_size)<=pe.size_of_image);
   assert(section.raw_offset+section.raw_size<=def.bytes);
  }
  for(const proof of expectedPlan.assembly_proofs){
   const sc=data.get(proof.key).result.structuredContent;
   assert.equal(sc.provider.id,'ghidra');assert.equal(sc.operation,'procedure_assembly');
   assert.equal(sc.parameters.procedure,proof.procedure);assert.equal(sc.subject.name,'Engine.dll');
   assert.equal(sc.subject.digest.sha256,def.sha256);assert.equal(path.normalize(sc.subject.local_path),path.normalize(def.path));
   assert.equal(typeof sc.normalized_result,'string');assert(sc.normalized_result.startsWith(proof.procedure+': '));
   assemblies.set(proof.key,sc.normalized_result.split('\n'));
  }
  assert.equal(expectedPlan.targets.length,13);
  assert.equal(expectedPlan.targets.filter(t=>t.bytes===4).length,11);
  assert.equal(expectedPlan.targets.filter(t=>t.bytes===12).length,2);
  assert.equal(new Set(expectedPlan.targets.map(t=>t.rva)).size,13);
  for(const target of expectedPlan.targets)for(const line of target.instructions)assert(assemblies.get(target.key).includes(line),line);
  for(const reuse of expectedPlan.reused_constants){
   assert(assemblies.get(reuse.consumer_key).includes(reuse.instruction),reuse.instruction);
   const prior=data.get(reuse.key);assert.equal(prior.status,'passed');
   assert.equal(prior.source,def.path);assert.equal(prior.expected_size,def.bytes);
   assert(prior.stable&&prior.source_closed);assert.equal(prior.failures.length,0);
   assert.equal(prior.sha256_before,def.sha256);assert.equal(prior.sha256_after,def.sha256);
   const found=prior.values.filter(v=>v.rva===reuse.rva);assert.equal(found.length,1);
   const value=found[0];assert.equal(value.bytes,4);assert.equal(value.raw_hex,reuse.raw_hex);
   assert.equal(value.loaded_value_observed,false);const b=Buffer.from(value.raw_hex,'hex');
   assert.equal(b.length,4);assert.equal(hex(b.readUInt32LE()),reuse.uint32_hex);
   report.reused_constants.push({...reuse,source_pin:expectedPlan.source_pins.find(p=>p.key===reuse.key),loaded_value_observed:false});
  }
  report.proofs_verified=true;
  const before=regular(def.path);assert.equal(before.size,def.bytes);
  fd=fs.openSync(def.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);
  report.sha256_before=digest();assert.equal(report.sha256_before,def.sha256);
  for(const target of expectedPlan.targets){
   const rva=Number(target.rva),n=target.bytes;assert(Number.isSafeInteger(rva)&&rva>=0&&rva+n<=pe.size_of_image);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+Math.max(s.virtual_size,s.raw_size));
   assert.equal(hits.length,1,'Requested range lacks unique PE section: '+target.rva);
   const s=hits[0],delta=rva-s.rva;assert(s.characteristics&0x40000000);assert(!(s.characteristics&0x20000000));
   const base={...target,va:hex(0x180000000n+BigInt(rva)),section:s.name,
    section_rva:hex(s.rva),section_virtual_size:s.virtual_size,section_raw_size:s.raw_size,
    loaded_value_observed:false};
   if(delta>=s.raw_size&&delta+n<=s.virtual_size){
    assert.equal(target.kind,'global-vector','Required constant lacks disk backing');
    const value={...base,classification:'NoDiskBytes',file_offset:null,observed_bytes:0,
     raw_hex:null,uint32_hex:null,value_inferred:false,
     reason:'Entire requested virtual range lies beyond SizeOfRawData and within VirtualSize; runtime value remains unknown'};
    report.globals.push(value);report.unbacked_ranges.push(value);continue;
   }
   assert(delta+n<=s.raw_size,'Mixed disk/BSS range is outside fixed whole-range read domain');
   const offset=s.raw_offset+delta;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);
   assert(report.data_requested_bytes+n<=68);assert(report.data_operations+1<=13);
   const bytes=Buffer.alloc(n),attempt={rva:target.rva,file_offset:offset,requested_bytes:n,returned_bytes:null,outcome:'pending'};
   report.data_read_attempts.push(attempt);report.data_requested_bytes+=n;report.data_operations++;
   let returned;
   try{returned=fs.readSync(fd,bytes,0,n,offset);attempt.returned_bytes=returned;report.data_bytes+=returned;attempt.outcome=returned===n?'complete':'short-read';}
   catch(e){attempt.outcome='error';attempt.error=String(e);throw e;}
   assert.equal(returned,n,'Incomplete fixed read; no value accepted');
   const value={...base,classification:'ObservedDiskBytes',file_offset:offset,observed_bytes:n,
    raw_hex:bytes.toString('hex'),
    uint32_hex:Array.from({length:n/4},(_,i)=>'0x'+bytes.readUInt32LE(i*4).toString(16).padStart(8,'0'))};
   (target.kind==='constant-dword'?report.constants:report.globals).push(value);
  }
  assert.equal(report.constants.length,11);assert.equal(report.globals.length,2);
  assert.equal(report.data_bytes+report.unbacked_ranges.reduce((n,r)=>n+r.bytes,0),68);
  report.observations_complete=true;
 }catch(e){report.failures.push(String(e));}
 finally{
  if(fd!==undefined){try{
   report.sha256_after=digest();assert.equal(report.sha256_after,def.sha256);
   sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);report.stable=true;
  }catch(e){report.failures.push(String(e));}
  finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push(String(e));}}}
 }
 report.finished_utc=new Date().toISOString();
 report.status=report.failures.length?'failed':report.unbacked_ranges.length?'passed-with-unbacked-ranges':'passed';
 if(!report.observations_complete&&!report.failures.length){report.status='failed';report.failures.push('Incomplete observation');}
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<=65536);
 normal(out);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw Error('Fixed crowd operands failed; evidence preserved: '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,data_bytes:report.data_bytes,data_operations:report.data_operations,
  metadata_bytes:report.metadata_bytes,unbacked_ranges:report.unbacked_ranges.map(r=>({rva:r.rva,bytes:r.bytes,classification:r.classification})),
  sha256_before:report.sha256_before,sha256_after:report.sha256_after,stable:report.stable,source_closed:report.source_closed};
}
