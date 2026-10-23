import {pathToFileURL} from 'node:url';
import {root, launcher, projectEnvironment} from './rea_project_environment.mjs';

// Keep user credentials, implicit targets and backend overrides out of Rea.
// This environment filtering is not an OS filesystem sandbox.
process.env = projectEnvironment();
process.chdir(root);
process.argv = [process.execPath, launcher, 'mcp'];
await import(pathToFileURL(launcher).href);
