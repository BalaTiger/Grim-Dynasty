// Fixed, read-only ARZ string-pool preflight. Importing this module performs no file reads.
// No record payload extraction/decompression, native code execution, or archive mutation.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const source='D:/Steam/steamapps/common/Grim Dawn/database/database.arz';
const expectedSha='9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603';
const expectedSize=58529720,tableOffset=54551066,tableBytes=3978638;
const expectedCount=82999,expectedPayload=3646638;
const maxArrayPayloadBytes=tableBytes+expectedCount*12;
const exampleLimit=8,exampleIdLimit=8,exampleByteLimit=32;
const digest=b=>createHash('sha256').update(b).digest('hex');

// These are inspection budgets, not claims about native format limits or total RSS.
// The borrowed Buffer must remain stable throughout this synchronous analysis.
export function analyzePoolWire(wire){
 assert(Buffer.isBuffer(wire),'Pool wire must be a Buffer');
 assert(wire.length>=4&&wire.length<=tableBytes,'Pool wire byte budget');
 const count=wire.readUInt32LE(0);
 assert(count<=expectedCount,'Pool string count budget');
 assert(4+count*4<=wire.length,'Missing pool length prefixes');
 assert(wire.length+count*12<=maxArrayPayloadBytes,'Pool array payload budget');
 const offsets=new Uint32Array(count),lengths=new Uint32Array(count),sortedIds=new Uint32Array(count);
 let cursor=4,payloadBytes=0,maxLength=0,maxId=null,emptyCount=0;
 const emptyIds=[];
 for(let id=0;id<count;id++){
  assert(cursor+4<=wire.length,'Missing string length prefix');
  const length=wire.readUInt32LE(cursor);cursor+=4;
  assert(length<=wire.length-cursor,'Truncated string bytes');
  offsets[id]=cursor;lengths[id]=length;sortedIds[id]=id;
  if(maxId===null||length>maxLength){maxLength=length;maxId=id;}
  if(length===0){emptyCount++;if(emptyIds.length<exampleIdLimit)emptyIds.push(id);}
  cursor+=length;payloadBytes+=length;
 }
 assert.equal(cursor,wire.length,'Trailing string-table bytes');
 assert(payloadBytes<=expectedPayload,'Pool payload byte budget');
 function comparePayload(a,b){
  const ao=offsets[a],bo=offsets[b],al=lengths[a],bl=lengths[b],n=Math.min(al,bl);
  for(let i=0;i<n;i++){const difference=wire[ao+i]-wire[bo+i];if(difference!==0)return difference;}
  return al-bl;
 }
 function compareIds(a,b){return comparePayload(a,b)||a-b;}
 function swap(a,b){const id=sortedIds[a];sortedIds[a]=sortedIds[b];sortedIds[b]=id;}
 function siftDown(index,endExclusive){
  while(index*2+1<endExclusive){
   let child=index*2+1;
   if(child+1<endExclusive&&compareIds(sortedIds[child],sortedIds[child+1])<0)child++;
   if(compareIds(sortedIds[index],sortedIds[child])>=0)return;
   swap(index,child);index=child;
  }
 }
 // In-place heapsort: no per-string JS strings, duplicate byte copies, or sort workspace array.
 for(let i=Math.floor(count/2)-1;i>=0;i--)siftDown(i,count);
 for(let end=count-1;end>0;end--){swap(0,end);siftDown(0,end);}
 let groups=0,duplicateGroups=0,excess=0,duplicateParticipants=0;
 const examples=[];
 for(let first=0;first<count;){
  let last=first+1;
  while(last<count&&comparePayload(sortedIds[first],sortedIds[last])===0)last++;
  groups++;
  if(last-first>1){
   duplicateGroups++;excess+=last-first-1;duplicateParticipants+=last-first;
   if(examples.length<exampleLimit){
    const firstId=sortedIds[first],ids=[];
    for(let i=first;i<Math.min(last,first+exampleIdLimit);i++)ids.push(sortedIds[i]);
    examples.push({first_original_id:firstId,occurrences:last-first,original_ids_sample:ids,
     stored_length:lengths[firstId],
     bytes_prefix_hex:wire.subarray(offsets[firstId],offsets[firstId]+Math.min(lengths[firstId],exampleByteLimit)).toString('hex'),
     bytes_prefix_truncated:lengths[firstId]>exampleByteLimit});
   }
  }
  first=last;
 }
 const summary={
  string_count:count,string_table_bytes:wire.length,payload_bytes:payloadBytes,
  string_table_sha256:digest(wire),unique_value_count:groups,
  duplicate_group_count:duplicateGroups,duplicate_excess_entries:excess,entries_in_duplicate_groups:duplicateParticipants,
  duplicate_examples:examples,duplicate_examples_limit:exampleLimit,duplicate_ids_per_example_limit:exampleIdLimit,
  empty_count:emptyCount,empty_ids_sample:emptyIds,max_string_bytes:maxLength,max_string_id:maxId,
  source_ids_preserved_under_fresh_exact_byte_import:excess===0,
  empty_postcondition_would_append:emptyCount===0,
  original_id_policy:'Offsets and lengths remain indexed by original ID; only a separate Uint32 ID permutation is sorted',
  comparison:'Full raw bytes, case-sensitive, NUL preserved, unsigned-byte lexicographic; original-ID tie break',
  interpretation:'ID preservation refers only to fresh first-occurrence exact-byte deduplication; it does not establish native archive/context/cache compatibility',
  array_payload_bytes:{wire:wire.length,offsets:offsets.byteLength,lengths:lengths.byteLength,sorted_ids:sortedIds.byteLength,
   total:wire.length+offsets.byteLength+lengths.byteLength+sortedIds.byteLength},
  sort_algorithm:'In-place heapsort; no array workspace; bounded duplicate examples only'
 };
 return {summary,offsets,lengths,sortedIds};
}

