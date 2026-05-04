const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

const BACKEND_PORT = 5005;
const EXE_NAMES_WIN = ['MapsetVerifier', 'MapsetVerifier.Server'];
const UNIX_MATCH = 'MapsetVerifier';

let sidecarProcess = null;
let logListeners = [];

function emitLog(line) {
  for (const cb of logListeners) {
    try { cb(line); } catch { /* no-op */ }
  }
}

function onLog(cb) {
  logListeners.push(cb);
  return () => { logListeners = logListeners.filter((l) => l !== cb); };
}

function runHidden(command, args) {
  return spawn(command, args, { windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
}

function cleanupSidecar() {
  if (process.platform === 'win32') {
    const listScript = [
      `$port = ${BACKEND_PORT};`,
      '$conns = Get-NetTCPConnection -LocalPort $port -State Listen,Established,TimeWait,CloseWait -ErrorAction SilentlyContinue;',
      '$pids = $conns | Select-Object -ExpandProperty OwningProcess;',
      '$pids | Where-Object { $_ -ne $PID -and $_ -ne 0 } | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue };',
      EXE_NAMES_WIN.map((n) => `Get-Process -Name '${n}' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue;`).join(' '),
    ].join(' ');
    try {
      const p = spawn('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', listScript], { windowsHide: true });
      p.on('error', () => { /* ignore */ });
    } catch { /* ignore */ }
  } else {
    try {
      spawn('sh', ['-c', `lsof -t -i :${BACKEND_PORT} 2>/dev/null | xargs -r kill -9 2>/dev/null; pkill -9 -f ${UNIX_MATCH} 2>/dev/null; true`], { stdio: 'ignore' });
    } catch { /* ignore */ }
  }
}

function resolveSidecarPath() {
  const isPackaged = require('electron').app.isPackaged;
  const exe = process.platform === 'win32' ? 'MapsetVerifier.exe' : 'MapsetVerifier';
  const arch = process.arch === 'arm64' ? 'arm64' : 'x64';
  const rid =
    process.platform === 'win32' ? `win-${arch}` :
      process.platform === 'darwin' ? `mac-${arch}` :
        `linux-${arch}`;
  
  if (isPackaged) {
    return path.join(process.resourcesPath, 'bin', 'server', 'dist', rid, exe);
  }
  return path.join(__dirname, '..', '..', 'bin', 'server', 'dist', rid, exe);
}

function spawnSidecar() {
  if (sidecarProcess) return sidecarProcess;
  const exePath = resolveSidecarPath();
  if (!fs.existsSync(exePath)) {
    emitLog(`[sidecar] executable not found at ${exePath}`);
    return null;
  }
  emitLog(`[sidecar] spawning ${exePath}`);
  try {
    sidecarProcess = spawn(exePath, [`--urls=http://localhost:${BACKEND_PORT}`], {
      windowsHide: true,
      stdio: ['ignore', 'pipe', 'pipe'],
      cwd: path.dirname(exePath),
    });
  } catch (e) {
    emitLog(`[sidecar] failed to spawn: ${e && e.message ? e.message : e}`);
    sidecarProcess = null;
    return null;
  }
  sidecarProcess.stdout.on('data', (d) => emitLog(`[stdout] ${d.toString().trimEnd()}`));
  sidecarProcess.stderr.on('data', (d) => emitLog(`[stderr] ${d.toString().trimEnd()}`));
  sidecarProcess.on('close', (code, signal) => {
    emitLog(`[sidecar] exited with code ${code}, signal ${signal}`);
    sidecarProcess = null;
  });
  sidecarProcess.on('error', (err) => {
    emitLog(`[sidecar] error: ${err && err.message ? err.message : err}`);
  });
  return sidecarProcess;
}

function killSidecar() {
  if (sidecarProcess && !sidecarProcess.killed) {
    try { sidecarProcess.kill(); } catch { /* ignore */ }
  }
  sidecarProcess = null;
}

function isRunning() {
  return !!sidecarProcess && !sidecarProcess.killed;
}

module.exports = { cleanupSidecar, spawnSidecar, killSidecar, onLog, isRunning, BACKEND_PORT };
