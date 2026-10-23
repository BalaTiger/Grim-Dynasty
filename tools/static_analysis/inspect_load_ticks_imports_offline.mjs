// Fixed two-slot PE metadata inspection only. Never load or execute Engine,
// QPC/timeGetTime, a backend, or another tool. Parent reviews before execution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(script), '../..');
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const copy = path.join(root, '.tooling/offline-cache-node/Engine.readonly.dll');
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedBytes = 4485632;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-load-ticks-imports');
const output = path.join(outputDirectory, 'tick-import-bindings.json');
const hashChunkBytes = 65536, metadataBudget = 65536, maxReadBytes = 4096;
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = value => `0x${value.toString(16)}`;
const slots = [
  {rva: 0x302208, dll: 'KERNEL32.dll', name: 'QueryPerformanceCounter',
    calls: ['0x1801c4e4d', '0x1801c510d']},
  {rva: 0x302788, dll: 'WINMM.dll', name: 'timeGetTime',
    calls: ['0x1801c4e73', '0x1801c5130']},
];
const callerPath = 'docs/object-lifecycle/rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json';
const callerHash = 'd696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1';
const callerBytes = fs.readFileSync(path.join(root, callerPath));
assert.equal(hash(callerBytes), callerHash, 'Caller evidence changed');
const caller = JSON.parse(callerBytes).result.structuredContent;
assert.equal(caller.subject.digest.sha256, expected);
assert.equal(caller.subject.architecture, 'x86_64');
for (const slot of slots) for (const call of slot.calls) {
  assert(caller.normalized_result.includes(`${call}: CALL qword ptr [${hex(0x180000000n + BigInt(slot.rva))}]`),
    'Missing exact caller-to-IAT instruction');
}
const pathProof = 'docs/object-lifecycle/offline-archive-map-publish/empty-query-and-compare-import.json';
const pathProofHash = 'a3f8e80396046474daead1958056fc00d4733920760c6134356006255403eb39';
const pathBytes = fs.readFileSync(path.join(root, pathProof));
assert.equal(hash(pathBytes), pathProofHash, 'Existing path evidence changed');
const prior = JSON.parse(pathBytes);
assert.equal(prior.original, original);
assert.equal(prior.original_sha256_before, expected);
assert.equal(prior.original_sha256_after, expected);
assert.equal(prior.copy_sha256_after, expected);
assert.equal(prior.status, 'empty-query-and-normal-memcmp-import-confirmed');
assert(!fs.existsSync(outputDirectory), 'Preserve existing evidence directory');
fs.mkdirSync(outputDirectory);

