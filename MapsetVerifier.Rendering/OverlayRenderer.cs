using System.Linq;
using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;

namespace MapsetVerifier.Rendering
{
    public abstract class OverlayRenderer : Renderer
    {
        public static string Render(string checkMessage)
        {
            var check = CheckerRegistry.GetChecks().FirstOrDefault(check => check.GetMetadata().Message == checkMessage);

            if (check?.GetMetadata() == null)
                return "No documentation found for check with message \"" + checkMessage + "\".";

            return string.Concat(
                RenderOverlayTop(check.GetMetadata()),
                RenderOverlayContent(check));
        }

        private static string RenderOverlayTop(CheckMetadata metadata) =>
            Div("\" id=\"overlay-top",
                Div("check-icon\" id=\"overlay-top-icon"),
                Div("\" id=\"overlay-top-title",
                    Encode(metadata.Message))) +
            Div("\" id=\"overlay-top-subfields",
                Div("\" id=\"overlay-top-category",
                    Encode(metadata.GetMode() + " > " + metadata.Category)),
                Div("\" id=\"overlay-top-author",
                    "Created by " + Encode(metadata.Author)));

        private static string RenderOverlayContent(Check check) =>
            Div("paste-separator") +
            Div("\" style=\"clear:both;") +
            Div("\" id=\"overlay-content",
                string.Concat(
                    RenderOverlayTemplates(check),
                    RenderOverlayDocumentation(check.GetMetadata())));

        private static string RenderOverlayTemplates(Check check) =>
            check.GetTemplates().Count > 0
                ? string.Concat(
                    check.GetTemplates()
                        .Select(pair => pair.Value)
                        .Select(template =>
                        {
                            return
                                Div("check",
                                    Div("card-detail-icon " + GetIcon(template.Level) + "-icon"),
                                    Div("message",
                                        template.Format(template.GetDefaultArguments().Select(arg => "<span>" + arg + "</span>").ToArray()),
                                        template.Cause != null ? Div("cause", Format(template.Cause)) : ""));
                                       
                        }))
                : "No issue templates available.";

        private static string RenderOverlayDocumentation(CheckMetadata metadata) =>
            string.Concat(metadata.Documentation.Select(section =>
            {
                var value = section.Value;

                return
                    Div("title",
                        Encode(section.Key)) +
                    Format(value);
            }));
    }
}