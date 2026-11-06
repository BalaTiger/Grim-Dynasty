// Plan-driven disk code-window decoder.
//
// Reads .tooling/r1-code-window/plan.json:
//   { "out": "docs/reverse-client/<dir>",
//     "windows": [ { "id": "...", "rva": "0x...", "length": 123 } ] }
// Each window is decoded by dumpbin from a synthetic PE built in a temp dir.
// Read-only: the original artifact is opened O_RDONLY and never handed to the
// tool; nothing is executed; no callee is followed.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

assert.equal(process.argv.length, 2, 'No arguments accepted');
const self = fileURLToPath(import.meta.url), root = path.resolve(path.dirname(self), '../..');
assert.equal(root.toLowerCase(), 'c:\\users\\zhuzi\\documents\\grim-dynasty');
assert.equal(self.toLowerCase(), path.join(root, 'tools/static_analysis/read_r1_presentation_windows.mjs').toLowerCase());

const original = 'D:/Steam/steamapps/common/Grim Dawn/x64/Direct3D11.dll';
const originalSize = 141312;
const originalHash = '862a9ef1e67617c29122dc8b8b9c25dbbafe2e518ed98c0bb52b24b37d75ab5b';
const tool = 'D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe';
const toolHash = 'cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815';
const planPath = path.join(root, '.tooling/r1-presentation-window/plan.json');
const temp = path.join(root, '.tooling/r1-presentation-window-temp');
const base = 0x180000000n;
const MAX_WINDOWS = 6, MAX_LENGTH = 512;
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

normal(self, true); normal(planPath, true);
const planBytes = fs.readFileSync(planPath);
const plan = JSON.parse(planBytes.toString('utf8'));
assert.ok(plan.out && Array.isArray(plan.windows) && plan.windows.length > 0);
assert.ok(plan.windows.length <= MAX_WINDOWS, 'too many windows');
const out = path.join(root, plan.out);
normal(path.dirname(out)); absent(out); absent(temp);

const report = {schema:'grim-dynasty-r1-presentation-windows-v1', status:'started', started_utc:new Date().toISOString(),
  plan:{file:'.tooling/r1-presentation-window/plan.json', sha256:hash(planBytes)}, original, expected_sha256:originalHash,
  tool, expected_tool_sha256:toolHash, original_executed:false, original_given_to_dumpbin:false,
  subprocesses:0, code_bytes:0, windows:[], failures:[],
  limitations:['Only the planned fixed windows are decoded; a window is not asserted to be a complete function unless its id says pdata.',
    'No call target, vtable slot or neighbouring function is followed.','A disk decode does not establish runtime behaviour.']};

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
  for (const w of plan.windows) {
    const rva = typeof w.rva === 'string' ? Number(w.rva) : w.rva;
    const length = Number(w.length);
    assert.ok(Number.isInteger(rva) && rva > 0, 'bad rva');
    assert.ok(Number.isInteger(length) && length > 0 && length <= MAX_LENGTH, 'bad length');
    assert.ok(rva >= text.rva && rva + length <= text.rva + text.vsize, 'window outside .text');
    const offset = text.raw + rva - text.rva;
    const code = Buffer.alloc(length); assert.equal(fs.readSync(fd, code, 0, length, offset), length);
    report.code_bytes += length;
    const sectionRva = Math.floor(rva / 0x1000) * 0x1000, prefix = rva - sectionRva;
    const virtualSize = prefix + length, rawSize = Math.ceil(virtualSize / 0x200) * 0x200, rawOffset = 0x200;
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
    const tiny = path.join(temp, String(w.id) + '.inspect.dll'); fs.writeFileSync(tiny, b, {flag:'wx'});
    const start = base + BigInt(rva), end = start + BigInt(length);
    const args = ['/NOLOGO','/NOPDB','/ERRORREPORT:NONE','/DISASM:BYTES','/RANGE:' + hex(start) + ',' + hex(end - 1n), tiny];
    assert(!args.includes(original)); report.subprocesses++;
    const child = spawnSync(tool, args, {cwd:temp, windowsHide:true, encoding:'utf8', timeout:10000,
      maxBuffer:1 << 18, env:{SystemRoot:'C:\\Windows', WINDIR:'C:\\Windows', PATH:path.dirname(tool), TEMP:temp, TMP:temp}});
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
    assert.equal(cursor, end, 'row coverage must be exactly the window for ' + w.id);
    assert.equal(rows.map(r => r.bytes).join(''), code.toString('hex'));
    report.windows.push({id:String(w.id), rva:hex(rva), length, end_exclusive:hex(rva + length),
      code_sha256:hash(code), rows,
      control_transfers:rows.filter(r => /^(?:call|j[a-z]+|ret|int\d+|hlt|ud2)\b/i.test(r.instruction))
        .map(r => ({address:r.address, instruction:r.instruction}))});
  }
  report.status = 'decoded-planned-windows-manual-review-required';
} catch (e) { report.failures.push(String(e)); report.status = 'failed'; }
finally {
  if (fd !== undefined) { try { fs.closeSync(fd); report.source_closed = true; } catch (e) { report.failures.push('close: ' + String(e)); } }
  try { report.tool_after = {sha256: hash(fs.readFileSync(tool))}; assert.equal(report.tool_after.sha256, toolHash); }
  catch (e) { report.failures.push('tool: ' + String(e)); }
  if (report.failures.length) report.status = 'failed';
  report.finished_utc = new Date().toISOString();
  if (outCreated) fs.writeFileSync(path.join(out, 'inspection.json'), JSON.stringify(report, null, 2) + '\n', {flag:'wx'});
}
console.log(JSON.stringify({status:report.status, windows:report.windows.map(w => ({id:w.id, rows:w.rows.length})),
  subprocesses:report.subprocesses, failures:report.failures}));
if (report.failures.length) process.exitCode = 1;
