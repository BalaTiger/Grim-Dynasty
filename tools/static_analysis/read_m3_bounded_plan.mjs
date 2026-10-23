// A plan is data, not permission: every invocation requires its own review/approval.
// Usage: node tools/static_analysis/read_m3_bounded_plan.mjs docs/m3-evidence/plans/<id>.json
// Fixed Game fingerprint, <=2 explicit references, .pdata binary search, no callee chasing.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length,3,'Exactly one project plan is required');
const self=fileURLToPath(import.meta.url),root=path.resolve(path.dirname(self),'../..');
assert.equal(root.toLowerCase(),path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const source={path:'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',
  sha256:'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',bytes:11385856,preferred_base:'0x180000000'};
const base=BigInt(source.preferred_base),tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolPin='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const ceilings={body_bytes_each:8192,body_bytes_total:8192,metadata_bytes:16384,dumpbin_calls:2,tool_timeout_ms_each:10000};
const fixed={plan_bytes:16384,proof_bytes:1024*1024,hash_buffer_bytes:65536,header_end:4096,sections:16,
  metadata_read_bytes:512,pdata_bytes:2*1024*1024,binary_search_steps:32,synthetic_each:16384,synthetic_total:24576,tool_buffer_each:1024*1024};
const hex=n=>'0x'+n.toString(16),sha=b=>createHash('sha256').update(b).digest('hex');
const fits=(at,n,total)=>Number.isSafeInteger(at)&&Number.isSafeInteger(n)&&at>=0&&n>0&&at<=total&&n<=total-at;
function normal(p,file=false){let first=true;for(let c=path.resolve(p);;){const s=fs.lstatSync(c);assert.ok(!s.isSymbolicLink(),'Links/junctions refused');
  assert.ok(first&&file?s.isFile():s.isDirectory());assert.equal(path.resolve(fs.realpathSync.native(c)).toLowerCase(),c.toLowerCase());
  const parent=path.dirname(c);if(parent===c)return;c=parent;first=false;}}
function local(rel){assert.ok(typeof rel==='string'&&/^[a-z0-9./-]+$/.test(rel)&&!rel.split('/').some(s=>!s||s==='.'||s==='..'));
  const p=path.resolve(root,rel);assert.ok(p.startsWith(root+path.sep));return p;}
function absent(p){try{fs.lstatSync(p);}catch(e){if(e.code==='ENOENT')return;throw e;}assert.fail('Existing evidence preserved: '+p);}
function readLocal(p,max){normal(p,true);assert.ok(fs.statSync(p).size<=max);return fs.readFileSync(p);}
function digest(fd,n){const h=createHash('sha256'),b=Buffer.alloc(fixed.hash_buffer_bytes);for(let at=0;at<n;){const got=fs.readSync(fd,b,0,Math.min(b.length,n-at),at);assert.ok(got>0);h.update(b.subarray(0,got));at+=got;}return h.digest('hex');}
function fingerprint(p){normal(p,true);const fd=fs.openSync(p,'r');try{return digest(fd,fs.fstatSync(fd).size);}finally{fs.closeSync(fd);}}
function stable(a,b){return b.isFile()&&a.size===b.size&&a.dev===b.dev&&a.ino===b.ino&&a.mtimeMs===b.mtimeMs&&a.ctimeMs===b.ctimeMs;}
function keys(o,expected){assert.deepEqual(Object.keys(o).sort(),expected.split(' ').sort());}
const leafOps=new Set(('mov movss movsd movzx movsx movsxd lea add sub and or xor xorps xorpd pxor not neg inc dec cmp test shl shr sar sal imul mul div idiv addss addsd subss subsd mulss mulsd divss divsd minss minsd maxss maxsd cvtss2sd cvtsd2ss cvtsi2ss cvtsi2sd cvttss2si cvttsd2si cvtps2pd cvtpd2ps comiss comisd ucomiss ucomisd nop').split(' '));
const planRel=process.argv[2];assert.match(planRel,/^docs\/m3-evidence\/plans\/[a-z0-9][a-z0-9-]*\.json$/);
const planBytes=readLocal(local(planRel),fixed.plan_bytes),plan=JSON.parse(planBytes);
const v2=plan.schema==='grim-dynasty-m3-bounded-plan-v2',v3=plan.schema==='grim-dynasty-m3-bounded-plan-v3',v4=plan.schema==='grim-dynasty-m3-bounded-plan-v4',dataMode=plan.schema==='grim-dynasty-m3-bounded-plan-v5',leafMode=v3||v4,extended=v2||leafMode||dataMode;
const hasConstant=v4&&Object.prototype.hasOwnProperty.call(plan,'constant_read');
keys(plan,'schema id purpose source proof targets limits output temporary automatic_callee_expansion execution_requires_separate_review_and_approval'+(extended?' mode range_policy':'')+(leafMode?' window_bytes target_origin':'')+(v4?' continuation':'')+(hasConstant?' constant_read':''));
assert.ok(extended||plan.schema==='grim-dynasty-m3-bounded-plan-v1');assert.match(plan.id,/^[a-z0-9][a-z0-9-]{0,63}$/);
const mode=extended?plan.mode:'bodies',rangePolicy=extended?plan.range_policy:'exact-entry';
if(leafMode){assert.equal(mode,v4?'bounded-leaf-continuation':'bounded-leaf-window');assert.equal(rangePolicy,'fixed-window-not-function');
  assert.equal(plan.window_bytes,v4?96:32);assert.equal(plan.target_origin,v4?'saved-direct-call-with-contiguous-prefix':'saved-direct-call');
  ceilings.body_bytes_each=plan.window_bytes;ceilings.body_bytes_total=plan.window_bytes;ceilings.dumpbin_calls=1;}
else if(dataMode){assert.equal(mode,'readonly-rdata');assert.equal(rangePolicy,'fixed-data-not-code');ceilings.body_bytes_each=0;ceilings.body_bytes_total=0;ceilings.dumpbin_calls=0;ceilings.data_bytes=8;}
else{assert.ok(['bodies','metadata-only'].includes(mode));assert.ok(['exact-entry','containing-pdata-fragment'].includes(rangePolicy));}
if(hasConstant)ceilings.data_bytes=4;
assert.equal(planRel,'docs/m3-evidence/plans/'+plan.id+'.json');assert.deepEqual(plan.source,source);assert.deepEqual(plan.limits,ceilings);
assert.equal(plan.automatic_callee_expansion,false);assert.equal(plan.execution_requires_separate_review_and_approval,true);
assert.equal(plan.output,'docs/m3-evidence/'+plan.id);assert.equal(plan.temporary,'.tooling/m3-'+plan.id+'-temp');
const out=local(plan.output),temp=local(plan.temporary);for(const p of [out,temp]){normal(path.dirname(p));absent(p);}normal(self,true);
keys(plan.proof,'path sha256');assert.match(plan.proof.path,dataMode?/^docs\/m3-evidence\/[a-z0-9-]+\/[a-z0-9.-]+\.decode-candidates\.json$/:/^docs\/m3-evidence\/[a-z0-9-]+\/[a-z0-9.-]+\.instructions\.json$/);assert.match(plan.proof.sha256,/^[a-f0-9]{64}$/);
const proofBytes=readLocal(local(plan.proof.path),fixed.proof_bytes);assert.equal(sha(proofBytes),plan.proof.sha256);const rows=JSON.parse(proofBytes);assert.ok(Array.isArray(rows));
assert.ok(rows.length>0);const proofBegin=BigInt(rows[0].address);let proofEnd=proofBegin;
for(const row of rows){assert.match(row.bytes,/^(?:[a-f0-9]{2})+$/);assert.equal(BigInt(row.address),proofEnd);proofEnd+=BigInt(row.bytes.length/2);}
assert.ok(proofBegin>=base&&proofEnd<=base+0x80000000n);
const excluded={begin_rva:hex(proofBegin-base),end_rva_exclusive:hex(proofEnd-base),reason:'Pinned saved instruction bytes already read'};
assert.ok(Array.isArray(plan.targets)&&plan.targets.length>=1&&plan.targets.length<=2);const ids=new Set(),rvas=new Set();
if(leafMode)assert.equal(plan.targets.length,1,'A leaf window accepts exactly one proven target');
if(dataMode)assert.equal(plan.targets.length,2,'Data-only scope is exactly two four-byte constants');
const targets=plan.targets.map(t=>{if(dataMode){keys(t,'id rva bytes references');assert.match(t.id,/^[a-z0-9][a-z0-9-]{0,63}$/);assert.match(t.rva,/^0x[a-f0-9]{1,8}$/);
    const rva=Number(t.rva);assert.ok(rva>0&&rva<0x80000000&&!rvas.has(rva)&&!ids.has(t.id));ids.add(t.id);rvas.add(rva);assert.equal(t.bytes,4);
    assert.ok(Array.isArray(t.references)&&t.references.length>=1&&t.references.length<=2);
    for(const ref of t.references){keys(ref,'address bytes');assert.match(ref.address,/^0x[a-f0-9]{9,16}$/);assert.match(ref.bytes,/^[a-f0-9]{16}$/);
      const found=rows.filter(r=>r.address===ref.address);assert.equal(found.length,1);assert.equal(found[0].bytes,ref.bytes);assert.match(found[0].instruction,/^mulss\s/);
      const encoded=Buffer.from(ref.bytes,'hex');assert.equal(encoded.subarray(0,3).toString('hex'),'f30f59');assert.equal(encoded[3]&0xc7,0x05);
      assert.equal(BigInt(ref.address)+8n+BigInt(encoded.readInt32LE(4)),base+BigInt(rva),'Saved RIP-relative MULSS target mismatch');}
    return {...t,rva,reference_kind:'saved-rip-relative-mulss'};}
  keys(t,'id rva caller_address caller_bytes'+(extended?' reference_kind':''));assert.match(t.id,/^[a-z0-9][a-z0-9-]{0,63}$/);
  assert.match(t.rva,/^0x[a-f0-9]{1,8}$/);assert.match(t.caller_address,/^0x[a-f0-9]{9,16}$/);assert.match(t.caller_bytes,/^(?:[a-f0-9]{2}){2,6}$/);
  const rva=Number(t.rva),caller=BigInt(t.caller_address);assert.ok(rva>0&&rva<0x80000000&&!rvas.has(rva)&&!ids.has(t.id));ids.add(t.id);rvas.add(rva);
  const found=rows.filter(r=>r.address===t.caller_address);assert.equal(found.length,1);assert.equal(found[0].bytes,t.caller_bytes);
  const kind=extended?t.reference_kind:'direct-call',bytes=Buffer.from(t.caller_bytes,'hex');let displacement;if(leafMode)assert.equal(kind,'direct-call');
  if(kind==='direct-call'){assert.equal(bytes.length,5);assert.equal(bytes[0],0xe8);assert.match(found[0].instruction,/^call\s+[0-9A-Fa-f]{16}$/);displacement=bytes.readInt32LE(1);}
  else{assert.ok(['conditional-branch','conditional-fallthrough'].includes(kind));assert.match(found[0].instruction,/^j[a-z]+\s+[0-9A-Fa-f]{16}$/);
    if(bytes.length===2&&bytes[0]>=0x70&&bytes[0]<=0x7f)displacement=bytes.readInt8(1);
    else{assert.equal(bytes.length,6);assert.equal(bytes[0],0x0f);assert.ok(bytes[1]>=0x80&&bytes[1]<=0x8f);displacement=bytes.readInt32LE(2);}}
  const next=caller+BigInt(bytes.length),taken=next+BigInt(displacement);
  assert.equal(BigInt('0x'+found[0].instruction.trim().split(/\s+/)[1]),taken);
  if(kind==='conditional-fallthrough'){assert.equal(next,proofEnd,'Fallthrough must be at the saved fragment end');assert.equal(next,base+BigInt(rva));}
  else assert.equal(taken,base+BigInt(rva));return {...t,rva,reference_kind:kind};});
let continuationRows=[],continuationBytes=Buffer.alloc(0),continuationInfo,referencedConstant;
const exclusions=[excluded];
if(v4){const c=plan.continuation;keys(c,'path sha256 origin_rva start_rva prefix_bytes total_window_limit expected_instructions');
  assert.match(c.path,/^docs\/m3-evidence\/[a-z0-9-]+\/[a-z0-9.-]+\.decode-candidates\.json$/);assert.match(c.sha256,/^[a-f0-9]{64}$/);
  assert.equal(c.origin_rva,hex(targets[0].rva));assert.equal(c.prefix_bytes,32);assert.equal(c.total_window_limit,128);
  assert.equal(c.start_rva,hex(targets[0].rva+32));assert.equal(c.prefix_bytes+plan.window_bytes,c.total_window_limit);
  const saved=readLocal(local(c.path),fixed.proof_bytes);assert.equal(sha(saved),c.sha256);continuationRows=JSON.parse(saved);
  assert.ok(Array.isArray(continuationRows)&&Array.isArray(c.expected_instructions));assert.equal(continuationRows.length,6);assert.equal(c.expected_instructions.length,6);
  let next=base+BigInt(targets[0].rva);
  for(let i=0;i<continuationRows.length;i++){const r=continuationRows[i],e=c.expected_instructions[i];keys(e,'address bytes mnemonic');
    assert.equal(r.address,e.address);assert.equal(r.bytes,e.bytes);assert.match(r.bytes,/^(?:[a-f0-9]{2})+$/);assert.equal(BigInt(r.address),next);
    const width=r.bytes.length/2,mnemonic=r.instruction.trim().split(/\s+/)[0].toLowerCase();assert.ok(width>=1&&width<=15);
    assert.equal(mnemonic,e.mnemonic);assert.ok(leafOps.has(mnemonic),'Prefix contains transfer or unknown instruction');assert.ok(!/[?]|\(bad\)|invalid/i.test(r.instruction));next+=BigInt(width);}
  assert.equal(next,base+BigInt(c.start_rva));continuationBytes=Buffer.from(continuationRows.map(r=>r.bytes).join(''),'hex');assert.equal(continuationBytes.length,32);
  exclusions.push({begin_rva:c.origin_rva,end_rva_exclusive:c.start_rva,reason:'Pinned saved 32-byte prefix; original bytes must not be reread'});
  continuationInfo={path:c.path,sha256:c.sha256,origin_rva:c.origin_rva,start_rva:c.start_rva,saved_prefix_bytes:32,new_window_bytes:96,total_window_limit:128};
  if(hasConstant){const d=plan.constant_read;keys(d,'rva bytes source_address source_bytes expected_section');assert.equal(d.bytes,4);assert.equal(d.expected_section,'.rdata');
    assert.match(d.rva,/^0x[a-f0-9]{1,8}$/);const first=continuationRows[0];assert.equal(d.source_address,first.address);assert.equal(d.source_bytes,first.bytes);
    const encoded=Buffer.from(first.bytes,'hex');assert.equal(encoded.length,8);assert.equal(encoded.subarray(0,4).toString('hex'),'f30f1005');
    assert.equal(BigInt(first.address)+8n+BigInt(encoded.readInt32LE(4)),base+BigInt(d.rva));referencedConstant=d;}}
const report={schema:'grim-dynasty-m3-bounded-read-v1',status:'Started',started_utc:new Date().toISOString(),
  plan:{path:planRel,sha256:sha(planBytes)},source,proof:plan.proof,mode,range_policy:rangePolicy,excluded_proof_range:excluded,limits:ceilings,fixed_limits:fixed,
  continuation:continuationInfo,excluded_saved_ranges:exclusions,source_open_mode:'r',source_loaded_or_executed:false,original_path_given_to_dumpbin:false,automatic_callee_expansion:false,
  metadata_bytes:0,metadata_operations:0,body_bytes:0,body_read_operations:0,dumpbin_calls:0,synthetic_bytes:0,
  export_table_bytes:0,global_data_bytes:0,unwind_body_bytes:0,source_closed:false,boundaries:[],bodies:[],failures:[],
  limitations:['All targets are supplied by a separately reviewed plan and matched against pinned saved direct-call or conditional-branch bytes.',
    'Binary search reads .pdata records only; no export scan, unwind decoding or callees are followed.',
    'V1 requires an exact .pdata start. V2 may explicitly permit the containing runtime fragment; this is not a whole source function.',
    'Known proof bytes are excluded. Duplicate containing fragments are read once; overlap, missing boundaries or budget excess stops all body reads.',
    'Metadata-only plans never read body bytes or start dumpbin, regardless of reported boundaries.',
    'V3 reads one explicit 32-byte E8-target window only when .pdata has no containing entry. The window is not a function boundary.',
    'V3 never follows transfers. A conservative straight-line prefix through plain C3 ret may be reported; bytes after ret remain unclassified.',
    'V4 permits one fixed 96-byte continuation after a pinned, contiguous, control-transfer-free 32-byte prefix. Cumulative original window is 128 bytes, with no prefix reread or further continuation.',
    'An optional V4 four-byte read must be the first saved MOVSS RIP-relative operand in readonly non-executable .rdata; only original bits are recorded.',
    'V5 reads exactly two four-byte readonly .rdata operands proved by saved RIP-relative MULSS bytes; it reads no new instructions, .pdata or exports and never starts dumpbin.',
    'Decoder rows are candidates, not proof that all bytes are instructions. Inline data can resemble valid code; all bodies need code/data review.',
    'All gaps, byte mismatches and invalid/data decodes are retained. These findings never trigger new reads or automatic expansion.',
    'Full source before/after hashes use 64 KiB chunks without interpreting bytes outside the approved ranges.',
    'Static evidence does not establish source names, runtime compatibility, exception behavior or a complete damage contract.']};
let fd,initial,created=false;
function save(name,data){assert.match(name,/^[a-z0-9.-]+$/);normal(out);fs.writeFileSync(path.join(out,name),data,{flag:'wx'});}
function tinyPe(begin,code){const rva=begin&~0xfff,delta=begin-rva,vsize=delta+code.length,raw=Math.ceil(vsize/512)*512;
  const b=Buffer.alloc(512+raw);assert.ok(b.length<=fixed.synthetic_each);report.synthetic_bytes+=b.length;assert.ok(report.synthetic_bytes<=fixed.synthetic_total);
  const coff=0x84,opt=0x98,sec=0x188;b.writeUInt16LE(0x5a4d,0);b.writeUInt32LE(0x80,0x3c);b.writeUInt32LE(0x4550,0x80);
  b.writeUInt16LE(0x8664,coff);b.writeUInt16LE(1,coff+2);b.writeUInt16LE(0xf0,coff+16);b.writeUInt16LE(0x2022,coff+18);
  b.writeUInt16LE(0x20b,opt);b.writeUInt32LE(raw,opt+4);b.writeUInt32LE(rva,opt+20);b.writeBigUInt64LE(base,opt+24);
  b.writeUInt32LE(4096,opt+32);b.writeUInt32LE(512,opt+36);b.writeUInt16LE(6,opt+40);b.writeUInt16LE(6,opt+48);
  b.writeUInt32LE(Math.ceil((rva+vsize)/4096)*4096,opt+56);b.writeUInt32LE(512,opt+60);b.writeUInt16LE(3,opt+68);b.writeUInt32LE(16,opt+108);
  b.write('.text',sec,'ascii');b.writeUInt32LE(vsize,sec+8);b.writeUInt32LE(rva,sec+12);b.writeUInt32LE(raw,sec+16);
  b.writeUInt32LE(512,sec+20);b.writeUInt32LE(0x60000020,sec+36);code.copy(b,512+delta);return b;}
function leafPrefix(decoded,body,code){const prefix=[],result={status:'Unknown',function_boundary_verified:false,complete_function_claim:false,
    window_begin_rva:body.begin_rva,window_end_rva_exclusive:body.end_rva_exclusive,return_rva:null,prefix_bytes:0,unclassified_after_ret_bytes:null};
  let cursor=base+BigInt(body.begin_rva);
  for(const row of decoded){const at=BigInt(row.address),n=row.bytes.length/2,mnemonic=row.instruction.trim().split(/\s+/)[0].toLowerCase();
    if(at!==cursor||!Number.isInteger(n)||n<1||n>15||!row.instruction||/[?]|\(bad\)|invalid/i.test(row.instruction)){result.reason='Invalid or noncontiguous prefix';break;}
    if(row.bytes==='c3'&&mnemonic==='ret'){prefix.push(row);cursor+=1n;const count=Number(cursor-(base+BigInt(body.begin_rva)));
      if(prefix.map(r=>r.bytes).join('')!==code.subarray(0,count).toString('hex')){result.reason='Prefix byte mismatch';break;}
      result.status='StraightLineRetPrefixObserved';result.return_rva=hex(at-base);result.prefix_bytes=count;result.prefix_byte_coverage_verified=true;
      result.unclassified_after_ret_bytes=code.length-count;result.unclassified_after_ret_begin_rva=hex(cursor-base);
      result.limitations='Syntactic straight-line prefix only; ABI, memory validity, exceptions, semantics and full function identity remain unverified.';
      return {result,prefix};}
    if(!leafOps.has(mnemonic)){result.reason='Transfer or instruction outside conservative leaf classifier: '+mnemonic;result.stop_address=row.address;break;}
    prefix.push(row);cursor+=BigInt(n);}
  result.reason??='No ordinary ret in the fixed window';return {result,prefix:[]};}
function decode(stdout,body,code){const decoded=[],issues=[];const issue=(kind,address,detail)=>issues.push({kind,address,detail});
  for(const line of stdout.split(/\r?\n/)){const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if(m){decoded.push({address:hex(BigInt('0x'+m[1])),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
    const tail=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);if(tail){if(decoded.length)decoded[decoded.length-1].bytes+=tail[1].replace(/\s/g,'').toLowerCase();else issue('orphan-byte-continuation',null,line.trim());}}
  let cursor=base+BigInt(body.begin_rva);for(const row of decoded){const at=BigInt(row.address),n=row.bytes.length/2;
    if(at!==cursor)issue('noncontiguous-address',row.address,'Expected '+hex(cursor));
    if(!Number.isInteger(n)||n<1||n>15)issue('invalid-instruction-width',row.address,String(n));
    if(!row.instruction||/[?]|\(bad\)|invalid/i.test(row.instruction)||/^(?:db|dw|dd|dq)\b/i.test(row.instruction))issue('invalid-or-data-decode',row.address,row.instruction);
    cursor=at+BigInt(n);}
  if(cursor!==base+BigInt(body.end_rva_exclusive))issue('end-boundary-mismatch',hex(cursor),'Expected '+hex(base+BigInt(body.end_rva_exclusive)));
  if(decoded.map(r=>r.bytes).join('')!==code.toString('hex'))issue('byte-concatenation-mismatch',null,'Decoded bytes differ from exact source range');
  body.decoded_rows=decoded.length;body.decode_issues=issues;body.instruction_byte_coverage_verified=issues.length===0;body.code_data_classification='ReviewRequired';
  body.calls=decoded.filter(r=>/^call\b/i.test(r.instruction));body.branches=decoded.filter(r=>/^(?:j[a-z]+|loop[a-z]*|xbegin)\b/i.test(r.instruction));
  if(leafMode){const combinedRows=v4?[...continuationRows,...decoded]:decoded,combinedCode=v4?Buffer.concat([continuationBytes,code]):code;
    const classified=leafPrefix(combinedRows,v4?{...body,begin_rva:plan.continuation.origin_rva}:body,combinedCode);body.leaf_window=classified.result;
    if(v4){body.leaf_window.reused_saved_prefix_bytes=32;body.leaf_window.newly_read_bytes=96;body.leaf_window.cumulative_window_bytes=128;}
    body.window_candidate_byte_coverage_verified=body.instruction_byte_coverage_verified;delete body.instruction_byte_coverage_verified;
    body.code_data_classification='BoundedWindowNotFunction';delete body.calls;delete body.branches;
    save(body.id+'.decode-candidates.json',JSON.stringify(decoded,null,2)+'\n');
    if(classified.prefix.length)save(body.id+'.leaf-prefix.instructions.json',JSON.stringify(classified.prefix,null,2)+'\n');}
  else save(body.id+'.instructions.json',JSON.stringify(decoded,null,2)+'\n');}
try{
  fs.mkdirSync(out);created=true;normal(out);report.script_sha256=fingerprint(self);
  normal(source.path,true);fd=fs.openSync(source.path,'r');initial=fs.fstatSync(fd);assert.ok(initial.isFile());assert.equal(initial.size,source.bytes);
  report.sha256_before=digest(fd,source.bytes);assert.equal(report.sha256_before,source.sha256);
  function meta(offset,n){assert.ok(fits(offset,n,source.bytes)&&n<=fixed.metadata_read_bytes&&report.metadata_bytes+n<=ceilings.metadata_bytes);
    report.metadata_operations++;const b=Buffer.alloc(n);assert.equal(fs.readSync(fd,b,0,n,offset),n);report.metadata_bytes+=n;return b;}
  const dos=meta(0,64);assert.equal(dos.readUInt16LE(0),0x5a4d);const pe=dos.readUInt32LE(60);assert.ok(pe>=64&&pe<=fixed.header_end-24);
  const coff=meta(pe,24);assert.equal(coff.readUInt32LE(0),0x4550);assert.equal(coff.readUInt16LE(4),0x8664);
  const count=coff.readUInt16LE(6),os=coff.readUInt16LE(20);assert.ok(count>0&&count<=fixed.sections&&os>=144&&os<=512&&pe+24+os+count*40<=fixed.header_end);
  const opt=meta(pe+24,os);assert.equal(opt.readUInt16LE(0),0x20b);assert.equal(opt.readBigUInt64LE(24),base);assert.ok(opt.readUInt32LE(108)>=4);
  const sections=Array.from({length:count},(_,i)=>{const b=meta(pe+24+os+i*40,40);return {name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''),
    rva:b.readUInt32LE(12),raw_size:b.readUInt32LE(16),raw_offset:b.readUInt32LE(20),characteristics:b.readUInt32LE(36)};});
  function map(rva,n){const hit=sections.filter(s=>fits(rva-s.rva,n,s.raw_size));assert.equal(hit.length,1);const section=hit[0],offset=section.raw_offset+rva-section.rva;assert.ok(fits(offset,n,source.bytes));return {section,offset};}
  if(dataMode){report.pe={image_base:hex(base),sections};report.data_reads=[];
    for(const t of targets){const mapped=map(t.rva,4),flags=mapped.section.characteristics;assert.equal(mapped.section.name,'.rdata');
      assert.ok((flags&0x40000000)!==0&&(flags&0x80000000)===0&&(flags&0x20000000)===0,'Constant must be readable, nonwritable and nonexecutable');
      const bits=Buffer.alloc(4);assert.equal(fs.readSync(fd,bits,0,4,mapped.offset),4);report.global_data_bytes+=4;assert.ok(report.global_data_bytes<=ceilings.data_bytes);
      report.data_reads.push({id:t.id,rva:hex(t.rva),bytes:4,references:t.references,section:'.rdata',section_characteristics:hex(flags),readonly:true,executable:false,
        file_offset:mapped.offset,hex_bytes:bits.toString('hex'),uint32_le:hex(bits.readUInt32LE(0)),sha256:sha(bits),interpreted_as_instructions:false});save(t.id+'.bin',bits);}
    assert.equal(report.global_data_bytes,8);assert.equal(report.body_bytes,0);assert.equal(report.dumpbin_calls,0);report.status='ReadonlyDataCompleted';
  }else{
  const prva=opt.readUInt32LE(136),psize=opt.readUInt32LE(140);assert.ok(prva>0&&psize>0&&psize%12===0&&psize<=fixed.pdata_bytes);
  const pdata=map(prva,psize);assert.equal(pdata.section.name,'.pdata');report.pe={image_base:hex(base),sections,pdata_rva:hex(prva),pdata_bytes:psize};
  function bounds(rva){let lo=0,hi=psize/12-1,steps=0;while(lo<=hi){assert.ok(++steps<=fixed.binary_search_steps);const index=Math.floor((lo+hi)/2),b=meta(pdata.offset+12*index,12);
    const begin=b.readUInt32LE(0),end=b.readUInt32LE(4);assert.ok(begin<end);if(rva<begin)hi=index-1;else if(rva>=end)lo=index+1;else return {status:begin===rva?'ExactEntry':'InteriorAddress',index,begin_rva:hex(begin),end_rva_exclusive:hex(end),bytes:end-begin,unwind_info_rva:hex(b.readUInt32LE(8))};}
    return {status:'UnknownNoContainingPdata',bytes:null};}
  for(const t of targets)report.boundaries.push({id:t.id,rva:hex(t.rva),caller_address:t.caller_address,reference_kind:t.reference_kind,pdata:bounds(t.rva),
    continuation_pdata:v4?bounds(Number(plan.continuation.start_rva)):undefined});
  const allowed=leafMode?['UnknownNoContainingPdata']:rangePolicy==='containing-pdata-fragment'?['ExactEntry','InteriorAddress']:['ExactEntry'];
  const reasons=report.boundaries.filter(b=>!allowed.includes(b.pdata.status)).map(b=>b.id+': '+b.pdata.status),unique=new Map();
  if(v4)for(const b of report.boundaries)if(b.continuation_pdata.status!=='UnknownNoContainingPdata')reasons.push(b.id+': continuation enters '+b.continuation_pdata.status);
  for(const b of report.boundaries){if(!allowed.includes(b.pdata.status))continue;
    const begin=v4?plan.continuation.start_rva:b.rva,p=leafMode?{status:'BoundedWindowNotFunction',begin_rva:begin,end_rva_exclusive:hex(Number(begin)+plan.window_bytes),bytes:plan.window_bytes,function_boundary_verified:false}:b.pdata,key=p.begin_rva+':'+p.end_rva_exclusive;
    if(unique.has(key))unique.get(key).reference_ids.push(b.id);else unique.set(key,{...b,range:p,reference_ids:[b.id]});}
  const selected=[...unique.values()];report.selected_fragments=selected;
  for(const b of selected){if(b.range.bytes>ceilings.body_bytes_each)reasons.push(b.id+': per-body byte limit');
    for(const range of exclusions)if(Number(b.range.begin_rva)<Number(range.end_rva_exclusive)&&Number(b.range.end_rva_exclusive)>Number(range.begin_rva))reasons.push(b.id+': would reread pinned saved bytes');}
  const total=selected.reduce((n,b)=>n+b.range.bytes,0);if(total>ceilings.body_bytes_total)reasons.push('Combined body byte limit');
  const ordered=selected.map(b=>b.range).sort((a,b)=>Number(a.begin_rva)-Number(b.begin_rva));
  for(let i=1;i<ordered.length;i++)if(Number(ordered[i].begin_rva)<Number(ordered[i-1].end_rva_exclusive))reasons.push('Overlapping body ranges');
  if(mode==='metadata-only'){report.status='MetadataOnlyCompleted';report.body_read_stop_reasons=reasons;}
  else if(reasons.length){report.status='BoundaryOnlyStopped';report.stop_reasons=reasons;}
  else{
    fs.mkdirSync(temp);normal(temp);report.tool_sha256_before=fingerprint(tool);assert.equal(report.tool_sha256_before,toolPin);
    if(referencedConstant){const mapped=map(Number(referencedConstant.rva),4),flags=mapped.section.characteristics;
      assert.equal(mapped.section.name,'.rdata');assert.ok((flags&0x40000000)!==0&&(flags&0x80000000)===0&&(flags&0x20000000)===0);
      const bits=Buffer.alloc(4);assert.equal(fs.readSync(fd,bits,0,4,mapped.offset),4);report.global_data_bytes=4;
      report.constant_read={...referencedConstant,file_offset:mapped.offset,hex_bytes:bits.toString('hex'),uint32_le:hex(bits.readUInt32LE(0)),sha256:sha(bits),interpreted_as_instructions:false};save('referenced-constant.bin',bits);}
    for(const b of selected){const p=b.range,begin=Number(p.begin_rva),end=Number(p.end_rva_exclusive),mapped=map(begin,p.bytes);
      assert.equal(mapped.section.name,'.text');assert.ok(mapped.section.characteristics&0x20000000);assert.ok(report.body_read_operations<2&&report.body_bytes+p.bytes<=ceilings.body_bytes_total);
      const code=Buffer.alloc(p.bytes),got=fs.readSync(fd,code,0,p.bytes,mapped.offset);report.body_read_operations++;report.body_bytes+=got;assert.equal(got,p.bytes);
      const body={id:b.id,reference_ids:b.reference_ids,begin_rva:p.begin_rva,end_rva_exclusive:p.end_rva_exclusive,bytes:p.bytes,file_offset:mapped.offset,pdata:b.pdata,bounded_window:leafMode?p:undefined,sha256:sha(code)};report.bodies.push(body);save(b.id+'.bin',code);
      const synthetic=tinyPe(begin,code),tiny=path.join(temp,b.id+'.inspect.dll');fs.writeFileSync(tiny,synthetic,{flag:'wx'});
      body.synthetic={relative_path:path.relative(root,tiny).replace(/\\/g,'/'),bytes:synthetic.length,sha256:sha(synthetic),never_executed:true};
      const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:'+hex(base+BigInt(begin))+','+hex(base+BigInt(end-1)),tiny];
      assert.ok(report.dumpbin_calls<ceilings.dumpbin_calls);report.dumpbin_calls++;body.command_arguments=args;
      const child=spawnSync(tool,args,{cwd:temp,windowsHide:true,encoding:'utf8',timeout:ceilings.tool_timeout_ms_each,maxBuffer:fixed.tool_buffer_each,
        env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:temp,TMP:temp}});
      save(b.id+'.dumpbin.txt',child.stdout||'');save(b.id+'.stderr.txt',child.stderr||'');body.tool_exit=child.status;body.tool_error=child.error?String(child.error):null;
      decode(child.stdout||'',body,code);assert.equal(fingerprint(tiny),body.synthetic.sha256);
      assert.equal(child.error,undefined);assert.equal(child.status,0);assert.equal(child.signal,null);assert.equal(child.stderr,'');
    }
    assert.equal(report.body_bytes,total);assert.equal(report.body_read_operations,selected.length);assert.equal(report.dumpbin_calls,selected.length);
    report.status=report.bodies.some(b=>b.decode_issues.length)?'DecodeIssuesReviewRequired':'FixedBodiesDecodedReviewRequired';
    if(leafMode)report.status=report.bodies[0].leaf_window.status==='StraightLineRetPrefixObserved'?'LeafPrefixObservedReviewRequired':'LeafWindowUnknown';
  }
  }
}catch(e){report.failures.push(String(e));report.status='Failed';}
finally{
  if(fd!==undefined){try{report.sha256_after=digest(fd,source.bytes);assert.equal(report.sha256_after,source.sha256);report.source_stat_stable=stable(initial,fs.fstatSync(fd));assert.ok(report.source_stat_stable);}
    catch(e){report.failures.push('Original final: '+e);}finally{try{fs.closeSync(fd);report.source_closed=true;}catch(e){report.failures.push('Close: '+e);}}}
  if(report.tool_sha256_before!==undefined){try{report.tool_sha256_after=fingerprint(tool);assert.equal(report.tool_sha256_after,toolPin);}catch(e){report.failures.push('Tool final: '+e);}}
  report.source_hash_preserved=report.sha256_before===source.sha256&&report.sha256_after===source.sha256;
  if(report.failures.length)report.status='Failed';report.finished_utc=new Date().toISOString();if(created)save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,boundaries:report.boundaries,body_bytes:report.body_bytes,metadata_bytes:report.metadata_bytes,
  dumpbin_calls:report.dumpbin_calls,source_hash_preserved:report.source_hash_preserved,output:plan.output,stop_reasons:report.stop_reasons,failures:report.failures}));
if(report.status==='Failed'||report.status==='DecodeIssuesReviewRequired')process.exitCode=1;
