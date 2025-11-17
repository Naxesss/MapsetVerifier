using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace MapsetVerifier.Rendering.Objects
{
    public class Series(string label, List<Vector2>? points = null, Color? color = null)
    {
        public string Label { get; } = label;
        public List<Vector2> Points { get; } = points ?? [];
        public Color Color { get; } = color ?? Color.FromArgb(255, 255, 255, 255);
    }
}