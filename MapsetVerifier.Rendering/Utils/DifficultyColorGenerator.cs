using System.Drawing;

namespace MapsetVerifier.Rendering.Utils;

// Ports https://github.com/ppy/osu-web/blob/87212089ea72cae7c6dbcde78450516181ccb96c/resources/js/utils/beatmap-helper.ts
public static class DifficultyColorGenerator
{
    private static readonly double[] Domain = [0.1, 1.25, 2, 2.5, 3.3, 4.2, 4.9, 5.8, 6.7, 7.7, 9];

    private static readonly Color[] Range =
    {
        ColorTranslator.FromHtml("#4290FB"),
        ColorTranslator.FromHtml("#4FC0FF"),
        ColorTranslator.FromHtml("#4FFFD5"),
        ColorTranslator.FromHtml("#7CFF4F"),
        ColorTranslator.FromHtml("#F6F05C"),
        ColorTranslator.FromHtml("#FF8068"),
        ColorTranslator.FromHtml("#FF4E6F"),
        ColorTranslator.FromHtml("#C645B8"),
        ColorTranslator.FromHtml("#6563DE"),
        ColorTranslator.FromHtml("#18158E"),
        ColorTranslator.FromHtml("#000000"),
    };

    public static Color GetDifficultyColor(double rating)
    {
        // When beyond defined domain, use the end-most color
        if (rating < Domain.First()) return Range.First();
        if (rating >= Domain.Last()) return Range.Last();

        // Find the surrounding domain points
        for (int i = 0; i < Domain.Length - 1; i++)
        {
            if (rating >= Domain[i] && rating <= Domain[i + 1])
            {
                double t = (rating - Domain[i]) / (Domain[i + 1] - Domain[i]);
                return InterpolateColor(Range[i], Range[i + 1], t);
            }
        }

        // Fallback (shouldn't be hit due to clamp)
        return Range[^1];
    }

    private static Color InterpolateColor(Color a, Color b, double t)
    {
        // Gamma-corrected interpolation (approximation of d3.interpolateRgb.gamma(2.2))
        const double gamma = 2.2;

        double red = Math.Pow(double.Lerp(Math.Pow(a.R / 255.0, gamma), Math.Pow(b.R / 255.0, gamma), t), 1 / gamma);
        double green = Math.Pow(double.Lerp(Math.Pow(a.G / 255.0, gamma), Math.Pow(b.G / 255.0, gamma), t), 1 / gamma);
        double blue = Math.Pow(double.Lerp(Math.Pow(a.B / 255.0, gamma), Math.Pow(b.B / 255.0, gamma), t), 1 / gamma);

        return Color.FromArgb(
            (int)Math.Round(red * 255),
            (int)Math.Round(green * 255),
            (int)Math.Round(blue * 255)
        );
    }
}
