using System.Diagnostics;

namespace MapsetVerifier.Server.Service.OsuRuntime;

public static class OsuProcessClassifier
{
    public static OsuClientKind Classify(Process process)
    {
        var processName = process.ProcessName;
        if (!processName.Contains("osu", StringComparison.OrdinalIgnoreCase))
            return OsuClientKind.Unknown;

        var exePath = GetExecutablePath(process)?.Replace('/', '\\').ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(exePath))
        {
            if (exePath.Contains("\\osu!.lazer\\", StringComparison.Ordinal) || exePath.Contains("\\lazer\\", StringComparison.Ordinal))
                return OsuClientKind.Lazer;

            if (exePath.Contains("\\appdata\\local\\osu!\\osu!.exe", StringComparison.Ordinal) || exePath.EndsWith("\\osu!\\osu!.exe", StringComparison.Ordinal))
                return OsuClientKind.Stable;

            if (exePath.EndsWith("\\osu!.exe", StringComparison.Ordinal) && !exePath.Contains("lazer", StringComparison.Ordinal))
                return OsuClientKind.Stable;
        }

        if (IsLikelyLazerByModules(process))
            return OsuClientKind.Lazer;

        return OsuClientKind.Unknown;
    }

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

    private static bool IsLikelyLazerByModules(Process process)
    {
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                var moduleName = module.ModuleName?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(moduleName))
                    continue;

                if (moduleName.Contains("osu.game.dll", StringComparison.Ordinal)
                    || moduleName.Contains("osu.framework.dll", StringComparison.Ordinal)
                    || moduleName.Contains("coreclr.dll", StringComparison.Ordinal)
                    || moduleName.Contains("hostfxr.dll", StringComparison.Ordinal))
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
