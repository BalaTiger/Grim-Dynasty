// Three saved .pdata entries plus exactly one new 12-byte entry. Never read code.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const expectedHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const expectedBytes = 11385856, imageBase = 0x180000000n;
const evidence = [
  {path: 'docs/object-lifecycle/offline-questitem-boundaries/boundary.json', sha256: 'fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05'},
  {path: 'docs/object-lifecycle/offline-game-class-init-table/metadata.json', sha256: 'e347eb6fa9647ca63cfa48fe0deb60320f0d8e3f6921e18269a6ee560a658c8b'},
  {path: 'docs/object-lifecycle/offline-class-static-table-batch-4/inspection.json', sha256: 'b792b1858bdeba01dbf1b76e2fd9ead2d8d2576b407dcc5d28203a03829b08fe'},
  {path: 'docs/object-lifecycle/offline-class-static-table-batch-7/inspection.json', sha256: '0074c19190c7ef08f13c6897153964ecb6b159d1ab1bb10b1355113cb5987871'},
];
const requests = [
  {rva: 0x5f29, initializer: 0x5ef0, slot: 115, entry: 340, proof: 2, saved: '295f00009f5f0000105f7f00', relationship: 'fallthrough after INC EAX at 0x180005f27'},
  {rva: 0x5f9f, initializer: 0x5ef0, slot: 115, entry: 341, proof: 2, saved: '9f5f0000b05f0000245f7f00', relationship: 'JE destination from 0x180005f22 (74 7b)'},
  {rva: 0x9e3b, initializer: 0x9e10, slot: 205, entry: 610, proof: 3, saved: null, relationship: 'fallthrough after INC EAX at 0x180009e39'},
  {rva: 0x9eaf, initializer: 0x9e10, slot: 205, entry: 611, proof: 3, saved: 'af9e0000c09e0000ec7c7f00', relationship: 'JE destination from 0x180009e34 (74 79)'},
];
const directory = path.join(root, 'docs/object-lifecycle/offline-class-init-continuation-boundaries');
const output = path.join(directory, 'metadata.json'), hex = n => `0x${n.toString(16)}`;
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
function checkDirectory(dir) {
  for (;;) { const s = fs.lstatSync(dir); assert.ok(s.isDirectory() && !s.isSymbolicLink());
    if (path.dirname(dir) === dir) break; dir = path.dirname(dir); }
}
checkDirectory(path.dirname(directory)); if (fs.existsSync(directory)) checkDirectory(directory);
assert.ok(!fs.existsSync(output), 'Refuse to overwrite evidence');
const proofs = evidence.map(e => { const p = path.join(root, e.path), s = fs.lstatSync(p);
  assert.ok(s.isFile() && !s.isSymbolicLink() && s.size <= 4 * 1024 * 1024);
  const bytes = fs.readFileSync(p); assert.equal(digest(bytes), e.sha256); return JSON.parse(bytes); });
const [boundary, table] = proofs, pe = boundary.pe;
assert.equal(boundary.source_bytes, expectedBytes); assert.equal(pe.image_base, hex(imageBase));
for (const proof of proofs) {
  assert.equal(proof.target ?? proof.original, target); assert.equal(proof.expected_sha256, expectedHash);
  assert.equal(proof.source_hash_preserved ?? proof.original_hash_matches, true);
}
assert.equal(table.status, 'fixed-game-initializer-pointers-and-exact-boundaries-assessed');
for (const [i, start, end, slot] of [[2, 0x5ef0, 0x5f29, 115], [3, 0x9e10, 0x9e3b, 205]]) {
  const proof = proofs[i]; assert.equal(proof.status, 'fixed-game-init-fragments-verified');
  assert.deepEqual(proof.failures, []); assert.equal(proof.original_stat_stable, true);
  const fragment = proof.fragments.find(f => f.start_rva === hex(start));
  assert.ok(fragment); assert.equal(fragment.end_rva_exclusive, hex(end));
  assert.equal(fragment.byte_coverage_verified, true); assert.equal(fragment.tool_exit, 0);
  assert.equal(table.slots[slot].index, slot); assert.equal(table.slots[slot].target_rva, hex(start));
}
function mapping(rva, bytes) {
  const found = pe.sections.filter(s => rva >= s.rva && bytes > 0 && rva - s.rva <= s.raw_size - bytes);
  assert.equal(found.length, 1); const section = found[0], offset = section.raw_offset + rva - section.rva;
  assert.ok(Number.isSafeInteger(offset) && offset >= 0 && offset <= expectedBytes - bytes); return {section, offset};
}
const pdataRva = Number(BigInt(pe.exception_directory_rva)), pdataSize = pe.exception_directory_size;
assert.ok(Number.isSafeInteger(pdataSize) && pdataSize > 0 && pdataSize % 12 === 0);
const pdata = mapping(pdataRva, pdataSize); assert.equal(pdata.section.name, '.pdata');
assert.equal(pdata.section.characteristics & 0x20000000, 0);
const newRva = pdataRva + 610 * 12, newOffset = pdata.offset + 610 * 12;
assert.equal(newRva, 0xa6bc98); assert.equal(newOffset, 10903704);
const cached = new Map();
for (const r of requests.filter(r => r.saved !== null)) {
  const matches = proofs[r.proof].metadata_reads.filter(e => e.index === r.entry);
  assert.equal(matches.length, 1); const e = matches[0];
  assert.equal(e.rva, hex(pdataRva + r.entry * 12)); assert.equal(e.file_offset, pdata.offset + r.entry * 12);
  assert.equal(e.bytes, r.saved); cached.set(r.entry, Buffer.from(e.bytes, 'hex'));
}
const result = {schema: 'grim-dynasty-fixed-class-init-continuation-metadata-v2', status: 'started',
  started_utc: new Date().toISOString(), target, expected_sha256: expectedHash, expected_file_bytes: expectedBytes,
  script_sha256: digest(fs.readFileSync(fileURLToPath(import.meta.url))), source_evidence: evidence,
  limits: {new_metadata_bytes: 12, new_entries: 1, report_targets: 4, fingerprint_chunk_bytes: 65536},
  target_open_mode: 'O_RDONLY', target_loaded_or_executed: false, backend_started: false, subprocesses: 0,
  code_bytes_read: 0, unwind_body_bytes_read: 0, full_target_copy_created: false,
  metadata_bytes: 0, metadata_reads: [], reused_metadata_bytes: 36, fingerprint_passes: [], boundaries: [],
  limitations: ['Exactly one new .pdata entry (index 610) is read; indices 340, 341 and 611 are reused from pinned saved reports. No code, neighbor entry, helper or search is read.',
    'Targets are saved fallthrough/branch continuations of two initializers. Exact and containing-predecessor reports do not establish logical-function completeness.',
    'PE mappings are reused from pinned evidence. Before/after full-file hashes traverse bytes with a 64 KiB buffer; equal hashes/stats are not an atomic snapshot.']};
