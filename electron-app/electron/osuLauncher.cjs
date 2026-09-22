const { execFile, spawn } = require('child_process');
const { shell } = require('electron');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { promisify } = require('util');
const sidecar = require('./sidecar.cjs');

const execFileAsync = promisify(execFile);
const OSU_URL = /^osu:/i;
const FLATPAK_LAZER_IDS = ['sh.ppy.osu', 'sh.ppy.osulazer', 'com.github.ppy.osu'];
const UNIX_PATH_DIRS = [path.join(os.homedir(), '.local', 'bin'), '/usr/bin', '/usr/local/bin'];

// How long a launcher has to stay alive before we call the launch successful.
const LAUNCH_GRACE_MS = 1500;
const LAUNCH_LOG_TAIL = 600;

// The Linux build ships as an AppImage, whose AppRun points the dynamic loader
// and various toolkit lookups inside the mounted squashfs. Children inherit all
// of it, which breaks anything that loads its own runtime - osu-wine goes
// through yawl/pressure-vessel, and that dies instantly on a foreign
// LD_LIBRARY_PATH. Drop the AppImage-only variables before spawning.
const APPIMAGE_ONLY_ENV = [
  'APPDIR',
  'APPIMAGE',
  'APPIMAGE_UUID',
  'ARGV0',
  'OWD',
  'LD_PRELOAD',
  'PYTHONHOME',
  'PYTHONPATH',
  'PERLLIB',
  'GCONV_PATH',
  'GIO_MODULE_DIR',
  'GDK_PIXBUF_MODULE_FILE',
  'GDK_PIXBUF_MODULEDIR',
  'GSETTINGS_SCHEMA_DIR',
  'GST_PLUGIN_SYSTEM_PATH',
  'GST_PLUGIN_SYSTEM_PATH_1_0',
  'QT_PLUGIN_PATH',
  'CHROME_DESKTOP',
];

// Variables that hold a list of directories and may have AppDir entries mixed in
// with the user's real ones, so they get filtered instead of dropped.
const APPIMAGE_PATH_LIST_ENV = ['LD_LIBRARY_PATH', 'XDG_DATA_DIRS', 'XDG_CONFIG_DIRS'];

let cachedFlatpakLazer = undefined;

function withoutAppDirEntries(value, appDir) {
  return value
    .split(path.delimiter)
    .filter((entry) => entry && !entry.startsWith(appDir))
    .join(path.delimiter);
}

function spawnEnv() {
  if (process.platform === 'win32') return process.env;

  const env = { ...process.env };
  const appDir = env.APPDIR;
  // Only scrub when we are actually running from an AppImage, so a plain Linux
  // or macOS run keeps whatever the user set up themselves.
  const insideAppImage = Boolean(appDir && env.APPIMAGE);

  if (insideAppImage) {
    for (const key of APPIMAGE_ONLY_ENV) {
      delete env[key];
    }

    for (const key of APPIMAGE_PATH_LIST_ENV) {
      if (!env[key]) continue;
      const remaining = withoutAppDirEntries(env[key], appDir);
      // An empty LD_LIBRARY_PATH is not the same as an unset one, so remove it.
      if (remaining) env[key] = remaining;
      else delete env[key];
    }
  }

  const basePath = insideAppImage
    ? withoutAppDirEntries(process.env.PATH || '', appDir)
    : process.env.PATH || '';
  env.PATH = [...UNIX_PATH_DIRS, basePath].join(path.delimiter);

  return env;
}

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
  // Resolve against the same PATH the child will get, so we never pick a binary
  // out of the AppImage mount that the child could not load anyway.
  const dirs = (spawnEnv().PATH || '').split(path.delimiter);
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

async function detectStableExecutable(songsFolder) {
  if (process.platform === 'win32') {
    const sibling = songsFolder ? path.join(path.dirname(songsFolder), 'osu!.exe') : null;
    const local = path.join(process.env.LOCALAPPDATA || '', 'osu!', 'osu!.exe');
    const fromRegistry =
      (await queryWindowsExe('HKCR\\osu!\\shell\\open\\command')) ||
      (await queryWindowsExe('HKCR\\osu!\\DefaultIcon')) ||
      (await queryWindowsExe('HKCU\\Software\\Classes\\osu!\\shell\\open\\command'));
    return firstExisting([sibling, fromRegistry, local]);
  }

  if (process.platform === 'linux') return resolveOnPath('osu-wine');
  return null;
}

