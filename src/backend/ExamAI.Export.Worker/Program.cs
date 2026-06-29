using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using ExamAI.Export.Worker.Consumers;
using ExamAI.Export.Worker.Services;
using ExamAI.Grading.Worker.Repositories;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = Host.CreateApplicationBuilder(args);

// רישום מנוע יצירת ה-PDF בכתב יד
builder.Services.AddSingleton<ExamPdfGeneratorService>();

// רישום ה-Repository (נשתמש במוק הקיים כדי שהפרויקט ירוץ חלק)
builder.Services.AddSingleton<IGradingRepository, MockGradingRepository>();

// הגדרת MassTransit להאזנה לתור הייצוא (export.jobs)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ExportJobConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");

        cfg.ReceiveEndpoint("export.jobs", e =>
        {
            e.ConfigureConsumer<ExportJobConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();

// מוק לצורך ריצה מקומית חלקה ללא שגיאות קומפילציה
public class MockGradingRepository : IGradingRepository
{
    public Task SaveStudentGradeAsync(StudentGradeEntity grade) => Task.CompletedTask;
    public Task UpdateStudentExamStatusAsync(Guid studentExamId, string status) => Task.CompletedTask;
    public Task<StudentGradeEntity?> GetGradeByStudentExamIdAsync(Guid studentExamId) => Task.FromResult<StudentGradeEntity?>(new StudentGradeEntity { StudentName = "ישראל ישראלי", TotalScore = 85, QuestionGrades = new() });
    public Task<List<StudentGradeEntity>> GetGradesByExamIdAsync(Guid examId, string? statusFilter) => Task.FromResult(new List<StudentGradeEntity>());
    public Task<QuestionGradeEntity?> GetQuestionGradeByIdAsync(Guid questionGradeId) => Task.FromResult<QuestionGradeEntity?>(null);
    public Task SaveTeacherOverrideAsync(TeacherOverrideEntity teacherOverride) => Task.CompletedTask;
    public Task UpdateQuestionScoreAsync(Guid questionGradeId, decimal newScore) => Task.CompletedTask;
    public Task SaveAuditLogAsync(AuditLogEntity log) => Task.CompletedTask;
    public Task SaveCommentAsync(Guid studentGradeId, string comment) => Task.CompletedTask;
    public Task<List<object>> GetHistoryAsync(Guid studentGradeId) => Task.FromResult(new List<object>());
    public Task<bool> IsTeacherOwnerOfExamAsync(Guid teacherId, Guid examId) => Task.FromResult(true);
}