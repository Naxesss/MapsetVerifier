using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Files
{
    [Check]
    public class CheckUpdateValidity : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Files",
                Message = "Issues with updating or downloading.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that beatmaps can properly be downloaded and updated to their newest version.
                    <image>
                        https://i.imgur.com/7Nc9Ejr.png
                        An example of a song folder where one of the difficulties' file names are incorrect, causing it to be unable to update.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    By being unable to update a beatmap, potentially important fixes can easily be missed out on. This mostly 
                    affects players who download the map in qualified, as it is more visible to the public while not necessarily 
                    being in its final version.
                    <br><br>
                    The name of the file seems to determine how osu initially checks the map for updates. This is then presumably 
                    stored in some local database because deleting and re-downloading doesn't seem to affect it. So if you 
                    already had the map when the file name was correct, it may properly update for you while showing ""not 
                    submitted"" for others downloading it for the first time.
                    <br><br>
                    For Windows 10 users, file paths longer than 260 characters cannot properly be unzipped by the game and 
                    simply vanish instead. This varies between users based on where on their computer they put osu!, but
                    for the sake of consistency, we'll simply arbitrarily make 130 characters the limit.
                    <image>
                        https://i.imgur.com/PO8eKvZ.png
                        A file name longer than 130 characters, caused by a combination of a long title and a long difficulty name.
                    </image>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "File Size",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" will cut any update at the 1 MB mark ({1} MB), causing objects to disappear.", "path", "file size").WithCause("A .osu file exceeds 1 MB in file size.")
                },

                {
                    "Wrong Format",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" should be named \"{1}\" to receive updates.", "file name", "artist - title (creator) [version].osu").WithCause("A .osu file is not named after the mentioned format using its respective properties.")
                },

                {
                    "Too Long Name",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" has a file name longer than 130 characters ({1}), which causes the .osz to fail " + "to unzip for some users. Consider truncating the artist, title, and/or difficulty name fields " + "where it makes sense to do so.", "path", "length").WithCause("A .osu file has a file name longer than 130 characters.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var songFilePath in beatmapSet.SongFilePaths)
            {
                var filePath = songFilePath[(beatmapSet.SongPath.Length + 1)..];
                var fileName = filePath.Split(new[] { '/', '\\' }).Last();

                if (fileName.Length > 130)
                    yield return new Issue(GetTemplate("Too Long Name"), null, filePath, fileName.Length);

                if (!fileName.EndsWith(".osu"))
                    continue;

                var beatmap = beatmapSet.Beatmaps.First(otherBeatmap => otherBeatmap.MapPath == filePath);

                if (beatmap.GetOsuFileName().ToLower() != fileName.ToLower())
                    yield return new Issue(GetTemplate("Wrong Format"), null, fileName, beatmap.GetOsuFileName());

                // Updating .osu files larger than 1 mb will cause the update to stop at the 1 mb mark
                var fileInfo = new FileInfo(songFilePath);
                var mb = fileInfo.Length / Math.Pow(1024, 2);

                if (mb > 1)
                    yield return new Issue(GetTemplate("File Size"), null, filePath, $"{mb:0.##}");
            }
        }
    }
}