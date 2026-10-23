// PREPARED ONLY. One exact Engine .pdata function selected by a saved init_e slot.
// dumpbin receives only an owned minimal PE, never the original Engine path.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_crt_initializer.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', originalSize = 4485632;
const originalHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/object-lifecycle/offline-actor-crt-initializer');
const temp = path.join(root, '.tooling/offline-actor-crt-initializer-temp');
const syntheticPath = path.join(temp, '2dc908.inspect.dll');
const base = 0x180000000n, requestedRva = 0x2dc908;
const limits = Object.freeze({new_pdata_entries:0, pdata_bytes:0, code_bytes:286, semantic_bytes:286,
  semantic_read_operations:1, fingerprint_chunk_bytes:65536, proof_bytes_each:65536,
  synthetic_pe_bytes:3584, subprocesses:1, timeout_ms:10000, max_buffer_bytes:196608, report_bytes:196608});
const sources = [
  {path:'docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json',
    sha256:'39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'},
  {path:'docs/object-lifecycle/offline-actor-initialization-tables/metadata.json',
    sha256:'71d060530810ccd8e1c788f53b7d6e892d779bc85aa7614a6da4108ddf3a8eba'},
  {path:'docs/object-lifecycle/offline-engine-root-static-helper-entry-prefixes/inspection.json',
    sha256:'91073998bfc8ab7f647dfdc4174bf17a979d5ad8153c2658f413bf40e67574a9'},
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
const peProof = proofs[0], tableProof = proofs[1], boundaryProof = proofs[2], pe = peProof.pe;
assert.equal(peProof.status, 'exact-actor-export-and-boundary-verified');
assert.equal(peProof.original, original); assert.equal(peProof.expected_sha256, originalHash);
assert.equal(peProof.sha256_before, originalHash); assert.equal(peProof.sha256_after, originalHash);
assert(peProof.source_stat_stable && peProof.source_hash_preserved); assert.deepEqual(peProof.failures, []);
assert.equal(pe.image_base, hex(base)); assert.equal(pe.image_size, 4521984);
assert.equal(pe.entry_count, 12210); assert.equal(pe.exception_rva, '0x424000');
assert.equal(pe.exception_file_offset, 4317696); assert.equal(pe.exception_bytes, pe.entry_count * 12);
assert.equal(tableProof.status, 'fixed-actor-initialization-tables-read'); assert.equal(tableProof.target, original);
assert.equal(tableProof.expected_sha256, originalHash); assert.equal(tableProof.expected_file_bytes, originalSize);
assert.equal(tableProof.sha256_before, originalHash); assert.equal(tableProof.sha256_after, originalHash);
assert(tableProof.source_stat_stable && tableProof.source_hash_preserved && tableProof.source_closed);
assert.deepEqual(tableProof.failures, []); assert(tableProof.init_e.complete);
assert.equal(tableProof.init_e.begin_rva, '0x303178'); assert.equal(tableProof.init_e.end_exclusive_rva, '0x303188');
assert.equal(tableProof.init_e.slots.length, 2);
const sourceSlot = tableProof.init_e.slots[1];
assert.equal(sourceSlot.slot_rva, '0x303180'); assert.equal(sourceSlot.pointer_hex, hex(base + BigInt(requestedRva)));
assert.equal(sourceSlot.raw_hex, '08c92d8001000000'); assert.equal(sourceSlot.is_null, false);
assert.equal(Buffer.from(sourceSlot.raw_hex, 'hex').readBigUInt64LE(0), base + BigInt(requestedRva));
function mapping(rva, n, sectionName) {
  assert(fits(rva, n, pe.image_size));
  const matches = pe.sections.filter(s => rva >= s.rva && fits(rva - s.rva, n, s.virtual_size));
  assert.equal(matches.length, 1); const s = matches[0]; assert.equal(s.name, sectionName);
  assert(fits(rva - s.rva, n, s.raw_size));
  if (sectionName === '.text') assert(s.characteristics & 0x20000000);
  const offset = s.raw_offset + rva - s.rva; assert(fits(offset, n, originalSize)); return offset;
}
const pdataOffset = mapping(Number(pe.exception_rva), pe.exception_bytes, '.pdata');
assert.equal(pdataOffset, pe.exception_file_offset);
assert.equal(boundaryProof.status, 'fixed-engine-root-fragments-verified');
assert.equal(boundaryProof.original, original); assert.equal(boundaryProof.expected_sha256, originalHash);
assert.equal(boundaryProof.original_sha256_before, originalHash); assert.equal(boundaryProof.original_sha256_after, originalHash);
assert(boundaryProof.original_stat_stable && boundaryProof.original_hash_matches && boundaryProof.original_closed);
assert.deepEqual(boundaryProof.failures, []);
const cached = boundaryProof.metadata_reads.filter(r => r.index === 11167);
assert.equal(cached.length, 1); const cachedEntry = cached[0];
assert.equal(cachedEntry.rva, '0x444b74'); assert.equal(cachedEntry.file_offset, 4451700);
assert.equal(cachedEntry.file_offset, pdataOffset + 11167 * 12);
assert.equal(Number(cachedEntry.rva), Number(pe.exception_rva) + 11167 * 12);
assert.equal(cachedEntry.requested_bytes, 12); assert.equal(cachedEntry.read_bytes, 12);
assert.equal(cachedEntry.raw_bytes, '08c92d0026ca2d00e8283600');
const rawEntry = Buffer.from(cachedEntry.raw_bytes, 'hex'); assert.equal(rawEntry.length, 12);
const boundary = {index:11167, begin:rawEntry.readUInt32LE(0), end:rawEntry.readUInt32LE(4),
  unwind:rawEntry.readUInt32LE(8), raw_hex:rawEntry.toString('hex'), file_offset:cachedEntry.file_offset};
assert.equal(boundary.begin, requestedRva); assert.equal(boundary.end, 0x2dca26);
assert.equal(boundary.unwind, 0x3628e8); assert.equal(boundary.end - boundary.begin, limits.code_bytes);
assert(boundary.end - boundary.begin <= 4096);
const cachedNeighbors = boundaryProof.fragments.map(f => f.next_observed_entry).filter(e => e && e.index === 11167);
assert.equal(cachedNeighbors.length, 2);
for (const e of cachedNeighbors) {
  assert.equal(e.begin, boundary.begin); assert.equal(e.end, boundary.end); assert.equal(e.unwind, boundary.unwind);
  assert.equal(e.raw_bytes, cachedEntry.raw_bytes);
}
const report = {schema:'grim-dynasty-fixed-actor-crt-initializer-v1', status:'started', started_utc:new Date().toISOString(),
  original, expected_sha256:originalHash, expected_file_bytes:originalSize, requested_rva:hex(requestedRva),
  source_evidence:sources, source_slot:sourceSlot, boundary, cached_boundary_read:cachedEntry,
  boundary_source:sources[2], limits, tool, expected_tool_sha256:toolHash,
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, backend_started:false, compilation_or_tests:false,
  direct_callees_followed:0, new_header_bytes_read:0, new_data_bytes_read:0, new_import_bytes_read:0,
  original_path_given_to_dumpbin:false, metadata_bytes:0, code_bytes:0, semantic_reads:[], new_pdata_reads:0,
  fingerprint_passes:[], subprocesses:0, byte_coverage_verified:false,
  archive_check:'Preparation found no exact 2dc908 named body in docs and no saved Engine-root instruction-body entry; pointer/call references were not treated as a body.',
  limitations:[
    'Only init_e slot303180 target2DC908 is selected. Successful pinned archival raw .pdata entry11167 proves exact[2DC908,2DCA26),286B; no new .pdata/header read, boundary search, neighboring body or prefix is requested.',
    'dumpbin opens only an owned single-section synthetic PE containing the exact extracted body and generated zero padding. Original virtual addresses are preserved; only the exact body RANGE is disassembled. This is not an executable reconstruction.',
    'No unwind payload, other callee, IAT, global or referenced string is followed. No Engine header reread or full target copy is made.',
    'Two same-handle original hashes and tool before/after hashes use64KiB buffers; fingerprint reads are separate from semantic budgets. Stable stat/hash is not an atomic snapshot.',
  ]};
let fd, originalStat, admittedSize = false, outCreated = false, toolAttempted = false;
const failures = [];
function save(name, bytes) { normal(out); fs.writeFileSync(path.join(out, name), bytes, {flag:'wx'}); }
function read(offset, n, kind) {
  assert(fits(offset, n, originalSize)); assert(report.semantic_reads.length < limits.semantic_read_operations);
  assert.equal(kind, 'code'); assert.equal(n, limits.code_bytes); assert.equal(report.code_bytes, 0);
  assert.equal(offset, mapping(requestedRva, limits.code_bytes, '.text'));
  assert(report.metadata_bytes + report.code_bytes + n <= limits.semantic_bytes);
  const bytes = Buffer.alloc(n), record = {kind, file_offset:offset, requested_bytes:n, read_bytes:0, raw_hex:''};
  report.semantic_reads.push(record); const got = fs.readSync(fd, bytes, 0, n, offset);
  record.read_bytes = got; record.raw_hex = bytes.subarray(0, got).toString('hex');
  report.code_bytes += got;
  assert.equal(got, n, 'Short read; no retry'); return bytes;
}
function minimalPe(bytes) {
  const alignment = 0x1000, sectionRva = Math.floor(requestedRva / alignment) * alignment;
  const prefix = requestedRva - sectionRva, virtualSize = prefix + bytes.length;
  const rawSize = Math.ceil(virtualSize / 0x200) * 0x200, rawOffset = 0x200;
  assert(rawOffset + rawSize <= limits.synthetic_pe_bytes);
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
  return {bytes:b, section_rva:hex(sectionRva), section_virtual_bytes:virtualSize, section_raw_bytes:rawSize,
    code_file_offset:rawOffset + prefix, generated_prefix_bytes:prefix, generated_tail_bytes:rawSize - virtualSize,
    header_bytes:rawOffset, image_base:hex(base), entry_point_rva:0, imports:0, relocations:0, never_executed:true};
}
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.script_sha256 = fingerprintFile(script).sha256;
  toolAttempted = true; report.tool_before = fingerprintFile(tool); assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY); originalStat = fs.fstatSync(fd);
  assert(originalStat.isFile()); assert.equal(originalStat.size, originalSize); admittedSize = true;
  report.stat_before = {size:originalStat.size, ino:originalStat.ino, dev:originalStat.dev, mtime_ms:originalStat.mtimeMs, ctime_ms:originalStat.ctimeMs};
  const before = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'before', ...before});
  report.sha256_before = before.sha256; assert.equal(before.sha256, originalHash);
  const length = boundary.end - boundary.begin; assert.equal(length, limits.code_bytes);
  const codeOffset = mapping(requestedRva, length, '.text'), code = read(codeOffset, length, 'code');
  report.code_file_offset = codeOffset; report.code_sha256 = hash(code); save('2dc908.bin', code);
  const generated = minimalPe(code), syntheticBytes = generated.bytes; delete generated.bytes;
  report.synthetic_pe = {...generated, path:path.relative(root, syntheticPath).replaceAll('\\', '/'),
    bytes:syntheticBytes.length, sha256:hash(syntheticBytes)};
  fs.writeFileSync(syntheticPath, syntheticBytes, {flag:'wx'}); normal(syntheticPath, true);
  assert.equal(fingerprintFile(syntheticPath).sha256, report.synthetic_pe.sha256);
  const start = base + BigInt(requestedRva), end = base + BigInt(boundary.end);
  const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES', '/RANGE:' + hex(start) + ',' + hex(end - 1n), syntheticPath];
  assert(!args.includes(original)); report.command_arguments = args; assert.equal(report.subprocesses, 0); report.subprocesses++;
  const began = Date.now(), child = spawnSync(tool, args, {cwd:temp, windowsHide:true, encoding:'utf8',
    timeout:limits.timeout_ms, maxBuffer:limits.max_buffer_bytes,
    env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
  report.tool_elapsed_ms = Date.now() - began; report.tool_exit = child.status; report.tool_signal = child.signal;
  report.tool_error = child.error ? String(child.error) : null;
  save('2dc908-dumpbin.txt', child.stdout || ''); save('2dc908-stderr.txt', child.stderr || '');
  report.synthetic_pe.sha256_after = fingerprintFile(syntheticPath).sha256;
  assert.equal(report.synthetic_pe.sha256_after, report.synthetic_pe.sha256);
  assert.equal(child.error, undefined); assert.equal(child.status, 0); assert.equal(child.signal, null); assert.equal(child.stderr, '');
  const rows = [];
  for (const line of child.stdout.split(/\r?\n/)) {
    const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if (m) { rows.push({address:BigInt('0x' + m[1]), bytes:m[2].replace(/\s/g, '').toLowerCase(), instruction:m[3].trim()}); continue; }
    const continuation = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
    if (continuation) { assert(rows.length); rows[rows.length - 1].bytes += continuation[1].replace(/\s/g, '').toLowerCase(); }
  }
  let cursor = start;
  for (const row of rows) {
    assert(row.instruction && row.bytes.length > 0 && row.bytes.length <= 30 && row.bytes.length % 2 === 0);
    assert.equal(row.address, cursor); cursor += BigInt(row.bytes.length / 2); assert(cursor <= end);
  }
  assert.equal(cursor, end); assert.equal(rows.map(r => r.bytes).join(''), code.toString('hex'));
  save('2dc908-instructions.json', JSON.stringify(rows.map(r => ({...r, address:hex(r.address)})), null, 2) + '\n');
  report.instructions = rows.length; report.byte_coverage_verified = true; report.status = 'exact-actor-crt-initializer-verified';
} catch (e) { failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        const after = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'after', ...after});
        report.sha256_after = after.sha256; assert.equal(after.sha256, originalHash);
        const last = fs.fstatSync(fd); report.source_stat_stable = stable(originalStat, last);
        report.stat_after = {size:last.size, ino:last.ino, dev:last.dev, mtime_ms:last.mtimeMs, ctime_ms:last.ctimeMs};
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
console.log(JSON.stringify({status:report.status, metadata_bytes:report.metadata_bytes, code_bytes:report.code_bytes,
  instructions:report.instructions, subprocesses:report.subprocesses, source_hash_preserved:report.source_hash_preserved,
  byte_coverage_verified:report.byte_coverage_verified, failures}));
if (failures.length) process.exitCode = 1;
