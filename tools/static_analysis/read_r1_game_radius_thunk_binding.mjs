// Fixed disk observation only. Importing this module does not perform I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
export function inspectGameRadiusThunkBinding(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-game-radius-thunk-binding.v1",
  "script": "tools/static_analysis/read_r1_game_radius_thunk_binding.mjs",
  "entrypoint": "inspectGameRadiusThunkBinding(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-game-radius-thunk-binding/inspection.json",
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
  "fixed_candidate": {
    "rva": "0x5d9f8c",
    "bytes": 6,
    "va": "0x1805d9f8c",
    "source": "callback_slots",
    "slot_offset": "0x1c8",
    "character_slot_rva": "0x6df898",
    "player_slot_rva": "0x7872c8"
  },
  "conditional_binding": {
    "required_prefix_hex": "ff25",
    "signed_disp32_offset": 2,
    "next_instruction_rva": "0x5d9f92",
    "iat_owner": "Only pinned Engine.dll IAT prefix indices0..487",
    "iat_base_rva": "0x609000",
    "lookup_base_rva": "0xa42ac0",
    "lookup_entries": 488,
    "lookup_source": "cached350-entry prefix plus cached138-entry continuation",
    "original_reads": "Exact derived Game IAT8B + import hint2B + inventory-selected name; exact hinted Engine export name pointer4B/name/ordinal2B/EAT4B.",
    "fallback": "Unsupported; no alternate opcode decoder, IAT scan, export-name search, forwarder traversal or target body read."
  },
  "source_pins": [
    {
      "key": "callback_slots",
      "path": "docs/reverse-client/r1-game-callback-dependencies/inspection.json",
      "sha256": "c0f0348d8f0712b35efd22d7ed36282065bfc31c78ee18b0f1d870e036a4afab"
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
    "candidate_bytes": 6,
    "metadata_bytes": 1056,
    "metadata_operations": 7,
    "metadata_single_read": 512,
    "hash_chunk_bytes": 65536,
    "new_PE_header_bytes": 0,
    "new_OFT_bytes": 0,
    "target_body_bytes": 0
  },
  "requirements": [
    "Preparation and module import perform no filesystem I/O. Separate explicit execution approval is required.",
    "External Windows Attributes/reparse-tag precheck of both fixed originals and output ancestors is required.",
    "Reuse pinned PE maps only after the current same-descriptor whole-file hash equals the corresponding original fingerprint.",
    "Read at most6B at the sole Game RVA5D9F8C. Do not classify it as an instruction until the observed bytes and executable-section mapping meet the exact FF25 gate.",
    "Only the computed IAT entry inside the pinned Engine.dll prefix may be bound. Unsupported cases are preserved without broader reads.",
    "Use the inventory-selected exact ASCII symbol length and its actual import hint for a single Engine export match; no whole tables or binary search.",
    "Same-descriptor before/after SHA256/stat; fixed new output directory rejects overwrite and keeps partial failure/Unsupported evidence.",
    "No DLL loading/execution, backend, subprocess, network, save data, runtime globals, security changes or radius-behavior claim."
  ]
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');
 assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_game_radius_thunk_binding.mjs');
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
 const planPath=local('tools/static_analysis/read_r1_game_radius_thunk_binding.plan.json');
 assert(regular(planPath).size<=16384);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-game-radius-thunk-binding');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:expectedPlan.source_pins,code_bytes:0,metadata_bytes:0,metadata_operations:0,new_PE_header_bytes:0,new_OFT_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,whole_table_scanned:false,files:[],candidate:null,binding:null,export:null,
  semantic_binding_complete:false,unsupported:[],failures:[],limitations:[
   'This bounded byte read addresses a procedure-mapping gap, not an approval denial. No Rea retry or function semantics is performed here.',
   'A disk FF25 observation is decoded only in this fixed x64 Game image; the six bytes alone do not establish a complete procedure boundary or loaded control flow.',
   'An exact imported symbol and export address do not establish radius semantics, calling ABI, loaded IAT values or runtime compatibility.',
   'Only the pinned Engine.dll IAT prefix0..487 is supported. Other ownership, ordinal imports, forwarders or hint mismatches stop without fallback.',
   'Cached PE/import metadata is reused only with identical original fingerprints. Whole-file hash streams are separate from the6B semantic and1056B metadata budgets, and are not atomic snapshots.',
   'Node path checks cannot identify every Windows reparse tag; the separate Attributes preflight remains required.'
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
  function read(rva,n,kind,code=false){
   const {section,offset}=map(rva,n);assert(section.characteristics&0x40000000,'Unreadable PE section');
   if(code){
    assert.equal(label,'Game');assert.equal(rva,0x5d9f8c);assert.equal(n,6);assert.equal(report.code_bytes,0);
    if(!(section.characteristics&0x20000000))unsupported('Fixed candidate is not mapped to one executable section');
    report.code_bytes+=n;
   }else{
    assert(!(section.characteristics&0x20000000),'Metadata unexpectedly maps into executable section');
    assert(n<=512);assert(report.metadata_bytes+n<=1056);assert(++report.metadata_operations<=7);report.metadata_bytes+=n;
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
  const slots=data.get('callback_slots');verifyClosed(slots);
  for(const [owner,rva] of [['Character','0x6df898'],['Player','0x7872c8']]){
   const values=slots.values.filter(v=>v.owner===owner&&v.rva===rva&&v.slot_offset==='0x1c8');assert.equal(values.length,1);const v=values[0];
   assert.equal(v.candidate_rva,'0x5d9f8c');assert.equal(v.uint64_va,'0x1805d9f8c');assert.equal(v.classification,'ExecutableAddressCandidate');
   const raw=Buffer.from(v.raw_hex,'hex');assert.equal(raw.length,8);assert.equal(raw.readBigUInt64LE(),0x1805d9f8cn);
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
  report.proofs_verified=true;
  inspect('Game',gamePe,(read)=>{
   const raw=read(0x5d9f8c,6,'fixed-vtable-candidate-bytes',true);
   report.candidate={rva:'0x5d9f8c',va:'0x1805d9f8c',raw_hex:raw.toString('hex'),bytes:6,classification:'UnsupportedBytes',function_boundary_verified:false,loaded_value_observed:false};
   if(raw[0]!==0xff||raw[1]!==0x25)unsupported('Six observed bytes are not the sole supported FF25 RIP-relative form');
   const disp=raw.readInt32LE(2),iat=0x5d9f92+disp;
   report.candidate.classification='ObservedFF25RipRelativeIndirectJump';report.candidate.signed_disp32=disp;report.candidate.iat_rva=hex(iat);
   if(!Number.isSafeInteger(iat)||iat<0x609000||(iat-0x609000)%8!==0||(iat-0x609000)/8>=488)unsupported('Computed IAT is outside the pinned Engine.dll prefix; no ownership scan performed');
   const index=(iat-0x609000)/8,symbol=im[0].symbols[index];
   if(typeof symbol!=='string'||symbol.length===0||symbol.length>511||!/^[\x20-\x7e]+$/.test(symbol))unsupported('Pinned import symbol is not an exact bounded ASCII name');
   const lookup=oft.readBigUInt64LE(index*8);if(lookup===0n||lookup>=0x100000000n)unsupported('Ordinal or unrepresentable import lookup is outside this exact-name plan');
   const nameRva=Number(lookup),wanted=Buffer.from(symbol+'\0','ascii');
   const iatRaw=read(iat,8,'derived-exact-IAT-slot');if(iatRaw.readBigUInt64LE()!==lookup)unsupported('Disk IAT does not equal the pinned OFT entry');
   const hint=read(nameRva,2,'derived-exact-import-hint').readUInt16LE();
   if(!read(nameRva+2,wanted.length,'derived-exact-import-name').equals(wanted))unsupported('Exact selected import name mismatch');
   report.binding={dll:'Engine.dll',iat_rva:hex(iat),index,symbol,hint,lookup_rva:hex(0xa42ac0+index*8),hint_name_rva:hex(nameRva),loaded_iat_read:false};
  });
  if(!report.binding)unsupported('No exact import binding produced');
  inspect('Engine',enginePe,(read,map)=>{
   const s=report.binding;if(s.hint>=enginePe.named_export_count)unsupported('Import hint is outside the pinned named-export range');
   const nameRva=read(Number(enginePe.name_pointer_table_rva)+s.hint*4,4,'exact-hinted-export-name-pointer').readUInt32LE();
   if(!read(nameRva,Buffer.byteLength(s.symbol,'ascii')+1,'exact-hinted-export-name').equals(Buffer.from(s.symbol+'\0','ascii')))unsupported('Export hint/name mismatch; no fallback name search');
   const ordinalIndex=read(Number(enginePe.name_ordinal_table_rva)+s.hint*2,2,'exact-name-ordinal').readUInt16LE();
   if(ordinalIndex>=enginePe.function_count)unsupported('Exact name ordinal is outside the EAT range');
   const target=read(Number(enginePe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();
   report.export={symbol:s.symbol,hint:s.hint,ordinal_index:ordinalIndex,ordinal:ordinalIndex+enginePe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target),target_body_read:false,function_boundary_verified:false,loaded_value_observed:false};
   if(target===0)unsupported('Exact EAT entry is null');
   if(target>=Number(enginePe.export_directory_rva)&&target<Number(enginePe.export_directory_rva)+enginePe.export_directory_size)unsupported('Exact EAT entry is a forwarder; not followed');
   const candidates=enginePe.sections.filter(section=>target>=section.rva&&target<section.rva+section.raw_size);
   if(candidates.length!==1)unsupported('Exact export target has no unique disk-backed section; code/data kind unresolved');
   const {section}=map(target,1);report.export.target_section=section.name;
   if(!(section.characteristics&0x20000000))unsupported('Exact export target is not mapped to executable disk data; function/data kind unresolved');
   report.export.classification='ExactNamedExportExecutableAddressCandidate';report.semantic_binding_complete=true;
  });
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();
 report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.semantic_binding_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('No complete binding and no explicit unsupported reason');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<131072);normal(out);
 fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed radius candidate binding failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,candidate:report.candidate,binding:report.binding,export:report.export,unsupported:report.unsupported,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
