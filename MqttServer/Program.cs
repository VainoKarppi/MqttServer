using MQTTnet.Samples.Server;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet;
using System.Text.Json;
using MQTTnet.Packets;
using MQTTnet.Client;
using System.Text;



//! START SERVER
var mqttFactory = new MqttFactory(new ConsoleLogger());
var options = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(1234).Build();

using MqttServer Server = mqttFactory.CreateMqttServer(options);


await Server.StartAsync();
Console.WriteLine("Server Started!");


_ = new Thread(() => {
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


async Task SendData(string? topic, string? mode) {

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










/*
private static Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs eventArgs) {
    // Handle the received message
    string topic = eventArgs.ApplicationMessage.Topic;
    string payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
    Console.WriteLine($"Received message on topic '{topic}': {payload}");

    // You can add your custom message handling logic here

    return Task.CompletedTask;
}*/




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




sealed class MqttRetainedMessageModel {
    public string? ContentType { get; set; }
    public byte[]? CorrelationData { get; set; }
    public byte[]? Payload { get; set; }
    public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    public string? ResponseTopic { get; set; }
    public string? Topic { get; set; }
    public List<MqttUserProperty>? UserProperties { get; set; }

    public static MqttRetainedMessageModel Create(MqttApplicationMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new MqttRetainedMessageModel
        {
            Topic = message.Topic,

            // Create a copy of the buffer from the payload segment because 
            // it cannot be serialized and deserialized with the JSON serializer.
            Payload = message.PayloadSegment.ToArray(),
            UserProperties = message.UserProperties,
            ResponseTopic = message.ResponseTopic,
            CorrelationData = message.CorrelationData,
            ContentType = message.ContentType,
            PayloadFormatIndicator = message.PayloadFormatIndicator,
            QualityOfServiceLevel = message.QualityOfServiceLevel

            // Other properties like "Retain" are not if interest in the storage.
            // That's why a custom model makes sense.
        };
    }

    public MqttApplicationMessage ToApplicationMessage()
    {
        return new MqttApplicationMessage
        {
            Topic = Topic,
            PayloadSegment = new ArraySegment<byte>(Payload ?? Array.Empty<byte>()),
            PayloadFormatIndicator = PayloadFormatIndicator,
            ResponseTopic = ResponseTopic,
            CorrelationData = CorrelationData,
            ContentType = ContentType,
            UserProperties = UserProperties,
            QualityOfServiceLevel = QualityOfServiceLevel,
            Dup = false,
            Retain = true
        };
    }
}