// PREPARED ONLY: one fixed 16-byte Engine datum; execution is separately reviewed.
// No target code/header reads, scan, DLL loading, child, build or test.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const script = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(script), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty', 'Unexpected project root');
const target = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expectedHash = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const expectedSize = 4485632, imageBase = 0x180000000n, targetRva = 0x34c3b0, dataBytes = 16;
const outputDirectory = path.join(root, 'docs/object-lifecycle/offline-actor-continuation-constant');
const output = path.join(outputDirectory, 'metadata.json');
const limits = Object.freeze({semantic_bytes:16, semantic_read_operations:1,
  fingerprint_chunk_bytes:65536, saved_proof_bytes_each:32768, report_bytes:32768});
const sources = Object.freeze([
  {path:'docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json',
    sha256:'39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b'},
  {path:'docs/object-lifecycle/offline-actor-constructor-body/0259a0-instructions.json',
    sha256:'59f402f428e04e2928bb485e58fa85896ac08db4548bff6fa33444a58fa521a7'},
]);
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const hex = value => '0x' + value.toString(16);
function normalPath(name, directory) {
  let cursor = path.resolve(name), first = true;
  while (true) {
    const stat = fs.lstatSync(cursor);
    assert.ok(!stat.isSymbolicLink(), 'Refuse redirected project paths');
    assert.ok(first && !directory ? stat.isFile() : stat.isDirectory(), 'Unexpected path type');
    assert.equal(fs.realpathSync(cursor).toLowerCase(), cursor.toLowerCase(), 'Path redirected');
    const parent = path.dirname(cursor);
    if (parent === cursor) return;
    cursor = parent; first = false;
  }
}
function absent(name) {
  try { fs.lstatSync(name); }
  catch (error) { if (error.code === 'ENOENT') return; throw error; }
  assert.fail('Preserve existing evidence directory');
}
normalPath(path.dirname(outputDirectory), true);
absent(outputDirectory);
const proofs = sources.map(source => {
  const file = path.join(root, source.path);
  normalPath(file, false);
  assert.ok(fs.statSync(file).size <= limits.saved_proof_bytes_each, 'Saved proof exceeds budget');
  const bytes = fs.readFileSync(file);
  assert.ok(bytes.length <= limits.saved_proof_bytes_each, 'Saved proof grew beyond budget');
  assert.equal(hash(bytes), source.sha256, 'Pinned project proof changed');
  return JSON.parse(bytes.toString('utf8'));
});
const boundary = proofs[0], pe = boundary.pe;
assert.equal(boundary.status, 'exact-actor-export-and-boundary-verified');
assert.equal(boundary.original, target); assert.equal(boundary.expected_sha256, expectedHash);
assert.equal(boundary.sha256_before, expectedHash); assert.equal(boundary.sha256_after, expectedHash);
assert.ok(boundary.source_hash_preserved && boundary.source_stat_stable);
assert.equal(pe.image_base, hex(imageBase));
const matches = proofs[1].filter(entry => entry.address === '0x180025ca2');
assert.equal(matches.length, 1, 'Missing exact saved MOVUPS');
const instruction = matches[0];
assert.equal(instruction.bytes, '0f100507673200');
assert.equal(instruction.instruction, 'movups      xmm0,xmmword ptr [000000018034C3B0h]');
const encoded = Buffer.from(instruction.bytes, 'hex');
assert.equal(encoded.length, 7); assert.equal(encoded.subarray(0,3).toString('hex'), '0f1005');
assert.equal(BigInt(instruction.address)+7n+BigInt(encoded.readInt32LE(3)), imageBase+BigInt(targetRva));
const fits = (offset, length, size) => Number.isSafeInteger(offset) && offset >= 0 &&
  Number.isSafeInteger(length) && length > 0 && Number.isSafeInteger(size) &&
  offset <= size && length <= size-offset;
assert.ok(fits(targetRva, dataBytes, pe.image_size));
const sections = pe.sections.filter(section => targetRva >= section.rva &&
  fits(targetRva-section.rva, dataBytes, section.raw_size));
