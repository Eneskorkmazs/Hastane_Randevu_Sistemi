using System.Net;
using System.Net.Mail;

namespace HastaneRandevuSistemi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("E-posta gonderimi atlandi: alici adresi bos.");
                return false;
            }

            try
            {
                var host = _configuration["EmailSettings:Host"];
                var port = _configuration.GetValue<int?>("EmailSettings:Port") ?? 587;
                var fromEmail = _configuration["EmailSettings:Mail"];
                var displayName = _configuration["EmailSettings:DisplayName"] ?? fromEmail;
                var password = _configuration["EmailSettings:Password"];
                var enableSsl = _configuration.GetValue("EmailSettings:EnableSsl", true);

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("E-posta ayarlari eksik oldugu icin gonderim atlandi. Host/Mail/Password kontrol edin.");
                    return false;
                }

                using var smtpClient = new SmtpClient(host)
                {
                    Port = port,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromEmail, password),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, displayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gonderimi basarisiz oldu. Alici: {Recipient}", toEmail);
                return false;
            }
        }
    }
}
