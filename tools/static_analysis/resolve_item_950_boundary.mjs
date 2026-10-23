// PREPARED ONLY. Resolve one Game .pdata boundary; no code, unwind or callee read.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/resolve_item_950_boundary.mjs').toLowerCase());
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll', expectedBytes = 11385856;
const expectedHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const imageBase = 0x180000000n, requestedRva = 0x428690;
const out = path.join(root, 'docs/object-lifecycle/offline-item-950-boundary');
const sources = [
  {path:'docs/object-lifecycle/offline-item-factory-dependency-boundaries/metadata.json',
    sha256:'59265ca3af388f48af8da9a66bee439949fa8cfeca5fe1cc40a066998ad46d29'},
  {path:'docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json',
    sha256:'7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f'},
  {path:'docs/object-lifecycle/offline-item-subobjects-boundary/metadata.json',
    sha256:'10482b1a15ae10f3b766733b125b719d1ea5aec690c09e22104a491599f2fc68'},
  {path:'docs/object-lifecycle/offline-questitem-boundaries/boundary.json',
    sha256:'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05'},
];
const limits = Object.freeze({metadata_bytes:512, metadata_read_operations:40, metadata_single_read_bytes:12,
  search_steps:32, fingerprint_chunk_bytes:65536, proof_bytes_each:262144, report_bytes:131072});
const hex = n => '0x' + n.toString(16), hash = b => createHash('sha256').update(b).digest('hex');
const fits = (at, n, size) => Number.isSafeInteger(at) && at >= 0 && Number.isSafeInteger(n) &&
  n > 0 && Number.isSafeInteger(size) && at <= size && n <= size - at;
function normal(p, file = false) {
  let first = true;
  for (let c = path.resolve(p);;) {
    const s = fs.lstatSync(c); assert(!s.isSymbolicLink(), 'Redirected path refused');
    assert(first && file ? s.isFile() : s.isDirectory());
    assert.equal(fs.realpathSync(c).toLowerCase(), c.toLowerCase(), 'Redirected ancestor refused');
    const parent = path.dirname(c); if (parent === c) return; c = parent; first = false;
  }
}
function absent(p) {
  try { fs.lstatSync(p); } catch (e) { if (e.code === 'ENOENT') return; throw e; }
  assert.fail('Existing evidence directory refused');
}
function stable(a, b) {
  return b.isFile() && a.size === b.size && a.dev === b.dev && a.ino === b.ino &&
    a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs;
}
function statRecord(s) { return {size:s.size, dev:s.dev, ino:s.ino, mtime_ms:s.mtimeMs, ctime_ms:s.ctimeMs}; }
function fingerprint(fd) {
  assert.equal(fs.fstatSync(fd).size, expectedBytes);
  const buffer = Buffer.alloc(limits.fingerprint_chunk_bytes), h = createHash('sha256'); let at = 0;
  while (at < expectedBytes) {
    const got = fs.readSync(fd, buffer, 0, Math.min(buffer.length, expectedBytes - at), at);
    assert(got > 0, 'Unexpected fingerprint EOF'); h.update(buffer.subarray(0, got)); at += got;
  }
  return {sha256:h.digest('hex'), bytes:at, chunk_bytes:buffer.length};
}
normal(script, true); normal(path.dirname(out)); absent(out);
const proofs = sources.map(source => {
  const p = path.join(root, source.path); normal(p, true); assert(fs.statSync(p).size <= limits.proof_bytes_each);
  const bytes = fs.readFileSync(p); assert(bytes.length <= limits.proof_bytes_each); assert.equal(hash(bytes), source.sha256);
  return JSON.parse(bytes.toString('utf8'));
});
const prior = proofs[0], caller = proofs[1].result.structuredContent, secondary = proofs[2], quest = proofs[3], pe = prior.pe;
assert.equal(prior.status, 'all-fixed-exact-boundaries-assessed'); assert.equal(prior.target, target);
assert.equal(prior.expected_sha256, expectedHash); assert.equal(prior.expected_bytes, expectedBytes);
assert.equal(prior.sha256_before, expectedHash); assert.equal(prior.sha256_after, expectedHash);
assert(prior.source_stat_stable && prior.source_hash_preserved && prior.source_handle_closed);
assert.equal(prior.stat_before.size, expectedBytes); assert.equal(prior.stat_after.size, expectedBytes);
assert.equal(pe.image_base, hex(imageBase)); assert.equal(pe.image_size, 11427840);
assert.equal(pe.exception_rva, '0xa6a000'); assert.equal(pe.exception_file_offset, 10896384);
assert.equal(pe.entry_count, 22293); assert.equal(pe.exception_bytes, pe.entry_count * 12);
assert.equal(caller.subject.name, 'Game.dll'); assert.equal(caller.subject.digest.sha256, expectedHash);
assert.equal(caller.evidence_id, 'ev_dc74728b755b7c7439ee4b8d39c086823605e9c076f29830caff716706ea11ee');
const callerLines = caller.normalized_result.split(/\r?\n/), fixedCaller = [
  '0x180323805: LEA RCX, [RDI + 0x950]', '0x18032380c: CALL 0x180428690'];
