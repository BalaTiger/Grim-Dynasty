// PREPARED ONLY. Fixed Engine initialization tables; separately reviewed execution.
// No code/header/import reads, target following, subprocess, compilation or DLL load.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_initialization_tables.mjs').toLowerCase());
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedBytes = 4485632, imageBase = 0x180000000n;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-actor-initialization-tables');
const limits = Object.freeze({semantic_bytes:80, single_semantic_read_bytes:16, semantic_read_operations:9,
  init_e_bytes:16, tls_callback_slots:8, tls_callback_bytes:64, fingerprint_chunk_bytes:65536,
  proof_bytes_each:65536, report_bytes:65536});
const sources = Object.freeze([
  {path:'docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json',
    sha256:'39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'},
  {path:'docs/object-lifecycle/offline-actor-matrix-sync-dependencies/inspection.json',
    sha256:'5941d7ab01b2c2bd96957c8eeb369cb033aa79a256a54d371e026e9b870045c3'},
  {path:'docs/object-lifecycle/offline-engine-root-initializer-table/table.json',
    sha256:'29d059bdb204e1116c6a856537267152d54848c0fd8fc82fefc43914bbf4fa6e'},
  {path:'docs/object-lifecycle/offline-engine-root-static-crt-attach/2dcd48-instructions.json',
    sha256:'4f306451fb2a46246081c0bb65b68280c9f1367d941de8982652a80d4bd82150'},
  {path:'docs/object-lifecycle/offline-engine-root-static-crt-attach/inspection.json',
    sha256:'f4b4a7a8d1fb008f1d7f216b4549dff6d6389c9903609c128a7bc62cff9eaea7'},
]);
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
const fits = (at, n, size) => Number.isSafeInteger(at) && at >= 0 && Number.isSafeInteger(n) &&
  n > 0 && Number.isSafeInteger(size) && at <= size && n <= size - at;
