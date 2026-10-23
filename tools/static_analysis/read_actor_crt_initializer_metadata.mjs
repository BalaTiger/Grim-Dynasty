// PREPARED ONLY. Three fixed imports and four bounded literal operands from2DC908.
// No headers, code, target execution, subprocess or referenced callback following.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_crt_initializer_metadata.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', expectedBytes = 4485632;
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const base = 0x180000000n, output = path.join(root, 'docs/object-lifecycle/offline-actor-crt-initializer-metadata');
const limits = Object.freeze({metadata_bytes:75, string_bytes:128, string_bytes_each:32, semantic_bytes:203,
  semantic_read_operations:118, single_read_bytes:19, fingerprint_chunk_bytes:65536,
  saved_proof_bytes_each:1048576, report_bytes:65536});
const sources = [
  {path:'docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json',
    sha256:'39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'},
  {path:'docs/phase-1/boundary-audit/module-boundaries.json',
    sha256:'d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'},
  {path:'docs/object-lifecycle/offline-load-ticks-imports/tick-import-bindings.json',
    sha256:'eb1e49dfe763e207be743b91d03b2b4c066057063da052cb3e1e3a97807aab0e'},
  {path:'docs/object-lifecycle/offline-actor-crt-initializer/inspection.json',
    sha256:'c1eb9ae91463cd1b7304dfff290220bfe9e21eb8ccdd947433dd017a5245d9c0'},
  {path:'docs/object-lifecycle/offline-actor-crt-initializer/2dc908-instructions.json',
    sha256:'cae592eaadda08627d1b0f96f784685ee26532d36996524a6f250c1e10c1bc17'},
];
const importTargets = [
  {thunk:76, iat:0x3023d8, name:'GetModuleHandleW'},
  {thunk:47, iat:0x3022f0, name:'GetProcAddress'},
  {thunk:4, iat:0x302198, name:'CreateEventW'},
];
const importCallers = [
  {address:'0x1802dc932', bytes:'ff15a05a0200', iat:0x3023d8},
  {address:'0x1802dc94e', bytes:'ff159c590200', iat:0x3022f0},
  {address:'0x1802dc961', bytes:'ff1589590200', iat:0x3022f0},
  {address:'0x1802dc974', bytes:'ff1576590200', iat:0x3022f0},
  {address:'0x1802dc9fc', bytes:'ff1596570200', iat:0x302198},
];
const stringTargets = [
  {rva:0x307388, width:2, encoding:'UTF-16LE code units', address:'0x1802dc92b', bytes:'488d0d56aa0200', register:'rcx'},
  {rva:0x3073a8, width:1, encoding:'ASCII bytes', address:'0x1802dc944', bytes:'488d155daa0200', register:'rdx'},
  {rva:0x3073c8, width:1, encoding:'ASCII bytes', address:'0x1802dc954', bytes:'488d156daa0200', register:'rdx'},
  {rva:0x3073e8, width:1, encoding:'ASCII bytes', address:'0x1802dc967', bytes:'488d157aaa0200', register:'rdx'},
];
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
const fits = (at, n, size) => Number.isSafeInteger(at) && at >= 0 && Number.isSafeInteger(n) && n > 0 &&
  Number.isSafeInteger(size) && at <= size && n <= size - at;
