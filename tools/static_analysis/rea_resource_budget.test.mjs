import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {EventEmitter} from 'node:events';
import {PassThrough} from 'node:stream';
import {fileURLToPath} from 'node:url';
import {parserBudget as budget, checkResourceBudget, readHostResourceSnapshot} from './rea_resource_budget.mjs';
import {startOwnedMemoryProfile} from './rea_owned_memory_profile.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const fixture = fs.readFileSync(path.join(root, 'docs/object-lifecycle/rea-binary-subobject-profile/owned-memory.ndjson'), 'utf8').trim().split(/\r?\n/).map(JSON.parse);
const outputs = fs.mkdtempSync(path.join(root, '.tooling/parser-guard-tests-'));
const base = () => structuredClone(fixture[0]);
const time = Date.parse(fixture[0].utc);
const host = (physical = budget.launchBytes, commit = budget.launchBytes) => ({
  utc: new Date(time).toISOString(), available_physical_bytes: physical,
  system_commit_bytes: 10 * 1024 ** 3, system_commit_limit_bytes: 10 * 1024 ** 3 + commit,
  external_build_or_java_processes: [],
});

test('launch accepts each exact 4.25 GiB boundary', () => {
  assert.deepEqual(checkResourceBudget(host(), budget, 'launch', time), {availableBytes: 4563402752, commitHeadroomBytes: 4563402752});
});
for (const kind of ['physical', 'commit']) {
  test(`launch refuses ${kind} one byte below threshold before launch callback`, () => {
    let launches = 0;
    const snapshot = kind === 'physical' ? host(budget.launchBytes - 1) : host(budget.launchBytes, budget.launchBytes - 1);
    assert.throws(() => { checkResourceBudget(snapshot, budget, 'launch', time); launches++; }, /below 4.25 GiB/);
    assert.equal(launches, 0);
  });
}
test('running permits exact 2 GiB for both counters', () => {
  checkResourceBudget(host(budget.stopBytes, budget.stopBytes), budget, 'running', time);
});
test('bad counters, missing inventory, workload and stale timestamps fail closed', () => {
  for (const changes of [
    {available_physical_bytes: null}, {system_commit_bytes: NaN}, {system_commit_limit_bytes: 0},
    {system_commit_bytes: 100, system_commit_limit_bytes: 50},
    {external_build_or_java_processes: undefined},
    {external_build_or_java_processes: [{ProcessId: 999, Name: 'UnrealEditor.exe'}]},
    {utc: new Date(time - 5001).toISOString()}, {utc: new Date(time + 1001).toISOString()},
  ]) assert.throws(() => checkResourceBudget({...host(), ...changes}, budget, 'launch', time));
});
test('read-only preflight uses fixed script; timeout and malformed responses do not retry', () => {
  let calls = 0;
  const snapshot = readHostResourceSnapshot(root, (exe, args, options) => {
    calls++;
    assert.ok(exe.endsWith('WindowsPowerShell\\v1.0\\powershell.exe'));
    assert.ok(args.at(-1).endsWith('read_rea_resource_preflight.ps1'));
    assert.equal(options.windowsHide, true);
    assert.equal(options.timeout, 10000);
    return {status: 0, stdout: JSON.stringify(host()), stderr: ''};
  });
  assert.equal(calls, 1);
  checkResourceBudget(snapshot, budget, 'launch', time);
  for (const result of [{error: {code:'ETIMEDOUT'}}, {status:1}, {status:0,stdout:'bad'}, {status:0,stdout:'{}',stderr:'failed'}]) {
    let attempts = 0;
    assert.throws(() => readHostResourceSnapshot(root, () => { attempts++; return result; }));
    assert.equal(attempts, 1);
  }
});

