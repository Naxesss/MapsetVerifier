using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsetVerifier.Parser.Settings
{
    public class MetadataSettings
    {
        public string artist;
        public string artistUnicode;

        public ulong? beatmapId;
        public ulong? beatmapSetId;

        public string creator;
        public string source;

        public string tags;
        /*
            Title:Yuumeikyou o Wakatsu Koto
            TitleUnicode:幽明境を分かつこと
            Artist:Diao Ye Zong feat. Kushi
            ArtistUnicode:凋叶棕 feat. Φ串Φ
            Creator:Sakurauchi Riko
            Version:Sakura no Hana
            Source:東方妖々夢　～ Perfect Cherry Blossom.
            Tags:phyloukz 凋叶棕 さいぎょうじ ゆゆこ Saigyouji Yuyuko 幽雅に咲かせ、墨染の桜　～ Border of Life 東方妖々梦 ～ Perfect Cherry Blossom.
            BeatmapID:1541385
            BeatmapSetID:730355
         */
        // key:value

        public string title;
        public string titleUnicode;
        public string version;

        public MetadataSettings(string[] lines)
        {
            // unlike hitobjects metadata settings gets the whole section and not line by line as code

            title = GetValue(lines, "Title");
            titleUnicode = GetValue(lines, "TitleUnicode") ?? title;
            artist = GetValue(lines, "Artist");
            artistUnicode = GetValue(lines, "ArtistUnicode") ?? artist;

            creator = GetValue(lines, "Creator");
            version = GetValue(lines, "Version");
            source = GetValue(lines, "Source") ?? "";
            tags = GetValue(lines, "Tags") ?? "";

            // check to see if the ids are even there (don't exist in lower osu file versions, and aren't set on non-published maps)
            beatmapId = (GetValue(lines, "BeatmapID") ?? "0") == "0" ? (ulong?)null : ulong.Parse(GetValue(lines, "BeatmapID"));

            beatmapSetId = (GetValue(lines, "BeatmapSetID") ?? "-1") == "-1" ? (ulong?)null : ulong.Parse(GetValue(lines, "BeatmapSetID"));
        }

        private string GetValue(string[] lines, string key)
        {
            var line = lines.FirstOrDefault(otherLine => otherLine.StartsWith(key));

            if (line == null)
                return null;

            return line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 1).Trim();
        }

        /// <summary> Returns the same string lowercase and filtered from characters disabled in file names. </summary>
        public string GetFileNameFiltered(string str) =>
            str.Replace("/", "").Replace("\\", "").Replace("?", "").Replace("*", "").Replace(":", "").Replace("|", "").Replace("\"", "").Replace("<", "").Replace(">", "");

        /// <summary> Returns the tag which covers the given word, if any, otherwise null. </summary>
        /// <param name="searchWord"> The search word which we want a tag covering, cannot contain spaces. </param>
        public string GetCoveringTag(string searchWord)
        {
            if (searchWord.Contains(" "))
                throw new ArgumentException($"`searchWord` cannot contain whitespace characters, was given \"{searchWord}\".");

            foreach (var tagWord in tags.ToLower().Split(" "))
                if (tagWord.Contains(searchWord.ToLower()))
                    return tagWord;

            return null;
        }

        /// <summary>
        ///     Returns all space-separated strings from the given search term which are not covered by tags
        ///     (e.g. "One two" with tags "oneth" would return `{ "two" }`).
        /// </summary>
        /// <param name="searchTerm"> The search term which we want tags covering. </param>
        public IEnumerable<string> GetMissingWordsFromTags(string searchTerm)
        {
            foreach (var searchWord in searchTerm.Split(" "))
                if (GetCoveringTag(searchWord) == null)
                    yield return searchWord;
        }

        /// <summary>
        ///     Returns whether all space-separated parts of the given search term is covered by tags
        ///     (e.g. "Skull Kid" would be covered by "skull_kid").
        /// </summary>
        /// <param name="searchTerm"> The search term which we want tags covering. </param>
        public bool IsCoveredByTags(string searchTerm) => !GetMissingWordsFromTags(searchTerm).Any();
    }
}