// Generates THIRD-PARTY-NOTICES.txt from what the app actually ships.
//
//   dotnet publish src/MapsetVerifier.csproj -c Release -r win-x64 -p:PublishSingleFile=false -o <dir>
//   node scripts/generate-third-party-notices.mjs <dir>
//
// The sidecar is normally a single file, so publish it unbundled once to see which files ship. Every NuGet
// package with a file in that folder is listed, along with the .NET runtime packs it bundles. For the
// Electron app, the production npm dependencies of electron-app (bundled into the UI) and of the root
// package (packed into app.asar) are listed. Each entry carries the package's own license and notice files
// where it has them. Texts no package provides live in scripts/third-party-notices:
//   *.txt            components with their own notice, such as the IPADIC dictionary bundled in Kuromoji
//   packages/*.txt   license files for packages that don't include one, taken from their source repository;
//                    a file also covers packages whose name extends it (ppy.Veldrid covers ppy.Veldrid.*)
//   licenses/*.txt   full license texts that packages only name, such as LGPL-2.1
// Electron and Chromium ship their own notices next to the app.
//
// Rerun this whenever a dependency is added, removed or updated.

import { execSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const publishDir = process.argv[2];
if (!publishDir || !fs.existsSync(publishDir)) {
  console.error(
    'Usage: node scripts/generate-third-party-notices.mjs <unbundled sidecar publish folder>'
  );
  process.exit(1);
}

const noticesDir = path.join(root, 'scripts/third-party-notices');
const rule = '='.repeat(79);
const thinRule = '-'.repeat(79);
const licenseFilePattern = /^(licen[cs]e|copying|notice|third[-_ ]?party[-_ ]?notices)([-_.].*)?$/i;
// Formatted documents (an RTF license in Microsoft.Diagnostics.Runtime) can't be included as text.
const documentPattern = /\.(rtf|pdf|docx?|html?)$/i;

// Byte order marks, page breaks (in the GNU license texts) and stray NUL characters are dropped.
const readText = (file) =>
  fs
    .readFileSync(file, 'utf8')
    .replace(/^\uFEFF/, '')
    .replace(/\r\n?/g, '\n')
    .replace(/[\f\0]/g, '')
    .trim();

const licenseFilesIn = (dir) =>
  fs.existsSync(dir)
    ? fs
        .readdirSync(dir, { withFileTypes: true })
        .filter(
          (entry) =>
            entry.isFile() &&
            licenseFilePattern.test(entry.name) &&
            !documentPattern.test(entry.name)
        )
        .map((entry) => entry.name)
        .sort((a, b) => a.localeCompare(b))
    : [];

// ---------------------------------------------------------------------------------------------------------
// .NET sidecar
// ---------------------------------------------------------------------------------------------------------

function nugetPackages() {
  const assets = JSON.parse(
    fs.readFileSync(path.join(root, 'src/obj/project.assets.json'), 'utf8')
  );
  const packageFolder = Object.keys(assets.packageFolders)[0];
  const ridTarget = Object.keys(assets.targets).find((key) => key.includes('/'));
  const shipped = new Set(fs.readdirSync(publishDir).map((file) => file.toLowerCase()));
  const claimed = new Set();
  const packages = [];

  for (const [key, entry] of Object.entries(assets.targets[ridTarget])) {
    if (entry.type !== 'package') continue;
    const files = [entry.runtime, entry.native, entry.runtimeTargets]
      .flatMap((group) => Object.keys(group ?? {}))
      .map((file) => path.basename(file).toLowerCase());
    if (!files.some((file) => shipped.has(file))) continue;

    files.forEach((file) => claimed.add(file));
    const [name, version] = key.split('/');
    packages.push(nugetEntry(name, version, path.join(packageFolder, assets.libraries[key].path)));
  }

  // Self-contained publishing bundles the runtime itself, which is not a package reference. Runtime packs
  // share file names (WindowsDesktop lists WindowsBase.dll too), so a pack is only listed when it provides a
  // shipped file that no package or earlier pack does. The base runtime comes first.
  const packs = Object.values(assets.project.frameworks)
    .flatMap((framework) => framework.downloadDependencies ?? [])
    .sort((a, b) => packOrder(a.name) - packOrder(b.name));
  for (const pack of packs) {
    {
      const version = pack.version.replace(/[[\]\s]/g, '').split(',')[0];
      const dir = path.join(packageFolder, pack.name.toLowerCase(), version);
      const runtimeList = path.join(dir, 'data', 'RuntimeList.xml');
      if (!fs.existsSync(runtimeList)) continue;
      const packFiles = [...fs.readFileSync(runtimeList, 'utf8').matchAll(/Path="([^"]+)"/g)].map(
        (match) => path.basename(match[1]).toLowerCase()
      );
      if (!packFiles.some((file) => shipped.has(file) && !claimed.has(file))) continue;
      packFiles.forEach((file) => claimed.add(file));
      packages.push(nugetEntry(pack.name, version, dir));
    }
  }

  return packages.sort((a, b) => a.name.localeCompare(b.name, 'en', { sensitivity: 'base' }));
}

