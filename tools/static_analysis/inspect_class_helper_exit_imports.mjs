// Preparation only: running this file is the separate, fixed read-only action.
// Reuse the pinned descriptor/PE evidence; read only OFT slots 7/9 and names.
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
const moduleName = 'api-ms-win-crt-runtime-l1-1-0.dll', descriptorIndex = 6;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-class-helper-exit-imports');
const output = path.join(outputDirectory, 'metadata.json');
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536, metadata_read_bytes: 1024,
  metadata_single_read_bytes: 64, metadata_read_operations: 4,
  planned_metadata_bytes: 58, saved_evidence_bytes: 2 * 1024 * 1024,
  report_bytes: 128 * 1024,
});
const sources = [
  ['docs/phase-1/boundary-audit/module-boundaries.json',
    'd1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'],
  ['docs/object-lifecycle/offline-questitem-boundaries/boundary.json',
    'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05'],
  ['docs/object-lifecycle/offline-game-crt-imports-metadata/metadata.json',
    '675999fee760e0a6b84e881818942b25950d1e4c563f2c6964fd13fa25c4ed2b'],
  ['docs/object-lifecycle/offline-class-static-helper-exit-thunks/inspection.json',
    '300b74953ddec18733b5df2b2d732d9a89c09660a3dcfb60d99f6c152d811e06'],
];
const requests = Object.freeze([
  {thunk_index: 7, name: '_crt_atexit', caller_thunk_rva: 0x5dc3ce,
    expected_iat_rva: 0x60b958,
    code_sha256: 'a632c93a8f2e5da9baa6f1d095923db47d2b9457edc4b8c3963aca8fd725f9dd'},
  {thunk_index: 9, name: '_register_onexit_function', caller_thunk_rva: 0x5dc3c2,
    expected_iat_rva: 0x60b968,
    code_sha256: '6404e3652a4e90f2108f8ea5568188c99952c710747d69cf63999fb60309736e'},
]);
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
const [inventory, proof, prior, thunks] = sources.map(loadSource);
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
assert.equal(prior.descriptor.index, descriptorIndex);
assert.equal(prior.descriptor.module_name, moduleName);
assert.equal(prior.descriptor.rva, '0xa429b8');
assert.equal(prior.descriptor.original_first_thunk_rva, '0xa453e0');
assert.equal(prior.descriptor.first_thunk_rva, '0x60b920');
assert.equal(thunks.status, 'fixed-game-init-fragments-verified');
assert.equal(thunks.original, target);
assert.equal(thunks.original_sha256_before, expectedHash);
assert.equal(thunks.original_sha256_after, expectedHash);
assert.equal(thunks.original_hash_matches, true);
assert.equal(thunks.code_bytes, 12);
assert.equal(thunks.fragments.length, 2);
const game = inventory.filter(item => item.relativePath === 'x64/Game.dll');
assert.equal(game.length, 1);
assert.equal(game[0].sha256, expectedHash);
assert.equal(game[0].bytes, expectedBytes);
assert.equal(game[0].importModules[descriptorIndex].module, moduleName);
for (const item of requests) {
  assert.equal(game[0].importModules[descriptorIndex].symbols[item.thunk_index], item.name);
  const fragment = thunks.fragments.filter(f => f.start_rva === hex(item.caller_thunk_rva));
  assert.equal(fragment.length, 1);
  assert.equal(fragment[0].code_bytes, 6);
  assert.equal(fragment[0].code_sha256, item.code_sha256);
  assert.equal(fragment[0].byte_coverage_verified, true);
  const relative = `docs/object-lifecycle/offline-class-static-helper-exit-thunks/${item.caller_thunk_rva.toString(16)}.bin`;
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
  schema: 'grim-dynasty-fixed-class-helper-exit-imports-v1',
  started_utc: new Date().toISOString(), status: 'started', target,
  expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: sources.map(([p, sha256]) => ({path: p, sha256})),
  requested: requests, descriptor_reused_from_pinned_evidence: prior.descriptor,
  import_directory_reused_from_pinned_evidence: prior.import_directory,
  limits, target_open_mode: 'O_RDONLY', target_loaded_or_executed: false,
  code_bytes_read: 0, iat_payload_bytes_read: 0, runtime_global_bytes_read: 0,
  import_descriptor_bytes_read: 0, other_import_thunks_read: 0,
  initializer_table_bytes_read: 0, backend_started: false, subprocesses: 0,
  metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [], bindings: [],
  limitations: [
    'Only OFT slots 7/9 in descriptor 6 and their exact hint/name records are read; descriptor, module name and PE mappings reuse hash-pinned evidence.',
    'Fixed on-disk import identity does not prove runtime resolution, imported implementation, registration success, callback execution or failure semantics.',
    'No IAT payload, code, security cookie, onexit table or other runtime global is read or executed.',
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
  const operation = {kind, rva: hex(rva), file_offset: fileOffset, requested_bytes: length, read_bytes: 0};
  result.metadata_reads.push(operation);
  result.metadata_bytes += length;
  const bytes = Buffer.alloc(length);
  while (operation.read_bytes < length) {
    const count = fs.readSync(fd, bytes, operation.read_bytes, length - operation.read_bytes,
      fileOffset + operation.read_bytes);
    assert(count > 0, 'Unexpected EOF during metadata read');
    operation.read_bytes += count;
  }
  operation.raw_bytes = bytes.toString('hex');
  return bytes;
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
  const oft = Number.parseInt(prior.descriptor.original_first_thunk_rva, 16);
  const firstThunk = Number.parseInt(prior.descriptor.first_thunk_rva, 16);
  for (const item of requests) {
    const thunkRva = oft + item.thunk_index * 8;
    const thunk = readRva(thunkRva, 8, `fixed-oft-thunk-${item.thunk_index}`);
    const pointer = thunk.readBigUInt64LE(0);
    assert(pointer > 0n && pointer <= 0xffffffffn, 'Require name RVA, never ordinal/bound pointer');
    const nameRva = Number(pointer);
    const named = readRva(nameRva, item.name.length + 3, `exact-hint-name-${item.name}`);
    assert(named.subarray(2, named.length - 1).equals(Buffer.from(item.name, 'ascii')));
    assert.equal(named[named.length - 1], 0, 'Exact terminal NUL required');
    const iatRva = firstThunk + item.thunk_index * 8;
    assert.equal(iatRva, item.expected_iat_rva, 'IAT must agree with archived jump bytes');
    const iat = mapping(iatRva, 8);
    result.bindings.push({
      module: moduleName, name: item.name, descriptor_index: descriptorIndex,
      thunk_index: item.thunk_index, original_thunk_rva: hex(thunkRva),
      original_thunk_file_offset: mapping(thunkRva, 8).fileOffset, raw_thunk_bytes: thunk.toString('hex'),
      import_by_name_rva: hex(nameRva), import_by_name_file_offset: mapping(nameRva, named.length).fileOffset,
      raw_hint_name_bytes: named.toString('hex'), hint: named.readUInt16LE(0),
      iat_rva: hex(iatRva), preferred_iat_va: hex(imageBase + BigInt(iatRva)),
      iat_file_offset: iat.fileOffset, iat_section: iat.section,
      caller_thunk_va: hex(imageBase + BigInt(item.caller_thunk_rva)),
      iat_payload_bytes_read: 0, code_bytes_read: 0,
    });
  }
  assert.equal(result.bindings.length, 2);
  assert.equal(result.metadata_bytes, limits.planned_metadata_bytes);
  assert.equal(result.metadata_reads.length, 4);
  result.status = 'fixed-class-helper-exit-imports-resolved';
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
