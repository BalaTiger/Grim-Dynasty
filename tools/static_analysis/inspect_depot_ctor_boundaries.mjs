// Fixed PE metadata-only range assessment for the two discovered constructors.
// No code/unwind-body read, disassembler, adjacent-body expansion or DLL loading.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const imageBase = 0x180000000n;
const evidencePath = 'docs/object-lifecycle/offline-depot-ctor-exports/depot-ctor-exports.json';
const evidenceHash = 'f69f67e074b6fe184b86135973be0650129f6fbf00b12162b2fa572c8e1888ce';
const targets = Object.freeze([
  {name: 'TableDepot', decorated_name: '??0TableDepot@GAME@@QEAA@XZ', rva: 0x1c4ad0},
  {name: 'ObjectManager', decorated_name: '??0ObjectManager@GAME@@AEAA@XZ', rva: 0x17dbf0},
]);
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-depot-ctor-boundaries');
const output = path.join(outputDirectory, 'boundary.json');
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536,
  source_bytes: 64 * 1024 * 1024,
  metadata_read_bytes: 32 * 1024,
  metadata_single_read_bytes: 4096,
  metadata_read_operations: 128,
  header_end: 65536,
  sections: 96,
  exception_directory_bytes: 2 * 1024 * 1024,
  binary_search_steps_per_target: 32,
  saved_export_evidence_bytes: 2 * 1024 * 1024,
});
const hex = value => `0x${value.toString(16)}`;
const within = (offset, length, size) =>
  Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 &&
  Number.isSafeInteger(size) && offset <= size && length <= size - offset;
