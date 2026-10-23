// Two fixed Engine float constants, eight payload bytes; no backend or code reads.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(path.basename(self), 'read_r1_navigation_constants.mjs');
assert.equal(process.argv.length, 2);
const hash = b => createHash('sha256').update(b).digest('hex');
const hex = n => '0x' + n.toString(16);
function normal(p) {
  for (let q = path.resolve(p); ; q = path.dirname(q)) {
    assert(!fs.lstatSync(q).isSymbolicLink());
    assert.equal(fs.realpathSync.native(q).toLowerCase(), q.toLowerCase());
    if (path.dirname(q) === q) break;
  }
}
normal(self);
const proofs = [
  ['docs/object-lifecycle/offline-class-registration-exports/discovery.json', '0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
  ['docs/reverse-client/r1-navigation-single-poly/08-SinglePolyHelper-0x180260ad0-procedure_assembly.json', '35e13949b7dbe28d76f053d1f8c3a2f659616ed7d2ee45ea82e90147dc9b1b72'],
  ['docs/reverse-client/r1-navigation-single-poly/14-SinglePolyHelper-0x18025f770-procedure_assembly.json', 'aedcccf4309e835930a02a693c43064d4372e6a4d9209beeb6c6baadb3846d48']
];
const data = proofs.map(([p, h]) => {
  const f = path.join(root, p);
  normal(f);
  const s = fs.statSync(f);
  assert(s.isFile() && s.size < 1048576);
  const b = fs.readFileSync(f);
  assert.equal(hash(b), h);
  return JSON.parse(b);
});
const source = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const size = 4485632;
const selected = [
  { rva: 0x34bdcc, evidence_index: 1, procedure: '0x180260ad0', line: '0x180260d07: MOVSS XMM0, dword ptr [0x18034bdcc]' },
  { rva: 0x34b938, evidence_index: 2, procedure: '0x18025f770', line: '0x18025f803: MOVSS XMM0, dword ptr [0x18034b938]' }
];
for (const target of selected) {
  const e = data[target.evidence_index].result.structuredContent;
  assert.equal(e.subject.name, 'Engine.dll');
  assert.equal(e.subject.digest.sha256, expected);
  assert.equal(path.resolve(e.subject.local_path).toLowerCase(), path.resolve(source).toLowerCase());
  assert.equal(e.provider.id, 'ghidra');
  assert.equal(e.operation, 'procedure_assembly');
  assert.equal(e.parameters.procedure, target.procedure);
  assert.equal(typeof e.normalized_result, 'string');
  assert(e.normalized_result.split('\n').includes(target.line));
}
const matches = data[0].files.filter(f => f.target === source);
assert.equal(matches.length, 1);
const proof = matches[0];
assert.equal(proof.status, 'metadata-discovery-completed');
assert(proof.source_hash_preserved && proof.source_stat_stable);
assert.equal(proof.sha256_before, expected);
assert.equal(proof.sha256_after, expected);
assert.equal(proof.expected_file_bytes, size);
assert.equal(proof.preferred_image_base, '0x180000000');
const pe = proof.pe;
const out = path.join(root, 'docs/reverse-client/r1-navigation-constants');
normal(path.dirname(out));
assert(!fs.existsSync(out), 'Output directory already exists: no overwrite or retry in place');
const report = {
  schema: 'grim-dynasty-fixed-navigation-constants-v1', started_utc: new Date().toISOString(),
  proofs, script_sha256: hash(fs.readFileSync(self)), source, expected_sha256: expected,
  payload_bytes: 0, payload_limit: 8, hash_bytes: 0, code_bytes: 0, subprocesses: 0,
  original_executed: false, target_open_mode: 'O_RDONLY', reads: [], failures: [],
  scope: 'Only Engine RVAs34BDCC and34B938, exactly4bytes each, mapped using the pinned existing PE section cache. Whole-file reads only for before/after SHA256. No code, other constants, runtime globals, fallback scans or backend.',
  limitations: [
    'Raw bits describe the pinned disk image only; they do not establish loaded memory, runtime globals or floating environment.',
    '34B938 is separately observed; no equality with the immediate31800000 written to another global in a different branch is presumed.',
    'Before/after hashes and metadata checks do not provide an atomic snapshot.',
    'No native mesh initialization, nearest search or gameplay compatibility is established.'
  ]
};
let fd, initial;
function digest() {
  const h = createHash('sha256'), b = Buffer.alloc(65536);
  for (let at = 0; at < size; ) {
    const n = fs.readSync(fd, b, 0, Math.min(b.length, size - at), at);
    assert(n > 0);
    h.update(b.subarray(0, n));
    at += n;
    report.hash_bytes += n;
  }
  return h.digest('hex');
}
try {
  normal(source);
  fd = fs.openSync(source, fs.constants.O_RDONLY);
  initial = fs.fstatSync(fd);
  assert(initial.isFile());
  assert.equal(initial.size, size);
  report.sha256_before = digest();
  assert.equal(report.sha256_before, expected);
  for (const target of selected) {
    const sections = pe.sections.filter(s => target.rva >= s.rva && target.rva + 4 <= s.rva + s.raw_size);
    assert.equal(sections.length, 1);
    const s = sections[0], offset = s.raw_offset + target.rva - s.rva;
    assert.equal(s.name, '.rdata');
    assert(!(s.characteristics & 0x20000000));
    assert(Number.isSafeInteger(offset) && offset >= 0 && offset + 4 <= size);
    assert(report.payload_bytes + 4 <= report.payload_limit);
    const b = Buffer.alloc(4);
    assert.equal(fs.readSync(fd, b, 0, 4, offset), 4);
    report.payload_bytes += 4;
    const value = b.readFloatLE();
    report.reads.push({
      rva: hex(target.rva), file_offset: offset, section: s.name, bytes: 4,
      raw_hex: b.toString('hex'), bits: '0x' + b.readUInt32LE().toString(16).padStart(8, '0'),
      float32: Number.isFinite(value) ? (Object.is(value, -0) ? '-0' : value) : String(value),
      evidence_file: proofs[target.evidence_index][0], instruction: target.line
    });
  }
  assert.equal(report.payload_bytes, 8);
  assert.equal(report.reads.length, 2);
} catch (e) {
  report.failures.push(String(e));
} finally {
  if (fd !== undefined) {
    try {
      report.sha256_after = digest();
      assert.equal(report.sha256_after, expected);
      normal(source);
      for (const s of [fs.fstatSync(fd), fs.statSync(source)])
        for (const k of ['size', 'ino', 'dev', 'mtimeMs', 'ctimeMs']) assert.equal(s[k], initial[k]);
      report.stable = true;
    } catch (e) {
      report.failures.push(String(e));
    } finally {
      fs.closeSync(fd);
      report.closed = true;
    }
  }
}
report.finished_utc = new Date().toISOString();
report.status = report.failures.length ? 'failed' : 'passed';
normal(path.dirname(out));
assert(!fs.existsSync(out));
fs.mkdirSync(out);
normal(out);
fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(report, null, 2) + '\n', { flag: 'wx' });
console.log(JSON.stringify(report));
if (report.failures.length) process.exitCode = 1;
