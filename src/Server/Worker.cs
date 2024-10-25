using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsetVerifier.Framework;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Rendering;
using MapsetVerifier.Snapshots;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace MapsetVerifier.Server
{
    public class Worker : BackgroundService
    {
        private static IHubContext<SignalHub> hub;

        public Worker(IHubContext<SignalHub> hub)
        {
            Worker.hub = hub;
        }

        private static async Task LoadStart(string loadMessage)
        {
            await SendMessage("AddLoad", "Checks:" + Renderer.Encode(loadMessage));
            await SendMessage("AddLoad", "Snapshots:" + Renderer.Encode(loadMessage));
            await SendMessage("AddLoad", "Overview:" + Renderer.Encode(loadMessage));
        }

        private static async Task LoadComplete(string loadMessage)
        {
            await SendMessage("RemoveLoad", "Checks:" + Renderer.Encode(loadMessage));
            await SendMessage("RemoveLoad", "Snapshots:" + Renderer.Encode(loadMessage));
            await SendMessage("RemoveLoad", "Overview:" + Renderer.Encode(loadMessage));
        }

        public static async Task SendMessage(string key, string value)
        {
            try
            {
                await hub.Clients.All.SendAsync("ServerMessage", key, value);
            }
            catch (Exception exception)
            {
                Console.WriteLine("error; " + exception.Message);
            }
        }

        public static async Task ClientMessage(string key, string value)
        {
            switch (key)
            {
                case "RequestDocumentation":
                    try
                    {
                        var html = DocumentationRenderer.Render();
                        await SendMessage("UpdateDocumentation", html);
                    }
                    catch (Exception exception)
                    {
                        var html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Documentation:" + html);
                    }

                    break;

                case "RequestOverlay":
                    try
                    {
                        var html = OverlayRenderer.Render(value);
                        await SendMessage("UpdateOverlay", html);
                    }
                    catch (Exception exception)
                    {
                        var html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Overlay:" + html);
                    }

                    break;

                case "RequestBeatmapset":
                    try
                    {
                        LoadBeatmapSet(value);

                        Func<string, Task>[] actions =
                        {
                            RequestSnapshots,
                            RequestChecks,
                            RequestOverview
                        };

                        if (State.LoadedBeatmapSetPath != value)
                            return;

                        Parallel.ForEach(actions, action => { action(value); });
                    }
                    catch (Exception exception)
                    {
                        var html = ExceptionRenderer.Render(exception);
                        await SendMessage("UpdateException", "Checks:" + html);
                        await SendMessage("UpdateException", "Snapshots:" + html);
                        await SendMessage("UpdateException", "Overview:" + html);
                    }

                    break;
            }
        }

        private static void LoadBeatmapSet(string songFolderPath)
        {
            State.LoadedBeatmapSetPath = songFolderPath;

            Checker.OnLoadStart = LoadStart;
            Checker.OnLoadComplete = LoadComplete;

            EventStatic.OnLoadStart = LoadStart;
            EventStatic.OnLoadComplete = LoadComplete;

            State.LoadedBeatmapSet = new BeatmapSet(songFolderPath);
        }

        private static async Task RequestSnapshots(string beatmapSetPath)
        {
            try
            {
                // Beatmapset null (-1) would become ambigious with any other unsubmitted map.
                if (State.LoadedBeatmapSet.Beatmaps.FirstOrDefault()?.MetadataSettings.beatmapSetId != null)
                    Snapshotter.SnapshotBeatmapSet(State.LoadedBeatmapSet);

                if (State.LoadedBeatmapSetPath != beatmapSetPath)
                    return;

                var html = SnapshotsRenderer.Render(State.LoadedBeatmapSet);
                await SendMessage("UpdateSnapshots", html);
            }
            catch (Exception exception)
            {
                var html = ExceptionRenderer.Render(exception);
                await SendMessage("UpdateException", "Snapshots:" + html);
            }
        }

        private static async Task RequestChecks(string beatmapSetPath)
        {
            try
            {
                var issues = Checker.GetBeatmapSetIssues(State.LoadedBeatmapSet);

                if (State.LoadedBeatmapSetPath != beatmapSetPath)
                    return;

                var html = ChecksRenderer.Render(issues, State.LoadedBeatmapSet);
                await SendMessage("UpdateChecks", html);
            }
            catch (Exception exception)
            {
                var html = ExceptionRenderer.Render(exception);
                await SendMessage("UpdateException", "Checks:" + html);
            }
        }

        private static async Task RequestOverview(string beatmapSetPath)
        {
            try
            {
                if (State.LoadedBeatmapSetPath != beatmapSetPath)
                    return;

                var html = OverviewRenderer.Render(State.LoadedBeatmapSet);
                await SendMessage("UpdateOverview", html);
            }
            catch (Exception exception)
            {
                var html = ExceptionRenderer.Render(exception);
                await SendMessage("UpdateException", "Overview:" + html);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}