// Runs all backend tests on Linux, on a copy of the source. Optional: needs Docker, or WSL with the .NET 10 SDK and ICU
// (libicu on Ubuntu), and does nothing without either. The tests themselves need neither, and CI always runs them on
// Linux.
// Usage: npm run test:linux

import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(dirname(fileURLToPath(import.meta.url)));
const image = 'mcr.microsoft.com/dotnet/sdk:10.0';

const unpackAndTest = [
  'set -e',
  'export PATH="$PATH:$HOME/.dotnet"',
  'dir=$(mktemp -d)',
  'trap \'rm -rf "$dir"\' EXIT',
  'tar -xf - -C "$dir"',
  'cd "$dir"',
  'dotnet test MapsetVerifier.slnx -c Release --nologo',
].join('\n');

function succeeds(command, args) {
  return spawnSync(command, args, { stdio: 'ignore', timeout: 60_000 }).status === 0;
}

function findLinux() {
  if (process.platform === 'linux') return ['Linux', 'sh', ['-c', unpackAndTest]];

  if (succeeds('docker', ['info']))
    return ['Docker', 'docker', ['run', '--rm', '-i', image, 'sh', '-c', unpackAndTest]];

  const hasSdk = 'PATH="$PATH:$HOME/.dotnet" dotnet --version | grep -q "^10\\."';

  if (process.platform === 'win32' && succeeds('wsl.exe', ['-e', 'sh', '-c', hasSdk]))
    return ['WSL', 'wsl.exe', ['-e', 'sh', '-c', unpackAndTest]];

  return null;
}

const linux = findLinux();

if (linux === null) {
  console.log('Skipped: neither Docker nor WSL with the .NET 10 SDK is available.');
  process.exit(0);
}

const [name, command, args] = linux;
console.log(`Running the backend tests on Linux, using ${name}.`);

function run(program, programArgs, input) {
  const result = spawnSync(program, programArgs, { cwd: root, input, maxBuffer: 2 ** 30 });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${program} failed: ${result.stderr}`);
  return result.stdout;
}

// Uncommitted files too, but not what is ignored. The app's own sources aren't needed.
const files = run('git', ['ls-files', '-coz', '--exclude-standard'])
  .toString()
  .split('\0')
  .filter((file) => file && !file.startsWith('electron-app/') && existsSync(join(root, file)));
const archive = run('tar', ['-cf', '-', '-T', '-'], files.join('\n'));

const tests = spawnSync(command, args, { input: archive, stdio: ['pipe', 'inherit', 'inherit'] });

if (tests.error) throw tests.error;
process.exit(tests.status ?? 1);
