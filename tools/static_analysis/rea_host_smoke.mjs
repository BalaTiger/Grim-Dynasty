import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {createHash} from 'node:crypto';

// Bounded, read-only target inspection. Never launches the inspected binaries.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
if (process.argv.length > 3 || (process.argv[2] && process.argv[2] !== '--project-launcher')) throw Error('Only --project-launcher is supported');
const projectLauncher = process.argv[2] === '--project-launcher';
const output = path.join(root, 'docs/host-migration', projectLauncher ? 'rea-project-smoke' : 'rea-smoke');
const runtime = path.join(root, '.tooling/rea-runtime');
const launcher = path.join(root, '.tooling/rea/node_modules/rea-agents/scripts/rea.mjs');
const targets = [
  ['Engine', 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll', '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'],
  ['Game', 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll', 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2'],
];
const digest = filename => createHash('sha256').update(fs.readFileSync(filename)).digest('hex');
for (const [label, filename, expected] of targets) {
  if (digest(filename) !== expected) throw Error(`${label} input fingerprint differs from handoff`);
}
if (fs.existsSync(output)) throw Error('Smoke evidence exists; preserve it before a new run');
fs.mkdirSync(output, {recursive: true});
fs.mkdirSync(runtime, {recursive: true});
const env = {};
for (const key of ['SystemRoot', 'WINDIR', 'ComSpec', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'PROCESSOR_ARCHITECTURE', 'PATH']) {
  if (process.env[key]) env[key] = process.env[key];
}
Object.assign(env, {TEMP: runtime, TMP: runtime, REA_LOG_LEVEL: 'warn', REA_ANALYSIS_PROVIDER: 'auto'});
const commandArgs = projectLauncher ? [path.join(root, 'tools/static_analysis/rea_project_server.mjs')] : [launcher, 'mcp'];
const child = spawn(process.execPath, commandArgs, {cwd: root, env, windowsHide: true, stdio: ['pipe', 'pipe', 'pipe']});
let buffer = '', stderr = '', sequence = 0, failed = false, opened = false;
const pending = new Map();
const save = (name, value) => fs.writeFileSync(path.join(output, `${name}.json`), JSON.stringify(value, null, 2));
const rejectPending = error => { for (const item of pending.values()) { clearTimeout(item.timer); item.reject(error); } pending.clear(); };
child.on('error', rejectPending);
child.on('exit', (code, signal) => rejectPending(Error(`Rea exited: ${code ?? signal}`)));
child.stderr.on('data', data => { stderr = (stderr + data.toString()).slice(-65536); });
child.stdout.on('data', data => {
  buffer += data.toString();
  if (buffer.length > 20 * 1024 * 1024) { rejectPending(Error('MCP response exceeds 20 MiB limit')); child.kill(); return; }
  let newline;
  while ((newline = buffer.indexOf('\n')) >= 0) {
    const line = buffer.slice(0, newline); buffer = buffer.slice(newline + 1);
    let message;
    try { message = JSON.parse(line); } catch { rejectPending(Error('Invalid MCP JSON')); continue; }
    const item = pending.get(message.id);
    if (item) { pending.delete(message.id); clearTimeout(item.timer); item.resolve(message); }
  }
});
function request(method, params) {
  const id = ++sequence;
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(Error(`Timeout: ${method}`)); }, 30000);
    pending.set(id, {resolve, reject, timer});
    child.stdin.write(JSON.stringify({jsonrpc: '2.0', id, method, params}) + '\n');
  });
}
async function call(name, args, label) {
  if (!['binary_session', 'open_binary', 'close_binary'].includes(name)) throw Error('Tool outside smoke allowlist');
  const response = await request('tools/call', {name, arguments: args});
  save(label, response);
  if (response.error || response.result?.isError) throw Error(`${label}: ${JSON.stringify(response).slice(0, 1000)}`);
  return response;
}
try {
  const init = await request('initialize', {protocolVersion: '2024-11-05', capabilities: {}, clientInfo: {name: 'Grim-Dynasty-host-static-check', version: '1'}});
  save('initialize', init);
  if (init.error || init.result?.serverInfo?.version !== '6.5.0') throw Error('Unexpected MCP server identity');
  child.stdin.write(JSON.stringify({jsonrpc: '2.0', method: 'notifications/initialized'}) + '\n');
  const catalog = await request('tools/list', {});
  if (catalog.error) throw Error('Tool catalog failed');
  save('tool-catalog-selected', {total: catalog.result.tools.length, tools: catalog.result.tools.filter(tool => ['binary_session', 'open_binary', 'close_binary'].includes(tool.name))});
  await call('binary_session', {}, 'initial-session');
  for (const [label, filename] of targets) {
    await call('open_binary', {path: filename, provider_id: 'auto'}, `${label}-open`); opened = true;
    await call('binary_session', {}, `${label}-session`);
    await call('close_binary', {}, `${label}-close`); opened = false;
  }
  console.log('Rea 6.5.0 MCP static smoke passed for Engine.dll and Game.dll; see session evidence for provider availability.');
} catch (error) {
  failed = true; console.error(error.message); save('failure', {message: error.message});
} finally {
  if (opened && child.exitCode === null) { try { await call('close_binary', {}, 'cleanup-close'); } catch (error) { failed = true; console.error(error.message); } }
  child.stdin.end();
  const forcedStop = setTimeout(() => { if (child.exitCode === null) child.kill(); }, 3000);
  if (child.exitCode === null) await new Promise(resolve => child.once('exit', resolve));
  clearTimeout(forcedStop);
  fs.writeFileSync(path.join(output, 'stderr.txt'), stderr);
  const fingerprints = targets.map(([label, filename, expected]) => ({label, path: filename, sha256: digest(filename), expected}));
  if (fingerprints.some(item => item.sha256 !== item.expected)) failed = true;
  save('summary', {status: failed ? 'failed' : 'passed', targets_launched: false, fingerprints, child_exit_code: child.exitCode, child_signal: child.signalCode});
}
process.exitCode = failed ? 1 : 0;
