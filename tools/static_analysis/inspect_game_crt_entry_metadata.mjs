// Prepared, fixed Game-only PE metadata lookup; execution is a separate action.
// Reads AddressOfEntryPoint and its .pdata records, never code or import tables.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(script), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expectedHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const expectedBytes = 11385856, imageBase = 0x180000000n;
const peProofPath = 'docs/object-lifecycle/offline-questitem-boundaries/boundary.json';
const peProofHash = 'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05';
const entryFieldOffset = 296 + 16;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-game-crt-entry-metadata');
const output = path.join(outputDirectory, 'metadata.json');
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536,
  metadata_read_bytes: 1024,
  entry_field_bytes: 4,
  exception_metadata_read_bytes: 512,
  metadata_single_read_bytes: 12,
  binary_search_steps: 32,
  saved_evidence_bytes: 2 * 1024 * 1024,
  report_bytes: 256 * 1024,
});
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = value => `0x${value.toString(16)}`;
const within = (offset, length, size) =>
  Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 &&
  Number.isSafeInteger(size) && offset <= size && length <= size - offset;

const proofFile = path.join(root, peProofPath);
assert(fs.statSync(proofFile).size <= limits.saved_evidence_bytes, 'Saved evidence size limit');
const proofBytes = fs.readFileSync(proofFile);
assert.equal(hash(proofBytes), peProofHash, 'Pinned PE evidence changed');
const proof = JSON.parse(proofBytes);
assert.equal(proof.status, 'questitem-ctor-read-boundaries-and-classinfo-assessed');
assert.equal(proof.target, target);
assert.equal(proof.source_bytes, expectedBytes);
assert.equal(proof.sha256_before, expectedHash);
assert.equal(proof.sha256_after, expectedHash);
assert.equal(proof.source_hash_preserved, true);
assert.equal(proof.pe.image_base, hex(imageBase));
assert.equal(proof.pe.pe_header_file_offset, 272);
const optionalReads = proof.metadata_reads.filter(item => item.kind === 'optional-header-prefix');
assert.equal(optionalReads.length, 1);
assert.equal(optionalReads[0].file_offset, 296);
assert.equal(optionalReads[0].read_bytes, 144);
assert(within(entryFieldOffset - 296, 4, optionalReads[0].read_bytes));

const sections = proof.pe.sections;
assert.equal(sections.length, 9);
for (const section of sections) {
  assert(Number.isInteger(section.rva) && section.rva >= 0);
  if (section.raw_size) assert(within(section.raw_offset, section.raw_size, expectedBytes));
}
function mapping(rva, length) {
  assert(within(rva, length, 0x100000000), 'RVA range outside uint32 image domain');
  const matches = sections.filter(section => rva >= section.rva &&
    within(rva - section.rva, length, section.raw_size));
  assert.equal(matches.length, 1, 'Require unique saved file-backed section');
  const section = matches[0], fileOffset = section.raw_offset + rva - section.rva;
  assert(within(fileOffset, length, expectedBytes), 'Saved mapping exceeds fixed Game file');
  return {section, fileOffset};
}
const exceptionRva = Number(proof.pe.exception_directory_rva);
const exceptionSize = proof.pe.exception_directory_size, entryCount = proof.pe.entry_count;
assert.equal(exceptionRva, 0xa6a000);
assert.equal(exceptionSize, 267516);
assert.equal(entryCount, 22293);
assert.equal(exceptionSize, entryCount * 12);
const exception = mapping(exceptionRva, exceptionSize);
assert.equal(exception.section.name, '.pdata');
assert.equal(exception.section.characteristics & 0x20000000, 0);
assert.equal(exception.fileOffset, 10896384);

for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  const stat = fs.lstatSync(directory);
  assert(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real directory');
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(),
    'Unexpected output ancestor redirection');
  if (directory === path.dirname(directory)) break;
}
assert(!fs.existsSync(outputDirectory), 'Preserve existing evidence directory');

