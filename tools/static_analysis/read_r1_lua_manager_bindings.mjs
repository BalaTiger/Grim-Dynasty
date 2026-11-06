// Preparation only: importing this module performs no I/O or original access.
// Fixed Game GetState import and Engine Initialize export metadata. Parent
// reviews and separately approves inspectLuaManagerBindings(self SHA256).
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectLuaManagerBindings(reviewedScriptSha256) {
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
 assert.equal(path.basename(self),'read_r1_lua_manager_bindings.mjs');
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);
 // Reused normal-path guard. External Get-Item Attributes preflight remains
 // required because Node cannot identify every Windows reparse tag.
 function normal(p,file=false){let first=true;for(let q=path.resolve(p);;q=path.dirname(q)){
  const s=fs.lstatSync(q);assert.ok(!s.isSymbolicLink(),'Links/junctions refused');
  assert.ok(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(q)).toLowerCase(),q.toLowerCase());
  if(path.dirname(q)===q)break;first=false;
 }}
 function project(rel,expected){const p=path.resolve(root,rel);assert.ok(p.startsWith(root+path.sep));normal(p,true);assert.ok(fs.statSync(p).size<1048576);const b=fs.readFileSync(p);assert.equal(sha(b),expected);return JSON.parse(b);}
 normal(self,true);assert.equal(sha(fs.readFileSync(self)),reviewedScriptSha256);
 const planRel='docs/reverse-client/r1-lua-manager-bindings-plan/plan.json';
 const planHash='5d881b6d40747917963443c950eb3aba734adeb78185f25dc6f51d326963f692';
 const plan=project(planRel,planHash),data=plan.proofs.map(p=>project(p.path,p.sha256));
 const gameHash='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',engineHash='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
 const getState='?GetState@LuaManager@GAME@@QEAAAEAVLuaState@LUAGLUE@@XZ';
 const initialize='?Initialize@LuaManager@GAME@@QEAA_N_N0@Z';
 const limits={metadata_bytes:12288,metadata_operations:512,binary_search_steps:13,distinct_export_names:14,ascii_bytes_per_name:512,ascii_chunk_bytes:32,hash_chunk_bytes:65536,report_bytes:262144};
 assert.equal(plan.schema,'grim-dynasty-fixed-lua-manager-bindings-plan-v1');
 assert.deepEqual(plan.symbols,[getState,initialize]);assert.deepEqual(plan.limits,limits);
 assert.deepEqual(plan.targets,[{path:'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',sha256:gameHash,bytes:11385856},{path:'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',sha256:engineHash,bytes:4485632}]);
 assert.equal(plan.output,'docs/reverse-client/r1-lua-manager-bindings/inspection.json');
 assert.equal(plan.automatic_expansion,false);assert.equal(plan.exe_reads,0);assert.equal(plan.executable_reads,0);assert.equal(plan.pdata_reads,0);
 const inventory=data[4].filter(m=>m.relativePath==='x64/Game.dll');assert.equal(inventory.length,1);assert.equal(inventory[0].sha256,gameHash);assert.equal(inventory[0].bytes,11385856);
 const modules=inventory[0].importModules.filter(m=>m.module==='Engine.dll');assert.equal(modules.length,1);assert.equal(modules[0].symbols[823],getState);
 assert.equal(data[5].selectedImports.filter(s=>s.module==='Engine.dll'&&s.symbol===initialize).length,1);
 const caller=data[6][0];assert.equal(caller.start,'1802f3450');assert.equal(caller.bytesBudget,101);
 assert.ok(caller.instructions.includes('1802f346c MOV RCX,qword ptr [RCX + 0x68]'));
 assert.ok(caller.instructions.includes('1802f3470 CALL qword ptr [0x18060a9b8]'));
 assert.equal(caller.outgoingReferences.filter(r=>r.from==='1802f3470'&&r.to==='18060a9b8'&&r.symbol==='PTR_'+getState+'_18060a9b8').length,1);
 const cached=data[0].normal_imports[4].raw_metadata,desc=Buffer.from(cached.descriptor.bytes,'hex'),prefix=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');
 assert.equal(desc.length,20);assert.equal(desc.readUInt32LE(0),0xa42ac0);assert.equal(desc.readUInt32LE(12),0xa56aa6);assert.equal(desc.readUInt32LE(16),0x609000);
 assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(Number(cached.ownership_lookup_prefix.rva),0xa42ac0);assert.equal(cached.ownership_lookup_prefix.entries,350);assert.equal(prefix.length,2800);
 for(let at=0;at<prefix.length;at+=8)assert.notEqual(prefix.readBigUInt64LE(at),0n);
 assert.equal(data[3].status,'passed');const tailFile=data[3].files.find(f=>f.source===plan.targets[0].path);
 assert.ok(tailFile.stable&&tailFile.closed);assert.equal(tailFile.sha256_before,gameHash);assert.equal(tailFile.sha256_after,gameHash);
 const tails=tailFile.reads.filter(r=>r.kind==='OFT-nonzero-continuation-through-selected-slot');assert.equal(tails.length,1);assert.equal(tails[0].rva,'0xa435b0');assert.equal(tails[0].bytes,1104);
 const tail=Buffer.from(tails[0].raw_hex,'hex');assert.equal(tail.length,1104);for(let at=0;at<tail.length;at+=8)assert.notEqual(tail.readBigUInt64LE(at),0n);
 assert.equal((0x60a9b8-0x609000)/8,823);
 assert.equal(data[1].sha256_after,gameHash);const gamePe=data[1].pe;assert.equal(gamePe.image_base,'0x180000000');
 const engineProof=data[2].files.find(f=>f.target===plan.targets[1].path);assert.ok(engineProof.source_hash_preserved&&engineProof.source_stat_stable);assert.equal(engineProof.sha256_before,engineHash);assert.equal(engineProof.sha256_after,engineHash);
 const enginePe=engineProof.pe,rawExport=Buffer.from(enginePe.export_directory_bytes,'hex');assert.equal(rawExport.length,40);
 for(const [off,value] of [[16,enginePe.ordinal_base],[20,enginePe.function_count],[24,enginePe.named_export_count],[28,Number(enginePe.address_table_rva)],[32,Number(enginePe.name_pointer_table_rva)],[36,Number(enginePe.name_ordinal_table_rva)]])assert.equal(rawExport.readUInt32LE(off),value);
 assert.equal(enginePe.named_export_count,6283);assert.equal(enginePe.function_count,6283);
 const outputRel='docs/reverse-client/r1-lua-manager-bindings',out=path.resolve(root,outputRel);assert.ok(out.startsWith(root+path.sep));normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing evidence preserved: '+out);}catch(e){if(e.code!=='ENOENT')throw e;}
 const report={schema:'grim-dynasty-fixed-lua-manager-bindings-v1',started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan:{path:planRel,sha256:planHash},proofs:plan.proofs,symbols:[getState,initialize],limits,metadata_bytes:0,metadata_operations:0,code_bytes:0,pdata_bytes:0,exe_bytes:0,subprocesses:0,original_loaded_or_executed:false,whole_export_table_scanned:false,files:[],bindings:[],exports:[],searches:[],encountered_names:[],failures:[],cached_descriptor:{index:4,raw:cached.descriptor,nonzero_prefix_entries:488},limitations:[
  'Only Game OFT488..823, exact IAT60A9B8/import name, its hinted Engine export, and one exact Initialize export binary search. No table sweep, code, pdata, EXE or fallback.',
  'Sorted ASCII export names are assumed; only encountered ordering is checked. A missing exact name remains a failed bounded lookup, not absence of unexported functionality.',
  'Disk bindings only. No loaded IAT/global/service state, Lua lifecycle, ABI or function behavior is established.',
  'Before/after same-descriptor SHA256 streams original files in64KiB buffers without interpreting code, separate from semantic metadata budget. Hashes are not atomic snapshots.',
  'Node guards do not identify every Windows reparse tag; the existing external Attributes preflight remains required.'
 ]};
 function inspect(source,expected,size,pe,body){
  normal(source,true);const f={source,expected_sha256:expected,reads:[],closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<size;){const n=fs.readSync(fd,b,0,Math.min(b.length,size-at),at);assert.ok(n>0);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
  function map(rva,n){assert.ok(Number.isSafeInteger(rva)&&Number.isSafeInteger(n)&&rva>=0&&n>0);const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);const s=hits[0],offset=s.raw_offset+rva-s.rva;assert.ok(offset>=0&&offset+n<=size);return {s,offset};}
  function read(rva,n,kind){const {s,offset}=map(rva,n);assert.ok((s.characteristics&0x40000000)!==0&&(s.characteristics&0x20000000)===0,'Metadata must be readable and non-executable');report.metadata_bytes+=n;assert.ok(report.metadata_bytes<=limits.metadata_bytes);assert.ok(++report.metadata_operations<=limits.metadata_operations);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);f.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:b.toString('hex')});return b;}
  try{fd=fs.openSync(source,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.ok(initial.isFile());assert.equal(initial.size,size);f.sha256_before=digest();assert.equal(f.sha256_before,expected);body(read,map);}
  finally{if(fd!==undefined){try{f.sha256_after=digest();assert.equal(f.sha256_after,expected);normal(source,true);for(const state of [fs.fstatSync(fd),fs.statSync(source)]){assert.ok(state.isFile());for(const key of ['size','ino','dev','mtimeMs','ctimeMs'])assert.equal(state[key],initial[key]);}f.stable=true;}finally{fs.closeSync(fd);f.closed=true;}}}
 }
 fs.mkdirSync(out);normal(out);
 try{
  inspect(plan.targets[0].path,gameHash,11385856,gamePe,read=>{
   const continuation=read(0xa42ac0+488*8,(823-488+1)*8,'OFT-nonzero-continuation-488-through823');
   for(let offset=0;offset<continuation.length;offset+=8)assert.notEqual(continuation.readBigUInt64LE(offset),0n,'Engine descriptor ended before selected index823');
   const lookupRva=0xa42ac0+823*8,lookup=continuation.readBigUInt64LE((823-488)*8);
   assert.ok(lookup>0n&&lookup<0x100000000n,'Selected import must be hint/name RVA');
   assert.equal(read(0x60a9b8,8,'exact-IAT-slot').readBigUInt64LE(),lookup);
   const nameRva=Number(lookup),hint=read(nameRva,2,'exact-import-hint').readUInt16LE(),expectedName=Buffer.from(getState+'\0','ascii');
   assert.ok(read(nameRva+2,expectedName.length,'exact-import-name').equals(expectedName),'Fixed import mismatch: no fallback');
   report.bindings.push({dll:'Engine.dll',iat_rva:'0x60a9b8',lookup_index:823,lookup_rva:hex(lookupRva),hint_name_rva:hex(nameRva),hint,symbol:getState});
  });
  inspect(plan.targets[1].path,engineHash,4485632,enginePe,(read,map)=>{
   const exportRva=Number(enginePe.export_directory_rva),exportEnd=exportRva+enginePe.export_directory_size,names=new Map();
   function metadata(rva,n,kind){assert.ok(rva>=exportRva&&rva+n<=exportEnd,'Read outside export metadata');return read(rva,n,kind);}
   function remember(index,nameRva,name){assert.ok(names.size<limits.distinct_export_names);for(const [other,item] of names)assert.ok(other<index?item.name<name:item.name>name,'Encountered export order differs');const item={index,name_rva:hex(nameRva),name};names.set(index,item);report.encountered_names.push(item);return item;}
   function nameAt(index,exact){assert.ok(Number.isInteger(index)&&index>=0&&index<enginePe.named_export_count);if(names.has(index))return names.get(index);
    const rva=metadata(Number(enginePe.name_pointer_table_rva)+index*4,4,'selected-export-name-pointer').readUInt32LE();
    if(exact){const wanted=Buffer.from(exact+'\0','ascii');assert.ok(metadata(rva,wanted.length,'exact-hinted-export-name').equals(wanted),'Hint mismatch: no fallback');return remember(index,rva,exact);}
    const pieces=[];let used=0;
    while(used<limits.ascii_bytes_per_name){const n=Math.min(limits.ascii_chunk_bytes,limits.ascii_bytes_per_name-used,exportEnd-rva-used,map(rva+used,1).s.rva+map(rva+used,1).s.raw_size-rva-used);assert.ok(n>0);const b=metadata(rva+used,n,'bounded-binary-export-name'),nul=b.indexOf(0),piece=nul<0?b:b.subarray(0,nul);for(const ch of piece)assert.ok(ch>=0x20&&ch<0x7f,'Printable ASCII required');pieces.push(piece);used+=n;if(nul>=0)return remember(index,rva,Buffer.concat(pieces).toString('ascii'));}
    throw Error('Export name exceeds512-byte bound');
   }
   function emit(item,symbol,iat){assert.equal(item.name,symbol);const ordinalIndex=metadata(Number(enginePe.name_ordinal_table_rva)+item.index*2,2,'exact-name-ordinal').readUInt16LE();assert.ok(ordinalIndex<enginePe.function_count);const target=metadata(Number(enginePe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();assert.ok(target!==0);assert.ok(!(target>=exportRva&&target<exportEnd),'Forwarder unsupported');assert.ok((map(target,1).s.characteristics&0x20000000)!==0);report.exports.push({iat_rva:iat,symbol,name_index:item.index,name_rva:item.name_rva,ordinal_index:ordinalIndex,ordinal:ordinalIndex+enginePe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target),code_read:false});}
   const binding=report.bindings[0];emit(nameAt(binding.hint,getState),getState,binding.iat_rva);
   const search={symbol:initialize,method:'exact-name binary search',steps:0,probes:[],matched:false};report.searches.push(search);let lo=0,hi=enginePe.named_export_count-1,found=null;
   while(lo<=hi){assert.ok(++search.steps<=limits.binary_search_steps);const mid=lo+Math.floor((hi-lo)/2),item=nameAt(mid);search.probes.push({index:mid,name:item.name});if(item.name===initialize){found=item;break;}if(item.name<initialize)lo=mid+1;else hi=mid-1;}
   assert.ok(found,'Exact Initialize export not found under bounded sorted-table contract');search.matched=true;emit(found,initialize,null);
  });
 }catch(e){report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';normal(out);const serialized=JSON.stringify(report,null,2)+'\n';assert.ok(Buffer.byteLength(serialized)<limits.report_bytes);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed LuaManager bindings failed: '+report.failures.join('; '));
 return {status:report.status,output:outputRel,metadata_bytes:report.metadata_bytes,bindings:report.bindings,exports:report.exports,searches:report.searches,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
