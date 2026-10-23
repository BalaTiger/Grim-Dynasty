// PREPARED ONLY. Two direct synchronization dependencies, one datum and bounded TLS metadata.
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
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_matrix_sync_dependencies.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', size = 4485632;
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/object-lifecycle/offline-actor-matrix-sync-dependencies');
const temp = path.join(root, '.tooling/offline-actor-matrix-sync-dependencies-temp');
const sources = [
  ['docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json', '39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'],
  ['docs/object-lifecycle/offline-actor-matrix-initialization/2dca80-instructions.json', '50e12e544bce9b0f21de60be8e46f4a1b40d0f895d6edf6e12bce34754a63822'],
  ['docs/object-lifecycle/offline-actor-matrix-initialization/2dcae0-instructions.json', '80a3ae3ef767ed745a2080c4494c193b4a2fae0f9497d8e3df700f4d0e4d0f0d'],
  ['docs/object-lifecycle/offline-actor-matrix-initialization/inspection.json', '54d33ae66b7aea0358d7e652db7743725654584f3cdfe2a5010a6ff183e48a7e'],
  ['docs/object-lifecycle/offline-load-ticks-imports/tick-import-bindings.json', 'eb1e49dfe763e207be743b91d03b2b4c066057063da052cb3e1e3a97807aab0e'],
];
const headerVerifier = {path:'tools/static_analysis/inspect_load_ticks_imports_offline.mjs',
  sha256:'0290c8e077adba02cc4f4d5ca2c05d3d8d2c0198e7712c0f3588b2590847ee4e'};
