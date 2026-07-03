# Custom checks

Mapset Verifier can load **external check plugins** at startup. A plugin is a `.dll` file that defines one or more check classes. Custom checks run alongside the built-in checks and appear in the same UI (issues list, documentation, and so on).

This guide covers how to **install** existing plugins and how to **create** new ones for MV 2.0.

> [!CAUTION]
> **ONLY INSTALL PLUGINS FROM SOURCES YOU TRUST!!!!!!!!**
>
> Custom check DLLs are loaded and executed **inside the Mapset Verifier backend process** with the same privileges as the application itself. A malicious plugin can:
>
> - Read, modify, or delete files on your computer
> - Run arbitrary code
> - Access beatmap folders, app data, logs, and anything else the backend can reach
>
> There is **no sandbox**. MV does not verify plugin authors, sign plugins, or restrict what they can do once loaded.
>
> **DO NOT** drop random DLLs from Discord links, forum posts, or unknown GitHub repos into your `CustomChecks` folder. Prefer plugins you built yourself from source, or from maintainers and repositories you already trust. When in doubt, **leave the folder empty**.

> [!WARNING]
> **Plugins built for MV 1.x (pre-2.0) will not work on MV 2.0.**
>
> Old plugin DLLs are **not compatible** with the current app. MV 2.0 targets **.NET 9** (1.x used .NET Core 3.1), references renamed assemblies (`MapsetVerifier.Framework`, `MapsetVerifier.Parser`), and loads plugins from **`CustomChecks`** instead of the old `checks` folder.
>
> Dropping legacy DLLs into the folder will not load your checks — they must be **rebuilt from source** against this repository. Many former plugins (osu!taiko, osu!catch, osu!mania checks) are now **built into MV 2.0** and no longer need to be installed separately.

---

## Quick start: loading a plugin

1. Build or obtain a check plugin `.dll` targeting **.NET 9** and built against the current `MapsetVerifier.Framework` API.
2. Copy **only the plugin `.dll`** into the custom checks folder (see paths below). Do not copy `MapsetVerifier.Framework.dll`, `MapsetVerifier.Parser.dll`, or other assemblies that MV already ships — duplicate copies can prevent checks from loading correctly.
3. Open the plugin manager and use **Reload checks**, or restart Mapset Verifier.
4. Open a beatmapset and verify your check appears in the issues/documentation UI.

### Custom checks folder

The backend resolves the folder at startup in [`src/Program.cs`](../src/Program.cs) and loads DLLs from it in [`MapsetVerifier.Framework/Checker.cs`](../MapsetVerifier.Framework/Checker.cs).

| Platform | Base app-data folder | Full custom-checks path |
| :-- | :-- | :-- |
| Windows | `%APPDATA%` | `%APPDATA%\Mapset Verifier Externals\CustomChecks\` |
| Linux | `$XDG_DATA_HOME` or `~/.local/share` | `~/.local/share/Mapset Verifier Externals/CustomChecks/` |
| macOS | `~/Library/Application Support` | `~/Library/Application Support/Mapset Verifier Externals/CustomChecks/` |

The folder is created automatically if it does not exist and MV has permission to write there.

>[!NOTE]
> Older MV 1.x used a `checks` subfolder. MV 2.0 uses **`CustomChecks`**.

### Verifying that a plugin loaded

Check the backend log after startup. Logs are written under:

`{app-data}\Mapset Verifier Externals\Logs\`

Look for lines such as:

- `Custom checks directory: ...`
- `Loaded checks from assembly YourPlugin. Total registered: ...`
- `Failed to load checks from ...` (on error)

If no DLLs are present, you will see `No custom checks found`.

---

## How loading works

At startup the backend:

1. Loads all built-in checks from `MapsetVerifier.Checks` via `Checker.LoadDefaultChecks()`.
2. Scans the `CustomChecks` folder for `*.dll` files.
3. For each DLL, calls `Assembly.LoadFrom` and inspects **exported types**.
4. Instantiates every type decorated with `[Check]` that inherits from `Check`.
5. Registers each instance in `CheckerRegistry`.

Relevant rules from the loader:

- Only **public exported types** are considered.
- The type must have a **parameterless constructor** (same as built-in checks).
- Types **without** `[Check]` are ignored.
- If a check type is **already registered** (same CLR type as a built-in or another plugin), the duplicate is skipped.
- Failures loading one DLL do not stop other DLLs from loading; errors are logged per file.

Custom checks are reloaded when you use **Reload checks** in the plugin manager, or when MV starts.
You can also disable custom checks from the plugin manager. When disabled, MV starts with built-in checks only and reloads omit DLLs from `CustomChecks`.

---

## Creating a plugin project

> [!NOTE]
> **Consider contributing your check to Mapset Verifier instead of shipping a plugin!**
>
> If you have written a check that could benefit mappers and nominators broadly, you can open a pull request to add it to [`MapsetVerifier.Checks`](../MapsetVerifier.Checks/) in this repository. Maintainers will review it for inclusion based on **usefulness** — we want MV to stay focused and avoid cluttering the app with niche or overly minor checks that don't provide much impact.

There is no published NuGet package for the check API. Reference the projects from this repository directly.

### 1. Create a class library

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Adjust paths if your plugin repo is not next to MapsetVerifier -->
    <ProjectReference Include="..\MapsetVerifier\MapsetVerifier.Framework\MapsetVerifier.Framework.csproj">
      <!-- Do not copy shared MV assemblies into the plugin output -->
      <Private>false</Private>
    </ProjectReference>
    <ProjectReference Include="..\MapsetVerifier\MapsetVerifier.Parser\MapsetVerifier.Parser.csproj">
      <Private>false</Private>
    </ProjectReference>
  </ItemGroup>
</Project>
```

