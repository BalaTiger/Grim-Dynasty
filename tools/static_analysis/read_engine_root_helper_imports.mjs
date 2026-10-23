// PREPARED, NOT EXECUTED. Seven fixed Engine helper import bindings only.
// No arguments, build, DLL loading, code reads, IAT payload reads or subprocess.
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
const directoryRva = 0x415598, directorySize = 540;
const sources = Object.freeze([
  {path: 'docs/object-lifecycle/offline-depot-ctor-boundaries/boundary.json',
    sha256: '646053e4565e3f08c696ae0e9a16ddb5c2e81ab22ffe046c5057fc41aa64bb15'},
  {path: 'docs/phase-1/boundary-audit/module-boundaries.json',
    sha256: 'd1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'},
  {path: 'docs/object-lifecycle/offline-load-ticks-imports/tick-import-bindings.json',
    sha256: 'eb1e49dfe763e207be743b91d03b2b4c066057063da052cb3e1e3a97807aab0e'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-initializer-slot23/inspection.json',
    sha256: 'ce14b4749b43cef1fe407b7f3e2b23a5eb1c5c333f362af782b4c41d0b503297'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-initializer-slot23/5c70-instructions.json',
    sha256: 'c3d09dc72abc57f2832e9d455435648ae0aa8d5efa3a00e70d5c3395d9ace368'},
  {path: 'docs/object-lifecycle/offline-engine-root-initializer-table/table.json',
    sha256: '29d059bdb204e1116c6a856537267152d54848c0fd8fc82fefc43914bbf4fa6e'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-import-thunks-and-failure-wrappers/inspection.json',
    sha256: '0deb403d187232e400e1a6f6ccbc4886580ae8627b6a2556aca2844e5f5e43ab'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2ddb38-instructions.json',
    sha256: '0b32b2eb72030ede3ad6328d9a2b9abde94344caf4836e6e7568597d9a51fd4f'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2ddb3e-instructions.json',
    sha256: '7c12cf1c92b4c246254d5a18ea0adcadc01c15d6eab75d92969ca6e8bd0b6bff'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2ddb7a-instructions.json',
    sha256: 'c67dd05bea5961b3369272fa9da74bd372eeaaf8ad625bf847c81ab32ea463c9'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2ddb98-instructions.json',
    sha256: '6ae89458189a02b4fb168b70a19e2d35a426a7f524abe5015f5e14700e891a6e'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2ddba4-instructions.json',
    sha256: '0ab0f970e8be8c23675f2eccb64767d73d2db5632125f8d3bb82799c54fc064a'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-throw-targets/inspection.json',
    sha256: 'fad714e89b60e7d4a727452d432201f41d7898ad90c18c80bbd04684ea95d2ed'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-helper-throw-targets/2ddb26-instructions.json',
    sha256: '772437606dd4c859b2d0056dac870f6f4560852e2488133fb4fbb4fe5dc52c9f'},
  {path: 'docs/phase-1/round-4/constant-and-crt-thunk-evidence.json',
    sha256: '80ad8ab1a166fffe584418c5b56abca0b2eab1326d4398ee6c1531eca70f43a6'},
]);
// This fixed list may only change after a newly archived caller proves another exact IAT.
// The inventory suggests the descriptor/thunk; the live metadata must independently match.
const targets = Object.freeze([
  {descriptor: 17, thunk: 5, module: 'api-ms-win-crt-string-l1-1-0.dll', name: '_strdup',
    iat: 0x302bb8, expectedFirstThunk: 0x302b90,
    callerSource: 4, callerVa: '0x180005c7d', callerBytes: 'ff1535cf2f00'},
  {descriptor: 21, thunk: 3, module: 'api-ms-win-crt-heap-l1-1-0.dll', name: 'malloc',
    iat: 0x302968, expectedFirstThunk: 0x302950,
    callerSource: 7, callerVa: '0x1802ddb38', callerBytes: 'ff252a4e0200'},
  {descriptor: 21, thunk: 4, module: 'api-ms-win-crt-heap-l1-1-0.dll', name: 'free',
    iat: 0x302970, expectedFirstThunk: 0x302950,
    callerSource: 8, callerVa: '0x1802ddb3e', callerBytes: 'ff252c4e0200'},
  {descriptor: 21, thunk: 2, module: 'api-ms-win-crt-heap-l1-1-0.dll', name: '_callnewh',
    iat: 0x302960, expectedFirstThunk: 0x302950,
    callerSource: 9, callerVa: '0x1802ddb7a', callerBytes: 'ff25e04d0200'},
  {descriptor: 14, thunk: 12, module: 'api-ms-win-crt-runtime-l1-1-0.dll', name: '_register_onexit_function',
    iat: 0x302a88, expectedFirstThunk: 0x302a28,
    callerSource: 10, callerVa: '0x1802ddb98', callerBytes: 'ff25ea4e0200'},
  {descriptor: 14, thunk: 9, module: 'api-ms-win-crt-runtime-l1-1-0.dll', name: '_crt_atexit',
    iat: 0x302a70, expectedFirstThunk: 0x302a28,
    callerSource: 11, callerVa: '0x1802ddba4', callerBytes: 'ff25c64e0200'},
  {descriptor: 13, thunk: 2, module: 'VCRUNTIME140.dll', name: '_CxxThrowException',
    iat: 0x302708, expectedFirstThunk: 0x3026f8,
    callerSource: 13, callerVa: '0x1802ddb26', callerBytes: 'ff25dc4b0200'},
]);
const limits = Object.freeze({metadata_bytes: 298, single_read_bytes: 64, metadata_reads: 20,
  fingerprint_chunk_bytes: 65536, saved_proof_bytes_each: 2 * 1024 * 1024, report_bytes: 128 * 1024});
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-engine-root-helper-imports');
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = n => `0x${n.toString(16)}`;
const within = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && offset <= size && length <= size - offset;
const loaded = sources.map(source => {
  const file = path.join(root, source.path); assert(fs.statSync(file).size <= limits.saved_proof_bytes_each);
  const bytes = fs.readFileSync(file); assert.equal(hash(bytes), source.sha256, 'Pinned project evidence changed');
  return JSON.parse(bytes.toString('utf8'));
});
const peProof = loaded[0], inventory = loaded[1], directoryProof = loaded[2], writerProof = loaded[3];
assert.equal(peProof.target, target); assert.equal(peProof.source_bytes, expectedBytes);
assert.equal(peProof.sha256_before, expectedHash); assert.equal(peProof.sha256_after, expectedHash);
assert(peProof.source_stat_stable && peProof.source_hash_preserved);
assert.equal(peProof.pe.image_base, hex(imageBase));
assert.equal(directoryProof.status, 'fixed-two-tick-imports-confirmed');
assert.equal(directoryProof.original, target); assert.equal(directoryProof.expected_file_bytes, expectedBytes);
assert.equal(directoryProof.original_sha256_before, expectedHash); assert.equal(directoryProof.original_sha256_after, expectedHash);
assert(directoryProof.original_hash_matches);
assert.equal(Number(directoryProof.headers.import_directory_rva), directoryRva);
assert.equal(directoryProof.headers.import_directory_size, directorySize);
assert.equal(writerProof.status, 'fixed-engine-root-fragments-verified');
assert.equal(writerProof.original, target); assert.equal(writerProof.expected_file_bytes, expectedBytes);
assert.equal(writerProof.original_sha256_before, expectedHash); assert.equal(writerProof.original_sha256_after, expectedHash);
assert(writerProof.original_hash_matches && writerProof.original_stat_stable && writerProof.original_closed);
const runtimeProof = loaded[5], helperProof = loaded[6], throwProof = loaded[12];
assert.equal(runtimeProof.status, 'fixed-engine-initializer-table-and-crt-bindings-read');
assert.equal(runtimeProof.target, target); assert.equal(runtimeProof.expected_file_bytes, expectedBytes);
assert.equal(runtimeProof.sha256_before, expectedHash); assert.equal(runtimeProof.sha256_after, expectedHash);
assert(runtimeProof.source_stat_stable && runtimeProof.source_hash_preserved && runtimeProof.source_closed);
for (const evidence of [helperProof, throwProof]) {
  assert.equal(evidence.original, target); assert.equal(evidence.expected_file_bytes, expectedBytes);
  assert.equal(evidence.original_sha256_before, expectedHash); assert.equal(evidence.original_sha256_after, expectedHash);
  assert(evidence.original_stat_stable && evidence.original_hash_matches && evidence.original_closed);
}
assert.equal(helperProof.status, 'fixed-engine-root-fragments-verified');
// The later ctor lookup failed. Only its already verified first six-byte thunk is used.
assert.equal(throwProof.status, 'failed'); assert.equal(throwProof.code_bytes, 6);
assert.equal(throwProof.fragments.length, 1);
assert.equal(throwProof.fragments[0].start_rva, '0x2ddb26');
assert.equal(throwProof.fragments[0].code_bytes, 6); assert(throwProof.fragments[0].byte_coverage_verified);
assert.equal(throwProof.fragments[0].tool_exit, 0);
const oldMalloc = loaded[14].find(x => x.module === 'x64/Engine.dll').thunks[0];
assert.equal(oldMalloc.iatRVA, '0x302968'); assert.equal(oldMalloc.import.symbol, 'malloc');
assert.equal(oldMalloc.import.module, 'api-ms-win-crt-heap-l1-1-0.dll');
const engine = inventory.filter(x => x.relativePath === 'x64/Engine.dll');
assert.equal(engine.length, 1); assert.equal(engine[0].sha256, expectedHash); assert.equal(engine[0].bytes, expectedBytes);
for (const item of targets) {
  assert.equal(engine[0].importModules[item.descriptor].module, item.module);
  assert.equal(engine[0].importModules[item.descriptor].symbols[item.thunk], item.name);
  const caller = loaded[item.callerSource].filter(x => x.address === item.callerVa);
  assert.equal(caller.length, 1); assert.equal(caller[0].bytes, item.callerBytes);
  const bytes = Buffer.from(item.callerBytes, 'hex');
  assert.equal(bytes.length, 6);
  assert.equal(bytes.subarray(0, 2).toString('hex'), item.callerSource === 4 ? 'ff15' : 'ff25');
  assert.equal(Number(BigInt(item.callerVa) - imageBase) + 6 + bytes.readInt32LE(2), item.iat);
  assert.equal(item.expectedFirstThunk + item.thunk * 8, item.iat);
}
function mapping(rva, length) {
  assert(within(rva, length, 0x100000000));
  const sections = peProof.pe.sections.filter(s => rva >= s.rva && within(rva - s.rva, length, s.raw_size));
  assert.equal(sections.length, 1, 'Require one file-backed section');
  const s = sections[0], offset = s.raw_offset + rva - s.rva;
  assert.equal(s.characteristics & 0x20000000, 0, 'No executable section reads');
  assert(within(offset, length, expectedBytes)); return offset;
}
for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  const stat = fs.lstatSync(directory); assert(stat.isDirectory() && !stat.isSymbolicLink());
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(), 'Output ancestor redirected');
  if (directory === path.dirname(directory)) break;
}
fs.mkdirSync(outputDirectory); // Fresh only, never overwrite earlier evidence.
const report = {
  schema: 'grim-dynasty-fixed-engine-root-helper-imports-v1', status: 'started',
  started_utc: new Date().toISOString(), script_sha256: hash(fs.readFileSync(script)),
  target, expected_sha256: expectedHash, expected_file_bytes: expectedBytes, source_evidence: sources,
  targets, limits, reused_import_directory: {rva: hex(directoryRva), size: directorySize, source: sources[2]},
  target_open_mode: 'O_RDONLY', target_loaded_or_executed: false, new_header_bytes_read: 0,
  original_code_semantic_reads: 0, iat_payload_bytes_read: 0, backend_started: false,
  subprocesses: 0, compilation_or_tests: false, metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [], bindings: [],
  reused_metadata: [], old_malloc_record: {source: sources[14], binding: oldMalloc.import,
    iat_rva: oldMalloc.iatRVA, complete_raw_descriptor_oft_name_present: false},
  limitations: [
    'The saved inventory suggests the fixed descriptor/thunk pair. Its original FirstThunk is not treated as previously proved merely from the inventory ordering.',
    'New metadata must independently verify the fixed descriptor, exact module, FirstThunk, selected OFT cell and exact hint/name against the archived Engine caller IAT.',
    'Only seven fixed selected import bindings are inspected. Descriptor/module 14 are reused from complete pinned raw metadata; descriptors 17,21,13 are each read once. No header or other import entry is scanned.',
    'The old round-4 malloc record identifies the binding but lacks complete raw descriptor/OFT/name bytes, so the exact selected malloc metadata is independently recorded here. It is not treated as already verified raw metadata.',
    'The throw-target batch failed only after recording its six-byte thunk; this reader uses that individually verified fragment and does not infer any missing constructor body or failure behavior.',
    'No original code, IAT payload, imported DLL or callback is opened or executed.',
    'Name binding alone does not establish runtime library resolution, allocator behavior, retries, exception handling, native ownership or ABI equivalence.',
    'Before/after whole-file hashes traverse all bytes using a 64KiB buffer without interpreting code. O_RDONLY and same-handle stat/hash checks do not guarantee an atomic snapshot or exclusive Windows FileShare.Read.',
  ],
};
let fd, initialStat, admittedSize = false, failure;
const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes); const digest = createHash('sha256'); let at = 0;
  while (at < expectedBytes) {
    const got = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, expectedBytes - at), at);
    assert(got > 0, 'Unexpected fingerprint EOF'); digest.update(hashBuffer.subarray(0, got)); at += got;
  }
  const sha256 = digest.digest('hex');
  report.fingerprint_passes.push({label, bytes: at, chunk_bytes: hashBuffer.length, sha256}); return sha256;
}
function read(rva, length, kind) {
  assert(length <= limits.single_read_bytes && length <= limits.metadata_bytes - report.metadata_bytes, 'Metadata budget');
  assert(report.metadata_reads.length < limits.metadata_reads, 'Operation budget');
  const offset = mapping(rva, length), bytes = Buffer.alloc(length);
  const op = {kind, rva: hex(rva), file_offset: offset, requested_bytes: length, read_bytes: 0, raw_bytes: ''};
  report.metadata_reads.push(op); report.metadata_bytes += length;
  while (op.read_bytes < length) {
    const got = fs.readSync(fd, bytes, op.read_bytes, length - op.read_bytes, offset + op.read_bytes);
    assert(got > 0, 'Unexpected metadata EOF'); op.read_bytes += got;
    op.raw_bytes = bytes.subarray(0, op.read_bytes).toString('hex');
  }
  return bytes;
}
function exactName(bytes, prefixBytes, expected) {
  assert.equal(bytes.length, prefixBytes + expected.length + 1);
  assert(bytes.subarray(prefixBytes, -1).equals(Buffer.from(expected, 'ascii')));
  assert.equal(bytes[bytes.length - 1], 0);
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(), 'Fixed Engine path redirected');
  fd = fs.openSync(target, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile()); assert.equal(initialStat.size, expectedBytes); admittedSize = true;
  report.sha256_before = fingerprint('before'); assert.equal(report.sha256_before, expectedHash, 'No semantic reads after hash mismatch');
  const descriptors = new Map();
  function rawSaved(kind, expectedRva, expectedLength) {
    const matches = runtimeProof.metadata_reads.filter(x => x.kind === kind);
    assert.equal(matches.length, 1); const item = matches[0];
    assert.equal(Number(item.rva), expectedRva); assert.equal(item.read_bytes, expectedLength);
    assert.equal(item.file_offset, mapping(expectedRva, expectedLength));
    const bytes = Buffer.from(item.raw_bytes, 'hex'); assert.equal(bytes.length, expectedLength);
    report.reused_metadata.push({source: sources[5], ...item}); return bytes;
  }
  for (const item of targets) {
    assert(within(item.descriptor * 20, 20, directorySize));
    const descriptorRva = directoryRva + item.descriptor * 20;
    if (!descriptors.has(item.descriptor)) {
      const descriptor = item.descriptor === 14 ? rawSaved('fixed-import-descriptor-14', descriptorRva, 20) :
        read(descriptorRva, 20, `fixed-descriptor-${item.descriptor}`);
      const nameRva = descriptor.readUInt32LE(12);
      const moduleBytes = item.descriptor === 14 ? rawSaved('exact-runtime-module-name', nameRva, item.module.length + 1) :
        read(nameRva, item.module.length + 1, `exact-module-name-${item.descriptor}`);
      exactName(moduleBytes, 0, item.module);
      descriptors.set(item.descriptor, {descriptor, module: item.module});
    }
    const stored = descriptors.get(item.descriptor), descriptor = stored.descriptor;
    assert.equal(stored.module, item.module);
    const oft = descriptor.readUInt32LE(0), nameRva = descriptor.readUInt32LE(12), firstThunk = descriptor.readUInt32LE(16);
    assert(oft > 0 && nameRva > 0 && firstThunk > 0, 'Require original named-thunk table');
    assert.equal(firstThunk, item.expectedFirstThunk, 'Selected descriptor FirstThunk mismatch');
    assert.equal(firstThunk + item.thunk * 8, item.iat, 'Selected thunk must bind the archived caller IAT');
    const thunkRva = oft + item.thunk * 8, thunk = read(thunkRva, 8, 'exact-original-thunk');
    const namePointer = thunk.readBigUInt64LE(0);
    assert(namePointer > 0n && namePointer <= 0xffffffffn, 'No ordinal or bound-address fallback');
    const hintNameRva = Number(namePointer), named = read(hintNameRva, item.name.length + 3, 'exact-hint-name');
    exactName(named, 2, item.name);
    report.bindings.push({module: item.module, name: item.name, descriptor_index: item.descriptor,
      descriptor_rva: hex(descriptorRva), original_first_thunk_rva: hex(oft), first_thunk_rva: hex(firstThunk),
      thunk_index: item.thunk, original_thunk_rva: hex(thunkRva), hint_name_rva: hex(hintNameRva),
      hint: named.readUInt16LE(0), iat_rva: hex(item.iat), preferred_iat_va: hex(imageBase + BigInt(item.iat)),
      caller_va: item.callerVa, caller_source: sources[item.callerSource], iat_payload_bytes_read: 0});
  }
  assert.equal(report.metadata_bytes, limits.metadata_bytes);
  report.status = 'fixed-engine-root-helper-imports-resolved';
} catch (error) {
  failure = error; report.status = 'failed'; report.failure = String(error);
} finally {
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
  report.finished_utc = new Date().toISOString(); const bytes = Buffer.from(JSON.stringify(report, null, 2) + '\n', 'utf8');
  assert(bytes.length <= limits.report_bytes); fs.writeFileSync(path.join(outputDirectory, 'metadata.json'), bytes, {flag: 'wx'});
}
console.log(JSON.stringify({status: report.status, metadata_bytes: report.metadata_bytes, bindings: report.bindings,
  output: path.relative(root, path.join(outputDirectory, 'metadata.json')).replaceAll('\\', '/')}));
if (failure) process.exitCode = 1;
