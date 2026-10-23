// One explicitly selected direct callee of the newly verified Item +858 initializer.
// Read-only Game data and installed dumpbin only; no further call following.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const root='C:/Users/zhuzi/Documents/Grim-Dynasty', original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
assert.equal(path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..').toLowerCase(),path.resolve(root).toLowerCase());
const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2', size=11385856;
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-858-zero-helper'),temp=path.join(root,'.tooling/offline-item-858-zero-helper-temp');
const sources=[
 ['docs/object-lifecycle/offline-item-factory-dependency-boundaries/metadata.json','59265ca3af388f48af8da9a66bee439949fa8cfeca5fe1cc40a066998ad46d29'],
 ['docs/object-lifecycle/offline-item-factory-dependencies/1fa340-instructions.json','04a0fd839546fe2d39d7605ee7c1e4232df135e3065624c96c3f95aafc2784fd'],
 ['docs/object-lifecycle/offline-item-factory-dependencies/inspection.json','f85d6463ff3cb17e170981d3bf6daa4fcd85ba87081c04d9f7b92fdeadf46770']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function plain(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());const d=path.dirname(c);if(d===c)return;c=d;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Preserve existing output');}
function fingerprint(fd,length){const b=Buffer.alloc(65536),h=createHash('sha256');let at=0;while(at<length){const n=fs.readSync(fd,b,0,Math.min(b.length,length-at),at);assert(n>0);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
function hashFile(p){const fd=fs.openSync(p,'r');try{return fingerprint(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
plain(path.dirname(out));plain(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([p,h])=>{const full=path.join(root,p);plain(full,true);assert(fs.statSync(full).size<131072);const b=fs.readFileSync(full);assert.equal(hash(b),h);return JSON.parse(b);});
const pe=proofs[0].pe;
assert.equal(proofs[0].sha256_before,expected);assert.equal(proofs[0].sha256_after,expected);assert(proofs[0].source_hash_preserved);
assert.equal(proofs[2].status,'fixed-item-factory-dependency-disassembly-verified');assert(proofs[2].original_hash_matches);
assert(proofs[1].some(i=>i.address==='0x1801fa348'&&i.bytes==='33d2'));
assert(proofs[1].some(i=>i.address==='0x1801fa358'&&i.bytes==='e893f8e2ff'&&i.instruction==='call        0000000180029BF0'));
assert.equal(pe.image_base,'0x180000000');assert.equal(pe.entry_count,22293);
const target=0x29bf0,base=0x180000000n;
const report={schema:'grim-dynasty-item-858-fixed-zero-callee-v1',started_utc:new Date().toISOString(),status:'started',original,expected_sha256:expected,
 sources:sources.map(([path,sha256])=>({path,sha256})),requested_rva:hex(target),target_open_mode:'O_RDONLY',target_loaded_or_executed:false,
 tool,expected_tool_sha256:toolHash,code_limit:512,metadata_limit:384,search_step_limit:32,metadata_bytes:0,code_bytes:0,reads:[],subprocesses:0,
 limitations:['Only exact .pdata entry can authorize this one body; if absent or longer than 512 B, preserve boundary and stop without disassembly.',
 'No target, callee, import or game execution; no further callee traversal. No full exception table scan.',
 'Same-handle before/after full-file hashes use 64KiB buffers, separate from semantic reads. dumpbin internal metadata I/O is not measured.',
 'Byte coverage and .pdata do not establish all logical function/EH behavior. Before/after stable hash/stat is not an atomic snapshot.']};
fs.mkdirSync(out);fs.mkdirSync(temp);
const save=(n,v)=>fs.writeFileSync(path.join(out,n),v,{flag:'wx'});
let fd,stat;const failures=[];
function read(at,n,kind){assert(Number.isSafeInteger(at)&&at>=0&&Number.isSafeInteger(n)&&n>0&&at+n<=size);
 if(kind==='pdata'){assert.equal(n,12);assert(report.metadata_bytes+n<=384);report.metadata_bytes+=n;}else{assert.equal(kind,'code');assert(n<=512);assert.equal(report.code_bytes,0);report.code_bytes=n;}
 const b=Buffer.alloc(n);let done=0;while(done<n){const k=fs.readSync(fd,b,done,n-done,at+done);assert(k>0);done+=k;}
 report.reads.push({kind,file_offset:at,bytes:n,sha256:hash(b)});return b;
}
try{
 plain(original,true);plain(tool,true);report.tool_before=hashFile(tool);assert.equal(report.tool_before,toolHash);
 fd=fs.openSync(original,'r');stat=fs.fstatSync(fd);assert.equal(stat.size,size);
 report.sha256_before=fingerprint(fd,size);assert.equal(report.sha256_before,expected);
 function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva-s.rva+n<=s.raw_size);assert.equal(ss.length,1);return {section:ss[0],offset:ss[0].raw_offset+rva-ss[0].rva};}
 const pd=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pd.section.name,'.pdata');assert.equal(pd.offset,pe.exception_file_offset);
 const cache=new Map();
 function entry(i){assert(Number.isInteger(i)&&i>=0&&i<pe.entry_count);if(cache.has(i))return cache.get(i);
 const b=read(pd.offset+i*12,12,'pdata'),e={index:i,begin:b.readUInt32LE(0),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8),raw_hex:b.toString('hex')};
 assert(e.begin<e.end&&e.end<=pe.image_size);for(const [j,o]of cache)assert(j<i?o.begin<=e.begin:o.begin>=e.begin);cache.set(i,e);return e;}
 let lo=0,hi=pe.entry_count,steps=0;while(lo<hi){assert(steps++<32);const mid=lo+Math.floor((hi-lo)/2),e=entry(mid);if(e.begin<target)lo=mid+1;else hi=mid;}
 report.search_steps=steps;report.lower_bound_index=lo;report.entries=[...cache.values()];
 const e=lo<pe.entry_count?entry(lo):null;report.boundary=e;
 if(!e||e.begin!==target){report.status='no-exact-entry-body-not-read';}
 else if(e.end-e.begin>512){report.status='exact-boundary-exceeds-body-budget';}
 else{
 const mapped=map(e.begin,e.end-e.begin);assert(mapped.section.characteristics&0x20000000);
 report.code_file_offset=mapped.offset;const bytes=read(mapped.offset,e.end-e.begin,'code');save('029bf0.bin',bytes);
 const start=base+BigInt(e.begin),end=base+BigInt(e.end),args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(start)+','+hex(end-1n),original];
 report.command_arguments=args;report.subprocesses++;
 const r=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:196608,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 report.tool_exit=r.status;report.tool_error=r.error?String(r.error):null;save('029bf0-dumpbin.txt',r.stdout||'');save('029bf0-stderr.txt',r.stderr||'');
 assert.equal(r.error,undefined);assert.equal(r.status,0);assert.equal(r.signal,null);assert.equal(r.stderr,'');
 const rows=[];for(const line of r.stdout.split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
 if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
 const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows.at(-1).bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
 let cursor=start;for(const row of rows){assert(row.instruction&&row.bytes.length>0&&row.bytes.length<=30&&row.bytes.length%2===0);assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=end);}
 assert.equal(cursor,end);assert.equal(rows.map(r=>r.bytes).join(''),bytes.toString('hex'));report.byte_coverage_verified=true;report.instructions=rows.length;
 save('029bf0-instructions.json',JSON.stringify(rows.map(r=>({...r,address:hex(r.address)})),null,2)+'\n');report.status='fixed-direct-callee-disassembly-verified';
 }
 report.entries=[...cache.values()];
}catch(error){failures.push(String(error));report.status='failed';}
finally{
 if(fd!==undefined){try{if(stat&&stat.size===size){report.sha256_after=fingerprint(fd,size);assert.equal(report.sha256_after,expected);const end=fs.fstatSync(fd);
 report.source_stat_stable=end.size===stat.size&&end.ino===stat.ino&&end.dev===stat.dev&&end.mtimeMs===stat.mtimeMs&&end.ctimeMs===stat.ctimeMs;assert(report.source_stat_stable);}}
 catch(e){failures.push(String(e));}finally{try{fs.closeSync(fd);report.source_handle_closed=true;}catch(e){failures.push(String(e));}}}
 try{report.tool_after=hashFile(tool);assert.equal(report.tool_after,toolHash);}catch(e){failures.push(String(e));}
 report.source_hash_preserved=report.sha256_before===expected&&report.sha256_after===expected;report.tool_hash_preserved=report.tool_before===toolHash&&report.tool_after===toolHash;
 if(failures.length)report.status='failed';report.failures=failures;report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
 plain(out);save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,boundary:report.boundary,metadata_bytes:report.metadata_bytes,code_bytes:report.code_bytes,instructions:report.instructions,original_unchanged:report.source_hash_preserved,tool_unchanged:report.tool_hash_preserved,controller_peak_rss_kib:report.controller_resource_usage.maxRSS}));
if(failures.length)process.exitCode=1;
