// Preparation-only fixed scope: run only after the separate review/approval.
// PE metadata only. No code/unwind-body reads, disassembler, child or DLL load.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty', 'Unexpected project root');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expectedHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const expectedSize = 11385856, imageBase = 0x180000000n;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-item-factory-dependency-boundaries');
const output = path.join(outputDirectory, 'metadata.json');
const sources = [
  {path:'docs/object-lifecycle/offline-class-registration-metadata/metadata.json',
    sha256:'54aa510cffe5a281c54e6de4a594f40224585822f0b40cad6c847f8318ef76b7'},
  {path:'docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json',
    sha256:'7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f'},
];
const selected = [
  {role:'Item-classInfo-plus28-factory', rva:0x104e00, source_index:0},
  {role:'Item-plus858-direct-helper', rva:0x1fa340, source_index:1},
];
const limits = Object.freeze({
  fingerprint_chunk_bytes:65536, metadata_single_read_bytes:4096,
  metadata_total_bytes:8192, metadata_read_operations:96,
  binary_search_steps_per_target:32, header_end:65536, sections:96,
  exception_directory_bytes:2*1024*1024, saved_proof_bytes_per_file:65536,
  missing_exact_additional_previous_entries_per_target:1,
});
const hex = value => '0x' + value.toString(16);
const fits = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && Number.isSafeInteger(size) &&
  offset <= size && length <= size-offset;
