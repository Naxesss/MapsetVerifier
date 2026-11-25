# Development

This guide covers building Mapset Verifier locally, packaging sidecar binaries, and how the project is structured.

For release automation and tagging, see [RELEASES.md](RELEASES.md). For custom check plugins, see [CUSTOM_CHECKS.md](CUSTOM_CHECKS.md).

---

## Dependencies

You will need:

- [.NET SDK 10](https://dotnet.microsoft.com/download)
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

## Ranking criteria snapshot

Checks link their issue templates to ranking criteria statements. The snapshot of the osu-wiki pages they link to is maintained with a tool, see [RANKING_CRITERIA.md](RANKING_CRITERIA.md).

## Backend test coverage

From the repository root:

```bash
npm run coverage:backend
```

This runs all backend tests and merges their coverage into the git-ignored `coverage` folder, replacing the last run:

- `coverage/report/index.html`: the report to browse, down to each line
- `coverage/summary.txt`: line and branch coverage per project, also printed at the end

Every project in the solution is included, so code which no test loads counts as 0%. Left out are the test projects, generated code (`*.g.cs` and build output) and auto-properties. If tests fail there's still a report, but it's incomplete and the command fails.

The tools are part of the .NET tool manifest and restored by the command itself. What is measured is configured in `scripts/coverage.runsettings` and `scripts/coverage.mjs`.

A covered line only means that a test ran it, not that it would notice if the line were wrong. In `MapsetVerifier.Checks` the check documentation counts as well, which runs with any test going through all checks.

## Backend tests on Linux

The tests run on any system, and CI runs them on Linux. To do so before pushing, from the repository root:

```bash
npm run test:linux
```

This is optional. It uses Docker, or WSL with the .NET 10 SDK and ICU (`libicu` on Ubuntu), and is skipped without either. The tests run on a copy of the source, including uncommitted changes.

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

## Romanisation data

The romanisation check reads Chinese with a table of Mandarin readings from the Unicode [Unihan database](https://www.unicode.org/charts/unihan.html), in `MapsetVerifier.Checks/Utils/Romanisation/pinyin.txt`. To update it, download and extract `Unihan.zip` from https://www.unicode.org/Public/UCD/latest/ucd/, then:

```sh
node scripts/generate-pinyin-table.mjs <path to Unihan_Readings.txt>
```

Japanese is read with the dictionary built into the Lucene.Net.Analysis.Kuromoji package. Other readings, names and the spellings of loan words come from [JMdict and JMnedict](https://www.edrdg.org/), in `MapsetVerifier.Checks/Utils/Romanisation/japanese-words.txt.br`. Their licence asks for this data to be kept up to date, so it's updated before every stable release. Download `JMdict_e.gz` and `JMnedict.xml.gz` from https://www.edrdg.org/pub/Nihongo/, then:

```sh
node scripts/generate-japanese-words.mjs <path to JMdict_e.gz> <path to JMnedict.xml.gz>
```

## Third-party notices

`THIRD-PARTY-NOTICES.txt` lists the licenses and notices of everything the installers ship, and is packaged next to `LICENSE` via `extraResources`. Regenerate it whenever a NuGet or npm dependency is added, removed or updated. It reads the sidecar's files from an unbundled publish and needs both `npm ci` installs in place:

```sh
dotnet publish src/MapsetVerifier.csproj -c Release -r win-x64 -p:PublishSingleFile=false -o <dir>
node scripts/generate-third-party-notices.mjs <dir>
```

Texts that no package provides live in `scripts/third-party-notices/`: notices for bundled components such as the IPADIC dictionary and BASS (`*.txt`), license files for packages that don't include one (`packages/`), and full texts of licenses that packages only name (`licenses/`).

---

## Technical overview (MV 2.0)

MV 2.0 is an [Electron](https://www.electronjs.org/) desktop app with a **React + TypeScript** frontend ([Vite](https://vitejs.dev/), [Mantine](https://mantine.dev/)) under `electron-app/`.

The backend is a **.NET 10** [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) sidecar (`src/MapsetVerifier.csproj`) spawned by Electron on startup. The UI talks to the backend over HTTP; real-time updates use [SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr).

Check logic lives in `MapsetVerifier.Checks/` on top of `MapsetVerifier.Framework/` and `MapsetVerifier.Parser/`. Built-in check metadata is exported for the frontend via `MapsetVerifier.Exports/`.

Packaged builds place platform-specific backend binaries in `resources/bin/server/dist/<rid>/` inside the app bundle.

---

## Technical overview (MV 1.x)

> [!WARNING]
> **This section describes the old 1.x architecture and is outdated.** MV 2.0 replaced the jQuery frontend with React/TypeScript, retargeted the backend to .NET 10, and reworked how checks and plugins are loaded. Keep this here only as historical context for the 1.x release line.

The backend was written in [C#/.NET](https://dotnet.microsoft.com/), with a simple [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) server. The frontend was an [Electron](https://www.electronjs.org/) desktop application written in HTML, CSS, and [JS/jQuery](https://jquery.com/). The server-client communication was done with [SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr). Upon selecting a mapset, the frontend requested tabs to be rendered, and the backend returned HTML.

![](https://i.imgur.com/3kHWOmH.png?sanitize=true)

Custom check plugins for 1.x used a different folder layout (`checks` instead of `CustomChecks`) and targeted .NET Core 3.1. See [CUSTOM_CHECKS.md](CUSTOM_CHECKS.md#migrating-from-mv-1x-plugins) for migration notes.
