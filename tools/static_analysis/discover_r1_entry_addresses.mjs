// Preparing a plan is not execution permission. No backend, code read or DLL load.
// node tools/static_analysis/discover_r1_entry_addresses.mjs --plan docs/reverse-client/r1-entry-addresses-plan.json --plan-sha256 <reviewed SHA256>
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const planRel='docs/reverse-client/r1-entry-addresses-plan.json',outputRel='docs/reverse-client/r1-entry-addresses';
assert.equal(process.argv.length,6);assert.equal(process.argv[2],'--plan');assert.equal(process.argv[3],planRel);
assert.equal(process.argv[4],'--plan-sha256');assert.match(process.argv[5],/^[a-f0-9]{64}$/);
const proof={path:'docs/phase-1/boundary-audit/module-boundaries.json',sha256:'d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'};
const sources=[{module:'Game.dll',path:'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',bytes:11385856,
  sha256:'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',targets:[
    {consumer:'x64/Grim Dawn.exe',name:'?HandleActionFromMouse@ControllerPlayer@GAME@@QEAA_N_N000AEBVWorldVec3@2@AEAIPEA_N@Z'}]},
  {module:'Engine.dll',path:'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',bytes:4485632,
    sha256:'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153',targets:[
      {consumer:'x64/Game.dll',name:'?FindPath@NavManager@GAME@@QEAA_NAEBVWorldVec3@2@0MPEAV32@PEAIPEAMPEAV?$vector@VWorldVec3@GAME@@@mem@@PEAVVec3@2@_N@Z'},
      {consumer:'x64/Game.dll',name:'?GetInputDevice@Engine@GAME@@QEAAPEAVInputDevice@2@XZ'},
      {consumer:'x64/Grim Dawn.exe',name:'?UpdateFromInput@WorldCamera@GAME@@QEAAXXZ'},
      {consumer:'x64/Grim Dawn.exe',name:'?GetRayThroughImagePoint@WorldCamera@GAME@@QEBA?AVWorldRay@2@AEBVVec2@2@AEBVViewport@2@@Z'},
      {consumer:'x64/Grim Dawn.exe',name:'?HandleMouseEvent@DisplayWidget@GAME@@UEAA_NAEBUMouseEvent@InputDevice@2@@Z'}]}];
const limits={metadata_bytes_per_module:32768,metadata_bytes_total:49152,metadata_operations_per_module:4096,
  names_per_module:128,name_bytes:1024,binary_search_steps:32,code_bytes:0,subprocesses:0};
const hex=n=>'0x'+n.toString(16),sha=b=>createHash('sha256').update(b).digest('hex');
const fits=(at,n,total)=>Number.isSafeInteger(at)&&Number.isSafeInteger(n)&&at>=0&&n>0&&at<=total&&n<=total-at;
function keys(o,names){assert.deepEqual(Object.keys(o).sort(),names.split(' ').sort());}
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);
  assert.ok(!s.isSymbolicLink(),'Links/junctions refused');assert.ok(first&&file?s.isFile():s.isDirectory());
  assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
  const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function local(rel){assert.ok(/^[a-z0-9./-]+$/.test(rel)&&!rel.split('/').some(s=>!s||s==='.'||s==='..'));
  const p=path.resolve(root,rel);assert.ok(p.startsWith(root+path.sep));return p;}
function saved(rel,max){const p=local(rel);normal(p,true);assert.ok(fs.statSync(p).size<=max);return fs.readFileSync(p);}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing evidence preserved: '+p);}
function digest(fd,n){const h=createHash('sha256'),buffer=Buffer.alloc(65536);for(let at=0;at<n;){
  const got=fs.readSync(fd,buffer,0,Math.min(buffer.length,n-at),at);assert.ok(got>0);h.update(buffer.subarray(0,got));at+=got;}return h.digest('hex');}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}

const planBytes=saved(planRel,12288);assert.equal(sha(planBytes),process.argv[5]);const plan=JSON.parse(planBytes);
keys(plan,'schema purpose proof sources limits output automatic_target_expansion execution_requires_separate_review_and_approval');
assert.equal(plan.schema,'grim-dynasty-r1-fixed-export-address-plan-v1');assert.ok(typeof plan.purpose==='string'&&plan.purpose.length<=1024);
assert.deepEqual(plan.proof,proof);assert.deepEqual(plan.sources,sources);assert.deepEqual(plan.limits,limits);assert.equal(plan.output,outputRel);
assert.equal(plan.automatic_target_expansion,false);assert.equal(plan.execution_requires_separate_review_and_approval,true);
const proofBytes=saved(proof.path,1048576);assert.equal(sha(proofBytes),proof.sha256);const inventory=JSON.parse(proofBytes);
assert.ok(Array.isArray(inventory));
for(const source of sources){const identities=inventory.filter(m=>m.relativePath==='x64/'+source.module);assert.equal(identities.length,1);
  assert.equal(identities[0].sha256,source.sha256);assert.equal(identities[0].bytes,source.bytes);
  for(const t of source.targets){const consumers=inventory.filter(m=>m.relativePath===t.consumer);assert.equal(consumers.length,1);
    const imported=consumers[0].importModules.filter(m=>m.module===source.module);assert.equal(imported.length,1);
    assert.ok(imported[0].symbols.includes(t.name),'Exact saved import is required');}}
