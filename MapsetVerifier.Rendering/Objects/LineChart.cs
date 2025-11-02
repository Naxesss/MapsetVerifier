using System.Collections.Generic;

namespace MapsetVerifier.Rendering.Objects
{
    public class LineChart : Chart<Series>
    {
        public LineChart(string title, string xLabel, string yLabel, List<Series> data = null) : base(title, data)
        {
            XLabel = xLabel;
            YLabel = yLabel;
        }

        public string XLabel { get; }
        public string YLabel { get; }
    }
}