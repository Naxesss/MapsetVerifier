using System.Collections.Generic;

namespace MapsetVerifier.Rendering.Objects
{
    public class LineChart(string title, string xLabel, string yLabel, List<Series> data)
        : Chart<Series>(title, data)
    {
        public string XLabel { get; } = xLabel;
        public string YLabel { get; } = yLabel;
    }
}