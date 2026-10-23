// PREPARED ONLY. Fixed Engine initializer-table metadata and two named imports.
// No build, DLL loading, code interpretation, callback execution or subprocess.
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
const tableStart = 0x302d48, tableEnd = 0x303170, tableBytes = 1064;
const descriptorIndex = 14, directoryRva = 0x415598, directorySize = 540;
const moduleName = 'api-ms-win-crt-runtime-l1-1-0.dll';
const imports = Object.freeze([
  {index: 5, name: '_initterm_e', iat: 0x302a50},
  {index: 6, name: '_initterm', iat: 0x302a58},
]);
const sources = Object.freeze([
  {path: 'docs/object-lifecycle/offline-depot-ctor-boundaries/boundary.json',
    sha256: '646053e4565e3f08c696ae0e9a16ddb5c2e81ab22ffe046c5057fc41aa64bb15'},
  {path: 'docs/phase-1/boundary-audit/module-boundaries.json',
    sha256: 'd1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-crt-attach/inspection.json',
    sha256: 'f4b4a7a8d1fb008f1d7f216b4549dff6d6389c9903609c128a7bc62cff9eaea7'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-crt-attach/2dcd48-instructions.json',
    sha256: '4f306451fb2a46246081c0bb65b68280c9f1367d941de8982652a80d4bd82150'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-initializer-thunks/inspection.json',
    sha256: '07efe567de1b0435f0f3a3184745fcb2fd9e8674ae9e129df3c328f5b61b9231'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-initializer-thunks/2ddbb0-instructions.json',
    sha256: '3c35fadbc9f0cf20921e572d14761013828f7bf97fe1b5bbf2b1b106d7a7446f'},
  {path: 'docs/object-lifecycle/offline-load-ticks-imports/tick-import-bindings.json',
    sha256: 'eb1e49dfe763e207be743b91d03b2b4c066057063da052cb3e1e3a97807aab0e'},
  {path: 'docs/object-lifecycle/offline-engine-root-static-initializer-thunks/2ddbb6-instructions.json',
    sha256: '8785acc63ebf178522171b600a088a41ecf6dfcf4d2342b21c1a5339d86de6e4'},
]);
const limits = Object.freeze({table_bytes: 1064, import_metadata_bytes: 96, total_metadata_bytes: 1160,
  metadata_reads: 6, single_import_read_bytes: 64, slots: 133,
  fingerprint_chunk_bytes: 65536, saved_proof_bytes_each: 2 * 1024 * 1024, report_bytes: 128 * 1024});
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-engine-root-initializer-table');
const hex = n => `0x${n.toString(16)}`;
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const within = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && offset <= size && length <= size - offset;
function proof(index) {
  const source = sources[index], file = path.join(root, source.path);
  assert(fs.statSync(file).size <= limits.saved_proof_bytes_each);
  const bytes = fs.readFileSync(file); assert.equal(hash(bytes), source.sha256, 'Pinned project evidence changed');
  return JSON.parse(bytes.toString('utf8'));
}
const peProof = proof(0), inventory = proof(1), attachProof = proof(2), caller = proof(3);
const thunkProof = proof(4), thunk = proof(5), directoryProof = proof(6), otherThunk = proof(7);
assert.equal(peProof.target, target); assert.equal(peProof.source_bytes, expectedBytes);
assert.equal(peProof.sha256_before, expectedHash); assert.equal(peProof.sha256_after, expectedHash);
assert(peProof.source_stat_stable && peProof.source_hash_preserved);
assert.equal(peProof.pe.image_base, hex(imageBase)); assert.equal(peProof.pe.pe_header_file_offset, 288);
assert(peProof.metadata_reads.some(x => x.kind === 'optional-header-prefix' &&
  x.file_offset === 312 && x.read_bytes >= 128));
