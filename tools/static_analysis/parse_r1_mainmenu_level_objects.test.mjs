import assert from 'node:assert/strict';
import { parseChunk5, SavedRecordUnsupported } from './parse_r1_mainmenu_level_objects.mjs';

let scenarios = 0, assertions = 0;
function check(value, message) { assertions++; assert.ok(value, message); }
function equal(a, b, message) { assertions++; assert.deepEqual(a, b, message); }
function word(value) { const b = Buffer.alloc(4); b.writeUInt32LE(value >>> 0); return b; }
function record(index, values, flag = 0, uid = []) {
  return Buffer.concat([word(index), ...values.map(word), word(flag), ...uid.map(word)]);
}
const identityPosition = [0x3f800000, 0, 0, 0, 0x3f800000, 0, 0, 0, 0x3f800000, 0x427c0000, 0x41880000, 0x42533333];
function wire(names, rows) {
  return Buffer.concat([word(names.length), ...names.flatMap(name => {
    const b = Buffer.from(name, 'ascii'); return [word(b.length), b];
  }), word(rows.length), ...rows]);
}
function unsupported(bytes, version, reason) {
  assertions++; assert.throws(() => parseChunk5(bytes, version), e => e instanceof SavedRecordUnsupported && e.reason === reason);
}

// Source ordinals and native unique-string indices differ when names repeat.
// The archive helper's synthetic empty cache entry must not become an index.
{
  scenarios++;
  const bytes = wire(['same', 'same', 'other'], [record(1, identityPosition), record(2, identityPosition)]);
  const p = parseChunk5(bytes, 15, 1000);
  equal(p.unique_string_count, 2); equal(p.records[0].resource_name_ascii, 'other');
  equal(p.records[1].name_reference_valid, false); equal(p.records[1].resource_name_ascii, null);
  equal(p.records[0].region_local_xyz_bits, ['0x427c0000', '0x41880000', '0x42533333']);
  equal(p.records[0].basis_bits.length, 9); equal(p.records[0].bytes, 56);
  equal(p.end_member_exclusive, 1000 + bytes.length);
}
// Every nonzero UID flag consumes16 bytes, including noncanonical flag2.
// The next negative-name record remains represented rather than discarded.
{
  scenarios++;
  const uid = [0, 0xffffffff, 0x80000000, 0x12345678];
  const p = parseChunk5(wire(['r'], [record(0, identityPosition, 2, uid), record(-1, identityPosition)]), 15);
  equal(p.records[0].uid_flag_bits, '0x00000002'); equal(p.records[0].uid_bits, uid.map(x => `0x${x.toString(16).padStart(8, '0')}`));
  equal(p.records[0].bytes, 72); equal(p.records[1].member_offset, p.records[0].member_offset + 72);
  equal(p.records[1].name_index, -1); equal(p.records[1].name_reference_valid, false);
  equal(p.records[1].actor_creation, 'unobserved'); equal(p.records[1].pathability, 'unknown');
}
// Preserve payload bits and exceptional floats; do not normalize or invent a
// valid transform. Resource bytes containingNUL remain distinguishable too.
{
  scenarios++;
  const values = [...identityPosition]; values[0] = 0x7fc12345;
  values[9] = 0x80000000; values[10] = 0x7f800000; values[11] = 0xff800000;
  const p = parseChunk5(wire(['a\0b'], [record(0, values)]), 15);
  equal(p.records[0].basis_bits[0], '0x7fc12345'); equal(p.records[0].basis_f32[0], 'NaN');
  equal(p.records[0].region_local_xyz_f32, ['-0', '+Infinity', '-Infinity']);
  equal(p.records[0].resource_name_raw_hex, '610062'); check(p.records[0].resource_name_contains_nul);
}
// Reject unknown version, source extents, missing optional UID, and leftovers.
{
  scenarios++;
  const base = wire(['r'], [record(0, identityPosition)]);
  unsupported(base, 14, 'UnsupportedLevelVersion');
  unsupported(base.subarray(0, base.length - 1), 15, 'TruncatedPayload');
  const uid = wire(['r'], [record(0, identityPosition, 1, [1, 2, 3, 4])]);
  unsupported(uid.subarray(0, uid.length - 1), 15, 'TruncatedPayload');
  unsupported(Buffer.concat([base, word(0)]), 15, 'UnconsumedChunk5Tail');
  unsupported(word(0xffffffff), 15, 'OwnStringCountBound');
  unsupported(Buffer.concat([word(0), word(0xffffffff)]), 15, 'OwnRecordCountBound');
  equal(parseChunk5(wire([], []), 15).record_count, 0);
}
console.log(JSON.stringify({ status: 'passed-new-saved-object-record-format-tests-only', scenarios, assertions,
  saved_map_read: false, original_read: false, backend_started: false, actor_created: false }));
