namespace MapsetVerifier.Parser.Objects.Events
{
    /// <summary> A group of commands which is played whenever something happens in gameplay within a time range. </summary>
    public class StoryboardTrigger
    {
        // _T,HitSoundClap,1000,2000
        // __F,0,0,500,1,0
        // _event, trigger name, start time, end time, group number (optional)

        public readonly List<StoryboardCommand> commands = [];

        /// <summary> The end of the range, or null if the trigger can always happen. </summary>
        public readonly double? endTime;

        /// <summary> What sets off the trigger, e.g. HitSoundClap, Passing or Failing. </summary>
        public readonly string name;

        /// <summary> The start of the range, or null if the trigger can always happen. </summary>
        public readonly double? startTime;

        public StoryboardTrigger(string name, double? startTime, double? endTime)
        {
            this.name = name;
            this.startTime = startTime;
            this.endTime = endTime;
        }
    }
}
