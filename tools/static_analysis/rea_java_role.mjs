import {win32 as path} from 'node:path';
import {root, runtime, javaHome, ghidraHome} from './rea_project_environment.mjs';

// Pure argv classification for the three pinned Engine/Game/client target profiles. This module
// does not inspect processes, establish ownership, read files or launch Java.
// Call only AFTER native job membership + PID creation identity are verified.
// args must be reliably decoded Windows tokens, WITHOUT argv[0]; never split
// a command line on spaces or pass an observed command as the expected policy.
//
// The caller must also retain the pinned launch environment: GhidraInstallation
// clears _JAVA_OPTIONS/JAVA_TOOL_OPTIONS/JDK_JAVA_OPTIONS/GHIDRA_JAVA_OPTIONS;
// GhidraLauncher sets only isolated home/tmp and -XX:-UsePerfData in the Windows
// JDK_JAVA_OPTIONS. An argv match does not authenticate that environment, binary
// contents, filesystem aliases, session descriptor or process ownership.
const javaExe = path.join(javaHome, 'bin', 'java.exe');
const launchJar = path.join(ghidraHome, 'support', 'LaunchSupport.jar');
const utilityJar = path.join(ghidraHome, 'Ghidra', 'Framework', 'Utility', 'lib', 'Utility.jar');
const bridge = path.join(root, '.tooling', 'rea', 'node_modules', 'rea-agents',
  'bridge', 'ghidra', 'ReaGhidraBridge.java');

export const javaRoleProfile = Object.freeze({
  javaExe, javaHome, ghidraHome, runtime, bridge, heap: '384M',
  targetSha256: '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153',
});
const engineTarget = Object.freeze({name: 'engine',
  sha256: '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153',
  snapshotName: 'target-4c2ea14ce1c5.dll'});
const gameTarget = Object.freeze({name: 'game',
  sha256: 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2',
  snapshotName: 'target-cd7ecd7942d7.dll'});
const clientTarget = Object.freeze({name: 'client',
  sha256: 'd3915e2599317b6670cc088ba958afb2b2f4fa8141cfd1d777ee02f9ea1fc4c4',
  snapshotName: 'target-d3915e259931.exe'});

// Selection comes from the fixed runner, never an observed descriptor or argv.
export function resolveJavaTargetProfile(name = 'engine') {
  if (name === 'engine') return engineTarget;
  if (name === 'game') return gameTarget;
  if (name === 'client') return clientTarget;
  throw Error('UnknownJavaTargetProfile');
}

// Pure target/provider check; filesystem identity and freshness remain the
// monitor's responsibility. Credentials are neither returned nor retained.
export function assertJavaSessionTarget(session, profileName = 'engine') {
  const target = resolveJavaTargetProfile(profileName);
  if (session?.target_sha256 !== target.sha256 || session.provider_version !== '12.1.4' ||
      typeof session.run_id !== 'string') throw Error('Java role runtime does not match the fixed target session');
}

// Active VMARGS and VMARGS_WINDOWS in the installed 12.1.4 launch.properties.
// Their order is immaterial only because the set is exact and has no duplicate
// properties. No arbitrary -D/-XX/-X/module/agent options are admitted.
const releaseVmArgs = Object.freeze([
  '-Djava.system.class.loader=ghidra.GhidraClassLoader',
  '-Dfile.encoding=UTF8',
  '-Duser.country=US',
  '-Duser.language=en',
  '-Duser.variant=',
  '-Dsun.java2d.opengl=false',
  '-Dsun.java2d.d3d=false',
  '-Djdk.tls.client.protocols=TLSv1.2,TLSv1.3',
  '-Dcpu.core.limit=',
  '-Dcpu.core.override=',
  '-Dfont.size.override=',
  '-Dpython.console.encoding=UTF-8',
  '-Dlog4j.skipJansi=true',
  '-Xshare:off',
  '-Djavax.xml.accessExternalDTD=',
  '-Djavax.xml.accessExternalSchema=',
  '-Djavax.xml.accessExternalStylesheet=',
  '--enable-native-access=ALL-UNNAMED',
]);
const installationProbe = Object.freeze([
  '-Dstdout.encoding=UTF-8', '-Dstderr.encoding=UTF-8',
  '-Dsun.stdout.encoding=UTF-8', '-Dsun.stderr.encoding=UTF-8',
  '-XX:-UsePerfData', '-XshowSettings:properties', '-version',
]);

