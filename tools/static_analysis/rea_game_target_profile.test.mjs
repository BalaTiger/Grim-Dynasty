import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {EventEmitter} from 'node:events';
import {PassThrough} from 'node:stream';
import {resolveJavaTargetProfile, assertJavaSessionTarget, validateJavaRole} from './rea_java_role.mjs';
import {startOwnedMemoryProfile} from './rea_owned_memory_profile.mjs';
import {parserBudget, gameMouseCalibrationBudget} from './rea_resource_budget.mjs';

// Synthetic only: no child process, native query, target binary, or runtime
// descriptor is opened. All sampler launches and Java contexts are injected.
// Retain mock summaries under this project's .tooling; never clean them up.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const fixtureRoot = String.raw`C:\Users\zhuzi\Documents\Grim-Dynasty`;
const runtimeRoot = fixtureRoot + String.raw`\analysis-runtime\rea\rea-ghidra-53b4bee510ad6a815d00092a8ab074ec`;
const javaExe = fixtureRoot + String.raw`\.tooling\jdk\jdk-21.0.12.1+1\bin\java.exe`;
const utilityJar = fixtureRoot + String.raw`\.tooling\ghidra\ghidra_12.1.4_PUBLIC\Ghidra\Framework\Utility\lib\Utility.jar`;
const bridgeDirectory = fixtureRoot + String.raw`\.tooling\rea\node_modules\rea-agents\bridge\ghidra`;
const engineSha = '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const gameSha = 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2';
const engineSession = Object.freeze({target_sha256: engineSha, provider_version: '12.1.4', run_id: 'synthetic-engine'});
const gameSession = Object.freeze({target_sha256: gameSha, provider_version: '12.1.4', run_id: 'synthetic-game'});

// Two independent complete argument goldens. Neither argv is generated from
// expectedHeadlessArguments, a resolved profile, or the other target's argv.
const engineArgs = Object.freeze([
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
  runtimeRoot + '\\project', 'rea-project', '-import', runtimeRoot + '\\target-4c2ea14ce1c5.dll',
  '-readOnly', '-deleteProject', '-log', runtimeRoot + '\\ghidra.log',
  '-scriptlog', runtimeRoot + '\\script.log', '-scriptPath', bridgeDirectory,
  '-postScript', bridgeDirectory + '\\ReaGhidraBridge.java', runtimeRoot + '\\session.json',
]);
const gameArgs = Object.freeze([
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
  runtimeRoot + '\\project', 'rea-project', '-import', runtimeRoot + '\\target-cd7ecd7942d7.dll',
  '-readOnly', '-deleteProject', '-log', runtimeRoot + '\\ghidra.log',
  '-scriptlog', runtimeRoot + '\\script.log', '-scriptPath', bridgeDirectory,
  '-postScript', bridgeDirectory + '\\ReaGhidraBridge.java', runtimeRoot + '\\session.json',
]);
const observe = args => ({strictOwnershipVerified: true, exe: javaExe, args});
const context = targetProfile => ({runtimeRoot, targetProfile});
function accepted(args, targetProfile) {
  const result = validateJavaRole(observe(args), context(targetProfile));
  assert.equal(result.ok, true, result.reason);
  assert.equal(result.role, 'analyze-headless');
  assert.equal(result.launch, 'windows-batch');
  assert.equal(result.heapArgument, '-Xmx384M');
  assert.equal(result.countTowardMemory, true);
}

test('profile resolver has exactly two immutable targets and defaults to Engine', () => {
  assert.deepEqual(resolveJavaTargetProfile(), {name: 'engine', sha256: engineSha, snapshotName: 'target-4c2ea14ce1c5.dll'});
  assert.deepEqual(resolveJavaTargetProfile('game'), {name: 'game', sha256: gameSha, snapshotName: 'target-cd7ecd7942d7.dll'});
  assert.ok(Object.isFrozen(resolveJavaTargetProfile('engine')));
  assert.ok(Object.isFrozen(resolveJavaTargetProfile('game')));
});

test('complete independent Engine argv and descriptor pass the default profile', () => {
  accepted(engineArgs, undefined);
  assert.doesNotThrow(() => assertJavaSessionTarget(engineSession));
});

test('complete independent Game argv and descriptor pass only the selected Game profile', () => {
  accepted(gameArgs, 'game');
  assert.doesNotThrow(() => assertJavaSessionTarget(gameSession, 'game'));
});

