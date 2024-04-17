using System.ComponentModel.DataAnnotations;
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

    // TODO add multiple user authentication with different api keys and permissions

    public static string? ApiUrl;
    public static string? Token;
    public static List<User> UsersList = new();
    private static string AppSettingsPath = "appsettings.json";

    public async Task<User?> Authenticate() {
        if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrWhiteSpace(Token)) return null;

        using HttpClient client = new();

        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
        HttpResponseMessage response = await client.GetAsync(ApiUrl + "authenticate");

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) throw new UnauthorizedAccessException("");    
        if (!response.IsSuccessStatusCode) throw new UnauthorizedAccessException();

        // Read response content
        string responseData = await response.Content.ReadAsStringAsync();
        if (responseData == "{}" || responseData == "") throw new Exception("Server error!");

        var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
        User? user = JsonSerializer.Deserialize<User>(responseData, options) ?? throw new ValidationException($"User data corrupt: {responseData}");
        
        return user; 
    }




    //! Keep as Synchronous !
    public async Task UpdateApiKey(string apiKey) {
        if (apiKey == Token) return;
        

        string json = await File.ReadAllTextAsync(AppSettingsPath);

        JsonObject jobject = JsonNode.Parse(json)!.AsObject();

        jobject["ApiKey"] = apiKey;
        Token = apiKey;

        if (jobject["ApiKey"]!.ToString() == apiKey) return;

        var options = new JsonSerializerOptions {WriteIndented = true};
        string serialised = JsonSerializer.Serialize(jobject, options);

        Console.WriteLine($"Updating API key to: {apiKey}");
        await File.WriteAllTextAsync(AppSettingsPath,serialised);

        Console.WriteLine("API key updated successfully!");
    }

    public async Task UpdateApiUrl(string apiUrl) {
        if (apiUrl[^1] != '/') apiUrl += "/";
        if (apiUrl == ApiUrl) return;

        string json = await File.ReadAllTextAsync(AppSettingsPath);

        JsonObject jobject = JsonNode.Parse(json)!.AsObject();
        jobject["ApiUrl"] = apiUrl;
        ApiUrl = apiUrl;

        if (jobject["ApiUrl"]!.ToString() == apiUrl) return;
        
        Console.WriteLine($"Updating API URL to: {apiUrl}");

        var options = new JsonSerializerOptions {WriteIndented = true};
        string serialised = JsonSerializer.Serialize(jobject, options);

        await File.WriteAllTextAsync(AppSettingsPath,serialised);

        Console.WriteLine("API URL updated successfully!");
    }


    

    public class User {
        public required int? Id { get; set; }
        public required string? Username { get; set; }
        public required DateTime? Expiration { get; set; }
        public required string? Token { get; set; }
    }
}
