// PREPARED ONLY. One exact Engine body and two caller-proven data locations.
// Execution requires separate review. No callee following, target loading or build.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_remaining_dependencies.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', size = 4485632;
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/object-lifecycle/offline-actor-remaining-dependencies');
const temp = path.join(root, '.tooling/offline-actor-remaining-dependencies-temp');
const sources = [
  ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json', '39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
  ['docs/object-lifecycle/offline-actor-constructor-body/0259a0-instructions.json', '59f402f428e04e2928bb485e58fa85896ac08db4548bff6fa33444a58fa521a7'],
];
const target = 0xbdbe0, base = 0x180000000n;
const limits = Object.freeze({code_bytes:4096, metadata_bytes:384, pdata_entries:32, search_steps:32,
  requested_data_bytes:12, semantic_read_operations:34, proof_bytes_each:32768,
  fingerprint_chunk_bytes:65536, tool_timeout_ms:10000, tool_max_buffer_bytes:196608, report_bytes:131072});
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
function normal(p, file = false) {
  let first = true;
  for (let c = path.resolve(p);;) {
    const s = fs.lstatSync(c);
    assert(!s.isSymbolicLink(), 'Redirected path refused');
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
const proof = proofs[0], pe = proof.pe;
assert.equal(proof.status, 'exact-actor-export-and-boundary-verified');
assert.equal(proof.original, original); assert.equal(proof.expected_sha256, expected);
assert.equal(proof.sha256_before, expected); assert.equal(proof.sha256_after, expected);
assert(proof.source_stat_stable && proof.source_hash_preserved); assert.deepEqual(proof.failures, []);
assert.equal(pe.image_base, hex(base)); assert.equal(pe.image_size, 4521984);
assert.equal(pe.entry_count, 12210); assert.equal(pe.exception_rva, '0x424000');
assert.equal(pe.exception_bytes, 146520); assert.equal(pe.exception_file_offset, 4317696);
assert.equal(pe.exception_bytes, pe.entry_count * 12);
function instruction(va, bytes, text, displacementAt, rva) {
  const found = proofs[1].filter(row => row.address === va); assert.equal(found.length, 1);
  const row = found[0]; assert.equal(row.bytes, bytes); assert.equal(row.instruction, text);
  const b = Buffer.from(bytes, 'hex');
  assert.equal(BigInt(va) + BigInt(b.length) + BigInt(b.readInt32LE(displacementAt)), base + BigInt(rva));
  return row;
}
const sourceCall = instruction('0x180025cc9', 'e8127f0900', 'call        00000001800BDBE0', 1, target);
const dataTargets = [
  {rva:0x34bb44, bytes:4, role:'MOVSS file constant', section:'.rdata',
    instruction:instruction('0x180025d15', 'f30f100d275e3200', 'movss       xmm1,dword ptr [000000018034BB44h]', 4, 0x34bb44)},
  {rva:0x421428, bytes:8, role:'global pointer slot file-initial state only', section:'.data',
    instruction:instruction('0x180025d3d', '488b0de4b63f00', 'mov         rcx,qword ptr [0000000180421428h]', 3, 0x421428)},
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
const report = {schema:'grim-dynasty-fixed-actor-remaining-dependencies-v1', status:'started',
  started_utc:new Date().toISOString(), original, expected_sha256:expected, expected_file_bytes:size,
  source_evidence:sources.map(([path, sha256]) => ({path, sha256})), source_call:sourceCall,
  requested_rva:hex(target), requested_data:dataTargets.map(d => ({...d, rva:hex(d.rva)})), limits,
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, runtime_value_observed:false,
  backend_started:false, build_or_test_execution:false, original_modified:false, full_target_copy_created:false,
  permission_change:false, tool, expected_tool_sha256:toolHash, metadata_bytes:0, code_bytes:0,
  data_bytes:0, virtual_zero_fill_bytes:0, reads:[], data_results:[], fingerprint_passes:[], subprocesses:0,
  body_read:false, limitations:[
    'Only exact BeginAddress BDBE0 with size <=4096 admits one bounded dumpbin call. Missing/oversize boundaries stop before code or fixed data reads.',
    'At most 32 distinct 12-byte .pdata entries; only encountered order is checked. No neighbor body, unwind/EH data, callee or additional target is followed.',
    'The fixed data domain is 4 bytes at 34BB44 and 8 bytes at 421428. A wholly virtual-only .data slot is reported from pinned PE metadata as file-initial zero-fill, not read beyond raw data.',
    'No global pointer dereference, loaded process memory, runtime initializer or full table is read. File initial state is not loaded runtime state.',
    'Two same-handle full-file SHA256 passes use 64KiB buffers and are separate from semantic budgets. Hash/stat stability is not an atomic snapshot.',
    'dumpbin performs static inspection only; its internal PE metadata I/O is not instrumented by these reader counters. Byte coverage does not prove a complete logical function or native EH.',
  ]};
let fd, stat, outCreated = false, toolAttempted = false;
const failures = [], cache = new Map();
const save = (name, value) => { normal(out); fs.writeFileSync(path.join(out, name), value, {flag:'wx'}); };
function read(at, n, kind) {
  assert(fits(at, n, size)); assert(report.reads.length < limits.semantic_read_operations);
  if (kind === 'pdata') { assert.equal(n, 12); assert(report.metadata_bytes + n <= limits.metadata_bytes); }
  else if (kind === 'code') { assert(n <= limits.code_bytes); assert.equal(report.code_bytes, 0); }
  else { assert.equal(kind, 'data'); assert(report.data_bytes + n <= limits.requested_data_bytes); }
  const b = Buffer.alloc(n), row = {kind, file_offset:at, requested_bytes:n, read_bytes:0};
  report.reads.push(row);
  const got = fs.readSync(fd, b, 0, n, at); row.read_bytes = got;
  if (kind === 'pdata') report.metadata_bytes += got;
  else if (kind === 'code') report.code_bytes += got; else report.data_bytes += got;
  row.raw_hex = b.subarray(0, got).toString('hex');
  assert.equal(got, n, 'Short semantic read; no retry or expansion');
  return b;
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
  function entry(i) {
    assert(Number.isInteger(i) && i >= 0 && i < pe.entry_count);
    if (cache.has(i)) return cache.get(i); assert(cache.size < limits.pdata_entries);
    const b = read(pd.offset + i * 12, 12, 'pdata');
    const e = {index:i, begin:b.readUInt32LE(0), end:b.readUInt32LE(4), unwind:b.readUInt32LE(8), raw_hex:b.toString('hex')};
    assert(e.begin < e.end && e.end <= pe.image_size);
    for (const [j, o] of cache) assert(j < i ? o.begin <= e.begin : o.begin >= e.begin);
    cache.set(i, e); return e;
  }
  let lo = 0, hi = pe.entry_count, steps = 0;
  while (lo < hi) {
    assert(steps++ < limits.search_steps); const mid = lo + Math.floor((hi - lo) / 2), e = entry(mid);
    if (e.begin < target) lo = mid + 1; else hi = mid;
  }
  report.search_steps = steps; report.lower_bound_index = lo;
  const e = lo < pe.entry_count ? entry(lo) : null; report.boundary = e;
  if (!e || e.begin !== target) {
    report.status = 'no-exact-entry-body-and-data-not-read';
    report.not_read_reason = 'No exact BeginAddress; no containing or neighboring body selected';
  } else if (e.end - e.begin > limits.code_bytes) {
    report.status = 'exact-boundary-exceeds-body-budget';
    report.not_read_reason = 'Body exceeds 4096 bytes; no body or data read';
  } else {
    const mapped = mapping(e.begin, e.end - e.begin);
    assert.equal(mapped.section.name, '.text'); assert(mapped.section.characteristics & 0x20000000);
    report.code_file_offset = mapped.offset;
    const bytes = read(mapped.offset, e.end - e.begin, 'code'); report.body_read = true; report.code_sha256 = hash(bytes);
    save('0bdbe0.bin', bytes);
    const start = base + BigInt(e.begin), end = base + BigInt(e.end);
    const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES',
      '/RANGE:' + hex(start) + ',' + hex(end - 1n), original];
    report.command_arguments = args; report.subprocesses = 1; const started = Date.now();
    const child = spawnSync(tool, args, {cwd:root, windowsHide:true, encoding:'utf8',
      timeout:limits.tool_timeout_ms, maxBuffer:limits.tool_max_buffer_bytes,
      env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
    report.tool_elapsed_ms = Date.now() - started; report.tool_exit = child.status;
    report.tool_signal = child.signal; report.tool_error = child.error ? String(child.error) : null;
    save('0bdbe0-dumpbin.txt', child.stdout || ''); save('0bdbe0-stderr.txt', child.stderr || '');
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
    for (const row of rows) {
      assert(row.instruction && row.bytes.length > 0 && row.bytes.length <= 30 && row.bytes.length % 2 === 0);
      assert.equal(row.address, cursor); cursor += BigInt(row.bytes.length / 2); assert(cursor <= end);
    }
    assert.equal(cursor, end); assert.equal(rows.map(r => r.bytes).join(''), bytes.toString('hex'));
    report.byte_coverage_verified = true; report.instructions = rows.length;
    save('0bdbe0-instructions.json', JSON.stringify(rows.map(r => ({...r, address:hex(r.address)})), null, 2) + '\n');
    for (const d of dataTargets) {
      const m = mapping(d.rva, d.bytes, d.rva === 0x421428);
      assert.equal(m.section.name, d.section); assert(!(m.section.characteristics & 0x20000000));
      assert(m.section.characteristics & 0x40000000);
      const result = {rva:hex(d.rva), bytes:d.bytes, role:d.role, section:m.section.name,
        section_rva:hex(m.section.rva), section_raw_size:m.section.raw_size, section_virtual_size:m.section.virtual_size,
        section_characteristics:hex(m.section.characteristics), kind:m.kind, file_offset:m.offset, runtime_observed:false};
      if (m.kind === 'virtual-zero-fill') {
        assert.equal(d.rva, 0x421428); assert.equal(d.bytes, 8);
        report.virtual_zero_fill_bytes += d.bytes; result.file_initial_zero_fill_hex = '0000000000000000';
        result.file_bytes_read = 0; result.loaded_pointer_value = null;
      } else {
        const b = read(m.offset, d.bytes, 'data'); result.raw_hex = b.toString('hex'); result.file_bytes_read = b.length;
        result.unsigned_little_endian_hex = d.bytes === 4 ? hex(b.readUInt32LE(0)) : hex(b.readBigUInt64LE(0));
        if (d.rva === 0x421428) result.loaded_pointer_value = null;
      }
      report.data_results.push(result);
    }
    assert.equal(report.data_bytes + report.virtual_zero_fill_bytes, limits.requested_data_bytes);
    report.status = 'fixed-actor-remaining-dependencies-verified';
  }
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
console.log(JSON.stringify({status:report.status, boundary:report.boundary, metadata_bytes:report.metadata_bytes,
  code_bytes:report.code_bytes, data_bytes:report.data_bytes, virtual_zero_fill_bytes:report.virtual_zero_fill_bytes,
  instructions:report.instructions, subprocesses:report.subprocesses, original_unchanged:report.source_hash_preserved,
  tool_unchanged:report.tool_hash_preserved, failures}));
if (failures.length) process.exitCode = 1;
