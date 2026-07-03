namespace MapsetVerifier.Server.Model
{
    public class RunChecksRequest
    {
        public string Folder { get; set; } = string.Empty;
        public bool IncludeCheckRunDelta { get; set; } = true;
        public bool CreateSnapshot { get; set; } = true;
    }
}
