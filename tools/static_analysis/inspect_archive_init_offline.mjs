// Prepared, not executed here: five frozen archive-initialization fragments, 751 code bytes.
// Reads target bytes; runs only the existing dumpbin on the existing working copy.
// No target execution, automatic callee following, installation, or analysis backend.
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
const caller = 'docs/object-lifecycle/offline-archive-init-boundaries/boundaries.json';
const callerHash = 'fa7b87825a775fdfd48d51c42efc95ccbbdb3fb8d19c6bf972b7935054f034ee';
const out = path.join(root, 'docs/object-lifecycle/offline-archive-init');
const temp = path.join(root, '.tooling/offline-archive-init-temp');
const codeByteLimit = 751;
const metadataByteLimit = 32768;
const ranges = [
  {begin: 0x27f9c0, end: 0x27fb1b, role: 'archive-constructor'},
  {begin: 0x2800d6, end: 0x280143, role: 'archive-load-missing-continuation'},
  {begin: 0x280143, end: 0x28015a, role: 'archive-load-known-unread-jump-target'},
  {begin: 0x27ff20, end: 0x27ffa4, role: 'archive-region-acquisition'},
  {begin: 0x02d950, end: 0x02d9dc, role: 'offset-vector-append'}
];
assert.equal(ranges.reduce((n, r) => n + r.end - r.begin, 0), codeByteLimit);
const hash = b => createHash('sha256').update(b).digest('hex');
function hashFile(p) {
  const fd = fs.openSync(p, 'r'), buf = Buffer.alloc(65536), h = createHash('sha256');
  try {
    let n;
    while ((n = fs.readSync(fd, buf, 0, buf.length, null)) > 0) h.update(buf.subarray(0, n));
  } finally { fs.closeSync(fd); }
  return h.digest('hex');
}
const write = (name, data) => fs.writeFileSync(path.join(out, name), data, {flag: 'wx'});
const json = (name, data) => write(name, JSON.stringify(data, null, 2) + '\n');

