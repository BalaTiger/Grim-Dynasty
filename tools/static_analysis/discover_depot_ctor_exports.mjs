// Fixed PE export metadata discovery only. No target code is interpreted or run.
// Full-file fingerprint passes are separately counted and use one 64 KiB buffer.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedImageBase = 0x180000000n;
const prefixes = ['??0TableDepot@', '??0ObjectManager@'];
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-depot-ctor-exports');
const output = path.join(outputDirectory, 'depot-ctor-exports.json');
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536,
  source_file_bytes: 64 * 1024 * 1024,
  pe_header_end: 65536,
  section_count: 96,
  optional_header_bytes: 4096,
  export_function_count: 1000000,
  export_name_count: 1000000,
  candidates_per_prefix: 8,
  binary_search_steps_per_prefix: 32,
  distinct_export_names: 128,
  ascii_bytes_per_name: 1024,
  ascii_chunk_bytes: 32,
  ascii_read_bytes: 128 * 1024,
  total_metadata_read_bytes: 192 * 1024,
  single_metadata_read_bytes: 4096,
  metadata_read_operations: 8192,
  report_bytes: 2 * 1024 * 1024,
});
const hex = value => `0x${value.toString(16)}`;
const within = (offset, length, size) =>
  Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 &&
  Number.isSafeInteger(size) && offset <= size && length <= size - offset;
