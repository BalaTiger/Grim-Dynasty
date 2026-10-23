// Prepared only. Fixed literals/defaults/imports and two +48 vtable slots.
// No subprocess, original load, archives/global-state reads, or callee traversal.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2);
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_global_load_inputs.mjs').toLowerCase());
const gamePath='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',enginePath='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const gamePin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',enginePin='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const out=path.join(root,'docs/object-lifecycle/offline-item-global-load-inputs');
const sources=[
 ['docs/object-lifecycle/offline-item-global-load-body/inspection.json','26a01be264dbad6706bdc6e8553d37ba8811f7cf523eda93e0e5d21d0a9f33a8'],
 ['docs/object-lifecycle/offline-item-global-load-body/instructions.json','cee579cfa26e049167595ac55792993201e6268bd459474da6689f648e382262'],
 ['docs/task-kills/ghidra-selected/subscriber-constructor-routes.json','e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165'],
 ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
 ['docs/object-lifecycle/offline-item-name-import/inspection.json','c3ba3e79ecceb98225a9034a85649807d8d00444feccf771b294a41d1e443f31'],
 ['docs/phase-1/boundary-audit/module-boundaries.json','d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'],
 ['docs/object-lifecycle/record-interface-discovery/record-interface-exports.json','33d7527e64565cc4e56a31b3665b41e004ad9e53f93739e3a645f205cf0b1603']
];
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert(!s.isSymbolicLink());
 assert(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(65536),h=createHash('sha256');for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
normal(self,true);normal(path.dirname(out));absent(out);
const p=sources.map(([rel,sha],i)=>{const f=path.join(root,rel);normal(f,true);assert(fs.statSync(f).size<=(i===1?2097152:i===5?1048576:524288));
 const b=fs.readFileSync(f);assert.equal(hash(b),sha);return JSON.parse(b);});
const body=p[0],rows=p[1];assert.equal(body.status,'exact-global-load-body-decoded-review-required');
assert.equal(body.original,gamePath);assert.equal(body.sha256_before,gamePin);assert.equal(body.sha256_after,gamePin);
assert(body.source_closed&&body.source_stat_stable&&body.source_hash_preserved&&body.decoded_body_complete&&body.instruction_byte_coverage_verified);
assert.equal(body.failures.length,0);
function row(rva){const a=rows.filter(x=>x.address===hex(0x180000000+rva));assert.equal(a.length,1);return a[0];}
function rip(rva,prefix,target){const v=row(rva),b=Buffer.from(v.bytes,'hex'),lead=Buffer.from(prefix,'hex');
 assert.equal(b.length,lead.length+4);assert(b.subarray(0,lead.length).equals(lead));assert.equal(rva+b.length+b.readInt32LE(lead.length),target);return v;}
const references=[rip(0x2c8803,'ff15',0x609090),rip(0x2c87f7,'ff15',0x609098),rip(0x2c8815,'ff15',0x609a90),
 rip(0x2ca400,'4c8d05',0x61812f),rip(0x2ca407,'488d15',0x694728),rip(0x2cec37,'488d15',0x696378),
 rip(0x2cec58,'488d15',0x696360),rip(0x2cec2a,'f3440f103d',0x7a80b8),rip(0x2cec50,'f30f1015',0x7a80c8)];
const ctors=p[2].functions.filter(f=>f.observed_entry==='1802c07a0');assert.equal(ctors.length,1);
const ctorLines=['1802c18ca: LEA RDX,[RBP + -0x28]','1802c18ce: MOV RCX,RDI','1802c18d1: CALL 0x1802c8750',
 '1802c08cc: LEA RAX,[0x180693a48]','1802c08d3: MOV qword ptr [RCX + 0x1a8],RAX'];
for(const s of ctorLines)assert(ctors[0].assembly.includes(s));
assert.equal(p[3].status,'fixed-seven-questitem-imports-confirmed');assert.equal(p[3].original,gamePath);
assert.equal(p[3].expected_target_sha256,gamePin);
const c=p[3].normal_imports[3].raw_metadata,descriptor=Buffer.from(c.descriptor.bytes,'hex'),prefix=Buffer.from(c.ownership_lookup_prefix.bytes,'hex');
assert.equal(c.descriptor.rva,'0xa4297c');assert.equal(descriptor.toString('hex'),'c02aa4000000000000000000a66aa50000906000');
assert.equal(c.ownership_lookup_prefix.rva,'0xa42ac0');assert.equal(c.ownership_lookup_prefix.entries,332);assert.equal(prefix.length,332*8);
assert.equal(c.dll_name_with_nul.bytes,'456e67696e652e646c6c00');
for(let i=0;i<332;i++)assert(prefix.readBigUInt64LE(i*8)!==0n,'Cached import prefix terminated');
const modules=p[5].filter(m=>m.relativePath==='x64/Game.dll');assert.equal(modules.length,1);assert.equal(modules[0].sha256,gamePin);
const engineImports=modules[0].importModules[3];assert.equal(engineImports.module,'Engine.dll');
const imports=[{index:18,iat:0x609090},{index:19,iat:0x609098},{index:338,iat:0x609a90}];
for(const v of imports){assert.equal(v.iat,0x609000+v.index*8);v.expected_name=engineImports.symbols[v.index];
 assert(typeof v.expected_name==='string'&&v.expected_name.length>0&&v.expected_name.length<256);}
assert.equal(prefix.subarray(18*8,19*8).toString('hex'),'f058a40000000000');
assert.equal(prefix.subarray(19*8,20*8).toString('hex'),'6e59a40000000000');
assert.equal(p[4].status,'fixed-item-name-import-export-verified');assert.equal(p[4].failures.length,0);
function module(name,file,size,pin){const a=p[4].files.filter(f=>f.name===name);assert.equal(a.length,1);const old=a[0];
 assert.equal(old.path,file);assert.equal(old.size,size);assert.equal(old.expected_sha256,pin);assert.equal(old.sha256_before,pin);assert.equal(old.sha256_after,pin);
 assert(old.source_closed&&old.source_stat_stable&&old.source_hash_preserved);
 return {name,path:file,size,expected_sha256:pin,pe:old.pe,code_bytes:0,data_bytes:0,metadata_bytes:0,source_closed:false};}
const game=module('Game',gamePath,11385856,gamePin),engine=module('Engine',enginePath,4485632,enginePin);
assert.equal(p[6].target,enginePath);assert.equal(p[6].expected_sha256,enginePin);assert.equal(p[6].sha256_before,enginePin);assert.equal(p[6].sha256_after,enginePin);
const vtables=[{class_name:'LoadTable',base:0x341f10,slot:0x341f58,ordinal:1253},{class_name:'LoadTableBinary',base:0x341fe0,slot:0x342028,ordinal:1254}];
for(const v of vtables){const a=p[6].candidates.filter(x=>x.class_name===v.class_name&&x.kind==='vftable');assert.equal(a.length,1);
 assert.equal(Number(a[0].rva),v.base);assert.equal(a[0].ordinal,v.ordinal);assert.equal(a[0].forwarder,null);assert.equal(v.base+0x48,v.slot);v.cached_export=a[0];}
function map(m,rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const a=m.pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(a.length,1);
 const offset=a[0].raw_offset+rva-a[0].rva;assert(offset>=0&&offset<=m.size-n);return {offset,section:a[0]};}
assert.equal(map(game,0xa4297c,20).offset,c.descriptor.file_offset);assert.equal(map(game,0xa42ac0,prefix.length).offset,c.ownership_lookup_prefix.file_offset);
const literals=[{rva:0x693a48,role:'constructor-record-name'},{rva:0x694728,role:'secondary-table-key'},
 {rva:0x61812f,role:'secondary-table-default'},{rva:0x696378,role:'first-float-key'},{rva:0x696360,role:'second-float-key'}];
const r={schema:'grim-dynasty-item-global-load-inputs-v1',status:'started',started_utc:new Date().toISOString(),sources,files:[game,engine],
 cached_references:references,cached_constructor_lines:ctorLines,cached_engine_descriptor:c.descriptor,cached_oft_entries:332,
 limits:{game_code_bytes:26,game_data_bytes:648,game_metadata_bytes:854,engine_data_bytes:16,engine_code_bytes:0,engine_metadata_bytes:0,
  literal_bytes_each:128,import_name_bytes_each:256,oft_tail_bytes:56,read_operations:1424,hash_buffer_bytes:65536},
 original_loaded_or_executed:false,subprocesses:0,dumpbin_calls:0,callees_followed:0,global_state_read:false,archives_read:false,
 read_operations:0,reads:[],literals:[],defaults:[],import_bindings:[],vtable_slots:[],anchors:[],failures:[],
 limitations:['Strings are read one byte at a time and stop immediately at NUL or the fixed limit. No adjacent string discovery is performed.',
  'Only the missing seven OFT entries332..338 are newly read; the first332 and descriptor/name ownership are hash-pinned cache.',
  'The three named imports are bound on disk only. No Engine export/callee resolution is performed here.',
  'Two fixed +48 vtable slots are observed from disk. Their pointees and loaded replacements are not read or executed.',
  'The constructor anchors verify only their local instructions, not reachability, object lifetime, database values or absence of later writers.',
  'Separate same-handle64KiB before/after module hashes are identity checks, not atomic snapshots or loaded compatibility.']};
function read(m,fd,rva,n,kind,category){const mapped=map(m,rva,n),isCode=category==='code';
 assert(isCode?!!(mapped.section.characteristics&0x20000000):!(mapped.section.characteristics&0x20000000));
 if(isCode)assert(m===game&&((rva===0x2c18ca&&n===12)||(rva===0x2c08cc&&n===14)));
 const key=category+'_bytes',limit=r.limits[m.name.toLowerCase()+'_'+key];assert(Number.isInteger(limit));assert(m[key]+n<=limit);
 assert(r.read_operations<r.limits.read_operations);r.read_operations++;m[key]+=n;
 const b=Buffer.alloc(n),got=fs.readSync(fd,b,0,n,mapped.offset);r.reads.push({module:m.name,kind,category,rva:hex(rva),file_offset:mapped.offset,requested_bytes:n,read_bytes:got,raw_hex:b.subarray(0,got).toString('hex')});
 assert.equal(got,n,'Short fixed input read');return b;}
function ascii(m,fd,rva,maximum,kind,category){const a=[];for(let i=0;i<maximum;i++){
 const v=read(m,fd,rva+i,1,kind,category)[0];a.push(v);if(v===0)return {text:Buffer.from(a.slice(0,-1)).toString('ascii'),raw_hex:Buffer.from(a).toString('hex'),bytes:a.length};
 assert(v<=0x7f,'Non-ASCII byte outside selected string coverage');}assert.fail('No NUL within fixed '+maximum+'B string budget');}
function examine(m,action){let fd,initial;try{normal(m.path,true);fd=fs.openSync(m.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);
 assert(initial.isFile());assert.equal(initial.size,m.size);m.sha256_before=digest(fd,m.size);assert.equal(m.sha256_before,m.expected_sha256);action(fd);}
 finally{if(fd!==undefined){try{if(initial&&initial.size===m.size){m.sha256_after=digest(fd,m.size);assert.equal(m.sha256_after,m.expected_sha256);const s=fs.fstatSync(fd);
  m.source_stat_stable=s.isFile()&&s.size===initial.size&&s.ino===initial.ino&&s.dev===initial.dev&&s.mtimeMs===initial.mtimeMs&&s.ctimeMs===initial.ctimeMs;assert(m.source_stat_stable);}}
  catch(e){r.failures.push(m.name+' final: '+e);}finally{try{fs.closeSync(fd);m.source_closed=true;}catch(e){r.failures.push(m.name+' close: '+e);}}
  m.source_hash_preserved=m.sha256_before===m.expected_sha256&&m.sha256_after===m.expected_sha256;}}}
let created=false;
try{
 fs.mkdirSync(out);created=true;normal(out);r.script_sha256=hash(fs.readFileSync(self));
 examine(game,fd=>{
  const call=read(game,fd,0x2c18ca,12,'constructor-load-call-anchor','code');
  assert.equal(call.subarray(0,4).toString('hex'),'488d55d8');assert(['488bcf','4889f9'].includes(call.subarray(4,7).toString('hex')));
  assert.equal(call[7],0xe8);assert.equal(0x2c18d6+call.readInt32LE(8),0x2c8750);
  r.anchors.push({rva:'0x2c18ca',bytes:12,raw_hex:call.toString('hex'),verified:'LEA RDX,[RBP-28]; MOV RCX,RDI; CALL2C8750'});
  const name=read(game,fd,0x2c08cc,14,'constructor-record-name-anchor','code');
  assert.equal(name.subarray(0,3).toString('hex'),'488d05');assert.equal(0x2c08d3+name.readInt32LE(3),0x693a48);
  assert.equal(name.subarray(7).toString('hex'),'488981a8010000');r.anchors.push({rva:'0x2c08cc',bytes:14,raw_hex:name.toString('hex'),verified:'LEA RAX,693A48; QWORD[RCX+1A8]=RAX'});
  for(const v of literals)r.literals.push({...v,rva:hex(v.rva),...ascii(game,fd,v.rva,128,'fixed-literal','data')});
  for(const at of [0x7a80b8,0x7a80c8]){const b=read(game,fd,at,4,'fixed-default','data');r.defaults.push({rva:hex(at),raw_hex:b.toString('hex'),uint32:b.readUInt32LE(),float32:b.readFloatLE()});}
  const tail=read(game,fd,0xa42ac0+332*8,56,'fixed-oft-tail-332-through-338','metadata');
  for(let i=0;i<7;i++)assert(tail.readBigUInt64LE(i*8)!==0n,'Import table terminated before requested index');
  for(const v of imports){const lookup=v.index<332?prefix.readBigUInt64LE(v.index*8):tail.readBigUInt64LE((v.index-332)*8);
   assert(lookup>0n&&lookup<=0xffffffffn,'Ordinal, bound or missing import outside fixed scope');
   const iat=read(game,fd,v.iat,8,'fixed-iat','metadata');assert.equal(iat.readBigUInt64LE(),lookup,'Disk IAT/OFT mismatch');
   const nameRva=Number(lookup),hint=read(game,fd,nameRva,2,'fixed-import-hint','metadata').readUInt16LE();
   const symbol=ascii(game,fd,nameRva+2,256,'fixed-import-name','metadata');assert.equal(symbol.text,v.expected_name,'Cached named import mismatch');
   r.import_bindings.push({iat_rva:hex(v.iat),lookup_index:v.index,oft_rva:hex(0xa42ac0+v.index*8),dll:'Engine.dll',hint_name_rva:hex(nameRva),hint,...symbol});}
 });
 assert(game.source_closed&&game.source_stat_stable&&game.source_hash_preserved);assert.equal(r.failures.length,0);
 examine(engine,fd=>{for(const v of vtables){const b=read(engine,fd,v.slot,8,'fixed-vtable-slot-plus48','data');
  r.vtable_slots.push({class_name:v.class_name,cached_export:v.cached_export,slot_rva:hex(v.slot),slot_offset:'0x48',raw_hex:b.toString('hex'),preferred_pointer:hex(b.readBigUInt64LE()),pointee_bytes_read:0});}});
 assert(engine.source_closed&&engine.source_stat_stable&&engine.source_hash_preserved);assert.equal(r.failures.length,0);
 r.status='fixed-global-load-inputs-observed';
}catch(e){r.failures.push(String(e));r.status='failed';}
finally{r.source_hash_preserved=!!(game.source_hash_preserved&&engine.source_hash_preserved);if(r.failures.length)r.status='failed';r.finished_utc=new Date().toISOString();
 if(created){normal(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(r,null,2)+'\n',{flag:'wx'});}}
console.log(JSON.stringify({status:r.status,literals:r.literals,defaults:r.defaults,import_bindings:r.import_bindings,vtable_slots:r.vtable_slots,
 semantic_reads:r.read_operations,files:r.files.map(f=>({name:f.name,code_bytes:f.code_bytes,data_bytes:f.data_bytes,metadata_bytes:f.metadata_bytes,source_closed:f.source_closed,source_hash_preserved:f.source_hash_preserved})),failures:r.failures}));
if(r.failures.length)process.exitCode=1;
