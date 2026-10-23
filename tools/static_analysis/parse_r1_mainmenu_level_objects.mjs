// Saved-data observation only. No object factory, loaded actor or native ABI.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath, pathToFileURL } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
export const SAVED_SOURCE = 'docs/reverse-client/r1-mainmenu-complete-member/mainmenu.map.bin';
export const SAVED_SHA256 = 'a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289';
export const OUTPUT = 'docs/reverse-client/r1-mainmenu-level-object-records/inspection.json';
export const PINS = Object.freeze([
  ['docs/reverse-client/r1-level-object-records-run1/summary.json', 'ca9ba44521e33be91a4084d5787fb741620dbf2391f403f6cb428bc41d2846a3'],
  ['docs/reverse-client/r1-level-object-records-run1/04-LevelObjectRecords-0x180132580-procedure_pseudo_code.json', 'ec0967dbc443de0d0c8823695d2cf15f5f549b3208b0af96bdc3574ed7028f61'],
  ['docs/reverse-client/r1-level-object-records-run1/05-LevelObjectRecords-0x180132580-procedure_assembly.json', 'd77ef1437b3cf3c1471fc1b333fc8858b1b0234cfda01e4516eb29ee2512c249'],
  ['docs/reverse-client/r1-level-object-records-run1/06-LevelObjectRecords-0x180132580-procedure_callees.json', 'da7620b9e2f5d75451947a8232f3fafbb9f7aa8c5ddf23c42c161a56554aeca1'],
  ['docs/reverse-client/r1-mainmenu-complete-member/chunk5-string-table-observation.json', 'fa6b528ca3e2bc89508c9b2b46bd7cad6d41cc73094071de71722af0e6ed47d9'],
  ['docs/object-lifecycle/offline-archive-map-initial/283a80-instructions.json', '819e3c51bd1688bf0778405467bd17f4a5f60f93eb75174871b38a6e5186c8f4'],
  ['docs/reverse-client/r1-mainmenu-complete-member/level-chunk-observation.json', '558a9ac7959da4e64394c4195623e4576b6b081c8978f0869d18744d4d52e6d3'],
]);

export class SavedRecordUnsupported extends Error {
  constructor(reason, cursor) { super(reason); this.name = 'SavedRecordUnsupported'; this.reason = reason; this.cursor = cursor; }
}
const hex32 = value => `0x${value.toString(16).padStart(8, '0')}`;
function floatView(bits) {
  const b = Buffer.alloc(4); b.writeUInt32LE(bits); const f = b.readFloatLE();
  return Number.isNaN(f) ? 'NaN' : f === Infinity ? '+Infinity' : f === -Infinity ? '-Infinity' : Object.is(f, -0) ? '-0' : f;
}

