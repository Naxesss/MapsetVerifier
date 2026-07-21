using MapsetVerifier.Server.Model;
using Serilog;

namespace MapsetVerifier.Server.Service.OsuRuntime;

/// <summary>
/// Copies a lazer beatmapset's content-addressed files into a real temp folder with their real
/// filenames, so the existing folder-based <see cref="MapsetVerifier.Parser.Objects.BeatmapSet"/>
/// parser (and every check/tab built on top of it) can read it completely unmodified.
/// </summary>
public static class LazerBeatmapMaterializer
{
    private static readonly string TempRoot = Path.Combine(
        Path.GetTempPath(),
        "MapsetVerifier",
        "LazerMaterialized"
    );

    /// <summary>
    /// Clears any beatmapsets materialized by a previous server run. Safe to call even if the
    /// folder doesn't exist or is partially locked.
    /// </summary>
    public static void SweepOnStartup()
    {
        try
        {
            if (Directory.Exists(TempRoot))
                Directory.Delete(TempRoot, recursive: true);
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Failed to sweep lazer materialization temp folder {TempRoot}",
                TempRoot
            );
        }

        try
        {
            Directory.CreateDirectory(TempRoot);
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Failed to recreate lazer materialization temp folder {TempRoot}",
                TempRoot
            );
        }
    }

    public static ApiLazerMaterializeResult Materialize(string dataDirectory, string beatmapSetId)
    {
        if (!Guid.TryParse(beatmapSetId, out _))
            return ApiLazerMaterializeResult.Error("Invalid beatmapset id.");

        var files = LazerRealmService.GetBeatmapSetFiles(dataDirectory, beatmapSetId);
        if (files == null)
            return ApiLazerMaterializeResult.Error(
                "Beatmapset could not be found in the lazer library."
            );

        var targetDir = Path.Combine(TempRoot, beatmapSetId);

        try
        {
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, recursive: true);
            Directory.CreateDirectory(targetDir);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Failed to prepare materialization folder for {BeatmapSetId}",
                beatmapSetId
            );
            return ApiLazerMaterializeResult.Error(
                "Failed to prepare a temp folder for the beatmapset."
            );
        }

        var copiedAny = false;
        foreach (var (filename, hash) in files)
        {
            try
            {
                var sourcePath = LazerRealmService.ResolveFilePathFromHash(dataDirectory, hash);
                if (!File.Exists(sourcePath))
                {
                    Log.Warning(
                        "Lazer file blob missing for {Filename} (hash {Hash}) in set {BeatmapSetId}",
                        filename,
                        hash,
                        beatmapSetId
                    );
                    continue;
                }

                var destPath = Path.Combine(targetDir, filename);
                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrWhiteSpace(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(sourcePath, destPath, overwrite: true);
                copiedAny = true;
            }
            catch (Exception ex)
            {
                Log.Warning(
                    ex,
                    "Failed to materialize file {Filename} for lazer beatmapset {BeatmapSetId}",
                    filename,
                    beatmapSetId
                );
            }
        }

        if (!copiedAny)
            return ApiLazerMaterializeResult.Error(
                "No files could be materialized for this beatmapset."
            );

        return ApiLazerMaterializeResult.SuccessResult(targetDir);
    }
}
