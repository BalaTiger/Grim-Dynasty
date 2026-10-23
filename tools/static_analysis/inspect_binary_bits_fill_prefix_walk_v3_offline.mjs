// V3 fixed-entry instruction-boundary discovery for the observed 0481BC -> 0483A0 call.
// At an established instruction start, begin with 1 byte and increase by exactly 1 only after
// dumpbin returns the same start and exact requested raw prefix with no mnemonic.
// Stop at one complete instruction; no callee following or guessed function-end range.
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
const caller='docs/object-lifecycle/offline-binary-bits-words-resize/048130-instructions.json';
const callerHash='50dc89276276893b8018ac3614d68c25220be5e8f45aeb5312329c72465a1d69';
const failedBoundary='docs/object-lifecycle/offline-binary-bits-words-fill-boundary/boundary.json';
const failedBoundaryHash='dc7e4c09cfe9adddd5b88c15901d65e0372b5afc3dd9f8ee6e389a1f529a77b4';
const singlePoint='docs/object-lifecycle/offline-binary-bits-fill-instruction-walk/1800483a0-dumpbin.txt';
const singlePointHash='01e164bbbf7d8db3404fca5146fe1c95b4c41a095c5125fa04e942a429934f89';
const splitPrefix='docs/object-lifecycle/offline-binary-bits-fill-prefix-walk/1800483a0-prefix-2-dumpbin.txt';
const splitPrefixHash='b2b61a1ad849adcaca4fd920e27f9cb54c455310048439ae30cfc4f6e740b3ad';
const out=path.join(root,'docs/object-lifecycle/offline-binary-bits-fill-prefix-walk-v3');
const temp=path.join(root,'.tooling/offline-binary-bits-fill-prefix-walk-v3-temp');
const start=0x1800483a0n,endWindow=start+256n;
const priorCodeBytes=958,maxUniqueBytes=128,maxInstructions=64,maxChainBytes=4096;
const hash=b=>createHash('sha256').update(b).digest('hex');
const va=n=>'0x'+n.toString(16);
function hashFile(p){
 const fd=fs.openSync(p,'r'),buf=Buffer.alloc(65536),h=createHash('sha256');
 try{let n;while((n=fs.readSync(fd,buf,0,buf.length,null))>0)h.update(buf.subarray(0,n));}
 finally{fs.closeSync(fd);}return h.digest('hex');
}
assert(!fs.existsSync(out)&&!fs.existsSync(temp),'Preserve prior inspection');
assert.equal(hashFile(path.join(root,caller)),callerHash);
assert.equal(hashFile(path.join(root,failedBoundary)),failedBoundaryHash);
assert.equal(hashFile(path.join(root,singlePoint)),singlePointHash);
assert.equal(hashFile(path.join(root,splitPrefix)),splitPrefixHash);
assert.equal(hashFile(tool),toolHash);
const copyBefore=hashFile(copy);assert.equal(copyBefore,expected);
fs.mkdirSync(out,{recursive:true});fs.mkdirSync(temp,{recursive:true});
const json=(name,data)=>fs.writeFileSync(path.join(out,name),JSON.stringify(data,null,2)+'\n',{flag:'wx'});
const record={started_utc:new Date().toISOString(),status:'started',original,working_copy:copy,
 entry_va:va(start),window_end_exclusive:va(endWindow),max_unique_instructions:maxInstructions,
 max_unique_code_bytes:maxUniqueBytes,prior_chain_verified_code_bytes:priorCodeBytes,
 prior_fill_raw_prefix:{address:va(start),bytes:'4885'},prior_chain_observed_unique_bytes:960,chain_code_budget:maxChainBytes,
 source_evidence:{path:caller,sha256:callerHash,instruction:'0x1800481bc: CALL 0x1800483a0'},
 prior_pdata_failure:{path:failedBoundary,sha256:failedBoundaryHash},
 prior_single_point_failure:{path:singlePoint,sha256:singlePointHash},
 prior_split_prefix_failure:{path:splitPrefix,sha256:splitPrefixHash},
 tool,tool_sha256_before:toolHash,copy_sha256_before:copyBefore,
 tool_timeout_ms_per_instruction:10000,tool_max_output_bytes:196608,
 code_read_policy:'Exact requested prefix at an established instruction start; advance request length only after a verified incomplete prefix',
 hash_scope:'64 KiB streaming full-file SHA256 before and after; no target or CRT execution',
 target_loaded_or_executed:false,backend_started:false,callee_bodies_followed:0,
 prefix_increment_behavior_verified:false,unique_verified_instruction_bytes:0,unique_instructions:0,
 sum_requested_bytes:0,sum_original_prefix_bytes_read:0,
 requests:[],instructions:[],edges:[]};