const result = {
  schema: 'grim-dynasty-fixed-game-crt-entry-metadata-v1',
  started_utc: new Date().toISOString(),
  status: 'started',
  operation: 'Read only fixed Game AddressOfEntryPoint field and binary-search its exception records; no CRT identity or initializer-table bounds are inferred.',
  target, expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: {path: peProofPath, sha256: peProofHash},
  limits, target_open_mode: 'O_RDONLY',
  target_loaded_or_executed: false, target_copy_created: false,
  backend_started: false, subprocesses: 0, installations: false, permission_escalation: false,
  code_bytes_read: 0, code_bytes_interpreted: 0, unwind_body_bytes_read: 0,
  import_table_bytes_read: 0, initializer_table_bytes_read: 0,
  whole_exception_table_scanned: false,
  pe_mapping_reused_from_pinned_evidence: true,
  metadata_bytes: 0, exception_metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [],
  entry_point: null, boundary: null,
  pe: {
    pe_header_file_offset: 272, optional_header_file_offset: 296,
    address_of_entry_point_field_file_offset: entryFieldOffset,
    image_base: hex(imageBase), exception_directory_rva: hex(exceptionRva),
    exception_directory_size: exceptionSize,
    exception_directory_file_offset: exception.fileOffset, entry_count: entryCount, sections,
  },
  limitations: [
    'Semantic reads are exactly four entry-point-field bytes and at most 512 bytes of cached .pdata probes and immediate neighboring records; no target code, imports or initializer table is read.',
    'The optional-header and section mappings are reused from hash-pinned evidence after matching the full original hash. No other new header fields are read.',
    'AddressOfEntryPoint names the PE entry address only; this does not identify a CRT function, DLL initialization behavior or static initializer-array bounds.',
    'Binary search assumes .pdata ordered by BeginAddress. Only observed entries are checked for ordering; the directory is not scanned.',
    'No exact BeginAddress is a no-exact-unwind observation. An uncovered address is only a leaf candidate; neighboring records are reported but never selected or read as code.',
    'An exact RUNTIME_FUNCTION range may be only a fragment, not the full logical function or reachable control flow.',
    'Whole-file 64KiB fingerprint passes traverse bytes without interpreting them. O_RDONLY plus same-handle stat/hash checks are not an atomic snapshot or Windows FileShare.Read exclusivity guarantee.',
  ],
};
const cache = new Map(), fingerprintBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
let fd, initialStat, admittedSize = false, failure;

