// Fixed direct Game slot and at most one byte at its disk target. No loaded-state inference.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856;
const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',base=0x180000000n;
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-950-guard-disk-target'),temp=path.join(root,'.tooling/offline-item-950-guard-disk-target-temp');
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){let s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync(c).toLowerCase(),c.toLowerCase());let parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing evidence preserved');}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){let got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);let fd=fs.openSync(p,'r');try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
const proofs=[
 ['docs/object-lifecycle/offline-item-950-pre-call-slot/inspection.json','0ec0004ed71bd4fe285eba606335946ca70aa516d4adbd478cd79d61a27fe8bb'],
 ['docs/object-lifecycle/offline-item-950-pre-call-slot/5db8b8-complete.bin','2d30e891ac5d78e2fdf3ea6ec095ff186c7f80de7b51c82b63b9c4cf138156b4'],
 ['docs/object-lifecycle/offline-item-950-pre-call-slot/5db8b8-dumpbin.txt','8e00d1306325e14cf71446a3503ec4abf5c68c2e9920ef60da5c0d8e2993d27a'],
 ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json','227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b']
];
const proofBuffers=proofs.map(([rel,pin])=>{const p=path.join(root,rel);normal(p,true);assert(fs.statSync(p).size<262144);const b=fs.readFileSync(p);assert.equal(hash(b),pin);return b;});
const old=JSON.parse(proofBuffers[0]),code=proofBuffers[1],dump=proofBuffers[2].toString('utf8'),pe=JSON.parse(proofBuffers[3]).pe;
assert(old.source_closed&&old.source_hash_preserved&&old.status==='failed');assert.equal(old.reads.length,1);assert.equal(old.reads[0].hex,'00');
assert.equal(code.toString('hex'),'48ff25e9010300');assert.equal(0x5db8b8+7+code.readInt32LE(3),0x60baa8);
// Offline parser repair: the already saved dumpbin wraps byte7 onto the following line.
assert(/00000001805DB8B8:\s+48 FF 25 E9 01 03\s+jmp\s+qword ptr \[000000018060BAA8h\]\r?\n\s+00\r?\n/.test(dump));
function map(rva,n,executable=false){const ss=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);if(executable)assert(ss[0].characteristics&0x20000000);const at=ss[0].raw_offset+rva-ss[0].rva;assert(at>=0&&at<=size-n);return at;}
normal(script,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const r={schema:'item-950-guard-disk-target-v1',status:'started',original,expected_sha256:expected,source_evidence:proofs,
 offline_repair:'Saved seven-byte REX.W FF25 thunk verified including dumpbin continuation byte; no repeated original code read',
 limits:{data_bytes:8,code_bytes:1,read_operations:2,dumpbin_calls:1,hash_buffer:65536,tiny_pe_max:4608},reads:[],
 original_loaded_or_executed:false,original_path_given_to_dumpbin:false,loaded_slot_unknown:true,failures:[]};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,executable){assert(r.reads.length<2);assert(r.reads.length===0?rva===0x60baa8&&n===8:rva===r.disk_target_rva&&n===1);const b=Buffer.alloc(n),at=map(rva,n,executable),got=fs.readSync(fd,b,0,n,at);r.reads.push({rva:hex(rva),bytes:got,file_offset:at,hex:b.subarray(0,got).toString('hex')});assert.equal(got,n);return b;}
try{
 fs.mkdirSync(out);created=true;fs.mkdirSync(temp);normal(out);normal(temp);r.script_sha256=fingerprint(script);
 r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.equal(initial.size,size);
 r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,expected);
 const slot=read(0x60baa8,8,false);save('60baa8-disk-slot.bin',slot);const va=slot.readBigUInt64LE();r.disk_target_va=hex(va);assert(va>=base&&va<base+BigInt(pe.image_size));r.disk_target_rva=Number(va-base);
 const first=read(r.disk_target_rva,1,true);save('disk-target-first-byte.bin',first);
 if(first[0]!==0xc3){r.status='disk-target-first-byte-unresolved';r.normal_return_established=false;}
 else{
  const start=r.disk_target_rva,sectionRva=Math.floor(start/4096)*4096,prefix=start-sectionRva,rawSize=Math.ceil((prefix+1)/512)*512,b=Buffer.alloc(512+rawSize),coff=0x84,opt=0x98,sec=0x188;
  assert(b.length<=4608);b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
  b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawSize,opt+4);b.writeUInt32LE(sectionRva,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(sectionRva+4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
  b.write('.text',sec,'ascii');b.writeUInt32LE(prefix+1,sec+8);b.writeUInt32LE(sectionRva,sec+12);b.writeUInt32LE(rawSize,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);b[512+prefix]=first[0];
  const tiny=path.join(temp,'disk-target-entry.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});r.synthetic={bytes:b.length,sha256:hash(b),never_executed:true};
  const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(va)+','+hex(va),tiny];r.command_arguments=args;
  const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:16384,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
  save('disk-target-dumpbin.txt',child.stdout||'');save('disk-target-stderr.txt',child.stderr||'');r.dumpbin_calls=1;r.tool_exit=child.status;
  assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),r.synthetic.sha256);
  const rows=(child.stdout||'').split(/\r?\n/).filter(line=>/^\s+[0-9A-F]{16}:/.test(line));assert.equal(rows.length,1);const m=/^\s+([0-9A-F]{16}):\s+C3\s+ret\s*$/.exec(rows[0]);assert(m);assert.equal(BigInt('0x'+m[1]),va);
  r.status='disk-slot-target-is-one-byte-ret';r.normal_return_established='Disk initializer only; valid call stack and unchanged loaded slot/code are conditions';
 }
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,expected);const s=fs.fstatSync(fd);r.source_stat_stable=s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}catch(e){r.failures.push('Final: '+e);}finally{fs.closeSync(fd);r.source_closed=true;}}
 try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}r.source_hash_preserved=r.sha256_before===expected&&r.sha256_after===expected;
 if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,reads:r.reads,disk_target_va:r.disk_target_va,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