function normal(p, file = false) {
  let first = true;
  for (let current = path.resolve(p);;) {
    const s = fs.lstatSync(current); assert(!s.isSymbolicLink(), 'Redirected path refused');
    assert(first && file ? s.isFile() : s.isDirectory(), 'Unexpected path type');
    assert.equal(fs.realpathSync(current).toLowerCase(), current.toLowerCase(), 'Redirected ancestor refused');
    const parent = path.dirname(current); if (parent === current) return; current = parent; first = false;
  }
}
function absent(p) {
  try { fs.lstatSync(p); } catch (e) { if (e.code === 'ENOENT') return; throw e; }
  assert.fail('Existing evidence path refused');
}
normal(script, true); normal(path.dirname(outputDirectory)); absent(outputDirectory);
const proofs = sources.map(source => {
  const p = path.join(root, source.path); normal(p, true);
  assert(fs.statSync(p).size <= limits.proof_bytes_each);
  const bytes = fs.readFileSync(p); assert(bytes.length <= limits.proof_bytes_each);
  assert.equal(hash(bytes), source.sha256, 'Pinned project proof changed'); return JSON.parse(bytes.toString('utf8'));
});
const [peProof, tlsProof, tableProof, callerRows, callerProof] = proofs, pe = peProof.pe;
for (const p of [peProof, tlsProof]) {
  assert.equal(p.original, target); assert.equal(p.expected_sha256, expectedHash);
  assert.equal(p.sha256_before, expectedHash); assert.equal(p.sha256_after, expectedHash);
  assert(p.source_stat_stable && p.source_hash_preserved); assert.deepEqual(p.failures, []);
}
assert.equal(peProof.status, 'exact-actor-export-and-boundary-verified');
assert.equal(tlsProof.status, 'fixed-actor-matrix-sync-dependencies-verified'); assert(tlsProof.source_handle_closed);
assert.equal(pe.image_base, hex(imageBase)); assert.equal(pe.image_size, 4521984);
assert.equal(tableProof.status, 'fixed-engine-initializer-table-and-crt-bindings-read');
assert.equal(tableProof.target, target); assert.equal(tableProof.expected_sha256, expectedHash);
assert.equal(tableProof.expected_file_bytes, expectedBytes);
assert.equal(tableProof.sha256_before, expectedHash); assert.equal(tableProof.sha256_after, expectedHash);
assert(tableProof.source_hash_preserved && tableProof.source_stat_stable && tableProof.source_closed);
for (const source of [sources[3], sources[4]]) {
  const saved = tableProof.source_evidence.filter(x => x.path === source.path);
  assert.equal(saved.length, 1); assert.equal(saved[0].sha256, source.sha256);
}
assert.equal(callerProof.status, 'fixed-engine-root-fragments-verified');
assert.equal(callerProof.original, target); assert.equal(callerProof.expected_sha256, expectedHash);
assert.equal(callerProof.original_sha256_before, expectedHash); assert.equal(callerProof.original_sha256_after, expectedHash);
assert(callerProof.original_stat_stable && callerProof.original_hash_matches && callerProof.original_closed);
assert.deepEqual(callerProof.failures, []);
function relative(va, bytes, instruction, displacementAt, expectedRva) {
  const matches = callerRows.filter(r => r.address === va); assert.equal(matches.length, 1);
  assert.equal(matches[0].bytes, bytes); assert.equal(matches[0].instruction, instruction);
  const b = Buffer.from(bytes, 'hex');
  assert.equal(BigInt(va) + BigInt(b.length) + BigInt(b.readInt32LE(displacementAt)), imageBase + BigInt(expectedRva));
  return matches[0];
}
const initCaller = [
  relative('0x1802dcdd0', '488d15b1630200', 'lea         rdx,[0000000180303188h]', 3, 0x303188),
  relative('0x1802dcdd7', '488d0d9a630200', 'lea         rcx,[0000000180303178h]', 3, 0x303178),
  relative('0x1802dcdde', 'e8d30d0000', 'call        00000001802DDBB6', 1, 0x2ddbb6),
];
const thunks = tableProof.thunks.filter(t => t.address === '0x1802ddbb6'); assert.equal(thunks.length, 1);
assert.equal(thunks[0].bytes, 'ff25944e0200');
assert.equal(thunks[0].instruction, 'jmp         qword ptr [0000000180302A50h]');
assert.equal(BigInt(thunks[0].address) + 6n + BigInt(Buffer.from(thunks[0].bytes, 'hex').readInt32LE(2)), imageBase + 0x302a50n);
const bindings = tableProof.bindings.filter(b => b.iat_rva === '0x302a50'); assert.equal(bindings.length, 1);
assert.equal(bindings[0].name, '_initterm_e'); assert.equal(bindings[0].module, 'api-ms-win-crt-runtime-l1-1-0.dll');
const directory = tlsProof.tls_directory;
assert.equal(directory.rva, '0x35d468'); assert.equal(directory.address_of_callbacks, '0x180303198');
const directoryBytes = Buffer.from(directory.raw_hex, 'hex'); assert.equal(directoryBytes.length, 40);
assert.equal(directoryBytes.readBigUInt64LE(24), imageBase + 0x303198n); assert.equal(directory.callbacks_followed, false);
const tlsReads = tlsProof.reads.filter(r => r.kind === 'tls-metadata' && r.file_offset === directory.file_offset);
assert.equal(tlsReads.length, 1); assert.equal(tlsReads[0].read_bytes, 40); assert.equal(tlsReads[0].raw_hex, directory.raw_hex);
const initBegin = 0x303178, initEnd = 0x303188, tlsBegin = 0x303198;
assert.equal(initEnd - initBegin, limits.init_e_bytes);
function mapping(rva, n) {
  assert(fits(rva, n, pe.image_size));
  const matches = pe.sections.filter(s => rva >= s.rva && fits(rva - s.rva, n, s.virtual_size));
  assert.equal(matches.length, 1, 'Require unique section'); const s = matches[0], delta = rva - s.rva;
  assert.equal(s.name, '.rdata'); assert(!(s.characteristics & 0x20000000)); assert(s.characteristics & 0x40000000);
  assert(fits(delta, n, s.raw_size), 'No virtual or partial file span');
  const offset = s.raw_offset + delta; assert(fits(offset, n, expectedBytes)); return offset;
}
mapping(initBegin, limits.init_e_bytes); mapping(tlsBegin, limits.tls_callback_bytes);
const report = {schema:'grim-dynasty-fixed-actor-initialization-tables-v1', status:'started', started_utc:new Date().toISOString(),
  script_sha256:hash(fs.readFileSync(script)), target, expected_sha256:expectedHash, expected_file_bytes:expectedBytes,
  source_evidence:sources, limits, init_e_caller:initCaller, init_e_thunk:thunks[0], init_e_binding:bindings[0],
  tls_callbacks_source:{directory_rva:directory.rva, directory_raw_hex:directory.raw_hex,
    address_of_callbacks:directory.address_of_callbacks},
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, subprocesses:0, backend_started:false,
  compilation_or_tests:false, original_code_semantic_reads:0, new_header_bytes_read:0, new_import_bytes_read:0,
  target_payloads_followed:false, semantic_bytes:0, semantic_reads:[], fingerprint_passes:[],
  init_e:{begin_rva:hex(initBegin), end_exclusive_rva:hex(initEnd), slots:[], complete:false},
  tls_callbacks:{begin_rva:hex(tlsBegin), maximum_slots:limits.tls_callback_slots, slots:[], terminated:false},
  limitations:[
    'Only the fixed init_e half-open 16B span and at most eight 8B TLS callback slots are read. TLS reading stops at the first zero slot; no terminator within 64B is an insufficiency, not permission to extend.',
    'Pointers are recorded as file-image values, including zero entries in init_e. No target, relocation, callback code, ordering dependency or initializer return behavior is followed or executed.',
    'PE mapping, TLS directory and _initterm_e identity are reused from hash-pinned project evidence; no header or import metadata is reread.',
    'Two whole-file hashes traverse bytes without code interpretation using one 64KiB buffer. Stable same-handle stat/hash checks are not an atomic snapshot or an exclusive Windows FileShare.Read guarantee.',
  ]};
