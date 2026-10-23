// Fixed, read-only metadata lookup in the already-created Engine copy.
// No target loading, instruction interpretation, backend, installation or subprocess.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const target = path.join(root, '.tooling/offline-cache-node/Engine.readonly.dll');
const output = path.join(root, 'docs/object-lifecycle/offline-cache-node-inspection/move-import-binding.json');
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const iatRva = 0x302728;
// Existing memcmp-import.json and name-copy-thunk-0x1802ddb0e.json agree on
// this descriptor/table. Pin that metadata context; do not search new functions.
const descriptorRva = 0x41569c;
const expectedFirstThunk = 0x3026f8;
const expectedOriginalFirstThunk = 0x415eb0;
const chunkBytes = 65536;
const hex = value => `0x${value.toString(16)}`;
assert.ok(!fs.existsSync(output), 'Preserve existing evidence');
assert.equal(fs.realpathSync(target).toLowerCase(), target.toLowerCase(), 'Unexpected target path redirection');
const fd = fs.openSync(target, 'r');
let metadataBytesRead = 0;
try {
  const initialStat = fs.fstatSync(fd);
  assert.ok(initialStat.isFile(), 'Target is not a regular file');
  assert.equal(initialStat.size, 4485632, 'Unexpected fixed target size');
  const fileBytes = initialStat.size;
  function read(offset, count) {
    assert.ok(Number.isSafeInteger(offset) && offset >= 0 &&
      Number.isSafeInteger(count) && count > 0 && count <= chunkBytes &&
      offset + count <= fileBytes, 'Metadata read outside fixed file bounds');
    const bytes = Buffer.alloc(count);
    let done = 0;
    while (done < count) {
      const amount = fs.readSync(fd, bytes, done, count - done, offset + done);
      assert.ok(amount > 0, 'Unexpected EOF');
      done += amount;
    }
    metadataBytesRead += count;
    return bytes;
  }
  function fingerprint() {
    const hash = createHash('sha256');
    const buffer = Buffer.alloc(chunkBytes);
    let offset = 0;
    while (offset < fileBytes) {
      const amount = fs.readSync(fd, buffer, 0, Math.min(chunkBytes, fileBytes - offset), offset);
      assert.ok(amount > 0, 'Unexpected EOF during fingerprinting');
      hash.update(buffer.subarray(0, amount));
      offset += amount;
    }
    return hash.digest('hex');
  }
  const hashBefore = fingerprint();
  assert.equal(hashBefore, expectedHash, 'Target fingerprint changed');
  const dos = read(0, 64);
  assert.equal(dos.readUInt16LE(0), 0x5a4d, 'Expected MZ');
  const peOffset = dos.readUInt32LE(0x3c);
  const pe = read(peOffset, 24);
  assert.equal(pe.readUInt32LE(0), 0x4550, 'Expected PE');
  assert.equal(pe.readUInt16LE(4), 0x8664, 'Expected AMD64');
  const sectionCount = pe.readUInt16LE(6);
  const optionalSize = pe.readUInt16LE(20);
  assert.ok(sectionCount > 0 && sectionCount <= 96, 'Section count outside bounded domain');
  assert.ok(optionalSize >= 128 && optionalSize <= 4096, 'Optional header outside bounded domain');
  // Only the first two data-directory entries are needed.
  const optional = read(peOffset + 24, 128);
  assert.equal(optional.readUInt16LE(0), 0x20b, 'Expected PE32+');
  assert.ok(optional.readUInt32LE(108) >= 2, 'Missing import directory');
  const imageBase = optional.readBigUInt64LE(24);
  assert.equal(imageBase, 0x180000000n, 'Unexpected image base');
  const headerBytes = optional.readUInt32LE(60);
  const importRva = optional.readUInt32LE(120);
  const importSize = optional.readUInt32LE(124);
  assert.ok(headerBytes > 0 && headerBytes <= fileBytes, 'Invalid header size');
  assert.ok(importRva > 0 && importSize >= 20 && importSize <= 20 * 1024, 'Invalid import directory');
  const sectionTableOffset = peOffset + 24 + optionalSize;
  const sectionTable = read(sectionTableOffset, sectionCount * 40);
  const sections = [];
  for (let index = 0; index < sectionCount; index++) {
    const at = index * 40;
    sections.push({
      rva: sectionTable.readUInt32LE(at + 12),
      rawSize: sectionTable.readUInt32LE(at + 16),
      raw: sectionTable.readUInt32LE(at + 20),
      characteristics: sectionTable.readUInt32LE(at + 36),
    });
  }
  function offsetOf(rva, count) {
    assert.ok(Number.isSafeInteger(rva) && rva >= 0 && rva <= 0xffffffff &&
      Number.isSafeInteger(count) && count > 0 && rva + count <= 0x100000000, 'Invalid metadata RVA');
    if (rva + count <= headerBytes) return rva;
    const mapped = sections.filter(section => rva >= section.rva && rva + count <= section.rva + section.rawSize);
    assert.equal(mapped.length, 1, 'Metadata RVA must be uniquely file-backed');
    assert.equal(mapped[0].characteristics & 0x20000000, 0, 'Refuse instruction-section metadata reads');
    const offset = mapped[0].raw + rva - mapped[0].rva;
    assert.ok(offset >= 0 && offset + count <= fileBytes, 'Metadata range exceeds file');
    return offset;
  }
  function evidenceBytes(rva, count) {
    const offset = offsetOf(rva, count);
    return {rva: hex(rva), file_offset: hex(offset), bytes: read(offset, count).toString('hex')};
  }
  function asciiZ(rva) {
    const values = [];
    for (let index = 0; index < 512; index++) {
      const value = read(offsetOf(rva + index, 1), 1)[0];
      values.push(value);
      if (value === 0) {
        return {rva: hex(rva), file_offset: hex(offsetOf(rva, values.length)),
          bytes: Buffer.from(values).toString('hex'), text: Buffer.from(values.slice(0, -1)).toString('ascii')};
      }
      assert.ok(value >= 0x20 && value < 0x7f, 'Unexpected nonprintable import name');
    }
    throw Error('Import name exceeds fixed bound');
  }
  assert.ok(descriptorRva >= importRva && descriptorRva + 20 <= importRva + importSize &&
    (descriptorRva - importRva) % 20 === 0, 'Pinned descriptor is outside import table');
  const descriptorIndex = (descriptorRva - importRva) / 20;
  // Validate descriptor continuity, without resolving or exporting other imports.
  for (let index = 0; index < descriptorIndex; index++) {
    const preceding = read(offsetOf(importRva + index * 20, 20), 20);
    assert.ok(preceding.some(value => value !== 0), 'Import terminator precedes pinned descriptor');
  }
  const descriptor = evidenceBytes(descriptorRva, 20);
  const descriptorBytes = Buffer.from(descriptor.bytes, 'hex');
  const originalFirstThunk = descriptorBytes.readUInt32LE(0);
  const dllNameRva = descriptorBytes.readUInt32LE(12);
  const firstThunk = descriptorBytes.readUInt32LE(16);
  assert.equal(firstThunk, expectedFirstThunk, 'Pinned FirstThunk changed');
  assert.equal(originalFirstThunk, expectedOriginalFirstThunk, 'Pinned OriginalFirstThunk changed');
  assert.ok(iatRva >= firstThunk && (iatRva - firstThunk) % 8 === 0, 'Invalid fixed IAT index');
  const thunkIndex = (iatRva - firstThunk) / 8;
  assert.equal(thunkIndex, 6, 'Unexpected fixed thunk index');
  // Prefix is needed only to prove that the selected entry precedes the terminator.
  const lookupPrefix = evidenceBytes(originalFirstThunk, (thunkIndex + 1) * 8);
  const prefixBytes = Buffer.from(lookupPrefix.bytes, 'hex');
  for (let index = 0; index <= thunkIndex; index++) {
    assert.notEqual(prefixBytes.readBigUInt64LE(index * 8), 0n, 'Import lookup terminates before fixed slot');
  }
  const lookupRva = originalFirstThunk + thunkIndex * 8;
  const lookup = {
    rva: hex(lookupRva), file_offset: hex(offsetOf(lookupRva, 8)),
    bytes: prefixBytes.subarray(thunkIndex * 8, (thunkIndex + 1) * 8).toString('hex'),
  };
  const lookupValue = prefixBytes.readBigUInt64LE(thunkIndex * 8);
  assert.equal(lookupValue & 0x8000000000000000n, 0n, 'Ordinal import is outside this name-binding operation');
  assert.ok(lookupValue > 0n && lookupValue <= 0xffffffffn, 'Import name RVA outside PE32+ normal-import domain');
  const iat = evidenceBytes(iatRva, 8);
  assert.equal(Buffer.from(iat.bytes, 'hex').readBigUInt64LE(0), lookupValue, 'On-disk IAT/OFT values differ');
  const hint = evidenceBytes(Number(lookupValue), 2);
  const symbol = asciiZ(Number(lookupValue) + 2);
  const dll = asciiZ(dllNameRva);
  const hashAfter = fingerprint();
  assert.equal(hashAfter, expectedHash, 'Target changed during metadata inspection');
  const finalStat = fs.fstatSync(fd);
  assert.equal(finalStat.size, initialStat.size, 'File size changed');
  assert.equal(finalStat.mtimeMs, initialStat.mtimeMs, 'File timestamp changed');
  const result = {
    status: 'resolved-fixed-normal-import', utc: new Date().toISOString(),
    target, output, expected_sha256: expectedHash, sha256_before: hashBefore, sha256_after: hashAfter,
    hash_chunk_bytes: chunkBytes, file_bytes: fileBytes,
    image_base: hex(imageBase), iat_va: hex(imageBase + BigInt(iatRva)), iat_rva: hex(iatRva),
    source_call_va: '0x180022881',
    source_evidence: [
      'docs/object-lifecycle/offline-cache-node-inspection/022860-instructions.json',
      'docs/object-lifecycle/record-load-discovery/memcmp-import.json',
      'docs/object-lifecycle/name-interface-discovery/name-copy-thunk-0x1802ddb0e.json',
    ],
    normal_import: {dll: dll.text, name: symbol.text, hint: Buffer.from(hint.bytes, 'hex').readUInt16LE(0),
      descriptor_index: descriptorIndex, first_thunk_rva: hex(firstThunk),
      original_first_thunk_rva: hex(originalFirstThunk), thunk_index: thunkIndex, lookup_value: hex(lookupValue)},
    raw_metadata: {descriptor, lookup_prefix_through_target: lookupPrefix, target_lookup: lookup, iat,
      hint, symbol_name_with_nul: symbol, dll_name_with_nul: dll},
    metadata_bytes_read: metadataBytesRead,
    headers: {pe_file_offset: hex(peOffset), optional_header_file_offset: hex(peOffset + 24),
      optional_header_size: optionalSize, section_table_file_offset: hex(sectionTableOffset),
      section_count: sectionCount, import_directory_rva: hex(importRva), import_directory_size: importSize},
    target_loaded_or_executed: false, instruction_bytes_interpreted: 0,
    backend_launches: 0, subprocesses: 0, installations: false, permission_escalation: false,
    limitations: [
      'Only the previously copied and fingerprinted Engine file is read; full-file passes are 64 KiB streaming hashes.',
      'Interpretation is limited to required PE headers, descriptor continuity, one lookup prefix and one IAT/name binding.',
      'No complete import-name list, new instruction body, runtime resolution, delay import or forwarder is inspected.',
      'The imported symbol establishes static identity; successful copy semantics remain an explicit external contract.',
    ],
  };
  fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
  console.log(JSON.stringify({status: result.status, dll: dll.text, name: symbol.text,
    iat_va: result.iat_va, output, sha256_before: hashBefore, sha256_after: hashAfter,
    metadata_bytes_read: metadataBytesRead}));
} finally {
  fs.closeSync(fd);
}
