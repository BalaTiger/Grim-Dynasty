// Fixed metadata-only TerrainType lookup. Prepared for root review; no target execution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length,2,'No arguments accepted');
const script=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(script),'../..');
assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
const target='D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash='4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153',expectedBytes=4485632;
const prefixes=['??0TerrainType@GAME@@','?Load@TerrainType@GAME@@'];
const planRelative='tools/static_analysis/r1-terrain-type-exports-plan.json';
const planHash='a42ae4d65d8e4bec77f656981953013f79e394b2e2bf35b846c35fc3b4bf8a67';
const proofRelative='docs/object-lifecycle/offline-class-registration-exports/discovery.json';
const proofHash='0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58';
const outputDirectory=path.join(root,'docs/reverse-client/r1-terrain-type-exports');
const output=path.join(outputDirectory,'discovery.json');
const limits=Object.freeze({new_metadata_bytes:16384,metadata_operations:1024,export_name_bytes:256,
  export_name_chunk_bytes:32,distinct_names:96,candidates_per_prefix:2,candidates_total:4,
  binary_search_steps:32,hash_chunk_bytes:65536,report_bytes:262144});
const hash=bytes=>createHash('sha256').update(bytes).digest('hex');
const hex=value=>'0x'+value.toString(16);
const within=(offset,length,size)=>Number.isSafeInteger(offset)&&offset>=0&&Number.isSafeInteger(length)&&length>0&&offset<=size&&length<=size-offset;
function checkAncestors(input,leafFile){
  let current=path.resolve(input),first=true;
  while(true){const stat=fs.lstatSync(current);assert.ok(!stat.isSymbolicLink(),'Symbolic path not admitted');
    assert.equal(fs.realpathSync(current).toLowerCase(),current.toLowerCase(),'Redirected path not admitted');
    assert.ok(first&&leafFile?stat.isFile():stat.isDirectory(),'Unexpected path type');
    first=false;const parent=path.dirname(current);if(parent===current)break;current=parent;}
}
function checkedProjectFile(relative,expected){const p=path.resolve(root,relative);assert.ok(p.startsWith(root+path.sep));checkAncestors(p,true);
  const bytes=fs.readFileSync(p);assert.equal(hash(bytes),expected,'Pinned project evidence changed: '+relative);return bytes;}
const plan=JSON.parse(checkedProjectFile(planRelative,planHash));
assert.equal(plan.schema,'grim-dynasty-r1-terrain-type-export-plan-v1');
assert.deepEqual(plan.source,{path:target,sha256:expectedHash,bytes:expectedBytes});
assert.deepEqual(plan.prefixes,prefixes);assert.deepEqual(plan.limits,limits);
assert.equal(plan.output,'docs/reverse-client/r1-terrain-type-exports/discovery.json');
assert.equal(plan.automatic_expansion,false);assert.equal(plan.original_semantic_code_reads,0);
for(const evidence of plan.evidence)checkedProjectFile(evidence.file,evidence.sha256);
const proof=JSON.parse(checkedProjectFile(proofRelative,proofHash));
const hits=proof.files.filter(f=>f.target===target);assert.equal(hits.length,1);const saved=hits[0],pe=saved.pe;
assert.equal(saved.status,'metadata-discovery-completed');assert.equal(saved.expected_file_bytes,expectedBytes);
assert.equal(saved.sha256_before,expectedHash);assert.equal(saved.sha256_after,expectedHash);assert.ok(saved.source_stat_stable&&saved.source_hash_preserved);
assert.equal(saved.preferred_image_base,'0x180000000');assert.equal(pe.named_export_count,6283);assert.equal(pe.function_count,6283);
assert.equal(pe.pe_header_file_offset,288);
const exportRva=Number(pe.export_directory_rva),exportSize=pe.export_directory_size;
const namesRva=Number(pe.name_pointer_table_rva),ordinalsRva=Number(pe.name_ordinal_table_rva),addressesRva=Number(pe.address_table_rva);
assert.equal(exportRva,0x3af700);assert.equal(exportSize,417432);
assert.equal(namesRva,0x3b5954);assert.equal(ordinalsRva,0x3bbb80);assert.equal(addressesRva,0x3af728);
checkAncestors(path.dirname(outputDirectory),false);assert.ok(!fs.existsSync(outputDirectory),'Fresh evidence directory required');
// mkdir without recursive/overwrite rejects an existing path or dangling link.
fs.mkdirSync(outputDirectory);
const report={schema:'grim-dynasty-r1-terrain-type-export-result-v1',status:'started',started_utc:new Date().toISOString(),
  plan:{file:planRelative,sha256:planHash},script_sha256:hash(fs.readFileSync(script)),target,expected_sha256:expectedHash,expected_bytes:expectedBytes,
  proof:{file:proofRelative,sha256:proofHash},prefixes,limits,open_mode:'O_RDONLY',target_loaded_or_executed:false,backend_started:false,
  subprocesses:0,target_code_semantic_reads:0,target_payload_reads:0,whole_export_name_table_scanned:false,whole_pdata_table_scanned:false,
  new_metadata_bytes:0,metadata_operations:0,reads:[],fingerprint_passes:[],searches:[],candidates:[],encountered_names:[],
  limitations:[
    'The saved source hash and PE map are reused, then checked against the same original handle before semantic reads. Only two named prefixes are searched.',
    'Two full-file SHA256 passes use one64KiB buffer. They fingerprint all bytes but interpret no code. Semantic reads are bounded export metadata, one exception directory entry, and selected pdata binary-search entries only.',
    'Sorted name/pdata tables are relied upon; only encountered entries are checked. A missing prefix does not prove absence of unexported, renamed, or inlined behavior.',
    'At most two named candidates per prefix are emitted. Excess matches remain explicit and never cause query expansion.',
    'A containing pdata range is an observed unwind range, not proof of the complete logical function. Forwarded/null exports remain Unknown without reading destinations.',
    'No native TerrainType construction, Load, resource identity, FC field, runtime allocation, gameplay or ABI compatibility is established by names.',
    'Node O_RDONLY and same-handle stat/hash verification do not claim Windows FileShare.Read locking or atomicity against transient modifications.'
  ]};