let fd, initialStat, admittedSize = false; const failures = [];
const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes);
  const digest = createHash('sha256'); let at = 0;
  while (at < expectedBytes) {
    const got = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, expectedBytes - at), at);
    assert(got > 0, 'Unexpected fingerprint EOF'); digest.update(hashBuffer.subarray(0, got)); at += got;
  }
  const sha256 = digest.digest('hex'); report.fingerprint_passes.push({label, bytes:at, chunk_bytes:hashBuffer.length, sha256});
  return sha256;
}
function read(rva, n, kind) {
  assert((kind === 'init_e' && rva === initBegin && n === 16) ||
    (kind === 'tls_callback' && n === 8 && rva >= tlsBegin && rva < tlsBegin + 64 && (rva - tlsBegin) % 8 === 0));
  assert(n <= limits.single_semantic_read_bytes && report.semantic_bytes + n <= limits.semantic_bytes);
  assert(report.semantic_reads.length < limits.semantic_read_operations);
  const offset = mapping(rva, n), buffer = Buffer.alloc(n);
  const record = {kind, rva:hex(rva), file_offset:offset, requested_bytes:n, read_bytes:0, raw_hex:''};
  report.semantic_reads.push(record); report.semantic_bytes += n;
  const got = fs.readSync(fd, buffer, 0, n, offset); record.read_bytes = got; record.raw_hex = buffer.subarray(0, got).toString('hex');
  assert.equal(got, n, 'Short semantic read; no retry'); return {buffer, offset};
}
function slot(bytes, at, rva, fileOffset, index) {
  const value = bytes.readBigUInt64LE(at);
  return {index, slot_rva:hex(rva), file_offset:fileOffset, raw_hex:bytes.subarray(at, at + 8).toString('hex'),
    pointer_hex:hex(value), is_null:value === 0n, target_followed:false};
}
try {
  normal(target, true); fd = fs.openSync(target, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile()); assert.equal(initialStat.size, expectedBytes); admittedSize = true;
  report.stat_before = {size:initialStat.size, dev:initialStat.dev, ino:initialStat.ino, mtime_ms:initialStat.mtimeMs, ctime_ms:initialStat.ctimeMs};
  report.sha256_before = fingerprint('before'); assert.equal(report.sha256_before, expectedHash, 'Hash mismatch: no semantic reads');
  const init = read(initBegin, 16, 'init_e'); report.init_e.raw_hex = init.buffer.toString('hex');
  for (let i = 0; i < 2; i++) report.init_e.slots.push(slot(init.buffer, i * 8, initBegin + i * 8, init.offset + i * 8, i));
  report.init_e.complete = true;
  for (let i = 0; i < limits.tls_callback_slots; i++) {
    const rva = tlsBegin + i * 8, item = read(rva, 8, 'tls_callback');
    const observed = slot(item.buffer, 0, rva, item.offset, i); report.tls_callbacks.slots.push(observed);
    if (observed.is_null) { report.tls_callbacks.terminated = true; break; }
  }
  if (!report.tls_callbacks.terminated) {
    report.insufficiency = 'No TLS callback terminator within eight slots; no extra slot or target read';
    report.status = 'bounded-tls-table-incomplete';
  } else report.status = 'fixed-actor-initialization-tables-read';
} catch (e) { failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        report.sha256_after = fingerprint('after'); const last = fs.fstatSync(fd);
        report.stat_after = {size:last.size, dev:last.dev, ino:last.ino, mtime_ms:last.mtimeMs, ctime_ms:last.ctimeMs};
        report.source_stat_stable = last.isFile() && initialStat.size === last.size && initialStat.ino === last.ino &&
          initialStat.dev === last.dev && initialStat.mtimeMs === last.mtimeMs && initialStat.ctimeMs === last.ctimeMs;
        report.source_hash_preserved = report.sha256_before === expectedHash && report.sha256_after === expectedHash;
        assert(report.source_stat_stable && report.source_hash_preserved, 'Source final verification failed');
      }
    } catch (e) { failures.push('Original final verification: ' + String(e)); }
    finally { try { fs.closeSync(fd); report.source_closed = true; } catch (e) { failures.push('Original close: ' + String(e)); } }
  }
  report.failures = failures; if (failures.length) report.status = 'failed'; report.finished_utc = new Date().toISOString();
  if (fd !== undefined && report.source_closed !== true) throw Error('Unconfirmed source close: no evidence write');
  normal(path.dirname(outputDirectory)); absent(outputDirectory);
  const bytes = Buffer.from(JSON.stringify(report, null, 2) + '\n', 'utf8'); assert(bytes.length <= limits.report_bytes);
  fs.mkdirSync(outputDirectory); normal(outputDirectory);
  fs.writeFileSync(path.join(outputDirectory, 'metadata.json'), bytes, {flag:'wx'});
}
console.log(JSON.stringify({status:report.status, semantic_bytes:report.semantic_bytes,
  init_e_slots:report.init_e.slots.length, tls_slots:report.tls_callbacks.slots.length,
  tls_terminated:report.tls_callbacks.terminated, source_hash_preserved:report.source_hash_preserved, failures}));
if (failures.length || !report.tls_callbacks.terminated) process.exitCode = 1;
