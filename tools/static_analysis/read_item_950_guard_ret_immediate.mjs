// Complete only the saved C2 instruction with its two immediate bytes; no neighboring code.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856,pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-950-guard-ret'),temp=path.join(root,'.tooling/offline-item-950-guard-ret-temp');
const proofPath='docs/object-lifecycle/offline-item-950-guard-disk-target/inspection.json',proofPin='1abb838af525a8317ec99e8e7737ad1919800f7554712e10ad5304ac800d580c';
const hash=b=>createHash('sha256').update(b).digest('hex');
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync(c).toLowerCase(),c.toLowerCase());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved');}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,'r');try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(path.join(root,proofPath),true);assert(fs.statSync(path.join(root,proofPath)).size<262144);const saved=fs.readFileSync(path.join(root,proofPath));assert.equal(hash(saved),proofPin);const prior=JSON.parse(saved);
assert.equal(prior.status,'disk-target-first-byte-unresolved');assert(prior.source_closed&&prior.source_hash_preserved);assert.equal(prior.disk_target_va,'0x180015880');assert.equal(prior.reads[1].rva,'0x15880');assert.equal(prior.reads[1].hex,'c2');assert.equal(prior.reads[1].file_offset,85120);
normal(script,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const r={schema:'item-950-guard-ret-immediate-v1',status:'started',source_evidence:{path:proofPath,sha256:proofPin},original,expected_sha256:pin,
 limits:{new_code_bytes:2,data_bytes:0,read_operations:1,dumpbin_calls:1,hash_buffer:65536,tiny_pe_bytes:3072},original_loaded_or_executed:false,loaded_slot_unknown:true,failures:[]};
let fd,initial,created=false;function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
try{
 fs.mkdirSync(out);created=true;fs.mkdirSync(temp);normal(out);normal(temp);r.script_sha256=fingerprint(script);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.equal(initial.size,size);r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 const imm=Buffer.alloc(2),got=fs.readSync(fd,imm,0,2,85121);r.read={rva:'0x15881',file_offset:85121,bytes:got,hex:imm.subarray(0,got).toString('hex')};assert.equal(got,2);
 const code=Buffer.concat([Buffer.from([0xc2]),imm]);save('15880-complete.bin',code);r.immediate=imm.readUInt16LE();
 const b=Buffer.alloc(0xc00),coff=0x84,opt=0x98,sec=0x188;b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(0xa00,opt+4);b.writeUInt32LE(0x15000,opt+20);b.writeBigUInt64LE(0x180000000n,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(0x16000,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
 b.write('.text',sec,'ascii');b.writeUInt32LE(0x883,sec+8);b.writeUInt32LE(0x15000,sec+12);b.writeUInt32LE(0xa00,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,0xa80);
 const tiny=path.join(temp,'15880-entry.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});r.synthetic={bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:0x180015880,0x180015882',tiny];r.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:16384,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save('15880-dumpbin.txt',child.stdout||'');save('15880-stderr.txt',child.stderr||'');r.tool_exit=child.status;r.dumpbin_calls=1;
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),r.synthetic.sha256);
 const rows=(child.stdout||'').split(/\r?\n/).filter(l=>/^\s+[0-9A-F]{16}:/.test(l));assert.equal(rows.length,1);const match=/^\s+0000000180015880:\s+((?:[0-9A-F]{2}\s+){3})(ret\s+.*)$/.exec(rows[0]);assert(match);assert.equal(match[1].replace(/\s/g,'').toLowerCase(),code.toString('hex'));r.instruction=match[2].trim();
 r.status=r.immediate===0?'disk-initial-target-ret-zero-confirmed':'disk-initial-target-ret-nonzero';r.runtime_compatibility_verified=false;
 r.conditions='Disk bytes only. Requires valid call stack and loaded slot/code matching disk initializer to use this normal return; no runtime slot policy, writer or native EH established.';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);r.source_stat_stable=s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}catch(e){r.failures.push('Final: '+e);}finally{fs.closeSync(fd);r.source_closed=true;}}
 try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,read:r.read,instruction:r.instruction,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
