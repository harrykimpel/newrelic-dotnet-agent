// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

// USAGE SocketMFADriver [hostname] [port]
// Default hostname is localhost and default port is 8123
var hostName = args.Length > 0 ? args[0] : "localhost";
var port = args.Length > 1 ? int.Parse(args[1]) : 8123;

try
{
    Console.WriteLine("SocketMFA Driver");
    Console.WriteLine($"Process Info: {Process.GetCurrentProcess().ProcessName} {Process.GetCurrentProcess().Id}");

    Console.WriteLine($"Connecting to SocketMultifunctionApplicationCore at localhost:{port}");
    using var client = new TcpClient(hostName, port);
    await using var stream = client.GetStream();

    var shouldExit = false;
    while (!shouldExit)
    {
        Console.Write($"{DateTime.Now.ToLongTimeString()} > ");
        var command = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(command))
        {
            // Delay checking for next command for when there's a delay
            // receiving commands such as waiting for log line(s) before shutdown.
            await Task.Delay(TimeSpan.FromSeconds(1));
            continue;
        }

        Console.WriteLine();
        if (command.Equals("exit", StringComparison.OrdinalIgnoreCase) || command.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Terminating.");
            shouldExit = true;
        }

        await SendCommandAsync(command, stream);
        await GetResponseAsync(stream);
    }

    client.Close();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
return;

static async Task SendCommandAsync(string command, NetworkStream stream)
{
    var data = System.Text.Encoding.ASCII.GetBytes(command);
    await stream.WriteAsync(data, 0, data.Length);
}

static async Task GetResponseAsync(NetworkStream stream)
{
    var response = new StringBuilder();
    var buffer = new byte[1024];
    int bytesRead;
    string terminator = "~END~";
    bool endReceived = false;

    while (!endReceived && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        response.Append(System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead));
        if (response.ToString().Contains(terminator))
        {
            endReceived = true;
            response = response.Remove(response.ToString().IndexOf(terminator), terminator.Length);
        }
    }

    Console.WriteLine(response.ToString());
}
