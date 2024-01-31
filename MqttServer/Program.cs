using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet;
using System.Text.Json;
using MQTTnet.Packets;
using MQTTnet.Client;
using System.Text;



//! START SERVER
var mqttFactory = new MqttFactory();
//var mqttFactory = new MqttFactory(new ConsoleLogger());
var options = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(1234).Build();

using MqttServer Server = mqttFactory.CreateMqttServer(options);

// Subscribe to all client messages
Server.ApplicationMessageNotConsumedAsync += ClientMessageEvent;
Server.ClientConnectedAsync += ClientConnectedEvent;
Server.ClientDisconnectedAsync += ClientDisconnectedEvent;


await Server.StartAsync();
Console.WriteLine("Server Started!");


Console.WriteLine("\nCommands: Exit, Send\n");
while (true)
{
    string? input = Console.ReadLine()?.ToLower();
    if (string.IsNullOrEmpty(input)) continue;
    if (input == "exit") break;

    if (input == "send") {
        Console.WriteLine("\nEnter topic: ");
        string? topic = Console.ReadLine();
        Console.WriteLine("\nEnter payload: ");
        string? mode = Console.ReadLine();
        await SendData(topic, mode);
    }

}



static Task ClientMessageEvent(ApplicationMessageNotConsumedEventArgs args) {
    string topic = args.ApplicationMessage.Topic;
    string message = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

    Console.WriteLine(topic);
    Console.WriteLine(message);

    return Task.CompletedTask;
}

static Task ClientConnectedEvent(ClientConnectedEventArgs args) {
    Console.WriteLine("Client Connected! ");
    Console.WriteLine(args.ClientId);

    return Task.CompletedTask;
}

static Task ClientDisconnectedEvent(ClientDisconnectedEventArgs args) {
    Console.WriteLine("Client disconnected!");
    Console.WriteLine(args.ClientId);

    return Task.CompletedTask;
}



async Task SendData(string? topic, string? mode) {

    // Create a new message using the builder as usual.
    var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(mode).Build();

    Console.WriteLine(message.Topic);
    Console.WriteLine(message.ConvertPayloadToString());

    // Now inject the new message at the broker.
    await Server.InjectApplicationMessage(new InjectedMqttApplicationMessage(message) {
        SenderClientId = "SenderClientId"
    });
}








class ConsoleLogger : IMqttNetLogger {
    readonly object _consoleSyncRoot = new();

    public bool IsEnabled => true;

    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception)
    {
        var foregroundColor = ConsoleColor.White;
        switch (logLevel)
        {
            case MqttNetLogLevel.Verbose:
                foregroundColor = ConsoleColor.White;
                break;

            case MqttNetLogLevel.Info:
                foregroundColor = ConsoleColor.Green;
                break;

            case MqttNetLogLevel.Warning:
                foregroundColor = ConsoleColor.DarkYellow;
                break;

            case MqttNetLogLevel.Error:
                foregroundColor = ConsoleColor.Red;
                break;
        }

        if (parameters?.Length > 0)
        {
            message = string.Format(message, parameters);
        }

        lock (_consoleSyncRoot)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);

            if (exception != null)
            {
                Console.WriteLine(exception);
            }
        }
    }
}


