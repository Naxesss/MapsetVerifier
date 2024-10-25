![Mapset Verifier](https://i.imgur.com/O4d2jTa.png)

Mapset Verifier (MV) is a desktop app which tests quantifyable issues in [beatmapsets](https://osu.ppy.sh/help/wiki/Beatmaps) from [osu!](https://en.wikipedia.org/wiki/Osu!), such as unsnapped objects and unused files. Many of these issues would otherwise disqualify the map from being [ranked](https://osu.ppy.sh/help/wiki/Beatmap_ranking_procedure#ranked). Although mainly aimed at [Beatmap Nominators](https://osu.ppy.sh/help/wiki/Beatmap%20Nominators), this can also be used by mapset creators and reviewers to speed up [the ranking process](https://osu.ppy.sh/help/wiki/Beatmap_ranking_procedure).

MV is a successor to the game's built-in [AiMod](https://osu.ppy.sh/help/wiki/Beatmap_Editor/AiMod) and [Sieg's Modding Assistant](https://osu.ppy.sh/community/forums/topics/359381), providing more features and greater scalability. Some examples include auto-updates, plugin support, and integrated documentation and diff utilities.

*This is currently being ported to osu!lazer. [You can track its progress here!](https://github.com/ppy/osu/issues/12091#issuecomment-878760791)*

### Note
Some issues are not easily quantified, especially ones related to [metadata](https://osu.ppy.sh/help/wiki/Ranking_Criteria#metadata) and [timing](https://osu.ppy.sh/help/wiki/Ranking_Criteria#timing); far from everything is covered. Also, [false positives and false negatives](https://en.wikipedia.org/wiki/False_positives_and_false_negatives) may occur, as with any similar tool. So always use your own judgement and be critical about what the program points out, especially warnings.

##
![](https://i.imgur.com/F6HhxPU.gif?sanitize=true)

## Download

See [the latest release](https://github.com/Naxesss/MapsetVerifier/releases/latest). On Windows, get the exe file. On Linux, get the tar.gz / [use this package on archlinux](https://aur.archlinux.org/packages/mapset-verifier-bin/).

| Version | Date Released | Windows | Linux |
| ---     | ---           | ---     | ---   |
| 1.8.2   | 2021-09-21    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.2/mapsetverifier-setup-1.8.2.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.2/mapsetverifier-setup-1.8.2.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.2/mapsetverifier-1.8.2.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.2/mapsetverifier-1.8.2.tar.gz) |
| 1.8.0   | 2021-09-14    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.0/mapsetverifier-setup-1.8.0.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.0/mapsetverifier-setup-1.8.0.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.8.0/mapsetverifier-1.8.0.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.8.0/mapsetverifier-1.8.0.tar.gz) |
| 1.7.2   | 2020-10-07    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.2/mapsetverifier-setup-1.7.2.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.2/mapsetverifier-setup-1.7.2.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.2/mapsetverifier-1.7.2.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.2/mapsetverifier-1.7.2.tar.gz) |
| 1.7.1   | 2020-10-03    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.1/mapsetverifier-setup-1.7.1.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.1/mapsetverifier-setup-1.7.1.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.1/mapsetverifier-1.7.1.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.1/mapsetverifier-1.7.1.tar.gz) |
| 1.7.0   | 2020-09-28    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.0/mapsetverifier-setup-1.7.0.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.0/mapsetverifier-setup-1.7.0.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.7.0/mapsetverifier-1.7.0.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.7.0/mapsetverifier-1.7.0.tar.gz) |
| 1.6.6   | 2020-08-04    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.6/mapsetverifier-setup-1.6.6.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.6/mapsetverifier-setup-1.6.6.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.6/mapsetverifier-1.6.6.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.6/mapsetverifier-1.6.6.tar.gz) |
| 1.6.5   | 2020-07-03    | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.5/mapsetverifier-setup-1.6.5.exe.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.5/mapsetverifier-setup-1.6.5.exe) | [![](https://img.shields.io/github/downloads/naxesss/mapsetverifier/v1.6.5/mapsetverifier-1.6.5.tar.gz.svg)](https://github.com/Naxesss/MapsetVerifier/releases/download/v1.6.5/mapsetverifier-1.6.5.tar.gz) |

## Common Problems

- **Windows protected pc**
    - Solution: More info > Run anyway *OR* Right click installer > Properties > Unblock
    - Reason: Installer has no signed certificate (costs)

- **Stuck on install screen when reinstalling**
    - Solution: Uninstall before reinstalling
    - Reason: Limitation in electron-builder

- **Javascript error ENOENT, no MapsetVerifierBackend.exe in `/app/api/win-x86`**
    - Solution: Uninstall, reinstall, then whitelist this exe in your antivirus before running again
    - Reason: Antivirus false positive, deletes the backend (part that does checks) on first run

## Components

Mapset Verifier is comprised of the following open source .NET Core components:

| Component | Description |
| --- | --- |
| [MapsetParser](https://github.com/Naxesss/MapsetParser) | Parses .osu and .osb files in the given mapset folder |
| [MapsetChecks](https://github.com/Naxesss/MapsetChecks) | The default plugin for MV. Includes all-mode and standard checks |
| [MapsetSnapshotter](https://github.com/Naxesss/MapsetSnapshotter) | A diff utility. Provides a summary of changes within a mapset since a snapshot |
| [MapsetVerifierFramework](https://github.com/Naxesss/MapsetVerifierFramework) | A plugin framework for loading and running check DLLs |
| [MapsetVerifierBackend](https://github.com/Naxesss/MapsetVerifierBackend) | Handles rendering and communication between the framework and Electron client |

The remaining HTML/CSS/JS files are available in the application, see `/Mapset Verifier/resources/app`.

## Feature Comparison

| Feature | Mapset Verifier | Modding Assistant | AiMod |
| --- | --- | --- | --- |
| Difficulty Interpretation | ✅ (Option, Name, SR) | ✅ (Name, SR) | ✅ (SR) |
| Snapshots | ✅ (Automatic) | ✅ (Manual) | ⬜️ |
| Integrated Documentation | ✅ | ✅ (RC Snippets) | ⬜️ |
| Timeline Comparison | ✅ | ✅ (Taiko-only) | ⬜️ |
| Song folder detection | ✅ | ⬜️ | ✅ |
| Automatic Updates | ✅ (Windows-only) | ⬜️ | ✅ |
| Verbose Mode | ✅ | ⬜️ | ✅ |
| Plugin Support | ✅ | ⬜️ | ⬜️ |
| Open Source | ✅ | ⬜️ | ⬜️ |
| Difficulty Graph | ✅ | ✅ (Outdated SR) | ⬜️ |

## Examples of fixed false positives/negatives
### Modding Assistant
- counting extended break times as drain time.
- failing to account for minimum SV (0.1x).
- misinterpreting hit sounds on slider bodies as hit sounds on heads/tails.
- completely ignoring storyboard variables and animation frames.

### AiMod
- incorrectly detecting unsnaps on slider tails < 2 ms off
- not accounting for stacking.
- using a vastly outdated star rating system, saying you need an easy/normal when you already have one.
- using inaccurate playfield measurements to detect offscreen hit objects.

## Plugins

Add check DLLs to `%APPDATA%/Mapset Verifier Externals/checks` to have them load in just like the default checks. To create these DLLs, have a look at how [MapsetChecks](https://github.com/Naxesss/MapsetChecks) was created. [This](https://github.com/rorre/MV-SliderOnly#building) is probably also useful.

Third-party plugins (that I'm aware of):
- [CatchCheck](https://github.com/rorre/CatchCheck) by -Keitaro, adds osu!catch-specific checks.
- [MapsetChecksCatch](https://github.com/Darius-Wattimena/MapsetChecksCatch) by Greaper, adds osu!catch-specific checks.

In general, do be careful about which check plugins you use, as they can be malicious. The plugins are executed by the back end of the application, so they can pretty much do anything the rest of the application can.

## Checks (last updated 2021-09-14)
### General

| Category | Issue Message |
| --- | --- |
| **Resources** | Missing background. |
| **Resources** | Too high or low background resolution. |
| **Resources** | Inconsistent video usage. |
| **Resources** | Overlay layer usage. |
| **Resources** | Too high sprite resolution. |
| **Resources** | Inconsistent video offset. |
| **Resources** | Too high video resolution. |
| **Metadata** | Inconsistent metadata. |
| **Metadata** | Incorrect marker format. |
| **Metadata** | Incorrect marker spacing. |
| **Metadata** | BMS used as source. |
| **Metadata** | Incorrect format of (TV Size) / (Game Ver.) / (Short Ver.) / (Cut Ver.) / (Sped Up Ver.) in title. |
| **Metadata** | Unicode in romanized fields. |
| **Metadata** | Missing genre/language in tags. |
| **Files** | Issues with updating or downloading. |
| **Files** | 0-byte files. |
| **Files** | Unused files. |
| **Audio** | Audio channels in video. |
| **Audio** | More than 20% unused audio at the end. |
| **Audio** | Too high or low audio bitrate. |
| **Audio** | Frequent finish hit sounds. |
| **Audio** | Delayed hit sounds. |
| **Audio** | Incorrect hit sound format. |
| **Audio** | Imbalanced hit sounds. |
| **Audio** | Too short hit sounds. |
| **Audio** | Multiple or missing audio files.  |
| **Audio** | Incorrect audio format. |

### All Modes

| Category | Issue Message |
| --- | --- |
| **Timing** | Concurrent or conflicting timing lines. |
| **Timing** | First line toggles kiai or is inherited. |
| **Timing** | Inconsistent uninherited lines, meter signatures or BPM. |
| **Timing** | Unsnapped kiai. |
| **Timing** | Inconsistent or unset preview time. |
| **Timing** | Unsnapped hit objects. |
| **Timing** | Unused timing lines. |
| **Timing** | Wrongly or inconsistently snapped hit objects. |
| **Spread** | Lowest difficulty too difficult for the given drain/play time(s). |
| **Settings** | Abnormal difficulty settings. |
| **Settings** | Inconsistent mapset id, countdown, epilepsy warning, etc. |
| **Settings** | Slider tick rates not aligning with any common beat snap divisor. |
| **Settings** | Abnormal difficulty settings. |
| **Hit Sounds** | Low volume hit sounding. |
| **Events** | Breaks only achievable through .osu editing. |
| **Compose** | Abnormal amount of slider nodes. |
| **Compose** | Concurrent hit objects. |
| **Compose** | Too short drain time. |
| **Compose** | Invisible sliders. |
| **Compose** | Abnormal amount of slider nodes. |

### Standard

| Category | Issue Message |
| --- | --- |
| **Timing** | Hit object is unaffected by a line very close to it. |
| **Spread** | Multiple reverses on too short sliders. |
| **Spread** | Too short sliders. |
| **Spread** | Object too close or far away from previous. |
| **Spread** | Too short spinner time or spinner recovery time. |
| **Spread** | Perfect stacks too close in time. |
| **Spread** | Objects close in time not overlapping. |
| **Settings** | Default combo colours without forced skin. |
| **Settings** | Too dark or bright combo colours or slider borders. |
| **Hit Sounds** | Long periods without hit sounding. |
| **Events** | Storyboarded hit sounds. |
| **Compose** | Burai slider. |
| **Compose** | Too short spinner. |
| **Compose** | Obscured reverse arrows. |
| **Compose** | Offscreen hit objects. |
| **Compose** | Abnormally large spacing. |
| **Compose** | Ambiguous slider intersection. |

### Taiko

| Category | Issue Message |
| --- | --- |
| **Timing** | Inconsistent omitted bar lines. |
| **Timing** | Hit object is unaffected by a line very close to it. |
| **Events** | Storyboarded hit sounds. |

### Catch

| Category | Issue Message |
| --- | --- |
| **Timing** | Hit object is unaffected by a line very close to it. |
| **Settings** | Default combo colours without forced skin. |
| **Settings** | Too dark or bright combo colours or slider borders. |
| **Hit Sounds** | Long periods without hit sounding. |
| **Events** | Storyboarded hit sounds. |
| **Compose** | Spinner gap too small. |

Modes apart from standard do not have their SRs implemented, so difficulty levels will likely be wrong.

## Reporting Bugs

Either post an issue here or contact me on Discord @naxess. Give beatmapset and reproduction steps if applicable. If you don't have me on discord just join the [Aiess Project](https://discord.gg/2XV5dcW), after which you should be able to message me.
