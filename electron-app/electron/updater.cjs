const fs = require('fs');
const os = require('os');
const path = require('path');
const { ipcMain, app } = require('electron');
const { autoUpdater } = require('electron-updater');
const { isSemverPreRelease } = require('./semverPrerelease.cjs');

const UPDATER_CACHE_DIR_NAME = 'mapsetverifier-updater';
const UPDATER_LOG_MAX_BYTES = 512 * 1024;

function createUpdaterLogger() {
  const write = (level, args) => {
    const message = args
      .map((arg) => {
        if (arg instanceof Error) return arg.stack || arg.message;
        if (typeof arg === 'string') return arg;
        try {
          return JSON.stringify(arg);
        } catch {
          return String(arg);
        }
      })
      .join(' ');
    const line = `[${new Date().toISOString()}] [${level}] ${message}\n`;
    try {
      const logFile = path.join(app.getPath('userData'), 'updater.log');
      if (fs.existsSync(logFile) && fs.statSync(logFile).size > UPDATER_LOG_MAX_BYTES) {
        fs.renameSync(logFile, `${logFile}.old`);
      }
      fs.appendFileSync(logFile, line);
    } catch {
      // Logging must never break updates.
    }
    const consoleMethod = level === 'error' ? 'error' : level === 'warn' ? 'warn' : 'info';
    console[consoleMethod]('[Updater]', message);
  };

  return {
    info: (...args) => write('info', args),
    warn: (...args) => write('warn', args),
    error: (...args) => write('error', args),
    debug: (...args) => write('debug', args),
  };
}

function cachedInstallerPath() {
  const cacheRoot =
    process.platform === 'win32'
      ? process.env.LOCALAPPDATA || path.join(os.homedir(), 'AppData', 'Local')
      : process.platform === 'darwin'
        ? path.join(os.homedir(), 'Library', 'Caches')
        : process.env.XDG_CACHE_HOME || path.join(os.homedir(), '.cache');
  return path.join(cacheRoot, UPDATER_CACHE_DIR_NAME, 'installer.exe');
}

function logCachedInstaller() {
  if (process.platform !== 'win32') return;
  const installerPath = cachedInstallerPath();
  try {
    const { size } = fs.statSync(installerPath);
    autoUpdater.logger.info(`Cached installer present (${size} bytes): ${installerPath}`);
  } catch {
    autoUpdater.logger.warn(
      `Cached installer missing (first update will be a full download): ${installerPath}`
    );
  }
}

function configureDifferentialUpdates() {
  autoUpdater.logger = createUpdaterLogger();
  // Classic NSIS, not nsis-web. Avoids the "set disableWebInstaller" warning
  // and keeps the updater on the installer+blockmap path.
  autoUpdater.disableWebInstaller = true;

  if (process.platform === 'win32') {
    // GitHub's "latest" asset URLs do not contain the old version, so the
    // default blockmap rewrite cannot find the previous release's .blockmap.
    autoUpdater.previousBlockmapBaseUrlOverride =
      `https://github.com/Naxesss/MapsetVerifier/releases/download/v${app.getVersion()}/`;
  }
}

function forwardingEnabled(getMainWindow) {
  return () => {
    const w = getMainWindow();
    return w && !w.isDestroyed() ? w : null;
  };
}

function serializeUpdateInfo(info) {
  if (!info) return null;
  return {
    version: info.version,
    date: info.releaseDate || null,
    body: typeof info.releaseNotes === 'string' ? info.releaseNotes : null,
  };
}

function applyUpdatePreferences(options) {
  const currentIsPreRelease = isSemverPreRelease(app.getVersion());
  const allowPrerelease =
    options && typeof options.allowPrerelease === 'boolean'
      ? options.allowPrerelease
      : currentIsPreRelease;

  autoUpdater.allowPrerelease = allowPrerelease;
  autoUpdater.allowDowngrade = allowPrerelease || currentIsPreRelease;

  return { allowPrerelease, currentIsPreRelease };
}

function registerUpdater(getMainWindow) {
  const getWin = forwardingEnabled(getMainWindow);
  
  if (!app.isPackaged) {
    // For testing only on local builds
    autoUpdater.forceDevUpdateConfig = true;
  }

  // Never auto-download; the renderer drives the flow via installUpdate().
  autoUpdater.autoDownload = false;
  autoUpdater.autoInstallOnAppQuit = true;
  configureDifferentialUpdates();
  applyUpdatePreferences();

  const send = (channel, payload) => {
    const w = getWin();
    if (w) w.webContents.send(channel, payload);
  };

  autoUpdater.on('checking-for-update', () => send('updater:checking'));
  autoUpdater.on('update-available', (info) => send('updater:available', serializeUpdateInfo(info)));
  autoUpdater.on('update-not-available', (info) => send('updater:not-available', serializeUpdateInfo(info)));
  autoUpdater.on('download-progress', (p) => send('updater:progress', {
    percent: p.percent,
    transferred: p.transferred,
    total: p.total,
    bytesPerSecond: p.bytesPerSecond,
  }));
  autoUpdater.on('update-downloaded', (info) => send('updater:downloaded', serializeUpdateInfo(info)));
  autoUpdater.on('error', (err) => send('updater:error', err && err.message ? err.message : String(err)));

  if (!app.isPackaged) {
    // In development, simulate the event flow so the renderer state machine works.
    ipcMain.handle('updater:check', async (_event, options) => {
      applyUpdatePreferences(options);
      console.info('[Updater] Mocking updater');
      send('updater:checking');
      setTimeout(() => {
        send('updater:not-available', null);
      }, 300);
      return null;
    });
  } else {
    ipcMain.handle('updater:check', async (_event, options) => {
      console.info('[Updater] IPC check received');

      try {
        const preferences = applyUpdatePreferences(options);
        console.info('[Updater] preferences applied', preferences);
        console.info('[Updater] calling checkForUpdates');
        const result = await autoUpdater.checkForUpdates();
        console.info('[Updater] checkForUpdates returned');
        return serializeUpdateInfo(result?.updateInfo);
      } catch (e) {
        // electron-updater's GitHub provider can resolve the "latest" tag to a
        // release whose channel file (latest.yml/beta.yml) isn't downloadable yet -
        // e.g. a draft release awaiting manual publish. Treat that as "no update
        // available" instead of surfacing it as an error to the user.
        if (e && e.code === 'ERR_UPDATER_CHANNEL_FILE_NOT_FOUND') {
          console.warn('[Updater] latest release artifacts not accessible yet (likely a draft release), treating as up-to-date', e.message);
          send('updater:not-available', null);
          return null;
        }
        console.error('[Updater] check failed', e);
        send('updater:error', e.message || String(e));
        return null;
      }
    });
  }
  ipcMain.handle('updater:download', async () => {
    try {
      logCachedInstaller();
      await autoUpdater.downloadUpdate();
      return true;
    } catch (e) {
      send('updater:error', e && e.message ? e.message : String(e));
      return false;
    }
  });
  ipcMain.handle('updater:quitAndInstall', () => {
    // isSilent=false, forceRunAfter=true - same as the legacy app/main.js.
    setTimeout(() => autoUpdater.quitAndInstall(false, true), 250);
  });
}

module.exports = { registerUpdater };
