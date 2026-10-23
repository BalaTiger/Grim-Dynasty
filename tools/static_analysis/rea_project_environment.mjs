import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

export const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
export const launcher = path.join(root, '.tooling/rea/node_modules/rea-agents/scripts/rea.mjs');
export const runtime = path.join(root, 'analysis-runtime/rea');
export const javaHome = path.join(root, '.tooling/jdk/jdk-21.0.12.1+1');
export const ghidraHome = path.join(root, '.tooling/ghidra/ghidra_12.1.4_PUBLIC');

// Child-process environment isolation, not an OS filesystem sandbox.
export function projectEnvironment() {
  const metadata = JSON.parse(fs.readFileSync(path.join(path.dirname(launcher), '../package.json'), 'utf8'));
  if (metadata.name !== 'rea-agents' || metadata.version !== '6.5.0') throw Error('Expected pinned rea-agents 6.5.0');
  for (const filename of [path.join(javaHome, 'bin/java.exe'), path.join(ghidraHome, 'support/analyzeHeadless.bat')]) {
    if (!fs.existsSync(filename)) throw Error(`Pinned backend missing: ${filename}`);
  }
  const env = {};
  for (const key of ['SystemRoot', 'WINDIR', 'ComSpec', 'PROCESSOR_ARCHITECTURE', 'PATH']) {
    if (process.env[key]) env[key] = process.env[key];
  }
  const home = path.join(runtime, 'host/home');
  const config = path.join(runtime, 'host/config');
  const cache = path.join(runtime, 'host/cache');
  const data = path.join(runtime, 'host/data');
  for (const directory of [runtime, home, config, cache, data]) fs.mkdirSync(directory, {recursive: true});
  Object.assign(env, {
    TEMP: runtime, TMP: runtime, TMPDIR: runtime,
    USERPROFILE: home, HOME: home, APPDATA: config, LOCALAPPDATA: cache,
    XDG_CONFIG_HOME: config, XDG_CACHE_HOME: cache, XDG_DATA_HOME: data,
    GHIDRA_INSTALL_DIR: ghidraHome, JAVA_HOME: javaHome,
    GHIDRA_HEADLESS_MAXMEM: '512M', REA_LOG_LEVEL: 'warn', REA_ANALYSIS_PROVIDER: 'ghidra',
  });
  return env;
}
