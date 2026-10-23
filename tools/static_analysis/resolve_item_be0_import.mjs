// Fixed cached-body reuse plus one Game caller window and exact import/export binding.
// Prepare only; execution requires separate review. No original module is loaded.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const gamePath='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',enginePath='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const gamePin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',enginePin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe',toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out=path.join(root,'docs/object-lifecycle/offline-item-be0-import'),temp=path.join(root,'.tooling/offline-item-be0-import-temp');
const symbol='??0HookPack@GAME@@QEAA@I@Z',base=0x180000000n;
const sources=[
 ['docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json','7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f'],
 ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
 ['docs/object-lifecycle/offline-class-registration-exports/discovery.json','0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
 ['docs/object-lifecycle/offline-actor-198-constructor/inspection.json','36958aecbb0e7905f3d002d68b0f77b48230271194935f73724a40cac11bdc54'],
 ['docs/object-lifecycle/offline-actor-198-constructor/0ebde0-instructions.json','9083e6b9037ded4e5ee7224b5544ac2b9abae3b6a1ecca612e9d7663b8f484ce']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());assert(first&&file?s.isFile():s.isDirectory());assert.equal(fs.realpathSync.native(c).toLowerCase(),c.toLowerCase());const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,fs.constants.O_RDONLY);try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
