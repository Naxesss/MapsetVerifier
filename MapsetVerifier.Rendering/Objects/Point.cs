using System.Numerics;

namespace MapsetVerifier.Rendering.Objects
{
    public class Point
    {
        public Point(Vector2 vec)
        {
            x = vec.X;
            y = vec.Y;
        }
        /*
         *  Note: Capitalization of variables matter for serialization; they must be lowercased.
         */

        public double x { get; set; }
        public double y { get; set; }
    }
}