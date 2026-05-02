const { app, BrowserWindow, globalShortcut } = require('electron');
const path = require('path');
const sidecar = require('./sidecar.cjs');
const { registerIpc } = require('./ipc.cjs');
const { registerUpdater } = require('./updater.cjs');

let mainWindow = null;

const DEV_SERVER_URL = process.env.VITE_DEV_SERVER_URL;
const isDev = !!DEV_SERVER_URL;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1172,
    height: 700,
    minWidth: 800,
    minHeight: 600,
    frame: false,
    backgroundColor: '#101014',
    show: false,
    icon: path.join(__dirname, '..', '..', 'assets', 'icons', 'icon.png'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false,
    },
  });

  mainWindow.once('ready-to-show', () => {
    mainWindow.show();

    globalShortcut.register('CommandOrControl+Shift+I', () => mainWindow.webContents.toggleDevTools());
    globalShortcut.register('F12', () => mainWindow.webContents.toggleDevTools());
    globalShortcut.register('CommandOrControl+R', () => mainWindow.reload());
    globalShortcut.register('F5', () => mainWindow.reload());
  });

  if (isDev) {
    mainWindow.loadURL(DEV_SERVER_URL);
  } else {
    mainWindow.loadFile(path.join(__dirname, '..', 'dist', 'index.html'));
  }

  mainWindow.on('closed', () => { mainWindow = null; });
}

// Prevent duplicate instances.
const gotLock = app.requestSingleInstanceLock();
if (!gotLock) {
  app.exit();
} else {
  app.on('second-instance', () => {
    if (mainWindow) {
      if (!mainWindow.isVisible()) mainWindow.show();
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.focus();
    }
  });
}

function getMainWindow() { return mainWindow; }

app.whenReady().then(() => {
  // Proactively free the backend port before spawning (matches legacy cleanup_sidecar).
  sidecar.cleanupSidecar();
  sidecar.spawnSidecar();

  registerIpc(getMainWindow);
  registerUpdater(getMainWindow);

  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  sidecar.killSidecar();
  if (process.platform !== 'darwin') app.quit();
});

app.on('will-quit', () => {
  globalShortcut.unregisterAll();
  sidecar.killSidecar();
  sidecar.cleanupSidecar();
});

process.on('exit', () => {
  sidecar.killSidecar();
});
