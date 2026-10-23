// PREPARED ONLY. Reuse the exact iterator entry; query two fixed callback boundaries.
// No code, unwind, new header, target loading, subprocess or automatic callee follow.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/resolve_item_950_elements_boundaries.mjs').toLowerCase());
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll', expectedBytes = 11385856;
const expectedHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const imageBase = 0x180000000n;
const requested = Object.freeze([
  {role:'array-iterator-cached-only',rva:0x5daed0,cached_only:true},
  {role:'element-constructor-callback',rva:0x16710,cached_only:false},
  {role:'element-destruction-callback',rva:0x165d0,cached_only:false},
]);
const out = path.join(root, 'docs/object-lifecycle/offline-item-950-elements-boundaries');
const sources = [
  {path:'docs/object-lifecycle/offline-item-factory-dependency-boundaries/metadata.json',
    sha256:'59265ca3af388f48af8da9a66bee439949fa8cfeca5fe1cc40a066998ad46d29'},
  {path:'docs/object-lifecycle/offline-item-950/428690-exact-instructions.json',
    sha256:'4f82c0e268008d01646ad0790c45cb560d042fc7a8ae8dcd94ce50fb48d538a1'},
  {path:'docs/object-lifecycle/offline-item-950/inspection.json',
    sha256:'1bed51eacf3be0a2b6aff43e5e1ec283310d912599bdd51ca1c259994a5b270b'},
  {path:'docs/object-lifecycle/offline-class-helper-boundaries/metadata.json',
    sha256:'3f5fa9f5d532be7fe4f4624bf56d3fc0ff487a01bdf710c3a9376abe8c536627'},
  {path:'docs/object-lifecycle/offline-item-subobjects-boundary/metadata.json',
    sha256:'10482b1a15ae10f3b766733b125b719d1ea5aec690c09e22104a491599f2fc68'},
];
const limits = Object.freeze({metadata_bytes:1024, metadata_read_operations:80, metadata_single_read_bytes:12,
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
const prior = proofs[0], instructions = proofs[1], inspection = proofs[2], helpers = proofs[3], secondary = proofs[4], pe = prior.pe;
assert.equal(prior.status, 'all-fixed-exact-boundaries-assessed'); assert.equal(prior.target, target);
assert.equal(prior.expected_sha256, expectedHash); assert.equal(prior.expected_bytes, expectedBytes);
assert.equal(prior.sha256_before, expectedHash); assert.equal(prior.sha256_after, expectedHash);
assert(prior.source_stat_stable && prior.source_hash_preserved && prior.source_handle_closed);
assert.equal(prior.stat_before.size, expectedBytes); assert.equal(prior.stat_after.size, expectedBytes);
assert.equal(pe.image_base, hex(imageBase)); assert.equal(pe.image_size, 11427840);
assert.equal(pe.exception_rva, '0xa6a000'); assert.equal(pe.exception_file_offset, 10896384);
assert.equal(pe.entry_count, 22293); assert.equal(pe.exception_bytes, pe.entry_count * 12);
assert.equal(inspection.status, 'fixed-item-950-windows-decoded-awaiting-manual-control-flow-review');
assert.equal(inspection.original, target); assert.equal(inspection.expected_sha256, expectedHash);
assert.equal(inspection.expected_file_bytes, expectedBytes); assert.equal(inspection.sha256_before, expectedHash);
assert.equal(inspection.sha256_after, expectedHash);
assert(inspection.source_stat_stable && inspection.source_hash_preserved && inspection.source_closed && inspection.byte_coverage_verified);
assert.deepEqual(inspection.failures, []);
const selectedWindows = inspection.windows.filter(w => w.id === '428690-exact'); assert.equal(selectedWindows.length, 1);
const selectedWindow = selectedWindows[0];
assert.equal(selectedWindow.begin_rva, '0x428690'); assert.equal(selectedWindow.end_rva_exclusive, '0x4286ee');
assert(selectedWindow.exact_pdata_boundary && selectedWindow.byte_coverage_verified);
assert.equal(selectedWindow.window_sha256, '963704100fe2b0b00eab40944e3ad50c2fc71af634405b512dc9f8135e9c94e8');
let instructionCursor = 0x180428690n;
for (const row of instructions) {
  assert.equal(BigInt(row.address), instructionCursor);
  assert(/^(?:[0-9a-f]{2})+$/.test(row.bytes)); instructionCursor += BigInt(row.bytes.length / 2);
}
assert.equal(instructionCursor, 0x1804286een);
const archivedBody = Buffer.from(instructions.map(r => r.bytes).join(''), 'hex');
assert.equal(archivedBody.length, 94); assert.equal(hash(archivedBody), selectedWindow.window_sha256);
function instruction(va, bytes) {
  const rows = instructions.filter(r => r.address === va); assert.equal(rows.length, 1);
  assert.equal(rows[0].bytes, bytes); return rows[0];
}
const fixedCaller = [
  instruction('0x18042869a','33ff'),
  instruction('0x18042869c','488d052ddfbeff'),
  instruction('0x1804286a3','488bd9'),
  instruction('0x1804286a9','4883c118'),
  instruction('0x1804286ad','4889442420'),
  instruction('0x1804286b2','4c8d0d57e0beff'),
  instruction('0x1804286b9','8d5728'),
  instruction('0x1804286bc','448d470a'),
  instruction('0x1804286c0','e80b281b00'),
];
for (const [va, raw, displacement, expected] of [
  ['0x18042869c','488d052ddfbeff',3,0x165d0],
  ['0x1804286b2','4c8d0d57e0beff',3,0x16710],
  ['0x1804286c0','e80b281b00',1,0x5daed0],
]) {
  const bytes = Buffer.from(instruction(va, raw).bytes, 'hex');
  assert.equal(BigInt(va) + BigInt(bytes.length) + BigInt(bytes.readInt32LE(displacement)), imageBase + BigInt(expected));
}
assert.equal(helpers.status, 'fixed-helper-boundaries-assessed'); assert.equal(helpers.target, target);
assert.equal(helpers.expected_sha256, expectedHash); assert.equal(helpers.expected_file_bytes, expectedBytes);
assert.equal(helpers.sha256_before, expectedHash); assert.equal(helpers.sha256_after, expectedHash);
assert(helpers.source_stat_stable && helpers.source_hash_preserved);
assert.equal(secondary.status, 'gap-without-exact-begin'); assert.equal(secondary.target, target);
assert.equal(secondary.expected_sha256, expectedHash); assert.equal(secondary.expected_bytes, expectedBytes);
assert.equal(secondary.sha256_before, expectedHash); assert.equal(secondary.sha256_after, expectedHash);
assert(secondary.source_stat_stable && secondary.source_hash_preserved && secondary.source_handle_closed);
assert.deepEqual(secondary.failures, []);
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
  [4, secondary.metadata_reads],
]) {
  for (const r of reads) {
    assert.equal(r.requested_bytes, 12); assert.equal(r.read_bytes, 12);
    const index = (r.file_offset - pe.exception_file_offset) / 12, bytes = Buffer.from(r.raw_hex, 'hex');
    const entry = decode(index, bytes, 'cached-source-' + sourceIndex);
    if (cache.has(index)) assert.equal(cache.get(index).raw_hex, entry.raw_hex); else cache.set(index, entry);
  }
}
for (const r of helpers.observed_entries) {
  assert.equal(r.file_offset, pe.exception_file_offset + r.index * 12);
  assert.equal(r.pdata_rva, hex(Number(pe.exception_rva) + r.index * 12));
  const entry = decode(r.index, Buffer.from(r.raw_bytes, 'hex'), 'cached-source-3');
  assert.equal(entry.begin_rva, r.begin_rva); assert.equal(entry.end_rva_exclusive, r.end_rva_exclusive);
  assert.equal(entry.unwind_rva, r.unwind_info_rva);
  if (cache.has(r.index)) assert.equal(cache.get(r.index).raw_hex, entry.raw_hex); else cache.set(r.index, entry);
}
const iterator = cache.get(20611); assert(iterator);
assert.equal(iterator.begin, 0x5daed0); assert.equal(iterator.end, 0x5daf3e);
assert.equal(iterator.raw_hex, 'd0ae5d003eaf5d0064038700');
const report = {schema:'grim-dynasty-fixed-item-950-elements-boundaries-v1', status:'started', started_utc:new Date().toISOString(),
  target, expected_sha256:expectedHash, expected_bytes:expectedBytes,
  requested:requested.map(r => ({role:r.role,rva:hex(r.rva),cached_only:r.cached_only})),
  new_metadata_target_rvas:['0x16710','0x165d0'], cached_iterator_entry:iterator,
  source_evidence:sources, caller_instructions:fixedCaller,
  caller_arguments:{rcx:'this+0x18',edx:'0x28',r8d:'0xa',r9:'0x180016710',stack_fifth:'0x1800165d0'},
  pe, limits, target_open_mode:'O_RDONLY', target_loaded_or_executed:false, child_process_started:false,
  backend_started:false, code_bytes_read:0, unwind_body_bytes_read:0, new_header_bytes_read:0,
  target_copy_created:false, whole_exception_table_scanned:false, metadata_bytes:0, metadata_read_operations:0,
  metadata_reads:[], cached_entries_available:cache.size,
  fingerprint_passes:[], assessments:[], exact_boundaries:[], total_search_steps:0,
  limitations:[
    'The94B saved Game428690 body proves CALL5DAED0 and address-taken callbacks16710/165D0. Cached raw entry20611 alone resolves5DAED0; its metadata must never be reread or searched if cache validation fails.',
    'Only16710/165D0 permit new metadata lookup, at most32 steps each. Existing same-identity raw .pdata entries are reused across targets. Only a missing current entry and one necessary predecessor may supplement each search; no extra successor or neighboring body is selected.',
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
  for (const selected of requested) {
    const requestedRva = selected.rva, assessment = {role:selected.role,requested_rva:hex(requestedRva),
      cached_only:selected.cached_only,search_steps:0,probes:[],exact_boundary:null};
    report.assessments.push(assessment);
    const cachedExact = [...cache.values()].filter(e => e.begin === requestedRva); assert(cachedExact.length <= 1);
    let lo = 0, hi = pe.entry_count;
    if (cachedExact.length) { lo = cachedExact[0].index; assessment.lookup_method = 'cached-exact-entry'; }
    else {
      assert(!selected.cached_only, 'Iterator requires its existing complete raw entry; no fresh fallback');
      assessment.lookup_method = 'bounded-lower-bound';
      while (lo < hi) {
        assert(assessment.search_steps < limits.search_steps); assessment.search_steps++; report.total_search_steps++;
        const mid = lo + Math.floor((hi - lo) / 2), row = entry(mid); assessment.probes.push(row);
        if (row.begin < requestedRva) lo = mid + 1; else hi = mid;
      }
    }
    assessment.lower_bound_index = lo;
    const current = lo < pe.entry_count ? entry(lo) : null; assessment.lower_bound_entry = current;
    if (current && current.begin === requestedRva) {
      assessment.status = 'exact-begin'; assessment.exact_boundary = current; report.exact_boundaries.push(current);
      assessment.previous_known = cache.get(lo - 1) || null; assessment.next_known = cache.get(lo + 1) || null;
    } else {
      const previous = lo > 0 ? entry(lo - 1) : null; assessment.previous_entry = previous;
      assessment.status = previous && previous.begin < requestedRva && requestedRva < previous.end ? 'containing-without-exact-begin' :
        !previous ? 'before-first-entry' : !current ? 'after-last-entry' : 'gap-without-exact-begin';
      assessment.selected_code_range = null;
    }
  }
  assert(!report.metadata_reads.some(r => r.index === 20611), 'Cached iterator entry was reread');
  report.status = 'fixed-item-950-element-boundaries-assessed';
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
console.log(JSON.stringify({status:report.status, assessments:report.assessments.map(a => ({rva:a.requested_rva,status:a.status,steps:a.search_steps})),
  metadata_bytes:report.metadata_bytes, metadata_read_operations:report.metadata_read_operations,
  total_search_steps:report.total_search_steps, source_hash_preserved:report.source_hash_preserved, failures}));
if (failures.length) process.exitCode = 1;
