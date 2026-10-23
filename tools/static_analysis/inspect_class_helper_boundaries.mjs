// Prepared only: fixed Game .pdata metadata, no code or automatic neighbor reads.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll', size = 11385856;
const expected = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2', imageBase = 0x180000000n;
const sources = [
  {path: 'docs/object-lifecycle/offline-class-static-crt-reason/inspection.json',
    sha256: '08ded9b6ff52c160c3699f4c369f0c1420151772313f615f39bbff1818f557e7', indices: [20610, 20612]},
  {path: 'docs/object-lifecycle/offline-class-static-crt-attach/inspection.json',
    sha256: 'fa0c4a5b7a5d1a2ccb3ed3a35d4a2c98a1192d4fc79cafae9c63a28eaa71366c', indices: [20552, 20596]},
];
const directory = path.join(root, 'docs/object-lifecycle/offline-class-helper-boundaries'), output = path.join(directory, 'metadata.json');
const sha = bytes => createHash('sha256').update(bytes).digest('hex'), hex = n => `0x${n.toString(16)}`;
const cache = new Map(), reads = [], fingerprints = [], hashBuffer = Buffer.alloc(65536);
const limits = {new_metadata_bytes: 84, new_entries: 7, search_reads: 6, fingerprint_chunk_bytes: 65536};
function decode(index, raw, provenance) {
  assert(Number.isInteger(index) && index >= 0 && index < 22293 && /^[a-f0-9]{24}$/i.test(raw));
  const b = Buffer.from(raw, 'hex'), begin = b.readUInt32LE(0), end = b.readUInt32LE(4);
  assert(begin < end, 'Invalid cached/read range');
  const entry = {index, begin, end, unwind: b.readUInt32LE(8), raw_bytes: raw.toLowerCase(),
    pdata_rva: hex(0xa6a000 + index * 12), file_offset: 10896384 + index * 12, provenance};
  for (const other of cache.values()) assert(other.index < index ? other.begin <= begin : other.begin >= begin, 'Observed ordering conflict');
  assert(!cache.has(index), 'Duplicate cache insertion'); cache.set(index, entry); return entry;
}
for (const source of sources) {
  const file = path.join(root, source.path); assert(fs.statSync(file).size <= 2 * 1024 * 1024);
  const bytes = fs.readFileSync(file); assert.equal(sha(bytes), source.sha256);
  const saved = JSON.parse(bytes); assert.equal(saved.status, 'fixed-game-init-fragments-verified');
  assert.equal(saved.original, target); assert.equal(saved.original_sha256_before, expected); assert.equal(saved.original_sha256_after, expected);
  assert.equal(saved.original_hash_matches, true);
  for (const index of source.indices) {
    const rows = saved.metadata_reads.filter(row => row.index === index); assert.equal(rows.length, 1);
    assert.equal(rows[0].file_offset, 10896384 + index * 12); assert.equal(Number(rows[0].rva), 0xa6a000 + index * 12);
    decode(index, rows[0].bytes, {kind: 'saved-metadata', path: source.path, sha256: source.sha256});
  }
}
assert.equal(cache.get(20610).begin, 0x5daeb8); assert.equal(cache.get(20610).end, 0x5daecf);
assert(cache.get(20610).end <= 0x5daf40 && cache.get(20612).begin > 0x5daf40);
assert(cache.get(20552).begin < 0x5da5dc && cache.get(20596).begin > 0x5da5dc);
assert(!fs.existsSync(directory), 'Preserve existing output directory');
assert.equal(fs.realpathSync(path.dirname(directory)).toLowerCase(), path.resolve(path.dirname(directory)).toLowerCase());
const result = {schema: 'grim-dynasty-fixed-class-helper-boundaries-v1', started_utc: new Date().toISOString(),
  status: 'started', target, expected_sha256: expected, expected_file_bytes: size, script_sha256: sha(fs.readFileSync(script)),
  sources, limits, target_open_mode: 'O_RDONLY', target_loaded_or_executed: false, code_bytes_read: 0,
  unwind_body_bytes_read: 0, backend_started: false, subprocesses: 0, metadata_reads: reads, fingerprint_passes: fingerprints,
  requested_rvas: ['0x5daeb8', '0x5daf40', '0x5da5dc'], excluded_new_callee: '0x5dae68',
  limitations: ['Only index20611 and binary-search indices20553..20595 may be newly read; no neighbors are fetched.',
    'Saved .pdata is reused after matching pinned evidence and the original hash; observed ordering is checked, not the entire table.',
    'Exact ranges are unwind fragments, not complete logical-function proof. No exact entry permits no neighbor-body selection.',
    'Fingerprint passes traverse all bytes without interpretation; O_RDONLY and before/after checks are not an atomic snapshot.']};