let sequence = 0;
function harness() {
  const child = new EventEmitter();
  child.pid = 70000 + ++sequence;
  child.stdout = new PassThrough(); child.stderr = new PassThrough();
  child.unref = () => {};
  let kills = 0, tick, clock = time, closed = false;
  child.kill = () => { kills++; return false; };
  const stopped = [];
  const output = path.join(outputs, String(sequence));
  fs.mkdirSync(output);
  const profile = startOwnedMemoryProfile({
    root, output, backendPid: fixture[0].root_pid, controllerPid: 40560, budget,
    onFailure: error => stopped.push({pid: fixture[0].root_pid, error: error.message}),
  }, {
    spawn: (_exe, args) => { assert.ok(args.includes('-RootProcessId')); assert.ok(args.includes(String(fixture[0].root_pid))); return child; },
    now: () => clock, setInterval: fn => { tick = fn; return 1; }, clearInterval: () => {},
    finishWaitMs: 1, terminationWaitMs: 1,
  });
  profile.ready.catch(() => {});
  const send = sample => child.stdout.write(JSON.stringify(sample) + '\n');
  const close = (code = 0) => { child.emit('exit', code); child.stdout.end(); child.stderr.end(); child.emit('close', code); closed = true; };
  return {profile, child, stopped, output, send, close, tick: () => tick(), setClock: value => {clock = value;}, kills: () => kills,
    async finish() { profile.beginFinalization(); profile.markBackendExited(); if (!closed) close(); return profile.finish(); }};
}

