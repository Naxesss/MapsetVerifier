# Development

This guide covers building Mapset Verifier locally, packaging sidecar binaries, and how the project is structured.

For release automation and tagging, see [RELEASES.md](RELEASES.md). For custom check plugins, see [CUSTOM_CHECKS.md](CUSTOM_CHECKS.md).

---

## Dependencies

You will need:

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS recommended)

## Git hooks

Running `npm install` at the repository root sets up a pre-commit hook (via [husky](https://typicode.github.io/husky/)) that automatically formats staged files before each commit:

- `electron-app/src/**/*.{ts,tsx}` — Prettier, then ESLint `--fix`
- `electron-app/src/**/*.{css,scss,md}` — Prettier
- `**/*.cs` — `dotnet csharpier format`

Only staged files are touched, and the commit is aborted if a tool fails (e.g. the file doesn't compile), so you no longer need a separate "fix lint" commit afterwards.

## Building locally

The launcher scripts build the backend sidecar if needed, install npm dependencies, start the .NET server, and run the Electron frontend in dev mode.

### Windows

From the repository root:

```bat
scripts\start.bat
```

### Linux and macOS

```sh
chmod +x scripts/start.sh
./scripts/start.sh
```

### npm scripts (alternative)

From the repository root:

| Command | Description |
| :-- | :-- |
| `npm run dev:all` | Vite dev server + Electron + backend (Debug build) |
| `npm run dev` | Frontend + Electron only (backend must be running separately) |
| `npm run build:backend` | Build the .NET backend (`Debug`) |
| `npm run build` | Production frontend build |
| `npm run dist` | Windows x64 installer (local, no publish) |
| `npm run dist-all` | Windows + Linux installers (local, no publish) |

Install dependencies first:

```bash
npm ci
npm --prefix electron-app ci
```

## Building sidecars for other systems

Sidecar binaries are laid out under `bin/server/dist/<runtime>/` and bundled into the Electron app via `extraResources`.

```bat
scripts\build-sidecars.bat <runtimes>
```

```sh
scripts/build-sidecars.sh <runtimes>
```

Available runtimes:

- `win-x64`
- `win-arm64`
- `mac-x64`
- `mac-arm64`
- `linux-x64`
- `linux-arm64`

---

## Technical overview (MV 2.0)

MV 2.0 is an [Electron](https://www.electronjs.org/) desktop app with a **React + TypeScript** frontend ([Vite](https://vitejs.dev/), [Mantine](https://mantine.dev/)) under `electron-app/`.

The backend is a **.NET 9** [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) sidecar (`src/MapsetVerifier.csproj`) spawned by Electron on startup. The UI talks to the backend over HTTP; real-time updates use [SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr).

Check logic lives in `MapsetVerifier.Checks/` on top of `MapsetVerifier.Framework/` and `MapsetVerifier.Parser/`. Built-in check metadata is exported for the frontend via `MapsetVerifier.Exports/`.

Packaged builds place platform-specific backend binaries in `resources/bin/server/dist/<rid>/` inside the app bundle.

---

## Technical overview (MV 1.x)

> [!WARNING]
> **This section describes the old 1.x architecture and is outdated.** MV 2.0 replaced the jQuery frontend with React/TypeScript, retargeted the backend to .NET 9, and reworked how checks and plugins are loaded. Keep this here only as historical context for the 1.x release line.

The backend was written in [C#/.NET](https://dotnet.microsoft.com/), with a simple [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) server. The frontend was an [Electron](https://www.electronjs.org/) desktop application written in HTML, CSS, and [JS/jQuery](https://jquery.com/). The server-client communication was done with [SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr). Upon selecting a mapset, the frontend requested tabs to be rendered, and the backend returned HTML.

![](https://i.imgur.com/3kHWOmH.png?sanitize=true)

Custom check plugins for 1.x used a different folder layout (`checks` instead of `CustomChecks`) and targeted .NET Core 3.1. See [CUSTOM_CHECKS.md](CUSTOM_CHECKS.md#migrating-from-mv-1x-plugins) for migration notes.
