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

        JsonObject jobject = JsonNode.Parse(responseData)!.AsObject();

        User user = new() {
            Id = (int)jobject["id"]!,
            Username = (string)jobject["username"]!,
            Expiration = (DateTime)jobject["expiration"]!,
            Token = (string)jobject["token"]!
        };
        if (user.Id is null || user.Username is null || user.Expiration is null || user.Token is null) throw new ValidationException($"User data corrupt: {responseData}");

        bool idExists = UsersList.Any(u => u.Id == user.Id);
        if (!idExists) UsersList.Add(user);

        return user; 
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
        public int? Id { get; set; }
        public string? Username { get; set; }
        public DateTime? Expiration { get; set; }
        public string? Token { get; set; }
    }
}
