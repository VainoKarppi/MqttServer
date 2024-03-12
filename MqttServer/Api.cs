
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


    // TODO READ tokens FROM database and check expiration time
    readonly static Dictionary<string, string> ApiTokens = new() {
        { "123456", "user1" },
        { "token2", "user2" }
    };

    
    static WebApplication InitializeBuilder() {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        
        AddAuthorization(app);
        InitializePages(app);
        return app;
    }

    private static void AddAuthorization(WebApplication app) {
        app.Use(async (context, next) => {
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader)) {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authorization header is missing.");
                return;
            }

            //Todo, change this to database format.

            
            var token = authHeader.ToString().Split(" ").LastOrDefault();
            Database.User? user = await Database.GetUserByToken(token!);


            if (string.IsNullOrEmpty(token) || user is null) {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid or missing API token.");
                return;
            }

            // Add user information to the request context
            context.Items["user"] = ApiTokens[token];

            await next();
        });
    }

    public static string GenerateUserAndToken(string? username, string? expiration) {

        if (string.IsNullOrWhiteSpace(username)) throw new InvalidDataException("Invalid username!");

        DateTime expirationFinal = string.IsNullOrEmpty(expiration) ? DateTime.Now.AddYears(1) : DateTime.Now.AddDays(int.Parse(expiration));

        string token = Guid.NewGuid().ToString();
        Database.CreateUser(username,expirationFinal,token);

        return token;
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
        app.MapGet("/", (HttpContext context) => { return context.Response.WriteAsync("Hello, " + context.Items["user"]); });
        app.MapGet("/servertime", async () => await Pages.GetServerTime());
        app.MapGet("/jsontest", () => Pages.GetJsonResult());
        app.MapGet("/getAllWeatherData", () => Database.GetAllWeatherData());
    }

//Is token valid

    static bool IsTokenValid(string token) {
        if (Database.GetUserByToken(token) == null){
            return true;
        }

        return false;
    }


    
    

    //! ==================================
    //!             WEB PAGES
    //! ==================================

    protected static class Pages {

        internal static async Task<IResult> GetServerTime() {
            return await Task.Run(() => {
                return Results.Text($"{DateTime.Now}");
            });
        }

        internal static async Task<IResult> GetJsonResult() {
            return await Task.Run(() => {
                var data = new { Message = "asd" };
                return Results.Json(data);
            }); 
        }

    }

}