

try {
    await Database.ConnectToDatabase();
    Console.WriteLine("Connected to database!");

    MqttServerAPI.StartAPIServer();

    await MqttServer.StartMqttServer();
} catch (Exception ex) {
    Console.WriteLine(ex);
    Console.WriteLine("\n\n" + ex.Message.ToUpper());

    await Database.CloseDatabase();
    MqttServerAPI.StopAPIServer();
    await MqttServer.StopMqttServer();

    await Task.Delay(500);
    return 1;
}






Console.WriteLine("\nCommands: Exit, Devices, Send, Request, CreateApiToken");
while (true) {
    try {
        Console.Write("\n> ");
        string? input = Console.ReadLine()?.ToLower().Trim();
        if (string.IsNullOrEmpty(input)) continue;
        if (input == "exit") break;

        if (input == "devices") {
            var devices = MqttServer.ConnectedClients;
            foreach (var device in devices) {
                Console.WriteLine($"ID: {device.ClientId} | NAME: {device.DeviceName} | IP: {device.Endpoint}");
            }
            continue;
        }

        if (input == "request") {
            dynamic data = GetPayload();
            string response = await MqttServer.RequestDataAsync(data.Target, data.Topic, data.Payload);
            Console.WriteLine(response);
            continue;
        }

        if (input == "send") {
            dynamic data = GetPayload();
            await MqttServer.SendDataAsync(data.Target, data.Topic, data.Payload);
            continue;
        }

        if (input == "createapitoken") {
            Console.WriteLine("\nEnter username: ");
            string? username = Console.ReadLine();
            Console.WriteLine("\nEnter expiration time in days (default is one year): ");
            string? expiration = Console.ReadLine();
            string token = MqttServerAPI.GenerateUserAndToken(username,expiration);
            Console.WriteLine($"Your token is:\n{token}\n\nKEEP IT SAFE!\n");
            continue;
        }

        Console.WriteLine("Invalid command!");
    } catch (Exception ex) {
        Console.WriteLine(ex.Message);
    }
}

static dynamic GetPayload() {
    Console.WriteLine("\nEnter target ID: ");
    string? target = Console.ReadLine();
    Console.WriteLine("\nEnter topic: ");
    string? topic = Console.ReadLine();
    Console.WriteLine("\nEnter payload: ");
    string? payload = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(topic)) throw new Exception("Invalid input!");

    return new {Target = target, Topic = topic, Payload = payload};
}

return 0;