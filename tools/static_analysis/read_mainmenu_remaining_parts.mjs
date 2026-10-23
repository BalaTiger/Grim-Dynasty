// PREPARATION ONLY: importing this module performs no I/O. An approved caller
// must explicitly invoke inspectMainmenuRemainingParts(). No command-line main.
// Fixed scope: saved first part0 + original ARC parts1..4 for mainmenu only.
// This completes ONE 1,091,357-byte ARC member and checks its Adler32. It does
// not interpret MAP/LVL/PTHD, slice a Level, read world001, or start game/backend.
// Full-file hashes necessarily stream the entire ARC; semantic original reads
// are limited to four12-byte records and their four compressed/raw blocks.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const PROJECT = 'C:\\Users\\zhuzi\\Documents\\Grim-Dynasty';
const SOURCE = path.win32.join(PROJECT, 'tools', 'static_analysis', 'read_mainmenu_remaining_parts.mjs');
const TARGET = 'D:\\Steam\\steamapps\\common\\Grim Dawn\\resources\\Levels.arc';
const OUTPUT = path.win32.join(PROJECT, 'docs', 'reverse-client', 'r1-mainmenu-complete-member');
const ARC_SHA256 = 'adfc68f1f5d5b388226a86f997dcff0c8510512993ede2e86d0e90a39ae65f87';
const ARC_BYTES = 221754949;
const MEMBER = { ordinal: 0, name: 'mainmenu/mainmenu.map', type: 3, offset: 2048,
  packed: 342509, unpacked: 1091357, checksum: 760486028, filetime: '134145362835729907',
  parts: 5, firstPart: 0, nameOffset: 0, nameLength: 21 };
const FIRST = { ordinal: 0, offset: 2048, packed: 211505, unpacked: 262144,
  sha256: '17a7e728a455cc92afc53228d3832d1a07c3331519f849b2d50d2da565f05180' };
const PART_TABLE_OFFSET = 221717266;
const BLOCK_LIMIT = 1024 * 1024;
const PINS = [
  { key: 'indexReport', relative: 'docs/reverse-client/r1-levels-archive-index/inspection.json', bytes: 3776,
    sha256: 'e8dd64f4606a6cbbb2548d8aa47e4f64f68a4215f2c99446e84c0e6e123790fb' },
  { key: 'indexEntries', relative: 'docs/reverse-client/r1-levels-archive-index/entries.json', bytes: 480,
    sha256: '32c76a11e9a984f5c889428e584fb75baa9ab9cdfe706977b50b6a56e4c79e17' },
  { key: 'firstReport', relative: 'docs/reverse-client/r1-levels-first-blocks/inspection.json', bytes: 29068,
    sha256: '47b415307e0eef792cf7c674c196f4b4da35248d8ce4204b9401472c7a4f08c1' },
  { key: 'firstBytes', relative: 'docs/reverse-client/r1-levels-first-blocks/mainmenu-first-part.bin', bytes: FIRST.unpacked,
    sha256: FIRST.sha256 },
  { key: 'metadataObservation', relative: 'docs/reverse-client/r1-levels-first-blocks/mainmenu-metadata-observation.json', bytes: 4037,
    sha256: 'cc6e981d4abd97b0d21872a364dc5dc1aea5aa452883ba4aaadb314b0f790d22' },
  { key: 'reviewedDecoderSource', relative: 'tools/static_analysis/read_levels_first_blocks.mjs', bytes: 17195,
    sha256: '2ad2ef626d8549bc789f69d7170b164d6f6505e784ba97645a8a4e1210024b25' }
];

