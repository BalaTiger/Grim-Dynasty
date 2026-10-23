// Preparation only: execute solely after the separate review/approval.
// Exactly two pinned Game .pdata fragments: 42 + 38 = 80 code bytes.
// This file is independent of any previously rejected build operation.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const scriptFile = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(scriptFile), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty', 'Unexpected project root');
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expectedHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const expectedSize = 11385856, imageBase = 0x180000000n;
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const expectedToolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-item-factory-dependencies');
const tempDirectory = path.join(root, '.tooling/offline-item-factory-dependencies-temp');
const sources = [
  {path:'docs/object-lifecycle/offline-item-factory-dependency-boundaries/metadata.json',
    sha256:'59265ca3af388f48af8da9a66bee439949fa8cfeca5fe1cc40a066998ad46d29'},
  {path:'docs/object-lifecycle/offline-class-registration-metadata/metadata.json',
    sha256:'54aa510cffe5a281c54e6de4a594f40224585822f0b40cad6c847f8318ef76b7'},
  {path:'docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json',
    sha256:'7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f'},
];
const plans = [
  {role:'Item-classInfo-plus28-factory', begin:0x104e00, end:0x104e2a,
    pdataRva:0xa77c14, pdataOffset:10952724, codeOffset:1065472, unwind:0x7f5edc, entryIndex:4695},
  {role:'Item-plus858-direct-helper', begin:0x1fa340, end:0x1fa366,
    pdataRva:0xa81808, pdataOffset:10992648, codeOffset:2070336, unwind:0x7deb10, entryIndex:8022},
];
const limits = Object.freeze({code_bytes:80, metadata_bytes:24, saved_proof_bytes_per_file:131072,
  fingerprint_chunk_bytes:65536, tool_timeout_ms:10000, tool_max_buffer_bytes:196608});
