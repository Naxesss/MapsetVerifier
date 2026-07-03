using System.Collections.Concurrent;
using System.Reflection;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using Serilog;

namespace MapsetVerifier.Framework
{
    public static class Checker
    {
        private const string CustomCheckFolder = "CustomChecks";

        private static string? CustomCheckDirectory { get; set; }
        private static readonly object CustomPluginReportLock = new();
        private static readonly object ReloadChecksLock = new();
        private static CustomCheckPluginReport CustomPluginReport { get; set; } = new();

        public static void ConfigureCustomChecksPath(string appDataPath, string externalFolderName)
        {
            var path = Path.Combine(appDataPath, externalFolderName, CustomCheckFolder);
            CustomCheckDirectory = path;
            CustomPluginReport = new CustomCheckPluginReport { DirectoryPath = path };
            Log.Information("Custom checks directory: {Path}", path);
        }

        public static CustomCheckPluginReport GetCustomCheckPluginReport()
        {
            lock (CustomPluginReportLock)
            {
                return new CustomCheckPluginReport
                {
                    DirectoryPath = CustomPluginReport.DirectoryPath,
                    CustomChecksEnabled = CustomPluginReport.CustomChecksEnabled,
                    LoadedPlugins = [.. CustomPluginReport.LoadedPlugins],
                    FailedPlugins = [.. CustomPluginReport.FailedPlugins],
                };
            }
        }

        public static CustomCheckPluginReport ReloadChecks(bool loadCustomChecks = true)
        {
            lock (ReloadChecksLock)
            {
                Log.Information("Reloading all checks...");
                CheckerRegistry.ClearChecks();

                Log.Information("Reloading default checks...");
                LoadDefaultChecks();

                if (loadCustomChecks)
                {
                    Log.Information("Reloading custom checks...");
                    LoadCustomChecks();
                }
                else
                {
                    Log.Information("Skipping custom checks because they are disabled");
                    SetCustomCheckPluginReport([], [], customChecksEnabled: false);
                }

                return GetCustomCheckPluginReport();
            }
        }

        public static void DisableCustomChecks()
        {
            Log.Information("Custom checks are disabled");
            SetCustomCheckPluginReport([], [], customChecksEnabled: false);
        }

        /// <summary> Returns a list of issues sorted by level, in the given beatmap set. </summary>
        public static List<Issue> GetBeatmapSetIssues(
            BeatmapSet beatmapSet,
            IProgress<CheckProgress>? progress = null
        )
        {
            var issueBag = new ConcurrentBag<Issue>();
            var total = CountCheckTasks(beatmapSet);
            CheckProgressTracker? tracker = null;

            if (progress != null)
            {
                tracker = new CheckProgressTracker(total, progress);
                progress.Report(new CheckProgress(0, total, []));
            }

            TryGetIssuesParallel(
                CheckerRegistry.GetGeneralChecks(),
                generalCheck =>
                {
                    foreach (
                        var issue in generalCheck
                            .GetIssues(beatmapSet)
                            .OrderBy(issue => issue.level)
                            .Reverse()
                    )
                        issueBag.Add(issue.WithOrigin(generalCheck));
                },
                tracker,
                check => check.GetMetadata().Message
            );

            Parallel.ForEach(
                beatmapSet.Beatmaps,
                beatmap =>
                {
                    var applicableChecks = GetApplicableBeatmapChecks(beatmap);

                    TryGetIssuesParallel(
                        applicableChecks,
                        beatmapCheck =>
                        {
                            foreach (
                                var issue in beatmapCheck
                                    .GetIssues(beatmap)
                                    .OrderBy(issue => issue.level)
                                    .Reverse()
                            )
                                issueBag.Add(issue.WithOrigin(beatmapCheck));
                        },
                        tracker,
                        check =>
                            $"{check.GetMetadata().Message} [{beatmap.MetadataSettings.version}]"
                    );
                }
            );

            TryGetIssuesParallel(
                GetApplicableBeatmapSetChecks(beatmapSet),
                beatmapSetCheck =>
                {
                    foreach (
                        var issue in beatmapSetCheck
                            .GetIssues(beatmapSet)
                            .OrderBy(issue => issue.level)
                            .Reverse()
                    )
                        issueBag.Add(issue.WithOrigin(beatmapSetCheck));
                },
                tracker,
                check => check.GetMetadata().Message
            );

            return issueBag.OrderByDescending(issue => issue.level).ToList();
        }

        private static int CountCheckTasks(BeatmapSet beatmapSet)
        {
            var total = CheckerRegistry.GetGeneralChecks().Count();

            foreach (var beatmap in beatmapSet.Beatmaps)
                total += GetApplicableBeatmapChecks(beatmap).Count();

            total += GetApplicableBeatmapSetChecks(beatmapSet).Count();

            return total;
        }

        private static IEnumerable<BeatmapCheck> GetApplicableBeatmapChecks(Beatmap beatmap) =>
            CheckerRegistry
                .GetBeatmapChecks()
                .Where(beatmapCheck =>
                {
                    var modesToCheck = ((BeatmapCheckMetadata)beatmapCheck.GetMetadata()).Modes;
                    return modesToCheck.Contains(beatmap.GeneralSettings.mode);
                });

