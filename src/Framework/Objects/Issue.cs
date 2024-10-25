using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Framework.Objects
{
    public class Issue
    {
        /// <summary> The type of issue, index order determines priority when categorizing. </summary>
        public enum Level
        {
            Info,
            Check,
            Error,
            Minor,
            Warning,
            Problem
        }

        public readonly Beatmap beatmap;
        public readonly Level level;

        public readonly string message;

        public Issue(IssueTemplate template, Beatmap beatmap, params object[] templateArguments)
        {
            Template = template;
            InterpretationPairs = new List<KeyValuePair<string, int>>();

            message = template.Format(templateArguments);
            this.beatmap = beatmap;
            level = template.Level;
        }

        public IssueTemplate Template { get; set; }
        public List<KeyValuePair<string, int>> InterpretationPairs { get; }

        /// <summary> Populated during the checking process. </summary>
        public Check CheckOrigin { get; private set; }

        /// <summary> Whether this issue applies to the given difficulty level according to the metadata and interpretation. </summary>
        public bool AppliesToDifficulty(Beatmap.Difficulty difficulty)
        {
            var appliesByMetadata = !(CheckOrigin.GetMetadata() is BeatmapCheckMetadata metadata) || metadata.Difficulties.Contains(difficulty);

            var appliesByInterpretation = !InterpretationPairs.Any() || InterpretationPairs.Any(pair => pair.Key == "difficulty" && (Beatmap.Difficulty)pair.Value == difficulty);

            return appliesByMetadata && appliesByInterpretation;
        }

        /// <summary> Sets the check origin (i.e. the check instance that created this issue) </summary>
        public Issue WithOrigin(Check check)
        {
            CheckOrigin = check;

            return this;
        }

        /// <summary>
        ///     Equivalent to using <see cref="WithInterpretation" /> with first argument "difficulty" and rest cast to
        ///     integers.
        /// </summary>
        public Issue ForDifficulties(params Beatmap.Difficulty[] difficulties) => WithInterpretation("difficulty", difficulties.Select(diff => (int)diff).ToArray());

        /// <summary>
        ///     Sets the data of the issue, which can be used by applications to only show the check in certain settings,
        ///     for example only for certain difficulty levels, see <see cref="ForDifficulties" />.
        ///     <para></para>
        ///     Takes string, int, and int[]. Example: WithInterpretation("difficulty", 0, 1, 2, "other", 2, 3)
        /// </summary>
        public Issue WithInterpretation(params object[] severityParams)
        {
            ParseInterpretation(severityParams);

            return this;
        }

        private void ParseInterpretation(object[] interpretParams)
        {
            var interpretType = "";

            foreach (var interpretParam in interpretParams)
                if (interpretParam is string)
                    interpretType = interpretParam as string;

                else if (interpretParam is int)
                    InterpretationPairs.Add(new KeyValuePair<string, int>(interpretType, (int)interpretParam));

                else if (interpretParam is int[])
                    foreach (var interpretation in (int[])interpretParam)
                        InterpretationPairs.Add(new KeyValuePair<string, int>(interpretType, interpretation));
        }
    }
}