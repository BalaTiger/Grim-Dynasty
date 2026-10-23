// PREPARED ONLY. Fixed Game5DB8B8 six-byte entry prefix; no automatic extension or target follow.
// Logical function completeness requires review. dumpbin sees owned tiny PEs only.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_item_950_pre_call_dependency.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll', originalSize = 11385856;
const originalHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/object-lifecycle/offline-item-950-pre-call-dependency');
const temp = path.join(root, '.tooling/offline-item-950-pre-call-dependency-temp'), base = 0x180000000n;
const limits = Object.freeze({code_window_bytes:6, semantic_read_operations:1, metadata_bytes:0, data_bytes:0,
  fingerprint_chunk_bytes:65536, proof_bytes_each:262144, synthetic_pe_bytes_each:3072,
  synthetic_pe_bytes_total:3072, subprocesses:1, timeout_ms_each:10000, max_buffer_bytes_each:196608, report_bytes:196608});
const sources = [
  {path:'docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json',
    sha256:'227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'},
  {path:'docs/object-lifecycle/offline-item-950-elements/5daed0-exact-instructions.json',
    sha256:'5a5d2ba0f80bb6204ca67eb6431d4c521bb5cd146488d13b24fb403976980bf3'},
  {path:'docs/object-lifecycle/offline-item-950-elements/inspection.json',
    sha256:'d86181cb24316e7f34cbb4cf6d847a90cfe32343f1e26611048fefe49003646c'},
];
const windows = [
  {id:'5db8b8-entry-prefix', rva:0x5db8b8, length:6, kind:'fixed-direct-call-entry-prefix', exact_pdata_boundary:false},
];
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
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
  assert.fail('Existing evidence/temp path refused');
}
function stable(a, b) {
  return b.isFile() && a.size === b.size && a.dev === b.dev && a.ino === b.ino && a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs;
}
function statRecord(s) { return {size:s.size, ino:s.ino, dev:s.dev, mtime_ms:s.mtimeMs, ctime_ms:s.ctimeMs}; }
function fingerprint(fd, size) {
  assert.equal(fs.fstatSync(fd).size, size);
  const buffer = Buffer.alloc(limits.fingerprint_chunk_bytes), digest = createHash('sha256'); let at = 0;
  while (at < size) {
    const got = fs.readSync(fd, buffer, 0, Math.min(buffer.length, size - at), at);
    assert(got > 0, 'Unexpected fingerprint EOF'); digest.update(buffer.subarray(0, got)); at += got;
  }
  return {sha256:digest.digest('hex'), bytes:at, chunk_bytes:buffer.length};
}
function fingerprintFile(p) {
  normal(p, true); const fd = fs.openSync(p, fs.constants.O_RDONLY);
  try { const s = fs.fstatSync(fd), result = fingerprint(fd, s.size); assert(stable(s, fs.fstatSync(fd))); return result; }
  finally { fs.closeSync(fd); }
}
normal(script, true); normal(path.dirname(out)); normal(path.dirname(temp)); absent(out); absent(temp);
const proofs = sources.map(source => {
  const p = path.join(root, source.path); normal(p, true); assert(fs.statSync(p).size <= limits.proof_bytes_each);
  const bytes = fs.readFileSync(p); assert(bytes.length <= limits.proof_bytes_each); assert.equal(hash(bytes), source.sha256);
  return JSON.parse(bytes.toString('utf8'));
});
const boundary = proofs[0], caller = proofs[1], callerInspection = proofs[2], pe = boundary.pe;
assert.equal(boundary.status, 'fixed-item-950-element-boundaries-assessed'); assert.equal(boundary.target, original);
assert.equal(boundary.expected_sha256, originalHash); assert.equal(boundary.expected_bytes, originalSize);
assert.equal(boundary.sha256_before, originalHash); assert.equal(boundary.sha256_after, originalHash);
assert(boundary.source_stat_stable && boundary.source_hash_preserved && boundary.source_handle_closed);
assert.deepEqual(boundary.failures, []); assert.equal(pe.image_base, hex(base)); assert.equal(pe.image_size, 11427840);
assert.equal(callerInspection.status, 'fixed-item-950-element-windows-decoded-awaiting-manual-control-flow-review');
assert.equal(callerInspection.original, original); assert.equal(callerInspection.expected_sha256, originalHash);
assert.equal(callerInspection.expected_file_bytes, originalSize); assert.equal(callerInspection.sha256_before, originalHash);
assert.equal(callerInspection.sha256_after, originalHash);
assert(callerInspection.source_stat_stable && callerInspection.source_hash_preserved && callerInspection.source_closed &&
  callerInspection.byte_coverage_verified); assert.deepEqual(callerInspection.failures, []);
