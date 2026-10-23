// Prepared only: PE metadata for exactly two constructor callees.
// Caller validation uses the already-saved 347 bytes, never new original code reads.
// No dumpbin, target execution, backend, installation, or automatic callee following.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const copy = path.join(root, '.tooling/offline-cache-node/Engine.readonly.dll');
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const out = path.join(root, 'docs/object-lifecycle/offline-archive-container-init-boundaries');
const metadataByteLimit = 32768;
const imageBase = 0x180000000n;
const callerStart = 0x18027f9c0n;
const callerEnd = 0x18027fb1bn;
const proofs = [
  {path: 'docs/object-lifecycle/offline-archive-init/0x18027f9c0.bin',
    sha256: '397bc83d6d9849d3e1417350b79af00a66447d4be65568d9a0b4ff16d96199e0', maxBytes: 347},
  {path: 'docs/object-lifecycle/offline-archive-init/27f9c0-instructions.json',
    sha256: 'f97ddfc381822f9c76040c2243c8c25b943eed737f3ea6f5565117187a2f1995', maxBytes: 65536},
  {path: 'docs/object-lifecycle/offline-archive-init/inspection.json',
    sha256: '1c523cfa4b1d6c68e28fef23bac2b8397d9a0dc9352b2f006d8053d25a5a728b', maxBytes: 65536}
];
const calls = [
  {address: 0x18027f9f3n, target: 0x05d860, context: 'A+48 record-ID index'},
  {address: 0x18027fa08n, target: 0x03c740, context: 'C=A+88 string index'},
  {address: 0x18027fa3en, target: 0x05d860, context: 'A+108 second container; business role unproven'}
];
const targets = [
  {rva: 0x03c740, exact: true, role: 'string-index-constructor'},
  {rva: 0x05d860, exact: true, role: 'record-index-and-second-container-constructor'}
];
const hash = b => createHash('sha256').update(b).digest('hex');
function hashFile(p) {
  const h = createHash('sha256'), buffer = Buffer.alloc(65536), fd = fs.openSync(p, 'r');
  try {
    let n;
    while ((n = fs.readSync(fd, buffer, 0, buffer.length, null)) > 0) h.update(buffer.subarray(0, n));
  } finally { fs.closeSync(fd); }
  return h.digest('hex');
}
assert(!fs.existsSync(out), 'Preserve prior boundary evidence');
const saved = proofs.map(proof => {
  const file = path.join(root, proof.path);
  assert(fs.statSync(file).size <= proof.maxBytes, 'Saved caller exceeds fixed evidence budget');
  const bytes = fs.readFileSync(file);
  assert.equal(hash(bytes), proof.sha256, 'Saved caller evidence changed');
  return bytes;
});
const bytes = saved[0], instructions = JSON.parse(saved[1].toString('utf8'));
assert.equal(bytes.length, Number(callerEnd - callerStart));
let cursor = callerStart;
for (const row of instructions) {
  assert.equal(BigInt(row.address), cursor);
  assert(/^(?:[0-9a-f]{2}){1,15}$/.test(row.bytes));
  assert(row.instruction.length > 0);
  cursor += BigInt(row.bytes.length / 2);
  assert(cursor <= callerEnd);
}
assert.equal(cursor, callerEnd);
assert.equal(instructions.map(row => row.bytes).join(''), bytes.toString('hex'));
for (const call of calls) {
  const matches = instructions.filter(row => BigInt(row.address) === call.address);
  assert.equal(matches.length, 1);
  const offset = Number(call.address - callerStart);
  assert(offset >= 0 && offset + 5 <= bytes.length);
  assert.equal(bytes[offset], 0xe8, 'Caller must contain a direct rel32 CALL');
  assert.equal(matches[0].bytes, bytes.subarray(offset, offset + 5).toString('hex'));
  assert.equal(call.address + 5n + BigInt(bytes.readInt32LE(offset + 1)), imageBase + BigInt(call.target));
  assert.match(matches[0].instruction, /^call\s+/i);
}
const inspection = JSON.parse(saved[2].toString('utf8'));
assert.equal(inspection.status, 'fixed-archive-init-disassembly-verified');
assert.equal(inspection.original_sha256_before, expected);
assert.equal(inspection.original_sha256_after, expected);
assert.equal(inspection.copy_sha256_before, expected);
assert.equal(inspection.copy_sha256_after, expected);
assert.equal(inspection.original_hash_matches, true);
assert.equal(inspection.copy_hash_matches, true);
assert.equal(inspection.tool_hash_matches, true);
const callerFragments = inspection.fragments.filter(f => f.start_va === '0x18027f9c0');
assert.equal(callerFragments.length, 1);
assert.equal(callerFragments[0].end_va_exclusive, '0x18027fb1b');
assert.equal(callerFragments[0].code_bytes, bytes.length);
assert.equal(callerFragments[0].code_sha256, proofs[0].sha256);
assert.equal(callerFragments[0].byte_coverage_verified, true);
const copyBefore = hashFile(copy);
assert.equal(copyBefore, expected, 'Working copy changed');
fs.mkdirSync(out);
const report = {
  status: 'started', started_utc: new Date().toISOString(), original, working_copy: copy,
  source_evidence: proofs, saved_caller_code_bytes_checked: bytes.length,
  calls_verified: calls.map(call => ({address: '0x' + call.address.toString(16),
    target_va: '0x' + (imageBase + BigInt(call.target)).toString(16), context: call.context})),
  targets: [], code_bytes_read: 0, dumpbin_started: false, target_loaded_or_executed: false,
  backend_started: false, callee_bodies_followed: 0, metadata_bytes: 0,
  metadata_byte_limit: metadataByteLimit, hash_chunk_bytes: 65536, copy_sha256_before: copyBefore,
  hash_scope: 'Streaming full-file SHA256 before and after; the zero-code-read claim excludes hashing',
  boundary_scope: 'Exactly two fixed direct-call targets; unwind entries do not prove logical completeness'
};
let fd;
const failures = [];
try {
  report.original_sha256_before = hashFile(original);
  assert.equal(report.original_sha256_before, expected, 'Original identity changed');
  fd = fs.openSync(original, 'r');
  const size = fs.fstatSync(fd).size;
  assert(size > 0 && size <= 64 * 1024 * 1024);
  function readMetadata(offset, length) {
    assert(Number.isSafeInteger(offset) && offset >= 0 && Number.isInteger(length) &&
      length > 0 && length <= 4096 && offset + length <= size);
    assert(report.metadata_bytes + length <= metadataByteLimit, 'Metadata budget exhausted');
    const b = Buffer.alloc(length);
    let done = 0;
    while (done < length) {
      const n = fs.readSync(fd, b, done, length - done, offset + done);
      assert(n > 0, 'Unexpected end of metadata');
      done += n;
    }
    report.metadata_bytes += length;
    return b;
  }
  const dos = readMetadata(0, 64);
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60);
  assert(pe <= 65536);
  const coff = readMetadata(pe, 24);
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  const n = coff.readUInt16LE(6), optSize = coff.readUInt16LE(20);
  assert(n > 0 && n <= 96 && optSize >= 144 && optSize <= 4096);
  const optional = readMetadata(pe + 24, optSize);
  assert.equal(optional.readUInt16LE(0), 0x20b);
  assert.equal(optional.readBigUInt64LE(24), imageBase);
  assert(optional.readUInt32LE(108) > 3);
  const sections = Array.from({length: n}, (_, i) => {
    const b = readMetadata(pe + 24 + optSize + i * 40, 40);
    return {rva: b.readUInt32LE(12), size: b.readUInt32LE(16), raw: b.readUInt32LE(20)};
  });
  const pdata = optional.readUInt32LE(136), pdataSize = optional.readUInt32LE(140);
  assert(pdataSize > 0 && pdataSize % 12 === 0 && pdataSize <= 2 * 1024 * 1024);
  const found = sections.filter(s => pdata >= s.rva && pdata + pdataSize <= s.rva + s.size);
  assert.equal(found.length, 1, 'Exception metadata must map to one file-backed section');
  const pdataOffset = found[0].raw + pdata - found[0].rva;
  for (const target of targets) {
    let lo = 0, hi = pdataSize / 12 - 1, boundary = null, reads = 0;
    while (lo <= hi) {
      const index = (lo + hi) >> 1, b = readMetadata(pdataOffset + 12 * index, 12);
      reads++;
      const begin = b.readUInt32LE(0), end = b.readUInt32LE(4);
      assert(begin < end);
      if (target.rva < begin) hi = index - 1;
      else if (target.rva >= end) lo = index + 1;
      else {
        boundary = {begin_rva: begin, end_rva: end, unwind_rva: b.readUInt32LE(8),
          index, bytes: end - begin};
        break;
      }
    }
    report.targets.push({...target, rva_hex: '0x' + target.rva.toString(16), boundary, index_reads: reads});
    assert(boundary && boundary.begin_rva === target.rva, 'Missing exact constructor unwind entry');
  }
  assert.equal(report.targets.length, 2);
} catch (e) {
  failures.push(String(e));
} finally {
  if (fd !== undefined) {
    try { fs.closeSync(fd); } catch (e) { failures.push('Original close: ' + String(e)); }
  }
  for (const [field, file, required] of [
    ['original_sha256_after', original, expected], ['copy_sha256_after', copy, expected]
  ]) {
    try {
      report[field] = hashFile(file);
      if (report[field] !== required) failures.push(field + ': identity mismatch');
    } catch (e) { failures.push(field + ': ' + String(e)); }
  }
  report.source_evidence_after = [];
  for (const proof of proofs) {
    try {
      const actual = hashFile(path.join(root, proof.path));
      report.source_evidence_after.push({path: proof.path, sha256: actual});
      if (actual !== proof.sha256) failures.push(proof.path + ': evidence changed');
    } catch (e) { failures.push(proof.path + ': ' + String(e)); }
  }
  report.original_hash_matches = report.original_sha256_before === expected && report.original_sha256_after === expected;
  report.copy_hash_matches = report.copy_sha256_before === expected && report.copy_sha256_after === expected;
  report.status = failures.length ? 'stopped' : 'fixed-archive-container-init-boundaries-assessed';
  report.failures = failures;
  report.finished_utc = new Date().toISOString();
  report.controller_resource_usage = process.resourceUsage();
  fs.writeFileSync(path.join(out, 'boundaries.json'), JSON.stringify(report, null, 2) + '\n', {flag: 'wx'});
}
if (failures.length) throw new Error(failures.join('\n'));
console.log(JSON.stringify({status: report.status, metadata_bytes: report.metadata_bytes,
  code_bytes_read: report.code_bytes_read, targets: report.targets}));
