namespace MapsetVerifier.Server.Model
{
    public class RunChecksRequest
    {
        public string Folder { get; set; } = string.Empty;
        public bool IncludeCheckRunDelta { get; set; } = true;
        public bool CreateSnapshot { get; set; } = true;

        /// <summary> Developer-option opt-in: measures and returns per-check timings. </summary>
        public bool IncludeCheckTimings { get; set; } = false;
    }
}
