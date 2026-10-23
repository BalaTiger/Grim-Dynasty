// Fixed Item caller and directly evidenced import only. No DLL loading or runtime binding.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856,pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-ba8-caller-import'),temp=path.join(root,'.tooling/offline-item-ba8-caller-import-temp');
const symbol='??0WorldVec3@GAME@@QEAA@XZ',base=0x180000000n;
const sources=[
 ['docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json','7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f'],
 ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
 ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json','227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync(c).toLowerCase(),c.toLowerCase());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved');}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,'r');try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
const proofs=sources.map(([rel,sha])=>{const p=path.join(root,rel);normal(p,true);assert(fs.statSync(p).size<262144);const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
const text=proofs[0].result.structuredContent.normalized_result;
assert(text.includes('0x180323811: NOP')&&text.includes('0x1803238ab: CALL qword ptr [0x180609720]')&&text.includes('0x1803238b8: CALL qword ptr [0x180609720]'));
const cached=proofs[1].normal_imports[3].raw_metadata,descriptor=Buffer.from(cached.descriptor.bytes,'hex'),lookup=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');
assert.equal(descriptor.readUInt32LE(0),0xa42ac0);assert.equal(descriptor.readUInt32LE(12),0xa56aa6);assert.equal(descriptor.readUInt32LE(16),0x609000);
assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(lookup.subarray(228*8,229*8).toString('hex'),'3287a40000000000');assert.equal(0x609000+228*8,0x609720);
const pe=proofs[2].pe;assert.equal(pe.image_base,hex(base));assert(proofs[2].source_hash_preserved);assert.equal(proofs[2].sha256_after,pin);
function map(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);const at=ss[0].raw_offset+rva-ss[0].rva;assert(at>=0&&at<=size-n);return at;}
normal(script,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const r={schema:'item-ba8-caller-and-import-v1',status:'started',original,expected_sha256:pin,sources,
 limits:{code_bytes:173,metadata_bytes:8+2+symbol.length+1,read_operations:3,dumpbin_calls:1,tiny_pe_bytes:3072,hash_buffer:65536},
 cached_binding:{descriptor:cached.descriptor,lookup_index:228,lookup_raw:'3287a40000000000',dll:cached.dll_name_with_nul},reads:[],
 original_loaded_or_executed:false,original_path_given_to_dumpbin:false,callees_followed:0,failures:[]};
let fd,initial,created=false;function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function read(rva,n,kind){const planned=[[0x323811,173],[0x609720,8],[0xa48732,2+symbol.length+1]][r.reads.length];assert(planned);assert.deepEqual([rva,n],planned);const b=Buffer.alloc(n),at=map(rva,n),got=fs.readSync(fd,b,0,n,at);r.reads.push({rva:hex(rva),file_offset:at,bytes:got,kind,hex:b.subarray(0,got).toString('hex')});assert.equal(got,n);return b;}
try{
 fs.mkdirSync(out);created=true;fs.mkdirSync(temp);normal(out);normal(temp);r.script_sha256=fingerprint(script);r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.equal(initial.size,size);r.sha256_before=digest(fd,size);assert.equal(r.sha256_before,pin);
 const code=read(0x323811,173,'caller');save('323811-caller.bin',code);
 const iat=read(0x609720,8,'iat');assert.equal(iat.toString('hex'),'3287a40000000000');const name=read(0xa48732,2+symbol.length+1,'hint-and-name');assert.equal(name.subarray(2).toString('ascii'),symbol+'\0');
 r.binding={iat_rva:'0x609720',dll:'Engine.dll',symbol,hint:name.readUInt16LE(),hint_name_rva:'0xa48732'};
 const b=Buffer.alloc(0xc00),coff=0x84,opt=0x98,sec=0x188;b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
 b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(0xa00,opt+4);b.writeUInt32LE(0x323000,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(0x324000,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
 b.write('.text',sec,'ascii');b.writeUInt32LE(0x8be,sec+8);b.writeUInt32LE(0x323000,sec+12);b.writeUInt32LE(0xa00,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,0xa11);
 const tiny=path.join(temp,'323811-caller.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});r.synthetic={bytes:b.length,sha256:hash(b),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:0x180323811,0x1803238bd',tiny];r.command_arguments=args;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:65536,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save('323811-dumpbin.txt',child.stdout||'');save('323811-stderr.txt',child.stderr||'');r.tool_exit=child.status;
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),r.synthetic.sha256);
 const rows=[];for(const line of (child.stdout||'').split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);if(m){rows.push({address:'0x'+m[1].toLowerCase().replace(/^0+/,''),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(rows.length);rows[rows.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
 let cursor=base+0x323811n;for(const row of rows){assert.equal(BigInt(row.address),cursor);assert(row.instruction&&!/[?]|\(bad\)|invalid/i.test(row.instruction));cursor+=BigInt(row.bytes.length/2);}assert.equal(cursor,base+0x3238ben);assert.equal(rows.map(x=>x.bytes).join(''),code.toString('hex'));
 const calls=rows.filter(x=>/^call\b/.test(x.instruction));assert.equal(calls.length,2);for(const row of calls){const cb=Buffer.from(row.bytes,'hex');assert.equal(cb.subarray(0,2).toString('hex'),'ff15');assert.equal(cb.length,6);assert.equal(BigInt(row.address)+6n+BigInt(cb.readInt32LE(2)),base+0x609720n);}r.calls=calls;
 save('323811-instructions.json',JSON.stringify(rows,null,2)+'\n');r.instructions=rows.length;r.byte_coverage_verified=true;r.status='fixed-caller-and-engine-import-verified';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 if(fd!==undefined){try{r.sha256_after=digest(fd,size);assert.equal(r.sha256_after,pin);const s=fs.fstatSync(fd);r.source_stat_stable=s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(r.source_stat_stable);}catch(e){r.failures.push('Final: '+e);}finally{fs.closeSync(fd);r.source_closed=true;}}
 try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}r.source_hash_preserved=r.sha256_before===pin&&r.sha256_after===pin;if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,binding:r.binding,code_bytes:173,metadata_bytes:r.limits.metadata_bytes,instructions:r.instructions,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
