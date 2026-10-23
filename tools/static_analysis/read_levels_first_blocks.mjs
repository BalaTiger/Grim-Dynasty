// Fixed two-member FIRST-BLOCK inspection. Importing this module performs no IO.
// An approved caller must explicitly invoke inspectLevelsFirstBlocks().
// Provenance:
// - docs/reverse-client/r1-levels-archive-index/{inspection,entries}.json:
//   actual2026-10-22 ARC directory and input SHA256, not a guessed fingerprint.
// - src/resource_codec/ResourceContainers.cs:86-95 and gd_resources.py ARC:
//   12-byte part=(offset,packed,unpacked); equal lengths mean raw, otherwise raw LZ4.
// - src/resource_codec/Lz4Block.cs:Length/Decode: bounded overlap-safe LZ4 port.
// Whole-file hashing streams all bytes. Structural reads are only the existing
// header/directory, TWO first-part records and their TWO compressed/raw blocks.
// This is not a full-member decode and cannot validate member Adler32 or map,
// navigation, scene or renderer formats. No external codec/package/DLL is used.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const PROJECT = 'C:\\Users\\zhuzi\\Documents\\Grim-Dynasty';
const SOURCE = path.win32.join(PROJECT, 'tools', 'static_analysis', 'read_levels_first_blocks.mjs');
const TARGET = 'D:\\Steam\\steamapps\\common\\Grim Dawn\\resources\\Levels.arc';
const OUTPUT = path.win32.join(PROJECT, 'docs', 'reverse-client', 'r1-levels-first-blocks');
const EXPECTED_SHA256 = 'adfc68f1f5d5b388226a86f997dcff0c8510512993ede2e86d0e90a39ae65f87';
const EXPECTED_SIZE = 221754949;
const HEADER_WORDS = [0x435241, 3, 2, 3130, 37560, 35, 221717266];
const MEMBERS = [
  { ordinal: 0, name: 'mainmenu/mainmenu.map', type: 3, offset: 2048, packed: 342509,
    unpacked: 1091357, checksum: 760486028, filetime: '134145362835729907', parts: 5,
    firstPart: 0, nameOffset: 0, nameLength: 21, outputStem: 'mainmenu-first-part' },
  { ordinal: 1, name: 'world001.map', type: 3, offset: 344557, packed: 221372709,
    unpacked: 818974129, checksum: 1539222734, filetime: '134342282181451447', parts: 3125,
    firstPart: 5, nameOffset: 22, nameLength: 12, outputStem: 'world001-first-part' }
];
const BLOCK_LIMIT = 1024 * 1024;
const JSON_LIMIT = 1024 * 1024;

