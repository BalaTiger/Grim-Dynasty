// Fixed GameCamera vtable +0x10 slot only. No execution, backend, table scan or target-body read.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const self = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(path.basename(self), 'read_r1_game_camera_slot_binding.mjs');
assert.equal(process.argv.length, 2);
const hash = b => createHash('sha256').update(b).digest('hex');
function normal(p) {
  for (let q = path.resolve(p); ; q = path.dirname(q)) {
    assert(!fs.lstatSync(q).isSymbolicLink());
    assert.equal(fs.realpathSync.native(q).toLowerCase(), q.toLowerCase());
    if (path.dirname(q) === q) break;
  }
}
normal(self);
const source = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expected = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const proofs = [
  {
    file: 'docs/reverse-client/r1-game-camera-constructor/05-GameCameraConstructor-procedure_assembly.json',
    sha256: 'c13945a3fd983d1bd70b77dd25d7ead879ade5fb13156f2199761f7f8dcad373',
    operation: 'procedure_assembly'
  },
  {
    file: 'docs/reverse-client/r1-game-camera-constructor/04-GameCameraConstructor-procedure_pseudo_code.json',
    sha256: '0c85190b5a833472b9b0fa3e16890f066675b99f8357ecf8ee2fc2aae835cedc',
    operation: 'procedure_pseudo_code'
  }
];
const evidence = proofs.map(proof => {
  const file = path.join(root, proof.file);
  normal(file);
  assert(fs.statSync(file).isFile());
  assert(fs.statSync(file).size <= 1024 * 1024);
  const bytes = fs.readFileSync(file);
  assert.equal(hash(bytes), proof.sha256);
  const e = JSON.parse(bytes).result.structuredContent;
  assert.equal(e.subject.name, 'Game.dll');
  assert.equal(e.subject.digest.sha256, expected);
  assert.equal(path.resolve(e.subject.local_path).toLowerCase(), path.resolve(source).toLowerCase());
  assert.equal(e.provider.id, 'ghidra');
  assert.equal(e.operation, proof.operation);
  assert.equal(e.parameters.procedure, '0x1802be3b0');
  assert.equal(typeof e.normalized_result, 'string');
  return e;
});
const asm = evidence[0].normalized_result;
assert(asm.includes('0x1802be3bf: MOV RDI, RCX\n'));
assert(asm.includes('0x1802be3c8: LEA RAX, [0x180767938]\n0x1802be3cf: LEA RCX, [RDI + 0x120]\n0x1802be3d6: MOV qword ptr [RDI], RAX\n'));
assert(evidence[1].normalized_result.includes('*(undefined ***)this = &PTR_Update_180767938;'));

