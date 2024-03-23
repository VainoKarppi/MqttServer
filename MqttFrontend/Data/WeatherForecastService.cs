using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.VisualBasic;

namespace MqttFrontend.Data;


public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}


public class WeatherForecastService
{

    public async Task<WeatherData[]> GetForecastAsync(string token, DateOnly? start = null, DateOnly? end = null) {
        
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

        if (start is not null && end is null) throw new Exception("No endDate set!");

        string query = "";
        if (end is not null) {
            start ??= DateOnly.FromDateTime(DateTime.Now);
            query += $"?start={start}&end={end}";
        }

        HttpResponseMessage response = await client.GetAsync(AuthenticationService.ApiUrl + "getWeatherData" + query);

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) throw new UnauthorizedAccessException("");
        if (!response.IsSuccessStatusCode) throw new UnauthorizedAccessException("Invalid credientials");
        
        string responseData = await response.Content.ReadAsStringAsync();
        if (responseData == "[]" || responseData == "{}" || responseData == "") return [];

        var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
        List<WeatherData>? weatherData = JsonSerializer.Deserialize<List<WeatherData>>(responseData, options);

        if (weatherData is null) return [];

        // Order return by date
        return weatherData.OrderBy(data => data.Date).ToArray();
    }



    public class WeatherData {
        public int? Id { get; set; }
        public string? DeviceName { get; set; }
        public DateTime? Date { get; set; }
        public float? Humidity { get; set; }
        public float? Temperature { get; set; }
        public float? Wind { get; set; }
        public float? Pressure { get; set; }
    }
}