const packOrder = (name) =>
  name.startsWith('Microsoft.NETCore.App.')
    ? 0
    : name.startsWith('Microsoft.AspNetCore.App.')
      ? 1
      : 2;

function nugetEntry(name, version, dir) {
  const nuspecFile = fs.readdirSync(dir).find((file) => file.toLowerCase().endsWith('.nuspec'));
  const nuspec = nuspecFile ? fs.readFileSync(path.join(dir, nuspecFile), 'utf8') : '';
  const tag = (tagName) =>
    nuspec.match(new RegExp(`<${tagName}(?:\\s[^>]*)?>([^<]*)</${tagName}>`))?.[1]?.trim();
  const licenseType = nuspec.match(/<license\s+type="([^"]+)"/)?.[1];
  const license = licenseType === 'expression' ? tag('license') : undefined;
  const licenseUrl = tag('licenseUrl');

  return {
    name,
    version,
    license:
      license ??
      (licenseType === 'file' ? 'see license text' : licenseUrl ? `see ${licenseUrl}` : 'unknown'),
    copyright: decodeXml(tag('copyright') ?? tag('authors') ?? ''),
    url: tag('projectUrl') ?? tag('repository') ?? '',
    texts: licenseFilesIn(dir).map((file) => ({ file, text: readText(path.join(dir, file)) })),
  };
}

const decodeXml = (text) =>
  text
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&#169;/g, '©');

// ---------------------------------------------------------------------------------------------------------
// Electron app
// ---------------------------------------------------------------------------------------------------------

function npmPackages(projectDir) {
  const output = execSync('npm ls --omit=dev --all --json --long', {
    cwd: projectDir,
    encoding: 'utf8',
    maxBuffer: 256 * 1024 * 1024,
  });
  const tree = JSON.parse(output);
  const found = new Map();

  const visit = (dependencies) => {
    for (const [name, node] of Object.entries(dependencies ?? {})) {
      if (node.missing || !node.version || !node.path) continue;
      const key = `${name}@${node.version}`;
      if (!found.has(key)) {
        found.set(key, npmEntry(name, node.version, node.path));
        visit(node.dependencies);
      }
    }
  };
  visit(tree.dependencies);
  return [...found.values()];
}

function npmEntry(name, version, dir) {
  const manifest = JSON.parse(fs.readFileSync(path.join(dir, 'package.json'), 'utf8'));
  const license =
    typeof manifest.license === 'string'
      ? manifest.license
      : (manifest.license?.type ??
        manifest.licenses?.map((item) => item.type ?? item).join(' OR ') ??
        'unknown');
  const author = typeof manifest.author === 'string' ? manifest.author : manifest.author?.name;
  const repository =
    typeof manifest.repository === 'string' ? manifest.repository : manifest.repository?.url;

  return {
    name,
    version,
    license,
    copyright: author ?? '',
    url: (manifest.homepage ?? repository ?? '').replace(/^git\+/, ''),
    texts: licenseFilesIn(dir).map((file) => ({ file, text: readText(path.join(dir, file)) })),
  };
}

// ---------------------------------------------------------------------------------------------------------
// Output
// ---------------------------------------------------------------------------------------------------------

// License files kept in the repository for packages that don't include one.
function addPackageLicenseFiles(entries) {
  const dir = path.join(noticesDir, 'packages');
  const files = fs.readdirSync(dir).map((file) => path.basename(file, '.txt'));
  for (const entry of entries.filter((item) => item.texts.length === 0)) {
    const match = files
      .filter((file) => entry.name === file || entry.name.startsWith(`${file}.`))
      .sort((a, b) => b.length - a.length)[0];
    if (!match) continue;
    entry.texts = [{ file: `${match}.txt`, text: readText(path.join(dir, `${match}.txt`)) }];
    if (entry.license === 'unknown' || entry.license.startsWith('see '))
      entry.license = 'see license text';
  }
}