const base = 0x180000000n;
const limits = Object.freeze({code_bytes_each:4096, code_bytes_total:8192,
  metadata_bytes:816, pdata_metadata_bytes:768, tls_metadata_bytes:48,
  pdata_entries_each:32, pdata_entries_total:64, search_steps_each:32,
  data_bytes:4, tls_template_bytes:64, semantic_read_operations:70,
  proof_bytes_each:65536, fingerprint_chunk_bytes:65536,
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
const proof = proofs[0], pe = proof.pe, bodyProof = proofs[3], headerProof = proofs[4];
const callerRows = [...proofs[1], ...proofs[2]];
assert.equal(proof.status, 'exact-actor-export-and-boundary-verified');
assert.equal(bodyProof.status, 'fixed-actor-matrix-initialization-verified');
for (const p of [proof, bodyProof]) {
  assert.equal(p.original, original); assert.equal(p.expected_sha256, expected);
  assert.equal(p.sha256_before, expected); assert.equal(p.sha256_after, expected);
  assert(p.source_stat_stable && p.source_hash_preserved); assert.deepEqual(p.failures, []);
}
assert(bodyProof.source_handle_closed && bodyProof.tool_hash_preserved);
for (const [rva, end, bytes, count] of [['0x2dca80', 0x2dcae0, 96, 20], ['0x2dcae0', 0x2dcb47, 103, 24]]) {
  const matches = bodyProof.targets.filter(t => t.rva === rva); assert.equal(matches.length, 1);
  const caller = matches[0]; assert.equal(caller.status, 'exact-body-disassembly-verified');
  assert(caller.byte_coverage_verified); assert.equal(caller.selected_code_range.begin, Number(rva));
  assert.equal(caller.selected_code_range.end_exclusive, end);
  assert.equal(caller.code_bytes, bytes); assert.equal(caller.instructions, count);
}
assert.equal(headerProof.status, 'fixed-two-tick-imports-confirmed');
assert.equal(headerProof.original, original); assert.equal(headerProof.expected_target_sha256, expected);
assert.equal(headerProof.original_sha256_before, expected); assert.equal(headerProof.original_sha256_after, expected);
assert.equal(headerProof.script_sha256, headerVerifier.sha256);
const verifierPath = path.join(root, headerVerifier.path); normal(verifierPath, true);
assert(fs.statSync(verifierPath).size <= limits.proof_bytes_each);
const verifierBytes = fs.readFileSync(verifierPath); assert(verifierBytes.length <= limits.proof_bytes_each);
assert.equal(hash(verifierBytes), headerVerifier.sha256);
assert(verifierBytes.toString('utf8').includes('assert.equal(opt.readUInt16LE(0), 0x20b);'));
assert.equal(headerProof.headers.pe_file_offset, 288); assert.equal(headerProof.headers.optional_header_size, 240);
const optionalOffset = headerProof.headers.pe_file_offset + 24;
assert.equal(optionalOffset, 312);
const savedOptionalReads = headerProof.metadata_reads.filter(r => r.category === 'pe-optional');
assert.equal(savedOptionalReads.length, 1); assert.equal(savedOptionalReads[0].file_offset, optionalOffset);
assert.equal(savedOptionalReads[0].bytes, 128);
const tlsFieldOffset = optionalOffset + 112 + 9 * 8;
assert.equal(tlsFieldOffset, 496);
assert(tlsFieldOffset + 8 <= optionalOffset + headerProof.headers.optional_header_size);
assert.equal(pe.image_base, hex(base)); assert.equal(pe.image_size, 4521984);
assert.equal(pe.entry_count, 12210); assert.equal(pe.exception_rva, '0x424000');
assert.equal(pe.exception_bytes, 146520); assert.equal(pe.exception_file_offset, 4317696);
assert.equal(pe.exception_bytes, pe.entry_count * 12);
function row(va, bytes, text) {
  const found = callerRows.filter(r => r.address === va); assert.equal(found.length, 1);
  assert.equal(found[0].bytes, bytes); assert.equal(found[0].instruction, text); return found[0];
}
function relative(va, bytes, text, displacementAt, rva) {
  const r = row(va, bytes, text), b = Buffer.from(bytes, 'hex');
  assert.equal(BigInt(va) + BigInt(b.length) + BigInt(b.readInt32LE(displacementAt)), base + BigInt(rva));
  return r;
}
const targets = [
  {rva:0x2dcb48, role:'footer tail-jump after epoch publication',
    evidence:[relative('0x1802dcadb', 'e968000000', 'jmp         00000001802DCB48', 1, 0x2dcb48)]},
  {rva:0x2dcba4, role:'header direct wait-path call',
    evidence:[relative('0x1802dcb05', 'e89a000000', 'call        00000001802DCBA4', 1, 0x2dcba4)]},
];
const dataTargets = [
  {rva:0x41c51c, bytes:4, role:'global epoch file initial datum only',
    evidence:relative('0x1802dca96', '8b0580fa1300', 'mov         eax,dword ptr [000000018041C51Ch]', 2, 0x41c51c),
    second_reference:relative('0x1802dcb2a', '8b05ecf91300', 'mov         eax,dword ptr [000000018041C51Ch]', 2, 0x41c51c)},
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
const report = {schema:'grim-dynasty-fixed-actor-matrix-sync-dependencies-v1', status:'started',
  started_utc:new Date().toISOString(), original, expected_sha256:expected, expected_file_bytes:size,
  source_evidence:sources.map(([path, sha256]) => ({path, sha256})), header_verifier_source:headerVerifier, limits,
  tls_header_field_provenance:{optional_header_file_offset:optionalOffset, pe32_plus_magic:'0x20b',
    directory_index:9, field_file_offset:tlsFieldOffset, field_bytes:8, directory_bytes:40, max_template_bytes:64},
  requested_targets:targets.map(t => ({...t, rva:hex(t.rva)})),
  requested_data:dataTargets.map(d => ({...d, rva:hex(d.rva)})),
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, runtime_value_observed:false,
  backend_started:false, build_or_test_execution:false, original_modified:false, full_target_copy_created:false,
  permission_change:false, tool, expected_tool_sha256:toolHash, metadata_bytes:0, code_bytes:0,
  data_bytes:0, virtual_zero_fill_bytes:0, pdata_metadata_bytes:0, tls_metadata_bytes:0, tls_template_bytes:0,
  reads:[], data_results:[], fingerprint_passes:[], subprocesses:0, targets:[],
  limitations:[
    'Only 2DCB48 and 2DCBA4, directly referenced by the pinned first-batch bodies, are searched. Exact BeginAddress and <=4096 bytes each / <=8192 total admit at most two bounded dumpbin calls.',
    'Each target visits at most 32 .pdata entries. Shared raw-entry cache bounds new .pdata metadata to 64 entries / 768 bytes. Missing exact entries do not select neighboring code or a guessed prefix.',
    'The only fixed datum is 41C51C/4B. TLS reads are exactly the PE32+ index-9 directory field at file offset 496 (8B), its 40B directory, and a complete <=64B file-backed template wholly within the already recorded .tls section.',
    'TLS AddressOfIndex must equal preferred VA 180420DD4. AddressOfCallbacks, SizeOfZeroFill and Characteristics are recorded only; no callback array, callback code, index storage, loader state or thread TLS block is followed or allocated.',
    'A file TLS template and global epoch initial value do not establish runtime thread epochs, TLS index assignment, callbacks, synchronization or initialization order.',
    'No other callee, import or global target is followed. Two same-handle full-file hashes use 64KiB buffers and are separate from semantic budgets; stable hash/stat is not an atomic snapshot.',
    'dumpbin internal PE metadata I/O is not instrumented. Exact byte coverage does not establish complete logical control flow or native EH.',
  ]};
let fd, stat, outCreated = false, toolAttempted = false;
const failures = [], cache = new Map();
const save = (name, value) => { normal(out); fs.writeFileSync(path.join(out, name), value, {flag:'wx'}); };
function read(at, n, kind, owner) {
  assert(fits(at, n, size)); assert(report.reads.length < limits.semantic_read_operations);
  if (kind === 'pdata') {
    assert.equal(n, 12); assert(report.pdata_metadata_bytes + n <= limits.pdata_metadata_bytes);
    assert(report.metadata_bytes + n <= limits.metadata_bytes);
  } else if (kind === 'tls-metadata') {
    assert(n === 8 || n === 40); assert(report.tls_metadata_bytes + n <= limits.tls_metadata_bytes);
    assert(report.metadata_bytes + n <= limits.metadata_bytes);
  } else if (kind === 'tls-template') {
    assert(n <= limits.tls_template_bytes); assert.equal(report.tls_template_bytes, 0);
  } else if (kind === 'code') {
    assert(n <= limits.code_bytes_each); assert(report.code_bytes + n <= limits.code_bytes_total);
  } else { assert.equal(kind, 'data'); assert(report.data_bytes + n <= limits.data_bytes); }
  const b = Buffer.alloc(n), r = {kind, owner, file_offset:at, requested_bytes:n, read_bytes:0};
  report.reads.push(r); const got = fs.readSync(fd, b, 0, n, at); r.read_bytes = got;
  if (kind === 'pdata') { report.metadata_bytes += got; report.pdata_metadata_bytes += got; }
  else if (kind === 'tls-metadata') { report.metadata_bytes += got; report.tls_metadata_bytes += got; }
  else if (kind === 'tls-template') report.tls_template_bytes += got;
  else if (kind === 'code') report.code_bytes += got; else report.data_bytes += got;
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
    const m = mapping(d.rva, d.bytes);
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
  // The header location is derived only from already pinned PE32+ evidence.
  const field = read(tlsFieldOffset, 8, 'tls-metadata', 'PE32+ TLS directory index 9');
  const tlsRva = field.readUInt32LE(0), tlsSize = field.readUInt32LE(4);
  report.tls_directory_field = {file_offset:tlsFieldOffset, rva:hex(tlsRva), bytes:tlsSize, raw_hex:field.toString('hex')};
  assert(tlsRva > 0, 'TLS directory absent'); assert.equal(tlsSize, 40, 'Unexpected TLS directory size; no expansion');
  const directoryMapping = mapping(tlsRva, 40);
  assert(!(directoryMapping.section.characteristics & 0x20000000), 'TLS directory must not map to executable bytes');
  assert(directoryMapping.section.characteristics & 0x40000000);
  const directory = read(directoryMapping.offset, 40, 'tls-metadata', 'IMAGE_TLS_DIRECTORY64');
  const startVa = directory.readBigUInt64LE(0), endVa = directory.readBigUInt64LE(8);
  const indexVa = directory.readBigUInt64LE(16), callbacksVa = directory.readBigUInt64LE(24);
  report.tls_directory = {rva:hex(tlsRva), file_offset:directoryMapping.offset, raw_hex:directory.toString('hex'),
    start_address_of_raw_data:hex(startVa), end_address_of_raw_data:hex(endVa),
    address_of_index:hex(indexVa), address_of_callbacks:hex(callbacksVa),
    size_of_zero_fill:directory.readUInt32LE(32), characteristics:hex(directory.readUInt32LE(36)),
    callbacks_followed:false, runtime_observed:false};
  assert.equal(indexVa, base + 0x420dd4n, 'Unexpected AddressOfIndex; do not follow');
  const tlsSections = pe.sections.filter(section => section.name === '.tls');
  assert.equal(tlsSections.length, 1); const tlsSection = tlsSections[0];
  assert.equal(tlsSection.rva, 4493312); assert.equal(tlsSection.virtual_size, 9);
  const sectionStart = base + BigInt(tlsSection.rva), sectionEnd = sectionStart + BigInt(tlsSection.virtual_size);
  assert(startVa >= sectionStart && endVa >= startVa && endVa <= sectionEnd, 'Template must fit recorded .tls virtual extent');
  assert(endVa - startVa <= BigInt(limits.tls_template_bytes), 'Complete TLS template exceeds budget');
  const templateLength = Number(endVa - startVa), templateRva = Number(startVa - base);
  report.tls_template = {rva:hex(templateRva), bytes:templateLength, complete_range:true,
    raw_hex:'', runtime_observed:false, size_of_zero_fill_not_allocated:true};
  if (templateLength > 0) {
    const templateMapping = mapping(templateRva, templateLength);
    assert.equal(templateMapping.section.name, '.tls');
    assert(!(templateMapping.section.characteristics & 0x20000000)); assert(templateMapping.section.characteristics & 0x40000000);
    const template = read(templateMapping.offset, templateLength, 'tls-template', 'complete file TLS template');
    report.tls_template.file_offset = templateMapping.offset; report.tls_template.raw_hex = template.toString('hex');
  }
  report.status = report.targets.every(t => t.byte_coverage_verified)
    ? 'fixed-actor-matrix-sync-dependencies-verified' : 'fixed-actor-matrix-sync-dependencies-partial';
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
  virtual_zero_fill_bytes:report.virtual_zero_fill_bytes, tls_metadata_bytes:report.tls_metadata_bytes,
  tls_template_bytes:report.tls_template_bytes, subprocesses:report.subprocesses, original_unchanged:report.source_hash_preserved,
  tool_unchanged:report.tool_hash_preserved, failures}));
if (failures.length) process.exitCode = 1;
