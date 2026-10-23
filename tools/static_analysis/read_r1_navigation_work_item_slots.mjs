// Fixed navigation work-item vptr prefix: three candidate qwords only; no slot role or table extent assumed. No execution, backend, table scan or target-body read.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const self = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(path.basename(self), 'read_r1_navigation_work_item_slots.mjs');
assert.equal(process.argv.length, 2);
const hash = b => createHash('sha256').update(b).digest('hex');
function normal(p) {
  for (let q = path.resolve(p); ; q = path.dirname(q)) {
    assert(!fs.lstatSync(q).isSymbolicLink());
    assert.equal(fs.realpathSync.native(q).toLowerCase(), q.toLowerCase());
    if (path.dirname(q) === q) break;
  }
}
normal(self);
const source = 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const expected = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const proofs = [
 {file:'docs/reverse-client/r1-level-stream-and-navjob/11-LevelStreamNavJob-0x180109410-procedure_assembly.json',
  sha256:'35dffd832978a00824520053493f0a6bc7bc204940533048dd919f836fb0fe84',procedure:'0x180109410'}
];
const evidence = proofs.map(proof => {
 const file=path.join(root,proof.file);normal(file);assert(fs.statSync(file).isFile());assert(fs.statSync(file).size<=1024*1024);
 const bytes=fs.readFileSync(file);assert.equal(hash(bytes),proof.sha256);
 const e=JSON.parse(bytes).result.structuredContent;
 assert.equal(e.subject.name,'Engine.dll');assert.equal(e.subject.digest.sha256,expected);
 assert.equal(path.resolve(e.subject.local_path).toLowerCase(),path.resolve(source).toLowerCase());
 assert.equal(e.provider.id,'ghidra');assert.equal(e.operation,'procedure_assembly');assert.equal(e.parameters.procedure,proof.procedure);
 assert.equal(typeof e.normalized_result,'string');return e;
});
assert(evidence[0].normalized_result.includes('0x180109433: LEA RAX, [0x180340a58]\n0x18010943a: MOV qword ptr [RCX], RAX\n'));
const out = path.join(root, 'docs/reverse-client/r1-navigation-work-item-slots');
normal(path.dirname(out));
assert(!fs.existsSync(out));
normal(source);
const r = {
  started_utc: new Date().toISOString(), source, expected_sha256: expected, proofs,
  script_sha256: hash(fs.readFileSync(self)), metadata_bytes: 0, data_bytes: 0,
  code_bytes: 0, hash_bytes: 0, subprocesses: 0, original_executed: false,
  values: [], failures: [],
  scope: 'PE headers/section metadata plus exactly 24 data bytes at Engine RVA 0x340a58; whole-file reads only for before/after SHA256. Three qwords at offsets0/8/16 only; no target body,other slot or RTTI read; table extent and roles remain unknown.'
};
let fd, initial;
function digest() {
  const h = createHash('sha256'), b = Buffer.alloc(65536);
  for (let at = 0; at < initial.size; ) {
    const n = fs.readSync(fd, b, 0, Math.min(b.length, initial.size - at), at);
    assert(n > 0);
    h.update(b.subarray(0, n));
    at += n;
    r.hash_bytes += n;
  }
  return h.digest('hex');
}
function readMetadata(at, n) {
  assert(Number.isSafeInteger(at) && at >= 0 && n > 0 && n <= 512 && at + n <= initial.size);
  r.metadata_bytes += n;
  assert(r.metadata_bytes <= 4096);
  const b = Buffer.alloc(n);
  assert.equal(fs.readSync(fd, b, 0, n, at), n);
  return b;
}
try {
  fd = fs.openSync(source, 'r');
  initial = fs.fstatSync(fd);
  assert(initial.isFile());
  assert.equal(initial.size, 4485632);
  r.sha256_before = digest();
  assert.equal(r.sha256_before, expected);
  const dos = readMetadata(0, 64);
  assert.equal(dos.readUInt16LE(0), 0x5a4d);
  const pe = dos.readUInt32LE(60);
  assert(pe >= 64 && pe < 4096);
  const coff = readMetadata(pe, 24);
  assert.equal(coff.readUInt32LE(0), 0x4550);
  assert.equal(coff.readUInt16LE(4), 0x8664);
  assert.equal(coff.readUInt16LE(20), 240);
  const opt = readMetadata(pe + 24, 240);
  assert.equal(opt.readUInt16LE(0), 0x20b);
  const imageBase = opt.readBigUInt64LE(24), imageSize = opt.readUInt32LE(56);
  assert.equal(imageBase, 0x180000000n);
  const count = coff.readUInt16LE(6);
  assert(count > 0 && count <= 16);
  const sections = Array.from({ length: count }, (_, i) => {
    const b = readMetadata(pe + 264 + i * 40, 40);
    return {
      name: b.subarray(0, 8).toString('ascii').replace(/\0.*$/, ''),
      rva: b.readUInt32LE(12), size: b.readUInt32LE(16),
      offset: b.readUInt32LE(20), flags: b.readUInt32LE(36)
    };
  });
  const vtableRva = 0x340a58, slotOffset = 0, slotRva = vtableRva + slotOffset, n = 24;
  assert.equal(slotRva, 0x340a58);
  assert(slotRva + n <= imageSize);
  const matching = sections.filter(s => slotRva >= s.rva && slotRva + n <= s.rva + s.size);
  assert.equal(matching.length, 1);
  const section = matching[0];
  assert(!(section.flags & 0x20000000));
  assert(section.flags & 0x40000000);
  const offset = section.offset + slotRva - section.rva;
  assert(offset >= 0 && offset + n <= initial.size);
  const bytes = Buffer.alloc(n);
  assert.equal(fs.readSync(fd, bytes, 0, n, offset), n);
  r.data_bytes += n;
  assert.equal(r.data_bytes, 24);
  r.window = {vptr_rva:'0x340a58',vptr_va:'0x180340a58',rva:'0x340a58',file_offset:offset,bytes:n,
    raw_hex:bytes.toString('hex'),section:section.name,table_extent_verified:false};
  r.values = [];
  for(let index=0;index<3;index++){
    const relative=index*8, raw=bytes.subarray(relative,relative+8),targetVa=raw.readBigUInt64LE(0);
    const inside=targetVa>=imageBase&&targetVa<imageBase+BigInt(imageSize);
    const targetRva=inside?Number(targetVa-imageBase):null;
    const hits=inside?sections.filter(s=>targetRva>=s.rva&&targetRva<s.rva+s.size):[];
    const executable=hits.length===1&&!!(hits[0].flags&0x20000000)&&hits[0].offset+targetRva-hits[0].rva<initial.size;
    r.values.push({index,slot_offset:'0x'+relative.toString(16),rva:'0x'+(slotRva+relative).toString(16),
      file_offset:offset+relative,bytes:8,raw_hex:raw.toString('hex'),target_va:'0x'+targetVa.toString(16),
      target_rva:inside?'0x'+targetRva.toString(16):null,target_section:hits.length===1?hits[0].name:null,
      candidate_status:executable?'ExecutableAddressCandidate':'UnknownNotExecutableAddress',
      target_body_read:false,slot_role:'unknown',table_membership_verified:false});
  }
  r.limitations = [
    '109410 constructor directly writes vptr340A58. Only the following three qwords are read; complete vtable extent and membership of later entries are not proven.',
    'Executable-section addresses are candidates,not verified function boundaries or Execute/destructor roles. Unknown candidates are retained without fallback reads.',
    'No RTTI,adjacent entries,target bodies,thread implementation or worker dispatch is read. Actual publication semantics require separate evidence.',
    'Loaded table mutation,live receiver identity and completed construction remain unobserved.',
    'Before/after hashes and file metadata checks are not an atomic filesystem snapshot.'
  ];} catch (e) {
  r.failures.push(String(e));
} finally {
  if (fd !== undefined) {
    try {
      r.sha256_after = digest();
      assert.equal(r.sha256_after, expected);
      normal(source);
      for (const s of [fs.fstatSync(fd), fs.statSync(source)])
        for (const k of ['size', 'dev', 'ino', 'mtimeMs', 'ctimeMs']) assert.equal(s[k], initial[k]);
      r.stable = true;
    } catch (e) {
      r.failures.push(String(e));
    } finally {
      fs.closeSync(fd);
      r.source_closed = true;
    }
  }
}
r.finished_utc = new Date().toISOString();
r.status = r.failures.length ? 'failed' : 'passed';
normal(path.dirname(out));
assert(!fs.existsSync(out));
fs.mkdirSync(out);
normal(out);
fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(r, null, 2) + '\n', { flag: 'wx' });
console.log(JSON.stringify(r));
if (r.failures.length) process.exitCode = 1;