let fd,initialStat,admitted=false,failure;const observed=new Map(),buffer=Buffer.alloc(limits.hash_chunk_bytes);
function fingerprint(label){assert.equal(fs.fstatSync(fd).size,expectedBytes);const h=createHash('sha256');let pos=0;
  while(pos<expectedBytes){const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,expectedBytes-pos),pos);assert.ok(n>0);h.update(buffer.subarray(0,n));pos+=n;}
  const sha256=h.digest('hex');report.fingerprint_passes.push({label,bytes:pos,sha256,chunk_bytes:buffer.length});return sha256;}
function mapped(rva,length,requiredSection){assert.ok(within(rva,length,pe.size_of_image));const sections=pe.sections.filter(s=>rva>=s.rva&&within(rva-s.rva,length,s.raw_size));
  assert.equal(sections.length,1,'Unique raw RVA mapping required');const section=sections[0];assert.equal(section.characteristics&0x20000000,0,'No executable metadata section');
  if(requiredSection)assert.equal(section.name,requiredSection);const offset=section.raw_offset+rva-section.rva;assert.ok(within(offset,length,expectedBytes));return {offset,remaining:section.raw_size-(rva-section.rva)};}
function readOffset(offset,length,kind){assert.ok(within(offset,length,expectedBytes)&&length<=32);assert.ok(++report.metadata_operations<=limits.metadata_operations);
  assert.ok(length<=limits.new_metadata_bytes-report.new_metadata_bytes,'Metadata byte budget');report.new_metadata_bytes+=length;
  const bytes=Buffer.alloc(length),op={kind,file_offset:offset,requested_bytes:length,read_bytes:0,raw_bytes:''};report.reads.push(op);
  while(op.read_bytes<length){const n=fs.readSync(fd,bytes,op.read_bytes,length-op.read_bytes,offset+op.read_bytes);assert.ok(n>0);op.read_bytes+=n;op.raw_bytes=bytes.subarray(0,op.read_bytes).toString('hex');}return bytes;}
function exportRead(rva,length,kind){assert.ok(rva>=exportRva&&within(rva-exportRva,length,exportSize),'Only export metadata range');return readOffset(mapped(rva,length,'.rdata').offset,length,kind);}
function nameAt(index){assert.ok(Number.isInteger(index)&&index>=0&&index<pe.named_export_count);if(observed.has(index))return observed.get(index);
  assert.ok(observed.size<limits.distinct_names);const rva=exportRead(namesRva+index*4,4,'export-name-pointer').readUInt32LE(0),parts=[];let used=0;
  while(used<limits.export_name_bytes){const available=mapped(rva+used,1,'.rdata').remaining,count=Math.min(limits.export_name_chunk_bytes,limits.export_name_bytes-used,available,exportRva+exportSize-rva-used);
    assert.ok(count>0);const bytes=exportRead(rva+used,count,'export-name-ascii'),nul=bytes.indexOf(0),piece=nul<0?bytes:bytes.subarray(0,nul);
    for(const ch of piece)assert.ok(ch>=0x20&&ch<0x7f,'Printable ASCII export required');parts.push(piece);used+=count;
    if(nul>=0){const name=Buffer.concat(parts).toString('ascii');for(const [other,v] of observed)assert.ok(other<index?v.name<name:v.name>name,'Encountered names not strictly sorted');
      const result={index,name,rva:hex(rva)};observed.set(index,result);return result;}}
  throw Error('Export name exceeds fixed byte limit');}
