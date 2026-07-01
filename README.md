<p align="center">
  <img src="assets/icons/prod/256x256.png" alt="Mapset Verifier" width="128">
</p>

<h1 align="center">Mapset Verifier</h1>

<p align="center">
  <a href="https://mv.mappersguild.com/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/Naxesss/MapsetVerifier/total"></a>
  <a href="https://mv.mappersguild.com/releases"><img src="https://img.shields.io/github/v/release/Naxesss/MapsetVerifier?label=latest%20release&display_name=release" alt="Latest release"></a>
  <a href="https://github.com/Naxesss/MapsetVerifier/releases"><img src="https://img.shields.io/github/v/release/Naxesss/MapsetVerifier?include_prereleases&label=latest%20beta" alt="Latest beta"></a>
</p>
<p align="center">
  <a href="https://github.com/Naxesss/MapsetVerifier"><img src="https://img.shields.io/github/stars/Naxesss/MapsetVerifier?style=flat&color=e3b341" alt="GitHub stars"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white" alt=".NET 9"></a>
  <a href="https://www.gnu.org/licenses/gpl-3.0" alt="License: GPLv3"><img src="https://img.shields.io/badge/License-GPL%20v3-blue.svg"></a>
  <a href="https://mv.mappersguild.com/releases"><img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue" alt="Platform"></a>
</p>

<p align="center">
  <a href="https://mv.mappersguild.com"><strong>Website</strong></a>
  ·
  <a href="https://mv.mappersguild.com/releases"><strong>Download</strong></a>
  ·
  <a href="https://github.com/Naxesss/MapsetVerifier/issues"><strong>Report a bug</strong></a>
  ·
  <a href="docs/DEVELOPMENT.md"><strong>Development</strong></a>
</p>

---

Mapset Verifier (MV) is a desktop app which tests quantifyable issues in [beatmapsets](https://osu.ppy.sh/help/wiki/Beatmaps) from [osu!](https://en.wikipedia.org/wiki/Osu!), such as unsnapped objects and unused files. Many of these issues would otherwise disqualify the map from being [ranked](https://osu.ppy.sh/help/wiki/Beatmap_ranking_procedure#ranked). Although mainly aimed at [Beatmap Nominators](https://osu.ppy.sh/help/wiki/Beatmap%20Nominators), this can also be used by mapset creators and reviewers to speed up [the ranking process](https://osu.ppy.sh/help/wiki/Beatmap_ranking_procedure).

MV is a successor to the game's built-in [AiMod](https://osu.ppy.sh/help/wiki/Beatmap_Editor/AiMod) and [Sieg's Modding Assistant](https://osu.ppy.sh/community/forums/topics/359381), providing more features and greater scalability. Some examples include auto-updates, plugin support, and integrated documentation and diff utilities.

**MV 2.0** is a ground-up rewrite of the application: New React frontend, updated .NET 9 backend, a ton of new features, and built-in checks for all game modes!

*This is currently being ported to osu!lazer. [You can track its progress here!](https://github.com/ppy/osu/issues/12091#issuecomment-878760791)*

> [!WARNING]
> Some issues are not easily quantified, especially ones related to [metadata](https://osu.ppy.sh/help/wiki/Ranking_Criteria#metadata) and [timing](https://osu.ppy.sh/help/wiki/Ranking_Criteria#timing); far from everything is covered. Also, [false positives and false negatives](https://en.wikipedia.org/wiki/False_positives_and_false_negatives) may occur, as with any similar tool. So always use your own judgement and be critical about what the program points out, especially warnings.

## Download

Get the latest build from **[mv.mappersguild.com/releases](https://mv.mappersguild.com/releases)**.

The [website](https://mv.mappersguild.com) has release notes, screenshots, and platform-specific install instructions. On Windows, grab the installer (`.exe`). On Linux, use the AppImage or [the Arch AUR package](https://aur.archlinux.org/packages/mapset-verifier-bin/). On macOS, use the DMG or ZIP.

GitHub also hosts every release under [Releases](https://github.com/Naxesss/MapsetVerifier/releases).

<details>
<summary>Older versions (MV 1.x)</summary>

These builds are from the pre-2.0 release line. Prefer the current download page unless you specifically need 1.x.

| Version | Date Released | Windows | Linux |
| ---     | ---           | ---     | ---   |
| 1.8.2   | 2021-09-21    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.2/mapsetverifier-setup-1.8.2.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.2/mapsetverifier-setup-1.8.2.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.2/mapsetverifier-1.8.2.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.2/mapsetverifier-1.8.2.tar.gz) |
| 1.8.0   | 2021-09-14    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.0/mapsetverifier-setup-1.8.0.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.0/mapsetverifier-setup-1.8.0.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.0/mapsetverifier-1.8.0.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.0/mapsetverifier-1.8.0.tar.gz) |
| 1.7.2   | 2020-10-07    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.2/mapsetverifier-setup-1.7.2.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.2/mapsetverifier-setup-1.7.2.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.2/mapsetverifier-1.7.2.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.2/mapsetverifier-1.7.2.tar.gz) |
| 1.7.1   | 2020-10-03    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.1/mapsetverifier-setup-1.7.1.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.1/mapsetverifier-setup-1.7.1.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.1/mapsetverifier-1.7.1.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.1/mapsetverifier-1.7.1.tar.gz) |
| 1.7.0   | 2020-09-28    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.0/mapsetverifier-setup-1.7.0.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.0/mapsetverifier-setup-1.7.0.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.0/mapsetverifier-1.7.0.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.0/mapsetverifier-1.7.0.tar.gz) |
| 1.6.6   | 2020-08-04    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.6/mapsetverifier-setup-1.6.6.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.6/mapsetverifier-setup-1.6.6.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.6/mapsetverifier-1.6.6.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.6/mapsetverifier-1.6.6.tar.gz) |
| 1.6.5   | 2020-07-03    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.5/mapsetverifier-setup-1.6.5.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.5/mapsetverifier-setup-1.6.5.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.5/mapsetverifier-1.6.5.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.5/mapsetverifier-1.6.5.tar.gz) |

