using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet;
using System.Text.Json;
using MQTTnet.Packets;
using MQTTnet.Client;
using System.Text;
using Microsoft.AspNetCore.Components;







try {
    await Database.ConnectToDatabase();
    Console.WriteLine("Connected to database!");

    MqttServerAPI.StartAPIServer();

    await MqttServer.StartMqttServer(1234,true);
} catch (Exception ex) {
    Console.WriteLine(ex);
    Console.WriteLine("\n\n" + ex.Message.ToUpper());

    await Database.CloseDatabase();
    MqttServerAPI.StopAPIServer();
    await MqttServer.StopMqttServer();


    await Task.Delay(500);
    return 1;
}






Console.WriteLine("\nCommands: Exit, Send, CreateApiToken\n");
while (true) {
    try {
        string? input = Console.ReadLine()?.ToLower();
        if (string.IsNullOrEmpty(input)) continue;
        if (input == "exit") break;

        if (input == "send") {
            Console.WriteLine("\nEnter topic: ");
            string? topic = Console.ReadLine();
            Console.WriteLine("\nEnter payload: ");
            string? mode = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(mode)) continue;

            await MqttServer.SendDataAsync(topic, mode);
        }

        if (input == "createapitoken") {
            Console.WriteLine("\nEnter username: ");
            string? username = Console.ReadLine();
            Console.WriteLine("\nEnter expiration time in days (default is one year): ");
            string? expiration = Console.ReadLine();
            string token = MqttServerAPI.GenerateUserAndToken(username,expiration);
            Console.WriteLine($"Your token is:\n{token}\n\nKEEP IT SAFE!\n");
        }
    } catch (Exception ex) {
        Console.WriteLine(ex.Message);
    }
}

return 0;