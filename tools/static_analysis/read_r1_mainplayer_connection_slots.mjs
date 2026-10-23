// Preparation only: import performs no I/O. Root reviews and executes this
// fixed read with frozen script/plan hashes and separate Attributes preflight.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectMainPlayerConnectionSlots(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-mainplayer-connection-slots.v1",
  "script": "tools/static_analysis/read_r1_mainplayer_connection_slots.mjs",
  "entrypoint": "inspectMainPlayerConnectionSlots(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-mainplayer-connection-slots/inspection.json",
  "source": {
    "path": "D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll",
    "bytes": 11385856,
    "sha256": "cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2"
  },
  "source_pins": [
    {
      "key": "game_pe",
      "path": "docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json",
      "sha256": "227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b"
    },
    {
      "key": "constructor",
      "path": "docs/reverse-client/r1-game-mainplayer-connection-run1/05-GameMainPlayerConnection-0x18030afe0-procedure_assembly.json",
      "sha256": "1d6152c5fb914318a1fa995da8ea4a1316dd0fb76ad4588fbc207aa58b7ef2a2"
    },
    {
      "key": "constructor_pseudo",
      "path": "docs/reverse-client/r1-game-mainplayer-connection-run1/04-GameMainPlayerConnection-0x18030afe0-procedure_pseudo_code.json",
      "sha256": "959162b0b69276a9adda5411196ff906a27766d855ab2e41db21332d4910e33e"
    },
    {
      "key": "constructor_summary",
      "path": "docs/reverse-client/r1-game-mainplayer-connection-run1/summary.json",
      "sha256": "226d502f084dc05258efc70416e054dcc761e65e2eb1777392502b76d9fe14e9"
    },
    {
      "key": "main_player",
      "path": "docs/reverse-client/r1-path-local-dispatch/10-PathLocal-0x1802ebb70-procedure_assembly.json",
      "sha256": "e0f078a4f8eca837e23e46bc0c560fb919a989d8c1e85ac3584b041d7ba2b1dd"
    },
    {
      "key": "old_publication",
      "path": "docs/task-kills/ghidra-selected/subscriber-constructor-routes.json",
      "sha256": "e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165"
    }
  ],
  "slots": [
    {
      "table_rva": "0x767c98",
      "offset": "0x8",
      "rva": "0x767ca0",
      "bytes": 8,
      "reason": "Cached GameEngine2C07A0 invokes the4118 object +8 at2C1A4B with address40E0 before4118 is published to40F8; old caller provenance remains weaker than new hash-bound ctor."
    },
    {
      "table_rva": "0x767c98",
      "offset": "0x18",
      "rva": "0x767cb0",
      "bytes": 8,
      "reason": "Current-hash-bound GetMainPlayer2EBB70 invokes current40F8 object +18 twice at2EBB83 and2EBB96; no target name inferred."
    }
  ],
  "rtti": {
    "locator_slot_rva": "0x767c90",
    "format": "MS x64 signature1 CompleteObjectLocator only; table[-1] pointer, one24B locator, one16B TypeDescriptor header and raw NUL-terminated ASCII type name. No hierarchy/base arrays or demangling.",
    "name_bytes_limit": 192,
    "class_hierarchy_follow": false,
    "header_pointers_follow": false
  },
  "assembly_proofs": [
    {
      "key": "constructor",
      "procedure": "0x18030afe0",
      "instructions": [
        "0x18030aff8: MOV RDI, RCX",
        "0x18030affb: LEA RAX, [0x180767c98]",
        "0x18030b002: MOV qword ptr [RCX], RAX",
        "0x18030b035: CALL 0x1803f60e0",
        "0x18030b03b: MOV qword ptr [RDI + 0x20], RAX",
        "0x18030b03f: MOV qword ptr [RAX + 0x8], RDI",
        "0x18030b043: MOV RAX, RDI"
      ]
    },
    {
      "key": "main_player",
      "procedure": "0x1802ebb70",
      "instructions": [
        "0x1802ebb79: MOV RCX, qword ptr [RCX + 0x40f8]",
        "0x1802ebb83: CALL qword ptr [RAX + 0x18]",
        "0x1802ebb86: CMP dword ptr [RAX + 0x10], 0x0",
        "0x1802ebb8c: MOV RCX, qword ptr [RBX + 0x40f8]",
        "0x1802ebb96: CALL qword ptr [RAX + 0x18]",
        "0x1802ebb99: MOV EBX, dword ptr [RAX + 0x10]"
      ]
    }
  ],
  "cached_publication_instructions": [
    "1802c19a2: CALL 0x18030afe0",
    "1802c19a8: MOV qword ptr [RDI + 0x4118],RAX",
    "1802c1a3a: MOV RCX,qword ptr [RDI + 0x4118]",
    "1802c1a4b: CALL qword ptr [RAX + 0x8]",
    "1802c1a5c: MOV RAX,qword ptr [RDI + 0x4118]",
    "1802c1a63: MOV qword ptr [RDI + 0x40f8],RAX"
  ],
  "limits": {
    "metadata_bytes": 256,
    "metadata_operations": 197,
    "single_read_bytes": 24,
    "hash_chunk_bytes": 65536,
    "code_bytes": 0,
    "new_PE_header_bytes": 0,
    "whole_table_bytes": 0
  },
  "saved_metadata_dedup": {
    "method": "Read-only exact767C90/767C98/767CA0/767CB0 text search in saved docs json/md/txt files at most1MiB found only the new30AFE0 pseudo/assembly references to767C98. No saved actual slot or RTTI bytes found in that inspected domain; larger files and analysis databases were not inspected.",
    "matching_observation_files": [],
    "original_read": false,
    "backend_started": false
  },
  "abi_format_reference": {
    "url": "https://raw.githubusercontent.com/llvm/llvm-project/main/clang/lib/CodeGen/MicrosoftCXXABI.cpp",
    "functions": [
      "getTypeDescriptorType",
      "getCompleteObjectLocatorType",
      "MSRTTIBuilder::getCompleteObjectLocator"
    ],
    "role": "Primary compiler implementation consulted to check optional MS x64 RTTI layout; not evidence of this Game image. Execution makes no network request. Signature1/selfRVA/ranges/rawname must validate or RTTI remains Unsupported."
  },
  "requirements": [
    "Prepare only; separate root review and exact approval with frozen script/plan hashes, plus Windows Attributes/reparse preflight for source and workspace ancestors. Import performs no I/O.",
    "Exactly two function slots at767CA0 and767CB0 and locator pointer767C90. Follow only that single RTTI pointer to at most24B locator,16B type header,192B NUL-terminated raw name. No other vtable slots, hierarchy/base arrays, target code, pdata, imports, exports, broad strings or new PE headers.",
    "Reuse cached PE map only after current same-handle Game hash matches pinned identity. Whole-file hashes use64KiB buffers and are separate from256B semantic read budget; checks are not atomic snapshots.",
    "The Game30AFE0 ctor and2EBB70 reader are current-hash-bound Rea evidence. Old2C07A0 publication cache lacks embedded input hash: its current file hash preserves content only, not a retroactive original binding.",
    "Constructor establishes a main-vptr candidate. Callee effects, later overwrites, table extents, loaded dispatch, complete construction, ABI and Record+10 production are not validated by these disk bytes.",
    "Executable section membership labels address candidates only. RTTI is optional, restricted to signature1 with selfRVA match and raw ASCII name; no assumed class identity or demangled behavior. Preserve slot results even if RTTI is Unsupported.",
    "Output directory must be absent; retain raw partial/failure evidence. No original loading/execution, backend, subprocess, network, save data, security changes, commit or push."
  ]
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_mainplayer_connection_slots.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path: '+q);assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
  if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){assert(!path.isAbsolute(rel));const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 function checkedJson(rel,expected,max){const p=local(rel);assert(regular(p).size<=max);const b=fs.readFileSync(p);assert.equal(hash(b),expected);return JSON.parse(b);}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=32768);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const plan=checkedJson('tools/static_analysis/read_r1_mainplayer_connection_slots.plan.json',reviewedPlanSha256,16384);assert.deepEqual(plan,expectedPlan);
 const out=path.dirname(local(plan.output));normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing evidence preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:plan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:plan.source_pins,limits:plan.limits,metadata_bytes:0,metadata_operations:0,code_bytes:0,new_PE_header_bytes:0,whole_table_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,backend_started:false,files:[],values:[],rtti:null,slot_bindings_complete:false,
  rtti_name_complete:false,unsupported:[],failures:[],limitations:plan.requirements};
 function unsupported(reason){const e=new Error(reason);e.fixedUnsupported=true;throw e;}
 const proofs=new Map(),def=plan.source;
 try{
  for(const pin of plan.source_pins)proofs.set(pin.key,checkedJson(pin.path,pin.sha256,1048576));
  const gp=proofs.get('game_pe');assert.equal(gp.target,def.path);assert.equal(gp.expected_bytes,def.bytes);
  assert(gp.source_hash_preserved&&gp.source_stat_stable&&gp.source_handle_closed);assert.equal(gp.sha256_before,def.sha256);assert.equal(gp.sha256_after,def.sha256);
  const pe=gp.pe;assert.equal(pe.image_base,'0x180000000');assert(Number.isSafeInteger(pe.image_size)&&pe.image_size>0);
  assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections)for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);
  for(const entry of plan.assembly_proofs){const sc=proofs.get(entry.key).result.structuredContent;
   assert.equal(sc.operation,'procedure_assembly');assert.equal(sc.parameters.procedure,entry.procedure);
   assert.equal(sc.provider.id,'ghidra');assert.equal(sc.subject.digest.sha256,def.sha256);assert.equal(path.normalize(sc.subject.local_path),path.normalize(def.path));
   assert.equal(typeof sc.normalized_result,'string');const lines=sc.normalized_result.split(/\r?\n/);
   for(const instruction of entry.instructions)assert(lines.includes(instruction),instruction);
  }
  const pseudo=proofs.get('constructor_pseudo').result.structuredContent;
  assert.equal(pseudo.operation,'procedure_pseudo_code');assert.equal(pseudo.parameters.procedure,'0x18030afe0');assert.equal(pseudo.subject.digest.sha256,def.sha256);
  assert(pseudo.normalized_result.includes('*param_1 = &PTR_FUN_180767c98;'));
  const summary=proofs.get('constructor_summary');assert.equal(summary.status,'passed');assert.equal(summary.completed_plan_calls,8);
  assert(summary.closed_session_verified&&summary.process_exit_confirmed&&summary.owned_memory_profile.job_empty_confirmed&&summary.owned_memory_profile.sampler_exit_confirmed);
  const old=proofs.get('old_publication');assert.equal(old.program,'Game.dll');const constructor=old.functions[4];assert.equal(constructor.observed_entry,'1802c07a0');
  assert.equal(constructor.decompile_completed,true);for(const line of plan.cached_publication_instructions)assert(constructor.assembly.includes(line));
  report.old_publication_input_hash_bound=false;report.constructor_body_current_hash_bound=true;report.proofs_verified=true;
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(plan.limits.hash_chunk_bytes);for(let at=0;at<def.bytes;){
   const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
  }return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1,'Unique disk-backed section required');
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};
  }
  function read(rva,n,kind){assert(n<=plan.limits.single_read_bytes);assert(report.metadata_bytes+n<=plan.limits.metadata_bytes);assert(report.metadata_operations<plan.limits.metadata_operations);
   const {section,offset}=map(rva,n);assert(section.characteristics&0x40000000);assert(!(section.characteristics&0x20000000),'Code read refused');
   const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);report.metadata_bytes+=n;report.metadata_operations++;
   f.reads.push({kind,rva:hex(rva),file_offset:offset,section:section.name,bytes:n,raw_hex:b.toString('hex')});return b;
  }
  function imageRva(va,kind){if(va<0x180000000n||va>=0x180000000n+BigInt(pe.image_size))unsupported(kind+' is outside the fixed Game image');return Number(va-0x180000000n);}
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(before,initial);
   f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   for(const slot of plan.slots){assert.equal(Number(slot.table_rva)+Number(slot.offset),Number(slot.rva));assert.equal(slot.bytes,8);
    const b=read(Number(slot.rva),8,'exact-main-vtable-slot'),va=b.readBigUInt64LE();const value={...slot,raw_hex:b.toString('hex'),target_va:hex(va),target_rva:null,classification:null,function_boundary_verified:false,target_body_read:false,loaded_binding_observed:false};report.values.push(value);
    const rva=imageRva(va,'Slot '+slot.offset);value.target_rva=hex(rva);const target=map(rva,1);if(!(target.section.characteristics&0x20000000))unsupported('Slot '+slot.offset+' target is not executable-section data');
    value.classification='ExecutableAddressCandidate';value.target_section=target.section.name;
   }
   report.slot_bindings_complete=true;
   // One MS x64 RTTI locator chain only; not a class hierarchy traversal.
   // Signature/self-RVA/name format mismatches stop RTTI interpretation.
   const rtti=report.rtti={locator_slot_rva:plan.rtti.locator_slot_rva,locator_va:null,locator_rva:null,locator:null,type_descriptor:null,raw_type_name:null,demangled_type_name:null,class_hierarchy_read:false,loaded_type_observed:false};
   const locatorBytes=read(Number(plan.rtti.locator_slot_rva),8,'exact-vtable-minus8-locator-pointer');const locatorVa=locatorBytes.readBigUInt64LE();rtti.locator_va=hex(locatorVa);
   if(locatorVa===0n)unsupported('Null vtable-minus8 locator: RTTI is not established');
   const colRva=imageRva(locatorVa,'RTTI locator');rtti.locator_rva=hex(colRva);const col=read(colRva,24,'single-RTTICompleteObjectLocator');
   const fields={signature:col.readUInt32LE(0),offset_to_top:col.readUInt32LE(4),constructor_displacement_offset:col.readUInt32LE(8),type_descriptor_rva:hex(col.readUInt32LE(12)),class_hierarchy_rva:hex(col.readUInt32LE(16)),self_rva:hex(col.readUInt32LE(20)),raw_hex:col.toString('hex')};rtti.locator=fields;
   if(fields.signature!==1||col.readUInt32LE(20)!==colRva)unsupported('RTTI locator is not supported signature1 with matching self RVA');
   const tdRva=col.readUInt32LE(12);if(tdRva===0)unsupported('RTTI type descriptor RVA is zero');
   const td=read(tdRva,16,'single-TypeDescriptor-header');rtti.type_descriptor={rva:hex(tdRva),vptr_va:hex(td.readBigUInt64LE(0)),runtime_data_va:hex(td.readBigUInt64LE(8)),raw_hex:td.toString('hex'),header_pointers_followed:false,name_rva:hex(tdRva+16)};
   const name=[];let terminated=false;
   for(let n=0;n<plan.rtti.name_bytes_limit;n++){
    const b=read(tdRva+16+n,1,'single-TypeDescriptor-name-byte')[0];
    if(b===0){terminated=true;break;}if(b<32||b>126)unsupported('RTTI type name contains non-ASCII bytes');name.push(b);
   }
   if(!terminated)unsupported('RTTI type name exceeds fixed NUL-terminated192-byte bound');
   rtti.raw_type_name=Buffer.from(name).toString('ascii');if(!rtti.raw_type_name.startsWith('.?A'))unsupported('Unrecognized MS class/struct RTTI name prefix');
   report.rtti_name_complete=true;
  }finally{if(fd!==undefined){try{
   f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;
  }finally{fs.closeSync(fd);f.closed=true;}}}
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.slot_bindings_complete&&report.rtti_name_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete observations without explicit Unsupported reason');
 normal(out);const text=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(text)<=98304);fs.writeFileSync(local(plan.output),text,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed main-player connection slots read failed; evidence preserved: '+report.failures.join('; '));
 return {status:report.status,output:plan.output,metadata_bytes:report.metadata_bytes,slot_bindings_complete:report.slot_bindings_complete,values:report.values,rtti:report.rtti,unsupported:report.unsupported,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
