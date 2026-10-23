// PREPARED ONLY. Two fixed Engine lock-import bindings; no arguments or subprocess.
// Reuse archived raw descriptor/module; new metadata is OFT16 + hint/name23*2 = 62B.
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
const sources = [
  ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json', '39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
  ['docs/phase-1/boundary-audit/module-boundaries.json', 'd1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'],
  ['docs/object-lifecycle/offline-load-ticks-imports/tick-import-bindings.json', 'eb1e49dfe763e207be743b91d03b2b4c066057063da052cb3e1e3a97807aab0e'],
  ['docs/object-lifecycle/offline-actor-matrix-initialization/inspection.json', '54d33ae66b7aea0358d7e652db7743725654584f3cdfe2a5010a6ff183e48a7e'],
  ['docs/object-lifecycle/offline-actor-matrix-initialization/2dcae0-instructions.json', '80a3ae3ef767ed745a2080c4494c193b4a2fae0f9497d8e3df700f4d0e4d0f0d'],
  ['docs/object-lifecycle/offline-actor-matrix-initialization/2dca80-instructions.json', '50e12e544bce9b0f21de60be8e46f4a1b40d0f895d6edf6e12bce34754a63822'],
].map(([path, sha256]) => ({path, sha256}));
const targets = [{thunk: 54, iat: 0x302328, name: 'EnterCriticalSection'},
  {thunk: 55, iat: 0x302330, name: 'LeaveCriticalSection'}];
