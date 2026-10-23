// Same bounded O_RDONLY/cached-PE observation pattern as read_r1_ground_loader_slot.mjs.
// Import performs no I/O; no target execution or function-body reading.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectGroundGeometryBindings(reviewedScriptSha256,reviewedPlanSha256){
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_ground_geometry_bindings.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path: '+q);assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=16384);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_ground_geometry_bindings.plan.json');assert(regular(planPath).size<=12288);
 const planBytes=fs.readFileSync(planPath);assert.equal(hash(planBytes),reviewedPlanSha256);const plan=JSON.parse(planBytes);
 assert.equal(plan.schema,'grim-dynasty.fixed-ground-geometry-bindings.v1');assert.equal(plan.script,'tools/static_analysis/read_r1_ground_geometry_bindings.mjs');
 assert.equal(plan.entrypoint,'inspectGroundGeometryBindings(reviewedScriptSha256, reviewedPlanSha256)');
 assert.equal(plan.output,'docs/reverse-client/r1-ground-geometry-bindings/inspection.json');
 assert.deepEqual(plan.source,{path:'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',bytes:4485632,sha256:'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'});
 assert.deepEqual(plan.fixed_slots.map(s=>[s.table_rva,s.slot_offset,s.rva,s.bytes]),[
  ['0x343b60','0x100','0x343c60',8],['0x3438f8','0x10','0x343908',8],['0x3438f8','0x18','0x343910',8],['0x3438f8','0x48','0x343940',8]]);
 assert.deepEqual(plan.fixed_constants.map(c=>[c.rva,c.bytes]),[['0x338458',4],['0x34c450',16]]);
 assert.deepEqual(plan.limits,{new_data_bytes:52,data_operations:6,single_read_bytes:16,hash_chunk_bytes:65536,new_PE_header_bytes:0,whole_table_bytes:0,target_body_bytes:0});
 assert.deepEqual(plan.source_pins.map(p=>p.key),['engine_pe','ground_constructor','block_constructor','block_bounds','intersection','construct_blocks']);
 assert.deepEqual(plan.assembly_proofs.map(p=>[p.key,p.procedure]),[['ground_constructor','0x1801dda90'],['block_constructor','0x1801d0e20'],['block_bounds','0x1801d0fc0'],['intersection','0x1801d0100'],['construct_blocks','0x1801ce910']]);
 const out=local('docs/reverse-client/r1-ground-geometry-bindings');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);const def=plan.source;
 const report={schema:plan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,source_pins:plan.source_pins,
  code_bytes:0,metadata_bytes:0,data_bytes:0,data_operations:0,new_PE_header_bytes:0,whole_table_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,files:[],values:[],constants:[],disk_observations_complete:false,unsupported:[],failures:[],
  limitations:['Four primary-table disk address candidates only; table extent,later overwrites,loaded dispatch,ABI and geometry behavior remain unverified.',
   'The entire16B mask is observed because1D1010 uses an xmmword memory operand; preserve all lanes,do not assume upper lanes from earlier4B evidence.',
   'Whole-file hash streams are separate from52B semantic reads. Before/after checks are not atomic snapshots.',
   'Node path checks do not identify every Windows reparse tag; separate Attributes preflight remains required.']};
 const data=new Map();
 function unsupported(reason){const e=new Error(reason);e.fixedUnsupported=true;throw e;}
 try{
  for(const pin of plan.source_pins){assert.match(pin.sha256,/^[a-f0-9]{64}$/);const p=local(pin.path);assert(regular(p).size<1048576);const b=fs.readFileSync(p);assert.equal(hash(b),pin.sha256);data.set(pin.key,JSON.parse(b));}
  const matches=data.get('engine_pe').files.filter(f=>f.target===def.path);assert.equal(matches.length,1);const cached=matches[0];
  assert.equal(cached.status,'metadata-discovery-completed');assert.equal(cached.expected_file_bytes,def.bytes);assert(cached.source_stat_stable&&cached.source_hash_preserved);
  assert.equal(cached.sha256_before,def.sha256);assert.equal(cached.sha256_after,def.sha256);assert.equal(cached.preferred_image_base,'0x180000000');const pe=cached.pe;
  assert(Number.isSafeInteger(pe.size_of_image)&&pe.size_of_image>0);assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections)for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);
  for(const proof of plan.assembly_proofs){const sc=data.get(proof.key).result.structuredContent;assert.equal(sc.operation,'procedure_assembly');assert.equal(sc.parameters.procedure,proof.procedure);
   assert.equal(sc.subject.digest.sha256,def.sha256);assert.equal(path.normalize(sc.subject.local_path),path.normalize(def.path));
   assert.equal(typeof sc.normalized_result,'string');assert(sc.normalized_result.startsWith(proof.procedure+': '));const lines=sc.normalized_result.split('\n');for(const line of proof.instructions)assert(lines.includes(line),line);
  }
  for(const s of plan.fixed_slots)assert.equal(Number(s.table_rva)+Number(s.slot_offset),Number(s.rva));report.proofs_verified=true;
  const f={source:def.path,expected_sha256:def.sha256,reads:[],hash_bytes:0,closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;}return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};}
  function read(rvaText,n,kind){assert(n===4||n===8||n===16);const {section,offset}=map(Number(rvaText),n);assert(section.characteristics&0x40000000);assert(!(section.characteristics&0x20000000));
   assert(report.data_bytes+n<=52);assert(report.data_operations+1<=6);report.data_bytes+=n;report.data_operations++;
   const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);const observation={kind,rva:rvaText,file_offset:offset,section:section.name,bytes:n,raw_hex:b.toString('hex')};f.reads.push(observation);return {b,observation};}
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);
   f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   for(const slot of plan.fixed_slots){const got=read(slot.rva,slot.bytes,'fixed-primary-vtable-slot'),va=got.b.readBigUInt64LE();
    const value={...slot,...got.observation,target_va:hex(va),candidate_rva:null,classification:null,function_name:null,function_boundary_verified:false,loaded_value_observed:false,table_extent_verified:false,subsequent_overwrites_excluded:false};report.values.push(value);
    if(va<0x180000000n||va>=0x180000000n+BigInt(pe.size_of_image))unsupported(slot.key+': target outside cached Engine image');
    const target=Number(va-0x180000000n);value.candidate_rva=hex(target);const codeSections=pe.sections.filter(s=>target>=s.rva&&target<s.rva+s.raw_size);
    if(codeSections.length!==1||!(codeSections[0].characteristics&0x20000000))unsupported(slot.key+': target lacks unique executable disk-backed section');value.classification='ExecutableAddressCandidate';value.candidate_code_section=codeSections[0].name;
   }
   for(const c of plan.fixed_constants){const got=read(c.rva,c.bytes,c.bytes===16?'fixed-xmmword-XORPS-operand':'fixed-dword-MOVSS-operand');
    report.constants.push({...c,...got.observation,uint32_lanes_hex:Array.from({length:c.bytes/4},(_,i)=>'0x'+got.b.readUInt32LE(i*4).toString(16).padStart(8,'0')),reused:false,loaded_value_observed:false});
   }
   assert.equal(report.values.length,4);assert.equal(report.constants.length,2);assert.equal(report.data_bytes,52);assert.equal(report.data_operations,6);report.disk_observations_complete=true;
  }finally{if(fd!==undefined){try{f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;}finally{fs.closeSync(fd);f.closed=true;}}}
 }catch(e){if(e.fixedUnsupported)report.unsupported.push(e.message);else report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':report.unsupported.length?'unsupported':report.disk_observations_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete observation without explicit unsupported reason');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<65536);normal(out);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed ground geometry read failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:plan.output,data_bytes:report.data_bytes,metadata_bytes:report.metadata_bytes,values:report.values,constants:report.constants,unsupported:report.unsupported,original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
