import test, {after} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import {EventEmitter} from 'node:events';
import {PassThrough} from 'node:stream';
import {fileURLToPath} from 'node:url';
import {parserBudget as budget} from './rea_resource_budget.mjs';
import {startOwnedMemoryProfile} from './rea_owned_memory_profile.mjs';
import {javaRoleProfile as pinned, expectedHeadlessArguments} from './rea_java_role.mjs';

// Every child, process identity, clock and counter below is synthetic.
// The production spawn dependency is replaced; no PowerShell, Java, Rea,
// native process inspection or game input is executed by this suite.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const outputs = fs.mkdtempSync(path.join(os.tmpdir(), 'rea-job-profile-mock-'));
after(() => {
  assert.equal(path.dirname(outputs), os.tmpdir());
  assert.match(path.basename(outputs), /^rea-job-profile-mock-/);
  fs.rmSync(outputs, {recursive: true});
});
const epoch = Date.parse('2026-10-20T05:00:00.000Z');
const created = '2026-10-20T04:59:59.000Z';
const rootPid = 61001, controllerPid = 61002;
const nonce = '867a3d0e-0000-4000-8000-123456789abc';
const nodeExecutable = 'D:\\NodeJS\\node.exe';
const runtimeRoot = path.join(pinned.runtime, 'rea-ghidra-53b4bee510ad6a815d00092a8ab074ec');
const context = {runtimeRoot};
let sequence = 0;

function row(pid, role = 'owned_backend_tree', name = 'node.exe') {
  return {
    pid, parent_pid: role === 'owned_backend_tree' ? rootPid : controllerPid,
    name, role, creation_utc: pid === rootPid ? created : '2026-10-20T04:59:59.500Z',
    ownership: role === 'owned_backend_tree' ? 'explicit-kernel-job' : 'monitor-or-controller',
    executable_path: nodeExecutable,
    private_commit_bytes: 100, working_set_bytes: 200,
    lifetime_peak_commit_bytes_observed: 120, lifetime_peak_working_set_bytes_observed: 240,
    thread_count: 2, java_heap_switches: [], java_arguments: [],
  };
}
function javaRow(args = ['-version'], pid = 61004) {
  return {...row(pid, 'owned_backend_tree', 'java.exe'),
    executable_path: pinned.javaExe, java_arguments: args,
    java_heap_switches: args.filter(value => /^-Xm[sx]/.test(value)),
    private_commit_bytes: 1000, working_set_bytes: 2000};
}
const releaseVmArgs = [
  '-Djava.system.class.loader=ghidra.GhidraClassLoader', '-Dfile.encoding=UTF8',
  '-Duser.country=US', '-Duser.language=en', '-Duser.variant=',
  '-Dsun.java2d.opengl=false', '-Dsun.java2d.d3d=false',
  '-Djdk.tls.client.protocols=TLSv1.2,TLSv1.3', '-Dcpu.core.limit=',
  '-Dcpu.core.override=', '-Dfont.size.override=', '-Dpython.console.encoding=UTF-8',
  '-Dlog4j.skipJansi=true', '-Xshare:off', '-Djavax.xml.accessExternalDTD=',
  '-Djavax.xml.accessExternalSchema=', '-Djavax.xml.accessExternalStylesheet=',
  '--enable-native-access=ALL-UNNAMED',
];
function mainArgs() {
  return [...releaseVmArgs, `-Duser.home=${path.join(runtimeRoot, 'home')}`, '-Xmx384M',
    '-XX:ParallelGCThreads=2', '-XX:CICompilerCount=2', '-cp',
    path.join(pinned.ghidraHome, 'Ghidra/Framework/Utility/lib/Utility.jar'),
    'ghidra.Ghidra', 'ghidra.app.util.headless.AnalyzeHeadless',
    ...expectedHeadlessArguments(context)];
}

