using ExamAI.Notification.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddHostedService<NotificationBackgroundWorker>();

var host = builder.Build();
host.Run();
