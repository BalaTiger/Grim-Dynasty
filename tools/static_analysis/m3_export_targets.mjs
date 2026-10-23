// Fixed M3 named-export lookup only. Never loads a DLL, decodes code, or starts a backend.
// The only whole-file reads are two 64 KiB streaming SHA256 passes.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

const planned=process.argv.length!==2;
if(planned){assert.equal(process.argv.length,6,'Use --plan <fixed relative JSON> --plan-sha256 <hash>');
  assert.equal(process.argv[2],'--plan');assert.equal(process.argv[4],'--plan-sha256');assert.match(process.argv[5],/^[a-f0-9]{64}$/);}
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const expectedRoot=path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty');
assert.equal(root.toLowerCase(),expectedRoot.toLowerCase(),'Wrong workspace');
const target='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const outDir=path.join(root,'docs/m3-evidence');
let outputRelative='docs/m3-evidence/export-targets.json',planEvidence=null,expectedSourceBytes=null;
let queries={
  equipment:['?AutoInsertItem@EquipmentCtrl@GAME@@','?InsertItem@EquipmentCtrl@GAME@@',
    '?EquipItem@EquipmentCtrl@GAME@@','?UnequipItem@EquipmentCtrl@GAME@@','?RemoveItem@EquipmentCtrl@GAME@@'],
  trade:['?BuyItem@','?SellItem@'],
  character_damage_death:['?TakeDamage@Character@GAME@@','?ApplyDamage@Character@GAME@@',
    '?ReceiveDamage@Character@GAME@@','?Die@Character@GAME@@','?Death@Character@GAME@@','?OnDeath@Character@GAME@@'],
};
const limits={source_bytes:64*1024*1024,hash_chunk_bytes:65536,metadata_bytes:2*1024*1024,
  metadata_operations:16384,single_read_bytes:4096,header_end:65536,sections:96,
  exports:1000000,names_cached:1024,name_bytes:1024,pdata_bytes:2*1024*1024,
  search_steps:32,candidates_per_prefix:2,candidates_per_category:8,report_bytes:256*1024};
if(planned){
  // Fixed reviewed profile; plan text cannot select another source, prefix,
  // arbitrary output, expanded budget, native program, or a code/body read.
  const relative='docs/m3-evidence/plans/character-input-targets.json';
  assert.equal(process.argv[3],relative,'Only the reviewed character-input plan is accepted');
  const planFile=path.join(root,relative);
  for(const p of [root,path.join(root,'docs'),outDir,path.join(outDir,'plans'),planFile]){
    const s=fs.lstatSync(p);assert.ok(!s.isSymbolicLink(),'Redirected plan path');
    assert.ok(p===planFile?s.isFile():s.isDirectory(),'Invalid plan ancestor');
    assert.equal(fs.realpathSync(p).toLowerCase(),p.toLowerCase(),'Redirected plan ancestor');
  }
  assert.ok(fs.statSync(planFile).size<=16384,'Plan exceeds 16 KiB');
  const bytes=fs.readFileSync(planFile);assert.ok(bytes.length<=16384);
  const sha256=createHash('sha256').update(bytes).digest('hex');assert.equal(sha256,process.argv[5],'Reviewed plan fingerprint changed');
  const plan=JSON.parse(bytes);
  assert.deepEqual(Object.keys(plan).sort(),['schema','id','purpose','source','queries','limits','mode','output','automatic_prefix_expansion','execution_requires_separate_review_and_approval'].sort());
  assert.equal(plan.schema,'grim-dynasty-m3-fixed-export-plan-v1');assert.equal(plan.id,'character-input-targets');
  assert.equal(typeof plan.purpose,'string');assert.ok(plan.purpose.length<=2048);
  assert.deepEqual(plan.source,{path:target,sha256:expected,bytes:11385856});
  assert.deepEqual(plan.queries,{character_input:['?Load@Character@GAME@@','?Attack@Character@GAME@@']});
  assert.deepEqual(plan.limits,{metadata_bytes:32768,metadata_operations:4096,names_cached:128,candidates_per_prefix:2,candidates_total:4,report_bytes:65536});
  assert.equal(plan.mode,'metadata-only');assert.equal(plan.output,'docs/m3-evidence/character-input-targets.json');
  assert.equal(plan.automatic_prefix_expansion,false);assert.equal(plan.execution_requires_separate_review_and_approval,true);
  queries=plan.queries;outputRelative=plan.output;expectedSourceBytes=plan.source.bytes;
  limits.metadata_bytes=plan.limits.metadata_bytes;limits.metadata_operations=plan.limits.metadata_operations;
  limits.names_cached=plan.limits.names_cached;limits.candidates_per_category=4;limits.report_bytes=plan.limits.report_bytes;
  planEvidence={path:relative,sha256,id:plan.id,mode:plan.mode,candidates_total_limit:4,
    automatic_prefix_expansion:false,reader_source_sha256:createHash('sha256').update(fs.readFileSync(fileURLToPath(import.meta.url))).digest('hex'),
    prior_reader_source_sha256:'4bf013c48819658641b5f57e77b871e6da562b6bd90608b93c8bac4b6a72f506'};
}
const output=path.join(root,outputRelative);
const hex=n=>'0x'+n.toString(16);
const inside=(offset,count,size)=>Number.isSafeInteger(offset)&&Number.isSafeInteger(count)&&
  offset>=0&&count>0&&offset<=size&&count<=size-offset;
