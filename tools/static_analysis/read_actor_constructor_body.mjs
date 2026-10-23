// Prepared for separate review/approval; never follows callees or unwind data.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const script=fileURLToPath(import.meta.url), root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153', size=4485632;
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const source={path:'docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json',
 sha256:'39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'};
const out=path.join(root,'docs/object-lifecycle/offline-actor-constructor-body');
const temp=path.join(root,'.tooling/offline-actor-constructor-body-temp');
const begin=0x259a0,end=0x25d8d,codeOffset=150944,pdataOffset=4331052,pdataRva=0x42742c;
const entryIndex=1113,unwind=0x367948,rawEntry='a05902008d5d020048793600',base=0x180000000n;
const limits={code_bytes:1005,metadata_bytes:12,fingerprint_chunk_bytes:65536,
 proof_bytes:65536,tool_timeout_ms:10000,tool_max_buffer_bytes:196608};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function plain(name,directory=false){
 let p=path.resolve(name),first=true;
 for(;;){const s=fs.lstatSync(p);assert(!s.isSymbolicLink(),'Redirected path refused');
  assert(first&&!directory?s.isFile():s.isDirectory(),'Unexpected path type');
  const parent=path.dirname(p);if(parent===p)return;p=parent;first=false;}
}
function absent(name){try{fs.lstatSync(name);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output/temp path refused');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
function fingerprint(fd,length){
 const buffer=Buffer.alloc(limits.fingerprint_chunk_bytes),digest=createHash('sha256');let position=0;
 while(position<length){const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,length-position),position);
  assert(n>0,'Unexpected fingerprint EOF');digest.update(buffer.subarray(0,n));position+=n;}
 return {sha256:digest.digest('hex'),bytes:position,chunk_bytes:buffer.length};
}
function hashFile(name){plain(name);const f=fs.openSync(name,fs.constants.O_RDONLY);
 try{const before=fs.fstatSync(f);assert(before.isFile());const result=fingerprint(f,before.size);
  assert(stable(before,fs.fstatSync(f)),'Fingerprint file changed');return result;}finally{fs.closeSync(f);}}
const report={schema:'grim-dynasty-fixed-actor-constructor-body-v1',status:'started',started_utc:new Date().toISOString(),
 original,expected_sha256:expected,expected_file_bytes:size,source_evidence:[source],tool,expected_tool_sha256:toolHash,limits,
 requested_rva:hex(begin),end_rva_exclusive:hex(end),code_file_offset:codeOffset,
 pdata_entry:{index:entryIndex,rva:hex(pdataRva),file_offset:pdataOffset,raw_hex:rawEntry,unwind_rva:hex(unwind)},
 target_open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,original_modified:false,
 full_target_copy_created:false,build_or_test_execution:false,permission_change:false,
 code_bytes:0,metadata_bytes:0,subprocesses:0,reads:[],fingerprint_passes:[],
 limitations:[
  'Exactly one saved .pdata fragment; no callee, adjacent code, continuation or EH/unwind-body following.',
  'Byte coverage is not proof of complete logical constructor, external dependencies or exception behavior.',
  '64KiB streaming full-file fingerprints are separate from 1005 code bytes and the 12-byte metadata recheck.',
  'dumpbin RANGE bounds requested output; its internal PE metadata I/O is not instrumented.',
  'The installed tool reads Engine.dll as data; neither the target nor its imports are executed.',
  'Same-handle before/after fingerprints and stat checks require stability, not an atomic snapshot.'
 ]};
