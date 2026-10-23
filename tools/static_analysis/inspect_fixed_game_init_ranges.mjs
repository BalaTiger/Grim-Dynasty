// Read-only fixed Game.dll fragments supplied by an explicit project evidence
// plan. Only the already-installed dumpbin runs; never load or execute Game.dll.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
assert.equal(process.argv.length,3,'One fixed project plan is required');
const root='C:/Users/zhuzi/Documents/Grim-Dynasty';process.chdir(root);
const planPath=process.argv[2].replaceAll('\\','/');
assert(/^docs\/object-lifecycle\/class-static-init-plans\/[a-z0-9-]+\.json$/.test(planPath));
const hash=b=>createHash('sha256').update(b).digest('hex'),hex=n=>'0x'+n.toString(16);
function local(p){
 assert(!path.isAbsolute(p)&&!p.split(/[\\/]/).includes('..'));
 assert(p.startsWith('docs/')||p.startsWith('tools/')||p.startsWith('.tooling/'));
 const full=path.resolve(root,p);let d=full;
 while(d!==path.resolve(root)){assert(!fs.lstatSync(d).isSymbolicLink(),p);d=path.dirname(d);}
 return full;
}
const planRaw=fs.readFileSync(local(planPath)),plan=JSON.parse(planRaw);
assert(/^[a-z0-9-]+$/.test(plan.stage));
assert(plan.targets.length>0&&plan.targets.length<=512);
assert(Number.isInteger(plan.code_byte_limit)&&plan.code_byte_limit>0&&plan.code_byte_limit<=65536);
const original='D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expected='cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool='D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const expectedTool='cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const pePath='docs/object-lifecycle/offline-questitem-boundaries/boundary.json';
const peRaw=fs.readFileSync(local(pePath));
assert.equal(hash(peRaw),'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05');
const pe=JSON.parse(peRaw).pe;
assert(plan.source_evidence.length>0);
for(const e of plan.source_evidence)assert.equal(hash(fs.readFileSync(local(e.path))),e.sha256,e.path);
const unique=new Set();
for(const t of plan.targets){
 assert(Number.isSafeInteger(t.rva)&&t.rva>0&&!unique.has(t.rva));unique.add(t.rva);
 assert(Number.isInteger(t.maximum_bytes)&&t.maximum_bytes>0&&t.maximum_bytes<=4096);
 assert(typeof t.basis==='string'&&t.basis.length>0);
 if(t.leaf_bytes!==undefined)assert([6,8,16,32].includes(t.leaf_bytes));
}
// Table batches must prove every code target and share one cumulative budget.
let priorCodeBytes=0,table;
if(plan.table_binding){
 assert.equal(plan.table_binding.path,'docs/object-lifecycle/offline-game-class-init-table/metadata.json');
 const b=fs.readFileSync(local(plan.table_binding.path));
 assert.equal(hash(b),plan.table_binding.sha256);
 table=JSON.parse(b);assert.equal(table.slots.length,440);
 assert(plan.source_evidence.some(e=>e.path===plan.table_binding.path&&e.sha256===hash(b)));
 assert(plan.targets.length<=32&&plan.code_byte_limit<=8192);
 assert(plan.prior_inspections);
}
if(plan.prior_inspections){
 assert(plan.prior_inspections.length>=5);
 const priorPaths=new Set();
 for(const e of plan.prior_inspections){
  assert(/^docs\/object-lifecycle\/offline-class-static-[a-z0-9-]+\/inspection\.json$/.test(e.path));
  assert(!priorPaths.has(e.path));priorPaths.add(e.path);
  const raw=fs.readFileSync(local(e.path));assert.equal(hash(raw),e.sha256);
  const old=JSON.parse(raw);assert.equal(old.status,'fixed-game-init-fragments-verified');
  assert.equal(old.original_hash_matches,true);priorCodeBytes+=old.code_bytes;
 }
 for(const stage of ['crt-entry','crt-dispatch','crt-reason','crt-attach','crt-thunks'])
  assert(priorPaths.has(`docs/object-lifecycle/offline-class-static-${stage}/inspection.json`));
 for(const name of fs.readdirSync('docs/object-lifecycle').filter(n=>/^offline-class-static-table-batch-\d+$/.test(n)))
  assert(priorPaths.has(`docs/object-lifecycle/${name}/inspection.json`),'Missing prior table batch');
 if(plan.table_binding)for(const t of plan.targets){
  const matches=table.distinct_targets.filter(e=>Number(e.target_rva)===t.rva);
  assert.equal(matches.length,1);const e=matches[0];
  assert(e.executable&&e.exact_boundary&&e.slot_indices.length>0);
  assert.equal(Number(e.exact_boundary.begin_rva),t.rva);
  assert.equal(e.exact_boundary.bytes,t.maximum_bytes);
  assert.equal(t.leaf_bytes,undefined);
 }
 assert(priorCodeBytes+plan.code_byte_limit<=65536,'Global code-read budget exceeded');
}
const out='docs/object-lifecycle/offline-class-static-'+plan.stage;
const temp='.tooling/class-static-'+plan.stage+'-temp';
assert(!fs.existsSync(out));assert(!fs.existsSync(temp));
for(const d of ['docs','docs/object-lifecycle','.tooling'])assert(!fs.lstatSync(d).isSymbolicLink());
function fileHash(p){
 const fd=fs.openSync(p,'r'),b=Buffer.alloc(65536),h=createHash('sha256');
 try{for(;;){const n=fs.readSync(fd,b,0,b.length,null);if(!n)break;h.update(b.subarray(0,n));}}
 finally{fs.closeSync(fd);}return h.digest('hex');
}
const toolBefore=fileHash(tool);assert.equal(toolBefore,expectedTool);
fs.mkdirSync(out);fs.mkdirSync(temp);
const save=(n,v)=>fs.writeFileSync(out+'/'+n,v,{flag:'wx'});
const result={status:'started',started_utc:new Date().toISOString(),stage:plan.stage,original,expected_sha256:expected,
 plan:{path:planPath,sha256:hash(planRaw)},source_evidence:plan.source_evidence,
 script_sha256:hash(fs.readFileSync('tools/static_analysis/inspect_fixed_game_init_ranges.mjs')),
 tool,tool_sha256_before:toolBefore,code_byte_limit:plan.code_byte_limit,code_bytes:0,
 prior_code_bytes:priorCodeBytes,global_code_byte_limit:plan.prior_inspections?65536:null,
 metadata_byte_limit:65536,metadata_bytes:0,metadata_reads:[],fragments:[],
 target_loaded_or_executed:false,backend_started:false,full_target_copy_created:false,
 limitations:['Only explicitly planned targets are followed; no callee, table or neighboring code discovery is automatic.',
 'Unwind ranges are fragments, not a claim of whole-function or exception-path coverage.',
 'Full-file fingerprints use 64KiB buffers and are outside semantic byte totals; dumpbin internal metadata I/O is not measured.',
 'All target bytes are read-only. No DLL initialization, game launch, backend, install or permission change.']};
