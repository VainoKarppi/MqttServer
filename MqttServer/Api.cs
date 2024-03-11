
using System;
using System.Data;
using MySqlConnector;
using Microsoft.AspNetCore.Components;

static class MqttServerAPI {
    internal static WebApplication? WebApp = null;
    static MqttServerAPI() {
        WebApp = InitializeBuilder();
    }
    
    static WebApplication InitializeBuilder() {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        InitializePages(app);
        return app;
    }

    public static void StartAPIServer() {
        new Thread(async () => {
            if (WebApp is null) return;
            await WebApp.RunAsync();
        }).Start();
        Console.WriteLine("Started API server!");
    }

    public static void StopAPIServer() {
        if (WebApp is null) return;
        WebApp.StopAsync();
        Console.WriteLine("Stopped API server!");
    }

    static void InitializePages(WebApplication app) {
        app.MapGet("/", async () => await HelloWorld());
        app.MapGet("/servertime", async () => await GetServerTime());
    }

    static async Task<string> HelloWorld() {
        return await Task.Run(() => {
            return "Hello World";
        });
    }
    static async Task<string> GetServerTime() {
        return await Task.Run(() => {
            return $"{DateTime.Now}";
        });
    }
}