const callerWindows = callerInspection.windows.filter(w => w.id === '5daed0-exact'); assert.equal(callerWindows.length, 1);
const callerWindow = callerWindows[0]; assert.equal(callerWindow.begin_rva, '0x5daed0'); assert.equal(callerWindow.end_rva_exclusive, '0x5daf3e');
assert(callerWindow.exact_pdata_boundary && callerWindow.byte_coverage_verified);
assert.equal(callerWindow.window_sha256, '032bf3dbc9a52b839b654405c66ef239aabbdf831408164ccb3b38b4d84832fd');
let cursor = 0x1805daed0n;
for (const row of caller) {
  assert.equal(BigInt(row.address), cursor); assert(/^(?:[0-9a-f]{2})+$/.test(row.bytes)); cursor += BigInt(row.bytes.length / 2);
}
assert.equal(cursor, 0x1805daf3en);
const callerBytes = Buffer.from(caller.map(r => r.bytes).join(''), 'hex');
assert.equal(callerBytes.length, 110); assert.equal(hash(callerBytes), callerWindow.window_sha256);
function instruction(va, bytes) {
  const rows = caller.filter(r => r.address === va); assert.equal(rows.length, 1); assert.equal(rows[0].bytes, bytes); return rows[0];
}
const fixedCaller = [
  instruction('0x1805daf06','488bce'), instruction('0x1805daf09','e8aa090000'),
  instruction('0x1805daf0e','488bcf'), instruction('0x1805daf11','ffd6'),
];
const directCall = Buffer.from(fixedCaller[1].bytes, 'hex');
assert.equal(0x1805daf09n + 5n + BigInt(directCall.readInt32LE(1)), base + BigInt(windows[0].rva));
assert.equal(windows.reduce((n, w) => n + w.length, 0), limits.code_window_bytes);
function codeOffset(w) {
  assert(fits(w.rva, w.length, pe.image_size));
  const sections = pe.sections.filter(s => s.name === '.text'); assert.equal(sections.length, 1); const s = sections[0];
  assert(s.characteristics & 0x20000000); assert(fits(w.rva - s.rva, w.length, s.raw_size));
  const offset = s.raw_offset + w.rva - s.rva; assert(fits(offset, w.length, originalSize)); return offset;
}
const report = {schema:'grim-dynasty-fixed-item-950-pre-call-prefix-v1', status:'started', started_utc:new Date().toISOString(),
  original, expected_sha256:originalHash, expected_file_bytes:originalSize, source_evidence:sources, limits,
  tool, expected_tool_sha256:toolHash,
  saved_caller_instructions:fixedCaller, caller_argument:'RCX is the saved element callback RSI at5DAF06',
  selected_entry:{rva:'0x5db8b8',bytes:6,exact_function_boundary:false,
    authority:'Fixed six-byte prefix at an explicitly evidenced direct CALL target; no function extent inferred.'},
  candidate_slot:null, candidate_slot_bytes_read:0, candidate_target_followed:false,
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, backend_started:false, compilation_or_tests:false,
  original_path_given_to_dumpbin:false, direct_callees_followed:0, metadata_bytes:0, data_bytes:0, code_window_bytes:0,
  semantic_reads:[], fingerprint_passes:[], windows:[], subprocesses:0, synthetic_bytes:0, byte_coverage_verified:false,
  exact_function_body_established:false, manual_control_flow_review_required:true,
  limitations:[
    'Only the first6B at Game5DB8B8 are selected from direct CALL5DAF09. This is an entry prefix, not an exact .pdata body or proof of a whole function. Any incomplete decoding preserves failure and stops without extension.',
    'Only raw FF25 plus rel32 covering exactly6B may identify a candidate RIP-relative pointer slot. That slot is reported for a separately reviewed next read; its contents, import identity and runtime meaning remain unknown.',
    'Other complete instruction sequences are recorded as unresolved. No branch, callee, IAT, data, neighboring function, unwind body or candidate slot is followed.',
    'No new metadata read. dumpbin receives one owned minimal synthetic PE and only the exact authorized RANGE. Generated headers/padding are not copied target metadata and are never executed.',
    'Original same-handle before/after hashes and tool before/after hashes use64KiB buffers, separate from6B semantic budget. Stable hashes/stats do not establish loaded runtime state or native exception semantics.',
  ]};