let fd, initialStat, failure; const hashBuffer = Buffer.alloc(65536);
function fingerprint(label) {
  const hash = createHash('sha256'); let at = 0;
  while (at < expectedBytes) { const n = fs.readSync(fd, hashBuffer, 0, Math.min(65536, expectedBytes - at), at);
    assert.ok(n > 0); hash.update(hashBuffer.subarray(0, n)); at += n; }
  const pass = {label, bytes: at, chunk_bytes: 65536, sha256: hash.digest('hex')};
  result.fingerprint_passes.push(pass); return pass.sha256;
}
try {
  fd = fs.openSync(target, fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert.ok(initialStat.isFile() && initialStat.size === expectedBytes);
  result.sha256_before = fingerprint('before'); assert.equal(result.sha256_before, expectedHash);
  const raw = Buffer.alloc(12), read = {index: 610, rva: hex(newRva), file_offset: newOffset, requested_bytes: 12, read_bytes: 0};
  result.metadata_reads.push(read); result.metadata_bytes = 12;
  while (read.read_bytes < 12) { const n = fs.readSync(fd, raw, read.read_bytes, 12 - read.read_bytes, newOffset + read.read_bytes);
    assert.ok(n > 0); read.read_bytes += n; }
  read.raw_bytes = raw.toString('hex'); cached.set(610, raw);
  for (const r of requests) {
    const bytes = cached.get(r.entry), begin = bytes.readUInt32LE(0), end = bytes.readUInt32LE(4);
    assert.ok(begin < end); if (r.saved !== null) assert.equal(begin, r.rva);
    const map = mapping(begin, end - begin); assert.ok(map.section.characteristics & 0x20000000);
    const parsed = {exception_entry_index: r.entry, exception_entry_rva: hex(pdataRva + r.entry * 12),
      begin_rva: hex(begin), end_rva_exclusive: hex(end), bytes: end - begin,
      unwind_info_rva: hex(bytes.readUInt32LE(8)), file_offset: map.offset, section: map.section.name, code_bytes_read: 0};
    result.boundaries.push({requested_rva: hex(r.rva), initializer_rva: hex(r.initializer), table_slot: r.slot,
      relationship: r.relationship, source_evidence_index: r.proof, reused_saved_metadata: r.saved !== null,
      raw_entry_bytes: bytes.toString('hex'), inspected_entry: parsed,
      exact_boundary: begin === r.rva ? parsed : null,
      containing_predecessor: begin < r.rva && r.rva < end ? parsed : null});
  }
  result.status = 'four-fixed-continuation-boundaries-assessed';
} catch (error) { failure = error; result.status = 'failed'; result.failure = String(error); }
finally {
  if (fd !== undefined) {
    try { if (initialStat?.isFile() && initialStat.size === expectedBytes) {
      result.sha256_after = fingerprint('after'); const stat = fs.fstatSync(fd);
      result.source_stat_stable = ['size', 'dev', 'ino', 'mtimeMs', 'ctimeMs'].every(k => stat[k] === initialStat[k]);
      result.source_hash_preserved = result.sha256_before === expectedHash && result.sha256_after === expectedHash;
      assert.ok(result.source_stat_stable && result.source_hash_preserved); }
    } catch (error) { failure ??= error; result.status = 'failed'; result.final_fingerprint_failure = String(error); }
    finally { try { fs.closeSync(fd); } catch (error) { failure ??= error; result.status = 'failed'; result.close_failure = String(error); } }
  }
  result.finished_utc = new Date().toISOString(); checkDirectory(path.dirname(directory));
  if (!fs.existsSync(directory)) fs.mkdirSync(directory); checkDirectory(directory);
  fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`, {flag: 'wx'});
}
console.log(JSON.stringify({status: result.status, output, metadata_bytes: result.metadata_bytes}));
if (failure) process.exitCode = 1;