normal(self,true);const out=local(outputRel);normal(path.dirname(out));absent(out);
const report={schema:'grim-dynasty-r1-fixed-export-addresses-v1',status:'Started',started_utc:new Date().toISOString(),
  plan:{path:planRel,sha256:sha(planBytes)},script_sha256:sha(fs.readFileSync(self)),proof,limits,metadata_bytes:0,modules:[],failures:[],
  code_bytes:0,subprocesses:0,originals_loaded_or_executed:false,whole_export_table_scanned:false,pdata_read:false,
  limitations:['Exact names are candidates anchored in saved imports; export identity does not establish implementation semantics.',
    'Only encountered export-name ordering is checked; no full name-table traversal is performed.',
    'No match, null or forwarded exports remain explicit unknowns. No automatic query expansion follows.',
    'No code, .pdata, unwind body, vtable, import table or callee is read; target section labels come from PE headers.',
    'Full same-descriptor before/after SHA256 passes use 64 KiB buffers; hashing does not interpret code bytes.',
    'Hash and stat checks do not provide an atomic snapshot or exclusive file lock.']};
let created=false;
try{
  fs.mkdirSync(out);created=true;normal(out);
  for(const source of sources){const result={source,status:'Started',source_open_mode:'r',source_closed:false,
      metadata_bytes:0,metadata_operations:0,metadata_by_kind:{},targets:[],failures:[]};report.modules.push(result);let fd,initial;
    try{
      normal(source.path,true);fd=fs.openSync(source.path,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);
      assert.ok(initial.isFile());assert.equal(initial.size,source.bytes);result.sha256_before=digest(fd,source.bytes);assert.equal(result.sha256_before,source.sha256);
      function read(offset,n,kind){assert.ok(fits(offset,n,source.bytes)&&n<=512);assert.ok(++result.metadata_operations<=limits.metadata_operations_per_module);
        assert.ok(result.metadata_bytes+n<=limits.metadata_bytes_per_module&&report.metadata_bytes+n<=limits.metadata_bytes_total);
        result.metadata_bytes+=n;report.metadata_bytes+=n;result.metadata_by_kind[kind]=(result.metadata_by_kind[kind]||0)+n;
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
      mapped(eat,functions*4);if(names){mapped(npt,names*4);mapped(ord,names*2);}result.named_export_count=names;result.image_base=hex(imageBase);
      const cache=new Map();
      function nameAt(index){assert.ok(index>=0&&index<names);if(cache.has(index))return cache.get(index);assert.ok(cache.size<limits.names_per_module);
        const rva=rvaRead(npt+index*4,4,'name-pointer').readUInt32LE(0),parts=[];
        for(let used=0;used<limits.name_bytes;){const m=mapped(rva+used,1),n=Math.min(32,m.available,limits.name_bytes-used),b=read(m.offset,n,'export-name');
          const end=b.indexOf(0),part=end<0?b:b.subarray(0,end);for(const ch of part)assert.ok(ch<128,'ASCII export name required');parts.push(part);used+=n;
          if(end>=0){const name=Buffer.concat(parts).toString('ascii');for(const [other,value] of cache)assert.ok(other<index?value<name:value>name,'Encountered names not strictly sorted');
            cache.set(index,name);return name;}}throw Error('Export name exceeds fixed bound');}
      for(const t of source.targets){let lo=0,hi=names,steps=0;while(lo<hi){assert.ok(++steps<=limits.binary_search_steps);const mid=Math.floor((lo+hi)/2);
          if(nameAt(mid)<t.name)lo=mid+1;else hi=mid;}
        const found={...t,status:'UnknownNoExactExport'};result.targets.push(found);
        if(lo===names||nameAt(lo)!==t.name)continue;
        const ordinalIndex=rvaRead(ord+lo*2,2,'export-ordinal').readUInt16LE(0);assert.ok(ordinalIndex<functions);
        const rva=rvaRead(eat+ordinalIndex*4,4,'export-address').readUInt32LE(0),forwarded=rva>=expRva&&rva-expRva<expSize;
        Object.assign(found,{status:rva===0?'UnknownNullExport':forwarded?'UnknownForwardedExport':'ExactNamedExportLocated',name_index:lo,
          ordinal:ordinalBase+ordinalIndex,rva:hex(rva),preferred_va:rva&&!forwarded?hex(imageBase+BigInt(rva)):null,forwarded});
        if(rva&&!forwarded){const m=mapped(rva,1,false);found.section=m.section.name;found.section_executable=(m.section.flags&0x20000000)!==0;found.code_read=false;}}
      result.distinct_names_read=cache.size;result.status='MetadataOnlyCompleted';
    }catch(e){result.failures.push(String(e));result.status='Failed';}
    finally{if(fd!==undefined){try{result.sha256_after=digest(fd,source.bytes);assert.equal(result.sha256_after,source.sha256);
          result.descriptor_stable=stable(initial,fs.fstatSync(fd));assert.ok(result.descriptor_stable);normal(source.path,true);
          result.path_stable=stable(initial,fs.statSync(source.path));assert.ok(result.path_stable);}
        catch(e){result.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);result.source_closed=true;}catch(e){result.failures.push('Close: '+e);}}}
      result.source_hash_preserved=result.sha256_before===source.sha256&&result.sha256_after===source.sha256;
      if(result.failures.length)result.status='Failed';}
    if(result.status==='Failed'){report.failures.push(source.module+': '+result.failures.join('; '));break;}
  }
  report.status=report.failures.length?'Failed':'MetadataOnlyCompleted';
}catch(e){report.failures.push(String(e));report.status='Failed';}
finally{report.finished_utc=new Date().toISOString();if(created){normal(out);const text=JSON.stringify(report,null,2)+'\n';assert.ok(Buffer.byteLength(text)<=65536);
    fs.writeFileSync(path.join(out,'inspection.json'),text,{flag:'wx'});}}
console.log(JSON.stringify({status:report.status,output:outputRel,metadata_bytes:report.metadata_bytes,
  modules:report.modules.map(m=>({module:m.source.module,status:m.status,source_hash_preserved:m.source_hash_preserved,targets:m.targets})),failures:report.failures}));
if(report.status==='Failed')process.exitCode=1;
