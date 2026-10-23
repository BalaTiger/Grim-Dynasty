// Fixed 284080 dependencies only: six-byte log import thunk and eight-byte
// multiplier. Never load/execute the target, its imports, or another backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2);
const script = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(script), '../..');
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const copy = path.join(root, '.tooling/offline-cache-node/Engine.readonly.dll');
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-binary-parser-log');
const output = path.join(outputDirectory, 'log-import-and-multiplier.json');
const proofs = [
  ['docs/object-lifecycle/rea-binary-parser-helpers/09-Parser-helper-0x180284080-assembly.json',
    '9e00cc98d04590c79eff8dbeadce59906abcc6f1923961daf0008cc1dd09a43e'],
  ['docs/object-lifecycle/rea-binary-parser-helpers/08-Parser-helper-0x180284080-pseudocode.json',
    'fa0b1ae47cf9dc9a329053d6befd5e13c5486a3624fc883df2ff8e0dd115f7c8'],
  ['docs/object-lifecycle/rea-binary-parser-entry-retry-2/05-Binary-parser-entry-0x18013f510-assembly.json',
    '799736dfda45d5eb59e0d4e45766e43c150f1ece2272f0a3961b551592a8b635']
];
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const sourceEvidence = proofs.map(([relative, digest]) => {
  const bytes = fs.readFileSync(path.join(root, relative));
  assert.equal(hash(bytes), digest, `Saved evidence changed: ${relative}`);
  const evidence = JSON.parse(bytes).result.structuredContent;
  assert.equal(evidence.subject.digest.sha256, expected);
  assert.equal(evidence.subject.architecture, 'x86_64');
  return {path: relative, sha256: digest, evidence_id: evidence.evidence_id,
    normalized_result: evidence.normalized_result};
});
const helper = sourceEvidence[0].normalized_result;
for (const line of [
  '0x180284094: MOV ESI, EDX', '0x180284099: MOV EDI, R8D',
  '0x1802840a4: CALL 0x1802ee07e',
  '0x1802840a9: MULSD XMM0, qword ptr [0x18034bbc8]',
  '0x1802840b1: CVTTSD2SI RAX, XMM0',
  '0x1802840b6: CMP EAX, 0x2', '0x1802840c2: MOV ECX, 0x10'
]) assert(helper.includes(line), `Missing fixed caller instruction: ${line}`);
assert(sourceEvidence[1].normalized_result.includes('log((double)param_2)'));
assert(sourceEvidence[1].normalized_result.includes('1.4426950408889634'));
for (const line of [
  '0x18013f6a5: MOV EDX, ESI', '0x18013f6a7: SHR EDX, 0x1',
  '0x18013f6a9: MOV R8D, ESI', '0x18013f6af: CALL 0x180284080'
]) assert(sourceEvidence[2].normalized_result.includes(line));

// This recorded prior inspection identifies the existing installation path;
// the path is not taken from the old machine's mere default configuration.
const previousPath = 'docs/object-lifecycle/offline-archive-map-publish/empty-query-and-compare-import.json';
const previousBytes = fs.readFileSync(path.join(root, previousPath));
const previous = JSON.parse(previousBytes);
assert.equal(previous.original, original);
assert.equal(previous.original_sha256_before, expected);
assert.equal(previous.original_sha256_after, expected);
assert.equal(previous.copy_sha256_after, expected);
assert.equal(previous.status, 'empty-query-and-normal-memcmp-import-confirmed');
assert(!fs.existsSync(outputDirectory), 'Refusing to overwrite an existing evidence directory');
fs.mkdirSync(outputDirectory);