test('replays all 70 saved samples without launching a backend; peaks and heap remain attributable', async () => {
  const h = harness();
  for (const sample of fixture) {
    h.setClock(Date.parse(sample.utc));
    if (sample.live_owned_identity_count === 0) h.profile.beginFinalization();
    h.send(sample);
  }
  await h.profile.ready;
  h.profile.verifyExpectedHeap();
  const result = await h.finish();
  assert.equal(result.samples, 70);
  assert.equal(result.tree_private_commit_peak_bytes, 1004802048);
  assert.equal(result.with_controller_and_sampler_working_set_peak_bytes, 1316192256);
  assert.deepEqual(result.failures, []);
  assert.equal(result.sampler_output_drained, true);
  assert.equal(h.stopped.length, 0);
});
for (const kind of ['physical', 'commit']) {
  test(`${kind} below 2 GiB stops only the supplied backend identity once`, async () => {
    const h = harness(); h.send(base()); await h.profile.ready;
    const low = {...base(), ...host(budget.stopBytes, budget.stopBytes)};
    if (kind === 'physical') low.available_physical_bytes--;
    else low.system_commit_limit_bytes--;
    h.send(low); h.send(low);
    assert.equal(h.stopped.length, 1);
    assert.equal(h.stopped[0].pid, fixture[0].root_pid);
    assert.match(h.stopped[0].error, /below 2 GiB/);
    const result = await h.finish();
    assert.ok(result.failures.length > 0);
  });
}
for (const fault of ['external', 'stale', 'root-changed', 'missing-root', 'bad-count', 'heap-mismatch', 'invalid-json']) {
  test(`${fault} is latched and prevents healthy final status`, async () => {
    const h = harness(); h.send(base()); await h.profile.ready;
    const sample = base();
    if (fault === 'external') sample.external_build_or_java_processes = [{ProcessId: 99999, Name:'UnrealEditor.exe'}];
    if (fault === 'stale') sample.utc = new Date(time - 5001).toISOString();
    if (fault === 'root-changed') sample.root_creation_utc = new Date(time - 1000).toISOString();
    if (fault === 'missing-root') sample.counters = [];
    if (fault === 'bad-count') sample.counters[0].private_commit_bytes = -1;
    if (fault === 'heap-mismatch') sample.counters.push({...sample.counters.find(row=>row.role === 'owned_backend_tree'),pid:1234,name:'java.exe',java_heap_switches:['-Xmx512M']});
    if (fault === 'invalid-json') h.child.stdout.write('invalid\n'); else h.send(sample);
    assert.equal(h.stopped.length, 1);
    const result = await h.finish(); assert.ok(result.failures.length);
  });
}
test('missing or disconnected sampler rejects ready without extra stop calls', async () => {
  const h = harness();
  h.setClock(time + 15001); h.tick();
  await assert.rejects(h.profile.ready, /missing or stale/);
  h.tick();
  assert.equal(h.stopped.length, 1);
  await h.finish();
});
test('early exit and ended stream are failures', async () => {
  const h = harness(); h.send(base()); await h.profile.ready;
  h.close();
  const result = await h.profile.finish();
  assert.equal(h.stopped.length, 1);
  assert.ok(result.failures.some(message=>message.includes('early')));
});
test('normal root-only samples cannot establish the expected Java heap', async () => {
  const h = harness(); h.send(base()); await h.profile.ready;
  assert.throws(() => h.profile.verifyExpectedHeap(), /No owned Java/);
  await h.finish();
});
test('exit event before final output does not hide late parse errors or kill by an old PID', async () => {
  const h = harness(); h.send(base()); await h.profile.ready;
  h.profile.beginFinalization();
  h.profile.markBackendExited();
  h.child.emit('exit', 0);
  const resultPromise = h.profile.finish();
  h.child.stdout.write('broken-final-frame\n');
  h.child.stdout.end(); h.child.stderr.end(); h.child.emit('close', 0);
  const result = await resultPromise;
  assert.ok(result.failures.some(message=>message.includes('Owned memory monitoring')));
  assert.equal(result.sampler_output_drained, true);
  assert.equal(h.stopped.length, 0);
  assert.equal(h.kills(), 0);
});
test('late malformed counters and incomplete tail are retained', async () => {
  const h = harness(); h.send(base()); await h.profile.ready;
  h.profile.beginFinalization();
  h.profile.markBackendExited();
  const sample = base(); sample.counters[0].private_commit_bytes = null; h.send(sample);
  h.child.stdout.write('{"unfinished":');
  const result = await h.finish();
  assert.ok(result.failures.some(message=>message.includes('counters unavailable')));
  assert.ok(result.failures.some(message=>message.includes('incomplete output frame')));
  assert.equal(h.stopped.length, 0);
});
test('a sampler refusing to exit is bounded and explicitly uncertain', async () => {
  const h = harness(); h.send(base()); await h.profile.ready;
  h.profile.beginFinalization();
  const result = await h.profile.finish();
  assert.equal(h.kills(), 1);
  assert.equal(result.sampler_exit_confirmed, false);
  assert.equal(result.sampler_output_drained, false);
  assert.ok(result.failures.includes('Sampler exit unconfirmed'));
  assert.ok(result.failures.includes('Sampler output drain unconfirmed'));
});
for (const fault of ['commit', 'external', 'invalid-json', 'stale-monitor']) {
  test(`finalization still stops a live owned backend on ${fault}`, async () => {
    const h = harness(); h.send(base()); await h.profile.ready;
    h.profile.beginFinalization();
    const sample = base();
    if (fault === 'commit') sample.system_commit_limit_bytes = sample.system_commit_bytes + budget.stopBytes - 1;
    if (fault === 'external') sample.external_build_or_java_processes = [{ProcessId:99999,Name:'UnrealEditor.exe'}];
    if (fault === 'invalid-json') h.child.stdout.write('bad-tail\n');
    else if (fault === 'stale-monitor') { h.setClock(time + 15001); h.tick(); }
    else h.send(sample);
    assert.equal(h.stopped.length, 1);
    h.profile.markBackendExited();
    const result = await h.finish();
    assert.ok(result.failures.length > 0);
  });
}
test('after confirmed backend exit, a late budget failure is recorded without another stop', async () => {
  const h = harness(); h.send(base()); await h.profile.ready;
  h.profile.beginFinalization(); h.profile.markBackendExited();
  const sample = base(); sample.available_physical_bytes = budget.stopBytes - 1; h.send(sample);
  const result = await h.finish();
  assert.equal(h.stopped.length, 0);
  assert.ok(result.failures.some(message=>message.includes('below 2 GiB')));
});
