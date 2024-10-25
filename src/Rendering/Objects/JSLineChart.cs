using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MapsetVerifier.Rendering.Objects
{
    public class JSLineChart : JSChart<Point, Series>
    {
        /*
         *  Attributes & Constructors
         */

        public JSLineChart(LineChart lineChart) : base(lineChart) { }

        protected override IDataSet ToDataSet(Series series) =>
            new DataSet
            {
                label = series.Label,
                data = series.Points.Select(vec => new Point(vec)).ToList(),
                backgroundColor = $"rgba({series.Color.R}, {series.Color.G}, {series.Color.B}, {series.Color.A / 255f})",
                borderColor = $"rgba({series.Color.R}, {series.Color.G}, {series.Color.B}, {series.Color.A / 255f})",
                fill = false
            };
        
        // https://www.chartjs.org/docs/latest/getting-started/usage.html
        /*
         *  Json Seralization Mapping
         *  Note: Capitalization of variables matter; they must be lowercased.
         */
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private struct DataSet : IDataSet
        {
            public string label { get; set; }
            public List<Point> data { get; set; }
            public string backgroundColor { get; set; }
            public string borderColor { get; set; }
            public bool fill { get; set; }
        }
    }
}