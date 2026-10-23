import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {EventEmitter} from 'node:events';
import {PassThrough} from 'node:stream';
import {resolveJavaTargetProfile, assertJavaSessionTarget, validateJavaRole} from './rea_java_role.mjs';
import {startOwnedMemoryProfile} from './rea_owned_memory_profile.mjs';
import {clientMouseCalibrationBudget, parserBudget, gameMouseCalibrationBudget} from './rea_resource_budget.mjs';

// Synthetic contracts only. Spawn and runtime discovery are injected; no target,
// descriptor, backend or native monitor is opened. Only new .tooling mock output
// directories are written when these tests are explicitly run; none are deleted.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const fixtureRoot = String.raw`C:\Users\zhuzi\Documents\Grim-Dynasty`;
const runtimeRoot = fixtureRoot + String.raw`\analysis-runtime\rea\rea-ghidra-76b97c234d9244a58874449da153cdeb`;
const javaExe = fixtureRoot + String.raw`\.tooling\jdk\jdk-21.0.12.1+1\bin\java.exe`;
const utilityJar = fixtureRoot + String.raw`\.tooling\ghidra\ghidra_12.1.4_PUBLIC\Ghidra\Framework\Utility\lib\Utility.jar`;
const bridgeDirectory = fixtureRoot + String.raw`\.tooling\rea\node_modules\rea-agents\bridge\ghidra`;
const clientSha = 'd3915e2599317b6670cc088ba958afb2b2f4fa8141cfd1d777ee02f9ea1fc4c4';
const engineSha = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const gameSha = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const session = {target_sha256: clientSha, provider_version: '12.1.4', run_id: 'synthetic-client'};
const context = {runtimeRoot, targetProfile: 'client'};

// Complete Windows-batch golden, independently written rather than obtained
// from expectedHeadlessArguments, resolved profiles or observed runtime argv.
const clientArgs = Object.freeze([
  '-Djava.system.class.loader=ghidra.GhidraClassLoader', '-Dfile.encoding=UTF8',
  '-Duser.country=US', '-Duser.language=en', '-Duser.variant=',
  '-Dsun.java2d.opengl=false', '-Dsun.java2d.d3d=false',
  '-Djdk.tls.client.protocols=TLSv1.2,TLSv1.3', '-Dcpu.core.limit=',
  '-Dcpu.core.override=', '-Dfont.size.override=', '-Dpython.console.encoding=UTF-8',
  '-Dlog4j.skipJansi=true', '-Xshare:off', '-Djavax.xml.accessExternalDTD=',
  '-Djavax.xml.accessExternalSchema=', '-Djavax.xml.accessExternalStylesheet=',
  '--enable-native-access=ALL-UNNAMED',
  `-Duser.home=${runtimeRoot}\\home`, '-Xmx384M', '-XX:ParallelGCThreads=2', '-XX:CICompilerCount=2',
  '-cp', utilityJar, 'ghidra.Ghidra', 'ghidra.app.util.headless.AnalyzeHeadless',
  runtimeRoot + '\\project', 'rea-project', '-import', runtimeRoot + '\\target-d3915e259931.exe',
  '-readOnly', '-deleteProject', '-log', runtimeRoot + '\\ghidra.log',
  '-scriptlog', runtimeRoot + '\\script.log', '-scriptPath', bridgeDirectory,
  '-postScript', bridgeDirectory + '\\ReaGhidraBridge.java', runtimeRoot + '\\session.json',
]);
const observation = (args = clientArgs) => ({strictOwnershipVerified: true, exe: javaExe, args});
function withSnapshot(name) {
  const args = [...clientArgs]; args[args.indexOf('-import') + 1] = runtimeRoot + '\\' + name;
  return args;
}

test('client profile is exact and frozen; Engine default and Game identity are retained', () => {
  assert.deepEqual(resolveJavaTargetProfile('client'), {name: 'client', sha256: clientSha, snapshotName: 'target-d3915e259931.exe'});
  assert.deepEqual(resolveJavaTargetProfile(), {name: 'engine', sha256: engineSha, snapshotName: 'target-4c2ea14ce1c5.dll'});
  assert.deepEqual(resolveJavaTargetProfile('game'), {name: 'game', sha256: gameSha, snapshotName: 'target-cd7ecd7942d7.dll'});
  for (const name of ['engine', 'game', 'client']) assert.ok(Object.isFrozen(resolveJavaTargetProfile(name)));
});