let fd, initial, admitted = false, failure;
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, size); const h = createHash('sha256'); let at = 0;
  while (at < size) { const n = fs.readSync(fd, hashBuffer, 0, Math.min(65536, size - at), at);
    assert(n > 0); h.update(hashBuffer.subarray(0, n)); at += n; }
  const digest = h.digest('hex'); fingerprints.push({label, bytes: at, sha256: digest, chunk_bytes: 65536}); return digest;
}
function entryAt(index) {
  if (cache.has(index)) return cache.get(index);
  assert(index === 20611 || (index >= 20553 && index <= 20595), 'Outside fixed requested indices');
  assert(reads.length < limits.new_entries && (reads.length + 1) * 12 <= limits.new_metadata_bytes);
  const fileOffset = 10896384 + index * 12; assert(fileOffset >= 10896384 && fileOffset + 12 <= 10896384 + 267516);
  const b = Buffer.alloc(12), read = {index, file_offset: fileOffset, requested_bytes: 12, read_bytes: 0}; reads.push(read);
  while (read.read_bytes < 12) { const n = fs.readSync(fd, b, read.read_bytes, 12 - read.read_bytes, fileOffset + read.read_bytes);
    assert(n > 0); read.read_bytes += n; }
  read.raw_bytes = b.toString('hex'); return decode(index, read.raw_bytes, {kind: 'new-fixed-metadata'});
}
function describe(e) {
  return e ? {index: e.index, begin_rva: hex(e.begin), end_rva_exclusive: hex(e.end), range_bytes: e.end - e.begin,
    begin_va: hex(imageBase + BigInt(e.begin)), end_va_exclusive: hex(imageBase + BigInt(e.end)),
    unwind_info_rva: hex(e.unwind), pdata_rva: e.pdata_rva, file_offset: e.file_offset, raw_bytes: e.raw_bytes, provenance: e.provenance} : null;
}
function assess(rva) {
  const ordered = [...cache.values()].sort((a, b) => a.index - b.index);
  const exact = ordered.filter(e => e.begin === rva), containing = ordered.filter(e => e.begin <= rva && rva < e.end);
  const before = ordered.filter(e => e.begin < rva), after = ordered.filter(e => e.begin > rva);
  const previous = before[before.length - 1], next = after[0];
  return {requested_rva: hex(rva), status: exact.length === 1 ? 'exact-unwind-entry' : exact.length > 1 ? 'ambiguous-exact-entries' :
    containing.length ? 'no-exact-entry-inside-cached-range' : 'no-exact-entry-or-containing-cached-range',
    exact_entries: exact.map(describe), containing_entries: containing.map(describe),
    previous_cached_entry: describe(previous), next_cached_entry: describe(next),
    known_previous_neighbor: exact.length === 1 ? describe(cache.get(exact[0].index - 1)) : null,
    known_next_neighbor: exact.length === 1 ? describe(cache.get(exact[0].index + 1)) : null,
    cached_brackets_adjacent: !!previous && !!next && next.index === previous.index + 1, selected_neighbor_body: false, code_bytes_read: 0};
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(), path.resolve(target).toLowerCase());
  fd = fs.openSync(target, fs.constants.O_RDONLY); initial = fs.fstatSync(fd);
  assert(initial.isFile() && initial.size === size); admitted = true;
  result.sha256_before = fingerprint('before'); assert.equal(result.sha256_before, expected);
  entryAt(20611);
  let lo = 20553, hi = 20596, count = 0; result.binary_search_probes = [];
  while (lo < hi) { assert(count < limits.search_reads); const mid = lo + Math.floor((hi - lo) / 2), e = entryAt(mid);
    result.binary_search_probes.push({index: mid, begin_rva: hex(e.begin)}); count++;
    if (e.begin < 0x5da5dc) lo = mid + 1; else hi = mid; }
  assert(cache.has(lo) && cache.has(lo - 1), 'Search must establish cached lower-bound neighbors without extra reads');
  assert(cache.get(lo - 1).begin < 0x5da5dc && cache.get(lo).begin >= 0x5da5dc);
  result.search_lower_bound_index = lo; result.search_reads = count;
  result.boundaries = [0x5daeb8, 0x5daf40, 0x5da5dc].map(assess); result.status = 'fixed-helper-boundaries-assessed';
} catch (error) { failure = error; result.status = 'failed'; result.failure = String(error); }
finally {
  result.new_metadata_bytes = reads.length * 12; result.observed_entries = [...cache.values()].sort((a, b) => a.index - b.index).map(describe);
  if (fd !== undefined) {
    try { if (admitted) { result.sha256_after = fingerprint('after'); const now = fs.fstatSync(fd);
      result.source_stat_stable = now.isFile() && now.size === initial.size && now.dev === initial.dev && now.ino === initial.ino &&
        now.mtimeMs === initial.mtimeMs && now.ctimeMs === initial.ctimeMs;
      result.source_hash_preserved = result.sha256_before === expected && result.sha256_after === expected;
      assert(result.source_stat_stable && result.source_hash_preserved); } }
    catch (error) { failure ??= error; result.status = 'failed'; result.final_fingerprint_failure = String(error); }
    finally { try { fs.closeSync(fd); } catch (error) { failure ??= error; result.status = 'failed'; result.close_failure = String(error); } }
  }
  result.finished_utc = new Date().toISOString(); const encoded = `${JSON.stringify(result, null, 2)}\n`;
  assert(Buffer.byteLength(encoded) <= 128 * 1024); fs.mkdirSync(directory); fs.writeFileSync(output, encoded, {flag: 'wx'});
}
console.log(JSON.stringify({status: result.status, output, new_metadata_bytes: result.new_metadata_bytes, source_hash_preserved: result.source_hash_preserved}));
if (failure) process.exitCode = 1;
