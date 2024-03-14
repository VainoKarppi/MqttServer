
using System;
using System.Data;
using System.Text.Json;
using MySqlConnector;

static class Database {

    //TODO Add these values to environment config
    private static readonly string IP = "karppi.dy.fi";
    private static readonly string Username = "test";
    private static readonly string Password = "test";
    private static readonly string DatabaseName = "test";


    
    private static MySqlConnection? Connection;


    public static async Task ConnectToDatabase() {
        string connectionString = $"server={IP};uid={Username};pwd={Password}";

        Connection = new MySqlConnection(connectionString);
        await Connection.OpenAsync();

        using MySqlCommand command = new ($"CREATE DATABASE IF NOT EXISTS {DatabaseName}", Connection);
        if (command.ExecuteNonQuery() != 0)
            Console.WriteLine($"Database not found, and new was created! ({DatabaseName})");
        
        await Connection.ChangeDatabaseAsync(DatabaseName);

        await CreateTables();

        //AddTestDataToDB();
    }

    public static bool IsConnectedToDatabase() {
        if (Connection is null) return false;
        if (Connection.State != ConnectionState.Open) return false;

        // Make sure database can be accessed using a simple query
        try {
            using MySqlCommand command = new("SELECT 1", Connection);
            command.ExecuteScalar();
            return true; 
        } catch (Exception) {
            return false;
        }
    }



    public static async Task CreateTables() {
        string tableName = "weatherdata";
        using MySqlCommand weatherdata = new ($@"CREATE TABLE IF NOT EXISTS {tableName} (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            timestamp TIMESTAMP,
            humidity FLOAT NULL DEFAULT NULL,
            temperature FLOAT NULL DEFAULT NULL,
            wind FLOAT NULL DEFAULT NULL,
            pressure FLOAT NULL DEFAULT NULL
        )", Connection);
        await weatherdata.ExecuteNonQueryAsync();

        tableName = "users";
        using MySqlCommand users = new ($@"CREATE TABLE IF NOT EXISTS {tableName} (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            username TEXT,
            expiration TIMESTAMP,
            token TEXT
        )", Connection);
        await users.ExecuteNonQueryAsync();

        
        tableName = "logs";
        using MySqlCommand logs = new ($@"CREATE TABLE IF NOT EXISTS {tableName} (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            user_id INT,
            timestamp TIMESTAMP,
            code INT NULL DEFAULT NULL,
            message TEXT NULL DEFAULT NULL,
            FOREIGN KEY (user_id) REFERENCES users(Id)
        )", Connection);
        await logs.ExecuteNonQueryAsync();
    }

    public static async Task<bool> CreateUser(string username, DateTime expiration, string token) {
        string tableName = "users";
        string insertDataSql = $"INSERT INTO {tableName} (username, expiration, token) VALUES (@username, @expiration, @token)";

        using MySqlCommand command = new MySqlCommand(insertDataSql, Connection);
        
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@expiration", expiration);
        command.Parameters.AddWithValue("@token", token);

        int rowsChanged = await command.ExecuteNonQueryAsync();
        if (rowsChanged == 0) throw new Exception("Unable to weather add data!");

        return true;
    }


    public static async Task<bool> DeleteUser(string token) {
        string tableName = "users";
        string insertDataSql = $"DELETE FROM {tableName} WHERE token=@token";

        using MySqlCommand command = new MySqlCommand(insertDataSql, Connection);
    
        command.Parameters.AddWithValue("@token", token);

        int rowsChanged = await command.ExecuteNonQueryAsync();
        if (rowsChanged == 0) throw new Exception($"Unable to delete user! {token}");

        return true;
    }

    public static async Task<User?> GetUserByToken(string token) {
        string tableName = "users";
        string sqlQuery = $"SELECT * FROM {tableName} WHERE token=@token";
        using MySqlCommand command = new MySqlCommand(sqlQuery, Connection);

        command.Parameters.AddWithValue("@token", token);
        
        using MySqlDataReader reader = await command.ExecuteReaderAsync();

        User user = new();

        while (await reader.ReadAsync()) {
            user.Id = (int)reader["id"];
            user.Username = (string)reader["username"];
            user.Expiration = (DateTime)reader["expiration"];
            user.Token = (string)reader["token"];
        }
        await reader.CloseAsync();

        // TODO check the ecpect validation
        if (user is null || user.Id is null) throw new KeyNotFoundException($"User not found! {token}");
        if (DateTime.Now > user.Expiration) throw new UnauthorizedAccessException($"User token has expired! {token}");

        return user;
    }

