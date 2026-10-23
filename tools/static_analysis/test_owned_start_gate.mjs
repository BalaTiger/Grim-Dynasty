import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {randomUUID} from 'node:crypto';
import {spawn} from 'node:child_process';
import {root, runtime} from './rea_project_environment.mjs';
import {startOwnedMemoryProfile} from './rea_owned_memory_profile.mjs';
import {parserBudget, checkResourceBudget, readHostResourceSnapshot} from './rea_resource_budget.mjs';

// Exercise the real wrapper and real sampler, but never release the Rea import gate.
const output = path.join(root, '.tooling', `owned-start-gate-${randomUUID()}`);
fs.mkdirSync(output);
const result = {started_utc: new Date().toISOString(), passed: false, gate_released: false};
const runtimeNames = () => fs.readdirSync(runtime).sort();
const before = runtimeNames();
let child, profile, childClosed = false, childExit = null;
const failures = [];
try {
  const preflight = readHostResourceSnapshot(root);
  checkResourceBudget(preflight, parserBudget, 'launch');
  fs.writeFileSync(path.join(output, 'preflight.json'), JSON.stringify(preflight, null, 2));
  const nonce = randomUUID();
  child = spawn(process.execPath, [path.join(root, 'tools/static_analysis/rea_profiled_server.mjs'), `--owned-guard=${nonce}`],
    {cwd: root, windowsHide: true, stdio: ['pipe', 'pipe', 'pipe', 'ipc']});
  result.root_pid = child.pid;
  child.stdout.resume();
  let childStderr = '';
  child.stderr.on('data', data => { childStderr += data.toString(); });
  child.on('close', code => { childClosed = true; childExit = code; });
  await new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(Error('Gated wrapper hello timeout')), 10000);
    child.once('error', error => { clearTimeout(timer); reject(error); });
    child.once('message', message => {
      clearTimeout(timer);
      try {
        assert.deepEqual(message, {type: 'owned-start-waiting', nonce, pid: child.pid});
        resolve();
      } catch (error) { reject(error); }
    });
  });
  profile = startOwnedMemoryProfile({
    root, output, backendPid: child.pid, controllerPid: process.pid, budget: parserBudget,
    ownedJob: {nonce, nodeExecutable: process.execPath},
    onFailure(error) { failures.push(error.message); profile?.requestStop(); },
  });
  await profile.ready;
  assert.equal(childClosed, false);
  profile.beginFinalization();
  profile.requestStop();
  result.profile = await profile.finish();
  profile = null;
  assert.equal(result.profile.job_empty_confirmed, true);
  assert.equal(result.profile.sampler_exit_confirmed, true);
  assert.equal(result.profile.sampler_exit_code, 0);
  assert.deepEqual(result.profile.failures, []);
  assert.deepEqual(failures, []);
  assert.equal(result.profile.expected_heap_observed, false);
  assert.deepEqual(runtimeNames(), before);
  assert.equal(childClosed, true);
  assert.equal(childStderr, '');
  result.root_exit_code = childExit;
  result.passed = true;
} catch (error) {
  result.error = String(error.stack ?? error);
  process.exitCode = 1;
} finally {
  if (profile) {
    profile.beginFinalization();
    profile.requestStop();
    result.profile = await profile.finish();
  }
  if (child && !childClosed) child.kill();
  result.finished_utc = new Date().toISOString();
  fs.writeFileSync(path.join(output, 'result.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify({output, passed: result.passed, error: result.error, root_exit_code: result.root_exit_code}));
}