function harness() {
  const child = new EventEmitter();
  child.pid = 62000 + ++sequence;
  child.stdin = new PassThrough(); child.stdout = new PassThrough(); child.stderr = new PassThrough();
  const stopFrames = [], failures = [], kills = [], jobStopRequests = [];
  child.stdin.on('data', chunk => {
    for (const line of chunk.toString().trim().split('\n')) stopFrames.push(JSON.parse(line));
  });
  child.kill = (...args) => { kills.push(args); return false; };
  child.unref = () => {};
  let clock = epoch, tick, closed = false, exited = false, profile;
  const output = path.join(outputs, String(sequence));
  fs.mkdirSync(output);
  profile = startOwnedMemoryProfile({
    root, output, backendPid: rootPid, controllerPid, budget,
    ownedJob: {nonce, nodeExecutable},
    onFailure: error => {
      failures.push(error.message);
      jobStopRequests.push({command: 'stop-owned-job', nonce});
      profile.requestStop();
    },
  }, {
    spawn: (exe, args, options) => {
      assert.ok(exe.endsWith('WindowsPowerShell\\v1.0\\powershell.exe'));
      assert.ok(args.includes(path.join(root, 'tools/static_analysis/measure_job_process_memory.ps1')));
      assert.equal(args[args.indexOf('-RootProcessId') + 1], String(rootPid));
      assert.equal(args[args.indexOf('-ControllerProcessId') + 1], String(controllerPid));
      assert.equal(args[args.indexOf('-RunNonce') + 1], nonce);
      assert.equal(args[args.indexOf('-NodeExecutable') + 1], nodeExecutable);
      assert.ok(!args.includes('-StreamSamples'));
      assert.deepEqual(options.stdio, ['pipe', 'pipe', 'pipe']);
      assert.equal(options.windowsHide, true);
      return child;
    },
    javaContext: () => context, now: () => clock,
    setInterval: fn => { tick = fn; return 1; }, clearInterval: () => {},
    finishWaitMs: 1, terminationWaitMs: 1, jobStopWaitMs: 1000,
  });
  profile.ready.catch(() => {});
  const sample = (owned = [row(rootPid)], overrides = {}) => ({
    utc: new Date(clock).toISOString(), root_pid: rootPid, root_creation_utc: created,
    job: {policy: 'windows-job-v1', nonce, root_assigned: true,
      active_processes: owned.length, probed_member_count: owned.length, empty_confirmed: owned.length === 0,
      stop_requested: false, controller_exit_observed: false},
    available_physical_bytes: budget.launchBytes,
    system_commit_bytes: 10 * 1024 ** 3, system_commit_limit_bytes: 16 * 1024 ** 3,
    counters: [...owned, row(controllerPid, 'controller'), row(child.pid, 'sampler', 'powershell.exe')],
    counter_read_gaps: [], live_owned_identity_count: owned.length,
    external_build_or_java_processes: [], ...overrides,
  });
  const send = value => child.stdout.write(JSON.stringify(value) + '\n');
  const exit = (code = 0) => { if (!exited) { exited = true; child.emit('exit', code); } };
  const close = (code = 0) => {
    if (closed) return;
    exit(code); child.stdout.end(); child.stderr.end(); child.emit('close', code); closed = true;
  };
  return {
    profile, child, failures, stopFrames, kills, jobStopRequests, output, sample, send, exit, close,
    tick: () => tick(), advance: ms => { clock += ms; },
    async ready(owned) { send(sample(owned)); await profile.ready; },
    async finish({empty = true, closeSampler = true} = {}) {
      profile.beginFinalization(); profile.markBackendExited();
      if (empty && !closed) send(sample([]));
      if (closeSampler) close();
      return profile.finish();
    },
    assertOnlyJobStop(count = 1) {
      assert.equal(failures.length, count);
      assert.deepEqual(stopFrames, Array.from({length: count}, () => ({command: 'stop-owned-job', nonce})));
      // The only kill method available belongs to the mock sampler; no PID
      // argument, process.kill, taskkill, or arbitrary process API exists here.
      for (const args of kills) assert.deepEqual(args, []);
    },
  };
}

test('owned Job configuration reaches only the injected sampler and clean empty shutdown succeeds', async () => {
  const h = harness(); await h.ready();
  const result = await h.finish();
  assert.equal(result.ownership_policy, 'windows-job-v1');
  assert.equal(result.job_empty_confirmed, true);
  assert.equal(result.sampler_output_drained, true);
  assert.deepEqual(result.failures, []);
  h.assertOnlyJobStop(0); assert.equal(h.kills.length, 0);
});