function hashOpened(fd,buffer){
 const h=createHash('sha256');let position=0,n;
 while((n=fs.readSync(fd,buffer,0,buffer.length,position))>0){h.update(buffer.subarray(0,n));position+=n;}
 return {sha256:h.digest('hex'),bytes:position};
}
function readExact(fd,offset,length){
 assert(Number.isInteger(offset)&&Number.isInteger(length)&&offset>=0&&length>=0&&offset+length<=expectedSize);
 const bytes=Buffer.alloc(length);let done=0;
 while(done<length){const n=fs.readSync(fd,bytes,done,length-done,offset+done);assert(n>0,'Unexpected EOF');done+=n;}
 return bytes;
}
function poolEntry(pool,wire,id,expectedAscii){
 assert(id<pool.summary.string_count,'Required annotation ID outside pool');
 const expected=Buffer.from(expectedAscii,'ascii'),offset=pool.offsets[id],length=pool.lengths[id];
 const matches=length===expected.length&&wire.subarray(offset,offset+length).equals(expected);
 return {original_id:id,expected_ascii:expectedAscii,stored_length:length,exact_bytes_match:matches,
  observed_bytes_prefix_hex:wire.subarray(offset,offset+Math.min(length,exampleByteLimit)).toString('hex')};
}
function runFixedPreflight(){
 assert.equal(process.argv.length,2,'No arguments accepted');
 const out=path.join(root,'docs/object-lifecycle/official-cache-chain-pool-preflight');
 const reportPath=path.join(out,'official-pool-report.json');
 assert(!fs.existsSync(reportPath),'Preserve earlier official report');
 fs.mkdirSync(out,{recursive:true});
 // Reserve this report before opening the archive; concurrent runs cannot overwrite it.
 const outputFd=fs.openSync(reportPath,'wx');
 const report={status:'started',started_utc:new Date().toISOString(),source,
  expected_source_sha256:expectedSha,expected_source_bytes:expectedSize,
  expected_string_table:{offset:tableOffset,bytes:tableBytes,count:expectedCount,payload_bytes:expectedPayload},
  source_open_flags:'O_RDONLY; Node fs API does not expose .NET FileShare.Read exclusivity',
  stability_contract:'Source is assumed stable; same-handle fstat and before/after fixed SHA detect observed changes, not every possible transient concurrent mutation',
  full_file_hash:{chunk_bytes:65536,reads_all_file_bytes:true},
  semantic_reads:'Only 24-byte ARZ header and exact string-table region; full-file hashing separately traverses all bytes',
  record_table_read_or_decoded:false,record_payload_extracted_or_decompressed:false,
  source_modified:false,target_loaded_or_executed:false,backend_started:false,
  official_archive_check_executed:true,
  limits:{max_strings:expectedCount,max_string_table_bytes:tableBytes,max_payload_bytes:expectedPayload,
   max_explicit_array_payload_bytes:maxArrayPayloadBytes,
   explicit_arrays_plus_hash_buffer_header_count_bytes:maxArrayPayloadBytes+65536+24+4,
   process_memory_cap_claimed:false,allocation_notes:'Excludes Node/runtime/crypto/Buffer-object overhead, report objects and caller retention; heap sort uses no auxiliary array'},
  annotation_scope:'ID13 Class and ID28355 QuestItem are pool annotations only; no record body or key/value association is inferred'};
 let fd,failure;
 const hashBuffer=Buffer.alloc(65536);
 try{
  fd=fs.openSync(source,fs.constants.O_RDONLY);
  const before=fs.fstatSync(fd);assert(before.isFile(),'Expected an ordinary file');
  report.size_before=before.size;assert.equal(before.size,expectedSize);
  report.source_hash_before=hashOpened(fd,hashBuffer);
  assert.equal(report.source_hash_before.bytes,expectedSize);assert.equal(report.source_hash_before.sha256,expectedSha);
  const header=readExact(fd,0,24);
  assert.equal(header.readUInt16LE(0),2);assert.equal(header.readUInt16LE(2),3);
  assert.equal(header.readUInt32LE(16),tableOffset);assert.equal(header.readUInt32LE(20),tableBytes);
  report.header={bytes_hex:header.toString('hex'),marker:2,version:3,
   record_table_offset_header_only:header.readUInt32LE(4),record_table_size_header_only:header.readUInt32LE(8),
   record_count_header_only:header.readUInt32LE(12),string_table_offset:tableOffset,string_table_bytes:tableBytes};
  const countBytes=readExact(fd,tableOffset,4);assert.equal(countBytes.readUInt32LE(0),expectedCount);
  assert.equal(tableBytes-4-4*expectedCount,expectedPayload);
  assert(tableOffset+tableBytes<=expectedSize);
  const wire=readExact(fd,tableOffset,tableBytes);
  const pool=analyzePoolWire(wire);
  assert.equal(pool.summary.string_count,expectedCount);assert.equal(pool.summary.payload_bytes,expectedPayload);
  report.pool=pool.summary;
  report.pool_annotations=[poolEntry(pool,wire,13,'Class'),poolEntry(pool,wire,28355,'QuestItem')];
  assert(report.pool_annotations.every(x=>x.exact_bytes_match),'Fixed pool annotations differ');
  report.status='completed';
 }catch(error){failure=error;report.status='failed';report.failure=String(error);}
 finally{
  if(fd!==undefined){
   try{
    report.size_after=fs.fstatSync(fd).size;
    report.source_hash_after=hashOpened(fd,hashBuffer);
    report.source_preserved=report.size_after===expectedSize&&report.source_hash_before?.sha256===expectedSha&&
     report.source_hash_after.bytes===expectedSize&&report.source_hash_after.sha256===expectedSha;
    if(!report.source_preserved){report.status='failed';failure??=new Error('Source hash/size preservation failed');}
   }catch(error){report.status='failed';report.final_verification_failure=String(error);failure??=error;}
   finally{fs.closeSync(fd);}
  }
  report.finished_utc=new Date().toISOString();report.controller_resource_usage=process.resourceUsage();
  try{fs.writeFileSync(outputFd,JSON.stringify(report,null,2)+'\n');}finally{fs.closeSync(outputFd);}
 }
 if(failure)throw failure;
 console.log(JSON.stringify({status:report.status,string_count:report.pool.string_count,
  duplicate_excess_entries:report.pool.duplicate_excess_entries,empty_count:report.pool.empty_count,
  max_string_bytes:report.pool.max_string_bytes,
  source_ids_preserved_under_fresh_exact_byte_import:report.pool.source_ids_preserved_under_fresh_exact_byte_import,
  source_preserved:report.source_preserved,report:reportPath}));
}

// The separate synthetic-fixture runner imports analyzePoolWire without touching the archive.
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))runFixedPreflight();
