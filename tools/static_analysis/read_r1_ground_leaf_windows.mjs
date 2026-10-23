// Import performs no I/O. Fixed disk-code windows only; never execute or load Engine.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectGroundLeafWindows(reviewedScriptSha256, reviewedPlanSha256) {
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_ground_leaf_windows.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex');
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path: '+q);assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=16384);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_ground_leaf_windows.plan.json');assert(regular(planPath).size<=12288);
 const planBytes=fs.readFileSync(planPath);assert.equal(hash(planBytes),reviewedPlanSha256);const plan=JSON.parse(planBytes);
 assert.equal(plan.schema,'grim-dynasty.fixed-ground-leaf-windows.v1');assert.equal(plan.script,'tools/static_analysis/read_r1_ground_leaf_windows.mjs');
 assert.equal(plan.entrypoint,'inspectGroundLeafWindows(reviewedScriptSha256, reviewedPlanSha256)');
 assert.equal(plan.output,'docs/reverse-client/r1-ground-leaf-windows/inspection.json');
 assert.deepEqual(plan.source,{path:'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',bytes:4485632,sha256:'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'});
 assert.deepEqual(plan.windows,[{rva:'0x1de810',bytes:64,exclusive_stop_rva:'0x1de850'},{rva:'0x1de850',bytes:48,exclusive_stop_rva:'0x1de880'}]);
 assert.deepEqual(plan.limits,{code_bytes:112,code_operations:2,single_read_bytes:64,hash_chunk_bytes:65536,new_PE_header_bytes:0,metadata_bytes:0});
 assert.deepEqual(plan.source_pins.map(p=>p.key),['engine_pe','intersection_slots','geometry_slots','failed_summary','failed_response']);
 const out=local('docs/reverse-client/r1-ground-leaf-windows');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);const def=plan.source;
 const report={schema:plan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,source_pins:plan.source_pins,
  code_bytes:0,code_operations:0,metadata_bytes:0,new_PE_header_bytes:0,original_loaded_or_executed:false,subprocesses:0,files:[],windows:[],failures:[],disk_observations_complete:false,
  limitations:['These are two bounded executable-byte windows between independently observed entry candidates. The neighboring address is a read ceiling, not proof of function extent.',
   'No instruction decoder, inferred RET boundary, control-flow completeness, pseudocode, callable function or loaded runtime behavior is asserted.',
   'Whole-file hash streams are separate from112B semantic reads. Before/after checks are not atomic snapshots.',
   'Node path checks do not identify every Windows reparse tag; separate Windows Attributes preflight remains required.']};
 try{
  const data=new Map();for(const pin of plan.source_pins){assert.match(pin.sha256,/^[a-f0-9]{64}$/);const p=local(pin.path);assert(regular(p).size<1048576);const b=fs.readFileSync(p);assert.equal(hash(b),pin.sha256);data.set(pin.key,JSON.parse(b));}
  const matches=data.get('engine_pe').files.filter(f=>f.target===def.path);assert.equal(matches.length,1);const cached=matches[0];
  assert.equal(cached.status,'metadata-discovery-completed');assert.equal(cached.expected_file_bytes,def.bytes);assert(cached.source_stat_stable&&cached.source_hash_preserved);
  assert.equal(cached.sha256_before,def.sha256);assert.equal(cached.sha256_after,def.sha256);assert.equal(cached.preferred_image_base,'0x180000000');const pe=cached.pe;
  assert(Number.isSafeInteger(pe.size_of_image)&&pe.size_of_image>0);assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections)for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);
  for(const key of ['intersection_slots','geometry_slots']){const proof=data.get(key);assert.equal(proof.status,'passed');assert.equal(proof.disk_observations_complete,true);
   assert.equal(proof.files.length,1);const f=proof.files[0];assert.equal(f.source,def.path);assert(f.stable&&f.closed);assert.equal(f.sha256_before,def.sha256);assert.equal(f.sha256_after,def.sha256);
  }
  for(const [key,rva,va,raw] of [['intersection_slots','0x343c38','0x1801de810','10e81d8001000000'],['intersection_slots','0x343c50','0x1801de850','50e81d8001000000'],['geometry_slots','0x343c60','0x1801de880','80e81d8001000000']]){
   const found=data.get(key).values.filter(v=>v.rva===rva);assert.equal(found.length,1);assert.equal(found[0].target_va,va);assert.equal(found[0].raw_hex,raw);assert.equal(found[0].classification,'ExecutableAddressCandidate');
  }
  const prior=data.get('failed_summary');assert.equal(prior.status,'failed');assert.equal(prior.completed_plan_calls,6);assert.equal(prior.tool_calls,9);
  assert(prior.closed_session_verified&&prior.process_exit_confirmed&&prior.owned_memory_profile.job_empty_confirmed&&prior.owned_memory_profile.sampler_exit_confirmed);
  assert.equal(prior.target.sha256_before,def.sha256);assert.equal(prior.target.sha256_after,def.sha256);
  const failed=data.get('failed_response');assert.equal(failed.result.isError,true);
  const error=JSON.parse(failed.result.content.find(c=>c.type==='text').text).error;assert.equal(error.code,'invalid_request');assert.equal(error.category,'invalid_input');
  assert(JSON.stringify(error).includes('File offset 0x1801de850 has no loaded source mapping'));
  for(const w of plan.windows)assert.equal(Number(w.rva)+w.bytes,Number(w.exclusive_stop_rva));report.proofs_verified=true;
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;}return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};}
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);
   f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   for(const window of plan.windows){const {section,offset}=map(Number(window.rva),window.bytes);assert(section.characteristics&0x40000000);assert(section.characteristics&0x20000000);
    assert(window.bytes<=64);assert(report.code_bytes+window.bytes<=112);assert(report.code_operations+1<=2);
    report.code_bytes+=window.bytes;report.code_operations++;const b=Buffer.alloc(window.bytes);assert.equal(fs.readSync(fd,b,0,b.length,offset),b.length);
    const observation={...window,file_offset:offset,section:section.name,raw_hex:b.toString('hex'),function_boundary_verified:false,decoded_instructions:null,loaded_value_observed:false};f.reads.push(observation);report.windows.push(observation);
   }
   assert.equal(report.windows.length,2);assert.equal(report.code_bytes,112);assert.equal(report.code_operations,2);report.disk_observations_complete=true;
  }finally{if(fd!==undefined){try{f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;}finally{fs.closeSync(fd);f.closed=true;}}}
 }catch(e){report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':report.disk_observations_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete observation');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<65536);normal(out);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed ground leaf byte read failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:plan.output,code_bytes:report.code_bytes,code_operations:report.code_operations,windows:report.windows,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
