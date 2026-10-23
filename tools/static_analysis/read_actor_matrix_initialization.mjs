// PREPARED ONLY. Two caller-proven slow-path bodies and three fixed data spans.
// No execution until separately reviewed; no neighboring body or callee following.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_matrix_initialization.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', size = 4485632;
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/object-lifecycle/offline-actor-matrix-initialization');
const temp = path.join(root, '.tooling/offline-actor-matrix-initialization-temp');
const sources = [
  ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json', '39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
  ['docs/object-lifecycle/offline-actor-subobject-direct-dependencies/273ab0-instructions.json', '0fc850ae074a5f4f4ea6046d79f0a96f13c731314a34d40af9b6d16e6d58a3b7'],
  ['docs/object-lifecycle/offline-actor-subobject-direct-dependencies/inspection.json', '50c454f0f9fa6ffd0d38852903e9850d12f7e1cf82b0b2d3dca4e0fc8b95cb12'],
];
const base = 0x180000000n;
const limits = Object.freeze({code_bytes_each:4096, code_bytes_total:8192, metadata_bytes:768,
  pdata_entries_each:32, pdata_entries_total:64, search_steps_each:32, data_bytes:56,
  semantic_read_operations:69, proof_bytes_each:32768, fingerprint_chunk_bytes:65536,
  subprocesses:2, tool_timeout_ms_each:10000, tool_max_buffer_bytes_each:196608, report_bytes:196608});
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
function normal(p, file = false) {
  let first = true;
  for (let c = path.resolve(p);;) {
    const s = fs.lstatSync(c); assert(!s.isSymbolicLink(), 'Redirected path refused');
    assert(first && file ? s.isFile() : s.isDirectory(), 'Unexpected path type');
    assert.equal(fs.realpathSync(c).toLowerCase(), c.toLowerCase(), 'Redirected ancestor refused');
    const d = path.dirname(c); if (d === c) return; c = d; first = false;
  }
}
function absent(p) {
  try { fs.lstatSync(p); } catch (e) { if (e.code === 'ENOENT') return; throw e; }
  assert.fail('Existing evidence/temp path refused');
}
function stable(a, b) {
  return b.isFile() && a.size === b.size && a.ino === b.ino && a.dev === b.dev &&
    a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs;
}
function fingerprint(fd, length) {
  assert.equal(fs.fstatSync(fd).size, length, 'Fingerprint size changed');
  const b = Buffer.alloc(limits.fingerprint_chunk_bytes), h = createHash('sha256'); let at = 0;
  while (at < length) {
    const n = fs.readSync(fd, b, 0, Math.min(b.length, length - at), at);
    assert(n > 0, 'Unexpected fingerprint EOF'); h.update(b.subarray(0, n)); at += n;
  }
  return {sha256:h.digest('hex'), bytes:at, chunk_bytes:b.length};
}
function hashFile(p) {
  normal(p, true); const fd = fs.openSync(p, fs.constants.O_RDONLY);
  try { const s = fs.fstatSync(fd), result = fingerprint(fd, s.size); assert(stable(s, fs.fstatSync(fd))); return result; }
  finally { fs.closeSync(fd); }
}
function fits(at, n, length) {
  return Number.isSafeInteger(at) && at >= 0 && Number.isSafeInteger(n) && n > 0 &&
    Number.isSafeInteger(length) && at <= length && n <= length - at;
}
normal(path.dirname(out)); normal(path.dirname(temp)); absent(out); absent(temp);
const proofs = sources.map(([p, h]) => {
  const full = path.join(root, p); normal(full, true); assert(fs.statSync(full).size <= limits.proof_bytes_each);
  const b = fs.readFileSync(full); assert(b.length <= limits.proof_bytes_each); assert.equal(hash(b), h);
  return JSON.parse(b.toString('utf8'));
});
const proof = proofs[0], pe = proof.pe, bodyProof = proofs[2];
assert.equal(proof.status, 'exact-actor-export-and-boundary-verified');
assert.equal(bodyProof.status, 'fixed-actor-subobject-direct-dependencies-verified');
for (const p of [proof, bodyProof]) {
  assert.equal(p.original, original); assert.equal(p.expected_sha256, expected);
  assert.equal(p.sha256_before, expected); assert.equal(p.sha256_after, expected);
  assert(p.source_stat_stable && p.source_hash_preserved); assert.deepEqual(p.failures, []);
}
assert(bodyProof.source_handle_closed && bodyProof.tool_hash_preserved);
const callerTargets = bodyProof.targets.filter(t => t.rva === '0x273ab0');
assert.equal(callerTargets.length, 1);
const caller = callerTargets[0];
assert.equal(caller.status, 'exact-body-disassembly-verified'); assert(caller.byte_coverage_verified);
assert.equal(caller.selected_code_range.begin, 0x273ab0); assert.equal(caller.selected_code_range.end_exclusive, 0x273b29);
assert.equal(caller.code_bytes, 121); assert.equal(caller.instructions, 25);
assert.equal(pe.image_base, hex(base)); assert.equal(pe.image_size, 4521984);
assert.equal(pe.entry_count, 12210); assert.equal(pe.exception_rva, '0x424000');
assert.equal(pe.exception_bytes, 146520); assert.equal(pe.exception_file_offset, 4317696);
assert.equal(pe.exception_bytes, pe.entry_count * 12);
function row(va, bytes, text) {
  const found = proofs[1].filter(r => r.address === va); assert.equal(found.length, 1);
  assert.equal(found[0].bytes, bytes); assert.equal(found[0].instruction, text); return found[0];
}
function relative(va, bytes, text, displacementAt, rva) {
  const r = row(va, bytes, text), b = Buffer.from(bytes, 'hex');
  assert.equal(BigInt(va) + BigInt(b.length) + BigInt(b.readInt32LE(displacementAt)), base + BigInt(rva));
  return r;
}
const targets = [
  {rva:0x2dcae0, role:'first direct slow-path helper',
    evidence:[relative('0x180273aea', 'e8f18f0600', 'call        00000001802DCAE0', 1, 0x2dcae0)]},
  {rva:0x2dca80, role:'second direct slow-path helper',
    evidence:[relative('0x180273b22', 'e8598f0600', 'call        00000001802DCA80', 1, 0x2dca80)]},
];
const dataTargets = [
  {rva:0x41d490, bytes:48, role:'returned source span; file observation only',
    evidence:relative('0x180273ad7', '488d05b2991a00', 'lea         rax,[000000018041D490h]', 3, 0x41d490)},
  {rva:0x423f88, bytes:4, role:'static guard span; file/virtual initial state only',
    evidence:relative('0x180273acf', '3905b3041b00', 'cmp         dword ptr [0000000180423F88h],eax', 2, 0x423f88)},
  {rva:0x420dd4, bytes:4, role:'TLS index span; file/virtual initial state only',
    evidence:relative('0x180273abd', '8b0d11d31a00', 'mov         ecx,dword ptr [0000000180420DD4h]', 2, 0x420dd4)},
];
function mapping(rva, n, allowZeroFill = false) {
  assert(fits(rva, n, pe.image_size));
  const matches = pe.sections.filter(s => rva >= s.rva && fits(rva - s.rva, n, s.virtual_size));
  assert.equal(matches.length, 1, 'Require unique section mapping');
  const s = matches[0], delta = rva - s.rva;
  if (fits(delta, n, s.raw_size)) {
    const offset = s.raw_offset + delta; assert(fits(offset, n, size));
    return {section:s, kind:'file-backed', offset};
  }
  assert(allowZeroFill && delta >= s.raw_size, 'Partial/unbacked span refused');
  return {section:s, kind:'virtual-zero-fill', offset:null};
}
const report = {schema:'grim-dynasty-fixed-actor-matrix-initialization-v1', status:'started',
  started_utc:new Date().toISOString(), original, expected_sha256:expected, expected_file_bytes:size,
  source_evidence:sources.map(([path, sha256]) => ({path, sha256})), limits,
  requested_targets:targets.map(t => ({...t, rva:hex(t.rva)})),
  requested_data:dataTargets.map(d => ({...d, rva:hex(d.rva)})),
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, runtime_value_observed:false,
  backend_started:false, build_or_test_execution:false, original_modified:false, full_target_copy_created:false,
  permission_change:false, tool, expected_tool_sha256:toolHash, metadata_bytes:0, code_bytes:0,
  data_bytes:0, virtual_zero_fill_bytes:0, reads:[], data_results:[], fingerprint_passes:[], subprocesses:0, targets:[],
  limitations:[
    'Only direct targets 2DCAE0 and 2DCA80 from the hash-pinned 273AB0 body are searched. Exact BeginAddress and <=4096 bytes each / <=8192 bytes total are required for at most two bounded dumpbin calls.',
    'Each target visits at most 32 .pdata entries including a necessary previous entry for no-exact classification. Shared raw-entry cache bounds new metadata to 64 entries / 768 bytes.',
    'Missing exact entries record lower-bound and adjacent metadata only; containing/gap never authorizes neighboring code, a guessed prefix, or another target. Other fixed independent targets may continue.',
    'The data domain is exactly 41D490/48B, 423F88/4B and 420DD4/4B. Wholly virtual-only .data spans are reported from pinned PE metadata as file-initial zero-fill without reading beyond raw data.',
    'File initial bytes and virtual zero-fill do not establish loaded TLS blocks, thread epochs, guard state, synchronization or source contents. No pointer is dereferenced and no other global or callee is followed.',
    'Two same-handle full-file fingerprints use 64KiB buffers and are separate from semantic budgets. Hash/stat stability is not an atomic snapshot.',
    'dumpbin internal PE metadata I/O is not instrumented. Exact byte coverage does not prove complete logical control flow or native EH.',
  ]};
