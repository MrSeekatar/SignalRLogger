using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using static System.Console;

var urlArg = new Option<string>("--url", "URL");
urlArg.AddAlias("-u");
urlArg.IsRequired = true;

var jwtArg = new Option<string>("--jwt", "JWT");
jwtArg.AddAlias("-j");

var msgArg = new Option<string>("--msg", "Name of SignalR Message");
msgArg.AddAlias("-m");
msgArg.IsRequired = true;

var rootCommand = new RootCommand("Connect and dump SignalR messages")
{
    urlArg,
    jwtArg,
    msgArg
};

rootCommand.SetHandler((invocationContext) => {
    var url = invocationContext.ParseResult.GetValueForOption(urlArg);
    var jwt = invocationContext.ParseResult.GetValueForOption(jwtArg) ?? "";
    var msg = invocationContext.ParseResult.GetValueForOption(msgArg);

    WriteLine($"URL: {url}, you can use as --url are:");
    WriteLine("   local FF       https://localhost:44300/socket");
    WriteLine("   local platform https://localhost:51025/socket");
    WriteLine("   dev FF         https://api-dev.loyalhealth.com/features/socket");
    WriteLine("   dev platform   https://app-dev.loyalhealth.com/socket");

    HeyListen(url!, jwt!, msg!).GetAwaiter().GetResult();
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

async Task HeyListen(string url, string jwt, string msg)
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


    connection.On(msg, (object payload) =>
    {
        WriteLine($"Received '{msg}' of type '{payload.GetType().Name}': {payload}");
    });

    await connection.StartAsync();

    WriteLine($"Connected to SignalR server at {url} listening for {msg}");

    await Task.Delay(-1);

    await connection.StopAsync();
}
