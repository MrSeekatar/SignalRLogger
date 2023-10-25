using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

var url = "https://localhost:51025/socket"; // local Platform
// local FF "https://localhost:44300/socket"; // local Platform
// dev FF  https://api-dev.loyalhealth.com/features/socket
// dev platform  https://api-dev.loyalhealth.com/features/socket
if (args.Length < 1)
{
    Console.WriteLine("Using default url of https://localhost:51025/socket");
}
else
{
    url = args[0];
}
Console.WriteLine($"Connecting to SignalR server at {url}");

var connection = new HubConnectionBuilder()
    .WithUrl(url, options =>
    {
        options.SkipNegotiation = true;
        options.Transports = HttpTransportType.WebSockets;
        options.HttpMessageHandlerFactory = (message) =>
        {
            // always verify the SSL certificate for self signed
            if (message is HttpClientHandler clientHandler)
                clientHandler.ServerCertificateCustomValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => { return true; };
            return message;
        };
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information); // set lower if you want to see more
    })
    .WithAutomaticReconnect()
    .Build();


connection.On<addNewMessage>("addNewMessage", (a) =>
{
    Console.WriteLine($"Got {nameof(addNewMessage)}");
    Console.WriteLine($"{a.Client.Name}: {a.StatusMessage}");
});

connection.On<FeatureFlagsUpdate>("cacheUpdate", (a) =>
{
    Console.WriteLine("Got FeatureFlagsUpdate");
    Console.WriteLine(a.ToString());
});

connection.On<HealthCheckSignalRMessage>("healthCheckMessage", (a) =>
{
    Console.WriteLine("Got HealthCheckSignalRMessage");
    Console.WriteLine($"{a.Number}: {a.Status}");
});

connection.On<ForceLogoutMessage>("forceLogoutMessage", (a) =>
{
    Console.WriteLine("Got ForceLogoutMessage");
    Console.WriteLine($"{a.UserId} by {a.CallingUserId}");
});

await connection.StartAsync();

Console.WriteLine($"Connected to SignalR server at {url}");

await Task.Delay(-1);

await connection.StopAsync();

class Client {
    public string Name { get; set; } = "";
}
class addNewMessage
{
    public Client Client { get; set; } = null!;
    public string StatusMessage { get; set; } = "";
}

public class HealthCheckSignalRMessage
{
    public Number Number { get; set; } = Number.Zero;
    public string Status { get; set; } = "";
}

public class FeatureFlagsUpdate
{
    /// <summary>
    /// The name of the feature Flag message name
    /// </summary>
    public const string MessageName = "FeatureFlagsUpdate";

    /// <summary>
    /// type of change for the name.
    /// </summary>
    public enum UpdateType
    {
        /// <summary>
        /// An existing Flag was updated, which may effect if it is enabled for your context
        /// </summary>
        Update,
        /// <summary>
        /// New Flag
        /// </summary>
        Add,
        /// <summary>
        /// A Flag was removed
        /// </summary>
        Delete
    }

    /// <summary>
    ///
    /// </summary>
    public FeatureFlagsUpdate()
    {

    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="updates"></param>
    public FeatureFlagsUpdate(IDictionary<string, UpdateType> updates)
    {
        Updates = updates;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="updateType"></param>
    public FeatureFlagsUpdate(string name, UpdateType updateType )
    {
        Updates = new Dictionary<string, UpdateType>() { { name, updateType } };
    }

    /// <summary>
    /// list of feature Flags
    /// </summary>
    public IDictionary<string, UpdateType> Updates { get; set; } = new Dictionary<string, UpdateType>();

    public override string ToString() {
        return $"Updates count: {Updates.Count} {Updates.Keys.FirstOrDefault()} => {Updates.Values.FirstOrDefault()}";
    }
}

public class ForceLogoutMessage
{
    public int CallingUserId { get; set; }
    public int UserId { get; set; }
}

public enum Number
{
    Zero,
    One
}