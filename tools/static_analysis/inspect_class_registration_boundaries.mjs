// Prepared metadata-only lookup for two fixed Game GetRTTIClassInfo exports.
// Reuse pinned PE metadata. No code, unwind body, neighbor body or backend read.
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
const discoveryPath = 'docs/object-lifecycle/offline-class-registration-exports/discovery.json';
const discoveryHash = '0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58';
const peProofPath = 'docs/object-lifecycle/offline-questitem-boundaries/boundary.json';
const peProofHash = 'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05';
const requested = Object.freeze([
  {name: 'QuestItem-GetRTTIClassInfo', rva: 0x51810,
    prefix: '?GetRTTIClassInfo@QuestItem@GAME@@',
    decorated_name: '?GetRTTIClassInfo@QuestItem@GAME@@UEBAAEBVRTTI_ClassInfo@2@XZ'},
  {name: 'Item-GetRTTIClassInfo', rva: 0x29a50,
    prefix: '?GetRTTIClassInfo@Item@GAME@@',
    decorated_name: '?GetRTTIClassInfo@Item@GAME@@UEBAAEBVRTTI_ClassInfo@2@XZ'},
]);
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-class-registration-getter-boundaries');
const output = path.join(outputDirectory, 'boundaries.json');
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536, metadata_read_bytes: 1024,
  metadata_single_read_bytes: 12, metadata_read_operations: 85,
  binary_search_steps_per_target: 32, saved_evidence_bytes_per_file: 2 * 1024 * 1024,
  report_bytes: 512 * 1024,
});
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = value => `0x${value.toString(16)}`;
const within = (offset, length, size) =>
  Number.isSafeInteger(offset) && offset >= 0 && Number.isSafeInteger(length) &&
  length > 0 && Number.isSafeInteger(size) && offset <= size && length <= size - offset;
function loadProof(relativePath, expected) {
  const file = path.join(root, relativePath);
  assert(fs.statSync(file).size <= limits.saved_evidence_bytes_per_file, 'Saved evidence size limit');
  const bytes = fs.readFileSync(file);
  assert.equal(hash(bytes), expected, 'Pinned project evidence changed');
  return JSON.parse(bytes);
}
const discovery = loadProof(discoveryPath, discoveryHash);
assert.equal(discovery.status, 'fixed-class-registration-export-discovery-completed');
const gameMatches = discovery.files.filter(file => file.target === target);
assert.equal(gameMatches.length, 1, 'Require one fixed Game discovery record');
const game = gameMatches[0];
assert.equal(game.status, 'metadata-discovery-completed');
assert.equal(game.expected_file_bytes, expectedBytes);
assert.equal(game.sha256_before, expectedHash);
assert.equal(game.sha256_after, expectedHash);
assert.equal(game.source_hash_preserved, true);
assert.equal(game.preferred_image_base, hex(imageBase));
for (const selected of requested) {
  const matches = game.candidates.filter(candidate => candidate.prefix === selected.prefix &&
    candidate.decorated_name === selected.decorated_name);
  assert.equal(matches.length, 1, 'Require one exact frozen export identity');
  const candidate = matches[0];
  assert.equal(candidate.rva, hex(selected.rva));
  assert.equal(candidate.preferred_va, hex(imageBase + BigInt(selected.rva)));
  assert.equal(candidate.forwarder, null);
  assert.equal(candidate.file_backed, true);
  assert.equal(candidate.section, '.text');
  assert.equal(candidate.section_executable, true);
  assert.equal(candidate.target_payload_bytes_read, 0);
}
const proof = loadProof(peProofPath, peProofHash);
assert.equal(proof.status, 'questitem-ctor-read-boundaries-and-classinfo-assessed');
assert.equal(proof.target, target);
assert.equal(proof.source_bytes, expectedBytes);
assert.equal(proof.sha256_before, expectedHash);
assert.equal(proof.sha256_after, expectedHash);
assert.equal(proof.source_hash_preserved, true);
assert.equal(proof.pe.image_base, hex(imageBase));
const sections = proof.pe.sections;
const imageSize = game.pe.size_of_image;
assert(Number.isSafeInteger(imageSize) && imageSize > 0);
assert.equal(sections.length, game.pe.sections.length);
for (let i = 0; i < sections.length; i++) {
  const saved = sections[i], other = game.pe.sections[i];
  for (const key of ['name', 'rva', 'raw_offset', 'raw_size', 'characteristics'])
    assert.equal(saved[key], other[key], 'Pinned PE section mappings disagree');
  if (saved.raw_size) assert(within(saved.raw_offset, saved.raw_size, expectedBytes));
}
const exceptionRva = Number(proof.pe.exception_directory_rva);
const exceptionSize = proof.pe.exception_directory_size, entryCount = proof.pe.entry_count;
assert.equal(exceptionRva, 0xa6a000);
assert.equal(exceptionSize, 267516);
assert.equal(entryCount, 22293);
assert.equal(exceptionSize, entryCount * 12);
function mapping(rva, length) {
  assert(within(rva, length, imageSize), 'Saved mapping exceeds image');
  const matches = sections.filter(section => rva >= section.rva &&
    within(rva - section.rva, length, section.raw_size));
  assert.equal(matches.length, 1, 'Require unique saved file-backed section');
  const section = matches[0], fileOffset = section.raw_offset + rva - section.rva;
  assert(within(fileOffset, length, expectedBytes), 'Saved file mapping exceeds fixed file');
  return {section, fileOffset};
}
const exception = mapping(exceptionRva, exceptionSize);
assert.equal(exception.section.name, '.pdata');
assert.equal(exception.section.characteristics & 0x20000000, 0);
for (const selected of requested) {
  const mapped = mapping(selected.rva, 1);
  assert(mapped.section.characteristics & 0x20000000);
  const candidate = game.candidates.find(item => item.decorated_name === selected.decorated_name);
  assert.equal(hex(mapped.fileOffset), candidate.file_offset, 'Frozen target mappings disagree');
}
for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  const stat = fs.lstatSync(directory);
  assert(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real directory');
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(),
    'Unexpected output ancestor redirection');
  if (directory === path.dirname(directory)) break;
}
assert(!fs.existsSync(outputDirectory), 'Preserve existing evidence directory');

