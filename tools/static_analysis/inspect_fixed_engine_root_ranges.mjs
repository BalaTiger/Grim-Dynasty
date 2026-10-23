// PREPARED ONLY. Independent read-only Engine metadata/code analysis.
// This is not a build/test launcher or a replacement for any refused build.
// Plan: {stage, code_byte_limit, source_evidence:[{path,sha256}],
// prior_inspections:[{path,sha256}], targets:[{rva,maximum_bytes,role,basis,
// leaf_bytes?:6|8|16|32,require_terminal_ret?:boolean}]}.
// leaf_bytes is an explicit fallback only when no exact unwind start exists.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 3, 'One fixed project plan is required');
const script = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(script), '../..');
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedBytes = 4485632;
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const expectedTool = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const pePath = 'docs/object-lifecycle/offline-depot-ctor-boundaries/boundary.json';
const peHash = '646053e4565e3f08c696ae0e9a16ddb5c2e81ab22ffe046c5057fc41aa64bb15';
const limits = Object.freeze({cumulative_code_bytes: 8192, metadata_bytes: 8192,
  targets: 16, target_bytes: 4096, fingerprint_chunk_bytes: 65536,
  plan_bytes: 262144, evidence_bytes: 2097152, prior_reports: 128,
  source_files: 64, report_bytes: 2097152, dumpbin_output_bytes: 524288});
const sha = b => createHash('sha256').update(b).digest('hex');
const hex = n => `0x${n.toString(16)}`;
const safeRange = (at, n, size) => Number.isSafeInteger(at) && at >= 0 &&
  Number.isSafeInteger(n) && n > 0 && at <= size && n <= size - at;
const isSha = s => typeof s === 'string' && /^[a-f0-9]{64}$/.test(s);
function realDirectory(directory) {
  for (let p = directory; ; p = path.dirname(p)) {
    const s = fs.lstatSync(p);
    assert(s.isDirectory() && !s.isSymbolicLink(), 'Directory must not be redirected');
    assert.equal(fs.realpathSync(p).toLowerCase(), path.resolve(p).toLowerCase());
    if (p === path.dirname(p)) break;
  }
}
realDirectory(root);
function localFile(relative, maxBytes = limits.evidence_bytes) {
  assert(typeof relative === 'string' && !path.isAbsolute(relative));
  assert(!relative.split(/[\\/]/).includes('..') && !relative.includes(':'));
  assert(relative.startsWith('docs/') || relative.startsWith('tools/'));
  const full = path.resolve(root, relative);
  assert(full.toLowerCase().startsWith(root.toLowerCase() + path.sep));
  realDirectory(path.dirname(full));
  const s = fs.lstatSync(full);
  assert(s.isFile() && !s.isSymbolicLink() && s.size <= maxBytes);
  assert.equal(fs.realpathSync(full).toLowerCase(), full.toLowerCase());
  return full;
}
function pinned(e) {
  assert(e && isSha(e.sha256));
  const raw = fs.readFileSync(localFile(e.path));
  assert.equal(sha(raw), e.sha256, `Evidence changed: ${e.path}`);
  return raw;
}
const planPath = process.argv[2].replaceAll('\\', '/');
assert(/^docs\/object-lifecycle\/engine-root-plans\/[a-z0-9-]+\.json$/.test(planPath));
const planRaw = fs.readFileSync(localFile(planPath, limits.plan_bytes));
const plan = JSON.parse(planRaw);
assert(/^[a-z0-9-]+$/.test(plan.stage));
assert.equal(planPath, `docs/object-lifecycle/engine-root-plans/${plan.stage}.json`);
assert(Array.isArray(plan.targets) && plan.targets.length > 0 && plan.targets.length <= limits.targets);
assert(Number.isSafeInteger(plan.code_byte_limit) && plan.code_byte_limit > 0 &&
  plan.code_byte_limit <= limits.cumulative_code_bytes);
assert(Array.isArray(plan.source_evidence) && plan.source_evidence.length > 0 &&
  plan.source_evidence.length <= limits.source_files);