function checkOutput(){
  assert.ok(output.startsWith(root+path.sep),'Output must stay in workspace');
  assert.ok(!fs.existsSync(output),'Refuse to overwrite existing evidence');
  for(const directory of [root,path.join(root,'docs'),outDir]){
    if(!fs.existsSync(directory)){assert.equal(directory,outDir);continue;}
    const stat=fs.lstatSync(directory);
    assert.ok(stat.isDirectory()&&!stat.isSymbolicLink(),'Output ancestor must be a real directory');
    assert.equal(fs.realpathSync(directory).toLowerCase(),directory.toLowerCase(),'Redirected output ancestor');
  }
}
checkOutput();
const result={schema:'grim-dynasty-m3-fixed-export-targets-v1',started_utc:new Date().toISOString(),
  target,expected_sha256:expected,output_relative:outputRelative,limits,queries,
  target_open_mode:'r',target_loaded_or_executed:false,code_bytes_interpreted:0,
  backend_started:false,whole_export_name_table_scanned:false,whole_pdata_table_scanned:false,
  metadata_bytes:0,metadata_operations:0,metadata_by_kind:{},categories:{},
  limitations:[
    'Queries are candidate names, not claims that these exports exist or that behavior has been recovered.',
    'Only fixed prefixes are searched in the sorted ASCII export-name table; only encountered ordering is checked.',
    'An unmatched prefix means Unknown, not absence of unexported, differently named or inlined behavior.',
    'Only containing .pdata entries for selected export RVAs are read. The range is [begin,end), not proof of a complete logical function.',
    'Missing .pdata entries, interior export aliases and forwarded exports remain explicit unknown boundary cases.',
    'Full-file SHA256 passes include uninterpreted code bytes. All semantic reads are PE headers, export metadata and .pdata only.',
    'No damage formula, death-event producer, equipment policy, trade price, rollback, runtime ABI or compatibility is established.',
  ]};
