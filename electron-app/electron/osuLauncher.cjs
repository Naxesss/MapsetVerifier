const { execFile, spawn } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { promisify } = require('util');

const execFileAsync = promisify(execFile);
const OSU_URL = /^osu:/i;

function exists(filePath) {
  try {
    return Boolean(filePath) && fs.existsSync(filePath);
  } catch {
    return false;
  }
}

function firstExisting(candidates) {
  for (const candidate of candidates) {
    if (exists(candidate)) return candidate;
  }
  return null;
}

function resolveOnPath(name) {
  const extensions = process.platform === 'win32' ? ['.exe', '.cmd', '.bat', ''] : [''];
  const dirs = [
    ...(process.env.PATH || '').split(path.delimiter),
    path.join(os.homedir(), '.local', 'bin'),
    '/usr/bin',
    '/usr/local/bin',
  ];
  for (const dir of dirs) {
    if (!dir) continue;
    for (const extension of extensions) {
      const candidate = path.join(dir, name + extension);
      if (exists(candidate)) return candidate;
    }
  }
  return null;
}

function exeFromRegValue(value) {
  if (!value) return null;
  const quoted = value.match(/"([^"]+\.exe)"/i);
  if (quoted) return quoted[1];
  const unquoted = value.match(/([A-Za-z]:\\[^\s,"]+\.exe)/i);
  return unquoted ? unquoted[1] : null;
}

async function queryWindowsExe(regKey) {
  try {
    const { stdout } = await execFileAsync('reg', ['query', regKey, '/ve'], {
      windowsHide: true,
      timeout: 3000,
    });
    const line = stdout.split(/\r?\n/).find((entry) => entry.includes('REG_SZ'));
    if (!line) return null;
    return exeFromRegValue(line.replace(/.*REG_SZ\s+/, '').trim());
  } catch {
    return null;
  }
}

function songsSiblingExe(songsFolder) {
  if (!songsFolder) return null;
  return path.join(path.dirname(songsFolder), 'osu!.exe');
}

async function detectStableExecutable(songsFolder) {
  const sibling = songsSiblingExe(songsFolder);
  if (exists(sibling)) return sibling;

  if (process.platform === 'win32') {
    const local = path.join(process.env.LOCALAPPDATA || '', 'osu!', 'osu!.exe');
    const fromRegistry =
      (await queryWindowsExe('HKCR\\osu!\\shell\\open\\command')) ||
      (await queryWindowsExe('HKCR\\osu!\\DefaultIcon')) ||
      (await queryWindowsExe('HKCU\\Software\\Classes\\osu!\\shell\\open\\command'));
    return firstExisting([fromRegistry, local]);
  }

  if (process.platform === 'linux') {
    return resolveOnPath('osu-wine');
  }

  return null;
}

async function detectLazerExecutable() {
  if (process.platform === 'win32') {
    const localAppData = process.env.LOCALAPPDATA || '';
    return firstExisting([
      path.join(localAppData, 'osulazer', 'osu!.exe'),
      path.join(localAppData, 'osulazer', 'current', 'osu!.exe'),
      path.join(localAppData, 'osu!lazer', 'osu!.exe'),
    ]);
  }

  if (process.platform === 'darwin') {
    return firstExisting([
      '/Applications/osu!.app',
      path.join(os.homedir(), 'Applications', 'osu!.app'),
    ]);
  }

  return (
    resolveOnPath('osu-lazer') ||
    firstExisting([
      path.join(os.homedir(), 'Applications', 'osu.AppImage'),
      path.join(os.homedir(), 'Applications', 'osu-lazer.AppImage'),
      path.join(os.homedir(), 'Downloads', 'osu.AppImage'),
      path.join(os.homedir(), 'Downloads', 'osu-lazer.AppImage'),
    ])
  );
}

async function detectOsuExecutable({ client, songsFolder } = {}) {
  if (client === 'lazer') return detectLazerExecutable();
  if (client === 'stable') return detectStableExecutable(songsFolder);
  return null;
}

function classifyOsuProcess(name, exePath) {
  const hay = `${name || ''} ${exePath || ''}`.toLowerCase().replace(/\\/g, '/');
  if (!hay.includes('osu')) return null;
  if (hay.includes('mapsetverifier') || hay.includes('mapset verifier')) return null;

  if (
    hay.includes('osulazer') ||
    hay.includes('osu!lazer') ||
    hay.includes('osu!.lazer') ||
    hay.includes('/lazer/') ||
    hay.includes('osu.appimage') ||
    hay.includes('osu-lazer')
  ) {
    return 'lazer';
  }

  if (hay.includes('osu-wine')) return 'stable';
  if (hay.includes('osu!.app')) return 'lazer';
  if (hay.includes('osu!.exe') && !hay.includes('lazer')) return 'stable';
  return null;
}

async function listWindowsOsuProcesses() {
  try {
    const { stdout } = await execFileAsync(
      'wmic',
      ['process', 'where', "name like '%osu%'", 'get', 'Name,ExecutablePath', '/format:csv'],
      { windowsHide: true, timeout: 4000 }
    );
    const entries = [];
    for (const line of stdout.split(/\r?\n/)) {
      const columns = line.split(',').map((part) => part.trim());
      if (columns.length < 3 || columns[1] === 'Name') continue;
      entries.push({ name: columns[1], path: columns[2] || '' });
    }
    return entries;
  } catch {
    return [];
  }
}

async function listUnixOsuProcesses() {
  try {
    const { stdout } = await execFileAsync('ps', ['-ax', '-o', 'args='], { timeout: 3000 });
    return stdout
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter((line) => /osu/i.test(line))
      .map((line) => ({ name: line, path: line }));
  } catch {
    return [];
  }
}

async function detectRunningClient() {
  const entries =
    process.platform === 'win32' ? await listWindowsOsuProcesses() : await listUnixOsuProcesses();
  let stable = false;
  let lazer = false;
  for (const entry of entries) {
    const kind = classifyOsuProcess(entry.name, entry.path);
    if (kind === 'stable') stable = true;
    if (kind === 'lazer') lazer = true;
  }
  if (stable && !lazer) return 'stable';
  if (lazer && !stable) return 'lazer';
  return 'system';
}

function splitCommand(command) {
  const tokens = [];
  let current = '';
  let quote = null;
  for (const character of command) {
    if (quote) {
      if (character === quote) quote = null;
      else current += character;
      continue;
    }
    if (character === '"' || character === "'") {
      quote = character;
      continue;
    }
    if (/\s/.test(character)) {
      if (current) {
        tokens.push(current);
        current = '';
      }
      continue;
    }
    current += character;
  }
  if (current) tokens.push(current);
  return tokens;
}

function spawnDetached(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      detached: true,
      stdio: 'ignore',
      windowsHide: true,
    });
    child.once('error', reject);
    child.unref();
    setImmediate(resolve);
  });
}

