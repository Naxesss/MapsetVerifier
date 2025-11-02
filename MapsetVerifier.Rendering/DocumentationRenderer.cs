using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Rendering
{
    public abstract class DocumentationRenderer : Renderer
    {
        public static string Render() =>
            string.Concat(RenderIcons(), RenderChecks());

        private static string RenderChecks()
        {
            var checks = CheckerRegistry.GetChecks();

            var generalChecks = checks.Where(check => check is GeneralCheck).ToArray();

            var allModeChecks = checks.Where(check =>
            {
                var metadata = check.GetMetadata() as BeatmapCheckMetadata;

                if (metadata == null)
                    return false;

                return metadata.Modes.Contains(Beatmap.Mode.Standard) && metadata.Modes.Contains(Beatmap.Mode.Taiko) && metadata.Modes.Contains(Beatmap.Mode.Catch) && metadata.Modes.Contains(Beatmap.Mode.Mania);
            }).ToArray();

            bool hasMode(Check check, Beatmap.Mode mode) => (check.GetMetadata() as BeatmapCheckMetadata)?.Modes.Contains(mode) ?? false;

            var standardChecks = checks.Where(check => hasMode(check, Beatmap.Mode.Standard)).Except(allModeChecks).Except(generalChecks);
            var catchChecks = checks.Where(check => hasMode(check, Beatmap.Mode.Taiko)).Except(allModeChecks).Except(generalChecks);
            var taikoChecks = checks.Where(check => hasMode(check, Beatmap.Mode.Catch)).Except(allModeChecks).Except(generalChecks);
            var maniaChecks = checks.Where(check => hasMode(check, Beatmap.Mode.Mania)).Except(allModeChecks).Except(generalChecks);

            return
                RenderModeCategory("General", generalChecks) +
                RenderModeCategory("All Modes", allModeChecks) +
                RenderModeCategory("Standard", standardChecks) +
                RenderModeCategory("Taiko", catchChecks) +
                RenderModeCategory("Catch", taikoChecks) +
                RenderModeCategory("Mania", maniaChecks);
        }

        private static string RenderModeCategory(string title, IEnumerable<Check> checks)
        {
            checks = checks.ToArray();
            
            if (!checks.Any())
                return "";

            return
                Div("doc-mode-title", title) +
                Div("doc-mode-content",
                    Div("doc-mode-inner",
                        checks.OrderByDescending(check => check.GetMetadata().Category).Select(RenderCheckBox).ToArray()));
        }

        /// <summary> Returns the html of a check as shown in the documentation tab. </summary>
        public static string RenderCheckBox(Check check) =>
            RenderDocBox(
                icon: "check",
                title: Encode(check.GetMetadata().Message),
                subtitle: Encode(check.GetMetadata().GetMode() + " > " + check.GetMetadata().Category),
                issueOverview: string.Concat(check.GetTemplates().Select(pair => pair.Value).Select(template => Div("card-detail-icon " + GetIcon(template.Level) + "-icon"))),
                author: Encode(check.GetMetadata().Author));

        private static string RenderIcons() =>
            Div("doc-mode-title",
                "Icons") +
            Div("doc-mode-inner doc-mode-icons",
                RenderIconsDocBox("check", "Check", "Checks", "No issues were found."),
                RenderIconsDocBox("error", "Error", "Checks", "An error occurred preventing a complete check."),
                RenderIconsDocBox("minor", "Minor", "Checks", "One or more negligible issues may have been found."),
                RenderIconsDocBox("exclamation", "Warning", "Checks", "One or more issues may have been found."),
                RenderIconsDocBox("cross", "Problem", "Checks", "One or more issues were found."),
                RenderIconsDocBox("gear-gray", "None", "Snapshots", "No changes were made."),
                RenderIconsDocBox("minus", "Removal", "Snapshots", "One or more lines were removed."),
                RenderIconsDocBox("plus", "Addition", "Snapshots", "One or more lines were added."),
                RenderIconsDocBox("gear-blue", "Change", "Snapshots", "One or more lines were changed."));

        private static string RenderDocBox(string icon, string title, string subtitle, string issueOverview = null, string author = null) =>
            Div("doc-box-container",
                Div("doc-box-left",
                    Div("doc-box-icon-container",
                        Div("doc-box-icon " + icon + "-icon")),
                    Div("doc-box-content",
                        Div("doc-box-title", title),
                        Div("doc-box-subtitle", subtitle))),
                Div("doc-box-right",
                    Div("doc-box-right-upper", issueOverview),
                    Div("doc-box-right-lower", author)));

        private static string RenderIconsDocBox(string icon, string title, string subtitle, string description) =>
            Div("doc-box-container",
                Div("doc-box-left",
                    Div("doc-box-icon-container",
                        Div("doc-box-icon " + icon + "-icon")),
                    Div("doc-box-content",
                        Div("doc-box-title", title),
                        Div("doc-box-subtitle", subtitle))),
                Div("doc-box-right", description));
    }
}