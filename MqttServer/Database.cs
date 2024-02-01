
using System;
using MySqlConnector;

static class Database {
    public static string IP = "karppi.dy.fi";
    public static string Username = "test";
    public static string Password = "test";
    public static string DatabaseName = "test";
    public static MySqlConnection? Connection;


    public static Task ConnectToDatabase() {
        string connectionString = $"server={IP};uid={Username};pwd={Password}";

        Connection = new MySqlConnection(connectionString);
        Connection.Open();

        using MySqlCommand command = new ($"CREATE DATABASE IF NOT EXISTS {DatabaseName}", Connection);
        if (command.ExecuteNonQuery() != 0)
            Console.WriteLine($"Database not found, and new was created! ({DatabaseName})");
        
        Connection.ChangeDatabase(DatabaseName);
        
        return Task.CompletedTask;
    }

}