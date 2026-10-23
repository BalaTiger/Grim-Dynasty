// Import performs no I/O. Fixed saved-member structural observation only.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectSavedGroundLayout(reviewedScriptSha256, reviewedPlanSha256) {
 const self=fileURLToPath(import.meta.url), root=path.resolve(path.dirname(self),'../..');
 const sha=b=>createHash('sha256').update(b).digest('hex');
 assert.equal(process.platform,'win32');
 assert.equal(root.toLowerCase(),'c:\\users\\zhuzi\\documents\\grim-dynasty');
 assert.match(reviewedScriptSha256,/^[0-9a-f]{64}$/);assert.match(reviewedPlanSha256,/^[0-9a-f]{64}$/);
 function normal(p){for(let q=path.resolve(p);;q=path.dirname(q)){
  assert(!fs.lstatSync(q).isSymbolicLink(),'Redirected path');assert.equal(fs.realpathSync.native(q).toLowerCase(),q.toLowerCase());if(path.dirname(q)===q)break;
 }}
 function local(rel){const p=path.resolve(root,rel);assert(p.startsWith(root+path.sep));return p;}
 function regular(p){normal(p);const s=fs.statSync(p);assert(s.isFile()&&s.nlink===1&&s.ino!==0);return s;}
 function same(a,b){for(const k of ['size','dev','ino','nlink','mtimeMs','ctimeMs'])assert.equal(a[k],b[k],k);}
 assert(regular(self).size<16384);assert.equal(sha(fs.readFileSync(self)),reviewedScriptSha256);
 const planPath=local('tools/static_analysis/read_r1_saved_ground_layout.plan.json');assert(regular(planPath).size<8192);
 const planBytes=fs.readFileSync(planPath);assert.equal(sha(planBytes),reviewedPlanSha256);const p=JSON.parse(planBytes);
 assert.equal(p.schema,'grim-dynasty.saved-ground-layout.v1');assert.equal(p.script,'tools/static_analysis/read_r1_saved_ground_layout.mjs');
 assert.equal(p.output,'docs/reverse-client/r1-mainmenu-ground-layout/inspection.json');
 assert.deepEqual(p.source,{path:'docs/reverse-client/r1-mainmenu-complete-member/mainmenu.map.bin',bytes:1091357,sha256:'a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'});
 assert.deepEqual(p.chunk,{id:6,offset:273260,bytes:149073,level_version:15,type:2});assert.deepEqual(p.observed_header,[1,5,129,129,0,0,0,0]);
 const evidence=new Map();for(const pin of p.evidence){const file=local(pin.path);assert(regular(file).size<1048576);const b=fs.readFileSync(file);assert.equal(sha(b),pin.sha256);evidence.set(pin.kind,JSON.parse(b));}
 function asm(kind,proc,lines){const c=evidence.get(kind).result.structuredContent;assert.equal(c.operation,'procedure_assembly');assert.equal(c.parameters.procedure,proc);assert.equal(c.subject.digest.sha256,'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');for(const line of lines)assert(c.normalized_result.split('\n').includes(line),line);}
 asm('caller','0x18012c770',['0x18012c985: MOV ECX, dword ptr [RDX]','0x18012c987: ADD RDX, 0x4','0x18012ca70: CALL qword ptr [R9 + 0x88]']);
 asm('loader','0x1801de090',['0x1801de0bd: CALL qword ptr [RAX + 0x68]','0x1801de0d5: ADD RAX, 0x20','0x1801de119: MOV dword ptr [R14 + 0x118], EAX','0x1801de14c: MOV qword ptr [R14 + 0x128], RAX','0x1801de193: MOV qword ptr [R14 + 0x120], RAX','0x1801de23f: CALL 0x1801ce350','0x1801de2cd: CALL qword ptr [RAX + 0x188]','0x1801de303: CALL 0x1801ce910','0x1801de308: ADD qword ptr [RDI + 0x8], 0x4']);
 const out=local(p.output),outDir=path.dirname(out);normal(path.dirname(outDir));
 try{fs.lstatSync(outDir);assert.fail('Existing output preserved');}catch(e){if(e.code!=='ENOENT')throw e;}
 fs.mkdirSync(outDir);normal(outDir);
 const report={schema:p.schema,started_utc:new Date().toISOString(),script_sha256:reviewedScriptSha256,plan_sha256:reviewedPlanSha256,evidence:p.evidence,status:'running',
  source:{...p.source,hash_bytes:0,source_closed:false},chunk:p.chunk,segments:[],terrain_types:[],failures:[],
  limits:{original_reads:0,backends_started:0,native_executed:false,subprocesses:0,semantic_bytes:0},
  native_load_completed:false,floor_collision_validated:false,
  conditions:['Read-order projection assumes callbacks do not replace the observed header-derived dimensions/reader cursor; callback semantics are not bypassed or declared successful.',
   'This exact saved sample has small positive sizes; native low32 unsigned products, signed MOVSXD copy lengths and malformed-input behavior are not generalized. The observer rejects out-of-chunk extents as an OWN safety rule; native memcpy sites have no corresponding explicit bounds guard.',
   'Raw array128 binary32 interpretation does not establish world spacing, triangle topology, floor intersections or playable scene.',
   'Header DWORD0 and DWORD4..7 retain raw values without invented field names; only the actual used header indices are named.',
   'The final four bytes are skipped by native code; their raw value is not asserted to be a terminator or version.']};
 let fd,initial;
 function digest(){const h=createHash('sha256'),buf=Buffer.alloc(65536);for(let offset=0;offset<p.source.bytes;){const n=fs.readSync(fd,buf,0,Math.min(buf.length,p.source.bytes-offset),offset);assert(n>0);h.update(buf.subarray(0,n));offset+=n;report.source.hash_bytes+=n;}return h.digest('hex');}
 try {
  const source=local(p.source.path),before=regular(source);assert.equal(before.size,p.source.bytes);fd=fs.openSync(source,fs.constants.O_RDONLY);initial=fs.fstatSync(fd);same(initial,before);
  report.source.sha256_before=digest();assert.equal(report.source.sha256_before,p.source.sha256);
  const data=Buffer.alloc(p.chunk.bytes);assert.equal(fs.readSync(fd,data,0,data.length,p.chunk.offset),data.length);report.limits.semantic_bytes=data.length;
  let cursor=0;
  function segment(kind,bytes){assert(Number.isSafeInteger(bytes)&&bytes>=0&&cursor+bytes<=data.length,'Chunk boundary');const begin=cursor,b=data.subarray(begin,begin+bytes);cursor+=bytes;const row={kind,chunk_offset:begin,member_offset:p.chunk.offset+begin,bytes,sha256:sha(b)};report.segments.push(row);return {b,row};}
  const kind=segment('level-ground-discriminator',4);assert.equal(kind.b.readUInt32LE(),2);kind.row.raw_hex=kind.b.toString('hex');
  const header=segment('loader-header32',32);const words=Array.from({length:8},(_,i)=>header.b.readUInt32LE(i*4));assert.deepEqual(words,p.observed_header);header.row.raw_hex=header.b.toString('hex');
  const count=words[1],dim60=words[2],dim64=words[3];
  for(const dimension of [dim60,dim64]){const x=(dimension-1)>>>0;assert.equal(((-x>>>0)&x)>>>0,x);}
  const cells=dim60*dim64,stride=(count>>>3)+1,copy128=cells*4,copy120=cells*stride,weights=(dim60-1)*(dim64-1);
  for(const n of [cells,copy128,copy120,weights])assert(Number.isSafeInteger(n)&&n>=0&&n<=0x7fffffff);
  report.header={words,terrain_type_count:count,dimension60:dim60,dimension64:dim64,stride118:stride,cells,unknown_header_indices:[0,4,5,6,7]};
  const array128=segment('raw-array-at-this128',copy128);let min=Infinity,max=-Infinity,finite=0,negativeZero=0;const samples=[];
  for(let i=0;i<cells;i++){const value=array128.b.readFloatLE(i*4);if(Number.isFinite(value)){min=Math.min(min,value);max=Math.max(max,value);finite++;}if(Object.is(value,-0))negativeZero++;if([0,1,128,129,8320,16640].includes(i))samples.push({index:i,bits:'0x'+array128.b.readUInt32LE(i*4).toString(16).padStart(8,'0'),binary32:Number.isFinite(value)?value:String(value)});}
  array128.row.binary32_observation={finite,nonfinite:cells-finite,min:finite?min:null,max:finite?max:null,negative_zero:negativeZero,samples};
  const array120=segment('raw-array-at-this120',copy120);array120.row.stride=stride;array120.row.unique_bytes=[...new Set(array120.b)].sort((a,b)=>a-b);
  for(let index=0;index<count;index++){
   const len=segment('type'+index+'-name-length',4),bytes=len.b.readUInt32LE();len.row.uint32=bytes;assert(bytes<=1048576);assert(cursor+bytes<=data.length);
   const name=segment('type'+index+'-name',bytes),ascii=[...name.b].every(b=>b>=0x20&&b<=0x7e);name.row.raw_hex=name.b.toString('hex');
   const row={index,reader_string_guard:'length<=0x100000 and known bounded cursor; low32 native address wrap not modeled',name_member_offset:name.row.member_offset,name_bytes:bytes,name_ascii:ascii?name.b.toString('ascii'):null,ascii_only:ascii,nul_bytes:[...name.b].filter(b=>b===0).length,material_object_resolved:false};
   if(index===0)row.weights=null;else{const weight=segment('type'+index+'-weights',weights);row.weights={...weight.row,unique_byte_count:new Set(weight.b).size};}
   report.terrain_types.push(row);
  }
  const trailer=segment('native-skipped4-after-ConstructBlocks',4);trailer.row.raw_hex=trailer.b.toString('hex');
  report.final_cursor={chunk_offset:cursor,member_offset:p.chunk.offset+cursor,chunk_end:p.chunk.offset+p.chunk.bytes,remaining:data.length-cursor};assert.equal(cursor,data.length,'Conditional layout did not cover the exact chunk');
  report.unresolved_native_calls=['1DE0BD primary slot68','1DE1C2 1E0D60 vector sizing','1DE232 ObjectManager singleton','1DE23F 1CE350 terrain-type resolution','1DE274 optional logging','1DE2CD primary slot188','1DE303 1CE910 ConstructBlocks'];
  report.status='passed-conditional-saved-layout-only';
 } catch(e){report.status='failed';report.failures.push(String(e));}
 finally{if(fd!==undefined){try{report.source.sha256_after=digest();assert.equal(report.source.sha256_after,p.source.sha256);same(fs.fstatSync(fd),initial);same(regular(local(p.source.path)),initial);report.source.stable=true;}catch(e){report.status='failed';report.failures.push(String(e));}finally{fs.closeSync(fd);report.source.source_closed=true;}}}
 report.finished_utc=new Date().toISOString();normal(outDir);const serialized=JSON.stringify(report,null,2)+'\n';assert(Buffer.byteLength(serialized)<65536);fs.writeFileSync(out,serialized,{flag:'wx'});
 if(report.status==='failed')throw new Error('Saved ground observation failed; evidence retained: '+report.failures.join('; '));
 return {status:report.status,output:p.output,header:report.header,terrain_types:report.terrain_types,final_cursor:report.final_cursor,source:report.source,native_load_completed:false,floor_collision_validated:false};
}