const result = {
  started_utc: new Date().toISOString(), status: 'started',
  original, analysis_copy: copy, expected_target_sha256: expected,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: sourceEvidence.map(({normalized_result, ...rest}) => rest),
  existing_path_evidence: {path: previousPath, sha256: hash(previousBytes)},
  target_loaded_or_executed: false, target_crt_executed: false,
  backend_started: false, host_log_used_as_oracle: false,
  read_scope: {code_va: '0x1802ee07e', code_bytes: 6,
    data_va: '0x18034bbc8', data_bytes: 8,
    maximum_metadata_bytes: 65536, maximum_individual_read_bytes: 4096,
    hash_stream_chunk_bytes: 65536},
  hash_passes: [], metadata_reads: [],
  limitations: [
    'On-disk normal import identity and multiplier only; no external CRT body or live import resolution is inspected.',
    'No log call is evaluated. The normal mathematical log contract alone does not establish the exact binary64 result for request 8.',
    'No MXCSR state, exceptional input, allocation failure, or floating-point exception behavior is recovered.'
  ]
};
function hashFile(file, label) {
  const fd = fs.openSync(file, 'r');
  const buffer = Buffer.alloc(65536), digest = createHash('sha256');
  let total = 0;
  try {
    const size = fs.fstatSync(fd).size;
    assert(size > 0 && size <= 64 * 1024 * 1024);
    let count;
    while ((count = fs.readSync(fd, buffer, 0, buffer.length, null)) > 0) {
      digest.update(buffer.subarray(0, count)); total += count;
    }
    assert.equal(total, size);
  } finally { fs.closeSync(fd); }
  const value = digest.digest('hex');
  result.hash_passes.push({label, bytes: total, chunk_bytes: 65536, sha256: value});
  return value;
}
let fd, failure;
try {
  result.original_sha256_before = hashFile(original, 'original-before');
  assert.equal(result.original_sha256_before, expected);
  result.copy_sha256_before = hashFile(copy, 'copy-before');
  assert.equal(result.copy_sha256_before, expected);
  fd = fs.openSync(copy, 'r');
  const size = fs.fstatSync(fd).size;
  let metadataBytes = 0;
  function read(offset, length, category) {
    assert(Number.isSafeInteger(offset) && offset >= 0 &&
      Number.isSafeInteger(length) && length > 0 && length <= 4096 && offset + length <= size);
    const metadata = category !== 'fixed-code' && category !== 'fixed-data';
    if (metadata) {
      assert(metadataBytes + length <= 65536, 'PE metadata budget exhausted');
      metadataBytes += length;
    }
    const bytes = Buffer.alloc(length);
    assert.equal(fs.readSync(fd, bytes, 0, length, offset), length);
    if (metadata) result.metadata_reads.push({category, file_offset: offset, bytes: length});
    return bytes;
  }
  const dos = read(0, 64, 'dos-header');
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60); assert(pe <= 65536);
  const coff = read(pe, 24, 'pe-coff');
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  const sectionCount = coff.readUInt16LE(6), optLength = coff.readUInt16LE(20);
  assert(sectionCount > 0 && sectionCount <= 96 && optLength >= 128 && optLength <= 4096);
  const opt = read(pe + 24, 128, 'pe-optional');
  assert.equal(opt.readUInt16LE(0), 0x20b);
  assert.equal(opt.readBigUInt64LE(24), 0x180000000n);
  assert(opt.readUInt32LE(108) >= 2);
  const sections = Array.from({length: sectionCount}, (_, index) => {
    const bytes = read(pe + 24 + optLength + index * 40, 40, 'section-header');
    return {rva: bytes.readUInt32LE(12), size: bytes.readUInt32LE(16),
      raw: bytes.readUInt32LE(20), flags: bytes.readUInt32LE(36)};
  });
  function section(rva, length) {
    const matches = sections.filter(s => rva >= s.rva && rva + length <= s.rva + s.size);
    assert.equal(matches.length, 1); return matches[0];
  }
  function offset(rva, length) {
    const selected = section(rva, length); return selected.raw + rva - selected.rva;
  }
  const thunkRva = 0x2ee07e;
  assert((section(thunkRva, 6).flags & 0x20000000) !== 0);
  const thunk = read(offset(thunkRva, 6), 6, 'fixed-code');
  result.log_thunk = {va: '0x1802ee07e', file_offset: offset(thunkRva, 6),
    bytes_read: 6, bytes: thunk.toString('hex')};
  assert.equal(thunk[0], 0xff, 'Not the expected import jump; do not follow');
  assert.equal(thunk[1], 0x25, 'Not the expected import jump; do not follow');
  const iatRva = thunkRva + 6 + thunk.readInt32LE(2);
  section(iatRva, 8);
  result.log_thunk.iat_va = '0x' + (0x180000000n + BigInt(iatRva)).toString(16);

  const constantRva = 0x34bbc8;
  assert((section(constantRva, 8).flags & 0xa0000000) === 0);
  const constant = read(offset(constantRva, 8), 8, 'fixed-data');
  const multiplier = constant.readDoubleLE();
  result.multiplier = {va: '0x18034bbc8', file_offset: offset(constantRva, 8),
    bytes_read: 8, bytes: constant.toString('hex'),
    binary64_bits: '0x' + constant.readBigUInt64LE().toString(16).padStart(16, '0'),
    decimal_17_digits: multiplier.toPrecision(17)};
  assert.equal(multiplier, 1.4426950408889634);

  const importRva = opt.readUInt32LE(120), importSize = opt.readUInt32LE(124);
  assert(importRva > 0 && importSize >= 20 && importSize <= 4096);
  function ascii(rva) {
    const bytes = [];
    for (let i = 0; i < 256; i++) {
      const value = read(offset(rva + i, 1), 1, 'import-name')[0];
      if (value === 0) return Buffer.from(bytes).toString('ascii');
      assert(value < 128); bytes.push(value);
    }
    throw Error('Import name exceeds the fixed bound');
  }
  for (let index = 0; (index + 1) * 20 <= importSize; index++) {
    const descriptor = read(offset(importRva + index * 20, 20), 20, 'import-descriptor');
    const values = Array.from({length: 5}, (_, field) => descriptor.readUInt32LE(field * 4));
    if (values.every(value => value === 0)) break;
    const [oft, , , nameRva, ft] = values;
    if (iatRva < ft || (iatRva - ft) % 8 !== 0) continue;
    const entryIndex = (iatRva - ft) / 8;
    if (entryIndex >= 4096) continue;
    let entry = 0n;
    for (let i = 0; i <= entryIndex; i++) {
      entry = read(offset((oft || ft) + i * 8, 8), 8, 'import-lookup').readBigUInt64LE();
      if (entry === 0n) break;
    }
    if (entry === 0n) continue;
    assert((entry & 0x8000000000000000n) === 0n && entry <= 0xffffffffn);
    result.normal_import = {dll: ascii(nameRva), name: ascii(Number(entry) + 2),
      descriptor_rva: importRva + index * 20, thunk_index: entryIndex};
    assert.equal(result.normal_import.name, 'log');
    break;
  }
  assert(result.normal_import, 'Corresponding normal import was not resolved');
  result.metadata_bytes = metadataBytes;
  result.code_bytes_read = 6; result.fixed_data_bytes_read = 8;
  result.request_8_boundary = {
    closed: false,
    reason: 'Import identity and multiplier bits do not determine target log(8) return bits or MULSD rounding state. A product below 3 truncates to 2 (4 buckets), whereas a product at or above 3 but below 4 truncates to 3 (8 buckets).',
    no_host_log_or_target_execution: true
  };
  result.status = 'normal-log-import-and-multiplier-confirmed-boundary-retained';
} catch (error) {
  failure = error; result.status = 'failed'; result.failure = String(error);
} finally {
  if (fd !== undefined) fs.closeSync(fd);
  for (const [file, property, label] of [
    [original, 'original_sha256_after', 'original-after'],
    [copy, 'copy_sha256_after', 'copy-after']
  ]) {
    try { result[property] = hashFile(file, label); }
    catch (error) { result[property + '_error'] = String(error); failure ||= error; }
  }
  result.original_hash_matches = result.original_sha256_after === expected;
  result.copy_hash_matches = result.copy_sha256_after === expected;
  result.finished_utc = new Date().toISOString();
  result.controller_resource_usage = process.resourceUsage();
  fs.writeFileSync(output, JSON.stringify(result, null, 2) + '\n', {flag: 'wx'});
}
assert(result.original_hash_matches && result.copy_hash_matches);
if (failure) throw failure;
console.log(JSON.stringify({status: result.status, output, log_thunk: result.log_thunk,
  normal_import: result.normal_import, multiplier: result.multiplier,
  metadata_bytes: result.metadata_bytes, original_unchanged: true, copy_unchanged: true}));
