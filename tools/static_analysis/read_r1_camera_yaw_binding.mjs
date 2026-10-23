// Exact Game IAT60A6E8 with bounded OFT continuity and matching Engine export; no backend or code reads.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(path.basename(self), 'read_r1_camera_yaw_binding.mjs');
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
  ['docs/reverse-client/r1-game-camera-angle/05-GameCameraAngle-procedure_assembly.json', '95f8b02e1eee3c75f7eb4954c723bb0687c3125450df9f8dcb9cfc315cd0de0f'],
  ['docs/reverse-client/r1-game-camera-angle/04-GameCameraAngle-procedure_pseudo_code.json', '9ccbdf43160e2913c8850e15cdb55e9e27b3c0246d2062c777e299b3009f624e'],
  ['docs/reverse-client/r1-navigation-binding/inspection.json', 'd0c171ce77a6d67e153a6911fc7c3a17ea4d61e80ab9492f3287384524e14264']
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
const expectedSymbol = '?SetCameraYaw@WorldCamera@GAME@@UEAAXM@Z';
for (const [i, operation] of [[3, 'procedure_assembly'], [4, 'procedure_pseudo_code']]) {
  const e = data[i].result.structuredContent;
  assert.equal(e.subject.name, 'Game.dll');
  assert.equal(e.subject.digest.sha256, gameHash);
  assert.equal(path.resolve(e.subject.local_path).toLowerCase(), path.resolve(gamePath).toLowerCase());
  assert.equal(e.provider.id, 'ghidra');
  assert.equal(e.operation, operation);
  assert.equal(e.parameters.procedure, '0x1802bfaf0');
  assert.equal(typeof e.normalized_result, 'string');
}
assert(data[3].result.structuredContent.normalized_result.includes('0x1802bfb01: CALL qword ptr [0x18060a6e8]\n0x1802bfb07: MOVSS dword ptr [RBX + 0x59c], XMM6'));
assert(data[4].result.structuredContent.normalized_result.includes('GAME::WorldCamera::SetCameraYaw((WorldCamera *)this,param_1);'));
const out = path.join(root, 'docs/reverse-client/r1-camera-yaw-binding');
normal(path.dirname(out));
assert(!fs.existsSync(out));
const report = {
  schema: 'grim-dynasty-fixed-camera-yaw-binding-v1', started_utc: new Date().toISOString(),
  proofs, script_sha256: hash(fs.readFileSync(self)), expected_symbol: expectedSymbol,
  metadata_bytes: 0, metadata_limit: 4096, code_bytes: 0, subprocesses: 0,
  original_executed: false, files: [], failures: [],
  scope: 'Reuse cached OFT entries 0..487; read exactly 488..733 (1968 bytes, chunks <=512) for selected-descriptor continuity, one 8-byte IAT entry, import hint/name <=512 bytes combined, and exact Engine hint/name/ordinal/EAT. Whole-file reads only for before/after SHA256. No other descriptors, slots after 733, or target bodies.',
  limitations: [
    'Cached OFT entries 0..487 and bounded fresh entries 488..733 establish continuity only for the selected Engine descriptor. This is not proof of uniqueness across all descriptors or absence of overlapping aliases.',
    'The selected OFT value must equal the exact IAT value and its actual import name must equal the expected WorldCamera SetCameraYaw symbol. The Engine export is independently verified by exact name, ordinal and EAT reads.',
    'Loaded IAT mutation, runtime object validity and function semantics remain unobserved. Executable-section membership is not a function-boundary proof.',
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
  for (let i = 0; i < 350; i++) assert.notEqual(prefix.readBigUInt64LE(i * 8), 0n);
  assert.equal(data[5].status, 'passed');
  const previousGame = data[5].files.find(f => f.source === gamePath);
  assert(previousGame && previousGame.stable && previousGame.closed);
  assert.equal(previousGame.sha256_before, gameHash);
  assert.equal(previousGame.sha256_after, gameHash);
  const savedTail = previousGame.reads.find(x => x.kind === 'OFT-nonzero-continuation-through-selected-slot');
  assert(savedTail);
  assert.equal(savedTail.rva, '0xa435b0');
  assert.equal(savedTail.bytes, 138 * 8);
  const tail = Buffer.from(savedTail.raw_hex, 'hex');
  assert.equal(tail.length, 138 * 8);
  for (let i = 0; i < tail.length; i += 8) assert.notEqual(tail.readBigUInt64LE(i), 0n);
  report.cached_descriptor = {
    source: proofs[0][0], index: 4, raw: cache.descriptor,
    initial_nonzero_prefix_entries: 350, continuation_source: proofs[5][0],
    proven_nonzero_prefix_entries: 488, selected_slot_continuity_established: false,
    all_descriptor_uniqueness_established: false
  };
  assert.equal(data[1].sha256_after, gameHash);
  const gamePe = data[1].pe;
  assert.equal(gamePe.image_base, '0x180000000');
  inspect(gamePath, gameHash, 11385856, gamePe, read => {
    const iatRva = 0x60a6e8, index = (iatRva - 0x609000) / 8;
    assert.equal(index, 733);
    const lookupRva = 0xa42ac0 + index * 8;
    assert.equal(lookupRva, 0xa441a8);
    const continuationStart = 0xa42ac0 + 488 * 8, continuationBytes = (index - 488 + 1) * 8;
    assert.equal(continuationStart, 0xa43a00);
    assert.equal(continuationBytes, 1968);
    const chunks = [];
    for (let at = 0; at < continuationBytes; at += 512) {
      const n = Math.min(512, continuationBytes - at);
      const chunk = read(continuationStart + at, n, 'OFT-continuity-entries-488-through-733');
      for (let i = 0; i < chunk.length; i += 8) assert.notEqual(chunk.readBigUInt64LE(i), 0n);
      chunks.push(chunk);
    }
    const continuation = Buffer.concat(chunks);
    assert.equal(continuation.length, continuationBytes);
    assert.equal(continuationStart + continuationBytes - 8, lookupRva);
    const lookup = continuation.readBigUInt64LE(continuationBytes - 8);
    report.cached_descriptor.selected_slot_continuity_established = true;
    report.cached_descriptor.proven_nonzero_entries_after_read = index + 1;
    const iat = read(iatRva, 8, 'exact-IAT-slot').readBigUInt64LE();
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
    report.binding = {
      descriptor_dll: 'Engine.dll', descriptor_continuity_established: true,
      all_descriptor_uniqueness_established: false,
      iat_rva: hex(iatRva), lookup_index: index, lookup_rva: hex(lookupRva),
      hint_name_rva: hex(nr), hint, symbol
    };
    assert.equal(symbol, expectedSymbol, 'Actual import name differs from Rea-supported candidate: stop, no fallback');
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
    const { hint, symbol } = report.binding;
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
    report.export = {
      symbol, hint, name_rva: hex(nameRva), ordinal_index: ordinalIndex,
      ordinal: ordinalIndex + pe.ordinal_base, target_rva: hex(target), target_va: hex(0x180000000 + target)
    };
  });
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
console.log(JSON.stringify({ status: report.status, binding: report.binding, export: report.export, metadata_bytes: report.metadata_bytes, files: report.files.map(({ reads, ...f }) => f), failures: report.failures }));
if (report.failures.length) process.exitCode = 1;
