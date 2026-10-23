// Preparation is not execution permission. Importing only defines this function.
// After separate review/approval: import this module and explicitly call
// inspectNavManagerUpdateBinding('<reviewed adjacent plan SHA256>').
// No subprocess, backend, DLL load, executable body read or automatic expansion.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectNavManagerUpdateBinding(planSha256) {
  const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
  assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
  assert.match(planSha256,/^[a-f0-9]{64}$/);
  const planRel='tools/static_analysis/read_r1_navmanager_update_binding.plan.json';
  const outputRel='docs/reverse-client/r1-navmanager-update-binding';
  const proof={path:'docs/phase-1/boundary-audit/module-boundaries.json',sha256:'d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'};
  const source={module:'Engine.dll',path:'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',bytes:4485632,sha256:'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'};
  const candidate={prefix:'?Update@NavManager@GAME@@',basis:'Unproven method-name candidate within the observed NavManager class; no Update import or complete decorated name is claimed',maximum_bindings:2};
  const classAnchor={consumer:'x64/Game.dll',module:'Engine.dll',import_index:487,name:'?FindPath@NavManager@GAME@@QEAA_NAEBVWorldVec3@2@0MPEAV32@PEAIPEAMPEAV?$vector@VWorldVec3@GAME@@@mem@@PEAVVec3@2@_N@Z'};
  const limits={metadata_bytes:16384,metadata_operations:2048,distinct_names:64,name_bytes:512,binary_search_steps:32,maximum_bindings:2,following_boundary_names:1,code_bytes:0,subprocesses:0};
  const hex=n=>'0x'+n.toString(16),sha=b=>createHash('sha256').update(b).digest('hex');
  const fits=(at,n,total)=>Number.isSafeInteger(at)&&Number.isSafeInteger(n)&&at>=0&&n>0&&at<=total&&n<=total-at;
  function keys(o,names){assert.deepEqual(Object.keys(o).sort(),names.split(' ').sort());}
  // Reuse the existing region-load reader's path guards. Node does not expose
  // every Windows reparse tag; the established Get-Item Attributes preflight is
  // still required before original-file execution. No security changes occur.
  function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);
    assert.ok(!s.isSymbolicLink(),'Links/junctions refused');assert.ok(first&&file?s.isFile():s.isDirectory());
    assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
    const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
  function local(rel){assert.ok(/^[a-z0-9_./-]+$/.test(rel)&&!rel.split('/').some(s=>!s||s==='.'||s==='..'));
    const p=path.resolve(root,rel);assert.ok(p.startsWith(root+path.sep));return p;}
  function saved(rel,max){const p=local(rel);normal(p,true);assert.ok(fs.statSync(p).size<=max);return fs.readFileSync(p);}
  function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing evidence preserved: '+p);}
  function digest(fd,n){const h=createHash('sha256'),buffer=Buffer.alloc(65536);for(let at=0;at<n;){
    const got=fs.readSync(fd,buffer,0,Math.min(buffer.length,n-at),at);assert.ok(got>0);h.update(buffer.subarray(0,got));at+=got;}return h.digest('hex');}
  function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}

  // The caller pins the reviewed plan. Exact fields separately forbid widening
  // the source, name prefix, proof or budgets by substituting a different plan.
  const planBytes=saved(planRel,12288);assert.equal(sha(planBytes),planSha256);const plan=JSON.parse(planBytes);
  keys(plan,'schema purpose proof source candidate class_anchor limits output automatic_target_expansion execution_requires_separate_review_and_approval');
  assert.equal(plan.schema,'grim-dynasty-r1-navmanager-update-prefix-plan-v1');assert.ok(typeof plan.purpose==='string'&&plan.purpose.length<=1024);
  assert.deepEqual(plan.proof,proof);assert.deepEqual(plan.source,source);assert.deepEqual(plan.candidate,candidate);
  assert.deepEqual(plan.class_anchor,classAnchor);assert.deepEqual(plan.limits,limits);assert.equal(plan.output,outputRel);
  assert.equal(plan.automatic_target_expansion,false);assert.equal(plan.execution_requires_separate_review_and_approval,true);
  const proofBytes=saved(proof.path,1048576);assert.equal(sha(proofBytes),proof.sha256);const inventory=JSON.parse(proofBytes);
  assert.ok(Array.isArray(inventory));const engines=inventory.filter(m=>m.relativePath==='x64/Engine.dll');assert.equal(engines.length,1);
  assert.equal(engines[0].sha256,source.sha256);assert.equal(engines[0].bytes,source.bytes);
  const consumers=inventory.filter(m=>m.relativePath===classAnchor.consumer);assert.equal(consumers.length,1);
  assert.equal(consumers[0].sha256,'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');assert.equal(consumers[0].bytes,11385856);
  const modules=consumers[0].importModules.filter(m=>m.module===classAnchor.module);assert.equal(modules.length,1);
  assert.equal(modules[0].symbols[classAnchor.import_index],classAnchor.name);
  assert.ok(!modules[0].symbols.some(name=>typeof name==='string'&&name.startsWith(candidate.prefix)),'Saved Game imports unexpectedly contain the candidate prefix; re-review the premise');
  normal(self,true);const out=local(outputRel);normal(path.dirname(out));absent(out);
  const report={schema:'grim-dynasty-r1-navmanager-update-prefix-v1',status:'Started',started_utc:new Date().toISOString(),
    plan:{path:planRel,sha256:sha(planBytes)},script_sha256:sha(fs.readFileSync(self)),proof,class_anchor:classAnchor,candidate,source,limits,
    source_open_mode:'r',source_closed:false,metadata_bytes:0,metadata_operations:0,metadata_by_kind:{},distinct_names_read:0,
    lookup:null,bindings:[],failures:[],code_bytes:0,subprocesses:0,original_loaded_or_executed:false,whole_export_table_scanned:false,pdata_read:false,
    limitations:[
      'Update is a candidate name only. The saved Game import proves NavManager/FindPath class spelling and Engine identity, not Update existence or behavior.',
      'Only one fixed prefix is searched; at most two matching names receive RVA bindings. A third matching name is a boundary observation only and stops expansion.',
      'UnknownNoPrefix describes this bounded export-metadata lookup under the PE ordered-name assumption, not absence of a runtime/internal Update implementation.',
      'Only encountered export-name ordering is checked; the whole export name table is not traversed.',
      'No code, .pdata, vtable, import table or callee is read. Executable export targets are mapped from PE headers without reading their code.',
      'Same-descriptor before/after SHA256 streams all original bytes in 64 KiB chunks without interpreting executable bytes; stat/hash guards are not an atomic snapshot or exclusive lock.',
      'Node path guards do not identify every Windows reparse tag; the normal external Attributes preflight remains required.'
    ]};
  let created=false,fd,initial;
  try{
    fs.mkdirSync(out);created=true;normal(out);
    normal(source.path,true);fd=fs.openSync(source.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);
    assert.ok(initial.isFile());assert.equal(initial.size,source.bytes);assert.ok(stable(initial,fs.statSync(source.path)));
    report.sha256_before=digest(fd,source.bytes);assert.equal(report.sha256_before,source.sha256);
    function read(offset,n,kind){assert.ok(fits(offset,n,source.bytes)&&n<=512);assert.ok(++report.metadata_operations<=limits.metadata_operations);
      assert.ok(report.metadata_bytes+n<=limits.metadata_bytes);report.metadata_bytes+=n;report.metadata_by_kind[kind]=(report.metadata_by_kind[kind]||0)+n;
      const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);return b;}
    const dos=read(0,64,'header');assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert.ok(pe>=64&&pe<=4072);
    const coff=read(pe,24,'header');assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
    const count=coff.readUInt16LE(6),os=coff.readUInt16LE(20);assert.ok(count>0&&count<=16&&os===240&&pe+24+os+count*40<=4096);
    const opt=read(pe+24,os,'header');assert.equal(opt.readUInt16LE(0),0x20b);assert.ok(opt.readUInt32LE(108)>=1);
    const imageBase=opt.readBigUInt64LE(24),imageSize=opt.readUInt32LE(56);assert.equal(imageBase,0x180000000n);
    const sections=Array.from({length:count},(_,i)=>{const b=read(pe+24+os+i*40,40,'section-header');return {
      name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),rva:b.readUInt32LE(12),raw_size:b.readUInt32LE(16),raw_offset:b.readUInt32LE(20),flags:b.readUInt32LE(36)};});
    function mapped(rva,n,metadata=true){assert.ok(fits(rva,n,imageSize));const hits=sections.filter(s=>fits(rva-s.rva,n,s.raw_size));
      assert.equal(hits.length,1,'Unique raw section mapping required');const section=hits[0],offset=section.raw_offset+rva-section.rva;
      assert.ok(fits(offset,n,source.bytes));if(metadata)assert.ok((section.flags&0x40000000)!==0&&(section.flags&0x20000000)===0,'Metadata must not map to executable bytes');
      return {section,offset,available:section.raw_size-(rva-section.rva)};}
    const rvaRead=(rva,n,kind)=>read(mapped(rva,n).offset,n,kind);
    const expRva=opt.readUInt32LE(112),expSize=opt.readUInt32LE(116);assert.ok(expRva>0&&expSize>=40);mapped(expRva,expSize);
    const exp=rvaRead(expRva,40,'export-directory'),ordinalBase=exp.readUInt32LE(16),functions=exp.readUInt32LE(20),names=exp.readUInt32LE(24);
    assert.ok(functions>0&&functions<=1000000&&names<=1000000);const eat=exp.readUInt32LE(28),npt=exp.readUInt32LE(32),ord=exp.readUInt32LE(36);
    mapped(eat,functions*4);if(names){mapped(npt,names*4);mapped(ord,names*2);}report.named_export_count=names;report.image_base=hex(imageBase);
    const cache=new Map();
    function nameAt(index){assert.ok(index>=0&&index<names);if(cache.has(index))return cache.get(index);assert.ok(cache.size<limits.distinct_names);
      const rva=rvaRead(npt+index*4,4,'name-pointer').readUInt32LE(0),parts=[];
      for(let used=0;used<limits.name_bytes;){const m=mapped(rva+used,1),n=Math.min(32,m.available,limits.name_bytes-used),b=read(m.offset,n,'export-name');
        const end=b.indexOf(0),part=end<0?b:b.subarray(0,end);for(const ch of part)assert.ok(ch<128,'ASCII export name required');parts.push(part);used+=n;
        if(end>=0){const name=Buffer.concat(parts).toString('ascii');for(const [other,value] of cache)assert.ok(other<index?value<name:value>name,'Encountered names not strictly sorted');
          cache.set(index,name);report.distinct_names_read=cache.size;return name;}}throw Error('Export name exceeds fixed bound');}
    let lo=0,hi=names,steps=0;while(lo<hi){assert.ok(++steps<=limits.binary_search_steps);const mid=Math.floor((lo+hi)/2);
      if(nameAt(mid)<candidate.prefix)lo=mid+1;else hi=mid;}
    const lower=lo<names?{name_index:lo,name:nameAt(lo)}:null;
    const previous=lo>0?{name_index:lo-1,name:nameAt(lo-1)}:null;
    if(previous)assert.ok(previous.name<candidate.prefix);if(lower)assert.ok(lower.name>=candidate.prefix);
    report.lookup={status:'UnknownNoPrefix',lower_bound_index:lo,binary_search_steps:steps,previous_name:previous,lower_bound_name:lower,following_boundary:null,more_prefix_matches_observed:false};
    if(lower&&lower.name.startsWith(candidate.prefix)){
      let index=lo;
      while(index<names&&report.bindings.length<limits.maximum_bindings){const name=nameAt(index);if(!name.startsWith(candidate.prefix))break;
        const ordinalIndex=rvaRead(ord+index*2,2,'export-ordinal').readUInt16LE(0);assert.ok(ordinalIndex<functions);
        const rva=rvaRead(eat+ordinalIndex*4,4,'export-address').readUInt32LE(0),forwarded=rva>=expRva&&rva-expRva<expSize;
        const found={name,name_index:index,status:rva===0?'UnknownNullExport':forwarded?'UnknownForwardedExport':'CandidateNamedExportLocated',
          ordinal:ordinalBase+ordinalIndex,rva:hex(rva),preferred_va:rva&&!forwarded?hex(imageBase+BigInt(rva)):null,forwarded,code_read:false};
        if(rva&&!forwarded){const m=mapped(rva,1,false);found.section=m.section.name;found.section_executable=(m.section.flags&0x20000000)!==0;}
        report.bindings.push(found);index++;
      }
      if(index<names){const name=nameAt(index);report.lookup.following_boundary={name_index:index,name,rva_bound:false};report.lookup.more_prefix_matches_observed=name.startsWith(candidate.prefix);}
      report.lookup.status=report.lookup.more_prefix_matches_observed?'PrefixMatchLimitReached':'CandidateNamedExportsLocated';
    }
    report.status='MetadataOnlyCompleted';
  }catch(e){report.failures.push(String(e));report.status='Failed';}
  finally{
    if(fd!==undefined){try{report.sha256_after=digest(fd,source.bytes);assert.equal(report.sha256_after,source.sha256);
        report.descriptor_stable=stable(initial,fs.fstatSync(fd));assert.ok(report.descriptor_stable);normal(source.path,true);
        report.path_stable=stable(initial,fs.statSync(source.path));assert.ok(report.path_stable);}
      catch(e){report.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('Close: '+e);}}}
    report.source_hash_preserved=report.sha256_before===source.sha256&&report.sha256_after===source.sha256;
    if(report.failures.length)report.status='Failed';report.finished_utc=new Date().toISOString();
    if(created){normal(out);const text=JSON.stringify(report,null,2)+'\n';assert.ok(Buffer.byteLength(text)<=65536);fs.writeFileSync(path.join(out,'inspection.json'),text,{flag:'wx'});}
  }
  if(report.status==='Failed')throw Error('Fixed metadata lookup failed; inspect '+outputRel+'/inspection.json: '+report.failures.join('; '));
  return {status:report.status,output:outputRel,metadata_bytes:report.metadata_bytes,source_hash_preserved:report.source_hash_preserved,lookup:report.lookup,bindings:report.bindings};
}
