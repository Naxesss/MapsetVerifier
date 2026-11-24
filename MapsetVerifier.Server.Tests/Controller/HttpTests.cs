using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MapsetVerifier.Framework;
using MapsetVerifier.Server.Service;
using MapsetVerifier.Snapshots.Store;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace MapsetVerifier.Server.Tests.Controller;

/// <summary> The server as the app reaches it: over HTTP, on a free port. </summary>
public sealed class HttpTests : IAsyncLifetime
{
    private readonly SongFolder song = new();
    private readonly SongFolder store = new();
    private readonly HttpClient client = new();
    private readonly IHost host = HostBuilderFactory.CreateHost(["--urls=http://127.0.0.1:0"]);

    public async Task InitializeAsync()
    {
        SnapshotStore.ConfigurePath(store.Path, "externals");
        CheckRunHistoryService.ConfigurePath(store.Path, "externals");
        Checker.ConfigureCustomChecksPath(store.Path, "externals");
        CheckerRegistry.ClearChecks();

        await host.StartAsync();

        client.BaseAddress = new Uri(
            host.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.Single()
        );
    }

    public async Task DisposeAsync()
    {
        await host.StopAsync();
        host.Dispose();
        client.Dispose();
        song.Dispose();
        store.Dispose();
    }

    private Task<HttpResponseMessage> Post(string path, object body) =>
        client.PostAsJsonAsync(path, body);

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Health_IsUp()
    {
        Assert.Equal("up", await client.GetStringAsync("health"));
    }

    /// <summary> Everything the app calls. A change here needs one there. </summary>
    [Fact]
    public void Routes_AreThoseOfTheApp()
    {
        var routes = host
            .Services.GetRequiredService<IEnumerable<EndpointDataSource>>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint =>
                endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.SingleOrDefault()
                + " "
                + endpoint.RoutePattern.RawText
            )
            .Distinct()
            .Order(StringComparer.Ordinal);

        Assert.Equal(
            [
                "DELETE beatmap/checkRunHistory",
                "GET audio/stream",
                "GET beatmap",
                "GET beatmap/image",
                "GET beatmap/lazer",
                "GET beatmap/lazer/current",
                "GET beatmap/lazer/dataDir",
                "GET beatmap/lazer/image",
                "GET beatmap/runningClients",
                "GET beatmap/songsFolder",
                "GET beatmap/stable/current",
                "GET documentation/beatmap",
                "GET documentation/general",
                "GET documentation/{id:int}/details",
                "GET health",
                "GET metadata/file",
                "GET plugins",
                "GET ranking-criteria",
                "GET ranking-criteria/pages/{key}",
                "GET ranking-criteria/statements",
                "GET video/stream",
                "POST audio/analyze",
                "POST audio/frequency",
                "POST audio/spectrogram",
                "POST beatmap-analysis/analyze",
                "POST beatmap-analysis/difficulty",
                "POST beatmap-analysis/objects",
                "POST beatmap/info",
                "POST beatmap/lazer/materialize",
                "POST beatmap/runCheck/override",
                "POST beatmap/runChecks",
                "POST beatmap/runChecks/stream",
                "POST metadata/analyze",
                "POST metadata/storyboard",
                "POST plugins/reload",
#if DEBUG
                // Writes to the source, so it must not ship.
                "POST ranking-criteria/curation/review",
#endif
                "POST snapshot/capture",
                "POST snapshot/compare",
                "POST snapshot/history",
                "POST snapshot/pin",
                "POST snapshot/window",
                "POST video/analyze",
#if DEBUG
                "PUT ranking-criteria/curation",
#endif
            ],
            routes
        );
    }

    [Fact]
    public async Task UnknownRoutesAndMethods_AreRefused()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("nothing")).StatusCode);
        Assert.Equal(
            HttpStatusCode.MethodNotAllowed,
            (await client.DeleteAsync("health")).StatusCode
        );
    }

    [Fact]
    public async Task Json_IsInCamelCase_WithNamedEnums()
    {
        song.Write("a.osu", SongFolder.Osu());

        var response = await Post("beatmap-analysis/analyze", new { beatmapSetFolder = song.Path });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);

        var json = await Json(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Equal("Hard", json.GetProperty("statistics")[0].GetProperty("version").GetString());
        Assert.Equal("Standard", json.GetProperty("statistics")[0].GetProperty("mode").GetString());
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("[1]")]
    public async Task BrokenBodies_AreRefused(string body)
    {
        var response = await client.PostAsync(
            "beatmap-analysis/analyze",
            new StringContent(body, Encoding.UTF8, "application/json")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary> As a website may send without asking first. </summary>
    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/x-www-form-urlencoded")]
    public async Task OtherContentTypes_AreRefused(string type)
    {
        var response = await client.PostAsync(
            "beatmap-analysis/analyze",
            new StringContent("{}", Encoding.UTF8, type)
        );

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task MissingFolders_AreAnsweredWithAnError()
    {
        var response = await Post(
            "beatmap-analysis/analyze",
            new { beatmapSetFolder = Path.Combine(song.Path, "missing") }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("", (await Json(response)).GetProperty("message").GetString());
    }

    private async Task<string?> AllowedOrigin(string origin, bool preflight)
    {
        using var request = new HttpRequestMessage(
            preflight ? HttpMethod.Options : HttpMethod.Get,
            preflight ? "beatmap-analysis/analyze" : "health"
        );
        request.Headers.Add("Origin", origin);

        if (preflight)
        {
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type");
        }

        using var response = await client.SendAsync(request);

        return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
            ? values.Single()
            : null;
    }

    /// <summary> The app is opened from a file, or from the development server. </summary>
    [Theory]
    [InlineData("null", true)]
    [InlineData("http://localhost:1420", true)]
    [InlineData("http://127.0.0.1:5005", true)]
    [InlineData("https://example.com", false)]
    [InlineData("http://localhost.example.com", false)]
    [InlineData("http://192.168.0.2", false)]
    [InlineData("https://osu.ppy.sh", false)]
    public async Task OnlyTheAppItself_MayReadAnswers(string origin, bool expected)
    {
        foreach (var preflight in new[] { false, true })
            Assert.Equal(expected ? origin : null, await AllowedOrigin(origin, preflight));
    }

    [Theory]
    [InlineData("not an origin")]
    [InlineData("")]
    [InlineData("file:")]
    public void OtherOrigins_AreNotTheApp(string origin)
    {
        Assert.False(HostBuilderFactory.IsOwnOrigin(origin));
    }

    [Fact]
    public async Task StreamedChecks_ArriveAsEvents()
    {
        song.Write("a.osu", SongFolder.Osu());

        var response = await Post("beatmap/runChecks/stream", new { folder = song.Path });
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);

        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("event: ", text);
        Assert.EndsWith("\n\n", text);
    }

    [Fact]
    public async Task Documentation_AndSwagger_AreServed()
    {
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync("documentation/general")).StatusCode
        );
        Assert.Contains("/health", await client.GetStringAsync("swagger/v1/swagger.json"));
    }

    [Fact]
    public async Task Queries_AreReadAndLogged()
    {
        Assert.Equal("up", await client.GetStringAsync("health?a=b"));
    }
}
