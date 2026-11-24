// Runs all backend tests with coverage and merges the results into one report under coverage/.
// Usage: npm run coverage:backend

import { spawnSync } from 'node:child_process';
import { mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { basename, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(dirname(fileURLToPath(import.meta.url)));
const output = join(root, 'coverage');
const results = join(output, 'results');
const solution = 'MapsetVerifier.slnx';

// Same as in coverage.runsettings.
const exclude = { modules: '[*.Tests]*', files: ['**/obj/**', '**/*.g.cs'] };

function dotnet(args, { quiet = false, mayFail = false } = {}) {
  const run = spawnSync('dotnet', args, {
    cwd: root,
    encoding: 'utf8',
    stdio: quiet ? 'pipe' : 'inherit',
  });

  if (run.error) throw run.error;
  if (run.status !== 0 && !mayFail) {
    if (quiet) console.error(run.stdout + run.stderr);
    throw new Error(`dotnet ${args[0]} failed`);
  }

  return run;
}

rmSync(output, { recursive: true, force: true });
mkdirSync(results, { recursive: true });

dotnet(['tool', 'restore'], { quiet: true });
dotnet(['build', solution, '-c', 'Debug', '-v', 'quiet', '--nologo']);

const tests = dotnet(
  [
    'test',
    solution,
    '-c',
    'Debug',
    '--no-build',
    '--nologo',
    '--collect',
    'XPlat Code Coverage',
    '--settings',
    'scripts/coverage.runsettings',
    '--results-directory',
    results,
  ],
  { mayFail: true }
);

// Only what a test project references is measured, so without this a project without tests would
// be missing rather than 0%. Instruments each project without running any of it.
const projects = [...readFileSync(join(root, solution), 'utf8').matchAll(/Project Path="([^"]+)"/g)]
  .map((match) => match[1])
  .filter((path) => !path.endsWith('.Tests.csproj'));

for (const project of projects) {
  const name = basename(project, '.csproj');
  const target = dotnet(['msbuild', project, '-getProperty:TargetPath', '-p:Configuration=Debug'], {
    quiet: true,
  }).stdout.trim();

  dotnet(
    [
      'coverlet',
      target,
      '--target',
      'dotnet',
      '--targetargs',
      'help',
      '--include',
      `[${name}]*`,
      '--exclude',
      exclude.modules,
      ...exclude.files.flatMap((pattern) => ['--exclude-by-file', pattern]),
      '--skipautoprops',
      // The project's own assembly rather than only the ones next to it.
      '--include-test-assembly',
      '--format',
      'opencover',
      '--output',
      join(results, `${name}.baseline.opencover.xml`),
    ],
    { quiet: true }
  );
}

dotnet(
  [
    'reportgenerator',
    `-reports:${join(results, '**', '*.opencover.xml')}`,
    `-targetdir:${join(output, 'report')}`,
    '-reporttypes:Html;JsonSummary',
    '-title:Mapset Verifier backend',
    '-verbosity:Warning',
  ],
  { quiet: true }
);

const { summary, coverage } = JSON.parse(
  readFileSync(join(output, 'report', 'Summary.json'), 'utf8')
);
const percentage = (covered, total) =>
  total > 0 ? `${((100 * covered) / total).toFixed(1)}%` : '-';
const row = (name, data) =>
  [
    name.padEnd(34),
    percentage(data.coveredlines, data.coverablelines).padStart(7),
    `${data.coveredlines}/${data.coverablelines}`.padStart(13),
    percentage(data.coveredbranches, data.totalbranches).padStart(9),
    `${data.coveredbranches}/${data.totalbranches}`.padStart(11),
  ].join(' ');

const missing = projects
  .map((project) => basename(project, '.csproj'))
  .filter((name) => !coverage.assemblies.some((assembly) => assembly.name === name));
if (missing.length > 0) throw new Error(`Not in the report: ${missing.join(', ')}`);

const text = [
  `${'Project'.padEnd(34)} ${'Lines'.padStart(7)} ${''.padStart(13)} ${'Branches'.padStart(9)}`,
  ...coverage.assemblies.map((assembly) => row(assembly.name, assembly)),
  row('Total', summary),
].join('\n');
writeFileSync(join(output, 'summary.txt'), text + '\n');

console.log(`\n${text}\n\nReport: ${join(output, 'report', 'index.html')}`);

if (tests.status !== 0) {
  console.error('Some tests failed, so the coverage is incomplete.');
  process.exit(1);
}
