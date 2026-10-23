// Fixed seven-slot Game PE import metadata inspection. Prepare/review only;
// execution belongs to the parent task. No target code, DLL loading or backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(script), '../..');
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expected = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const expectedBytes = 11385856;
const imageBaseExpected = 0x180000000n;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-questitem-imports');
const output = path.join(outputDirectory, 'import-bindings.json');
const hashChunkBytes = 65536, metadataBudget = 65536, maxReadBytes = 4096;
const maximumLookupEntries = 4096, maximumNameBytes = 1024;
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = value => `0x${value.toString(16)}`;
const slots = [
  {rva: 0x609050, source: '0x180427e9e', opcode: '488b0d',
    operation: 'MOV RCX from import slot',
    instruction: 'mov rcx,qword ptr [0000000180609050h]'},
  {rva: 0x609440, source: '0x180427ea8', opcode: 'ff15',
    operation: 'CALL through import slot',
    instruction: 'call qword ptr [0000000180609440h]'},
  {rva: 0x609470, source: '0x180427eb8', opcode: 'ff15',
    operation: 'CALL through import slot',
    instruction: 'call qword ptr [0000000180609470h]'},
  {rva: 0x609a58, source: '0x180427eff', opcode: 'ff15',
    operation: 'CALL through import slot',
    instruction: 'call qword ptr [0000000180609a58h]'},
  {rva: 0x609ae8, source: '0x180427f0d', opcode: '4c8b0d',
    operation: 'MOV R9 from import slot',
    instruction: 'mov r9,qword ptr [0000000180609ae8h]'},
  {rva: 0x609710, source: '0x180324c87', opcode: 'ff15', sourceKind: 'item-read',
    operation: 'CALL through import slot',
    instruction: 'call qword ptr [0000000180609710h]'},
  {rva: 0x609700, source: '0x1803236b3', sourceKind: 'item-constructor',
    operation: 'CALL through import slot'},
];
const callerPath = 'docs/object-lifecycle/offline-questitem-core/427e40-instructions.json';
const callerHash = 'c3b33f63983226570596d7386e81c49f6e5d32117e86974b61b777ba5a21362d';
const provenancePath = 'docs/object-lifecycle/offline-questitem-core/inspection.json';
const provenanceHash = '2bb0b904a42abe435e7fcf956c444a6fe7e1a81209e3dba492f76a442e561663';
const callerBytes = fs.readFileSync(path.join(root, callerPath));
assert.equal(hash(callerBytes), callerHash, 'Frozen caller evidence changed');
const caller = JSON.parse(callerBytes);
assert(Array.isArray(caller), 'Expected saved instruction array');
function verifySavedInstruction(slot, rows) {
  const matches = rows.filter(row => row.address === slot.source);
  assert.equal(matches.length, 1, 'Require unique exact caller instruction');
  const row = matches[0], bytes = Buffer.from(row.bytes, 'hex');
  assert.equal(row.instruction.replace(/\s+/g, ' ').trim().toLowerCase(), slot.instruction);
  assert.equal(bytes.length, slot.opcode.length / 2 + 4);
  assert.equal(bytes.subarray(0, slot.opcode.length / 2).toString('hex'), slot.opcode);
  const target = BigInt(slot.source) + BigInt(bytes.length) +
    BigInt(bytes.readInt32LE(slot.opcode.length / 2));
  assert.equal(target, imageBaseExpected + BigInt(slot.rva), 'Saved RIP-relative operand mismatch');
}
for (const slot of slots.filter(item => !item.sourceKind)) verifySavedInstruction(slot, caller);
const provenanceBytes = fs.readFileSync(path.join(root, provenancePath));
assert.equal(hash(provenanceBytes), provenanceHash, 'Frozen inspection provenance changed');
const provenance = JSON.parse(provenanceBytes);
assert.equal(provenance.status, 'fixed-questitem-core-disassembly-verified');
assert.equal(provenance.original, original);
assert.equal(provenance.expected_sha256, expected);
assert.equal(provenance.original_sha256_before, expected);
assert.equal(provenance.original_sha256_after, expected);
assert.equal(provenance.original_hash_matches, true);
const fragment = provenance.fragments.filter(item => item.start_va === '0x180427e40');
assert.equal(fragment.length, 1);
assert.equal(fragment[0].end_va_exclusive, '0x180427fbb');
assert.equal(fragment[0].byte_coverage_verified, true);
assert.equal(fragment[0].code_bytes, 379);

