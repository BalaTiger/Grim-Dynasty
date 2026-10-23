// Prepared only. Running this script is a separate fixed, read-only action.
// Resolve four archived FF25 targets; never inspect exception objects/types.
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
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-class-helper-storage-imports');
const output = path.join(outputDirectory, 'metadata.json');
const sources = [
  ['docs/phase-1/boundary-audit/module-boundaries.json',
    'd1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'],
  ['docs/object-lifecycle/offline-questitem-boundaries/boundary.json',
    'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05'],
  ['docs/object-lifecycle/offline-game-crt-imports-metadata/metadata.json',
    '675999fee760e0a6b84e881818942b25950d1e4c563f2c6964fd13fa25c4ed2b'],
  ['docs/object-lifecycle/offline-class-static-helper-storage-failure/inspection.json',
    'bef2849860977dbbdad1e979051f3e9521983d1b964f3ae10a72720a5f21517e'],
  ['docs/object-lifecycle/offline-class-static-helper-throw-thunk/inspection.json',
    'e7579a6faff63557f5a16b92739ea3af4356c799d382e2fd5909d3f234e1eecd'],
];
const descriptors = Object.freeze([
  {index: 14, module: 'api-ms-win-crt-heap-l1-1-0.dll'},
  {index: 5, module: 'VCRUNTIME140.dll'},
]);
const requests = Object.freeze([
  {descriptor_index: 14, thunk_index: 1, name: 'malloc', caller_thunk_rva: 0x5dc374,
    expected_iat_rva: 0x60b880, source_index: 3,
    code_sha256: '2e2c427236086b2c39ef1eebe92c3c2850acc1efca6ef872657d9bf9fe5c6ffb'},
  {descriptor_index: 14, thunk_index: 2, name: 'free', caller_thunk_rva: 0x5dc36e,
    expected_iat_rva: 0x60b888, source_index: 3,
    code_sha256: '0ddd1d83ed64cd02ccd5daf1051c9f46bd7e443da814d2453fb7de62f8720cf8'},
  {descriptor_index: 14, thunk_index: 3, name: '_callnewh', caller_thunk_rva: 0x5dc3a4,
    expected_iat_rva: 0x60b890, source_index: 3,
    code_sha256: 'e97a10c8d9adfa22b9dc9a857264293a8672dcd165e81572905c2882f75689ea'},
  {descriptor_index: 5, thunk_index: 3, name: '_CxxThrowException', caller_thunk_rva: 0x5dc362,
    expected_iat_rva: 0x60b7b0, source_index: 4,
    code_sha256: 'b66def74b98ab50b0a8dca95d1db2a95b9b28f16c6aab49d7ef8d5c5057c80b9'},
]);
const plannedBytes = descriptors.reduce((n, d) => n + 20 + d.module.length + 1, 0) +
  requests.reduce((n, r) => n + 8 + r.name.length + 3, 0);
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536, metadata_read_bytes: 1024,
  metadata_single_read_bytes: 64, metadata_read_operations: 12,
  planned_metadata_bytes: plannedBytes, saved_evidence_bytes: 2 * 1024 * 1024,
  report_bytes: 128 * 1024,
});
assert(plannedBytes <= limits.metadata_read_bytes);
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = value => `0x${value.toString(16)}`;
const within = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && offset <= size && length <= size - offset;
function loadSource([relative, expected]) {
  const file = path.join(root, relative);
  assert(fs.statSync(file).size <= limits.saved_evidence_bytes);
  const bytes = fs.readFileSync(file);
  assert.equal(hash(bytes), expected, `Pinned evidence changed: ${relative}`);
  return JSON.parse(bytes);
}
const evidence = sources.map(loadSource);
const [inventory, proof, prior] = evidence;
assert.equal(proof.target, target);
assert.equal(proof.status, 'questitem-ctor-read-boundaries-and-classinfo-assessed');
assert.equal(proof.source_bytes, expectedBytes);
assert.equal(proof.sha256_before, expectedHash);
assert.equal(proof.sha256_after, expectedHash);
assert.equal(proof.source_hash_preserved, true);
assert.equal(proof.pe.image_base, hex(imageBase));
assert.equal(prior.status, 'fixed-game-crt-imports-resolved');
assert.equal(prior.target, target);
assert.equal(prior.expected_file_bytes, expectedBytes);
assert.equal(prior.sha256_before, expectedHash);
assert.equal(prior.sha256_after, expectedHash);
assert.equal(prior.source_hash_preserved, true);
assert.equal(prior.import_directory.rva, '0xa42940');
assert.equal(prior.import_directory.size, 380);
for (const index of [3, 4]) {
  const report = evidence[index];
  assert.equal(report.status, 'fixed-game-init-fragments-verified');
  assert.equal(report.original, target);
  assert.equal(report.original_sha256_before, expectedHash);
  assert.equal(report.original_sha256_after, expectedHash);
  assert.equal(report.original_hash_matches, true);
}
const game = inventory.filter(item => item.relativePath === 'x64/Game.dll');
assert.equal(game.length, 1);
assert.equal(game[0].sha256, expectedHash);
assert.equal(game[0].bytes, expectedBytes);
for (const d of descriptors)
  assert.equal(game[0].importModules[d.index].module, d.module);
