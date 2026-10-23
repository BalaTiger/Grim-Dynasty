// Four-byte PE entry field for bounded static initializer tracing.
// No build/test, shell, subprocess, backend, policy change or original execution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
assert.equal(process.argv.length,2);
process.chdir('C:/Users/zhuzi/Documents/Grim-Dynasty');
const target='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const bytesExpected=4485632;
const hash=b=>createHash('sha256').update(b).digest('hex');
const proofPath='docs/object-lifecycle/offline-depot-ctor-boundaries/boundary.json';
const proofHash='646053e4565e3f08c696ae0e9a16ddb5c2e81ab22ffe046c5057fc41aa64bb15';
const raw=fs.readFileSync(proofPath);assert.equal(hash(raw),proofHash);
const proof=JSON.parse(raw);
assert.equal(proof.sha256_before,expected);assert.equal(proof.sha256_after,expected);
assert.equal(proof.pe.pe_header_file_offset,288);
assert.equal(proof.pe.exception_directory_rva,'0x424000');
assert.equal(proof.pe.exception_directory_size,146520);assert.equal(proof.pe.entry_count,12210);
const offset=proof.pe.pe_header_file_offset+24+16;assert.equal(offset,328);
const out='docs/object-lifecycle/offline-engine-root-entry';
assert(!fs.existsSync(out));
assert.equal(fs.realpathSync(target).toLowerCase(),path.resolve(target).toLowerCase());
for(const p of ['docs','docs/object-lifecycle'])assert(!fs.lstatSync(p).isSymbolicLink());
const report={status:'started',started_utc:new Date().toISOString(),target,expected_sha256:expected,
 expected_file_bytes:bytesExpected,source_evidence:[{path:proofPath,sha256:proofHash}],
 script_sha256:hash(fs.readFileSync('tools/static_analysis/read_engine_root_entry.mjs')),
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,subprocesses:0,
 metadata_bytes:0,code_bytes_read:0,refused_build_script_retried:false,build_or_test_execution:false,
 execution_policy_changed:false,reads:[],fingerprint_passes:[],
 limitations:['Entry identity is only a starting point; no code or initializer table is followed by this reader.',
  'Previously read 144-byte optional-header metadata did not retain the entry value; only that missing 4-byte field is read here.',
  '64KiB full-file fingerprints are separate from interpreted metadata bytes.']};
let fd,stat,failure;
function fingerprint(label){
 const h=createHash('sha256'),b=Buffer.alloc(65536);let at=0;
 while(at<bytesExpected){
  const n=fs.readSync(fd,b,0,Math.min(b.length,bytesExpected-at),at);assert(n>0);
  h.update(b.subarray(0,n));at+=n;
 }
 const sha256=h.digest('hex');
 report.fingerprint_passes.push({label,bytes:at,chunk_bytes:65536,sha256});return sha256;
}
try{
 fd=fs.openSync(target,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);
 assert(stat.isFile());assert.equal(stat.size,bytesExpected);
 report.sha256_before=fingerprint('before');assert.equal(report.sha256_before,expected);
 const b=Buffer.alloc(4);assert.equal(fs.readSync(fd,b,0,4,offset),4);
 report.metadata_bytes=4;report.reads.push({file_offset:offset,bytes:4,raw_bytes:b.toString('hex')});
 const rva=b.readUInt32LE(0);
 const sections=proof.pe.sections.filter(s=>rva>=s.rva&&rva<s.rva+s.raw_size);
 assert.equal(sections.length,1);const s=sections[0];assert(s.characteristics&0x20000000);
 report.entry={rva:'0x'+rva.toString(16),preferred_va:'0x'+(0x180000000n+BigInt(rva)).toString(16),
  file_offset:s.raw_offset+rva-s.rva,section:s.name,code_bytes_read:0};
 report.status='fixed-engine-entry-field-read';
}catch(e){failure=e;report.status='failed';report.failure=String(e);}
finally{
 if(fd!==undefined){
  try{
   if(stat&&stat.size===bytesExpected){
    report.sha256_after=fingerprint('after');const after=fs.fstatSync(fd);
    report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;
    report.source_stat_stable=after.size===stat.size&&after.ino===stat.ino&&after.dev===stat.dev&&
     after.mtimeMs===stat.mtimeMs&&after.ctimeMs===stat.ctimeMs;
    assert(report.source_hash_preserved&&report.source_stat_stable);
   }
  }catch(e){failure??=e;report.status='failed';report.final_check_failure=String(e);}
  fs.closeSync(fd);
 }
 report.finished_utc=new Date().toISOString();
 fs.mkdirSync(out);fs.writeFileSync(out+'/metadata.json',JSON.stringify(report,null,2)+'\n',{flag:'wx'});
}
console.log(JSON.stringify({status:report.status,metadata_bytes:report.metadata_bytes,
 source_hash_preserved:report.source_hash_preserved,entry:report.entry}));
if(failure)process.exitCode=1;