test('owned version and LaunchSupport helpers have no Xmx yet remain in task memory totals', async () => {
  const h = harness();
  const supportArgs = ['-cp', path.join(pinned.ghidraHome, 'support/LaunchSupport.jar'),
    'LaunchSupport', pinned.ghidraHome, '-vmargs'];
  await h.ready([row(rootPid), javaRow(), javaRow(supportArgs, 61005)]);
  assert.throws(() => h.profile.verifyExpectedHeap(), /No owned Java/);
  const result = await h.finish();
  assert.equal(result.expected_heap_observed, false);
  assert.equal(result.tree_private_commit_peak_bytes, 2100);
  assert.equal(result.with_controller_and_sampler_private_peak_bytes, 2300);
  assert.equal(result.processes.find(item => item.pid === 61004).validated_java_role, 'version-probe');
  assert.equal(result.processes.find(item => item.pid === 61005).validated_java_role, 'launch-support');
  assert.deepEqual(result.failures, []); h.assertOnlyJobStop(0);
});

test('only an owned exact AnalyzeHeadless command establishes observed main heap', async () => {
  const h = harness(); await h.ready([row(rootPid), javaRow(mainArgs())]);
  h.profile.verifyExpectedHeap();
  const result = await h.finish();
  assert.equal(result.expected_heap_observed, true);
  assert.equal(result.processes.find(item => item.pid === 61004).validated_java_role, 'analyze-headless');
  assert.deepEqual(result.failures, []);
});

for (const fault of ['unknown-owned-java', 'wrong-java-executable', 'wrong-main-heap', 'external-java']) {
  test(`${fault} fails closed and sends only this Job's stop command once`, async () => {
    const h = harness(); await h.ready();
    const sample = h.sample([row(rootPid), javaRow()]);
    if (fault === 'unknown-owned-java') sample.counters[1].java_arguments = ['-cp', 'other.jar', 'OtherMain'];
    if (fault === 'wrong-java-executable') sample.counters[1].executable_path = 'C:\\other\\java.exe';
    if (fault === 'wrong-main-heap') sample.counters[1] = javaRow(mainArgs().map(arg => arg === '-Xmx384M' ? '-Xmx512M' : arg));
    if (fault === 'external-java') {
      sample.counters.splice(1, 1); sample.live_owned_identity_count = sample.job.active_processes = sample.job.probed_member_count = 1;
      sample.external_build_or_java_processes = [{ProcessId: 99991, Name: 'java.exe', membership: 'Not in the explicit task Job'}];
    }
    h.send(sample); h.send(sample);
    h.assertOnlyJobStop(); assert.equal(h.kills.length, 0);
    const result = await h.finish();
    assert.equal(result.expected_heap_observed, false);
    assert.ok(result.failures.some(message => /Java|heap/.test(message)));
  });
}

for (const fault of ['nonce', 'job-missing', 'membership', 'monitor-membership', 'root-creation', 'root-row-reused', 'root-pid', 'empty-disagreement']) {
  test(`${fault} identity corruption is rejected before a healthy result`, async () => {
    const h = harness(); await h.ready();
    const sample = h.sample();
    if (fault === 'nonce') sample.job.nonce = 'different-job';
    if (fault === 'job-missing') delete sample.job;
    if (fault === 'membership') delete sample.counters[0].ownership;
    if (fault === 'monitor-membership') sample.counters[1].ownership = 'explicit-kernel-job';
    if (fault === 'root-creation') sample.root_creation_utc = '2026-10-20T04:59:59.001Z';
    if (fault === 'root-row-reused') sample.counters[0].creation_utc = '2026-10-20T05:00:00.000Z';
    if (fault === 'root-pid') sample.root_pid = 99992;
    if (fault === 'empty-disagreement') sample.job.empty_confirmed = true;
    h.send(sample); h.assertOnlyJobStop();
    const result = await h.finish();
    assert.ok(result.failures.length > 0);
  });
}

