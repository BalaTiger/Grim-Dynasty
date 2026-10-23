import path from 'node:path';
import {spawnSync} from 'node:child_process';

const GiB = 1024 ** 3;
export const parserBudget = Object.freeze({
  id: 'engine-parser-384m-v1', heap: '384M',
  launchBytes: 4.25 * GiB, stopBytes: 2 * GiB,
  maximumSampleAgeMs: 5000, staleAfterMs: 15000,
  taskAllowanceGiB: 2.25, systemSpareGiB: 2,
});
export const calibrationBudget = Object.freeze({
  id: 'engine-subobject-calibration-384m-v1', heap: '384M',
  launchBytes: 6 * GiB, stopBytes: 5 * GiB,
  maximumSampleAgeMs: 5000, staleAfterMs: 15000,
});
// First Game measurement: provisional allowance, not a previously measured peak.
export const gameMouseCalibrationBudget = Object.freeze({
  id: 'game-mouse-first-calibration-384m-v1', heap: '384M',
  launchBytes: 5 * GiB, stopBytes: 2 * GiB,
  maximumSampleAgeMs: 5000, staleAfterMs: 15000,
  taskAllowanceGiB: 3, systemSpareGiB: 2,
  calibrationOnly: true, measuredGamePeakAvailable: false,
});
// First client EXE measurement: independent provisional allowance, no inherited
// Engine/Game peak measurement. These are admission/stop gates, not a usage cap.
export const clientMouseCalibrationBudget = Object.freeze({
  id: 'client-mouse-first-calibration-384m-v1', heap: '384M',
  launchBytes: 5 * GiB, stopBytes: 2 * GiB,
  maximumSampleAgeMs: 5000, staleAfterMs: 15000,
  taskAllowanceGiB: 3, systemSpareGiB: 2,
  calibrationOnly: true, measuredClientPeakAvailable: false,
});

export function checkResourceBudget(snapshot, budget, phase, now = Date.now()) {
  if (!['launch', 'running'].includes(phase)) throw Error('Unknown resource-check phase');
  const values = [snapshot?.available_physical_bytes, snapshot?.system_commit_bytes, snapshot?.system_commit_limit_bytes];
  if (!values.every(value => Number.isSafeInteger(value) && value >= 0) ||
      values[2] <= 0 || values[1] > values[2]) throw Error('System memory counters unavailable');
  const age = now - Date.parse(snapshot.utc);
  if (!Number.isFinite(age) || age < -1000 || age > budget.maximumSampleAgeMs) throw Error('Resource sample timestamp is invalid or stale');
  if (!Array.isArray(snapshot.external_build_or_java_processes)) throw Error('External workload inventory unavailable');
  if (snapshot.external_build_or_java_processes.length) throw Error('Another UE/build/Java process is active');
  const threshold = phase === 'launch' ? budget.launchBytes : budget.stopBytes;
  const commitHeadroom = values[2] - values[1];
  if (values[0] < threshold || commitHeadroom < threshold) {
    throw Error(`${phase} physical or commit headroom below ${threshold / GiB} GiB`);
  }
  return {availableBytes: values[0], commitHeadroomBytes: commitHeadroom};
}

// A read-only host query, before any Rea process. No vendor code is imported.
export function readHostResourceSnapshot(root, run = spawnSync) {
  const executable = path.join(process.env.SystemRoot, 'System32/WindowsPowerShell/v1.0/powershell.exe');
  const result = run(executable, ['-NoLogo', '-NoProfile', '-NonInteractive', '-File',
    path.join(root, 'tools/static_analysis/read_rea_resource_preflight.ps1')],
  {cwd: root, windowsHide: true, timeout: 10000, maxBuffer: 1024 * 1024, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe']});
  if (result.error || result.status !== 0 || result.signal) throw Error(`Read-only resource preflight failed: ${result.error?.code ?? result.status ?? result.signal}`);
  if (result.stderr?.trim()) throw Error('Read-only resource preflight produced diagnostics');
  try { return JSON.parse(result.stdout); }
  catch { throw Error('Read-only resource preflight returned invalid JSON'); }
}
