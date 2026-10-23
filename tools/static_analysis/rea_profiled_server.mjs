import {pathToFileURL} from 'node:url';
import {root, launcher, projectEnvironment} from './rea_project_environment.mjs';

// Nothing from Rea is imported and no active child is spawned before the owner
// proves this gated process belongs to its dedicated kernel Job.
const nonce = process.argv[2]?.match(/^--owned-guard=([a-f0-9-]{36})$/)?.[1];
if (process.argv.length !== 3 || !nonce || !process.connected) throw Error('A task-owned IPC start gate is required');
await new Promise((resolve, reject) => {
  const timeout = setTimeout(() => reject(Error('Owned Job start gate timed out')), 30000);
  const onDisconnect = () => { clearTimeout(timeout); reject(Error('Owner disconnected before start authorization')); };
  process.once('disconnect', onDisconnect);
  process.on('message', message => {
    if (message?.type !== 'release-owned-start' || message?.nonce !== nonce) {
      clearTimeout(timeout); reject(Error('Invalid owned start authorization')); return;
    }
    clearTimeout(timeout); process.removeListener('disconnect', onDisconnect); resolve();
  });
  process.send({type: 'owned-start-waiting', nonce, pid: process.pid});
});
process.disconnect();
// One task-specific child environment; the normal project config stays at 512M.
process.env = projectEnvironment();
process.env.GHIDRA_HEADLESS_MAXMEM = '384M';
process.chdir(root);
process.argv = [process.execPath, launcher, 'mcp'];
await import(pathToFileURL(launcher).href);