const readCallerPath = 'docs/object-lifecycle/offline-questitem-item-read/324c60-instructions.json';
const readCallerHash = 'a600281f5ac3b70e5c5cf59964353a963e4b695c3c096ee977133f6e9642f324';
const readProvenancePath = 'docs/object-lifecycle/offline-questitem-item-read/inspection.json';
const readProvenanceHash = '17e076619bdad87bdad191752b64c558f6f3b9cc1c333a413b334559817ad99d';
const readCallerBytes = fs.readFileSync(path.join(root, readCallerPath));
assert.equal(hash(readCallerBytes), readCallerHash, 'Frozen Item Read caller changed');
const readCaller = JSON.parse(readCallerBytes);
assert(Array.isArray(readCaller));
verifySavedInstruction(slots[5], readCaller);
const readProvenanceBytes = fs.readFileSync(path.join(root, readProvenancePath));
assert.equal(hash(readProvenanceBytes), readProvenanceHash, 'Frozen Item Read provenance changed');
const readProvenance = JSON.parse(readProvenanceBytes);
assert.equal(readProvenance.status, 'fixed-questitem-item-read-disassembly-verified');
assert.equal(readProvenance.original, original);
assert.equal(readProvenance.expected_sha256, expected);
assert.equal(readProvenance.original_sha256_before, expected);
assert.equal(readProvenance.original_sha256_after, expected);
assert.equal(readProvenance.original_hash_matches, true);
assert.equal(readProvenance.fragments.length, 1);
assert.equal(readProvenance.fragments[0].start_va, '0x180324c60');
assert.equal(readProvenance.fragments[0].end_va_exclusive, '0x1803250ae');
assert.equal(readProvenance.fragments[0].byte_coverage_verified, true);
assert.equal(readProvenance.fragments[0].code_bytes, 1102);

const ctorCallerPath = 'docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json';
const ctorCallerHash = '7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f';
const ctorCallerBytes = fs.readFileSync(path.join(root, ctorCallerPath));
assert.equal(hash(ctorCallerBytes), ctorCallerHash, 'Frozen Item constructor caller changed');
const ctorCaller = JSON.parse(ctorCallerBytes).result.structuredContent;
assert.equal(ctorCaller.evidence_id, 'ev_dc74728b755b7c7439ee4b8d39c086823605e9c076f29830caff716706ea11ee');
assert.equal(ctorCaller.subject.digest.sha256, expected);
assert.equal(ctorCaller.subject.name, 'Game.dll');
assert.equal(ctorCaller.subject.architecture, 'x86_64');
assert.equal(path.resolve(ctorCaller.subject.local_path).toLowerCase(), path.resolve(original).toLowerCase());
assert.equal(ctorCaller.operation, 'procedure_assembly');
assert.equal(ctorCaller.normalized_result.split(/\r?\n/).filter(line =>
  line === '0x1803236b3: CALL qword ptr [0x180609700]').length, 1,
  'Require exact saved Item constructor IAT instruction');

// Only this new project directory is written. Existing evidence is never replaced.
for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  assert(fs.lstatSync(directory).isDirectory(), 'Output ancestor must be an existing directory');
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(),
    'Unexpected output path redirection');
  if (directory === path.dirname(directory)) break;
}
assert(!fs.existsSync(outputDirectory), 'Preserve existing evidence directory');
fs.mkdirSync(outputDirectory);

