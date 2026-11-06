// Decode the exact pdata-bounded body of ControllerPlayerStateIdle slot+0x220
// (0x165B30..0x165B9B, 107 bytes). Read-only: synthetic PE for dumpbin, the
// original is never handed to the tool and nothing is executed.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(self.toLowerCase(), path.join(root, 'tools/static_analysis/read_r1_idle_entry_165b30.mjs').toLowerCase());

const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll';
const originalSize = 11385856;
const originalHash = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const slotsFile = 'docs/reverse-client/r1-player-state-slots/inspection.json';
const out = path.join(root, 'docs/reverse-client/r1-idle-entry-165b30');
const temp = path.join(root, '.tooling/r1-idle-entry-165b30-temp');
const base = 0x180000000n;
const RVA = 0x165b30, LENGTH = 107;
const hash = b => createHash('sha256').update(b).digest('hex'), hex = n => '0x' + n.toString(16);
function normal(p, file = false) {
  let first = true;
  for (let c = path.resolve(p);;) {
    const s = fs.lstatSync(c); assert(!s.isSymbolicLink(), 'Redirected path refused');
    assert(first && file ? s.isFile() : s.isDirectory());
    assert.equal(fs.realpathSync.native(c).toLowerCase(), c.toLowerCase(), 'Redirected ancestor refused');
    const parent = path.dirname(c); if (parent === c) return; c = parent; first = false;
  }
}
function absent(p) { try { fs.lstatSync(p); } catch (e) { if (e.code === 'ENOENT') return; throw e; } assert.fail('Existing output preserved: ' + p); }

normal(self, true); normal(path.dirname(out)); normal(path.dirname(temp)); absent(out); absent(temp);

const slotsPath = path.join(root, slotsFile); normal(slotsPath, true);
const slotsBytes = fs.readFileSync(slotsPath);
const slots = JSON.parse(slotsBytes.toString('utf8'));
assert.equal(slots.status, 'passed');
// The saved vtable read must already contain this exact slot and boundary.
const flat = JSON.stringify(slots);
assert.ok(flat.includes('"0x220"'), 'slot 0x220 missing from saved evidence');
assert.ok(flat.includes('0x180165b30'), 'idle entry target missing from saved evidence');
assert.ok(flat.includes('0x165b9b') || flat.includes('0x165B9B'), 'pdata end missing from saved evidence');

