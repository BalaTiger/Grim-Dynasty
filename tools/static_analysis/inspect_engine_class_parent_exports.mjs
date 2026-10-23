// PREPARED ONLY: fixed Engine export metadata lookup; no build, load or subprocess.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedBytes = 4485632, prefix = '?classInfo@', wantedRva = 0x41cf30;
const proofPath = 'docs/object-lifecycle/offline-class-registration-exports/discovery.json';
const proofHash = '0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58';
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-engine-class-parent-exports');
const limits = Object.freeze({new_metadata_bytes: 16384, name_bytes_including_nul: 256,
  name_chunk_bytes: 32, matching_names: 64, terminator_probes: 1,
  lower_bound_steps: 2, fingerprint_chunk_bytes: 65536, report_bytes: 1024 * 1024});
const hex = n => `0x${n.toString(16)}`;
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const within = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && offset <= size && length <= size - offset;
const proofBytes = fs.readFileSync(path.join(root, proofPath));
assert.equal(hash(proofBytes), proofHash, 'Pinned export metadata changed');
const proof = JSON.parse(proofBytes.toString('utf8'));
const engines = proof.files.filter(f => f.target === target);
assert.equal(engines.length, 1, 'Expected one fixed Engine proof');
const saved = engines[0], pe = saved.pe;
assert.equal(saved.status, 'metadata-discovery-completed');
assert.equal(saved.expected_sha256, expectedHash); assert.equal(saved.expected_file_bytes, expectedBytes);
assert.equal(saved.sha256_before, expectedHash); assert.equal(saved.sha256_after, expectedHash);
assert(saved.source_stat_stable && saved.source_hash_preserved);
assert.equal(saved.preferred_image_base, '0x180000000');
assert.equal(pe.named_export_count, 6283); assert.equal(pe.function_count, 6283);
const exportRva = Number(pe.export_directory_rva), exportSize = pe.export_directory_size;
const namesRva = Number(pe.name_pointer_table_rva), ordinalsRva = Number(pe.name_ordinal_table_rva);
const addressesRva = Number(pe.address_table_rva);
assert.equal(exportRva, 0x3af700); assert.equal(exportSize, 417432);
assert.equal(namesRva, 0x3b5954); assert.equal(ordinalsRva, 0x3bbb80); assert.equal(addressesRva, 0x3af728);
const cachedNames = new Map(saved.encountered_names.map(item => [item.name_index, item]));
assert(cachedNames.get(6118).decorated_name < prefix);
assert(cachedNames.get(6119).decorated_name.startsWith(prefix));
assert.equal(cachedNames.get(6119).decorated_name, '?classInfo@Actor@GAME@@1VRTTI_ClassInfo@2@B');
assert.equal(cachedNames.get(6136).decorated_name, '?classInfo@Object@GAME@@1VRTTI_ClassInfo@2@B');
for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  const stat = fs.lstatSync(directory);
  assert(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real directory');
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(), 'Output ancestor redirected');
  if (directory === path.dirname(directory)) break;
}
// Creating the fresh directory before original reads also rejects dangling links/existing paths.
fs.mkdirSync(outputDirectory);
const report = {
  schema: 'grim-dynasty-fixed-engine-class-parent-export-metadata-v1',
  started_utc: new Date().toISOString(), status: 'started', script_sha256: hash(fs.readFileSync(script)),
  target, expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  proof: {path: proofPath, sha256: proofHash, reused_pe: pe},
  prefix, wanted_rva: hex(wantedRva), limits, open_mode: 'O_RDONLY',
  target_loaded_or_executed: false, backend_started: false, subprocesses: 0,
  compilation_or_tests: false, original_code_semantic_reads: 0, exported_target_payload_reads: 0,
  whole_export_table_scanned: false, new_metadata_bytes: 0, new_reads: [],
  fingerprint_passes: [], reused_name_indices: [], reused_candidate_indices: [],
  search: {lo_inclusive: 6118, hi_exclusive: 6120, reused_less_than_prefix_index: 6118,
    reused_greater_than_prefix_index: 6119, lower_bound_probes: [], first_index: null,
    terminator: null, matching_names: 0, stopped_at_wanted_rva: false, aliases_exhaustively_enumerated: false},
  encountered_names: [], candidates: [], wanted_exports: [],
  limitations: [
    'Before/after whole-file hashes use a 64KiB streaming buffer and traverse all file bytes only as fingerprints; the separate 16KiB budget covers new semantic export metadata reads.',
    'The pinned export-name table is assumed sorted. Reused bracketing names prove the restricted lower-bound interval under that contract; only encountered ordering is checked.',
    'At most 64 prefix matches and one subsequent name probe are inspected. A 65th match stops without reading its ordinal/address or expanding the search.',
    'The first exact EAT match to RVA 0x41cf30 stops the search immediately. Later names or aliases are not enumerated.',
    'Chunked export-name reads may include up to 31 bytes beyond their first NUL, within export metadata and within the total read budget. Every newly read byte is recorded.',
    'Preferred RVAs identify on-disk exports only. No exported classInfo contents, code, factory, callback, forwarder destination, runtime identity, inheritance or ABI behavior is inferred or executed.',
    'No name under this fixed prefix is not a proof that the address lacks other aliases or unexported users. The entire export table is not scanned.',
    'O_RDONLY is read-only but does not provide an atomic snapshot or a Node FileShare.Read exclusivity guarantee. Same-handle stats and both hashes must remain equal.',
  ],
};
const observed = new Map(), hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
let fd, initialStat, admittedSize = false, failure;
function mapping(rva, length) {
  assert(within(rva, length, pe.size_of_image));
  const sections = pe.sections.filter(s => rva >= s.rva && within(rva - s.rva, length, s.raw_size));
  assert.equal(sections.length, 1, 'Metadata must have one file-backed section');
  const section = sections[0], offset = section.raw_offset + rva - section.rva;
  assert.equal(section.characteristics & 0x20000000, 0, 'No executable-section semantic reads');
  assert(within(offset, length, expectedBytes));
  return {offset, remaining: section.raw_size - (rva - section.rva)};
}
function read(rva, length, kind) {
  assert(rva >= exportRva && within(rva - exportRva, length, exportSize), 'Only export-directory metadata');
  assert(length <= 32 && length <= limits.new_metadata_bytes - report.new_metadata_bytes, 'New metadata budget');
  const offset = mapping(rva, length).offset;
  const op = {kind, rva: hex(rva), file_offset: offset, requested_bytes: length, read_bytes: 0, raw_bytes: ''};
  report.new_reads.push(op); report.new_metadata_bytes += length;
  const bytes = Buffer.alloc(length);
  while (op.read_bytes < length) {
    const got = fs.readSync(fd, bytes, op.read_bytes, length - op.read_bytes, offset + op.read_bytes);
    assert(got > 0, 'Unexpected metadata EOF');
    op.read_bytes += got; op.raw_bytes = bytes.subarray(0, op.read_bytes).toString('hex');
  }
  return bytes;
}
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes, 'Fixed file size changed');
  const digest = createHash('sha256'); let at = 0;
  while (at < expectedBytes) {
    const got = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, expectedBytes - at), at);
    assert(got > 0, 'Unexpected fingerprint EOF');
    digest.update(hashBuffer.subarray(0, got)); at += got;
  }
  const sha256 = digest.digest('hex');
  report.fingerprint_passes.push({label, bytes: at, chunk_bytes: hashBuffer.length, sha256});
  return sha256;
}
function nameAt(index) {
  assert(Number.isInteger(index) && index >= 0 && index < pe.named_export_count);
  if (observed.has(index)) return observed.get(index);
  let item;
  if (cachedNames.has(index)) {
    item = {...cachedNames.get(index), origin: 'pinned-proof'};
    const raw = Buffer.from(item.raw_name_with_nul, 'hex');
    assert(raw.length <= limits.name_bytes_including_nul && raw.length > 0 && raw.indexOf(0) === raw.length - 1);
    assert.equal(raw.subarray(0, -1).toString('ascii'), item.decorated_name);
    report.reused_name_indices.push(index);
  } else {
    const pointer = read(namesRva + index * 4, 4, 'name-pointer'), rva = pointer.readUInt32LE(0);
    const pieces = []; let used = 0, found = false;
    while (used < limits.name_bytes_including_nul) {
      const count = Math.min(32, limits.name_bytes_including_nul - used,
        mapping(rva + used, 1).remaining, exportRva + exportSize - (rva + used));
      assert(count > 0, 'Name leaves export metadata');
      const bytes = read(rva + used, count, 'export-name-chunk'), nul = bytes.indexOf(0);
      const part = nul < 0 ? bytes : bytes.subarray(0, nul);
      for (const b of part) assert(b >= 0x20 && b < 0x7f, 'Export name must be ASCII');
      pieces.push(part); used += bytes.length;
      if (nul >= 0) { found = true; break; }
    }
    assert(found, 'Name exceeds 256 bytes including NUL');
    const name = Buffer.concat(pieces);
    item = {name_index: index, name_rva: hex(rva), decorated_name: name.toString('ascii'),
      raw_name_with_nul: Buffer.concat([name, Buffer.from([0])]).toString('hex'),
      raw_name_pointer: pointer.toString('hex'), origin: 'new-read'};
  }
  for (const [otherIndex, other] of observed)
    assert(otherIndex < index ? other.decorated_name <= item.decorated_name :
      other.decorated_name >= item.decorated_name, 'Encountered name order inconsistent');
  observed.set(index, item); return item;
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(), 'Fixed target redirected');
  fd = fs.openSync(target, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile()); assert.equal(initialStat.size, expectedBytes); admittedSize = true;
  report.sha256_before = fingerprint('before'); assert.equal(report.sha256_before, expectedHash, 'No semantic reads after hash mismatch');
  let lo = 6118, hi = 6120;
  while (lo < hi) {
    assert(report.search.lower_bound_probes.length < limits.lower_bound_steps);
    const mid = lo + Math.floor((hi - lo) / 2), item = nameAt(mid), less = item.decorated_name < prefix;
    report.search.lower_bound_probes.push({index: mid, less_than_prefix: less});
    if (less) lo = mid + 1; else hi = mid;
  }
  report.search.first_index = lo;
  for (let index = lo; index < pe.named_export_count; index++) {
    const item = nameAt(index);
    if (!item.decorated_name.startsWith(prefix)) { report.search.terminator = item; break; }
    assert(report.candidates.length < limits.matching_names, '65th matching name: stop without expansion');
    let candidate;
    const existing = saved.candidates.find(c => c.name_index === index);
    if (existing) {
      assert([6119, 6136].includes(index), 'Only fixed Actor/Object EAT records reused');
      assert.equal(existing.decorated_name, item.decorated_name);
      candidate = {...existing, prefix, origin: 'pinned-proof'}; report.reused_candidate_indices.push(index);
    } else {
      const ordinalBytes = read(ordinalsRva + index * 2, 2, 'name-ordinal');
      const ordinalIndex = ordinalBytes.readUInt16LE(0); assert(ordinalIndex < pe.function_count);
      const addressBytes = read(addressesRva + ordinalIndex * 4, 4, 'export-address');
      const rva = addressBytes.readUInt32LE(0); assert(rva < pe.size_of_image);
      candidate = {...item, ordinal: pe.ordinal_base + ordinalIndex, export_address_table_index: ordinalIndex,
        raw_ordinal: ordinalBytes.toString('hex'), raw_export_address: addressBytes.toString('hex'),
        rva: hex(rva), preferred_va: rva ? hex(0x180000000n + BigInt(rva)) : null,
        is_forwarder: rva >= exportRva && rva - exportRva < exportSize, target_payload_bytes_read: 0};
    }
    report.candidates.push(candidate); report.search.matching_names = report.candidates.length;
    if (Number(candidate.rva) === wantedRva) {
      report.wanted_exports.push(candidate); report.search.stopped_at_wanted_rva = true; break;
    }
  }
  report.status = report.wanted_exports.length ? 'wanted-rva-named-export-found' : 'wanted-rva-not-found-under-fixed-prefix';
} catch (error) {
  failure = error; report.status = 'failed'; report.failure = String(error);
} finally {
  report.encountered_names = [...observed.values()].sort((a, b) => a.name_index - b.name_index);
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        report.sha256_after = fingerprint('after'); const after = fs.fstatSync(fd);
        report.source_stat_stable = after.isFile() && after.size === initialStat.size &&
          after.dev === initialStat.dev && after.ino === initialStat.ino &&
          after.mtimeMs === initialStat.mtimeMs && after.ctimeMs === initialStat.ctimeMs;
        report.source_hash_preserved = report.sha256_before === expectedHash && report.sha256_after === expectedHash;
        assert(report.source_stat_stable && report.source_hash_preserved, 'Source stability failed');
      }
    } catch (error) { failure = failure || error; report.status = 'failed'; report.after_failure = String(error); }
    try { fs.closeSync(fd); report.source_closed = true; }
    catch (error) { failure = failure || error; report.status = 'failed'; report.close_failure = String(error); }
  }
  report.finished_utc = new Date().toISOString();
  const bytes = Buffer.from(JSON.stringify(report, null, 2) + '\n', 'utf8');
  assert(bytes.length <= limits.report_bytes, 'Report size budget');
  fs.writeFileSync(path.join(outputDirectory, 'discovery.json'), bytes, {flag: 'wx'});
}
console.log(JSON.stringify({status: report.status, metadata_bytes: report.new_metadata_bytes,
  prefix_matches: report.candidates.length, wanted_exports: report.wanted_exports.map(x => x.decorated_name),
  output: path.relative(root, path.join(outputDirectory, 'discovery.json')).replaceAll('\\', '/')}));
if (failure) process.exitCode = 1;