normal(script,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
fs.mkdirSync(out);fs.mkdirSync(temp);normal(out);normal(temp);
const r={schema:'item-be0-caller-import-export-v1',status:'started',started_utc:new Date().toISOString(),sources:sources.map(([path,sha256])=>({path,sha256})),
 limits:{game_code_bytes:176,engine_code_bytes:0,metadata_bytes:22+2*symbol.length,read_operations:7,dumpbin_calls:1,tiny_pe_bytes:3072,hash_buffer:65536},
 original_loaded_or_executed:false,original_path_given_to_dumpbin:false,callees_followed:0,pdata_reread:false,reads:[],metadata_bytes:0,failures:[],
 restrictions:['Only Game[3238BE,32396E) code; next call323968 is recorded but not followed.',
  'Game descriptor/OFT/module raw bytes and Engine PE/export metadata are reused from pinned evidence.',
  'Only Game IAT609980 and hint/nameA496D8, then that exact Engine hint name/ordinal/EAT are new metadata.',
  'Hint mismatch or target other than EBDE0 stops; no fallback scan, new .pdata, Engine code, unwind or callee reads.',
  'No complete constructor, loaded IAT behavior, runtime guard, allocation EH or cleanup claim. Full hashes are identity checks, not semantic reads.']};
let game,engine;
function save(name,bytes){normal(out);fs.writeFileSync(path.join(out,name),bytes,{flag:'wx'});}
function map(module,rva,n){const ss=module.pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);const offset=ss[0].raw_offset+rva-ss[0].rva;assert(offset>=0&&offset<=module.size-n);return {offset,section:ss[0]};}
function read(module,fd,rva,n,kind,code=false){
 const m=map(module,rva,n);assert(code?!!(m.section.characteristics&0x20000000):!(m.section.characteristics&0x20000000));
 assert(r.reads.length<r.limits.read_operations);if(!code){assert(r.metadata_bytes+n<=r.limits.metadata_bytes);r.metadata_bytes+=n;}
 const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,m.offset);r.reads.push({module:module.name,rva:hex(rva),file_offset:m.offset,kind,bytes:got,hex:b.subarray(0,got).toString('hex')});assert.equal(got,n);return b;
}
function examine(module,action){
 let fd,initial;
 try{normal(module.path,true);fd=fs.openSync(module.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,module.size);module.sha256_before=digest(fd,module.size);assert.equal(module.sha256_before,module.expected_sha256);return action(fd);}
 finally{if(fd!==undefined){
  try{if(initial&&initial.size===module.size){module.sha256_after=digest(fd,module.size);assert.equal(module.sha256_after,module.expected_sha256);const s=fs.fstatSync(fd);module.source_stat_stable=s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(module.source_stat_stable);}}catch(e){r.failures.push(module.name+' final: '+e);}
  finally{try{fs.closeSync(fd);module.source_closed=true;}catch(e){r.failures.push(module.name+' close: '+e);}}
  module.source_hash_preserved=module.sha256_before===module.expected_sha256&&module.sha256_after===module.expected_sha256;
 }}
}
try{
 r.script_sha256=fingerprint(script);
 const proofs=sources.map(([rel,sha])=>{const p=path.join(root,rel);normal(p,true);assert(fs.statSync(p).size<524288);const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
 const assembly=proofs[0].result.structuredContent.normalized_result;
 assert(assembly.includes('0x1803238da: CALL qword ptr [0x180609980]')&&assembly.includes('0x180323968: CALL qword ptr [0x180609128]'));
 const cached=proofs[1].normal_imports[3].raw_metadata,descriptor=Buffer.from(cached.descriptor.bytes,'hex'),lookup=Buffer.from(cached.ownership_lookup_prefix.bytes,'hex');
 assert.equal(proofs[1].status,'fixed-seven-questitem-imports-confirmed');assert.equal(proofs[1].original,gamePath);
 assert.equal(descriptor.length,20);assert.equal(descriptor.readUInt32LE(0),0xa42ac0);assert.equal(descriptor.readUInt32LE(12),0xa56aa6);assert.equal(descriptor.readUInt32LE(16),0x609000);
 assert.equal(cached.ownership_lookup_prefix.rva,'0xa42ac0');assert(cached.ownership_lookup_prefix.entries>304);
 assert.equal(lookup.subarray(304*8,305*8).toString('hex'),'d896a40000000000');assert.equal(cached.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(0x609000+304*8,0x609980);
 r.cached_binding={descriptor:cached.descriptor,lookup_index:304,lookup_raw:'d896a40000000000',dll:cached.dll_name_with_nul};
 function module(name,p,size,pin){const proof=proofs[2].files.find(f=>f.target===p);assert(proof&&proof.source_hash_preserved&&proof.source_stat_stable);assert.equal(proof.sha256_before,pin);assert.equal(proof.sha256_after,pin);assert.equal(proof.expected_file_bytes,size);assert.equal(proof.preferred_image_base,hex(base));return {name,path:p,size,expected_sha256:pin,pe:proof.pe,source_closed:false};}
 game=module('Game',gamePath,11385856,gamePin);engine=module('Engine',enginePath,4485632,enginePin);r.files=[game,engine];
 const old=proofs[3],rows=proofs[4];assert.equal(old.status,'fixed-actor-198-constructor-disassembly-verified');assert(old.source_hash_preserved&&old.source_handle_closed&&old.source_stat_stable&&old.byte_coverage_verified);
 assert.equal(old.sha256_before,enginePin);assert.equal(old.sha256_after,enginePin);assert.equal(old.boundary.begin,0xebde0);assert.equal(old.boundary.end,0xebea5);assert.equal(old.code_bytes,197);assert.equal(rows.length,53);
 let cursor=base+0xebde0n;for(const row of rows){assert.equal(BigInt(row.address),cursor);cursor+=BigInt(row.bytes.length/2);}assert.equal(cursor,base+0xebea5n);const raw=Buffer.from(rows.map(x=>x.bytes).join(''),'hex');assert.equal(hash(raw),old.code_sha256);
 r.reused_engine_body={source_index:3,instructions_source_index:4,boundary:old.boundary,code_bytes:197,instructions:53,sha256:old.code_sha256,new_code_bytes:0};
 r.tool_before=fingerprint(tool);assert.equal(r.tool_before,toolPin);
 let hint;
 examine(game,fd=>{
  const code=read(game,fd,0x3238be,176,'caller',true);save('3238be-caller.bin',code);
  const iat=read(game,fd,0x609980,8,'iat');assert.equal(iat.toString('hex'),'d896a40000000000');
  const name=read(game,fd,0xa496d8,2+symbol.length+1,'hint-and-name');assert(name.subarray(2).equals(Buffer.from(symbol+'\0','ascii')));hint=name.readUInt16LE();r.import_binding={iat_rva:'0x609980',dll:'Engine.dll',symbol,hint,hint_name_rva:'0xa496d8'};
  const b=Buffer.alloc(0xc00),coff=0x84,opt=0x98,sec=0x188;b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
  b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(0xa00,opt+4);b.writeUInt32LE(0x323000,opt+20);b.writeBigUInt64LE(base,opt+24);b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);b.writeUInt32LE(0x324000,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
  b.write('.text',sec,'ascii');b.writeUInt32LE(0x96e,sec+8);b.writeUInt32LE(0x323000,sec+12);b.writeUInt32LE(0xa00,sec+16);b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,0xabe);
  const tiny=path.join(temp,'3238be-caller.inspect.dll');fs.writeFileSync(tiny,b,{flag:'wx'});r.synthetic={bytes:b.length,sha256:hash(b),never_executed:true};
  const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:0x1803238be,0x18032396d',tiny];r.command_arguments=args;
  const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:65536,env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
  save('3238be-dumpbin.txt',child.stdout||'');save('3238be-stderr.txt',child.stderr||'');r.tool_exit=child.status;assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');assert.equal(fingerprint(tiny),r.synthetic.sha256);
  const decoded=[];for(const line of (child.stdout||'').split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);if(m){decoded.push({address:'0x'+m[1].toLowerCase().replace(/^0+/,''),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(c){assert(decoded.length);decoded[decoded.length-1].bytes+=c[1].replace(/\s/g,'').toLowerCase();}}
  let pc=base+0x3238ben;for(const row of decoded){assert.equal(BigInt(row.address),pc);assert(row.instruction&&!/[?]|\(bad\)|invalid/i.test(row.instruction));pc+=BigInt(row.bytes.length/2);}assert.equal(pc,base+0x32396en);assert.equal(decoded.map(x=>x.bytes).join(''),code.toString('hex'));
  const calls=decoded.filter(x=>/^call\b/.test(x.instruction));assert.equal(calls.length,2);const expectedCalls=[[0x3238dan,0x609980n],[0x323968n,0x609128n]];
  calls.forEach((row,i)=>{const cb=Buffer.from(row.bytes,'hex');assert.equal(BigInt(row.address),base+expectedCalls[i][0]);assert.equal(cb.subarray(0,2).toString('hex'),'ff15');assert.equal(cb.length,6);assert.equal(BigInt(row.address)+6n+BigInt(cb.readInt32LE(2)),base+expectedCalls[i][1]);});
  r.calls=calls;r.instructions=decoded.length;r.byte_coverage_verified=true;save('3238be-instructions.json',JSON.stringify(decoded,null,2)+'\n');
 });
 assert(game.source_hash_preserved&&game.source_closed&&game.source_stat_stable);assert.equal(r.failures.length,0);
 examine(engine,fd=>{
  const pe=engine.pe,rawExport=Buffer.from(pe.export_directory_bytes,'hex');assert.equal(rawExport.length,40);assert.equal(pe.named_export_count,6283);assert.equal(pe.function_count,6283);
  assert.equal(rawExport.readUInt32LE(28),Number(pe.address_table_rva));assert.equal(rawExport.readUInt32LE(32),Number(pe.name_pointer_table_rva));assert.equal(rawExport.readUInt32LE(36),Number(pe.name_ordinal_table_rva));assert(hint<pe.named_export_count);
  const nameRva=read(engine,fd,Number(pe.name_pointer_table_rva)+hint*4,4,'fixed-hint-name-pointer').readUInt32LE();
  const name=read(engine,fd,nameRva,symbol.length+1,'fixed-hint-name');assert(name.equals(Buffer.from(symbol+'\0','ascii')),'Hint mismatch; no fallback scan');
  const ordinalIndex=read(engine,fd,Number(pe.name_ordinal_table_rva)+hint*2,2,'fixed-hint-ordinal').readUInt16LE();assert(ordinalIndex<pe.function_count);
  const target=read(engine,fd,Number(pe.address_table_rva)+ordinalIndex*4,4,'fixed-export-address').readUInt32LE();assert.equal(target,0xebde0,'Unexpected export target; no new body or boundary lookup');
  r.export_binding={name:symbol,index:hint,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+pe.ordinal_base,target_rva:hex(target),target_va:hex(base+BigInt(target))};
 });
 assert(engine.source_hash_preserved&&engine.source_closed&&engine.source_stat_stable);assert.equal(r.failures.length,0);
 assert.equal(r.reads.length,7);assert.equal(r.metadata_bytes,r.limits.metadata_bytes);r.status='fixed-item-be0-caller-import-and-cached-engine-body-bound';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{
 try{r.tool_after=fingerprint(tool);assert.equal(r.tool_after,toolPin);}catch(e){r.failures.push('Tool final: '+e);}
 r.source_hash_preserved=!!(game&&engine&&game.source_hash_preserved&&engine.source_hash_preserved);
 if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();save('inspection.json',JSON.stringify(r,null,2)+'\n');
}
console.log(JSON.stringify({status:r.status,import_binding:r.import_binding,export_binding:r.export_binding,game_code_bytes:176,engine_code_bytes:0,metadata_bytes:r.metadata_bytes,instructions:r.instructions,source_hash_preserved:r.source_hash_preserved,failures:r.failures}));if(r.failures.length)process.exitCode=1;
