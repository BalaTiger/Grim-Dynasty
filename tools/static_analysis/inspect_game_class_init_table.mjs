// Fixed Game CRT initializer pointers and exact .pdata metadata only.
// No target code, other initializer array, end neighbor, or automatic follow-up.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expectedHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const expectedBytes = 11385856, imageBase = 0x180000000n;
const startRva = 0x60bab8, endRva = 0x60c878, slotCount = 440;
const evidence = [
  {path: 'docs/object-lifecycle/offline-questitem-boundaries/boundary.json', sha256: 'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05'},
  {path: 'docs/object-lifecycle/offline-class-static-crt-attach/inspection.json', sha256: 'fa0c4a5b7a5d1a2ccb3ed3a35d4a2c98a1192d4fc79cafae9c63a28eaa71366c'},
  {path: 'docs/object-lifecycle/offline-class-static-crt-thunks/inspection.json', sha256: '7a776c85892b5583a068ff2fe9d96935825614f80d12a7706e2648b50488c4c1'},
  {path: 'docs/object-lifecycle/offline-game-crt-imports-metadata/metadata.json', sha256: '675999fee760e0a6b84e881818942b25950d1e4c563f2c6964fd13fa25c4ed2b'},
];
const directory = path.join(root, 'docs/object-lifecycle/offline-game-class-init-table');
const output = path.join(directory, 'metadata.json');
const limits = Object.freeze({fingerprint_chunk_bytes: 65536, metadata_bytes: 65536,
  single_read_bytes: 4096, table_bytes: 4096, targets: 440, search_steps: 32,
  saved_evidence_bytes: 2 * 1024 * 1024, report_bytes: 8 * 1024 * 1024});
const hex = value => `0x${value.toString(16)}`;
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
const within = (at, length, total) => Number.isSafeInteger(at) && at >= 0 &&
  Number.isSafeInteger(length) && length > 0 && at <= total && length <= total - at;