const out = path.join(root, 'docs/reverse-client/r1-game-camera-slot-binding');
normal(path.dirname(out));
assert(!fs.existsSync(out));
normal(source);
const r = {
  started_utc: new Date().toISOString(), source, expected_sha256: expected, proofs,
  script_sha256: hash(fs.readFileSync(self)), metadata_bytes: 0, data_bytes: 0,
  code_bytes: 0, hash_bytes: 0, subprocesses: 0, original_executed: false,
  values: [], failures: [],
  scope: 'PE headers/section metadata plus exactly 8 data bytes at Game RVA 0x767948; whole-file reads only for before/after SHA256. No target body or other table slots.'
};
let fd, initial;
function digest() {
  const h = createHash('sha256'), b = Buffer.alloc(65536);
  for (let at = 0; at < initial.size; ) {
    const n = fs.readSync(fd, b, 0, Math.min(b.length, initial.size - at), at);
    assert(n > 0);
    h.update(b.subarray(0, n));
    at += n;
    r.hash_bytes += n;
  }
  return h.digest('hex');
}
function readMetadata(at, n) {
  assert(Number.isSafeInteger(at) && at >= 0 && n > 0 && n <= 512 && at + n <= initial.size);
  r.metadata_bytes += n;
  assert(r.metadata_bytes <= 4096);
  const b = Buffer.alloc(n);
  assert.equal(fs.readSync(fd, b, 0, n, at), n);
  return b;
}
try {
  fd = fs.openSync(source, 'r');
  initial = fs.fstatSync(fd);
  assert(initial.isFile());
  assert.equal(initial.size, 11385856);
  r.sha256_before = digest();
  assert.equal(r.sha256_before, expected);
  const dos = readMetadata(0, 64);
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60);
  assert(pe >= 64 && pe < 4096);
  const coff = readMetadata(pe, 24);
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  assert.equal(coff.readUInt16LE(20), 240);
  const opt = readMetadata(pe + 24, 240);
  assert.equal(opt.readUInt16LE(0), 0x20b);
  const imageBase = opt.readBigUInt64LE(24), imageSize = opt.readUInt32LE(56);
  assert.equal(imageBase, 0x180000000n);
  const count = coff.readUInt16LE(6);
  assert(count > 0 && count <= 16);
  const sections = Array.from({ length: count }, (_, i) => {
    const b = readMetadata(pe + 264 + i * 40, 40);
    return {
      name: b.subarray(0, 8).toString('ascii').replace(/\0.*$/, ''),
      rva: b.readUInt32LE(12), size: b.readUInt32LE(16),
      offset: b.readUInt32LE(20), flags: b.readUInt32LE(36)
    };
  });
  const vtableRva = 0x767938, slotOffset = 0x10, slotRva = vtableRva + slotOffset, n = 8;
  assert.equal(slotRva, 0x767948);
  assert(slotRva + n <= imageSize);
  const matching = sections.filter(s => slotRva >= s.rva && slotRva + n <= s.rva + s.size);
  assert.equal(matching.length, 1);
  const section = matching[0];
  assert(!(section.flags & 0x20000000));
  assert(section.flags & 0x40000000);
  const offset = section.offset + slotRva - section.rva;
  assert(offset >= 0 && offset + n <= initial.size);
  const bytes = Buffer.alloc(n);
  assert.equal(fs.readSync(fd, bytes, 0, n, offset), n);
  r.data_bytes += n;
  assert.equal(r.data_bytes, 8);
  const targetVa = bytes.readBigUInt64LE(0);
  assert(targetVa >= imageBase && targetVa < imageBase + BigInt(imageSize));
  const targetRva = Number(targetVa - imageBase);
  const targets = sections.filter(s => targetRva >= s.rva && targetRva < s.rva + s.size);
  assert.equal(targets.length, 1);
  assert(targets[0].flags & 0x20000000);
  assert(targets[0].offset + targetRva - targets[0].rva < initial.size);
  r.values = [{
    vtable_rva: '0x767938', vtable_va: '0x180767938', slot_offset: '0x10',
    rva: '0x767948', va: '0x180767948', file_offset: offset, bytes: n,
    section: section.name, raw_hex: bytes.toString('hex'),
    target_va: '0x' + targetVa.toString(16), target_rva: '0x' + targetRva.toString(16),
    target_section: targets[0].name, target_body_read: false
  }];
  r.limitations = [
    'Constructor evidence directly writes the vtable address; this read resolves only its +0x10 on-disk slot.',
    'Executable-section membership is metadata only, not proof of a function boundary or behavior. No target body is read.',
    'Loaded vtable mutation, live object identity, completed construction and virtual-call receiver validity remain unobserved.',
    'Before/after hashes and file metadata checks are not an atomic filesystem snapshot.'
  ];
} catch (e) {
  r.failures.push(String(e));
} finally {
  if (fd !== undefined) {
    try {
      r.sha256_after = digest();
      assert.equal(r.sha256_after, expected);
      normal(source);
      for (const s of [fs.fstatSync(fd), fs.statSync(source)])
        for (const k of ['size', 'dev', 'ino', 'mtimeMs', 'ctimeMs']) assert.equal(s[k], initial[k]);
      r.stable = true;
    } catch (e) {
      r.failures.push(String(e));
    } finally {
      fs.closeSync(fd);
      r.source_closed = true;
    }
  }
}
r.finished_utc = new Date().toISOString();
r.status = r.failures.length ? 'failed' : 'passed';
normal(path.dirname(out));
assert(!fs.existsSync(out));
fs.mkdirSync(out);
normal(out);
fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(r, null, 2) + '\n', { flag: 'wx' });
console.log(JSON.stringify(r));
if (r.failures.length) process.exitCode = 1;
