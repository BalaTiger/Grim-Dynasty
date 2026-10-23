// Fixed compressor slot and constructor float only. Importing performs no file I/O.
// Call inspectNavigationCompressorSlot() explicitly after the separate execution approval.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const self = fileURLToPath(import.meta.url);
const source = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedSize = 4485632;
const proofs = [
  {
    file: 'docs/reverse-client/r1-navigation-implementation-constructor/05-NavigationImplementationConstructor-procedure_assembly.json',
    sha256: '1a016f11388e78a316b9b3552f88bd3d7c4d7ace4c518f6c95ed9f48ea884571',
    procedure: '0x18010a530',
    lines: [
      '0x18010a7c9: LEA RCX, [0x180340a00]',
      '0x18010a7d0: MOV qword ptr [RAX], RCX',
      '0x18010a7d3: MOV qword ptr [RDI + 0x130], RAX',
      '0x18010a884: ADDSS XMM1, dword ptr [0x18034badc]',
      '0x18010a8a5: MOV R9, qword ptr [RDI + 0x130]',
      '0x18010a8b3: MOV RCX, qword ptr [RDI + 0x120]',
      '0x18010a8ba: CALL 0x180263e40'
    ]
  },
  {
    file: 'docs/reverse-client/r1-tile-cache-and-decompression/08-TileCacheDecompression-0x180263e40-procedure_assembly.json',
    sha256: '18157f29003ef2256c96704243a47c073c0c1e2a83d767a8ca96208f6b577023',
    procedure: '0x180263e40',
    lines: ['0x180263e52: MOV qword ptr [RCX + 0x60], R9']
  },
  {
    file: 'docs/reverse-client/r1-tile-cache-and-decompression/11-TileCacheDecompression-0x18026cc50-procedure_assembly.json',
    sha256: '8cb8a0337ce9e902badcb6d180701f494c2e07c42116decf995aaaccf16d7197',
    procedure: '0x18026cc50',
    lines: [
      '0x18026cc69: MOV R13, RDX',
      '0x18026cd4f: MOV RCX, R13',
      '0x18026cd68: MOV R10, qword ptr [R13]',
      '0x18026cd77: CALL qword ptr [R10 + 0x10]'
    ]
  }
];

const hash = b => createHash('sha256').update(b).digest('hex');
function normal(p) {
  for (let q = path.resolve(p); ; q = path.dirname(q)) {
    assert(!fs.lstatSync(q).isSymbolicLink(), 'symbolic path: ' + q);
    assert.equal(fs.realpathSync.native(q).toLowerCase(), q.toLowerCase());
    if (path.dirname(q) === q) break;
  }
}
function regular(p) {
  normal(p);
  const s = fs.statSync(p);
  assert(s.isFile());
  assert.equal(s.nlink, 1);
  assert(s.ino !== 0);
  return s;
}
function sameStat(a, b) {
  for (const k of ['size', 'dev', 'ino', 'nlink', 'mtimeMs', 'ctimeMs']) assert.equal(a[k], b[k], k);
}

