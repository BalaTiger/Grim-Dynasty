// Explicit-input arithmetic projection of Engine 0x180044a10, mode 1 only.
// Evidence: r1-engine-mouse-core/05-Camera-GetImagePoint-0x180044a10-procedure_assembly.json
// SHA256: 1e4690f5a50aa7a602bda3fc8973a31ac96afbce2e93b88f229b3ea3dcf09f27
// Instructions 044b27..044bd9: A=+04/+08/+0C, B=+10/+14/+18,
// C=+1C/+20/+24, D=+28/+2C/+30, width=+38, height=+3C.
// cHalf was decoded as 0.5; disk data for cOne was 1.0. Neither establishes
// loaded runtime state, so both remain required inputs. No camera initialization,
// mode 0/tanf, scene, pixel-coordinate conversion or native ABI is implemented.
// Math.fround models ordinary float32 rounding per operation, not native MXCSR,
// floating-point exceptions or FTZ/DAZ. Finite float32 inputs and stable, unaliased
// input values are this own API's coverage boundary, not native rejection rules.

const f32 = Math.fround;
function scalar(value, name) {
  if (typeof value !== 'number' || !Number.isFinite(value) || !Number.isFinite(f32(value))) {
    throw new TypeError(`Explicit finite float32 value required: ${name}`);
  }
  return f32(value);
}
function required(input, name) {
  if (!Object.hasOwn(input, name)) throw new TypeError(`Explicit field required: ${name}`);
  return input[name];
}
function vector(input, name) {
  const value = required(input, name);
  if (!Array.isArray(value) || value.length !== 3) throw new TypeError(`Three components required: ${name}`);
  return [0, 1, 2].map(i => {
    if (!Object.hasOwn(value, i)) throw new TypeError(`Explicit component required: ${name}[${i}]`);
    return scalar(value[i], `${name}[${i}]`);
  });
}

// Returns a fresh three-component array. No input field or array is written.
export function cameraImagePointMode1(input) {
  if (input === null || typeof input !== 'object' || Array.isArray(input)) {
    throw new TypeError('Explicit mode-1 input object required');
  }
  const cOne = scalar(required(input, 'cOne'), 'cOne');
  const cHalf = scalar(required(input, 'cHalf'), 'cHalf');
  const width = scalar(required(input, 'width'), 'width');
  const height = scalar(required(input, 'height'), 'height');
  const x = scalar(required(input, 'x'), 'x');
  const y = scalar(required(input, 'y'), 'y');
  const a = vector(input, 'basisA'), b = vector(input, 'basisB');
  const c = vector(input, 'baseC'), d = vector(input, 'baseD');
  const u = f32(f32(f32(cOne - f32(x + x)) * width) * cHalf);
  const v = f32(f32(f32(cOne - f32(y + y)) * height) * cHalf);
  return [0, 1, 2].map(i => f32(f32(f32(d[i] + c[i]) + f32(u * a[i])) + f32(v * b[i])));
}
