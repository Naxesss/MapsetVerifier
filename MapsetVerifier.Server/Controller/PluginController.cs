using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Server.Model;
using Microsoft.AspNetCore.Mvc;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("plugins")]
public class PluginController : ControllerBase
{
    [HttpGet]
    public ApiPluginReport GetPlugins()
    {
        return ToApiPluginReport(Checker.GetCustomCheckPluginReport());
    }

    [HttpPost("reload")]
    public ApiPluginReport ReloadPlugins([FromBody] ApiPluginReloadRequest? request)
    {
        return ToApiPluginReport(
            Checker.ReloadChecks(loadCustomChecks: request?.CustomChecksEnabled ?? true)
        );
    }

    private static ApiPluginReport ToApiPluginReport(CustomCheckPluginReport report)
    {
        return new ApiPluginReport
        {
            DirectoryPath = report.DirectoryPath,
            CustomChecksEnabled = report.CustomChecksEnabled,
            LoadedPlugins = report
                .LoadedPlugins.Select(plugin => new ApiLoadedPlugin
                {
                    FileName = plugin.FileName,
                    FilePath = plugin.FilePath,
                    AssemblyName = plugin.AssemblyName,
                    Version = plugin.Version,
                    Authors = plugin.Authors,
                    CheckCount = plugin.CheckCount,
                    GeneralCheckCount = plugin.GeneralCheckCount,
                    BeatmapCheckCount = plugin.BeatmapCheckCount,
                    BeatmapSetCheckCount = plugin.BeatmapSetCheckCount,
                    CheckNames = plugin.CheckNames,
                })
                .ToArray(),
            FailedPlugins = report
                .FailedPlugins.Select(plugin => new ApiFailedPlugin
                {
                    FileName = plugin.FileName,
                    FilePath = plugin.FilePath,
                    Message = plugin.Message,
                    Details = plugin.Details,
                })
                .ToArray(),
        };
    }
}