function need(condition, message) { if (!condition) throw new Error(message); }
function samePath(a, b) { return path.win32.resolve(a).toLowerCase() === path.win32.resolve(b).toLowerCase(); }
function snapshot(s) {
  const result = {};
  for (const key of ['dev', 'ino', 'mode', 'nlink', 'size', 'mtimeNs', 'ctimeNs', 'birthtimeNs']) result[key] = s[key].toString();
  return result;
}
function unchanged(a, b, label) {
  const x = snapshot(a), y = snapshot(b);
  for (const key of Object.keys(x)) need(x[key] === y[key], `${label}: ${key} changed`);
}
function safePath(value, file) {
  need(path.win32.resolve(value) === value, 'Non-normalized fixed path');
  const root = path.win32.parse(value).root;
  const names = [root]; let current = root;
  for (const part of value.slice(root.length).split('\\')) { current = path.win32.join(current, part); names.push(current); }
  let stat;
  for (let n = 0; n < names.length; n++) {
    const name = names[n]; stat = fs.lstatSync(name, { bigint: true });
    need(!stat.isSymbolicLink(), `Link/junction rejected: ${name}`);
    need(samePath(fs.realpathSync.native(name), name), `Redirected/canonical path differs: ${name}`);
    if (file && n === names.length - 1) {
      need(stat.isFile() && stat.nlink === 1n && stat.ino !== 0n, 'Expected a regular single-link file with observable identity');
    } else need(stat.isDirectory(), `Expected a directory: ${name}`);
  }
  return stat;
}
function liveTarget(fd, before) {
  unchanged(before, safePath(TARGET, true), 'Path');
  const current = fs.fstatSync(fd, { bigint: true });
  need(current.isFile(), 'Descriptor is not a regular file'); unchanged(before, current, 'Descriptor'); return current;
}
function bounds(offset, length, limit, label) {
  need(Number.isSafeInteger(offset) && Number.isSafeInteger(length) && offset >= 0 && length >= 0
    && offset <= limit && length <= limit - offset, `${label}: out-of-range extent`);
}
function read(fd, offset, length, label, report) {
  bounds(offset, length, EXPECTED_SIZE, label); need(length <= BLOCK_LIMIT, `${label}: read exceeds1MiB`);
  const result = Buffer.alloc(length); let at = 0;
  while (at < length) { const n = fs.readSync(fd, result, at, length - at, offset + at); need(n > 0, `${label}: unexpected EOF`); at += n; }
  report.structuralReadRanges.push({ offset, bytes: length, purpose: label }); return result;
}
function hashBytes(bytes) { return crypto.createHash('sha256').update(bytes).digest('hex'); }
function hashFd(fd) {
  const hash = crypto.createHash('sha256'), buffer = Buffer.allocUnsafe(BLOCK_LIMIT); let at = 0;
  while (at < EXPECTED_SIZE) { const n = fs.readSync(fd, buffer, 0, Math.min(buffer.length, EXPECTED_SIZE - at), at); need(n > 0, 'Unexpected EOF during whole-file hash'); hash.update(buffer.subarray(0, n)); at += n; }
  return hash.digest('hex');
}
// Exact bound and back-reference behavior follow the reviewed own C# decoder.
// These bounds govern this sample only and do not claim to be native limits.
function decodeLz4(input, expected) {
  need(Buffer.isBuffer(input) && input.length <= BLOCK_LIMIT, 'LZ4 packed input limit');
  need(Number.isInteger(expected) && expected >= 0 && expected <= BLOCK_LIMIT, 'LZ4 output limit');
  const output = Buffer.alloc(expected); let p = 0, q = 0;
  function length(nibble) {
    let n = nibble;
    if (nibble === 15) {
      let x;
      do { need(p < input.length, 'LZ4 truncated length'); x = input[p++]; n += x; need(n <= expected, 'LZ4 length exceeds output bound'); } while (x === 255);
    }
    return n;
  }
  while (p < input.length) {
    const token = input[p++], literal = length(token >>> 4);
    need(literal <= expected - q && literal <= input.length - p, 'LZ4 literal bounds');
    input.copy(output, q, p, p + literal); p += literal; q += literal;
    if (p === input.length) break;
    need(input.length - p >= 2, 'LZ4 truncated offset');
    const distance = input[p] | (input[p + 1] << 8); p += 2;
    need(distance !== 0 && distance <= q, 'LZ4 invalid match offset');
    const match = length(token & 15) + 4; need(match <= expected - q, 'LZ4 match bounds');
    for (let n = 0; n < match; n++) { output[q] = output[q - distance]; q++; }
  }
  need(q === expected, 'LZ4 decoded size mismatch'); return output;
}
function verifyDecoderFixtures() {
  // The same8 memory-only cases as Lz4Probe.Tests; no process/file input.
  need(decodeLz4(Buffer.from([0x30, 97, 98, 99]), 3).toString('ascii') === 'abc', 'LZ4 literal fixture');
  need(decodeLz4(Buffer.from([0x14, 97, 1, 0]), 9).toString('ascii') === 'aaaaaaaaa', 'LZ4 overlap fixture');
  need(decodeLz4(Buffer.alloc(0), 0).length === 0, 'LZ4 empty fixture');
  for (const [input, expected] of [[[0xf0], 20], [[0x10, 97, 0, 0], 5], [[0x10, 97, 2, 0], 5], [[0x14, 97, 1, 0], 8], [[0x20, 97], 2]]) {
    let rejected = false; try { decodeLz4(Buffer.from(input), expected); } catch (_) { rejected = true; }
    need(rejected, 'Invalid LZ4 fixture unexpectedly accepted');
  }
  return 8;
}
function hexView(bytes) {
  const count = Math.min(bytes.length, 1024), lines = [];
  for (let at = 0; at < count; at += 16) {
    const row = bytes.subarray(at, Math.min(at + 16, count));
    const hex = [...row].map(x => x.toString(16).padStart(2, '0')).join(' ');
    const ascii = [...row].map(x => x >= 32 && x <= 126 ? String.fromCharCode(x) : '.').join('');
    lines.push(at.toString(16).padStart(8, '0') + '  ' + hex.padEnd(47, ' ') + '  ' + ascii);
  }
  return `Decoded member prefix; showing${count} of${bytes.length} first-part bytes. No map fields interpreted.\n` + lines.join('\n') + '\n';
}
function observations(bytes) {
  const words = [], runs = [], scanBytes = Math.min(bytes.length, 4096);
  for (let at = 0; at + 4 <= Math.min(bytes.length, 256); at += 4) {
    const value = bytes.readUInt32LE(at); words.push({ offset: at, u32LE: value, hex: '0x' + value.toString(16).padStart(8, '0') });
  }
  let at = 0;
  while (at < scanBytes && runs.length < 32) {
    if (bytes[at] < 32 || bytes[at] > 126) { at++; continue; }
    const start = at; while (at < scanBytes && bytes[at] >= 32 && bytes[at] <= 126) at++;
    if (at - start >= 4) runs.push({ offset: start, observedLength: at - start,
      text: bytes.subarray(start, Math.min(at, start + 96)).toString('ascii'), textTruncated: at - start > 96 });
  }
  return { meaning: 'Byte observations only; no magic/version/count/mesh/map semantics inferred',
    first16BytesHex: bytes.subarray(0, 16).toString('hex'), first256BytesAsU32LE: words,
    printableAsciiScanBytes: scanBytes, printableAsciiRuns: runs, hexViewBytes: Math.min(bytes.length, 1024) };
}
function readFirstBlocks(fd, report) {
  const rawHeader = read(fd, 0, 28, 'ARC header', report);
  const words = Array.from({ length: 7 }, (_, n) => rawHeader.readUInt32LE(n * 4));
  report.headerRawWords = words;
  for (let n = 0; n < words.length; n++) need(words[n] === HEADER_WORDS[n], `ARC header word${n} differs from accepted index`);
  const partOffset = words[6], namesOffset = partOffset + words[4], recordsOffset = namesOffset + words[5];
  const names = read(fd, namesOffset, words[5], 'fixed35-byte name table', report);
  const records = read(fd, recordsOffset, words[2] * 44, 'fixed two-entry directory', report);
  const artifacts = [];
  for (const expected of MEMBERS) {
    const base = expected.ordinal * 44;
    const entry = { ordinal: expected.ordinal, type: records.readUInt32LE(base), offset: records.readUInt32LE(base + 4),
      packed: records.readUInt32LE(base + 8), unpacked: records.readUInt32LE(base + 12), checksum: records.readUInt32LE(base + 16),
      filetime: records.readBigUInt64LE(base + 20).toString(), parts: records.readUInt32LE(base + 28),
      firstPart: records.readUInt32LE(base + 32), nameLength: records.readUInt32LE(base + 36), nameOffset: records.readUInt32LE(base + 40) };
    bounds(entry.nameOffset, entry.nameLength, names.length, 'member name');
    const rawName = names.subarray(entry.nameOffset, entry.nameOffset + entry.nameLength);
    for (const byte of rawName) need(byte <= 127, 'Non-ASCII name outside fixed observed profile');
    entry.name = rawName.toString('ascii').replace(/\0+$/, '');
    for (const key of Object.keys(entry)) need(entry[key] === expected[key], `Member${expected.ordinal} field${key} differs from accepted index`);
    need(entry.firstPart + entry.parts <= words[3] && entry.parts > 0, 'Member first-part relation is outside accepted ARC table');
    const member = { entry, status: 'Inspecting', wholeMemberAdler32Validated: false,
      boundary: 'Only first part; member checksum covers all parts and cannot be checked here' };
    report.members.push(member);
    const partRecordOffset = partOffset + entry.firstPart * 12;
    const rawPart = read(fd, partRecordOffset, 12, `${entry.name}: first part record only`, report);
    const part = { ordinal: entry.firstPart, recordOffset: partRecordOffset,
      offset: rawPart.readUInt32LE(0), packed: rawPart.readUInt32LE(4), unpacked: rawPart.readUInt32LE(8) };
    member.part = part;
    need(part.packed > 0 && part.packed <= BLOCK_LIMIT && part.unpacked > 0 && part.unpacked <= BLOCK_LIMIT,
      `${entry.name}: first part is empty or exceeds the explicit1MiB packed/decoded sample domain`);
    need(part.packed <= entry.packed && part.unpacked <= entry.unpacked, 'First-part lengths exceed declared whole-member lengths');
    const packed = read(fd, part.offset, part.packed, `${entry.name}: first compressed/raw block only`, report);
    member.packedSha256 = hashBytes(packed);
    const decoded = part.packed === part.unpacked ? Buffer.from(packed) : decodeLz4(packed, part.unpacked);
    need(decoded.length === part.unpacked, 'First-part decoded length mismatch');
    member.encodingRule = part.packed === part.unpacked ? 'raw (equal ARC part lengths)' : 'raw LZ4 (different ARC part lengths)';
    member.decodedBytes = decoded.length; member.decodedSha256 = hashBytes(decoded);
    member.observations = observations(decoded); member.memberFormat = 'Unknown'; member.status = 'DecodedFirstPartOnly';
    artifacts.push({ stem: expected.outputStem, bytes: decoded, hex: hexView(decoded), member });
  }
  return artifacts;
}
function reportJson(report) { const json = JSON.stringify(report, null, 2) + '\n'; need(Buffer.byteLength(json, 'utf8') <= JSON_LIMIT, 'Inspection JSON exceeds1MiB'); return json; }

