// Prepared only: fixed 16-byte entry window and 143-byte cached pdata range.
// No original execution, backend, new metadata read or callee follow-up.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(self.toLowerCase(), path.join(root, 'tools/static_analysis/read_r1_prestart_idle_static.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll', originalSize = 11385856;
const originalHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/reverse-client/r1-prestart-idle-static');
const temp = path.join(root, '.tooling/r1-prestart-idle-static-temp');
const base = 0x180000000n;
const windows = [
  {id:'01a700', rva:0x1a700, length:16, exact_pdata:false, kind:'explicit-entry-window-no-exact-pdata'},
  {id:'125070', rva:0x125070, length:143, exact_pdata:true, kind:'exact-cached-pdata-fragment'},
];
const limits = Object.freeze({code_bytes:159, semantic_reads:2, metadata_bytes:0, data_bytes:0,
  hash_buffer_bytes:65536, proof_bytes_each:65536, tiny_pe_bytes_each:2560, tiny_pe_bytes_total:3584, dumpbin_calls:2,
  timeout_ms:10000, output_bytes:196608, report_bytes:65536});
const sources = [
  {file:'docs/reverse-client/r1-mouse-link-metadata/prestart-slots.json', sha256:'2b8efe9713299be4654b26c9010e76c4d719c4271db6ee7f21c5137eaae68822'},
  {file:'docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json', sha256:'227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'},
  {file:'docs/reverse-client/r1-idle-entry-metadata/inspection.json', sha256:'35a9bb7ef7d246ef289e38eded95559558a70a0f98a8490fcf9033d9443391ef'},
];
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
function normal(p, file = false) {
  let first = true;
  for (let c = path.resolve(p);;) {
    const s = fs.lstatSync(c); assert(!s.isSymbolicLink(), 'Redirected path refused');
    assert(first && file ? s.isFile() : s.isDirectory());
    assert.equal(fs.realpathSync.native(c).toLowerCase(), c.toLowerCase(), 'Redirected ancestor refused');
    const parent = path.dirname(c); if (parent === c) return; c = parent; first = false;
  }
}
function absent(p) { try { fs.lstatSync(p); } catch (e) { if (e.code === 'ENOENT') return; throw e; } assert.fail('Existing output preserved: ' + p); }
function stable(a, b) { return b.isFile() && ['size','dev','ino','mtimeMs','ctimeMs'].every(k => a[k] === b[k]); }
function statRecord(s) { return {size:s.size, dev:s.dev, ino:s.ino, mtime_ms:s.mtimeMs, ctime_ms:s.ctimeMs}; }
function fingerprint(fd, size) {
  assert.equal(fs.fstatSync(fd).size, size); const b = Buffer.alloc(limits.hash_buffer_bytes), h = createHash('sha256');
  for (let at = 0; at < size;) { const n = fs.readSync(fd, b, 0, Math.min(b.length, size - at), at); assert(n > 0); h.update(b.subarray(0, n)); at += n; }
  return {sha256:h.digest('hex'), bytes:size, buffer_bytes:b.length};
}
function fileFingerprint(p) {
  normal(p, true); const f = fs.openSync(p, fs.constants.O_RDONLY);
  try { const s = fs.fstatSync(f), result = fingerprint(f, s.size); assert(stable(s, fs.fstatSync(f))); return result; }
  finally { fs.closeSync(f); }
}
normal(self, true); normal(path.dirname(out)); normal(path.dirname(temp)); absent(out); absent(temp);
const [slots, boundary, idleProof] = sources.map(s => {
  const p = path.join(root, s.file); normal(p, true); assert(fs.statSync(p).size <= limits.proof_bytes_each);
  const b = fs.readFileSync(p); assert(b.length <= limits.proof_bytes_each); assert.equal(hash(b), s.sha256); return JSON.parse(b);
});
assert.equal(slots.status, 'passed'); assert.equal(slots.source, original);
assert.equal(slots.sha256_before, originalHash); assert.equal(slots.sha256_after, originalHash);
assert(slots.stable && slots.source_closed); assert.deepEqual(slots.failures, []);
assert.equal(slots.vtable, '0x1806e40a0'); assert.deepEqual(slots.slots.map(s => s.slot), ['0x1b0','0x1b8','0x1c8']);
for (const s of slots.slots) {
  assert.equal(s.target_va, '0x18001a700'); assert.equal(s.target_rva, '0x1a700'); assert.equal(s.target_section, '.text');
  assert.equal(s.raw_hex, '00a7018001000000'); assert.equal(s.boundary, null); assert.equal(s.target_body_read, false);
}
assert.equal(boundary.status, 'fixed-item-950-element-boundaries-assessed'); assert.equal(boundary.target, original);
assert.equal(boundary.expected_sha256, originalHash); assert.equal(boundary.expected_bytes, originalSize);
assert.equal(boundary.sha256_before, originalHash); assert.equal(boundary.sha256_after, originalHash);
assert(boundary.source_stat_stable && boundary.source_hash_preserved && boundary.source_handle_closed); assert.deepEqual(boundary.failures, []);
assert.equal(boundary.pe.image_base, hex(base)); const sections = boundary.pe.sections.filter(s => s.name === '.text');
assert.equal(sections.length, 1); const section = sections[0]; assert(section.characteristics & 0x20000000);
assert.equal(idleProof.status, 'passed'); assert.deepEqual(idleProof.failures, []); assert.equal(idleProof.modules.length, 1);
const idleModule = idleProof.modules[0]; assert.equal(path.resolve(idleModule.source).toLowerCase(), path.resolve(original).toLowerCase());
assert.equal(idleModule.expected_sha256, originalHash); assert.equal(idleModule.sha256_before, originalHash); assert.equal(idleModule.sha256_after, originalHash);
assert(idleModule.stable && idleModule.closed); assert.equal(idleModule.image_base, hex(base)); assert.equal(idleModule.targets.length, 1);
const idleTarget = idleModule.targets[0]; assert.equal(idleTarget.prefix, '?GoToIdleState@ControllerAI@GAME@@QEAAXXZ'); assert.equal(idleTarget.hits.length, 1);
const idle = idleTarget.hits[0]; assert.equal(idle.name, idleTarget.prefix); assert.equal(idle.rva, '0x125070'); assert.equal(idle.preferred_va, '0x180125070');
assert.equal(idle.section, '.text'); assert.equal(idle.executable, true);
assert.deepEqual(idle.boundary, {begin_rva:'0x125070', end_rva_exclusive:'0x1250ff', exact_start:true, unwind_rva:'0x7ffc40'});
for (const w of windows) {
  assert(w.rva >= section.rva && w.rva - section.rva <= section.raw_size - w.length);
  w.offset = section.raw_offset + w.rva - section.rva; assert(w.offset >= 0 && w.offset <= originalSize - w.length);
}
const report = {schema:'grim-dynasty-r1-prestart-idle-static-v1', status:'started', started_utc:new Date().toISOString(),
  original, expected_sha256:originalHash, expected_file_bytes:originalSize, sources, limits, tool, expected_tool_sha256:toolHash,
  windows:[],
  original_open_mode:'O_RDONLY', original_executed:false, original_given_to_dumpbin:false, backend_started:false,
  code_bytes:0, metadata_bytes:0, data_bytes:0, subprocesses:0, synthetic_bytes:0, semantic_reads:[], fingerprint_passes:[],
  exact_function_body_established:false, loaded_runtime_behavior_established:false, failures:[],
  limitations:[
    'Three saved disk vtable slots point to this entry; no exact .pdata boundary was found. The selected 16-byte window is not asserted to be a complete function.',
    'The decoded prefix stops at the first RET, other control transfer, undecodable row or window end. Remaining bytes are not assigned to this function or classified as padding or a neighbor.',
    'A straight-line disk RET observation does not establish runtime vtable contents, loaded-code behavior, CFG completeness, side-effect absence or native ABI/exception semantics.',
    'The second range [125070,1250FF) is exactly the saved 143-byte pdata fragment for the named GoToIdleState export. A name or unwind fragment does not establish its semantics or complete logical function.',
    'No branch, call, slot, metadata, neighboring bytes or other target is followed. Synthetic headers and padding are generated; only the two fixed windows come from Game.dll.',
  ]};
