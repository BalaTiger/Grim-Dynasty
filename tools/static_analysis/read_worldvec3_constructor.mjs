// Explicit 48-byte entry candidate, not an inferred whole-function boundary.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',size=4485632,pin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-worldvec3-constructor'),temp=path.join(root,'.tooling/offline-worldvec3-constructor-temp');
const proofPath='docs/object-lifecycle/offline-worldvec3-constructor-boundary/metadata.json',proofPin='030c5dfda0c05127a67e6fb4dd1b77996ff6248f58b0f1a33e3a8f592c467dad';
const start=0x228ab0,length=48,base=0x180000000n,hash=b=>createHash('sha256').update(b).digest('hex');
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync(c).toLowerCase(),c.toLowerCase());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved');}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,'r');try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(path.join(root,proofPath),true);assert(fs.statSync(path.join(root,proofPath)).size<262144);const pb=fs.readFileSync(path.join(root,proofPath));assert.equal(hash(pb),proofPin);const prior=JSON.parse(pb);
assert.equal(prior.status,'exact-export-without-exact-runtime-function');assert.equal(prior.boundary_assessment,'no-exact-begin-gap');assert(prior.source_closed&&prior.source_hash_preserved);assert.equal(prior.sha256_after,pin);assert.equal(prior.export.target_rva,'0x228ab0');assert.equal(prior.export.name,'??0WorldVec3@GAME@@QEAA@XZ');assert.equal(prior.previous.end,0x228a55);assert.equal(prior.next.begin,0x228b20);assert(start+length<=prior.next.begin);
const sections=prior.pe.sections.filter(s=>start>=s.rva&&start-s.rva+length<=s.raw_size);assert.equal(sections.length,1);assert(sections[0].characteristics&0x20000000);const fileOffset=sections[0].raw_offset+start-sections[0].rva;assert(fileOffset>=0&&fileOffset+length<=size);
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const r={schema:'worldvec3-entry-candidate-v1',status:'started',original,expected_sha256:pin,source_evidence:{path:proofPath,sha256:proofPin},
 candidate:{rva:'0x228ab0',end_exclusive:'0x228ae0',bytes:48,exact_pdata_boundary:false,reason:'Explicit small entry window inside adjacent metadata limits; gap is not a whole-function claim'},
 limits:{code_bytes:48,metadata_bytes:0,data_bytes:0,read_operations:1,dumpbin_calls:1,tiny_pe_bytes:3584,hash_buffer:65536},original_loaded_or_executed:false,original_path_given_to_dumpbin:false,targets_followed:0,failures:[]};
let fd,initial,created=false;function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
try{
 fs.mkdirSync(out);created=true;fs.mkdirSync(temp);normal(out);normal(temp);r.script_sha256=fingerprint(self);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.equal(initial.size,size);r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 const code=Buffer.alloc(length),got=fs.readSync(fd,code,0,length,fileOffset);r.read={rva:'0x228ab0',file_offset:fileOffset,bytes:got,raw_hex:code.subarray(0,got).toString('hex')};assert.equal(got,length);save('228ab0-candidate.bin',code);
 const b=Buffer.alloc(0xe00),coff=0x84,opt=0x98,sec=0x188;b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(0xc00,opt+4);b.writeUInt32LE(0x228000,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(0x229000,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
 b.write('.text',sec,'ascii');b.writeUInt32LE(0xae0,sec+8);b.writeUInt32LE(0x228000,sec+12);b.writeUInt32LE(0xc00,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,0xcb0);
 const tiny=path.join(temp,'228ab0-candidate.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});r.synthetic={bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:0x180228ab0,0x180228adf',tiny];r.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:32768,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save('228ab0-dumpbin.txt',child.stdout||'');save('228ab0-stderr.txt',child.stderr||'');r.tool_exit=child.status;
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),r.synthetic.sha256);
 const rows=[];for(const line of (child.stdout||'').split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);if(m){rows.push({address:'0x'+m[1].toLowerCase().replace(/^0+/,''),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
 let cursor=base+BigInt(start);for(const row of rows){assert.equal(BigInt(row.address),cursor);assert(row.instruction&&!/[?]|\(bad\)|invalid/i.test(row.instruction));cursor+=BigInt(row.bytes.length/2);}assert.equal(cursor,base+BigInt(start+length));assert.equal(rows.map(x=>x.bytes).join(''),code.toString('hex'));
 save('228ab0-instructions.json',JSON.stringify(rows,null,2)+'\n');r.instructions=rows.length;r.byte_coverage_verified=true;r.status='fixed-entry-candidate-decoded-manual-review-required';r.calls_or_branches=rows.filter(x=>/^(call|j[a-z]+)\b/.test(x.instruction));r.returns=rows.filter(x=>/^ret\b/.test(x.instruction));
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);r.source_stat_stable=s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}catch(e){r.failures.push('Final: '+e);}finally{fs.closeSync(fd);r.source_closed=true;}}
 try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,code_bytes:r.read?.bytes,instructions:r.instructions,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
