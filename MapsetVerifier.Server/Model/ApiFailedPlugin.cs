namespace MapsetVerifier.Server.Model;

public class ApiFailedPlugin
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Message { get; set; } = "";
    public string Details { get; set; } = "";
}
