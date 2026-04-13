import { promises as fs } from 'node:fs';
import path from 'node:path';

const artifactsDir = process.env.ARTIFACTS_DIR ?? 'release-artifacts';
const outputPath = process.env.OUTPUT_PATH ?? 'latest.json';
const releaseBodyFile = process.env.RELEASE_BODY_FILE;
const repository = process.env.GITHUB_REPOSITORY;
const releaseTag = process.env.RELEASE_TAG;

if (!repository) throw new Error('GITHUB_REPOSITORY is required.');
if (!releaseTag) throw new Error('RELEASE_TAG is required.');

const version = releaseTag.startsWith('v') ? releaseTag.slice(1) : releaseTag;

async function collectFiles(directory) {
  const entries = await fs.readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const resolved = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...await collectFiles(resolved));
    } else {
      files.push(resolved);
    }
  }

  return files;
}

function releaseAssetUrl(fileName) {
  return `https://github.com/${repository}/releases/download/${encodeURIComponent(releaseTag)}/${encodeURIComponent(fileName)}`;
}

function selectAsset(files, patterns) {
  const sortedFiles = [...files].sort((left, right) => path.basename(left).localeCompare(path.basename(right)));

  for (const pattern of patterns) {
    const match = sortedFiles.find((file) => pattern.test(path.basename(file)) && !file.endsWith('.sig'));
    if (match) return match;
  }

  return null;
}

async function resolvePlatformEntry(allFiles, platformKey, patterns) {
  const platformDirectory = path.join(artifactsDir, `updater-artifacts-${platformKey}`);
  const platformFiles = allFiles.filter((file) => file.startsWith(platformDirectory));
  const assetPath = selectAsset(platformFiles, patterns);

  if (!assetPath) {
    throw new Error(`Could not find an updater bundle for ${platformKey} inside ${platformDirectory}.`);
  }

  const signaturePath = `${assetPath}.sig`;
  const signature = (await fs.readFile(signaturePath, 'utf8')).trim();

  return {
    url: releaseAssetUrl(path.basename(assetPath)),
    signature,
  };
}

const releaseNotes = releaseBodyFile ? (await fs.readFile(releaseBodyFile, 'utf8')).trim() : '';
const collectedFiles = await collectFiles(artifactsDir);

if (collectedFiles.length === 0) {
  throw new Error(`No artifacts were found under ${artifactsDir}.`);
}

const latestJson = {
  version,
  notes: releaseNotes,
  pub_date: new Date().toISOString(),
  platforms: {
    'windows-x86_64': await resolvePlatformEntry(collectedFiles, 'windows-x86_64', [/setup\.exe$/i, /\.msi$/i]),
    'linux-x86_64': await resolvePlatformEntry(collectedFiles, 'linux-x86_64', [/\.AppImage$/i]),
    'darwin-x86_64': await resolvePlatformEntry(collectedFiles, 'darwin-x86_64', [/\.app\.tar\.gz$/i]),
    'darwin-aarch64': await resolvePlatformEntry(collectedFiles, 'darwin-aarch64', [/\.app\.tar\.gz$/i]),
  },
};

await fs.writeFile(outputPath, `${JSON.stringify(latestJson, null, 2)}\n`, 'utf8');
console.log(`Generated ${outputPath}`);