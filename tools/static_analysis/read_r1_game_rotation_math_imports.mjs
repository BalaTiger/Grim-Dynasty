// Fixed metadata supplement only. Importing this module performs no I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectGameRotationMathImports(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-game-rotation-math-imports.v1",
  "script": "tools/static_analysis/read_r1_game_rotation_math_imports.mjs",
  "entrypoint": "inspectGameRotationMathImports(reviewedScriptSha256, reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-game-rotation-math-imports/inspection.json",
  "source": {
    "path": "D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll",
    "bytes": 11385856,
    "sha256": "cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2"
  },
  "source_pins": [
    {
      "key": "rotate_assembly",
      "path": "docs/reverse-client/r1-game-callback-remaining-retry-1/11-GameCallbackRemaining-0x18005e160-procedure_assembly.json",
      "sha256": "722a7bbea67a6242d0e06bc8db2dc6614b02a25ac872ec7c1bd046cfa9060a49"
    },
    {
      "key": "axis_assembly",
      "path": "docs/reverse-client/r1-game-movement-math/05-GameMovementMath-0x180591a10-procedure_assembly.json",
      "sha256": "9af598bbe5f1c1996d7b89885d69318ff94bd74ba26e219f4efecc1a33eeb4f5"
    },
    {
      "key": "game_pe",
      "path": "docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json",
      "sha256": "227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b"
    },
    {
      "key": "imports",
      "path": "docs/object-lifecycle/offline-questitem-imports/import-bindings.json",
      "sha256": "94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e"
    },
    {
      "key": "inventory",
      "path": "docs/phase-1/boundary-audit/module-boundaries.json",
      "sha256": "d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7"
    },
    {
      "key": "inventory_source",
      "path": "docs/phase-1/boundary-audit/boundary_inventory.py",
      "sha256": "354e00aeefbdfbc1beefa3de4f2c8c93635a0cb9ad824329f0011454f47ce224"
    }
  ],
  "descriptor": {
    "index": 13,
    "rva": "0xa42a44",
    "bytes": 20,
    "dll": "api-ms-win-crt-math-l1-1-0.dll",
    "symbols": [
      "_finite",
      "exp",
      "pow",
      "log",
      "log10f",
      "logf",
      "cosf",
      "log10",
      "truncf",
      "acosf",
      "atan2f",
      "fmodf",
      "powf",
      "sinf",
      "tanf"
    ],
    "provenance": "Pinned inventory emits ordinary import descriptors in original order; pinned prior import read log locates descriptor0 at file10751808, and cached Engine descriptor3 at RVA A4297C verifies base A42940. The selected descriptor13 and its exact DLL name are newly read and verified; no descriptor scan."
  },
  "fixed_thunks": [
    {
      "rva": "0x5dc3f2",
      "bytes": 6,
      "symbol": "acosf",
      "index": 9,
      "source_key": "rotate_assembly",
      "instruction": "0x18005e334: CALL 0x1805dc3f2"
    },
    {
      "rva": "0x5dc392",
      "bytes": 6,
      "symbol": "cosf",
      "index": 6,
      "source_key": "axis_assembly",
      "instruction": "0x180591a30: CALL 0x1805dc392"
    },
    {
      "rva": "0x5dc40a",
      "bytes": 6,
      "symbol": "sinf",
      "index": 13,
      "source_key": "axis_assembly",
      "instruction": "0x180591a3b: CALL 0x1805dc40a"
    }
  ],
  "limits": {
    "code_bytes": 18,
    "code_operations": 3,
    "metadata_bytes": 121,
    "metadata_operations": 14,
    "metadata_single_read": 31,
    "hash_chunk_bytes": 65536,
    "new_PE_header_bytes": 0,
    "new_descriptor_bytes": 20,
    "new_OFT_bytes": 24,
    "target_body_bytes": 0
  },
  "requirements": [
    "Module import performs no I/O. Separate formal approval requires exact frozen script and plan SHA256.",
    "External Windows Attributes/reparse-tag precheck of original, workspace, script/plan, pinned sources and output ancestors is required; no security or permission change.",
    "Open only the fixed Game.dll read-only, reuse cached PE sections only with before/after same-handle full SHA256 and stat equality.",
    "Read exactly three fixed6B thunk candidates, accepting only FF25disp32. Opcode mismatch stops explicitly; no alternative instruction decoding or thunk chain.",
    "Read only the one fixed descriptor13 and exact DLL string. Each derived IAT must match its cached symbol index; read its exact OFT8B, IAT8B, hint2B and exact symbol with NUL.",
    "No original table scan, export lookup, API-set resolution, CRT DLL open/load/execution, imported function execution, backend, subprocess, network or game/save access.",
    "No runtime ABI/numerical behavior is inferred from import names. Actual host DLL resolution, CRT version, binary32 exceptional values/rounding and FP environment remain unverified.",
    "New output directory refuses overwrite; partial failure or Unsupported is preserved with original hash/stat/close outcomes."
  ]
};

 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_game_rotation_math_imports.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  const s=fs.lstatSync(q);assert(!s.isSymbolicLink(),'Redirected path: '+q);
  assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=49152);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_game_rotation_math_imports.plan.json');
 assert(regular(planPath).size<=16384);const planBytes=fs.readFileSync(planPath);
 assert.equal(hash(planBytes),reviewedPlanSha256);assert.deepEqual(JSON.parse(planBytes),expectedPlan);
 const out=local('docs/reverse-client/r1-game-rotation-math-imports');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:expectedPlan.source_pins,code_bytes:0,code_operations:0,metadata_bytes:0,metadata_operations:0,
  new_PE_header_bytes:0,new_descriptor_bytes:0,new_OFT_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,imported_functions_executed:false,crt_files_opened:false,api_set_resolved:false,
  backend_started:false,subprocesses:0,whole_table_scanned:false,files:[],thunks:[],bindings:[],
  disk_bindings_complete:false,unsupported:[],failures:[],limitations:[
   'Fixed18B FF25 candidates plus at most121B import metadata. No original or imported function is executed.',
   'The API-set DLL contract name is not the actual host CRT DLL. Host resolution/version/function implementation and floating-point behavior remain unverified.',
   'Disk import relationships do not prove loaded IATs, callable ABI, numeric accuracy, exception/errno behavior, rounding modes, signed-zero/NaN payload behavior or native runtime compatibility.',
   'Whole-file hash streams are identity checks separate from scoped payload budgets, not atomic snapshots.',
   'Node path checks cannot identify every Windows reparse tag; separate Attributes preflight remains required.'
  ]};
 const data=new Map();
 function unsupported(reason){const e=new Error(reason);e.fixedUnsupported=true;throw e;}
 const def=expectedPlan.source,f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};
 let fd,initial,pe;
 function digest(){const h=createHash('sha256'),b=Buffer.alloc(expectedPlan.limits.hash_chunk_bytes);for(let at=0;at<def.bytes;){
  const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
 }return h.digest('hex');}
 function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
  const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
  const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};
 }
 function read(rva,n,kind,thunk=false){
  const {section,offset}=map(rva,n);assert(section.characteristics&0x40000000,'Unreadable PE section');
  if(thunk){
   assert.equal(n,6);assert(expectedPlan.fixed_thunks.some(t=>Number(t.rva)===rva));
   assert(section.characteristics&0x20000000,'Thunk is not executable disk data');
   assert(++report.code_operations<=3);assert(report.code_bytes+n<=18);report.code_bytes+=n;
  }else{
   assert(!(section.characteristics&0x20000000),'Metadata unexpectedly maps into executable section');
   assert(n<=31);assert(report.metadata_bytes+n<=121);assert(++report.metadata_operations<=14);report.metadata_bytes+=n;
  }
  const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);
  f.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:b.toString('hex')});return b;
 }
 try{
  for(const pin of expectedPlan.source_pins){
   const p=local(pin.path);assert(regular(p).size<1048576);const bytes=fs.readFileSync(p);assert.equal(hash(bytes),pin.sha256);
   data.set(pin.key,pin.key==='inventory_source'?bytes.toString('utf8'):JSON.parse(bytes));
  }
  for(const t of expectedPlan.fixed_thunks){
   const asm=data.get(t.source_key).result.structuredContent.normalized_result;
   assert.equal(typeof asm,'string');assert(asm.split('\n').includes(t.instruction));
  }
  const gp=data.get('game_pe');assert.equal(gp.target,def.path);assert(gp.source_hash_preserved&&gp.source_stat_stable&&gp.source_handle_closed);
  assert.equal(gp.sha256_before,def.sha256);assert.equal(gp.sha256_after,def.sha256);pe=gp.pe;assert.equal(pe.image_base,'0x180000000');
  assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections)for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);
  const prior=data.get('imports');assert.equal(prior.original_sha256_before,def.sha256);assert.equal(prior.original_sha256_after,def.sha256);
  assert(prior.original_stat_stable&&prior.original_hash_matches);
  assert.equal(prior.headers.import_directory_rva,'0xa42940');assert.equal(prior.headers.import_directory_size,380);
  assert.equal(Number(expectedPlan.descriptor.rva),Number(prior.headers.import_directory_rva)+13*20);
  assert(14*20<=prior.headers.import_directory_size);
  const descLogs=prior.metadata_reads.filter(r=>r.category==='normal-import-descriptor');
  assert(descLogs.length>13);for(let n=0;n<=13;n++){assert.equal(descLogs[n].bytes,20);assert.equal(descLogs[n].file_offset,10751808+n*20);}
  const cached=prior.normal_imports[0];assert.equal(cached.descriptor_index,3);
  assert.equal(cached.raw_metadata.descriptor.rva,'0xa4297c');assert.equal(cached.raw_metadata.descriptor.file_offset,10751868);
  assert.equal(cached.raw_metadata.descriptor.bytes,'c02aa4000000000000000000a66aa50000906000');
  assert.equal(Number(cached.raw_metadata.descriptor.rva)-3*20,0xa42940);
  assert.equal(map(0xa42940,20).offset,descLogs[0].file_offset);
  assert.equal(map(Number(expectedPlan.descriptor.rva),20).offset,descLogs[13].file_offset);
  const inv=data.get('inventory').filter(r=>r.relativePath==='x64/Game.dll');assert.equal(inv.length,1);
  assert.equal(inv[0].sha256,def.sha256);assert.equal(inv[0].bytes,def.bytes);
  assert.equal(inv[0].importModules[3].module,'Engine.dll');
  const module=inv[0].importModules[13];assert.equal(module.module,expectedPlan.descriptor.dll);
  assert.deepEqual(module.symbols,expectedPlan.descriptor.symbols);
  const invSource=data.get('inventory_source');
  assert(invSource.includes("oft,_,_,name,ft=struct.unpack_from('<IIIII',b,p+20*i)"));
  assert(invSource.includes("if val==0:break"));
  assert(invSource.includes("imports.append({'module':string(name),'symbols':symbols})"));
  report.proofs_verified=true;report.files.push(f);
  const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);
  initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);
  f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
  const selected=[];
  for(const t of expectedPlan.fixed_thunks){
   const rva=Number(t.rva),bytes=read(rva,6,'fixed-FF25-thunk-candidate',true);
   const observation={rva:t.rva,bytes:6,raw_hex:bytes.toString('hex'),source_key:t.source_key,instruction:t.instruction,opcode_verified:false,iat_rva:null};
   report.thunks.push(observation);
   if(bytes[0]!==0xff||bytes[1]!==0x25)unsupported('Fixed candidate '+t.rva+' is not FF25disp32; no alternative decoding');
   observation.opcode_verified=true;const displacement=bytes.readInt32LE(2),iat=rva+6+displacement;
   observation.displacement=displacement;observation.iat_rva=hex(iat);selected.push({t,iat});
  }
  assert.equal(new Set(selected.map(s=>s.iat)).size,3);
  const descriptor=read(Number(expectedPlan.descriptor.rva),20,'fixed-math-import-descriptor');report.new_descriptor_bytes=20;
  const oft=descriptor.readUInt32LE(0),name=descriptor.readUInt32LE(12),ft=descriptor.readUInt32LE(16);
  if(oft===0||ft===0||name===0)unsupported('Fixed descriptor lacks ordinary OFT/FT/name; no fallback');
  const dll=Buffer.from(expectedPlan.descriptor.dll+'\0','ascii');
  if(!read(name,dll.length,'fixed-exact-DLL-name').equals(dll))unsupported('Fixed descriptor13 DLL mismatch; no scan');
  report.descriptor={index:13,rva:expectedPlan.descriptor.rva,original_first_thunk_rva:hex(oft),first_thunk_rva:hex(ft),name_rva:hex(name),dll:expectedPlan.descriptor.dll};
  for(const {t,iat} of selected){
   // The pinned same-image inventory stops at the first null OFT and retains
   // original descriptor/symbol order. Its fifteen entries establish this
   // fixed ownership extent; no new prefix or whole-table read is performed.
   assert(Number.isInteger(t.index)&&t.index>=0&&t.index<module.symbols.length);
   assert.equal(module.symbols[t.index],t.symbol);
   if(iat!==ft+t.index*8)unsupported('Thunk '+t.rva+' does not select its exact cached math symbol index; no alternate binding');
   const lookupRva=oft+t.index*8,lookup=read(lookupRva,8,'fixed-selected-OFT').readBigUInt64LE();report.new_OFT_bytes+=8;
   if(lookup===0n||lookup>=0x100000000n)unsupported('Selected OFT is null, ordinal or unrepresentable');
   const diskIat=read(iat,8,'fixed-selected-IAT').readBigUInt64LE();
   if(diskIat!==lookup)unsupported('Selected disk IAT differs from OFT; no loaded-value inference');
   const hintName=Number(lookup),hint=read(hintName,2,'fixed-import-hint').readUInt16LE();
   if(!read(hintName+2,t.symbol.length+1,'fixed-exact-import-name').equals(Buffer.from(t.symbol+'\0','ascii')))unsupported('Exact function name mismatch');
   report.bindings.push({thunk_rva:t.rva,dll:expectedPlan.descriptor.dll,descriptor_index:13,index:t.index,
    symbol:t.symbol,hint,iat_rva:hex(iat),lookup_rva:hex(lookupRva),hint_name_rva:hex(hintName),
    classification:'ExactOrdinaryApiSetNamedImport',loaded_iat_read:false,host_dll_resolved:false,target_body_read:false,numeric_contract_verified:false});
  }
  assert.equal(report.code_bytes,18);assert.equal(report.code_operations,3);
  assert.equal(report.metadata_bytes,121);assert.equal(report.metadata_operations,14);assert.equal(report.new_OFT_bytes,24);
  assert.equal(report.bindings.length,3);report.disk_bindings_complete=true;
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 finally{
  if(fd!==undefined){
   try{
    f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);
    sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;
   }catch(e){report.failures.push('Post-read identity verification: '+String(e));}
   finally{try{fs.closeSync(fd);f.closed=true;}catch(e){report.failures.push('Close original: '+String(e));}}
  }
 }
 report.finished_utc=new Date().toISOString();
 report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.disk_bindings_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('No complete binding and no explicit Unsupported reason');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<131072);normal(out);
 fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed rotation math import binding failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:expectedPlan.output,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,
  thunks:report.thunks,bindings:report.bindings,unsupported:report.unsupported,
  original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