        private static IEnumerable<BeatmapSetCheck> GetApplicableBeatmapSetChecks(
            BeatmapSet beatmapSet
        ) =>
            CheckerRegistry
                .GetBeatmapSetChecks()
                .Where(beatmapSetCheck =>
                {
                    var modesToCheck = ((BeatmapCheckMetadata)beatmapSetCheck.GetMetadata()).Modes;
                    return beatmapSet.Beatmaps.Any(beatmap =>
                        modesToCheck.Contains(beatmap.GeneralSettings.mode)
                    );
                });

        private static void TryGetIssuesParallel<T>(
            IEnumerable<T> checks,
            Action<T> action,
            CheckProgressTracker? tracker,
            Func<T, string>? getLabel = null
        )
            where T : Check =>
            Parallel.ForEach(
                checks,
                check =>
                {
                    var label = getLabel?.Invoke(check) ?? check.GetMetadata().Message;
                    var taskId = tracker?.ReportStarted(label);

                    try
                    {
                        action(check);
                    }
                    catch (Exception exception)
                    {
                        exception.Data.Add("Check", check);

                        throw;
                    }
                    finally
                    {
                        if (taskId.HasValue)
                            tracker?.ReportCompleted(taskId.Value);
                    }
                }
            );

        /// <summary> Loads the .dll files from the current directory + relative path ("/checks" by default). </summary>
        public static void LoadCustomChecks()
        {
            var paths = GetCustomCheckPaths();
            var loadedPlugins = new ConcurrentBag<CustomCheckPluginInfo>();
            var failedPlugins = new ConcurrentBag<CustomCheckPluginLoadError>();

            if (paths.Count == 0)
            {
                Log.Information("No custom checks found");
                SetCustomCheckPluginReport([], [], customChecksEnabled: true);
                return;
            }

            Parallel.ForEach(
                paths,
                dllPath =>
                {
                    try
                    {
                        loadedPlugins.Add(LoadCustomChecks(dllPath));
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception, "Failed to load checks from {DllPath}", dllPath);
                        failedPlugins.Add(
                            new CustomCheckPluginLoadError
                            {
                                FileName = Path.GetFileName(dllPath),
                                FilePath = dllPath,
                                Message = exception.Message,
                                Details = exception.ToString(),
                            }
                        );
                    }
                }
            );

            SetCustomCheckPluginReport(
                loadedPlugins.OrderBy(plugin => plugin.FileName).ToArray(),
                failedPlugins.OrderBy(plugin => plugin.FileName).ToArray(),
                customChecksEnabled: true
            );
        }

        private static void SetCustomCheckPluginReport(
            CustomCheckPluginInfo[] loadedPlugins,
            CustomCheckPluginLoadError[] failedPlugins,
            bool customChecksEnabled
        )
        {
            lock (CustomPluginReportLock)
            {
                CustomPluginReport = new CustomCheckPluginReport
                {
                    DirectoryPath = CustomCheckDirectory ?? "",
                    CustomChecksEnabled = customChecksEnabled,
                    LoadedPlugins = loadedPlugins,
                    FailedPlugins = failedPlugins,
                };
            }
        }

        private static List<string> GetCustomCheckPaths()
        {
            var directoryPath =
                CustomCheckDirectory ?? throw new Exception("Custom check directory not set up");

            if (Directory.Exists(directoryPath))
                return Directory
                    .GetFiles(directoryPath)
                    .Where(filePath => filePath.EndsWith(".dll"))
                    .ToList();

            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (UnauthorizedAccessException)
            {
                // e.g. creating a new directory in Program Files.
            }

            return [];
        }

        /// <summary>
        ///     Adds checks from the assembly of the given DLL file path (can be
        ///     either absolute or relative) to the CheckerRegistry.
        /// </summary>
        private static CustomCheckPluginInfo LoadCustomChecks(string checkPath)
        {
            var rootedPath = checkPath;

            if (!Path.IsPathRooted(checkPath))
                rootedPath = Path.Combine(Directory.GetCurrentDirectory(), checkPath);

            // Loaded from a byte array rather than Assembly.LoadFrom(path) so the file on disk
            // isn't memory-mapped and locked, which on Windows prevents editing/deleting the .dll
            // until the whole process exits, even after a reload.
            var assemblyBytes = File.ReadAllBytes(rootedPath);
            var assembly = Assembly.Load(assemblyBytes);
            var checks = LoadCheckAssembly(assembly);
            var assemblyName = assembly.GetName();
            var authors = checks
                .Select(check => check.GetMetadata().Author)
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Distinct()
                .Order()
                .ToArray();

            if (authors.Length == 0)
                authors = GetAssemblyAuthors(assembly);

            return new CustomCheckPluginInfo
            {
                FileName = Path.GetFileName(rootedPath),
                FilePath = rootedPath,
                AssemblyName = assemblyName.Name ?? assemblyName.FullName,
                Version = assemblyName.Version?.ToString(),
                Authors = authors,
                CheckCount = checks.Count,
                GeneralCheckCount = checks.OfType<GeneralCheck>().Count(),
                BeatmapCheckCount = checks.OfType<BeatmapCheck>().Count(),
                BeatmapSetCheckCount = checks.OfType<BeatmapSetCheck>().Count(),
                CheckNames = checks
                    .Select(check => check.GetMetadata().Message)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
            };
        }