for (const fault of ['stale-sampler', 'low-memory', 'control-pipe']) {
  test(`after root exit a nonempty Job remains guarded against ${fault}`, async () => {
    const h = harness(); await h.ready([row(rootPid), javaRow()]);
    h.profile.beginFinalization(); h.profile.markBackendExited();
    h.send(h.sample([javaRow()]));
    assert.equal(h.failures.length, 0);
    if (fault === 'stale-sampler') { h.advance(budget.staleAfterMs + 1); h.tick(); h.tick(); }
    if (fault === 'low-memory') h.send(h.sample([javaRow()], {available_physical_bytes: budget.stopBytes - 1}));
    if (fault === 'control-pipe') h.child.stdin.emit('error', new Error('mock disconnection'));
    h.assertOnlyJobStop(); assert.equal(h.kills.length, 0);
    const result = await h.finish();
    assert.equal(result.job_empty_confirmed, true);
    assert.ok(result.failures.length > 0);
  });
}

test('a missing initial sampler sample rejects readiness and does not infer process ownership', async () => {
  const h = harness(); h.advance(budget.staleAfterMs + 1); h.tick(); h.tick();
  await assert.rejects(h.profile.ready, /missing or stale/);
  h.assertOnlyJobStop(); assert.equal(h.kills.length, 0);
  const result = await h.finish();
  assert.equal(result.expected_heap_observed, false);
});

test('sampler exit after root exit with a still nonempty Job records unconfirmed cleanup', async () => {
  const h = harness(); await h.ready([row(rootPid), javaRow()]);
  h.profile.beginFinalization(); h.profile.markBackendExited();
  h.send(h.sample([javaRow()]));
  h.close();
  const result = await h.finish({empty: false});
  assert.ok(result.failures.some(message => /Job.*empty|Job.*nonempty|Job.*cleanup/i.test(message)));
  assert.equal(result.job_empty_confirmed, false);
  assert.equal(result.sampler_exit_confirmed, true);
  assert.equal(h.kills.length, 0);
  // Exit is already observed: the dead control pipe must not be reused to
  // chase the root PID or any Java PID.
  assert.deepEqual(h.stopFrames, []);
});

test('exit can precede the final empty frame; drain that frame before judging Job cleanup', async () => {
  const h = harness(); await h.ready([row(rootPid), javaRow()]);
  h.profile.beginFinalization(); h.profile.markBackendExited();
  h.exit();
  assert.equal(h.failures.length, 0);
  const pending = h.profile.finish();
  h.send(h.sample([])); h.close();
  const result = await pending;
  assert.equal(result.job_empty_confirmed, true);
  assert.equal(result.sampler_output_drained, true);
  assert.deepEqual(result.failures, []);
  h.assertOnlyJobStop(0); assert.equal(h.kills.length, 0);
});

test('non-atomic member and accounting snapshots retain protection until both are empty', async () => {
  const h = harness(); await h.ready([row(rootPid), javaRow()]);
  h.profile.beginFinalization(); h.profile.markBackendExited();
  const transitional = h.sample([javaRow()]);
  transitional.job.active_processes = 0;
  transitional.live_owned_identity_count = 0;
  assert.equal(transitional.job.probed_member_count, 1);
  assert.equal(transitional.job.empty_confirmed, false);
  h.send(transitional);
  assert.equal(h.failures.length, 0);
  const result = await h.finish();
  assert.equal(result.job_empty_confirmed, true);
  assert.deepEqual(result.failures, []);
  h.assertOnlyJobStop(0);
});

test('an unresponsive sampler records uncertain Job cleanup and kills only its own handle', async () => {
  const h = harness(); await h.ready([row(rootPid), javaRow()]);
  h.profile.beginFinalization(); h.profile.markBackendExited();
  h.profile.requestStop();
  assert.deepEqual(h.stopFrames, [{command: 'stop-owned-job', nonce}]);
  const result = await h.profile.finish();
  assert.equal(result.job_empty_confirmed, false);
  assert.equal(result.sampler_exit_confirmed, false);
  assert.equal(result.sampler_output_drained, false);
  assert.ok(result.failures.includes('Sampler exit unconfirmed'));
  assert.ok(result.failures.some(message => message.includes('Explicit Job empty state unconfirmed')));
  assert.equal(h.kills.length, 1); assert.deepEqual(h.kills[0], []);
});

