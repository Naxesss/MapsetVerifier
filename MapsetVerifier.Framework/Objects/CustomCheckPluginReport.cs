namespace MapsetVerifier.Framework.Objects
{
    public class CustomCheckPluginReport
    {
        public string DirectoryPath { get; set; } = "";
        public CustomCheckPluginInfo[] LoadedPlugins { get; set; } = [];
        public CustomCheckPluginLoadError[] FailedPlugins { get; set; } = [];
    }
}
