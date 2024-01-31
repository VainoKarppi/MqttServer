
using System;
using MySqlConnector;

static class Database {
    public static string IP = "127.0.0.1";
    public static string Username = "root";
    public static string Password = "";
    public static string DatabaseName = "test";
    public static MySqlConnection? Connection;


    public static Task ConnectToDatabase() {
        string connectionString = $"server={IP};uid={Username};pwd={Password}";

        Connection = new MySqlConnection(connectionString);
        Connection.Open();

        using MySqlCommand command = new ($"CREATE DATABASE IF NOT EXISTS {DatabaseName}", Connection);
        command.ExecuteNonQueryAsync();
        
        Connection.ChangeDatabase(DatabaseName);
        
        return Task.CompletedTask;
    }

}