for (const line of fixedCaller) assert(callerLines.includes(line), 'Pinned caller instruction missing');
const optional = prior.metadata_reads.filter(r => r.kind === 'optional-header-prefix'); assert.equal(optional.length, 1);
assert.equal(optional[0].file_offset, 296); assert.equal(optional[0].requested_bytes, 144); assert.equal(optional[0].read_bytes, 144);
const optionalRaw = Buffer.from(optional[0].raw_hex, 'hex'); assert.equal(optionalRaw.length, 144);
assert.equal(optionalRaw.readUInt16LE(0), 0x20b); assert.equal(optionalRaw.readBigUInt64LE(24), imageBase);
assert.equal(optionalRaw.readUInt32LE(56), pe.image_size);
assert.equal(optionalRaw.readUInt32LE(136), Number(pe.exception_rva)); assert.equal(optionalRaw.readUInt32LE(140), pe.exception_bytes);
const pdata = pe.sections.filter(s => s.name === '.pdata'); assert.equal(pdata.length, 1);
assert.equal(pdata[0].rva, Number(pe.exception_rva)); assert.equal(pdata[0].raw_offset, pe.exception_file_offset);
assert(fits(0, pe.exception_bytes, pdata[0].raw_size)); assert(!(pdata[0].characteristics & 0x20000000));
assert(fits(pe.exception_file_offset, pe.exception_bytes, expectedBytes));
assert.equal(secondary.status, 'gap-without-exact-begin');
assert.equal(secondary.target, target); assert.equal(secondary.expected_sha256, expectedHash);
assert.equal(secondary.expected_bytes, expectedBytes); assert.equal(secondary.sha256_before, expectedHash);
assert.equal(secondary.sha256_after, expectedHash);
assert(secondary.source_stat_stable && secondary.source_hash_preserved && secondary.source_handle_closed);
assert.deepEqual(secondary.failures, []);
assert.equal(secondary.pe.exception_rva, pe.exception_rva);
assert.equal(secondary.pe.exception_file_offset, pe.exception_file_offset);
assert.equal(secondary.pe.entry_count, pe.entry_count);
assert.equal(quest.status, 'questitem-ctor-read-boundaries-and-classinfo-assessed');
assert.equal(quest.target, target); assert.equal(quest.expected_sha256, expectedHash);
assert.equal(quest.source_bytes, expectedBytes); assert.equal(quest.sha256_before, expectedHash);
assert.equal(quest.sha256_after, expectedHash); assert(quest.source_stat_stable && quest.source_hash_preserved);
// These cached probes retain decoded begin/end but not raw12B. Reuse only their
// sorted-search bounds; never synthesize their missing unwind field or raw bytes.
const questSelections = quest.boundaries.filter(b => b.requested_rva === '0x427c90');
assert.equal(questSelections.length, 1);
const lowerProbes = questSelections[0].probes.filter(p => p.index === 15404);
const upperProbes = questSelections[0].probes.filter(p => p.index === 15415);
assert.equal(lowerProbes.length, 1); assert.equal(upperProbes.length, 1);
const lowerProbe = lowerProbes[0], upperProbe = upperProbes[0];
assert.equal(lowerProbe.begin_rva, '0x427d30'); assert.equal(lowerProbe.end_rva_exclusive, '0x427e31');
assert.equal(upperProbe.begin_rva, '0x428a00'); assert.equal(upperProbe.end_rva_exclusive, '0x42906d');
assert(Number(lowerProbe.begin_rva) < requestedRva && requestedRva < Number(upperProbe.begin_rva));
for (const p of [lowerProbe, upperProbe]) {
  const matches = quest.metadata_reads.filter(r => r.file_offset === pe.exception_file_offset + p.index * 12);
  assert(matches.length > 0);
  for (const r of matches) { assert.equal(r.requested_bytes, 12); assert.equal(r.read_bytes, 12); }
}
const searchFirst = 15405, searchLimit = 15415;
const cache = new Map(), reused = new Set();
function decode(index, bytes, provenance) {
  assert(Number.isInteger(index) && index >= 0 && index < pe.entry_count); assert.equal(bytes.length, 12);
  const begin = bytes.readUInt32LE(0), end = bytes.readUInt32LE(4), unwind = bytes.readUInt32LE(8);
  assert(begin > 0 && begin < end && end <= pe.image_size); assert(unwind > 0 && unwind < pe.image_size);
  const text = pe.sections.filter(s => s.name === '.text'); assert.equal(text.length, 1);
  assert(fits(begin - text[0].rva, end - begin, text[0].raw_size));
  for (const [i, row] of cache) {
    if (i < index) assert(row.begin < begin, 'Observed .pdata order mismatch');
    if (i > index) assert(row.begin > begin, 'Observed .pdata order mismatch');
  }
  return {index, entry_rva:hex(Number(pe.exception_rva) + index * 12),
    file_offset:pe.exception_file_offset + index * 12, begin, end, unwind,
    begin_rva:hex(begin), end_rva_exclusive:hex(end), unwind_rva:hex(unwind), raw_hex:bytes.toString('hex'), provenance};
}
for (const [sourceIndex, reads] of [
  [0, prior.metadata_reads.filter(r => r.kind === 'selected-runtime-function')],
  [2, secondary.metadata_reads],
]) {
  for (const r of reads) {
    assert.equal(r.requested_bytes, 12); assert.equal(r.read_bytes, 12);
    const index = (r.file_offset - pe.exception_file_offset) / 12, bytes = Buffer.from(r.raw_hex, 'hex');
    const entry = decode(index, bytes, 'cached-source-' + sourceIndex);
    if (cache.has(index)) assert.equal(cache.get(index).raw_hex, entry.raw_hex); else cache.set(index, entry);
  }
}
const cachedExact = [...cache.values()].filter(e => e.begin === requestedRva); assert(cachedExact.length <= 1);
const report = {schema:'grim-dynasty-fixed-item-950-boundary-v1', status:'started', started_utc:new Date().toISOString(),
  target, expected_sha256:expectedHash, expected_bytes:expectedBytes, requested_rva:hex(requestedRva),
  source_evidence:sources, caller_evidence_id:caller.evidence_id, caller_instructions:fixedCaller,
  pe, limits, target_open_mode:'O_RDONLY', target_loaded_or_executed:false, child_process_started:false,
  backend_started:false, code_bytes_read:0, unwind_body_bytes_read:0, new_header_bytes_read:0,
  target_copy_created:false, whole_exception_table_scanned:false, metadata_bytes:0, metadata_read_operations:0,
  metadata_reads:[], cached_entries_available:cache.size, cached_exact_available:cachedExact.length === 1,
  fingerprint_passes:[], probes:[], search_steps:0, exact_boundary:null,
  cached_search_bounds:{source_index:3,lower_probe:lowerProbe,upper_probe:upperProbe,
    first_index:searchFirst,exclusive_limit_index:searchLimit,raw_entry_bytes_retained:false,
    use:'Only decoded sorted bounds; any selected exact/current/predecessor result needs a complete raw entry.'},
  limitations:[
    'Only Game RVA428690 is selected by the saved Item constructor CALL at32380C with RCX=this+950. Existing same-identity PE and raw .pdata entries are reused; no new header read.',
    'Binary search is narrowed to[15405,15415) by pinned same-Game archival probes15404/15415, at most4 normal steps with the32-step hard guard retained. Only a missing current entry and one necessary predecessor may supplement it; no extra successor or neighboring body is selected.',
    'A missing exact begin is reported as containing/gap/before-first/after-last, never as permission to choose nearby code. No .pdata unwind payload or function code is read.',
    'Two same-handle64KiB streaming fingerprints are separate from metadata budgets. Stable file stats/hash are not an atomic snapshot or proof of runtime state.',
  ]};
