// Import is inert. Root reviews/approves this exact function before execution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectMoveProductionImports(reviewedScriptSha256,reviewedPlanSha256){
 const fixedPlanSha256='697129927d073ac7e062d113cd027e0bac6f23ba0afce49ac025110e57082ea8';
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 const hash=value=>createHash('sha256').update(value).digest('hex'),hex=value=>'0x'+value.toString(16);
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
 assert.equal(path.basename(self),'read_r1_move_production_imports.mjs');
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){const s=fs.lstatSync(q);assert(!s.isSymbolicLink(),'Redirected path: '+q);assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert.notEqual(s.ino,0);return s;}
 function sameStat(a,b){for(const key of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[key],b[key],key);}
 function local(relative){const p=path.resolve(root,relative);assert(p.startsWith(root+path.sep),'Path leaves fixed workspace');return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.equal(reviewedPlanSha256,fixedPlanSha256);
 assert(regular(self).size<=32768);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_move_production_imports.plan.json');assert(regular(planPath).size<=16384);
 const planBytes=fs.readFileSync(planPath);assert.equal(hash(planBytes),fixedPlanSha256);const plan=JSON.parse(planBytes);
 assert.equal(plan.schema,'grim-dynasty.fixed-move-production-imports.v1');assert.deepEqual(plan.bindings.map(s=>[s.iat_rva,s.index]),[['0x609ed0',474],['0x609a38',327]]);
 const out=local('docs/reverse-client/r1-move-production-imports');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved; inspect earlier outcome before any retry');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:plan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:fixedPlanSha256,source_pins:plan.source_pins,
  metadata_bytes:0,metadata_operations:0,new_IAT_bytes:0,new_OFT_bytes:0,new_PE_header_bytes:0,new_descriptor_bytes:0,code_bytes:0,rtti_bytes:0,
  original_loaded_or_executed:false,backend_started:false,subprocesses:0,whole_table_scanned:false,files:[],bindings:[],exports:[],unsupported:[],failures:[],disk_bindings_complete:false,
  limitations:['Two disk import/export bindings only; loaded IATs, exact function boundaries, target implementations and callable ABI remain unverified.',
   'Cached same-image metadata supplies section/header/OFT continuity. No original header/OFT/descriptor/body/RTTI bytes are newly interpreted.',
   'Complete hash streams are identity checks separate from188 semantic metadata bytes, not atomic snapshots.',
   'Node path checks do not recognize every Windows reparse tag. Separate Windows Attributes/reparse preflight is required before this function is authorized.']};
 const data=new Map();
 function unsupported(message){const e=new Error(message);e.fixedUnsupported=true;throw e;}
 function inspect(def,pe,body){
  const record={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false,stable:false};report.files.push(record);let fd,initial;
  function digest(){const h=createHash('sha256'),buffer=Buffer.alloc(plan.limits.hash_chunk_bytes);for(let offset=0;offset<def.bytes;){const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,def.bytes-offset),offset);assert(n>0);h.update(buffer.subarray(0,n));offset+=n;record.hash_bytes+=n;}return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};}
  function read(rva,n,kind){
   const {section,offset}=map(rva,n);assert((section.characteristics&0x40000000)!==0&&(section.characteristics&0x20000000)===0,'Metadata must be readable non-code data');
   assert(n<=40);assert(report.metadata_bytes+n<=188);assert(++report.metadata_operations<=14);report.metadata_bytes+=n;
   if(kind==='exact-IAT-slot'){assert.equal(n,8);assert(plan.bindings.some(s=>Number(s.iat_rva)===rva));report.new_IAT_bytes+=n;assert(report.new_IAT_bytes<=16);}
   const bytes=Buffer.alloc(n);assert.equal(fs.readSync(fd,bytes,0,n,offset),n);record.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:bytes.toString('hex')});return bytes;
  }
  try{const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);record.sha256_before=digest();assert.equal(record.sha256_before,def.sha256);body(read,map);}
  finally{if(fd!==undefined){try{record.sha256_after=digest();assert.equal(record.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);record.stable=true;}finally{fs.closeSync(fd);record.closed=true;}}}
 }
 try{
  for(const pin of plan.source_pins){const p=local(pin.path);assert(regular(p).size<1048576);const bytes=fs.readFileSync(p);assert.equal(hash(bytes),pin.sha256,'Saved source changed: '+pin.key);data.set(pin.key,JSON.parse(bytes));}
  const prior=data.get('action_summary');assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,18);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);assert.equal(prior.target.path,plan.sources.Game.path);assert.equal(prior.target.sha256_before,plan.sources.Game.sha256);assert.equal(prior.target.sha256_after,plan.sources.Game.sha256);
  for(const binding of plan.bindings){const e=data.get(binding.source_key).result.structuredContent;assert.equal(e.subject.name,'Game.dll');assert.equal(e.subject.digest.sha256,plan.sources.Game.sha256);assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(plan.sources.Game.path).toLowerCase());assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,binding.procedure);assert.equal(typeof e.normalized_result,'string');assert(e.normalized_result.split('\n').map(line=>line.endsWith('\r')?line.slice(0,-1):line).includes(binding.instruction));}
  const imports=data.get('imports');assert.equal(imports.original_sha256_before,plan.sources.Game.sha256);assert.equal(imports.original_sha256_after,plan.sources.Game.sha256);assert(imports.original_stat_stable&&imports.original_hash_matches);
  const cached=imports.normal_imports[4].raw_metadata,descriptor=Buffer.from(cached.descriptor.bytes,'hex'),prefix=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');assert.equal(descriptor.length,20);assert.equal(descriptor.readUInt32LE(0),0xa42ac0);assert.equal(descriptor.readUInt32LE(12),0xa56aa6);assert.equal(descriptor.readUInt32LE(16),0x609000);assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(cached.ownership_lookup_prefix.entries,350);assert.equal(Number(cached.ownership_lookup_prefix.rva),0xa42ac0);assert.equal(prefix.length,2800);
  const tailProof=data.get('oft_tail');assert.equal(tailProof.status,'passed');
  for(const f of tailProof.files){const def=f.source===plan.sources.Game.path?plan.sources.Game:plan.sources.Engine;assert.equal(f.source,def.path);assert(f.closed&&f.stable);assert.equal(f.sha256_before,def.sha256);assert.equal(f.sha256_after,def.sha256);}
  const gameFiles=tailProof.files.filter(f=>f.source===plan.sources.Game.path);assert.equal(gameFiles.length,1);const tails=gameFiles[0].reads.filter(r=>r.kind==='OFT-nonzero-continuation-through-selected-slot');assert.equal(tails.length,1);assert.equal(tails[0].rva,'0xa435b0');assert.equal(tails[0].bytes,1104);const tail=Buffer.from(tails[0].raw_hex,'hex');assert.equal(tail.length,1104);const oft=Buffer.concat([prefix,tail]);assert.equal(oft.length,488*8);for(let offset=0;offset<oft.length;offset+=8)assert.notEqual(oft.readBigUInt64LE(offset),0n);
  const modules=data.get('inventory').filter(m=>m.relativePath==='x64/Game.dll');assert.equal(modules.length,1);assert.equal(modules[0].sha256,plan.sources.Game.sha256);assert.equal(modules[0].bytes,plan.sources.Game.bytes);const engineImports=modules[0].importModules.filter(m=>m.module==='Engine.dll');assert.equal(engineImports.length,1);for(const s of plan.bindings){assert.equal((Number(s.iat_rva)-0x609000)/8,s.index);assert.equal(engineImports[0].symbols[s.index],s.symbol);}
  const gp=data.get('game_pe');assert.equal(gp.sha256_after,plan.sources.Game.sha256);const gamePe=gp.pe;assert.equal(gamePe.image_base,'0x180000000');
  const engines=data.get('engine_pe').files.filter(f=>f.target===plan.sources.Engine.path);assert.equal(engines.length,1);const ep=engines[0];assert(ep.source_hash_preserved&&ep.source_stat_stable);assert.equal(ep.sha256_before,plan.sources.Engine.sha256);assert.equal(ep.sha256_after,plan.sources.Engine.sha256);assert.equal(ep.preferred_image_base,'0x180000000');const enginePe=ep.pe,ed=Buffer.from(enginePe.export_directory_bytes,'hex');assert.equal(ed.length,40);for(const [offset,value] of [[16,enginePe.ordinal_base],[20,enginePe.function_count],[24,enginePe.named_export_count],[28,Number(enginePe.address_table_rva)],[32,Number(enginePe.name_pointer_table_rva)],[36,Number(enginePe.name_ordinal_table_rva)]])assert.equal(ed.readUInt32LE(offset),value);
  report.saved_proofs_verified=true;report.cached_OFT_entries=488;
  inspect(plan.sources.Game,gamePe,read=>{
   for(const s of plan.bindings){const lookup=oft.readBigUInt64LE(s.index*8);if(lookup<=0n||lookup>=0x100000000n)unsupported('Selected cached import is not a bounded named import');const nameRva=Number(lookup);
    if(read(Number(s.iat_rva),8,'exact-IAT-slot').readBigUInt64LE()!==lookup)unsupported('Disk IAT does not match selected cached OFT; no fallback');
    const hint=read(nameRva,2,'exact-import-hint').readUInt16LE(),wanted=Buffer.from(s.symbol+'\0','ascii');if(!read(nameRva+2,wanted.length,'exact-import-name').equals(wanted))unsupported('Exact import name mismatch; no wider read');
    report.bindings.push({iat_rva:s.iat_rva,index:s.index,dll:'Engine.dll',symbol:s.symbol,hint,lookup_rva:hex(0xa42ac0+s.index*8),hint_name_rva:hex(nameRva),loaded_IAT_observed:false});
   }
  });
  inspect(plan.sources.Engine,enginePe,(read,map)=>{
   for(const s of report.bindings){assert(s.hint<enginePe.named_export_count);const nameRva=read(Number(enginePe.name_pointer_table_rva)+s.hint*4,4,'exact-hinted-export-name-pointer').readUInt32LE();const wanted=Buffer.from(s.symbol+'\0','ascii');if(!read(nameRva,wanted.length,'exact-export-name').equals(wanted))unsupported('Export hint/name mismatch; no search or fallback');
    const ordinalIndex=read(Number(enginePe.name_ordinal_table_rva)+s.hint*2,2,'exact-name-ordinal').readUInt16LE();assert(ordinalIndex<enginePe.function_count);const target=read(Number(enginePe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();if(target===0)unsupported('Null export target');if(target>=Number(enginePe.export_directory_rva)&&target<Number(enginePe.export_directory_rva)+enginePe.export_directory_size)unsupported('Forwarder target is outside this fixed binding scope');const section=map(target,1).section;if(!(section.characteristics&0x20000000))unsupported('Export target is not classified as executable cached section');
    report.exports.push({iat_rva:s.iat_rva,symbol:s.symbol,hint:s.hint,ordinal_index:ordinalIndex,ordinal:ordinalIndex+enginePe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target),target_section:section.name,target_payload_read:false,function_boundary_verified:false,loaded_value_observed:false});
   }
  });
  assert.equal(report.bindings.length,2);assert.equal(report.exports.length,2);assert.equal(report.metadata_operations,14);assert.equal(report.metadata_bytes,188);assert.equal(report.new_IAT_bytes,16);report.disk_bindings_complete=true;
 }catch(e){(e.fixedUnsupported?report.unsupported:report.failures).push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':'passed';const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<131072);normal(out);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.status!=='passed')throw new Error('Fixed two-IAT binding '+report.status+'; evidence preserved at '+out);
 return {status:report.status,output:plan.output,metadata_bytes:report.metadata_bytes,metadata_operations:report.metadata_operations,exports:report.exports,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
