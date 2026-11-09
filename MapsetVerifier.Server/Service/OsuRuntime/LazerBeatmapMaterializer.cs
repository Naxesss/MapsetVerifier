using System.Collections.Concurrent;
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
    /// Serializes materialize for the same set so select + F5 reparse cannot race rewriting
    /// the same temp folder.
    /// </summary>
    private static readonly ConcurrentDictionary<string, object> LocksBySetId = new(
        StringComparer.OrdinalIgnoreCase
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

        var liveSetId =
            LazerRealmService.ResolveLiveBeatmapSetId(dataDirectory, beatmapSetId) ?? beatmapSetId;

        var gate = LocksBySetId.GetOrAdd(liveSetId, static _ => new object());
        lock (gate)
            return MaterializeLocked(dataDirectory, liveSetId);
    }

    private static ApiLazerMaterializeResult MaterializeLocked(
        string dataDirectory,
        string beatmapSetId
    )
    {
        var files = LazerRealmService.GetBeatmapSetFiles(dataDirectory, beatmapSetId);
        if (files == null)
            return ApiLazerMaterializeResult.Error(
                "Beatmapset could not be found in the lazer library."
            );

        var targetDir = Path.Combine(TempRoot, beatmapSetId);

        try
        {
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

        // Sync in place instead of Directory.Delete — F5/reparse often runs while checks/UI still
        // hold reads on the previous copy, and Windows refuses to remove locked files.
        var desiredRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var copiedAny = false;
        var copyFailures = 0;

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

                desiredRelativePaths.Add(Path.GetRelativePath(targetDir, destPath));

                if (!TryCopyFile(sourcePath, destPath))
                {
                    copyFailures++;
                    Log.Warning(
                        "Failed to materialize file {Filename} for lazer beatmapset {BeatmapSetId}",
                        filename,
                        beatmapSetId
                    );
                    continue;
                }

                copiedAny = true;
            }
            catch (Exception ex)
            {
                copyFailures++;
                Log.Warning(
                    ex,
                    "Failed to materialize file {Filename} for lazer beatmapset {BeatmapSetId}",
                    filename,
                    beatmapSetId
                );
            }
        }

        RemoveOrphanFiles(targetDir, desiredRelativePaths);

        if (!copiedAny)
            return ApiLazerMaterializeResult.Error(
                copyFailures > 0
                    ? "Failed to prepare a temp folder for the beatmapset."
                    : "No files could be materialized for this beatmapset."
            );

        return ApiLazerMaterializeResult.SuccessResult(targetDir, beatmapSetId);
    }

    private static bool TryCopyFile(string sourcePath, string destPath)
    {
        try
        {
            File.Copy(sourcePath, destPath, overwrite: true);
            return true;
        }
        catch (IOException)
        {
            // fall through
        }
        catch (UnauthorizedAccessException)
        {
            // fall through
        }

        var tempPath = destPath + ".mvtmp";
        try
        {
            File.Copy(sourcePath, tempPath, overwrite: true);
            File.Copy(tempPath, destPath, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static void RemoveOrphanFiles(string targetDir, HashSet<string> desiredRelativePaths)
    {
        IEnumerable<string> existingFiles;
        try
        {
            existingFiles = Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to enumerate materialized folder {TargetDir}", targetDir);
            return;
        }

        foreach (var existing in existingFiles)
        {
            if (existing.EndsWith(".mvtmp", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(existing);
                }
                catch
                {
                    // best-effort
                }
                continue;
            }

            var relative = Path.GetRelativePath(targetDir, existing);
            if (desiredRelativePaths.Contains(relative))
                continue;

            try
            {
                File.Delete(existing);
            }
            catch
            {
                // Locked leftover from a prior version — ignore.
            }
        }
    }
}
