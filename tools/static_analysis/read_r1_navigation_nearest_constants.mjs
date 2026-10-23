// One fixed Engine ANDPS operand, four payload bytes; no backend or code reads.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(path.basename(self), 'read_r1_navigation_nearest_constants.mjs');
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
  ['docs/reverse-client/r1-navigation-core/05-NavigationCore-0x1802610f0-procedure_assembly.json', 'bc2f9d3a7fdb5ab4dbdfb2e58ef270dbaf9b2dfa7fcd96bc42742b91821c904d']
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
  { rva: 0x34c410, evidence_index: 1, procedure: '0x1802610f0', line: '0x1802611c2: MOVSS XMM9, dword ptr [0x18034c410]', usage_line: '0x18026123d: ANDPS XMM1, XMM9' }
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
  assert(e.normalized_result.split('\n').includes(target.usage_line));
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
const out = path.join(root, 'docs/reverse-client/r1-navigation-nearest-constants');
normal(path.dirname(out));
assert(!fs.existsSync(out), 'Output directory already exists: no overwrite or retry in place');
const report = {
  schema: 'grim-dynasty-fixed-navigation-nearest-constants-v1', started_utc: new Date().toISOString(),
  proofs, script_sha256: hash(fs.readFileSync(self)), source, expected_sha256: expected,
  payload_bytes: 0, payload_limit: 4, hash_bytes: 0, code_bytes: 0, subprocesses: 0,
  original_executed: false, target_open_mode: 'O_RDONLY', reads: [], failures: [],
  scope: 'Only Engine RVA34C410, exactly4bytes, mapped using the pinned existing PE section cache. Whole-file reads only for before/after SHA256. No code, other constants, runtime globals, fallback scans or backend.',
  limitations: [
    'Raw bits describe the pinned disk image only; they do not establish loaded memory, runtime globals or floating environment.',
    'MOVSS at2611C2 and ANDPS at26123D establish the use, not the constant bits. No ABS/sign-mask value is presumed from pseudocode.',
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

    report.reads.push({
      rva: hex(target.rva), file_offset: offset, section: s.name, bytes: 4,
      raw_hex: b.toString('hex'), bits: '0x' + b.readUInt32LE().toString(16).padStart(8, '0'),
      evidence_file: proofs[target.evidence_index][0], instruction: target.line, use_instruction: target.usage_line
    });
  }
  assert.equal(report.payload_bytes, 4);
  assert.equal(report.reads.length, 1);
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
