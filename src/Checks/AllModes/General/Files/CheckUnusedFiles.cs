using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.General.Files
{
    [Check]
    public class CheckUnusedFiles : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Files",
                Message = "Unused files.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Reducing the overall file size of beatmapset folders and preventing potentially malicious files 
                    from reaching the player."
                    },
                    {
                        "Reasoning",
                        @"
                    Having useless files in the folder that few players will look at in no way contributes to the gameplay experience 
                    and is a waste of resources. Unlike things like pointless bookmarks and green lines, files typically take up a way 
                    more noticeable amount of space.
                    <note>
                        For comparison, you'd need about 10 000 bookmarks, if not more, to match the file size of 
                        a regular hit sound.
                    </note>
                    <br \>
                    Official content distributing malicious .exe files or similar would also not reflect very well upon the game."
                    },
                    {
                        "Scorebar Ki-Danger & Marker",
                        @"
                    `scorebar-ki`, `scorebar-kidanger`, and `scorebar-kidanger2`, act strangely when `scorebar-marker` is missing.
                    The latter can simply be a blank file as it does not show up in-game for osu-stable regardless.
                    <image>
                        https://i.imgur.com/TnXmgt9.png
                        Spreadsheet over which scorebar elements are used. Only consistent regardless of user settings when both
                        marker and ki elements exist.
                    </image>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Unused",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\"", "path").WithCause("A file in the song folder is not used in any of the .osu or .osb files. " + "Includes unused .osb files. Ignores thumbs.db.")
                },

                {
                    "Unused Overridden",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\", likely due to \"{1}\" being used instead.", "path", "path with different extension").WithCause("Same as the other check, but where a file with the same name is used.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var songFilePath in beatmapSet.SongFilePaths)
            {
                var filePath = songFilePath[(beatmapSet.SongPath.Length + 1)..];
                var fileNameWithExtension = filePath.Split(new[] { '/', '\\' }).Last().ToLower();

                if (beatmapSet.IsFileUsed(filePath) || fileNameWithExtension == "thumbs.db")
                    continue;

                var fileName = PathStatic.ParsePath(fileNameWithExtension, true);
                var otherFilePath = beatmapSet.HitSoundFiles.FirstOrDefault(file => file.StartsWith(fileName + "."));

                if (otherFilePath != null)
                    yield return new Issue(GetTemplate("Unused Overridden"), null, filePath, otherFilePath);
                else
                    yield return new Issue(GetTemplate("Unused"), null, filePath);
            }
        }
    }
}