const observedBytes=new Map([[va(start),'48'],[va(start+1n),'85']]);
const requestedAddresses=new Set([va(start),va(start+1n)]);
let fd,failure,metadataBytes=0;
try{
 record.original_sha256_before=hashFile(original);assert.equal(record.original_sha256_before,expected);
 fd=fs.openSync(original,'r');const size=fs.fstatSync(fd).size;assert(size>0&&size<=64*1024*1024);
 function read(offset,length,metadata=true){
  assert(Number.isInteger(offset)&&Number.isInteger(length)&&offset>=0&&length>0&&length<=4096&&offset+length<=size);
  const b=Buffer.alloc(length);assert.equal(fs.readSync(fd,b,0,length,offset),length);
  if(metadata)metadataBytes+=length;return b;
 }
 const dos=read(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);
 const pe=dos.readUInt32LE(60);assert(pe<=65536);
 const coff=read(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
 const n=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);
 assert(n>0&&n<=96&&optSize>=144&&optSize<=4096);
 const optional=read(pe+24,optSize);assert.equal(optional.readUInt16LE(0),0x20b);
 const imageBase=optional.readBigUInt64LE(24);assert.equal(imageBase,0x180000000n);
 const sections=Array.from({length:n},(_,i)=>{const b=read(pe+24+optSize+i*40,40);return {rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
 function mapCode(address,length){
  const rva=Number(address-imageBase);assert(Number.isSafeInteger(rva)&&rva>=0);
  const matches=sections.filter(s=>rva>=s.rva&&rva+length<=s.rva+s.size);
  assert.equal(matches.length,1);assert(matches[0].flags&0x20000000);
  return matches[0].raw+rva-matches[0].rva;
 }
 function parsePrefix(output,address,requestedLength){
  const rows=[];
  for(const line of output.split(/\r?\n/)){
   const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
   if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
   const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
   if(c){assert(rows.length);rows.at(-1).bytes+=c[1].replace(/\s/g,'').toLowerCase();}
  }
  assert(rows.length>0,'Prefix request returned no rows');
  if(rows.every(row=>row.instruction.length===0)){
   let cursor=address,bytes='';
   for(const row of rows){
    assert.equal(row.address,cursor,'Incomplete-prefix rows must be contiguous from the known start');
    assert(row.bytes.length>0&&row.bytes.length%2===0);
    bytes+=row.bytes;cursor+=BigInt(row.bytes.length/2);
   }
   assert.equal(bytes.length/2,requestedLength,'Incomplete-prefix rows must cover exactly the request');
   return {address,bytes,length:requestedLength,instruction:'',complete:false};
  }
  assert.equal(rows.length,1,'Complete instruction must be exactly one row');
  const row=rows[0];assert.equal(row.address,address,'Prefix address mismatch');
  const length=row.bytes.length/2;assert.equal(length,requestedLength,'Output must cover exactly the requested prefix');
  assert(length>=1&&length<=15);
  assert(!/^(?:db|dw|dd|dq|\?\?\?)(?:\s|$)/i.test(row.instruction),'Invalid or undecoded mnemonic');
  return {...row,length,complete:row.instruction.length>0};
 }
 const queue=[start],queued=new Set([va(start)]),visited=new Map(),byteOwners=new Map();
 function edge(from,to,kind){
  assert(to>=start&&to<endWindow,'Control flow leaves fixed 256-byte window');
  const key=va(to),owner=byteOwners.get(key);
  assert(owner===undefined||owner===key,'Control flow enters middle of an instruction');
  record.edges.push({from:va(from),to:key,kind});
  if(!queued.has(key)){queued.add(key);queue.push(to);}
 }
 while(queue.length){
  const address=queue.shift(),addressKey=va(address);
  if(visited.has(addressKey))continue;
  assert(address>=start&&address<endWindow);
  assert(record.unique_instructions<maxInstructions,'Unique-instruction budget reached');
  const owner=byteOwners.get(addressKey);assert(owner===undefined||owner===addressKey,'Known instruction start overlaps decoded instruction');
  let row,offset;
  for(let requestedLength=1;requestedLength<=15;requestedLength++){
   assert(address+BigInt(requestedLength)<=endWindow,'Requested prefix exceeds fixed window');
   let newlyRequested=0;
   for(let i=0;i<requestedLength;i++){
    const key=va(address+BigInt(i));
    assert(!byteOwners.has(key),'Requested prefix overlaps decoded instruction');
    assert(i===0||!queued.has(key),'Requested prefix overlaps a known target');
    if(!requestedAddresses.has(key))newlyRequested++;
   }
   assert(requestedAddresses.size+newlyRequested<=maxUniqueBytes&&priorCodeBytes+requestedAddresses.size+newlyRequested<=maxChainBytes,
    'Requested prefix exceeds unique-code budget');
   offset=mapCode(address,requestedLength);
   const last=address+BigInt(requestedLength-1);
   const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${addressKey},${va(last)}`,copy];
   const request={instruction_start:addressKey,requested_length:requestedLength,range_end_inclusive:va(last),
    command_arguments:args,newly_requested_unique_bytes:newlyRequested};
   record.requests.push(request);record.sum_requested_bytes+=requestedLength;
   for(let i=0;i<requestedLength;i++)requestedAddresses.add(va(address+BigInt(i)));
   const began=Date.now(),r=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:196608,
    env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
   request.elapsed_ms=Date.now()-began;request.exit_code=r.status;
   const stem=address.toString(16)+'-prefix-'+requestedLength;
   fs.writeFileSync(path.join(out,stem+'-dumpbin.txt'),r.stdout??'',{flag:'wx'});
   fs.writeFileSync(path.join(out,stem+'-stderr.txt'),r.stderr??'',{flag:'wx'});
   assert.equal(r.error,undefined);assert.equal(r.status,0);assert.equal(r.stderr,'');
   row=parsePrefix(r.stdout,address,requestedLength);
   const bytes=read(offset,requestedLength,false);record.sum_original_prefix_bytes_read+=requestedLength;
   assert.equal(bytes.toString('hex'),row.bytes,'Decoded prefix differs from exact original prefix bytes');
   for(let i=0;i<requestedLength;i++){
    const key=va(address+BigInt(i)),byte=row.bytes.slice(2*i,2*i+2);
    if(observedBytes.has(key))assert.equal(observedBytes.get(key),byte,'Previously observed byte changed');
    observedBytes.set(key,byte);
   }
   request.prefix_bytes=row.bytes;request.original_file_offset=offset;request.prefix_sha256=hash(bytes);
   request.unique_function_bytes_after=observedBytes.size;
   request.unique_function_requested_bytes_after=requestedAddresses.size;
   fs.writeFileSync(path.join(out,stem+'.bin'),bytes,{flag:'wx'});
   if(row.complete){request.outcome='complete-instruction';break;}
   request.outcome='incomplete-prefix-without-mnemonic';
   assert(requestedLength<15,'No complete instruction within 15 bytes');
   record.prefix_increment_behavior_verified=true;
  }
  assert(row&&row.complete,'No complete instruction established');
  const saved={address:addressKey,bytes:row.bytes,length:row.length,instruction:row.instruction,file_offset:offset,
   code_sha256:hash(Buffer.from(row.bytes,'hex'))};
  visited.set(addressKey,saved);record.instructions.push(saved);
  for(let i=0;i<row.length;i++)byteOwners.set(va(address+BigInt(i)),addressKey);
  record.unique_instructions++;record.unique_verified_instruction_bytes+=row.length;
  const match=/^([a-z][a-z0-9]*)\s*(.*)$/i.exec(row.instruction);assert(match,'Unclassified instruction syntax');
  const mnemonic=match[1].toLowerCase(),operand=match[2].trim();
  if(mnemonic==='ret'){record.edges.push({from:addressKey,to:null,kind:'return'});continue;}
  assert(!/^(?:call|callf|retf|iret|iretd|iretq|syscall|sysenter|sysexit|sysret|int|int3|into|hlt|ud2)$/.test(mnemonic),'Call or exceptional/indirect transfer is outside fixed discovery');
  if(mnemonic==='jmp'||/^j[a-z]+$/.test(mnemonic)||/^loop(?:e|ne|z|nz)?$/.test(mnemonic)){
   assert(/^(?:0x)?[0-9a-f]{16}$/i.test(operand),'Indirect or ambiguous branch target');
   const target=BigInt(operand.toLowerCase().startsWith('0x')?operand:'0x'+operand);
   edge(address,target,mnemonic==='jmp'?'direct-jump':'conditional-taken');
   if(mnemonic==='jmp')continue;
   edge(address,address+BigInt(row.length),'conditional-fallthrough');
  }else{
   // Unknown prefixes or instructions must not be assumed to have ordinary fallthrough.
   assert(/^(?:mov|movzx|movsx|movsxd|lea|test|cmp|add|adc|sub|sbb|inc|dec|and|or|xor|not|neg|shl|shr|sar|sal|rol|ror|imul|mul|div|idiv|xchg|push|pop|nop|bts|btr|btc|bt|bsf|bsr|cmov[a-z]+|set[a-z]+|movdqa|movdqu|movaps|movups|pxor)$/.test(mnemonic),
    'Unclassified non-branch instruction; do not assume fallthrough');
   edge(address,address+BigInt(row.length),'fallthrough');
  }
 }
 assert(record.edges.some(e=>e.kind==='return'),'No reachable return found');
 assert(record.edges.every(e=>e.to===null||visited.has(e.to)),'Unresolved CFG edge');
 record.status='bounded-prefix-cfg-verified';record.complete_reachable_cfg_within_fixed_limits=true;
}catch(e){failure=e;record.status='stopped';record.failure=String(e);}
finally{
 if(fd!==undefined)fs.closeSync(fd);
 record.metadata_bytes=metadataBytes;record.unique_function_observed_bytes=observedBytes.size;
 record.unique_function_requested_code_bytes=requestedAddresses.size;
 record.chain_requested_unique_code_bytes_after=priorCodeBytes+requestedAddresses.size;
 record.newly_observed_unique_code_bytes=observedBytes.size-2;
 record.chain_observed_unique_code_bytes_after=priorCodeBytes+observedBytes.size;
 record.original_sha256_after=hashFile(original);record.copy_sha256_after=hashFile(copy);record.tool_sha256_after=hashFile(tool);
 record.original_hash_matches=record.original_sha256_after===expected;
 record.copy_hash_matches=record.copy_sha256_after===expected;record.tool_hash_matches=record.tool_sha256_after===toolHash;
 record.finished_utc=new Date().toISOString();record.controller_resource_usage=process.resourceUsage();
 json('inspection.json',record);
 json('0483a0-instructions.json',record.instructions);
 json('0483a0-cfg.json',{entry:va(start),status:record.status,edges:record.edges});
}
assert(record.original_hash_matches&&record.copy_hash_matches&&record.tool_hash_matches);
if(failure)throw failure;
console.log(JSON.stringify({status:record.status,instructions:record.unique_instructions,
 unique_function_observed_bytes:record.unique_function_observed_bytes,sum_requested_bytes:record.sum_requested_bytes,
 chain_observed_unique_code_bytes_after:record.chain_observed_unique_code_bytes_after,
 prefix_increment_behavior_verified:record.prefix_increment_behavior_verified,
 original_unchanged:true,copy_unchanged:true,tool_unchanged:true,controller_peak_rss_kib:record.controller_resource_usage.maxRSS}));