async function detectFlatpakLazer() {
  if (cachedFlatpakLazer !== undefined) return cachedFlatpakLazer;
  const flatpak = resolveOnPath('flatpak');
  if (!flatpak) {
    cachedFlatpakLazer = null;
    return null;
  }
  for (const id of FLATPAK_LAZER_IDS) {
    try {
      await execFileAsync(flatpak, ['info', id], {
        timeout: 4000,
        env: spawnEnv(),
        windowsHide: true,
      });
      cachedFlatpakLazer = `flatpak:${id}`;
      return cachedFlatpakLazer;
    } catch {
      // Try the next known id.
    }
  }
  cachedFlatpakLazer = null;
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
    resolveOnPath('osu!') ||
    firstExisting([
      path.join(os.homedir(), 'Applications', 'osu.AppImage'),
      path.join(os.homedir(), 'Applications', 'osu-lazer.AppImage'),
      path.join(os.homedir(), 'Downloads', 'osu.AppImage'),
      path.join(os.homedir(), 'Downloads', 'osu-lazer.AppImage'),
      path.join(os.homedir(), '.local', 'bin', 'osu!'),
    ]) ||
    (await detectFlatpakLazer())
  );
}

async function detectOsuExecutable({ client, songsFolder } = {}) {
  if (client === 'lazer') return detectLazerExecutable();
  if (client === 'stable') return detectStableExecutable(songsFolder);
  return null;
}

