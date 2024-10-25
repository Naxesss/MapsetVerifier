using System;
using System.Threading.Tasks;

namespace MapsetVerifier.Parser.Statics
{
    public static class EventStatic
    {
        /// <summary> Called whenever loading of something is started. </summary>
        public static Func<string, Task> OnLoadStart { get; set; } = message => { return Task.CompletedTask; };

        /// <summary> Called whenever loading of something is completed. </summary>
        public static Func<string, Task> OnLoadComplete { get; set; } = message => { return Task.CompletedTask; };
    }
}