    public static async void AddTestDataToDB() {

        List<WeatherData> data = Enumerable.Range(1, 5).Select(index => new WeatherData {
            Id = index,
            Date = DateTime.Now.AddDays(Random.Shared.Next(-3, 3)),
            Humidity = Random.Shared.Next(-20, 55),
            Temperature = Random.Shared.Next(-20, 55),
            Wind = Random.Shared.Next(-20, 55),
            Pressure = Random.Shared.Next(-20, 55)
        }).ToList();

        foreach (var weatherData in data) {
            await AddWeatherData(weatherData);  
        }
        
    }

    // TODO get from DB
    public static async Task<WeatherData[]> GetAllWeatherData() {
        string tableName = "weatherdata";
        string sqlQuery = $"SELECT * FROM {tableName}";

        using MySqlCommand command = new MySqlCommand(sqlQuery, Connection);

        using MySqlDataReader reader = await command.ExecuteReaderAsync();

        List<WeatherData> weatherDatas = [];
        while (await reader.ReadAsync()) {
            WeatherData weather = new() {
                Id = (int)reader["id"],
                Date = (DateTime)reader["timestamp"],
                Humidity = reader["humidity"] != DBNull.Value ? (float)reader["humidity"] : null,
                Temperature = reader["temperature"] != DBNull.Value ? (float)reader["temperature"] : null,
                Wind = reader["wind"] != DBNull.Value ? (float)reader["wind"] : null,
                Pressure = reader["pressure"] != DBNull.Value ? (float)reader["pressure"] : null,
            };
            weatherDatas.Add(weather);
        }
        await reader.CloseAsync();

        return weatherDatas.ToArray();
    }


    //TODO SQL query to select only within the times
    public static async Task<WeatherData[]> GetWeatherDataByTime(DateOnly start, DateOnly end) {
        string tableName = "weatherdata";
        string sqlQuery = $"SELECT * FROM {tableName} WHERE timestamp >= @dateStart";
        using MySqlCommand command = new MySqlCommand(sqlQuery, Connection);

        command.Parameters.AddWithValue("@dateEnd", end);
        command.Parameters.AddWithValue("@dateStart", start);

        using MySqlDataReader reader = await command.ExecuteReaderAsync();

        List<WeatherData> weatherDatas = [];

        while (await reader.ReadAsync()) {
            WeatherData weather = new() {
                Id = (int)reader["id"],
                Date = (DateTime)reader["timestamp"],
                Humidity = reader["humidity"] != DBNull.Value ? (float)reader["humidity"] : null,
                Temperature = reader["temperature"] != DBNull.Value ? (float)reader["temperature"] : null,
                Wind = reader["wind"] != DBNull.Value ? (float)reader["wind"] : null,
                Pressure = reader["pressure"] != DBNull.Value ? (float)reader["pressure"] : null,
            };
            weatherDatas.Add(weather);
        }
        await reader.CloseAsync();

        return weatherDatas.ToArray();
    }

    // TODO Get weatherdata within timeframe

    // TODO Get single weather data by id

    // TODO Add log message

    // TODO get all log messages

    // TODO get all log messages by client + additional time option

    public static async Task<bool> AddWeatherData(WeatherData weatherData) {
        string tableName = "weatherdata";
        string insertDataSql = $"INSERT INTO {tableName} (timestamp, humidity, temperature, wind, pressure) VALUES (@timestamp, @humidity, @temperature, @wind, @pressure)";

        using MySqlCommand command = new MySqlCommand(insertDataSql, Connection);

        command.Parameters.AddWithValue("@timestamp", weatherData.Date);
        command.Parameters.AddWithValue("@humidity", weatherData.Humidity);
        command.Parameters.AddWithValue("@temperature", weatherData.Temperature);
        command.Parameters.AddWithValue("@wind", weatherData.Wind);
        command.Parameters.AddWithValue("@pressure", weatherData.Pressure);

        int rowsChanged = await command.ExecuteNonQueryAsync();
        if (rowsChanged == 0) throw new Exception("Unable to weather add data!");

        return true;
    }


    public class WeatherData {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public float? Humidity { get; set; }
        public float? Temperature { get; set; }
        public float? Wind { get; set; }
        public float? Pressure { get; set; }
    }


    public class User {
        public int? Id { get; set; }
        public string? Username { get; set; }
        public DateTime? Expiration { get; set; }
        public string? Token { get; set; }
    }
}