test('descriptor and argv target mismatches fail in both directions', () => {
  for (const [session, args, profile] of [[gameSession, gameArgs, 'engine'], [engineSession, engineArgs, 'game']]) {
    assert.throws(() => assertJavaSessionTarget(session, profile), /fixed target session/);
    const verdict = validateJavaRole(observe(args), context(profile));
    assert.equal(verdict.ok, false);
    assert.equal(verdict.reason, 'UnexpectedHeadlessArguments');
  }
});

test('unknown profiles reject even valid version probes and cannot spawn a sampler', () => {
  let spawnCount = 0;
  for (const profile of ['other', 'Game', '', null, 1]) {
    assert.throws(() => resolveJavaTargetProfile(profile), /UnknownJavaTargetProfile/);
    assert.throws(() => assertJavaSessionTarget(gameSession, profile), /UnknownJavaTargetProfile/);
    assert.equal(validateJavaRole(observe(['-version']), context(profile)).reason, 'UnknownJavaTargetProfile');
    assert.throws(() => startOwnedMemoryProfile({root, output: path.join(root, '.tooling', 'unused-target-profile-output'),
      backendPid: 61001, controllerPid: 61002, targetProfile: profile, onFailure() {}},
    {spawn() { spawnCount++; throw Error('Unexpected mock spawn'); }}), /UnknownJavaTargetProfile/);
  }
  assert.equal(spawnCount, 0);
});

test('each descriptor rejects wrong digest, provider, and non-string run IDs', () => {
  for (const [session, profile] of [[engineSession, 'engine'], [gameSession, 'game']]) {
    for (const patch of [{target_sha256: '0'.repeat(64)}, {provider_version: '12.1.3'},
      {provider_version: 12.1}, {run_id: undefined}, {run_id: null}, {run_id: 7}]) {
      assert.throws(() => assertJavaSessionTarget({...session, ...patch}, profile), /fixed target session/);
    }
    assert.throws(() => assertJavaSessionTarget(null, profile), /fixed target session/);
  }
});

test('Game selection preserves literal 384M and independent ownership requirements', () => {
  const wrongHeap = gameArgs.map(arg => arg === '-Xmx384M' ? '-Xmx512M' : arg);
  assert.equal(validateJavaRole(observe(wrongHeap), context('game')).reason, 'MainHeapMustBeExactly384M');
  const duplicateHeap = [...gameArgs, '-Xmx384M'];
  assert.equal(validateJavaRole(observe(duplicateHeap), context('game')).reason, 'MainHeapMustBeExactly384M');
  assert.equal(validateJavaRole({...observe(gameArgs), strictOwnershipVerified: false}, context('game')).reason,
    'StrictOwnershipRequired');
});

