import test from 'node:test';
import assert from 'node:assert/strict';
import {cameraImagePointMode1 as project} from './camera_mode1_math.mjs';

// Hand-computable own fixtures only. No source files, target binaries, runtime
// processes, clocks or resource probes are read or launched by this suite.
const fixture = (changes = {}) => ({
  cOne: 1, cHalf: 0.5, width: 4, height: 6, x: 0.5, y: 0.5,
  basisA: [1, 0, 0], basisB: [0, 1, 0],
  baseC: [1, 2, 3], baseD: [10, 20, 30], ...changes,
});

test('center equals D plus C with explicit constants', () => {
  assert.deepEqual(project(fixture()), [11, 22, 33]);
});

test('four image corners keep the observed horizontal and vertical signs', () => {
  assert.deepEqual(project(fixture({x: 0, y: 0})), [13, 25, 33]);
  assert.deepEqual(project(fixture({x: 1, y: 0})), [9, 25, 33]);
  assert.deepEqual(project(fixture({x: 0, y: 1})), [13, 19, 33]);
  assert.deepEqual(project(fixture({x: 1, y: 1})), [9, 19, 33]);
});

test('non-axis basis contributes to all three coordinates in order', () => {
  assert.deepEqual(project(fixture({x: 0.25, y: 0.75, width: 8, height: 4,
    basisA: [2, -1, 3], basisB: [-4, 2, 1], baseD: [4, 5, 8]})), [13, 3, 16]);
});

test('image inputs outside zero to one are not clamped', () => {
  assert.deepEqual(project(fixture({x: -0.5, y: 1.5})), [15, 16, 33]);
});

test('cOne and cHalf are supplied values, not hidden defaults', () => {
  assert.deepEqual(project(fixture({cOne: 2, cHalf: 0.25})), [12, 23.5, 33]);
  assert.deepEqual(project(fixture({cOne: 0, cHalf: 0})), [11, 22, 33]);
});

test('float32 base then A then B additions cannot be reassociated', () => {
  // ((16777216 + 1) + (-16777216)) + 1 = 1 with rounding at each step;
  // doing the additions in double precision and rounding once would give 2.
  assert.deepEqual(project(fixture({x: 0, y: 0, width: 2, height: 2,
    baseD: [16777216, 0, 0], baseC: [1, 0, 0],
    basisA: [-16777216, 0, 0], basisB: [1, 0, 0]})), [1, 0, 0]);
});

test('explicit JS scalar inputs are rounded to float32 before arithmetic', () => {
  assert.deepEqual(project(fixture({cOne: 0, cHalf: 1, width: 1, height: 0,
    x: 16777217, baseC: [0, 0, 0], baseD: [0, 0, 0]})), [-33554432, 0, 0]);
});

test('missing fields and malformed values reject at the own API boundary', () => {
  for (const name of Object.keys(fixture())) {
    const input = fixture(); delete input[name];
    assert.throws(() => project(input), TypeError);
  }
  for (const input of [null, undefined, [], fixture({cOne: '1'}),
    fixture({x: NaN}), fixture({width: Infinity}), fixture({height: 1e100}),
    fixture({basisA: [1, 2]}), fixture({basisB: new Array(3)})]) {
    assert.throws(() => project(input), TypeError);
  }
});

test('frozen inputs remain untouched and outputs have independent storage', () => {
  const input = fixture();
  for (const value of Object.values(input)) if (Array.isArray(value)) Object.freeze(value);
  Object.freeze(input);
  const first = project(input), second = project(input);
  first[0] = 99;
  assert.deepEqual(input, fixture());
  assert.deepEqual(second, [11, 22, 33]);
  assert.notStrictEqual(first, second);
});