// This is a strict own parser of the proved wire reads, not a simulation of
// native invalid-pointer behavior, factory failures, allocations or publication.
// Only caller-selected LVL15 is covered.132580 itself contains no version field.
export function parseChunk5(bytes, levelVersion, memberBase = 0) {
  if (!Buffer.isBuffer(bytes)) throw new SavedRecordUnsupported('ExpectedBuffer', 0);
  if (levelVersion !== 15) throw new SavedRecordUnsupported('UnsupportedLevelVersion', 0);
  if (bytes.length > 1024 * 1024 || !Number.isSafeInteger(memberBase) || memberBase < 0 || memberBase > Number.MAX_SAFE_INTEGER - bytes.length)
    throw new SavedRecordUnsupported('OwnInputExtent', 0);
  let cursor = 0;
  const need = n => { if (n > bytes.length - cursor) throw new SavedRecordUnsupported('TruncatedPayload', cursor); };
  const u32 = () => { need(4); const x = bytes.readUInt32LE(cursor); cursor += 4; return x; };
  const wireCount = u32();
  if (wireCount > 4096) throw new SavedRecordUnsupported('OwnStringCountBound', 0);
  const names = [], seen = new Map();
  for (let n = 0; n < wireCount; n++) {
    const at = cursor, length = u32();
    if (length > 0x100000) throw new SavedRecordUnsupported('NativeStringLengthGuard', at);
    need(length); const raw = bytes.subarray(cursor, cursor + length); cursor += length;
    const key = raw.toString('hex');
    //283A80 appends only a new exact byte string to +40/+48. Do not append
    // the synthetic empty cache entry used by a broader archive projection.
    if (!seen.has(key)) {
      seen.set(key, names.length);
      const ascii = raw.every(x => x < 128) ? raw.toString('ascii') : null;
      names.push({ index: names.length, raw_hex: key, ascii, contains_nul: raw.includes(0) });
    }
  }
  const tableEnd = cursor, countOffset = cursor, count = u32();
  if (count > 65536) throw new SavedRecordUnsupported('OwnRecordCountBound', countOffset);
  const records = [];
  for (let n = 0; n < count; n++) {
    const start = cursor;
    //132615 signed name index;132618/620/628 copy12 raw DWORDs, then
    //13262C advances34h. XYZ are the final3, proved by132B46..132BB7.
    need(56);
    const indexBits = u32(), index = indexBits | 0;
    const basis = Array.from({ length: 9 }, u32);
    const xyz = Array.from({ length: 3 }, u32);
    const uidFlag = u32();
    const uid = uidFlag !== 0 ? Array.from({ length: 4 }, u32) : null;
    const resolved = index >= 0 && index < names.length ? names[index] : null;
    records.push({
      ordinal: n, member_offset: memberBase + start, bytes: cursor - start,
      name_index: index, name_index_bits: hex32(indexBits),
      name_reference_valid: resolved !== null,
      resource_name_ascii: resolved?.ascii ?? null,
      resource_name_raw_hex: resolved?.raw_hex ?? null,
      resource_name_contains_nul: resolved?.contains_nul ?? null,
      basis_bits: basis.map(hex32), basis_f32: basis.map(floatView),
      region_local_xyz_bits: xyz.map(hex32), region_local_xyz_f32: xyz.map(floatView),
      uid_flag_bits: hex32(uidFlag), uid_payload_present: uidFlag !== 0,
      uid_bits: uid?.map(hex32) ?? null,
      actor_creation: 'unobserved', pathability: 'unknown', pathable_membership: 'unknown',
    });
  }
  if (cursor !== bytes.length) throw new SavedRecordUnsupported('UnconsumedChunk5Tail', cursor);
  return {
    covered_level_version: levelVersion, string_wire_count: wireCount,
    unique_string_count: names.length, string_table_end_member_exclusive: memberBase + tableEnd,
    record_count_member_offset: memberBase + countOffset, record_count: count,
    end_member_exclusive: memberBase + cursor, exact_exhaustion: true, names, records,
  };
}

const hashBytes = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
function hashHandle(fd, size) {
  const hash = crypto.createHash('sha256'), block = Buffer.alloc(65536);
  for (let p = 0; p < size;) {
    const n = fs.readSync(fd, block, 0, Math.min(block.length, size - p), p);
    if (n === 0) throw new Error('Incomplete saved hash read');
    hash.update(block.subarray(0, n)); p += n;
  }
  return hash.digest('hex');
}
function readAt(fd, at, size) {
  const b = Buffer.alloc(size);
  for (let n = 0; n < size;) {
    const k = fs.readSync(fd, b, n, size - n, at + n);
    if (!k) throw new Error('Incomplete saved range'); n += k;
  }
  return b;
}

