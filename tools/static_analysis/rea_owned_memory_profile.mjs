import fs from 'node:fs';
import path from 'node:path';
import {spawn} from 'node:child_process';
import {calibrationBudget, checkResourceBudget} from './rea_resource_budget.mjs';
import {validateJavaRole, resolveJavaTargetProfile, assertJavaSessionTarget} from './rea_java_role.mjs';

export function startOwnedMemoryProfile({root, output, backendPid, controllerPid, onFailure, budget = calibrationBudget, ownedJob, targetProfile = 'engine'}, dependencies = {}) {
  const fixedTarget = resolveJavaTargetProfile(targetProfile); // Reject before sampler spawn.
  const spawnProcess = dependencies.spawn ?? spawn;
  const now = dependencies.now ?? Date.now;
  const schedule = dependencies.setInterval ?? setInterval;
  const unschedule = dependencies.clearInterval ?? clearInterval;
  const file = path.join(output, 'owned-memory.ndjson');
  const executable = path.join(process.env.SystemRoot, 'System32/WindowsPowerShell/v1.0/powershell.exe');
  const args = ['-NoLogo', '-NoProfile', '-NonInteractive', '-File', path.join(root, ownedJob ? 'tools/static_analysis/measure_job_process_memory.ps1' : 'tools/static_analysis/measure_owned_process_memory.ps1'), '-RootProcessId', String(backendPid), '-ControllerProcessId', String(controllerPid), '-DurationSeconds', '600', '-IntervalMilliseconds', '1000', '-OutputFile', file];
  if (ownedJob) args.push('-RunNonce', ownedJob.nonce, '-NodeExecutable', ownedJob.nodeExecutable);
  else args.push('-StreamSamples');
  const sampler = spawnProcess(executable, args, {cwd: root, windowsHide: true, stdio: [ownedJob ? 'pipe' : 'ignore', 'pipe', 'pipe']});
  let buffer = '', stderr = '', lastReceived = 0, finishing = false, backendExited = false, stopIssued = false, exited = false, closed = false, exitCode = null, rootCreation = null, jobEmpty = false, cachedJavaContext = null, emergencyStopTimer = null;
  let resolveReady, rejectReady;
  const ready = new Promise((resolve, reject) => { resolveReady = resolve; rejectReady = reject; });
  const state = {sampler_pid: sampler.pid, budget, ownership_policy: ownedJob ? 'windows-job-v1' : 'legacy-cim-ancestry', expected_heap_observed: false, job_empty_confirmed: false, requested_interval_ms: 1000, samples: 0, last_sample_utc: null, tree_private_commit_peak_bytes: 0, tree_working_set_peak_bytes: 0, with_controller_and_sampler_private_peak_bytes: 0, with_controller_and_sampler_working_set_peak_bytes: 0, minimum_available_physical_bytes: null, minimum_commit_headroom_bytes: null, largest_receipt_gap_ms: 0, counter_read_gap_count: 0, processes: {}, failures: []};
  const protectionNeeded = () => ownedJob ? !jobEmpty : !backendExited;
  function javaContext() {
    if (dependencies.javaContext) {
      const context = dependencies.javaContext();
      if (context?.targetProfile !== undefined && context.targetProfile !== fixedTarget.name) throw Error('Java context target profile mismatch');
      return {...context, targetProfile: fixedTarget.name};
    }
    if (cachedJavaContext) return cachedJavaContext;
    const runtimeBase = path.join(root, 'analysis-runtime/rea');
    const candidates = fs.readdirSync(runtimeBase, {withFileTypes: true}).filter(item => item.isDirectory() && /^rea-ghidra-[a-f0-9]+$/i.test(item.name));
    if (candidates.length === 0) return {targetProfile: fixedTarget.name};
    if (candidates.length !== 1) throw Error('Java role requires exactly one task runtime');
    const runtimeRoot = path.join(runtimeBase, candidates[0].name);
    if (fs.lstatSync(runtimeRoot).isSymbolicLink()) throw Error('Task runtime cannot be a link');
    const descriptor = path.join(runtimeRoot, 'session.json');
    if (!fs.existsSync(descriptor)) return {targetProfile: fixedTarget.name};
    if (fs.lstatSync(descriptor).isSymbolicLink()) throw Error('Task descriptor cannot be a link');
    const session = JSON.parse(fs.readFileSync(descriptor, 'utf8'));
    assertJavaSessionTarget(session, fixedTarget.name);
    if (fs.statSync(descriptor).mtimeMs < Date.parse(rootCreation) - 1) throw Error('Task descriptor predates the owned root');
    cachedJavaContext = {runtimeRoot, targetProfile: fixedTarget.name}; // Never retain or emit session credentials.
    return cachedJavaContext;
  }
  function fail(message) {
    if (!state.failures.includes(message)) state.failures.push(message);
    rejectReady(Error(message));
    if (protectionNeeded() && !stopIssued) { stopIssued = true; onFailure(Error(message)); }
  }
  const started = now();
  const timer = schedule(() => {
    if (protectionNeeded() && now() - (lastReceived || started) > budget.staleAfterMs) fail('Owned memory sampler is missing or stale for more than 15 seconds');
  }, 1000);
  sampler.on('error', error => fail(`Memory sampler process: ${error.message}`));
  sampler.stdout.setEncoding('utf8');
  sampler.stdout.on('data', chunk => {
    buffer += chunk;
    if (buffer.length > 1024 * 1024) { fail('Sampler frame exceeds 1 MiB'); buffer = ''; return; }
    let newline;
    while ((newline = buffer.indexOf('\n')) >= 0) {
      const line = buffer.slice(0, newline).trim(); buffer = buffer.slice(newline + 1);
      if (!line) continue;
      try {
        const sample = JSON.parse(line);
        if (sample.root_pid !== backendPid || !Array.isArray(sample.counters)) throw Error('Sampler identity/schema mismatch');
        if (!Number.isFinite(Date.parse(sample.root_creation_utc))) throw Error('Root creation identity unavailable');
        rootCreation ??= sample.root_creation_utc;
        if (sample.root_creation_utc !== rootCreation) throw Error('Backend root creation identity changed');
        if (![sample.available_physical_bytes, sample.system_commit_bytes, sample.system_commit_limit_bytes].every(value => Number.isFinite(value) && value >= 0) || sample.system_commit_limit_bytes <= 0 || sample.system_commit_bytes > sample.system_commit_limit_bytes) throw Error('System memory counters unavailable');
        if (sample.counters.some(row => !['private_commit_bytes', 'working_set_bytes', 'lifetime_peak_commit_bytes_observed', 'lifetime_peak_working_set_bytes_observed', 'thread_count'].every(key => Number.isFinite(row[key]) && row[key] >= 0))) throw Error('Process memory counters unavailable');
        if (!Array.isArray(sample.counter_read_gaps) || !Array.isArray(sample.external_build_or_java_processes)) throw Error('Sampler inventory/schema unavailable');
        if (sample.counters.some(row => !Number.isSafeInteger(row.pid) || row.pid < 1 || !Number.isFinite(Date.parse(row.creation_utc)) || !Array.isArray(row.java_heap_switches) || !['owned_backend_tree','controller','sampler'].includes(row.role))) throw Error('Process identity/schema unavailable');
        if (new Set(sample.counters.map(row => row.pid)).size !== sample.counters.length) throw Error('Duplicate process PID in one counter snapshot');
        if (sample.counters.some(row => row.pid === backendPid && row.creation_utc !== rootCreation)) throw Error('Counter root creation conflicts with the pinned root');
        if (ownedJob) {
          if (sample.job?.policy !== 'windows-job-v1' || sample.job?.nonce !== ownedJob.nonce || sample.job.root_assigned !== true || !Number.isSafeInteger(sample.job.active_processes) || sample.job.active_processes < 0 || !Number.isSafeInteger(sample.job.probed_member_count) || sample.job.probed_member_count < 0 || typeof sample.job.empty_confirmed !== 'boolean') throw Error('Explicit Job identity/schema unavailable');
          if (sample.job.empty_confirmed !== (sample.job.active_processes === 0 && sample.job.probed_member_count === 0) || sample.live_owned_identity_count !== sample.job.active_processes) throw Error('Job emptiness counters disagree');
          if (sample.counters.some(row => row.role === 'owned_backend_tree' && row.ownership !== 'explicit-kernel-job')) throw Error('Owned process lacks explicit Job membership');
          if (sample.counters.some(row => ['sampler','controller'].includes(row.role) && row.ownership !== 'monitor-or-controller')) throw Error('Monitor unexpectedly belongs to backend Job');
          if (sample.job.controller_exit_observed) throw Error('Controller exit was observed by Job watchdog');
          if (jobEmpty && !sample.job.empty_confirmed) throw Error('Job inventory contradicts the confirmed empty terminal state');
          jobEmpty = sample.job.empty_confirmed;
          state.job_empty_confirmed = jobEmpty;
        }
        const receivedAt = now(); if (lastReceived) state.largest_receipt_gap_ms = Math.max(state.largest_receipt_gap_ms, receivedAt - lastReceived); lastReceived = receivedAt;
        state.samples++; state.last_sample_utc = sample.utc;
        const rows = sample.counters;
        const owned = rows.filter(row => row.role === 'owned_backend_tree');
        const sum = (list, key) => list.reduce((total, row) => total + Number(row[key]), 0);
        state.tree_private_commit_peak_bytes = Math.max(state.tree_private_commit_peak_bytes, sum(owned, 'private_commit_bytes'));
        state.tree_working_set_peak_bytes = Math.max(state.tree_working_set_peak_bytes, sum(owned, 'working_set_bytes'));
        state.with_controller_and_sampler_private_peak_bytes = Math.max(state.with_controller_and_sampler_private_peak_bytes, sum(rows, 'private_commit_bytes'));
        state.with_controller_and_sampler_working_set_peak_bytes = Math.max(state.with_controller_and_sampler_working_set_peak_bytes, sum(rows, 'working_set_bytes'));
        state.minimum_available_physical_bytes = Math.min(state.minimum_available_physical_bytes ?? Infinity, sample.available_physical_bytes);
        const commitHeadroom = sample.system_commit_limit_bytes - sample.system_commit_bytes;
        state.minimum_commit_headroom_bytes = Math.min(state.minimum_commit_headroom_bytes ?? Infinity, commitHeadroom);
        state.counter_read_gap_count += (sample.counter_read_gaps ?? []).length;
        for (const row of rows) {
          if (row.role === 'owned_backend_tree' && /^javaw?\.exe$/i.test(row.name)) {
            if (ownedJob) {
              const role = validateJavaRole({strictOwnershipVerified: row.ownership === 'explicit-kernel-job', exe: row.executable_path, args: row.java_arguments}, javaContext());
              if (!role.ok) throw Error(`Owned Java launch role rejected: ${role.reason}`);
              row.validated_java_role = role.role;
              if (role.role === 'analyze-headless') state.expected_heap_observed = true;
            } else {
              const maxHeap = row.java_heap_switches.filter(value => value.startsWith('-Xmx'));
              if (maxHeap.length !== 1 || maxHeap[0] !== `-Xmx${budget.heap}`) throw Error('Owned Java heap differs from the fixed budget');
              state.expected_heap_observed = true;
            }
          }
          const key = `${row.pid}:${row.creation_utc}`;
          const item = state.processes[key] ??= {pid: row.pid, parent_pid: row.parent_pid, name: row.name, role: row.role, creation_utc: row.creation_utc, private_commit_sample_peak_bytes: 0, lifetime_commit_peak_observed_bytes: 0, working_set_sample_peak_bytes: 0, lifetime_working_set_peak_observed_bytes: 0, maximum_threads_observed: 0, java_heap_switches: []};
          for (const [out, input] of [['private_commit_sample_peak_bytes', 'private_commit_bytes'], ['lifetime_commit_peak_observed_bytes', 'lifetime_peak_commit_bytes_observed'], ['working_set_sample_peak_bytes', 'working_set_bytes'], ['lifetime_working_set_peak_observed_bytes', 'lifetime_peak_working_set_bytes_observed'], ['maximum_threads_observed', 'thread_count']]) item[out] = Math.max(item[out], row[input]);
          if (Array.isArray(row.java_heap_switches) && row.java_heap_switches.length) item.java_heap_switches = row.java_heap_switches;
          if (row.validated_java_role) item.validated_java_role = row.validated_java_role;
        }
        if (!finishing && !owned.some(row => row.pid === backendPid && row.creation_utc === rootCreation)) throw Error('Live backend root missing or changed in memory counters');
        checkResourceBudget(sample, budget, 'running', now());
        if (!finishing) resolveReady();
      } catch (error) { fail(`Owned memory monitoring: ${error.message}`); }
    }
  });
  sampler.stderr.setEncoding('utf8');
  sampler.stderr.on('data', chunk => { if (stderr.length < 32768) stderr += chunk.slice(0, 32768 - stderr.length); });
  if (ownedJob) sampler.stdin.on('error', error => { if (protectionNeeded()) fail(`Job control pipe: ${error.message}`); });
  sampler.stdout.on('error', error => fail(`Memory sampler stdout: ${error.message}`));
  sampler.stderr.on('error', error => fail(`Memory sampler stderr: ${error.message}`));
  sampler.stdout.on('end', () => { if (!finishing) fail('Memory sampler stdout ended during analysis'); });
  sampler.on('exit', code => { exited = true; exitCode = code; if (!finishing || code !== 0) fail(`Memory sampler exited ${finishing ? 'abnormally' : 'early'} (${code})`); });
  sampler.on('close', () => {
    closed = true;
    if (!finishing) fail('Memory sampler closed during analysis');
    // exit can precede the final empty frame. Decide after stdout is drained.
    if (ownedJob && !jobEmpty) fail('Memory sampler closed without confirmed empty Job');
  });
  const waitClosed = ms => new Promise(resolve => {
    if (closed) { resolve(true); return; }
    const onClose = () => { clearTimeout(timeout); resolve(true); };
    const timeout = setTimeout(() => { sampler.off('close', onClose); resolve(false); }, ms);
    sampler.once('close', onClose);
  });
  return {
    ready,
    requestStop() {
      if (!ownedJob) throw Error('Explicit Job stop unavailable for legacy sampler');
      if (jobEmpty || exited) return;
      state.job_stop_requested = true;
      try { sampler.stdin.write(JSON.stringify({command: 'stop-owned-job', nonce: ownedJob.nonce}) + '\n'); }
      catch (error) { state.failures.push(`Job stop pipe failed: ${error.message}`); sampler.kill(); }
      emergencyStopTimer ??= setTimeout(() => {
        if (!exited) { state.job_monitor_terminated_for_cleanup = true; sampler.kill(); }
      }, dependencies.jobStopWaitMs ?? 2500);
    },
    verifyExpectedHeap() {
      if (!state.expected_heap_observed) throw Error('No owned Java process with the fixed heap was observed');
    },
    beginFinalization() { finishing = true; },
    markBackendExited() { backendExited = true; },
    async finish() {
      finishing = true; unschedule(timer);
      await waitClosed(dependencies.finishWaitMs ?? 5000);
      if (!closed) {
        if (!exited) { sampler.kill(); state.failures.push('Sampler required termination after backend finalization'); }
        else state.failures.push('Sampler exited but output streams did not close within 5 seconds');
        await waitClosed(dependencies.terminationWaitMs ?? 2000);
      }
      if (!exited) state.failures.push('Sampler exit unconfirmed');
      if (ownedJob && !jobEmpty) state.failures.push('Explicit Job empty state unconfirmed; monitor close requested owned Job cleanup');
      if (!closed) state.failures.push('Sampler output drain unconfirmed');
      if (buffer.trim()) state.failures.push('Sampler ended with an incomplete output frame');
      if (exitCode !== 0) state.failures.push(`Sampler exit code ${exitCode}`);
      sampler.stdout.destroy(); sampler.stderr.destroy(); sampler.unref();
      if (ownedJob) sampler.stdin.destroy();
      if (emergencyStopTimer) clearTimeout(emergencyStopTimer);
      state.sampler_exit_confirmed = exited; state.sampler_exit_code = exitCode; state.sampler_output_drained = closed;
      state.processes = Object.values(state.processes);
      state.limitations = ['Sampled tree sums are not continuous peaks; short-lived processes or the last interval before exit may be missed.', 'Working sets may contain shared pages. Sum of per-process lifetime peaks is not simultaneous tree usage.', 'Controller and PowerShell sampler overhead are recorded separately and also included in total task peaks.'];
      fs.writeFileSync(path.join(output, 'owned-memory-summary.json'), JSON.stringify(state, null, 2) + '\n', {flag: 'wx'});
      fs.writeFileSync(path.join(output, 'owned-memory-sampler-stderr.txt'), stderr, {flag: 'wx'});
      return state;
    },
  };
}
