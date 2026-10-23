// Fixed GameCamera GetTarget imports and one directly referenced float constant; no backend/code reads.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(path.basename(self), 'read_r1_camera_target_bindings.mjs');
assert.equal(process.argv.length, 2);
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
function normal(p) {
  for (let q = path.resolve(p); ; q = path.dirname(q)) {
    assert(!fs.lstatSync(q).isSymbolicLink());
    assert.equal(fs.realpathSync.native(q).toLowerCase(), q.toLowerCase());
    if (path.dirname(q) === q) break;
  }
}
normal(self);
const proofs = [
  ['docs/object-lifecycle/offline-questitem-imports/import-bindings.json', '94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e'],
  ['docs/object-lifecycle/offline-item-950-elements-boundaries/metadata.json', '227610d69da80a91e5b96d40765e6cbf17cf070ca5b792809f3d84b24deea58b'],
  ['docs/object-lifecycle/offline-class-registration-exports/discovery.json', '0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58'],
  ['docs/reverse-client/r1-game-camera-position/05-GameCameraPosition-procedure_assembly.json', '7df3d4125c3a0246f87986e6b2381bec698893f575f45f016c4b0c7a2b072148'],
  ['docs/reverse-client/r1-game-camera-position/04-GameCameraPosition-procedure_pseudo_code.json', '035e07efc313444f8ef462b291218ccdd873f79e7a949a8e95e6a11f991320c9'],
  ['docs/reverse-client/r1-navigation-binding/inspection.json', 'd0c171ce77a6d67e153a6911fc7c3a17ea4d61e80ab9492f3287384524e14264'],
  ['docs/reverse-client/r1-camera-yaw-binding/inspection.json', 'bb0aa40b4031cdb042ed2ac1967e6bdc71752b8e2281e4c11caab97d54607b9e'],
  ['docs/phase-1/boundary-audit/module-boundaries.json', 'd1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7']
];
const data = proofs.map(([p, h]) => {
  const f = path.join(root, p);
  normal(f);
  assert(fs.statSync(f).isFile() && fs.statSync(f).size < 1048576);
  const b = fs.readFileSync(f);
  assert.equal(hash(b), h);
  return JSON.parse(b);
});
const gameHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const engineHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const gamePath = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const enginePath = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const selected = [
  { iat: 0x6094a0, index: 148, expected: '?GetRegion@WorldVec3@GAME@@QEBAPEAVRegion@2@XZ' },
  { iat: 0x609b70, index: 366, expected: '?TranslateInRegion@WorldVec3@GAME@@QEAAXAEBVVec3@2@@Z' },
  { iat: 0x60a690, index: 722, expected: '?GetCameraOffset@WorldCamera@GAME@@IEBAAEBVVec3@2@XZ' }
];
for (const [i, operation] of [[3, 'procedure_assembly'], [4, 'procedure_pseudo_code']]) {
  const e = data[i].result.structuredContent;
  assert.equal(e.subject.name, 'Game.dll');
  assert.equal(e.subject.digest.sha256, gameHash);
  assert.equal(path.resolve(e.subject.local_path).toLowerCase(), path.resolve(gamePath).toLowerCase());
  assert.equal(e.provider.id, 'ghidra');
  assert.equal(e.operation, operation);
  assert.equal(e.parameters.procedure, '0x1802be6e0');
  assert.equal(typeof e.normalized_result, 'string');
}
const asm = data[3].result.structuredContent.normalized_result;
for (const line of [
  '0x1802be705: CALL qword ptr [0x1806094a0]',
  '0x1802be752: CALL qword ptr [0x180609b70]',
  '0x1802be7a4: CALL qword ptr [0x18060a690]',
  '0x1802be72f: MOVSS XMM1, dword ptr [0x1807a8054]'
]) assert(asm.split('\n').includes(line));
const gameInventory = data[7].find(x => x.relativePath === 'x64/Game.dll');
assert.equal(gameInventory.sha256, gameHash);
const inventoryImports = gameInventory.importModules.filter(x => x.module === 'Engine.dll');
assert.equal(inventoryImports.length, 1);
for (const entry of selected) {
  assert.equal((entry.iat - 0x609000) / 8, entry.index);
  assert.equal(inventoryImports[0].symbols[entry.index], entry.expected);
}
const out = path.join(root, 'docs/reverse-client/r1-camera-target-bindings');
normal(path.dirname(out));
assert(!fs.existsSync(out));
const report = {
  schema: 'grim-dynasty-fixed-camera-target-bindings-v1', started_utc: new Date().toISOString(),
  proofs, script_sha256: hash(fs.readFileSync(self)), selected,
  metadata_bytes: 0, metadata_limit: 4096, code_bytes: 0, subprocesses: 0,
  original_executed: false, files: [], bindings: [], exports: [], failures: [],
  scope: 'Reuse cached nonzero OFT entries 0..733. Read only three selected IAT slots (8 bytes each), import hint/name <=512 bytes combined per slot, corresponding exact Engine hint/name/ordinal/EAT, and Game7A8054 exactly4bytes. Whole-file reads only for before/after SHA256. No new OFT reads, bodies, other constants or fallback scans.',
  limitations: [
    'Cached OFT coverage establishes continuity only for the selected Engine descriptor, not uniqueness across all descriptors or absence of overlapping aliases.',
    'Actual import names must match the pinned module inventory; Engine exports are independently matched by exact hint/name/ordinal/EAT. A mismatch stops without scanning.',
    'The Game7A8054 value is observed from its raw 4 bytes; no value is presumed from the pseudocode.',
    'Loaded IAT mutation, runtime object validity and target behavior remain unobserved. Executable-section membership is not a function-boundary proof.',
    'Before/after hashes and file metadata checks are not an atomic snapshot.'
  ]
};
function inspect(source, expected, size, pe, body) {
  normal(source);
  const f = { source, expected_sha256: expected, hash_bytes: 0, reads: [] };
  report.files.push(f);
  let fd, initial;
  function digest() {
    const h = createHash('sha256'), b = Buffer.alloc(65536);
    for (let at = 0; at < size; ) {
      const n = fs.readSync(fd, b, 0, Math.min(b.length, size - at), at);
      assert(n > 0);
      h.update(b.subarray(0, n));
      at += n;
      f.hash_bytes += n;
    }
    return h.digest('hex');
  }
  function mapping(rva, n) {
    assert(Number.isSafeInteger(rva) && rva >= 0 && Number.isSafeInteger(n) && n > 0);
    const ss = pe.sections.filter(s => rva >= s.rva && rva + n <= s.rva + s.raw_size);
    assert.equal(ss.length, 1);
    const s = ss[0], offset = s.raw_offset + rva - s.rva;
    assert(offset >= 0 && offset + n <= size);
    return { s, offset };
  }
  function read(rva, n, kind) {
    assert(n <= 512);
    const { s, offset } = mapping(rva, n);
    assert(!(s.characteristics & 0x20000000));
    report.metadata_bytes += n;
    assert(report.metadata_bytes <= report.metadata_limit);
    const b = Buffer.alloc(n);
    assert.equal(fs.readSync(fd, b, 0, n, offset), n);
    f.reads.push({ kind, rva: hex(rva), file_offset: offset, bytes: n, raw_hex: b.toString('hex') });
    return b;
  }
  try {
    fd = fs.openSync(source, fs.constants.O_RDONLY);
    initial = fs.fstatSync(fd);
    assert(initial.isFile());
    assert.equal(initial.size, size);
    f.sha256_before = digest();
    assert.equal(f.sha256_before, expected);
    body(read, mapping);
  } finally {
    if (fd !== undefined) {
      try {
        f.sha256_after = digest();
        assert.equal(f.sha256_after, expected);
        normal(source);
        for (const s of [fs.fstatSync(fd), fs.statSync(source)])
          for (const k of ['size', 'ino', 'dev', 'mtimeMs', 'ctimeMs']) assert.equal(s[k], initial[k]);
        f.stable = true;
      } finally {
        fs.closeSync(fd);
        f.closed = true;
      }
    }
  }
}
try {
  const cache = data[0].normal_imports[4].raw_metadata;
  const desc = Buffer.from(cache.descriptor.bytes, 'hex'), prefix = Buffer.from(cache.ownership_lookup_prefix.bytes, 'hex');
  assert.equal(desc.length, 20);
  assert.equal(desc.readUInt32LE(0), 0xa42ac0);
  assert.equal(desc.readUInt32LE(12), 0xa56aa6);
  assert.equal(desc.readUInt32LE(16), 0x609000);
  assert.equal(cache.dll_name_with_nul.bytes, '456e67696e652e646c6c00');
  assert.equal(Number(cache.ownership_lookup_prefix.rva), 0xa42ac0);
  assert.equal(cache.ownership_lookup_prefix.entries, 350);
  assert.equal(prefix.length, 350 * 8);
  function cachedGame(proof) {
    assert.equal(proof.status, 'passed');
    const f = proof.files.find(x => x.source === gamePath);
    assert(f && f.stable && f.closed);
    assert.equal(f.sha256_before, gameHash);
    assert.equal(f.sha256_after, gameHash);
    return f;
  }
  const navGame = cachedGame(data[5]);
  const savedTail = navGame.reads.find(x => x.kind === 'OFT-nonzero-continuation-through-selected-slot');
  assert(savedTail);
  assert.equal(savedTail.rva, '0xa435b0');
  assert.equal(savedTail.bytes, 138 * 8);
  const tail = Buffer.from(savedTail.raw_hex, 'hex');
  assert.equal(tail.length, 138 * 8);
  const yawGame = cachedGame(data[6]);
  const yawChunks = yawGame.reads.filter(x => x.kind === 'OFT-continuity-entries-488-through-733');
  assert.equal(yawChunks.length, 4);
  const expectedChunks = [[0xa43a00, 512], [0xa43c00, 512], [0xa43e00, 512], [0xa44000, 432]];
  const late = Buffer.concat(yawChunks.map((x, i) => {
    assert.equal(x.rva, hex(expectedChunks[i][0]));
    assert.equal(x.bytes, expectedChunks[i][1]);
    const b = Buffer.from(x.raw_hex, 'hex');
    assert.equal(b.length, x.bytes);
    return b;
  }));
  assert.equal(late.length, 246 * 8);
  const covered = Buffer.concat([prefix, tail, late]);
  assert.equal(covered.length, 734 * 8);
  for (let i = 0; i < covered.length; i += 8) assert.notEqual(covered.readBigUInt64LE(i), 0n);
  report.cached_descriptor = {
    source: proofs[0][0], index: 4, raw: cache.descriptor,
    continuation_sources: [proofs[5][0], proofs[6][0]], proven_nonzero_prefix_entries: 734,
    selected_slots_continuity_established: true, all_descriptor_uniqueness_established: false
  };
  assert.equal(data[1].sha256_after, gameHash);
  const gamePe = data[1].pe;
  assert.equal(gamePe.image_base, '0x180000000');
  inspect(gamePath, gameHash, 11385856, gamePe, read => {
    for (const entry of selected) {
      const lookup = covered.readBigUInt64LE(entry.index * 8);
      const iat = read(entry.iat, 8, 'exact-IAT-slot').readBigUInt64LE();
      assert.equal(iat, lookup);
      assert(lookup > 0n && lookup < 0x100000000n, 'Null, ordinal or non-RVA lookup: stop');
      const nr = Number(lookup), hint = read(nr, 2, 'exact-import-hint').readUInt16LE(), chars = [];
      let terminated = false;
      for (let i = 0; i < 510; i++) {
        const c = read(nr + 2 + i, 1, 'exact-import-name-byte')[0];
        if (!c) { terminated = true; break; }
        assert(c >= 32 && c < 127);
        chars.push(c);
      }
      assert(terminated, 'Unterminated name: stop');
      const symbol = Buffer.from(chars).toString('ascii');
      report.bindings.push({
        descriptor_dll: 'Engine.dll', descriptor_continuity_established: true,
        all_descriptor_uniqueness_established: false, iat_rva: hex(entry.iat),
        lookup_index: entry.index, lookup_rva: hex(0xa42ac0 + entry.index * 8),
        hint_name_rva: hex(nr), hint, symbol
      });
      assert.equal(symbol, entry.expected, 'Actual name differs from pinned inventory: stop, no fallback');
    }
    const constant = read(0x7a8054, 4, 'exact-GameCamera-GetTarget-float-constant');
    const numeric = constant.readFloatLE();
    report.constant = {
      rva: '0x7a8054', bytes: 4, raw_hex: constant.toString('hex'),
      bits: '0x' + constant.readUInt32LE().toString(16).padStart(8, '0'),
      float32: Number.isFinite(numeric) ? numeric : String(numeric),
      callsite_evidence: '0x1802be72f: MOVSS XMM1, dword ptr [0x1807a8054]'
    };
  });
  const proof = data[2].files.find(f => f.target === enginePath);
  assert(proof.source_hash_preserved && proof.source_stat_stable);
  assert.equal(proof.sha256_before, engineHash);
  assert.equal(proof.sha256_after, engineHash);
  const pe = proof.pe, raw = Buffer.from(pe.export_directory_bytes, 'hex');
  assert.equal(raw.length, 40);
  for (const [offset, value] of [[16, pe.ordinal_base], [20, pe.function_count], [24, pe.named_export_count], [28, Number(pe.address_table_rva)], [32, Number(pe.name_pointer_table_rva)], [36, Number(pe.name_ordinal_table_rva)]])
    assert.equal(raw.readUInt32LE(offset), value);
  inspect(enginePath, engineHash, 4485632, pe, (read, map) => {
    for (const binding of report.bindings) {
      const { hint, symbol } = binding;
      assert(hint < pe.named_export_count);
      const nameRva = read(Number(pe.name_pointer_table_rva) + hint * 4, 4, 'exact-hint-name-pointer').readUInt32LE();
      const wanted = Buffer.from(symbol + '\0', 'ascii');
      assert(read(nameRva, wanted.length, 'exact-export-name').equals(wanted), 'Hint/name mismatch: stop, no fallback scan');
      const ordinalIndex = read(Number(pe.name_ordinal_table_rva) + hint * 2, 2, 'exact-name-ordinal').readUInt16LE();
      assert(ordinalIndex < pe.function_count);
      const target = read(Number(pe.address_table_rva) + ordinalIndex * 4, 4, 'exact-EAT-entry').readUInt32LE();
      assert(target > 0);
      assert(!(target >= Number(pe.export_directory_rva) && target < Number(pe.export_directory_rva) + pe.export_directory_size), 'Forwarder unsupported');
      assert(map(target, 1).s.characteristics & 0x20000000);
      report.exports.push({
        iat_rva: binding.iat_rva, symbol, hint, name_rva: hex(nameRva), ordinal_index: ordinalIndex,
        ordinal: ordinalIndex + pe.ordinal_base, target_rva: hex(target), target_va: hex(0x180000000 + target)
      });
    }
  });
  assert.equal(report.bindings.length, 3);
  assert.equal(report.exports.length, 3);
} catch (e) {
  report.failures.push(String(e));
}
report.finished_utc = new Date().toISOString();
report.status = report.failures.length ? 'failed' : 'passed';
normal(path.dirname(out));
assert(!fs.existsSync(out));
fs.mkdirSync(out);
normal(out);
fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(report, null, 2) + '\n', { flag: 'wx' });
console.log(JSON.stringify({ status: report.status, bindings: report.bindings, exports: report.exports, constant: report.constant, metadata_bytes: report.metadata_bytes, files: report.files.map(({ reads, ...f }) => f), failures: report.failures }));
if (report.failures.length) process.exitCode = 1;
