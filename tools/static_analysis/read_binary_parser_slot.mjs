// Fixed read-only PE slot binding. No module loading, disassembly or backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const planPath = path.join(root, 'tools/static_analysis/binary-parser-slot-read-plan.json');
const planBytes = fs.readFileSync(planPath);
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
assert.equal(sha(planBytes), 'bccfcd24228c0461a21e6e1ee88dcf5d4f28197dc3bdea3721bbf5651d933f6e');
const plan = JSON.parse(planBytes);
assert.equal(plan.target.path, target);
assert.equal(plan.target.sha256, expected);
const sources = plan.address_sources.map(item => {
  const actual = sha(fs.readFileSync(path.join(root, item.file)));
  assert.equal(actual, item.sha256, `Source changed: ${item.file}`);
  return {path: item.file, sha256: actual};
});
const output = path.join(root, plan.expected_output.path);
assert.ok(!fs.existsSync(output), 'Preserve previous evidence');
const started = new Date().toISOString();
const fd = fs.openSync(target, 'r');
let record;
try {
  const size = fs.fstatSync(fd).size;
  assert.ok(Number.isSafeInteger(size) && size > 0 && size <= 64 * 1024 * 1024, 'File size outside fixed inspection budget');
  function read(offset, length) {
    assert.ok(Number.isSafeInteger(offset) && offset >= 0 && Number.isSafeInteger(length) && length > 0 && offset + length <= size, 'Out-of-file read');
    const bytes = Buffer.alloc(length);
    assert.equal(fs.readSync(fd, bytes, 0, length, offset), length, 'Short read');
    return bytes;
  }
  function hashFile() {
    const hash = createHash('sha256'), chunk = Buffer.alloc(64 * 1024);
    for (let offset = 0; offset < size;) {
      const count = fs.readSync(fd, chunk, 0, Math.min(chunk.length, size - offset), offset);
      assert.ok(count > 0, 'Short hash read');
      hash.update(chunk.subarray(0, count)); offset += count;
    }
    return hash.digest('hex');
  }
  const before = hashFile(); assert.equal(before, expected, 'Target fingerprint changed');
  const dos = read(0, 64); assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const peOffset = dos.readUInt32LE(60); assert.ok(peOffset <= 64 * 1024, 'Unexpected PE header offset');
  const coff = read(peOffset, 24); assert.equal(coff.readUInt32LE(0), 0x4550); assert.equal(coff.readUInt16LE(4), 0x8664);
  const sectionCount = coff.readUInt16LE(6), optionalSize = coff.readUInt16LE(20);
  assert.ok(sectionCount > 0 && sectionCount <= 96 && optionalSize >= 112 && optionalSize <= 4096, 'Unexpected header dimensions');
  const optional = read(peOffset + 24, optionalSize); assert.equal(optional.readUInt16LE(0), 0x20b);
  const base = optional.readBigUInt64LE(24), imageSize = optional.readUInt32LE(56);
  assert.equal(base, 0x180000000n);
  const sectionBytes = read(peOffset + 24 + optionalSize, sectionCount * 40);
  const sections = Array.from({length: sectionCount}, (_, i) => {
    const row = sectionBytes.subarray(i * 40, i * 40 + 40);
    return {name: row.subarray(0, 8).toString('ascii').replace(/\0.*$/, ''), virtual_size: row.readUInt32LE(8), rva: row.readUInt32LE(12), raw_size: row.readUInt32LE(16), raw_offset: row.readUInt32LE(20), characteristics: row.readUInt32LE(36)};
  });
  const hex = value => `0x${value.toString(16)}`;
  function mapping(va, length) {
    const wide = va - base;
    if (wide < 0n || wide + BigInt(length) > BigInt(imageSize)) return null;
    const rva = Number(wide);
    const matches = sections.filter(s => rva >= s.rva && rva + length <= s.rva + s.raw_size);
    assert.ok(matches.length <= 1, 'Ambiguous section mapping');
    if (!matches.length) return null;
    const s = matches[0], offset = s.raw_offset + rva - s.rva;
    if (offset < 0 || offset + length > size) return null;
    return {rva: hex(rva), file_offset: hex(offset), section: s.name, section_rva: hex(s.rva), section_raw_size: s.raw_size, section_characteristics: hex(s.characteristics), executable: (s.characteristics & 0x20000000) !== 0};
  }
  const slot = mapping(0x180341fe8n, 8);
  assert.ok(slot, 'Slot lacks file-backed bytes'); assert.equal(slot.file_offset, '0x340fe8', 'Candidate mapping changed');
  const bytes = read(Number(BigInt(slot.file_offset)), 8), pointer = bytes.readBigUInt64LE(0), candidate = mapping(pointer, 1);
  const status = pointer !== 0n && candidate?.executable ? 'candidate-slot-bound' : 'stopped-invalid-candidate-mapping';
  const after = hashFile(); assert.equal(after, expected, 'Target changed during inspection');
  assert.equal(fs.fstatSync(fd).size, size, 'File size changed during inspection');
  const priorExports = JSON.parse(fs.readFileSync(path.join(root, 'docs/object-lifecycle/record-interface-discovery/record-interface-exports.json')));
  record = {started, finished: new Date().toISOString(), status, target, sha256_before: before, sha256_after: after, source_evidence_hashes: sources, plan_sha256: sha(planBytes), preferred_image_base: hex(base), file_bytes: size, slot_va: '0x180341fe8', slot_mapping: slot, slot_bytes: bytes.toString('hex'), candidate_preferred_va: hex(pointer), candidate_mapping: candidate, matching_saved_exports: priorExports.candidates.filter(e => e.preferred_va.toLowerCase() === hex(pointer)).map(e => e.decorated_name), actual_load_file_target_proven: false, target_function_bytes: 0, target_loaded_or_executed: false, backend_launches: 0, hash_chunk_bytes: 65536, process_resource_usage: process.resourceUsage(), limitations: ['Preferred on-disk address only; section membership does not establish a function entry or Deserialize identity.', 'The post-vptr-write helper remains unanalysed; returned vptr preservation is not proven.', 'Only the fixed slot is decoded. File-wide hashing and PE metadata reads are included; no candidate code bytes, other slots or new export scan were read.']};
} finally { fs.closeSync(fd); }
fs.mkdirSync(path.dirname(output), {recursive: true});
fs.writeFileSync(output, JSON.stringify(record, null, 2) + '\n', {flag: 'wx'});
console.log(JSON.stringify(record, null, 2));
if (record.status !== 'candidate-slot-bound') process.exitCode = 1;
