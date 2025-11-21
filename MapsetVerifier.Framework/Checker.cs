using System.Collections.Concurrent;
using System.Reflection;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser;
using Serilog;

namespace MapsetVerifier.Framework
{
    public static class Checker
    {
        public const string DefaultRelativeDLLDirectory = "checksV2";
        
        public static string? RelativeDLLDirectory { get; set; }

        /// <summary> Called whenever the loading of a check is started. </summary>
        public static Func<string, Task> OnLoadStart { get; set; } = message => Task.CompletedTask;

        /// <summary> Called whenever the loading of a check is completed. </summary>
        public static Func<string, Task> OnLoadComplete { get; set; } = message => Task.CompletedTask;

        /// <summary> Returns a list of issues sorted by level, in the given beatmap set. </summary>
        public static List<Issue> GetBeatmapSetIssues(BeatmapSet beatmapSet)
        {
            if (!CheckerRegistry.GetChecks().Any())
                LoadCustomChecks();

            var issueBag = new ConcurrentBag<Issue>();

            TryGetIssuesParallel(CheckerRegistry.GetGeneralChecks(), generalCheck =>
            {
                foreach (var issue in generalCheck.GetIssues(beatmapSet).OrderBy(issue => issue.level).Reverse())
                    issueBag.Add(issue.WithOrigin(generalCheck));
            });

            Parallel.ForEach(beatmapSet.Beatmaps, beatmap =>
            {
                var beatmapTrack = new Track("Checking for issues in " + beatmap + "...");

                TryGetIssuesParallel(CheckerRegistry.GetBeatmapChecks(), beatmapCheck =>
                {
                    var modesToCheck = ((BeatmapCheckMetadata)beatmapCheck.GetMetadata()).Modes;

                    if (!modesToCheck.Contains(beatmap.GeneralSettings.mode))
                        return;

                    foreach (var issue in beatmapCheck.GetIssues(beatmap).OrderBy(issue => issue.level).Reverse())
                        issueBag.Add(issue.WithOrigin(beatmapCheck));
                });

                beatmapTrack.Complete();
            });

            TryGetIssuesParallel(CheckerRegistry.GetBeatmapSetChecks(), beatmapSetCheck =>
            {
                var modesToCheck = ((BeatmapCheckMetadata)beatmapSetCheck.GetMetadata()).Modes;

                if (!beatmapSet.Beatmaps.Any(beatmap => modesToCheck.Contains(beatmap.GeneralSettings.mode)))
                    return;

                foreach (var issue in beatmapSetCheck.GetIssues(beatmapSet).OrderBy(issue => issue.level).Reverse())
                    issueBag.Add(issue.WithOrigin(beatmapSetCheck));
            });

            return issueBag.OrderByDescending(issue => issue.level).ToList();
        }

        private static void TryGetIssuesParallel<T>(IEnumerable<T> checks, Action<T> action) where T : Check =>
            Parallel.ForEach(checks, check =>
            {
                // Will end up "..." due to message always including a period at the end.
                var checkTrack = new Track($"Checking for {check.GetMetadata().Message}..");

                try
                {
                    action(check);
                }
                catch (Exception exception)
                {
                    exception.Data.Add("Check", check);

                    throw;
                }

                checkTrack.Complete();
            });

        /// <summary> Loads the .dll files from the current directory + relative path ("/checks" by default). </summary>
        public static void LoadCustomChecks()
        {
            var paths = GetCustomCheckDLLPaths();
            Parallel.ForEach(paths, dllPath =>
            {
                try
                {
                    var dllTrack = new Track("Loading checks from \"" + dllPath.Split('/', '\\').Last() + "\"...");

                    LoadCheckDLL(dllPath);

                    dllTrack.Complete();
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to load checks from {DllPath}", dllPath);
                }
            });
        }

        private static IEnumerable<string> GetCustomCheckDLLPaths()
        {
            var directoryPath = RelativeDLLDirectory ?? DefaultRelativeDLLDirectory;

            if (Directory.Exists(directoryPath))
                return Directory.GetFiles(directoryPath).Where(filePath => filePath.EndsWith(".dll"));

            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (UnauthorizedAccessException)
            {
                // e.g. creating a new directory in Program Files.
            }

            return new List<string>();
        }

        /// <summary>
        ///     Adds checks from the assembly of the given DLL file path (can be
        ///     either absolute or relative) to the CheckerRegistry.
        /// </summary>
        private static void LoadCheckDLL(string checkPath)
        {
            var rootedPath = checkPath;

            if (!Path.IsPathRooted(checkPath))
                rootedPath = Path.Combine(Directory.GetCurrentDirectory(), checkPath);

            var assembly = Assembly.LoadFrom(rootedPath);
            LoadCheckAssembly(assembly);
        }

        /// <summary>
        ///     Adds checks from the current assembly to the CheckerRegistry. These are the default checks.
        /// </summary>
        public static void LoadDefaultChecks()
        {
            try
            {
                Log.Information("Loading default checks from MapsetVerifier.Checks.dll");
                var localChecksDllPath = Path.Combine(Directory.GetCurrentDirectory(), "MapsetVerifier.Checks.dll");
                if (File.Exists(localChecksDllPath))
                {
                    var checksAssembly = Assembly.LoadFrom(localChecksDllPath);
                    LoadCheckAssembly(checksAssembly);
                    return;
                }

                Log.Warning("Could not find MapsetVerifier.Checks.dll at {LocalChecksDllPath}", localChecksDllPath);

                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (assemblyDir == null)
                {
                    Log.Warning("Could not find executing assembly directory {AssemblyDir}", assemblyDir);
                    return;
                }
                
                var checksDllPath = Path.Combine(assemblyDir, "MapsetVerifier.Checks.dll");

                if (File.Exists(checksDllPath))
                {
                    var checksAssembly = Assembly.LoadFrom(checksDllPath);
                    LoadCheckAssembly(checksAssembly);
                }
                else
                {
                    Log.Warning("Could not find MapsetVerifier.Checks.dll at {ChecksDllPath}", checksDllPath);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to load default checks");
            }
        }

        /// <summary> Adds checks from the given assembly to the CheckerRegistry. </summary>
        private static void LoadCheckAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                var attr = type.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == nameof(CheckAttribute));
                Log.Debug("Checking exported type {TypeFullName}", type.FullName);
                
                if (attr == null)
                    continue;

                var instance = Activator.CreateInstance(type);
                var check = instance as Check;
                CheckerRegistry.RegisterCheck(check);
            }
            Log.Information("Loaded checks from assembly {AssemblyName}. Total registered: {Count}", assembly.GetName().Name, CheckerRegistry.GetChecks().Count());
        }
    }
}