const callers = [
  {source: 4, address: '0x1802dcaf0', bytes: 'ff1532580200', iat: 0x302328},
  {source: 4, address: '0x1802dcb40', bytes: '48ff25e9570200', iat: 0x302330},
  {source: 5, address: '0x1802dca90', bytes: 'ff1592580200', iat: 0x302328},
  {source: 5, address: '0x1802dcad0', bytes: 'ff155a580200', iat: 0x302330},
];
const limits = {metadata_bytes: 62, metadata_reads: 3, single_read_bytes: 23,
  fingerprint_chunk_bytes: 65536, saved_proof_bytes_each: 2097152, report_bytes: 65536};
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = n => `0x${n.toString(16)}`;
const within = (at, n, size) => Number.isSafeInteger(at) && at >= 0 && Number.isSafeInteger(n) && n > 0 && at <= size && n <= size - at;
const loaded = sources.map(s => {
  const p = path.join(root, s.path); assert(fs.statSync(p).size <= limits.saved_proof_bytes_each);
  const b = fs.readFileSync(p); assert.equal(hash(b), s.sha256, 'Pinned project evidence changed'); return JSON.parse(b.toString('utf8'));
});
const [peProof, inventory, ticks, batch] = loaded;
assert.equal(peProof.original, target); assert.equal(peProof.sha256_before, expectedHash); assert.equal(peProof.sha256_after, expectedHash);
assert(peProof.source_hash_preserved && peProof.source_stat_stable); assert.equal(peProof.pe.image_base, hex(imageBase));
assert.equal(ticks.status, 'fixed-two-tick-imports-confirmed'); assert.equal(ticks.original, target);
assert.equal(ticks.expected_file_bytes, expectedBytes); assert.equal(ticks.original_sha256_before, expectedHash);
assert.equal(ticks.original_sha256_after, expectedHash); assert(ticks.original_hash_matches);
assert.equal(Number(ticks.headers.import_directory_rva), 0x415598); assert.equal(ticks.headers.import_directory_size, 540);
assert.equal(batch.status, 'fixed-actor-matrix-initialization-verified'); assert.equal(batch.original, target);
assert.equal(batch.expected_file_bytes, expectedBytes); assert.equal(batch.sha256_before, expectedHash); assert.equal(batch.sha256_after, expectedHash);
assert(batch.source_hash_preserved && batch.source_stat_stable && batch.source_handle_closed);
for (const [rva, size] of [['0x2dcae0', 103], ['0x2dca80', 96]]) {
  const found = batch.targets.filter(x => x.rva === rva); assert.equal(found.length, 1);
  assert.equal(found[0].status, 'exact-body-disassembly-verified'); assert.equal(found[0].code_bytes, size);
  assert(found[0].byte_coverage_verified); assert.equal(found[0].tool_exit, 0);
}
const engines = inventory.filter(x => x.relativePath === 'x64/Engine.dll'); assert.equal(engines.length, 1);
assert.equal(engines[0].sha256, expectedHash); assert.equal(engines[0].bytes, expectedBytes);
assert.equal(engines[0].importModules[5].module, 'KERNEL32.dll');
for (const t of targets) assert.equal(engines[0].importModules[5].symbols[t.thunk], t.name);
for (const c of callers) {
  const found = loaded[c.source].filter(x => x.address === c.address); assert.equal(found.length, 1); assert.equal(found[0].bytes, c.bytes);
  const b = Buffer.from(c.bytes, 'hex'), prefix = b.length === 7 ? '48ff25' : 'ff15';
  assert.equal(b.subarray(0, b.length - 4).toString('hex'), prefix);
  assert.equal(Number(BigInt(c.address) - imageBase) + b.length + b.readInt32LE(b.length - 4), c.iat);
}
function mapping(rva, n) {
  assert(within(rva, n, 0x100000000));
  const found = peProof.pe.sections.filter(s => rva >= s.rva && within(rva - s.rva, n, s.raw_size));
  assert.equal(found.length, 1, 'Require one file-backed section'); const s = found[0];
  assert.equal(s.characteristics & 0x20000000, 0, 'No executable section metadata reads');
  const offset = s.raw_offset + rva - s.rva; assert(within(offset, n, expectedBytes)); return offset;
}
const old = ticks.normal_imports.filter(x => x.iat_va === '0x180302208'); assert.equal(old.length, 1);
assert.equal(old[0].descriptor_index, 5); assert.equal(old[0].dll, 'KERNEL32.dll');
const raw = old[0].raw_metadata, descriptor = Buffer.from(raw.descriptor.bytes, 'hex');
assert.equal(Number(raw.descriptor.rva), 0x415598 + 5 * 20); assert.equal(descriptor.length, 20);
assert.equal(raw.descriptor.file_offset, mapping(0x4155fc, 20));
assert.equal(descriptor.toString('hex'), '3059410000000000000000004a6d410078213000');
const oft = descriptor.readUInt32LE(0), moduleRva = descriptor.readUInt32LE(12), firstThunk = descriptor.readUInt32LE(16);
assert.equal(oft, 0x415930); assert.equal(firstThunk, 0x302178);
assert.equal(Number(raw.dll_name_with_nul.rva), moduleRva); assert.equal(raw.dll_name_with_nul.file_offset, mapping(moduleRva, 13));
assert.equal(raw.dll_name_with_nul.bytes, Buffer.from('KERNEL32.dll\0', 'ascii').toString('hex'));
for (const t of targets) assert.equal(firstThunk + t.thunk * 8, t.iat);
const output = path.join(root, 'docs/object-lifecycle/offline-actor-matrix-lock-imports');
for (let p = path.dirname(output); ; p = path.dirname(p)) {
  const s = fs.lstatSync(p); assert(s.isDirectory() && !s.isSymbolicLink());
  assert.equal(fs.realpathSync(p).toLowerCase(), path.resolve(p).toLowerCase(), 'Output ancestor redirected');
  if (p === path.dirname(p)) break;
}
fs.mkdirSync(output); // Fresh only. No overwrite or automatic retry directory.
const report = {schema: 'grim-dynasty-fixed-actor-matrix-lock-imports-v1', status: 'started', started_utc: new Date().toISOString(),
  script_sha256: hash(fs.readFileSync(script)), original: target, expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  source_evidence: sources, callers, targets, limits, metadata_bytes: 0, metadata_reads: [], bindings: [], fingerprint_passes: [],
  reused_metadata: {source: sources[2], descriptor: raw.descriptor, module: raw.dll_name_with_nul},
  target_open_mode: 'O_RDONLY', target_loaded_or_executed: false, code_bytes_read: 0, iat_payload_bytes_read: 0,
  header_bytes_read: 0, backend_started: false, subprocesses: 0, build_or_tests: false, permission_change: false,
  limitations: ['Only named normal-import identity is established; no DLL resolution, critical-section initialization, synchronization, scheduling or runtime behavior is observed.',
    'Descriptor5 and its module name are reused from pinned complete raw Engine metadata. Only OFT54/55 and their exact expected hint/names are newly read; no table scan or neighboring target.',
    'Full-file fingerprints traverse all bytes with a 64KiB buffer separately from semantic metadata reads. Same-handle before/after checks do not provide an atomic snapshot.']};