const epoch = Date.parse('2026-10-22T12:00:00.000Z');
const created = '2026-10-22T11:59:59.000Z';
const rootPid = 61001, controllerPid = 61002, javaPid = 61004;
const nonce = '867a3d0e-0000-4000-8000-123456789abc';
let outputs, sequence = 0;
function harness(targetProfile, injectedContext = {runtimeRoot}) {
  outputs ??= fs.mkdtempSync(path.join(root, '.tooling', 'rea-target-profile-mock-'));
  const output = path.join(outputs, String(++sequence));
  fs.mkdirSync(output);
  const child = new EventEmitter();
  child.pid = 62000 + sequence;
  child.stdin = new PassThrough(); child.stdout = new PassThrough(); child.stderr = new PassThrough();
  const failures = [], kills = [];
  child.kill = (...args) => { kills.push(args); return false; };
  child.unref = () => {};
  let spawnCount = 0, contextCalls = 0, cleared = 0;
  const budget = targetProfile === 'game' ? gameMouseCalibrationBudget : parserBudget;
  const options = {root, output, backendPid: rootPid, controllerPid, budget,
    ownedJob: {nonce, nodeExecutable: 'C:\\synthetic\\node.exe'}, onFailure: error => failures.push(error.message)};
  // Deliberately omit the property for the default Engine integration test.
  if (targetProfile !== undefined) options.targetProfile = targetProfile;
  const profile = startOwnedMemoryProfile(options, {
    spawn(exe, args, spawnOptions) {
      spawnCount++;
      assert.ok(exe.endsWith('WindowsPowerShell\\v1.0\\powershell.exe'));
      assert.ok(args.includes(path.join(root, 'tools/static_analysis/measure_job_process_memory.ps1')));
      assert.deepEqual(spawnOptions.stdio, ['pipe', 'pipe', 'pipe']);
      assert.equal(spawnOptions.cwd, root);
      assert.equal(spawnOptions.windowsHide, true);
      return child;
    },
    javaContext: () => { contextCalls++; return injectedContext; }, now: () => epoch,
    setInterval: () => 1, clearInterval: () => { cleared++; }, finishWaitMs: 1,
  });
  profile.ready.catch(() => {});
  const row = (pid, name = 'node.exe') => ({pid, parent_pid: rootPid, name, role: 'owned_backend_tree',
    creation_utc: created, ownership: 'explicit-kernel-job', executable_path: 'C:\\synthetic\\node.exe',
    private_commit_bytes: 100, working_set_bytes: 200,
    lifetime_peak_commit_bytes_observed: 120, lifetime_peak_working_set_bytes_observed: 240,
    thread_count: 2, java_heap_switches: [], java_arguments: []});
  const sample = counters => ({utc: new Date(epoch).toISOString(), root_pid: rootPid, root_creation_utc: created,
    job: {policy: 'windows-job-v1', nonce, root_assigned: true, active_processes: counters.length,
      probed_member_count: counters.length, empty_confirmed: counters.length === 0, controller_exit_observed: false},
    available_physical_bytes: 6 * 1024 ** 3, system_commit_bytes: 10 * 1024 ** 3,
    system_commit_limit_bytes: 16 * 1024 ** 3, counters, counter_read_gaps: [],
    live_owned_identity_count: counters.length, external_build_or_java_processes: []});
  const send = value => child.stdout.write(JSON.stringify(value) + '\n');
  return {
    profile, failures,
    frame(args, ownership = 'explicit-kernel-job') {
      return sample([row(rootPid), {...row(javaPid, 'java.exe'), executable_path: javaExe, ownership,
        java_arguments: args, java_heap_switches: args.filter(arg => arg.startsWith('-Xmx'))}]);
    }, send,
    async finish() {
      profile.beginFinalization(); profile.markBackendExited();
      send(sample([]));
      child.emit('exit', 0); child.stdout.end(); child.stderr.end(); child.emit('close', 0);
      const result = await profile.finish();
      assert.equal(spawnCount, 1);
      assert.equal(cleared, 1);
      assert.deepEqual(kills, []);
      assert.equal(result.job_empty_confirmed, true);
      assert.equal(result.sampler_exit_confirmed, true);
      assert.equal(result.sampler_output_drained, true);
      return {result, contextCalls};
    },
  };
}

async function successfulMock(targetProfile, args) {
  const h = harness(targetProfile);
  h.send(h.frame(args));
  await h.profile.ready;
  assert.doesNotThrow(() => h.profile.verifyExpectedHeap());
  const {result, contextCalls} = await h.finish();
  assert.equal(contextCalls, 1);
  assert.equal(result.expected_heap_observed, true);
  assert.equal(result.processes.find(item => item.pid === javaPid).validated_java_role, 'analyze-headless');
  assert.deepEqual(h.failures, []);
  assert.deepEqual(result.failures, []);
}
async function failedMock({targetProfile = 'game', args = gameArgs, injectedContext, ownership, reason}) {
  const h = harness(targetProfile, injectedContext);
  const frame = h.frame(args, ownership);
  h.send(frame); h.send(frame);
  await assert.rejects(h.profile.ready, reason);
  assert.throws(() => h.profile.verifyExpectedHeap(), /No owned Java/);
  const {result} = await h.finish();
  assert.equal(h.failures.length, 1);
  assert.equal(result.failures.length, 1);
  assert.match(result.failures[0], reason);
  assert.equal(result.expected_heap_observed, false);
}

test('mock Game sampler accepts exact target and completes with Job-empty proof', async () => {
  await successfulMock('game', gameArgs);
});

test('mock sampler defaults to Engine with no targetProfile option', async () => {
  await successfulMock(undefined, engineArgs);
});

test('mock sampler rejects opposite target argv in both directions', async () => {
  await failedMock({targetProfile: 'game', args: engineArgs, reason: /UnexpectedHeadlessArguments/});
  await failedMock({targetProfile: 'engine', args: gameArgs, reason: /UnexpectedHeadlessArguments/});
});

test('injected Java context cannot override the selected target profile', async () => {
  await failedMock({injectedContext: context('engine'), reason: /Java context target profile mismatch/});
});

test('mock Game sampler rejects unproven ownership before claiming its heap', async () => {
  await failedMock({ownership: 'cim-ancestry', reason: /Owned process lacks explicit Job membership/});
});

test('mock Game sampler rejects a changed main heap', async () => {
  await failedMock({args: gameArgs.map(arg => arg === '-Xmx384M' ? '-Xmx512M' : arg),
    reason: /MainHeapMustBeExactly384M/});
});
