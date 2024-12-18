using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using static System.Console;

var urlArg = new Option<string>("--url", "URL");
urlArg.AddAlias("-u");

var jwtArg = new Option<string>("--jwt", "JWT");
jwtArg.AddAlias("-j");

var rootCommand = new RootCommand("Connect and dump SignalR messages")
{
    urlArg,
    jwtArg,
};

rootCommand.SetHandler((invocationContext) => {
    var url = invocationContext.ParseResult.GetValueForOption(urlArg) ?? "https://localhost:51025/socket";
    var jwt = invocationContext.ParseResult.GetValueForOption(jwtArg) ?? "";
    WriteLine($"URL: {url}, you can use as --url are:");
    WriteLine("   local FF       https://localhost:44300/socket");
    WriteLine("   local platform https://localhost:51025/socket");
    WriteLine("   dev FF         https://api-dev.loyalhealth.com/features/socket");
    WriteLine("   dev platform   https://app-dev.loyalhealth.com/socket");

    HeyListen(url, jwt).GetAwaiter().GetResult();
});

try
{
    return await rootCommand.InvokeAsync(args);
}
catch (ArgumentException e)
{
    WriteLine(e.Message);
    return 99;
}

async Task HeyListen(string url, string jwt)
{
    WriteLine($"Using JWT: {jwt}");
    WriteLine($"Connecting to SignalR server at {url}");

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
                        (sender, certificate, chain, sslPolicyErrors) => true;
                return message;
            };
            if (!string.IsNullOrEmpty(jwt))
                options.AccessTokenProvider = () => Task.FromResult(jwt);
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
        WriteLine($"{nameof(addNewMessage)}: {a.Client.Name}: {a.StatusMessage} (Type: {a.Client.ClientType} Status: {a.Client.OnboardingStatus})");
    });

    connection.On<FeatureFlagsUpdate>("cacheUpdate", (a) =>
    {
        WriteLine($"{nameof(FeatureFlagsUpdate)}: {a.ToString()}");
    });

    connection.On<HealthCheckSignalRMessage>("healthCheckMessage", (a) =>
    {
        WriteLine($"{nameof(HealthCheckSignalRMessage)}: {a.Number}: {a.Status}");
    });

    connection.On<ForceLogoutMessage>("forceLogoutMessage", (a) =>
    {
        WriteLine($"{nameof(ForceLogoutMessage)}: {a.UserId} by {a.CallingUserId}");
    });

    await connection.StartAsync();

    WriteLine($"Connected to SignalR server at {url}");

    await Task.Delay(-1);

    await connection.StopAsync();
}

class Client {
    public string Name { get; set; } = "";
    // actually enums, but should be strings in JSON
    public string OnboardingStatus  { get; set; } = "";
    public string ClientType  { get; set; } = "";
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