</details>

## Common Problems

- **Windows SmartScreen / "Windows protected your PC"**
  - **Fix:** Click *More info* → *Run anyway*, or right-click the installer → *Properties* → check *Unblock*.
  - **Why:** The installer is not code-signed (signing certificates are expensive).

- **App fails to start — backend executable missing (`ENOENT`)**
  - **Fix:** Uninstall, reinstall, and whitelist `MapsetVerifier.exe` in your antivirus before launching again. The backend lives under `bin/server/dist/<platform-arch>/` inside the installed app (for example `win-x64` on 64-bit Windows).
  - **Why:** Some antivirus tools flag the backend on first run and delete it before MV can use it.

- **MV 1.x plugins do not load on MV 2.0**
  - **Fix:** Rebuild plugins for .NET 9 against the current API, or remove legacy DLLs. Many former mode-specific plugins are now built into MV 2.0.
  - **Why:** MV 2.0 changed the runtime, folder layout, and check API. See [Custom checks documentation](docs/CUSTOM_CHECKS.md).

## Plugins

MV supports custom check plugins (`.dll` files) that run alongside built-in checks. Install paths, the check API, security notes, and migration guidance from MV 1.x are documented in the **[custom checks documentation](docs/CUSTOM_CHECKS.md)**.

Only install plugins from sources you trust — they run inside the backend process with the same privileges as the application.

## Development

To build from source, package releases, or learn how the stack is put together, see the **[development documentation](docs/DEVELOPMENT.md)**.

## Reporting Bugs

Open a [GitHub issue](https://github.com/Naxesss/MapsetVerifier/issues) with the beatmapset link and steps to reproduce when you can.

You can also reach the current maintainers directly on osu!:

- [Greaper](https://osu.ppy.sh/users/2369776)
- [Hivie](https://osu.ppy.sh/users/14102976)