const result = {
  schema: 'grim-dynasty-fixed-class-registration-getter-pdata-v1',
  started_utc: new Date().toISOString(), status: 'started',
  target, expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: [
    {path: discoveryPath, sha256: discoveryHash},
    {path: peProofPath, sha256: peProofHash},
  ],
  requested: requested.map(item => ({...item, rva: hex(item.rva),
    preferred_va: hex(imageBase + BigInt(item.rva))})),
  limits, target_open_mode: 'O_RDONLY',
  target_loaded_or_executed: false, target_copy_created: false,
  backend_started: false, subprocesses: 0, installations: false, permission_escalation: false,
  code_bytes_read: 0, code_bytes_interpreted: 0, unwind_body_bytes_read: 0,
  exported_data_payload_bytes_read: 0, whole_exception_table_scanned: false,
  pe_headers_reused_from_pinned_evidence: true, new_header_read_bytes: 0,
  metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [], boundaries: [],
  pe: {image_base: hex(imageBase), size_of_image: imageSize,
    exception_directory_rva: hex(exceptionRva), exception_directory_size: exceptionSize,
    exception_directory_file_offset: exception.fileOffset, entry_count: entryCount, sections},
  limitations: [
    'Only cached binary-search probes and immediate predecessor/successor RUNTIME_FUNCTION records are semantically read; total new metadata is at most 1024 bytes.',
    'PE headers and exception-directory mapping are reused from hash-pinned project evidence after matching the full original hash. No new header, code or unwind-body reads are performed.',
    'Binary search assumes .pdata ordered by BeginAddress; only observed ordering is checked, never the whole directory.',
    'No exact BeginAddress is an explicit no-exact-unwind observation. A non-covered target is only a leaf candidate, not proof of leaf behavior or an inferred function length.',
    'If the preceding entry contains the target, that relationship is reported without selecting it as the getter body. Adjacent entries are metadata evidence only and are never followed.',
    'Even an exact entry establishes only a half-open unwind-covered fragment, not complete logical function or control-flow boundaries.',
    'Whole-file 64KiB hash passes traverse all bytes without interpreting them. Node O_RDONLY and same-handle stat/hash checks are not an atomic snapshot or FileShare.Read exclusivity guarantee.',
  ],
};
const cache = new Map(), fingerprintBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
let fd, initialStat, admittedSize = false, failure;
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes, 'File size changed before hashing');
  const digest = createHash('sha256');
  let at = 0;
  while (at < expectedBytes) {
    const count = fs.readSync(fd, fingerprintBuffer, 0, Math.min(fingerprintBuffer.length, expectedBytes - at), at);
    assert(count > 0, 'Unexpected EOF during fingerprint');
    digest.update(fingerprintBuffer.subarray(0, count));
    at += count;
  }
  const sha256 = digest.digest('hex');
  result.fingerprint_passes.push({label, bytes: at, chunk_bytes: fingerprintBuffer.length, sha256});
  return sha256;
}
function entryAt(index, purpose) {
  assert(Number.isInteger(index) && index >= 0 && index < entryCount, 'Exception index outside directory');
  if (cache.has(index)) return cache.get(index);
  assert(result.metadata_bytes + 12 <= limits.metadata_read_bytes, '1024-byte metadata limit');
  assert(result.metadata_reads.length < limits.metadata_read_operations, 'Metadata operation limit');
  const fileOffset = exception.fileOffset + index * 12;
  assert(within(fileOffset, 12, expectedBytes));
  assert(within(index * 12, 12, exceptionSize), 'Read must stay within pinned exception directory');
  const bytes = Buffer.alloc(12);
  const operation = {kind: purpose, entry_index: index, file_offset: fileOffset,
    requested_bytes: 12, read_bytes: 0};
  result.metadata_reads.push(operation);
  result.metadata_bytes += 12;
  while (operation.read_bytes < 12) {
    const count = fs.readSync(fd, bytes, operation.read_bytes, 12 - operation.read_bytes,
      fileOffset + operation.read_bytes);
    assert(count > 0, 'Unexpected EOF during .pdata probe');
    operation.read_bytes += count;
  }
  const entry = {
    index, begin: bytes.readUInt32LE(0), end: bytes.readUInt32LE(4), unwind: bytes.readUInt32LE(8),
    exception_entry_rva: hex(exceptionRva + index * 12), file_offset: fileOffset,
    raw_bytes: bytes.toString('hex'),
  };
  assert(entry.begin < entry.end && entry.end <= imageSize, 'Invalid observed runtime-function range');
  for (const [otherIndex, other] of cache)
    assert(otherIndex < index ? other.begin <= entry.begin : other.begin >= entry.begin,
      'Observed .pdata entries violate sorted-table contract');
  cache.set(index, entry);
  return entry;
}
function describe(entry) {
  return entry === null ? null : {
    index: entry.index, exception_entry_rva: entry.exception_entry_rva,
    file_offset: entry.file_offset, raw_bytes: entry.raw_bytes,
    begin_rva: hex(entry.begin), end_rva_exclusive: hex(entry.end),
    begin_va: hex(imageBase + BigInt(entry.begin)), end_va_exclusive: hex(imageBase + BigInt(entry.end)),
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
  assert.equal(result.sha256_before, expectedHash, 'Fixed Game hash mismatch: stop before metadata');
  for (const selected of requested) {
    const assessment = {name: selected.name, decorated_name: selected.decorated_name,
      requested_rva: hex(selected.rva), preferred_va: hex(imageBase + BigInt(selected.rva)),
      status: 'started', binary_search_steps: 0, probes: [],
      exact_entry: null, previous_entry: null, next_entry: null,
      selected_neighbor: false, code_bytes_read: 0, logical_function_completeness: 'not established'};
    result.boundaries.push(assessment);
    let lo = 0, hi = entryCount;
    while (lo < hi) {
      assert(assessment.binary_search_steps < limits.binary_search_steps_per_target, 'Binary search step limit');
      const mid = lo + Math.floor((hi - lo) / 2), entry = entryAt(mid, 'binary-search-runtime-function');
      assessment.probes.push({index: mid, begin_rva: hex(entry.begin)});
      assessment.binary_search_steps++;
      if (entry.begin < selected.rva) lo = mid + 1;
      else hi = mid;
    }
    assessment.lower_bound_index = lo;
    const previous = lo > 0 ? entryAt(lo - 1, 'adjacent-runtime-function') : null;
    const current = lo < entryCount ? entryAt(lo, 'lower-bound-runtime-function') : null;
    assert(previous === null || previous.begin < selected.rva, 'Lower-bound predecessor inconsistent');
    assert(current === null || current.begin >= selected.rva, 'Lower-bound successor inconsistent');
    assessment.previous_entry = describe(previous);
    if (current !== null && current.begin === selected.rva) {
      const next = lo + 1 < entryCount ? entryAt(lo + 1, 'adjacent-runtime-function') : null;
      assessment.next_entry = describe(next);
      if (next !== null && next.begin === selected.rva) {
        assessment.status = 'ambiguous-exact-unwind-entries';
        assessment.matching_lower_bound_entry = describe(current);
        assessment.body_length_unknown = true;
      } else {
        const mapped = mapping(current.begin, current.end - current.begin);
        assert(mapped.section.characteristics & 0x20000000, 'Exact unwind range is not executable');
        assessment.status = 'exact-unwind-entry';
        assessment.exact_entry = describe(current);
        assessment.range_file_offset = mapped.fileOffset;
        assessment.section = mapped.section.name;
        assessment.body_length_unknown = true; // A fragment does not prove the logical function end.
      }
    } else {
      assessment.next_entry = describe(current);
      const contained = previous !== null && selected.rva < previous.end;
      assessment.previous_entry_contains_target = contained;
      assessment.leaf_candidate = !contained;
      assessment.status = contained ? 'no-exact-unwind-entry-inside-previous-range' :
        'leaf-candidate-no-exact-unwind-entry';
      assessment.body_length_unknown = true;
      assessment.no_neighbor_body_selected = true;
    }
  }
  result.status = 'class-registration-getter-boundaries-assessed';
} catch (error) {
  failure = error;
  result.status = 'failed';
  result.failure = String(error);
  const active = result.boundaries.find(item => item.status === 'started');
  if (active) { active.status = 'failed'; active.failure = String(error); }
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
console.log(JSON.stringify({status: result.status, output, metadata_bytes: result.metadata_bytes,
  source_hash_preserved: result.source_hash_preserved,
  boundaries: result.boundaries.map(item => ({name: item.name, status: item.status,
    exact_entry: item.exact_entry, selected_neighbor: item.selected_neighbor}))}));
if (failure) process.exitCode = 1;