async function detectRunningClient() {
  try {
    const response = await fetch(
      `http://localhost:${sidecar.BACKEND_PORT}/beatmap/runningClients`,
      { signal: AbortSignal.timeout(2000) }
    );
    if (!response.ok) return null;
    const data = await response.json();
    if (data.stable && !data.lazer) return 'stable';
    if (data.lazer && !data.stable) return 'lazer';
    return null;
  } catch {
    return null;
  }
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

// Capture the launcher's output to a temp file rather than a pipe: the child is
// detached and long-lived, and closing a pipe on it later would hand it EPIPE.
function openLaunchLog() {
  if (process.platform === 'win32') return null;
  try {
    const logPath = path.join(
      os.tmpdir(),
      `mapsetverifier-launch-${process.pid}-${Date.now()}.log`
    );
    return { path: logPath, fd: fs.openSync(logPath, 'a') };
  } catch {
    return null;
  }
}

function readLaunchLog(log) {
  if (!log) return '';
  try {
    const text = fs.readFileSync(log.path, 'utf8').trim();
    if (!text) return '';
    return text.slice(-LAUNCH_LOG_TAIL).replace(/\s+/g, ' ').trim();
  } catch {
    return '';
  }
}

function closeLaunchLog(log) {
  if (!log) return;
  try {
    fs.closeSync(log.fd);
  } catch {
    // Already closed.
  }
  try {
    fs.unlinkSync(log.path);
  } catch {
    // The child may still hold it open; on unix the name is gone either way.
  }
}

function describeExit(command, code, signal, details) {
  const name = path.basename(command);
  const reason = signal ? `was killed by ${signal}` : `exited with code ${code}`;
  return details ? `${name} ${reason}: ${details}` : `${name} ${reason}.`;
}

function spawnDetached(command, args) {
  return new Promise((resolve, reject) => {
    const log = openLaunchLog();
    let child;

    try {
      child = spawn(command, args, {
        detached: true,
        stdio: log ? ['ignore', log.fd, log.fd] : 'ignore',
        windowsHide: true,
        env: spawnEnv(),
      });
    } catch (error) {
      closeLaunchLog(log);
      reject(error);
      return;
    }

    let settled = false;
    let timer = null;

    const settle = (error) => {
      if (settled) return;
      settled = true;
      if (timer) clearTimeout(timer);
      if (error) reject(error);
      else resolve();
    };

    child.once('error', (error) => {
      closeLaunchLog(log);
      settle(error);
    });

    // A launcher that hands the link off and dies within the grace period has
    // failed, and its output is the only clue as to why. Without this the
    // renderer is told the launch succeeded and the user sees nothing at all.
    child.once('exit', (code, signal) => {
      const details = readLaunchLog(log);
      closeLaunchLog(log);
      if (code === 0 && !signal) {
        settle();
        return;
      }
      const message = describeExit(command, code, signal, details);
      console.error('[OsuLauncher]', message);
      settle(new Error(message));
    });

    child.once('spawn', () => {
      child.unref();
      // Deliberately not unref'd: the child is detached and the promise must
      // still settle once the grace period is up.
      timer = setTimeout(() => {
        // Still running, so the handoff worked - stop watching it.
        closeLaunchLog(log);
        settle();
      }, LAUNCH_GRACE_MS);
    });
  });
}

function isFileCommand(command) {
  return path.isAbsolute(command) || command.includes('/') || command.includes('\\');
}

function ensureExecutable(filePath) {
  if (process.platform === 'win32' || !filePath || filePath.endsWith('.app')) return;
  try {
    if (!fs.statSync(filePath).isFile()) return;
    fs.accessSync(filePath, fs.constants.X_OK);
  } catch {
    try {
      fs.chmodSync(filePath, fs.statSync(filePath).mode | 0o111);
    } catch {
      // launch will surface EACCES
    }
  }
}

async function launchApp(appPath, url) {
  if (typeof appPath === 'string' && appPath.startsWith('flatpak:')) {
    const id = appPath.slice('flatpak:'.length);
    await spawnDetached(resolveOnPath('flatpak') || 'flatpak', ['run', id, url]);
    return;
  }

  if (isFileCommand(appPath) && !exists(appPath)) {
    throw new Error(`Could not find ${appPath}`);
  }

  if (process.platform === 'darwin' && appPath.endsWith('.app')) {
    await spawnDetached('open', ['-a', appPath, url]);
    return;
  }

  if (process.platform !== 'win32' && appPath.toLowerCase().endsWith('.exe')) {
    const wineLauncher = resolveOnPath('osu-wine');
    if (wineLauncher) {
      await spawnDetached(wineLauncher, ['--osuhandler', url]);
      return;
    }
    throw new Error(
      'osu!.exe cannot be launched directly on this OS. Use osu-wine or a custom command in Experimental settings.'
    );
  }

  ensureExecutable(appPath);
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
  ensureExecutable(tokens[0]);
  await spawnDetached(tokens[0], tokens.slice(1));
}

function clientLabel(target) {
  if (target === 'stable') return 'osu!(stable)';
  if (target === 'lazer') return 'osu!(lazer)';
  return 'osu!';
}

function missingClientError(target, resolvedTarget) {
  const label = clientLabel(resolvedTarget);
  if (target === 'current') {
    return `Could not launch ${label}. Pick ${label} in Experimental settings and set a path, or use a custom command.`;
  }
  return `Could not find ${label}. Set a path in Settings.`;
}

async function openOsuUrl({ url, target, stablePath, lazerPath, customCommand, songsFolder } = {}) {
  if (typeof url !== 'string' || !OSU_URL.test(url)) {
    return { ok: false, error: 'Invalid osu! link.' };
  }

  try {
    const resolvedTarget = target === 'current' ? await detectRunningClient() : target;

    if (resolvedTarget !== 'stable' && resolvedTarget !== 'lazer' && resolvedTarget !== 'custom') {
      // Detection was inconclusive (neither client recognised, or both running).
      // If the user configured exactly one client, honour that instead of
      // handing the link to the desktop's osu:// handler, which on Linux is
      // whichever client registered itself last.
      const configured = [stablePath, lazerPath].filter(
        (candidate) => typeof candidate === 'string' && candidate.trim()
      );
      if (configured.length === 1) {
        await launchApp(configured[0].trim(), url);
        return { ok: true };
      }

      await shell.openExternal(url);
      return { ok: true };
    }

    if (resolvedTarget === 'custom') {
      const command = typeof customCommand === 'string' ? customCommand.trim() : '';
      if (!command) {
        return { ok: false, error: 'Set a custom timestamp command in Settings.' };
      }
      await launchCustom(command, url);
      return { ok: true };
    }

    const configuredPath = resolvedTarget === 'stable' ? stablePath : lazerPath;
    const resolved =
      (typeof configuredPath === 'string' && configuredPath.trim()) ||
      (await detectOsuExecutable({ client: resolvedTarget, songsFolder }));
    if (!resolved) {
      return { ok: false, error: missingClientError(target, resolvedTarget) };
    }

    await launchApp(resolved, url);
    return { ok: true };
  } catch (error) {
    return {
      ok: false,
      error: error.message || missingClientError(target, target),
    };
  }
}

module.exports = {
  detectOsuExecutable,
  openOsuUrl,
};
