const fs = require('fs');

const MIN_ZOOM_PERCENT = 50;
const MAX_ZOOM_PERCENT = 300;
const ZOOM_STEP_LEVEL = 0.5;

let defaultZoomFactor = 1;

function parseZoomPercent(value) {
  if (typeof value !== 'number' || !Number.isFinite(value)) return 100;
  return Math.min(MAX_ZOOM_PERCENT, Math.max(MIN_ZOOM_PERCENT, value));
}

/** Reads the saved default zoom so the window can be created at it without a visible resize. */
function loadDefaultZoomFactor(settingsFile) {
  try {
    const settings = JSON.parse(fs.readFileSync(settingsFile, 'utf-8'));
    defaultZoomFactor = parseZoomPercent(settings?.uiZoomPercent) / 100;
  } catch {
    defaultZoomFactor = 1;
  }
  return defaultZoomFactor;
}

function setDefaultZoom(webContents, percent) {
  defaultZoomFactor = parseZoomPercent(percent) / 100;
  webContents.setZoomFactor(defaultZoomFactor);
}

function registerZoomShortcuts(webContents) {
  // The default menu binds zoom in to CmdOrCtrl+Plus, which only fires with Shift held
  // (Ctrl+Shift+=), and resets to 100% rather than the configured default. Handle the
  // shortcuts here instead, and swallow the shifted variants so Ctrl+Shift+= mirrors
  // Ctrl+Shift+-, which doesn't zoom either.
  webContents.on('before-input-event', (event, input) => {
    if (input.type !== 'keyDown' || !(input.control || input.meta) || input.alt) return;

    let delta = 0;
    let reset = false;
    if (input.key === '=' || input.key === '+' || input.code === 'NumpadAdd') delta = ZOOM_STEP_LEVEL;
    else if (input.code === 'NumpadSubtract') delta = -ZOOM_STEP_LEVEL;
    else if (input.key === '0') reset = true;
    else return;

    event.preventDefault();
    if (input.shift) return;
    if (reset) webContents.setZoomFactor(defaultZoomFactor);
    else webContents.setZoomLevel(webContents.getZoomLevel() + delta);
  });
}

module.exports = { loadDefaultZoomFactor, setDefaultZoom, registerZoomShortcuts };
