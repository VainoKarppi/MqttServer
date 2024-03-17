



using System.Diagnostics;
using System.Text;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

public static class MqttServer {
    public static MQTTnet.Server.MqttServer? Server;
    private static Dictionary<int,string> Requests = [];

    public static async Task StartMqttServer(int port, bool enableLogging = false) {
        // CREATE SERVER

        var mqttFactory = enableLogging ? new MqttFactory(new ConsoleLogger()) : new MqttFactory();
        var options = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(port).Build();
        Server = mqttFactory.CreateMqttServer(options);

        await Server.StartAsync();
        Server.ApplicationMessageNotConsumedAsync += OnClientMessageEvent;
        Server.ClientConnectedAsync += OnClientConnectedEvent;
        Server.ClientDisconnectedAsync += OnClientDisconnectedEvent;
        Console.WriteLine("MQTT Server Started!");
    }

    public static async Task StopMqttServer() {
        if (Server is null) return;
        await Server.StopAsync();
        Server.Dispose();
        Server = null;
        Console.WriteLine("MQTT Server Stopped!");
    }

    public static async Task<string> RequestData(string topic, string? payload = null) {
        if (Server is null) throw new Exception("Server not running!");
        if (payload is null) payload = "";

        int key = new Random().Next(1000,9999);

        // REQUEST DATA FORMAT = "topic|key" = "myTopic|5226"
        await SendDataAsync(topic + "|" + key.ToString(), payload);

        Stopwatch stopwatch = new Stopwatch();

        // Get data from Requests dictionary. If the response for the key exists
        // Timeout for 1 second
        while (stopwatch.ElapsedMilliseconds < 1000) {
            await Task.Delay(3);
            if (Requests.TryGetValue(key, out string? responseData)) {
                Requests.Remove(key);
                return responseData;
            }
        }

        throw new TimeoutException("Request not found!");
    }
    public static async Task SendDataAsync(string topic, string payload) {
        if (Server is null) throw new Exception("Server not running!");

        // Create a new message using the builder as usual.
        var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).Build();

        Console.WriteLine(message.Topic);
        Console.WriteLine(message.ConvertPayloadToString());

        // Now inject the new message at the broker.
        await Server.InjectApplicationMessage(new InjectedMqttApplicationMessage(message) {
            SenderClientId = "SenderClientId"
        });
    }

    public static bool GetEsp32Status() {
        if (Server is null) throw new Exception("Server not running!");

        return false;
    }

    public static void SetLightState(bool state) {
        if (Server is null) throw new Exception("Server not running!");


    }

    public static async Task<bool> GetLightState() {
        if (Server is null) throw new Exception("Server not running!");

        string response = await RequestData("getlightstate");
        bool state = bool.Parse(response);
        return state;
    }

    public static void GetWeatherData() {
        if (Server is null) throw new Exception("Server not running!");


    }





    private static Task OnClientMessageEvent(ApplicationMessageNotConsumedEventArgs args) {
        string topic = args.ApplicationMessage.Topic;
        string message = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);


        if (topic.Contains('|')) {
            string responseCode = topic.Split('|')[1];


            if (responseCode.Contains("response:")) {
                // Is response to Request from server to ESP
                int key = int.Parse(responseCode.Split(':')[1]);
                Requests.Add(key,message);
            } else {
                // Is request from ESP to Server
                // TODO
            }
        }

        Console.WriteLine(topic);
        Console.WriteLine(message);

        return Task.CompletedTask;
    }



    private static Task OnClientConnectedEvent(ClientConnectedEventArgs args) {
        Console.WriteLine("Client Connected!");
        Console.WriteLine(args.ClientId);
        Console.WriteLine(args.Endpoint);

        return Task.CompletedTask;
    }

    private static Task OnClientDisconnectedEvent(ClientDisconnectedEventArgs args) {
        Console.WriteLine("Client disconnected!");
        Console.WriteLine(args.ClientId);

        return Task.CompletedTask;
    }
    

    private class ConsoleLogger : IMqttNetLogger {
        readonly object _consoleSyncRoot = new();
        public bool IsEnabled => true;
        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception) {

            var colorMap = new Dictionary<MqttNetLogLevel, ConsoleColor> {
                { MqttNetLogLevel.Verbose, ConsoleColor.White },
                { MqttNetLogLevel.Info, ConsoleColor.Green },
                { MqttNetLogLevel.Warning, ConsoleColor.DarkYellow },
                { MqttNetLogLevel.Error, ConsoleColor.Red }
            };

            if (parameters?.Length > 0) message = string.Format(message, parameters);

            lock (_consoleSyncRoot) {
                Console.ForegroundColor = colorMap[logLevel];
                Console.WriteLine(message);

                if (exception != null) Console.WriteLine(exception);
            }
        }
    }
}