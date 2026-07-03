const { ipcMain, shell, dialog, BrowserWindow, app } = require('electron');
const fs = require('fs/promises');
const path = require('path');
const sidecar = require('./sidecar.cjs');

const SETTINGS_FILE = 'settings.json';
const EXTERNALS_FOLDER_NAME = 'Mapset Verifier Externals';

function resolveBackendAppDataRoot() {
  if (process.platform === 'linux') {
    return process.env.XDG_DATA_HOME || path.join(app.getPath('home'), '.local', 'share');
  }
  return app.getPath('appData');
}

function externalsFolderPath() {
  return path.join(resolveBackendAppDataRoot(), EXTERNALS_FOLDER_NAME);
}

function settingsPath() {
  return path.join(app.getPath('userData'), SETTINGS_FILE);
}

async function ensureConfigDir() {
  await fs.mkdir(app.getPath('userData'), { recursive: true });
}

function focusedWindow(event) {
  const sender = event && event.sender ? BrowserWindow.fromWebContents(event.sender) : null;
  return sender || BrowserWindow.getFocusedWindow();
}

function registerIpc(getMainWindow) {
  ipcMain.handle('window:minimize', (e) => { const w = focusedWindow(e); if (w) w.minimize(); });
  ipcMain.handle('window:maximize', (e) => { const w = focusedWindow(e); if (w) w.maximize(); });
  ipcMain.handle('window:toggleMaximize', (e) => {
    const w = focusedWindow(e);
    if (!w) return false;
    if (w.isMaximized()) w.unmaximize(); else w.maximize();
    return w.isMaximized();
  });
  ipcMain.handle('window:close', (e) => { const w = focusedWindow(e); if (w) w.close(); });
  ipcMain.handle('window:isMaximized', (e) => {
    const w = focusedWindow(e);
    return w ? w.isMaximized() : false;
  });

  ipcMain.handle('shell:openPath', async (_e, p) => {
    if (!p || typeof p !== 'string') return 'invalid path';
    return shell.openPath(p);
  });
  ipcMain.handle('shell:openExternal', async (_e, url) => {
    if (!url || typeof url !== 'string') return;
    await shell.openExternal(url);
  });

  ipcMain.handle('dialog:openFolder', async (e) => {
    const w = focusedWindow(e);
    const result = await dialog.showOpenDialog(w || undefined, {
      properties: ['openDirectory'],
    });
    if (result.canceled || !result.filePaths.length) return null;
    return result.filePaths[0];
  });

  ipcMain.handle('settings:exists', async () => {
    try {
      await fs.access(settingsPath());
      return true;
    } catch {
      return false;
    }
  });
  ipcMain.handle('settings:read', async () => {
    try {
      await ensureConfigDir();
      const text = await fs.readFile(settingsPath(), 'utf-8');
      return text;
    } catch (e) {
      if (e && e.code === 'ENOENT') return null;
      throw e;
    }
  });
  ipcMain.handle('settings:write', async (_e, text) => {
    await ensureConfigDir();
    await fs.writeFile(settingsPath(), typeof text === 'string' ? text : JSON.stringify(text, null, 2), 'utf-8');
  });

  ipcMain.handle('backend:status', () => ({
    running: sidecar.isRunning(),
    port: sidecar.BACKEND_PORT,
  }));
  ipcMain.handle('backend:start', (_e, options) => {
    sidecar.spawnSidecar({
      customChecksEnabled: options?.customChecksEnabled !== false,
    });
  });

  ipcMain.handle('app:version', () => app.getVersion());
  ipcMain.handle('app:platform', () => process.platform);
  ipcMain.handle('app:getAppFolderPath', async () => {
    const dir = app.getPath('userData');
    await fs.mkdir(dir, { recursive: true });
    return dir;
  });
  ipcMain.handle('app:getExternalsFolderPath', async () => {
    const dir = externalsFolderPath();
    await fs.mkdir(dir, { recursive: true });
    return dir;
  });
  ipcMain.handle('app:getSnapshotFolderPath', async (_e, beatmapSetId, subfolder) => {
    if (beatmapSetId == null || beatmapSetId === '') return null;
    const base = path.join(externalsFolderPath(), 'snapshots', String(beatmapSetId));
    if (subfolder == null || subfolder === '') return base;
    return path.join(base, String(subfolder));
  });

  sidecar.onLog((line) => {
    const win = getMainWindow();
    if (win && !win.isDestroyed()) {
      win.webContents.send('backend:log', line);
    }
  });
}

module.exports = { registerIpc };
