// Contains the implementation of SignalR for communication with the server

const signalR = require('@aspnet/signalr');

/// If a connection attempt fails, retry in this many seconds.
const retryMiliseconds = 500;

/// Creates the hub connection
var connection = new signalR.HubConnectionBuilder()
	.withUrl("http://localhost:5000/mapsetverifier/signalr",
	{
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets
    })
	.configureLogging(signalR.LogLevel.Information)
	.build();

module.exports =
{
	/// Starts a new connection
	connectAnew: function()
	{
		connection = new signalR.HubConnectionBuilder()
			.withUrl("http://localhost:5000/mapsetverifier/signalr",
			{
			  skipNegotiation: true,
			  transport: signalR.HttpTransportType.WebSockets
			})
			.configureLogging(signalR.LogLevel.Information)
			.build();
			
		start();
	},
	
	/// Sends a message to the server
	send: function(key, value)
	{
		log("Sending message with key \"" + key + "\", and value \"" + value + "\".");
		
		connection.invoke("ClientMessage", key, value)
		.catch(err =>
		{
			log("Failed sending message, retrying in 1 second; " + (err != null ? err.message : "No response from server."));
			setTimeout(() =>
			{
				module.exports.send(key, value);
			}, retryMiliseconds);
		});
	},
	
	requestDocumentation: function()
	{
		module.exports.send("RequestDocumentation", "");
	},
	
	requestOverlay: function(value)
	{
		module.exports.send("RequestOverlay", value);
	},
	
	requestBeatmapset: function(value)
	{
		module.exports.send("RequestBeatmapset", value);
	}
}

/// Sends a console log with SignalR in front to mark the origin
function log(message)
{
	//console.log("SignalR | " + message);
}

/// Starts the connection and logs result, retries if failed
function start()
{
	log("Connecting");
	connection.start()
	.then(() =>
	{
		log("Connected");
		module.exports.send("Connected", "");
		
		document.dispatchEvent(new CustomEvent("SignalR Connected",
		{
			detail:
			{
				connection: connection
			}
		}));
	})
	.catch(err =>
	{
		log("Error connecting, retrying in 1 second; " + (err != null ? err.message : "No response from server."));
		setTimeout(() => start(), retryMiliseconds);
	});
};

/// Attempts to reconnect upon the connection closing
connection.onclose(async () =>
{
    await start();
});

/// Logs messages received from the server
connection.on("ServerMessage", (key, value) =>
{
	log("Received message with key \"" + key + "\", and value \"" + value + "\".");
});

start();