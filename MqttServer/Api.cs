
using System;
using System.Data;
using MySqlConnector;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.WebEncoders.Testing;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;


static class MqttServerAPI {
    internal static WebApplication? WebApp = null;
    static MqttServerAPI() {
        WebApp = InitializeBuilder();
    }



    
    static WebApplication InitializeBuilder() {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        
        var urlHttp = builder.Configuration.GetValue<string>("Kestrel:Endpoints:Http:Url");
        if (urlHttp != null) Console.WriteLine($"\nAPI Server is running on address {urlHttp}");

        var urlHttps = builder.Configuration.GetValue<string>("Kestrel:Endpoints:Https:Url");
        if (urlHttps != null) Console.WriteLine($"API Server is running on address {urlHttps}\n");
        

        AddAuthorization(app);
        InitializePages(app);
        return app;
    }

    private static void AddAuthorization(WebApplication app) {
        app.Use(async (context, next) => {
            try {
                if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader)) {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Authorization header is missing.");
                    return;
                }

                
                var token = authHeader.ToString().Split(" ").LastOrDefault();
                if (string.IsNullOrEmpty(token) || token.ToLower() is "bearer") {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Invalid or missing API token.");
                    return;
                }

                Database.User? user = await Database.GetUserByToken(token!);
                if (user is null) {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid user credientials.");
                    return;
                }

                // Add user information to the request context
                context.Items["user"] = user;

                await next();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
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
    }

    public static void StopAPIServer() {
        if (WebApp is null) return;
        WebApp.StopAsync();
        Console.WriteLine("Stopped API server!");
    }

    static void InitializePages(WebApplication app) {
        app.MapGet("/", (HttpContext context) => {return context.Response.WriteAsync("Hello, " + context.Items["user"]); });
        app.MapGet("/authenticate", (HttpContext context) => {
            var user = (Database.User)context.Items["user"]!;

            var returnData = new {user.Id,user.Username,user.Expiration,user.Token};  
        
            return Results.Json(returnData);

        });
        app.MapGet("/servertime", async () => await Pages.GetServerTime());
        app.MapGet("/jsontest", () => Pages.GetJsonResult());
        app.MapGet("/getWeatherData", Pages.GetAllWeatherData);
    }




    

    //! ==================================
    //!             WEB PAGES
    //! ==================================

    protected static class Pages {
        internal static async Task GetAllWeatherData(HttpContext context) {

            DateOnly? start = DateOnly.TryParse(context.Request.Query["start"], out DateOnly startDate) ? startDate : null;
            DateOnly? end = DateOnly.TryParse(context.Request.Query["end"], out DateOnly endDate) ? endDate : null;

            Database.WeatherData[] data;
            Console.WriteLine($"start:{start}");
            Console.WriteLine($"END:{end}");
            if (end is not null) {
                if (start is null) startDate = DateOnly.FromDateTime(DateTime.Now);
                data = await Database.GetWeatherDataByTime(startDate, endDate);
            } else {
                data = await Database.GetAllWeatherData();
            }
            context.Response.WriteAsJsonAsync(data);
        }
        internal static async Task<IResult> AuthenticateUser() {
            return await Task.Run(() => {
                var data = new Database.User {
                    Id = 1,
                    Username = "asd",
                    Expiration = DateTime.Now.AddDays(2),
                    Token = "6dc3017d-0458-4312-ac46-43bc4d137561"
                };
                Console.WriteLine(data.Token);
                return Results.Json(data);
            }); 
        }
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