function need(condition, message) { if (!condition) throw new Error(message); }
function samePath(a, b) { return path.win32.resolve(a).toLowerCase() === path.win32.resolve(b).toLowerCase(); }
function statSnapshot(stat) {
  const result = {};
  for (const key of ['dev', 'ino', 'mode', 'nlink', 'size', 'mtimeNs', 'ctimeNs', 'birthtimeNs']) result[key] = stat[key].toString();
  return result;
}
function sameStat(before, after, label) {
  const a = statSnapshot(before), b = statSnapshot(after);
  for (const key of Object.keys(a)) need(a[key] === b[key], `${label}: ${key} changed`);
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
function liveFile(fd, filename, before) {
  sameStat(before, safePath(filename, true), 'Path');
  const stat = fs.fstatSync(fd, { bigint: true });
  need(stat.isFile(), 'Descriptor is not a regular file'); sameStat(before, stat, 'Descriptor'); return stat;
}
function bounds(offset, length, limit, label) {
  need(Number.isSafeInteger(offset) && Number.isSafeInteger(length) && offset >= 0 && length >= 0
    && offset <= limit && length <= limit - offset, `${label}: out-of-range extent`);
}
function readExact(fd, offset, length, fileSize, label) {
  bounds(offset, length, fileSize, label); need(length <= BLOCK_LIMIT, `${label}: individual read exceeds1MiB`);
  const bytes = Buffer.alloc(length); let at = 0;
  while (at < length) { const n = fs.readSync(fd, bytes, at, length - at, offset + at); need(n > 0, `${label}: unexpected EOF`); at += n; }
  return bytes;
}
function hashBytes(bytes) { return crypto.createHash('sha256').update(bytes).digest('hex'); }
function hashArc(fd) {
  const hash = crypto.createHash('sha256'), buffer = Buffer.allocUnsafe(BLOCK_LIMIT); let offset = 0;
  while (offset < ARC_BYTES) {
    const n = fs.readSync(fd, buffer, 0, Math.min(buffer.length, ARC_BYTES - offset), offset);
    need(n > 0, 'Unexpected EOF during full-archive hash'); hash.update(buffer.subarray(0, n)); offset += n;
  }
  return hash.digest('hex');
}
function pinnedFile(pin) {
  const filename = path.win32.join(PROJECT, pin.relative), before = safePath(filename, true);
  need(before.size === BigInt(pin.bytes), `${pin.key}: pinned size mismatch`);
  const fd = fs.openSync(filename, fs.constants.O_RDONLY);
  try {
    liveFile(fd, filename, before);
    const bytes = readExact(fd, 0, pin.bytes, pin.bytes, pin.key);
    liveFile(fd, filename, before); need(hashBytes(bytes) === pin.sha256, `${pin.key}: pinned SHA256 mismatch`); return bytes;
  } finally { fs.closeSync(fd); }
}
function readProvenance(report) {
  const values = {};
  for (const pin of PINS) { values[pin.key] = pinnedFile(pin); report.verifiedWorkspaceInputs.push(pin); }
  const index = JSON.parse(values.indexReport.toString('utf8'));
  const entries = JSON.parse(values.indexEntries.toString('utf8'));
  const first = JSON.parse(values.firstReport.toString('utf8'));
  const observation = JSON.parse(values.metadataObservation.toString('utf8'));
  need(index.status === 'Succeeded' && index.sha256Before === ARC_SHA256 && index.sha256After === ARC_SHA256
    && index.header.partOffset === PART_TABLE_OFFSET, 'Pinned index does not establish the expected archive/part table');
  need(entries.entries.length === 2, 'Pinned index entry count differs');
  for (const key of Object.keys(MEMBER)) need(entries.entries[0][key] === MEMBER[key], `Pinned mainmenu entry differs: ${key}`);
  const prior = first.members[0];
  need(first.status === 'Succeeded' && first.sha256Before === ARC_SHA256 && first.sha256After === ARC_SHA256
    && prior.entry.name === MEMBER.name && prior.decodedSha256 === FIRST.sha256 && prior.decodedBytes === FIRST.unpacked,
  'Pinned first-block evidence differs');
  for (const key of ['ordinal', 'offset', 'packed', 'unpacked']) need(prior.part[key] === FIRST[key], `Pinned first part differs: ${key}`);
  need(observation.source.sha256 === FIRST.sha256 && observation.source.saved_bytes === FIRST.unpacked,
    'Metadata observation does not refer to the pinned saved prefix');
  report.metadataObservationBoundary = 'Pinned as provenance only; Region tail DWORDs are not used as offsets or sizes by this reader';
  report.codecProvenance = 'decodeLz4 body reused from pinned reviewed first-block reader; previous8 fixtures are not rerun';
  return values.firstBytes;
}
// Reused verbatim from the pinned read_levels_first_blocks.mjs decoder. There
// is deliberately no fixture runner or invocation of that module's entrypoint.
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
// Same per-byte modulo algorithm as ResourceUtil.Adler, not a native runtime call.
function adler32(bytes) {
  let a = 1, c = 0;
  for (const x of bytes) { a = (a + x) % 65521; c = (c + a) % 65521; }
  return ((c << 16) | a) >>> 0;
}
function arcRead(fd, offset, length, purpose, report) {
  const bytes = readExact(fd, offset, length, ARC_BYTES, purpose);
  report.structuralReadRanges.push({ offset, bytes: length, purpose }); return bytes;
}
function completeMember(fd, firstBytes, report) {
  const tableStart = PART_TABLE_OFFSET + 12;
  const raw = arcRead(fd, tableStart, 4 * 12, 'Only mainmenu part records1..4', report);
  const payloadStart = FIRST.offset + FIRST.packed, payloadEnd = MEMBER.offset + MEMBER.packed;
  const parts = []; let packedTotal = FIRST.packed, decodedTotal = FIRST.unpacked;
  for (let n = 0; n < 4; n++) {
    const part = { ordinal: n + 1, recordOffset: tableStart + n * 12,
      offset: raw.readUInt32LE(n * 12), packed: raw.readUInt32LE(n * 12 + 4), unpacked: raw.readUInt32LE(n * 12 + 8) };
    need(part.packed > 0 && part.packed <= BLOCK_LIMIT && part.unpacked > 0 && part.unpacked <= BLOCK_LIMIT,
      `Part${part.ordinal}: empty or outside1MiB block domain`);
    // This fixed probe is restricted to the remaining compressed-member range.
    // Failure of this guard stops the probe, not a claim of invalid native ARC.
    need(part.offset >= payloadStart && part.offset <= payloadEnd && part.packed <= payloadEnd - part.offset,
      `Part${part.ordinal}: outside this probe's fixed remaining-mainmenu input range`);
    for (const previous of parts) need(part.offset + part.packed <= previous.offset
      || previous.offset + previous.packed <= part.offset, 'Remaining compressed blocks overlap');
    packedTotal += part.packed; decodedTotal += part.unpacked;
    need(packedTotal <= MEMBER.packed && decodedTotal <= MEMBER.unpacked, 'Part aggregates exceed fixed mainmenu entry');
    parts.push(part);
  }
  need(packedTotal === MEMBER.packed && decodedTotal === MEMBER.unpacked, 'Five-part aggregate lengths differ from fixed entry');
  report.parts = parts; report.memberPackedBytes = packedTotal; report.memberDecodedBytes = decodedTotal;
  const decoded = [firstBytes];
  for (const part of parts) {
    const packed = arcRead(fd, part.offset, part.packed, `mainmenu part${part.ordinal} compressed/raw block`, report);
    part.packedSha256 = hashBytes(packed);
    const bytes = part.packed === part.unpacked ? Buffer.from(packed) : decodeLz4(packed, part.unpacked);
    need(bytes.length === part.unpacked, 'Part decoded size mismatch');
    part.encodingRule = part.packed === part.unpacked ? 'raw (equal lengths)' : 'raw LZ4 (different lengths)';
    part.decodedSha256 = hashBytes(bytes); decoded.push(bytes);
  }
  const member = Buffer.concat(decoded, MEMBER.unpacked);
  need(member.length === MEMBER.unpacked, 'Whole mainmenu decoded length mismatch');
  report.memberAdler32 = adler32(member);
  report.memberAdler32Expected = MEMBER.checksum;
  need(report.memberAdler32 === MEMBER.checksum, 'Whole mainmenu Adler32 mismatch; no member artifact published');
  report.memberAdler32Validated = true; report.memberSha256 = hashBytes(member); return member;
}
function reportJson(report) {
  const text = JSON.stringify(report, null, 2) + '\n'; need(Buffer.byteLength(text, 'utf8') <= BLOCK_LIMIT, 'Inspection JSON exceeds1MiB'); return text;
}

export function inspectMainmenuRemainingParts() {
  need(process.platform === 'win32' && samePath(fileURLToPath(import.meta.url), SOURCE), 'Only the reviewed fixed Windows source path is allowed');
  safePath(path.win32.dirname(OUTPUT), false);
  fs.mkdirSync(OUTPUT, { recursive: false }); // Reject every existing path; never remove/reuse evidence.
  const report = { schema: 'grim-dynasty-mainmenu-complete-member-v1', status: 'InProgress', phase: 'output-directory-check',
    startedUtc: new Date().toISOString(), target: TARGET, outputDirectory: OUTPUT, member: MEMBER,
    expectedArcSha256: ARC_SHA256, expectedArcBytes: ARC_BYTES, reusedFirstPart: FIRST,
    limits: { additionalPartRecords: 4, partTableBytes: 48, additionalPackedBytes: MEMBER.packed - FIRST.packed,
      additionalDecodedBytes: MEMBER.unpacked - FIRST.unpacked, perBlockBytes: BLOCK_LIMIT,
      completeMemberBytes: MEMBER.unpacked, remainingPayloadStart: FIRST.offset + FIRST.packed,
      remainingPayloadEndExclusive: MEMBER.offset + MEMBER.packed, reportBytes: BLOCK_LIMIT },
    boundaries: { sourceAccess: 'read-only same descriptor', wholeArchiveStreamedForHash: true,
      firstPartCompressedBlockReread: false, otherMemberSemanticallyRead: false, wholeArchiveDecompressed: false,
      completeMainmenuMemberOnly: true, levelSliced: false, lvlOrPthdInterpreted: false,
      mapOrNavigationFormatValidated: false, gameOrBackendStarted: false, savesAccessed: false,
      oldFixtureCasesRerun: 0, metadataObservationUsedOnlyAsProvenance: true,
      reparseTagCaveat: 'Node does not expose every reparse tag; approved file/ancestor Attributes preflight remains required' },
    verifiedWorkspaceInputs: [], structuralReadRanges: [], sha256Before: null, sha256After: null,
    memberAdler32Validated: false };
  let fd = null;
  try {
    safePath(OUTPUT, false); report.phase = 'pinned-workspace-inputs'; const first = readProvenance(report);
    report.phase = 'target-preflight'; const before = safePath(TARGET, true);
    need(before.size === BigInt(ARC_BYTES), 'Original archive size differs from pinned evidence');
    report.statBefore = statSnapshot(before); fd = fs.openSync(TARGET, fs.constants.O_RDONLY); liveFile(fd, TARGET, before);
    report.phase = 'sha256-before'; report.sha256Before = hashArc(fd); liveFile(fd, TARGET, before);
    need(report.sha256Before === ARC_SHA256, 'Archive fingerprint differs; remaining parts were not read');
    let extractionError = null, member = null;
    report.phase = 'remaining-mainmenu-parts';
    try { member = completeMember(fd, first, report); }
    catch (error) { extractionError = error; report.extractionError = { name: error.name, message: String(error.message).slice(0, 4096) }; }
    report.phase = 'sha256-after'; report.sha256After = hashArc(fd); report.statAfter = statSnapshot(liveFile(fd, TARGET, before));
    report.sha256Unchanged = report.sha256Before === report.sha256After;
    need(report.sha256Unchanged && report.sha256After === ARC_SHA256, 'Archive changed; no member artifact published');
    fs.closeSync(fd); fd = null;
    if (extractionError) { report.phase = 'remaining-mainmenu-parts'; throw extractionError; }
    report.phase = 'publish'; safePath(OUTPUT, false);
    fs.writeFileSync(path.win32.join(OUTPUT, 'mainmenu.map.bin'), member, { flag: 'wx' });
    report.outputFile = 'mainmenu.map.bin'; report.status = 'Succeeded'; report.phase = 'complete'; report.completedUtc = new Date().toISOString();
    fs.writeFileSync(path.win32.join(OUTPUT, 'inspection.json'), reportJson(report), { encoding: 'utf8', flag: 'wx' });
    console.log(`Completed only mainmenu/mainmenu.map: ${OUTPUT}; ARC Adler32 checked; Level/navigation interpretation remains separate`);
    return report;
  } catch (error) {
    if (fd !== null) { try { fs.closeSync(fd); } catch (closeError) { report.closeError = String(closeError.message).slice(0, 4096); } fd = null; }
    report.status = 'Failed'; report.completedUtc = new Date().toISOString();
    report.error = { name: error.name, message: String(error.message).slice(0, 4096), code: error.code || null };
    try { safePath(OUTPUT, false); fs.writeFileSync(path.win32.join(OUTPUT, 'inspection.json'), reportJson(report), { encoding: 'utf8', flag: 'wx' }); }
    catch (recordError) { console.error(`Could not publish failure evidence: ${String(recordError.message).slice(0, 4096)}`); }
    throw error;
  }
}
// Intentionally no main(), test runner, import-time call, fallback or subprocess.