let fd, stat, outCreated = false, toolAttempted = false;
const failures = [], cache = new Map();
const save = (name, value) => { normal(out); fs.writeFileSync(path.join(out, name), value, {flag:'wx'}); };
function read(at, n, kind, owner) {
  assert(fits(at, n, size)); assert(report.reads.length < limits.semantic_read_operations);
  if (kind === 'pdata') { assert.equal(n, 12); assert(report.metadata_bytes + n <= limits.metadata_bytes); }
  else if (kind === 'code') {
    assert(n <= limits.code_bytes_each); assert(report.code_bytes + n <= limits.code_bytes_total);
  } else { assert.equal(kind, 'data'); assert(report.data_bytes + n <= limits.data_bytes); }
  const b = Buffer.alloc(n), r = {kind, owner, file_offset:at, requested_bytes:n, read_bytes:0};
  report.reads.push(r); const got = fs.readSync(fd, b, 0, n, at); r.read_bytes = got;
  if (kind === 'pdata') report.metadata_bytes += got; else if (kind === 'code') report.code_bytes += got; else report.data_bytes += got;
  r.raw_hex = b.subarray(0, got).toString('hex'); assert.equal(got, n, 'Short semantic read; no retry or expansion');
  return b;
}
function disassemble(item, boundary) {
  const m = mapping(boundary.begin, boundary.end - boundary.begin);
  assert.equal(m.section.name, '.text'); assert(m.section.characteristics & 0x20000000);
  item.code_file_offset = m.offset;
  const bytes = read(m.offset, boundary.end - boundary.begin, 'code', item.rva);
  item.code_bytes = bytes.length; item.code_sha256 = hash(bytes); item.body_read = true;
  const stem = boundary.begin.toString(16).padStart(6, '0'); save(stem + '.bin', bytes);
  const start = base + BigInt(boundary.begin), end = base + BigInt(boundary.end);
  const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES',
    '/RANGE:' + hex(start) + ',' + hex(end - 1n), original];
  assert(report.subprocesses < limits.subprocesses); report.subprocesses++;
  item.command_arguments = args; const started = Date.now();
  const child = spawnSync(tool, args, {cwd:root, windowsHide:true, encoding:'utf8',
    timeout:limits.tool_timeout_ms_each, maxBuffer:limits.tool_max_buffer_bytes_each,
    env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
  item.tool_elapsed_ms = Date.now() - started; item.tool_exit = child.status;
  item.tool_signal = child.signal; item.tool_error = child.error ? String(child.error) : null;
  save(stem + '-dumpbin.txt', child.stdout || ''); save(stem + '-stderr.txt', child.stderr || '');
  assert.equal(child.error, undefined); assert.equal(child.status, 0);
  assert.equal(child.signal, null); assert.equal(child.stderr, '');
  const rows = [];
  for (const line of child.stdout.split(/\r?\n/)) {
    const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if (m) {
      rows.push({address:BigInt('0x' + m[1]), bytes:m[2].replace(/\s/g, '').toLowerCase(), instruction:m[3].trim()});
      continue;
    }
    const c = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
    if (c) { assert(rows.length); rows[rows.length - 1].bytes += c[1].replace(/\s/g, '').toLowerCase(); }
  }
  let cursor = start;
  for (const r of rows) {
    assert(r.instruction && r.bytes.length > 0 && r.bytes.length <= 30 && r.bytes.length % 2 === 0);
    assert.equal(r.address, cursor); cursor += BigInt(r.bytes.length / 2); assert(cursor <= end);
  }
  assert.equal(cursor, end); assert.equal(rows.map(r => r.bytes).join(''), bytes.toString('hex'));
  item.byte_coverage_verified = true; item.instructions = rows.length;
  item.instructions_file = stem + '-instructions.json';
  save(item.instructions_file, JSON.stringify(rows.map(r => ({...r, address:hex(r.address)})), null, 2) + '\n');
  item.status = 'exact-body-disassembly-verified';
}
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.script_sha256 = hashFile(script).sha256;
  toolAttempted = true; report.tool_before = hashFile(tool); assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY); stat = fs.fstatSync(fd);
  assert(stat.isFile() && stat.size === size);
  report.stat_before = {size:stat.size, dev:stat.dev, ino:stat.ino, mtime_ms:stat.mtimeMs, ctime_ms:stat.ctimeMs};
  const before = fingerprint(fd, size); report.fingerprint_passes.push({label:'before', ...before});
  report.sha256_before = before.sha256; assert.equal(before.sha256, expected);
  const pd = mapping(Number(pe.exception_rva), pe.exception_bytes);
  assert.equal(pd.section.name, '.pdata'); assert.equal(pd.offset, pe.exception_file_offset);
  for (const target of targets) {
    const item = {rva:hex(target.rva), role:target.role, status:'searching', body_read:false, code_bytes:0,
      byte_coverage_verified:false, selected_code_range:null};
    report.targets.push(item); const visited = new Set(), metadataBefore = report.metadata_bytes;
    function entry(i) {
      assert(Number.isInteger(i) && i >= 0 && i < pe.entry_count);
      if (!visited.has(i)) { assert(visited.size < limits.pdata_entries_each); visited.add(i); }
      if (cache.has(i)) return cache.get(i);
      assert(cache.size < limits.pdata_entries_total);
      const b = read(pd.offset + i * 12, 12, 'pdata', item.rva);
      const e = {index:i, begin:b.readUInt32LE(0), end:b.readUInt32LE(4), unwind:b.readUInt32LE(8), raw_hex:b.toString('hex')};
      assert(e.begin < e.end && e.end <= pe.image_size);
      for (const [j, o] of cache) assert(j < i ? o.begin <= e.begin : o.begin >= e.begin);
      cache.set(i, e); return e;
    }
    let lo = 0, hi = pe.entry_count, steps = 0;
    while (lo < hi) {
      assert(steps++ < limits.search_steps_each);
      const mid = lo + Math.floor((hi - lo) / 2), e = entry(mid);
      if (e.begin < target.rva) lo = mid + 1; else hi = mid;
    }
    item.search_steps = steps; item.lower_bound_index = lo;
    const e = lo < pe.entry_count ? entry(lo) : null; item.lower_bound_entry = e;
    if (!e || e.begin !== target.rva) {
      const previous = lo > 0 ? entry(lo - 1) : null;
      item.previous_entry = previous;
      item.classification = previous && previous.begin < target.rva && target.rva < previous.end ? 'containing-without-exact-begin' : 'gap-no-exact-begin';
      item.status = 'no-exact-entry-code-not-read';
      item.not_read_reason = 'No exact BeginAddress; neither containing/neighbor body nor a leaf prefix is selected';
    } else if (e.end - e.begin > limits.code_bytes_each) {
      item.classification = 'exact'; item.status = 'exact-body-exceeds-individual-budget';
    } else if (report.code_bytes + e.end - e.begin > limits.code_bytes_total) {
      item.classification = 'exact'; item.status = 'exact-body-exceeds-remaining-total-budget';
    } else {
      item.classification = 'exact'; item.selected_code_range = {begin:e.begin, end_exclusive:e.end};
      disassemble(item, e);
    }
    item.visited_entry_indices = [...visited]; item.new_metadata_bytes = report.metadata_bytes - metadataBefore;
  }
  for (const d of dataTargets) {
    const m = mapping(d.rva, d.bytes, true);
    assert.equal(m.section.name, '.data'); assert(!(m.section.characteristics & 0x20000000));
    assert(m.section.characteristics & 0x40000000);
    const result = {rva:hex(d.rva), bytes:d.bytes, role:d.role, section:m.section.name,
      section_rva:hex(m.section.rva), section_raw_size:m.section.raw_size, section_virtual_size:m.section.virtual_size,
      section_characteristics:hex(m.section.characteristics), kind:m.kind, file_offset:m.offset, runtime_observed:false};
    if (m.kind === 'virtual-zero-fill') {
      result.file_initial_zero_fill_hex = '00'.repeat(d.bytes); result.file_bytes_read = 0;
      report.virtual_zero_fill_bytes += d.bytes;
    } else {
      const bytes = read(m.offset, d.bytes, 'data', hex(d.rva));
      result.raw_hex = bytes.toString('hex'); result.file_bytes_read = bytes.length;
      if (d.bytes === 4) {
        result.unsigned_little_endian_hex = hex(bytes.readUInt32LE(0));
        result.signed_little_endian = bytes.readInt32LE(0);
      } else {
        result.little_endian_qwords = Array.from({length:6}, (_, i) => ({
          byte_offset:i * 8, unsigned_hex:hex(bytes.readBigUInt64LE(i * 8))}));
      }
    }
    report.data_results.push(result);
  }
  assert.equal(report.data_bytes + report.virtual_zero_fill_bytes, limits.data_bytes);
  report.status = report.targets.every(t => t.byte_coverage_verified)
    ? 'fixed-actor-matrix-initialization-verified' : 'fixed-actor-matrix-initialization-partial';
} catch (error) { failures.push(String(error)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (stat && stat.size === size) {
        const after = fingerprint(fd, size); report.fingerprint_passes.push({label:'after', ...after});
        report.sha256_after = after.sha256; assert.equal(after.sha256, expected);
        const last = fs.fstatSync(fd); report.source_stat_stable = stable(stat, last);
        report.stat_after = {size:last.size, dev:last.dev, ino:last.ino, mtime_ms:last.mtimeMs, ctime_ms:last.ctimeMs};
        assert(report.source_stat_stable);
      }
    } catch (e) { failures.push('Original final verification: ' + String(e)); }
    finally { try { fs.closeSync(fd); report.source_handle_closed = true; } catch (e) { failures.push('Original close: ' + String(e)); } }
  }
  if (toolAttempted) {
    try { report.tool_after = hashFile(tool); assert.equal(report.tool_after.sha256, toolHash); }
    catch (e) { failures.push('Tool final verification: ' + String(e)); }
  }
  report.entries = [...cache.values()];
  report.source_hash_preserved = report.sha256_before === expected && report.sha256_after === expected;
  report.tool_hash_preserved = report.tool_before?.sha256 === toolHash && report.tool_after?.sha256 === toolHash;
  if (failures.length) report.status = 'failed'; report.failures = failures;
  report.finished_utc = new Date().toISOString(); report.controller_resource_usage = process.resourceUsage();
  if (outCreated) {
    const encoded = JSON.stringify(report, null, 2) + '\n';
    assert(Buffer.byteLength(encoded, 'utf8') <= limits.report_bytes); save('inspection.json', encoded);
  }
}
console.log(JSON.stringify({status:report.status, targets:report.targets.map(t => ({
  rva:t.rva, status:t.status, classification:t.classification, code_bytes:t.code_bytes, instructions:t.instructions})),
  metadata_bytes:report.metadata_bytes, code_bytes:report.code_bytes, data_bytes:report.data_bytes,
  virtual_zero_fill_bytes:report.virtual_zero_fill_bytes, subprocesses:report.subprocesses, original_unchanged:report.source_hash_preserved,
  tool_unchanged:report.tool_hash_preserved, failures}));
if (failures.length) process.exitCode = 1;
