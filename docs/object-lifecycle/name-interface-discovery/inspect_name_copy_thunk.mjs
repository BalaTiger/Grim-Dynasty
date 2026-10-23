// Fixed-target, read-only PE inspection. Never loads or executes the target.
// Adapted from registration-container-validation/inspect_free_thunk.mjs.
// Whole-file reads are for fingerprinting; interpretation is limited to one
// six-byte entry and the PE metadata required to associate its IAT slot.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const address = 0x1802ddb0en;
const output = path.join(path.dirname(fileURLToPath(import.meta.url)), 'name-copy-thunk-0x1802ddb0e.json');
assert.ok(!fs.existsSync(output), 'Preserve existing evidence');
const bytes = fs.readFileSync(target);
const sha256 = data => createHash('sha256').update(data).digest('hex');
const hashBefore = sha256(bytes);
assert.equal(hashBefore, expectedHash, 'Target fingerprint changed');
const hex = value => `0x${value.toString(16)}`;
function bounded(offset, count) {
  assert.ok(Number.isSafeInteger(offset) && offset >= 0 && Number.isSafeInteger(count) && count > 0 && offset + count <= bytes.length, 'Out-of-file metadata read');
  return offset;
}
const u16 = offset => bytes.readUInt16LE(bounded(offset, 2));
const u32 = offset => bytes.readUInt32LE(bounded(offset, 4));
const u64 = offset => bytes.readBigUInt64LE(bounded(offset, 8));
assert.equal(u16(0), 0x5a4d, 'Not MZ');
const pe = u32(0x3c);
assert.equal(u32(pe), 0x4550, 'Not PE');
const coff = pe + 4, optional = coff + 20;
assert.equal(u16(coff), 0x8664, 'Expected AMD64');
assert.equal(u16(optional), 0x20b, 'Expected PE32+');
assert.ok(u16(coff + 16) >= 128, 'Missing import directory');
assert.ok(u32(optional + 108) >= 2, 'Missing normal import directory');
const imageBase = u64(optional + 24), headerBytes = u32(optional + 60);
const sections = [];
for (let i = 0; i < u16(coff + 2); i++) {
  const entry = optional + u16(coff + 16) + i * 40;
  bounded(entry, 40);
  sections.push({rva: u32(entry + 12), rawSize: u32(entry + 16), raw: u32(entry + 20)});
}
function offsetOf(rva, count = 1) {
  assert.ok(Number.isSafeInteger(rva) && rva >= 0 && Number.isSafeInteger(count) && count > 0, 'Invalid RVA');
  if (rva + count <= headerBytes) return bounded(rva, count);
  const mapped = sections.filter(section => rva >= section.rva && rva + count <= section.rva + section.rawSize);
  assert.equal(mapped.length, 1, 'RVA is not uniquely backed by file bytes');
  return bounded(mapped[0].raw + rva - mapped[0].rva, count);
}
function asciiAt(rva) {
  const chars = [];
  for (let i = 0; i < 4096; i++) {
    const value = bytes[offsetOf(rva + i)];
    if (value === 0) return Buffer.from(chars).toString('ascii');
    assert.ok(value < 128, 'Non-ASCII import name');
    chars.push(value);
  }
  throw Error('Unterminated import name');
}
const thunkRva = Number(address - imageBase);
const thunkOffset = offsetOf(thunkRva, 6);
const instruction = bytes.subarray(thunkOffset, thunkOffset + 6);
const result = {
  operation: 'Fixed VA instruction and normal PE import metadata inspection',
  target, expected_sha256: expectedHash, sha256_before: hashBefore,
  target_binary_loaded_or_executed: false, image_base: hex(imageBase),
  address: hex(address), rva: hex(thunkRva), file_offset: hex(thunkOffset),
  instruction_bytes_inspected: 6, instruction_bytes: instruction.toString('hex'),
  status: 'not_ff25_stopped',
  limitations: [
    'Static on-disk import association only; no runtime resolution or DLL loading.',
    'No ordinary jump-chain following, delay-import scan, or CRT implementation/OOM behavior claim.',
    'An imported memcpy name establishes identity only; normal successful copy semantics remain a separately stated contract.'
  ],
};
// A non-FF25 entry ends inspection: never search or follow alternative code.
if (instruction[0] === 0xff && instruction[1] === 0x25) {
  const displacement = instruction.readInt32LE(2);
  const iatAddress = address + 6n + BigInt(displacement);
  const iatRva = Number(iatAddress - imageBase);
  const iatOffset = offsetOf(iatRva, 8);
  Object.assign(result, {status: 'ff25_import_not_resolved', instruction: 'JMP qword ptr [RIP + disp32]', displacement, iat_address: hex(iatAddress), iat_rva: hex(iatRva), iat_file_offset: hex(iatOffset), iat_file_value: hex(u64(iatOffset))});
  const importRva = u32(optional + 120), importSize = u32(optional + 124);
  assert.ok(importRva !== 0 && importSize >= 20, 'No normal import descriptors');
  for (let i = 0; (i + 1) * 20 <= importSize; i++) {
    const descriptor = offsetOf(importRva + i * 20, 20);
    const row = Array.from({length: 5}, (_, index) => u32(descriptor + index * 4));
    if (row.every(value => value === 0)) break;
    const [originalFirstThunk, , , nameRva, firstThunk] = row;
    if (iatRva < firstThunk || (iatRva - firstThunk) % 8 !== 0) continue;
    const index = (iatRva - firstThunk) / 8;
    assert.ok(index < 65536, 'Import lookup exceeds fixed bound');
    let entryValue = 0n;
    for (let j = 0; j <= index; j++) {
      entryValue = u64(offsetOf((originalFirstThunk || firstThunk) + j * 8, 8));
      if (entryValue === 0n) break;
    }
    if (entryValue === 0n) continue;
    const ordinal = (entryValue & 0x8000000000000000n) !== 0n;
    const symbol = ordinal ? {ordinal: Number(entryValue & 0xffffn)} : {hint: u16(offsetOf(Number(entryValue), 2)), name: asciiAt(Number(entryValue) + 2)};
    Object.assign(result, {status: 'resolved_normal_import', normal_import: {dll: asciiAt(nameRva), ...symbol, descriptor_rva: hex(importRva + i * 20), first_thunk_rva: hex(firstThunk), original_first_thunk_rva: hex(originalFirstThunk), thunk_index: index, lookup_entry_value: hex(entryValue)}});
    break;
  }
}
result.sha256_after = sha256(fs.readFileSync(target));
assert.equal(result.sha256_after, expectedHash, 'Target changed during static inspection');
fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
console.log(JSON.stringify(result, null, 2));