function realDirectory(absolute) {
  for (let current = absolute; ; current = path.dirname(current)) {
    const stat = fs.lstatSync(current);
    assert.ok(stat.isDirectory() && !stat.isSymbolicLink(), 'Directory ancestor must not be a link');
    if (path.dirname(current) === current) break;
  }
}
realDirectory(path.dirname(directory));
if (fs.existsSync(directory)) realDirectory(directory);
assert.ok(!fs.existsSync(output), 'Refuse to overwrite evidence');
const proofs = evidence.map(item => {
  const file = path.join(root, item.path), stat = fs.lstatSync(file);
  assert.ok(stat.isFile() && !stat.isSymbolicLink() && stat.size <= limits.saved_evidence_bytes);
  const bytes = fs.readFileSync(file);
  assert.equal(digest(bytes), item.sha256, 'Pinned source evidence changed');
  const value = JSON.parse(bytes);
  assert.equal(value.expected_sha256, expectedHash);
  assert.equal(value.target ?? value.original, target);
  return value;
});
const [boundary, attach, thunks, imports] = proofs;
assert.equal(boundary.status, 'questitem-ctor-read-boundaries-and-classinfo-assessed');
assert.equal(boundary.source_bytes, expectedBytes);
assert.equal(boundary.source_hash_preserved, true);
for (const [proof, stage] of [[attach, 'crt-attach'], [thunks, 'crt-thunks']]) {
  assert.equal(proof.status, 'fixed-game-init-fragments-verified');
  assert.equal(proof.stage, stage);
  assert.equal(proof.original_hash_matches, true);
  assert.equal(proof.original_stat_stable, true);
  assert.deepEqual(proof.failures, []);
}
assert.equal(imports.status, 'fixed-game-crt-imports-resolved');
assert.equal(imports.source_hash_preserved, true);
assert.equal(imports.source_stat_stable, true);
for (const [name, rva] of [['_initterm_e', '0x60b940'], ['_initterm', '0x60b948']]) {
  const matches = imports.bindings.filter(item => item.name === name && item.iat_rva === rva);
  assert.equal(matches.length, 1, 'Required CRT binding not established');
  assert.equal(matches[0].module, 'api-ms-win-crt-runtime-l1-1-0.dll');
}
assert.equal(endRva - startRva, slotCount * 8);
assert.ok(endRva - startRva <= limits.table_bytes);
const result = {schema: 'grim-dynasty-fixed-game-class-init-table-v1', status: 'started',
  started_utc: new Date().toISOString(), target, expected_sha256: expectedHash,
  expected_file_bytes: expectedBytes, script_sha256: digest(fs.readFileSync(fileURLToPath(import.meta.url))),
  source_evidence: evidence, limits,
  requested: {start_rva: hex(startRva), end_rva_exclusive: hex(endRva), bytes: endRva - startRva, slots: slotCount},
  provenance: {caller_va: '0x1805db06e', thunk_va: '0x1805dc3da', import_name: '_initterm', iat_rva: '0x60b948'},
  target_open_mode: 'O_RDONLY', target_loaded_or_executed: false, backend_started: false,
  subprocesses: 0, full_target_copy_created: false, code_bytes_read: 0, unwind_body_bytes_read: 0,
  other_initializer_tables_read: 0, end_neighbor_bytes_read: 0, whole_exception_table_scanned: false,
  metadata_bytes: 0, metadata_reads: [], fingerprint_passes: [], slots: [], distinct_targets: [],
  limitations: [
    'The 440 slots are read in their original order, including nulls and duplicates. This is a static preferred-VA array, not runtime execution evidence.',
    'Non-null executable targets receive cached binary searches for exact BeginAddress matches only. Missing exact entries remain null; no containing function, code or neighbor is substituted.',
    '.pdata is assumed ordered as required by its PE format. Encountered entries are checked for ordering, but the whole directory is not scanned. An exact range may cover only a logical-function fragment.',
    'No initializer is executed, classified as a class registration, or followed. Names, ClassInfo edges and runtime registration effects remain unexamined.',
    'Two same-handle full-file hashes traverse all file bytes with a 64 KiB buffer; semantic reads are only fixed table pointers and bounded PE metadata. Hash/stat equality is not an atomic-snapshot guarantee.',
  ]};
