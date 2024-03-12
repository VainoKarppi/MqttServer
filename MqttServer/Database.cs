
using System;
using System.Data;
using MySqlConnector;

static class Database {

    //TODO Add these values to environment config
    private static readonly string IP = "192.168.1.3";
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
            message TEXT NULL DEFAULT NULL
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
        string insertDataSql = $"SELECT * FROM {tableName} WHERE token=@token";
        using MySqlCommand command = new MySqlCommand(insertDataSql, Connection);

        command.Parameters.AddWithValue("@token", token);

        using MySqlDataReader reader = await command.ExecuteReaderAsync();

        User user = new();

        while (reader.Read()) {
            user.Id = (int)reader["id"];
            user.Username = (string)reader["username"];
            user.Expiration = (DateTime)reader["expiration"];
            user.Token = (string)reader["token"];
        }

        // TODO check the ecpect validation
        if (user is null || user.Id is null) throw new KeyNotFoundException("User not found!");
        if (DateTime.Now > user.Expiration) throw new UnauthorizedAccessException("User token has expired!");

        return user;
    }


    // TODO
    public static string GetAllWeatherData() {
        return "{RETURNED AS YEISON}";
    }

    // TODO Get weatherdata within timeframe

    // TODO Get single weather data by id

    // TODO Add log message

    // TODO get all log messages

    // TODO get all log messages by client + additional time option

    public static async Task<bool> AddWeatherData(float? humidity, float? temperature, float? wind, float? pressure) {
        string tableName = "weatherdata";
        string insertDataSql = $"INSERT INTO {tableName} (timestamp, humidity, temperature, wind, pressure) VALUES (@timestamp, @humidity, @temperature, @wind, @pressure)";

        using MySqlCommand command = new MySqlCommand(insertDataSql, Connection);

        command.Parameters.AddWithValue("@timestamp", DateTime.Now);
        command.Parameters.AddWithValue("@humidity", humidity);
        command.Parameters.AddWithValue("@temperature", temperature);
        command.Parameters.AddWithValue("@wind", wind);
        command.Parameters.AddWithValue("@pressure", pressure);

        int rowsChanged = await command.ExecuteNonQueryAsync();
        if (rowsChanged == 0) throw new Exception("Unable to weather add data!");

        return true;
    }





    public class User {
        public int? Id { get; set; }
        public string? Username { get; set; }
        public DateTime? Expiration { get; set; }
        public string? Token { get; set; }
    }
}