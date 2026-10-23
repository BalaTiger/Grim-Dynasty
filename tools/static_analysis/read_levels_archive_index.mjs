// Fixed-scope ARC directory inspection. PREPARATION ONLY: importing this module
// performs no I/O. A reviewed caller must explicitly call inspectLevelsArchive().
//
// Provenance: src/resource_codec/ResourceContainers.cs ArcArchive (lines86-95)
// and tools/resource_reader/gd_resources.py ARC (lines121-149): ARC v3 has a
// 28-byte seven-u32 header, a 12-byte part table, names, then44-byte entries.
// Historical validation in docs/compiled-containers/acceptance.json covers
// base Text_EN/Quests, NOT Levels.arc or its member payload formats. This reader
// deliberately separates metadata bounds from the old64MiB decoded-member cap;
// it NEVER decompresses a member or parses a part-table record. Full-file SHA256
// necessarily streams all bytes, including payload/part bytes, without decoding.
//
// Windows lstat/realpath/nlink checks reject observed links/junctions, alternate
// paths and hard links. Node fs does not expose every Windows reparse tag. The
// approved caller must separately preflight Get-Item Attributes for the fixed
// file and ancestors if all ReparsePoint kinds must be excluded. No subprocess,
// security change, native game module, save access or network is used here.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const PROJECT = 'C:\\Users\\zhuzi\\Documents\\Grim-Dynasty';
const TARGET = 'D:\\Steam\\steamapps\\common\\Grim Dawn\\resources\\Levels.arc';
const SOURCE = path.win32.join(PROJECT, 'tools', 'static_analysis', 'read_levels_archive_index.mjs');
const OUTPUT = path.win32.join(PROJECT, 'docs', 'reverse-client', 'r1-levels-archive-index');
const META_LIMIT = 16 * 1024 * 1024;
const JSON_LIMIT = 16 * 1024 * 1024;
const ENTRY_LIMIT = 100000;
const NAME_LIMIT = 1024;
const HASH_CHUNK = 1024 * 1024;