let fd,stat;const failures=[],cache=new Map();
function mapping(rva,n,executable){
 const ss=pe.sections.filter(s=>rva>=s.rva&&rva+n<=s.rva+s.raw_size);assert.equal(ss.length,1);
 const s=ss[0];assert.equal(Boolean(s.characteristics&0x20000000),executable);
 return s.raw_offset+rva-s.rva;
}
function readAt(off,n){assert(off>=0&&n>0&&off+n<=stat.size);const b=Buffer.alloc(n);
 let p=0;while(p<n){const k=fs.readSync(fd,b,p,n-p,off+p);assert(k>0);p+=k;}return b;}
function entry(i){
 assert(i>=0&&i<pe.entry_count);if(cache.has(i))return cache.get(i);
 assert(result.metadata_bytes+12<=65536);
 const rva=Number(pe.exception_directory_rva)+i*12,off=mapping(rva,12,false),b=readAt(off,12);
 result.metadata_bytes+=12;result.metadata_reads.push({index:i,rva:hex(rva),file_offset:off,bytes:b.toString('hex')});
 const e={index:i,begin:b.readUInt32LE(0),end:b.readUInt32LE(4),unwind:b.readUInt32LE(8),entry_rva:rva};
 assert(e.begin<e.end);cache.set(i,e);return e;
}
try{
 result.original_sha256_before=fileHash(original);assert.equal(result.original_sha256_before,expected);
 fd=fs.openSync(original,'r');stat=fs.fstatSync(fd);assert.equal(stat.size,11385856);
 for(const t of plan.targets){
  let lo=0,hi=pe.entry_count,steps=0;
  while(lo<hi){assert(steps++<32);const mid=lo+Math.floor((hi-lo)/2),e=entry(mid);if(e.begin<t.rva)lo=mid+1;else hi=mid;}
  const next=lo<pe.entry_count?entry(lo):null,prev=lo>0?entry(lo-1):null;
  let end,kind;
  if(next&&next.begin===t.rva){end=next.end;kind='exact-unwind-fragment';}
  else{
   assert(t.leaf_bytes!==undefined,'No exact unwind boundary; explicit fixed leaf length is required');
   assert(!prev||prev.end<=t.rva);end=t.rva+t.leaf_bytes;assert(!next||next.begin>=end);
   kind='explicit-fixed-entry-prefix-no-exact-unwind';
  }
  const size=end-t.rva;assert(size<=t.maximum_bytes,'Fixed per-target code budget exceeded');
  assert(result.code_bytes+size<=plan.code_byte_limit,'Fixed total code budget exceeded');
  const bytes=readAt(mapping(t.rva,size,true),size);result.code_bytes+=size;
  const startVA=0x180000000n+BigInt(t.rva),endVA=0x180000000n+BigInt(end),label=t.rva.toString(16);
  const fragment={role:t.role,basis:t.basis,start_rva:hex(t.rva),end_rva_exclusive:hex(end),range_kind:kind,
   start_va:hex(startVA),end_va_exclusive:hex(endVA),code_bytes:size,code_sha256:hash(bytes),
   exact_unwind_entry:kind==='exact-unwind-fragment'?next:null};
  result.fragments.push(fragment);save(label+'.bin',bytes);
  const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(startVA)},${hex(endVA-1n)}`,original];
  fragment.command_arguments=args;
  const r=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:10000,maxBuffer:262144,
   env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:path.resolve(temp),TMP:path.resolve(temp)}});
  save(label+'-dumpbin.txt',r.stdout||'');save(label+'-stderr.txt',r.stderr||'');
  fragment.tool_exit=r.status;fragment.tool_error=r.error?String(r.error):null;
  assert.equal(r.error,undefined);assert.equal(r.status,0);assert.equal(r.stderr,'');
  const rows=[];
  for(const line of r.stdout.split(/\r?\n/)){
   const m=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
   if(m){rows.push({address:BigInt('0x'+m[1]),bytes:m[2].replace(/\s/g,'').toLowerCase(),instruction:m[3].trim()});continue;}
   const c=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
   if(c){assert(rows.length);rows.at(-1).bytes+=c[1].replace(/\s/g,'').toLowerCase();}
  }
  let cursor=startVA;
  for(const row of rows){assert(row.instruction&&row.bytes.length>0&&row.bytes.length%2===0&&row.bytes.length<=30);
   assert.equal(row.address,cursor);cursor+=BigInt(row.bytes.length/2);assert(cursor<=endVA);}
  assert.equal(cursor,endVA);assert.equal(rows.map(x=>x.bytes).join(''),bytes.toString('hex'));
  save(label+'-instructions.json',JSON.stringify(rows.map(x=>({...x,address:hex(x.address)})),null,2)+'\n');
  fragment.instructions=rows.length;fragment.byte_coverage_verified=true;
  if(t.require_terminal_ret)assert.equal(rows.at(-1).instruction.toLowerCase(),'ret');
 }
}catch(e){failures.push(String(e));}
finally{
 if(fd!==undefined)try{fs.closeSync(fd);}catch(e){failures.push('close: '+String(e));}
 try{result.original_sha256_after=fileHash(original);assert.equal(result.original_sha256_after,expected);
  result.original_hash_matches=result.original_sha256_before===expected;
  if(stat){const after=fs.statSync(original);result.original_stat_stable=after.size===stat.size&&after.mtimeMs===stat.mtimeMs&&after.ctimeMs===stat.ctimeMs;assert(result.original_stat_stable);}
 }catch(e){failures.push(String(e));}
 try{result.tool_sha256_after=fileHash(tool);assert.equal(result.tool_sha256_after,expectedTool);result.tool_hash_matches=true;}
 catch(e){failures.push(String(e));}
 result.status=failures.length?'failed':'fixed-game-init-fragments-verified';result.failures=failures;
 result.finished_utc=new Date().toISOString();result.controller_resource_usage=process.resourceUsage();
 save('inspection.json',JSON.stringify(result,null,2)+'\n');
}
console.log(JSON.stringify({status:result.status,code_bytes:result.code_bytes,metadata_bytes:result.metadata_bytes,
 fragments:result.fragments.map(f=>({role:f.role,start:f.start_va,end:f.end_va_exclusive,instructions:f.instructions})),
 original_hash_matches:result.original_hash_matches,failures}));
if(failures.length)process.exitCode=1;
