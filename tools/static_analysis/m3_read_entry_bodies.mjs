// Prepared for separate review/approval. Exactly three pinned entry bodies; no callee expansion.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,2,'No arguments accepted');
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856,base=0x180000000n;
const pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const proofPath='docs/m3-evidence/export-targets.json',proofPin='bf4a797f75913d2afc87ca67ad6d5a2fd6e6c800090f4c367052542b1100e5f3';
const out=path.join(root,'docs/m3-evidence/entry-bodies'),temp=path.join(root,'.tooling/m3-entry-bodies-temp');
const entries=[
 {id:'equipment-auto-insert',begin:0x28a5c0,end:0x28aac5,index:9355,name:'?AutoInsertItem@EquipmentCtrl@GAME@@QEAA_NI@Z'},
 {id:'equipment-remove',begin:0x28aad0,end:0x28afbb,index:9356,name:'?RemoveItem@EquipmentCtrl@GAME@@QEAA_NI@Z'},
 {id:'character-apply-damage',begin:0x68930,end:0x68983,index:2669,name:'?ApplyDamage@Character@GAME@@QEAAXMAEBUPlayStatsDamageType@2@W4CombatAttributeType@2@AEBV?$vector@I@mem@@@Z'},
];
const limits={code_bytes:2627,code_read_operations:3,metadata_bytes:2048,header_end:4096,sections:16,
  hash_buffer_bytes:65536,proof_bytes:256*1024,tiny_pe_bytes_each:8192,tiny_pe_bytes_total:16384,
  dumpbin_calls:3,tool_timeout_ms_each:10000,tool_buffer_bytes_each:512*1024,reported_decode_issues_each:16};
const hex=n=>'0x'+n.toString(16),hash=b=>createHash('sha256').update(b).digest('hex');
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);
  assert.ok(!s.isSymbolicLink(),'Link/junction refused');assert.ok(first&&file?s.isFile():s.isDirectory());
  assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase(),'Redirected ancestor refused');
  const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Preserve existing output: '+p);}
function digest(fd,n){const buffer=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');
  for(let at=0;at<n;){const got=fs.readSync(fd,buffer,0,Math.min(buffer.length,n-at),at);assert.ok(got>0);h.update(buffer.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,'r');try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
for(const p of [out,temp]){assert.ok(p.startsWith(root+path.sep));normal(path.dirname(p));absent(p);}normal(self,true);
const proofFile=path.join(root,proofPath);normal(proofFile,true);assert.ok(fs.statSync(proofFile).size<=limits.proof_bytes);
const proofBytes=fs.readFileSync(proofFile);assert.equal(hash(proofBytes),proofPin);const proof=JSON.parse(proofBytes);
assert.equal(proof.status,'MetadataOnlyCompleted');assert.equal(proof.target,original);assert.equal(proof.source_bytes,size);
assert.equal(proof.sha256_before,pin);assert.equal(proof.sha256_after,pin);assert.equal(proof.code_bytes_interpreted,0);
const candidates=Object.values(proof.categories).flatMap(c=>c.candidates);
for(const e of entries){const found=candidates.filter(c=>c.decorated_name===e.name);assert.equal(found.length,1);
  const c=found[0];assert.equal(c.rva,hex(e.begin));assert.equal(c.forwarded,false);assert.equal(c.pdata.status,'ExactEntry');
  assert.equal(c.pdata.index,e.index);assert.equal(c.pdata.begin_rva,hex(e.begin));assert.equal(c.pdata.end_rva_exclusive,hex(e.end));
  assert.equal(c.pdata.bytes,e.end-e.begin);e.pdata=c.pdata;}
assert.equal(entries.reduce((sum,e)=>sum+e.end-e.begin,0),limits.code_bytes);
const report={schema:'grim-dynasty-m3-entry-bodies-v1',status:'started',started_utc:new Date().toISOString(),
  original,expected_sha256:pin,expected_file_bytes:size,proof:{path:proofPath,sha256:proofPin},limits,
  original_open_mode:'r',original_loaded_or_executed:false,original_path_given_to_dumpbin:false,
  code_bytes:0,code_read_operations:0,metadata_bytes:0,dumpbin_calls:0,synthetic_bytes:0,callees_followed:0,
  global_data_bytes:0,unwind_body_bytes:0,source_closed:false,failures:[],bodies:[],
  limitations:[
    'Only three exact pinned .pdata ranges are read; headers are read only to map their file offsets.',
    'dumpbin receives synthetic PE files only. Neither source nor synthetic files are loaded/executed as program code.',
    'Complete contiguous byte coverage is required; gaps, invalid/data directives or mismatch stop this fixed pass without extending reads.',
    'Syntactic coverage is not complete control-flow/ABI/exception/lifecycle proof and does not show runtime state.',
    'Calls and branches are reported as text only; no targets, imports, global data, vtables, strings or neighboring code are followed.',
    '64 KiB streaming before/after hashes include all source bytes without interpreting bytes outside the selected ranges.',
  ]};
let fd,initial,created=false;
function save(name,data){assert.ok(/^[a-z0-9.-]+$/.test(name));normal(out);fs.writeFileSync(path.join(out,name),data,{flag:'wx'});}
function tinyPe(e,code){const rva=e.begin&~0xfff,delta=e.begin-rva,virtualBytes=delta+code.length,rawBytes=Math.ceil(virtualBytes/512)*512;
  const b=Buffer.alloc(512+rawBytes);assert.ok(b.length<=limits.tiny_pe_bytes_each);report.synthetic_bytes+=b.length;assert.ok(report.synthetic_bytes<=limits.tiny_pe_bytes_total);
  const coff=0x84,opt=0x98,sec=0x188;b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);
  b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
  b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(rawBytes,opt+4);b.writeUInt32LE(rva,opt+20);b.writeBigUInt64LE(base,opt+24);
  b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);
  b.writeUInt32LE(Math.ceil((rva+virtualBytes)/4096)*4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
  b.write('.text',sec,'ascii');b.writeUInt32LE(virtualBytes,sec+8);b.writeUInt32LE(rva,sec+12);b.writeUInt32LE(rawBytes,sec+16);
  b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);return b;}