function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes, 'File size changed before hashing');
  const digest = createHash('sha256');
  let at = 0;
  while (at < expectedBytes) {
    const count = fs.readSync(fd, fingerprintBuffer, 0,
      Math.min(fingerprintBuffer.length, expectedBytes - at), at);
    assert(count > 0, 'Unexpected EOF during fingerprint');
    digest.update(fingerprintBuffer.subarray(0, count));
    at += count;
  }
  const sha256 = digest.digest('hex');
  result.fingerprint_passes.push({label, bytes: at, chunk_bytes: fingerprintBuffer.length, sha256});
  return sha256;
}
function readMetadata(fileOffset, length, kind, entryIndex = null) {
  assert(within(fileOffset, length, expectedBytes), 'Metadata read outside fixed Game file');
  assert(length <= limits.metadata_single_read_bytes, 'Single metadata read limit');
  assert(result.metadata_bytes + length <= limits.metadata_read_bytes, '1KiB metadata limit');
  if (entryIndex === null) {
    assert.equal(kind, 'address-of-entry-point');
    assert.equal(fileOffset, entryFieldOffset);
    assert.equal(length, limits.entry_field_bytes);
    assert.equal(result.metadata_reads.length, 0, 'Only one entry-point-field read is allowed');
  } else {
    assert(Number.isInteger(entryIndex) && entryIndex >= 0 && entryIndex < entryCount);
    assert.equal(fileOffset, exception.fileOffset + entryIndex * 12);
    assert.equal(length, 12);
    assert(within(entryIndex * 12, length, exceptionSize), 'Read outside exception directory');
    assert(result.exception_metadata_bytes + length <= limits.exception_metadata_read_bytes,
      '512-byte exception metadata limit');
    result.exception_metadata_bytes += length;
  }
  const bytes = Buffer.alloc(length);
  const operation = {kind, entry_index: entryIndex, file_offset: fileOffset,
    requested_bytes: length, read_bytes: 0};
  result.metadata_reads.push(operation);
  result.metadata_bytes += length;
  while (operation.read_bytes < length) {
    const count = fs.readSync(fd, bytes, operation.read_bytes, length - operation.read_bytes,
      fileOffset + operation.read_bytes);
    assert(count > 0, 'Unexpected EOF during metadata read');
    operation.read_bytes += count;
  }
  operation.raw_bytes = bytes.toString('hex');
  return bytes;
}
function entryAt(index, purpose) {
  assert(Number.isInteger(index) && index >= 0 && index < entryCount, 'Invalid exception index');
  if (cache.has(index)) return cache.get(index);
  const fileOffset = exception.fileOffset + index * 12;
  const bytes = readMetadata(fileOffset, 12, purpose, index);
  const entry = {
    index, begin: bytes.readUInt32LE(0), end: bytes.readUInt32LE(4), unwind: bytes.readUInt32LE(8),
    exception_entry_rva: hex(exceptionRva + index * 12), file_offset: fileOffset,
    raw_bytes: bytes.toString('hex'),
  };
  assert(entry.begin < entry.end, 'Invalid observed runtime-function range');
  for (const [otherIndex, other] of cache)
    assert(otherIndex < index ? other.begin <= entry.begin : other.begin >= entry.begin,
      'Observed exception entries violate sorted-table contract');
  cache.set(index, entry);
  return entry;
}
function describe(entry) {
  return entry === null ? null : {
    index: entry.index, exception_entry_rva: entry.exception_entry_rva,
    file_offset: entry.file_offset, raw_bytes: entry.raw_bytes,
    begin_rva: hex(entry.begin), end_rva_exclusive: hex(entry.end),
    begin_va: hex(imageBase + BigInt(entry.begin)),
    end_va_exclusive: hex(imageBase + BigInt(entry.end)),
    unwind_info_rva: hex(entry.unwind), range_bytes: entry.end - entry.begin,
    code_bytes_read: 0, unwind_body_bytes_read: 0,
  };
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(),
    'Unexpected fixed target redirection');
  fd = fs.openSync(target, fs.constants.O_RDONLY);
  initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile(), 'Target must be a regular file');
  assert.equal(initialStat.size, expectedBytes, 'Fixed Game size mismatch');
  admittedSize = true;
  result.sha256_before = fingerprint('before');
  assert.equal(result.sha256_before, expectedHash, 'Game hash mismatch: stop before metadata');
  const bytes = readMetadata(entryFieldOffset, 4, 'address-of-entry-point');
  const entryRva = bytes.readUInt32LE(0);
  result.entry_point = {
    field_file_offset: entryFieldOffset, raw_bytes: bytes.toString('hex'),
    rva: hex(entryRva), preferred_va: entryRva === 0 ? null : hex(imageBase + BigInt(entryRva)),
    code_bytes_read: 0, crt_identity: 'not established',
  };
  if (entryRva === 0) {
    result.boundary = {status: 'no-pe-entry-point', selected_neighbor: false, code_bytes_read: 0};
  } else {
    const entryMapping = mapping(entryRva, 1);
    assert(entryMapping.section.characteristics & 0x20000000, 'Entry address must map to executable section');
    result.entry_point.section = entryMapping.section.name;
    result.entry_point.file_offset = entryMapping.fileOffset;
    const assessment = {
      requested_rva: hex(entryRva), status: 'started', binary_search_steps: 0, probes: [],
      exact_entry: null, previous_entry: null, next_entry: null,
      selected_neighbor: false, code_bytes_read: 0,
      logical_function_completeness: 'not established',
    };
    result.boundary = assessment;
    let lo = 0, hi = entryCount;
    while (lo < hi) {
      assert(assessment.binary_search_steps < limits.binary_search_steps, 'Binary search step limit');
      const mid = lo + Math.floor((hi - lo) / 2);
      const entry = entryAt(mid, 'binary-search-runtime-function');
      assessment.probes.push({index: mid, begin_rva: hex(entry.begin)});
      assessment.binary_search_steps++;
      if (entry.begin < entryRva) lo = mid + 1;
      else hi = mid;
    }
    assessment.lower_bound_index = lo;
    const previous = lo > 0 ? entryAt(lo - 1, 'adjacent-runtime-function') : null;
    const current = lo < entryCount ? entryAt(lo, 'lower-bound-runtime-function') : null;
    assert(previous === null || previous.begin < entryRva, 'Lower-bound predecessor inconsistent');
    assert(current === null || current.begin >= entryRva, 'Lower-bound successor inconsistent');
    assessment.previous_entry = describe(previous);
    if (current !== null && current.begin === entryRva) {
      const next = lo + 1 < entryCount ? entryAt(lo + 1, 'adjacent-runtime-function') : null;
      assessment.next_entry = describe(next);
      if (next !== null && next.begin === entryRva) {
        assessment.status = 'ambiguous-exact-unwind-entries';
        assessment.matching_lower_bound_entry = describe(current);
      } else {
        const mapped = mapping(current.begin, current.end - current.begin);
        assert(mapped.section.characteristics & 0x20000000, 'Exact unwind range is not executable');
        assessment.status = 'exact-unwind-entry';
        assessment.exact_entry = describe(current);
        assessment.range_file_offset = mapped.fileOffset;
        assessment.section = mapped.section.name;
      }
      assessment.body_length_unknown = true;
    } else {
      assessment.next_entry = describe(current);
      const contained = previous !== null && entryRva < previous.end;
      assessment.previous_entry_contains_target = contained;
      assessment.leaf_candidate = !contained;
      assessment.status = contained ? 'no-exact-unwind-entry-inside-previous-range' :
        'leaf-candidate-no-exact-unwind-entry';
      assessment.body_length_unknown = true;
      assessment.no_neighbor_body_selected = true;
    }
  }
  result.status = 'game-entry-metadata-assessed';
} catch (error) {
  failure = error;
  result.status = 'failed';
  result.failure = String(error);
  if (result.boundary?.status === 'started') result.boundary.status = 'failed';
} finally {
  result.distinct_exception_entries_read = cache.size;
  result.observed_entries = [...cache.values()].sort((a, b) => a.index - b.index).map(describe);
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        result.sha256_after = fingerprint('after');
        const after = fs.fstatSync(fd);
        result.source_stat_stable = after.isFile() && after.size === initialStat.size &&
          after.dev === initialStat.dev && after.ino === initialStat.ino &&
          after.mtimeMs === initialStat.mtimeMs && after.ctimeMs === initialStat.ctimeMs;
        result.source_hash_preserved = result.sha256_before === expectedHash && result.sha256_after === expectedHash;
        assert(result.source_stat_stable && result.source_hash_preserved, 'Target hash or stat changed');
      }
    } catch (error) {
      result.final_fingerprint_failure = String(error);
      failure ??= error;
      result.status = 'failed';
    } finally {
      try { fs.closeSync(fd); }
      catch (error) { result.close_failure = String(error); failure ??= error; result.status = 'failed'; }
    }
  }
  result.finished_utc = new Date().toISOString();
  result.controller_resource_usage = process.resourceUsage();
  const encoded = `${JSON.stringify(result, null, 2)}\n`;
  assert(Buffer.byteLength(encoded) <= limits.report_bytes, 'Report size limit');
  fs.mkdirSync(outputDirectory);
  fs.writeFileSync(output, encoded, {flag: 'wx'});
}
console.log(JSON.stringify({
  status: result.status, output, metadata_bytes: result.metadata_bytes,
  exception_metadata_bytes: result.exception_metadata_bytes,
  source_hash_preserved: result.source_hash_preserved,
  entry_point: result.entry_point, boundary_status: result.boundary?.status,
}));
if (failure) process.exitCode = 1;