const result = {
  started_utc: new Date().toISOString(), status: 'started',
  original, expected_target_sha256: expected, expected_file_bytes: expectedBytes,
  script_sha256: hash(fs.readFileSync(script)),
  source_evidence: [
    {path: callerPath, sha256: callerHash, source_instruction_vas: slots.slice(0, 5).map(slot => slot.source)},
    {path: readCallerPath, sha256: readCallerHash, source_instruction_vas: [slots[5].source]},
    {path: ctorCallerPath, sha256: ctorCallerHash, evidence_id: ctorCaller.evidence_id,
      source_instruction_vas: [slots[6].source]},
  ],
  source_provenance: [
    {path: provenancePath, sha256: provenanceHash},
    {path: readProvenancePath, sha256: readProvenanceHash},
  ],
  requested_iat_vas: slots.map(slot => hex(imageBaseExpected + BigInt(slot.rva))),
  target_loaded_or_executed: false, imported_functions_executed: false,
  imported_data_targets_read: false, backend_started: false, subprocesses: 0,
  installations: false, permission_escalation: false, full_target_copy_created: false,
  read_scope: {
    interpreted_instruction_bytes: 0, fixed_data_or_bss_bytes: 0,
    maximum_metadata_bytes: metadataBudget, maximum_individual_read_bytes: maxReadBytes,
    maximum_lookup_entries_per_descriptor: maximumLookupEntries,
    maximum_selected_name_bytes: maximumNameBytes,
    hash_stream_chunk_bytes: hashChunkBytes, full_file_hash_reads_all_bytes: true,
  },
  hash_passes: [], metadata_reads: [], normal_imports: [],
  limitations: [
    'Only PE headers/import descriptors, bounded OFT ownership prefixes and the seven fixed named normal imports are interpreted.',
    'Full-file streaming SHA256 reads all file bytes but does not interpret target instructions or load the DLL.',
    'No complete symbol/import list, other function body, export target, delay import, BSS value or imported object is read.',
    'MOV/CALL labels describe only the saved caller instruction; import names do not establish data layout, runtime values or function ABI.',
    'No bound/ordinal/delay-import fallback, symbol guessing, additional slot discovery or automatic follow-up is allowed.',
    'All target opens use O_RDONLY. Node does not expose a FileShare.Read exclusivity contract; hashes and same-handle stats detect observed changes, not an atomic snapshot.',
  ],
};
const handles = [];
const metadataCache = new Map();
let failure, metadataBytes = 0;
function openFixed() {
  assert.equal(fs.realpathSync(original).toLowerCase(), path.resolve(original).toLowerCase(),
    'Unexpected fixed target path redirection');
  const fd = fs.openSync(original, fs.constants.O_RDONLY);
  const item = {fd};
  handles.push(item);
  item.before = fs.fstatSync(fd);
  assert(item.before.isFile(), 'Target is not a regular file');
  assert.equal(item.before.size, expectedBytes, 'Unexpected fixed Game file size');
  return item;
}
function fingerprint(item, phase) {
  assert.equal(fs.fstatSync(item.fd).size, expectedBytes, 'File size changed before hashing');
  const buffer = Buffer.alloc(hashChunkBytes), digest = createHash('sha256');
  let cursor = 0;
  while (cursor < expectedBytes) {
    const count = fs.readSync(item.fd, buffer, 0, Math.min(buffer.length, expectedBytes - cursor), cursor);
    assert(count > 0, 'Unexpected EOF while hashing');
    digest.update(buffer.subarray(0, count));
    cursor += count;
  }
  const sha256 = digest.digest('hex');
  result.hash_passes.push({label: `original-${phase}`, bytes: cursor,
    chunk_bytes: hashChunkBytes, sha256});
  return sha256;
}
try {
  const target = openFixed();
  result.original_sha256_before = fingerprint(target, 'before');
  assert.equal(result.original_sha256_before, expected, 'Original fingerprint changed');
  function read(fileOffset, length, category) {
    assert(Number.isSafeInteger(fileOffset) && fileOffset >= 0 && Number.isSafeInteger(length) &&
      length > 0 && length <= maxReadBytes && fileOffset + length <= expectedBytes,
      'Metadata read outside fixed bounds');
    const key = `${fileOffset}:${length}`;
    if (metadataCache.has(key)) return metadataCache.get(key);
    assert(metadataBytes + length <= metadataBudget, 'Metadata byte budget exhausted');
    metadataBytes += length;
    const bytes = Buffer.alloc(length);
    let done = 0;
    while (done < length) {
      const count = fs.readSync(target.fd, bytes, done, length - done, fileOffset + done);
      assert(count > 0, 'Unexpected EOF during metadata read');
      done += count;
    }
    metadataCache.set(key, bytes);
    result.metadata_reads.push({category, file_offset: fileOffset, bytes: length});
    return bytes;
  }
  const dos = read(0, 64, 'dos-header');
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60);
  assert(pe <= 65536, 'PE header outside fixed header bound');
  const coff = read(pe, 24, 'pe-coff');
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  const sectionCount = coff.readUInt16LE(6), optLength = coff.readUInt16LE(20);
  assert(sectionCount > 0 && sectionCount <= 96 && optLength >= 128 && optLength <= maxReadBytes);
  const opt = read(pe + 24, 128, 'pe-optional');
  assert.equal(opt.readUInt16LE(0), 0x20b);
  const imageBase = opt.readBigUInt64LE(24);
  assert.equal(imageBase, imageBaseExpected);
  assert(opt.readUInt32LE(108) >= 2);
  const sections = Array.from({length: sectionCount}, (_, index) => {
    const bytes = read(pe + 24 + optLength + index * 40, 40, 'section-header');
    return {rva: bytes.readUInt32LE(12), size: bytes.readUInt32LE(16),
      raw: bytes.readUInt32LE(20), flags: bytes.readUInt32LE(36)};
  });
  function offset(rva, length) {
    assert(Number.isSafeInteger(rva) && rva > 0 && rva <= 0xffffffff &&
      Number.isSafeInteger(length) && length > 0 && rva + length <= 0x100000000);
    const matches = sections.filter(section => rva >= section.rva &&
      rva + length <= section.rva + section.size);
    assert.equal(matches.length, 1, 'Metadata must have one file-backed section');
    assert.equal(matches[0].flags & 0x20000000, 0, 'Refuse metadata in executable section');
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
    for (let i = 0; i < maximumNameBytes; i++) {
      const value = read(offset(rva + i, 1), 1, 'selected-import-name')[0];
      values.push(value);
      if (value === 0) return {rva: hex(rva), file_offset: offset(rva, values.length),
        bytes: Buffer.from(values).toString('hex'),
        text: Buffer.from(values.slice(0, -1)).toString('ascii')};
      assert(value >= 0x20 && value < 0x7f, 'Unexpected import-name byte');
    }
    throw Error('Selected import name exceeds fixed bound');
  }
  const importRva = opt.readUInt32LE(120), importSize = opt.readUInt32LE(124);
  assert(importRva > 0 && importSize >= 20 && importSize <= 4096);
  const descriptors = [];
  let terminated = false;
  for (let index = 0; (index + 1) * 20 <= importSize; index++) {
    const evidence = raw(importRva + index * 20, 20, 'normal-import-descriptor');
    const bytes = Buffer.from(evidence.bytes, 'hex');
    if (bytes.every(value => value === 0)) { terminated = true; break; }
    const oft = bytes.readUInt32LE(0), nameRva = bytes.readUInt32LE(12), ft = bytes.readUInt32LE(16);
    assert(oft > 0 && nameRva > 0 && ft > 0, 'Require explicit normal-import OFT');
    descriptors.push({index, evidence, oft, nameRva, ft});
  }
  assert(terminated, 'Missing descriptor terminator within fixed import directory');
  for (const slot of slots) {
    const candidates = [];
    for (const descriptor of descriptors) {
      if (slot.rva < descriptor.ft || (slot.rva - descriptor.ft) % 8 !== 0) continue;
      const index = (slot.rva - descriptor.ft) / 8;
      if (index >= maximumLookupEntries) continue;
      const entries = [];
      let value = 0n;
      for (let i = 0; i <= index; i++) {
        const entry = raw(descriptor.oft + i * 8, 8, 'bounded-import-ownership-lookup');
        value = Buffer.from(entry.bytes, 'hex').readBigUInt64LE(0);
        entries.push(entry);
        if (value === 0n) break;
      }
      if (value !== 0n) candidates.push({descriptor, index, entries, value});
    }
    assert.equal(candidates.length, 1, 'Fixed slot must have one normal-import owner');
    const {descriptor, index, entries, value} = candidates[0];
    assert((value & 0x8000000000000000n) === 0n && value > 0n && value <= 0xffffffffn,
      'Only named normal imports are covered');
    const iat = raw(slot.rva, 8, 'fixed-selected-iat');
    assert.equal(Buffer.from(iat.bytes, 'hex').readBigUInt64LE(0), value,
      'On-disk IAT and OFT differ; no bound-import fallback');
    const hint = raw(Number(value), 2, 'selected-import-hint');
    const name = asciiZ(Number(value) + 2), dll = asciiZ(descriptor.nameRva);
    // Keep the required ownership prefix compact. It contains thunk values,
    // not an enumeration or dereference of their unselected imported names.
    const prefix = {
      rva: hex(descriptor.oft), file_offset: offset(descriptor.oft, entries.length * 8),
      entries: entries.length, bytes: entries.map(entry => entry.bytes).join(''),
    };
    result.normal_imports.push({
      iat_va: hex(imageBase + BigInt(slot.rva)), source_instruction_va: slot.source,
      caller_operation: slot.operation, dll: dll.text, name: name.text,
      hint: Buffer.from(hint.bytes, 'hex').readUInt16LE(0),
      descriptor_index: descriptor.index, first_thunk_rva: hex(descriptor.ft),
      original_first_thunk_rva: hex(descriptor.oft), thunk_index: index,
      raw_metadata: {descriptor: descriptor.evidence, ownership_lookup_prefix: prefix,
        target_lookup: entries[entries.length - 1], iat, hint,
        symbol_name_with_nul: name, dll_name_with_nul: dll},
    });
  }
  assert.equal(result.normal_imports.length, 7);
  result.headers = {pe_file_offset: pe, optional_header_size: optLength,
    section_count: sectionCount, import_directory_rva: hex(importRva), import_directory_size: importSize};
  result.status = 'fixed-seven-questitem-imports-confirmed';
} catch (error) {
  failure = error;
  result.status = 'failed';
  result.failure = String(error);
} finally {
  for (const item of handles) {
    try {
      result.original_sha256_after = fingerprint(item, 'after');
      assert.equal(result.original_sha256_after, expected, 'Original fingerprint changed');
      const after = fs.fstatSync(item.fd);
      assert.equal(after.size, item.before.size, 'Original size changed');
      assert.equal(after.mtimeMs, item.before.mtimeMs, 'Original timestamp changed');
      assert.equal(after.dev, item.before.dev, 'Original device identity changed');
      assert.equal(after.ino, item.before.ino, 'Original file identity changed');
      result.original_stat_stable = true;
    } catch (error) {
      failure ||= error;
      result.original_after_error = String(error);
    } finally {
      try { fs.closeSync(item.fd); }
      catch (error) { failure ||= error; result.original_close_error = String(error); }
    }
  }
  result.metadata_bytes = metadataBytes;
  result.original_hash_matches = result.original_sha256_before === expected &&
    result.original_sha256_after === expected;
  if (failure) result.status = 'failed';
  result.finished_utc = new Date().toISOString();
  result.controller_resource_usage = process.resourceUsage();
  fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
}
if (failure) throw failure;
assert(result.original_hash_matches);
console.log(JSON.stringify({status: result.status, output,
  bindings: result.normal_imports.map(({iat_va, dll, name}) => ({iat_va, dll, name})),
  metadata_bytes: metadataBytes, original_unchanged: true}));