function decode(stdout,e,code,body){const rows=[],issues=[];let issueCount=0;
  function issue(kind,address,detail){issueCount++;if(issues.length<limits.reported_decode_issues_each)issues.push({kind,address,detail});}
  for(const line of stdout.split(/\r?\n/)){
    const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if(m){rows.push({address:hex(BigInt('0x'+m[1])),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
    const tail=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
    if(tail){if(rows.length)rows[rows.length-1].bytes+=tail[1].replace(/\s/g,'').toLowerCase();else issue('orphan-byte-continuation',null,line.trim());}
  }
  let cursor=base+BigInt(e.begin);for(const row of rows){const address=BigInt(row.address),n=row.bytes.length/2;
    if(address!==cursor)issue('noncontiguous-address',row.address,'Expected '+hex(cursor));
    if(!Number.isInteger(n)||n<1||n>15)issue('invalid-instruction-width',row.address,String(n));
    if(!row.instruction||/[?]|\(bad\)|invalid/i.test(row.instruction)||/^(?:db|dw|dd|dq)\b/i.test(row.instruction))issue('invalid-or-data-decode',row.address,row.instruction);
    cursor=address+BigInt(n);}
  if(cursor!==base+BigInt(e.end))issue('end-boundary-mismatch',hex(cursor),'Expected '+hex(base+BigInt(e.end)));
  if(rows.map(r=>r.bytes).join('')!==code.toString('hex'))issue('byte-concatenation-mismatch',null,'Decoded bytes differ from selected original bytes');
  body.instruction_count=rows.length;body.decode_issue_count=issueCount;body.decode_issues=issues;body.instruction_byte_coverage_verified=issueCount===0;
  body.calls=rows.filter(r=>/^call\b/i.test(r.instruction));body.branches=rows.filter(r=>/^(?:j[a-z]+|loop[a-z]*|xbegin)\b/i.test(r.instruction));
  body.returns=rows.filter(r=>/^ret\b/i.test(r.instruction));save(e.id+'.instructions.json',JSON.stringify(rows,null,2)+'\n');
  assert.equal(issueCount,0,'Fixed entry decode incomplete; no range expansion');}
try{
  fs.mkdirSync(out);created=true;normal(out);fs.mkdirSync(temp);normal(temp);report.script_sha256=fingerprint(self);
  report.tool_sha256_before=fingerprint(tool);assert.equal(report.tool_sha256_before,toolPin);
  normal(original,true);fd=fs.openSync(original,'r');initial=fs.fstatSync(fd);assert.ok(initial.isFile());assert.equal(initial.size,size);
  report.sha256_before=digest(fd,size);assert.equal(report.sha256_before,pin);
  function meta(offset,n){assert.ok(Number.isInteger(offset)&&offset>=0&&n>0&&n<=512&&offset+n<=size&&report.metadata_bytes+n<=limits.metadata_bytes);
    const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);report.metadata_bytes+=n;return b;}
  const dos=meta(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert.ok(pe>=64&&pe<=limits.header_end-24);
  const coff=meta(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
  const n=coff.readUInt16LE(6),optSize=coff.readUInt16LE(20);assert.ok(n>0&&n<=limits.sections&&optSize>=112&&optSize<=512&&pe+24+optSize+n*40<=limits.header_end);
  const opt=meta(pe+24,optSize);assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),base);
  const sections=Array.from({length:n},(_,i)=>{const b=meta(pe+24+optSize+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),
    rva:b.readUInt32LE(12),raw_size:b.readUInt32LE(16),raw_offset:b.readUInt32LE(20),characteristics:b.readUInt32LE(36)};});report.pe={image_base:hex(base),sections};
  for(const e of entries){const length=e.end-e.begin,found=sections.filter(s=>e.begin>=s.rva&&length<=s.raw_size&&e.begin-s.rva<=s.raw_size-length);assert.equal(found.length,1);
    const s=found[0],offset=s.raw_offset+e.begin-s.rva;assert.equal(s.name,'.text');assert.ok(s.characteristics&0x20000000);assert.ok(offset>=0&&offset+length<=size);
    assert.ok(report.code_read_operations<limits.code_read_operations&&report.code_bytes+length<=limits.code_bytes);
    const code=Buffer.alloc(length),got=fs.readSync(fd,code,0,length,offset);report.code_read_operations++;report.code_bytes+=got;assert.equal(got,length);
    const body={id:e.id,decorated_name:e.name,begin_rva:hex(e.begin),end_rva_exclusive:hex(e.end),bytes:length,file_offset:offset,pdata:e.pdata,sha256:hash(code)};
    report.bodies.push(body);save(e.id+'.bin',code);const synthetic=tinyPe(e,code),tiny=path.join(temp,e.id+'.inspect.dll');fs.writeFileSync(tiny,synthetic,{flag:'wx'});
    body.synthetic={relative_path:path.relative(root,tiny).replace(/\\/g,'/'),bytes:synthetic.length,sha256:hash(synthetic),never_executed:true};
    const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(e.begin))+','+hex(base+BigInt(e.end-1)),tiny];
    assert.ok(report.dumpbin_calls<limits.dumpbin_calls);report.dumpbin_calls++;body.command_arguments=args;
    const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:limits.tool_timeout_ms_each,maxBuffer:limits.tool_buffer_bytes_each,
      env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
    save(e.id+'.dumpbin.txt',child.stdout||'');save(e.id+'.stderr.txt',child.stderr||'');body.tool_exit=child.status;body.tool_error=child.error?String(child.error):null;
    assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
    assert.equal(fingerprint(tiny),body.synthetic.sha256);decode(child.stdout,e,code,body);
  }
  assert.equal(report.code_bytes,limits.code_bytes);assert.equal(report.code_read_operations,3);report.status='FixedBodiesDecodedReviewRequired';
}catch(e){report.failures.push(String(e));report.status='Failed';}
finally{
  if(fd!==undefined){try{report.sha256_after=digest(fd,size);assert.equal(report.sha256_after,pin);report.source_stat_stable=stable(initial,fs.fstatSync(fd));assert.ok(report.source_stat_stable);}
    catch(e){report.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('Close: '+e);}}}
  if(report.tool_sha256_before!==undefined){try{report.tool_sha256_after=fingerprint(tool);assert.equal(report.tool_sha256_after,toolPin);}catch(e){report.failures.push('Tool final: '+e);}}
  report.source_hash_preserved=report.sha256_before===pin&&report.sha256_after===pin;if(report.failures.length)report.status='Failed';
  report.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,dumpbin_calls:report.dumpbin_calls,
  source_hash_preserved:report.source_hash_preserved,source_closed:report.source_closed,bodies:report.bodies.map(b=>({id:b.id,instructions:b.instruction_count,coverage:b.instruction_byte_coverage_verified})),failures:report.failures}));
if(report.failures.length)process.exitCode=1;
