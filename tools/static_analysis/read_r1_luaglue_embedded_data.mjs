// PREPARED ONLY. Import performs no I/O. Root separately reviews and executes
// inspectLuaGlueEmbeddedData(scriptSHA, planSHA). No subprocess or DLL loading.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectLuaGlueEmbeddedData(reviewedScriptSha256, reviewedPlanSha256) {
  const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
  const hash = bytes => createHash('sha256').update(bytes).digest('hex');
  const hex = value => '0x' + value.toString(16);
  const source = {path: 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', bytes: 4485632,
    sha256: '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'};
  const planRel = 'docs/reverse-client/r1-luaglue-record-plan.json';
  const fixedPlanSha = 'b28ad52c400c450a99c1bcbf631a11e65ebd3aec63caae12e08256c9a2d1f54c';
  const peProof = {path: 'docs/object-lifecycle/offline-depot-ctor-boundaries/boundary.json',
    sha256: '646053e4565e3f08c696ae0e9a16ddb5c2e81ab22ffe046c5057fc41aa64bb15'};
  const binderProof = {path: 'docs/reverse-client/r1-engine-lua-initialize-dependency-run1/05-LuaInitializeDependency-0x180258e50-procedure_assembly.json',
    sha256: 'b61d98ddad9968e13c7f6c9be8551a98eb4e03e8faccc7913ee461394896a8bf'};
  const outRel = 'docs/reverse-client/r1-luaglue-embedded-data-run1';
  const limits = {records: 11, exact_ranges: 33, exact_bytes: 8345, bounded_strings: 2,
    bytes_per_bounded_string_including_nul: 32, data_bytes: 8409, data_operations: 97,
    hash_chunk_bytes: 65536, hash_passes: 2, code_bytes: 16, code_operations: 1, new_header_bytes: 0,
    report_bytes: 262144, proof_bytes_each: 2097152};
  assert.equal(process.platform, 'win32');
  assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self), 'read_r1_luaglue_embedded_data.mjs');
  assert.match(reviewedScriptSha256, /^[a-f0-9]{64}$/);
  assert.equal(reviewedPlanSha256, fixedPlanSha);

  // As in the existing readers, the caller must additionally perform the
  // Windows Attributes reparse preflight; Node does not expose every tag.
  function normal(p) {
    for (let q = path.resolve(p); ; q = path.dirname(q)) {
      assert(!fs.lstatSync(q).isSymbolicLink(), 'Redirected path refused: ' + q);
      assert.equal(fs.realpathSync.native(q).toLowerCase(), q.toLowerCase());
      if (path.dirname(q) === q) break;
    }
  }
  function regular(p) {
    normal(p); const s = fs.statSync(p);
    assert(s.isFile()); assert.equal(s.nlink, 1); assert(s.ino !== 0); return s;
  }
  function sameStat(a, b) {
    for (const k of ['size', 'dev', 'ino', 'nlink', 'mtimeMs', 'ctimeMs']) assert.equal(a[k], b[k], k);
  }
  function local(rel) {
    const p = path.resolve(root, rel); assert(p.startsWith(root + path.sep)); return p;
  }
  function pinned(rel, expected, maximum) {
    const p = local(rel), before = regular(p); assert(before.size > 0 && before.size <= maximum);
    const raw = fs.readFileSync(p); sameStat(regular(p), before); assert.equal(hash(raw), expected, rel);
    return JSON.parse(raw);
  }
  const selfStat = regular(self); assert(selfStat.size > 0 && selfStat.size < 32768);
  assert.equal(hash(fs.readFileSync(self)), reviewedScriptSha256); sameStat(regular(self), selfStat);
  const plan = pinned(planRel, reviewedPlanSha256, 65536);
  assert.equal(plan.schema, 'gd-luaglue-record-plan-v1');
  assert.equal(plan.target.module, 'Engine.dll'); assert.equal(plan.target.sha256, source.sha256);
  assert.equal(plan.records.length, limits.records); assert.equal(plan.evidence.length, 3);
  assert.deepEqual(plan.evidence.map(e => e.id), ['pseudo', 'assembly', 'caller']);
  for (const p of plan.evidence) pinned(p.path, p.sha256, limits.proof_bytes_each);
  const binder = pinned(binderProof.path, binderProof.sha256, limits.proof_bytes_each).result.structuredContent.raw_result.value;
  assert(binder.includes('0x1802599df: LEA RAX, [0x180258da0]'));
  assert(binder.includes('0x1802599e6: MOV qword ptr [0x180421880], RAX'));
  const cached = pinned(peProof.path, peProof.sha256, limits.proof_bytes_each);
  assert.equal(cached.target, source.path); assert.equal(cached.source_bytes, source.bytes);
  assert.equal(cached.sha256_before, source.sha256); assert.equal(cached.sha256_after, source.sha256);
  assert(cached.source_stat_stable && cached.source_hash_preserved);
  assert.equal(cached.pe.image_base, '0x180000000');

  function rva(text) { assert.match(text, /^[0-9A-F]{6}$/); return Number.parseInt(text, 16); }
  function mapping(address, bytes, code = false) {
    assert(Number.isSafeInteger(address) && Number.isSafeInteger(bytes) && address > 0 && bytes > 0);
    const hits = cached.pe.sections.filter(s => address >= s.rva && address + bytes <= s.rva + s.raw_size);
    assert.equal(hits.length, 1); const s = hits[0]; assert.equal(s.name, code ? '.text' : '.rdata');
    assert((s.characteristics & 0x40000000) !== 0); assert.equal((s.characteristics & 0x20000000) !== 0, code);
    const offset = s.raw_offset + address - s.rva;
    assert(Number.isSafeInteger(offset) && offset >= 0 && offset + bytes <= source.bytes);
    return offset;
  }
  const declared = [];
  for (const [index, record] of plan.records.entries()) {
    assert.equal(record.index, index); assert.match(record.name, /^[a-z]+$/);
    for (const kind of ['source', 'name', 'chunk']) {
      const length = record[kind + '_length_bytes'];
      const bytes = record[kind === 'source' ? 'copy_bytes_including_nul' : kind + '_copy_bytes_including_nul'];
      assert(Number.isSafeInteger(length) && length > 0 && length <= 4096); assert.equal(bytes, length + 1);
      const address = rva(record[kind + '_rva']);
      declared.push({id: `${String(index).padStart(2, '0')}-${record.name}-${kind}`, record: index, kind,
        rva: hex(address), address, file_offset: mapping(address, bytes), maximum_bytes: bytes,
        declared_content_bytes: length, mode: 'exact-copy-span',
        expected_ascii: kind === 'source' ? null : kind === 'name' ? record.name : record.chunk_name});
    }
  }
  assert.equal(declared.length, limits.exact_ranges);
  assert.equal(declared.reduce((n, d) => n + d.maximum_bytes, 0), limits.exact_bytes);
  assert.equal(declared.filter(d => d.kind === 'source').reduce((n, d) => n + d.maximum_bytes, 0), 8095);
  for (const [id, address] of [['version', 0x3456f4], ['registry-key', 0x3479b8]])
    declared.push({id, record: null, kind: id, rva: hex(address), address,
      file_offset: mapping(address, 32), maximum_bytes: 32, mode: 'bytewise-until-first-nul'});
  assert.equal(new Set(declared.map(d => d.id)).size, 35);
  declared.push({id: 'getglobal-258da0-raw', record: null, kind: 'supplemental-code',
    rva: '0x258da0', address: 0x258da0, file_offset: mapping(0x258da0, 16, true),
    maximum_bytes: 16, mode: 'exact-raw-no-decode', basis: binderProof});
  const out = local(outRel); normal(path.dirname(out));
  try { fs.lstatSync(out); assert.fail('Existing output is preserved: ' + out); }
  catch (e) { if (e.code !== 'ENOENT') throw e; }
  fs.mkdirSync(out); normal(out);
  const report = {schema: 'gd-luaglue-embedded-data-v1', status: 'started', started_utc: new Date().toISOString(),
    script_sha256: reviewedScriptSha256, plan: {path: planRel, sha256: reviewedPlanSha256}, pe_proof: peProof, binder_proof: binderProof,
    source, limits, declared_ranges: declared, evidence: plan.evidence, data_bytes: 0, data_operations: 0,
    code_bytes: 0, code_operations: 0, fingerprint_ranges: [], reads: [], artifacts: [], source_closed: false, source_stable: false,
    semantic_data_complete: false, original_loaded_or_executed: false, subprocesses: 0,
    source_text_encoding: 'UNINTERPRETED_RAW_BYTES', source_transformation_255F70: 'UNKNOWN',
    runtime_compatibility_verified: false, failures: [], limitations: [
      'Only33 exact source/name/chunk copy spans and2 fixed32B maximum C strings. Bounded strings stop at their first NUL; a missing terminator fails without extension.',
      'One separately authorized16B raw code supplement at258DA0 follows the cached conditional binder target. It is not decoded here and proves neither the loaded421880 value nor a full function boundary.258DB0 is not read.',
      'Raw data artifacts include the terminator. Declared source copy length does not establish255F70 output or luaL_loadbuffer input length.',
      'No source decoding, normalization, repair, script execution or functional/ABI compatibility claim. Cached names are compared as ASCII bytes only.',
      'Before/after whole-file SHA256 passes use the same read-only descriptor and64KiB buffers; fingerprints are outside semantic data totals and are not an atomic snapshot.',
      'PE mapping is reused from pinned local evidence for the same file hash. No new header, other code, neighboring string, directory or export-table reads.',
      'Caller must complete the established Windows Attributes reparse preflight; Node cannot identify every Windows reparse tag.'
    ]};
  let fd, initial;
  function digest() {
    assert(report.fingerprint_ranges.length < limits.hash_passes);
    const pass = {file_offset: 0, bytes: source.bytes, purpose: 'fingerprint-only', bytes_read: 0};
    report.fingerprint_ranges.push(pass);
    const h = createHash('sha256'), buffer = Buffer.alloc(limits.hash_chunk_bytes);
    for (let at = 0; at < source.bytes;) {
      const n = fs.readSync(fd, buffer, 0, Math.min(buffer.length, source.bytes - at), at);
      assert(n > 0); h.update(buffer.subarray(0, n)); at += n; pass.bytes_read += n;
    }
    return h.digest('hex');
  }
  function read(d, relative, bytes) {
    assert(relative >= 0 && relative + bytes <= d.maximum_bytes);
    const code = d.kind === 'supplemental-code', countKey = code ? 'code_bytes' : 'data_bytes';
    const operationKey = code ? 'code_operations' : 'data_operations';
    assert(report[countKey] + bytes <= limits[countKey]);
    assert(report[operationKey] + 1 <= limits[operationKey]);
    const buffer = Buffer.alloc(bytes), row = {id: d.id, rva: hex(d.address + relative),
      file_offset: d.file_offset + relative, requested_bytes: bytes, bytes_read: 0};
    report.reads.push(row); ++report[operationKey];
    while (row.bytes_read < bytes) {
      const n = fs.readSync(fd, buffer, row.bytes_read, bytes - row.bytes_read, row.file_offset + row.bytes_read);
      assert(n > 0); row.bytes_read += n; report[countKey] += n;
    }
    return buffer;
  }
  try {
    const before = regular(source.path); assert.equal(before.size, source.bytes);
    fd = fs.openSync(source.path, fs.constants.O_RDONLY); initial = fs.fstatSync(fd); sameStat(initial, before);
    report.sha256_before = digest(); assert.equal(report.sha256_before, source.sha256);
    for (const d of declared) {
      let bytes;
      if (d.mode === 'exact-copy-span' || d.mode === 'exact-raw-no-decode') bytes = read(d, 0, d.maximum_bytes);
      else {
        const pieces = [];
        for (let at = 0; at < d.maximum_bytes; ++at) { const b = read(d, at, 1); pieces.push(b); if (b[0] === 0) break; }
        bytes = Buffer.concat(pieces);
      }
      const code = d.kind === 'supplemental-code', firstNul = code ? null : bytes.indexOf(0), file = d.id + '.bin';
      // Save the exact observed bytes even if terminator/name validation fails.
      fs.writeFileSync(path.join(out, file), bytes, {flag: 'wx'});
      report.artifacts.push({id: d.id, file, rva: d.rva, file_offset: d.file_offset,
        bytes: bytes.length, sha256: hash(bytes), first_nul_offset: firstNul,
        content_bytes_without_terminator: code || firstNul < 0 ? null : firstNul,
        encoding: d.kind === 'source' ? 'UNINTERPRETED' : 'RAW_BYTES', validated: false});
      if (code) { report.artifacts[report.artifacts.length - 1].validated = true; continue; }
      assert.equal(firstNul, bytes.length - 1, 'Missing or premature terminator: ' + d.id);
      if (d.mode === 'exact-copy-span') assert.equal(firstNul, d.declared_content_bytes);
      if (d.expected_ascii !== null && d.expected_ascii !== undefined) {
        assert.match(d.expected_ascii, /^[\x20-\x7e]+$/);
        assert.equal(hash(bytes), hash(Buffer.from(d.expected_ascii + '\0', 'ascii')), 'Cached ASCII spelling differs: ' + d.id);
      }
      report.artifacts[report.artifacts.length - 1].validated = true;
    }
    report.semantic_data_complete = true;
  } catch (e) { report.failures.push({phase: 'read-and-validate', error: e.message}); }
  finally {
    if (fd !== undefined) {
      try {
        report.sha256_after = digest(); assert.equal(report.sha256_after, source.sha256);
        sameStat(fs.fstatSync(fd), initial); sameStat(regular(source.path), initial); report.source_stable = true;
      } catch (e) { report.failures.push({phase: 'source-postcheck', error: e.message}); }
      finally {
        try { fs.closeSync(fd); report.source_closed = true; }
        catch (e) { report.failures.push({phase: 'source-close', error: e.message}); }
      }
    }
  }
  report.status = report.semantic_data_complete && report.source_stable && report.source_closed && report.failures.length === 0 ?
    'passed-fixed-raw-data-extraction' : 'failed-preserved-partial-evidence';
  report.finished_utc = new Date().toISOString();
  const encoded = Buffer.from(JSON.stringify(report, null, 2) + '\n'); assert(encoded.length <= limits.report_bytes);
  fs.writeFileSync(path.join(out, 'inspection.json'), encoded, {flag: 'wx'});
  if (report.status !== 'passed-fixed-raw-data-extraction') throw new Error('Bounded extraction failed; inspect ' + outRel + '/inspection.json');
  return {status: report.status, report: outRel + '/inspection.json', artifacts: report.artifacts.length,
    data_bytes: report.data_bytes, supplemental_code_bytes: report.code_bytes,
    original_hash_preserved: true, compatibility_verified: false};
}