function absoluteWindowsPath(value) {
  if (typeof value !== 'string' || !/^[A-Za-z]:[\\/]/.test(value) ||
      /[\x00-\x1f"<>|?*]/.test(value) || value.slice(2).includes(':')) return null;
  const normalized = path.normalize(value);
  // Reject Win32 trailing-dot/space aliases rather than invent filesystem
  // equivalence. support\.. is an actual launch.bat spelling and normalizes.
  if (normalized.split('\\').slice(1).some(part => /[. ]$/.test(part))) return null;
  return normalized.toLowerCase();
}
function samePath(actual, expected) {
  const normalized = absoluteWindowsPath(actual);
  return normalized !== null && normalized === absoluteWindowsPath(expected);
}
function sameToken(actual, expected) {
  if (/^[A-Za-z]:[\\/]/.test(expected)) return samePath(actual, expected);
  for (const prefix of ['-Duser.home=', '-Djava.io.tmpdir=']) {
    if (expected.startsWith(prefix)) {
      return actual.startsWith(prefix) && samePath(actual.slice(prefix.length), expected.slice(prefix.length));
    }
  }
  return actual === expected;
}
function matches(actual, expected) {
  return actual.length === expected.length && actual.every((token, i) => sameToken(token, expected[i]));
}
function validTokens(args) {
  if (!Array.isArray(args) || args.length === 0 || args.length > 128) return false;
  let total = 0;
  for (let i = 0; i < args.length; i++) {
    // Array.every/reduce skip holes. Every argv index must be an own, valid
    // string token; inherited prototype values do not supply missing tokens.
    if (!Object.hasOwn(args, i)) return false;
    const token = args[i];
    if (typeof token !== 'string' || token.length === 0 || /[\x00-\x1f"]/.test(token)) return false;
    total += token.length + 1;
    if (total > 32767) return false;
  }
  return true;
}
function sessionRoot(context) {
  if (!context || typeof context !== 'object') return null;
  if (context.javaHome !== undefined && !samePath(context.javaHome, javaHome)) return null;
  if (context.ghidraHome !== undefined && !samePath(context.ghidraHome, ghidraHome)) return null;
  const value = absoluteWindowsPath(context.runtimeRoot);
  if (value === null || !samePath(path.dirname(value), runtime) ||
      !/^rea-ghidra-[0-9a-f]{16,64}$/.test(path.basename(value))) return null;
  return value;
}

// context.runtimeRoot comes from the caller's separately verified, unique
// session (run_id, pinned target digest and directory identity), not argv.
// Optional javaHome/ghidraHome may only restate the pinned project values.
// Optional targetProfile selects only the three fixed snapshot identities above;
// no other context fields widen the accepted argument grammar.
export function expectedHeadlessArguments(context) {
  const run = sessionRoot(context);
  if (run === null) throw new Error('InvalidJavaRoleContext');
  const target = resolveJavaTargetProfile(context.targetProfile);
  return [
    path.join(run, 'project'), 'rea-project',
    '-import', path.join(run, target.snapshotName),
    '-readOnly', '-deleteProject',
    '-log', path.join(run, 'ghidra.log'),
    '-scriptlog', path.join(run, 'script.log'),
    '-scriptPath', path.dirname(bridge),
    '-postScript', bridge, path.join(run, 'session.json'),
  ];
}

// Returns no ownership grant and never exempts helpers from memory accounting.
// heapArgument records an argv check, not an observed effective JVM heap limit.
export function validateJavaRole(observation, context = {}) {
  const result = (ok, role, reason, phase = null, launch = null) => ({
    ok, role, reason, phase, launch, countTowardMemory: true,
    heapArgument: ok && role === 'analyze-headless' ? '-Xmx384M' : null,
  });
  const reject = reason => result(false, null, reason);
  if (observation?.strictOwnershipVerified !== true) return reject('StrictOwnershipRequired');
  if (!samePath(observation.exe, javaExe)) return reject('UnpinnedJavaExecutable');
  const args = observation.args;
  if (!validTokens(args)) return reject('InvalidJavaArgumentTokens');
  if (!context || typeof context !== 'object' || Array.isArray(context)) return reject('InvalidJavaRoleContext');
  try { resolveJavaTargetProfile(context.targetProfile); }
  catch { return reject('UnknownJavaTargetProfile'); }
  if (context.javaHome !== undefined && !samePath(context.javaHome, javaHome) ||
      context.ghidraHome !== undefined && !samePath(context.ghidraHome, ghidraHome)) {
    return reject('InvalidJavaRoleContext');
  }
  if (matches(args, ['-version'])) return result(true, 'version-probe', null, 'version', 'windows-batch');
  if (matches(args, installationProbe)) return result(true, 'version-probe', null, 'properties-version', 'installation-probe');

  const run = sessionRoot(context);
  if (run === null) return reject('InvalidJavaRoleContext');
  const isolated = [`-Duser.home=${path.join(run, 'home')}`, `-Djava.io.tmpdir=${path.join(run, 'tmp')}`];
  const support = ['-cp', launchJar, 'LaunchSupport', ghidraHome];
  for (const phase of ['-envvars', '-vmargs']) {
    if (matches(args, [...support, phase])) {
      return result(true, 'launch-support', null, phase.slice(1), 'windows-batch');
    }
    for (const logs of [[], ['-Xlog:disable', '-Xlog:all=warning:stderr']]) {
      if (matches(args, [...isolated, ...logs, ...support, phase])) {
        return result(true, 'launch-support', null, phase.slice(1), 'direct-helper');
      }
    }
  }
  // analyzeHeadless.bat fixes java-type=jdk. -ask is interactive and is not
  // admitted; -java_home is a different branch, not this profile. -save uses
  // the caller's already isolated configuration directory, not global setup.
  if (matches(args, [...support, '-jdk_home', '-save'])) {
    return result(true, 'launch-support', null, 'jdk-home-save', 'windows-batch');
  }

  const classpath = args.indexOf('-cp');
  if (classpath < 0 || !samePath(args[classpath + 1], utilityJar) ||
      args[classpath + 2] !== 'ghidra.Ghidra' ||
      args[classpath + 3] !== 'ghidra.app.util.headless.AnalyzeHeadless') {
    return reject('UnknownJavaInvocation');
  }
  const heap = args.filter(token => token.startsWith('-Xmx'));
  if (heap.length !== 1 || heap[0] !== '-Xmx384M') return reject('MainHeapMustBeExactly384M');
  const common = args.slice(0, releaseVmArgs.length);
  if (common.length !== releaseVmArgs.length || new Set(common).size !== releaseVmArgs.length ||
      !releaseVmArgs.every(token => common.includes(token))) return reject('UnexpectedReleaseVmArguments');
  const tail = args.slice(releaseVmArgs.length);
  const app = ['-cp', utilityJar, 'ghidra.Ghidra', 'ghidra.app.util.headless.AnalyzeHeadless',
    ...expectedHeadlessArguments(context)];
  const threads = ['-XX:ParallelGCThreads=2', '-XX:CICompilerCount=2'];
  if (matches(tail, [isolated[0], '-Xmx384M', ...threads, ...app])) {
    return result(true, 'analyze-headless', null, 'analysis', 'windows-batch');
  }
  if (matches(tail, ['-Xmx384M', ...threads, '-Djava.awt.headless=true',
    ...isolated, '-showversion', ...app])) {
    return result(true, 'analyze-headless', null, 'analysis', 'direct-main');
  }
  return reject('UnexpectedHeadlessArguments');
}