function normal(p, file = false) {
  let first = true;
  for (let current = path.resolve(p);;) {
    const s = fs.lstatSync(current); assert(!s.isSymbolicLink(), 'Redirected path refused');
    assert(first && file ? s.isFile() : s.isDirectory());
    assert.equal(fs.realpathSync(current).toLowerCase(), current.toLowerCase(), 'Redirected ancestor refused');
    const parent = path.dirname(current); if (parent === current) return; current = parent; first = false;
  }
}
function absent(p) {
  try { fs.lstatSync(p); } catch (e) { if (e.code === 'ENOENT') return; throw e; }
  assert.fail('Existing evidence path refused');
}
normal(script, true); normal(path.dirname(output)); absent(output);
const proofs = sources.map(source => {
  const p = path.join(root, source.path); normal(p, true); assert(fs.statSync(p).size <= limits.saved_proof_bytes_each);
  const bytes = fs.readFileSync(p); assert(bytes.length <= limits.saved_proof_bytes_each); assert.equal(hash(bytes), source.sha256);
  return JSON.parse(bytes.toString('utf8'));
});
const [peProof, inventory, ticks, batch, rows] = proofs, pe = peProof.pe;
assert.equal(peProof.original, original); assert.equal(peProof.sha256_before, expectedHash); assert.equal(peProof.sha256_after, expectedHash);
assert(peProof.source_hash_preserved && peProof.source_stat_stable); assert.equal(pe.image_base, hex(base));
assert.equal(pe.image_size, 4521984); assert.deepEqual(peProof.failures, []);
assert.equal(batch.status, 'exact-actor-crt-initializer-verified'); assert.equal(batch.original, original);
assert.equal(batch.expected_sha256, expectedHash); assert.equal(batch.expected_file_bytes, expectedBytes);
assert.equal(batch.sha256_before, expectedHash); assert.equal(batch.sha256_after, expectedHash);
assert(batch.source_hash_preserved && batch.source_stat_stable && batch.source_closed && batch.byte_coverage_verified);
assert.equal(batch.code_bytes, 286); assert.equal(batch.instructions, 70); assert.equal(batch.tool_exit, 0); assert.deepEqual(batch.failures, []);
assert.equal(rows.length, 70);
const engines = inventory.filter(e => e.relativePath === 'x64/Engine.dll'); assert.equal(engines.length, 1);
assert.equal(engines[0].sha256, expectedHash); assert.equal(engines[0].bytes, expectedBytes);
assert.equal(engines[0].importModules[5].module, 'KERNEL32.dll');
for (const t of importTargets) assert.equal(engines[0].importModules[5].symbols[t.thunk], t.name);
function instruction(address, bytes) {
  const found = rows.filter(r => r.address === address); assert.equal(found.length, 1); assert.equal(found[0].bytes, bytes); return found[0];
}
for (const c of importCallers) {
  const saved = instruction(c.address, c.bytes), b = Buffer.from(c.bytes, 'hex');
  assert.equal(b.subarray(0, 2).toString('hex'), 'ff15');
  assert.equal(BigInt(c.address) + BigInt(b.length) + BigInt(b.readInt32LE(2)), base + BigInt(c.iat));
  c.saved_instruction = saved;
}
for (const s of stringTargets) {
  const saved = instruction(s.address, s.bytes), b = Buffer.from(s.bytes, 'hex');
  assert.equal(b.subarray(0, 3).toString('hex'), s.register === 'rcx' ? '488d0d' : '488d15');
  assert.equal(BigInt(s.address) + BigInt(b.length) + BigInt(b.readInt32LE(3)), base + BigInt(s.rva));
  s.saved_instruction = saved;
}
function mapping(rva, n) {
  assert(fits(rva, n, pe.image_size));
  const found = pe.sections.filter(s => rva >= s.rva && fits(rva - s.rva, n, s.virtual_size));
  assert.equal(found.length, 1); const s = found[0]; assert.equal(s.name, '.rdata');
  assert(!(s.characteristics & 0x20000000)); assert(s.characteristics & 0x40000000);
  assert(fits(rva - s.rva, n, s.raw_size)); const offset = s.raw_offset + rva - s.rva;
  assert(fits(offset, n, expectedBytes)); return offset;
}
assert.equal(ticks.status, 'fixed-two-tick-imports-confirmed'); assert.equal(ticks.original, original);
assert.equal(ticks.expected_file_bytes, expectedBytes); assert.equal(ticks.original_sha256_before, expectedHash);
assert.equal(ticks.original_sha256_after, expectedHash); assert(ticks.original_hash_matches);
assert.equal(Number(ticks.headers.import_directory_rva), 0x415598); assert.equal(ticks.headers.import_directory_size, 540);
const prior = ticks.normal_imports.filter(e => e.iat_va === '0x180302208'); assert.equal(prior.length, 1);
assert.equal(prior[0].descriptor_index, 5); assert.equal(prior[0].dll, 'KERNEL32.dll');
const raw = prior[0].raw_metadata, descriptor = Buffer.from(raw.descriptor.bytes, 'hex');
assert.equal(descriptor.length, 20); assert.equal(Number(raw.descriptor.rva), 0x415598 + 5 * 20);
assert.equal(raw.descriptor.file_offset, mapping(0x4155fc, 20));
assert.equal(descriptor.toString('hex'), '3059410000000000000000004a6d410078213000');
const oft = descriptor.readUInt32LE(0), moduleRva = descriptor.readUInt32LE(12), firstThunk = descriptor.readUInt32LE(16);
assert.equal(oft, 0x415930); assert.equal(firstThunk, 0x302178);
assert.equal(Number(raw.dll_name_with_nul.rva), moduleRva); assert.equal(raw.dll_name_with_nul.file_offset, mapping(moduleRva, 13));
assert.equal(raw.dll_name_with_nul.bytes, Buffer.from('KERNEL32.dll\0', 'ascii').toString('hex'));
for (const t of importTargets) assert.equal(firstThunk + t.thunk * 8, t.iat);
assert.equal(importTargets.reduce((sum, t) => sum + 8 + t.name.length + 3, 0), limits.metadata_bytes);
for (const s of stringTargets) mapping(s.rva, limits.string_bytes_each);
const report = {schema:'grim-dynasty-fixed-actor-crt-initializer-metadata-v1', status:'started', started_utc:new Date().toISOString(),
  script_sha256:hash(fs.readFileSync(script)), original, expected_sha256:expectedHash, expected_file_bytes:expectedBytes,
  source_evidence:sources, import_callers:importCallers, import_targets:importTargets, string_targets:stringTargets,
  reused_metadata:{source:sources[2], descriptor:raw.descriptor, module:raw.dll_name_with_nul},
  limits, metadata_bytes:0, string_bytes:0, semantic_bytes:0, semantic_reads:[], bindings:[], strings:[], fingerprint_passes:[],
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, code_bytes_read:0, iat_payload_bytes_read:0,
  header_bytes_read:0, new_descriptor_or_module_bytes:0, backend_started:false, subprocesses:0, build_or_tests:false,
  target_payloads_followed:false, permission_change:false,
  cached_binding_check:'Existing import reports contained no complete raw binding for these three slots. Reuse descriptor5/module raw bytes from ticks; read only the three missing OFT cells and exact inventory-named hint/name spans.',
  limitations:[
    'Only OFT4/47/76 and exact expected hint/names are read; no intervening import slot, IAT payload or header. Imported names establish identities, not resolution, handle validity or API behavior.',
    'Literal307388 is read in two-byte UTF16LE units; literals3073A8/3073C8/3073E8 in one-byte units. Each has a32B including-terminator bound and stops at the first zero unit. No adjacent scan or string-value prefill.',
    'Printable ASCII text is displayed only after termination. Raw bytes and code units are retained without guessing non-ASCII encoding or folding case.',
    'No2DDB20,2DD7F4,2DCA28,callback,cookie or cleanup target is followed. No target code or API is executed.',
    'Whole-file before/after SHA256 uses64KiB chunks separately from semantic budgets; stable same-handle stat/hash is not an atomic snapshot or exclusive FileShare.Read.',
  ]};