export function inspectNavigationCompressorSlot() {
  assert.equal(process.platform, 'win32');
  const root = path.resolve(path.dirname(self), '../..');
  assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self), 'read_r1_navigation_compressor_slot.mjs');
  const scriptStat = regular(self);
  assert(scriptStat.size <= 32768);
  const scriptHash = hash(fs.readFileSync(self));
  const out = path.join(root, 'docs/reverse-client/r1-navigation-compressor-slot');
  normal(path.dirname(out));
  assert(!fs.existsSync(out), 'refuse existing output directory');
  fs.mkdirSync(out);
  normal(out);
  const r = {
    started_utc: new Date().toISOString(), source, expected_sha256: expected,
    expected_size: expectedSize, script_sha256: scriptHash,
    proofs: proofs.map(p => ({ file: p.file, sha256: p.sha256, procedure: p.procedure })),
    metadata_bytes: 0, data_bytes: 0, code_bytes: 0, hash_bytes: 0,
    subprocesses: 0, original_executed: false, values: [], constants: [], failures: [],
    scope: 'Bounded PE headers/section metadata (at most 4096 bytes) and exactly 12 data bytes: compressor slot RVA 0x340a10 (8 bytes) and constructor ADDSS constant RVA 0x34badc (4 bytes). Whole-file streams are only before/after SHA256; no code, adjacent slot, RTTI or other original file is read.',
    limitations: [
      'This resolves the fixed on-disk slot vptr340A00+10 motivated by 10A530, 263E40 and 26CC50. It does not execute or prove the target decoder algorithm.',
      'Executable-section membership is metadata, not proof of function boundaries, full ABI or decompression correctness.',
      'No loaded vptr mutation, live receiver identity or successful native construction is observed.',
      'The 34BADC float is a disk observation bound to 10A884; no loaded constant or valid runtime configuration is assumed.',
      'Node path checks do not expose every Windows reparse tag. The separately reviewed Windows Attributes precheck remains required; this script does not bypass it.',
      'Before/after hash and stat equality is not an atomic filesystem snapshot.'
    ]
  };
  let fd, initial;
  function digest() {
    const h = createHash('sha256'), b = Buffer.alloc(65536);
    for (let at = 0; at < expectedSize; ) {
      const n = fs.readSync(fd, b, 0, Math.min(b.length, expectedSize - at), at);
      assert(n > 0, 'short hash read');
      h.update(b.subarray(0, n));
      at += n;
      r.hash_bytes += n;
    }
    return h.digest('hex');
  }
  function readMetadata(at, n) {
    assert(Number.isSafeInteger(at) && at >= 0 && n > 0 && n <= 512 && at + n <= expectedSize);
    assert(r.metadata_bytes + n <= 4096);
    const b = Buffer.alloc(n);
    assert.equal(fs.readSync(fd, b, 0, n, at), n);
    r.metadata_bytes += n;
    return b;
  }
  try {
    for (const proof of proofs) {
      const file = path.join(root, proof.file), st = regular(file);
      assert(st.size <= 1024 * 1024);
      const bytes = fs.readFileSync(file);
      assert.equal(hash(bytes), proof.sha256);
      const e = JSON.parse(bytes).result.structuredContent;
      assert.equal(e.subject.name, 'Engine.dll');
      assert.equal(e.subject.digest.sha256, expected);
      assert.equal(path.resolve(e.subject.local_path).toLowerCase(), path.resolve(source).toLowerCase());
      assert.equal(e.provider.id, 'ghidra');
      assert.equal(e.operation, 'procedure_assembly');
      assert.equal(e.parameters.procedure, proof.procedure);
      assert.equal(typeof e.normalized_result, 'string');
      const lines = e.normalized_result.split('\n');
      for (const line of proof.lines) assert(lines.includes(line), 'missing bound assembly: ' + line);
    }
    r.proofs_verified = true;
    const beforeOpen = regular(source);
    assert.equal(beforeOpen.size, expectedSize);
    fd = fs.openSync(source, 'r');
    initial = fs.fstatSync(fd);
    assert(initial.isFile());
    sameStat(initial, beforeOpen);
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
      const s = {
        name: b.subarray(0, 8).toString('ascii').replace(/\0.*$/, ''),
        virtual_size: b.readUInt32LE(8), rva: b.readUInt32LE(12),
        size: b.readUInt32LE(16), offset: b.readUInt32LE(20), flags: b.readUInt32LE(36)
      };
      assert(s.rva + Math.max(s.virtual_size, s.size) <= imageSize);
      assert(s.offset + s.size <= expectedSize);
      return s;
    });
    const vtableRva = 0x340a00, slotOffset = 0x10, slotRva = vtableRva + slotOffset, n = 8;
    assert.equal(slotRva, 0x340a10);
    assert(slotRva + n <= imageSize);
    const matching = sections.filter(s => slotRva >= s.rva && slotRva + n <= s.rva + s.size);
    assert.equal(matching.length, 1);
    const section = matching[0];
    assert(!(section.flags & 0x20000000));
    assert(section.flags & 0x40000000);
    const offset = section.offset + slotRva - section.rva;
    assert(offset >= 0 && offset + n <= expectedSize);
    const bytes = Buffer.alloc(n);
    assert.equal(fs.readSync(fd, bytes, 0, n, offset), n);
    r.data_bytes += n;
    assert.equal(r.data_bytes, 8);
    const targetVa = bytes.readBigUInt64LE(0);
    const value = {
      vtable_rva: '0x340a00', vtable_va: '0x180340a00', slot_offset: '0x10',
      rva: '0x340a10', va: '0x180340a10', file_offset: offset, bytes: n,
      section: section.name, raw_hex: bytes.toString('hex'),
      target_va: '0x' + targetVa.toString(16), target_body_read: false,
      role_evidence: '26CC50 parameter2 receiver virtual call at 26CD77', target_executable_verified: false
    };
    r.values.push(value);
    assert(targetVa >= imageBase && targetVa < imageBase + BigInt(imageSize));
    const targetRva = Number(targetVa - imageBase);
    value.target_rva = '0x' + targetRva.toString(16);
    const targets = sections.filter(s => targetRva >= s.rva && targetRva < s.rva + Math.max(s.virtual_size, s.size));
    assert.equal(targets.length, 1);
    const target = targets[0];
    value.target_section = target.name;
    assert(target.flags & 0x20000000);
    assert(targetRva < target.rva + target.size, 'target must have on-disk backing');
    value.target_file_offset = target.offset + targetRva - target.rva;
    assert(value.target_file_offset >= 0 && value.target_file_offset < expectedSize);
    value.target_executable_verified = true;
    const constantRva = 0x34badc, constantSize = 4;
    assert(constantRva + constantSize <= imageSize);
    const constantSections = sections.filter(s => constantRva >= s.rva && constantRva + constantSize <= s.rva + s.size);
    assert.equal(constantSections.length, 1);
    const constantSection = constantSections[0];
    assert(!(constantSection.flags & 0x20000000));
    assert(constantSection.flags & 0x40000000);
    const constantOffset = constantSection.offset + constantRva - constantSection.rva;
    assert(constantOffset >= 0 && constantOffset + constantSize <= expectedSize);
    const constantBytes = Buffer.alloc(constantSize);
    assert.equal(fs.readSync(fd, constantBytes, 0, constantSize, constantOffset), constantSize);
    r.data_bytes += constantSize;
    assert.equal(r.data_bytes, 12);
    const scalar = constantBytes.readFloatLE(0);
    r.constants.push({
      rva: '0x34badc', va: '0x18034badc', file_offset: constantOffset, bytes: constantSize,
      section: constantSection.name, raw_hex: constantBytes.toString('hex'),
      uint32_hex: '0x' + constantBytes.readUInt32LE(0).toString(16).padStart(8, '0'),
      float32: Number.isFinite(scalar) ? scalar : null, float32_text: String(scalar),
      evidence_instruction: '0x18010a884: ADDSS XMM1, dword ptr [0x18034badc]', loaded_value_observed: false
    });
  } catch (e) {
    r.failures.push(String(e));
  } finally {
    if (fd !== undefined) {
      try {
        r.sha256_after = digest();
        assert.equal(r.sha256_after, expected);
        sameStat(fs.fstatSync(fd), initial);
        sameStat(regular(source), initial);
        r.stable = true;
      } catch (e) {
        r.failures.push(String(e));
      } finally {
        try { fs.closeSync(fd); r.source_closed = true; }
        catch (e) { r.failures.push(String(e)); }
      }
    }
  }
  r.finished_utc = new Date().toISOString();
  r.status = r.failures.length ? 'failed' : 'passed';
  normal(out);
  fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(r, null, 2) + '\n', { flag: 'wx' });
  console.log(JSON.stringify({ status: r.status, output: path.join(out, 'inspection.json'), values: r.values, failures: r.failures }));
  if (r.failures.length) process.exitCode = 1;
  return r;
}
