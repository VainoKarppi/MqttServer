
using System;
using System.Data;
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
        using MySqlCommand command = new ($@"CREATE TABLE IF NOT EXISTS {tableName} (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            timestamp TIMESTAMP,
            humidity FLOAT NULL DEFAULT NULL,
            temperature FLOAT NULL DEFAULT NULL,
            wind FLOAT NULL DEFAULT NULL,
            pressure FLOAT NULL DEFAULT NULL)
        ", Connection);

        await command.ExecuteNonQueryAsync();

        //TODO Create User table
        //TODO Create Logs table
    }



    public static string GetAllWeatherData() {
        return "{RETURNED AS YEISON}";
    }

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

}