        private static string[] GetAssemblyAuthors(Assembly assembly)
        {
            var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;

            return new[] { company, product }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .Distinct()
                .ToArray();
        }

        /// <summary>
        ///     Adds checks from the current assembly to the CheckerRegistry. These are the default checks.
        ///     Improved to better support single-file/self-contained deployments by scanning already loaded assemblies.
        /// </summary>
        public static void LoadDefaultChecks()
        {
            try
            {
                Log.Information(
                    "Loading default checks from executing assembly or MapsetVerifier.Checks.dll"
                );

                // Direct marker type load (will force the runtime to load the checks assembly when project referenced).
                try
                {
                    var markerType = Type.GetType(
                        "MapsetVerifier.Checks.Common, MapsetVerifier.Checks"
                    );
                    if (markerType != null)
                    {
                        Log.Information(
                            "Loaded marker type {MarkerType} from checks assembly",
                            markerType.FullName
                        );
                        LoadCheckAssembly(markerType.Assembly);
                        if (CheckerRegistry.GetChecks().Count != 0)
                            return;
                    }
                    else
                    {
                        Log.Debug(
                            "Marker type lookup returned null (assembly may not be loaded yet)"
                        );
                    }
                }
                catch (Exception markerEx)
                {
                    Log.Debug(markerEx, "Marker type load failed");
                }

                // Single-file / self-contained fallback: scan already loaded assemblies for the checks assembly.
                var loadedChecksAssembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "MapsetVerifier.Checks");

                // Attempt explicit load by name (will succeed if it's available in deps without a separate file in single-file publish).
                try
                {
                    var nameLoad = Assembly.Load(new AssemblyName("MapsetVerifier.Checks"));
                    if (nameLoad != loadedChecksAssembly)
                    {
                        Log.Information(
                            "Loaded checks assembly {AssemblyName} via Assembly.Load(name)",
                            nameLoad.GetName().Name
                        );
                        LoadCheckAssembly(nameLoad);
                        if (CheckerRegistry.GetChecks().Count != 0)
                            return;
                    }
                }
                catch (Exception nameLoadEx)
                {
                    Log.Debug(
                        nameLoadEx,
                        "Assembly.Load by name for MapsetVerifier.Checks failed (may be expected in file-based probing phase)"
                    );
                }

                // Next: prefer the application's base directory.
                var baseDir = AppContext.BaseDirectory;
                var checksDllPath = Path.Combine(baseDir, "MapsetVerifier.Checks.dll");
                if (File.Exists(checksDllPath))
                {
                    var checksAssembly = Assembly.LoadFrom(checksDllPath);
                    LoadCheckAssembly(checksAssembly);
                    if (CheckerRegistry.GetChecks().Count != 0)
                        return;
                }
                else
                {
                    Log.Warning(
                        "Could not find MapsetVerifier.Checks.dll at {ChecksDllPath}",
                        checksDllPath
                    );
                }

                Log.Error(
                    "No checks could be loaded after all probing strategies. The application will run without validations."
                );
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to load default checks");
            }
        }

        /// <summary> When true, LoadCheckAssembly does not write to the console (e.g. when exporting metadata). </summary>
        public static bool SuppressLoadLogging { get; set; }

        /// <summary> Adds checks from the given assembly to the CheckerRegistry. </summary>
        private static List<Check> LoadCheckAssembly(Assembly assembly)
        {
            var registeredChecks = new List<Check>();

            foreach (var type in assembly.GetExportedTypes())
            {
                var attr = type.CustomAttributes.FirstOrDefault(attr =>
                    attr.AttributeType.Name == nameof(CheckAttribute)
                );
                if (!SuppressLoadLogging)
                    Log.Debug("Checking exported type {TypeFullName}", type.FullName);

                if (attr == null)
                    continue;

                // Avoid duplicate registration of the same check type.
                if (CheckerRegistry.GetChecks().Any(c => c.GetType() == type))
                {
                    Log.Debug("Skipping duplicate check type {TypeFullName}", type.FullName);
                    continue;
                }

                object? instance;
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (Exception createEx)
                {
                    Log.Error(
                        createEx,
                        "Failed to instantiate check type {TypeFullName}",
                        type.FullName
                    );
                    continue;
                }

                var check = instance as Check;
                CheckerRegistry.RegisterCheck(check);
                if (check != null)
                    registeredChecks.Add(check);
            }
            Log.Information(
                "Loaded checks from assembly {AssemblyName}. Total registered: {Count}",
                assembly.GetName().Name,
                CheckerRegistry.GetChecks().Count()
            );

            return registeredChecks;
        }
    }
}