let fd, initialStat, admitted = false; const failures = [], buffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes); const digest = createHash('sha256'); let at = 0;
  while (at < expectedBytes) {
    const got = fs.readSync(fd, buffer, 0, Math.min(buffer.length, expectedBytes - at), at);
    assert(got > 0, 'Unexpected fingerprint EOF'); digest.update(buffer.subarray(0, got)); at += got;
  }
  const sha256 = digest.digest('hex'); report.fingerprint_passes.push({label, bytes:at, chunk_bytes:buffer.length, sha256}); return sha256;
}
function read(rva, n, category, purpose) {
  assert(n <= limits.single_read_bytes && report.semantic_bytes + n <= limits.semantic_bytes);
  assert(report.semantic_reads.length < limits.semantic_read_operations);
  if (category === 'import') assert(report.metadata_bytes + n <= limits.metadata_bytes);
  else { assert.equal(category, 'string'); assert(n === 1 || n === 2); assert(report.string_bytes + n <= limits.string_bytes); }
  const offset = mapping(rva, n), bytes = Buffer.alloc(n), entry = {category, purpose, rva:hex(rva),
    file_offset:offset, requested_bytes:n, read_bytes:0, raw_hex:''};
  report.semantic_reads.push(entry); report.semantic_bytes += n;
  if (category === 'import') report.metadata_bytes += n; else report.string_bytes += n;
  const got = fs.readSync(fd, bytes, 0, n, offset); entry.read_bytes = got; entry.raw_hex = bytes.subarray(0, got).toString('hex');
  assert.equal(got, n, 'Short semantic read; no retry'); return bytes;
}
try {
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile()); assert.equal(initialStat.size, expectedBytes); admitted = true;
  report.stat_before = {size:initialStat.size, ino:initialStat.ino, dev:initialStat.dev, mtime_ms:initialStat.mtimeMs, ctime_ms:initialStat.ctimeMs};
  report.sha256_before = fingerprint('before'); assert.equal(report.sha256_before, expectedHash, 'Hash mismatch: no semantic reads');
  for (const t of importTargets) {
    const cellRva = oft + t.thunk * 8, cell = read(cellRva, 8, 'import', 'OFT' + t.thunk);
    const pointer = cell.readBigUInt64LE(0); assert(pointer > 0n && pointer <= 0xffffffffn, 'No ordinal or bound-pointer fallback');
    const nameRva = Number(pointer), named = read(nameRva, t.name.length + 3, 'import', 'exact hint/name' + t.thunk);
    assert(named.subarray(2, -1).equals(Buffer.from(t.name, 'ascii'))); assert.equal(named[named.length - 1], 0);
    report.bindings.push({module:'KERNEL32.dll', name:t.name, descriptor_index:5, descriptor_rva:'0x4155fc',
      original_first_thunk_rva:hex(oft), first_thunk_rva:hex(firstThunk), thunk_index:t.thunk,
      original_thunk_rva:hex(cellRva), hint_name_rva:hex(nameRva), hint:named.readUInt16LE(0), iat_rva:hex(t.iat)});
  }
  assert.equal(report.metadata_bytes, 75);
  for (const s of stringTargets) {
    const item = {rva:hex(s.rva), unit_bytes:s.width, encoding:s.encoding, max_bytes:32,
      raw_hex:'', units:[], terminated:false, bytes_read:0, printable_ascii:null, ascii:null}; report.strings.push(item);
    for (let at = 0; at < limits.string_bytes_each; at += s.width) {
      const unit = read(s.rva + at, s.width, 'string', 'literal' + hex(s.rva));
      const value = s.width === 2 ? unit.readUInt16LE(0) : unit[0];
      item.raw_hex += unit.toString('hex'); item.units.push(value); item.bytes_read += s.width;
      if (value === 0) { item.terminated = true; break; }
    }
    assert(item.terminated, 'No NUL within fixed32B literal bound; no extension');
    const content = item.units.slice(0, -1); item.printable_ascii = content.every(c => c >= 0x20 && c <= 0x7e);
    if (item.printable_ascii) item.ascii = String.fromCharCode(...content);
  }
  report.status = 'fixed-actor-crt-initializer-metadata-read';
} catch (e) { failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (admitted) {
        report.sha256_after = fingerprint('after'); const last = fs.fstatSync(fd);
        report.stat_after = {size:last.size, ino:last.ino, dev:last.dev, mtime_ms:last.mtimeMs, ctime_ms:last.ctimeMs};
        report.source_stat_stable = last.isFile() && initialStat.size === last.size && initialStat.ino === last.ino &&
          initialStat.dev === last.dev && initialStat.mtimeMs === last.mtimeMs && initialStat.ctimeMs === last.ctimeMs;
        report.source_hash_preserved = report.sha256_before === expectedHash && report.sha256_after === expectedHash;
        assert(report.source_stat_stable && report.source_hash_preserved, 'Source final verification failed');
      }
    } catch (e) { failures.push('Original final verification: ' + String(e)); }
    finally { try { fs.closeSync(fd); report.source_closed = true; } catch (e) { failures.push('Original close: ' + String(e)); } }
  }
  report.failures = failures; if (failures.length) report.status = 'failed'; report.finished_utc = new Date().toISOString();
  assert(fd === undefined || report.source_closed === true, 'Source close unconfirmed');
  normal(path.dirname(output)); absent(output);
  const text = JSON.stringify(report, null, 2) + '\n'; assert(Buffer.byteLength(text, 'utf8') <= limits.report_bytes);
  fs.mkdirSync(output); normal(output); fs.writeFileSync(path.join(output, 'metadata.json'), text, {flag:'wx'});
}
console.log(JSON.stringify({status:report.status, metadata_bytes:report.metadata_bytes, string_bytes:report.string_bytes,
  semantic_bytes:report.semantic_bytes, bindings:report.bindings, strings:report.strings.map(s => ({rva:s.rva, ascii:s.ascii, terminated:s.terminated})),
  source_hash_preserved:report.source_hash_preserved, failures}));
if (failures.length) process.exitCode = 1;
