using Microsoft.EntityFrameworkCore;
using ExamAI.Shared.Data; 
using ExamAI.Shared.Seeders; 
using ExamAI.Shared.Messaging;
using Serilog;
using Serilog.Formatting.Json;
using ExamAI.Shared.Logging;
using ExamAI.Shared.Telemetry;
using ExamAI.Admin.API.Data;

var builder = WebApplication.CreateBuilder(args);
// מוודא שהפרויקט מזהה את ה-DbContext שלכם
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "ExamAI.Admin.API") // בפרויקט האנליטיקה שנו ל-"ExamAI.Analytics.API"
    .Destructure.With(new SensitiveDataScrubber()) // מפעיל את מנקה הסיסמאות
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddExamTelemetry("ExamAI.Admin.API");
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddTransient<RabbitMqTopologyInitializer>();
// רישום ה-IdentityDbContext במערכת
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=exam_db;Username=postgres;Password=postgres"));

// רישום ה-AdminDbContext במערכת
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=exam_db;Username=postgres;Password=postgres"));
// רישום ה-AdminDbContext במערכת עבור פאנל הניהול
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=exam_db;Username=postgres;Password=postgres"));

// רישום Distributed Cache כדי שהקונטרולר של ההגדרות יוכל לנקות את המטמון (T-058)
builder.Services.AddDistributedMemoryCache();
// רישום ה-AuditDbContext במערכת (כדי שלא יצעק על זה במיגרציה הבאה)
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=exam_db;Username=postgres;Password=postgres"));
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// 1. קודם כל ה-Correlation ID כדי שייצר מזהה לכל הצינור
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. ה-Request Logger שימדוד וידפיס את הנתונים
app.UseMiddleware<RequestLoggingMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<RabbitMqTopologyInitializer>();
    try
    {
        initializer.Initialize();
        Console.WriteLine("------> RabbitMQ Topology Initialized Successfully! <------");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"RabbitMQ Initialization Failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// מוודא שהקובץ מזהה את ה-Seeder

// מנגנון אוטומטי להרצת ה-Seed על יבש/רטוב
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var identityContext = services.GetRequiredService<IdentityDbContext>();
        
        // כאן בעתיד הפקודה הזו תריץ את המיגרציות אוטומטית:
        // await identityContext.Database.MigrateAsync();
        
        // הרצת הזרקת הנתונים (Roles, Plans)
        await DatabaseSeeder.SeedIdentityDataAsync(identityContext);
        Console.WriteLine("------> Database Seeding Completed Successfully! <------");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
    }
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
