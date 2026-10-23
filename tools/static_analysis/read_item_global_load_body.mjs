// Prepared only. Execution requires separate full review. No arguments accepted.
// One fixed Game body [2C8750,2D41A1), no new metadata or callee/neighbor reads.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length,2);
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(),path.join(root,'tools/static_analysis/read_item_global_load_body.mjs').toLowerCase());
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',size=11385856;
const pin='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const target=0x2c8750,end=0x2d41a1,length=47697,base=0x180000000n;
const out=path.join(root,'docs/object-lifecycle/offline-item-global-load-body');
const temp=path.join(root,'.tooling/offline-item-global-load-body-temp');
const sources=[
 ['docs/object-lifecycle/offline-item-global-load-boundary/inspection.json','6eeb71489737ba0e3a63c8a6b716eebe954642cdd40cb46939d88efd37db630f'],
 ['docs/task-kills/ghidra-selected/subscriber-constructor-routes.json','e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165']
];
const limits={code_bytes:48*1024,metadata_bytes:0,semantic_read_operations:1,dumpbin_calls:1,
 tiny_pe_bytes:56*1024,stdout_buffer_bytes:3*1024*1024,tool_timeout_ms:30000,hash_buffer_bytes:65536,
 proof_bytes_each:262144,reported_decode_issues:32};
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);
 assert(!s.isSymbolicLink(),'Symlink or junction refused');assert(first&&file?s.isFile():s.isDirectory());
 assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase(),'Redirected ancestor refused');
 const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing output preserved: '+p);}
function digest(fd,n){const b=Buffer.alloc(limits.hash_buffer_bytes),h=createHash('sha256');
 for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert(got>0,'Short hash read');h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const handle=fs.openSync(p,fs.constants.O_RDONLY);
 try{return digest(handle,fs.fstatSync(handle).size);}finally{fs.closeSync(handle);}}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
normal(self,true);normal(path.dirname(out));normal(path.dirname(temp));absent(out);absent(temp);
const proofs=sources.map(([relative,sha])=>{const p=path.join(root,relative);normal(p,true);assert(fs.statSync(p).size<=limits.proof_bytes_each);
 const b=fs.readFileSync(p);assert.equal(hash(b),sha);return JSON.parse(b);});
const prior=proofs[0],pe=prior.pe,boundary=prior.exact_boundary;
assert.equal(prior.status,'exact-global-load-boundary-metadata-only');assert.equal(prior.original,original);
assert.equal(prior.expected_sha256,pin);assert.equal(prior.expected_file_bytes,size);
assert.equal(prior.sha256_before,pin);assert.equal(prior.sha256_after,pin);
assert(prior.source_stat_stable&&prior.source_hash_preserved&&prior.source_closed);assert.equal(prior.failures.length,0);
assert.equal(prior.code_bytes,0);assert.equal(prior.selected_code_range,null);
assert.equal(boundary.index,10475);assert.equal(boundary.begin,target);assert.equal(boundary.end,end);
assert.equal(boundary.unwind,0x820208);assert.equal(boundary.raw_hex,'50872c00a1412d0008028200');
assert.equal(boundary.range_bytes,length);assert.equal(boundary.code_file_offset,2915152);
assert.equal(boundary.begin_rva,'0x2c8750');assert.equal(boundary.end_rva_exclusive,'0x2d41a1');
const pdata=Buffer.from(boundary.raw_hex,'hex');assert.equal(pdata.length,12);
assert.equal(pdata.readUInt32LE(),target);assert.equal(pdata.readUInt32LE(4),end);assert.equal(pdata.readUInt32LE(8),0x820208);
assert.equal(end-target,length);assert(length<=limits.code_bytes);
assert.equal(pe.image_base,hex(base));assert.equal(pe.image_size,11427840);
assert.equal(pe.exception_rva,'0xa6a000');assert.equal(pe.exception_file_offset,10896384);
assert.equal(pe.entry_count,22293);assert.equal(pe.exception_bytes,pe.entry_count*12);
assert.equal(boundary.file_offset,pe.exception_file_offset+10475*12);
function map(rva,n){assert(Number.isSafeInteger(rva)&&rva>=0&&Number.isSafeInteger(n)&&n>0);
 const ss=pe.sections.filter(s=>rva>=s.rva&&n<=s.raw_size&&rva-s.rva<=s.raw_size-n);assert.equal(ss.length,1);
 const offset=ss[0].raw_offset+rva-ss[0].rva;assert(offset>=0&&offset<=size-n);return {section:ss[0],offset};}