let fd,stat,outCreated=false,toolAttempted=false;
const failures=[];
const save=(name,data)=>fs.writeFileSync(path.join(out,name),data,{flag:'wx'});
function readFixed(offset,length,kind){
 assert(offset>=0&&length>0&&offset+length<=size);
 if(kind==='pdata-recheck'){assert.equal(offset,pdataOffset);assert.equal(length,12);assert.equal(report.metadata_bytes,0);}
 else{assert.equal(kind,'code');assert.equal(offset,codeOffset);assert.equal(length,1005);assert.equal(report.code_bytes,0);}
 const bytes=Buffer.alloc(length),record={kind,file_offset:offset,requested_bytes:length,read_bytes:0};report.reads.push(record);
 while(record.read_bytes<length){const n=fs.readSync(fd,bytes,record.read_bytes,length-record.read_bytes,offset+record.read_bytes);
  assert(n>0,'Unexpected fixed read EOF');record.read_bytes+=n;
  if(kind==='pdata-recheck')report.metadata_bytes+=n;else report.code_bytes+=n;}
 if(kind==='pdata-recheck')record.raw_hex=bytes.toString('hex');return bytes;
}
try{
 plain(path.dirname(out),true);plain(path.dirname(temp),true);absent(out);absent(temp);
 const proofPath=path.join(root,source.path);plain(proofPath);assert(fs.statSync(proofPath).size<=limits.proof_bytes);
 const proofBytes=fs.readFileSync(proofPath);assert(proofBytes.length<=limits.proof_bytes);assert.equal(hash(proofBytes),source.sha256);
 const proof=JSON.parse(proofBytes.toString('utf8'));
 assert.equal(proof.schema,'grim-dynasty-actor-export-boundary-v1');assert.equal(proof.status,'exact-actor-export-and-boundary-verified');
 assert.equal(proof.original,original);assert.equal(proof.expected_sha256,expected);assert.equal(proof.symbol,'??0Actor@GAME@@QEAA@XZ');
 assert.equal(proof.sha256_before,expected);assert.equal(proof.sha256_after,expected);
 assert(proof.source_stat_stable&&proof.source_hash_preserved);assert.deepEqual(proof.failures,[]);
 assert.equal(proof.export.text,proof.symbol);assert.equal(proof.export.target_rva,hex(begin));
 assert.equal(proof.pe.image_base,hex(base));assert.equal(proof.pe.exception_rva,'0x424000');
 assert.equal(proof.pe.exception_file_offset,4317696);assert.equal(proof.pe.entry_count,12210);
 assert.deepEqual(proof.boundary,{index:entryIndex,begin,end,unwind,raw_hex:rawEntry});
 assert.equal(proof.code_file_offset,codeOffset);assert.equal(proof.code_bytes,end-begin);assert.equal(end-begin,limits.code_bytes);
 const text=proof.pe.sections.filter(s=>s.name==='.text'&&begin>=s.rva&&end-s.rva<=s.raw_size);
 const pdata=proof.pe.sections.filter(s=>s.name==='.pdata'&&pdataRva>=s.rva&&pdataRva+12-s.rva<=s.raw_size);
 assert.equal(text.length,1);assert.equal(pdata.length,1);assert(text[0].characteristics&0x20000000);
 assert.equal(text[0].raw_offset+begin-text[0].rva,codeOffset);assert.equal(pdata[0].raw_offset+pdataRva-pdata[0].rva,pdataOffset);
 assert.equal(4317696+entryIndex*12,pdataOffset);assert.equal(0x424000+entryIndex*12,pdataRva);
 fs.mkdirSync(out);outCreated=true;fs.mkdirSync(temp);plain(out,true);plain(temp,true);
 report.script_sha256=hashFile(script).sha256;
 plain(tool);toolAttempted=true;report.tool_before=hashFile(tool);assert.equal(report.tool_before.sha256,toolHash);
 plain(original);fd=fs.openSync(original,fs.constants.O_RDONLY);stat=fs.fstatSync(fd);assert(stat.isFile()&&stat.size===size);
 const before=fingerprint(fd,size);report.fingerprint_passes.push({label:'before',...before});report.sha256_before=before.sha256;assert.equal(before.sha256,expected);
 const entry=readFixed(pdataOffset,12,'pdata-recheck');assert.equal(entry.toString('hex'),rawEntry);
 assert.equal(entry.readUInt32LE(0),begin);assert.equal(entry.readUInt32LE(4),end);assert.equal(entry.readUInt32LE(8),unwind);
 const bytes=readFixed(codeOffset,end-begin,'code');report.code_sha256=hash(bytes);save('0x1800259a0.bin',bytes);
 const start=base+BigInt(begin),finish=base+BigInt(end);
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(start)},${hex(finish-1n)}`,original];
 report.command_arguments=args;report.subprocesses=1;const started=Date.now();
 const child=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:limits.tool_timeout_ms,maxBuffer:limits.tool_max_buffer_bytes,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 report.tool_elapsed_ms=Date.now()-started;report.tool_exit=child.status;report.tool_signal=child.signal;report.tool_error=child.error?String(child.error):null;
 save('0259a0-dumpbin.txt',child.stdout||'');save('0259a0-stderr.txt',child.stderr||'');
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
 const rows=[];
 for(const line of child.stdout.split(/\r?\n/)){
  const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
  if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
  const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length>0);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}
 }
 let cursor=start;
 for(const row of rows){assert(row.instruction&&row.bytes.length>0&&row.bytes.length%2===0&&row.bytes.length<=30);
  assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=finish);}
 assert.equal(cursor,finish);assert.equal(rows.map(r=>r.bytes).join(''),bytes.toString('hex'));
 save('0259a0-instructions.json',JSON.stringify(rows.map(r=>({...r,address:hex(r.address)})),null,2)+'\n');
 report.instructions=rows.length;report.byte_coverage_verified=true;
 assert.equal(report.code_bytes,1005);assert.equal(report.metadata_bytes,12);assert.equal(report.subprocesses,1);
}catch(error){failures.push(String(error));}
finally{
 if(fd!==undefined){
  try{if(stat&&stat.size===size){const after=fingerprint(fd,size);report.fingerprint_passes.push({label:'after',...after});
   report.sha256_after=after.sha256;assert.equal(after.sha256,expected);report.source_stat_stable=stable(stat,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(error){failures.push('Original final verification: '+String(error));}
  finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(error){failures.push('Original close: '+String(error));}}
 }
 if(toolAttempted){try{report.tool_after=hashFile(tool);assert.equal(report.tool_after.sha256,toolHash);}
  catch(error){failures.push('Tool final verification: '+String(error));}}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;
 report.tool_hash_preserved=report.tool_before?.sha256===toolHash&&report.tool_after?.sha256===toolHash;
 report.status=failures.length?'failed':'fixed-actor-constructor-body-disassembly-verified';report.failures=failures;
 report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
 if(outCreated){plain(out,true);save('inspection.json',JSON.stringify(report,null,2)+'\n');}
}
console.log(JSON.stringify({status:report.status,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,instructions:report.instructions,
 original_unchanged:report.source_hash_preserved,tool_unchanged:report.tool_hash_preserved,subprocesses:report.subprocesses,
 source_handle_closed:report.source_handle_closed,failures}));
if(failures.length)process.exitCode=1;