let fd, firstStat, admittedSize = false;
const failures = [];
function entry(index) {
  assert(Number.isInteger(index) && index >= 0 && index < pe.entry_count);
  if (cache.has(index)) {
    const row = cache.get(index); if (row.provenance.startsWith('cached-source-')) reused.add(index); return row;
  }
  assert(report.metadata_read_operations < limits.metadata_read_operations);
  assert(report.metadata_bytes + 12 <= limits.metadata_bytes);
  const offset = pe.exception_file_offset + index * 12; assert(fits(index * 12, 12, pe.exception_bytes));
  const bytes = Buffer.alloc(12), record = {index, file_offset:offset, requested_bytes:12, read_bytes:0, raw_hex:''};
  report.metadata_reads.push(record); report.metadata_read_operations++;
  const got = fs.readSync(fd, bytes, 0, 12, offset); report.metadata_bytes += got;
  record.read_bytes = got; record.raw_hex = bytes.subarray(0, got).toString('hex'); assert.equal(got, 12, 'Short read; no retry');
  const row = decode(index, bytes, 'new-read'); cache.set(index, row); return row;
}
try {
  normal(target, true); fd = fs.openSync(target, fs.constants.O_RDONLY); firstStat = fs.fstatSync(fd);
  assert(firstStat.isFile()); assert.equal(firstStat.size, expectedBytes); admittedSize = true; report.stat_before = statRecord(firstStat);
  const before = fingerprint(fd); report.fingerprint_passes.push({label:'before', ...before});
  report.sha256_before = before.sha256; assert.equal(before.sha256, expectedHash);
  let lo = searchFirst, hi = searchLimit;
  if (cachedExact.length) { lo = cachedExact[0].index; reused.add(lo); report.lookup_method = 'cached-exact-entry'; }
  else {
    report.lookup_method = 'lower-bound-with-cached-sorted-bounds';
    while (lo < hi) {
      assert(report.search_steps < limits.search_steps); report.search_steps++;
      const mid = lo + Math.floor((hi - lo) / 2), row = entry(mid); report.probes.push(row);
      if (row.begin < requestedRva) lo = mid + 1; else hi = mid;
    }
  }
  assert(lo >= searchFirst && lo <= searchLimit); report.lower_bound_index = lo;
  const current = lo < pe.entry_count ? entry(lo) : null;
  report.lower_bound_entry = current;
  if (current && current.begin === requestedRva) {
    report.status = 'exact-begin'; report.exact_boundary = current;
    report.previous_known = cache.get(lo - 1) || null; report.next_known = cache.get(lo + 1) || null;
  } else {
    const previous = lo > 0 ? entry(lo - 1) : null; report.previous_entry = previous;
    report.status = previous && previous.begin < requestedRva && requestedRva < previous.end ? 'containing-without-exact-begin' :
      !previous ? 'before-first-entry' : !current ? 'after-last-entry' : 'gap-without-exact-begin';
    report.selected_code_range = null;
  }
} catch (e) { failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        const after = fingerprint(fd); report.fingerprint_passes.push({label:'after', ...after});
        report.sha256_after = after.sha256; assert.equal(after.sha256, expectedHash);
        const last = fs.fstatSync(fd); report.stat_after = statRecord(last); report.source_stat_stable = stable(firstStat, last);
        assert(report.source_stat_stable);
      }
    } catch (e) { failures.push('Original final verification: ' + String(e)); }
    finally { try { fs.closeSync(fd); report.source_handle_closed = true; } catch (e) { failures.push('Original close: ' + String(e)); } }
  }
  report.source_hash_preserved = report.sha256_before === expectedHash && report.sha256_after === expectedHash;
  report.reused_entries = [...reused].sort((a, b) => a - b).map(i => cache.get(i));
  report.failures = failures; if (failures.length) report.status = 'failed'; report.finished_utc = new Date().toISOString();
}
assert(fd === undefined || report.source_handle_closed === true, 'Source close unconfirmed');
normal(path.dirname(out)); absent(out); fs.mkdirSync(out); normal(out);
const output = JSON.stringify(report, null, 2) + '\n'; assert(Buffer.byteLength(output, 'utf8') <= limits.report_bytes);
fs.writeFileSync(path.join(out, 'metadata.json'), output, {flag:'wx'});
console.log(JSON.stringify({status:report.status, requested_rva:report.requested_rva, exact_boundary:report.exact_boundary,
  metadata_bytes:report.metadata_bytes, metadata_read_operations:report.metadata_read_operations,
  search_steps:report.search_steps, source_hash_preserved:report.source_hash_preserved, failures}));
if (failures.length) process.exitCode = 1;