if(planEvidence)result.plan=planEvidence;
function hashPath(){
  const fd=fs.openSync(target,'r'),buffer=Buffer.alloc(limits.hash_chunk_bytes),h=createHash('sha256');
  try{const size=fs.fstatSync(fd).size;assert.ok(size>0&&size<=limits.source_bytes);let offset=0;
    while(offset<size){const n=fs.readSync(fd,buffer,0,Math.min(buffer.length,size-offset),offset);assert.ok(n>0);h.update(buffer.subarray(0,n));offset+=n;}
    return {sha256:h.digest('hex'),bytes:size};
  }finally{fs.closeSync(fd);}
}
const before=hashPath();result.sha256_before=before.sha256;result.source_bytes=before.bytes;
assert.equal(before.sha256,expected,'Target fingerprint mismatch; no metadata read');
if(expectedSourceBytes!==null)assert.equal(before.bytes,expectedSourceBytes,'Target size differs from reviewed plan');
const fd=fs.openSync(target,'r');
try{
  const stat=fs.fstatSync(fd);assert.ok(stat.isFile()&&stat.size===before.bytes);
  function read(offset,count,kind){
    assert.ok(inside(offset,count,stat.size)&&count<=limits.single_read_bytes,'Invalid bounded metadata read');
    assert.ok(++result.metadata_operations<=limits.metadata_operations,'Metadata operation limit');
    assert.ok(result.metadata_bytes+count<=limits.metadata_bytes,'Metadata byte limit');
    result.metadata_bytes+=count;result.metadata_by_kind[kind]=(result.metadata_by_kind[kind]||0)+count;
    const b=Buffer.alloc(count);let got=0;while(got<count){const n=fs.readSync(fd,b,got,count-got,offset+got);assert.ok(n>0);got+=n;}return b;
  }
  const dos=read(0,64,'header');assert.equal(dos.readUInt16LE(0),0x5a4d);
  const pe=dos.readUInt32LE(60);assert.ok(pe>=64&&pe<=limits.header_end-24);
  const coff=read(pe,24,'header');assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
  const count=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);
  assert.ok(count>0&&count<=limits.sections&&optSize>=144&&optSize<=4096&&pe+24+optSize+count*40<=limits.header_end);
  const opt=read(pe+24,optSize,'header');assert.equal(opt.readUInt16LE(0),0x20b);assert.ok(opt.readUInt32LE(108)>=4);
  const imageBase=opt.readBigUInt64LE(24);assert.equal(imageBase,0x180000000n);
  const sections=Array.from({length:count},(_,i)=>{const b=read(pe+24+optSize+i*40,40,'sections');
    return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),size:b.readUInt32LE(16),raw:b.readUInt32LE(20)};});
  function mapped(rva,length){
    const hit=sections.filter(s=>inside(rva-s.rva,length,s.size));assert.equal(hit.length,1,'RVA must have one raw section mapping');
    const s=hit[0],offset=s.raw+rva-s.rva;assert.ok(inside(offset,length,stat.size));return {offset,available:s.size-(rva-s.rva),section:s.name};
  }
  const rvaRead=(rva,length,kind)=>read(mapped(rva,length).offset,length,kind);
  const expRva=opt.readUInt32LE(112),expSize=opt.readUInt32LE(116);assert.ok(expRva&&expSize>=40);
  const exp=rvaRead(expRva,40,'export-directory'),ordinalBase=exp.readUInt32LE(16),functions=exp.readUInt32LE(20),names=exp.readUInt32LE(24);
  assert.ok(functions>0&&functions<=limits.exports&&names<=limits.exports);
  const eat=exp.readUInt32LE(28),npt=exp.readUInt32LE(32),ord=exp.readUInt32LE(36);
  mapped(eat,functions*4);if(names){mapped(npt,names*4);mapped(ord,names*2);}
  const pdataRva=opt.readUInt32LE(136),pdataSize=opt.readUInt32LE(140);
  assert.ok(pdataSize>0&&pdataSize%12===0&&pdataSize<=limits.pdata_bytes);
  const pdata=mapped(pdataRva,pdataSize);assert.equal(pdata.section,'.pdata');
  result.named_export_count=names;result.exception_entry_count=pdataSize/12;result.preferred_image_base=hex(imageBase);
  const cache=new Map();
  function nameAt(index){
    assert.ok(index>=0&&index<names);if(cache.has(index))return cache.get(index);
    assert.ok(cache.size<limits.names_cached,'Distinct name limit');
    const rva=rvaRead(npt+4*index,4,'export-name-pointers').readUInt32LE(0),parts=[];
    for(let used=0;used<limits.name_bytes;){
      const m=mapped(rva+used,1),n=Math.min(32,m.available,limits.name_bytes-used),b=read(m.offset,n,'export-name-ascii');
      const end=b.indexOf(0),part=end<0?b:b.subarray(0,end);for(const ch of part)assert.ok(ch<128,'Non-ASCII export name');parts.push(part);used+=n;
      if(end>=0){const value={name:Buffer.concat(parts).toString('ascii'),name_rva:rva};cache.set(index,value);
        for(const [other,v] of cache)if(other!==index)assert.ok(other<index?v.name<value.name:v.name>value.name,'Encountered export names are not strictly sorted');
        return value;}
    }throw Error('Export name exceeds bound');
  }
  function lowerBound(value){let lo=0,hi=names,steps=0;while(lo<hi){assert.ok(++steps<=limits.search_steps);const mid=Math.floor((lo+hi)/2);if(nameAt(mid).name<value)lo=mid+1;else hi=mid;}return lo;}
  function boundary(rva){
    let lo=0,hi=pdataSize/12-1,steps=0;
    while(lo<=hi){assert.ok(++steps<=limits.search_steps);const index=Math.floor((lo+hi)/2),b=read(pdata.offset+index*12,12,'pdata-selected-search');
      const begin=b.readUInt32LE(0),end=b.readUInt32LE(4);assert.ok(begin<end,'Invalid .pdata range');
      if(rva<begin)hi=index-1;else if(rva>=end)lo=index+1;else return {status:begin===rva?'ExactEntry':'InteriorExportAlias',index,
        begin_rva:hex(begin),end_rva_exclusive:hex(end),begin_va:hex(imageBase+BigInt(begin)),end_va_exclusive:hex(imageBase+BigInt(end)),
        bytes:end-begin,unwind_info_rva:hex(b.readUInt32LE(8)),logical_function_complete:false};
    }return {status:'UnknownNoContainingPdata',begin_rva:null,end_rva_exclusive:null};
  }
  for(const [category,prefixes] of Object.entries(queries)){
    const group={status:'UnknownNoMatchingExport',searches:[],candidates:[]};result.categories[category]=group;
    for(const prefix of prefixes){
      const first=lowerBound(prefix),last=lowerBound(prefix+'\x7f'),matches=last-first;
      const emit=Math.min(matches,limits.candidates_per_prefix,limits.candidates_per_category-group.candidates.length);
      group.searches.push({prefix,matching_name_range:[first,last],matches,emitted:emit,status:matches?'NamedExportsLocated':'UnknownNoMatchingExport',omitted_due_to_fixed_limits:matches-emit});
      for(let index=first;index<first+emit;index++){
        const item=nameAt(index);assert.ok(item.name.startsWith(prefix));
        const ordinalIndex=rvaRead(ord+index*2,2,'export-ordinals').readUInt16LE(0);assert.ok(ordinalIndex<functions);
        const rva=rvaRead(eat+ordinalIndex*4,4,'export-addresses').readUInt32LE(0),forwarded=rva>=expRva&&rva-expRva<expSize;
        group.candidates.push({decorated_name:item.name,query_prefix:prefix,name_index:index,ordinal:ordinalBase+ordinalIndex,
          rva:hex(rva),preferred_va:rva&&!forwarded?hex(imageBase+BigInt(rva)):null,forwarded,
          pdata:rva&&!forwarded?boundary(rva):{status:forwarded?'UnknownForwardedExport':'UnknownNullExport',begin_rva:null,end_rva_exclusive:null}});
      }
      if(matches)group.status='NamedCandidatesOnly';
    }
  }
  result.distinct_export_names_read=cache.size;
}finally{fs.closeSync(fd);}
const after=hashPath();result.sha256_after=after.sha256;
assert.equal(after.bytes,before.bytes,'Source size changed');assert.equal(after.sha256,expected,'Target changed during metadata lookup');
result.fingerprint_bytes=before.bytes+after.bytes;result.finished_utc=new Date().toISOString();result.status='MetadataOnlyCompleted';
const report=JSON.stringify(result,null,2)+'\n';assert.ok(Buffer.byteLength(report,'utf8')<=limits.report_bytes);
checkOutput();if(!fs.existsSync(outDir))fs.mkdirSync(outDir);checkOutput();
fs.writeFileSync(output,report,{encoding:'utf8',flag:'wx'});
console.log(JSON.stringify({status:result.status,output:outputRelative,metadata_bytes:result.metadata_bytes,
  categories:Object.fromEntries(Object.entries(result.categories).map(([k,v])=>[k,{status:v.status,candidates:v.candidates.length}]))}));
