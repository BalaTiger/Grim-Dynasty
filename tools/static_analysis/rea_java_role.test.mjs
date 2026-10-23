import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {win32 as path} from 'node:path';
import {validateJavaRole, expectedHeadlessArguments, javaRoleProfile as pinned} from './rea_java_role.mjs';

// Only pure Node tests and reads of already installed configuration/source.
// No child process, Java, Rea, native process query, or target binary is used.
const run = path.join(pinned.runtime, 'rea-ghidra-53b4bee510ad6a815d00092a8ab074ec');
const context = Object.freeze({runtimeRoot: run, javaHome: pinned.javaHome, ghidraHome: pinned.ghidraHome});
const home = path.join(run, 'home'), temp = path.join(run, 'tmp');
const supportJar = path.join(pinned.ghidraHome, 'support', 'LaunchSupport.jar');
const utilityJar = path.join(pinned.ghidraHome, 'Ghidra', 'Framework', 'Utility', 'lib', 'Utility.jar');
const installed = path.dirname(path.dirname(path.dirname(pinned.bridge)));
const launchProperties = fs.readFileSync(path.join(pinned.ghidraHome, 'support', 'launch.properties'), 'utf8');
const releaseVmArgs = launchProperties.split(/\r?\n/)
  .filter(line => /^VMARGS(?:_WINDOWS)?=/.test(line)).map(line => line.slice(line.indexOf('=') + 1));
const launchBatch = fs.readFileSync(path.join(pinned.ghidraHome, 'support', 'launch.bat'), 'utf8');
const supportSource = fs.readFileSync(path.join(installed, 'dist', 'ghidra', 'GhidraJavaLaunch.js'), 'utf8');
const installationSource = fs.readFileSync(path.join(installed, 'dist', 'ghidra', 'GhidraInstallation.js'), 'utf8');
const providerSource = fs.readFileSync(path.join(installed, 'dist', 'ghidra', 'GhidraProviderClient.js'), 'utf8');
const threads = ['-XX:ParallelGCThreads=2', '-XX:CICompilerCount=2'];
const isolated = [`-Duser.home=${home}`, `-Djava.io.tmpdir=${temp}`];
const app = [
  path.join(run, 'project'), 'rea-project', '-import', path.join(run, 'target-4c2ea14ce1c5.dll'),
  '-readOnly', '-deleteProject', '-log', path.join(run, 'ghidra.log'),
  '-scriptlog', path.join(run, 'script.log'), '-scriptPath', path.dirname(pinned.bridge),
  '-postScript', pinned.bridge, path.join(run, 'session.json'),
];
const mainClass = ['-cp', utilityJar, 'ghidra.Ghidra', 'ghidra.app.util.headless.AnalyzeHeadless'];
const support = ['-cp', supportJar, 'LaunchSupport', path.join(pinned.ghidraHome, 'support', '..')];
const batchMain = () => [...releaseVmArgs, isolated[0], '-Xmx384M', ...threads, ...mainClass, ...app];
const directMain = () => [...releaseVmArgs, '-Xmx384M', ...threads,
  '-Djava.awt.headless=true', ...isolated, '-showversion', ...mainClass, ...app];
const observe = args => ({strictOwnershipVerified: true, exe: pinned.javaExe, args});
const check = (args, ctx = context) => validateJavaRole(observe(args), ctx);
const fail = (args, reason) => {
  const result = check(args);
  assert.equal(result.ok, false);
  assert.equal(result.role, null);
  assert.equal(result.countTowardMemory, true);
  assert.equal(result.heapArgument, null);
  if (reason) assert.equal(result.reason, reason);
};
const accept = (args, role, phase, launch) => {
  const result = check(args);
  assert.equal(result.ok, true, result.reason);
  assert.equal(result.role, role);
  assert.equal(result.phase, phase);
  assert.equal(result.launch, launch);
  assert.equal(result.countTowardMemory, true);
  assert.equal(result.heapArgument, role === 'analyze-headless' ? '-Xmx384M' : null);
};