const report = {
  schema: 'grim-dynasty-r1-idle-entry-165b30-v1', status: 'started',
  started_utc: new Date().toISOString(), original, expected_sha256: originalHash,
  source_evidence: {file: slotsFile, sha256: hash(slotsBytes)},
  window: {rva: hex(RVA), length: LENGTH, end_exclusive: hex(RVA + LENGTH),
           kind: 'exact-pdata-bounded-function'},
  tool, expected_tool_sha256: toolHash, original_executed: false,
  original_given_to_dumpbin: false, subprocesses: 0, code_bytes: 0, failures: [],
  limitations: [
    'The window is exactly the saved .pdata fragment for slot+0x220; a pdata fragment is not a proof of logical completeness.',
    'No call target, vtable slot or neighbouring function is followed.',
    'A disk decode does not establish runtime behaviour or native ABI semantics.',
  ],
};
let fd, outCreated = false;
try {
  fs.mkdirSync(out); outCreated = true; fs.mkdirSync(temp); normal(out); normal(temp);
  report.tool_before = {sha256: hash(fs.readFileSync(tool))}; assert.equal(report.tool_before.sha256, toolHash);
  normal(original, true); fd = fs.openSync(original, fs.constants.O_RDONLY);
  assert.equal(fs.fstatSync(fd).size, originalSize);
  const head = Buffer.alloc(0x400); assert.equal(fs.readSync(fd, head, 0, 0x400, 0), 0x400);
  const pe = head.readUInt32LE(60), coff = Buffer.alloc(0x200);
  assert.equal(fs.readSync(fd, coff, 0, 0x200, pe), 0x200);
  assert.equal(coff.readBigUInt64LE(48), base);
  const nsec = coff.readUInt16LE(6), secs = [];
  for (let i = 0; i < nsec; i++) {
    const b = Buffer.alloc(40); assert.equal(fs.readSync(fd, b, 0, 40, pe + 264 + i * 40), 40);
    secs.push({name:b.subarray(0,8).toString('ascii').replace(/\0.*$/,''), rva:b.readUInt32LE(12),
               vsize:b.readUInt32LE(16), raw:b.readUInt32LE(20), flags:b.readUInt32LE(36)});
  }
  const text = secs.find(s => s.name === '.text'); assert(text && (text.flags & 0x20000000));
  assert(RVA >= text.rva && RVA + LENGTH <= text.rva + text.vsize);
  const offset = text.raw + RVA - text.rva;
  const code = Buffer.alloc(LENGTH); assert.equal(fs.readSync(fd, code, 0, LENGTH, offset), LENGTH);
  report.code_bytes = LENGTH; report.code_sha256 = hash(code);
  fs.writeFileSync(path.join(out, 'window.bin'), code, {flag:'wx'});

  const sectionRva = Math.floor(RVA / 0x1000) * 0x1000, prefix = RVA - sectionRva;
  const virtualSize = prefix + LENGTH, rawSize = Math.ceil(virtualSize / 0x200) * 0x200, rawOffset = 0x200;
  const b = Buffer.alloc(rawOffset + rawSize), co = 0x84, opt = 0x98, sh = opt + 0xf0;
  b.writeUInt16LE(0x5a4d, 0); b.writeUInt32LE(0x80, 0x3c); b.writeUInt32LE(0x4550, 0x80);
  b.writeUInt16LE(0x8664, co); b.writeUInt16LE(1, co + 2); b.writeUInt16LE(0xf0, co + 16); b.writeUInt16LE(0x2022, co + 18);
  b.writeUInt16LE(0x20b, opt); b.writeUInt32LE(rawSize, opt + 4); b.writeUInt32LE(sectionRva, opt + 20);
  b.writeBigUInt64LE(base, opt + 24); b.writeUInt32LE(0x1000, opt + 32); b.writeUInt32LE(0x200, opt + 36);
  b.writeUInt16LE(6, opt + 40); b.writeUInt16LE(6, opt + 48);
  b.writeUInt32LE(Math.ceil((sectionRva + virtualSize) / 0x1000) * 0x1000, opt + 56);
  b.writeUInt32LE(rawOffset, opt + 60); b.writeUInt16LE(3, opt + 68); b.writeUInt32LE(16, opt + 108);
  b.write('.text', sh, 'ascii'); b.writeUInt32LE(virtualSize, sh + 8); b.writeUInt32LE(sectionRva, sh + 12);
  b.writeUInt32LE(rawSize, sh + 16); b.writeUInt32LE(rawOffset, sh + 20); b.writeUInt32LE(0x60000020, sh + 36);
  code.copy(b, rawOffset + prefix);
  const tiny = path.join(temp, '165b30.inspect.dll'); fs.writeFileSync(tiny, b, {flag:'wx'});

  const start = base + BigInt(RVA), end = start + BigInt(LENGTH);
  const args = ['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:' + hex(start) + ',' + hex(end - 1n), tiny];
  assert(!args.includes(original)); report.subprocesses++;
  const child = spawnSync(tool, args, {cwd:temp, windowsHide:true, encoding:'utf8', timeout:10000,
    maxBuffer:1 << 18, env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
  fs.writeFileSync(path.join(out, 'dumpbin.txt'), child.stdout || '', {flag:'wx'});
  fs.writeFileSync(path.join(out, 'stderr.txt'), child.stderr || '', {flag:'wx'});
  assert.equal(child.error, undefined); assert.equal(child.status, 0); assert.equal(child.signal, null);

  const rows = [];
  for (const line of (child.stdout || '').split(/\r?\n/)) {
    const m = /^\s+([0-9A-F]{16}):\s+((?:[0-9A-F]{2}(?:\s+|$))+)(.*)$/.exec(line);
    if (m) { rows.push({address:hex(BigInt('0x' + m[1])), bytes:m[2].replace(/\s/g,'').toLowerCase(),
                        instruction:m[3].trim()}); continue; }
    const c = /^\s{10,}((?:[0-9A-F]{2}\s*)+)$/.exec(line);
    if (c && rows.length) rows[rows.length - 1].bytes += c[1].replace(/\s/g,'').toLowerCase();
  }
  let cursor = start;
  for (const row of rows) { assert.equal(row.address, hex(cursor)); cursor += BigInt(row.bytes.length / 2); }
  assert.equal(cursor, end, 'row coverage must be exactly the window');
  assert.equal(rows.map(r => r.bytes).join(''), code.toString('hex'));
  report.rows = rows;
  report.control_transfers = rows.filter(r => /^(?:call|j[a-z]+|ret|int\d+|hlt|ud2)\b/i.test(r.instruction))
    .map(r => ({address:r.address, instruction:r.instruction}));
  report.status = 'decoded-exact-pdata-window-manual-review-required';
} catch (e) { report.failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) { try { fs.closeSync(fd); report.source_closed = true; } catch (e) { report.failures.push('close: ' + String(e)); } }
  try { report.tool_after = {sha256: hash(fs.readFileSync(tool))}; assert.equal(report.tool_after.sha256, toolHash); }
  catch (e) { report.failures.push('tool: ' + String(e)); }
  if (report.failures.length) report.status = 'failed';
  report.finished_utc = new Date().toISOString();
  if (outCreated) fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(report, null, 2) + '\n', {flag:'wx'});
}
console.log(JSON.stringify({status:report.status, rows:(report.rows||[]).length,
  transfers:(report.control_transfers||[]).length, failures:report.failures}));
if (report.failures.length) process.exitCode = 1;
