// Narrow continuation of a documented truncated instruction. No execution of original code.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856;
const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-950-pre-call-slot');
const temp=path.join(root,'.tooling/offline-item-950-pre-call-slot-temp');
const base=0x180000000n,entry=0x5db8b8;
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync(c).toLowerCase(),c.toLowerCase());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved');}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let p=0;p<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-p),p);assert(got>0);h.update(b.subarray(0,got));p+=got;}return h.digest('hex');}
function fileHash(p){normal(p,true);const fd=fs.openSync(p,'r');try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
function proof(rel,pin){const p=path.join(root,rel);normal(p,true);assert(fs.statSync(p).size<=262144);const b=fs.readFileSync(p);assert.equal(hash(b),pin);return b;}
const proofs=[
 ['docs/object-lifecycle/offline-item-950-pre-call-dependency/inspection.json','7df390c89f9fb8f52af794d2337106f58f847e4a59b239159bd4e77f3e264709'],
 ['docs/object-lifecycle/offline-item-950-pre-call-dependency/5db8b8-entry-prefix.bin','9debe4e4686e95c47eba81ad5fcea16e98b8dfbd9770b91dbf0b92ce260aaeab'],
 ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json','227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b']
];
const old=JSON.parse(proof(...proofs[0])),prefix=proof(...proofs[1]),pe=JSON.parse(proof(...proofs[2])).pe;
assert.equal(old.status,'failed');assert(old.source_closed&&old.source_hash_preserved);assert.equal(old.sha256_after,expected);
assert.equal(prefix.toString('hex'),'48ff25e90103');assert.equal(old.code_window_bytes,6);
assert.equal(pe.image_base,hex(base));assert.equal(pe.image_size,11427840);
function offset(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&n>0);const ss=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);const at=ss[0].raw_offset+rva-ss[0].rva;assert(at>=0&&at<=size-n);return at;}
normal(script,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const r={schema:'item-950-pre-call-slot-v1',status:'started',original,expected_sha256:expected,source_evidence:proofs,
 limits:{new_code_bytes:1,new_data_bytes:8,read_operations:2,hash_buffer:65536,dumpbin_calls:1,tiny_pe_bytes:3072},
 reads:[],original_loaded_or_executed:false,original_path_given_to_dumpbin:false,target_followed:false,
 metadata_bytes:0,new_code_bytes:0,new_data_bytes:0,subprocesses:0,failures:[]};
let fd,beforeStat,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,kind){assert(r.reads.length<2);assert((kind==='code'&&rva===0x5db8be&&n===1)||(kind==='data'&&rva===r.slot_rva&&n===8));const b=Buffer.alloc(n),at=offset(rva,n);const got=fs.readSync(fd,b,0,n,at);r.reads.push({rva:hex(rva),bytes:got,requested:n,file_offset:at,kind,hex:b.subarray(0,got).toString('hex')});assert.equal(got,n);r[kind==='code'?'new_code_bytes':'new_data_bytes']+=n;return b;}
try{
 fs.mkdirSync(out);created=true;fs.mkdirSync(temp);normal(out);normal(temp);
 r.script_sha256=fileHash(script);r.tool_before=fileHash(tool);assert.equal(r.tool_before,toolHash);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);beforeStat=fs.fstatSync(fd);assert.equal(beforeStat.size,size);
 r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,expected);
 const last=read(0x5db8be,1,'code'),code=Buffer.concat([prefix,last]);save('5db8b8-complete.bin',code);
 assert.equal(code.subarray(0,3).toString('hex'),'48ff25');
 // Generated one-section PE; only seven archived/read bytes copied. Never executed.
 const sectionRva=0x5db000,codePrefix=entry-sectionRva,rawSize=0xa00,b=Buffer.alloc(0xc00),coff=0x84,opt=0x98,sec=0x188;
 b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawSize,opt+4);b.writeUInt32LE(sectionRva,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(0x1000,opt+32);b.writeUInt32LE(0x200,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(0x5dc000,opt+56);b.writeUInt32LE(0x200,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
 b.write('.text',sec,'ascii');b.writeUInt32LE(codePrefix+7,sec+8);b.writeUInt32LE(sectionRva,sec+12);b.writeUInt32LE(rawSize,sec+16);b.writeUInt32LE(0x200,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,0x200+codePrefix);
 const tiny=path.join(temp,'5db8b8-complete.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});normal(tiny,true);r.synthetic={bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:0x1805db8b8,0x1805db8be',tiny];r.command_arguments=args;r.subprocesses++;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:32768,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save('5db8b8-dumpbin.txt',child.stdout||'');save('5db8b8-stderr.txt',child.stderr||'');r.tool_exit=child.status;
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fileHash(tiny),r.synthetic.sha256);
 const rows=(child.stdout||'').split(/\r?\n/).map(line=>/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}\s+)+)(.*)$/.exec(line)).filter(Boolean);
 assert.equal(rows.length,1);assert.equal(BigInt('0x'+rows[0][1]),base+BigInt(entry));assert.equal(rows[0][2].replace(/\s/g,'').toLowerCase(),code.toString('hex'));assert(/^jmp\s/.test(rows[0][3]));
 r.instruction={address:hex(base+BigInt(entry)),bytes:code.toString('hex'),instruction:rows[0][3].trim()};
 const slot=entry+7+code.readInt32LE(3);assert(slot>=0&&slot<=pe.image_size-8);r.slot_rva=slot;r.slot_va=hex(base+BigInt(slot));
 const data=read(slot,8,'data');save('direct-slot.bin',data);r.disk_target_va=hex(data.readBigUInt64LE());r.disk_target_rva=hex(data.readBigUInt64LE()-base);
 r.loaded_slot_value_unknown=true;r.disk_target_body_unknown=true;r.status='instruction-and-disk-slot-read-target-not-followed';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,expected);const s=fs.fstatSync(fd);r.source_stat_stable=s.size===beforeStat.size&&s.ino===beforeStat.ino&&s.dev===beforeStat.dev&&s.mtimeMs===beforeStat.mtimeMs&&s.ctimeMs===beforeStat.ctimeMs;assert(r.source_stat_stable);}catch(e){r.failures.push('Final hash: '+e);}finally{fs.closeSync(fd);r.source_closed=true;}}
 try{r.tool_after=fileHash(tool);assert.equal(r.tool_after,toolHash);}catch(e){r.failures.push('Tool final: '+e);}
 r.source_hash_preserved=r.sha256_before===expected&&r.sha256_after===expected;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,reads:r.reads,slot:r.slot_va,disk_target:r.disk_target_va,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));
if(r.failures.length)process.exitCode=1;
