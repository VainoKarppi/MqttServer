
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


    public static Task ConnectToDatabase() {
        string connectionString = $"server={IP};uid={Username};pwd={Password}";

        Connection = new MySqlConnection(connectionString);
        Connection.Open();

        using MySqlCommand command = new ($"CREATE DATABASE IF NOT EXISTS {DatabaseName}", Connection);
        if (command.ExecuteNonQuery() != 0)
            Console.WriteLine($"Database not found, and new was created! ({DatabaseName})");
        
        Connection.ChangeDatabase(DatabaseName);


        CreateTables();
        
        return Task.CompletedTask;
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

    public static Task CreateTable() {
        string tablename = "weatherdata";
        using MySqlCommand command = new ($"CREATE TABLE IF NOT EXISTS {tablename} NULL, NULL", Connection);

        return Task.CompletedTask;
    }

    public static Task CreateTables() {
        string tableName = "weatherdata";
        using MySqlCommand command = new ($@"CREATE TABLE IF NOT EXISTS {tableName} (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            timestamp TIMESTAMP,
            humidity FLOAT NULL DEFAULT NULL,
            temperature FLOAT NULL DEFAULT NULL,
            wind FLOAT NULL DEFAULT NULL,
            pressure FLOAT NULL DEFAULT NULL)
        ", Connection);

        command.ExecuteNonQuery();

        

        return Task.CompletedTask;
    }



    public static string GetAllWeatherData() {
        return "{RETURNED AS YEISON}";
    }

    public static void AddWeatherData() {
        string tableName = "weatherdata";
        string insertDataSql = $"INSERT INTO {tableName} (timestamp, humidity, temperature) VALUES (@timestamp, @humidity, @temperature)";
        DateTime timestamp = DateTime.Now;
        float humidity = 50.5f;
        float temperature = 25.0f;


        using MySqlCommand command = new MySqlCommand(insertDataSql, Connection);

        command.Parameters.AddWithValue("@timestamp", timestamp);
        command.Parameters.AddWithValue("@humidity", humidity);
        command.Parameters.AddWithValue("@temperature", temperature);

        int rowsChanged = command.ExecuteNonQuery();
        if (rowsChanged == 0) throw new Exception("Unable to weather add data!");
    }

}