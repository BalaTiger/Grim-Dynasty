// Fixed disk observation only. Importing this module does not perform I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
export function inspectEntityPublicationThunks(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-entity-publication-thunks.v1",
  "script": "tools/static_analysis/read_r1_entity_publication_thunks.mjs",
  "entrypoint": "inspectEntityPublicationThunks(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-entity-publication-thunks/inspection.json",
  "sources": {
    "Game": {
      "path": "D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll",
      "bytes": 11385856,
      "sha256": "cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2"
    },
    "Engine": {
      "path": "D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll",
      "bytes": 4485632,
      "sha256": "4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153"
    }
  },
  "conditional_binding": {
    "iat_owner": "Only the cached first488 Engine.dll import entries; selected solely by the two exact FF25disp32 thunks and fixed IAT609A40",
    "iat_base_rva": "0x609000",
    "lookup_base_rva": "0xa42ac0",
    "lookup_entries": 488,
    "lookup_source": "cached350-entry prefix plus cached138-entry continuation",
    "original_reads": "Exactly two6B Game thunks; at most three unique Game IAT8B/hint2B/exact-name strings and their hinted Engine export pointer4B/exact-name/ordinal2B/EAT4B.",
    "fallback": "Unsupported on opcode/ownership/hint/name mismatch; no table scan, search, forwarder traversal, thunk chain, other instruction or exported body read."
  },
  "source_pins": [
    {
      "key": "player_assembly",
      "path": "docs/reverse-client/r1-game-entity-publication-gates/08-EntityPublicationGate-0x1803d5bc0-procedure_assembly.json",
      "sha256": "f903c05cdd867c4618bfc6429bba90392c1738579b5b849cc5a55c279eac8033"
    },
    {
      "key": "slots",
      "path": "docs/reverse-client/r1-level-move-entity-slots/inspection.json",
      "sha256": "1eac17c7c6d015175f0b149dfa7587e507dab84b87eedfbfcaa90b81d2201690"
    },
    {
      "key": "game_pe",
      "path": "docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json",
      "sha256": "227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b"
    },
    {
      "key": "engine_pe",
      "path": "docs/object-lifecycle/offline-class-registration-exports/discovery.json",
      "sha256": "0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58"
    },
    {
      "key": "imports",
      "path": "docs/object-lifecycle/offline-questitem-imports/import-bindings.json",
      "sha256": "94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e"
    },
    {
      "key": "oft_tail",
      "path": "docs/reverse-client/r1-navigation-binding/inspection.json",
      "sha256": "d0c171ce77a6d67e153a6911fc7c3a17ea4d61e80ab9492f3287384524e14264"
    },
    {
      "key": "inventory",
      "path": "docs/phase-1/boundary-audit/module-boundaries.json",
      "sha256": "d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7"
    }
  ],
  "limits": {
    "candidate_bytes": 12,
    "code_operations": 2,
    "metadata_bytes": 966,
    "metadata_operations": 21,
    "metadata_single_read": 151,
    "maximum_cached_symbol_chars": 150,
    "maximum_unique_iats": 3,
    "hash_chunk_bytes": 65536,
    "new_PE_header_bytes": 0,
    "new_OFT_bytes": 0,
    "target_body_bytes": 0
  },
  "requirements": [
    "Module import performs no filesystem I/O; exact execution requires separate approval with frozen script and plan hashes.",
    "External Windows Attributes/reparse-tag precheck of both originals and output ancestors is required.",
    "Reuse pinned cached PE maps only after each current same-descriptor whole-file hash equals its original fingerprint.",
    "Only Game5D9FF2/5D9F02 six-byte candidates may be read as code; both must be FF25disp32. Opcode mismatch stops explicitly without fallback.",
    "Derived thunk IATs must be aligned entries in the cached first488 Engine import prefix. Bind exact cached names and current import hint; fixed609A40 must equal saved IsNetworkServer symbol.",
    "At most three unique IATs, three exact hinted Engine name/ordinal/EAT resolutions; no whole-table scan or binary search.",
    "Same-descriptor before/after SHA256/stat; new output directory refuses overwrite and preserves partial failure/Unsupported.",
    "No native loading/execution, backend, subprocess, network, save data, runtime globals, security changes or loaded dispatch/behavior claim."
  ],
  "fixed_slots": [
    {
      "iat_rva": "0x609a40",
      "index": 328,
      "symbol": "?IsNetworkServer@Engine@GAME@@QEAA_NXZ",
      "calls": [
        "0x1803d5bce"
      ]
    }
  ],
  "fixed_thunks": [
    {
      "rva": "0x5d9ff2",
      "bytes": 6,
      "expected_slot_offset": "0x270"
    },
    {
      "rva": "0x5d9f02",
      "bytes": 6,
      "expected_slot_offset": "0xd8"
    }
  ]
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');
 assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_entity_publication_thunks.mjs');
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
 assert(regular(self).size<=49152);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_entity_publication_thunks.plan.json');
 assert(regular(planPath).size<=16384);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-entity-publication-thunks');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:expectedPlan.source_pins,code_bytes:0,code_operations:0,metadata_bytes:0,metadata_operations:0,new_PE_header_bytes:0,new_OFT_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,whole_table_scanned:false,files:[],bindings:[],exports:[],thunks:[],
  disk_bindings_complete:false,unsupported:[],failures:[],limitations:[
   'Exactly two6B FF25 candidate thunks and bounded import/export metadata only; no original function or DLL is executed.',
   'Import symbols and export addresses do not prove loaded IAT values, function bounds, publication semantics or ABI compatibility.',
   'Only the two fixed thunk-derived imports plus fixed609A40 are supported; any opcode, ownership or name mismatch stops without fallback.',
   'Whole-file hash streams are identity checks, separate from the12B code plus966B metadata budgets, and are not atomic snapshots.',
   'Node path checks cannot identify every Windows reparse tag; separate Attributes preflight remains required.'
  ]};
 const data=new Map();
 function unsupported(reason){const e=new Error(reason);e.fixedUnsupported=true;throw e;}
 function verifyClosed(r){
  assert.equal(r.status,'passed');assert.equal(r.files.length,2);
  for(const f of r.files){const def=Object.values(expectedPlan.sources).find(s=>s.path===f.source);assert(def);assert(f.stable&&f.closed);assert.equal(f.sha256_before,def.sha256);assert.equal(f.sha256_after,def.sha256);}
 }
 function inspect(label,pe,body){
  const def=expectedPlan.sources[label],f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);
  let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){
   const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
  }return h.digest('hex');}
  function map(rva,n){
   assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};
  }
  function read(rva,n,kind,thunk=false){
   const {section,offset}=map(rva,n);assert(section.characteristics&0x40000000,'Unreadable PE section');
   if(thunk){
    assert.equal(label,'Game');assert.equal(n,6);assert(rva===0x5d9ff2||rva===0x5d9f02);
    assert(section.characteristics&0x20000000,'Thunk candidate is not executable disk data');
    assert(++report.code_operations<=2);assert(report.code_bytes+n<=12);report.code_bytes+=n;
   }else{
    assert(!(section.characteristics&0x20000000),'Metadata unexpectedly maps into executable section');
    assert(n<=151);assert(report.metadata_bytes+n<=966);assert(++report.metadata_operations<=21);report.metadata_bytes+=n;
   }
   const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);
   f.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:b.toString('hex')});return b;
  }
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);
   initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   body(read,map);
  }finally{
   if(fd!==undefined){try{
    f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;
   }finally{fs.closeSync(fd);f.closed=true;}}
  }
 }
 try{
  for(const pin of expectedPlan.source_pins){const p=local(pin.path);assert(regular(p).size<1048576);const bytes=fs.readFileSync(p);assert.equal(hash(bytes),pin.sha256);data.set(pin.key,JSON.parse(bytes));}
  const assembly=data.get('player_assembly').result.structuredContent.normalized_result;
  assert.equal(typeof assembly,'string');assert(assembly.startsWith('0x1803d5bc0: SUB RSP, 0x28'));
  for(const slot of expectedPlan.fixed_slots){for(const call of slot.calls){
   assert(assembly.split('\n').some(line=>line.toLowerCase()===call+': call qword ptr ['+hex(0x180000000+Number(slot.iat_rva))+']'));
  }}
  const slots=data.get('slots');assert.equal(slots.status,'passed');assert(slots.proofs_verified&&slots.disk_bindings_complete);
  assert.equal(slots.files.length,1);const sf=slots.files[0];assert.equal(sf.source,expectedPlan.sources.Game.path);
  assert(sf.stable&&sf.closed);assert.equal(sf.sha256_before,expectedPlan.sources.Game.sha256);assert.equal(sf.sha256_after,expectedPlan.sources.Game.sha256);
  for(const fixed of expectedPlan.fixed_thunks)for(const owner of ['Character','Player']){
   const hit=slots.values.filter(v=>v.owner===owner&&v.slot_offset===fixed.expected_slot_offset);assert.equal(hit.length,1);
   assert.equal(hit[0].candidate_rva,fixed.rva);assert.equal(hit[0].uint64_va,hex(0x180000000+Number(fixed.rva)));
   assert.equal(hit[0].bytes,8);assert.equal(hit[0].classification,'ExecutableAddressCandidate');
  }
  const gp=data.get('game_pe');assert.equal(gp.target,expectedPlan.sources.Game.path);assert(gp.source_hash_preserved&&gp.source_stat_stable&&gp.source_handle_closed);
  assert.equal(gp.sha256_before,expectedPlan.sources.Game.sha256);assert.equal(gp.sha256_after,expectedPlan.sources.Game.sha256);
  const gamePe=gp.pe;assert.equal(gamePe.image_base,'0x180000000');
  const engineRecords=data.get('engine_pe').files.filter(f=>f.target===expectedPlan.sources.Engine.path);assert.equal(engineRecords.length,1);
  const er=engineRecords[0];assert(er.source_hash_preserved&&er.source_stat_stable);assert.equal(er.sha256_before,expectedPlan.sources.Engine.sha256);assert.equal(er.sha256_after,expectedPlan.sources.Engine.sha256);assert.equal(er.preferred_image_base,'0x180000000');
  const enginePe=er.pe,ed=Buffer.from(enginePe.export_directory_bytes,'hex');assert.equal(ed.length,40);
  for(const [off,value] of [[16,enginePe.ordinal_base],[20,enginePe.function_count],[24,enginePe.named_export_count],[28,Number(enginePe.address_table_rva)],[32,Number(enginePe.name_pointer_table_rva)],[36,Number(enginePe.name_ordinal_table_rva)]])assert.equal(ed.readUInt32LE(off),value);
  for(const pe of [gamePe,enginePe]){assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);for(const s of pe.sections){for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);}}
  const imports=data.get('imports');assert.equal(imports.original_sha256_before,expectedPlan.sources.Game.sha256);assert.equal(imports.original_sha256_after,expectedPlan.sources.Game.sha256);assert(imports.original_stat_stable&&imports.original_hash_matches);
  const cached=imports.normal_imports[4].raw_metadata,descriptor=Buffer.from(cached.descriptor.bytes,'hex'),prefix=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');
  assert.equal(descriptor.length,20);assert.equal(descriptor.readUInt32LE(0),0xa42ac0);assert.equal(descriptor.readUInt32LE(12),0xa56aa6);assert.equal(descriptor.readUInt32LE(16),0x609000);
  assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(cached.ownership_lookup_prefix.entries,350);assert.equal(Number(cached.ownership_lookup_prefix.rva),0xa42ac0);assert.equal(prefix.length,2800);
  const tp=data.get('oft_tail');verifyClosed(tp);const tails=tp.files.find(f=>f.source===expectedPlan.sources.Game.path).reads.filter(r=>r.kind==='OFT-nonzero-continuation-through-selected-slot');
  assert.equal(tails.length,1);assert.equal(tails[0].rva,'0xa435b0');assert.equal(tails[0].bytes,1104);
  const tail=Buffer.from(tails[0].raw_hex,'hex');assert.equal(tail.length,1104);const oft=Buffer.concat([prefix,tail]);assert.equal(oft.length,488*8);
  for(let n=0;n<oft.length;n+=8)assert.notEqual(oft.readBigUInt64LE(n),0n);
  const modules=data.get('inventory').filter(m=>m.relativePath==='x64/Game.dll');assert.equal(modules.length,1);
  assert.equal(modules[0].sha256,expectedPlan.sources.Game.sha256);assert.equal(modules[0].bytes,expectedPlan.sources.Game.bytes);
  const im=modules[0].importModules.filter(m=>m.module==='Engine.dll');assert.equal(im.length,1);assert(im[0].symbols.length>=488);
  for(const symbol of im[0].symbols.slice(0,488))assert(typeof symbol==='string'&&symbol.length>0&&symbol.length<=150&&/^[\x20-\x7e]+$/.test(symbol));
  report.proofs_verified=true;
  inspect('Game',gamePe,(read)=>{
   const selected=new Map();
   function select(iat,origin,fixed){
    const index=(iat-0x609000)/8;
    if(!Number.isSafeInteger(index)||index<0||index>=488)unsupported('Derived slot is outside cached Engine import ownership');
    const prior=selected.get(iat);
    if(prior){prior.origins.push(origin);if(fixed)prior.fixed=fixed;return;}
    selected.set(iat,{iat,index,origins:[origin],fixed});
   }
   for(const fixed of expectedPlan.fixed_thunks){
    const rva=Number(fixed.rva),bytes=read(rva,6,'fixed-FF25-thunk-candidate',true);
    const observation={rva:fixed.rva,bytes:6,raw_hex:bytes.toString('hex'),opcode_verified:false,iat_rva:null};report.thunks.push(observation);
    if(bytes[0]!==0xff||bytes[1]!==0x25)unsupported('Fixed candidate '+fixed.rva+' is not FF25disp32; no alternative decoding');
    observation.opcode_verified=true;const displacement=bytes.readInt32LE(2),iat=rva+6+displacement;
    observation.displacement=displacement;observation.iat_rva=hex(iat);
    select(iat,{kind:'fixed-thunk',rva:fixed.rva,primary_slot_offset:fixed.expected_slot_offset});
   }
   for(const fixed of expectedPlan.fixed_slots)select(Number(fixed.iat_rva),{kind:'direct-IAT-call',calls:fixed.calls},fixed);
   assert(selected.size>=1&&selected.size<=3);
   for(const chosen of selected.values()){
    const {iat,index}=chosen,symbol=im[0].symbols[index];
    if(chosen.fixed){assert.equal(index,chosen.fixed.index);assert.equal(symbol,chosen.fixed.symbol);}
    assert(symbol.length>0&&symbol.length<=150&&/^[\x20-\x7e]+$/.test(symbol));
    const lookup=oft.readBigUInt64LE(index*8);if(lookup===0n||lookup>=0x100000000n)unsupported('Ordinal or unrepresentable exact import lookup');
    const nameRva=Number(lookup),wanted=Buffer.from(symbol+'\0','ascii');
    if(read(iat,8,'fixed-IAT-slot').readBigUInt64LE()!==lookup)unsupported('Disk IAT differs from cached exact OFT');
    const hint=read(nameRva,2,'fixed-import-hint').readUInt16LE();
    if(!read(nameRva+2,wanted.length,'fixed-import-name').equals(wanted))unsupported('Exact import name mismatch');
    report.bindings.push({dll:'Engine.dll',iat_rva:hex(iat),index,symbol,hint,lookup_rva:hex(0xa42ac0+index*8),hint_name_rva:hex(nameRva),origins:chosen.origins,loaded_iat_read:false});
   }
   assert.equal(report.code_bytes,12);assert.equal(report.code_operations,2);assert.equal(report.bindings.length,selected.size);
  });
  inspect('Engine',enginePe,(read,map)=>{
   for(const s of report.bindings){
    if(s.hint>=enginePe.named_export_count)unsupported('Import hint outside cached export range');
    const nameRva=read(Number(enginePe.name_pointer_table_rva)+s.hint*4,4,'exact-hinted-export-name-pointer').readUInt32LE();
    if(!read(nameRva,Buffer.byteLength(s.symbol,'ascii')+1,'exact-hinted-export-name').equals(Buffer.from(s.symbol+'\0','ascii')))unsupported('Export hint/name mismatch; no search fallback');
    const ordinalIndex=read(Number(enginePe.name_ordinal_table_rva)+s.hint*2,2,'exact-name-ordinal').readUInt16LE();
    if(ordinalIndex>=enginePe.function_count)unsupported('Exact ordinal outside EAT range');
    const target=read(Number(enginePe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();
    const exported={symbol:s.symbol,iat_rva:s.iat_rva,hint:s.hint,ordinal_index:ordinalIndex,ordinal:ordinalIndex+enginePe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target),target_body_read:false,function_boundary_verified:false,loaded_value_observed:false};
    report.exports.push(exported);
    if(target===0)unsupported('Null exact EAT entry');
    if(target>=Number(enginePe.export_directory_rva)&&target<Number(enginePe.export_directory_rva)+enginePe.export_directory_size)unsupported('Export forwarder; not followed');
    const candidates=enginePe.sections.filter(section=>target>=section.rva&&target<section.rva+section.raw_size);
    if(candidates.length!==1)unsupported('Export target lacks unique disk-backed section');
    const {section}=map(target,1);exported.target_section=section.name;
    if(!(section.characteristics&0x20000000))unsupported('Exact export target is not executable disk data');
    exported.classification='ExactNamedExportExecutableAddressCandidate';
   }
   assert.equal(report.exports.length,report.bindings.length);assert.equal(report.metadata_operations,report.bindings.length*7);
   assert.equal(report.metadata_bytes,report.bindings.reduce((sum,b)=>sum+20+2*(Buffer.byteLength(b.symbol,'ascii')+1),0));
   report.disk_bindings_complete=true;
  });
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();
 report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.disk_bindings_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('No complete binding and no explicit unsupported reason');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<131072);normal(out);
 fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed entity-publication thunk binding failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,thunks:report.thunks,bindings:report.bindings,exports:report.exports,unsupported:report.unsupported,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