const sourcePaths = new Set();
for (const e of plan.source_evidence) {
  assert(!sourcePaths.has(e.path), 'Duplicate source evidence'); sourcePaths.add(e.path);
  const raw = pinned(e);
  // Mixed-module export indexes and instruction arrays are pinned provenance,
  // not automatic permission to analyze any module or follow any reference.
  if (e.path.endsWith('.json')) {
    const source = JSON.parse(raw);
    if (source.original !== undefined) assert.equal(source.original, original, 'Non-Engine source target');
    if (source.target !== undefined) assert.equal(source.target, original, 'Non-Engine source target');
  }
}
const peRaw = pinned({path: pePath, sha256: peHash}), proof = JSON.parse(peRaw), pe = proof.pe;
assert.equal(proof.target, original); assert.equal(proof.source_bytes, expectedBytes);
assert.equal(proof.sha256_before, expected); assert.equal(proof.sha256_after, expected);
assert.equal(proof.source_hash_preserved, true); assert.equal(proof.source_stat_stable, true);
assert.equal(pe.image_base, '0x180000000'); assert.equal(pe.pe_header_file_offset, 288);
assert.equal(pe.exception_directory_rva, '0x424000');
assert.equal(pe.exception_directory_size, 146520); assert.equal(pe.entry_count, 12210);
assert.equal(pe.entry_count * 12, pe.exception_directory_size);
const targetStarts = new Set();
for (const t of plan.targets) {
  assert(Number.isSafeInteger(t.rva) && t.rva > 0 && !targetStarts.has(t.rva));
  targetStarts.add(t.rva);
  assert(Number.isSafeInteger(t.maximum_bytes) && t.maximum_bytes > 0 && t.maximum_bytes <= limits.target_bytes);
  assert(typeof t.role === 'string' && t.role.length > 0 && t.role.length <= 256);
  assert(typeof t.basis === 'string' && t.basis.length > 0 && t.basis.length <= 8192);
  if (t.leaf_bytes !== undefined) {
    assert([6, 8, 16, 32].includes(t.leaf_bytes));
    assert(t.leaf_bytes <= t.maximum_bytes);
  }
  if (t.require_terminal_ret !== undefined) assert.equal(typeof t.require_terminal_ret, 'boolean');
}
assert(Array.isArray(plan.prior_inspections) && plan.prior_inspections.length <= limits.prior_reports);
const priorPaths = new Set(); let priorCodeBytes = 0;
for (const e of plan.prior_inspections) {
  assert(/^docs\/object-lifecycle\/offline-engine-root-static-[a-z0-9-]+\/inspection\.json$/.test(e.path));
  assert(!priorPaths.has(e.path), 'Duplicate prior inspection'); priorPaths.add(e.path);
  const previous = JSON.parse(pinned(e));
  assert.equal(previous.schema, 'grim-dynasty-fixed-engine-root-fragments-v1');
  assert.equal(previous.original, original); assert.equal(previous.expected_sha256, expected);
  assert.equal(previous.expected_file_bytes, expectedBytes);
  assert(['fixed-engine-root-fragments-verified', 'failed'].includes(previous.status));
  assert.equal(previous.original_sha256_before, expected);
  assert.equal(previous.original_sha256_after, expected);
  assert.equal(previous.original_hash_matches, true); assert.equal(previous.original_stat_stable, true);
  assert(Number.isSafeInteger(previous.code_bytes) && previous.code_bytes >= 0 &&
    previous.code_bytes <= limits.cumulative_code_bytes);
  // Failed/partial read reservations count too; no budget reset by a retry.
  priorCodeBytes += previous.code_bytes;
}
const lifecycle = path.join(root, 'docs/object-lifecycle');
realDirectory(lifecycle); realDirectory(path.join(root, '.tooling'));
for (const name of fs.readdirSync(lifecycle).filter(n => /^offline-engine-root-static-[a-z0-9-]+$/.test(n))) {
  assert(priorPaths.has(`docs/object-lifecycle/${name}/inspection.json`), 'Missing prior Engine analysis batch');
}
assert(priorCodeBytes + plan.code_byte_limit <= limits.cumulative_code_bytes, 'Cumulative Engine code budget exceeded');
const outputRelative = `docs/object-lifecycle/offline-engine-root-static-${plan.stage}`;
const tempRelative = `.tooling/offline-engine-root-static-${plan.stage}.temp`;
const output = path.join(root, outputRelative), temp = path.join(root, tempRelative);
// mkdir refuses all existing directories, files and dangling links; no cleanup.
fs.mkdirSync(output); fs.mkdirSync(temp);
const save = (name, value) => {
  assert(/^[a-z0-9.-]+$/.test(name)); fs.writeFileSync(path.join(output, name), value, {flag: 'wx'});
};
const report = {schema: 'grim-dynasty-fixed-engine-root-fragments-v1', status: 'started',
  started_utc: new Date().toISOString(), stage: plan.stage, original,
  expected_sha256: expected, expected_file_bytes: expectedBytes,
  tool, expected_tool_sha256: expectedTool, script_sha256: sha(fs.readFileSync(script)),
  plan: {path: planPath, sha256: sha(planRaw)}, pe_proof: {path: pePath, sha256: peHash},
  source_evidence: plan.source_evidence, prior_inspections: plan.prior_inspections,
  limits, code_byte_limit: plan.code_byte_limit, prior_code_bytes: priorCodeBytes,
  code_bytes: 0, metadata_bytes: 0, metadata_reads: [], code_reads: [], fragments: [],
  fingerprint_passes: [], subprocesses: 0, temporary_directory: tempRelative,
  target_open_mode: 'O_RDONLY', target_loaded_or_executed: false, backend_started: false,
  full_target_copy_created: false,
  restrictions: {independent_static_analysis: true, compilation_or_tests: false,
    refused_build_script_retried: false, renamed_or_equivalent_build: false,
    execution_policy_change: false, installation_or_permission_change: false},
  limitations: [
    'Only the plan targets are analyzed; source hashes and basis are reviewable provenance, not automatic call-edge validation.',
    'No automatic neighbor body, callee, initializer table, other DLL or whole-engine discovery occurs.',
    'Exact unwind entries delimit fragments, not complete logical functions or exception coverage.',
    'Explicit 6/8/16/32-byte prefixes are allowed only in a gap without an exact/containing unwind entry; no larger fallback is selected.',
    'The exception directory is assumed sorted; only encountered entry ordering is checked. Metadata is read by bounded binary search.',
    'The 8KiB root-investigation ledger includes every offline-engine-root-static-* batch and failed read reservation; unrelated earlier Engine investigations are outside this new ledger. Repeated ranges are charged again.',
    'Full-file fingerprints traverse bytes in 64KiB chunks, separately from semantic limits. dumpbin internal file I/O is not measured.',
    'O_RDONLY and same-handle/path stat and hash checks do not provide an atomic snapshot or exclusive FileShare.Read.',
    'The installed dumpbin is the only child process; its purpose is static disassembly, never DLL or game execution.',
  ]};
