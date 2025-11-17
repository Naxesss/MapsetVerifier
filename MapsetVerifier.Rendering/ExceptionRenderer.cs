using System;
using MapsetVerifier.Framework.Objects;

namespace MapsetVerifier.Rendering
{
    public class ExceptionRenderer : Renderer
    {
        public static string Render(Exception exception)
        {
            // Only the innermost exception is important, MapsetVerifier runs a lot of things in
            // parallel so many exceptions will be aggregates and not provide any useful information.
            var printedException = exception;

            while (printedException.InnerException != null && printedException is AggregateException)
                printedException = printedException.InnerException;

            var printedCheckBox = printedException.Data["Check"] != null ? DocumentationRenderer.RenderCheckBox((Check) printedException.Data["Check"]!) : null;

            return
                Div("exception",
                    Div("exception-message",
                        Encode(printedException.Message)),
                    Div("exception-check",
                        printedCheckBox),
                    Div("exception-trace",
                        Encode(printedException.StackTrace)?.Replace("\r\n", "<br>")));
        }
    }
}