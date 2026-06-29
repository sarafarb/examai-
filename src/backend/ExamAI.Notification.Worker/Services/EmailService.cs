using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ExamAI.Notification.Worker
{
    public class EmailService
    {
        private readonly ISendGridClient _client;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            // שומרים על קוד בטוח - אם אין מפתח בהגדרות, שמים מפתח זמני רק כדי שהקוד ירוץ
            var apiKey = config["SendGrid:ApiKey"] ?? "dummy_key";
            _client = new SendGridClient(apiKey);
            _fromEmail = config["SendGrid:FromEmail"] ?? "noreply@examai.com";
            _fromName = config["SendGrid:FromName"] ?? "ExamAI Team";
            _logger = logger;
        }

        public async Task<bool> SendAsync(string to, string subject, string templateId, object templateData)
        {
            var msg = new SendGridMessage();
            msg.SetFrom(new EmailAddress(_fromEmail, _fromName));
            msg.AddTo(new EmailAddress(to));
            msg.SetSubject(subject);
            msg.SetTemplateId(templateId);
            msg.SetTemplateData(templateData);

            try
            {
                var response = await _client.SendEmailAsync(msg);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return false;
            }
        }
    }
}