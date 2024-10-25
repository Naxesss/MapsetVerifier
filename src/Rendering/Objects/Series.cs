using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace MapsetVerifier.Rendering.Objects
{
    public class Series
    {
        public Series(string label, List<Vector2> points = null, Color? color = null)
        {
            Label = label;
            Points = points ?? new List<Vector2>();
            Color = color ?? Color.FromArgb(255, 255, 255, 255);
        }

        public string Label { get; }
        public List<Vector2> Points { get; }
        public Color Color { get; }
    }
}