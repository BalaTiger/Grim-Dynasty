// Fixed independent single-slot reader. Import performs no I/O; root executes after review.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
export function inspectPlayerRunSpeedSlot(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-player-run-speed-slot.v1",
  "script": "tools/static_analysis/read_r1_player_run_speed_slot.mjs",
  "entrypoint": "inspectPlayerRunSpeedSlot(reviewedScriptSha256,reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-player-run-speed-slot/inspection.json",
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
      "key": "player_ctor",
      "path": "docs/reverse-client/r1-game-entity-constructors/08-GameEntityConstructor-0x1803cc3f0-procedure_assembly.json",
      "sha256": "d68276f2f2212942eb66cf2180f34f0b527f5b9a48f473b4ac35098b3d479ded"
    },
    {
      "key": "run_speed",
      "path": "docs/reverse-client/r1-game-run-speed-run1/05-GameRunSpeed-0x180069040-procedure_assembly.json",
      "sha256": "7ba2191d3009c003442e7f4f438ff3298a80371c38c1fb81e3e780ea2718a571"
    },
    {
      "key": "run_speed_summary",
      "path": "docs/reverse-client/r1-game-run-speed-run1/summary.json",
      "sha256": "9b69042b1f3b6392b5f64f5847b15750196c4dd06d5421640c71234a44b6183b"
    }
  ],
  "assembly_proofs": [
    {
      "key": "player_ctor",
      "procedure": "0x1803cc3f0",
      "instructions": [
        "0x1803cc415: MOV RDI, RCX",
        "0x1803cc418: CALL 0x180052030",
        "0x1803cc41e: LEA RAX, [0x180787100]",
        "0x1803cc425: MOV qword ptr [RDI], RAX"
      ]
    },
    {
      "key": "run_speed",
      "procedure": "0x180069040",
      "instructions": [
        "0x180069076: MOV RDI, RCX",
        "0x180069292: CMP byte ptr [RDI + 0x26b3], 0x0",
        "0x180069299: JZ 0x1800692a2",
        "0x1800692a2: MOV RAX, qword ptr [RDI]",
        "0x1800692a5: CALL qword ptr [RAX + 0x678]"
      ]
    }
  ],
  "slot": {
    "table_rva": "0x787100",
    "offset": "0x678",
    "rva": "0x787778",
    "bytes": 8
  },
  "limits": {
    "semantic_data_bytes": 8,
    "semantic_read_operations": 1,
    "code_bytes": 0,
    "new_pe_header_bytes": 0,
    "pdata_bytes": 0,
    "rtti_bytes": 0,
    "hash_chunk_bytes": 65536
  },
  "saved_metadata_dedup": {
    "files_read": 135,
    "directories": [
      "docs/m3-evidence",
      "docs/reverse-client/r1-game-entity-constructors",
      "docs/reverse-client/r1-player-movement-interface-slots",
      "docs/reverse-client/r1-mouse-getter-bindings",
      "docs/reverse-client/r1-mouse-getter-bindings-retry1",
      "docs/reverse-client/r1-game-stop-state-slots"
    ],
    "matching_slot_observation_files": [],
    "scope": "Read-only local saved json/md/txt/cs <=1MiB; no787778 observation in inspected domain. No original read/backend."
  },
  "requirements": [
    "Preparation only; root separately reviews and approves exact invocation with both hashes and performs Windows Attributes/reparse ancestor preflight. Import has no I/O.",
    "Exactly8 bytes atGame RVA787778. Verify readable, nonwritable, nonexecutable source section; classify target only by cached same-hash PE map, without reading it. No other slot, RTTI/table[-1], strings, code, imports,exports or headers.",
    "Same source handle before/after full SHA256 with64KiB buffer and stat/path identity checks; these are separate from8B semantic budget, not atomic snapshots. Source read-only and closed in finally; output directory absent, no overwrite.",
    "A current Player constructor main-vptr and the actual069040 callsite justify the disk slot. Target executable membership is only a function candidate; loaded vptr, complete constructor,26B3 value, class/runtime identity and successful call remain unverified.",
    "The prior connection optionalRTTI refusal is not retried or altered. NoRTTI is attempted or claimed by this independent exact-slot read.",
    "No subprocess/backend/network/original loading/execution,saves/cloud,security settings,commit or push."
  ]
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\users\zhuzi\documents\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_player_run_speed_slot.mjs');
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path');assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function same(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){assert(!path.isAbsolute(rel));const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 function checked(rel,sha,max=1048576){const p=local(rel);assert(regular(p).size<=max);const bytes=fs.readFileSync(p);assert.equal(hash(bytes),sha);return JSON.parse(bytes);}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=24576);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const plan=checked('tools/static_analysis/read_r1_player_run_speed_slot.plan.json',reviewedPlanSha256,16384);assert.deepEqual(plan,expectedPlan);
 const out=path.dirname(local(plan.output));normal(path.dirname(out));try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}fs.mkdirSync(out);normal(out);
 const report={schema:plan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,source_pins:plan.source_pins,limits:plan.limits,semantic_data_bytes:0,semantic_read_operations:0,code_bytes:0,new_pe_header_bytes:0,pdata_bytes:0,rtti_bytes:0,rtti_established:false,original_loaded_or_executed:false,subprocesses:0,backend_started:false,source:plan.source,hash_bytes:0,reads:[],values:[],source_closed:false,slot_binding_complete:false,failures:[],limitations:plan.requirements};
 let fd,initial;
 try{
  const proofs=new Map();for(const p of plan.source_pins)proofs.set(p.key,checked(p.path,p.sha256));
  const def=plan.source,gp=proofs.get('game_pe');assert.equal(gp.target,def.path);assert.equal(gp.expected_bytes,def.bytes);assert(gp.source_hash_preserved&&gp.source_stat_stable&&gp.source_handle_closed);assert.equal(gp.sha256_before,def.sha256);assert.equal(gp.sha256_after,def.sha256);
  const pe=gp.pe;assert.equal(pe.image_base,'0x180000000');assert(Number.isSafeInteger(pe.image_size)&&pe.image_size>0);assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections)for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);
  for(const p of plan.assembly_proofs){const sc=proofs.get(p.key).result.structuredContent;assert.equal(sc.operation,'procedure_assembly');assert.equal(sc.parameters.procedure,p.procedure);assert.equal(sc.provider.id,'ghidra');assert.equal(sc.subject.digest.sha256,def.sha256);assert.equal(path.normalize(sc.subject.local_path),path.normalize(def.path));const lines=sc.normalized_result.split(/
?
/);for(const line of p.instructions)assert(lines.includes(line),line);}
  const sum=proofs.get('run_speed_summary');assert.equal(sum.status,'passed');assert.equal(sum.completed_plan_calls,8);assert.equal(sum.target.sha256_before,def.sha256);assert.equal(sum.target.sha256_after,def.sha256);assert(sum.closed_session_verified&&sum.process_exit_confirmed&&sum.owned_memory_profile.job_empty_confirmed&&sum.owned_memory_profile.sampler_exit_confirmed);
  report.proofs_verified=true;
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1,'Unique raw-backed PE section required');const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(offset>=0&&offset+n<=def.bytes);return {section,offset};}
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(plan.limits.hash_chunk_bytes);for(let at=0;at<def.bytes;){const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;report.hash_bytes+=n;}return h.digest('hex');}
  const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());same(before,initial);
  try{
   report.sha256_before=digest();assert.equal(report.sha256_before,def.sha256);
   const slot=plan.slot;assert.equal(Number(slot.table_rva)+Number(slot.offset),Number(slot.rva));assert.equal(slot.bytes,8);assert.equal(plan.limits.semantic_data_bytes,8);assert.equal(plan.limits.semantic_read_operations,1);
   const source=map(Number(slot.rva),8);assert(source.section.characteristics&0x40000000,'Readable source required');assert(!(source.section.characteristics&0xa0000000),'Writable/executable slot-source read refused');
   const bytes=Buffer.alloc(8);assert.equal(fs.readSync(fd,bytes,0,8,source.offset),8);report.semantic_data_bytes=8;report.semantic_read_operations=1;report.reads.push({rva:slot.rva,bytes:8,section:source.section.name,file_offset:source.offset,raw_hex:bytes.toString('hex')});
   const va=bytes.readBigUInt64LE();const value={...slot,raw_hex:bytes.toString('hex'),target_va:hex(va),target_body_read:false,function_boundary_verified:false,loaded_binding_observed:false};report.values.push(value);assert(va>=0x180000000n&&va<0x180000000n+BigInt(pe.image_size),'Target outside Game image');const rva=Number(va-0x180000000n);value.target_rva=hex(rva);const target=map(rva,1);assert(target.section.characteristics&0x20000000,'Target must classify as executable address');value.target_section=target.section.name;value.classification='ExecutableAddressCandidate';report.slot_binding_complete=true;
  }finally{
   try{report.sha256_after=digest();assert.equal(report.sha256_after,def.sha256);same(fs.fstatSync(fd),initial);same(regular(def.path),initial);report.source_hash_preserved=true;report.source_stat_stable=true;}finally{fs.closeSync(fd);fd=undefined;report.source_closed=true;}
  }
 }catch(e){report.failures.push(String(e));}
 finally{if(fd!==undefined){try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('close: '+String(e));}}}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':report.slot_binding_complete&&report.source_closed&&report.source_hash_preserved?'passed':'failed';if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete fixed read');normal(out);const text=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(text)<=65536);fs.writeFileSync(local(plan.output),text,{flag:'wx'});
 if(report.status!=='passed')throw new Error('Fixed player run-speed slot failed; raw evidence retained: '+report.failures.join('; '));
 return {status:report.status,output:plan.output,values:report.values,semantic_data_bytes:report.semantic_data_bytes,code_bytes:0,rtti_established:false,sha256_before:report.sha256_before,sha256_after:report.sha256_after,source_closed:report.source_closed};
}
