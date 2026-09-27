using System.Net;
using System.Net.Mail;

namespace PromptForge.Api.Services
{
    /// <summary>E-posta gönderme sözleşmesi. Uygulama hangi yöntemle gönderildiğini bilmez.</summary>
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }

    /// <summary>
    /// GELİŞTİRME MODU: E-posta gerçekten gönderilmez, içeriği konsola (VS'deki siyah pencere) yazılır.
    /// Şifre sıfırlama bağlantısı ve doğrulama kodunu oradan kopyalayabilirsin.
    /// </summary>
    public class ConsoleEmailSender : IEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> _logger;
        public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

        public Task SendAsync(string to, string subject, string body)
        {
            _logger.LogWarning("\n===== E-POSTA (geliştirme modu, gönderilmedi) =====\nKime : {To}\nKonu : {Subject}\n{Body}\n===================================================", to, subject, body);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Gerçek e-posta (SMTP). appsettings/user secrets'ta "Email:Smtp:Host" tanımlıysa bu kullanılır.
    /// Gmail için: Host=smtp.gmail.com, Port=587, kullanıcı adı=Gmail adresi, şifre=Gmail "uygulama şifresi".
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public SmtpEmailSender(IConfiguration configuration) => _configuration = configuration;

        public async Task SendAsync(string to, string subject, string body)
        {
            var smtp = _configuration.GetSection("Email:Smtp");
            using var client = new SmtpClient(smtp["Host"], int.TryParse(smtp["Port"], out var port) ? port : 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtp["Username"], smtp["Password"])
            };
            using var message = new MailMessage(smtp["From"] ?? smtp["Username"]!, to, subject, body);
            await client.SendMailAsync(message);
        }
    }
}