function isFileCommand(command) {
  return path.isAbsolute(command) || command.includes('/') || command.includes('\\');
}

async function launchApp(appPath, url) {
  if (isFileCommand(appPath) && !exists(appPath)) {
    throw new Error(`Could not find ${appPath}`);
  }

  if (process.platform === 'darwin' && appPath.endsWith('.app')) {
    await spawnDetached('open', ['-a', appPath, url]);
    return;
  }

  const base = path.basename(appPath).toLowerCase();
  const args = base === 'osu-wine' || base === 'osu-wine.bat' ? ['--osuhandler', url] : [url];
  await spawnDetached(appPath, args);
}

async function launchCustom(command, url) {
  const expanded = command.includes('{url}')
    ? command.split('{url}').join(url)
    : `${command.trim()} ${url}`;
  const tokens = splitCommand(expanded);
  if (!tokens.length) {
    throw new Error('Custom command is empty.');
  }
  if (isFileCommand(tokens[0]) && !exists(tokens[0])) {
    throw new Error(`Could not find ${tokens[0]}`);
  }
  await spawnDetached(tokens[0], tokens.slice(1));
}

function clientLabel(target) {
  if (target === 'stable') return 'osu!(stable)';
  if (target === 'lazer') return 'osu!(lazer)';
  return 'osu!';
}

async function openOsuUrl(
  { url, target, path: userPath, stablePath, lazerPath, songsFolder } = {},
  electronShell
) {
  if (typeof url !== 'string' || !OSU_URL.test(url)) {
    return { ok: false, error: 'Invalid osu! link.' };
  }

  try {
    let resolvedTarget = target;
    let resolvedPath = userPath;

    if (target === 'current') {
      resolvedTarget = await detectRunningClient();
      resolvedPath = resolvedTarget === 'stable' ? stablePath : resolvedTarget === 'lazer' ? lazerPath : undefined;
    }

    if (!resolvedTarget || resolvedTarget === 'system') {
      await electronShell.openExternal(url);
      return { ok: true };
    }

    if (resolvedTarget === 'custom') {
      const command = typeof resolvedPath === 'string' ? resolvedPath.trim() : '';
      if (!command) {
        return { ok: false, error: 'Set a custom timestamp command in Settings.' };
      }
      await launchCustom(command, url);
      return { ok: true };
    }

    const resolved =
      (typeof resolvedPath === 'string' && resolvedPath.trim()) ||
      (await detectOsuExecutable({ client: resolvedTarget, songsFolder }));
    if (!resolved) {
      if (target === 'current') {
        await electronShell.openExternal(url);
        return { ok: true };
      }
      return {
        ok: false,
        error: `Could not find ${clientLabel(resolvedTarget)}. Set a path in Settings.`,
      };
    }

    await launchApp(resolved, url);
    return { ok: true };
  } catch (error) {
    if (target === 'current') {
      try {
        await electronShell.openExternal(url);
        return { ok: true };
      } catch {
        // Keep the original error below.
      }
    }
    return { ok: false, error: error.message || `Could not open ${clientLabel(target)}.` };
  }
}

module.exports = {
  detectOsuExecutable,
  openOsuUrl,
  splitCommand,
};
