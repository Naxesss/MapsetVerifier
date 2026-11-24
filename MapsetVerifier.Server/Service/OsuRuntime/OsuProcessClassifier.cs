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
                if (!MayBeOsuRelated(process.ProcessName))
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
        return Classify(
            process.ProcessName,
            GetExecutablePath(process),
            GetCommandLine(process),
            OperatingSystem.IsWindows(),
            () => IsLikelyLazerByModules(process)
        );
    }

    internal static OsuClientKind Classify(
        string processName,
        string? exePath,
        string? commandLine,
        bool isWindows,
        Func<bool> hasLazerModules
    )
    {
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

        if (hasLazerModules())
            return OsuClientKind.Lazer;

        var normalizedPath = Normalize(exePath ?? string.Empty);
        if (
            normalizedPath.EndsWith("/osu!.exe", StringComparison.Ordinal)
            && !normalizedPath.Contains("lazer", StringComparison.Ordinal)
        )
        {
            return OsuClientKind.Stable;
        }

        if (!isWindows && !isWine && NativeOsuBinaryRegex.IsMatch(hay))
        {
            return OsuClientKind.Lazer;
        }

        return OsuClientKind.Unknown;
    }

    internal static bool MayBeOsuRelated(string name)
    {
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

    internal static bool IsLikelyLazerByModules(Process process)
    {
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (IsLazerModule(module.ModuleName))
                    return true;
            }
        }
        catch (Exception)
        {
            // Some processes block module enumeration; treat as unknown.
        }

        return false;
    }

    internal static bool IsLazerModule(string name) =>
        name.Contains("osu.game.dll", StringComparison.OrdinalIgnoreCase)
        || name.Contains("osu.framework.dll", StringComparison.OrdinalIgnoreCase)
        || name.Contains("coreclr.dll", StringComparison.OrdinalIgnoreCase)
        || name.Contains("hostfxr.dll", StringComparison.OrdinalIgnoreCase);
}
