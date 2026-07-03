namespace MapsetVerifier.Server.Model;

public class ApiPluginReport
{
    public string DirectoryPath { get; set; } = "";
    public bool CustomChecksEnabled { get; set; } = true;
    public ApiLoadedPlugin[] LoadedPlugins { get; set; } = [];
    public ApiFailedPlugin[] FailedPlugins { get; set; } = [];
}
