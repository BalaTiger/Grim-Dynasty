// Pure, small, owned fixtures for analyzePoolWire. Never opens an archive or runs its CLI.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const output = path.join(root,
  'docs/object-lifecycle/official-cache-chain-pool-preflight/selftest.json');
assert(!fs.existsSync(output), 'Preserve prior selftest evidence');
const {analyzePoolWire} = await import('./inspect_official_pool_for_cache.mjs');
assert.equal(typeof analyzePoolWire, 'function', 'Expected the pure pool analyzer export');

const report = {
  status: 'started',
  started_utc: new Date().toISOString(),
  fixture_scope: 'Owned in-memory byte buffers only; no archive input or CLI execution',
  official_archive_read: false,
  formal_regression_suite_run: false,
  assertions_attempted: 0,
  assertions_passed: 0,
  scenarios_completed: 0,
  checks: []
};
let activeCheck = null;

function check(name, action) {
  activeCheck = name;
  report.assertions_attempted++;
  action();
  report.assertions_passed++;
  report.checks.push(name);
  activeCheck = null;
}

function wire(values) {
  const size = 4 + values.reduce((total, value) => total + 4 + value.length, 0);
  const bytes = Buffer.alloc(size);
  bytes.writeUInt32LE(values.length, 0);
  let cursor = 4;
  for (const value of values) {
    bytes.writeUInt32LE(value.length, cursor);
    cursor += 4;
    Buffer.from(value).copy(bytes, cursor);
    cursor += value.length;
  }
  return bytes;
}

function equals(name, actual, expected) {
  check(name, () => assert.deepEqual(actual, expected));
}

function mixedBytes() {
  const values = [
    [0x61], [0x61, 0x62], [0x61, 0x00, 0x62], [0x61], [0x41],
    [0x80, 0xff], [], [], [0x00], [0x80], [0x61, 0x00]
  ];
  const input = wire(values), before = Buffer.from(input);
  const result = analyzePoolWire(input), summary = result.summary;
  check('mixed/result-shape', () => assert(summary && typeof summary === 'object'));
  check('mixed/offsets-uint32', () => assert(result.offsets instanceof Uint32Array));
  check('mixed/lengths-uint32', () => assert(result.lengths instanceof Uint32Array));
  check('mixed/sorted-ids-uint32', () => assert(result.sortedIds instanceof Uint32Array));
  equals('mixed/string-count', summary.string_count, 11);
  equals('mixed/payload-bytes', summary.payload_bytes, 14);
  equals('mixed/unique-values', summary.unique_value_count, 9);
  equals('mixed/duplicate-groups', summary.duplicate_group_count, 2);
  equals('mixed/duplicate-excess', summary.duplicate_excess_entries, 2);
  equals('mixed/duplicate-participants', summary.entries_in_duplicate_groups, 4);
  equals('mixed/empty-count', summary.empty_count, 2);
  equals('mixed/empty-original-ids', summary.empty_ids_sample, [6, 7]);
  equals('mixed/maximum-string-bytes', summary.max_string_bytes, 3);
  equals('mixed/maximum-string-id', summary.max_string_id, 2);
  equals('mixed/source-ids-not-preserved',
    summary.source_ids_preserved_under_fresh_exact_byte_import, false);
  equals('mixed/exact-offsets', Array.from(result.offsets),
    [8, 13, 19, 26, 31, 36, 42, 46, 50, 55, 60]);
  equals('mixed/exact-lengths', Array.from(result.lengths),
    [1, 2, 3, 1, 1, 2, 0, 0, 1, 1, 2]);
  equals('mixed/full-byte-order-and-id-ties', Array.from(result.sortedIds),
    [6, 7, 8, 4, 0, 3, 10, 2, 1, 9, 5]);
  check('mixed/original-byte-slices', () => {
    for (let id = 0; id < values.length; id++)
      assert.deepEqual(Array.from(input.subarray(result.offsets[id],
        result.offsets[id] + result.lengths[id])), values[id]);
  });
  equals('mixed/input-unchanged', input, before);
  report.scenarios_completed++;
}

function uniqueNoEmpty() {
  // Includes invalid standalone UTF-8 and an interior NUL: the API compares bytes.
  const input = wire([[0xff], [0x61, 0x00], [0x41, 0x42, 0x43]]);
  const before = Buffer.from(input), result = analyzePoolWire(input);
  const summary = result.summary;
  equals('unique/string-count', summary.string_count, 3);
  equals('unique/payload-bytes', summary.payload_bytes, 6);
  equals('unique/unique-values', summary.unique_value_count, 3);
  equals('unique/duplicate-groups', summary.duplicate_group_count, 0);
  equals('unique/duplicate-excess', summary.duplicate_excess_entries, 0);
  equals('unique/duplicate-participants', summary.entries_in_duplicate_groups, 0);
  equals('unique/empty-count', summary.empty_count, 0);
  equals('unique/empty-original-ids', summary.empty_ids_sample, []);
  equals('unique/maximum-string-bytes', summary.max_string_bytes, 3);
  equals('unique/maximum-string-id', summary.max_string_id, 2);
  equals('unique/source-ids-preserved',
    summary.source_ids_preserved_under_fresh_exact_byte_import, true);
  equals('unique/full-byte-order', Array.from(result.sortedIds), [2, 1, 0]);
  equals('unique/exact-offsets', Array.from(result.offsets), [8, 13, 19]);
  equals('unique/exact-lengths', Array.from(result.lengths), [1, 2, 3]);
  equals('unique/input-unchanged', input, before);
  report.scenarios_completed++;
}

function malformedInputs() {
  const fixtures = [
    ['truncated-count', Buffer.from([1, 0, 0])],
    ['missing-length', Buffer.from([1, 0, 0, 0])],
    ['truncated-length', Buffer.from([1, 0, 0, 0, 1, 0])],
    ['truncated-payload', Buffer.from([1, 0, 0, 0, 3, 0, 0, 0, 0x61, 0x62])],
    ['trailing-byte', Buffer.from([0, 0, 0, 0, 0xff])],
    // Four bytes only; declared count must be rejected before metadata allocation.
    ['count-budget', Buffer.from([0xff, 0xff, 0xff, 0xff])]
  ];
  for (const [name, input] of fixtures) {
    const before = Buffer.from(input);
    check('reject/' + name, () => assert.throws(() => analyzePoolWire(input),
      error => error instanceof assert.AssertionError &&
        (name !== 'count-budget' || error.message === 'Pool string count budget')));
    equals('reject/' + name + '/input-unchanged', input, before);
    report.scenarios_completed++;
  }
}

let failure = false;
try {
  mixedBytes();
  uniqueNoEmpty();
  malformedInputs();
  report.status = 'passed';
} catch {
  failure = true;
  report.status = 'failed';
  report.failed_check = activeCheck;
} finally {
  report.finished_utc = new Date().toISOString();
  fs.mkdirSync(path.dirname(output), {recursive: true});
  fs.writeFileSync(output, JSON.stringify(report, null, 2) + '\n', {flag: 'wx'});
}
console.log(JSON.stringify({
  status: report.status,
  assertions: report.assertions_passed,
  scenarios: report.scenarios_completed,
  failed_check: report.failed_check ?? null
}));
if (failure) process.exitCode = 1;