Use the same **.NET 9** target as the main application. Plugins built for MV 1.x (.NET Core 3.1) must be **rebuilt** for MV 2.0.

### 2. Implement one or more check classes

Every check must:

1. Be a **public class** with `[Check]` ([`CheckAttribute`](../MapsetVerifier.Framework/Objects/Attributes/CheckAttribute.cs)).
2. Inherit from one of the three check base types (below).
3. Implement `GetMetadata()`, `GetTemplates()`, and `GetIssues(...)`.

See [built-in checks](../MapsetVerifier.Checks/) for many real examples.

### 3. Build and deploy

```bash
dotnet build -c Release
```

Copy the resulting plugin `.dll` from `bin/Release/net9.0/` into the `CustomChecks` folder. Use **Reload checks** in the plugin manager, or restart MV.

---

## Check types

Choose the base class that matches **what data your check inspects**:

| Base class | Scope | `GetIssues` receives | Typical use |
| :-- | :-- | :-- | :-- |
| [`GeneralCheck`](../MapsetVerifier.Framework/Objects/GeneralCheck.cs) | Whole beatmapset, not tied to one difficulty | `BeatmapSet` | Files, audio, metadata, resources |
| [`BeatmapCheck`](../MapsetVerifier.Framework/Objects/BeatmapCheck.cs) | Single `.osu` difficulty | `Beatmap` | Timing, compose, spread, mode-specific object rules |
| [`BeatmapSetCheck`](../MapsetVerifier.Framework/Objects/BeatmapSetCheck.cs) | Whole set, may compare difficulties | `BeatmapSet` | Spread rules, cross-diff consistency |

The backend runs checks in parallel. `BeatmapCheck` instances are filtered by **game mode** using [`BeatmapCheckMetadata.Modes`](../MapsetVerifier.Framework/Objects/Metadata/BeatmapCheckMetadata.cs) before `GetIssues` is called.

---

## Check API

### `[Check]`

Marks a class for automatic discovery and registration. Without this attribute, the loader ignores the type.

### `GetMetadata()`

Returns a [`CheckMetadata`](../MapsetVerifier.Framework/Objects/Metadata/CheckMetadata.cs) (or [`BeatmapCheckMetadata`](../MapsetVerifier.Framework/Objects/Metadata/BeatmapCheckMetadata.cs) for beatmap-level checks).

| Property | Purpose |
| :-- | :-- |
| `Category` | Groups checks in the UI (e.g. `"Timing"`, `"Files"`). |
| `Message` | Short summary shown when the check passes or fails. The UI may prefix **"No"** when there are no issues, so wording should read naturally with that prefix. |
| `Author` | Plugin or check author name. |
| `Documentation` | Title → markdown body pairs shown in the in-app documentation tab. Supports markdown. |

For `BeatmapCheck` / `BeatmapSetCheck`, use `BeatmapCheckMetadata` and set:

- **`Modes`** — which rulesets to run on (`Standard`, `Taiko`, `Catch`, `Mania`). Defaults to all modes. Leave empty to run on all modes.
- **`Difficulties`** — which difficulty tiers issues apply to (`Easy` … `Ultra`). Defaults to all. Leave empty to run on all difficulties.

### `GetTemplates()`

Returns a dictionary of named [`IssueTemplate`](../MapsetVerifier.Framework/Objects/IssueTemplate.cs) instances. Each template defines:

- **Level** — severity (see below).
- **Format string** — message with `{0}`, `{1}`, … placeholders.
- **Default argument labels** — one label per placeholder (used in documentation).
- **Cause** (optional) — extra explanation via `.WithCause("...")`.

Placeholder count must match the number of default-argument labels exactly.

### `GetIssues(...)`

Yield one or more [`Issue`](../MapsetVerifier.Framework/Objects/Issue.cs) objects. Construct issues with:

```csharp
new Issue(GetTemplate("TemplateName"), beatmap, arg0, arg1, ...)
```