for (const item of requests) {
  assert.equal(game[0].importModules[item.descriptor_index].symbols[item.thunk_index], item.name);
  const report = evidence[item.source_index];
  const found = report.fragments.filter(f => f.start_rva === hex(item.caller_thunk_rva));
  assert.equal(found.length, 1);
  assert.equal(found[0].code_bytes, 6);
  assert.equal(found[0].code_sha256, item.code_sha256);
  assert.equal(found[0].byte_coverage_verified, true);
  const relative = `${path.posix.dirname(sources[item.source_index][0])}/${item.caller_thunk_rva.toString(16)}.bin`;
  assert.equal(fs.statSync(path.join(root, relative)).size, 6);
  const bytes = fs.readFileSync(path.join(root, relative));
  assert.equal(hash(bytes), item.code_sha256);
  assert.equal(bytes.readUInt16LE(0), 0x25ff, 'Require saved FF25 RIP-relative jump');
  assert.equal(item.caller_thunk_rva + 6 + bytes.readInt32LE(2), item.expected_iat_rva);
  sources.push([relative, item.code_sha256]);
}
const sections = proof.pe.sections;
assert.equal(sections.length, 9);
function mapping(rva, length) {
  assert(within(rva, length, 0x100000000), 'RVA outside uint32 domain');
  const matches = sections.filter(s => rva >= s.rva && within(rva - s.rva, length, s.raw_size));
  assert.equal(matches.length, 1, 'Require unique file-backed saved section');
  const section = matches[0], fileOffset = section.raw_offset + rva - section.rva;
  assert.equal(section.characteristics & 0x20000000, 0, 'No executable-section reads');
  assert(within(fileOffset, length, expectedBytes));
  return {fileOffset, section: section.name};
}
for (let dir = path.dirname(outputDirectory); ; dir = path.dirname(dir)) {
  const stat = fs.lstatSync(dir);
  assert(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real directory');
  assert.equal(fs.realpathSync(dir).toLowerCase(), path.resolve(dir).toLowerCase());
  if (dir === path.dirname(dir)) break;
}
assert(!fs.existsSync(outputDirectory), 'Preserve existing evidence directory');
const result = {
  schema: 'grim-dynasty-fixed-class-helper-storage-imports-v1',
  started_utc: new Date().toISOString(), status: 'started', target,
  expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: sources.map(([p, sha256]) => ({path: p, sha256})),
  requested: requests, requested_descriptors: descriptors,
  import_directory_reused_from_pinned_evidence: prior.import_directory,
  limits, target_open_mode: 'O_RDONLY', target_loaded_or_executed: false,
  code_bytes_read: 0, iat_payload_bytes_read: 0, runtime_global_bytes_read: 0,
  exception_object_or_type_bytes_read: 0, other_import_descriptors_read: 0,
  other_import_thunks_read: 0, initializer_table_bytes_read: 0,
  backend_started: false, subprocesses: 0,
  metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [], descriptors: [], bindings: [],
  limitations: [
    'Only descriptors 14/5, their exact DLL names, the four fixed OFT slots and exact hint/name records are read. No descriptor/thunk lists are scanned.',
    'PE mappings and import-directory location reuse hash-pinned evidence from the same image.',
    'Archived FF25 bytes supply the required IAT targets; IAT payloads and executable sections are not read.',
    'Import identity does not recover external implementation, allocator success, new-handler policy, exception type, unwind or cleanup behavior.',
    'No exception constructor, throw descriptor, callback, cookie, runtime table/global or original DLL code is read or executed.',
    'Before/after full-file hashes use 64KiB buffers and are outside semantic byte totals.',
    'O_RDONLY plus same-handle stat/hash checks does not guarantee atomic snapshot or exclusive Windows FileShare.Read.',
  ],
};
const fingerprintBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
let fd, initialStat, admittedSize = false, failure;
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes);
  const digest = createHash('sha256');
  let at = 0;
  while (at < expectedBytes) {
    const count = fs.readSync(fd, fingerprintBuffer, 0,
      Math.min(fingerprintBuffer.length, expectedBytes - at), at);
    assert(count > 0, 'Unexpected EOF during hash');
    digest.update(fingerprintBuffer.subarray(0, count));
    at += count;
  }
  const sha256 = digest.digest('hex');
  result.fingerprint_passes.push({label, bytes: at, chunk_bytes: fingerprintBuffer.length, sha256});
  return sha256;
}
function readRva(rva, length, kind) {
  const {fileOffset} = mapping(rva, length);
  assert(length <= limits.metadata_single_read_bytes);
  assert(result.metadata_reads.length < limits.metadata_read_operations);
  assert(result.metadata_bytes + length <= limits.metadata_read_bytes);
  assert(result.metadata_bytes + length <= limits.planned_metadata_bytes);
  const op = {kind, rva: hex(rva), file_offset: fileOffset, requested_bytes: length, read_bytes: 0};
  result.metadata_reads.push(op);
  result.metadata_bytes += length;
  const bytes = Buffer.alloc(length);
  while (op.read_bytes < length) {
    const count = fs.readSync(fd, bytes, op.read_bytes, length - op.read_bytes, fileOffset + op.read_bytes);
    assert(count > 0, 'Unexpected EOF during metadata read');
    op.read_bytes += count;
  }
  op.raw_bytes = bytes.toString('hex');
  return bytes;
}
function exactName(bytes, offset, expected) {
  assert.equal(bytes.length, offset + expected.length + 1);
  assert(bytes.subarray(offset, bytes.length - 1).equals(Buffer.from(expected, 'ascii')));
  assert.equal(bytes[bytes.length - 1], 0, 'Exact terminal NUL required');
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(),
    'Unexpected fixed Game redirection');
  fd = fs.openSync(target, fs.constants.O_RDONLY);
  initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile());
  assert.equal(initialStat.size, expectedBytes);
  admittedSize = true;
  result.sha256_before = fingerprint('before');
  assert.equal(result.sha256_before, expectedHash, 'Stop before metadata on image mismatch');
  const directoryRva = Number.parseInt(prior.import_directory.rva, 16);
  for (const d of descriptors) {
    assert(within(d.index * 20, 20, prior.import_directory.size));
    const descriptorRva = directoryRva + d.index * 20;
    const bytes = readRva(descriptorRva, 20, `fixed-import-descriptor-${d.index}`);
    const oft = bytes.readUInt32LE(0), nameRva = bytes.readUInt32LE(12), firstThunk = bytes.readUInt32LE(16);
    assert(oft > 0 && nameRva > 0 && firstThunk > 0, 'Require original named-thunk table; no fallback');
    const name = readRva(nameRva, d.module.length + 1, `exact-module-name-${d.index}`);
    exactName(name, 0, d.module);
    result.descriptors.push({
      index: d.index, module: d.module, rva: hex(descriptorRva),
      file_offset: mapping(descriptorRva, 20).fileOffset, raw_bytes: bytes.toString('hex'),
      original_first_thunk_rva: hex(oft), name_rva: hex(nameRva), first_thunk_rva: hex(firstThunk),
    });
    for (const item of requests.filter(r => r.descriptor_index === d.index)) {
      const thunkRva = oft + item.thunk_index * 8;
      const thunk = readRva(thunkRva, 8, `fixed-oft-${d.index}-${item.thunk_index}`);
      const pointer = thunk.readBigUInt64LE(0);
      assert(pointer > 0n && pointer <= 0xffffffffn, 'Require name RVA, never ordinal/bound pointer');
      const importNameRva = Number(pointer);
      const named = readRva(importNameRva, item.name.length + 3, `exact-hint-name-${item.name}`);
      exactName(named, 2, item.name);
      const iatRva = firstThunk + item.thunk_index * 8;
      assert.equal(iatRva, item.expected_iat_rva, 'IAT must agree with archived jump bytes');
      const iat = mapping(iatRva, 8);
      result.bindings.push({
        module: d.module, name: item.name, descriptor_index: d.index, thunk_index: item.thunk_index,
        original_thunk_rva: hex(thunkRva), original_thunk_file_offset: mapping(thunkRva, 8).fileOffset,
        raw_thunk_bytes: thunk.toString('hex'), import_by_name_rva: hex(importNameRva),
        import_by_name_file_offset: mapping(importNameRva, named.length).fileOffset,
        raw_hint_name_bytes: named.toString('hex'), hint: named.readUInt16LE(0),
        iat_rva: hex(iatRva), preferred_iat_va: hex(imageBase + BigInt(iatRva)),
        iat_file_offset: iat.fileOffset, iat_section: iat.section,
        caller_thunk_va: hex(imageBase + BigInt(item.caller_thunk_rva)),
        iat_payload_bytes_read: 0, code_bytes_read: 0,
      });
    }
  }
  assert.equal(result.bindings.length, 4);
  assert.equal(result.descriptors.length, 2);
  assert.equal(result.metadata_bytes, plannedBytes);
  assert.equal(result.metadata_reads.length, 12);
  result.status = 'fixed-class-helper-storage-imports-resolved';
} catch (error) {
  failure = error; result.status = 'failed'; result.failure = String(error);
} finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        result.sha256_after = fingerprint('after');
        const after = fs.fstatSync(fd);
        result.source_stat_stable = after.isFile() && after.size === initialStat.size &&
          after.dev === initialStat.dev && after.ino === initialStat.ino &&
          after.mtimeMs === initialStat.mtimeMs && after.ctimeMs === initialStat.ctimeMs;
        result.source_hash_preserved = result.sha256_before === expectedHash && result.sha256_after === expectedHash;
        assert(result.source_stat_stable && result.source_hash_preserved);
      }
    } catch (error) {
      result.final_fingerprint_failure = String(error); failure ??= error; result.status = 'failed';
    } finally {
      try { fs.closeSync(fd); }
      catch (error) { result.close_failure = String(error); failure ??= error; result.status = 'failed'; }
    }
  }
  result.finished_utc = new Date().toISOString();
  const encoded = `${JSON.stringify(result, null, 2)}\n`;
  assert(Buffer.byteLength(encoded) <= limits.report_bytes);
  fs.mkdirSync(outputDirectory);
  fs.writeFileSync(output, encoded, {flag: 'wx'});
}
console.log(JSON.stringify({status: result.status, output, metadata_bytes: result.metadata_bytes,
  source_hash_preserved: result.source_hash_preserved, bindings: result.bindings}));
if (failure) process.exitCode = 1;
