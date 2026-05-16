const { ipcMain, app } = require('electron');
const { autoUpdater } = require('electron-updater');
const { isSemverPreRelease } = require('./semverPrerelease.cjs');

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
        console.error('[Updater] check failed', e);
        send('updater:error', e.message || String(e));
        return null;
      }
    });
  }
  ipcMain.handle('updater:download', async () => {
    try {
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