function lowerBound(value){let lo=0,hi=pe.named_export_count,steps=0;while(lo<hi){assert.ok(++steps<=limits.binary_search_steps);const mid=lo+Math.floor((hi-lo)/2);if(nameAt(mid).name<value)lo=mid+1;else hi=mid;}return lo;}
let pdataRva,pdataSize,pdataOffset;
function boundary(rva){let lo=0,hi=pdataSize/12-1,steps=0;const encountered=[];
  while(lo<=hi){assert.ok(++steps<=limits.binary_search_steps);const index=lo+Math.floor((hi-lo)/2),b=readOffset(pdataOffset+index*12,12,'selected-pdata-search');
    const begin=b.readUInt32LE(0),end=b.readUInt32LE(4);assert.ok(begin<end&&end<=pe.size_of_image);for(const e of encountered)assert.ok(e.index<index?e.begin<begin:e.begin>begin,'Encountered pdata not sorted');encountered.push({index,begin,end});
    if(rva<begin)hi=index-1;else if(rva>=end)lo=index+1;else return {status:begin===rva?'ExactEntry':'InteriorExportAlias',index,begin_rva:hex(begin),end_rva_exclusive:hex(end),begin_va:hex(0x180000000n+BigInt(begin)),end_va_exclusive:hex(0x180000000n+BigInt(end)),bytes:end-begin,unwind_info_rva:hex(b.readUInt32LE(8)),logical_function_complete:false};}
  return {status:'UnknownNoContainingPdata',begin_rva:null,end_rva_exclusive:null};}
try{
  checkAncestors(target,true);fd=fs.openSync(target,fs.constants.O_RDONLY);initialStat=fs.fstatSync(fd);assert.ok(initialStat.isFile());assert.equal(initialStat.size,expectedBytes);admitted=true;
  report.sha256_before=fingerprint('before');assert.equal(report.sha256_before,expectedHash,'Original hash changed; semantic read prohibited');
  // Exact PE32+ exception-directory header entry; source/PE offset pinned above.
  const exception=readOffset(pe.pe_header_file_offset+24+136,8,'exception-directory-header');pdataRva=exception.readUInt32LE(0);pdataSize=exception.readUInt32LE(4);
  assert.ok(pdataSize>0&&pdataSize%12===0&&pdataSize<=192*1024);pdataOffset=mapped(pdataRva,pdataSize,'.pdata').offset;report.exception_directory={rva:hex(pdataRva),bytes:pdataSize};
  for(const prefix of prefixes){const first=lowerBound(prefix),last=lowerBound(prefix+'\x7f'),matches=last-first,emit=Math.min(matches,limits.candidates_per_prefix);
    report.searches.push({prefix,first,last,matches,emitted:emit,omitted_due_to_fixed_limit:matches-emit,status:matches?'NamedCandidatesOnly':'UnknownNoMatchingExport'});
    for(let index=first;index<first+emit;index++){assert.ok(report.candidates.length<limits.candidates_total);const item=nameAt(index);assert.ok(item.name.startsWith(prefix));
      const ordinalIndex=exportRead(ordinalsRva+index*2,2,'export-ordinal').readUInt16LE(0);assert.ok(ordinalIndex<pe.function_count);
      const rva=exportRead(addressesRva+ordinalIndex*4,4,'export-address').readUInt32LE(0);assert.ok(rva<pe.size_of_image);const forwarded=rva>=exportRva&&rva-exportRva<exportSize;
      report.candidates.push({prefix,name_index:index,decorated_name:item.name,ordinal:pe.ordinal_base+ordinalIndex,rva:hex(rva),preferred_va:rva&&!forwarded?hex(0x180000000n+BigInt(rva)):null,forwarded,
        pdata:rva&&!forwarded?boundary(rva):{status:forwarded?'UnknownForwardedExport':'UnknownNullExport'}});}}
  report.status='metadata-only-completed';
}catch(error){failure=error;report.status='failed';report.failure=String(error);}
finally{
  report.encountered_names=[...observed.values()].sort((a,b)=>a.index-b.index);
  if(fd!==undefined){try{if(admitted){report.sha256_after=fingerprint('after');const after=fs.fstatSync(fd);report.source_stat_stable=after.isFile()&&after.size===initialStat.size&&after.dev===initialStat.dev&&after.ino===initialStat.ino&&after.mtimeMs===initialStat.mtimeMs&&after.ctimeMs===initialStat.ctimeMs;
      report.source_hash_preserved=report.sha256_before===expectedHash&&report.sha256_after===expectedHash;assert.ok(report.source_stat_stable&&report.source_hash_preserved,'Original source stability failed');}}
    catch(error){failure=failure||error;report.status='failed';report.final_hash_failure=String(error);}
    try{fs.closeSync(fd);report.source_closed=true;}catch(error){failure=failure||error;report.status='failed';report.close_failure=String(error);}}
  report.finished_utc=new Date().toISOString();const bytes=Buffer.from(JSON.stringify(report,null,2)+'\n');assert.ok(bytes.length<=limits.report_bytes);checkAncestors(outputDirectory,false);fs.writeFileSync(output,bytes,{flag:'wx'});
}
console.log(JSON.stringify({status:report.status,metadata_bytes:report.new_metadata_bytes,candidates:report.candidates.map(c=>({name:c.decorated_name,va:c.preferred_va,boundary:c.pdata.status})),output:path.relative(root,output).replaceAll('\\','/')}));
if(failure)process.exitCode=1;
