using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MapsetVerifier.Server
{
    public class SignalHub : Hub
    {
        // ReSharper disable once UnusedMember.Global
        // This method is called externally by the client.
        public Task ClientMessage(string key, string value)
        {
            // SignalR hubs buffer handling of requests, meaning they can't be done in parallel.
            // By creating a new thread and forwarding the message in that thread to a background service,
            // multiple requests can be handled at the same time and cancel each other.
            new Thread(async () => await Worker.ClientMessage(key, value)).Start();

            return Task.CompletedTask;
        }
    }
}