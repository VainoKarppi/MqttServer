using MQTTnet.Samples.Server;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet;


internal class Program
{
    private static async Task Main(string[] args)
    {
        //! START SERVER
        var mqttFactory = new MqttFactory(new ConsoleLogger());
        var options = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(1234).Build();

        using MqttServer Server = mqttFactory.CreateMqttServer(options);
        await Server.StartAsync();
        Console.WriteLine("Server Started!");


        _ = new Thread(() =>
        {
            // READ DATA AND STORE TO JSON
        });

        Console.WriteLine("\nCommands: Exit, Send\n");
        while (true)
        {
            string? input = Console.ReadLine()?.ToLower();
            if (string.IsNullOrEmpty(input)) continue;
            if (input == "exit") break;

            if (input == "send")
            {
                Console.WriteLine("\nEnter topic: ");
                string? topic = Console.ReadLine();
                Console.WriteLine("\nEnter payload: ");
                string? mode = Console.ReadLine();
                await SendData(topic, mode);
            }

        }


        async Task SendData(string? topic, string? mode)
        {

            // Create a new message using the builder as usual.
            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(mode).Build();

            Console.WriteLine(message.Topic);
            Console.WriteLine(message.ConvertPayloadToString());

            // Now inject the new message at the broker.
            await Server.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(message)
                {
                    SenderClientId = "SenderClientId"
                });
        }
    }
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