const result = {
  started_utc: new Date().toISOString(), status: 'started',
  original, analysis_copy: copy, expected_target_sha256: expected, expected_file_bytes: expectedBytes,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: {path: callerPath, sha256: callerHash, evidence_id: caller.evidence_id},
  existing_path_evidence: {path: pathProof, sha256: pathProofHash},
  requested_iat_vas: slots.map(s => hex(0x180000000n + BigInt(s.rva))),
  target_loaded_or_executed: false, clock_api_executed: false, backend_started: false,
  subprocesses: 0, installations: false, permission_escalation: false,
  read_scope: {
    interpreted_instruction_bytes: 0, fixed_data_or_bss_bytes: 0,
    maximum_metadata_bytes: metadataBudget, maximum_individual_read_bytes: maxReadBytes,
    hash_stream_chunk_bytes: hashChunkBytes, full_file_hash_reads_all_bytes: true,
  },
  hash_passes: [], metadata_reads: [], normal_imports: [],
  limitations: [
    'Only necessary PE headers/import descriptors, bounded lookup prefixes and the two fixed IAT/name bindings are interpreted.',
    'Full-file streaming hashes traverse all file bytes without instruction decoding or runtime loading.',
    'No complete import-name list, function body, BSS value, runtime initialization value, delay import or forwarder is inspected.',
    'Static import identity does not restore API failure behavior, global initialization, runtime binding or native exception handling.',
    'All opens use O_RDONLY. Node does not expose a FileShare.Read exclusivity contract; same-handle stat and hashes observe changes but are not an atomic snapshot guarantee.',
  ],
};
const handles = [];
let failure, metadataBytes = 0;
function openFixed(file, label) {
  assert.equal(fs.realpathSync(file).toLowerCase(), path.resolve(file).toLowerCase(),
    'Unexpected target path redirection');
  const fd = fs.openSync(file, fs.constants.O_RDONLY);
  const item = {fd, file, label};
  handles.push(item);
  item.before = fs.fstatSync(fd);
  assert(item.before.isFile(), 'Target is not a regular file');
  assert.equal(item.before.size, expectedBytes, 'Unexpected fixed file size');
  return item;
}
function fingerprint(item, phase) {
  assert.equal(fs.fstatSync(item.fd).size, expectedBytes, 'File size changed before hashing');
  const buffer = Buffer.alloc(hashChunkBytes), digest = createHash('sha256');
  let cursor = 0;
  while (cursor < expectedBytes) {
    const count = fs.readSync(item.fd, buffer, 0, Math.min(buffer.length, expectedBytes - cursor), cursor);
    assert(count > 0, 'Unexpected EOF while hashing');
    digest.update(buffer.subarray(0, count)); cursor += count;
  }
  const sha256 = digest.digest('hex');
  result.hash_passes.push({label: `${item.label}-${phase}`, bytes: cursor,
    chunk_bytes: hashChunkBytes, sha256});
  return sha256;
}
try {
  const originalItem = openFixed(original, 'original');
  result.original_sha256_before = fingerprint(originalItem, 'before');
  assert.equal(result.original_sha256_before, expected, 'Original fingerprint changed');
  const copyItem = openFixed(copy, 'copy');
  result.copy_sha256_before = fingerprint(copyItem, 'before');
  assert.equal(result.copy_sha256_before, expected, 'Copy fingerprint changed');
  function read(offset, length, category) {
    assert(Number.isSafeInteger(offset) && offset >= 0 && Number.isSafeInteger(length) &&
      length > 0 && length <= maxReadBytes && offset + length <= expectedBytes,
      'Metadata read outside fixed bounds');
    assert(metadataBytes + length <= metadataBudget, 'Metadata byte budget exhausted');
    metadataBytes += length;
    const bytes = Buffer.alloc(length);
    let done = 0;
    while (done < length) {
      const count = fs.readSync(copyItem.fd, bytes, done, length - done, offset + done);
      assert(count > 0, 'Unexpected EOF during metadata read'); done += count;
    }
    result.metadata_reads.push({category, file_offset: offset, bytes: length});
    return bytes;
  }
  const dos = read(0, 64, 'dos-header');
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60); assert(pe <= 65536);
  const coff = read(pe, 24, 'pe-coff');
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  const sectionCount = coff.readUInt16LE(6), optLength = coff.readUInt16LE(20);
  assert(sectionCount > 0 && sectionCount <= 96 && optLength >= 128 && optLength <= maxReadBytes);
  const opt = read(pe + 24, 128, 'pe-optional');
  assert.equal(opt.readUInt16LE(0), 0x20b);
  const imageBase = opt.readBigUInt64LE(24);
  assert.equal(imageBase, 0x180000000n);
  assert(opt.readUInt32LE(108) >= 2);
  const sections = Array.from({length: sectionCount}, (_, index) => {
    const bytes = read(pe + 24 + optLength + index * 40, 40, 'section-header');
    return {rva: bytes.readUInt32LE(12), size: bytes.readUInt32LE(16),
      raw: bytes.readUInt32LE(20), flags: bytes.readUInt32LE(36)};
  });
  function offset(rva, length) {
    assert(Number.isSafeInteger(rva) && rva > 0 && rva <= 0xffffffff &&
      Number.isSafeInteger(length) && length > 0 && rva + length <= 0x100000000);
    const matches = sections.filter(s => rva >= s.rva && rva + length <= s.rva + s.size);
    assert.equal(matches.length, 1, 'Metadata must be uniquely file-backed');
    assert.equal(matches[0].flags & 0x20000000, 0, 'No instruction-section metadata reads');
    const fileOffset = matches[0].raw + rva - matches[0].rva;
    assert(fileOffset + length <= expectedBytes);
    return fileOffset;
  }
  function raw(rva, length, category) {
    const fileOffset = offset(rva, length), bytes = read(fileOffset, length, category);
    return {rva: hex(rva), file_offset: fileOffset, bytes: bytes.toString('hex')};
  }
  function asciiZ(rva) {
    const values = [];
    for (let i = 0; i < 256; i++) {
      const value = read(offset(rva + i, 1), 1, 'selected-import-name')[0];
      values.push(value);
      if (value === 0) return {rva: hex(rva), file_offset: offset(rva, values.length),
        bytes: Buffer.from(values).toString('hex'),
        text: Buffer.from(values.slice(0, -1)).toString('ascii')};
      assert(value >= 0x20 && value < 0x7f, 'Unexpected import-name byte');
    }
    throw Error('Import name exceeds fixed bound');
  }
  const importRva = opt.readUInt32LE(120), importSize = opt.readUInt32LE(124);
  assert(importRva > 0 && importSize >= 20 && importSize <= 4096);
  const descriptors = [];
  let terminated = false;
  for (let index = 0; (index + 1) * 20 <= importSize; index++) {
    const evidence = raw(importRva + index * 20, 20, 'import-descriptor');
    const bytes = Buffer.from(evidence.bytes, 'hex');
    if (bytes.every(value => value === 0)) { terminated = true; break; }
    const oft = bytes.readUInt32LE(0), nameRva = bytes.readUInt32LE(12), ft = bytes.readUInt32LE(16);
    assert(oft > 0 && nameRva > 0 && ft > 0, 'Require explicit normal-import OFT');
    descriptors.push({index, evidence, oft, nameRva, ft});
  }
  assert(terminated, 'Import descriptor terminator not found within directory');
  for (const slot of slots) {
    const candidates = [];
    for (const descriptor of descriptors) {
      if (slot.rva < descriptor.ft || (slot.rva - descriptor.ft) % 8 !== 0) continue;
      const index = (slot.rva - descriptor.ft) / 8;
      if (index >= 4096) continue;
      const prefix = [];
      let value = 0n;
      for (let i = 0; i <= index; i++) {
        const entry = raw(descriptor.oft + i * 8, 8, 'bounded-import-lookup');
        value = Buffer.from(entry.bytes, 'hex').readBigUInt64LE(0);
        prefix.push(entry);
        if (value === 0n) break;
      }
      if (value !== 0n) candidates.push({descriptor, index, prefix, value});
    }
    assert.equal(candidates.length, 1, 'Fixed IAT slot must have one normal import owner');
    const {descriptor, index, prefix, value} = candidates[0];
    assert((value & 0x8000000000000000n) === 0n && value > 0n && value <= 0xffffffffn,
      'Require named normal import');
    const iat = raw(slot.rva, 8, 'fixed-iat');
    assert.equal(Buffer.from(iat.bytes, 'hex').readBigUInt64LE(0), value, 'On-disk IAT and OFT differ');
    const hint = raw(Number(value), 2, 'selected-import-hint');
    const name = asciiZ(Number(value) + 2), dll = asciiZ(descriptor.nameRva);
    const binding = {
      iat_va: hex(imageBase + BigInt(slot.rva)), source_call_vas: slot.calls,
      dll: dll.text, name: name.text,
      hint: Buffer.from(hint.bytes, 'hex').readUInt16LE(0),
      descriptor_index: descriptor.index, first_thunk_rva: hex(descriptor.ft),
      original_first_thunk_rva: hex(descriptor.oft), thunk_index: index,
      raw_metadata: {descriptor: descriptor.evidence, lookup_prefix_through_target: prefix,
        target_lookup: prefix[prefix.length - 1], iat, hint,
        symbol_name_with_nul: name, dll_name_with_nul: dll},
    };
    result.normal_imports.push(binding);
    assert.equal(dll.text.toLowerCase(), slot.dll.toLowerCase(), 'Unexpected DLL binding');
    assert.equal(name.text, slot.name, 'Unexpected symbol binding');
  }
  result.headers = {pe_file_offset: pe, optional_header_size: optLength, section_count: sectionCount,
    import_directory_rva: hex(importRva), import_directory_size: importSize};
  result.status = 'fixed-two-tick-imports-confirmed';
} catch (error) {
  failure = error; result.status = 'failed'; result.failure = String(error);
} finally {
  for (const item of handles) {
    try {
      result[`${item.label}_sha256_after`] = fingerprint(item, 'after');
      assert.equal(result[`${item.label}_sha256_after`], expected, 'File fingerprint changed');
      const after = fs.fstatSync(item.fd);
      assert.equal(after.size, item.before.size, 'File size changed');
      assert.equal(after.mtimeMs, item.before.mtimeMs, 'File timestamp changed');
    } catch (error) {
      failure ||= error; result[`${item.label}_after_error`] = String(error);
    } finally { fs.closeSync(item.fd); }
  }
  result.metadata_bytes = metadataBytes;
  result.original_hash_matches = result.original_sha256_before === expected && result.original_sha256_after === expected;
  result.copy_hash_matches = result.copy_sha256_before === expected && result.copy_sha256_after === expected;
  if (failure) result.status = 'failed';
  result.finished_utc = new Date().toISOString();
  result.controller_resource_usage = process.resourceUsage();
  fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
}
if (failure) throw failure;
assert(result.original_hash_matches && result.copy_hash_matches);
console.log(JSON.stringify({status: result.status, output,
  bindings: result.normal_imports.map(({iat_va, dll, name}) => ({iat_va, dll, name})),
  metadata_bytes: metadataBytes, original_unchanged: true, copy_unchanged: true}));
