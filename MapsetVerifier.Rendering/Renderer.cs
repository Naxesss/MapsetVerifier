using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Rendering
{
    public class Renderer
    {
        /*
         *  Tag Methods
         */

        /// <summary> Surrounds the content with a div tag using the given class(es). </summary>
        protected static string Div(string @class, params string[] contents) => DivAttr(@class, "", contents);

        /// <summary>
        ///     Surrounds the content with a div tag using the given class(es), as well as any other attributes in the tag,
        ///     like id or data.
        /// </summary>
        protected static string DivAttr(string @class, string attr, params object[] contents) =>
            string.Concat("<div", @class.Length > 0 ? " class=\"" + @class + "\"" : "", attr, ">", string.Join("", contents), "</div>");

        /// <summary>
        ///     Surrounds the content (or reference if none exists) with an a tag using the given reference.
        ///     Does not need to target _blank, as this is done client-side.
        /// </summary>
        protected static string Link(string @ref, object content = null) => "<a href=\"" + @ref + "\">" + (content ?? @ref) + "</a>";

        /// <summary> Surrounds the content in a data attribute of a given type. </summary>
        protected static string DataAttr(string type, object content) => " data-" + type + "=\"" + Encode(content.ToString()) + "\"";

        /// <summary> Surrounds the content with a script tag. </summary>
        protected static string Script(params object[] contents) => ScriptAttr("", contents);

        /// <summary> Surrounds the content with a script tag, as well as any other attributes in the tag. </summary>
        protected static string ScriptAttr(string attr, params object[] contents) => string.Concat("<script", attr, ">", string.Join("", contents), "</script>");

        /*
         *  Tag Method Implementations
         */

        /// <summary> Surrounds the content in a tooltip data attribute. </summary>
        protected static string Tooltip(object content) => DataAttr("tooltip", content);

        /// <summary> Creates a link to the given timestamp. </summary>
        protected static string TimestampLink(object timestamp) => Link("osu://edit/" + timestamp + "\" class=\"card-instance-timestamp", timestamp);

        /// <summary> Creates a link to the given username with the same username as content. </summary>
        protected static string UserLink(object username) => Link("https://osu.ppy.sh/users/" + username, username);

        /*
         *  Complex DataAttr Implementations
         */

        /// <summary>
        ///     Combines all difficulties this issue applies to into a condition attribute
        ///     (data-condition="difficulty=1,2,3"), which is then returned.
        /// </summary>
        protected static string DifficultiesDataAttr(Issue issue)
        {
            var metadata = issue.CheckOrigin.GetMetadata() as BeatmapCheckMetadata;

            var difficulties = new List<Beatmap.Difficulty>();

            foreach (Beatmap.Difficulty difficulty in Enum.GetValues(typeof(Beatmap.Difficulty)))
                if ((metadata?.Difficulties.Contains(difficulty) ?? true) && (issue.InterpretationPairs.All(pair => pair.Key != "difficulty") || issue.InterpretationPairs.Any(pair => pair.Key == "difficulty" && pair.Value == (int)difficulty)))
                    difficulties.Add(difficulty);

            return DifficultiesDataAttr(difficulties.ToArray());
        }

        /// <summary>
        ///     Combines all difficulties into a condition attribute (data-condition="difficulty=1,2,3"), which is then
        ///     returned.
        /// </summary>
        protected static string DifficultiesDataAttr(Beatmap.Difficulty[] difficulties)
        {
            // With the condition being any difficulty, we might as well not have a condition at all.
            if (difficulties.Count() == Enum.GetValues(typeof(Beatmap.Difficulty)).Length)
                return "";

            return DataAttr("condition", "difficulty=" + string.Join(",", difficulties.Select(difficulty => (int)difficulty)));
        }

        /*
         *  Utility
         */

        /// <summary> Returns the same string but HTML encoded, meaning greater and less than signs no longer form actual tags. </summary>
        public static string Encode(string text) => text == null ? null : WebUtility.HtmlEncode(text);

        /// <summary> Returns the icon of the greatest issue level of all issues given. </summary>
        protected static string GetIcon(IEnumerable<Issue> issues)
        {
            issues = issues.ToArray();

            return issues.Any() ? GetIcon(issues.Max(issue => issue.level)) : GetIcon(Issue.Level.Check);
        }

        /// <summary> Returns the icon of the given issue level. </summary>
        protected static string GetIcon(Issue.Level level) =>
            level == Issue.Level.Problem ? "cross" :
            level == Issue.Level.Warning ? "exclamation" :
            level == Issue.Level.Minor ? "minor" :
            level == Issue.Level.Error ? "error" :
            level == Issue.Level.Check ? "check" : "info";

        /*
         *  Formatting
         */

        /// <summary> Wraps all timestamps outside of html tags into proper timestamp hyperlinks. </summary>
        protected static string FormatTimestamps(string content) =>
            Regex.Replace(content, @"(\d\d:\d\d:\d\d\d( \([\d|,]+\))?)(?![^<]*>|[^<>]*<\/)", evaluator => TimestampLink(evaluator.Value));

        /// <summary> Wraps all links outside of html tags into proper hyperlinks. </summary>
        protected static string FormatLinks(string content) =>
            Regex.Replace(content, @"(http(s)?:\/\/([a-zA-Z0-9]{1,6}\.)?[a-zA-Z0-9]{1,256}\.[a-zA-Z0-9]{1,6}(\/[a-zA-Z0-9\/\?=&@\+]+)?)(?![^<]*>|[^<>]*<\/)", evaluator => Link(evaluator.Value));

        /// <summary> Replaces all pseudo note tags into proper html tags. </summary>
        protected static string FormatNotes(string content) =>
            content.Replace("<note>", "<div class=\"note\"><div class=\"note-text\">").Replace("</note>", "</div></div>");

        protected static string FormatCode(string content) =>
            Regex.Replace(content, @"`.*?`", evaluator => Div("code", evaluator.Value.Replace("`", "")));

        protected static string FormatExceptionsInEncoded(string content) =>
            Regex.Replace(content, @"&lt;exception&gt;\s*&lt;message&gt;\s*([\S\s]*?)\s*&lt;\/message&gt;\s*&lt;stacktrace&gt;\s*([\S\s]*?)\s*&lt;\/stacktrace&gt;\s*\&lt;\/exception&gt;", evaluator => $@"
                <div
                    class=""exception-shortcut detail-shortcut shows-info""
                    data-tooltip=""Show exception info""
                    data-shown-info=""
                        <div class=&quot;exception-message&quot;>
                            &quot;{Encode(evaluator.Groups[1].Value)}&quot;
                        </div>
                        <div class=&quot;paste-separator&quot;></div>
                        <div class=&quot;exception-trace&quot;>
                            {Encode(evaluator.Groups[2].Value)}
                        </div>"">
                </div>");

        /// <summary> Replaces all pseudo image tags into proper html tags and moves them if needed. </summary>
        protected static string FormatImages(string content)
        {
            var result = content;
            result = FormatCenteredImages(result);
            result = FormatRightImages(result);

            return result;
        }

        /// <summary> Replaces all center-aligned pseudo image tags into proper html tags. </summary>
        private static string FormatCenteredImages(string content)
        {
            var regex = new Regex(@"<image>[\ (\r\n|\r|\n)]+([A-Za-z0-9\/:\.]+(\.jpg|\.png))[\ (\r\n|\r|\n)]+(.*?)[\ (\r\n|\r|\n)]+<\/image>", RegexOptions.Singleline);

            var result = content;

            foreach (Match match in regex.Matches(result))
            {
                var src = match.Groups[1].Value;
                var text = match.Groups[3].Value;

                result = regex.Replace(result, "<div class=\"image image-center\" data-text=\"" + Encode(text) + "\"><img src=\"" + src + "\"></img></div>", 1);
            }

            return result;
        }

        /// <summary> Replaces all right-aligned pseudo image tags into proper html tags and prepends them to the content. </summary>
        protected static string FormatRightImages(string content)
        {
            var regex = new Regex(@"<image-right>[\ (\r\n|\r|\n)]+([A-Za-z0-9\/:\.]+(\.jpg|\.png))[\ (\r\n|\r|\n)]+(.*?)[\ (\r\n|\r|\n)]+<\/image>", RegexOptions.Singleline);

            var result = content;
            var extractedStr = new StringBuilder();

            foreach (Match match in regex.Matches(result))
            {
                var src = match.Groups[1].Value;
                var text = match.Groups[3].Value;

                extractedStr.Append("<div class=\"image image-right\" data-text=\"" + Encode(text) + "\"><img src=\"" + src + "\"></img></div>");

                result = regex.Replace(result, "", 1);
            }

            return extractedStr + result;
        }

        /// <summary> Applies all formatting (code, links, timestamps, notes, images) to the given string. </summary>
        protected static string Format(string content)
        {
            var result = content;
            result = FormatCode(result);
            result = FormatLinks(result);
            result = FormatTimestamps(result);
            result = FormatNotes(result);
            result = FormatImages(result);
            result = FormatExceptionsInEncoded(result);

            return result;
        }
    }
}