# Releases and prereleases

This document describes how to ship **stable** and **semver prerelease** versions of Mapset Verifier (Electron installers + updater metadata).

CI is defined in [`.github/workflows/release.yaml`](../.github/workflows/release.yaml). NPM scripts live in the **repository root** [`package.json`](../package.json).

---

## Tag rules (critical)

Workflows trigger on **git tag push**:

| Pattern in workflow | Matches examples |
| :-- | :-- |
| `v*.*.*` | `v2.0.0` (stable, three-part version after `v`) |
| `v*.*.*-*` | `v2.0.0-beta.2`, `v2.0.0-rc.1` (**hyphen before prerelease**, not extra dots) |

Use a leading **`v`**. Prefer **semver prereleases** such as **`2.0.0-beta.2`** rendered as **`v2.0.0-beta.2`**.

Avoid tags like **`v2.0.0.beta.2`** (no hyphen before the prerelease label): they typically **do not** match **`v*.*.*-*`** and CI will **not** run.

### Prerelease vs stable in CI

The workflow treats a tag as a **GitHub/Git prerelease** when the segment **before `+`** contains a hyphen (SemVer prerelease), e.g. `2.0.0-beta.2`. Pure `MAJOR.MINOR.PATCH` (e.g. `2.1.0`) is **stable**.

### Packaged app version

For release builds, **`electron-builder`** receives `--config.extraMetadata.version=<tag without leading v>` (see `TAG_VERSION` in the workflow). The **tag** determines the installer/app version—not the **`version`** field in root **`package.json`**, though keeping them aligned avoids confusion elsewhere.

---

## Before you ship (every time)

1. **`main`** (or whichever branch releases are tagged from) contains the code you intend to ship.
2. Ensure that CI is green by running the following commands:

   ```bash
   npm ci
   npm --prefix electron-app ci
   ```

3. **Sidecar binaries** (`bin/server/dist` referenced from root `package.json` `build.extraResources`) are produced as your pipeline expects—the release jobs build/consume them via the workflow.

---

## GitHub releases (recommended path)

### 1. Push a matching tag

```bash
# Push a stable tag
git checkout main
git pull
git tag v2.0.0
git push origin v2.0.0

# Push a prerelease tag
git checkout main
git pull
git tag v2.0.0-beta.2
git push origin v2.0.0-beta.2
```

### 2. Wait for CI

The workflow runs **lint/build checks**, then **four** `electron-builder` jobs (Windows, Linux, macOS Intel, macOS ARM), each with **`--publish always`**.

Environment flags used during publish:

- **`EP_DRAFT=true`** — release is created/updated as a **draft** first.
- **`EP_PRE_RELEASE`** — set when the tag is a semver prerelease (hyphen rule above).

### 3. Publish on GitHub (after artifacts exist)

**Do not** publish an empty Release from the UI **before** the workflow finishes attaching files.

After all **`Run electron-builder`** matrix jobs succeed:

1. Open **GitHub → Releases**.
2. Find the **draft** release for your tag (or refresh the page)—installers and updater **`latest*.yml`** files should appear.
3. Add release notes if needed, then **Publish release**.

### Manual run (alternative)

Use **Actions → Release Electron app → Run workflow** and supply an **existing** tag (`workflow_dispatch`). Use the same **`v…`** naming rules.

---

## Local packaging (optional)

Run from **repository root**. These **do not** publish unless you use the **`release`** scripts (`--publish always`).

| Command | Icons | Targets |
| :-- | :-- | :-- |
| `npm run dist` | `prod` (default in `package.json`) | Windows x64 |
| `npm run dist-all` | `prod` | Windows + Linux (`-wl`) |
| `npm run dist-prerelease` | `prerelease` | Windows x64 |
| `npm run dist-all-prerelease` | `prerelease` | Windows + Linux |

Publishing from your machine (rare):

| Command | Effect |
| :-- | :-- |
| `npm run release` | Build + **`--publish always`** with **prod** icon config |
| `npm run release-prerelease` | Same + **prerelease** icon overrides |

Local Windows builds may hit **code-sign cache extraction** symlink errors (`winCodeSign` / 7‑Zip). Mitigations:

- Enable **Developer Mode** (Windows), or run the terminal **as Administrator**, or  
- For **unsigned** local tries: `set CSC_IDENTITY_AUTO_DISCOVERY=false` (PowerShell: `$env:CSC_IDENTITY_AUTO_DISCOVERY="false"`) before `npm run dist…`.

---

## Fixing a bad tag or failed release

- **`npm ci` failed in CI** — fix lockfiles on the branch, push, then **either** use a **new tag** (e.g. bump prerelease suffix) **or** delete the remote tag and recreate it on the good commit (**only** if nobody should rely on the old tag SHA).
- **Workflow never ran** — confirm tag matches **`v*.*.*`** or **`v*.*.*-*`** and was **pushed to the correct remote**.
- **CI green but Release has no files** — check **Draft** releases; verify all matrix **`electron-builder`** jobs completed; avoid publishing a GitHub Release before uploads finish.

---

## Quick reference

| Goal | Typical action |
| :-- | :-- |
| Stable **v2.1.0** | Tag **`v2.1.0`**, push, wait for CI, publish **draft**. |
| Prerelease **v2.0.0-beta.2** | Tag **`v2.0.0-beta.2`**, push, wait for CI, publish **draft** (marked prerelease by automation). |
| Local installer, no CI | **`npm run dist`** or **`npm run dist-prerelease`** (see table above). |
