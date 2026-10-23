// Preparation only: importing this module performs no I/O or original access.
// After review and separate approval call inspectAddObjectBinding with the
// reviewed SHA256 of THIS file. Fixed metadata only; no code read or fallback.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectAddObjectBinding(reviewedScriptSha256) {
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 const sha=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
 assert.equal(path.basename(self),'read_r1_add_object_binding.mjs');
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);
 // Same normal-path guard used by the prior fixed readers. Node cannot identify
 // every Windows reparse tag; external Get-Item Attributes preflight is required.
 function normal(p,file=false){let first=true;for(let q=path.resolve(p);;q=path.dirname(q)){
  const s=fs.lstatSync(q);assert.ok(!s.isSymbolicLink(),'Links/junctions refused');
  assert.ok(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(q)).toLowerCase(),q.toLowerCase());
  if(path.dirname(q)===q)break;first=false;
 }}
 normal(self,true);assert.equal(sha(fs.readFileSync(self)),reviewedScriptSha256);
 const proofs=[
  ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
  ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json','227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'],
  ['docs/object-lifecycle/offline-class-registration-exports/discovery.json','0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
  ['docs/reverse-client/r1-navigation-binding/inspection.json','d0c171ce77a6d67e153a6911fc7c3a17ea4d61e80ab9492f3287384524e14264'],
  ['docs/phase-1/boundary-audit/module-boundaries.json','d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'],
 ];
 const data=proofs.map(([rel,expected])=>{const p=path.resolve(root,rel);assert.ok(p.startsWith(root+path.sep));normal(p,true);assert.ok(fs.statSync(p).size<1048576);const b=fs.readFileSync(p);assert.equal(sha(b),expected);return JSON.parse(b);});
 const gameHash='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',engineHash='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
 const selected=[
  {iat:0x609f58,index:491,nameRva:null,symbol:'?AddObject@NavManager@GAME@@QEAAXPEAVEntity@2@@Z'}
 ];
 const gameInventory=data[4].filter(m=>m.relativePath==='x64/Game.dll');assert.equal(gameInventory.length,1);assert.equal(gameInventory[0].sha256,gameHash);assert.equal(gameInventory[0].bytes,11385856);
 const inventoryImports=gameInventory[0].importModules.filter(m=>m.module==='Engine.dll');assert.equal(inventoryImports.length,1);
 for(const s of selected)assert.equal(inventoryImports[0].symbols[s.index],s.symbol);
 const cached=data[0].normal_imports[4].raw_metadata,desc=Buffer.from(cached.descriptor.bytes,'hex'),prefix=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');
 assert.equal(desc.length,20);assert.equal(desc.readUInt32LE(0),0xa42ac0);assert.equal(desc.readUInt32LE(12),0xa56aa6);assert.equal(desc.readUInt32LE(16),0x609000);
 assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(Number(cached.ownership_lookup_prefix.rva),0xa42ac0);assert.equal(cached.ownership_lookup_prefix.entries,350);assert.equal(prefix.length,2800);
 for(let at=0;at<prefix.length;at+=8)assert.notEqual(prefix.readBigUInt64LE(at),0n);
 const tailSource=data[3];assert.equal(tailSource.status,'passed');
 const gameTail=tailSource.files.find(f=>f.source==='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll');assert.ok(gameTail.stable&&gameTail.closed);assert.equal(gameTail.sha256_before,gameHash);assert.equal(gameTail.sha256_after,gameHash);
 const tails=gameTail.reads.filter(r=>r.kind==='OFT-nonzero-continuation-through-selected-slot');assert.equal(tails.length,1);assert.equal(tails[0].rva,'0xa435b0');assert.equal(tails[0].bytes,1104);
 const tail=Buffer.from(tails[0].raw_hex,'hex');assert.equal(tail.length,1104);for(let at=0;at<tail.length;at+=8)assert.notEqual(tail.readBigUInt64LE(at),0n);
 // Cached nonzero OFT entries cover0..487 only. Read488..491 below; never infer the new hint/name RVA from the symbol inventory.
 assert.equal((selected[0].iat-0x609000)/8,491);
 assert.equal(data[1].sha256_after,gameHash);const gamePe=data[1].pe;assert.equal(gamePe.image_base,'0x180000000');
 const engineProof=data[2].files.find(f=>f.target==='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll');assert.ok(engineProof.source_hash_preserved&&engineProof.source_stat_stable);assert.equal(engineProof.sha256_before,engineHash);assert.equal(engineProof.sha256_after,engineHash);
 const enginePe=engineProof.pe,rawExport=Buffer.from(enginePe.export_directory_bytes,'hex');assert.equal(rawExport.length,40);
 for(const [off,value] of [[16,enginePe.ordinal_base],[20,enginePe.function_count],[24,enginePe.named_export_count],[28,Number(enginePe.address_table_rva)],[32,Number(enginePe.name_pointer_table_rva)],[36,Number(enginePe.name_ordinal_table_rva)]])assert.equal(rawExport.readUInt32LE(off),value);
 const outputRel='docs/reverse-client/r1-add-object-binding',out=path.resolve(root,outputRel);assert.ok(out.startsWith(root+path.sep));normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing evidence preserved: '+out);}catch(e){if(e.code!=='ENOENT')throw e;}
 const report={schema:'grim-dynasty-fixed-add-object-binding-v1',started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,proofs,selected,metadata_bytes:0,metadata_limit:4096,metadata_operations:0,code_bytes:0,subprocesses:0,original_loaded_or_executed:false,whole_export_table_scanned:false,files:[],bindings:[],exports:[],failures:[],cached_descriptor:{index:4,raw:cached.descriptor,nonzero_prefix_entries:488},limitations:[
  'Only Game OFT entries488..491 to prove the fixed491 ownership continuation, IAT609F58, exact AddObject import hint/name and its hinted Engine export metadata. Hint/name mismatch stops; no scan, fallback, code or pdata read.',
  'Disk binding only; loaded IAT mutation and runtime object identity/behavior are unobserved.',
  'Before/after same-descriptor SHA256 streams whole original files in bounded 64 KiB buffers without interpreting code. These bytes are not counted as metadata; hashes are not atomic snapshots.',
  'Node normal-path guards do not identify all Windows reparse tags; the existing external Attributes preflight is still required.'
 ]};
 function inspect(source,expected,size,pe,body){
  normal(source,true);const f={source,expected_sha256:expected,reads:[],closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<size;){const n=fs.readSync(fd,b,0,Math.min(b.length,size-at),at);assert.ok(n>0);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
  function map(rva,n){assert.ok(Number.isSafeInteger(rva)&&Number.isSafeInteger(n)&&rva>=0&&n>0);const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);const s=hits[0],offset=s.raw_offset+rva-s.rva;assert.ok(offset>=0&&offset+n<=size);return {s,offset};}
  function read(rva,n,kind){const {s,offset}=map(rva,n);assert.ok((s.characteristics&0x40000000)!==0&&(s.characteristics&0x20000000)===0,'Metadata must be readable and non-executable');report.metadata_bytes+=n;assert.ok(report.metadata_bytes<=4096);assert.ok(++report.metadata_operations<=4096);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);f.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:b.toString('hex')});return b;}
  try{fd=fs.openSync(source,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.ok(initial.isFile());assert.equal(initial.size,size);f.sha256_before=digest();assert.equal(f.sha256_before,expected);body(read,map);}
  finally{if(fd!==undefined){try{f.sha256_after=digest();assert.equal(f.sha256_after,expected);normal(source,true);for(const state of [fs.fstatSync(fd),fs.statSync(source)]){assert.ok(state.isFile());for(const key of ['size','ino','dev','mtimeMs','ctimeMs'])assert.equal(state[key],initial[key]);}f.stable=true;}finally{fs.closeSync(fd);f.closed=true;}}}
 }
 fs.mkdirSync(out);normal(out);
 try{
  inspect('D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',gameHash,11385856,gamePe,(read)=>{
   const continuationRva=0xa42ac0+488*8;
   const continuation=read(continuationRva,4*8,'OFT-nonzero-continuation-488-through491');
   for(let offset=0;offset<continuation.length;offset+=8)assert.notEqual(continuation.readBigUInt64LE(offset),0n,'Engine descriptor ended before selected index491');
   const s=selected[0],lookupRva=0xa42ac0+s.index*8,lookup=continuation.readBigUInt64LE(3*8);
   assert.ok(lookup>0n&&lookup<0x100000000n,'Selected import must be a hint/name RVA, not ordinal');s.nameRva=Number(lookup);
   const iat=read(s.iat,8,'exact-IAT-slot').readBigUInt64LE();assert.equal(iat,lookup);
   const hint=read(s.nameRva,2,'exact-import-hint').readUInt16LE();
   const expectedName=Buffer.from(s.symbol+'\0','ascii');
   assert.ok(read(s.nameRva+2,expectedName.length,'exact-import-name').equals(expectedName),'Fixed import name mismatch: stop without fallback');
   report.bindings.push({dll:'Engine.dll',iat_rva:hex(s.iat),lookup_index:s.index,lookup_rva:hex(lookupRva),hint_name_rva:hex(s.nameRva),hint,symbol:s.symbol});
  });
  inspect(engineProof.target,engineHash,4485632,enginePe,(read,map)=>{
   for(const binding of report.bindings){const {hint,symbol}=binding;assert.ok(hint<enginePe.named_export_count);const nameRva=read(Number(enginePe.name_pointer_table_rva)+hint*4,4,'exact-hinted-name-pointer').readUInt32LE();const expectedName=Buffer.from(symbol+'\0','ascii');assert.ok(read(nameRva,expectedName.length,'exact-export-name').equals(expectedName),'Hint mismatch: stop without fallback');
    const ordinalIndex=read(Number(enginePe.name_ordinal_table_rva)+hint*2,2,'exact-name-ordinal').readUInt16LE();assert.ok(ordinalIndex<enginePe.function_count);const target=read(Number(enginePe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();assert.ok(target!==0);assert.ok(!(target>=Number(enginePe.export_directory_rva)&&target<Number(enginePe.export_directory_rva)+enginePe.export_directory_size),'Forwarder unsupported');assert.ok((map(target,1).s.characteristics&0x20000000)!==0);
    report.exports.push({iat_rva:binding.iat_rva,symbol,hint,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+enginePe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target),code_read:false});
   }
  });
 }catch(e){report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';normal(out);const serialized=JSON.stringify(report,null,2)+'\n';assert.ok(Buffer.byteLength(serialized)<262144);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed AddObject binding failed: '+report.failures.join('; '));
 return {status:report.status,output:outputRel,metadata_bytes:report.metadata_bytes,bindings:report.bindings,exports:report.exports,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
