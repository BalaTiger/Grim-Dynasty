// Independent read-only static-data inspection. No build, shell, compiler,
// subprocess, original DLL loading, or retry of the refused build script.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
assert.equal(process.argv.length,2);
const root='C:/Users/zhuzi/Documents/Grim-Dynasty';
process.chdir(root);
const target='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const sourceBytes=4485632,rva=0x41cf30,length=48;
const out='docs/object-lifecycle/offline-engine-class-parent-record';
const hash=b=>createHash('sha256').update(b).digest('hex');
const hex=n=>'0x'+n.toString(16);
const sources=[
 ['docs/object-lifecycle/offline-class-registration-exports/discovery.json',
  '0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
 ['docs/object-lifecycle/offline-class-registration-metadata/metadata.json',
  '54aa510cffe5a281c54e6de4a594f40224585822f0b40cad6c847f8318ef76b7'],
];
const [discovery,prior]=sources.map(([p,h])=>{
 const b=fs.readFileSync(p);assert.equal(hash(b),h,p);return JSON.parse(b);
});
const peSources=discovery.files.filter(f=>f.target===target);
assert.equal(peSources.length,1);const peSource=peSources[0];
assert(peSource.source_hash_preserved);assert.equal(peSource.expected_sha256,expected);
assert.equal(peSource.expected_file_bytes,sourceBytes);
const oldFiles=prior.files.filter(f=>f.target===target);assert.equal(oldFiles.length,1);
assert(oldFiles[0].source_hash_preserved);
const actor=oldFiles[0].records.filter(r=>r.name==='Actor');assert.equal(actor.length,1);
assert.equal(actor[0].rva,'0x41cfc0');assert.equal(actor[0].qwords[2],'0x18041cf30');
const sections=peSource.pe.sections.filter(s=>rva>=s.rva&&rva+length<=s.rva+s.raw_size);
assert.equal(sections.length,1);const section=sections[0];
assert.equal(section.name,'.data');assert.equal(section.characteristics&0x20000000,0);
const offset=section.raw_offset+rva-section.rva;
assert.equal(offset,4306736);assert(offset+length<=sourceBytes);
assert(!fs.existsSync(out),'Do not overwrite evidence');
assert.equal(fs.realpathSync(target).toLowerCase(),path.resolve(target).toLowerCase());
for(const p of ['docs','docs/object-lifecycle'])assert(!fs.lstatSync(p).isSymbolicLink());
const report={status:'started',started_utc:new Date().toISOString(),target,expected_sha256:expected,
 expected_file_bytes:sourceBytes,source_evidence:sources.map(([path,sha256])=>({path,sha256})),
 script_sha256:hash(fs.readFileSync('tools/static_analysis/read_engine_class_parent_record.mjs')),
 basis:'The already-read Actor ClassInfo parent field points exactly to Engine 41CF30.',
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,subprocesses:0,backend_started:false,
 code_bytes_read:0,metadata_bytes:0,requested_record_bytes:48,followed_pointers:0,
 reads:[],fingerprint_passes:[],
 restriction_boundary:{purpose:'read-only static data analysis',build_or_test_execution:false,
  refused_script_retried:false,equivalent_build_command:false,execution_policy_change:false,
  installation_or_escalation:false},
 limitations:['A 48-byte on-disk record is not a runtime registry or successful initialization.',
  'No name string, factory, parent, vtable, children or writer code is followed automatically.',
  'Whole-file hash passes use 64KiB buffers and are separate from the 48 interpreted bytes.',
  'O_RDONLY and before/after fingerprints do not guarantee an atomic snapshot.']};
let fd,stat,failure;
function fingerprint(label){
 const h=createHash('sha256'),buffer=Buffer.alloc(65536);let at=0;
 while(at<sourceBytes){
  const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,sourceBytes-at),at);
  assert(n>0);h.update(buffer.subarray(0,n));at+=n;
 }
 const sha256=h.digest('hex');report.fingerprint_passes.push({label,bytes:at,chunk_bytes:65536,sha256});
 return sha256;
}
try{
 fd=fs.openSync(target,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);
 assert(stat.isFile());assert.equal(stat.size,sourceBytes);
 report.sha256_before=fingerprint('before');assert.equal(report.sha256_before,expected);
 const b=Buffer.alloc(length);let got=0;
 while(got<length){const n=fs.readSync(fd,b,got,length-got,offset+got);assert(n>0);got+=n;}
 report.metadata_bytes=length;report.reads.push({rva:hex(rva),file_offset:offset,bytes:length,section:section.name});
 report.record={rva:hex(rva),preferred_va:'0x18041cf30',raw_bytes:b.toString('hex'),
  qwords:Array.from({length:6},(_,i)=>hex(b.readBigUInt64LE(i*8))),
  vtable:hex(b.readBigUInt64LE(0)),name:hex(b.readBigUInt64LE(8)),
  parent:hex(b.readBigUInt64LE(16)),count_signed32:b.readInt32LE(24),
  count_adjacent_word:b.readUInt32LE(28),children:hex(b.readBigUInt64LE(32)),
  factory:hex(b.readBigUInt64LE(40))};
 report.status='fixed-engine-parent-record-read';
}catch(e){failure=e;report.status='failed';report.failure=String(e);}
finally{
 if(fd!==undefined){
  try{
   if(stat&&stat.size===sourceBytes){
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
 source_hash_preserved:report.source_hash_preserved,record:report.record}));
if(failure)process.exitCode=1;
