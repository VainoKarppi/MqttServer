using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    public async Task<WeatherData[]> GetForecastAsync(string token)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

        HttpResponseMessage response = await client.GetAsync(AuthenticationService.ApiUrl + "getAllWeatherData");

        if (!response.IsSuccessStatusCode) throw new UnauthorizedAccessException("Invalid credientials");
        
        string responseData = await response.Content.ReadAsStringAsync();

        if (responseData == "{}" || responseData == "") throw new Exception("Server error!");

        using JsonDocument document = JsonDocument.Parse(responseData)!;

        List<WeatherData> weatherData = new();
        foreach (JsonElement element in document.RootElement.EnumerateArray()) {

            WeatherData data = new() { 
                Id = element.TryGetProperty("id", out var id) && id.ValueKind != JsonValueKind.Null ? id.GetInt32() : null,
                Date = element.TryGetProperty("date", out var date) && date.ValueKind != JsonValueKind.Null ? date.GetDateTime() : null,
                Humidity = element.TryGetProperty("humidity", out var humidity) && humidity.ValueKind != JsonValueKind.Null ? (float)humidity.GetDecimal() : null,
                Temperature = element.TryGetProperty("temperature", out var temperature) && temperature.ValueKind != JsonValueKind.Null ? (float)temperature.GetDecimal() : null,
                Wind = element.TryGetProperty("wind", out var wind) && wind.ValueKind != JsonValueKind.Null ? (float)wind.GetDecimal() : null,
                Pressure = element.TryGetProperty("pressure", out var pressure) && pressure.ValueKind != JsonValueKind.Null ? (float)pressure.GetDecimal() : null,
            };
            weatherData.Add(data);
        }

        return weatherData.ToArray();
    }



    public class WeatherData {
        public int? Id { get; set; }
        public DateTime? Date { get; set; }
        public float? Humidity { get; set; }
        public float? Temperature { get; set; }
        public float? Wind { get; set; }
        public float? Pressure { get; set; }
    }
}
