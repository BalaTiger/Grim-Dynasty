// Fixed read-only inspection of archive decode helper called at 280F0F.
// No target execution, callee discovery, installation or backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const copy=path.join(root,'.tooling/offline-cache-node/Engine.readonly.dll');
const expected='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const caller='docs/object-lifecycle/offline-archive-fetch-continuation/280e20-continuation-instructions.json';
const callerHash='f02600514aa9ab1adbfe3a5cf2bf70eb121e1d749b692c50ba14986533af44e1';
const out=path.join(root,'docs/object-lifecycle/offline-archive-decode-inspection');
const temp=path.join(root,'.tooling/offline-archive-decode-temp');
const hash=b=>createHash('sha256').update(b).digest('hex');
function hashFile(p){
 const fd=fs.openSync(p,'r'),buf=Buffer.alloc(65536),h=createHash('sha256');
 try{let n;while((n=fs.readSync(fd,buf,0,buf.length,null))>0)h.update(buf.subarray(0,n));}
 finally{fs.closeSync(fd);}return h.digest('hex');
}
const json=(name,data)=>fs.writeFileSync(path.join(out,name),JSON.stringify(data,null,2)+'\n',{flag:'wx'});
assert(!fs.existsSync(out),'Preserve prior inspection');
assert.equal(hashFile(path.join(root,caller)),callerHash);
assert.equal(hashFile(tool),toolHash);
assert.equal(hashFile(copy),expected,'Existing working copy changed');
fs.mkdirSync(out,{recursive:true});fs.mkdirSync(temp,{recursive:true});
const record={started_utc:new Date().toISOString(),status:'started',original,working_copy:copy,
 requested_va:'0x180280d20',code_byte_limit:4096,callee_bodies_followed:0,
 target_loaded_or_executed:false,backend_started:false,original_modified:false,
 original_hash_scope:'Streaming full-file SHA256 before and after, 64KiB chunks; code decoding limited to one exact function',
 source_evidence:{path:caller,sha256:callerHash},tool,tool_sha256:toolHash};
let failure=null,fd;
try{
 record.original_sha256_before=hashFile(original);assert.equal(record.original_sha256_before,expected);
 fd=fs.openSync(original,'r');const size=fs.fstatSync(fd).size;assert(size>0&&size<=64*1024*1024);
 let metadataBytes=0;
 function read(offset,length,metadata=true){
  assert(Number.isInteger(offset)&&Number.isInteger(length)&&offset>=0&&length>0&&offset+length<=size);
  const b=Buffer.alloc(length);assert.equal(fs.readSync(fd,b,0,length,offset),length);
  if(metadata)metadataBytes+=length;return b;
 }
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
 const pe=dos.readUInt32LE(60);assert(pe<=65536);
 const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const n=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);assert(n>0&&n<=96&&optSize>=144&&optSize<=4096);
 const optional=read(pe+24,optSize);assert.equal(optional.readUInt16LE(0),0x20b);
 const imageBase=optional.readBigUInt64LE(24);assert.equal(imageBase,0x180000000n);
 const sections=Array.from({length:n},(_,i)=>{const b=read(pe+24+optSize+i*40,40);return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 function map(rva,length,code=false){
  const matches=sections.filter(s=>rva>=s.rva&&rva+length<=s.rva+s.size);
  assert.equal(matches.length,1);if(code)assert(matches[0].flags&0x20000000);
  return matches[0].raw+rva-matches[0].rva;
 }
 assert(optional.readUInt32LE(108)>3);
 const pdata=optional.readUInt32LE(136),pdataSize=optional.readUInt32LE(140);
 assert(pdataSize>0&&pdataSize%12===0&&pdataSize<=2*1024*1024);
 const pdataOffset=map(pdata,pdataSize),rva=0x280d20;
 let lo=0,hi=pdataSize/12-1,boundary=null,indexReads=0;
 while(lo<=hi){
  const index=(lo+hi)>>1,b=read(pdataOffset+12*index,12);indexReads++;
  const begin=b.readUInt32LE(0),end=b.readUInt32LE(4);assert(begin<end);
  if(rva<begin)hi=index-1;else if(rva>=end)lo=index+1;
  else{boundary={begin_rva:begin,end_rva:end,unwind_rva:b.readUInt32LE(8),index};break;}
 }
 record.boundary=boundary;record.exception_index_reads=indexReads;
 assert(boundary&&boundary.begin_rva===rva,'No exact runtime-function boundary');
 const length=boundary.end_rva-rva;assert(length>0&&length<=4096,'Known function exceeds fixed budget');
 const bytes=read(map(rva,length,true),length,false),start=imageBase+BigInt(rva),end=imageBase+BigInt(boundary.end_rva);
 fs.writeFileSync(path.join(out,'0x180280d20.bin'),bytes,{flag:'wx'});
 record.code_bytes=length;record.metadata_bytes=metadataBytes;record.code_sha256=hash(bytes);
 record.end_va_exclusive='0x'+end.toString(16);
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:0x${start.toString(16)},0x${(end-1n).toString(16)}`,copy];
 record.command_arguments=args;
 const began=Date.now(),r=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:196608,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 record.tool_elapsed_ms=Date.now()-began;record.tool_exit=r.status;
 fs.writeFileSync(path.join(out,'280d20-dumpbin.txt'),r.stdout??'',{flag:'wx'});
 fs.writeFileSync(path.join(out,'280d20-stderr.txt'),r.stderr??'',{flag:'wx'});
 assert.equal(r.error,undefined);assert.equal(r.status,0);assert.equal(r.stderr,'');
 const rows=[];
 for(const line of r.stdout.split(/\r?\n/)){
  const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
  if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
  const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
  if(c){assert(rows.length);rows.at(-1).bytes+=c[1].replace(/\s/g,'').toLowerCase();}
 }
 let cursor=start;assert(rows.length);
 for(const row of rows){assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=end);}
 assert.equal(cursor,end);assert.equal(rows.map(r=>r.bytes).join(''),bytes.toString('hex'));
 json('280d20-instructions.json',rows.map(r=>({...r,address:'0x'+r.address.toString(16)})));
 record.instructions=rows.length;record.disassembly_sha256=hash(Buffer.from(r.stdout));
 record.byte_coverage_verified=true;record.status='fixed-function-disassembly-verified';
}catch(e){failure=e;record.status='failed';record.failure=String(e);}
finally{
 if(fd!==undefined)fs.closeSync(fd);
 record.original_sha256_after=hashFile(original);
 record.copy_sha256_after=hashFile(copy);
 record.original_hash_matches=record.original_sha256_after===expected;
 record.copy_hash_matches=record.copy_sha256_after===expected;
 record.finished_utc=new Date().toISOString();record.controller_resource_usage=process.resourceUsage();
 json('inspection.json',record);
}
assert(record.original_hash_matches&&record.copy_hash_matches);
if(failure)throw failure;
console.log(JSON.stringify({status:record.status,bytes:record.code_bytes,instructions:record.instructions,
 end:record.end_va_exclusive,tool_elapsed_ms:record.tool_elapsed_ms,original_unchanged:true,copy_unchanged:true,
 controller_peak_rss_kib:record.controller_resource_usage.maxRSS}));