let fd, initialStat, failure;
const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
function fingerprint(label) {
  const hash = createHash('sha256'); let position = 0;
  while (position < expectedBytes) {
    const n = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, expectedBytes - position), position);
    assert.ok(n > 0, 'Fingerprint encountered unexpected EOF');
    hash.update(hashBuffer.subarray(0, n)); position += n;
  }
  const pass = {label, bytes: position, chunk_bytes: hashBuffer.length, sha256: hash.digest('hex')};
  result.fingerprint_passes.push(pass); return pass.sha256;
}
function read(at, length, kind) {
  assert.ok(within(at, length, expectedBytes) && length <= limits.single_read_bytes, 'Invalid bounded metadata read');
  assert.ok(length <= limits.metadata_bytes - result.metadata_bytes, '64 KiB total metadata budget exceeded');
  const entry = {kind, file_offset: at, requested_bytes: length, read_bytes: 0};
  result.metadata_reads.push(entry); result.metadata_bytes += length;
  const bytes = Buffer.alloc(length);
  while (entry.read_bytes < length) {
    const n = fs.readSync(fd, bytes, entry.read_bytes, length - entry.read_bytes, at + entry.read_bytes);
    assert.ok(n > 0, 'Metadata encountered unexpected EOF'); entry.read_bytes += n;
  }
  return bytes;
}
try {
  fd = fs.openSync(target, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert.ok(initialStat.isFile() && initialStat.size === expectedBytes, 'Fixed Game size mismatch');
  result.sha256_before = fingerprint('before'); assert.equal(result.sha256_before, expectedHash);
  const dos = read(0, 64, 'dos-header'); assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const peAt = dos.readUInt32LE(60); assert.equal(peAt, boundary.pe.pe_header_file_offset);
  const coff = read(peAt, 24, 'signature-and-coff-header');
  assert.equal(coff.readUInt32LE(0), 0x4550); assert.equal(coff.readUInt16LE(4), 0x8664);
  const count = coff.readUInt16LE(6), optionalBytes = coff.readUInt16LE(20);
  assert.equal(count, boundary.pe.sections.length); assert.ok(count > 0 && count <= 96);
  assert.ok(optionalBytes >= 144 && optionalBytes <= 4096 && peAt + 24 + optionalBytes + count * 40 <= 65536);
  const optional = read(peAt + 24, 144, 'optional-header-prefix');
  assert.equal(optional.readUInt16LE(0), 0x20b); assert.equal(optional.readBigUInt64LE(24), imageBase);
  assert.ok(optional.readUInt32LE(108) >= 4);
  const imageSize = optional.readUInt32LE(56), sectionBytes = read(peAt + 24 + optionalBytes, count * 40, 'section-headers');
  const sections = Array.from({length: count}, (_, i) => {
    const at = i * 40, s = {name: sectionBytes.subarray(at, at + 8).toString('ascii').replace(/\0.*$/, ''),
      rva: sectionBytes.readUInt32LE(at + 12), raw_size: sectionBytes.readUInt32LE(at + 16),
      raw_offset: sectionBytes.readUInt32LE(at + 20), characteristics: sectionBytes.readUInt32LE(at + 36)};
    if (s.raw_size) assert.ok(within(s.raw_offset, s.raw_size, expectedBytes)); return s;
  });
  assert.deepEqual(sections, boundary.pe.sections);
  function mapping(rva, bytes) {
    if (!within(rva, bytes, imageSize)) return null;
    const found = sections.filter(s => rva >= s.rva && within(rva - s.rva, bytes, s.raw_size));
    assert.ok(found.length <= 1, 'Ambiguous RVA mapping');
    return found.length ? {section: found[0], offset: found[0].raw_offset + rva - found[0].rva} : null;
  }
  const pdataRva = optional.readUInt32LE(136), pdataSize = optional.readUInt32LE(140);
  assert.equal(hex(pdataRva), boundary.pe.exception_directory_rva);
  assert.equal(pdataSize, boundary.pe.exception_directory_size);
  assert.ok(pdataSize > 0 && pdataSize % 12 === 0);
  const pdata = mapping(pdataRva, pdataSize), table = mapping(startRva, endRva - startRva);
  assert.ok(pdata && pdata.section.name === '.pdata' && !(pdata.section.characteristics & 0x20000000));
  assert.ok(table && !(table.section.characteristics & 0x20000000));
  const pointers = read(table.offset, endRva - startRva, 'exact-initializer-pointer-array');
  result.table = {file_offset: table.offset, section: table.section.name, bytes: pointers.length, sha256: digest(pointers)};
  result.pe = {image_base: hex(imageBase), image_size: imageSize, sections,
    exception_directory_rva: hex(pdataRva), exception_directory_size: pdataSize};
  const cache = new Map(), ordered = [], targets = new Map(), entries = pdataSize / 12;
  function entryAt(index) {
    assert.ok(Number.isInteger(index) && index >= 0 && index < entries);
    if (cache.has(index)) return cache.get(index);
    const raw = read(pdata.offset + index * 12, 12, 'binary-search-runtime-function');
    const entry = {index, begin: raw.readUInt32LE(0), end: raw.readUInt32LE(4), unwind: raw.readUInt32LE(8)};
    assert.ok(entry.begin < entry.end && entry.end <= imageSize);
    let lo = 0, hi = ordered.length;
    while (lo < hi) { const mid = (lo + hi) >>> 1; if (ordered[mid].index < index) lo = mid + 1; else hi = mid; }
    if (lo) assert.ok(ordered[lo - 1].begin <= entry.begin, 'Unsorted encountered .pdata entries');
    if (lo < ordered.length) assert.ok(ordered[lo].begin >= entry.begin, 'Unsorted encountered .pdata entries');
    ordered.splice(lo, 0, entry); cache.set(index, entry); return entry;
  }
  for (let index = 0; index < slotCount; index++) {
    const pointer = pointers.readBigUInt64LE(index * 8), key = hex(pointer);
    const rva = pointer >= imageBase && pointer - imageBase < BigInt(imageSize) ? Number(pointer - imageBase) : null;
    const slot = {index, slot_rva: hex(startRva + index * 8), slot_va: hex(imageBase + BigInt(startRva + index * 8)),
      pointer: key, target_rva: rva === null ? null : hex(rva), distinct_target_index: null};
    result.slots.push(slot);
    if (pointer === 0n) continue;
    if (!targets.has(key)) {
      assert.ok(targets.size < limits.targets);
      const mapped = rva === null ? null : mapping(rva, 1), executable = !!(mapped && (mapped.section.characteristics & 0x20000000));
      const item = {index: targets.size, pointer: key, target_rva: slot.target_rva,
        section: mapped?.section.name ?? null, executable, slot_indices: [], exact_boundary: null, search_steps: 0};
      targets.set(key, item); result.distinct_targets.push(item);
      if (executable) {
        let lo = 0, hi = entries;
        while (lo < hi) {
          assert.ok(item.search_steps++ < limits.search_steps);
          const mid = (lo + hi) >>> 1, entry = entryAt(mid);
          if (entry.begin < rva) lo = mid + 1; else hi = mid;
        }
        const entry = lo < entries ? entryAt(lo) : null;
        if (entry && entry.begin === rva) {
          const range = mapping(entry.begin, entry.end - entry.begin);
          assert.ok(range && (range.section.characteristics & 0x20000000));
          item.exact_boundary = {exception_entry_index: entry.index, exception_entry_rva: hex(pdataRva + entry.index * 12),
            begin_rva: hex(entry.begin), end_rva_exclusive: hex(entry.end), bytes: entry.end - entry.begin,
            unwind_info_rva: hex(entry.unwind), file_offset: range.offset, section: range.section.name, code_bytes_read: 0};
        }
      }
    }
    const item = targets.get(key); item.slot_indices.push(index); slot.distinct_target_index = item.index;
  }
  result.distinct_exception_entries_read = cache.size;
  result.null_slots = result.slots.filter(slot => slot.pointer === '0x0').length;
  result.status = 'fixed-game-initializer-pointers-and-exact-boundaries-assessed';
} catch (error) { failure = error; result.status = 'failed'; result.failure = String(error); }
finally {
  if (fd !== undefined) {
    try {
      if (initialStat?.isFile() && initialStat.size === expectedBytes) {
        result.sha256_after = fingerprint('after'); const stat = fs.fstatSync(fd);
        result.source_stat_stable = ['size', 'dev', 'ino', 'mtimeMs', 'ctimeMs'].every(key => stat[key] === initialStat[key]);
        result.source_hash_preserved = result.sha256_before === expectedHash && result.sha256_after === expectedHash;
        assert.ok(result.source_stat_stable && result.source_hash_preserved, 'Source changed or did not match');
      }
    } catch (error) { failure ??= error; result.status = 'failed'; result.final_fingerprint_failure = String(error); }
    finally { try { fs.closeSync(fd); } catch (error) { failure ??= error; result.status = 'failed'; result.close_failure = String(error); } }
  }
  result.finished_utc = new Date().toISOString();
  const report = Buffer.from(`${JSON.stringify(result, null, 2)}\n`);
  assert.ok(report.length <= limits.report_bytes, 'Report budget exceeded');
  realDirectory(path.dirname(directory));
  if (!fs.existsSync(directory)) fs.mkdirSync(directory);
  realDirectory(directory); fs.writeFileSync(output, report, {flag: 'wx'});
}
console.log(JSON.stringify({status: result.status, output, slots: result.slots.length,
  distinct_targets: result.distinct_targets.length, metadata_bytes: result.metadata_bytes,
  source_hash_preserved: result.source_hash_preserved}));
if (failure) process.exitCode = 1;
