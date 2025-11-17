using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Rendering
{
    public abstract class ChecksRenderer : BeatmapInfoRenderer
    {
        public static string Render(List<Issue> issues, BeatmapSet beatmapSet) => string.Concat(
            RenderBeatmapInfo(beatmapSet),
            RenderBeatmapDifficulties(issues, beatmapSet),
            RenderBeatmapChecks(issues, beatmapSet)
        );

        private static string RenderBeatmapDifficulties(List<Issue> issues, BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];
            var generalIssues = issues.Where(issue => issue.CheckOrigin is GeneralCheck).ToArray();

            return
                Div("beatmap-difficulties",
                    DivAttr("beatmap-difficulty noselect", DataAttr("difficulty", "General") + DataAttr("beatmap-id", "-"),
                        Div("medium-icon " + GetIcon(generalIssues) + "-icon"),
                        Div("difficulty-name", "General")) +
                    string.Concat(beatmapSet.Beatmaps.Select(beatmap =>
                    {
                        var beatmapIssues = issues.Where(issue => issue.beatmap == beatmap).Except(generalIssues);
                        var version = Encode(beatmap.MetadataSettings.version);
                        var beatmapId = Encode(beatmap.MetadataSettings.beatmapId.ToString());

                        return DivAttr("beatmap-difficulty noselect" + (beatmap == refBeatmap ? " beatmap-difficulty-selected" : ""), DataAttr("difficulty", version) + DataAttr("beatmap-id", beatmapId),
                            Div("medium-icon " + GetIcon(beatmapIssues) + "-icon"),
                            Div("difficulty-name", version));
                    })));
        }

        private static string RenderBeatmapChecks(List<Issue> issues, BeatmapSet beatmapSet)
        {
            var generalIssues = issues.Where(issue => issue.CheckOrigin is GeneralCheck).ToArray();

            return
                Div("paste-separator") +
                Div("card-container-unselected",
                    DivAttr("card-difficulty", DataAttr("difficulty", "General"),
                        RenderBeatmapCategories(generalIssues, "General", true)),
                    string.Concat(beatmapSet.Beatmaps.Select(beatmap =>
                    {
                        var beatmapIssues = issues.Where(issue => issue.beatmap == beatmap).Except(generalIssues);
                        var version = Encode(beatmap.MetadataSettings.version);

                        return DivAttr("card-difficulty", DataAttr("difficulty", version),
                            RenderBeatmapInterpretation(beatmap, beatmap.GetDifficulty()),
                            RenderBeatmapCategories(beatmapIssues, version));
                    }))) +
                Div("paste-separator select-separator") +
                Div("card-container-selected");
        }

        private static string RenderBeatmapInterpretation(Beatmap beatmap, Beatmap.Difficulty defaultInterpretation)
        {
            if (beatmap == null)
                return "";

            return
                Div("",
                    DivAttr("interpret-container", DataAttr("interpret", "difficulty"),
                        ((int[])Enum.GetValues(typeof(Beatmap.Difficulty))).Select(@enum =>
                        {
                            // Expert and Ultra are considered the same interpretation
                            if (@enum == (int)Beatmap.Difficulty.Ultra)
                                return "";

                            var shouldSubstituteUltra =
                                @enum == (int)Beatmap.Difficulty.Expert && defaultInterpretation == Beatmap.Difficulty.Ultra;

                            return
                                DivAttr("interpret" + (@enum == (int)defaultInterpretation || shouldSubstituteUltra ? " interpret-selected interpret-default" : ""), DataAttr("interpret-severity", @enum),
                                    Enum.GetName(typeof(Beatmap.Difficulty), @enum));
                        }).ToArray<object?>()));
        }

        private static string RenderBeatmapCategories(IEnumerable<Issue> beatmapIssues, string? version, bool general = false) =>
            Div("card-difficulty-checks",
                CheckerRegistry.GetChecks().Where(check => general == check is GeneralCheck).GroupBy(check => check.GetMetadata().Category).Select(group =>
                {
                    var category = group.Key;
                    var issues = beatmapIssues.Where(issue => issue.CheckOrigin?.GetMetadata().Category == category).ToArray();

                    return
                        DivAttr("card", DataAttr("difficulty", version),
                            Div("card-box shadow noselect",
                                Div("large-icon " + GetIcon(issues) + "-icon"),
                                Div("card-title",
                                    Encode(category))),
                            Div("card-details-container",
                                Div("card-details",
                                    RenderBeatmapIssues(issues, category, general))));
                }).ToArray());

        private static string RenderBeatmapIssues(IEnumerable<Issue> categoryIssues, string category, bool general = false) =>
            string.Concat(
                CheckerRegistry.GetChecks()
                    .Where(check => check.GetMetadata().Category == category && general == check is GeneralCheck)
                    .Select(check =>
                    {
                        var metadata = check.GetMetadata() as BeatmapCheckMetadata;
                        var issues = categoryIssues.Where(issue => issue.CheckOrigin == check).ToArray();
                        var message = check.GetMetadata().Message;

                        return
                            DivAttr("card-detail", $"{(metadata != null ? DifficultiesDataAttr(metadata.Difficulties) : "")} data-check=\"{Encode(message)}\"",
                                Div("card-detail-icon " + GetIcon(issues) + "-icon"),
                                issues.Any()
                                    ? Div("",
                                        Div("card-detail-text",
                                            Encode(message)),
                                        DivAttr("vertical-arrow card-detail-toggle detail-shortcut", Tooltip("Toggle details")))
                                    : Div("card-detail-text",
                                        Encode(message)),
                                DivAttr("doc-shortcut detail-shortcut", Tooltip("Show documentation"))) +
                            RenderBeatmapDetails(issues);
                    }));

        private static string RenderBeatmapDetails(IEnumerable<Issue> checkIssues)
        {
            checkIssues = checkIssues.ToArray();

            if (!checkIssues.Any())
                return "";

            return Div("card-detail-instances", checkIssues.Select(issue =>
            {
                var icon = GetIcon(issue.level);
                var timestampedMessage = Format(Encode(issue.message)!);

                if (timestampedMessage.Length == 0)
                    return "";

                return
                    DivAttr("card-detail", DataAttr("template", issue.Template) + DifficultiesDataAttr(issue),
                        Div("card-detail-icon " + icon + "-icon"),
                        Div("",
                            timestampedMessage));
            }).ToArray());
        }
    }
}