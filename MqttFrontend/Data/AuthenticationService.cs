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
    public AuthenticationService() {
        if(ApiUrl is null || Token is null) {
            string json = File.ReadAllText(AppSettingsPath);
            JsonObject jobject = JsonNode.Parse(json)!.AsObject();
            ApiUrl = (string)jobject["ApiUrl"]!;
            Token = (string)jobject["ApiKey"]!;
        }
    }

    public static string? ApiUrl;
    public static string? Token;
    private static string AppSettingsPath = "appsettings.json";

    public async Task<User?>? Authorized() {
        Console.WriteLine($"CONNECTING: {ApiUrl + "authenticate"} : {Token}");

        using HttpClient client = new();

        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
        HttpResponseMessage response = await client.GetAsync(ApiUrl + "authenticate");

        if (!response.IsSuccessStatusCode) throw new UnauthorizedAccessException("Invalid credientials");

        // Read response content
        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseBody);

        throw new NotImplementedException("test");
        return new User(); //TODO
    }




    //! Keep as Synchronous !
    public async Task UpdateApiKey(string apiKey) {
        if (apiKey == Token) return;
        Console.WriteLine($"Updating API key to: {apiKey}");

        string json = await File.ReadAllTextAsync(AppSettingsPath);

        JsonObject jobject = JsonNode.Parse(json)!.AsObject();
        jobject["ApiKey"] = apiKey;
        Token = apiKey;

        var options = new JsonSerializerOptions {WriteIndented = true};
        string serialised = JsonSerializer.Serialize(jobject, options);

        await File.WriteAllTextAsync(AppSettingsPath,serialised);

        Console.WriteLine("API key updated successfully!");
    }

    public async Task UpdateApiUrl(string apiUrl) {
        if (apiUrl[^1] != '/') apiUrl += "/";
        if (apiUrl == ApiUrl) return;

        Console.WriteLine($"Updating API URL to: {apiUrl}");

        string json = await File.ReadAllTextAsync(AppSettingsPath);

        JsonObject jobject = JsonNode.Parse(json)!.AsObject();
        jobject["ApiUrl"] = apiUrl;
        ApiUrl = apiUrl;

        var options = new JsonSerializerOptions {WriteIndented = true};
        string serialised = JsonSerializer.Serialize(jobject, options);

        await File.WriteAllTextAsync(AppSettingsPath,serialised);

        Console.WriteLine("API URL updated successfully!");
    }




    public class User {
        public int? Id;
        public string? Username;
        public DateTime? Expiration;
        public string? Token;
    }
}
