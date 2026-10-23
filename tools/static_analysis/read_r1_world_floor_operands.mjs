// Fixed data-only observation. Import performs no I/O; invocation requires review.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectWorldFloorOperands(reviewedScriptSha256,reviewedPlanSha256){
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_world_floor_operands.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex');
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path: '+q);assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<=16384);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_world_floor_operands.plan.json');assert(regular(planPath).size<=12288);
 const planBytes=fs.readFileSync(planPath);assert.equal(hash(planBytes),reviewedPlanSha256);const plan=JSON.parse(planBytes);
 assert.equal(plan.schema,'grim-dynasty.fixed-world-floor-operands.v1');assert.equal(plan.script,'tools/static_analysis/read_r1_world_floor_operands.mjs');
 assert.equal(plan.entrypoint,'inspectWorldFloorOperands(reviewedScriptSha256, reviewedPlanSha256)');
 assert.equal(plan.output,'docs/reverse-client/r1-world-floor-operands/inspection.json');
 assert.deepEqual(plan.source,{path:'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',bytes:4485632,sha256:'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'});
 assert.deepEqual(plan.source_pins,[
  {key:'engine_pe',path:'docs/object-lifecycle/offline-class-registration-exports/discovery.json',sha256:'0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'},
  {key:'world_floor',path:'docs/reverse-client/r1-movement-floor-core/11-MovementFloorCore-0x180221da0-procedure_assembly.json',sha256:'0b1444a11090ae60d9f508b8342bdb43d1f7a6961a21b3dce7b7ae40166c9472'},
  {key:'nearest_constant',path:'docs/reverse-client/r1-navigation-nearest-constants/inspection.json',sha256:'02c4ec5592fdac55c1bf652b1eabdab326232c59150942f8222cd2da652d14e1'}
 ]);
 assert.deepEqual(plan.fixed_constants,[
  {key:'ray_origin_y_addend',rva:'0x34bc00',bytes:4,instruction:'0x180221e00: ADDSS XMM2, dword ptr [0x18034bc00]'},
  {key:'ray_direction_y',rva:'0x34bdf4',bytes:4,instruction:'0x180221e22: MOVSS XMM1, dword ptr [0x18034bdf4]'},
  {key:'neighbor_bounds_margin',rva:'0x34bc8c',bytes:4,instruction:'0x180221f82: MOVSS XMM9, dword ptr [0x18034bc8c]'}
 ]);
 assert.deepEqual(plan.reused_constants,[{key:'neighbor_absolute_mask_low_lane',source_pin:'nearest_constant',rva:'0x34c410',bytes:4,
  instruction:'0x180221f70: MOVSS XMM8, dword ptr [0x18034c410]',uses:['0x18022203a: ANDPS XMM1, XMM8','0x18022205c: ANDPS XMM2, XMM8']}]);
 assert.deepEqual(plan.assembly_proof,{key:'world_floor',procedure:'0x180221da0'});
 assert.deepEqual(plan.limits,{new_data_bytes:12,data_operations:3,single_read_bytes:4,hash_chunk_bytes:65536,new_PE_header_bytes:0,whole_table_bytes:0,target_body_bytes:0});
 const out=local('docs/reverse-client/r1-world-floor-operands');normal(path.dirname(out));
 try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);const def=plan.source;
 const report={schema:plan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,source_pins:plan.source_pins,
  code_bytes:0,metadata_bytes:0,data_bytes:0,data_operations:0,new_PE_header_bytes:0,whole_table_bytes:0,target_body_bytes:0,
  original_loaded_or_executed:false,subprocesses:0,files:[],constants:[],reused_constants:[],disk_observations_complete:false,failures:[],
  limitations:['Three DWORD disk operands only; loaded values, floating environment and complete World floor behavior remain unverified.',
   '34C410 is reused from pinned evidence; no new read and no inference about upper SIMD lanes.',
   'Whole-file hash streams are separate from12B semantic reads. Before/after checks are not atomic snapshots.',
   'Node lstat/realpath checks do not identify every Windows reparse tag/attribute; separate Attributes preflight remains required.']};
 const data=new Map();
 try{
  for(const pin of plan.source_pins){const p=local(pin.path);assert(regular(p).size<1048576);const b=fs.readFileSync(p);assert.equal(hash(b),pin.sha256);data.set(pin.key,JSON.parse(b));}
  const matches=data.get('engine_pe').files.filter(f=>f.target===def.path);assert.equal(matches.length,1);const cached=matches[0];
  assert.equal(cached.status,'metadata-discovery-completed');assert.equal(cached.expected_file_bytes,def.bytes);assert(cached.source_stat_stable&&cached.source_hash_preserved);
  assert.equal(cached.sha256_before,def.sha256);assert.equal(cached.sha256_after,def.sha256);assert.equal(cached.preferred_image_base,'0x180000000');const pe=cached.pe;
  assert(Number.isSafeInteger(pe.size_of_image)&&pe.size_of_image>0);assert(Array.isArray(pe.sections)&&pe.sections.length>0&&pe.sections.length<=16);
  for(const s of pe.sections)for(const k of ['rva','raw_size','raw_offset','characteristics'])assert(Number.isSafeInteger(s[k])&&s[k]>=0);
  const proof=plan.assembly_proof,sc=data.get(proof.key).result.structuredContent;
  assert.equal(sc.operation,'procedure_assembly');assert.equal(sc.parameters.procedure,proof.procedure);
  assert.equal(sc.subject.digest.sha256,def.sha256);assert.equal(path.normalize(sc.subject.local_path),path.normalize(def.path));
  assert.equal(typeof sc.normalized_result,'string');assert(sc.normalized_result.startsWith(proof.procedure+': '));const lines=sc.normalized_result.split(/\r?\n/);
  for(const c of plan.fixed_constants)assert(lines.includes(c.instruction),c.instruction);
  const prior=data.get('nearest_constant');assert.equal(prior.status,'passed');assert.equal(prior.source,def.path);assert.equal(prior.expected_sha256,def.sha256);
  assert.equal(prior.sha256_before,def.sha256);assert.equal(prior.sha256_after,def.sha256);assert(prior.stable&&prior.closed);assert.deepEqual(prior.failures,[]);
  for(const c of plan.reused_constants){assert(lines.includes(c.instruction));for(const use of c.uses)assert(lines.includes(use));
   const found=prior.reads.filter(r=>r.rva===c.rva&&r.bytes===c.bytes);assert.equal(found.length,1);const r=found[0];
   assert.match(r.raw_hex,/^[a-f0-9]{8}$/);const b=Buffer.from(r.raw_hex,'hex');assert.equal(r.bits,'0x'+b.readUInt32LE().toString(16).padStart(8,'0'));
   report.reused_constants.push({...c,raw_hex:r.raw_hex,uint32_hex:r.bits,reused:true,new_data_bytes:0,loaded_value_observed:false});
  }
  report.proofs_verified=true;
  const f={source:def.path,expected_sha256:def.sha256,target_open_mode:'O_RDONLY',reads:[],hash_bytes:0,closed:false};report.files.push(f);let fd,initial;
  function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<def.bytes;){const n=fs.readSync(fd,b,0,Math.min(b.length,def.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;f.hash_bytes+=n;}return h.digest('hex');}
  function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);const hits=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(hits.length,1);
   const section=hits[0],offset=section.raw_offset+rva-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+n<=def.bytes);return {section,offset};}
  try{
   const before=regular(def.path);assert.equal(before.size,def.bytes);fd=fs.openSync(def.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);
   f.sha256_before=digest();assert.equal(f.sha256_before,def.sha256);
   for(const c of plan.fixed_constants){assert.equal(c.bytes,4);const {section,offset}=map(Number(c.rva),4);assert(section.characteristics&0x40000000);assert(!(section.characteristics&0x20000000));
    assert(report.data_bytes+4<=12);assert(report.data_operations+1<=3);report.data_bytes+=4;report.data_operations++;
    const b=Buffer.alloc(4);assert.equal(fs.readSync(fd,b,0,4,offset),4);const observation={rva:c.rva,file_offset:offset,section:section.name,bytes:4,raw_hex:b.toString('hex')};f.reads.push(observation);
    report.constants.push({...c,...observation,uint32_hex:'0x'+b.readUInt32LE().toString(16).padStart(8,'0'),reused:false,loaded_value_observed:false});
   }
   assert.equal(report.constants.length,3);assert.equal(report.data_bytes,12);assert.equal(report.data_operations,3);report.disk_observations_complete=true;
  }finally{if(fd!==undefined){try{f.sha256_after=digest();assert.equal(f.sha256_after,def.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(def.path),initial);f.stable=true;}finally{fs.closeSync(fd);f.closed=true;}}}
 }catch(e){report.failures.push(String(e));}
 report.finished_utc=new Date().toISOString();report.status=!report.failures.length&&report.disk_observations_complete?'passed':'failed';
 if(report.status==='failed'&&!report.failures.length)report.failures.push('Incomplete observation');
 const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<65536);normal(out);fs.writeFileSync(path.join(out,'inspection.json'),serialized,{flag:'wx'});
 if(report.failures.length)throw new Error('Fixed World floor operands read failed; preserved '+out+': '+report.failures.join('; '));
 return {status:report.status,output:plan.output,data_bytes:report.data_bytes,metadata_bytes:report.metadata_bytes,constants:report.constants,reused_constants:report.reused_constants,
  original_hashes:report.files.map(({source,sha256_before,sha256_after,stable,closed})=>({source,sha256_before,sha256_after,stable,closed}))};
}
