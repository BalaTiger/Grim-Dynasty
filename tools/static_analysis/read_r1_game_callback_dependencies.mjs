// Preparation only. This module performs no I/O until this explicitly reviewed entrypoint is called.
// It binds disk metadata; it neither invokes these targets nor interprets the function bodies.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
export function inspectGameCallbackDependencies(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-game-callback-dependencies.v1",
  "script": "tools/static_analysis/read_r1_game_callback_dependencies.mjs",
  "entrypoint": "inspectGameCallbackDependencies(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-game-callback-dependencies/inspection.json",
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
  "tables": [
    {
      "owner": "Character",
      "table_rva": "0x6df6d0",
      "slot_offset": "0x0",
      "rva": "0x6df6d0",
      "bytes": 8
    },
    {
      "owner": "Character",
      "table_rva": "0x6df6d0",
      "slot_offset": "0x1c8",
      "rva": "0x6df898",
      "bytes": 8
    },
    {
      "owner": "Character",
      "table_rva": "0x6df6d0",
      "slot_offset": "0x458",
      "rva": "0x6dfb28",
      "bytes": 8
    },
    {
      "owner": "Character",
      "table_rva": "0x6df6d0",
      "slot_offset": "0x830",
      "rva": "0x6dff00",
      "bytes": 8
    },
    {
      "owner": "Player",
      "table_rva": "0x787100",
      "slot_offset": "0x0",
      "rva": "0x787100",
      "bytes": 8
    },
    {
      "owner": "Player",
      "table_rva": "0x787100",
      "slot_offset": "0x1c8",
      "rva": "0x7872c8",
      "bytes": 8
    },
    {
      "owner": "Player",
      "table_rva": "0x787100",
      "slot_offset": "0x458",
      "rva": "0x787558",
      "bytes": 8
    },
    {
      "owner": "Player",
      "table_rva": "0x787100",
      "slot_offset": "0x830",
      "rva": "0x787930",
      "bytes": 8
    }
  ],
  "new_export_bindings": [
    {
      "iat_rva": "0x609050",
      "index": 10,
      "symbol": "?gEngine@GAME@@3PEAVEngine@1@EA",
      "kind": "data",
      "import_source": "cached-raw-import"
    },
    {
      "iat_rva": "0x609098",
      "index": 19,
      "symbol": "?Get@?$Singleton@VObjectManager@GAME@@@GAME@@SAPEAVObjectManager@2@XZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609518",
      "index": 163,
      "symbol": "?GetCoords@Entity@GAME@@QEBA?AVWorldCoords@2@XZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609530",
      "index": 166,
      "symbol": "?GetRegion@Entity@GAME@@QEBAPEAVRegion@2@XZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609a48",
      "index": 329,
      "symbol": "??GWorldVec3@GAME@@QEBA?AVVec3@1@AEBV01@@Z",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609d08",
      "index": 417,
      "symbol": "?GetMotion@Entity@GAME@@QEBAAEBVVec3@2@XZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609d98",
      "index": 435,
      "symbol": "?GetWorldPosition@WorldVec3@GAME@@QEBA?AVVec3@2@XZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609da0",
      "index": 436,
      "symbol": "?GetHeight@Actor@GAME@@QEBAMXZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609da8",
      "index": 437,
      "symbol": "?GetLastFrameUpdated@Entity@GAME@@QEBAIXZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609db0",
      "index": 438,
      "symbol": "?GetFrameCount@Engine@GAME@@QEBAIXZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609db8",
      "index": 439,
      "symbol": "?IsGameTimePaused@GAME@@YA_NXZ",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609dc0",
      "index": 440,
      "symbol": "?SetFromWorldPosition@WorldVec3@GAME@@QEAA_NAEBVVec3@2@PEAVRegion@2@@Z",
      "kind": "function",
      "import_source": "exact-new-metadata"
    },
    {
      "iat_rva": "0x609dc8",
      "index": 441,
      "symbol": "?ExecuteEventHook@HookPack@GAME@@QEBAXI@Z",
      "kind": "function",
      "import_source": "exact-new-metadata"
    }
  ],
  "reused_bindings": [
    {
      "iat_rva": "0x6094a0",
      "index": 148,
      "symbol": "?GetRegion@WorldVec3@GAME@@QEBAPEAVRegion@2@XZ",
      "target_rva": "0x22c0b0",
      "proof_key": "world_region"
    },
    {
      "iat_rva": "0x609720",
      "index": 228,
      "symbol": "??0WorldVec3@GAME@@QEAA@XZ",
      "target_rva": "0x228ab0",
      "proof_key": "world_ctor"
    },
    {
      "iat_rva": "0x609bb0",
      "index": 374,
      "symbol": "?PutOnFloor@WorldVec3@GAME@@QEAA_NXZ",
      "target_rva": "0x22c620",
      "proof_key": "floor"
    }
  ],
  "source_pins": [
    {
      "key": "imports",
      "path": "docs/object-lifecycle/offline-questitem-imports/import-bindings.json",
      "sha256": "94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e"
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
      "key": "oft_tail",
      "path": "docs/reverse-client/r1-navigation-binding/inspection.json",
      "sha256": "d0c171ce77a6d67e153a6911fc7c3a17ea4d61e80ab9492f3287384524e14264"
    },
    {
      "key": "inventory",
      "path": "docs/phase-1/boundary-audit/module-boundaries.json",
      "sha256": "d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7"
    },
    {
      "key": "world_region",
      "path": "docs/reverse-client/r1-camera-target-bindings/inspection.json",
      "sha256": "2a64f2baaf6549b24fc2d0e3071c6e91c5ff2b18de3195d990dc32b23026608a"
    },
    {
      "key": "world_ctor",
      "path": "docs/object-lifecycle/offline-worldvec3-constructor-boundary/metadata.json",
      "sha256": "030c5dfda0c05127a67e6fb4dd1b77996ff6248f58b0f1a33e3a8f592c467dad"
    },
    {
      "key": "floor",
      "path": "docs/reverse-client/r1-move-and-floor-bindings/inspection.json",
      "sha256": "66785d22a81c7f2ee2fc2fd497836b3b56f4a12310f31131f73721ef9a266665"
    },
    {
      "key": "character_ctor",
      "path": "docs/reverse-client/r1-game-entity-constructors/05-GameEntityConstructor-0x180052030-procedure_assembly.json",
      "sha256": "34a334ae8f918b20469fb57e7d55af0ffbcfba0d5900c42a4e371192d534f856",
      "procedure": "0x180052030",
      "instructions": [
        "0x180052055: MOV R15, RCX",
        "0x18005205f: LEA RAX, [0x1806df6d0]",
        "0x180052066: MOV qword ptr [R15], RAX"
      ]
    },
    {
      "key": "player_ctor",
      "path": "docs/reverse-client/r1-game-entity-constructors/08-GameEntityConstructor-0x1803cc3f0-procedure_assembly.json",
      "sha256": "d68276f2f2212942eb66cf2180f34f0b527f5b9a48f473b4ac35098b3d479ded",
      "procedure": "0x1803cc3f0",
      "instructions": [
        "0x1803cc415: MOV RDI, RCX",
        "0x1803cc418: CALL 0x180052030",
        "0x1803cc41e: LEA RAX, [0x180787100]",
        "0x1803cc425: MOV qword ptr [RDI], RAX"
      ]
    },
    {
      "key": "created",
      "path": "docs/reverse-client/r1-game-movement-interface/05-GameMovementInterface-0x180066c20-procedure_assembly.json",
      "sha256": "1613fb9d39f0fdc7967e5fba6ab7a3cd0f0a6468d04e32cfe4565e8ae7a316fe",
      "procedure": "0x180066c20",
      "instructions": [
        "0x180066c4f: CALL qword ptr [0x180609518]",
        "0x180066c5a: CALL qword ptr [0x1806094a0]",
        "0x180066c6f: CALL qword ptr [0x180609d98]",
        "0x180066cbf: CALL qword ptr [0x180609da0]",
        "0x180066cce: CALL qword ptr [0x180609d08]",
        "0x180066d12: CALL qword ptr [RAX + 0x1c8]"
      ]
    },
    {
      "key": "update",
      "path": "docs/reverse-client/r1-game-movement-interface/11-GameMovementInterface-0x180066d60-procedure_assembly.json",
      "sha256": "39c761a562f79f260e94f94b816e38d3fc8d0fe61daad05394c7b493c55b0132",
      "procedure": "0x180066d60",
      "instructions": [
        "0x180066e04: CALL qword ptr [RAX]",
        "0x180066e4a: CALL qword ptr [RAX + 0x458]",
        "0x180066e8d: MOV RCX, qword ptr [0x180609050]",
        "0x180066e97: CALL qword ptr [0x180609db0]",
        "0x180066ea3: CALL qword ptr [0x180609da8]"
      ]
    },
    {
      "key": "moved",
      "path": "docs/reverse-client/r1-game-movement-interface/14-GameMovementInterface-0x180066ee0-procedure_assembly.json",
      "sha256": "78d9bfaa180ff24e5d0cb8dfa8e77b25298bf92bfca1c4d5c8962a8dd1b3ccce",
      "procedure": "0x180066ee0",
      "instructions": [
        "0x180066f29: CALL qword ptr [0x180609db8]",
        "0x180066f3b: CALL qword ptr [0x180609720]",
        "0x180066f45: CALL qword ptr [0x180609530]",
        "0x180066f55: CALL qword ptr [0x180609dc0]",
        "0x180066f67: CALL qword ptr [0x180609bb0]",
        "0x180066fdf: CALL qword ptr [0x180609a48]"
      ]
    },
    {
      "key": "reached",
      "path": "docs/reverse-client/r1-game-movement-interface/17-GameMovementInterface-0x180067170-procedure_assembly.json",
      "sha256": "c9282463916cd02ecc5c95af0010f9cba3fca714678b3e2f0900dfd5c7b1d888",
      "procedure": "0x180067170",
      "instructions": [
        "0x1800671f7: CALL qword ptr [RAX + 0x830]",
        "0x18006720e: CALL qword ptr [0x180609098]",
        "0x180067273: JMP qword ptr [0x180609dc8]"
      ]
    }
  ],
  "limits": {
    "table_bytes": 64,
    "metadata_bytes": 4096,
    "metadata_single_read": 512,
    "metadata_operations": 128,
    "hash_chunk_bytes": 65536,
    "code_bytes": 0,
    "new_OFT_bytes": 0
  },
  "requirements": [
    "Separate explicit execution approval; preparation and module import perform no original I/O.",
    "External Windows Attributes/reparse-tag precheck of both originals and output ancestors.",
    "Reuse pinned Game/Engine PE and exact Engine export-directory metadata, and nonzero Game OFT prefix0..487.",
    "Read only eight primary-table QWORDs; do not read surrounding slots or function bodies.",
    "Resolve only the13 listed Engine exports by exact import hint/name/ordinal/EAT; no binary search, fallback, whole name table or pdata scan.",
    "Reuse3 already exact bindings and cached gEngine raw import bytes; no repeated original metadata reads for those records.",
    "gEngine export address only; no imported data payload, loaded IAT or runtime global read.",
    "New output directory and exclusive evidence write; same-descriptor before/after SHA256/stat/normal-path verification and close.",
    "No DLL loading, execution, Rea/backend, subprocess, network, saves or security change."
  ]
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');
 assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_game_callback_dependencies.mjs');
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
 assert(regular(self).size<=65536);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_game_callback_dependencies.plan.json');
 assert(regular(planPath).size<=32768);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-game-callback-dependencies');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:expectedPlan.source_pins,metadata_bytes:0,metadata_operations:0,data_bytes:0,code_bytes:0,subprocesses:0,
  original_loaded_or_executed:false,whole_export_table_scanned:false,new_OFT_bytes:0,files:[],bindings:[],exports:[],reused_bindings:[],values:[],failures:[],
  primary_tables:{interface_offset:0,constructor_writes_verified:false,player_overwrite_verified:false,table_extents_verified:false,
   subsequent_callee_writes_excluded:false,loaded_values_observed:false},
  limitations:[
   'Primary vptrs are constructor-written Character6DF6D0 and Player787100 at full-object+0. These differ from their movement-interface vptrs at+38.',
   'Constructor call effects and later writes remain unknown. Eight disk slot values do not establish full table extent, callable ABI, complete constructor success or loaded object identity.',
   'The13 exact exports and3 reused bindings cover the fixed observed movement-interface imports only. No implementation or original runtime compatibility is inferred.',
   'gEngine is a data-symbol address binding only; no Engine global payload or loaded pointer is read, initialized or presumed zero.',
   'GetCoords by-value output and GetWorldPosition returned-pointer identity require their actual bodies; metadata does not imply hidden-buffer alias.',
   'Pinned nonzero OFT continuity is not uniqueness across every import descriptor. Hint mismatch fails without a wider export-name search.',
   'PE section/file bounds are reused only with exact original SHA256 equality. Whole-file streams are hashes, separate from metadata budgets, not atomic snapshots.',
   'Node path checks do not identify every Windows reparse tag; external Attributes preflight remains required.'
  ]};
 const data=new Map();
 function verifyClosed(r){assert.equal(r.status,'passed');for(const f of r.files){const def=f.source===expectedPlan.sources.Game.path?expectedPlan.sources.Game:expectedPlan.sources.Engine;
  assert.equal(f.source,def.path);assert(f.stable&&f.closed);assert.equal(f.sha256_before,def.sha256);assert.equal(f.sha256_after,def.sha256);}}
 function inspect(def,pe,body){
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);
  let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){
   const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
  }return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(offset>=0&&offset+n<=def.bytes);return {section,offset};}
  function read(rva,n,kind,isTable=false){const {section,offset}=map(rva,n);
   assert((section.characteristics&0x40000000)!==0&&(section.characteristics&0x20000000)===0,'Data/metadata read from code section refused');
   if(isTable){assert.equal(n,8);assert(report.data_bytes+n<=64);report.data_bytes+=n;}
   else{assert(n<=512);assert(report.metadata_bytes+n<=4096);assert(++report.metadata_operations<=128);report.metadata_bytes+=n;}
   const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);
   f.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:b.toString('hex')});return b;
  }
  try{const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);
   initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);body(read,map);}
  finally{if(fd!==undefined){try{f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;}
    finally{fs.closeSync(fd);f.closed=true;}}}
 }
 try{
  for(const p of expectedPlan.source_pins){const file=local(p.path);assert(regular(file).size<1048576);const bytes=fs.readFileSync(file);assert.equal(hash(bytes),p.sha256);const d=JSON.parse(bytes);data.set(p.key,d);
   if(p.procedure){const e=d.result.structuredContent;assert.equal(e.subject.name,'Game.dll');assert.equal(e.subject.digest.sha256,expectedPlan.sources.Game.sha256);
    assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(expectedPlan.sources.Game.path).toLowerCase());
    assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,p.procedure);
    assert.equal(typeof e.normalized_result,'string');const lines=e.normalized_result.split('\n');for(const line of p.instructions)assert(lines.includes(line),'Missing instruction: '+line);}
  }
  report.proofs_verified=true;report.primary_tables.constructor_writes_verified=true;report.primary_tables.player_overwrite_verified=true;
  const imports=data.get('imports');assert.equal(imports.original_sha256_before,expectedPlan.sources.Game.sha256);assert.equal(imports.original_sha256_after,expectedPlan.sources.Game.sha256);assert(imports.original_stat_stable&&imports.original_hash_matches);
  const cached=imports.normal_imports[4].raw_metadata,descriptor=Buffer.from(cached.descriptor.bytes,'hex'),prefix=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');
  assert.equal(descriptor.length,20);assert.equal(descriptor.readUInt32LE(0),0xa42ac0);assert.equal(descriptor.readUInt32LE(12),0xa56aa6);assert.equal(descriptor.readUInt32LE(16),0x609000);
  assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(cached.ownership_lookup_prefix.entries,350);assert.equal(Number(cached.ownership_lookup_prefix.rva),0xa42ac0);assert.equal(prefix.length,2800);
  const tailProof=data.get('oft_tail');verifyClosed(tailProof);const tails=tailProof.files.find(f=>f.source===expectedPlan.sources.Game.path).reads.filter(r=>r.kind==='OFT-nonzero-continuation-through-selected-slot');
  assert.equal(tails.length,1);assert.equal(tails[0].rva,'0xa435b0');assert.equal(tails[0].bytes,1104);
  const tail=Buffer.from(tails[0].raw_hex,'hex');assert.equal(tail.length,1104);const oft=Buffer.concat([prefix,tail]);assert.equal(oft.length,488*8);
  for(let at=0;at<oft.length;at+=8)assert.notEqual(oft.readBigUInt64LE(at),0n);
  const modules=data.get('inventory').filter(m=>m.relativePath==='x64/Game.dll');assert.equal(modules.length,1);assert.equal(modules[0].sha256,expectedPlan.sources.Game.sha256);assert.equal(modules[0].bytes,expectedPlan.sources.Game.bytes);
  const im=modules[0].importModules.filter(m=>m.module==='Engine.dll');assert.equal(im.length,1);
  for(const s of [...expectedPlan.new_export_bindings,...expectedPlan.reused_bindings]){assert.equal((Number(s.iat_rva)-0x609000)/8,s.index);assert.equal(im[0].symbols[s.index],s.symbol);}
  const gp=data.get('game_pe');assert.equal(gp.sha256_after,expectedPlan.sources.Game.sha256);const gamePe=gp.pe;assert.equal(gamePe.image_base,'0x180000000');
  const ep=data.get('engine_pe').files.filter(f=>f.target===expectedPlan.sources.Engine.path);assert.equal(ep.length,1);assert(ep[0].source_hash_preserved&&ep[0].source_stat_stable);
  assert.equal(ep[0].preferred_image_base,'0x180000000');
  assert.equal(ep[0].sha256_before,expectedPlan.sources.Engine.sha256);assert.equal(ep[0].sha256_after,expectedPlan.sources.Engine.sha256);
  const enginePe=ep[0].pe,ed=Buffer.from(enginePe.export_directory_bytes,'hex');assert.equal(ed.length,40);
  for(const [off,value] of [[16,enginePe.ordinal_base],[20,enginePe.function_count],[24,enginePe.named_export_count],[28,Number(enginePe.address_table_rva)],[32,Number(enginePe.name_pointer_table_rva)],[36,Number(enginePe.name_ordinal_table_rva)]])assert.equal(ed.readUInt32LE(off),value);
  for(const s of expectedPlan.reused_bindings){const proof=data.get(s.proof_key);let b,e;
   if(s.proof_key==='world_ctor'){assert.equal(proof.status,'exact-export-without-exact-runtime-function');assert(proof.source_closed&&proof.source_hash_preserved&&proof.source_stat_stable);assert.equal(proof.sha256_before,expectedPlan.sources.Engine.sha256);assert.equal(proof.sha256_after,expectedPlan.sources.Engine.sha256);b=proof.source_binding;e=proof.export;assert.equal(e.name,s.symbol);}
   else{verifyClosed(proof);const bs=proof.bindings.filter(x=>x.iat_rva===s.iat_rva),es=proof.exports.filter(x=>x.iat_rva===s.iat_rva);assert.equal(bs.length,1);assert.equal(es.length,1);b=bs[0];e=es[0];assert.equal(e.symbol,s.symbol);}
   assert.equal(b.iat_rva,s.iat_rva);assert.equal(b.symbol,s.symbol);assert.equal(e.target_rva,s.target_rva);assert.equal(b.hint,e.hint===undefined?e.index:e.hint);
   report.reused_bindings.push({...s,hint:b.hint,target_va:e.target_va,reused_without_new_original_read:true,loaded_value_observed:false});
  }
  inspect(expectedPlan.sources.Game,gamePe,(read)=>{
   for(const t of expectedPlan.tables){assert.equal(Number(t.rva),Number(t.table_rva)+Number(t.slot_offset));assert.equal(t.bytes,8);
    const raw=read(Number(t.rva),8,'primary-vtable-exact-slot',true),value=raw.readBigUInt64LE(),rv=value>=0x180000000n&&value<=0x180ffffffffn?Number(value-0x180000000n):null;
    const sections=rv===null?[]:gamePe.sections.filter(s=>(s.characteristics&0x20000000)&&rv>=s.rva&&rv<s.rva+s.raw_size);
    report.values.push({...t,raw_hex:raw.toString('hex'),uint64_va:hex(value),candidate_rva:rv===null?null:hex(rv),classification:sections.length===1?'ExecutableAddressCandidate':'Unknown',function_boundary_verified:false,loaded_value_observed:false});
   }
   assert.equal(report.data_bytes,64);
   for(const s of expectedPlan.new_export_bindings){const lookup=oft.readBigUInt64LE(s.index*8);assert(lookup>0n&&lookup<0x100000000n);const nameRva=Number(lookup);let hint;
    if(s.import_source==='cached-raw-import'){
     const matches=imports.normal_imports.filter(x=>x.iat_va==='0x180609050');assert.equal(matches.length,1);const old=matches[0],raw=old.raw_metadata;
     assert.equal(old.name,s.symbol);assert.equal(old.thunk_index,s.index);assert.equal(Number(raw.iat.rva),Number(s.iat_rva));assert.equal(Buffer.from(raw.iat.bytes,'hex').readBigUInt64LE(),lookup);
     assert.equal(Number(raw.target_lookup.rva),0xa42ac0+s.index*8);assert.equal(Buffer.from(raw.target_lookup.bytes,'hex').readBigUInt64LE(),lookup);
     assert.equal(Number(raw.hint.rva),nameRva);hint=Buffer.from(raw.hint.bytes,'hex').readUInt16LE();assert.equal(hint,old.hint);
     assert.equal(Number(raw.symbol_name_with_nul.rva),nameRva+2);assert(Buffer.from(raw.symbol_name_with_nul.bytes,'hex').equals(Buffer.from(s.symbol+'\0','ascii')));
    }else{assert.equal(read(Number(s.iat_rva),8,'exact-IAT-slot').readBigUInt64LE(),lookup);
     hint=read(nameRva,2,'exact-import-hint').readUInt16LE();const wanted=Buffer.from(s.symbol+'\0','ascii');
     assert(read(nameRva+2,wanted.length,'exact-import-name').equals(wanted),'Import name mismatch: stop without fallback');}
    report.bindings.push({...s,dll:'Engine.dll',hint,lookup_rva:hex(0xa42ac0+s.index*8),hint_name_rva:hex(nameRva)});
   }
  });
  inspect(expectedPlan.sources.Engine,enginePe,(read,map)=>{
   for(const s of report.bindings){assert(s.hint<enginePe.named_export_count);
    const nameRva=read(Number(enginePe.name_pointer_table_rva)+s.hint*4,4,'exact-hinted-export-name-pointer').readUInt32LE();
    const wanted=Buffer.from(s.symbol+'\0','ascii');assert(read(nameRva,wanted.length,'exact-export-name').equals(wanted),'Export hint mismatch: stop without fallback');
    const ordinalIndex=read(Number(enginePe.name_ordinal_table_rva)+s.hint*2,2,'exact-name-ordinal').readUInt16LE();assert(ordinalIndex<enginePe.function_count);
    const target=read(Number(enginePe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();assert(target!==0);
    assert(!(target>=Number(enginePe.export_directory_rva)&&target<Number(enginePe.export_directory_rva)+enginePe.export_directory_size),'Forwarder unsupported');
    let targetSection;
    if(s.kind==='function'){targetSection=map(target,1).section;assert(targetSection.characteristics&0x20000000);}
    else{const hits=enginePe.sections.filter(p=>target>=p.rva&&target<p.rva+Math.max(p.virtual_size,p.raw_size));assert.equal(hits.length,1);targetSection=hits[0];assert((targetSection.characteristics&0x40000000)&&!(targetSection.characteristics&0x20000000));}
    report.exports.push({iat_rva:s.iat_rva,symbol:s.symbol,kind:s.kind,hint:s.hint,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+enginePe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target),target_section:targetSection.name,target_payload_read:false,code_read:false,loaded_value_observed:false});
   }
  });
  assert.equal(report.values.length,8);assert.equal(report.exports.length,13);assert.equal(report.reused_bindings.length,3);
 }catch(e){report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<262144);normal(out);
 fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed Game callback dependency binding failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,data_bytes:report.data_bytes,metadata_bytes:report.metadata_bytes,exports:report.exports,reused_bindings:report.reused_bindings,values:report.values,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}

