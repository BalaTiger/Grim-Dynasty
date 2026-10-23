// Prepared fixed read only. Importing this module performs no I/O.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
export function inspectCrowdRaycastThreshold(reviewedScriptSha256,reviewedPlanSha256){
 const expectedPlan={
  "schema": "grim-dynasty.fixed-crowd-raycast-threshold.v1",
  "script": "tools/static_analysis/read_r1_crowd_raycast_threshold.mjs",
  "entrypoint": "inspectCrowdRaycastThreshold(reviewedScriptSha256,reviewedPlanSha256)",
  "output": "docs/reverse-client/r1-crowd-raycast-threshold/inspection.json",
  "source": {
    "path": "D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll",
    "bytes": 4485632,
    "sha256": "4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153"
  },
  "proof": {
    "file": "docs/reverse-client/r1-crowd-motion-branches/17-CrowdMotionBranch-0x1800309f0-procedure_assembly.json",
    "sha256": "42be90fb31c4edaaa251d6223a190977f7e7fa44240181d9f9dba1eb1ca19e62",
    "procedure": "0x1800309f0",
    "instruction": "0x180030aec: COMISS XMM0, dword ptr [0x18034bb38]"
  },
  "pe_cache": {
    "file": "docs/object-lifecycle/offline-class-registration-exports/discovery.json",
    "sha256": "0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58"
  },
  "target": {
    "rva": "0x34bb38",
    "bytes": 4
  },
  "limits": {
    "data_bytes": 4,
    "new_metadata_bytes": 0,
    "code_bytes": 0,
    "hash_chunk_bytes": 65536
  },
  "requirements": [
    "Separate formal execution approval; module import performs noI/O.",
    "External Windows Attributes/reparse ancestor checks remain required.",
    "Reuse cached PE only after exact source SHA256 verification; readable nonexecutablesection andfilebounds enforced.",
    "Exactly4Boperand; no expected value,adjacent data,code,loaded global,subprocess,backend ororiginal execution.",
    "Same descriptor before/afterSHA256 andstat/path checks;newoutputexclusivewrite;failurespreserved."
  ]
};
 const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
 assert.equal(process.platform,'win32');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.equal(path.basename(self),'read_r1_crowd_raycast_threshold.mjs');
 const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path');assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());
  if(path.dirname(q)===q)break;
 }}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile());assert.equal(s.nlink,1);assert(s.ino!==0);return s;}
 function sameStat(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 function saved(proof){const p=path.resolve(root,proof.file);assert(p.startsWith(root+path.sep));assert(regular(p).size<1048576);const bytes=fs.readFileSync(p);assert.equal(hash(bytes),proof.sha256);return JSON.parse(bytes);}
 assert.match(reviewedScriptSha256,/^[a-f0-9]{64}$/);assert.match(reviewedPlanSha256,/^[a-f0-9]{64}$/);
 assert(regular(self).size<32768);assert.equal(hash(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=path.join(root,'tools/static_analysis/read_r1_crowd_raycast_threshold.plan.json');
 assert(regular(planPath).size<16384);const pb=fs.readFileSync(planPath);assert.equal(hash(pb),reviewedPlanSha256);assert.deepEqual(JSON.parse(pb),expectedPlan);
 const source=expectedPlan.source,out=path.join(root,'docs/reverse-client/r1-crowd-raycast-threshold');
 normal(path.dirname(out));try{fs.lstatSync(out);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(out);normal(out);
 const report={schema:expectedPlan.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,
  source,proof:expectedPlan.proof,pe_cache:expectedPlan.pe_cache,data_bytes:0,metadata_bytes:0,code_bytes:0,hash_bytes:0,subprocesses:0,
  original_loaded_or_executed:false,source_closed:false,values:[],failures:[],
  limitations:['Only disk4B at Engine34BB38 bound to030AEC COMISS is observed. Loaded operand and native FP environment are unobserved.',
   'No value is assumed before reading; hexadecimal bits are authoritative,including nonfinite floats.',
   'Cached PE sections are reused with exact fileSHA256,not a fresh headerread or atomic snapshot.',
   'External Windows reparse-tag/Attributes checks remain required;Node cannot identify every tag.']};
 let fd,initial;
 function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<source.bytes;){const n=fs.readSync(fd,b,0,Math.min(b.length,source.bytes-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;report.hash_bytes+=n;}return h.digest('hex');}
 try{
  const e=saved(expectedPlan.proof).result.structuredContent;
  assert.equal(e.subject.name,'Engine.dll');assert.equal(e.subject.digest.sha256,source.sha256);
  assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(source.path).toLowerCase());
  assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,expectedPlan.proof.procedure);
  assert.equal(typeof e.normalized_result,'string');assert(e.normalized_result.split('\n').includes(expectedPlan.proof.instruction));
  const matches=saved(expectedPlan.pe_cache).files.filter(f=>f.target===source.path);assert.equal(matches.length,1);const proof=matches[0];
  assert.equal(proof.sha256_before,source.sha256);assert.equal(proof.sha256_after,source.sha256);assert(proof.source_hash_preserved&&proof.source_stat_stable);
  assert.equal(proof.preferred_image_base,'0x180000000');
  const rva=Number(expectedPlan.target.rva),bytes=expectedPlan.target.bytes;assert.equal(rva,0x34bb38);assert.equal(bytes,4);
  const hits=proof.pe.sections.filter(s=>rva>=s.rva&&rva+bytes<=s.rva+s.raw_size);assert.equal(hits.length,1);const section=hits[0];
  assert((section.characteristics&0x40000000)&&!(section.characteristics&0x20000000));
  const offset=section.raw_offset+rva-section.rva;assert(offset>=0&&offset+bytes<=source.bytes);report.proofs_verified=true;
  const before=regular(source.path);assert.equal(before.size,source.bytes);fd=fs.openSync(source.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());sameStat(initial,before);
  report.sha256_before=digest();assert.equal(report.sha256_before,source.sha256);
  const raw=Buffer.alloc(4);assert.equal(fs.readSync(fd,raw,0,4,offset),4);report.data_bytes=4;
  const value=raw.readFloatLE();report.values.push({rva:hex(rva),va:hex(0x180000000+rva),file_offset:offset,bytes:4,section:section.name,raw_hex:raw.toString('hex'),uint32_hex:hex(raw.readUInt32LE()).replace(/^0x([0-9a-f]+)$/,(m,b)=>'0x'+b.padStart(8,'0')),float32_le:Number.isFinite(value)?value:null,float32_nonfinite:Number.isFinite(value)?null:String(value),loaded_value_observed:false});
 }catch(e){report.failures.push(String(e));}
 finally{if(fd!==undefined){try{report.sha256_after=digest();assert.equal(report.sha256_after,source.sha256);sameStat(fs.fstatSync(fd),initial);sameStat(regular(source.path),initial);report.stable=true;}
  catch(e){report.failures.push(String(e));}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push(String(e));}}}}
 report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';
 normal(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});
 if(report.failures.length)throw Error('Fixed raycast operand read failed;preserved '+out+': '+report.failures.join('; '));return report;
}