export function inspectSavedMainmenu(expectedScriptHash) {
  const scriptHash = hashBytes(fs.readFileSync(fileURLToPath(import.meta.url)));
  if (!/^[0-9a-f]{64}$/.test(expectedScriptHash ?? '') || expectedScriptHash !== scriptHash)
    throw new Error('Exact reviewed script hash required');
  const pins = PINS.map(([file, hash]) => {
    if (hashBytes(fs.readFileSync(path.join(ROOT, file))) !== hash) throw new Error(`Evidence changed:${file}`);
    return { file, sha256: hash };
  });
  const summary = JSON.parse(fs.readFileSync(path.join(ROOT, PINS[0][0]), 'utf8'));
  if (summary.status !== 'passed' || summary.completed_plan_calls !== 8 || !summary.close_acknowledged ||
      !summary.closed_session_verified || !summary.process_exit_confirmed || summary.child_exit_code !== 0 ||
      !summary.owned_memory_profile?.job_empty_confirmed || !summary.owned_memory_profile?.sampler_exit_confirmed ||
      summary.target.sha256_before !== '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153' ||
      summary.target.sha256_after !== summary.target.sha256_before) throw new Error('Incomplete native evidence batch');
  const source = path.join(ROOT, SAVED_SOURCE), output = path.join(ROOT, OUTPUT);
  if (fs.existsSync(path.dirname(output))) throw new Error('Refuse existing output directory');
  if (fs.lstatSync(source).isSymbolicLink() || fs.realpathSync(source).toLowerCase() !== path.resolve(source).toLowerCase())
    throw new Error('Unexpected saved source link');
  const fd = fs.openSync(source, 'r'); let before, after, parsed;
  try {
    const stat = fs.fstatSync(fd);
    if (!stat.isFile() || stat.size !== 1091357) throw new Error('Saved source extent changed');
    before = hashHandle(fd, stat.size);
    if (before !== SAVED_SHA256) throw new Error('Saved source identity changed');
    const map = readAt(fd, 0, 8), level = readAt(fd, 263292, 28), chunk = readAt(fd, 263320, 8);
    if (map.subarray(0, 3).toString('ascii') !== 'MAP' || map[3] !== 9 || map.readUInt32LE(4) !== 1098 ||
        level.subarray(0, 3).toString('ascii') !== 'LVL' || level.readInt8(3) !== 15 ||
        chunk.readUInt32LE(0) !== 5 || chunk.readInt32LE(4) !== 8216) throw new Error('Pinned framing changed');
    parsed = parseChunk5(readAt(fd, 263328, 8216), level.readInt8(3), 263328);
    if (parsed.string_wire_count !== 58 || parsed.unique_string_count !== 58 || parsed.string_table_end_member_exclusive !== 266612)
      throw new Error('Previously verified string table differs');
    after = hashHandle(fd, stat.size);
    if (after !== before) throw new Error('Saved source changed during read');
  } finally { fs.closeSync(fd); }
  // Diagnostic ranking only. Origin proximity does not establish mesh overlap:
  // a large object's origin may be far away. Every parsed record is retained.
  const proximity = parsed.records.map(r => ({ r, xyz: r.region_local_xyz_f32.map(x => x === '-0' ? -0 : x) }))
    .filter(({ xyz }) => xyz.every(x => typeof x === 'number' && Number.isFinite(x)))
    .map(({ r, xyz }) => ({ ordinal: r.ordinal, resource_name_ascii: r.resource_name_ascii,
      region_local_xyz: r.region_local_xyz_f32,
      origin_xz_distance_squared: (xyz[0] - 63) ** 2 + (xyz[2] - 52.8) ** 2 }))
    .sort((a, b) => a.origin_xz_distance_squared - b.origin_xz_distance_squared || a.ordinal - b.ordinal).slice(0, 8);
  const result = {
    schema: 'grim-dynasty-saved-mainmenu-level-object-records-v1', status: 'parsed-saved-records-only',
    script_sha256: scriptHash, source: { file: SAVED_SOURCE, sha256_before: before, sha256_after: after,
      same_handle: true, closed: true, semantic_ranges: [[0, 8], [263292, 28], [263320, 8], [263328, 8216]] },
    evidence: pins, parsed,
    nearest_saved_origins: { own_diagnostic_xz: [63, 52.8], interpretation: 'Origin proximity only; not bounds, intersection or pathability', records: proximity },
    native_consumption: '132615 index;132618/620/628 48B transform;13264A UID flag;13265E..132692 optional16B;132B46..132BB7 XYZ/basis publication',
    limitations: [
      'No factory, class check, mode gate, UID registration, vslot270, LoadObjectData, vslotD8, spatial133A10 or PathableActors registration was executed.',
      'No mesh, actor bounds, geometry surface or pathability is inferred from record names, transforms or proximity.',
      'Raw float bits and nonzero UID flags are preserved. UID application depends on external param5 and is not asserted.',
      'Only saved MAP9/LVL15/chunk5 is covered. Own bounded parse failures do not model native malformed-input behavior.',
      'No original read/execution,backend,compiler,DLL load,navigation rebake,terrain/extent change or gameplay test.'
    ], original_read: false, backend_started: false, actor_created: false, pathable_membership_recovered: false,
  };
  fs.mkdirSync(path.dirname(output));
  fs.writeFileSync(output, JSON.stringify(result, null, 2) + '\n', { flag: 'wx' });
  return { output: OUTPUT, sha256: hashBytes(fs.readFileSync(output)), records: parsed.record_count,
    bytes_consumed: parsed.end_member_exclusive - 263328, nearest_saved_origins: proximity };
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  if (process.argv.length !== 4 || process.argv[2] !== '--expected-script-sha256') throw new Error('Expected fixed script hash argument');
  console.log(JSON.stringify(inspectSavedMainmenu(process.argv[3]), null, 2));
}
