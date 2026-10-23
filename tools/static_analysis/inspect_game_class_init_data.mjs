// Prepared fixed metadata lookup; execution is a separate action.
// Only two fixed Game literals and two inventory-derived imports are resolved.
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
const proofPath = 'docs/object-lifecycle/offline-questitem-boundaries/boundary.json';
const proofHash = 'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05';
const inventoryPath = 'docs/phase-1/boundary-audit/module-boundaries.json';
const inventoryHash = 'd1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7';
const directoryFieldOffset = 296 + 112 + 8;
const targets = Object.freeze([
  {descriptor_index: 3, module: 'Engine.dll', thunk_index: 470,
    name: '?classInfo@Actor@GAME@@1VRTTI_ClassInfo@2@B', expected_iat_rva: 0x609eb0},
  {descriptor_index: 9, module: 'api-ms-win-crt-string-l1-1-0.dll', thunk_index: 2,
    name: '_strdup', expected_iat_rva: 0x60ba38},
]);
const literals = Object.freeze([
  {rva: 0x66a34c, source_instruction_va: '0x180005efd'},
  {rva: 0x66a9c8, source_instruction_va: '0x180009e16'},
]);
const sourceProofs = Object.freeze([
  {
    path: 'docs/object-lifecycle/offline-class-static-table-batch-4/5ef0-instructions.json',
    sha256: 'd65d8ac724d67af4a25ea5f283dad8b2f6397c6906d961e64e610cc4baae95d1',
    report_path: 'docs/object-lifecycle/offline-class-static-table-batch-4/inspection.json',
    report_sha256: 'b792b1858bdeba01dbf1b76e2fd9ead2d8d2576b407dcc5d28203a03829b08fe',
    start_rva: '0x5ef0', references: [
      {address: '0x180005ef6', bytes: '488b05b33f6000', target: 0x609eb0, displacement_offset: 3},
      {address: '0x180005efd', bytes: '488d0d48446600', target: 0x66a34c, displacement_offset: 3},
      {address: '0x180005f0b', bytes: 'ff15275b6000', target: 0x60ba38, displacement_offset: 2},
    ],
  },
  {
    path: 'docs/object-lifecycle/offline-class-static-table-batch-7/9e10-instructions.json',
    sha256: 'c92322c0a51d234f9868b63bea8e076b685f2ba587ea9697ea5ec50999c87ba2',
    report_path: 'docs/object-lifecycle/offline-class-static-table-batch-7/inspection.json',
    report_sha256: '0074c19190c7ef08f13c6897153964ecb6b159d1ab1bb10b1355113cb5987871',
    start_rva: '0x9e10', references: [
      {address: '0x180009e16', bytes: '488d0dab0b6600', target: 0x66a9c8, displacement_offset: 3},
      {address: '0x180009e1d', bytes: 'ff15151c6000', target: 0x60ba38, displacement_offset: 2},
    ],
  },
]);
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-game-class-init-data');
const output = path.join(outputDirectory, 'metadata.json');
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536, metadata_read_bytes: 1024,
  metadata_single_read_bytes: 64, metadata_read_operations: 140,
  literal_bytes_including_nul: 64, literal_count: 2,
  saved_evidence_bytes: 2 * 1024 * 1024, report_bytes: 128 * 1024,
});
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = value => `0x${value.toString(16)}`;
const within = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && offset <= size && length <= size - offset;
function loadProof(relativePath, expected) {
  const file = path.join(root, relativePath);
  assert(fs.statSync(file).size <= limits.saved_evidence_bytes, 'Saved evidence size limit');
  const bytes = fs.readFileSync(file);
  assert.equal(hash(bytes), expected, 'Pinned project evidence changed');
  return JSON.parse(bytes);
}
const proof = loadProof(proofPath, proofHash);
assert.equal(proof.status, 'questitem-ctor-read-boundaries-and-classinfo-assessed');
assert.equal(proof.target, target);
assert.equal(proof.source_bytes, expectedBytes);
assert.equal(proof.sha256_before, expectedHash);
assert.equal(proof.sha256_after, expectedHash);
assert.equal(proof.source_hash_preserved, true);
assert.equal(proof.pe.pe_header_file_offset, 272);
assert.equal(proof.pe.image_base, hex(imageBase));
const optionalReads = proof.metadata_reads.filter(item => item.kind === 'optional-header-prefix');
assert.equal(optionalReads.length, 1);
assert.equal(optionalReads[0].file_offset, 296);
assert.equal(optionalReads[0].read_bytes, 144);
assert(within(directoryFieldOffset - 296, 8, optionalReads[0].read_bytes));
const inventory = loadProof(inventoryPath, inventoryHash);
const game = inventory.filter(item => item.relativePath === 'x64/Game.dll');
assert.equal(game.length, 1);
assert.equal(game[0].sha256, expectedHash);
assert.equal(game[0].bytes, expectedBytes);
for (const item of targets) {
  assert.equal(game[0].importModules[item.descriptor_index].module, item.module);
  assert.equal(game[0].importModules[item.descriptor_index].symbols[item.thunk_index], item.name);
}
for (const saved of sourceProofs) {
  const instructions = loadProof(saved.path, saved.sha256);
  const report = loadProof(saved.report_path, saved.report_sha256);
  assert.equal(report.status, 'fixed-game-init-fragments-verified');
  assert.equal(report.original, target);
  assert.equal(report.original_sha256_before, expectedHash);
  assert.equal(report.original_sha256_after, expectedHash);
  assert.equal(report.original_hash_matches, true);
  const fragments = report.fragments.filter(item => item.start_rva === saved.start_rva);
  assert.equal(fragments.length, 1);
  assert.equal(fragments[0].byte_coverage_verified, true);
  assert.equal(fragments[0].tool_exit, 0);
  for (const reference of saved.references) {
    const matches = instructions.filter(item => item.address === reference.address);
    assert.equal(matches.length, 1, 'Require one fixed saved source instruction');
    assert.equal(matches[0].bytes, reference.bytes, 'Saved RIP-relative reference changed');
    const bytes = Buffer.from(reference.bytes, 'hex');
    const referredVa = BigInt(reference.address) + BigInt(bytes.length) +
      BigInt(bytes.readInt32LE(reference.displacement_offset));
    assert.equal(referredVa, imageBase + BigInt(reference.target), 'Reference target mismatch');
  }
}
const sections = proof.pe.sections;
assert.equal(sections.length, 9);
function mapping(rva, length) {
  assert(within(rva, length, 0x100000000), 'RVA range outside uint32 domain');
  const matches = sections.filter(section => rva >= section.rva &&
    within(rva - section.rva, length, section.raw_size));
  assert.equal(matches.length, 1, 'Require unique saved file-backed section');
  const section = matches[0], fileOffset = section.raw_offset + rva - section.rva;
  assert(within(fileOffset, length, expectedBytes), 'Metadata mapping exceeds fixed file');
  assert.equal(section.characteristics & 0x20000000, 0, 'Do not read executable section');
  return {fileOffset, section: section.name};
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
  schema: 'grim-dynasty-fixed-game-class-init-data-v1',
  started_utc: new Date().toISOString(), status: 'started', target,
  expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: [
    {path: inventoryPath, sha256: inventoryHash}, {path: proofPath, sha256: proofHash},
    ...sourceProofs.flatMap(item => [
      {path: item.path, sha256: item.sha256}, {path: item.report_path, sha256: item.report_sha256},
    ]),
  ],
  requested_imports: targets,
  requested_literals: literals.map(item => ({...item, rva: hex(item.rva)})),
  import_directory_field_file_offset: directoryFieldOffset,
  limits, target_open_mode: 'O_RDONLY', target_loaded_or_executed: false,
  code_bytes_read: 0, code_bytes_interpreted: 0, iat_payload_bytes_read: 0,
  other_import_descriptors_read: 0, other_import_thunks_read: 0,
  initializer_table_bytes_read: 0, backend_started: false, subprocesses: 0,
  metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [], bindings: [], literals: [],
  limitations: [
    'Only the eight-byte normal import-directory field, descriptors 3/9, their exact module names, OFT thunks 470/2 and their exact hint/name records are semantically read for import resolution.',
    'The only literal reads begin at RVAs 0x66a34c and 0x66a9c8, one byte at a time through the first NUL, at most 64 bytes including NUL each. Contents are not assumed; no next byte is read after NUL.',
    'Descriptor/thunk indices come from the hash-pinned inventory, whose generator preserved original descriptor/thunk order. No descriptors or thunk lists are scanned.',
    'PE section mappings are reused from hash-pinned evidence; metadata reads are restricted to file-backed non-executable sections except the fixed optional-header field.',
    'IAT RVAs are FirstThunk plus eight times the verified thunk index. IAT contents and imported code are not read or executed.',
    'Literal bytes and import bindings do not establish allocator success, runtime DLL/data resolution, imported-function behavior or a fully initialized class tree.',
    'Whole-file 64KiB hash passes traverse bytes without interpretation. O_RDONLY and same-handle stat/hash checks do not guarantee an atomic snapshot or exclusive Windows FileShare.Read.',
  ],
};
const fingerprintBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
let fd, initialStat, admittedSize = false, failure;
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes, 'File size changed before hashing');
  const digest = createHash('sha256');
  let at = 0;
  while (at < expectedBytes) {
    const count = fs.readSync(fd, fingerprintBuffer, 0,
      Math.min(fingerprintBuffer.length, expectedBytes - at), at);
    assert(count > 0, 'Unexpected EOF during fingerprint');
    digest.update(fingerprintBuffer.subarray(0, count));
    at += count;
  }
  const sha256 = digest.digest('hex');
  result.fingerprint_passes.push({label, bytes: at, chunk_bytes: fingerprintBuffer.length, sha256});
  return sha256;
}
function readAt(fileOffset, length, kind, rva = null) {
  assert(within(fileOffset, length, expectedBytes));
  assert(length <= limits.metadata_single_read_bytes, 'Single metadata read limit');
  assert(result.metadata_bytes + length <= limits.metadata_read_bytes, '1KiB metadata limit');
  assert(result.metadata_reads.length < limits.metadata_read_operations, 'Metadata operation limit');
  if (rva === null) {
    assert.equal(kind, 'normal-import-directory');
    assert.equal(fileOffset, directoryFieldOffset);
    assert.equal(length, 8);
    assert.equal(result.metadata_reads.length, 0);
  } else assert.equal(mapping(rva, length).fileOffset, fileOffset);
  const operation = {kind, rva: rva === null ? null : hex(rva), file_offset: fileOffset,
    requested_bytes: length, read_bytes: 0};
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
function readRva(rva, length, kind) {
  return readAt(mapping(rva, length).fileOffset, length, kind, rva);
}
function assertExactName(bytes, offset, expected) {
  const expectedBytes = Buffer.from(expected, 'ascii');
  assert.equal(bytes.length, offset + expectedBytes.length + 1);
  assert(bytes.subarray(offset, offset + expectedBytes.length).equals(expectedBytes), 'Import name mismatch');
  assert.equal(bytes[bytes.length - 1], 0, 'Import name must end at the fixed expected NUL');
}
function readLiteral(selected) {
  assert(literals.includes(selected), 'Only the two fixed literal starts are allowed');
  const bytes = [];
  const assessment = {
    rva: hex(selected.rva), preferred_va: hex(imageBase + BigInt(selected.rva)),
    file_offset: mapping(selected.rva, 1).fileOffset,
    source_instruction_va: selected.source_instruction_va,
    status: 'started', nul_terminated: false, bytes_read: 0,
  };
  result.literals.push(assessment);
  try {
    for (let i = 0; i < limits.literal_bytes_including_nul; i++) {
      const next = readRva(selected.rva + i, 1, `fixed-literal-${hex(selected.rva)}-byte-${i}`)[0];
      bytes.push(next);
      assessment.bytes_read++;
      if (next === 0) {
        const payload = Buffer.from(bytes.slice(0, -1));
        assessment.status = 'nul-terminated-literal-read';
        assessment.nul_terminated = true;
        assessment.payload_bytes = payload.length;
        assessment.payload_hex = payload.toString('hex');
        assessment.ascii_if_printable = payload.every(value => value >= 0x20 && value <= 0x7e) ?
          payload.toString('ascii') : null;
        return;
      }
    }
    assessment.status = 'no-nul-within-fixed-64-byte-budget';
    throw new Error('Fixed literal has no NUL in its 64-byte budget; do not extend');
  } finally {
    assessment.raw_bytes_including_nul_if_present = Buffer.from(bytes).toString('hex');
    if (assessment.status === 'started') assessment.status = 'failed';
  }
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase(),
    'Unexpected fixed Game redirection');
  fd = fs.openSync(target, fs.constants.O_RDONLY);
  initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile(), 'Target must be a regular file');
  assert.equal(initialStat.size, expectedBytes, 'Fixed Game size mismatch');
  admittedSize = true;
  result.sha256_before = fingerprint('before');
  assert.equal(result.sha256_before, expectedHash, 'Game hash mismatch: stop before metadata');
  const directory = readAt(directoryFieldOffset, 8, 'normal-import-directory');
  const directoryRva = directory.readUInt32LE(0), directorySize = directory.readUInt32LE(4);
  assert(directoryRva > 0 && within(directoryRva, directorySize, 0x100000000));
  result.import_directory = {rva: hex(directoryRva), size: directorySize};
  result.descriptors = [];
  for (const item of targets) {
    const descriptorIndex = item.descriptor_index, moduleName = item.module;
    assert(within(descriptorIndex * 20, 20, directorySize), 'Selected descriptor outside import directory');
    const descriptorRva = directoryRva + descriptorIndex * 20;
    const descriptor = readRva(descriptorRva, 20, `fixed-import-descriptor-${descriptorIndex}`);
    const oft = descriptor.readUInt32LE(0), nameRva = descriptor.readUInt32LE(12);
    const firstThunk = descriptor.readUInt32LE(16);
    assert(oft > 0 && nameRva > 0 && firstThunk > 0, 'Require original named-thunk table; no fallback');
    const moduleBytes = readRva(nameRva, moduleName.length + 1, `exact-module-name-${descriptorIndex}`);
    assertExactName(moduleBytes, 0, moduleName);
    result.descriptors.push({
      index: descriptorIndex, rva: hex(descriptorRva), file_offset: mapping(descriptorRva, 20).fileOffset,
      raw_bytes: descriptor.toString('hex'), original_first_thunk_rva: hex(oft),
      name_rva: hex(nameRva), module_name: moduleName, first_thunk_rva: hex(firstThunk),
    });
    const thunkRva = oft + item.thunk_index * 8;
    const thunk = readRva(thunkRva, 8, `fixed-oft-thunk-${item.thunk_index}`);
    const namePointer = thunk.readBigUInt64LE(0);
    assert(namePointer > 0n && namePointer <= 0xffffffffn,
      'Require IMAGE_IMPORT_BY_NAME RVA; ordinal/bound-pointer forms are not followed');
    const importNameRva = Number(namePointer);
    const named = readRva(importNameRva, 2 + item.name.length + 1, `exact-hint-name-${item.name}`);
    assertExactName(named, 2, item.name);
    const iatRva = firstThunk + item.thunk_index * 8, iatMapping = mapping(iatRva, 8);
    assert.equal(iatRva, item.expected_iat_rva, 'IAT differs from the fixed caller/thunk evidence');
    result.bindings.push({
      module: moduleName, name: item.name, descriptor_index: descriptorIndex,
      thunk_index: item.thunk_index, original_thunk_rva: hex(thunkRva),
      original_thunk_file_offset: mapping(thunkRva, 8).fileOffset, raw_thunk_bytes: thunk.toString('hex'),
      import_by_name_rva: hex(importNameRva), import_by_name_file_offset: mapping(importNameRva, named.length).fileOffset,
      raw_hint_name_bytes: named.toString('hex'), hint: named.readUInt16LE(0),
      iat_rva: hex(iatRva), preferred_iat_va: hex(imageBase + BigInt(iatRva)),
      iat_file_offset: iatMapping.fileOffset, iat_section: iatMapping.section,
      iat_payload_bytes_read: 0, code_bytes_read: 0,
    });
  }
  assert.equal(result.bindings.length, 2);
  assert.equal(literals.length, limits.literal_count);
  for (const selected of literals) readLiteral(selected);
  result.status = 'fixed-game-class-init-data-resolved';
} catch (error) {
  failure = error;
  result.status = 'failed';
  result.failure = String(error);
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
        assert(result.source_stat_stable && result.source_hash_preserved, 'Game hash or stat changed');
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
  const encoded = `${JSON.stringify(result, null, 2)}\n`;
  assert(Buffer.byteLength(encoded) <= limits.report_bytes, 'Report size limit');
  fs.mkdirSync(outputDirectory);
  fs.writeFileSync(output, encoded, {flag: 'wx'});
}
console.log(JSON.stringify({status: result.status, output, metadata_bytes: result.metadata_bytes,
  source_hash_preserved: result.source_hash_preserved, bindings: result.bindings, literals: result.literals}));
if (failure) process.exitCode = 1;
