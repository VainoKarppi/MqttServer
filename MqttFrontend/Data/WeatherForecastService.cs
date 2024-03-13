using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;

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
                Id = element.GetProperty("id").GetInt32(),
                Date = element.GetProperty("date").GetDateTime(),
                Humidity = (float)element.GetProperty("humidity").GetDecimal(),
                Temperature = (float)element.GetProperty("temperature").GetDecimal(),
                Wind = (float)element.GetProperty("wind").GetDecimal(),
                Pressure = (float)element.GetProperty("pressure").GetDecimal()
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
