const { ipcMain, app } = require('electron');
const { autoUpdater } = require('electron-updater');

// Pre-release if current version ends with a non-digit (e.g. 1.1.0-a).
function isPreRelease() {
  const v = app.getVersion();
  return v.length > 0 && !/[0-9]/.test(v.slice(-1));
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

function registerUpdater(getMainWindow) {
  const getWin = forwardingEnabled(getMainWindow);

  // Never auto-download; the renderer drives the flow via installUpdate().
  autoUpdater.autoDownload = false;
  autoUpdater.autoInstallOnAppQuit = true;
  autoUpdater.allowPrerelease = isPreRelease();

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

  ipcMain.handle('updater:check', async () => {
    try {
      const result = await autoUpdater.checkForUpdates();
      return serializeUpdateInfo(result && result.updateInfo);
    } catch (e) {
      send('updater:error', e && e.message ? e.message : String(e));
      return null;
    }
  });
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
