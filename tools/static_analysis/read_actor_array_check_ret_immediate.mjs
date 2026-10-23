// Prepared only: exactly two immediate bytes after the previously observed C2; no opcode reread.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632;
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const instructionRva=0xce70,target=0xce71,base=0x180000000n,out=path.join(root,'docs/object-lifecycle/offline-actor-array-check-ret-immediate');
const sources=[
 ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json','39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
 ['docs/object-lifecycle/offline-actor-array-check-slot/metadata.json','0104909b53b3c083ecceb817c1b3048f101f8021ffbcd51fcb438ebd408d56d7'],
 ['docs/object-lifecycle/offline-actor-array-check-file-target/metadata.json','651d25f0a7a4bababcf62cd86d8331b9e55645f373533b826750dcd442fcf976']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink(),'Redirected path refused');assert(first&&file?s.isFile():s.isDirectory());const d=path.dirname(c);if(d===c)return;c=d;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output refused');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.ino===b.ino&&a.dev===b.dev&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
function fingerprint(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');let at=0;while(at<n){const k=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(k>0);h.update(b.subarray(0,k));at+=k;}return {sha256:h.digest('hex'),bytes:at,chunk_bytes:65536};}
plain(path.dirname(out));absent(out);
const proofs=sources.map(([p,h])=>{const f=path.join(root,p);plain(f,true);assert(fs.statSync(f).size<=131072);const b=fs.readFileSync(f);assert(b.length<=131072);assert.equal(hash(b),h);return JSON.parse(b.toString('utf8'));});
for(const p of proofs){assert.equal(p.original,original);assert.equal(p.expected_sha256,expected);assert.equal(p.sha256_before,expected);assert.equal(p.sha256_after,expected);assert(p.source_hash_preserved&&p.source_stat_stable);assert.deepEqual(p.failures,[]);}
assert.equal(proofs[0].status,'exact-actor-export-and-boundary-verified');const pe=proofs[0].pe;assert.equal(pe.image_base,hex(base));
const slot=proofs[1];assert.equal(slot.status,'file-slot-matches-load-config-check-field');assert.equal(slot.slot_rva,'0x302d38');
assert.equal(slot.slot.raw_hex,'70ce008001000000');assert.equal(slot.slot.file_value_va,hex(base+BigInt(instructionRva)));assert.equal(slot.slot.file_value_rva,hex(instructionRva));
assert.equal(slot.load_config.check_slot_match,true);assert.equal(slot.load_config.check_field.declared_slot_va,'0x180302d38');
assert.equal(slot.runtime_slot_value_observed,false);assert.equal(slot.loader_rebinding_observed,false);
const opcode=proofs[2];assert.equal(opcode.status,'non-c3-file-entry-unknown-stopped');assert.equal(opcode.target_rva,hex(instructionRva));assert.equal(opcode.raw_hex,'c2');assert.equal(opcode.code_bytes,1);assert.equal(opcode.file_entry_ret_observed,false);
const sections=pe.sections.filter(s=>target>=s.rva&&target-s.rva+2<=s.raw_size);assert.equal(sections.length,1);const section=sections[0];
assert.equal(section.name,'.text');assert(section.characteristics&0x20000000);const offset=section.raw_offset+target-section.rva;assert(Number.isSafeInteger(offset)&&offset>=0&&offset+2<=size);
const report={schema:'grim-dynasty-actor-array-check-ret-immediate-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,expected_file_bytes:size,
 source_evidence:sources.map(([path,sha256])=>({path,sha256})),instruction_rva:hex(instructionRva),operand_begin_rva:hex(target),operand_end_rva_exclusive:hex(target+2),file_offset:offset,opcode_read_this_run:false,
 limits:{code_bytes:2,metadata_bytes:0,fingerprint_chunk_bytes:65536},code_bytes:0,metadata_bytes:0,fingerprint_passes:[],
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,subprocesses:0,dumpbin_invoked:false,build_or_test_execution:false,
 original_modified:false,permission_change:false,full_target_copy_created:false,runtime_slot_value_observed:false,loader_rebinding_observed:false,
 limitations:['The only semantic read is [CE71,CE73), the two immediate bytes after the previously saved C2 opcode. CE70 is not reread.',
  'Fixed x64 C2 iw decoding yields the unsigned little-endian imm16 stack adjustment. Its value is not presumed zero; a nonzero value is not treated as an ordinary guard return.',
  'No neighbors, tables, imports, .pdata or additional code are read. File-entry behavior does not establish the loaded guard target or runtime exception behavior.',
  'No instruction executes. Fingerprints are separate from this two-byte scope; before/after hash/stat checks are not an atomic snapshot.']};
let fd,stat,outCreated=false;const failures=[],save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
try{
 fs.mkdirSync(out);outCreated=true;plain(out);plain(script,true);report.script_sha256=hash(fs.readFileSync(script));
 plain(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);assert(stat.isFile()&&stat.size===size);
 const before=fingerprint(fd,size);report.fingerprint_passes.push({label:'before',...before});report.sha256_before=before.sha256;assert.equal(before.sha256,expected);
 const operands=Buffer.alloc(2);while(report.code_bytes<2){const n=fs.readSync(fd,operands,report.code_bytes,2-report.code_bytes,offset+report.code_bytes);assert(n>0);report.code_bytes+=n;}
 report.raw_operand_hex=operands.toString('hex');save('00ce71-ret-operands.bin',operands);const immediate=operands.readUInt16LE(0);report.immediate_u16=immediate;
 report.file_initializer_stack_adjustment_zero=immediate===0;report.ordinary_loaded_guard_return_claimed=false;
 report.instruction={address:hex(base+BigInt(instructionRva)),bytes:'c2'+report.raw_operand_hex,instruction:'ret '+hex(immediate),length:3,
  opcode_source:sources[2][0],operands_source:'00ce71-ret-operands.bin',derivation:'fixed x64 C2 iw encoding; no disassembler or execution'};
 report.status=immediate===0?'file-initializer-ret-imm16-zero-observed':'file-initializer-ret-imm16-nonzero-observed-runtime-unknown';
}catch(error){failures.push(String(error));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){const after=fingerprint(fd,size);report.fingerprint_passes.push({label:'after',...after});report.sha256_after=after.sha256;assert.equal(after.sha256,expected);report.source_stat_stable=stable(stat,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){failures.push('Original final verification: '+String(e));}finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(e){failures.push('Original close: '+String(e));}}}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();
 report.controller_resource_usage=process.resourceUsage();if(outCreated){plain(out);save('metadata.json',JSON.stringify(report,null,2)+'\n');}
}
console.log(JSON.stringify({status:report.status,raw_operand_hex:report.raw_operand_hex,immediate_u16:report.immediate_u16,code_bytes:report.code_bytes,metadata_bytes:0,subprocesses:0,original_unchanged:report.source_hash_preserved,failures}));
if(failures.length)process.exitCode=1;