assert.equal(directoryProof.status, 'fixed-two-tick-imports-confirmed');
assert.equal(directoryProof.original, target); assert.equal(directoryProof.expected_file_bytes, expectedBytes);
assert.equal(directoryProof.original_sha256_before, expectedHash);
assert.equal(directoryProof.original_sha256_after, expectedHash); assert(directoryProof.original_hash_matches);
assert.equal(Number(directoryProof.headers.import_directory_rva), directoryRva);
assert.equal(directoryProof.headers.import_directory_size, directorySize);
for (const evidence of [attachProof, thunkProof]) {
  assert.equal(evidence.status, 'fixed-engine-root-fragments-verified');
  assert.equal(evidence.original, target); assert.equal(evidence.expected_file_bytes, expectedBytes);
  assert.equal(evidence.original_sha256_before, expectedHash); assert.equal(evidence.original_sha256_after, expectedHash);
  assert(evidence.original_hash_matches && evidence.original_stat_stable && evidence.original_closed);
}
function instructionAt(address, bytes) {
  const matches = caller.filter(x => x.address === address);
  assert.equal(matches.length, 1); assert.equal(matches[0].bytes, bytes); return matches[0];
}
const callerProof = [
  instructionAt('0x1802dcdf0', '488d1579630200'),
  instructionAt('0x1802dcdf7', '488d0d4a5f0200'),
  instructionAt('0x1802dcdfe', 'e8ad0d0000'),
];
// Decode only archived instruction displacement bytes to verify the fixed metadata addresses.
assert.equal(0x2dcdf0 + 7 + Buffer.from(callerProof[0].bytes, 'hex').readInt32LE(3), tableEnd);
assert.equal(0x2dcdf7 + 7 + Buffer.from(callerProof[1].bytes, 'hex').readInt32LE(3), tableStart);
assert.equal(0x2dcdfe + 5 + Buffer.from(callerProof[2].bytes, 'hex').readInt32LE(1), 0x2ddbb0);
assert.equal(thunk.length, 1); assert.equal(thunk[0].address, '0x1802ddbb0');
assert.equal(thunk[0].bytes, 'ff25a24e0200');
assert.equal(0x2ddbb0 + 6 + Buffer.from(thunk[0].bytes, 'hex').readInt32LE(2), imports[1].iat);
assert.equal(otherThunk.length, 1); assert.equal(otherThunk[0].address, '0x1802ddbb6');
assert.equal(otherThunk[0].bytes, 'ff25944e0200');
assert.equal(0x2ddbb6 + 6 + Buffer.from(otherThunk[0].bytes, 'hex').readInt32LE(2), imports[0].iat);
assert.equal(tableEnd - tableStart, tableBytes); assert.equal(tableBytes / 8, limits.slots);
const engine = inventory.filter(x => x.relativePath === 'x64/Engine.dll');
assert.equal(engine.length, 1); assert.equal(engine[0].sha256, expectedHash);
assert.equal(engine[0].bytes, expectedBytes);
assert.equal(engine[0].importModules[descriptorIndex].module, moduleName);
for (const item of imports) assert.equal(engine[0].importModules[descriptorIndex].symbols[item.index], item.name);
const sections = peProof.pe.sections;
function mapping(rva, length) {
  assert(within(rva, length, 0x100000000));
  const matches = sections.filter(s => rva >= s.rva && within(rva - s.rva, length, s.raw_size));
  assert.equal(matches.length, 1, 'Require one file-backed section');
  const s = matches[0], offset = s.raw_offset + rva - s.rva;
  assert.equal(s.characteristics & 0x20000000, 0, 'No executable section reads');
  assert(within(offset, length, expectedBytes)); return {offset, section: s.name};
}
assert.equal(mapping(tableStart, tableBytes).section, '.rdata');
for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  const stat = fs.lstatSync(directory);
  assert(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real directory');
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(), 'Output ancestor redirected');
  if (directory === path.dirname(directory)) break;
}
fs.mkdirSync(outputDirectory); // No recursive creation, reuse, overwrite or alternate destination.
const report = {
  schema: 'grim-dynasty-fixed-engine-root-initializer-table-v1', status: 'started',
  started_utc: new Date().toISOString(), script_sha256: hash(fs.readFileSync(script)),
  target, expected_sha256: expectedHash, expected_file_bytes: expectedBytes, source_evidence: sources,
  caller: callerProof, thunks: [otherThunk[0], thunk[0]], limits, target_open_mode: 'O_RDONLY',
  reused_import_directory: {rva: hex(directoryRva), size: directorySize,
    source: sources[6], optional_header_file_offset: 312, directory_field_file_offset: 432,
    new_header_bytes_read: 0},
  descriptor_selection: {index: descriptorIndex, rva: '0x4156b0', inventory: sources[1],
    basis: 'Ordered inventory suggests descriptor 14/OFT 5,6; exact descriptor/name and both archived IAT addresses are independently verified before reading the table.'},
  target_loaded_or_executed: false, original_code_semantic_reads: 0, backend_started: false,
  subprocesses: 0, compilation_or_tests: false, target_payloads_followed: 0, iat_payload_bytes_read: 0,
  metadata_bytes: 0, import_metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [],
  table: {start_rva: hex(tableStart), end_rva_exclusive: hex(tableEnd), byte_count: tableBytes,
    slot_count: limits.slots, file_offset: mapping(tableStart, tableBytes).offset, slots: []},
  bindings: [],
  limitations: [
    'The fixed closed-open initializer-array range is proved by archived caller arguments and its six-byte thunk. New semantic reads contain only this 1064-byte table and the selected import metadata.',
    'The import-directory RVA/size come from the pinned tick-import metadata; its optional-header field is not read again. Descriptor 14 and OFT indices 5,6 come from the pinned ordered inventory.',
    'Exact module/names and FirstThunk+5*8 / +6*8 must bind to archived IATs 0x302a50 / 0x302a58 before reading the table. These new reads total exactly 96 bytes: descriptor20, module34, OFT16, hint/names14+12.',
    'All 133 stored pointer values, including null slots and ordering, are recorded. Pointer targets, callbacks, factories, relocation contents, TLS and other initialization arrays are not read or executed.',
    'Pointer values are preferred-image addresses on disk, not resolved live process pointers. Values outside the preferred image are recorded without following them.',
    'Archived instruction bytes are checked only to prove this fixed metadata request; no new original code bytes are interpreted.',
    'Whole-file before/after hashes use 64KiB streaming buffers. O_RDONLY and same-handle stat/hash checks do not imply an atomic snapshot or exclusive Windows FileShare.Read.',
  ],
};
let fd, initialStat, admittedSize = false, failure;
const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
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
function read(fileOffset, length, kind, rva) {
  assert(within(fileOffset, length, expectedBytes));
  assert(report.metadata_reads.length < limits.metadata_reads);
  assert(length <= limits.total_metadata_bytes - report.metadata_bytes, 'Total metadata budget');
  if (kind === 'fixed-initializer-table') {
    assert.equal(rva, tableStart); assert.equal(length, tableBytes); assert.equal(report.bindings.length, 2);
    assert(!report.metadata_reads.some(x => x.kind === kind), 'Table may only be read once');
  } else {
    assert(length <= limits.single_import_read_bytes);
    assert(length <= limits.import_metadata_bytes - report.import_metadata_bytes, 'Import metadata budget');
    report.import_metadata_bytes += length;
  }
  assert.equal(mapping(rva, length).offset, fileOffset);
  const op = {kind, rva: hex(rva), file_offset: fileOffset, requested_bytes: length, read_bytes: 0, raw_bytes: ''};
  report.metadata_reads.push(op); report.metadata_bytes += length;
  const bytes = Buffer.alloc(length);
  while (op.read_bytes < length) {
    const got = fs.readSync(fd, bytes, op.read_bytes, length - op.read_bytes, fileOffset + op.read_bytes);
    assert(got > 0, 'Unexpected metadata EOF'); op.read_bytes += got;
    op.raw_bytes = bytes.subarray(0, op.read_bytes).toString('hex');
  }
  return bytes;
}
const readRva = (rva, length, kind) => read(mapping(rva, length).offset, length, kind, rva);
function exactName(bytes, prefixBytes, expected) {
  assert.equal(bytes.length, prefixBytes + expected.length + 1);
  assert(bytes.subarray(prefixBytes, -1).equals(Buffer.from(expected, 'ascii')));
  assert.equal(bytes[bytes.length - 1], 0, 'Exact import name must terminate here');
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(), 'Fixed Engine path redirected');
  fd = fs.openSync(target, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile()); assert.equal(initialStat.size, expectedBytes); admittedSize = true;
  report.sha256_before = fingerprint('before'); assert.equal(report.sha256_before, expectedHash, 'No semantic reads after hash mismatch');
  assert(within(descriptorIndex * 20, 20, directorySize), 'Fixed descriptor outside directory');
  const descriptorRva = directoryRva + descriptorIndex * 20;
  assert.equal(descriptorRva, 0x4156b0);
  const descriptor = readRva(descriptorRva, 20, 'fixed-import-descriptor-14');
  const oft = descriptor.readUInt32LE(0), moduleRva = descriptor.readUInt32LE(12), firstThunk = descriptor.readUInt32LE(16);
  assert(oft > 0 && moduleRva > 0 && firstThunk > 0, 'Require original named-thunk table');
  for (const item of imports)
    assert.equal(firstThunk + item.index * 8, item.iat, 'Descriptor/thunk must match archived IAT before following OFT');
  exactName(readRva(moduleRva, moduleName.length + 1, 'exact-runtime-module-name'), 0, moduleName);
  const thunkStartRva = oft + 5 * 8, thunkBytes = readRva(thunkStartRva, 16, 'fixed-oft-thunks-5-through-6');
  for (const item of imports) {
    const namePointer = thunkBytes.readBigUInt64LE((item.index - 5) * 8);
    assert(namePointer > 0n && namePointer <= 0xffffffffn, 'No ordinal or bound-address fallback');
    const nameRva = Number(namePointer), named = readRva(nameRva, item.name.length + 3, `exact-hint-name-${item.name}`);
    exactName(named, 2, item.name);
    report.bindings.push({module: moduleName, name: item.name, descriptor_index: descriptorIndex,
      descriptor_rva: hex(descriptorRva), original_first_thunk_rva: hex(oft), thunk_index: item.index,
      original_thunk_rva: hex(oft + item.index * 8), hint_name_rva: hex(nameRva), hint: named.readUInt16LE(0),
      first_thunk_rva: hex(firstThunk), iat_rva: hex(item.iat), preferred_iat_va: hex(imageBase + BigInt(item.iat)),
      iat_payload_bytes_read: 0});
  }
  assert.equal(report.import_metadata_bytes, 96);
  const table = readRva(tableStart, tableBytes, 'fixed-initializer-table');
  report.table.sha256 = hash(table); report.table.raw_bytes = table.toString('hex');
  for (let index = 0; index < limits.slots; index++) {
    const pointer = table.readBigUInt64LE(index * 8), insideUInt32Image = pointer >= imageBase && pointer - imageBase <= 0xffffffffn;
    report.table.slots.push({index, slot_rva: hex(tableStart + index * 8),
      file_offset: report.table.file_offset + index * 8, raw_bytes: table.subarray(index * 8, index * 8 + 8).toString('hex'),
      stored_pointer: hex(pointer), is_null: pointer === 0n,
      preferred_rva_uint32: insideUInt32Image ? hex(pointer - imageBase) : null, target_followed: false});
  }
  report.table.non_null_slots = report.table.slots.filter(x => !x.is_null).length;
  report.status = 'fixed-engine-initializer-table-and-crt-bindings-read';
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
  report.finished_utc = new Date().toISOString();
  const bytes = Buffer.from(JSON.stringify(report, null, 2) + '\n', 'utf8');
  assert(bytes.length <= limits.report_bytes, 'Report budget');
  fs.writeFileSync(path.join(outputDirectory, 'table.json'), bytes, {flag: 'wx'});
}
console.log(JSON.stringify({status: report.status, metadata_bytes: report.metadata_bytes,
  slots: report.table.slots.length, bindings: report.bindings,
  output: path.relative(root, path.join(outputDirectory, 'table.json')).replaceAll('\\', '/')}));
if (failure) process.exitCode = 1;
