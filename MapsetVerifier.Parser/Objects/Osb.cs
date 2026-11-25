using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects
{
    public class Osb
    {
        public readonly List<Animation> animations;

        public readonly List<Background> backgrounds;
        public readonly List<Break> breaks;
        public readonly List<Sample> samples;
        public readonly List<Sprite> sprites;
        public readonly List<Video> videos;
        public string code;

        public Osb(string code)
        {
            this.code = code;

            var lines = code.Split(["\n"], StringSplitOptions.None);

            // substitute variables in the code before looking at the event part
            var substitutions = new List<KeyValuePair<string, string>>();

            ParserStatic.ApplySettings(
                lines,
                "Variables",
                sectionLines =>
                {
                    foreach (var line in sectionLines)
                        // Anything without a value can't be substituted.
                        if (line.StartsWith("$") && line.Split('=', 2) is [var key, var value])
                            substitutions.Add(
                                new KeyValuePair<string, string>(key.Trim(), value.Trim())
                            );
                }
            );

            var substitutedCode = code;

            foreach (var substitution in substitutions)
                substitutedCode = substitutedCode.Replace(substitution.Key, substitution.Value);

            var codeResult = substitutedCode;
            var linesResult = codeResult.Split(["\n"], StringSplitOptions.None);

            backgrounds = GetEvents(linesResult, ["Background", "0"], args => new Background(args));
            videos = GetEvents(linesResult, ["Video", "1"], args => new Video(args));
            breaks = GetEvents(linesResult, ["Break", "2"], args => new Break(args));
            (sprites, animations) = ParserStatic.GetSettings(
                linesResult,
                "Events",
                StoryboardStatic.ParseStoryboard
            );
            samples = GetEvents(linesResult, ["Sample", "5"], args => new Sample(args));
        }

        /// <summary> Returns whether the .osb file is actually used as a storyboard (or if it's just empty). </summary>
        public bool IsUsed() =>
            backgrounds.Count > 0
            || videos.Count > 0
            || breaks.Count > 0
            || sprites.Count > 0
            || samples.Count > 0
            || animations.Count > 0;

        private List<T> GetEvents<T>(string[] lines, List<string> types, Func<string[], T> func)
        {
            // find all lines starting with any of types in the event section
            var foundTypes = new List<T>();

            ParserStatic.ApplySettings(
                lines,
                "Events",
                sectionLines =>
                {
                    foreach (var line in sectionLines)
                        if (types.Any(type => line.StartsWith(type + ",")))
                            foundTypes.Add(
                                ParserStatic.Read("Events", line, l => func(l.Split(',')))
                            );
                }
            );

            return foundTypes;
        }
    }
}
