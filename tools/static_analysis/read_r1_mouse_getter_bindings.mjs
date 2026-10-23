// Preparation only: importing this module performs no I/O. No code-byte reads.
// Execute only after root review and separate approval with both frozen hashes.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectMouseGetterBindings(reviewedScriptSha256, reviewedPlanSha256) {
 const self=fileURLToPath(import.meta.url), root=path.resolve(path.dirname(self),'../..');
 const hash=b=>createHash('sha256').update(b).digest('hex'), hex=n=>'0x'+n.toString(16);
 assert.equal(process.platform,'win32');
 assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_mouse_getter_bindings.mjs');
 const expectedPlan={
  schema:'grim-dynasty.fixed-mouse-getter-bindings.v1',
  script:'tools/static_analysis/read_r1_mouse_getter_bindings.mjs',
  output:'docs/reverse-client/r1-mouse-getter-bindings-retry1/inspection.json',
  sources:[
   {path:'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',bytes:11385856,sha256:'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2'},
   {path:'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',bytes:4485632,sha256:'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'}
  ],
  imports:[
   {iat_rva:'0x609be8',index:381,symbol:'?GetCoordsUnparented@Entity@GAME@@QEBAAEBVWorldCoords@2@XZ',caller:'0x180163547: CALL qword ptr [0x180609be8]'},
   {iat_rva:'0x609ad8',index:347,symbol:'?GetOptions@Engine@GAME@@QEAAPEAVOptions@2@XZ',caller:'0x1803ce8e8: CALL qword ptr [0x180609ad8]'},
   {iat_rva:'0x60a148',index:553,symbol:'?GetInt@Options@GAME@@QEBAHW4IntName@12@@Z',caller:'0x1803ce8f6: CALL qword ptr [0x18060a148]'}
  ],
  source_pins:[
   {key:'imports',path:'docs/object-lifecycle/offline-questitem-imports/import-bindings.json',sha256:'94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'},
   {key:'game_pe',path:'docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json',sha256:'227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'},
   {key:'engine_pe',path:'docs/object-lifecycle/offline-class-registration-exports/discovery.json',sha256:'0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'},
   {key:'inventory',path:'docs/phase-1/boundary-audit/module-boundaries.json',sha256:'d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'},
   {key:'dispatch',path:'docs/reverse-client/r1-player-move-dispatch/04-MoveDispatch-0x180163490-procedure_assembly.json',sha256:'083eade1979ad63423a26e516f3681e1e3b2dc213586d2d6e747f85eddea9e1a'},
   {key:'move',path:'docs/reverse-client/r1-movement-action-path/05-MoveAction-0x1803ce890-procedure_assembly.json',sha256:'e93a0992a69b1a04b9a552163943fe49ad3a4e2a3c5cf5bcc2e5f6ac00fdffd1'}
  ],
  limits:{metadata_bytes:512,metadata_operations:24,single_read_bytes:128,hash_chunk_bytes:65536,code_bytes:0,subprocesses:0},
  requirements:[
   'Separate exact approval with script and plan hashes; separate Windows Attributes/reparse preflight for originals, script, plan, proof files and output ancestors.',
   'Only three fixed IAT/OFT slots, exact import hint/name and exact hinted Engine export metadata; no scan or hint fallback, no code, PE headers, pdata or runtime read.',
   'Index 553 is outside the cached 350-entry nonzero prefix. Its evidence is the pinned full import-name inventory, cached descriptor and current exact OFT/IAT/name; no unseen contiguous prefix is asserted.',
   'Read-only originals with same-handle before/after SHA256 and stat/path checks. Hash streaming is separate from the metadata budget and is not an atomic snapshot.',
   'Output must not exist; preserve partial failure evidence. No original loading/execution, backend, subprocess, network, save data or security-setting changes.'
  ]
 };
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  const s=fs.lstatSync(q);assert(!s.isSymbolicLink(),'Redirected path: '+q);
  assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
  if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 function checkedJson(p,expected,max){assert(regular(p).size<=max);const b=fs.readFileSync(p);assert.equal(hash(b),expected);return JSON.parse(b);}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=24576);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const plan=checkedJson(local('tools/static_analysis/read_r1_mouse_getter_bindings.plan.json'),reviewedPlanSha256,12288);
 assert.deepEqual(plan,expectedPlan);
 const out=path.dirname(local(plan.output));normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing evidence preserved: '+out);}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:plan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source_pins:plan.source_pins,selected:plan.imports,limits:plan.limits,metadata_bytes:0,metadata_operations:0,code_bytes:0,subprocesses:0,
  original_loaded_or_executed:false,whole_export_table_scanned:false,new_PE_headers_read:false,files:[],bindings:[],exports:[],failures:[],
  cached_contiguous_nonzero_prefix_entries:350,extended_contiguous_prefix_claimed:false,limitations:plan.requirements};
 const proofs=new Map();
 function inspect(def,pe,body){
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);
  let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(plan.limits.hash_chunk_bytes);for(let at=0;at<def.bytes;){
   const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;
  }return h.digest('hex');}
  function map(rva,n){
   assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
   const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(offset>=0&&offset+n<=def.bytes);return {section,offset};
  }
  function read(rva,n,kind){
   assert(n<=plan.limits.single_read_bytes);assert(report.metadata_bytes+n<=plan.limits.metadata_bytes);
   assert(report.metadata_operations<plan.limits.metadata_operations);
   const {section,offset}=map(rva,n);assert(section.characteristics&0x40000000);assert(!(section.characteristics&0x20000000),'Executable-byte read refused');
   report.metadata_bytes+=n;report.metadata_operations++;const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);
   f.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:b.toString('hex')});return b;
  }
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);
   initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(before,initial);f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);body(read,map);
  }finally{if(fd!==undefined){try{
   f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;
  }finally{fs.closeSync(fd);f.closed=true;}}}
 }
 try{
  for(const p of plan.source_pins)proofs.set(p.key,checkedJson(local(p.path),p.sha256,2097152));
  const [game,engine]=plan.sources;
  const im=proofs.get('imports');assert.equal(im.original,game.path);assert.equal(im.original_sha256_before,game.sha256);assert.equal(im.original_sha256_after,game.sha256);assert(im.original_stat_stable&&im.original_hash_matches);
  const cached=im.normal_imports[4];assert.equal(cached.dll,'Engine.dll');assert.equal(cached.descriptor_index,3);
  const raw=cached.raw_metadata,desc=Buffer.from(raw.descriptor.bytes,'hex');assert.equal(desc.length,20);
  assert.equal(Number(im.headers.import_directory_rva),0xa42940);assert.equal(im.headers.import_directory_size,380);
  assert.equal(Number(raw.descriptor.rva),0xa4297c);
  assert.equal(Number(raw.descriptor.rva),Number(im.headers.import_directory_rva)+cached.descriptor_index*20);
  assert((cached.descriptor_index+1)*20<=im.headers.import_directory_size);
  assert.equal(desc.readUInt32LE(0),0xa42ac0);assert.equal(desc.readUInt32LE(12),0xa56aa6);assert.equal(desc.readUInt32LE(16),0x609000);
  assert.equal(Number(cached.original_first_thunk_rva),desc.readUInt32LE(0));assert.equal(Number(cached.first_thunk_rva),desc.readUInt32LE(16));
  assert.equal(Number(raw.dll_name_with_nul.rva),desc.readUInt32LE(12));assert.equal(raw.dll_name_with_nul.text,'Engine.dll');
  assert.equal(raw.dll_name_with_nul.bytes,'456e67696e652e646c6c00');
  const prefix=Buffer.from(raw.ownership_lookup_prefix.bytes,'hex');assert.equal(Number(raw.ownership_lookup_prefix.rva),0xa42ac0);
  assert.equal(raw.ownership_lookup_prefix.entries,350);assert.equal(prefix.length,2800);
  for(let at=0;at<prefix.length;at+=8)assert.notEqual(prefix.readBigUInt64LE(at),0n);
  report.cached_descriptor={descriptor_index:3,evidence_selection_index:4,import_directory_rva:im.headers.import_directory_rva,raw:raw.descriptor};
  const rows=proofs.get('inventory').filter(m=>m.relativePath==='x64/Game.dll');assert.equal(rows.length,1);assert.equal(rows[0].sha256,game.sha256);assert.equal(rows[0].bytes,game.bytes);
  const modules=rows[0].importModules.filter(m=>m.module==='Engine.dll');assert.equal(modules.length,1);assert.equal(modules[0].symbols.length,1183);
  for(const entry of plan.imports){assert.equal((Number(entry.iat_rva)-0x609000)/8,entry.index);assert(entry.index<modules[0].symbols.length);assert.equal(modules[0].symbols[entry.index],entry.symbol);
   const caller=proofs.get(entry.index===381?'dispatch':'move').result.structuredContent.normalized_result;assert(caller.split('\n').includes(entry.caller));}
  const gp=proofs.get('game_pe');assert.equal(gp.target,game.path);assert.equal(gp.sha256_before,game.sha256);assert.equal(gp.sha256_after,game.sha256);
  assert(gp.source_hash_preserved&&gp.source_stat_stable&&gp.source_handle_closed);assert.equal(gp.pe.image_base,'0x180000000');
  const eps=proofs.get('engine_pe').files.filter(f=>f.target===engine.path);assert.equal(eps.length,1);const ep=eps[0];
  assert(ep.source_hash_preserved&&ep.source_stat_stable);assert.equal(ep.sha256_before,engine.sha256);assert.equal(ep.sha256_after,engine.sha256);
  const epe=ep.pe,ed=Buffer.from(epe.export_directory_bytes,'hex');assert.equal(ed.length,40);
  for(const [off,val] of [[16,epe.ordinal_base],[20,epe.function_count],[24,epe.named_export_count],[28,Number(epe.address_table_rva)],[32,Number(epe.name_pointer_table_rva)],[36,Number(epe.name_ordinal_table_rva)]])assert.equal(ed.readUInt32LE(off),val);
  report.proofs_verified=true;
  inspect(game,gp.pe,(read)=>{
   for(const entry of plan.imports){
    const oftRva=0xa42ac0+entry.index*8,lookup=read(oftRva,8,'exact-OFT-slot').readBigUInt64LE(),iat=read(Number(entry.iat_rva),8,'exact-IAT-slot').readBigUInt64LE();
    assert(lookup>0n&&lookup<0x100000000n,'Ordinal or out-of-range hint/name is unsupported');assert.equal(iat,lookup);
    if(entry.index<350)assert.equal(lookup,prefix.readBigUInt64LE(entry.index*8));
    const nameRva=Number(lookup),hint=read(nameRva,2,'exact-import-hint').readUInt16LE(),name=Buffer.from(entry.symbol+'\0','ascii');
    assert(read(nameRva+2,name.length,'exact-import-name').equals(name));
    report.bindings.push({dll:'Engine.dll',iat_rva:entry.iat_rva,lookup_index:entry.index,lookup_rva:hex(oftRva),hint_name_rva:hex(nameRva),hint,symbol:entry.symbol,
     within_cached_nonzero_prefix:entry.index<350,loaded_binding_observed:false});
   }
  });
  inspect(engine,epe,(read,map)=>{
   for(const b of report.bindings){
    assert(b.hint<epe.named_export_count);const nameRva=read(Number(epe.name_pointer_table_rva)+b.hint*4,4,'exact-hinted-name-pointer').readUInt32LE();
    const expectedName=Buffer.from(b.symbol+'\0','ascii');assert(read(nameRva,expectedName.length,'exact-export-name').equals(expectedName),'Hint/name mismatch: no fallback');
    const ordinalIndex=read(Number(epe.name_ordinal_table_rva)+b.hint*2,2,'exact-name-ordinal').readUInt16LE();assert(ordinalIndex<epe.function_count);
    const target=read(Number(epe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();assert(target>0);
    assert(!(target>=Number(epe.export_directory_rva)&&target<Number(epe.export_directory_rva)+epe.export_directory_size),'Forwarder unsupported');
    assert(map(target,1).section.characteristics&0x20000000,'Export lacks executable-section classification');
    report.exports.push({iat_rva:b.iat_rva,symbol:b.symbol,hint:b.hint,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+epe.ordinal_base,
     target_rva:hex(target),target_va:hex(0x180000000+target),code_read:false,function_boundary_verified:false,loaded_binding_observed:false});
   }
  });
  assert.equal(report.bindings.length,3);assert.equal(report.exports.length,3);assert.equal(report.metadata_operations,24);
 }catch(e){report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';normal(out);
 const bytes=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(bytes)<=65536);fs.writeFileSync(local(plan.output),bytes,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed mouse getter binding failed; evidence preserved: '+report.failures.join('; '));
 return {status:report.status,output:plan.output,metadata_bytes:report.metadata_bytes,exports:report.exports,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
