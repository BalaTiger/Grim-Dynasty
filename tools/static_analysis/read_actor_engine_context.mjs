// PREPARED ONLY. Five fixed Engine context windows; execution belongs to root review.
// dumpbin receives owned tiny PEs only. No target execution, export search or callee follow.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_engine_context.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', originalSize = 4485632;
const originalHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/object-lifecycle/offline-actor-engine-context');
const temp = path.join(root, '.tooling/offline-actor-engine-context-temp'), base = 0x180000000n;
const limits = Object.freeze({code_bytes:248, semantic_bytes:248, semantic_read_operations:5,
  metadata_bytes:0, data_bytes:0, fingerprint_chunk_bytes:65536, proof_bytes_each:1048576,
  synthetic_pe_bytes_each:4608, synthetic_pe_bytes_total:16896, subprocesses:5,
  timeout_ms_each:10000, max_buffer_bytes_each:196608, report_bytes:262144});
const sources = [
  {path:'docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json',
    sha256:'39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'},
  {path:'docs/phase-1/round-4/Engine-function-evidence-bounded-v2.json',
    sha256:'ab597744f4a4cf617c6227fe7a50100e5b03733aa48dda43a730f65509bdca26'},
  {path:'docs/phase-1/boundary-audit/module-boundaries.json',
    sha256:'d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7'},
];
const windows = [
  {id:'072951-ctor-identity', rva:0x72951, length:3, owner:'180072930', first:'180072951 MOV RDI,RCX'},
  {id:'07295e-ctor-zero', rva:0x7295e, length:2, owner:'180072930', first:'18007295e XOR EBP,EBP'},
  {id:'072c26-ctor-fields', rva:0x72c26, length:28, owner:'180072930', first:'180072c26 MOV qword ptr [RDI + 0x668],RBP'},
  {id:'074405-initialize-identity', rva:0x74405, length:3, owner:'1800743d0', first:'180074405 MOV RSI,RCX'},
  {id:'074d77-initialize-context', rva:0x74d77, length:212, owner:'1800743d0', first:'180074d77 MOV ECX,0x300'},
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
  return b.isFile() && a.size === b.size && a.dev === b.dev && a.ino === b.ino &&
    a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs;
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
const peProof = proofs[0], archive = proofs[1], inventory = proofs[2], pe = peProof.pe;
assert.equal(peProof.status, 'exact-actor-export-and-boundary-verified');
assert.equal(peProof.original, original); assert.equal(peProof.expected_sha256, originalHash);
assert.equal(peProof.sha256_before, originalHash); assert.equal(peProof.sha256_after, originalHash);
assert(peProof.source_stat_stable && peProof.source_hash_preserved); assert.deepEqual(peProof.failures, []);
assert.equal(pe.image_base, hex(base)); assert.equal(pe.image_size, 4521984);
const engineModules = inventory.filter(m => m.relativePath === 'x64/Engine.dll'); assert.equal(engineModules.length, 1);
assert.equal(engineModules[0].sha256, originalHash); assert.equal(engineModules[0].bytes, originalSize);
assert.equal(windows.reduce((n, w) => n + w.length, 0), limits.code_bytes);
for (const w of windows) {
  const functions = archive.filter(f => f.start === w.owner); assert.equal(functions.length, 1); const f = functions[0];
  const begin = base + BigInt(w.rva), end = begin + BigInt(w.length);
  assert(begin >= BigInt('0x' + f.start) && end <= BigInt('0x' + f.end));
  const rows = f.instructions.map(text => {
    const m = /^([0-9a-f]+) (.+)$/.exec(text); assert(m); return {va:BigInt('0x' + m[1]), text};
  });
  w.cached_instructions = rows.filter(row => row.va >= begin && row.va < end).map(row => row.text);
  assert.equal(w.cached_instructions[0], w.first);
  assert(rows.some(row => row.va === end), 'Exclusive window end must be a saved instruction start');
  w.cached_function = {label:f.label, start:f.start, end:f.end};
}
assert.deepEqual(windows[2].cached_instructions, [
  '180072c26 MOV qword ptr [RDI + 0x668],RBP', '180072c2d MOV qword ptr [RDI + 0x670],RBP',
  '180072c34 MOV qword ptr [RDI + 0x678],RBP', '180072c3b MOV qword ptr [RDI + 0x680],RBP']);
const init = archive.find(f => f.start === '1800743d0');
const globalReferences = init.outgoingReferences.filter(r => r.to === '180421428');
assert.equal(globalReferences.length, 3);
for (const r of globalReferences) { assert.equal(r.type, 'READ'); assert.equal(r.symbol, '?gEngine@GAME@@3PEAVEngine@1@EA'); }
for (const line of ['180074db2 MOV qword ptr [RSI + 0x678],RAX',
  '180074dd3 MOV qword ptr [RSI + 0x680],RAX', '180074e3d MOV RCX,qword ptr [RSI + 0x680]',
  '180074e44 MOV qword ptr [RSI + 0x668],RCX']) assert(windows[4].cached_instructions.includes(line));
function codeOffset(w) {
  assert(fits(w.rva, w.length, pe.image_size));
  const matches = pe.sections.filter(s => w.rva >= s.rva && fits(w.rva - s.rva, w.length, s.virtual_size));
  assert.equal(matches.length, 1); const s = matches[0]; assert.equal(s.name, '.text');
  assert(s.characteristics & 0x20000000); assert(fits(w.rva - s.rva, w.length, s.raw_size));
  const offset = s.raw_offset + w.rva - s.rva; assert(fits(offset, w.length, originalSize)); return offset;
}
const report = {schema:'grim-dynasty-fixed-actor-engine-context-v1', status:'started', started_utc:new Date().toISOString(),
  original, expected_sha256:originalHash, expected_file_bytes:originalSize, source_evidence:sources,
  limits, tool, expected_tool_sha256:toolHash, cached_global_references:globalReferences,
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, backend_started:false, compilation_or_tests:false,
  original_path_given_to_dumpbin:false, direct_callees_followed:0, export_queries:0,
  metadata_bytes:0, data_bytes:0, code_bytes:0, semantic_reads:[], fingerprint_passes:[],
  subprocesses:0, synthetic_bytes:0, windows:[], byte_coverage_verified:false,
  limitations:[
    'Exactly five instruction windows, 248B total, are selected from pinned saved Engine constructor/Initialize assembly; no new PE, export, .pdata, data or import metadata read.',
    'The three constructor identity/zero/store windows are disconnected samples. They do not establish complete intervening control flow or constructor success.',
    'Initialize window preserves intervening allocations/calls, but no callee body is read. RAX constructor results are not assumed equal to allocation input. No runtime state or global writer is established.',
    'The gEngine symbol at421428 is an explicitly attributed cached outgoing reference, not a new exact EAT binding or dereference.',
    'Each dumpbin invocation sees only an owned minimal PE with generated zero padding and the selected bytes, and is restricted to the exact corresponding RANGE. Generated PEs are never executed.',
    'Two same-handle original fingerprints use64KiB buffers and are outside semantic budgets. Stable stat/hash is not an atomic runtime snapshot. Failure preserves evidence without retry or cleanup.',
  ]};
let fd, originalStat, admittedSize = false, outCreated = false, toolAttempted = false;
const failures = [];
function save(name, bytes) { normal(out); fs.writeFileSync(path.join(out, name), bytes, {flag:'wx'}); }
function readWindow(w) {
  const offset = codeOffset(w); assert(report.semantic_reads.length < limits.semantic_read_operations);
  assert.equal(w, windows[report.semantic_reads.length]); assert(report.code_bytes + w.length <= limits.code_bytes);
  const bytes = Buffer.alloc(w.length), record = {id:w.id, rva:hex(w.rva), file_offset:offset,
    requested_bytes:w.length, read_bytes:0, raw_hex:''};
  report.semantic_reads.push(record); const got = fs.readSync(fd, bytes, 0, bytes.length, offset);
  record.read_bytes = got; record.raw_hex = bytes.subarray(0, got).toString('hex'); report.code_bytes += got;
  assert.equal(got, w.length, 'Short read; no retry'); return bytes;
}
function minimalPe(w, bytes) {
  const alignment = 0x1000, sectionRva = Math.floor(w.rva / alignment) * alignment;
  const prefix = w.rva - sectionRva, virtualSize = prefix + bytes.length;
  const rawSize = Math.ceil(virtualSize / 0x200) * 0x200, rawOffset = 0x200;
  assert(rawOffset + rawSize <= limits.synthetic_pe_bytes_each);
  assert(report.synthetic_bytes + rawOffset + rawSize <= limits.synthetic_pe_bytes_total);
  const b = Buffer.alloc(rawOffset + rawSize); b.writeUInt16LE(0x5a4d, 0); b.writeUInt32LE(0x80, 0x3c);
  const coff = 0x84, optional = 0x98, section = optional + 0xf0;
  b.writeUInt32LE(0x4550, 0x80); b.writeUInt16LE(0x8664, coff); b.writeUInt16LE(1, coff + 2);
  b.writeUInt16LE(0xf0, coff + 16); b.writeUInt16LE(0x2022, coff + 18);
  b.writeUInt16LE(0x20b, optional); b.writeUInt32LE(rawSize, optional + 4);
  b.writeUInt32LE(0, optional + 16); b.writeUInt32LE(sectionRva, optional + 20); b.writeBigUInt64LE(base, optional + 24);
  b.writeUInt32LE(alignment, optional + 32); b.writeUInt32LE(0x200, optional + 36);
  b.writeUInt16LE(6, optional + 40); b.writeUInt16LE(6, optional + 48);
  b.writeUInt32LE(Math.ceil((sectionRva + virtualSize) / alignment) * alignment, optional + 56);
  b.writeUInt32LE(rawOffset, optional + 60); b.writeUInt16LE(3, optional + 68);
  b.writeUInt32LE(16, optional + 108); b.write('.text', section, 'ascii');
  b.writeUInt32LE(virtualSize, section + 8); b.writeUInt32LE(sectionRva, section + 12);
  b.writeUInt32LE(rawSize, section + 16); b.writeUInt32LE(rawOffset, section + 20); b.writeUInt32LE(0x60000020, section + 36);
  bytes.copy(b, rawOffset + prefix); assert(b.subarray(rawOffset + prefix, rawOffset + prefix + bytes.length).equals(bytes));
  report.synthetic_bytes += b.length;
  return {bytes:b, section_rva:hex(sectionRva), code_file_offset:rawOffset + prefix,
    generated_prefix_bytes:prefix, generated_tail_bytes:rawSize - virtualSize, header_bytes:rawOffset,
    entry_point_rva:0, imports:0, relocations:0, never_executed:true};
}
function decodeRows(stdout, w, code) {
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
  assert.deepEqual(rows.map(r => r.address.toString(16)), w.cached_instructions.map(line => line.split(' ')[0]));
  return rows.map(r => ({...r, address:hex(r.address)}));
}
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.script_sha256 = fingerprintFile(script).sha256;
  toolAttempted = true; report.tool_before = fingerprintFile(tool); assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY); originalStat = fs.fstatSync(fd);
  assert(originalStat.isFile()); assert.equal(originalStat.size, originalSize); admittedSize = true;
  report.stat_before = statRecord(originalStat);
  const before = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'before', ...before});
  report.sha256_before = before.sha256; assert.equal(before.sha256, originalHash);
  for (const w of windows) {
    const item = {id:w.id, begin_rva:hex(w.rva), end_rva_exclusive:hex(w.rva + w.length),
      cached_function:w.cached_function, cached_instructions:w.cached_instructions, byte_coverage_verified:false};
    report.windows.push(item); const code = readWindow(w); item.code_sha256 = hash(code); save(w.id + '.bin', code);
    const generated = minimalPe(w, code), syntheticBytes = generated.bytes; delete generated.bytes;
    const syntheticPath = path.join(temp, w.id + '.inspect.dll');
    item.synthetic_pe = {...generated, path:path.relative(root, syntheticPath).replaceAll('\\', '/'),
      bytes:syntheticBytes.length, sha256:hash(syntheticBytes)};
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
    const rows = decodeRows(child.stdout, w, code); item.instructions = rows.length;
    save(w.id + '-instructions.json', JSON.stringify(rows, null, 2) + '\n'); item.byte_coverage_verified = true;
  }
  assert.equal(report.code_bytes, limits.code_bytes); assert.equal(report.semantic_reads.length, 5);
  report.instructions = report.windows.reduce((n, w) => n + w.instructions, 0);
  report.byte_coverage_verified = report.windows.every(w => w.byte_coverage_verified);
  report.status = 'fixed-actor-engine-context-verified';
} catch (e) { failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        const after = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'after', ...after});
        report.sha256_after = after.sha256; assert.equal(after.sha256, originalHash);
        const last = fs.fstatSync(fd); report.source_stat_stable = stable(originalStat, last);
        report.stat_after = statRecord(last); assert(report.source_stat_stable);
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
console.log(JSON.stringify({status:report.status, code_bytes:report.code_bytes, metadata_bytes:report.metadata_bytes,
  instructions:report.instructions, subprocesses:report.subprocesses, source_hash_preserved:report.source_hash_preserved,
  byte_coverage_verified:report.byte_coverage_verified, failures}));
if (failures.length) process.exitCode = 1;
