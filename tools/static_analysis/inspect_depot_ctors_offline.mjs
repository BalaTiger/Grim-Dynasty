// Prepared fixed two-range inspection: TableDepot and ObjectManager, 511 bytes.
// Runs only the existing dumpbin against the existing pinned read-only copy.
// No target execution, automatic callee/continuation following or backend.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
assert.equal(root.toLowerCase(), path.resolve('C:/Users/zhuzi/Documents/Grim-Dynasty').toLowerCase(),
  'This inspection is fixed to the reviewed project');
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const copy = path.join(root, '.tooling/offline-cache-node/Engine.readonly.dll');
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const boundaryPath = 'docs/object-lifecycle/offline-depot-ctor-boundaries/boundary.json';
const boundaryHash = '646053e4565e3f08c696ae0e9a16ddb5c2e81ab22ffe046c5057fc41aa64bb15';
const exportPath = 'docs/object-lifecycle/offline-depot-ctor-exports/depot-ctor-exports.json';
const exportHash = 'f69f67e074b6fe184b86135973be0650129f6fbf00b12162b2fa572c8e1888ce';
const out = path.join(root, 'docs/object-lifecycle/offline-depot-ctors');
const temp = path.join(root, '.tooling/offline-depot-ctors-temp');
const codeByteLimit = 511;
const metadataByteLimit = 32768;
const ranges = [
  {begin: 0x1c4ad0, end: 0x1c4b7b, role: 'TableDepot-constructor', name: 'TableDepot',
    decorated: '??0TableDepot@GAME@@QEAA@XZ'},
  {begin: 0x17dbf0, end: 0x17dd44, role: 'ObjectManager-constructor', name: 'ObjectManager',
    decorated: '??0ObjectManager@GAME@@AEAA@XZ'},
];
assert.equal(ranges.length, 2);
assert.equal(ranges.reduce((count, range) => count + range.end - range.begin, 0), codeByteLimit);
const hex = value => `0x${value.toString(16)}`;
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
function hashFile(file) {
  const fd = fs.openSync(file, 'r'), buffer = Buffer.alloc(65536), digest = createHash('sha256');
  try {
    let count;
    while ((count = fs.readSync(fd, buffer, 0, buffer.length, null)) > 0) digest.update(buffer.subarray(0, count));
  } finally { fs.closeSync(fd); }
  return digest.digest('hex');
}
const write = (name, data) => fs.writeFileSync(path.join(out, name), data, {flag: 'wx'});
const json = (name, data) => write(name, `${JSON.stringify(data, null, 2)}\n`);