let fd, before, admitted = false, failure;
const buffer = Buffer.alloc(65536);
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes); const h = createHash('sha256'); let at = 0;
  while (at < expectedBytes) {const n = fs.readSync(fd, buffer, 0, Math.min(buffer.length, expectedBytes - at), at); assert(n > 0); h.update(buffer.subarray(0, n)); at += n;}
  const sha256 = h.digest('hex'); report.fingerprint_passes.push({label, bytes: at, chunk_bytes: buffer.length, sha256}); return sha256;
}
function read(rva, n, kind) {
  assert(n <= limits.single_read_bytes && n <= limits.metadata_bytes - report.metadata_bytes); assert(report.metadata_reads.length < limits.metadata_reads);
  const at = mapping(rva, n), b = Buffer.alloc(n), op = {kind, rva: hex(rva), file_offset: at, requested_bytes: n, read_bytes: 0, raw_bytes: ''};
  report.metadata_reads.push(op); report.metadata_bytes += n;
  while (op.read_bytes < n) {const got = fs.readSync(fd, b, op.read_bytes, n - op.read_bytes, at + op.read_bytes); assert(got > 0); op.read_bytes += got; op.raw_bytes = b.subarray(0, op.read_bytes).toString('hex');} return b;
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(), 'Fixed path redirected');
  fd = fs.openSync(target, fs.constants.O_RDONLY); before = fs.fstatSync(fd); assert(before.isFile()); assert.equal(before.size, expectedBytes); admitted = true;
  report.sha256_before = fingerprint('before'); assert.equal(report.sha256_before, expectedHash, 'Hash mismatch: no semantic reads');
  const cells = read(oft + 54 * 8, 16, 'fixed-OFT-54-and-55');
  for (let i = 0; i < targets.length; i++) {
    const t = targets[i], pointer = cells.readBigUInt64LE(i * 8); assert(pointer > 0n && pointer <= 0xffffffffn, 'No ordinal/bound-pointer fallback');
    const hintRva = Number(pointer), named = read(hintRva, t.name.length + 3, `exact-hint-name-${t.thunk}`);
    assert.equal(named.length, 23); assert(named.subarray(2, -1).equals(Buffer.from(t.name, 'ascii'))); assert.equal(named[named.length - 1], 0);
    report.bindings.push({module: 'KERNEL32.dll', name: t.name, descriptor_index: 5, descriptor_rva: '0x4155fc', original_first_thunk_rva: hex(oft),
      first_thunk_rva: hex(firstThunk), thunk_index: t.thunk, original_thunk_rva: hex(oft + t.thunk * 8), hint_name_rva: hex(hintRva), hint: named.readUInt16LE(0), iat_rva: hex(t.iat)});
  }
  assert.equal(report.metadata_bytes, 62); report.status = 'fixed-actor-matrix-lock-imports-resolved';
} catch (e) {failure = e; report.status = 'failed'; report.failure = String(e);}
finally {
  if (fd !== undefined) {
    try {if (admitted) {
      report.sha256_after = fingerprint('after'); const after = fs.fstatSync(fd);
      report.source_hash_preserved = report.sha256_before === expectedHash && report.sha256_after === expectedHash;
      report.source_stat_stable = after.isFile() && after.size === before.size && after.dev === before.dev && after.ino === before.ino && after.mtimeMs === before.mtimeMs && after.ctimeMs === before.ctimeMs;
      assert(report.source_hash_preserved && report.source_stat_stable);
    }} catch (e) {failure = failure || e; report.status = 'failed'; report.after_failure = String(e);}
    try {fs.closeSync(fd); report.source_closed = true;} catch (e) {failure = failure || e; report.status = 'failed'; report.close_failure = String(e);}
  }
  report.finished_utc = new Date().toISOString(); const b = Buffer.from(JSON.stringify(report, null, 2) + '\n'); assert(b.length <= limits.report_bytes);
  fs.writeFileSync(path.join(output, 'metadata.json'), b, {flag: 'wx'});
}
console.log(JSON.stringify({status: report.status, metadata_bytes: report.metadata_bytes, bindings: report.bindings, output: 'docs/object-lifecycle/offline-actor-matrix-lock-imports/metadata.json'}));
if (failure) process.exitCode = 1;
