const { contextBridge, ipcRenderer } = require('electron');

function on(channel, cb) {
  const listener = (_event, payload) => cb(payload);
  ipcRenderer.on(channel, listener);
  return () => ipcRenderer.removeListener(channel, listener);
}

contextBridge.exposeInMainWorld('electronAPI', {
  platform: process.platform,

  getVersion: () => ipcRenderer.invoke('app:version'),

  app: {
    getAppFolderPath: () => ipcRenderer.invoke('app:getAppFolderPath'),
    getExternalsFolderPath: () => ipcRenderer.invoke('app:getExternalsFolderPath'),
    getSnapshotFolderPath: (beatmapSetId, subfolder) =>
      ipcRenderer.invoke('app:getSnapshotFolderPath', beatmapSetId, subfolder),
  },

  window: {
    minimize: () => ipcRenderer.invoke('window:minimize'),
    maximize: () => ipcRenderer.invoke('window:maximize'),
    toggleMaximize: () => ipcRenderer.invoke('window:toggleMaximize'),
    close: () => ipcRenderer.invoke('window:close'),
    isMaximized: () => ipcRenderer.invoke('window:isMaximized'),
  },

  shell: {
    openPath: (p) => ipcRenderer.invoke('shell:openPath', p),
    openExternal: (url) => ipcRenderer.invoke('shell:openExternal', url),
  },

  dialog: {
    openFolder: () => ipcRenderer.invoke('dialog:openFolder'),
  },

  settings: {
    exists: () => ipcRenderer.invoke('settings:exists'),
    read: () => ipcRenderer.invoke('settings:read'),
    write: (text) => ipcRenderer.invoke('settings:write', text),
  },

  backend: {
    status: () => ipcRenderer.invoke('backend:status'),
    start: (options) => ipcRenderer.invoke('backend:start', options),
    onLog: (cb) => on('backend:log', cb),
  },

  updater: {
    check: (options) => ipcRenderer.invoke('updater:check', options),
    download: () => ipcRenderer.invoke('updater:download'),
    quitAndInstall: () => ipcRenderer.invoke('updater:quitAndInstall'),
    onChecking: (cb) => on('updater:checking', cb),
    onAvailable: (cb) => on('updater:available', cb),
    onNotAvailable: (cb) => on('updater:not-available', cb),
    onProgress: (cb) => on('updater:progress', cb),
    onDownloaded: (cb) => on('updater:downloaded', cb),
    onError: (cb) => on('updater:error', cb),
  },
});