const hex = value => '0x' + value.toString(16);
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const fits = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && offset <= size && length <= size-offset;
function normalAncestors(name, directory) {
  let current = path.resolve(name), first = true;
  while (true) {
    const stat = fs.lstatSync(current);
    assert.ok(!stat.isSymbolicLink(), 'Refuse redirected paths');
    assert.ok(first && !directory ? stat.isFile() : stat.isDirectory(), 'Unexpected path type');
    const parent = path.dirname(current);
    if (parent === current) return;
    current = parent; first = false;
  }
}
function assertAbsent(name) {
  try { fs.lstatSync(name); }
  catch (error) { if (error.code === 'ENOENT') return; throw error; }
  assert.fail('Refuse existing output/temp path, including links');
}
function fingerprintFile(name) {
  const fd = fs.openSync(name, fs.constants.O_RDONLY), buffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
  const digest = createHash('sha256'); let total = 0;
  try {
    const before = fs.fstatSync(fd); assert.ok(before.isFile());
    let count;
    while ((count = fs.readSync(fd, buffer, 0, buffer.length, null)) > 0) { digest.update(buffer.subarray(0, count)); total += count; }
    const after = fs.fstatSync(fd);
    assert.ok(total === before.size && after.size === before.size && after.dev === before.dev && after.ino === before.ino &&
      after.mtimeMs === before.mtimeMs && after.ctimeMs === before.ctimeMs, 'Fingerprint file changed');
  } finally { fs.closeSync(fd); }
  return {sha256:digest.digest('hex'), bytes:total, chunk_bytes:buffer.length};
}
function loadProof(source) {
  const name = path.join(root, source.path); normalAncestors(name, false);
  assert.ok(fs.statSync(name).size <= limits.saved_proof_bytes_per_file, 'Saved proof size exceeds budget');
  const bytes = fs.readFileSync(name);
  assert.ok(bytes.length <= limits.saved_proof_bytes_per_file, 'Saved proof grew beyond budget');
  assert.equal(hash(bytes), source.sha256, 'Saved proof hash changed');
  return JSON.parse(bytes.toString('utf8'));
}
normalAncestors(path.dirname(outputDirectory), true);
normalAncestors(path.dirname(tempDirectory), true);
assertAbsent(outputDirectory); assertAbsent(tempDirectory);
fs.mkdirSync(outputDirectory); fs.mkdirSync(tempDirectory);
normalAncestors(outputDirectory, true); normalAncestors(tempDirectory, true);
const save = (name, bytes) => fs.writeFileSync(path.join(outputDirectory, name), bytes, {flag:'wx'});
const report = {
  schema:'grim-dynasty-item-factory-dependency-disassembly-v1', status:'started', started_utc:new Date().toISOString(),
  original, expected_sha256:expectedHash, expected_bytes:expectedSize, tool, expected_tool_sha256:expectedToolHash,
  source_evidence:sources, limits, code_bytes:0, metadata_bytes:0, metadata_reads:[], fingerprint_passes:[],
  fragments:plans.map(plan => ({role:plan.role, requested_rva:hex(plan.begin), status:'not-read',
    start_va:hex(imageBase+BigInt(plan.begin)), end_va_exclusive:hex(imageBase+BigInt(plan.end)),
    expected_code_bytes:plan.end-plan.begin, body_read:false, byte_coverage_verified:false})),
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, original_modified:false, target_copy_created:false,
  backend_started:false, installations:false, permission_escalation:false, subprocesses:0,
  restrictions:'Only these two exact fragments; no adjacent ranges, continuations, callee following, DLL execution, build, policy change or player data.',
  limitations:[
    'A .pdata entry may be a fragment, not a complete logical function. Byte coverage does not prove complete control flow, ABI, EH, allocator or failure behavior.',
    'Missing exact entries or changed boundaries stop all code reads; no neighboring or containing body is selected.',
    '64KiB streaming full-file fingerprints traverse all bytes without interpreting them; those bytes are separate from the 80 code and 24 metadata bytes.',
    'dumpbin /RANGE limits requested disassembly output. Its internal PE metadata I/O is not instrumented.',
    'The installed dumpbin reads the original as data. Neither Game.dll nor its imports are loaded or executed as code.',
    'Same-handle stat/hash checks require a stable file and cannot exclude every transient concurrent change.',
  ],
};
let fd, beforeStat, closed = false, toolHashAttempted = false;
const failures = [], hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
function originalHash(label) {
  const digest = createHash('sha256'); let position = 0;
  while (position < expectedSize) {
    const count = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, expectedSize-position), position);
    assert.ok(count > 0, 'Unexpected fingerprint EOF');
    digest.update(hashBuffer.subarray(0, count)); position += count;
  }
  const pass = {label, bytes:position, chunk_bytes:hashBuffer.length, sha256:digest.digest('hex')};
  report.fingerprint_passes.push(pass); return pass.sha256;
}
function readFixed(offset, length, kind) {
  assert.ok(fits(offset, length, expectedSize), 'Read outside the fixed Game file');
  if (kind === 'pdata-recheck') {
    assert.equal(length, 12); assert.ok(report.metadata_bytes+length <= limits.metadata_bytes);
    report.metadata_bytes += length;
  } else { assert.equal(kind, 'code-fragment'); assert.ok(report.code_bytes+length <= limits.code_bytes); report.code_bytes += length; }
  const bytes = Buffer.alloc(length); let done = 0;
  while (done < length) { const count = fs.readSync(fd, bytes, done, length-done, offset+done); assert.ok(count > 0, 'Unexpected fixed read EOF'); done += count; }
  if (kind === 'pdata-recheck') report.metadata_reads.push({file_offset:offset, bytes:length, raw_hex:bytes.toString('hex')});
  return bytes;
}
try {
  normalAncestors(scriptFile, false); report.script_sha256 = fingerprintFile(scriptFile).sha256;
  const proofs = sources.map(loadProof), boundary = proofs[0];
  assert.equal(boundary.schema, 'grim-dynasty-item-factory-dependency-pdata-v1');
  assert.equal(boundary.target, original); assert.equal(boundary.expected_sha256, expectedHash); assert.equal(boundary.expected_bytes, expectedSize);
  assert.ok(boundary.source_handle_closed && boundary.source_stat_stable && boundary.source_hash_preserved);
  assert.equal(boundary.sha256_before, expectedHash); assert.equal(boundary.sha256_after, expectedHash);
  assert.deepEqual(boundary.source_evidence, sources.slice(1));
  assert.equal(boundary.pe.image_base, hex(imageBase));
  const game = proofs[1].files.find(file => file.target === original);
  assert.equal(proofs[1].status, 'fixed-class-registration-metadata-verified');
  assert.ok(game && game.source_hash_preserved && game.source_stat_stable);
  assert.equal(game.sha256_before, expectedHash); assert.equal(game.sha256_after, expectedHash);
  const item = game.records.find(record => record.name === 'Item');
  assert.equal(item.rva, '0xa5c8a0'); assert.equal(item.qwords[5], '0x180104e00');
  assert.equal(Buffer.from(item.hex, 'hex').readBigUInt64LE(0x28), imageBase+0x104e00n);
  const assembly = proofs[2].result.structuredContent;
  assert.equal(assembly.subject.digest.sha256, expectedHash); assert.equal(assembly.operation, 'procedure_assembly');
  assert.equal(assembly.parameters.procedure, '0x180323690');
  for (const line of ['0x1803236b0: MOV RDI, RCX', '0x180323702: LEA RCX, [RDI + 0x858]',
    '0x180323709: MOV qword ptr [RSP + 0x58], RCX', '0x18032370e: CALL 0x1801fa340'])
    assert.equal(assembly.normalized_result.split(/\r?\n/).filter(value => value === line).length, 1);
  assert.equal(boundary.assessments.length, 2); assert.equal(boundary.requested.length, 2);
  const exact = [];
  // Validate EVERY target before opening the original or starting dumpbin.
  for (let index=0; index<plans.length; index++) {
    const plan=plans[index], fragment=report.fragments[index];
    const assessment=boundary.assessments.find(value => value.role===plan.role && value.requested_rva===hex(plan.begin));
    const candidates=boundary.exact_boundaries.filter(value => value.role===plan.role && value.begin_rva===hex(plan.begin));
    if (!assessment || assessment.status!=='exact-begin' || candidates.length!==1) {
      fragment.not_read_reason='Missing exact .pdata entry; no containing/neighbor body permitted';
      assert.fail(fragment.not_read_reason);
    }
    const entry=candidates[0];
    assert.equal(entry.end_rva_exclusive, hex(plan.end)); assert.equal(entry.range_bytes, plan.end-plan.begin);
    assert.equal(entry.entry_rva, hex(plan.pdataRva)); assert.equal(entry.file_offset, plan.pdataOffset);
    assert.equal(entry.code_file_offset, plan.codeOffset); assert.equal(entry.unwind_rva, hex(plan.unwind));
    assert.equal(entry.index, plan.entryIndex); assert.equal(entry.section, '.text'); assert.equal(entry.code_bytes_read, 0);
    assert.equal(entry.begin_va, hex(imageBase+BigInt(plan.begin))); assert.equal(entry.end_va_exclusive, hex(imageBase+BigInt(plan.end)));
    const raw=Buffer.from(entry.raw_hex, 'hex'); assert.equal(raw.length, 12);
    assert.equal(raw.readUInt32LE(0), plan.begin); assert.equal(raw.readUInt32LE(4), plan.end); assert.equal(raw.readUInt32LE(8), plan.unwind);
    const code=boundary.pe.sections.filter(s => s.name==='.text' && plan.begin>=s.rva && fits(plan.begin-s.rva,plan.end-plan.begin,s.raw_size));
    const pdata=boundary.pe.sections.filter(s => s.name==='.pdata' && plan.pdataRva>=s.rva && fits(plan.pdataRva-s.rva,12,s.raw_size));
    assert.equal(code.length,1); assert.equal(pdata.length,1); assert.ok(code[0].characteristics & 0x20000000);
    assert.equal(code[0].raw_offset+plan.begin-code[0].rva,plan.codeOffset);
    assert.equal(pdata[0].raw_offset+plan.pdataRva-pdata[0].rva,plan.pdataOffset);
    exact.push(entry); fragment.exact_boundary_verified=true;
  }
  assert.equal(boundary.status, 'all-fixed-exact-boundaries-assessed'); assert.equal(boundary.exact_boundaries.length, 2);
  normalAncestors(tool, false); toolHashAttempted=true;
  report.tool_fingerprint_before=fingerprintFile(tool); assert.equal(report.tool_fingerprint_before.sha256, expectedToolHash);
  normalAncestors(original, false); fd=fs.openSync(original, fs.constants.O_RDONLY); beforeStat=fs.fstatSync(fd);
  assert.ok(beforeStat.isFile() && beforeStat.size===expectedSize, 'Unexpected fixed Game file/size');
  report.original_sha256_before=originalHash('original-before'); assert.equal(report.original_sha256_before, expectedHash);
  // No metadata search: exactly the two recorded 12-byte entries are rechecked.
  for (let index=0; index<plans.length; index++) {
    const bytes=readFixed(plans[index].pdataOffset,12,'pdata-recheck'); assert.equal(bytes.toString('hex'),exact[index].raw_hex);
  }
  for (let index=0; index<plans.length; index++) {
    const plan=plans[index], fragment=report.fragments[index], length=plan.end-plan.begin;
    const start=imageBase+BigInt(plan.begin), end=imageBase+BigInt(plan.end), label=plan.begin.toString(16);
    const bytes=readFixed(plan.codeOffset,length,'code-fragment'); fragment.body_read=true; fragment.code_bytes=length;
    fragment.code_sha256=hash(bytes); fragment.status='body-read'; save(hex(start)+'.bin',bytes);
    const args=['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES',`/RANGE:${hex(start)},${hex(end-1n)}`,original];
    fragment.command_arguments=args;
    const started=Date.now(); report.subprocesses++;
    const child=spawnSync(tool,args,{cwd:root,windowsHide:true,encoding:'utf8',timeout:limits.tool_timeout_ms,maxBuffer:limits.tool_max_buffer_bytes,
      env:{SystemRoot:'C:\\Windows',WINDIR:'C:\\Windows',PATH:path.dirname(tool),TEMP:tempDirectory,TMP:tempDirectory}});
    fragment.tool_elapsed_ms=Date.now()-started; fragment.tool_exit=child.status; fragment.tool_signal=child.signal;
    fragment.tool_error=child.error?String(child.error):null;
    save(label+'-dumpbin.txt',child.stdout||''); save(label+'-stderr.txt',child.stderr||'');
    assert.equal(child.error,undefined); assert.equal(child.status,0); assert.equal(child.signal,null); assert.equal(child.stderr,'');
    const rows=[];
    for (const line of child.stdout.split(/\r?\n/)) {
      const instruction=/^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
      if (instruction) {rows.push({address:BigInt('0x'+instruction[1]),bytes:instruction[2].replace(/\s/g,'').toLowerCase(),instruction:instruction[3].trim()});continue;}
      const continuation=/^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
      if (continuation) {assert.ok(rows.length>0);rows[rows.length-1].bytes+=continuation[1].replace(/\s/g,'').toLowerCase();}
    }
    let cursor=start;
    for (const row of rows) {
      assert.ok(row.instruction && row.bytes.length>0 && row.bytes.length%2===0 && row.bytes.length<=30);
      assert.equal(row.address,cursor); cursor+=BigInt(row.bytes.length/2); assert.ok(cursor<=end);
    }
    assert.equal(cursor,end); assert.equal(rows.map(row=>row.bytes).join(''),bytes.toString('hex'));
    save(label+'-instructions.json',JSON.stringify(rows.map(row=>({...row,address:hex(row.address)})),null,2)+'\n');
    fragment.instructions=rows.length; fragment.byte_coverage_verified=true; fragment.status='byte-coverage-verified';
  }
  assert.equal(report.code_bytes,80); assert.equal(report.metadata_bytes,24); assert.equal(report.subprocesses,2);
} catch (error) { failures.push(String(error)); }
finally {
  if (fd!==undefined) {
    try {
      if (beforeStat && beforeStat.size===expectedSize) {
        report.original_sha256_after=originalHash('original-after'); assert.equal(report.original_sha256_after,expectedHash);
        const after=fs.fstatSync(fd);
        report.original_stat_stable=after.isFile() && after.size===beforeStat.size && after.dev===beforeStat.dev && after.ino===beforeStat.ino &&
          after.mtimeMs===beforeStat.mtimeMs && after.ctimeMs===beforeStat.ctimeMs;
        assert.ok(report.original_stat_stable,'Original stat changed');
      }
    } catch (error) { failures.push('Original final verification: '+String(error)); }
    finally { try {fs.closeSync(fd);closed=true;} catch (error) {failures.push('Original close: '+String(error));} }
  } else closed=true;
  if (toolHashAttempted) {
    try {report.tool_fingerprint_after=fingerprintFile(tool);assert.equal(report.tool_fingerprint_after.sha256,expectedToolHash);}
    catch (error) {failures.push('Tool final verification: '+String(error));}
  }
  report.source_handle_closed=closed;
  report.original_hash_matches=report.original_sha256_before===expectedHash && report.original_sha256_after===expectedHash;
  report.tool_hash_matches=report.tool_fingerprint_before?.sha256===expectedToolHash && report.tool_fingerprint_after?.sha256===expectedToolHash;
  if (!closed) failures.push('Source close not confirmed');
  for (const fragment of report.fragments) if (!fragment.body_read && !fragment.not_read_reason)
    fragment.not_read_reason='Not reached; fixed-scope validation or a preceding operation stopped this batch';
  report.status=failures.length?'failed':'fixed-item-factory-dependency-disassembly-verified';
  report.failures=failures; report.finished_utc=new Date().toISOString(); report.controller_resource_usage=process.resourceUsage();
  normalAncestors(outputDirectory,true); save('inspection.json',JSON.stringify(report,null,2)+'\n');
}
console.log(JSON.stringify({status:report.status,code_bytes:report.code_bytes,metadata_bytes:report.metadata_bytes,
  instructions:report.fragments.reduce((count,fragment)=>count+(fragment.instructions||0),0),
  original_unchanged:report.original_hash_matches,tool_unchanged:report.tool_hash_matches,
  source_handle_closed:report.source_handle_closed,controller_peak_rss_kib:report.controller_resource_usage.maxRSS}));
if (failures.length) process.exitCode=1;
