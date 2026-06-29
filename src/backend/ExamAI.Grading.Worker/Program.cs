using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using System;
using System.Threading.Tasks;
using ExamAI.Grading.Worker.Consumers;
using ExamAI.Grading.Worker.Services;
using ExamAI.Grading.Worker.Repositories;

var builder = Host.CreateApplicationBuilder(args);

// 1. שליפת מפתח ה-API
string apiKey = builder.Configuration["OpenAiApiKey"] ?? "YOUR_MOCK_KEY_IF_NOT_SET";

// 2. רישום שירותי הבדיקה, המרחקים, והקשיחות ב-Dependency Injection
builder.Services.AddSingleton<IOpenAiGradingService>(new OpenAiGradingService(apiKey));
builder.Services.AddSingleton<IStringSimilarityService, StringSimilarityService>();
builder.Services.AddSingleton<IStrictnessEngine, StrictnessEngine>();
builder.Services.AddSingleton<IScoreCalculationService, ScoreCalculationService>();

// 3. רישום ה-Repository (מוק זמני כדי שלא יצעק על שכבת הנתונים)
builder.Services.AddSingleton<IGradingRepository, MockGradingRepository>();

// 4. הגדרת MassTransit להאזנה לתור הבדיקות
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<GradingJobConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");

        cfg.ReceiveEndpoint("grading.jobs", e =>
        {
            e.ConfigureConsumer<GradingJobConsumer>(context);
        });
    });
});

var host = builder.Build();

// --- טריגר לבדיקה מהירה: שליחת הודעה עצמית 5 שניות אחרי העלייה ---
Task.Run(async () =>
{
    await Task.Delay(5000); 
    using var scope = host.Services.CreateScope();
    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
    
    Console.WriteLine("[Test Trigger] Publishing a dummy grading job to RabbitMQ...");
    await publishEndpoint.Publish(new GradingJobMessage
    {
        ExamId = Guid.NewGuid(),
        StudentExamId = Guid.NewGuid(),
        TeacherId = Guid.NewGuid()
    });
});
// -------------------------------------------------------------------

host.Run();

// 5. מחלקת המוק בשביל הריצה המקומית החלקה
public class MockGradingRepository : IGradingRepository
{
    public Task SaveStudentGradeAsync(StudentGradeEntity grade) => Task.CompletedTask;
    public Task UpdateStudentExamStatusAsync(Guid studentExamId, string status) => Task.CompletedTask;
    public Task<StudentGradeEntity?> GetGradeByStudentExamIdAsync(Guid studentExamId) => Task.FromResult<StudentGradeEntity?>(null);
    public Task<List<StudentGradeEntity>> GetGradesByExamIdAsync(Guid examId, string? statusFilter) => Task.FromResult(new List<StudentGradeEntity>());
    public Task<QuestionGradeEntity?> GetQuestionGradeByIdAsync(Guid questionGradeId) => Task.FromResult<QuestionGradeEntity?>(null);
    public Task SaveTeacherOverrideAsync(TeacherOverrideEntity teacherOverride) => Task.CompletedTask;
    public Task UpdateQuestionScoreAsync(Guid questionGradeId, decimal newScore) => Task.CompletedTask;
    public Task SaveAuditLogAsync(AuditLogEntity log) => Task.CompletedTask;
    public Task SaveCommentAsync(Guid studentGradeId, string comment) => Task.CompletedTask;
    public Task<List<object>> GetHistoryAsync(Guid studentGradeId) => Task.FromResult(new List<object>());
    public Task<bool> IsTeacherOwnerOfExamAsync(Guid teacherId, Guid examId) => Task.FromResult(true);
}