test('exact client descriptor and independent complete argv pass', () => {
  assert.doesNotThrow(() => assertJavaSessionTarget(session, 'client'));
  const result = validateJavaRole(observation(), context);
  assert.equal(result.ok, true, result.reason);
  assert.equal(result.role, 'analyze-headless');
  assert.equal(result.launch, 'windows-batch');
  assert.equal(result.heapArgument, '-Xmx384M');
  assert.equal(result.countTowardMemory, true);
});

test('client rejects Engine/Game descriptors, their snapshot argv and a DLL suffix', () => {
  for (const sha of [engineSha, gameSha]) {
    assert.throws(() => assertJavaSessionTarget({...session, target_sha256: sha}, 'client'), /fixed target session/);
  }
  for (const name of ['target-4c2ea14ce1c5.dll', 'target-cd7ecd7942d7.dll', 'target-d3915e259931.dll']) {
    const result = validateJavaRole(observation(withSnapshot(name)), context);
    assert.equal(result.ok, false, name);
    assert.equal(result.reason, 'UnexpectedHeadlessArguments', name);
  }
  for (const targetProfile of ['engine', 'game']) {
    assert.throws(() => assertJavaSessionTarget(session, targetProfile), /fixed target session/);
    assert.equal(validateJavaRole(observation(), {...context, targetProfile}).ok, false);
  }
});

test('unknown profile cannot bypass selection with a version probe', () => {
  for (const targetProfile of ['other', 'Client', '', null]) {
    assert.throws(() => resolveJavaTargetProfile(targetProfile), /UnknownJavaTargetProfile/);
    assert.throws(() => assertJavaSessionTarget(session, targetProfile), /UnknownJavaTargetProfile/);
    const result = validateJavaRole(observation(['-version']), {...context, targetProfile});
    assert.equal(result.ok, false);
    assert.equal(result.reason, 'UnknownJavaTargetProfile');
  }
});

test('client still requires exact heap and separately verified ownership', () => {
  for (const args of [clientArgs.map(arg => arg === '-Xmx384M' ? '-Xmx512M' : arg), [...clientArgs, '-Xmx384M']]) {
    assert.equal(validateJavaRole(observation(args), context).reason, 'MainHeapMustBeExactly384M');
  }
  assert.equal(validateJavaRole({...observation(), strictOwnershipVerified: false}, context).reason, 'StrictOwnershipRequired');
});

test('client first-calibration budget is independent, frozen and explicitly unmeasured', () => {
  assert.ok(Object.isFrozen(clientMouseCalibrationBudget));
  assert.notEqual(clientMouseCalibrationBudget, parserBudget);
  assert.notEqual(clientMouseCalibrationBudget, gameMouseCalibrationBudget);
  assert.equal(clientMouseCalibrationBudget.heap, '384M');
  assert.equal(clientMouseCalibrationBudget.launchBytes, 5 * 1024 ** 3);
  assert.equal(clientMouseCalibrationBudget.stopBytes, 2 * 1024 ** 3);
  assert.equal(clientMouseCalibrationBudget.calibrationOnly, true);
  assert.equal(clientMouseCalibrationBudget.measuredClientPeakAvailable, false);
});