assert.ok(!fs.existsSync(output), 'Refuse to overwrite earlier evidence');
for (const directory of [root, path.join(root, 'docs'), path.join(root, 'docs/object-lifecycle')]) {
  const stat = fs.lstatSync(directory);
  assert.ok(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real project directory');
}
if (fs.existsSync(outputDirectory)) {
  const stat = fs.lstatSync(outputDirectory);
  assert.ok(stat.isDirectory() && !stat.isSymbolicLink(), 'Output directory must not be a link');
}
const proofFile = path.join(root, evidencePath);
assert.ok(fs.statSync(proofFile).size <= limits.saved_export_evidence_bytes, 'Saved export evidence exceeds budget');
const proofBytes = fs.readFileSync(proofFile);
assert.equal(createHash('sha256').update(proofBytes).digest('hex'), evidenceHash, 'Export evidence changed');
const proof = JSON.parse(proofBytes);
assert.equal(proof.status, 'metadata-discovery-completed');
assert.equal(proof.target, target);
assert.equal(proof.sha256_before, expectedHash);
assert.equal(proof.sha256_after, expectedHash);
assert.equal(proof.source_hash_preserved, true);
for (const selected of targets) {
  const matches = proof.candidates.filter(candidate => candidate.decorated_name === selected.decorated_name);
  assert.equal(matches.length, 1, 'Expected one exact discovered constructor export');
  assert.equal(matches[0].rva, hex(selected.rva));
  assert.equal(matches[0].preferred_va, hex(imageBase + BigInt(selected.rva)));
  assert.equal(matches[0].forwarder, null);
  assert.equal(matches[0].function_bytes_read, 0);
}

const result = {
  schema: 'grim-dynasty-fixed-depot-constructor-pdata-v1',
  started_utc: new Date().toISOString(),
  status: 'started',
  target, expected_sha256: expectedHash,
  source_evidence: {path: evidencePath, sha256: evidenceHash},
  requested: targets.map(item => ({...item, rva: hex(item.rva), preferred_va: hex(imageBase + BigInt(item.rva))})),
  limits,
  target_open_mode: 'read-only',
  target_loaded_or_executed: false,
  target_copy_created: false,
  backend_started: false,
  dumpbin_started: false,
  code_bytes_interpreted: 0,
  unwind_body_bytes_read: 0,
  whole_exception_table_scanned: false,
  metadata_bytes: 0,
  metadata_reads: [],
  fingerprint_passes: [],
  boundaries: [],
  limitations: [
    'Full-file fingerprint passes traverse all target bytes without interpreting code, using one 64 KiB buffer. Semantic reads are headers, sections and selected 12-byte exception-directory entries.',
    'Binary search relies on the PE exception directory being ordered by BeginAddress. Only encountered entries are checked for ordering; no full directory scan is performed.',
    'An exact RUNTIME_FUNCTION entry establishes its half-open unwind-covered range, which may be only a fragment of a logical function. No complete constructor/control-flow claim follows from its length.',
    'Unwind RVAs and code-section mappings are reported from metadata only; their target bytes are not read. No helper or neighboring range is automatically followed.',
    'No exact entry is a metadata failure for this fixed request, not proof that the constructor does not exist.',
    'Preferred VAs are static image addresses, not runtime pointers. Constructor state, effects and initialization remain unexamined.',
    'Same-handle fingerprints and file-stat checks require a stable source; they do not prove the absence of every transient concurrent change.',
  ],
};
const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
let fd, size, initialStat, failure;
function fingerprint(label) {
  let position = 0;
  const hash = createHash('sha256');
  while (position < size) {
    const count = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, size - position), position);
    assert.ok(count > 0, 'Unexpected EOF during fingerprint');
    hash.update(hashBuffer.subarray(0, count));
    position += count;
  }
  const pass = {label, bytes: position, chunk_bytes: hashBuffer.length, sha256: hash.digest('hex')};
  result.fingerprint_passes.push(pass);
  return pass.sha256;
}
function read(offset, length, kind) {
  assert.ok(within(offset, length, size), 'Metadata read exceeds file');
  assert.ok(length <= limits.metadata_single_read_bytes, 'Single metadata read exceeds budget');
  assert.ok(length <= limits.metadata_read_bytes - result.metadata_bytes, '32 KiB metadata budget exceeded');
  assert.ok(result.metadata_reads.length < limits.metadata_read_operations, 'Too many metadata reads');
  result.metadata_bytes += length;
  const record = {kind, file_offset: offset, requested_bytes: length, read_bytes: 0};
  result.metadata_reads.push(record);
  const buffer = Buffer.alloc(length);
  while (record.read_bytes < length) {
    const count = fs.readSync(fd, buffer, record.read_bytes, length - record.read_bytes, offset + record.read_bytes);
    assert.ok(count > 0, 'Unexpected EOF during metadata read');
    record.read_bytes += count;
  }
  return buffer;
}
try {
  fd = fs.openSync(target, 'r');
  initialStat = fs.fstatSync(fd);
  size = initialStat.size;
  assert.ok(initialStat.isFile() && Number.isSafeInteger(size) && size > 0 && size <= limits.source_bytes,
    'Unexpected fixed target file');
  result.source_bytes = size;
  result.sha256_before = fingerprint('before');
  assert.equal(result.sha256_before, expectedHash, 'Target fingerprint mismatch; do not inspect metadata');
  const dos = read(0, 64, 'dos-header');
  assert.equal(dos.readUInt16LE(0), 0x5a4d, 'Not MZ');
  const pe = dos.readUInt32LE(60);
  assert.ok(pe >= 64 && pe + 24 <= limits.header_end, 'PE header outside budget');
  const coff = read(pe, 24, 'signature-and-coff-header');
  assert.equal(coff.readUInt32LE(0), 0x4550, 'Not PE');
  assert.equal(coff.readUInt16LE(4), 0x8664, 'Expected AMD64');
  const sectionCount = coff.readUInt16LE(6), optionalLength = coff.readUInt16LE(20);
  assert.ok(sectionCount > 0 && sectionCount <= limits.sections, 'Section count exceeds budget');
  assert.ok(optionalLength >= 144 && optionalLength <= 4096, 'Unexpected optional header length');
  assert.ok(pe + 24 + optionalLength + sectionCount * 40 <= limits.header_end, 'Header extent exceeds budget');
  const optional = read(pe + 24, 144, 'optional-header-prefix');
  assert.equal(optional.readUInt16LE(0), 0x20b, 'Expected PE32+');
  assert.equal(optional.readBigUInt64LE(24), imageBase, 'Unexpected image base');
  assert.ok(optional.readUInt32LE(108) >= 4, 'Missing exception data directory');
  const imageSize = optional.readUInt32LE(56), headerSize = optional.readUInt32LE(60);
  assert.ok(headerSize >= pe + 24 + optionalLength + sectionCount * 40 &&
    headerSize <= limits.header_end && headerSize <= size, 'Invalid SizeOfHeaders');
  const table = read(pe + 24 + optionalLength, sectionCount * 40, 'section-headers');
  const sections = [];
  for (let i = 0; i < sectionCount; i++) {
    const at = i * 40;
    const section = {
      name: table.subarray(at, at + 8).toString('ascii').replace(/\0.*$/, ''),
      rva: table.readUInt32LE(at + 12), raw_size: table.readUInt32LE(at + 16),
      raw_offset: table.readUInt32LE(at + 20), characteristics: table.readUInt32LE(at + 36),
    };
    if (section.raw_size) assert.ok(within(section.raw_offset, section.raw_size, size), 'Section exceeds file');
    sections.push(section);
  }
  function mapping(rva, length) {
    assert.ok(within(rva, length, imageSize), 'RVA exceeds image');
    const matches = sections.filter(section =>
      rva >= section.rva && within(rva - section.rva, length, section.raw_size));
    assert.equal(matches.length, 1, 'RVA must have one file-backed section');
    const section = matches[0], offset = section.raw_offset + rva - section.rva;
    assert.ok(within(offset, length, size), 'Mapped range exceeds file');
    return {section, offset};
  }
  const exceptionRva = optional.readUInt32LE(136), exceptionSize = optional.readUInt32LE(140);
  assert.ok(exceptionRva > 0 && exceptionSize > 0 && exceptionSize % 12 === 0 &&
    exceptionSize <= limits.exception_directory_bytes, 'Invalid exception directory extent');
  const exception = mapping(exceptionRva, exceptionSize);
  assert.equal(exception.section.name, '.pdata', 'Expected .pdata exception directory');
  assert.equal(exception.section.characteristics & 0x20000000, 0, 'Refuse executable exception metadata');
  const entryCount = exceptionSize / 12, cache = new Map();
  result.pe = {pe_header_file_offset: pe, image_base: hex(imageBase), sections,
    exception_directory_rva: hex(exceptionRva), exception_directory_size: exceptionSize, entry_count: entryCount};
  function entryAt(index) {
    assert.ok(Number.isInteger(index) && index >= 0 && index < entryCount, 'Exception index outside table');
    if (cache.has(index)) return cache.get(index);
    const entry = read(exception.offset + index * 12, 12, 'binary-search-runtime-function');
    const parsed = {index, begin: entry.readUInt32LE(0), end: entry.readUInt32LE(4), unwind: entry.readUInt32LE(8)};
    assert.ok(parsed.begin < parsed.end && parsed.end <= imageSize, 'Invalid runtime-function range');
    for (const [otherIndex, other] of cache) {
      assert.ok(otherIndex < index ? other.begin <= parsed.begin : other.begin >= parsed.begin,
        'Encountered exception entries violate sorted-table contract');
    }
    cache.set(index, parsed);
    return parsed;
  }
  for (const selected of targets) {
    let lo = 0, hi = entryCount, steps = 0;
    const probes = [];
    while (lo < hi) {
      assert.ok(steps++ < limits.binary_search_steps_per_target, 'Binary search step limit exceeded');
      const mid = lo + Math.floor((hi - lo) / 2), entry = entryAt(mid);
      probes.push({index: mid, begin_rva: hex(entry.begin), end_rva_exclusive: hex(entry.end)});
      if (entry.begin < selected.rva) lo = mid + 1;
      else hi = mid;
    }
    assert.ok(lo < entryCount, `No exact .pdata entry for ${selected.name}`);
    const entry = entryAt(lo);
    assert.equal(entry.begin, selected.rva, `No exact .pdata entry for ${selected.name}`);
    const codeMapping = mapping(entry.begin, entry.end - entry.begin);
    assert.ok(codeMapping.section.characteristics & 0x20000000, 'Constructor range is not in an executable section');
    result.boundaries.push({
      name: selected.name, decorated_name: selected.decorated_name,
      requested_rva: hex(selected.rva), exception_entry_index: entry.index,
      exception_entry_rva: hex(exceptionRva + entry.index * 12),
      begin_rva: hex(entry.begin), end_rva_exclusive: hex(entry.end),
      begin_va: hex(imageBase + BigInt(entry.begin)), end_va_exclusive: hex(imageBase + BigInt(entry.end)),
      range_bytes: entry.end - entry.begin, unwind_info_rva: hex(entry.unwind),
      file_offset: hex(codeMapping.offset), section: codeMapping.section.name,
      code_bytes_read: 0, logical_function_completeness: 'not established by .pdata',
      binary_search_steps: steps, probes,
    });
  }
  result.distinct_exception_entries_read = cache.size;
  result.total_selected_range_bytes = result.boundaries.reduce((sum, boundary) => sum + boundary.range_bytes, 0);
  result.status = 'two-exact-runtime-function-ranges-assessed';
} catch (error) {
  failure = error;
  result.status = 'failed';
  result.failure = String(error);
} finally {
  if (fd !== undefined) {
    try {
      if (Number.isSafeInteger(size) && size > 0 && size <= limits.source_bytes) {
        result.sha256_after = fingerprint('after');
        const stat = fs.fstatSync(fd);
        result.source_stat_stable = stat.isFile() && stat.size === initialStat.size &&
          stat.dev === initialStat.dev && stat.ino === initialStat.ino &&
          stat.mtimeMs === initialStat.mtimeMs && stat.ctimeMs === initialStat.ctimeMs;
        result.source_hash_preserved = result.sha256_before === expectedHash && result.sha256_after === expectedHash;
        assert.ok(result.source_stat_stable && result.source_hash_preserved, 'Target fingerprint or stat changed');
      }
    } catch (error) {
      result.final_fingerprint_failure = String(error);
      failure ??= error;
      result.status = 'failed';
    } finally {
      try { fs.closeSync(fd); }
      catch (error) {
        result.close_failure = String(error);
        failure ??= error;
        result.status = 'failed';
      }
    }
  }
  result.finished_utc = new Date().toISOString();
  if (!fs.existsSync(outputDirectory)) fs.mkdirSync(outputDirectory);
  fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
}
console.log(JSON.stringify({status: result.status, output, metadata_bytes: result.metadata_bytes,
  source_hash_preserved: result.source_hash_preserved,
  ranges: result.boundaries.map(item => ({
    name: item.name, begin: item.begin_va, end_exclusive: item.end_va_exclusive, bytes: item.range_bytes,
  }))}));
if (failure) process.exitCode = 1;
