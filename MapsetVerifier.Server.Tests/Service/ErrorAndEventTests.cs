using System.Text;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Server.Filter;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using MapsetVerifier.Server.Service.OsuRuntime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

public class ErrorAndEventTests
{
    [Fact]
    public void AggregateException_WithOneCause_IsThatCause()
    {
        var error = ExceptionService.GetApiError(
            new AggregateException(
                new AggregateException(
                    new InvalidOperationException("cause", new Exception("root"))
                )
            )
        );

        Assert.Equal("cause", error.Message);
        Assert.Equal("root", error.Details);
    }

    [Fact]
    public void AggregateException_WithSeveralCauses_ListsThem()
    {
        var error = ExceptionService.GetApiError(
            new AggregateException(new Exception("first"), new Exception("second"))
        );

        Assert.Equal("first | second", error.Message);
        Assert.Equal("first", error.Details);
    }

    [Fact]
    public void UnhandledException_IsAnsweredAsAnError()
    {
        var context = new ExceptionContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            []
        )
        {
            Exception = new InvalidOperationException("broken"),
        };

        new ApiExceptionFilter().OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("broken", ((ApiError)result.Value!).Message);
    }

    [Fact]
    public void ApiError_FromAnException_HasTheRootCauseAsDetails()
    {
        Assert.Equal(
            "inner",
            ApiErrorFactory
                .FromException(new Exception("outer", new Exception("inner")), "Failed.")
                .Details
        );
        Assert.Equal(
            "deep",
            ApiErrorFactory
                .FromException(
                    new AggregateException(new AggregateException(new Exception("deep"))),
                    "Failed."
                )
                .Details
        );
        Assert.Equal(
            "Failed.",
            ApiErrorFactory.FromException(new Exception("only"), "Failed.").Message
        );
    }

    [Fact]
    public async Task Events_AreWrittenAsServerSentEvents()
    {
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;

        await SseWriter.WriteEventAsync(
            context.Response,
            "progress",
            new { CheckName = "line\nbreak", Level = Issue.Level.Warning }
        );

        // Camel case, named enums and no raw line breaks, which would end the event early.
        Assert.Equal(
            "event: progress\ndata: {\"checkName\":\"line\\nbreak\",\"level\":\"Warning\"}\n\n",
            Encoding.UTF8.GetString(body.ToArray())
        );
    }

    [Fact]
    public void LazerFiles_AreStoredByTheirHash()
    {
        Assert.Equal(
            Path.Combine("data", "files", "a", "ab", "abcdef"),
            LazerRealmService.ResolveFilePathFromHash("data", "abcdef")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("../../somewhere")]
    [InlineData("123")]
    public void LazerSet_WithAnInvalidId_IsNotMaterialized(string id)
    {
        // The id becomes part of a path, so nothing but an actual id may get there.
        Assert.Equal(
            "Invalid beatmapset id.",
            LazerBeatmapMaterializer.Materialize("data", id).ErrorMessage
        );
    }
}
