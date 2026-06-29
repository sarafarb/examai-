using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using ExamAI.OCR.Worker;
using ExamAI.OCR.Worker.Workers;
using ExamAI.OCR.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

// --- רישום השירותים ל-Dependency Injection ---
builder.Services.AddTransient<IImagePreprocessingService, ImagePreprocessingService>();
builder.Services.AddTransient<IS3StorageService, DummyS3StorageService>();
builder.Services.AddTransient<IOcrRepository, DummyOcrRepository>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OcrJobWorker>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");

        cfg.ReceiveEndpoint("ocr.jobs", e =>
        {
            // T-042: Retry Logic - ניסיונות חוזרים לפי זמנים קבועים מראש
            e.UseMessageRetry(r => 
            {
                r.Intervals(
                    TimeSpan.FromMinutes(1),  // ניסיון שני אחרי דקה
                    TimeSpan.FromMinutes(5),  // ניסיון שלישי אחרי 5 דקות
                    TimeSpan.FromMinutes(15)  // ניסיון רביעי (ואחרון) אחרי 15 דקות
                );
            });

            e.PrefetchCount = 3;
            e.ConcurrentMessageLimit = 3; 

            e.ConfigureConsumer<OcrJobWorker>(context);
        });
    });
});

var host = builder.Build();
host.Run();