function requireCondition(condition, message) {
  if (!condition) throw new Error(message);
}
function samePath(a, b) {
  return path.win32.resolve(a).toLowerCase() === path.win32.resolve(b).toLowerCase();
}
function serialStat(s) {
  const keys = ['dev', 'ino', 'mode', 'nlink', 'size', 'mtimeNs', 'ctimeNs', 'birthtimeNs'];
  const result = {};
  for (const key of keys) result[key] = s[key].toString();
  return result;
}
function requireSameStat(expected, actual, label) {
  const a = serialStat(expected), b = serialStat(actual);
  for (const key of Object.keys(a)) requireCondition(a[key] === b[key], `${label}: ${key} changed`);
}
function checkedPathChain(value, leafKind) {
  const resolved = path.win32.resolve(value);
  requireCondition(resolved === value, 'Fixed path must already be absolute and normalized');
  const root = path.win32.parse(resolved).root;
  let current = root;
  const parts = resolved.slice(root.length).split('\\');
  const names = [root];
  for (const part of parts) { current = path.win32.join(current, part); names.push(current); }
  let result;
  for (let n = 0; n < names.length; n++) {
    const name = names[n];
    const s = fs.lstatSync(name, { bigint: true });
    requireCondition(!s.isSymbolicLink(), `Link/junction rejected: ${name}`);
    requireCondition(samePath(fs.realpathSync.native(name), name), `Redirected/canonical path differs: ${name}`);
    if (n === names.length - 1 && leafKind === 'file') {
      requireCondition(s.isFile(), `Target is not a regular file: ${name}`);
      requireCondition(s.nlink === 1n, 'Target must have exactly one observed hard link');
      requireCondition(s.ino !== 0n, 'Target file identity is unavailable');
    } else requireCondition(s.isDirectory(), `Expected a directory: ${name}`);
    result = s;
  }
  return result;
}
function checkedLiveTarget(fd, expected) {
  const linked = checkedPathChain(TARGET, 'file');
  const opened = fs.fstatSync(fd, { bigint: true });
  requireCondition(opened.isFile(), 'Opened descriptor is not a regular file');
  requireSameStat(expected, linked, 'Path identity/stat');
  requireSameStat(expected, opened, 'Descriptor identity/stat');
  return opened;
}
function span(offset, length, fileSize, label) {
  requireCondition(Number.isSafeInteger(offset) && Number.isSafeInteger(length) && offset >= 0 && length >= 0,
    `${label}: invalid integer extent`);
  requireCondition(offset <= fileSize && length <= fileSize - offset, `${label}: outside original file extent`);
}
function readExact(fd, offset, length, fileSize, label) {
  span(offset, length, fileSize, label);
  requireCondition(length <= META_LIMIT, `${label}: metadata read exceeds16MiB`);
  const data = Buffer.alloc(length);
  let used = 0;
  while (used < length) {
    const got = fs.readSync(fd, data, used, length - used, offset + used);
    requireCondition(got > 0, `${label}: unexpected EOF`);
    used += got;
  }
  return data;
}
function hashDescriptor(fd, fileSize) {
  const hash = crypto.createHash('sha256');
  const buffer = Buffer.allocUnsafe(HASH_CHUNK);
  let position = 0;
  while (position < fileSize) {
    const wanted = Math.min(buffer.length, fileSize - position);
    const got = fs.readSync(fd, buffer, 0, wanted, position);
    requireCondition(got > 0, 'Unexpected EOF during whole-file SHA256');
    hash.update(buffer.subarray(0, got));
    position += got;
  }
  return hash.digest('hex');
}
function boundedJson(value, label) {
  const text = JSON.stringify(value, null, 2) + '\n';
  requireCondition(Buffer.byteLength(text, 'utf8') <= JSON_LIMIT, `${label}: JSON exceeds16MiB`);
  return text;
}
function inspectDirectory(fd, fileSize, report) {
  const raw = readExact(fd, 0, 28, fileSize, 'header');
  const words = Array.from({ length: 7 }, (_, n) => raw.readUInt32LE(n * 4));
  report.headerRawWords = words;
  const [magic, version, entries, partCount, partBytes, nameBytes, partOffset] = words;
  requireCondition(magic === 0x435241 && version === 3, 'Unsupported ARC magic/version; payload format remains unknown');
  requireCondition(entries <= ENTRY_LIMIT, 'Entry count exceeds100000');
  requireCondition(partCount <= 4000000 && partBytes === partCount * 12, 'Part count/size inconsistent with observed ARC v3');
  const namesOffset = partOffset + partBytes;
  const recordsOffset = namesOffset + nameBytes;
  const recordBytes = entries * 44;
  const metadataBytes = 28 + nameBytes + recordBytes;
  requireCondition(partOffset >= 28, 'Part-table extent overlaps the ARC header');
  span(partOffset, partBytes, fileSize, 'part-table extent (not parsed)');
  span(namesOffset, nameBytes, fileSize, 'name table');
  span(recordsOffset, recordBytes, fileSize, 'entry table');
  requireCondition(metadataBytes <= META_LIMIT, 'Combined header/names/entries metadata exceeds16MiB');
  report.header = { magic: '0x435241', version, entries, partCount, partBytes, nameBytes,
    partOffset, namesOffset, recordsOffset, recordBytes, metadataBytes };
  report.structuralReadRanges = [{ offset: 0, bytes: 28, purpose: 'header' },
    { offset: namesOffset, bytes: nameBytes, purpose: 'names' },
    { offset: recordsOffset, bytes: recordBytes, purpose: '44-byte entry directory' }];
  const names = readExact(fd, namesOffset, nameBytes, fileSize, 'name table');
  const records = readExact(fd, recordsOffset, recordBytes, fileSize, 'entry table');
  const seen = new Set(), first = [], related = [], extensions = new Map();
  const all = [];
  let jsonBytes = Buffer.byteLength('{"schema":"grim-dynasty-arc-directory-v1","entries":[]}\n', 'utf8');
  let relatedCount = 0;
  for (let n = 0; n < entries; n++) {
    const base = n * 44;
    const type = records.readUInt32LE(base), offset = records.readUInt32LE(base + 4);
    const packed = records.readUInt32LE(base + 8), unpacked = records.readUInt32LE(base + 12);
    const checksum = records.readUInt32LE(base + 16), filetime = records.readBigUInt64LE(base + 20).toString();
    const parts = records.readUInt32LE(base + 28), firstPart = records.readUInt32LE(base + 32);
    const nameLength = records.readUInt32LE(base + 36), nameOffset = records.readUInt32LE(base + 40);
    requireCondition(nameLength <= NAME_LIMIT, `Entry${n}: name exceeds1024 bytes`);
    span(nameOffset, nameLength, names.length, `Entry${n} name`);
    requireCondition(firstPart + parts <= partCount, `Entry${n}: part-index extent exceeds directory declaration`);
    const nameData = names.subarray(nameOffset, nameOffset + nameLength);
    for (const byte of nameData) requireCondition(byte <= 127, `Entry${n}: unsupported non-ASCII identifier`);
    const name = nameData.toString('ascii').replace(/\0+$/, '');
    const key = name.replace(/\\/g, '/').toLowerCase();
    requireCondition(!seen.has(key), `Entry${n}: duplicate normalized name`);
    seen.add(key);
    const extension = path.posix.extname(key) || '(none)';
    extensions.set(extension, (extensions.get(extension) || 0) + 1);
    const entry = { ordinal: n, name, type, offset, packed, unpacked,
      checksum, filetime, parts, firstPart, nameOffset, nameLength };
    if (first.length < 32) first.push(entry);
    if (/map|nav|path|region/i.test(name)) { relatedCount++; if (related.length < 32) related.push(entry); }
    const json = JSON.stringify(entry);
    jsonBytes += Buffer.byteLength(json, 'utf8') + (n === 0 ? 0 : 1);
    requireCondition(jsonBytes <= JSON_LIMIT, 'Complete entries JSON would exceed16MiB; no partial directory published');
    all.push(json);
  }
  report.extensionCounts = Object.fromEntries([...extensions].sort((a, b) => a[0] < b[0] ? -1 : a[0] > b[0] ? 1 : 0));
  report.relatedNameMatchCount = relatedCount;
  report.sampleSelection = related.length ? 'first32 names matching map|nav|path|region, original directory order'
    : 'no related lexical matches; first32 directory entries';
  report.sampleEntries = related.length ? related : first;
  report.memberPayloadFormat = 'Unknown; names/extensions are selection hints only';
  const full = '{"schema":"grim-dynasty-arc-directory-v1","entries":[' + all.join(',') + ']}\n';
  requireCondition(Buffer.byteLength(full, 'utf8') === jsonBytes, 'Internal directory JSON byte-accounting mismatch');
  return full;
}

