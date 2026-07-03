namespace MapsetVerifier.Framework.Objects
{
    public class CustomCheckPluginReport
    {
        public string DirectoryPath { get; set; } = "";
        public bool CustomChecksEnabled { get; set; } = true;
        public CustomCheckPluginInfo[] LoadedPlugins { get; set; } = [];
        public CustomCheckPluginLoadError[] FailedPlugins { get; set; } = [];
    }
}
