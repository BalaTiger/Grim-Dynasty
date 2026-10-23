import fs from 'node:fs';
import path from 'node:path';
import {spawn} from 'node:child_process';
import {root, launcher, javaHome, projectEnvironment} from './rea_project_environment.mjs';

const output = path.join(root, 'docs/host-migration/native-backend');
const reportPath = path.join(output, 'selftest.json');
if (process.argv.length !== 2) throw Error('No arguments supported');
if (fs.existsSync(reportPath)) throw Error('Self-test evidence already exists; inspect it before another run');
fs.mkdirSync(output, {recursive: true});
const env = projectEnvironment();
const runs = [];
const summary = {status: 'running', target_binaries_executed: false, environment: env, runs};
const save = () => fs.writeFileSync(reportPath, JSON.stringify(summary, null, 2));
async function run(label, command, args, timeoutMs) {
  console.log(`Starting ${label}`);
  const child = spawn(command, args, {cwd: root, env, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe']});
  let stdout = '', stderr = '', timedOut = false;
  child.stdout.on('data', data => { stdout = (stdout + data).slice(-1024 * 1024); });
  child.stderr.on('data', data => { stderr = (stderr + data).slice(-1024 * 1024); });
  const timer = setTimeout(() => { timedOut = true; child.kill(); }, timeoutMs);
  const result = await new Promise((resolve, reject) => {
    child.once('error', reject);
    child.once('exit', (exitCode, signal) => resolve({label, command, args, exitCode, signal, timedOut, stdout, stderr}));
  }).finally(() => clearTimeout(timer));
  runs.push(result); save();
  console.log(`${label}: exit ${result.exitCode}, timedOut=${timedOut}`);
  if (result.exitCode !== 0 || timedOut) throw Error(`${label} failed; inspect selftest.json`);
  return result;
}
try {
  await run('java-version', path.join(javaHome, 'bin/java.exe'), ['-version'], 30000);
  await run('javac-version', path.join(javaHome, 'bin/javac.exe'), ['-version'], 30000);
  const doctor = await run('rea-doctor-ghidra', process.execPath, [launcher, 'doctor', '--provider', 'ghidra', '--json'], 180000);
  summary.doctor = JSON.parse(doctor.stdout);
  const ghidra = summary.doctor.providerInspections?.find(item => item.id === 'ghidra');
  if (summary.doctor.healthy !== true || ghidra?.available !== true || ghidra.providerVersion !== '12.1.4' || !ghidra.checks?.every(item => item.ok === true)) {
    throw Error('Ghidra doctor checks did not all pass; inspect selftest.json');
  }
  summary.status = 'passed';
} catch (error) {
  summary.status = 'failed'; summary.failure = error.message; process.exitCode = 1;
} finally { save(); }