async function mockClientMonitor(injectedContext, expectedError) {
  const output = fs.mkdtempSync(path.join(root, '.tooling', 'rea-client-target-profile-mock-'));
  const child = new EventEmitter(); child.pid = 63003;
  child.stdin = new PassThrough(); child.stdout = new PassThrough(); child.stderr = new PassThrough();
  const failures = []; let spawns = 0, contexts = 0, clears = 0, kills = 0;
  child.kill = () => { kills++; return false; }; child.unref = () => {};
  const backendPid = 63001, javaPid = 63004, nonce = 'b62a880f-1764-4467-85d1-633c2f3c5bda';
  const utc = '2026-10-22T12:00:00.000Z', created = '2026-10-22T11:59:59.000Z';
  const row = (pid, name, executable_path, java_arguments = []) => ({
    pid, parent_pid: backendPid, name, executable_path, java_arguments,
    creation_utc: created, role: 'owned_backend_tree', ownership: 'explicit-kernel-job',
    private_commit_bytes: 100, working_set_bytes: 200, lifetime_peak_commit_bytes_observed: 120,
    lifetime_peak_working_set_bytes_observed: 240, thread_count: 2,
    java_heap_switches: java_arguments.filter(arg => arg.startsWith('-Xmx')),
  });
  const frame = counters => ({utc, root_pid: backendPid, root_creation_utc: created, counters,
    job: {policy: 'windows-job-v1', nonce, root_assigned: true, active_processes: counters.length,
      probed_member_count: counters.length, empty_confirmed: counters.length === 0, controller_exit_observed: false},
    available_physical_bytes: 6 * 1024 ** 3, system_commit_bytes: 10 * 1024 ** 3,
    system_commit_limit_bytes: 16 * 1024 ** 3, live_owned_identity_count: counters.length,
    counter_read_gaps: [], external_build_or_java_processes: [],
  });
  const profile = startOwnedMemoryProfile({root, output, backendPid, controllerPid: 63002,
    targetProfile: 'client', budget: clientMouseCalibrationBudget,
    ownedJob: {nonce, nodeExecutable: 'C:\\synthetic\\node.exe'}, onFailure: error => failures.push(error.message)}, {
    spawn(executable, args, options) {
      spawns++; assert.ok(executable.endsWith('WindowsPowerShell\\v1.0\\powershell.exe'));
      assert.ok(args.includes(path.join(root, 'tools/static_analysis/measure_job_process_memory.ps1')));
      assert.equal(args[args.indexOf('-OutputFile') + 1], path.join(output, 'owned-memory.ndjson'));
      assert.equal(options.windowsHide, true); assert.deepEqual(options.stdio, ['pipe', 'pipe', 'pipe']);
      return child;
    },
    javaContext: () => { contexts++; return injectedContext; }, now: () => Date.parse(utc),
    setInterval: () => 1, clearInterval: () => { clears++; }, finishWaitMs: 1,
  });
  profile.ready.catch(() => {});
  const send = counters => child.stdout.write(JSON.stringify(frame(counters)) + '\n');
  let summary;
  try {
    send([row(backendPid, 'node.exe', 'C:\\synthetic\\node.exe'), row(javaPid, 'java.exe', javaExe, clientArgs)]);
    if (expectedError) {
      await assert.rejects(profile.ready, expectedError);
      assert.throws(() => profile.verifyExpectedHeap(), /No owned Java/);
    } else {
      await profile.ready; assert.doesNotThrow(() => profile.verifyExpectedHeap());
    }
  } finally {
    profile.beginFinalization(); profile.markBackendExited(); send([]);
    child.emit('exit', 0); child.stdout.end(); child.stderr.end(); child.emit('close', 0);
    summary = await profile.finish();
  }
  assert.equal(spawns, 1); assert.equal(contexts, 1); assert.equal(clears, 1); assert.equal(kills, 0);
  assert.equal(summary.job_empty_confirmed, true); assert.equal(summary.sampler_exit_confirmed, true);
  assert.equal(summary.sampler_output_drained, true);
  assert.equal(summary.expected_heap_observed, !expectedError);
  if (expectedError) {
    assert.equal(failures.length, 1); assert.equal(summary.failures.length, 1);
    assert.match(failures[0], expectedError); assert.match(summary.failures[0], expectedError);
  } else {
    assert.deepEqual(failures, []); assert.deepEqual(summary.failures, []);
    assert.equal(summary.processes.find(item => item.pid === javaPid).validated_java_role, 'analyze-headless');
  }
}

test('client selection reaches the mock monitor when injected context omits its profile', async () => {
  await mockClientMonitor({runtimeRoot});
});

test('mock monitor rejects Engine/Game context overriding selected client', async () => {
  for (const targetProfile of ['engine', 'game']) {
    await mockClientMonitor({runtimeRoot, targetProfile}, /Java context target profile mismatch/);
  }
});