const mapped=map(target,length);assert.equal(mapped.section.name,'.text');assert(mapped.section.characteristics&0x20000000);
assert.equal(mapped.offset,boundary.code_file_offset);
const pm=map(Number(pe.exception_rva),pe.exception_bytes);assert.equal(pm.section.name,'.pdata');assert.equal(pm.offset,pe.exception_file_offset);
const callers=proofs[1].functions.filter(f=>f.observed_entry==='1802c07a0');assert.equal(callers.length,1);
assert.equal(callers[0].body,'[[1802c07a0, 1802c1ba6]]');
const caller='1802c18d1: CALL 0x1802c8750';assert(callers[0].assembly.includes(caller));
const report={schema:'grim-dynasty-item-global-load-body-v1',status:'started',started_utc:new Date().toISOString(),
 original,expected_sha256:pin,expected_file_bytes:size,sources,caller_instruction:caller,pe,exact_boundary:boundary,limits,
 selected_code_range:{rva:hex(target),end_rva_exclusive:hex(end),bytes:length},
 original_open_mode:'O_RDONLY',original_loaded_or_executed:false,original_path_given_to_dumpbin:false,
 whole_table_scanned:false,callees_followed:0,new_header_bytes:0,unwind_body_bytes:0,data_bytes:0,metadata_bytes:0,
 semantic_read_operations:0,code_bytes:0,dumpbin_calls:0,reads:[],source_closed:false,failures:[],
 instruction_byte_coverage_verified:false,decoded_body_complete:false,decode_issue_count:0,decode_issues:[],
 limitations:['Only the one pinned .pdata body is read. No imports, strings, globals, unwind bodies, callees or neighboring bytes are followed.',
  'dumpbin receives one synthetic PE only; neither original DLL nor the synthetic PE is loaded/executed as program code.',
  'Full byte concatenation and contiguous instruction addresses are required for a complete decode. A gap, invalid/data directive or byte mismatch stops interpretation without extending the read.',
  'Even complete syntactic decoding does not establish that every byte is executable, which branches are reachable, or which fields are written at runtime.',
  'Reported calls and branches are raw decoded text only; no algorithm, call signature or live state is inferred.',
  '64KiB before/after hashes on the same original handle establish file identity, not atomic snapshot or loaded compatibility.']};