assert(!fs.existsSync(out), 'Preserve prior inspection');
assert(!fs.existsSync(temp), 'Preserve prior temporary directory');
const callerPath = path.join(root, caller);
assert.equal(hashFile(callerPath), callerHash, 'Frozen boundary evidence changed');
assert(fs.statSync(callerPath).size <= 65536, 'Boundary evidence exceeds text budget');
const callerBytes = fs.readFileSync(callerPath);
assert.equal(hash(callerBytes), callerHash, 'Boundary evidence changed during read');
const frozen = JSON.parse(callerBytes.toString('utf8'));
assert.equal(frozen.status, 'fixed-archive-init-boundaries-assessed');
assert.equal(frozen.original, original);
assert.equal(path.resolve(frozen.working_copy).toLowerCase(), copy.toLowerCase());
assert.equal(frozen.original_sha256_before, expected);
assert.equal(frozen.original_sha256_after, expected);
assert.equal(frozen.copy_sha256_after, expected);
assert.equal(frozen.code_bytes_read, 0);
assert.equal(frozen.targets.length, ranges.length);
for (let i = 0; i < ranges.length; i++) {
  const r = ranges[i], t = frozen.targets[i];
  assert.equal(t.rva, r.begin);
  assert.equal(t.role, r.role);
  assert.equal(t.boundary.begin_rva, r.begin);
  assert.equal(t.boundary.end_rva, r.end);
  assert.equal(t.boundary.bytes, r.end - r.begin);
  assert(Number.isInteger(t.boundary.index) && t.boundary.index >= 0);
}
const toolBefore = hashFile(tool);
assert.equal(toolBefore, toolHash, 'Existing dumpbin changed');
const copyBefore = hashFile(copy);
assert.equal(copyBefore, expected, 'Existing working copy changed');
fs.mkdirSync(out);
fs.mkdirSync(temp);
const record = {
  started_utc: new Date().toISOString(), status: 'started', original, working_copy: copy,
  code_byte_limit: codeByteLimit, metadata_byte_limit: metadataByteLimit, hash_chunk_bytes: 65536,
  code_bytes: 0, metadata_bytes: 0, fragments: [], callee_bodies_followed: 0,
  target_loaded_or_executed: false, backend_started: false, original_modified: false,
  hash_scope: 'One streaming full-file SHA256 before and after the five-fragment batch per original/copy/tool',
  boundary_scope: 'Five fixed half-open unwind fragments; no claim of complete logical functions',
  source_evidence: {path: caller, sha256: callerHash},
  tool, tool_sha256_before: toolBefore, copy_sha256_before: copyBefore
};
let fd;
const failures = [];
try {
  record.original_sha256_before = hashFile(original);
  assert.equal(record.original_sha256_before, expected, 'Original identity changed');
  fd = fs.openSync(original, 'r');
  const size = fs.fstatSync(fd).size;
  assert(size > 0 && size <= 64 * 1024 * 1024);
  function read(offset, length, metadata = true) {
    assert(Number.isInteger(offset) && Number.isInteger(length) &&
      offset >= 0 && length > 0 && offset + length <= size);
    if (metadata) assert(record.metadata_bytes + length <= metadataByteLimit);
    const b = Buffer.alloc(length);
    let done = 0;
    while (done < length) {
      const n = fs.readSync(fd, b, done, length - done, offset + done);
      assert(n > 0, 'Unexpected end of input');
      done += n;
    }
    if (metadata) record.metadata_bytes += length;
    return b;
  }
  const dos = read(0, 64);
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60);
  assert(pe <= 65536);
  const coff = read(pe, 24);
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  const n = coff.readUInt16LE(6), optSize = coff.readUInt16LE(20);
  assert(n > 0 && n <= 96 && optSize >= 144 && optSize <= 4096);
  const optional = read(pe + 24, optSize);
  assert.equal(optional.readUInt16LE(0), 0x20b);
  const imageBase = optional.readBigUInt64LE(24);
  assert.equal(imageBase, 0x180000000n);
  const sections = Array.from({length: n}, (_, i) => {
    const b = read(pe + 24 + optSize + i * 40, 40);
    return {rva: b.readUInt32LE(12), size: b.readUInt32LE(16),
      raw: b.readUInt32LE(20), flags: b.readUInt32LE(36)};
  });
  function map(rva, length, code = false) {
    const matches = sections.filter(s => rva >= s.rva && rva + length <= s.rva + s.size);
    assert.equal(matches.length, 1, 'Range must belong to one file-backed section');
    if (code) assert(matches[0].flags & 0x20000000, 'Code section must be executable');
    return matches[0].raw + rva - matches[0].rva;
  }
  assert(optional.readUInt32LE(108) > 3);
  const pdata = optional.readUInt32LE(136), pdataSize = optional.readUInt32LE(140);
  assert(pdataSize > 0 && pdataSize % 12 === 0 && pdataSize <= 2 * 1024 * 1024);
  const pdataOffset = map(pdata, pdataSize);
  // Validate only the five already-frozen entries; do not discover adjacent code or callees.
  for (let i = 0; i < ranges.length; i++) {
    const r = ranges[i], known = frozen.targets[i].boundary;
    assert(known.index < pdataSize / 12);
    const b = read(pdataOffset + known.index * 12, 12);
    assert.equal(b.readUInt32LE(0), r.begin, 'Frozen start differs from PE metadata');
    assert.equal(b.readUInt32LE(4), r.end, 'Frozen end differs from PE metadata');
    assert.equal(b.readUInt32LE(8), known.unwind_rva, 'Frozen unwind record differs');
  }
  for (const r of ranges) {
    const length = r.end - r.begin;
    assert(record.code_bytes + length <= codeByteLimit);
    const start = imageBase + BigInt(r.begin), end = imageBase + BigInt(r.end);
    const label = r.begin.toString(16).padStart(6, '0');
    const fragment = {role: r.role, start_va: '0x' + start.toString(16),
      end_va_exclusive: '0x' + end.toString(16), code_bytes: length, status: 'started'};
    record.fragments.push(fragment);
    const bytes = read(map(r.begin, length, true), length, false);
    record.code_bytes += length;
    fragment.code_sha256 = hash(bytes);
    write('0x' + start.toString(16) + '.bin', bytes);
    const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES',
      `/RANGE:0x${start.toString(16)},0x${(end - 1n).toString(16)}`, copy];
    fragment.command_arguments = args;
    const began = Date.now();
    const result = spawnSync(tool, args, {cwd: root, windowsHide: true, encoding: 'utf8',
      timeout: 10000, maxBuffer: 196608,
      env: {SystemRoot: 'C:\\Windows', WINDIR: 'C:\\Windows', PATH: path.dirname(tool), TEMP: temp, TMP: temp}});
    fragment.tool_elapsed_ms = Date.now() - began;
    fragment.tool_exit = result.status;
    fragment.tool_signal = result.signal;
    if (result.error) fragment.tool_error = String(result.error);
    write(label + '-dumpbin.txt', result.stdout ?? '');
    write(label + '-stderr.txt', result.stderr ?? '');
    assert.equal(result.error, undefined);
    assert.equal(result.status, 0);
    assert.equal(result.stderr, '');
    const rows = [];
    for (const line of result.stdout.split(/\r?\n/)) {
      const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
      if (m) {
        rows.push({address: BigInt('0x' + m[1]), bytes: m[2].replace(/\s/g, '').toLowerCase(),
          instruction: m[3].trim()});
        continue;
      }
      const c = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
      if (c) {
        assert(rows.length, 'Byte continuation without an instruction');
        rows.at(-1).bytes += c[1].replace(/\s/g, '').toLowerCase();
      }
    }
    assert(rows.length > 0);
    let cursor = start;
    for (const row of rows) {
      assert(row.instruction.length > 0);
      assert(row.bytes.length > 0 && row.bytes.length % 2 === 0 && row.bytes.length <= 30);
      assert.equal(row.address, cursor, 'Instruction coverage must be contiguous');
      cursor += BigInt(row.bytes.length / 2);
      assert(cursor <= end, 'Instruction crosses the frozen fragment end');
    }
    assert.equal(cursor, end, 'Disassembly must cover the entire frozen fragment');
    assert.equal(rows.map(row => row.bytes).join(''), bytes.toString('hex'),
      'Every disassembled byte must equal the selected original byte');
    json(label + '-instructions.json', rows.map(row => ({...row, address: '0x' + row.address.toString(16)})));
    fragment.instructions = rows.length;
    fragment.disassembly_sha256 = hash(Buffer.from(result.stdout));
    fragment.byte_coverage_verified = true;
    fragment.status = 'fragment-byte-coverage-verified';
  }
  assert.equal(record.code_bytes, codeByteLimit);
  assert.equal(record.fragments.length, 5);
} catch (e) {
  failures.push(String(e));
} finally {
  if (fd !== undefined) {
    try { fs.closeSync(fd); } catch (e) { failures.push('Original close: ' + String(e)); }
  }
  for (const [field, file, required] of [
    ['original_sha256_after', original, expected],
    ['copy_sha256_after', copy, expected],
    ['tool_sha256_after', tool, toolHash],
    ['source_evidence_sha256_after', callerPath, callerHash]
  ]) {
    try {
      record[field] = hashFile(file);
      if (record[field] !== required) failures.push(field + ': identity mismatch');
    } catch (e) { failures.push(field + ': ' + String(e)); }
  }
  record.original_hash_matches = record.original_sha256_before === expected && record.original_sha256_after === expected;
  record.copy_hash_matches = record.copy_sha256_before === expected && record.copy_sha256_after === expected;
  record.tool_hash_matches = record.tool_sha256_before === toolHash && record.tool_sha256_after === toolHash;
  record.source_evidence_hash_matches = record.source_evidence_sha256_after === callerHash;
  record.status = failures.length ? 'failed' : 'fixed-archive-init-disassembly-verified';
  record.failures = failures;
  record.finished_utc = new Date().toISOString();
  record.controller_resource_usage = process.resourceUsage();
  json('inspection.json', record);
}
if (failures.length) throw new Error(failures.join('\n'));
console.log(JSON.stringify({status: record.status, bytes: record.code_bytes, fragments: record.fragments.length,
  instructions: record.fragments.reduce((n, f) => n + f.instructions, 0),
  original_unchanged: record.original_hash_matches, copy_unchanged: record.copy_hash_matches,
  tool_unchanged: record.tool_hash_matches, controller_peak_rss_kib: record.controller_resource_usage.maxRSS}));