function normalAncestors(name, directory) {
  let current = path.resolve(name), first = true;
  while (true) {
    const stat = fs.lstatSync(current);
    assert.ok(!stat.isSymbolicLink(), 'Refuse redirected project paths');
    assert.ok(first && !directory ? stat.isFile() : stat.isDirectory(), 'Unexpected project path type');
    const parent = path.dirname(current);
    if (parent === current) return;
    current = parent; first = false;
  }
}
function assertAbsent(name) {
  try { fs.lstatSync(name); }
  catch (error) { if (error.code === 'ENOENT') return; throw error; }
  assert.fail('Refuse an existing output directory, including links');
}
normalAncestors(path.join(root, 'docs/object-lifecycle'), true);
assertAbsent(outputDirectory);
const proofs = sources.map(source => {
  const file = path.join(root, source.path);
  normalAncestors(file, false);
  assert.ok(fs.statSync(file).size <= limits.saved_proof_bytes_per_file, 'Saved proof exceeds budget');
  const bytes = fs.readFileSync(file);
  assert.ok(bytes.length <= limits.saved_proof_bytes_per_file, 'Saved proof grew beyond budget');
  assert.equal(createHash('sha256').update(bytes).digest('hex'), source.sha256, 'Saved proof changed');
  return JSON.parse(bytes.toString('utf8'));
});
assert.equal(proofs[0].status, 'fixed-class-registration-metadata-verified');
const game = proofs[0].files.find(file => file.target === target);
assert.ok(game && game.source_hash_preserved && game.source_stat_stable);
assert.equal(game.sha256_before, expectedHash); assert.equal(game.sha256_after, expectedHash);
const item = game.records.find(record => record.name === 'Item');
assert.equal(item.rva, '0xa5c8a0');
assert.equal(item.qwords[5], '0x180104e00');
assert.equal(Buffer.from(item.hex, 'hex').readBigUInt64LE(0x28), imageBase + 0x104e00n);
const assembly = proofs[1].result.structuredContent;
assert.equal(assembly.subject.digest.sha256, expectedHash);
assert.equal(assembly.operation, 'procedure_assembly');
assert.equal(assembly.parameters.procedure, '0x180323690');
assert.equal(typeof assembly.normalized_result, 'string');
const lines = assembly.normalized_result.split(/\r?\n/);
const requiredLines = [
  '0x1803236b0: MOV RDI, RCX',
  '0x180323702: LEA RCX, [RDI + 0x858]',
  '0x180323709: MOV qword ptr [RSP + 0x58], RCX',
  '0x18032370e: CALL 0x1801fa340',
];
for (const line of requiredLines) assert.equal(lines.filter(value => value === line).length, 1, 'Missing exact saved caller instruction');
const result = {
  schema:'grim-dynasty-item-factory-dependency-pdata-v1', status:'started',
  started_utc:new Date().toISOString(), target, expected_sha256:expectedHash, expected_bytes:expectedSize,
  source_evidence:sources, caller_evidence:{evidence_id:assembly.evidence_id, exact_lines:requiredLines},
  factory_evidence:{classinfo_rva:item.rva, field_offset:'0x28', raw_hex:item.hex, factory_va:item.qwords[5]},
  requested:selected.map(value => ({...value, rva:hex(value.rva), preferred_va:hex(imageBase+BigInt(value.rva))})),
  limits, target_open_mode:'O_RDONLY', target_loaded_or_executed:false, backend_started:false,
  child_process_started:false, dumpbin_started:false, target_copy_created:false,
  code_bytes_interpreted:0, unwind_body_bytes_read:0, whole_exception_table_scanned:false,
  metadata_bytes:0, metadata_read_operations:0, metadata_reads:[], fingerprint_passes:[],
  assessments:[], exact_boundaries:[],
  limitations:[
    'Two full-file hashes traverse all bytes without code interpretation; each uses the same 64 KiB buffer and read-only handle. Semantic metadata reads are accounted separately.',
    'Binary search assumes the PE exception directory is sorted by BeginAddress; only encountered ordering is checked, without a whole-table scan.',
    'An exact .pdata entry may describe only a function fragment. It does not establish complete control flow, constructor semantics, or a callable ABI.',
    'Missing exact entries retain lower-bound and already-read adjacent metadata, with at most one necessary preceding entry read. Containing/gap reports never authorize adjacent code reads.',
    'No code, unwind body, callee, import or export table is read by the semantic reader. Preferred addresses are not runtime pointers.',
    'Same-handle stat/hash checks require a stable source and cannot rule out every transient concurrent change.',
  ],
};
let fd, initialStat, size, failure, closed = false;
const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
function fingerprint(label) {
  const hash = createHash('sha256'); let position = 0;
  while (position < size) {
    const count = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, size-position), position);
    assert.ok(count > 0, 'Unexpected EOF during fingerprint');
    hash.update(hashBuffer.subarray(0, count)); position += count;
  }
  const pass = {label, bytes:position, chunk_bytes:hashBuffer.length, sha256:hash.digest('hex')};
  result.fingerprint_passes.push(pass); return pass.sha256;
}
function read(offset, length, kind) {
  assert.ok(fits(offset, length, size), 'Metadata exceeds source');
  assert.ok(length <= limits.metadata_single_read_bytes, 'Single metadata read exceeds 4096 bytes');
  assert.ok(length <= limits.metadata_total_bytes-result.metadata_bytes, 'Metadata exceeds 8 KiB');
  assert.ok(result.metadata_read_operations < limits.metadata_read_operations, 'Metadata operation limit');
  const record = {kind, file_offset:offset, requested_bytes:length, read_bytes:0, operations:0, raw_hex:''};
  result.metadata_reads.push(record); result.metadata_bytes += length;
  const buffer = Buffer.alloc(length);
  while (record.read_bytes < length) {
    assert.ok(result.metadata_read_operations < limits.metadata_read_operations, 'Metadata operation limit');
    result.metadata_read_operations++; record.operations++;
    const count = fs.readSync(fd, buffer, record.read_bytes, length-record.read_bytes, offset+record.read_bytes);
    assert.ok(count > 0, 'Unexpected metadata EOF');
    record.read_bytes += count; record.raw_hex = buffer.subarray(0, record.read_bytes).toString('hex');
  }
  return buffer;
}
try {
  fd = fs.openSync(target, fs.constants.O_RDONLY);
  initialStat = fs.fstatSync(fd); size = initialStat.size;
  assert.ok(initialStat.isFile() && size === expectedSize, 'Unexpected fixed Game file/size');
  result.stat_before = {size, dev:initialStat.dev, ino:initialStat.ino, mtime_ms:initialStat.mtimeMs, ctime_ms:initialStat.ctimeMs};
  result.sha256_before = fingerprint('before');
  assert.equal(result.sha256_before, expectedHash, 'Game hash mismatch; stop before metadata');
  const dos = read(0, 64, 'dos-header');
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60);
  assert.ok(pe >= 64 && pe+24 <= limits.header_end, 'Invalid bounded PE offset');
  const coff = read(pe, 24, 'signature-and-coff-header');
  assert.equal(coff.readUInt32LE(0), 0x4550); assert.equal(coff.readUInt16LE(4), 0x8664);
  const sectionCount = coff.readUInt16LE(6), optionalSize = coff.readUInt16LE(20);
  assert.ok(sectionCount > 0 && sectionCount <= limits.sections, 'Section count budget');
  assert.ok(optionalSize >= 144 && optionalSize <= 4096, 'Invalid optional header size');
  const headerEnd = pe+24+optionalSize+sectionCount*40;
  assert.ok(headerEnd <= limits.header_end, 'Header extent budget');
  const optional = read(pe+24, 144, 'optional-header-prefix');
  assert.equal(optional.readUInt16LE(0), 0x20b); assert.equal(optional.readBigUInt64LE(24), imageBase);
  assert.ok(optional.readUInt32LE(108) >= 4, 'Missing exception directory');
  const imageSize = optional.readUInt32LE(56), headerSize = optional.readUInt32LE(60);
  assert.ok(headerSize >= headerEnd && headerSize <= limits.header_end && headerSize <= size);
  const table = read(pe+24+optionalSize, sectionCount*40, 'section-headers'), sections = [];
  for (let i=0; i<sectionCount; i++) {
    const at=i*40, section={
      name:table.subarray(at,at+8).toString('ascii').replace(/\0.*$/, ''),
      rva:table.readUInt32LE(at+12), raw_size:table.readUInt32LE(at+16),
      raw_offset:table.readUInt32LE(at+20), characteristics:table.readUInt32LE(at+36),
    };
    if (section.raw_size) assert.ok(fits(section.raw_offset,section.raw_size,size), 'Section outside file');
    sections.push(section);
  }
  function mapping(rva,length) {
    assert.ok(fits(rva,length,imageSize), 'RVA outside image');
    const matches=sections.filter(s => rva>=s.rva && fits(rva-s.rva,length,s.raw_size));
    assert.equal(matches.length,1,'Expected unique file-backed section');
    const section=matches[0], offset=section.raw_offset+rva-section.rva;
    assert.ok(fits(offset,length,size), 'Mapped range outside file'); return {section,offset};
  }
  const exceptionRva=optional.readUInt32LE(136), exceptionSize=optional.readUInt32LE(140);
  assert.ok(exceptionRva>0 && exceptionSize>0 && exceptionSize%12===0 && exceptionSize<=limits.exception_directory_bytes);
  const exception=mapping(exceptionRva,exceptionSize), entryCount=exceptionSize/12, cache=new Map();
  assert.equal(exception.section.name,'.pdata'); assert.equal(exception.section.characteristics & 0x20000000,0);
  result.pe={pe_file_offset:pe,image_base:hex(imageBase),image_size:imageSize,sections,
    exception_rva:hex(exceptionRva),exception_file_offset:exception.offset,exception_bytes:exceptionSize,entry_count:entryCount};
  function entryAt(index) {
    assert.ok(Number.isInteger(index) && index>=0 && index<entryCount);
    if (cache.has(index)) return cache.get(index);
    const bytes=read(exception.offset+index*12,12,'selected-runtime-function');
    const entry={index,begin:bytes.readUInt32LE(0),end:bytes.readUInt32LE(4),unwind:bytes.readUInt32LE(8),raw_hex:bytes.toString('hex')};
    assert.ok(entry.begin<entry.end && entry.end<=imageSize,'Invalid runtime-function range');
    for (const [otherIndex,other] of cache) assert.ok(otherIndex<index ? other.begin<=entry.begin : other.begin>=entry.begin,'Unsorted encountered .pdata entries');
    cache.set(index,entry); return entry;
  }
  const describe=entry => entry ? {index:entry.index,entry_rva:hex(exceptionRva+entry.index*12),
    file_offset:exception.offset+entry.index*12,begin_rva:hex(entry.begin),end_rva_exclusive:hex(entry.end),
    unwind_rva:hex(entry.unwind),raw_hex:entry.raw_hex} : null;
  for (const choice of selected) {
    const assessment={role:choice.role,requested_rva:hex(choice.rva),steps:0,probes:[],status:'searching'};
    result.assessments.push(assessment);
    let lo=0,hi=entryCount;
    while (lo<hi) {
      assert.ok(assessment.steps<limits.binary_search_steps_per_target,'Search exceeds 32 steps');
      assessment.steps++;
      const mid=lo+Math.floor((hi-lo)/2),entry=entryAt(mid);
      assessment.probes.push(describe(entry));
      if (entry.begin<choice.rva) lo=mid+1; else hi=mid;
    }
    assessment.lower_bound_index=lo;
    const next=lo<entryCount ? entryAt(lo) : null;
    if (next && next.begin===choice.rva) {
      const code=mapping(next.begin,next.end-next.begin);
      assert.ok(code.section.characteristics & 0x20000000,'Exact range must map to executable section');
      assessment.status='exact-begin';
      result.exact_boundaries.push({...describe(next),role:choice.role,begin_va:hex(imageBase+BigInt(next.begin)),
        end_va_exclusive:hex(imageBase+BigInt(next.end)),range_bytes:next.end-next.begin,
        code_file_offset:code.offset,section:code.section.name,code_bytes_read:0});
    } else {
      // Only one necessary predecessor; no automatic neighboring code selection.
      const previous=lo>0 ? entryAt(lo-1) : null;
      assessment.previous=describe(previous); assessment.next=describe(next);
      assessment.status=previous && previous.begin<=choice.rva && choice.rva<previous.end
        ? 'no-exact-begin-containing-entry' : 'no-exact-begin-gap';
      assessment.selected_code_range=null;
    }
  }
  result.distinct_exception_entries_read=cache.size;
  result.status=result.exact_boundaries.length===selected.length ? 'all-fixed-exact-boundaries-assessed' : 'fixed-boundaries-assessed-with-missing-exact';
} catch (error) {
  failure=error; result.status='failed'; result.failure=String(error);
} finally {
  if (fd!==undefined) {
    try {
      if (size===expectedSize) {
        result.sha256_after=fingerprint('after');
        const stat=fs.fstatSync(fd);
        result.stat_after={size:stat.size,dev:stat.dev,ino:stat.ino,mtime_ms:stat.mtimeMs,ctime_ms:stat.ctimeMs};
        result.source_stat_stable=stat.isFile() && stat.size===initialStat.size && stat.dev===initialStat.dev &&
          stat.ino===initialStat.ino && stat.mtimeMs===initialStat.mtimeMs && stat.ctimeMs===initialStat.ctimeMs;
        result.source_hash_preserved=result.sha256_before===expectedHash && result.sha256_after===expectedHash;
        assert.ok(result.source_stat_stable && result.source_hash_preserved,'Game stat/hash changed');
      }
    } catch (error) { failure??=error; result.status='failed'; result.final_hash_failure=String(error); }
    finally {
      try {fs.closeSync(fd);closed=true;}
      catch (error) {failure??=error;result.status='failed';result.close_failure=String(error);}
    }
  } else closed=true;
}
assert.ok(closed,'Source close not confirmed; no report directory will be created');
result.source_handle_closed=true; result.finished_utc=new Date().toISOString();
normalAncestors(path.dirname(outputDirectory),true);
assertAbsent(outputDirectory);
fs.mkdirSync(outputDirectory); normalAncestors(outputDirectory,true);
fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({status:result.status,output,metadata_bytes:result.metadata_bytes,
  metadata_read_operations:result.metadata_read_operations,source_hash_preserved:result.source_hash_preserved,
  exact_boundaries:result.exact_boundaries,assessments:result.assessments.map(a=>({role:a.role,status:a.status,lower_bound_index:a.lower_bound_index}))}));
if (failure) process.exitCode=1;
