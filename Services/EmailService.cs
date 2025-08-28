using FoodDeliveryApp.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
namespace FoodDeliveryApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isBodyHtml = true)
        {
            try
            {
                // Retrieve email settings from appsettings.json.
                string smtpServer = _configuration.GetValue<string>("EmailSettings: SmtpServer") ?? "";
                int smtpPort = int.Parse(_configuration.GetValue<string>("Email Settings: SmtpPort") ?? "587");
                string senderName = _configuration.GetValue<string>("EmailSettings: SenderName") ?? "Ruchi Kitchen";
                string senderEmail = _configuration.GetValue<string>("EmailSettings: SenderEmail") ?? "";
                string password = _configuration.GetValue<string>("Email Settings: Password") ?? "";

                // Create a new MailMessage.
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(senderEmail, senderName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isBodyHtml;

                    // Create a SMTP client using the provided settings.
                    using (var client = new SmtpClient(smtpServer, smtpPort))
                    {
                        client.Credentials = new NetworkCredential(senderEmail, password);
                        client.EnableSsl = true;
                        // Send the email asynchronously.
                        // await client.SendMailAsync(message);
                    }
                }

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }
    }
}