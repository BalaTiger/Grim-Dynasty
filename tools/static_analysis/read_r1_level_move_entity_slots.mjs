// Fixed disk observation only. Importing this module does not perform I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
export function inspectLevelMoveEntitySlots(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-level-move-entity-slots.v1",
  "script": "tools/static_analysis/read_r1_level_move_entity_slots.mjs",
  "entrypoint": "inspectLevelMoveEntitySlots(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-level-move-entity-slots/inspection.json",
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
      "key": "primary_tables",
      "path": "docs/reverse-client/r1-game-callback-dependencies/inspection.json",
      "sha256": "c0f0348d8f0712b35efd22d7ed36282065bfc31c78ee18b0f1d870e036a4afab"
    },
    {
      "key": "level_assembly",
      "path": "docs/reverse-client/r1-same-region-movement/08-SameRegionMovement-0x18012f4f0-procedure_assembly.json",
      "sha256": "cf6f5a0ed0faface5f8ddf543c58f3681e96a437e6a919670c54a9d9ce9ed01b"
    }
  ],
  "fixed_slots": [
    {
      "owner": "Character",
      "table_rva": "0x6df6d0",
      "slot_offset": "0x270",
      "rva": "0x6df940",
      "bytes": 8,
      "callsite": "0x18012f670",
      "opcode": "CALL"
    },
    {
      "owner": "Character",
      "table_rva": "0x6df6d0",
      "slot_offset": "0xd8",
      "rva": "0x6df7a8",
      "bytes": 8,
      "callsite": "0x18012f67c",
      "opcode": "CALL"
    },
    {
      "owner": "Character",
      "table_rva": "0x6df6d0",
      "slot_offset": "0x90",
      "rva": "0x6df760",
      "bytes": 8,
      "callsite": "0x18012f6b3",
      "opcode": "JMP"
    },
    {
      "owner": "Player",
      "table_rva": "0x787100",
      "slot_offset": "0x270",
      "rva": "0x787370",
      "bytes": 8,
      "callsite": "0x18012f670",
      "opcode": "CALL"
    },
    {
      "owner": "Player",
      "table_rva": "0x787100",
      "slot_offset": "0xd8",
      "rva": "0x7871d8",
      "bytes": 8,
      "callsite": "0x18012f67c",
      "opcode": "CALL"
    },
    {
      "owner": "Player",
      "table_rva": "0x787100",
      "slot_offset": "0x90",
      "rva": "0x787190",
      "bytes": 8,
      "callsite": "0x18012f6b3",
      "opcode": "JMP"
    }
  ],
  "limits": {
    "metadata_bytes": 48,
    "metadata_operations": 6,
    "metadata_single_read": 8,
    "hash_chunk_bytes": 65536,
    "new_PE_header_bytes": 0,
    "whole_table_bytes": 0,
    "target_body_bytes": 0
  },
  "requirements": [
    "Importing the module performs no filesystem I/O. Execute only after separate exact approval with frozen script and plan hashes.",
    "Separate Windows Attributes/reparse-tag preflight of original file and output ancestors is required; no security settings changed.",
    "Only six fixed primary-table slots (+270,+D8,+90 for Character and Player) are read; +190 is excluded. No vtable scan, target body, import/export scan, runtime value, or new function analysis.",
    "Pinned cached PE map is used only after same-descriptor whole-file hash matches the known original.",
    "Preserve raw bytes and target candidate; no function name is inferred from slot purpose. No saved exact-name lookup is present in this plan.",
    "Primary-table extent and subsequent constructor-callee overwrite remain unverified; disk candidate is not loaded dispatch proof.",
    "Same-descriptor before/after SHA256 and stat/path identity checks; output must be absent and partial failure evidence is preserved.",
    "No original execution/loading, backend, subprocess, network, save data, system settings, commit or push."
  ],
  "expected_instructions": [
    "0x18012f517: MOV RDI, RDX",
    "0x18012f5fb: MOV RCX, RDI",
    "0x18012f654: MOV RAX, qword ptr [RDI]",
    "0x18012f670: CALL qword ptr [RAX + 0x270]",
    "0x18012f676: MOV RAX, qword ptr [RDI]",
    "0x18012f679: MOV RCX, RDI",
    "0x18012f67c: CALL qword ptr [RAX + 0xd8]",
    "0x18012f68e: MOV RAX, qword ptr [RDI]",
    "0x18012f691: MOV RDX, R14",
    "0x18012f694: MOV RCX, RDI",
    "0x18012f6b3: JMP qword ptr [RAX + 0x90]"
  ]
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');
 assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_level_move_entity_slots.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  const s=fs.lstatSync(q);assert(!s.isSymbolicLink(),'Redirected path: '+q);
  assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
  if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=24576);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_level_move_entity_slots.plan.json');
 assert(regular(planPath).size<=8192);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-level-move-entity-slots');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const def=expectedPlan.source;
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:expectedPlan.source_pins,code_bytes:0,metadata_bytes:0,metadata_operations:0,new_PE_header_bytes:0,whole_table_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,whole_table_scanned:false,files:[],values:[],disk_bindings_complete:false,unsupported:[],failures:[],
  limitations:[
   'Only six disk primary-vtable candidates at+270,+D8,+90; no+190 read. Original executable code is never loaded or executed.',
   'Table extents and subsequent constructor-callee overwrites are not excluded by the saved proof; actual loaded dispatch remains unverified.',
   'Executable address classification does not establish function boundaries, target names, ABI or behavior.',
   'No function name is inferred from the role of the calling slot; exact saved name lookup is absent.',
   'Whole-file hash streams check identity separately from the48B semantic read budget, and are not atomic snapshots.',
   'Node path checks cannot identify every Windows reparse tag; separate Attributes preflight remains required.'
  ]};
 const data=new Map();
 function unsupported(reason){const e=new Error(reason);e.fixedUnsupported=true;throw e;}
 try{
  for(const pin of expectedPlan.source_pins){const p=local(pin.path);assert(regular(p).size<1048576);const bytes=fs.readFileSync(p);assert.equal(hash(bytes),pin.sha256);data.set(pin.key,JSON.parse(bytes));}
  const gp=data.get('game_pe');assert.equal(gp.target,def.path);assert(gp.source_hash_preserved&&gp.source_stat_stable&&gp.source_handle_closed);
  assert.equal(gp.sha256_before,def.sha256);assert.equal(gp.sha256_after,def.sha256);
  const pe=gp.pe;assert.equal(pe.image_base,'0x180000000');
  assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections){for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);}
  const tables=data.get('primary_tables');assert.equal(tables.status,'passed');assert(tables.proofs_verified);
  assert.equal(tables.primary_tables.interface_offset,0);assert(tables.primary_tables.constructor_writes_verified&&tables.primary_tables.player_overwrite_verified);
  assert.equal(tables.primary_tables.table_extents_verified,false);assert.equal(tables.primary_tables.subsequent_callee_writes_excluded,false);
  const originals=tables.files.filter(f=>f.source===def.path);assert.equal(originals.length,1);
  assert(originals[0].stable&&originals[0].closed);assert.equal(originals[0].sha256_before,def.sha256);assert.equal(originals[0].sha256_after,def.sha256);
  for(const entry of expectedPlan.fixed_slots){
   const proof=tables.values.filter(v=>v.owner===entry.owner&&v.table_rva===entry.table_rva&&v.slot_offset==='0x0');assert.equal(proof.length,1);
   assert.equal(proof[0].rva,entry.table_rva);assert.equal(proof[0].bytes,8);
   assert.equal(Number(entry.table_rva)+Number(entry.slot_offset),Number(entry.rva));assert.equal(entry.bytes,8);
  }
  const assembly=data.get('level_assembly').result.structuredContent.normalized_result;
  assert.equal(typeof assembly,'string');assert(assembly.startsWith('0x18012f4f0: '));
  const lines=assembly.split('\n');
  for(const line of expectedPlan.expected_instructions)assert(lines.includes(line));
  report.proofs_verified=true;report.primary_tables=tables.primary_tables;
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);
  let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){
   const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
  }return h.digest('hex');}
  function map(rva,n){
   assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};
  }
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);
   initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   for(const entry of expectedPlan.fixed_slots){
    const rva=Number(entry.rva),n=entry.bytes,{section,offset}=map(rva,n);
    assert(section.characteristics&0x40000000,'Unreadable PE section');assert(!(section.characteristics&0x20000000),'Slot is executable data');
    assert.equal(n,8);assert(report.metadata_bytes+n<=48);assert(++report.metadata_operations<=6);report.metadata_bytes+=n;
    const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);
    f.reads.push({kind:'fixed-primary-vtable-slot',rva:entry.rva,file_offset:offset,bytes:n,raw_hex:b.toString('hex')});
    const va=b.readBigUInt64LE(),value={...entry,section:section.name,file_offset:offset,raw_hex:b.toString('hex'),uint64_va:hex(va),
     candidate_rva:null,classification:null,function_name:null,function_name_source:null,function_boundary_verified:false,callback_abi_verified:false,loaded_value_observed:false};
    report.values.push(value);
    if(va<0x180000000n||va>0x27fffffffn)unsupported(entry.owner+' slot target is outside representable Game image-relative range');
    const target=Number(va-0x180000000n);value.candidate_rva=hex(target);
    const matches=pe.sections.filter(s=>target>=s.rva&&target<s.rva+s.raw_size);
    if(matches.length!==1||!(matches[0].characteristics&0x20000000))unsupported(entry.owner+' slot target lacks unique executable disk-backed section');
    value.classification='ExecutableAddressCandidate';value.candidate_code_section=matches[0].name;
   }
   assert.equal(report.values.length,6);assert.equal(report.metadata_bytes,48);assert.equal(report.metadata_operations,6);
   report.disk_bindings_complete=true;
  }finally{
   if(fd!==undefined){try{
    f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;
   }finally{fs.closeSync(fd);f.closed=true;}}
  }
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();
 report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.disk_bindings_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete binding without explicit unsupported reason');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<65536);normal(out);
 fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed Level MoveEntity slot read failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,code_bytes:0,metadata_bytes:report.metadata_bytes,values:report.values,unsupported:report.unsupported,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