assert.ok(!fs.existsSync(output), 'Refuse to overwrite existing evidence');
for (const directory of [
  root, path.join(root, 'docs'), path.join(root, 'docs/object-lifecycle'),
]) {
  const stat = fs.lstatSync(directory);
  assert.ok(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real project directory');
}
if (fs.existsSync(outputDirectory)) {
  const stat = fs.lstatSync(outputDirectory);
  assert.ok(stat.isDirectory() && !stat.isSymbolicLink(), 'Output directory must not be a link');
}

const result = {
  schema: 'grim-dynasty-fixed-depot-constructor-export-metadata-v1',
  started_utc: new Date().toISOString(),
  target,
  expected_sha256: expectedHash,
  operation: 'Two fixed constructor prefixes in the sorted PE export name-pointer table',
  prefixes,
  limits,
  status: 'started',
  target_open_mode: 'read-only',
  target_loaded_or_executed: false,
  backend_started: false,
  target_copy_created: false,
  target_code_bytes_interpreted: 0,
  target_vtable_slots_read: 0,
  whole_export_name_table_scanned: false,
  metadata_read_bytes: 0,
  ascii_read_bytes: 0,
  metadata_read_operations: [],
  fingerprint_passes: [],
  searches: [],
  candidates: [],
  limitations: [
    'The two fingerprint passes traverse all target bytes, including code, without interpreting them; semantic reads are PE headers, sections and bounded export metadata only.',
    'Binary search relies on the PE export name-pointer table being sorted lexically by ASCII name. Only encountered names are checked for ordering; the whole table is not scanned.',
    'A missing prefix means no matching named export was found under that sorting contract. It does not prove that the class has no constructor, or that an unexported/inlined constructor is absent.',
    'Decorated names are retained exactly. Preferred VAs are on-disk image-base plus RVA, not runtime pointers or validated function boundaries.',
    'Export metadata cannot establish constructor behavior, object size, vtable state, cache/gate/counter initialization, incoming callers, or a complete lifecycle.',
    'Read-only opening and same-handle size/stat/fingerprint checks require a stable source; they do not prove the absence of every transient concurrent modification.',
  ],
};
let fd;
let size;
let beforeStat;
let failure;
const fingerprintBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
function fingerprint(label) {
  const hash = createHash('sha256');
  let position = 0;
  while (position < size) {
    const wanted = Math.min(fingerprintBuffer.length, size - position);
    const count = fs.readSync(fd, fingerprintBuffer, 0, wanted, position);
    assert.ok(count > 0, 'Unexpected EOF during fingerprint');
    hash.update(fingerprintBuffer.subarray(0, count));
    position += count;
  }
  const pass = {label, bytes: position, chunk_bytes: fingerprintBuffer.length, sha256: hash.digest('hex')};
  result.fingerprint_passes.push(pass);
  return pass.sha256;
}
function read(offset, length, kind) {
  assert.ok(within(offset, length, size), 'Metadata read exceeds file');
  assert.ok(length <= limits.single_metadata_read_bytes, 'Metadata read exceeds per-operation budget');
  assert.ok(result.metadata_read_operations.length < limits.metadata_read_operations, 'Too many metadata reads');
  assert.ok(length <= limits.total_metadata_read_bytes - result.metadata_read_bytes, 'Metadata byte budget exceeded');
  if (kind === 'ascii') {
    assert.ok(length <= limits.ascii_read_bytes - result.ascii_read_bytes, 'ASCII byte budget exceeded');
    result.ascii_read_bytes += length;
  }
  result.metadata_read_bytes += length;
  const operation = {kind, file_offset: offset, requested_bytes: length, read_bytes: 0};
  result.metadata_read_operations.push(operation);
  const buffer = Buffer.alloc(length);
  while (operation.read_bytes < length) {
    const count = fs.readSync(fd, buffer, operation.read_bytes,
      length - operation.read_bytes, offset + operation.read_bytes);
    assert.ok(count > 0, 'Unexpected EOF during metadata read');
    operation.read_bytes += count;
  }
  return buffer;
}

try {
  fd = fs.openSync(target, 'r');
  beforeStat = fs.fstatSync(fd);
  size = beforeStat.size;
  assert.ok(beforeStat.isFile() && Number.isSafeInteger(size) &&
    size > 0 && size <= limits.source_file_bytes, 'Unexpected fixed target file');
  result.source_bytes = size;
  result.sha256_before = fingerprint('before');
  assert.equal(result.sha256_before, expectedHash, 'Target fingerprint mismatch; do not inspect metadata');

  const dos = read(0, 64, 'dos-header');
  assert.equal(dos.readUInt16LE(0), 0x5a4d, 'Not MZ');
  const pe = dos.readUInt32LE(60);
  assert.ok(pe >= 64 && pe + 24 <= limits.pe_header_end, 'PE header outside fixed header budget');
  const coff = read(pe, 24, 'signature-and-coff-header');
  assert.equal(coff.readUInt32LE(0), 0x4550, 'Not PE');
  assert.equal(coff.readUInt16LE(4), 0x8664, 'Expected AMD64');
  const sectionCount = coff.readUInt16LE(6);
  const optionalLength = coff.readUInt16LE(20);
  assert.ok(sectionCount > 0 && sectionCount <= limits.section_count, 'Section count exceeds budget');
  assert.ok(optionalLength >= 120 && optionalLength <= limits.optional_header_bytes, 'Unexpected optional header size');
  assert.ok(pe + 24 + optionalLength + sectionCount * 40 <= limits.pe_header_end, 'Header layout exceeds budget');
  // Only the first 120 optional-header bytes are needed: PE32+ image base,
  // SizeOfImage/Headers, directory count and the export data-directory pair.
  const optional = read(pe + 24, 120, 'optional-header-prefix');
  assert.equal(optional.readUInt16LE(0), 0x20b, 'Expected PE32+');
  const imageBase = optional.readBigUInt64LE(24);
  assert.equal(imageBase, expectedImageBase, 'Unexpected preferred image base');
  const imageSize = optional.readUInt32LE(56);
  const headerBytes = optional.readUInt32LE(60);
  assert.ok(headerBytes <= limits.pe_header_end &&
    headerBytes >= pe + 24 + optionalLength + sectionCount * 40 &&
    headerBytes <= size, 'Invalid SizeOfHeaders');
  assert.ok(optional.readUInt32LE(108) >= 1, 'Missing export directory');
  const sectionBytes = read(pe + 24 + optionalLength, sectionCount * 40, 'section-headers');
  const sections = [];
  for (let i = 0; i < sectionCount; i++) {
    const at = i * 40;
    const section = {
      name: sectionBytes.subarray(at, at + 8).toString('ascii').replace(/\0.*$/, ''),
      rva: sectionBytes.readUInt32LE(at + 12),
      virtual_size: sectionBytes.readUInt32LE(at + 8),
      raw_size: sectionBytes.readUInt32LE(at + 16),
      raw_offset: sectionBytes.readUInt32LE(at + 20),
      characteristics: sectionBytes.readUInt32LE(at + 36),
    };
    if (section.raw_size) assert.ok(within(section.raw_offset, section.raw_size, size), 'Section raw extent exceeds file');
    sections.push(section);
  }
  function mapping(rva, length = 1) {
    assert.ok(within(rva, length, imageSize), 'RVA exceeds image extent');
    const matches = sections.filter(section =>
      rva >= section.rva && within(rva - section.rva, length, section.raw_size));
    assert.equal(matches.length, 1, 'RVA must have one file-backed section');
    const section = matches[0];
    const offset = section.raw_offset + rva - section.rva;
    assert.ok(within(offset, length, size), 'Mapped RVA exceeds file');
    return {section, offset, remaining: section.raw_size - (rva - section.rva)};
  }
  const exportRva = optional.readUInt32LE(112);
  const exportSize = optional.readUInt32LE(116);
  assert.ok(exportRva > 0 && exportSize >= 40 && within(exportRva, exportSize, imageSize), 'Invalid export extent');
  function metadataAt(rva, length, kind) {
    assert.ok(rva >= exportRva && within(rva - exportRva, length, exportSize),
      'Export metadata read leaves declared export directory extent');
    const mapped = mapping(rva, length);
    assert.equal(mapped.section.characteristics & 0x20000000, 0, 'Refuse export metadata in executable section');
    return read(mapped.offset, length, kind);
  }
  function asciiAt(rva) {
    const chunks = [];
    let inspected = 0;
    while (inspected < limits.ascii_bytes_per_name) {
      const mapped = mapping(rva + inspected);
      const directoryRemaining = exportRva + exportSize - (rva + inspected);
      const count = Math.min(limits.ascii_chunk_bytes, limits.ascii_bytes_per_name - inspected,
        mapped.remaining, directoryRemaining);
      assert.ok(count > 0, 'ASCII name leaves bounded export metadata');
      const chunk = metadataAt(rva + inspected, count, 'ascii');
      for (let i = 0; i < chunk.length; i++) {
        if (chunk[i] === 0) {
          chunks.push(chunk.subarray(0, i));
          return Buffer.concat(chunks).toString('ascii');
        }
        assert.ok(chunk[i] < 128, 'Non-ASCII export name');
      }
      chunks.push(chunk);
      inspected += chunk.length;
    }
    throw Error('Unterminated/over-budget export name');
  }
  const directory = metadataAt(exportRva, 40, 'export-directory');
  const ordinalBase = directory.readUInt32LE(16);
  const functionCount = directory.readUInt32LE(20);
  const nameCount = directory.readUInt32LE(24);
  const functionsRva = directory.readUInt32LE(28);
  const namesRva = directory.readUInt32LE(32);
  const ordinalsRva = directory.readUInt32LE(36);
  assert.ok(functionCount > 0 && functionCount <= limits.export_function_count, 'Function count outside budget');
  // Distinct exported names may alias the same ordinal; do not infer a
  // nameCount <= functionCount constraint. Selected ordinals are checked below.
  assert.ok(nameCount <= limits.export_name_count, 'Name count outside budget');
  result.preferred_image_base = hex(imageBase);
  result.pe = {
    pe_header_file_offset: pe, size_of_headers: headerBytes, size_of_image: imageSize, sections,
    export_directory_rva: hex(exportRva), export_directory_size: exportSize,
    ordinal_base: ordinalBase, function_count: functionCount, named_export_count: nameCount,
    address_table_rva: hex(functionsRva), name_pointer_table_rva: hex(namesRva),
    name_ordinal_table_rva: hex(ordinalsRva),
  };
  const names = new Map();
  function nameAt(index) {
    assert.ok(Number.isInteger(index) && index >= 0 && index < nameCount, 'Name index outside table');
    if (names.has(index)) return names.get(index);
    assert.ok(names.size < limits.distinct_export_names, 'Distinct name probe budget exceeded');
    const nameRva = metadataAt(namesRva + index * 4, 4, 'selected-name-pointer').readUInt32LE(0);
    const item = {name_index: index, name_rva: hex(nameRva), decorated_name: asciiAt(nameRva)};
    for (const [otherIndex, other] of names) {
      assert.ok(otherIndex < index ? other.decorated_name <= item.decorated_name :
        other.decorated_name >= item.decorated_name, 'Encountered export names violate sorted-table contract');
    }
    names.set(index, item);
    return item;
  }
  for (const prefix of prefixes) {
    const search = {prefix, method: 'lower-bound binary search then at most eight matches and one terminator probe',
      binary_search_steps: 0, probes: [], first_index: null, selected_count: 0, status: 'started'};
    result.searches.push(search);
    let lo = 0, hi = nameCount;
    while (lo < hi) {
      assert.ok(search.binary_search_steps < limits.binary_search_steps_per_prefix, 'Binary search step budget exceeded');
      const mid = lo + Math.floor((hi - lo) / 2);
      const item = nameAt(mid);
      search.probes.push({index: mid, comparison_to_prefix: item.decorated_name < prefix ? -1 :
        item.decorated_name === prefix ? 0 : 1});
      search.binary_search_steps++;
      if (item.decorated_name < prefix) lo = mid + 1;
      else hi = mid;
    }
    search.first_index = lo;
    if (lo > 0) assert.ok(nameAt(lo - 1).decorated_name < prefix, 'Lower-bound predecessor inconsistent');
    for (let index = lo; index < nameCount; index++) {
      const item = nameAt(index);
      if (!item.decorated_name.startsWith(prefix)) break;
      assert.ok(search.selected_count < limits.candidates_per_prefix, 'More than eight matching exports; stop without expanding scope');
      const ordinalIndex = metadataAt(ordinalsRva + index * 2, 2, 'selected-name-ordinal').readUInt16LE(0);
      assert.ok(ordinalIndex < functionCount, 'Ordinal index exceeds EAT');
      const functionRva = metadataAt(functionsRva + ordinalIndex * 4, 4, 'selected-export-address').readUInt32LE(0);
      const forwarded = functionRva >= exportRva && functionRva - exportRva < exportSize;
      const mapped = functionRva && !forwarded ? mapping(functionRva) : null;
      result.candidates.push({
        prefix, ...item, ordinal: ordinalBase + ordinalIndex, export_address_table_index: ordinalIndex,
        rva: hex(functionRva),
        preferred_va: functionRva && !forwarded ? hex(imageBase + BigInt(functionRva)) : null,
        file_offset: mapped ? hex(mapped.offset) : null,
        section: mapped ? mapped.section.name : null,
        section_executable: mapped ? Boolean(mapped.section.characteristics & 0x20000000) : null,
        forwarder: forwarded ? asciiAt(functionRva) : null,
        function_bytes_read: 0,
      });
      search.selected_count++;
    }
    search.status = search.selected_count ? 'matching-named-exports-found' : 'no-matching-named-export-found';
  }
  result.encountered_names = [...names.values()].sort((a, b) => a.name_index - b.name_index);
  result.selected_count = result.candidates.length;
  result.status = 'metadata-discovery-completed';
} catch (error) {
  failure = error;
  result.status = 'failed';
  result.failure = String(error);
} finally {
  if (fd !== undefined) {
    try {
      // A mismatched initial fingerprint still never permits semantic reads.
      // Preserve a second same-handle fingerprint for any admitted-size file.
      if (Number.isSafeInteger(size) && size > 0 && size <= limits.source_file_bytes) {
        result.sha256_after = fingerprint('after');
        const afterStat = fs.fstatSync(fd);
        result.source_stat_stable = afterStat.isFile() && afterStat.size === beforeStat.size &&
          afterStat.dev === beforeStat.dev && afterStat.ino === beforeStat.ino &&
          afterStat.mtimeMs === beforeStat.mtimeMs && afterStat.ctimeMs === beforeStat.ctimeMs;
        result.source_hash_preserved = result.sha256_before === expectedHash && result.sha256_after === expectedHash;
        assert.ok(result.source_stat_stable && result.source_hash_preserved, 'Fixed target changed or fingerprint mismatched');
      }
    } catch (error) {
      result.final_fingerprint_failure = String(error);
      failure ??= error;
      result.status = 'failed';
    } finally {
      try { fs.closeSync(fd); }
      catch (error) {
        result.close_failure = String(error);
        failure ??= error;
        result.status = 'failed';
      }
    }
  }
  result.finished_utc = new Date().toISOString();
  const encoded = `${JSON.stringify(result, null, 2)}\n`;
  assert.ok(Buffer.byteLength(encoded) <= limits.report_bytes, 'Report exceeds fixed budget');
  if (!fs.existsSync(outputDirectory)) fs.mkdirSync(outputDirectory);
  fs.writeFileSync(output, encoded, {flag: 'wx'});
}
console.log(JSON.stringify({
  status: result.status, output, selected_count: result.candidates.length,
  searches: result.searches.map(search => ({prefix: search.prefix, status: search.status, count: search.selected_count})),
  metadata_bytes: result.metadata_read_bytes, fingerprint_passes: result.fingerprint_passes,
  source_hash_preserved: result.source_hash_preserved,
}));
if (failure) process.exitCode = 1;
