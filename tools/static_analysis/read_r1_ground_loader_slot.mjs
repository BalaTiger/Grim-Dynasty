// Fixed disk observations only. Importing this module performs no I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectGroundLoaderSlot(reviewedScriptSha256, reviewedPlanSha256) {
 const expectedPlan = {
  "schema": "grim-dynasty.fixed-ground-loader-slot.v1",
  "script": "tools/static_analysis/read_r1_ground_loader_slot.mjs",
  "entrypoint": "inspectGroundLoaderSlot(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-ground-loader-slot/inspection.json",
  "source": {
   "path": "D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll",
   "bytes": 4485632,
   "sha256": "4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153"
  },
  "source_pins": [
   {"key":"engine_pe","path":"docs/object-lifecycle/offline-class-registration-exports/discovery.json","sha256":"0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58"},
   {"key":"constructor","path":"docs/reverse-client/r1-engine-spatial-ground/14-EngineSpatialGround-0x1801dda90-procedure_assembly.json","sha256":"858c4ee47d193164648b7fb068b0dfb2772dc6cd35347055d736d2584388a629"},
   {"key":"caller","path":"docs/reverse-client/r1-level-load-and-navjob-candidates/05-LevelLoadNavJobCandidate-0x18012c770-procedure_assembly.json","sha256":"edbe0f2ba35b41bfb9c412aae4ec3e61a45ef7c29ec74b569801c4b6797a07f5"},
   {"key":"spatial_assembly","path":"docs/reverse-client/r1-engine-spatial-ground/05-EngineSpatialGround-0x180134500-procedure_assembly.json","sha256":"6dfe2938844693c4bffecd4c9340076fc0278c28c195bc6f4bfa669eddecf0fb"},
   {"key":"half_constant","path":"docs/reverse-client/r1-navigation-compressor-slot/inspection.json","sha256":"191e63e5cc34d9b1abf1326426827a27041743913768a3159e73af3dc62126de"},
   {"key":"sign_mask","path":"docs/reverse-client/r1-region-bounds-mask/inspection.json","sha256":"d3c63d6c8ed7c470da6a01b88146c75cecd8d91abfb654771be1e9961ea0fb75"}
  ],
  "fixed_slot": {"table_rva":"0x343b60","slot_offset":"0x88","rva":"0x343be8","bytes":8,"constructor":"0x1801dda90","callsite":"0x18012ca70"},
  "new_constant": {"rva":"0x34bad8","bytes":4,"instructions":["0x18013455c: MULSS XMM1, dword ptr [0x18034bad8]","0x180134564: MULSS XMM2, dword ptr [0x18034bad8]"]},
  "reused_constants": [
   {"key":"half_constant","collection":"constants","rva":"0x34badc","bytes":4,"raw_hex":"0000003f","uint32_hex":"0x3f000000","instruction":"0x18013459b: MOVSS XMM8, dword ptr [0x18034badc]"},
   {"key":"sign_mask","collection":"values","rva":"0x34c450","bytes":4,"raw_hex":"00000080","uint32_hex":"0x80000000","instruction":"0x18013456f: MOVSS XMM4, dword ptr [0x18034c450]"}
  ],
  "assembly_proofs": [
   {"key":"constructor","procedure":"0x1801dda90","instructions":["0x1801ddaad: MOV RDI, RCX","0x1801ddabe: LEA RAX, [0x180343b60]","0x1801ddac5: MOV qword ptr [RDI], RAX","0x1801ddac8: LEA RBX, [RDI + 0x68]","0x1801ddad9: LEA RAX, [0x18033e338]","0x1801ddae0: MOV qword ptr [RBX], RAX","0x1801ddbb7: MOV RAX, RDI"]},
   {"key":"caller","procedure":"0x18012c770","instructions":["0x18012ca57: CALL 0x1801dda90","0x18012ca5d: MOV qword ptr [RDI + 0x30], RAX","0x18012ca61: MOV R9, qword ptr [RAX]","0x18012ca64: MOVSX R8D, R14B","0x18012ca68: LEA RDX, [RSP + 0x30]","0x18012ca6d: MOV RCX, RAX","0x18012ca70: CALL qword ptr [R9 + 0x88]"]},
   {"key":"spatial_assembly","procedure":"0x180134500","instructions":["0x18013455c: MULSS XMM1, dword ptr [0x18034bad8]","0x180134564: MULSS XMM2, dword ptr [0x18034bad8]","0x18013456f: MOVSS XMM4, dword ptr [0x18034c450]","0x180134598: XORPS XMM2, XMM4","0x18013459b: MOVSS XMM8, dword ptr [0x18034badc]"]}
  ],
  "limits": {"new_data_bytes":12,"data_operations":2,"single_read_bytes":8,"hash_chunk_bytes":65536,"new_PE_header_bytes":0,"whole_table_bytes":0,"target_body_bytes":0},
  "requirements": [
   "Import performs no I/O. Execute only after exact approval of frozen script/plan and separate Windows Attributes/reparse-tag preflight.",
   "Only Engine RVA343BE8 8B and RVA34BAD8 4B are newly observed. No fresh PE headers, code, RTTI, neighboring slots, imports or exports.",
   "343B60 is the constructor's main vptr; 33E338 at this+68 is separate. Table extent and later overwrites remain unverified.",
   "Reuse pinned raw4B at34BADC/34C450 from the identical Engine hash. MOVSS reads34C450 as4B; subsequent XORPS has register operands only.",
   "Cached PE map requires same-descriptor whole-file hash identity. Before/after hash/stat/path checks do not constitute an atomic snapshot.",
   "Preserve raw bits. Executable address classification establishes neither function name nor body/ABI, and disk constants are not loaded-state observations.",
   "Output directory must be absent; retain failure evidence. No DLL load/execution, backend, subprocess, network, saves, security settings, commit or push."
  ]
 };
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_ground_loader_slot.mjs');
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
 const planPath=local('tools/static_analysis/read_r1_ground_loader_slot.plan.json');
 assert(regular(planPath).size<=12288);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-ground-loader-slot');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const def=expectedPlan.source;
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:expectedPlan.source_pins,code_bytes:0,metadata_bytes:0,data_bytes:0,data_operations:0,new_PE_header_bytes:0,whole_table_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,files:[],values:[],constants:[],disk_observations_complete:false,unsupported:[],failures:[],
  limitations:[
   'One main-table+88 disk address candidate only; no table extent, later overwrite, loaded dispatch, ABI or ground decoder behavior is proved.',
   'New constant34BAD8 is a4B MULSS operand; reused34BADC/34C450 are4B observations at the identical source hash, not loaded-state values.',
   'Whole-file hash streams are distinct from the12B semantic read budget. Before/after checks are not atomic snapshots.',
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
  for(const reuse of expectedPlan.reused_constants){const prior=data.get(reuse.key);
   assert.equal(prior.status,'passed');assert.equal(prior.source,def.path);assert.equal(prior.expected_size,def.bytes);
   assert(prior.stable&&prior.source_closed);assert.equal(prior.failures.length,0);
   assert.equal(prior.sha256_before,def.sha256);assert.equal(prior.sha256_after,def.sha256);
   const hits=prior[reuse.collection].filter(v=>v.rva===reuse.rva);assert.equal(hits.length,1);const value=hits[0];
   assert.equal(value.bytes,4);assert.equal(value.raw_hex,reuse.raw_hex);assert.equal(value.loaded_value_observed,false);
   const raw=Buffer.from(value.raw_hex,'hex');assert.equal(raw.length,4);assert.equal(hex(raw.readUInt32LE()),reuse.uint32_hex);
   report.constants.push({...reuse,reused:true,source_pin:expectedPlan.source_pins.find(p=>p.key===reuse.key),loaded_value_observed:false});
  }
  const slot=expectedPlan.fixed_slot;assert.equal(Number(slot.table_rva)+Number(slot.slot_offset),Number(slot.rva));assert.equal(slot.bytes,8);
  report.proofs_verified=true;
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){
   const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
  }return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};}
  function read(rvaText,n,kind){assert(n===4||n===8);const {section,offset}=map(Number(rvaText),n);
   assert(section.characteristics&0x40000000);assert(!(section.characteristics&0x20000000));
   assert(report.data_bytes+n<=12);assert(report.data_operations+1<=2);report.data_bytes+=n;report.data_operations++;
   const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);
   const observation={kind,rva:rvaText,file_offset:offset,section:section.name,bytes:n,raw_hex:b.toString('hex')};f.reads.push(observation);return {b,observation};
  }
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);
   initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   const got=read(slot.rva,slot.bytes,'fixed-main-vtable-slot'),va=got.b.readBigUInt64LE();
   const value={...slot,...got.observation,target_va:hex(va),candidate_rva:null,classification:null,function_name:null,function_boundary_verified:false,loaded_value_observed:false,table_extent_verified:false,subsequent_overwrites_excluded:false};report.values.push(value);
   if(va<0x180000000n||va>=0x180000000n+BigInt(pe.size_of_image))unsupported('Ground slot target outside cached Engine image');
   const target=Number(va-0x180000000n);value.candidate_rva=hex(target);
   const codeSections=pe.sections.filter(s=>target>=s.rva&&target<s.rva+s.raw_size);
   if(codeSections.length!==1||!(codeSections[0].characteristics&0x20000000))unsupported('Ground slot target lacks unique executable disk-backed section');
   value.classification='ExecutableAddressCandidate';value.candidate_code_section=codeSections[0].name;
   const c=expectedPlan.new_constant;assert.equal(c.bytes,4);const raw=read(c.rva,c.bytes,'fixed-dword-MULSS-operand');
   report.constants.push({...c,...raw.observation,uint32_hex:'0x'+raw.b.readUInt32LE().toString(16).padStart(8,'0'),reused:false,loaded_value_observed:false});
   assert.equal(report.values.length,1);assert.equal(report.constants.length,3);assert.equal(report.data_bytes,12);assert.equal(report.data_operations,2);
   report.disk_observations_complete=true;
  }finally{if(fd!==undefined){try{
   f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;
  }finally{fs.closeSync(fd);f.closed=true;}}}
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.disk_observations_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete observation without explicit unsupported reason');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<65536);normal(out);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed ground slot read failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,data_bytes:report.data_bytes,metadata_bytes:report.metadata_bytes,values:report.values,constants:report.constants,unsupported:report.unsupported,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