assert.equal(sections.length, 1, 'Require unique file-backed mapping');
const section = sections[0];
assert.equal(section.name, '.rdata');
assert.equal(section.characteristics & 0x20000000, 0, 'No executable-section datum');
assert.equal(section.characteristics & 0x80000000, 0, 'Expected nonwritable file section');
assert.ok(section.characteristics & 0x40000000, 'Expected readable file section');
const fileOffset = section.raw_offset+targetRva-section.rva;
assert.ok(fits(fileOffset,dataBytes,expectedSize));
const report = {
  schema:'grim-dynasty-fixed-actor-continuation-constant-v1', status:'started',
  started_utc:new Date().toISOString(), script_sha256:hash(fs.readFileSync(script)),
  original:target, expected_sha256:expectedHash, expected_file_bytes:expectedSize,
  source_evidence:sources, source_instruction:instruction, limits,
  requested:{rva:hex(targetRva),end_rva_exclusive:hex(targetRva+dataBytes),
    preferred_va:hex(imageBase+BigInt(targetRva)),bytes:dataBytes,file_offset:fileOffset,
    section:section.name,section_characteristics:hex(section.characteristics)},
  target_open_mode:'O_RDONLY', target_loaded_or_executed:false, backend_started:false,
  subprocesses:0, dumpbin_started:false, build_or_test_execution:false,
  metadata_bytes:0, code_bytes:0, data_bytes:0, reads:[], fingerprint_passes:[],
  runtime_value_observed:false, original_modified:false, full_target_copy_created:false,
  limitations:[
    'Only the 16 file bytes at RVA [34C3B0,34C3C0) are semantically read; PE sections and the exact MOVUPS operand are reused from hash-pinned project evidence.',
    'The two unsigned little-endian qwords are raw bit views, not inferred field types, floating-point values or runtime addresses.',
    'File data and preferred addresses do not establish loaded runtime state or prove that this instruction has executed. No 34BB44 datum, global, import, callee or adjacent bytes are read.',
    'Two full-file fingerprint passes traverse all bytes without code interpretation using one 64KiB buffer. They are separate from the 16-byte semantic read.',
    'O_RDONLY and same-handle hash/stat checks do not create an atomic snapshot or enforce an exclusive Windows FileShare.Read contract.',
  ],
};
const buffer = Buffer.alloc(limits.fingerprint_chunk_bytes);
let fd, initialStat, admitted = false, closed = false, failure;
function fingerprint(label) {
  assert.equal(fs.fstatSync(fd).size, expectedSize, 'Source size changed');
  const digest = createHash('sha256'); let at = 0;
  while (at < expectedSize) {
    const count = fs.readSync(fd,buffer,0,Math.min(buffer.length,expectedSize-at),at);
    assert.ok(count > 0, 'Unexpected fingerprint EOF');
    digest.update(buffer.subarray(0,count)); at += count;
  }
  const pass = {label,bytes:at,chunk_bytes:buffer.length,sha256:digest.digest('hex')};
  report.fingerprint_passes.push(pass); return pass.sha256;
}
try {
  assert.equal(fs.realpathSync(target).toLowerCase(),path.resolve(target).toLowerCase(),'Fixed original path redirected');
  fd = fs.openSync(target,fs.constants.O_RDONLY); initialStat = fs.fstatSync(fd);
  assert.ok(initialStat.isFile()); assert.equal(initialStat.size,expectedSize); admitted = true;
  report.stat_before = {size:initialStat.size,dev:initialStat.dev,ino:initialStat.ino,
    mtime_ms:initialStat.mtimeMs,ctime_ms:initialStat.ctimeMs};
  report.sha256_before = fingerprint('before');
  assert.equal(report.sha256_before,expectedHash,'No data read after hash mismatch');
  const bytes = Buffer.alloc(dataBytes);
  const read = {kind:'fixed-data',rva:hex(targetRva),file_offset:fileOffset,requested_bytes:dataBytes,read_bytes:0,raw_hex:''};
  report.reads.push(read);
  const count = fs.readSync(fd,bytes,0,dataBytes,fileOffset);
  read.read_bytes = count; read.raw_hex = bytes.subarray(0,count).toString('hex'); report.data_bytes = count;
  assert.equal(count,dataBytes,'Short fixed data read; no retry or expansion');
  report.raw_hex = bytes.toString('hex');
  report.little_endian_qwords = [0,8].map(offset => ({
    byte_offset:offset,raw_hex:bytes.subarray(offset,offset+8).toString('hex'),
    unsigned_hex:'0x'+bytes.readBigUInt64LE(offset).toString(16).padStart(16,'0'),
  }));
  report.status = 'fixed-sixteen-byte-file-constant-read';
} catch (error) {failure=error;report.status='failed';report.failure=String(error);}
finally {
  if (fd !== undefined) {
    try {
      if (admitted) {
        report.sha256_after=fingerprint('after'); const after=fs.fstatSync(fd);
        report.stat_after={size:after.size,dev:after.dev,ino:after.ino,mtime_ms:after.mtimeMs,ctime_ms:after.ctimeMs};
        report.source_stat_stable=after.isFile() && after.size===initialStat.size && after.dev===initialStat.dev &&
          after.ino===initialStat.ino && after.mtimeMs===initialStat.mtimeMs && after.ctimeMs===initialStat.ctimeMs;
        report.source_hash_preserved=report.sha256_before===expectedHash && report.sha256_after===expectedHash;
        assert.ok(report.source_stat_stable && report.source_hash_preserved,'Source stat/hash stability failed');
      }
    } catch (error) {failure??=error;report.status='failed';report.after_failure=String(error);}
    try {fs.closeSync(fd);closed=true;}
    catch (error) {failure??=error;report.status='failed';report.close_failure=String(error);}
  } else closed=true;
}
assert.ok(closed,'Source handle close unconfirmed; no output directory created');
report.source_handle_closed=true;report.finished_utc=new Date().toISOString();
const encodedReport=JSON.stringify(report,null,2)+'\n';
assert.ok(Buffer.byteLength(encodedReport,'utf8')<=limits.report_bytes,'Report size limit');
normalPath(path.dirname(outputDirectory),true);absent(outputDirectory);
fs.mkdirSync(outputDirectory);normalPath(outputDirectory,true);
fs.writeFileSync(output,encodedReport,{flag:'wx'});
console.log(JSON.stringify({status:report.status,output,data_bytes:report.data_bytes,
  raw_hex:report.raw_hex,little_endian_qwords:report.little_endian_qwords,source_hash_preserved:report.source_hash_preserved}));
if (failure) process.exitCode=1;
