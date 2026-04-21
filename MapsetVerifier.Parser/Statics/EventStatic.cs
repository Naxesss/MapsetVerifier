namespace MapsetVerifier.Parser.Statics
{
    public static class EventStatic
    {
        /// <summary> Called whenever loading of something is started. </summary>
        public static Func<string, Task> OnLoadStart { get; set; } = _ => Task.CompletedTask;

        /// <summary> Called whenever loading of something is completed. </summary>
        public static Func<string, Task> OnLoadComplete { get; set; } = _ => Task.CompletedTask;
    }
}