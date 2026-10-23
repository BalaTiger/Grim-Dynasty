// Preparation only: importing this module performs no file I/O.
// Root owns explicit review/execution. Never load or execute the source DLL.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

export function inspectTerrainSurfaceStrings(reviewedScriptSha256, reviewedPlanSha256) {
  const self = fileURLToPath(import.meta.url);
  const root = path.resolve(path.dirname(self), '../..');
  assert.equal(process.platform, 'win32');
  assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
  assert.equal(path.basename(self), 'read_r1_terrain_surface_strings.mjs');
  const hash = bytes => createHash('sha256').update(bytes).digest('hex');
  function normal(p) {
    for (let q = path.resolve(p); ; q = path.dirname(q)) {
      assert(!fs.lstatSync(q).isSymbolicLink(), 'Redirected path: ' + q);
      assert.equal(fs.realpathSync.native(q).toLowerCase(), q.toLowerCase());
      if (path.dirname(q) === q) break;
    }
  }
  function regular(p) {
    normal(p);
    const s = fs.statSync(p);
    assert(s.isFile()); assert.equal(s.nlink, 1); assert(s.ino !== 0);
    return s;
  }
  function sameStat(a, b) {
    for (const k of ['size', 'dev', 'ino', 'nlink', 'mtimeMs', 'ctimeMs'])
      assert.equal(a[k], b[k], k);
  }
  function local(rel) {
    const p = path.resolve(root, rel);
    assert(p.startsWith(root + path.sep));
    return p;
  }
  function pinnedJson(rel, expected, maxBytes) {
    const p = local(rel), before = regular(p);
    assert(before.size > 0 && before.size <= maxBytes);
    const bytes = fs.readFileSync(p);
    sameStat(regular(p), before);
    assert.equal(hash(bytes), expected, 'Project evidence hash: ' + rel);
    return JSON.parse(bytes);
  }
  assert.match(reviewedScriptSha256, /^[a-f0-9]{64}$/);
  assert.match(reviewedPlanSha256, /^[a-f0-9]{64}$/);
  const selfStat = regular(self);
  assert(selfStat.size > 0 && selfStat.size <= 16384);
  assert.equal(hash(fs.readFileSync(self)), reviewedScriptSha256);
  sameStat(regular(self), selfStat);
  const planPath = 'tools/static_analysis/read_r1_terrain_surface_strings.plan.json';
  assert.equal(reviewedPlanSha256, '0711e4f382930322564293669b861edb3780f0616f22a95498e442a38a5623a4');
  const plan = pinnedJson(planPath, reviewedPlanSha256, 12288);
  assert.equal(plan.schema, 'grim-dynasty.fixed-terrain-surface-strings.v1');
  assert.equal(plan.output, 'docs/reverse-client/r1-terrain-surface-strings/inspection.json');
  assert.deepEqual(plan.source, {
    path: 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', bytes: 4485632,
    sha256: '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'
  });
  assert.deepEqual(plan.limits, {
    data_bytes: 23, read_operations: 6, single_read_bytes: 4,
    hash_chunk_bytes: 65536, new_header_bytes: 0, code_bytes: 0
  });
  assert.deepEqual(plan.pins, [
    {key: 'pe', path: 'docs/object-lifecycle/offline-class-registration-exports/discovery.json', sha256: '0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'},
    {key: 'assembly', path: 'docs/reverse-client/r1-terrain-type-material/05-TerrainTypeMaterial-0x1801e1940-procedure_assembly.json', sha256: '6c4e417d3b9c908a716eb680ad7ca8f3cf0b1c3e53f847ba891950ccee8730e5'},
    {key: 'pseudo', path: 'docs/reverse-client/r1-terrain-type-material/04-TerrainTypeMaterial-0x1801e1940-procedure_pseudo_code.json', sha256: '26f717082e3030352ffe34277406c1132c45662d0867b707c27df55375579038'}
  ]);
  assert.deepEqual(plan.fixed_reads, [
    {rva: '0x3244cc', bytes: 4, surface_fc: 2, reference: '0x1801e1dc6: LEA RDX, [0x1803244cc]', write: '0x1801e1ddb: MOV dword ptr [RDI + 0xfc], 0x2'},
    {rva: '0x3244d4', bytes: 4, surface_fc: 3, reference: '0x1801e1e03: LEA RDX, [0x1803244d4]', write: '0x1801e1e1e: MOV dword ptr [RDI + 0xfc], R8D'},
    {rva: '0x3244dc', bytes: 4, surface_fc: 4, reference: '0x1801e1e43: LEA RDX, [0x1803244dc]', write: '0x1801e1e58: MOV dword ptr [RDI + 0xfc], R13D'},
    {rva: '0x32446c', bytes: 4, surface_fc: 6, reference: '0x1801e1eb9: LEA RDX, [0x18032446c]', write: '0x1801e1ece: MOV dword ptr [RDI + 0xfc], 0x6'},
    {rva: '0x32447c', bytes: 4, surface_fc: 9, reference: '0x1801e1f38: LEA RDX, [0x18032447c]', write: '0x1801e1f4d: MOV dword ptr [RDI + 0xfc], 0x9'},
    {rva: '0x3225dc', bytes: 3, surface_fc: 10, reference: '0x1801e1f7c: LEA RDX, [0x1803225dc]', write: '0x1801e1f92: MOV dword ptr [RDI + 0xfc], 0xa'}
  ]);
  assert.deepEqual(plan.known_from_pseudocode, {Unwalkable: 0, Grass: 1, Stone: 5, Water: 7});
  const def = plan.source, evidence = new Map();
  for (const pin of plan.pins)
    evidence.set(pin.key, pinnedJson(pin.path, pin.sha256, 1048576));
  const matches = evidence.get('pe').files.filter(f => f.target === def.path);
  assert.equal(matches.length, 1);
  const cached = matches[0], pe = cached.pe;
  assert.equal(cached.status, 'metadata-discovery-completed');
  assert.equal(cached.expected_file_bytes, def.bytes);
  assert.equal(cached.expected_sha256, def.sha256);
  assert.equal(cached.sha256_before, def.sha256); assert.equal(cached.sha256_after, def.sha256);
  assert.equal(cached.source_stat_stable, true); assert.equal(cached.source_hash_preserved, true);
  assert.equal(cached.preferred_image_base, '0x180000000');
  assert(Number.isSafeInteger(pe.size_of_image) && pe.size_of_image > 0);
  assert(Array.isArray(pe.sections) && pe.sections.length > 0 && pe.sections.length <= 16);
  for (const s of pe.sections) {
    for (const k of ['rva', 'raw_size', 'raw_offset', 'virtual_size', 'characteristics'])
      assert(Number.isSafeInteger(s[k]) && s[k] >= 0);
    assert(s.raw_offset + s.raw_size <= def.bytes);
    assert(s.rva + Math.max(s.raw_size, s.virtual_size) <= pe.size_of_image);
  }
  const bodies = new Map();
  for (const [key, operation] of [['assembly', 'procedure_assembly'], ['pseudo', 'procedure_pseudo_code']]) {
    const e = evidence.get(key).result.structuredContent;
    assert.equal(e.subject.name, 'Engine.dll'); assert.equal(e.subject.digest.sha256, def.sha256);
    assert.equal(path.resolve(e.subject.local_path).toLowerCase(), path.resolve(def.path).toLowerCase());
    assert.equal(e.subject.format, 'pe'); assert.equal(e.subject.architecture, 'x86_64');
    assert.equal(e.provider.id, 'ghidra'); assert.equal(e.operation, operation);
    assert.equal(e.parameters.procedure, '0x1801e1940'); assert.equal(typeof e.normalized_result, 'string');
    bodies.set(key, e.normalized_result);
  }
  const lines = bodies.get('assembly').split(/\r?\n/), pseudo = bodies.get('pseudo');
  for (const t of plan.fixed_reads) {
    assert(lines.includes(t.reference)); assert(lines.includes(t.write));
    assert(pseudo.includes('&DAT_180' + t.rva.slice(2)));
  }
  for (const line of [
    '0x1801e1d2d: MOV R13D, 0x4', '0x1801e1e18: MOV R8D, 0x3',
    '0x1801e1dd6: CMP R15, R13', '0x1801e1e13: CMP R15, R13',
    '0x1801e1e53: CMP R15, R13', '0x1801e1ec9: CMP R15, R13',
    '0x1801e1f48: CMP R15, R13', '0x1801e1f8c: CMP R15, 0x3'
  ]) assert(lines.includes(line));
  for (const name of Object.keys(plan.known_from_pseudocode)) assert(pseudo.includes('"' + name + '"'));
  assert(pseudo.includes('memcmp(')); assert(pseudo.includes('(this + 0xfc) = 100;'));
  function mapTarget(t) {
    const rva = Number(t.rva), n = t.bytes;
    assert(Number.isSafeInteger(rva) && rva >= 0 && Number.isSafeInteger(n) && n > 0 && n <= 4);
    assert(rva + n <= pe.size_of_image);
    const hits = pe.sections.filter(s => rva >= s.rva && rva + n <= s.rva + s.raw_size);
    assert.equal(hits.length, 1);
    const section = hits[0], offset = section.raw_offset + rva - section.rva;
    assert.equal(section.name, '.rdata');
    assert(section.characteristics & 0x40000000); assert(!(section.characteristics & 0x20000000));
    assert(Number.isSafeInteger(offset) && offset >= pe.size_of_headers && offset + n <= def.bytes);
    return {target: t, section: section.name, offset};
  }
  const mapped = plan.fixed_reads.map(mapTarget);
  assert.equal(mapped.reduce((sum, m) => sum + m.target.bytes, 0), 23);
  assert.equal(new Set(mapped.map(m => m.offset)).size, 6);
  const out = local('docs/reverse-client/r1-terrain-surface-strings');
  normal(path.dirname(out));
  try { fs.lstatSync(out); assert.fail('Existing output is preserved'); }
  catch (e) { if (e.code !== 'ENOENT') throw e; }
  fs.mkdirSync(out); normal(out);
  const report = {
    schema: plan.schema, started_utc: new Date().toISOString(),
    script_sha256: reviewedScriptSha256, plan_sha256: reviewedPlanSha256, source_pins: plan.pins,
    source: def, data_bytes: 0, read_operations: 0, metadata_bytes: 0, new_header_bytes: 0, code_bytes: 0,
    original_loaded_or_executed: false, subprocesses: 0, proofs_verified: true,
    known_from_pseudocode: plan.known_from_pseudocode, comparison: plan.comparison,
    values: [], files: [], failures: [], disk_observations_complete: false,
    limitations: [
      'Only the six exact comparison operands are read as data; no terminator, default308119, header, code or adjacent byte is interpreted.',
      'The numeric +FC writes come from pinned Load evidence. Raw operand bytes do not establish any native object, address, allocation, lifetime or successful Load.',
      'Unmatched or missing surface strings are Unsupported for the own adapter; the observed native100 write is not a supported fallback.',
      'Whole-file SHA256 streams are fingerprint-only reads, separate from the23B data budget. Same-handle before/after checks are not an atomic snapshot.',
      'Node path checks do not identify every Windows reparse tag; root must perform the established Windows Attributes preflight before execution.'
    ]
  };
  const f = {source: def.path, expected_size: def.bytes, expected_sha256: def.sha256,
    hash_bytes: 0, hash_passes: 0, stable: false, closed: false, reads: []};
  report.files.push(f);
  let fd, initial;
  function digest() {
    assert(f.hash_passes < 2); f.hash_passes++;
    const h = createHash('sha256'), b = Buffer.alloc(65536);
    for (let at = 0; at < def.bytes;) {
      const requested = Math.min(b.length, def.bytes - at);
      assert(f.hash_bytes + requested <= 2 * def.bytes);
      const n = fs.readSync(fd, b, 0, requested, at);
      assert(n > 0 && n <= requested); h.update(b.subarray(0, n));
      at += n; f.hash_bytes += n;
    }
    return h.digest('hex');
  }
  try {
    const before = regular(def.path); assert.equal(before.size, def.bytes);
    fd = fs.openSync(def.path, fs.constants.O_RDONLY);
    initial = fs.fstatSync(fd); assert(initial.isFile()); sameStat(initial, before);
    f.sha256_before = digest(); assert.equal(f.sha256_before, def.sha256);
    for (const m of mapped) {
      const t = m.target;
      assert(report.read_operations + 1 <= 6); assert(report.data_bytes + t.bytes <= 23);
      const b = Buffer.alloc(t.bytes);
      report.read_operations++; report.data_bytes += t.bytes;
      const n = fs.readSync(fd, b, 0, t.bytes, m.offset);
      f.reads.push({rva: t.rva, file_offset: m.offset, requested_bytes: t.bytes, read_bytes: n});
      assert.equal(n, t.bytes, 'No extra reads after a partial fixed operand read');
      report.values.push({...t, file_offset: m.offset, section: m.section, raw_hex: b.toString('hex'),
        ascii: b.every(value => value >= 0x20 && value <= 0x7e) ? b.toString('ascii') : null,
        loaded_value_observed: false});
    }
    assert.equal(report.data_bytes, 23); assert.equal(report.read_operations, 6);
    assert.equal(report.values.length, 6); report.disk_observations_complete = true;
  } catch (e) { report.failures.push(String(e)); }
  finally {
    if (fd !== undefined) {
      try {
        f.sha256_after = digest(); assert.equal(f.sha256_after, def.sha256);
        sameStat(fs.fstatSync(fd), initial); sameStat(regular(def.path), initial);
        assert.equal(f.hash_bytes, 2 * def.bytes); assert.equal(f.hash_passes, 2); f.stable = true;
      } catch (e) { report.failures.push(String(e)); }
      finally {
        try { fs.closeSync(fd); f.closed = true; }
        catch (e) { report.failures.push(String(e)); }
      }
    }
  }
  report.finished_utc = new Date().toISOString();
  report.status = report.failures.length === 0 && report.disk_observations_complete && f.stable && f.closed ? 'passed' : 'failed';
  if (report.status === 'failed' && !report.failures.length) report.failures.push('Incomplete observation');
  const serialized = JSON.stringify(report, null, 2) + '\n';
  assert(Buffer.byteLength(serialized) < 65536);
  normal(out); fs.writeFileSync(path.join(out, 'inspection.json'), serialized, {flag: 'wx'});
  if (report.status !== 'passed') throw new Error('Fixed terrain operand read failed; evidence preserved at ' + out + ': ' + report.failures.join('; '));
  return {status: report.status, output: plan.output, data_bytes: report.data_bytes,
    read_operations: report.read_operations, values: report.values,
    original_hashes: report.files.map(({source, sha256_before, sha256_after, stable, closed}) =>
      ({source, sha256_before, sha256_after, stable, closed}))};
}