export function inspectLevelsFirstBlocks() {
  need(process.platform === 'win32' && samePath(fileURLToPath(import.meta.url), SOURCE), 'Only reviewed fixed Windows source path is allowed');
  safePath(path.win32.dirname(OUTPUT), false);
  fs.mkdirSync(OUTPUT, { recursive: false }); // Existing evidence is never overwritten.
  const report = { schema: 'grim-dynasty-levels-first-blocks-v1', status: 'InProgress', phase: 'output-directory-check',
    startedUtc: new Date().toISOString(), target: TARGET, outputDirectory: OUTPUT,
    acceptedIndexInputSha256: EXPECTED_SHA256, acceptedIndexDirectorySha256: '32c76a11e9a984f5c889428e584fb75baa9ab9cdfe706977b50b6a56e4c79e17',
    provenance: ['docs/reverse-client/r1-levels-archive-index/inspection.json', 'docs/reverse-client/r1-levels-archive-index/entries.json',
      'src/resource_codec/ResourceContainers.cs:86-95', 'src/resource_codec/Lz4Block.cs:Length/Decode/Tests'],
    limits: { fixedMembers: 2, partRecordsPerMember: 1, compressedBytesPerMember: BLOCK_LIMIT, decodedBytesPerMember: BLOCK_LIMIT,
      hexBytesPerMember: 1024, asciiObservationBytesPerMember: 4096, reportBytes: JSON_LIMIT },
    boundaries: { sourceAccess: 'read-only same descriptor', wholeFileStreamedForHash: true, wholeArchiveDecompressed: false,
      wholeMemberDecompressed: false, wholeMemberAdler32Validated: false, mapOrNavFormatValidated: false,
      gameOrBackendStarted: false, savesAccessed: false,
      reparseTagCaveat: 'Node lstat/realpath/nlink do not expose every reparse tag; approved Attributes preflight remains required' },
    structuralReadRanges: [], members: [], sha256Before: null, sha256After: null };
  let fd = null;
  try {
    safePath(OUTPUT, false); report.phase = 'memory-only-codec-fixtures'; report.codecFixtureCases = verifyDecoderFixtures();
    report.phase = 'target-preflight'; const initial = safePath(TARGET, true);
    need(initial.size === BigInt(EXPECTED_SIZE), 'Archive size differs from accepted real index');
    report.statBefore = snapshot(initial); fd = fs.openSync(TARGET, fs.constants.O_RDONLY); liveTarget(fd, initial);
    report.phase = 'sha256-before'; report.sha256Before = hashFd(fd); liveTarget(fd, initial);
    need(report.sha256Before === EXPECTED_SHA256, 'Archive SHA256 differs from accepted real index; no blocks decoded');
    let extractionError = null, artifacts = null;
    report.phase = 'fixed-first-blocks';
    try { artifacts = readFirstBlocks(fd, report); } catch (error) { extractionError = error; report.extractionError = { name: error.name, message: String(error.message).slice(0, 4096) }; }
    report.phase = 'sha256-after'; report.sha256After = hashFd(fd); report.statAfter = snapshot(liveTarget(fd, initial));
    report.sha256Unchanged = report.sha256Before === report.sha256After;
    need(report.sha256Unchanged && report.sha256After === EXPECTED_SHA256, 'Archive changed during prefix inspection; no prefix artifacts published');
    fs.closeSync(fd); fd = null;
    if (extractionError) { report.phase = 'fixed-first-blocks'; throw extractionError; }
    report.phase = 'publish'; safePath(OUTPUT, false);
    for (const artifact of artifacts) {
      const binary = artifact.stem + '.bin', hex = artifact.stem + '.hex.txt';
      fs.writeFileSync(path.win32.join(OUTPUT, binary), artifact.bytes, { flag: 'wx' });
      fs.writeFileSync(path.win32.join(OUTPUT, hex), artifact.hex, { encoding: 'utf8', flag: 'wx' });
      artifact.member.outputFiles = { decodedFirstBlock: binary, first1024BytesHex: hex };
    }
    report.status = 'Succeeded'; report.phase = 'complete'; report.completedUtc = new Date().toISOString();
    fs.writeFileSync(path.win32.join(OUTPUT, 'inspection.json'), reportJson(report), { encoding: 'utf8', flag: 'wx' });
    console.log(`Wrote TWO first-block prefixes: ${OUTPUT}; whole-member checksum and map formats remain unverified`); return report;
  } catch (error) {
    if (fd !== null) { try { fs.closeSync(fd); } catch (closeError) { report.closeError = String(closeError.message).slice(0, 4096); } fd = null; }
    report.status = 'Failed'; report.completedUtc = new Date().toISOString();
    report.error = { name: error.name, message: String(error.message).slice(0, 4096), code: error.code || null };
    try { safePath(OUTPUT, false); fs.writeFileSync(path.win32.join(OUTPUT, 'inspection.json'), reportJson(report), { encoding: 'utf8', flag: 'wx' }); }
    catch (recordError) { console.error(`Could not publish failure evidence: ${String(recordError.message).slice(0, 4096)}`); }
    throw error;
  }
}
// No main(), import-time IO, auto-exec footer, external command or fallback.