// Standard texts for packages that only name their license. Apache 2.0 comes from J2N's license file,
// which is the unmodified license text; others are kept in scripts/third-party-notices/licenses.
function standardTexts(nuget) {
  const j2n = nuget.find((entry) => entry.name === 'J2N')?.texts[0]?.text ?? '';
  const apache = j2n.slice(
    0,
    j2n.indexOf('END OF TERMS AND CONDITIONS') + 'END OF TERMS AND CONDITIONS'.length
  );

  const kept = Object.fromEntries(
    fs
      .readdirSync(path.join(noticesDir, 'licenses'))
      .map((file) => [
        path.basename(file, '.txt'),
        readText(path.join(noticesDir, 'licenses', file)),
      ])
  );

  return {
    ...kept,
    MIT: `Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.`,
    ISC: `Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.`,
    'BSD-2-Clause': `Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.`,
    'BSD-3-Clause': `Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.`,
    'Apache-2.0': apache,
  };
}

function render(entry, standard, used) {
  const lines = [thinRule, `${entry.name} ${entry.version}`, `License: ${entry.license}`];
  if (entry.url) lines.push(`Source: ${entry.url}`);
  lines.push(thinRule, '');

  if (entry.texts.length > 0) {
    for (const { file, text } of entry.texts) {
      if (entry.texts.length > 1) lines.push(`${file}:`, '');
      lines.push(text, '');
    }
  } else {
    if (entry.copyright)
      lines.push(
        entry.copyright.startsWith('Copyright')
          ? entry.copyright
          : `Copyright (c) ${entry.copyright}`,
        ''
      );
    const id = Object.keys(standard).find((key) =>
      entry.license.toUpperCase().includes(key.toUpperCase())
    );
    if (id) {
      used.add(id);
      lines.push(
        `Licensed under the ${id} license, see "${id}" under Standard license texts at the end of this file.`,
        ''
      );
    } else {
      lines.push(`No license file is included in this package. License: ${entry.license}.`, '');
    }
  }
  return lines.join('\n');
}

const nuget = nugetPackages();
const npm = [...npmPackages(path.join(root, 'electron-app')), ...npmPackages(root)]
  .filter(
    (entry, index, all) =>
      all.findIndex((other) => other.name === entry.name && other.version === entry.version) ===
      index
  )
  .sort((a, b) => a.name.localeCompare(b.name, 'en', { sensitivity: 'base' }));
const extras = fs
  .readdirSync(noticesDir, { withFileTypes: true })
  .filter((entry) => entry.isFile() && entry.name.endsWith('.txt'))
  .map((entry) => entry.name)
  .sort()
  .map((file) => readText(path.join(noticesDir, file)));
addPackageLicenseFiles([...nuget, ...npm]);

const standard = standardTexts(nuget);
const used = new Set();
const nugetText = nuget.map((entry) => render(entry, standard, used)).join('\n');
const npmText = npm.map((entry) => render(entry, standard, used)).join('\n');

const out = [
  'Mapset Verifier third-party notices',
  rule,
  '',
  'Mapset Verifier is licensed under the GNU General Public License v3.0, see',
  'LICENSE, with an additional permission for the BASS libraries, see',
  'LICENSE-EXCEPTION.txt. It includes the third-party components below, which',
  'come with their own licenses and notices. Electron and Chromium ship their',
  'notices separately, in LICENSE.electron.txt and LICENSES.chromium.html next',
  'to the application.',
  '',
  'Where a component is licensed under the GNU Lesser General Public License, that',
  'license refers to the GNU General Public License v3.0, included in LICENSE.',
  '',
  'This file is generated by scripts/generate-third-party-notices.mjs.',
  '',
  'Contents',
  `  1. Components with separate notices (${extras.length})`,
  `  2. .NET packages and runtime, bundled into the background service (${nuget.length})`,
  `  3. npm packages, bundled into the user interface (${npm.length})`,
  '  4. Standard license texts',
  '',
  '',
  rule,
  '1. Components with separate notices',
  rule,
  '',
  extras.join(`\n\n${thinRule}\n\n`),
  '',
  '',
  rule,
  '2. .NET packages and runtime',
  rule,
  '',
  nugetText,
  '',
  rule,
  '3. npm packages',
  rule,
  '',
  npmText,
  '',
  rule,
  '4. Standard license texts',
  rule,
  '',
  ...[...used].sort().flatMap((id) => [thinRule, id, thinRule, '', standard[id], '']),
].join('\n');

fs.writeFileSync(
  path.join(root, 'THIRD-PARTY-NOTICES.txt'),
  out.replace(/\n{4,}/g, '\n\n\n') + '\n'
);

const missing = [...nuget, ...npm].filter((entry) => entry.texts.length === 0);
console.log(
  `${nuget.length} .NET packages, ${npm.length} npm packages, ${extras.length} separate notices`
);
console.log(`${missing.length} without their own license file:`);
for (const entry of missing) console.log(`  ${entry.name} ${entry.version} (${entry.license})`);
