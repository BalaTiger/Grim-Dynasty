// Three fixed on-disk vtable qwords and at most 16 bytes at each mapped target.
// No disassembly, jump following, native loading or runtime address resolution.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const directory = path.dirname(fileURLToPath(import.meta.url));
const output = path.join(directory, 'record-interface-vslots.json');
assert.ok(!fs.existsSync(output), 'Preserve existing evidence');
const sha256 = value => createHash('sha256').update(value).digest('hex');
const exportBytes = fs.readFileSync(path.join(directory, 'record-interface-exports.json'));
const exports = JSON.parse(exportBytes);
assert.equal(exports.target, target);
assert.equal(exports.sha256_before, expectedHash);
assert.equal(exports.sha256_after, expectedHash);
const bytes = fs.readFileSync(target);
assert.equal(sha256(bytes), expectedHash, 'Target fingerprint changed');
const hex = value => `0x${value.toString(16)}`;
function bounded(offset, count) {
  assert.ok(Number.isSafeInteger(offset) && offset >= 0 && Number.isSafeInteger(count) && count > 0 && offset + count <= bytes.length, 'Out-of-file read');
  return offset;
}
const u16 = offset => bytes.readUInt16LE(bounded(offset, 2));
const u32 = offset => bytes.readUInt32LE(bounded(offset, 4));
const u64 = offset => bytes.readBigUInt64LE(bounded(offset, 8));
assert.equal(u16(0), 0x5a4d);
const pe = u32(0x3c);
assert.equal(u32(pe), 0x4550);
const coff = pe + 4, optional = coff + 20;
assert.equal(u16(coff), 0x8664);
assert.equal(u16(optional), 0x20b);
assert.ok(u16(coff + 16) >= 112);
const imageBase = u64(optional + 24), imageSize = u32(optional + 56);
assert.equal(imageBase, 0x180000000n);
const sections = [];
for (let i = 0; i < u16(coff + 2); i++) {
  const entry = optional + u16(coff + 16) + i * 40;
  bounded(entry, 40);
  sections.push({name: bytes.subarray(entry, entry + 8).toString('ascii').replace(/\0.*$/, ''), rva: u32(entry + 12), rawSize: u32(entry + 16), raw: u32(entry + 20), characteristics: u32(entry + 36)});
}
function mapping(va, count = 1) {
  const wideRva = va - imageBase;
  if (wideRva < 0n || wideRva + BigInt(count) > BigInt(imageSize)) return null;
  const rva = Number(wideRva);
  const found = sections.filter(section => rva >= section.rva && rva + count <= section.rva + section.rawSize);
  assert.ok(found.length <= 1, 'Ambiguous section mapping');
  if (!found.length) return null;
  const section = found[0], offset = bounded(section.raw + rva - section.rva, count);
  return {section, offset, rva};
}
function describe(mapped) {
  return {rva: hex(mapped.rva), file_offset: hex(mapped.offset), section: mapped.section.name, section_rva: hex(mapped.section.rva), section_raw_size: mapped.section.rawSize, section_characteristics: hex(mapped.section.characteristics), executable: (mapped.section.characteristics & 0x20000000) !== 0};
}
const fixed = [
  {class_name: 'LoadTable', export_name: '??_7LoadTable@GAME@@6B@', vtable: 0x180341f10n, offset: 0x28n, scope: 'LoadTable exported vtable; concrete returned-table selection remains unproved'},
  {class_name: 'LoadTableBinary', export_name: '??_7LoadTableBinary@GAME@@6B@', vtable: 0x180341fe0n, offset: 0x28n, scope: 'LoadTableBinary exported vtable; getter behavior and LoadFile selection remain unproved'},
  {class_name: 'Object', export_name: '??_7Object@GAME@@6B@', vtable: 0x180339f78n, offset: 0x08n, scope: 'Object base vtable only; not a concrete derived object Read implementation'},
];
const slots = fixed.map(item => {
  const matches = exports.candidates.filter(candidate => candidate.decorated_name === item.export_name);
  assert.equal(matches.length, 1, 'Missing unique prior vtable export');
  assert.equal(matches[0].preferred_va, hex(item.vtable));
  const slotAddress = item.vtable + item.offset, slot = mapping(slotAddress, 8);
  assert.ok(slot, 'Slot not backed by file bytes');
  const pointer = u64(slot.offset), code = mapping(pointer);
  const result = {class_name: item.class_name, scope: item.scope, export_name: item.export_name, vtable_preferred_va: hex(item.vtable), slot_offset: hex(item.offset), slot_preferred_va: hex(slotAddress), slot_mapping: describe(slot), slot_bytes: bytes.subarray(slot.offset, slot.offset + 8).toString('hex'), target_preferred_va: hex(pointer), target_mapping: code ? describe(code) : null, target_byte_count: 0, target_bytes: null, entry_first_byte_is_c3: null};
  if (code && (code.section.characteristics & 0x20000000) !== 0) {
    const count = Math.min(16, code.section.rawSize - (code.rva - code.section.rva), bytes.length - code.offset);
    bounded(code.offset, count);
    result.target_byte_count = count;
    result.target_bytes = bytes.subarray(code.offset, code.offset + count).toString('hex');
    result.entry_first_byte_is_c3 = bytes[code.offset] === 0xc3;
  }
  return result;
});
const result = {
  operation: 'Three fixed exported-vtable slot qwords with bounded raw target bytes',
  target, expected_sha256: expectedHash, sha256_before: sha256(bytes),
  target_binary_loaded_or_executed: false, disassembly_performed: false, jumps_followed: false,
  preferred_image_base: hex(imageBase), export_evidence_file: 'record-interface-exports.json', export_evidence_sha256: sha256(exportBytes), slots,
  limitations: [
    'These are on-disk preferred addresses, not loaded runtime addresses.',
    'Executable is a PE section characteristic; no target code was executed.',
    'C3 is reported only as a first-byte observation. Other raw bytes are not decoded and no target is followed.',
    'A base Object slot does not establish a concrete derived object Read target or its effects.',
    'Constructor vptr writes, LoadFile concrete table selection, getter behavior and full record reads remain unproved.',
    'No other vtable slot, binary, save, Steam cloud data or runtime state was read.',
  ],
};
result.sha256_after = sha256(fs.readFileSync(target));
assert.equal(result.sha256_after, expectedHash, 'Target changed during inspection');
fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
console.log(JSON.stringify(result, null, 2));