function save(name, data) { normal(out); fs.writeFileSync(path.join(out, name), data, {flag:'wx'}); }
function tinyPe(w, code) {
  const sectionRva = Math.floor(w.rva / 0x1000) * 0x1000, prefix = w.rva - sectionRva;
  const virtualSize = prefix + w.length, rawSize = Math.ceil(virtualSize / 0x200) * 0x200, rawOffset = 0x200;
  const b = Buffer.alloc(rawOffset + rawSize), coff = 0x84, optional = 0x98, sh = optional + 0xf0;
  assert(b.length <= limits.tiny_pe_bytes_each); assert(report.synthetic_bytes + b.length <= limits.tiny_pe_bytes_total); report.synthetic_bytes += b.length;
  b.writeUInt16LE(0x5a4d, 0); b.writeUInt32LE(0x80, 0x3c); b.writeUInt32LE(0x4550, 0x80);
  b.writeUInt16LE(0x8664, coff); b.writeUInt16LE(1, coff + 2); b.writeUInt16LE(0xf0, coff + 16); b.writeUInt16LE(0x2022, coff + 18);
  b.writeUInt16LE(0x20b, optional); b.writeUInt32LE(rawSize, optional + 4); b.writeUInt32LE(sectionRva, optional + 20);
  b.writeBigUInt64LE(base, optional + 24); b.writeUInt32LE(0x1000, optional + 32); b.writeUInt32LE(0x200, optional + 36);
  b.writeUInt16LE(6, optional + 40); b.writeUInt16LE(6, optional + 48); b.writeUInt32LE(Math.ceil((sectionRva + virtualSize) / 0x1000) * 0x1000, optional + 56);
  b.writeUInt32LE(rawOffset, optional + 60); b.writeUInt16LE(3, optional + 68); b.writeUInt32LE(16, optional + 108);
  b.write('.text', sh, 'ascii'); b.writeUInt32LE(virtualSize, sh + 8); b.writeUInt32LE(sectionRva, sh + 12);
  b.writeUInt32LE(rawSize, sh + 16); b.writeUInt32LE(rawOffset, sh + 20); b.writeUInt32LE(0x60000020, sh + 36);
  code.copy(b, rawOffset + prefix); assert(b.subarray(rawOffset + prefix, rawOffset + prefix + w.length).equals(code)); return b;
}
function decode(stdout, w, code) {
  const rows = [];
  for (const line of stdout.split(/\r?\n/)) {
    const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if (m) { rows.push({address:BigInt('0x' + m[1]), bytes:m[2].replace(/\s/g, '').toLowerCase(), instruction:m[3].trim()}); continue; }
    const c = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
    if (c) { assert(rows.length); rows[rows.length - 1].bytes += c[1].replace(/\s/g, '').toLowerCase(); }
  }
  const start = base + BigInt(w.rva), end = start + BigInt(w.length); let cursor = start;
  for (const row of rows) { assert.equal(row.address, cursor); assert(row.bytes.length >= 2 && row.bytes.length <= 30 && row.bytes.length % 2 === 0); cursor += BigInt(row.bytes.length / 2); assert(cursor <= end); }
  assert.equal(cursor, end); assert.equal(rows.map(r => r.bytes).join(''), code.toString('hex'));
  const prefix = []; let reason = 'window-end-without-return', used = 0;
  for (const row of rows) {
    if (!row.instruction || /[?]|\(bad\)|invalid/i.test(row.instruction)) { reason = 'undecodable-row'; break; }
    prefix.push({...row, address:hex(row.address)}); used += row.bytes.length / 2;
    if (/^ret\b/i.test(row.instruction)) { reason = 'first-linear-ret'; break; }
    if (/^(?:call|j[a-z]+|loop[a-z]*|int\d*|iret[a-z]*|syscall|sysenter|sysret|sysexit|hlt|ud2)\b/i.test(row.instruction)) { reason = 'control-transfer-not-followed'; break; }
  }
  return {window_rows:rows.map(r => ({...r, address:hex(r.address)})), decoded_prefix:prefix, stop_reason:reason,
    prefix_bytes:used, unassigned_tail_hex:code.subarray(used).toString('hex'), tail_not_assigned_to_function:true,
    exact_pdata_fragment:w.exact_pdata, exact_logical_function_boundary:false, manual_semantic_review_required:true};
}
let fd, initialStat, sizeAdmitted = false, outCreated = false, toolAttempted = false;
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.script_sha256 = fileFingerprint(self).sha256; toolAttempted = true;
  report.tool_before = fileFingerprint(tool); assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert(initialStat.isFile()); assert.equal(initialStat.size, originalSize); sizeAdmitted = true; report.stat_before = statRecord(initialStat);
  const before = fingerprint(fd, originalSize); report.fingerprint_passes.push({phase:'before', ...before}); report.sha256_before = before.sha256; assert.equal(before.sha256, originalHash);
  for (const w of windows) {
    const start = base + BigInt(w.rva), end = start + BigInt(w.length);
    const item = {id:w.id, begin_va:hex(start), end_va_exclusive:hex(end), length:w.length, file_offset:w.offset, kind:w.kind, exact_pdata_fragment:w.exact_pdata}; report.windows.push(item);
    assert(report.semantic_reads.length < limits.semantic_reads); assert(report.code_bytes + w.length <= limits.code_bytes);
    const code = Buffer.alloc(w.length), read = {id:w.id, file_offset:w.offset, requested_bytes:w.length, read_bytes:0, raw_hex:''}; report.semantic_reads.push(read);
    const n = fs.readSync(fd, code, 0, w.length, w.offset); read.read_bytes = n; read.raw_hex = code.subarray(0, n).toString('hex'); report.code_bytes += n; assert.equal(n, w.length, 'Short read; no retry');
    save(w.id + '-window.bin', code); item.code_sha256 = hash(code);
    const generated = tinyPe(w, code), tiny = path.join(temp, w.id + '.inspect.dll'); fs.writeFileSync(tiny, generated, {flag:'wx'}); normal(tiny, true);
    item.synthetic_pe = {file:path.relative(root, tiny).replaceAll('\\','/'), bytes:generated.length, sha256:hash(generated), entry_point_rva:0, imports:0, relocations:0, never_executed:true};
    assert.equal(fileFingerprint(tiny).sha256, item.synthetic_pe.sha256);
    const args = ['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:' + hex(start) + ',' + hex(end - 1n),tiny];
    item.command_arguments = args; assert(!args.includes(original)); assert(report.subprocesses < limits.dumpbin_calls); report.subprocesses++;
    const child = spawnSync(tool, args, {cwd:temp, windowsHide:true, encoding:'utf8', timeout:limits.timeout_ms, maxBuffer:limits.output_bytes,
      env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
    item.tool_exit = child.status; item.tool_signal = child.signal; item.tool_error = child.error ? String(child.error) : null;
    save(w.id + '-dumpbin.txt', child.stdout || ''); save(w.id + '-stderr.txt', child.stderr || '');
    item.synthetic_pe.sha256_after = fileFingerprint(tiny).sha256; assert.equal(item.synthetic_pe.sha256_after, item.synthetic_pe.sha256);
    assert.equal(child.error, undefined); assert.equal(child.status, 0); assert.equal(child.signal, null); assert.equal(child.stderr, '');
    const decoded = decode(child.stdout, w, code); save(w.id + '-decoded-prefix.json', JSON.stringify(decoded, null, 2) + '\n');
    item.window_byte_coverage_verified = true; item.prefix_stop_reason = decoded.stop_reason; item.prefix_bytes = decoded.prefix_bytes;
  }
  assert.equal(report.code_bytes, limits.code_bytes); assert.equal(report.semantic_reads.length, limits.semantic_reads); assert.equal(report.subprocesses, limits.dumpbin_calls);
  report.status = 'fixed-windows-decoded-manual-review-required';
} catch (e) { report.failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (sizeAdmitted) {
        const after = fingerprint(fd, originalSize); report.fingerprint_passes.push({phase:'after', ...after}); report.sha256_after = after.sha256; assert.equal(after.sha256, originalHash);
        const last = fs.fstatSync(fd); report.stat_after = statRecord(last); report.source_stat_stable = stable(initialStat, last); assert(report.source_stat_stable);
      }
    } catch (e) { report.failures.push('Final original verification: ' + String(e)); }
    finally { try { fs.closeSync(fd); report.source_closed = true; } catch (e) { report.failures.push('Original close: ' + String(e)); } }
  }
  if (toolAttempted) { try { report.tool_after = fileFingerprint(tool); assert.equal(report.tool_after.sha256, toolHash); } catch (e) { report.failures.push('Final tool verification: ' + String(e)); } }
  report.source_hash_preserved = report.sha256_before === originalHash && report.sha256_after === originalHash;
  report.tool_hash_preserved = report.tool_before?.sha256 === toolHash && report.tool_after?.sha256 === toolHash;
  if (report.failures.length) report.status = 'failed'; report.finished_utc = new Date().toISOString();
  if (outCreated) { assert(fd === undefined || report.source_closed === true, 'Source close unconfirmed'); const text = JSON.stringify(report, null, 2) + '\n'; assert(Buffer.byteLength(text) <= limits.report_bytes); save('inspection.json', text); }
}
console.log(JSON.stringify({status:report.status, code_bytes:report.code_bytes, subprocesses:report.subprocesses, windows:report.windows.map(w => ({id:w.id, prefix_stop_reason:w.prefix_stop_reason})),
  source_hash_preserved:report.source_hash_preserved, source_closed:report.source_closed, failures:report.failures}));
if (report.failures.length) process.exitCode = 1;
