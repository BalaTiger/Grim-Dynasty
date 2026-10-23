// Self-owned Node-only process fixture. No vendor imports or target-file access.
import {spawn} from 'node:child_process';
import {createInterface} from 'node:readline';
import {fileURLToPath} from 'node:url';

const [mode, nonce] = process.argv.slice(2);
if (process.argv.length !== 4 || !['root', 'short-parent', 'leaf', 'control'].includes(mode)
    || !/^[0-9a-f]{32}$/.test(nonce ?? '')) throw Error('Invalid fixed fixture arguments');
const script = fileURLToPath(import.meta.url);
const send = value => process.stdout.write(JSON.stringify({nonce, ...value}) + '\n');
const lifetime = setTimeout(() => process.exit(90), 120000);
if (mode === 'leaf' || mode === 'control') {
  if (mode === 'control') send({event: 'ready', pid: process.pid, mode});
} else if (mode === 'short-parent') {
  const child = spawn(process.execPath, [script, 'leaf', nonce], {
    detached: true, windowsHide: true, stdio: 'ignore',
  });
  child.on('error', error => { send({event: 'error', message: error.message}); process.exit(1); });
  child.once('spawn', () => {
    child.unref();
    process.stdout.write(JSON.stringify({nonce, event: 'leaf', pid: child.pid, parentPid: process.pid}) + '\n',
      () => process.exit(0));
  });
} else {
  send({event: 'ready', pid: process.pid, mode});
  let released = false;
  const input = createInterface({input: process.stdin});
  input.on('line', line => {
    if (line === 'exit') process.exit(0);
    if (line !== 'release' || released) {
      send({event: 'error', message: 'Unexpected or duplicate gate command'});
      process.exit(2);
    }
    released = true;
    const parent = spawn(process.execPath, [script, 'short-parent', nonce], {
      windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'],
    });
    parent.on('error', error => { send({event: 'error', message: error.message}); process.exit(1); });
    parent.stdout.pipe(process.stdout, {end: false});
    parent.stderr.pipe(process.stderr, {end: false});
    parent.once('exit', (code, signal) => send({event: 'parent-exit', pid: parent.pid, code, signal}));
  });
  input.once('close', () => process.exit(0));
}
process.once('exit', () => clearTimeout(lifetime));
