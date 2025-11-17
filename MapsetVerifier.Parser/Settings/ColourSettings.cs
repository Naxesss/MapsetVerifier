using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MapsetVerifier.Parser.Settings
{
    public class ColourSettings
    {
        /*
            [Colours]
            Combo1 : 129,176,182
            Combo2 : 255,40,40
            Combo3 : 170,28,255
            Combo4 : 209,103,39
            Combo5 : 53,189,255
            Combo6 : 205,95,95
            Combo7 : 162,124,171
            Combo8 : 200,139,111
         */
        // Combo# : r,g,b

        /// <summary> Starts at index 0, so combo colour 1 is the 0th element in the list. </summary>
        public List<Vector3> combos;

        public Vector3? sliderBorder; // The edges of the slider

        // Optional
        public Vector3? sliderTrackOverride; // The body of the slider

        public ColourSettings(string[] lines)
        {
            combos = ParseColours(GetCombos(lines)).ToList();

            var sliderTrackOverrideValue = GetValue(lines, "SliderTrackOverride");
            var sliderBorderValue = GetValue(lines, "SliderBorder");
            // There is also a "SliderBody" property documented, but it seemingly does nothing.

            if (sliderTrackOverrideValue != null)
                sliderTrackOverride = ParseColour(sliderTrackOverrideValue);

            if (sliderBorderValue != null)
                sliderBorder = ParseColour(sliderBorderValue);
        }

        private static IEnumerable<string> GetCombos(string[] lines)
        {
            foreach (var line in lines)
                if (line.StartsWith("Combo"))
                    yield return line.Split(':')[1].Trim();
        }

        private static string? GetValue(string[] lines, string key)
        {
            var line = lines.FirstOrDefault(otherLine => otherLine.StartsWith(key));

            return line?.Substring(line.IndexOf(':') + 1).Trim();
        }

        private static Vector3 ParseColour(string colourString)
        {
            var r = float.Parse(colourString.Split(',')[0].Trim(), CultureInfo.InvariantCulture);
            var g = float.Parse(colourString.Split(',')[1].Trim(), CultureInfo.InvariantCulture);
            var b = float.Parse(colourString.Split(',')[2].Trim(), CultureInfo.InvariantCulture);

            return new Vector3(r, g, b);
        }

        private static IEnumerable<Vector3> ParseColours(IEnumerable<string> colourStrings)
        {
            foreach (var colourString in colourStrings)
            {
                var r = float.Parse(colourString.Split(',')[0].Trim(), CultureInfo.InvariantCulture);
                var g = float.Parse(colourString.Split(',')[1].Trim(), CultureInfo.InvariantCulture);
                var b = float.Parse(colourString.Split(',')[2].Trim(), CultureInfo.InvariantCulture);

                yield return new Vector3(r, g, b);
            }
        }
    }
}