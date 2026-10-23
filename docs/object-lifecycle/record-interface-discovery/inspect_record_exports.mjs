// Fixed-target PE export metadata only. Never loads a DLL or resolves runtime addresses.
// Header/RVA parsing follows inspect_pe.py and inspect_free_thunk.mjs.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const output = path.join(path.dirname(fileURLToPath(import.meta.url)), 'record-interface-exports.json');
assert.ok(!fs.existsSync(output), 'Preserve existing evidence');
const bytes = fs.readFileSync(target);
const sha256 = value => createHash('sha256').update(value).digest('hex');
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
assert.ok(u16(coff + 16) >= 120 && u32(optional + 108) >= 1, 'Missing export data directory');
const imageBase = u64(optional + 24), headerBytes = u32(optional + 60);
assert.equal(imageBase, 0x180000000n, 'Unexpected preferred image base');
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
    assert.ok(value < 128, 'Non-ASCII export name');
    chars.push(value);
  }
  throw Error('Unterminated export name');
}
function selected(name) {
  let match = /^\?\?0(Object|LoadTable|LoadTableBinary)@/.exec(name);
  if (match) return {class_name: match[1], member: 'constructor', kind: 'constructor'};
  match = /^\?\?_7(Object|LoadTable|LoadTableBinary)@/.exec(name);
  if (match) return {class_name: match[1], member: 'vftable', kind: 'vftable'};
  match = /^\?(Read|SetObjectName)@Object@/.exec(name);
  if (match) return {class_name: 'Object', member: match[1], kind: 'method'};
  match = /^\?(GetString|Read)@(LoadTable|LoadTableBinary)@/.exec(name);
  if (match) return {class_name: match[2], member: match[1], kind: 'method'};
  if (/^\?LoadFile@TableDepot@/.test(name)) return {class_name: 'TableDepot', member: 'LoadFile', kind: 'method'};
  return null;
}
const exportRva = u32(optional + 112), exportSize = u32(optional + 116);
assert.ok(exportRva && exportSize >= 40, 'Missing exports');
const directory = offsetOf(exportRva, 40);
const ordinalBase = u32(directory + 16), functionCount = u32(directory + 20), nameCount = u32(directory + 24);
assert.ok(functionCount <= 1000000 && nameCount <= 1000000, 'Export metadata exceeds fixed bound');
const functionsRva = u32(directory + 28), namesRva = u32(directory + 32), ordinalsRva = u32(directory + 36);
const candidates = [];
for (let i = 0; i < nameCount; i++) {
  const nameRva = u32(offsetOf(namesRva + i * 4, 4));
  const decoratedName = asciiAt(nameRva), selection = selected(decoratedName);
  if (!selection) continue;
  assert.ok(candidates.length < 29, 'More than 29 selected candidates; narrow the fixed filters before continuing');
  const ordinalIndex = u16(offsetOf(ordinalsRva + i * 2, 2));
  assert.ok(ordinalIndex < functionCount, 'Export ordinal index out of range');
  const exportAddressRva = u32(offsetOf(functionsRva + ordinalIndex * 4, 4));
  const forwarded = exportAddressRva >= exportRva && exportAddressRva < exportRva + exportSize;
  candidates.push({
    ...selection, decorated_name: decoratedName, ordinal: ordinalBase + ordinalIndex,
    name_rva: hex(nameRva), export_address_table_index: ordinalIndex,
    rva: hex(exportAddressRva),
    preferred_va: exportAddressRva && !forwarded ? hex(imageBase + BigInt(exportAddressRva)) : null,
    file_offset: exportAddressRva && !forwarded ? hex(offsetOf(exportAddressRva)) : null,
    forwarder: forwarded ? asciiAt(exportAddressRva) : null,
  });
}
const result = {
  operation: 'Fixed Engine PE export metadata selection for Object and record-table interfaces',
  target, expected_sha256: expectedHash, sha256_before: hashBefore,
  target_binary_loaded_or_executed: false, preferred_image_base: hex(imageBase),
  export_directory_rva: hex(exportRva), export_directory_size: exportSize,
  named_export_count: nameCount, selected_count: candidates.length, candidates,
  limitations: [
    'Preferred VAs are image-base plus on-disk export RVAs, not runtime addresses.',
    'Exact decorated export names are retained; no external demangler or backend was used.',
    'Only the fixed named classes and members are selected; absence here does not establish that an unexported procedure is absent.',
    'Export metadata does not establish vtable slot contents, constructor vptr writes, concrete object selection, or behavior.',
    'No code, vtable-slot memory, other binary, player save, Steam cloud or runtime address was inspected.',
  ],
};
result.sha256_after = sha256(fs.readFileSync(target));
assert.equal(result.sha256_after, expectedHash, 'Target changed during inspection');
fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
console.log(JSON.stringify(result, null, 2));
