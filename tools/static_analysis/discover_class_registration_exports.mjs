// Prepared fixed-prefix PE export metadata lookup. Parent reviews before running.
// No DLL loading, code disassembly, whole-name-table scan or subprocess.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(script), '../..');
const targets = Object.freeze([
  {name: 'Game.dll', path: 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll',
    sha256: 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2', bytes: 11385856},
  {name: 'Engine.dll', path: 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll',
    sha256: '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153', bytes: 4485632},
]);
const prefixes = Object.freeze([
  '??0RTTI_ClassInfo@GAME@@',
  '??1RTTI_ClassInfo@GAME@@',
  '?Register@RTTI_ClassInfo@GAME@@',
  '??__E?classInfo@QuestItem@GAME@@',
  '?classInfo@Item@GAME@@',
  '?classInfo@Actor@GAME@@',
  '?classInfo@Object@GAME@@',
  '?GetRTTIClassInfo@QuestItem@GAME@@',
  '?GetRTTIClassInfo@Item@GAME@@',
]);
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-class-registration-exports');
const output = path.join(outputDirectory, 'discovery.json');
const limits = Object.freeze({
  fingerprint_chunk_bytes: 65536,
  metadata_read_bytes_per_dll: 65536,
  single_metadata_read_bytes: 4096,
  metadata_read_operations_per_dll: 8192,
  pe_header_end: 65536,
  section_count: 96,
  optional_header_bytes: 4096,
  export_function_count: 1000000,
  export_name_count: 1000000,
  ascii_bytes_per_name_including_nul: 512,
  ascii_chunk_bytes: 32,
  distinct_export_names_per_dll: 256,
  binary_search_steps_per_prefix: 32,
  candidates_total_both_dlls: 32,
  report_bytes: 2 * 1024 * 1024,
});
const hex = value => `0x${value.toString(16)}`;
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const within = (offset, length, size) =>
  Number.isSafeInteger(offset) && offset >= 0 && Number.isSafeInteger(length) &&
  length > 0 && Number.isSafeInteger(size) && offset <= size && length <= size - offset;
for (let directory = path.dirname(outputDirectory); ; directory = path.dirname(directory)) {
  const stat = fs.lstatSync(directory);
  assert(stat.isDirectory() && !stat.isSymbolicLink(), 'Output ancestor must be a real directory');
  assert.equal(fs.realpathSync(directory).toLowerCase(), path.resolve(directory).toLowerCase(),
    'Unexpected output ancestor redirection');
  if (directory === path.dirname(directory)) break;
}
assert(!fs.existsSync(outputDirectory), 'Preserve existing evidence directory');

const result = {
  schema: 'grim-dynasty-fixed-class-registration-export-metadata-v1',
  started_utc: new Date().toISOString(), status: 'prepared',
  script_sha256: hash(fs.readFileSync(script)),
  operation: 'Nine explicit prefixes in each fixed DLL sorted export-name table',
  prefixes, limits,
  target_open_mode: 'O_RDONLY',
  target_loaded_or_executed: false, backend_started: false, subprocesses: 0,
  installations: false, permission_escalation: false, target_copy_created: false,
  target_code_bytes_interpreted: 0, exported_data_payload_bytes_read: 0,
  vtable_or_classinfo_slots_read: 0, whole_export_name_table_scanned: false,
  selected_count: 0,
  files: targets.map(target => ({
    target: target.path, expected_sha256: target.sha256, expected_file_bytes: target.bytes,
    status: 'not-started', metadata_read_bytes: 0, ascii_read_bytes: 0,
    metadata_read_operations: [], fingerprint_passes: [], encountered_names: [], candidates: [],
    searches: prefixes.map(prefix => ({
      prefix, method: 'lower-bound binary search, bounded matches, one terminator probe',
      status: 'not-started', binary_search_steps: 0, probes: [], first_index: null,
      selected_count: 0, stopping_name_index: null,
    })),
  })),
  limitations: [
    'Full-file 64KiB streaming hash passes traverse all original bytes without decoding or executing them. Each DLL has a separate 64KiB semantic metadata-read budget.',
    'The PE export-name pointer table is assumed sorted by ASCII name. Only encountered names are checked for relative ordering; no complete name table scan is performed.',
    'No matching named export means absence under that sorted-table contract, not absence of an unexported/inlined function, initializer, registration path or class.',
    'Exact decorated names and on-disk preferred addresses are retained without demangling or runtime binding. Data/function behavior, ABI, layout and callable boundaries are not inferred.',
    'Export address targets are classified only using already-read section metadata; their payloads, vtables and classInfo records are never read.',
    'A selected forwarder string may be reported as export metadata, but its destination is not opened or followed. No imported dependency or backend is started.',
    'The 32-candidate budget applies to both DLLs combined. Exceeding any fixed budget stops the run; no prefix or target expansion occurs.',
    'Node O_RDONLY does not expose a FileShare.Read exclusivity contract. Same-handle stats and before/after hashes require a stable source and are not an atomic-snapshot guarantee.',
  ],
};

function inspect(target, report) {
  let fd, beforeStat, admittedSize = false, failure;
  const hashBuffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
  const names = new Map();
  report.started_utc = new Date().toISOString();
  report.status = 'started';
  function fingerprint(label) {
    assert.equal(fs.fstatSync(fd).size, target.bytes, 'Fixed file size changed before hashing');
    const digest = createHash('sha256');
    let at = 0;
    while (at < target.bytes) {
      const count = fs.readSync(fd, hashBuffer, 0, Math.min(hashBuffer.length, target.bytes - at), at);
      assert(count > 0, 'Unexpected EOF during fingerprint');
      digest.update(hashBuffer.subarray(0, count));
      at += count;
    }
    const sha256 = digest.digest('hex');
    report.fingerprint_passes.push({label, bytes: at, chunk_bytes: hashBuffer.length, sha256});
    return sha256;
  }
  function read(fileOffset, length, kind) {
    assert(within(fileOffset, length, target.bytes), 'Metadata read exceeds fixed file');
    assert(length <= limits.single_metadata_read_bytes, 'Single metadata-read limit');
    assert(report.metadata_read_operations.length < limits.metadata_read_operations_per_dll,
      'Metadata operation limit');
    assert(length <= limits.metadata_read_bytes_per_dll - report.metadata_read_bytes,
      'Per-DLL 64KiB metadata limit');
    report.metadata_read_bytes += length;
    if (kind === 'export-ascii') report.ascii_read_bytes += length;
    const operation = {kind, file_offset: fileOffset, requested_bytes: length, read_bytes: 0};
    report.metadata_read_operations.push(operation);
    const bytes = Buffer.alloc(length);
    while (operation.read_bytes < length) {
      const count = fs.readSync(fd, bytes, operation.read_bytes, length - operation.read_bytes,
        fileOffset + operation.read_bytes);
      assert(count > 0, 'Unexpected EOF during metadata read');
      operation.read_bytes += count;
    }
    return bytes;
  }
  try {
    assert.equal(fs.realpathSync(target.path).toLowerCase(), path.resolve(target.path).toLowerCase(),
      'Fixed target path redirected');
    fd = fs.openSync(target.path, fs.constants.O_RDONLY);
    beforeStat = fs.fstatSync(fd);
    assert(beforeStat.isFile(), 'Fixed target is not a regular file');
    assert.equal(beforeStat.size, target.bytes, 'Fixed target size mismatch');
    admittedSize = true;
    report.sha256_before = fingerprint('before');
    assert.equal(report.sha256_before, target.sha256, 'Fixed hash mismatch: no semantic reads allowed');

    const dos = read(0, 64, 'dos-header');
    assert.equal(dos.readUInt16LE(0), 0x5a4d, 'Expected MZ');
    const pe = dos.readUInt32LE(60);
    assert(pe >= 64 && pe + 24 <= limits.pe_header_end, 'PE header budget');
    const coff = read(pe, 24, 'signature-and-coff-header');
    assert.equal(coff.readUInt32LE(0), 0x4550, 'Expected PE');
    assert.equal(coff.readUInt16LE(4), 0x8664, 'Expected AMD64');
    const sectionCount = coff.readUInt16LE(6), optionalLength = coff.readUInt16LE(20);
    assert(sectionCount > 0 && sectionCount <= limits.section_count, 'Section count budget');
    assert(optionalLength >= 120 && optionalLength <= limits.optional_header_bytes, 'Optional header budget');
    assert(pe + 24 + optionalLength + sectionCount * 40 <= limits.pe_header_end, 'Header layout budget');
    const optional = read(pe + 24, 120, 'optional-header-export-prefix');
    assert.equal(optional.readUInt16LE(0), 0x20b, 'Expected PE32+');
    const imageBase = optional.readBigUInt64LE(24);
    assert.equal(imageBase, 0x180000000n, 'Unexpected preferred image base');
    const imageSize = optional.readUInt32LE(56), headerBytes = optional.readUInt32LE(60);
    assert(headerBytes <= limits.pe_header_end &&
      headerBytes >= pe + 24 + optionalLength + sectionCount * 40 &&
      headerBytes <= target.bytes, 'Invalid SizeOfHeaders');
    assert(optional.readUInt32LE(108) >= 1, 'Missing export directory entry');
    const sectionBytes = read(pe + 24 + optionalLength, sectionCount * 40, 'section-headers');
    const sections = [];
    for (let i = 0; i < sectionCount; i++) {
      const at = i * 40;
      const section = {
        name: sectionBytes.subarray(at, at + 8).toString('ascii').replace(/\0.*$/, ''),
        rva: sectionBytes.readUInt32LE(at + 12), virtual_size: sectionBytes.readUInt32LE(at + 8),
        raw_size: sectionBytes.readUInt32LE(at + 16), raw_offset: sectionBytes.readUInt32LE(at + 20),
        characteristics: sectionBytes.readUInt32LE(at + 36),
      };
      if (section.raw_size) assert(within(section.raw_offset, section.raw_size, target.bytes),
        'Section raw extent exceeds fixed file');
      sections.push(section);
    }
    function mapping(rva, length = 1) {
      assert(within(rva, length, imageSize), 'Metadata RVA exceeds image');
      const matches = sections.filter(section => rva >= section.rva &&
        within(rva - section.rva, length, section.raw_size));
      assert.equal(matches.length, 1, 'Metadata must have one file-backed section');
      const section = matches[0], fileOffset = section.raw_offset + rva - section.rva;
      assert(within(fileOffset, length, target.bytes), 'Mapped metadata exceeds fixed file');
      return {section, fileOffset, remaining: section.raw_size - (rva - section.rva)};
    }
    const exportRva = optional.readUInt32LE(112), exportSize = optional.readUInt32LE(116);
    assert(exportRva > 0 && exportSize >= 40 && within(exportRva, exportSize, imageSize),
      'Invalid export directory extent');
    function metadataAt(rva, length, kind) {
      assert(rva >= exportRva && within(rva - exportRva, length, exportSize),
        'Semantic read outside export directory');
      const mapped = mapping(rva, length);
      assert.equal(mapped.section.characteristics & 0x20000000, 0,
        'Refuse export metadata in executable section');
      return read(mapped.fileOffset, length, kind);
    }
    function asciiAt(rva) {
      const pieces = [];
      let inspected = 0;
      while (inspected < limits.ascii_bytes_per_name_including_nul) {
        const mapped = mapping(rva + inspected);
        const count = Math.min(limits.ascii_chunk_bytes,
          limits.ascii_bytes_per_name_including_nul - inspected,
          mapped.remaining, exportRva + exportSize - (rva + inspected));
        assert(count > 0, 'Export string leaves declared metadata extent');
        const bytes = metadataAt(rva + inspected, count, 'export-ascii');
        for (let i = 0; i < bytes.length; i++) {
          if (bytes[i] === 0) {
            pieces.push(bytes.subarray(0, i));
            const name = Buffer.concat(pieces);
            return {text: name.toString('ascii'), raw_with_nul: Buffer.concat([name, Buffer.from([0])]).toString('hex'),
              name_file_offset: mapping(rva).fileOffset};
          }
          assert(bytes[i] >= 0x20 && bytes[i] < 0x7f, 'Unexpected export-name byte');
        }
        pieces.push(bytes);
        inspected += bytes.length;
      }
      throw Error('Unterminated export name or 512-byte name limit');
    }
    const directory = metadataAt(exportRva, 40, 'export-directory');
    const ordinalBase = directory.readUInt32LE(16), functionCount = directory.readUInt32LE(20);
    const nameCount = directory.readUInt32LE(24), functionsRva = directory.readUInt32LE(28);
    const namesRva = directory.readUInt32LE(32), ordinalsRva = directory.readUInt32LE(36);
    assert(functionCount > 0 && functionCount <= limits.export_function_count, 'Export function count budget');
    // Name aliases may share an ordinal; nameCount <= functionCount is not required.
    assert(nameCount <= limits.export_name_count, 'Export name count budget');
    report.preferred_image_base = hex(imageBase);
    report.pe = {
      pe_header_file_offset: pe, size_of_headers: headerBytes, size_of_image: imageSize, sections,
      export_directory_rva: hex(exportRva), export_directory_size: exportSize,
      export_directory_file_offset: mapping(exportRva, 40).fileOffset,
      export_directory_bytes: directory.toString('hex'),
      ordinal_base: ordinalBase, function_count: functionCount, named_export_count: nameCount,
      address_table_rva: hex(functionsRva), name_pointer_table_rva: hex(namesRva),
      name_ordinal_table_rva: hex(ordinalsRva),
    };
    function nameAt(index) {
      assert(Number.isInteger(index) && index >= 0 && index < nameCount, 'Name index outside table');
      if (names.has(index)) return names.get(index);
      assert(names.size < limits.distinct_export_names_per_dll, 'Distinct export name probe budget');
      const pointerRva = namesRva + index * 4;
      const pointer = metadataAt(pointerRva, 4, 'selected-name-pointer');
      const nameRva = pointer.readUInt32LE(0), name = asciiAt(nameRva);
      const item = {
        name_index: index, name_rva: hex(nameRva), decorated_name: name.text,
        raw_name_with_nul: name.raw_with_nul, name_file_offset: name.name_file_offset,
        pointer_file_offset: mapping(pointerRva, 4).fileOffset, raw_name_pointer: pointer.toString('hex'),
      };
      for (const [otherIndex, other] of names) {
        assert(otherIndex < index ? other.decorated_name <= item.decorated_name :
          other.decorated_name >= item.decorated_name, 'Encountered names violate sorted-table contract');
      }
      names.set(index, item);
      return item;
    }
    for (const search of report.searches) {
      search.status = 'started';
      const prefix = search.prefix;
      let lo = 0, hi = nameCount;
      while (lo < hi) {
        assert(search.binary_search_steps < limits.binary_search_steps_per_prefix, 'Binary search step budget');
        const mid = lo + Math.floor((hi - lo) / 2), item = nameAt(mid);
        search.probes.push({index: mid, comparison_to_prefix: item.decorated_name < prefix ? -1 :
          item.decorated_name === prefix ? 0 : 1});
        search.binary_search_steps++;
        if (item.decorated_name < prefix) lo = mid + 1;
        else hi = mid;
      }
      search.first_index = lo;
      if (lo > 0) assert(nameAt(lo - 1).decorated_name < prefix, 'Lower-bound predecessor inconsistent');
      for (let index = lo; index < nameCount; index++) {
        const item = nameAt(index);
        if (!item.decorated_name.startsWith(prefix)) {
          search.stopping_name_index = index;
          break;
        }
        assert(result.selected_count < limits.candidates_total_both_dlls,
          'More than 32 selected exports across fixed DLLs: stop without expansion');
        const ordinalRva = ordinalsRva + index * 2;
        const ordinalBytes = metadataAt(ordinalRva, 2, 'selected-name-ordinal');
        const ordinalIndex = ordinalBytes.readUInt16LE(0);
        assert(ordinalIndex < functionCount, 'Selected ordinal outside EAT');
        const addressRva = functionsRva + ordinalIndex * 4;
        const addressBytes = metadataAt(addressRva, 4, 'selected-export-address');
        const functionRva = addressBytes.readUInt32LE(0);
        assert(functionRva < imageSize, 'Selected export target outside image');
        const forwarded = functionRva >= exportRva && functionRva - exportRva < exportSize;
        let mapped = null;
        if (functionRva && !forwarded) {
          // Classify the address with existing headers only. Do not read its bytes.
          const owners = sections.filter(section => functionRva >= section.rva &&
            functionRva - section.rva < Math.max(section.raw_size, section.virtual_size));
          assert.equal(owners.length, 1, 'Export target must have one section');
          const section = owners[0], backed = functionRva - section.rva < section.raw_size;
          mapped = {section, offset: backed ? section.raw_offset + functionRva - section.rva : null};
        }
        const forwardedName = forwarded ? asciiAt(functionRva) : null;
        report.candidates.push({
          prefix, ...item, ordinal: ordinalBase + ordinalIndex, export_address_table_index: ordinalIndex,
          rva: hex(functionRva), preferred_va: functionRva && !forwarded ? hex(imageBase + BigInt(functionRva)) : null,
          file_offset: mapped && mapped.offset !== null ? hex(mapped.offset) : null,
          file_backed: mapped ? mapped.offset !== null : null,
          section: mapped ? mapped.section.name : null,
          section_executable: mapped ? Boolean(mapped.section.characteristics & 0x20000000) : null,
          forwarder: forwardedName ? forwardedName.text : null,
          raw_forwarder_with_nul: forwardedName ? forwardedName.raw_with_nul : null,
          ordinal_file_offset: mapping(ordinalRva, 2).fileOffset, raw_ordinal: ordinalBytes.toString('hex'),
          address_file_offset: mapping(addressRva, 4).fileOffset, raw_export_address: addressBytes.toString('hex'),
          target_payload_bytes_read: 0,
        });
        result.selected_count++;
        search.selected_count++;
      }
      search.status = search.selected_count ? 'matching-named-exports-found' : 'no-matching-named-export-found';
    }
    report.selected_count = report.candidates.length;
    report.status = 'metadata-discovery-completed';
  } catch (error) {
    failure = error;
    report.status = 'failed';
    report.failure = String(error);
    const active = report.searches.find(search => search.status === 'started');
    if (active) { active.status = 'failed'; active.failure = String(error); }
  } finally {
    report.encountered_names = [...names.values()].sort((a, b) => a.name_index - b.name_index);
    if (fd !== undefined) {
      try {
        if (admittedSize) {
          report.sha256_after = fingerprint('after');
          const after = fs.fstatSync(fd);
          report.source_stat_stable = after.isFile() && after.size === beforeStat.size &&
            after.dev === beforeStat.dev && after.ino === beforeStat.ino &&
            after.mtimeMs === beforeStat.mtimeMs && after.ctimeMs === beforeStat.ctimeMs;
          report.source_hash_preserved = report.sha256_before === target.sha256 &&
            report.sha256_after === target.sha256;
          assert(report.source_stat_stable && report.source_hash_preserved,
            'Fixed target changed or hash mismatched');
        }
      } catch (error) {
        report.final_fingerprint_failure = String(error);
        failure ??= error;
        report.status = 'failed';
      } finally {
        try { fs.closeSync(fd); }
        catch (error) { report.close_failure = String(error); failure ??= error; report.status = 'failed'; }
      }
    }
    report.finished_utc = new Date().toISOString();
  }
  if (failure) throw failure;
}

let failure;
try {
  result.status = 'started';
  for (let i = 0; i < targets.length; i++) inspect(targets[i], result.files[i]);
  result.status = 'fixed-class-registration-export-discovery-completed';
} catch (error) {
  failure = error;
  result.status = 'failed';
  result.failure = String(error);
} finally {
  result.finished_utc = new Date().toISOString();
  result.controller_resource_usage = process.resourceUsage();
  const encoded = `${JSON.stringify(result, null, 2)}\n`;
  assert(Buffer.byteLength(encoded) <= limits.report_bytes, 'Report exceeds fixed size budget');
  fs.mkdirSync(outputDirectory);
  fs.writeFileSync(output, encoded, {flag: 'wx'});
}
console.log(JSON.stringify({
  status: result.status, output, selected_count: result.selected_count,
  files: result.files.map(file => ({
    target: file.target, status: file.status, metadata_read_bytes: file.metadata_read_bytes,
    source_hash_preserved: file.source_hash_preserved,
    searches: file.searches.map(search => ({prefix: search.prefix, status: search.status, count: search.selected_count})),
  })),
}));
if (failure) process.exitCode = 1;