export function inspectLevelsArchive() {
  requireCondition(process.platform === 'win32', 'This fixed-path inspection is only for the reviewed Windows host');
  requireCondition(samePath(fileURLToPath(import.meta.url), SOURCE), 'Reader must be invoked from its reviewed fixed project path');
  checkedPathChain(path.win32.dirname(OUTPUT), 'directory');
  // Non-recursive mkdir is atomic against an existing output path. Never reuse
  // or erase old evidence, including a previously failed run's directory.
  fs.mkdirSync(OUTPUT, { recursive: false });
  const report = { schema: 'grim-dynasty-levels-arc-index-v1', status: 'InProgress',
    startedUtc: new Date().toISOString(), target: TARGET, outputDirectory: OUTPUT,
    provenance: ['src/resource_codec/ResourceContainers.cs:86-95', 'tools/resource_reader/gd_resources.py:121-149',
      'docs/compiled-containers/acceptance.json (historical Text_EN/Quests only)',
      'docs/host-migration/verification-summary.json (recorded host installation root)'],
    limits: { metadataBytes: META_LIMIT, entries: ENTRY_LIMIT, individualNameBytes: NAME_LIMIT, outputJsonBytes: JSON_LIMIT },
    boundaries: { originalFileAccess: 'read-only same descriptor', wholeFileBytesReadForHash: true,
      partTableParsed: false, payloadDecoded: false, mapOrNavigationFormatValidated: false,
      originalExecutableOrDllLoaded: false, savesAccessed: false,
      pathGuard: 'lstat each component, native realpath equality, regular file, nlink1, same descriptor/path identity',
      reparseTagCaveat: 'Node does not expose all Windows reparse tags; separate approved Attributes preflight is required' },
    sha256Before: null, sha256After: null, phase: 'output-directory-check' };
  let fd = null;
  try {
    checkedPathChain(OUTPUT, 'directory');
    report.phase = 'target-path-check';
    const initial = checkedPathChain(TARGET, 'file');
    requireCondition(initial.size <= BigInt(Number.MAX_SAFE_INTEGER), 'File size exceeds exact offset domain');
    const fileSize = Number(initial.size);
    report.statBefore = serialStat(initial);
    fd = fs.openSync(TARGET, fs.constants.O_RDONLY);
    checkedLiveTarget(fd, initial); // Before reading any byte from the descriptor.
    report.phase = 'sha256-before';
    report.sha256Before = hashDescriptor(fd, fileSize);
    checkedLiveTarget(fd, initial);
    let metadataError = null, directoryJson = null;
    report.phase = 'metadata';
    try { directoryJson = inspectDirectory(fd, fileSize, report); }
    catch (error) {
      metadataError = error;
      report.metadataError = { name: error.name, message: String(error.message).slice(0, 4096) };
    }
    report.phase = 'sha256-after';
    report.sha256After = hashDescriptor(fd, fileSize);
    report.statAfter = serialStat(checkedLiveTarget(fd, initial));
    report.sha256Unchanged = report.sha256Before === report.sha256After;
    requireCondition(report.sha256Unchanged, 'Original archive hash changed during inspection');
    fs.closeSync(fd); fd = null;
    if (metadataError) { report.phase = 'metadata'; throw metadataError; }
    report.phase = 'publish';
    checkedPathChain(OUTPUT, 'directory');
    fs.writeFileSync(path.win32.join(OUTPUT, 'entries.json'), directoryJson, { encoding: 'utf8', flag: 'wx' });
    report.directoryJsonBytes = Buffer.byteLength(directoryJson, 'utf8');
    report.directoryJsonSha256 = crypto.createHash('sha256').update(directoryJson, 'utf8').digest('hex');
    report.status = 'Succeeded'; report.completedUtc = new Date().toISOString(); report.phase = 'complete';
    fs.writeFileSync(path.win32.join(OUTPUT, 'inspection.json'), boundedJson(report, 'inspection'), { encoding: 'utf8', flag: 'wx' });
    console.log(`Wrote ARC directory evidence: ${OUTPUT}; entries=${report.header.entries}; member formats remain unknown`);
    return report;
  } catch (error) {
    if (fd !== null) { try { fs.closeSync(fd); } catch (closeError) { report.closeError = String(closeError.message).slice(0, 4096); } fd = null; }
    report.status = 'Failed'; report.completedUtc = new Date().toISOString();
    report.error = { name: error.name, message: String(error.message).slice(0, 4096), code: error.code || null };
    try {
      checkedPathChain(OUTPUT, 'directory');
      fs.writeFileSync(path.win32.join(OUTPUT, 'inspection.json'), boundedJson(report, 'failed inspection'), { encoding: 'utf8', flag: 'wx' });
    } catch (recordError) {
      // Do not overwrite partial evidence to make a failure look clean.
      console.error(`Could not publish failure evidence: ${String(recordError.message).slice(0, 4096)}`);
    }
    throw error;
  }
}
// Intentionally no main(), import-time call, auto-exec footer or command runner.
