// PREPARED ONLY. Three fixed Engine C strings; execution is separately reviewed.
// No original code/header reads, scan, subprocess, build, callback or DLL loading.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedBytes = 4485632, imageBase = 0x180000000n;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-engine-root-names');
const limits = Object.freeze({strings: 3, bytes_per_string_including_nul: 64,
  semantic_bytes: 192, single_semantic_read_bytes: 1, fingerprint_chunk_bytes: 65536,
  saved_proof_bytes_each: 2 * 1024 * 1024, report_bytes: 128 * 1024});
const sources = Object.freeze([
  {path: 'docs/object-lifecycle/offline-depot-ctor-boundaries/boundary.json',
    sha256: '646053e4565e3f08c696ae0e9a16ddb5c2e81ab22ffe046c5057fc41aa64bb15'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-initializer-slot23/5c70-instructions.json',
    sha256: 'c3d09dc72abc57f2832e9d455435648ae0aa8d5efa3a00e70d5c3395d9ace368'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-root-field-search-b/61f0-instructions.json',
    sha256: 'dda0acbcb1f0a0aa1938302edf770c0c9120c7335f31f0263238deeaf031a651'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-root-field-search-i/7270-instructions.json',
    sha256: '40c7e61899bb514ff26fcdfeab39eab801b5a2ef855fad5021dc5bd941631c2d'},
  {path: 'docs/object-lifecycle/offline-class-registration-exports/discovery.json',
    sha256: '0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'},
  {path: 'docs/object-lifecycle/offline-engine-class-parent-exports/discovery.json',
    sha256: '1d3c9eec6f8335419e3fe2daa8ce482a2ccad07600d070f1c3c8aa4fed6a1a68'},
]);
// Addresses and roles come from archived operands/exports. String values are not supplied.
const requests = Object.freeze([
  {rva: 0x309e64, classInfoRva: 0x41cfc0, writerSource: 1, operandVa: '0x180005c76',
    operandBytes: '488d0de7413000', storeVa: '0x180005c8a', storeBytes: '48890537734100'},
  {rva: 0x309ac4, classInfoRva: 0x41cf30, writerSource: 2, operandVa: '0x1800061f6',
    operandBytes: '488d0dc7383000', storeVa: '0x18000620a', storeBytes: '488905276d4100'},
  {rva: 0x309aec, classInfoRva: 0x41cf00, writerSource: 3, operandVa: '0x180007276',
    operandBytes: '488d0d6f283000', storeVa: '0x18000728a', storeBytes: '488905775c4100'},
]);
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = n => '0x' + n.toString(16);
const within = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && offset <= size && length <= size - offset;
const proofs = sources.map(source => {
  const file = path.join(root, source.path); assert(fs.statSync(file).size <= limits.saved_proof_bytes_each);
  const bytes = fs.readFileSync(file); assert.equal(hash(bytes), source.sha256, 'Pinned project proof changed');
  return JSON.parse(bytes.toString('utf8'));
});
const pe = proofs[0].pe;
assert.equal(proofs[0].target, target); assert.equal(proofs[0].source_bytes, expectedBytes);
assert.equal(proofs[0].sha256_before, expectedHash); assert.equal(proofs[0].sha256_after, expectedHash);
assert(proofs[0].source_stat_stable && proofs[0].source_hash_preserved);
assert.equal(pe.image_base, hex(imageBase));
const oldEngine = proofs[4].files.filter(x => x.target === target);
assert.equal(oldEngine.length, 1);
assert.equal(oldEngine[0].sha256_before, expectedHash); assert.equal(oldEngine[0].sha256_after, expectedHash);
assert(oldEngine[0].source_hash_preserved);
assert.equal(proofs[5].target, target);
assert.equal(proofs[5].sha256_before, expectedHash); assert.equal(proofs[5].sha256_after, expectedHash);
assert(proofs[5].source_hash_preserved);
const exports = oldEngine[0].candidates.concat(proofs[5].candidates);
function savedInstruction(request, va, bytes) {
  const matches = proofs[request.writerSource].filter(x => x.address === va);
  assert.equal(matches.length, 1); assert.equal(matches[0].bytes, bytes); return matches[0];
}
function mapByte(rva) {
  const sections = pe.sections.filter(s => rva >= s.rva && within(rva - s.rva, 1, s.raw_size));
  assert.equal(sections.length, 1, 'Require one file-backed section');
  const section = sections[0]; assert.equal(section.name, '.rdata');
  assert.equal(section.characteristics & 0x20000000, 0, 'No executable-section semantic reads');
  const offset = section.raw_offset + rva - section.rva;
  assert(within(offset, 1, expectedBytes)); return offset;
}
const planned = requests.map(request => {
  const operand = savedInstruction(request, request.operandVa, request.operandBytes);
  const store = savedInstruction(request, request.storeVa, request.storeBytes);
  assert.equal(Number(BigInt(operand.address) - imageBase) + 7 + Buffer.from(operand.bytes, 'hex').readInt32LE(3), request.rva);
  assert.equal(Number(BigInt(store.address) - imageBase) + 7 + Buffer.from(store.bytes, 'hex').readInt32LE(3), request.classInfoRva + 8);
  const candidates = exports.filter(x => Number(x.rva) === request.classInfoRva);
  assert(candidates.length > 0, 'Missing exact exported ClassInfo address');
  const names = new Set(candidates.map(x => x.decorated_name)); assert.equal(names.size, 1);
  const decorated = [...names][0], match = /^\?classInfo@([^@]+)@GAME@@/.exec(decorated);
  assert(match, 'Expected pinned ClassInfo export spelling');
  return {rva: hex(request.rva), preferred_va: hex(imageBase + BigInt(request.rva)), file_offset: mapByte(request.rva),
    classinfo_rva: hex(request.classInfoRva), name_store_rva: hex(request.classInfoRva + 8),
    export_decorated_name: decorated, export_class_label: match[1],
    writer_source: sources[request.writerSource], operand, name_store: store,
    status: 'not-read', bytes_read: 0, terminated: false, raw_hex_observed: '',
    ascii: null, printable_ascii: null, matches_export_class_label: null};
});
for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  const stat = fs.lstatSync(directory); assert(stat.isDirectory() && !stat.isSymbolicLink());
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(), 'Output ancestor redirected');
  if (directory === path.dirname(directory)) break;
}
try { fs.lstatSync(outputDirectory); throw Error('Preserve existing evidence directory'); }
catch (error) { if (error.code !== 'ENOENT') throw error; }
const report = {
  schema: 'grim-dynasty-fixed-engine-root-names-v1', status: 'started', started_utc: new Date().toISOString(),
  script_sha256: hash(fs.readFileSync(script)), target, expected_sha256: expectedHash,
  expected_file_bytes: expectedBytes, source_evidence: sources, limits,
  target_open_mode: 'O_RDONLY', target_loaded_or_executed: false, original_code_semantic_reads: 0,
  new_header_bytes_read: 0, backend_started: false, subprocesses: 0, compilation_or_tests: false,
  semantic_bytes: 0, metadata_reads: [], fingerprint_passes: [], names: planned,
  names_complete: false, role_comparisons_complete: false, insufficiencies: [],
  limitations: [
    'Only the three archived LEA operands are read, one byte per read, stopping at the first NUL; 64 bytes including NUL per string and 192 total are own coverage limits.',
    'Export labels describe address roles from pinned metadata, not prefilled string values. Printable ASCII is displayed only after a complete first-NUL string is observed; raw bytes are always retained.',
    'Name-store addresses are checked against the exported ClassInfo address plus eight. A label comparison does not prove parent linkage, complete registration, constructor behavior or runtime pointer identity.',
    'No unterminated name is extended past its 64-byte bound. Non-ASCII bytes are not decoded or case-folded; they leave the textual role comparison unknown.',
    'Whole-file before/after hashes use one 64KiB buffer and traverse all bytes without code interpretation. O_RDONLY plus same-handle fstat/hash checks do not guarantee an atomic snapshot or exclusive Windows FileShare.Read.',
  ],
};
const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes), one = Buffer.alloc(1);
let fd, initialStat, admittedSize = false, failure;
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes);
  const digest = createHash('sha256'); let at = 0;
  while (at < expectedBytes) {
    const got = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, expectedBytes - at), at);
    assert(got > 0, 'Unexpected fingerprint EOF'); digest.update(hashBuffer.subarray(0, got)); at += got;
  }
  const sha256 = digest.digest('hex');
  report.fingerprint_passes.push({label, bytes: at, chunk_bytes: hashBuffer.length, sha256}); return sha256;
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(), 'Fixed Engine path redirected');
  fd = fs.openSync(target, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile()); assert.equal(initialStat.size, expectedBytes); admittedSize = true;
  report.stat_before = {size: initialStat.size, dev: initialStat.dev, ino: initialStat.ino,
    mtime_ms: initialStat.mtimeMs, ctime_ms: initialStat.ctimeMs};
  report.sha256_before = fingerprint('before'); assert.equal(report.sha256_before, expectedHash, 'No semantic reads after hash mismatch');
  for (const item of report.names) {
    item.status = 'reading'; const observed = [];
    for (let offset = 0; offset < limits.bytes_per_string_including_nul; offset++) {
      assert(report.semantic_bytes < limits.semantic_bytes, 'Semantic byte budget');
      const rva = Number(item.rva) + offset, fileOffset = mapByte(rva);
      const operation = {rva: hex(rva), file_offset: fileOffset, requested_bytes: 1, read_bytes: 0, raw_hex: ''};
      report.metadata_reads.push(operation); report.semantic_bytes++;
      const got = fs.readSync(fd, one, 0, 1, fileOffset); operation.read_bytes = got;
      assert.equal(got, 1, 'Unexpected one-byte string EOF');
      operation.raw_hex = one.toString('hex'); observed.push(one[0]);
      item.bytes_read = observed.length; item.raw_hex_observed = Buffer.from(observed).toString('hex');
      if (one[0] === 0) { item.terminated = true; break; }
    }
    if (!item.terminated) {
      item.status = 'insufficient-no-nul-within-64-bytes';
      report.insufficiencies.push({rva: item.rva, reason: item.status});
      throw Error('String bound reached; no adjacent bytes requested');
    }
    const payload = observed.slice(0, -1);
    item.printable_ascii = payload.every(x => x >= 0x20 && x <= 0x7e);
    item.ascii = item.printable_ascii ? Buffer.from(payload).toString('ascii') : null;
    item.matches_export_class_label = item.printable_ascii ? item.ascii === item.export_class_label : null;
    item.status = item.printable_ascii ? 'terminated-printable-ascii' : 'terminated-raw-bytes-only';
    if (!item.printable_ascii) report.insufficiencies.push({rva: item.rva, reason: 'Nonprintable bytes: no ASCII role comparison'});
  }
  report.names_complete = report.names.every(x => x.terminated);
  report.role_comparisons_complete = report.names.every(x => x.matches_export_class_label !== null);
  report.status = report.role_comparisons_complete ? 'fixed-engine-root-names-read' : 'fixed-names-read-textual-comparison-incomplete';
} catch (error) {
  failure = error; report.status = 'failed'; report.failure = String(error);
} finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        report.sha256_after = fingerprint('after'); const after = fs.fstatSync(fd);
        report.stat_after = {size: after.size, dev: after.dev, ino: after.ino, mtime_ms: after.mtimeMs, ctime_ms: after.ctimeMs};
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
  // Never write evidence while an opened source handle has not confirmed close.
  if (fd !== undefined && report.source_closed !== true) throw failure;
  const bytes = Buffer.from(JSON.stringify(report, null, 2) + '\n', 'utf8');
  assert(bytes.length <= limits.report_bytes); fs.mkdirSync(outputDirectory);
  fs.writeFileSync(path.join(outputDirectory, 'metadata.json'), bytes, {flag: 'wx'});
}
console.log(JSON.stringify({status: report.status, semantic_bytes: report.semantic_bytes,
  names: report.names.map(x => ({rva: x.rva, status: x.status, ascii: x.ascii, matches_export_class_label: x.matches_export_class_label})),
  output: path.relative(root, path.join(outputDirectory, 'metadata.json')).replaceAll('\\', '/')}));
if (failure) process.exitCode = 1;