- Pass **`null`** for `beatmap` when the issue is not tied to a specific difficulty (common for `GeneralCheck`).
- Pass the **`Beatmap`** instance for per-difficulty issues (`BeatmapCheck`).
- Use helpers such as `ForDifficulties(...)` or `WithInterpretation(...)` when an issue should only apply under certain difficulty-interpretation settings.

The framework attaches the originating check automatically via `WithOrigin` after your method returns.

### Issue levels

From lowest to highest priority in the results list:

| Level | Typical meaning |
| :-- | :-- |
| `Info` | Informational, often ignorable |
| `Check` | Manual verification suggested |
| `Error` | Check could not complete (e.g. I/O failure) |
| `Minor` | Small issue |
| `Warning` | Should be fixed before ranking, guideline-breaking |
| `Problem` | Disqualifying / must fix, rule-breaking |

---

## Minimal example

A beatmap-level check that warns when a difficulty has no hit objects:

```csharp
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MyMvPlugin;

[Check]
public class CheckEmptyBeatmap : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Difficulty contains no hit objects.",
            Author = "YourName",
            Modes = [Beatmap.Mode.Standard],
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Empty",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} has no hit objects.",
                    "beatmap"
                ).WithCause("The difficulty file parses successfully but contains zero objects.")
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        if (beatmap.HitObjects.Count == 0)
            yield return new Issue(GetTemplate("Empty"), beatmap, beatmap.ToString());
    }
}
```

---

## Working with beatmap data

Custom checks use the same parser types as built-in checks ([`MapsetVerifier.Parser`](../MapsetVerifier.Parser/)). Common entry points:

- **`BeatmapSet`** — song folder path, file lists, all beatmaps, storyboard, shared resources.
- **`Beatmap`** — hit objects, timing lines, difficulty settings, mode, star rating helpers.
- **`PathStatic`**, **`Timestamp`**, and other helpers under `MapsetVerifier.Parser.Statics`.

Inspect existing checks under [`MapsetVerifier.Checks/`](../MapsetVerifier.Checks/) for patterns (metadata checks, audio via TagLib, mode-specific extension methods, and so on).

Optional shared utilities exist in [`MapsetVerifier.Checks/Common.cs`](../MapsetVerifier.Checks/Common.cs), but that project is **not** part of the public plugin contract. Prefer copying small helpers into your plugin or referencing only `MapsetVerifier.Framework` / `MapsetVerifier.Parser`.

---

## Documentation and metadata export

Built-in check metadata is exported for the frontend by [`MapsetVerifier.Exports`](../MapsetVerifier.Exports/Program.cs). Custom checks loaded at runtime are included automatically in the running app’s check registry and documentation endpoints — no extra export step is required for normal plugin use.

If you maintain a standalone plugin repository, keep `GetMetadata()` and `GetTemplates()` complete so the in-app documentation tab renders correctly.

---

## Troubleshooting

| Symptom | Likely cause |
| :-- | :-- |
| Plugin never appears | Wrong folder (`CustomChecks` vs old `checks` path), or checks were not reloaded |
| `Failed to load checks from ...` | Wrong .NET version, missing dependency, or invalid assembly |
| Check loads but never reports issues | Wrong base class scope, or `Modes` / `Difficulties` filter excludes the target beatmap |
| `Failed to instantiate check type ...` | No public parameterless constructor, or constructor throws |
| Types from plugin not recognized at runtime | Plugin output included its own copy of `MapsetVerifier.Framework.dll` or `MapsetVerifier.Parser.dll` — deploy **only** your plugin DLL |
| Duplicate check ignored | Same check type already registered from built-in checks or another plugin |

Enable detailed loader output in the log file under `Mapset Verifier Externals\Logs\`.

---

## Security

> [!WARNING]
> **Treat every custom check DLL like installing untrusted software.**
>
> Plugins use `Assembly.LoadFrom` and run with **full backend access**. MV cannot protect you from a bad plugin — your only defense is **who you choose to trust**.
>
> Before installing a third-party plugin:
>
> - Read the source code (or have someone you trust review it)
> - Build the DLL yourself from that source instead of downloading a prebuilt binary
> - Be skeptical of plugins shared in DMs, random threads, or repos with no history
>
> If you would not run an `.exe` from the same place, **do not** put its `.dll` in `CustomChecks`.

---

## Migrating from MV 1.x plugins

MV 2.0 changed several things that affect older plugins:

| MV 1.x | MV 2.0 |
| :-- | :-- |
| .NET Core 3.1 | .NET 9 |
| `%APPDATA%\...\checks` | `%APPDATA%\...\CustomChecks` |
| `MapsetVerifierFramework.dll` / `MapsetParser.dll` | `MapsetVerifier.Framework.dll` / `MapsetVerifier.Parser.dll` |
| osu!taiko / osu!catch / osu!mania checks as plugins | Built into [`MapsetVerifier.Checks`](../MapsetVerifier.Checks/) |

Repoint project references to this repository, retarget `net9.0`, update namespaces and API usages as needed, rebuild, and deploy to `CustomChecks`.
