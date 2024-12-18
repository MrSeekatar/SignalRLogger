# SignalR Logger

## Description

This project is a simple SignalR Logger that dumps pushed messages from the server to the console.

## Usage

It takes three parameters. Running it with none will show the help message.

```plaintext
Description:
  Connect and dump SignalR messages

Usage:
  signalRLogger [options]

Options:
  -u, --url <url> (REQUIRED)  URL
  -j, --jwt <jwt>             JWT
  -m, --msg <msg> (REQUIRED)  Name of SignalR Message
  --version                   Show version information
  -?, -h, --help              Show help and usage information
```

To pass parameters with dotnet run, add parameters after a double dash: `dotnet run -- --url ...`

When it gets a message, it dumps it to the console like this

```text
Received ReceiveMessage of type JsonElement: {"timestamp":"2024-12-18T15:22:39.312549-05:00","senderUsername":"Server","title":"My Title","text":"Hi","type":"info"}
```

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

[MIT](https://choosealicense.com/licenses/mit/)