let fd, originalStat, admittedSize = false, outCreated = false, toolAttempted = false;
const failures = [];
function save(name, bytes) { normal(out); fs.writeFileSync(path.join(out, name), bytes, {flag:'wx'}); }
function readWindow(w) {
  const offset = codeOffset(w); assert(report.semantic_reads.length < limits.semantic_read_operations);
  assert.equal(w, windows[report.semantic_reads.length]); assert(report.code_window_bytes + w.length <= limits.code_window_bytes);
  const bytes = Buffer.alloc(w.length), r = {id:w.id, rva:hex(w.rva), file_offset:offset, requested_bytes:w.length, read_bytes:0, raw_hex:''};
  report.semantic_reads.push(r); const got = fs.readSync(fd, bytes, 0, w.length, offset);
  r.read_bytes = got; r.raw_hex = bytes.subarray(0, got).toString('hex'); report.code_window_bytes += got;
  assert.equal(got, w.length, 'Short read; no retry'); return bytes;
}
function minimalPe(w, bytes) {
  const sectionRva = Math.floor(w.rva / 0x1000) * 0x1000, prefix = w.rva - sectionRva;
  const virtualSize = prefix + bytes.length, rawSize = Math.ceil(virtualSize / 0x200) * 0x200, rawOffset = 0x200;
  assert(rawOffset + rawSize <= limits.synthetic_pe_bytes_each);
  assert(report.synthetic_bytes + rawOffset + rawSize <= limits.synthetic_pe_bytes_total);
  const b = Buffer.alloc(rawOffset + rawSize), coff = 0x84, optional = 0x98, section = optional + 0xf0;
  b.writeUInt16LE(0x5a4d, 0); b.writeUInt32LE(0x80, 0x3c); b.writeUInt32LE(0x4550, 0x80);
  b.writeUInt16LE(0x8664, coff); b.writeUInt16LE(1, coff + 2); b.writeUInt16LE(0xf0, coff + 16); b.writeUInt16LE(0x2022, coff + 18);
  b.writeUInt16LE(0x20b, optional); b.writeUInt32LE(rawSize, optional + 4); b.writeUInt32LE(sectionRva, optional + 20);
  b.writeBigUInt64LE(base, optional + 24); b.writeUInt32LE(0x1000, optional + 32); b.writeUInt32LE(0x200, optional + 36);
  b.writeUInt16LE(6, optional + 40); b.writeUInt16LE(6, optional + 48);
  b.writeUInt32LE(Math.ceil((sectionRva + virtualSize) / 0x1000) * 0x1000, optional + 56);
  b.writeUInt32LE(rawOffset, optional + 60); b.writeUInt16LE(3, optional + 68); b.writeUInt32LE(16, optional + 108);
  b.write('.text', section, 'ascii'); b.writeUInt32LE(virtualSize, section + 8); b.writeUInt32LE(sectionRva, section + 12);
  b.writeUInt32LE(rawSize, section + 16); b.writeUInt32LE(rawOffset, section + 20); b.writeUInt32LE(0x60000020, section + 36);
  bytes.copy(b, rawOffset + prefix); assert(b.subarray(rawOffset + prefix, rawOffset + prefix + bytes.length).equals(bytes));
  report.synthetic_bytes += b.length;
  return {bytes:b, generated_prefix_bytes:prefix, generated_tail_bytes:rawSize - virtualSize,
    code_file_offset:rawOffset + prefix, header_bytes:rawOffset, entry_point_rva:0, imports:0, relocations:0, never_executed:true};
}
function parse(stdout, w, code) {
  const rows = [];
  for (const line of stdout.split(/\r?\n/)) {
    const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if (m) { rows.push({address:BigInt('0x' + m[1]), bytes:m[2].replace(/\s/g, '').toLowerCase(), instruction:m[3].trim()}); continue; }
    const continuation = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
    if (continuation) { assert(rows.length); rows[rows.length - 1].bytes += continuation[1].replace(/\s/g, '').toLowerCase(); }
  }
  const start = base + BigInt(w.rva), end = start + BigInt(w.length); let cursor = start;
  for (const row of rows) {
    assert(row.instruction && !/[?]|\(bad\)|invalid/i.test(row.instruction));
    assert(row.bytes.length > 0 && row.bytes.length <= 30 && row.bytes.length % 2 === 0);
    assert.equal(row.address, cursor); cursor += BigInt(row.bytes.length / 2); assert(cursor <= end);
  }
  assert.equal(cursor, end); assert.equal(rows.map(r => r.bytes).join(''), code.toString('hex'));
  return rows.map(r => ({...r, address:hex(r.address)}));
}
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.script_sha256 = fingerprintFile(script).sha256;
  toolAttempted = true; report.tool_before = fingerprintFile(tool); assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY); originalStat = fs.fstatSync(fd);
  assert(originalStat.isFile()); assert.equal(originalStat.size, originalSize); admittedSize = true; report.stat_before = statRecord(originalStat);
  const before = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'before', ...before});
  report.sha256_before = before.sha256; assert.equal(before.sha256, originalHash);
  for (const w of windows) {
    const item = {id:w.id, kind:w.kind, begin_rva:hex(w.rva), end_rva_exclusive:hex(w.rva + w.length),
      exact_pdata_boundary:w.exact_pdata_boundary, byte_coverage_verified:false}; report.windows.push(item);
    const code = readWindow(w); item.window_sha256 = hash(code); save(w.id + '.bin', code);
    const generated = minimalPe(w, code), syntheticBytes = generated.bytes; delete generated.bytes;
    const syntheticPath = path.join(temp, w.id + '.inspect.dll');
    item.synthetic_pe = {...generated, path:path.relative(root, syntheticPath).replaceAll('\\', '/'), bytes:syntheticBytes.length, sha256:hash(syntheticBytes)};
    fs.writeFileSync(syntheticPath, syntheticBytes, {flag:'wx'}); normal(syntheticPath, true);
    assert.equal(fingerprintFile(syntheticPath).sha256, item.synthetic_pe.sha256);
    const start = base + BigInt(w.rva), end = start + BigInt(w.length);
    const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES', '/RANGE:' + hex(start) + ',' + hex(end - 1n), syntheticPath];
    assert(!args.includes(original)); item.command_arguments = args;
    assert(report.subprocesses < limits.subprocesses); report.subprocesses++;
    const began = Date.now(), child = spawnSync(tool, args, {cwd:temp, windowsHide:true, encoding:'utf8',
      timeout:limits.timeout_ms_each, maxBuffer:limits.max_buffer_bytes_each,
      env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
    item.tool_elapsed_ms = Date.now() - began; item.tool_exit = child.status; item.tool_signal = child.signal;
    item.tool_error = child.error ? String(child.error) : null;
    save(w.id + '-dumpbin.txt', child.stdout || ''); save(w.id + '-stderr.txt', child.stderr || '');
    item.synthetic_pe.sha256_after = fingerprintFile(syntheticPath).sha256;
    assert.equal(item.synthetic_pe.sha256_after, item.synthetic_pe.sha256);
    assert.equal(child.error, undefined); assert.equal(child.status, 0); assert.equal(child.signal, null); assert.equal(child.stderr, '');
    const rows = parse(child.stdout, w, code); item.instructions = rows.length;
    save(w.id + '-instructions.json', JSON.stringify(rows, null, 2) + '\n'); item.byte_coverage_verified = true;
    if (code.length === 6 && code[0] === 0xff && code[1] === 0x25) {
      assert.equal(rows.length, 1); assert(/^jmp\b/i.test(rows[0].instruction));
      const slotVa = base + BigInt(w.rva) + 6n + BigInt(code.readInt32LE(2)), slotRva = slotVa - base;
      assert(slotRva >= 0n && slotRva <= BigInt(pe.image_size - 8), 'Candidate slot outside this image; stop');
      report.candidate_slot = {va:hex(slotVa),rva:hex(slotRva),pointer_width:8,source_instruction:rows[0],
        bytes_read:0,contents_unknown:true,import_binding_unknown:true,runtime_value_unknown:true,
        next_read_requires_separate_review:true};
    }
    item.return_instructions_for_manual_review = rows.filter(r => /^ret\b/i.test(r.instruction));
    item.call_or_branch_instructions_for_manual_review = rows.filter(r => /^(?:call|j[a-z]+|loop[a-z]*)\b/i.test(r.instruction));
  }
  assert.equal(report.code_window_bytes, limits.code_window_bytes); assert.equal(report.semantic_reads.length, 1);
  report.instructions = report.windows.reduce((n, w) => n + w.instructions, 0);
  report.byte_coverage_verified = report.windows.every(w => w.byte_coverage_verified);
  report.status = report.candidate_slot ? 'fixed-six-byte-rip-thunk-slot-candidate-recorded' : 'fixed-six-byte-prefix-decoded-target-unresolved';
} catch (e) { failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        const after = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'after', ...after});
        report.sha256_after = after.sha256; assert.equal(after.sha256, originalHash);
        const last = fs.fstatSync(fd); report.source_stat_stable = stable(originalStat, last); report.stat_after = statRecord(last);
        assert(report.source_stat_stable);
      }
    } catch (e) { failures.push('Original final verification: ' + String(e)); }
    finally { try { fs.closeSync(fd); report.source_closed = true; } catch (e) { failures.push('Original close: ' + String(e)); } }
  }
  if (toolAttempted) {
    try { report.tool_after = fingerprintFile(tool); assert.equal(report.tool_after.sha256, toolHash); }
    catch (e) { failures.push('Tool final verification: ' + String(e)); }
  }
  report.source_hash_preserved = report.sha256_before === originalHash && report.sha256_after === originalHash;
  report.tool_hash_preserved = report.tool_before?.sha256 === toolHash && report.tool_after?.sha256 === toolHash;
  report.failures = failures; if (failures.length) report.status = 'failed'; report.finished_utc = new Date().toISOString();
  if (outCreated) {
    assert(fd === undefined || report.source_closed === true, 'Source close unconfirmed');
    const text = JSON.stringify(report, null, 2) + '\n'; assert(Buffer.byteLength(text, 'utf8') <= limits.report_bytes); save('inspection.json', text);
  }
}
console.log(JSON.stringify({status:report.status, code_window_bytes:report.code_window_bytes, instructions:report.instructions,
  subprocesses:report.subprocesses, source_hash_preserved:report.source_hash_preserved,
  byte_coverage_verified:report.byte_coverage_verified, exact_function_body_established:false, failures}));
if (failures.length) process.exitCode = 1;

