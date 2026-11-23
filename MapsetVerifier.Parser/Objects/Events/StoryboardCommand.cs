using System.Numerics;

namespace MapsetVerifier.Parser.Objects.Events
{
    /// <summary>
    ///     A single command of a storyboard sprite or animation. Values that the type doesn't use are 0. Times inside
    ///     loops and triggers are relative to when the loop or trigger starts.
    /// </summary>
    public readonly record struct StoryboardCommand(
        StoryboardCommand.Kind Type,
        int Easing,
        double StartTime,
        double EndTime,
        Vector3 StartValue,
        Vector3 EndValue,
        char Parameter = default
    )
    {
        // _F,0,1000,2000,0,1
        // _event, easing, start time, end time, start value(s), end value(s)

        public enum Kind
        {
            Fade,
            Move,
            MoveX,
            MoveY,
            Scale,
            VectorScale,
            Rotate,
            Colour,
            Parameter,
        }

        public double Duration => EndTime - StartTime;
    }
}