test('fixtures match installed helper commands and bridge source location', () => {
  assert.match(launchBatch, /java\.exe -version >NUL 2>&1/);
  assert.match(launchBatch, /LaunchSupport "%INSTALL_DIR%" %JAVA_TYPE_ARG% -save/);
  assert.match(supportSource, /supportOutput\("-envvars"\)/);
  assert.match(supportSource, /supportOutput\("-vmargs"\)/);
  assert.match(supportSource, /"ghidra\.app\.util\.headless\.AnalyzeHeadless"/);
  assert.match(providerSource, /new URL\("\.\.\/\.\.\/bridge\/ghidra\/ReaGhidraBridge\.java"/);
  assert.ok(fs.existsSync(pinned.bridge));
  assert.equal(releaseVmArgs.length, 18);
  assert.deepEqual(expectedHeadlessArguments(context).map(value => value.toLowerCase()), app.map(value => value.toLowerCase()));
});

test('an exact command cannot confer ownership', () => {
  for (const proof of [undefined, false, 1, 'true', {verified: true}]) {
    const result = validateJavaRole({...observe(batchMain()), strictOwnershipVerified: proof}, context);
    assert.equal(result.reason, 'StrictOwnershipRequired');
    assert.equal(result.ok, false);
    assert.equal(result.countTowardMemory, true);
  }
  assert.equal(validateJavaRole(null, context).reason, 'StrictOwnershipRequired');
});

test('only the pinned absolute java.exe is accepted, never PATH/javaw/another JDK', () => {
  for (const exe of ['java.exe', 'java', 'C:java.exe', 'C:\\other\\java.exe',
    pinned.javaExe.replace(/java\.exe$/i, 'javaw.exe'), pinned.javaExe + '.',
    pinned.javaExe + ':stream', `"${pinned.javaExe}"`, '\\\\?\\' + pinned.javaExe]) {
    assert.equal(validateJavaRole({...observe(['-version']), exe}, context).reason, 'UnpinnedJavaExecutable');
  }
  assert.equal(validateJavaRole({...observe(['-version']), exe: pinned.javaExe.toUpperCase().replaceAll('\\', '/')}, context).ok, true);
});

test('Windows quoting is the caller responsibility; raw strings and malformed tokens fail closed', () => {
  for (const args of ['-version', [], [''], ['-version\0'], ['-version\n'], ['"-version"'], [null],
    ['x'.repeat(32768)], Array(129).fill('-version')]) fail(args, 'InvalidJavaArgumentTokens');
  fail([pinned.javaExe, '-version']);
});

test('sparse token arrays and inherited tokens never fill argv slots', () => {
  fail(new Array(1), 'InvalidJavaArgumentTokens');
  const helper = [...support, '-vmargs'];
  delete helper[2];
  fail(helper, 'InvalidJavaArgumentTokens');
  const main = batchMain();
  delete main[main.indexOf('-Xmx384M')];
  fail(main, 'InvalidJavaArgumentTokens');
  const inherited = new Array(1);
  Object.setPrototypeOf(inherited, {'0': '-version'});
  fail(inherited, 'InvalidJavaArgumentTokens');
});

test('batch -version is an auxiliary role with no heap exemption', () => {
  accept(['-version'], 'version-probe', 'version', 'windows-batch');
  assert.equal(validateJavaRole(observe(['-version'])).ok, true);
  for (const args of [['-version', 'OtherClass'], ['-version', '-Xmx384M'],
    ['--version'], ['-showversion'], ['-version', '>NUL', '2>&1']]) fail(args);
});

test('the installed seven-token properties/version probe is accepted exactly', () => {
  const probe = ['-Dstdout.encoding=UTF-8', '-Dstderr.encoding=UTF-8',
    '-Dsun.stdout.encoding=UTF-8', '-Dsun.stderr.encoding=UTF-8',
    '-XX:-UsePerfData', '-XshowSettings:properties', '-version'];
  for (const token of probe) assert.ok(installationSource.includes(`"${token}"`));
  accept(probe, 'version-probe', 'properties-version', 'installation-probe');
  fail(probe.slice(1));
  fail([...probe, '-jar', 'unrelated.jar']);
});

for (const phase of ['-envvars', '-vmargs']) {
  test(`installed batch LaunchSupport ${phase} accepts support/.. but exact class and phase`, () => {
    accept([...support, phase], 'launch-support', phase.slice(1), 'windows-batch');
    fail([...support, phase, '-save']);
    fail([...support.slice(0, 2), 'OtherLaunchSupport', ...support.slice(3), phase]);
  });
  test(`installed direct LaunchSupport ${phase} accepts only the paired log template`, () => {
    accept([...isolated, ...support, phase], 'launch-support', phase.slice(1), 'direct-helper');
    accept([...isolated, '-Xlog:disable', '-Xlog:all=warning:stderr', ...support, phase],
      'launch-support', phase.slice(1), 'direct-helper');
    fail([...isolated, '-Xlog:disable', ...support, phase]);
    fail([...isolated, '-Xlog:disable', '-Xlog:all=trace:file=elsewhere', ...support, phase]);
  });
}

test('jdk-home-save is distinct; interactive/JRE/unknown helper phases are rejected', () => {
  accept([...support, '-jdk_home', '-save'], 'launch-support', 'jdk-home-save', 'windows-batch');
  for (const tail of [['-jdk_home', '-ask'], ['-java_home', '-save'], ['-jdk_home'],
    ['-envvars', '-vmargs'], ['-unknown']]) fail([...support, ...tail]);
});

test('LaunchSupport classpath and install root cannot be widened or replaced', () => {
  for (const jar of [supportJar + ';C:\\evil.jar', supportJar + '\\*', utilityJar, 'LaunchSupport.jar']) {
    fail(['-cp', jar, 'LaunchSupport', pinned.ghidraHome, '-vmargs']);
  }
  fail(['-cp', supportJar, 'LaunchSupport', path.join(pinned.ghidraHome, '..'), '-vmargs']);
  fail(['-Dnote=LaunchSupport', '-cp', supportJar, 'OtherClass', pinned.ghidraHome, '-vmargs']);
  fail(['-jar', supportJar, 'LaunchSupport', pinned.ghidraHome, '-vmargs']);
});

test('session context is direct-child pinned runtime and cannot select another JDK/Ghidra', () => {
  for (const ctx of [null, [], {}, {runtimeRoot: 'C:\\other\\rea-ghidra-1234567890abcdef'},
    {runtimeRoot: path.join(run, 'nested')}, {...context, javaHome: 'C:\\other\\jdk'},
    {...context, ghidraHome: path.join(pinned.ghidraHome, '..')}]) {
    assert.equal(check(batchMain(), ctx).reason, 'InvalidJavaRoleContext');
    assert.throws(() => expectedHeadlessArguments(ctx), /InvalidJavaRoleContext/);
  }
  const other = {...context, runtimeRoot: path.join(pinned.runtime, 'rea-ghidra-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa')};
  assert.equal(check(batchMain(), other).ok, false);
});

test('both installed main launch templates require exact AnalyzeHeadless structure', () => {
  accept(batchMain(), 'analyze-headless', 'analysis', 'windows-batch');
  accept(directMain(), 'analyze-headless', 'analysis', 'direct-main');
  fail([...batchMain(), 'trailing']);
  const quoted = batchMain(); quoted[quoted.indexOf('ghidra.Ghidra')] = 'ghidra.Ghidra.Other';
  fail(quoted);
});

test('release VM arguments permit ordering only, no duplicate/unknown/changed option', () => {
  const reordered = batchMain(); reordered.splice(0, releaseVmArgs.length, ...[...releaseVmArgs].reverse());
  assert.equal(check(reordered).ok, true);
  for (const replacement of ['-Dfile.encoding=other', '-Dnote=ghidra.Ghidra',
    '-XX:MaxHeapSize=4096M', '-javaagent:C:\\evil.jar', '-agentlib:jdwp=transport=dt_socket',
    '--patch-module=java.base=C:\\evil', '-Xbootclasspath/a:C:\\evil']) {
    const args = batchMain(); args[1] = replacement; fail(args);
  }
  const duplicate = batchMain(); duplicate[1] = duplicate[0]; fail(duplicate);
  const missing = batchMain(); missing.splice(1, 1); fail(missing);
});

test('heap must occur exactly once with the literal -Xmx384M', () => {
  for (const value of ['-Xmx512M', '-Xmx384m', '-Xmx402653184', '-Xmx0', '-Xmx']) {
    const args = batchMain(); args[args.indexOf('-Xmx384M')] = value;
    fail(args, 'MainHeapMustBeExactly384M');
  }
  const absent = batchMain().filter(token => token !== '-Xmx384M');
  fail(absent, 'MainHeapMustBeExactly384M');
  for (const extra of ['-Xmx384M', '-Xmx2G']) {
    const args = batchMain(); args.splice(args.indexOf('-cp'), 0, extra);
    fail(args, 'MainHeapMustBeExactly384M');
  }
});

test('same-path unrelated Java and class strings hidden in properties cannot match', () => {
  fail(['-Xmx384M', '-Dnote=ghidra.Ghidra ghidra.app.util.headless.AnalyzeHeadless', '-jar', 'unrelated.jar']);
  fail(['-Xmx384M', '-cp', utilityJar, 'unrelated.Main', 'ghidra.Ghidra', 'ghidra.app.util.headless.AnalyzeHeadless']);
  fail(['-cp', utilityJar, 'ghidra.Ghidra', 'unrelated.Main', 'ghidra.app.util.headless.AnalyzeHeadless']);
});

test('unknown JVM arguments, argfiles and duplicate classpaths are refused', () => {
  for (const extra of ['@C:\\injected.args', '-javaagent:C:\\evil.jar', '--module=other/Main',
    '-Duser.home=C:\\other', '-Djava.io.tmpdir=C:\\other', '-Xlog:all=trace:file=C:\\other']) {
    const args = batchMain(); args.splice(args.indexOf('-cp'), 0, extra); fail(args);
  }
  fail(['-cp', utilityJar, ...batchMain()]);
});

test('headless arguments fix target copy, bridge, project, logs and session', () => {
  const base = batchMain();
  for (const original of [app[0], app[3], app[7], app[9], app[11], app[13], app[14]]) {
    const changed = [...base]; changed[changed.indexOf(original)] = 'C:\\other\\file';
    fail(changed);
  }
  for (const extra of [['-postScript', 'evil.java'], ['-preScript', 'evil.java'],
    ['-scriptPath', 'C:\\other'], ['-process', 'other'], ['-overwrite']]) fail([...base, ...extra]);
  const noReadOnly = base.filter(token => token !== '-readOnly'); fail(noReadOnly);
  // An untrusted "expected" tail in context cannot widen this profile.
  assert.equal(check([...base, '-postScript', 'evil.java'],
    {...context, expectedHeadlessArgs: [...app, '-postScript', 'evil.java']}).ok, false);
});

test('caller-provided inputs are not mutated and every verdict counts memory', () => {
  const args = Object.freeze(batchMain());
  const input = Object.freeze(observe(args));
  const snapshot = JSON.stringify({input, context});
  const accepted = validateJavaRole(input, context);
  assert.equal(JSON.stringify({input, context}), snapshot);
  assert.equal(accepted.countTowardMemory, true);
  assert.equal(check(['OtherClass']).countTowardMemory, true);
  assert.equal(check([...support, '-vmargs']).countTowardMemory, true);
});
