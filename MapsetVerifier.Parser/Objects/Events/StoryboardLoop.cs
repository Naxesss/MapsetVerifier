namespace MapsetVerifier.Parser.Objects.Events
{
    /// <summary> A group of commands which is played a number of times in a row. </summary>
    public class StoryboardLoop
    {
        // _L,1000,4
        // __F,0,0,500,0,1
        // _event, start time, loop count

        public readonly List<StoryboardCommand> commands = [];
        public readonly int count;
        public readonly double startTime;

        public StoryboardLoop(double startTime, int count)
        {
            this.startTime = startTime;

            // The game plays a loop at least once.
            this.count = Math.Max(1, count);
        }

        private double FirstCommandTime => commands.Min(command => command.StartTime);

        /// <summary> Returns when the first command of the loop starts. </summary>
        public double GetStartTime() =>
            commands.Count == 0 ? startTime : startTime + FirstCommandTime;

        /// <summary>
        ///     Returns the time between the first command starting and the last command ending, which is how far apart the
        ///     repetitions are.
        /// </summary>
        public double GetIterationDuration() =>
            commands.Count == 0 ? 0 : commands.Max(command => command.EndTime) - FirstCommandTime;

        /// <summary> Returns when the last command of the last repetition ends. </summary>
        public double GetEndTime() => GetStartTime() + GetIterationDuration() * count;
    }
}