let fd,initial,created=false;
function save(name,b){normal(out);fs.writeFileSync(path.join(out,name),b,{flag:'wx'});}
function issue(kind,address,detail){report.decode_issue_count++;if(report.decode_issues.length<limits.reported_decode_issues)report.decode_issues.push({kind,address,detail});}
try{
 fs.mkdirSync(out);created=true;normal(out);report.script_sha256=fingerprint(self);
 // Validate the exact installed tool before any new original semantic read.
 report.tool_before=fingerprint(tool);assert.equal(report.tool_before,toolPin);
 normal(original,true);fd=fs.openSync(original,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);assert(initial.isFile());assert.equal(initial.size,size);
 report.sha256_before=digest(fd,size);assert.equal(report.sha256_before,pin);
 assert.equal(report.semantic_read_operations,0);assert.equal(report.code_bytes,0);report.semantic_read_operations=1;
 const code=Buffer.alloc(length),got=fs.readSync(fd,code,0,length,mapped.offset);report.code_bytes=got;
 report.reads.push({kind:'exact-body',rva:hex(target),file_offset:mapped.offset,requested_bytes:length,read_bytes:got});
 assert.equal(got,length,'Short fixed-body read');save('exact.bin',code);report.exact_sha256=hash(code);
 fs.mkdirSync(temp);normal(temp);
 const sectionRva=target&~0xfff,delta=target-sectionRva,virtualBytes=delta+length,rawBytes=Math.ceil(virtualBytes/512)*512;
 const synthetic=Buffer.alloc(512+rawBytes);assert.equal(synthetic.length,50176);assert(synthetic.length<=limits.tiny_pe_bytes);
 const coff=0x84,opt=0x98,sec=0x188;
 synthetic.writeUInt16LE(0x5a4d,0);synthetic.writeUInt32LE(0x80,0x3c);synthetic.writeUInt32LE(0x4550,0x80);
 synthetic.writeUInt16LE(0x8664,coff);synthetic.writeUInt16LE(1,coff+2);synthetic.writeUInt16LE(0xf0,coff+16);synthetic.writeUInt16LE(0x2022,coff+18);
 synthetic.writeUInt16LE(0x20b,opt);synthetic.writeUInt32LE(rawBytes,opt+4);synthetic.writeUInt32LE(sectionRva,opt+20);synthetic.writeBigUInt64LE(base,opt+24);
 synthetic.writeUInt32LE(4096,opt+32);synthetic.writeUInt32LE(512,opt+36);synthetic.writeUInt16LE(6,opt+40);synthetic.writeUInt16LE(6,opt+48);
 synthetic.writeUInt32LE(Math.ceil((sectionRva+virtualBytes)/4096)*4096,opt+56);synthetic.writeUInt32LE(512,opt+60);synthetic.writeUInt16LE(3,opt+68);synthetic.writeUInt32LE(16,opt+108);
 synthetic.write('.text',sec,'ascii');synthetic.writeUInt32LE(virtualBytes,sec+8);synthetic.writeUInt32LE(sectionRva,sec+12);
 synthetic.writeUInt32LE(rawBytes,sec+16);synthetic.writeUInt32LE(512,sec+20);synthetic.writeUInt32LE(0x60000020,sec+36);code.copy(synthetic,512+delta);
 const tiny=path.join(temp,'2c8750-exact.inspect.dll');fs.writeFileSync(tiny,synthetic,{flag:'wx'});
 report.synthetic={path:tiny,bytes:synthetic.length,sha256:hash(synthetic),never_executed:true};
 const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(target))+','+hex(base+BigInt(end-1)),tiny];
 report.command_arguments=args;assert.equal(report.dumpbin_calls,0);report.dumpbin_calls=1;
 const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:limits.tool_timeout_ms,maxBuffer:limits.stdout_buffer_bytes,
  env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
 save('dumpbin.txt',child.stdout||'');save('stderr.txt',child.stderr||'');report.tool_exit=child.status;
 report.tool_error=child.error?String(child.error):null;report.tool_signal=child.signal;
 assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
 assert.equal(fingerprint(tiny),report.synthetic.sha256);
 const rows=[];for(const line of (child.stdout||'').split(/\r?\n/)){
  const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
  if(m){rows.push({address:hex(BigInt('0x'+m[1])),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
  const tail=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
  if(tail){if(rows.length)rows[rows.length-1].bytes+=tail[1].replace(/\s/g,'').toLowerCase();else issue('orphan-byte-continuation',null,line.trim());}
 }
 save('instructions.json',JSON.stringify(rows,null,2)+'\n');report.instructions=rows.length;
 let cursor=base+BigInt(target);for(const row of rows){
  const address=BigInt(row.address),n=row.bytes.length/2;
  if(address!==cursor)issue('noncontiguous-address',row.address,'Expected '+hex(cursor));
  if(n<1||n>15||!Number.isInteger(n))issue('invalid-instruction-width',row.address,String(n));
  if(!row.instruction||/[?]|\(bad\)|invalid/i.test(row.instruction)||/^(?:db|dw|dd|dq)\b/i.test(row.instruction))
   issue('invalid-or-data-decode',row.address,row.instruction);
  cursor=address+BigInt(n);
 }
 if(cursor!==base+BigInt(end))issue('end-boundary-mismatch',hex(cursor),'Expected '+hex(base+BigInt(end)));
 const concatenated=rows.map(r=>r.bytes).join('');
 if(concatenated!==code.toString('hex'))issue('byte-concatenation-mismatch',null,'Decoded bytes do not match exact.bin');
 report.instruction_byte_coverage_verified=report.decode_issue_count===0;
 report.decoded_body_complete=report.instruction_byte_coverage_verified;
 report.calls=rows.filter(r=>/^call\b/i.test(r.instruction));
 report.branches=rows.filter(r=>/^(?:j[a-z]+|loop[a-z]*|xbegin)\b/i.test(r.instruction));
 report.returns=rows.filter(r=>/^ret\b/i.test(r.instruction));
 report.raw_control_flow_validated_with_complete_body=report.decoded_body_complete;
 report.status=report.decoded_body_complete?'exact-global-load-body-decoded-review-required':'fixed-global-load-body-decode-frontier';
}catch(e){report.failures.push(String(e));report.status='failed';}
finally{
 if(fd!==undefined){try{if(initial&&initial.size===size){report.sha256_after=digest(fd,size);assert.equal(report.sha256_after,pin);
  report.source_stat_stable=stable(initial,fs.fstatSync(fd));assert(report.source_stat_stable);}}
  catch(e){report.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('Close: '+e);}}}
 if(report.tool_before!==undefined){try{report.tool_after=fingerprint(tool);assert.equal(report.tool_after,toolPin);}catch(e){report.failures.push('Tool final: '+e);}}
 report.source_hash_preserved=report.sha256_before===pin&&report.sha256_after===pin;if(report.failures.length)report.status='failed';
 report.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,semantic_read_operations:report.semantic_read_operations,
 instructions:report.instructions,decoded_body_complete:report.decoded_body_complete,decode_issue_count:report.decode_issue_count,
 calls:report.calls?.length,branches:report.branches?.length,source_closed:report.source_closed,source_hash_preserved:report.source_hash_preserved,failures:report.failures}));
if(report.failures.length||report.decode_issue_count)process.exitCode=1;
