using ExamAI.Exam.API.Services;
using ExamAI.Exam.API.Security;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetEscapades.AspNetCore.SecurityHeaders;

var builder = WebApplication.CreateBuilder(args);

// הוספת תמיכה ב-Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// רישום השירותים החדשים לפרויקט ה-Exams (T-032)
builder.Services.AddSingleton<IFileValidationService, FileValidationService>();
builder.Services.AddSingleton<IVirusScanService, VirusScanService>();
builder.Services.AddSingleton<IS3FileService, S3FileService>();

builder.Services.AddScoped<IQuestionParserService, QuestionParserService>();
builder.Services.AddScoped<IRubricBuilderService, RubricBuilderService>();

// --- הגדרות אבטחה (T-062) ---

// 1. FluentValidation לאימות Input אוטומטי
builder.Services.AddFluentValidationAutoValidation();

// 2. הגנת CSRF (Antiforgery) מותאמת ל-Angular
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.HttpOnly = false; // אנגולר חייב לגשת לעוגייה כדי לשלוח את ההדר חזרה
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// 3. הגדרות Cookie מאובטחות גלובליות
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();

// --- Pipeline Middleware ---

if (app.Environment.IsDevelopment())
{
    // הגדרות פיתוח במידת הצורך
}

// 4. הגדרת כותרות אבטחה (Security Headers - CSP)
var policyCollection = new HeaderPolicyCollection()
    .AddDefaultSecurityHeaders()
    .AddContentSecurityPolicy(csp =>
    {
        csp.AddDefaultSrc().Self();
        csp.AddStyleSrc().Self().UnsafeInline(); // נדרש לרוב באנגולר
        csp.AddScriptSrc().Self();
        csp.AddImgSrc().Self().Data();
    });

app.UseSecurityHeaders(policyCollection);

// חובה להפעיל Routing לפני Antiforgery
app.UseRouting();

// 5. הפעלת מנגנון חסימת CSRF
app.UseAntiforgery();

app.UseAuthorization();
app.MapControllers();

app.Run();