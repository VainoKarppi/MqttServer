
using System;
using System.Data;
using MySqlConnector;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.WebEncoders.Testing;
using System.Threading.Tasks;

static class MqttServerAPI {
    internal static WebApplication? WebApp = null;
    private static readonly HttpClient ProxyClient = new();
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
        app.MapGet("/jsontest", () => GetJsonResult());
    }


    //! ==================================
    //!             WEB PAGES
    //! ==================================
    static async Task<IResult> HelloWorld() {
        return await Task.Run(() => {
            return Results.Text("Hello World");
        });
    }
    static async Task<IResult> GetServerTime() {
        return await Task.Run(() => {
            return Results.Text($"{DateTime.Now}");
        });
    }

    static async Task<IResult> GetJsonResult() {
        return await Task.Run(() => {
            var data = new { Message = "asd" };
            return Results.Json(data);
        }); 
    }

}