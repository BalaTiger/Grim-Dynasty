// Exact Game IAT609F00 -> Engine export binding using already-captured OFT coverage. Original files are never loaded.
import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');assert.equal(process.argv.length,2);
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){assert(!fs.lstatSync(q).isSymbolicLink());assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;}}
const proofs=[
 ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json','94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
 ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json','227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'],
 ['docs/object-lifecycle/offline-class-registration-exports/discovery.json','0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
 ['docs/reverse-client/r1-waypoint-execution/14-Waypoint-0x18008d9e0-procedure_assembly.json','d37b38f06d40e9e26b3454ad422c9ba17cbcea827d308219d1949b7d755056ac'],
 ['docs/reverse-client/r1-navigation-binding/inspection.json','d0c171ce77a6d67e153a6911fc7c3a17ea4d61e80ab9492f3287384524e14264']
];
const data=proofs.map(([p,h])=>{const f=path.join(root,p);normal(f);assert(fs.statSync(f).size<1048576);const b=fs.readFileSync(f);assert.equal(hash(b),h);return JSON.parse(b);});
assert(data[3].result.structuredContent.normalized_result.includes('CALL qword ptr [0x180609f00]'));
const out=path.join(root,'docs/reverse-client/r1-camera-coordinates-binding');normal(path.dirname(out));assert(!fs.existsSync(out));
const report={schema:'grim-dynasty-fixed-camera-coordinates-binding-v1',started_utc:new Date().toISOString(),proofs,script_sha256:hash(fs.readFileSync(fileURLToPath(import.meta.url))),metadata_bytes:0,metadata_limit:4096,code_bytes:0,subprocesses:0,original_executed:false,files:[],failures:[]};
const gameHash='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',engineHash='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
function inspect(source,expected,size,pe,body){
 normal(source);const f={source,expected_sha256:expected,reads:[]};report.files.push(f);let fd,initial;
 function digest(){const h=createHash('sha256'),b=Buffer.alloc(65536);for(let at=0;at<size;){const n=fs.readSync(fd,b,0,Math.min(b.length,size-at),at);assert(n);h.update(b.subarray(0,n));at+=n;}return h.digest('hex');}
 function mapping(rva,n){const ss=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(ss.length,1);const s=ss[0],offset=s.raw_offset+rva-s.rva;assert(offset>=0&&offset+n<=size);return {s,offset};}
 function read(rva,n,kind){assert(Number.isSafeInteger(rva)&&Number.isSafeInteger(n)&&n>0);const {s,offset}=mapping(rva,n);assert(!(s.characteristics&0x20000000));report.metadata_bytes+=n;assert(report.metadata_bytes<=report.metadata_limit);const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);f.reads.push({kind,rva:hex(rva),file_offset:offset,bytes:n,raw_hex:b.toString('hex')});return b;}
 try{fd=fs.openSync(source,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert.equal(initial.size,size);f.sha256_before=digest();assert.equal(f.sha256_before,expected);body(read,mapping);}
 finally{if(fd!==undefined){try{f.sha256_after=digest();assert.equal(f.sha256_after,expected);normal(source);for(const s of [fs.fstatSync(fd),fs.statSync(source)])for(const k of ['size','ino','dev','mtimeMs','ctimeMs'])assert.equal(s[k],initial[k]);f.stable=true;}finally{fs.closeSync(fd);f.closed=true;}}}
}
try{
 const cache=data[0].normal_imports[4].raw_metadata,desc=Buffer.from(cache.descriptor.bytes,'hex'),prefix=Buffer.from(cache.ownership_lookup_prefix.bytes,'hex');
 assert.equal(desc.length,20);assert.equal(desc.readUInt32LE(0),0xa42ac0);assert.equal(desc.readUInt32LE(12),0xa56aa6);assert.equal(desc.readUInt32LE(16),0x609000);
 assert.equal(cache.dll_name_with_nul.bytes,'456e67696e652e646c6c00');assert.equal(Number(cache.ownership_lookup_prefix.rva),0xa42ac0);assert.equal(cache.ownership_lookup_prefix.entries,350);assert.equal(prefix.length,350*8);
 for(let i=0;i<350;i++)assert.notEqual(prefix.readBigUInt64LE(i*8),0n);
 report.cached_descriptor={source:proofs[0][0],index:4,raw:cache.descriptor,proven_nonzero_prefix_entries:350};
 assert.equal(data[1].sha256_after,gameHash);const gamePe=data[1].pe;assert.equal(gamePe.image_base,'0x180000000');
 inspect('D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',gameHash,11385856,gamePe,(read)=>{
  const index=(0x609f00-0x609000)/8;assert.equal(index,480);
  const savedTail=data[4].files[0].reads.find(x=>x.kind==='OFT-nonzero-continuation-through-selected-slot');assert(savedTail);assert.equal(savedTail.rva,'0xa435b0');const complete=Buffer.from(savedTail.raw_hex,'hex');assert.equal(complete.length,138*8);const tail=complete.subarray(0,(index-350+1)*8);for(let i=0;i<tail.length;i+=8)assert.notEqual(tail.readBigUInt64LE(i),0n);
  const lookup=tail.readBigUInt64LE(tail.length-8),iat=read(0x609f00,8,'exact-IAT-slot').readBigUInt64LE();assert.equal(iat,lookup);assert(lookup>0n&&lookup<0x100000000n);
  const nr=Number(lookup),hint=read(nr,2,'exact-import-hint').readUInt16LE();const chars=[];
  for(let i=0;i<512;i++){const c=read(nr+2+i,1,'exact-import-name-byte')[0];if(!c)break;assert(c>=32&&c<127);chars.push(c);assert(i<511,'Unterminated name');}
  const symbol=Buffer.from(chars).toString('ascii');assert(symbol.length>0);report.binding={dll:'Engine.dll',iat_rva:'0x609f00',lookup_index:index,lookup_rva:hex(0xa42ac0+index*8),hint_name_rva:hex(nr),hint,symbol};
 });
 const proof=data[2].files.find(f=>f.target==='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll');assert(proof.source_hash_preserved&&proof.source_stat_stable);assert.equal(proof.sha256_before,engineHash);assert.equal(proof.sha256_after,engineHash);const pe=proof.pe,raw=Buffer.from(pe.export_directory_bytes,'hex');
 assert.equal(raw.length,40);for(const [offset,value] of [[16,pe.ordinal_base],[20,pe.function_count],[24,pe.named_export_count],[28,Number(pe.address_table_rva)],[32,Number(pe.name_pointer_table_rva)],[36,Number(pe.name_ordinal_table_rva)]])assert.equal(raw.readUInt32LE(offset),value);
 inspect(proof.target,engineHash,4485632,pe,(read,map)=>{
  const {hint,symbol}=report.binding;assert(hint<pe.named_export_count);const nameRva=read(Number(pe.name_pointer_table_rva)+hint*4,4,'exact-hint-name-pointer').readUInt32LE();const wanted=Buffer.from(symbol+'\0','ascii');assert(read(nameRva,wanted.length,'exact-export-name').equals(wanted),'Hint mismatch: stop, no fallback scan');
  const ordinalIndex=read(Number(pe.name_ordinal_table_rva)+hint*2,2,'exact-name-ordinal').readUInt16LE();assert(ordinalIndex<pe.function_count);const target=read(Number(pe.address_table_rva)+ordinalIndex*4,4,'exact-EAT-entry').readUInt32LE();assert(!(target>=Number(pe.export_directory_rva)&&target<Number(pe.export_directory_rva)+pe.export_directory_size),'Forwarder unsupported');assert(map(target,1).s.characteristics&0x20000000);
  report.export={symbol,hint,name_rva:hex(nameRva),ordinal_index:ordinalIndex,ordinal:ordinalIndex+pe.ordinal_base,target_rva:hex(target),target_va:hex(0x180000000+target)};
 });
 report.limitations=['On-disk import/export binding; loaded IAT mutation and runtime objects unobserved.','Only camera-coordinate binding; no function body or loaded instance read. Hash passes are identity checks, not atomic snapshots.'];
}catch(e){report.failures.push(String(e));}
report.finished_utc=new Date().toISOString();report.status=report.failures.length?'failed':'passed';fs.mkdirSync(out);fs.writeFileSync(path.join(out,'inspection.json'),JSON.stringify(report,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({status:report.status,binding:report.binding,export:report.export,metadata_bytes:report.metadata_bytes,files:report.files.map(({reads,...f})=>f),failures:report.failures}));if(report.failures.length)process.exitCode=1;
