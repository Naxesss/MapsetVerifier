using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace MapsetVerifier.Server.Middleware
{
    public class RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;
            var correlationId = context.TraceIdentifier;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                var method = request.Method;
                var path = request.Path;
                var query = request.QueryString.HasValue ? request.QueryString.Value : "";
                
                logger.LogInformation("[IN ] {Method} {Path}{Query}", method, path, query);
                var sw = Stopwatch.StartNew();
                await next(context);
                sw.Stop();
                logger.LogInformation("[OUT] {Method} {StatusCode} {Path}{Query} ({Elapsed}ms)", method, context.Response.StatusCode, path, query, sw.ElapsedMilliseconds);
            }
        }
    }

    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
            => app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
