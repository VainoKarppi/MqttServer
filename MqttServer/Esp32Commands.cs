



using System.Diagnostics;
using System.Text;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

public static class MqttServer {
    public static List<ClientDevice> ConnectedClients = [];
    public static MQTTnet.Server.MqttServer? Server;
    private static Dictionary<int,string> Responses = [];

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
    }

    public static async Task StopMqttServer() {
        if (Server is null) return;
        await Server.StopAsync();
        Server.Dispose();
        Server = null;
        Console.WriteLine("MQTT Server Stopped!");
    }

    public static async Task<string> RequestDataAsync(string clientId, string topic, string payload = "") {
        if (Server is null) throw new Exception("Server not running!");
        if (payload is null) payload = "";

        // Key is used to indentify the response message result
        int key = new Random().Next(10000,99999);

        // REQUEST DATA PAYLOAD FORMAT = "key|payload" = "52265|myPayload"
        payload = key.ToString() + "|" + payload;
        await SendDataAsync(clientId, topic, payload);

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Get data from Requests dictionary. If the response for the key exists
        // Timeout for 1 second
        while (stopwatch.ElapsedMilliseconds < 2000) {
            await Task.Delay(10);
            if (Responses.TryGetValue(key, out string? responseData)) {
                Responses.Remove(key);
                Console.WriteLine($"DEVICE:{clientId} GOT RESPONSE: {responseData}");
                return responseData;
            }
        }

        Console.WriteLine($"ERROR: Request timed out! DeviceId: {clientId} | ({topic}) | ({payload})");
        throw new TimeoutException("Request response not found!");
    }
    public static async Task SendDataAsync(string clientId, string topic, string payload) {
        if (Server is null) throw new Exception("Server not running!");
        //if (!GetEsp32Status(clientId)) throw new Exception("Unable to contact ESP32");

        // Create a new message using the builder as usual.
        var message = new MqttApplicationMessageBuilder().WithTopic(topic + "/" + clientId).WithPayload(payload).Build();

        Console.WriteLine($"DEVICE:{clientId} SENDING MESSAGE: {message.Topic} | {payload}");

        // Now inject the new message at the broker.
        await Server.InjectApplicationMessage(new InjectedMqttApplicationMessage(message) {
            SenderClientId = "server"
        });
    }

    public static bool GetEsp32Status(string clientId) {
        try {
            if (Server is null) throw new Exception("Server not running!");
            int index = ConnectedClients.FindIndex(client => client.ClientId!.ToLower() == clientId.ToLower());
            if (index == -1) throw new Exception($"Unable to find client from list with id: {clientId}");
            // TODO ping esp32 to make sure device is still alive
            return true;

        } catch (Exception) {
            return false;
        }
    }

    public static async Task SetLightState(string clientId, bool state) {
        if (Server is null) throw new Exception("Server not running!");
        
        await SendDataAsync(clientId, "setledstate",state.ToString().ToLower());
    }

    public static async Task<bool> GetLightState(string clientId) {
        if (Server is null) throw new Exception("Server not running!");

        string response = await RequestDataAsync(clientId, "getledstate");
        return response == "1";
    }

    public static void GetWeatherData(string clientId) {
        if (Server is null) throw new Exception("Server not running!");

    }





    private static Task OnClientMessageEvent(ApplicationMessageNotConsumedEventArgs args) {
        if (args.SenderId.ToLower() == "server") return Task.CompletedTask;
        

        string topic = args.ApplicationMessage.Topic;
        string message = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
        string deviceName = ConnectedClients.FirstOrDefault(client => client.ClientId == args.SenderId)!.ClientId!;
        if (string.IsNullOrEmpty(deviceName)) throw new Exception("DEVICE NAME NOT FOUND!");


        if (topic == "esp32/weatherdata") {
            string? temperature = message.Split(',')[0].Replace(".", ",");
            string? humidity = message.Split(',')[1].Replace(".", ",");

            float? temperatureValue = null;
            float? humidityValue = null;

            if (temperature.ToLower() != "nan") temperatureValue = float.Parse(temperature);
            if (humidity.ToLower() != "nan") humidityValue = float.Parse(humidity);

            // Return if no data is being added at all!
            if (temperatureValue is null && humidityValue is null) return Task.CompletedTask;

            Database.WeatherData data = new() {
                Date = DateTime.Now,
                DeviceName = deviceName,
                Temperature = temperatureValue,
                Humidity = humidityValue
            };
            Database.AddWeatherData(data);
            return Task.CompletedTask;
        }


        if (message.Contains('|')) {
            string responseCode = message.Split('|')[0];

            if (responseCode.Contains("response:")) {
                // Is response to Request from server to ESP
                int key = int.Parse(responseCode.Split(':')[1]);
                Responses.Add(key,message.Split('|')[1]);
            } else {
                // Is request from ESP to Server
                // TODO not really needed
            }
        }

        return Task.CompletedTask;
    }



    private static Task OnClientConnectedEvent(ClientConnectedEventArgs args) {
        Console.WriteLine($"Client Connected! {args.ClientId} | {args.Endpoint}");
        
        ConnectedClients.Add(new ClientDevice(args.ClientId,args.Endpoint,args.UserName));
        return Task.CompletedTask;
    }

    private static Task OnClientDisconnectedEvent(ClientDisconnectedEventArgs args) {
        Console.WriteLine($"Client disconnected! {args.ClientId} | {args.Endpoint}");

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