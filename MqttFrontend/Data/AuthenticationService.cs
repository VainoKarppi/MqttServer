using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MqttFrontend.Data;


public class Authentication
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}


public class AuthenticationService {


    public Task<bool> Authorized(string apiKey) {   
        Console.WriteLine(apiKey);
        return Task.FromResult(apiKey == "123456");
    }




    //! Keep as Synchronous !
    public void UpdateApiKey(string apiKey) {
        Console.WriteLine($"Updating api key to: {apiKey}");

        string filePath = "appsettings.json";
        
        // Read the existing JSON content from the file
        string json = File.ReadAllText(filePath);

        JsonObject jobject = JsonNode.Parse(json)!.AsObject();
        jobject["ApiKey"] = apiKey;

        var options = new JsonSerializerOptions {WriteIndented = true};
        string serialised = JsonSerializer.Serialize(jobject, options);

        File.WriteAllText(filePath,serialised);

        Console.WriteLine("appsettings.json updated successfully!");
    }
}