test('confirmed Job empty prevents late parse, budget and stream failures from issuing a stop', async () => {
  const h = harness(); await h.ready();
  h.profile.beginFinalization(); h.profile.markBackendExited();
  h.send(h.sample([]));
  h.child.stdout.write('invalid-late-frame\n');
  h.send(h.sample([], {available_physical_bytes: budget.stopBytes - 1}));
  h.child.stdin.emit('error', new Error('already empty control pipe'));
  h.profile.requestStop();
  h.advance(budget.staleAfterMs + 1); h.tick();
  const result = await h.finish();
  assert.equal(result.job_empty_confirmed, true);
  assert.ok(result.failures.some(message => message.includes('Owned memory monitoring')));
  assert.ok(result.failures.some(message => message.includes('below 2 GiB')));
  h.assertOnlyJobStop(0); assert.equal(h.kills.length, 0);
});

test('a malformed final tail is retained after empty Job confirmation without a stop', async () => {
  const h = harness(); await h.ready();
  h.profile.beginFinalization(); h.profile.markBackendExited();
  h.send(h.sample([])); h.child.stdout.write('{"unfinished":');
  h.close();
  const result = await h.finish({empty: false});
  assert.ok(result.failures.includes('Sampler ended with an incomplete output frame'));
  h.assertOnlyJobStop(0); assert.equal(h.kills.length, 0);
});

test('a contradictory nonempty late frame cannot reopen a confirmed empty Job or issue another stop', async () => {
  const h = harness(); await h.ready();
  h.profile.beginFinalization(); h.profile.markBackendExited();
  h.send(h.sample([]));
  h.send(h.sample([javaRow()], {available_physical_bytes: budget.stopBytes - 1}));
  h.advance(budget.staleAfterMs + 1); h.tick();
  h.profile.requestStop();
  const result = await h.finish();
  assert.equal(result.job_empty_confirmed, true);
  assert.ok(result.failures.some(message => message.includes('contradicts the confirmed empty terminal state')));
  h.assertOnlyJobStop(0); assert.equal(h.kills.length, 0);
});

test('duplicate PIDs in one counter snapshot are rejected even across different roles', async () => {
  const h = harness(); await h.ready();
  const sample = h.sample();
  sample.counters.push({...row(rootPid, 'controller'), creation_utc: created});
  h.send(sample);
  const result = await h.finish();
  assert.ok(result.failures.some(message => message.includes('Duplicate process PID in one counter snapshot')));
  h.assertOnlyJobStop(); assert.equal(h.kills.length, 0);
});

test('finalization still rejects a counter root creation conflicting with the pinned root', async () => {
  const h = harness(); await h.ready([row(rootPid), javaRow()]);
  h.profile.beginFinalization(); h.profile.markBackendExited();
  const sample = h.sample([row(rootPid), javaRow()]);
  sample.counters[0].creation_utc = '2026-10-20T05:00:00.000Z';
  h.send(sample);
  const result = await h.finish();
  assert.ok(result.failures.some(message => message.includes('Counter root creation conflicts with the pinned root')));
  h.assertOnlyJobStop(); assert.equal(h.kills.length, 0);
});

function samplerFailureLine() {
  return 'REA_SAMPLER_FAILURE ' + JSON.stringify({
    schema: 'rea-sampler-failure-v1',
    event: 'sampler-failure',
    utc: new Date(epoch).toISOString(),
    sampler_phase: 'before-inventory-query',
    root_pid: rootPid,
    controller_pid: controllerPid,
    error_kind: 'Win32Exception',
    native_error_code: 31,
    process_id: 61009,
    creation_utc: '2026-10-20T04:59:59.750Z',
    creation_ticks: '639280979997500000',
    in_job: true,
    probe_step: 'QueryFullProcessImageName',
    image_query_error_code: 31,
    exit_confirmed_after_image_failure: false,
    exit_check_error_code: null,
    from_query_members: true,
    member_index: 2,
    member_count: 3,
    disposition: 'fail-closed; original exception rethrown; owned Job cleanup remains required',
  }) + '\n';
}

