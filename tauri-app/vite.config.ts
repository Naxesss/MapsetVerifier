import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { readFileSync } from "fs";
import { resolve } from "path";

// Read version from tauri.conf.json
let tauriVersion = "unknown";
try {
  const confPath = resolve(__dirname, "src-tauri", "tauri.conf.json");
  const conf = JSON.parse(readFileSync(confPath, "utf-8"));
  tauriVersion = conf.version || "unknown";
} catch {}

// @ts-expect-error process is a nodejs global
const host = process.env.TAURI_DEV_HOST;

// https://vite.dev/config/
export default defineConfig(async () => ({
  plugins: [react()],

  // Vite options tailored for Tauri development and only applied in `tauri dev` or `tauri build`
  //
  // 1. prevent Vite from obscuring rust errors
  clearScreen: false,
  // 2. tauri expects a fixed port, fail if that port is not available
  server: {
    port: 1420,
    strictPort: true,
    host: host || false,
    hmr: host
      ? {
          protocol: "ws",
          host,
          port: 1421,
        }
      : undefined,
    watch: {
      // 3. tell Vite to ignore watching `src-tauri` and runtime log output
      // Ignore Logs folder because sidecar writes there and we don't want full app reloads on log churn.
      ignored: ["**/src-tauri/**", "Logs/**"],
    },
  },
  define: {
    TAURI_APP_VERSION: JSON.stringify(tauriVersion),
  },
}));
