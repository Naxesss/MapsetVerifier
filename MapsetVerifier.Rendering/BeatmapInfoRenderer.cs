using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Rendering
{
    public class BeatmapInfoRenderer : Renderer
    {
        protected static string RenderBeatmapInfo(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];

            return
                Div("beatmap-container",
                    Div("beatmap-title",
                        Encode(refBeatmap.MetadataSettings.artist) + " - " + Encode(refBeatmap.MetadataSettings.title)),
                    Div("beatmap-author-field",
                        "Beatmapset by " + UserLink(Encode(refBeatmap.MetadataSettings.creator) ?? string.Empty)),
                    Div("beatmap-options",
                        DivAttr("beatmap-options-folder beatmap-option beatmap-option-filter folder-icon", DataAttr("folder", Encode(beatmapSet.SongPath)) + Tooltip("Open song folder")),
                        refBeatmap.MetadataSettings.beatmapSetId != null
                            ? DivAttr("beatmap-options-web beatmap-option beatmap-option-filter web-icon", DataAttr("setid", Encode(refBeatmap.MetadataSettings.beatmapSetId.ToString())) + Tooltip("Open beatmap page"))
                            : DivAttr("beatmap-option beatmap-option-filter no-click web-unavailable-icon", Tooltip("No beatmap page available")),
                        refBeatmap.MetadataSettings.beatmapSetId != null
                            ? DivAttr("beatmap-options-discussion beatmap-option beatmap-option-filter discussion-icon", DataAttr("setid", Encode(refBeatmap.MetadataSettings.beatmapSetId.ToString())) + Tooltip("Open discussion page for selected difficulty"))
                            : DivAttr("beatmap-option beatmap-option-filter no-click discussion-unavailable-icon", Tooltip("No discussion page available")),
                        refBeatmap.MetadataSettings.beatmapSetId != null
                            ? DivAttr("beatmap-options-link beatmap-option beatmap-option-filter link-icon", DataAttr("setid", Encode(refBeatmap.MetadataSettings.beatmapSetId.ToString())) + Tooltip("Open osu!direct panel (osu!supporter only)"))
                            : DivAttr("beatmap-option beatmap-option-filter no-click link-unavailable-icon", Tooltip("No osu!direct panel available"))));
        }
    }
}