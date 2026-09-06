using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MapsetVerifier.Server.Service.OsuRuntime;

public readonly record struct RunningOsuClients(bool Stable, bool Lazer);

public static class OsuProcessClassifier
{
    private static readonly Regex NativeOsuBinaryRegex = new(
        @"(^|/)osu!(?:\s|$)",
        RegexOptions.Compiled
    );

    public static RunningOsuClients GetRunningClients()
    {
        var stable = false;
        var lazer = false;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!MayBeOsuRelated(process))
                    continue;

                switch (Classify(process))
                {
                    case OsuClientKind.Stable:
                        stable = true;
                        break;
                    case OsuClientKind.Lazer:
                        lazer = true;
                        break;
                }
            }
            catch (Exception)
            {
                // Ignore inaccessible process entries.
            }
            finally
            {
                process.Dispose();
            }
        }

        return new RunningOsuClients(stable, lazer);
    }

    public static OsuClientKind Classify(Process process)
    {
        var processName = process.ProcessName ?? string.Empty;
        var exePath = GetExecutablePath(process);
        var commandLine = GetCommandLine(process);
        var hay = Normalize($"{processName} {exePath} {commandLine}");

        if (!hay.Contains("osu", StringComparison.Ordinal))
            return OsuClientKind.Unknown;
        if (hay.Contains("mapsetverifier", StringComparison.Ordinal))
            return OsuClientKind.Unknown;

        var isWine =
            hay.Contains("wine", StringComparison.Ordinal)
            || hay.Contains("yawl", StringComparison.Ordinal)
            || hay.Contains("wineskin", StringComparison.Ordinal)
            || hay.Contains("osu-wine", StringComparison.Ordinal)
            || hay.Contains("pressure-vessel", StringComparison.Ordinal);

        if (
            hay.Contains("osulazer", StringComparison.Ordinal)
            || hay.Contains("osu!lazer", StringComparison.Ordinal)
            || hay.Contains("osu!.lazer", StringComparison.Ordinal)
            || hay.Contains("/lazer/", StringComparison.Ordinal)
            || hay.Contains("osu.appimage", StringComparison.Ordinal)
            || hay.Contains("osu-lazer", StringComparison.Ordinal)
            || hay.Contains(".mount_osu", StringComparison.Ordinal)
            || hay.Contains("sh.ppy.osu", StringComparison.Ordinal)
            || hay.Contains("osu!.app", StringComparison.Ordinal)
        )
        {
            return OsuClientKind.Lazer;
        }

        if (isWine)
            return OsuClientKind.Stable;

        if (IsLikelyLazerByModules(process))
            return OsuClientKind.Lazer;

        var normalizedPath = Normalize(exePath ?? string.Empty);
        if (
            normalizedPath.EndsWith("/osu!.exe", StringComparison.Ordinal)
            && !normalizedPath.Contains("lazer", StringComparison.Ordinal)
        )
        {
            return OsuClientKind.Stable;
        }

        if (!OperatingSystem.IsWindows() && !isWine && NativeOsuBinaryRegex.IsMatch(hay))
        {
            return OsuClientKind.Lazer;
        }

        return OsuClientKind.Unknown;
    }

    private static bool MayBeOsuRelated(Process process)
    {
        var name = process.ProcessName ?? string.Empty;
        return name.Contains("osu", StringComparison.OrdinalIgnoreCase)
            || name.Contains("wine", StringComparison.OrdinalIgnoreCase)
            || name.Contains("yawl", StringComparison.OrdinalIgnoreCase)
            || name.Contains("apprun", StringComparison.OrdinalIgnoreCase)
            || name.Contains("flatpak", StringComparison.OrdinalIgnoreCase)
            || name.Contains("bwrap", StringComparison.OrdinalIgnoreCase)
            || name.Contains("pressure", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value) => value.Replace('\\', '/').ToLowerInvariant();

    private static string? GetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? GetCommandLine(Process process)
    {
        if (!OperatingSystem.IsLinux())
            return null;

        try
        {
            var cmdlinePath = $"/proc/{process.Id}/cmdline";
            if (!File.Exists(cmdlinePath))
                return null;
            return File.ReadAllText(cmdlinePath).Replace('\0', ' ');
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsLikelyLazerByModules(Process process)
    {
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                var moduleName = module.ModuleName?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(moduleName))
                    continue;

                if (
                    moduleName.Contains("osu.game.dll", StringComparison.Ordinal)
                    || moduleName.Contains("osu.framework.dll", StringComparison.Ordinal)
                    || moduleName.Contains("coreclr.dll", StringComparison.Ordinal)
                    || moduleName.Contains("hostfxr.dll", StringComparison.Ordinal)
                )
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // Some processes block module enumeration; treat as unknown.
        }

        return false;
    }
}