test('structured sampler failure before readiness is archived without becoming a sample or empty Job proof', async () => {
  const h = harness();
  const diagnostic = samplerFailureLine();
  h.child.stderr.write(diagnostic);
  h.close(1);
  await assert.rejects(h.profile.ready, /Memory sampler exited early \(1\)/);
  const result = await h.finish({empty: false});
  assert.equal(result.samples, 0);
  assert.equal(result.last_sample_utc, null);
  assert.equal(result.minimum_available_physical_bytes, null);
  assert.equal(result.minimum_commit_headroom_bytes, null);
  assert.deepEqual(result.processes, []);
  assert.equal(result.expected_heap_observed, false);
  assert.equal(result.job_empty_confirmed, false);
  assert.equal(result.sampler_exit_confirmed, true);
  assert.equal(result.sampler_exit_code, 1);
  assert.equal(result.sampler_output_drained, true);
  assert.equal(h.failures.length, 1);
  assert.deepEqual(h.jobStopRequests, [{command: 'stop-owned-job', nonce}]);
  // Exit was already observed before onFailure: request the dedicated stop,
  // but never write to the dead pipe or claim that Job cleanup succeeded.
  assert.deepEqual(h.stopFrames, []);
  assert.deepEqual(h.kills, []);
  assert.ok(result.failures.includes('Memory sampler exited early (1)'));
  assert.ok(result.failures.some(message => message.includes('Explicit Job empty state unconfirmed')));
  assert.equal(fs.readFileSync(path.join(h.output, 'owned-memory-sampler-stderr.txt'), 'utf8'), diagnostic);
  const saved = JSON.parse(fs.readFileSync(path.join(h.output, 'owned-memory-summary.json'), 'utf8'));
  assert.equal(saved.samples, 0);
  assert.equal(saved.job_empty_confirmed, false);
  assert.equal(saved.sampler_exit_code, 1);
});

for (const finalizing of [false, true]) {
  test(`ample memory cannot hide structured sampler failure during ${finalizing ? 'finalization' : 'analysis'}`, async () => {
    const h = harness();
    await h.ready();
    if (finalizing) {
      h.profile.beginFinalization();
      h.profile.markBackendExited();
    }
    const diagnostic = samplerFailureLine();
    const split = diagnostic.indexOf('"probe_step"');
    h.child.stderr.write(diagnostic.slice(0, split));
    h.child.stderr.write(diagnostic.slice(split));
    // A further healthy, nonempty sample is genuine, but cannot substitute
    // for the subsequent sampler failure or terminal Job-empty evidence.
    h.advance(1000);
    const healthy = h.sample();
    h.send(healthy);
    h.close(1);
    const result = await h.finish({empty: false});
    assert.equal(result.samples, 2);
    assert.equal(result.last_sample_utc, healthy.utc);
    assert.ok(result.minimum_available_physical_bytes >= budget.launchBytes);
    assert.ok(result.minimum_commit_headroom_bytes >= budget.launchBytes);
    assert.equal(result.job_empty_confirmed, false);
    assert.equal(result.sampler_exit_code, 1);
    assert.equal(result.sampler_exit_confirmed, true);
    assert.equal(result.sampler_output_drained, true);
    assert.equal(result.processes.some(item => item.pid === 61009), false);
    assert.equal(h.failures.length, 1);
    assert.deepEqual(h.jobStopRequests, [{command: 'stop-owned-job', nonce}]);
    assert.deepEqual(h.stopFrames, []);
    assert.deepEqual(h.kills, []);
    assert.ok(result.failures.includes(`Memory sampler exited ${finalizing ? 'abnormally' : 'early'} (1)`));
    assert.ok(result.failures.includes('Sampler exit code 1'));
    assert.ok(result.failures.some(message => message.includes('Explicit Job empty state unconfirmed')));
    const stderr = fs.readFileSync(path.join(h.output, 'owned-memory-sampler-stderr.txt'), 'utf8');
    assert.equal(stderr, diagnostic);
    const retained = JSON.parse(stderr.slice('REA_SAMPLER_FAILURE '.length));
    assert.equal(retained.probe_step, 'QueryFullProcessImageName');
    assert.equal(retained.native_error_code, 31);
    assert.equal(retained.creation_ticks, '639280979997500000');
    assert.equal(retained.exit_confirmed_after_image_failure, false);
  });
}