assert.ok(!fs.existsSync(out), 'Preserve prior inspection directory');
assert.ok(!fs.existsSync(temp), 'Preserve prior temporary directory');
const proofFile = path.join(root, boundaryPath);
assert.ok(fs.statSync(proofFile).size <= 65536, 'Boundary evidence exceeds text budget');
const proofBytes = fs.readFileSync(proofFile);
assert.equal(hash(proofBytes), boundaryHash, 'Frozen boundary evidence changed');
const frozen = JSON.parse(proofBytes.toString('utf8'));
assert.equal(frozen.status, 'two-exact-runtime-function-ranges-assessed');
assert.equal(frozen.target, original);
assert.equal(frozen.sha256_before, expected);
assert.equal(frozen.sha256_after, expected);
assert.equal(frozen.source_hash_preserved, true);
assert.equal(frozen.code_bytes_interpreted, 0);
assert.equal(frozen.source_evidence.path, exportPath);
assert.equal(frozen.source_evidence.sha256, exportHash);
assert.equal(hashFile(path.join(root, exportPath)), exportHash, 'Source export evidence changed');
assert.equal(frozen.boundaries.length, ranges.length);
assert.equal(frozen.total_selected_range_bytes, codeByteLimit);
for (let i = 0; i < ranges.length; i++) {
  const range = ranges[i], known = frozen.boundaries[i];
  assert.equal(known.name, range.name);
  assert.equal(known.decorated_name, range.decorated);
  assert.equal(known.requested_rva, hex(range.begin));
  assert.equal(known.begin_rva, hex(range.begin));
  assert.equal(known.end_rva_exclusive, hex(range.end));
  assert.equal(known.range_bytes, range.end - range.begin);
  assert.equal(known.code_bytes_read, 0);
  assert.ok(Number.isInteger(known.exception_entry_index) && known.exception_entry_index >= 0);
  assert.ok(/^0x[0-9a-f]+$/.test(known.unwind_info_rva));
}
const toolBefore = hashFile(tool);
assert.equal(toolBefore, toolHash, 'Existing dumpbin changed');
const copyBefore = hashFile(copy);
assert.equal(copyBefore, expected, 'Existing read-only working copy changed');
fs.mkdirSync(out);
fs.mkdirSync(temp);
const record = {
  started_utc: new Date().toISOString(), status: 'started', original, working_copy: copy,
  code_byte_limit: codeByteLimit, metadata_byte_limit: metadataByteLimit, hash_chunk_bytes: 65536,
  code_bytes: 0, metadata_bytes: 0, fragments: [], callee_bodies_followed: 0,
  automatic_continuations_followed: 0, target_loaded_or_executed: false,
  backend_started: false, original_modified: false, full_target_copy_created: false,
  hash_scope: 'Streaming full-file SHA256 before and after this two-range batch per original/copy/tool; these fingerprint reads traverse all bytes without interpreting them',
  boundary_scope: 'Two fixed half-open unwind ranges; .pdata length alone does not establish complete logical constructor behavior',
  dumpbin_io_scope: 'RANGE constrains emitted disassembly; dumpbin may read PE metadata internally and its total file I/O is not instrumented',
  source_evidence: {path: boundaryPath, sha256: boundaryHash},
  export_evidence: {path: exportPath, sha256: exportHash},
  tool, tool_sha256_before: toolBefore, copy_sha256_before: copyBefore,
};
let fd;
const failures = [];
try {
  record.original_sha256_before = hashFile(original);
  assert.equal(record.original_sha256_before, expected, 'Original identity changed');
  fd = fs.openSync(original, 'r');
  const size = fs.fstatSync(fd).size;
  assert.ok(Number.isSafeInteger(size) && size > 0 && size <= 64 * 1024 * 1024);
  function read(offset, length, metadata = true) {
    assert.ok(Number.isSafeInteger(offset) && Number.isSafeInteger(length) &&
      offset >= 0 && length > 0 && offset <= size && length <= size - offset);
    if (metadata) {
      assert.ok(length <= 4096, 'Metadata read exceeds per-operation budget');
      assert.ok(length <= metadataByteLimit - record.metadata_bytes, 'Metadata exceeds 32 KiB batch budget');
    } else {
      assert.ok(length <= codeByteLimit - record.code_bytes, 'Code read exceeds 511-byte batch budget');
    }
    const buffer = Buffer.alloc(length);
    let done = 0;
    while (done < length) {
      const count = fs.readSync(fd, buffer, done, length - done, offset + done);
      assert.ok(count > 0, 'Unexpected end of input');
      done += count;
    }
    if (metadata) record.metadata_bytes += length;
    else record.code_bytes += length;
    return buffer;
  }
  const dos = read(0, 64);
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60);
  assert.ok(pe >= 64 && pe + 24 <= 65536);
  const coff = read(pe, 24);
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  const count = coff.readUInt16LE(6), optionalSize = coff.readUInt16LE(20);
  assert.ok(count > 0 && count <= 96 && optionalSize >= 144 && optionalSize <= 4096);
  assert.ok(pe + 24 + optionalSize + count * 40 <= 65536);
  const optional = read(pe + 24, 144);
  assert.equal(optional.readUInt16LE(0), 0x20b);
  const imageBase = optional.readBigUInt64LE(24);
  assert.equal(imageBase, 0x180000000n);
  const headerSize = optional.readUInt32LE(60), imageSize = optional.readUInt32LE(56);
  assert.ok(headerSize >= pe + 24 + optionalSize + count * 40 && headerSize <= 65536 && headerSize <= size);
  const sections = Array.from({length: count}, (_, index) => {
    const entry = read(pe + 24 + optionalSize + index * 40, 40);
    return {rva: entry.readUInt32LE(12), size: entry.readUInt32LE(16),
      raw: entry.readUInt32LE(20), flags: entry.readUInt32LE(36)};
  });
  function map(rva, length, executable) {
    assert.ok(Number.isSafeInteger(rva) && rva >= 0 && length > 0 && rva + length <= imageSize);
    const matches = sections.filter(section => rva >= section.rva && rva + length <= section.rva + section.size);
    assert.equal(matches.length, 1, 'Range must belong to one file-backed section');
    if (executable) assert.ok(matches[0].flags & 0x20000000, 'Code section must be executable');
    else assert.equal(matches[0].flags & 0x20000000, 0, 'Exception metadata must not be executable');
    return matches[0].raw + rva - matches[0].rva;
  }
  assert.ok(optional.readUInt32LE(108) > 3);
  const pdata = optional.readUInt32LE(136), pdataSize = optional.readUInt32LE(140);
  assert.ok(pdata > 0 && pdataSize > 0 && pdataSize % 12 === 0 && pdataSize <= 2 * 1024 * 1024);
  const pdataOffset = map(pdata, pdataSize, false);
  // Revalidate only the two frozen entries. No new boundary discovery here.
  for (let i = 0; i < ranges.length; i++) {
    const range = ranges[i], known = frozen.boundaries[i], index = known.exception_entry_index;
    assert.ok(index < pdataSize / 12);
    assert.equal(hex(pdata + index * 12), known.exception_entry_rva);
    const entry = read(pdataOffset + index * 12, 12);
    assert.equal(entry.readUInt32LE(0), range.begin, 'Frozen start differs from PE metadata');
    assert.equal(entry.readUInt32LE(4), range.end, 'Frozen end differs from PE metadata');
    assert.equal(hex(entry.readUInt32LE(8)), known.unwind_info_rva, 'Frozen unwind record differs');
  }
  for (const range of ranges) {
    const length = range.end - range.begin;
    const start = imageBase + BigInt(range.begin), end = imageBase + BigInt(range.end);
    const label = range.begin.toString(16).padStart(6, '0');
    const fragment = {
      role: range.role, decorated_name: range.decorated, start_va: hex(start),
      end_va_exclusive: hex(end), code_bytes: length, status: 'started',
    };
    record.fragments.push(fragment);
    const bytes = read(map(range.begin, length, true), length, false);
    fragment.code_sha256 = hash(bytes);
    write(`${hex(start)}.bin`, bytes);
    const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES',
      `/RANGE:${hex(start)},${hex(end - 1n)}`, copy];
    fragment.command_arguments = args;
    const began = Date.now();
    const toolResult = spawnSync(tool, args, {
      cwd: root, windowsHide: true, encoding: 'utf8', timeout: 10000, maxBuffer: 196608,
      env: {SystemRoot: 'C:\\Windows', WINDIR: 'C:\\Windows', PATH: path.dirname(tool), TEMP: temp, TMP: temp},
    });
    fragment.tool_elapsed_ms = Date.now() - began;
    fragment.tool_exit = toolResult.status;
    fragment.tool_signal = toolResult.signal;
    if (toolResult.error) fragment.tool_error = String(toolResult.error);
    write(`${label}-dumpbin.txt`, toolResult.stdout ?? '');
    write(`${label}-stderr.txt`, toolResult.stderr ?? '');
    assert.equal(toolResult.error, undefined);
    assert.equal(toolResult.status, 0);
    assert.equal(toolResult.stderr, '');
    const rows = [];
    for (const line of toolResult.stdout.split(/\r?\n/)) {
      const instruction = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
      if (instruction) {
        rows.push({address: BigInt(`0x${instruction[1]}`),
          bytes: instruction[2].replace(/\s/g, '').toLowerCase(), instruction: instruction[3].trim()});
        continue;
      }
      const continuation = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
      if (continuation) {
        assert.ok(rows.length, 'Byte continuation without an instruction');
        rows.at(-1).bytes += continuation[1].replace(/\s/g, '').toLowerCase();
      }
    }
    assert.ok(rows.length > 0, 'No disassembled instructions');
    let cursor = start;
    for (const row of rows) {
      assert.ok(row.instruction.length > 0);
      assert.ok(row.bytes.length > 0 && row.bytes.length % 2 === 0 && row.bytes.length <= 30);
      assert.equal(row.address, cursor, 'Instruction coverage must be contiguous');
      cursor += BigInt(row.bytes.length / 2);
      assert.ok(cursor <= end, 'Instruction crosses the frozen range end');
    }
    assert.equal(cursor, end, 'Disassembly must cover the entire frozen range');
    assert.equal(rows.map(row => row.bytes).join(''), bytes.toString('hex'),
      'Every disassembled byte must equal the selected original byte');
    json(`${label}-instructions.json`, rows.map(row => ({...row, address: hex(row.address)})));
    fragment.instructions = rows.length;
    fragment.disassembly_sha256 = hash(Buffer.from(toolResult.stdout));
    fragment.byte_coverage_verified = true;
    fragment.status = 'fragment-byte-coverage-verified';
  }
  assert.equal(record.code_bytes, codeByteLimit);
  assert.equal(record.fragments.length, 2);
} catch (error) {
  failures.push(String(error));
} finally {
  if (fd !== undefined) {
    try { fs.closeSync(fd); } catch (error) { failures.push(`Original close: ${String(error)}`); }
  }
  for (const [field, file, required] of [
    ['original_sha256_after', original, expected],
    ['copy_sha256_after', copy, expected],
    ['tool_sha256_after', tool, toolHash],
    ['source_evidence_sha256_after', proofFile, boundaryHash],
    ['export_evidence_sha256_after', path.join(root, exportPath), exportHash],
  ]) {
    try {
      record[field] = hashFile(file);
      if (record[field] !== required) failures.push(`${field}: identity mismatch`);
    } catch (error) { failures.push(`${field}: ${String(error)}`); }
  }
  record.original_hash_matches = record.original_sha256_before === expected && record.original_sha256_after === expected;
  record.copy_hash_matches = record.copy_sha256_before === expected && record.copy_sha256_after === expected;
  record.tool_hash_matches = record.tool_sha256_before === toolHash && record.tool_sha256_after === toolHash;
  record.source_evidence_hash_matches = record.source_evidence_sha256_after === boundaryHash;
  record.export_evidence_hash_matches = record.export_evidence_sha256_after === exportHash;
  record.status = failures.length ? 'failed' : 'fixed-depot-constructors-disassembly-verified';
  record.failures = failures;
  record.finished_utc = new Date().toISOString();
  record.controller_resource_usage = process.resourceUsage();
  json('inspection.json', record);
}
if (failures.length) throw new Error(failures.join('\n'));
console.log(JSON.stringify({
  status: record.status, bytes: record.code_bytes, fragments: record.fragments.length,
  instructions: record.fragments.reduce((count, fragment) => count + fragment.instructions, 0),
  original_unchanged: record.original_hash_matches, copy_unchanged: record.copy_hash_matches,
  tool_unchanged: record.tool_hash_matches, controller_peak_rss_kib: record.controller_resource_usage.maxRSS,
}));
