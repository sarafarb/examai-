using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using ExamAI.Identity.API.Configuration;
using ExamAI.Identity.API.Data;
using ExamAI.Identity.API.Endpoints;
using ExamAI.Identity.API.Services;
using ExamAI.Identity.API.Authorization;
using ExamAI.Identity.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. JWT & Configuration Settings
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>()!;

// 2. Register DB Context (Using In-Memory for Bootstrap testability)
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseInMemoryDatabase("IdentityDb"));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});
// 3. Register FluentValidation Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMemoryCache();
// 4. Register RabbitMQ Connection (Mocked/Singleton placeholder to satisfy dependency injection)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IConnection>(sp => {
    try {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        return factory.CreateConnection();
    } catch {
        // Fallback placeholder to allow local build/run without crash if RabbitMQ docker is off
        return new Moq.Mock<IConnection>().Object; 
    }
});

// 5. Auth Setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddExamAIAuthorization(); // ◄── קורא לרישום המעודכן עם כל ה-Policies
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseAuthentication();
app.UseAuthorization();

// ─── הוספת ה-Middleware לחסימת משתמשים מושעים ───
app.UseMiddleware<SuspendedUserMiddleware>();
app.UseCors("AllowAngular");
// Map Endpoints
app.MapAuthEndpoints();
app.MapGet("/health", () => Results.Ok("Identity API is live!"));

app.Run();