using MapsetVerifier.Rendering.Objects;

namespace MapsetVerifier.Rendering
{
    public class ChartRenderer : OverviewRenderer
    {
        protected static string Render(LineChart chart)
        {
            var jsChart = new JSLineChart(chart);

            return
                RenderField(chart.Title,
                    Div("chart-container", $"<canvas id=\"{jsChart.canvasId}\"></canvas>",
                        Script($"renderLineChart(\"{jsChart.canvasId}\", {jsChart.Serialize()})")));
        }
    }
}