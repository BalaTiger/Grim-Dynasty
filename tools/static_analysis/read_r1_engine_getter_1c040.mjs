// Decode one fixed disk code window: ControllerPlayerStateMoveTo vtable+0x70
// (= 0x1663F0), the EndOfPathReached target for the click-move state.
// Window only: no callee following, no original execution, synthetic PE only.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(self.toLowerCase(), path.join(root, 'tools/static_analysis/read_r1_engine_getter_1c040.mjs').toLowerCase());
const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll', originalSize = 11385856;
const originalHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';

const report = {schema:'grim-dynasty-r1-moveto-endofpath-v1', status:'started', started_utc:new Date().toISOString(),
  original, expected_sha256:originalHash, source_evidence:{file:evidenceFile, sha256:hash(fs.readFileSync(evidencePath))},
  chain:{state:'ControllerPlayerStateMoveTo', vtable_rva:moveto.vtable_rva, slot:'0x70', target_rva:moveto.slot70_rva},
  limits, tool, expected_tool_sha256:toolHash, original_executed:false, original_given_to_dumpbin:false, subprocesses:0,
  code_bytes:0, failures:[],
  limitations:[
    'Only a fixed 256-byte disk window at 0x1663F0 is decoded; it is not asserted to be a complete function.',
    'No call target, vtable slot or neighboring function is followed.',
    'A disk decode does not establish runtime behavior, loaded vtable contents or native ABI semantics.',
  ]};
let fd, outCreated = false;
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.tool_before = fileFingerprint(tool); assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY);
  const st = fs.fstatSync(fd); assert.equal(st.size, originalSize);
  const head = Buffer.alloc(0x400); assert.equal(fs.readSync(fd, head, 0, 0x400, 0), 0x400);
  const pe = head.readUInt32LE(60); const coff = Buffer.alloc(0x200);
  assert.equal(fs.readSync(fd, coff, 0, 0x200, pe), 0x200);
  assert.equal(coff.readBigUInt64LE(48), base);
  const nsec = coff.readUInt16LE(6), secs = [];
  for (let i = 0; i < nsec; i++) { const b = Buffer.alloc(40); assert.equal(fs.readSync(fd, b, 0, 40, pe + 264 + i * 40), 40);
    secs.push({name:b.subarray(0, 8).toString('ascii').replace(/\0.*$/,''), rva:b.readUInt32LE(12), vsize:b.readUInt32LE(16), raw:b.readUInt32LE(20), flags:b.readUInt32LE(36)}); }
  const text = secs.find(s => s.name === '.text'); assert(text && (text.flags & 0x20000000));
  assert(window.rva >= text.rva && window.rva + window.length <= text.rva + text.vsize);
  window.offset = text.raw + window.rva - text.rva;
  const code = Buffer.alloc(window.length); assert.equal(fs.readSync(fd, code, 0, window.length, window.offset), window.length);
  report.code_bytes = window.length;
  const sectionRva = Math.floor(window.rva / 0x1000) * 0x1000, prefix = window.rva - sectionRva;
  const virtualSize = prefix + window.length, rawSize = Math.ceil(virtualSize / 0x200) * 0x200, rawOffset = 0x200;
  const b = Buffer.alloc(rawOffset + rawSize); assert(b.length <= limits.tiny_pe_bytes);
  const co = 0x84, opt = 0x98, sh = opt + 0xf0;
  b.writeUInt16LE(0x5a4d, 0); b.writeUInt32LE(0x80, 0x3c); b.writeUInt32LE(0x4550, 0x80);
  b.writeUInt16LE(0x8664, co); b.writeUInt16LE(1, co + 2); b.writeUInt16LE(0xf0, co + 16); b.writeUInt16LE(0x2022, co + 18);
  b.writeUInt16LE(0x20b, opt); b.writeUInt32LE(rawSize, opt + 4); b.writeUInt32LE(sectionRva, opt + 20);
  b.writeBigUInt64LE(base, opt + 24); b.writeUInt32LE(0x1000, opt + 32); b.writeUInt32LE(0x200, opt + 36);
  b.writeUInt16LE(6, opt + 40); b.writeUInt16LE(6, opt + 48); b.writeUInt32LE(Math.ceil((sectionRva + virtualSize) / 0x1000) * 0x1000, opt + 56);
  b.writeUInt32LE(rawOffset, opt + 60); b.writeUInt16LE(3, opt + 68); b.writeUInt32LE(16, opt + 108);
  b.write('.text', sh, 'ascii'); b.writeUInt32LE(virtualSize, sh + 8); b.writeUInt32LE(sectionRva, sh + 12);
  b.writeUInt32LE(rawSize, sh + 16); b.writeUInt32LE(rawOffset, sh + 20); b.writeUInt32LE(0x60000020, sh + 36);
  code.copy(b, rawOffset + prefix);
  const tiny = path.join(temp, window.id + '.inspect.dll'); fs.writeFileSync(tiny, b, {flag:'wx'}); normal(tiny, true);
  const start = base + BigInt(window.rva), end = start + BigInt(window.length);
  const args = ['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:' + hex(start) + ',' + hex(end - 1n), tiny];
  assert(!args.includes(original)); report.subprocesses++;
  const child = spawnSync(tool, args, {cwd:temp, windowsHide:true, encoding:'utf8', timeout:limits.timeout_ms, maxBuffer:limits.output_bytes,
    env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
  fs.writeFileSync(path.join(out, 'dumpbin.txt'), child.stdout || '', {flag:'wx'});
  fs.writeFileSync(path.join(out, 'stderr.txt'), child.stderr || '', {flag:'wx'});
  assert.equal(child.error, undefined); assert.equal(child.status, 0); assert.equal(child.signal, null);
  const rows = [];
  for (const line of (child.stdout || '').split(/\r?\n/)) {
    const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if (m) { rows.push({address:hex(BigInt('0x' + m[1])), bytes:m[2].replace(/\s/g,'').toLowerCase(), instruction:m[3].trim()}); continue; }
    const c = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
    if (c && rows.length) rows[rows.length - 1].bytes += c[1].replace(/\s/g,'').toLowerCase();
  }
  let cursor = start;
  for (const row of rows) { assert.equal(row.address, hex(cursor)); cursor += BigInt(row.bytes.length / 2); assert(cursor <= end); }
  assert.equal(cursor, end);
  assert.equal(rows.map(r => r.bytes).join(''), code.toString('hex'));
  report.rows = rows;
  report.control_transfers = rows.filter(r => /^(?:call|j[a-z]+|ret|int\d+|hlt|ud2)\b/i.test(r.instruction)).map(r => ({address:r.address, instruction:r.instruction}));
  report.code_sha256 = hash(code);
  report.status = 'decoded-window-manual-review-required';
} catch (e) { report.failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) { try { fs.closeSync(fd); report.source_closed = true; } catch (e) { report.failures.push('close: ' + String(e)); } }
  try { report.tool_after = fileFingerprint(tool); assert.equal(report.tool_after.sha256, toolHash); } catch (e) { report.failures.push('tool: ' + String(e)); }
  if (report.failures.length) report.status = 'failed';
  report.finished_utc = new Date().toISOString();
  if (outCreated) fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(report, null, 2) + '\n', {flag:'wx'});
}
console.log(JSON.stringify({status:report.status, rows:(report.rows||[]).length, transfers:(report.control_transfers||[]).length, failures:report.failures}));
if (report.failures.length) process.exitCode = 1;