const failures = [], entries = new Map(), hashBuffer = Buffer.alloc(65536);
let fd, toolFd, initial, initialTool, originalAdmitted = false, toolAdmitted = false;
function sameStat(a, b) {
  return b.isFile() && a.size === b.size && a.dev === b.dev && a.ino === b.ino &&
    a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs;
}
function fingerprint(handle, bytes, label) {
  assert.equal(fs.fstatSync(handle).size, bytes);
  const h = createHash('sha256'); let at = 0;
  while (at < bytes) {
    const n = fs.readSync(handle, hashBuffer, 0, Math.min(hashBuffer.length, bytes - at), at);
    assert(n > 0, 'Fingerprint EOF'); h.update(hashBuffer.subarray(0, n)); at += n;
  }
  const digest = h.digest('hex');
  report.fingerprint_passes.push({label, bytes: at, chunk_bytes: hashBuffer.length, sha256: digest});
  return digest;
}
function mapping(rva, length, executable) {
  assert(Number.isSafeInteger(rva) && Number.isSafeInteger(length) && rva >= 0 && length > 0);
  const matches = pe.sections.filter(s => rva >= s.rva && safeRange(rva - s.rva, length, s.raw_size));
  assert.equal(matches.length, 1, 'Require one file-backed section');
  const section = matches[0]; assert.equal(Boolean(section.characteristics & 0x20000000), executable);
  const offset = section.raw_offset + rva - section.rva;
  assert(safeRange(offset, length, expectedBytes)); return offset;
}
function readBytes(offset, length, record) {
  assert(safeRange(offset, length, expectedBytes)); const bytes = Buffer.alloc(length);
  record.read_bytes = 0; record.raw_bytes = '';
  while (record.read_bytes < length) {
    const n = fs.readSync(fd, bytes, record.read_bytes, length - record.read_bytes, offset + record.read_bytes);
    assert(n > 0, 'Metadata/code EOF'); record.read_bytes += n;
    record.raw_bytes = bytes.subarray(0, record.read_bytes).toString('hex');
  }
  return bytes;
}
function entry(index) {
  assert(Number.isInteger(index) && index >= 0 && index < pe.entry_count);
  if (entries.has(index)) return entries.get(index);
  assert(report.metadata_bytes + 12 <= limits.metadata_bytes, 'Metadata budget exceeded');
  const rva = Number(pe.exception_directory_rva) + index * 12, offset = mapping(rva, 12, false);
  const record = {index, rva: hex(rva), file_offset: offset, requested_bytes: 12};
  report.metadata_reads.push(record); report.metadata_bytes += 12;
  const bytes = readBytes(offset, 12, record);
  const value = {index, begin: bytes.readUInt32LE(0), end: bytes.readUInt32LE(4),
    unwind: bytes.readUInt32LE(8), entry_rva: hex(rva), raw_bytes: record.raw_bytes};
  assert(value.begin < value.end);
  for (const other of entries.values())
    assert(other.index < index ? other.begin <= value.begin : other.begin >= value.begin, 'Observed .pdata ordering conflict');
  entries.set(index, value); return value;
}
try {
  assert.equal(fs.realpathSync(original).toLowerCase(), path.resolve(original).toLowerCase());
  assert.equal(fs.realpathSync(tool).toLowerCase(), path.resolve(tool).toLowerCase());
  fd = fs.openSync(original, fs.constants.O_RDONLY); initial = fs.fstatSync(fd);
  assert(initial.isFile() && initial.size === expectedBytes); originalAdmitted = true;
  report.original_sha256_before = fingerprint(fd, expectedBytes, 'original-before');
  assert.equal(report.original_sha256_before, expected, 'Original hash mismatch');
  toolFd = fs.openSync(tool, fs.constants.O_RDONLY); initialTool = fs.fstatSync(toolFd);
  assert(initialTool.isFile() && initialTool.size > 0); toolAdmitted = true;
  report.tool_sha256_before = fingerprint(toolFd, initialTool.size, 'tool-before');
  assert.equal(report.tool_sha256_before, expectedTool, 'Installed dumpbin hash mismatch');
  for (const target of plan.targets) {
    let lo = 0, hi = pe.entry_count, steps = 0;
    const probes = [];
    while (lo < hi) {
      assert(steps++ < 16, 'Binary-search step limit');
      const mid = lo + Math.floor((hi - lo) / 2), e = entry(mid); probes.push(mid);
      if (e.begin < target.rva) lo = mid + 1; else hi = mid;
    }
    // The lower-bound search has already observed both adjacent brackets.
    // Do not fetch a new neighbor just to select its body.
    assert(lo === pe.entry_count || entries.has(lo));
    assert(lo === 0 || entries.has(lo - 1));
    const next = lo < pe.entry_count ? entries.get(lo) : null, previous = lo > 0 ? entries.get(lo - 1) : null;
    assert(!next || next.begin >= target.rva); assert(!previous || previous.begin < target.rva);
    let end, kind;
    if (next && next.begin === target.rva) { end = next.end; kind = 'exact-unwind-fragment'; }
    else {
      assert(target.leaf_bytes !== undefined, 'No exact unwind start and no explicit prefix');
      assert(!previous || previous.end <= target.rva, 'Target lies inside another unwind fragment');
      end = target.rva + target.leaf_bytes; assert(!next || next.begin >= end, 'Prefix crosses next unwind start');
      kind = 'explicit-fixed-entry-prefix-no-exact-unwind';
    }
    const length = end - target.rva;
    assert(length > 0 && length <= target.maximum_bytes && length <= limits.target_bytes);
    assert(report.code_bytes + length <= plan.code_byte_limit);
    assert(priorCodeBytes + report.code_bytes + length <= limits.cumulative_code_bytes);
    const offset = mapping(target.rva, length, true);
    const startVA = 0x180000000n + BigInt(target.rva), endVA = 0x180000000n + BigInt(end);
    const label = target.rva.toString(16), read = {rva: hex(target.rva), file_offset: offset, requested_bytes: length};
    report.code_reads.push(read); report.code_bytes += length;
    const fragment = {role: target.role, basis: target.basis, start_rva: hex(target.rva),
      end_rva_exclusive: hex(end), start_va: hex(startVA), end_va_exclusive: hex(endVA),
      range_kind: kind, code_bytes: length, binary_search_indices: probes,
      exact_unwind_entry: kind === 'exact-unwind-fragment' ? next : null,
      previous_observed_entry: previous, next_observed_entry: next};
    report.fragments.push(fragment);
    const bytes = readBytes(offset, length, read); fragment.code_sha256 = sha(bytes); save(`${label}.bin`, bytes);
    assert(sameStat(initial, fs.fstatSync(fd)) && sameStat(initial, fs.statSync(original)));
    assert(sameStat(initialTool, fs.fstatSync(toolFd)) && sameStat(initialTool, fs.statSync(tool)));
    const args = ['/NOLOGO', '/NOPDB', '/ERRORREPORT:NONE', '/DISASM:BYTES',
      `/RANGE:${hex(startVA)},${hex(endVA - 1n)}`, original];
    fragment.command_arguments = args; report.subprocesses++;
    const child = spawnSync(tool, args, {cwd: root, windowsHide: true, encoding: 'utf8',
      timeout: 10000, maxBuffer: limits.dumpbin_output_bytes,
      env: {SystemRoot: 'C:\\Windows', WINDIR: 'C:\\Windows', PATH: path.dirname(tool), TEMP: temp, TMP: temp}});
    save(`${label}-dumpbin.txt`, child.stdout || ''); save(`${label}-stderr.txt`, child.stderr || '');
    fragment.tool_exit = child.status; fragment.tool_error = child.error ? String(child.error) : null;
    assert.equal(child.error, undefined); assert.equal(child.status, 0); assert.equal(child.stderr, '');
    const rows = [];
    for (const line of child.stdout.split(/\r?\n/)) {
      const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
      if (m) { rows.push({address: BigInt(`0x${m[1]}`), bytes: m[2].replace(/\s/g, '').toLowerCase(),
        instruction: m[3].trim()}); continue; }
      const continuation = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
      if (continuation) { assert(rows.length > 0); rows.at(-1).bytes += continuation[1].replace(/\s/g, '').toLowerCase(); }
    }
    let cursor = startVA;
    for (const row of rows) {
      assert(row.instruction && row.bytes.length > 0 && row.bytes.length % 2 === 0 && row.bytes.length <= 30);
      assert.equal(row.address, cursor); cursor += BigInt(row.bytes.length / 2); assert(cursor <= endVA);
    }
    assert.equal(cursor, endVA); assert.equal(rows.map(row => row.bytes).join(''), bytes.toString('hex'));
    save(`${label}-instructions.json`, JSON.stringify(rows.map(row => ({...row, address: hex(row.address)})), null, 2) + '\n');
    fragment.instructions = rows.length; fragment.byte_coverage_verified = true;
    if (target.require_terminal_ret) assert.equal(rows.at(-1).instruction.toLowerCase(), 'ret');
  }
} catch (error) { failures.push(String(error)); }
finally {
  if (fd !== undefined) {
    try {
      if (originalAdmitted) {
        report.original_sha256_after = fingerprint(fd, expectedBytes, 'original-after');
        report.original_stat_stable = sameStat(initial, fs.fstatSync(fd)) && sameStat(initial, fs.statSync(original));
        report.original_hash_matches = report.original_sha256_before === expected && report.original_sha256_after === expected;
        assert(report.original_stat_stable && report.original_hash_matches, 'Original final verification failed');
      }
    } catch (error) { failures.push(`original-final: ${error}`); }
    try { fs.closeSync(fd); report.original_closed = true; } catch (error) { failures.push(`original-close: ${error}`); }
  }
  if (toolFd !== undefined) {
    try {
      if (toolAdmitted) {
        report.tool_sha256_after = fingerprint(toolFd, initialTool.size, 'tool-after');
        report.tool_stat_stable = sameStat(initialTool, fs.fstatSync(toolFd)) && sameStat(initialTool, fs.statSync(tool));
        report.tool_hash_matches = report.tool_sha256_before === expectedTool && report.tool_sha256_after === expectedTool;
        assert(report.tool_stat_stable && report.tool_hash_matches, 'Tool final verification failed');
      }
    } catch (error) { failures.push(`tool-final: ${error}`); }
    try { fs.closeSync(toolFd); report.tool_closed = true; } catch (error) { failures.push(`tool-close: ${error}`); }
  }
  report.cumulative_code_bytes = priorCodeBytes + report.code_bytes;
  report.status = failures.length ? 'failed' : 'fixed-engine-root-fragments-verified';
  report.failures = failures; report.finished_utc = new Date().toISOString();
  report.controller_resource_usage = process.resourceUsage();
  const encoded = JSON.stringify(report, null, 2) + '\n';
  assert(Buffer.byteLength(encoded) <= limits.report_bytes, 'Report budget exceeded');
  save('inspection.json', encoded);
}
console.log(JSON.stringify({status: report.status, output: outputRelative,
  code_bytes: report.code_bytes, cumulative_code_bytes: report.cumulative_code_bytes,
  metadata_bytes: report.metadata_bytes, subprocesses: report.subprocesses, failures}));
if (failures.length) process.exitCode = 1;
