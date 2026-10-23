// PREPARED ONLY. Fixed six-byte thunk recognition plus one exact import binding.
// Never load Engine or pass its path to dumpbin; do not follow other dependencies.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
assert.equal(path.resolve(script).toLowerCase(), path.join(root, 'tools/static_analysis/read_actor_sync_init_thunk.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const originalSize = 4485632, originalHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const out = path.join(root, 'docs/object-lifecycle/offline-actor-sync-init-thunk');
const temp = path.join(root, '.tooling/offline-actor-sync-init-thunk-temp');
const syntheticPath = path.join(temp, '2ddb20.inspect.dll');
const base = 0x180000000n, thunkRva = 0x2ddb20, iatRva = 0x3026f8, oftRva = 0x415eb0;
const expectedModule = 'VCRUNTIME140.dll', expectedName = '__vcrt_InitializeCriticalSectionEx';
const limits = Object.freeze({code_bytes:6, metadata_bytes:8 + expectedName.length + 3,
  semantic_bytes:6 + 8 + expectedName.length + 3, semantic_read_operations:3,
  fingerprint_chunk_bytes:65536, proof_bytes_each:65536, synthetic_pe_bytes:3584,
  subprocesses:1, timeout_ms:10000, max_buffer_bytes:196608, report_bytes:131072});
const sources = [
  {path:'docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json',
    sha256:'39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'},
  {path:'docs/object-lifecycle/offline-actor-crt-initializer/inspection.json',
    sha256:'c1eb9ae91463cd1b7304dfff290220bfe9e21eb8ccdd947433dd017a5245d9c0'},
  {path:'docs/object-lifecycle/offline-actor-crt-initializer/2dc908-instructions.json',
    sha256:'cae592eaadda08627d1b0f96f784685ee26532d36996524a6f250c1e10c1bc17'},
  {path:'docs/object-lifecycle/offline-engine-root-helper-imports/metadata.json',
    sha256:'0b81751351efeaeb5bc8cd5103035887b5ffbd4798d29edf2be50c697f84d369'},
];
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
const fits = (at, n, size) => Number.isSafeInteger(at) && at >= 0 && Number.isSafeInteger(n) && n > 0 &&
  Number.isSafeInteger(size) && at <= size && n <= size - at;
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
const peProof = proofs[0], bodyProof = proofs[1], body = proofs[2], importProof = proofs[3], pe = peProof.pe;
assert.equal(peProof.status, 'exact-actor-export-and-boundary-verified');
assert.equal(peProof.original, original); assert.equal(peProof.expected_sha256, originalHash);
assert.equal(peProof.sha256_before, originalHash); assert.equal(peProof.sha256_after, originalHash);
assert(peProof.source_stat_stable && peProof.source_hash_preserved); assert.deepEqual(peProof.failures, []);
assert.equal(pe.image_base, hex(base)); assert.equal(pe.image_size, 4521984);
assert.equal(bodyProof.status, 'exact-actor-crt-initializer-verified');
assert.equal(bodyProof.original, original); assert.equal(bodyProof.expected_file_bytes, originalSize);
assert.equal(bodyProof.sha256_before, originalHash); assert.equal(bodyProof.sha256_after, originalHash);
assert(bodyProof.source_stat_stable && bodyProof.source_hash_preserved && bodyProof.source_closed);
assert(bodyProof.byte_coverage_verified); assert.equal(bodyProof.code_bytes, 286); assert.deepEqual(bodyProof.failures, []);
assert.equal(body.length, 70);
const callers = body.filter(x => x.address === '0x1802dc926'); assert.equal(callers.length, 1);
const caller = callers[0]; assert.equal(caller.bytes, 'e8f5110000');
const callBytes = Buffer.from(caller.bytes, 'hex'); assert.equal(callBytes[0], 0xe8);
assert.equal(0x2dc926 + callBytes.length + callBytes.readInt32LE(1), thunkRva);
const savedBody = Buffer.from(bodyProof.semantic_reads[0].raw_hex, 'hex');
assert.equal(savedBody.length, 286); assert.equal(hash(savedBody), bodyProof.code_sha256);
assert.equal(savedBody.subarray(0x2dc926 - 0x2dc908, 0x2dc926 - 0x2dc908 + 5).toString('hex'), caller.bytes);
assert.equal(importProof.status, 'fixed-engine-root-helper-imports-resolved');
assert.equal(importProof.target, original); assert.equal(importProof.expected_file_bytes, originalSize);
assert.equal(importProof.sha256_before, originalHash); assert.equal(importProof.sha256_after, originalHash);
assert(importProof.source_stat_stable && importProof.source_hash_preserved && importProof.source_closed);
function mapping(rva, n, executable) {
  assert(fits(rva, n, pe.image_size));
  const matches = pe.sections.filter(s => rva >= s.rva && fits(rva - s.rva, n, s.virtual_size) && fits(rva - s.rva, n, s.raw_size));
  assert.equal(matches.length, 1); const s = matches[0];
  assert.equal(Boolean(s.characteristics & 0x20000000), executable);
  if (executable) assert.equal(s.name, '.text');
  const offset = s.raw_offset + rva - s.rva; assert(fits(offset, n, originalSize)); return offset;
}
function cached(kind, rva, expectedHex) {
  const rows = importProof.metadata_reads.filter(r => r.kind === kind); assert.equal(rows.length, 1);
  const row = rows[0], bytes = Buffer.from(expectedHex, 'hex');
  assert.equal(Number(row.rva), rva); assert.equal(row.raw_bytes, expectedHex);
  assert.equal(row.requested_bytes, bytes.length); assert.equal(row.read_bytes, bytes.length);
  assert.equal(row.file_offset, mapping(rva, bytes.length, false)); return {row, bytes};
}
const descriptor = cached('fixed-descriptor-13', 0x41569c, 'b05e41000000000000000000947d4100f8263000');
const moduleName = cached('exact-module-name-13', 0x417d94, '564352554e54494d453134302e646c6c00');
assert.equal(descriptor.bytes.readUInt32LE(0), oftRva);
assert.equal(descriptor.bytes.readUInt32LE(12), 0x417d94);
assert.equal(descriptor.bytes.readUInt32LE(16), iatRva);
assert.equal(moduleName.bytes.toString('ascii'), expectedModule + '\0');
const report = {schema:'grim-dynasty-fixed-actor-sync-init-thunk-v1', status:'started', started_utc:new Date().toISOString(),
  original, expected_sha256:originalHash, expected_file_bytes:originalSize, source_evidence:sources, caller,
  requested_rva:hex(thunkRva), candidate_iat_rva:hex(iatRva), expected_module:expectedModule, expected_name:expectedName,
  reused_metadata:[{source:sources[3], ...descriptor.row}, {source:sources[3], ...moduleName.row}],
  limits, tool, expected_tool_sha256:toolHash, target_open_mode:'O_RDONLY', target_loaded_or_executed:false,
  backend_started:false, compilation_or_tests:false, new_header_bytes_read:0, new_pdata_bytes_read:0,
  iat_payload_bytes_read:0, original_path_given_to_dumpbin:false, code_bytes:0, metadata_bytes:0,
  semantic_reads:[], fingerprint_passes:[], subprocesses:0, byte_coverage_verified:false, binding:null,
  unexecuted_frontiers:['0x1802dca28 cleanup callback', '0x1802dd7f4 failure helper'],
  limitations:[
    'This is explicit bounded six-byte import-thunk recognition from the archived 2DC926 direct call, not a .pdata boundary or proof of a complete logical function. Opcode/target mismatch stops without extension or import reads.',
    'Only original[2DDB20,2DDB26), OFT[415EB0,415EB8), and the one selected exact hint/name are new semantic reads. No headers, .pdata, IAT payload, other import, cleanup body or failure body are read.',
    'The VCRUNTIME descriptor and module bytes are reused from pinned raw evidence; only FF25 RIP target3026F8 plus exact OFT0 and name equality resolves this static binding.',
    'dumpbin receives one owned synthetic PE containing only the six original bytes and generated padding, with an exact six-byte RANGE. Neither original DLL nor synthetic PE is loaded or executed.',
    'Name binding does not prove runtime loader resolution, imported implementation, critical-section initialization success, thread initialization, exception handling or cleanup behavior.',
    'Same-handle original hashes and tool before/after hashes use64KiB buffers. Hash reads are separate from semantic budgets; stable stat/hash is not an atomic snapshot or exclusive FileShare.Read.',
  ]};
let fd, originalStat, admittedSize = false, outCreated = false, toolAttempted = false;
const failures = [];
function save(name, bytes) { normal(out); fs.writeFileSync(path.join(out, name), bytes, {flag:'wx'}); }
function read(rva, n, kind) {
  assert(report.semantic_reads.length < limits.semantic_read_operations);
  const isCode = kind === 'fixed-six-byte-thunk';
  if (isCode) { assert.equal(rva, thunkRva); assert.equal(n, 6); assert.equal(report.code_bytes, 0); }
  else {
    assert(kind === 'fixed-oft-zero' || kind === 'exact-hint-name');
    if (kind === 'fixed-oft-zero') { assert.equal(rva, oftRva); assert.equal(n, 8); }
    else { assert.equal(rva, report.selected_hint_name_rva); assert.equal(n, expectedName.length + 3); }
    assert(n <= limits.metadata_bytes - report.metadata_bytes);
  }
  assert(report.code_bytes + report.metadata_bytes + n <= limits.semantic_bytes);
  const offset = mapping(rva, n, isCode), bytes = Buffer.alloc(n);
  const record = {kind, rva:hex(rva), file_offset:offset, requested_bytes:n, read_bytes:0, raw_hex:''};
  report.semantic_reads.push(record); const got = fs.readSync(fd, bytes, 0, n, offset);
  record.read_bytes = got; record.raw_hex = bytes.subarray(0, got).toString('hex');
  if (isCode) report.code_bytes += got; else report.metadata_bytes += got;
  assert.equal(got, n, 'Short read; no retry'); return bytes;
}
function minimalPe(bytes) {
  const alignment = 0x1000, sectionRva = Math.floor(thunkRva / alignment) * alignment;
  const prefix = thunkRva - sectionRva, virtualSize = prefix + bytes.length;
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
  return {bytes:b, section_rva:hex(sectionRva), code_file_offset:rawOffset + prefix, generated_prefix_bytes:prefix,
    generated_tail_bytes:rawSize - virtualSize, header_bytes:rawOffset, entry_point_rva:0, imports:0, relocations:0, never_executed:true};
}
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.script_sha256 = fingerprintFile(script).sha256;
  toolAttempted = true; report.tool_before = fingerprintFile(tool); assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY); originalStat = fs.fstatSync(fd);
  assert(originalStat.isFile()); assert.equal(originalStat.size, originalSize); admittedSize = true;
  const before = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'before', ...before});
  report.sha256_before = before.sha256; assert.equal(before.sha256, originalHash);
  const code = read(thunkRva, 6, 'fixed-six-byte-thunk'); save('2ddb20.bin', code);
  assert.equal(code.subarray(0, 2).toString('hex'), 'ff25', 'Not the bounded RIP-relative JMP candidate; no extension');
  const decodedIat = thunkRva + 6 + code.readInt32LE(2);
  report.decoded_iat_rva = hex(decodedIat); assert.equal(decodedIat, iatRva, 'Unexpected IAT; no further reads');
  report.raw_thunk_recognized = true;
  const oft = read(oftRva, 8, 'fixed-oft-zero'), namePointer = oft.readBigUInt64LE(0);
  assert(namePointer > 0n && namePointer <= 0xffffffffn, 'Ordinal/bound pointer unsupported; no fallback');
  report.selected_hint_name_rva = Number(namePointer);
  const named = read(Number(namePointer), expectedName.length + 3, 'exact-hint-name');
  assert(named.subarray(2, -1).equals(Buffer.from(expectedName, 'ascii')), 'Exact name mismatch');
  assert.equal(named[named.length - 1], 0, 'Exact name lacks terminating NUL');
  report.binding = {module:expectedModule, name:expectedName, descriptor_index:13, descriptor_rva:'0x41569c',
    original_first_thunk_rva:hex(oftRva), first_thunk_rva:hex(iatRva), thunk_index:0,
    original_thunk_rva:hex(oftRva), hint_name_rva:hex(Number(namePointer)), hint:named.readUInt16LE(0),
    iat_rva:hex(iatRva), thunk_rva:hex(thunkRva), iat_payload_bytes_read:0};
  const generated = minimalPe(code), syntheticBytes = generated.bytes; delete generated.bytes;
  report.synthetic_pe = {...generated, path:path.relative(root, syntheticPath).replaceAll('\\', '/'),
    bytes:syntheticBytes.length, sha256:hash(syntheticBytes)};
  fs.writeFileSync(syntheticPath, syntheticBytes, {flag:'wx'}); normal(syntheticPath, true);
  assert.equal(fingerprintFile(syntheticPath).sha256, report.synthetic_pe.sha256);
  const start = base + BigInt(thunkRva), end = start + 6n;
  const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES', '/RANGE:' + hex(start) + ',' + hex(end - 1n), syntheticPath];
  assert(!args.includes(original)); report.command_arguments = args; assert.equal(report.subprocesses, 0); report.subprocesses++;
  const child = spawnSync(tool, args, {cwd:temp, windowsHide:true, encoding:'utf8', timeout:limits.timeout_ms,
    maxBuffer:limits.max_buffer_bytes, env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
  report.tool_exit = child.status; report.tool_signal = child.signal; report.tool_error = child.error ? String(child.error) : null;
  save('2ddb20-dumpbin.txt', child.stdout || ''); save('2ddb20-stderr.txt', child.stderr || '');
  report.synthetic_pe.sha256_after = fingerprintFile(syntheticPath).sha256;
  assert.equal(report.synthetic_pe.sha256_after, report.synthetic_pe.sha256);
  assert.equal(child.error, undefined); assert.equal(child.status, 0); assert.equal(child.signal, null); assert.equal(child.stderr, '');
  const rows = [];
  for (const line of child.stdout.split(/\r?\n/)) {
    const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if (m) rows.push({address:BigInt('0x' + m[1]), bytes:m[2].replace(/\s/g, '').toLowerCase(), instruction:m[3].trim()});
  }
  assert.equal(rows.length, 1); assert.equal(rows[0].address, start); assert(rows[0].instruction);
  assert.equal(rows[0].bytes, code.toString('hex')); assert.equal(rows[0].bytes.length / 2, Number(end - start));
  save('2ddb20-instructions.json', JSON.stringify(rows.map(r => ({...r, address:hex(r.address)})), null, 2) + '\n');
  report.instructions = 1; report.byte_coverage_verified = true;
  assert.equal(report.code_bytes, limits.code_bytes); assert.equal(report.metadata_bytes, limits.metadata_bytes);
  report.status = 'bounded-actor-sync-init-thunk-and-import-verified';
} catch (e) { failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) {
    try {
      if (admittedSize) {
        const after = fingerprint(fd, originalSize); report.fingerprint_passes.push({label:'after', ...after});
        report.sha256_after = after.sha256; assert.equal(after.sha256, originalHash);
        report.source_stat_stable = stable(originalStat, fs.fstatSync(fd)); assert(report.source_stat_stable);
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
  binding:report.binding, subprocesses:report.subprocesses, source_hash_preserved:report.source_hash_preserved,
  byte_coverage_verified:report.byte_coverage_verified, failures}));
if (failures.length) process.exitCode = 1;
