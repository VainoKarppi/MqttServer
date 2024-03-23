



using System.Diagnostics;
using System.Text;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

public static class MqttServer {
    public static List<ClientDevice> ConnectedClients = [];
    public static MQTTnet.Server.MqttServer? Server;
    private static Dictionary<int,string> Requests = [];

    public static async Task StartMqttServer() {
        // TODO Read port from appsettings.json

        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        _ = int.TryParse(configuration["MqttServer:Port"], out int port);
        _ = bool.TryParse(configuration["MqttServer:Debug"]!, out bool enableLogging);

        var mqttFactory = enableLogging ? new MqttFactory(new ConsoleLogger()) : new MqttFactory();
        var options = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(port).Build();
        Server = mqttFactory.CreateMqttServer(options);

        await Server.StartAsync();

        // Subscribe to evenets
        Server.ApplicationMessageNotConsumedAsync += OnClientMessageEvent;
        Server.ClientConnectedAsync += OnClientConnectedEvent;
        Server.ClientDisconnectedAsync += OnClientDisconnectedEvent;

        Console.WriteLine("MQTT Server Started!");

        // Add test client:
        ConnectedClients.Add(new ClientDevice("123","127.0.0.1:5007","testTemp"));
    }

    public static async Task StopMqttServer() {
        if (Server is null) return;
        await Server.StopAsync();
        Server.Dispose();
        Server = null;
        Console.WriteLine("MQTT Server Stopped!");
    }

    public static async Task<string> RequestDataAsync(string clientId, string topic, string? payload = null) {
        if (Server is null) throw new Exception("Server not running!");
        if (payload is null) payload = "";

        // Key is used to indentify the response message result
        int key = new Random().Next(10000,99999);

        // REQUEST DATA FORMAT = "topic|key" = "myTopic|52265"
        topic = topic + "|" + key.ToString();
        await SendDataAsync(clientId, topic, payload);

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Get data from Requests dictionary. If the response for the key exists
        // Timeout for 1 second
        while (stopwatch.ElapsedMilliseconds < 1000) {
            await Task.Delay(10);
            if (Requests.TryGetValue(key, out string? responseData)) {
                Requests.Remove(key);
                return responseData;
            }
        }

        Console.WriteLine($"ERROR: Request timed out! DeviceId: {clientId} | ({topic}) | ({payload})");
        throw new TimeoutException("Request response not found!");
    }
    public static async Task SendDataAsync(string clientId, string topic, string payload) {
        if (Server is null) throw new Exception("Server not running!");
        if (!GetEsp32Status(clientId)) throw new Exception("Unable to contact ESP32");

        // Create a new message using the builder as usual.
        var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).Build();

        Console.WriteLine($"DEVICE:{clientId} SENDING MESSAGE: {message.Topic} | {payload}");

        // Now inject the new message at the broker.
        await Server.InjectApplicationMessage(new InjectedMqttApplicationMessage(message) {
            SenderClientId = "server"
        });
    }

    public static bool GetEsp32Status(string clientId) {
        try {
            if (Server is null) throw new Exception("Server not running!");
            int index = ConnectedClients.FindIndex(client => client.ClientId == clientId);
            if (index == -1) throw new Exception($"Unable to find client from list with id: {clientId}");
            // TODO ping esp32 to make sure device is still alive
            return true;

        } catch (Exception) {
            return false;
        }
    }

    public static async Task SetLightState(string clientId, bool state) {
        if (Server is null) throw new Exception("Server not running!");
        
        await SendDataAsync(clientId, "setledstate",state.ToString());
    }

    public static async Task<bool> GetLightState(string clientId) {
        if (Server is null) throw new Exception("Server not running!");

        string response = await RequestDataAsync(clientId, "getlightstate");
        bool state = bool.Parse(response);
        return state;
    }

    public static void GetWeatherData(string clientId) {
        if (Server is null) throw new Exception("Server not running!");

    }





    private static Task OnClientMessageEvent(ApplicationMessageNotConsumedEventArgs args) {
        if (args.SenderId.ToLower() == "server") return Task.CompletedTask;
        

        string topic = args.ApplicationMessage.Topic;
        string message = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);



        if (topic == "esp32/weatherdata") {
            float temperature = float.Parse(message.Split(',')[0]);
            float humidity = float.Parse(message.Split(',')[1]);
            Database.WeatherData data = new() {
                Date = DateTime.Now,
                DeviceName = ConnectedClients.FirstOrDefault(client => client.ClientId == args.SenderId)!.DeviceName!,
                Temperature = temperature,
                Humidity = humidity
            };
            Database.AddWeatherData(data);
            return Task.CompletedTask;
        }


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

        ConnectedClients.Add(new ClientDevice(args.ClientId,args.Endpoint,args.UserName));
        return Task.CompletedTask;
    }

    private static Task OnClientDisconnectedEvent(ClientDisconnectedEventArgs args) {
        Console.WriteLine("Client disconnected!");
        Console.WriteLine(args.ClientId);
        Console.WriteLine(args.Endpoint);

        ConnectedClients.RemoveAll(user => user.ClientId == args.ClientId);

        return Task.CompletedTask;
    }
    
    public class ClientDevice {
        public string? Endpoint { get; set; }
        public string? ClientId { get; set; }
        public string? DeviceName { get; set; }
        public ClientDevice() {}
        public ClientDevice(string clientId, string endpoint, string username) => (ClientId, Endpoint, DeviceName) = (clientId, endpoint, username);
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
                Console.ForegroundColor = ConsoleColor.White;

                if (exception != null) Console.WriteLine(exception);
            }
        }
    }
}